# Nazor.Input

Public and protected API of the current development candidate.

Inherited members follow their declaring type. Positional record parameters also define properties with the same names. Signatures and descriptions below follow the current source; application layout and validation remain caller-owned unless a control explicitly supplies them.

## Closing surface input

Retained outgoing surfaces reject pointer, keyboard and accessibility actions. An exiting modal or modal ancestor also blocks its owned external popup scope and window shortcuts. Focus is cleared from closing content and returns after removal; reopening refocuses the same surface without replacing its original return target. Blur, focus-out and pointer-leave cleanup still runs. `CanInteract` includes visibility and this current modal policy. [Closing motion](../Guide/11-Commands-Menus-and-Popups.md#closing-motion)

## Types

- [`InputManager`](#type-inputmanager)
- [`TextSelection`](#type-textselection)
- [`WindowRegion`](#type-windowregion)

<a id="type-inputmanager"></a>

## `InputManager`

Namespace: `Nazor.Input`

```csharp
public sealed partial class InputManager : IDisposable, IElementInteraction
```

Turns normalised pointer and keyboard input into element states and semantic events: `:hover` on the element under the pointer and its ancestors, `:active` on a pressed activatable element or a captured slider/handle, focus and focus-visible, Tab order, and `onclick` from a pointer release over the pressed element or from Enter/Space on the focused one. Elements opt in through attributes set by controls: `onclick`, `focusable`, `disabled`. Everything runs on the dispatcher thread.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L30)


### `TextPolicy`

```csharp
public TextInputPolicy TextPolicy { get; set; }
```

Backend editing conventions; real event modifiers remain unchanged.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L54)


### `WheelScrollDistance`

```csharp
public float WheelScrollDistance { get; set; }
```

Logical distance per wheel detent; configured by the backend.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L57)


### `WheelScrollByPage`

```csharp
public bool WheelScrollByPage { get; set; }
```

Gets or sets whether a wheel detent scrolls by a viewport page rather than a fixed distance.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L62)


### `InputManager`

```csharp
public InputManager(Renderer renderer, Scene scene, KeyBindings? keyBindings = null)
```

Dispatches input to the retained element tree and manages focus, selection, and pointer capture.

- `renderer`: The renderer that owns the component tree.

- `scene`: The scene used for layout, hit testing, and drawing.

- `keyBindings`: The command registry; null creates a registry for this input manager.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L82)


### `Hovered`

```csharp
public ElementNode? Hovered { get; }
```

The deepest element under the pointer.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L97)


### `Focused`

```csharp
public ElementNode? Focused { get; private set; }
```

Gets the element with keyboard focus, or null when no element is focused.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L100)


### `KeyBindings`

```csharp
public KeyBindings KeyBindings { get; }
```

Gets the keyboard command registry used for input dispatch.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L103)


### `CapturedPointer`

```csharp
public ElementNode? CapturedPointer { get; private set; }
```

Gets the element receiving captured pointer input, or null when capture is inactive.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L106)


### `PointerCaptureChanged`

```csharp
public event Action<bool>? PointerCaptureChanged;
```

Occurs when pointer capture changes.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L109)


### `FocusAsync`

```csharp
public Task FocusAsync(ElementNode element, bool preventScroll = false)
```

Requests focus for the target element, optionally preserving scroll offsets. Requests made during pointer-down dispatch use pointer focus cues.

- `element`: The element to inspect or update.

- `preventScroll`: Whether focusing should preserve the current scroll position.

Returns: A task that completes when the operation has finished.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L115)


### `CapturePointer`

```csharp
public bool CapturePointer(ElementNode element)
```

Captures pointer input for the target element until capture is released or canceled. Transferring capture raises `onlostpointercapture` on the previous live owner after the new owner takes capture.

- `element`: The element to inspect or update.

Returns: true if pointer capture was acquired; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L127)


### `HasPointerCapture`

```csharp
public bool HasPointerCapture(ElementNode element)
```

Returns true while `element` owns this input manager's pointer capture, including during its release callback. Transfer, release and cancellation update the result. Implements `IElementInteraction.HasPointerCapture`; available to controls through `ElementReference.IsPointerCaptured`.

