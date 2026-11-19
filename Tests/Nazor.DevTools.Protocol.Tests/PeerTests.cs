using System.IO.Pipes;
using System.Text;
using System.Text.Json.Nodes;
using Nazor.JsonRpc;

namespace Nazor.DevTools.Protocol.Tests;

public sealed class PeerTests
{
    private static readonly string[] Capabilities = [DevToolsCapabilities.Tree, DevToolsCapabilities.Styles, DevToolsCapabilities.Geometry, DevToolsCapabilities.Frames, DevToolsCapabilities.Selection, DevToolsCapabilities.Inspection];
    private static JsonNode Context() => DevToolsJson.Write(new ContextRequest(ContractTests.Context()));
    private static JsonNode Selection() => DevToolsJson.Write(new SelectionRequest(ContractTests.Context(), "element", "e1"));
    private static TaskCompletionSource<T> Completion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task<T> Within<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Picker_capability_and_selection_reads_use_the_current_context_and_mutation_lease()
    {
        using var pair = await Pair.Create(capabilities: [DevToolsCapabilities.Selection, DevToolsCapabilities.Inspection]);
        Assert.True(pair.Client.Supports(DevToolsCapabilities.Inspection));
        Assert.False(pair.Client.Supports(DevToolsCapabilities.Tree));
        var mutations = 0;
        pair.Server.RequestHandler = (request, token) =>
        {
            if (request.Method == DevToolsProtocol.Inspect) request.ApplyMutation(() => mutations++);
            return Task.FromResult<JsonNode?>(DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", null) { Revision = 1, IsInspecting = mutations > 0 }));
        };
        var read = await pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.GetSelection, Context(), TestContext.Current.CancellationToken);
        Assert.False(read.IsInspecting);
        var picked = await pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Inspect, DevToolsJson.Write(new InspectionRequest(ContractTests.Context(), true)), TestContext.Current.CancellationToken);
        Assert.True(picked.IsInspecting); Assert.Equal(1, mutations);
        pair.Server.SetTargetState(ContractTests.State(2, "paused", "stop-2"));
        var paused = Completion<bool>();
        pair.Client.NotificationHandler = (method, data) => { if (method == DevToolsProtocol.SessionChanged) paused.TrySetResult(true); };
        await pair.Server.NotifyAsync(DevToolsProtocol.SessionChanged, DevToolsJson.Write(ContractTests.State(2, "paused", "stop-2")), TestContext.Current.CancellationToken);
        await Within(paused.Task);
        var error = await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Inspect,
            DevToolsJson.Write(new InspectionRequest(ContractTests.Context(2, "stop-2"), true)), TestContext.Current.CancellationToken));
        Assert.Equal(DevToolsErrors.TargetPaused, error.Code); Assert.Equal(1, mutations);
    }

    [Fact]
    public async Task Initialization_negotiates_capabilities_and_responses_can_arrive_out_of_order()
    {
        using var pair = await Pair.Create();
        var firstEntered = Completion<bool>();
        var releaseFirst = Completion<bool>();
        var count = 0;
        pair.Server.RequestHandler = async (request, token) =>
        {
            var number = Interlocked.Increment(ref count);
            if (number == 1) { firstEntered.SetResult(true); await releaseFirst.Task.WaitAsync(token); }
            return DevToolsJson.Write(ContractTests.Snapshot(number));
        };
        var first = pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken);
        await Within(firstEntered.Task);
        var second = await Within(pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken));
        Assert.Equal(2, second.Revision);
        Assert.False(first.IsCompleted);
        releaseFirst.SetResult(true);
        Assert.Equal(1, (await Within(first)).Revision);
        Assert.Equal(ContractTests.Identity, pair.Client.RemoteTarget!.Identity);
    }

    [Fact]
    public async Task Unsupported_unavailable_stale_and_paused_outcomes_are_explicit()
    {
        using var pair = await Pair.Create(capabilities: [DevToolsCapabilities.Tree, DevToolsCapabilities.Selection]);
        Assert.Equal(DevToolsErrors.Unsupported, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<StyleDetails>(DevToolsProtocol.GetStyles,
            DevToolsJson.Write(new NodeRequest(ContractTests.Context(), "e1")), TestContext.Current.CancellationToken))).Code);
        Assert.Equal(DevToolsErrors.Unavailable, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken))).Code);
        Assert.Equal(DevToolsErrors.StaleSession, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree,
            DevToolsJson.Write(new ContextRequest(ContractTests.Context(2))), TestContext.Current.CancellationToken))).Code);
        pair.Server.SetTargetState(ContractTests.State(2, "paused", "stop-2"));
        Assert.Equal(DevToolsErrors.TargetPaused, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select,
            DevToolsJson.Write(new SelectionRequest(ContractTests.Context(2, "stop-2"), "element", "e1")), TestContext.Current.CancellationToken))).Code);
    }

    [Fact]
    public async Task Cancelling_a_request_while_its_frame_waits_to_be_written_keeps_the_connection_open()
    {
        GatedStream? output = null;
        using var pair = await Pair.Create(clientOutput: stream => output = new GatedStream(stream));
        pair.Server.RequestHandler = (request, token) =>
            Task.FromResult<JsonNode?>(DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", "e1")));
        output!.Hold();
        using var cancellation = new CancellationTokenSource();
        var superseded = pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), cancellation.Token);
        await Within(output.Waiting);
        cancellation.Cancel();
        output.Release();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Within(superseded));
        Assert.False(pair.Client.Closed.IsCompleted);
        Assert.Equal("e1", (await Within(pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), TestContext.Current.CancellationToken))).NodeId);
    }

    [Fact]
    public async Task Cancellation_and_continue_invalidate_queued_mutations_before_execution()
    {
        using var pair = await Pair.Create();
        var queued = Completion<(DevToolsRequest Request, CancellationToken Token)>();
        var release = Completion<bool>();
        var applied = 0;
        pair.Server.RequestHandler = async (request, token) =>
        {
            queued.SetResult((request, token));
            await release.Task;
            request.ApplyMutation(() => applied++);
            return DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", "e1"));
        };
        using var cancellation = new CancellationTokenSource();
        var pending = pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), cancellation.Token);
        var captured = await Within(queued.Task);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Within(pending));
        var cancelled = Completion<bool>();
        using var registration = captured.Token.Register(() => cancelled.TrySetResult(true));
        await Within(cancelled.Task);
        pair.Server.SetTargetState(ContractTests.State(2, "paused", "stop-2"));
        pair.Server.SetTargetState(ContractTests.State(3));
        release.SetResult(true);
        Assert.ThrowsAny<OperationCanceledException>(() => captured.Request.ApplyMutation(() => applied++));
        Assert.Equal(0, applied);
    }

    [Fact]
    public async Task Absolute_client_deadlines_expire_the_remote_mutation_lease_before_delayed_UI_work()
    {
        using var pair = await Pair.Create();
        var entered = Completion<DevToolsRequest>();
        var release = Completion<bool>();
        var finished = Completion<bool>();
        var applied = 0;
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(300);
        pair.Server.RequestHandler = async (request, token) =>
        {
            entered.SetResult(request);
            Assert.Equal(deadline.ToUniversalTime().ToString("O"), request.Parameters["deadlineUtc"]!.GetValue<string>());
            await release.Task;
            try { request.ApplyMutation(() => applied++); }
            finally { finished.TrySetResult(true); }
            return DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", "e1"));
        };
        var pending = pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), deadline, TestContext.Current.CancellationToken);
        await Within(entered.Task);
        try { await pending; Assert.Fail("The deferred mutation must expire."); }
        catch (Exception error) when (error is OperationCanceledException or DevToolsException { Code: DevToolsErrors.Cancelled }) { }
        release.SetResult(true); await Within(finished.Task);
        Assert.Equal(0, applied);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), DateTimeOffset.UtcNow.AddMinutes(1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Completed_mutation_leases_cannot_be_reused_and_live_mutations_work()
    {
        using var pair = await Pair.Create();
        DevToolsRequest? captured = null;
        var applied = 0;
        pair.Server.RequestHandler = (request, token) =>
        {
            captured = request;
            request.ApplyMutation(() => applied++);
            return Task.FromResult<JsonNode?>(DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", "e1")));
        };
        await Within(pair.Client.RequestAsync<SelectionResult>(DevToolsProtocol.Select, Selection(), TestContext.Current.CancellationToken));
        Assert.Equal(1, applied);
        Assert.Equal(DevToolsErrors.Cancelled, Assert.Throws<DevToolsException>(() => captured!.ApplyMutation(() => applied++)).Code);
        Assert.Equal(1, applied);
    }

    [Fact]
    public async Task Disconnect_and_session_change_release_pending_requests()
    {
        using var pair = await Pair.Create();
        var entered = Completion<bool>();
        pair.Server.RequestHandler = async (request, token) => { entered.TrySetResult(true); await Task.Delay(Timeout.Infinite, token); return null; };
        var pending = pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken);
        await Within(entered.Task);
        pair.Server.SetTargetState(ContractTests.State(2));
        await pair.Server.NotifyAsync(DevToolsProtocol.SessionChanged, DevToolsJson.Write(ContractTests.State(2)), TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<DevToolsException>(() => Within(pending));
        Assert.Contains(exception.Code, new[] { DevToolsErrors.Cancelled, DevToolsErrors.StaleSession });
        entered = Completion<bool>();
        var next = pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, DevToolsJson.Write(new ContextRequest(ContractTests.Context(2))), TestContext.Current.CancellationToken);
        await Within(entered.Task);
        pair.Server.Dispose();
        Assert.Equal(DevToolsErrors.ConnectionClosed, (await Assert.ThrowsAsync<DevToolsException>(() => Within(next))).Code);
    }

    [Fact]
    public async Task Noncooperative_cancelled_handlers_keep_their_bounded_slots()
    {
        using var pair = await Pair.Create();
        var entered = Completion<bool>();
        var release = Completion<bool>();
        var count = 0;
        pair.Server.RequestHandler = async (request, token) =>
        {
            if (Interlocked.Increment(ref count) == DevToolsProtocol.MaxConcurrentRequests) entered.TrySetResult(true);
            await release.Task;
            return DevToolsJson.Write(ContractTests.Snapshot());
        };
        using var cancellation = new CancellationTokenSource();
        var requests = Enumerable.Range(0, DevToolsProtocol.MaxConcurrentRequests).Select(_ => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), cancellation.Token)).ToArray();
        try
        {
            await Within(entered.Task);
            cancellation.Cancel();
            foreach (var request in requests) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Within(request));
            Assert.Equal(DevToolsErrors.Busy, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken))).Code);
            Assert.Equal(DevToolsProtocol.MaxConcurrentRequests, count);
        }
        finally { release.TrySetResult(true); }
    }

    [Fact]
    public async Task Major_mismatch_returns_an_error_and_closes_the_connection()
    {
        using var pair = await Pair.Create(initialize: false);
        var raw = new JsonRpcConnection(pair.ClientStream, pair.ClientStream);
        await raw.WriteAsync(new() { ["id"] = "hello", ["method"] = "initialize", ["params"] = DevToolsJson.Write(new InitializeRequest(new(2, 0), "fixture", Capabilities)) }, TestContext.Current.CancellationToken);
        var response = await Within(raw.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(DevToolsErrors.VersionMismatch, response!["error"]!["code"]!.GetValue<int>());
        await pair.Server.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Outbound_request_capacity_is_bounded_and_disconnect_finishes_every_wait()
    {
        using var pair = await Pair.Create(initialize: false, startServer: false);
        var raw = new JsonRpcConnection(pair.ServerStream, pair.ServerStream);
        var initialize = pair.Client.InitializeAsync(TestContext.Current.CancellationToken);
        var request = await Within(raw.ReadAsync(TestContext.Current.CancellationToken));
        await raw.WriteAsync(new() { ["id"] = request!["id"]!.DeepClone(), ["result"] = DevToolsJson.Write(new InitializeResult(new(1, 0), "raw", Capabilities, ContractTests.State())) }, TestContext.Current.CancellationToken);
        await Within(initialize);
        var requests = Enumerable.Range(0, DevToolsProtocol.MaxPendingRequests).Select(_ => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken)).ToArray();
        foreach (var pending in requests) Assert.NotNull(await Within(raw.ReadAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(DevToolsErrors.Busy, (await Assert.ThrowsAsync<DevToolsException>(() => pair.Client.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, Context(), TestContext.Current.CancellationToken))).Code);
        pair.Server.Dispose();
        foreach (var pending in requests)
            Assert.Equal(DevToolsErrors.ConnectionClosed, (await Assert.ThrowsAsync<DevToolsException>(() => Within(pending))).Code);
    }

    [Fact]
    public async Task An_initialization_error_is_preserved_when_the_server_immediately_disconnects()
    {
        using var pair = await Pair.Create(initialize: false, startServer: false);
        var raw = new JsonRpcConnection(pair.ServerStream, pair.ServerStream);
        var initialize = pair.Client.InitializeAsync(TestContext.Current.CancellationToken);
        var request = await Within(raw.ReadAsync(TestContext.Current.CancellationToken));
        await raw.WriteAsync(new() { ["id"] = request!["id"]!.DeepClone(), ["error"] = new JsonObject { ["code"] = DevToolsErrors.VersionMismatch, ["message"] = "Incompatible version." } }, TestContext.Current.CancellationToken);
        pair.Server.Dispose();
        Assert.Equal(DevToolsErrors.VersionMismatch, (await Assert.ThrowsAsync<DevToolsException>(() => Within(initialize))).Code);
        await pair.Client.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Old_selection_events_are_not_delivered_after_a_session_transition()
    {
        using var pair = await Pair.Create();
        var ready = Completion<bool>();
        var selected = Completion<bool>();
        var count = 0;
        pair.Client.NotificationHandler = (method, parameters) =>
        {
            if (method == DevToolsProtocol.SessionChanged) ready.TrySetResult(true);
            else { Interlocked.Increment(ref count); selected.TrySetResult(true); }
        };
        pair.Server.SetTargetState(ContractTests.State(2));
        await pair.Server.NotifyAsync(DevToolsProtocol.SessionChanged, DevToolsJson.Write(ContractTests.State(2)), TestContext.Current.CancellationToken);
        await Within(ready.Task);
        var raw = new JsonRpcConnection(pair.ServerStream, pair.ServerStream);
        await raw.WriteAsync(new() { ["method"] = DevToolsProtocol.SelectionChanged, ["params"] = DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(), "element", "old")) }, TestContext.Current.CancellationToken);
        await raw.WriteAsync(new() { ["method"] = DevToolsProtocol.SelectionChanged, ["params"] = DevToolsJson.Write(new SelectionResult(ContractTests.Stamp(2), "element", "new")) }, TestContext.Current.CancellationToken);
        await Within(selected.Task);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("{\"id\":\"x\",\"result\":null}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":5,\"result\":null}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"x\",\"result\":null,\"error\":{\"code\":1,\"message\":\"bad\"}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"x\",\"error\":null}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"tree/get\",\"params\":[]}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"jsonrpc\":\"2.0\",\"id\":\"x\",\"result\":null}")]
    public async Task Malformed_envelopes_close_the_peer(string json)
    {
        using var pair = await Pair.Create(initialize: false);
        var bytes = Encoding.UTF8.GetBytes(json);
        await pair.ClientStream.WriteAsync(Encoding.ASCII.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n"), TestContext.Current.CancellationToken);
        await pair.ClientStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
        await pair.ClientStream.FlushAsync(TestContext.Current.CancellationToken);
        await pair.Server.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Requests_before_initialization_and_expired_requests_do_not_reach_handlers()
    {
        using var pair = await Pair.Create(initialize: false);
        var raw = new JsonRpcConnection(pair.ClientStream, pair.ClientStream);
        var parameters = Context();
        parameters["deadlineUtc"] = DateTimeOffset.UtcNow.AddSeconds(5).ToString("O");
        await raw.WriteAsync(new() { ["id"] = "first", ["method"] = DevToolsProtocol.GetTree, ["params"] = parameters }, TestContext.Current.CancellationToken);
        Assert.Equal(DevToolsErrors.NotInitialized, (await Within(raw.ReadAsync(TestContext.Current.CancellationToken)))!["error"]!["code"]!.GetValue<int>());
        await raw.WriteAsync(new() { ["id"] = "hello", ["method"] = "initialize", ["params"] = DevToolsJson.Write(new InitializeRequest(new(1, 99), "fixture", [DevToolsCapabilities.Tree, "future.capability"])) }, TestContext.Current.CancellationToken);
        var hello = DevToolsJson.Read<InitializeResult>((await Within(raw.ReadAsync(TestContext.Current.CancellationToken)))!["result"]);
        Assert.Equal(DevToolsProtocol.Minor, hello.Version.Minor);
        Assert.Equal([DevToolsCapabilities.Tree], hello.Capabilities);
        parameters["deadlineUtc"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("O");
        await raw.WriteAsync(new() { ["id"] = "expired", ["method"] = DevToolsProtocol.GetTree, ["params"] = parameters.DeepClone() }, TestContext.Current.CancellationToken);
        Assert.Equal(DevToolsErrors.Cancelled, (await Within(raw.ReadAsync(TestContext.Current.CancellationToken)))!["error"]!["code"]!.GetValue<int>());
    }

    private sealed class Pair(NamedPipeServerStream serverStream, NamedPipeClientStream clientStream, DevToolsPeer server, DevToolsPeer client) : IDisposable
    {
        public DevToolsPeer Server { get; } = server;
        public DevToolsPeer Client { get; } = client;
        public NamedPipeClientStream ClientStream { get; } = clientStream;
        public NamedPipeServerStream ServerStream { get; } = serverStream;
        public static async Task<Pair> Create(bool initialize = true, string[]? capabilities = null, bool startServer = true, Func<Stream, Stream>? clientOutput = null)
        {
            var name = "nazor-protocol-test-" + Guid.NewGuid().ToString("N");
            var serverStream = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var clientStream = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            var accept = serverStream.WaitForConnectionAsync(TestContext.Current.CancellationToken);
            await clientStream.ConnectAsync(5000, TestContext.Current.CancellationToken);
            await accept;
            var server = new DevToolsPeer(serverStream, serverStream, "agent fixture", capabilities ?? Capabilities, ContractTests.State());
            var client = new DevToolsPeer(clientStream, clientOutput?.Invoke(clientStream) ?? clientStream, "tools fixture", Capabilities);
            var pair = new Pair(serverStream, clientStream, server, client);
            try { if (startServer) server.Start(); if (initialize) await Within(client.InitializeAsync(TestContext.Current.CancellationToken)); return pair; }
            catch { pair.Dispose(); throw; }
        }
        public void Dispose() { Client.Dispose(); Server.Dispose(); ServerStream.Dispose(); ClientStream.Dispose(); }
    }

    // Holds outgoing frames, as a busy pipe would, so a request can be cancelled before its frame is written.
    private sealed class GatedStream(Stream inner) : Stream
    {
        private TaskCompletionSource open = Opened();
        private readonly TaskCompletionSource<bool> waiting = Completion<bool>();
        public Task<bool> Waiting => waiting.Task;
        public void Hold() => open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => open.TrySetResult();
        private static TaskCompletionSource Opened() { var source = new TaskCompletionSource(); source.SetResult(); return source; }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!open.Task.IsCompleted) waiting.TrySetResult(true);
            await open.Task.WaitAsync(cancellationToken);
            await inner.WriteAsync(buffer, cancellationToken);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override void Flush() => inner.Flush();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
