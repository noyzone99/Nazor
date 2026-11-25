# DevTools wire protocol 1.3

`Nazor.DevTools.Protocol` defines the detached data exchanged by tools, the optional Agent and debugger adapters. It references no Nazor runtime, native backend, UI or IDE assembly. The package version follows Nazor. The supported wire version is `1.3`, with minor-version negotiation during initialization. The optional Agent supplies a retained-tree exporter and local desktop pipe service. The standalone inspector supplies the external retained cache, linked trees and Styles/Computed/Frames panels. [Standalone inspector](DevTools.md) · [.NET API](ApiReferences/Nazor.DevTools.Protocol.md)

## Runtime and language compatibility

| Package asset | Contents | Consumers |
| --- | --- | --- |
| netstandard2.0 | Plain DTO records, method/capability/error constants | .NET Framework 4.7.2 IDE extensions, using their own serializer and messaging stack |
| net8.0 / net9.0 / net10.0 | The same DTOs plus `DevToolsJson`, `DevToolsPeer`, `RevisionCursor` and `LocalDevTools` | Optional Agent, standalone tools and compatible .NET adapter processes |

The `netstandard2.0` asset has no serializer package dependency. The Visual Studio extension serializes camel-case JSON with its own stack. JavaScript and Kotlin clients can use the wire contract directly. The Agent references Protocol. The production publish guard rejects the optional DevTools packages. [Production publication](Guide/28-Libraries-Packaging-and-Deployment.md#production-without-devtools)

## Framing and envelope

Messages use JSON-RPC 2.0 with ASCII `Content-Length: <decimal UTF-8 byte count>\r\n\r\n`, followed by exactly that many UTF-8 bytes without a BOM. The framing implementation serializes concurrent writes. Optional Content-Type accepts `application/json` or `application/vscode-jsonrpc`. The charset defaults to UTF-8 and accepts the explicit spellings `utf-8` and `utf8`. The receiver rejects duplicate or unknown headers, invalid CRLF, malformed lengths, non-UTF-8 bodies, arrays and other non-object messages. [JSON-RPC 2.0](https://www.jsonrpc.org/specification) · [Framing implementation](../Sources/Shared/DevChannel/JsonRpcConnection.cs)

This profile uses nonempty string request IDs up to 128 UTF-16 code units. Requests contain `jsonrpc`, `id`, `method` and object `params`. Notifications omit `id`. Responses contain `id` and exactly one of `result` or an error object with integer `code` and string `message`. `jsonrpc` must equal `2.0`. Envelope field names are exact and unknown envelope fields are rejected. Readers ignore unknown DTO fields within the wire limits. Required DTO fields must be present, including explicit `null` for required nullable fields. The .NET writer omits null fields marked `OptionalField`, which readers accept as null. The optional fields include component `rootId` and `declaringComponentId` and element `rootId`, `key` and `isRendered`. A null visibility means unavailable, while false identifies a hidden retained element. The receiver rejects duplicate JSON properties at every depth.

```json
{"jsonrpc":"2.0","id":"1","method":"initialize","params":{"version":{"major":1,"minor":3},"clientName":"Example tools","capabilities":["tree","selection"]}}
```

Malformed framing/UTF-8/JSON/envelopes close the connection; the peer does not attempt to find a new message boundary or trust an ID in malformed input. A syntactically valid request with bad parameters receives `InvalidParams`. Unknown methods receive `MethodNotFound`. There is no reply to a notification. Invalid/unknown notifications close the connection. Unrecognized late response IDs are ignored after envelope validation, allowing cancellation to race with a response.

## Limits

| Boundary | Limit |
| --- | --- |
| DevTools body | 1,048,576 bytes, both directions |
| Headers including final CRLF | 8,192 ASCII bytes; unique supported headers; at most 16 lines |
| JSON nesting | 32 |
| JSON values / properties per object | 65,536 / 64; property names at most 128 code units |
| Individual string / cumulative string text | 16,384 / 1,048,576 UTF-16 code units |
| Array / complete tree | 4,096 entries / 4,096 total component and element nodes |
| Frame batch / damage rectangles per frame | 120 / 128 |
| Capabilities / capability or identity string | 32 / 128 code units |
| Method name | 64 code units |
| Outgoing pending requests / incoming handlers | 32 / 16 |
| Pending notifications / concurrent writes | 32 / 64 |
| Request and write deadline | 10 seconds |
| Sequence/generation/revision/frame number | Nonnegative integer at most 9,007,199,254,740,991; identities/generations and snapshot/frame revisions start above zero |

Limits apply before dispatch. Body allocation and serialization buffering are capped. Over-capacity requests receive `Busy`. The local API also rejects excess outstanding work. Cancelled handlers retain their incoming slot until their task finishes, bounding the number of unfinished handlers. A failed or cancelled partial write closes the stream. Oversized or invalid outbound responses close the connection and finish all outstanding waits.

## Initialization and target identity

`DevToolsPeer.Supports(capability)` reports negotiated support after initialization. `DevToolsPeer.IsInitialized` reports local handshake completion, independently of UI responsiveness. The tools call `initialize` before target methods. `InitializeRequest` carries version, client name and capability strings. `InitializeResult` returns the selected version, peer name, capability intersection and optional `TargetState`. Major mismatch returns `VersionMismatch` and closes the connection. The minor version is the smaller offered value. Clients reject a different major, a higher minor or capabilities they did not offer. Initialization failure closes the initiating peer and releases pending waits. A stream is bound to one window/session at a time; switching target is an explicit session transition.

Capabilities are `tree`, `styles`, `geometry`, `frames`, `selection`, `inspection` and `debugger.fields`. Unknown capabilities can be advertised but do not grant any known operation unless both peers support them. Each target method/event requires its corresponding negotiated capability; `session/changed` belongs to the lifecycle. `debugger.fields` advertises adapter support, not permission to call target getters. Actual debugger-backed collection belongs to the adapter implementation.

`TargetIdentity` contains a PID and independent opaque process-instance, runtime-instance, application, window and session IDs. A PID alone cannot identify a restarted process. Component and element IDs identify retained instances within the session and node kind; they are not type names, markup `Id` attributes or process addresses. Retired identities must not be reused for replacements. A session cannot change its identity tuple. Reconnect/restart obtains a new session or a strictly newer capture generation before reusing cached state.

`TargetState` carries the identity, generation, `running`/`paused`/`exited` state and nullable stop ID. Every execution/target transition advances generation. Paused state requires a unique debugger stop ID; running/exited state has none. `RequestContext` carries session ID, generation and stop ID. Every target request must match all three. Hosts call `SetTargetState` before permitting queued callbacks after pause/step/continue or replacement, then publish `session/changed`. A host must enforce that ordering; constructing a peer does not observe debugger events automatically.

## Methods, results and events

DTO definitions are in [Contracts.cs](../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs). Names below are wire names; DTO JSON fields use camel case.

| Method | Params DTO | Result DTO | Capability |
| --- | --- | --- | --- |
| tree/get | ContextRequest | TreeSnapshot | tree |
| tree/browse | TreeBrowseRequest | TreeBrowsePage | tree.paged |
| styles/get | NodeRequest | StyleDetails | styles |
| geometry/get | NodeRequest | GeometryDetails | geometry |
| frames/get | ContextRequest | FrameBatch | frames |
| selection/get | ContextRequest | SelectionResult | selection |
| selection/set | SelectionRequest | SelectionResult | selection |
| inspection/set | InspectionRequest | SelectionResult | inspection |

Target params additionally carry `deadlineUtc`, supplied by the .NET peer. It is a UTC RFC 3339 expiry, at most ten seconds ahead (one second reception tolerance), checked at receipt and immediately before mutation. JavaScript `Date.toISOString()` and .NET round-trip UTC strings are accepted. Peers are local desktop processes sharing the clock; this does not define a remote/mobile clock synchronization protocol.

| Event | Params DTO |
| --- | --- |
| session/changed | TargetState |
| tree/changed | TreeDelta |
| tree/invalidated | TreeInvalidation |
| frames/added | FrameBatch |
| selection/changed | SelectionResult |
| $/cancelRequest | Object containing the original string request id |

`CaptureStamp` contains session ID, generation, `live`/`retained`/`debugger` provenance, nullable debugger stop ID and UTC RFC 3339 capture time. Debugger provenance requires a stop ID; live provenance has none. Retained data keeps its original stamp and must not be relabelled as a fresh live or current-stop capture. Typed results are validated on both sides against the request's session/generation/stop, before being returned to the caller. A valid wire response with invalid typed data is rejected without publishing it.

Tree snapshots contain roots, component records and element/text records. Components carry logical parent/children and output element links, independently of producing/declaring ownership on elements. Zero/multiple outputs, repeated component types, keys and framework-detail presentation are representable. A complete snapshot validates unique IDs, reciprocal parent/child links, roots, reachability, cycles and output/ownership references. Style data is detached selectors, declarations, computed values, origins and optional source coordinates. Geometry uses finite logical rectangles/edges and positive scale; margins may be negative. Frames contain ordered sequence numbers, causes, phase timings, counters and damage rectangles. No field carries runtime objects, delegates, reflection Types or arbitrary component properties. A collector must return `Unavailable` for uncaptured detail, never use empty data to disguise it.

## Paged live trees

`tree.paged` is negotiated independently of the complete `tree` capability. A page carries a capture stamp, a renderer revision, detached node summaries, a nullable continuation, the number of logical nodes visited, and the time spent reading on the target UI thread (`readMs`). The Agent visits at most 256 logical nodes and returns at most 64 results per browse operation. It checks an elapsed-time budget between visits. A page can contain zero results and a continuation when transparent wrappers or a search require more traversal. A continuation is opaque, single-use, scoped to the connection and request, and can expire after a relevant render or when the Agent evicts it. On `ResyncRequired`, start that request again. All existing framing limits still apply per response.

| Mode | Node and result |
| --- | --- |
| `children` | Supply a parent ID, or null for roots. Returns projected siblings in tree order. |
| `path` | Supply a node ID. Returns the node and its ancestors, from the node toward the root. |
| `details` | Supply a node ID. Returns one description with bounded retained parameters, attributes or text. |
| `outputs` | Supply a component ID. Returns its top-level physical output nodes in tree order. |
| `search` | Supply a nonempty `query` with at most 256 characters and a null node ID. Returns matching nodes from the retained tree. Search uses bounded text and attribute previews. |

`kind` is `component` or `element`. `includeFramework` controls whether projected component lists include framework components. Each `TreeBrowseNode` contains exactly one component or element description and a `hasChildren` hint. Summary records carry only the fields needed to identify the node. Clients use the page results to assemble the loaded hierarchy. Structural notifications identify changed nodes and parents, root changes, or an `all` flag when the notification budget is exceeded. Reload affected loaded branches; a gap can be recovered by reloading the roots. A paged client may still request a complete snapshot explicitly through `tree/get`.

Optional metadata includes `ComponentInfo.parameters` and `ElementInfo.elementId`, `attributes`, `classes`, `states` and `events`. Parameters and attributes are `PropertyValue[]`, with at most 128 unique names per record. Other metadata arrays contain at most 128 entries. Missing or null metadata means unavailable. Parameter origins are `supplied` and values come from retained direct parameter inputs without getter evaluation. Attributes use origin `attribute`. Event callbacks are represented by their attribute names. String, message and shape limits apply to all metadata.

`InspectionRequest(Context, Enabled)` toggles the existing app-side picker. `SelectionResult` adds optional `isInspecting` and a positive `revision`, monotonic within a connection generation. Selection notifications coalesce to the latest state; `selection/get` recovers it after notification overflow/full tree resync. Hover-only highlight moves do not publish unchanged selected identities/mode. Local cached navigation can diverge from app selection: repeated unchanged remote selection reads must not replace a locally chosen component. Initial null remote selection does not clear a retained local choice. A new picked identity or completed picker updates it; removed identities are still pruned by the tree.

## Ordering and retained data

Generation scopes capture validity. Tree and frame revisions order separate update streams within it. A snapshot is one complete message at a positive tree revision. A collector that cannot fit a complete bounded snapshot returns `Unavailable`.

A tree delta includes complete current root lists, component/element upserts, removals, `baseRevision` and `revision = baseRevision + 1`. IDs cannot be both updated and removed. Updates replace the identified record, including child/output lists. Frame batches also advance one batch revision, with strictly increasing frame numbers inside the bounded batch. Arrival order of RPC responses is independent of these data revisions.

`PropertyValue.longhands` and `winning` preserve declaration expansion and winning subsets. `rule` and `declaration` index the original trace. `StyleRule.level`, `order`, `specificity`, `media` and `mediaMatches` retain cascade context, including inactive media rules. `StyleDetails.customProperties` contains resolved custom values. Inline declarations use a final `element.style` rule. Missing optional fields mean unavailable. Style capture does not evaluate application getters.

`GeometryDetails.marginBox`, `paddingBox` and `contentBox` complement the required `borderBox`. These are actual transformed/scrolled viewport rectangles in logical units; edge widths remain local logical values. `scale` converts viewport coordinates to physical pixels. Never reconstruct inner transformed rectangles by deflating the border bounding box.

`FrameTimings.overlayMs` and `elapsedMs` describe overlay work and time since attachment. `FrameRecord.stylesRecomputed` records style work and `layoutCount` records layout computations. Optional `hasNodeCount` and `hasPaintCount` describe availability of the required count fields. An unavailable count is sent as zero and displayed as unavailable. An empty damage array with `hasDamage: false` means no recorded damage list. With `hasDamage: true` it means recorded zero damage. Damage rectangles use physical pixels. Optional numeric fields must be finite and nonnegative. Winning longhands must be unique members of the declared expansion, and rule references must fit the complete trace.

Optional renderer diagnostics include `renderer` (`GPU` or `CPU`), `requestedRenderer` (`AUTO`, `GPU` or `CPU`), `graphicsBackend`, `graphicsDevice`, `fallbackReason`, `renderWorkerFrame`, `renderWorkerMs` and `gpuFramesReplaced`. GPU paint timing measures UI recording and submission. Worker timing identifies the separately numbered last completed worker frame and includes CPU submission and presentation wait. GPU execution and physical scanout are outside these measurements.

Style/geometry are explicit live reads, rejected before scene/trace evaluation for paused targets. `frames/get` copies existing history and starts coalesced `frames/added` publication after the first successful read; it never requests a frame. Empty or over-budget history is unavailable, not synthetic or partial. A frame batch stamp identifies its sampling context/time, independently of each frame's number/elapsed/timings. Hidden clients may discard frame events and resynchronize with a full read when shown. Total timing is target paint-handler work, not native presentation completion or input-to-screen latency.

`RevisionCursor` rejects an old session/generation/stop or old revision as `Stale`. A missing base triggers `ResyncRequired` and blocks subsequent deltas until a complete snapshot arrives. Frame gaps require `frames/get`; accept its bounded retained batch with `resynchronize:true`. Do not treat a late missing delta as sufficient to clear a detected gap. A new target context requires a new cursor. Old display data may remain visible with its original provenance after disconnect, while no further changes are accepted.

The cursor only manages ordering and belongs to one serialized cache-update path; it is not a concurrent cache. The standalone UI cache stages each delta into a candidate tree, validates the resulting full topology and publishes the candidate and revision atomically. It never mutates the visible tree while parsing or validating a message. Its [retained model and connection client](DevTools.md#retained-model-and-boundaries) enforce this above the wire contracts.

## Cancellation, execution and errors

The existing `RequestAsync<T>(method, parameters, cancellationToken)` uses the protocol maximum deadline. The overload accepting `DateTimeOffset deadlineUtc` before the token uses that same absolute deadline on both ends; it rejects deadlines beyond the ten-second maximum and already-expired work. If the local timer fires early, the peer waits for the absolute expiry before reporting a local timeout. Standalone commands/captures use a two-second deadline including queued work.

Client cancellation stops the local wait and sends `$/cancelRequest`. It never interrupts the request frame itself: a request already queued for writing is written whole and then cancelled, so superseding one request with another keeps the connection open. Server handler tokens include cancellation, disconnect, deadline and target-generation transitions. Context is checked again after a handler finishes. Disconnection completes pending requests with `ConnectionClosed`; target change invalidates old waits with `StaleSession` or cancellation, depending on which event arrives first.

Handlers must return the task representing their whole operation and observe its token. Target mutations execute only through `DevToolsRequest.ApplyMutation`, inside the actual dispatcher callback. It checks the token, deadline, still-active handler, authoritative session/generation/stop and running state immediately before the synchronous action. Do not run a mutation directly before/after that call, or launch an untracked mutation task. A cancelled or completed lease cannot later apply an old queued mutation after continue. Cancellation cannot undo an action that already started; resynchronize after an ambiguous completion. Target mutation while paused returns `TargetPaused`; reads may use retained or debugger-backed data only when actually available.

| Code | Outcome |
| --- | --- |
| -32600 / -32601 / -32602 / -32603 | InvalidRequest / MethodNotFound / InvalidParams / InternalError |
| -32010 / -32011 | Unsupported capability / Unavailable data or target |
| -32012 / -32013 | StaleSession / TargetPaused |
| -32014 / -32015 | VersionMismatch / ResyncRequired |
| -32016 / -32017 | Cancelled or expired / Busy |
| -32018 / -32019 | ConnectionClosed / NotInitialized |

The stream peer sanitizes unexpected handler exceptions as `InternalError`; it does not send arbitrary exception data or stack traces. `ResyncRequired` is available to an exporter that cannot serve an incremental request; the local cursor reports the equivalent ordering outcome. Explicit caller cancellation uses .NET cancellation exceptions; remote cancellation/deadline and protocol failures use `DevToolsException` codes.

## Verification

```powershell
pwsh -NoProfile -File Scripts/Tests/verify-devtools-protocol.ps1
```

The script runs the suite on .NET 8/10, launches a headless stdio fixture with an independently written Node client, and on Windows round-trips a snapshot through .NET Framework 4.7.2 and its existing Newtonsoft.Json serializer. It preserves logs/JSON under a fresh `artifacts/devtools-dt04/` directory. A shorter focused run is `dotnet run --project Tests/Nazor.DevTools.Protocol.Tests -c Release -f net10.0`.

Tests cover Unicode byte lengths, partial reads, exact size boundaries, malformed/oversize envelopes, missing/duplicate fields, version negotiation, stale contexts, out-of-order responses, cancellation, queued mutation invalidation, bounded noncooperative handlers and disconnect cleanup. These are headless contracts/stream tests; native pipe discovery/permissions, debugger event wiring and retained UI behavior require their separate integration steps. Actual tree collection is covered by the Diagnostics and Agent suites below.

## Retained-tree export

`DevToolsSession.CaptureTree()` copies both retained trees on the owning UI thread after a render batch. Transparent regions are flattened; component hierarchy uses actual retained instances. Nonvisual wrappers can share output links with their descendants, and zero-output components remain selectable. Producing and lexical declaring identities remain independent for projected content. All roots belong to the supplied target window/session; `rootId` identifies the renderer root component within it. Unknown/external declaring instances have null identities.

Capture is atomic within one dispatcher turn and bounded to 4,096 combined component/physical nodes, 8,192 logical nodes including regions, depth 128, 16,384 links, 16,384 code units per string and 262,144 total copied code units. Failure returns no partial tree. No component property evaluation, unknown value formatting, style/layout update or JSON serialization occurs during capture. Hidden elements remain present; dirty style visibility is unavailable. Style, geometry and arbitrary component details are separate requests.

`TreeSnapshotExporter.Capture(TargetState)` produces a full snapshot and, after its first successful capture in a generation, a contiguous delta. Intermediate renderer batches may coalesce; exporter revisions count successful captures, independently of `TreeRevision`. Paused, exited, replaced and older-generation targets cannot trigger live collection. A new generation starts at revision 1 with a full snapshot. Removed IDs include every captured component and physical descendant, including zero-output components; replacements get new IDs.

`TreeSnapshotExporter.SerializeAsync` validates and serializes detached data on a worker. It reserves 4 KiB of the message budget for the envelope and returns `Unavailable` for wire limits. Capture advances the exporter baseline before serialization or delivery: after a failed/dropped send, publish a complete snapshot and resynchronize instead of sending a later delta against the receiver's old revision. An initial changes-only serialization returns `ResyncRequired`. The returned DTOs remain usable after disposal; callers must not mutate their arrays while serializing.

```powershell
dotnet run --project Tests/Nazor.Diagnostics.Tests -c Release
dotnet run --project Tests/Nazor.DevTools.Agent.Tests -c Release
```

These shared headless fixtures cover repeated/keyed instances, projected content, wrappers, zero/multiple outputs, hidden nodes, stable IDs, retirement/recreation, complete deltas, capture limits, unavailable/paused/stale cases and serialization after disposal. They do not start a listener or implement debugger-backed paused reads.

## Local desktop connection

The optional Agent uses `NamedPipeServerStream` in asynchronous duplex byte mode, independently of Hot Reload. A window explicitly calls `EnableDevTools()` after build opt-in. One endpoint belongs to one diagnostics attachment; the peer remains stream based. Current Agent capabilities are `tree`, `tree.paged`, `styles`, `geometry`, `frames`, `selection` and `inspection`; debugger fields are not advertised.

Modern Protocol assets expose these BCL-only discovery/client APIs; the standard DTO asset excludes them:

```csharp
public sealed record LocalDevToolsDescriptor(int Version, TargetIdentity Identity,
    string ProcessStartUtc, string PipeName, string ProcessStartToken)
{
    public string? ApplicationName { get; init; }
    public string? InspectorPath { get; init; }
    public string? InspectorVersion { get; init; }
}
public static class LocalDevTools
{
    public static string DiscoveryDirectory { get; }
    public static void PrepareDirectory(string directory);
    public static string PipeName(string directory, string sessionId);
    public static string Save(string directory, LocalDevToolsDescriptor descriptor);
    public static IReadOnlyList<LocalDevToolsDescriptor> Discover(string? directory = null);
    public static bool IsAlive(LocalDevToolsDescriptor descriptor);
    public static string ProcessStartToken(int processId);
    public static Task<DevToolsPeer> ConnectAsync(LocalDevToolsDescriptor descriptor,
        string clientName, string[] capabilities, CancellationToken cancellationToken = default);
}
```

Discovery format version 1 is separate from the negotiated wire version. Its camel-case JSON contains the full target identity, UTC process start time for display, OS process-start token and exact local pipe endpoint. Files are atomically published as `<sessionId>.json` after the listener exists. Normal detach/process exit removes the record. A crash may leave stale files; discovery ignores dead/reused PIDs and does not delete arbitrary records. `Discover` examines at most 256 files and 16 KiB per file; excess directory size returns `Unavailable`. Malformed, stale, oversized or linked records are ignored. A missing directory yields no candidates without creating storage.

Windows defaults to the current user's LocalApplicationData/Nazor/DevTools. New discovery directories have a protected DACL for the current user. Existing directories must exclude access grants to unrelated principals; system/administrator grants are permitted. Both pipe ends use `CurrentUserOnly`. Unix defaults to a short, private `nazor-dt-<username hash>` directory under the system temporary directory. Existing directory links or group/other permissions are rejected; new directories use 0700, descriptors and socket files use 0600. Unix pipe names are absolute socket paths inside this directory, at most 100 UTF-8 bytes. Custom locations must use trusted parents and meet the same privacy/path rules; unsafe storage fails rather than being silently chmod'd.

On the supported .NET runtimes, `CurrentUserOnly` checks peer credentials. The Agent sets the Unix socket mode explicitly inside its private directory.

`IsAlive` only checks the OS process instance. Linux uses boot ID plus `/proc/<pid>/stat` start ticks; an exact UTC `Process.StartTime` comparison is unsuitable because its boot-time conversion can differ between processes. Windows/macOS use the OS-backed start-time ticks. `ConnectAsync` validates descriptor shape, checks process identity, connects and initializes within three seconds, then compares the complete identity tuple returned by the peer. A transient closed connection during initialization is retried within that same deadline; application requests are never retried automatically. PID reuse, stale endpoints and session/window replacement cannot redirect the connection. Invalid descriptor data throws `InvalidDataException`/contract errors; dead or mismatched instances return `StaleSession`; cancellation/timeouts and I/O retain their .NET exceptions. Discovery and liveness are not evidence that a UI can respond.

After initialization call `tree/get` using the returned target context, then receive `tree/changed`. Full requests resynchronize any revision gap; a new connection generation always needs a full snapshot. Bounded coalescing captures the latest structure after each pending send. A failed send disconnects instead of publishing a partial update; no unbounded tree queue accumulates. Connection close leaves the client's detached data usable with its original stamp. The client owns `DevToolsPeer.Dispose()` and the visible retained-data model; the Agent does not implement a graphical cache.

```powershell
pwsh -NoProfile -File Scripts/Tests/verify-devtools-agent.ps1
```

The script runs a separate-process Agent fixture on both application frameworks against the modern client. The normal headless Agent suite runs the .NET 10 fixture; Wayland verification runs both. Tests cover discovery privacy, identity checks, both trees/deltas, resync/reconnect, replacement, normal exit/crash, partial and malformed framing, idle initialization, disabled startup, slow readers, unavailable UI/cancellation and inspection cleanup. Native macOS execution remains a separate acceptance requirement.

## IDE debugger data

DebuggerStatus, DebuggerReadRequest, DebuggerFields and DebuggerField are detached IDE/tool data contracts. They do not add a debugger service to the application Agent. An adapter validates the actual process, opaque session/component ID and current stop, and returns at most 128 raw field entries with explicit availability. Debugger field stamps use source debugger and the current stop, while structural/geometry caches retain their own earlier provenance. A raw field value is not a newly completed render/layout.

The inspector retains its Agent connection generation and overlays the independently observed debugger stop state. It cancels live requests and discards live notifications during pause; on continue it requires fresh Agent data. It revalidates the debugger stop after a field reply before publishing and rejects late values. Per-stop debugger object references never enter the UI cache; the cache receives detached strings only. Current Visual Studio automation runs inside the existing extension; the UI's Windows adapter has no IDE SDK reference. Other adapters require independently validated no-function-evaluation behavior.

Private local descriptors may include optional inspectorPath for a matching source/bundled tool executable. This metadata is not an Agent wire capability and does not change protocol 1.3 negotiation. Production exclusion still covers both Agent dependencies and any future tool assets.

## Bundled inspector and IDE activation

Local discovery optionally includes `applicationName` (the target entry assembly name, at most 256 characters without control characters), `inspectorPath` and `inspectorVersion`; these are private desktop launch metadata, not Agent wire capabilities. The standalone UI validates its bundle manifest and expected version. It owns a private per-session activation pipe and `.ui` process marker so app F12 and editor commands reuse one inspector. Closing the tool removes its marker; closing a tool leaves the app alive. App-launched inspectors carry `--owned` and an optional opaque `--owner-handle` token; activation preserves that ownership when editor commands reuse the same session. The inspector monitors private session discovery and process identity without calling the target UI dispatcher, closing when the owner session ends. Manual/IDE-only inspectors retain independent lifetime. There is no permanent broker.

VS Code's per-debug-session bridge uses separate `.ide` descriptors and a private named pipe/Unix socket, bounded Content-Length framing and a 256-bit secret stored only in private discovery storage. It translates safe coreclr DAP reads into the same debugger DTOs consumed by the UI. Descriptor, endpoint and target process-start identity are validated; every execution transition retires stop references. The bridge is separate from Agent messaging and never attaches another debugger. Other transports/hosts can implement the existing stream/adapter boundary without adding IDE dependencies to the runtime.
