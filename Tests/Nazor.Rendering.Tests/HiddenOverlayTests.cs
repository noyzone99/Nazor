using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class HiddenOverlayPage : ComponentBase
{
    private Button? opener;
    [Parameter] public bool Modal { get; set; }
    [Parameter] public bool AncestorHidden { get; set; }
    public bool Hidden { get; set; } = true;
    public int Actions { get; private set; }
    public int Dismissals { get; private set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Control.Id), "opener");
        builder.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Open")));
        builder.AddComponentReferenceCapture(3, component => opener = (Button)component);
        builder.CloseComponent();
        builder.OpenElement(10, "Owner");
        builder.AddAttribute(11, "style", Hidden && AncestorHidden ? "display: none" : null);
        builder.OpenComponent(20, Modal ? typeof(DialogContainer) : typeof(PopoverContainer));
        builder.AddComponentParameter(21, nameof(OverlayContainer.IsOpen), true);
        builder.AddComponentParameter(22, nameof(Control.Style), Hidden && !AncestorHidden ? "display: none" : "width: 120px; height: 70px");
        if (!Modal) builder.AddComponentParameter(23, nameof(PopoverContainer.Anchor), opener?.Element);
        builder.AddComponentParameter(24, nameof(OverlayContainer.OnDismiss), EventCallback.Factory.Create(this, () => Dismissals++));
        builder.AddComponentParameter(25, nameof(OverlayContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<Button>(0);
            content.AddComponentParameter(1, nameof(Control.Id), "inside");
            content.AddComponentParameter(2, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            content.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Action")));
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class HiddenOverlayTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Hidden_surfaces_are_inactive_and_can_be_shown_hidden_and_shown_again(bool modal, bool ancestorHidden)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var arrangements = 0;
        harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 400, 300);
        harness.Scene.ArrangeNativePopup = (_, request) => { arrangements++; return request.Bounds; };
        var root = harness.Host.Render<HiddenOverlayPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(HiddenOverlayPage.Modal)] = modal,
            [nameof(HiddenOverlayPage.AncestorHidden)] = ancestorHidden,
        }));
        var page = (HiddenOverlayPage)root.Component;
        Frame();
        var opener = root.Elements("Button").Single(element => element.Id == "opener");
        input.Focus(opener, visible: true);
        Assert.Empty(harness.Scene.Overlays);
        Assert.Equal(0, arrangements);
        Assert.Same(opener, input.Focused);
        Assert.False(input.Activate(root.Elements("Button").Single(element => element.Id == "inside")));
        input.KeyDown("Escape", KeyModifiers.None, false);
        Assert.Equal(0, page.Dismissals);

        SetHidden(false);
        Assert.Equal(modal ? 2 : 1, harness.Scene.Overlays.Count);
        Assert.Equal("inside", input.Focused?.Id);
        Assert.Equal(modal ? 0 : 1, Math.Sign(arrangements));
        var shown = arrangements;

        SetHidden(true);
        Assert.Empty(harness.Scene.Overlays);
        Assert.Equal(shown, arrangements);
        Assert.Same(opener, input.Focused);
        Assert.False(input.Activate(root.Elements("Button").Single(element => element.Id == "inside")));
        Assert.Equal(0, page.Actions);

        SetHidden(false);
        Assert.Equal(modal ? 2 : 1, harness.Scene.Overlays.Count);
        Assert.Equal("inside", input.Focused?.Id);

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(400, 300, 1);
            harness.Host.RunPending();
            harness.Render(400, 300, 1);
        }

        void SetHidden(bool hidden)
        {
            page.Hidden = hidden;
            page.Refresh();
            Frame();
        }
    }
}
