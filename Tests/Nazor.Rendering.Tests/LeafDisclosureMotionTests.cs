using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafDisclosureMotionTests
{
    private static void Frame(RenderHarness harness)
    {
        for (var i = 0; i < 6; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    [Theory]
    [InlineData(false, false, 18)]
    [InlineData(false, true, 50)]
    [InlineData(true, false, 18)]
    [InlineData(true, true, 50)]
    public void Leaf_replays_the_reference_body_entrance_on_open_with_immediate_close_and_conditional_leading_inset(bool keepMounted, bool leading, int inset)
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        harness.Scene.Transitions.ReducedMotion = false;
        harness.Scene.Viewport = new Size(500, 400);
        var root = harness.Host.Render<CollapsibleMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(CollapsibleMotionPage.Expander)] = true,
            [nameof(CollapsibleMotionPage.KeepMounted)] = keepMounted,
            [nameof(CollapsibleMotionPage.Leading)] = leading,
        }));
        var page = (CollapsibleMotionPage)root.Component;
        Frame(harness);
        page.Open = true;
        page.Refresh();
        Frame(harness);
        var content = root.Single("ExpanderPanelContent");
        Assert.Equal(inset, harness.Scene.GetLayoutNode(content)!.Padding.Left);
        Assert.Equal(0, harness.Scene.GetEffectiveValue(content, StyleProperties.Opacity));
        Assert.Equal(8, harness.Scene.GetTransform(content).OffsetY, 2);
        clock.Advance(200);
        Frame(harness);
        Assert.InRange(harness.Scene.GetEffectiveValue(content, StyleProperties.Opacity), 0.01, 0.9999);
        clock.Advance(200);
        Frame(harness);
        Assert.Equal(1, harness.Scene.GetEffectiveValue(content, StyleProperties.Opacity));
        page.Open = false;
        page.Refresh();
        Frame(harness);
        Assert.Equal(0, harness.Scene.GetBounds(root.Single("ExpanderPanel")).Height);
        Assert.Equal(keepMounted ? 1 : 0, root.Elements("ExpanderPanelContent").Count());
        page.Open = true;
        page.Refresh();
        Frame(harness);
        var reopened = root.Single("ExpanderPanelContent");
        if (keepMounted) Assert.Same(content, reopened);
        Assert.Equal(0, harness.Scene.GetEffectiveValue(reopened, StyleProperties.Opacity));
        clock.Advance(400);
        Frame(harness);
        Assert.Equal(1, harness.Scene.GetEffectiveValue(reopened, StyleProperties.Opacity));
    }
}
