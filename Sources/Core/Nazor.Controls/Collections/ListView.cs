using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A virtualized list of <see cref="Items" /> with selection, built on <see cref="Virtualizer{TItem}" /> and
/// <see cref="SelectionModel{TKey}" />: only the visible rows exist, so a list of a hundred thousand items costs as much
/// as a screenful. Each row shows <see cref="ItemTemplate" /> or <see cref="ItemText" />. The list is a single Tab stop:
/// the arrows, Home, End, Page Up and Page Down move the current row (marked <c>current</c>) and the selection with it,
/// Shift extends a multiple selection, Ctrl moves without selecting, Space toggles, Ctrl+A selects all, typing jumps to a
/// matching row and Enter or a double click raise <see cref="OnItemActivated" />. <see cref="SelectedItem" />
/// (<c>@bind-SelectedItem</c>) holds a single selection and <see cref="SelectedItems" /> (<c>@bind-SelectedItems</c>) a
/// multiple one. <see cref="GroupBy" /> adds section headers; <see cref="OnEndReached" /> and
/// <see cref="IsLoadingMore" /> support incremental loading. The element <c>ListView</c> (role <c>listbox</c>) holds the
/// scrolling <c>ListViewViewport</c> (which needs a height, given by the theme or a class), the rows
/// <c>ListViewItem</c> (role <c>option</c>, with <c>ListViewItemCheck</c> and <c>ListViewItemContent</c>),
/// <c>ListViewSectionHeader</c>, <c>ListViewEmpty</c> and <c>ListViewLoadingMore</c>. The noninteractive
/// <c>ListViewRowMetric</c> supplies the theme's automatic row height when <see cref="ItemHeight"/> is null.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class ListView<TItem> : Control
{
    private readonly ItemSelection<TItem> selection = new();
    private readonly RovingFocusController keys = new() { Orientation = Orientation.Vertical };
    private readonly RowFocus focus = new();
    private readonly ItemReorderController reorder;
    private readonly HoverTracker hover;
    private object? reorderedKey;
    private double measuredItemHeight = 32;
    private readonly RenderFragment<VirtualItem<Entry>> rowTemplate;
    private readonly Dictionary<object, int> rowOf = [];
    private Virtualizer<Entry>? virtualizer;
    private List<Entry> entries = [];
    private IReadOnlyList<TItem>? source;
    private int sourceCount = -1;
    private Func<TItem, string>? grouping;
    private Func<TItem, object>? keying;
    private VirtualRange range;
    private int current = -1;
    private int tabStop = -1;
    private int endReachedAt = -1;

    /// <summary>Creates a list view.</summary>
    public ListView()
    {
        hover = new(StateHasChanged);
        rowTemplate = item => builder => BuildRow(builder, item.Item, item.Index);
        selection.Key = KeyOf;
        reorder = new(() => Element, "ListViewItem", row => IsSkipped(row) ? null : KeyOf(entries[row].Item), CanMoveRow, ReorderAsync, StateHasChanged);
    }

    private readonly record struct Entry(TItem Item, string? Group, bool IsHeader, int Position, int Size);

    /// <summary>Gets or sets the items, in display order unless <see cref="GroupBy" /> groups them.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets how each item is rendered; <see cref="ItemText" /> is shown when null.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the text of an item, shown without a template and matched by typing; <c>ToString</c> is used when null.
    /// With a template it also becomes the accessible name of the row.
    /// </summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets a function that gives each item a stable key for selection and rendering; the item itself is used when null.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected.</summary>
    [Parameter] public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>Gets or sets a fixed row height, or an estimate with <see cref="MeasureItems"/>. Null uses the theme's measured ListViewRowMetric; Basic falls back to 32 logical units.</summary>
    [Parameter] public double? ItemHeight { get; set; }

    /// <summary>Gets or sets whether rows are measured after layout, for rows of different heights.</summary>
    [Parameter] public bool MeasureItems { get; set; }

    /// <summary>Gets or sets how many items can be selected.</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Gets or sets the selected item in single selection.</summary>
    [Parameter] public TItem? SelectedItem { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another item in single selection.</summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Gets or sets the selected items in multiple selection.</summary>
    [Parameter] public IReadOnlyCollection<TItem>? SelectedItems { get; set; }

    /// <summary>Gets or sets the callback invoked with the selected items, in display order, when the user changes a multiple selection.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// Gets or sets whether rows show a check mark in multiple selection; a click or Space then toggles the row and the
    /// arrows move without changing the selection, as in a checklist.
    /// </summary>
    [Parameter] public bool ShowCheckBoxes { get; set; }

    /// <summary>Gets or sets whether primary-pointer dragging and Ctrl+Shift+Up/Down can request single-item moves within the current group. Requires <see cref="OnItemReorder"/>.</summary>
    [Parameter] public bool CanReorderItems { get; set; }

    /// <summary>Gets or sets the callback requesting a move in <see cref="Items"/>. The application applies it and supplies updated items; selection follows stable item keys.</summary>
    [Parameter] public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }

    /// <summary>Gets or sets the callback invoked when a row is activated with Enter or a double click.</summary>
    [Parameter] public EventCallback<TItem> OnItemActivated { get; set; }

    /// <summary>
    /// Gets or sets a function that gives the group of an item; the items are then shown by group, in the order in which
    /// each group first appears, each group under a <c>ListViewSectionHeader</c>.
    /// </summary>
    [Parameter] public Func<TItem, string>? GroupBy { get; set; }

    /// <summary>Gets or sets how a section header is rendered; the group name is shown when null.</summary>
    [Parameter] public RenderFragment<string>? GroupHeaderTemplate { get; set; }

    /// <summary>Gets or sets the content shown when there are no items; a localized "No results" when null.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets the callback invoked when the realized rows change.</summary>
    [Parameter] public EventCallback<VirtualRange> OnRangeChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the realized rows come within <see cref="EndThreshold" /> of the end, once
    /// for each number of items, to load the next page.
    /// </summary>
    [Parameter] public EventCallback OnEndReached { get; set; }

    /// <summary>Gets or sets how many rows before the end <see cref="OnEndReached" /> is invoked.</summary>
    [Parameter] public int EndThreshold { get; set; } = 10;

    /// <summary>Gets or sets whether more items are being loaded; the <c>ListViewLoadingMore</c> part is then shown.</summary>
    [Parameter] public bool IsLoadingMore { get; set; }

    /// <summary>Gets or sets the content of the <c>ListViewLoadingMore</c> part; a localized "Loading" when null.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Gets or sets whether the list rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the index of the current row among the displayed rows, section headers included, or -1.</summary>
    public int CurrentIndex => current;

    /// <summary>Gets the realized rows.</summary>
    public VirtualRange Range => range;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "ListView";

    /// <summary>Gets the row height variable and the caller's declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-list-item-height: " + Css.Px(RowHeight), Style);

    private double RowHeight => ItemHeight ?? measuredItemHeight;

    /// <summary>Validates the parameters and synchronizes the rows and the selection.</summary>
    protected override void OnParametersSet()
    {
        if (ItemHeight is { } itemHeight && (!double.IsFinite(itemHeight) || itemHeight <= 0)) throw new ArgumentOutOfRangeException(nameof(ItemHeight));
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (EndThreshold < 0) throw new ArgumentOutOfRangeException(nameof(EndThreshold));
        var items = Items ?? [];
        if (!ReferenceEquals(items, source) || items.Count != sourceCount || GroupBy != grouping || ItemKey != keying) Rebuild(items);
        selection.Sync(SelectionMode, SelectedItem, SelectedItems);
        reorder.Enabled = CanReorderItems && !Disabled && OnItemReorder.HasDelegate;
        reorder.Synchronize();
        ResolveReorderedItem();
        UpdateTabStop();
    }

    /// <summary>Reads the items again, after the list passed to <see cref="Items" /> was changed in place.</summary>
    public void Refresh()
    {
        Rebuild(Items ?? []);
        reorder.Synchronize();
        UpdateTabStop();
        StateHasChanged();
    }

    /// <summary>Scrolls so that an item is visible and makes it the current row.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the item is in the list.</returns>
    public bool ScrollIntoView(TItem item)
    {
        if (!rowOf.TryGetValue(KeyOf(item), out var row)) return false;
        current = row;
        UpdateTabStop();
        virtualizer?.ScrollIntoView(row);
        StateHasChanged();
        return true;
    }

    private object KeyOf(TItem item) => ItemKey?.Invoke(item) ?? item!;

    private void Rebuild(IReadOnlyList<TItem> items)
    {
        var previousCurrent = IsSkipped(current) ? null : KeyOf(entries[current].Item);
        source = items;
        sourceCount = items.Count;
        grouping = GroupBy;
        keying = ItemKey;
        var list = new List<Entry>(items.Count);
        if (GroupBy is { } group)
        {
            foreach (var members in items.GroupBy(item => group(item) ?? ""))
            {
                var size = members.Count();
                list.Add(new Entry(default!, members.Key, true, 0, size));
                var position = 0;
                foreach (var item in members) list.Add(new Entry(item, members.Key, false, ++position, size));
            }
        }
        else
            for (var i = 0; i < items.Count; i++) list.Add(new Entry(items[i], null, false, i + 1, items.Count));
        entries = list;
        rowOf.Clear();
        for (var i = 0; i < list.Count; i++)
            if (!list[i].IsHeader) rowOf.TryAdd(KeyOf(list[i].Item), i);
        selection.Model.SetOrder(list.Where(e => !e.IsHeader).Select(e => KeyOf(e.Item)));
        if (previousCurrent is not null && rowOf.TryGetValue(previousCurrent, out var retained)) current = retained;
        if (current >= list.Count) current = list.Count - 1;
    }

    private bool IsSkipped(int row) => row < 0 || row >= entries.Count || entries[row].IsHeader || ItemDisabled?.Invoke(entries[row].Item) == true;

    private bool CanMoveRow(int from, int to) => !IsSkipped(from) && !IsSkipped(to) && entries[from].Group == entries[to].Group;

    private async Task ReorderAsync(int from, int to)
    {
        if (!reorder.Enabled || !CanMoveRow(from, to)) return;
        var item = entries[from].Item;
        var itemKey = KeyOf(item);
        var targetKey = KeyOf(entries[to].Item);
        var oldIndex = -1;
        var newIndex = -1;
        for (var i = 0; i < Items.Count && (oldIndex < 0 || newIndex < 0); i++)
        {
            var key = KeyOf(Items[i]);
            if (Equals(key, itemKey)) oldIndex = i;
            if (Equals(key, targetKey)) newIndex = i;
        }
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;
        reorderedKey = itemKey;
        await OnItemReorder.InvokeAsync(new(item, oldIndex, newIndex));
        StateHasChanged();
    }

    private void ResolveReorderedItem()
    {
        if (reorderedKey is not { } key || !rowOf.TryGetValue(key, out var row)) return;
        reorderedKey = null;
        current = row;
        if (RowFocus.HasFocusWithin(Element))
        {
            virtualizer?.ScrollIntoView(row);
            focus.Pending = row;
        }
    }

    private string? TextOf(int row) => entries[row].IsHeader ? null : ItemText?.Invoke(entries[row].Item) ?? entries[row].Item?.ToString();

    private void UpdateTabStop()
    {
        if (!IsSkipped(current)) { tabStop = current; return; }
        tabStop = -1;
        if (selection.Model.Count > 0)
            for (var i = 0; i < entries.Count && tabStop < 0; i++)
                if (!IsSkipped(i) && selection.IsSelected(entries[i].Item)) tabStop = i;
        for (var i = 0; i < entries.Count && tabStop < 0; i++)
            if (!IsSkipped(i)) tabStop = i;
    }

    private bool Realized(int row) => row >= range.First && row < range.Last;

    /// <summary>Adds the list role, the selection mode and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var rootStop = !Disabled && tabStop >= 0 && !Realized(tabStop);
        builder.AddAttribute(10, "role", "listbox");
        builder.AddAttribute(11, "multiselectable", SelectionMode == SelectionMode.Multiple);
        builder.AddAttribute(12, "disabled", Disabled);
        builder.AddAttribute(13, "focusable", rootStop);
        if (TabIndex is null && rootStop) builder.AddAttribute(14, "tabindex", 0);
        builder.AddAttribute(15, "selection-mode", SelectionMode.ToString().ToLowerInvariant());
        builder.AddAttribute(16, "grouped", GroupBy is not null);
        builder.AddAttribute(17, "loading", IsLoadingMore);
        builder.AddAttribute(18, "empty", entries.Count == 0);
        builder.AddAttribute(19, "__internal_onkeydown#listview", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(20, "__internal_onfocus#listview", EventCallback.Factory.Create<FocusEventArgs>(this, RootFocused));
        builder.AddAttribute(21, "reorderable", reorder.Enabled);
        builder.AddAttribute(22, "dragging", reorder.IsDragging);
        builder.AddAttribute(23, "__internal_onpointermove#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Move));
        builder.AddAttribute(24, "__internal_onpointerup#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.UpAsync));
        builder.AddAttribute(25, "__internal_onlostpointercapture#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Lost));
    }

    /// <summary>Adds the viewport with the realized rows, or the empty content, and the loading part.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(500, "ListViewRowMetric");
        builder.AddAttribute(501, "semantic-hidden", true);
        builder.AddAttribute(502, "__internal_onresize#metric", EventCallback.Factory.Create<ResizeEventArgs>(this, MeasureRow));
        builder.CloseElement();
        if (entries.Count == 0 && !IsLoadingMore)
        {
            builder.OpenElement(1000, "ListViewEmpty");
            builder.AddAttribute(1001, "role", "text");
            if (EmptyContent is null) builder.AddContent(1002, ControlStrings.Get("NoResults"));
            else builder.AddContent(1003, EmptyContent);
            builder.CloseElement();
        }
        else if (entries.Count > 0)
        {
            builder.OpenComponent<Virtualizer<Entry>>(1010);
            builder.AddComponentParameter(1011, nameof(Virtualizer<Entry>.As), "ListViewViewport");
            builder.AddComponentParameter(1012, nameof(Virtualizer<Entry>.Items), entries);
            builder.AddComponentParameter(1013, nameof(Virtualizer<Entry>.ItemSize), RowHeight);
            builder.AddComponentParameter(1014, nameof(Virtualizer<Entry>.MeasureItems), MeasureItems);
            builder.AddComponentParameter(1015, nameof(Virtualizer<Entry>.ItemTemplate), rowTemplate);
            builder.AddComponentParameter(1016, nameof(Virtualizer<Entry>.OnRangeChanged), EventCallback.Factory.Create<VirtualRange>(this, RangeChangedAsync));
            if (ItemKey is not null)
                builder.AddComponentParameter(1017, nameof(Virtualizer<Entry>.ItemKey), (Func<Entry, object>)(e => e.IsHeader ? ("group", e.Group) : KeyOf(e.Item)));
            builder.AddComponentParameter(1018, nameof(Virtualizer<Entry>.AdditionalAttributes), hover.Attributes(this, () => virtualizer?.Element, row => row.Name == "ListViewItem"));
            builder.AddComponentParameter(1019, nameof(Virtualizer<Entry>.OverlayContent), (RenderFragment)(content =>
            {
                content.OpenComponent<Indicator>(0);
                content.AddComponentParameter(1, nameof(Indicator.As), "HoverIndicator");
                content.AddComponentParameter(2, nameof(Indicator.Target), hover.Target);
                content.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(1020, component => virtualizer = (Virtualizer<Entry>)component);
            builder.CloseComponent();
        }
        if (IsLoadingMore)
        {
            builder.OpenElement(1100, "ListViewLoadingMore");
            builder.AddAttribute(1101, "role", "status");
            builder.AddAttribute(1102, "live", "polite");
            if (LoadingContent is null) builder.AddContent(1103, ControlStrings.Get("Loading"));
            else builder.AddContent(1104, LoadingContent);
            builder.CloseElement();
        }
    }

    private void BuildRow(RenderTreeBuilder builder, Entry entry, int row)
    {
        if (entry.IsHeader)
        {
            builder.OpenElement(0, "ListViewSectionHeader");
            builder.AddAttribute(1, "role", "text");
            builder.AddAttribute(2, "index", row);
            if (GroupHeaderTemplate is null) builder.AddContent(3, entry.Group);
            else builder.AddContent(4, GroupHeaderTemplate(entry.Group!));
            builder.CloseElement();
            return;
        }
        var item = entry.Item;
        var disabled = ItemDisabled?.Invoke(item) == true;
        var selected = selection.IsSelected(item);
        builder.OpenElement(10, "ListViewItem");
        builder.AddAttribute(11, "role", "option");
        builder.AddAttribute(12, "index", row);
        builder.AddAttribute(13, "selected", selected);
        builder.AddAttribute(14, "current", row == current);
        builder.AddAttribute(15, "disabled", disabled);
        builder.AddAttribute(16, "focusable", !disabled && !Disabled);
        builder.AddAttribute(17, "tabindex", row == tabStop ? 0 : -1);
        builder.AddAttribute(18, "position-in-set", entry.Position);
        builder.AddAttribute(19, "size-of-set", entry.Size);
        if (ItemTemplate is not null && ItemText is not null) builder.AddAttribute(20, "accessible-name", ItemText(item));
        builder.AddAttribute(21, "accessible-description", entry.Group);
        builder.AddAttribute(22, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => ClickAsync(row, e)));
        builder.AddAttribute(23, "__internal_ondblclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => reorder.SuppressPointerActivation ? Task.CompletedTask : ActivateAsync(row)));
        builder.AddAttribute(24, "__internal_onpointerdown#listview", EventCallback.Factory.Create<PointerEventArgs>(this, e => PointerDownAsync(row, e)));
        builder.AddAttribute(25, "__internal_onfocus#listview", EventCallback.Factory.Create<FocusEventArgs>(this, () => RowFocused(row)));
        builder.AddAttribute(26, "item-position", new LogicalItemPosition((this, entry.Group), entry.Position, entry.Size));
        builder.AddAttribute(27, "__internal_onpointerdown#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, e => reorder.Down(row, e)));
        builder.AddAttribute(28, "dragging", row == reorder.Source);
        builder.AddAttribute(29, "drop-before", row == reorder.Target && !reorder.After);
        builder.AddAttribute(30, "drop-after", row == reorder.Target && reorder.After);
        builder.AddAttribute(31, "layout-transition", true);
        if (ShowCheckBoxes && SelectionMode == SelectionMode.Multiple)
        {
            builder.OpenElement(32, "ListViewItemCheck");
            builder.AddAttribute(33, "semantic-hidden", true);
            builder.AddAttribute(34, "checked", selected);
            builder.AddAttribute(35, "glyph", selected ? "check" : null);
            builder.CloseElement();
        }
        builder.OpenElement(40, "ListViewItemContent");
        if (ItemTemplate is null) builder.AddContent(41, ItemText?.Invoke(item) ?? item?.ToString());
        else builder.AddContent(42, ItemTemplate(item));
        builder.CloseElement();
        builder.CloseElement();
    }

    private void MeasureRow(ResizeEventArgs e)
    {
        if (!double.IsFinite(e.Height) || e.Height <= 0 || Math.Abs(measuredItemHeight - e.Height) < .001) return;
        measuredItemHeight = e.Height;
        if (ItemHeight is null) StateHasChanged();
    }

    private void RowFocused(int row)
    {
        if (current == row) return;
        current = row;
        UpdateTabStop();
    }

    private void RootFocused()
    {
        if (focus.Quiet || !RowFocus.HasOwnFocus(Element) || tabStop < 0) return;
        focus.Pending = tabStop;
        virtualizer?.ScrollIntoView(tabStop);
    }

    private async Task RangeChangedAsync(VirtualRange next)
    {
        var lost = Realized(current) && !(current >= next.First && current < next.Last) && RowFocus.HasFocusWithin(Element) && !RowFocus.HasOwnFocus(Element);
        range = next;
        if (lost && focus.Pending < 0) focus.RootQuietly = true;
        await OnRangeChanged.InvokeAsync(next);
    }

    /// <summary>Moves the focus to the current row once it is realized and requests more items near the end.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the work is done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        hover.Refresh();
        ResolveReorderedItem();
        if (virtualizer is not null && virtualizer.Range != range)
        {
            range = virtualizer.Range;
            StateHasChanged();
        }
        else if (virtualizer is null && entries.Count == 0) range = default;
        if (focus.Pending >= entries.Count) focus.Pending = -1;
        await focus.ApplyAsync(Element, "ListViewItem");
        await CheckEndAsync();
    }

    private async Task CheckEndAsync()
    {
        if (!OnEndReached.HasDelegate || IsLoadingMore || endReachedAt == sourceCount) return;
        if (entries.Count > 0 && (virtualizer is not { ViewportSize: > 0 } || range.Last < entries.Count - EndThreshold)) return;
        endReachedAt = sourceCount;
        await OnEndReached.InvokeAsync();
    }

    private void MoveTo(int row)
    {
        current = row;
        UpdateTabStop();
        focus.Pending = row;
        virtualizer?.ScrollIntoView(row);
        StateHasChanged();
    }

    private Task NotifyAsync() =>
        selection.NotifyAsync(entries.Where(e => !e.IsHeader).Select(e => e.Item), SelectedItemChanged, SelectedItemsChanged, Element);

    private async Task ClickAsync(int row, PointerEventArgs e)
    {
        if (reorder.SuppressClick(e)) return;
        if (Disabled || IsSkipped(row)) return;
        current = row;
        var key = KeyOf(entries[row].Item);
        var model = selection.Model;
        var changed = CollectionKeys.Shift(e) && SelectionMode == SelectionMode.Multiple ? model.Extend(key, CollectionKeys.Control(e))
            : CollectionKeys.Control(e) || (ShowCheckBoxes && SelectionMode == SelectionMode.Multiple) ? model.Toggle(key)
            : model.Select(key);
        UpdateTabStop();
        StateHasChanged();
        if (changed) await NotifyAsync();
    }

    private async Task PointerDownAsync(int row, PointerEventArgs e)
    {
        if (e.Button != PointerButton.Secondary || Disabled || IsSkipped(row) || selection.IsSelected(entries[row].Item)) return;
        current = row;
        UpdateTabStop();
        if (selection.Model.Select(KeyOf(entries[row].Item))) await NotifyAsync();
    }

    private Task ActivateAsync(int row) =>
        Disabled || IsSkipped(row) ? Task.CompletedTask : OnItemActivated.InvokeAsync(entries[row].Item);

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || entries.Count == 0) return;
        var at = IsSkipped(current) ? tabStop : current;
        if (await reorder.KeyAsync(e, at, entries.Count)) return;
        var control = CollectionKeys.Control(e);
        var shift = CollectionKeys.Shift(e);
        var multiple = SelectionMode == SelectionMode.Multiple;
        var model = selection.Model;
        if (e.Key == " ")
        {
            e.Handled = true;
            if (at < 0) return;
            var key = KeyOf(entries[at].Item);
            var toggled = shift && multiple ? model.Extend(key, control) : multiple || control ? model.Toggle(key) : model.Select(key);
            MoveTo(at);
            if (toggled) await NotifyAsync();
            return;
        }
        if (e.Key == "Enter")
        {
            e.Handled = true;
            if (at < 0) return;
            var picked = SelectionMode == SelectionMode.Single && model.Select(KeyOf(entries[at].Item));
            MoveTo(at);
            if (picked) await NotifyAsync();
            await ActivateAsync(at);
            return;
        }
        if (e.Key is "a" or "A" && control)
        {
            e.Handled = true;
            if (multiple && model.SelectAll())
            {
                StateHasChanged();
                await NotifyAsync();
            }
            return;
        }
        keys.PageSize = virtualizer?.PageSize() ?? 10;
        var target = keys.Move(CollectionKeys.WithoutControl(e), at, entries.Count, IsSkipped, TextOf);
        if (target is not { } row) return;
        e.Handled = true;
        var next = KeyOf(entries[row].Item);
        var changed = !control && (shift && multiple ? model.Extend(next) : !(ShowCheckBoxes && multiple) && model.Select(next));
        MoveTo(row);
        if (changed) await NotifyAsync();
    }
}
