using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Shared segmented picker frame and draft-selection lifecycle. Opening snapshots the current value into a draft;
/// column interaction updates only the draft, Confirm commits once, and Cancel, Escape or outside dismissal discard it.
/// External value/configuration changes cancel an open draft. The field has one entry Tab stop and its popup uses
/// bounded nine-row selector columns with keyboard, wheel and primary-pointer drag input.
/// </summary>
/// <typeparam name="TValue">The date or time value selected by the picker.</typeparam>
public abstract class SegmentedPickerBase<TValue> : Control where TValue : struct
{
    private readonly ControllableState<TValue?> value = new(null);
    private readonly ControllableState<bool> open = new(false);
    private TValue draft;
    private object? configuration;
    private bool notifyCanceled;
    private string? activeSegment;
    private PopoverContainer? popup;
    private ElementReference? alignmentTarget;

    /// <summary>Gets or sets the committed value; null displays segment placeholders.</summary>
    [Parameter] public TValue? Value { get; set; }
    /// <summary>Gets or sets the callback invoked when confirmation changes the committed value.</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    /// <summary>Gets or sets the display culture; null uses the current culture.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }
    /// <summary>Gets or sets whether the control rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Gets or sets whether the value can be read and focused but not changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>Gets or sets whether a value is required; validation remains application-owned.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Gets or sets the application-owned invalid state.</summary>
    [Parameter] public bool Invalid { get; set; }
    /// <summary>Gets or sets whether the popup is open; supports <c>@bind-IsOpen</c>.</summary>
    [Parameter] public bool IsOpen { get; set; }
    /// <summary>Gets or sets the callback invoked when the popup opens, confirms or cancels.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    /// <summary>Gets or sets the field frame appearance.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }
    /// <summary>Gets or sets the control size.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;
    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }
    /// <summary>Gets the current committed value.</summary>
    public TValue? CurrentValue => value.Value;
    /// <summary>Gets the draft while the popup is open, or null while closed.</summary>
    public TValue? DraftValue => Opened ? draft : null;
    /// <summary>Gets whether the popup is currently open.</summary>
    public bool Opened => open.Value;
    /// <summary>Gets the draft for rendering and editing its columns.</summary>
    protected TValue Draft => draft;
    /// <summary>Gets the culture in use.</summary>
    protected CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;
    /// <summary>Gets whether the control is disabled, including through its enclosing field.</summary>
    protected bool IsInactive => Disabled || Field?.Disabled == true;
    /// <summary>Gets the localized accessible name of the picker popup.</summary>
    protected abstract string PickerLabel { get; }
    /// <summary>Gets the names of the visible segments in display order.</summary>
    protected abstract IReadOnlyList<string> SegmentNames { get; }
    /// <summary>Gets a value-equality key for configuration that invalidates an open draft.</summary>
    protected abstract object ConfigurationKey { get; }
    /// <summary>Validates the concrete picker configuration.</summary>
    protected abstract void ValidateConfiguration();
    /// <summary>Creates a valid initial draft from the committed value or a deterministic fallback.</summary>
    /// <returns>The initial draft.</returns>
    protected abstract TValue CreateInitialDraft();
    /// <summary>Tests whether a value can be committed.</summary>
    /// <param name="candidate">The value to inspect.</param>
    /// <returns>true when it is allowed.</returns>
    protected abstract bool IsAllowed(TValue candidate);
    /// <summary>Renders the entry segments.</summary>
    /// <param name="builder">The builder receiving the segment frames.</param>
    protected abstract void RenderSegments(RenderTreeBuilder builder);
    /// <summary>Renders the draft's selector columns.</summary>
    /// <param name="builder">The builder receiving the column frames.</param>
    protected abstract void RenderColumns(RenderTreeBuilder builder);

    /// <summary>Validates and synchronizes value/configuration, canceling stale draft edits.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        ValidateConfiguration();
        var changed = value.Sync(Value);
        var current = ConfigurationKey;
        var configurationChanged = configuration is not null && !Equals(configuration, current);
        configuration = current;
        var wasOpen = Opened;
        open.Sync(IsOpen);
        if (IsInactive || ReadOnly || wasOpen && (changed || configurationChanged))
        {
            notifyCanceled |= open.Value;
            open.Set(false);
        }
        else if (Opened && !wasOpen) draft = CreateInitialDraft();
        if (activeSegment is null || !SegmentNames.Contains(activeSegment)) activeSegment = SegmentNames.FirstOrDefault();
    }

    /// <summary>Reports cancellation caused by external value or configuration changes.</summary>
    /// <returns>A task completed when the open-state callback has finished.</returns>
    protected override Task OnParametersSetAsync()
    {
        if (!notifyCanceled) return Task.CompletedTask;
        notifyCanceled = false;
        return IsOpenChanged.InvokeAsync(false);
    }

    /// <summary>Adds combo-box semantics, field state and entry keyboard input.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "combobox");
        builder.AddAttribute(11, "focusable", !IsInactive);
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "readonly", ReadOnly);
        builder.AddAttribute(14, "expanded", Opened);
        builder.AddAttribute(15, "empty", CurrentValue is null);
        builder.AddAttribute(16, "required", Required || Field?.Required == true);
        builder.AddAttribute(17, "invalid", Invalid || Field?.Invalid == true || CurrentValue is { } current && !IsAllowed(current));
        builder.AddAttribute(18, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(19, "size", Vocabulary.Name(Size));
        builder.AddAttribute(20, "__internal_onkeydown#segments", EventCallback.Factory.Create<KeyboardEventArgs>(this, EntryKeyDownAsync));
        if (Id is null && Field is { } field) builder.AddAttribute(21, "id", field.ControlId);
        Field?.ApplyTo(builder, 22, LabeledBy, DescribedBy);
        builder.AddAttribute(30, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Opened ? CancelAsync() : OpenAsync()));
    }

    /// <summary>Renders entry segments and the draft popup with Confirm and Cancel actions.</summary>
    /// <param name="builder">The builder receiving the content.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "SegmentedPickerSegments");
        RenderSegments(builder);
        builder.CloseElement();
        builder.OpenComponent<PopoverContainer>(1100);
        builder.AddComponentParameter(1101, nameof(PopoverContainer.As), ElementName + "Popup");
        builder.AddComponentParameter(1102, nameof(PopoverContainer.IsOpen), Opened);
        builder.AddComponentParameter(1103, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, next => next ? Task.CompletedTask : CancelAsync()));
        builder.AddComponentParameter(1104, nameof(PopoverContainer.Anchor), Element);
        builder.AddComponentParameter(1105, nameof(PopoverContainer.ReturnFocus), Element);
        builder.AddComponentParameter(1106, nameof(PopoverContainer.InitialFocus), (Func<ElementReference?>)InitialFocus);
        builder.AddComponentParameter(1107, nameof(PopoverContainer.Role), "dialog");
        builder.AddComponentParameter(1108, nameof(Control.AccessibleName), PickerLabel);
        builder.AddComponentParameter(1109, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["__internal_onkeydown#segmented"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, PopupKeyDownAsync),
        });
        builder.AddComponentParameter(1110, nameof(PopoverContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "SegmentedPickerColumns");
            RenderColumns(content);
            content.CloseElement();
            content.OpenElement(100, "SegmentedPickerActions");
            ActionButton(content, 110, true);
            ActionButton(content, 120, false);
            content.CloseElement();
        }));
        builder.AddComponentParameter(1111, nameof(PopoverContainer.AlignmentTarget), alignmentTarget);
        builder.AddComponentReferenceCapture(1112, component => popup = (PopoverContainer)component);
        builder.CloseComponent();
    }

    /// <summary>Tracks the active selected row so the popup can align it with the entry after its retained parts are available.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!Opened || InitialFocus() is not { } selected || selected.Element == alignmentTarget?.Element) return;
        alignmentTarget = selected;
        StateHasChanged();
    }

    private void ActionButton(RenderTreeBuilder builder, int sequence, bool confirm)
    {
        builder.OpenElement(sequence, confirm ? "SegmentedPickerConfirm" : "SegmentedPickerCancel");
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "focusable", true);
        builder.AddAttribute(sequence + 3, "accessible-name", ControlStrings.Get(confirm ? "Ok" : "Cancel"));
        builder.AddAttribute(sequence + 5, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => confirm ? ConfirmAsync() : CancelAsync()));
        builder.OpenElement(sequence + 6, "SegmentedPickerActionIcon");
        builder.AddAttribute(sequence + 7, "semantic-hidden", true);
        if (confirm) builder.AddAttribute(sequence + 8, "glyph", "check");
        builder.CloseElement();
        builder.CloseElement();
    }

    /// <summary>Renders a clickable entry segment; only the root participates in normal Tab traversal.</summary>
    /// <param name="builder">The builder receiving the segment.</param>
    /// <param name="sequence">The starting frame sequence.</param>
    /// <param name="name">The segment's stable name.</param>
    /// <param name="label">Its localized name/placeholder.</param>
    /// <param name="text">The committed display text, or null for the placeholder.</param>
    protected void Segment(RenderTreeBuilder builder, int sequence, string name, string label, string? text)
    {
        builder.OpenElement(sequence, "SegmentedPickerSegment");
        builder.SetKey(name);
        builder.AddAttribute(sequence + 1, "segment", name);
        builder.AddAttribute(sequence + 2, "current", name == activeSegment);
        builder.AddAttribute(sequence + 3, "role", "button");
        builder.AddAttribute(sequence + 4, "focusable", !IsInactive);
        builder.AddAttribute(sequence + 5, "tabindex", -1);
        builder.AddAttribute(sequence + 6, "accessible-name", label + (text is null ? "" : ": " + text));
        builder.AddAttribute(sequence + 7, "placeholder", text is null);
        builder.AddAttribute(sequence + 8, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => OpenSegmentAsync(name)));
        builder.AddContent(sequence + 9, text ?? label);
        builder.CloseElement();
    }

    /// <summary>Renders one bounded selector column bound to the draft.</summary>
    /// <param name="builder">The builder receiving the column.</param>
    /// <param name="sequence">The starting frame sequence.</param>
    /// <param name="name">The segment name.</param>
    /// <param name="label">The localized column name.</param>
    /// <param name="selected">The draft's selected integer.</param>
    /// <param name="values">The ascending available values.</param>
    /// <param name="format">The value display formatter.</param>
    /// <param name="apply">Converts a chosen value into the next draft.</param>
    protected void Column(RenderTreeBuilder builder, int sequence, string name, string label, int selected, IReadOnlyList<int> values, Func<int, string> format, Func<int, TValue> apply)
    {
        builder.OpenComponent<SegmentedPickerColumn>(sequence);
        builder.SetKey(name);
        builder.AddComponentParameter(sequence + 1, nameof(SegmentedPickerColumn.Name), name);
        builder.AddComponentParameter(sequence + 2, nameof(Control.AccessibleName), label);
        builder.AddComponentParameter(sequence + 3, nameof(SegmentedPickerColumn.Values), values);
        builder.AddComponentParameter(sequence + 4, nameof(SegmentedPickerColumn.Value), selected);
        builder.AddComponentParameter(sequence + 5, nameof(SegmentedPickerColumn.Format), format);
        builder.AddComponentParameter(sequence + 6, nameof(SegmentedPickerColumn.ValueChanged), EventCallback.Factory.Create<int>(this, value => SetDraft(apply(value))));
        builder.AddComponentParameter(sequence + 7, nameof(SegmentedPickerColumn.OnFocused), EventCallback.Factory.Create<string>(this, name => activeSegment = name));
        builder.CloseComponent();
    }

    /// <summary>Changes only the open draft when it is allowed.</summary>
    /// <param name="next">The next draft value.</param>
    protected void SetDraft(TValue next)
    {
        if (!Opened || IsInactive || ReadOnly || !IsAllowed(next) || EqualityComparer<TValue>.Default.Equals(draft, next)) return;
        draft = next;
        StateHasChanged();
    }

    /// <summary>Opens a draft popup unless disabled or read-only.</summary>
    /// <returns>A task completed after the open-state callback.</returns>
    public Task OpenAsync() => IsInactive || ReadOnly ? Task.CompletedTask : SetOpenAsync(true);
    /// <summary>Discards the draft and closes the popup.</summary>
    /// <returns>A task completed after the open-state callback.</returns>
    public Task CancelAsync() => SetOpenAsync(false);
    /// <summary>Commits an allowed draft once, closes the popup and reports the value change.</summary>
    /// <returns>A task completed after open/value callbacks and input/change events.</returns>
    public async Task ConfirmAsync()
    {
        if (!Opened || IsInactive || ReadOnly || !IsAllowed(draft)) return;
        var changed = value.Set(draft);
        await SetOpenAsync(false);
        StateHasChanged();
        if (!changed) return;
        await ValueChanged.InvokeAsync(value.Value);
        await Events.RaiseChangeAsync(Element, value.Value);
    }

    private Task OpenSegmentAsync(string name) { activeSegment = name; return OpenAsync(); }
    private async Task SetOpenAsync(bool next)
    {
        if (next == Opened) return;
        if (next) { draft = CreateInitialDraft(); if (!IsAllowed(draft)) return; }
        open.Set(next);
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(next);
    }

    private ElementReference? InitialFocus()
    {
        if (popup?.Element.Element is not { } surface) return null;
        var columns = ControlTree.Descendants(surface).Where(e => e.Name == "SegmentedPickerColumn").ToArray();
        var column = columns.FirstOrDefault(e => Equals(e.Attributes.GetValueOrDefault("column"), activeSegment)) ?? columns.FirstOrDefault();
        var selected = column is null ? null : ControlTree.Descendants(column).FirstOrDefault(e => e.Attributes.GetValueOrDefault("current") is true);
        return selected is null ? null : new ElementReference(selected);
    }

    private Task EntryKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || ReadOnly) return Task.CompletedTask;
        if (e.Key == "F4" && e.Modifiers == KeyModifiers.None || e.Key == "ArrowDown" && e.Modifiers == KeyModifiers.Alt ||
            (e.Key is "Enter" or " ") && e.Modifiers == KeyModifiers.None)
        {
            e.Handled = true;
            return OpenAsync();
        }
        if (e.Modifiers == KeyModifiers.None && (e.Key is "ArrowLeft" or "ArrowRight"))
        {
            var position = Array.IndexOf(SegmentNames.ToArray(), activeSegment);
            activeSegment = SegmentNames[Math.Clamp(position + (e.Key == "ArrowLeft" ? -1 : 1), 0, SegmentNames.Count - 1)];
            e.Handled = true;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    private Task PopupKeyDownAsync(KeyboardEventArgs e)
    {
        if (!e.Handled && (e.Key == "F4" && e.Modifiers == KeyModifiers.None || e.Key == "ArrowUp" && e.Modifiers == KeyModifiers.Alt))
        {
            e.Handled = true;
            return CancelAsync();
        }
        if (e.Handled || e.Modifiers != KeyModifiers.None) return Task.CompletedTask;
        if (e.Key == "Enter")
        {
            if (popup?.Element.Element is { } content && ControlTree.Descendants(content).Any(n =>
                n.States.HasFlag(ElementStates.Focus) && n.Name is "SegmentedPickerConfirm" or "SegmentedPickerCancel")) return Task.CompletedTask;
            e.Handled = true;
            return ConfirmAsync();
        }
        if (e.Key is not ("ArrowLeft" or "ArrowRight") || popup?.Element.Element is not { } surface) return Task.CompletedTask;
        var columns = ControlTree.Descendants(surface).Where(n => n.Name == "SegmentedPickerColumn").ToArray();
        var current = Array.FindIndex(columns, n => n.States.HasFlag(ElementStates.FocusWithin));
        if (current < 0) return Task.CompletedTask;
        e.Handled = true;
        var column = columns[Math.Clamp(current + (e.Key == "ArrowLeft" ? -1 : 1), 0, columns.Length - 1)];
        var option = ControlTree.Descendants(column).FirstOrDefault(n => n.Attributes.GetValueOrDefault("current") is true);
        return option is null ? Task.CompletedTask : new ElementReference(option).FocusAsync(preventScroll: true);
    }
}
