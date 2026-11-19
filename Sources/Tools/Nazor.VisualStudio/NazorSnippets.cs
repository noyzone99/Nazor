using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using MSXML;

namespace Nazor.VisualStudio
{
    [Export(typeof(ICommandHandler))]
    [ContentType(NazorContentTypes.Component)]
    [Name("Nazor snippets")]
    [Order(Before = "CompletionCommandHandler")]
    internal sealed class SnippetCommandHandler :
        ICommandHandler<TabKeyCommandArgs>, ICommandHandler<BackTabKeyCommandArgs>, ICommandHandler<ReturnKeyCommandArgs>, ICommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<SurroundWithCommandArgs>
    {
        private readonly NazorLanguageClient client;
        private readonly IAsyncCompletionBroker completion;
        private readonly IVsEditorAdaptersFactoryService adapters;
        private readonly ITextDocumentFactoryService documents;

        [ImportingConstructor]
        public SnippetCommandHandler(NazorLanguageClient client, IAsyncCompletionBroker completion, IVsEditorAdaptersFactoryService adapters, ITextDocumentFactoryService documents)
        {
            this.client = client;
            this.completion = completion;
            this.adapters = adapters;
            this.documents = documents;
        }

        public string DisplayName => "Nazor snippets";

        public CommandState GetCommandState(SurroundWithCommandArgs args) => args.TextView.Selection.IsEmpty ? CommandState.Unspecified : CommandState.Available;

        public bool ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext executionContext)
        {
            var view = args.TextView;
            var buffer = args.SubjectBuffer;
            if (client.Rpc == null || view.Selection.IsEmpty || !(view is IWpfTextView wpf) ||
                !documents.TryGetTextDocument(buffer, out var document)) return false;
            var selected = view.BufferGraph.MapDownToBuffer(view.Selection.StreamSelectionSpan.SnapshotSpan, SpanTrackingMode.EdgeExclusive, buffer);
            if (selected.Count != 1) return false;
            var snapshot = buffer.CurrentSnapshot;
            SnippetResponse[] choices;
            try
            {
                choices = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        return await client.Rpc.InvokeWithParameterObjectAsync<SnippetResponse[]>("nazor/surroundWith", new
                        {
                            textDocument = new { uri = new Uri(document.FilePath).AbsoluteUri }, text = snapshot.GetText(),
                            start = selected[0].Start.Position, end = selected[0].End.Position,
                        }, timeout.Token);
                });
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException)) { return false; }
            if (choices == null || choices.Length == 0 || buffer.CurrentSnapshot != snapshot) return false;
            var menu = new ContextMenu { PlacementTarget = wpf.VisualElement, Placement = PlacementMode.Center };
            foreach (var choice in choices)
            {
                var item = new MenuItem { Header = choice.title };
                item.Click += (sender, e) =>
                {
                    if (buffer.CurrentSnapshot == snapshot) Insert(buffer, snapshot, choice);
                    wpf.VisualElement.Focus();
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
            return true;
        }

        public CommandState GetCommandState(TabKeyCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext executionContext) =>
            SnippetSession.TryGet(args.SubjectBuffer, out var session) ? session.Next() : TryExpand(args.TextView, args.SubjectBuffer);

        public CommandState GetCommandState(BackTabKeyCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(BackTabKeyCommandArgs args, CommandExecutionContext executionContext) =>
            SnippetSession.TryGet(args.SubjectBuffer, out var session) && session.Previous();

        public CommandState GetCommandState(ReturnKeyCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext) =>
            SnippetSession.TryGet(args.SubjectBuffer, out var session) && session.Commit();

        public CommandState GetCommandState(EscapeKeyCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            if (!SnippetSession.TryGet(args.SubjectBuffer, out var session)) return false;
            session.End(leaveCaret: true);
            return true;
        }

        private bool TryExpand(ITextView view, ITextBuffer buffer)
        {
            var rpc = client.Rpc;
            if (rpc == null || !view.Selection.IsEmpty) return false;
            var caret = view.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Predecessor);
            if (caret == null || !documents.TryGetTextDocument(buffer, out var document)) return false;
            var point = caret.Value;
            var snapshot = point.Snapshot;
            var start = point.Position;
            while (start > 0 && (char.IsLetterOrDigit(snapshot[start - 1]) || snapshot[start - 1] == '_')) start--;
            if (start == point.Position) return false;
            var line = point.GetContainingLine();
            SnippetResponse response;
            try
            {
                response = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                        return await rpc.InvokeWithParameterObjectAsync<SnippetResponse>("nazor/snippet", new
                        {
                            textDocument = new { uri = new Uri(document.FilePath).AbsoluteUri },
                            position = new { line = line.LineNumber, character = point.Position - line.Start.Position },
                            text = snapshot.GetText(),
                        }, timeout.Token);
                });
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                return false;
            }
            if (response?.snippet == null || buffer.CurrentSnapshot != snapshot) return false;
            completion.GetSession(view)?.Dismiss();
            return Insert(buffer, snapshot, response);
        }

        private bool Insert(ITextBuffer buffer, ITextSnapshot snapshot, SnippetResponse response)
        {
            if (response?.range?.start == null || response.range.end == null || string.IsNullOrEmpty(response.snippet) || buffer.CurrentSnapshot != snapshot) return false;
            if (!(adapters.GetBufferAdapter(buffer) is IVsExpansion expansion)) return false;
            var xml = new DOMDocument();
            if (!xml.loadXML(SnippetConversion.ToVisualStudio(response.snippet))) return false;
            var newLine = snapshot.LineCount > 1 ? snapshot.GetLineFromLineNumber(0).GetLineBreakText() : Environment.NewLine;
            var span = new TextSpan
            {
                iStartLine = response.range.start.line,
                iStartIndex = response.range.start.character,
                iEndLine = response.range.end.line,
                iEndIndex = response.range.end.character,
            };
            ((IVsTextBuffer)expansion).GetLanguageServiceID(out var language);
            return expansion.InsertSpecificExpansion(xml.documentElement, span, new SnippetClient(buffer, newLine), language, string.Empty, out _) == VSConstants.S_OK;
        }
    }

