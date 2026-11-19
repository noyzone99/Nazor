using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Agent.Tests;

public sealed class LocalAgentTests
{
    [Fact]
    public async Task A_separate_process_supplies_trees_deltas_resync_reconnect_and_detaches_cleanly()
    {
        using var fixture = await Fixture.StartAsync();
        var descriptor = Assert.Single(LocalDevTools.Discover(fixture.Directory));
        Assert.Equal("DevToolsAgent", descriptor.ApplicationName);
        Assert.Throws<InvalidDataException>(() => LocalDevTools.Save(fixture.Directory, descriptor with { ApplicationName = "bad\nname" }));
        Assert.Throws<InvalidDataException>(() => LocalDevTools.Save(fixture.Directory, descriptor with { ApplicationName = new('x', 257) }));
        using var peer = await LocalDevTools.ConnectAsync(descriptor, "Agent test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        var changes = new ConcurrentQueue<TreeDelta>();
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        peer.NotificationHandler = (method, payload) => { if (method == DevToolsProtocol.TreeChanged) { changes.Enqueue(DevToolsJson.Read<TreeDelta>(payload)); changed.TrySetResult(); } };
        var initial = await TreeAsync(peer, TestContext.Current.CancellationToken);
        Assert.Equal(descriptor.Identity.SessionId, initial.Capture.SessionId);
        Assert.Contains(initial.Elements, e => e.Text == "left-0");
        await fixture.CommandAsync("change", "CHANGED");
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(changes.TryDequeue(out var delta));
        Assert.Equal(initial.Revision, delta.BaseRevision);
        Assert.Contains(delta.Elements, e => e.Text == "left-1");
        Assert.NotEmpty(delta.RemovedComponents);
        var resync = await TreeAsync(peer, TestContext.Current.CancellationToken);
        Assert.True(resync.Revision > initial.Revision);
        Assert.Contains(resync.Elements, e => e.Text == "left-1");
        var generation = peer.RemoteTarget!.Generation;
        await fixture.CommandAsync("inspect", "INSPECTING");
        peer.Dispose();
        using var reconnected = await LocalDevTools.ConnectAsync(descriptor, "Agent test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.True(reconnected.RemoteTarget!.Generation > generation);
        Assert.Equal(1, (await TreeAsync(reconnected, TestContext.Current.CancellationToken)).Revision);
        await fixture.CommandAsync("inspection", "CLEAR");
        await fixture.CommandAsync("detach", "DETACHED");
        await reconnected.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Empty(LocalDevTools.Discover(fixture.Directory));
        Assert.Contains(initial.Elements, e => e.Text == "left-0");
    }

    [Fact]
    public async Task Replacement_exit_and_pid_reuse_cannot_retarget_a_stale_descriptor()
    {
        using var fixture = await Fixture.StartAsync();
        var old = fixture.Descriptor!;
        using var peer = await LocalDevTools.ConnectAsync(old, "Lifecycle test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        await fixture.SendAsync("replace");
        var replacement = JsonSerializer.Deserialize<LocalDevToolsDescriptor>(await fixture.LineAsync())!;
        await peer.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.NotEqual(old.Identity.SessionId, replacement.Identity.SessionId);
        Assert.NotEqual(old.Identity.WindowId, replacement.Identity.WindowId);
        Assert.Equal(old.Identity.ProcessInstanceId, replacement.Identity.ProcessInstanceId);
        Assert.Equal(replacement, Assert.Single(LocalDevTools.Discover(fixture.Directory)));
        using var fresh = await LocalDevTools.ConnectAsync(replacement, "Lifecycle test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.Equal(1, (await TreeAsync(fresh, TestContext.Current.CancellationToken)).Revision);
        var reusedPid = replacement with { ProcessStartToken = "previous-process" };
        Assert.False(LocalDevTools.IsAlive(reusedPid));
        Assert.Equal(DevToolsErrors.StaleSession, (await Assert.ThrowsAsync<DevToolsException>(() => LocalDevTools.ConnectAsync(reusedPid, "Stale test", [], TestContext.Current.CancellationToken))).Code);
        await fixture.SendAsync("exit");
        await fixture.Process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await fresh.Closed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Empty(LocalDevTools.Discover(fixture.Directory));
    }

    [Fact]
    public async Task Discovery_identity_is_checked_again_after_initialization()
    {
        using var fixture = await Fixture.StartAsync();
        var forged = fixture.Descriptor! with { Identity = fixture.Descriptor!.Identity with { WindowId = "another-window" } };
        Assert.Equal(DevToolsErrors.StaleSession, (await Assert.ThrowsAsync<DevToolsException>(() =>
            LocalDevTools.ConnectAsync(forged, "Identity test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken))).Code);
        using var peer = await LocalDevTools.ConnectAsync(fixture.Descriptor!, "Identity test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.NotEmpty((await TreeAsync(peer, TestContext.Current.CancellationToken)).Components);
    }

    [Fact]
    public async Task Disabled_startup_creates_no_endpoint_or_discovery_storage()
    {
        using var fixture = await Fixture.StartAsync(disabled: true);
        Assert.False(System.IO.Directory.Exists(fixture.Directory));
        Assert.Empty(LocalDevTools.Discover(fixture.Directory));
        await fixture.CommandAsync("ping", "PONG");
    }

    [Fact]
    public async Task Fragmented_messages_work_and_malformed_or_uninitialized_connections_release_the_endpoint()
    {
        using var fixture = await Fixture.StartAsync();
        using (var stream = await RawConnectAsync(fixture.Descriptor!))
        {
            await InitializeRawAsync(stream, fragmented: true);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("Content-Length: 2\r\nUnexpected: yes\r\n\r\n{}"), TestContext.Current.CancellationToken);
            Assert.Equal(0, await ReadEndAsync(stream));
        }
        await Task.Delay(100, TestContext.Current.CancellationToken);
        using (var idle = await RawConnectAsync(fixture.Descriptor!))
        {
            var elapsed = Stopwatch.StartNew();
            Assert.Equal(0, await ReadEndAsync(idle));
            Assert.True(elapsed.Elapsed >= TimeSpan.FromSeconds(2));
        }
        using var peer = await LocalDevTools.ConnectAsync(fixture.Descriptor!, "Recovered test", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.NotEmpty((await TreeAsync(peer, TestContext.Current.CancellationToken)).Components);
    }

    [Fact]
    public async Task A_slow_reader_does_not_block_the_ui_and_reconnect_requires_a_full_snapshot()
    {
        using var fixture = await Fixture.StartAsync();
        long generation;
        using (var stream = await RawConnectAsync(fixture.Descriptor!))
        {
            var initialized = await InitializeRawAsync(stream);
            var target = DevToolsJson.Read<InitializeResult>(initialized["result"]).Target!;
            generation = target.Generation;
            await WriteRawAsync(stream, new JsonObject
            {
                ["jsonrpc"] = "2.0", ["id"] = "tree", ["method"] = "tree/get",
                ["params"] = new JsonObject { ["context"] = DevToolsJson.Write(new RequestContext(target.Identity.SessionId, target.Generation, null)),
                    ["deadlineUtc"] = DateTimeOffset.UtcNow.AddSeconds(10).ToString("O", CultureInfo.InvariantCulture) }
            });
            Assert.True(await ReadHeaderAsync(stream) > 100000);
            await fixture.CommandAsync("flood", "FLOODED");
            await fixture.CommandAsync("ping", "PONG");
        }
        using var peer = await LocalDevTools.ConnectAsync(fixture.Descriptor!, "After slow reader", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.True(peer.RemoteTarget!.Generation > generation);
        var current = await TreeAsync(peer, TestContext.Current.CancellationToken);
        Assert.Equal(1, current.Revision);
        Assert.Contains(current.Elements, e => e.Text == "left-500");
    }

    [Fact]
    public async Task A_live_connection_to_a_blocked_dispatcher_is_not_a_fresh_capture_and_cancellation_skips_queued_work()
    {
        using var fixture = await Fixture.StartAsync();
        using var peer = await LocalDevTools.ConnectAsync(fixture.Descriptor!, "Blocked UI", [DevToolsCapabilities.Tree], TestContext.Current.CancellationToken);
        Assert.Equal(1, (await TreeAsync(peer, TestContext.Current.CancellationToken)).Revision);
        await fixture.CommandAsync("freeze", "FROZEN");
        await Task.Delay(50, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TreeAsync(peer, cancellation.Token));
        Assert.True(LocalDevTools.IsAlive(fixture.Descriptor!));
        Assert.False(peer.Closed.IsCompleted);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(DevToolsErrors.Busy, (await Assert.ThrowsAsync<DevToolsException>(() => TreeAsync(peer, TestContext.Current.CancellationToken))).Code);
        await fixture.SendAsync("resume");
        await fixture.CommandAsync("ping", "PONG");
        Assert.Equal(2, (await TreeAsync(peer, TestContext.Current.CancellationToken)).Revision);
    }

    [Fact]
    public async Task Discovery_ignores_malformed_stale_and_linked_records_and_uses_private_unix_modes()
    {
        using var fixture = await Fixture.StartAsync();
        File.WriteAllText(Path.Combine(fixture.Directory, "invalid.json"), "{");
        var stale = fixture.Descriptor! with { Identity = fixture.Descriptor!.Identity with { SessionId = Guid.NewGuid().ToString("N") }, ProcessStartToken = "previous-process" };
        stale = stale with { PipeName = LocalDevTools.PipeName(fixture.Directory, stale.Identity.SessionId) };
        LocalDevTools.Save(fixture.Directory, stale);
        Assert.Single(LocalDevTools.Discover(fixture.Directory));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(fixture.Directory));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(fixture.Descriptor!.PipeName));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(fixture.Directory, fixture.Descriptor.Identity.SessionId + ".json")));
            var linked = Path.Combine(fixture.Directory, "linked.json");
            File.CreateSymbolicLink(linked, Path.Combine(fixture.Directory, fixture.Descriptor.Identity.SessionId + ".json"));
            Assert.Single(LocalDevTools.Discover(fixture.Directory));
            File.SetUnixFileMode(fixture.Directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.OtherRead);
            Assert.Throws<UnauthorizedAccessException>(() => LocalDevTools.Discover(fixture.Directory));
            File.SetUnixFileMode(fixture.Directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        fixture.Process.Kill(true);
        await fixture.Process.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.Empty(LocalDevTools.Discover(fixture.Directory));
    }

    private static Task<TreeSnapshot> TreeAsync(DevToolsPeer peer, CancellationToken token)
    {
        var state = peer.RemoteTarget!;
        return peer.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, DevToolsJson.Write(new ContextRequest(new(state.Identity.SessionId, state.Generation, state.StopId))), token);
    }

    private static async Task<NamedPipeClientStream> RawConnectAsync(LocalDevToolsDescriptor descriptor)
    {
        var stream = new NamedPipeClientStream(".", descriptor.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await stream.ConnectAsync(3000, TestContext.Current.CancellationToken);
        return stream;
    }

    private static async Task<JsonObject> InitializeRawAsync(Stream stream, bool fragmented = false)
    {
        await WriteRawAsync(stream, new JsonObject { ["jsonrpc"] = "2.0", ["id"] = "init", ["method"] = "initialize",
            ["params"] = DevToolsJson.Write(new InitializeRequest(new(DevToolsProtocol.Major, DevToolsProtocol.Minor), "Raw client", [DevToolsCapabilities.Tree])) }, fragmented);
        var bytes = new byte[await ReadHeaderAsync(stream)];
        await stream.ReadExactlyAsync(bytes, TestContext.Current.CancellationToken);
        return (JsonObject)JsonNode.Parse(bytes)!;
    }

    private static async Task WriteRawAsync(Stream stream, JsonObject message, bool fragmented = false)
    {
        var body = Encoding.UTF8.GetBytes(message.ToJsonString());
        var bytes = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n").Concat(body).ToArray();
        var size = fragmented ? 3 : bytes.Length;
        for (var i = 0; i < bytes.Length; i += size)
            await stream.WriteAsync(bytes.AsMemory(i, Math.Min(size, bytes.Length - i)), TestContext.Current.CancellationToken);
    }

    private static async Task<int> ReadEndAsync(Stream stream)
    {
        try { return await stream.ReadAsync(new byte[1], TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken); }
        catch (IOException) { return 0; }
    }

    private static async Task<int> ReadHeaderAsync(Stream stream)
    {
        var text = new StringBuilder();
        var one = new byte[1];
        while (text.Length < 8192)
        {
            if (await stream.ReadAsync(one, TestContext.Current.CancellationToken) == 0) throw new EndOfStreamException();
            text.Append((char)one[0]);
            if (text.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) return int.Parse(text.ToString().Split(':')[1].Trim(), CultureInfo.InvariantCulture);
        }
        throw new InvalidDataException();
    }

    private sealed class Fixture : IDisposable
    {
        internal Process Process { get; }
        internal string Directory { get; }
        internal LocalDevToolsDescriptor? Descriptor { get; private set; }
        private readonly Task<string> errors;
        private Fixture(Process process, string directory) { Process = process; Directory = directory; errors = process.StandardError.ReadToEndAsync(); }
        internal static async Task<Fixture> StartAsync(bool disabled = false)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(root.FullName, "Nazor.slnx"))) root = root.Parent ?? throw new InvalidOperationException("Repository root not found.");
            var directory = OperatingSystem.IsWindows() ? Path.Combine(root.FullName, "artifacts", "dt06-tests", Guid.NewGuid().ToString("N"))
                : Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
            var framework = Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_FIXTURE_FRAMEWORK") ?? "net10.0";
            var dll = Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_FIXTURE") ?? Path.Combine(root.FullName, "Tests", "Fixtures", "DevToolsAgent", "bin",
                typeof(LocalAgentTests).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false).Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration,
                framework, "DevToolsAgent.dll");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(dll); start.ArgumentList.Add(directory);
            if (disabled) start.ArgumentList.Add("--disabled");
            var fixture = new Fixture(Process.Start(start)!, directory);
            try
            {
                var line = await fixture.LineAsync();
                if (disabled) Assert.Equal("DISABLED", line);
                else fixture.Descriptor = JsonSerializer.Deserialize<LocalDevToolsDescriptor>(line)!;
                return fixture;
            }
            catch { fixture.Dispose(); throw; }
        }
        internal async Task<string> LineAsync() => await Process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Fixture exited: " + await errors);
        internal async Task SendAsync(string command) { await Process.StandardInput.WriteLineAsync(command); await Process.StandardInput.FlushAsync(TestContext.Current.CancellationToken); }
        internal async Task CommandAsync(string command, string expected) { await SendAsync(command); Assert.Equal(expected, await LineAsync()); }
        public void Dispose() { if (!Process.HasExited) Process.Kill(true); Process.WaitForExit(); Process.Dispose(); }
    }
}
