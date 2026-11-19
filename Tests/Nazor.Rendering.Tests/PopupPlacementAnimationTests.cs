using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PopupPlacementAnimationPage : ComponentBase
{
    [Parameter] public bool Editable { get; set; }
    public int Top { get; set; } = 20;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", $"padding-top: {Top}px; padding-left: 20px");
        builder.OpenComponent(10, Editable ? typeof(ComboBox<string>) : typeof(Select<string>));
        builder.AddComponentParameter(11, "Items", new[] { "Item 1", "Item 2", "Item 3", "Item 4" });
        if (Editable) builder.AddComponentParameter(12, nameof(ComboBox<string>.OpenOnFocus), true);
        else builder.AddComponentParameter(12, nameof(Select<string>.IsOpen), true);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class PopupPlacementAnimationTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private RootComponent root = null!;

    public PopupPlacementAnimationTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(400, 300);
        harness.Scene.Transitions.ReducedMotion = false;
        var style = NssCompiler.Compile("popup-direction", """
            SelectPopup, ComboBoxPopup {
                --opening-top: 0%; --opening-bottom: 100%;
                animation: directional-open 1000ms linear both;
            }
            SelectPopup:popup-above, ComboBoxPopup:popup-above { --opening-top: 100%; --opening-bottom: 0%; }
            @keyframes directional-open {
                from { clip-path: inset(var(--opening-top) 0 var(--opening-bottom)); }
                to { clip-path: inset(0); }
            }
            """);
        Assert.True(style.Success, string.Join("\n", style.Diagnostics));
        harness.Scene.Styles.Application = style.StyleSheet;
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Popup => harness.Scene.Overlays.Single();
    private PopupPlacementAnimationPage Page => (PopupPlacementAnimationPage)root.Component;

    private void Show(bool editable, int top)
    {
        root = harness.Host.Render<PopupPlacementAnimationPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PopupPlacementAnimationPage.Editable)] = editable,
        }));
        Page.Top = top;
        Page.Refresh();
        Frame();
        if (!editable) return;
        input.Focus(root.Single("ComboBoxInput"), visible: true);
        Frame();
    }

    private void Frame()
    {
        harness.Host.RunPending();
        harness.Scene.Update();
        harness.Scene.BuildDisplayList();
    }

    private void AssertOpeningSide(bool above, double percent)
    {
        Assert.Equal(above, Popup.States.HasFlag(ElementStates.PopupAbove));
        var clip = harness.Scene.GetEffectiveValue(Popup, StyleProperties.ClipPath);
        Assert.Equal(above ? percent : 0, clip.Top.Resolve(100), 6);
        Assert.Equal(above ? 0 : percent, clip.Bottom.Resolve(100), 6);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void The_first_visible_frame_and_midpoint_reveal_from_the_resolved_anchor_edge(bool editable, bool above)
    {
        Show(editable, above ? 250 : 20);
        AssertOpeningSide(above, 100);

        clock.Advance(500);
        Frame();

        AssertOpeningSide(above, 50);
        Assert.Equal("bottom", Popup.Attributes["placement"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Moving_the_anchor_changes_the_reveal_edge_without_restarting_the_timeline(bool editable)
    {
        Show(editable, 20);
        var trigger = harness.Scene.Transitions.GetAnimations(Popup).Single().Trigger;
        clock.Advance(250);
        Page.Top = 250;
        Page.Refresh();
        Frame();

        AssertOpeningSide(above: true, 75);
        Assert.Equal(trigger, harness.Scene.Transitions.GetAnimations(Popup).Single().Trigger);
        clock.Advance(250);
        Frame();
        AssertOpeningSide(above: true, 50);
    }

    [Fact]
    public void Filtering_an_editable_list_preserves_its_entry_timeline_and_resolved_side()
    {
        Show(editable: true, top: 250);
        var trigger = harness.Scene.Transitions.GetAnimations(Popup).Single().Trigger;
        clock.Advance(250);

        input.TextInput("1");
        Frame();

        Assert.Single(root.Elements("ComboBoxOption"));
        AssertOpeningSide(above: true, 75);
        Assert.Equal(trigger, harness.Scene.Transitions.GetAnimations(Popup).Single().Trigger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_compositor_placement_controls_the_first_animation_frame_without_unbounded_arrangement(bool above)
    {
        var arrangements = 0;
        harness.Scene.NativePopupWorkArea = _ => new Rect(0, 0, 400, 500);
        harness.Scene.ArrangeNativePopup = (_, request) =>
        {
            arrangements++;
            return new Rect(request.Bounds.X, above ? request.Anchor.Y - request.Bounds.Height : request.Anchor.Bottom,
                request.Bounds.Width, request.Bounds.Height);
        };

        Show(editable: false, top: above ? 100 : 250);

        AssertOpeningSide(above, 100);
        Assert.InRange(arrangements, 1, 2);
        Assert.Equal("bottom", Popup.Attributes["placement"]);
        var previous = arrangements;
        Frame();
        AssertOpeningSide(above, 100);
        Assert.Equal(previous + 1, arrangements);
    }

    [Fact]
    public void Reduced_motion_exposes_the_final_unclipped_surface()
    {
        harness.Scene.Transitions.ReducedMotion = true;
        Show(editable: false, top: 250);

        AssertOpeningSide(above: true, 0);
    }
}
