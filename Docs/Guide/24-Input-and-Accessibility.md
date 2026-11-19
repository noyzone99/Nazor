# 24. Input and accessibility

## Event surface

| Events | Arguments and behavior |
| --- | --- |
| `onclick`, `ondblclick`, `oncontextmenu` | `PointerEventArgs`; activation can be pointer, keyboard, or accessibility-generated |
| `onpointerdown`, `onpointerup`, `onpointermove`, `onpointerenter`, `onpointerleave`, `onlostpointercapture` | Position in logical window coordinates, button, modifiers, synthetic flag |
| `onwheel` | `WheelEventArgs`; deltas and units distinguish notches from continuous logical pixels |
| `onkeydown`, `onkeyup` | `KeyboardEventArgs`; key, actual modifiers, repeat |
| `onfocus`, `onblur`, `onfocusin`, `onfocusout` | `FocusEventArgs`; related target |
| `oninput`, `onchange` | `ChangeEventArgs`; updated string/bool or control-specific value |
| `onscroll` | `ScrollEventArgs`; offsets, viewport size, content size |
| `onresize` | `ResizeEventArgs`; bounds after layout in logical units, including position within the parent |

Routed events bubble through physical ancestors, including across component boundaries. `onfocus`, `onblur`, `onpointerenter`, `onpointerleave`, `ondismiss`, and `onresize` stay on their target. Use `onfocusin` and `onfocusout` to observe focus changes on descendants. The renderer snapshots the event route before dispatch. A render during a handler leaves the remaining route intact.

`PointerEventArgs.OriginalTarget` identifies the first dispatch target and survives bubbling or forwarding. A container can use it to leave descendant editor/button gestures to that child. Set `WheelEventArgs.Handled` synchronously when consuming a supported wheel gesture: remaining callbacks and default scrolling stop, including at a value boundary. Preserve unhandled gestures when the control's option is off or the gesture is unsupported.

Set `KeyboardEventArgs.Handled` before awaiting work when a component consumes a
key. Remaining routed handlers and built-in keyboard actions are suppressed.
This keeps coordinated toggles, disclosure headers and resize handles from
also triggering an ancestor's keyboard behavior. Disabled ancestors block
descendant interaction; a modal dialog also confines focus and accessibility
actions to its surface and owned overlays.

`@onkeydown:preventDefault` suppresses the control's default action; `:stopPropagation` prevents ancestor handlers. They are independent and can be boolean expressions even without a local handler. Stopping propagation does not cancel editing/toggling. Input/change describe a change that already happened; prevent its initiating key/click when appropriate. Binding updates occur before the corresponding field notifications.

