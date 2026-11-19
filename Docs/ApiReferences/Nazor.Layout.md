# Nazor.Layout

Block, flex, and grid layout contracts, available space, layout nodes, and measurement.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Types

- [`ArrangeFunction`](#type-arrangefunction)
- [`AvailableKind`](#type-availablekind)
- [`AvailableSpace`](#type-availablespace)
- [`LayoutEngine`](#type-layoutengine)
- [`LayoutNode`](#type-layoutnode)
- [`LayoutStyle`](#type-layoutstyle)
- [`MeasureFunction`](#type-measurefunction)

<a id="type-arrangefunction"></a>

## `ArrangeFunction`

Namespace: `Nazor.Layout`

```csharp
public delegate IReadOnlyList<Rect> ArrangeFunction(LayoutNode parent)
```

Positions retained child border boxes inside measured content after the parent has a final layout box. `parent` exposes its absolute and snapped content boxes. Returns one finite, nonnegative-size frame per child relative to the parent's border-box origin. Invalid count or geometry throws InvalidOperationException during layout. It supplies child geometry without invoking ordinary child stacking; use Measure for the parent's intrinsic size.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

<a id="type-availablekind"></a>

## `AvailableKind`

Namespace: `Nazor.Layout`

```csharp
public enum AvailableKind : byte
```

Distinguishes definite space from minimum-content and maximum-content constraints.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

### Members

#### `Definite`

```csharp
Definite
```

Provides a known logical size constraint.

#### `MinContent`

```csharp
MinContent
```

Requests the minimum intrinsic content size.

#### `MaxContent`

```csharp
MaxContent
```

Requests the maximum intrinsic content size.


<a id="type-availablespace"></a>

## `AvailableSpace`

Namespace: `Nazor.Layout`

```csharp
public readonly record struct AvailableSpace(AvailableKind Kind, float Value)
```

Space offered along an axis: a definite size, or an intrinsic sizing constraint.

| Parameter | Description |
| --- | --- |
| `Kind` | The available kind represented by this value. |
| `Value` | The numeric value or payload represented by this record. |

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

### Members

#### `MinContent`

```csharp
public static readonly AvailableSpace MinContent ;
```

A minimum-content intrinsic size constraint.

#### `MaxContent`

```csharp
public static readonly AvailableSpace MaxContent ;
```

A maximum-content intrinsic size constraint.

#### `Definite`

```csharp
public static AvailableSpace Definite(float value)
```

Creates available space with a definite logical size.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting AvailableSpace value.

#### `IsDefinite`

```csharp
public bool IsDefinite {get; }
```

Gets whether the available space has a definite logical size.

#### `Shrink`

```csharp
public AvailableSpace Shrink(float amount)
```

Subtracts an amount from definite space, clamping at zero; intrinsic constraints remain unchanged.

| Parameter | Description |
| --- | --- |
| `amount` | The logical distance by which to grow, shrink, or offset the value. |

Returns: The resulting AvailableSpace value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-layoutengine"></a>

## `LayoutEngine`

Namespace: `Nazor.Layout`

```csharp
public static partial class LayoutEngine
```

CSS Grid Layout Level 1, as summarised in Docs/Guide/20-Layout-and-Scrolling.md: the explicit grid with automatic repetitions (§7), line-based and automatic placement (§8), track sizing (§12) and the alignment of tracks and items (§10). Line names, subgrid and baselines are not supported; when an item spans several tracks, the extra space goes to them in equal shares.

[Source](../../Sources/Core/Nazor.Layout/GridLayout.cs)

### Members

#### `Compute`

```csharp
public static int Compute(LayoutNode root, Size viewport, float scale = 1)
```

Lays out `root` as the viewport and snaps edges to the pixel grid of `scale`. Returns how many node layouts or measurements were computed (not served from caches).

| Parameter | Description |
| --- | --- |
| `root` | The root (LayoutNode). |
| `viewport` | The viewport dimensions used to evaluate the media query. |
| `scale` | The number of physical pixels per logical unit. |

Returns: The resulting int value.


<a id="type-layoutnode"></a>

## `LayoutNode`

Namespace: `Nazor.Layout`

```csharp
public sealed class LayoutNode
```

A node of the layout tree. After `LayoutEngine.Compute`, `Frame` holds the border box relative to the parent's border box, `AbsoluteFrame` the same box in window coordinates and `RoundedFrame` its edges snapped to physical pixels.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L261)


### `Style`

```csharp
public LayoutStyle Style { get; set; }
```

Gets or sets the style values used to lay out this node.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L270)


### `Measure`

```csharp
public MeasureFunction? Measure { get; set; }
```

Gets or sets the function used to measure the node's intrinsic content.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L282)


### `ArrangeChildren`

```csharp
public ArrangeFunction? ArrangeChildren { get; set; }
```

Optional measured-content child arrangement. A measured node remains intrinsically measured while retaining child geometry for input and inspection. Assigning a different callback marks layout dirty. Null restores ordinary child layout. The callback runs with the final parent frame before child absolute/snapped frames are completed.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

### `Parent`

```csharp
public LayoutNode? Parent { get; private set; }
```

Gets the parent node, or null for a root node.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L307)


### `Children`

```csharp
public IReadOnlyList<LayoutNode> Children { get; }
```

Gets the ordered child nodes.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L310)


### `Tag`

```csharp
public object? Tag { get; set; }
```

The object the node lays out (an element, an anonymous text run).

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L313)


### `Frame`

```csharp
public Rect Frame { get; internal set; }
```

Gets the layout rectangle relative to the parent content area, in logical units.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L316)


### `TableRow`

```csharp
public int? TableRow { get; internal set; }
```

Gets the resolved zero-based table row, excluding a caption.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L319)


### `TableColumn`

```csharp
public int? TableColumn { get; internal set; }
```

Gets the resolved zero-based table column.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L322)


