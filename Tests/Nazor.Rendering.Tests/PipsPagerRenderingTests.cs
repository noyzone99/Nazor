using Nazor.Controls;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class PipsPagerRenderingTests
{
    [Theory]
    [InlineData("basic", 24, 8)]
    [InlineData("fluent", 12, 6)]
    [InlineData("leaf", 24, 10)]
    public void Each_theme_has_visible_selected_dots_and_a_bounded_hit_target(string theme, double targetWidth, double selectedWidth)
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = Theme(theme);
        var root = h.Host.Render<PipsPager>(Parameters());
        h.Render(450, 300, 1.5f);
        var pips = root.Elements("PipsPagerPip").ToArray();
        Assert.Equal(5, pips.Length);
        var selected = pips.Single(pip => pip.Attributes.GetValueOrDefault("current") is true);
        var dot = selected.Elements().Single(element => element.Name == "PipsPagerDot");
        Assert.InRange(h.Scene.GetBounds(selected).Width, targetWidth - .7, targetWidth + .7);
        Assert.InRange(h.Scene.GetBounds(dot).Width, selectedWidth - .7, selectedWidth + .7);
        Assert.True(h.Scene.GetBounds(dot).Height > 0);
        Assert.True(h.Scene.GetStyle(dot).BackgroundColor.A > 0);
        Assert.All(root.Elements("PipsPagerDot"), mark => Assert.True(mark.Attributes.GetValueOrDefault("semantic-hidden") is true));
        Assert.True(h.Scene.GetBounds(root.Single("PipsPagerPrevious")).Width >= 23);
    }

    [Fact]
    public void Fluent_vertical_targets_follow_the_native_resource_axis_and_hover_press_keep_the_hit_target()
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = Theme("fluent");
        var root = h.Host.Render<PipsPager>(Parameters(orientation: Orientation.Vertical));
        h.Render(450, 300, 1);
        var pip = root.Elements("PipsPagerPip").First();
        var dot = pip.Elements().Single(element => element.Name == "PipsPagerDot");
        var box = h.Scene.GetBounds(pip);
        Assert.Equal(24, box.Width, 1); Assert.Equal(12, box.Height, 1);
        input.PointerMoved(new(box.X + box.Width / 2, box.Y + box.Height / 2), KeyModifiers.None); h.Render(450, 300, 1);
        Assert.Equal(6, h.Scene.GetBounds(dot).Width, 1);
        input.PointerPressed(new(box.X + box.Width / 2, box.Y + box.Height / 2), PointerButton.Primary, KeyModifiers.None); h.Render(450, 300, 1);
        Assert.Equal(4, h.Scene.GetBounds(dot).Width, 1);
        Assert.Equal(box, h.Scene.GetBounds(pip));
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Forced_colors_preserve_dot_selection_and_disabled_mark_contrast(string theme)
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = Theme(theme);
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Dark, true, ForcedColors: true, Colors: SystemColors.Default);
        var root = h.Host.Render<PipsPager>(Parameters(disabled: true));
        h.Render(450, 300, 1);
        Assert.All(root.Elements("PipsPagerDot"), dot => Assert.Equal(SystemColors.Default.GrayText, h.Scene.GetStyle(dot).BackgroundColor));
    }

    private static StyleSheet Theme(string name) => name switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic,
    };
    private static ParameterView Parameters(bool disabled = false, Orientation orientation = Orientation.Horizontal) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(PipsPager.Count)] = 12, [nameof(PipsPager.SelectedIndex)] = 2,
        [nameof(PipsPager.Disabled)] = disabled, [nameof(PipsPager.Orientation)] = orientation,
    });
}
