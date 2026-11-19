using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentDisclosureMotionTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private RootComponent root = null!;
    private CollapsibleMotionPage Page => (CollapsibleMotionPage)root.Component;
    private ElementNode Panel => root.Single("ExpanderPanel");
    private ElementNode Content => root.Single("ExpanderPanelContent");
    private ElementNode Header => root.Single("ExpanderHeader");
    private double Height => harness.Scene.GetBounds(Panel).Height;
    private double Translation => harness.Scene.GetTransform(Content).OffsetY;

    public FluentDisclosureMotionTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(500, 500);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Transitions.ReducedMotion = false;
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void Show(ExpandDirection direction, bool keepMounted)
    {
        root = harness.Host.Render<CollapsibleMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(CollapsibleMotionPage.Expander)] = true,
            [nameof(CollapsibleMotionPage.Direction)] = direction,
            [nameof(CollapsibleMotionPage.KeepMounted)] = keepMounted,
        }));
        Frame();
    }

    private void Frame()
    {
        for (var i = 0; i < 6; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private void SetOpen(bool value) { Page.Open = value; Page.Refresh(); Frame(); }
    private void Advance(int milliseconds) { clock.Advance(milliseconds); Frame(); }

    [Theory]
    [InlineData(ExpandDirection.Down, false)]
    [InlineData(ExpandDirection.Down, true)]
    [InlineData(ExpandDirection.Up, false)]
    [InlineData(ExpandDirection.Up, true)]
    public void Fluent_moves_natural_content_inside_a_full_height_clip_then_releases_the_close_phase(ExpandDirection direction, bool keepMounted)
    {
        Show(direction, keepMounted);
        Assert.Equal(0, Height);
        SetOpen(true);
        var content = Content;
        var natural = harness.Scene.GetLayoutNode(content)!.RoundedFrame.Height;
        var sign = direction == ExpandDirection.Down ? -1 : 1;
        Assert.True(natural > Page.Height);
        Assert.Equal(natural, Height, 2);
        Assert.Equal(sign * natural, Translation, 2);
        Assert.Equal(true, content.Attributes["revealed"]);
        var panelBox = harness.Scene.GetBounds(Panel);
        var headerBox = harness.Scene.GetBounds(Header);
        Assert.Equal(direction == ExpandDirection.Up, panelBox.Bottom <= headerBox.Y);
        Advance(166);
        Assert.Equal(natural, Height, 2);
        Assert.InRange(Math.Abs(Translation), 0.001, natural - 0.001);
        Advance(167);
        Assert.Equal(0, Translation, 2);
        var inside = root.Elements("Button").Single(element => element.Id == "inside");
        input.Focus(inside, visible: true);
        SetOpen(false);
        Assert.Same(Header, input.Focused);
        Assert.Equal(natural, Height, 2);
        Assert.Equal(true, Panel.Attributes["exiting"]);
        Assert.True(content.IsDisabled);
        Assert.False(input.Activate(inside));
        var retention = harness.Scene.GetStyle(Panel).Get(StyleProperties.OverlayExitDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(direction == ExpandDirection.Down ? 167 : 200), retention);
        Advance(166);
        Assert.Equal(natural, Height, 2);
        Assert.InRange(Math.Abs(Translation), 0.001, natural);
        Advance(1);
        Assert.Equal(sign * natural, Translation, 2);
        harness.Host.RunUntil(() => Panel.Attributes.GetValueOrDefault("closed") is true, timeoutMilliseconds: 2000);
        Frame();
        Assert.Equal(0, Height);
        Assert.Equal(keepMounted ? 1 : 0, root.Elements("ExpanderPanelContent").Count());
        if (keepMounted) Assert.Same(content, Content);
        Page.Height = 160;
        Page.Refresh();
        Frame();
        SetOpen(true);
        var nextNatural = harness.Scene.GetLayoutNode(Content)!.RoundedFrame.Height;
        Assert.True(nextNatural > natural);
        Assert.Equal(nextNatural, Height, 2);
        Assert.Equal(sign * nextNatural, Translation, 2);
        Assert.Equal(false, Panel.Attributes["measuring"]);
    }

    [Theory]
    [InlineData(ExpandDirection.Down)]
    [InlineData(ExpandDirection.Up)]
    public void Fluent_reverses_from_the_current_transform_and_measures_a_natural_resize_without_a_jump(ExpandDirection direction)
    {
        Show(direction, keepMounted: true);
        SetOpen(true);
        Advance(100);
        var entering = Translation;
        Assert.NotEqual(0, entering);
        SetOpen(false);
        Assert.Equal(entering, Translation, 2);
        Advance(40);
        var closing = Translation;
        Page.Height = 160;
        Page.Refresh();
        Frame();
        Assert.Equal(closing, Translation, 2);
        Assert.True(Height > 160);
        var resized = Content;
        SetOpen(true);
        Assert.Equal(closing, Translation, 2);
        Advance(333);
        Assert.Equal(0, Translation, 2);
        Assert.Same(resized, Content);
        Assert.False(Content.IsDisabled);
        SetOpen(false);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Equal(0, Height);
        Assert.Equal(true, Panel.Attributes["closed"]);
        Assert.Same(resized, Content);
        Page.Height = 240;
        Page.Refresh();
        Frame();
        Assert.True(harness.Scene.GetLayoutNode(Content)!.RoundedFrame.Height > 240);
        Assert.Equal(0, Height);
        SetOpen(true);
        Assert.True(Height > 240);
        Assert.Equal(0, Translation, 2);
    }
}