#pragma warning disable IDE1006
    internal sealed class SnippetResponse
    {
        public string title { get; set; }
        public LspRange range { get; set; }
        public string snippet { get; set; }
    }

    internal sealed class LspRange
    {
        public LspPosition start { get; set; }
        public LspPosition end { get; set; }
    }

    internal sealed class LspPosition
    {
        public int line { get; set; }
        public int character { get; set; }
    }
#pragma warning restore IDE1006

    internal sealed class SnippetSession
    {
        private readonly IVsExpansionSession session;

        public IVsExpansionSession Expansion => session;
        private readonly ITextBuffer buffer;
        private bool active = true;

        private SnippetSession(IVsExpansionSession session, ITextBuffer buffer)
        {
            this.session = session;
            this.buffer = buffer;
        }

        public static void Start(IVsExpansionSession session, ITextBuffer buffer)
        {
            if (TryGet(buffer, out var previous)) previous.End(leaveCaret: true);
            buffer.Properties[typeof(SnippetSession)] = new SnippetSession(session, buffer);
        }

        public static bool TryGet(ITextBuffer buffer, out SnippetSession session) => buffer.Properties.TryGetProperty(typeof(SnippetSession), out session);

        public static void Finished(ITextBuffer buffer)
        {
            if (TryGet(buffer, out var session)) session.active = false;
            buffer.Properties.RemoveProperty(typeof(SnippetSession));
        }

        public bool Next()
        {
            if (session.GoToNextExpansionField(1) == VSConstants.S_OK) return true;
            End(leaveCaret: false);
            return true;
        }

        public bool Previous()
        {
            if (session.GoToPreviousExpansionField() == VSConstants.S_OK) return true;
            return true;
        }

        public bool Commit()
        {
            End(leaveCaret: false);
            return true;
        }

        public void End(bool leaveCaret)
        {
            if (!active) return;
            active = false;
            session.EndCurrentExpansion(leaveCaret ? 1 : 0);
            buffer.Properties.RemoveProperty(typeof(SnippetSession));
        }
    }

    internal sealed class SnippetClient : IVsExpansionClient
    {
        private readonly ITextBuffer buffer;
        private readonly string newLine;

        public SnippetClient(ITextBuffer buffer, string newLine)
        {
            this.buffer = buffer;
            this.newLine = newLine;
        }

        public int OnBeforeInsertion(IVsExpansionSession pSession)
        {
            SnippetSession.Start(pSession, buffer);
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession) => VSConstants.S_OK;

        public int EndExpansion()
        {
            SnippetSession.Finished(buffer);
            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
        {
            if (ts == null || ts.Length == 0) return VSConstants.S_OK;
            var end = new TextSpan[1];
            var hasEnd = SnippetSession.TryGet(buffer, out var session) && session.Expansion.GetEndSpan(end) == VSConstants.S_OK;
            var snapshot = buffer.CurrentSnapshot;
            using (var edit = buffer.CreateEdit())
            {
                for (var number = ts[0].iStartLine; number < ts[0].iEndLine && number < snapshot.LineCount; number++)
                {
                    var line = snapshot.GetLineFromLineNumber(number);
                    if (line.LineBreakLength > 0 && line.GetLineBreakText() != newLine) edit.Replace(line.End.Position, line.LineBreakLength, newLine);
                }
                edit.Apply();
            }
            if (hasEnd) session.Expansion.SetEndSpan(end[0]);
            return VSConstants.S_OK;
        }

        public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            pFunc = null;
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
        {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
        {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts) => VSConstants.S_OK;

        public int OnItemChosen(string pszTitle, string pszPath) => VSConstants.E_NOTIMPL;
    }
}
