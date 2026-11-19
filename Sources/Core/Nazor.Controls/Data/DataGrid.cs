using System.ComponentModel;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A table of items with columns declared as <see cref="DataGridColumn{TItem}" /> children: rows are virtualized, so tens
/// of thousands of items stay fast; columns sort (Shift adds a sort), resize, reorder by dragging their headers, freeze at
/// the start and edit their cells; rows are selected (<see cref="SelectionMode" />, optionally with a column of check
/// boxes), grouped through the <see cref="DataView{TItem}" /> with headers that open and close, and can show details
/// below. The grid is a single Tab stop: the arrows, Home, End (with Ctrl for the first and last row), Page Up and Page
/// Down move between cells and the header; Enter or F2 edits, Space selects the row, Ctrl+A selects every row. On a
/// header Enter or Space sorts, Alt with the arrows moves the column and Ctrl with the arrows resizes it. The parts are
/// <c>DataGridHeader</c>, <c>DataGridHeaderRow</c>, <c>DataGridColumnHeader</c> (<c>sort</c>, <c>sortable</c>,
/// <c>frozen</c>, <c>dragging</c>, <c>drop-target</c>) with <c>DataGridColumnHeaderContent</c>, <c>DataGridSortMark</c>
/// and a <c>ResizeHandle</c>, <c>DataGridSelectionHeader</c>, <c>DataGridDetailsHeader</c>, <c>DataGridBody</c> (the
/// virtualized viewport, with <c>DataGridBodyItem</c>), <c>DataGridRow</c> (<c>selected</c>, <c>alternate</c>,
/// <c>expanded</c>), <c>DataGridCell</c> (<c>current</c>, <c>editable</c>, <c>editing</c>, <c>frozen</c>,
/// <c>frozen-last</c>, <c>align</c>), <c>DataGridSelectionCell</c>, <c>DataGridDetailsCell</c> with
/// <c>DataGridDetailsToggle</c>, <c>DataGridCellEditor</c>, <c>DataGridRowDetails</c>, <c>DataGridGroupHeader</c> with
/// <c>DataGridGroupHeaderCell</c>, <c>DataGridGroupToggle</c>, <c>DataGridGroupKey</c> and <c>DataGridGroupCount</c>,
/// <c>DataGridFooter</c>, <c>DataGridFooterRow</c>, <c>DataGridFooterCell</c>, <c>DataGridEmpty</c> and
/// <c>DataGridLoading</c>. The noninteractive <c>DataGridRowMetric</c> supplies the theme's automatic row pitch
/// when <see cref="RowHeight"/> is null.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
[CascadingTypeParameter(nameof(TItem))]
public sealed class DataGrid<TItem> : Control, IDisposable
{
    private const double SelectionWidth = 32;
    private const double DetailsWidth = 28;
    private readonly List<DataGridColumn<TItem>> columns = [];
    private readonly Dictionary<string, double> userWidths = [];
    private readonly HashSet<object> expandedDetails = [];
    private readonly SelectionModel<object> selection = new(SelectionMode.None);
    private readonly ControllableState<IReadOnlyList<DataSort>> sorts = new([]);
    private readonly ControllableState<int> page = new(0);
    private readonly DataView<TItem> ownView = new();
    private DataView<TItem> view;
    private List<string>? order;
    private Virtualizer<DataRow<TItem>>? body;
    private readonly HoverTracker hover;
    private EditableText? editorComponent;
    private double viewportWidth, scrollLeft;
    private double measuredRowHeight = 32;
    private int currentRow = -2, currentColumn;
    private bool pendingFocus, pendingEditorFocus, syncing, started, justDragged;
    private Edit? editing;
    private Drag? drag;
    private int keyedVersion = -1, selectionVersion, countedVersion = -1, countedSelection = -1, selectedVisible;
    private Dictionary<object, TItem> byKey = [];
    private object? lastSelectedItem;
    private IReadOnlyList<TItem>? lastSelectedItems;

    /// <summary>Creates a grid.</summary>
    public DataGrid()
    {
        hover = new(StateHasChanged);
        view = ownView;
        view.Changed += ViewChanged;
        selection.Changed += () => selectionVersion++;
    }

    /// <summary>Gets or sets the items shown, processed in memory.</summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Gets or sets the asynchronous source of the items for server-side data, used instead of <see cref="Items" />.</summary>
    [Parameter] public Func<DataRequest, Task<DataPage<TItem>>>? ItemsProvider { get; set; }

    /// <summary>
    /// Gets or sets a view shared with other components, such as a pager; when null the grid uses its own. The grid
    /// writes its items, filter, grouping, sorts and paging parameters into the view.
    /// </summary>
    [Parameter] public DataView<TItem>? View { get; set; }

    /// <summary>Gets or sets the application-defined filter sent to <see cref="ItemsProvider" />, such as a search text.</summary>
    [Parameter] public object? Query { get; set; }

    /// <summary>Gets or sets the predicate an item must satisfy to be shown.</summary>
    [Parameter] public Func<TItem, bool>? Filter { get; set; }

    /// <summary>Gets or sets the function that gives the group of an item; groups are shown under headers that open and close.</summary>
    [Parameter] public Func<TItem, object?>? GroupBy { get; set; }

    /// <summary>Gets or sets the content of a group header, after the toggle; the key and the count by default.</summary>
    [Parameter] public RenderFragment<DataGroup<TItem>>? GroupHeaderTemplate { get; set; }

