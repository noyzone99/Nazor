using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafFeedbackMotionPage : ComponentBase
{
    [Parameter] public bool Tooltip { get; set; }
    public ToastQueue Queue { get; } = new();
    public TooltipContainer Tip { get; private set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Tooltip)
        {
            builder.OpenComponent<TooltipContainer>(0);
            builder.AddComponentParameter(1, nameof(TooltipContainer.As), "Tooltip");
            builder.AddComponentParameter(2, nameof(TooltipContainer.Trigger), (RenderFragment)(trigger =>
            {
                trigger.OpenComponent<Button>(0);
                trigger.AddComponentParameter(1, nameof(Button.Style), "margin: 32px;");
                trigger.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Help")));
                trigger.CloseComponent();
            }));
            builder.AddComponentParameter(3, nameof(TooltipContainer.ChildContent), (RenderFragment)(text => text.AddContent(0, "Description")));
            builder.AddComponentReferenceCapture(4, value => Tip = (TooltipContainer)value);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenComponent<ToastHost>(10);
            builder.AddComponentParameter(11, nameof(ToastHost.Queue), Queue);
            builder.AddComponentParameter(12, nameof(ToastHost.Placement), ToastPlacement.BottomCenter);
            builder.CloseComponent();
        }
    }
}

public sealed class LeafFeedbackMotionTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void Toasts_use_their_own_height_for_travel_and_separate_opacity_from_geometry_with_no_bottom_anchor_jump(int height)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        Application(false);
        var root = h.Host.Render<LeafFeedbackMotionPage>();
        var page = (LeafFeedbackMotionPage)root.Component;
        var entry = page.Queue.Show(new ToastOptions { Message = "Saved", Duration = null }); Frame();
        var toast = root.Single("Toast"); var items = root.Single("ItemsTransition");
        Assert.Equal(TimeSpan.FromMilliseconds(550), h.Scene.GetStyle(items).Get(StyleProperties.OverlayExitDuration));
        var raw = h.Scene.GetLayoutNode(toast)!.RoundedFrame;
        Assert.Equal(height, raw.Height);
        Assert.Equal(0, Opacity());
        Assert.Equal(raw.Y + raw.Height / 2 + 1.6f * raw.Height, CenterY(), 3);
        var animations = h.Scene.Transitions.GetAnimations(toast);
        Assert.Equal(TimeSpan.FromMilliseconds(200), Assert.Single(animations, animation => animation.Definition.Name == "leaf-toast-opacity").Definition.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(550), Assert.Single(animations, animation => animation.Definition.Name == "leaf-toast-travel").Definition.Duration);
        clock.Advance(200); Frame(); Assert.Equal(1, Opacity()); Assert.True(CenterY() > raw.Y + raw.Height / 2);
        clock.Advance(350); var full = Frame();
        Assert.Equal(raw.Y + raw.Height / 2, CenterY());
        Assert.NotEqual(h.Scene.Background, full.GetPixel((int)raw.X + 8, (int)(raw.Y + raw.Height / 2)));
        Application(true); Frame();
        Assert.True(page.Queue.Dismiss(entry)); Frame();
        Assert.Equal(raw, h.Scene.GetLayoutNode(toast)!.RoundedFrame);
        Assert.Equal(raw.Y + raw.Height / 2, CenterY());
        Assert.True(toast.IsDisabled); Assert.True(toast.ParentElement!.Attributes.GetValueOrDefault("exiting") is true);
        clock.Advance(200); Frame(); Assert.Equal(0, Opacity());
        clock.Advance(350); Frame(); Assert.Equal(raw.Y + raw.Height / 2 + 1.6f * raw.Height, CenterY(), 3);
        h.Scene.Transitions.ReducedMotion = true; Frame();
        Assert.True(toast.IsRemoved);
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
        double Opacity() => h.Scene.GetEffectiveValue(toast, StyleProperties.Opacity);
        float CenterY() { var box = h.Scene.GetBounds(toast); return box.Y + box.Height / 2; }
        RenderedImage Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); return h.Render(500, 400, 1); }
        void Application(bool retain)
        {
            var result = NssCompiler.Compile("toast-height", "Toast { min-height: 0; width: 240px; height: " + height + "px; }" + (retain ? "ToastHostRegion > ItemsTransition { overlay-exit-duration: 10s; }" : ""));
            Assert.True(result.Success, string.Join("\n", result.Diagnostics)); h.Scene.Styles.Application = result.StyleSheet;
        }
    }

    [Fact]
    public async Task Tooltip_opacity_and_travel_use_independent_phases_and_retained_reopen_preserves_the_current_values()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafFeedbackMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(LeafFeedbackMotionPage.Tooltip)] = true }));
        var page = (LeafFeedbackMotionPage)root.Component; Frame();
        await page.Tip.OpenAsync(); Frame();
        var tooltip = root.Single("Tooltip");
        Assert.Equal(TimeSpan.FromMilliseconds(300), h.Scene.GetStyle(tooltip).Get(StyleProperties.OverlayExitDuration));
        var animations = h.Scene.Transitions.GetAnimations(tooltip);
        Assert.Equal(TimeSpan.FromMilliseconds(150), Assert.Single(animations, animation => animation.Definition.Name == "leaf-tooltip-opacity").Definition.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.Single(animations, animation => animation.Definition.Name == "leaf-tooltip-travel").Definition.Duration);
        Assert.Equal(0, Opacity());
        clock.Advance(150); Frame(); Assert.Equal(1, Opacity()); Assert.NotEqual(h.Scene.GetStyle(tooltip).Get(StyleProperties.Transform), Transform());
        var retain = NssCompiler.Compile("tooltip-retention", "Tooltip { overlay-exit-duration: 10s; }");
        Assert.True(retain.Success); h.Scene.Styles.Application = retain.StyleSheet; Frame();
        var transform = Transform(); await page.Tip.CloseAsync(); Frame(); Assert.Equal(transform, Transform());
        clock.Advance(40); Frame(); var opacity = Opacity(); transform = Transform();
        await page.Tip.OpenAsync(); Frame();
        Assert.Same(tooltip, root.Single("Tooltip")); Assert.Equal(opacity, Opacity()); Assert.Equal(transform, Transform());
        clock.Advance(350); Frame(); Assert.Equal(1, Opacity());
        await page.Tip.CloseAsync(); Frame(); clock.Advance(150); Frame();
        Assert.Equal(0, Opacity());
        Assert.NotEqual(h.Scene.GetStyle(tooltip).Get(StyleProperties.Transform), Transform());
        clock.Advance(150); Frame(); Assert.Equal(h.Scene.GetStyle(tooltip).Get(StyleProperties.Transform), Transform());
        h.Scene.Transitions.ReducedMotion = true; Frame();
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "Tooltip");
        Assert.Equal(Display.None, h.Scene.GetStyle(root.Single("Tooltip")).Display);
        double Opacity() => h.Scene.GetEffectiveValue(tooltip, StyleProperties.Opacity);
        ValueList<TransformFunction> Transform() => h.Scene.GetEffectiveValue(tooltip, StyleProperties.Transform);
        void Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); h.Render(500, 400, 1); }
    }

    [Fact]
    public void Completing_the_last_real_toast_exit_removes_its_overlay_and_leaves_only_a_hidden_carrier()
    {
        using var h = new RenderHarness(); using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafFeedbackMotionPage>(); var page = (LeafFeedbackMotionPage)root.Component;
        var entry = page.Queue.Show(new ToastOptions { Message = "Saved", Duration = null }); Frame();
        var toast = root.Single("Toast");
        page.Queue.Dismiss(entry); Frame(); Assert.False(toast.IsRemoved);
        h.Host.RunUntil(() => root.Single("ToastHostRegion").Attributes.GetValueOrDefault("overlay") is false, timeoutMilliseconds: 2000);
        Frame(); Assert.True(toast.IsRemoved);
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
        Assert.Equal(Display.None, h.Scene.GetStyle(root.Single("ToastHostRegion")).Display);
        void Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); h.Render(500, 400, 1); }
    }
}
