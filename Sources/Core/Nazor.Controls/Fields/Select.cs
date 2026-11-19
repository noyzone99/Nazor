using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A choice of one value from a list that opens above or below the box. <see cref="Value" /> (<c>@bind-Value</c>) is the chosen
/// value. The options are the <see cref="SelectItem{TValue}" /> written inside, or <see cref="Items" /> shown with
/// <see cref="ItemText" /> (<c>ToString</c> by default) or <see cref="ItemTemplate" />. A click, Space, Enter, Alt+Down or F4
/// open the list on the chosen value; there the arrows move, Enter or a click choose and close, Escape or a click outside
/// close without choosing. Arrow navigation stops at the ends and scrolls the focused option into view.
/// With the list closed the arrows, Home, End and typing change the value directly. The parts
/// are <c>SelectLeading</c>, <c>SelectValue</c> (with <c>placeholder</c> when empty), <c>SelectChevron</c>,
/// <c>SelectPopup</c>, <c>SelectGroupHeader</c>, <c>SelectOption</c> (<c>selected</c> for the chosen one) and its
/// <c>SelectOptionCheck</c> mark.
/// </summary>
/// <typeparam name="TValue">The type of the values.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
public sealed class Select<TValue> : Control
{
    private readonly List<SelectItem<TValue>> declared = [];
    private readonly ControllableState<TValue?> value = new(default);
    private readonly ControllableState<bool> open = new(false);
    private readonly RovingFocusController typeahead = new() { Typeahead = true };

    /// <summary>Gets or sets the chosen value.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user chooses another value.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets or sets the values offered, when there are no declared items.</summary>
    [Parameter] public IEnumerable<TValue>? Items { get; set; }

    /// <summary>Gets or sets the text of a value.</summary>
    [Parameter] public Func<TValue, string>? ItemText { get; set; }

    /// <summary>Gets or sets how a value is rendered in the list and in the box.</summary>
    [Parameter] public RenderFragment<TValue>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the text shown when no value is chosen.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the control rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the list is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the list opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the preferred position of the list relative to the field.</summary>
    [Parameter] public DropDownPlacement DropDownPlacement { get; set; }

    /// <summary>Gets or sets the preferred minimum number of visible options, or null for no minimum preference. Positive values must be at most <see cref="PreferredMaxVisibleItems" />.</summary>
    [Parameter] public int? PreferredMinVisibleItems { get; set; }

    /// <summary>Gets or sets the positive preferred maximum number of visible options. The default is 10. Available space can reduce the viewport.</summary>
    [Parameter] public int PreferredMaxVisibleItems { get; set; } = DropDownOptions.DefaultMaximum;

    /// <summary>Gets or sets content shown in the box before the value, such as an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets how the box is drawn.</summary>
    [Parameter] public FieldAppearance Appearance { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the declared <see cref="SelectItem{TValue}" /> options.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the chosen value.</summary>
    public TValue? SelectedValue => value.Value;

    /// <summary>Gets whether the list is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the NSS element name emitted by Select.</summary>
    protected override string ElementName => "Select";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Synchronizes the value and the open state.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        DropDownOptions.Validate(DropDownPlacement, PreferredMinVisibleItems, PreferredMaxVisibleItems);
        value.Sync(Value);
        open.Sync(IsOpen);
    }

    private readonly record struct Entry(TValue Value, RenderFragment Content, string Text, bool Disabled, string? Group);

    private List<Entry> Entries() => Items is not null
        ? [.. Items.Select(item => new Entry(item, ItemTemplate is { } template ? template(item) : TextFragment(TextOf(item)), TextOf(item), false, null))]
        : [.. declared.Select(item => new Entry(item.Value, item.ChildContent ?? TextFragment(item.Text ?? TextOf(item.Value)), item.Text ?? TextOf(item.Value), item.Disabled, item.Group))];

    private string TextOf(TValue item) => ItemText is { } text ? text(item) : item?.ToString() ?? "";

    private static RenderFragment TextFragment(string text) => builder => builder.AddContent(0, text);

    private bool IsChosen(TValue candidate) => EqualityComparer<TValue>.Default.Equals(value.Value!, candidate);

