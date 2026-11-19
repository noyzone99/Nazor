using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>When a slider shows its value next to the thumb, written as the <c>value-label</c> attribute.</summary>
public enum ValueLabelMode
{
    /// <summary>The value is not shown.</summary>
    None,
    /// <summary>The value is shown while the slider has the keyboard focus.</summary>
    OnFocus,
    /// <summary>The value is always shown.</summary>
    Always,
}

internal static class SliderSupport
{
    private const int MaximumTicks = 1000;

    internal static void Validate(double minimum, double maximum, double step, double largeStep, double tickFrequency)
    {
        if (!double.IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum must be finite.");
        if (!double.IsFinite(maximum) || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum must be finite and not below the minimum.");
        if (!double.IsFinite(step) || step < 0) throw new ArgumentOutOfRangeException(nameof(step), "The step must be zero or positive.");
        if (!double.IsFinite(largeStep) || largeStep < 0) throw new ArgumentOutOfRangeException(nameof(largeStep), "The large step must be zero or positive.");
        if (!double.IsFinite(tickFrequency) || tickFrequency < 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency), "The tick frequency must be zero or positive.");
        if (tickFrequency > 0 && (maximum - minimum) / tickFrequency > MaximumTicks)
            throw new ArgumentOutOfRangeException(nameof(tickFrequency), "The tick frequency produces too many ticks.");
    }

    internal static double Snap(double value, double minimum, double maximum, double step)
    {
        if (step > 0) value = minimum + Math.Round((value - minimum) / step) * step;
        return Math.Clamp(value, minimum, maximum);
    }

    internal static List<double> Ticks(double minimum, double maximum, double frequency)
    {
        var ticks = new List<double>();
        if (frequency <= 0) return ticks;
        for (var i = 0; ; i++)
        {
            var tick = minimum + i * frequency;
            if (tick > maximum + frequency * 1e-9) break;
            ticks.Add(Math.Min(tick, maximum));
        }
        if (ticks[^1] < maximum - frequency * 1e-9) ticks.Add(maximum);
        return ticks;
    }

    internal static string Format(double value, string? format)
    {
        if (string.IsNullOrEmpty(format)) return value.ToString("0.##", CultureInfo.CurrentCulture);
        return format.Contains('{') ? string.Format(CultureInfo.CurrentCulture, format, value) : value.ToString(format, CultureInfo.CurrentCulture);
    }

    internal static string Name(ValueLabelMode mode) => mode switch { ValueLabelMode.OnFocus => "on-focus", ValueLabelMode.Always => "always", _ => "none" };

    internal static void AddTicks(RenderTreeBuilder builder, int sequence, string prefix, double minimum, double maximum, double frequency,
        bool reversed, bool labels, string? format)
    {
        if (frequency <= 0) return;
        builder.OpenElement(sequence, prefix + "Ticks");
        builder.AddAttribute(sequence + 1, "semantic-hidden", true);
        foreach (var tick in Ticks(minimum, maximum, frequency))
        {
            var fraction = RangeMath.Fraction(tick, minimum, maximum);
            builder.OpenElement(sequence + 2, prefix + "Tick");
            builder.SetKey(tick);
            builder.AddAttribute(sequence + 3, "style", "--nz-slider-tick-position: " + Css.Number(reversed ? 1 - fraction : fraction));
            if (labels)
            {
                builder.OpenElement(sequence + 4, prefix + "TickLabel");
                builder.AddContent(sequence + 5, Format(tick, format));
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}

/// <summary>
/// Chooses a number between <see cref="Minimum" /> and <see cref="Maximum" /> by dragging a thumb along a track,
/// clicking the track, or with the keyboard: the arrows move by <see cref="Step" />, Page Up and Page Down by
/// <see cref="LargeStep" />, Home and End to the ends. <see cref="Value" /> (<c>@bind-Value</c>) and <c>oninput</c>
/// follow the value while it changes, <c>onchange</c> comes when the user lets go. The element has the <c>slider</c> role
/// with the range attributes; the parts are <c>SliderTrack</c> with <c>SliderFill</c>, <c>SliderThumb</c> with
/// <c>SliderValueLabel</c> (<see cref="ValueLabel" />), and <c>SliderTicks</c> with <c>SliderTick</c> and
/// <c>SliderTickLabel</c>. The variable <c>--nz-slider-fraction</c> (0 to 1) is the value's place in the range and
/// <c>--nz-slider-position</c> the thumb's place from the start of the track (left, or bottom when vertical), which
/// differs when <see cref="IsDirectionReversed" />; each tick has <c>--nz-slider-tick-position</c>.
/// </summary>
public sealed class Slider : Control
{
    private readonly ControllableState<double> value = new(0);

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

    /// <summary>Gets or sets the direction of the track; a vertical slider grows upwards.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets whether the minimum is at the end of the track (right, or top when vertical).</summary>
    [Parameter] public bool IsDirectionReversed { get; set; }

    /// <summary>Gets or sets the distance between tick marks, or zero for none.</summary>
    [Parameter] public double TickFrequency { get; set; }

    /// <summary>Gets or sets whether the tick marks show their values.</summary>
    [Parameter] public bool ShowTickLabels { get; set; }

    /// <summary>Gets or sets when the value is shown next to the thumb.</summary>
    [Parameter] public ValueLabelMode ValueLabel { get; set; }

    /// <summary>Gets or sets how the value and the tick labels are written: a numeric format such as <c>0.0</c> or
    /// <c>P0</c>, or a composite format such as <c>{0} °C</c>; null writes up to two decimals.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the slider rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current value, kept within the range.</summary>
    public double CurrentValue => Math.Clamp(value.Value, Minimum, Maximum);

    /// <summary>Gets the NSS element name emitted by Slider.</summary>
    protected override string ElementName => "Slider";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private double Fraction => RangeMath.Fraction(value.Value, Minimum, Maximum);

    /// <summary>Gets the fraction variables and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join(
        "--nz-slider-fraction: " + Css.Number(Fraction) + "; --nz-slider-position: " + Css.Number(IsDirectionReversed ? 1 - Fraction : Fraction), Style);

    /// <summary>Validates the parameters and synchronizes the value.</summary>
    protected override void OnParametersSet()
    {
        SliderSupport.Validate(Minimum, Maximum, Step, LargeStep, TickFrequency);
        if (!double.IsFinite(Value)) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(ValueLabel)) throw new ArgumentOutOfRangeException(nameof(ValueLabel));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        value.Sync(Value);
    }

    /// <summary>Adds the role, the range and the field relationships.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "slider");
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "focusable", !IsInactive);
        RangeMath.AddRange(builder, 13, value.Value, Minimum, Maximum);
        builder.AddAttribute(16, "range-step", Step);
        builder.AddAttribute(17, "range-large-step", LargeStep);
        builder.AddAttribute(18, "vertical", Orientation == Orientation.Vertical);
        builder.AddAttribute(19, "range-reversed", IsDirectionReversed);
        builder.AddAttribute(20, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(21, "reversed", IsDirectionReversed);
        builder.AddAttribute(22, "value-label", SliderSupport.Name(ValueLabel));
        builder.AddAttribute(23, "ticks", TickFrequency > 0);
        builder.AddAttribute(24, "tick-labels", TickFrequency > 0 && ShowTickLabels);
        builder.AddAttribute(25, "size", Vocabulary.Name(Size));
        builder.AddAttribute(26, "__internal_oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        if (Id is null && Field is { } field) builder.AddAttribute(27, "id", field.ControlId);
        builder.AddAttribute(28, "required", Field?.Required == true);
        builder.AddAttribute(29, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 30, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the track, the ticks and the thumb.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "SliderTrack");
        builder.AddAttribute(1001, "range-track", true);
        builder.OpenElement(1002, "SliderFill");
        builder.CloseElement();
        builder.CloseElement();
        SliderSupport.AddTicks(builder, 1010, "Slider", Minimum, Maximum, TickFrequency, IsDirectionReversed, ShowTickLabels, Format);
        builder.OpenElement(1020, "SliderThumb");
        if (ValueLabel != ValueLabelMode.None)
        {
            builder.OpenElement(1021, "SliderValueLabel");
            builder.AddAttribute(1022, "semantic-hidden", true);
            builder.AddContent(1023, SliderSupport.Format(CurrentValue, Format));
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private Task InputAsync(ChangeEventArgs e)
    {
        if (IsInactive || e.Value is not double next || !value.Set(next)) return Task.CompletedTask;
        StateHasChanged();
        return ValueChanged.InvokeAsync(next);
    }
}

/// <summary>
/// Chooses a range between <see cref="Minimum" /> and <see cref="Maximum" /> with two thumbs, such as a price filter:
/// <see cref="LowerValue" /> and <see cref="UpperValue" /> (<c>@bind-LowerValue</c>, <c>@bind-UpperValue</c>) stay at
/// least <see cref="MinimumRange" /> apart. Each thumb is its own <c>slider</c> element (<c>RangeSliderHandle</c> with
/// the <c>thumb</c> attribute <c>lower</c> or <c>upper</c>) with the keyboard of a <see cref="Slider" />; a press on the
/// track moves the nearer thumb. The element <c>RangeSlider</c> is a <c>group</c>; the parts are
/// <c>RangeSliderRail</c> holding <c>RangeSliderTrack</c> with <c>RangeSliderFill</c> and the two handles, each with
/// its <c>RangeSliderThumb</c> (and <c>RangeSliderValueLabel</c>), and <c>RangeSliderTicks</c> with
/// <c>RangeSliderTick</c> and <c>RangeSliderTickLabel</c>. The variables <c>--nz-range-lower</c> and
/// <c>--nz-range-upper</c> are the values' places in the range, <c>--nz-range-start</c> and <c>--nz-range-end</c> the
/// filled part of the track from its start, and each handle has <c>--nz-range-thumb-position</c>, the thumb's place
/// within the handle; <c>oninput</c> and <c>onchange</c> come from the handle that changed.
/// </summary>
public sealed class RangeSlider : Control
{
    private const double MinimumRegion = 0.0005;
    private readonly ControllableState<double> lower = new(0);
    private readonly ControllableState<double> upper = new(0);

    /// <summary>Gets or sets the lower end of the range.</summary>
    [Parameter] public double LowerValue { get; set; }

    /// <summary>Gets or sets the callback invoked while the user moves the lower thumb.</summary>
    [Parameter] public EventCallback<double> LowerValueChanged { get; set; }

    /// <summary>Gets or sets the upper end of the range.</summary>
    [Parameter] public double UpperValue { get; set; } = 100;

    /// <summary>Gets or sets the callback invoked while the user moves the upper thumb.</summary>
    [Parameter] public EventCallback<double> UpperValueChanged { get; set; }

    /// <summary>Gets or sets the lowest value.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the highest value.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>Gets or sets the change made by an arrow key; zero allows any value.</summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>Gets or sets the change made by Page Up and Page Down.</summary>
    [Parameter] public double LargeStep { get; set; } = 10;

    /// <summary>Gets or sets the smallest distance kept between the two values.</summary>
    [Parameter] public double MinimumRange { get; set; }

    /// <summary>Gets or sets the direction of the track; a vertical range slider grows upwards.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets whether the minimum is at the end of the track (right, or top when vertical).</summary>
    [Parameter] public bool IsDirectionReversed { get; set; }

    /// <summary>Gets or sets the distance between tick marks, or zero for none.</summary>
    [Parameter] public double TickFrequency { get; set; }

    /// <summary>Gets or sets whether the tick marks show their values.</summary>
    [Parameter] public bool ShowTickLabels { get; set; }

    /// <summary>Gets or sets when the values are shown next to the thumbs.</summary>
    [Parameter] public ValueLabelMode ValueLabel { get; set; }

    /// <summary>Gets or sets how the values and the tick labels are written, as <see cref="Slider.Format" />.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Gets or sets the accessible name of the lower thumb; null uses a localized "Minimum".</summary>
    [Parameter] public string? LowerLabel { get; set; }

    /// <summary>Gets or sets the accessible name of the upper thumb; null uses a localized "Maximum".</summary>
    [Parameter] public string? UpperLabel { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the range slider rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current lower value, kept within the range.</summary>
    public double CurrentLowerValue => Math.Clamp(lower.Value, Minimum, Maximum);

    /// <summary>Gets the current upper value, kept within the range and not below the lower value.</summary>
    public double CurrentUpperValue => Math.Clamp(upper.Value, CurrentLowerValue, Maximum);

    /// <summary>Gets the NSS element name emitted by RangeSlider.</summary>
    protected override string ElementName => "RangeSlider";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private bool Vertical => Orientation == Orientation.Vertical;

    private double Position(double fraction) => IsDirectionReversed ? 1 - fraction : fraction;

    /// <summary>Gets the range variables and the caller's declarations.</summary>
    protected override string? ElementStyle
    {
        get
        {
            var low = RangeMath.Fraction(CurrentLowerValue, Minimum, Maximum);
            var high = RangeMath.Fraction(CurrentUpperValue, Minimum, Maximum);
            var (start, end) = (Math.Min(Position(low), Position(high)), Math.Max(Position(low), Position(high)));
            return Css.Join($"--nz-range-lower: {Css.Number(low)}; --nz-range-upper: {Css.Number(high)}; --nz-range-start: {Css.Number(start)}; --nz-range-end: {Css.Number(end)}", Style);
        }
    }

    /// <summary>Validates the parameters and synchronizes the values.</summary>
    protected override void OnParametersSet()
    {
        SliderSupport.Validate(Minimum, Maximum, Step, LargeStep, TickFrequency);
        if (!double.IsFinite(LowerValue)) throw new ArgumentOutOfRangeException(nameof(LowerValue));
        if (!double.IsFinite(UpperValue)) throw new ArgumentOutOfRangeException(nameof(UpperValue));
        if (!double.IsFinite(MinimumRange) || MinimumRange < 0 || MinimumRange > Maximum - Minimum) throw new ArgumentOutOfRangeException(nameof(MinimumRange));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(ValueLabel)) throw new ArgumentOutOfRangeException(nameof(ValueLabel));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        lower.Sync(LowerValue);
        upper.Sync(UpperValue);
    }

    /// <summary>Adds the group role, the look attributes and the field relationships.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(13, "reversed", IsDirectionReversed);
        builder.AddAttribute(14, "value-label", SliderSupport.Name(ValueLabel));
        builder.AddAttribute(15, "ticks", TickFrequency > 0);
        builder.AddAttribute(16, "tick-labels", TickFrequency > 0 && ShowTickLabels);
        builder.AddAttribute(17, "size", Vocabulary.Name(Size));
        if (Id is null && Field is { } field) builder.AddAttribute(18, "id", field.ControlId);
        Field?.ApplyTo(builder, 19, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the rail with the track and the two handles, and the ticks.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var low = Position(RangeMath.Fraction(CurrentLowerValue, Minimum, Maximum));
        var high = Position(RangeMath.Fraction(CurrentUpperValue, Minimum, Maximum));
        var middle = Math.Clamp((low + high) / 2, MinimumRegion, 1 - MinimumRegion);
        var lowerFirst = low <= high;
        builder.OpenElement(1000, "RangeSliderRail");
        builder.OpenElement(1001, "RangeSliderTrack");
        builder.OpenElement(1002, "RangeSliderFill");
        builder.CloseElement();
        builder.CloseElement();
        var lowerOnTop = Math.Abs(low - high) < 1e-9 && low > 0.5;
        AddHandle(builder, 1100, isLower: true, low, lowerFirst ? 0 : middle, lowerFirst ? middle : 1, lowerOnTop);
        AddHandle(builder, 1200, isLower: false, high, lowerFirst ? middle : 0, lowerFirst ? 1 : middle, !lowerOnTop);
        builder.CloseElement();
        SliderSupport.AddTicks(builder, 1300, "RangeSlider", Minimum, Maximum, TickFrequency, IsDirectionReversed, ShowTickLabels, Format);
    }

    private void AddHandle(RenderTreeBuilder builder, int sequence, bool isLower, double position, double from, double to, bool onTop)
    {
        var size = to - from;
        var (start, extent) = Vertical ? ("bottom", "height") : ("left", "width");
        var current = isLower ? CurrentLowerValue : CurrentUpperValue;
        builder.OpenElement(sequence, "RangeSliderHandle");
        builder.SetKey(isLower ? "lower" : "upper");
        builder.AddAttribute(sequence + 1, "role", "slider");
        builder.AddAttribute(sequence + 2, "thumb", isLower ? "lower" : "upper");
        builder.AddAttribute(sequence + 3, "accessible-name", (isLower ? LowerLabel : UpperLabel) ?? ControlStrings.Get(isLower ? "RangeLower" : "RangeUpper"));
        builder.AddAttribute(sequence + 4, "focusable", !IsInactive);
        builder.AddAttribute(sequence + 5, "disabled", IsInactive);
        RangeMath.AddRange(builder, sequence + 6, current, Minimum, Maximum);
        builder.AddAttribute(sequence + 9, "range-step", Step);
        builder.AddAttribute(sequence + 10, "range-large-step", LargeStep);
        builder.AddAttribute(sequence + 11, "vertical", Vertical);
        builder.AddAttribute(sequence + 12, "range-reversed", IsDirectionReversed);
        builder.AddAttribute(sequence + 13, "on-top", onTop);
        builder.AddAttribute(sequence + 14, "required", Field?.Required == true);
        builder.AddAttribute(sequence + 15, "invalid", Field?.Invalid == true);
        builder.AddAttribute(sequence + 16, "style",
            $"{start}: {Percent(from)}; {extent}: {Percent(size)}; --nz-range-thumb-position: {Css.Number((position - from) / size)}");
        builder.AddAttribute(sequence + 17, "__internal_oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => InputAsync(isLower, e)));
        builder.OpenElement(sequence + 20, "RangeSliderHandleTrack");
        builder.AddAttribute(sequence + 21, "range-track", true);
        builder.AddAttribute(sequence + 22, "semantic-hidden", true);
        builder.AddAttribute(sequence + 23, "style", $"{start}: {Percent(-from / size)}; {extent}: {Percent(1 / size)}");
        builder.CloseElement();
        builder.OpenElement(sequence + 30, "RangeSliderThumb");
        if (ValueLabel != ValueLabelMode.None)
        {
            builder.OpenElement(sequence + 31, "RangeSliderValueLabel");
            builder.AddAttribute(sequence + 32, "semantic-hidden", true);
            builder.AddContent(sequence + 33, SliderSupport.Format(current, Format));
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    }

    private static string Percent(double fraction) => Css.Number(fraction * 100) + "%";

    private async Task InputAsync(bool isLower, ChangeEventArgs e)
    {
        if (IsInactive || e.Value is not double next) return;
        if (isLower)
        {
            var limit = Math.Max(Minimum, CurrentUpperValue - MinimumRange);
            next = Math.Clamp(next, Minimum, limit);
            if (!lower.Set(next)) { StateHasChanged(); return; }
            StateHasChanged();
            await LowerValueChanged.InvokeAsync(next);
        }
        else
        {
            var limit = Math.Min(Maximum, CurrentLowerValue + MinimumRange);
            next = Math.Clamp(next, limit, Maximum);
            if (!upper.Set(next)) { StateHasChanged(); return; }
            StateHasChanged();
            await UpperValueChanged.InvokeAsync(next);
        }
    }
}
