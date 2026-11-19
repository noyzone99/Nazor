using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ScrollBarAnimationTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;

    public ScrollBarAnimationTests()
    {
        harness = new RenderHarness(clock: clock);
        harness.Scene.Transitions.ReducedMotion = false;
        SetStyle("");
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<ScrollBarPage>();
        Frame();
    }

    private ElementNode Viewer => root.Single("Viewer");
    private ScrollBarGeometry Bar => harness.Scene.GetScrollBar(Viewer, false)!.Value;
    private bool WheelScrolling => Viewer.States.HasFlag(ElementStates.WheelScrolling);

    private void SetStyle(string declarations, string extra = "")
    {
        var result = NssCompiler.Compile("scrollbar-animation", "Page { width: 300px; height: 200px; } " +
            "Viewer { width: 200px; height: 120px; overflow: auto; scrollbar-visibility: autohide; " + declarations +
            " } Content { width: 600px; height: 400px; } Cover { display: none; } " + extra);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        harness.Scene.Styles.Theme = result.StyleSheet;
    }

    private DisplayList Frame(double elapsed = 0)
    {
        clock.Advance(elapsed);
        harness.Scene.Viewport = new Size(300, 200);
        harness.Scene.Update();
        return harness.Scene.BuildDisplayList();
    }

    [Fact]
    public void Default_autohide_fades_in_holds_and_fades_out_without_layout_changes()
    {
        Assert.Equal(0, Bar.Opacity);
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Frame(60);
        Assert.InRange(Bar.Opacity, 0.1, 0.99);
        Assert.Equal(TimeSpan.Zero, harness.Scene.TimeUntilNextFrame);
        Frame(60);
        Assert.Equal(1, Bar.Opacity);
        input.PointerLeft();
        Assert.Equal(TimeSpan.FromMilliseconds(300), harness.Scene.TimeUntilNextFrame);
        Frame(300);
        Assert.Equal(1, Bar.Opacity);
        var commands = Frame(100).Commands;
        Assert.InRange(Bar.Opacity, 0.01, 0.99);
        Assert.Contains(commands, command => command is PushOpacity { Opacity: > 0 and < 1 });
        Assert.Equal(0, harness.Scene.LastUpdate.LayoutComputations);
        Frame(100);
        Assert.Equal(0, Bar.Opacity);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Custom_fade_timing_and_reversals_continue_from_the_current_opacity()
    {
        SetStyle("scrollbar-fade-in-duration: 200ms; scrollbar-fade-out-duration: 400ms; scrollbar-fade-out-delay: 0; scrollbar-fade-timing-function: linear;");
        Frame();
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Frame(100);
        Assert.Equal(0.5, Bar.Opacity, 5);
        input.PointerLeft();
        Assert.Equal(0.5, Bar.Opacity, 5);
        Frame(100);
        Assert.Equal(0.375, Bar.Opacity, 5);
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Assert.Equal(0.375, Bar.Opacity, 5);
        Frame(100);
        Assert.Equal(0.6875, Bar.Opacity, 5);
    }

    [Fact]
    public void Reduced_motion_and_zero_durations_disable_automatic_fades()
    {
        harness.Scene.Transitions.ReducedMotion = true;
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Assert.Equal(1, Bar.Opacity);
        input.PointerLeft();
        Assert.Equal(0, Bar.Opacity);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
        harness.Scene.Transitions.ReducedMotion = false;
        SetStyle("scrollbar-fade-in-duration: 0; scrollbar-fade-out-duration: 0; scrollbar-fade-out-delay: 0;");
        Frame();
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Assert.Equal(1, Bar.Opacity);
        input.PointerLeft();
        Assert.Equal(0, Bar.Opacity);
    }

    [Fact]
    public void Wheel_reveals_the_bar_and_resets_the_configurable_idle_timeout()
    {
        SetStyle("scrollbar-wheel-timeout: 200ms; scrollbar-fade-in-duration: 0; scrollbar-fade-out-duration: 0; scrollbar-fade-out-delay: 0;");
        Frame();
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Frame();
        Assert.True(WheelScrolling);
        Assert.Equal(1, Bar.Opacity);
        Assert.Equal(TimeSpan.FromMilliseconds(200), harness.Scene.TimeUntilNextFrame);
        Frame(150);
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Frame(150);
        Assert.True(WheelScrolling);
        Frame(50);
        Assert.False(WheelScrolling);
        Assert.Equal(0, Bar.Opacity);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Wheel_scrolling_selector_can_hide_scrollbars_and_restore_proximity_after_idle()
    {
        harness.Scene.Transitions.ReducedMotion = true;
        SetStyle("scrollbar-wheel-timeout: 100ms;", "Viewer:wheel-scrolling { scrollbar-visibility: none; }");
        Frame();
        input.PointerMoved(new Point(190, 40), KeyModifiers.None);
        Assert.True(Bar.IsVisible);
        input.PointerWheel(new Point(190, 40), -1, KeyModifiers.None);
        Frame();
        Assert.True(WheelScrolling);
        Assert.Null(harness.Scene.GetScrollBar(Viewer, false));
        Frame(100);
        Assert.False(WheelScrolling);
        Assert.True(Bar.IsVisible);
    }

    [Fact]
    public void Wheel_at_a_scroll_limit_still_activates_the_state_but_zero_input_does_not()
    {
        input.PointerWheel(new Point(50, 50), 0, KeyModifiers.None);
        Assert.False(WheelScrolling);
        input.PointerWheel(new Point(50, 50), 1, KeyModifiers.None);
        Assert.True(WheelScrolling);
        Assert.Equal(0, harness.Scene.GetScrollState(Viewer)!.OffsetY);
    }

    [Fact]
    public void Custom_keyframes_animate_only_scrollbar_properties_during_wheel_scrolling()
    {
        SetStyle("scrollbar-fade-in-duration: 0;", """
            Viewer:wheel-scrolling { animation: wheel-bar 200ms linear both; }
            @keyframes wheel-bar {
                from { scrollbar-opacity: 0.2; scrollbar-width: 4px; scrollbar-thumb-color: red; }
                to { scrollbar-opacity: 0.8; scrollbar-width: 12px; scrollbar-thumb-color: blue; }
            }
            """);
        Frame();
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Frame();
        var commands = Frame(100).Commands;
        Assert.Equal(0.5, Bar.Opacity, 4);
        Assert.Equal(8, Bar.Thumb.Width, 4);
        Assert.Contains(commands, command => command is FillRectangle fill && fill.Rect == Bar.Thumb && fill.Color == new Nazor.Graphics.Color(128, 0, 128, 255));
        Assert.Equal(1, harness.Scene.GetStyle(Viewer).Opacity);
    }

    [Fact]
    public void Standard_transitions_animate_scrollbar_styles_from_a_wheel_selector()
    {
        SetStyle("scrollbar-visibility: visible; transition: scrollbar-width 200ms linear;", "Viewer:wheel-scrolling { scrollbar-width: 12px; }");
        Frame();
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Frame();
        Frame(100);
        Assert.Equal(8, Bar.Thumb.Width, 4);
    }

    [Fact]
    public void Configurable_reveal_distance_and_opacity_apply_independently_of_the_container()
    {
        SetStyle("scrollbar-reveal-distance: 30px; scrollbar-opacity: 0.4; scrollbar-fade-in-duration: 0;");
        Frame();
        input.PointerMoved(new Point(175, 40), KeyModifiers.None);
        Frame();
        Assert.Equal(0.4, Bar.Opacity);
        Assert.Equal(1, harness.Scene.GetStyle(Viewer).Opacity);
    }

    [Fact]
    public void Hidden_or_removed_containers_stop_requesting_scrollbar_frames()
    {
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Frame();
        Assert.NotNull(harness.Scene.TimeUntilNextFrame);
        SetStyle("display: none;");
        Frame();
        Assert.False(WheelScrolling);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Prevented_wheel_and_programmatic_scrolling_do_not_activate_the_wheel_state()
    {
        var page = (ScrollBarPage)root.Component;
        page.PreventWheel = true;
        page.Refresh();
        Frame();
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Assert.False(WheelScrolling);
        Assert.Equal(0, harness.Scene.GetScrollState(Viewer)!.OffsetY);
        input.ScrollTo(Viewer, null, 80);
        Assert.False(WheelScrolling);
    }

    [Fact]
    public void Nested_wheel_scrolling_marks_the_container_that_consumes_the_event()
    {
        SetStyle("", "Content { overflow: auto; } Detail { width: 1200px; height: 1000px; }");
        Frame();
        var inner = root.Single("Content");
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Assert.True(inner.States.HasFlag(ElementStates.WheelScrolling));
        Assert.False(WheelScrolling);
        Frame(600);
        input.ScrollTo(inner, null, harness.Scene.GetScrollState(inner)!.MaxOffsetY);
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Assert.False(inner.States.HasFlag(ElementStates.WheelScrolling));
        Assert.True(WheelScrolling);
        Assert.True(harness.Scene.GetScrollState(Viewer)!.OffsetY > 0);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }
}