### `ScrollTo`

```csharp
public bool ScrollTo(ElementNode element, double? left, double? top)
```

Scrolls a container to an offset, as `ElementReference.ScrollTo` asks; `onscroll` follows at once.

- `element`: The element to inspect or update.

- `left`: The requested horizontal scroll offset, or null to keep it unchanged.

- `top`: The requested vertical scroll offset, or null to keep it unchanged.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L177)


### `ReleasePointerCapture`

```csharp
public void ReleasePointerCapture(ElementNode element)
```

Releases pointer capture held by the target element and raises `onlostpointercapture` on its live owner. Releasing an element that does not own capture has no effect. Cleanup notification also runs while disabled or while its modal surface exits.

- `element`: The element to inspect or update.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L222)


### `VisualStateChanged`

```csharp
public event Action? VisualStateChanged;
```

Raised when states or the focus change, so the host can schedule a frame.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L241)


### `PointerMoved`

```csharp
public void PointerMoved(Point point, KeyModifiers modifiers)
```

Processes the pointer moved notification in logical viewport coordinates.

- `point`: The position in logical viewport coordinates.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L246)


### `PointerPressed`

```csharp
public void PointerPressed(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer pressed notification in logical viewport coordinates, preserving synchronous focus requests made by pointer handlers.

- `point`: The position in logical viewport coordinates.

- `button`: The pointer button associated with the event.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L265)


### `PointerReleased`

```csharp
public void PointerReleased(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer released notification in logical viewport coordinates.

- `point`: The position in logical viewport coordinates.

- `button`: The pointer button associated with the event.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L301)


### `PointerLeft`

```csharp
public void PointerLeft()
```

Processes the pointer left notification in logical viewport coordinates.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L333)


### `PointerDoubleClicked`

