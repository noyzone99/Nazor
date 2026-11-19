using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class DialWheelPage : ComponentBase
{
    public bool WheelChanges { get; set; }
    public bool Disabled { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 100;
    public double Step { get; set; } = 1;
    public double Value { get; set; } = 40;
    public int ParentWheels { get; private set; }
    public int DefaultWheels { get; private set; }
    public List<string> Events { get; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Scroller");
        builder.AddAttribute(1, "style", "display: flex; flex-direction: column; gap: 8px; width: 240px; height: 180px; overflow: auto; scroll-behavior: auto");
        builder.AddAttribute(2, "onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, () => ParentWheels++));
        builder.AddAttribute(3, "__default_onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, () => DefaultWheels++));
        builder.OpenComponent<TextField>(10);
        builder.AddComponentParameter(11, nameof(Control.Id), "focus");
        builder.CloseComponent();
        builder.OpenComponent<Dial>(20);
        builder.AddComponentParameter(21, nameof(Dial.Value), Value);
        builder.AddComponentParameter(22, nameof(Dial.ValueChanged), EventCallback.Factory.Create<double>(this, next => { Value = next; Events.Add("value"); }));
        builder.AddComponentParameter(23, nameof(Dial.Minimum), Minimum);
        builder.AddComponentParameter(24, nameof(Dial.Maximum), Maximum);
        builder.AddComponentParameter(25, nameof(Dial.Step), Step);
        builder.AddComponentParameter(26, nameof(Dial.WheelChanges), WheelChanges);
        builder.AddComponentParameter(27, nameof(Dial.Disabled), Disabled);
        builder.AddComponentParameter(28, nameof(Control.Style), "flex-shrink: 0");
        builder.AddComponentParameter(29, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, _ => Events.Add("input")));
        builder.AddComponentParameter(30, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, _ => Events.Add("change")));
        builder.CloseComponent();
        builder.OpenElement(40, "After");
        builder.AddAttribute(41, "style", "width: 600px; height: 600px; flex-shrink: 0");
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class DialWheelTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly DialWheelPage page;

    public DialWheelTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<DialWheelPage>();
        page = (DialWheelPage)root.Component;
        Frame();
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Dial => root.Single("Dial");
    private ScrollState Scroll => harness.Scene.GetScrollState(root.Single("Scroller"))!;

    private void Frame()
    {
        harness.Host.RunPending();
        harness.Render(400, 300, 1);
    }

    private void Change(Action<DialWheelPage> change)
    {
        change(page);
        page.Refresh();
        Frame();
    }

    private void Wheel(float y = 1, ScrollDeltaUnit unit = ScrollDeltaUnit.Notches, float x = 0, KeyModifiers modifiers = KeyModifiers.None)
    {
        var bounds = harness.Scene.GetBounds(Dial);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        input.PointerMoved(point, modifiers);
        input.PointerWheel(point, x, y, modifiers, unit);
        Frame();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_off_or_disabled_dial_leaves_normal_wheel_scrolling_available(bool disabled)
    {
        Change(p => { p.WheelChanges = disabled; p.Disabled = disabled; });

        Wheel(y: -1);

        Assert.Equal(40, page.Value);
        Assert.Empty(page.Events);
        Assert.True(Scroll.OffsetY > 0);
        if (!disabled)
        {
            Assert.Equal(1, page.ParentWheels);
            Assert.Equal(1, page.DefaultWheels);
        }
    }

    [Fact]
    public void Hover_wheel_changes_the_dial_without_taking_focus_or_scrolling_its_page()
    {
        Change(p => p.WheelChanges = true);
        var editor = root.Single("TextFieldInput");
        input.Focus(editor, visible: true);

        Wheel();

        Assert.Equal(41, page.Value);
        Assert.Equal(["value", "input", "change"], page.Events);
        Assert.Same(editor, input.Focused);
        Assert.Equal(0, Scroll.OffsetY);
        Assert.Equal(0, page.ParentWheels);
        Assert.Equal(0, page.DefaultWheels);
    }

    [Theory]
    [InlineData(ScrollDeltaUnit.Notches, 0.25f)]
    [InlineData(ScrollDeltaUnit.LogicalPixels, 12f)]
    public void Precision_gestures_accumulate_before_making_a_whole_step(ScrollDeltaUnit unit, float delta)
    {
        Change(p => { p.WheelChanges = true; p.Step = 2; });
        for (var i = 0; i < 3; i++) Wheel(delta, unit);
        Assert.Equal(40, page.Value);
        Assert.Empty(page.Events);

        Wheel(delta, unit);

        Assert.Equal(42, page.Value);
        Assert.Equal(["value", "input", "change"], page.Events);
        Assert.Equal(0, Scroll.OffsetY);
        Assert.Equal(0, page.ParentWheels);
    }

    [Theory]
    [InlineData(KeyModifiers.Shift, 0f, -1f)]
    [InlineData(KeyModifiers.Control, 0f, -1f)]
    [InlineData(KeyModifiers.Alt, 0f, -1f)]
    [InlineData(KeyModifiers.Meta, 0f, -1f)]
    [InlineData(KeyModifiers.None, 1f, 0f)]
    public void Modified_and_horizontal_gestures_keep_their_default_behavior(KeyModifiers modifiers, float x, float y)
    {
        Change(p => p.WheelChanges = true);

        Wheel(y, x: x, modifiers: modifiers);

        Assert.Equal(40, page.Value);
        Assert.Empty(page.Events);
        Assert.True(Scroll.OffsetX > 0 || Scroll.OffsetY > 0);
        Assert.Equal(1, page.ParentWheels);
        Assert.Equal(1, page.DefaultWheels);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(100, 1)]
    public void A_bound_consumes_outward_gestures_without_emitting_changes_or_delaying_reversal(double value, float direction)
    {
        Change(p => { p.WheelChanges = true; p.Value = value; });

        Wheel(3.75f * direction);

        Assert.Equal(value, page.Value);
        Assert.Empty(page.Events);
        Assert.Equal(0, Scroll.OffsetY);
        Assert.Equal(0, page.ParentWheels);
        Assert.Equal(0, page.DefaultWheels);
        Wheel(-direction);
        Assert.Equal(value - direction, page.Value);
        Assert.Equal(["value", "input", "change"], page.Events);
    }

    [Fact]
    public void A_continuous_dial_applies_fractional_gestures_proportionally_to_its_range()
    {
        Change(p => { p.WheelChanges = true; p.Minimum = 10; p.Maximum = 20; p.Value = 14; p.Step = 0; });

        Wheel(12, ScrollDeltaUnit.LogicalPixels);

        Assert.Equal(14.025, page.Value, 8);
        Assert.Equal(["value", "input", "change"], page.Events);
        Assert.Equal(0, Scroll.OffsetY);
    }

    [Fact]
    public void Wheel_steps_use_the_same_grid_and_bounds_as_keyboard_input()
    {
        Change(p => { p.WheelChanges = true; p.Minimum = 1; p.Maximum = 10; p.Step = 2.5; p.Value = 3.5; });
        Wheel();
        Assert.Equal(6, page.Value);
        Wheel(5);
        Assert.Equal(10, page.Value);
        input.Focus(Dial, visible: true);
        input.KeyDown("ArrowDown", KeyModifiers.None, false);
        input.KeyUp("ArrowDown", KeyModifiers.None);
        Frame();
        Assert.Equal(8.5, page.Value);
    }

    [Fact]
    public void Leaving_or_reconfiguring_the_dial_discards_partial_gestures()
    {
        Change(p => p.WheelChanges = true);
        Wheel(0.75f);
        input.PointerMoved(new Point(300, 200), KeyModifiers.None);
        Wheel(0.25f);
        Assert.Equal(40, page.Value);
        Change(p => p.Value = 60);
        Wheel(0.75f);
        Assert.Equal(60, page.Value);
        Change(p => p.WheelChanges = false);
        Change(p => p.WheelChanges = true);
        Wheel(0.25f);
        Assert.Equal(60, page.Value);
    }
}
