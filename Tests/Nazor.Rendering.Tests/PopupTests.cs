using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PopupPage : ComponentBase
{
    private Button? opener;

    public bool Open { get; set; }
    public bool Modal { get; set; }
    public bool LightDismiss { get; set; } = true;
    public bool Anchored { get; set; } = true;
    public OverlayPlacement Placement { get; set; }
    public string PageClass { get; set; } = "page";
    public List<string> Log { get; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(PopupPage));
        builder.AddComponentParameter(1, "Class", PageClass);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<Button>(3, typeof(PopupPage));
            page.AddComponentParameter(4, "Class", "opener");
            page.AddComponentParameter(5, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Open = !Open));
            page.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(7, "Apri")));
            page.AddComponentReferenceCapture(8, reference => opener = (Button)reference);
            page.CloseComponent();
            page.OpenComponent<TextBlock>(9, typeof(PopupPage));
            page.AddComponentParameter(10, "Class", "after");
            page.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(12, "Sotto")));
            page.CloseComponent();
            page.OpenComponent(13,Modal ? typeof(DialogContainer) : typeof(PopoverContainer),typeof(PopupPage));
            page.AddComponentParameter(14, "IsOpen", Open);
            page.AddComponentParameter(15, "IsOpenChanged", EventCallback.Factory.Create<bool>(this, value => Open = value));
            if (!Modal)
            {
                page.AddComponentParameter(16, "Anchor", Anchored ? opener?.Element : null);
                page.AddComponentParameter(17, "Placement", Placement);
            }
            page.AddComponentParameter(18, "AccessibleName", "Options");
            if (!Modal) page.AddComponentParameter(181,"FocusOnOpen",false);
            page.AddComponentParameter(182,"CloseOnEscape",LightDismiss);
            page.AddComponentParameter(19, "CloseOnOutside", LightDismiss);
            page.AddComponentParameter(20, "OnDismiss", EventCallback.Factory.Create(this, () => Log.Add("dismiss")));
            page.AddAttribute(21, "ChildContent", (RenderFragment)(popup =>
            {
                Item(popup, 22, "first");
                Item(popup, 26, "second");
            }));
            page.CloseComponent();
            Item(page, 30, "outside");
        }));
        builder.CloseComponent();
    }

    private void Item(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Button>(sequence, typeof(PopupPage));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.AddComponentParameter(sequence + 2, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add(name)));
        builder.AddAttribute(sequence + 3, "ChildContent", (RenderFragment)(b => b.AddContent(0, name)));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class PopupTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly PopupPage page;

    public PopupTests()
    {
        var result = NssCompiler.Compile("PopupPage", """
            .page { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; padding: 10px; }
            .low { padding-top: 260px; }
            .right { align-items: flex-end; }
            PopoverContainer, DialogContainer { width: 100px; padding: 0; border: none; box-shadow: none; }
            Button { width: 60px; height: 20px; padding: 0; }
            .outside { margin-left: 150px; }
            .gap PopoverContainer { margin: 6px; }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(PopupPage), () => sheet);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<PopupPage>();
        page = (PopupPage)root.Component;
        Frame();
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private void Frame() => harness.Render(300, 300, 1);

    private void Change(Action<PopupPage> change)
    {
        change(page);
        page.Refresh();
        harness.Host.RunPending();
        Frame();
    }

    private ElementNode Element(string name) => root.Elements().Single(e => e.Class == name);

    private ElementNode? PopupElement => root.Elements().SingleOrDefault(element=>element.Name is "PopoverContainer" or "DialogContainer");

    private void Click(ElementNode element)
    {
        var bounds = harness.Scene.GetBounds(element);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        Frame();
    }

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        input.KeyDown(key, modifiers, false);
        input.KeyUp(key, modifiers);
        harness.Host.RunPending();
        Frame();
    }

    [Fact]
    public void An_open_popup_sits_below_its_anchor_above_everything_and_out_of_the_flow()
    {
        var after = harness.Scene.GetBounds(Element("after"));
        Click(Element("opener"));
        Assert.True(page.Open);
        var opener = harness.Scene.GetBounds(Element("opener"));
        var popup = harness.Scene.GetBounds(PopupElement!);
        Assert.Equal(new Rect(opener.X, opener.Bottom, 100, popup.Height), popup);
        Assert.Equal(after, harness.Scene.GetBounds(Element("after")));
        Assert.Same(PopupElement, harness.Scene.Overlays.Single());
        var first = harness.Scene.GetBounds(Element("first"));
        Assert.Same(Element("first"), harness.Scene.HitTest(new Point(first.X + 5, first.Y + 5)));
    }

    [Fact]
    public void The_margin_facing_the_anchor_separates_the_popup_on_either_side()
    {
        Change(p => (p.PageClass, p.Open) = ("page gap", true));
        var opener = harness.Scene.GetBounds(Element("opener"));
        var popup = harness.Scene.GetBounds(PopupElement!);
        Assert.Equal(opener.X, popup.X, 0.01f);
        Assert.Equal(opener.Bottom + 6, popup.Y, 0.01f);
        Change(p => p.PageClass = "page gap low");
        Assert.Equal(harness.Scene.GetBounds(Element("opener")).Y - 6, harness.Scene.GetBounds(PopupElement!).Bottom, 0.01f);
    }

    [Fact]
    public void A_popup_changes_side_without_room_and_stays_in_the_window()
    {
        Change(p => (p.PageClass, p.Open) = ("page low", true));
        Assert.Equal(harness.Scene.GetBounds(Element("opener")).Y, harness.Scene.GetBounds(PopupElement!).Bottom, 0.01f);
        Change(p => (p.PageClass, p.Placement) = ("page right", OverlayPlacement.Right));
        var opener = harness.Scene.GetBounds(Element("opener"));
        Assert.Equal(opener.X - 100, harness.Scene.GetBounds(PopupElement!).X, 0.01f);
        Change(p => p.Anchored = false);
        var centered = harness.Scene.GetBounds(PopupElement!);
        Assert.Equal((300 - 100) / 2f, centered.X, 0.01f);
        Assert.Equal((300 - centered.Height) / 2, centered.Y, 0.01f);
    }

    [Fact]
    public void A_click_inside_stays_and_a_click_outside_only_closes()
    {
        Click(Element("opener"));
        Click(Element("first"));
        Assert.True(page.Open);
        Assert.Equal(["first"], page.Log);
        Click(Element("outside"));
        Assert.False(page.Open);
        Assert.Null(PopupElement);
        Assert.Equal(["first", "dismiss"], page.Log);
        Assert.Empty(harness.Scene.Overlays);
    }

    [Fact]
    public void Clicking_the_anchor_of_an_open_popup_closes_it_without_reopening()
    {
        Click(Element("opener"));
        Click(Element("opener"));
        Assert.False(page.Open);
    }

    [Fact]
    public void Escape_closes_and_the_focus_stays_on_the_opener()
    {
        Key("Tab");
        Assert.Same(Element("opener"), input.Focused);
        Key("Enter");
        Assert.True(page.Open);
        Key("Escape");
        Assert.False(page.Open);
        Assert.Same(Element("opener"), input.Focused);
    }

    [Fact]
    public void A_modal_popup_takes_the_focus_keeps_tab_inside_and_gives_it_back()
    {
        Change(p => p.Modal = true);
        Key("Tab");
        Key("Enter");
        Assert.Same(Element("first"), input.Focused);
        Key("Tab");
        Assert.Same(Element("second"), input.Focused);
        Key("Tab");
        Assert.Same(Element("first"), input.Focused);
        Key("Escape");
        Assert.Null(PopupElement);
        Assert.Same(Element("opener"), input.Focused);
        Assert.True((Element("opener").States & ElementStates.FocusVisible) != 0);
    }

    [Fact]
    public void A_modal_popup_that_does_not_close_by_itself_blocks_the_rest()
    {
        Change(p => (p.Modal, p.LightDismiss, p.Open) = (true, false, true));
        Click(Element("outside"));
        Key("Escape");
        Assert.True(page.Open);
        Assert.Empty(page.Log);
    }
}
