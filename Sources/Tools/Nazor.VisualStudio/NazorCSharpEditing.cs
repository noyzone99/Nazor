using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Nazor.Authoring;

namespace Nazor.VisualStudio
{
    internal sealed class EditingResult
    {
        internal int Caret;
        internal int VirtualSpaces;
        internal IReadOnlyList<TextChange> Changes;
        internal string Service;
    }

    internal static class NazorCSharpEditing
    {
        private static HostServices host;
        private static Func<string, Project> findProject;
        private static readonly ConcurrentDictionary<string, byte> Initialized = new ConcurrentDictionary<string, byte>();
        internal static void Configure(HostServices services, Func<string, Project> project) { host = services; findProject = project; }
        private static readonly Lazy<AdhocWorkspace> Workspace = new Lazy<AdhocWorkspace>(() =>
            new AdhocWorkspace(host ?? MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(new[] {
                NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Features"), NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.CSharp.Workspaces") }).Distinct())));

        private static readonly Dictionary<char, string> Services = new Dictionary<char, string>
        {
            ['{'] = "CurlyBrace", ['('] = "ParenthesisBrace", ['['] = "BracketBrace", ['"'] = "StringLiteralBrace", ['\''] = "CharLiteralBrace", ['<'] = "LessAndGreaterThanBrace",
        };

        internal static EditingResult Complete(string text, NazorEditingContext region, int opening, char character, IReadOnlyDictionary<string, string> preferences)
        {
            if (!Services.TryGetValue(character, out var name)) return null;
            return Run(text, region, opening, opening + 1, opening + 1, name, "complete", preferences);
        }

        internal static EditingResult After(string text, NazorEditingContext region, int opening, int closing, int caret, char character, bool returned, IReadOnlyDictionary<string, string> preferences, string service = null)
        {
            if (!Services.TryGetValue(character, out var name)) return null;
            return Run(text, region, opening, closing, caret, service ?? name, returned ? "return" : "after", preferences);
        }

        internal static EditingResult Indent(string text, NazorEditingContext region, int caret, IReadOnlyDictionary<string, string> preferences) =>
            Run(text, region, caret, caret, caret, "", "indent", preferences);

        internal static EditingResult Overtype(string text, NazorEditingContext region, int opening, int closing, int caret, char character, IReadOnlyDictionary<string, string> preferences, string service) =>
            Services.TryGetValue(character, out var name) ? Run(text, region, opening, closing, caret, service ?? name, "overtype", preferences) : null;

        internal static EditingResult Return(string text, NazorEditingContext region, int caret, IReadOnlyDictionary<string, string> preferences) =>
            Run(text, region, caret, caret, caret, "CurlyBrace", "existingReturn", preferences);

        internal static EditingResult Format(string text, NazorEditingContext region, int caret, IReadOnlyDictionary<string, string> preferences) =>
            Run(text, region, caret, caret, caret, "", "format", preferences);

        private static EditingResult Run(string text, NazorEditingContext region, int opening, int closing, int caret, string name, string operation, IReadOnlyDictionary<string, string> preferences)
        {
            var workspaces = typeof(AdhocWorkspace).Assembly;
            var features = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.Features");
            var csharp = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.CSharp.Features");
            var csharpWorkspaces = NazorRoslynAssemblies.Load("Microsoft.CodeAnalysis.CSharp.Workspaces");
            var newline = preferences.TryGetValue("end_of_line", out var eol) && eol == "crlf" ? "\r\n" : "\n";
            var tabSize = preferences.TryGetValue("tab_width", out var tabText) && int.TryParse(tabText, out var tabValue) ? tabValue : 4;
            var indentSize = preferences.TryGetValue("indent_size", out var indentText) && int.TryParse(indentText, out var indentValue) ? indentValue : tabSize;
            var anchorLine = Math.Min(region.Anchor, text.Length);
            while (anchorLine > 0 && text[anchorLine - 1] != '\n') anchorLine--;
            var depth = CodeIndentation.Width(text, anchorLine, tabSize) / Math.Max(indentSize, 1);
            var members = region.Prefix.StartsWith("class ", StringComparison.Ordinal);
            var prefix = "";
            var suffix = "";
            var typesNewline = !preferences.TryGetValue("csharp_new_line_before_open_brace", out var braces) || braces.Split(',').Any(b => b.Trim() == "all" || b.Trim() == "types");
            for (var level = 0; level < depth; level++)
            {
                var indent = new string(' ', level * indentSize);
                prefix += indent + (members ? "class __NazorScope" + level : "void __NazorScope" + level + "()") +
                    (typesNewline ? newline + indent : " ") + "{" + newline;
                suffix = newline + indent + "}" + suffix;
            }
            prefix += new string(' ', depth * indentSize) + (members ? "class __NazorEditing" + (typesNewline ? newline + new string(' ', depth * indentSize) : " ") + "{" : region.Prefix);
            suffix = region.Suffix + suffix;
            var source = prefix + text.Substring(region.Start, region.End - region.Start) + suffix;
            var delta = prefix.Length - region.Start;
            var project = preferences.TryGetValue("nazor_file_path", out var file) ? findProject?.Invoke(file) : null;
            project = project ?? Workspace.Value.CurrentSolution.AddProject(ProjectId.CreateNewId(), "Nazor editing", "NazorEditing", LanguageNames.CSharp).Projects.Last();
            var document = project.AddDocument("Editing.cs", SourceText.From(source));
            var cold = Initialized.TryAdd(operation + ":" + name, 0);
            using (var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(!cold && preferences.TryGetValue("nazor_responsive_completion", out var responsive) && responsive == "true" ? 200 : 2000)))
            {
                var token = timeout.Token;
                var parsed = workspaces.GetType("Microsoft.CodeAnalysis.ParsedDocument", true).GetMethod("CreateSynchronously").Invoke(null, new object[] { document, token });
                var structured = workspaces.GetType("Microsoft.CodeAnalysis.Diagnostics.StructuredAnalyzerConfigOptions", true)
                    .GetMethod("Create").Invoke(null, new object[] { new Options(preferences), null });
                var formatting = Activator.CreateInstance(csharpWorkspaces.GetType("Microsoft.CodeAnalysis.CSharp.Formatting.CSharpSyntaxFormattingOptions", true), new[] { structured });
                var indentation = Activator.CreateInstance(workspaces.GetType("Microsoft.CodeAnalysis.Indentation.IndentationOptions", true), new[] { formatting });
                var autoType = workspaces.GetType("Microsoft.CodeAnalysis.Formatting.AutoFormattingOptions", true);
                var automatic = autoType.GetField("Default").GetValue(null);
                foreach (var namePart in new[] { "Return", "Typing", "Semicolon", "CloseBrace" })
                    if (preferences.TryGetValue("csharp_format_on_" + (namePart == "CloseBrace" ? "close_brace" : namePart.ToLowerInvariant()), out var enabled))
                        autoType.GetProperty("FormatOn" + namePart).SetValue(automatic, enabled == "true");
                indentation.GetType().GetProperty("AutoFormattingOptions").SetValue(indentation, automatic);
                if (operation == "format")
                {
                    var character = text[caret - 1];
                    if (character == ';' && (!(bool)autoType.GetProperty("FormatOnTyping").GetValue(automatic) || !(bool)autoType.GetProperty("FormatOnSemicolon").GetValue(automatic))) return null;
                    var serviceType = csharpWorkspaces.GetType("Microsoft.CodeAnalysis.CSharp.Formatting.CSharpSyntaxFormattingService", true);
                    var formatter = Activator.CreateInstance(serviceType, parsed.GetType().GetProperty("LanguageServices").GetValue(parsed));
                    if (!(bool)serviceType.GetMethod("ShouldFormatOnTypedCharacter").Invoke(formatter, new object[] { parsed, character, caret + delta, token })) return null;
                    var nativeChanges = ((IEnumerable<TextChange>)serviceType.GetMethod("GetFormattingChangesOnTypedCharacter").Invoke(formatter, new[] { parsed, caret + delta, indentation, token })).ToArray();
                    if (nativeChanges.Any(c => c.Span.Start < prefix.Length || c.Span.End > prefix.Length + region.End - region.Start)) return null;
                    var mapped = nativeChanges.Select(c => new TextChange(new TextSpan(c.Span.Start - delta, c.Span.Length), c.NewText)).ToArray();
                    return new EditingResult { Changes = mapped,
                        Caret = caret + mapped.Where(c => c.Span.End <= caret).Sum(c => c.NewText.Length - c.Span.Length) };
                }
                if (operation == "indent")
                {
                    var sourceText = SourceText.From(source);
                    var indentService = Activator.CreateInstance(csharpWorkspaces.GetType("Microsoft.CodeAnalysis.CSharp.Indentation.CSharpIndentationService", true), true);
                    var indent = indentService.GetType().GetMethod("GetIndentation").Invoke(indentService, new[] { parsed, sourceText.Lines.GetLineFromPosition(caret + delta).LineNumber, indentation, token });
                    var basis = (int)indent.GetType().GetProperty("BasePosition").GetValue(indent);
                    var offset = (int)indent.GetType().GetProperty("Offset").GetValue(indent);
                    var tab = preferences.TryGetValue("tab_width", out var value) && int.TryParse(value, out var width) ? width : 4;
                    var column = 0;
                    for (var i = sourceText.Lines.GetLineFromPosition(basis).Start; i < basis; i++) column = source[i] == '\t' ? (column / tab + 1) * tab : column + 1;
                    var desired = Math.Max(0, column + offset);
                    var authored = SourceText.From(text).Lines.GetLineFromPosition(caret);
                    if (authored.Span.Length == 0) return new EditingResult { Caret = caret, VirtualSpaces = desired };
                    var end = caret;
                    while (end < authored.End && char.IsWhiteSpace(text[end])) end++;
                    var prefixText = CodeIndentation.Indent(desired, tabSize, !preferences.TryGetValue("indent_style", out var style) || style != "tab");
                    var change = new TextChange(TextSpan.FromBounds(caret, end), prefixText);
                    return new EditingResult { Caret = caret + prefixText.Length, Changes = new[] { change } };
                }
                var context = Activator.CreateInstance(features.GetType("Microsoft.CodeAnalysis.BraceCompletion.BraceCompletionContext", true),
                    new[] { parsed, structured, opening + delta, closing + delta, caret + delta });
                if (operation == "existingReturn")
                {
                    var existingService = Activator.CreateInstance(csharp.GetType("Microsoft.CodeAnalysis.CSharp.BraceCompletion.CurlyBraceCompletionService", true), true);
                    context = existingService.GetType().GetMethod("GetCompletedBraceContext").Invoke(existingService, new[] { parsed, structured, caret + delta });
                    if (context == null) return null;
                    operation = "return";
                }
                var candidates = operation == "complete" && name == "CurlyBrace" ? new[] { "InterpolationBrace", name } : operation == "complete" && name == "StringLiteralBrace" ? new[] { "InterpolatedStringBrace", name } : new[] { name };
                object service = null;
                foreach (var candidate in candidates)
                {
                    var instance = Activator.CreateInstance(csharp.GetType("Microsoft.CodeAnalysis.CSharp.BraceCompletion." + candidate + "CompletionService", true), true);
                    if (operation != "complete") { service = instance; break; }
                    var pending = instance.GetType().GetMethod("HasBraceCompletionAsync").Invoke(instance, new object[] { context, document, token });
                    var task = pending.GetType().GetMethod("AsTask").Invoke(pending, null);
                    if ((bool)task.GetType().GetProperty("Result").GetValue(task)) { service = instance; name = candidate; break; }
                }
                if (service == null) return null;
                if (operation == "overtype")
                    return (bool)service.GetType().GetMethod("AllowOverType").Invoke(service, new[] { context, token })
                        ? new EditingResult { Caret = caret + 1 } : null;
                object result;
                if (operation == "complete")
                {
                    result = service.GetType().GetMethod("GetBraceCompletion").Invoke(service, new[] { context });
                }
                else
                {
                    result = service.GetType().GetMethod(operation == "return" ? "GetTextChangeAfterReturn" : "GetTextChangesAfterCompletion")
                        .Invoke(service, new[] { context, indentation, token });
                    if (result == null) return null;
                }
                var changes = (IEnumerable<TextChange>)result.GetType().GetProperty("TextChanges").GetValue(result);
                var edits = changes.ToArray();
                if (edits.Any(c => c.Span.Start < prefix.Length || c.Span.End > prefix.Length + region.End - region.Start)) return null;
                var updated = SourceText.From(source).WithChanges(edits);
                var location = (LinePosition)result.GetType().GetProperty("CaretLocation").GetValue(result);
                var line = updated.Lines[location.Line];
                return new EditingResult
                {
                    Caret = line.Start + Math.Min(location.Character, line.Span.Length) - delta,
                    VirtualSpaces = Math.Max(0, location.Character - line.Span.Length),
                    Changes = edits.Select(c => new TextChange(new TextSpan(c.Span.Start - delta, c.Span.Length), c.NewText)).ToArray(),
                    Service = name,
                };
            }
        }

        private sealed class Options : AnalyzerConfigOptions
        {
            private readonly IReadOnlyDictionary<string, string> values;
            internal Options(IReadOnlyDictionary<string, string> values) => this.values = values;
            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value);
        }
    }
}
