using Nazor.Input;
using Nazor.Graphics;

namespace Nazor.Controls.Tests.Selection;

public sealed class RangePressedProbe : ComponentBase
{
    public bool Range { get; set; }
    public bool Disabled { get; set; }
    public bool Show { get; set; } = true;
    public bool Hidden { get; set; }
    public double Value { get; set; } = 20;
    public List<double> Inputs { get; } = [];
    public List<double> Changes { get; } = [];
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page"); builder.AddAttribute(1, "style", "padding: 24px;");
        if (Show)
        {
            builder.OpenComponent(10, Range ? typeof(RangeSlider) : typeof(Slider));
            builder.AddComponentParameter(11, "Style", "width: 220px;" + (Hidden ? "display: none;" : ""));
            builder.AddComponentParameter(12, "Disabled", Disabled);
            builder.AddComponentParameter(13, Range ? nameof(RangeSlider.LowerValue) : nameof(Slider.Value), Value);
            builder.AddComponentParameter(14, Range ? nameof(RangeSlider.LowerValueChanged) : nameof(Slider.ValueChanged), EventCallback.Factory.Create<double>(this, value => { Value = value; Inputs.Add(value); }));
            if (Range) builder.AddComponentParameter(15, nameof(RangeSlider.UpperValue), 80d);
            builder.AddComponentParameter(16, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => Changes.Add((double)e.Value!)));
            builder.CloseComponent();
        }
        builder.OpenComponent<Button>(30);
        builder.AddComponentParameter(31, nameof(Button.Id), "captor");
        builder.AddComponentParameter(32, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Captor")));
        builder.CloseComponent(); builder.CloseElement();
    }
}

[Collection("Controls")]
public sealed class RangePressedTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private RangePressedProbe Show(bool range)
    {
        h.Render<RangePressedProbe>(); var probe = h.Component<RangePressedProbe>(); probe.Range = range; probe.Refresh(); h.Frame(); return probe;
    }
    private ElementNode Owner(bool range) => range ? h.All("RangeSliderHandle").First() : h.One("Slider");
    private Point PointAt(ElementNode owner, double fraction)
    {
        var track = owner.Elements().SelectMany(element => element.Elements().Prepend(element)).First(element => element.Attributes.GetValueOrDefault("range-track") is true);
        var box = h.Bounds(track); return new Point(box.X + box.Width * (float)fraction, box.Y + box.Height / 2);
    }
    private void Press(ElementNode owner)
    {
        var thumb = owner.Elements().SelectMany(element => element.Elements().Prepend(element)).First(element => element.Name is "SliderThumb" or "RangeSliderThumb");
        var box = h.Bounds(thumb); var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
        h.Input.PointerMoved(center, KeyModifiers.None); h.Input.PointerPressed(center, PointerButton.Primary, KeyModifiers.None); h.Frame();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_captured_range_publishes_pressed_and_input_during_movement_then_one_change_on_release(bool range)
    {
        var probe = Show(range); var owner = Owner(range); Press(owner);
        Assert.Same(owner, h.Input.CapturedPointer); Assert.True(owner.States.HasFlag(ElementStates.Pressed));
        var point = PointAt(owner, .65); h.Input.PointerMoved(point, KeyModifiers.None); h.Frame();
        Assert.NotEmpty(probe.Inputs); Assert.Empty(probe.Changes);
        h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); h.Frame();
        Assert.Null(h.Input.CapturedPointer); Assert.False(owner.States.HasFlag(ElementStates.Pressed));
        Assert.Equal(probe.Value, Assert.Single(probe.Changes));
        var value = probe.Value; var inputs = probe.Inputs.Count;
        h.Input.PointerMoved(PointAt(owner, .9), KeyModifiers.None); h.Frame();
        Assert.Equal(value, probe.Value); Assert.Equal(inputs, probe.Inputs.Count); Assert.Single(probe.Changes);
    }

    [Theory]
    [InlineData(false, "cancel")]
    [InlineData(true, "cancel")]
    [InlineData(false, "release")]
    [InlineData(true, "release")]
    [InlineData(false, "transfer")]
    [InlineData(true, "transfer")]
    [InlineData(false, "disable")]
    [InlineData(true, "disable")]
    [InlineData(false, "remove")]
    [InlineData(true, "remove")]
    [InlineData(false, "hide")]
    [InlineData(true, "hide")]
    public void Capture_cancellation_clears_pressed_and_blocks_stale_move_or_release_before_a_fresh_gesture(bool range, string reason)
    {
        var probe = Show(range); var owner = Owner(range); Press(owner);
        h.Input.PointerMoved(PointAt(owner, .6), KeyModifiers.None); h.Frame();
        var stalePoint = PointAt(owner, .7); var value = probe.Value; var inputs = probe.Inputs.Count;
        switch (reason)
        {
            case "cancel": h.Input.PointerCanceled(); break;
            case "release": h.Input.ReleasePointerCapture(owner); break;
            case "transfer": Assert.True(h.Input.CapturePointer(h.One("Button"))); break;
            case "disable": probe.Disabled = true; probe.Refresh(); break;
            case "remove": probe.Show = false; probe.Refresh(); break;
            case "hide": probe.Hidden = true; probe.Refresh(); break;
        }
        h.Frame(); Assert.False(owner.States.HasFlag(ElementStates.Pressed));
        if (reason == "transfer") Assert.Same(h.One("Button"), h.Input.CapturedPointer); else Assert.Null(h.Input.CapturedPointer);
        h.Input.PointerMoved(stalePoint, KeyModifiers.None); h.Input.PointerReleased(stalePoint, PointerButton.Primary, KeyModifiers.None); h.Frame();
        Assert.Equal(value, probe.Value); Assert.Equal(inputs, probe.Inputs.Count); Assert.Empty(probe.Changes);
        probe.Disabled = false; probe.Show = true; probe.Hidden = false; probe.Refresh(); h.Frame();
        owner = Owner(range); Press(owner);
        var fresh = PointAt(owner, .7); h.Input.PointerMoved(fresh, KeyModifiers.None); h.Frame();
        h.Input.PointerReleased(fresh, PointerButton.Primary, KeyModifiers.None); h.Frame();
        Assert.Single(probe.Changes); Assert.False(owner.States.HasFlag(ElementStates.Pressed));
    }
}