```csharp
public void PointerDoubleClicked(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer double clicked notification in logical viewport coordinates.

- `point`: The position in logical viewport coordinates.

- `button`: The pointer button associated with the event.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L343)


### `ContextMenuRequested`

```csharp
public void ContextMenuRequested(KeyModifiers modifiers = KeyModifiers.None)
```

Opens the context menu associated with the focused element.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L348)


### `TooltipDelay`

```csharp
public TimeSpan TooltipDelay { get; set; }
```

How long the pointer rests on a control, or the keyboard focus, before its tooltip shows.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L359)


### `TooltipTarget`

```csharp
public ElementNode? TooltipTarget { get; }
```

The control whose tooltip is showing, or null.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L371)


### `PointerWheel`

```csharp
public void PointerWheel(Point point, float notches, KeyModifiers modifiers)
```

The wheel moved by `notches` (positive away from the user: scroll up).

- `point`: The position in logical viewport coordinates.

- `notches`: The number of wheel detents delivered by the backend.

- `modifiers`: The modifier keys pressed when the input occurred.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L377)


### `PointerWheel`

```csharp
public void PointerWheel(Point point, float deltaX, float deltaY, KeyModifiers modifiers, ScrollDeltaUnit unit = ScrollDeltaUnit.Notches)
```

A wheel moved: `deltaX` notches to the right (horizontal wheel, tilt), `deltaY` away from the user. Unless a handler prevents it, the nearest container that can scroll on that axis scrolls; Shift turns the vertical wheel horizontal. Scrolling stops at the containing overlay surface, and an active modal surface blocks wheel interaction with its background.

- `point`: The position in logical viewport coordinates.

- `deltaX`: The horizontal scroll delta in the specified unit.

- `deltaY`: The vertical scroll delta in the specified unit.

- `modifiers`: The modifier keys pressed when the input occurred.

- `unit`: The unit used for the scroll deltas.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L390)


### `PointerCanceled`

```csharp
public void PointerCanceled()
```

The platform cancelled the interaction (capture lost, window deactivated).

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L422)


### `PointerCanceled`

```csharp
public void PointerCanceled(bool clearHover)
```

Cancels a gesture; inspection preserves the hover while taking over the pointer.

- `clearHover`: Whether the current pointer hover state should also be cleared.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L426)


### `KeyDown`

```csharp
public bool KeyDown(string key, KeyModifiers modifiers, bool repeat)
```

Handles navigation and activation keys; returns true when the key was consumed.

- `key`: The normalized name of the key.

- `modifiers`: The modifier keys pressed when the input occurred.

- `repeat`: Whether this key event is generated by auto-repeat.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L446)


### `KeyUp`

```csharp
public bool KeyUp(string key, KeyModifiers modifiers)
```

Processes a released key and reports whether it was handled.

- `key`: The normalized name of the key.

- `modifiers`: The modifier keys pressed when the input occurred.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L606)


### `Focus`

```csharp
public void Focus(ElementNode? element, bool visible)
```

Moves the focus; `visible` marks keyboard-originated focus (`:focus-visible`).

- `element`: The element to inspect or update.

- `visible`: Whether the window, control, or native decoration should be visible.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L622)


### `RegionAt`

```csharp
public WindowRegion RegionAt(Point point)
```

What a point of the window is for the system, in a window with the content in the title bar: an element with the attribute `window-drag` (a title bar) moves the window where no control that takes the pointer is above it. Focusable controls, pointer event handlers, tooltips, selectable text and scrollable content keep their client input, including composite control frames. Active pointer capture returns `Client`; otherwise a `snap-layout-area` ancestor marks the native maximize region without changing the target of pointer events.

- `point`: The position in logical viewport coordinates.

Returns: The resulting WindowRegion value.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L759)


### `Activate`

```csharp
public bool Activate(ElementNode element)
```

Activates an element as a click would (semantic action, for example from accessibility tools): false when it is disabled or has no `onclick` handler.

- `element`: The element to inspect or update.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L836)


### `Activate`

```csharp
public bool Activate(ElementNode element, KeyModifiers modifiers)
```

Activates an element as a click with modifiers does (Ctrl+click adds an item to a selection).

- `element`: The element to inspect or update.

- `modifiers`: The modifier keys pressed when the input occurred.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L844)


### `BringIntoView`

```csharp
public bool BringIntoView(ElementNode element)
```

Brings an element inside the visible area of its scrolling ancestors, stopping at its overlay surface; true when something scrolled.

- `element`: The element to inspect or update.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L857)


### `ScrollBy`

```csharp
public bool ScrollBy(ElementNode container, float deltaY)
```

Scrolls a scrolling container by `deltaY` logical units; true when it moved.

- `container`: The container (ElementNode).

- `deltaY`: The vertical scroll delta in the specified unit.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L872)


### `ScrollBy`

```csharp
public bool ScrollBy(ElementNode container, float deltaX, float deltaY)
```

Scrolls a scrolling container on both axes, in logical units; true when it moved.

- `container`: The container (ElementNode).

- `deltaX`: The horizontal scroll delta in the specified unit.

- `deltaY`: The vertical scroll delta in the specified unit.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L879)


### `WindowFocusChanged`

```csharp
public void WindowFocusChanged(bool focused)
```

The window lost or regained the keyboard focus.

- `focused`: Whether the native window has keyboard focus.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L889)


### `CursorAt`

```csharp
public Cursor CursorAt(Point point)
```

The cursor requested by the style of the element under `point`.

- `point`: The position in logical viewport coordinates.

Returns: The resulting Cursor value.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L908)


### `Dismiss`

```csharp
public bool Dismiss(ElementNode overlay)
```

Requests dismissal of an interactive overlay through its ordinary dismissal event.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L1109)


### `CanInteract`

```csharp
public bool CanInteract(ElementNode element)
```

Gets whether user interaction can reach this element in the active modal scope.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L1116)


### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L1196)


### `SetRangeValue`

```csharp
public bool SetRangeValue(ElementNode element, double value)
```

Sets the value of a slider as the user would: snapped to its step, kept in its range, with oninput and onchange. False when the element is disabled or the value does not change.

- `element`: The element to inspect or update.

- `value`: The value to use.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/RangeInput.cs#L21)


### `Clipboard`

```csharp
public IClipboard Clipboard { get; set; }
```

Clipboard used by text editing; the window host sets the system clipboard.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L39)


### `FocusedTextBox`

```csharp
public ElementNode? FocusedTextBox { get; }
```

The focused text input, if any.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L42)


### `TextInput`

```csharp
public void TextInput(string text)
```

Inserts typed text in the focused text input (characters from the keyboard).

- `text`: The text to process.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L53)


### `SetText`

```csharp
public bool SetText(ElementNode box, string text)
```

Replaces the whole value of an enabled text input (semantic action, for example the UI Automation Value pattern); the change is delivered through `oninput` like typing.

- `box`: The box (ElementNode).

- `text`: The text to process.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L67)


### `CompositionUpdate`

```csharp
public void CompositionUpdate(string text, int cursor)
```

Shows the IME preedit at the caret; an empty string ends the composition without text.

- `text`: The text to process.

- `cursor`: The cursor (int).

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L79)


### `CompositionCommit`

```csharp
public void CompositionCommit(string text)
```

Inserts the text committed by the IME, replacing the preedit.

- `text`: The text to process.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L96)


### `GetTextCaretRect`

```csharp
public Rect? GetTextCaretRect()
```

The caret of the focused text input in window coordinates (IME candidate window), or null.

Returns: The resulting Rect? value.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L107)


### `GetTextSelection`

```csharp
public TextSelection GetTextSelection(ElementNode box)
```

The selection of a text box with its caret (the caret alone when nothing is selected), as assistive technologies read it.

- `box`: The box (ElementNode).

Returns: The resulting TextSelection value.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L123)


### `SetTextSelection`

```csharp
public bool SetTextSelection(ElementNode box, int start, int end)
```

Selects [start, end) of a text box with the caret at the end, as Shift+arrows do; false when it is not one.

- `box`: The box (ElementNode).

- `start`: The start offset in the UTF-16 source text.

- `end`: The end offset in the UTF-16 source text.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L137)


### `GetTextRangeBounds`

```csharp
public IReadOnlyList<Rect> GetTextRangeBounds(ElementNode box, int start, int end)
```

The rectangles of [start, end) of a text box in window coordinates, one per line it covers; the caret for an empty range.

- `box`: The box (ElementNode).

- `start`: The start offset in the UTF-16 source text.

- `end`: The end offset in the UTF-16 source text.

Returns: The resulting IReadOnlyList<Rect> value.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L155)


### `GetTextIndexAt`

```csharp
public int GetTextIndexAt(ElementNode box, Point point)
```

The position in the text of a text box closest to a point of the window.

- `box`: The box (ElementNode).

- `point`: The position in logical viewport coordinates.

Returns: The resulting int value.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L172)


### `GetTextLines`

```csharp
public IReadOnlyList<(int Start, int End)> GetTextLines(ElementNode box)
```

The lines of a text box as shown (broken at its width): where each starts and ends.

- `box`: The box (ElementNode).

Returns: The resulting IReadOnlyList<(int Start, int End)> value.

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L177)


<a id="type-textselection"></a>

## `TextSelection`

Namespace: `Nazor.Input`

```csharp
public readonly record struct TextSelection(int Start, int End, int Caret)
```

A selection in a text: from `Start` to `End`, the caret at one of them.

- `Start`: The start offset in the UTF-16 source text.

- `End`: The end offset in the UTF-16 source text.

- `Caret`: The caret (int).

[Source](../../Sources/Core/Nazor.Input/TextEditing.cs#L16)


<a id="type-windowregion"></a>

## `WindowRegion`

Namespace: `Nazor.Input`

```csharp
public enum WindowRegion
```

What a point of a window with the content in the title bar is (`InputManager.RegionAt`).

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L11)


### `Client`

```csharp
Client
```

Routes input to the application content.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L14)


### `Caption`

```csharp
Caption
```

Uses native title-bar dragging and double-click behavior.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L17)


### `MaximizeButton`

```csharp
MaximizeButton
```

Uses native maximize-button hit testing, including Snap Layouts where supported.

[Source](../../Sources/Core/Nazor.Input/InputManager.cs#L20)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `InputManager`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Input/InputManager.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class InputManager : IDisposable, IElementInteraction` | Turns normalised pointer and keyboard input into element states and semantic events: `:hover` on the element under the pointer and its ancestors, `:active` on a pressed activatable element or a captured slider/handle, focus and focus-visible, Tab order, and `onclick` from a pointer release over the pressed element or from Enter/Space on the focused one. Elements opt in through attributes set by controls: `onclick`, `focusable`, `disabled`. Everything runs on the dispatcher thread. |
| `public (double X, double Y, double Width, double Height)? GetBounds(ElementNode element)` | Gets the element's border box in logical window coordinates, as `Nazor.ElementReference.GetBounds` asks.<br>`element`. The element to inspect.<br>**Returns**. The left and top edges, the width and the height, or null when the element is not laid out. |
| `public (double X, double Y)? GetLocalPoint(ElementNode element, double windowX, double windowY)` | Converts logical window coordinates to the element's untransformed border-box origin through inverse own/ancestor transforms and ancestor scroll offsets. Returns null after disposal or for nonfinite/out-of-float-range coordinates, detached/hidden/unlaid-out elements and singular transforms. |
| `public ElementNode? Focused { get; }` | Gets the element with keyboard focus, or null when no element is focused. |
| `public ElementNode? CapturedPointer { get; }` | Gets the element receiving captured pointer input, or null when capture is inactive. |

