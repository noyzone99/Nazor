using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// An editable choice: the user types to filter <see cref="Items" /> or opens the whole list with the chevron or
/// Alt+Down, then chooses with the arrows and Enter or a click. <see cref="Value" /> (<c>@bind-Value</c>) is the chosen
/// item; <see cref="Text" /> (<c>@bind-Text</c>) is the text in the box. When the text is committed (Enter or leaving
/// the box) an item whose text matches it is chosen; other text is kept as free text with <see cref="AllowCustomValue" />
/// (the value becomes the default) or goes back to the chosen item's text otherwise; empty text clears the value. Built on
/// <see cref="Combobox{TItem}" />, so the parts are <c>ComboBoxInput</c>, <c>ComboBoxLeading</c>, <c>ComboBoxTrailing</c>
/// (holding the <c>ComboBoxChevron</c> button), <c>ComboBoxPopup</c>, <c>ComboBoxOption</c> (<c>current</c> for the
/// highlighted one, <c>selected</c> for the committed value) and <c>ComboBoxEmpty</c>. Inside a <see cref="Field" /> it is labeled and described automatically.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class ComboBox<TItem> : Control
{
    private readonly ControllableState<TItem?> value = new(default);
    private readonly ControllableState<string> text = new("");
    private Combobox<TItem>? combobox;
    private bool filtering;
    private bool synchronized;

    /// <summary>Gets or sets the items offered.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the chosen item.</summary>
    [Parameter] public TItem? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user chooses another item; supports <c>@bind-Value</c>.</summary>
    [Parameter] public EventCallback<TItem?> ValueChanged { get; set; }

    /// <summary>Gets or sets the text in the box; null follows the chosen item.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Gets or sets the callback invoked on every change of the text; supports <c>@bind-Text</c>.</summary>
    [Parameter] public EventCallback<string> TextChanged { get; set; }

    /// <summary>Gets or sets the text of an item; <c>ToString</c> by default.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets how an item is rendered in the list.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets whether an item matches the typed text; by default its text contains the text, ignoring case.</summary>
    [Parameter] public Func<TItem, string, bool>? Filter { get; set; }

    /// <summary>Gets or sets whether text that matches no item is kept as a free value.</summary>
    [Parameter] public bool AllowCustomValue { get; set; }

    /// <summary>Gets or sets content shown in the list when no item matches.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets whether the list opens when the box receives the focus.</summary>
    [Parameter] public bool OpenOnFocus { get; set; }

    /// <summary>Gets or sets the preferred position of the list relative to the field.</summary>
    [Parameter] public DropDownPlacement DropDownPlacement { get; set; }

    /// <summary>Gets or sets the preferred minimum number of visible options, or null for no minimum preference. Positive values must be at most <see cref="PreferredMaxVisibleItems" />.</summary>
    [Parameter] public int? PreferredMinVisibleItems { get; set; }

    /// <summary>Gets or sets the positive preferred maximum number of visible options. The default is 10. Available space can reduce the viewport.</summary>
    [Parameter] public int PreferredMaxVisibleItems { get; set; } = DropDownOptions.DefaultMaximum;

    /// <summary>Gets or sets the hint shown while the text is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether the text cannot be edited; the list can still be opened with the chevron.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether the control rejects interaction.</summary>
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

    /// <summary>Gets the chosen item.</summary>
    public TItem? SelectedValue => value.Value;

    /// <summary>Gets the text in the box.</summary>
    public string CurrentText => text.Value;

    /// <summary>Gets whether the list is open.</summary>
    public bool Opened => combobox?.Opened == true;

    /// <summary>Gets the NSS element name emitted by ComboBox.</summary>
    protected override string ElementName => "ComboBox";

    private bool IsInactive => Disabled || Field?.Disabled == true;

    private bool HasValue => value.Value is not null;

    /// <summary>Validates the parameters and synchronizes the value and the text.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        DropDownOptions.Validate(DropDownPlacement, PreferredMinVisibleItems, PreferredMaxVisibleItems);
        var valueChanged = value.Sync(Value);
        if (Text is not null) text.Sync(Text);
        else if (valueChanged || !synchronized) text.Set(HasValue ? TextOf(value.Value!) : "");
        synchronized = true;
    }

    /// <summary>Gets the text of an item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The text shown for it.</returns>
    public string TextOf(TItem item) => ItemText is { } describe ? describe(item) : item?.ToString() ?? "";

    /// <summary>Gets the items shown in the list: the matches of the typed text, or all of them after the chevron opened the list.</summary>
    /// <returns>The items.</returns>
    public IReadOnlyList<TItem> VisibleItems()
    {
        var query = text.Value.Trim();
        if (!filtering || query.Length == 0) return Items;
        return [.. Items.Where(item => Filter is { } filter ? filter(item, query) : TextOf(item).Contains(query, StringComparison.CurrentCultureIgnoreCase))];
    }

    /// <summary>Delegates the rendering to a combo box primitive named after the control.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var attributes = new Dictionary<string, object?>(AdditionalAttributes ?? new Dictionary<string, object?>())
        {
            ["appearance"] = Appearance.ToString().ToLowerInvariant(),
            ["size"] = Vocabulary.Name(Size),
            ["empty"] = text.Value.Length == 0,
            ["readonly"] = ReadOnly,
            ["invalid"] = Invalid || Field?.Invalid == true,
        };
        var input = new Dictionary<string, object?>
        {
            [nameof(EditableText.ReadOnly)] = ReadOnly,
            [nameof(EditableText.Required)] = Required,
            [nameof(EditableText.Invalid)] = Invalid,
            [nameof(Control.AccessibleDescription)] = AccessibleDescription,
            [nameof(Control.DescribedBy)] = DescribedBy,
            [nameof(Control.TabIndex)] = TabIndex,
            [nameof(EditableText.OnChange)] = EventCallback.Factory.Create<ChangeEventArgs>(this, CommittedAsync),
        };
        OpenDelegatedComponent<Combobox<TItem>>(builder, 0);
        builder.AddComponentParameter(1, nameof(Combobox<TItem>.As), "ComboBox");
        builder.AddComponentParameter(2, nameof(Combobox<TItem>.Text), text.Value);
        builder.AddComponentParameter(3, nameof(Combobox<TItem>.TextChanged), EventCallback.Factory.Create<string>(this, TypedAsync));
        builder.AddComponentParameter(4, nameof(Combobox<TItem>.Items), VisibleItems());
        builder.AddComponentParameter(5, nameof(Combobox<TItem>.ItemText), (Func<TItem, string>)TextOf);
        builder.AddComponentParameter(6, nameof(Combobox<TItem>.ItemTemplate), ItemTemplate);
        builder.AddComponentParameter(7, nameof(Combobox<TItem>.OnSelect), EventCallback.Factory.Create<TItem>(this, ChooseAsync));
        builder.AddComponentParameter(8, nameof(Combobox<TItem>.FillOnSelect), false);
        builder.AddComponentParameter(9, nameof(Combobox<TItem>.EmptyContent), EmptyContent);
        builder.AddComponentParameter(10, nameof(Combobox<TItem>.OpenOnFocus), OpenOnFocus);
        builder.AddComponentParameter(11, nameof(Combobox<TItem>.Placeholder), Placeholder);
        builder.AddComponentParameter(12, nameof(Combobox<TItem>.Disabled), Disabled);
        builder.AddComponentParameter(13, nameof(Combobox<TItem>.AutoFocus), AutoFocus);
        builder.AddComponentParameter(14, nameof(Combobox<TItem>.Leading), Leading);
        builder.AddComponentParameter(15, nameof(Combobox<TItem>.Trailing), (RenderFragment)AddChevron);
        builder.AddComponentParameter(16, nameof(Combobox<TItem>.InputAttributes), input);
        builder.AddComponentParameter(17, nameof(Class), Class);
        builder.AddComponentParameter(18, nameof(Id), Id);
        builder.AddComponentParameter(19, nameof(AccessibleName), AccessibleName);
        builder.AddComponentParameter(20, nameof(LabeledBy), LabeledBy);
        builder.AddComponentParameter(21, nameof(Tooltip), Tooltip);
        builder.AddComponentParameter(22, nameof(Style), Style);
        builder.AddComponentParameter(23, nameof(AdditionalAttributes), attributes);
        builder.AddComponentParameter(24, nameof(Combobox<TItem>.DropDownPlacement), DropDownPlacement);
        builder.AddComponentParameter(25, nameof(Combobox<TItem>.PreferredMinVisibleItems), PreferredMinVisibleItems);
        builder.AddComponentParameter(26, nameof(Combobox<TItem>.PreferredMaxVisibleItems), PreferredMaxVisibleItems);
        builder.AddComponentReferenceCapture(27, component => combobox = (Combobox<TItem>)component);
        builder.AddComponentParameter(28, nameof(Combobox<TItem>.IsItemSelected), (Func<TItem, bool>)(item => HasValue && EqualityComparer<TItem>.Default.Equals(value.Value!, item)));
        builder.CloseComponent();
    }

    /// <summary>Exposes the combo box element as the control's element.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (combobox is not null) Element = combobox.Element;
    }

    private void AddChevron(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "ComboBoxChevron");
        builder.AddAttribute(1, "role", "button");
        builder.AddAttribute(2, "glyph", "chevron-down");
        builder.AddAttribute(3, "accessible-name", ControlStrings.Get("ShowSuggestions"));
        builder.AddAttribute(4, "tabindex", -1);
        builder.AddAttribute(5, "disabled", IsInactive);
        builder.AddAttribute(6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        builder.CloseElement();
    }

    /// <summary>Opens the whole list when closed and closes it when open, as the chevron does.</summary>
    /// <returns>A task that completes when the list has changed.</returns>
    public async Task ToggleAsync()
    {
        if (IsInactive || combobox is null) return;
        filtering = false;
        StateHasChanged();
        await combobox.SetOpenAsync(!combobox.Opened);
        await FocusAsync(preventScroll: true);
    }

    /// <summary>Chooses an item as the user would, writes its text and reports the change.</summary>
    /// <param name="item">The item to choose.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ChooseAsync(TItem item)
    {
        if (IsInactive) return;
        filtering = false;
        await SetTextAsync(TextOf(item));
        await SetValueAsync(item);
    }

    /// <summary>Moves the keyboard focus to the editor.</summary>
    /// <param name="preventScroll">Whether focusing keeps the current scroll position.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    public new Task FocusAsync(bool preventScroll = false) =>
        combobox?.Element.Element is { } root && ControlTree.Descendants(root).FirstOrDefault(e => e.Name == "ComboBoxInput") is { } input
            ? new ElementReference(input).FocusAsync(preventScroll) : Task.CompletedTask;

    private async Task TypedAsync(string typed)
    {
        filtering = true;
        await SetTextAsync(typed);
    }

    private async Task CommittedAsync(ChangeEventArgs e)
    {
        if (e.Value is not string committed || IsInactive || ReadOnly) return;
        var query = committed.Trim();
        if (query.Length == 0)
        {
            await SetValueAsync(default);
            return;
        }
        var match = Items.FirstOrDefault(item => string.Equals(TextOf(item), query, StringComparison.CurrentCultureIgnoreCase));
        if (match is not null)
        {
            await ChooseAsync(match);
            return;
        }
        if (AllowCustomValue)
        {
            await SetValueAsync(default);
            return;
        }
        filtering = false;
        await SetTextAsync(HasValue ? TextOf(value.Value!) : "");
    }

    private async Task SetTextAsync(string next)
    {
        if (!text.Set(next)) return;
        StateHasChanged();
        await TextChanged.InvokeAsync(next);
    }

    private async Task SetValueAsync(TItem? next)
    {
        if (!value.Set(next)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(next);
        await Events.RaiseChangeAsync(Element, next);
    }
}
