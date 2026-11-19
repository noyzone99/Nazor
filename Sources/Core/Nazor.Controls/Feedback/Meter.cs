using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How good the value of a <see cref="Meter" /> is, written as the <c>quality</c> attribute.</summary>
public enum MeterQuality
{
    /// <summary>The value is in the preferred region (<c>optimal</c>).</summary>
    Optimal,
    /// <summary>The value is acceptable but outside the preferred region (<c>suboptimal</c>).</summary>
    Suboptimal,
    /// <summary>The value is at the opposite end from the preferred region (<c>worst</c>).</summary>
    Worst,
}

/// <summary>
/// A read-only measurement within a known range, such as disk usage, battery charge or password strength; use a
/// <see cref="ProgressBar" /> for the completion of a task. <see cref="Low" />, <see cref="High" /> and
/// <see cref="Optimum" /> split the range into regions as the HTML meter does, and the region of the value is written as
/// the <c>quality</c> attribute (<see cref="Quality" />) so themes can color it. The parts are <c>MeterLabel</c>,
/// <c>MeterValue</c> (with <see cref="ShowValue" />) and <c>MeterTrack</c>, which holds <c>MeterFill</c>, or with
/// <see cref="Segments" /> one <c>MeterSegment</c> per unit of capacity, each with the <c>filled</c> attribute. The
/// element publishes <c>--nz-meter-fraction</c> (0 to 1) and the <c>orientation</c> attribute.
/// </summary>
public sealed class Meter : Control
{
    /// <summary>Gets or sets the measurement, clamped to the range.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Gets or sets the lower bound of the range; 0 by default.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the upper bound of the range; 1 by default.</summary>
    [Parameter] public double Maximum { get; set; } = 1;

    /// <summary>Gets or sets the upper end of the low region; null uses <see cref="Minimum" />.</summary>
    [Parameter] public double? Low { get; set; }

    /// <summary>Gets or sets the lower end of the high region; null uses <see cref="Maximum" />.</summary>
    [Parameter] public double? High { get; set; }

    /// <summary>Gets or sets the preferred value; null uses the middle of the range.</summary>
    [Parameter] public double? Optimum { get; set; }

    /// <summary>Gets or sets the number of discrete units drawn, such as the bars of a signal indicator; 0 is continuous.</summary>
    [Parameter] public int Segments { get; set; }

    /// <summary>Gets or sets the direction in which the meter fills.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets the visible label, which also names the meter for assistive technologies.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets whether the value part shows the measured percentage of the range.</summary>
    [Parameter] public bool ShowValue { get; set; }

    /// <summary>Gets or sets the text of the value part instead of the percentage, such as "42 GB of 256 GB".</summary>
    [Parameter] public string? ValueText { get; set; }

    /// <summary>Gets or sets the size step, which themes use for the thickness.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets the measured fraction of the range, from 0 to 1.</summary>
    public double Fraction => RangeMath.Fraction(Value, Minimum, Maximum);

    /// <summary>Gets the region of the current value.</summary>
    public MeterQuality Quality
    {
        get
        {
            var (low, high, optimum) = Thresholds();
            var value = Math.Clamp(Value, Minimum, Maximum);
            if (optimum < low) return value < low ? MeterQuality.Optimal : value <= high ? MeterQuality.Suboptimal : MeterQuality.Worst;
            if (optimum > high) return value > high ? MeterQuality.Optimal : value >= low ? MeterQuality.Suboptimal : MeterQuality.Worst;
            return value >= low && value <= high ? MeterQuality.Optimal : MeterQuality.Suboptimal;
        }
    }

    /// <summary>Gets the NSS element name emitted by Meter.</summary>
    protected override string ElementName => "Meter";

    /// <summary>Gets the fraction variable and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-meter-fraction: " + Css.Number(Fraction), Style);

    private (double Low, double High, double Optimum) Thresholds()
    {
        var low = Math.Clamp(Low ?? Minimum, Minimum, Maximum);
        var high = Math.Clamp(High ?? Maximum, low, Maximum);
        var optimum = Math.Clamp(Optimum ?? Minimum / 2 + Maximum / 2, Minimum, Maximum);
        return (low, high, optimum);
    }

    /// <summary>Validates the range, the thresholds and the look.</summary>
    protected override void OnParametersSet()
    {
        RangeMath.Validate(Value, Minimum, Maximum);
        if (Low is { } low && !double.IsFinite(low)) throw new ArgumentOutOfRangeException(nameof(Low));
        if (High is { } high && !double.IsFinite(high)) throw new ArgumentOutOfRangeException(nameof(High));
        if (Optimum is { } optimum && !double.IsFinite(optimum)) throw new ArgumentOutOfRangeException(nameof(Optimum));
        if (Segments < 0) throw new ArgumentOutOfRangeException(nameof(Segments), "The number of segments cannot be negative.");
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    /// <summary>Adds the meter role, the range, the thresholds and the quality.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var (low, high, optimum) = Thresholds();
        builder.AddAttribute(10, "role", "meter");
        RangeMath.AddRange(builder, 11, Value, Minimum, Maximum);
        builder.AddAttribute(14, "quality", Quality switch { MeterQuality.Optimal => "optimal", MeterQuality.Suboptimal => "suboptimal", _ => "worst" });
        builder.AddAttribute(15, "meter-low", low);
        builder.AddAttribute(16, "meter-high", high);
        builder.AddAttribute(17, "meter-optimum", optimum);
        builder.AddAttribute(18, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(19, "segmented", Segments > 0);
        builder.AddAttribute(20, "size", Vocabulary.Name(Size));
        if (AccessibleName is null) builder.AddAttribute(21, "accessible-name", Label);
    }

    /// <summary>Adds the label, the value and the track with its fill or segments.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Label is not null)
        {
            builder.OpenElement(1000, "MeterLabel");
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.AddContent(1002, Label);
            builder.CloseElement();
        }
        if (ShowValue)
        {
            builder.OpenElement(1010, "MeterValue");
            builder.AddAttribute(1011, "semantic-hidden", true);
            builder.AddContent(1012, ValueText ?? FeedbackParts.Percent(Fraction));
            builder.CloseElement();
        }
        builder.OpenElement(1020, "MeterTrack");
        builder.AddAttribute(1021, "semantic-hidden", true);
        if (Segments > 0)
        {
            var filled = FeedbackParts.Filled(Fraction, Segments);
            for (var i = 0; i < Segments; i++)
            {
                builder.OpenElement(1030, "MeterSegment");
                builder.SetKey(i);
                builder.AddAttribute(1031, "filled", i < filled);
                builder.CloseElement();
            }
        }
        else
        {
            builder.OpenElement(1040, "MeterFill");
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
