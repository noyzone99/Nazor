using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class SmoothScrollingTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;
    private ElementNode Viewer => root.Single("Viewer");
    private ScrollState State => harness.Scene.GetScrollState(Viewer)!;
    private static readonly Point WheelPoint = new(50, 50);

    public SmoothScrollingTests()
    {
        harness = new RenderHarness(clock: clock);
        harness.Scene.Transitions.ReducedMotion = false;
        SetStyle();
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<ScrollBarPage>();
        Frame();
    }

    private void SetStyle(string declarations = "", string extra = "")
    {
        var result = NssCompiler.Compile("smooth-scroll", "Page { width: 300px; height: 200px; } " +
            "Viewer { width: 200px; height: 120px; overflow: auto; scroll-behavior: smooth; scroll-duration: 200ms; scroll-timing-function: linear; " + declarations +
            " } Content { width: 600px; height: 1000px; } Cover { display: none; } " + extra);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        harness.Scene.Styles.Theme = result.StyleSheet;
    }

    private void Frame(double milliseconds = 0)
    {
        clock.Advance(milliseconds);
        harness.Scene.Viewport = new Size(300, 200);
        harness.Scene.Update();
        harness.Scene.BuildDisplayList();
    }

    [Fact]
    public void Smooth_requests_schedule_frames_and_preserve_fractional_offsets_without_layout()
    {
        Assert.True(harness.Scene.ScrollTo(Viewer, 15.5f, 100.5f));
        Assert.Equal(0, State.OffsetY);
        Assert.Equal(TimeSpan.Zero, harness.Scene.TimeUntilNextFrame);
        Frame(50);
        Assert.Equal(25.125f, State.OffsetY);
        Assert.Equal(3.875f, State.OffsetX);
        Assert.Equal(0, harness.Scene.LastUpdate.LayoutComputations);
        Frame(150);
        Assert.Equal(100.5f, State.OffsetY);
        Assert.Equal(15.5f, State.OffsetX);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Successive_detents_accumulate_and_reversal_changes_direction_from_the_visible_position()
    {
        SetStyle("scroll-wheel-step: 40px;");
        Frame();
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(50);
        Assert.Equal(10, State.OffsetY);
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Assert.Equal(10, State.OffsetY);
        Frame(100);
        Assert.Equal(45, State.OffsetY);
        input.PointerWheel(WheelPoint, 0.5f, KeyModifiers.None);
        Frame(100);
        Assert.Equal(35, State.OffsetY);
        Frame(100);
        Assert.Equal(25, State.OffsetY);
    }

    [Theory]
    [InlineData("24.5px", "20px", 24.5)]
    [InlineData("3 lines", "20px", 60)]
    [InlineData("1.5lines", "20px", 30)]
    [InlineData("2 lines", "1.5", 48)]
    [InlineData("2em", "20px", 32)]
    [InlineData("calc(20px + 0.5em)", "20px", 28)]
    [InlineData("0", "20px", 0)]
    public void Wheel_step_uses_lengths_or_the_container_line_height(string step, string lineHeight, float expected)
    {
        SetStyle($"scroll-wheel-step: {step}; font-size: 16px; line-height: {lineHeight};");
        Frame();
        input.WheelScrollByPage = true;
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        Assert.Equal(expected, State.OffsetY, 3);
    }

    [Fact]
    public void Auto_step_preserves_system_distance_and_page_scrolling()
    {
        input.WheelScrollDistance = 37;
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        Assert.Equal(37, State.OffsetY);
        input.WheelScrollByPage = true;
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        Assert.Equal(37 + State.ViewportHeight, State.OffsetY);
    }

    [Fact]
    public void Precise_pixel_input_remains_direct_and_interrupts_pending_motion()
    {
        SetStyle("scroll-wheel-step: 3 lines;");
        Frame();
        harness.Scene.ScrollTo(Viewer, null, 100);
        Frame(50);
        input.WheelScrollByPage = true;
        input.PointerWheel(WheelPoint, 0, -0.75f, KeyModifiers.None, ScrollDeltaUnit.LogicalPixels);
        Assert.Equal(25.75f, State.OffsetY);
        Frame(600);
        Assert.Equal(25.75f, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shift_and_horizontal_wheel_use_the_same_step_and_smoothing(bool shift)
    {
        SetStyle("scroll-wheel-step: 30px;");
        Frame();
        if (shift) input.PointerWheel(WheelPoint, -1, KeyModifiers.Shift);
        else input.PointerWheel(WheelPoint, 1, 0, KeyModifiers.None);
        Frame(100);
        Assert.Equal(15, State.OffsetX);
        Assert.Equal(0, State.OffsetY);
        Frame(100);
        Assert.Equal(30, State.OffsetX);
    }

    [Fact]
    public void Thumb_capture_stops_at_any_offset_and_dragging_never_animates_or_snaps_back()
    {
        harness.Scene.ScrollTo(Viewer, null, 100);
        Frame(51);
        var bar = harness.Scene.GetScrollBar(Viewer, false)!.Value;
        var point = new Point(bar.Thumb.X + bar.Thumb.Width / 2, bar.Thumb.Y + bar.Thumb.Height / 2);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Viewer, input.CapturedPointer);
        Frame(500);
        Assert.Equal(25.5f, State.OffsetY, 3);
        input.PointerMoved(new Point(point.X, point.Y + 5.25f), KeyModifiers.None);
        var dragged = State.OffsetY;
        Assert.True(dragged > 25.5f);
        input.PointerReleased(new Point(point.X, point.Y + 5.25f), PointerButton.Primary, KeyModifiers.None);
        Frame(500);
        Assert.Equal(dragged, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Theory]
    [InlineData("scroll-behavior: auto;")]
    [InlineData("scroll-duration: 0ms;")]
    public void Auto_and_zero_duration_apply_the_offset_immediately(string declaration)
    {
        SetStyle(declaration);
        Frame();
        harness.Scene.ScrollTo(Viewer, null, 100.5f);
        Assert.Equal(100.5f, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Reduced_motion_finishes_active_requests_and_disables_new_animations()
    {
        harness.Scene.ScrollTo(Viewer, null, 100);
        Frame(50);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Equal(100, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
        harness.Scene.ScrollBy(Viewer, 30);
        Assert.Equal(130, State.OffsetY);
    }

    [Fact]
    public void Style_changes_and_hidden_containers_do_not_leave_animation_timers_running()
    {
        harness.Scene.ScrollTo(Viewer, null, 100);
        Frame(50);
        SetStyle("scroll-behavior: auto;");
        Frame();
        Assert.Equal(100, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
        SetStyle();
        Frame();
        harness.Scene.ScrollTo(Viewer, null, 200);
        SetStyle("display: none;");
        Frame();
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Resize_clamps_pending_destinations_and_requests_beyond_the_limit_finish_cleanly()
    {
        harness.Scene.ScrollTo(Viewer, null, 5000);
        Frame(50);
        SetStyle(extra: "Content { height: 150px; }");
        Frame(150);
        Assert.Equal(30, State.OffsetY);
        Assert.False(harness.Scene.ScrollBy(Viewer, 40));
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Wheel_chains_to_the_parent_when_the_inner_pending_target_reaches_its_limit()
    {
        SetStyle("scroll-wheel-step: 50px;", "Content { overflow: auto; scroll-wheel-step: 10000px; } Detail { width: 1200px; height: 2000px; }");
        Frame();
        var inner = root.Single("Content");
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        var innerState = harness.Scene.GetScrollState(inner)!;
        Assert.Equal(innerState.MaxOffsetY, innerState.OffsetY);
        Assert.Equal(50, State.OffsetY);
    }

    [Fact]
    public void Global_defaults_inherit_and_component_overrides_can_select_any_fractional_distance()
    {
        SetStyle("scroll-behavior: inherit; scroll-duration: inherit;", "Page { scroll-behavior: smooth; scroll-duration: 400ms; scroll-wheel-step: 1.5 lines; line-height: 25px; }");
        Frame();
        input.PointerWheel(WheelPoint, -0.5f, KeyModifiers.None);
        Frame(200);
        Assert.Equal(9.375f, State.OffsetY, 3);
        Frame(200);
        Assert.Equal(18.75f, State.OffsetY, 3);
        SetStyle("scroll-wheel-step: 0.5px;");
        Frame();
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        Assert.Equal(19.25f, State.OffsetY, 3);
    }

    [Fact]
    public void Keyboard_pages_animate_but_home_cancels_a_pending_request_even_before_the_first_frame()
    {
        input.PointerMoved(WheelPoint, KeyModifiers.None);
        Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
        Assert.True(input.KeyDown("Home", KeyModifiers.None, false));
        Frame(200);
        Assert.Equal(0, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
        Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
        Frame(100);
        Assert.Equal(State.ViewportHeight * 0.45f, State.OffsetY);
        Frame(100);
        Assert.Equal(State.ViewportHeight * 0.9f, State.OffsetY);
    }

    [Fact]
    public void Scroll_events_follow_visible_offsets_on_animation_frames()
    {
        harness.Host.RunPending();
        var page = (ScrollBarPage)root.Component;
        var before = page.ScrollEvents;
        input.ScrollTo(Viewer, null, 100.5);
        Assert.Equal(before, page.ScrollEvents);
        Frame(50);
        harness.Host.RunPending();
        Assert.True(page.ScrollEvents > before);
        var midway = page.ScrollEvents;
        Assert.Equal(25.125f, State.OffsetY);
        Frame(150);
        harness.Host.RunPending();
        Assert.True(page.ScrollEvents > midway);
        Assert.Equal(100.5f, State.OffsetY);
    }

    [Fact]
    public void Custom_easing_changes_the_intermediate_position_and_stop_preserves_it()
    {
        SetStyle("scroll-timing-function: ease-out;");
        Frame();
        harness.Scene.ScrollTo(Viewer, null, 100);
        Frame(100);
        Assert.InRange(State.OffsetY, 60, 80);
        var stopped = State.OffsetY;
        harness.Scene.StopScrolling(Viewer);
        Frame(500);
        Assert.Equal(stopped, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Prevented_wheel_does_not_schedule_scroll_motion()
    {
        var page = (ScrollBarPage)root.Component;
        page.PreventWheel = true;
        page.Refresh();
        Frame();
        input.PointerWheel(WheelPoint, -1, KeyModifiers.None);
        Frame(200);
        Assert.Equal(0, State.OffsetY);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }
}
