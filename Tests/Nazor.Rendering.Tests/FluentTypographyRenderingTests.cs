using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentTypographyRenderingTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Fluent_resolves_the_installed_variable_family_and_paints_optically_sized_text_without_system_discovery(float scale)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires an installed Windows Segoe UI Variable font; portable icon coverage runs separately.");
        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegUIVar.ttf");
        Assert.SkipUnless(File.Exists(installed), "The Windows Segoe UI Variable font is unavailable.");
        using var harness = new RenderHarness();
        harness.Graphics.FontProvider.Register(installed);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        var root = harness.Host.Render<TypographyProbe>();
        var image = harness.Render(600, 220, scale);
        var drawings = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().ToArray();
        Assert.Equal(4, drawings.Length);
        var sizes = new[] { 12f, 14f, 20f, 28f };
        var family = new[] { "Segoe UI Variable", "Segoe UI Variable Text", "Segoe UI" };
        var fonts = harness.Graphics.Fonts;
        foreach (var element in root.Elements("TextBlock"))
        {
            var style = harness.Scene.GetStyle(element);
            Assert.Equal(family, style.Get(StyleProperties.FontFamily).Items.Take(3));
            Assert.Equal(Nazor.Style.LineHeightKind.Normal, style.Get(StyleProperties.LineHeight).Kind);
        }
        for (var i = 0; i < drawings.Length; i++)
        {
            var drawing = drawings[i];
            Assert.Equal(sizes[i], drawing.Layout.Style.FontSize);
            var expected = fonts.Match(family, 400, false, sizes[i]);
            Assert.Contains(expected.FamilyName, new[] { "Segoe UI Variable", "Segoe UI Variable Small", "Segoe UI Variable Text", "Segoe UI Variable Display" });
            Assert.Same(expected, drawing.Layout.Primary);
            Assert.All(drawing.Layout.Shaped.Runs, run =>
            {
                Assert.Same(expected, run.Face);
                Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
            });
            var primary = drawing.Layout.Primary;
            Assert.Equal(primary.Ascent(sizes[i]) + primary.Descent(sizes[i]) + primary.LineGap(sizes[i]), drawing.Layout.Height);
            Assert.Equal(primary.Ascent(sizes[i]) + primary.LineGap(sizes[i]) / 2, drawing.Layout.Baseline);
        }
        var small = fonts.Match(family, 400, false, 12);
        var large = fonts.Match(family, 400, false, 28);
        var strong = fonts.Match(family, 600, false, 12);
        Assert.NotSame(small, large);
        Assert.NotSame(small, strong);
        var smallText = new TextLayout("Hamburgefons123", new(family, 12), null, fonts, harness.Graphics.Shaper);
        var largeText = new TextLayout("Hamburgefons123", new(family, 28), null, fonts, harness.Graphics.Shaper);
        var strongText = new TextLayout("Hamburgefons123", new(family, 12, 600), null, fonts, harness.Graphics.Shaper);
        Assert.NotEqual(Math.Round(smallText.Width / 12, 5), Math.Round(largeText.Width / 28, 5));
        Assert.NotEqual(smallText.Width, strongText.Width);
        Assert.Contains(Enumerable.Range(0, image.Width * image.Height), index =>
        {
            var pixel = image.GetPixel(index % image.Width, index / image.Width);
            return pixel.R > pixel.G + 20 && pixel.B > pixel.G + 20;
        });
    }

    private sealed class TypographyProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Container");
            builder.AddAttribute(1, "style", "display: flex; flex-direction: column; gap: 8px; padding: 12px");
            foreach (var size in new[] { 12, 14, 20, 28 })
            {
                builder.OpenElement(2, "TextBlock");
                builder.AddAttribute(3, "style", $"font-size: {size}px; color: #ff00ff");
                builder.AddContent(4, "Typography AVATAR 0123456789");
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }
}
