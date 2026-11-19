using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The shared frame of the pickers that edit one value as text and offer a popup to choose it: a frame named after the
/// control with the <c>appearance</c>, <c>size</c>, <c>disabled</c>, <c>readonly</c>, <c>invalid</c>, <c>empty</c> and
/// <c>expanded</c> attributes, the <c>{name}Input</c> editor (role <c>combobox</c>) that parses the typed text in
/// <see cref="Culture" />, an optional <c>{name}Clear</c> button, the <c>{name}Button</c> that opens the popup and the
/// <c>{name}Popup</c> surface. Typing a text that parses updates <see cref="Value" /> at once; a text that does not
/// parse, or a value outside the limits, keeps the value and marks the frame and the editor <c>invalid</c>
/// (<see cref="IsTextInvalid" />). Enter or leaving the editor rewrites a valid text in the display format. Alt+Down or
/// F4 open the popup, Escape or Alt+Up close it. Inside a <see cref="Field" /> the editor is labeled and described by the
/// field.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public abstract class PickerFieldBase<TValue> : Control where TValue : struct
{
    /// <inheritdoc />
    protected override int? ElementTabIndex => null;

    private readonly ControllableState<TValue?> value = new(null);
    private readonly ControllableState<bool> open = new(false);
    private string text = "";
    private string? formatKey;
    private bool invalidText;
    private EditableText? editor;
    private PopoverContainer? popup;

    /// <summary>Gets or sets the value, or null when empty.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the value.</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Gets or sets the culture that formats and parses the text; the current culture by default.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Gets or sets the hint shown while the text is empty; null shows the expected pattern.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the picker rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the value cannot change while it can still be read and copied.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether a value is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state, combined with the parsing state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets whether a button clears the value when there is one.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the popup is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the popup opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current value.</summary>
    public TValue? CurrentValue => value.Value;

    /// <summary>Gets the text in the editor.</summary>
    public string Text => text;

    /// <summary>Gets whether the text in the editor does not parse to an allowed value.</summary>
    public bool IsTextInvalid => invalidText;

    /// <summary>Gets whether the popup is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the editor element, which receives the focus.</summary>
    public ElementReference InputElement => editor?.Element ?? default;

    /// <summary>Gets the culture in use.</summary>
    protected CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    /// <summary>Gets whether the picker rejects interaction, including through its field.</summary>
    protected bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Gets the accessible name of the button and of the popup.</summary>
    protected abstract string ButtonLabel { get; }

    /// <summary>Gets the pattern shown when there is no <see cref="Placeholder" />.</summary>
    protected virtual string? PatternHint => null;

    /// <summary>Gets a key that changes when the display format changes, so the text is rewritten.</summary>
    protected virtual string FormatKey => EffectiveCulture.Name;

    /// <summary>Formats a value for the editor.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The display text.</returns>
    protected abstract string FormatValue(TValue value);

    /// <summary>Parses the text typed in the editor.</summary>
    /// <param name="text">The trimmed, non-empty text.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns>true if the text parses.</returns>
    protected abstract bool TryParseText(string text, out TValue value);

    /// <summary>Tells whether a parsed value is within the limits.</summary>
    /// <param name="value">The value.</param>
    /// <returns>true if the value can be chosen.</returns>
    protected virtual bool IsAllowed(TValue value) => true;

    /// <summary>Renders the content of the popup.</summary>
    /// <param name="builder">The builder positioned inside the popup.</param>
    protected abstract void RenderPopup(RenderTreeBuilder builder);

    /// <summary>Validates the shared parameters and synchronizes the value, the text and the open state.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        var key = FormatKey;
        if (value.Sync(Value) || key != formatKey && !invalidText)
        {
            text = value.Value is { } v ? FormatValue(v) : "";
            invalidText = false;
        }
        formatKey = key;
        open.Sync(IsOpen);
        if (IsInactive || ReadOnly) open.Set(false);
    }

    /// <summary>Adds the frame states and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "readonly", ReadOnly);
        builder.AddAttribute(14, "invalid", Invalid || invalidText || Field?.Invalid == true);
        builder.AddAttribute(15, "empty", text.Length == 0);
        builder.AddAttribute(16, "expanded", open.Value);
        builder.AddAttribute(17, "__internal_onkeydown#picker", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(18, "__internal_onpointerdown#picker", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
    }

    /// <summary>Adds the editor, the clear button, the popup button and the popup.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<EditableText>(1000);
        builder.AddComponentParameter(1001, nameof(EditableText.As), ElementName + "Input");
        builder.AddComponentParameter(1002, nameof(EditableText.Value), text);
        builder.AddComponentParameter(1003, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, TextChangedAsync));
        builder.AddComponentParameter(1004, nameof(EditableText.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, CommitText));
        builder.AddComponentParameter(1005, nameof(EditableText.Placeholder), Placeholder ?? PatternHint);
        builder.AddComponentParameter(1006, nameof(EditableText.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1007, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1008, nameof(EditableText.Required), Required);
        builder.AddComponentParameter(1009, nameof(EditableText.Invalid), Invalid || invalidText);
        builder.AddComponentParameter(1010, nameof(EditableText.Role), "combobox");
        builder.AddComponentParameter(1011, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1012, nameof(Control.AccessibleDescription), AccessibleDescription);
        builder.AddComponentParameter(1013, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1014, nameof(Control.DescribedBy), DescribedBy);
        builder.AddComponentParameter(1015, nameof(Control.TabIndex), TabIndex);
        builder.AddComponentParameter(1016, nameof(Control.Id), Id is null ? null : Id + "-input");
        builder.AddComponentParameter(1017, nameof(Control.AdditionalAttributes), new Dictionary<string, object?> { ["expanded"] = open.Value });
        builder.AddComponentReferenceCapture(1018, component => editor = (EditableText)component);
        builder.CloseComponent();
        if (Clearable && text.Length > 0 && !IsInactive && !ReadOnly)
        {
            builder.OpenElement(1020, ElementName + "Clear");
            builder.AddAttribute(1021, "role", "button");
            builder.AddAttribute(1022, "accessible-name", ControlStrings.Get("Clear"));
            builder.AddAttribute(1023, "glyph", "minus");
            builder.AddAttribute(1024, "tabindex", -1);
            builder.AddAttribute(1025, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClearAsync));
            builder.CloseElement();
        }
        builder.OpenElement(1030, ElementName + "Button");
        builder.AddAttribute(1031, "role", "button");
        builder.AddAttribute(1032, "accessible-name", ButtonLabel);
        builder.AddAttribute(1033, "glyph", "chevron-down");
        builder.AddAttribute(1034, "tabindex", -1);
        builder.AddAttribute(1035, "focusable", !IsInactive && !ReadOnly);
        builder.AddAttribute(1036, "disabled", IsInactive || ReadOnly);
        builder.AddAttribute(1037, "expanded", open.Value);
        builder.AddAttribute(1038, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        builder.AddAttribute(1039, "combo-expander", true);
        builder.CloseElement();
        builder.OpenComponent<PopoverContainer>(1040);
        builder.AddComponentParameter(1041, nameof(PopoverContainer.As), ElementName + "Popup");
        builder.AddComponentParameter(1042, nameof(PopoverContainer.IsOpen), open.Value);
        builder.AddComponentParameter(1043, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => v ? Task.CompletedTask : SetOpenAsync(false)));
        builder.AddComponentParameter(1044, nameof(PopoverContainer.Anchor), Element);
        builder.AddComponentParameter(1045, nameof(PopoverContainer.Role), "dialog");
        builder.AddComponentParameter(1046, nameof(Control.AccessibleName), ButtonLabel);
        builder.AddComponentParameter(1047, nameof(PopoverContainer.ReturnFocus), editor?.Element);
        builder.AddComponentParameter(1048, nameof(PopoverContainer.InitialFocus), (Func<ElementReference?>)InitialFocus);
        builder.AddComponentParameter(1049, nameof(PopoverContainer.ChildContent), (RenderFragment)RenderPopup);
        builder.AddComponentReferenceCapture(1050, component => popup = (PopoverContainer)component);
        builder.CloseComponent();
    }

    /// <summary>Chooses the element focused when the popup opens: the first element holding a Tab stop inside it.</summary>
    /// <returns>The element, or null to focus the first focusable one.</returns>
    protected virtual ElementReference? InitialFocus() =>
        popup?.Element.Element is { } surface && ControlTree.Descendants(surface).FirstOrDefault(n => n.Attributes.GetValueOrDefault("tabindex") is 0 && !n.IsDisabled) is { } stop
            ? new ElementReference(stop) : null;

    /// <summary>Opens the popup unless the picker is disabled or read-only.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => IsInactive || ReadOnly ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the popup.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => InputElement.FocusAsync(preventScroll);

    /// <summary>Changes the value as the user would, rewrites the text and reports the change.</summary>
    /// <param name="next">The new value, or null to clear.</param>
    /// <param name="close">Whether the popup closes and the focus returns to the editor.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    protected async Task SelectAsync(TValue? next, bool close)
    {
        if (IsInactive || ReadOnly) return;
        text = next is { } v ? FormatValue(v) : "";
        invalidText = false;
        if (close) await SetOpenAsync(false);
        StateHasChanged();
        await ReportAsync(next);
    }

    private Task ToggleAsync() => open.Value ? CloseAsync() : OpenAsync();

    private async Task SetOpenAsync(bool next)
    {
        if (!open.Set(next)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(next);
    }

    private async Task ReportAsync(TValue? next)
    {
        if (!value.Set(next)) return;
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }

    private async Task TextChangedAsync(string typed)
    {
        text = typed;
        var trimmed = typed.Trim();
        if (trimmed.Length == 0)
        {
            invalidText = false;
            StateHasChanged();
            await ReportAsync(null);
            return;
        }
        if (TryParseText(trimmed, out var parsed) && IsAllowed(parsed))
        {
            invalidText = false;
            StateHasChanged();
            await ReportAsync(parsed);
            return;
        }
        invalidText = true;
        StateHasChanged();
    }

    private void CommitText(ChangeEventArgs e)
    {
        if (invalidText) return;
        var formatted = value.Value is { } v ? FormatValue(v) : "";
        if (formatted == text) return;
        text = formatted;
        StateHasChanged();
    }

    private async Task ClearAsync()
    {
        await SelectAsync(null, close: false);
        await FocusAsync();
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element)
            ? Task.CompletedTask : InputElement.FocusAsync(preventScroll: true);

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (IsInactive || e.Handled) return Task.CompletedTask;
        var alt = (e.Modifiers & KeyModifiers.Alt) != 0;
        if (e.Key == "F4" || e.Key == "ArrowDown" && alt)
        {
            e.Handled = true;
            return open.Value ? CloseAsync() : OpenAsync();
        }
        if (e.Key == "ArrowUp" && alt && open.Value)
        {
            e.Handled = true;
            return CloseAsync();
        }
        return Task.CompletedTask;
    }
}
