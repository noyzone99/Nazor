using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Rendering;
using System.Diagnostics;

namespace Nazor.DevTools;

public static class DebuggerRoots
{
    private static readonly object Gate = new();
    public static DebuggerWindow[] Windows = [];

    internal static DebuggerWindow Add(DevToolsSession session, TargetIdentity identity)
    {
        var window = new DebuggerWindow(session, identity);
        lock (Gate) Windows = [.. Windows, window];
        return window;
    }
    internal static void Remove(DebuggerWindow window)
    {
        window.Dispose();
        lock (Gate) Windows = Windows.Where(w => w != window).ToArray();
    }
}

public sealed class DebuggerWindow
{
    public readonly string SessionId;
    public DebuggerComponent[] Components = [];
    public string TreeJson = "";
    public string[] TreeChunks = [];
    public bool Updating;
    private readonly DevToolsSession session;
    private readonly TargetState target;
    private readonly TreeSnapshotExporter exporter;
    private int pending;
    private int dirty;
    private int disposed;
    private long revision;
    private readonly object publishGate = new();
    private readonly CancellationTokenSource lifetime = new();
    private long lastCapture;
    private bool paged;

    internal DebuggerWindow(DevToolsSession session, TargetIdentity identity)
    {
        SessionId = session.SessionId;
        this.session = session;
        target = new(identity, 1, "running", null);
        exporter = new(session);
        session.TreeChanged += Changed;
        ScheduleIfNeeded();
    }
    private void Changed(TreeChange change) => ScheduleIfNeeded();
    private void ScheduleIfNeeded()
    {
        if (!paged && (Debugger.IsAttached || Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_DEBUGGER_SNAPSHOTS") == "1")) Schedule();
    }

    internal void UsePagedTree(bool enabled)
    {
        if (paged == enabled) return;
        paged = enabled;
        if (!enabled) { ScheduleIfNeeded(); return; }
        Interlocked.Increment(ref revision);
        lock (publishGate) { Components = []; TreeJson = ""; TreeChunks = []; }
    }

    internal void Remember(int id, object instance)
    {
        var key = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (Components.Any(component => component.Id == key)) return;
        Components = [.. Components.TakeLast(63), new(key, instance)];
    }

    internal void Schedule()
    {
        Interlocked.Exchange(ref dirty, 1);
        if (Volatile.Read(ref disposed) != 0 || Interlocked.Exchange(ref pending, 1) != 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                var delay = lastCapture == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(250) - Stopwatch.GetElapsedTime(lastCapture);
                if (delay > TimeSpan.Zero) await Task.Delay(delay, lifetime.Token).ConfigureAwait(false);
                if (Volatile.Read(ref disposed) != 0) return;
                var context = session.Renderer.Dispatcher.SynchronizationContext;
                if (context is not null) context.Post(_ => Capture(), null);
                else await session.Renderer.Dispatcher.InvokeAsync(Capture).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        });
    }
    private void Capture()
    {
        Interlocked.Exchange(ref dirty, 0);
        if (Volatile.Read(ref disposed) != 0) return;
        if (paged) { Finished(); return; }
        lastCapture = Stopwatch.GetTimestamp();
        Updating = true;
        try
        {
            var capture = exporter.Capture(target);
            Components = capture.Snapshot.Components.Select(c => new DebuggerComponent(c.Id, ((ComponentNode)session.NodeOf(int.Parse(c.Id))!).Component)).ToArray();
            var capturedRevision = Interlocked.Increment(ref revision);
            _ = PublishAsync(capture, capturedRevision);
        }
        catch (DevToolsException)
        {
            Interlocked.Increment(ref revision);
            lock (publishGate) { Components = []; TreeJson = ""; TreeChunks = []; }
            Finished();
        }
        finally { Updating = false; }
    }
    private async Task PublishAsync(TreeCapture capture, long capturedRevision)
    {
        try
        {
            var json = (await TreeSnapshotExporter.SerializeAsync(capture, cancellationToken: lifetime.Token).ConfigureAwait(false)).ToJsonString();
            lock (publishGate)
                if (Volatile.Read(ref disposed) == 0 && Interlocked.Read(ref revision) == capturedRevision)
                {
                    TreeChunks = Enumerable.Range(0, (json.Length + 255) / 256).Select(i => json.Substring(i * 256, Math.Min(256, json.Length - i * 256))).ToArray();
                    Volatile.Write(ref TreeJson, json);
                }
        }
        catch (DevToolsException) { }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { Finished(); }
    }
    private void Finished()
    {
        Interlocked.Exchange(ref pending, 0);
        if (Volatile.Read(ref dirty) != 0 && Volatile.Read(ref disposed) == 0) Schedule();
    }
    internal void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        session.TreeChanged -= Changed;
        lock (publishGate) { Components = []; TreeJson = ""; TreeChunks = []; }
    }
}

public sealed class DebuggerComponent(string id, object instance)
{
    public readonly string Id = id;
    public readonly object Instance = instance;
}
