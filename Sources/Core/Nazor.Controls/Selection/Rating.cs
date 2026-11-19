using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A score from zero to <see cref="Maximum" /> stars: <see cref="Value" /> (<c>@bind-Value</c>) changes with a click on
/// a star (on its first half with <see cref="AllowHalf" />), or with the keyboard: the arrows move by one star (half a
/// star with <see cref="AllowHalf" />), Home clears and End gives the maximum. Hovering previews the score. The element
/// has the <c>slider</c> role, announced as a rating, with the range attributes and a <c>value-text</c> such as "3 of
/// 5", also used as the accessible description. Each star is a <c>RatingItem</c> with the <c>fill</c> attribute
/// (<c>full</c>, <c>half</c> or <c>empty</c>) and, while hovering, <c>preview</c>; it holds <c>RatingItemSymbol</c>
/// (<see cref="EmptySymbol" />), <c>RatingItemFill</c> (<see cref="FullSymbol" />, clipped by themes for a half star)
/// and two <c>RatingItemZone</c> parts (<c>half</c> = <c>start</c> or <c>end</c>) that receive the pointer.
/// </summary>
public sealed class Rating : Control
{
    private readonly ControllableState<double> value = new(0);
    private double? preview;

    /// <summary>Gets or sets the score.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the score.</summary>
    [Parameter] public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Gets or sets the number of stars.</summary>
    [Parameter] public int Maximum { get; set; } = 5;

    /// <summary>Gets or sets whether the score can end with half a star.</summary>
    [Parameter] public bool AllowHalf { get; set; }

    /// <summary>Gets or sets whether a click on the current score clears it.</summary>
    [Parameter] public bool AllowClear { get; set; }

    /// <summary>Gets or sets whether the score is only shown: it stays focusable and announced but cannot change.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the rating rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the text of a full star.</summary>
    [Parameter] public string FullSymbol { get; set; } = "★";

    /// <summary>Gets or sets the text of an empty star.</summary>
    [Parameter] public string EmptySymbol { get; set; } = "☆";

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current score, kept within the range and rounded to whole or half stars.</summary>
    public double CurrentValue => Round(value.Value);

    /// <summary>Gets the score previewed by the pointer, or null.</summary>
    public double? PreviewValue => preview;

    /// <summary>Gets the NSS element name emitted by Rating.</summary>
    protected override string ElementName => "Rating";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private bool CanChange => !IsInactive && !ReadOnly;

    private double Round(double score)
    {
        var clamped = Math.Clamp(score, 0, Maximum);
        return AllowHalf || ReadOnly ? Math.Round(clamped * 2, MidpointRounding.AwayFromZero) / 2 : Math.Round(clamped, MidpointRounding.AwayFromZero);
    }

    /// <summary>Validates the parameters and synchronizes the score.</summary>
    protected override void OnParametersSet()
    {
        if (Maximum is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(Maximum), "The number of stars must be between 1 and 100.");
        if (!double.IsFinite(Value)) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        ArgumentNullException.ThrowIfNull(FullSymbol);
        ArgumentNullException.ThrowIfNull(EmptySymbol);
        value.Sync(Value);
        if (!CanChange) preview = null;
    }

    /// <summary>Adds the slider role, the range, the value text and the pointer handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var text = ControlStrings.Format("RatingValue", CurrentValue, Maximum);
        builder.AddAttribute(10, "role", "slider");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("Rating"));
        builder.AddAttribute(12, "focusable", !IsInactive);
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "readonly", ReadOnly);
        RangeMath.AddRange(builder, 15, CurrentValue, 0, Maximum);
        builder.AddAttribute(18, "range-step", AllowHalf ? 0.5 : 1.0);
        builder.AddAttribute(19, "range-large-step", 1.0);
        builder.AddAttribute(20, "value-text", text);
        if (AccessibleDescription is null) builder.AddAttribute(21, "accessible-description", text);
        builder.AddAttribute(22, "previewing", preview is not null);
        builder.AddAttribute(23, "size", Vocabulary.Name(Size));
        builder.AddAttribute(24, "allow-half", AllowHalf);
        builder.AddAttribute(25, "__internal_oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        builder.AddEventPreventDefaultAttribute(26, "onpointerdown", true);
        builder.AddAttribute(27, "__internal_onpointerleave#rating", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetPreview(null)));
        if (Id is null && Field is { } field) builder.AddAttribute(28, "id", field.ControlId);
        builder.AddAttribute(29, "required", Field?.Required == true);
        builder.AddAttribute(30, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 31, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the stars.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        for (var i = 1; i <= Maximum; i++)
        {
            var star = i;
            builder.OpenElement(1000, "RatingItem");
            builder.SetKey(star);
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.AddAttribute(1002, "fill", Fill(CurrentValue, star));
            builder.AddAttribute(1003, "preview", preview is { } previewed ? Fill(previewed, star) : null);
            builder.AddAttribute(1004, "position-in-set", star);
            builder.OpenElement(1010, "RatingItemSymbol");
            builder.AddContent(1011, EmptySymbol);
            builder.CloseElement();
            builder.OpenElement(1020, "RatingItemFill");
            builder.AddContent(1021, FullSymbol);
            builder.CloseElement();
            AddZone(builder, 1030, "start", AllowHalf ? star - 0.5 : star);
            AddZone(builder, 1040, "end", star);
            builder.CloseElement();
        }
    }

    private void AddZone(RenderTreeBuilder builder, int sequence, string half, double score)
    {
        builder.OpenElement(sequence, "RatingItemZone");
        builder.AddAttribute(sequence + 1, "half", half);
        builder.AddAttribute(sequence + 2, "__internal_onpointerenter#rating", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetPreview(score)));
        builder.AddAttribute(sequence + 3, "__internal_onpointerdown#rating", EventCallback.Factory.Create<PointerEventArgs>(this, e => PressAsync(e, score)));
        builder.CloseElement();
    }

    private static string Fill(double score, int star) => score >= star ? "full" : score >= star - 0.5 ? "half" : "empty";

    private void SetPreview(double? score)
    {
        if (!CanChange) score = null;
        if (preview == score) return;
        preview = score;
        StateHasChanged();
    }

    private async Task PressAsync(PointerEventArgs e, double score)
    {
        if (IsInactive || e.Button != PointerButton.Primary) return;
        if (Element.Element is { } element && (element.States & ElementStates.Focus) == 0) _ = Element.FocusAsync(preventScroll: true);
        if (ReadOnly) return;
        await SetValueAsync(AllowClear && score == CurrentValue ? 0 : score);
    }

    private Task InputAsync(ChangeEventArgs e) => CanChange && e.Value is double next ? SetValueAsync(next, raise: false) : Task.CompletedTask;

    /// <summary>Changes the score as a user action does and reports the change.</summary>
    /// <param name="score">The new score, rounded to whole or half stars and kept within the range.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task SetValueAsync(double score) => CanChange ? SetValueAsync(score, raise: true) : Task.CompletedTask;

    private async Task SetValueAsync(double score, bool raise)
    {
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));
        var next = Round(score);
        if (next == CurrentValue && value.Value == next) return;
        value.Set(next);
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        if (raise) await Events.RaiseChangeAsync(Element, next);
    }
}
