using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class InspectorClientTests
{
    [Fact]
    public async Task Connecting_changes_reconnect_and_target_exit_leave_a_navigable_capture()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store, target.Directory);
        Assert.Single(await client.DiscoverAsync(TestContext.Current.CancellationToken));
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        Assert.Equal(InspectorConnectionStatus.Connected, store.Read().Connection);
        Assert.NotNull(store.Read().Tree);
        Assert.Equal("Nazor DevTools - DevToolsAgent", store.WindowTitle);
        var firstGeneration = store.Read().Tree!.Capture.Generation;
        await target.CommandAsync("change", "CHANGED");
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Elements.Any(e => e.Text == "left-1"));
        var retained = store.Read().Tree!;
        client.Disconnect();
        Assert.False(target.Process.HasExited);
        Assert.True(store.Select("component", retained.ComponentRoots[0]));
        Assert.True(store.Read().IsRetained);
        await target.CommandAsync("ping", "PONG");
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        Assert.True(store.Read().Tree!.Capture.Generation > firstGeneration);
        Assert.Equal(1, store.Read().Tree!.Revision);
        await target.SendAsync("exit");
        await AgentProcess.UntilAsync(() => store.Read().Connection == InspectorConnectionStatus.Disconnected);
        Assert.True(store.Select("element", store.Read().Tree!.ElementRoots[0]));
        Assert.Equal("left-1", store.Read().Tree!.Elements.First(e => e.Kind == "text").Text);
    }

    [Fact]
    public async Task A_frozen_target_cannot_block_local_selection_or_relabel_retained_data_as_fresh()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        var capture = store.Read().Tree!;
        await target.CommandAsync("freeze", "FROZEN");
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var refresh = client.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(store.Select("component", capture.ComponentRoots[0]));
        Assert.Equal(capture.Capture, store.Read().Tree!.Capture);
        await refresh.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
        Assert.Equal(InspectorConnectionStatus.Unresponsive, store.Read().Connection);
        Assert.True(store.Read().IsRetained);
        Assert.Equal(capture.Capture, store.Read().Tree!.Capture);
        await target.SendAsync("resume");
        await target.CommandAsync("ping", "PONG");
        await client.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(InspectorConnectionStatus.Connected, store.Read().Connection);
        client.Dispose();
        await target.CommandAsync("ping", "PONG");
    }

    [Fact]
    public async Task Switching_targets_and_replacing_sessions_cannot_publish_old_callbacks_or_commands()
    {
        using var a = await AgentProcess.StartAsync();
        using var b = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        var old = client.ConnectAsync(a.Descriptor, TestContext.Current.CancellationToken);
        await client.ConnectAsync(b.Descriptor, TestContext.Current.CancellationToken);
        await old;
        Assert.Equal(b.Descriptor.Identity, store.Read().Target!.Identity);
        var selected = store.Read().Tree!.ComponentRoots[0];
        store.Select("component", selected);
        await a.CommandAsync("change", "CHANGED");
        Assert.Equal(b.Descriptor.Identity.SessionId, store.Read().Tree!.Capture.SessionId);
        var replacement = await b.ReplaceAsync();
        await AgentProcess.UntilAsync(() => store.Read().Connection == InspectorConnectionStatus.Disconnected);
        await client.ConnectAsync(replacement, TestContext.Current.CancellationToken);
        Assert.Null(store.Read().Selection);
        Assert.Equal(replacement.Identity.SessionId, store.Read().Tree!.Capture.SessionId);
        client.Clear();
        Assert.Null(store.Read().Tree);
        await b.CommandAsync("ping", "PONG");
    }

    [Fact]
    public async Task Cancelled_connection_attempts_do_not_leave_the_shell_connecting_or_reconnect_later()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await client.ConnectAsync(target.Descriptor, cancellation.Token);
        Assert.Equal(InspectorConnectionStatus.Disconnected, store.Read().Connection);
        Assert.Null(store.Read().Tree);
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        Assert.NotNull(store.Read().Tree);
    }

    [Fact]
    public async Task Pause_step_continue_and_exit_keep_capture_contexts_separate_without_live_pause_reads()
    {
        using var server = new FakeTarget();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(server.Descriptor, TestContext.Current.CancellationToken);
        var peer = await server.Ready.Task.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
        var count = Volatile.Read(ref server.Requests);
        await server.TransitionAsync(peer, "paused", "stop-1");
        await AgentProcess.UntilAsync(() => store.Read().Target!.StopId == "stop-1");
        await client.RefreshAsync(TestContext.Current.CancellationToken);
        await client.CaptureDetailsAsync("e", token: TestContext.Current.CancellationToken);
        await client.RefreshFramesAsync(TestContext.Current.CancellationToken);
        await client.SetInspectModeAsync(true, TestContext.Current.CancellationToken);
        await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(count, Volatile.Read(ref server.Requests));
        Assert.Contains("unavailable", store.Read().Message!);
        Assert.True(store.Read().IsRetained);
        Assert.True(store.Select("element", "e"));
        store.Apply(CapturedData.Tree(stamp: server.Stamp()));
        Assert.False(store.Read().IsRetained);
        await server.TransitionAsync(peer, "paused", "stop-2");
        await AgentProcess.UntilAsync(() => store.Read().Target!.StopId == "stop-2");
        Assert.True(store.Read().IsRetained);
        Assert.Equal("stop-1", store.Read().Tree!.Capture.StopId);
        await server.TransitionAsync(peer, "running", null);
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Capture.Generation == server.Target.Generation);
        Assert.Null(store.Read().Tree!.Capture.StopId);
        await server.TransitionAsync(peer, "exited", null);
        await AgentProcess.UntilAsync(() => store.Read().Target!.State == "exited");
        peer.Dispose();
        await AgentProcess.UntilAsync(() => store.Read().Connection == InspectorConnectionStatus.Disconnected);
        Assert.True(store.Select("component", "child"));
    }

    [Fact]
    public async Task Revision_gaps_and_invalid_partial_topology_trigger_full_resync_without_partial_publication()
    {
        using var server = new FakeTarget();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(server.Descriptor, TestContext.Current.CancellationToken);
        var peer = await server.Ready.Task.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
        var invalidStates = new List<long>();
        store.Changed += () =>
        {
            var snapshot = store.Read().Tree;
            if (snapshot is not null && snapshot.Elements.Any(e => e.ProducingComponentId is { } id && snapshot.Components.All(c => c.Id != id))) invalidStates.Add(snapshot.Revision);
        };
        server.Revision = 7;
        await peer.NotifyAsync(DevToolsProtocol.TreeChanged, DevToolsJson.Write(CapturedData.Delta(7, server.Stamp())), TestContext.Current.CancellationToken);
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Revision == 7);
        Assert.False(store.Read().NeedsTree);
        server.Revision = 8;
        var broken = CapturedData.Delta(8, server.Stamp()) with { Components = [], Elements = [], RemovedComponents = ["child"] };
        await peer.NotifyAsync(DevToolsProtocol.TreeChanged, DevToolsJson.Write(broken), TestContext.Current.CancellationToken);
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Revision == 8);
        Assert.Empty(invalidStates);
        Assert.True(Volatile.Read(ref server.Requests) >= 3);
    }

    [Fact]
    public async Task Overflowed_notifications_resync_to_a_complete_snapshot_and_accept_subsequent_changes()
    {
        using var server = new FakeTarget();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(server.Descriptor, TestContext.Current.CancellationToken);
        var peer = await server.Ready.Task.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
        var block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.BlockReads = block;
        server.Revision = 50;
        await peer.NotifyAsync(DevToolsProtocol.TreeChanged, DevToolsJson.Write(CapturedData.Delta(50, server.Stamp())), TestContext.Current.CancellationToken);
        await AgentProcess.UntilAsync(() => Volatile.Read(ref server.Requests) >= 2);
        for (var revision = 51; revision <= 80; revision++)
        {
            server.Revision = revision;
            await peer.NotifyAsync(DevToolsProtocol.TreeChanged, DevToolsJson.Write(CapturedData.Delta(revision, server.Stamp())), TestContext.Current.CancellationToken);
        }
        block.SetResult();
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Revision == 80 && !store.Read().NeedsTree);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.InRange(Volatile.Read(ref server.Requests), 2, 4);
        server.Revision = 81;
        await peer.NotifyAsync(DevToolsProtocol.TreeChanged, DevToolsJson.Write(CapturedData.Delta(81, server.Stamp())), TestContext.Current.CancellationToken);
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Revision == 81);
        DevToolsJson.Write(store.Read().Tree!);
    }

    [Fact]
    public async Task A_reconnected_peer_with_an_older_generation_is_rejected_without_relabelling_old_data()
    {
        using var server = new FakeTarget();
        var store = new InspectionStore();
        store.Bind(server.Target with { Generation = 3 });
        store.Apply(CapturedData.Tree(stamp: CapturedData.Stamp(3, session: server.Target.Identity.SessionId)));
        using var client = new InspectorClient(store);
        await client.ConnectAsync(server.Descriptor, TestContext.Current.CancellationToken);
        Assert.Equal(InspectorConnectionStatus.Disconnected, store.Read().Connection);
        Assert.Equal(3, store.Read().Tree!.Capture.Generation);
        Assert.True(store.Read().IsRetained);
        Assert.Equal(0, Volatile.Read(ref server.Requests));
    }

    [Fact]
    public async Task App_picker_links_to_actual_instances_and_cancellation_never_replays_after_resume()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        Assert.True(client.SupportsSelection); Assert.True(client.SupportsInspection);
        await client.SetInspectModeAsync(true, TestContext.Current.CancellationToken);
        Assert.True(store.Read().IsInspecting);
        await target.CommandAsync("pick", "PICKED");
        await AgentProcess.UntilAsync(() => !store.Read().IsInspecting && store.Read().Selection is not null);
        var picked = store.Read().Selection!;
        var element = store.Read().Tree!.Elements.Single(e => e.Id == picked.NodeId);
        Assert.Equal("Piece", element.Name);
        var owner = store.Read().Tree!.Components.Single(c => c.Id == element.ProducingComponentId);
        Assert.Equal(2, owner.OutputElements.Length);
        Assert.Contains(owner.Parameters!, p => p.Name == "Text" && p.Value == "left-0");
        store.Select("component", owner.Id);
        await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("highlighted", owner.Id);
        store.Select("element", element.Id);
        await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("highlighted", element.Id);
        await client.ClearHighlightAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("highlighted", "NONE");
        Assert.Equal(element.Id, store.Read().Selection?.NodeId);
        await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("highlighted", element.Id);
        await client.SetInspectModeAsync(true, TestContext.Current.CancellationToken);
        await target.CommandAsync("escape", "ESCAPED");
        await AgentProcess.UntilAsync(() => !store.Read().IsInspecting);
        await target.CommandAsync("inspect-mode", "OFF");
        await target.CommandAsync("freeze", "FROZEN");
        var elapsed = Stopwatch.StartNew();
        await client.SetInspectModeAsync(true, TestContext.Current.CancellationToken);
        Assert.True(elapsed.Elapsed >= TimeSpan.FromSeconds(1.5));
        await target.SendAsync("resume");
        await target.CommandAsync("ping", "PONG");
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await target.CommandAsync("inspect-mode", "OFF");
        var projected = store.Read().Tree!.Components.Single(c => c.Key == "projected");
        store.Select("element", projected.OutputElements[0]);
        await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("change", "CHANGED");
        await AgentProcess.UntilAsync(() => !store.Read().Tree!.Components.Any(c => c.Id == projected.Id) && store.Read().Selection is null);
        Assert.False(store.Select("element", projected.OutputElements[0]));
        await target.CommandAsync("change", "CHANGED");
        await AgentProcess.UntilAsync(() => store.Read().Tree!.Components.Any(c => c.Key == "projected"));
        Assert.NotEqual(projected.Id, store.Read().Tree!.Components.Single(c => c.Key == "projected").Id);
        await client.SetInspectModeAsync(false, TestContext.Current.CancellationToken);
        await client.SetInspectModeAsync(true, TestContext.Current.CancellationToken);
        client.Disconnect();
        var end = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            await target.SendAsync("inspection");
            if (await target.LineAsync() == "CLEAR") break;
            Assert.True(DateTime.UtcNow < end);
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task A_selection_read_racing_the_full_tree_resyncs_before_publishing_an_unknown_instance()
    {
        using var server = new FakeTarget(selectionRace: true);
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(server.Descriptor, TestContext.Current.CancellationToken);
        await AgentProcess.UntilAsync(() => Volatile.Read(ref server.Requests) >= 4 &&
            store.Read() is { Selection.NodeId: "new", NeedsTree: false });
        Assert.Contains(store.Read().Tree!.Elements, e => e.Id == "new");
        Assert.Equal(2, store.Read().Tree!.Revision);
        Assert.InRange(Volatile.Read(ref server.Requests), 4, 6);
    }

    [Fact]
    public async Task Style_geometry_and_frame_reads_remain_explicit_and_do_not_create_target_frames()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        Assert.True(client.Supports(DevToolsCapabilities.Styles)); Assert.True(client.Supports(DevToolsCapabilities.Geometry)); Assert.True(client.Supports(DevToolsCapabilities.Frames));
        var piece = store.Read().Tree!.Elements.First(e => e.Name == "Piece");
        await client.CaptureDetailsAsync(piece.Id, token: TestContext.Current.CancellationToken);
        Assert.Contains(store.Read().Styles[piece.Id].Computed, p => p.Name == "padding-top" && p.Value == "4px");
        Assert.Equal(4, store.Read().Geometry[piece.Id].Padding.Top);
        Assert.NotNull(store.Read().Geometry[piece.Id].ContentBox);
        await target.CommandAsync("frame-count", "0");
        await client.RefreshFramesAsync(TestContext.Current.CancellationToken);
        Assert.Null(store.ReadFrames());
        await target.CommandAsync("frame-count", "0");
        await target.CommandAsync("frames", "FRAMES");
        client.SetFrameUpdates(true);
        await client.RefreshFramesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(120, store.ReadFrames()!.Frames.Length); Assert.Equal(6, store.ReadFrames()!.Frames[0].Number);
        var treeEvents = 0; store.Changed += () => treeEvents++;
        var revision = store.ReadFrames()!.Revision;
        await target.CommandAsync("frame", "FRAME");
        await AgentProcess.UntilAsync(() => store.ReadFrames()!.Frames[^1].Number == 126);
        Assert.True(store.ReadFrames()!.Revision > revision); Assert.Equal(0, treeEvents);
        client.SetFrameUpdates(false);
        await target.CommandAsync("frame", "FRAME");
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(126, store.ReadFrames()!.Frames[^1].Number);
        client.SetFrameUpdates(true);
        await client.RefreshFramesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(127, store.ReadFrames()!.Frames[^1].Number);
        await client.RefreshFramesAsync(TestContext.Current.CancellationToken);
        await target.CommandAsync("frame-count", "127");
        var old = store.Read().Styles[piece.Id].Capture;
        await target.CommandAsync("freeze", "FROZEN");
        await client.CaptureDetailsAsync(piece.Id, token: TestContext.Current.CancellationToken);
        Assert.Equal(old, store.Read().Styles[piece.Id].Capture);
        Assert.Equal(InspectorConnectionStatus.Unresponsive, store.Read().Connection);
        await target.SendAsync("resume"); await target.CommandAsync("ping", "PONG");
        await client.RefreshAsync(TestContext.Current.CancellationToken);
        var hidden = store.Read().Tree!.Elements.First(e => e.Name == "Hidden");
        await client.CaptureDetailsAsync(hidden.Id, token: TestContext.Current.CancellationToken);
        Assert.True(store.Read().Styles.ContainsKey(hidden.Id)); Assert.False(store.Read().Geometry.ContainsKey(hidden.Id));
        client.Disconnect();
        Assert.NotNull(store.ReadFrames()); Assert.True(store.Read().Styles.ContainsKey(piece.Id));
    }

    private sealed class FakeTarget : IDisposable
    {
        private readonly NamedPipeServerStream stream;
        private readonly CancellationTokenSource stopping = new();
        private readonly Task serving;
        private readonly string file;
        internal readonly TaskCompletionSource<DevToolsPeer> Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TargetState Target { get; private set; }
        internal LocalDevToolsDescriptor Descriptor { get; }
        internal int Requests;
        internal volatile TaskCompletionSource? BlockReads;
        internal volatile int Revision = 1;
        internal FakeTarget(bool selectionRace = false)
        {
            var directory = Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
            LocalDevTools.PrepareDirectory(directory);
            var session = Guid.NewGuid().ToString("N");
            Target = CapturedData.Target(session: session) with { Identity = CapturedData.Target(session: session).Identity with { ProcessId = Environment.ProcessId } };
            using var process = Process.GetCurrentProcess();
            Descriptor = new(1, Target.Identity, process.StartTime.ToUniversalTime().ToString("O"), LocalDevTools.PipeName(directory, session), LocalDevTools.ProcessStartToken(Environment.ProcessId));
            stream = new(Descriptor.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Descriptor.PipeName, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            file = LocalDevTools.Save(directory, Descriptor);
            serving = Task.Run(async () =>
            {
                try
                {
                    await stream.WaitForConnectionAsync(stopping.Token);
                    using var peer = new DevToolsPeer(stream, stream, "Fixture", selectionRace ? [DevToolsCapabilities.Tree, DevToolsCapabilities.Selection, DevToolsCapabilities.Inspection] : [DevToolsCapabilities.Tree], Target);
                    peer.RequestHandler = async (request, token) =>
                    {
                        Interlocked.Increment(ref Requests);
                        if (BlockReads is { } block) await block.Task.WaitAsync(token);
                        if (selectionRace && request.Method == DevToolsProtocol.GetSelection)
                        {
                            Revision = 2;
                            return DevToolsJson.Write(new SelectionResult(Stamp(), "element", "new") { Revision = 1, IsInspecting = false });
                        }
                        var tree = CapturedData.Tree(Revision, Stamp());
                        if (selectionRace && Revision == 2) tree = tree with
                        {
                            Elements = [tree.Elements[0] with { Children = ["text", "new"] }, tree.Elements[1],
                                new("new", "e", "element", "New", null, "child", "c", []) { RootId = "c" }]
                        };
                        return DevToolsJson.Write(tree);
                    };
                    peer.Start(); Ready.TrySetResult(peer);
                    await peer.Closed;
                }
                catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or IOException) { Ready.TrySetException(exception); }
            });
        }
        internal CaptureStamp Stamp() => CapturedData.Stamp(Target.Generation, Target.StopId, Target.Identity.SessionId);
        internal async Task TransitionAsync(DevToolsPeer peer, string state, string? stop)
        {
            Target = Target with { Generation = Target.Generation + 1, State = state, StopId = stop };
            peer.SetTargetState(Target);
            await peer.NotifyAsync(DevToolsProtocol.SessionChanged, DevToolsJson.Write(Target), TestContext.Current.CancellationToken);
        }
        public void Dispose() { stopping.Cancel(); stream.Dispose(); File.Delete(file); serving.GetAwaiter().GetResult(); stopping.Dispose(); }
    }
}
