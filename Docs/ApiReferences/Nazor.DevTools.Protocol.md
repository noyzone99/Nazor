# Nazor.DevTools.Protocol

Modern transport APIs are available in the .NET 8/9/10 assets; .NET 11 applications select the compatible .NET 10 asset. The .NET Standard 2.0 asset preserves the DTO contract for IDE adapters. Standalone DevTools and language services run on .NET 10 LTS.

The signatures below describe the public and protected types and members declared by this package. Inherited members are documented with the declaring type.

The DTOs and constants are available in .NET Standard 2.0 and .NET 8/9/10 assets. The stream transport and local desktop discovery require .NET 8 or later. See the [wire protocol](../DevTools-Protocol.md) for message formats and limits.

## `CaptureStamp`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CaptureStamp : IEquatable<CaptureStamp>` |  |
| `public CaptureStamp(string SessionId, long Generation, string Source, string? StopId, string CapturedAtUtc)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(CaptureStamp? left, CaptureStamp? right)` |  |
| `public static bool operator ==(CaptureStamp? left, CaptureStamp? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(CaptureStamp? other)` |  |
| `public void Deconstruct(out string SessionId, out long Generation, out string Source, out string? StopId, out string CapturedAtUtc)` |  |
| `public string SessionId { get; init; }` |  |
| `public long Generation { get; init; }` |  |
| `public string Source { get; init; }` |  |
| `public string? StopId { get; init; }` |  |
| `public string CapturedAtUtc { get; init; }` |  |

## `ComponentInfo`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ComponentInfo : IEquatable<ComponentInfo>` |  |
| `public ComponentInfo(string Id, string? ParentId, string TypeName, string? Key, bool IsFramework, string[] Children, string[] OutputElements)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(ComponentInfo? left, ComponentInfo? right)` |  |
| `public static bool operator ==(ComponentInfo? left, ComponentInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ComponentInfo? other)` |  |
| `public void Deconstruct(out string Id, out string? ParentId, out string TypeName, out string? Key, out bool IsFramework, out string[] Children, out string[] OutputElements)` |  |
| `public string Id { get; init; }` |  |
| `public string? ParentId { get; init; }` |  |
| `public string TypeName { get; init; }` |  |
| `public string? Key { get; init; }` |  |
| `public bool IsFramework { get; init; }` |  |
| `public string[] Children { get; init; }` |  |
| `public string[] OutputElements { get; init; }` |  |
| `public string? RootId { get; init; }` |  |
| `public string? DeclaringComponentId { get; init; }` |  |
| `public PropertyValue[]? Parameters { get; init; }` |  |

## `ContextRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ContextRequest : IEquatable<ContextRequest>` |  |
| `public ContextRequest(RequestContext Context)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(ContextRequest? left, ContextRequest? right)` |  |
| `public static bool operator ==(ContextRequest? left, ContextRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ContextRequest? other)` |  |
| `public void Deconstruct(out RequestContext Context)` |  |
| `public RequestContext Context { get; init; }` |  |

## `DebuggerField`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/DebuggerContracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DebuggerField : IEquatable<DebuggerField>` |  |
| `public DebuggerField(string Name, string Type, string Value, bool Available)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(DebuggerField? left, DebuggerField? right)` |  |
| `public static bool operator ==(DebuggerField? left, DebuggerField? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DebuggerField? other)` |  |
| `public void Deconstruct(out string Name, out string Type, out string Value, out bool Available)` |  |
| `public string Name { get; init; }` |  |
| `public string Type { get; init; }` |  |
| `public string Value { get; init; }` |  |
| `public bool Available { get; init; }` |  |

## `DebuggerFields`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/DebuggerContracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DebuggerFields : IEquatable<DebuggerFields>` |  |
| `public DebuggerFields(CaptureStamp Capture, string ComponentId, DebuggerField[] Fields, string? Message)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(DebuggerFields? left, DebuggerFields? right)` |  |
| `public static bool operator ==(DebuggerFields? left, DebuggerFields? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DebuggerFields? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out string ComponentId, out DebuggerField[] Fields, out string? Message)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public string ComponentId { get; init; }` |  |
| `public DebuggerField[] Fields { get; init; }` |  |
| `public string? Message { get; init; }` |  |

