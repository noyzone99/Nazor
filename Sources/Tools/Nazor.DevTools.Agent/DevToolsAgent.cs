using System.Diagnostics;
using System.Globalization;
using Nazor.Rendering;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using Nazor.Diagnostics;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed class DevToolsAgent : IDisposable
{
    private static readonly string ProcessInstance = Guid.NewGuid().ToString("N");
    private static readonly string RuntimeInstance = Guid.NewGuid().ToString("N");
    private static readonly string ApplicationInstance = Guid.NewGuid().ToString("N");
    private readonly DevToolsSession session;
    private readonly CancellationTokenSource stopping = new();
    private readonly SemaphoreSlim changed = new(0, 1);
    private readonly object gate = new();
    private readonly EventHandler processExit;
    private readonly Task accepting;
    private readonly DebuggerWindow debuggerWindow;
    private NamedPipeServerStream? pipe;
    private Client? client;
    private long generation;
    private int dirty;
    private int selectionDirty;
    private int frameDirty;
    private int uiPending;
    private int cleanupPending;
    private int stopped;

    public DevToolsAgent(DevToolsSession session, string? discoveryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.Renderer.Dispatcher.AssertAccess();
        this.session = session;
        var directory = Path.GetFullPath(discoveryDirectory ?? LocalDevTools.DiscoveryDirectory);
        LocalDevTools.PrepareDirectory(directory);
        var identity = new TargetIdentity(Environment.ProcessId, ProcessInstance, RuntimeInstance, ApplicationInstance, Guid.NewGuid().ToString("N"), session.SessionId);
        using var process = Process.GetCurrentProcess();
        Descriptor = new(1, identity, process.StartTime.ToUniversalTime().ToString("O"), LocalDevTools.PipeName(directory, session.SessionId), LocalDevTools.ProcessStartToken(Environment.ProcessId)) {
            ApplicationName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? process.ProcessName,
            InspectorPath = InspectorLocator.Find(AppContext.BaseDirectory), InspectorVersion = InspectorLauncher.Version
        };
        processExit = (_, _) => Stop();
        try
        {
            pipe = CreatePipe();
            DiscoveryFile = LocalDevTools.Save(directory, Descriptor);
            debuggerWindow = DebuggerRoots.Add(session, identity);
            session.TreeChanged += OnTreeChanged;
            session.StatesChanged += OnStatesChanged;
            session.OverlayChanged += OnOverlayChanged;
            session.FrameRecorded += OnFrameRecorded;
            AppDomain.CurrentDomain.ProcessExit += processExit;
            accepting = Task.Run(AcceptAsync);
        }
        catch { pipe?.Dispose(); throw; }
    }

    public LocalDevToolsDescriptor Descriptor { get; }
    public string DiscoveryFile { get; }
    public Task Completion => accepting;
    public bool IsDisposed => Volatile.Read(ref stopped) != 0;
    internal event Action? Disposed;
    public bool IsConnected { get { lock (gate) return client is not null; } }

    private NamedPipeServerStream CreatePipe()
    {
        var stream = new NamedPipeServerStream(Descriptor.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 4096, 4096);
        try
        {
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Descriptor.PipeName, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }

    private async Task AcceptAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            Client? connected = null;
            try
            {
                NamedPipeServerStream listening;
                lock (gate) listening = pipe ??= CreatePipe();
                await listening.WaitForConnectionAsync(stopping.Token).ConfigureAwait(false);
                var target = new TargetState(Descriptor.Identity, Interlocked.Increment(ref generation), "running", null);
                connected = new(new(listening, listening, "Nazor Agent", [DevToolsCapabilities.Tree, DevToolsCapabilities.Selection, DevToolsCapabilities.Inspection, DevToolsCapabilities.Styles, DevToolsCapabilities.Geometry, DevToolsCapabilities.Frames], target), new(session), target);
                lock (gate)
                {
                    if (stopped != 0) { connected.Peer.Dispose(); break; }
                    client = connected;
                }
                var active = connected;
                active.Peer.RequestHandler = (request, token) => HandleAsync(active, request, token);
                active.Peer.Start();
                _ = EnforceInitializationAsync(active);
                var publishing = PublishAsync(active);
                await active.Peer.Closed.ConfigureAwait(false);
                active.Lifetime.Cancel();
                await publishing.ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException or OperationCanceledException or DevToolsException)
            {
                if (!stopping.IsCancellationRequested) await Task.Delay(100).ConfigureAwait(false);
            }
            finally
            {
                connected?.Peer.Dispose();
                connected?.Lifetime.Cancel();
                lock (gate) { client = null; pipe?.Dispose(); pipe = null; }
                Interlocked.Exchange(ref dirty, 0);
                QueueCleanup();
            }
        }
    }

    private static async Task EnforceInitializationAsync(Client active)
    {
        await Task.WhenAny(active.Peer.Closed, Task.Delay(TimeSpan.FromSeconds(3), active.Lifetime.Token)).ConfigureAwait(false);
        if (!active.Peer.IsInitialized) active.Peer.Dispose();
    }

    private async Task<JsonNode?> HandleAsync(Client active, DevToolsRequest request, CancellationToken token)
    {
        if (request.Method is DevToolsProtocol.GetStyles or DevToolsProtocol.GetGeometry or DevToolsProtocol.GetFrames)
        {
            var node = request.Method == DevToolsProtocol.GetFrames ? null : DevToolsJson.Read<NodeRequest>(request.Parameters);
            using var detailLifetime = CancellationTokenSource.CreateLinkedTokenSource(token, active.Lifetime.Token, stopping.Token);
            await active.Work.WaitAsync(detailLifetime.Token).ConfigureAwait(false);
            try
            {
                var exporter = new DetailExporter(session);
                if (request.Method == DevToolsProtocol.GetStyles)
                    return await DetailExporter.SerializeAsync(await OnUiAsync(active, () => exporter.Styles(active.Target, node!.ElementId), detailLifetime.Token).ConfigureAwait(false), detailLifetime.Token).ConfigureAwait(false);
                if (request.Method == DevToolsProtocol.GetGeometry)
                    return await DetailExporter.SerializeAsync(await OnUiAsync(active, () => exporter.Geometry(active.Target, node!.ElementId), detailLifetime.Token).ConfigureAwait(false), detailLifetime.Token).ConfigureAwait(false);
                var batch = await OnUiAsync(active, () => ReadFrames(active, true)!, detailLifetime.Token).ConfigureAwait(false);
                var payload = await DetailExporter.SerializeAsync(batch, detailLifetime.Token).ConfigureAwait(false);
                active.HasFrames = true;
                Interlocked.Exchange(ref frameDirty, 1);
                Signal();
                return payload;
            }
            finally { active.Work.Release(); }
        }
        if (request.Method == DevToolsProtocol.GetSelection)
        {
            await active.Work.WaitAsync(token).ConfigureAwait(false);
            try { return DevToolsJson.Write(await OnUiAsync(active, () => ReadSelection(active), token).ConfigureAwait(false)); }
            finally { active.Work.Release(); }
        }
        if (request.Method is DevToolsProtocol.Select or DevToolsProtocol.Inspect)
        {
            var inspection = request.Method == DevToolsProtocol.Inspect ? DevToolsJson.Read<InspectionRequest>(request.Parameters) : null;
            var selection = request.Method == DevToolsProtocol.Select ? DevToolsJson.Read<SelectionRequest>(request.Parameters) : null;
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(token, active.Lifetime.Token, stopping.Token);
            await active.Work.WaitAsync(operation.Token).ConfigureAwait(false);
            try
            {
                return DevToolsJson.Write(await OnUiAsync(active, () =>
                {
                    request.ApplyMutation(() =>
                    {
                        if (request.Method == DevToolsProtocol.Inspect)
                        {
                            if (inspection!.Enabled) session.SetInspectMode(true);
                            else session.CancelInspection();
                        }
                        else
                        {
                            var select = selection!;
                            int? id = null;
                            if (select.NodeId is { } value)
                            {
                                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || session.NodeOf(number) is not { } node ||
                                    (select.Kind == "component" ? node is not ComponentNode : node is not (ElementNode or TextNode)))
                                    throw new DevToolsException(DevToolsErrors.Unavailable, "The selected instance no longer exists.");
                                id = number;
                            }
                            session.CancelInspection();
                            session.Select(id);
                        }
                    });
                    return ReadSelection(active);
                }, operation.Token).ConfigureAwait(false));
            }
            finally { active.Work.Release(); }
        }
        if (request.Method != DevToolsProtocol.GetTree) throw new DevToolsException(DevToolsErrors.Unsupported, "The operation is unavailable.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, active.Lifetime.Token, stopping.Token);
        await active.Work.WaitAsync(lifetime.Token).ConfigureAwait(false);
        try
        {
            var capture = await OnUiAsync(active, () =>
            {
                Interlocked.Exchange(ref dirty, 0);
                return active.Exporter.Capture(active.Target);
            }, lifetime.Token).ConfigureAwait(false);
            var payload = await TreeSnapshotExporter.SerializeAsync(capture, cancellationToken: lifetime.Token).ConfigureAwait(false);
            active.HasSnapshot = true;
            Signal();
            return payload;
        }
        finally { active.Work.Release(); }
    }

    private FrameBatch? ReadFrames(Client active, bool full)
    {
        var records = new DetailExporter(session).Frames(active.Target);
        var current = full ? records : records.Where(f => f.Number > active.LastFrame).ToArray();
        if (current.Length == 0) return null;
        var batch = new FrameBatch(DetailExporter.Stamp(active.Target), active.FrameRevision, active.FrameRevision + 1, current);
        active.FrameRevision++;
        active.LastFrame = current[^1].Number;
        return batch;
    }
    private void OnFrameRecorded(FrameInfo frame)
    {
        if (Volatile.Read(ref stopped) != 0 || Volatile.Read(ref client)?.HasFrames != true) return;
        Interlocked.Exchange(ref frameDirty, 1);
        Signal();
    }
    private void OnTreeChanged(TreeChange change) => OnStatesChanged();
    private void OnOverlayChanged()
    {
        if (Volatile.Read(ref stopped) != 0 || Volatile.Read(ref client)?.Peer.Supports(DevToolsCapabilities.Selection) != true) return;
        Interlocked.Exchange(ref selectionDirty, 1);
        Signal();
    }
    private SelectionResult ReadSelection(Client active)
    {
        var result = new SelectionResult(new(session.SessionId, active.Target.Generation, "live", null, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            session.SelectedId is { } id && session.NodeOf(id) is ComponentNode ? "component" : "element", session.SelectedId?.ToString(CultureInfo.InvariantCulture))
            { IsInspecting = session.IsInspecting, Revision = ++active.SelectionRevision };
        active.LastSelection = result;
        return result;
    }
    private void Signal()
    {
        if (changed.CurrentCount == 0) { try { changed.Release(); } catch (SemaphoreFullException) { } }
    }
    private void OnStatesChanged()
    {
        if (Volatile.Read(ref stopped) != 0) return;
        Interlocked.Exchange(ref dirty, 1);
        Signal();
    }

    private async Task PublishAsync(Client active)
    {
        try
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token, active.Lifetime.Token);
            while (!lifetime.IsCancellationRequested)
            {
                await changed.WaitAsync(lifetime.Token).ConfigureAwait(false);
                if ((!active.HasSnapshot || Volatile.Read(ref dirty) == 0 && Volatile.Read(ref selectionDirty) == 0) && (!active.HasFrames || Volatile.Read(ref frameDirty) == 0)) continue;
                await active.Work.WaitAsync(lifetime.Token).ConfigureAwait(false);
                try
                {
                    var update = await OnUiAsync(active, () =>
                    {
                        var capture = active.HasSnapshot && Interlocked.Exchange(ref dirty, 0) != 0 ? active.Exporter.Capture(active.Target) : null;
                        SelectionResult? selection = null;
                        if (Interlocked.Exchange(ref selectionDirty, 0) != 0)
                        {
                            var previous = active.LastSelection;
                            var current = ReadSelection(active);
                            if (previous?.Kind != current.Kind || previous.NodeId != current.NodeId || previous.IsInspecting != current.IsInspecting) selection = current;
                        }
                        FrameBatch? frames = null;
                        if (active.HasFrames && Interlocked.Exchange(ref frameDirty, 0) != 0)
                        {
                            try { frames = ReadFrames(active, false); }
                            catch (DevToolsException error) when (error.Code == DevToolsErrors.Unavailable) { active.HasFrames = false; }
                        }
                        return (capture, selection, frames);
                    }, lifetime.Token).ConfigureAwait(false);
                    if (update.capture is not null)
                    {
                        var payload = await TreeSnapshotExporter.SerializeAsync(update.capture, true, lifetime.Token).ConfigureAwait(false);
                        await active.Peer.NotifyAsync(DevToolsProtocol.TreeChanged, payload, lifetime.Token).ConfigureAwait(false);
                    }
                    if (update.frames is not null)
                    {
                        try
                        {
                            var payload = await DetailExporter.SerializeAsync(update.frames, lifetime.Token).ConfigureAwait(false);
                            await active.Peer.NotifyAsync(DevToolsProtocol.FramesAdded, payload, lifetime.Token).ConfigureAwait(false);
                        }
                        catch (DevToolsException error) when (error.Code == DevToolsErrors.Unavailable) { active.HasFrames = false; }
                    }
                    if (update.selection is not null && active.Peer.Supports(DevToolsCapabilities.Selection))
                        await active.Peer.NotifyAsync(DevToolsProtocol.SelectionChanged, DevToolsJson.Write(update.selection), lifetime.Token).ConfigureAwait(false);
                }
                finally { active.Work.Release(); }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or DevToolsException or ObjectDisposedException or IOException)
        { active.Peer.Dispose(); }
    }

    private Task<T> OnUiAsync<T>(Client active, Func<T> work, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref uiPending, 1, 0) != 0) throw new DevToolsException(DevToolsErrors.Busy, "The UI dispatcher already has pending collection work.");
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellation = token.Register(() => completion.TrySetCanceled(token));
        try
        {
            _ = session.Renderer.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    lock (gate)
                    {
                        if (stopped != 0 || client != active) throw new DevToolsException(DevToolsErrors.StaleSession, "The connection changed before collection.");
                    }
                    completion.TrySetResult(work());
                }
                catch (Exception exception) { completion.TrySetException(exception); }
                finally { Interlocked.Exchange(ref uiPending, 0); cancellation.Dispose(); }
            });
        }
        catch { Interlocked.Exchange(ref uiPending, 0); cancellation.Dispose(); throw; }
        return completion.Task;
    }

    private void QueueCleanup()
    {
        if (Volatile.Read(ref stopped) != 0 || Interlocked.CompareExchange(ref cleanupPending, 1, 0) != 0) return;
        _ = session.Renderer.Dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref cleanupPending, 0);
            if (Volatile.Read(ref stopped) != 0) return;
            ClearInspection();
        });
    }

    private void ClearInspection()
    {
        session.CancelInspection();
        session.Highlight(null);
        session.Select(null);
    }

    private void Stop()
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        stopping.Cancel();
        lock (gate) { client?.Peer.Dispose(); client?.Lifetime.Cancel(); pipe?.Dispose(); }
        try { File.Delete(DiscoveryFile); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    public void Dispose()
    {
        session.Renderer.Dispatcher.AssertAccess();
        DebuggerRoots.Remove(debuggerWindow);
        if (Volatile.Read(ref stopped) != 0) return;
        session.TreeChanged -= OnTreeChanged;
        session.StatesChanged -= OnStatesChanged;
        session.OverlayChanged -= OnOverlayChanged;
        session.FrameRecorded -= OnFrameRecorded;
        AppDomain.CurrentDomain.ProcessExit -= processExit;
        try { ClearInspection(); }
        finally { try { Stop(); } finally { Disposed?.Invoke(); } }
    }

    private sealed class Client(DevToolsPeer peer, TreeSnapshotExporter exporter, TargetState target)
    {
        internal DevToolsPeer Peer { get; } = peer;
        internal TreeSnapshotExporter Exporter { get; } = exporter;
        internal TargetState Target { get; } = target;
        internal SemaphoreSlim Work { get; } = new(1, 1);
        internal CancellationTokenSource Lifetime { get; } = new();
        internal volatile bool HasSnapshot;
        internal volatile bool HasFrames;
        internal long FrameRevision;
        internal long LastFrame;
        internal long SelectionRevision;
        internal SelectionResult? LastSelection;
    }
}