### `TableRowSpan`

```csharp
public int TableRowSpan { get; internal set; }
```

Gets the resolved row span, constrained to the containing row group.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L306)


### `TableRows`

```csharp
public int? TableRows { get; internal set; }
```

Gets the number of visible table rows.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L328)


### `TableColumns`

```csharp
public int? TableColumns { get; internal set; }
```

Gets the number of visible table columns.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L331)


### `AbsoluteFrame`

```csharp
public Rect AbsoluteFrame { get; internal set; }
```

Gets the layout rectangle in absolute logical viewport coordinates.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L334)


### `RoundedFrame`

```csharp
public Rect RoundedFrame { get; internal set; }
```

Gets the layout rectangle after pixel-grid rounding.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L337)


### `Padding`

```csharp
public Thickness Padding { get; internal set; }
```

Gets resolved logical padding, including host content insets and reserved scrollbar space.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L340)


### `Border`

```csharp
public Thickness Border { get; internal set; }
```

Gets the resolved logical border width on each edge.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L324)


### `Margin`

```csharp
public Thickness Margin { get; internal set; }
```

Gets the resolved logical margin on each edge.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L346)


### `ContentBox`

```csharp
public Rect ContentBox { get; }
```

The content box in window coordinates, snapped like `RoundedFrame`.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L349)


### `ContentWidth`

```csharp
public float ContentWidth { get; }
```

Width of the content box before pixel snapping: text must break lines at the width layout measured, because snapping can make the box up to a pixel narrower.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L355)


### `IsDirty`

```csharp
public bool IsDirty { get; private set; }
```

Gets whether this node requires layout recalculation.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L358)


### `Add`

```csharp
public void Add(LayoutNode child)
```

Appends a child layout node and invalidates the affected layout.

- `child`: The child layout node to insert or remove.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L362)


### `Insert`

```csharp
public void Insert(int index, LayoutNode child)
```

Inserts a child layout node at the specified index and invalidates the affected layout.

- `index`: The zero-based collection position.

- `child`: The child layout node to insert or remove.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L367)


### `Remove`

```csharp
public void Remove(LayoutNode child)
```

Removes a child layout node and invalidates the affected layout.

- `child`: The child layout node to insert or remove.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L377)


### `SetChildren`

```csharp
public void SetChildren(IReadOnlyList<LayoutNode> nodes)
```

Replaces the children only if the list differs.

- `nodes`: The nodes (IReadOnlyList<LayoutNode>).

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L386)


### `MarkDirty`

```csharp
public void MarkDirty()
```

Invalidates cached sizes of this node and its ancestors.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L409)


### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L404)


<a id="type-layoutstyle"></a>

## `LayoutStyle`

Namespace: `Nazor.Layout`

```csharp
public sealed record LayoutStyle
```