## `DebuggerReadRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/DebuggerContracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DebuggerReadRequest : IEquatable<DebuggerReadRequest>` |  |
| `public DebuggerReadRequest(int ProcessId, string SessionId, long Generation, string StopId, string ComponentId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(DebuggerReadRequest? left, DebuggerReadRequest? right)` |  |
| `public static bool operator ==(DebuggerReadRequest? left, DebuggerReadRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DebuggerReadRequest? other)` |  |
| `public void Deconstruct(out int ProcessId, out string SessionId, out long Generation, out string StopId, out string ComponentId)` |  |
| `public int ProcessId { get; init; }` |  |
| `public string SessionId { get; init; }` |  |
| `public long Generation { get; init; }` |  |
| `public string StopId { get; init; }` |  |
| `public string ComponentId { get; init; }` |  |

## `DebuggerStatus`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/DebuggerContracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DebuggerStatus : IEquatable<DebuggerStatus>` |  |
| `public DebuggerStatus(int ProcessId, string Adapter, string State, string? StopId, bool CanReadFields, string? Message)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(DebuggerStatus? left, DebuggerStatus? right)` |  |
| `public static bool operator ==(DebuggerStatus? left, DebuggerStatus? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DebuggerStatus? other)` |  |
| `public void Deconstruct(out int ProcessId, out string Adapter, out string State, out string? StopId, out bool CanReadFields, out string? Message)` |  |
| `public int ProcessId { get; init; }` |  |
| `public string Adapter { get; init; }` |  |
| `public string State { get; init; }` |  |
| `public string? StopId { get; init; }` |  |
| `public bool CanReadFields { get; init; }` |  |
| `public string? Message { get; init; }` |  |

## `DevToolsCapabilities`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DevToolsCapabilities` |  |
| `public const string Tree = "tree"` |  |
| `public const string Styles = "styles"` |  |
| `public const string Geometry = "geometry"` |  |
| `public const string Frames = "frames"` |  |
| `public const string Selection = "selection"` |  |
| `public const string Inspection = "inspection"` |  |
| `public const string DebuggerFields = "debugger.fields"` |  |

## `DevToolsErrors`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DevToolsErrors` |  |
| `public const int InvalidRequest = -32600` |  |
| `public const int MethodNotFound = -32601` |  |
| `public const int InvalidParams = -32602` |  |
| `public const int InternalError = -32603` |  |
| `public const int Unsupported = -32010` |  |
| `public const int Unavailable = -32011` |  |
| `public const int StaleSession = -32012` |  |
| `public const int TargetPaused = -32013` |  |
| `public const int VersionMismatch = -32014` |  |
| `public const int ResyncRequired = -32015` |  |
| `public const int Cancelled = -32016` |  |
| `public const int Busy = -32017` |  |
| `public const int ConnectionClosed = -32018` |  |
| `public const int NotInitialized = -32019` |  |

## `DevToolsException`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DevToolsException : Exception` |  |
| `public DevToolsException(int code, string message)` |  |
| `public int Code { get; }` |  |

## `DevToolsJson`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/DevToolsJson.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DevToolsJson` | **Modern transport assets (.NET 8 or later).** |
| `public static T Read<T>(JsonNode? node)` | **Modern transport assets (.NET 8 or later).** |
| `public static JsonNode Write<T>(T value)` | **Modern transport assets (.NET 8 or later).** |
| `public static JsonSerializerOptions Options { get; }` | **Modern transport assets (.NET 8 or later).** |

## `DevToolsPeer`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/DevToolsPeer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DevToolsPeer : IDisposable` | **Modern transport assets (.NET 8 or later).** |
| `public DevToolsPeer(Stream input, Stream output, string name, string[] capabilities, TargetState? target = null)` | **Modern transport assets (.NET 8 or later).** |
| `public bool Supports(string capability)` | **Modern transport assets (.NET 8 or later).** |
| `public void Start()` | **Modern transport assets (.NET 8 or later).** |
| `public Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))` | **Modern transport assets (.NET 8 or later).** |
| `public Task<T> RequestAsync<T>(string method, JsonNode parameters, CancellationToken cancellationToken = default(CancellationToken))` | **Modern transport assets (.NET 8 or later).** |
| `public Task<T> RequestAsync<T>(string method, JsonNode parameters, DateTimeOffset deadlineUtc, CancellationToken cancellationToken = default(CancellationToken))` | **Modern transport assets (.NET 8 or later).** |
| `public void SetTargetState(TargetState state)` | **Modern transport assets (.NET 8 or later).** |
| `public Task NotifyAsync(string method, JsonNode parameters, CancellationToken cancellationToken = default(CancellationToken))` | **Modern transport assets (.NET 8 or later).** |
| `public void Dispose()` | **Modern transport assets (.NET 8 or later).** |
| `public Func<DevToolsRequest, CancellationToken, Task<JsonNode?>>? RequestHandler { get; set; }` | **Modern transport assets (.NET 8 or later).** |
| `public Action<string, JsonNode>? NotificationHandler { get; set; }` | **Modern transport assets (.NET 8 or later).** |
| `public Task Closed { get; }` | **Modern transport assets (.NET 8 or later).** |
| `public bool IsInitialized { get; }` | **Modern transport assets (.NET 8 or later).** |
| `public TargetState? RemoteTarget { get; }` | **Modern transport assets (.NET 8 or later).** |

