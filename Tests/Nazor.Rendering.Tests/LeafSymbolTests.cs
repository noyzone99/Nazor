using System.Reflection;
using System.Text.Json;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Skia;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafSymbolTests
{
    private sealed record SymbolEntry(string Name, string File, int Codepoint);

    private static SymbolEntry[] Entries => JsonSerializer.Deserialize<SymbolEntry[]>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LeafCatalog", "Symbols.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static LeafSymbol Symbol(string name) => Assert.IsType<LeafSymbol>(typeof(LeafSymbol).GetProperty(name)!.GetValue(null));

    private static ParameterView Parameters(IFontSymbol? symbol, ImageSource? source = null, int size = 24) =>
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Icon.Symbol)] = symbol,
            [nameof(Icon.Source)] = source,
            [nameof(Icon.Style)] = $"font-size: {size}px; line-height: 1; color: #000000; background: #ffffff",
        });

    private static void RegisterIconFont(RenderHarness harness) =>
        ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, NazorLeafTheme.AssetDirectory, "NazorLeafIcons.otf"));

    [Fact]
    public void The_catalog_matches_the_stable_map_and_private_icon_font()
    {
        var options = new FontOptions { UseSystemFonts = false };
        options.AddNazorLeafFonts();
        using var provider = new SkiaFontProvider(options);
        var entries = Entries;
        var members = typeof(LeafSymbol).GetProperties(BindingFlags.Static | BindingFlags.Public);
        Assert.Equal(55, entries.Length);
        Assert.Equal("\uE026", LeafSymbol.Star.Glyph);
        Assert.Equal("\uE030", LeafSymbol.StarFilled.Glyph);
        Assert.Equal("\uE031", LeafSymbol.Mail.Glyph);
        Assert.Equal("\uE032", LeafSymbol.Chat.Glyph);
        Assert.Equal("\uE033", LeafSymbol.At.Glyph);
        Assert.Equal("\uE034", LeafSymbol.Pause.Glyph);
        Assert.Equal("\uE035", LeafSymbol.Dot.Glyph);
        Assert.Equal("\uE036", LeafSymbol.Dock.Glyph);
        Assert.Equal(entries.Select(entry => entry.Name).Order(), members.Select(member => member.Name).Order());
        var face = provider.Match(["NazorLeaf Icons"], 400, false);
        Assert.Equal("NazorLeaf Icons", face.FamilyName);
        foreach (var entry in entries)
        {
            var symbol = Symbol(entry.Name);
            Assert.Equal("NazorLeaf Icons", symbol.FontFamily);
            Assert.Equal(char.ConvertFromUtf32(entry.Codepoint), symbol.Glyph);
            Assert.True(face.HasGlyph(entry.Codepoint), $"Missing Leaf glyph: {entry.Name}");
        }
    }

    [Fact]
    public void Registration_preserves_application_fonts_and_does_not_duplicate_paths()
    {
        var options = new FontOptions();
        options.Add("Fonts/Application.ttf");
        Assert.Same(options, options.AddNazorLeafFonts());
        options.AddNazorLeafFonts();
        Assert.Equal(new[] { "Fonts/Application.ttf" }.Concat(NazorLeafTheme.FontFiles), options.PrivateFontFiles);
        Assert.Contains("Themes/NazorLeaf/NazorLeafIcons.otf", options.PrivateFontFiles);
        Assert.Throws<ArgumentNullException>(() => NazorLeafTheme.AddNazorLeafFonts(null!));
    }

    [Fact]
    public void The_default_leaf_symbol_is_not_an_active_icon()
    {
        using var host = new TestHost(captureExceptions: true);
        host.Render<Icon>(Parameters(default(LeafSymbol)));
        Assert.IsType<ArgumentException>(Assert.Single(host.Exceptions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Symbols_work_with_basic_or_leaf_and_raw_glyphs_use_the_leaf_family(bool leafTheme)
    {
        using var harness = new RenderHarness();
        RegisterIconFont(harness);
        if (leafTheme) harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        var parameters = Parameters(leafTheme ? null : LeafSymbol.Plus);
        var values = new Dictionary<string, object?>(parameters.ToDictionary());
        values[nameof(Icon.Glyph)] = LeafSymbol.Plus.Glyph;
        var root = harness.Host.Render<Icon>(ParameterView.FromDictionary(values));
        harness.Render(80, 80, 1);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(LeafSymbol.Plus.Glyph, root.Single("Icon").Text());
        Assert.Equal("NazorLeaf Icons", Assert.Single(text.Layout.Shaped.Runs).Face.FamilyName);
    }

    [Fact]
    public void Copy_symbol_keeps_its_hollow_regions_with_the_native_font_backend()
    {
        using var harness = new RenderHarness();
        RegisterIconFont(harness);
        harness.Host.Render<Icon>(Parameters(LeafSymbol.Copy, size: 48));
        var image = harness.Render(48, 48, 1);
        Assert.Equal(Nazor.Graphics.Color.White, image.GetPixel(12, 12));
        Assert.Equal(Nazor.Graphics.Color.White, image.GetPixel(28, 28));
        Assert.True(image.GetPixel(8, 18).R < 50);
    }

    [Theory]
    [InlineData(24, 2f)]
    [InlineData(48, 1f)]
    [InlineData(96, 1f)]
    public void Every_font_icon_preserves_the_original_svg_artwork(int size, float scale)
    {
        using var harness = new RenderHarness();
        RegisterIconFont(harness);
        var root = harness.Host.Render<Icon>();
        foreach (var entry in Entries)
        {
            Assert.True(root.Component.SetParametersAsync(Parameters(Symbol(entry.Name), size: size)).IsCompletedSuccessfully);
            harness.Host.RunPending();
            var fontImage = harness.Render(size, size, scale);
            var run = Assert.Single(Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Shaped.Runs);
            Assert.Equal("NazorLeaf Icons", run.Face.FamilyName);
            Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
            var source = ImageSource.FromFile(Path.Combine("LeafCatalog", entry.File));
            Assert.True(root.Component.SetParametersAsync(Parameters(null, source, size)).IsCompletedSuccessfully);
            harness.Host.RunPending();
            harness.Render(size, size, scale);
            var loading = harness.Scene.WhenImagesLoaded();
            harness.Host.RunUntil(() => loading.IsCompleted);
            Assert.True(loading.IsCompletedSuccessfully);
            harness.Host.RunPending();
            var svgImage = harness.Render(size, size, scale);
            double error = 0, coverage = 0;
            for (var y = 0; y < svgImage.Height; y++)
            for (var x = 0; x < svgImage.Width; x++)
            {
                var expected = svgImage.GetPixel(x, y).R;
                error += Math.Abs(fontImage.GetPixel(x, y).R - expected);
                coverage += 255 - expected;
            }
            Assert.True(coverage > 0, $"Empty SVG reference: {entry.Name}");
            var meanError = error / (255 * svgImage.Width * svgImage.Height);
            if (meanError >= 0.04)
            {
                var artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts", "leaf-icons");
                Directory.CreateDirectory(artifacts);
                File.WriteAllBytes(Path.Combine(artifacts, $"{entry.Name}-{size}-{scale}-font.png"), harness.Graphics.EncodePng(fontImage));
                File.WriteAllBytes(Path.Combine(artifacts, $"{entry.Name}-{size}-{scale}-svg.png"), harness.Graphics.EncodePng(svgImage));
            }
            Assert.True(meanError < 0.04, $"Leaf icon differs from SVG: {entry.Name}, size {size}, scale {scale}, mean error {meanError:F3}");
            Assert.True(error / coverage < 0.25, $"Leaf icon differs from SVG: {entry.Name}, size {size}, scale {scale}, relative error {error / coverage:F3}");
        }
    }
}
