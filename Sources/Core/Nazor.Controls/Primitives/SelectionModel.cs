namespace Nazor.Controls.Primitives;

/// <summary>How many items a <see cref="SelectionModel{TKey}" /> can select.</summary>
public enum SelectionMode
{
    /// <summary>Nothing can be selected.</summary>
    None,
    /// <summary>At most one item is selected.</summary>
    Single,
    /// <summary>Any number of items can be selected, with ranges and toggles.</summary>
    Multiple,
}

/// <summary>
/// The selection of a collection, independent of how the items are drawn: single or multiple selection, a range from the
/// anchor with Shift, a toggle with Ctrl, select all, and whether the selection may become empty. It holds keys, so the
/// same model serves lists, trees, grids, tabs and segmented controls. The order used for ranges is supplied by the caller.
/// </summary>
/// <typeparam name="TKey">The type of the item keys.</typeparam>
public sealed class SelectionModel<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> selected;
    private readonly List<TKey> order = [];
    private readonly HashSet<TKey> known;
    private SelectionMode mode;

    /// <summary>Creates an empty selection.</summary>
    /// <param name="mode">How many items can be selected.</param>
    /// <param name="comparer">The key comparer, or null for the default one.</param>
    public SelectionModel(SelectionMode mode = SelectionMode.Single, IEqualityComparer<TKey>? comparer = null)
    {
        selected = new HashSet<TKey>(comparer);
        known = new HashSet<TKey>(comparer);
        Mode = mode;
    }

    /// <summary>Gets or sets how many items can be selected; reducing it trims the selection.</summary>
    public SelectionMode Mode
    {
        get => mode;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            mode = value;
            if (value == SelectionMode.None && selected.Count > 0) Replace([]);
            else if (value == SelectionMode.Single && selected.Count > 1) Replace([SelectedKeys[0]]);
        }
    }

    /// <summary>Gets or sets whether a user action may leave nothing selected (Ctrl+click on the only selected item).</summary>
    public bool AllowEmpty { get; set; } = true;

    /// <summary>Gets the key from which Shift extends a range, or default when there is none.</summary>
    public TKey? Anchor { get; private set; }

    /// <summary>Gets the selected keys, in the order of the current items when known.</summary>
    public IReadOnlyList<TKey> SelectedKeys => [.. order.Where(selected.Contains), .. selected.Where(key => !known.Contains(key))];

    /// <summary>Gets the number of selected keys.</summary>
    public int Count => selected.Count;

    /// <summary>Gets the first selected key in item order, or default when nothing is selected.</summary>
    public TKey? SelectedKey => SelectedKeys.Count > 0 ? SelectedKeys[0] : default;

    /// <summary>Raised after every change of the selected keys.</summary>
    public event Action? Changed;

    /// <summary>Sets the order of the current items, used for ranges and for <see cref="SelectAll" />.</summary>
    /// <param name="keys">The keys of every item, in display order.</param>
    public void SetOrder(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        order.Clear();
        order.AddRange(keys);
        known.Clear();
        known.UnionWith(order);
    }

    /// <summary>Gets whether a key is selected.</summary>
    /// <param name="key">The item key.</param>
    /// <returns>true if the key is selected.</returns>
    public bool IsSelected(TKey key) => selected.Contains(key);

    /// <summary>Replaces the selection with a single key, as a plain click or an arrow key does.</summary>
    /// <param name="key">The item key.</param>
    /// <returns>true if the selection changed.</returns>
    public bool Select(TKey key)
    {
        if (Mode == SelectionMode.None) return false;
        Anchor = key;
        return Replace([key]);
    }

    /// <summary>Adds or removes one key, as Ctrl+click does; in single mode it selects or clears that key.</summary>
    /// <param name="key">The item key.</param>
    /// <returns>true if the selection changed.</returns>
    public bool Toggle(TKey key)
    {
        if (Mode == SelectionMode.None) return false;
        Anchor = key;
        if (selected.Contains(key))
        {
            if (!AllowEmpty && selected.Count == 1) return false;
            selected.Remove(key);
            Changed?.Invoke();
            return true;
        }
        if (Mode == SelectionMode.Single) return Replace([key]);
        selected.Add(key);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Selects the keys between the anchor and a key, as Shift+click does.</summary>
    /// <param name="key">The end of the range.</param>
    /// <param name="keepExisting">Whether the range is added to the current selection (Ctrl+Shift).</param>
    /// <returns>true if the selection changed.</returns>
    public bool Extend(TKey key, bool keepExisting = false)
    {
        if (Mode != SelectionMode.Multiple || Anchor is null) return Select(key);
        var from = order.IndexOf(Anchor);
        var to = order.IndexOf(key);
        if (from < 0 || to < 0) return Select(key);
        var range = order.GetRange(Math.Min(from, to), Math.Abs(to - from) + 1);
        return Replace(keepExisting ? [.. selected, .. range] : range);
    }

    /// <summary>Selects every key of the current order in multiple mode.</summary>
    /// <returns>true if the selection changed.</returns>
    public bool SelectAll() => Mode == SelectionMode.Multiple && Replace(order);

    /// <summary>Removes every key from the selection.</summary>
    /// <returns>true if the selection changed.</returns>
    public bool Clear() => Replace([]);

    /// <summary>Replaces the selection with the given keys, trimmed to the selection mode, without raising user semantics.</summary>
    /// <param name="keys">The keys to select.</param>
    /// <returns>true if the selection changed.</returns>
    public bool Replace(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var next = Mode switch
        {
            SelectionMode.None => [],
            SelectionMode.Single => keys.Take(1).ToList(),
            _ => keys.Distinct(selected.Comparer).ToList(),
        };
        if (next.Count == selected.Count && next.All(selected.Contains)) return false;
        selected.Clear();
        foreach (var key in next) selected.Add(key);
        if (Anchor is not null && next.Count == 0) Anchor = default;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Removes keys that are no longer in the current order, after the items changed.</summary>
    /// <returns>true if the selection changed.</returns>
    public bool Prune()
    {
        var removed = selected.RemoveWhere(key => !known.Contains(key));
        if (Anchor is not null && !known.Contains(Anchor)) Anchor = default;
        if (removed == 0) return false;
        Changed?.Invoke();
        return true;
    }
}
