namespace Nazor.DevTools.Protocol;

public static class DevToolsProtocol
{
    public const int Major = 1;
    public const int Minor = 3;
    public const int MaxMessageBytes = 1024 * 1024;
    public const int MaxHeaderBytes = 8192;
    public const int MaxDepth = 32;
    public const int MaxStringLength = 16384;
    public const int MaxNodes = 4096;
    public const int MaxFrames = 120;
    public const int MaxPendingRequests = 32;
    public const int MaxConcurrentRequests = 16;
    public const int RequestTimeoutSeconds = 10;
    public const long MaxSequence = 9007199254740991;

    public const string Initialize = "initialize";
    public const string GetTree = "tree/get";
    public const string TreeChanged = "tree/changed";
    public const string GetStyles = "styles/get";
    public const string GetGeometry = "geometry/get";
    public const string GetFrames = "frames/get";
    public const string FramesAdded = "frames/added";
    public const string Select = "selection/set";
    public const string GetSelection = "selection/get";
    public const string Inspect = "inspection/set";
    public const string SelectionChanged = "selection/changed";
    public const string SessionChanged = "session/changed";
    public const string CancelRequest = "$/cancelRequest";
}

public static class DevToolsCapabilities
{
    public const string Tree = "tree";
    public const string Styles = "styles";
    public const string Geometry = "geometry";
    public const string Frames = "frames";
    public const string Selection = "selection";
    public const string Inspection = "inspection";
    public const string DebuggerFields = "debugger.fields";
}

public static class DevToolsErrors
{
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
    public const int Unsupported = -32010;
    public const int Unavailable = -32011;
    public const int StaleSession = -32012;
    public const int TargetPaused = -32013;
    public const int VersionMismatch = -32014;
    public const int ResyncRequired = -32015;
    public const int Cancelled = -32016;
    public const int Busy = -32017;
    public const int ConnectionClosed = -32018;
    public const int NotInitialized = -32019;
}

public sealed class DevToolsException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

public sealed record ProtocolVersion(int Major, int Minor);
public sealed record InitializeRequest(ProtocolVersion Version, string ClientName, string[] Capabilities);
public sealed record InitializeResult(ProtocolVersion Version, string PeerName, string[] Capabilities, TargetState? Target);
public sealed record TargetIdentity(int ProcessId, string ProcessInstanceId, string RuntimeInstanceId,
    string ApplicationId, string WindowId, string SessionId);
public sealed record TargetState(TargetIdentity Identity, long Generation, string State, string? StopId);
public sealed record RequestContext(string SessionId, long Generation, string? StopId);
public sealed record CaptureStamp(string SessionId, long Generation, string Source, string? StopId, string CapturedAtUtc);
public sealed record ContextRequest(RequestContext Context);
public sealed record NodeRequest(RequestContext Context, string ElementId);
public sealed record SelectionRequest(RequestContext Context, string Kind, string? NodeId);
public sealed record InspectionRequest(RequestContext Context, bool Enabled);
public sealed record SelectionResult(CaptureStamp Capture, string Kind, string? NodeId)
{
    [OptionalField] public bool? IsInspecting { get; init; }
    [OptionalField] public long? Revision { get; init; }
}
public sealed record ComponentInfo(string Id, string? ParentId, string TypeName, string? Key,
    bool IsFramework, string[] Children, string[] OutputElements)
{
    [OptionalField] public string? RootId { get; init; }
    [OptionalField] public string? DeclaringComponentId { get; init; }
    [OptionalField] public PropertyValue[]? Parameters { get; init; }
}
public sealed record ElementInfo(string Id, string? ParentId, string Kind, string Name, string? Text,
    string? ProducingComponentId, string? DeclaringComponentId, string[] Children)
{
    [OptionalField] public string? RootId { get; init; }
    [OptionalField] public string? Key { get; init; }
    [OptionalField] public bool? IsRendered { get; init; }
    [OptionalField] public string? ElementId { get; init; }
    [OptionalField] public PropertyValue[]? Attributes { get; init; }
    [OptionalField] public string[]? Classes { get; init; }
    [OptionalField] public string[]? States { get; init; }
    [OptionalField] public string[]? Events { get; init; }
}
public sealed record TreeSnapshot(CaptureStamp Capture, long Revision, string[] ComponentRoots,
    string[] ElementRoots, ComponentInfo[] Components, ElementInfo[] Elements);
public sealed record TreeDelta(CaptureStamp Capture, long BaseRevision, long Revision, string[] ComponentRoots,
    string[] ElementRoots, ComponentInfo[] Components, ElementInfo[] Elements,
    string[] RemovedComponents, string[] RemovedElements);
public sealed record PropertyValue(string Name, string Value, string Origin)
{
    [OptionalField] public string[]? Longhands { get; init; }
    [OptionalField] public string[]? Winning { get; init; }
    [OptionalField] public int? Rule { get; init; }
    [OptionalField] public int? Declaration { get; init; }
}
public sealed record StyleRule(string Selector, string? Source, int? Line, PropertyValue[] Declarations)
{
    [OptionalField] public string? Level { get; init; }
    [OptionalField] public int? Order { get; init; }
    [OptionalField] public string? Specificity { get; init; }
    [OptionalField] public string? Media { get; init; }
    [OptionalField] public bool? MediaMatches { get; init; }
}
public sealed record StyleDetails(CaptureStamp Capture, string ElementId, PropertyValue[] Computed, StyleRule[] Rules)
{
    [OptionalField] public PropertyValue[]? CustomProperties { get; init; }
}
public sealed record WireRect(double X, double Y, double Width, double Height);
public sealed record WireEdges(double Top, double Right, double Bottom, double Left);
public sealed record GeometryDetails(CaptureStamp Capture, string ElementId, WireRect BorderBox,
    WireEdges Margin, WireEdges Border, WireEdges Padding, double Scale)
{
    [OptionalField] public WireRect? MarginBox { get; init; }
    [OptionalField] public WireRect? PaddingBox { get; init; }
    [OptionalField] public WireRect? ContentBox { get; init; }
}
public sealed record FrameTimings(double StyleMs, double LayoutMs, double DisplayListMs, double PaintMs, double TotalMs)
{
    [OptionalField] public double? OverlayMs { get; init; }
    [OptionalField] public double? ElapsedMs { get; init; }
}
public sealed record FrameRecord(long Number, string[] Causes, FrameTimings Timings,
    int NodeCount, int LayoutCount, int PaintCount, WireRect[] Damage)
{
    [OptionalField] public string? Renderer { get; init; }
    [OptionalField] public string? RequestedRenderer { get; init; }
    [OptionalField] public string? GraphicsBackend { get; init; }
    [OptionalField] public string? GraphicsDevice { get; init; }
    [OptionalField] public string? FallbackReason { get; init; }
    [OptionalField] public long? RenderWorkerFrame { get; init; }
    [OptionalField] public double? RenderWorkerMs { get; init; }
    [OptionalField] public long? GpuFramesReplaced { get; init; }
    [OptionalField] public int? StylesRecomputed { get; init; }
    [OptionalField] public bool? HasNodeCount { get; init; }
    [OptionalField] public bool? HasPaintCount { get; init; }
    [OptionalField] public bool? HasDamage { get; init; }
}
public sealed record FrameBatch(CaptureStamp Capture, long BaseRevision, long Revision, FrameRecord[] Frames);

[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionalFieldAttribute : Attribute;
