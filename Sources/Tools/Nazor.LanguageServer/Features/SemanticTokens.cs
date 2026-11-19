using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Classification;
using Nazor.Compiler;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Features;

internal static partial class SemanticTokens
{
    public const string Transition = "razorTransition";
    public const string Directive = "razorDirective";
    public const string DirectiveAttribute = "razorDirectiveAttribute";
    public const string ComponentElement = "razorComponentElement";
    public const string ComponentAttribute = "razorComponentAttribute";
    public const string ComponentParameter = "property name";
    public const string UnknownParameter = "excluded code";
    public const string UnknownComponent = "identifier";
    public const string CommentTransition = "razorCommentTransition";
    public const string CommentStar = "razorCommentStar";
    public const string Comment = "razorComment";
    public const string TagDelimiter = "markupTagDelimiter";
    public const string Operator = "markupOperator";
    public const string AttributeQuote = "markupAttributeQuote";
    public const string AttributeValue = "markupAttributeValue";
    public const string Element = "markupElement";
    public const string Attribute = "markupAttribute";

    private static readonly string[] Razor =
    [
        Transition, Directive, DirectiveAttribute, ComponentElement, ComponentAttribute, ComponentParameter, UnknownParameter, CommentTransition, CommentStar, Comment,
        TagDelimiter, Operator, AttributeQuote, AttributeValue, Element, Attribute,
    ];

    private static readonly Dictionary<string, string> Modifiers = new()
    {
        [ClassificationTypeNames.StaticSymbol] = "static",
        ["reassigned variable"] = "ReassignedVariable",
        ["obsolete symbol"] = "deprecated",
    };

    public static readonly string[] TokenTypes = [.. Razor.Concat(ClassificationTypeNames.AllTypeNames.Where(n => !Modifiers.ContainsKey(n) && n != "test code")).Distinct()];

    public static readonly string[] TokenModifiers = [.. Modifiers.Values];

    private static readonly Dictionary<string, int> TypeIndex = TokenTypes.Select((t, i) => (t, i)).ToDictionary(p => p.t, p => p.i);

    private static readonly string[] LineDirectives = ["using", "inject", "namespace", "inherits", "implements", "typeparam", "attribute", "layout", "preservewhitespace", "window"];