## `DevToolsProtocol`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public static class DevToolsProtocol` |  |
| `public const int Major = 1` |  |
| `public const int Minor = 3` |  |
| `public const int MaxMessageBytes = 1048576` |  |
| `public const int MaxHeaderBytes = 8192` |  |
| `public const int MaxDepth = 32` |  |
| `public const int MaxStringLength = 16384` |  |
| `public const int MaxNodes = 4096` |  |
| `public const int MaxFrames = 120` |  |
| `public const int MaxPendingRequests = 32` |  |
| `public const int MaxConcurrentRequests = 16` |  |
| `public const int RequestTimeoutSeconds = 10` |  |
| `public const long MaxSequence = 9007199254740991` |  |
| `public const string Initialize = "initialize"` |  |
| `public const string GetTree = "tree/get"` |  |
| `public const string TreeChanged = "tree/changed"` |  |
| `public const string GetStyles = "styles/get"` |  |
| `public const string GetGeometry = "geometry/get"` |  |
| `public const string GetFrames = "frames/get"` |  |
| `public const string FramesAdded = "frames/added"` |  |
| `public const string Select = "selection/set"` |  |
| `public const string GetSelection = "selection/get"` |  |
| `public const string Inspect = "inspection/set"` |  |
| `public const string SelectionChanged = "selection/changed"` |  |
| `public const string SessionChanged = "session/changed"` |  |
| `public const string CancelRequest = "$/cancelRequest"` |  |

## `DevToolsRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/DevToolsPeer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DevToolsRequest` | **Modern transport assets (.NET 8 or later).** |
| `public void ApplyMutation(Action action)` | **Modern transport assets (.NET 8 or later).** |
| `public string Method { get; }` | **Modern transport assets (.NET 8 or later).** |
| `public JsonNode Parameters { get; }` | **Modern transport assets (.NET 8 or later).** |
| `public RequestContext Context { get; }` | **Modern transport assets (.NET 8 or later).** |

## `ElementInfo`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ElementInfo : IEquatable<ElementInfo>` |  |
| `public ElementInfo(string Id, string? ParentId, string Kind, string Name, string? Text, string? ProducingComponentId, string? DeclaringComponentId, string[] Children)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(ElementInfo? left, ElementInfo? right)` |  |
| `public static bool operator ==(ElementInfo? left, ElementInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ElementInfo? other)` |  |
| `public void Deconstruct(out string Id, out string? ParentId, out string Kind, out string Name, out string? Text, out string? ProducingComponentId, out string? DeclaringComponentId, out string[] Children)` |  |
| `public string Id { get; init; }` |  |
| `public string? ParentId { get; init; }` |  |
| `public string Kind { get; init; }` |  |
| `public string Name { get; init; }` |  |
| `public string? Text { get; init; }` |  |
| `public string? ProducingComponentId { get; init; }` |  |
| `public string? DeclaringComponentId { get; init; }` |  |
| `public string[] Children { get; init; }` |  |
| `public string? RootId { get; init; }` |  |
| `public string? Key { get; init; }` |  |
| `public bool? IsRendered { get; init; }` |  |
| `public string? ElementId { get; init; }` |  |
| `public PropertyValue[]? Attributes { get; init; }` |  |
| `public string[]? Classes { get; init; }` |  |
| `public string[]? States { get; init; }` |  |
| `public string[]? Events { get; init; }` |  |

