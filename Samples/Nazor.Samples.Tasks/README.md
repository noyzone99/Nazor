# Tasks: application structure and scrolling

```powershell
dotnet run --project Samples/Nazor.Samples.Tasks -c Release
```

Add, search, filter, complete, select, and delete tasks, then open **Scrollbars** or **About** and return.
The singleton store retains tasks for the process lifetime; closing the app loses the data.
This standalone sample needs no test fixtures, private fonts, or headless launch mode.

## Learning purposes

| Purpose | Where to explore it |
| --- | --- |
| Compose an application with explicit service lifetimes, reusable components, templates and stable keys. | `Program.cs`, `TasksView.*`, `ItemList.nazor` and `TaskCard.nazor`. |
| Keep persistent session data separate from temporary view/settings state. | `ITaskStore`, conditional shell navigation and the local scrollbar preview fixtures. |
| Define application-wide scroll defaults and override them within a component. | `App.nazor.nss`, `ItemList.nazor.nss` and `ScrollbarsView.nazor.nss`. |
| Compare inherited window defaults with component-scoped overrides on ordinary Containers using NSS `overflow: auto`. | The two live task previews in **Scrollbars**. |
| Exercise scrollbar visibility, placement, geometry, track, buttons, proximity/wheel fades and custom animations. | The playground controls and its commented NSS rules. |
| Compare immediate/smooth scrolling, pixel/line/system steps and free fractional positions on both axes. | Wheel distance, animation controls, offset readouts and the fractional-position button. |
| Preserve keyboard, precise-input and reduced-motion behavior while customizing presentation. | Focusable previews/titles, the reduced-motion media query and the checks below. |

## Read the app from the outside in

| Path | Responsibility and reason |
| --- | --- |
| `Program.cs` | The composition root selects the platform, culture, color scheme, and service lifetimes before building the host. |
| `App.nazor` | The shell owns local view selection. A few screens do not need navigation history or a route table. |
| `_Imports.nazor` | Shared namespaces flow into child folders without repeating imports in each component. |
| `Views/TasksView.nazor` | The screen composes controls, reusable components, and templates. Its `.nazor.cs` partial class coordinates interactions. |
| `Components/SectionHeader.*` | Simple inputs and a `ChildContent` slot allow a caller to add its own summary or actions. |
| `Components/TaskCard.nazor` | Values flow down; typed `EventCallback` notifications report user intent to the parent. |
| `Components/ItemList.nazor` | A generic component owns scrolling and row identity while the caller supplies each item's presentation. |
| `Views/ScrollbarsView.*` | The playground owns temporary settings and two stable scroll surfaces; NSS supplies their behavior and appearance. |
| `Components/ScrollTaskRows.*` | Both surfaces receive the same finite read-only fixture. Deliberately wide rows exercise horizontal overflow; compact content fits both axes. |
| `Models/TaskItem.cs` | Immutable records hold data independently of controls and renderers. |
| `Services/` | `ITaskStore` separates data operations from UI; an in-memory implementation keeps the example focused. |

## Understand ownership before adding code

`TasksView` receives services through `@inject`; presentation components receive parameters and callbacks.
`Program.cs` registers the store as a singleton intentionally: conditional navigation disposes the view, but not its session data.
This is a lifetime choice for this example, not a rule that every service should be a singleton.

The store replaces a task record when its completion state changes. Selection and row keys use the stable `Id`,
so replacing data does not replace the identity of the corresponding controls.
Put `@key` on the outer repeated row: a key hidden inside an isolated template cannot identify sibling rows.
The generic list uses `RenderFragment<TItem>` and named fragments instead of depending on `TaskItem`.
It is a teaching list, not a virtualized list for large datasets.

Component event callbacks schedule rendering after the handler. Keyboard shortcut registrations, however, belong to the window's registry:
the view stores their disposable handles and removes them in `Dispose()` so opening the view again does not duplicate shortcuts.
`@ref` fields are assigned after rendering; shortcut lambdas access them when the user invokes the command.

The new-task field stops key bubbling, and the workspace handler checks input focus before acting on Delete.
Nested card actions stop click propagation so deletion does not also trigger row selection.
The labels include item titles so repeated actions remain distinguishable to screen readers.

## Follow the styles into their sidecars

Each component owns its layout in `.nazor.nss`. Selectors stay scoped, while window-level `--nz-*` properties inherit into children.
The base control theme still supplies focus, hover, and disabled states; no global stylesheet or manual registration is needed.
`min-height: 0` lets the list shrink inside its flex parent and scroll without pushing the footer outside the window.
The app explicitly chooses a light color scheme because its custom palette is light.

`App.nazor.nss` defines inherited autohide, thumb geometry/color and smooth scrolling on `:root`.
That is the application-wide policy in this sample, even though the file is the shell's sidecar: inherited values cross component boundaries.
`ItemList` specifies structural layout only, so another caller can replace the scroll policy without changing the generic component.
The playground's **Global autohide** and unselected **Custom track and thumb** demonstrate that inheritance; its other classes provide local overrides on the right preview without `::deep`.

