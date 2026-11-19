using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class CollapsibleMotionPage : ComponentBase
{
    private Button? opener;
    [Parameter] public bool Expander { get; set; }
    [Parameter] public bool KeepMounted { get; set; }
    [Parameter] public bool Leading { get; set; }
    [Parameter] public ExpandDirection Direction { get; set; }
    public bool Open { get; set; }
    public bool Disabled { get; set; }
    public int Height { get; set; } = 80;
    public int Width { get; set; } = 240;
    public bool Wrap { get; set; }
    public int Actions { get; private set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.AddAttribute(1, "style", $"width: {Width}px");
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Control.Id), "opener");
        builder.AddComponentParameter(12, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Toggle")));
        builder.AddComponentReferenceCapture(13, component => opener = (Button)component);
        builder.CloseComponent();
        builder.OpenComponent(20, Expander ? typeof(Expander) : typeof(Collapsible));
        if (Expander)
        {
            builder.AddComponentParameter(21, nameof(Controls.Expander.Header), "Details");
            builder.AddComponentParameter(22, nameof(Controls.Expander.IsExpanded), Open);
            builder.AddComponentParameter(23, nameof(Controls.Expander.ExpandDirection), Direction);
            builder.AddComponentParameter(24, nameof(Controls.Expander.IsExpandedChanged), EventCallback.Factory.Create<bool>(this, value => Open = value));
            builder.AddComponentParameter(25, nameof(Controls.Expander.KeepMounted), KeepMounted);
            if (Leading) builder.AddComponentParameter(26, nameof(Controls.Expander.Leading), (RenderFragment)(content => content.AddContent(0, "L")));
        }
        else
        {
            builder.AddComponentParameter(25, nameof(Collapsible.As), "ProbeRegion");
            builder.AddComponentParameter(26, nameof(Collapsible.IsOpen), Open);
            builder.AddComponentParameter(27, nameof(Collapsible.KeepMounted), KeepMounted);
            builder.AddComponentParameter(28, nameof(Collapsible.ReturnFocus), opener?.Element);
        }
        builder.AddComponentParameter(29, "Disabled", Disabled);
        builder.AddComponentParameter(30, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenElement(0, "Body");
            content.AddAttribute(1, "style", Wrap ? null : $"height: {Height}px");
            if (Wrap) content.AddContent(2, "Content wraps into more lines when its container becomes narrower. This additional paragraph keeps its full natural height while clipped.");
            content.OpenComponent<Button>(10);
            content.AddComponentParameter(11, nameof(Control.Id), "inside");
            content.AddComponentParameter(12, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            content.AddComponentParameter(13, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Action")));
            content.CloseComponent();
            content.CloseElement();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class CollapsibleMotionTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private RootComponent root = null!;
    private CollapsibleMotionPage Page => (CollapsibleMotionPage)root.Component;
    private ElementNode Panel => root.Elements().Single(element => element.Name is "ProbeRegion" or "ExpanderPanel");
    private ElementNode Content => root.Elements().Single(element => element.Name is "ProbeRegionContent" or "ExpanderPanelContent");
    private ElementNode Inside => root.Elements("Button").Single(element => element.Id == "inside");
    private double PanelHeight => harness.Scene.GetBounds(Panel).Height;

    public CollapsibleMotionTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(400, 400);
        harness.Scene.Transitions.ReducedMotion = false;
        var compiled = NssCompiler.Compile("measured-reveal", """
            ProbeRegion, ExpanderPanel { transition: height 1000ms linear; overlay-exit-duration: 1100ms; }
            ProbeRegionContent, ExpanderPanelContent { display: block; padding: 0; border: 0; }
            Expander[expand-direction=up] { flex-direction: column-reverse; }
            ExpanderChevron { transform: rotate(0deg); transition: transform 1000ms linear; }
            Expander[expanded] > ExpanderHeader > ExpanderChevron { transform: rotate(180deg); }
            Expander[expand-direction=up] > ExpanderHeader > ExpanderChevron { transform: rotate(180deg); }
            Expander[expand-direction=up][expanded] > ExpanderHeader > ExpanderChevron { transform: rotate(0deg); }
            """);
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        harness.Scene.Styles.Application = compiled.StyleSheet;
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void Show(bool expander = false, bool keepMounted = false, ExpandDirection direction = ExpandDirection.Down)
    {
        root = harness.Host.Render<CollapsibleMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(CollapsibleMotionPage.Expander)] = expander,
            [nameof(CollapsibleMotionPage.KeepMounted)] = keepMounted,
            [nameof(CollapsibleMotionPage.Direction)] = direction,
        }));
        Frame();
    }

    private void Frame()
    {
        for (var frame = 0; frame < 4; frame++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private void SetOpen(bool value)
    {
        Page.Open = value;
        Page.Refresh();
        Frame();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_viewport_reveals_and_collapses_the_natural_height_without_remeasuring_the_clipped_height(bool keepMounted)
    {
        Show(keepMounted: keepMounted);
        Assert.Equal(0, PanelHeight);
        SetOpen(true);
        var natural = harness.Scene.GetBounds(Content).Height;
        Assert.Equal(80, natural);
        Assert.Equal(0, PanelHeight);
        clock.Advance(500);
        Frame();
        Assert.Equal(natural / 2, PanelHeight, 2);
        Assert.Equal(natural, harness.Scene.GetBounds(Content).Height);
        clock.Advance(500);
        Frame();
        Assert.Equal(natural, PanelHeight);
        Assert.Equal(Overflow.Hidden, harness.Scene.GetStyle(Panel).Get(StyleProperties.OverflowY));
        SetOpen(false);
        Assert.True(Content.IsDisabled);
        Assert.True(Content.Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.False(input.Activate(Inside));
        clock.Advance(500);
        Frame();
        Assert.Equal(natural / 2, PanelHeight, 2);
        Assert.Equal(natural, harness.Scene.GetBounds(Content).Height);
        clock.Advance(500);
        Frame();
        Assert.Equal(0, PanelHeight);
        Assert.Equal(0, Page.Actions);
    }

    [Fact]
    public void Reversing_mid_reveal_continues_from_the_current_height_without_a_jump()
    {
        Show();
        SetOpen(true);
        clock.Advance(400);
        Frame();
        Assert.Equal(32, PanelHeight, 2);
        SetOpen(false);
        Assert.Equal(32, PanelHeight, 2);
        clock.Advance(200);
        Frame();
        Assert.Equal(16, PanelHeight, 2);
        SetOpen(true);
        Assert.Equal(16, PanelHeight, 2);
        clock.Advance(400);
        Frame();
        Assert.InRange(PanelHeight, 16.01, 79.99);
        clock.Advance(600);
        Frame();
        Assert.Equal(80, PanelHeight);
    }

    [Fact]
    public void Kept_content_remeasures_while_closed_and_after_width_changes()
    {
        Show(keepMounted: true);
        Page.Height = 160;
        Page.Refresh();
        Frame();
        Assert.Equal(160, harness.Scene.GetBounds(Content).Height);
        SetOpen(true);
        clock.Advance(1000);
        Frame();
        Assert.Equal(160, PanelHeight);
        Page.Wrap = true;
        Page.Refresh();
        Frame();
        clock.Advance(1000);
        Frame();
        var wideHeight = PanelHeight;
        Page.Width = 120;
        Page.Refresh();
        Frame();
        var narrowNatural = harness.Scene.GetBounds(Content).Height;
        Assert.True(narrowNatural > wideHeight);
        clock.Advance(1000);
        Frame();
        Assert.Equal(narrowNatural, PanelHeight);
        Frame();
        Assert.Equal(narrowNatural, PanelHeight);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reduced_motion_finishes_collapse_and_keeps_only_explicitly_mounted_content(bool keepMounted)
    {
        Show(keepMounted: keepMounted);
        SetOpen(true);
        clock.Advance(1000);
        Frame();
        SetOpen(false);
        Assert.Contains(root.Elements(), element => element.Name.EndsWith("RegionContent", StringComparison.Ordinal));
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Equal(0, PanelHeight);
        Assert.Equal(keepMounted ? 1 : 0, root.Elements("ProbeRegionContent").Count());
    }

    [Theory]
    [InlineData(ExpandDirection.Down)]
    [InlineData(ExpandDirection.Up)]
    public void Expander_places_the_panel_on_the_requested_side_and_returns_hidden_focus_to_its_header(ExpandDirection direction)
    {
        Show(expander: true, direction: direction);
        var header = root.Single("ExpanderHeader");
        SetOpen(true);
        clock.Advance(1000);
        Frame();
        var headerBounds = harness.Scene.GetBounds(header);
        var panelBounds = harness.Scene.GetBounds(Panel);
        Assert.Equal(direction == ExpandDirection.Up, panelBounds.Bottom <= headerBounds.Y);
        Assert.Equal("chevron-down", root.Single("ExpanderChevron").Attributes["glyph"]);
        input.Focus(Inside, visible: true);
        SetOpen(false);
        Assert.Same(header, input.Focused);
        Page.Disabled = true;
        Page.Refresh();
        Frame();
        Assert.False(input.Activate(header));
        Assert.False(Page.Open);
    }
}
