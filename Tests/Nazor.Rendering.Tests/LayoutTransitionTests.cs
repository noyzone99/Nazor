using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class LayoutTransitionPage : ComponentBase
{
    public double Top { get; set; }
    public double Width { get; set; } = 100;
    public string ParentTransform { get; set; } = "none";
    public bool Enabled { get; set; } = true;
    public bool Destination { get; set; }
    public bool ShowSource { get; set; } = true;
    public LayoutTransitionOrigin? Origin { get; set; }
    public LayoutTransition? Source { get; private set; }
    public int Actions { get; private set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Parent");
        builder.AddAttribute(1, "style", $"width: 300px; height: 180px; overflow: auto; transform: {ParentTransform}; transform-origin: 0 0");
        builder.OpenElement(2, "Flow");
        builder.AddAttribute(3, "style", "height: 800px");
        if (ShowSource)
        {
            builder.OpenComponent<LayoutTransition>(10);
            builder.AddComponentParameter(11, nameof(Control.Id), "source");
            builder.AddComponentParameter(12, nameof(Control.Style), $"display: block; width: {Width}px; height: 40px; margin-top: {Top}px");
            builder.AddComponentParameter(13, nameof(LayoutTransition.IsEnabled), Enabled);
            builder.AddComponentParameter(14, nameof(LayoutTransition.ChildContent), (RenderFragment)(content => RenderButton(content, "source-button")));
            builder.AddComponentReferenceCapture(15, component => Source = (LayoutTransition)component);
            builder.CloseComponent();
        }
        if (Destination)
        {
            builder.OpenComponent<LayoutTransition>(20);
            builder.AddComponentParameter(21, nameof(Control.Id), "destination");
            builder.AddComponentParameter(22, nameof(Control.Style), "display: block; width: 200px; height: 80px; margin-top: 300px");
            builder.AddComponentParameter(23, nameof(LayoutTransition.From), Origin);
            builder.AddComponentParameter(24, nameof(LayoutTransition.ChildContent), (RenderFragment)(content => RenderButton(content, "destination-button")));
            builder.CloseComponent();
        }
        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderButton(RenderTreeBuilder builder, string id)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Control.Id), id);
        builder.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Action")));
        builder.AddComponentParameter(3, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
        builder.CloseComponent();
    }
}

