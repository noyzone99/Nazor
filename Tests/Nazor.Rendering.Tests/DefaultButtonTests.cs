using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class DefaultButtonPage : ComponentBase
{
    public bool FlyoutOpen { get; set; }
    public List<string> Log { get; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(DefaultButtonPage));
        builder.AddComponentParameter(1, "Style", "display: flex; flex-direction: column; align-items: flex-start; gap: 8px; padding: 10px");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<TextField>(3, typeof(DefaultButtonPage));
            page.AddComponentParameter(4, "Class", "name");
            page.CloseComponent();
            Button(page, 10, "save", isDefault: true);
            Button(page, 20, "cancel", isDefault: false);
            page.OpenComponent<Nazor.Controls.Primitives.PopoverContainer>(30, typeof(DefaultButtonPage));
            page.AddComponentParameter(31, "IsOpen", FlyoutOpen);
            page.AddAttribute(32, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Altro")));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void Button(RenderTreeBuilder builder, int sequence, string name, bool isDefault)
    {
        builder.OpenComponent<Button>(sequence, typeof(DefaultButtonPage));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.AddComponentParameter(sequence + 2, "IsDefault", isDefault);
        builder.AddComponentParameter(sequence + 3, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add(name)));
        builder.AddAttribute(sequence + 4, "ChildContent", (RenderFragment)(b => b.AddContent(0, name)));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class DefaultButtonTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly DefaultButtonPage page;

    public DefaultButtonTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<DefaultButtonPage>();
        page = (DefaultButtonPage)root.Component;
        harness.Render(400, 300, 1);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Element(string name) => root.Elements().Single(e => e.Class == name);

    private void Enter()
    {
        input.KeyDown("Enter", KeyModifiers.None, false);
        input.KeyUp("Enter", KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(400, 300, 1);
    }

    [Fact]
    public void Enter_presses_the_default_button_unless_the_focused_control_uses_it_or_a_popup_is_open()
    {
        Enter();
        input.Focus(root.Elements("TextFieldInput").Single(), visible: true);
        Enter();
        Assert.Equal(["save", "save"], page.Log);
        input.Focus(Element("cancel"), visible: true);
        Enter();
        Assert.Equal(["save", "save", "cancel"], page.Log);

        page.FlyoutOpen = true;
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(400, 300, 1);
        input.Focus(root.Elements("TextFieldInput").Single(), visible: true);
        Enter();
        Assert.Equal(3, page.Log.Count);
    }

    [Fact]
    public void The_default_button_is_drawn_in_the_accent_color()
    {
        Assert.Equal(Style.SystemColors.Resolve().AccentColor, harness.Scene.GetStyle(Element("save")).BackgroundColor);
        Assert.Equal(Style.SystemColors.Resolve().AccentColorText, harness.Scene.GetStyle(Element("save")).Color);
        Assert.NotEqual(harness.Scene.GetStyle(Element("save")).BackgroundColor, harness.Scene.GetStyle(Element("cancel")).BackgroundColor);
    }
}