## `FrameBatch`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FrameBatch : IEquatable<FrameBatch>` |  |
| `public FrameBatch(CaptureStamp Capture, long BaseRevision, long Revision, FrameRecord[] Frames)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(FrameBatch? left, FrameBatch? right)` |  |
| `public static bool operator ==(FrameBatch? left, FrameBatch? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FrameBatch? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out long BaseRevision, out long Revision, out FrameRecord[] Frames)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public long BaseRevision { get; init; }` |  |
| `public long Revision { get; init; }` |  |
| `public FrameRecord[] Frames { get; init; }` |  |

## `FrameRecord`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FrameRecord : IEquatable<FrameRecord>` |  |
| `public FrameRecord(long Number, string[] Causes, FrameTimings Timings, int NodeCount, int LayoutCount, int PaintCount, WireRect[] Damage)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(FrameRecord? left, FrameRecord? right)` |  |
| `public static bool operator ==(FrameRecord? left, FrameRecord? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FrameRecord? other)` |  |
| `public void Deconstruct(out long Number, out string[] Causes, out FrameTimings Timings, out int NodeCount, out int LayoutCount, out int PaintCount, out WireRect[] Damage)` |  |
| `public long Number { get; init; }` |  |
| `public string[] Causes { get; init; }` |  |
| `public FrameTimings Timings { get; init; }` |  |
| `public int NodeCount { get; init; }` |  |
| `public int LayoutCount { get; init; }` |  |
| `public int PaintCount { get; init; }` |  |
| `public WireRect[] Damage { get; init; }` |  |
| `public string? Renderer { get; init; }` |  |
| `public string? RequestedRenderer { get; init; }` |  |
| `public string? GraphicsBackend { get; init; }` |  |
| `public string? GraphicsDevice { get; init; }` |  |
| `public string? FallbackReason { get; init; }` |  |
| `public long? RenderWorkerFrame { get; init; }` |  |
| `public double? RenderWorkerMs { get; init; }` |  |
| `public long? GpuFramesReplaced { get; init; }` |  |
| `public int? StylesRecomputed { get; init; }` |  |
| `public bool? HasNodeCount { get; init; }` |  |
| `public bool? HasPaintCount { get; init; }` |  |
| `public bool? HasDamage { get; init; }` |  |

## `FrameTimings`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FrameTimings : IEquatable<FrameTimings>` |  |
| `public FrameTimings(double StyleMs, double LayoutMs, double DisplayListMs, double PaintMs, double TotalMs)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(FrameTimings? left, FrameTimings? right)` |  |
| `public static bool operator ==(FrameTimings? left, FrameTimings? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FrameTimings? other)` |  |
| `public void Deconstruct(out double StyleMs, out double LayoutMs, out double DisplayListMs, out double PaintMs, out double TotalMs)` |  |
| `public double StyleMs { get; init; }` |  |
| `public double LayoutMs { get; init; }` |  |
| `public double DisplayListMs { get; init; }` |  |
| `public double PaintMs { get; init; }` |  |
| `public double TotalMs { get; init; }` |  |
| `public double? OverlayMs { get; init; }` |  |
| `public double? ElapsedMs { get; init; }` |  |

## `GeometryDetails`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record GeometryDetails : IEquatable<GeometryDetails>` |  |
| `public GeometryDetails(CaptureStamp Capture, string ElementId, WireRect BorderBox, WireEdges Margin, WireEdges Border, WireEdges Padding, double Scale)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(GeometryDetails? left, GeometryDetails? right)` |  |
| `public static bool operator ==(GeometryDetails? left, GeometryDetails? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(GeometryDetails? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out string ElementId, out WireRect BorderBox, out WireEdges Margin, out WireEdges Border, out WireEdges Padding, out double Scale)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public string ElementId { get; init; }` |  |
| `public WireRect BorderBox { get; init; }` |  |
| `public WireEdges Margin { get; init; }` |  |
| `public WireEdges Border { get; init; }` |  |
| `public WireEdges Padding { get; init; }` |  |
| `public double Scale { get; init; }` |  |
| `public WireRect? MarginBox { get; init; }` |  |
| `public WireRect? PaddingBox { get; init; }` |  |
| `public WireRect? ContentBox { get; init; }` |  |

