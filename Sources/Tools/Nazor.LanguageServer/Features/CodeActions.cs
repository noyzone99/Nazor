using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nazor.LanguageServer.Features;

internal sealed record FileChange(string Path, string? OldText, string? NewText);

internal sealed record CodeActionResult(string Title, IReadOnlyList<FileChange> Changes);

internal static partial class CodeActions
{
    private const string MovedStart = "/* <nazor-moved> */";
    private const string MovedEnd = "/* </nazor-moved> */";
    private static readonly HashSet<string> DefaultUsings = ["System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks", "Nazor", "Nazor.Input", "Nazor.Controls"];

    public static List<CodeActionResult> For(string path, string text, int start, int end, string? generated, Func<string, string?> read, Compilation? compilation = null)
    {
        var actions = new List<CodeActionResult>();
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var name = Path.GetFileName(path);
        foreach (var block in Blocks(text))
        {
            var directiveEnd = block.Start + (text.AsSpan(block.Start).StartsWith("@functions") ? "@functions".Length : ("@" + block.Kind).Length);
            if (!(start >= block.Start && start < directiveEnd || start == block.Open || start == block.Close)) continue;
            if (block.Kind == "style")
                actions.Add(new($"Move the styles to {name}.nss", MoveStyles(path, text, block, read, newLine)));
            else if (generated is not null && ClassOf(generated) is { } type &&
                (read(path + ".cs") is not { } existing || string.IsNullOrWhiteSpace(existing) || TargetClass(existing, type) is not null))
                actions.Add(new($"Move the code to {name}.cs", MoveCode(path, text, block, type, read, newLine, compilation)));
        }
        if (read(path + ".cs") is { } codeBehind && generated is not null && ClassOf(generated) is { } componentType && TargetClass(codeBehind, componentType) is not null &&
            Transferred(codeBehind, code: true) is { } codeTransfer &&
            (codeTransfer.Body is not null ? new CodeBehind([], UsingsOf(codeBehind)) : Members(codeBehind)) is { } members)
        {
            var content = codeTransfer.Body is null ? members : members with { Members = Dedent([.. codeTransfer.Body.Replace("\r\n", "\n").Split('\n')]) };
            if (content.Members.Count > 0)
                actions.Add(new($"Move the code of {name}.cs into the component", BringCode(path, text, codeBehind, content, newLine, codeTransfer.Remaining, componentType.Usings)));
        }
        if (read(path + ".nss") is { } sheet)
        {
            if (Transferred(sheet) is { } transfer && (transfer.Body ?? sheet).Trim().Length > 0)
                actions.Add(new($"Move the styles of {name}.nss into the component", BringStyles(path, text, sheet, newLine, transfer)));
        }
        return actions;
    }

    internal sealed record Block(string Kind, int Start, int Open, int Close);

    internal static IEnumerable<Block> Blocks(string text)
    {
        foreach (Match match in BlockStart().Matches(text))
        {
            var open = match.Index + match.Length - 1;
            if (NazorLanguage.Classify(text, open + 1) is not { Kind: NazorContextKind.CSharp or NazorContextKind.Style } context || context.Start != open + 1) continue;
            var kind = match.Groups["name"].Value;
            var close = kind == "style" ? NazorLanguage.CloseNss(text, open) : NazorLanguage.CloseCSharp(text, open);
            if (close < text.Length) yield return new(kind == "functions" ? "code" : kind, match.Index, open, close);
        }
    }

    private sealed record ClassInfo(string? Namespace, string Name, string TypeParameters, IReadOnlyList<string> Usings);

