using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nazor.LanguageServer.Workspace;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.LanguageServer.Features;

internal static partial class ExtractComponent
{
    public const string BaseName = "ExtractedComponent";

    public static async Task<CodeActionResult?> ActionAsync(ProjectSnapshot snapshot, ComponentDocument component, string text, int start, int end,
        Func<string, string?> read, CancellationToken cancellation)
    {
        if (Selection(text, start, end) is not var (from, to)) return null;
        if (snapshot.Document(component) is not { } document || await document.GetSemanticModelAsync(cancellation) is not { } model) return null;
        var root = await model.SyntaxTree.GetRootAsync(cancellation);
        var type = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c => model.GetDeclaredSymbol(c, cancellation)).FirstOrDefault(s => s is not null);
        if (type is null) return null;

        var spans = component.Generated.Mappings.Where(m => m.OriginalStart >= from && m.OriginalEnd <= to)
            .Select(m => new TextSpan(m.GeneratedStart, m.Length)).ToList();
        bool Inside(Location location) => location.IsInSource && location.SourceTree == model.SyntaxTree && spans.Any(s => s.Contains(location.SourceSpan));

        var parameters = new List<(string Name, string Declaration, string Value)>();
        foreach (var span in spans)
        {
            foreach (var name in root.DescendantNodes(span).OfType<IdentifierNameSyntax>().Where(n => span.Contains(n.Span)))
            {
                var symbol = model.GetSymbolInfo(name, cancellation) is { Symbol: { } found } info ? found : model.GetSymbolInfo(name, cancellation).CandidateSymbols.FirstOrDefault();
                if (symbol is null || parameters.Any(p => p.Name == symbol.Name)) continue;
                var position = name.SpanStart;
                switch (symbol)
                {
                    case IFieldSymbol { IsStatic: false } field when SymbolEqualityComparer.Default.Equals(field.ContainingType, type):
                        parameters.Add(Data(field.Name, field.Type.ToMinimalDisplayString(model, position)));
                        break;
                    case IPropertySymbol { IsStatic: false } property when SymbolEqualityComparer.Default.Equals(property.ContainingType, type):
                        parameters.Add(Data(property.Name, property.Type.ToMinimalDisplayString(model, position)));
                        break;
                    case ILocalSymbol local when !local.Locations.Any(Inside):
                        parameters.Add(Data(local.Name, Flow(model, name, local.Type, cancellation).ToMinimalDisplayString(model, position)));
                        break;
                    case IParameterSymbol parameter when !parameter.Locations.Any(Inside) && parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction }:
                        parameters.Add(Data(parameter.Name, Flow(model, name, parameter.Type, cancellation).ToMinimalDisplayString(model, position)));
                        break;
                    case IMethodSymbol { IsStatic: false, MethodKind: MethodKind.Ordinary } method when SymbolEqualityComparer.Default.Equals(method.ContainingType, type):
                        parameters.Add(Callback(method, Invoked(name), model, position));
                        break;
                }
            }
        }

        var componentName = BaseName;
        var directory = Path.GetDirectoryName(component.Path)!;
        for (var n = 2; read(Path.Combine(directory, componentName + ".nazor")) is not null; n++) componentName = BaseName + n;
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var indent = text.Contains("\n\t", StringComparison.Ordinal) ? "\t" : "    ";

        var use = new StringBuilder("<").Append(componentName);
        foreach (var parameter in parameters) use.Append(' ').Append(parameter.Name).Append("=\"").Append(parameter.Value).Append('"');
        use.Append(" />");
        var styles = Styles(component.Path, text, from, to, read);
        var edits = new List<(int Start, int End, string Text)> { (from, to, use.ToString()) };
        edits.AddRange(styles.Removals.Select(r => (r.Start, r.End, "")));

        var markup = Dedent(text[from..to], from - LineStart(text, from), newLine);
        var builder = new StringBuilder();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n').Where(l => l.TrimStart().StartsWith("@using ", StringComparison.Ordinal)))
            builder.Append(line.Trim()).Append(newLine);
        if (builder.Length > 0) builder.Append(newLine);
        builder.Append(markup).Append(newLine);
        var blocks = CodeActions.Blocks(text).ToList();
        var codeFirst = blocks.FirstOrDefault()?.Kind == "code";
        if (codeFirst) AppendCode(builder, parameters, indent, newLine);
        if (styles.Copied.Count > 0)
        {
            builder.Append(newLine).Append("@style {").Append(newLine);
            for (var i = 0; i < styles.Copied.Count; i++)
            {
                if (i > 0) builder.Append(newLine);
                foreach (var line in styles.Copied[i].Split('\n'))
                    builder.Append(line.Length == 0 ? "" : indent + line).Append(newLine);
            }
            builder.Append('}').Append(newLine);
        }
        if (!codeFirst) AppendCode(builder, parameters, indent, newLine);

        var path = Path.Combine(directory, componentName + ".nazor");
        var changes = new List<FileChange> { new(component.Path, text, Apply(text, edits)), new(path, null, builder.ToString()) };
        if (styles.Sheet is { } sheet) changes.Add(sheet);
        return new($"Extract component {componentName}", changes);
    }

    private static void AppendCode(StringBuilder builder, List<(string Name, string Declaration, string Value)> parameters, string indent, string newLine)
    {
        if (parameters.Count == 0) return;
        builder.Append(newLine).Append("@code {").Append(newLine);
        foreach (var parameter in parameters) builder.Append(indent).Append(parameter.Declaration).Append(newLine);
        builder.Append('}').Append(newLine);
    }

    private static ITypeSymbol Flow(SemanticModel model, IdentifierNameSyntax name, ITypeSymbol declared, CancellationToken cancellation) =>
        declared.IsReferenceType && model.GetTypeInfo(name, cancellation).Nullability.FlowState == NullableFlowState.NotNull
            ? declared.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            : declared;

    private static (string, string, string) Data(string name, string type) =>
        (name, $"[Parameter] public {type} {name} {{ get; set; }} = default!;", "@" + name);

    private static (string, string, string) Callback(IMethodSymbol method, bool invoked, SemanticModel model, int position)
    {
        string Display(ITypeSymbol t) => t.ToMinimalDisplayString(model, position);
        var returnsTask = method.ReturnType.ToDisplayString() == "System.Threading.Tasks.Task";
        if (!invoked && method.Parameters.Length <= 1 && (method.ReturnsVoid || returnsTask))
        {
            var callback = method.Parameters.Length == 0 ? "EventCallback" : $"EventCallback<{Display(method.Parameters[0].Type)}>";
            return (method.Name, $"[Parameter] public {callback} {method.Name} {{ get; set; }}", method.Name);
        }
        var arguments = string.Join(", ", method.Parameters.Select(p => Display(p.Type)));
        var delegateType = method.ReturnsVoid
            ? method.Parameters.Length == 0 ? "Action" : $"Action<{arguments}>"
            : $"Func<{(arguments.Length == 0 ? "" : arguments + ", ")}{Display(method.ReturnType)}>";
        return (method.Name, $"[Parameter] public {delegateType} {method.Name} {{ get; set; }} = default!;", method.Name);
    }

    private static bool Invoked(IdentifierNameSyntax name) =>
        name.Parent is InvocationExpressionSyntax invocation && invocation.Expression == name ||
        name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } access && access.Name == name;

    private static (int From, int To)? Selection(string text, int start, int end)
    {
        while (start < end && char.IsWhiteSpace(text[start])) start++;
        while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
        if (end <= start || text[start] != '<' || text[end - 1] != '>') return null;
        if (NazorLanguage.Classify(text, start + 1).Kind is NazorContextKind.CSharp or NazorContextKind.Style or NazorContextKind.Comment) return null;
        var depth = 0;
        var any = false;
        foreach (var tag in NazorLanguage.Tags(text))
        {
            if (tag.End <= start) continue;
            if (tag.Start >= end) break;
            if (tag.Start < start || tag.End > end) return null;
            any = true;
            if (tag.SelfClosing) continue;
            depth += tag.IsEnd ? -1 : 1;
            if (depth < 0) return null;
        }
        return any && depth == 0 ? (start, end) : null;
    }

    private static int LineStart(string text, int index) => index == 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;

    private static string Dedent(string markup, int column, string newLine)
    {
        var lines = markup.Replace("\r\n", "\n").Split('\n');
        return string.Join(newLine, lines.Select((line, i) =>
            i == 0 ? line.TrimEnd() : line[Math.Min(column, line.Length - line.TrimStart().Length)..].TrimEnd()));
    }

    private static string Apply(string text, IEnumerable<(int Start, int End, string Text)> edits)
    {
        var result = text;
        foreach (var edit in edits.OrderByDescending(e => e.Start)) result = result[..edit.Start] + edit.Text + result[edit.End..];
        if (!text.EndsWith('\n')) return result;
        return result.TrimEnd('\r', '\n', ' ', '\t') + (text.EndsWith("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n");
    }


    private sealed record Element(string Name, string? Id, bool AnyId, IReadOnlySet<string> Classes, bool AnyClass);

    private sealed record Rule(int Start, int End, string Text, IReadOnlyList<Selector> Selectors, string? Keyframes);

    private sealed record StyleChanges(List<string> Copied, List<(int Start, int End)> Removals, FileChange? Sheet);

    private static StyleChanges Styles(string path, string text, int from, int to, Func<string, string?> read)
    {
        var tags = NazorLanguage.Tags(text).Where(t => !t.IsEnd).ToList();
        var extracted = tags.Where(t => t.Start >= from && t.End <= to).Select(t => ElementOf(text, t)).ToList();
        var remaining = tags.Where(t => t.End <= from || t.Start >= to).Select(t => ElementOf(text, t)).ToList();

        var sheetPath = path + ".nss";
        var sheet = read(sheetPath);
        var sheetRules = sheet is null ? [] : Rules(sheet, 0, sheet.Length);
        var blocks = CodeActions.Blocks(text).Where(b => b.Kind == "style").Select(b => (Block: b, Rules: Rules(text, b.Open + 1, b.Close))).ToList();
        var all = sheetRules.Concat(blocks.SelectMany(b => b.Rules)).ToList();

        var copied = all.Where(r => r.Selectors.Any(s => extracted.Any(e => CanSelect(s.Subject, e)))).ToHashSet();
        var removed = copied.Where(r => !r.Selectors.Any(s => remaining.Any(e => CanSelect(s.Subject, e)))).ToHashSet();
        foreach (var keyframes in all.Where(r => r.Keyframes is not null))
        {
            if (!copied.Any(r => Uses(r, keyframes.Keyframes!))) continue;
            copied.Add(keyframes);
            if (!all.Any(r => r.Keyframes is null && !removed.Contains(r) && Uses(r, keyframes.Keyframes!))) removed.Add(keyframes);
        }

        var removals = new List<(int, int)>();
        foreach (var (block, rules) in blocks)
        {
            var gone = Leaving(text, rules.Where(removed.Contains));
            if (gone.Count == 0) continue;
            var empty = Enumerable.Range(block.Open + 1, block.Close - block.Open - 1)
                .All(i => char.IsWhiteSpace(text[i]) || gone.Any(g => i >= g.Start && i < g.End));
            if (empty) removals.Add(CodeActions.Span(text, block));
            else removals.AddRange(gone);
        }
        FileChange? sheetChange = null;
        if (sheet is not null && Leaving(sheet, sheetRules.Where(removed.Contains)) is { Count: > 0 } leaving)
        {
            var rest = Apply(sheet, leaving.Select(g => (g.Start, g.End, "")));
            sheetChange = new(sheetPath, sheet, string.IsNullOrWhiteSpace(rest) ? null : rest);
        }
        return new([.. all.Where(copied.Contains).Select(r => r.Text)], removals, sheetChange);
    }

    private static bool Uses(Rule rule, string animation) =>
        Regex.IsMatch(rule.Text, $@"(?<![\w-]){Regex.Escape(animation)}(?![\w-])");

    private static bool CanSelect(CompoundSelector subject, Element element) =>
        (subject.Type is null || string.Equals(subject.Type, element.Name, StringComparison.Ordinal)) &&
        (subject.Id is null || element.AnyId || string.Equals(subject.Id, element.Id, StringComparison.Ordinal)) &&
        (element.AnyClass || subject.Classes.All(element.Classes.Contains));

    private static Element ElementOf(string text, MarkupTag tag)
    {
        string? id = null;
        var anyId = false;
        var anyClass = false;
        var classes = new HashSet<string>(StringComparer.Ordinal);
        var source = text[tag.Start..tag.End];
        foreach (Match match in ClassOrId().Matches(source))
        {
            var start = match.Index + match.Length;
            var written = start < source.Length && source[start] == '@';
            var end = source.IndexOf(match.Groups["quote"].Value[0], start);
            var value = end < 0 ? "" : source[start..end];
            if (match.Groups["name"].Value.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                if (written) anyId = true;
                else id = value.Trim();
            }
            else if (written) anyClass = true;
            else classes.UnionWith(value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        }
        return new(tag.Name, id, anyId, classes, anyClass);
    }

    private static List<Rule> Rules(string text, int from, int to) =>
    [
        .. NssBlocks.At(text, from, to).Select(block =>
        {
            var rule = text[block.Start..block.End];
            var selectors = NssCompiler.Compile("extract", rule).StyleSheet.Rules.Select(r => r.Selector).ToList();
            var keyframes = Keyframes().Match(rule) is { Success: true } match ? match.Groups["name"].Value : null;
            return new Rule(block.Start, block.End, Dedent(rule, block.Start - LineStart(text, block.Start), "\n"), selectors, keyframes);
        }),
    ];

    private static List<(int Start, int End)> Leaving(string text, IEnumerable<Rule> rules)
    {
        var spans = new List<(int Start, int End)>();
        foreach (var rule in rules.OrderBy(r => r.Start))
        {
            var (start, end) = WholeLines(text, rule.Start, rule.End);
            if (spans.Count > 0 && spans[^1].End <= start && text[spans[^1].End..start].Trim().Length == 0) spans[^1] = (spans[^1].Start, end);
            else spans.Add((start, end));
        }
        return [.. spans.Select(s => Tidy(text, s.Start, s.End))];
    }

    private static (int Start, int End) WholeLines(string text, int start, int end)
    {
        var from = start;
        while (from > 0 && text[from - 1] is ' ' or '\t') from--;
        var to = LineEnd(text, end);
        return OnLines(text, from, to) ? (from, to) : (start, end);
    }

    private static bool OnLines(string text, int from, int to) =>
        (from == 0 || text[from - 1] == '\n') && (to >= text.Length || text[to - 1] == '\n');

    private static (int Start, int End) Tidy(string text, int from, int to)
    {
        if (!OnLines(text, from, to)) return (from, to);
        var previousStart = from == 0 ? 0 : LineStart(text, from - 1);
        var previous = from == 0 ? null : text[previousStart..from].Trim();
        var nextEnd = to >= text.Length ? to : text.IndexOf('\n', to) is var n and >= 0 ? n + 1 : text.Length;
        var next = to >= text.Length ? null : text[to..nextEnd].Trim();
        if (next == "" && (previous is null or "" || previous.EndsWith('{'))) return (from, nextEnd);
        if (previous == "" && (next is null || next.StartsWith('}'))) return (previousStart, to);
        return (from, to);
    }

    private static int LineEnd(string text, int index)
    {
        while (index < text.Length && text[index] is ' ' or '\t') index++;
        if (index < text.Length && text[index] == '\r') index++;
        if (index < text.Length && text[index] == '\n') index++;
        return index;
    }

    [GeneratedRegex(@"(?<![\w@:.-])(?<name>Class|Id)\s*=\s*(?<quote>[""'])", RegexOptions.IgnoreCase)]
    private static partial Regex ClassOrId();

    [GeneratedRegex(@"^@keyframes\s+(?<name>[\w-]+)")]
    private static partial Regex Keyframes();
}
