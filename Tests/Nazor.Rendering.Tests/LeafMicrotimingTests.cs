using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class LeafPressMotionPage : ComponentBase
{
    [Parameter] public string Kind { get; set; } = "Button";
    [Parameter] public bool IconOnly { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.AddAttribute(1, "style", "padding: 32px;");
        builder.OpenComponent(10, Kind switch { "ToggleButton" => typeof(ToggleButton), "MenuButton" => typeof(MenuButton), _ => typeof(Button) });
        if (IconOnly)
        {
            builder.AddComponentParameter(11, nameof(Button.Leading), (RenderFragment)(content =>
            {
                content.OpenElement(0, "Mark"); content.AddAttribute(1, "style", "width: 20px; height: 20px; background: currentColor;"); content.CloseElement();
            }));
        }
        else builder.AddComponentParameter(12, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Apply")));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class LeafMicrotimingTests
{
    [Theory]
    [InlineData("Button")]
    [InlineData("ToggleButton")]
    [InlineData("MenuButton")]
    public void Ordinary_press_uses_eighty_milliseconds_for_each_property_and_reversal_samples_current_corners(string kind)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<LeafPressMotionPage>(Parameters(kind, false));
        Frame();
        var control = root.Single(kind);
        var box = h.Scene.GetBounds(control);
        var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
        input.PointerMoved(center, KeyModifiers.None); Frame(); clock.Advance(500); Frame();
        var before = Corner();
        input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None); Frame();
        Assert.Equal(before, Corner());
        Assert.All(h.Scene.GetStyle(control).Get(StyleProperties.Transition).Items, item => Assert.Equal(TimeSpan.FromMilliseconds(80), item.Duration));
        Assert.Equal(TimeSpan.FromMilliseconds(80), Assert.IsType<RunningTransition>(h.Scene.Transitions.Find(control, StyleProperties.Transform)).Duration);
        clock.Advance(20); Frame();
        before = Corner();
        input.PointerCanceled(); Frame();
        Assert.Equal(before, Corner());
        clock.Advance(500); Frame();
        Assert.Equal(h.Scene.GetStyle(control).Get(StyleProperties.BorderTopLeftRadius), Corner());
        RadiusValue Corner() => h.Scene.GetEffectiveValue(control, StyleProperties.BorderTopLeftRadius);
        void Frame() { h.Host.RunPending(); h.Render(400, 160, 1); h.Host.RunPending(); h.Render(400, 160, 1); }
    }

    [Fact]
    public void Icon_only_buttons_keep_their_separate_one_hundred_fifty_color_and_two_hundred_fifty_transform_timelines()
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        var root = h.Host.Render<LeafPressMotionPage>(Parameters("Button", true));
        h.Host.RunPending(); h.Render(200, 160, 1);
        var definitions = h.Scene.GetStyle(root.Single("Button")).Get(StyleProperties.Transition).Items;
        Assert.Equal(TimeSpan.FromMilliseconds(150), Assert.Single(definitions, item => item.Property == "background-color").Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(150), Assert.Single(definitions, item => item.Property == "color").Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(250), Assert.Single(definitions, item => item.Property == "transform").Duration);
    }

    public static IEnumerable<object[]> Sliders()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var range in new[] { false, true })
        foreach (var labels in new[] { ValueLabelMode.None, ValueLabelMode.Always }) yield return [vertical, range, labels];
    }

    [Theory]
    [MemberData(nameof(Sliders))]
    public void Thumb_dimensions_deform_over_two_hundred_milliseconds_around_a_fixed_center_and_values_move_directly(bool vertical, bool range, ValueLabelMode labels)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme; h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<SliderCutoutPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SliderCutoutPage.Vertical)] = vertical, [nameof(SliderCutoutPage.Labels)] = labels,
        }));
        var page = (SliderCutoutPage)root.Component;
        page.Configure(false, 50, 25, 75); Frame();
        var thumb = root.Elements(range ? "RangeSliderThumb" : "SliderThumb").First();
        var center = Center();
        Assert.Equal(24, Long()); Assert.Equal(6, Short());
        input.PointerMoved(center, KeyModifiers.None); Frame();
        Assert.Equal(TimeSpan.FromMilliseconds(200), Assert.IsType<RunningTransition>(h.Scene.Transitions.Find(thumb, vertical ? StyleProperties.Width : StyleProperties.Height)).Duration);
        clock.Advance(50); Frame();
        Assert.InRange(Long(), 24.01f, 27.99f); Assert.Equal(center, Center());
        clock.Advance(150); Frame();
        Assert.Equal(28, Long()); Assert.Equal(center, Center());
        input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None); Frame();
        Assert.Equal(TimeSpan.FromMilliseconds(200), Assert.IsType<RunningTransition>(h.Scene.Transitions.Find(thumb, vertical ? StyleProperties.Height : StyleProperties.Width)).Duration);
        clock.Advance(50); Frame();
        Assert.InRange(Short(), 6.01f, 9.99f); Assert.Equal(center, Center());
        var partial = Short();
        input.PointerCanceled(); Frame();
        Assert.Equal(partial, Short());
        clock.Advance(300); Frame(); Assert.Equal(6, Short());
        center = Center();
        page.Configure(false, range ? 50 : 75, range ? 35 : 25, 75); Frame();
        var moved = Center();
        Assert.NotEqual(center, moved);
        Assert.Null(h.Scene.Transitions.Find(thumb, vertical ? StyleProperties.Bottom : StyleProperties.Left));
        clock.Advance(250); Frame(); Assert.Equal(moved, Center());
        float Long() { var size = h.Scene.GetLayoutNode(thumb)!.AbsoluteFrame.Size; return vertical ? size.Width : size.Height; }
        float Short() { var size = h.Scene.GetLayoutNode(thumb)!.AbsoluteFrame.Size; return vertical ? size.Height : size.Width; }
        Point Center() { var box = h.Scene.GetBounds(thumb); return new Point(box.X + box.Width / 2, box.Y + box.Height / 2); }
        void Frame() { h.Host.RunPending(); h.Render(400, 360, 1); h.Host.RunPending(); h.Render(400, 360, 1); }
    }

    private static ParameterView Parameters(string kind, bool iconOnly) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(LeafPressMotionPage.Kind)] = kind, [nameof(LeafPressMotionPage.IconOnly)] = iconOnly,
    });
}
