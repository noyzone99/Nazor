using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A virtualized grid of tiles, such as files, photos or products: as many columns of <see cref="ItemWidth" /> as fit
/// in its width, rows of <see cref="ItemHeight" />, and only the visible rows realized. It is a single Tab stop: the
/// four arrows move in two dimensions, Home and End go to the ends of the row (with Ctrl to the ends of the grid), Page
/// Up and Page Down move by a screen, typing finds a tile, Space toggles, Ctrl+A selects all, Shift extends and Ctrl
/// moves without selecting; Enter or a double click raise <see cref="OnItemActivated" />. <see cref="SelectedItem" />
/// and <see cref="SelectedItems" /> support <c>@bind-</c>. The element <c>GridView</c> (role <c>listbox</c>) holds the
/// scrolling <c>GridViewViewport</c> (a grid whose columns are written in its style; it needs a height, given by the
/// theme or a class) with the tiles <c>GridViewItem</c> (role <c>option</c>, with <c>GridViewItemCheck</c> and
/// <c>GridViewItemContent</c>); <c>GridViewEmpty</c> replaces it when there are no items.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class GridView<TItem> : Control
{
    private readonly ItemSelection<TItem> selection = new();
    private readonly RovingFocusController keys = new() { Orientation = null };
    private readonly RowFocus focus = new();
    private readonly ItemReorderController reorder;
    private object? reorderedKey;
    private object? currentKey;
    private readonly RenderFragment<VirtualItem<TItem>> tileTemplate;
    private readonly Dictionary<object, int> indexOf = [];
    private Virtualizer<TItem>? virtualizer;
    private IReadOnlyList<TItem> items = [];
    private IReadOnlyList<TItem>? source;
    private int sourceCount = -1;
    private Func<TItem, object>? keying;
    private VirtualRange range;
    private double width;
    private int columns = 1;
    private int current = -1;
    private int tabStop = -1;

    /// <summary>Creates a grid view.</summary>
    public GridView()
    {
        tileTemplate = tile => builder => BuildTile(builder, tile.Item, tile.Index);
        selection.Key = KeyOf;
        reorder = new(() => Element, "GridViewItem", index => IsSkipped(index) ? null : KeyOf(items[index]),
            (from, to) => !IsSkipped(from) && !IsSkipped(to), ReorderAsync, StateHasChanged) { Horizontal = true };
    }

    /// <summary>Gets or sets the items, laid out from left to right and from top to bottom.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets how each tile is rendered; <see cref="ItemText" /> is shown when null.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets the text of an item, shown without a template, matched by typing and used as the accessible name; <c>ToString</c> is used when null.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }

    /// <summary>Gets or sets a function that gives each item a stable key for selection and rendering; the item itself is used when null.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets a function that tells whether an item is disabled: it is skipped by the keyboard and cannot be selected.</summary>
    [Parameter] public Func<TItem, bool>? ItemDisabled { get; set; }

    /// <summary>Gets or sets the minimum width of a column; the columns share the remaining width.</summary>
    [Parameter] public double ItemWidth { get; set; } = 120;

    /// <summary>Gets or sets the height of a row of tiles.</summary>
    [Parameter] public double ItemHeight { get; set; } = 120;

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

    /// <summary>Gets or sets whether tiles show a check mark in multiple selection; a click then toggles the tile.</summary>
    [Parameter] public bool ShowCheckBoxes { get; set; }

    /// <summary>Gets or sets whether dragging or Ctrl+Shift+arrows can request single-tile moves. Requires <see cref="OnItemReorder"/>; Up/Down use the current column count.</summary>
    [Parameter] public bool CanReorderItems { get; set; }

    /// <summary>Gets or sets the callback requesting a move in <see cref="Items"/>. The application applies the move and supplies updated items; selection follows stable keys.</summary>
    [Parameter] public EventCallback<ItemReorderRequest<TItem>> OnItemReorder { get; set; }

    /// <summary>Gets or sets the callback invoked when a tile is activated with Enter or a double click.</summary>
    [Parameter] public EventCallback<TItem> OnItemActivated { get; set; }

    /// <summary>Gets or sets the content shown when there are no items; a localized "No results" when null.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Gets or sets the callback invoked when the realized tiles change, for example to load more data near the end.</summary>
    [Parameter] public EventCallback<VirtualRange> OnRangeChanged { get; set; }

    /// <summary>Gets or sets whether the grid rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the number of columns, computed from the width.</summary>
    public int Columns => columns;

    /// <summary>Gets the index of the current tile, or -1.</summary>
    public int CurrentIndex => current;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "GridView";

    /// <summary>Gets the tile size variables and the caller's declarations.</summary>
    protected override string? ElementStyle =>
        Css.Join($"--nz-grid-item-width: {Css.Px(ItemWidth)}; --nz-grid-item-height: {Css.Px(ItemHeight)}; --nz-grid-columns: {columns}", Style);

    /// <summary>Validates the parameters and synchronizes the items and the selection.</summary>
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(ItemWidth) || ItemWidth <= 0) throw new ArgumentOutOfRangeException(nameof(ItemWidth));
        if (!double.IsFinite(ItemHeight) || ItemHeight <= 0) throw new ArgumentOutOfRangeException(nameof(ItemHeight));
        if (!Enum.IsDefined(SelectionMode)) throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        var next = Items ?? [];
        if (!ReferenceEquals(next, source) || next.Count != sourceCount || ItemKey != keying) Rebuild(next);
        columns = ColumnsFor(width);
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

    /// <summary>Scrolls so that an item is visible and makes it the current tile.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the item is in the grid.</returns>
    public bool ScrollIntoView(TItem item)
    {
        if (!indexOf.TryGetValue(KeyOf(item), out var index)) return false;
        SetCurrent(index);
        UpdateTabStop();
        virtualizer?.ScrollIntoView(index);
        StateHasChanged();
        return true;
    }

    private object KeyOf(TItem item) => ItemKey?.Invoke(item) ?? item!;

    private int ColumnsFor(double available) => available <= 0 ? Math.Max(1, columns) : Math.Max(1, (int)Math.Floor((available + 0.5) / ItemWidth));

    private void Rebuild(IReadOnlyList<TItem> next)
    {
        source = items = next;
        sourceCount = next.Count;
        keying = ItemKey;
        indexOf.Clear();
        for (var i = 0; i < next.Count; i++) indexOf.TryAdd(KeyOf(next[i]), i);
        selection.Model.SetOrder(next.Select(KeyOf));
        if (currentKey is not null && indexOf.TryGetValue(currentKey, out var retained)) current = retained;
        if (current >= next.Count) current = next.Count - 1;
        currentKey = IsSkipped(current) ? null : KeyOf(items[current]);
    }

    private bool IsSkipped(int index) => index < 0 || index >= items.Count || ItemDisabled?.Invoke(items[index]) == true;

    private void SetCurrent(int index)
    {
        current = index;
        currentKey = IsSkipped(index) ? null : KeyOf(items[index]);
    }

    private async Task ReorderAsync(int from, int to)
    {
        if (!reorder.Enabled || IsSkipped(from) || IsSkipped(to) || from == to) return;
        var item = items[from];
        reorderedKey = KeyOf(item);
        await OnItemReorder.InvokeAsync(new(item, from, to));
        StateHasChanged();
    }

    private void ResolveReorderedItem()
    {
        if (reorderedKey is not { } key || !indexOf.TryGetValue(key, out var index)) return;
        reorderedKey = null;
        SetCurrent(index);
        if (RowFocus.HasFocusWithin(Element))
        {
            virtualizer?.ScrollIntoView(index);
            focus.Pending = index;
        }
    }

    private string? TextOf(int index) => ItemText?.Invoke(items[index]) ?? items[index]?.ToString();

    private void UpdateTabStop()
    {
        if (!IsSkipped(current)) { tabStop = current; return; }
        tabStop = -1;
        if (selection.Model.Count > 0)
            for (var i = 0; i < items.Count && tabStop < 0; i++)
                if (!IsSkipped(i) && selection.IsSelected(items[i])) tabStop = i;
        for (var i = 0; i < items.Count && tabStop < 0; i++)
            if (!IsSkipped(i)) tabStop = i;
    }

    private bool Realized(int index) => index >= range.First && index < range.Last;

    /// <summary>Adds the list role, the selection mode, the size listener and the keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var rootStop = !Disabled && tabStop >= 0 && !Realized(tabStop);
        builder.AddAttribute(10, "role", "listbox");
        builder.AddAttribute(11, "multiselectable", SelectionMode == SelectionMode.Multiple);
        builder.AddAttribute(12, "disabled", Disabled);
        builder.AddAttribute(13, "focusable", rootStop);
        if (TabIndex is null && rootStop) builder.AddAttribute(14, "tabindex", 0);
        builder.AddAttribute(15, "column-count", columns);
        builder.AddAttribute(16, "empty", items.Count == 0);
        builder.AddAttribute(17, "__internal_onresize#gridview", EventCallback.Factory.Create<ResizeEventArgs>(this, Resized));
        builder.AddAttribute(18, "__internal_onkeydown#gridview", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(19, "__internal_onfocus#gridview", EventCallback.Factory.Create<FocusEventArgs>(this, RootFocused));
        builder.AddAttribute(20, "reorderable", reorder.Enabled);
        builder.AddAttribute(21, "dragging", reorder.IsDragging);
        builder.AddAttribute(22, "__internal_onpointermove#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Move));
        builder.AddAttribute(23, "__internal_onpointerup#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.UpAsync));
        builder.AddAttribute(24, "__internal_onlostpointercapture#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Lost));
    }

    /// <summary>Adds the viewport with the realized tiles, or the empty content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (items.Count == 0)
        {
            builder.OpenElement(1000, "GridViewEmpty");
            builder.AddAttribute(1001, "role", "text");
            if (EmptyContent is null) builder.AddContent(1002, ControlStrings.Get("NoResults"));
            else builder.AddContent(1003, EmptyContent);
            builder.CloseElement();
            return;
        }
        builder.OpenComponent<Virtualizer<TItem>>(1010);
        builder.AddComponentParameter(1011, nameof(Virtualizer<TItem>.As), "GridViewViewport");
        builder.AddComponentParameter(1012, nameof(Virtualizer<TItem>.Items), items);
        builder.AddComponentParameter(1013, nameof(Virtualizer<TItem>.ItemSize), ItemHeight);
        builder.AddComponentParameter(1014, nameof(Virtualizer<TItem>.Columns), columns);
        builder.AddComponentParameter(1015, nameof(Virtualizer<TItem>.ItemTemplate), tileTemplate);
        builder.AddComponentParameter(1016, nameof(Virtualizer<TItem>.OnRangeChanged), EventCallback.Factory.Create<VirtualRange>(this, RangeChangedAsync));
        builder.AddComponentParameter(1017, nameof(Virtualizer<TItem>.Style), "grid-template-columns: " + string.Join(" ", Enumerable.Repeat("minmax(0px, 1fr)", columns)));
        if (ItemKey is not null) builder.AddComponentParameter(1018, nameof(Virtualizer<TItem>.ItemKey), (Func<TItem, object>)KeyOf);
        builder.AddComponentReferenceCapture(1019, component => virtualizer = (Virtualizer<TItem>)component);
        builder.CloseComponent();
    }

    private void BuildTile(RenderTreeBuilder builder, TItem item, int index)
    {
        var disabled = ItemDisabled?.Invoke(item) == true;
        var selected = selection.IsSelected(item);
        builder.OpenElement(0, "GridViewItem");
        builder.AddAttribute(1, "role", "option");
        builder.AddAttribute(2, "index", index);
        builder.AddAttribute(3, "selected", selected);
        builder.AddAttribute(4, "current", index == current);
        builder.AddAttribute(5, "disabled", disabled);
        builder.AddAttribute(6, "focusable", !disabled && !Disabled);
        builder.AddAttribute(7, "tabindex", index == tabStop ? 0 : -1);
        builder.AddAttribute(8, "position-in-set", index + 1);
        builder.AddAttribute(9, "size-of-set", items.Count);
        if (ItemTemplate is not null && ItemText is not null) builder.AddAttribute(10, "accessible-name", ItemText(item));
        builder.AddAttribute(11, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => ClickAsync(index, e)));
        builder.AddAttribute(12, "__internal_ondblclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => reorder.SuppressPointerActivation ? Task.CompletedTask : ActivateAsync(index)));
        builder.AddAttribute(13, "__internal_onfocus#gridview", EventCallback.Factory.Create<FocusEventArgs>(this, () => TileFocused(index)));
        builder.AddAttribute(14, "item-position", new LogicalItemPosition(this, index + 1, items.Count));
        builder.AddAttribute(15, "__internal_onpointerdown#reorder", EventCallback.Factory.Create<PointerEventArgs>(this, e => reorder.Down(index, e)));
        builder.AddAttribute(16, "dragging", index == reorder.Source);
        builder.AddAttribute(17, "drop-before", index == reorder.Target && !reorder.After);
        builder.AddAttribute(18, "drop-after", index == reorder.Target && reorder.After);
        builder.AddAttribute(19, "layout-transition", true);
        if (ShowCheckBoxes && SelectionMode == SelectionMode.Multiple)
        {
            builder.OpenElement(20, "GridViewItemCheck");
            builder.AddAttribute(21, "semantic-hidden", true);
            builder.AddAttribute(22, "checked", selected);
            builder.AddAttribute(23, "glyph", selected ? "check" : null);
            builder.CloseElement();
        }
        builder.OpenElement(30, "GridViewItemContent");
        if (ItemTemplate is null) builder.AddContent(31, ItemText?.Invoke(item) ?? item?.ToString());
        else builder.AddContent(32, ItemTemplate(item));
        builder.CloseElement();
        builder.CloseElement();
    }

    private void Resized(ResizeEventArgs e)
    {
        width = e.Width;
        var next = ColumnsFor(width);
        if (next == columns) return;
        columns = next;
        StateHasChanged();
    }

    private void TileFocused(int index)
    {
        if (current == index) return;
        SetCurrent(index);
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

    /// <summary>Moves the focus to the current tile once it is realized.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the work is done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        ResolveReorderedItem();
        if (virtualizer is not null && virtualizer.Range != range)
        {
            range = virtualizer.Range;
            StateHasChanged();
        }
        if (focus.Pending >= items.Count) focus.Pending = -1;
        await focus.ApplyAsync(Element, "GridViewItem");
    }

    private void MoveTo(int index)
    {
        SetCurrent(index);
        UpdateTabStop();
        focus.Pending = index;
        virtualizer?.ScrollIntoView(index);
        StateHasChanged();
    }

    private Task NotifyAsync() => selection.NotifyAsync(items, SelectedItemChanged, SelectedItemsChanged, Element);

    private async Task ClickAsync(int index, PointerEventArgs e)
    {
        if (reorder.SuppressClick(e)) return;
        if (Disabled || IsSkipped(index)) return;
        SetCurrent(index);
        var key = KeyOf(items[index]);
        var model = selection.Model;
        var changed = CollectionKeys.Shift(e) && SelectionMode == SelectionMode.Multiple ? model.Extend(key, CollectionKeys.Control(e))
            : CollectionKeys.Control(e) || (ShowCheckBoxes && SelectionMode == SelectionMode.Multiple) ? model.Toggle(key)
            : model.Select(key);
        UpdateTabStop();
        StateHasChanged();
        if (changed) await NotifyAsync();
    }

    private Task ActivateAsync(int index) =>
        Disabled || IsSkipped(index) ? Task.CompletedTask : OnItemActivated.InvokeAsync(items[index]);

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || items.Count == 0) return;
        var at = IsSkipped(current) ? tabStop : current;
        if (await reorder.KeyAsync(e, at, items.Count, columns)) return;
        var control = CollectionKeys.Control(e);
        var shift = CollectionKeys.Shift(e);
        var multiple = SelectionMode == SelectionMode.Multiple;
        var model = selection.Model;
        if (e.Key == " ")
        {
            e.Handled = true;
            if (at < 0) return;
            var key = KeyOf(items[at]);
            var toggled = shift && multiple ? model.Extend(key, control) : multiple || control ? model.Toggle(key) : model.Select(key);
            MoveTo(at);
            if (toggled) await NotifyAsync();
            return;
        }
        if (e.Key == "Enter")
        {
            e.Handled = true;
            if (at < 0) return;
            var picked = SelectionMode == SelectionMode.Single && model.Select(KeyOf(items[at]));
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
        keys.Columns = columns > 1 ? columns : 0;
        keys.PageSize = virtualizer?.PageSize() ?? columns * 3;
        var target = keys.Move(CollectionKeys.WithoutControl(e), at, items.Count, IsSkipped, TextOf);
        if (target is not { } index) return;
        e.Handled = true;
        var next = KeyOf(items[index]);
        var changed = !control && (shift && multiple ? model.Extend(next) : !(ShowCheckBoxes && multiple) && model.Select(next));
        MoveTo(index);
        if (changed) await NotifyAsync();
    }
}
