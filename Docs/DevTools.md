# Standalone DevTools

<img src="../Sources/Tools/Nazor.DevTools/Assets/nazor-devtools.svg" alt="Nazor DevTools icon" width="64" height="64">

The external `Nazor.DevTools` application inspects a development window through the optional local Agent. It runs in its own process and scene with the NazorLeaf theme. The target application's styles and blocked UI dispatcher do not control the inspector. The shell requires .NET 10; target applications can use .NET 8, .NET 10 or .NET 11.

Inspectors opened from an application window (F12, its system menu or `OpenDevToolsAsync`) belong to that window. Windows uses native HWND ownership, X11 uses a transient-parent hint, and Wayland imports an xdg-foreign-v2 owner token when the compositor supports it. Closing that window or detaching its Agent closes its inspector, including after target-process exit. Each window owns only its own inspector; closing the inspector leaves the app running and reopening reuses the current session. Native ownership is a desktop policy request; when unavailable, session lifetime tracking still closes the inspector. A manually started or IDE-only inspector retains its last capture after disconnection until explicitly opened from an application window.

## Start and connect

Enable the Agent in the target's development build as described in [local desktop inspection](Guide/27-Testing-and-Diagnostics.md#local-agent-connection). Package inclusion alone does not start an endpoint: configure `builder.UseDevTools()` in Program.cs, or explicitly call `window.EnableDevTools()`. Production publication excludes this integration; see [production without DevTools](Guide/28-Libraries-Packaging-and-Deployment.md#production-without-devtools).

Configure attachment in Program.cs before Build:

```csharp
using Nazor;

var builder = NazorApplication.CreateBuilder(args);
builder.Application.UseMainWindow<App>();
#if NAZOR_DEVTOOLS
builder.UseDevTools();
#endif
await using var app = builder.Build();
return await app.RunAsync();
```

The Agent attaches to each main/later window; no App.nazor attachment is required. Enable the property only for intended development configurations, for example `<NazorEnableDevTools Condition="'$(Configuration)' == 'Debug'">true</NazorEnableDevTools>`. Explicit attachment remains required in optimized development builds too. Publishing a Release build with DevTools also requires `NazorEnablePublishValidation=false`; ordinary Release publication validates dependency and payload exclusion by default. The repository workspace already uses this Program.cs setup in Debug. Run the app and press plain F12 in the window to inspect. The inspector opens separately and connects to that exact window.

### Shortcut and system menu

`UseDevTools()` defaults to plain F12 and an **Open Nazor DevTools** entry in the native system menu where customization is supported (currently Windows, including the TitleBar menu and Alt+Space). The entry shows the configured shortcut and uses the same session-specific inspector/reuse path. Linux/Wayland keeps its compositor-owned menu unchanged.

Configure launch independently of application key bindings:

```csharp
#if NAZOR_DEVTOOLS
builder.UseDevTools(options: new()
{
    ShortcutKey = "D",
    ShortcutModifiers = Nazor.Input.KeyModifiers.Control | Nazor.Input.KeyModifiers.Shift,
    ShowInSystemMenu = true,
});
#endif
```

This changes activation to Ctrl+Shift+D and leaves F12 available to the app. Set `ShortcutKey = null` to disable keyboard activation; the system-menu command, IDE command and `OpenDevToolsAsync()` remain available. Set `ShowInSystemMenu = false` to omit the menu entry independently. Defaults are restored with a new default options instance. Keys use the same normalized names and case-insensitive matching as application key bindings; modifiers match exactly and automatic repeats never launch the inspector.

`DevToolsOptions` is in namespace `Nazor`, provided by the optional Agent. Use its `DiscoveryDirectory` property or the string-directory overload to specify a discovery directory. Calling `EnableDevTools(options: ...)` on an active window updates its launch configuration without replacing the Agent. Disable the Agent before changing its discovery directory. Direct Agent disposal, detachment and window close remove the menu registration. Both the menu action and the shortcut require a running app dispatcher.

