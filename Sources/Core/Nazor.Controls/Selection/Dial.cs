using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A knob that chooses a number between <see cref="Minimum" /> and <see cref="Maximum" /> along an arc that starts at
/// <see cref="StartAngle" /> and spans <see cref="SweepAngle" /> degrees (clockwise from twelve o'clock). Dragging the
/// pointer's handle turns the knob to follow the pointer around the center; dragging anywhere else on the dial changes
/// the value with the distance moved (up or right increases), <see cref="DragDistance" /> logical units covering the whole
/// range. The keyboard works as on a <see cref="Slider" />. <see cref="Value" /> (<c>@bind-Value</c>) and <c>oninput</c>
/// follow the value, <c>onchange</c> comes when the user lets go. The element has the <c>slider</c> role, announced as a
/// dial. Losing pointer capture ends the active drag without an additional <c>onchange</c>. The parts are
/// <c>DialTrack</c> and <c>DialFill</c> (ring glyphs: the whole arc and the part up to the value),
/// <c>DialTicks</c> with <c>DialTick</c>, <c>DialPointer</c> with its <c>DialPointerHandle</c>, and <c>DialValue</c>.
/// The variables are <c>--nz-dial-angle</c> (the pointer's rotation, for example <c>transform: rotate(var(--nz-dial-angle))</c>),
/// <c>--nz-dial-start-angle</c>, <c>--nz-dial-fraction</c> and, on each tick, <c>--nz-dial-tick-angle</c>.
/// </summary>
public sealed class Dial : Control
{
    private readonly ControllableState<double> value = new(0);
    private ResizeEventArgs? pointerBox;
    private ResizeEventArgs? handleBox;
    private bool handleGrabbed;
    private Drag? drag;
    private double wheelRemainder;
    private (double Minimum, double Maximum, double Step) wheelRange;

    private sealed record Drag(double StartX, double StartY, double StartValue, double? CenterX, double? CenterY)
    {
        public bool Changed { get; set; }
    }

