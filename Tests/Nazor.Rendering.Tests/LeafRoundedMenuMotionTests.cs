using Nazor.Animation;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafRoundedMenuMotionTests
{
    [Fact]
    public void Menu_corner_morph_uses_reference_radii_and_retained_reversal_keeps_current_radii()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafMenuMotionPage>();
        var page = (LeafMenuMotionPage)root.Component;
        page.Lifetime = TimeSpan.FromSeconds(10); page.Refresh(); Frame();
        var menu = root.Single("Menu");
        Assert.Equal(Radius(16), Clip().TopLeftRadius);
        Assert.Equal(Radius(4), Clip().TopRightRadius);
        Assert.Equal(Radius(16), Clip().BottomRightRadius);
        Assert.Equal(Radius(4), Clip().BottomLeftRadius);
        clock.Advance(180); Frame();
        Assert.InRange(Clip().TopLeftRadius.X.Resolve(100), 16.01, 59.99);
        var opened = Clip();
        page.Open = false; page.Refresh(); Frame();
        Assert.Equal(opened, Clip());
        clock.Advance(50); Frame();
        var closing = Clip();
        page.Open = true; page.Refresh(); Frame();
        Assert.Same(menu, root.Single("Menu"));
        Assert.Equal(closing, Clip());
        clock.Advance(600); Frame();
        Assert.Equal(Radius(60), Clip().TopLeftRadius);
        Assert.Equal(Radius(60), Clip().BottomRightRadius);
        ClipPathValue Clip() => h.Scene.GetEffectiveValue(menu, StyleProperties.ClipPath);
        void Frame() { h.Host.RunPending(); h.Render(400, 300, 1); h.Host.RunPending(); h.Render(400, 300, 1); }
        static RadiusValue Radius(double value) => new(Length.Px(value), Length.Px(value));
    }
}
