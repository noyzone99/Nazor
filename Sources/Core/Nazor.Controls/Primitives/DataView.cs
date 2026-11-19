using System.ComponentModel;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>A sort applied to a <see cref="DataView{TItem}" />, by key and direction, as sent to a data provider.</summary>
/// <param name="Key">The key of the sorted field or column.</param>
/// <param name="Direction">The direction of the sort.</param>
public readonly record struct DataSort(string Key, ListSortDirection Direction);

/// <summary>A sort of a <see cref="DataView{TItem}" /> with the function that reads the sorted value of an item.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Key">The key of the sorted field or column.</param>
/// <param name="Selector">Reads the value compared for an item.</param>
/// <param name="Direction">The direction of the sort.</param>
public sealed record DataSortDescriptor<TItem>(string Key, Func<TItem, object?> Selector, ListSortDirection Direction = ListSortDirection.Ascending)
{
    /// <summary>Gets the comparer of the values, or null for the default comparer (nulls first, then <see cref="IComparable" />).</summary>
    public IComparer<object?>? Comparer { get; init; }
}

/// <summary>What a data provider of a <see cref="DataView{TItem}" /> is asked for: a window of the sorted, filtered items.</summary>
/// <param name="Skip">The number of items to skip.</param>
/// <param name="Take">The number of items wanted, or null for all the remaining items.</param>
/// <param name="Sorts">The sorts, the most significant first.</param>
/// <param name="Query">The application-defined filter, such as a search text, from <see cref="DataView{TItem}.Query" />.</param>
/// <param name="CancellationToken">Canceled when a newer load replaces this one or request settings change.</param>
public sealed record DataRequest(int Skip, int? Take, IReadOnlyList<DataSort> Sorts, object? Query, CancellationToken CancellationToken);

/// <summary>A window of items returned by a data provider of a <see cref="DataView{TItem}" />.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Items">The requested window in global filtered/sorted order, starting at DataRequest.Skip. Membership is copied when accepted.</param>
/// <param name="TotalCount">The complete count matching the request before paging. Nonempty windows must fit within this count and the requested Take.</param>
public sealed record DataPage<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

/// <summary>A group of items of a <see cref="DataView{TItem}" />, formed by <see cref="DataView{TItem}.GroupKey" />.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class DataGroup<TItem>
{
    internal DataGroup(object? key, IReadOnlyList<TItem> items, bool expanded)
    {
        Key = key;
        Items = items;
        IsExpanded = expanded;
    }

    /// <summary>Gets the key shared by the items of the group.</summary>
    public object? Key { get; }

    /// <summary>Gets the group's filtered/sorted items across all local pages, or within the accepted provider window. Provider group membership beyond that window is unknown.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>Gets the number of known items in Items; provider groups cover the accepted window only.</summary>
    public int Count => Items.Count;

    /// <summary>Gets whether the items of the group are shown.</summary>
    public bool IsExpanded { get; }
}

/// <summary>The kind of a row projected by a <see cref="DataView{TItem}" />.</summary>
public enum DataRowKind
{
    /// <summary>A row that shows an item.</summary>
    Item,
    /// <summary>The header of a group, before its items.</summary>
    GroupHeader,
}

/// <summary>A row of the flat projection of a <see cref="DataView{TItem}" />: an item or a group header. Value equality includes its immutable ItemPosition metadata; manually constructed rows have invalid default metadata unless explicitly initialized.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Kind">Whether the row is an item or a group header.</param>
/// <param name="Item">The item, or default for a group header.</param>
/// <param name="Index">The index of the item in <see cref="DataView{TItem}.FilteredItems" />, or -1 for a group header.</param>
/// <param name="Group">The group of the row, or null without grouping.</param>
public readonly record struct DataRow<TItem>(DataRowKind Kind, TItem Item, int Index, DataGroup<TItem>? Group)
{
    /// <summary>Gets the immutable global logical position/count of an item in the accepted local/provider result. Provider grouping preserves the response ordinal. Group headers have the default invalid value; Index retains its FilteredItems meaning.</summary>
    public LogicalItemPosition ItemPosition { get; init; }
}

