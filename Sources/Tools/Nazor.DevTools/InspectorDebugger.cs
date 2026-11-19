using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public interface IInspectorDebugger
{
    Task<DebuggerStatus> StateAsync(int processId, CancellationToken token);
    Task<TreeSnapshot> TreeAsync(int processId, string sessionId, string stopId, CancellationToken token);
    Task<DebuggerFields> FieldsAsync(DebuggerReadRequest request, CancellationToken token);
}

public sealed class InspectorDebugger(InspectorClient client, LocalDevToolsDescriptor descriptor, IInspectorDebugger adapter) : IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Task? running;
    public Task Completion => running ?? Task.CompletedTask;
    public void Start() => running ??= Task.Run(RunAsync);
    private async Task RunAsync()
    {
        var store = client.Store;
        if (store.DebuggerContext().Target is null)
        {
            store.Connecting(descriptor.Identity, descriptor.ApplicationName);
            store.Bind(new(descriptor.Identity, 1, "running", null));
        }
        DebuggerStatus? previous = null;
        string? readKey = null;
        string? treeAttempt = null;
        while (!lifetime.IsCancellationRequested)
        {
            try
            {
                var data = store.DebuggerContext();
                if (data.Target?.Identity.ProcessInstanceId != descriptor.Identity.ProcessInstanceId) { await Task.Delay(250, lifetime.Token); continue; }
                var state = await adapter.StateAsync(descriptor.Identity.ProcessId, lifetime.Token).ConfigureAwait(false);
                client.SetDebuggerState(state);
                if (state.State == "paused")
                {
                    data = store.DebuggerContext();
                    if (!data.HasTree && treeAttempt != state.StopId)
                    {
                        treeAttempt = state.StopId;
                        var tree = await adapter.TreeAsync(state.ProcessId, data.Target!.Identity.SessionId, state.StopId!, lifetime.Token).ConfigureAwait(false);
                        store.Apply(tree with { Capture = tree.Capture with { Generation = data.Target.Generation, Source = "retained", StopId = null } });
                    }
                    data = store.DebuggerContext();
                    if (data.Selection is { Kind: "component" } selected)
                    {
                        var key = state.StopId + ":" + data.Target!.Identity.SessionId + ":" + selected.NodeId;
                        if (key != readKey && state.CanReadFields)
                        {
                            readKey = key;
                            var fields = await adapter.FieldsAsync(new(state.ProcessId, data.Target.Identity.SessionId, data.Target.Generation, state.StopId!, selected.NodeId), lifetime.Token).ConfigureAwait(false);
                            var latest = await adapter.StateAsync(state.ProcessId, lifetime.Token).ConfigureAwait(false);
                            client.SetDebuggerState(latest);
                            if (latest.State == "paused" && latest.StopId == state.StopId) store.Apply(fields);
                        }
                    }
                }
                else if (state.State == "running" && (previous?.State != "running" || !store.DebuggerContext().HasTree))
                {
                    readKey = null;
                    if (client.Store.DebuggerContext().Connection == InspectorConnectionStatus.Disconnected || !client.Supports(DevToolsCapabilities.Tree)) {
                        var currentWindow = (await client.DiscoverAsync(lifetime.Token).ConfigureAwait(false)).FirstOrDefault(d => d.Identity.SessionId == data.Target!.Identity.SessionId);
                        if (currentWindow is not null) await client.ConnectAsync(currentWindow, lifetime.Token).ConfigureAwait(false);
                    }
                    else await client.RefreshAsync(lifetime.Token).ConfigureAwait(false);
                }
                if (state.State == "exited" && previous?.State != "exited")
                {
                    if (!LocalDevTools.IsAlive(descriptor)) client.Disconnect();
                    else if (client.Supports(DevToolsCapabilities.Tree)) await client.RefreshAsync(lifetime.Token).ConfigureAwait(false);
                }
                previous = state;
                await Task.Delay(250, lifetime.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { break; }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                readKey = null;
                treeAttempt = null;
                if (lifetime.IsCancellationRequested) break;
                client.SetDebuggerState((previous ?? new DebuggerStatus(descriptor.Identity.ProcessId, "Debugger bridge", "running", null, false, null))
                    with { CanReadFields = false, Message = "Debugger data unavailable: " + error.Message });
                try { await Task.Delay(500, lifetime.Token).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }
    }
    public void Dispose() { lifetime.Cancel(); }
}
