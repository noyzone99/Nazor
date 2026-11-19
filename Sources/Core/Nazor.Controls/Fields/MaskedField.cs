using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>What the <see cref="MaskedField.Value" /> of a <see cref="MaskedField" /> holds.</summary>
public enum MaskedValueFormat
{
    /// <summary>Only the characters typed into editable positions, such as <c>5551234567</c>.</summary>
    Raw,
    /// <summary>The text with the mask's literals, such as <c>(555) 123-4567</c>.</summary>
    Formatted,
}

/// <summary>
/// A text field that accepts only text fitting <see cref="Mask" /> (see <see cref="TextMask" /> for the mask characters).
/// The editor is an <see cref="EditableText" /> holding the formatted text typed so far: after every edit the text is fitted
/// into the mask again, so characters that do not fit are dropped, pasted text is filtered and literals are written as the
/// user types (the caret moves past them), while deleting a character also deletes the literals before it. The rest of the
/// mask follows the text in a <c>MaskedFieldPrompt</c> part, with <see cref="PromptCharacter" /> for each empty position,
/// while the field has the focus or some text. The engine owns the caret, so an edit in the middle that changes the
/// formatted text moves the caret to the end. <see cref="Value" /> (<c>@bind-Value</c>) is raw or formatted according to
/// <see cref="ValueFormat" />; <see cref="IsComplete" /> (the <c>complete</c> attribute) tells whether every required
/// position is filled. The parts are <c>MaskedFieldLeading</c>, <c>MaskedFieldInput</c> and <c>MaskedFieldPrompt</c>.
/// </summary>
public sealed class MaskedField : Control
{
    /// <inheritdoc />
    protected override int? ElementTabIndex => null;

    private readonly ControllableState<string> value = new("");
    private TextMask? mask;
    private EditableText? editor;
    private MaskResult result;
    private bool focused;
    private bool reportedComplete;

    /// <summary>Gets or sets the mask pattern, such as <c>(000) 000-0000</c>.</summary>
    [Parameter, EditorRequired] public string Mask { get; set; } = "";

    /// <summary>Gets or sets the value, raw or formatted according to <see cref="ValueFormat" />.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback invoked on every edit that changes the value; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets what <see cref="Value" /> holds.</summary>
    [Parameter] public MaskedValueFormat ValueFormat { get; set; }

    /// <summary>Gets or sets the character shown for each empty position; an underscore by default.</summary>
    [Parameter] public char PromptCharacter { get; set; } = '_';

    /// <summary>Gets or sets whether the rest of the mask is shown after the text; true by default.</summary>
    [Parameter] public bool ShowPrompt { get; set; } = true;

    /// <summary>Gets or sets the hint shown while the field is empty and has no focus.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets the callback invoked when the mask becomes complete or incomplete.</summary>
    [Parameter] public EventCallback<bool> IsCompleteChanged { get; set; }

    /// <summary>Gets or sets whether editing is blocked while selection and copying remain available.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets content shown before the text, such as an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets whether the editor takes the focus when it appears.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the formatted text in the editor.</summary>
    public string Text => result.Text;

    /// <summary>Gets the characters typed into editable positions.</summary>
    public string RawValue => result.Raw;

    /// <summary>Gets the formatted value, with the trailing literals once complete.</summary>
    public string FormattedValue => result.Formatted;

    /// <summary>Gets whether every required position is filled.</summary>
    public bool IsComplete => result.IsComplete;

    /// <summary>Gets the editor element, which receives the focus.</summary>
    public ElementReference InputElement => editor?.Element ?? default;

    /// <summary>Gets the NSS element name emitted by MaskedField.</summary>
    protected override string ElementName => "MaskedField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private string Output => ValueFormat == MaskedValueFormat.Raw ? result.Raw : result.Formatted;

