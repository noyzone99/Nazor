using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafMenuMotionPage : ComponentBase
{
    public double X { get; set; } = 20;
    public double Y { get; set; } = 20;
    public bool Open { get; set; } = true;
    public TimeSpan? Lifetime { get; set; }
    public int Exits { get; private set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<PopoverContainer>(0);
        builder.AddComponentParameter(1, nameof(PopoverContainer.As), "Menu");
        builder.AddComponentParameter(2, nameof(PopoverContainer.AnchorPoint), (X, Y));
        builder.AddComponentParameter(3, nameof(PopoverContainer.IsOpen), Open);
        builder.AddComponentParameter(4, nameof(PopoverContainer.FocusOnOpen), false);
        builder.AddComponentParameter(5, nameof(PopoverContainer.Style), "min-width: 0; min-height: 0; width: 100px; height: 90px; padding: 0");
        builder.AddComponentParameter(6, nameof(PopoverContainer.ExitDuration), Lifetime);
        builder.AddComponentParameter(7, nameof(PopoverContainer.OnExited), EventCallback.Factory.Create(this, () => Exits++));
        builder.CloseComponent();
    }
}

public sealed class LeafSubmenuDelayPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Menu>(0);
        builder.AddComponentParameter(1, nameof(Menu.IsOpen), true);
        builder.AddComponentParameter(2, nameof(Menu.AnchorPoint), (20d, 20d));
        builder.AddComponentParameter(3, nameof(Menu.ChildContent), (RenderFragment)(items =>
        {
            items.OpenComponent<Submenu>(0);
            items.AddComponentParameter(1, nameof(Submenu.ChildContent), (RenderFragment)(caption => caption.AddContent(0, "Parent")));
            items.AddComponentParameter(2, nameof(Submenu.Content), (RenderFragment)(children =>
            {
                children.OpenComponent<MenuItem>(0);
                children.AddComponentParameter(1, nameof(MenuItem.ChildContent), (RenderFragment)(caption => caption.AddContent(0, "Child")));
                children.CloseComponent();
            }));
            items.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class LeafMenuMotionTests
{
    [Fact]
    public void Actual_Leaf_submenu_style_supplies_sixty_milliseconds_and_reduced_motion_removes_the_delay()
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafSubmenuDelayPage>();
        h.Host.RunPending(); h.Render(400, 300, 1); h.Host.RunPending(); h.Render(400, 300, 1);
        var submenu = root.Single("Submenu");
        Assert.Equal(TimeSpan.FromMilliseconds(60), h.Scene.Styles.GetStyle(submenu).Get(StyleProperties.SubmenuOpenDelay));
        var motion = Assert.IsAssignableFrom<IOverlayMotion>(root.Single("Menu").GetFeature<IOverlayMotion>());
        Assert.Equal(TimeSpan.FromMilliseconds(60), motion.GetSubmenuOpenDelay(submenu));
        h.Scene.Transitions.ReducedMotion = true;
        Assert.Equal(TimeSpan.Zero, motion.GetSubmenuOpenDelay(submenu));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void Menu_clip_origins_follow_actual_pointer_clamping_and_native_configured_edges(bool native, bool right, bool bottom)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        if (native)
        {
            h.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 400, 300);
            h.Scene.ArrangeNativePopup = (_, request) => request.Bounds with { X = right ? 100 : 200, Y = bottom ? 50 : 140 };
        }
        var root = h.Host.Render<LeafMenuMotionPage>();
        var page = (LeafMenuMotionPage)root.Component;
        page.X = native ? 200 : right ? 380 : 20;
        page.Y = native ? 140 : bottom ? 280 : 20;
        page.Refresh();
        Frame();
        var menu = root.Single("Menu");
        Assert.Equal(right, menu.States.HasFlag(ElementStates.PopupOriginRight));
        Assert.Equal(bottom, menu.States.HasFlag(ElementStates.PopupOriginBottom));
        var style = h.Scene.Styles.GetStyle(menu);
        var clip = h.Scene.Transitions.GetValue(menu, StyleProperties.ClipPath, style.Get(StyleProperties.ClipPath));
        Assert.Equal(right ? 100 : 0, clip.Left.Resolve(100), 2);
        Assert.Equal(right ? 0 : 100, clip.Right.Resolve(100), 2);
        Assert.Equal(bottom ? 90 : 0, clip.Top.Resolve(90), 2);
        Assert.Equal(bottom ? 0 : 90, clip.Bottom.Resolve(90), 2);
        clock.Advance(120); Frame();
        Assert.Equal(1, h.Scene.Transitions.GetValue(menu, StyleProperties.Opacity, style.Opacity));
        var during = h.Scene.Transitions.GetValue(menu, StyleProperties.ClipPath, style.Get(StyleProperties.ClipPath));
        Assert.NotEqual(style.Get(StyleProperties.ClipPath), during);
        clock.Advance(400); Frame();
        Assert.Equal(style.Get(StyleProperties.ClipPath), h.Scene.Transitions.GetValue(menu, StyleProperties.ClipPath, style.Get(StyleProperties.ClipPath)));
        void Frame() { h.Host.RunPending(); h.Render(400, 300, 1); h.Host.RunPending(); h.Render(400, 300, 1); }
    }

    [Fact]
    public void Interrupted_entry_exit_and_retained_reopen_sample_current_properties_and_full_exit_resets_entry_metadata()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafMenuMotionPage>();
        var page = (LeafMenuMotionPage)root.Component;
        page.Lifetime = TimeSpan.FromSeconds(10); page.Refresh(); Frame();
        var menu = root.Single("Menu");
        Assert.Equal(TimeSpan.FromMilliseconds(220), h.Scene.Styles.GetStyle(menu).Get(StyleProperties.OverlayExitDuration));
        clock.Advance(160); Frame();
        var beforeClose = Clip();
        page.Open = false; page.Refresh(); Frame();
        Assert.Equal(beforeClose, Clip());
        Assert.True(menu.IsDisabled);
        clock.Advance(50); Frame();
        var beforeReopen = Clip();
        page.Open = true; page.Refresh(); Frame();
        Assert.Same(menu, root.Single("Menu"));
        Assert.Equal(true, menu.Attributes["reentered"]);
        Assert.Equal(beforeReopen, Clip());
        clock.Advance(600); Frame();
        Assert.Equal(h.Scene.Styles.GetStyle(menu).Get(StyleProperties.ClipPath), Clip());
        page.Lifetime = TimeSpan.Zero; page.Open = false; page.Refresh(); Frame();
        Assert.Empty(root.Elements("Menu"));
        Assert.Equal(1, page.Exits);
        page.Lifetime = TimeSpan.FromSeconds(10); page.Open = true; page.Refresh(); Frame();
        var fresh = root.Single("Menu");
        Assert.NotSame(menu, fresh);
        Assert.Equal(false, fresh.Attributes["reentered"]);
        ClipPathValue Clip() => h.Scene.Transitions.GetValue(menu, StyleProperties.ClipPath, h.Scene.Styles.GetStyle(menu).Get(StyleProperties.ClipPath));
        void Frame() { h.Host.RunPending(); h.Render(400, 300, 1); h.Host.RunPending(); h.Render(400, 300, 1); }
    }
}
