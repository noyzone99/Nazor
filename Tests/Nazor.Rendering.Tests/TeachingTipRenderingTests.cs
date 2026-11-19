using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TeachingTipRenderingPage : ComponentBase
{
    [Parameter] public OverlayPlacement Placement { get; set; }
    [Parameter] public float AnchorX { get; set; } = 390;
    [Parameter] public float AnchorY { get; set; } = 280;
    [Parameter] public bool Targeted { get; set; } = true;
    [Parameter] public bool Hero { get; set; }
    [Parameter] public bool LightDismiss { get; set; }
    [Parameter] public bool InitiallyOpen { get; set; } = true;
    [Parameter] public bool FocusOnOpen { get; set; }
    private ElementReference anchor;
    public TeachingTip Tip { get; private set; } = null!;
    public bool Open { get; private set; } = true;
    public int Dismissals { get; private set; }
    public int Actions { get; private set; }

    protected override void OnInitialized() => Open = InitiallyOpen;
    protected override void OnAfterRender(bool firstRender) { if (firstRender) StateHasChanged(); }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "width: 100%; height: 100%; background: #f1e3c7;");
        builder.OpenElement(10, "Anchor");
        builder.AddAttribute(11, "style", $"position: absolute; left: {AnchorX}px; top: {AnchorY}px; width: 20px; height: 24px;");
        builder.AddAttribute(12, "focusable", true);
        builder.AddAttribute(13, "role", "button");
        builder.AddElementReferenceCapture(14, value => anchor = value);
        builder.CloseElement();
        builder.OpenComponent<Button>(20);
        builder.AddComponentParameter(21, nameof(Button.Id), "outside");
        builder.AddComponentParameter(22, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
        builder.AddComponentParameter(23, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Outside")));
        builder.CloseComponent();
        builder.OpenComponent<TeachingTip>(30);
        builder.AddComponentParameter(31, nameof(TeachingTip.IsOpen), Open);
        builder.AddComponentParameter(32, nameof(TeachingTip.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => Open = value));
        builder.AddComponentParameter(33, nameof(TeachingTip.Target), Targeted ? (ElementReference?)anchor : null);
        builder.AddComponentParameter(34, nameof(TeachingTip.Placement), Placement);
        builder.AddComponentParameter(35, nameof(TeachingTip.Title), "Share your work");
        builder.AddComponentParameter(36, nameof(TeachingTip.Subtitle), "A new feature");
        builder.AddComponentParameter(37, nameof(TeachingTip.CloseOnOutside), LightDismiss);
        builder.AddComponentParameter(38, nameof(TeachingTip.OnDismiss), EventCallback.Factory.Create(this, () => Dismissals++));
        if (Hero) builder.AddComponentParameter(39, nameof(TeachingTip.Hero), (RenderFragment)(image =>
        {
            image.OpenElement(0, "HeroPaint");
            image.AddAttribute(1, "style", "width: 100%; height: 96px; background: #ed3344;");
            image.CloseElement();
        }));
        builder.AddComponentParameter(40, nameof(TeachingTip.ChildContent), (RenderFragment)(body =>
        {
            body.OpenComponent<TextField>(0);
            body.AddComponentParameter(1, nameof(TextField.Value), "A field inside the callout");
            body.CloseComponent();
        }));
        builder.AddComponentParameter(41, nameof(TeachingTip.Actions), (RenderFragment)(actions =>
        {
            actions.OpenComponent<Button>(0);
            actions.AddComponentParameter(1, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            actions.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Continue")));
            actions.CloseComponent();
        }));
        builder.AddComponentParameter(42, nameof(TeachingTip.FocusOnOpen), FocusOnOpen);
        builder.AddComponentReferenceCapture(43, component => Tip = (TeachingTip)component);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class TeachingTipRenderingTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Arrow_anchor_points_must_be_finite(double coordinate)
    {
        var overlay = new PopoverContainer { AnchorPoint = (coordinate, 0) };
        var method = typeof(PopoverContainer).GetMethod("OnParametersSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(overlay, null));
        Assert.Equal("AnchorPoint", Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException).ParamName);
    }

    public static IEnumerable<object[]> SidesAndScales()
    {
        foreach (var side in Enum.GetValues<OverlayPlacement>())
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        foreach (var flip in new[] { false, true })
            yield return [side, scale, flip];
    }

    [Theory]
    [MemberData(nameof(SidesAndScales))]
    public void Fluent_tail_follows_the_arranged_side_and_anchor_with_no_border_seam(OverlayPlacement side, float scale, bool flip)
    {
        using var context = new Context(side, scale, flip);
        var image = context.Frame();
        var surface = context.Surface;
        var bounds = context.Harness.Scene.GetBounds(surface);
        var arrow = context.Harness.Scene.GetBounds(context.Root.Single("TeachingTipArrow"));
        var anchor = context.Harness.Scene.GetBounds(context.Root.Single("Anchor"));
        Assert.InRange(bounds.Width, 320 - 1 / scale, 336 + 1 / scale);
        var actual = flip ? Opposite(side) : side;
        Assert.Equal(actual == OverlayPlacement.Top, surface.States.HasFlag(ElementStates.PopupAbove));
        Assert.Equal(actual == OverlayPlacement.Left, surface.States.HasFlag(ElementStates.PopupLeft));
        Assert.Equal(actual == OverlayPlacement.Right, surface.States.HasFlag(ElementStates.PopupRight));
        var center = new Point(arrow.X + arrow.Width / 2, arrow.Y + arrow.Height / 2);
        var seam = actual switch
        {
            OverlayPlacement.Top => new Point(center.X, bounds.Bottom - 0.5f / scale),
            OverlayPlacement.Bottom => new Point(center.X, bounds.Y + 0.5f / scale),
            OverlayPlacement.Left => new Point(bounds.Right - 0.5f / scale, center.Y),
            _ => new Point(bounds.X + 0.5f / scale, center.Y),
        };
        if (actual is OverlayPlacement.Top or OverlayPlacement.Bottom)
        {
            Assert.Equal(actual == OverlayPlacement.Top ? bounds.Bottom : bounds.Y, center.Y, 0.001f);
            Assert.Equal(anchor.X + anchor.Width / 2, center.X, 0.001f);
        }
        else
        {
            Assert.Equal(actual == OverlayPlacement.Left ? bounds.Right : bounds.X, center.X, 0.001f);
            Assert.Equal(anchor.Y + anchor.Height / 2, center.Y, 0.001f);
        }
        Assert.Equal(new Graphics.Color(253, 253, 253, 255), Pixel(image, seam, scale));
        var outside = actual switch
        {
            OverlayPlacement.Top => new Point(center.X, center.Y + arrow.Height / 4),
            OverlayPlacement.Bottom => new Point(center.X, center.Y - arrow.Height / 4),
            OverlayPlacement.Left => new Point(center.X + arrow.Width / 4, center.Y),
            _ => new Point(center.X - arrow.Width / 4, center.Y),
        };
        Assert.Equal(new Graphics.Color(253, 253, 253, 255), Pixel(image, outside, scale));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Clamped_tail_stays_clear_of_corners_and_hero_content_is_clipped(float scale)
    {
        using var context = new Context(OverlayPlacement.Bottom, scale, anchorX: 0, hero: true);
        var image = context.Frame();
        var surface = context.Harness.Scene.GetBounds(context.Surface);
        var arrow = context.Harness.Scene.GetBounds(context.Root.Single("TeachingTipArrow"));
        Assert.True(arrow.X >= surface.X + 9.9f);
        Assert.True(arrow.Right <= surface.Right - 9.9f);
        var hero = context.Harness.Scene.GetBounds(context.Root.Single("TeachingTipHero"));
        var corner = Pixel(image, new Point(hero.X + 0.5f / scale, hero.Y + 0.5f / scale), scale);
        var center = Pixel(image, new Point(hero.X + hero.Width / 2, hero.Y + hero.Height / 2), scale);
        Assert.NotEqual(center, corner);
        Assert.Equal(new Graphics.Color(237, 51, 68, 255), center);
    }

    [Theory]
    [InlineData(OverlayPlacement.Top)]
    [InlineData(OverlayPlacement.Bottom)]
    [InlineData(OverlayPlacement.Left)]
    [InlineData(OverlayPlacement.Right)]
    public void Native_positioner_side_controls_arrow_geometry_instead_of_the_requested_side(OverlayPlacement actual)
    {
        using var context = new Context(OverlayPlacement.Right, 1);
        context.Harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 800, 600);
        context.Harness.Scene.ArrangeNativePopup = (_, request) => actual switch
        {
            OverlayPlacement.Left => new Rect(request.Anchor.X - request.Bounds.Width - 8, request.Anchor.Y + (request.Anchor.Height - request.Bounds.Height) / 2, request.Bounds.Width, request.Bounds.Height),
            OverlayPlacement.Right => new Rect(request.Anchor.Right + 8, request.Anchor.Y + (request.Anchor.Height - request.Bounds.Height) / 2, request.Bounds.Width, request.Bounds.Height),
            OverlayPlacement.Top => new Rect(request.Anchor.X + (request.Anchor.Width - request.Bounds.Width) / 2, request.Anchor.Y - request.Bounds.Height - 8, request.Bounds.Width, request.Bounds.Height),
            _ => new Rect(request.Anchor.X + (request.Anchor.Width - request.Bounds.Width) / 2, request.Anchor.Bottom + 8, request.Bounds.Width, request.Bounds.Height),
        };
        context.Frame();
        var surface = context.Harness.Scene.GetBounds(context.Surface);
        var arrow = context.Harness.Scene.GetBounds(context.Root.Single("TeachingTipArrow"));
        Assert.Equal(actual == OverlayPlacement.Top, context.Surface.States.HasFlag(ElementStates.PopupAbove));
        Assert.Equal(actual == OverlayPlacement.Left, context.Surface.States.HasFlag(ElementStates.PopupLeft));
        Assert.Equal(actual == OverlayPlacement.Right, context.Surface.States.HasFlag(ElementStates.PopupRight));
        if (actual is OverlayPlacement.Left or OverlayPlacement.Right)
            Assert.Equal(actual == OverlayPlacement.Left ? surface.Right : surface.X, arrow.X + arrow.Width / 2, 0.001f);
        else Assert.Equal(actual == OverlayPlacement.Top ? surface.Bottom : surface.Y, arrow.Y + arrow.Height / 2, 0.001f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Untargeted_tips_center_and_persistent_or_light_dismiss_modes_keep_their_contract(bool lightDismiss)
    {
        using var context = new Context(OverlayPlacement.Bottom, 1, targeted: false, lightDismiss: lightDismiss);
        var bounds = context.Harness.Scene.GetBounds(context.Surface);
        Assert.Equal(400, bounds.X + bounds.Width / 2, 0.01f);
        Assert.Equal(300, bounds.Y + bounds.Height / 2, 0.01f);
        Assert.Empty(context.Root.Elements("TeachingTipArrow"));
        var outside = context.Root.Elements("Button").Single(element => element.Id == "outside");
        var button = context.Harness.Scene.GetBounds(outside);
        var point = new Point(button.X + button.Width / 2, button.Y + button.Height / 2);
        context.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        context.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        context.Frame();
        Assert.Equal(!lightDismiss, context.Page.Open);
        Assert.Equal(lightDismiss ? 1 : 0, context.Page.Dismissals);
        Assert.Equal(lightDismiss ? 0 : 1, context.Page.Actions);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public async Task Expand_and_contract_paint_from_the_tail_and_retained_content_stays_inactive(float scale)
    {
        var clock = new ManualAnimationClock();
        using var context = new Context(OverlayPlacement.Bottom, scale, initiallyOpen: false, focusOnOpen: true, reducedMotion: false, clock: clock);
        var anchor = context.Root.Single("Anchor");
        context.Input.Focus(anchor, visible: true);
        await context.Page.Tip.OpenAsync();
        context.Frame();
        var surface = context.Surface;
        var arrow = context.Root.Single("TeachingTipArrow");
        var initialArrow = context.Harness.Scene.GetBounds(arrow);
        var tip = new Point(initialArrow.X + initialArrow.Width / 2, initialArrow.Y);
        Assert.Equal(0d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        clock.Advance(100);
        var expanding = context.Frame();
        var opacity = context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity);
        Assert.InRange(opacity, 0.001, 0.999);
        var scaleValue = context.Harness.Scene.GetTransform(surface).M11;
        Assert.InRange(scaleValue, 0.9401f, 0.9999f);
        Assert.Contains(context.Harness.Scene.BuildDisplayList().Commands.OfType<PushTransform>(), transform => Math.Abs(transform.Matrix.M11 - scaleValue) < 0.001);
        var animatedArrow = context.Harness.Scene.GetBounds(arrow);
        Assert.Equal(tip.X, animatedArrow.X + animatedArrow.Width / 2, 0.001f);
        Assert.Equal(tip.Y, animatedArrow.Y, 0.001f);
        var bounds = context.Harness.Scene.GetBounds(surface);
        var panePoint = new Point(bounds.X + 5, bounds.Y + bounds.Height / 2);
        var partialPixel = Pixel(expanding, panePoint, scale);
        clock.Advance(300);
        context.Input.Focus(context.Root.Single("TextFieldInput"), visible: true);
        var opened = context.Frame();
        Assert.Equal(1d, context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity));
        Assert.NotEqual(partialPixel, Pixel(opened, panePoint, scale));
        var field = context.Harness.Scene.GetBounds(context.Root.Single("TextField"));
        foreach (var point in new[] { new Point(field.X - 2, field.Y + field.Height / 2), new Point(field.Right + 2, field.Y + field.Height / 2) })
            Assert.Equal(new Graphics.Color(216, 32, 199, 255), Pixel(opened, point, scale));

        await context.Page.Tip.CloseAsync();
        context.Frame();
        Assert.Same(surface, context.Surface);
        Assert.True(surface.Attributes.GetValueOrDefault("exiting") is true);
        Assert.True(surface.IsDisabled);
        Assert.True(surface.Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.False(context.Input.Activate(context.Root.Single("TeachingTipActions").Elements().Single(element => element.Name == "Button")));
        Assert.Equal(0, context.Page.Actions);
        clock.Advance(100);
        context.Frame();
        Assert.InRange(context.Harness.Scene.GetEffectiveValue(surface, StyleProperties.Opacity), 0.001, 0.999);
        var contractingArrow = context.Harness.Scene.GetBounds(arrow);
        Assert.Equal(tip.X, contractingArrow.X + contractingArrow.Width / 2, 0.001f);
        Assert.Equal(tip.Y, contractingArrow.Y, 0.001f);
        context.Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        context.Frame();
        context.Frame();
        Assert.Empty(context.Root.Elements("TeachingTip"));
        Assert.Same(anchor, context.Input.Focused);
    }

    private sealed class Context : IDisposable
    {
        public RenderHarness Harness { get; }
        public InputManager Input { get; }
        public RootComponent Root { get; }
        public TeachingTipRenderingPage Page { get; }
        private readonly float scale;
        public ElementNode Surface => Root.Single("TeachingTip");

        public Context(OverlayPlacement side, float scale, bool flip = false, float? anchorX = null, bool hero = false, bool targeted = true, bool lightDismiss = false,
            bool initiallyOpen = true, bool focusOnOpen = false, bool reducedMotion = true, ManualAnimationClock? clock = null)
        {
            this.scale = scale;
            Harness = new RenderHarness(clock: clock);
            Input = new InputManager(Harness.Host.Renderer, Harness.Scene);
            Harness.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
            Harness.Scene.Transitions.ReducedMotion = reducedMotion;
            Harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reducedMotion);
            var app = NssCompiler.Compile("TeachingTipProbe", "TeachingTip { --nz-layer: #fdfdfd; --nz-stroke-overlay: #2020aa; box-shadow: none; } TextField:focus-within { box-shadow: 0 0 0 4px #d820c7; }");
            Assert.True(app.Success, string.Join("\n", app.Diagnostics));
            Harness.Scene.Styles.Application = app.StyleSheet;
            Root = Harness.Host.Render<TeachingTipRenderingPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(TeachingTipRenderingPage.Placement)] = side,
                [nameof(TeachingTipRenderingPage.AnchorX)] = anchorX ?? (flip && side == OverlayPlacement.Right ? 775 : flip && side == OverlayPlacement.Left ? 5 : 390f),
                [nameof(TeachingTipRenderingPage.AnchorY)] = flip && side == OverlayPlacement.Bottom ? 560f : flip && side == OverlayPlacement.Top ? 5f : 280f,
                [nameof(TeachingTipRenderingPage.Targeted)] = targeted,
                [nameof(TeachingTipRenderingPage.Hero)] = hero,
                [nameof(TeachingTipRenderingPage.LightDismiss)] = lightDismiss,
                [nameof(TeachingTipRenderingPage.InitiallyOpen)] = initiallyOpen,
                [nameof(TeachingTipRenderingPage.FocusOnOpen)] = focusOnOpen,
            }));
            Page = (TeachingTipRenderingPage)Root.Component;
            Frame();
        }
        public RenderedImage Frame()
        {
            Harness.Host.RunPending();
            Harness.Render(800, 600, scale);
            Harness.Host.RunPending();
            return Harness.Render(800, 600, scale);
        }
        public void Dispose() { Input.Dispose(); Harness.Dispose(); }
    }

    private static OverlayPlacement Opposite(OverlayPlacement side) => side switch
    {
        OverlayPlacement.Top => OverlayPlacement.Bottom, OverlayPlacement.Bottom => OverlayPlacement.Top,
        OverlayPlacement.Left => OverlayPlacement.Right, _ => OverlayPlacement.Left,
    };
    private static Graphics.Color Pixel(RenderedImage image, Point point, float scale) => image.GetPixel((int)MathF.Floor(point.X * scale), (int)MathF.Floor(point.Y * scale));
}
