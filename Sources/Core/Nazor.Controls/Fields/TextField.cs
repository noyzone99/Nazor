using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Controls;

/// <summary>How a text field draws its frame, written as the <c>appearance</c> attribute of fields.</summary>
public enum FieldAppearance
{
    /// <summary>A full outline around the field.</summary>
    Outline,
    /// <summary>A tinted background without outline.</summary>
    Filled,
    /// <summary>Only a line under the text.</summary>
    Underline,
    /// <summary>No frame, for fields embedded in other surfaces such as a grid cell.</summary>
    Plain,
}

/// <summary>The kind of text a <see cref="TextField" /> expects, as a hint to platform input methods.</summary>
public enum TextFieldType
{
    /// <summary>Any text.</summary>
    Text,
    /// <summary>An email address.</summary>
    Email,
    /// <summary>A web address.</summary>
    Url,
    /// <summary>A telephone number.</summary>
    Telephone,
    /// <summary>Numeric text; for typed numbers with stepping use a number field.</summary>
    Number,
}

/// <summary>
/// The frame, side content and counter shared by single-line and multi-line text fields. The frame element is named
/// after the control; the editor is its <c>{name}Input</c> part.
/// </summary>
public abstract class TextFieldBase : Control
{
    /// <inheritdoc />
    protected override int? ElementTabIndex => null;

    private readonly ControllableState<string> text = new("");
    private EditableText? editor;
    private StylePartSet? inputPart;

    /// <summary>Gets or sets explicit outer public names to which this field forwards its exposed input part.</summary>
    [Parameter] public StylePartSet? InputParts { get; set; }

    /// <summary>Gets or sets the text; null shows as empty.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback invoked on every edit; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets the hint shown while the text is empty; it does not replace a label.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether editing is blocked while selection and copying remain available.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets the maximum number of characters; zero or negative means unlimited.</summary>
    [Parameter] public int MaxLength { get; set; }

    /// <summary>Gets or sets whether a <c>{name}Counter</c> part shows the length, and the limit when there is one.</summary>
    [Parameter] public bool ShowCount { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets content shown before the text, such as an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets content shown after the text, such as a button.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets whether the editor takes the focus when it appears.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Gets or sets the callback invoked with every raw edit.</summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

    /// <summary>Gets or sets the callback invoked with the committed value.</summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current text.</summary>
    public string Text => text.Value;

    /// <summary>Gets the editor element, which receives the focus.</summary>
    public ElementReference InputElement => editor?.Element ?? default;

    /// <summary>Gets whether Enter inserts a line break.</summary>
    protected abstract bool Multiline { get; }

    /// <summary>Gets whether the field rejects interaction, including through its field.</summary>
    protected bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Synchronizes the text with the parameter.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        text.Sync(Value ?? "");
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
        builder.AddAttribute(15, "empty", text.Value.Length == 0);
        builder.AddAttribute(16, "multiline", Multiline);
        builder.AddAttribute(17, "__internal_onpointerdown#field", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
    }

    /// <summary>Adds the side content, the editor and the trailing parts.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, ElementName + "Leading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        AddBeforeInput(builder);
        builder.OpenComponent<EditableText>(1010);
        builder.AddComponentParameter(1011, nameof(EditableText.As), ElementName + "Input");
        builder.AddComponentParameter(1012, nameof(EditableText.Value), text.Value);
        builder.AddComponentParameter(1013, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, ChangedAsync));
        builder.AddComponentParameter(1014, nameof(EditableText.Placeholder), Placeholder);
        builder.AddComponentParameter(1015, nameof(EditableText.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1016, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1017, nameof(EditableText.Required), Required);
        builder.AddComponentParameter(1018, nameof(EditableText.Invalid), Invalid);
        builder.AddComponentParameter(1019, nameof(EditableText.MaxLength), MaxLength);
        builder.AddComponentParameter(1020, nameof(EditableText.Multiline), Multiline);
        builder.AddComponentParameter(1021, nameof(EditableText.AutoFocus), AutoFocus);
        builder.AddComponentParameter(1022, nameof(EditableText.OnInput), OnInput);
        builder.AddComponentParameter(1023, nameof(EditableText.OnChange), OnChange);
        builder.AddComponentParameter(1024, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1025, nameof(Control.AccessibleDescription), AccessibleDescription);
        builder.AddComponentParameter(1026, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1027, nameof(Control.DescribedBy), DescribedBy);
        builder.AddComponentParameter(1028, nameof(Control.TabIndex), TabIndex);
        builder.AddComponentParameter(1029, nameof(Control.Id), Id is null ? null : Id + "-input");
        var parts = inputPart ??= StylePartSet.Empty.Expose(this, "input");
        builder.AddComponentParameter(1043, nameof(Control.Parts), InputParts is null ? parts : parts.Export(this, "input", InputParts));
        AddEditorParameters(builder);
        if (AdditionalAttributes?.TryGetValue("onfocus", out var focus) == true)
            builder.AddComponentParameter(1040, "onfocus", focus);
        if (AdditionalAttributes?.TryGetValue("onblur", out var blur) == true)
            builder.AddComponentParameter(1041, "onblur", blur);
        builder.AddComponentReferenceCapture(1042, component => editor = (EditableText)component);
        builder.CloseComponent();
        AddAfterInput(builder);
        if (Trailing is not null)
        {
            builder.OpenElement(1100, ElementName + "Trailing");
            builder.AddContent(1101, Trailing);
            builder.CloseElement();
        }
        if (ShowCount)
        {
            builder.OpenElement(1110, ElementName + "Counter");
            builder.AddAttribute(1111, "role", "text");
            builder.AddContent(1112, MaxLength > 0 ? $"{text.Value.Length}/{MaxLength}" : text.Value.Length.ToString(System.Globalization.CultureInfo.CurrentCulture));
            builder.CloseElement();
        }
    }

