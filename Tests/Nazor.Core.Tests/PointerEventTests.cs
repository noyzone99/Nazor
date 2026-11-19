using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Core.Tests;

public sealed class PointerEventTests
{
    [Fact]
    public async Task The_original_target_is_preserved_while_bubbling_and_forwarding_an_event()
    {
        using var host = new TestHost();
        var root = host.Render<PointerBoundary>();
        var fixture = (PointerBoundary)root.Component;
        var target = root.Single("Handle");
        var args = new PointerEventArgs { Button = PointerButton.Primary };

        await host.Renderer.DispatchEventAsync(target, "onpointerdown", args);

        Assert.Same(target, args.OriginalTarget);
        Assert.Same(target, fixture.SeenOrigin);
        await host.Renderer.DispatchEventAsync(root.Single("Parent"), "onpointerdown", args);
        Assert.Same(target, args.OriginalTarget);
        Assert.Same(target, fixture.SeenOrigin);
        Assert.Equal(2, fixture.ParentEvents);
    }

    private sealed class PointerBoundary : ComponentBase
    {
        public ElementNode? SeenOrigin { get; private set; }
        public int ParentEvents { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Parent");
            builder.AddAttribute(1, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, e => { SeenOrigin = e.OriginalTarget; ParentEvents++; }));
            builder.OpenElement(2, "Handle");
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
