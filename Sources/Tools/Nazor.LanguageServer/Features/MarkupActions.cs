using System.Text.RegularExpressions;
using Nazor.Compiler;

namespace Nazor.LanguageServer.Features;

internal static partial class MarkupActions
{
    public static List<CodeActionResult> For(string path, string text, int start, int end, NazorCatalog catalog, Func<string, string?> read, Func<int, bool>? unresolved = null)
    {
        var actions = new List<CodeActionResult>();
        var tags = NazorLanguage.Tags(text);
        foreach (var tag in tags.Where(t => !t.IsEnd && t.Start <= end && t.End >= start))
        {
            if (unresolved?.Invoke(tag.Start) == true) continue;
            var candidates = catalog.Find(tag.Name).ToArray();
            if (candidates.Length != 1) continue;
            var component = candidates[0];
            var context = NazorLanguage.Classify(text, tag.End - (tag.SelfClosing ? 2 : 1));
            var missing = component.Parameters.Where(p => p.IsRequired && !context.Attributes.Contains(p.Name, StringComparer.OrdinalIgnoreCase) &&
                !context.Attributes.Contains("@bind-" + p.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (!context.Attributes.Contains("@attributes"))
                foreach (var parameter in missing)
                {
                    var at = tag.End - (tag.SelfClosing ? 2 : 1);
                    var value = parameter.Kind == NazorParameterKind.EventCallback ? "@(default(global::Nazor.EventCallback))" :
                        parameter.TypeName == "string" ? "" : "@(default!)";
                    actions.Add(new("Add required parameter " + parameter.Name,
                        [new(path, text, text.Insert(at, " " + parameter.Name + "=\"" + value + "\""))]));
                }
            if (!tag.SelfClosing && missing.All(p => p.Kind != NazorParameterKind.ChildContent) &&
                NazorLanguage.EndOf(tags, tag) is { } close && close.Start == tag.End)
                actions.Add(new("Use self-closing tag", [new(path, text, text[..(tag.End - 1)].TrimEnd(' ', '\t') + " />" + text[close.End..])]));
        }

        if (Path.GetFileName(path).Equals("_Imports.nazor", StringComparison.OrdinalIgnoreCase)) return actions;
        var imports = ImportLine().Matches(text).Where(m => NazorLanguage.Classify(text, m.Index + m.Value.IndexOf('@') + 1).Kind == NazorContextKind.Directive).ToArray();
        if (imports.Length == 0 || !imports.Any(m => m.Index <= end && m.Index + m.Length >= start)) return actions;
        var target = Path.Combine(Path.GetDirectoryName(path)!, "_Imports.nazor");
        var existing = read(target);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var shared = existing ?? "";
        var known = ImportLine().Matches(shared)
            .Where(m => NazorLanguage.Classify(shared, m.Index + m.Value.IndexOf('@') + 1).Kind == NazorContextKind.Directive)
            .Select(m => m.Groups["directive"].Value.Trim()).ToHashSet(StringComparer.Ordinal);
        var additions = imports.Select(m => m.Groups["directive"].Value.Trim()).Where(known.Add).ToArray();
        var destination = existing ?? "";
        if (additions.Length > 0)
            destination += (destination.Length > 0 && !destination.EndsWith('\n') ? newline : "") + string.Join(newline, additions) + newline;
        var remaining = text;
        foreach (var import in imports.Reverse()) remaining = remaining.Remove(import.Index, import.Length);
        actions.Add(new("Move @using directives to _Imports.nazor", [new(target, existing, destination), new(path, text, remaining)]));
        return actions;
    }

    [GeneratedRegex(@"^[\t ]*(?<directive>@using[\t ]+[^\r\n]+)(?:\r?\n|$)", RegexOptions.Multiline)]
    private static partial Regex ImportLine();
}
