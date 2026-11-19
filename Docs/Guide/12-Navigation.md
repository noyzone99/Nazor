# 12. Navigation

The SDK supplies `Nazor.Controls.Frame` as the outlet for routed pages.

`Frame` displays a page component. The window-scoped `NavigationManager` stores the current location and Back/Forward history. Routing changes content within a window; opening another OS window is a separate service operation.

| Use | When it fits |
| --- | --- |
| [`Frame`](09-Control-Catalog.md#collections-and-navigation) | Pages need addresses, route parameters, or Back and Forward. |
| [`Tabs`](36-Advanced-Controls-and-Composition.md#compose-content) | Related views share the same place in the window and switch through visible tab labels. |
| [Conditional components](../../Samples/Nazor.Samples.Tasks/App.nazor) | A small shell switches local views without addresses or history. |

## Section and document tabs

`Tabs.Presentation` defaults to `Navigation` for related sections. Use `TabsPresentation.Document` for independent documents: themes can draw a selected sheet, separators and a distinct panel. This does not add route history. `WidthMode` controls horizontal document headers: `Equal` shares width within theme limits, `SizeToContent` uses natural header widths, and `Compact` hides unselected labels/badges while retaining their accessible name and tooltip. Fluent's horizontal limits are 100–240 logical units; vertical/custom overflow composition remains a portable extension, rather than a claim of native TabView geometry.

`ShowAddButton=true` shows `TabsAdd`; `OnAdd` creates the application's new declaration and selects it. Without a callback the button is disabled and Ctrl+T is available to the surrounding application. A `Closable` tab needs `OnClose`; Ctrl+F4 closes the selected enabled tab, and Delete closes a focused tab. These actions are scoped to the nearest tab strip, including nested strips. Removing an active tab selects the nearest enabled page and returns focus only when the removed header/panel held it. Removing an inactive page preserves the selected page and remaps the bound index.

```razor
<Tabs Presentation="TabsPresentation.Document" ShowAddButton="true" OnAdd="AddDocument"
      CanReorderTabs="true" OnTabReorder="Move" KeepMounted="true" @bind-SelectedKey="selectedKey">
    @foreach (var document in documents)
    {
        <Tab @key="document.Key" Key="document.Key" Label="document.Title"
             Closable="true" OnClose="@(() => CloseDocument(document))">
            <DocumentEditor Document="document" />
        </Tab>
    }
</Tabs>
```

Here `documents`, selection, add/close methods and the editor belong to the application. `Move(ItemReorderRequest<Tab> request)` removes the model item at `OldIndex` and inserts it at the final `NewIndex`. Nazor preserves keyed declaration, header and kept-panel identity, and updates declaration order after external keyed moves. Migrate mutable strips to stable unique nonempty `Tab.Key` values **and** matching `@key` declarations before enabling `CanReorderTabs` or `OnTabDragOut`; invalid keys throw. Reorder requires `OnTabReorder`. A request does not mutate an application model that declines it.

Dragging an enabled header crosses a six-unit threshold before displaying the drop hint. Pointer release or Ctrl+Shift+arrows requests a move; Command+Shift+arrows is also accepted. Close parts and focusable/editable header descendants keep their own gestures. Escape, lost capture, disabled/removed sources and key changes cancel. Reorder hit targets are realized headers only; scroll the strip to reach other headers. There is no automatic edge pan. When reorder is enabled the row's `transform` belongs to measured layout motion; place custom transforms on header children.

`OnTabDragOut` is a separate opt-in callback receiving `TabDragOutRequest(Tab, X, Y)` on release outside the **actual header bounds**, in logical window coordinates. It works without reordering. Capture is released before the callback, and one release never requests both actions. The application decides whether to remove the model item and create a window; no live component transfer or OS drag-and-drop is implied. Keep selection/editor data in the application model when creating another window. [Runnable tabs sample](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/TabsSample.nazor)

## Adaptive application navigation

`NavigationView<TItem>` combines existing SplitView, TreeView, roving buttons, menus and measured indicators. Give it finite `Items`, `Children`, `ItemText`, decorative `ItemIcon`, stable `ItemKey` and optional `ItemDisabled`. Keys must be globally unique and non-null; cyclic/duplicate hierarchies and depth beyond 128 are rejected. Stable wrappers preserve expansion when application objects are replaced by new objects with the same keys. Removed keys are pruned, including their retained expansion state. Disabling a parent disables its known subtree.

| Pane mode | Presentation |
| --- | --- |
| Auto | Minimal through 640 logical units, Compact below 1008, otherwise Left; thresholds are configurable. |
| Left | Open pane reserves `OpenPaneLength` (320 by default). User collapse keeps the compact root strip. |
| Compact | Closed root strip reserves `CompactPaneLength` (48 by default); opening covers content. |
| Minimal | Closed pane reserves no width; opening covers content. |
| Top | Explicit horizontal root strip above content; does not automatically change at narrow widths. |

Adaptation observes the control's real available width. Bind `IsPaneOpen` for toggle/dismiss/adaptation requests. `SetPaneOpenAsync` and `TogglePaneAsync` use the same state. Pane header/footer stay mounted while hidden/disabled in a closed pane; the hierarchy's TreeView instance and application `ChildContent` also stay mounted across all modes. The persistent application `Header` belongs to content, below the Top pane; Top's pane row is 48 units and the content header is 52. Pane header/footer content in Top must fit that row; slots shrink before the root strip, which preserves a minimum target width and truncates long root labels to its measured viewport while retaining complete accessible names.

```razor
<NavigationView TItem="Section" Items="sections" Children="section => section.Children"
                ItemKey="section => section.Key" ItemText="section => section.Title"
                @bind-SelectedItem="selected" OnItemInvoked="Navigate">
    <Header><Heading Level="2">Workspace</Heading></Header>
    <ChildContent><Frame /></ChildContent>
</NavigationView>
```

The application defines `Section`, its data, `Navigate` and route content. A primary click or Enter invokes `OnItemInvoked` even on the selected item; invocation starts before `SelectedItemChanged`. Both callbacks are awaited without a delayed stale reselection. Route callbacks can focus newly presented content; closing navigation preserves that focus. Programmatic selection and hierarchy normalization do not invoke routes. An explicit null selection clears a reference-item model; a removed/disabled current item makes `CurrentItem` unavailable without forcing application content replacement or a user callback. Modified clicks cannot clear the current route. Call `Refresh()` for in-place hierarchy mutations that did not otherwise rerender the owner.

Left hierarchy keyboard behavior follows TreeView; Top/compact roots have one roving stop, axis arrows, Home/End and typeahead. Child categories open existing Menu/Submenu behavior. Top keeps every root horizontally reachable and reveals keyboard focus after layout, including mode changes at narrow widths; it uses scrolling rather than claiming WinUI's overflow-button policy. Hidden trees/pane slots reject input and accessibility actions. Header/pane-header editors retain their own arrow-key input.

Escape, scrim dismissal and the navigation toggle close covering panes and return hidden navigation focus to the toggle. Moving keyboard focus into application content closes a covering pane while preserving the destination focus. Compact/Minimal covering panes are light-dismiss surfaces, not a second modal dialog system. Main content remains independently enabled when navigation is disabled. [Runnable NavigationView](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/NavigationViewSample.nazor)

This is a portable ready navigation control, not the entire WinUI NavigationView API. Built-in search/settings/back stacks, footer item collections, infinite hierarchy loading, native top overflow policy and animated media are separate composition/capability projects. The application can compose search/back/settings into the provided slots. Exact installed WinUiGallery typography, timing, chrome and accessibility acceptance remains distinct from source-informed metrics and headless behavior tests.

## Routes and the frame

In `App.nazor`:

```razor
@window
@inject NavigationManager Navigation

<Button OnClick="@(() => Navigation.NavigateTo("/orders/42"))">Order 42</Button>
<Frame>
    <NotFound><TextBlock>Page not found.</TextBlock></NotFound>
</Frame>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "Orders";
    }
}
```

In `OrderDetails.nazor`:

```razor
@page "/orders/{Id:int}"

<TextBlock>Order @Id</TextBlock>

@code {
    [Parameter] public int Id { get; set; }
}
```

Call `Navigation.NavigateTo("/orders/42")` to show the details page. The initial location is `/`; provide a page for it or a useful `NotFound` fragment. `Frame` discovers pages in the application assembly; supply `Assemblies` to include page libraries.

Literal route segments win over parameters; constrained parameters win over unconstrained parameters. Constraints include `int`, `long`, `bool`, `guid`, `decimal`, `double`, and `datetime`. A final segment can be optional, such as `{Tab?}`. Values are assigned to matching `[Parameter]` properties without case sensitivity. Multiple `@page` directives can expose one page at several routes.

`NavigateTo<Page>(parameters)` also supports type-based navigation without a route string.

## History and state

Use `GoBack()` and `GoForward()` for history. Alt+Left/Right, platform Back/Forward keys, and mouse side buttons integrate with the same navigation. A new navigation clears the forward branch.

Each navigation creates a page instance. Returning to a previous location does not restore that page's old local fields. Store persistent editing/session data in a scoped service and identify records through route parameters or explicitly passed parameters. Query-string values are not automatically bound to page parameters.

The [sample pages template](02-Getting-Started.md#template-options) puts its navigation shell in `Views/` and the routed components in `Pages/`. The Counter stores its value locally, and Tasks keeps its checked items in a window-scoped service.

## Page transitions

`Frame.Transition` requests `Entrance` motion by default. Import `Nazor.Controls.Primitives` to select `None`, `Slide`, `DrillIn` or `DrillOut`. The active theme owns appearance and timing. Back navigation reverses the direction, and swaps drill-in/drill-out. Basic supplies immediate replacement; Fluent supplies page motion. The first page is shown immediately.

`FrameTransition` contains the current `FrameTransitionLayer` and at most one outgoing layer. The outgoing page retains its component state, is disabled and hidden from accessibility, and is disposed when the theme's `overlay-exit-duration` ends. A newer navigation drops the oldest outgoing page. If focus was inside the outgoing page, it moves into the replacement without scrolling; navigation initiated from an external menu keeps that menu's focus. Reduced motion removes outgoing content immediately. [Content and layout motion](23-Transitions-and-Animations.md#content-and-layout-motion)

Retained pages keep their ordinary subscriptions until disposal. Use an owner-scoped guard for page-local work and observe `Navigation.LocationChanged` when other subscriptions must stop as soon as a page leaves. Guard ownership uses the exact `NavigationEntry` reference, preserving Back/Forward history identity; equal addresses alone do not match.

## Navigation guards

Register a location-changing handler for unsaved work and dispose its registration when the page leaves:

```csharp
private IDisposable? guard;

protected override void OnInitialized() =>
    guard = Navigation.RegisterLocationChangingHandler(async context =>
    {
        if (isDirty && await Windows.ShowMessageAsync(
                "Leave without saving?", "Unsaved changes", MessageBoxButtons.YesNo)
            != MessageBoxResult.Yes)
            context.PreventNavigation();
    }, Navigation.Current);

public void Dispose() => guard?.Dispose();
```

The overload with `Navigation.Current` guards only that page entry while it is current. Migrate page-owned guards to this overload when enabling retained transitions. The original one-argument overload remains an application-wide guard and runs for every navigation until disposed.

Here the page injects `NavigationManager Navigation` and `IWindowService Windows`, owns `isDirty`, and implements `IDisposable`. A navigation guard protects frame navigation; it does not make native window close cancelable.

When navigation requests overlap while a guard awaits a response, the newer request supersedes the older one. The older request returns `false` and cannot change history after its guard completes. This also preserves redirects initiated by a location-changing handler.

For a runnable route example and its page, see the [Gallery frame](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/FrameSample.nazor) and [route preview](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/FrameRoutePreview.nazor). API references are available for [Nazor.Core](../ApiReferences/Nazor.Core.md) and [Frame](../ApiReferences/Nazor.Controls.md#frame).

## Dot page navigation

`PipsPager` selects an index independently of content. Bind `SelectedIndex` to the same model value as `FlipView<TItem>` or another presenter. `Count` is nonnegative, selection is clamped, and `CurrentIndex` is -1 when empty. Collection/parameter normalization does not raise a user-selection callback. `SelectedIndexChanged` and the element change event fire once for an actual user change.

```razor
<FlipView TItem="string" Items="pages" @bind-SelectedIndex="selected" />
<PipsPager Count="pages.Length" @bind-SelectedIndex="selected" />
```

`MaxVisiblePips` defaults to five and accepts 1–1000. Only that bounded window is realized even for very large counts; selection shifts the window. Each dot keeps its full collection position and localized name. Tab reaches the selected dot, axis arrows change pages, and Home/End reach the first/last. Optional previous/next actions stop at bounds and stay outside the dot Tab order. `ShowNavigationButtons=false` keeps dot keyboard navigation. `Orientation.Vertical` uses Up/Down; `Disabled` suppresses pointer, keyboard and programmatic selection. Focus follows the selected realized dot after a keyboard move/window shift; count shrink repairs focused dots without scrolling the owner.

Basic, Fluent and NazorLeaf own dot shape, hit geometry and states. Fluent uses source-informed horizontal 12×24 and vertical 24×12 targets, four/six-unit normal/selected dots and 24×24 navigation actions. Native font-glyph optical shape/installed-gallery parity remains an acceptance check. The finite bounded model intentionally omits infinite-page semantics and custom native button visibility modes. [Runnable composition](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/PipsPagerSample.nazor)

---

[Guide index](README.md) · [API reference](../API.md)

## Breadcrumb paths

`BreadcrumbBar<TItem>` receives `Items` in ancestor-to-current order. Its final node is the current location. `OnItemInvoked` receives the exact item and its zero-based index, including current-node activation; the application updates the path or route. The control never truncates the model implicitly.

```razor
<BreadcrumbBar TItem="Folder" Items="path" ItemKey="@(folder => folder.Id)"
               ItemText="@(folder => folder.Name)" OnItemInvoked="OpenFolder" />
```

Here `path`, `Folder` and `OpenFolder(BreadcrumbItemInvokedEventArgs<Folder> request)` belong to the application. Supply stable unique non-null `ItemKey` values to preserve header identity as nodes change; the default index key fits immutable paths. Null or duplicate explicit keys throw. `ItemTemplate` customizes decorative labels, while `ItemText` remains the accessible name and typeahead text; embedded interactive controls are not supported in labels.

The control measures natural label widths, keeps trailing nodes visible and moves leading ancestors into an ordered overflow menu. An unusually long current label clips inside the available width, retaining its full accessible name and tooltip. It has one header Tab stop; Left/Right, Home/End and typeahead move focus without navigation. Overflow menu keys follow ordinary Menu behavior. Width changes move focus to overflow when an ancestor disappears; path removal restores the nearest surviving node. Current-node keyboard focus is drawn inside its visible slot, including both edges at very narrow widths. Each node retains its full path position/count metadata. `Disabled` blocks pointer, keyboard and programmatic requests.

Basic, Fluent and NazorLeaf supply appearance. Portable overflow behavior uses measured trailing-node retention; it does not claim native BreadcrumbBar's complete animation/overflow policy. [Runnable resizable path](../../Samples/Nazor.Samples.Gallery/Examples/Navigation/BreadcrumbSample.nazor)
