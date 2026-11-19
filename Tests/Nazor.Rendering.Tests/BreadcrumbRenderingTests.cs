using Nazor.Controls;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class BreadcrumbRenderingTests
{
    [Theory]
    [InlineData("basic", 1f)]
    [InlineData("basic", 1.5f)]
    [InlineData("basic", 2f)]
    [InlineData("fluent", 1f)]
    [InlineData("fluent", 1.5f)]
    [InlineData("fluent", 2f)]
    [InlineData("leaf", 1f)]
    [InlineData("leaf", 1.5f)]
    [InlineData("leaf", 2f)]
    public void Narrow_paths_preserve_the_current_location_and_its_complete_focus_border(string theme, float scale)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = h.Host.Render<BreadcrumbBar<string>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "width: 104px; margin: 12px",
            [nameof(BreadcrumbBar<string>.Items)] = new[] { "Home", "Documents", "Design", "An unusually long current location" },
        }));
        for (var frame = 0; frame < 8; frame++) { h.Render(500, 200, scale); h.Host.RunPending(); }
        var bar = (BreadcrumbBar<string>)root.Component;
        Assert.Equal(3, bar.HiddenItemCount);
        var current = root.Elements("Button").Single(node => node.Attributes.GetValueOrDefault("breadcrumb-index") is 3);
        var slot = root.Elements("BreadcrumbSlot").Last();
        var bounds = h.Scene.GetBounds(slot);
        Assert.True(bounds.Width > 8 && bounds.Right <= h.Scene.GetBounds(root.Single("BreadcrumbBar")).Right);
        Assert.True(h.Scene.GetBounds(current).Width > bounds.Width);
        Assert.Equal("An unusually long current location", current.Attributes["accessible-name"]);
        Assert.NotNull(current.Attributes.GetValueOrDefault("item-position"));
        input.Focus(current, visible: true);
        var image = h.Render(500, 200, scale);
        var style = h.Scene.GetStyle(slot);
        Assert.Equal(BorderStyle.Solid, style.Get(StyleProperties.OutlineStyle));
        Assert.Equal(BorderStyle.None, h.Scene.GetStyle(current).Get(StyleProperties.OutlineStyle));
        var outline = style.Get(StyleProperties.OutlineColor);
        var color = new Nazor.Graphics.Color(outline.R, outline.G, outline.B, outline.A);
        var y = (int)((bounds.Y + bounds.Height / 2) * scale);
        Assert.Equal(color, image.GetPixel((int)((bounds.X + 1.5f) * scale), y));
        Assert.Equal(color, image.GetPixel((int)((bounds.Right - 1.5f) * scale), y));
        input.KeyDown("Home", KeyModifiers.None, false);
        input.KeyUp("Home", KeyModifiers.None);
        Assert.Equal("MenuButton", input.Focused?.Name);
    }
}
