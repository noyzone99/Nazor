using Nazor.Controls;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class FluentSelectFocusTests
{
    [Theory]
    [InlineData(ColorScheme.Light, false)]
    [InlineData(ColorScheme.Dark, false)]
    [InlineData(ColorScheme.Light, true)]
    [InlineData(ColorScheme.Dark, true)]
    public async Task Opening_does_not_accent_the_frame_and_keyboard_focus_remains_visible(ColorScheme scheme, bool forcedColors)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors);
        var root = harness.Host.Render<Select<string>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Select<string>.Items)] = new[] { "Rome", "Milan" },
            [nameof(Select<string>.Value)] = "Rome",
            [nameof(Control.Style)] = "width: 220px; margin: 20px;",
        }));
        Frame();
        var field = root.Single("Select");
        var border = harness.Scene.GetStyle(field).Get(StyleProperties.BorderLeftColor);
        var bottom = harness.Scene.GetStyle(field).Get(StyleProperties.BorderBottomColor);
        await harness.Host.ClickAsync(field);
        Frame();
        Assert.True(field.Attributes.GetValueOrDefault("expanded") is true);
        AssertFrame();
        await ((Select<string>)root.Component).ToggleAsync();
        Frame();
        input.Focus(field, visible: true);
        Frame();
        AssertFrame();
        Assert.NotEqual(BorderStyle.None, harness.Scene.GetStyle(field).Get(StyleProperties.OutlineStyle));
        input.KeyDown("F4", KeyModifiers.None, false);
        Frame();
        Assert.True(field.Attributes.GetValueOrDefault("expanded") is true);
        AssertFrame();
        Assert.NotNull(input.Focused);
        Assert.NotEqual(BorderStyle.None, harness.Scene.GetStyle(input.Focused!).Get(StyleProperties.OutlineStyle));

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(320, 300, 1);
            harness.Host.RunPending();
            harness.Render(320, 300, 1);
        }

        void AssertFrame()
        {
            var style = harness.Scene.GetStyle(field);
            Assert.Equal(border, style.Get(StyleProperties.BorderLeftColor));
            Assert.Equal(bottom, style.Get(StyleProperties.BorderBottomColor));
        }
    }
}
