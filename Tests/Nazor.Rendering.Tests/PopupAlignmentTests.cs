using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PopupAlignmentPage : ComponentBase
{
    public int Top { get; set; } = 240;
    public int Before { get; set; } = 60;
    public bool TargetHidden { get; set; }
    public bool ForeignTarget { get; set; }
    private ElementReference anchor;
    private ElementReference target;
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Anchor");
        builder.AddAttribute(1, "style", $"margin-top: {Top}px; margin-left: 40px; width: 180px; height: 32px;");
        builder.AddElementReferenceCapture(2, reference => anchor = reference);
        builder.CloseElement();
        builder.OpenComponent<PopoverContainer>(10);
        builder.AddComponentParameter(11, nameof(PopoverContainer.As), "AlignedPopup");
        builder.AddComponentParameter(12, nameof(PopoverContainer.IsOpen), true);
        builder.AddComponentParameter(13, nameof(PopoverContainer.Anchor), anchor);
        builder.AddComponentParameter(14, nameof(PopoverContainer.AlignmentTarget), ForeignTarget ? anchor : target);
        builder.AddComponentParameter(15, nameof(PopoverContainer.FocusOnOpen), false);
        builder.AddComponentParameter(16, nameof(PopoverContainer.Style), "display: flex; flex-direction: column; width: 200px; height: 200px; padding: 0; border: 0;");
        builder.AddComponentParameter(17, nameof(PopoverContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "Before");
            content.AddAttribute(1, "style", $"height: {Before}px; flex-shrink: 0;");
            content.CloseElement();
            content.OpenElement(10, "Selected");
            content.AddAttribute(11, "style", $"height: 40px; width: 100%; flex-shrink: 0; display: {(TargetHidden ? "none" : "block")};");
            content.AddElementReferenceCapture(12, reference => target = reference);
            content.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) StateHasChanged();
    }
}

public sealed class PopupAlignmentTests
{
    [Fact]
    public void A_retained_selected_part_aligns_with_the_anchor_and_follows_changed_geometry()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PopupAlignmentPage>();
        var page = (PopupAlignmentPage)root.Component;
        Frame();
        AssertAligned();
        page.Before = 100; page.Refresh(); Frame();
        AssertAligned();
        page.Top = 5; page.Refresh(); Frame();
        Assert.Equal(0, harness.Scene.GetBounds(root.Single("AlignedPopup")).Y);

        void Frame() { harness.Host.RunPending(); harness.Render(600, 600, 1); harness.Host.RunPending(); harness.Render(600, 600, 1); }
        void AssertAligned()
        {
            var anchor = harness.Scene.GetBounds(root.Single("Anchor"));
            var selected = harness.Scene.GetBounds(root.Single("Selected"));
            Assert.Equal(anchor.Y + anchor.Height / 2, selected.Y + selected.Height / 2, 1);
            Assert.Equal(anchor.X, harness.Scene.GetBounds(root.Single("AlignedPopup")).X);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Hidden_or_unrelated_targets_use_the_ordinary_side_placement(bool hidden)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PopupAlignmentPage>();
        var page = (PopupAlignmentPage)root.Component;
        page.TargetHidden = hidden; page.ForeignTarget = !hidden; page.Refresh();
        harness.Host.RunPending(); harness.Render(600, 600, 1); harness.Host.RunPending(); harness.Render(600, 600, 1);
        Assert.Equal(harness.Scene.GetBounds(root.Single("Anchor")).Bottom, harness.Scene.GetBounds(root.Single("AlignedPopup")).Y);
    }

    [Fact]
    public void Native_placement_receives_the_selected_part_offset_including_work_area_clamping()
    {
        using var harness = new RenderHarness();
        var calls = 0;
        harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 600, 600);
        harness.Scene.ArrangeNativePopup = (_, request) =>
        {
            calls++;
            Assert.Equal("bottom", request.Side);
            Assert.Equal(request.Bounds.Y - request.Anchor.Bottom, request.Offset, 1);
            return request.Bounds;
        };
        var root = harness.Host.Render<PopupAlignmentPage>();
        Frame();
        Assert.True(calls > 0);
        var anchor = harness.Scene.GetBounds(root.Single("Anchor"));
        var selected = harness.Scene.GetBounds(root.Single("Selected"));
        Assert.Equal(anchor.Y + anchor.Height / 2, selected.Y + selected.Height / 2, 1);
        var page = (PopupAlignmentPage)root.Component;
        page.Top = 5; page.Refresh(); Frame();
        Assert.Equal(0, harness.Scene.GetBounds(root.Single("AlignedPopup")).Y);

        void Frame() { harness.Host.RunPending(); harness.Render(600, 600, 1); harness.Host.RunPending(); harness.Render(600, 600, 1); }
    }
}
