using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A virtualized hierarchy built on <see cref="TreeModel{TItem}" /> and <see cref="Virtualizer{TItem}" />: the
/// <see cref="Roots" /> and their children, given by <see cref="Children" /> or loaded the first time an item is expanded
/// with <see cref="LoadChildren" />. It is a single Tab stop with the tree keyboard conventions: Up and Down move,
/// Right expands or goes to the first child, Left collapses or goes to the parent, Home, End, Page Up and Page Down jump,
/// typing finds an item, <c>*</c> expands every sibling, Enter (or a double click) raises <see cref="OnItemActivated" />.
/// <see cref="SelectedItem" /> and <see cref="SelectedItems" /> support <c>@bind-</c>; with <see cref="ShowCheckBoxes" />
/// every item has a check box checked in cascade (a parent is checked, unchecked or mixed according to its children) and
/// <see cref="CheckedItems" /> (<c>@bind-CheckedItems</c>) holds the checked leaves. The element <c>TreeView</c> (role
/// <c>tree</c>) holds the scrolling <c>TreeViewViewport</c> and the rows <c>TreeViewItem</c> (role <c>treeitem</c>, with
/// <c>level</c>, <c>expanded</c> and the <c>--nz-tree-level</c> variable for the indent), whose parts are
/// <c>TreeViewIndentGuide</c>, <c>TreeViewExpander</c>, <c>TreeViewItemCheck</c>, <c>TreeViewItemContent</c> and
/// <c>TreeViewLoading</c>; <c>TreeViewEmpty</c> replaces the viewport when there are no roots. The noninteractive
/// <c>TreeViewRowMetric</c> supplies the theme's automatic row height when <see cref="ItemHeight"/> is null.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class TreeView<TItem> : Control where TItem : notnull
{
    private readonly TreeModel<TItem> model = new();
    private readonly ItemSelection<TItem> selection = new();
    private readonly RovingFocusController keys = new() { Orientation = Orientation.Vertical };
    private readonly RowFocus focus = new();
    private readonly ItemReorderController reorder;
    private TItem? reorderedItem;
    private bool hasReorderedItem;
    private double measuredItemHeight = 28;
    private readonly ControllableState<IReadOnlyCollection<TItem>?> checkedItems = new(null);
    private readonly HashSet<TItem> checkAfterLoad = [];
    private readonly Dictionary<TItem, int> indexOf = [];
    private readonly RenderFragment<VirtualItem<TreeRow<TItem>>> rowTemplate;
    private IReadOnlyList<TreeRow<TItem>> rows = [];
    private Virtualizer<TreeRow<TItem>>? virtualizer;
    private VirtualRange range;
    private TItem? currentItem;
    private bool hasCurrent;
    private int current = -1;
    private int tabStop = -1;
    private bool dirty = true;

    /// <summary>Creates a tree view.</summary>
    public TreeView()
    {
        rowTemplate = row => builder => BuildRow(builder, row.Item, row.Index);
        reorder = new(() => Element, "TreeViewItem", index => IsSkipped(index) ? null : rows[index].Item,
            CanMoveRow, ReorderAsync, StateHasChanged) { Destination = ReorderDestination };
        model.Changed += () =>
        {
            dirty = true;
            StateHasChanged();
        };
    }

    /// <summary>Gets or sets the top-level items.</summary>
    [Parameter] public IReadOnlyList<TItem> Roots { get; set; } = [];

    /// <summary>Gets or sets the function that gives an item's children, or null when they are loaded with <see cref="LoadChildren" />.</summary>
    [Parameter] public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }

    /// <summary>Gets or sets the function that tells whether an item has children before they are loaded.</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>Gets or sets the function that loads an item's children the first time it is expanded; a <c>TreeViewLoading</c> part shows meanwhile.</summary>
    [Parameter] public Func<TItem, Task<IReadOnlyList<TItem>>>? LoadChildren { get; set; }

    /// <summary>Gets or sets how each item is rendered; <see cref="ItemText" /> is shown when null.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the text of an item, shown without a template and matched by typing; <c>ToString</c> is used when null.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected.</summary>
    [Parameter] public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>Gets or sets a fixed row height, or an estimate with <see cref="MeasureItems"/>. Null uses the theme's measured TreeViewRowMetric; Basic falls back to 28 logical units.</summary>
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

    /// <summary>Gets or sets the callback invoked with the selected visible items, in display order, when the user changes a multiple selection.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Gets or sets whether every item shows a check box checked in cascade; Space toggles the current one.</summary>
    [Parameter] public bool ShowCheckBoxes { get; set; }

    /// <summary>Gets or sets whether dragging or Ctrl+Shift+Up/Down can request a single-item move within its current siblings. Reparenting is not performed; requires <see cref="OnItemReorder"/>.</summary>
    [Parameter] public bool CanReorderItems { get; set; }

    /// <summary>Gets or sets the callback requesting a move within the item's known siblings. Indices are relative to that sibling collection. The application updates its model and calls <see cref="Refresh"/> for in-place changes.</summary>
    [Parameter] public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }

    /// <summary>Gets or sets the checked items; checking a parent checks its known descendants, and the leaves are reported.</summary>
    [Parameter] public IReadOnlyCollection<TItem>? CheckedItems { get; set; }

    /// <summary>Gets or sets the callback invoked with the checked leaves when the user changes the checks.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> CheckedItemsChanged { get; set; }

    /// <summary>Gets or sets the callback invoked when an item is activated with Enter or a double click.</summary>
    [Parameter] public EventCallback<TItem> OnItemActivated { get; set; }

    /// <summary>Gets or sets whether rows show a <c>TreeViewIndentGuide</c> line for each ancestor level.</summary>
    [Parameter] public bool IndentGuides { get; set; }

    /// <summary>Gets or sets the indent of each level, written as the <c>--nz-tree-indent</c> variable; the theme's indent is used when null.</summary>
    [Parameter] public double? Indent { get; set; }

    /// <summary>Gets or sets the content shown when there are no roots; a localized "No results" when null.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets whether the tree rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the model that holds the expansion, the loaded children and the checks.</summary>
    public TreeModel<TItem> Model => model;

    internal bool AllowEmptySelection { get => selection.Model.AllowEmpty; set => selection.Model.AllowEmpty = value; }

    /// <summary>Gets the visible rows, in display order.</summary>
    public IReadOnlyList<TreeRow<TItem>> VisibleRows
    {
        get
        {
            EnsureRows();
            return rows;
        }
    }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TreeView";

    /// <summary>Gets the indent variable and the caller's declarations.</summary>
    protected override string? ElementStyle => Indent is { } indent ? Css.Join("--nz-tree-indent: " + Css.Px(indent), Style) : Style;

    private double RowHeight => ItemHeight ?? measuredItemHeight;

    /// <summary>Validates the parameters and synchronizes the model, the selection and the checks.</summary>
    protected override void OnParametersSet()
    {
        if (ItemHeight is { } itemHeight && (!double.IsFinite(itemHeight) || itemHeight <= 0)) throw new ArgumentOutOfRangeException(nameof(ItemHeight));
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (Indent is { } indent && (!double.IsFinite(indent) || indent < 0)) throw new ArgumentOutOfRangeException(nameof(Indent));
        if (!ReferenceEquals(model.Roots, Roots) || model.Children != Children || model.HasChildren != HasChildren || model.LoadChildren != LoadChildren)
        {
            model.Roots = Roots ?? [];
            model.Children = Children;
            model.HasChildren = HasChildren;
            model.LoadChildren = LoadChildren;
            dirty = true;
        }
        if (checkedItems.Sync(CheckedItems)) ApplyChecked(CheckedItems ?? []);
        selection.Sync(SelectionMode, SelectedItem, SelectedItems);
        EnsureRows();
        reorder.Enabled = CanReorderItems && !Disabled && OnItemReorder.HasDelegate;
        reorder.Synchronize();
        UpdateTabStop();
    }

    /// <summary>Expands or collapses an item, loading its children the first time when needed.</summary>
    /// <param name="item">The item.</param>
    /// <param name="expanded">Whether the item should be expanded.</param>
    /// <returns>A task that completes when the children are available.</returns>
    public async Task SetExpandedAsync(TItem item, bool expanded)
    {
        var deferCheck = expanded && ShowCheckBoxes && Children is null && LoadChildren is not null && model.ChildrenOf(item).Count == 0 &&
            model.CanExpand(item) && model.CheckState(item) == TreeCheckState.Checked;
        if (deferCheck)
        {
            model.SetChecked(item, false);
            checkAfterLoad.Add(item);
        }
        await model.SetExpandedAsync(item, expanded);
        if (checkAfterLoad.Remove(item))
        {
            model.SetChecked(item, true);
            await NotifyCheckedAsync();
        }
    }

    /// <summary>Expands every item whose children are known, recursively; children loaded on demand are not loaded.</summary>
    public void ExpandAll()
    {
        var pending = new Stack<TItem>(model.Roots);
        var seen = new HashSet<TItem>();
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            var children = model.ChildrenOf(item);
            if (children.Count > 0) _ = model.SetExpandedAsync(item, true);
            foreach (var child in children) pending.Push(child);
        }
    }

    /// <summary>Collapses every item.</summary>
    public void CollapseAll() => model.CollapseAll();

    /// <summary>Reads the known hierarchy again after the application's sibling collections change in place, preserving current/expanded items by identity.</summary>
    public void Refresh()
    {
        dirty = true;
        EnsureRows();
        reorder.Synchronize();
        StateHasChanged();
    }

    /// <summary>Expands the ancestors of an item, makes it the current row and scrolls it into view.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the item is among the known items.</returns>
    public bool Reveal(TItem item)
    {
        if (!model.Reveal(item)) return false;
        EnsureRows();
        if (!indexOf.TryGetValue(item, out var row)) return false;
        SetCurrent(row);
        UpdateTabStop();
        virtualizer?.ScrollIntoView(row);
        StateHasChanged();
        return true;
    }

    private void ApplyChecked(IEnumerable<TItem> items)
    {
        var wanted = new HashSet<TItem>(items);
        checkAfterLoad.RemoveWhere(item => !wanted.Contains(item));
        foreach (var leaf in model.CheckedLeaves().ToArray())
            if (!wanted.Contains(leaf)) model.SetChecked(leaf, false);
        foreach (var item in wanted)
            if (model.IsLoading(item)) checkAfterLoad.Add(item);
            else model.SetChecked(item, true);
    }

    private void EnsureRows()
    {
        if (!dirty) return;
        dirty = false;
        rows = model.VisibleRows();
        indexOf.Clear();
        for (var i = 0; i < rows.Count; i++) indexOf.TryAdd(rows[i].Item, i);
        selection.Model.SetOrder(rows.Select(row => (object)row.Item));
        if (!hasCurrent) current = -1;
        else if (indexOf.TryGetValue(currentItem!, out var index)) current = index;
        else
        {
            current = -1;
            foreach (var ancestor in (model.PathTo(currentItem!) ?? []).Reverse())
                if (indexOf.TryGetValue(ancestor, out var visible)) { current = visible; break; }
            if (current >= 0) currentItem = rows[current].Item;
            else hasCurrent = false;
        }
        UpdateTabStop();
    }

    private void SetCurrent(int row)
    {
        current = row;
        hasCurrent = row >= 0;
        currentItem = row >= 0 ? rows[row].Item : default;
    }

    private bool IsSkipped(int row) => row < 0 || row >= rows.Count || ItemDisabled?.Invoke(rows[row].Item) == true;

    private bool CanMoveRow(int from, int to) => !IsSkipped(from) && !IsSkipped(to) && rows[from].Level == rows[to].Level &&
        (rows[from].Level == 1 || EqualityComparer<TItem>.Default.Equals(rows[from].Parent!, rows[to].Parent!));

    private int ReorderDestination(int from, int target, bool after)
    {
        var source = rows[from];
        var oldIndex = source.PositionInSet - 1;
        var next = rows[target].PositionInSet - 1 + (after ? 1 : 0);
        if (next > oldIndex) next--;
        var siblings = source.Level == 1 ? model.Roots : model.ChildrenOf(source.Parent!);
        return next >= 0 && next < siblings.Count && indexOf.TryGetValue(siblings[next], out var row) ? row : from;
    }

    private async Task ReorderAsync(int from, int to)
    {
        if (!reorder.Enabled || !CanMoveRow(from, to) || from == to) return;
        var source = rows[from];
        SetCurrent(from);
        await OnItemReorder.InvokeAsync(new(source.Item, source.PositionInSet - 1, rows[to].PositionInSet - 1));
        reorderedItem = source.Item;
        hasReorderedItem = true;
        Refresh();
    }

    private string? TextOf(int row) => ItemText?.Invoke(rows[row].Item) ?? rows[row].Item.ToString();

    private TreeCheckState CheckStateOf(TItem item) => checkAfterLoad.Contains(item) ? TreeCheckState.Checked : model.CheckState(item);

    private void UpdateTabStop()
    {
        if (!IsSkipped(current)) { tabStop = current; return; }
        tabStop = -1;
        if (selection.Model.Count > 0)
            for (var i = 0; i < rows.Count && tabStop < 0; i++)
                if (!IsSkipped(i) && selection.IsSelected(rows[i].Item)) tabStop = i;
        for (var i = 0; i < rows.Count && tabStop < 0; i++)
            if (!IsSkipped(i)) tabStop = i;
    }

    private bool Realized(int row) => row >= range.First && row < range.Last;

    /// <summary>Adds the tree role, the selection mode and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        EnsureRows();
        reorder.Synchronize();
        var rootStop = !Disabled && tabStop >= 0 && !Realized(tabStop);
        builder.AddAttribute(10, "role", "tree");
        builder.AddAttribute(11, "multiselectable", SelectionMode == SelectionMode.Multiple);
        builder.AddAttribute(12, "disabled", Disabled);
        builder.AddAttribute(13, "focusable", rootStop);
        if (TabIndex is null && rootStop) builder.AddAttribute(14, "tabindex", 0);
        builder.AddAttribute(15, "checkable", ShowCheckBoxes);
        builder.AddAttribute(16, "indent-guides", IndentGuides);
        builder.AddAttribute(17, "empty", rows.Count == 0);
        builder.AddAttribute(18, "__internal_onkeydown#treeview", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(19, "__internal_onfocus#treeview", EventCallback.Factory.Create<FocusEventArgs>(this, RootFocused));
        builder.AddAttribute(20, "reorderable", reorder.Enabled);
        builder.AddAttribute(21, "dragging", reorder.IsDragging);
        builder.AddAttribute(22, "__internal_onpointermove#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Move));
        builder.AddAttribute(23, "__internal_onpointerup#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.UpAsync));
        builder.AddAttribute(24, "__internal_onlostpointercapture#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Lost));
    }

    /// <summary>Adds the viewport with the realized rows, or the empty content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(500, "TreeViewRowMetric");
        builder.AddAttribute(501, "semantic-hidden", true);
        builder.AddAttribute(502, "__internal_onresize#metric", EventCallback.Factory.Create<ResizeEventArgs>(this, MeasureRow));
        builder.CloseElement();
        if (rows.Count == 0)
        {
            builder.OpenElement(1000, "TreeViewEmpty");
            builder.AddAttribute(1001, "role", "text");
            if (EmptyContent is null) builder.AddContent(1002, ControlStrings.Get("NoResults"));
            else builder.AddContent(1003, EmptyContent);
            builder.CloseElement();
            return;
        }
        builder.OpenComponent<Virtualizer<TreeRow<TItem>>>(1010);
        builder.AddComponentParameter(1011, nameof(Virtualizer<TreeRow<TItem>>.As), "TreeViewViewport");
        builder.AddComponentParameter(1012, nameof(Virtualizer<TreeRow<TItem>>.Items), rows);
        builder.AddComponentParameter(1013, nameof(Virtualizer<TreeRow<TItem>>.ItemSize), RowHeight);
        builder.AddComponentParameter(1014, nameof(Virtualizer<TreeRow<TItem>>.MeasureItems), MeasureItems);
        builder.AddComponentParameter(1015, nameof(Virtualizer<TreeRow<TItem>>.ItemTemplate), rowTemplate);
        builder.AddComponentParameter(1016, nameof(Virtualizer<TreeRow<TItem>>.ItemKey), (Func<TreeRow<TItem>, object>)(row => row.Item));
        builder.AddComponentParameter(1017, nameof(Virtualizer<TreeRow<TItem>>.OnRangeChanged), EventCallback.Factory.Create<VirtualRange>(this, RangeChanged));
        builder.AddComponentReferenceCapture(1018, component => virtualizer = (Virtualizer<TreeRow<TItem>>)component);
        builder.CloseComponent();
    }

    private void BuildRow(RenderTreeBuilder builder, TreeRow<TItem> row, int index)
    {
        var item = row.Item;
        var disabled = ItemDisabled?.Invoke(item) == true;
        var loading = model.IsLoading(item);
        var state = ShowCheckBoxes ? CheckStateOf(item) : TreeCheckState.Unchecked;
        builder.OpenElement(0, "TreeViewItem");
        builder.AddAttribute(1, "role", "treeitem");
        builder.AddAttribute(2, "index", index);
        builder.AddAttribute(3, "level", row.Level);
        builder.AddAttribute(4, "position-in-set", row.PositionInSet);
        builder.AddAttribute(5, "size-of-set", row.SizeOfSet);
        if (row.HasChildren) builder.AddAttribute(6, "expanded", row.IsExpanded);
        builder.AddAttribute(7, "selected", selection.IsSelected(item));
        builder.AddAttribute(8, "current", index == current);
        builder.AddAttribute(9, "disabled", disabled);
        builder.AddAttribute(10, "focusable", !disabled && !Disabled);
        builder.AddAttribute(11, "tabindex", index == tabStop ? 0 : -1);
        builder.AddAttribute(12, "busy", loading);
        if (ShowCheckBoxes)
        {
            builder.AddAttribute(13, "checked", state == TreeCheckState.Checked);
            builder.AddAttribute(14, "indeterminate", state == TreeCheckState.Mixed);
        }
        if (ItemTemplate is not null && ItemText is not null) builder.AddAttribute(15, "accessible-name", ItemText(item));
        builder.AddAttribute(16, "style", "--nz-tree-level: " + (row.Level - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AddAttribute(17, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => ClickAsync(index, e)));
        builder.AddAttribute(18, "__internal_ondblclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => reorder.SuppressPointerActivation ? Task.CompletedTask : DoubleClickAsync(index)));
        builder.AddAttribute(19, "__internal_onfocus#treeview", EventCallback.Factory.Create<FocusEventArgs>(this, () => RowFocused(index)));
        builder.AddAttribute(20, "item-position", new LogicalItemPosition((this, row.Level == 1 ? null : (object?)row.Parent), row.PositionInSet, row.SizeOfSet));
        builder.AddAttribute(21, "__internal_onpointerdown#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, e => reorder.Down(index, e)));
        builder.AddAttribute(22, "dragging", index == reorder.Source);
        builder.AddAttribute(23, "drop-before", index == reorder.Target && !reorder.After);
        builder.AddAttribute(24, "drop-after", index == reorder.Target && reorder.After);
        builder.AddAttribute(25, "layout-transition", true);
        if (IndentGuides)
            for (var level = 1; level < row.Level; level++)
            {
                builder.OpenElement(30, "TreeViewIndentGuide");
                builder.SetKey(level);
                builder.AddAttribute(31, "semantic-hidden", true);
                builder.AddAttribute(32, "style", "--nz-tree-guide: " + (level - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.CloseElement();
            }
        builder.OpenElement(40, "TreeViewExpander");
        builder.AddAttribute(41, "semantic-hidden", true);
        builder.AddAttribute(42, "expander", row.HasChildren);
        builder.AddAttribute(43, "expanded", row.HasChildren && row.IsExpanded);
        builder.AddAttribute(44, "glyph", row.HasChildren ? "chevron-right" : null);
        if (row.HasChildren) builder.AddAttribute(45, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ToggleAsync(index)));
        builder.CloseElement();
        if (ShowCheckBoxes)
        {
            builder.OpenElement(50, "TreeViewItemCheck");
            builder.AddAttribute(51, "semantic-hidden", true);
            builder.AddAttribute(52, "checked", state == TreeCheckState.Checked);
            builder.AddAttribute(53, "indeterminate", state == TreeCheckState.Mixed);
            builder.AddAttribute(54, "glyph", state switch { TreeCheckState.Checked => "check", TreeCheckState.Mixed => "minus", _ => null });
            builder.AddAttribute(55, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ToggleCheckAsync(index)));
            builder.CloseElement();
        }
        builder.OpenElement(60, "TreeViewItemContent");
        if (ItemTemplate is null) builder.AddContent(61, ItemText?.Invoke(item) ?? item.ToString());
        else builder.AddContent(62, ItemTemplate(item));
        builder.CloseElement();
        if (loading)
        {
            builder.OpenElement(70, "TreeViewLoading");
            builder.AddAttribute(71, "role", "status");
            builder.AddContent(72, ControlStrings.Get("Loading"));
            builder.CloseElement();
        }
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
        EnsureRows();
        if (current == row || row >= rows.Count) return;
        SetCurrent(row);
        UpdateTabStop();
    }

    private void RootFocused()
    {
        if (focus.Quiet || !RowFocus.HasOwnFocus(Element) || tabStop < 0) return;
        focus.Pending = tabStop;
        virtualizer?.ScrollIntoView(tabStop);
    }

    private void RangeChanged(VirtualRange next)
    {
        var lost = Realized(current) && !(current >= next.First && current < next.Last) && RowFocus.HasFocusWithin(Element) && !RowFocus.HasOwnFocus(Element);
        range = next;
        if (lost && focus.Pending < 0) focus.RootQuietly = true;
    }

    /// <summary>Moves the focus to the current row once it is realized.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the work is done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (hasReorderedItem)
        {
            hasReorderedItem = false;
            if (indexOf.TryGetValue(reorderedItem!, out var row))
            {
                SetCurrent(row);
                UpdateTabStop();
                if (RowFocus.HasFocusWithin(Element))
                {
                    virtualizer?.ScrollIntoView(row);
                    focus.Pending = row;
                }
                StateHasChanged();
            }
        }
        if (virtualizer is not null && virtualizer.Range != range)
        {
            range = virtualizer.Range;
            StateHasChanged();
        }
        if (focus.Pending >= rows.Count) focus.Pending = -1;
        await focus.ApplyAsync(Element, "TreeViewItem");
    }

    private void MoveTo(int row)
    {
        SetCurrent(row);
        UpdateTabStop();
        focus.Pending = row;
        virtualizer?.ScrollIntoView(row);
        StateHasChanged();
    }

    private Task NotifyAsync() =>
        selection.NotifyAsync(rows.Select(row => row.Item), SelectedItemChanged, SelectedItemsChanged, Element);

    private async Task NotifyCheckedAsync()
    {
        IReadOnlyCollection<TItem> list = [.. model.CheckedLeaves(), .. checkAfterLoad];
        checkedItems.Set(list);
        await CheckedItemsChanged.InvokeAsync(list);
        await Events.RaiseChangeAsync(Element, list);
    }

    private async Task ClickAsync(int row, PointerEventArgs e)
    {
        if (reorder.SuppressClick(e)) return;
        EnsureRows();
        if (Disabled || IsSkipped(row)) return;
        SetCurrent(row);
        var item = (object)rows[row].Item;
        var model = selection.Model;
        var changed = CollectionKeys.Shift(e) && SelectionMode == SelectionMode.Multiple ? model.Extend(item, CollectionKeys.Control(e))
            : CollectionKeys.Control(e) ? model.Toggle(item) : model.Select(item);
        UpdateTabStop();
        StateHasChanged();
        if (changed) await NotifyAsync();
    }

    private async Task DoubleClickAsync(int row)
    {
        EnsureRows();
        if (Disabled || IsSkipped(row)) return;
        var item = rows[row].Item;
        if (rows[row].HasChildren) await SetExpandedAsync(item, !rows[row].IsExpanded);
        await OnItemActivated.InvokeAsync(item);
    }

    private async Task ToggleAsync(int row)
    {
        EnsureRows();
        if (Disabled || row >= rows.Count || !rows[row].HasChildren) return;
        await SetExpandedAsync(rows[row].Item, !rows[row].IsExpanded);
    }

    private async Task ToggleCheckAsync(int row)
    {
        EnsureRows();
        if (Disabled || IsSkipped(row)) return;
        var item = rows[row].Item;
        var value = CheckStateOf(item) != TreeCheckState.Checked;
        if (model.IsLoading(item))
        {
            if (value) checkAfterLoad.Add(item);
            else checkAfterLoad.Remove(item);
            StateHasChanged();
        }
        else model.SetChecked(item, value);
        await NotifyCheckedAsync();
    }

    private async Task NavigateAsync(int row, bool control, bool shift)
    {
        var item = (object)rows[row].Item;
        var model = selection.Model;
        var changed = !control && (shift && SelectionMode == SelectionMode.Multiple ? model.Extend(item) : model.Select(item));
        MoveTo(row);
        if (changed) await NotifyAsync();
    }

    private int ParentRow(int row)
    {
        for (var i = row - 1; i >= 0; i--)
            if (rows[i].Level < rows[row].Level) return i;
        return -1;
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        EnsureRows();
        if (e.Handled || Disabled || rows.Count == 0) return;
        var at = IsSkipped(current) ? tabStop : current;
        if (at < 0) return;
        if (reorder.CancelFromKey(e)) return;
        if (reorder.Enabled && (e.Modifiers == (KeyModifiers.Control | KeyModifiers.Shift) || e.Modifiers == (KeyModifiers.Meta | KeyModifiers.Shift)) &&
            (e.Key == "ArrowUp" || e.Key == "ArrowDown"))
        {
            var direction = e.Key == "ArrowUp" ? -1 : 1;
            var next = at + direction;
            while (next >= 0 && next < rows.Count && rows[next].Level > rows[at].Level) next += direction;
            await reorder.KeyAsync(e, at, rows.Count, next >= 0 && next < rows.Count ? Math.Abs(next - at) : rows.Count);
            return;
        }
        var control = CollectionKeys.Control(e);
        var shift = CollectionKeys.Shift(e);
        var row = rows[at];
        var model = selection.Model;
        switch (e.Key)
        {
            case "ArrowRight":
                e.Handled = true;
                if (row.HasChildren && !row.IsExpanded)
                {
                    MoveTo(at);
                    await SetExpandedAsync(row.Item, true);
                }
                else if (row.IsExpanded && at + 1 < rows.Count && rows[at + 1].Level > row.Level && !IsSkipped(at + 1)) await NavigateAsync(at + 1, control, shift);
                return;
            case "ArrowLeft":
                e.Handled = true;
                if (row.HasChildren && row.IsExpanded)
                {
                    MoveTo(at);
                    await SetExpandedAsync(row.Item, false);
                }
                else if (ParentRow(at) is var parent and >= 0 && !IsSkipped(parent)) await NavigateAsync(parent, control, shift);
                return;
            case "Enter":
                e.Handled = true;
                var picked = SelectionMode == SelectionMode.Single && model.Select(row.Item);
                MoveTo(at);
                if (picked) await NotifyAsync();
                if (OnItemActivated.HasDelegate) await OnItemActivated.InvokeAsync(row.Item);
                else if (row.HasChildren) await SetExpandedAsync(row.Item, !row.IsExpanded);
                return;
            case " ":
                e.Handled = true;
                MoveTo(at);
                if (ShowCheckBoxes)
                {
                    await ToggleCheckAsync(at);
                    return;
                }
                var multiple = SelectionMode == SelectionMode.Multiple;
                var toggled = shift && multiple ? model.Extend(row.Item, control) : multiple || control ? model.Toggle(row.Item) : model.Select(row.Item);
                if (toggled) await NotifyAsync();
                return;
            case "*":
                e.Handled = true;
                var siblings = row.Parent is { } owner ? this.model.ChildrenOf(owner) : this.model.Roots;
                MoveTo(at);
                foreach (var sibling in siblings.ToArray())
                    if (this.model.CanExpand(sibling) && !this.model.IsExpanded(sibling)) await SetExpandedAsync(sibling, true);
                return;
            case "a" or "A" when control:
                e.Handled = true;
                if (SelectionMode == SelectionMode.Multiple && model.SelectAll())
                {
                    StateHasChanged();
                    await NotifyAsync();
                }
                return;
        }
        keys.PageSize = virtualizer?.PageSize() ?? 10;
        var target = keys.Move(CollectionKeys.WithoutControl(e), at, rows.Count, IsSkipped, TextOf);
        if (target is not { } index) return;
        e.Handled = true;
        await NavigateAsync(index, control, shift);
    }
}
