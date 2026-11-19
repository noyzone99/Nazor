# Nazor.Presentation

Scene composition, display-list construction, visual geometry, scrolling, and text presentation.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Retained surface motion

`Scene` implements `IOverlayMotion` explicitly and attaches that Core feature to overlay elements and elements marked `exit-presence=true`. It resolves NSS `overlay-exit-duration` from the normal surface style and observes both transition settings and current media preferences. A preference change notifies retained controls so an active exit can finish immediately. Removed elements and scene disposal clear the feature. Consumers must not retain the scene provider beyond their element lifetime. [Core contract](Nazor.Core.md#type-ioverlaymotion)

An anchored exiting surface whose concrete anchor is hidden or removed is no longer rendered or included in `Overlays`; native arrangement stops rather than centering the outgoing surface in the owner window. Native surfaces otherwise remain owned through retention.

## Compositor snapshots

`Scene.CanAnimateOnCompositor` reports whether current system-clock animation work consists solely of transform, transform-origin and opacity, without UI-driven scrolling work. `Scene.BuildCompositionList()` returns a display list with detached timelines in that case; manual clocks, mixed paint/layout animations and other motion use ordinary commands. Input hit testing and effective transform reads sample current compositor time. Existing `BuildDisplayList()` remains the ordinary, deterministically sampled drawing path.

[Source](../../Sources/Core/Nazor.Presentation/Composition.cs)

## Scene and content types

- [`LoadedImage`](#type-loadedimage)
- [`Scene`](#type-scene)
- [`ScrollBarGeometry`](#type-scrollbargeometry)
- [`ScrollState`](#type-scrollstate)
- [`TextEditState`](#type-texteditstate)
- [`TextSnapshot`](#type-textsnapshot)

<a id="type-loadedimage"></a>

## `LoadedImage`

Namespace: `Nazor.Presentation`

```csharp
public sealed record LoadedImage(DecodedImage Image, float Scale)
```

A decoded image and the display scale of the variant it came from.

| Parameter | Description |
| --- | --- |
| `Image` | The rendered or decoded image to process. |
| `Scale` | The scale (float). |

[Source](../../Sources/Core/Nazor.Presentation/ImageCache.cs)

### Layout transition geometry

Elements marked `layout-transition=true` receive the scene-owned `ILayoutTransition` provider. Scene samples their visible box before layout, then applies the configured NSS transform transition after natural relocation/resizing. Retargeting samples the current animated box, without an extra layout pass. Connected origins are one-use snapshots tied to the same scene; inverse ancestor transforms account for scrolling, translation and axis-aligned scale. Rotation/skew, singular transforms and cross-scene origins throw explicitly when motion is requested. Wrapper transforms are reserved by `LayoutTransition`; place authored transforms inside its content. Removing/disabling elements and disposing the scene detach features and geometry state. Reduced/suspended motion uses final layout immediately. Geometry transforms share normal hit testing, painting and eligible compositor sampling.

### Members

#### `NaturalSize`

```csharp
public Size NaturalSize {get; }
```

The size in logical units: "logo@2x.png" of 200 pixels is 100 wide.


<a id="type-scene"></a>

## `Scene`

Namespace: `Nazor.Presentation`

```csharp
public sealed class Scene : ILayoutTransition, IOverlayMotion, IDisposable
```

One drawable surface: keeps style, layout and drawing of the elements rendered by a component renderer in sync. `Update` resolves styles and lays out the tree for the viewport; `BuildDisplayList` produces the drawing commands; `HitTest(Point)` finds the element under a point. Text is measured and drawn through the same `TextLayout`.

[Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

### Members

#### `Scene`

```csharp
public Scene(Renderer renderer, IFontProvider fonts, ITextShaper shaper, StyleEngine? styles = null, IAnimationClock? clock = null, IImageDecoder? images = null)
```

Initializes a new instance of Scene.

| Parameter | Description |
| --- | --- |
| `renderer` | The renderer that owns the component tree. |
| `fonts` | The provider used to resolve font faces and fallback glyphs. |
| `shaper` | The service that converts text into positioned glyphs. |
| `styles` | The styles (StyleEngine?). |
| `clock` | The clock used to measure animation time. |
| `images` | The image decoder used for raster or vector image content. |

#### `Styles`

```csharp
public StyleEngine Styles { get; }
```

Gets the style engine used to compute element styles.

#### `Fonts`

```csharp
public IFontProvider Fonts {get; }
```

The fonts and shaper used by the scene, also used for diagnostic labels.

#### `TextShaper`

```csharp
public ITextShaper TextShaper {get; }
```

Gets the service used to shape text into positioned glyphs.

#### `Overlays`

```csharp
public IReadOnlyList<ElementNode> Overlays {get; }
```

The logical window's open overlays, the last one on top, including overlays presented on native popup surfaces.

#### `IsOverlay`

```csharp
public static bool IsOverlay(ElementNode element)
```

Gets whether the element is an open overlay laid out apart from its parent.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `ImageLoaded`

```csharp
public event Action? ImageLoaded;
```

Raised on a thread-pool thread when an image load ends: the host asks for a frame on the UI thread, where the next `Update` lays the image out and reports it to its element.

#### `WhenImagesLoaded`

```csharp
public Task WhenImagesLoaded()
```

Completes when the images that the elements are loading (Image, Icon, url() backgrounds) have loaded or failed.

Returns: A task that completes when the operation has finished.

#### `Transitions`

```csharp
public TransitionEngine Transitions { get; }
```

Transitions of the animatable values (colors, opacity): started by computed-style changes, sampled once per `BuildDisplayList`. The computed styles stay untouched.

#### `Viewport`

```csharp
public GraphicsSize Viewport { get; set;}
```

Viewport size in logical units.

#### `Scale`

```csharp
public float Scale { get; set;}
```

Physical pixels per logical unit (DPI / 96).

#### `Background`

```csharp
public GraphicsColor Background { get; set;}
```

Color drawn behind everything.

#### `RootLayout`

```csharp
public LayoutNode RootLayout {get; }
```

Gets the root node of the scene's physical layout tree.

#### `LastUpdate`

```csharp
public (int StylesRecomputed, int LayoutComputations) LastUpdate { get; private set;}
```

Work done by the last `Update`: styles recomputed and layouts computed.

#### `LastUpdateDuration`

```csharp
public (TimeSpan Styles, TimeSpan Layout) LastUpdateDuration { get; private set;}
```

How long the last `Update` took: the styles, then the rest (layout, scroll states).

#### `Update`

```csharp
public void Update()
```

Resolves styles and computes the layout of every element.

#### `Updated`

```csharp
public event Action? Updated;
```

Raised at the end of `Update`: styles and layout are current.

#### `IsRendered`

```csharp
public bool IsRendered(ElementNode element)
```

Whether an element is shown: it is in the tree, no ancestor-or-self has `display: none` and its own `visibility` is `visible`. Only shown elements take the focus.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `GetScrollBar`

```csharp
public ScrollBarGeometry? GetScrollBar(ElementNode element, bool horizontal)
```

Gets track and thumb bounds in the element's untransformed layout coordinates, plus current visibility. Returns null when the axis does not overflow, scrollbar visibility is `none`, or the configured geometry has no space. `horizontal` selects the axis. Apply `GetTransform(element)` for window coordinates.

#### `TimeUntilNextFrame`

```csharp
public TimeSpan? TimeUntilNextFrame { get; }
```

Combines style animation scheduling, scrollbar fade delays/progress, wheel-state expiry and held end-button repetition. Null means no frame is pending; zero means animation is moving now. Scene hosts should schedule using this property so scrollbar interaction continues without further input.

#### `NotifyWheelScroll`

```csharp
public void NotifyWheelScroll(ElementNode element)
```

Activates `:wheel-scrolling` on the receiving container and restarts its NSS idle timeout on the animation clock. Also applies at a scroll limit. `InputManager` calls this for unprevented, nonzero wheel input. `Update()` expires the state before resolving styles.

#### `PressScrollBarButton`

```csharp
public void PressScrollBarButton(ElementNode element, bool horizontal, bool decrease)
```

Scrolls one end-button step on the selected axis and starts the NSS repeat delay. Repetition runs only while the pointer stays over that button and the offset can advance. `InputManager` handles capture and scroll event dispatch.

#### `ReleaseScrollBarButton`

```csharp
public void ReleaseScrollBarButton()
```

Releases the pressed end button and cancels its repeat timer. `InputManager` calls it on release, cancellation and capture loss.

#### `UpdateScrollBarInteraction`

```csharp
public bool UpdateScrollBarInteraction(Point? point, ElementNode? target, ElementNode? dragged = null)
```

Updates autohide proximity from a window-coordinate pointer position and its hit-tested target. Null clears proximity. An optional dragged container keeps its scrollbars revealed. Call on the UI thread; true requests a new frame. `InputManager` calls this automatically for pointer movement, leave, capture changes and scene updates.

#### `GetScrollState`

```csharp
public ScrollState? GetScrollState(ElementNode element)
```

The scroll state of a scroll container, or null for other elements.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting ScrollState? value.

#### `FindScrollContainer`

```csharp
public ElementNode? FindScrollContainer(ElementNode? element)
```

The nearest ancestor-or-self that can scroll, without crossing the containing overlay surface.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting ElementNode? value.

#### `ScrollBy`

```csharp
public bool ScrollBy(ElementNode container, float deltaY)
```

Scrolls by `deltaY` logical units; returns false when already at the limit.

| Parameter | Description |
| --- | --- |
| `container` | The container (ElementNode). |
| `deltaY` | The vertical scroll delta in the specified unit. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `ScrollBy`

```csharp
public bool ScrollBy(ElementNode container, float deltaX, float deltaY)
public bool ScrollBy(ElementNode container, float deltaX, float deltaY, bool immediate)
```

Scrolls by `deltaX` and `deltaY` logical units; false when nothing moved.

| Parameter | Description |
| --- | --- |
| `container` | The container (ElementNode). |
| `deltaX` | The horizontal scroll delta in the specified unit. |
| `deltaY` | The vertical scroll delta in the specified unit. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `ScrollTo`

```csharp
public bool ScrollTo(ElementNode container, float? x, float? y)
public bool ScrollTo(ElementNode container, float? x, float? y, bool immediate)
```

Scrolls a container to an offset (null keeps that axis), within its limits; false when nothing moved.

| Parameter | Description |
| --- | --- |
| `container` | The container (ElementNode). |
| `x` | The x (float?). |
| `y` | The y (float?). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

`ScrollBy` and `ScrollTo` honor the container's inherited `scroll-behavior`, `scroll-duration` and `scroll-timing-function`. They return true when accepting a new destination, even before the first animated frame. `immediate: true` cancels interpolation and applies the offset directly. Fractional offsets are preserved. Reduced motion bypasses animation.

#### `StopScrolling`

```csharp
public void StopScrolling(ElementNode container)
```

Samples the pending animation at the current time and stops there. It does not snap to lines or wheel-step multiples. Thumb capture uses this before direct dragging.

#### `ScrollWheel`

```csharp
public bool ScrollWheel(ElementNode container, float deltaX, float deltaY,
    ScrollDeltaUnit unit, float systemDistance, bool systemByPage)
```

Converts wheel detents using `scroll-wheel-step` or the backend settings and requests scrolling. Signed positive deltas move towards the end on both axes. `LogicalPixels` bypasses detent scaling and interpolation. Returns true for a consumed request, including a configured zero step.

#### `TakeScrollChanges`

```csharp
public IReadOnlyList<ElementNode> TakeScrollChanges()
```

The scroll containers whose offset, viewport or content changed since the last call, by scrolling or by a layout: the input system tells them with `onscroll`.

Returns: The resulting IReadOnlyList<ElementNode> value.

#### `BringIntoView`

```csharp
public bool BringIntoView(ElementNode element)
```

Scrolls the containers around `element` so that it becomes visible, stopping at the containing overlay surface so that revealing popup content does not scroll its background.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `GetBounds`

```csharp
public Rect GetBounds(ElementNode element)
```

The element's border box in logical window coordinates, snapped to physical pixels: the layout box moved by the scroll offsets of the ancestors and, with transforms, the bounds of the transformed box (as `getBoundingClientRect()`).

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting Rect value.

#### `GetTransform`

```csharp
public Matrix GetTransform(ElementNode element)
```

From the element's own coordinates (those of its layout box, `GetContentBox`) to the window: its transform, then the scroll offsets and transforms of its ancestors.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting Matrix value.

#### `GetLayoutNode`

```csharp
public LayoutNode? GetLayoutNode(ElementNode element)
```

The layout box of an element (frame snapped to pixels, padding, border, margin) in its own coordinates, before scroll offsets and transforms; null when it is not laid out. For tools.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting LayoutNode? value.

#### `GetContentBox`

```csharp
public Rect GetContentBox(ElementNode element)
```

The content box of an element in its own coordinates, before scroll offsets and transforms (`GetTransform`).

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting Rect value.

#### `GetStyle`

```csharp
public ComputedStyle GetStyle(ElementNode element)
```

Gets the computed style associated with the element.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting ComputedStyle value.

#### `HitTest`

```csharp
public ElementNode? HitTest(Point point)
```

The deepest element containing `point`, honouring clipping and visibility.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: The resulting ElementNode? value.

#### `GetEffectiveValue`

```csharp
public T GetEffectiveValue<T>(ElementNode element, StyleProperty<T> property) where T : notnull
```

The value an element shows for an animatable property: the running transition's value at the current frame time, otherwise the computed value.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `property` | The style property to read or update. |

Returns: The resulting T value.

#### `BuildDisplayList`

```csharp
public DisplayList BuildDisplayList()
```

Drawing commands for the current layout, in paint order; samples the transitions once. What cannot be seen (outside the window, scrolled away or outside a clip) is not drawn.

Circular box decorations retain their original square geometry rather than independently pixel-rounded edges. This applies to backgrounds, borders, shadows and outlines of square boxes with uniform circular radii. Layout bounds, clipping, content and hit testing keep their existing rounding.

Returns: The resulting DisplayList value.

#### `DescribeLayout`

```csharp
public string DescribeLayout()
```

Indented element geometry, for tests: name, classes and snapped border box.

Returns: The resulting string value.

#### `GetEditableLayout`

```csharp
public TextLayout? GetEditableLayout(ElementNode element)
```

The layout of an editable element's displayed text (broken at its width when it wraps), or null.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting TextLayout? value.

#### `GetEditableLineOffset`

```csharp
public float GetEditableLineOffset(ElementNode element, TextLine line)
```

Gets the horizontal alignment offset of an editable text line inside its content box.

| Parameter | Description |
| --- | --- |
| `element` | The editable element. |
| `line` | A line from the element's current editable layout. |

Returns: The logical offset used by text painting, caret placement, selection and hit testing.

#### `GetContentBounds`

```csharp
public Rect GetContentBounds(ElementNode element)
```

The content box of an element in window coordinates, scroll offsets and transforms included (its bounds when transformed).

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting Rect value.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.


<a id="type-scrollbargeometry"></a>

## `ScrollBarGeometry`

Namespace: `Nazor.Presentation`

```csharp
public readonly record struct ScrollBarGeometry(Rect Track, Rect Thumb, bool IsVisible)
```

Scrollbar geometry in the element's untransformed layout coordinates. `Track` receives paging input; `Thumb` is the draggable region. `IsVisible` reports whether opacity is greater than zero, including autohide proximity, wheel input and active dragging. The additional `double Opacity { get; init; }` property is the sampled automatic fade multiplied by the effective `scrollbar-opacity`.

`Rect Bounds { get; init; }` covers the full bar including buttons. `Rect DecreaseButton { get; init; }` and `Rect IncreaseButton { get; init; }` contain the end-button bounds; they are empty when buttons are hidden. The track excludes both buttons.

[Source](../../Sources/Core/Nazor.Presentation/ScrollBars.cs)

<a id="type-scrollstate"></a>

## `ScrollState`

Namespace: `Nazor.Presentation`

```csharp
public sealed class ScrollState
```

Scroll position of an element whose `overflow-x` or `overflow-y` is `auto` or `scroll`. An axis that does not scroll has offset and maximum 0.

[Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

### Members

#### `OffsetX`

```csharp
public float OffsetX { get; internal set;}
```

Gets the horizontal offset in logical units.

#### `MaxOffsetX`

```csharp
public float MaxOffsetX { get; internal set;}
```

Largest horizontal offset: content width minus viewport width (0 when everything fits).

#### `ViewportWidth`

```csharp
public float ViewportWidth { get; internal set;}
```

Gets the logical width of the visible scroll viewport.

#### `ContentWidth`

```csharp
public float ContentWidth { get; internal set;}
```

Gets the logical width of the scrollable content.

#### `OffsetY`

```csharp
public float OffsetY { get; internal set;}
```

Gets the vertical offset in logical units.

#### `MaxOffsetY`

```csharp
public float MaxOffsetY { get; internal set;}
```

Largest offset: content height minus viewport height (0 when everything fits).

#### `ViewportHeight`

```csharp
public float ViewportHeight { get; internal set;}
```

Gets the logical height of the visible scroll viewport.

#### `ContentHeight`

```csharp
public float ContentHeight { get; internal set;}
```

Gets the logical height of the scrollable content.

#### `CanScrollX`

```csharp
public bool CanScrollX {get; }
```

Gets whether the content overflows the viewport horizontally.

#### `CanScrollY`

```csharp
public bool CanScrollY {get; }
```

Gets whether the content overflows the viewport vertically.

#### `CanScroll`

```csharp
public bool CanScroll {get; }
```

The element can scroll on at least one axis.


<a id="type-texteditstate"></a>

## `TextEditState`

Namespace: `Nazor.Presentation`

```csharp
public sealed class TextEditState
```

Editing state of a text input, owned by the input system and drawn by the scene: caret, selection (between `SelectionAnchor` and `CaretIndex`), the IME composition shown inline at the caret, and the horizontal scroll that keeps the caret visible. Indices are UTF-16 positions in `Value`.

[Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

### Members

#### `Value`

```csharp
public string Value { get; set;}
```

The value the editor last knew (from the element or produced by an edit).

#### `CaretIndex`

```csharp
public int CaretIndex { get; set;}
```

Gets or sets the UTF-16 caret offset in the text.

#### `SelectionAnchor`

```csharp
public int SelectionAnchor { get; set;}
```

Gets or sets the UTF-16 offset at which the text selection is anchored.

#### `Composition`

```csharp
public string? Composition { get; set;}
```

Preedit text of an IME composition, inserted at the caret while composing.

#### `CompositionCursor`

```csharp
public int CompositionCursor { get; set;}
```

Caret position inside `Composition`.

#### `ScrollX`

```csharp
public float ScrollX { get; set;}
```

Gets or sets the horizontal text scroll offset in logical units.

#### `ScrollY`

```csharp
public float ScrollY { get; set;}
```

The vertical scroll of a text box on more lines.

#### `DesiredX`

```csharp
public float? DesiredX { get; set;}
```

The x the caret keeps while it moves up and down between lines.

#### `SelectionStart`

```csharp
public int SelectionStart {get; }
```

Gets the smaller UTF-16 offset of the selection endpoints.

#### `SelectionEnd`

```csharp
public int SelectionEnd {get; }
```

Gets the larger UTF-16 offset of the selection endpoints.

#### `HasSelection`

```csharp
public bool HasSelection {get; }
```

Gets whether the text selection contains at least one character.

#### `DisplayCaret`

```csharp
public int DisplayCaret {get; }
```

The caret position in the displayed text (value plus composition).

#### `UndoStates`

```csharp
public List<TextSnapshot> UndoStates { get; }
```

The states before the last edits, the latest last (Ctrl+Z goes back to it).

#### `RedoStates`

```csharp
public List<TextSnapshot> RedoStates { get; }
```

The states undone, the latest last (Ctrl+Y goes forward to it).

#### `TypingEnd`

```csharp
public int? TypingEnd { get; set;}
```

Where the characters typed last end: typing there continues the same undo step.


<a id="type-textsnapshot"></a>

## `TextSnapshot`

Namespace: `Nazor.Presentation`

```csharp
public readonly record struct TextSnapshot(string Value, int CaretIndex, int SelectionAnchor)
```

A value of a text box with its caret and selection, kept for undo and redo.

| Parameter | Description |
| --- | --- |
| `Value` | The numeric value or payload represented by this record. |
| `CaretIndex` | The caret index (int). |
| `SelectionAnchor` | The selection anchor (int). |

[Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `LoadedImage`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/ImageCache.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LoadedImage : IEquatable<LoadedImage>` | A decoded image and the display scale of the variant it came from.<br>`Image`. The rendered or decoded image to process.<br>`Scale`. The scale (float). |
| `public LoadedImage(DecodedImage Image, float Scale)` | A decoded image and the display scale of the variant it came from.<br>`Image`. The rendered or decoded image to process.<br>`Scale`. The scale (float). |
| `public override string ToString()` |  |
| `public static bool operator !=(LoadedImage? left, LoadedImage? right)` |  |
| `public static bool operator ==(LoadedImage? left, LoadedImage? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(LoadedImage? other)` |  |
| `public void Deconstruct(out DecodedImage Image, out float Scale)` |  |
| `public DecodedImage Image { get; init; }` | The rendered or decoded image to process. |
| `public float Scale { get; init; }` | The scale (float). |

### `Scene`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/Composition.cs)

| Declaration | Contract |
| --- | --- |
| `public DisplayList BuildCompositionList()` | Builds a display list containing detached animation snapshots when all moving work is compositable.<br>**Returns**. A list retaining ordinary commands for manual clocks, layout animations and other UI-driven motion. |
| `public IReadOnlyList<(ElementNode Element, ResizeEventArgs Args)> TakeResizeChanges()` | The elements listening to `onresize` whose size or position within their parent changed since the last call, with logical layout values before pixel snapping: the input system tells them with `onresize`.<br>**Returns**. The changed elements and their new box. |
| `public bool ScrollWheel(ElementNode container, float deltaX, float deltaY, ScrollDeltaUnit unit, float systemDistance, bool systemByPage)` | Applies a wheel delta using the container's NSS step. Precise pixel input remains direct.<br>`container`. The scroll container.<br>`deltaX`. Signed horizontal distance or detents, positive towards the end.<br>`deltaY`. Signed vertical distance or detents, positive towards the end.<br>`unit`. Whether the deltas are pixels or wheel detents.<br>`systemDistance`. Backend distance per detent when scroll-wheel-step is auto.<br>`systemByPage`. Whether the system scrolls by pages instead of a fixed distance.<br>**Returns**. Whether the request was consumed, including a configured zero step. |
| `public bool CanAnimateOnCompositor { get; }` | Whether the current animations can run without further UI layout, paint or scrolling work. |
| `public Size Viewport { get; set; }` | Viewport size in logical units. |
| `public Color Background { get; set; }` | Color drawn behind everything. |
| `public (int StylesRecomputed, int LayoutComputations) LastUpdate { get; }` | Work done by the last `Nazor.Presentation.Scene.Update`: styles recomputed and layouts computed. |
| `public (TimeSpan Styles, TimeSpan Layout) LastUpdateDuration { get; }` | How long the last `Nazor.Presentation.Scene.Update` took: the styles, then the rest (layout, scroll states). |

### `ScrollBarGeometry`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/ScrollBars.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ScrollBarGeometry : IEquatable<ScrollBarGeometry>` | Scrollbar geometry in the element's untransformed layout coordinates.<br>`Track`. The track receiving pointer input.<br>`Thumb`. The draggable thumb.<br>`IsVisible`. Whether the scrollbar is currently painted. |
| `public ScrollBarGeometry()` |  |
| `public ScrollBarGeometry(Rect Track, Rect Thumb, bool IsVisible)` | Scrollbar geometry in the element's untransformed layout coordinates.<br>`Track`. The track receiving pointer input.<br>`Thumb`. The draggable thumb.<br>`IsVisible`. Whether the scrollbar is currently painted. |
| `public override string ToString()` |  |
| `public static bool operator !=(ScrollBarGeometry left, ScrollBarGeometry right)` |  |
| `public static bool operator ==(ScrollBarGeometry left, ScrollBarGeometry right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ScrollBarGeometry other)` |  |
| `public void Deconstruct(out Rect Track, out Rect Thumb, out bool IsVisible)` |  |
| `public Rect Track { get; init; }` | The track receiving pointer input. |
| `public Rect Thumb { get; init; }` | The draggable thumb. |
| `public bool IsVisible { get; init; }` | Whether the scrollbar is currently painted. |
| `public double Opacity { get; init; }` | Gets the sampled opacity, including automatic fade and the effective scrollbar-opacity value. |
| `public Rect Bounds { get; init; }` | Gets the full scrollbar bounds, including its end buttons. |
| `public Rect DecreaseButton { get; init; }` | Gets the decrease button bounds, or an empty rectangle when buttons are hidden. |
| `public Rect IncreaseButton { get; init; }` | Gets the increase button bounds, or an empty rectangle when buttons are hidden. |

### `ScrollState`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

| Declaration | Contract |
| --- | --- |
| `public ScrollState()` |  |

### `TextEditState`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

| Declaration | Contract |
| --- | --- |
| `public TextEditState()` |  |

### `TextSnapshot`

Namespace `Nazor.Presentation`. [Source](../../Sources/Core/Nazor.Presentation/Scene.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TextSnapshot : IEquatable<TextSnapshot>` | A value of a text box with its caret and selection, kept for undo and redo.<br>`Value`. The numeric value or payload represented by this record.<br>`CaretIndex`. The caret index (int).<br>`SelectionAnchor`. The selection anchor (int). |
| `public TextSnapshot()` |  |
| `public TextSnapshot(string Value, int CaretIndex, int SelectionAnchor)` | A value of a text box with its caret and selection, kept for undo and redo.<br>`Value`. The numeric value or payload represented by this record.<br>`CaretIndex`. The caret index (int).<br>`SelectionAnchor`. The selection anchor (int). |
| `public override string ToString()` |  |
| `public static bool operator !=(TextSnapshot left, TextSnapshot right)` |  |
| `public static bool operator ==(TextSnapshot left, TextSnapshot right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TextSnapshot other)` |  |
| `public void Deconstruct(out string Value, out int CaretIndex, out int SelectionAnchor)` |  |
| `public string Value { get; init; }` | The numeric value or payload represented by this record. |
| `public int CaretIndex { get; init; }` | The caret index (int). |
| `public int SelectionAnchor { get; init; }` | The selection anchor (int). |

Scene-clock numeric presenters sample current values before retargeting and request layout/paint frames only while active. Hidden/removed/disposed owners release numeric work; reduced motion snaps immediately. Numeric presentation prevents compositor-only export while its displayed content changes. See [numeric presentation](../Guide/23-Transitions-and-Animations.md#numeric-presentation).
