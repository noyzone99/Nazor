using Nazor.Nss;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class OverflowWindow : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Content");
        builder.AddAttribute(1, "style", "width: 600px; height: 400px; flex-shrink: 0");
        builder.CloseElement();
    }
}

public sealed class WindowScrollTests
{
    [Fact]
    public void Window_content_scrolls_from_the_keyboard_before_any_pointer_or_focus_activity()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<OverflowWindow>();
        harness.Render(300, 200, 1);
        var scroll = harness.Scene.GetScrollState(root.Single("WindowContent"))!;
        Assert.Null(input.Focused);
        Assert.Null(input.Hovered);
        Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
        Assert.True(scroll.OffsetY > 0);
        Assert.True(input.KeyDown("End", KeyModifiers.None, false));
        Assert.Equal(scroll.MaxOffsetY, scroll.OffsetY);
        Assert.True(input.KeyDown("Home", KeyModifiers.None, false));
        Assert.Equal(0, scroll.OffsetY);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Window_content_scrolls_on_both_axes_and_clears_offsets_when_it_fits(bool unstyled)
    {
        using var harness = new RenderHarness();
        if (unstyled) harness.Scene.Styles.Theme = null;
        var root = harness.Host.Render<OverflowWindow>();
        harness.Render(300, 200, 1);
        var content = root.Single("WindowContent");
        var scroll = harness.Scene.GetScrollState(content)!;
        Assert.True(scroll.CanScrollX);
        Assert.True(scroll.CanScrollY);
        Assert.NotNull(harness.Scene.GetScrollBar(content, horizontal: true));
        Assert.NotNull(harness.Scene.GetScrollBar(content, horizontal: false));
        Assert.Null(harness.Scene.GetScrollState(root.Single("Window")));
        Assert.True(harness.Scene.ScrollBy(content, 50, 60, immediate: true));
        Assert.Equal(50, scroll.OffsetX);
        Assert.Equal(60, scroll.OffsetY);
        harness.Render(800, 600, 1);
        Assert.False(scroll.CanScrollX);
        Assert.False(scroll.CanScrollY);
        Assert.Equal(0, scroll.OffsetX);
        Assert.Equal(0, scroll.OffsetY);
        Assert.Null(harness.Scene.GetScrollBar(content, horizontal: true));
        Assert.Null(harness.Scene.GetScrollBar(content, horizontal: false));
    }

    [Theory]
    [InlineData("hidden", Overflow.Hidden)]
    [InlineData("visible", Overflow.Visible)]
    [InlineData("initial", Overflow.Visible)]
    public void Window_styles_can_override_the_default_overflow(string value, Overflow expected)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<OverflowWindow>();
        harness.Scene.Styles.Application = NssCompiler.Compile("window-scroll", $"WindowContent {{ overflow: {value}; }}").StyleSheet;
        harness.Render(300, 200, 1);
        var content = root.Single("WindowContent");
        Assert.Equal(expected, harness.Scene.GetStyle(content).Get(StyleProperties.OverflowX));
        Assert.Equal(expected, harness.Scene.GetStyle(content).Get(StyleProperties.OverflowY));
        Assert.Null(harness.Scene.GetScrollState(content));
    }
}