Layout properties of a node, already resolved from the computed style (logical units). Compared by value: assigning an equal style keeps cached measurements (paint-only changes).

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L71)


### `Display`

```csharp
public Display Display { get; init; }
```

Gets or sets the layout mode used for the node.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L74)


### `TablePart`

```csharp
public string? TablePart { get; init; }
```

Gets the intrinsic table part: table, group, row, cell or caption; null uses ordinary layout.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L77)


### `TableRowSpan`

```csharp
public int TableRowSpan { get; init; }
```

Gets the requested table row span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L75)


### `TableColumnSpan`

```csharp
public int TableColumnSpan { get; init; }
```

Gets the requested table column span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L83)


### `FlexDirection`

```csharp
public FlexDirection FlexDirection { get; init; }
```

Gets or sets the main axis and ordering of flex items.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L86)


### `FlexWrap`

```csharp
public FlexWrap FlexWrap { get; init; }
```

Gets or sets whether flex items wrap into additional lines.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L89)


### `JustifyContent`

```csharp
public JustifyContent JustifyContent { get; init; }
```

Gets or sets the distribution of free space along the main axis.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L92)


### `AlignContent`

```csharp
public AlignContent AlignContent { get; init; }
```

Gets or sets the distribution of wrapped flex lines or grid tracks along the cross axis.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L95)


### `AlignItems`

```csharp
public AlignItems AlignItems { get; init; }
```

Gets or sets the default cross-axis alignment of child items.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L98)


### `AlignSelf`

```csharp
public AlignSelf AlignSelf { get; init; }
```

Gets or sets the node's cross-axis alignment override.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L101)


### `FlexGrow`

```csharp
public float FlexGrow { get; init; }
```

Gets or sets the share of positive free space assigned to this flex item.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L104)


### `FlexShrink`

```csharp
public float FlexShrink { get; init; }
```

Gets or sets the relative factor used to shrink this flex item when space is insufficient.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L107)


### `FlexBasis`

```csharp
public SizeValue FlexBasis { get; init; }
```

Gets or sets the initial main-axis size used by the flex algorithm.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L110)


### `Order`

```csharp
public int Order { get; init; }
```

Position among the items of a flex container (CSS `order`): smaller first, ties in tree order.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L115)


### `Width`

```csharp
public SizeValue Width { get; init; }
```

Gets or sets the preferred logical width.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L118)


### `Height`

```csharp
public SizeValue Height { get; init; }
```

Gets or sets the preferred logical height.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L121)


### `MinWidth`

```csharp
public SizeValue MinWidth { get; init; }
```

Gets or sets the minimum logical width.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L124)


### `MinHeight`

```csharp
public SizeValue MinHeight { get; init; }
```

Gets or sets the minimum logical height.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L127)


### `MaxWidth`

```csharp
public SizeValue MaxWidth { get; init; }
```

Gets or sets the maximum logical width.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L130)


### `MaxHeight`

```csharp
public SizeValue MaxHeight { get; init; }
```

Gets or sets the maximum logical height.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L133)


### `PaddingLeft`

```csharp
public Length PaddingLeft { get; init; }
```

Gets or sets the left padding length.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L136)


### `PaddingTop`

```csharp
public Length PaddingTop { get; init; }
```

Gets or sets the top padding length.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L139)


### `PaddingRight`

```csharp
public Length PaddingRight { get; init; }
```

Gets or sets the right padding length.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L142)


### `PaddingBottom`

```csharp
public Length PaddingBottom { get; init; }
```

Gets or sets the bottom padding length.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L145)


### `ContentInsets`

```csharp
public Thickness ContentInsets { get; init; }
```

Physical space reserved by the host, outside the content flow and in addition to NSS padding.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L150)


### `ScrollbarGutter`

```csharp
public Thickness ScrollbarGutter { get; init; }
```

Gets or sets reserved scrollbar space, included in resolved padding and excluded from the scroll viewport.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L153)


### `Border`

```csharp
public Thickness Border { get; init; }
```

Gets or sets the logical border widths on each edge.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L151)


### `MarginLeft`

```csharp
public SizeValue MarginLeft { get; init; }
```

Gets or sets the left margin, including automatic margin behavior.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L159)


### `MarginTop`

```csharp
public SizeValue MarginTop { get; init; }
```

Gets or sets the top margin, including automatic margin behavior.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L162)


