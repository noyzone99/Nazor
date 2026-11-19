using Nazor.Style.Syntax;

namespace Nazor.Style;

public static partial class StyleProperties
{
    private static FontSynthesis ParseFontSynthesis(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return Style.FontSynthesis.None;
        var result = Style.FontSynthesis.None;
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            var part = token.Is(StyleTokenKind.Ident,"weight") ? Style.FontSynthesis.Weight : token.Is(StyleTokenKind.Ident,"style") ? Style.FontSynthesis.Style : Style.FontSynthesis.None;
            if (part == Style.FontSynthesis.None || result.HasFlag(part)) throw TokenReader.Error("Expected weight and/or style, each once.",token);
            result |= part;
        }
        if (result == Style.FontSynthesis.None) throw TokenReader.Error("Expected none, weight or style.",reader.Peek());
        return result;
    }
}
