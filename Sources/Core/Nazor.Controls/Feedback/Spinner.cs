using Nazor.Rendering;
using Nazor.Controls.Primitives;

namespace Nazor.Controls;

/// <summary>
/// A ring-shaped progress indicator for work in progress, drawn with the engine's <c>ring</c> glyph. <see cref="Value" />
/// between <see cref="Minimum" /> and <see cref="Maximum" /> fills that fraction of the ring; null means the amount is
/// unknown (the <c>indeterminate</c> attribute), using the theme's loading presentation. Basic keeps a quarter ring
/// without motion. The parts are <c>SpinnerIndicator</c>, which stacks <c>SpinnerTrack</c> (the full ring behind) and
/// <c>SpinnerRing</c> (the arc, with <c>ring-fraction</c>), and <c>SpinnerLabel</c>. A theme can display
/// <c>SpinnerMark</c> with its four <c>SpinnerPetal</c> parts and the determinate percentage in <c>SpinnerValue</c>.
/// <c>SpinnerCheck</c> is a styleable vector completion mark. The target fraction is published as
/// <c>--nz-spinner-fraction</c>; both track and value use full-circle glyphs, with the value's NSS stroke trim selecting
/// the displayed fraction. Themes may animate that trim without delaying the accessible range value.
/// Without a <see cref="Label" /> it is named "Loading".
/// </summary>
public sealed class Spinner : Control
{
    private static readonly IReadOnlyDictionary<string, object?> Decorative = new Dictionary<string, object?> { ["semantic-hidden"] = true };
    /// <summary>Gets or sets the completed work, or null when it is unknown.</summary>
    [Parameter] public double? Value { get; set; }

    /// <summary>Gets or sets the value of no progress; 0 by default.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the value of complete progress; 100 by default.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>Gets or sets the size step of the ring.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the visible label shown next to the ring, which also names the spinner.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets the meaning of the ring's color.</summary>
    [Parameter] public Intent Intent { get; set; } = Intent.Accent;

    /// <summary>Gets whether the amount of completed work is unknown.</summary>
    public bool IsIndeterminate => Value is null;

    /// <summary>Gets the fraction of the ring drawn: the completed fraction, or a quarter while indeterminate.</summary>
    public double RingFraction => Value is { } value ? RangeMath.Fraction(value, Minimum, Maximum) : 0.25;

    /// <summary>Gets the NSS element name emitted by Spinner.</summary>
    protected override string ElementName => "Spinner";

    /// <summary>Gets the target fraction variable and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-spinner-fraction: " + Css.Number(IsIndeterminate ? 0 : RingFraction), Style);

    /// <summary>Validates the range and the look.</summary>
    protected override void OnParametersSet()
    {
        FeedbackParts.ValidateRange(Value, Minimum, Maximum, nameof(Value));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
    }

    /// <summary>Adds the progressbar role, actual range, look and empty/complete attributes independently of visual interpolation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "progressbar");
        builder.AddAttribute(11, "range-min", Minimum);
        builder.AddAttribute(12, "range-max", Maximum);
        if (Value is { } value) builder.AddAttribute(13, "range-value", Math.Clamp(value, Minimum, Maximum));
        builder.AddAttribute(14, "indeterminate", IsIndeterminate);
        builder.AddAttribute(15, "size", Vocabulary.Name(Size));
        builder.AddAttribute(16, "intent", Vocabulary.Name(Intent));
        if (AccessibleName is null) builder.AddAttribute(17, "accessible-name", Label ?? ControlStrings.Get("Loading"));
        builder.AddAttribute(18, "complete", !IsIndeterminate && RingFraction >= 1);
        builder.AddAttribute(19, "empty", !IsIndeterminate && RingFraction <= 0);
    }

    /// <summary>Adds the track, the ring and the label.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<NumberPresenter>(1000);
        builder.AddComponentParameter(1001, nameof(NumberPresenter.As), "SpinnerIndicator");
        builder.AddComponentParameter(1002, nameof(NumberPresenter.Value), IsIndeterminate ? 0d : RingFraction);
        builder.AddComponentParameter(1003, nameof(Control.AdditionalAttributes), Decorative);
        builder.AddComponentParameter(1004, nameof(NumberPresenter.ChildContent), (RenderFragment<NumberPresentationState>)(state => content => BuildIndicatorContent(content, state)));
        builder.CloseComponent();
        if (Label is null) return;
        builder.OpenElement(1010, "SpinnerLabel");
        builder.AddAttribute(1011, "semantic-hidden", true);
        builder.AddContent(1012, Label);
        builder.CloseElement();
    }

    private void BuildIndicatorContent(RenderTreeBuilder builder, NumberPresentationState state)
    {
        builder.OpenElement(0, "SpinnerTrack");
        builder.AddAttribute(1, "glyph", "ring");
        builder.AddAttribute(2, "ring-fraction", 1d);
        builder.CloseElement();
        builder.OpenElement(10, "SpinnerRing");
        builder.AddAttribute(11, "glyph", "ring");
        builder.AddAttribute(12, "ring-fraction", 1d);
        builder.AddAttribute(13, "indeterminate", IsIndeterminate);
        builder.CloseElement();
        builder.OpenElement(20, "SpinnerCheck");
        builder.AddAttribute(21, "glyph", "check");
        builder.CloseElement();
        // Themes can use these parts to replace the arc with their own loading mark.
        builder.OpenElement(30, "SpinnerMark");
        for (var petal = 1; petal <= 4; petal++)
        {
            builder.OpenElement(31, "SpinnerPetal");
            builder.SetKey(petal);
            builder.AddAttribute(32, "petal", petal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.CloseElement();
        }
        builder.CloseElement();
        if (!IsIndeterminate)
        {
            builder.OpenElement(40, "SpinnerValue");
            builder.AddContent(41, FeedbackParts.Percent(state.Value));
            builder.CloseElement();
        }
    }
}
