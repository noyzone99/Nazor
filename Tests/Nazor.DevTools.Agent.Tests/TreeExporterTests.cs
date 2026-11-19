using Nazor.DevTools.Fixtures;
using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.DevTools.Agent.Tests;

public sealed class TreeExporterTests
{
    [Fact]
    public async Task Exported_data_preserves_both_trees_and_serializes_after_the_target_is_gone()
    {
        TreeCapture capture;
        using (var fixture = new Fixture())
        {
            fixture.Host.Render<TreePage>();
            capture = fixture.Exporter.Capture(fixture.Target());
            var page = capture.Snapshot.Components.Single(c => c.TypeName == typeof(TreePage).FullName);
            var projected = capture.Snapshot.Components.Single(c => c.Key == "projected");
            Assert.Equal(page.Id, projected.RootId);
            Assert.Equal(page.Id, projected.DeclaringComponentId);
            Assert.All(projected.OutputElements, id => Assert.Equal(projected.Id, capture.Snapshot.Elements.Single(e => e.Id == id).ProducingComponentId));
            Assert.Null(capture.Delta);
        }
        var payload = await TreeSnapshotExporter.SerializeAsync(capture, cancellationToken: TestContext.Current.CancellationToken);
        var result = DevToolsJson.Read<TreeSnapshot>(payload);
        Assert.Contains(result.Elements, e => e.Text == "projected");
        Assert.Equal(capture.Target.SessionId, result.Capture.SessionId);
    }

