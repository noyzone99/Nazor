using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using GraphicsOpticalSizing = Nazor.Graphics.Text.FontOpticalSizing;

namespace Nazor.Rendering.Tests;

public sealed class FontVariationRenderingTests
{
    private static string ThemeFont(string file) => Path.Combine(AppContext.BaseDirectory, "Themes", "NazorLeaf", file);

    private static string Bricolage => ThemeFont("BricolageGrotesque.ttf");

    private static TextStyle Style(float size = 34, int weight = 800) => new(["Bricolage Grotesque"], size, weight);

    private static float Coordinate(FontFace face, uint tag) => Assert.Single(Assert.IsType<SkiaFontFace>(face).Typeface.VariationDesignPosition, coordinate => coordinate.Axis == tag).Value;

    [Theory]
    [InlineData("Bricolage Grotesque")]
    [InlineData("Bricolage Grotesque 96pt ExtraBold")]
    public void Private_typographic_and_legacy_families_resolve_before_an_available_default(string family)
    {
        using var fonts = new SkiaFontProvider(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(RenderHarness.FontDirectory, "FiraSans-Regular.ttf"), Bricolage],
            DefaultFamily = "Fira Sans", UseSystemFonts = false,
        });
        var face = fonts.Match(new TextStyle([family], 34));
        Assert.StartsWith("Bricolage Grotesque", face.FamilyName);
        Assert.Equal(25.5f, Coordinate(face, 0x6f70737a));
    }

    [Fact]
    public void Text_styles_copy_axis_input_and_compare_axis_contents_and_optical_policy()
    {
        var input = new Dictionary<string, double> { ["wght"] = 650, ["opsz"] = 72 };
        var first = Style() with { FontVariations = input, OpticalSizing = GraphicsOpticalSizing.Auto };
        var second = Style() with { FontVariations = new Dictionary<string, double> { ["opsz"] = 72, ["wght"] = 650 }, OpticalSizing = GraphicsOpticalSizing.Auto };
        input["opsz"] = 12;
        Assert.Equal(72, first.FontVariations["opsz"]);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, second with { OpticalSizing = GraphicsOpticalSizing.None });
        Assert.NotEqual(first, second with { FontVariations = new Dictionary<string, double> { ["opsz"] = 96, ["wght"] = 650 } });
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, double>)first.FontVariations).Add("wdth", 100));
        Assert.Throws<ArgumentException>(() => Style() with { FontVariations = new Dictionary<string, double> { ["bad"] = 1 } });
        Assert.Throws<ArgumentException>(() => Style() with { FontVariations = new Dictionary<string, double> { ["opsz"] = double.NaN } });
    }

    [Theory]
    [InlineData(17f)]
    [InlineData(28f)]
    [InlineData(64f)]
    public void Css_auto_and_native_optical_sizing_resolve_their_distinct_size_coordinates(float size)
    {
        using var fonts = new SkiaFontProvider(new FontOptions { PrivateFontFiles = [Bricolage], UseSystemFonts = false });
        var basis = Style(size);
        var auto = fonts.Match(basis with { OpticalSizing = GraphicsOpticalSizing.Auto });
        var native = fonts.Match(basis);
        Assert.Equal(size, Coordinate(auto, 0x6f70737a));
        Assert.Equal(size * 0.75f, Coordinate(native, 0x6f70737a));
        var none = basis with { OpticalSizing = GraphicsOpticalSizing.None };
        Assert.Same(auto, fonts.Match(none with { FontVariations = new Dictionary<string, double> { ["opsz"] = size } }));
        Assert.Same(native, fonts.Match(none with { FontVariations = new Dictionary<string, double> { ["opsz"] = size * 0.75 } }));
        Assert.Same(native, fonts.Match(basis.FontFamilies, basis.FontWeight, basis.Italic, basis.FontSize));
        Assert.Same(fonts.Match(basis.FontFamilies, basis.FontWeight, basis.Italic), fonts.Match(none));
        Assert.NotSame(native, auto);
    }

    [Fact]
    public void Explicit_axes_override_high_level_weight_and_optical_size_and_clamp_before_float_conversion()
    {
        using var fonts = new SkiaFontProvider(new FontOptions { PrivateFontFiles = [Bricolage], UseSystemFonts = false });
        var basis = Style() with { OpticalSizing = GraphicsOpticalSizing.Auto };
        FontFace Resolve(double optical) => fonts.Match(basis with { FontVariations = new Dictionary<string, double> { ["opsz"] = optical } });
        Assert.Same(Resolve(96), Resolve(double.MaxValue));
        Assert.Same(Resolve(12), Resolve(-double.MaxValue));
        Assert.Same(fonts.Match(basis), fonts.Match(basis with { FontVariations = new Dictionary<string, double> { ["OPSZ"] = 96, ["ZZZZ"] = 100 } }));
        var light = basis with { FontVariations = new Dictionary<string, double> { ["opsz"] = 72, ["wght"] = 400 } };
        var heavy = light with { FontVariations = new Dictionary<string, double> { ["opsz"] = 72, ["wght"] = 800 } };
        var shaper = new HarfBuzzTextShaper();
        Assert.NotEqual(new TextLayout("Hamburgefons012", light, null, fonts, shaper).Width, new TextLayout("Hamburgefons012", heavy, null, fonts, shaper).Width);
        var face = fonts.Match(heavy);
        Assert.Equal(72, Coordinate(face, 0x6f70737a));
        Assert.Equal(800, Coordinate(face, 0x77676874));
        Assert.Equal(face.GetGlyphAdvance('0', heavy.FontSize)!.Value, shaper.Shape("0", 0, 1, face, heavy.FontSize).Width, 4);
    }

    [Fact]
    public void Glyph_fallback_preserves_explicit_axes_and_original_selection_weight()
    {
        var icons = ThemeFont("NazorLeafIcons.otf");
        using var fonts = new SkiaFontProvider(new FontOptions { PrivateFontFiles = [Bricolage, icons], UseSystemFonts = false });
        var style = Style() with { FontFamilies = new[] { "NazorLeaf Icons" }, FontVariations = new Dictionary<string, double> { ["opsz"] = 72, ["wght"] = 450 } };
        var layout = new TextLayout("A0", style, null, fonts, new HarfBuzzTextShaper());
        var expected = fonts.Match(style with { FontFamilies = new[] { "Bricolage Grotesque" } });
        Assert.Equal("NazorLeaf Icons", layout.Primary.FamilyName);
        Assert.All(layout.Runs, run => Assert.Same(expected, run.Face));
    }

    [Fact]
    public void Static_glyph_fallback_uses_requested_weight_when_the_primary_has_only_regular_glyphs()
    {
        using var fonts = new SkiaFontProvider(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(RenderHarness.FontDirectory, "FiraSans-Regular.ttf"),
                Path.Combine(RenderHarness.FontDirectory, "FiraSans-Bold.ttf"), ThemeFont("NazorLeafIcons.otf")],
            DefaultFamily = "Fira Sans", UseSystemFonts = false,
        });
        var style = new TextStyle(["NazorLeaf Icons"], 24, 800);
        var layout = new TextLayout("A0", style, null, fonts, new HarfBuzzTextShaper());
        Assert.Equal(400, layout.Primary.Weight);
        var expected = fonts.Match(style with { FontFamilies = new[] { "Fira Sans" } });
        Assert.Equal(700, expected.Weight);
        Assert.All(layout.Runs, run => Assert.Same(expected, run.Face));
    }

    [Fact]
    public void Programmatic_axis_lists_use_the_last_duplicate_coordinate()
    {
        using var h = new RenderHarness();
        h.Graphics.FontProvider.Register(Bricolage);
        var result = NssCompiler.Compile("axes", "FontBox { font-family: 'Bricolage Grotesque'; font-size: 34px; width: 10ch; }");
        Assert.True(result.Success);
        var rule = Assert.Single(result.StyleSheet.Rules);
        var axes = new ValueList<FontVariationSetting>([new("opsz", 12), new("wght", 800), new("opsz", 72)]);
        h.Scene.Styles.Application = new StyleSheet("programmatic-axes", [new StyleRule(rule.Selector, rule.Media,
            [.. rule.Declarations, new ValueDeclaration(StyleProperties.FontVariationSettings, axes)], 0)]);
        var root = h.Host.Render<FontRelativeUnitPage>();
        h.Render(500, 120, 1);
        var drawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.StartsWith("Bricolage Grotesque", drawing.Layout.Primary.FamilyName);
        Assert.Equal(72, drawing.Layout.Style.FontVariations["opsz"]);
        Assert.Equal(72, Coordinate(drawing.Layout.Primary, 0x6f70737a));
        Assert.Equal(drawing.Layout.Primary.GetGlyphAdvance('0', 34)!.Value * 10,
            h.Scene.GetStyle(root.Single("FontBox")).Get(StyleProperties.Width).Length.Value, 4);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void Scene_axis_changes_invalidate_shaping_raster_and_font_relative_lengths(float scale)
    {
        using var h = new RenderHarness();
        h.Graphics.FontProvider.Register(Bricolage);
        var root = h.Host.Render<FontRelativeUnitPage>();
        var first = NssCompiler.Compile("axes", "FontBox { font-family: 'Bricolage Grotesque'; font-size: 34px; width: 10ch; height: 80px; font-variation-settings: 'opsz' 12, 'wght' 400; }");
        var second = NssCompiler.Compile("axes", "FontBox { font-family: 'Bricolage Grotesque'; font-size: 34px; width: 10ch; height: 80px; font-variation-settings: 'opsz' 96, 'wght' 800; }");
        Assert.True(first.Success && second.Success);
        h.Scene.Styles.Application = first.StyleSheet;
        var before = h.Render(500, 120, scale);
        var firstDrawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        var box = root.Single("FontBox");
        var firstWidth = h.Scene.GetStyle(box).Get(StyleProperties.Width).Length.Value;
        h.Scene.Styles.Application = second.StyleSheet;
        var after = h.Render(500, 120, scale);
        var drawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.StartsWith("Bricolage Grotesque", drawing.Layout.Primary.FamilyName);
        Assert.NotSame(firstDrawing.Layout.Shaped, drawing.Layout.Shaped);
        Assert.NotSame(firstDrawing.Layout.Primary, drawing.Layout.Primary);
        Assert.False(before.Pixels.SequenceEqual(after.Pixels));
        var zero = drawing.Layout.Primary.GetGlyphAdvance('0', 34)!.Value;
        Assert.Equal(zero * 10, h.Scene.GetStyle(box).Get(StyleProperties.Width).Length.Value, 4);
        Assert.NotEqual(firstWidth, zero * 10);
    }
}
