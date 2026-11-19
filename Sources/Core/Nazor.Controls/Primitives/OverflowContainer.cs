using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// A row of items that hands over to an overflow control the items that do not fit: the visible items are rendered
/// with <see cref="ItemTemplate" />, the hidden ones are passed to <see cref="OverflowTemplate" />, typically a "More"
/// menu button. Items with a lower <see cref="Priority" /> leave first; equal priorities leave from the end. Widths are
/// measured after layout, so the row adapts as the window or the items change. Toolbars, tab strips, breadcrumbs and
/// chip and avatar groups are built on it. The element is named by <see cref="As" />; each item is wrapped in a
/// <c>{name}Item</c> element.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class OverflowContainer<TItem> : Control
{
    private readonly Dictionary<int, double> widths = [];
    private double available = double.PositiveInfinity;
    private double overflowWidth;
    private HashSet<int> hidden = [];

    /// <summary>Gets or sets the element name, <c>OverflowContainer</c> by default.</summary>
    [Parameter] public string As { get; set; } = "OverflowContainer";

    /// <summary>Gets or sets the items.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets how a visible item is rendered.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Gets or sets how the hidden items are offered, usually a menu button; rendered only when some are hidden.</summary>
    [Parameter, EditorRequired] public RenderFragment<IReadOnlyList<TItem>>? OverflowTemplate { get; set; }

    /// <summary>Gets or sets the priority of an item; lower priorities leave first.</summary>
    [Parameter] public Func<TItem, int>? Priority { get; set; }

    /// <summary>Gets or sets the space between items, in logical units, counted when fitting.</summary>
    [Parameter] public double Gap { get; set; }

    /// <summary>Gets or sets the accessible role of the row, or null.</summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// Gets or sets extra attributes of the <c>{name}Item</c> wrapper of an item, such as the listener of an
    /// <see cref="IndicatorTracker{TKey}" /> that measures the item within the row.
    /// </summary>
    [Parameter] public Func<TItem, IEnumerable<KeyValuePair<string, object?>>?>? ItemAttributes { get; set; }

    /// <summary>Gets or sets content rendered inside the row after the items, such as an <see cref="Indicator" />.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets the items currently hidden, in their original order.</summary>
    public IReadOnlyList<TItem> HiddenItems => [.. Items.Where((_, i) => hidden.Contains(i))];

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Recomputes which items fit.</summary>
    protected override void OnParametersSet()
    {
        if (ItemTemplate is null || OverflowTemplate is null) throw new InvalidOperationException("OverflowContainer requires ItemTemplate and OverflowTemplate.");
        if (!double.IsFinite(Gap) || Gap < 0) throw new ArgumentOutOfRangeException(nameof(Gap));
        foreach (var stale in widths.Keys.Where(key => key >= Items.Count).ToList()) widths.Remove(stale);
        hidden = Fit();
    }

    /// <summary>Adds the role and the width listener.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "overflowing", hidden.Count > 0);
        builder.AddAttribute(12, "__internal_onresize#overflow", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Update(() => available = e.Width)));
    }

    /// <summary>Adds the visible items and the overflow control.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (hidden.Contains(i)) continue;
            var index = i;
            builder.OpenElement(1000, As + "Item");
            builder.SetKey(index);
            builder.AddAttribute(1001, "__internal_onresize#overflow", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Update(() => widths[index] = e.Width)));
            builder.AddMultipleAttributes(1002, ItemAttributes?.Invoke(Items[index]));
            builder.AddContent(1003, ItemTemplate!(Items[index]));
            builder.CloseElement();
        }
        builder.AddContent(1050, ChildContent);
        if (hidden.Count == 0) return;
        builder.OpenElement(1100, As + "Overflow");
        builder.AddAttribute(1101, "__internal_onresize#overflow", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Update(() => overflowWidth = e.Width)));
        builder.AddContent(1102, OverflowTemplate!(HiddenItems));
        builder.CloseElement();
    }

    private void Update(Action measure)
    {
        measure();
        var next = Fit();
        if (next.SetEquals(hidden)) return;
        hidden = next;
        StateHasChanged();
    }

    private HashSet<int> Fit()
    {
        if (double.IsPositiveInfinity(available) || Items.Count == 0) return [];
        double Width(int index) => widths.TryGetValue(index, out var width) ? width : 0;
        var total = Enumerable.Range(0, Items.Count).Sum(Width) + Gap * Math.Max(0, Items.Count - 1);
        if (total <= available + 0.5) return [];
        var order = Enumerable.Range(0, Items.Count)
            .OrderBy(i => Priority?.Invoke(Items[i]) ?? 0).ThenByDescending(i => i).ToList();
        var result = new HashSet<int>();
        var reserve = overflowWidth > 0 ? overflowWidth + Gap : 32;
        foreach (var index in order)
        {
            if (total + reserve <= available + 0.5) break;
            result.Add(index);
            total -= Width(index) + Gap;
        }
        return result;
    }
}
