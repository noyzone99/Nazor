using Nazor.Rendering;
using System.Globalization;

namespace Nazor.Controls.Primitives;

/// <summary>The displayed scalar and its theme-defined start/end boundary states, independent of an application's target value.</summary>
/// <param name="Value">The currently displayed value.</param>
/// <param name="AtStart">Whether the displayed value is within the theme's boundary tolerance of Minimum.</param>
/// <param name="AtEnd">Whether the displayed value is within the theme's boundary tolerance of Maximum.</param>
public readonly record struct NumberPresentationState(double Value, bool AtStart, bool AtEnd);

/// <summary>
/// Presents a bounded scalar through a typed child fragment and the host's scene clock. The theme's NSS
/// number-motion policy owns interpolation; none and reduced motion apply the target immediately. Initial motion
/// starts at Minimum. Target updates sample the displayed value before retargeting. The physical element publishes
/// --nz-presented-number and number-at-start/number-at-end; the owning application keeps its actual accessible value.
/// </summary>
public sealed class NumberPresenter : Control, IDisposable
{
    private NumberPresentation? presentation;

    /// <summary>Gets or sets the physical element name; NumberPresenter by default.</summary>
    [Parameter] public string As { get; set; } = "NumberPresenter";

    /// <summary>Gets or sets the finite target value; presentation clamps it to Minimum and Maximum.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Gets or sets the finite lower bound and initial displayed value; zero by default.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>Gets or sets the finite upper bound; one by default. The nonnegative range must also be finite.</summary>
    [Parameter] public double Maximum { get; set; } = 1;

    /// <summary>Gets or sets the content rendered for each displayed sample; keep stateful child keys stable.</summary>
    [Parameter] public RenderFragment<NumberPresentationState>? ChildContent { get; set; }

    /// <summary>Gets the current presentation sample. Before the first layout it starts at Minimum.</summary>
    public NumberPresentationState Presentation => presentation is { } value ? new(value.Value, value.AtStart, value.AtEnd) : new(Minimum, true, Minimum == Maximum);

    /// <summary>Gets the physical element name.</summary>
    protected override string ElementName => As;

    /// <summary>Gets the presented-number variable and caller declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-presented-number: " + Presentation.Value.ToString("R", CultureInfo.InvariantCulture), Style);

    /// <summary>Validates finite bounds and target, and updates the pending presentation target.</summary>
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Value)) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!double.IsFinite(Minimum)) throw new ArgumentOutOfRangeException(nameof(Minimum));
        if (!double.IsFinite(Maximum) || Maximum < Minimum || !double.IsFinite(Maximum - Minimum)) throw new ArgumentOutOfRangeException(nameof(Maximum));
        presentation ??= new(StateHasChanged);
        presentation.SetTarget(Value, Minimum, Maximum);
    }

    /// <summary>Adds the scene-owned presentation marker and displayed boundary states.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "number-presentation", presentation);
        builder.AddAttribute(11, "number-at-start", Presentation.AtStart);
        builder.AddAttribute(12, "number-at-end", Presentation.AtEnd);
        builder.AddAttribute(13, "number-target", presentation?.Target ?? Value);
        builder.AddAttribute(14, "number-minimum", Minimum);
        builder.AddAttribute(15, "number-maximum", Maximum);
    }

    /// <summary>Renders the child content with the displayed sample.</summary>
    /// <param name="builder">The builder receiving the content.</param>
    protected override void AddContent(RenderTreeBuilder builder) => builder.AddContent(1000, ChildContent?.Invoke(Presentation));

    /// <summary>Releases the presentation callback; the scene drops its owner on removal or the next update.</summary>
    public void Dispose() => presentation?.Dispose();
}