## `InitializeRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record InitializeRequest : IEquatable<InitializeRequest>` |  |
| `public InitializeRequest(ProtocolVersion Version, string ClientName, string[] Capabilities)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(InitializeRequest? left, InitializeRequest? right)` |  |
| `public static bool operator ==(InitializeRequest? left, InitializeRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(InitializeRequest? other)` |  |
| `public void Deconstruct(out ProtocolVersion Version, out string ClientName, out string[] Capabilities)` |  |
| `public ProtocolVersion Version { get; init; }` |  |
| `public string ClientName { get; init; }` |  |
| `public string[] Capabilities { get; init; }` |  |

## `InitializeResult`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record InitializeResult : IEquatable<InitializeResult>` |  |
| `public InitializeResult(ProtocolVersion Version, string PeerName, string[] Capabilities, TargetState? Target)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(InitializeResult? left, InitializeResult? right)` |  |
| `public static bool operator ==(InitializeResult? left, InitializeResult? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(InitializeResult? other)` |  |
| `public void Deconstruct(out ProtocolVersion Version, out string PeerName, out string[] Capabilities, out TargetState? Target)` |  |
| `public ProtocolVersion Version { get; init; }` |  |
| `public string PeerName { get; init; }` |  |
| `public string[] Capabilities { get; init; }` |  |
| `public TargetState? Target { get; init; }` |  |

## `InspectionRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record InspectionRequest : IEquatable<InspectionRequest>` |  |
| `public InspectionRequest(RequestContext Context, bool Enabled)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(InspectionRequest? left, InspectionRequest? right)` |  |
| `public static bool operator ==(InspectionRequest? left, InspectionRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(InspectionRequest? other)` |  |
| `public void Deconstruct(out RequestContext Context, out bool Enabled)` |  |
| `public RequestContext Context { get; init; }` |  |
| `public bool Enabled { get; init; }` |  |

## `InspectorLauncher`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/InspectorLauncher.cs)

| Declaration | Contract |
| --- | --- |
| `public static class InspectorLauncher` | **Modern transport assets (.NET 8 or later).** |
| `public static void Validate(string inspector, string? expectedVersion = null)` | **Modern transport assets (.NET 8 or later).** |
| `public static Task OpenAsync(LocalDevToolsDescriptor descriptor, CancellationToken cancellationToken = default(CancellationToken), string? discoveryDirectory = null, bool closeWithOwner = false, string? ownerHandle = null)` | **Modern transport assets (.NET 8 or later).** |
| `public static string RuntimeIdentifier { get; }` | **Modern transport assets (.NET 8 or later).** |
| `public static string Version { get; }` | **Modern transport assets (.NET 8 or later).** |

## `InspectorLocator`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/InspectorLocator.cs)

| Declaration | Contract |
| --- | --- |
| `public static class InspectorLocator` | **Modern transport assets (.NET 8 or later).** |
| `public static string? Find(string applicationDirectory)` | **Modern transport assets (.NET 8 or later).** |

## `LocalDevTools`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/LocalDevTools.cs)

| Declaration | Contract |
| --- | --- |
| `public static class LocalDevTools` | **Modern transport assets (.NET 8 or later).** |
| `public static void PrepareDirectory(string directory)` | **Modern transport assets (.NET 8 or later).** |
| `public static string PipeName(string directory, string sessionId)` | **Modern transport assets (.NET 8 or later).** |
| `public static string Save(string directory, LocalDevToolsDescriptor descriptor)` | **Modern transport assets (.NET 8 or later).** |
| `public static IReadOnlyList<LocalDevToolsDescriptor> Discover(string? directory = null)` | **Modern transport assets (.NET 8 or later).** |
| `public static bool IsAlive(LocalDevToolsDescriptor descriptor)` | **Modern transport assets (.NET 8 or later).** |
| `public static string ProcessStartToken(int processId)` | **Modern transport assets (.NET 8 or later).** |
| `public static Task<DevToolsPeer> ConnectAsync(LocalDevToolsDescriptor descriptor, string clientName, string[] capabilities, CancellationToken cancellationToken = default(CancellationToken))` | **Modern transport assets (.NET 8 or later).** |
| `public static string DiscoveryDirectory { get; }` | **Modern transport assets (.NET 8 or later).** |

