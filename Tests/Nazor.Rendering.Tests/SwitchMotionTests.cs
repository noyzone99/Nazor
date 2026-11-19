using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class SwitchMotionTests
{
    public static IEnumerable<object[]> ThemesAndSizes =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from size in Enum.GetValues<ControlSize>()
        select new object[] { theme, size };

    [Theory]
    [MemberData(nameof(ThemesAndSizes))]
    public async Task Checked_travel_has_an_intermediate_position_and_hover_or_press_does_not_shift_the_position_part(string theme, ControlSize size)
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Theme(theme);
        harness.Scene.Transitions.ReducedMotion = false;
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
        var root = harness.Host.Render<Switch>(Parameters(size));
        var control = (Switch)root.Component;
        Frame();
        var position = root.Single("SwitchThumbPosition");
        var off = harness.Scene.GetBounds(position).X;
        await control.ToggleAsync(); Frame();
        Assert.Equal(off, harness.Scene.GetBounds(position).X, 1);
        clock.Advance(20); Frame();
        var intermediate = harness.Scene.GetBounds(position).X;
        clock.Advance(600); Frame();
        var on = harness.Scene.GetBounds(position).X;
        Assert.InRange(intermediate, off + .1f, on - .1f);
        var thumb = harness.Scene.GetBounds(root.Single("SwitchThumb"));
        var center = new Point(thumb.X + thumb.Width / 2, thumb.Y + thumb.Height / 2);
        input.PointerMoved(center, KeyModifiers.None); Frame();
        clock.Advance(400); Frame();
        Assert.Equal(on, harness.Scene.GetBounds(position).X, 1);
        input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None); Frame();
        clock.Advance(400); Frame();
        Assert.Equal(on, harness.Scene.GetBounds(position).X, 1);
        input.PointerCanceled(); Frame();
        await control.ToggleAsync(); Frame();
        clock.Advance(20); Frame();
        Assert.InRange(harness.Scene.GetBounds(position).X, off + .1f, on - .1f);
        clock.Advance(600); Frame();
        Assert.Equal(off, harness.Scene.GetBounds(position).X, 1);

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(400, 160, 1);
            harness.Host.RunPending();
            harness.Render(400, 160, 1);
        }
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public async Task Reduced_motion_applies_the_checked_position_immediately(string theme)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = Theme(theme);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<Switch>(Parameters(ControlSize.Medium));
        harness.Render(400, 160, 1);
        var position = root.Single("SwitchThumbPosition");
        var off = harness.Scene.GetBounds(position).X;
        await ((Switch)root.Component).ToggleAsync();
        harness.Host.RunPending(); harness.Render(400, 160, 1);
        Assert.True(harness.Scene.GetBounds(position).X > off + 5);
    }

    private static ParameterView Parameters(ControlSize size) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(Switch.Size)] = size,
        [nameof(Switch.Style)] = "margin: 24px; width: 240px;",
    });

    private static StyleSheet Theme(string theme) => theme switch
    {
        "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
        "leaf" => NazorLeafTheme.Theme,
        _ => Nazor.Themes.Themes.Basic,
    };
}
