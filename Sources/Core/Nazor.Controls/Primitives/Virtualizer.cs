using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>An item rendered by a <see cref="Virtualizer{TItem}" />.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Index">Its position in the collection.</param>
public readonly record struct VirtualItem<TItem>(TItem Item, int Index);

/// <summary>The items currently realized by a <see cref="Virtualizer{TItem}" />.</summary>
/// <param name="First">The index of the first realized item.</param>
/// <param name="Last">The index after the last realized item.</param>
/// <param name="Count">The number of items in the collection.</param>
public readonly record struct VirtualRange(int First, int Last, int Count);

/// <summary>
/// A scrolling viewport that creates elements only for the items in view, so a list or a grid of any length stays fast.
/// Items have the size <see cref="ItemSize" />, or, with <see cref="MeasureItems" />, their measured size after layout
/// (the estimate is used until then). With <see cref="Columns" /> greater than one, items fill rows of a grid. The
/// viewport (<see cref="As" />, <c>Virtualizer</c> by default) needs an NSS size and <c>overflow</c>; each item is wrapped
/// in a <c>{name}Item</c> element whose position is set by spacers. Lists, trees, grids and long pickers are built on it.
/// Scroll notifications that keep the same realized range and repeated item measurements do not rebuild the rows.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class Virtualizer<TItem> : Control
{
    private PrefixSums sizes = new(0, 0);
    private IReadOnlyList<TItem> items = [];
    private double offset, viewport;
    private int first, last;
    private double estimate;
    private int columns;
    private readonly ViewportEvents viewportEvents = new();

    /// <summary>Gets or sets the element name of the viewport.</summary>
    [Parameter] public string As { get; set; } = "Virtualizer";

    /// <summary>Gets or sets the items.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets how each item is rendered.</summary>
    [Parameter, EditorRequired] public RenderFragment<VirtualItem<TItem>>? ItemTemplate { get; set; }

    /// <summary>Gets or sets nonvirtualized content rendered inside the scrolling root before the spacers and item wrappers, such as an absolute decorative indicator. It remains mounted across realized-range changes and empty lists. The caller owns its positioning and semantics; it is not counted or measured as an item.</summary>
    [Parameter] public RenderFragment? OverlayContent { get; set; }

    /// <summary>Gets or sets the size of an item (a row of a grid) along the scrolling axis, or its estimate when measured.</summary>
    [Parameter] public double ItemSize { get; set; } = 32;

    /// <summary>Gets or sets whether items are measured after layout, for items of different sizes.</summary>
    [Parameter] public bool MeasureItems { get; set; }

    /// <summary>Gets or sets the scrolling axis.</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gets or sets the number of items per row of a grid; one for a list.</summary>
    [Parameter] public int Columns { get; set; } = 1;

    /// <summary>Gets or sets how many rows beyond the viewport are kept realized on each side.</summary>
    [Parameter] public int Overscan { get; set; } = 3;

    /// <summary>Gets or sets how many rows are realized before the viewport size is known.</summary>
    [Parameter] public int InitialRows { get; set; } = 30;

    /// <summary>Gets or sets a function that gives each item a stable key; the index is used when null.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets the accessible role of the viewport, or null.</summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>Gets or sets the callback invoked when the realized range changes, for example to load more data near the end.</summary>
    [Parameter] public EventCallback<VirtualRange> OnRangeChanged { get; set; }

    /// <summary>Gets the realized range.</summary>
    public VirtualRange Range => new(first, last, items.Count);

    /// <summary>Gets the current scroll offset along the scrolling axis.</summary>
    public double ScrollOffset => offset;

    /// <summary>Gets the size of the viewport along the scrolling axis, or zero before the first layout.</summary>
    public double ViewportSize => viewport;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    private int Rows => (items.Count + columns - 1) / columns;

    private bool Vertical => Orientation == Orientation.Vertical;

    /// <summary>Validates the parameters and updates the realized range.</summary>
    protected override void OnParametersSet()
    {
        if (ItemTemplate is null) throw new InvalidOperationException("Virtualizer requires an ItemTemplate.");
        if (!double.IsFinite(ItemSize) || ItemSize <= 0) throw new ArgumentOutOfRangeException(nameof(ItemSize));
        if (Columns < 1) throw new ArgumentOutOfRangeException(nameof(Columns));
        if (Overscan < 0) throw new ArgumentOutOfRangeException(nameof(Overscan));
        items = Items ?? [];
        columns = Columns;
        var rows = Rows;
        if (sizes.Count != rows || estimate != ItemSize) sizes = sizes.Resize(rows, ItemSize, keepMeasured: MeasureItems && estimate == ItemSize);
        estimate = ItemSize;
        UpdateRange();
    }

    /// <summary>Adds the scroll listener and the role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(12, "__internal_onscroll#virtualizer", EventCallback.Factory.Create<ScrollEventArgs>(viewportEvents, ScrolledAsync));
    }

    /// <summary>Adds nonvirtualized overlay content followed by the spacers and realized items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (OverlayContent is not null) builder.AddContent(990, OverlayContent);
        var axis = Vertical ? "height" : "width";
        var firstRow = first / columns;
        var lastRow = (last + columns - 1) / columns;
        builder.OpenElement(1000, As + "Spacer");
        builder.AddAttribute(1001, "style", axis + ": " + Css.Px(sizes.Sum(firstRow)) + "; flex-shrink: 0");
        builder.AddAttribute(1002, "semantic-hidden", true);
        builder.CloseElement();
        for (var index = first; index < last; index++)
        {
            var item = items[index];
            var row = index / columns;
            builder.OpenElement(1010, As + "Item");
            builder.SetKey(ItemKey is { } key ? key(item) : index);
            builder.AddAttribute(1011, "index", index);
            builder.AddAttribute(1012, "item-position", new LogicalItemPosition(this, index + 1, items.Count));
            if (MeasureItems && index % columns == 0)
                builder.AddAttribute(1013, "__internal_onresize#virtualizer", EventCallback.Factory.Create<ResizeEventArgs>(viewportEvents, e => Measured(row, e)));
            else if (!MeasureItems) builder.AddAttribute(1014, "style", (columns == 1 ? axis : Vertical ? "height" : "width") + ": " + Css.Px(ItemSize));
            builder.AddContent(1015, ItemTemplate!(new VirtualItem<TItem>(item, index)));
            builder.CloseElement();
        }
        builder.OpenElement(1100, As + "Spacer");
        builder.AddAttribute(1101, "style", axis + ": " + Css.Px(sizes.Sum(sizes.Count) - sizes.Sum(lastRow)) + "; flex-shrink: 0");
        builder.AddAttribute(1102, "semantic-hidden", true);
        builder.CloseElement();
    }

    /// <summary>Scrolls so that an item is entirely visible, the least distance needed.</summary>
    /// <param name="index">The index of the item.</param>
    /// <returns>true if the viewport scrolled.</returns>
    public bool ScrollIntoView(int index)
    {
        if (index < 0 || index >= items.Count) return false;
        var row = index / columns;
        var start = sizes.Sum(row);
        var end = start + sizes[row];
        double? target = start < offset ? start : end > offset + viewport && viewport > 0 ? end - viewport : null;
        if (target is not { } value) return false;
        return Vertical ? Element.ScrollTo(null, value) : Element.ScrollTo(value, null);
    }

    /// <summary>Gets how many whole items fit in the viewport along the scrolling axis, at least one.</summary>
    /// <returns>The page size, for Page Up and Page Down.</returns>
    public int PageSize() => Math.Max(1, (int)(viewport / Math.Max(1, sizes.Count == 0 ? ItemSize : sizes.Sum(sizes.Count) / sizes.Count)) * columns - columns);

    private async Task ScrolledAsync(ScrollEventArgs e)
    {
        (offset, viewport) = Vertical ? (e.ScrollTop, e.ViewportHeight) : (e.ScrollLeft, e.ViewportWidth);
        if (UpdateRange())
        {
            StateHasChanged();
            await OnRangeChanged.InvokeAsync(Range);
        }
    }

    private void Measured(int row, ResizeEventArgs e)
    {
        var size = Vertical ? e.Height : e.Width;
        if (row >= sizes.Count || size <= 0 || Math.Abs(sizes[row] - size) < 0.5) return;
        sizes.Set(row, size);
        UpdateRange();
        StateHasChanged();
    }

    private bool UpdateRange()
    {
        var rows = Rows;
        int firstRow, lastRow;
        if (viewport <= 0)
        {
            firstRow = 0;
            lastRow = Math.Min(rows, InitialRows);
        }
        else
        {
            firstRow = Math.Max(0, sizes.IndexAt(offset) - Overscan);
            lastRow = Math.Min(rows, sizes.IndexAt(offset + viewport) + 1 + Overscan);
        }
        var (from, to) = (Math.Min(items.Count, firstRow * columns), Math.Min(items.Count, lastRow * columns));
        if ((from, to) == (first, last)) return false;
        (first, last) = (from, to);
        return true;
    }

    private sealed class ViewportEvents : IHandleEvent
    {
        public Task HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
    }

    private sealed class PrefixSums
    {
        private readonly double[] values;
        private readonly double[] tree;
        private readonly bool[] measured;

        public PrefixSums(int count, double size)
        {
            values = new double[count];
            tree = new double[count + 1];
            measured = new bool[count];
            for (var i = 0; i < count; i++) Add(i, values[i] = size);
        }

        public int Count => values.Length;

        public double this[int index] => values[index];

        public PrefixSums Resize(int count, double size, bool keepMeasured)
        {
            var next = new PrefixSums(count, size);
            if (!keepMeasured) return next;
            for (var i = 0; i < Math.Min(count, Count); i++)
                if (measured[i]) next.Set(i, values[i]);
            return next;
        }

        public void Set(int index, double value)
        {
            Add(index, value - values[index]);
            values[index] = value;
            measured[index] = true;
        }

        public double Sum(int count)
        {
            var sum = 0.0;
            for (var i = Math.Min(count, values.Length); i > 0; i -= i & -i) sum += tree[i];
            return sum;
        }

        public int IndexAt(double position)
        {
            if (values.Length == 0 || position <= 0) return 0;
            var index = 0;
            var step = 1;
            while (step * 2 <= values.Length) step *= 2;
            for (; step > 0; step /= 2)
                if (index + step <= values.Length && tree[index + step] <= position)
                {
                    index += step;
                    position -= tree[index];
                }
            return Math.Min(index, values.Length - 1);
        }

        private void Add(int index, double delta)
        {
            for (var i = index + 1; i < tree.Length; i += i & -i) tree[i] += delta;
        }
    }
}
