using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class FontRelativeUnitPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "FontBox");
        builder.AddAttribute(1, "id", "font-box");
        builder.AddContent(2, "0000");
        builder.CloseElement();
    }
}

public sealed class FontRelativeUnitRenderingTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Actual_font_zero_advance_and_xheight_drive_lengths_without_letter_spacing(float scale)
    {
        using var h = new RenderHarness();
        var sheet = NssCompiler.Compile("metrics", "FontBox { font-family: 'Fira Sans'; font-size: 20px; letter-spacing: 10px; width: 4ch; height: 3ex; }");
        Assert.True(sheet.Success);
        h.Scene.Styles.Application = sheet.StyleSheet;
        var root = h.Host.Render<FontRelativeUnitPage>();
        h.Render(400, 200, scale);
        var box = root.Single("FontBox");
        var face = h.Graphics.Fonts.Match(["Fira Sans"], 400, false, 20);
        var zero = Assert.IsType<float>(face.GetGlyphAdvance('0', 20));
        var x = Assert.IsType<float>(face.GetXHeight(20));
        var computed = h.Scene.GetStyle(box);
        Assert.Equal(zero * 4, computed.Get(StyleProperties.Width).Length.Value, 4);
        Assert.Equal(x * 3, computed.Get(StyleProperties.Height).Length.Value, 4);
        var shaped = h.Graphics.Shaper.Shape("0", 0, 1, face, 20);
        Assert.Equal(shaped.Width, zero, 4);
        Assert.InRange(Math.Abs(h.Scene.GetBounds(box).Width - zero * 4), 0, 1 / scale);
    }

    [Fact]
    public void Registering_the_requested_font_recomputes_previously_resolved_font_relative_lengths()
    {
        using var h = new RenderHarness();
        var sheet = NssCompiler.Compile("metrics", "FontBox { font-family: 'Geist Mono'; font-size: 32px; width: 10ch; }");
        Assert.True(sheet.Success);
        h.Scene.Styles.Application = sheet.StyleSheet;
        var root = h.Host.Render<FontRelativeUnitPage>();
        h.Render(800, 200, 1);
        var box = root.Single("FontBox");
        var before = h.Scene.GetStyle(box).Get(StyleProperties.Width).Length.Value;
        ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, "Themes", "NazorLeaf", "GeistMono.ttf"));
        h.Render(800, 200, 1);
        var face = h.Graphics.Fonts.Match(["Geist Mono"], 400, false, 32);
        Assert.Equal("Geist Mono", face.FamilyName);
        var after = h.Scene.GetStyle(box).Get(StyleProperties.Width).Length.Value;
        Assert.Equal(face.GetGlyphAdvance('0', 32)!.Value * 10, after, 4);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Unavailable_backend_metrics_are_nullable_and_invalid_metric_requests_are_rejected()
    {
        using var h = new RenderHarness();
        ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, "Themes", "NazorLeaf", "NazorLeafIcons.otf"));
        var face = h.Graphics.Fonts.Match(["NazorLeaf Icons"], 400, false, 20);
        Assert.Null(face.GetGlyphAdvance('0', 20));
        Assert.Null(face.GetXHeight(20));
        Assert.Equal(0, face.GetGlyphAdvance('x', 0) ?? 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => face.GetGlyphAdvance(0xD800, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => face.GetXHeight(float.NaN));
    }
}