    /// <summary>Gets or sets the sorts, the most significant first, by column key.</summary>
    [Parameter] public IReadOnlyList<DataSort>? Sorts { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the sorts; supports <c>@bind-Sorts</c>.</summary>
    [Parameter] public EventCallback<IReadOnlyList<DataSort>> SortsChanged { get; set; }

    /// <summary>Gets or sets the number of rows per page, or zero to show every row.</summary>
    [Parameter] public int PageSize { get; set; }

    /// <summary>Gets or sets the zero-based page shown.</summary>
    [Parameter] public int Page { get; set; }

    /// <summary>Gets or sets the callback invoked when the page changes; supports <c>@bind-Page</c>.</summary>
    [Parameter] public EventCallback<int> PageChanged { get; set; }

    /// <summary>Gets or sets the column declarations.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the fixed row pitch in logical units, including any theme gap. Null measures DataGridRowMetric; Basic falls back to 32.</summary>
    [Parameter] public double? RowHeight { get; set; }

    /// <summary>Gets or sets how many rows can be selected.</summary>
    [Parameter] public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>Gets or sets the selected item in single selection, or the first selected one.</summary>
    [Parameter] public TItem? SelectedItem { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the selection; supports <c>@bind-SelectedItem</c>.</summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Gets or sets the selected items in multiple selection.</summary>
    [Parameter] public IReadOnlyList<TItem>? SelectedItems { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the selection; supports <c>@bind-SelectedItems</c>.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>Gets or sets whether a first column of check boxes selects rows, with a header box that selects every row.</summary>
    [Parameter] public bool ShowSelectionColumn { get; set; }

    /// <summary>Gets or sets whether moving with the arrows also selects the row, in single selection.</summary>
    [Parameter] public bool SelectionFollowsFocus { get; set; }

    /// <summary>Gets or sets the function that gives each item a stable identity for selection and details; the item itself when null.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets the content shown below a row when its details are open, with a toggle column at the start.</summary>
    [Parameter] public RenderFragment<TItem>? RowDetailsTemplate { get; set; }

    /// <summary>Gets or sets the content shown when no row passes the filter; a localized "no results" text by default.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets whether the grid shows that data is loading, in addition to a running <see cref="ItemsProvider" /> request.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Gets or sets whether the user can reorder columns by dragging their headers or with Alt and the arrows.</summary>
    [Parameter] public bool ReorderableColumns { get; set; } = true;

    /// <summary>Gets or sets the lines drawn between cells.</summary>
    [Parameter] public DataGridLines GridLines { get; set; } = DataGridLines.Both;

    /// <summary>Gets or sets whether every other row is marked <c>alternate</c> for a striped look.</summary>
    [Parameter] public bool Striped { get; set; }

    /// <summary>Gets or sets the size step, for density.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the callback invoked with a committed cell edit.</summary>
    [Parameter] public EventCallback<DataGridCellEdit<TItem>> OnCellEdited { get; set; }

    /// <summary>Gets or sets the callback invoked when a row is activated by a double click or Enter on a cell that cannot be edited.</summary>
    [Parameter] public EventCallback<TItem> OnRowActivated { get; set; }

    /// <summary>Gets the view that computes the rows.</summary>
    public DataView<TItem> DataView => view;

    /// <summary>Gets the selected items, in display order.</summary>
    public IReadOnlyList<TItem> Selection => SelectedInOrder();

    /// <summary>Gets the keys of the visible columns, in display order.</summary>
    public IReadOnlyList<string> ColumnOrder => [.. VisibleColumns().Select(c => c.ColumnKey)];

    /// <summary>Gets whether a cell is being edited.</summary>
    public bool IsEditing => editing is not null;

    /// <summary>Gets the NSS element name emitted by DataGrid.</summary>
    protected override string ElementName => "DataGrid";

    /// <summary>Gets the resolved row pitch and the caller's style declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-data-row-height: " + Css.Px(EffectiveRowHeight), Style);

    private double EffectiveRowHeight => RowHeight ?? measuredRowHeight;

    private string RowStyle(double width) => "width: " + Css.Px(width) + "; height: max(0px, calc(" + Css.Px(EffectiveRowHeight) + " - var(--nz-data-row-gap, 0px)))";

    private bool HasDetails => RowDetailsTemplate is not null;

    private bool HasSelectionColumn => ShowSelectionColumn && SelectionMode != SelectionMode.None;

    private int Leading => (HasSelectionColumn ? 1 : 0) + (HasDetails ? 1 : 0);

    /// <summary>Validates the parameters and writes them into the view.</summary>
    protected override void OnParametersSet()
    {
        if (RowHeight is { } rowHeight && (!double.IsFinite(rowHeight) || rowHeight <= 0)) throw new ArgumentOutOfRangeException(nameof(RowHeight));
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (!Enum.IsDefined(GridLines)) throw new ArgumentOutOfRangeException(nameof(GridLines));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (PageSize < 0) throw new ArgumentOutOfRangeException(nameof(PageSize));
        if (Page < 0) throw new ArgumentOutOfRangeException(nameof(Page));
        var next = View ?? ownView;
        if (!ReferenceEquals(next, view))
        {
            view.Changed -= ViewChanged;
            view = next;
            view.Changed += ViewChanged;
        }
        sorts.Sync(Sorts ?? []);
        page.Sync(Page);
        selection.Mode = SelectionMode;
        syncing = true;
        try
        {
            view.Source = Items;
            view.Provider = ItemsProvider;
            view.Query = Query;
            view.Filter = Filter;
            view.GroupKey = GroupBy;
            view.PageSize = PageSize;
            view.Page = page.Value;
            ApplySorts();
        }
        finally { syncing = false; }
        SyncSelection();
        started = true;
    }

    /// <summary>Loads the data of a provider when the request changed.</summary>
    /// <returns>A task that completes when the data is available.</returns>
    protected override Task OnParametersSetAsync() => view.NeedsLoad ? view.LoadAsync() : Task.CompletedTask;

    private void ApplySorts()
    {
        var descriptors = new List<DataSortDescriptor<TItem>>();
        foreach (var sort in sorts.Value)
        {
            var column = columns.FirstOrDefault(c => c.ColumnKey == sort.Key);
            if (column?.SortKey is { } key) descriptors.Add(new DataSortDescriptor<TItem>(sort.Key, key, sort.Direction) { Comparer = column.Comparer });
            else if (ItemsProvider is not null) descriptors.Add(new DataSortDescriptor<TItem>(sort.Key, _ => null, sort.Direction));
        }
        view.Sorts = descriptors;
    }

    private void SyncSelection()
    {
        EnsureKeys();
        if (SelectionMode == SelectionMode.Multiple)
        {
            if (!started || !ReferenceEquals(SelectedItems, lastSelectedItems)) selection.Replace((SelectedItems ?? []).Select(KeyOf));
            lastSelectedItems = SelectedItems;
        }
        else
        {
            if (!started || !Equals(SelectedItem, lastSelectedItem)) selection.Replace(SelectedItem is { } item ? [KeyOf(item)] : []);
            lastSelectedItem = SelectedItem;
        }
    }

    private void ViewChanged()
    {
        if (syncing) return;
        if (view.Page != page.Value && page.Set(view.Page)) _ = InvokeAsync(() => PageChanged.InvokeAsync(view.Page));
        _ = InvokeAsync(StateHasChanged);
    }

    private object KeyOf(TItem item) => ItemKey is { } key ? key(item) : item ?? throw new InvalidOperationException("DataGrid items must not be null.");

    private void EnsureKeys()
    {
        var all = view.FilteredItems;
        if (keyedVersion == view.Version) return;
        keyedVersion = view.Version;
        var keys = new List<object>(all.Count);
        var map = new Dictionary<object, TItem>(all.Count);
        foreach (var item in all)
        {
            var key = KeyOf(item);
            keys.Add(key);
            map.TryAdd(key, item);
        }
        byKey = map;
        selection.SetOrder(keys);
    }

    private IReadOnlyList<TItem> SelectedInOrder()
    {
        EnsureKeys();
        var result = new List<TItem>(selection.Count);
        foreach (var key in selection.SelectedKeys)
            if (byKey.TryGetValue(key, out var item)) result.Add(item);
        return result;
    }

    private int SelectedVisibleCount()
    {
        EnsureKeys();
        if (countedVersion == view.Version && countedSelection == selectionVersion) return selectedVisible;
        (countedVersion, countedSelection) = (view.Version, selectionVersion);
        var count = 0;
        foreach (var key in byKey.Keys) if (selection.IsSelected(key)) count++;
        return selectedVisible = count;
    }

    internal void Register(DataGridColumn<TItem> column)
    {
        columns.Add(column);
        column.ColumnKey = column.Key ?? column.Header ?? "column" + columns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ApplySorts();
        StateHasChanged();
    }

    internal void Unregister(DataGridColumn<TItem> column)
    {
        if (!columns.Remove(column)) return;
        ApplySorts();
        StateHasChanged();
    }

    internal void ColumnChanged()
    {
        foreach (var column in columns) column.ColumnKey = column.Key ?? column.Header ?? column.ColumnKey;
        ApplySorts();
        StateHasChanged();
    }

    private List<DataGridColumn<TItem>> VisibleColumns()
    {
        var visible = columns.Where(c => !c.Hidden).ToList();
        if (order is not null)
        {
            var rank = order.Select((key, index) => (key, index)).ToDictionary(p => p.key, p => p.index);
            visible = [.. visible.OrderBy(c => rank.TryGetValue(c.ColumnKey, out var r) ? r : int.MaxValue).ThenBy(c => columns.IndexOf(c))];
        }
        return [.. visible.Where(c => c.Frozen), .. visible.Where(c => !c.Frozen)];
    }

    private enum Slot { Selection, Details, Data }

    private sealed record Cell(Slot Slot, DataGridColumn<TItem>? Column, double Left, double Width, bool Frozen, bool FrozenLast);

    private sealed record Edit(object Key, int Row, string Column, DataGridEditContext<TItem> Context)
    {
        public string Text { get; set; } = "";
    }

    private sealed record Drag(string Key, int Target);

    private List<Cell> Layout()
    {
        var data = VisibleColumns();
        var bases = data.Select(c => userWidths.TryGetValue(c.ColumnKey, out var w) ? w : c.Width ?? (c.Flex > 0 ? c.MinWidth : Math.Max(c.MinWidth, Math.Min(120, c.MaxWidth))))
            .Select((w, i) => Math.Clamp(w, data[i].MinWidth, data[i].MaxWidth)).ToArray();
        var leading = (HasSelectionColumn ? SelectionWidth : 0) + (HasDetails ? DetailsWidth : 0);
        var remaining = viewportWidth - leading - bases.Sum();
        var flexible = Enumerable.Range(0, data.Count).Where(i => data[i].Flex > 0 && data[i].Width is null && !userWidths.ContainsKey(data[i].ColumnKey)).ToList();
        for (var pass = 0; pass < 3 && remaining > 0.5 && flexible.Count > 0; pass++)
        {
            var shares = flexible.Sum(i => data[i].Flex);
            var given = 0.0;
            foreach (var i in flexible.ToArray())
            {
                var extra = remaining * data[i].Flex / shares;
                var grown = Math.Min(data[i].MaxWidth, bases[i] + extra);
                given += grown - bases[i];
                bases[i] = grown;
                if (grown >= data[i].MaxWidth) flexible.Remove(i);
            }
            remaining -= given;
        }
        var cells = new List<Cell>();
        var left = 0.0;
        var frozenData = data.Count(c => c.Frozen);
        if (HasSelectionColumn)
        {
            cells.Add(new Cell(Slot.Selection, null, left, SelectionWidth, true, false));
            left += SelectionWidth;
        }
        if (HasDetails)
        {
            cells.Add(new Cell(Slot.Details, null, left, DetailsWidth, true, false));
            left += DetailsWidth;
        }
        for (var i = 0; i < data.Count; i++)
        {
            cells.Add(new Cell(Slot.Data, data[i], left, Math.Floor(bases[i]), data[i].Frozen, false));
            left += Math.Floor(bases[i]);
        }
        var lastFrozen = cells.FindLastIndex(c => c.Frozen);
        if (lastFrozen >= 0 && (frozenData > 0 || lastFrozen < cells.Count - 1)) cells[lastFrozen] = cells[lastFrozen] with { FrozenLast = true };
        return cells;
    }

    /// <summary>Adds the table role, the counts and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var rows = view.Rows;
        builder.AddAttribute(10, "role", "table");
        builder.AddAttribute(11, "row-count", rows.Count + 1 + (HasFooter() ? 1 : 0));
        builder.AddAttribute(12, "column-count", Leading + VisibleColumns().Count);
        builder.AddAttribute(13, "multiselectable", SelectionMode == SelectionMode.Multiple);
        builder.AddAttribute(14, "gridlines", GridLines.ToString().ToLowerInvariant());
        builder.AddAttribute(15, "size", Vocabulary.Name(Size));
        builder.AddAttribute(16, "busy", Loading || view.IsLoading);
        builder.AddAttribute(17, "empty", rows.Count == 0);
        builder.AddAttribute(18, "striped", Striped);
        builder.AddAttribute(19, "__internal_onkeydown#grid", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(21, "__internal_onfocusout#grid", EventCallback.Factory.Create<FocusEventArgs>(this, FocusOutAsync));
    }

    private bool HasFooter() => columns.Any(c => !c.Hidden && c.FooterTemplate is not null);

    /// <summary>Adds the header, the virtualized body, the footer and the state parts.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(500, "DataGridRowMetric");
        builder.AddAttribute(501, "semantic-hidden", true);
        builder.AddAttribute(502, "__internal_onresize#metric", EventCallback.Factory.Create<ResizeEventArgs>(this, MeasureRow));
        builder.CloseElement();
        EnsureKeys();
        var cells = Layout();
        var rows = view.Rows;
        var total = cells.Count == 0 ? 0 : cells[^1].Left + cells[^1].Width;
        if (currentRow >= rows.Count) currentRow = rows.Count - 1;
        if (currentRow == -2) currentRow = rows.Count > 0 ? 0 : -1;
        currentColumn = Math.Clamp(currentColumn, 0, Math.Max(0, cells.Count - 1));
        builder.OpenComponent<CascadingValue<DataGrid<TItem>>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();

        builder.OpenElement(1010, "DataGridHeader");
        builder.AddAttribute(1011, "role", "rowgroup");
        builder.OpenElement(1012, "DataGridHeaderRow");
        builder.AddAttribute(1013, "role", "row");
        builder.AddAttribute(1014, "row-index", 1);
        builder.AddAttribute(1015, "style", "width: " + Css.Px(total) + "; left: " + Css.Px(-scrollLeft));
        builder.AddContent(1016, (RenderFragment)(b => RenderHeaderCells(b, cells)));
        builder.CloseElement();
        builder.CloseElement();

        if (rows.Count == 0 && !(Loading || view.IsLoading))
        {
            builder.OpenElement(1020, "DataGridEmpty");
            builder.AddAttribute(1021, "role", "status");
            if (EmptyContent is not null) builder.AddContent(1022, EmptyContent);
            else builder.AddContent(1023, view.LoadError is { } error ? error.Message : ControlStrings.Get("NoResults"));
            builder.CloseElement();
        }
        else
        {
            builder.OpenComponent<Virtualizer<DataRow<TItem>>>(1030);
            builder.AddComponentParameter(1031, nameof(Virtualizer<DataRow<TItem>>.As), "DataGridBody");
            builder.AddComponentParameter(1032, nameof(Virtualizer<DataRow<TItem>>.Items), rows);
            builder.AddComponentParameter(1033, nameof(Virtualizer<DataRow<TItem>>.ItemSize), EffectiveRowHeight);
            builder.AddComponentParameter(1034, nameof(Virtualizer<DataRow<TItem>>.MeasureItems), HasDetails);
            builder.AddComponentParameter(1035, nameof(Virtualizer<DataRow<TItem>>.Role), "rowgroup");
            builder.AddComponentParameter(1036, nameof(Virtualizer<DataRow<TItem>>.ItemKey), (Func<DataRow<TItem>, object>)RowKey);
            builder.AddComponentParameter(1037, nameof(Virtualizer<DataRow<TItem>>.ItemTemplate), (RenderFragment<VirtualItem<DataRow<TItem>>>)(row => b => RenderRow(b, row, cells, total)));
            builder.AddComponentParameter(1038, nameof(Virtualizer<DataRow<TItem>>.OnRangeChanged), new EventCallback<VirtualRange>(null, (Action<VirtualRange>)RangeChanged));
            builder.AddComponentParameter(1039, nameof(Virtualizer<DataRow<TItem>>.AdditionalAttributes),
                new Dictionary<string, object?>(hover.Attributes(this, () => body?.Element, row => row.Name is "DataGridRow" or "DataGridGroupHeader"))
                {
                    ["__internal_onscroll#grid"] = EventCallback.Factory.Create<ScrollEventArgs>(this, Scrolled),
                    ["__internal_onresize#grid"] = EventCallback.Factory.Create<ResizeEventArgs>(this, Resized),
                });
            builder.AddComponentParameter(1042, nameof(Virtualizer<DataRow<TItem>>.OverlayContent), (RenderFragment)(content =>
            {
                content.OpenComponent<Indicator>(0);
                content.AddComponentParameter(1, nameof(Indicator.As), "HoverIndicator");
                content.AddComponentParameter(2, nameof(Indicator.Target), hover.Target);
                content.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(1043, component => body = (Virtualizer<DataRow<TItem>>)component);
            builder.CloseComponent();
        }

        if (HasFooter())
        {
            builder.OpenElement(1050, "DataGridFooter");
            builder.AddAttribute(1051, "role", "rowgroup");
            builder.OpenElement(1052, "DataGridFooterRow");
            builder.AddAttribute(1053, "role", "row");
            builder.AddAttribute(1054, "row-index", rows.Count + 2);
            builder.AddAttribute(1055, "style", "width: " + Css.Px(total) + "; left: " + Css.Px(-scrollLeft));
            builder.AddContent(1056, (RenderFragment)(b => RenderFooterCells(b, cells)));
            builder.CloseElement();
            builder.CloseElement();
        }

        if (Loading || view.IsLoading)
        {
            builder.OpenElement(1060, "DataGridLoading");
            builder.AddAttribute(1061, "role", "status");
            builder.AddAttribute(1062, "live", "polite");
            builder.AddContent(1063, ControlStrings.Get("Loading"));
            builder.CloseElement();
        }
    }

    private void MeasureRow(ResizeEventArgs e)
    {
        if (!double.IsFinite(e.Height) || e.Height <= 0 || Math.Abs(measuredRowHeight - e.Height) < .001) return;
        measuredRowHeight = e.Height;
        if (RowHeight is null) StateHasChanged();
    }

    private object RowKey(DataRow<TItem> row) => row.Kind == DataRowKind.GroupHeader ? new GroupRowKey(row.Group!.Key) : KeyOf(row.Item);

    private readonly record struct GroupRowKey(object? Key);

    private string CellStyle(Cell cell, double? height = null) =>
        "width: " + Css.Px(cell.Width) + (cell.Frozen ? "; left: " + Css.Px(scrollLeft) : "") + (height is { } h ? "; height: " + Css.Px(h) : "");

    private void RenderHeaderCells(RenderTreeBuilder builder, List<Cell> cells)
    {
        for (var c = 0; c < cells.Count; c++)
        {
            var cell = cells[c];
            var index = c;
            var isCurrent = currentRow == -1 && currentColumn == c;
            switch (cell.Slot)
            {
                case Slot.Selection:
                    builder.OpenElement(0, "DataGridSelectionHeader");
                    builder.SetKey("#selection");
                    AddHeaderCellAttributes(builder, cell, index, isCurrent);
                    if (SelectionMode == SelectionMode.Multiple)
                    {
                        var count = SelectedVisibleCount();
                        builder.OpenComponent<CheckBox>(20);
                        builder.AddComponentParameter(21, nameof(CheckBox.IsChecked), count > 0 && count == byKey.Count);
                        builder.AddComponentParameter(22, nameof(CheckBox.IsIndeterminate), count > 0 && count < byKey.Count);
                        builder.AddComponentParameter(23, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, SelectAllAsync));
                        builder.AddComponentParameter(24, nameof(CheckBox.AccessibleName), ControlStrings.Get("SelectAll"));
                        builder.AddComponentParameter(25, nameof(CheckBox.TabIndex), -1);
                        builder.CloseComponent();
                    }
                    builder.CloseElement();
                    break;
                case Slot.Details:
                    builder.OpenElement(30, "DataGridDetailsHeader");
                    builder.SetKey("#details");
                    AddHeaderCellAttributes(builder, cell, index, isCurrent);
                    builder.CloseElement();
                    break;
                default:
                    RenderColumnHeader(builder, cell, index, isCurrent);
                    break;
            }
        }
    }

    private void AddHeaderCellAttributes(RenderTreeBuilder builder, Cell cell, int index, bool isCurrent)
    {
        builder.AddAttribute(1, "role", "columnheader");
        builder.AddAttribute(2, "column-index", index + 1);
        builder.AddAttribute(3, "row-index", 1);
        builder.AddAttribute(4, "focusable", true);
        builder.AddAttribute(5, "tabindex", isCurrent ? 0 : -1);
        builder.AddAttribute(6, "current", isCurrent);
        builder.AddAttribute(7, "frozen", cell.Frozen);
        builder.AddAttribute(8, "frozen-last", cell.FrozenLast);
        builder.AddAttribute(9, "style", CellStyle(cell));
        builder.AddAttribute(10, "accessible-name", cell.Slot switch
        {
            Slot.Selection => ControlStrings.Get("SelectAll"),
            Slot.Details => ControlStrings.Get("RowDetails"),
            _ => null,
        });
        builder.AddAttribute(11, "__internal_onfocusin#grid", EventCallback.Factory.Create<FocusEventArgs>(this, () => Focused(-1, index)));
    }

    private void RenderColumnHeader(RenderTreeBuilder builder, Cell cell, int index, bool isCurrent)
    {
        var column = cell.Column!;
        var key = column.ColumnKey;
        var direction = view.SortDirectionOf(key);
        var sortIndex = view.SortIndexOf(key);
        builder.OpenElement(40, "DataGridColumnHeader");
        builder.SetKey(key);
        AddHeaderCellAttributes(builder, cell, index, isCurrent);
        builder.AddAttribute(41, "class", column.Class);
        builder.AddAttribute(42, "accessible-name", column.Header);
        builder.AddAttribute(43, "sort", direction switch { ListSortDirection.Ascending => "ascending", ListSortDirection.Descending => "descending", _ => "none" });
        builder.AddAttribute(44, "accessible-description", direction switch
        {
            ListSortDirection.Ascending => ControlStrings.Get("SortAscending"),
            ListSortDirection.Descending => ControlStrings.Get("SortDescending"),
            _ => null,
        });
        builder.AddAttribute(45, "sortable", column.CanSort);
        builder.AddAttribute(46, "resizable", column.Resizable);
        builder.AddAttribute(47, "align", column.Alignment.ToString().ToLowerInvariant());
        builder.AddAttribute(48, "dragging", drag?.Key == key);
        builder.AddAttribute(49, "drop-target", drag is { } d && d.Key != key && VisibleColumns().FindIndex(c => c.ColumnKey == key) == d.Target
            ? (d.Target > VisibleColumns().FindIndex(c => c.ColumnKey == d.Key) ? "after" : "before") : null);
        if (column.CanSort) builder.AddAttribute(50, "onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => HeaderClickedAsync(column, e)));
        RenderFragment content = b =>
        {
            b.OpenElement(0, "DataGridColumnHeaderText");
            if (column.HeaderTemplate is not null) b.AddContent(1, column.HeaderTemplate);
            else b.AddContent(2, column.Header);
            b.CloseElement();
            if (direction is not null)
            {
                b.OpenElement(3, "DataGridSortMark");
                b.AddAttribute(4, "glyph", direction == ListSortDirection.Ascending ? "chevron-up" : "chevron-down");
                b.AddAttribute(5, "semantic-hidden", true);
                b.AddAttribute(6, "sort-index", sortIndex + 1);
                b.CloseElement();
                if (view.Sorts.Count > 1)
                {
                    b.OpenElement(7, "DataGridSortIndex");
                    b.AddAttribute(8, "semantic-hidden", true);
                    b.AddContent(9, (sortIndex + 1).ToString(System.Globalization.CultureInfo.CurrentCulture));
                    b.CloseElement();
                }
            }
        };
        if (ReorderableColumns)
        {
            builder.OpenComponent<Draggable>(60);
            builder.AddComponentParameter(61, nameof(Draggable.As), "DataGridColumnHeaderContent");
            builder.AddComponentParameter(62, nameof(Draggable.Axis), Orientation.Horizontal);
            builder.AddComponentParameter(63, nameof(Draggable.Threshold), 6.0);
            builder.AddComponentParameter(64, nameof(Draggable.OnDrag), EventCallback.Factory.Create<Primitives.DragEventArgs>(this, e => Dragged(column, e)));
            builder.AddComponentParameter(65, nameof(Draggable.OnDragEnd), EventCallback.Factory.Create<Primitives.DragEventArgs>(this, e => DroppedAsync(column, e)));
            builder.AddComponentParameter(66, nameof(Draggable.ChildContent), content);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(70, "DataGridColumnHeaderContent");
            builder.AddContent(71, content);
            builder.CloseElement();
        }
        if (column.Resizable)
        {
            builder.OpenComponent<ResizeHandle>(80);
            builder.AddComponentParameter(81, nameof(ResizeHandle.Edge), ResizeEdge.Right);
            builder.AddComponentParameter(82, nameof(ResizeHandle.Width), cell.Width);
            builder.AddComponentParameter(83, nameof(ResizeHandle.MinSize), column.MinWidth);
            builder.AddComponentParameter(84, nameof(ResizeHandle.MaxSize), column.MaxWidth);
            builder.AddComponentParameter(85, nameof(ResizeHandle.TabIndex), -1);
            builder.AddComponentParameter(86, nameof(ResizeHandle.AccessibleName), column.Header);
            builder.AddComponentParameter(87, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, v => Resize(key, v.Width)));
            builder.CloseComponent();
        }
        builder.CloseElement();
    }

    private void RenderFooterCells(RenderTreeBuilder builder, List<Cell> cells)
    {
        for (var c = 0; c < cells.Count; c++)
        {
            var cell = cells[c];
            builder.OpenElement(0, "DataGridFooterCell");
            builder.SetKey(cell.Column?.ColumnKey ?? "#" + cell.Slot);
            builder.AddAttribute(1, "role", "cell");
            builder.AddAttribute(2, "column-index", c + 1);
            builder.AddAttribute(3, "frozen", cell.Frozen);
            builder.AddAttribute(4, "frozen-last", cell.FrozenLast);
            builder.AddAttribute(5, "align", cell.Column?.Alignment.ToString().ToLowerInvariant());
            builder.AddAttribute(6, "class", cell.Column?.Class);
            builder.AddAttribute(7, "style", CellStyle(cell));
            if (cell.Column?.FooterTemplate is { } footer) builder.AddContent(8, footer(view.FilteredItems));
            builder.CloseElement();
        }
    }

    private void RenderRow(RenderTreeBuilder builder, VirtualItem<DataRow<TItem>> virtualRow, List<Cell> cells, double total)
    {
        var row = virtualRow.Item;
        var rowIndex = virtualRow.Index;
        if (row.Kind == DataRowKind.GroupHeader)
        {
            RenderGroupHeader(builder, row.Group!, rowIndex, cells.Count, total);
            return;
        }
        var item = row.Item;
        var key = KeyOf(item);
        var selected = selection.IsSelected(key);
        var open = HasDetails && expandedDetails.Contains(key);
        builder.OpenElement(0, "DataGridRow");
        builder.AddAttribute(1, "role", "row");
        builder.AddAttribute(2, "row-index", rowIndex + 2);
        builder.AddAttribute(3, "selected", selected);
        builder.AddAttribute(4, "alternate", Striped && row.ItemPosition.Position % 2 == 0);
        builder.AddAttribute(5, "expanded", HasDetails ? open : null);
        builder.AddAttribute(6, "current", rowIndex == currentRow);
        builder.AddAttribute(7, "style", RowStyle(total));
        builder.AddAttribute(8, "item-position", row.ItemPosition);
        for (var c = 0; c < cells.Count; c++) RenderCell(builder, cells[c], c, rowIndex, item, key, selected, open);
        builder.CloseElement();
        if (open)
        {
            builder.OpenElement(100, "DataGridRowDetails");
            builder.AddAttribute(101, "style", "width: " + Css.Px(Math.Max(total, viewportWidth)));
            builder.AddContent(102, RowDetailsTemplate!(item));
            builder.CloseElement();
        }
    }

    private void RenderCell(RenderTreeBuilder builder, Cell cell, int c, int rowIndex, TItem item, object key, bool selected, bool open)
    {
        var isCurrent = rowIndex == currentRow && c == currentColumn;
        var name = cell.Slot switch { Slot.Selection => "DataGridSelectionCell", Slot.Details => "DataGridDetailsCell", _ => "DataGridCell" };
        var column = cell.Column;
        var isEditing = editing is { } edit && edit.Row == rowIndex && column?.ColumnKey == edit.Column;
        builder.OpenElement(10, name);
        builder.SetKey(column?.ColumnKey ?? "#" + cell.Slot);
        builder.AddAttribute(11, "role", column?.IsRowHeader == true ? "rowheader" : "cell");
        builder.AddAttribute(12, "row-index", rowIndex + 2);
        builder.AddAttribute(13, "column-index", c + 1);
        builder.AddAttribute(14, "focusable", true);
        builder.AddAttribute(15, "tabindex", isCurrent && !isEditing ? 0 : -1);
        builder.AddAttribute(16, "current", isCurrent);
        builder.AddAttribute(17, "frozen", cell.Frozen);
        builder.AddAttribute(18, "frozen-last", cell.FrozenLast);
        builder.AddAttribute(19, "style", CellStyle(cell));
        builder.AddAttribute(20, "class", column?.Class);
        builder.AddAttribute(21, "align", column?.Alignment.ToString().ToLowerInvariant());
        builder.AddAttribute(22, "editable", column?.Editable == true);
        builder.AddAttribute(23, "editing", isEditing);
        builder.AddAttribute(24, "__internal_onfocusin#grid", EventCallback.Factory.Create<FocusEventArgs>(this, () => Focused(rowIndex, c)));
        builder.AddAttribute(25, "__internal_onpointerdown#grid", EventCallback.Factory.Create<PointerEventArgs>(this, e => CellPressedAsync(rowIndex, c, e)));
        if (cell.Slot == Slot.Data) builder.AddAttribute(26, "__internal_ondblclick#grid", EventCallback.Factory.Create<PointerEventArgs>(this, () => DoubleClickedAsync(rowIndex, c)));
        switch (cell.Slot)
        {
            case Slot.Selection:
                builder.AddAttribute(30, "accessible-name", ControlStrings.Get("SelectRow"));
                builder.AddAttribute(31, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ToggleSelectionAsync(key)));
                builder.OpenComponent<CheckBox>(32);
                builder.AddComponentParameter(33, nameof(CheckBox.IsChecked), selected);
                builder.AddComponentParameter(34, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, () => ToggleSelectionAsync(key)));
                builder.AddComponentParameter(35, nameof(CheckBox.AccessibleName), ControlStrings.Get("SelectRow"));
                builder.AddComponentParameter(36, nameof(CheckBox.TabIndex), -1);
                builder.CloseComponent();
                break;
            case Slot.Details:
                builder.AddAttribute(40, "accessible-name", ControlStrings.Get(open ? "Collapse" : "Expand"));
                builder.AddAttribute(41, "expanded", open);
                builder.AddAttribute(42, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ToggleDetails(key)));
                builder.OpenElement(43, "DataGridDetailsToggle");
                builder.AddAttribute(44, "glyph", open ? "chevron-down" : "chevron-right");
                builder.AddAttribute(45, "expanded", open);
                builder.AddAttribute(46, "semantic-hidden", true);
                builder.CloseElement();
                break;
            default:
                if (isEditing) RenderEditor(builder, editing!, column!);
                else if (column!.CellTemplate is { } template) builder.AddContent(50, template(item));
                else builder.AddContent(51, column.TextOf(item));
                break;
        }
        builder.CloseElement();
    }

    private void RenderEditor(RenderTreeBuilder builder, Edit edit, DataGridColumn<TItem> column)
    {
        if (column.EditorTemplate is { } template)
        {
            builder.AddContent(60, template(edit.Context));
            return;
        }
        builder.OpenComponent<EditableText>(61);
        builder.AddComponentParameter(62, nameof(EditableText.As), "DataGridCellEditor");
        builder.AddComponentParameter(63, nameof(EditableText.Value), edit.Text);
        builder.AddComponentParameter(64, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, text =>
        {
            edit.Text = text;
            edit.Context.Invalid = false;
        }));
        builder.AddComponentParameter(65, nameof(EditableText.Invalid), edit.Context.Invalid);
        builder.AddComponentParameter(66, nameof(EditableText.AccessibleName), column.Header);
        builder.AddComponentReferenceCapture(67, component => editorComponent = (EditableText)component);
        builder.CloseComponent();
    }

