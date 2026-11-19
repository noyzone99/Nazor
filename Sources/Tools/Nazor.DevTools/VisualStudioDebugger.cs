using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

[SupportedOSPlatform("windows")]
public sealed class VisualStudioDebugger(int ideProcessId) : IInspectorDebugger
{
    private readonly object gate = new();
    private Task<string>? pending;
    private dynamic? reader;
    internal static async Task<IInspectorDebugger?> FindAttachedAsync(int processId, CancellationToken token)
    {
        var candidates = await Task.Run(() =>
        {
            var result = new List<int>();
            Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out var table));
            Marshal.ThrowExceptionForHR(CreateBindCtx(0, out var context));
            table.EnumRunning(out var names);
            var items = new IMoniker[1];
            while (result.Count < 16 && names.Next(1, items, IntPtr.Zero) == 0)
            {
                items[0].GetDisplayName(context, null, out var name);
                if (name.StartsWith("!VisualStudio.DTE.", StringComparison.Ordinal) && int.TryParse(name[(name.LastIndexOf(':') + 1)..], out var id)) result.Add(id);
            }
            return result;
        }, token).WaitAsync(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
        foreach (var id in candidates)
        {
            try
            {
                var adapter = new VisualStudioDebugger(id);
                var state = await adapter.StateAsync(processId, token).ConfigureAwait(false);
                if (state.State is "running" or "paused") return adapter;
            }
            catch (Exception error) when (error is COMException or IOException or TimeoutException) { }
        }
        return null;
    }
    public async Task<DebuggerStatus> StateAsync(int processId, CancellationToken token) => DevToolsJson.Read<DebuggerStatus>(JsonNode.Parse(await CallAsync(r => r.GetState(processId), token))!);
    public async Task<TreeSnapshot> TreeAsync(int processId, string sessionId, string stopId, CancellationToken token) =>
        DevToolsJson.Read<TreeSnapshot>(JsonNode.Parse(await CallAsync(r => r.ReadTree(processId, sessionId, long.Parse(stopId)), token))!);
    public async Task<DebuggerFields> FieldsAsync(DebuggerReadRequest request, CancellationToken token) =>
        DevToolsJson.Read<DebuggerFields>(JsonNode.Parse(await CallAsync(r => r.ReadComponent(request.ProcessId, request.SessionId, request.Generation, request.ComponentId, long.Parse(request.StopId)), token))!);

    private async Task<string> CallAsync(Func<dynamic, string> call, CancellationToken token)
    {
        Task<string> work;
        lock (gate)
        {
            if (pending is { IsCompleted: false }) throw new IOException("The IDE is still processing the preceding read.");
            work = pending = Task.Run(() =>
            {
                var timeout = System.Diagnostics.Stopwatch.StartNew();
                while (true)
                {
                    try { reader ??= FindReader(); return (string)call(reader); }
                    catch (COMException error) when (error.HResult is unchecked((int)0x80010001) or unchecked((int)0x8001010A) && timeout.ElapsedMilliseconds < 1000) { Thread.Sleep(50); }
                }
            }, token);
        }
        return await work.WaitAsync(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
    }
    private object FindReader()
    {
        Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out var table));
        Marshal.ThrowExceptionForHR(CreateBindCtx(0, out var context));
        table.EnumRunning(out var names);
        var items = new IMoniker[1];
        while (names.Next(1, items, IntPtr.Zero) == 0)
        {
            items[0].GetDisplayName(context, null, out var name);
            if (!name.StartsWith("!VisualStudio.DTE.", StringComparison.Ordinal) || !name.EndsWith(":" + ideProcessId, StringComparison.Ordinal)) continue;
            table.GetObject(items[0], out var automation);
            return ((dynamic)automation).GetObject("Nazor.DevTools");
        }
        throw new IOException("The specified Visual Studio instance is unavailable.");
    }
    [DllImport("ole32.dll")] private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);
    [DllImport("ole32.dll")] private static extern int CreateBindCtx(int reserved, out IBindCtx context);
}
