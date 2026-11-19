using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Records a keyboard shortcut. When the recorder receives the focus (or is clicked, or activated with Enter or Space
/// after a cancel) it starts recording and shows "Press a key combination": the next key that is not a modifier, with the
/// modifiers held, becomes <see cref="Value" /> (<c>@bind-Value</c>) and recording stops. While recording, the held
/// modifiers are shown as they are pressed, Escape cancels, and Tab or Shift+Tab without other modifiers still move the
/// focus so the keyboard is never trapped; every other key is captured and kept from shortcuts. With
/// <see cref="RequireModifier" /> a key without Control, Alt or Meta (other than F1–F24) is ignored. The parts are
/// <c>ShortcutFieldRecorder</c> (the focusable button, <c>pressed</c> while recording), its <c>ShortcutFieldValue</c>
/// holding a <see cref="ShortcutLabel" /> or a <c>ShortcutFieldPrompt</c>, and <c>ShortcutFieldClear</c>. Delete or
/// Backspace on the recorder when it is not recording clear the value.
/// </summary>
public sealed class ShortcutField : Control
{
    private readonly ControllableState<KeyGesture?> value = new(null);
    private bool recording;
    private KeyModifiers held;
    private ElementReference recorder;

    /// <summary>Gets or sets the recorded shortcut; null when none.</summary>
    [Parameter] public KeyGesture? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user records or clears a shortcut; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<KeyGesture?> ValueChanged { get; set; }

    /// <summary>Gets or sets whether a shortcut must include Control, Alt or Meta, unless its key is a function key.</summary>
    [Parameter] public bool RequireModifier { get; set; }

    /// <summary>Gets or sets the text shown when there is no shortcut and the field is not recording; "None" by default.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether a button clears the shortcut; true by default.</summary>
    [Parameter] public bool Clearable { get; set; } = true;

    /// <summary>Gets or sets whether the shortcut cannot be changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the field rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether a shortcut is required; validation is application-owned.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets the externally determined invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets how the frame is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the recorded shortcut.</summary>
    public KeyGesture? CurrentValue => value.Value;

    /// <summary>Gets whether the field is waiting for a key.</summary>
    public bool IsRecording => recording;

    /// <summary>Gets the NSS element name emitted by ShortcutField.</summary>
    protected override string ElementName => "ShortcutField";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private bool CanRecord => !IsInactive && !ReadOnly;

    /// <summary>Validates the parameters and synchronizes the shortcut.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        value.Sync(Value);
        if (!CanRecord) recording = false;
    }

    /// <summary>Gets whether a key name is a modifier key.</summary>
    /// <param name="key">The key name.</param>
    /// <returns>true for Shift, Control, Alt, Meta and the lock keys.</returns>
    public static bool IsModifierKey(string key) =>
        key is "Shift" or "Control" or "Ctrl" or "Alt" or "AltGraph" or "Meta" or "OS" or "Super" or "Win" or "CapsLock" or "NumLock" or "ScrollLock" or "Unidentified" or "";

    /// <summary>Adds the frame states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "readonly", ReadOnly);
        builder.AddAttribute(14, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(15, "empty", value.Value is null);
        builder.AddAttribute(16, "recording", recording);
    }

    /// <summary>Adds the recorder and the clear button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var shown = value.Value?.ToDisplayString();
        builder.OpenElement(1000, "ShortcutFieldRecorder");
        builder.AddAttribute(1001, "role", "button");
        builder.AddAttribute(1002, "focusable", !IsInactive);
        builder.AddAttribute(1003, "tabindex", TabIndex);
        builder.AddAttribute(1004, "pressed", recording);
        builder.AddAttribute(1005, "accessible-description", recording ? ControlStrings.Get("RecordingShortcut") : shown ?? Placeholder ?? ControlStrings.Get("NoShortcut"));
        builder.AddAttribute(1006, "required", Required || Field?.Required == true);
        builder.AddAttribute(1007, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(1008, "readonly", ReadOnly);
        if (Field is { } field)
        {
            if (Id is null) builder.AddAttribute(1009, "id", field.ControlId);
            field.ApplyTo(builder, 1010, LabeledBy, DescribedBy);
        }
        builder.AddAttribute(1012, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, StartRecording));
        builder.AddAttribute(1013, "__internal_onkeydown#shortcut", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(1014, "__internal_onkeyup#shortcut", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyUp));
        builder.AddAttribute(1015, "__internal_onfocus#shortcut", EventCallback.Factory.Create<FocusEventArgs>(this, StartRecording));
        builder.AddAttribute(1016, "__internal_onblur#shortcut", EventCallback.Factory.Create<FocusEventArgs>(this, CancelRecording));
        builder.AddElementReferenceCapture(1017, reference => recorder = reference);
        builder.OpenElement(1020, "ShortcutFieldValue");
        if (recording || value.Value is not { } gesture)
        {
            builder.OpenElement(1021, "ShortcutFieldPrompt");
            builder.AddAttribute(1022, "placeholder", !recording);
            builder.AddAttribute(1023, "live", recording ? "polite" : null);
            builder.AddContent(1024, recording ? HeldText() ?? ControlStrings.Get("PressKeys") : Placeholder ?? ControlStrings.Get("NoShortcut"));
            builder.CloseElement();
        }
        else
        {
            builder.OpenComponent<ShortcutLabel>(1030);
            builder.AddComponentParameter(1031, nameof(ShortcutLabel.Gesture), gesture);
            builder.CloseComponent();
        }
        builder.CloseElement();
        builder.CloseElement();
        if (!Clearable || value.Value is null || recording || !CanRecord) return;
        builder.OpenElement(1040, "ShortcutFieldClear");
        builder.AddAttribute(1041, "role", "button");
        builder.AddAttribute(1042, "accessible-name", ControlStrings.Get("Clear"));
        builder.AddAttribute(1043, "glyph", "minus");
        builder.AddAttribute(1044, "tabindex", -1);
        builder.AddAttribute(1045, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClearAsync));
        builder.CloseElement();
    }

    private string? HeldText()
    {
        if (held == KeyModifiers.None) return null;
        var parts = new KeyGesture("?", held).DisplayParts();
        return string.Join(OperatingSystem.IsMacOS() ? "" : "+", parts.Take(parts.Count - 1)) + (OperatingSystem.IsMacOS() ? "" : "+") + "…";
    }

    /// <summary>Starts waiting for a key, as a click or the focus do.</summary>
    public void StartRecording()
    {
        if (!CanRecord || recording) return;
        recording = true;
        held = KeyModifiers.None;
        StateHasChanged();
    }

    /// <summary>Stops waiting for a key without changing the shortcut, as Escape does.</summary>
    public void CancelRecording()
    {
        if (!recording) return;
        recording = false;
        held = KeyModifiers.None;
        StateHasChanged();
    }

    /// <summary>Records a key event as the recorder would: modifiers alone and rejected keys keep it recording.</summary>
    /// <param name="key">The key name, as in <see cref="KeyboardEventArgs.Key" />.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <returns>true if a shortcut was recorded.</returns>
    public async Task<bool> RecordAsync(string key, KeyModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!CanRecord || IsModifierKey(key)) return false;
        var normalized = key.Length == 1 ? key.ToUpperInvariant() : key == "Spacebar" ? " " : key;
        var function = normalized.Length > 1 && normalized[0] == 'F' && int.TryParse(normalized.AsSpan(1), out var number) && number is >= 1 and <= 24;
        if (RequireModifier && !function && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0) return false;
        recording = false;
        held = KeyModifiers.None;
        await SetValueAsync(new KeyGesture(normalized, modifiers));
        return true;
    }

    /// <summary>Removes the shortcut as the clear button does.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ClearAsync()
    {
        if (!CanRecord) return;
        await SetValueAsync(null);
        await recorder.FocusAsync(preventScroll: true);
        CancelRecording();
    }

    /// <summary>Moves the keyboard focus to the recorder, which starts recording.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => recorder.FocusAsync(preventScroll);

    private async Task SetValueAsync(KeyGesture? next)
    {
        var changed = value.Set(next);
        StateHasChanged();
        if (!changed) return;
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || !CanRecord) return;
        if (!recording)
        {
            if (e.Modifiers == KeyModifiers.None && e.Key is "Delete" or "Backspace" && value.Value is not null)
            {
                e.Handled = true;
                await SetValueAsync(null);
            }
            return;
        }
        if (e.Key == "Tab" && (e.Modifiers & ~KeyModifiers.Shift) == KeyModifiers.None) return;
        e.Handled = true;
        if (e.Key == "Escape" && e.Modifiers == KeyModifiers.None)
        {
            CancelRecording();
            return;
        }
        if (IsModifierKey(e.Key))
        {
            held = e.Modifiers | e.Key switch
            {
                "Shift" => KeyModifiers.Shift,
                "Control" or "Ctrl" => KeyModifiers.Control,
                "Alt" or "AltGraph" => KeyModifiers.Alt,
                "Meta" or "OS" or "Super" or "Win" => KeyModifiers.Meta,
                _ => KeyModifiers.None,
            };
            StateHasChanged();
            return;
        }
        await RecordAsync(e.Key, e.Modifiers);
    }

    private void KeyUp(KeyboardEventArgs e)
    {
        if (!recording) return;
        held = e.Modifiers & ~(e.Key switch
        {
            "Shift" => KeyModifiers.Shift,
            "Control" or "Ctrl" => KeyModifiers.Control,
            "Alt" or "AltGraph" => KeyModifiers.Alt,
            "Meta" or "OS" or "Super" or "Win" => KeyModifiers.Meta,
            _ => KeyModifiers.None,
        });
        StateHasChanged();
    }
}
