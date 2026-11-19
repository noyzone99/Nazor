using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>What a <see cref="Chip" /> does, written as the <c>kind</c> attribute.</summary>
public enum ChipKind
{
    /// <summary>Runs an action, as a compact button (<c>button</c> role).</summary>
    Action,
    /// <summary>Turns a filter on or off (<c>togglebutton</c> role), alone or in a multiple-selection
    /// <see cref="ChipGroup{TValue}" />; a selected filter shows a <c>ChipCheck</c>.</summary>
    Filter,
    /// <summary>One choice of a single-selection <see cref="ChipGroup{TValue}" /> (<c>radio</c> role).</summary>
    Choice,
    /// <summary>An entry the user added, such as a recipient, removed with its <c>ChipRemove</c> button or with
    /// Backspace or Delete.</summary>
    Input,
}

internal interface IChipOwner
{
    string GroupId { get; }
    SelectionMode Mode { get; }
    ControlSize Size { get; }
    bool IsInactive { get; }
    bool IsSelected(Chip chip);
    bool IsHidden(Chip chip);
    bool IsTabStop(Chip chip);
    (int Position, int Count) PositionOf(Chip chip);
    void Register(Chip chip);
    void Unregister(Chip chip);
    Task ActivateAsync(Chip chip);
    ElementNode? Neighbor(Chip chip);
    void Focused(Chip chip);
}

/// <summary>
/// A compact element that represents a filter, a choice, an entry or an action (<see cref="Kind" />). A filter chip
/// keeps <see cref="IsSelected" /> (<c>@bind-IsSelected</c>) when it stands alone; inside a
/// <see cref="ChipGroup{TValue}" /> with a selection mode the group owns the selection through <see cref="Value" />. An
/// input chip calls <see cref="OnRemove" /> from its remove button or with Backspace or Delete. The element is
/// <c>Chip</c> with the <c>kind</c>, <c>selected</c>, <c>intent</c>, <c>size</c> and <c>removable</c> attributes; the
/// parts are <c>ChipDot</c> (<see cref="ShowDot" />) or <c>ChipLeading</c>, <c>ChipCheck</c>, <c>ChipLabel</c> and
/// <c>ChipRemove</c>.
/// </summary>
public sealed class Chip : ContentControl, IDisposable
{
    private readonly ControllableState<bool> selected = new(false);
    private readonly string chipId = UniqueId.Create("chip");
    private IChipOwner? registered;

    /// <summary>Gets or sets what the chip does; null takes it from the enclosing group: <see cref="ChipKind.Choice" />
    /// in single selection, <see cref="ChipKind.Filter" /> in multiple selection, <see cref="ChipKind.Action" />
    /// otherwise.</summary>
    [Parameter] public ChipKind? Kind { get; set; }

    /// <summary>Gets or sets the value the chip stands for in a <see cref="ChipGroup{TValue}" />; it must be of the
    /// group's value type.</summary>
    [Parameter] public object? Value { get; set; }

    /// <summary>Gets or sets whether a filter chip outside a selecting group is selected.</summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects or clears a filter chip outside a selecting
    /// group.</summary>
    [Parameter] public EventCallback<bool> IsSelectedChanged { get; set; }

    /// <summary>Gets or sets content shown before the label, such as an avatar or an icon.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets whether a colored dot is shown before the label, in the color of <see cref="Intent" />.</summary>
    [Parameter] public bool ShowDot { get; set; }

    /// <summary>Gets or sets the meaning of the chip's color.</summary>
    [Parameter] public Intent Intent { get; set; }

    /// <summary>Gets or sets the size step; null uses the group's.</summary>
    [Parameter] public ControlSize? Size { get; set; }

    /// <summary>Gets or sets whether the chip rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback invoked when the chip is activated, after any change of selection.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets or sets the callback invoked when the user removes an input chip; the application removes it.</summary>
    [Parameter] public EventCallback OnRemove { get; set; }

    [CascadingParameter] internal IChipOwner? Owner { get; set; }

    /// <summary>Gets the kind in effect, explicit or taken from the group.</summary>
    public ChipKind EffectiveKind => Kind ?? Owner?.Mode switch
    {
        SelectionMode.Single => ChipKind.Choice,
        SelectionMode.Multiple => ChipKind.Filter,
        _ => ChipKind.Action,
    };

