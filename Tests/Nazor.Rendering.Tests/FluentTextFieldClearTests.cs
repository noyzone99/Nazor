using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Fluent;

namespace Nazor.Rendering.Tests;

public sealed class FluentTextFieldClearTests
{
    [Theory]
    [InlineData(ColorScheme.Light, 1f)]
    [InlineData(ColorScheme.Light, 1.5f)]
    [InlineData(ColorScheme.Light, 2f)]
    [InlineData(ColorScheme.Dark, 1f)]
    [InlineData(ColorScheme.Dark, 1.5f)]
    [InlineData(ColorScheme.Dark, 2f)]
    public async Task Dismiss_glyph_is_centered_and_the_clear_action_remains_functional(ColorScheme scheme, float scale)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        foreach (var file in Nazor.Themes.Fluent.Themes.FontFiles)
            ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        harness.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true);
        var root = harness.Host.Render<TextField>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(TextField.Value)] = "Atelier",
            [nameof(TextField.Clearable)] = true,
            [nameof(Control.Style)] = "width: 260px; margin: 20px;",
        }));
        Frame();
        var clear = root.Single("TextFieldClear");
        var bounds = harness.Scene.GetBounds(clear);
        var icon = harness.Scene.Styles.GetGeneratedElement(clear, PseudoElement.Before);
        Assert.NotNull(icon);
        Assert.Equal(FluentSymbol.Dismiss12Regular.Glyph, icon.Text());
        Assert.Equal(0d, harness.Scene.GetStyle(clear).Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal(28f, bounds.Width, 3);
        var iconBounds = harness.Scene.GetBounds(icon);
        Assert.Equal(bounds.X + bounds.Width / 2, iconBounds.X + iconBounds.Width / 2, 3);
        Assert.Equal(bounds.Y + bounds.Height / 2, iconBounds.Y + iconBounds.Height / 2, 3);
        var drawing = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text == icon.Text());
        Assert.Equal(12f, drawing.Layout.Style.FontSize);
        Assert.All(drawing.Layout.Shaped.Runs.SelectMany(run => run.Glyphs), glyph => Assert.NotEqual((ushort)0, glyph));
        input.Focus(clear, visible: true);
        Frame();
        Assert.NotEqual(BorderStyle.None, harness.Scene.GetStyle(clear).Get(StyleProperties.OutlineStyle));
        await harness.Host.ClickAsync(clear);
        Frame();
        Assert.Empty(((TextField)root.Component).Text);
        Assert.Empty(root.Elements("TextFieldClear"));

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(340, 140, scale);
            harness.Host.RunPending();
            harness.Render(340, 140, scale);
        }
    }
}