The inspector window is titled `Nazor DevTools - <AppName>`, where `AppName` is the target entry assembly name. Discovery lists that name alongside each window and displays the process ID when the name is unavailable. The title follows target changes and keeps the last app name while disconnected with retained data. Clearing the capture restores `Nazor DevTools`. The inspector has its own NZ/component-tree icon for the native caption and taskbar button where the window backend supports them. Where the platform lets the app draw its caption, the inspector uses NazorLeaf's title bar with the four-leaf mark, the tool name and the inspected app. On Windows the bundled inspector uses its `NazorApplicationId` value, `org.nazor.devtools`, as its AppUserModelID. This groups inspector windows separately from other `dotnet` processes and associates them with the inspector icon.

Open **Settings** (the sliders button at the top right) and choose **System**, **Light** or **Dark** under Appearance. System is the initial choice and follows operating-system changes through the normal Nazor appearance service. A selection applies immediately to the inspector, independently of the target app, and is saved for future launches in `LocalApplicationData/Nazor/DevTools.appearance`. An absent or invalid preference falls back to System; unavailable storage does not prevent a session from changing appearance.

### Layout

The inspector follows the layout of browser developer tools and draws it with the [NazorLeaf theme](Guide/09-Control-Catalog.md#optional-theme-controls): the window's paper material, leaf-cornered panes, the seed that marks the selected row and NazorLeaf's fonts (Geist, Geist Mono and Bricolage Grotesque).

- One strip at the top holds the **picker** (left), the **Components**, **Elements** and **Performance** panels, and on the right the connection button (status light and app name), **Refresh capture** and **Settings**. The connection button opens the window selector with **Connect**, **Disconnect** and **Clear**.
- Components and Elements show a filterable tree pane on the left and a resizable details pane on the right, whose sections are grouped in boxed layers. Element rows read like markup (`<Button id="save" class="primary">`), text nodes as quoted strings and components as `Card key="left" #12`, with syntax colors in the browser convention (tags, attributes, strings) tuned to the NazorLeaf palette in light, dark and high-contrast schemes. A breadcrumb bar under the tree follows the captured ancestry.
- A banner under the strip reports picking, debugger pauses, disconnection (with **Reconnect**) and errors. A status bar reports connection state, capture age, node counts and the target identity.

With no capture, the panels show a connect page with the four-leaf mark, which lists discovered app windows and explains how to enable DevTools; while connecting, the mark turns into NazorLeaf's loading indicator. For compact windows (tested down to 1020 × 640), use the splitter to adjust the sidebar width; trees and details scroll separately. Cached navigation and capture provenance stay visible after a disconnect.

Keyboard shortcuts follow browser conventions: **Ctrl+Shift+C** toggles the picker, **Ctrl+F** focuses the tree filter, **F5** refreshes the capture and **Ctrl+Tab** moves to the next tab of the focused tab group.

From a source checkout, start the tools in a separate terminal, outside the target debugger's attached processes:

```powershell
dotnet run --project Sources/Tools/Nazor.DevTools -c Release
```

The optional Nazor.DevTools.Agent package includes framework-dependent inspector assets for Windows x64 and Linux x64. The SDK copies the host/RID payload into `Nazor.DevTools/` beside an opted-in executable, outside the app's assembly dependency graph. Install the .NET 10 runtime of the same architecture; the target app can use .NET 8, .NET 10 or .NET 11. A separate inspector installation or NuGet-cache path is unnecessary. On Linux, the selected desktop backend's [Wayland](Guide/33-Linux-Wayland.md) or [X11](Guide/35-Linux-X11.md) prerequisites still apply.

Plain F12 opens/connects the inspector for the invoking window. Repeated presses and IDE commands reuse that window's inspector; a different app window has its own inspector. With the default shortcut, Ctrl/Shift/Alt variants do not open it. Automatic key repeats are consumed for the configured shortcut without launching. Closing the tool leaves the app running; app exit leaves captured data readable. Missing/incompatible assets or runtime failures produce a development diagnostic (stderr/debug output); `await window.OpenDevToolsAsync()` exposes launch errors to custom application handling. Rebuild/restore matching packages after an asset error.

F12 requires a running app dispatcher. It cannot open the tool while the debugger has suspended the application. Use an already open inspector or the IDE command at a breakpoint. The tools never resume the debugger.

Source projects using the repository SDK build/copy the inspector automatically when enabled. The command above also starts an independent manual inspector for discovery.

1. Start the target application with explicit DevTools attachment.
2. On the connect page, select its window (refresh the list if it started later) and choose **Connect**. While connected, the connection button at the top right switches to another window.
3. Browse **Components** or **Elements**. Search, expansion, selection and linked navigation read the local capture.
4. Choose **Refresh capture** for a new complete tree. **Disconnect** retains the current data; **Clear** detaches and removes it.

Optional arguments follow `--` when using `dotnet run`:

```powershell
dotnet run --project Sources/Tools/Nazor.DevTools -c Release -- --discovery-directory <private-directory> --no-activate
```

The custom directory must match the Agent's directory and satisfy the [private discovery permissions](DevTools-Protocol.md#local-desktop-connection). `--no-activate` opens the window without requesting foreground activation. The tools exclude their own process from discovery and never attach an Agent to themselves.

## Visual Studio debugging

Install the [Nazor Visual Studio extension](Guide/26-Editors-and-Hot-Reload.md#install-editor-support). Start a Nazor app with `UseDevTools()` enabled, then choose **Tools > Nazor DevTools** during the debug session, including while paused. The inspector selects an attached app/window from private Agent discovery and lists other available windows. Repeated invocation reuses the session inspector, including one opened through F12. F12 also detects the attached supported Visual Studio instance. Visual Studio 2022 supports the [.NET 8 application setup](Guide/03-Projects-and-Builds.md#visual-studio-2022-and-net-8). The Visual Studio 2026 verification path targets .NET 10.

When running, the ordinary Agent supplies live data. At a debugger stop, the Components view remains navigable and selecting a component shows supported **current-stop fields** in its details sidebar. These are raw debugger reads of the real selected instance, including fields changed after the previous render. Getters, ToString and application functions are not invoked. Unsupported fields remain unavailable. The target may be inside an incomplete render/layout; retained trees, parameter inputs, traces and boxes keep their original capture rather than becoming a new completed frame.

Step/continue retires old debugger values. Earlier fields can remain visible as retained data; a later stop reacquires the selected instance. App overlays, picking, trace recomputation and other live UI operations stay disabled while paused. The inspector never resumes the target. An initial pause can use the Agent's already materialized structural tree without waiting for a suspended dispatcher; missing/oversize materialized data remains unavailable. Closing tools preserves the app; app exit preserves locally captured data.

The UI uses an isolated Windows COM automation adapter for the extension's Nazor.DevTools endpoint and references no IDE SDK. The extension owns the AD7 reader and validates process, session, component and stop. Its public breakpoint-spike endpoint remains a test harness. VS Code uses the same detached UI model through its own session bridge.

## VS Code debugging on Windows and Linux

Install the [Nazor VS Code extension](Guide/26-Editors-and-Hot-Reload.md#install-editor-support), Microsoft's C# extension and the .NET 10 runtime. Add `"nazorDevTools": true` to a `coreclr` launch/attach configuration and restart debugging. The Nazor extension disables implicit function evaluation, ToString and fast evaluation and enables raw values for that session. It validates the actual adapter options before allowing current-stop reads.

Use **Nazor: Open DevTools** from the command palette, including while paused. It selects the active debug process and lets you choose its window when necessary. Plain F12 in a running app opens the same external UI; the extension advertises its debugger bridge automatically, without requiring a language file to be open. An IDE command reuses the inspector already opened through F12.

The integration supports VS Code desktop on Windows and Linux with the `coreclr` adapter. If the adapter options cannot be validated, the inspector reports current-stop fields as unavailable and keeps retained navigation available. The field inspector is read-only.

## Linked trees and app picking

Components is the default view. Each row identifies the actual retained instance by type, session ID and optional key. Repeated types remain distinct. Components without output remain selectable and dimmed; components with several output roots show an output count, separate output links and have no single layout box. The component sidebar lists **Parameters**, **Rendered output**, **Rendered by** (the nearest ancestors first, as in React DevTools) and the instance facts.

**Framework components** (beside the tree filter, also in Settings) exposes the complete logical hierarchy. Hiding framework rows promotes their user-component descendants without changing captured IDs, relationships or selection. Search matches component names, instance IDs and keys, or physical names, markup IDs, classes, attributes and text. Matching descendants keep their ancestor path. A selection outside the filter remains retained; **Reveal selection** (the target button) clears the search, exposes its framework row when necessary, expands its ancestors and scrolls it into view. **Expand all** and **Collapse all** preserve selection. Rows remain virtualized at a fixed height.

Breadcrumbs follow the actual captured ancestry; long paths keep the nearest six ancestors. Component details link every physical output. The element sidebar header links the producing component (**Rendered by**) and the independently recorded declaring scope (**Declared in**), including projected content. Switching to Components reveals the producing instance. Switching to Elements automatically reveals a component's sole output; with several outputs, choose an explicit link. No one-to-one relationship is invented.

Component parameters show safely formatted **last directly supplied** values already retained by the renderer. They exclude cascading/default values and do not evaluate application getters. Unknown objects/delegates remain type placeholders. The element **Properties** tab shows captured attributes, classes, interaction states, event listener names, markup ID, key and visibility. Event callback instances never cross the wire. The inspector labels missing optional metadata as unavailable.

With a responsive, current capture and negotiated support, the **picker** (Ctrl+Shift+C) enables the app's existing inspector. Move over the app and click its control: application input is consumed, and the tools reveal the picked physical instance. Use **Rendered by** to reach its component and an output link to return to a specific element. Picking again or Escape in the target cancels inspection.

**Highlight in app**, beside the tree filter and in Settings, is on by default: choosing a node in a tree, a breadcrumb or a link draws the app's box-model overlay on it, as browser tools do for the selected node. Keyboard movement through the tree highlights only the row it settles on. An element highlights its margin, border, padding and content boxes; a component, which has no box of its own, highlights the boxes of its rendered outputs under one label with the component name and their combined size; a text node clears the overlay. Clearing the option removes the overlay and keeps the inspector's selection; the choice is saved in `LocalApplicationData/Nazor/DevTools.highlight`. With the option off, **Show in app** in the sidebar highlights the selection explicitly, and the crossed-eye button removes the overlay. Cached navigation makes no target query when the option is off or the capture is retained.

Picking/highlighting is unavailable while paused, disconnected, resynchronizing or unresponsive. These commands have the same absolute two-second deadline locally and on the remote mutation lease, and are never retried after continue. Cancellation cannot undo a mutation that already started. Current-stop debugger integration remains separate; the live Agent does not detect debugger stops automatically.

## Styles, Computed and Performance

Select a physical element in **Elements**, or follow a component's output link. The **Styles** tab of the element sidebar captures the existing style-engine trace and lists rules as a stylesheet, the strongest first: selector, source file, declarations with color swatches, and a line with cascade level, specificity and source order. Overridden declarations are struck through and tagged; shorthands that win only some longhands name them. Media conditions precede their rule, and inactive media dim it. Inline declarations appear as `element.style`. **Computed** lists resolved properties and custom properties, filterable by name or value, with their winning rule or inherited origin; **Show all** adds values that come from initial defaults.

Computed also shows the nested margin/border/padding/content diagram, as in browser tools, with a table of the viewport boxes in logical units and physical pixels. Edge widths are local logical values in top/right/bottom/left order. Each viewport rectangle comes directly from the diagnostics box model, including scrolling and transforms; physical pixel values multiply those rectangles by the captured target scale. Transformed inner boxes are captured independently, rather than derived from the border bounding rectangle. Hidden and text nodes may have no rendered element box.

Opening Styles/Computed or changing the selected physical element while one is visible captures that panel's data. The refresh button beside each panel's capture line explicitly repeats the read. Normal target frames do not retrace styles; cached tree navigation and hidden panels do not trigger detail collection. The Agent updates scene state only for an explicit style/geometry request. Unsupported capabilities, incomplete/over-budget traces and missing boxes are reported as unavailable while prior captures keep their original provenance.

**Performance** displays the latest 120 completed target frames as a timeline of bars against the 16.7 ms (60 fps) budget, green within it, amber up to 33.3 ms and red beyond, with frame count, average, maximum and over-budget totals. Choose a bar or a row of the frame list to see its phase breakdown. Each frame includes combined causes, styles/layout/display-list/overlay/raster timings, total paint-handler time, style recomputations, layout computations and copied physical damage rectangles. Total excludes the completion of native presentation and does not measure input-to-screen latency. Node and painted-node totals are currently unavailable because the target does not record those metrics. A missing damage list differs from a recorded empty list. Elapsed time is relative to diagnostics attachment; the history capture timestamp is the read time, not the last frame's wall-clock time.

Opening Performance requests retained history; later recorded frames stream through a bounded, coalesced update path. Reading history never requests a target frame. Selecting a historical frame pins its details until it leaves the 120-frame window. Hidden Performance notifications are discarded locally, and reopening the panel performs a complete catch-up read. The Agent may keep publishing after the first successful history request until disconnect; hidden views are not rebuilt for those updates.

Each detail panel shows its own original UTC capture/source/generation/stop context and labels retained or previous-context data. Paused, unresponsive and disconnected views remain readable. Live trace/box/history requests require a running target; pausing does not evaluate the style engine or resume the app. Current-stop component fields come from the [IDE debugger integration](#visual-studio-debugging); these detail panels retain their last capture while paused.

## Captures and lifecycle

The status bar shows connection/execution state, capture age, node counts and process/app/window identity. The capture panel records the original UTC timestamp, source, generation, structural revision and debugger stop ID when present. A live capture is the latest received snapshot, not a guarantee of continuous UI responsiveness. A transport handshake alone does not produce fresh inspection data.

A full request has a two-second deadline. If the target dispatcher is blocked, the shell shows **UI unavailable** and keeps cached navigation responsive. It never resumes the target. When a debugger adapter reports a pause, the last live tree becomes retained; current-stop debugger captures must carry that stop's own provenance. The Visual Studio bridge supplies pause/step/continue state; the Agent alone does not detect debugger stops automatically. Other IDE bridges require their own validated implementation.

Closing or disconnecting the tools leaves the target alive. Target exit or connection loss leaves the last capture readable and selectable, marked disconnected/retained. Reconnection requires a complete capture for its new generation. Switching to another app/window/session clears the previous target's data; an older connection cannot publish into the new one. **Clear** removes retained state and detaches.

## Retained model and boundaries

`InspectionStore` owns detached trees, local selection, style/geometry detail entries and frame history. It validates full DTOs and stages each ordered delta into a complete candidate tree before publishing atomically. Missing revisions, invalid resulting topology or dropped notifications require a complete snapshot. Late older responses cannot roll the tree back. Node identities are scoped by session, generation and debugger stop; retained data keeps its original stamp.

Reads return detached copies. The model retains at most 16 style and 16 geometry entries, and 120 frames; tree and wire limits remain those of the [protocol](DevTools-Protocol.md). Removed elements prune associated details; removed selections clear locally. The client serializes full requests and consumes a bounded eight-notification queue, resynchronizing after overflow. Discovery, transport and serialization run independently of the inspector UI dispatcher; UI updates marshal onto that dispatcher.

The shell references ordinary desktop runtime/theme packages and Protocol. Its assembly graph excludes Agent, Diagnostics and IDE SDKs. No new runtime dependency or auto-activation path is added to inspected applications.

The Agent advertises tree, styles, geometry, frames, selection and inspection. The inspector labels detail and picking unavailable when the peer does not advertise their capabilities. Debugger-backed reads require the separately connected IDE adapter.

## Verification

```powershell
pwsh -NoProfile -File Scripts/Tests/verify-devtools-shell.ps1
# Windows native startup, UI Automation and independent process lifetime:
pwsh -NoProfile -File Scripts/Tests/verify-devtools-shell.ps1 -Native
```

`-ArtifactsPath <path>` isolates managed build output, allowing verification while another source-built tools window holds its own DLLs open. Headless images are separated by OS under `artifacts/devtools-shell-images` and `artifacts/devtools-details-images`.

The script builds both target fixtures and runs the shell suite against .NET 8 and .NET 10 applications. Tests cover atomic cache updates, revision gaps/overflow, session changes, simulated pause/step/continue, real separate-process picking, cancellation and target hangs/exits, linked trees/filters/breadcrumbs, local pointer/keyboard actions and headless layout/rendering at 100%, 150% and 200% scale. `-Native` additionally starts the actual Windows shell, picks a fixture control, follows component/output links, cancels through Escape, reads Styles/Computed/Frames, verifies that repeated history reads leave the target frame count unchanged, selects retained rows and details after a target hang/exit and proves that closing the shell leaves the target responsive. Headless keyboard/scale evidence is distinct from foreground native keyboard, per-monitor DPI and Linux/macOS desktop acceptance.
