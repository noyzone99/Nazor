using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The progress of a task as a horizontal bar. <see cref="Value" /> between <see cref="Minimum" /> and
/// <see cref="Maximum" /> is the completed work; null means the amount is unknown (the <c>indeterminate</c> attribute,
/// and no <c>range-value</c> for assistive technologies). The parts are <c>ProgressBarLabel</c> (the visible
/// <see cref="Label" />), <c>ProgressBarValue</c> (the percentage, with <see cref="ShowValue" />) and
/// <c>ProgressBarTrack</c>, which holds <c>ProgressBarBuffer</c> (<see cref="BufferValue" />) and
/// <c>ProgressBarFill</c>, an optional second <c>ProgressBarPulse</c> for themed indeterminate motion, or with
/// <see cref="Segments" /> one <c>ProgressBarSegment</c> per block, each with the
/// <c>filled</c> attribute when the progress covers it. The element publishes the variables
/// <c>--nz-progress-fraction</c> and <c>--nz-progress-buffer</c> (0 to 1) and the <c>intent</c> and <c>size</c>
/// attributes.
/// </summary>
public sealed class ProgressBar : Control
{
    /// <summary>Gets or sets the completed work, or null when it is unknown.</summary>
    [Parameter] public double? Value { get; set; }

    /// <summary>Gets or sets the value of no progress; 0 by default.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the value of complete progress; 100 by default.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>
    /// Gets or sets a secondary amount ahead of the value, such as the downloaded part of a video being played, or null.
    /// </summary>
    [Parameter] public double? BufferValue { get; set; }

    /// <summary>Gets or sets the visible label, which also names the bar for assistive technologies.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets whether the value part shows the completed percentage (nothing while indeterminate).</summary>
    [Parameter] public bool ShowValue { get; set; }

    /// <summary>Gets or sets the text of the value part instead of the percentage, such as "3 of 7 files".</summary>
    [Parameter] public string? ValueText { get; set; }

    /// <summary>Gets or sets the meaning of the bar's color, such as success or danger.</summary>
    [Parameter] public Intent Intent { get; set; } = Intent.Accent;

    /// <summary>Gets or sets the size step, which themes use for the thickness.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the number of blocks of a segmented bar; 0 draws a continuous bar.</summary>
    [Parameter] public int Segments { get; set; }

    /// <summary>Gets whether the amount of completed work is unknown.</summary>
    public bool IsIndeterminate => Value is null;

    /// <summary>Gets the completed fraction from 0 to 1, or 0 while indeterminate.</summary>
    public double Fraction => Value is { } value ? RangeMath.Fraction(value, Minimum, Maximum) : 0;

    /// <summary>Gets the NSS element name emitted by ProgressBar.</summary>
    protected override string ElementName => "ProgressBar";

    /// <summary>Gets the fraction variables and the caller's declarations.</summary>
    protected override string? ElementStyle =>
        Css.Join("--nz-progress-fraction: " + Css.Number(Fraction) + "; --nz-progress-buffer: " + Css.Number(BufferFraction), Style);

    private double BufferFraction => BufferValue is { } buffer ? Math.Max(Fraction, RangeMath.Fraction(buffer, Minimum, Maximum)) : Fraction;

    /// <summary>Validates the range, the look and the number of segments.</summary>
    protected override void OnParametersSet()
    {
        FeedbackParts.ValidateRange(Value, Minimum, Maximum, nameof(Value));
        if (BufferValue is { } buffer && !double.IsFinite(buffer)) throw new ArgumentOutOfRangeException(nameof(BufferValue));
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (Segments < 0) throw new ArgumentOutOfRangeException(nameof(Segments), "The number of segments cannot be negative.");
    }

    /// <summary>Adds the progressbar role, the range and the look attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "progressbar");
        builder.AddAttribute(11, "range-min", Minimum);
        builder.AddAttribute(12, "range-max", Maximum);
        if (Value is { } value) builder.AddAttribute(13, "range-value", Math.Clamp(value, Minimum, Maximum));
        builder.AddAttribute(14, "indeterminate", IsIndeterminate);
        builder.AddAttribute(15, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(16, "size", Vocabulary.Name(Size));
        builder.AddAttribute(17, "segmented", Segments > 0);
        builder.AddAttribute(18, "complete", Value is not null && Fraction >= 1);
        if (AccessibleName is null) builder.AddAttribute(19, "accessible-name", Label);
    }

    /// <summary>Adds the label, the value and the track with its fill, buffer or segments.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Label is not null)
        {
            builder.OpenElement(1000, "ProgressBarLabel");
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.AddContent(1002, Label);
            builder.CloseElement();
        }
        if (ShowValue)
        {
            builder.OpenElement(1010, "ProgressBarValue");
            builder.AddAttribute(1011, "semantic-hidden", true);
            builder.AddContent(1012, ValueText ?? (IsIndeterminate ? null : FeedbackParts.Percent(Fraction)));
            builder.CloseElement();
        }
        builder.OpenElement(1020, "ProgressBarTrack");
        builder.AddAttribute(1021, "semantic-hidden", true);
        if (Segments > 0)
        {
            var filled = IsIndeterminate ? 0 : FeedbackParts.Filled(Fraction, Segments);
            for (var i = 0; i < Segments; i++)
            {
                builder.OpenElement(1030, "ProgressBarSegment");
                builder.SetKey(i);
                builder.AddAttribute(1031, "filled", i < filled);
                builder.AddAttribute(1032, "indeterminate", IsIndeterminate);
                builder.CloseElement();
            }
        }
        else
        {
            if (BufferValue is not null && !IsIndeterminate)
            {
                builder.OpenElement(1040, "ProgressBarBuffer");
                builder.CloseElement();
            }
            builder.OpenElement(1050, "ProgressBarFill");
            builder.AddAttribute(1051, "indeterminate", IsIndeterminate);
            builder.CloseElement();
            if (IsIndeterminate)
            {
                builder.OpenElement(1060, "ProgressBarPulse");
                builder.CloseElement();
            }
        }
        builder.CloseElement();
    }
}
