namespace Nazor.Controls.Primitives;

/// <summary>A visible row of a <see cref="TreeModel{TItem}" />, in display order.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Level">The depth, one for the roots.</param>
/// <param name="Parent">The parent item, or default for a root.</param>
/// <param name="HasChildren">Whether the item can be expanded.</param>
/// <param name="IsExpanded">Whether the item is expanded.</param>
/// <param name="PositionInSet">The one-based position among its siblings.</param>
/// <param name="SizeOfSet">The number of its siblings, itself included.</param>
public readonly record struct TreeRow<TItem>(TItem Item, int Level, TItem? Parent, bool HasChildren, bool IsExpanded, int PositionInSet, int SizeOfSet);

/// <summary>The check state of a tree item whose children are checked in cascade.</summary>
public enum TreeCheckState
{
    /// <summary>Neither the item nor any descendant is checked.</summary>
    Unchecked,
    /// <summary>Some descendants are checked.</summary>
    Mixed,
    /// <summary>The item and all its descendants are checked.</summary>
    Checked,
}

/// <summary>
/// The structure of a hierarchy, independent of how it is drawn: roots and children given by selectors, the expanded
/// items, the flattened visible rows that a <see cref="Virtualizer{TItem}" /> can render, children loaded on demand, and
/// checking in cascade, where a parent is checked, unchecked or mixed according to its children. Trees, nested
/// navigation, property grids and grouped grids share it. Every member must be used from the UI thread.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class TreeModel<TItem> where TItem : notnull
{
    private readonly HashSet<TItem> expanded;
    private readonly HashSet<TItem> checkedLeaves;
    private readonly Dictionary<TItem, IReadOnlyList<TItem>> loaded;
    private readonly IEqualityComparer<TItem> comparer;

    /// <summary>Creates a tree model.</summary>
    /// <param name="comparer">The item comparer, or null for the default one.</param>
    public TreeModel(IEqualityComparer<TItem>? comparer = null)
    {
        this.comparer = comparer ?? EqualityComparer<TItem>.Default;
        expanded = new HashSet<TItem>(this.comparer);
        checkedLeaves = new HashSet<TItem>(this.comparer);
        loaded = new Dictionary<TItem, IReadOnlyList<TItem>>(this.comparer);
    }

    /// <summary>Gets or sets the top-level items.</summary>
    public IReadOnlyList<TItem> Roots { get; set; } = [];

    /// <summary>Gets or sets the function that gives an item's children. Return null for items that still need to load.</summary>
    public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }

    /// <summary>Gets or sets the function that tells whether an item has children before they are loaded.</summary>
    public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>Gets or sets the function that loads an item's children when it is first expanded.</summary>
    public Func<TItem, Task<IReadOnlyList<TItem>>>? LoadChildren { get; set; }

    /// <summary>Raised after the expansion, the loaded children or the check states change.</summary>
    public event Action? Changed;

    /// <summary>Gets the children of an item that are known, without loading.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The children, empty when unknown.</returns>
    public IReadOnlyList<TItem> ChildrenOf(TItem item) =>
        Children?.Invoke(item) ?? (loaded.TryGetValue(item, out var children) ? children : []);

    /// <summary>Gets whether an item can be expanded.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the item has or may have children.</returns>
    public bool CanExpand(TItem item) => HasChildren?.Invoke(item) ?? ChildrenOf(item).Count > 0;

    /// <summary>Gets whether an item is expanded.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true if the item is expanded.</returns>
    public bool IsExpanded(TItem item) => expanded.Contains(item);

    /// <summary>Gets whether an item's children are being loaded.</summary>
    /// <param name="item">The item.</param>
    /// <returns>true while loading.</returns>
    public bool IsLoading(TItem item) => loading.Contains(item);

    private readonly HashSet<TItem> loading = [];

    /// <summary>Expands or collapses an item, loading its children the first time when needed.</summary>
    /// <param name="item">The item.</param>
    /// <param name="value">Whether the item should be expanded.</param>
    /// <returns>A task that completes when the children are available.</returns>
    public async Task SetExpandedAsync(TItem item, bool value)
    {
        if (!value)
        {
            if (expanded.Remove(item)) Changed?.Invoke();
            return;
        }
        if (!CanExpand(item) || !expanded.Add(item)) return;
        Changed?.Invoke();
        if (Children?.Invoke(item) is not null || Children is null && loaded.ContainsKey(item) || LoadChildren is null || !loading.Add(item)) return;
        Changed?.Invoke();
        try { loaded[item] = await LoadChildren(item); }
        finally
        {
            loading.Remove(item);
            Changed?.Invoke();
        }
    }

    /// <summary>Expands every ancestor of an item so that it becomes visible.</summary>
    /// <param name="item">The item to reveal.</param>
    /// <returns>true if the item was found among the known items.</returns>
    public bool Reveal(TItem item)
    {
        var path = PathTo(item);
        if (path is null) return false;
        var changed = false;
        foreach (var ancestor in path.Take(path.Count - 1)) changed |= expanded.Add(ancestor);
        if (changed) Changed?.Invoke();
        return true;
    }

    /// <summary>Gets the chain of items from a root to an item, or null when it is not among the known items.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The path, ending with the item.</returns>
    public IReadOnlyList<TItem>? PathTo(TItem item)
    {
        var path = new List<TItem>();
        return Find(Roots, item, path) ? path : null;
    }

    private bool Find(IReadOnlyList<TItem> level, TItem target, List<TItem> path)
    {
        foreach (var candidate in level)
        {
            path.Add(candidate);
            if (comparer.Equals(candidate, target) || Find(ChildrenOf(candidate), target, path)) return true;
            path.RemoveAt(path.Count - 1);
        }
        return false;
    }

    /// <summary>Gets the parent of a visible or known item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The parent, or default for a root or an unknown item.</returns>
    public TItem? ParentOf(TItem item) => PathTo(item) is { Count: > 1 } path ? path[^2] : default;

    /// <summary>Flattens the visible rows: every root and the children of every expanded item, in display order.</summary>
    /// <returns>The visible rows.</returns>
    public IReadOnlyList<TreeRow<TItem>> VisibleRows()
    {
        var rows = new List<TreeRow<TItem>>();
        Flatten(Roots, 1, default, rows);
        return rows;
    }

    private void Flatten(IReadOnlyList<TItem> level, int depth, TItem? parent, List<TreeRow<TItem>> rows)
    {
        for (var i = 0; i < level.Count; i++)
        {
            var item = level[i];
            var open = expanded.Contains(item);
            rows.Add(new TreeRow<TItem>(item, depth, parent, CanExpand(item), open, i + 1, level.Count));
            if (open) Flatten(ChildrenOf(item), depth + 1, item, rows);
        }
    }

    /// <summary>Gets the check state of an item from its own state or, for a parent, from its descendants.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The check state.</returns>
    public TreeCheckState CheckState(TItem item)
    {
        var children = ChildrenOf(item);
        if (children.Count == 0) return checkedLeaves.Contains(item) ? TreeCheckState.Checked : TreeCheckState.Unchecked;
        var states = children.Select(CheckState).Distinct().ToList();
        return states.Count == 1 ? states[0] : TreeCheckState.Mixed;
    }

    /// <summary>Checks or unchecks an item and every known descendant.</summary>
    /// <param name="item">The item.</param>
    /// <param name="value">Whether the item should be checked.</param>
    public void SetChecked(TItem item, bool value)
    {
        SetCheckedCore(item, value);
        Changed?.Invoke();
    }

    private void SetCheckedCore(TItem item, bool value)
    {
        var children = ChildrenOf(item);
        if (children.Count == 0)
        {
            if (value) checkedLeaves.Add(item);
            else checkedLeaves.Remove(item);
            return;
        }
        foreach (var child in children) SetCheckedCore(child, value);
    }

    /// <summary>Gets the checked items without checked-state descendants to report, that is the checked leaves.</summary>
    /// <returns>The checked leaves.</returns>
    public IReadOnlyCollection<TItem> CheckedLeaves() => checkedLeaves;

    /// <summary>Collapses every item.</summary>
    public void CollapseAll()
    {
        if (expanded.Count == 0) return;
        expanded.Clear();
        Changed?.Invoke();
    }
}