    /// <summary>Gets whether the chip is currently selected.</summary>
    public bool Selected => GroupSelects ? Owner!.IsSelected(this) : EffectiveKind is ChipKind.Filter or ChipKind.Choice && selected.Value;

    /// <summary>Gets the NSS element name emitted by Chip.</summary>
    protected override string ElementName => "Chip";

    internal string ItemId => Id ?? chipId;

    internal bool IsInactive => Disabled || Owner?.IsInactive == true;

    private bool GroupSelects => Owner is { Mode: not SelectionMode.None } && EffectiveKind is ChipKind.Filter or ChipKind.Choice;

    /// <summary>Validates the parameters, synchronizes the state and joins the enclosing group.</summary>
    protected override void OnParametersSet()
    {
        if (Kind is { } kind && !Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(Kind));
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (Size is { } size && !Enum.IsDefined(size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (EffectiveKind == ChipKind.Choice && Owner is not { Mode: SelectionMode.Single })
            throw new InvalidOperationException("A choice chip must be inside a ChipGroup with single selection.");
        selected.Sync(IsSelected);
        if (registered == Owner) return;
        registered?.Unregister(this);
        (registered = Owner)?.Register(this);
    }

    /// <summary>Adds the role, the states and the keyboard and pointer handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var kind = EffectiveKind;
        var isSelected = Selected;
        var hidden = Owner?.IsHidden(this) == true;
        builder.AddAttribute(10, "role", kind switch { ChipKind.Filter => "togglebutton", ChipKind.Choice => "radio", _ => "button" });
        builder.AddAttribute(11, "kind", kind.ToString().ToLowerInvariant());
        builder.AddAttribute(12, "selected", isSelected);
        if (kind == ChipKind.Filter) builder.AddAttribute(13, "pressed", isSelected);
        if (kind == ChipKind.Choice) builder.AddAttribute(14, "checked", isSelected);
        builder.AddAttribute(15, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(16, "size", Vocabulary.Name(Size ?? Owner?.Size ?? ControlSize.Medium));
        builder.AddAttribute(17, "removable", kind == ChipKind.Input);
        builder.AddAttribute(18, "disabled", IsInactive);
        builder.AddAttribute(19, "focusable", !IsInactive && !hidden);
        builder.AddAttribute(20, "hidden", hidden);
        builder.AddAttribute(21, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        if (kind == ChipKind.Input) builder.AddAttribute(22, "__internal_onkeydown#chip", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        if (Owner is { } owner)
        {
            var (position, count) = owner.PositionOf(this);
            builder.AddAttribute(23, "id", ItemId);
            builder.AddAttribute(24, "chip-group", owner.GroupId);
            if (TabIndex is null) builder.AddAttribute(25, "tabindex", owner.IsTabStop(this) ? 0 : -1);
            builder.AddAttribute(26, "position-in-set", position);
            builder.AddAttribute(27, "size-of-set", count);
            builder.AddAttribute(28, "__internal_onfocus#chips", EventCallback.Factory.Create<FocusEventArgs>(this, () => owner.Focused(this)));
        }
    }

    /// <summary>Adds the dot or the leading content, the check, the label and the remove button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ShowDot)
        {
            builder.OpenElement(1000, "ChipDot");
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.CloseElement();
        }
        else if (Leading is not null)
        {
            builder.OpenElement(1002, "ChipLeading");
            builder.AddContent(1003, Leading);
            builder.CloseElement();
        }
        if (EffectiveKind == ChipKind.Filter && Selected)
        {
            builder.OpenElement(1010, "ChipCheck");
            builder.AddAttribute(1011, "glyph", "check");
            builder.AddAttribute(1012, "semantic-hidden", true);
            builder.CloseElement();
        }
        if (ChildContent is not null)
        {
            builder.OpenElement(1020, "ChipLabel");
            builder.AddContent(1021, ChildContent);
            builder.CloseElement();
        }
        if (EffectiveKind == ChipKind.Input)
        {
            builder.OpenElement(1030, "ChipRemove");
            builder.AddAttribute(1031, "role", "button");
            builder.AddAttribute(1032, "accessible-name", RemoveName());
            builder.AddAttribute(1033, "onclick", EventCallback.Factory.Create<PointerEventArgs>(this, RemoveAsync));
            builder.AddEventStopPropagationAttribute(1034, "onclick", true);
            builder.AddContent(1035, "×");
            builder.CloseElement();
        }
    }

    private string RemoveName()
    {
        var label = AccessibleName ?? (Element.Element is { } element
            ? ControlTree.Descendants(element).FirstOrDefault(e => e.Name == "ChipLabel") is { } part ? ControlTree.Text(part) : null
            : null);
        return string.IsNullOrEmpty(label) ? ControlStrings.Get("Remove") : ControlStrings.Format("RemoveItem", label);
    }

    /// <summary>Renders the remove button's name once the label is known.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && EffectiveKind == ChipKind.Input) StateHasChanged();
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Activates the chip as a click does: changes its selection when it selects, then runs <see cref="OnClick" />.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the action has finished.</returns>
    public async Task ActivateAsync(PointerEventArgs e)
    {
        if (IsInactive || Element.Element?.IsDisabled is true) return;
        if (GroupSelects) await Owner!.ActivateAsync(this);
        else if (EffectiveKind == ChipKind.Filter)
        {
            var value = !selected.Value;
            selected.Set(value);
            StateHasChanged();
            await IsSelectedChanged.InvokeAsync(value);
            await Events.RaiseChangeAsync(Element, value);
        }
        await OnClick.InvokeAsync(e);
    }

    /// <summary>Removes an input chip as its remove button does: moves the focus to a neighbor when the chip had it and
    /// runs <see cref="OnRemove" />.</summary>
    /// <returns>A task that completes when the callback has finished.</returns>
    public async Task RemoveAsync()
    {
        if (IsInactive || EffectiveKind != ChipKind.Input) return;
        var hadFocus = Element.Element is { } element && (element.States & (ElementStates.Focus | ElementStates.FocusWithin)) != 0;
        var neighbor = Owner?.Neighbor(this);
        await OnRemove.InvokeAsync();
        if (hadFocus && neighbor is { IsRemoved: false }) await new ElementReference(neighbor).FocusAsync();
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.Key is not ("Backspace" or "Delete")) return Task.CompletedTask;
        e.Handled = true;
        return RemoveAsync();
    }

    /// <summary>Leaves the enclosing group.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}

/// <summary>
/// A wrapping set of <see cref="Chip" /> controls with a single Tab stop: the arrow keys move between chips, Home and
/// End jump to the ends. With <see cref="SelectionMode.Single" /> it is a <c>radiogroup</c> of choice chips whose
/// <see cref="Value" /> (<c>@bind-Value</c>) is the selected chip's value, and the arrow keys also select; with
/// <see cref="SelectionMode.Multiple" /> its filter chips toggle the values in <see cref="Values" />
/// (<c>@bind-Values</c>). <see cref="MaxVisible" /> shows only the first chips and a <c>ChipGroupOverflow</c> button
/// ("+N") that reveals the others. The element is <c>ChipGroup</c> with the <c>selection-mode</c> and <c>size</c>
/// attributes.
/// </summary>
/// <typeparam name="TValue">The type of the chip values.</typeparam>
public sealed class ChipGroup<TValue> : ContentControl, IChipOwner
{
    private readonly string groupId = UniqueId.Create("chips");
    private readonly ControllableState<TValue?> value = new(default);
    private readonly ControllableState<IReadOnlyList<TValue>?> values = new(null);
    private readonly RovingFocusController roving = new() { Orientation = null, Typeahead = false };
    private readonly List<Chip> chips = [];
    private string? activeId;
    private bool expanded;
    private bool membershipChanged;

    /// <summary>Gets or sets whether the chips select nothing, one value or several values.</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; }

    /// <summary>Gets or sets the value of the selected chip in single selection.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another chip in single selection.</summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Gets or sets the values of the selected chips in multiple selection.</summary>
    [Parameter] public IReadOnlyList<TValue>? Values { get; set; }

    /// <summary>Gets or sets the callback invoked with the new values, in chip order, in multiple selection.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TValue>> ValuesChanged { get; set; }

    /// <summary>Gets or sets whether activating the selected chip in single selection clears the value.</summary>
    [Parameter] public bool AllowEmpty { get; set; }

    /// <summary>Gets or sets how many chips are shown before the overflow button, or null to show all.</summary>
    [Parameter] public int? MaxVisible { get; set; }

    /// <summary>Gets or sets the size step of the chips.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether every chip rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the value of the selected chip in single selection.</summary>
    public TValue? SelectedValue => value.Value;

    /// <summary>Gets the values of the selected chips in multiple selection.</summary>
    public IReadOnlyList<TValue> SelectedValues => values.Value ?? [];

    /// <summary>Gets whether the chips beyond <see cref="MaxVisible" /> are shown.</summary>
    public bool IsExpanded => expanded;

    /// <summary>Gets the NSS element name emitted by ChipGroup.</summary>
    protected override string ElementName => "ChipGroup";

    string IChipOwner.GroupId => groupId;

    SelectionMode IChipOwner.Mode => SelectionMode;

    bool IChipOwner.IsInactive => Disabled;

    private int HiddenCount => MaxVisible is { } max && !expanded ? Math.Max(0, chips.Count - max) : 0;

    /// <summary>Validates the parameters and synchronizes the selection.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (MaxVisible is < 0) throw new ArgumentOutOfRangeException(nameof(MaxVisible));
        value.Sync(Value);
        values.Sync(Values);
        membershipChanged |= chips.Count > 0;
    }

    /// <summary>Adds the role and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", SelectionMode == SelectionMode.Single ? "radiogroup" : "group");
        builder.AddAttribute(11, "selection-mode", SelectionMode.ToString().ToLowerInvariant());
        builder.AddAttribute(12, "size", Vocabulary.Name(Size));
        builder.AddAttribute(13, "disabled", Disabled);
        builder.AddAttribute(14, "__internal_onkeydown#chips", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Adds the chips and the overflow button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IChipOwner>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
        var hidden = HiddenCount;
        if (hidden == 0 && !(expanded && MaxVisible is { } max && chips.Count > max)) return;
        var overflowId = groupId + "-overflow";
        builder.OpenElement(1010, "ChipGroupOverflow");
        builder.AddAttribute(1011, "id", overflowId);
        builder.AddAttribute(1012, "role", "button");
        builder.AddAttribute(1013, "chip-group", groupId);
        builder.AddAttribute(1014, "expanded", expanded);
        builder.AddAttribute(1015, "focusable", !Disabled);
        builder.AddAttribute(1016, "tabindex", IsTabStop(overflowId) ? 0 : -1);
        builder.AddAttribute(1017, "size", Vocabulary.Name(Size));
        builder.AddAttribute(1018, "accessible-name", expanded ? ControlStrings.Get("ShowFewer") : ControlStrings.Format("MoreItems", hidden));
        builder.AddAttribute(1019, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleExpanded));
        builder.AddAttribute(1020, "__internal_onfocus#chips", EventCallback.Factory.Create<FocusEventArgs>(this, () => Activate(overflowId)));
        builder.AddContent(1021, expanded ? ControlStrings.Get("ShowFewer") : ControlStrings.Format("AndMore", hidden));
        builder.CloseElement();
    }

    /// <summary>Refreshes the chips and the overflow button after the set of chips or the selection changed.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!membershipChanged) return;
        membershipChanged = false;
        Refresh();
        StateHasChanged();
    }