The About view uses `repeat(auto-fill, minmax(260px, 1fr))` to wrap cards into the columns that fit.
Its badge is positioned absolutely inside a relatively positioned card, leaving the normal text flow intact.

Try Enter to add a task, Ctrl+N to focus its title, Ctrl+F to search, and Delete to remove a selected task.
Repeat using Tab and Shift+Tab, switch views to check state retention, and resize the window.

## Verify scrollbar behavior in the playground

Both previews are ordinary Containers with explicit NSS overflow. Controls override the right preview only; the left remains a reference for inherited Window defaults. The two previews use 32 local task records. Their 880px-wide table overflows both axes at normal window sizes.
They do not add, edit or delete anything in `ITaskStore`. Switching settings keeps both scroll containers mounted;
their keyed rows and offsets survive appearance changes. **Content fits** intentionally changes the extent and clamps offsets when necessary.
The normal Tasks screen remains the example of applying the inherited policy to a reusable list.

| Action | Expected result / property demonstrated |
| --- | --- |
| Start with **Global autohide**, approach the right or bottom edge, then leave. | The inherited bar fades in, waits, then fades out. It overlays content. |
| Choose **Visible**, compare **Overlay** and **Reserve space**. | Overlay preserves the viewport size; reserve allocates room beside the content on both overflowing axes. |
| Return to **Autohide**. | Placement is fixed to overlay; the reserve choice is retained for a later return to visible. |
| Choose **None**, then wheel or focus a preview and press PageDown. | Bars disappear, while content can still scroll. |
| Enable **Custom track and thumb**. | Width, inset, thumb radius/minimum length/color/opacity and the independent track radius/border/color change locally. Geometry/color transitions leave the content unchanged. |
| Enable **End buttons**. Hover, press and hold one; reach an end. | The button skin shows hover/pressed/disabled states. Presses use a 36px step and held buttons repeat after 400ms every 75ms. |
| Untick **Repeat while held**, then hold an end button. | A zero repeat interval permits one step per press without repeating. |
| Drag either thumb, click before/after it in the track, or use Shift+wheel. | Dragging is direct and clamps to bounds; track clicks page; Shift+wheel scrolls horizontally. |
| Compare **Smooth scrolling** on/off and **Default timing**, **Slow custom easing**, **Instant (0ms)**. | Content interpolation, fade durations/delay/easing, reveal proximity and wheel timeout are independently controlled by the documented preset rules. |
| Choose **24.5px per notch**, **1.5 lines per notch**, then **System settings**. | Steps use fractional logical pixels, 1.5 times the surface's 24px line height (36px), or the backend's distance/page settings. |
| Choose **Zero (wheel disabled)**, then drag or use a scroll key. | Wheel detents are consumed without moving; other scroll input remains available. |
| Press **Go to x=25.5, y=40.25**, then change the appearance or use a partial wheel delta. | Readouts show the actual fractional positions. No row alignment or wheel-step grid is imposed. |
| Wheel repeatedly, reverse direction, or grab the thumb during interpolation. | Requests accumulate; reversal starts from the visible position; grabbing stops interpolation at an arbitrary offset. |
| Enable **Hide while wheeling**. | `:wheel-scrolling` temporarily sets visibility to `none`, even near an edge. The chosen policy returns after the idle timeout. |
| Enable **Animate on wheel**. | A custom keyframe animates scrollbar opacity/color, independently of content opacity. |
| Enable **Content fits**, then restore it. | Both bars disappear when neither axis overflows; restoring the fixture makes both axes scrollable again. |
| Focus either preview and try PageUp/PageDown/Home/End; Tab through selectable task titles. | Scroll keys work on both surfaces; focus can reveal a title outside the viewport. |
| Use precise pixel input from a supported touchpad/backend. | The native pixel distance stays direct; it is neither scaled to detents nor delayed by smooth interpolation. |
| Enable reduced motion in the operating system. | Content interpolation and automatic fades become immediate; the decorative wheel animation is disabled by a media query. |
| Narrow the window below 840px, then scroll to the second preview. | Settings wrap, previews stack and the outer lab becomes scrollable. Wheel input at an inner limit can reach the outer scroll container. |
| Restore defaults, leave for Tasks, then return to Scrollbars. | Settings reset with the view; session tasks remain unchanged. |

`@onscroll` supplies the displayed offsets; there is no polling timer and no assumption that an animated destination has already been reached.
Real applications should select a consistent policy rather than expose every teaching switch. Keep wheel distances independent of line snapping,
respect reduced motion, and use virtualized controls for large datasets instead of copying this finite table into a production data grid.

See the [scrolling guide](../../Docs/Guide/20-Layout-and-Scrolling.md#smooth-scrolling) and
[NSS property catalog](../../Docs/Guide/19-NSS-Property-Reference.md#scrolling) for the full grammar, defaults and scrollbar properties.
