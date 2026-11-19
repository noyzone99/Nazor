using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout;

/// <summary>Distinguishes definite space from minimum-content and maximum-content constraints.</summary>
public enum AvailableKind : byte
{
    /// <summary>Provides a known logical size constraint.</summary>
    Definite,

    /// <summary>Requests the minimum intrinsic content size.</summary>
    MinContent,

    /// <summary>Requests the maximum intrinsic content size.</summary>
    MaxContent,
}

/// <summary>Space offered along an axis: a definite size, or an intrinsic sizing constraint.</summary>
/// <param name="Kind">The available kind represented by this value.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
public readonly record struct AvailableSpace(AvailableKind Kind, float Value)
{
    /// <summary>A minimum-content intrinsic size constraint.</summary>
    public static readonly AvailableSpace MinContent = new(AvailableKind.MinContent, 0);

    /// <summary>A maximum-content intrinsic size constraint.</summary>
    public static readonly AvailableSpace MaxContent = new(AvailableKind.MaxContent, float.PositiveInfinity);

    /// <summary>Creates available space with a definite logical size.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting AvailableSpace value.</returns>
    public static AvailableSpace Definite(float value) => new(AvailableKind.Definite, Math.Max(0, value));

    /// <summary>Gets whether the available space has a definite logical size.</summary>
    public bool IsDefinite => Kind == AvailableKind.Definite;

    /// <summary>
    /// Subtracts an amount from definite space, clamping at zero; intrinsic constraints remain unchanged.
    /// </summary>
    /// <param name="amount">The logical distance by which to grow, shrink, or offset the value.</param>
    /// <returns>The resulting AvailableSpace value.</returns>
    public AvailableSpace Shrink(float amount) => Kind == AvailableKind.Definite ? Definite(Value - amount) : this;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind == AvailableKind.Definite ? Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : Kind.ToString();
}

/// <summary>
/// Measures the content of a leaf (text, images): known content sizes are fixed by the parent, the available space
/// says how much room there is or which intrinsic size is wanted. Returns the size of the content box. It must be
/// pure: results are cached.
/// </summary>
/// <param name="knownWidth">The known width (float?).</param>
/// <param name="knownHeight">The known height (float?).</param>
/// <param name="availableWidth">The available width (AvailableSpace).</param>
/// <param name="availableHeight">The available height (AvailableSpace).</param>
/// <returns>The resulting Size value.</returns>
public delegate Size MeasureFunction(float? knownWidth, float? knownHeight, AvailableSpace availableWidth, AvailableSpace availableHeight);

/// <summary>Positions retained child border boxes inside measured content after the parent has a final layout box.</summary>
/// <param name="parent">The arranged parent; its absolute and snapped content boxes are available.</param>
/// <returns>One finite nonnegative-size frame per child, relative to the parent's border-box origin.</returns>
public delegate IReadOnlyList<Rect> ArrangeFunction(LayoutNode parent);

/// <summary>
/// Layout properties of a node, already resolved from the computed style (logical units). Compared by value:
/// assigning an equal style keeps cached measurements (paint-only changes).
/// </summary>
public sealed record LayoutStyle
{
    /// <summary>Gets or sets the layout mode used for the node.</summary>
    public Display Display { get; init; } = Display.Block;

    /// <summary>Gets the intrinsic table part: table, group, row, cell or caption; null uses ordinary layout.</summary>
    public string? TablePart { get; init; }

    /// <summary>Gets the requested table row span.</summary>
    public int TableRowSpan { get; init; } = 1;

    /// <summary>Gets the requested table column span.</summary>
    public int TableColumnSpan { get; init; } = 1;

    /// <summary>Gets or sets the main axis and ordering of flex items.</summary>
    public FlexDirection FlexDirection { get; init; } = FlexDirection.Row;

    /// <summary>Gets or sets whether flex items wrap into additional lines.</summary>
    public FlexWrap FlexWrap { get; init; } = FlexWrap.NoWrap;

    /// <summary>Gets or sets the distribution of free space along the main axis.</summary>
    public JustifyContent JustifyContent { get; init; } = JustifyContent.Normal;

    /// <summary>Gets or sets the distribution of wrapped flex lines or grid tracks along the cross axis.</summary>
    public AlignContent AlignContent { get; init; } = AlignContent.Normal;

    /// <summary>Gets or sets the default cross-axis alignment of child items.</summary>
    public AlignItems AlignItems { get; init; } = AlignItems.Stretch;