    private void ToggleExpanded()
    {
        if (Disabled) return;
        expanded = !expanded;
        StateHasChanged();
        Refresh();
    }

    /// <summary>Shows or hides the chips beyond <see cref="MaxVisible" />.</summary>
    /// <param name="value">Whether every chip is shown.</param>
    public void SetExpanded(bool value)
    {
        if (expanded == value) return;
        expanded = value;
        StateHasChanged();
        Refresh();
    }

    private void Refresh()
    {
        foreach (var chip in chips.ToArray()) chip.Refresh();
    }

    private List<Chip> Ordered()
    {
        if (Element.Element is not { } group) return [.. chips];
        var order = ControlTree.Descendants(group).Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);
        return [.. chips.OrderBy(chip => chip.Element.Element is { } node && order.TryGetValue(node, out var index) ? index : int.MaxValue)];
    }

    private List<ElementNode> Items() => Element.Element is { } group
        ? [.. ControlTree.Descendants(group).Where(node => Equals(node.Attributes.GetValueOrDefault("chip-group"), groupId) && node.Attributes.GetValueOrDefault("hidden") is not true)]
        : [];

    private static TValue Convert(Chip chip) => chip.Value is TValue typed ? typed
        : chip.Value is null && default(TValue) is null ? default!
        : throw new InvalidOperationException($"The chip value must be of type {typeof(TValue).Name}.");

    bool IChipOwner.IsSelected(Chip chip) => IsSelected(Convert(chip));

    private bool IsSelected(TValue candidate) => SelectionMode == SelectionMode.Multiple
        ? SelectedValues.Contains(candidate, EqualityComparer<TValue>.Default)
        : SelectionMode == SelectionMode.Single && value.Value is not null && EqualityComparer<TValue?>.Default.Equals(value.Value, candidate);

    bool IChipOwner.IsHidden(Chip chip) => MaxVisible is { } max && !expanded && Ordered().IndexOf(chip) >= max;

    bool IChipOwner.IsTabStop(Chip chip) => IsTabStop(chip.ItemId);

    private bool IsTabStop(string itemId)
    {
        var ordered = Ordered().Where(chip => !((IChipOwner)this).IsHidden(chip)).ToList();
        var candidates = ordered.Where(chip => !chip.IsInactive).Select(chip => chip.ItemId).ToList();
        if (HiddenCount > 0 || expanded && MaxVisible is { } max && chips.Count > max) candidates.Add(groupId + "-overflow");
        if (activeId is not null && candidates.Contains(activeId)) return activeId == itemId;
        var selected = ordered.FirstOrDefault(chip => !chip.IsInactive && chip.Selected);
        return (selected?.ItemId ?? candidates.FirstOrDefault()) == itemId;
    }

    (int Position, int Count) IChipOwner.PositionOf(Chip chip)
    {
        var ordered = Ordered();
        var index = ordered.IndexOf(chip);
        return (index < 0 ? ordered.Count : index + 1, ordered.Count);
    }

    void IChipOwner.Register(Chip chip)
    {
        if (chips.Contains(chip)) return;
        chips.Add(chip);
        membershipChanged = true;
    }

    void IChipOwner.Unregister(Chip chip)
    {
        if (!chips.Remove(chip)) return;
        if (activeId == chip.ItemId) activeId = null;
        Refresh();
        StateHasChanged();
    }

    ElementNode? IChipOwner.Neighbor(Chip chip)
    {
        var items = Items();
        var index = items.FindIndex(node => Equals(node.Attributes.GetValueOrDefault("id"), chip.ItemId));
        if (index < 0) return null;
        activeId = null;
        return items.Skip(index + 1).FirstOrDefault(node => !node.IsDisabled) ?? items.Take(index).LastOrDefault(node => !node.IsDisabled);
    }

    async Task IChipOwner.ActivateAsync(Chip chip)
    {
        if (Disabled || Element.Element?.IsDisabled is true) return;
        activeId = chip.ItemId;
        var candidate = Convert(chip);
        if (SelectionMode == SelectionMode.Multiple)
        {
            var pressed = IsSelected(candidate);
            var next = Ordered().Where(c => c.EffectiveKind == ChipKind.Filter).Select(Convert)
                .Where(v => EqualityComparer<TValue>.Default.Equals(v, candidate) ? !pressed : IsSelected(v)).ToList();
            values.Set(next);
            Refresh();
            await ValuesChanged.InvokeAsync(next);
            await Events.RaiseChangeAsync(Element, next);
            return;
        }
        var selected = IsSelected(candidate);
        if (selected && !AllowEmpty) return;
        var result = selected ? default : candidate;
        value.Set(result);
        Refresh();
        await ValueChanged.InvokeAsync(result);
        await Events.RaiseChangeAsync(Element, result);
    }

    private void Activate(string itemId)
    {
        if (activeId == itemId) return;
        activeId = itemId;
        Refresh();
        StateHasChanged();
    }

    void IChipOwner.Focused(Chip chip) => Activate(chip.ItemId);

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled) return;
        var items = Items();
        var current = items.FindIndex(node => (node.States & ElementStates.Focus) != 0);
        if (current < 0) return;
        var target = roving.Move(e, current, items.Count, i => items[i].IsDisabled);
        if (e.Modifiers == KeyModifiers.None && e.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" or "Home" or "End") e.Handled = true;
        if (target is not { } index) return;
        e.Handled = true;
        if (index == current) return;
        var node = items[index];
        activeId = node.Attributes.GetValueOrDefault("id") as string;
        Refresh();
        StateHasChanged();
        await new ElementReference(node).FocusAsync();
        if (SelectionMode == SelectionMode.Single && node.Attributes.GetValueOrDefault("role") is "radio" && chips.FirstOrDefault(c => c.ItemId == activeId) is { } chip)
            await chip.ActivateAsync(new PointerEventArgs { IsSynthetic = true });
    }
}
