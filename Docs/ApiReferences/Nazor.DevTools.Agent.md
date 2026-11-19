# Nazor.DevTools.Agent

Optional window attachment for `Nazor.Diagnostics`, available for .NET 8 and .NET 10. `Nazor.Sdk` executables include the package when `NazorEnableDevTools=true` and define `NAZOR_DEVTOOLS` for guarded attachment code; the default is false. Explicit Agent references also require the property in SDK consumers. Ordinary application/desktop packages do not reference it. Namespace: `Nazor.DevTools`. No session, collector, timer or listener starts merely by referencing the package. See [build inclusion and attachment](../Guide/27-Testing-and-Diagnostics.md#window-inspection) and [production validation](../Guide/28-Libraries-Packaging-and-Deployment.md#production-without-devtools).

## `WindowDiagnosticsExtensions`

Namespace `Nazor.DevTools`.

```csharp
public static class WindowDiagnosticsExtensions
{
    public static DevToolsSession? GetDiagnostics(this NazorWindow window);
    public static DevToolsSession EnableDiagnostics(this NazorWindow window);
    public static void DisableDiagnostics(this NazorWindow window);
}
```

| Method | Contract |
| --- | --- |
| `GetDiagnostics(window)` | Returns the currently attached diagnostic session, or null. Does not create a session or collect data. |
| `EnableDiagnostics(window)` | Requires the window's UI dispatcher and an open window. Creates one window-owned session, or returns the same session on repeated calls. Throws `InvalidOperationException` if a different instrumentation integration is attached; it does not replace it. Throws `ObjectDisposedException` for a closed/disposed window. |
| `DisableDiagnostics(window)` | Requires the UI dispatcher. Disposes this package's attachment and requests a repaint to erase its overlay. Repeated calls are harmless; a different integration is left attached. |

All methods throw `ArgumentNullException` for a null `window`. The window also disposes its attachment when replaced or disposed, before releasing renderer/scene/input resources. Diagnostic input consumption, selection, overlays and the latest 120 frame snapshots follow the existing [Diagnostics contracts](Nazor.Diagnostics.md). Neutral frame causes map by bit value; `WindowFrameCause.Overlay` maps to `FrameCause.DevTools` in diagnostic history.

Package setup and window attachment: [window inspection](../Guide/27-Testing-and-Diagnostics.md#window-inspection). This package supplies explicit window attachment and a local desktop pipe service. The package bundles the standalone graphical inspector for Windows/Linux x64; it executes separately with its own .NET 10 runtime. The Hot Reload development channel is independently controlled by the application's development-channel configuration/environment. Production publication must enable the [SDK production profile or property](../Guide/28-Libraries-Packaging-and-Deployment.md#production-without-devtools).

[Source](../../Sources/Tools/Nazor.DevTools.Agent/WindowDiagnosticsExtensions.cs)

The Agent references the optional [DevTools protocol package](../DevTools-Protocol.md); this supplies detached contracts and stream messaging, without starting an endpoint or collecting snapshots automatically.

## `TreeSnapshotExporter` and `TreeCapture`

```csharp
public sealed record TreeCapture(TargetIdentity Target, TreeSnapshot Snapshot, TreeDelta? Delta);
public sealed class TreeSnapshotExporter
{
    public TreeSnapshotExporter(DevToolsSession session);
    public TreeCapture Capture(TargetState target);
    public static Task<JsonNode> SerializeAsync(TreeCapture capture, bool changesOnly = false,
        CancellationToken cancellationToken = default);
}
```

`Capture` requires the session's UI dispatcher outside a render batch and a running target with the matching `SessionId`. One exporter binds to one target identity tuple; generations cannot go backwards. Paused/stop-bearing targets return `TargetPaused`, invalid identity/generation returns `StaleSession`, and exited/disposed/over-budget captures return `Unavailable`. No live builder is called for a paused target. Thread/batch misuse throws `InvalidOperationException`.

Each successful capture supplies a detached full snapshot and optional delta relative to the last successful capture. First capture/new generation starts at revision 1 with no delta. Later revisions advance by one even if multiple renderer batches occurred. Captured read-only diagnostics data backs comparisons independently of the returned protocol arrays. Snapshot roots, producing/declaring instance IDs, keys and nullable visibility follow the [wire contract](../DevTools-Protocol.md#retained-tree-export).

`SerializeAsync` always schedules validation/JSON work off the caller thread. Cancellation uses .NET cancellation exceptions. `changesOnly` without a baseline returns `ResyncRequired`; wire/payload limit failures return `Unavailable`. The baseline advances on capture, before serialization or transport delivery. A failed/dropped send requires a full snapshot resync. Detached DTOs can be serialized after session disposal; do not mutate arrays during serialization. No scheduling, automatic collection, listener or transport delivery is supplied by this class.

[Source](../../Sources/Tools/Nazor.DevTools.Agent/TreeSnapshotExporter.cs)

## `DevToolsBuilderExtensions`

Namespace: `Nazor` (provided by the optional Agent assembly).

```csharp
public static class DevToolsBuilderExtensions
{
    public static NazorApplicationBuilder UseDevTools(this NazorApplicationBuilder builder,
        string? discoveryDirectory = null);
    public static NazorApplicationBuilder UseDevTools(this NazorApplicationBuilder builder, DevToolsOptions options);
}
```

Call in Program.cs before Build, guarding only the call with NAZOR_DEVTOOLS. The existing using Nazor; resolves this extension in both enabled and disabled builds; no conditional namespace import is needed. Other per-window Agent APIs remain in Nazor.DevTools. Returns the same builder and composes with Diagnostics.ConfigureWindow. Build snapshots the callback; later configuration changes do not affect the built app. Each native main/later window explicitly attaches its own Agent on its UI dispatcher after initialization and before first show. Closing the window releases its Agent/session/discovery. Existing per-window EnableDevTools semantics remain, including idempotence and rejection of conflicting instrumentation. Null builder throws ArgumentNullException; invalid/private discovery setup failures propagate during window creation and dispose that window. UseDevTools alone creates no native window or listener before Run.

The optional Agent supplies this extension; ordinary runtime/desktop packages still exclude Agent/Diagnostics. It enables attachment, the configured shortcut and the native system-menu command where supported. IDE debugging commands are supplied by the editor extensions. See [Program.cs setup](../DevTools.md#start-and-connect).

[Source](../../Sources/Tools/Nazor.DevTools.Agent/DevToolsBuilderExtensions.cs)

## Local desktop attachment

```csharp
public static DevToolsAgent EnableDevTools(this NazorWindow window, string? discoveryDirectory = null);
public static DevToolsAgent EnableDevTools(this NazorWindow window, DevToolsOptions options);
public static DevToolsAgent? GetDevTools(this NazorWindow window);
public static void DisableDevTools(this NazorWindow window);
public static Task OpenDevToolsAsync(this NazorWindow window, CancellationToken cancellationToken = default);

public sealed class DevToolsAgent : IDisposable
{
    public DevToolsAgent(DevToolsSession session, string? discoveryDirectory = null);
    public LocalDevToolsDescriptor Descriptor { get; }
    public string DiscoveryFile { get; }
    public Task Completion { get; }
    public bool IsConnected { get; }
    public bool IsDisposed { get; }
    public void Dispose();
}
```

`EnableDevTools` requires the window's UI dispatcher and the same build opt-in as Diagnostics. It attaches Diagnostics if needed, starts one window-owned local Agent, and returns the existing Agent on repeated calls. `EnableDiagnostics` alone continues to create no endpoint. A failed Agent startup restores an initially unattached window. A directly disposed Agent can be started again against the same diagnostic session. `DisableDevTools` delegates to `DisableDiagnostics`, releasing both layers. `GetDevTools` returns the attached handle or null; check `IsDisposed` if a caller disposed the handle directly.

Construction and disposal require the session's UI owner. Explicit construction performs private discovery storage and listener setup synchronously; connected stream I/O and snapshot serialization run on workers. Direct/headless construction does not own the supplied session: dispose the Agent before disposing the session. Window attachment does that automatically. Synchronous disposal unsubscribes collection, clears inspection/highlight/selection, cancels networking and removes discovery without waiting on the UI thread; `Completion` permits observing asynchronous shutdown. No automatic attachment, environment activation or Hot Reload listener is involved.

Each Agent serves one active duplex byte-stream connection at a time, with tree, styles, geometry, frames, selection and inspection capabilities. Additional connections wait for the occupied endpoint or time out. Initialization must complete within three seconds. Every accepted connection gets a newer generation and starts with `tree/get`; reconnect/full resync never depends on the receiver's old baseline. A client disconnect clears inspection state on the UI dispatcher. Window close invalidates the connection and removes discovery; other window Agents remain active. Process/app IDs are shared within the Agent assembly instance; window and attachment IDs are independent.

Collection runs on the dispatcher; connected stream I/O, snapshot validation and serialization run on workers. A single pending UI capture is shared across connection generations, and one dirty flag coalesces render batches while a write is in flight. A pending UI capture returns `Busy` to competing collection work. Cancelled callbacks check their token and connection again before capture. A slow reader cannot queue one capture per render or block UI input. Protocol write deadlines close stalled connections; a failed structural notification closes the connection and reconnect obtains a full snapshot. A failed/ambiguous request or revision gap requires `tree/get` resync. Snapshot responses and notifications can race; receivers must validate revisions before publication.

`IsConnected` indicates an accepted transport, including initialization, not a responsive UI or fresh capture. Capture timestamps/revisions provide freshness. A blocked or debugger-suspended UI cannot supply new live data; cancellation/deadline stops the wait without resuming the target. Debugger-backed current-stop reads remain a separate integration. `selection/get`, `selection/set` and `inspection/set` expose the existing picker/selection APIs. Mutations run through the protocol lease inside the actual UI callback, validating identity, token and absolute deadline immediately before applying. Component selections do not invent a layout box; choose an output element for an overlay. Selection mode/identity updates are coalesced, and unchanged hover moves do not publish a new selected identity. See [linked navigation and picking](../DevTools.md#linked-trees-and-app-picking).

Private discovery, Unix/Windows access rules and the client connector are documented in the [local connection contract](../DevTools-Protocol.md#local-desktop-connection).

[Agent service](../../Sources/Tools/Nazor.DevTools.Agent/DevToolsAgent.cs) and [window attachment](../../Sources/Tools/Nazor.DevTools.Agent/WindowDiagnosticsExtensions.cs).

## `DetailExporter`

Namespace `Nazor.DevTools`.

```csharp
public sealed class DetailExporter
{
    public DetailExporter(DevToolsSession session);
    public StyleDetails Styles(TargetState target, string elementId);
    public GeometryDetails Geometry(TargetState target, string elementId);
    public FrameRecord[] Frames(TargetState target);
    public static CaptureStamp Stamp(TargetState target);
    public static Task<JsonNode> SerializeAsync<T>(T value, CancellationToken token = default);
}
```

Collection requires the diagnostics UI owner, matching session and running state. Paused/stop-bearing targets fail with `TargetPaused` before touching scene/style evaluation. Unknown/non-element IDs and missing boxes return `Unavailable`. Styles updates the scene on explicit request, then copies the existing trace including rule/declaration precedence, custom properties and origins; geometry copies the actual diagnostics box model. Trace budgets are 256 rules and 2,048 declarations; wire limits may reject a smaller complete capture. No partial result is published.

Frames copies at most 120 existing records without scene updates or frame requests. Empty history or any record exceeding 128 damage rectangles is unavailable. Actual style/layout work counters and phase timings are exported; node/paint totals use explicit unavailable flags. `SerializeAsync` validates/serializes on a worker, preserves the envelope budget and maps invalid/over-budget complete details to `Unavailable`. DTOs are detached from the scene/session.

The Agent uses its existing worker gate and one bounded dispatcher callback for these reads. FrameRecorded only marks pending work; it never traces styles or serializes on the paint path. Successful `frames/get` starts bounded/coalesced publication for that connection, with increasing batch revisions and only newly recorded frames. Failed history collection disables automatic frame publication until another explicit successful full read, leaving structural inspection connected. Disposal unsubscribes frame notifications and preserves existing overlay cleanup.

[Source](../../Sources/Tools/Nazor.DevTools.Agent/DetailExporter.cs), [panel usage](../DevTools.md#styles-computed-and-performance) and [wire detail semantics](../DevTools-Protocol.md).

## Materialized debugger roots

An explicitly attached Agent registers a DebuggerWindow in the public field DebuggerRoots.Windows. This optional Agent-owned registry exists for field-only IDE reads, not as a normal application API or an always-on runtime collector. Each entry exposes its SessionId, detached TreeJson and Components entries mapping a captured ID to the actual Instance reference. Field names form the tooling adapter contract. The registry uses the existing diagnostics IDs; it does not identify components by type name or raw address.

```csharp
public static class DebuggerRoots
{
    public static DebuggerWindow[] Windows;
}
public sealed class DebuggerWindow
{
    public readonly string SessionId;
    public DebuggerComponent[] Components;
    public string TreeJson;
    public string[] TreeChunks;
    public bool Updating;
}
public sealed class DebuggerComponent(string id, object instance)
{
    public readonly string Id;
    public readonly object Instance;
}
```

`Updating` marks the UI-side component/capture update. Debugger readers must reject an entry while it is true and reacquire its fields at each stop; false does not prove that worker serialization has finished or that the retained JSON is a current-stop capture. `TreeChunks` contains the same retained structural JSON in bounded pieces for debugger adapters with truncated string displays. Consumers read these fields without changing them.

Capture is deferred outside render batches with one pending dispatcher callback per window and worker-side bounded JSON serialization. It calls the structural exporter only, with no trace/layout recomputation, getter evaluation or frame request. The JSON is a retained complete structural capture with its original timestamp; current component fields can differ from it. Disposal unregisters the window and clears retained references. A serialization/capture limit is unavailable data, not a partial tree. IDE readers must reacquire roots and validate session/component membership at every stop.

Local discovery adds optional inspectorPath metadata: an existing bundled executable or source-checkout inspector path. InspectorLocator checks the app's Nazor.DevTools directory first, then known build output under a detected source checkout. Missing assets are reported at launch; no UI assembly is loaded into the app. Source verification uses artifacts/devtools-host. The optional Agent package supplies the bundled payload and F12 launch described below.

[Registry source](../../Sources/Tools/Nazor.DevTools.Agent/DebuggerRoots.cs), [locator](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/InspectorLocator.cs) and [debugger workflow](../DevTools.md#visual-studio-debugging).

## Bundled launch and debugger materialization

`UseDevTools()` also enables plain F12 on each attached window. `EnableDiagnostics()` alone does not register this action. F12 opens the window-specific standalone inspector and reuses it on subsequent launches, including IDE commands. It needs a running dispatcher. The direct `OpenDevToolsAsync` API requires dispatcher access and an enabled Agent; errors can be caught by the caller. Automatic F12 failures are reported to stderr/debug output without terminating the app.

The payload lives in the opted-in app's `Nazor.DevTools/` subdirectory, requires .NET 10, and stays outside its dependency graph. The package selects win-x64/linux-x64 assets; unsupported RIDs report NZB009. Production guards exclude the whole folder. [InspectorLauncher](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/InspectorLauncher.cs) validates the manifest/platform/version/runtime; the inspector validates itself for every entry point.

Only an enabled Agent materializes debugger roots. Its retained structural cache is coalesced to at most four captures per second with one pending capture/serialization; it never requests a frame. This cache can lag current execution. Supported IDE adapters read current fields directly at the current stop and never label the cache as a new completed frame. `TreeChunks` holds complete bounded pieces for adapters with truncated string displays. Disposal cancels pending capture, unregisters roots and clears component references.

## `DevToolsOptions`

Namespace `Nazor`, optional Agent assembly. Immutable configuration passed as `builder.UseDevTools(options: new() { ... })` or `window.EnableDevTools(options: new() { ... })`.

| Property | Default | Behavior |
| --- | --- | --- |
| `DiscoveryDirectory` | null | Default private per-user discovery storage. An active Agent's directory cannot be changed in place. |
| `ShortcutKey` | `"F12"` | Normalized key name, matched case-insensitively; null disables keyboard launch and releases F12 to app bindings. Empty/control-character names are rejected. |
| `ShortcutModifiers` | `KeyModifiers.None` | Exact required Control/Shift/Alt/Meta flags; unknown bits are rejected. |
| `ShowInSystemMenu` | true | Add a native **Open Nazor DevTools** command with the current shortcut hint where supported. Currently Windows; unsupported backends omit it. |

Explicit options update an active window's shortcut/menu without creating another Agent. Directory overloads ensure an attachment exists and reuse an existing attachment. Menu and shortcut actions share launch coalescing, asset/runtime diagnostics, window targeting and inspector reuse. The optional integration removes its native registration on direct Agent disposal or detachment. Initialization before native window creation defers menu installation until that window exists.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `DevToolsBuilderExtensions`

Namespace `Nazor`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DevToolsBuilderExtensions.cs)

| Declaration | Contract |
| --- | --- |
| `public static NazorApplicationBuilder UseDevTools(this NazorApplicationBuilder builder, string? discoveryDirectory = null)` |  |

### `DevToolsOptions`

Namespace `Nazor`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DevToolsBuilderExtensions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DevToolsOptions : IEquatable<DevToolsOptions>` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(DevToolsOptions? left, DevToolsOptions? right)` |  |
| `public static bool operator ==(DevToolsOptions? left, DevToolsOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DevToolsOptions? other)` |  |
| `public DevToolsOptions()` |  |
| `public string? DiscoveryDirectory { get; init; }` |  |
| `public string? ShortcutKey { get; init; }` |  |
| `public KeyModifiers ShortcutModifiers { get; init; }` |  |
| `public bool ShowInSystemMenu { get; init; }` |  |

### `DebuggerComponent`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DebuggerRoots.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DebuggerComponent` |  |
| `public readonly string Id` |  |
| `public readonly object Instance` |  |
| `public DebuggerComponent(string id, object instance)` |  |

### `DebuggerRoots`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DebuggerRoots.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DebuggerRoots` |  |
| `public static DebuggerWindow[] Windows` |  |

### `DebuggerWindow`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DebuggerRoots.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DebuggerWindow` |  |
| `public readonly string SessionId` |  |
| `public DebuggerComponent[] Components` |  |
| `public string TreeJson` |  |
| `public string[] TreeChunks` |  |
| `public bool Updating` |  |

### `DetailExporter`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DetailExporter.cs)

| Declaration | Contract |
| --- | --- |
| `public static Task<JsonNode> SerializeAsync<T>(T value, CancellationToken token = default(CancellationToken))` |  |

### `DevToolsAgent`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/DevToolsAgent.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DevToolsAgent : IDisposable` |  |
| `public DevToolsAgent(DevToolsSession session, string? discoveryDirectory = null)` |  |
| `public void Dispose()` |  |
| `public LocalDevToolsDescriptor Descriptor { get; }` |  |
| `public string DiscoveryFile { get; }` |  |
| `public Task Completion { get; }` |  |
| `public bool IsDisposed { get; }` |  |
| `public bool IsConnected { get; }` |  |

### `TreeCapture`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/TreeSnapshotExporter.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TreeCapture : IEquatable<TreeCapture>` |  |
| `public TreeCapture(TargetIdentity Target, TreeSnapshot Snapshot, TreeDelta? Delta)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(TreeCapture? left, TreeCapture? right)` |  |
| `public static bool operator ==(TreeCapture? left, TreeCapture? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TreeCapture? other)` |  |
| `public void Deconstruct(out TargetIdentity Target, out TreeSnapshot Snapshot, out TreeDelta? Delta)` |  |
| `public TargetIdentity Target { get; init; }` |  |
| `public TreeSnapshot Snapshot { get; init; }` |  |
| `public TreeDelta? Delta { get; init; }` |  |

### `TreeSnapshotExporter`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/TreeSnapshotExporter.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TreeSnapshotExporter` |  |
| `public TreeSnapshotExporter(DevToolsSession session)` |  |
| `public TreeCapture Capture(TargetState target)` |  |
| `public static Task<JsonNode> SerializeAsync(TreeCapture capture, bool changesOnly = false, CancellationToken cancellationToken = default(CancellationToken))` |  |

### `WindowDiagnosticsExtensions`

Namespace `Nazor.DevTools`. [Source](../../Sources/Tools/Nazor.DevTools.Agent/WindowDiagnosticsExtensions.cs)

| Declaration | Contract |
| --- | --- |
| `public static DevToolsAgent EnableDevTools(this NazorWindow window, string? discoveryDirectory = null)` |  |
| `public static DevToolsAgent EnableDevTools(this NazorWindow window, DevToolsOptions options)` |  |
| `public static DevToolsAgent? GetDevTools(this NazorWindow window)` |  |
| `public static void DisableDevTools(this NazorWindow window)` |  |
| `public static Task OpenDevToolsAsync(this NazorWindow window, CancellationToken cancellationToken = default(CancellationToken))` |  |

`OpenDevToolsAsync` binds the inspector to the calling window session and supplies its exported native owner token. F12 and native menu launch use the same path. Closing or detaching the owner closes its inspector; closing the inspector preserves the application. See [ownership and usage](../DevTools.md).
