using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class DebuggerTests
{
    [Fact]
    public async Task Cold_paused_attachment_uses_the_discovered_application_name_without_a_live_connection()
    {
        var store = new InspectionStore();
        using var client = new InspectorClient(store);
        var adapter = new ControlledDebugger();
        using var debugger = new InspectorDebugger(client, new(1, CapturedData.Target().Identity, DateTimeOffset.UtcNow.ToString("O"), "unused", "unused")
            { ApplicationName = "Paused App" }, adapter);
        debugger.Start();
        var end = DateTime.UtcNow.AddSeconds(5);
        while (store.Read().Debugger?.State != "paused" && DateTime.UtcNow < end) await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal("paused", store.Read().Debugger!.State);
        Assert.Equal("Nazor DevTools - Paused App", store.WindowTitle);
    }

    [Fact]
    public void Current_stop_fields_are_detached_and_retired_on_step_continue_or_window_replacement()
    {
        var store = new InspectionStore();
        store.Bind(CapturedData.Target()); store.Apply(CapturedData.Tree()); store.Select("component", "c");
        store.SetDebugger(new(42, "fixture", "paused", "stop-1", true, null));
        Assert.Equal("paused", store.Read().Target!.State); Assert.True(store.Read().IsRetained);
        var fields = new DebuggerFields(CapturedData.Stamp(stop: "stop-1"), "c", [new("Value", "int", "11", true)], "The render may be intermediate.");
        Assert.True(store.Apply(fields));
        fields.Fields[0] = new("Value", "int", "tampered", true);
        Assert.Equal("11", store.Read().DebuggerFields!.Fields[0].Value);
        store.SetDebugger(new(42, "fixture", "paused", "stop-2", true, null));
        Assert.False(store.Apply(fields));
        Assert.Equal("stop-1", store.Read().DebuggerFields!.Capture.StopId);
        store.SetDebugger(new(42, "fixture", "running", null, true, null));
        Assert.Equal("running", store.Read().Target!.State);
        Assert.False(store.Apply(fields));
        store.Bind(CapturedData.Target(session: "other")); Assert.Null(store.Read().DebuggerFields); Assert.Null(store.Read().Debugger);
    }

    [Fact]
    public async Task A_late_read_cannot_be_published_after_continue_and_live_calls_are_blocked_during_pause()
    {
        var store = new InspectionStore(); store.Bind(CapturedData.Target()); store.Apply(CapturedData.Tree()); store.Select("component", "c");
        using var client = new InspectorClient(store);
        var adapter = new ControlledDebugger();
        using var debugger = new InspectorDebugger(client, new(1, CapturedData.Target().Identity, DateTimeOffset.UtcNow.ToString("O"), "unused", "unused"), adapter);
        debugger.Start();
        await adapter.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("paused", store.Read().Target!.State);
        await client.CaptureDetailsAsync("e", token: TestContext.Current.CancellationToken); await client.RefreshFramesAsync(TestContext.Current.CancellationToken); await client.HighlightSelectionAsync(TestContext.Current.CancellationToken);
        Assert.Null(store.Read().DebuggerFields);
        adapter.State = new(42, "fixture", "running", null, true, null);
        adapter.Result.SetResult(new(CapturedData.Stamp(stop: "stop-1"), "c", [new("Value", "int", "11", true)], null));
        var end = DateTime.UtcNow.AddSeconds(5);
        while (store.Read().Debugger?.State != "running" && DateTime.UtcNow < end) await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal("running", store.Read().Debugger!.State);
        Assert.Null(store.Read().DebuggerFields);
    }

    private sealed class ControlledDebugger : IInspectorDebugger
    {
        internal DebuggerStatus State = new(42, "fixture", "paused", "stop-1", true, null);
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<DebuggerFields> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<DebuggerStatus> StateAsync(int processId, CancellationToken token) => Task.FromResult(State);
        public Task<TreeSnapshot> TreeAsync(int processId, string sessionId, string stopId, CancellationToken token) => Task.FromResult(CapturedData.Tree());
        public Task<DebuggerFields> FieldsAsync(DebuggerReadRequest request, CancellationToken token) { Entered.SetResult(); return Result.Task.WaitAsync(token); }
    }
}
