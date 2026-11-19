using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Nazor.Authoring;

namespace Nazor.VisualStudio
{
    [Export(typeof(ICommandHandler))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    [Name("Nazor editing")]
    [Order(After = "CompletionCommandHandler")]
    [Order(Before = "BraceCompletionCommandHandler")]
    internal sealed class NazorEditingCommands : IChainedCommandHandler<TypeCharCommandArgs>, IChainedCommandHandler<ReturnKeyCommandArgs>,
        IChainedCommandHandler<TabKeyCommandArgs>, IChainedCommandHandler<BackspaceKeyCommandArgs>
    {
        private readonly ITextBufferUndoManagerProvider undoManagers;
        private readonly IEditorOperationsFactoryService operations;
        private readonly ITextDocumentFactoryService documents;
        private readonly VisualStudioWorkspace workspace;
        private readonly IServiceProvider services;

        [ImportingConstructor]
        public NazorEditingCommands(ITextBufferUndoManagerProvider undoManagers, IEditorOperationsFactoryService operations, ITextDocumentFactoryService documents, VisualStudioWorkspace workspace,
            [Import(typeof(SVsServiceProvider))] IServiceProvider services)
        {
            this.undoManagers = undoManagers;
            this.operations = operations;
            this.documents = documents;
            this.workspace = workspace;
            this.services = services;
            if (workspace != null) NazorCSharpEditing.Configure(workspace.Services.HostServices, FindProject);
        }

        public string DisplayName => "Nazor editing";
        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> next) => next();
        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> next) => next();
        public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> next) => next();
        public CommandState GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> next) => next();

        public void ExecuteCommand(TypeCharCommandArgs args, Action next, CommandExecutionContext context)
        {
            if (!Ready(args.TextView, args.SubjectBuffer, out var caret)) { next(); return; }
            var view = args.TextView;
            var buffer = args.SubjectBuffer;
            var state = State.For(view, buffer, caret);
            var pair = state.Top;
            var text = buffer.CurrentSnapshot.GetText();
            var closingPosition = pair == null ? caret : Math.Max(caret, pair.CloseStart);
            if (pair != null && !pair.Closing.StartsWith("</", StringComparison.Ordinal) && closingPosition < pair.CloseEnd.GetPosition(buffer.CurrentSnapshot) && text[closingPosition] == args.TypedChar &&
                text.Substring(caret, closingPosition - caret).All(char.IsWhiteSpace))
            {
                if (!pair.CSharp || Native(() => NazorCSharpEditing.Overtype(text, NazorEditingContext.At(text, pair.Open.GetPosition(buffer.CurrentSnapshot) + 1, Component(buffer)),
                    pair.Open.GetPosition(buffer.CurrentSnapshot), pair.CloseEnd.GetPosition(buffer.CurrentSnapshot), caret, pair.Opening[0], Options(view, buffer), pair.Service)) != null)
                {
                    Edit(view, buffer, "Overtype Nazor closer", () =>
                    {
                        buffer.Delete(new Span(caret, closingPosition - caret));
                        Move(view, buffer, caret + 1);
                        if (pair.CSharp && args.TypedChar == '}') Format(view, buffer, caret + 1);
                    });
                    return;
                }
            }
            var character = args.TypedChar;
            var typed = text.Insert(caret, character.ToString());
            var region = NazorEditingContext.At(typed, caret + 1, Component(buffer));
            var before = NazorEditingContext.At(text, caret, Component(buffer));
            if (before.Language == EditingLanguage.CSharp && view.Caret.Position.VirtualBufferPosition.VirtualSpaces > 0 && "{([\"'<".IndexOf(character) < 0)
            {
                var preferences = Options(view, buffer);
                Edit(view, buffer, "Type indented C#", () => { caret = Materialize(view, buffer, caret, preferences); buffer.Insert(caret, character.ToString()); Move(view, buffer, caret + 1); if (character == ';' || character == '}') Format(view, buffer, caret + 1); });
                return;
            }
            if (before.Language == EditingLanguage.CSharp && (character == ';' || character == '}'))
            {
                Edit(view, buffer, "Type C# terminator", () => { buffer.Insert(caret, character.ToString()); Move(view, buffer, caret + 1); Format(view, buffer, caret + 1); });
                return;
            }
            var pairing = view.Options.GetOptionValue(DefaultTextViewOptions.BraceCompletionEnabledOptionId);
            if (before.Language == EditingLanguage.CSharp || region.Language == EditingLanguage.CSharp)
                pairing = NazorCSharpOptions.BraceCompletionEnabled(services, pairing);
            // NSS has no native language preference; PlainText's disabled default does not apply to styles.
            if (before.Language == EditingLanguage.Style || region.Language == EditingLanguage.Style) pairing = true;
            if (!pairing) { next(); return; }
            var closing = "";
            var opening = character.ToString();
            if (character == '>' && before.Tag != null && before.Language == EditingLanguage.Markup && text[before.TagStart + 1] != '/' && (caret == 0 || text[caret - 1] != '/'))
                closing = HasEndTag(typed, before.TagStart, before.Tag) ? "" : "</" + before.Tag + ">";
            else if (character == '=' && before.Tag != null && before.Language == EditingLanguage.Markup && (caret == text.Length || text[caret] != '"' && text[caret] != '\''))
                closing = "\"\"";
            else if (character == '*' && caret > 0 && text[caret - 1] == '@' && before.Language == EditingLanguage.Markup)
            { opening = "@*"; closing = "*@"; }
            else if (character == '-' && caret >= 3 && text.Substring(caret - 3, 3) == "<!-" && before.Language == EditingLanguage.Markup)
            { opening = "<!--"; closing = "-->"; }
            else if (character == '*' && caret > 0 && text[caret - 1] == '/' && before.Language == EditingLanguage.Style && !NazorEditingContext.IsLiteral(text, caret - 1))
            { opening = "/*"; closing = "*/"; }
            else if ("{([\"'<".IndexOf(character) >= 0)
            {
                if (region.Language == EditingLanguage.CSharp && !region.Directive)
                {
                    var options = Options(view, buffer);
                    var completed = Native(() => NazorCSharpEditing.Complete(typed, region, caret, character, options));
                    Edit(view, buffer, "Type C# delimiter", () =>
                    {
                        var original = caret;
                        caret = Materialize(view, buffer, caret, options);
                        buffer.Insert(caret, character.ToString());
                        if (caret != original)
                        {
                            typed = buffer.CurrentSnapshot.GetText();
                            region = NazorEditingContext.At(typed, caret + 1, Component(buffer));
                            completed = Native(() => NazorCSharpEditing.Complete(typed, region, caret, character, options));
                        }
                        if (completed == null) { Move(view, buffer, caret + 1); return; }
                        Apply(view, buffer, completed);
                        var close = character == '{' ? "}" : character == '(' ? ")" : character == '[' ? "]" : character == '<' ? ">" : character.ToString();
                        state.Add(buffer, caret, character.ToString(), completed.Caret + close.Length, close, false, true);
                        var current = state.Top;
                        current.Service = completed.Service;
                        Apply(view, buffer, Native(() => NazorCSharpEditing.After(buffer.CurrentSnapshot.GetText(),
                            NazorEditingContext.At(buffer.CurrentSnapshot.GetText(), current.Open.GetPosition(buffer.CurrentSnapshot) + 1, Component(buffer)),
                            current.Open.GetPosition(buffer.CurrentSnapshot), current.CloseEnd.GetPosition(buffer.CurrentSnapshot), completed.Caret, character, false, options, current.Service)));
                    });
                    return;
                }
                if ((region.Language == EditingLanguage.Style && !NazorEditingContext.IsLiteral(text, caret) || region.Directive ||
                    before.Language == EditingLanguage.Markup && before.Tag != null && (character == '"' || character == '\'')) && character != '<')
                    closing = character == '{' ? "}" : character == '(' ? ")" : character == '[' ? "]" : character.ToString();
                else if (before.Language == EditingLanguage.Comment || before.Language == EditingLanguage.Attribute || before.Language == EditingLanguage.Style)
                {
                    Edit(view, buffer, "Type literal delimiter", () => { caret = Materialize(view, buffer, caret); buffer.Insert(caret, character.ToString()); Move(view, buffer, caret + 1); });
                    return;
                }
                else { next(); return; }
            }
            else { next(); return; }
            Edit(view, buffer, "Complete Nazor delimiter", () =>
            {
                caret = Materialize(view, buffer, caret);
                buffer.Insert(caret, character + closing);
                var target = caret + 1;
                if (character == '=' && closing.Length > 0)
                { state.Add(buffer, target, "\"", target + 2, "\"", false); target++; }
                else if (closing.Length > 0) state.Add(buffer, target - opening.Length, opening, target + closing.Length, closing, region.Directive);
                Move(view, buffer, target);
            });
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action next, CommandExecutionContext context)
        {
            if (!Ready(args.TextView, args.SubjectBuffer, out var caret)) { next(); return; }
            var view = args.TextView;
            var buffer = args.SubjectBuffer;
            var text = buffer.CurrentSnapshot.GetText();
            var region = NazorEditingContext.At(text, caret, Component(buffer));
            var pair = State.For(view, buffer, caret).Top;
            if (region.Language == EditingLanguage.Comment || region.Language == EditingLanguage.Attribute)
            { next(); return; }
            if (pair == null && region.Language == EditingLanguage.Markup) pair = EmptyElement(text, caret, buffer);
            if (pair == null && (region.Language == EditingLanguage.Style || region.Language == EditingLanguage.CSharp))
            {
                var left = caret - 1;
                var right = caret;
                while (left >= 0 && char.IsWhiteSpace(text[left])) left--;
                while (right < text.Length && char.IsWhiteSpace(text[right])) right++;
                if (left >= 0 && right < text.Length && text[left] == '{' && text[right] == '}')
                    pair = new Pair { Open = buffer.CurrentSnapshot.CreateTrackingPoint(left, PointTrackingMode.Positive), Opening = "{", Closing = "}",
                        CloseEnd = buffer.CurrentSnapshot.CreateTrackingPoint(right + 1, PointTrackingMode.Negative), Directive = left == region.Start - 1 };
            }
            var options = Options(view, buffer);
            var newline = view.Options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId);
            Edit(view, buffer, "Nazor newline", () =>
            {
                buffer.Insert(caret, newline);
                caret += newline.Length;
                text = buffer.CurrentSnapshot.GetText();
                region = NazorEditingContext.At(text, caret, Component(buffer));
                if (region.Language == EditingLanguage.CSharp && (pair == null || !pair.Directive || !text.Substring(pair.Open.GetPosition(buffer.CurrentSnapshot) + pair.Opening.Length,
                    pair.CloseStart - pair.Open.GetPosition(buffer.CurrentSnapshot) - pair.Opening.Length).All(char.IsWhiteSpace)))
                {
                    var formatted = pair == null || pair.Directive || caret > pair.CloseStart || !text.Substring(caret, pair.CloseStart - caret).All(char.IsWhiteSpace) ? null : Native(() => NazorCSharpEditing.After(text, region, pair.Open.GetPosition(buffer.CurrentSnapshot),
                        pair.CloseEnd.GetPosition(buffer.CurrentSnapshot), caret, pair.Opening[0], true, options, pair.Service));
                    if (formatted != null) { Apply(view, buffer, formatted); return; }
                    formatted = Native(() => NazorCSharpEditing.Return(text, region, caret, options));
                    if (formatted != null) { Apply(view, buffer, formatted); return; }
                    var indentation = Native(() => NazorCSharpEditing.Indent(text, region, caret, options));
                    if (indentation != null) { Apply(view, buffer, indentation); return; }
                }
                var csharp = region.Language == EditingLanguage.CSharp;
                var tab = csharp && int.TryParse(options["tab_width"], out var tabOption) ? tabOption : view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
                var unit = csharp && int.TryParse(options["indent_size"], out var indentOption) ? indentOption : view.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId);
                var spaces = csharp ? options["indent_style"] != "tab" : view.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
                var previous = buffer.CurrentSnapshot.GetLineFromPosition(caret - newline.Length);
                var width = CodeIndentation.Width(text, previous.Start.Position, tab);
                if (pair != null && text.Substring(pair.Open.GetPosition(buffer.CurrentSnapshot) + pair.Opening.Length,
                    pair.CloseStart - pair.Open.GetPosition(buffer.CurrentSnapshot) - pair.Opening.Length).All(char.IsWhiteSpace))
                {
                    var inner = CodeIndentation.Indent(width + unit, tab, spaces);
                    var outer = CodeIndentation.Indent(width, tab, spaces);
                    buffer.Replace(new Span(caret, pair.CloseStart - caret), inner + newline + outer);
                    Move(view, buffer, caret + inner.Length);
                }
                else
                {
                    var desired = CodeIndentation.Desired(text.Substring(0, caret), caret, unit) ?? width;
                    var prefix = CodeIndentation.Indent(desired, tab, spaces);
                    buffer.Insert(caret, prefix);
                    Move(view, buffer, caret + prefix.Length);
                }
            });
        }

        public void ExecuteCommand(TabKeyCommandArgs args, Action next, CommandExecutionContext context)
        {
            if (Ready(args.TextView, args.SubjectBuffer, out var caret) && State.For(args.TextView, args.SubjectBuffer, caret).Top is Pair pair &&
                caret <= pair.CloseStart && args.SubjectBuffer.CurrentSnapshot.GetText(caret, pair.CloseStart - caret).All(char.IsWhiteSpace))
                Edit(args.TextView, args.SubjectBuffer, "Tab past Nazor closer", () => Move(args.TextView, args.SubjectBuffer, pair.CloseEnd.GetPosition(args.SubjectBuffer.CurrentSnapshot)));
            else next();
        }

        public void ExecuteCommand(BackspaceKeyCommandArgs args, Action next, CommandExecutionContext context)
        {
            if (!Ready(args.TextView, args.SubjectBuffer, out var caret)) { next(); return; }
            var pair = State.For(args.TextView, args.SubjectBuffer, caret).Top;
            if (pair == null || caret != pair.Open.GetPosition(args.SubjectBuffer.CurrentSnapshot) + pair.Opening.Length || caret != pair.CloseStart) { next(); return; }
            Edit(args.TextView, args.SubjectBuffer, "Delete Nazor pair", () =>
            {
                var start = pair.Open.GetPosition(args.SubjectBuffer.CurrentSnapshot);
                args.SubjectBuffer.Delete(new Span(start, pair.Opening.Length + pair.Closing.Length));
                Move(args.TextView, args.SubjectBuffer, start);
            });
        }

        private IReadOnlyDictionary<string, string> Options(ITextView view, ITextBuffer buffer)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in NazorCSharpOptions.Current) result[item.Key] = item.Value;
            if (!result.ContainsKey("tab_width")) result["tab_width"] = view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId).ToString();
            if (!result.ContainsKey("indent_size")) result["indent_size"] = view.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId).ToString();
            if (!result.ContainsKey("indent_style")) result["indent_style"] = view.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId) ? "space" : "tab";
            result["end_of_line"] = view.Options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId) == "\r\n" ? "crlf" : "lf";
            result["nazor_responsive_completion"] = view.Options.GetOptionValue(DefaultOptions.ResponsiveCompletionOptionId) ? "true" : "false";
            if (documents != null && documents.TryGetTextDocument(buffer, out var document) && Path.IsPathRooted(document.FilePath))
            {
                result["nazor_file_path"] = document.FilePath;
                var project = FindProject(document.FilePath);
                var configs = project?.AnalyzerConfigDocuments.Select(d => d.FilePath).Where(p => p != null) ?? Enumerable.Empty<string>();
                for (var directory = Path.GetDirectoryName(document.FilePath); directory != null; directory = Path.GetDirectoryName(directory)) configs = configs.Append(Path.Combine(directory, ".editorconfig"));
                var parsed = configs.Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).Select(ReadConfig).Where(c => c != null).ToArray();
                foreach (var item in AnalyzerConfigSet.Create(parsed).GetOptionsForSourcePath(document.FilePath + ".cs").AnalyzerOptions) result[item.Key] = item.Value;
            }
            return result;
        }

        private Project FindProject(string path) => workspace?.CurrentSolution.Projects
            .Where(p => p.Language == LanguageNames.CSharp && p.FilePath != null && path.StartsWith(Path.GetDirectoryName(p.FilePath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.FilePath.Length).FirstOrDefault();

        private void Format(ITextView view, ITextBuffer buffer, int caret)
        {
            var text = buffer.CurrentSnapshot.GetText();
            var region = NazorEditingContext.At(text, Math.Max(0, caret - 1), Component(buffer));
            if (region.Language == EditingLanguage.CSharp && caret <= region.End)
                Apply(view, buffer, Native(() => NazorCSharpEditing.Format(text, region, caret, Options(view, buffer))));
        }

        private static AnalyzerConfig ReadConfig(string path)
        {
            try { return AnalyzerConfig.Parse(File.ReadAllText(path), path); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { return null; }
        }

        private void Edit(ITextView view, ITextBuffer buffer, string name, Action action)
        {
            using (var transaction = undoManagers.GetTextBufferUndoManager(buffer).TextBufferUndoHistory.CreateTransaction(name))
            {
                var editor = operations.GetEditorOperations(view);
                editor.AddBeforeTextBufferChangePrimitive();
                action();
                editor.AddAfterTextBufferChangePrimitive();
                transaction.Complete();
            }
        }

        private static EditingResult Native(Func<EditingResult> action)
        {
            try { return action(); }
            catch (Exception error) when (error is TargetInvocationException || error is OperationCanceledException || error is InvalidOperationException || error is ArgumentException || error is MissingMemberException || error is TypeLoadException || error is FileNotFoundException || error is FileLoadException)
            {
                if (error.GetBaseException() is OperationCanceledException) return null;
                ActivityLog.LogWarning("Nazor", "C# editing service unavailable: " + error.GetBaseException().Message);
                return null;
            }
        }

        private static void Apply(ITextView view, ITextBuffer buffer, EditingResult result)
        {
            if (result == null) return;
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in result.Changes ?? Array.Empty<Microsoft.CodeAnalysis.Text.TextChange>()) edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                edit.Apply();
            }
            Move(view, buffer, result.Caret, result.VirtualSpaces);
        }

        private static bool Ready(ITextView view, ITextBuffer buffer, out int caret)
        {
            var point = view.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Successor);
            caret = point?.Position ?? 0;
            return point.HasValue && view.Selection.IsEmpty && !buffer.IsReadOnly(caret);
        }

        private static int Materialize(ITextView view, ITextBuffer buffer, int caret, IReadOnlyDictionary<string, string> preferences = null)
        {
            var virtualSpaces = view.Caret.Position.VirtualBufferPosition.VirtualSpaces;
            if (virtualSpaces == 0) return caret;
            var line = buffer.CurrentSnapshot.GetLineFromPosition(caret);
            var tab = preferences != null && int.TryParse(preferences["tab_width"], out var value) ? value : view.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
            var spaces = preferences != null ? preferences["indent_style"] != "tab" : view.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
            var prefix = line.GetText().All(char.IsWhiteSpace) ? CodeIndentation.Indent(virtualSpaces,
                tab, spaces) : new string(' ', virtualSpaces);
            buffer.Insert(caret, prefix);
            return caret + prefix.Length;
        }

        private static void Move(ITextView view, ITextBuffer buffer, int caret, int virtualSpaces = 0)
        {
            var point = new SnapshotPoint(buffer.CurrentSnapshot, Math.Max(0, Math.Min(caret, buffer.CurrentSnapshot.Length)));
            var target = view.BufferGraph.MapUpToBuffer(point, PointTrackingMode.Positive, PositionAffinity.Successor, view.TextBuffer);
            if (target.HasValue) view.Caret.MoveTo(new VirtualSnapshotPoint(target.Value, virtualSpaces));
        }

        private static bool Component(ITextBuffer buffer) => buffer.ContentType.IsOfType(NazorContentTypes.Component);

        private static bool HasEndTag(string text, int start, string tag)
        {
            var open = 0;
            var close = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '<') continue;
                var context = NazorEditingContext.At(text, i + 1, true);
                if (context.Language != EditingLanguage.Markup || context.TagStart != i || context.Tag != tag || context.End >= text.Length || text[context.End] != '>') continue;
                if (i != start && text[context.End - 1] != '/')
                {
                    if (text[i + 1] == '/') close++; else open++;
                }
                i = context.End;
            }
            return close > open;
        }

        private static Pair EmptyElement(string text, int caret, ITextBuffer buffer)
        {
            if (caret == 0) return null;
            var opening = text.LastIndexOf('<', Math.Max(0, caret - 1));
            if (opening < 0) return null;
            var tag = NazorEditingContext.At(text, opening + 1, true);
            if (tag.Tag == null || tag.Language != EditingLanguage.Markup || text[opening + 1] == '/' || tag.End >= caret ||
                text[tag.End - 1] == '/' || !text.Substring(tag.End + 1, caret - tag.End - 1).All(char.IsWhiteSpace)) return null;
            var close = caret;
            while (close < text.Length && char.IsWhiteSpace(text[close])) close++;
            var closing = "</" + tag.Tag + ">";
            if (!NazorEditingContext.Starts(text, close, closing)) return null;
            return new Pair { Open = buffer.CurrentSnapshot.CreateTrackingPoint(opening, PointTrackingMode.Positive), Opening = text.Substring(opening, tag.End - opening + 1), Closing = closing,
                CloseEnd = buffer.CurrentSnapshot.CreateTrackingPoint(close + closing.Length, PointTrackingMode.Negative) };
        }

        private sealed class Pair
        {
            internal ITrackingPoint Open;
            internal ITrackingPoint CloseEnd;
            internal string Opening;
            internal string Closing;
            internal bool Directive;
            internal bool CSharp;
            internal string Service;
            internal int CloseStart => CloseEnd.GetPosition(CloseEnd.TextBuffer.CurrentSnapshot) - Closing.Length;
        }

        private sealed class State
        {
            private readonly List<Pair> pairs = new List<Pair>();
            internal Pair Top => pairs.LastOrDefault();
            internal void Add(ITextBuffer buffer, int open, string opening, int close, string closing, bool directive, bool csharp = false) => pairs.Add(new Pair
            {
                Open = buffer.CurrentSnapshot.CreateTrackingPoint(open, PointTrackingMode.Positive),
                CloseEnd = buffer.CurrentSnapshot.CreateTrackingPoint(close, PointTrackingMode.Negative), Opening = opening, Closing = closing, Directive = directive, CSharp = csharp,
            });
            internal static State For(ITextView view, ITextBuffer buffer, int caret)
            {
                var state = view.Properties.GetOrCreateSingletonProperty(() => new State());
                var snapshot = buffer.CurrentSnapshot;
                state.pairs.RemoveAll(p => p.Open.TextBuffer != buffer || p.Open.GetPosition(snapshot) >= caret || p.CloseEnd.GetPosition(snapshot) <= caret ||
                    p.Open.GetPosition(snapshot) + p.Opening.Length > snapshot.Length || p.CloseStart < 0 ||
                    snapshot.GetText(p.Open.GetPosition(snapshot), p.Opening.Length) != p.Opening || snapshot.GetText(p.CloseStart, p.Closing.Length) != p.Closing);
                return state;
            }
        }
    }
}
