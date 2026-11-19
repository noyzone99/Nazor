using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class OverlayExitPage : ComponentBase
{
    private Button? opener;
    private TooltipContainer? tooltip;
    [Parameter] public string Kind { get; set; } = "primitive";
    public bool Open { get; private set; }
    public int Actions { get; private set; }
    public int Keys { get; private set; }
    public int Exits { get; private set; }
    public bool AnchorHidden { get; private set; }

    public void HideAnchor() { AnchorHidden = true; StateHasChanged(); }

    public async Task SetOpenAsync(bool value)
    {
        Open = value;
        if (tooltip is not null)
        {
            if (value) await tooltip.OpenAsync();
            else await tooltip.CloseAsync();
        }
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Owner");
        builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => Keys++));
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Control.Id), "opener");
        if (AnchorHidden) builder.AddComponentParameter(12, nameof(Control.Style), "display: none");
        if (Kind == "implicit-tooltip") builder.AddComponentParameter(13, nameof(Control.Tooltip), "Description");
        builder.AddComponentParameter(14, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
        builder.AddComponentParameter(15, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Open")));
        builder.AddComponentReferenceCapture(16, component => opener = (Button)component);
        builder.CloseComponent();
        if (Kind == "implicit-tooltip") { builder.CloseElement(); return; }
        var type = Kind switch
        {
            "dialog" => typeof(Dialog), "popover" => typeof(Popover), "select" => typeof(Select<string>),
            "combo" => typeof(Combobox<string>), "message" => typeof(InlineMessage), "tooltip" => typeof(TooltipContainer),
            "native" => typeof(PopoverContainer), _ => typeof(DialogContainer),
        };
        builder.OpenComponent(20, type);
        if (Kind != "tooltip") builder.AddComponentParameter(21, "IsOpen", Open);
        if (Kind is "select" or "combo") builder.AddComponentParameter(22, "Items", new[] { "One", "Two" });
        else if (Kind == "tooltip")
        {
            builder.AddComponentParameter(23, nameof(TooltipContainer.Trigger), (RenderFragment)(text => text.AddContent(0, "Tip")));
            builder.AddComponentReferenceCapture(24, component => tooltip = (TooltipContainer)component);
        }
        else if (Kind is "native" or "popover") builder.AddComponentParameter(25, "Anchor", opener?.Element);
        if (Kind is "primitive" or "native")
            builder.AddComponentParameter(26, nameof(OverlayContainer.OnExited), EventCallback.Factory.Create(this, () => Exits++));
        if (Kind is not ("select" or "combo"))
            builder.AddComponentParameter(27, "ChildContent", (RenderFragment)(content =>
            {
                if (Kind == "tooltip") { content.AddContent(0, "Description"); return; }
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

public sealed class OverlayExitTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private RootComponent root = null!;
    private OverlayExitPage Page => (OverlayExitPage)root.Component;
    private ElementNode Opener => root.Elements("Button").Single(element => element.Id == "opener");
    private ElementNode Surface => harness.Scene.Overlays.Single(element => !element.Name.EndsWith("Scrim", StringComparison.Ordinal));

    public OverlayExitTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene) { TooltipDelay = TimeSpan.Zero };
        harness.Scene.Viewport = new Size(400, 300);
        harness.Scene.Transitions.ReducedMotion = false;
        SetDuration(5000);
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void SetDuration(int milliseconds)
    {
        var compiled = NssCompiler.Compile("retention", $$"""
            DialogContainer, Dialog, PopoverContainer, Popover, SelectPopup, ComboboxPopup, InlineMessagePresence, TooltipContainer, Tooltip {
                overlay-exit-duration: {{milliseconds}}ms;
            }
            [exiting=true] { opacity: 0.35; }
            """);
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        harness.Scene.Styles.Application = compiled.StyleSheet;
    }

    private void Frame()
    {
        for (var frame = 0; frame < 3; frame++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private async Task ShowAsync(string kind)
    {
        root = harness.Host.Render<OverlayExitPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(OverlayExitPage.Kind)] = kind }));
        Frame();
        input.Focus(Opener, visible: true);
        if (kind != "implicit-tooltip") await Page.SetOpenAsync(true);
        Frame();
    }

    private async Task CloseAsync()
    {
        await Page.SetOpenAsync(false);
        Frame();
    }

    [Theory]
    [InlineData("primitive")]
    [InlineData("dialog")]
    [InlineData("popover")]
    [InlineData("select")]
    [InlineData("combo")]
    public async Task Closing_retains_the_real_surface_and_prevents_content_activation(string kind)
    {
        await ShowAsync(kind);
        var surface = Surface;
        await CloseAsync();
        Assert.Same(surface, Surface);
        Assert.True(surface.Attributes.GetValueOrDefault("exiting") is true);
        Assert.True(surface.IsDisabled);
        Assert.True(surface.Attributes.GetValueOrDefault("semantic-hidden") is true);
        var action = Descendants(surface).First(element => element.Attributes.GetValueOrDefault("role") is "button" or "option");
        Assert.False(input.Activate(action));
        Assert.Equal(0, Page.Actions);
    }

    [Fact]
    public async Task An_exiting_modal_blocks_window_keyboard_actions_and_restores_its_original_opener_after_reopening()
    {
        await ShowAsync("primitive");
        var surface = Surface;
        Assert.Equal("inside", input.Focused?.Id);
        await CloseAsync();
        Assert.NotSame(Opener, input.Focused);
        var keys = Page.Keys;
        Assert.True(input.KeyDown("Enter", KeyModifiers.None, false));
        Assert.True(input.KeyDown("Tab", KeyModifiers.None, false));
        Assert.True(input.KeyUp("Enter", KeyModifiers.None));
        Assert.Equal(keys, Page.Keys);
        Assert.False(input.Activate(Opener));
        await Page.SetOpenAsync(true);
        Frame();
        Assert.Same(surface, Surface);
        Assert.Equal("inside", input.Focused?.Id);
        Assert.False(surface.IsDisabled);
        Assert.False(surface.Attributes.GetValueOrDefault("exiting") is true);
        Assert.Equal(0, Page.Exits);
        await CloseAsync();
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        Assert.Same(Opener, input.Focused);
        Assert.Equal(1, Page.Exits);
        Assert.Null(surface.GetFeature<IOverlayMotion>());
    }

    [Fact]
    public async Task Exit_completion_releases_the_surface_focus_and_motion_provider_once()
    {
        SetDuration(250);
        await ShowAsync("primitive");
        var surface = Surface;
        await CloseAsync();
        Assert.Contains(surface, harness.Scene.Overlays);
        harness.Host.RunUntil(() => surface.IsRemoved, timeoutMilliseconds: 2000);
        Frame();
        Assert.True(surface.IsRemoved);
        Assert.Empty(harness.Scene.Overlays);
        Assert.Same(Opener, input.Focused);
        Assert.Equal(1, Page.Exits);
        Assert.Null(surface.GetFeature<IOverlayMotion>());
    }

    [Fact]
    public async Task A_native_popup_remains_arrangeable_until_exit_finishes()
    {
        var arrangements = 0;
        harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 400, 300);
        harness.Scene.ArrangeNativePopup = (_, request) => { arrangements++; return request.Bounds; };
        await ShowAsync("native");
        var surface = Surface;
        var before = arrangements;
        await CloseAsync();
        Assert.Same(surface, Surface);
        Assert.True(arrangements > before);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        before = arrangements;
        Frame();
        Assert.Equal(before, arrangements);
    }

    [Fact]
    public async Task Changing_host_media_preferences_ends_an_active_exit_immediately()
    {
        await ShowAsync("primitive");
        await CloseAsync();
        Assert.True(Surface.Attributes.GetValueOrDefault("exiting") is true);
        harness.Scene.Styles.Preferences = harness.Scene.Styles.Preferences with { ReducedMotion = true };
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        Assert.Same(Opener, input.Focused);
        Assert.Equal(1, Page.Exits);
    }

    [Fact]
    public async Task An_exiting_native_popup_does_not_reappear_in_the_window_after_its_anchor_is_hidden()
    {
        var arrangements = 0;
        harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 400, 300);
        harness.Scene.ArrangeNativePopup = (_, request) => { arrangements++; return request.Bounds; };
        await ShowAsync("native");
        await CloseAsync();
        var surface = Surface;
        var before = arrangements;
        Page.HideAnchor();
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        Assert.False(harness.Scene.IsRendered(surface));
        Assert.Equal(before, arrangements);
    }

    [Fact]
    public async Task Inline_messages_retain_their_layout_but_hide_their_closed_actions()
    {
        await ShowAsync("message");
        var message = root.Single("InlineMessage");
        await CloseAsync();
        Assert.Same(message, root.Single("InlineMessage"));
        Assert.True(message.Attributes.GetValueOrDefault("exiting") is true);
        Assert.False(input.Activate(root.Elements("Button").Single(element => element.Id == "inside")));
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Empty(root.Elements("InlineMessage"));
    }

    [Fact]
    public async Task Tooltip_containers_retain_the_exit_then_keep_a_hidden_description()
    {
        await ShowAsync("tooltip");
        var surface = Surface;
        await CloseAsync();
        Assert.Same(surface, Surface);
        Assert.True(surface.Attributes.GetValueOrDefault("exiting") is true);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        var description = root.Single("TooltipContainer");
        Assert.Equal(surface.Id, description.Id);
        Assert.False(harness.Scene.IsRendered(description));
    }

    [Fact]
    public async Task Implicit_tooltips_reuse_the_same_exit_and_reduce_motion_while_closing()
    {
        await ShowAsync("implicit-tooltip");
        var surface = Surface;
        Assert.Equal("Tooltip", surface.Name);
        input.Focus(null, visible: false);
        Frame();
        Assert.Same(surface, Surface);
        Assert.True(surface.Attributes.GetValueOrDefault("exiting") is true);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Empty(harness.Scene.Overlays);
        Assert.True(surface.IsRemoved);
    }

    private static IEnumerable<ElementNode> Descendants(ElementNode parent)
    {
        foreach (var child in parent.Children.OfType<ElementNode>())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
