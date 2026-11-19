namespace Nazor.DevTools.Protocol;

public sealed record DebuggerStatus(int ProcessId, string Adapter, string State, string? StopId, bool CanReadFields, string? Message);
public sealed record DebuggerField(string Name, string Type, string Value, bool Available);
public sealed record DebuggerFields(CaptureStamp Capture, string ComponentId, DebuggerField[] Fields, string? Message);
public sealed record DebuggerReadRequest(int ProcessId, string SessionId, long Generation, string StopId, string ComponentId);
