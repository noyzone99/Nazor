using Nazor.Animation;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafDialogMotionPage : ComponentBase
{
    [Parameter] public string Surface { get; set; } = "DialogContainer";
    public bool Open { get; set; } = true;
    public TimeSpan? Lifetime { get; set; } = TimeSpan.FromSeconds(10);
    public string? Style { get; set; }
    public int Exits { get; private set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DialogContainer>(0);
        builder.AddComponentParameter(1, nameof(DialogContainer.As), Surface);
        builder.AddComponentParameter(2, nameof(DialogContainer.IsOpen), Open);
        builder.AddComponentParameter(3, nameof(DialogContainer.ExitDuration), Lifetime);
        builder.AddComponentParameter(4, nameof(DialogContainer.Style), "width: 240px; height: 160px;" + Style);
        builder.AddComponentParameter(5, nameof(DialogContainer.OnExited), EventCallback.Factory.Create(this, () => Exits++));
        builder.CloseComponent();
    }
}

public sealed class LeafDialogMotionTests
{
    [Theory]
    [InlineData("DialogContainer", false)]
    [InlineData("Dialog", false)]
    [InlineData("AlertDialog", false)]
    [InlineData("DialogContainer", true)]
    [InlineData("Dialog", true)]
    [InlineData("AlertDialog", true)]
    public void Corners_settle_with_reference_entry_and_exit_phases_or_apply_immediately_when_motion_is_reduced(string name, bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = reduced };
        var root = h.Host.Render<LeafDialogMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(LeafDialogMotionPage.Surface)] = name }));
        var page = (LeafDialogMotionPage)root.Component;
        Frame();
        var surface = root.Single(name);
        var style = h.Scene.GetStyle(surface);
        var finalRadius = style.Get(StyleProperties.BorderTopLeftRadius);
        if (!reduced)
        {
            Assert.Equal(TimeSpan.FromMilliseconds(180), style.Get(StyleProperties.OverlayExitDuration));
            Assert.Equal(Radius(60), Corner());
            Assert.Equal(Radius(8), h.Scene.GetEffectiveValue(surface, StyleProperties.BorderTopRightRadius));
            Assert.Equal(0, Opacity());
            clock.Advance(275); Frame();
            Assert.InRange(Corner().X.Resolve(240), finalRadius.X.Resolve(240), 59.99);
            Assert.InRange(Opacity(), 0.01, 0.99);
            clock.Advance(275); Frame();
        }
        Assert.Equal(finalRadius, Corner());
        Assert.Equal(1, Opacity());
        page.Open = false; page.Refresh(); Frame();
        if (reduced)
        {
            Assert.True(surface.IsRemoved);
            Assert.Equal(1, page.Exits);
            return;
        }
        Assert.Same(surface, root.Single(name));
        Assert.True(surface.IsDisabled);
        Assert.Equal(1, Opacity());
        clock.Advance(90); Frame();
        Assert.InRange(Opacity(), 0.01, 0.99);
        clock.Advance(90); Frame();
        Assert.Equal(0, Opacity());
        Assert.Equal(finalRadius, Corner());
        h.Scene.Transitions.ReducedMotion = true; Frame();
        Assert.True(surface.IsRemoved);
        Assert.Equal(1, page.Exits);
        RadiusValue Corner() => h.Scene.GetEffectiveValue(surface, StyleProperties.BorderTopLeftRadius);
        double Opacity() => h.Scene.GetEffectiveValue(surface, StyleProperties.Opacity);
        void Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); h.Render(500, 400, 1); }
    }

    [Fact]
    public void Closing_during_entry_and_reopening_retargets_visible_radii_and_keeps_the_callers_final_radius()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafDialogMotionPage>();
        var page = (LeafDialogMotionPage)root.Component;
        page.Style = "border-radius: 12px 2px 12px 2px;"; page.Refresh(); Frame();
        var surface = root.Single("DialogContainer");
        clock.Advance(140); Frame();
        var before = Corner();
        page.Open = false; page.Refresh(); Frame();
        Assert.Equal(before, Corner());
        clock.Advance(40); Frame();
        before = Corner();
        page.Open = true; page.Refresh(); Frame();
        Assert.Same(surface, root.Single("DialogContainer"));
        Assert.Equal(before, Corner());
        Assert.Equal(true, surface.Attributes["reentered"]);
        clock.Advance(600); Frame();
        Assert.Equal(Radius(12), Corner());
        RadiusValue Corner() => h.Scene.GetEffectiveValue(surface, StyleProperties.BorderTopLeftRadius);
        void Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); h.Render(500, 400, 1); }
    }

    [Fact]
    public void Scrim_keeps_its_independent_three_hundred_millisecond_entry_and_stays_full_until_the_body_is_removed()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafDialogMotionPage>();
        var page = (LeafDialogMotionPage)root.Component; Frame();
        var scrim = root.Single("DialogContainerScrim");
        Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.Single(h.Scene.Transitions.GetAnimations(scrim)).Definition.Duration);
        Assert.Equal(0, Opacity());
        clock.Advance(100); Frame();
        var current = Opacity();
        Assert.InRange(current, 0.01, 0.99);
        page.Open = false; page.Refresh(); Frame(); Assert.Equal(current, Opacity());
        clock.Advance(40); Frame(); current = Opacity();
        page.Open = true; page.Refresh(); Frame();
        Assert.Same(scrim, root.Single("DialogContainerScrim")); Assert.Equal(current, Opacity());
        clock.Advance(160); Frame(); Assert.Equal(1, Opacity());
        page.Open = false; page.Refresh(); Frame();
        clock.Advance(180); Frame();
        Assert.Equal(1, Opacity());
        Assert.Null(h.Scene.Transitions.Find(scrim, StyleProperties.Opacity));
        Assert.Equal(0, h.Scene.GetEffectiveValue(root.Single("DialogContainer"), StyleProperties.Opacity));
        page.Lifetime = TimeSpan.Zero; page.Refresh(); Frame();
        Assert.True(scrim.IsRemoved); Assert.Equal(1, page.Exits);
        double Opacity() => h.Scene.GetEffectiveValue(scrim, StyleProperties.Opacity);
        void Frame() { h.Host.RunPending(); h.Render(500, 400, 1); h.Host.RunPending(); h.Render(500, 400, 1); }
    }

    private static RadiusValue Radius(double value) => new(Length.Px(value), Length.Px(value));
}
