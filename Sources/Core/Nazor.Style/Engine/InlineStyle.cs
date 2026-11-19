using System.Collections.Concurrent;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>
/// The declarations of a <c>style</c> attribute (<c>width: 40%; --accent: red</c>), read at runtime: they apply after
/// every rule of every level, as the inline styles of CSS. Each value is parsed for the element, after <c>var()</c>,
/// so an invalid one behaves as <c>unset</c>; unknown properties and pieces without a colon are skipped, as browsers
/// do.
/// </summary>
public static class InlineStyle
{
    private const int CacheLimit = 1024;
    private static readonly ConcurrentDictionary<string, IReadOnlyList<StyleDeclaration>> Cache = new(StringComparer.Ordinal);

    /// <summary>The declarations of <paramref name="text" />, in order; the same text is read once.</summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting IReadOnlyList&lt;StyleDeclaration&gt; value.</returns>
    /// <exception cref="ArgumentNullException">text is null.</exception>
    public static IReadOnlyList<StyleDeclaration> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Cache.TryGetValue(text, out var cached)) return cached;

        if (Cache.Count >= CacheLimit) Cache.Clear();
        return Cache[text] = Read(StyleTokenizer.Tokenize(text));
    }

    private static List<StyleDeclaration> Read(List<StyleToken> tokens)
    {
        var declarations = new List<StyleDeclaration>();
        var start = 0;
        var depth = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            switch (tokens[i].Kind)
            {
                case StyleTokenKind.Function or StyleTokenKind.LeftParen or StyleTokenKind.LeftBracket or StyleTokenKind.LeftBrace:
                    depth++;
                    break;
                case StyleTokenKind.RightParen or StyleTokenKind.RightBracket or StyleTokenKind.RightBrace:
                    depth = Math.Max(0, depth - 1);
                    break;
                case StyleTokenKind.Semicolon when depth == 0:
                case StyleTokenKind.EndOfFile:
                    Add(tokens[start..i]);
                    start = i + 1;
                    break;
            }
        }
        return declarations;

        void Add(List<StyleToken> piece)
        {
            var trimmed = Trim(piece);
            if (trimmed.Count < 3 || trimmed[0].Kind != StyleTokenKind.Ident) return;
            var colon = 1;
            while (colon < trimmed.Count && trimmed[colon].Kind == StyleTokenKind.Whitespace) colon++;
            if (colon >= trimmed.Count || trimmed[colon].Kind != StyleTokenKind.Colon) return;
            var value = Trim(trimmed[(colon + 1)..]);
            if (value.Count == 0) return;
            var name = trimmed[0].Value;
            if (name.StartsWith("--", StringComparison.Ordinal))
                declarations.Add(new CustomPropertyDeclaration(name, SystemColors.Substitute(value)));
            else if (StyleProperties.TryGet(name, out _) || StyleProperties.TryGetShorthand(name, out _))
                declarations.Add(new PendingDeclaration(name.ToLowerInvariant(), SystemColors.MayName(name) ? SystemColors.Substitute(value) : value));
        }
    }

    private static List<StyleToken> Trim(List<StyleToken> tokens)
    {
        var (from, to) = (0, tokens.Count);
        while (from < to && tokens[from].Kind == StyleTokenKind.Whitespace) from++;
        while (to > from && tokens[to - 1].Kind == StyleTokenKind.Whitespace) to--;
        return tokens[from..to];
    }
}
