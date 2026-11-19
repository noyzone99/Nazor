using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// A bare text editor without frame or look, the editing core of every field: caret and selection, clipboard, undo,
/// input methods and the text context menu come from the engine. <see cref="Value" /> supports <c>@bind-Value</c> and
/// changes on every edit; <see cref="OnChange" /> reports the committed value (Enter on one line, or focus loss). The
/// element is named by <see cref="As" /> so a field can place it among its own parts. Inside a <see cref="Field" /> it is
/// named and described by the field automatically.
/// </summary>
public sealed class EditableText : Control
{
    private readonly ControllableState<string> text = new("");

    /// <summary>Gets or sets the element name, <c>EditableText</c> by default.</summary>
    [Parameter] public string As { get; set; } = "EditableText";

    /// <summary>Gets or sets the text; null shows as empty.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback invoked on every edit.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets the hint drawn while the text is empty; it does not replace a label.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether editing is blocked while selection and copying remain available.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the editor rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets the maximum number of UTF-16 characters; zero or negative means unlimited.</summary>
    [Parameter] public int MaxLength { get; set; }

    /// <summary>Gets or sets whether Enter inserts a line break.</summary>
    [Parameter] public bool Multiline { get; set; }

    /// <summary>Gets or sets whether the text is masked and protected from copying and assistive technologies.</summary>
    [Parameter] public bool Password { get; set; }

    /// <summary>Gets or sets the character drawn for each masked character.</summary>
    [Parameter] public char MaskCharacter { get; set; } = '●';

    /// <summary>Gets or sets whether the editor takes the focus when it appears or when its overlay opens.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Gets or sets the range stepped by the Up, Down, Page Up and Page Down keys, for numeric editors; null disables
    /// stepping. The owner converts the text and supplies the current numeric value.
    /// </summary>
    [Parameter] public SpinRange? Spin { get; set; }

    /// <summary>Gets or sets the input intent hint for platform input methods, such as <c>number</c> or <c>email</c>.</summary>
    [Parameter] public string? InputMode { get; set; }

    /// <summary>Gets or sets the accessible role; <c>textbox</c> by default, <c>combobox</c> or <c>spinbutton</c> for owners.</summary>
    [Parameter] public string Role { get; set; } = "textbox";

    /// <summary>Gets or sets the callback invoked with every raw edit or step.</summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

    /// <summary>Gets or sets the callback invoked with the committed value.</summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current text.</summary>
    public string Text => text.Value;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Synchronizes the text with the parameter.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(As)) throw new ArgumentException("EditableText requires an element name.", nameof(As));
        text.Sync(Value ?? "");
    }

    /// <summary>Adds the editing attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "value", text.Value);
        builder.AddAttribute(12, "focusable", !IsInactive);
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "readonly", ReadOnly);
        builder.AddAttribute(15, "placeholder-text", Placeholder);
        builder.AddAttribute(16, "maxlength", MaxLength > 0 ? MaxLength : null);
        builder.AddAttribute(17, "multiline", Multiline);
        builder.AddAttribute(18, "password", Password);
        builder.AddAttribute(19, "password-char", Password ? MaskCharacter.ToString() : null);
        builder.AddAttribute(20, "empty", text.Value.Length == 0);
        builder.AddAttribute(21, "autofocus", AutoFocus);
        builder.AddAttribute(22, "input-type", InputMode);
        builder.AddAttribute(23, "__internal_oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        if (OnChange.HasDelegate) builder.AddAttribute(24, "__internal_onchange", OnChange);
        if (Spin is { } spin)
        {
            builder.AddAttribute(25, "spin", true);
            builder.AddAttribute(26, "range-min", spin.Minimum);
            builder.AddAttribute(27, "range-max", Math.Max(spin.Minimum, spin.Maximum));
            builder.AddAttribute(28, "range-value", spin.Value);
            builder.AddAttribute(29, "range-step", spin.Step);
            builder.AddAttribute(30, "range-large-step", spin.LargeStep);
        }
        if (Id is null && Field is { } field) builder.AddAttribute(31, "id", field.ControlId);
        builder.AddAttribute(32, "required", Required || Field?.Required == true);
        builder.AddAttribute(33, "invalid", Invalid || Field?.Invalid == true);
        Field?.ApplyTo(builder, 40, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the displayed text, masked for passwords.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder) =>
        builder.AddContent(1000, Password ? new string(MaskCharacter, text.Value.Length) : text.Value);

    /// <summary>Replaces the text as an edit would and reports it.</summary>
    /// <param name="value">The new text.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetTextAsync(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!text.Set(value)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(value);
    }

    private async Task InputAsync(ChangeEventArgs e)
    {
        if (IsInactive) return;
        if (e.Value is string value && text.Set(value)) await ValueChanged.InvokeAsync(value);
        else if (e.Value is double) StateHasChanged();
        await OnInput.InvokeAsync(e);
    }
}

/// <summary>The numeric range stepped by the keys of an <see cref="EditableText" /> with <see cref="EditableText.Spin" />.</summary>
/// <param name="Value">The current numeric value of the text.</param>
/// <param name="Minimum">The lower bound.</param>
/// <param name="Maximum">The upper bound.</param>
/// <param name="Step">The change made by Up and Down.</param>
/// <param name="LargeStep">The change made by Page Up and Page Down.</param>
public readonly record struct SpinRange(double Value, double Minimum, double Maximum, double Step = 1, double LargeStep = 10);
