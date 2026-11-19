using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nazor.LanguageServer.Features;

internal static class EditorSnippets
{
    public static SnippetEdit? Directive(string text, int offset)
    {
        var context = NazorLanguage.Classify(text, offset);
        if (context.Kind != NazorContextKind.Directive || context.WordEnd != offset) return null;
        var body = text[context.WordStart..offset] switch
        {
            "code" => "code {\n    $0\n}",
            "style" => "style {\n    $0\n}",
            "window" => "window\n\n<TextBlock>@Window.Title</TextBlock>\n\n$0\n\n@code {\n    protected override void OnInitialized()\n    {\n        Window.Title = \"${1:Window title}\";\n    }\n}",
            "if" => "if (${1:condition})\n{\n    $0\n}",
            "foreach" => "foreach (var ${1:item} in ${2:items})\n{\n    $0\n}",
            "for" => "for (var ${1:i} = 0; ${1:i} < ${2:count}; ${1:i}++)\n{\n    $0\n}",
            "while" => "while (${1:condition})\n{\n    $0\n}",
            "switch" => "switch (${1:value})\n{\n    default:\n        $0\n        break;\n}",
            _ => null,
        };
        if (body is null) return null;
        var lineStart = text.LastIndexOf('\n', Math.Max(0, context.WordStart - 1)) + 1;
        var indentation = text[lineStart..(context.WordStart - 1)];
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return new(context.WordStart, offset, body.Replace("\n", newline + indentation, StringComparison.Ordinal));
    }

    public static IReadOnlyList<(string Title, SnippetEdit Edit)> Surround(string text, int start, int end, bool csharp)
    {
        if (start < 0 || end > text.Length || end <= start || string.IsNullOrWhiteSpace(text[start..end])) return [];
        var selected = text[start..end];
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var line = text.LastIndexOf('\n', Math.Max(0, start - 1)) + 1;
        var indent = new string(text[line..start].TakeWhile(c => c is ' ' or '\t').ToArray());
        var escaped = selected.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("}", "\\}", StringComparison.Ordinal);
        if (csharp)
        {
            var block = CodeActions.Blocks(text).FirstOrDefault(b => b.Kind == "code" && start > b.Open && end <= b.Close);
            if (block is null) return [];
            const string header = "class C {";
            var source = CSharpSyntaxTree.ParseText(header + text[(block.Open + 1)..block.Close] + "}").GetRoot();
            var from = header.Length + start - block.Open - 1 + selected.Length - selected.TrimStart().Length;
            var to = header.Length + end - block.Open - 1 - (selected.Length - selected.TrimEnd().Length);
            var statements = source.DescendantNodes().OfType<StatementSyntax>().Where(s => s.SpanStart >= from && s.Span.End <= to).ToArray();
            if (!statements.Any(s => s.SpanStart == from) || !statements.Any(s => s.Span.End == to)) return [];
            var root = CSharpSyntaxTree.ParseText("class C { void M() {\n" + selected + "\n} }").GetRoot();
            if (root.ContainsDiagnostics || root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Body?.Statements.Count is not > 0) return [];
            return [
                ("if", new(start, end, "if (${1:condition})" + newline + indent + "{" + newline + escaped + newline + indent + "}$0")),
                ("try / finally", new(start, end, "try" + newline + indent + "{" + newline + escaped + newline + indent + "}" + newline + indent + "finally" + newline + indent + "{" + newline + indent + "    $0" + newline + indent + "}")),
            ];
        }
        var tags = NazorLanguage.Tags(text);
        var markupStart = start + selected.Length - selected.TrimStart().Length;
        var markupEnd = end - (selected.Length - selected.TrimEnd().Length);
        var first = tags.FirstOrDefault(t => t.Start == markupStart && !t.IsEnd);
        if (first is null || tags.Where(t => t.Start >= markupStart && t.End <= markupEnd && !t.IsEnd && !t.SelfClosing)
            .Any(t => NazorLanguage.EndOf(tags, t) is not { } close || close.End > markupEnd) ||
            tags.Where(t => t.IsEnd && t.Start >= markupStart && t.End <= markupEnd).Any(t => NazorLanguage.StartOf(tags, t) is not { } open || open.Start < markupStart) ||
            !tags.Any(t => t.End == markupEnd)) return [];
        return [
            ("Container", new(start, end, "<${1:Container}>" + newline + escaped + newline + indent + "</${1:Container}>$0")),
            ("@if", new(start, end, "@if (${1:condition})" + newline + indent + "{" + newline + escaped + newline + indent + "}$0")),
        ];
    }
}
