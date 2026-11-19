using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.LanguageServer.Features;

internal sealed record NssVariable(string Name, int Start, int Block, string? Documentation);

internal static class NssVariables
{
    internal static List<NssVariable> Read(string text)
    {
        var tokens = StyleTokenizer.Tokenize(text).Where(t => t.Kind != StyleTokenKind.Whitespace).ToArray();
        var result = new List<NssVariable>();
        var blocks = new Stack<int>();
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.Kind == StyleTokenKind.LeftBrace) { blocks.Push(token.Start); continue; }
            if (token.Kind == StyleTokenKind.RightBrace) { if (blocks.Count > 0) blocks.Pop(); continue; }
            if (blocks.Count == 0 || token.Kind != StyleTokenKind.Ident || !StyleProperties.IsCustomProperty(token.Value) ||
                i + 1 >= tokens.Length || tokens[i + 1].Kind != StyleTokenKind.Colon || i == 0 ||
                tokens[i - 1].Kind is not (StyleTokenKind.LeftBrace or StyleTokenKind.Semicolon)) continue;
            var following = tokens.Skip(i + 2).FirstOrDefault(t => t.Kind is StyleTokenKind.Semicolon or StyleTokenKind.LeftBrace or StyleTokenKind.RightBrace);
            if (following.Kind == StyleTokenKind.LeftBrace) continue;
            result.Add(new(token.Value, token.Start, blocks.Peek(), Documentation(text, tokens[i - 1].Start + tokens[i - 1].Length, token.Start)));
        }
        return result;
    }

    internal static string? Describe(string text, string name, int offset, IReadOnlyList<NssVariable> declarations)
    {
        var candidates = declarations.Where(d => d.Name == name).ToArray();
        if (candidates.Length == 0) return null;
        var exact = candidates.FirstOrDefault(d => offset >= d.Start && offset <= d.Start + name.Length);
        if (exact is not null) return exact.Documentation;
        var block = new Stack<int>();
        foreach (var token in StyleTokenizer.Tokenize(text[..Math.Clamp(offset, 0, text.Length)]))
            if (token.Kind == StyleTokenKind.LeftBrace) block.Push(token.Start);
            else if (token.Kind == StyleTokenKind.RightBrace && block.Count > 0) block.Pop();
        if (block.Count > 0 && candidates.Where(d => d.Block == block.Peek()).OrderBy(d => d.Start).LastOrDefault() is { } local)
            return local.Documentation;
        var descriptions = candidates.Select(d => d.Documentation).Distinct(StringComparer.Ordinal).ToArray();
        return descriptions.Length == 1 ? descriptions[0] : null;
    }

    private static string? Documentation(string text, int boundary, int start)
    {
        var prefix = text[boundary..start].TrimEnd();
        if (!prefix.EndsWith("*/", StringComparison.Ordinal)) return null;
        var open = prefix.LastIndexOf("/*", StringComparison.Ordinal);
        if (open < 0 || !prefix.AsSpan(open).StartsWith("/**")) return null;
        var body = Regex.Replace(prefix[(open + 3)..^2].Replace("\r\n", "\n"), @"(?m)^[ \t]*\* ?", "").Trim();
        try
        {
            var doc = XElement.Parse(body, LoadOptions.PreserveWhitespace);
            if (doc.Name != "doc" || doc.HasAttributes || doc.Elements().Any()) return null;
            var description = string.Join("\n", doc.Value.Split('\n').Select(line => line.Trim())).Trim();
            if (description.Length == 0) return null;
            return Regex.Replace(description, @"([\\`*_{}\[\]()<>#+.!|])", @"\$1");
        }
        catch (System.Xml.XmlException) { return null; }
    }
}