### `TextSelection`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Input/TextEditing.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TextSelection : IEquatable<TextSelection>` | A selection in a text: from `Nazor.Input.TextSelection.Start` to `Nazor.Input.TextSelection.End`, the caret at one of them.<br>`Start`. The start offset in the UTF-16 source text.<br>`End`. The end offset in the UTF-16 source text.<br>`Caret`. The caret (int). |
| `public TextSelection()` |  |
| `public TextSelection(int Start, int End, int Caret)` | A selection in a text: from `Nazor.Input.TextSelection.Start` to `Nazor.Input.TextSelection.End`, the caret at one of them.<br>`Start`. The start offset in the UTF-16 source text.<br>`End`. The end offset in the UTF-16 source text.<br>`Caret`. The caret (int). |
| `public override string ToString()` |  |
| `public static bool operator !=(TextSelection left, TextSelection right)` |  |
| `public static bool operator ==(TextSelection left, TextSelection right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TextSelection other)` |  |
| `public void Deconstruct(out int Start, out int End, out int Caret)` |  |
| `public int Start { get; init; }` | The start offset in the UTF-16 source text. |
| `public int End { get; init; }` | The end offset in the UTF-16 source text. |
| `public int Caret { get; init; }` | The caret (int). |

### `WindowRegion`

Namespace `Nazor.Input`. [Source](../../Sources/Core/Nazor.Input/InputManager.cs)

| Declaration | Contract |
| --- | --- |
| `Client = 0` | Routes input to the application content. |
| `Caption = 1` | Uses native title-bar dragging and double-click behavior. |
| `MaximizeButton = 2` | Uses native maximize-button hit testing, including Snap Layouts where supported. |

Disabled elements still receive target-local `onresize` layout notifications while their disclosure is clipped or semantically hidden. This keeps natural-height measurement available without permitting input. `Collapsible.ReturnFocus` optionally returns hidden descendant focus to its header.

Implicit tooltip surfaces are keyed by their target element. Reentering the same target during retained exit publishes `reentered` and preserves current visual progress; switching target creates a fresh surface. Tooltips remain noninteractive throughout.

Shared range dragging publishes the captured slider or RangeSliderHandle pressed state through ordinary `:active` styling. ValueChanged/oninput follow movement; a normal primary release raises one onchange when the value changed before capture cleanup. Cancel, explicit release, transfer, disabling, hiding or removal clears pressed/drag state immediately and prevents stale move/up updates. Already reported values remain in the application model. Button release/lost-capture/click ordering is unchanged.
