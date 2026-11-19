using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class RoundedClipPage : ComponentBase
{
    public string Style { get; set; } = "clip-path: inset(10px 20px round 20px / 10px);";
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Viewport");
        builder.AddAttribute(1, "style", "width: 180px; height: 140px; background: white; overflow-y: auto;");
        builder.OpenElement(2, "Content");
        builder.AddAttribute(3, "style", "position: relative; height: 240px;");
        builder.OpenElement(4, "Rounded");
        builder.AddAttribute(5, "style", "position: absolute; left: 32px; top: 72px; width: 100px; height: 60px; background: red;" + Style);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class RoundedClipTests
{
    [Theory]
    [InlineData(1f, false, false)]
    [InlineData(1.25f, true, false)]
    [InlineData(1.5f, false, true)]
    [InlineData(2f, true, true)]
    public void Elliptical_clips_paint_and_hit_the_same_curve_after_scroll_and_inverse_transforms(float scale, bool scrolled, bool transformed)
    {
        using var h = new RenderHarness();
        var root = h.Host.Render<RoundedClipPage>();
        var page = (RoundedClipPage)root.Component;
        if (transformed) { page.Style += "transform: translate(10px, 5px);"; page.Refresh(); }
        h.Host.RunPending();
        h.Render(180, 140, scale);
        if (scrolled) Assert.True(h.Scene.ScrollTo(root.Single("Viewport"), null, 40, immediate: true));
        var image = h.Render(180, 140, scale);
        var element = root.Single("Rounded");
        var bounds = h.Scene.GetBounds(element);
        Assert.Equal(new Size(100, 60), bounds.Size);
        var corner = new Point(bounds.X + 21, bounds.Y + 11);
        var interior = new Point(bounds.X + 40, bounds.Y + 12);
        Assert.NotSame(element, h.Scene.HitTest(corner));
        Assert.Same(element, h.Scene.HitTest(interior));
        Assert.Equal(Color.White, Pixel(corner));
        Assert.Equal(new Color(255, 0, 0, 255), Pixel(interior));
        var clip = Assert.Single(h.Scene.BuildDisplayList().Commands, command => command is PushClip c && c.Radius == new CornerRadius(20, 10));
        Assert.Equal(new Size(60, 40), ((PushClip)clip).Rect.Size);
        Color Pixel(Point point) => image.GetPixel((int)(point.X * scale), (int)(point.Y * scale));
    }

    [Theory]
    [InlineData("inset(10px 20px round 20% / 10%)", 60f, 20f, 6f)]
    [InlineData("inset(10px 25px round 100px / 50px)", 50f, 25f, 12.5f)]
    public void Percentages_use_the_original_border_box_and_oversized_radii_shrink_together(string value, float width, float x, float y)
    {
        using var h = new RenderHarness();
        var root = h.Host.Render<RoundedClipPage>();
        var page = (RoundedClipPage)root.Component;
        page.Style = "clip-path: " + value; page.Refresh(); h.Host.RunPending();
        h.Render(180, 140, 1);
        Assert.Contains(h.Scene.BuildDisplayList().Commands, command => command is PushClip c && c.Rect.Size == new Size(width, 40) && c.Radius == new CornerRadius(x, y));
    }

    [Fact]
    public void Interrupted_radius_and_inset_transitions_retarget_from_current_geometry_without_layout_work()
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<RoundedClipPage>();
        var page = (RoundedClipPage)root.Component;
        Show("inset(0 round 0)");
        Show("inset(10px 20px round 20px / 10px)");
        clock.Advance(100); h.Render(180, 140, 1);
        var halfway = Clip();
        Assert.Equal(5, halfway.Top.Resolve(60));
        Assert.Equal(new RadiusValue(Length.Px(10), Length.Px(5)), halfway.TopLeftRadius);
        Show("inset(0 round 0)");
        Assert.Equal(halfway, Clip());
        clock.Advance(25); h.Render(180, 140, 1);
        Assert.InRange(Clip().TopLeftRadius.X.Resolve(100), 0, 10);
        Show("inset(10px 20px round 20px / 10px)");
        clock.Advance(250); h.Render(180, 140, 1);
        Assert.Equal(new RadiusValue(Length.Px(20), Length.Px(10)), Clip().TopLeftRadius);
        Assert.Equal(0, h.Scene.LastUpdate.LayoutComputations);

        ClipPathValue Clip() => h.Scene.GetEffectiveValue(root.Single("Rounded"), StyleProperties.ClipPath);
        void Show(string clip)
        {
            page.Style = "transition: clip-path 200ms linear; clip-path: " + clip;
            page.Refresh(); h.Host.RunPending(); h.Render(180, 140, 1);
        }
    }
}
