using Microsoft.Extensions.DependencyInjection;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Tasks.Services;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TasksScrollbarsSampleTests : IDisposable
{
    private readonly ServiceProvider services;
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;

    public TasksScrollbarsSampleTests()
    {
        services = new ServiceCollection().AddSingleton<ITaskStore, InMemoryTaskStore>().AddSingleton<KeyBindings>().BuildServiceProvider();
        harness = new RenderHarness(services, clock);
        harness.Scene.Styles.Theme = Nazor.Controls.StyleSheets.Default;
        harness.Scene.Transitions.ReducedMotion = false;
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<Nazor.Samples.Tasks.App>();
        Frame();
    }

    private ElementNode[] Surfaces => [.. root.Elements("Container")
        .Where(e => e.Id is "inherited-preview" or "container-preview")];
    private ElementNode Configured => Surfaces.Single(e => e.Id == "container-preview");
    private ScrollState State(ElementNode surface) => harness.Scene.GetScrollState(surface)!;
    private static Point Center(Rect bounds) => new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    private Point ContentPoint(ElementNode surface)
    {
        var bounds = harness.Scene.GetBounds(surface);
        return new(bounds.X + 20, bounds.Y + 55);
    }

    private RenderedImage Frame(double milliseconds = 0, float width = 1080)
    {
        clock.Advance(milliseconds);
        harness.Render(width, 760, 1);
        harness.Host.RunPending();
        return harness.Render(width, 760, 1);
    }

    private async Task OpenPlayground()
    {
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Scrollbars"));
        Frame();
        Assert.Equal(2, Surfaces.Length);
    }

    private async Task Choose(string id, string label)
    {
        await harness.Host.ClickAsync(root.Elements("Select").Single(e => e.Id == id));
        Frame();
        await harness.Host.ClickAsync(root.Elements("SelectOption").Single(e => e.Text() == label));
        Frame();
    }

    private async Task Toggle(string id)
    {
        await harness.Host.ClickAsync(root.Elements("CheckBox").Single(e => e.Id == id));
        Frame();
    }

    [Fact]
    public async Task Both_containers_inherit_defaults_and_preview_data_does_not_modify_session_tasks()
    {
        var store = services.GetRequiredService<ITaskStore>();
        var count = store.Items.Count;
        await OpenPlayground();
        foreach (var surface in Surfaces)
        {
            Assert.True(State(surface).CanScrollX);
            Assert.True(State(surface).CanScrollY);
            var style = harness.Scene.GetStyle(surface);
            Assert.Equal(ScrollbarVisibility.AutoHide, style.Get(StyleProperties.ScrollbarVisibility));
            Assert.Equal(Length.Px(8), style.Get(StyleProperties.ScrollbarWidth));
            Assert.Equal(ScrollBehavior.Smooth, style.Get(StyleProperties.ScrollBehavior));
            Assert.Equal(Length.Px(24), style.Get(StyleProperties.ScrollWheelStep).Length);
        }
        Assert.Equal(count, store.Items.Count);
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Tasks"));
        Frame();
        Assert.Equal(count, store.Items.Count);
        Assert.DoesNotContain("Task 32", root.Text());
    }

    [Fact]
    public async Task Local_overrides_change_only_the_configured_container()
    {
        await OpenPlayground();
        await Choose("scrollbar-visibility", "Visible");
        var before = State(Configured).ViewportWidth;
        await Choose("scrollbar-placement", "Reserve space");
        await Toggle("custom-scrollbar");
        await Toggle("scrollbar-buttons");
        Frame(180);
        var inherited = Surfaces.Single(e => e.Id == "inherited-preview");
        var inheritedStyle = harness.Scene.GetStyle(inherited);
        Assert.Equal(ScrollbarVisibility.AutoHide, inheritedStyle.Get(StyleProperties.ScrollbarVisibility));
        Assert.Equal(ScrollbarPlacement.Overlay, inheritedStyle.Get(StyleProperties.ScrollbarPlacement));
        Assert.Equal(Length.Px(8), inheritedStyle.Get(StyleProperties.ScrollbarWidth));
        Assert.False(inheritedStyle.Get(StyleProperties.ScrollbarButtons));
        foreach (var surface in new[] { Configured })
        {
            Assert.True(State(surface).ViewportWidth < before);
            var bar = harness.Scene.GetScrollBar(surface, false)!.Value;
            Assert.Equal(14, bar.Thumb.Width);
            Assert.True(bar.DecreaseButton.Height > 0);
            Assert.True(bar.IncreaseButton.Height > 0);
            Assert.Equal(Length.Px(3), harness.Scene.GetStyle(surface).Get(StyleProperties.ScrollbarTrackRadius).Length);
        }
        await Choose("scrollbar-visibility", "Autohide");
        Assert.Equal(true, root.Elements("Select").Single(e => e.Id == "scrollbar-placement").Attributes["disabled"]);
        Assert.All(new[] { Configured }, e => Assert.Equal(ScrollbarPlacement.Overlay, harness.Scene.GetStyle(e).Get(StyleProperties.ScrollbarPlacement)));
        await Choose("scrollbar-visibility", "Visible");
        Assert.All(new[] { Configured }, e => Assert.Equal(ScrollbarPlacement.Reserve, harness.Scene.GetStyle(e).Get(StyleProperties.ScrollbarPlacement)));
    }

    [Fact]
    public async Task Autohide_fades_and_wheel_policy_temporarily_hides_the_bar()
    {
        await OpenPlayground();
        var surface = Configured;
        var bar = harness.Scene.GetScrollBar(surface, false)!.Value;
        Assert.Equal(0, bar.Opacity);
        input.PointerMoved(Center(bar.Bounds), KeyModifiers.None);
        Frame(60);
        Assert.InRange(harness.Scene.GetScrollBar(surface, false)!.Value.Opacity, 0.01, 0.99);
        Frame(60);
        Assert.Equal(1, harness.Scene.GetScrollBar(surface, false)!.Value.Opacity);
        input.PointerLeft();
        Frame(500);
        Assert.Equal(0, harness.Scene.GetScrollBar(surface, false)!.Value.Opacity);
        await Choose("scrollbar-visibility", "Visible");
        await Toggle("hide-on-wheel");
        input.PointerWheel(ContentPoint(surface), -1, KeyModifiers.None);
        Frame();
        Assert.Null(harness.Scene.GetScrollBar(surface, false));
        Frame(600);
        Assert.NotNull(harness.Scene.GetScrollBar(surface, false));
    }

    [Fact]
    public async Task Fractional_position_and_line_steps_do_not_snap_and_settings_preserve_the_surface()
    {
        await OpenPlayground();
        var surfaces = Surfaces;
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Go to x=25.5, y=40.25"));
        Frame(180);
        Assert.All(surfaces, e => Assert.Equal((25.5f, 40.25f), (State(e).OffsetX, State(e).OffsetY)));
        Assert.Contains("x 25.5 · y 40.25", root.Text());
        await Toggle("custom-scrollbar");
        await Choose("scroll-step", "1.5 lines per notch");
        Frame(180);
        Assert.Equal(surfaces, Surfaces);
        foreach (var surface in new[] { Configured })
        {
            input.PointerWheel(ContentPoint(surface), -0.5f, KeyModifiers.None);
            Frame(180);
            Assert.Equal(58.25f, State(surface).OffsetY, 3);
        }
    }

    [Fact]
    public async Task None_keeps_scrolling_zero_consumes_detents_and_fitting_content_removes_both_bars()
    {
        await OpenPlayground();
        await Choose("scrollbar-visibility", "None");
        await Toggle("smooth-scroll");
        await Choose("scroll-step", "24.5px per notch");
        foreach (var surface in new[] { Configured })
        {
            input.PointerWheel(ContentPoint(surface), -1, KeyModifiers.None);
            Assert.Equal(24.5f, State(surface).OffsetY);
            input.PointerWheel(ContentPoint(surface), -1, KeyModifiers.Shift);
            Assert.Equal(24.5f, State(surface).OffsetX);
            Assert.Null(harness.Scene.GetScrollBar(surface, false));
        }
        await Choose("scroll-step", "Zero (wheel disabled)");
        foreach (var surface in new[] { Configured })
        {
            input.PointerWheel(ContentPoint(surface), -1, KeyModifiers.None);
            Assert.Equal(24.5f, State(surface).OffsetY);
        }
        await Toggle("fitting-content");
        Assert.All(Surfaces, e => Assert.False(State(e).CanScroll));
        await Toggle("fitting-content");
        Assert.All(Surfaces, e => Assert.True(State(e).CanScrollX && State(e).CanScrollY));
    }

    [Fact]
    public async Task Held_end_button_repeats_and_releasing_it_stops_the_repeat()
    {
        await OpenPlayground();
        await Choose("scrollbar-visibility", "Visible");
        await Toggle("scrollbar-buttons");
        var surface = Configured;
        var point = Center(harness.Scene.GetScrollBar(surface, false)!.Value.IncreaseButton);
        input.PointerMoved(point, KeyModifiers.None);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Frame(180);
        Assert.Equal(36, State(surface).OffsetY, 3);
        Frame(400);
        Frame(180);
        Assert.True(State(surface).OffsetY > 36);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        Frame(180);
        var released = State(surface).OffsetY;
        Frame(500);
        Assert.Equal(released, State(surface).OffsetY);
        await Toggle("repeat-buttons");
        harness.Scene.ScrollTo(surface, 0, 0, immediate: true);
        Frame();
        input.PointerMoved(point, KeyModifiers.None);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        Frame(1000);
        Assert.Equal(36, State(surface).OffsetY, 3);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        Frame(500);
        Assert.Equal(36, State(surface).OffsetY, 3);
    }

    [Fact]
    public async Task Both_surfaces_support_direct_thumb_drag_track_paging_and_scroll_keys()
    {
        await OpenPlayground();
        await Choose("scrollbar-visibility", "Visible");
        await Choose("scroll-motion", "Instant (0ms)");
        foreach (var surface in Surfaces)
        {
            var bar = harness.Scene.GetScrollBar(surface, false)!.Value;
            var point = Center(bar.Thumb);
            input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
            Assert.Same(surface, input.CapturedPointer);
            input.PointerMoved(new Point(point.X, point.Y + 13.25f), KeyModifiers.None);
            var dragged = State(surface).OffsetY;
            Assert.True(dragged > 0);
            input.PointerReleased(new Point(point.X, point.Y + 13.25f), PointerButton.Primary, KeyModifiers.None);
            Frame(500);
            Assert.Equal(dragged, State(surface).OffsetY);
            bar = harness.Scene.GetScrollBar(surface, false)!.Value;
            var track = new Point(bar.Track.X + bar.Track.Width / 2, bar.Track.Bottom - 2);
            input.PointerPressed(track, PointerButton.Primary, KeyModifiers.None);
            input.PointerReleased(track, PointerButton.Primary, KeyModifiers.None);
            Frame(180);
            Assert.True(State(surface).OffsetY > dragged);
            await input.FocusAsync(surface);
            Assert.True(input.KeyDown("Home", KeyModifiers.None, false));
            Frame(180);
            Assert.Equal(0, State(surface).OffsetY);
            Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
            Frame(180);
            Assert.True(State(surface).OffsetY > 0);
            Assert.True(input.KeyDown("End", KeyModifiers.None, false));
            Frame(180);
            Assert.Equal(State(surface).MaxOffsetY, State(surface).OffsetY);
        }
    }

    [Fact]
    public async Task Wheel_animation_is_scoped_to_the_scrollbar_and_slow_and_instant_profiles_are_effective()
    {
        await OpenPlayground();
        await Toggle("wheel-animation");
        await Choose("scroll-motion", "Slow custom easing");
        var surface = Configured;
        var style = harness.Scene.GetStyle(surface);
        Assert.Equal(TimeSpan.FromMilliseconds(550), style.Get(StyleProperties.ScrollDuration));
        Assert.Equal(TimeSpan.FromMilliseconds(700), style.Get(StyleProperties.ScrollbarFadeOutDelay));
        input.PointerWheel(ContentPoint(surface), -1, KeyModifiers.None);
        Frame();
        Frame(300);
        Assert.InRange(harness.Scene.GetEffectiveValue(surface, StyleProperties.ScrollbarOpacity), 0.31, 0.99);
        Assert.Equal(1, harness.Scene.GetStyle(surface).Opacity);
        await Choose("scroll-motion", "Instant (0ms)");
        Assert.All(new[] { Configured }, e => Assert.Equal(TimeSpan.Zero, harness.Scene.GetStyle(e).Get(StyleProperties.ScrollDuration)));
    }

    [Fact]
    public async Task Restore_defaults_and_navigation_reset_demo_settings_without_touching_the_task_store()
    {
        await OpenPlayground();
        await Choose("scrollbar-visibility", "Visible");
        await Choose("scrollbar-placement", "Reserve space");
        await Toggle("custom-scrollbar");
        await Toggle("scrollbar-buttons");
        await Toggle("smooth-scroll");
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Restore defaults"));
        Frame(180);
        foreach (var surface in Surfaces)
        {
            var style = harness.Scene.GetStyle(surface);
            Assert.Equal(ScrollbarVisibility.AutoHide, style.Get(StyleProperties.ScrollbarVisibility));
            Assert.Equal(Length.Px(8), style.Get(StyleProperties.ScrollbarWidth));
            Assert.Equal(ScrollBehavior.Smooth, style.Get(StyleProperties.ScrollBehavior));
            Assert.False(style.Get(StyleProperties.ScrollbarButtons));
        }
        var store = services.GetRequiredService<ITaskStore>();
        store.Add("Keep the workspace task");
        await harness.Host.ClickAsync(root.Elements("Button").Single(e => e.Text() == "Tasks"));
        Frame();
        Assert.Contains("Keep the workspace task", root.Text());
        await OpenPlayground();
        Assert.Equal(4, store.Items.Count);
        Assert.All(Surfaces, e => Assert.Equal(ScrollbarVisibility.AutoHide, harness.Scene.GetStyle(e).Get(StyleProperties.ScrollbarVisibility)));
    }

    [Fact]
    public async Task Playground_renders_at_desktop_and_narrow_sizes()
    {
        await OpenPlayground();
        ImageAssert.MatchesBaseline(harness.Graphics, Frame(), "tasks-scrollbars-1080x760@1x");
        await Choose("scrollbar-visibility", "Visible");
        await Choose("scrollbar-placement", "Reserve space");
        await Toggle("custom-scrollbar");
        await Toggle("scrollbar-buttons");
        ImageAssert.MatchesBaseline(harness.Graphics, Frame(180), "tasks-scrollbars-custom-1080x760@1x");
        ImageAssert.MatchesBaseline(harness.Graphics, Frame(width: 680), "tasks-scrollbars-680x760@1x");
        Assert.All(Surfaces, e => Assert.InRange(harness.Scene.GetBounds(e).Width, 200, 640));
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
        services.Dispose();
    }
}