    private void RenderGroupHeader(RenderTreeBuilder builder, DataGroup<TItem> group, int rowIndex, int span, double total)
    {
        var isCurrent = rowIndex == currentRow;
        builder.OpenElement(200, "DataGridGroupHeader");
        builder.AddAttribute(201, "role", "row");
        builder.AddAttribute(202, "row-index", rowIndex + 2);
        builder.AddAttribute(203, "expanded", group.IsExpanded);
        builder.AddAttribute(204, "current", isCurrent);
        builder.AddAttribute(205, "style", RowStyle(Math.Max(total, viewportWidth)));
        builder.OpenElement(206, "DataGridGroupHeaderCell");
        builder.AddAttribute(207, "role", "cell");
        builder.AddAttribute(208, "row-index", rowIndex + 2);
        builder.AddAttribute(209, "column-index", 1);
        builder.AddAttribute(210, "column-span", Math.Max(1, span));
        builder.AddAttribute(211, "focusable", true);
        builder.AddAttribute(212, "tabindex", isCurrent ? 0 : -1);
        builder.AddAttribute(213, "current", isCurrent);
        builder.AddAttribute(214, "expanded", group.IsExpanded);
        builder.AddAttribute(215, "style", "left: " + Css.Px(scrollLeft));
        builder.AddAttribute(216, "__internal_onfocusin#grid", EventCallback.Factory.Create<FocusEventArgs>(this, () => Focused(rowIndex, currentColumn)));
        builder.AddAttribute(217, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => view.ToggleGroup(group.Key)));
        builder.OpenElement(218, "DataGridGroupToggle");
        builder.AddAttribute(219, "glyph", group.IsExpanded ? "chevron-down" : "chevron-right");
        builder.AddAttribute(220, "semantic-hidden", true);
        builder.CloseElement();
        if (GroupHeaderTemplate is not null) builder.AddContent(221, GroupHeaderTemplate(group));
        else
        {
            builder.OpenElement(222, "DataGridGroupKey");
            builder.AddContent(223, Convert.ToString(group.Key, System.Globalization.CultureInfo.CurrentCulture) ?? "");
            builder.CloseElement();
            builder.OpenElement(224, "DataGridGroupCount");
            builder.AddContent(225, ControlStrings.Format("ItemCount", group.Count));
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    }

