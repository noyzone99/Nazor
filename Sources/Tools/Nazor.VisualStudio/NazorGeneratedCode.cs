using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class GeneratedCodeNavigation : IWpfTextViewCreationListener
    {
        [Import]
        private ITextDocumentFactoryService Documents { get; set; }

        [Import]
        private NazorLanguageClient Client { get; set; }

        [Import]
        private IVsEditorAdaptersFactoryService Adapters { get; set; }

        public void TextViewCreated(IWpfTextView view)
        {
            if (!Documents.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out var document) || !IsGenerated(document.FilePath)) return;
            var path = document.FilePath;
            var deadline = DateTime.UtcNow.AddSeconds(3);
            var done = false;
            EventHandler<CaretPositionChangedEventArgs> moved = null;
            void Check()
            {
                if (done) return;
                if (DateTime.UtcNow > deadline || view.IsClosed)
                {
                    done = true;
                    view.Caret.PositionChanged -= moved;
                    return;
                }
                var position = view.Caret.Position.BufferPosition;
                if (position.Position == 0) return;
                done = true;
                view.Caret.PositionChanged -= moved;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RedirectAsync(view, path, position);
                    }
                    catch (Exception exception) when (!(exception is OutOfMemoryException))
                    {
                    }
                });
            }
            moved = (sender, e) => Check();
            view.Caret.PositionChanged += moved;
            view.Closed += (sender, e) => view.Caret.PositionChanged -= moved;
            ThreadHelper.JoinableTaskFactory.StartOnIdle(Check).FileAndForget("nazor/navigation");
        }

        private static bool IsGenerated(string path) =>
            path != null && path.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) &&
            path.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0;

        private async Task RedirectAsync(IWpfTextView view, string path, SnapshotPoint position)
        {
            var text = position.Snapshot.GetText();
            var line = position.GetContainingLine();
            (string Path, int Line, int Character)? target = null;
            if (Client.Rpc is StreamJsonRpc.JsonRpc rpc)
            {
                try
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        var found = await rpc.InvokeWithParameterObjectAsync<LspLocation>("nazor/originalPosition", new
                        {
                            path,
                            text,
                            position = new { line = line.LineNumber, character = position.Position - line.Start.Position },
                        }, timeout.Token);
                        if (found?.uri != null && found.range?.start != null) target = (new Uri(found.uri).LocalPath, found.range.start.line, found.range.start.character);
                    }
                }
                catch (Exception exception) when (!(exception is OutOfMemoryException))
                {
                }
            }
            if (target == null) target = LineDirective(position);
            if (target == null || !File.Exists(target.Value.Path)) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, target.Value.Path, Guid.Empty, out _, out _, out _, out IVsTextView component);
            if (component != null)
            {
                component.SetCaretPos(target.Value.Line, target.Value.Character);
                component.CenterLines(target.Value.Line, 1);
            }
            if (!view.IsClosed && VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, path, Guid.Empty, out _, out _, out IVsWindowFrame generated) && generated != null)
                generated.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }

        private static (string Path, int Line, int Character)? LineDirective(SnapshotPoint position)
        {
            var snapshot = position.Snapshot;
            var line = position.GetContainingLine();
            for (var number = line.LineNumber - 1; number >= 0; number--)
            {
                var directive = snapshot.GetLineFromLineNumber(number).GetText().Trim();
                if (!directive.StartsWith("#line", StringComparison.Ordinal)) continue;
                var rest = directive.Substring("#line".Length).Trim();
                var quote = rest.IndexOf('"');
                if (quote < 0 || rest.LastIndexOf('"') <= quote) return null;
                var file = rest.Substring(quote + 1, rest.LastIndexOf('"') - quote - 1);
                var head = rest.Substring(0, quote).Trim();
                var digits = new string(head.TrimStart('(').TakeWhile(char.IsDigit).ToArray());
                if (!int.TryParse(digits, out var first) || !file.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase)) return null;
                return (file, first - 1 + line.LineNumber - number - 1, position.Position - line.Start.Position);
            }
            return null;
        }
    }

#pragma warning disable IDE1006
    internal sealed class LspLocation
    {
        public string uri { get; set; }
        public LspRange range { get; set; }
        public string preview { get; set; }
        public string before1 { get; set; }
        public string before2 { get; set; }
        public string after1 { get; set; }
        public string after2 { get; set; }
    }
#pragma warning restore IDE1006

    internal interface IGeneratedCodeWatcher
    {
        void Start();
        Task SynchronizeAsync(string componentPath);
    }

    [Export(typeof(IGeneratedCodeWatcher))]
    internal sealed class GeneratedCodeWatcher : IGeneratedCodeWatcher
    {
        private readonly VisualStudioWorkspace workspace;
        private readonly NazorLanguageClient client;
        private bool started;

        [ImportingConstructor]
        public GeneratedCodeWatcher(VisualStudioWorkspace workspace, NazorLanguageClient client)
        {
            this.workspace = workspace;
            this.client = client;
        }

        public void Start()
        {
            if (started) return;
            started = true;
            workspace.WorkspaceChanged += OnWorkspaceChanged;
            workspace.DocumentClosed += (sender, e) =>
            {
                if (e.Document.FilePath is string path && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    ThreadHelper.JoinableTaskFactory.RunAsync(() => SynchronizeAsync(path)).FileAndForget("nazor/csharpDocuments");
            };
        }

        public async Task SynchronizeAsync(string componentPath)
        {
            if (client.Rpc == null) return;
            var solution = workspace.CurrentSolution;
            var sidecar = solution.GetDocumentIdsWithFilePath(componentPath + ".cs").Select(solution.GetDocument).FirstOrDefault();
            var project = sidecar?.Project ?? solution.Projects.Where(p => p.FilePath != null &&
                componentPath.StartsWith(Path.GetDirectoryName(p.FilePath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.FilePath.Length).FirstOrDefault();
            if (project?.FilePath == null) return;
            var candidates = project.Documents.Where(d => workspace.IsDocumentOpen(d.Id));
            var texts = new System.Collections.Generic.List<object>();
            foreach (var document in candidates.Where(d => d.FilePath != null && !d.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)))
                texts.Add(new { path = document.FilePath, text = (await document.GetTextAsync().ConfigureAwait(false)).ToString() });
            await client.Rpc.NotifyWithParameterObjectAsync("nazor/csharpDocuments", new { projectDirectory = Path.GetDirectoryName(project.FilePath), documents = texts }).ConfigureAwait(false);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            var rpc = client.Rpc;
            if (e.Kind != WorkspaceChangeKind.DocumentChanged || e.DocumentId == null || rpc == null) return;
            var before = e.OldSolution.GetDocument(e.DocumentId);
            var after = e.NewSolution.GetDocument(e.DocumentId);
            var path = before?.FilePath;
            if (after == null || path == null) return;
            if (!path.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase))
            {
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && path.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) < 0)
                    ThreadHelper.JoinableTaskFactory.RunAsync(() => SynchronizeAsync(path)).FileAndForget("nazor/csharpDocuments");
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    var text = await before.GetTextAsync().ConfigureAwait(false);
                    var changes = await after.GetTextChangesAsync(before).ConfigureAwait(false);
                    await rpc.NotifyWithParameterObjectAsync("nazor/generatedChanged", new
                    {
                        path,
                        oldText = text.ToString(),
                        changes = changes.Select(c => new { start = c.Span.Start, length = c.Span.Length, text = c.NewText }).ToArray(),
                    }).ConfigureAwait(false);
                }
                catch (Exception exception) when (!(exception is OutOfMemoryException))
                {
                }
            });
        }
    }
}