public sealed class LayoutTransitionTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;
    private LayoutTransitionPage Page => (LayoutTransitionPage)root.Component;
    private ElementNode Source => root.Elements("LayoutTransition").Single(element => element.Id == "source");
    private ElementNode Destination => root.Elements("LayoutTransition").Single(element => element.Id == "destination");
    private Rect Bounds(ElementNode element) => harness.Scene.GetBounds(element);

    public LayoutTransitionTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(600, 600);
        harness.Scene.Transitions.ReducedMotion = false;
        var sheet = NssCompiler.Compile("layout-motion", "LayoutTransition { transition: transform 1000ms linear; }");
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        harness.Scene.Styles.Application = sheet.StyleSheet;
        root = harness.Host.Render<LayoutTransitionPage>();
        Frame();
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void Frame()
    {
        for (var i = 0; i < 3; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private void Change(Action action)
    {
        action();
        Page.Refresh();
        Frame();
    }

    private void Advance(int milliseconds) { clock.Advance(milliseconds); Frame(); }

    [Fact]
    public void Layout_relocation_and_resize_preserve_identity_focus_and_the_current_visible_box()
    {
        var source = Source;
        var button = root.Elements("Button").Single();
        input.Focus(button, visible: true);
        var first = Bounds(source);
        Change(() => { Page.Top = 80; Page.Width = 200; });
        Assert.Same(source, Source);
        Assert.Same(button, input.Focused);
        Assert.Equal(first, Bounds(Source));
        Assert.Equal(80, harness.Scene.GetLayoutNode(Source)!.RoundedFrame.Y);
        Advance(500);
        Assert.Equal(40, Bounds(Source).Y, 2);
        Assert.Equal(150, Bounds(Source).Width, 2);
        Advance(500);
        Assert.Equal(80, Bounds(Source).Y, 2);
        Assert.Equal(200, Bounds(Source).Width, 2);
        Assert.False(harness.Scene.Transitions.IsRunning);
        Assert.True(input.Activate(button));
        Assert.Equal(1, Page.Actions);
    }

    [Fact]
    public void A_retarget_samples_the_visible_position_rather_than_the_previous_layout_destination()
    {
        Change(() => Page.Top = 100);
        Advance(400);
        Assert.Equal(40, Bounds(Source).Y, 2);
        Change(() => Page.Top = 200);
        Assert.Equal(40, Bounds(Source).Y, 2);
        Advance(500);
        Assert.Equal(120, Bounds(Source).Y, 2);
        Advance(500);
        Assert.Equal(200, Bounds(Source).Y, 2);
    }

    [Fact]
    public void A_connected_destination_consumes_the_visible_origin_once_after_the_source_is_removed()
    {
        var sourceElement = Source;
        var source = Bounds(Source);
        Page.Origin = Page.Source!.CaptureOrigin();
        Assert.NotNull(Page.Origin);
        Change(() => { Page.ShowSource = false; Page.Destination = true; });
        Assert.True(sourceElement.IsRemoved);
        Assert.Null(sourceElement.GetFeature<ILayoutTransition>());
        Assert.Equal(source, Bounds(Destination));
        Advance(500);
        Assert.Equal(150, Bounds(Destination).Y, 2);
        Assert.Equal(150, Bounds(Destination).Width, 2);
        Advance(500);
        var destination = Bounds(Destination);
        Page.Refresh();
        Frame();
        Assert.Equal(destination, Bounds(Destination));
        Assert.False(harness.Scene.Transitions.IsRunning);
    }

    [Fact]
    public void A_connected_origin_cannot_be_applied_in_another_presentation_scene()
    {
        var origin = Page.Source!.CaptureOrigin();
        Assert.NotNull(origin);
        using var other = new RenderHarness();
        other.Scene.Viewport = new Size(400, 400);
        var otherRoot = other.Host.Render<LayoutTransitionPage>();
        var page = (LayoutTransitionPage)otherRoot.Component;
        page.ShowSource = false;
        page.Destination = true;
        page.Origin = origin;
        page.Refresh();
        other.Host.RunPending();
        Assert.Throws<InvalidOperationException>(() => other.Scene.Update());
    }

    [Fact]
    public void Scroll_translation_and_scaled_ancestors_are_inverted_for_connected_geometry()
    {
        Change(() => { Page.ParentTransform = "translate(30px, 10px) scale(1.5)"; Page.Top = 120; });
        Advance(1000);
        harness.Scene.ScrollTo(root.Single("Parent"), null, 50, immediate: true);
        Frame();
        var source = Bounds(Source);
        Page.Origin = Page.Source!.CaptureOrigin();
        Change(() => { Page.ShowSource = false; Page.Destination = true; });
        Assert.Equal(source.X, Bounds(Destination).X, 2);
        Assert.Equal(source.Y, Bounds(Destination).Y, 2);
        Assert.Equal(source.Width, Bounds(Destination).Width, 2);
        Advance(1000);
        Assert.Equal(300, Bounds(Destination).Width, 2);
    }

    [Theory]
    [InlineData("rotate(10deg)")]
    [InlineData("skewX(10deg)")]
    [InlineData("scale(0)")]
    public void Unsupported_connected_coordinate_spaces_fail_explicitly(string transform)
    {
        Change(() => Page.ParentTransform = transform);
        Assert.Throws<InvalidOperationException>(() => Page.Source!.CaptureOrigin());
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("reduced")]
    [InlineData("suspended")]
    public void Disabled_reduced_and_suspended_motion_finish_at_the_layout_destination(string mode)
    {
        Change(() => Page.Top = 100);
        Advance(300);
        if (mode == "reduced") harness.Scene.Transitions.ReducedMotion = true;
        if (mode == "suspended") harness.Scene.Transitions.Suspended = true;
        Change(() => { if (mode == "disabled") Page.Enabled = false; Page.Top = 200; });
        Assert.Equal(200, Bounds(Source).Y, 2);
        Assert.False(harness.Scene.Transitions.IsRunning);
        if (mode == "disabled") Assert.Null(Source.GetFeature<ILayoutTransition>());
    }
}
