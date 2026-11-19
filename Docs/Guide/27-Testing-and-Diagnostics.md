# 27. Testing and diagnostics

## Layers of verification

Test business services as ordinary .NET code. Test component callbacks and state with the renderer. Test style/layout/display-list behavior in a headless scene. Use image comparisons for intentional visual contracts, then exercise native integration separately.

For an application, start with the headless scene and window inspection below. Keep the [framework contributor checks](#repository-verification) for changes to Nazor itself.

The [Counter headless implementation](../../Samples/Nazor.Samples.Counter/Headless.cs) creates a dispatcher, renderer, Skia renderer, and scene; adds a component root; updates layout; dispatches a click; pumps the dispatcher; and writes PNGs at several scales:

```powershell
dotnet run --project Samples/Nazor.Samples.Counter --no-launch-profile -- --headless --png artifacts/counter
```

## Build a headless scene

```csharp
var dispatcher = new UIDispatcher();
using var renderer = new Renderer(dispatcher);
using var graphics = new SkiaRenderer();
using var scene = new Scene(renderer, graphics.Fonts, graphics.Shaper)
{
    Viewport = new Size(320, 200),
};
scene.Styles.Theme = Nazor.Controls.StyleSheets.Default;
var root = renderer.AddRootComponent<Counter>();
scene.Update();
var image = graphics.RenderToImage(scene.BuildDisplayList(), 1f, scene.Background);
File.WriteAllBytes("counter.png", graphics.EncodePng(image));
```

This fragment assumes a `Counter` component and usings for Nazor, Nazor.Rendering, Nazor.Graphics, Nazor.Presentation, and Nazor.Skia. A scene needs an explicit theme when testing themed controls. When dispatching an asynchronous event, pump the dispatcher until completion before asserting the updated tree, then call `scene.Update()` before inspecting geometry or drawing.

For deterministic tests, use private fonts with system fallback disabled, `ManualAnimationClock`, `MemoryClipboard`, an explicit culture, fixed viewport/DPI, and controlled style preferences. Native tests can set `Activate=false` to avoid focus changes. Do not compare screenshots produced with unrelated fonts or cultures.

## Window inspection

Window attachment is optional. In a `Nazor.Sdk` executable, enable its build dependency and compilation symbol together:

```xml
<PropertyGroup>
  <NazorEnableDevTools>true</NazorEnableDevTools>
</PropertyGroup>
```

The default is false, including Debug builds. The SDK adds `Nazor.DevTools.Agent` at its own version and defines `NAZOR_DEVTOOLS` only when enabled. The Agent references `Nazor.Diagnostics`; ordinary runtime/desktop packages do not depend on either. Inclusion creates no session, collector or listener. Guard both the using and attachment code so the disabled build has no Agent reference. From code that owns a `NazorWindow`, enable diagnostics on the UI thread:

```csharp
#if NAZOR_DEVTOOLS
using Nazor.DevTools;
#endif

#if NAZOR_DEVTOOLS
var session = window.EnableDiagnostics();
session.SetInspectMode(true);
session.SelectionChanged += id =>
{
    if (id is not { } selected) return;
    var node = session.Get(selected);
    var cascade = session.Trace(selected);
    var box = session.Box(selected);
};
#endif
```

`Roots`, `IdOf`, and `Get` expose stable IDs while nodes live. Trace describes matched declarations, winners, and computed-value origins. Box provides logical/physical geometry. Highlight and inspect mode add overlays; primary click selects without activating app controls and Escape ends inspection. Removed nodes lose selection/highlight.

`Frames` contains up to 120 recent frame snapshots with phase timing, causes, counters, and damage. `FrameRecorded` reports additions. Timing measures paint work rather than native presentation. Inspection does not request frames merely to refresh history; idle counters remain idle. `DisableDiagnostics()` removes overlays/subscriptions. The separate [standalone DevTools inspector](../DevTools.md) builds on these sessions and the optional Agent; direct session construction does not launch it.

For application-window inspection, reference `Nazor.DevTools.Agent`. Its `EnableDiagnostics()` and `DisableDiagnostics()` extension methods are in namespace `Nazor.DevTools`; `window.GetDiagnostics()` returns the current attachment. Each window owns one attachment and disposes it when detached or closed. Request an overlay repaint with `RequestFrame(WindowFrameCause.Overlay)`. Diagnostic history uses `Nazor.Diagnostics.FrameCause`, including its `DevTools` value.

For temporary inspection, use `dotnet build -c Release -p:NazorEnableDevTools=true`; optimized development builds are supported. To publish that development build, also set `-p:NazorEnablePublishValidation=false`, since Release publication enables the exclusion checks by default. An existing explicit Agent reference also requires the property in SDK consumers; remove the reference to produce the disabled artifact. Source applications importing `build/Nazor.targets` use the same property with a source Agent reference. Bare `Microsoft.NET.Sdk` consumers without that import must select the package and matching conditional compilation themselves.

Headless callers can keep a direct `Nazor.Diagnostics` reference and construct `DevToolsSession(renderer, scene, input)` without an Agent or application host. The ordinary window exposes only the neutral [instrumentation pipeline](../ApiReferences/Nazor.Application.md#type-iwindowinstrumentation). For deployment, use the [production publish guard](28-Libraries-Packaging-and-Deployment.md#production-without-devtools), which also rejects direct/headless diagnostic dependencies. The existing Hot Reload development channel remains separate: `builder.Diagnostics.DevChannel` / `NAZOR_DEVCHANNEL` can enable it without a diagnostic session or Agent. Set `builder.Diagnostics.DevChannel = false` explicitly when that endpoint must stay disabled.

Contributors can run the separate Visual Studio breakpoint feasibility fixture with [verify-devtools-breakpoints.ps1](../../Scripts/Tests/verify-devtools-breakpoints.ps1). It deploys the source extension to a dedicated experimental IDE profile and checks real paused field reads without target getter/ToString execution. This test harness is not an application attachment API or a graphical inspector; see the [script reference](../../Scripts/README.md) for prerequisites and options.

## Detached structural capture

For both component and element navigation, call `session.CaptureTree()` on the UI dispatcher after rendering. Post a callback from `TreeChanged`/`BatchCompleted`; synchronous capture inside a render batch is rejected. The read-only result includes actual component instances, logical child order, physical outputs and independent producing/declaring identities. It survives subsequent renders and disposal. Hidden nodes remain in the tree; nullable visibility reports unavailable when styles are dirty. Capture does not force style/layout updates or invoke component getters and unknown `ToString` implementations.

Capture returns a complete bounded tree or throws `TreeCaptureLimitException`; it never presents an uncaptured subtree as empty. The [Diagnostics API](../ApiReferences/Nazor.Diagnostics.md#detached-tree-types) lists budgets and DTOs. Existing `Get`/`Box`/`Trace` are live detail APIs and may update scene state; they are not breakpoint readers.

With the optional Agent, use `new TreeSnapshotExporter(session).Capture(targetState)` for protocol snapshots/deltas and `await TreeSnapshotExporter.SerializeAsync(capture)` for worker serialization. The target identity must use `session.SessionId` and a valid running generation. Retain one exporter per attachment. A new generation requires a full snapshot; a lost delta/send requires resynchronization. See the [Agent API](../ApiReferences/Nazor.DevTools.Agent.md#treesnapshotexporter-and-treecapture) for lifecycle contracts. No graphical inspector or automatic connection is started by these APIs.

Focused checks are `dotnet run --project Tests/Nazor.Diagnostics.Tests -c Release` and `dotnet run --project Tests/Nazor.DevTools.Agent.Tests -c Release`.

## Local Agent connection

Prefer `builder.UseDevTools()` in Program.cs before Build, guarding only the call with `NAZOR_DEVTOOLS`. The extension is in namespace `Nazor`, available through the ordinary `using Nazor;`. This attaches an Agent to each main/later window after native initialization and enables plain F12 to open the bundled .NET 10 inspector; see the [complete startup example](../DevTools.md#start-and-connect). Use `window.EnableDevTools()` on the UI dispatcher for explicit individual window attachment. Repeated calls return the same Agent. `EnableDiagnostics()` remains local inspection without a listener. Window close or `DisableDevTools()` disposes the attachment, clears inspection/overlays and removes discovery. Each window has its own endpoint and identity. The existing build opt-in remains required; neither referencing a package nor setting an environment variable starts the Agent.

An external .NET 8/10 client can use Protocol without an Agent/runtime reference:

```csharp
var descriptor = LocalDevTools.Discover().Single();
using var peer = await LocalDevTools.ConnectAsync(descriptor, "Example tools", [DevToolsCapabilities.Tree]);
var target = peer.RemoteTarget!;
var context = new RequestContext(target.Identity.SessionId, target.Generation, target.StopId);
var tree = await peer.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree,
    DevToolsJson.Write(new ContextRequest(context)));
```

This example uses `Nazor.DevTools.Protocol`. Choose the desired window rather than `Single()` in multiwindow tools. Consume `tree/changed` through `NotificationHandler` and apply the [revision/resync rules](../DevTools-Protocol.md#ordering-and-retained-data). Keep old captures labelled with their original timestamp after disconnect. A connected pipe or live PID does not prove the UI is responsive. New capture waits expire/cancel while a dispatcher is blocked; the Agent never resumes a paused target.

The connection supplies structural trees and explicit style/geometry/frame reads. The [standalone DevTools shell](../DevTools.md) provides F12/IDE launch, automatic targeting and manual connection and cached Components/Elements navigation in a separate process. Linked search, filters, breadcrumbs, component/output navigation and explicit app picking are available. Styles, Computed/box model and Frames are available with original capture provenance; see the [panel guide](../DevTools.md#styles-computed-and-performance). The [Agent API](../ApiReferences/Nazor.DevTools.Agent.md#local-desktop-attachment) and [private discovery/client contract](../DevTools-Protocol.md#local-desktop-connection) describe permissions, limits, ownership and lifecycle. Run `pwsh -NoProfile -File Scripts/Tests/verify-devtools-agent.ps1` for both application framework fixtures.

## Contributor checks

### Repository verification

These checks are for contributors working in a Nazor checkout. See the script reference for prerequisites and outputs. [Technical script reference](../../Scripts/README.md)

```powershell
pwsh -NoProfile -File ./Scripts/Tests/verify.ps1
```

From a repository checkout, the script builds the solution, runs executable test projects, checks benchmarks, runs Counter headlessly, builds editor artifacts, and verifies packaged SDK consumers. `-SkipSdk` omits package/SDK verification; `-HotReload` adds the end-to-end watch check and runs SDK verification even with `-SkipSdk`. `-RealInput` enables interactive SendInput checks that temporarily take foreground.

Native Windows pixel checks require an unlocked interactive desktop. The window test harness keeps the display active while a test window is alive; presentation checks skip explicitly when the input desktop is unavailable. A retained GPU frame alone does not verify that the desktop displayed it.

Run a relevant suite directly during focused changes:

```powershell
dotnet run --project Tests/Nazor.Core.Tests -c Release
dotnet run --project Tests/Nazor.Rendering.Tests -c Release
dotnet run --project Tests/Nazor.Application.Tests -c Release
```

These are xUnit v3 executable projects. Native tests can briefly create real windows. System clipboard tests are opt-in with `NAZOR_CLIPBOARD_TESTS=1` because they replace the clipboard contents.

### Visual baselines

Rendering tests compare against `Tests/Nazor.Rendering.Tests/Baselines`. Update only for a deliberate visual change, inspect the generated images, then remove the environment override:

```powershell
$env:NAZOR_UPDATE_BASELINES = '1'
try {
    dotnet run --project Tests/Nazor.Rendering.Tests -c Release
} finally {
    Remove-Item Env:NAZOR_UPDATE_BASELINES -ErrorAction SilentlyContinue
}
```

An updated baseline records an expectation. Review clipping, scrolling, focus states, and text at the affected sizes and scales. Windows and Linux have separate raster baselines to account for differences such as DirectWrite and FreeType font rasterization. [Rendering test coverage](../../Tests/README.md)

### Performance and hot-reload tests

```powershell
dotnet run --project Tests/Nazor.Benchmarks -c Release -- --out artifacts/benchmarks.json --check Scripts/Tests/benchmarks.json
```

Benchmark cases include typical scenes, a large list, and motion. `--samples N` changes repetitions; `--counters-only` skips timing thresholds. Compare optimized builds and inspect work/allocation counters before attributing a timing change to the framework.

The standard scene benchmarks use Basic. Interactive samples include the input action, queued component work and painting. List cases check viewport movement, bounded realized rows and both requested jump endpoints. Their counters sum work across the operation; commands report its largest display list. Style work uses differences of `StyleEngine.TotalRecomputedCount`, so lazy lookups during input are included even if a later idle update resets `LastRecomputedCount`. The component/input time column also includes lazy style resolution outside explicit scene updates; total time includes both paths.

For native CPU/GPU measurements on the same 80-card workload, run `pwsh -NoProfile -File Scripts/Tests/verify-renderers.ps1`. It requires a usable desktop and hardware GPU, saves JSON under `artifacts/renderers`, restores the caller's renderer environment setting and fails if strict GPU startup is unavailable. `--native --samples N --out path.json` selects native mode in `Tests/Nazor.Benchmarks`; choose the renderer with `NAZOR_RENDERER`. It measures UI paint and CPU submission/presentation wait separately. Request-to-completion observations include UI dispatcher notification and do not measure physical scanout or GPU execution. These measurements compare CPU and GPU renderers within Nazor and use separate thresholds from the headless regressions.

Hot-reload tests can use `HotReloadManager.ApplyUpdate`, renderer refresh, stylesheet replacement, and `WatchSession.Refresh`. Tests changing shared registries must serialize access and restore entries. See existing compiler/style tests before adding global-state tests.

Native Snap UI, real IME, Narrator speech, shell notifications, and mixed-monitor behavior need interactive verification on the target Windows configuration.

API: [Nazor.Diagnostics](../ApiReferences/Nazor.Diagnostics.md), [Nazor.Presentation](../ApiReferences/Nazor.Presentation.md), [Nazor.Renderer.Skia](../ApiReferences/Nazor.Renderer.Skia.md).

### Compare rendering across frameworks

The Windows comparison runs matched text, paint, and layout updates in Nazor and Avalonia. It checks geometry and screenshots, records the framework and backend versions, and exports completion times, process CPU, allocations, memory snapshots, and raw samples. The completion events account for different amounts of backend work, so interpret the timings together with the other measurements. Share the raw results and environment when reporting a result for a specific workload. [Comparison protocol](../../Tests/RendererComparison/README.md)

---

[Guide index](README.md) · [API reference](../API.md)