## `LocalDevToolsDescriptor`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/LocalDevTools.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LocalDevToolsDescriptor : IEquatable<LocalDevToolsDescriptor>` | **Modern transport assets (.NET 8 or later).** |
| `public LocalDevToolsDescriptor(int Version, TargetIdentity Identity, string ProcessStartUtc, string PipeName, string ProcessStartToken)` | **Modern transport assets (.NET 8 or later).** |
| `public override string ToString()` | **Modern transport assets (.NET 8 or later).** |
| `public static bool operator !=(LocalDevToolsDescriptor? left, LocalDevToolsDescriptor? right)` | **Modern transport assets (.NET 8 or later).** |
| `public static bool operator ==(LocalDevToolsDescriptor? left, LocalDevToolsDescriptor? right)` | **Modern transport assets (.NET 8 or later).** |
| `public override int GetHashCode()` | **Modern transport assets (.NET 8 or later).** |
| `public override bool Equals(object? obj)` | **Modern transport assets (.NET 8 or later).** |
| `public bool Equals(LocalDevToolsDescriptor? other)` | **Modern transport assets (.NET 8 or later).** |
| `public void Deconstruct(out int Version, out TargetIdentity Identity, out string ProcessStartUtc, out string PipeName, out string ProcessStartToken)` | **Modern transport assets (.NET 8 or later).** |
| `public int Version { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public TargetIdentity Identity { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string ProcessStartUtc { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string PipeName { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string ProcessStartToken { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string? ApplicationName { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string? InspectorPath { get; init; }` | **Modern transport assets (.NET 8 or later).** |
| `public string? InspectorVersion { get; init; }` | **Modern transport assets (.NET 8 or later).** |

## `NodeRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record NodeRequest : IEquatable<NodeRequest>` |  |
| `public NodeRequest(RequestContext Context, string ElementId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(NodeRequest? left, NodeRequest? right)` |  |
| `public static bool operator ==(NodeRequest? left, NodeRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(NodeRequest? other)` |  |
| `public void Deconstruct(out RequestContext Context, out string ElementId)` |  |
| `public RequestContext Context { get; init; }` |  |
| `public string ElementId { get; init; }` |  |

## `OptionalFieldAttribute`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class OptionalFieldAttribute : Attribute` |  |
| `public OptionalFieldAttribute()` |  |

## `PropertyValue`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PropertyValue : IEquatable<PropertyValue>` |  |
| `public PropertyValue(string Name, string Value, string Origin)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(PropertyValue? left, PropertyValue? right)` |  |
| `public static bool operator ==(PropertyValue? left, PropertyValue? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(PropertyValue? other)` |  |
| `public void Deconstruct(out string Name, out string Value, out string Origin)` |  |
| `public string Name { get; init; }` |  |
| `public string Value { get; init; }` |  |
| `public string Origin { get; init; }` |  |
| `public string[]? Longhands { get; init; }` |  |
| `public string[]? Winning { get; init; }` |  |
| `public int? Rule { get; init; }` |  |
| `public int? Declaration { get; init; }` |  |

## `ProtocolVersion`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ProtocolVersion : IEquatable<ProtocolVersion>` |  |
| `public ProtocolVersion(int Major, int Minor)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(ProtocolVersion? left, ProtocolVersion? right)` |  |
| `public static bool operator ==(ProtocolVersion? left, ProtocolVersion? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ProtocolVersion? other)` |  |
| `public void Deconstruct(out int Major, out int Minor)` |  |
| `public int Major { get; init; }` |  |
| `public int Minor { get; init; }` |  |

## `RequestContext`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RequestContext : IEquatable<RequestContext>` |  |
| `public RequestContext(string SessionId, long Generation, string? StopId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(RequestContext? left, RequestContext? right)` |  |
| `public static bool operator ==(RequestContext? left, RequestContext? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(RequestContext? other)` |  |
| `public void Deconstruct(out string SessionId, out long Generation, out string? StopId)` |  |
| `public string SessionId { get; init; }` |  |
| `public long Generation { get; init; }` |  |
| `public string? StopId { get; init; }` |  |

## `RevisionCursor`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/RevisionCursor.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RevisionCursor` | **Modern transport assets (.NET 8 or later).** |
| `public RevisionCursor(RequestContext context)` | **Modern transport assets (.NET 8 or later).** |
| `public RevisionOutcome Accept(TreeSnapshot snapshot)` | **Modern transport assets (.NET 8 or later).** |
| `public RevisionOutcome Accept(TreeDelta delta)` | **Modern transport assets (.NET 8 or later).** |
| `public RevisionOutcome Accept(FrameBatch batch, bool resynchronize = false)` | **Modern transport assets (.NET 8 or later).** |

