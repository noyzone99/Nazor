using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Where a <see cref="NumberField" /> places its increment and decrement buttons, written as the <c>spin-buttons</c>
/// attribute so that a theme can follow its platform.
/// </summary>
public enum SpinButtonPlacement
{
    /// <summary>
    /// A compact step affordance: an in-field stacked pair in Basic and NazorLeaf, or a focus-triggered flyout in Fluent.
    /// </summary>
    Compact,
    /// <summary>Inline buttons: on opposite sides in Basic and NazorLeaf, or adjacent at the trailing edge in Fluent.</summary>
    Inline,
    /// <summary>No buttons; the keyboard and the wheel still step.</summary>
    Hidden,
}

/// <summary>
/// A number typed as text and stepped with buttons, keys and optionally the wheel. <see cref="Value" />
/// (<c>@bind-Value</c>; null when empty) changes when the text is committed with Enter or by leaving the field: the
/// text is read in <see cref="Culture" /> (group separators and a trailing <see cref="Unit" /> are accepted), optionally
/// evaluated as arithmetic with <see cref="AcceptsExpression" />, kept between
/// <see cref="Minimum" /> and <see cref="Maximum" /> and written again with <see cref="Format" />; text that is not a
/// number goes back to the value, and incomplete text stays untouched while typing. Up and Down add or remove
/// <see cref="Step" />, Page Up and Page Down <see cref="LargeStep" /> (the engine's spin behavior), and the buttons repeat
/// while held. The editor has the <c>spinbutton</c> role with the range for assistive technologies. Parts:
/// <c>NumberFieldLeading</c>, <c>NumberFieldInput</c>, <c>NumberFieldUnit</c>, <c>NumberFieldSpinButtons</c>,
/// <c>NumberFieldIncrement</c> and <c>NumberFieldDecrement</c> (disabled at the bounds). Compact mode also renders
/// <c>NumberFieldSpinToggle</c> with decorative <c>NumberFieldSpinToggleUp</c> and <c>NumberFieldSpinToggleDown</c>
/// glyphs, and a <c>NumberFieldSpinPopup</c>; the theme selects the visible presentation. Its popup
/// preserves editor focus, closes with Escape or an outside press, and reopens on focus entry or its toggle action.
/// </summary>
public sealed class NumberField : Control
{
    /// <inheritdoc />
    protected override int? ElementTabIndex => null;

    private readonly ControllableState<double?> value = new(null);
    private EditableText? editor;
    private string text = "";
    private (string? Format, CultureInfo? Culture) shownWith;
    private bool focused;
    private bool spinOpen;
    private bool spinDismissed;

    /// <summary>Gets or sets the value; null when the field is empty.</summary>
    [Parameter] public double? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user commits or steps a new value; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<double?> ValueChanged { get; set; }

    /// <summary>Gets or sets the lowest value.</summary>
    [Parameter] public double Minimum { get; set; } = double.MinValue;

    /// <summary>Gets or sets the highest value.</summary>
    [Parameter] public double Maximum { get; set; } = double.MaxValue;

    /// <summary>Gets or sets the change made by the buttons, Up, Down and the wheel; 1 by default.</summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>Gets or sets the change made by Page Up and Page Down; 10 by default.</summary>
    [Parameter] public double LargeStep { get; set; } = 10;

    /// <summary>Gets or sets the .NET numeric format of the text, such as <c>N2</c> or <c>0.##</c>; the general format by default.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Gets or sets the culture the number is written and read in; the current culture by default.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets whether committed text accepts arithmetic expressions; false by default. Operators are
    /// <c>^</c>, then <c>* /</c>, then <c>+ -</c>, evaluated left to right at equal precedence. Parentheses override
    /// precedence and unary signs precede powers. Operands use <see cref="Culture" />; only finite results are
    /// accepted, and division by zero is invalid. Expressions are limited to 4096 characters and 64 nested parentheses.
    /// Enter, leaving the editor or stepping evaluates the expression, clamps the result and applies <see cref="Format" />
    /// to its displayed text without rounding the stored value. Invalid text restores the previous value on commit.
    /// </summary>
    [Parameter] public bool AcceptsExpression { get; set; }

