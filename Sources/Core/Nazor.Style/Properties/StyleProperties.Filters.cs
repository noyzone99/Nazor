using Nazor.Style.Syntax;
using static Nazor.Style.Syntax.ValueParsers;

namespace Nazor.Style;

public static partial class StyleProperties
{
    private static FilterValue ParseFilter(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return FilterValue.None;
        var functions = new List<FilterFunction>();
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            if (token.Kind != StyleTokenKind.Function) throw TokenReader.Error("Expected a filter function.", token);
            if (functions.Count == 64) throw TokenReader.Error("A filter chain supports at most64 operations.", token);
            var kind = token.Value.ToLowerInvariant() switch
            {
                "blur" => FilterKind.Blur, "brightness" => FilterKind.Brightness, "contrast" => FilterKind.Contrast,
                "grayscale" => FilterKind.Grayscale, "hue-rotate" => FilterKind.HueRotate, "invert" => FilterKind.Invert,
                "opacity" => FilterKind.Opacity, "saturate" => FilterKind.Saturate, "sepia" => FilterKind.Sepia,
                _ => throw TokenReader.Error($"Unsupported filter function '{token.Value}'.", token),
            };
            var radius = Length.Zero;
            var amount = kind == FilterKind.HueRotate ? 0d : 1d;
            if (reader.Peek().Kind != StyleTokenKind.RightParen)
            {
                if (kind == FilterKind.Blur) radius = ParseLength(reader, allowPercent: false);
                else if (kind == FilterKind.HueRotate) amount = ParseAngle(reader);
                else
                {
                    var value = reader.Next();
                    if (value.Kind is not (StyleTokenKind.Number or StyleTokenKind.Percentage) || value.Number < 0 || !double.IsFinite(value.Number))
                        throw TokenReader.Error("A filter amount must be a finite nonnegative number or percentage.", value);
                    amount = value.Number / (value.Kind == StyleTokenKind.Percentage ? 100 : 1);
                }
            }
            reader.Expect(StyleTokenKind.RightParen, "')'");
            functions.Add(new FilterFunction(kind, amount, radius));
        }
        if (functions.Count == 0) throw TokenReader.Error("Expected none or a filter function.", reader.Peek());
        return new(new ValueList<FilterFunction>(functions));
    }
}