## `RevisionOutcome`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Transport/RevisionCursor.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RevisionOutcome` | **Modern transport assets (.NET 8 or later).** |
| `Accepted = 0` | **Modern transport assets (.NET 8 or later).** |
| `Stale = 1` | **Modern transport assets (.NET 8 or later).** |
| `ResyncRequired = 2` | **Modern transport assets (.NET 8 or later).** |

## `SelectionRequest`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SelectionRequest : IEquatable<SelectionRequest>` |  |
| `public SelectionRequest(RequestContext Context, string Kind, string? NodeId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(SelectionRequest? left, SelectionRequest? right)` |  |
| `public static bool operator ==(SelectionRequest? left, SelectionRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SelectionRequest? other)` |  |
| `public void Deconstruct(out RequestContext Context, out string Kind, out string? NodeId)` |  |
| `public RequestContext Context { get; init; }` |  |
| `public string Kind { get; init; }` |  |
| `public string? NodeId { get; init; }` |  |

## `SelectionResult`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SelectionResult : IEquatable<SelectionResult>` |  |
| `public SelectionResult(CaptureStamp Capture, string Kind, string? NodeId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(SelectionResult? left, SelectionResult? right)` |  |
| `public static bool operator ==(SelectionResult? left, SelectionResult? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SelectionResult? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out string Kind, out string? NodeId)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public string Kind { get; init; }` |  |
| `public string? NodeId { get; init; }` |  |
| `public bool? IsInspecting { get; init; }` |  |
| `public long? Revision { get; init; }` |  |

## `StyleDetails`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record StyleDetails : IEquatable<StyleDetails>` |  |
| `public StyleDetails(CaptureStamp Capture, string ElementId, PropertyValue[] Computed, StyleRule[] Rules)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(StyleDetails? left, StyleDetails? right)` |  |
| `public static bool operator ==(StyleDetails? left, StyleDetails? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(StyleDetails? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out string ElementId, out PropertyValue[] Computed, out StyleRule[] Rules)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public string ElementId { get; init; }` |  |
| `public PropertyValue[] Computed { get; init; }` |  |
| `public StyleRule[] Rules { get; init; }` |  |
| `public PropertyValue[]? CustomProperties { get; init; }` |  |

## `StyleRule`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record StyleRule : IEquatable<StyleRule>` |  |
| `public StyleRule(string Selector, string? Source, int? Line, PropertyValue[] Declarations)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(StyleRule? left, StyleRule? right)` |  |
| `public static bool operator ==(StyleRule? left, StyleRule? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(StyleRule? other)` |  |
| `public void Deconstruct(out string Selector, out string? Source, out int? Line, out PropertyValue[] Declarations)` |  |
| `public string Selector { get; init; }` |  |
| `public string? Source { get; init; }` |  |
| `public int? Line { get; init; }` |  |
| `public PropertyValue[] Declarations { get; init; }` |  |
| `public string? Level { get; init; }` |  |
| `public int? Order { get; init; }` |  |
| `public string? Specificity { get; init; }` |  |
| `public string? Media { get; init; }` |  |
| `public bool? MediaMatches { get; init; }` |  |

## `TargetIdentity`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TargetIdentity : IEquatable<TargetIdentity>` |  |
| `public TargetIdentity(int ProcessId, string ProcessInstanceId, string RuntimeInstanceId, string ApplicationId, string WindowId, string SessionId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(TargetIdentity? left, TargetIdentity? right)` |  |
| `public static bool operator ==(TargetIdentity? left, TargetIdentity? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TargetIdentity? other)` |  |
| `public void Deconstruct(out int ProcessId, out string ProcessInstanceId, out string RuntimeInstanceId, out string ApplicationId, out string WindowId, out string SessionId)` |  |
| `public int ProcessId { get; init; }` |  |
| `public string ProcessInstanceId { get; init; }` |  |
| `public string RuntimeInstanceId { get; init; }` |  |
| `public string ApplicationId { get; init; }` |  |
| `public string WindowId { get; init; }` |  |
| `public string SessionId { get; init; }` |  |

