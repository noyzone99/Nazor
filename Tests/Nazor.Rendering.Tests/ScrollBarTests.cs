using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ScrollBarPage : ComponentBase
{
    public int Clicks { get; private set; }
    public int ScrollEvents { get; private set; }
    public bool PreventWheel { get; set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.OpenElement(1, "Viewer");
        builder.AddAttribute(2, "onscroll", EventCallback.Factory.Create<ScrollEventArgs>(this, () => ScrollEvents++));
        builder.AddEventPreventDefaultAttribute(3, "onwheel", PreventWheel);
        builder.OpenElement(4, "Content");
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => Clicks++));
        builder.OpenElement(6, "Detail");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenElement(7, "Cover");
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class ScrollBarTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;

    public ScrollBarTests()
    {
        harness.Scene.Styles.Theme = Nazor.Controls.StyleSheets.Basic;
        SetStyle("");
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<ScrollBarPage>();
        harness.Render(300, 200, 1);
    }

    private ElementNode Viewer => root.Single("Viewer");
    private ScrollState State => harness.Scene.GetScrollState(Viewer)!;
    private ScrollBarGeometry Bar(bool horizontal = false) => harness.Scene.GetScrollBar(Viewer, horizontal)!.Value;
    private static Point Center(Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    private static StyleSheet Compile(string text)
    {
        var result = NssCompiler.Compile("scrollbar-tests", text);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private void SetStyle(string declarations, string extra = "", StyleSheet? theme = null)
    {
        var sheet = Compile("Page { width: 300px; height: 200px; } Viewer { width: 200px; height: 120px; overflow: auto; " +
            declarations + " } Content { width: 600px; height: 400px; } Cover { display: none; } " + extra);
        harness.Scene.Styles.Theme = theme is null ? sheet : StyleSheet.Combine(theme, sheet);
        harness.Scene.Update();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Thumb_drag_captures_pointer_clamps_and_does_not_click_content(bool horizontal)
    {
        var start = Center(Bar(horizontal).Thumb);
        input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Viewer, input.CapturedPointer);
        input.PointerMoved(new Point(900, 900), KeyModifiers.None);
        Assert.Equal(horizontal ? State.MaxOffsetX : State.MaxOffsetY, horizontal ? State.OffsetX : State.OffsetY);
        Assert.Equal(0, horizontal ? State.OffsetY : State.OffsetX);
        Assert.True(((ScrollBarPage)root.Component).ScrollEvents > 0);
        input.PointerMoved(new Point(-900, -900), KeyModifiers.None);
        Assert.Equal(0, horizontal ? State.OffsetX : State.OffsetY);
        input.PointerReleased(start, PointerButton.Primary, KeyModifiers.None);
        Assert.Null(input.CapturedPointer);
        Assert.Equal(0, ((ScrollBarPage)root.Component).Clicks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Track_click_pages_without_activating_content(bool horizontal)
    {
        var track = Bar(horizontal).Track;
        var point = horizontal ? new Point(track.Right - 1, track.Y + 1) : new Point(track.X + 1, track.Bottom - 1);
        Assert.Same(Viewer, harness.Scene.HitTest(point));
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(horizontal ? State.ViewportWidth : State.ViewportHeight, horizontal ? State.OffsetX : State.OffsetY);
        Assert.Equal(0, ((ScrollBarPage)root.Component).Clicks);
    }

    [Fact]
    public void Autohide_reveals_only_near_the_axis_and_stays_visible_during_drag()
    {
        SetStyle("scrollbar-visibility: autohide;");
        Assert.False(Bar().IsVisible);
        Assert.False(Bar(true).IsVisible);
        input.PointerMoved(new Point(100, 50), KeyModifiers.None);
        Assert.False(Bar().IsVisible);
        input.PointerMoved(new Point(190, 50), KeyModifiers.None);
        Assert.True(Bar().IsVisible);
        Assert.False(Bar(true).IsVisible);
        input.PointerPressed(Center(Bar().Thumb), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(new Point(350, 80), KeyModifiers.None);
        input.PointerLeft();
        Assert.True(Bar().IsVisible);
        input.PointerCanceled();
        Assert.Null(input.CapturedPointer);
        Assert.False(Bar().IsVisible);
        input.PointerMoved(new Point(100, 115), KeyModifiers.None);
        Assert.True(Bar(true).IsVisible);
        Assert.False(Bar().IsVisible);
        input.PointerLeft();
        Assert.False(Bar(true).IsVisible);
    }

    [Fact]
    public void None_hides_both_bars_preserving_wheel_and_programmatic_scrolling()
    {
        SetStyle("scrollbar-visibility: none;");
        Assert.Null(harness.Scene.GetScrollBar(Viewer, false));
        Assert.Null(harness.Scene.GetScrollBar(Viewer, true));
        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Assert.Equal(48, State.OffsetY);
        Assert.True(input.ScrollTo(Viewer, 80, 90));
        Assert.Equal(80, State.OffsetX);
        Assert.Equal(90, State.OffsetY);
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands, c => c is FillRectangle);
    }

    [Fact]
    public void Nss_colors_and_geometry_are_painted_and_tiny_tracks_keep_thumbs_inside()
    {
        SetStyle("scrollbar-width: 8px; scrollbar-inset: 3px; scrollbar-radius: 3px; scrollbar-thumb-color: red; scrollbar-track-color: blue;");
        var bar = Bar();
        Assert.Equal(8, bar.Thumb.Width);
        Assert.Equal(197, bar.Track.Right);
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Contains(commands, c => c is FillRectangle f && f.Rect == bar.Thumb && f.Color == new Nazor.Graphics.Color(255, 0, 0, 255));
        Assert.Contains(commands, c => c is FillRectangle f && f.Rect == bar.Track && f.Color == new Nazor.Graphics.Color(0, 0, 255, 255));
        SetStyle("scrollbar-min-thumb-length: 100px;", "Viewer { width: 18px; height: 18px; }");
        Assert.True(Bar().Thumb.Height <= Bar().Track.Height);
        Assert.True(Bar(true).Thumb.Width <= Bar(true).Track.Width);
        input.PointerPressed(Center(Bar().Thumb), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(new Point(200, 200), KeyModifiers.None);
        Assert.True(float.IsFinite(State.OffsetY));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Styled_thumb_expands_and_changes_color_without_moving_the_hit_lane(bool horizontal)
    {
        SetStyle("""
            scrollbar-placement: reserve; scrollbar-width: 12px; scrollbar-inset: 0;
            scrollbar-thumb-color: red; color: lime; scrollbar-thumb-hover-color: currentColor;
            scrollbar-thumb-active-color: var(--drag-color); scrollbar-track-hover-color: yellow;
            scrollbar-thumb-border-width: 1px; scrollbar-thumb-border-color: black;
            """, "Page { scrollbar-thumb-width: 4px; scrollbar-thumb-expanded-width: 30px; --drag-color: blue; }");
        var bar = Bar(horizontal);
        var other = Bar(!horizontal);
        var gutter = harness.Scene.GetLayoutNode(Viewer)!.Style.ScrollbarGutter;
        var compact = horizontal ? new Rect(bar.Thumb.X, bar.Thumb.Y + 4, bar.Thumb.Width, 4)
            : new Rect(bar.Thumb.X + 4, bar.Thumb.Y, 4, bar.Thumb.Height);
        var otherCompact = horizontal ? new Rect(other.Thumb.X + 4, other.Thumb.Y, 4, other.Thumb.Height)
            : new Rect(other.Thumb.X, other.Thumb.Y + 4, other.Thumb.Width, 4);
        var red = new Nazor.Graphics.Color(255, 0, 0, 255);
        var green = new Nazor.Graphics.Color(0, 255, 0, 255);
        var blue = new Nazor.Graphics.Color(0, 0, 255, 255);
        var yellow = new Nazor.Graphics.Color(255, 255, 0, 255);

        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>(), fill => fill.Rect == compact && fill.Color == red);
        var trackPoint = horizontal ? new Point(bar.Track.Right - 1, bar.Track.Y + 1) : new Point(bar.Track.X + 1, bar.Track.Bottom - 1);
        input.PointerMoved(trackPoint, KeyModifiers.None);
        AssertPaint(bar.Thumb, red);
        var thumbPoint = horizontal ? new Point(Center(bar.Thumb).X, bar.Thumb.Y + 1) : new Point(bar.Thumb.X + 1, Center(bar.Thumb).Y);
        input.PointerMoved(thumbPoint, KeyModifiers.None);
        AssertPaint(bar.Thumb, green);
        input.PointerPressed(thumbPoint, PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Viewer, input.CapturedPointer);
        input.PointerLeft();
        AssertPaint(bar.Thumb, blue);
        Assert.Equal(bar, Bar(horizontal));
        Assert.Equal(gutter, harness.Scene.GetLayoutNode(Viewer)!.Style.ScrollbarGutter);
        input.PointerCanceled();
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>(), fill => fill.Rect == compact && fill.Color == red);

        void AssertPaint(Rect thumb, Nazor.Graphics.Color fillColor)
        {
            var commands = harness.Scene.BuildDisplayList().Commands;
            Assert.Contains(commands.OfType<FillRectangle>(), fill => fill.Rect == thumb && fill.Color == fillColor);
            Assert.Contains(commands.OfType<FillRectangle>(), fill => fill.Rect == bar.Track && fill.Color == yellow);
            Assert.Contains(commands.OfType<FillRectangle>(), fill => fill.Rect == otherCompact && fill.Color == red);
            Assert.Contains(commands.OfType<DrawBorder>(), border => border.Rect == thumb && border.Widths.Left == 1);
        }
    }

    [Theory]
    [InlineData("0", "white", false)]
    [InlineData("0", "auto", true)]
    [InlineData("3px", "white", true)]
    public void Square_thumbs_can_use_a_beveled_outline(string radius, string highlight, bool uniform)
    {
        SetStyle($"scrollbar-width: 12px; scrollbar-radius: {radius}; scrollbar-thumb-color: red; scrollbar-thumb-border-width: 1px;" +
            $" scrollbar-thumb-border-color: black; scrollbar-thumb-border-highlight-color: {highlight};");
        var border = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawBorder>(), draw => draw.Rect == Bar().Thumb);
        Assert.Equal(uniform, border.IsUniform);
        Assert.Equal(new Nazor.Graphics.Color(0, 0, 0, 255), border.Right.Color);
        Assert.Equal(border.Right, border.Bottom);
        Assert.Equal(border.Top, border.Left);
        Assert.Equal(uniform ? border.Right.Color : new Nazor.Graphics.Color(255, 255, 255, 255), border.Top.Color);
    }

    [Theory]
    [InlineData("Basic", 4, 4, 4, false)]
    [InlineData("Fluent", 12, 4, 8, true)]
    [InlineData("NazorLeaf", 8, 5, 7, false)]
    public void Existing_themes_use_their_scrollbar_metrics_and_system_forced_colors(
        string name, float laneWidth, float restingWidth, float expandedWidth, bool hidesAtRest)
    {
        var theme = name switch
        {
            "Fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "NazorLeaf" => Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        SetStyle("", theme: theme);
        Assert.Equal(laneWidth, Bar().Thumb.Width);
        Assert.Equal(!hidesAtRest, Bar().IsVisible);

        input.PointerWheel(new Point(50, 50), -1, KeyModifiers.None);
        Assert.True(Bar().IsVisible);
        AssertThumbPaint(restingWidth);
        input.PointerMoved(Center(Bar().Thumb), KeyModifiers.None);
        AssertThumbPaint(expandedWidth);

        input.PointerLeft();
        var canvasText = Nazor.Style.Color.FromRgb(255, 255, 0);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Dark, ReducedMotion: true, ForcedColors: true,
            Colors: SystemColors.Default with { CanvasText = canvasText });
        harness.Scene.Update();
        var forced = Bar();
        Assert.Equal(6, forced.Thumb.Width);
        Assert.True(forced.IsVisible);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>(),
            fill => fill.Rect == forced.Thumb && fill.Color == new Nazor.Graphics.Color(255, 255, 0, 255));

        void AssertThumbPaint(float width)
        {
            var thumb = Bar().Thumb;
            var bounds = new Rect(thumb.X + (thumb.Width - width) / 2, thumb.Y, width, thumb.Height);
            Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>(), fill => fill.Rect == bounds);
        }
    }

    [Fact]
    public void Transformed_scrollbar_drag_uses_local_coordinates_and_keeps_grab_position()
    {
        SetStyle("transform: translate(20px, 10px) scale(1.2); transform-origin: 0 0;");
        var bar = Bar();
        var transform = harness.Scene.GetTransform(Viewer);
        var local = new Point(bar.Thumb.X + 1, bar.Thumb.Y + 2);
        input.PointerPressed(transform.Transform(local), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(transform.Transform(new Point(local.X, local.Y + 10)), KeyModifiers.None);
        var expected = 10 / (bar.Track.Height - bar.Thumb.Height) * State.MaxOffsetY;
        Assert.Equal(expected, State.OffsetY, 3);
        input.PointerCanceled();
        var previous = State.OffsetY;
        input.PointerMoved(new Point(100, 100), KeyModifiers.None);
        Assert.Equal(previous, State.OffsetY);
    }

    [Fact]
    public void Style_changes_cancel_drag_when_the_scrollbar_disappears()
    {
        input.PointerPressed(Center(Bar().Thumb), PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Viewer, input.CapturedPointer);
        SetStyle("scrollbar-visibility: none;");
        Assert.Null(input.CapturedPointer);
    }

    [Theory]
    [InlineData("scrollbar-width: 0;")]
    [InlineData("scrollbar-inset: 300px;")]
    public void Scrollbar_geometry_without_available_space_is_not_interactive(string declarations)
    {
        SetStyle(declarations);
        Assert.Null(harness.Scene.GetScrollBar(Viewer, false));
        Assert.Null(harness.Scene.GetScrollBar(Viewer, true));
        input.PointerPressed(new Point(195, 10), PointerButton.Primary, KeyModifiers.None);
        Assert.Null(input.CapturedPointer);
    }

    [Fact]
    public void Content_that_fits_has_no_scrollbars_even_with_visible_visibility()
    {
        SetStyle("scrollbar-visibility: visible;", "Content { width: 50px; height: 50px; }");
        Assert.False(State.CanScroll);
        Assert.Null(harness.Scene.GetScrollBar(Viewer, false));
        Assert.Null(harness.Scene.GetScrollBar(Viewer, true));
    }

    [Fact]
    public void Autohide_uses_the_release_position_even_without_a_final_move_event()
    {
        SetStyle("scrollbar-visibility: autohide;");
        input.PointerPressed(Center(Bar().Thumb), PointerButton.Primary, KeyModifiers.None);
        Assert.True(Bar().IsVisible);
        input.PointerReleased(new Point(280, 150), PointerButton.Primary, KeyModifiers.None);
        Assert.Null(input.CapturedPointer);
        Assert.False(Bar().IsVisible);
    }

    [Fact]
    public void Occluding_siblings_prevent_scrollbar_hover_and_drag()
    {
        SetStyle("scrollbar-visibility: autohide;", "Cover { display: block; position: absolute; left: 180px; top: 0; width: 20px; height: 120px; }");
        var point = Center(Bar().Thumb);
        Assert.Same(root.Single("Cover"), harness.Scene.HitTest(point));
        input.PointerMoved(point, KeyModifiers.None);
        Assert.False(Bar().IsVisible);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Assert.Null(input.CapturedPointer);
        Assert.Equal(0, State.OffsetY);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }
}