    /// <summary>Gets or sets the node's cross-axis alignment override.</summary>
    public AlignSelf AlignSelf { get; init; } = AlignSelf.Auto;

    /// <summary>Gets or sets the share of positive free space assigned to this flex item.</summary>
    public float FlexGrow { get; init; }

    /// <summary>Gets or sets the relative factor used to shrink this flex item when space is insufficient.</summary>
    public float FlexShrink { get; init; } = 1;

    /// <summary>Gets or sets the initial main-axis size used by the flex algorithm.</summary>
    public SizeValue FlexBasis { get; init; } = SizeValue.Auto;

    /// <summary>
    /// Position among the items of a flex container (CSS <c>order</c>): smaller first, ties in tree order.
    /// </summary>
    public int Order { get; init; }

    /// <summary>Gets or sets the preferred logical width.</summary>
    public SizeValue Width { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the preferred logical height.</summary>
    public SizeValue Height { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the minimum logical width.</summary>
    public SizeValue MinWidth { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the minimum logical height.</summary>
    public SizeValue MinHeight { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the maximum logical width.</summary>
    public SizeValue MaxWidth { get; init; } = SizeValue.None;

    /// <summary>Gets or sets the maximum logical height.</summary>
    public SizeValue MaxHeight { get; init; } = SizeValue.None;

    /// <summary>Gets or sets the left padding length.</summary>
    public Length PaddingLeft { get; init; }

    /// <summary>Gets or sets the top padding length.</summary>
    public Length PaddingTop { get; init; }

    /// <summary>Gets or sets the right padding length.</summary>
    public Length PaddingRight { get; init; }

    /// <summary>Gets or sets the bottom padding length.</summary>
    public Length PaddingBottom { get; init; }

    /// <summary>
    /// Physical space reserved by the host, outside the content flow and in addition to NSS padding.
    /// </summary>
    public Thickness ContentInsets { get; init; }

    /// <summary>Gets or sets reserved scrollbar space, included in resolved padding and excluded from the scroll viewport.</summary>
    public Thickness ScrollbarGutter { get; init; }

    /// <summary>Gets or sets the logical border widths on each edge.</summary>
    public Thickness Border { get; init; }

    /// <summary>Gets or sets the left margin, including automatic margin behavior.</summary>
    public SizeValue MarginLeft { get; init; } = SizeValue.Fixed(Length.Zero);

    /// <summary>Gets or sets the top margin, including automatic margin behavior.</summary>
    public SizeValue MarginTop { get; init; } = SizeValue.Fixed(Length.Zero);

    /// <summary>Gets or sets the right margin, including automatic margin behavior.</summary>
    public SizeValue MarginRight { get; init; } = SizeValue.Fixed(Length.Zero);

    /// <summary>Gets or sets the bottom margin, including automatic margin behavior.</summary>
    public SizeValue MarginBottom { get; init; } = SizeValue.Fixed(Length.Zero);

    /// <summary>Gets or sets the logical gap between adjacent rows or flex lines.</summary>
    public Length RowGap { get; init; }

    /// <summary>Gets or sets the logical gap between adjacent columns or flex items.</summary>
    public Length ColumnGap { get; init; }

    /// <summary>Gets or sets whether the specified size includes padding and borders.</summary>
    public BoxSizing BoxSizing { get; init; } = BoxSizing.BorderBox;

    /// <summary>Gets or sets the horizontal overflow policy.</summary>
    public Overflow OverflowX { get; init; } = Overflow.Visible;

    /// <summary>Gets or sets the vertical overflow policy.</summary>
    public Overflow OverflowY { get; init; } = Overflow.Visible;

    /// <summary>Gets or sets the preferred width-to-height ratio.</summary>
    public AspectRatio AspectRatio { get; init; } = AspectRatio.Auto;

    /// <summary>Gets or sets the static, relative, or absolute positioning mode.</summary>
    public Position Position { get; init; } = Position.Static;

    /// <summary>
    /// The box has a <c>transform</c>: like a positioned box, it is the containing block of absolute descendants.
    /// </summary>
    public bool Transformed { get; init; }

    /// <summary>
    /// A replaced element (an image): in block flow an auto width is its natural width, not the width of the
    /// container (CSS 2 §10.3.4); flex and grid stretch it as any item.
    /// </summary>
    public bool IsReplaced { get; init; }

    /// <summary>Gets or sets the top offset used by positioned layout.</summary>
    public SizeValue Top { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the right offset used by positioned layout.</summary>
    public SizeValue Right { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the bottom offset used by positioned layout.</summary>
    public SizeValue Bottom { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the left offset used by positioned layout.</summary>
    public SizeValue Left { get; init; } = SizeValue.Auto;

    /// <summary>Gets or sets the explicit grid column tracks.</summary>
    public GridTrackList GridTemplateColumns { get; init; } = GridTrackList.None;

    /// <summary>Gets or sets the explicit grid row tracks.</summary>
    public GridTrackList GridTemplateRows { get; init; } = GridTrackList.None;

    /// <summary>Gets or sets the named grid-area template.</summary>
    public GridAreas GridTemplateAreas { get; init; } = GridAreas.None;

    /// <summary>Gets or sets the sizes of implicitly created grid columns.</summary>
    public ValueList<TrackSize> GridAutoColumns { get; init; } = AutoTracks;

    /// <summary>Gets or sets the sizes of implicitly created grid rows.</summary>
    public ValueList<TrackSize> GridAutoRows { get; init; } = AutoTracks;

    /// <summary>Gets or sets the direction and packing mode used for grid auto-placement.</summary>
    public GridAutoFlow GridAutoFlow { get; init; } = GridAutoFlow.Row;

    /// <summary>Gets or sets the starting grid row line or span.</summary>
    public GridLine GridRowStart { get; init; } = GridLine.Auto;

    /// <summary>Gets or sets the ending grid row line or span.</summary>
    public GridLine GridRowEnd { get; init; } = GridLine.Auto;

    /// <summary>Gets or sets the starting grid column line or span.</summary>
    public GridLine GridColumnStart { get; init; } = GridLine.Auto;

    /// <summary>Gets or sets the ending grid column line or span.</summary>
    public GridLine GridColumnEnd { get; init; } = GridLine.Auto;

    /// <summary>Alignment of grid items in their area along the row axis (CSS <c>justify-items</c>).</summary>
    public AlignItems JustifyItems { get; init; } = AlignItems.Stretch;

    /// <summary>Gets or sets the node's inline-axis alignment within its grid area.</summary>
    public AlignSelf JustifySelf { get; init; } = AlignSelf.Auto;

    private static readonly ValueList<TrackSize> AutoTracks = new([TrackSize.Auto]);

    /// <summary>The default layout style used when no explicit style is supplied.</summary>
    public static readonly LayoutStyle Default = new();
}

/// <summary>
/// A node of the layout tree. After <see cref="LayoutEngine.Compute" />, <see cref="Frame" /> holds the border box
/// relative to the parent's border box, <see cref="AbsoluteFrame" /> the same box in window coordinates and
/// <see cref="RoundedFrame" /> its edges snapped to physical pixels.
/// </summary>
public sealed class LayoutNode
{
    private readonly List<LayoutNode> children = [];
    private LayoutStyle style = LayoutStyle.Default;
    private MeasureFunction? measure;
    internal readonly List<(CacheKey Key, Size Size)> MeasureCache = [];
    internal CacheKey? LastLayout;

    /// <summary>Gets or sets the style values used to lay out this node.</summary>
    public LayoutStyle Style
    {
        get => style;
        set
        {
            if (Equals(style, value)) return;
            style = value;
            MarkDirty();
        }
    }

    /// <summary>Gets or sets the function used to measure the node's intrinsic content.</summary>
    public MeasureFunction? Measure
    {
        get => measure;
        set
        {
            measure = value;
            MarkDirty();
        }
    }

    private ArrangeFunction? arrangeChildren;

    /// <summary>Gets or sets optional measured-content child arrangement. A measured node remains intrinsically measured while retaining child geometry for input and inspection.</summary>
    public ArrangeFunction? ArrangeChildren
    {
        get => arrangeChildren;
        set
        {
            if (ReferenceEquals(arrangeChildren, value)) return;
            arrangeChildren = value;
            MarkDirty();
        }
    }

    /// <summary>Gets the parent node, or null for a root node.</summary>
    public LayoutNode? Parent { get; private set; }

    /// <summary>Gets the ordered child nodes.</summary>
    public IReadOnlyList<LayoutNode> Children => children;

    /// <summary>The object the node lays out (an element, an anonymous text run).</summary>
    public object? Tag { get; set; }

    /// <summary>Gets the layout rectangle relative to the parent content area, in logical units.</summary>
    public Rect Frame { get; internal set; }

    /// <summary>Gets the resolved zero-based table row, excluding a caption.</summary>
    public int? TableRow { get; internal set; }

    /// <summary>Gets the resolved zero-based table column.</summary>
    public int? TableColumn { get; internal set; }

    /// <summary>Gets the resolved row span, constrained to the containing row group.</summary>
    public int TableRowSpan { get; internal set; } = 1;

    /// <summary>Gets the number of visible table rows.</summary>
    public int? TableRows { get; internal set; }

    /// <summary>Gets the number of visible table columns.</summary>
    public int? TableColumns { get; internal set; }

    /// <summary>Gets the layout rectangle in absolute logical viewport coordinates.</summary>
    public Rect AbsoluteFrame { get; internal set; }

    /// <summary>Gets the layout rectangle after pixel-grid rounding.</summary>
    public Rect RoundedFrame { get; internal set; }

    /// <summary>Gets resolved logical padding, including host content insets and reserved scrollbar space.</summary>
    public Thickness Padding { get; internal set; }

    /// <summary>Gets the resolved logical border width on each edge.</summary>
    public Thickness Border { get; internal set; }

    /// <summary>Gets the resolved logical margin on each edge.</summary>
    public Thickness Margin { get; internal set; }

    /// <summary>The content box in window coordinates, snapped like <see cref="RoundedFrame" />.</summary>
    public Rect ContentBox => RoundedFrame.Deflate(Padding + Border);

    /// <summary>
    /// Width of the content box before pixel snapping: text must break lines at the width layout measured, because
    /// snapping can make the box up to a pixel narrower.
    /// </summary>
    public float ContentWidth => Frame.Width - Padding.Left - Padding.Right - Border.Left - Border.Right;

    /// <summary>Gets whether this node requires layout recalculation.</summary>
    public bool IsDirty { get; private set; } = true;

    /// <summary>Appends a child layout node and invalidates the affected layout.</summary>
    /// <param name="child">The child layout node to insert or remove.</param>
    public void Add(LayoutNode child) => Insert(children.Count, child);

    /// <summary>Inserts a child layout node at the specified index and invalidates the affected layout.</summary>
    /// <param name="index">The zero-based collection position.</param>
    /// <param name="child">The child layout node to insert or remove.</param>
    public void Insert(int index, LayoutNode child)
    {
        child.Parent?.children.Remove(child);
        child.Parent = this;
        children.Insert(index, child);
        MarkDirty();
    }

    /// <summary>Removes a child layout node and invalidates the affected layout.</summary>
    /// <param name="child">The child layout node to insert or remove.</param>
    public void Remove(LayoutNode child)
    {
        if (!children.Remove(child)) return;
        child.Parent = null;
        MarkDirty();
    }

    /// <summary>Replaces the children only if the list differs.</summary>
    /// <param name="nodes">The nodes (IReadOnlyList&lt;LayoutNode&gt;).</param>
    public void SetChildren(IReadOnlyList<LayoutNode> nodes)
    {
        if (Same(children, nodes)) return;
        foreach (var child in children) child.Parent = null;
        children.Clear();
        foreach (var node in nodes)
        {
            node.Parent?.children.Remove(node);
            node.Parent = this;
            children.Add(node);
        }
        MarkDirty();

        static bool Same(List<LayoutNode> current, IReadOnlyList<LayoutNode> nodes)
        {
            if (current.Count != nodes.Count) return false;
            for (var i = 0; i < current.Count; i++)
                if (!ReferenceEquals(current[i], nodes[i])) return false;
            return true;
        }
    }

    /// <summary>Invalidates cached sizes of this node and its ancestors.</summary>
    public void MarkDirty()
    {
        for (var node = this; node is not null && !(node.IsDirty && node != this); node = node.Parent)
        {
            node.IsDirty = true;
            node.MeasureCache.Clear();
            node.LastLayout = null;
        }
    }

    internal void ClearDirty() => IsDirty = false;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Tag ?? "node"} {RoundedFrame}";
}

internal readonly record struct CacheKey(float? KnownWidth, float? KnownHeight, AvailableSpace AvailableWidth, AvailableSpace AvailableHeight, BoxModel Box,
    bool DefiniteWidth, bool DefiniteHeight);