### `MarginRight`

```csharp
public SizeValue MarginRight { get; init; }
```

Gets or sets the right margin, including automatic margin behavior.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L165)


### `MarginBottom`

```csharp
public SizeValue MarginBottom { get; init; }
```

Gets or sets the bottom margin, including automatic margin behavior.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L168)


### `RowGap`

```csharp
public Length RowGap { get; init; }
```

Gets or sets the logical gap between adjacent rows or flex lines.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L171)


### `ColumnGap`

```csharp
public Length ColumnGap { get; init; }
```

Gets or sets the logical gap between adjacent columns or flex items.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L174)


### `BoxSizing`

```csharp
public BoxSizing BoxSizing { get; init; }
```

Gets or sets whether the specified size includes padding and borders.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L177)


### `OverflowX`

```csharp
public Overflow OverflowX { get; init; }
```

Gets or sets the horizontal overflow policy.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L180)


### `OverflowY`

```csharp
public Overflow OverflowY { get; init; }
```

Gets or sets the vertical overflow policy.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L183)


### `AspectRatio`

```csharp
public AspectRatio AspectRatio { get; init; }
```

Gets or sets the preferred width-to-height ratio.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L186)


### `Position`

```csharp
public Position Position { get; init; }
```

Gets or sets the static, relative, or absolute positioning mode.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L189)


### `Transformed`

```csharp
public bool Transformed { get; init; }
```

The box has a `transform`: like a positioned box, it is the containing block of absolute descendants.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L194)


### `IsReplaced`

```csharp
public bool IsReplaced { get; init; }
```

A replaced element (an image): in block flow an auto width is its natural width, not the width of the container (CSS 2 §10.3.4); flex and grid stretch it as any item.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L200)


### `Top`

```csharp
public SizeValue Top { get; init; }
```

Gets or sets the top offset used by positioned layout.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L203)


### `Right`

```csharp
public SizeValue Right { get; init; }
```

Gets or sets the right offset used by positioned layout.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L206)


### `Bottom`

```csharp
public SizeValue Bottom { get; init; }
```

Gets or sets the bottom offset used by positioned layout.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L209)


### `Left`

```csharp
public SizeValue Left { get; init; }
```

Gets or sets the left offset used by positioned layout.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L212)


### `GridTemplateColumns`

```csharp
public GridTrackList GridTemplateColumns { get; init; }
```

Gets or sets the explicit grid column tracks.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L215)


### `GridTemplateRows`

```csharp
public GridTrackList GridTemplateRows { get; init; }
```

Gets or sets the explicit grid row tracks.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L218)


### `GridTemplateAreas`

```csharp
public GridAreas GridTemplateAreas { get; init; }
```

Gets or sets the named grid-area template.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L221)


### `GridAutoColumns`

```csharp
public ValueList<TrackSize> GridAutoColumns { get; init; }
```

Gets or sets the sizes of implicitly created grid columns.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L224)


### `GridAutoRows`

```csharp
public ValueList<TrackSize> GridAutoRows { get; init; }
```

Gets or sets the sizes of implicitly created grid rows.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L227)


### `GridAutoFlow`

```csharp
public GridAutoFlow GridAutoFlow { get; init; }
```

Gets or sets the direction and packing mode used for grid auto-placement.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L230)


### `GridRowStart`

```csharp
public GridLine GridRowStart { get; init; }
```

Gets or sets the starting grid row line or span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L233)


### `GridRowEnd`

```csharp
public GridLine GridRowEnd { get; init; }
```

Gets or sets the ending grid row line or span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L236)


### `GridColumnStart`

```csharp
public GridLine GridColumnStart { get; init; }
```

Gets or sets the starting grid column line or span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L239)


### `GridColumnEnd`

```csharp
public GridLine GridColumnEnd { get; init; }
```

Gets or sets the ending grid column line or span.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L242)


### `JustifyItems`

```csharp
public AlignItems JustifyItems { get; init; }
```

Alignment of grid items in their area along the row axis (CSS `justify-items`).

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L245)


### `JustifySelf`

```csharp
public AlignSelf JustifySelf { get; init; }
```

Gets or sets the node's inline-axis alignment within its grid area.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L248)


### `Default`

```csharp
public static readonly LayoutStyle Default;
```

The default layout style used when no explicit style is supplied.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs#L253)


<a id="type-measurefunction"></a>

