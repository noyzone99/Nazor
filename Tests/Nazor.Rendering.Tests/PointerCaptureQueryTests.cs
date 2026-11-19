using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PointerCaptureQueryPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Target");
        builder.AddAttribute(1, "style", "width: 100px; height: 100px");
        builder.CloseElement();
        builder.OpenElement(2, "Other");
        builder.AddAttribute(3, "style", "width: 100px; height: 100px");
        builder.CloseElement();
    }
}

public sealed class PointerCaptureQueryTests
{
    [Fact]
    public void Capture_queries_follow_transfer_release_and_cancellation()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<PointerCaptureQueryPage>();
        harness.Render(300, 300, 1);
        var target = root.Single("Target");
        var other = root.Single("Other");
        Assert.False(input.HasPointerCapture(target));
        Assert.False(input.CapturePointer(target));
        input.PointerPressed(new Point(20, 20), PointerButton.Primary, KeyModifiers.None);
        Assert.True(input.CapturePointer(target));
        Assert.True(input.HasPointerCapture(target));
        Assert.False(input.HasPointerCapture(other));
        Assert.True(input.CapturePointer(other));
        Assert.False(input.HasPointerCapture(target));
        Assert.True(input.HasPointerCapture(other));
        input.ReleasePointerCapture(other);
        Assert.False(input.HasPointerCapture(other));
        Assert.True(input.CapturePointer(target));
        input.PointerCanceled();
        Assert.False(input.HasPointerCapture(target));
        Assert.False(input.HasPointerCapture(other));
    }
}