    /// <summary>Adds parts between the leading content and the editor, with sequence numbers from 1002 to 1009.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddBeforeInput(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds extra editor parameters, with sequence numbers from 1030 to 1039.</summary>
    /// <param name="builder">The builder positioned on the editor component.</param>
    protected virtual void AddEditorParameters(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds parts between the editor and the trailing content, with sequence numbers from 1050 to 1099.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddAfterInput(RenderTreeBuilder builder)
    {
    }

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => InputElement.FocusAsync(preventScroll);

    /// <summary>Replaces the text as an edit would and reports it.</summary>
    /// <param name="value">The new text.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetTextAsync(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (editor is not null) await editor.SetTextAsync(value);
        else await ChangedAsync(value);
    }

    /// <summary>Called after an edit changed the text and before <see cref="ValueChanged" /> reports it.</summary>
    /// <param name="previous">The text before the edit.</param>
    /// <param name="value">The new text.</param>
    /// <returns>A task that completes when the derived field has reacted.</returns>
    protected virtual Task TextChangedAsync(string previous, string value) => Task.CompletedTask;

    private async Task ChangedAsync(string value)
    {
        var previous = text.Value;
        if (!text.Set(value)) return;
        StateHasChanged();
        await TextChangedAsync(previous, value);
        await ValueChanged.InvokeAsync(value);
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element)
            ? Task.CompletedTask : InputElement.FocusAsync(preventScroll: true);
}

/// <summary>
/// A single-line text field: a frame with optional leading content, prefix, the editor, suffix, a clear button,
/// trailing content and a character counter. The parts are <c>TextFieldLeading</c>, <c>TextFieldPrefix</c>,
/// <c>TextFieldInput</c>, <c>TextFieldSuffix</c>, <c>TextFieldClear</c>, <c>TextFieldTrailing</c> and
/// <c>TextFieldCounter</c>. Inside a <see cref="Field" /> it is labeled and described automatically.
/// </summary>
public sealed class TextField : TextFieldBase
{
    /// <summary>Gets or sets the kind of text expected, as a hint to input methods.</summary>
    [Parameter] public TextFieldType Type { get; set; }

    /// <summary>Gets or sets fixed text shown before the editable text, such as a currency symbol.</summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>Gets or sets fixed text shown after the editable text, such as a unit.</summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>Gets or sets whether a button clears the text when there is some.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Gets the NSS element name emitted by TextField.</summary>
    protected override string ElementName => "TextField";

