using System.Globalization;
using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.LanguageServer.Features;

internal static class NssColors
{
    private static readonly HashSet<string> ColorFunctions = new(StringComparer.OrdinalIgnoreCase) { "rgb", "rgba", "hsl", "hsla", "color-mix" };

    public static List<(int Start, int End, Color Color)> Find(string text, bool component)
    {
        var colors = new List<(int, int, Color)>();
        if (!component) Find(text, 0, colors);
        else
            foreach (var (start, end) in NazorLanguage.StyleBlocks(text))
                Find(text[start..end], start, colors);
        return colors;
    }

    private static void Find(string nss, int offset, List<(int, int, Color)> colors)
    {
        var tokens = StyleTokenizer.Tokenize(nss);
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != StyleTokenKind.Ident) continue;
            var colon = i + 1;
            while (colon < tokens.Count && tokens[colon].Kind == StyleTokenKind.Whitespace) colon++;
            if (colon >= tokens.Count || tokens[colon].Kind != StyleTokenKind.Colon) continue;
            var end = colon + 1;
            while (end < tokens.Count && tokens[end].Kind is not (StyleTokenKind.Semicolon or StyleTokenKind.RightBrace or StyleTokenKind.LeftBrace or StyleTokenKind.EndOfFile)) end++;
            if (end < tokens.Count && tokens[end].Kind == StyleTokenKind.LeftBrace) continue;
            for (var k = colon + 1; k < end; k++)
            {
                var token = tokens[k];
                var last = k;
                if (token.Kind == StyleTokenKind.Function && ColorFunctions.Contains(token.Value))
                {
                    var depth = 1;
                    while (last + 1 < end && depth > 0)
                    {
                        last++;
                        if (tokens[last].Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                        else if (tokens[last].Kind == StyleTokenKind.RightParen) depth--;
                    }
                    if (depth != 0) continue;
                }
                else if (token.Kind is not (StyleTokenKind.Hash or StyleTokenKind.Ident)) continue;
                var start = token.Start;
                var stop = tokens[last].Start + tokens[last].Length;
                if (Parse(nss[start..stop]) is { } color) colors.Add((offset + start, offset + stop, color));
                k = last;
            }
            i = end - 1;
        }
    }

    internal static Color? Parse(string value)
    {
        try
        {
            return StyleProperties.Color.Parse(new TokenReader(StyleTokenizer.Tokenize(value))) is Color { IsCurrentColor: false } color ? color : null;
        }
        catch (StyleValueException)
        {
            return null;
        }
    }

    public static string[] Presentations(Color color)
    {
        var alpha = (color.A / 255.0).ToString("0.###", CultureInfo.InvariantCulture);
        return color.A == 255
            ? [$"#{color.R:x2}{color.G:x2}{color.B:x2}", $"rgb({color.R}, {color.G}, {color.B})"]
            : [$"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}", $"rgba({color.R}, {color.G}, {color.B}, {alpha})"];
    }
}
