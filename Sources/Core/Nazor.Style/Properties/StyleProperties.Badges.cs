using Nazor.Style.Syntax;
using static Nazor.Style.Syntax.ValueParsers;

namespace Nazor.Style;

public static partial class StyleProperties
{
    private static BorderStyle ParseBadgeBorderStyle(TokenReader reader) => ParseKeyword(reader, BorderStyleKeywords);
    private static BackgroundRepeat ParseBadgeRepeat(TokenReader reader) => ParseKeyword(reader, RepeatKeywords);

    private static ValueList<ImageValue> ParseBadgeMask(TokenReader reader)
    {
        var images = ParseImages(reader);
        if (images.Items.Count > 1 || images.Items.Any(image => image is not UrlImage))
            throw TokenReader.Error("A badge mask requires one url() image or none.", reader.Peek());
        return images;
    }

    private static void RegisterBadgeShorthands()
    {
        StyleProperty[] widths = [WindowsBadgeBorderTopWidth, WindowsBadgeBorderRightWidth, WindowsBadgeBorderBottomWidth, WindowsBadgeBorderLeftWidth];
        AddShorthand("-windows-badge-border-width", widths, "[thin | medium | thick | <length>]{1,4}", r => Box(r, widths, reader => ParseBorderWidth(reader)));
        StyleProperty[] styles = [WindowsBadgeBorderTopStyle, WindowsBadgeBorderRightStyle, WindowsBadgeBorderBottomStyle, WindowsBadgeBorderLeftStyle];
        AddShorthand("-windows-badge-border-style", styles, "[none | solid | dashed | dotted]{1,4}", r => Box(r, styles, reader => ParseBadgeBorderStyle(reader)));
        StyleProperty[] colors = [WindowsBadgeBorderTopColor, WindowsBadgeBorderRightColor, WindowsBadgeBorderBottomColor, WindowsBadgeBorderLeftColor];
        AddShorthand("-windows-badge-border-color", colors, "<color>{1,4}", r => Box(r, colors, reader => ParseColor(reader)));
        AddShorthand("-windows-badge-border", [.. widths, .. styles, .. colors], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowsBadgeBorderTopWidth, WindowsBadgeBorderTopStyle, WindowsBadgeBorderTopColor), (WindowsBadgeBorderRightWidth, WindowsBadgeBorderRightStyle, WindowsBadgeBorderRightColor), (WindowsBadgeBorderBottomWidth, WindowsBadgeBorderBottomStyle, WindowsBadgeBorderBottomColor), (WindowsBadgeBorderLeftWidth, WindowsBadgeBorderLeftStyle, WindowsBadgeBorderLeftColor)]));
        AddShorthand("-windows-badge-border-top", [WindowsBadgeBorderTopWidth, WindowsBadgeBorderTopStyle, WindowsBadgeBorderTopColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowsBadgeBorderTopWidth, WindowsBadgeBorderTopStyle, WindowsBadgeBorderTopColor)]));
        AddShorthand("-windows-badge-border-right", [WindowsBadgeBorderRightWidth, WindowsBadgeBorderRightStyle, WindowsBadgeBorderRightColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowsBadgeBorderRightWidth, WindowsBadgeBorderRightStyle, WindowsBadgeBorderRightColor)]));
        AddShorthand("-windows-badge-border-bottom", [WindowsBadgeBorderBottomWidth, WindowsBadgeBorderBottomStyle, WindowsBadgeBorderBottomColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowsBadgeBorderBottomWidth, WindowsBadgeBorderBottomStyle, WindowsBadgeBorderBottomColor)]));
        AddShorthand("-windows-badge-border-left", [WindowsBadgeBorderLeftWidth, WindowsBadgeBorderLeftStyle, WindowsBadgeBorderLeftColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowsBadgeBorderLeftWidth, WindowsBadgeBorderLeftStyle, WindowsBadgeBorderLeftColor)]));
        AddShorthand("-windows-badge-background", [WindowsBadgeBackgroundColor, WindowsBadgeBackgroundImage, WindowsBadgeBackgroundPosition, WindowsBadgeBackgroundSize, WindowsBadgeBackgroundRepeat],
            "[<bg-layer> ,]* [[<image> | none] || <position> [/ <bg-size>]? || [repeat | no-repeat | repeat-x | repeat-y] || <color>]",
            r => ParseBackground(r).Select(pair => pair.Item1.Name switch
            {
                "background-color" => ((StyleProperty)WindowsBadgeBackgroundColor, (object)new NativeColor((Color)pair.Item2)),
                "background-image" => (WindowsBadgeBackgroundImage, pair.Item2),
                "background-position" => (WindowsBadgeBackgroundPosition, pair.Item2),
                "background-size" => (WindowsBadgeBackgroundSize, pair.Item2),
                _ => (WindowsBadgeBackgroundRepeat, pair.Item2),
            }).ToArray());
    }
}
