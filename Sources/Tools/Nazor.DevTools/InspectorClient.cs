using System.Text.Json.Nodes;
using System.Threading.Channels;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed partial class InspectorClient(InspectionStore store, string? discoveryDirectory = null) : IDisposable
{
    private readonly object gate = new();
    private Connection? current;
    private long epoch;
    private bool disposed;
    private bool debuggerPaused;
    internal bool UsePagedTree { get; init; }
    public InspectionStore Store { get; } = store;

    public Task<IReadOnlyList<LocalDevToolsDescriptor>> DiscoverAsync(CancellationToken token = default) => Task.Run<IReadOnlyList<LocalDevToolsDescriptor>>(
        () => LocalDevTools.Discover(discoveryDirectory).Where(d => d.Identity.ProcessId != Environment.ProcessId).ToArray(), token);

    public async Task ConnectAsync(LocalDevToolsDescriptor descriptor, CancellationToken token = default)
    {
        Connection attempt;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var oldTarget = Store.DebuggerContext().Target;
            if (oldTarget?.Identity.ProcessInstanceId != descriptor.Identity.ProcessInstanceId) debuggerPaused = false;
            StopCurrent();
            attempt = new(++epoch, CancellationTokenSource.CreateLinkedTokenSource(token));
            current = attempt;
            Store.Connecting(descriptor.Identity, descriptor.ApplicationName);
        }
        try
        {
            string[] capabilities = [DevToolsCapabilities.Tree, DevToolsCapabilities.Selection, DevToolsCapabilities.Inspection, DevToolsCapabilities.Styles, DevToolsCapabilities.Geometry, DevToolsCapabilities.Frames];
            if (UsePagedTree) capabilities = [.. capabilities, DevToolsCapabilities.PagedTree];
            var peer = await LocalDevTools.ConnectAsync(descriptor, "Nazor DevTools", capabilities, attempt.Token).ConfigureAwait(false);
            lock (gate)
            {
                if (!Active(attempt) || attempt.Lifetime.IsCancellationRequested) { peer.Dispose(); attempt.Token.ThrowIfCancellationRequested(); return; }
                attempt.Peer = peer;
                peer.NotificationHandler = (method, data) =>
                {
                    lock (gate)
                    {
                        if (!Active(attempt)) return;
                        if (method == DevToolsProtocol.FramesAdded && !attempt.WatchFrames) { attempt.FrameGap = true; return; }
                        if (!attempt.Messages.Writer.TryWrite((method, data.DeepClone())))
                        { Interlocked.Exchange(ref attempt.Gap, 1); if (attempt.WatchFrames) attempt.FrameGap = true; }
                    }
                };
                if (!Store.Bind(peer.RemoteTarget!)) throw new DevToolsException(DevToolsErrors.StaleSession, "The target returned an older capture generation.");
            }
            _ = ConsumeAsync(attempt);
            _ = ObserveCloseAsync(attempt);
            await RefreshAsync(attempt, attempt.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException or DevToolsException)
        {
            lock (gate)
            {
                if (!Active(attempt)) return;
                StopCurrent();
                Store.Disconnected(Failure(exception));
            }
        }
    }

    public void SetDebuggerState(DebuggerStatus state)
    {
        lock (gate)
        {
            if (Store.DebuggerContext().Target?.Identity.ProcessId != state.ProcessId) return;
            var paused = state.State == "paused";
            if (paused && !debuggerPaused && current is { } active)
            { active.Execution.Cancel(); active.DetailsRequest?.Cancel(); active.Mutation?.Cancel(); }
            if (!paused && debuggerPaused)
            { if (current is { } resumed) { resumed.Execution.Dispose(); resumed.Execution = new(); } Store.RequireTree(); }
            debuggerPaused = paused;
            Store.SetDebugger(state);
        }
    }

    public bool SupportsSelection { get { lock (gate) return current?.Peer?.Supports(DevToolsCapabilities.Selection) == true; } }
    public bool SupportsInspection { get { lock (gate) return current?.Peer?.Supports(DevToolsCapabilities.Inspection) == true; } }

    public bool Supports(string capability) { lock (gate) return current?.Peer?.Supports(capability) == true; }
    public void SetFrameUpdates(bool enabled) { lock (gate) if (current is { } active) active.WatchFrames = enabled; }
    public void CancelDetailRequests()
    {
        lock (gate) if (current is { } active) { var previous = active.DetailsRequest; active.DetailsRequest = null; previous?.Cancel(); }
    }
    public Task CaptureDetailsAsync(string elementId, bool styles = true, bool geometry = true, CancellationToken token = default) =>
        ReadDetailsAsync(elementId, styles, geometry, false, token);
    public Task RefreshFramesAsync(CancellationToken token = default) => ReadDetailsAsync(null, false, false, true, token);

    private async Task ReadDetailsAsync(string? id, bool styles, bool geometry, bool frames, CancellationToken token)
    {
        Connection active;
        CancellationTokenSource operation;
        RequestContext context;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var state = Store.Read();
            if (debuggerPaused || current?.Peer?.RemoteTarget is not { State: "running" } target || state.Connection is InspectorConnectionStatus.Disconnected or InspectorConnectionStatus.Connecting ||
                id is not null && (state.IsRetained || state.Tree?.Elements.Any(e => e.Id == id && e.Kind == "element") != true))
            { Store.ReportDetails("Live details are unavailable for this capture. Retained details remain readable."); return; }
            active = current;
            active.DetailsRequest?.Cancel();
            operation = CancellationTokenSource.CreateLinkedTokenSource(token, active.Token, active.Execution.Token);
            operation.CancelAfter(TimeSpan.FromSeconds(2));
            active.DetailsRequest = operation;
            context = new(target.Identity.SessionId, target.Generation, target.StopId);
        }
        var entered = false;
        try
        {
            await active.Requests.WaitAsync(operation.Token).ConfigureAwait(false);
            entered = true;
            if (styles)
            {
                var value = await active.Peer!.RequestAsync<StyleDetails>(DevToolsProtocol.GetStyles, DevToolsJson.Write(new NodeRequest(context, id!)), deadline, operation.Token).ConfigureAwait(false);
                lock (gate) if (Active(active) && active.DetailsRequest == operation)
                { Store.Bind(active.Peer.RemoteTarget!); if (Store.Apply(value)) Store.Responsive(); }
            }
            if (geometry)
            {
                var value = await active.Peer!.RequestAsync<GeometryDetails>(DevToolsProtocol.GetGeometry, DevToolsJson.Write(new NodeRequest(context, id!)), deadline, operation.Token).ConfigureAwait(false);
                lock (gate) if (Active(active) && active.DetailsRequest == operation)
                { Store.Bind(active.Peer.RemoteTarget!); if (Store.Apply(value)) Store.Responsive(); }
            }
            if (frames)
            {
                var value = await active.Peer!.RequestAsync<FrameBatch>(DevToolsProtocol.GetFrames, DevToolsJson.Write(new ContextRequest(context)), deadline, operation.Token).ConfigureAwait(false);
                lock (gate) if (Active(active) && active.DetailsRequest == operation)
                { Store.Bind(active.Peer.RemoteTarget!); if (Store.Apply(value, resynchronize: true) == RevisionOutcome.Accepted) Store.Responsive(); active.FrameGap = false; }
            }
            lock (gate) if (Active(active) && active.DetailsRequest == operation) Store.ReportDetails(null);
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or DevToolsException)
        {
            lock (gate) if (Active(active) && active.DetailsRequest == operation && !token.IsCancellationRequested)
            {
                if (error is IOException or OperationCanceledException or DevToolsException { Code: DevToolsErrors.Cancelled })
                    Store.Unresponsive("The target did not respond in time. Retained captures remain available.");
                Store.ReportDetails(frames ? "Target frame history is unavailable. Retained history remains readable." : "Requested styles or geometry are unavailable. Retained details remain readable.");
            }
        }
        finally
        {
            if (entered) active.Requests.Release();
            lock (gate) if (active.DetailsRequest == operation) active.DetailsRequest = null;
            operation.Dispose();
        }
    }

    public Task HighlightSelectionAsync(CancellationToken token = default)
    {
        var selected = Store.Read().Selection;
        return MutateAsync(DevToolsProtocol.Select, context => DevToolsJson.Write(new SelectionRequest(context, selected?.Kind ?? "element", selected?.NodeId)), token);
    }
    /// <summary>Removes the app overlay while the inspector keeps its own selection.</summary>
    public Task ClearHighlightAsync(CancellationToken token = default) =>
        MutateAsync(DevToolsProtocol.Select, context => DevToolsJson.Write(new SelectionRequest(context, "element", null)), token, keepLocalSelection: true);
    public Task SetInspectModeAsync(bool enabled, CancellationToken token = default) =>
        MutateAsync(DevToolsProtocol.Inspect, context => DevToolsJson.Write(new InspectionRequest(context, enabled)), token);

    private async Task MutateAsync(string method, Func<RequestContext, JsonNode> parameters, CancellationToken token, bool keepLocalSelection = false)
    {
        Connection active;
        CancellationTokenSource operation;
        JsonNode payload;
        DateTimeOffset deadline;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var data = Store.Read();
            var capability = method == DevToolsProtocol.Inspect ? DevToolsCapabilities.Inspection : DevToolsCapabilities.Selection;
            if (debuggerPaused || current?.Peer?.RemoteTarget is not { State: "running" } target || data.IsRetained || data.Tree is null ||
                data.Connection != InspectorConnectionStatus.Connected || !current.Peer.Supports(capability))
            { Store.Report("App overlays and picking are unavailable for this capture. Cached navigation remains available."); return; }
            active = current;
            active.Mutation?.Cancel();
            operation = CancellationTokenSource.CreateLinkedTokenSource(token, active.Token, active.Execution.Token);
            deadline = DateTimeOffset.UtcNow.AddSeconds(2);
            operation.CancelAfter(TimeSpan.FromSeconds(2));
            active.Mutation = operation;
            payload = parameters(new(target.Identity.SessionId, target.Generation, target.StopId));
        }
        try
        {
            var resync = false;
            var result = await active.Peer!.RequestAsync<SelectionResult>(method, payload, deadline, operation.Token).ConfigureAwait(false);
            if (active.Peer.Supports(DevToolsCapabilities.PagedTree))
            {
                await ApplyPagedSelectionAsync(active, result, keepLocalSelection, operation.Token).ConfigureAwait(false);
                return;
            }
            lock (gate)
            {
                if (!Active(active) || active.Mutation != operation) return;
                Store.Bind(active.Peer.RemoteTarget!);
                if (Store.Apply(result, keepLocalSelection) == RevisionOutcome.ResyncRequired)
                { active.PendingSelection = result; Store.RequireTree(); resync = true; }
            }
            if (resync) await RefreshAsync(active, active.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or DevToolsException)
        {
            lock (gate) if (Active(active) && active.Mutation == operation)
                Store.Report("App selection is unavailable or expired. It will not be retried after continue.");
        }
        finally
        {
            lock (gate) if (active.Mutation == operation) active.Mutation = null;
            operation.Dispose();
        }
    }

    public Task RefreshAsync(CancellationToken token = default)
    {
        Connection? active;
        lock (gate) { ObjectDisposedException.ThrowIf(disposed, this); active = current; }
        return active?.Peer is null ? Task.CompletedTask : RefreshAsync(active, token);
    }

    private async Task RefreshAsync(Connection active, CancellationToken token)
    {
        if (active.Peer?.Supports(DevToolsCapabilities.PagedTree) == true)
        {
            await RefreshPagedAsync(active, token).ConfigureAwait(false);
            return;
        }
        CancellationToken execution;
        lock (gate) { if (debuggerPaused || !Active(active)) return; execution = active.Execution.Token; }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, active.Token, execution);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        lifetime.CancelAfter(TimeSpan.FromSeconds(2));
        var entered = false;
        try
        {
            await active.Requests.WaitAsync(lifetime.Token).ConfigureAwait(false);
            entered = true;
            TargetState target;
            lock (gate)
            {
                if (!Active(active) || active.Peer?.RemoteTarget is not { } state) return;
                target = state;
                Store.Bind(target);
                if (target.State != "running") return;
            }
            var context = new RequestContext(target.Identity.SessionId, target.Generation, target.StopId);
            var snapshot = await active.Peer!.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, DevToolsJson.Write(new ContextRequest(context)), deadline, lifetime.Token).ConfigureAwait(false);
            lock (gate)
            {
                if (!Active(active) || debuggerPaused) return;
                Store.Bind(active.Peer.RemoteTarget!);
                Store.Apply(snapshot);
                if (active.PendingSelection is { } selection && Store.Apply(selection) != RevisionOutcome.ResyncRequired)
                { active.PendingSelection = null; active.SelectionResync = false; }
            }
            if (active.Peer.Supports(DevToolsCapabilities.Selection) && active.Peer.Supports(DevToolsCapabilities.Inspection))
            {
                var selection = await active.Peer.RequestAsync<SelectionResult>(DevToolsProtocol.GetSelection, DevToolsJson.Write(new ContextRequest(context)), deadline, lifetime.Token).ConfigureAwait(false);
                lock (gate)
                {
                    if (!Active(active)) return;
                    if (Store.Apply(selection) == RevisionOutcome.ResyncRequired)
                    {
                        active.PendingSelection = selection;
                        if (!active.SelectionResync)
                        {
                            active.SelectionResync = true;
                            Store.RequireTree("The picked instance needs a newer complete tree.");
                            Interlocked.Exchange(ref active.Gap, 1);
                            active.Messages.Writer.TryWrite((DevToolsProtocol.SessionChanged, DevToolsJson.Write(active.Peer.RemoteTarget!)));
                        }
                        else Store.Report("The picked instance is unavailable in the complete capture.");
                    }
                    else { active.PendingSelection = null; active.SelectionResync = false; }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or DevToolsException)
        {
            lock (gate)
            {
                if (!Active(active) || debuggerPaused) return;
                if (exception is DevToolsException { Code: DevToolsErrors.StaleSession })
                { Store.Bind(active.Peer!.RemoteTarget!); Store.RequireTree("The execution context changed. Refresh the capture."); }
                else Store.Unresponsive(Failure(exception));
            }
        }
        finally { if (entered) active.Requests.Release(); }
    }

    private async Task ConsumeAsync(Connection active)
    {
        try
        {
            await foreach (var item in active.Messages.Reader.ReadAllAsync(active.Token).ConfigureAwait(false))
            {
                var resync = false;
                SelectionResult? pagedSelection = null;
                TreeInvalidation? invalidation = null;
                lock (gate)
                {
                    if (!Active(active)) return;
                    if (debuggerPaused) { Interlocked.Exchange(ref active.Gap, 1); continue; }
                    Store.Bind(active.Peer!.RemoteTarget!);
                    if (Interlocked.Exchange(ref active.Gap, 0) != 0)
                    { Store.RequireTree("Updates were missed. Resynchronizing the tree."); resync = true; }
                    if (item.Method == DevToolsProtocol.SessionChanged)
                    { Store.RequireTree(); resync = active.Peer.RemoteTarget!.State == "running"; }
                    else if (item.Method == DevToolsProtocol.TreeChanged)
                        resync |= Store.Apply(DevToolsJson.Read<TreeDelta>(item.Data)) == RevisionOutcome.ResyncRequired;
                    else if (item.Method == DevToolsProtocol.TreeInvalidated)
                        invalidation = DevToolsJson.Read<TreeInvalidation>(item.Data);
                    else if (item.Method == DevToolsProtocol.FramesAdded && active.WatchFrames)
                    {
                        active.FrameGap |= Store.Apply(DevToolsJson.Read<FrameBatch>(item.Data)) == RevisionOutcome.ResyncRequired;
                    }
                    else if (item.Method == DevToolsProtocol.SelectionChanged)
                    {
                        var selection = DevToolsJson.Read<SelectionResult>(item.Data);
                        if (active.Peer.Supports(DevToolsCapabilities.PagedTree)) pagedSelection = selection;
                        else if (Store.Apply(selection) == RevisionOutcome.ResyncRequired)
                        { active.PendingSelection = selection; Store.RequireTree(); resync = true; }
                    }
                }
                if (resync) await RefreshAsync(active, active.Token).ConfigureAwait(false);
                else if (invalidation is not null) await InvalidatePagedAsync(active, invalidation).ConfigureAwait(false);
                if (pagedSelection is not null) await ApplyPagedSelectionAsync(active, pagedSelection, false, active.Token).ConfigureAwait(false);
                bool readFrames;
                lock (gate) readFrames = Active(active) && active.WatchFrames && active.FrameGap;
                if (readFrames) await RefreshFramesAsync(active.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or DevToolsException or IOException)
        {
            lock (gate)
            {
                if (!Active(active)) return;
                StopCurrent();
                Store.Disconnected(Failure(exception));
            }
        }
    }

    private async Task ObserveCloseAsync(Connection active)
    {
        await active.Peer!.Closed.ConfigureAwait(false);
        lock (gate)
        {
            if (!Active(active)) return;
            StopCurrent();
            Store.Disconnected("Connection closed. The last capture remains available.");
        }
    }

    public void Disconnect()
    {
        lock (gate) { StopCurrent(); Store.Disconnected("Disconnected. The last capture remains available."); }
    }

    public void Clear()
    {
        lock (gate) { StopCurrent(); Store.Clear(); }
    }

    private bool Active(Connection active) => !disposed && current == active && epoch == active.Epoch;
    private void StopCurrent()
    {
        epoch++;
        var previous = current;
        current = null;
        visibleBranches = null;
        if (previous is null) return;
        previous.Execution.Cancel();
        previous.Execution.Dispose();
        previous.Lifetime.Cancel();
        previous.Lifetime.Dispose();
        previous.Messages.Writer.TryComplete();
        previous.Peer?.Dispose();
    }
    private static string Failure(Exception exception) => exception switch
    {
        OperationCanceledException => "The target did not respond in time. Cached data is still available.",
        DevToolsException { Code: DevToolsErrors.Unavailable } => "A complete capture is unavailable. Cached data is still available.",
        DevToolsException { Code: DevToolsErrors.Busy } => "The target UI is busy. Refresh to try again.",
        DevToolsException { Code: DevToolsErrors.StaleSession } => "The target session changed. Choose it again from discovery.",
        _ => "The target connection is unavailable. Cached data is still available."
    };

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            StopCurrent();
            disposed = true;
            Store.Disconnected();
        }
    }

    private sealed class Connection(long epoch, CancellationTokenSource lifetime)
    {
        internal long Epoch { get; } = epoch;
        internal CancellationTokenSource Lifetime { get; } = lifetime;
        internal CancellationToken Token { get; } = lifetime.Token;
        internal DevToolsPeer? Peer { get; set; }
        internal SemaphoreSlim Requests { get; } = new(1, 1);
        internal PagedTreeCache Paged { get; set; } = new();
        internal CancellationTokenSource Execution { get; set; } = new();
        internal int Gap;
        internal CancellationTokenSource? Mutation;
        internal SelectionResult? PendingSelection;
        internal bool SelectionResync;
        internal bool WatchFrames;
        internal bool FrameGap;
        internal CancellationTokenSource? DetailsRequest;
        internal Channel<(string Method, JsonNode Data)> Messages { get; } = Channel.CreateBounded<(string, JsonNode)>(new BoundedChannelOptions(8)
        { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    }
}
