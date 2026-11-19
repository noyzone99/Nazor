using System.Reflection;
using System.Text;
using System.Text.Json;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Fluent;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FontSymbolTests
{
    private readonly record struct CustomSymbol(string FontFamily, string Glyph) : IFontSymbol;

    private static ParameterView Parameters(IFontSymbol? symbol, string? style = null, ImageSource? source = null) =>
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Icon.Symbol)] = symbol,
            [nameof(Icon.Glyph)] = "raw glyph",
            [nameof(Icon.Style)] = style,
            [nameof(Icon.Source)] = source,
        });

    [Fact]
    public void A_generic_symbol_selects_its_family_and_preserves_color_and_size()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Icon>(Parameters(new CustomSymbol("Fira Sans", "A"),
            "font-family: Missing; color: #ff00ff; font-size: 32px"));
        harness.Render(80, 80, 1);
        var element = root.Single("Icon");
        var style = harness.Scene.GetStyle(element);
        Assert.Equal("A", element.Text());
        Assert.Equal(["Fira Sans"], style.Get(StyleProperties.FontFamily).Items);
        Assert.Equal(new Size(32, 32), harness.Scene.GetBounds(element).Size);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(new Graphics.Color(255, 0, 255, 255), text.Color);
        Assert.All(text.Layout.Shaped.Runs, run => Assert.Equal("Fira Sans", run.Face.FamilyName));
    }

    [Theory]
    [InlineData("A font with spaces")]
    [InlineData("A \"quoted\" font\\family; color: red")]
    public void Symbol_families_are_single_quoted_nss_values(string family)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Icon>(Parameters(new CustomSymbol(family, "A"), "color: #010203"));
        harness.Render(80, 80, 1);
        var style = harness.Scene.GetStyle(root.Single("Icon"));
        Assert.Equal([family], style.Get(StyleProperties.FontFamily).Items);
        Assert.Equal(new Nazor.Style.Color(1, 2, 3, 255), style.Color);
    }

    [Theory]
    [InlineData("", "A")]
    [InlineData("Fira Sans", "")]
    [InlineData("Fira\nSans", "A")]
    [InlineData("Fira\0Sans", "A")]
    public void Invalid_active_symbols_are_rejected(string family, string glyph)
    {
        using var host = new TestHost(captureExceptions: true);
        host.Render<Icon>(Parameters(new CustomSymbol(family, glyph)));
        Assert.IsType<ArgumentException>(Assert.Single(host.Exceptions));
    }

    [Fact]
    public void The_empty_default_fluent_symbol_is_rejected()
    {
        using var host = new TestHost(captureExceptions: true);
        host.Render<Icon>(Parameters(default(FluentSymbol)));
        Assert.IsType<ArgumentException>(Assert.Single(host.Exceptions));
    }

    [Fact]
    public void An_image_takes_precedence_over_symbol_and_raw_glyph()
    {
        using var harness = new RenderHarness();
        var source = ImageSource.FromBytes(Encoding.UTF8.GetBytes("<svg viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg>"));
        var root = harness.Host.Render<Icon>(Parameters(default(FluentSymbol), "font-family: Fira Sans", source));
        var element = root.Single("Icon");
        Assert.Equal("", element.Text());
        Assert.Same(source, element.Attributes["source"]);
        Assert.Equal("font-family: Fira Sans", element.Attributes["style"]);
    }

    [Fact]
    public async Task Removing_a_symbol_restores_raw_glyph_and_caller_font()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<Icon>(Parameters(new CustomSymbol("Other", "A"), "font-family: Fira Sans"));
        await root.Component.SetParametersAsync(Parameters(null, "font-family: Fira Sans"));
        harness.Host.RunPending();
        harness.Render(80, 80, 1);
        var element = root.Single("Icon");
        Assert.Equal("raw glyph", element.Text());
        Assert.Equal(["Fira Sans"], harness.Scene.GetStyle(element).Get(StyleProperties.FontFamily).Items);
    }

    [Fact]
    public void Registering_fluent_fonts_preserves_existing_fonts_and_is_idempotent()
    {
        var fonts = new FontOptions();
        fonts.Add("Fonts/Custom.ttf");
        Assert.Same(fonts, fonts.AddFluentFonts());
        fonts.AddFluentFonts();
        Assert.Equal(new[] { "Fonts/Custom.ttf" }.Concat(FluentThemes.FontFiles), fonts.PrivateFontFiles);
        Assert.Throws<ArgumentNullException>(() => FluentThemes.AddFluentFonts(null!));
    }

    [Fact]
    public void The_complete_catalog_matches_the_pinned_maps_and_private_fonts_without_system_fonts()
    {
        var options = new FontOptions { UseSystemFonts = false };
        options.AddFluentFonts();
        using var provider = new SkiaFontProvider(options);
        var symbols = typeof(FluentSymbol).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => Assert.IsType<FluentSymbol>(property.GetValue(null))).ToArray();
        Assert.Equal(20654, symbols.Length);
        foreach (var variant in new[] { "Regular", "Filled" })
        {
            var family = "FluentSystemIcons-" + variant;
            var face = provider.Match([family], 400, false);
            Assert.Equal(family, face.FamilyName);
            var variantSymbols = symbols.Where(symbol => symbol.FontFamily == family).ToArray();
            var actual = variantSymbols.Select(symbol => symbol.Glyph.EnumerateRunes().Single().Value).Order().ToArray();
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "FluentCatalog", family + ".json")))!;
            Assert.Equal(map.Values.Order(), actual);
            foreach (var symbol in variantSymbols)
                Assert.True(face.HasGlyph(symbol.Glyph.EnumerateRunes().Single().Value), $"Missing glyph in {family}: {symbol.Glyph}");
        }
        Assert.Contains(symbols, symbol => symbol.Glyph.Length == 2);
    }

    [Theory]
    [InlineData(nameof(FluentSymbol.Add20Regular))]
    [InlineData(nameof(FluentSymbol.Star20Filled))]
    [InlineData(nameof(FluentSymbol.AlbumSubtract20Regular))]
    [InlineData(nameof(FluentSymbol.ArrowDownload20Regular))]
    public void Bundled_symbols_shape_and_paint_without_a_system_icon_font(string symbolName)
    {
        using var harness = new RenderHarness();
        foreach (var file in FluentThemes.FontFiles) ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        IFontSymbol symbol = Assert.IsType<FluentSymbol>(typeof(FluentSymbol).GetProperty(symbolName)!.GetValue(null));
        var root = harness.Host.Render<Icon>(Parameters(symbol, "font-size: 32px; color: #ff00ff"));
        var image = harness.Render(80, 80, 1);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        var run = Assert.Single(text.Layout.Shaped.Runs);
        Assert.Equal(symbol.FontFamily, run.Face.FamilyName);
        Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
        Assert.Equal(symbol.Glyph, root.Single("Icon").Text());
        Assert.Contains(Enumerable.Range(0, image.Width * image.Height), pixel =>
        {
            var color = image.GetPixel(pixel % image.Width, pixel / image.Width);
            return color.R > color.G + 20 && color.B > color.G + 20 && color.A > 0;
        });
    }
}