    /// <summary>Gets that Enter does not insert a line break.</summary>
    protected override bool Multiline => false;

    /// <summary>Validates the type.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Type)) throw new ArgumentOutOfRangeException(nameof(Type));
    }

    /// <summary>Adds the prefix.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddBeforeInput(RenderTreeBuilder builder)
    {
        if (Prefix is null) return;
        builder.OpenElement(1002, "TextFieldPrefix");
        builder.AddAttribute(1003, "semantic-hidden", true);
        builder.AddContent(1004, Prefix);
        builder.CloseElement();
    }

    /// <summary>Adds the input intent.</summary>
    /// <param name="builder">The builder positioned on the editor component.</param>
    protected override void AddEditorParameters(RenderTreeBuilder builder) =>
        builder.AddComponentParameter(1030, nameof(EditableText.InputMode), Type == TextFieldType.Text ? null : Type.ToString().ToLowerInvariant());

    /// <summary>Adds the suffix and the clear button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAfterInput(RenderTreeBuilder builder)
    {
        if (Suffix is not null)
        {
            builder.OpenElement(1050, "TextFieldSuffix");
            builder.AddAttribute(1051, "semantic-hidden", true);
            builder.AddContent(1052, Suffix);
            builder.CloseElement();
        }
        if (!Clearable || Text.Length == 0 || IsInactive || ReadOnly) return;
        builder.OpenElement(1060, "TextFieldClear");
        builder.AddAttribute(1061, "role", "button");
        builder.AddAttribute(1062, "accessible-name", ControlStrings.Get("Clear"));
        builder.AddAttribute(1063, "glyph", "minus");
        builder.AddAttribute(1064, "tabindex", -1);
        builder.AddAttribute(1065, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClearAsync));
        builder.CloseElement();
    }

    private async Task ClearAsync()
    {
        await SetTextAsync("");
        await FocusAsync();
    }
}

/// <summary>
/// A multi-line text field where Enter inserts a line break. <see cref="Rows" /> sets the initial height in lines
/// (the <c>--nz-textarea-rows</c> variable); with <see cref="Resizable" /> a <c>ResizeHandle</c> part lets the user
/// change the height. The parts are <c>TextAreaLeading</c>, <c>TextAreaInput</c>, <c>TextAreaTrailing</c> and
/// <c>TextAreaCounter</c>.
/// </summary>
public sealed class TextArea : TextFieldBase
{
    private double? height;
    private double measured;

    /// <summary>Gets or sets the initial number of visible lines.</summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>Gets or sets whether the user can change the height with a handle.</summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>Gets the NSS element name emitted by TextArea.</summary>
    protected override string ElementName => "TextArea";

    /// <summary>Gets that Enter inserts a line break.</summary>
    protected override bool Multiline => true;

    /// <summary>Gets the number of lines, the user height and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-textarea-rows: " + Rows.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + (height is { } h ? "; height: " + Css.Px(h) : ""), Style);

    /// <summary>Validates the number of lines.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Rows < 1) throw new ArgumentOutOfRangeException(nameof(Rows));
    }

    /// <summary>Adds the frame states and, when resizable, the height measurement.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        if (Resizable) builder.AddAttribute(30, "__internal_onresize#textarea", EventCallback.Factory.Create<ResizeEventArgs>(this, e => measured = e.Height));
    }

    /// <summary>Adds the resize handle.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAfterInput(RenderTreeBuilder builder)
    {
        if (!Resizable || IsInactive) return;
        builder.OpenComponent<ResizeHandle>(1050);
        builder.AddComponentParameter(1051, nameof(ResizeHandle.Edge), ResizeEdge.Bottom);
        builder.AddComponentParameter(1052, nameof(ResizeHandle.Height), height ?? measured);
        builder.AddComponentParameter(1053, nameof(ResizeHandle.MinSize), 24.0);
        builder.AddComponentParameter(1054, nameof(ResizeHandle.AccessibleName), ControlStrings.Get("Expand"));
        builder.AddComponentParameter(1055, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, value => { height = value.Height; StateHasChanged(); }));
        builder.CloseComponent();
    }
}