    private static ClassInfo? ClassOf(string generated)
    {
        var root = CSharpSyntaxTree.ParseText(generated).GetCompilationUnitRoot();
        if (root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) is not { } type)
            return null;
        var usings = UsingsOf(generated).Select(u => u.Replace("global::", "", StringComparison.Ordinal)).Distinct().ToList();
        return new((type.Parent as BaseNamespaceDeclarationSyntax)?.Name.ToString(), type.Identifier.Text, type.TypeParameterList?.ToString() ?? "", usings);
    }

    private static string Without(string text, Block block)
    {
        var (from, to) = Span(text, block);
        var result = text[..from] + text[to..];
        var trimmed = result.TrimEnd('\r', '\n', ' ', '\t');
        return text.EndsWith('\n') ? trimmed + (text.EndsWith("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n") : trimmed;
    }

    internal static (int From, int To) Span(string text, Block block)
    {
        var from = block.Start;
        var to = block.Close + 1;
        while (to < text.Length && text[to] is ' ' or '\t') to++;
        if (to < text.Length && text[to] == '\r') to++;
        if (to < text.Length && text[to] == '\n') to++;
        while (from > 0 && text[from - 1] is ' ' or '\t') from--;
        var before = from;
        while (before > 0 && text[before - 1] is '\r' or '\n') before--;
        var lineBreaks = text[before..from].Count(c => c == '\n');
        if (lineBreaks >= 2) from = before + text[before..from].IndexOf('\n') + 1;
        return (from, to);
    }

    private static List<string> Body(string text, Block block)
    {
        var lines = text[(block.Open + 1)..block.Close].Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        return Dedent(lines);
    }

    private static List<string> Dedent(List<string> lines)
    {
        while (lines.Count > 0 && lines[0].Length == 0) lines.RemoveAt(0);
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        var indent = lines.Where(l => l.Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return [.. lines.Select(l => l.Length == 0 ? l : l[indent..])];
    }

    private static string Indented(IEnumerable<string> lines, string indent, string newLine) =>
        string.Concat(lines.Select(l => (l.Length == 0 ? "" : indent + l) + newLine));

    private static List<FileChange> MoveCode(string path, string text, Block block, ClassInfo type, Func<string, string?> read, string newLine, Compilation? compilation)
    {
        var members = Body(text, block);
        var target = path + ".cs";
        var existing = read(target);
        var changes = new List<FileChange> { new(path, text, Without(text, block)) };
        if (existing is not null && TargetClass(existing, type) is { } targetClass)
        {
            var closing = targetClass.CloseBraceToken.SpanStart;
            var separator = existing[..closing].TrimEnd().EndsWith('{') ? "" : newLine;
            var insert = separator + Indented([MovedStart, .. members, MovedEnd], "    ", newLine);
            var before = existing[..closing].TrimEnd(' ', '\t');
            if (!before.EndsWith('\n')) before += newLine;
            var imports = string.Concat(type.Usings.Except(UsingsOf(existing), StringComparer.Ordinal).Select(u => "using " + u + ";" + newLine));
            changes.Add(new(target, existing, RequiredUsings(imports + before + insert + existing[closing..], compilation, imports.Length)));
            return changes;
        }
        var builder = new StringBuilder();
        foreach (var name in type.Usings) builder.Append("using ").Append(name).Append(';').Append(newLine);
        if (type.Usings.Count > 0) builder.Append(newLine);
        if (type.Namespace is { } ns) builder.Append("namespace ").Append(ns).Append(';').Append(newLine).Append(newLine);
        builder.Append("public partial class ").Append(type.Name).Append(type.TypeParameters).Append(newLine).Append('{').Append(newLine);
        builder.Append(Indented(members, "    ", newLine)).Append('}').Append(newLine);
        changes.Add(new(target, existing, RequiredUsings(builder.ToString(), compilation)));
        return changes;
    }

    private static ClassDeclarationSyntax? TargetClass(string code, ClassInfo info)
    {
        var root = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();
        if (root.ContainsDiagnostics) return null;
        var matches = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c =>
            c.Identifier.ValueText == info.Name && c.TypeParameterList?.ToString() == (info.TypeParameters.Length == 0 ? null : info.TypeParameters) &&
            c.Modifiers.Any(SyntaxKind.PartialKeyword) && !c.CloseBraceToken.IsMissing &&
            (c.Parent as BaseNamespaceDeclarationSyntax)?.Name.ToString() == info.Namespace).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static List<FileChange> MoveStyles(string path, string text, Block block, Func<string, string?> read, string newLine)
    {
        var moved = Blocks(text).Where(b => b.Kind == "style" && b.Start <= block.Start).ToArray();
        var rules = string.Join(newLine, moved.Select(b => Indented(Body(text, b), "", newLine)));
        var target = path + ".nss";
        var remaining = text;
        foreach (var earlier in moved.Reverse()) remaining = Without(remaining, earlier);
        var changes = new List<FileChange> { new(path, text, remaining) };
        if (read(target) is { } existing)
            changes.Add(new(target, existing, existing + (existing.EndsWith('\n') ? newLine : newLine + newLine) + MovedStart + newLine + rules + MovedEnd + newLine));
        else changes.Add(new(target, null, rules));
        return changes;
    }

    private sealed record CodeBehind(IReadOnlyList<string> Members, IReadOnlyList<string> Usings);

    private static IReadOnlyList<string> UsingsOf(string code) => CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()
        .Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None))
        .Select(u => u.ReplaceTrivia(u.DescendantTrivia(), (_, _) => default).NormalizeWhitespace().ToString()["using ".Length..].TrimEnd(';')).ToArray();

    private static string RequiredUsings(string code, Compilation? compilation, int addedImportsLength = int.MaxValue)
    {
        compilation ??= CSharpCompilation.Create("Nazor.Move", references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var tree = CSharpSyntaxTree.ParseText(code, compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions);
        var unused = compilation.AddSyntaxTrees(tree).GetDiagnostics().Where(d => d.Id is "CS8019" or "CS8933" && d.Location.SourceTree == tree)
            .Select(d => d.Location.SourceSpan).ToArray();
        var root = tree.GetCompilationUnitRoot();
        foreach (var import in root.Usings.Where(u => u.SpanStart < addedImportsLength && unused.Any(s => s.IntersectsWith(u.Span))).Reverse())
            code = code.Remove(import.FullSpan.Start, import.FullSpan.Length);
        return code.TrimStart('\r', '\n');
    }

    private static CodeBehind? Members(string code)
    {
        var root = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();
        if (root.ContainsDiagnostics || root.Usings.Any(u => !u.GlobalKeyword.IsKind(SyntaxKind.None))) return null;
        var types = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToList();
        if (types.Count != 1 || types[0] is not ClassDeclarationSyntax { Modifiers: var modifiers } type || !modifiers.Any(SyntaxKind.PartialKeyword) ||
            type.AttributeLists.Count > 0 || type.BaseList is not null || root.Members.Count != 1 ||
            (root.Members[0] is BaseNamespaceDeclarationSyntax ns && ns.Members.Count != 1)) return null;
        var body = code[(type.OpenBraceToken.Span.End)..type.CloseBraceToken.SpanStart].Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        var usings = UsingsOf(code);
        return new CodeBehind(Dedent(body), usings);
    }

    private static List<FileChange> BringCode(string path, string text, string codeBehind, CodeBehind members, string newLine, string? remaining, IReadOnlyList<string> availableUsings)
    {
        var result = text;
        var existing = Blocks(text).LastOrDefault(b => b.Kind == "code");
        if (existing is not null)
        {
            var close = existing.Close;
            var before = result[..close].TrimEnd(' ', '\t');
            if (!before.EndsWith('\n')) before += newLine;
            var separator = result[(existing.Open + 1)..close].Trim().Length == 0 ? "" : newLine;
            result = before + separator + Indented(members.Members, "    ", newLine) + result[close..];
        }
        else
        {
            var body = result.TrimEnd();
            result = body + newLine + newLine + "@code {" + newLine + Indented(members.Members, "    ", newLine) + "}" + newLine;
        }
        var missing = members.Usings.Where(u => !DefaultUsings.Contains(u) && !availableUsings.Contains(u) && !Regex.IsMatch(text, $@"^\s*@using\s+{Regex.Escape(u)}\s*$", RegexOptions.Multiline)).ToList();
        if (missing.Count > 0)
        {
            var lines = string.Concat(missing.Select(u => "@using " + u + newLine));
            var last = UsingLine().Matches(result).LastOrDefault();
            var at = last is null ? 0 : result.IndexOf('\n', last.Index) + 1;
            result = result[..at] + lines + (last is null ? newLine : "") + result[at..];
        }
        if (remaining is not null && Members(remaining) is { Members.Count: 0 } &&
            !CSharpSyntaxTree.ParseText(remaining).GetRoot().DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
            remaining = null;
        return [new(path, text, result), new(path + ".cs", codeBehind, remaining)];
    }

    private static List<FileChange> BringStyles(string path, string text, string sheet, string newLine, (string? Body, string? Remaining) transfer)
    {
        var rules = Dedent([.. (transfer.Body ?? sheet).Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd())]);
        var block = "@style {" + newLine + Indented(rules, "    ", newLine) + "}" + newLine;
        var first = Blocks(text).FirstOrDefault(b => b.Kind == "style");
        string result;
        if (first is not null)
        {
            var at = first.Start;
            while (at > 0 && text[at - 1] is ' ' or '\t') at--;
            result = text[..at] + block + newLine + text[at..];
        }
        else result = text.TrimEnd() + newLine + newLine + block;
        return [new(path, text, result), new(path + ".nss", sheet, string.IsNullOrWhiteSpace(transfer.Remaining) ? null : transfer.Remaining)];
    }

    private static (string? Body, string? Remaining)? Transferred(string text, bool code = false)
    {
        if (!text.Contains(MovedStart, StringComparison.Ordinal) && !text.Contains(MovedEnd, StringComparison.Ordinal)) return (null, null);
        var matches = MovedRegion().Matches(text);
        if (matches.Count == 0 || matches.Count != text.Split(MovedStart).Length - 1 || matches.Count != text.Split(MovedEnd).Length - 1) return null;
        if (code)
        {
            var comments = CSharpSyntaxTree.ParseText(text).GetRoot().DescendantTrivia().Where(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia)).Select(t => t.SpanStart).ToHashSet();
            foreach (Match match in matches)
                if (!comments.Contains(text.IndexOf(MovedStart, match.Index, StringComparison.Ordinal)) ||
                    !comments.Contains(text.LastIndexOf(MovedEnd, match.Index + match.Length - 1, StringComparison.Ordinal))) return null;
        }
        var body = string.Join("\n", matches.Select(m => m.Groups["body"].Value));
        var rest = MovedRegion().Replace(text, "");
        return (body, rest);
    }

    [GeneratedRegex(@"^[\t ]*/\* <nazor-moved> \*/\r?\n(?<body>[\s\S]*?)^[\t ]*/\* </nazor-moved> \*/[\t ]*(?:\r?\n|$)", RegexOptions.Multiline)]
    private static partial Regex MovedRegion();

    [GeneratedRegex(@"@(?<name>code|functions|style)\s*\{")]
    private static partial Regex BlockStart();

    [GeneratedRegex(@"^\s*@using\s+[^\r\n]+$", RegexOptions.Multiline)]
    private static partial Regex UsingLine();
}
