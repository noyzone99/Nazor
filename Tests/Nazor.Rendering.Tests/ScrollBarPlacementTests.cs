using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ScrollBarPlacementTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;

    public ScrollBarPlacementTests()
    {
        harness = new RenderHarness(clock: clock);
        SetStyle("");
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<ScrollBarPage>();
        Frame();
    }

    private ElementNode Viewer => root.Single("Viewer");
    private ScrollState State => harness.Scene.GetScrollState(Viewer)!;
    private ScrollBarGeometry Bar(bool horizontal = false) => harness.Scene.GetScrollBar(Viewer, horizontal)!.Value;
    private static Point Center(Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    private void SetStyle(string declarations, string extra = "")
    {
        var result = NssCompiler.Compile("scrollbar-placement", "Page { width: 300px; height: 200px; } " +
            "Viewer { width: 200px; height: 120px; overflow: auto; " + declarations +
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
    public void Overlay_remains_the_default_and_buttons_are_optional()
    {
        Assert.Equal(200, State.ViewportWidth);
        Assert.Equal(120, State.ViewportHeight);
        Assert.True(harness.Scene.GetLayoutNode(Viewer)!.Style.ScrollbarGutter.IsZero);
        Assert.True(Bar().DecreaseButton.IsEmpty);
        Assert.Equal(Bar().Track, Bar().Bounds);
    }

    [Fact]
    public void Reserved_bars_reduce_the_viewport_clip_content_and_do_not_grow_the_container()
    {
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 14px;");
        var commands = Frame().Commands;
        Assert.Equal(new Rect(0, 0, 200, 120), harness.Scene.GetBounds(Viewer));
        Assert.Equal(new Rect(0, 0, 182, 102), harness.Scene.GetContentBox(Viewer));
        Assert.Equal(182, State.ViewportWidth);
        Assert.Equal(102, State.ViewportHeight);
        Assert.Equal(418, State.MaxOffsetX);
        Assert.Equal(298, State.MaxOffsetY);
        Assert.Equal(102, Bar().Track.Bottom);
        Assert.Equal(182, Bar(true).Track.Right);
        Assert.Contains(commands, command => command is PushClip clip && clip.Rect == new Rect(0, 0, 182, 102));
        Assert.Same(Viewer, harness.Scene.HitTest(new Point(183, 40)));
        Frame();
        Assert.Equal(0, harness.Scene.LastUpdate.LayoutComputations);
    }

    [Fact]
    public void A_vertical_gutter_reflows_percent_width_content_without_creating_horizontal_overflow()
    {
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 14px;", "Content { width: 100%; }");
        Frame();
        Assert.True(State.CanScrollY);
        Assert.False(State.CanScrollX);
        Assert.Equal(182, harness.Scene.GetBounds(root.Single("Content")).Width);
        Assert.Equal(120, State.ViewportHeight);
    }

    [Theory]
    [InlineData("display: flex; flex-direction: column;")]
    [InlineData("display: grid; grid-template-columns: 1fr;")]
    public void Reserved_scrollbars_reduce_flex_and_grid_child_widths(string layout)
    {
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 14px; " + layout,
            "Content { width: auto; flex-shrink: 0; }");
        Frame();
        Assert.True(State.CanScrollY);
        Assert.False(State.CanScrollX);
        Assert.Equal(182, harness.Scene.GetBounds(root.Single("Content")).Width);
    }

    [Fact]
    public void Resizing_can_remove_a_reserved_gutter_and_animated_width_updates_its_size()
    {
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 14px;", "Content { width: 190px; }");
        Frame();
        harness.Scene.Viewport = new Size(800, 600);
        SetStyle("width: 700px; height: 500px; scrollbar-placement: reserve; scrollbar-width: 14px;", "Content { width: 190px; }");
        harness.Scene.Update();
        Assert.False(State.CanScroll);
        Assert.True(harness.Scene.GetLayoutNode(Viewer)!.Style.ScrollbarGutter.IsZero);
        SetStyle("scrollbar-placement: reserve; animation: bar-size 200ms linear both;",
            "@keyframes bar-size { from { scrollbar-width: 4px; } to { scrollbar-width: 12px; } }");
        harness.Scene.Transitions.ReducedMotion = false;
        Frame();
        Frame(100);
        Assert.Equal(8, Bar().Thumb.Width, 4);
        Assert.Equal(188, State.ViewportWidth, 4);
    }

    [Fact]
    public void Reserving_one_axis_can_create_overflow_and_a_gutter_on_the_other_axis()
    {
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 14px;", "Content { width: 190px; }");
        Frame();
        Assert.True(State.CanScrollX);
        Assert.True(State.CanScrollY);
        Assert.Equal(182, State.ViewportWidth);
        Assert.Equal(102, State.ViewportHeight);
    }

    [Theory]
    [InlineData("autohide")]
    [InlineData("none")]
    public void Autohide_and_none_never_reserve_space(string visibility)
    {
        SetStyle($"scrollbar-placement: reserve; scrollbar-width: 14px; scrollbar-visibility: {visibility};");
        Frame();
        Assert.Equal(200, State.ViewportWidth);
        Assert.Equal(120, State.ViewportHeight);
        Assert.True(harness.Scene.GetLayoutNode(Viewer)!.Style.ScrollbarGutter.IsZero);
    }

    [Fact]
    public void Gutters_disappear_when_content_fits_or_the_bar_has_zero_width()
    {
        SetStyle("scrollbar-placement: reserve;");
        Frame();
        Assert.True(State.ViewportWidth < 200);
        SetStyle("scrollbar-placement: reserve;", "Content { width: 50px; height: 50px; }");
        Frame();
        Assert.Equal(200, State.ViewportWidth);
        Assert.Equal(120, State.ViewportHeight);
        Assert.False(State.CanScroll);
        SetStyle("scrollbar-placement: reserve; scrollbar-width: 0;");
        Frame();
        Assert.Equal(200, State.ViewportWidth);
        Assert.Null(harness.Scene.GetScrollBar(Viewer, false));
    }

    [Fact]
    public void Content_box_sizing_keeps_the_authored_outer_size_with_reserved_scrollbars()
    {
        SetStyle("box-sizing: content-box; padding: 4px; border: 1px solid black; scrollbar-placement: reserve; scrollbar-width: 14px;");
        Frame();
        Assert.Equal(new Rect(0, 0, 210, 130), harness.Scene.GetBounds(Viewer));
        Assert.Equal(182, harness.Scene.GetContentBox(Viewer).Width);
        Assert.Equal(102, harness.Scene.GetContentBox(Viewer).Height);
    }

    [Fact]
    public void Absolute_children_are_positioned_inside_the_reduced_padding_box()
    {
        SetStyle("position: relative; scrollbar-placement: reserve; scrollbar-width: 14px;",
            "Detail { position: absolute; right: 0; top: 0; width: 10px; height: 10px; }");
        Frame();
        Assert.Equal(182, harness.Scene.GetBounds(root.Single("Detail")).Right);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void End_buttons_scroll_by_the_configured_step_without_clicking_content(bool horizontal)
    {
        SetStyle("scrollbar-buttons: visible; scrollbar-width: 14px; scrollbar-button-step: 25px;");
        Frame();
        var bar = Bar(horizontal);
        input.PointerPressed(Center(bar.IncreaseButton), PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Viewer, input.CapturedPointer);
        Assert.Equal(25, horizontal ? State.OffsetX : State.OffsetY);
        input.PointerReleased(Center(bar.IncreaseButton), PointerButton.Primary, KeyModifiers.None);
        Assert.Null(input.CapturedPointer);
        input.PointerPressed(Center(bar.DecreaseButton), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(Center(bar.DecreaseButton), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(0, horizontal ? State.OffsetX : State.OffsetY);
        Assert.Equal(0, ((ScrollBarPage)root.Component).Clicks);
        Assert.True(((ScrollBarPage)root.Component).ScrollEvents >= 2);
    }

    [Fact]
    public void Held_buttons_repeat_pause_outside_and_stop_on_release_or_cancellation()
    {
        SetStyle("scrollbar-buttons: visible; scrollbar-width: 14px; scrollbar-button-step: 25px;");
        Frame();
        var point = Center(Bar().IncreaseButton);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Frame(399);
        Assert.Equal(25, State.OffsetY);
        Frame(1);
        Assert.Equal(50, State.OffsetY);
        Frame(50);
        Assert.Equal(75, State.OffsetY);
        input.PointerMoved(new Point(50, 50), KeyModifiers.None);
        Frame(1000);
        Assert.Equal(75, State.OffsetY);
        input.PointerMoved(point, KeyModifiers.None);
        Frame();
        Assert.Equal(100, State.OffsetY);
        input.PointerCanceled();
        Frame(1000);
        Assert.Equal(100, State.OffsetY);
        Assert.Null(input.CapturedPointer);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Zero_repeat_interval_disables_repetition_and_hiding_buttons_releases_capture()
    {
        SetStyle("scrollbar-buttons: visible; scrollbar-width: 14px; scrollbar-button-repeat-interval: 0;");
        Frame();
        input.PointerPressed(Center(Bar().IncreaseButton), PointerButton.Primary, KeyModifiers.None);
        Frame(1000);
        Assert.Equal(40, State.OffsetY);
        SetStyle("scrollbar-buttons: none;");
        Frame();
        Assert.Null(input.CapturedPointer);
        Assert.Null(harness.Scene.TimeUntilNextFrame);
    }

    [Fact]
    public void Track_and_button_styles_are_independent_and_buttons_show_hover_pressed_and_disabled_colors()
    {
        SetStyle("""
            scrollbar-buttons: visible; scrollbar-width: 14px; scrollbar-radius: 1px; scrollbar-track-radius: 5px;
            scrollbar-track-color: blue; scrollbar-track-border-width: 1px; scrollbar-track-border-color: red;
            scrollbar-button-background: #111111; scrollbar-button-hover-background: green;
            scrollbar-button-active-background: red; scrollbar-button-disabled-color: blue;
            """);
        var commands = Frame().Commands;
        var bar = Bar();
        Assert.Contains(commands, c => c is FillRectangle f && f.Rect == bar.Track && f.Radius.TopLeft.X == 5);
        Assert.Contains(commands, c => c is DrawBorder b && b.Rect == bar.Track && b.Widths.Top == 1);
        Assert.Contains(commands, c => c is DrawPolyline p && p.Color == new Nazor.Graphics.Color(0, 0, 255, 255));
        input.PointerMoved(Center(bar.IncreaseButton), KeyModifiers.None);
        commands = Frame().Commands;
        Assert.Contains(commands, c => c is FillRectangle f && f.Rect == bar.IncreaseButton && f.Color == new Nazor.Graphics.Color(0, 128, 0, 255));
        input.PointerPressed(Center(bar.IncreaseButton), PointerButton.Primary, KeyModifiers.None);
        commands = Frame().Commands;
        Assert.Contains(commands, c => c is FillRectangle f && f.Rect == bar.IncreaseButton && f.Color == new Nazor.Graphics.Color(255, 0, 0, 255));
    }

    [Fact]
    public void Short_scrollbars_clamp_buttons_and_thumbs_without_overlap()
    {
        SetStyle("width: 18px; height: 18px; scrollbar-buttons: visible; scrollbar-button-length: 100px;");
        Frame();
        foreach (var horizontal in new[] { false, true })
        {
            var bar = Bar(horizontal);
            Assert.True(horizontal ? bar.DecreaseButton.Right <= bar.Track.X : bar.DecreaseButton.Bottom <= bar.Track.Y);
            Assert.True(horizontal ? bar.Track.Right <= bar.IncreaseButton.X : bar.Track.Bottom <= bar.IncreaseButton.Y);
            Assert.True(horizontal ? bar.Thumb.Width <= bar.Track.Width : bar.Thumb.Height <= bar.Track.Height);
        }
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }
}
