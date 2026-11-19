using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class WheelEventTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Consuming_a_wheel_gesture_suppresses_later_handlers_and_default_actions(bool consumed)
    {
        using var host = new TestHost();
        var root = host.Render<WheelBoundary>();
        var fixture = (WheelBoundary)root.Component;
        var args = new WheelEventArgs { DeltaY = consumed ? 1 : 0 };

        await host.Renderer.DispatchEventAsync(root.Single("Handle"), "onwheel", args, out var prevented);

        Assert.Equal(consumed, prevented);
        Assert.Equal(consumed, args.Handled);
        Assert.Equal(consumed ? 0 : 1, fixture.LocalWheels);
        Assert.Equal(consumed ? 0 : 1, fixture.ParentWheels);
        Assert.Equal(consumed ? 0 : 1, fixture.DefaultWheels);
    }

    private sealed class WheelBoundary : ComponentBase
    {
        public int LocalWheels, ParentWheels, DefaultWheels;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Parent");
            builder.AddAttribute(1, "onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, _ => ParentWheels++));
            builder.OpenElement(2, "Handle");
            builder.AddAttribute(3, "__internal_onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, e => e.Handled = e.DeltaY != 0));
            builder.AddAttribute(4, "onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, _ => LocalWheels++));
            builder.AddAttribute(5, "__default_onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, _ => DefaultWheels++));
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