    /// <summary>Parses the mask and synchronizes the text with the value.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(ValueFormat)) throw new ArgumentOutOfRangeException(nameof(ValueFormat));
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (char.IsControl(PromptCharacter)) throw new ArgumentOutOfRangeException(nameof(PromptCharacter));
        var reparse = mask?.Pattern != Mask;
        if (reparse) mask = new TextMask(Mask);
        if (value.Sync(Value ?? "") || reparse)
        {
            result = mask!.Apply(value.Value, PromptCharacter);
            reportedComplete = result.IsComplete;
        }
        else result = mask!.Apply(result.Text, PromptCharacter);
    }

    /// <summary>Adds the frame states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "readonly", ReadOnly);
        builder.AddAttribute(14, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(15, "empty", result.Text.Length == 0);
        builder.AddAttribute(16, "complete", result.IsComplete);
        builder.AddAttribute(17, "__internal_onpointerdown#field", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
    }

    /// <summary>Adds the editor and the prompt.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "MaskedFieldLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        var prompting = ShowPrompt && (focused || result.Text.Length > 0);
        builder.OpenComponent<EditableText>(1010);
        builder.AddComponentParameter(1011, nameof(EditableText.As), "MaskedFieldInput");
        builder.AddComponentParameter(1012, nameof(EditableText.Value), result.Text);
        builder.AddComponentParameter(1013, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(1014, nameof(EditableText.Placeholder), prompting ? null : Placeholder);
        builder.AddComponentParameter(1015, nameof(EditableText.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1016, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1017, nameof(EditableText.Required), Required);
        builder.AddComponentParameter(1018, nameof(EditableText.Invalid), Invalid);
        builder.AddComponentParameter(1019, nameof(EditableText.AutoFocus), AutoFocus);
        builder.AddComponentParameter(1020, nameof(EditableText.InputMode), Mask.All(c => c is '0' or '9' || !char.IsLetter(c) && c != '*') ? "number" : null);
        builder.AddComponentParameter(1021, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1022, nameof(Control.AccessibleDescription), AccessibleDescription ?? Mask);
        builder.AddComponentParameter(1023, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1024, nameof(Control.DescribedBy), DescribedBy);
        builder.AddComponentParameter(1025, nameof(Control.TabIndex), TabIndex);
        builder.AddComponentParameter(1026, nameof(Control.Id), Id is null ? null : Id + "-input");
        builder.AddComponentParameter(1027, "__internal_onfocus#mask", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(true)));
        builder.AddComponentParameter(1028, "__internal_onblur#mask", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(false)));
        builder.AddComponentReferenceCapture(1029, component => editor = (EditableText)component);
        builder.CloseComponent();
        if (!prompting || result.Prompt.Length == 0) return;
        builder.OpenElement(1040, "MaskedFieldPrompt");
        builder.AddAttribute(1041, "semantic-hidden", true);
        builder.AddContent(1042, result.Prompt);
        builder.CloseElement();
    }

    /// <summary>Replaces the text as typing or pasting would, fitting it into the mask, and reports the value.</summary>
    /// <param name="input">The text to fit.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task SetTextAsync(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return editor is not null ? editor.SetTextAsync(input) : TypedAsync(input);
    }

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => InputElement.FocusAsync(preventScroll);

    private async Task TypedAsync(string typed)
    {
        result = mask!.Apply(typed, PromptCharacter);
        StateHasChanged();
        if (result.Text != typed && editor is not null) await editor.SetTextAsync(result.Text);
        if (value.Set(Output))
        {
            await ValueChanged.InvokeAsync(Output);
            await Events.RaiseChangeAsync(Element, Output, commit: false);
        }
        if (reportedComplete == result.IsComplete) return;
        reportedComplete = result.IsComplete;
        await IsCompleteChanged.InvokeAsync(result.IsComplete);
    }

    private void SetFocused(bool next)
    {
        if (focused == next) return;
        focused = next;
        StateHasChanged();
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element) ? Task.CompletedTask : FocusAsync(preventScroll: true);
}