/// <summary>
/// A view over a collection that a grid or a list renders: it filters, sorts by several keys (stable), groups by a key
/// with group headers that open and close, and pages the result. The source is a list (<see cref="Source" />) processed
/// in memory, or an asynchronous <see cref="Provider" /> that receives the sorts, the <see cref="Query" /> and the page
/// as a <see cref="DataRequest" /> for server-side data. Results are computed lazily and cached until a property
/// changes or <see cref="Refresh" /> is called; <see cref="Changed" /> reports every change of the results. Every member
/// must be used from the UI thread.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class DataView<TItem>
{
    private static readonly object NullKey = new();
    private readonly HashSet<object> collapsed = [];
    private IReadOnlyList<TItem>? source;
    private Func<DataRequest, Task<DataPage<TItem>>>? provider;
    private object? query;
    private Func<TItem, bool>? filter;
    private IReadOnlyList<DataSortDescriptor<TItem>> sorts = [];
    private Func<TItem, object?>? groupKey;
    private IComparer<object?>? groupComparer;
    private ListSortDirection groupDirection;
    private int pageSize;
    private int page;
    private bool dirty = true;
    private bool stale = true;
    private IReadOnlyList<TItem> filtered = [];
    private IReadOnlyList<TItem> items = [];
    private IReadOnlyList<DataGroup<TItem>> groups = [];
    private IReadOnlyList<DataRow<TItem>> rows = [];
    private sealed record LoadedWindow(IReadOnlyList<TItem> Items, int Offset, int TotalCount);
    private LoadedWindow loaded = new([], 0, 0);
    private int total;
    private CancellationTokenSource? loading;

    /// <summary>Gets or sets the items processed in memory; ignored while <see cref="Provider" /> is set.</summary>
    public IReadOnlyList<TItem>? Source
    {
        get => source;
        set => Change(ref source, value, reload: false);
    }

    /// <summary>
    /// Gets or sets the asynchronous source of the items for server-side data, or null to use <see cref="Source" />.
    /// Call <see cref="LoadAsync" /> after changing the request.
    /// </summary>
    public Func<DataRequest, Task<DataPage<TItem>>>? Provider
    {
        get => provider;
        set => Change(ref provider, value, reload: true);
    }

    /// <summary>Gets or sets the application-defined filter sent to <see cref="Provider" />, such as a search text.</summary>
    public object? Query
    {
        get => query;
        set
        {
            if (Equals(query, value)) return;
            query = value;
            Invalidate(reload: true);
        }
    }

    /// <summary>Gets or sets the predicate applied to a local Source, or null for every item. Providers receive Query instead.</summary>
    public Func<TItem, bool>? Filter
    {
        get => filter;
        set => Change(ref filter, value, reload: false);
    }

    /// <summary>Gets or sets the sorts, the most significant first; items with equal keys keep their source order.</summary>
    public IReadOnlyList<DataSortDescriptor<TItem>> Sorts
    {
        get => sorts;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (sorts.SequenceEqual(value)) return;
            sorts = [.. value];
            Invalidate(reload: true);
        }
    }

    /// <summary>Gets or sets the function that gives the group of an item, or null for no grouping.</summary>
    public Func<TItem, object?>? GroupKey
    {
        get => groupKey;
        set => Change(ref groupKey, value, reload: false);
    }

    /// <summary>Gets or sets the comparer that orders the groups, or null for the default comparer.</summary>
    public IComparer<object?>? GroupComparer
    {
        get => groupComparer;
        set => Change(ref groupComparer, value, reload: false);
    }

    /// <summary>Gets or sets the direction in which the groups are ordered.</summary>
    public ListSortDirection GroupDirection
    {
        get => groupDirection;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (groupDirection == value) return;
            groupDirection = value;
            Invalidate(reload: false);
        }
    }

    /// <summary>Gets or sets the number of items per page, or zero to show every item on one page.</summary>
    public int PageSize
    {
        get => pageSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (pageSize == value) return;
            pageSize = value;
            Invalidate(reload: true);
        }
    }

    /// <summary>Gets or sets the requested zero-based page. Local projection uses the last available page if necessary; providers receive the requested offset and may return an empty window beyond their total.</summary>
    public int Page
    {
        get => page;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (page == value) return;
            page = value;
            Invalidate(reload: true);
        }
    }

    /// <summary>Gets the number of items that pass the filter across all pages. With a provider, this is the accepted window's total until another result is accepted.</summary>
    public int TotalCount
    {
        get
        {
            Ensure();
            return total;
        }
    }

    /// <summary>Gets the number of pages, at least one.</summary>
    public int PageCount => pageSize == 0 ? 1 : Math.Max(1, (TotalCount + pageSize - 1) / pageSize);

    /// <summary>
    /// Gets every item that passes the filter in sorted order (grouped items stay together), across all pages; with a
    /// <see cref="Provider" />, the items of the loaded page.
    /// </summary>
    public IReadOnlyList<TItem> FilteredItems
    {
        get
        {
            Ensure();
            return filtered;
        }
    }

    /// <summary>Gets the items of the accepted page, in display order. Provider rows keep the accepted window's positions/count while a new request is pending.</summary>
    public IReadOnlyList<TItem> Items
    {
        get
        {
            Ensure();
            return items;
        }
    }

    /// <summary>Gets the groups, in display order; empty without grouping.</summary>
    public IReadOnlyList<DataGroup<TItem>> Groups
    {
        get
        {
            Ensure();
            return groups;
        }
    }

    /// <summary>
    /// Gets the flat projection of the current page that a list or a grid renders: a header before the items of each
    /// group, and the items of expanded groups only. Each item carries its accepted result's global ItemPosition;
    /// provider grouping can change display order without changing response ordinals.
    /// </summary>
    public IReadOnlyList<DataRow<TItem>> Rows
    {
        get
        {
            Ensure();
            return rows;
        }
    }

    /// <summary>Gets whether a <see cref="Provider" /> request is in progress.</summary>
    public bool IsLoading => loading is not null;

    /// <summary>Gets the error of the last <see cref="Provider" /> request, or null when it succeeded.</summary>
    public Exception? LoadError { get; private set; }

    /// <summary>Gets whether the request changed since the last <see cref="LoadAsync" />, so the provider must be called again.</summary>
    public bool NeedsLoad => provider is not null && stale;

    /// <summary>Gets a number that changes every time the results are computed again.</summary>
    public int Version { get; private set; }

    /// <summary>Raised after any change of the settings, the groups or the loaded data that changes the results.</summary>
    public event Action? Changed;

    /// <summary>Computes the results again, after the items of <see cref="Source" /> changed in place.</summary>
    public void Refresh()
    {
        dirty = true;
        Changed?.Invoke();
    }

    /// <summary>Gets the direction of the sort on a key, or null when the key is not sorted.</summary>
    /// <param name="key">The key of the field or column.</param>
    /// <returns>The direction, or null.</returns>
    public ListSortDirection? SortDirectionOf(string key)
    {
        foreach (var sort in sorts) if (sort.Key == key) return sort.Direction;
        return null;
    }

    /// <summary>Gets the zero-based priority of the sort on a key, or -1 when the key is not sorted.</summary>
    /// <param name="key">The key of the field or column.</param>
    /// <returns>The priority, zero for the most significant sort.</returns>
    public int SortIndexOf(string key)
    {
        for (var i = 0; i < sorts.Count; i++) if (sorts[i].Key == key) return i;
        return -1;
    }

    /// <summary>
    /// Sorts by a key: alone, replacing the other sorts, or added after them with <paramref name="add" />. A null
    /// direction removes the sort on that key.
    /// </summary>
    /// <param name="key">The key of the field or column.</param>
    /// <param name="selector">Reads the value compared for an item.</param>
    /// <param name="direction">The direction, or null to remove the sort.</param>
    /// <param name="add">Whether the other sorts are kept.</param>
    /// <param name="comparer">The comparer of the values, or null for the default one.</param>
    public void SetSort(string key, Func<TItem, object?> selector, ListSortDirection? direction, bool add = false, IComparer<object?>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(selector);
        var next = add ? sorts.ToList() : [];
        var index = next.FindIndex(s => s.Key == key);
        if (direction is not { } value)
        {
            if (index >= 0) next.RemoveAt(index);
        }
        else if (index >= 0) next[index] = next[index] with { Direction = value, Selector = selector, Comparer = comparer };
        else next.Add(new DataSortDescriptor<TItem>(key, selector, value) { Comparer = comparer });
        Sorts = next;
    }

    /// <summary>Gets whether the items of a group are shown; groups are expanded until collapsed.</summary>
    /// <param name="key">The key of the group.</param>
    /// <returns>true if the group is expanded.</returns>
    public bool IsGroupExpanded(object? key) => !collapsed.Contains(key ?? NullKey);

    /// <summary>Shows or hides the items of a group.</summary>
    /// <param name="key">The key of the group.</param>
    /// <param name="expanded">Whether the items are shown.</param>
    public void SetGroupExpanded(object? key, bool expanded)
    {
        if (expanded ? !collapsed.Remove(key ?? NullKey) : !collapsed.Add(key ?? NullKey)) return;
        Invalidate(reload: false);
    }

    /// <summary>Shows the items of a hidden group or hides those of a shown one.</summary>
    /// <param name="key">The key of the group.</param>
    public void ToggleGroup(object? key) => SetGroupExpanded(key, !IsGroupExpanded(key));

    /// <summary>Shows the items of every group.</summary>
    public void ExpandAllGroups()
    {
        if (collapsed.Count == 0) return;
        collapsed.Clear();
        Invalidate(reload: false);
    }

    /// <summary>Hides the items of every current group.</summary>
    public void CollapseAllGroups()
    {
        var changed = false;
        foreach (var group in Groups) changed |= collapsed.Add(group.Key ?? NullKey);
        if (changed) Invalidate(reload: false);
    }

    /// <summary>
    /// Asks <see cref="Provider" /> for the current page when the request changed since the last load (or always with
    /// <paramref name="force" />); a newer call or request-setting change cancels an older one. Accepted membership,
    /// offset and total form one snapshot; obsolete responses/faults are ignored. Invalid provider windows report
    /// LoadError and clear the accepted window. Without a provider it does nothing.
    /// </summary>
    /// <param name="force">Whether the provider is called even when the request did not change.</param>
    /// <returns>A task that completes when the data is available or the request was replaced.</returns>
    public async Task LoadAsync(bool force = false)
    {
        if (provider is not { } source || (!stale && !force)) return;
        loading?.Cancel();
        var cancellation = loading = new CancellationTokenSource();
        stale = false;
        try
        {
            var request = new DataRequest(pageSize == 0 ? 0 : checked(page * pageSize), pageSize == 0 ? null : pageSize,
                [.. sorts.Select(s => new DataSort(s.Key, s.Direction))], query, cancellation.Token);
            Changed?.Invoke();
            if (cancellation.IsCancellationRequested) return;
            var result = await source(request) ?? throw new InvalidOperationException("The data provider returned no page.");
            if (cancellation.IsCancellationRequested) return;
            ArgumentOutOfRangeException.ThrowIfNegative(result.TotalCount);
            var members = result.Items?.ToArray() ?? [];
            if (request.Take is { } take && members.Length > take || members.Length > 0 && (long)request.Skip + members.Length > result.TotalCount)
                throw new InvalidOperationException("The provider window must fit the requested range and total count.");
            loaded = new(members, request.Skip, result.TotalCount);
            LoadError = null;
        }
        catch (Exception error)
        {
            if (cancellation.IsCancellationRequested) return;
            loaded = new([], 0, 0);
            LoadError = error;
        }
        finally
        {
            var current = ReferenceEquals(loading, cancellation);
            if (current) loading = null;
            var canceled = cancellation.IsCancellationRequested;
            cancellation.Dispose();
            if (current && canceled) Changed?.Invoke();
        }
        dirty = true;
        Changed?.Invoke();
    }

    private void Change<T>(ref T field, T value, bool reload) where T : class?
    {
        if (ReferenceEquals(field, value)) return;
        field = value;
        Invalidate(reload);
    }

    private void Invalidate(bool reload)
    {
        dirty = true;
        if (reload)
        {
            stale = true;
            loading?.Cancel();
        }
        Changed?.Invoke();
    }

    private void Ensure()
    {
        if (!dirty) return;
        dirty = false;
        Version++;
        if (provider is not null) ComputeLoaded();
        else ComputeLocal();
    }

    private void ComputeLoaded()
    {
        var snapshot = loaded;
        IReadOnlyList<TItem> ordered = snapshot.Items;
        int[]? originalIndices = null;
        if (groupKey is not null) ordered = Order(ordered, sortItems: false, out originalIndices);
        total = snapshot.TotalCount;
        filtered = ordered;
        items = ordered;
        BuildGroups(ordered);
        BuildRows(ordered, 0, snapshot.Offset, originalIndices);
    }

    private void ComputeLocal()
    {
        IReadOnlyList<TItem> all = source ?? [];
        if (filter is { } predicate)
        {
            var kept = new List<TItem>();
            foreach (var item in all) if (predicate(item)) kept.Add(item);
            all = kept;
        }
        if (sorts.Count > 0 || groupKey is not null) all = Order(all, sortItems: true, out _);
        filtered = all;
        total = all.Count;
        BuildGroups(all);
        var first = 0;
        if (pageSize > 0)
        {
            var last = Math.Max(0, (total + pageSize - 1) / pageSize - 1);
            first = Math.Min(page, last) * pageSize;
            var count = Math.Min(pageSize, total - first);
            var window = new TItem[Math.Max(0, count)];
            for (var i = 0; i < window.Length; i++) window[i] = all[first + i];
            items = window;
        }
        else items = all;
        BuildRows(items, first);
    }

    private IReadOnlyList<TItem> Order(IReadOnlyList<TItem> list, bool sortItems, out int[] indexes)
    {
        var count = list.Count;
        var levels = new List<(object?[] Keys, IComparer<object?> Comparer, int Sign)>();
        if (groupKey is { } group)
        {
            var keys = new object?[count];
            for (var i = 0; i < count; i++) keys[i] = group(list[i]);
            levels.Add((keys, groupComparer ?? DefaultComparer.Instance, groupDirection == ListSortDirection.Descending ? -1 : 1));
        }
        if (sortItems)
            foreach (var sort in sorts)
            {
                var keys = new object?[count];
                for (var i = 0; i < count; i++) keys[i] = sort.Selector(list[i]);
                levels.Add((keys, sort.Comparer ?? DefaultComparer.Instance, sort.Direction == ListSortDirection.Descending ? -1 : 1));
            }
        indexes = new int[count];
        for (var i = 0; i < count; i++) indexes[i] = i;
        Array.Sort(indexes, (a, b) =>
        {
            foreach (var (keys, comparer, sign) in levels)
            {
                var result = comparer.Compare(keys[a], keys[b]);
                if (result != 0) return sign * result;
            }
            return a.CompareTo(b);
        });
        var ordered = new TItem[count];
        for (var i = 0; i < count; i++) ordered[i] = list[indexes[i]];
        return ordered;
    }

    private void BuildGroups(IReadOnlyList<TItem> list)
    {
        if (groupKey is not { } group)
        {
            groups = [];
            return;
        }
        var result = new List<DataGroup<TItem>>();
        var start = 0;
        object? current = null;
        for (var i = 0; i <= list.Count; i++)
        {
            var key = i < list.Count ? group(list[i]) : null;
            if (i > 0 && (i == list.Count || !Equals(key, current)))
            {
                var members = new TItem[i - start];
                for (var j = start; j < i; j++) members[j - start] = list[j];
                result.Add(new DataGroup<TItem>(current, members, IsGroupExpanded(current)));
                start = i;
            }
            current = key;
        }
        groups = result;
    }

    private void BuildRows(IReadOnlyList<TItem> window, int offset, int? logicalOffset = null, int[]? originalIndices = null)
    {
        DataRow<TItem> ItemRow(int i, DataGroup<TItem>? group) => new(DataRowKind.Item, window[i], offset + i, group)
        {
            ItemPosition = new LogicalItemPosition(this, (logicalOffset ?? offset) + (originalIndices?[i] ?? i) + 1, total),
        };
        if (groupKey is not { } group)
        {
            var plain = new DataRow<TItem>[window.Count];
            for (var i = 0; i < window.Count; i++) plain[i] = ItemRow(i, null);
            rows = plain;
            return;
        }
        var lookup = new Dictionary<object, DataGroup<TItem>>();
        foreach (var g in groups) lookup.TryAdd(g.Key ?? NullKey, g);
        var result = new List<DataRow<TItem>>(window.Count + groups.Count);
        DataGroup<TItem>? open = null;
        for (var i = 0; i < window.Count; i++)
        {
            var item = window[i];
            var key = group(item);
            if (open is null || !Equals(open.Key, key))
            {
                open = lookup[key ?? NullKey];
                result.Add(new DataRow<TItem>(DataRowKind.GroupHeader, default!, -1, open));
            }
            if (open.IsExpanded) result.Add(ItemRow(i, open));
        }
        rows = result;
    }

    private sealed class DefaultComparer : IComparer<object?>
    {
        public static readonly DefaultComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            if (x is string a && y is string b) return string.Compare(a, b, StringComparison.CurrentCulture);
            if (x is IComparable comparable && x.GetType() == y.GetType()) return comparable.CompareTo(y);
            if (x is IConvertible && y is IConvertible && IsNumber(x) && IsNumber(y))
                return Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(y, System.Globalization.CultureInfo.InvariantCulture));
            return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
        }

        private static bool IsNumber(object value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}