## `MeasureFunction`

Namespace: `Nazor.Layout`

```csharp
public delegate Size MeasureFunction(float? knownWidth, float? knownHeight, AvailableSpace availableWidth, AvailableSpace availableHeight);
```

Measures the content of a leaf (text, images): known content sizes are fixed by the parent, the available space says how much room there is or which intrinsic size is wanted. Returns the size of the content box. It must be pure: results are cached.

| Parameter | Description |
| --- | --- |
| `knownWidth` | The known width (float?). |
| `knownHeight` | The known height (float?). |
| `availableWidth` | The available width (AvailableSpace). |
| `availableHeight` | The available height (AvailableSpace). |

Returns: The resulting Size value.

[Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `AvailableKind`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AvailableKind` | Distinguishes definite space from minimum-content and maximum-content constraints. |
| `Definite = 0` | Provides a known logical size constraint. |
| `MinContent = 1` | Requests the minimum intrinsic content size. |
| `MaxContent = 2` | Requests the maximum intrinsic content size. |

### `AvailableSpace`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct AvailableSpace : IEquatable<AvailableSpace>` | Space offered along an axis: a definite size, or an intrinsic sizing constraint.<br>`Kind`. The available kind represented by this value.<br>`Value`. The numeric value or payload represented by this record. |
| `public AvailableSpace()` |  |
| `public AvailableSpace(AvailableKind Kind, float Value)` | Space offered along an axis: a definite size, or an intrinsic sizing constraint.<br>`Kind`. The available kind represented by this value.<br>`Value`. The numeric value or payload represented by this record. |
| `public static bool operator !=(AvailableSpace left, AvailableSpace right)` |  |
| `public static bool operator ==(AvailableSpace left, AvailableSpace right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(AvailableSpace other)` |  |
| `public void Deconstruct(out AvailableKind Kind, out float Value)` |  |
| `public AvailableKind Kind { get; init; }` | The available kind represented by this value. |
| `public float Value { get; init; }` | The numeric value or payload represented by this record. |

### `LayoutEngine`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/GridLayout.cs)

| Declaration | Contract |
| --- | --- |
| `public static class LayoutEngine` | CSS Grid Layout Level 1, as summarised in Docs/Guide/20-Layout-and-Scrolling.md: the explicit grid with automatic repetitions (§7), line-based and automatic placement (§8), track sizing (§12) and the alignment of tracks and items (§10). Line names, subgrid and baselines are not supported; when an item spans several tracks, the extra space goes to them in equal shares. |

### `LayoutNode`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `public LayoutNode()` |  |
| `public LayoutNode? Parent { get; }` | Gets the parent node, or null for a root node. |
| `public bool IsDirty { get; }` | Gets whether this node requires layout recalculation. |

### `LayoutStyle`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LayoutStyle : IEquatable<LayoutStyle>` | Layout properties of a node, already resolved from the computed style (logical units). Compared by value: assigning an equal style keeps cached measurements (paint-only changes). |
| `public override string ToString()` |  |
| `public static bool operator !=(LayoutStyle? left, LayoutStyle? right)` |  |
| `public static bool operator ==(LayoutStyle? left, LayoutStyle? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(LayoutStyle? other)` |  |
| `public LayoutStyle()` |  |

### `MeasureFunction`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `delegate Size MeasureFunction(float? knownWidth, float? knownHeight, AvailableSpace availableWidth, AvailableSpace availableHeight)` | Measures the content of a leaf (text, images): known content sizes are fixed by the parent, the available space says how much room there is or which intrinsic size is wanted. Returns the size of the content box. It must be pure: results are cached.<br>`knownWidth`. The known width (float?).<br>`knownHeight`. The known height (float?).<br>`availableWidth`. The available width (AvailableSpace).<br>`availableHeight`. The available height (AvailableSpace).<br>**Returns**. The resulting Size value. |

### `ArrangeFunction`

Namespace `Nazor.Layout`. [Source](../../Sources/Core/Nazor.Layout/LayoutNode.cs)

| Declaration | Contract |
| --- | --- |
| `delegate IReadOnlyList<Rect> ArrangeFunction(LayoutNode parent)` | Positions retained child border boxes relative to the arranged parent border box.<br>`parent`. The parent with final absolute and snapped content boxes.<br>**Returns**. One finite, nonnegative-size frame per child. |