## `TargetState`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TargetState : IEquatable<TargetState>` |  |
| `public TargetState(TargetIdentity Identity, long Generation, string State, string? StopId)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(TargetState? left, TargetState? right)` |  |
| `public static bool operator ==(TargetState? left, TargetState? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TargetState? other)` |  |
| `public void Deconstruct(out TargetIdentity Identity, out long Generation, out string State, out string? StopId)` |  |
| `public TargetIdentity Identity { get; init; }` |  |
| `public long Generation { get; init; }` |  |
| `public string State { get; init; }` |  |
| `public string? StopId { get; init; }` |  |

## `TreeDelta`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TreeDelta : IEquatable<TreeDelta>` |  |
| `public TreeDelta(CaptureStamp Capture, long BaseRevision, long Revision, string[] ComponentRoots, string[] ElementRoots, ComponentInfo[] Components, ElementInfo[] Elements, string[] RemovedComponents, string[] RemovedElements)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(TreeDelta? left, TreeDelta? right)` |  |
| `public static bool operator ==(TreeDelta? left, TreeDelta? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TreeDelta? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out long BaseRevision, out long Revision, out string[] ComponentRoots, out string[] ElementRoots, out ComponentInfo[] Components, out ElementInfo[] Elements, out string[] RemovedComponents, out string[] RemovedElements)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public long BaseRevision { get; init; }` |  |
| `public long Revision { get; init; }` |  |
| `public string[] ComponentRoots { get; init; }` |  |
| `public string[] ElementRoots { get; init; }` |  |
| `public ComponentInfo[] Components { get; init; }` |  |
| `public ElementInfo[] Elements { get; init; }` |  |
| `public string[] RemovedComponents { get; init; }` |  |
| `public string[] RemovedElements { get; init; }` |  |

## `TreeSnapshot`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TreeSnapshot : IEquatable<TreeSnapshot>` |  |
| `public TreeSnapshot(CaptureStamp Capture, long Revision, string[] ComponentRoots, string[] ElementRoots, ComponentInfo[] Components, ElementInfo[] Elements)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(TreeSnapshot? left, TreeSnapshot? right)` |  |
| `public static bool operator ==(TreeSnapshot? left, TreeSnapshot? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TreeSnapshot? other)` |  |
| `public void Deconstruct(out CaptureStamp Capture, out long Revision, out string[] ComponentRoots, out string[] ElementRoots, out ComponentInfo[] Components, out ElementInfo[] Elements)` |  |
| `public CaptureStamp Capture { get; init; }` |  |
| `public long Revision { get; init; }` |  |
| `public string[] ComponentRoots { get; init; }` |  |
| `public string[] ElementRoots { get; init; }` |  |
| `public ComponentInfo[] Components { get; init; }` |  |
| `public ElementInfo[] Elements { get; init; }` |  |

## `WireEdges`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WireEdges : IEquatable<WireEdges>` |  |
| `public WireEdges(double Top, double Right, double Bottom, double Left)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(WireEdges? left, WireEdges? right)` |  |
| `public static bool operator ==(WireEdges? left, WireEdges? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WireEdges? other)` |  |
| `public void Deconstruct(out double Top, out double Right, out double Bottom, out double Left)` |  |
| `public double Top { get; init; }` |  |
| `public double Right { get; init; }` |  |
| `public double Bottom { get; init; }` |  |
| `public double Left { get; init; }` |  |

## `WireRect`

Namespace `Nazor.DevTools.Protocol`. [Source](../../Sources/Tools/Nazor.DevTools.Protocol/Contracts.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WireRect : IEquatable<WireRect>` |  |
| `public WireRect(double X, double Y, double Width, double Height)` |  |
| `public override string ToString()` |  |
| `public static bool operator !=(WireRect? left, WireRect? right)` |  |
| `public static bool operator ==(WireRect? left, WireRect? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WireRect? other)` |  |
| `public void Deconstruct(out double X, out double Y, out double Width, out double Height)` |  |
| `public double X { get; init; }` |  |
| `public double Y { get; init; }` |  |
| `public double Width { get; init; }` |  |
| `public double Height { get; init; }` |  |

`InspectorLauncher.OpenAsync` uses `closeWithOwner` to bind the tool lifetime to the descriptor session. `ownerHandle` carries an opaque exported desktop owner token; do not pass process-local native pointers. Existing per-session inspectors inherit ownership on activation. Defaults preserve manual/editor-only capture retention.