    /// <summary>Gets or sets a unit shown after the number, such as "kg"; it also describes the field to assistive technologies.</summary>
    [Parameter] public string? Unit { get; set; }

    /// <summary>Gets or sets where the increment and decrement buttons are placed.</summary>
    [Parameter] public SpinButtonPlacement SpinButtons { get; set; }

    /// <summary>Gets or sets whether the mouse wheel steps the value while the field has the focus.</summary>
    [Parameter] public bool WheelChanges { get; set; }

    /// <summary>Gets or sets the hint shown while the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the value cannot be changed while it can still be selected and copied.</summary>
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

    /// <summary>Gets or sets how long a button must be held before it starts repeating.</summary>
    [Parameter] public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Gets or sets the interval between repetitions while a button is held.</summary>
    [Parameter] public TimeSpan RepeatInterval { get; set; } = TimeSpan.FromMilliseconds(60);

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current value.</summary>
    public double? CurrentValue => value.Value;

    /// <summary>Gets the current text, which may be uncommitted.</summary>
    public string Text => text;

    /// <summary>Gets the editor element, which receives the focus.</summary>
    public ElementReference InputElement => editor?.Element ?? default;

    /// <summary>Gets the NSS element name emitted by NumberField.</summary>
    protected override string ElementName => "NumberField";

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private double Top => Math.Max(Minimum, Maximum);

