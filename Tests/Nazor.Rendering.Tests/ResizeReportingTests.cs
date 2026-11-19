using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ResizeReportingPage : ComponentBase
{
    public double Margin { get; set; }
    public List<ResizeEventArgs> Reports { get; } = [];
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Parent");
        builder.AddAttribute(1, "style", "padding: 3.25px;");
        builder.OpenElement(2, "Measured");
        builder.AddAttribute(3, "style", "width: 40.5px; height: 30.5px; border: 4px solid black; padding: 2.25px; transition: margin-top 100ms linear; margin-top: " + Margin.ToString(System.Globalization.CultureInfo.InvariantCulture) + "px;");
        builder.AddAttribute(4, "onresize", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Reports.Add(e)));
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class ResizeReportingTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Animated_fractional_margin_reports_raw_layout_size_and_offsets_without_pixel_snapping_jitter(float scale)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<ResizeReportingPage>();
        var page = (ResizeReportingPage)root.Component;
        Frame();
        var element = root.Single("Measured");
        var roundedHeights = new HashSet<float> { h.Scene.GetBounds(element).Height };
        page.Margin = 1.25; page.Refresh(); Frame();
        for (var frame = 0; frame < 10; frame++)
        {
            clock.Advance(10); Frame();
            roundedHeights.Add(h.Scene.GetBounds(element).Height);
        }
        Assert.True(page.Reports.Count > 1);
        Assert.All(page.Reports, report =>
        {
            Assert.Equal(40.5, report.Width);
            Assert.Equal(30.5, report.Height);
            Assert.Equal(28, report.ContentWidth);
            Assert.Equal(18, report.ContentHeight);
            Assert.Equal(3.25, report.OffsetLeft);
        });
        Assert.Equal(4.5, page.Reports[^1].OffsetTop);
        Assert.Equal(30.5, h.Scene.GetLayoutNode(element)!.AbsoluteFrame.Height);
        if (scale < 2) Assert.True(roundedHeights.Count > 1);
        void Frame() { h.Host.RunPending(); h.Render(120, 120, scale); h.Host.RunPending(); h.Render(120, 120, scale); }
    }
}