    /// <summary>Gets or sets the value.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Gets or sets the callback invoked while the user changes the value.</summary>
    [Parameter] public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Gets or sets the lowest value.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the highest value.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>Gets or sets the change made by an arrow key; values are multiples of it from the minimum, or any value
    /// when zero.</summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>Gets or sets the change made by Page Up and Page Down.</summary>
    [Parameter] public double LargeStep { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether an unmodified vertical wheel gesture over the dial changes its value without taking focus;
    /// false by default. One detent, or 48 logical pixels of precision scrolling, changes the value by <see cref="Step" />.
    /// Fractional detents accumulate until a full step is available. When <see cref="Step" /> is zero, each detent changes
    /// the value continuously by one percent of the range. Qualifying gestures are consumed even at a bound.
    /// </summary>
    [Parameter] public bool WheelChanges { get; set; }

    /// <summary>Gets or sets the angle of the minimum, in degrees clockwise from twelve o'clock.</summary>
    [Parameter] public double StartAngle { get; set; } = -135;

    /// <summary>Gets or sets the angle between the minimum and the maximum, in degrees, from 1 to 360.</summary>
    [Parameter] public double SweepAngle { get; set; } = 270;

    /// <summary>Gets or sets the distance between tick marks, in value units, or zero for none.</summary>
    [Parameter] public double TickFrequency { get; set; }

    /// <summary>Gets or sets whether the value is written in the middle of the dial.</summary>
    [Parameter] public bool ShowValue { get; set; } = true;

    /// <summary>Gets or sets how the value is written, as <see cref="Slider.Format" />.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Gets or sets the pointer distance, in logical units, that covers the whole range when dragging outside
    /// the handle.</summary>
    [Parameter] public double DragDistance { get; set; } = 200;

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the dial rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current value, kept within the range.</summary>
    public double CurrentValue => Math.Clamp(value.Value, Minimum, Maximum);

    /// <summary>Gets the pointer's angle for the current value, in degrees clockwise from twelve o'clock.</summary>
    public double Angle => StartAngle + RangeMath.Fraction(value.Value, Minimum, Maximum) * SweepAngle;

    /// <summary>Gets whether the user is dragging the dial.</summary>
    public bool IsDragging => drag is not null;

    /// <summary>Gets the NSS element name emitted by Dial.</summary>
    protected override string ElementName => "Dial";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Gets the angle variables and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join(
        $"--nz-dial-angle: {Css.Number(Angle)}deg; --nz-dial-start-angle: {Css.Number(StartAngle)}deg; --nz-dial-fraction: {Css.Number(RangeMath.Fraction(value.Value, Minimum, Maximum))}",
        Style);

    /// <summary>Validates the parameters and synchronizes the value.</summary>
    protected override void OnParametersSet()
    {
        SliderSupport.Validate(Minimum, Maximum, Step, LargeStep, TickFrequency);
        if (!double.IsFinite(Value)) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!double.IsFinite(StartAngle)) throw new ArgumentOutOfRangeException(nameof(StartAngle));
        if (!double.IsFinite(SweepAngle) || SweepAngle is < 1 or > 360) throw new ArgumentOutOfRangeException(nameof(SweepAngle), "The sweep must be between 1 and 360 degrees.");
        if (!double.IsFinite(DragDistance) || DragDistance <= 0) throw new ArgumentOutOfRangeException(nameof(DragDistance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (value.Sync(Value) || !WheelChanges || IsInactive || wheelRange != (Minimum, Maximum, Step)) wheelRemainder = 0;
        wheelRange = (Minimum, Maximum, Step);
        if (IsInactive) drag = null;
    }

    /// <summary>Adds the slider role, the range and the pointer handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "slider");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("Dial"));
        builder.AddAttribute(12, "focusable", !IsInactive);
        builder.AddAttribute(13, "disabled", IsInactive);
        RangeMath.AddRange(builder, 14, value.Value, Minimum, Maximum);
        builder.AddAttribute(17, "range-step", Step);
        builder.AddAttribute(18, "range-large-step", LargeStep);
        builder.AddAttribute(19, "dragging", drag is not null);
        builder.AddAttribute(20, "size", Vocabulary.Name(Size));
        builder.AddAttribute(21, "ticks", TickFrequency > 0);
        builder.AddAttribute(22, "__internal_oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        builder.AddEventPreventDefaultAttribute(23, "onpointerdown", true);
        builder.AddAttribute(24, "__internal_onpointerdown#dial", EventCallback.Factory.Create<PointerEventArgs>(this, Down));
        builder.AddAttribute(25, "__internal_onpointermove#dial", EventCallback.Factory.Create<PointerEventArgs>(this, MoveAsync));
        builder.AddAttribute(26, "__internal_onpointerup#dial", EventCallback.Factory.Create<PointerEventArgs>(this, UpAsync));
        if (Id is null && Field is { } field) builder.AddAttribute(27, "id", field.ControlId);
        builder.AddAttribute(28, "required", Field?.Required == true);
        builder.AddAttribute(29, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 30, LabeledBy, DescribedBy);
        if (WheelChanges)
        {
            builder.AddAttribute(32, "__internal_onwheel#dial", EventCallback.Factory.Create<WheelEventArgs>(this, WheelAsync));
            builder.AddAttribute(33, "__internal_onpointerleave#dial", EventCallback.Factory.Create<PointerEventArgs>(this, () => wheelRemainder = 0));
        }
        builder.AddAttribute(34, "__internal_onlostpointercapture#dial", EventCallback.Factory.Create<PointerEventArgs>(this,
            e => { if (e.OriginalTarget == Element.Element) CancelDrag(); }));
    }

    /// <summary>Adds the track, the fill, the ticks, the pointer and the value.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "DialTrack");
        builder.AddAttribute(1001, "glyph", "ring");
        builder.AddAttribute(1002, "ring-fraction", SweepAngle / 360);
        builder.AddAttribute(1003, "semantic-hidden", true);
        builder.CloseElement();
        builder.OpenElement(1010, "DialFill");
        builder.AddAttribute(1011, "glyph", "ring");
        builder.AddAttribute(1012, "ring-fraction", RangeMath.Fraction(value.Value, Minimum, Maximum) * SweepAngle / 360);
        builder.AddAttribute(1013, "semantic-hidden", true);
        builder.CloseElement();
        if (TickFrequency > 0)
        {
            builder.OpenElement(1020, "DialTicks");
            builder.AddAttribute(1021, "semantic-hidden", true);
            foreach (var tick in SliderSupport.Ticks(Minimum, Maximum, TickFrequency))
            {
                builder.OpenElement(1022, "DialTick");
                builder.SetKey(tick);
                builder.AddAttribute(1023, "style", $"--nz-dial-tick-angle: {Css.Number(StartAngle + RangeMath.Fraction(tick, Minimum, Maximum) * SweepAngle)}deg");
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.OpenElement(1030, "DialPointer");
        builder.AddAttribute(1031, "semantic-hidden", true);
        builder.AddAttribute(1032, "__internal_onresize#dial", EventCallback.Factory.Create<ResizeEventArgs>(this, e => pointerBox = e));
        builder.OpenElement(1033, "DialPointerHandle");
        builder.AddAttribute(1034, "__internal_onresize#dial", EventCallback.Factory.Create<ResizeEventArgs>(this, e => handleBox = e));
        builder.AddAttribute(1035, "__internal_onpointerdown#dial", EventCallback.Factory.Create<PointerEventArgs>(this, () => handleGrabbed = true));
        builder.CloseElement();
        builder.CloseElement();
        if (ShowValue)
        {
            builder.OpenElement(1040, "DialValue");
            builder.AddAttribute(1041, "semantic-hidden", true);
            builder.AddContent(1042, SliderSupport.Format(CurrentValue, Format));
            builder.CloseElement();
        }
    }

    private void Down(PointerEventArgs e)
    {
        var grabbed = handleGrabbed;
        handleGrabbed = false;
        if (IsInactive || e.Button != PointerButton.Primary) return;
        if (Element.Element is { } element && (element.States & ElementStates.Focus) == 0) _ = Element.FocusAsync(preventScroll: true);
        double? centerX = null, centerY = null;
        if (grabbed && pointerBox is { } pointer && handleBox is { } handle)
        {
            var x = handle.OffsetLeft + handle.Width / 2 - pointer.Width / 2;
            var y = handle.OffsetTop + handle.Height / 2 - pointer.Height / 2;
            var radians = Angle * Math.PI / 180;
            centerX = e.X - (x * Math.Cos(radians) - y * Math.Sin(radians));
            centerY = e.Y - (x * Math.Sin(radians) + y * Math.Cos(radians));
        }
        drag = new Drag(e.X, e.Y, CurrentValue, centerX, centerY);
        wheelRemainder = 0;
        Element.CapturePointer();
        StateHasChanged();
    }

    /// <summary>Computes the value at a pointer angle, as dragging the handle does.</summary>
    /// <param name="angle">The angle in degrees clockwise from twelve o'clock.</param>
    /// <returns>The value, snapped to the step; angles outside the arc give the nearer end.</returns>
    public double ValueAtAngle(double angle)
    {
        var relative = ((angle - StartAngle) % 360 + 360) % 360;
        if (relative > SweepAngle) relative = relative - SweepAngle < 360 - relative ? SweepAngle : 0;
        return SliderSupport.Snap(Minimum + relative / SweepAngle * (Maximum - Minimum), Minimum, Maximum, Step);
    }

    private async Task MoveAsync(PointerEventArgs e)
    {
        if (drag is not { } active) return;
        double next;
        if (active.CenterX is { } cx && active.CenterY is { } cy)
        {
            var (dx, dy) = (e.X - cx, e.Y - cy);
            if (dx * dx + dy * dy < 4) return;
            next = ValueAtAngle(Math.Atan2(dx, -dy) * 180 / Math.PI);
        }
        else next = SliderSupport.Snap(active.StartValue + (e.X - active.StartX - (e.Y - active.StartY)) / DragDistance * (Maximum - Minimum), Minimum, Maximum, Step);
        if (!value.Set(next)) return;
        active.Changed = true;
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next, commit: false);
    }

    private async Task UpAsync(PointerEventArgs e)
    {
        if (drag is not { } active) return;
        drag = null;
        Element.ReleasePointerCapture();
        StateHasChanged();
        if (active.Changed) await Events.RaiseChangeAsync(Element, CurrentValue);
    }

    private void CancelDrag()
    {
        if (drag is null && !handleGrabbed) return;
        drag = null;
        handleGrabbed = false;
        StateHasChanged();
    }

    private Task InputAsync(ChangeEventArgs e)
    {
        if (IsInactive || e.Value is not double next || !value.Set(next)) return Task.CompletedTask;
        StateHasChanged();
        return ValueChanged.InvokeAsync(next);
    }

    private async Task WheelAsync(WheelEventArgs e)
    {
        if (e.Handled || !WheelChanges || IsInactive || e.Modifiers != KeyModifiers.None || !double.IsFinite(e.DeltaY) ||
            e.DeltaY == 0 || !Enum.IsDefined(e.DeltaUnit))
        {
            wheelRemainder = 0;
            return;
        }
        e.Handled = true;
        var notches = e.DeltaUnit == ScrollDeltaUnit.LogicalPixels ? e.DeltaY / 48 : e.DeltaY;
        if (notches > 0 && CurrentValue >= Maximum || notches < 0 && CurrentValue <= Minimum)
        {
            wheelRemainder = 0;
            return;
        }
        double delta;
        if (Step > 0)
        {
            wheelRemainder += notches;
            var steps = Math.Truncate(Math.Round(wheelRemainder, 9));
            wheelRemainder -= steps;
            if (steps == 0) return;
            delta = steps * Step;
        }
        else delta = notches * ((Maximum - Minimum) / 100);
        var next = SliderSupport.Snap(CurrentValue + delta, Minimum, Maximum, Step);
        if (!value.Set(next)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }
}
