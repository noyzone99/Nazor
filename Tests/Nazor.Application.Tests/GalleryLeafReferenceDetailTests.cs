using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class GalleryLeafReferenceDetailTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Token_cards_preserve_the_measured_reference_label_to_code_baseline_pitch(float scale)
    {
        using var host = new TestHost();
        var fonts = new FontOptions { UseSystemFonts = false };
        fonts.AddNazorLeafFonts();
        using var graphics = new SkiaRenderer(fonts);
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(900, 900), Scale = scale };
        scene.Styles.Theme = NazorLeafTheme.Theme;
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        scene.Transitions.ReducedMotion = true;
        var root = host.Render<GalleryTokenPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(GalleryTokenPage.Scene)] = scene }));
        for (var frame = 0; frame < 6; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        var card = root.Elements().First(node => node.Class == "demo-swatch-text");
        var text = card.Children.OfType<ElementNode>().ToArray();
        Assert.Equal(2, text.Length);
        var draws = scene.BuildDisplayList().Commands.OfType<DrawText>().ToArray();
        DrawText Drawing(ElementNode element)
        {
            var box = scene.GetContentBox(element);
            return Assert.Single(draws, drawing => drawing.Layout.Text == element.Text() &&
                Math.Abs(drawing.Origin.X - box.X) < 1 / scale && Math.Abs(drawing.Origin.Y - box.Y) < 1 / scale);
        }
        var label = Drawing(text[0]);
        var code = Drawing(text[1]);
        Assert.Equal(12.5f, label.Layout.Style.FontSize);
        Assert.Equal(11.5f, code.Layout.Style.FontSize);
        Assert.Equal("Geist", label.Layout.Style.FontFamilies[0]);
        Assert.Equal("Geist Mono", code.Layout.Style.FontFamilies[0]);
        var baselinePitch = code.Origin.Y + code.Layout.Baseline - label.Origin.Y - label.Layout.Baseline;
        Assert.True(Math.Abs(baselinePitch - 18.75f) <= 1 / scale,
            $"Baseline pitch {baselinePitch}; expected 18.75 ± {1 / scale}. Label origin/baseline {label.Origin.Y}/{label.Layout.Baseline}, code {code.Origin.Y}/{code.Layout.Baseline} at scale {scale}.");
        Assert.True(scene.GetContentBox(card).Contains(code.Origin));
    }
}
