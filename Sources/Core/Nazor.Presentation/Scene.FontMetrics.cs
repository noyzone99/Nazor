using Nazor.Graphics.Text;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private int fontMetricsVersion = -1;

    private FontRelativeMetrics MeasureFontRelativeUnits(ComputedStyle style)
    {
        if (style.FontSize == 0) return new FontRelativeMetrics(0, 0);
        var text = TextContent.ToTextStyle(style);
        var face = fonts.Match(text);
        var zero = face.HasGlyph('0') ? face : fonts.Fallback('0', face, text);
        var advance = zero is not null && text.FontFeatures.Count > 0 ? shaper.Shape("0",0,1,zero,text).Width : zero?.GetGlyphAdvance('0', text.FontSize);
        return new FontRelativeMetrics(advance, face.GetXHeight(text.FontSize));
    }

    private void RefreshFontRelativeUnits()
    {
        if (fontMetricsVersion == fonts.Version) return;
        fontMetricsVersion = fonts.Version;
        Styles.InvalidateAll();
    }
}