    [Fact]
    public Task Deltas_report_removals_updates_and_reorder_without_aliasing_the_exposed_arrays()
    {
        using var fixture = new Fixture();
        var page = (TreePage)fixture.Host.Render<TreePage>().Component;
        var first = fixture.Exporter.Capture(fixture.Target());
        var projected = first.Snapshot.Components.Single(c => c.Key == "projected");
        var components = first.Snapshot.Components.ToDictionary(c => c.Id);
        var elements = first.Snapshot.Elements.ToDictionary(e => e.Id);
        var oldIds = first.Snapshot.Components.Select(c => c.Id).ToHashSet();
        first.Snapshot.Components[0].Children[0] = "tampered";
        first.Snapshot.ComponentRoots[0] = "tampered";
        page.Show = false;
        page.Reverse = true;
        page.Version++;
        page.Refresh();
        fixture.Host.RunPending();
        var second = fixture.Exporter.Capture(fixture.Target());
        var delta = second.Delta!;
        Assert.Equal((1L, 2L), (delta.BaseRevision, delta.Revision));
        Assert.Contains(projected.Id, delta.RemovedComponents);
        Assert.All(projected.OutputElements, id => Assert.Contains(id, delta.RemovedElements));
        Assert.Contains(delta.Elements, e => e.Text == "left-1");
        Assert.DoesNotContain(second.Snapshot.ComponentRoots, id => id == "tampered");
        Assert.All(second.Snapshot.Components, c => Assert.Contains(c.Id, oldIds));
        foreach (var id in delta.RemovedComponents) components.Remove(id);
        foreach (var id in delta.RemovedElements) elements.Remove(id);
        foreach (var component in delta.Components) components[component.Id] = component;
        foreach (var element in delta.Elements) elements[element.Id] = element;
        var applied = new TreeSnapshot(delta.Capture, delta.Revision, delta.ComponentRoots, delta.ElementRoots, components.Values.ToArray(), elements.Values.ToArray());
        DevToolsJson.Write(applied);
        Assert.Equal(second.Snapshot.Components.Select(c => c.Id).Order(), applied.Components.Select(c => c.Id).Order());
        Assert.Equal(second.Snapshot.Elements.Select(e => e.Id).Order(), applied.Elements.Select(e => e.Id).Order());
        var unchanged = fixture.Exporter.Capture(fixture.Target()).Delta!;
        Assert.Empty(unchanged.Components);
        Assert.Empty(unchanged.Elements);
        page.Show = true;
        page.Refresh();
        fixture.Host.RunPending();
        var recreated = fixture.Exporter.Capture(fixture.Target());
        Assert.NotEqual(projected.Id, recreated.Snapshot.Components.Single(c => c.Key == "projected").Id);
        return TreeSnapshotExporter.SerializeAsync(recreated, changesOnly: true, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Metadata_uses_supplied_parameters_and_retained_attributes_without_user_getters_or_formatters()
    {
        using var fixture = new Fixture();
        PoisonParameter.GetterCalls = PoisonValue.Calls = 0;
        fixture.Host.Render<PoisonParameter>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = PoisonValue.Instance }));
        var page = fixture.Host.Render<TreePage>();
        var captured = fixture.Exporter.Capture(fixture.Target()).Snapshot;
        Assert.Equal("<PoisonValue>", captured.Components.Single(c => c.TypeName.EndsWith("PoisonParameter", StringComparison.Ordinal)).Parameters!.Single().Value);
        Assert.Equal(0, PoisonParameter.GetterCalls); Assert.Equal(0, PoisonValue.Calls);
        var piece = captured.Elements.First(e => e.Name == "Piece");
        Assert.Equal("left-0", piece.ElementId); Assert.Equal(["piece"], piece.Classes!);
        var payload = captured.Elements.Single(e => e.Name == "payload");
        Assert.Contains(payload.Attributes!, a => a.Name == "payload" && a.Value == "<PoisonValue>");
        ((TreePage)page.Component).Version++; ((TreePage)page.Component).Refresh(); fixture.Host.RunPending();
        var delta = fixture.Exporter.Capture(fixture.Target()).Delta!;
        Assert.Contains(delta.Components, c => c.Parameters?.Any(p => p.Name == "Text" && p.Value == "left-1") == true);
        Assert.Contains(delta.Elements, e => e.ElementId == "left-1");
    }

    [Fact]
    public void Paused_stale_exited_and_failed_captures_do_not_advance_the_baseline()
    {
        using var fixture = new Fixture();
        fixture.Host.Render<TreePage>();
        fixture.Exporter.Capture(fixture.Target());
        Assert.Equal(DevToolsErrors.TargetPaused, Assert.Throws<DevToolsException>(() => fixture.Exporter.Capture(fixture.Target() with { State = "paused", StopId = "stop" })).Code);
        Assert.Equal(DevToolsErrors.StaleSession, Assert.Throws<DevToolsException>(() => fixture.Exporter.Capture(fixture.Target() with { Identity = fixture.Target().Identity with { WindowId = "other" } })).Code);
        Assert.Equal(DevToolsErrors.Unavailable, Assert.Throws<DevToolsException>(() => fixture.Exporter.Capture(fixture.Target() with { State = "exited" })).Code);
        var oversized = fixture.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = DevToolsSession.MaxCaptureNodes }));
        Assert.Equal(DevToolsErrors.Unavailable, Assert.Throws<DevToolsException>(() => fixture.Exporter.Capture(fixture.Target())).Code);
        fixture.Host.Renderer.RemoveRootComponent(oversized);
        Assert.Equal(2, fixture.Exporter.Capture(fixture.Target()).Snapshot.Revision);
        var nextGeneration = fixture.Exporter.Capture(fixture.Target() with { Generation = 2 });
        Assert.Null(nextGeneration.Delta);
        Assert.Equal(1, nextGeneration.Snapshot.Revision);
        Assert.Equal(DevToolsErrors.StaleSession, Assert.Throws<DevToolsException>(() => fixture.Exporter.Capture(fixture.Target())).Code);
    }

    [Fact]
    public async Task Serialization_checks_cancellation_resynchronization_and_wire_size_off_the_dispatcher()
    {
        TreeCapture capture;
        using (var fixture = new Fixture()) { fixture.Host.Render<TreePage>(); capture = fixture.Exporter.Capture(fixture.Target()); }
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TreeSnapshotExporter.SerializeAsync(capture, cancellationToken: cancellation.Token));
        Assert.Equal(DevToolsErrors.ResyncRequired, (await Assert.ThrowsAsync<DevToolsException>(() => TreeSnapshotExporter.SerializeAsync(capture, true, TestContext.Current.CancellationToken))).Code);
        var oversized = capture with { Snapshot = capture.Snapshot with { Elements = [capture.Snapshot.Elements[0] with { Text = new string('x', DevToolsProtocol.MaxStringLength + 1) }] } };
        Assert.Equal(DevToolsErrors.Unavailable, (await Assert.ThrowsAsync<DevToolsException>(() => TreeSnapshotExporter.SerializeAsync(oversized, cancellationToken: TestContext.Current.CancellationToken))).Code);
    }

    private sealed class Fixture : IDisposable
    {
        public TestHost Host { get; } = new();
        private readonly SkiaRenderer graphics = new();
        private readonly Scene scene;
        private readonly InputManager input;
        private readonly DevToolsSession session;
        public TreeSnapshotExporter Exporter { get; }
        public Fixture()
        {
            scene = new(Host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 200) };
            input = new(Host.Renderer, scene);
            session = new(Host.Renderer, scene, input);
            Exporter = new(session);
        }
        public TargetState Target() => new(new(42, "process", "runtime", "app", "window", session.SessionId), 1, "running", null);
        public void Dispose() { session.Dispose(); input.Dispose(); scene.Dispose(); Host.Dispose(); graphics.Dispose(); }
    }
}