    /// <summary>Validates the range and synchronizes the value and the text.</summary>
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Minimum)) throw new ArgumentOutOfRangeException(nameof(Minimum));
        if (!double.IsFinite(Maximum) || Maximum < Minimum) throw new ArgumentOutOfRangeException(nameof(Maximum));
        if (!double.IsFinite(Step) || Step <= 0) throw new ArgumentOutOfRangeException(nameof(Step));
        if (!double.IsFinite(LargeStep) || LargeStep <= 0) throw new ArgumentOutOfRangeException(nameof(LargeStep));
        if (Value is { } given && !double.IsFinite(given)) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!Enum.IsDefined(SpinButtons)) throw new ArgumentOutOfRangeException(nameof(SpinButtons));
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (RepeatInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(RepeatInterval));
        if (RepeatDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(RepeatDelay));
        _ = Format is null ? "" : 0.0.ToString(Format, EffectiveCulture);
        if (value.Sync(Value) || shownWith != (Format, Culture) || editor is null) text = Write(value.Value);
        shownWith = (Format, Culture);
        if (SpinButtons != SpinButtonPlacement.Compact || IsInactive || ReadOnly) spinOpen = false;
    }

    /// <summary>Writes a value in the field's format and culture.</summary>
    /// <param name="number">The value, or null for an empty text.</param>
    /// <returns>The text.</returns>
    public string Write(double? number) => number is { } n ? n.ToString(Format ?? "G", EffectiveCulture) : "";

    /// <summary>
    /// Reads text in the field's culture, accepting group separators and a trailing <see cref="Unit" />.
    /// Evaluates arithmetic when <see cref="AcceptsExpression" /> is true. This method does not clamp or change the value.
    /// </summary>
    /// <param name="input">The text.</param>
    /// <param name="number">The number read, or null for an empty text.</param>
    /// <returns>true if the text is empty or a finite number.</returns>
    public bool TryRead(string input, out double? number)
    {
        ArgumentNullException.ThrowIfNull(input);
        var trimmed = input.Trim();
        if (Unit is { Length: > 0 } unit && trimmed.EndsWith(unit, StringComparison.CurrentCultureIgnoreCase)) trimmed = trimmed[..^unit.Length].TrimEnd();
        number = null;
        if (trimmed.Length == 0) return true;
        if ((!double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, EffectiveCulture, out var parsed) || !double.IsFinite(parsed))
            && (!AcceptsExpression || !NumberExpression.TryEvaluate(trimmed, EffectiveCulture, out parsed))) return false;
        number = parsed;
        return true;
    }

    /// <summary>Adds the frame states and the pointer and wheel handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "size", Vocabulary.Name(Size));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "readonly", ReadOnly);
        builder.AddAttribute(14, "invalid", Invalid || Field?.Invalid == true);
        builder.AddAttribute(15, "empty", text.Length == 0);
        builder.AddAttribute(16, "spin-buttons", SpinButtons.ToString().ToLowerInvariant());
        builder.AddAttribute(17, "__internal_onpointerdown#field", EventCallback.Factory.Create<PointerEventArgs>(this, FocusFromFrame));
        if (!WheelChanges) return;
        builder.AddAttribute(18, "__internal_onwheel#number", EventCallback.Factory.Create<WheelEventArgs>(this, WheelAsync));
        builder.AddEventPreventDefaultAttribute(19, "onwheel", focused && !IsInactive && !ReadOnly);
    }

    /// <summary>Adds the side content, the buttons, the editor and the unit.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "NumberFieldLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        if (SpinButtons == SpinButtonPlacement.Inline) AddButton(builder, 1010, false);
        builder.OpenComponent<EditableText>(1020);
        builder.AddComponentParameter(1021, nameof(EditableText.As), "NumberFieldInput");
        builder.AddComponentParameter(1022, nameof(EditableText.Value), text);
        builder.AddComponentParameter(1023, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, Typed));
        builder.AddComponentParameter(1024, nameof(EditableText.Placeholder), Placeholder);
        builder.AddComponentParameter(1025, nameof(EditableText.ReadOnly), ReadOnly);
        builder.AddComponentParameter(1026, nameof(EditableText.Disabled), Disabled);
        builder.AddComponentParameter(1027, nameof(EditableText.Required), Required);
        builder.AddComponentParameter(1028, nameof(EditableText.Invalid), Invalid);
        builder.AddComponentParameter(1029, nameof(EditableText.Role), "spinbutton");
        builder.AddComponentParameter(1030, nameof(EditableText.InputMode), "number");
        builder.AddComponentParameter(1031, nameof(EditableText.AutoFocus), AutoFocus);
        builder.AddComponentParameter(1032, nameof(EditableText.Spin), new SpinRange(value.Value ?? Math.Clamp(0, Minimum, Top), Minimum, Top, Step, LargeStep));
        builder.AddComponentParameter(1033, nameof(EditableText.OnInput), EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        builder.AddComponentParameter(1034, nameof(EditableText.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, ChangeAsync));
        builder.AddComponentParameter(1035, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(1036, nameof(Control.AccessibleDescription), AccessibleDescription ?? Unit);
        builder.AddComponentParameter(1037, nameof(Control.LabeledBy), LabeledBy);
        builder.AddComponentParameter(1038, nameof(Control.DescribedBy), DescribedBy);
        builder.AddComponentParameter(1039, nameof(Control.TabIndex), TabIndex);
        builder.AddComponentParameter(1040, nameof(Control.Id), Id is null ? null : Id + "-input");
        builder.AddComponentParameter(1041, "__internal_onkeydown#number", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddComponentParameter(1042, "__internal_onfocus#number", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(true)));
        builder.AddComponentParameter(1043, "__internal_onblur#number", EventCallback.Factory.Create<FocusEventArgs>(this, () => SetFocused(false)));
        builder.AddComponentReferenceCapture(1044, component => editor = (EditableText)component);
        builder.CloseComponent();
        if (Unit is not null)
        {
            builder.OpenElement(1050, "NumberFieldUnit");
            builder.AddAttribute(1051, "semantic-hidden", true);
            builder.AddContent(1052, Unit);
            builder.CloseElement();
        }
        if (SpinButtons == SpinButtonPlacement.Inline) AddButton(builder, 1060, true);
        if (SpinButtons != SpinButtonPlacement.Compact) return;
        builder.OpenElement(1070, "NumberFieldSpinButtons");
        AddButton(builder, 1080, true);
        AddButton(builder, 1090, false);
        builder.CloseElement();
        builder.OpenElement(1100, "NumberFieldSpinToggle");
        builder.AddAttribute(1101, "role", "button");
        builder.AddAttribute(1102, "tabindex", -1);
        builder.AddAttribute(1103, "disabled", IsInactive || ReadOnly);
        builder.AddAttribute(1104, "accessible-name", ControlStrings.Get("ShowSpinButtons"));
        builder.AddAttribute(1105, "expanded", spinOpen);
        builder.AddAttribute(1106, "__internal_onpointerdown#spin", EventCallback.Factory.Create<PointerEventArgs>(this,
            e => e.Button == PointerButton.Primary ? ToggleSpinAsync() : Task.CompletedTask));
        builder.AddAttribute(1107, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleSpinAsync));
        builder.AddEventPreventDefaultAttribute(1108, "onpointerdown", true);
        builder.OpenElement(1110, "NumberFieldSpinToggleUp");
        builder.AddAttribute(1111, "glyph", "chevron-up");
        builder.AddAttribute(1112, "semantic-hidden", true);
        builder.CloseElement();
        builder.OpenElement(1120, "NumberFieldSpinToggleDown");
        builder.AddAttribute(1121, "glyph", "chevron-down");
        builder.AddAttribute(1122, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenComponent<PopoverContainer>(1130);
        builder.AddComponentParameter(1131, nameof(PopoverContainer.As), "NumberFieldSpinPopup");
        builder.AddComponentParameter(1132, nameof(PopoverContainer.IsOpen), spinOpen);
        builder.AddComponentParameter(1133, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, SpinOpenChanged));
        builder.AddComponentParameter(1134, nameof(PopoverContainer.Anchor), Element);
        builder.AddComponentParameter(1135, nameof(PopoverContainer.Placement), OverlayPlacement.Right);
        builder.AddComponentParameter(1136, nameof(PopoverContainer.Alignment), OverlayAlignment.Center);
        builder.AddComponentParameter(1137, nameof(PopoverContainer.FocusOnOpen), false);
        builder.AddComponentParameter(1138, nameof(PopoverContainer.ReturnFocus), editor?.Element);
        builder.AddComponentParameter(1139, nameof(PopoverContainer.AccessibleName), ControlStrings.Get("ShowSpinButtons"));
        builder.AddComponentParameter(1140, nameof(PopoverContainer.ChildContent), (RenderFragment)(popup =>
        {
            AddButton(popup, 0, true);
            AddButton(popup, 20, false);
        }));
        builder.CloseComponent();
    }

    private void AddButton(RenderTreeBuilder builder, int sequence, bool up)
    {
        var atBound = value.Value is { } current && (up ? current >= Top : current <= Minimum);
        builder.OpenComponent<Pressable>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(Pressable.As), up ? "NumberFieldIncrement" : "NumberFieldDecrement");
        builder.AddComponentParameter(sequence + 2, nameof(Pressable.OnPress), EventCallback.Factory.Create<PointerEventArgs>(this, e => PressAsync(e, up)));
        builder.AddComponentParameter(sequence + 3, nameof(Pressable.RepeatInterval), (TimeSpan?)RepeatInterval);
        builder.AddComponentParameter(sequence + 4, nameof(Pressable.RepeatDelay), RepeatDelay);
        builder.AddComponentParameter(sequence + 5, nameof(Pressable.Disabled), IsInactive || ReadOnly || atBound);
        builder.AddComponentParameter(sequence + 6, nameof(Pressable.Focusable), false);
        builder.AddComponentParameter(sequence + 7, nameof(Control.AccessibleName), ControlStrings.Get(up ? "Increase" : "Decrease"));
        builder.AddComponentParameter(sequence + 8, "glyph", up ? "chevron-up" : "chevron-down");
        builder.AddComponentParameter(sequence + 9, "preserve-focus-on-press", true);
        builder.CloseComponent();
    }

    /// <summary>Adds <see cref="Step" /> (or <see cref="LargeStep" />) to the value, as the increment button does.</summary>
    /// <param name="large">Whether to use the large step.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task IncrementAsync(bool large = false) => StepAsync(large ? LargeStep : Step);

    /// <summary>Subtracts <see cref="Step" /> (or <see cref="LargeStep" />) from the value, as the decrement button does.</summary>
    /// <param name="large">Whether to use the large step.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task DecrementAsync(bool large = false) => StepAsync(-(large ? LargeStep : Step));

    /// <summary>Commits the current text as Enter would.</summary>
    /// <returns>A task that completes when the value has been reported.</returns>
    public Task CommitAsync() => CommitTextAsync(text);

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) => InputElement.FocusAsync(preventScroll);

    private async Task StepAsync(double delta)
    {
        if (IsInactive || ReadOnly) return;
        if (text != Write(value.Value) && TryRead(text, out var typed)) await SetValueAsync(typed is { } t ? Math.Clamp(t, Minimum, Top) : null);
        var start = value.Value ?? Math.Clamp(0, Minimum, Top);
        await SetValueAsync(Math.Clamp(Round(start + delta, Math.Max(Decimals(delta), Decimals(start))), Minimum, Top));
    }

    private static int Decimals(double number)
    {
        var written = Math.Abs(number).ToString("R", CultureInfo.InvariantCulture);
        if (written.Contains('E')) return 10;
        var point = written.IndexOf('.');
        return point < 0 ? 0 : Math.Min(10, written.Length - point - 1);
    }

    private static double Round(double number, int decimals) => Math.Round(number, decimals, MidpointRounding.AwayFromZero);

    private async Task SetValueAsync(double? number)
    {
        text = Write(number);
        var changed = value.Set(number);
        StateHasChanged();
        if (!changed) return;
        await ValueChanged.InvokeAsync(number);
        await Events.RaiseChangeAsync(Element, number);
    }

    private void Typed(string typed)
    {
        if (text == typed) return;
        text = typed;
        StateHasChanged();
    }

    private Task InputAsync(ChangeEventArgs e)
    {
        if (e.Value is not double stepped || IsInactive || ReadOnly) return Task.CompletedTask;
        return SetValueAsync(Math.Clamp(stepped, Minimum, Top));
    }

    private Task ChangeAsync(ChangeEventArgs e) => e.Value is string committed ? CommitTextAsync(committed) : Task.CompletedTask;

    private Task CommitTextAsync(string committed)
    {
        if (IsInactive || ReadOnly) return Task.CompletedTask;
        if (TryRead(committed, out var number)) return SetValueAsync(number is { } n ? Math.Clamp(n, Minimum, Top) : null);
        text = Write(value.Value);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || ReadOnly || e.Modifiers != KeyModifiers.None) return Task.CompletedTask;
        double? delta = e.Key switch
        {
            "ArrowUp" => Step,
            "ArrowDown" => -Step,
            "PageUp" => LargeStep,
            "PageDown" => -LargeStep,
            _ => null,
        };
        if (delta is not { } d) return Task.CompletedTask;
        e.Handled = true;
        return StepAsync(d);
    }

    private Task WheelAsync(WheelEventArgs e)
    {
        if (!focused || IsInactive || ReadOnly || e.DeltaY == 0) return Task.CompletedTask;
        return StepAsync(e.DeltaY > 0 ? Step : -Step);
    }

    private async Task PressAsync(PointerEventArgs e, bool up)
    {
        await StepAsync(up ? Step : -Step);
        if (!e.IsSynthetic && !focused) await FocusAsync(preventScroll: true);
    }

    private void SetFocused(bool next)
    {
        if (focused == next) return;
        focused = next;
        if (next) spinOpen = SpinButtons == SpinButtonPlacement.Compact && !IsInactive && !ReadOnly && !spinDismissed;
        else
        {
            spinOpen = false;
            spinDismissed = false;
        }
        StateHasChanged();
    }

    private async Task ToggleSpinAsync()
    {
        if (IsInactive || ReadOnly || SpinButtons != SpinButtonPlacement.Compact) return;
        spinOpen = !spinOpen;
        spinDismissed = !spinOpen;
        await FocusAsync(preventScroll: true);
        StateHasChanged();
    }

    private void SpinOpenChanged(bool next)
    {
        if (next) return;
        spinOpen = false;
        spinDismissed = true;
        StateHasChanged();
    }

    private Task FocusFromFrame(PointerEventArgs e) =>
        IsInactive || !ControlTree.IsFieldFramePress(e, Element) ? Task.CompletedTask : FocusAsync(preventScroll: true);
}
