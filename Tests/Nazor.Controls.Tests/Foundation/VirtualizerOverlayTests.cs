using Nazor.Controls.Tests.Foundation.Fixtures;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class VirtualizerOverlayTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();

    [Fact]
    public void A_nonvirtualized_decoration_stays_mounted_during_variable_height_scrolling_and_empty_transitions()
    {
        h.Render<VirtualizerOverlayProbe>();
        var probe = h.Component<VirtualizerOverlayProbe>();
        var overlay = h.Root.Elements().Single(element => element.Id == "hover-overlay");
        Assert.Equal(true, overlay.Attributes["semantic-hidden"]);
        Assert.False(overlay.Attributes.ContainsKey("item-position"));
        Assert.True(probe.List.Element.ScrollTo(null, 1000)); h.Frame();
        Assert.True(probe.List.Range.First > 0);
        Assert.Same(overlay, h.Root.Elements().Single(element => element.Id == "hover-overlay"));
        Assert.InRange(h.All("VirtualizerItem").Count(), 1, 40);
        probe.Values = []; probe.Refresh(); h.Frame();
        Assert.Same(overlay, h.Root.Elements().Single(element => element.Id == "hover-overlay"));
        Assert.Empty(h.All("VirtualizerItem"));
        Assert.Equal(0, probe.List.Range.Count);
        Assert.Equal(0, h.Scene.GetScrollState(probe.List.Element.Element!)!.MaxOffsetY);
    }
}
