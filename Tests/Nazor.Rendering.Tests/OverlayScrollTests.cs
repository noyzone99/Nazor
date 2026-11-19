using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class OverlayScrollPage : ComponentBase
{
    private Button? opener;

    [Parameter] public bool Modal { get; set; }
    [Parameter] public bool Scrollable { get; set; } = true;
    [Parameter] public bool Nested { get; set; }
    public bool Open { get; set; }
    public int BackgroundWheels { get; private set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "PageScroller");
        builder.AddAttribute(1, "style", "display: flex; flex-direction: column; width: 260px; height: 180px; overflow: auto");
        builder.AddAttribute(2, "onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, () => BackgroundWheels++));
        builder.OpenElement(3, "Before");
        builder.AddAttribute(4, "style", "height: 240px; flex-shrink: 0");
        builder.CloseElement();
        builder.OpenComponent<Button>(5);
        builder.AddComponentParameter(6, nameof(Button.Style), "width: 100px; height: 30px; flex-shrink: 0");
        builder.AddComponentParameter(7, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Open = true));
        builder.AddComponentParameter(8, nameof(Button.ChildContent), (RenderFragment)(b => b.AddContent(0, "Open")));
        builder.AddComponentReferenceCapture(9, component => opener = (Button)component);
        builder.CloseComponent();
        builder.OpenComponent(10, Modal ? typeof(DialogContainer) : typeof(PopoverContainer));
        builder.AddComponentParameter(11, nameof(OverlayContainer.IsOpen), Open);
        builder.AddComponentParameter(12, nameof(OverlayContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => Open = value));
        builder.AddComponentParameter(13, nameof(OverlayContainer.Style), "width: 180px; height: 120px; padding: 4px; display: flex; flex-direction: column; overflow: auto");
        if (!Modal) builder.AddComponentParameter(14, nameof(PopoverContainer.Anchor), opener?.Element);
        builder.AddComponentParameter(15, nameof(OverlayContainer.ChildContent), (RenderFragment)(content =>
        {
            if (Nested)
            {
                content.OpenElement(0, "InnerScroller");
                content.AddAttribute(1, "style", "height: 60px; overflow: auto; flex-shrink: 0; display: flex; flex-direction: column");
            }
            for (var i = 0; i < (Scrollable ? 12 : 1); i++)
            {
                content.OpenComponent<Button>(0);
                content.SetKey(i);
                content.AddComponentParameter(1, nameof(Button.Style), "height: 30px; flex-shrink: 0");
                content.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(b => b.AddContent(0, "Option")));
                content.CloseComponent();
            }
            if (Nested)
            {
                content.CloseElement();
                content.OpenElement(10, "PopupAfter");
                content.AddAttribute(11, "style", "height: 160px; flex-shrink: 0");
                content.CloseElement();
            }
        }));
        builder.CloseComponent();
        builder.OpenElement(16, "After");
        builder.AddAttribute(17, "style", "width: 520px; height: 500px; flex-shrink: 0");
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class OverlayScrollTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly OverlayScrollPage page;

    public OverlayScrollTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<OverlayScrollPage>();
        page = (OverlayScrollPage)root.Component;
        Frame();
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Background => root.Single("PageScroller");
    private ElementNode Surface => harness.Scene.Overlays.Single(element => element.Name is "PopoverContainer" or "DialogContainer");
    private void Frame()
    {
        harness.Host.RunPending();
        harness.Render(400, 300, 1);
        harness.Host.RunPending();
        harness.Render(400, 300, 1);
    }

    private void Open(bool modal, bool scrollable = true, bool nested = false)
    {
        harness.Scene.ScrollTo(Background, 40, 200, immediate: true);
        input.Focus(root.Single("Button"), visible: true);
        page.Modal = modal;
        page.Scrollable = scrollable;
        page.Nested = nested;
        page.Open = true;
        page.Refresh();
        Frame();
    }

    private void Wheel(Point point, float x = 0, float y = -1)
    {
        input.PointerWheel(point, x, y, KeyModifiers.None);
        Frame();
    }

    private Point Center(ElementNode element)
    {
        var bounds = harness.Scene.GetBounds(element);
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Opening_focusing_and_closing_a_surface_preserves_background_offsets(bool modal)
    {
        Open(modal);
        var background = harness.Scene.GetScrollState(Background)!;
        Assert.Equal(40, background.OffsetX);
        Assert.Equal(200, background.OffsetY);
        Assert.Same(root.Elements("Button").Skip(1).First(), input.Focused);
        var last = root.Elements("Button").Last();
        input.Focus(last, visible: true);
        input.BringIntoView(last);
        Frame();
        Assert.True(harness.Scene.GetScrollState(Surface)!.OffsetY > 0);
        Assert.Equal(40, background.OffsetX);
        Assert.Equal(200, background.OffsetY);
        input.BringIntoView(Surface);
        Frame();
        Assert.Equal(200, background.OffsetY);
        page.Open = false;
        page.Refresh();
        Frame();
        Assert.Same(root.Single("Button"), input.Focused);
        Assert.Equal(40, background.OffsetX);
        Assert.Equal(200, background.OffsetY);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Wheel_scrolls_the_surface_without_bubbling_or_chaining_into_its_page(bool modal)
    {
        Open(modal);
        var background = harness.Scene.GetScrollState(Background)!;
        var scroll = harness.Scene.GetScrollState(Surface)!;
        var point = Center(Surface);
        Wheel(point);
        Assert.True(scroll.OffsetY > 0);
        harness.Scene.ScrollTo(Surface, null, scroll.MaxOffsetY, immediate: true);
        Wheel(point);
        Wheel(point, x: 1, y: 0);
        Assert.Equal(40, background.OffsetX);
        Assert.Equal(200, background.OffsetY);
        Assert.Equal(0, page.BackgroundWheels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_surface_without_overflow_still_blocks_wheel_chaining(bool modal)
    {
        Open(modal, scrollable: false);
        Wheel(Center(Surface));
        Assert.Equal(0, harness.Scene.GetScrollState(Surface)!.MaxOffsetY);
        Assert.Equal(200, harness.Scene.GetScrollState(Background)!.OffsetY);
        Assert.Equal(0, page.BackgroundWheels);
    }

    [Fact]
    public void A_modal_surface_blocks_background_wheel_handlers_and_scrolling()
    {
        Open(modal: true);
        Wheel(new Point(20, 20));
        Assert.Equal(40, harness.Scene.GetScrollState(Background)!.OffsetX);
        Assert.Equal(200, harness.Scene.GetScrollState(Background)!.OffsetY);
        Assert.Equal(0, page.BackgroundWheels);
    }

    [Fact]
    public void A_nonmodal_surface_leaves_background_wheel_interaction_available_outside_it()
    {
        Open(modal: false);
        Wheel(new Point(20, 20));
        Assert.True(harness.Scene.GetScrollState(Background)!.OffsetY > 200);
        Assert.Equal(1, page.BackgroundWheels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Page_keys_in_a_short_surface_do_not_scroll_the_background(bool modal)
    {
        Open(modal, scrollable: false);
        foreach (var key in new[] { "PageDown", "End", "PageUp", "Home" })
        {
            input.KeyDown(key, KeyModifiers.None, false);
            input.KeyUp(key, KeyModifiers.None);
            Frame();
            Assert.Equal(40, harness.Scene.GetScrollState(Background)!.OffsetX);
            Assert.Equal(200, harness.Scene.GetScrollState(Background)!.OffsetY);
        }
    }

    [Fact]
    public void Nested_scroll_regions_can_chain_inside_the_surface_without_reaching_its_page()
    {
        Open(modal: true, nested: true);
        var inner = root.Single("InnerScroller");
        var innerScroll = harness.Scene.GetScrollState(inner)!;
        Wheel(Center(inner));
        Assert.True(innerScroll.OffsetY > 0);
        harness.Scene.ScrollTo(inner, null, innerScroll.MaxOffsetY, immediate: true);
        Wheel(Center(inner));
        Assert.True(harness.Scene.GetScrollState(Surface)!.OffsetY > 0);
        Assert.Equal(200, harness.Scene.GetScrollState(Background)!.OffsetY);
        Assert.Equal(0, page.BackgroundWheels);
    }
}