    /// <summary>Adds the combo box role, the states and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        builder.AddAttribute(11, "__internal_onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(12, "disabled", IsInactive);
        builder.AddAttribute(13, "role", "combobox");
        builder.AddAttribute(14, "focusable", !IsInactive);
        builder.AddAttribute(15, "expanded", open.Value);
        builder.AddAttribute(16, "appearance", Appearance.ToString().ToLowerInvariant());
        builder.AddAttribute(17, "size", Vocabulary.Name(Size));
        builder.AddAttribute(18, "invalid", Field?.Invalid == true);
        builder.AddAttribute(19, "required", Field?.Required == true);
        if (Id is null && Field is { } field) builder.AddAttribute(20, "id", field.ControlId);
        Field?.ApplyTo(builder, 21, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the box parts, the list and the declared items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var entries = Entries();
        var chosen = entries.FindIndex(e => IsChosen(e.Value));
        if (Leading is not null)
        {
            builder.OpenElement(1000, "SelectLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        builder.OpenElement(1010, "SelectValue");
        builder.AddAttribute(1011, "placeholder", chosen < 0);
        builder.AddAttribute(1012, "combo-value", true);
        if (chosen >= 0) builder.AddContent(1013, entries[chosen].Content);
        else builder.AddContent(1014, Placeholder);
        builder.CloseElement();
        builder.OpenElement(1020, "SelectChevron");
        builder.AddAttribute(1021, "glyph", "chevron-down");
        builder.AddAttribute(1022, "semantic-hidden", true);
        builder.CloseElement();
        {
            builder.OpenComponent<PopoverContainer>(1030);
            builder.AddComponentParameter(1031, nameof(PopoverContainer.As), "SelectPopup");
            builder.AddComponentParameter(1032, nameof(PopoverContainer.IsOpen), open.Value);
            builder.AddComponentParameter(1033, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => v ? Task.CompletedTask : SetOpenAsync(false)));
            builder.AddComponentParameter(1034, nameof(PopoverContainer.Anchor), Element);
            builder.AddComponentParameter(1035, nameof(PopoverContainer.MatchAnchorWidth), true);
            builder.AddComponentParameter(1036, nameof(PopoverContainer.Role), "listbox");
            builder.AddComponentParameter(1037, nameof(PopoverContainer.IsMenu), true);
            builder.AddComponentParameter(1038, nameof(PopoverContainer.ChildContent), (RenderFragment)(content => RenderOptions(content, entries)));
            builder.AddComponentParameter(1039, nameof(PopoverContainer.AdditionalAttributes),
                DropDownOptions.Attributes(DropDownPlacement, PreferredMinVisibleItems, PreferredMaxVisibleItems));
            builder.CloseComponent();
        }
        builder.OpenComponent<CascadingValue<Select<TValue>>>(1040);
        builder.AddComponentParameter(1041, "Value", this);
        builder.AddComponentParameter(1042, "IsFixed", true);
        builder.AddComponentParameter(1043, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    private void RenderOptions(RenderTreeBuilder builder, List<Entry> entries)
    {
        string? group = null;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Group is not null && entry.Group != group)
            {
                builder.OpenElement(0, "SelectGroupHeader");
                builder.SetKey("group:" + entry.Group + ":" + i);
                builder.AddAttribute(1, "role", "text");
                builder.AddContent(2, entry.Group);
                builder.CloseElement();
            }
            group = entry.Group;
            builder.OpenElement(10, "SelectOption");
            builder.SetKey(i);
            builder.AddAttribute(11, "role", "option");
            builder.AddAttribute(12, "selected", IsChosen(entry.Value));
            builder.AddAttribute(13, "disabled", entry.Disabled);
            builder.AddAttribute(14, "focusable", !entry.Disabled);
            builder.AddAttribute(15, "position-in-set", i + 1);
            builder.AddAttribute(16, "size-of-set", entries.Count);
            builder.AddAttribute(17, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => SelectAsync(entry.Value)));
            builder.OpenElement(18, "SelectOptionCheck");
            builder.AddAttribute(19, "glyph", IsChosen(entry.Value) ? "check" : null);
            builder.AddAttribute(20, "semantic-hidden", true);
            builder.CloseElement();
            builder.AddContent(21, entry.Content);
            builder.CloseElement();
        }
    }

    internal void Register(SelectItem<TValue> item)
    {
        declared.Add(item);
        StateHasChanged();
    }

    internal void Unregister(SelectItem<TValue> item)
    {
        if (declared.Remove(item)) StateHasChanged();
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Opens the list unless disabled.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => IsInactive ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the list without changing the value.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    /// <summary>Opens the list when closed and closes it when open.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task ToggleAsync() => IsInactive ? Task.CompletedTask : SetOpenAsync(!open.Value);

    private async Task SetOpenAsync(bool next)
    {
        if (!open.Set(next)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(next);
    }

    /// <summary>Chooses a value as the user would, closes the list and reports the change.</summary>
    /// <param name="candidate">The value to choose; it must be an enabled option.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SelectAsync(TValue candidate)
    {
        if (IsInactive || !Entries().Any(entry => EqualityComparer<TValue>.Default.Equals(entry.Value, candidate) && !entry.Disabled)) return;
        await CloseAsync();
        if (!value.Set(candidate)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(candidate);
        await Events.RaiseChangeAsync(Element, candidate);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (IsInactive || e.Handled) return Task.CompletedTask;
        if (e.Key == "F4" || (e.Key == "ArrowDown" && (e.Modifiers & KeyModifiers.Alt) != 0))
        {
            e.Handled = true;
            return OpenAsync();
        }
        if (open.Value) return Task.CompletedTask;
        var entries = Entries();
        var current = entries.FindIndex(entry => IsChosen(entry.Value));
        typeahead.Orientation = Orientation.Vertical;
        var target = typeahead.Move(e, current, entries.Count, i => entries[i].Disabled, i => entries[i].Text);
        if (target is not { } index) return Task.CompletedTask;
        e.Handled = true;
        return SelectAsync(entries[index].Value);
    }
}

/// <summary>
/// An option of a <see cref="Select{TValue}" />, with its content (the text of the value by default). Options with the
/// same <see cref="Group" /> are listed under a header with that title.
/// </summary>
/// <typeparam name="TValue">The type of the values.</typeparam>
public sealed class SelectItem<TValue> : ComponentBase, IDisposable
{
    private Select<TValue>? registered;
    private (TValue Value, bool Disabled, string? Text, string? Group)? shown;

    /// <summary>Gets or sets the value chosen by this option.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets whether the option cannot be chosen.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the text used for typing and, without content, shown.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Gets or sets the title of the group the option belongs to.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>Gets or sets the content of the option.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the enclosing select, supplied automatically.</summary>
    [CascadingParameter] public Select<TValue>? Owner { get; set; }

    /// <summary>Joins the enclosing select and refreshes it when the option changes.</summary>
    protected override void OnParametersSet()
    {
        if (registered != Owner)
        {
            registered?.Unregister(this);
            (registered = Owner)?.Register(this);
        }
        else if (!Equals(shown, (Value, Disabled, Text, Group))) registered?.Refresh();
        shown = (Value, Disabled, Text, Group);
    }

    /// <summary>Renders nothing; the select renders the option.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    /// <summary>Leaves the enclosing select.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