    private void Scrolled(ScrollEventArgs e)
    {
        if (Math.Abs(e.ScrollLeft - scrollLeft) < 0.5) return;
        scrollLeft = e.ScrollLeft;
    }

    private void Resized(ResizeEventArgs e)
    {
        // Columns fill the body's content box, so a theme's padding around the rows does not make them overflow.
        if (Math.Abs(e.ContentWidth - viewportWidth) < 0.5) return;
        viewportWidth = e.ContentWidth;
    }

    private void RangeChanged(VirtualRange range)
    {
        if (pendingFocus) StateHasChanged();
    }

    private void Focused(int row, int column)
    {
        if (row == currentRow && column == currentColumn) return;
        (currentRow, currentColumn) = (row, column);
        StateHasChanged();
    }

    private async Task FocusOutAsync(FocusEventArgs e)
    {
        if (editing is not { } edit) return;
        var editingCell = e.RelatedTarget is { } next ? ControlTree.Ancestors(next).FirstOrDefault(n => n.Attributes.GetValueOrDefault("editing") is true) : null;
        if (editingCell is not null) return;
        if (pendingEditorFocus) return;
        await CommitAsync(edit, refocus: false);
    }

    /// <summary>Refreshes hovered row geometry and focuses the pending cell or editor after rendering.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the focus has moved.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        hover.Refresh();
        if (pendingEditorFocus && editing is { } edit)
        {
            var cell = FindCell(edit.Row, currentColumn);
            var target = editorComponent?.Element.Element is { IsRemoved: false } editor ? editor
                : cell is null ? null : ControlTree.Descendants(cell).FirstOrDefault(ControlTree.IsFocusable);
            if (target is not null)
            {
                pendingEditorFocus = false;
                await new ElementReference(target).FocusAsync(preventScroll: true);
            }
            return;
        }
        if (!pendingFocus) return;
        if (FindCell(currentRow, currentRow >= 0 && view.Rows.Count > currentRow && view.Rows[currentRow].Kind == DataRowKind.GroupHeader ? 0 : currentColumn) is { } found)
        {
            pendingFocus = false;
            await new ElementReference(found).FocusAsync(preventScroll: true);
        }
    }

    private ElementNode? FindCell(int row, int column)
    {
        if (Element.Element is not { } root) return null;
        foreach (var node in ControlTree.Descendants(root))
            if (node.Attributes.GetValueOrDefault("column-index") is int c && c == column + 1 && node.Attributes.GetValueOrDefault("row-index") is int r && r == row + 2
                && node.Attributes.GetValueOrDefault("focusable") is true)
                return node;
        return null;
    }

    private void MoveTo(int row, int column)
    {
        var rows = view.Rows;
        var cells = Layout();
        row = Math.Clamp(row, -1, rows.Count - 1);
        column = Math.Clamp(column, 0, Math.Max(0, cells.Count - 1));
        (currentRow, currentColumn) = (row, column);
        if (row >= 0) body?.ScrollIntoView(row);
        if (cells.Count > 0 && rows.Count > 0 && (row < 0 || rows[row].Kind == DataRowKind.Item)) RevealColumn(cells, column);
        pendingFocus = true;
        StateHasChanged();
    }

    private void RevealColumn(List<Cell> cells, int column)
    {
        var cell = cells[column];
        if (cell.Frozen || body is null || viewportWidth <= 0) return;
        var frozen = cells.Where(c => c.Frozen).Sum(c => c.Width);
        double? target = cell.Left - frozen < scrollLeft ? cell.Left - frozen : cell.Left + cell.Width > scrollLeft + viewportWidth ? cell.Left + cell.Width - viewportWidth : null;
        if (target is { } left) body.Element.ScrollTo(Math.Max(0, left), null);
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled) return;
        if (editing is { } edit)
        {
            if (e.Key == "Enter" && (e.Modifiers & (KeyModifiers.Shift | KeyModifiers.Alt)) == 0)
            {
                e.Handled = true;
                await CommitAsync(edit, refocus: true);
            }
            else if (e.Key == "Escape")
            {
                e.Handled = true;
                CancelEdit(refocus: true);
            }
            return;
        }
        var rows = view.Rows;
        var cells = Layout();
        if (cells.Count == 0) return;
        var control = (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        var alt = (e.Modifiers & KeyModifiers.Alt) != 0;
        var row = currentRow;
        var column = Math.Clamp(currentColumn, 0, cells.Count - 1);
        var isGroup = row >= 0 && row < rows.Count && rows[row].Kind == DataRowKind.GroupHeader;
        if (e.Key is "a" or "A" && control && !alt)
        {
            if (SelectionMode != SelectionMode.Multiple) return;
            e.Handled = true;
            if (selection.SelectAll()) await NotifySelectionAsync();
            return;
        }
        if (row == -1 && cells[column].Column is { } header)
        {
            if (e.Key is "Enter" or " " && !control && !alt)
            {
                e.Handled = true;
                if (header.CanSort) await CycleSortAsync(header, shift);
                return;
            }
            if (alt && e.Key is "ArrowLeft" or "ArrowRight")
            {
                e.Handled = true;
                if (ReorderableColumns) await MoveColumnAsync(header.ColumnKey, e.Key == "ArrowLeft" ? -1 : 1);
                return;
            }
            if (control && e.Key is "ArrowLeft" or "ArrowRight")
            {
                e.Handled = true;
                if (header.Resizable) Resize(header.ColumnKey, cells[column].Width + (e.Key == "ArrowLeft" ? -10 : 10));
                return;
            }
        }
        if (row >= 0 && row < rows.Count && e.Key is " " or "Enter" or "F2" && !alt)
        {
            e.Handled = true;
            var data = rows[row];
            if (isGroup)
            {
                if (e.Key != "F2") view.ToggleGroup(data.Group!.Key);
                return;
            }
            var key = KeyOf(data.Item);
            var slot = cells[column];
            if (e.Key == " ")
            {
                if (slot.Slot == Slot.Details) ToggleDetails(key);
                else if (slot.Slot == Slot.Selection || control) await ToggleSelectionAsync(key);
                else if (shift && SelectionMode == SelectionMode.Multiple)
                {
                    if (selection.Extend(key)) await NotifySelectionAsync();
                }
                else if (selection.Select(key)) await NotifySelectionAsync();
                return;
            }
            if (slot.Slot == Slot.Details) ToggleDetails(key);
            else if (slot.Slot == Slot.Selection) await ToggleSelectionAsync(key);
            else if (slot.Column!.Editable) BeginEdit(row, column);
            else if (e.Key == "Enter") await OnRowActivated.InvokeAsync(data.Item);
            return;
        }
        if (isGroup && !control && !shift && e.Key is "ArrowLeft" or "ArrowRight")
        {
            var group = rows[row].Group!;
            if (group.IsExpanded == (e.Key == "ArrowRight")) return;
            e.Handled = true;
            view.SetGroupExpanded(group.Key, e.Key == "ArrowRight");
            return;
        }
        if (alt) return;
        var top = -1;
        var pageRows = body?.PageSize() ?? 10;
        (int Row, int Column)? target = e.Key switch
        {
            "ArrowDown" when !control => (Math.Min(rows.Count - 1, row + 1), column),
            "ArrowUp" when !control => (Math.Max(top, row - 1), column),
            "ArrowRight" when !control => (row, Math.Min(cells.Count - 1, column + 1)),
            "ArrowLeft" when !control => (row, Math.Max(0, column - 1)),
            "Home" when control => (rows.Count > 0 ? 0 : -1, 0),
            "End" when control => (rows.Count - 1, cells.Count - 1),
            "Home" => (row, 0),
            "End" => (row, cells.Count - 1),
            "PageDown" when !control => (Math.Min(rows.Count - 1, Math.Max(0, row) + pageRows), column),
            "PageUp" when !control => (Math.Max(rows.Count > 0 ? 0 : -1, row - pageRows), column),
            _ => null,
        };
        if (target is not { } next) return;
        e.Handled = true;
        if (next == (row, column)) return;
        MoveTo(next.Row, next.Column);
        if (next.Row >= 0 && next.Row < rows.Count && rows[next.Row].Kind == DataRowKind.Item && next.Row != row)
        {
            var key = KeyOf(rows[next.Row].Item);
            var changed = shift && SelectionMode == SelectionMode.Multiple && e.Key is not ("ArrowLeft" or "ArrowRight") ? ExtendFrom(row, key)
                : SelectionMode == SelectionMode.Single && SelectionFollowsFocus && !shift && selection.Select(key);
            if (changed) await NotifySelectionAsync();
        }
    }

    private bool ExtendFrom(int previousRow, object key)
    {
        if (selection.Anchor is null && previousRow >= 0 && previousRow < view.Rows.Count && view.Rows[previousRow].Kind == DataRowKind.Item)
            selection.Select(KeyOf(view.Rows[previousRow].Item));
        return selection.Extend(key);
    }

    private async Task CellPressedAsync(int row, int column, PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary) return;
        var rows = view.Rows;
        if (row < 0 || row >= rows.Count || rows[row].Kind != DataRowKind.Item) return;
        var cells = Layout();
        if (column < cells.Count && cells[column].Slot != Slot.Data) return;
        if (editing is { } edit && (edit.Row != row || cells[column].Column?.ColumnKey != edit.Column)) await CommitAsync(edit, refocus: false);
        if (editing is not null) return;
        var key = KeyOf(rows[row].Item);
        var control = (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var changed = (e.Modifiers & KeyModifiers.Shift) != 0 ? selection.Extend(key, control)
            : control ? selection.Toggle(key)
            : selection.Select(key);
        if (changed) await NotifySelectionAsync();
    }

    private async Task DoubleClickedAsync(int row, int column)
    {
        var rows = view.Rows;
        if (row < 0 || row >= rows.Count || rows[row].Kind != DataRowKind.Item || editing is not null) return;
        var cells = Layout();
        if (column < cells.Count && cells[column].Column is { Editable: true }) BeginEdit(row, column);
        else await OnRowActivated.InvokeAsync(rows[row].Item);
    }

    private async Task HeaderClickedAsync(DataGridColumn<TItem> column, PointerEventArgs e)
    {
        if (justDragged)
        {
            justDragged = false;
            return;
        }
        if (e.IsSynthetic) return;
        await CycleSortAsync(column, (e.Modifiers & KeyModifiers.Shift) != 0);
    }

    /// <summary>
    /// Sorts by a column as a click on its header does: ascending, then descending, then not sorted; with
    /// <paramref name="add" /> the other sorts are kept, as with Shift.
    /// </summary>
    /// <param name="key">The column key.</param>
    /// <param name="add">Whether the other sorts are kept.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task SortAsync(string key, bool add = false) =>
        columns.FirstOrDefault(c => c.ColumnKey == key) is { CanSort: true } column ? CycleSortAsync(column, add) : Task.CompletedTask;

    private async Task CycleSortAsync(DataGridColumn<TItem> column, bool add)
    {
        var key = column.ColumnKey;
        var current = sorts.Value.FirstOrDefault(s => s.Key == key);
        var has = sorts.Value.Any(s => s.Key == key);
        ListSortDirection? direction = !has ? ListSortDirection.Ascending : current.Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : null;
        if (!add && has && sorts.Value.Count > 1) direction = ListSortDirection.Ascending;
        var next = add ? sorts.Value.ToList() : [];
        var index = next.FindIndex(s => s.Key == key);
        if (direction is { } value)
        {
            if (index >= 0) next[index] = new DataSort(key, value);
            else next.Add(new DataSort(key, value));
        }
        else if (index >= 0) next.RemoveAt(index);
        sorts.Set(next);
        syncing = true;
        try { ApplySorts(); }
        finally { syncing = false; }
        StateHasChanged();
        await SortsChanged.InvokeAsync(next);
        if (view.NeedsLoad) await view.LoadAsync();
    }

    private void Resize(string key, double width)
    {
        var column = columns.FirstOrDefault(c => c.ColumnKey == key);
        if (column is null) return;
        userWidths[key] = Math.Clamp(width, column.MinWidth, column.MaxWidth);
        StateHasChanged();
    }

    /// <summary>Gets the current width of a visible column, as laid out.</summary>
    /// <param name="key">The column key.</param>
    /// <returns>The width in pixels, or null when the column is not visible.</returns>
    public double? GetColumnWidth(string key) => Layout().FirstOrDefault(c => c.Column?.ColumnKey == key)?.Width;

    private void Dragged(DataGridColumn<TItem> column, Primitives.DragEventArgs e)
    {
        var cells = Layout().Where(c => c.Slot == Slot.Data).ToList();
        var from = cells.FindIndex(c => c.Column == column);
        if (from < 0) return;
        var center = cells[from].Left + cells[from].Width / 2 + e.DeltaX;
        var target = cells.FindIndex(c => center >= c.Left && center < c.Left + c.Width);
        if (target < 0) target = center < cells[0].Left ? 0 : cells.Count - 1;
        if (cells[target].Frozen != column.Frozen) target = from;
        if (drag == new Drag(column.ColumnKey, target)) return;
        drag = new Drag(column.ColumnKey, target);
        StateHasChanged();
    }

    private async Task DroppedAsync(DataGridColumn<TItem> column, Primitives.DragEventArgs e)
    {
        Dragged(column, e);
        justDragged = true;
        if (drag is not { } d) return;
        drag = null;
        var keys = VisibleColumns().Select(c => c.ColumnKey).ToList();
        var from = keys.IndexOf(d.Key);
        if (from >= 0 && d.Target != from) await MoveColumnAsync(d.Key, d.Target - from);
        StateHasChanged();
    }

    /// <summary>Moves a column by a number of places among the visible columns, within the frozen or the scrolling columns.</summary>
    /// <param name="key">The column key.</param>
    /// <param name="offset">The number of places; negative moves towards the start.</param>
    /// <returns>A task that completes when the change has been applied.</returns>
    public Task MoveColumnAsync(string key, int offset)
    {
        var visible = VisibleColumns();
        var from = visible.FindIndex(c => c.ColumnKey == key);
        if (from < 0) return Task.CompletedTask;
        var to = Math.Clamp(from + offset, 0, visible.Count - 1);
        while (to != from && visible[to].Frozen != visible[from].Frozen) to += to > from ? -1 : 1;
        if (to == from) return Task.CompletedTask;
        var moved = visible[from];
        visible.RemoveAt(from);
        visible.Insert(to, moved);
        order = [.. visible.Select(c => c.ColumnKey), .. columns.Where(c => c.Hidden).Select(c => c.ColumnKey)];
        if (currentColumn == Leading + from) currentColumn = Leading + to;
        if (currentRow == -1) pendingFocus = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ToggleDetails(object key)
    {
        if (!expandedDetails.Remove(key)) expandedDetails.Add(key);
        StateHasChanged();
    }

    /// <summary>Opens or closes the details of an item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="open">Whether the details are shown.</param>
    public void SetDetailsOpen(TItem item, bool open)
    {
        var key = KeyOf(item);
        if (open ? expandedDetails.Add(key) : expandedDetails.Remove(key)) StateHasChanged();
    }

    private async Task ToggleSelectionAsync(object key)
    {
        if (SelectionMode == SelectionMode.None) return;
        if (selection.Toggle(key)) await NotifySelectionAsync();
    }

    private async Task SelectAllAsync(bool value)
    {
        if (value ? selection.SelectAll() : selection.Clear()) await NotifySelectionAsync();
        StateHasChanged();
    }

    private async Task NotifySelectionAsync()
    {
        StateHasChanged();
        var items = SelectedInOrder();
        if (SelectionMode == SelectionMode.Multiple)
        {
            lastSelectedItems = items;
            await SelectedItemsChanged.InvokeAsync(items);
        }
        var first = items.Count > 0 ? items[0] : default;
        lastSelectedItem = first;
        await SelectedItemChanged.InvokeAsync(first);
        await Events.RaiseChangeAsync(Element, SelectionMode == SelectionMode.Multiple ? items : first);
    }

    /// <summary>Starts editing a cell, as Enter does on it.</summary>
    /// <param name="item">The item of the row.</param>
    /// <param name="columnKey">The key of an editable column.</param>
    /// <returns>true if the edit started.</returns>
    public bool BeginEdit(TItem item, string columnKey)
    {
        var rows = view.Rows;
        var key = KeyOf(item);
        var row = -1;
        for (var i = 0; i < rows.Count; i++)
            if (rows[i].Kind == DataRowKind.Item && Equals(KeyOf(rows[i].Item), key))
            {
                row = i;
                break;
            }
        var column = Layout().FindIndex(c => c.Column?.ColumnKey == columnKey);
        if (row < 0 || column < 0) return false;
        body?.ScrollIntoView(row);
        return BeginEdit(row, column);
    }

    private bool BeginEdit(int row, int column)
    {
        var cell = Layout()[column];
        if (cell.Column is not { Editable: true } definition || view.Rows[row].Kind != DataRowKind.Item) return false;
        var item = view.Rows[row].Item;
        Edit? state = null;
        var value = definition.ValueOf(item);
        var context = new DataGridEditContext<TItem>(item, definition.ColumnKey, value, () => state is null ? Task.CompletedTask : CommitAsync(state, refocus: true), () => CancelEdit(refocus: true));
        state = new Edit(KeyOf(item), row, definition.ColumnKey, context) { Text = Convert.ToString(value, definition.Culture) ?? "" };
        editing = state;
        (currentRow, currentColumn) = (row, column);
        editorComponent = null;
        pendingEditorFocus = true;
        StateHasChanged();
        return true;
    }

    private async Task CommitAsync(Edit edit, bool refocus)
    {
        if (!ReferenceEquals(editing, edit)) return;
        var column = columns.FirstOrDefault(c => c.ColumnKey == edit.Column);
        if (column is null)
        {
            CancelEdit(refocus);
            return;
        }
        var context = edit.Context;
        if (column.EditorTemplate is null)
        {
            try { context.Value = column.ParseText(edit.Text, context.OldValue); }
            catch (Exception error) when (error is FormatException or InvalidCastException or OverflowException or ArgumentException)
            {
                context.Invalid = true;
            }
        }
        if (context.Invalid)
        {
            if (refocus) pendingEditorFocus = true;
            StateHasChanged();
            return;
        }
        editing = null;
        pendingFocus = refocus;
        StateHasChanged();
        if (Equals(context.Value, context.OldValue)) return;
        column.Setter?.Invoke(context.Item, context.Value);
        await OnCellEdited.InvokeAsync(new DataGridCellEdit<TItem>(context.Item, column.ColumnKey, context.OldValue, context.Value));
    }

    private void CancelEdit(bool refocus)
    {
        if (editing is null) return;
        editing = null;
        pendingFocus = refocus;
        StateHasChanged();
    }

    /// <summary>Computes the rows again, after the items changed in place.</summary>
    public void Refresh()
    {
        view.Refresh();
        StateHasChanged();
    }

    /// <summary>Stops listening to the view.</summary>
    public void Dispose() => view.Changed -= ViewChanged;
}
