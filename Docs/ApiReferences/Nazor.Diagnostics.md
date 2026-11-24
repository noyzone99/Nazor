# Nazor.Diagnostics

Explicit/headless inspection primitives. Construct `DevToolsSession(renderer, scene, input)` with a direct reference to this package. Application-window attachment is supplied separately by [Nazor.DevTools.Agent](Nazor.DevTools.Agent.md); `Nazor.Application` and ordinary desktop hosting do not reference Diagnostics. See [window inspection](../Guide/27-Testing-and-Diagnostics.md#window-inspection).

Runtime inspection, diagnostic node snapshots, frame measurements, and element highlighting.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Rendering metrics

`FrameInfo` optionally retains `RenderingInfo? Rendering`, `long? RenderWorkerFrame`, `TimeSpan? RenderWorkerDuration` and `long? GpuFramesReplaced`. `DevToolsSession.RecordFrame` accepts matching optional trailing parameters `rendering`, `renderWorkerFrame`, `renderWorkerDuration` and `gpuFramesReplaced`; existing callers may omit them. GPU raster slots describe UI recording/submission. Worker time describes the independently numbered completed worker frame, including CPU submission/presentation wait and excluding GPU execution timestamps.

## Diagnostic types

- [`ComponentNodeInfo`, `StructuralNodeInfo`, `DiagnosticTreeSnapshot`, `TreeCaptureLimitException`](#detached-tree-types)
- [`BoxModel`](#type-boxmodel)
- [`DevToolsSession`](#type-devtoolssession)
- [`FrameCause`](#type-framecause)
- [`FrameInfo`](#type-frameinfo)
- [`NodeInfo`](#type-nodeinfo)
- [`NodeKind`](#type-nodekind)
- [`TreeChange`](#type-treechange)

<a id="type-boxmodel"></a>

## `BoxModel`

Namespace: `Nazor.Diagnostics`

```csharp
public sealed record BoxModel(Rect MarginBox, Rect BorderBox, Rect PaddingBox, Rect ContentBox, Thickness Margin, Thickness Border, Thickness Padding, float Scale)
```

The boxes of an element as the box model of developer tools shows them, in logical units of the window, from the margin box out to the content box in, with the widths of each ring. With a transform each box is the bounds of the transformed one; scroll offsets are applied, as on the screen. `Physical` gives a box in pixels.

| Parameter | Description |
| --- | --- |
| `MarginBox` | The margin box (Rect). |
| `BorderBox` | The border box (Rect). |
| `PaddingBox` | The padding box (Rect). |
| `ContentBox` | The content box (Rect). |
| `Margin` | The margin (Thickness). |
| `Border` | The border (Thickness). |
| `Padding` | The padding (Thickness). |
| `Scale` | The scale (float). |

[Source](../../Sources/Core/Nazor.Diagnostics/BoxModel.cs)

### Members

#### `Physical`

```csharp
public Rect Physical(Rect box)
```

A box of this model in physical pixels.

| Parameter | Description |
| --- | --- |
| `box` | The box (Rect). |

Returns: The resulting Rect value.


<a id="type-devtoolssession"></a>

## `DevToolsSession`

Namespace: `Nazor.Diagnostics`

```csharp
public sealed partial class DevToolsSession : IDisposable
```

Exposes a window's component and element trees to developer tools as plain data with stable IDs. It is built on the renderer, the scene and the input of the window, runs on the UI thread, and costs nothing to a window until it is created. A node gets its id the first time the session meets it and keeps it while it lives; a removed node loses it, and ids are never reused.

[Source](../../Sources/Core/Nazor.Diagnostics/DevToolsSession.cs)

### Members

#### `DevToolsSession`

```csharp
public DevToolsSession(Renderer renderer, Scene scene, InputManager input)
```

Inspects a live window's component tree, visual state, and input selection.

| Parameter | Description |
| --- | --- |
| `renderer` | The renderer that owns the component tree. |
| `scene` | The scene used for layout, hit testing, and drawing. |
| `input` | The input manager associated with the scene. |

#### `Renderer`

```csharp
public Renderer Renderer { get; }
```

Gets the renderer that owns the component tree.

#### `Scene`

```csharp
public Scene Scene { get; }
```

Gets the scene that provides layout, hit testing, and drawing for the window.

#### `Input`

```csharp
public InputManager Input { get; }
```

Gets the input manager associated with this window.

#### `Roots`

```csharp
public IReadOnlyList<int> Roots { get ; }
```

The top-level nodes of every root component of the window (the window, the tooltip…), in order.

#### `TreeChanged`

```csharp
public event Action<TreeChange>? TreeChanged;
```

Nodes were added, removed or changed by a batch of renders.

#### `StatesChanged`

```csharp
public event Action? StatesChanged;
```

States of elements (hover, focus, pressed…) may have changed.

#### `IdOf`

```csharp
public int IdOf(Node node)
```

The id of a node of the tree, given now if the session has not met it yet.

| Parameter | Description |
| --- | --- |
| `node` | The JSON node or retained tree node to inspect. |

Returns: The resulting int value.

#### `NodeOf`

```csharp
public Node? NodeOf(int id)
```

The node with an id; null when it was removed or the id was never given.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the node. |

Returns: The resulting Node? value.

#### `ElementOf`

```csharp
public ElementNode? ElementOf(int id)
```

The element with an id; null for a text, a removed node or an unknown id.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the node. |

Returns: The resulting ElementNode? value.

#### `Get`

```csharp
public NodeInfo? Get(int id)
```

The physical node with an id as data; null for a component, a removed node or an unknown id. Unknown attribute/key objects are displayed as type placeholders without calling user formatting code.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the node. |

Returns: The resulting NodeInfo? value.

#### `Trace`

```csharp
public Nazor.Style.StyleTrace? Trace(int id)
```

How the style of an element came to be: its rules in cascade order with the declarations that win, and where every value comes from; null for a text or a node that is not in the tree.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the element to trace. |

Returns: The resulting Nazor.Style.StyleTrace? value.

#### `Box`

```csharp
public BoxModel? Box(int id)
```

The boxes of an element where the window shows them; null for a text, a node that is not in the tree, or an element that is not displayed.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the element to inspect. |

Returns: The resulting BoxModel? value.

#### `TypeName`

```csharp
public static string TypeName(Type type)
```

A type as code writes it: `ListView<String>`, without its namespace.

| Parameter | Description |
| --- | --- |
| `type` | The type (Type). |

Returns: The resulting string value.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.

#### `Frames`

```csharp
public IReadOnlyList<FrameInfo> Frames {get; }
```

The last 120 frames, oldest first. Reading them never asks the app for a frame.

#### `FrameRecorded`

```csharp
public event Action<FrameInfo>? FrameRecorded;
```

Occurs when frame recorded is reported.

#### `RecordFrame`

```csharp
public void RecordFrame(int number, FrameCause cause, TimeSpan total, TimeSpan styles, TimeSpan layout,
    TimeSpan displayList, TimeSpan overlay, TimeSpan raster, int stylesRecomputed, int layoutComputations, IReadOnlyList<PixelRect>? damage,
    RenderingInfo? rendering = null, long? renderWorkerFrame = null, TimeSpan? renderWorkerDuration = null, long? gpuFramesReplaced = null)
```

Records completed UI paint work before backend presentation completes. Optional GPU metrics describe the last completed worker frame.

| Parameter | Description |
| --- | --- |
| `number` | The number (int). |
| `cause` | The reason this frame was requested, recorded by diagnostics. |
| `total` | The total (TimeSpan). |
| `styles` | The styles (TimeSpan). |
| `layout` | The layout (TimeSpan). |
| `displayList` | Application display-list construction time. |
| `overlay` | The overlay (TimeSpan). |
| `raster` | Software rasterization or GPU recording and submission time on the UI thread. |
| `stylesRecomputed` | The styles recomputed (int). |
| `layoutComputations` | The layout computations (int). |
| `damage` | The damage (IReadOnlyList<PixelRect>?). |
| `rendering` | The requested and effective renderer, when available. |
| `renderWorkerFrame` | The last completed GPU worker frame number. |
| `renderWorkerDuration` | CPU submission plus presentation wait for that worker frame. |
| `gpuFramesReplaced` | The cumulative number of pending GPU frames replaced before rendering. |

#### `IsInspecting`

```csharp
public bool IsInspecting { get; private set;}
```

Gets whether pointer-based diagnostic inspection is active.

#### `HighlightedId`

```csharp
public int? HighlightedId { get; private set;}
```

Gets the highlighted node's diagnostic identifier, or null when no node is highlighted.

#### `SelectedId`

```csharp
public int? SelectedId { get; private set;}
```

Gets the selected node's diagnostic identifier, or null when no node is selected.

#### `OverlayChanged`

```csharp
public event Action? OverlayChanged;
```

The host requests one frame when the highlight or inspection mode changes.

#### `SelectionChanged`

```csharp
public event Action<int?>? SelectionChanged;
```

Occurs when selection changes.

#### `Highlight`

```csharp
public void Highlight(int? id)
```

Highlights the node with the specified diagnostic identifier, or clears the highlight for null. A component highlights the boxes of its top-level rendered elements; a text has no box and clears the highlight.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the node, or null to clear the current target. |

#### `Select`

```csharp
public void Select(int? id)
```

Selects the node with the specified diagnostic identifier, or clears the selection for null.

| Parameter | Description |
| --- | --- |
| `id` | The diagnostic identifier of the node, or null to clear the current target. |

#### `SetInspectMode`

```csharp
public void SetInspectMode(bool enabled)
```

Enables or disables pointer-based element inspection.

| Parameter | Description |
| --- | --- |
| `enabled` | Whether the feature or interaction is enabled. |

#### `PointerMoved`

```csharp
public bool PointerMoved(Point point)
```

True when inspection consumed this input; the application must not receive it.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `PointerPressed`

```csharp
public bool PointerPressed(Point point, PointerButton button)
```

Processes the pointer pressed notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `button` | The pointer button associated with the event. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `PointerReleased`

```csharp
public bool PointerReleased(Point point, PointerButton button)
```

Processes the pointer released notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `button` | The pointer button associated with the event. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `PointerLeft`

```csharp
public bool PointerLeft()
```

Processes the pointer left notification in logical viewport coordinates.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `CancelInspection`

```csharp
public void CancelInspection()
```

Leaves inspect mode and clears the highlighted element.

#### `KeyDown`

```csharp
public bool KeyDown(string key)
```

Processes an inspector key press and reports whether the inspector consumed it.

| Parameter | Description |
| --- | --- |
| `key` | The normalized name of the key. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `KeyUp`

```csharp
public bool KeyUp(string key)
```

Processes a released key and reports whether it was handled.

| Parameter | Description |
| --- | --- |
| `key` | The normalized name of the key. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Compose`

```csharp
public DisplayList Compose(DisplayList list)
```

Adds the box model above the application's drawings, after the scene has been updated. Its commands participate in damage tracking so moving or removing it restores the old area.

| Parameter | Description |
| --- | --- |
| `list` | The list (DisplayList). |

Returns: The resulting DisplayList value.


<a id="type-framecause"></a>

## `FrameCause`

Namespace: `Nazor.Diagnostics`

```csharp
[Flags] public enum FrameCause
```

Identifies why a frame was requested; values can be combined.

[Source](../../Sources/Core/Nazor.Diagnostics/FrameInfo.cs)

### Members

#### `None`

```csharp
None = 0
```

No frame cause is recorded.

#### `Paint`

```csharp
Paint = 1
```

The platform requested painting.

#### `RenderBatch`

```csharp
RenderBatch = 2
```

A component render batch changed the tree.

#### `Interaction`

```csharp
Interaction = 4
```

Pointer, keyboard, or focus interaction changed visual state.

#### `Resize`

```csharp
Resize = 8
```

The viewport size or scale changed.

#### `Animation`

```csharp
Animation = 16
```

An animation or transition requested another frame.

#### `HotReload`

```csharp
HotReload = 32
```

A hot reload update changed code or styles.

#### `DevTools`

```csharp
DevTools = 64
```

The diagnostics overlay changed.

#### `Image`

```csharp
Image = 128
```

Image loading changed the visual content.

#### `SystemSettings`

```csharp
SystemSettings = 256
```

A system preference or appearance setting changed.

#### `Explicit`

```csharp
Explicit = 512
```

Application code explicitly requested a frame.


<a id="type-frameinfo"></a>

## `FrameInfo`

Namespace: `Nazor.Diagnostics`

```csharp
public sealed record FrameInfo(int Number, TimeSpan Elapsed, FrameCause Cause, TimeSpan Total, TimeSpan Styles, TimeSpan Layout, TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster, int StylesRecomputed, int LayoutComputations, IReadOnlyList<PixelRect>? Damage)
```

A rasterized frame, with timings and counters from its actual scene update.

| Parameter | Description |
| --- | --- |
| `Number` | The number (int). |
| `Elapsed` | The elapsed (TimeSpan). |
| `Cause` | The reason this frame was requested, recorded by diagnostics. |
| `Total` | The total (TimeSpan). |
| `Styles` | The styles (TimeSpan). |
| `Layout` | The layout (TimeSpan). |
| `DisplayList` | The display list (TimeSpan). |
| `Overlay` | The overlay (TimeSpan). |
| `Raster` | The raster (TimeSpan). |
| `StylesRecomputed` | The styles recomputed (int). |
| `LayoutComputations` | The layout computations (int). |
| `Damage` | The damage (IReadOnlyList<PixelRect>?). |

[Source](../../Sources/Core/Nazor.Diagnostics/FrameInfo.cs)


<a id="type-nodeinfo"></a>

## `NodeInfo`

Namespace: `Nazor.Diagnostics`

```csharp
public sealed record NodeInfo
```

A node of the element tree as developer tools show it: plain data with the stable id the session gave the node, which a tool can keep and a remote client can receive.

[Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

### Members

#### `Id`

```csharp
public required int Id { get; init;}
```

Gets or sets the identifier used to refer to this node in the diagnostics session.

#### `Kind`

```csharp
public required NodeKind Kind { get; init;}
```

Gets or sets whether the snapshot describes an element or text node.

#### `Name`

```csharp
public required string Name { get; init;}
```

The element name (`Button`), or `#text`.

#### `Text`

```csharp
public string? Text { get; init;}
```

The content of a text node.

#### `Classes`

```csharp
public IReadOnlyList<string> Classes { get; init;}
```

Gets or sets the NSS class names captured for this node.

#### `ElementId`

```csharp
public string? ElementId { get; init;}
```

The `id` attribute (the `Id` of a control).

#### `Key`

```csharp
public string? Key { get; init;}
```

The `@key` of the element, as text.

#### `Attributes`

```csharp
public IReadOnlyDictionary<string, string> Attributes { get; init;}
```

The attributes with a value, as text; event handlers are in `Events`.

#### `Events`

```csharp
public IReadOnlyList<string> Events { get; init;}
```

The attributes that hold an event handler (`onclick`…).

#### `States`

```csharp
public ElementStates States { get; init;}
```

Gets or sets the interaction states recorded in this node snapshot.

#### `Owner`

```csharp
public string? Owner { get; init;}
```

The component whose render output contains the element.

#### `Scope`

```csharp
public string? Scope { get; init;}
```

The component whose markup declared it: its scoped styles apply.

#### `IsRendered`

```csharp
public bool IsRendered { get; init;}
```

False when the element or an ancestor is not displayed (`display: none`, not visible).

#### `ParentId`

```csharp
public int? ParentId { get; init;}
```

The parent element; null for the top-level nodes of a root component.

#### `Children`

```csharp
public IReadOnlyList<int> Children { get; init;}
```

The children elements and texts, in order.


<a id="type-nodekind"></a>

## `NodeKind`

Namespace: `Nazor.Diagnostics`

```csharp
public enum NodeKind
```

What a node of the element tree is.

[Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

### Members

#### `Element`

```csharp
Element
```

A retained element node.

#### `Text`

```csharp
Text
```

A retained text node.


<a id="type-treechange"></a>

## `TreeChange`

Namespace: `Nazor.Diagnostics`

```csharp
public sealed record TreeChange( IReadOnlyList<int> Added, IReadOnlyList<int> Removed, IReadOnlyList<int> Updated, IReadOnlyList<int> ChildrenChanged, bool RootsChanged)
```

The changes of one batch of renders among the nodes the session knows: nodes it never gave an id to are not reported, except the new children of the known ones.

| Parameter | Description |
| --- | --- |
| `Added` | The added (IReadOnlyList<int>). |
| `Removed` | The removed (IReadOnlyList<int>). |
| `Updated` | The updated (IReadOnlyList<int>). |
| `ChildrenChanged` | The children changed (IReadOnlyList<int>). |
| `RootsChanged` | The roots changed (bool). |

[Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

## Detached tree types

All types below are in `Nazor.Diagnostics`. Positional parameters define public properties. Capture returns detached read-only collections and no runtime nodes.

```csharp
public sealed record ComponentNodeInfo(int Id, int? ParentId, int RootId, string TypeName,
    string? Key, bool IsFramework, int? DeclaringComponentId,
    IReadOnlyList<int> Children, IReadOnlyList<int> OutputNodes);
public sealed record StructuralNodeInfo(int Id, int? ParentId, int RootId, NodeKind Kind,
    string Name, string? Text, string? Key, int? ProducingComponentId,
    int? DeclaringComponentId, bool? IsRendered, IReadOnlyList<int> Children);
public sealed record DiagnosticTreeSnapshot(string SessionId, long Revision,
    DateTimeOffset CapturedAtUtc, IReadOnlyList<int> ComponentRoots,
    IReadOnlyList<int> ElementRoots, IReadOnlyList<ComponentNodeInfo> Components,
    IReadOnlyList<StructuralNodeInfo> Nodes);
public sealed class TreeCaptureLimitException : InvalidOperationException;
```

Component `ParentId`/`Children` describe nearest logical component ancestry, flattening regions and physical containers. Physical ancestry is separate. `RootId` names the owning root component. `OutputNodes` are top-level physical outputs of that component; wrapper and child can share links. Empty outputs mean a captured component rendered no physical output. `IsFramework` identifies components from Nazor.Core/Nazor.Controls. `TypeName` is display metadata, never identity. Keys use known scalar formatting or a type placeholder. Declaring references outside the captured tree are null.

`StructuralNodeInfo` includes text and hidden nodes. Element producing ownership and lexical declaring identity are independent, including projected content. Text uses the enclosing component's identity. `IsRendered` is null for text or dirty styles; capture does not force style/layout. Missing visibility/detail means unavailable.

### Captured node metadata

`ComponentNodeInfo.Parameters` is an optional `IReadOnlyDictionary<string, string>` containing last directly supplied parameter inputs from the renderer. It does not read component properties and excludes defaults/cascading values. Scalars use invariant formatting; unknown values/delegates use a type placeholder. Null scalar values are displayed as `null`.

`StructuralNodeInfo` adds optional `string? ElementId`, `IReadOnlyDictionary<string, string> Attributes` and `IReadOnlyList<string> Classes`, `States`, `Events` init properties. They copy retained element data without style/layout recomputation. Events contain attribute names only, never delegates/callback instances. Text nodes leave these fields null. Collections are detached/read-only. Each parameter/attribute/list is bounded to 128 entries; the complete capture's existing node/string/link budgets also include this metadata.

### Additional `DevToolsSession` members

```csharp
public string SessionId { get; }
public long TreeRevision { get; private set; }
public DiagnosticTreeSnapshot CaptureTree();
public const int MaxCaptureNodes = 4096;
public const int MaxCaptureLogicalNodes = 8192;
public const int MaxCaptureDepth = 128;
public const int MaxCaptureStringLength = 16384;
public const int MaxCaptureCharacters = 262144;
public const int MaxCaptureLinks = 16384;
public const int MaxCaptureProperties = 128;
```

`SessionId` is opaque and stable for one attachment. `TreeRevision` starts at 1 and advances for every observed renderer batch. `IdOf` also registers component nodes; `NodeOf` can return them, while `ElementOf`/`Get` remain physical APIs. IDs survive retained reorders and are never reused after removal.

`CaptureTree` requires the UI owner outside a batch, including outside synchronous batch-completed observers; post capture to the dispatcher. Thread/batch misuse throws `InvalidOperationException`, disposed access throws `ObjectDisposedException`, and any budget excess throws `TreeCaptureLimitException` without a partial snapshot. Logical node/depth budgets include transparent regions. Combined node and link budgets cover both trees; string budgets use UTF-16 code units. Returned collections are read-only. No arbitrary getter/formatter or JSON work runs during capture.

### Additional `NodeInfo` and `TreeChange` members

`NodeInfo.ProducingComponentId` and `NodeInfo.DeclaringComponentId` are nullable init-only integers. They expose known instance IDs separately from display `Owner`/`Scope` names; structural capture registers all current component identities.

`TreeChange.ComponentsChanged` is an init-only bool and `TreeChange.Revision` an init-only long. A component-only render/removal raises `TreeChanged` even without physical changes. `Removed` includes retired component IDs and all reported physical descendants. Capture again after the batch to obtain complete logical child/output updates.

[Snapshot types](../../Sources/Core/Nazor.Diagnostics/TreeSnapshot.cs) and [capture implementation](../../Sources/Core/Nazor.Diagnostics/TreeCapture.cs).

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `BoxModel`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/BoxModel.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record BoxModel : IEquatable<BoxModel>` | The boxes of an element as the box model of developer tools shows them, in logical units of the window, from the margin box out to the content box in, with the widths of each ring. With a transform each box is the bounds of the transformed one; scroll offsets are applied, as on the screen. `Nazor.Diagnostics.BoxModel.Physical(Nazor.Graphics.Rect)` gives a box in pixels.<br>`MarginBox`. The margin box (Rect).<br>`BorderBox`. The border box (Rect).<br>`PaddingBox`. The padding box (Rect).<br>`ContentBox`. The content box (Rect).<br>`Margin`. The margin (Thickness).<br>`Border`. The border (Thickness).<br>`Padding`. The padding (Thickness).<br>`Scale`. The scale (float). |
| `public BoxModel(Rect MarginBox, Rect BorderBox, Rect PaddingBox, Rect ContentBox, Thickness Margin, Thickness Border, Thickness Padding, float Scale)` | The boxes of an element as the box model of developer tools shows them, in logical units of the window, from the margin box out to the content box in, with the widths of each ring. With a transform each box is the bounds of the transformed one; scroll offsets are applied, as on the screen. `Nazor.Diagnostics.BoxModel.Physical(Nazor.Graphics.Rect)` gives a box in pixels.<br>`MarginBox`. The margin box (Rect).<br>`BorderBox`. The border box (Rect).<br>`PaddingBox`. The padding box (Rect).<br>`ContentBox`. The content box (Rect).<br>`Margin`. The margin (Thickness).<br>`Border`. The border (Thickness).<br>`Padding`. The padding (Thickness).<br>`Scale`. The scale (float). |
| `public override string ToString()` |  |
| `public static bool operator !=(BoxModel? left, BoxModel? right)` |  |
| `public static bool operator ==(BoxModel? left, BoxModel? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(BoxModel? other)` |  |
| `public void Deconstruct(out Rect MarginBox, out Rect BorderBox, out Rect PaddingBox, out Rect ContentBox, out Thickness Margin, out Thickness Border, out Thickness Padding, out float Scale)` |  |
| `public Rect MarginBox { get; init; }` | The margin box (Rect). |
| `public Rect BorderBox { get; init; }` | The border box (Rect). |
| `public Rect PaddingBox { get; init; }` | The padding box (Rect). |
| `public Rect ContentBox { get; init; }` | The content box (Rect). |
| `public Thickness Margin { get; init; }` | The margin (Thickness). |
| `public Thickness Border { get; init; }` | The border (Thickness). |
| `public Thickness Padding { get; init; }` | The padding (Thickness). |
| `public float Scale { get; init; }` | The scale (float). |

### `ComponentNodeInfo`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/TreeSnapshot.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ComponentNodeInfo : IEquatable<ComponentNodeInfo>` | A detached retained component instance, including components with no physical output.<br>`Id`. Session identity of the component node.<br>`ParentId`. Nearest logical component ancestor, or null for a root.<br>`RootId`. Owning root component identity.<br>`TypeName`. Qualified runtime type name for display, never instance identity.<br>`Key`. Safe scalar key text or a type placeholder for an unknown key.<br>`IsFramework`. Whether the component belongs to Nazor.Core or Nazor.Controls.<br>`DeclaringComponentId`. Known lexical declaring instance, independently of logical parentage.<br>`Children`. Immediate logical component children in retained order.<br>`OutputNodes`. Top-level physical outputs, shared with nonvisual wrappers when applicable. |
| `public ComponentNodeInfo(int Id, int? ParentId, int RootId, string TypeName, string? Key, bool IsFramework, int? DeclaringComponentId, IReadOnlyList<int> Children, IReadOnlyList<int> OutputNodes)` | A detached retained component instance, including components with no physical output.<br>`Id`. Session identity of the component node.<br>`ParentId`. Nearest logical component ancestor, or null for a root.<br>`RootId`. Owning root component identity.<br>`TypeName`. Qualified runtime type name for display, never instance identity.<br>`Key`. Safe scalar key text or a type placeholder for an unknown key.<br>`IsFramework`. Whether the component belongs to Nazor.Core or Nazor.Controls.<br>`DeclaringComponentId`. Known lexical declaring instance, independently of logical parentage.<br>`Children`. Immediate logical component children in retained order.<br>`OutputNodes`. Top-level physical outputs, shared with nonvisual wrappers when applicable. |
| `public override string ToString()` |  |
| `public static bool operator !=(ComponentNodeInfo? left, ComponentNodeInfo? right)` |  |
| `public static bool operator ==(ComponentNodeInfo? left, ComponentNodeInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ComponentNodeInfo? other)` |  |
| `public void Deconstruct(out int Id, out int? ParentId, out int RootId, out string TypeName, out string? Key, out bool IsFramework, out int? DeclaringComponentId, out IReadOnlyList<int> Children, out IReadOnlyList<int> OutputNodes)` |  |
| `public int Id { get; init; }` | Session identity of the component node. |
| `public int? ParentId { get; init; }` | Nearest logical component ancestor, or null for a root. |
| `public int RootId { get; init; }` | Owning root component identity. |
| `public string TypeName { get; init; }` | Qualified runtime type name for display, never instance identity. |
| `public string? Key { get; init; }` | Safe scalar key text or a type placeholder for an unknown key. |
| `public bool IsFramework { get; init; }` | Whether the component belongs to Nazor.Core or Nazor.Controls. |
| `public int? DeclaringComponentId { get; init; }` | Known lexical declaring instance, independently of logical parentage. |
| `public IReadOnlyList<int> Children { get; init; }` | Immediate logical component children in retained order. |
| `public IReadOnlyList<int> OutputNodes { get; init; }` | Top-level physical outputs, shared with nonvisual wrappers when applicable. |
| `public IReadOnlyDictionary<string, string>? Parameters { get; init; }` | Last directly supplied parameter values, safely formatted without reading component getters; excludes defaults and cascading values. |

### `DevToolsSession`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/DevToolsSession.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DevToolsSession : IDisposable` | Exposes a window's element tree to developer tools as plain data with stable IDs. It is built on the renderer, the scene and the input of the window, runs on the UI thread, and costs nothing to a window until it is created. A node gets its id the first time the session meets it and keeps it while it lives; a removed node loses it, and ids are never reused. |
| `public const int MaxCaptureNodes = 4096` | Maximum combined component and physical node count in one complete capture. |
| `public const int MaxCaptureLogicalNodes = 8192` | Maximum retained logical nodes, including transparent regions. |
| `public const int MaxCaptureDepth = 128` | Maximum logical nesting depth, including regions and components. |
| `public const int MaxCaptureStringLength = 16384` | Maximum individual string length, measured in UTF-16 code units. |
| `public const int MaxCaptureCharacters = 262144` | Maximum total copied string content in one structural snapshot. |
| `public const int MaxCaptureLinks = 16384` | Maximum component-child, output and physical-child links in one capture. |
| `public const int MaxCaptureProperties = 128` | Maximum retained parameters or attributes copied from an individual node. |
| `public StyleTrace? Trace(int id)` | How the style of an element came to be: its rules in cascade order with the declarations that win, and where every value comes from; null for a text or a node that is not in the tree.<br>`id`. The diagnostic identifier of the element to trace.<br>**Returns**. The resulting Nazor.Style.StyleTrace? value. |
| `public void RecordFrame(int number, FrameCause cause, TimeSpan total, TimeSpan styles, TimeSpan layout, TimeSpan displayList, TimeSpan overlay, TimeSpan raster, int stylesRecomputed, int layoutComputations, IReadOnlyList<PixelRect>? damage, RenderingInfo? rendering = null, long? renderWorkerFrame = null, TimeSpan? renderWorkerDuration = null, long? gpuFramesReplaced = null)` | Records completed UI paint work before backend presentation completes. Optional GPU metrics describe the last completed worker frame.<br>`number`. The number (int).<br>`cause`. The reason this frame was requested, recorded by diagnostics.<br>`total`. The total (TimeSpan).<br>`styles`. The styles (TimeSpan).<br>`layout`. The layout (TimeSpan).<br>`displayList`. Application display-list construction time.<br>`overlay`. The overlay (TimeSpan).<br>`raster`. Software rasterization or GPU recording and submission time on the UI thread.<br>`stylesRecomputed`. The styles recomputed (int).<br>`layoutComputations`. The layout computations (int).<br>`damage`. The damage (IReadOnlyList<PixelRect>?).<br>`rendering`. The requested and effective renderer, when available.<br>`renderWorkerFrame`. The last completed GPU worker frame number.<br>`renderWorkerDuration`. CPU submission plus presentation wait for that worker frame.<br>`gpuFramesReplaced`. The cumulative number of replaced pending GPU frames. |
| `public DiagnosticTreeSnapshot CaptureTree()` | Copies both retained trees on the owning UI thread outside a render batch. No user properties, unknown ToString implementations, style/layout updates or JSON serialization are invoked. Hidden nodes remain in the structure; visibility is unavailable when styles are dirty. Collections are detached and read-only.<br>**Returns**. A complete snapshot that remains usable after target mutation or disposal.<br>**Throws** `Nazor.Diagnostics.TreeCaptureLimitException`. The complete snapshot exceeds the fixed capture budget.<br>**Throws** `System.InvalidOperationException`. The thread is not the UI owner, or a batch is still running.<br>**Throws** `System.ObjectDisposedException`. The session is disposed. |
| `public string SessionId { get; }` | Opaque identity of this attachment, unchanged until it is disposed. |
| `public long TreeRevision { get; }` | Revision advanced after every renderer batch observed by this attachment. |
| `public bool IsInspecting { get; }` | Gets whether pointer-based diagnostic inspection is active. |
| `public int? HighlightedId { get; }` | Gets the highlighted node's diagnostic identifier, or null when no node is highlighted. |
| `public int? SelectedId { get; }` | Gets the selected node's diagnostic identifier, or null when no node is selected. |

### `DiagnosticTreeSnapshot`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/TreeSnapshot.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DiagnosticTreeSnapshot : IEquatable<DiagnosticTreeSnapshot>` | A complete immutable-by-collection snapshot, usable without further access to the dispatcher or renderer.<br>`SessionId`. Opaque identity of the diagnostics attachment.<br>`Revision`. Renderer-batch revision at capture.<br>`CapturedAtUtc`. UTC capture time.<br>`ComponentRoots`. Root component identities in host order.<br>`ElementRoots`. Root physical outputs in host order.<br>`Components`. All component instances.<br>`Nodes`. All physical elements/texts, including hidden nodes. |
| `public DiagnosticTreeSnapshot(string SessionId, long Revision, DateTimeOffset CapturedAtUtc, IReadOnlyList<int> ComponentRoots, IReadOnlyList<int> ElementRoots, IReadOnlyList<ComponentNodeInfo> Components, IReadOnlyList<StructuralNodeInfo> Nodes)` | A complete immutable-by-collection snapshot, usable without further access to the dispatcher or renderer.<br>`SessionId`. Opaque identity of the diagnostics attachment.<br>`Revision`. Renderer-batch revision at capture.<br>`CapturedAtUtc`. UTC capture time.<br>`ComponentRoots`. Root component identities in host order.<br>`ElementRoots`. Root physical outputs in host order.<br>`Components`. All component instances.<br>`Nodes`. All physical elements/texts, including hidden nodes. |
| `public override string ToString()` |  |
| `public static bool operator !=(DiagnosticTreeSnapshot? left, DiagnosticTreeSnapshot? right)` |  |
| `public static bool operator ==(DiagnosticTreeSnapshot? left, DiagnosticTreeSnapshot? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DiagnosticTreeSnapshot? other)` |  |
| `public void Deconstruct(out string SessionId, out long Revision, out DateTimeOffset CapturedAtUtc, out IReadOnlyList<int> ComponentRoots, out IReadOnlyList<int> ElementRoots, out IReadOnlyList<ComponentNodeInfo> Components, out IReadOnlyList<StructuralNodeInfo> Nodes)` |  |
| `public string SessionId { get; init; }` | Opaque identity of the diagnostics attachment. |
| `public long Revision { get; init; }` | Renderer-batch revision at capture. |
| `public DateTimeOffset CapturedAtUtc { get; init; }` | UTC capture time. |
| `public IReadOnlyList<int> ComponentRoots { get; init; }` | Root component identities in host order. |
| `public IReadOnlyList<int> ElementRoots { get; init; }` | Root physical outputs in host order. |
| `public IReadOnlyList<ComponentNodeInfo> Components { get; init; }` | All component instances. |
| `public IReadOnlyList<StructuralNodeInfo> Nodes { get; init; }` | All physical elements/texts, including hidden nodes. |

### `FrameCause`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/FrameInfo.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FrameCause` | Identifies why a frame was requested; values can be combined. |

### `FrameInfo`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/FrameInfo.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FrameInfo : IEquatable<FrameInfo>` | A rasterized frame, with timings and counters from its actual scene update.<br>`Number`. The number (int).<br>`Elapsed`. The elapsed (TimeSpan).<br>`Cause`. The reason this frame was requested, recorded by diagnostics.<br>`Total`. The total (TimeSpan).<br>`Styles`. The styles (TimeSpan).<br>`Layout`. The layout (TimeSpan).<br>`DisplayList`. The display list (TimeSpan).<br>`Overlay`. The overlay (TimeSpan).<br>`Raster`. UI software rasterization or GPU recording/submission time.<br>`StylesRecomputed`. The styles recomputed (int).<br>`LayoutComputations`. The layout computations (int).<br>`Damage`. The damage (IReadOnlyList<PixelRect>?). |
| `public FrameInfo(int Number, TimeSpan Elapsed, FrameCause Cause, TimeSpan Total, TimeSpan Styles, TimeSpan Layout, TimeSpan DisplayList, TimeSpan Overlay, TimeSpan Raster, int StylesRecomputed, int LayoutComputations, IReadOnlyList<PixelRect>? Damage)` | A rasterized frame, with timings and counters from its actual scene update.<br>`Number`. The number (int).<br>`Elapsed`. The elapsed (TimeSpan).<br>`Cause`. The reason this frame was requested, recorded by diagnostics.<br>`Total`. The total (TimeSpan).<br>`Styles`. The styles (TimeSpan).<br>`Layout`. The layout (TimeSpan).<br>`DisplayList`. The display list (TimeSpan).<br>`Overlay`. The overlay (TimeSpan).<br>`Raster`. UI software rasterization or GPU recording/submission time.<br>`StylesRecomputed`. The styles recomputed (int).<br>`LayoutComputations`. The layout computations (int).<br>`Damage`. The damage (IReadOnlyList<PixelRect>?). |
| `public override string ToString()` |  |
| `public static bool operator !=(FrameInfo? left, FrameInfo? right)` |  |
| `public static bool operator ==(FrameInfo? left, FrameInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FrameInfo? other)` |  |
| `public void Deconstruct(out int Number, out TimeSpan Elapsed, out FrameCause Cause, out TimeSpan Total, out TimeSpan Styles, out TimeSpan Layout, out TimeSpan DisplayList, out TimeSpan Overlay, out TimeSpan Raster, out int StylesRecomputed, out int LayoutComputations, out IReadOnlyList<PixelRect>? Damage)` |  |
| `public int Number { get; init; }` | The number (int). |
| `public TimeSpan Elapsed { get; init; }` | The elapsed (TimeSpan). |
| `public FrameCause Cause { get; init; }` | The reason this frame was requested, recorded by diagnostics. |
| `public TimeSpan Total { get; init; }` | The total (TimeSpan). |
| `public TimeSpan Styles { get; init; }` | The styles (TimeSpan). |
| `public TimeSpan Layout { get; init; }` | The layout (TimeSpan). |
| `public TimeSpan DisplayList { get; init; }` | The display list (TimeSpan). |
| `public TimeSpan Overlay { get; init; }` | The overlay (TimeSpan). |
| `public TimeSpan Raster { get; init; }` | UI software rasterization or GPU recording/submission time. |
| `public int StylesRecomputed { get; init; }` | The styles recomputed (int). |
| `public int LayoutComputations { get; init; }` | The layout computations (int). |
| `public IReadOnlyList<PixelRect>? Damage { get; init; }` | The damage (IReadOnlyList<PixelRect>?). |
| `public RenderingInfo? Rendering { get; init; }` | Gets the renderer selected for the UI frame, when supplied by the host. |
| `public long? RenderWorkerFrame { get; init; }` | Gets the independently numbered last completed GPU worker frame. |
| `public TimeSpan? RenderWorkerDuration { get; init; }` | Gets that worker frame's CPU submission and presentation wait, excluding UI work and GPU timestamps. |
| `public long? GpuFramesReplaced { get; init; }` | Gets the cumulative count of replaced pending GPU frames. |

### `NodeInfo`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record NodeInfo : IEquatable<NodeInfo>` | A node of the element tree as developer tools show it: plain data with the stable id the session gave the node, which a tool can keep and a remote client can receive. |
| `public override string ToString()` |  |
| `public static bool operator !=(NodeInfo? left, NodeInfo? right)` |  |
| `public static bool operator ==(NodeInfo? left, NodeInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(NodeInfo? other)` |  |
| `public NodeInfo()` |  |
| `public int? ProducingComponentId { get; init; }` | Known producing instance identity after logical discovery; null when not yet captured. |
| `public int? DeclaringComponentId { get; init; }` | Known lexical declaring instance identity after logical discovery. |

### `NodeKind`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

| Declaration | Contract |
| --- | --- |
| `Element = 0` | A retained element node. |
| `Text = 1` | A retained text node. |

### `StructuralNodeInfo`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/TreeSnapshot.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record StructuralNodeInfo : IEquatable<StructuralNodeInfo>` | Detached structural element/text data; no style/layout collection or user property evaluation occurs.<br>`Id`. Session identity of the retained physical node.<br>`ParentId`. Nearest physical element parent, or null at a root output.<br>`RootId`. Owning root component identity.<br>`Kind`. Element or text.<br>`Name`. Element name or #text.<br>`Text`. Copied text content, otherwise null.<br>`Key`. Safe element key display, otherwise null.<br>`ProducingComponentId`. Component instance that produced the output.<br>`DeclaringComponentId`. Known lexical declaring instance, otherwise null.<br>`IsRendered`. Visibility when styles are current; null means unavailable, never an empty subtree.<br>`Children`. Physical children in retained order. |
| `public StructuralNodeInfo(int Id, int? ParentId, int RootId, NodeKind Kind, string Name, string? Text, string? Key, int? ProducingComponentId, int? DeclaringComponentId, bool? IsRendered, IReadOnlyList<int> Children)` | Detached structural element/text data; no style/layout collection or user property evaluation occurs.<br>`Id`. Session identity of the retained physical node.<br>`ParentId`. Nearest physical element parent, or null at a root output.<br>`RootId`. Owning root component identity.<br>`Kind`. Element or text.<br>`Name`. Element name or #text.<br>`Text`. Copied text content, otherwise null.<br>`Key`. Safe element key display, otherwise null.<br>`ProducingComponentId`. Component instance that produced the output.<br>`DeclaringComponentId`. Known lexical declaring instance, otherwise null.<br>`IsRendered`. Visibility when styles are current; null means unavailable, never an empty subtree.<br>`Children`. Physical children in retained order. |
| `public override string ToString()` |  |
| `public static bool operator !=(StructuralNodeInfo? left, StructuralNodeInfo? right)` |  |
| `public static bool operator ==(StructuralNodeInfo? left, StructuralNodeInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(StructuralNodeInfo? other)` |  |
| `public void Deconstruct(out int Id, out int? ParentId, out int RootId, out NodeKind Kind, out string Name, out string? Text, out string? Key, out int? ProducingComponentId, out int? DeclaringComponentId, out bool? IsRendered, out IReadOnlyList<int> Children)` |  |
| `public int Id { get; init; }` | Session identity of the retained physical node. |
| `public int? ParentId { get; init; }` | Nearest physical element parent, or null at a root output. |
| `public int RootId { get; init; }` | Owning root component identity. |
| `public NodeKind Kind { get; init; }` | Element or text. |
| `public string Name { get; init; }` | Element name or #text. |
| `public string? Text { get; init; }` | Copied text content, otherwise null. |
| `public string? Key { get; init; }` | Safe element key display, otherwise null. |
| `public int? ProducingComponentId { get; init; }` | Component instance that produced the output. |
| `public int? DeclaringComponentId { get; init; }` | Known lexical declaring instance, otherwise null. |
| `public bool? IsRendered { get; init; }` | Visibility when styles are current; null means unavailable, never an empty subtree. |
| `public IReadOnlyList<int> Children { get; init; }` | Physical children in retained order. |
| `public string? ElementId { get; init; }` | Copied markup identifier, independently of the diagnostic instance identity. |
| `public IReadOnlyDictionary<string, string>? Attributes { get; init; }` | Safely formatted retained attributes, excluding event callbacks; null for text nodes. |
| `public IReadOnlyList<string>? Classes { get; init; }` | Copied class names, or null for text nodes. |
| `public IReadOnlyList<string>? States { get; init; }` | Copied interaction-state names, or null for text nodes. |
| `public IReadOnlyList<string>? Events { get; init; }` | Event attribute names without callback instances, or null for text nodes. |

### `TreeCaptureLimitException`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/TreeSnapshot.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TreeCaptureLimitException : InvalidOperationException` | A structural capture exceeded its fixed work/data budget; no partial snapshot is returned. |

### `TreeChange`

Namespace `Nazor.Diagnostics`. [Source](../../Sources/Core/Nazor.Diagnostics/NodeInfo.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TreeChange : IEquatable<TreeChange>` | The changes of one batch of renders among the nodes the session knows: nodes it never gave an id to are not reported, except the new children of the known ones.<br>`Added`. The added (IReadOnlyList<int>).<br>`Removed`. The removed (IReadOnlyList<int>).<br>`Updated`. The updated (IReadOnlyList<int>).<br>`ChildrenChanged`. The children changed (IReadOnlyList<int>).<br>`RootsChanged`. The roots changed (bool). |
| `public TreeChange(IReadOnlyList<int> Added, IReadOnlyList<int> Removed, IReadOnlyList<int> Updated, IReadOnlyList<int> ChildrenChanged, bool RootsChanged)` | The changes of one batch of renders among the nodes the session knows: nodes it never gave an id to are not reported, except the new children of the known ones.<br>`Added`. The added (IReadOnlyList<int>).<br>`Removed`. The removed (IReadOnlyList<int>).<br>`Updated`. The updated (IReadOnlyList<int>).<br>`ChildrenChanged`. The children changed (IReadOnlyList<int>).<br>`RootsChanged`. The roots changed (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(TreeChange? left, TreeChange? right)` |  |
| `public static bool operator ==(TreeChange? left, TreeChange? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TreeChange? other)` |  |
| `public void Deconstruct(out IReadOnlyList<int> Added, out IReadOnlyList<int> Removed, out IReadOnlyList<int> Updated, out IReadOnlyList<int> ChildrenChanged, out bool RootsChanged)` |  |
| `public IReadOnlyList<int> Added { get; init; }` | The added (IReadOnlyList<int>). |
| `public IReadOnlyList<int> Removed { get; init; }` | The removed (IReadOnlyList<int>). |
| `public IReadOnlyList<int> Updated { get; init; }` | The updated (IReadOnlyList<int>). |
| `public IReadOnlyList<int> ChildrenChanged { get; init; }` | The children changed (IReadOnlyList<int>). |
| `public bool RootsChanged { get; init; }` | The roots changed (bool). |
| `public bool ComponentsChanged { get; init; }` | A component render/removal may have changed the logical tree, even without physical changes. |
| `public long Revision { get; init; }` | Renderer-batch revision, for detecting intervening changes during capture scheduling. |