    public static List<(int Start, int Length, string Type)> Markup(string text, NazorCatalog catalog, Func<int, bool> isCSharp, Func<int, bool>? unresolved = null)
    {
        var tokens = new List<(int, int, string)>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '@' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
                var close = end < 0 ? text.Length : end;
                tokens.Add((i, 1, CommentTransition));
                tokens.Add((i + 1, 1, CommentStar));
                if (close > i + 2) tokens.Add((i + 2, close - i - 2, Comment));
                if (end >= 0)
                {
                    tokens.Add((end, 1, CommentStar));
                    tokens.Add((end + 1, 1, CommentTransition));
                }
                i = end < 0 ? text.Length : end + 2;
                continue;
            }
            if (c == '@' && i + 1 < text.Length && text[i + 1] == '@')
            {
                i += 2;
                continue;
            }
            if (c == '@')
            {
                tokens.Add((i, 1, Transition));
                var block = Block().Match(text, i);
                if (block.Success)
                {
                    var name = block.Groups["name"];
                    tokens.Add((name.Index, name.Length, Directive));
                    var open = block.Index + block.Length - 1;
                    var close = name.Value == "style" ? NazorLanguage.CloseNss(text, open) : NazorLanguage.CloseCSharp(text, open);
                    tokens.Add((open, 1, Transition));
                    if (close < text.Length) tokens.Add((close, 1, Transition));
                    i = close + 1;
                    continue;
                }
                var word = Word().Match(text, i + 1);
                if (word.Success && word.Index == i + 1 && LineDirectives.Contains(word.Value) && AtLineStart(text, i))
                {
                    tokens.Add((word.Index, word.Length, Directive));
                    i = word.Index + word.Length;
                    continue;
                }
                if (i + 1 < text.Length && text[i + 1] == '{')
                {
                    var close = NazorLanguage.CloseCSharp(text, i + 1);
                    tokens.Add((i + 1, 1, Transition));
                    if (close < text.Length) tokens.Add((close, 1, Transition));
                    i = close + 1;
                    continue;
                }
                if (i + 1 < text.Length && text[i + 1] == '(')
                {
                    var close = CSharpLexer.Parenthesis(text, i + 1);
                    tokens.Add((i + 1, 1, Transition));
                    if (close < text.Length) tokens.Add((close, 1, Transition));
                    i = i + 2;
                    continue;
                }
                i++;
                continue;
            }
            if (c == '<' && i + 1 < text.Length && (char.IsAsciiLetter(text[i + 1]) || text[i + 1] == '/'))
            {
                i = Tag(text, i, catalog, isCSharp, tokens, unresolved);
                continue;
            }
            i++;
        }
        return tokens;
    }

    private static int Tag(string text, int start, NazorCatalog catalog, Func<int, bool> isCSharp, List<(int, int, string)> tokens, Func<int, bool>? unresolved)
    {
        var j = start + 1;
        var closing = j < text.Length && text[j] == '/';
        if (closing) j++;
        tokens.Add((start, j - start, TagDelimiter));
        var nameStart = j;
        while (j < text.Length && (char.IsAsciiLetterOrDigit(text[j]) || text[j] is '_' or '.')) j++;
        var name = text[nameStart..j];
        var component = unresolved?.Invoke(start) == true ? null : catalog.Find(name).FirstOrDefault();
        var child = unresolved?.Invoke(start) != true && component is null && NazorLanguage.ChildContent(text, new(NazorContextKind.TagName, nameStart, j) { Tag = name, TagStart = start }, catalog) is not null;
        if (j > nameStart) tokens.Add((nameStart, j - nameStart, component is not null || child ? ComponentElement :
            name.Length > 0 && char.IsUpper(name[0]) ? UnknownComponent : Element));
        var directive = false;
        while (j < text.Length)
        {
            var c = text[j];
            if (c == '>')
            {
                tokens.Add((j, 1, TagDelimiter));
                return j + 1;
            }
            if (c == '/' && j + 1 < text.Length && text[j + 1] == '>')
            {
                tokens.Add((j, 2, TagDelimiter));
                return j + 2;
            }
            if (c == '<') return j;
            if (char.IsAsciiLetter(c) || c == '@')
            {
                var attributeStart = j;
                while (j < text.Length && (char.IsAsciiLetterOrDigit(text[j]) || text[j] is '_' or '-' or '@' or ':' or '.')) j++;
                var attribute = text[attributeStart..j];
                directive = attribute.StartsWith('@');
                if (directive)
                {
                    tokens.Add((attributeStart, 1, Transition));
                    if (attribute.Length > 1) tokens.Add((attributeStart + 1, attribute.Length - 1, DirectiveAttribute));
                }
                else tokens.Add((attributeStart, attribute.Length, component?.Parameters.Any(p => p.Name.Equals(attribute, StringComparison.OrdinalIgnoreCase)) == true
                    ? ComponentParameter : component is not null ? UnknownParameter : Attribute));
                continue;
            }
            if (c == '=')
            {
                tokens.Add((j, 1, Operator));
                j++;
                continue;
            }
            if (c is '"' or '\'')
            {
                tokens.Add((j, 1, AttributeQuote));
                var value = j + 1;
                int close;
                if (directive || (value < text.Length && text[value] == '@' && !(value + 1 < text.Length && text[value + 1] == '@')) || isCSharp(value))
                {
                    close = CSharpLexer.ValueEnd(text, value, c);
                    if (value < close && text[value] == '@')
                    {
                        tokens.Add((value, 1, Transition));
                        if (value + 1 < close && text[value + 1] == '(')
                        {
                            tokens.Add((value + 1, 1, Transition));
                            var end = CSharpLexer.Parenthesis(text, value + 1);
                            if (end < close) tokens.Add((end, 1, Transition));
                        }
                    }
                }
                else
                {
                    close = text.IndexOf(c, value);
                    if (close < 0) close = text.Length;
                    TextValue(text, value, close, tokens);
                }
                if (close < text.Length) tokens.Add((close, 1, AttributeQuote));
                j = close + 1;
                continue;
            }
            j++;
        }
        return j;
    }

    public static int[] Encode(string text, IEnumerable<(int Start, int Length, string Type)> markup, IEnumerable<(int Start, int Length, string Classification)> csharp,
        int rangeStart = 0, int rangeEnd = int.MaxValue)
    {
        var markupTokens = markup.Where(t => t.Length > 0).ToList();
        var covered = new bool[text.Length + 1];
        foreach (var (start, length, _) in markupTokens)
            for (var k = start; k < start + length && k < covered.Length; k++) covered[k] = true;

        var csharpTokens = csharp.ToList();
        var modifiers = csharpTokens.Where(t => Modifiers.ContainsKey(t.Classification))
            .GroupBy(t => (t.Start, t.Length)).ToDictionary(g => g.Key, g => g.Aggregate(0, (m, t) => m | 1 << Array.IndexOf(TokenModifiers, Modifiers[t.Classification])));
        var all = markupTokens.Select(t => (t.Start, t.Length, t.Type, Modifiers: 0))
            .Concat(csharpTokens.Where(t => !Modifiers.ContainsKey(t.Classification) && TypeIndex.ContainsKey(t.Classification) &&
                    !Enumerable.Range(t.Start, t.Length).Any(k => k < covered.Length && covered[k]))
                .Select(t => (t.Start, t.Length, Type: t.Classification, Modifiers: modifiers.GetValueOrDefault((t.Start, t.Length)))))
            .Where(t => t.Start + t.Length > rangeStart && t.Start < rangeEnd)
            .OrderBy(t => t.Start).ThenBy(t => t.Length)
            .ToList();

        var data = new List<int>();
        var previousLine = 0;
        var previousStart = 0;
        var lastEnd = -1;
        foreach (var token in all)
        {
            if (token.Start < lastEnd) continue;
            lastEnd = token.Start + token.Length;
            var position = token.Start;
            var end = Math.Min(token.Start + token.Length, text.Length);
            while (position < end)
            {
                var lineEnd = position;
                while (lineEnd < end && text[lineEnd] is not ('\r' or '\n')) lineEnd++;
                if (lineEnd > position)
                {
                    var at = TextPositions.Position(text, position);
                    data.Add(at.Line - previousLine);
                    data.Add(at.Line == previousLine ? at.Character - previousStart : at.Character);
                    data.Add(lineEnd - position);
                    data.Add(TypeIndex[token.Type]);
                    data.Add(token.Modifiers);
                    previousLine = at.Line;
                    previousStart = at.Character;
                }
                position = lineEnd;
                while (position < end && text[position] is '\r' or '\n') position++;
            }
        }
        return [.. data];
    }

    private static bool AtLineStart(string text, int index)
    {
        for (var k = index - 1; k >= 0 && text[k] is not ('\r' or '\n'); k--)
            if (!char.IsWhiteSpace(text[k])) return false;
        return true;
    }

    private static void TextValue(string text, int start, int end, List<(int, int, string)> tokens)
    {
        var run = start;
        var k = start;
        while (k < end)
        {
            if (text[k] == '@' && k + 1 < end && text[k + 1] == '@')
            {
                k += 2;
                continue;
            }
            if (text[k] != '@' || k + 1 >= end || (k > start && char.IsLetterOrDigit(text[k - 1])) ||
                !(text[k + 1] == '(' || char.IsLetter(text[k + 1]) || text[k + 1] == '_'))
            {
                k++;
                continue;
            }
            if (k > run) tokens.Add((run, k - run, AttributeValue));
            tokens.Add((k, 1, Transition));
            if (text[k + 1] == '(')
            {
                tokens.Add((k + 1, 1, Transition));
                var close = CSharpLexer.Parenthesis(text, k + 1);
                if (close < end) tokens.Add((close, 1, Transition));
                k = Math.Min(close + 1, end);
            }
            else
            {
                k++;
                while (k < end && (char.IsLetterOrDigit(text[k]) || text[k] == '_' || (text[k] == '.' && k + 1 < end && char.IsLetter(text[k + 1])))) k++;
            }
            run = k;
        }
        if (end > run) tokens.Add((run, end - run, AttributeValue));
    }

    [GeneratedRegex(@"\G@(?<name>code|functions|style)\s*\{")]
    private static partial Regex Block();

    [GeneratedRegex(@"\G[A-Za-z_]\w*")]
    private static partial Regex Word();
}
