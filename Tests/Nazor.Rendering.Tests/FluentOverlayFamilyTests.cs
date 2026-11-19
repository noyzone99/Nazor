using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;
using DrawerControl = Nazor.Controls.Drawer;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentOverlayFamilyPage : ComponentBase
{
    [Parameter] public string Kind { get; set; } = "Popover";
    [Parameter] public OverlayPlacement Placement { get; set; }
    [Parameter] public DrawerPlacement Side { get; set; }
    private Button? anchor;
    private TooltipContainer? tooltip;
    public bool Open { get; private set; }
    public int Actions { get; private set; }
    public async Task SetOpenAsync(bool value)
    {
        Open = value;
        if (tooltip is not null) { if (value) await tooltip.OpenAsync(); else await tooltip.CloseAsync(); }
        StateHasChanged();
    }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Control.Id), "anchor");
        builder.AddComponentParameter(2, nameof(Control.Style), "position: absolute; left: 280px; top: 210px; width: 40px; height: 30px");
        builder.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Open")));
        if (Kind == "implicit") builder.AddComponentParameter(4, nameof(Control.Tooltip), "Description");
        builder.AddComponentReferenceCapture(5, component => anchor = (Button)component);
        builder.CloseComponent();
        if (Kind == "implicit") return;
        var type = Kind switch
        {
            "Drawer" => typeof(DrawerControl), "tooltip" => typeof(TooltipContainer), "Dialog" => typeof(Dialog),
            "AlertDialog" => typeof(AlertDialog), "DialogContainer" => typeof(DialogContainer), _ => typeof(PopoverContainer),
        };
        builder.OpenComponent(10, type);
        if (Kind == "tooltip")
        {
            builder.AddComponentParameter(11, nameof(TooltipContainer.Trigger), (RenderFragment)(content => content.AddContent(0, "Tip trigger")));
            builder.AddComponentReferenceCapture(12, component => tooltip = (TooltipContainer)component);
        }
        else
        {
            builder.AddComponentParameter(13, "IsOpen", Open);
            if (Kind == "Drawer") builder.AddComponentParameter(14, nameof(DrawerControl.Placement), Side);
            else if (Kind is not ("Dialog" or "AlertDialog" or "DialogContainer"))
            {
                builder.AddComponentParameter(15, nameof(OverlayContainer.As), Kind);
                builder.AddComponentParameter(16, nameof(PopoverContainer.Anchor), anchor?.Element);
                builder.AddComponentParameter(17, nameof(PopoverContainer.Placement), Placement);
                builder.AddComponentParameter(18, nameof(PopoverContainer.FocusOnOpen), false);
                builder.AddComponentParameter(19, nameof(OverlayContainer.ExitDuration), TimeSpan.FromSeconds(5));
            }
        }
        builder.AddComponentParameter(20, "ChildContent", (RenderFragment)(content =>
        {
            if (Kind == "tooltip") { content.AddContent(0, "Description"); return; }
            content.OpenComponent<Button>(0);
            content.AddComponentParameter(1, nameof(Control.Id), "action");
            content.AddComponentParameter(2, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            content.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Action")));
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class FluentOverlayFamilyTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness h;
    private readonly InputManager input;
    private RootComponent root = null!;
    private FluentOverlayFamilyPage Page => (FluentOverlayFamilyPage)root.Component;
    private ElementNode Surface => h.Scene.Overlays.Single(element => !element.Name.EndsWith("Scrim", StringComparison.Ordinal));
    private double Opacity(ElementNode element) => h.Scene.Transitions.GetValue(element, StyleProperties.Opacity, h.Scene.GetStyle(element).Opacity);
    private Matrix Transform(ElementNode element) => Scene.Compose(h.Scene.Transitions.GetValue(element, StyleProperties.Transform,
        h.Scene.GetStyle(element).Get(StyleProperties.Transform)), h.Scene.GetLayoutNode(element)!.RoundedFrame);
    public FluentOverlayFamilyTests()
    {
        h = new RenderHarness(clock: clock);
        h.Scene.Viewport = new Size(640, 500);
        h.Scene.Styles.Theme = FluentThemes.Fluent;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, false);
        h.Scene.Transitions.ReducedMotion = false;
        input = new InputManager(h.Host.Renderer, h.Scene) { TooltipDelay = TimeSpan.Zero };
    }
    public void Dispose() { input.Dispose(); h.Dispose(); }
    private void Frame() { for (var pass = 0; pass < 4; pass++) { h.Host.RunPending(); h.Scene.Update(); h.Scene.BuildDisplayList(); } }
    private void Advance(int milliseconds) { clock.Advance(milliseconds); Frame(); }
    private void Show(string kind, OverlayPlacement placement = OverlayPlacement.Bottom, DrawerPlacement side = DrawerPlacement.End)
    {
        root = h.Host.Render<FluentOverlayFamilyPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FluentOverlayFamilyPage.Kind)] = kind, [nameof(FluentOverlayFamilyPage.Placement)] = placement,
            [nameof(FluentOverlayFamilyPage.Side)] = side,
        }));
        Frame();
    }

    [Theory]
    [InlineData(OverlayPlacement.Bottom, 0, -50)]
    [InlineData(OverlayPlacement.Top, 0, 50)]
    [InlineData(OverlayPlacement.Left, 50, 0)]
    [InlineData(OverlayPlacement.Right, -50, 0)]
    public async Task Flyout_entry_follows_the_arranged_side_and_exit_reentry_samples_the_current_visual(OverlayPlacement placement, double x, double y)
    {
        Show("Popover", placement);
        await Page.SetOpenAsync(true); Frame();
        var surface = Surface;
        var initialBounds = h.Scene.GetBounds(surface);
        Assert.Equal(x, Transform(surface).OffsetX, 2);
        Assert.Equal(y, Transform(surface).OffsetY, 2);
        Advance(100);
        Assert.InRange(Math.Abs(Transform(surface).OffsetX), 0, Math.Abs(x));
        Assert.InRange(Math.Abs(Transform(surface).OffsetY), 0, Math.Abs(y));
        Advance(300);
        Assert.Equal(1, Opacity(surface));
        Assert.Equal(0, Transform(surface).OffsetX, 2);
        Assert.Equal(0, Transform(surface).OffsetY, 2);
        Assert.Equal(x, initialBounds.X - h.Scene.GetBounds(surface).X, 2);
        Assert.Equal(y, initialBounds.Y - h.Scene.GetBounds(surface).Y, 2);
        await Page.SetOpenAsync(false); Frame(); Advance(50);
        Assert.True(surface.IsDisabled);
        Assert.True(surface.Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.False(input.Activate(root.Elements("Button").Single(element => element.Id == "action")));
        var fading = Opacity(surface);
        Assert.InRange(fading, .01, .99);
        await Page.SetOpenAsync(true); Frame();
        Assert.Same(surface, Surface);
        Assert.Equal(fading, Opacity(surface), 3);
        Advance(50);
        Assert.True(Opacity(surface) > fading);
        Assert.Equal(0, Page.Actions);
    }

    [Theory]
    [InlineData("tooltip")]
    [InlineData("implicit")]
    public async Task Tooltip_entry_is_a_pure_fade_and_same_target_reentry_does_not_restart_from_zero(string kind)
    {
        Show(kind);
        if (kind == "implicit") HoverAnchor(); else { await Page.SetOpenAsync(true); Frame(); }
        var surface = Surface;
        Assert.Equal(0, Transform(surface).OffsetX, 2);
        Assert.Equal(0, Transform(surface).OffsetY, 2);
        Advance(100); Assert.InRange(Opacity(surface), .1, .9);
        Advance(200); Assert.Equal(1, Opacity(surface));
        if (kind == "implicit") { input.PointerMoved(new Point(1, 450), KeyModifiers.None); Frame(); }
        else { await Page.SetOpenAsync(false); Frame(); }
        Advance(50); var fading = Opacity(surface);
        Assert.InRange(fading, .01, .99);
        if (kind == "implicit") HoverAnchor(); else { await Page.SetOpenAsync(true); Frame(); }
        Assert.Same(surface, Surface);
        Assert.Equal(fading, Opacity(surface), 3);
        Advance(50); Assert.True(Opacity(surface) > fading);

        void HoverAnchor()
        {
            var box = h.Scene.GetBounds(root.Elements("Button").Single(element => element.Id == "anchor"));
            input.PointerMoved(new Point(box.X + box.Width / 2, box.Y + box.Height / 2), KeyModifiers.None); Frame();
        }
    }

    [Fact]
    public async Task Select_retains_its_clip_entry_and_uses_a_noninteractive_fade_exit()
    {
        Show("SelectPopup"); await Page.SetOpenAsync(true); Frame();
        var surface = Surface;
        Assert.Equal(0, Transform(surface).OffsetY, 2);
        Assert.NotNull(h.Scene.Transitions.GetAnimations(surface).SingleOrDefault(animation => animation.Definition.Name == "nz-combo-open"));
        Advance(300); await Page.SetOpenAsync(false); Frame(); Advance(50);
        Assert.Same(surface, Surface);
        Assert.InRange(Opacity(surface), .01, .99);
        Assert.True(surface.IsDisabled);
    }

    [Theory]
    [InlineData(DrawerPlacement.Start)]
    [InlineData(DrawerPlacement.End)]
    [InlineData(DrawerPlacement.Top)]
    [InlineData(DrawerPlacement.Bottom)]
    public async Task Drawer_enters_from_its_edge_and_keeps_the_modal_barrier_during_exit(DrawerPlacement side)
    {
        Show("Drawer", side: side); await Page.SetOpenAsync(true); Frame();
        var surface = Surface;
        var box = h.Scene.GetBounds(surface);
        var initial = Transform(surface);
        if (side is DrawerPlacement.Start or DrawerPlacement.End) Assert.Equal(side == DrawerPlacement.Start ? -box.Width : box.Width, initial.OffsetX, 2);
        else Assert.Equal(side == DrawerPlacement.Top ? -box.Height : box.Height, initial.OffsetY, 2);
        Advance(400); await Page.SetOpenAsync(false); Frame(); Advance(50);
        Assert.True(surface.IsDisabled);
        Assert.Contains(surface, h.Scene.Overlays);
        Assert.True(surface.Attributes.GetValueOrDefault("modal") is true);
        Assert.False(input.Activate(root.Elements("Button").Single(element => element.Id == "anchor")));
        Assert.InRange(Opacity(surface), .01, .99);
        await Page.SetOpenAsync(true); Frame();
        Assert.Same(surface, Surface);
        Advance(400); Assert.Equal(1, Opacity(surface));
    }

    [Fact]
    public async Task Reduced_motion_removes_retained_family_surfaces_immediately()
    {
        Show("Popover"); await Page.SetOpenAsync(true); Frame(); Advance(400);
        await Page.SetOpenAsync(false); Frame();
        Assert.NotEmpty(h.Scene.Overlays);
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = true }; Frame();
        Assert.Empty(h.Scene.Overlays);
    }

    [Theory]
    [InlineData("Dialog")]
    [InlineData("AlertDialog")]
    [InlineData("DialogContainer")]
    public async Task Every_modal_dialog_family_samples_entry_exit_and_reopen_including_its_scrim(string kind)
    {
        Show(kind); await Page.SetOpenAsync(true); Frame();
        var surface = Surface;
        var scrim = h.Scene.Overlays.Single(element => element.Name.EndsWith("Scrim", StringComparison.Ordinal));
        Assert.Equal(1.05, Transform(surface).M11, 3);
        Assert.Equal(0, Opacity(surface));
        Advance(30);
        var enteringScale = Transform(surface).M11;
        var enteringOpacity = Opacity(surface);
        Assert.InRange(enteringScale, 1.001, 1.049);
        await Page.SetOpenAsync(false); Frame();
        Assert.Equal(enteringScale, Transform(surface).M11, 3);
        Assert.Equal(enteringOpacity, Opacity(surface), 3);
        Advance(20);
        var exitingScale = Transform(surface).M11;
        var exitingOpacity = Opacity(surface);
        var scrimOpacity = Opacity(scrim);
        Assert.True(exitingScale > enteringScale);
        Assert.True(exitingOpacity < enteringOpacity);
        Assert.True(surface.IsDisabled);
        await Page.SetOpenAsync(true); Frame();
        Assert.Same(surface, Surface);
        Assert.Equal(exitingScale, Transform(surface).M11, 3);
        Assert.Equal(exitingOpacity, Opacity(surface), 3);
        Assert.Equal(scrimOpacity, Opacity(scrim), 3);
        Advance(40);
        Assert.True(Transform(surface).M11 < exitingScale);
        Assert.True(Opacity(surface) > exitingOpacity);
        Assert.True(Opacity(scrim) > scrimOpacity);
        Advance(250);
        Assert.Equal(1, Transform(surface).M11, 3);
        Assert.Equal(1, Opacity(surface));
    }
}