Retained closing overlays are disabled, hidden from the semantic tree and excluded from actions while `exiting=true`. A closing modal continues blocking its background and owned overlay scope until removal; keyboard actions and window shortcuts are suppressed during that phase. Focus returns when the surface is removed. Reopening cancels the exit, restores content focus and retains the original return target. Tooltips remain noninteractive throughout their visible lifetime. [Closing motion](11-Commands-Menus-and-Popups.md#closing-motion)

Retained content-transition layers and removed collection items are disabled and semantically hidden immediately. Only the current subtree participates in Tab traversal and accessibility actions. Focus moves to current content only when it would otherwise be hidden or removed; pure layout relocation keeps the focused element and local component state. [Content and layout motion](23-Transitions-and-Animations.md#content-and-layout-motion)

`onresize` is a noninteractive layout notification and still reaches disabled elements. Measuring a closed kept disclosure does not enable its actions. Collapsible content is disabled and semantically hidden as soon as it closes, including throughout retained motion. Expander and collapsible GroupBox headers receive focus only when closing hides focused descendants; custom compositions can set `Collapsible.ReturnFocus`.

## Focus, shortcuts, and pointer capture

Use `ElementReference.GetLocalPoint(windowX, windowY)` to map a logical window pointer position into an element's local border-box space through scrolling and transforms. It returns null for invalid coordinates, unavailable layout/input support or singular transforms; it does not clamp the point to the element's bounds. `GetBounds()` instead returns an axis-aligned window-space rectangle and cannot undo a rotation or scale.

Tab visits enabled visible focusable controls, with positive TabIndex values first, then zero/default in source order. Negative values exclude Tab but allow programmatic focus. Avoid positive indices when natural markup order can express the interaction. Hidden/disabled controls are skipped; opacity zero does not hide them from input.

After rendering, `await control.FocusAsync()` or `control.Element.FocusAsync(preventScroll: true)` requests focus. Programmatic/accessibility focus shows focus-visible styling. Pointer click normally focuses without focus-visible. Preserve a visible focus indicator in custom themes.

Inject `KeyBindings` for a window shortcut and retain its disposable registration:

```csharp
registration = Shortcuts.Register("f", KeyModifiers.Control,
    EventCallback.Factory.Create<KeyboardEventArgs>(this, () => searchBox.FocusAsync()));
```

The example assumes an assigned `TextField` or `TextArea` reference. Dispose the registration with its owner. Modifiers must match exactly; the most recent registration wins. Repeat is consumed without rerunning unless `allowRepeat: true`. Window shortcuts run before focused-control editing.

`CapturePointer()` during pointer-down routes later movement/release to a control beyond its borders. `ReleasePointerCapture()` releases it; cancellation, removal, and final release also clear capture. The API is useful for drag interactions, but it is not an OS file drag-and-drop subsystem.

Handle `@onlostpointercapture` to clear gesture state when capture is released, transferred, canceled by the host or cleared after disabling its owner. This cleanup event bubbles from the previous live owner after capture changes; `OriginalTarget` identifies that owner, coordinates/modifiers describe the last pointer input, and `Button` is `None`. It remains available while the owner is disabled or its modal surface exits. Repeated capture of the same owner and releasing an element that does not own capture send no extra notification. `Dial`, `Switch`, `FlipView`, `Draggable`, `ResizeHandle` and `ColorArea` clear their gesture state without emitting a completion callback for a canceled gesture. Values already previewed remain unchanged.

Check `ElementReference.IsPointerCaptured` before continuing a drag so cancellation or capture transfer cannot commit a stale gesture. The value is false for detached elements and custom interaction providers that do not implement `IElementInteraction.HasPointerCapture`.

Calendar headers traverse title, previous and next navigation before the current date; disabled buttons are skipped. Their visual order is supplied by the theme. Weekday labels remain decorative and do not create extra focus stops or announcements.

## Text input conventions

Text fields support grapheme movement, Ctrl+arrow word navigation, Shift selection, Home/End, deletion, clipboard, undo/redo, pointer selection, and IME composition. Multiline fields also support line movement and Ctrl+Home/End. Built-in context menus provide undo/cut/copy/paste/select-all, respecting password restrictions.

The backend supplies editing conventions through `TextInputPolicy`; event arguments retain the real modifiers. Do not hard-code one platform's modifier assumptions into a custom portable control. Real IME behavior still requires an interactive check with the intended input method.

Captured Slider and RangeSliderHandle gestures publish `:active` until normal release or capture cancellation. Movement reports ValueChanged/oninput; a normal release reports one onchange when the value changed. Explicit capture release, transfer, host cancellation, disabled/hidden state or removal ends the pressed gesture immediately, so later stale pointer events cannot change the range. Values already reported remain in the application model. Generic button release/lost-capture/click routing retains its ordering.

## Accessible names and semantics

Built-in controls supply roles and actions such as Invoke, Toggle, Value, RangeValue, Selection, SelectionItem, ExpandCollapse, Scroll, and text-field Text patterns. The Windows bridge publishes this semantic tree through UI Automation when requested by assistive technology. Linux desktop backends expose it through AT-SPI; the [Wayland](33-Linux-Wayland.md#feature-mapping) and [X11](35-Linux-X11.md#system-dependencies-and-behavior) guides describe backend-specific limits. DRM has no native accessibility provider.

Names are resolved from `AccessibleName`, then a `LabeledBy` target's text, then suitable control text/title, then Tooltip where needed. A text field never names itself from its current value:

```razor
<TextBlock Id="sensor-name-label">Sensor name</TextBlock>
<TextField @bind-Value="name" LabeledBy="sensor-name-label"
         AccessibleDescription="Used in exported reports" />
<Button OnClick="Refresh" AccessibleName="Refresh"><Icon Glyph="&#xE72C;" /></Button>
```

`AccessibleDescription` supplies a longer description. Tooltip supplies help text when it is not already the name. `Id` becomes AutomationId. Layout-only unnamed containers are transparent. Naming a container or enabling NSS `overflow-x`/`overflow-y: auto` or `scroll` exposes a group; scrolling is a capability independent of the role. Hidden bars and currently fitting content retain the capability. Position, extent and axis availability update as the scene changes, and disabled containers reject accessible actions. An explicit `TabIndex` makes a container focusable; use `0` to include it in keyboard traversal. Decorative images/icons should not introduce redundant announcements.

Windows exposes ScrollPattern from the overflow capability and reports changes to availability, scroll percentages and view sizes. Linux exposes scroll/page actions alongside any existing activation/selection role. The [scrolling guide](20-Layout-and-Scrolling.md#scrolling) describes the NSS layout and input policies, and the [Tasks playground](../../Samples/Nazor.Samples.Tasks/README.md) demonstrates them.

Lists, trees, tabs, radio choices, and menu items expose set position/size and selection where appropriate. Virtualized collections count the logical collection while realizing visible rows; they do not implement every UI Automation virtualization or scroll-item pattern. Password contents are not exposed.

## Announcements and live regions

Use a live region for visible changing status:

```razor
<Container @attributes="statusAttributes"><TextBlock>@status</TextBlock></Container>

@code {
    private string status = "Ready";
    private readonly Dictionary<string, object?> statusAttributes = new() { ["live"] = "polite" };
}
```

Polite announcements wait; assertive announcements interrupt. Initial content is not announced as a change. `InlineMessage` supplies live-region behavior, using assertive priority for warning and error feedback.

Inject `IAnnouncer` for a message without a corresponding visible status: `Announcer.Announce("Saved")`. `AnnouncementPriority.Assertive` requests interruption. It has no visible effect without assistive technology. Do not use a native desktop banner as a substitute for accessible in-app feedback.

## Validate the experience

Document List/ListItem differs from selectable ListView: it exposes list/item
structure and positions without selection. Heading exposes a level; Label,
Field and Fieldset/Legend provide naming, descriptions and required/invalid
state without generating validation or layout. Table exposes rows, columns,
spans and header relationships through Windows Grid/Table patterns and Linux
AT-SPI Table/TableCell interfaces. Splitter exposes an adjustable range; Meter
and determinate Progress expose read-only ranges. CheckBox and menu checkbox
items expose mixed state. Dialog surfaces expose their name, description,
modality and closing contract; tooltips supply supplementary descriptions.
See [semantic primitives](37-Semantic-Primitives.md) for keyboard and ownership
rules and the [catalog](09-Control-Catalog.md) for all current families.

Check keyboard-only traversal, labels, field descriptions, focus after popup/dialog closure, selection announcements, disabled states, and reduced-motion/high-contrast behavior. Use system colors in forced-color styles. Test with actual Narrator on Windows or Orca on the intended Linux desktop; semantic unit tests cannot certify speech order or native shell UI.

Example: [Gallery](../../Samples/Nazor.Samples.Gallery/README.md). API: [Nazor.Input](../ApiReferences/Nazor.Input.md), [Nazor.Accessibility](../ApiReferences/Nazor.Accessibility.md).

Repeating Button/Pressable actions use pointer capture while held and cancel on capture loss, leave, focused Escape or blur. Focused unmodified Enter/Space uses the same configured delay/interval; native OS key-repeat events are ignored and key release adds no action. Ordinary buttons keep their existing keyboard/click behavior when `RepeatInterval` is null. [Held action timing](11-Commands-Menus-and-Popups.md#held-repeat-actions)

---

[Guide index](README.md) · [API reference](../API.md)

Document tab actions are optional: Ctrl+T requires a visible add button and `OnAdd`, and Ctrl+F4 requires a selected enabled closable tab with `OnClose`. Nested tab strips own their keyboard actions; editor keyboard input cannot initiate reordering. Compact tab headers retain a plain-text accessible name. Closing returns focus only from the removed header/panel, to the nearest enabled tab or the available add action/strip fallback. [Tab contracts](12-Navigation.md#section-and-document-tabs)

NavigationView closes covering panes on focused Escape, scrim/toggle dismissal or focus leaving navigation for application content. Hidden trees and pane slots stay mounted but disabled/semantically hidden. Focus returns only when navigation hides its owner; route handlers can retain focus in application content. Top/compact root roving does not consume nested header editor arrows. [Navigation contract](12-Navigation.md#adaptive-application-navigation)
