using Nazor.DevTools.Fixtures;
using Nazor.Testing;

namespace Nazor.Diagnostics.Tests;

public sealed class TreeCaptureTests : IDisposable
{
    private readonly DiagnosticsHarness harness = new();
    public void Dispose() => harness.Dispose();

    [Fact]
    public void Logical_instances_outputs_projection_hidden_nodes_and_root_identity_are_detached()
    {
        harness.Host.Render<TreePage>();
        harness.Host.Render<Empty>();
        harness.Scene.Update();
        var data = harness.Session.CaptureTree();
        Assert.Equal(2, data.ComponentRoots.Count);
        var page = Assert.Single(data.Components, c => c.TypeName == typeof(TreePage).FullName);
        var wrapper = Assert.Single(data.Components, c => c.TypeName == typeof(PassThrough).FullName);
        var left = Assert.Single(data.Components, c => c.Key == "left");
        var right = Assert.Single(data.Components, c => c.Key == "right");
        var projected = Assert.Single(data.Components, c => c.Key == "projected");
        var container = Assert.Single(data.Components, c => c.IsFramework);
        Assert.NotEqual(left.Id, right.Id);
        Assert.Equal(page.Id, wrapper.ParentId);
        Assert.Equal(wrapper.Id, left.ParentId);
        Assert.Equal(container.Id, projected.ParentId);
        Assert.Equal(page.Id, projected.DeclaringComponentId);
        Assert.Equal(2, left.OutputNodes.Count);
        Assert.All(left.OutputNodes, id => { Assert.Contains(id, wrapper.OutputNodes); Assert.Contains(id, page.OutputNodes); });
        Assert.Contains(data.Components, c => c.TypeName == typeof(Empty).FullName && c.OutputNodes.Count == 0);
        var element = Assert.Single(data.Nodes, n => n.Id == projected.OutputNodes[0]);
        Assert.Equal(projected.Id, element.ProducingComponentId);
        Assert.Equal(page.Id, element.DeclaringComponentId);
        Assert.Equal(page.Id, element.RootId);
        Assert.All(data.Nodes.Where(n => n.Name == "Hidden"), n => Assert.False(n.IsRendered));
        var detail = harness.Session.Get(element.Id)!;
        Assert.Equal((projected.Id, page.Id), (detail.ProducingComponentId, detail.DeclaringComponentId));
        Assert.Null(harness.Session.Get(projected.Id));
        Assert.Throws<NotSupportedException>(() => ((IList<int>)left.OutputNodes).Clear());
        harness.Session.Dispose();
        Assert.Equal("projected", data.Nodes.Single(n => n.Id == element.Children[0]).Text);
    }

    [Fact]
    public void Identities_survive_reorder_and_recreation_retires_every_removed_instance()
    {
        var root = harness.Host.Render<TreePage>();
        var page = (TreePage)root.Component;
        var before = harness.Session.CaptureTree();
        var left = before.Components.Single(n => n.Key == "left");
        var projected = before.Components.Single(n => n.Key == "projected");
        var removedComponents = before.Components.Where(n => n.Id == projected.Id || n.ParentId == projected.ParentId && n.TypeName == typeof(Empty).FullName).Select(n => n.Id).ToArray();
        var changes = new List<TreeChange>();
        harness.Session.TreeChanged += changes.Add;
        page.Reverse = true;
        page.Version++;
        page.Refresh();
        harness.Host.RunPending();
        var reordered = harness.Session.CaptureTree();
        Assert.Equal(left.Id, reordered.Components.Single(n => n.Key == "left").Id);
        Assert.Equal(left.OutputNodes, reordered.Components.Single(n => n.Key == "left").OutputNodes);
        Assert.Contains(reordered.Nodes, n => n.Text == "left-1");
        page.Show = false;
        page.Refresh();
        harness.Host.RunPending();
        var removed = harness.Session.CaptureTree();
        Assert.All(removedComponents.Concat(projected.OutputNodes), id =>
        {
            Assert.Null(harness.Session.NodeOf(id));
            Assert.Contains(changes, change => change.Removed.Contains(id));
        });
        Assert.All(changes, change => Assert.True(change.ComponentsChanged));
        Assert.True(removed.Revision > before.Revision);
        page.Show = true;
        page.Refresh();
        harness.Host.RunPending();
        var recreated = harness.Session.CaptureTree().Components.Single(n => n.Key == "projected");
        Assert.NotEqual(projected.Id, recreated.Id);
        Assert.DoesNotContain(recreated.OutputNodes, id => projected.OutputNodes.Contains(id));
    }

    [Fact]
    public void Capture_and_legacy_details_never_evaluate_component_getters_or_unknown_formatters()
    {
        harness.Host.Render<TreePage>();
        harness.Scene.Update();
        PoisonValue.Calls = TreePage.GetterCalls = 0;
        var snapshot = harness.Session.CaptureTree();
        Assert.Contains(snapshot.Components, c => c.Key == "<PoisonValue>");
        var payload = snapshot.Nodes.Single(n => n.Name == "payload");
        Assert.Equal("<PoisonValue>", harness.Session.Get(payload.Id)!.Attributes["payload"]);
        Assert.Equal(0, PoisonValue.Calls);
        Assert.Equal(0, TreePage.GetterCalls);
    }

    [Fact]
    public void Capture_marks_uncalculated_visibility_unavailable_and_enforces_node_string_and_depth_budgets()
    {
        var root = harness.Host.Render<TreePage>();
        Assert.All(harness.Session.CaptureTree().Nodes, n => Assert.Null(n.IsRendered));
        harness.Host.Renderer.RemoveRootComponent(root);
        var maximum = harness.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = DevToolsSession.MaxCaptureNodes - 1 }));
        Assert.Equal(DevToolsSession.MaxCaptureNodes - 1, harness.Session.CaptureTree().Nodes.Count);
        harness.Host.Renderer.RemoveRootComponent(maximum);
        var oversized = harness.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = DevToolsSession.MaxCaptureNodes }));
        Assert.Throws<TreeCaptureLimitException>(() => harness.Session.CaptureTree());
        harness.Host.Renderer.RemoveRootComponent(oversized);
        var longText = harness.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Text"] = new string('x', DevToolsSession.MaxCaptureStringLength + 1) }));
        Assert.Throws<TreeCaptureLimitException>(() => harness.Session.CaptureTree());
        harness.Host.Renderer.RemoveRootComponent(longText);
        harness.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Depth"] = DevToolsSession.MaxCaptureDepth + 1 }));
        Assert.Throws<TreeCaptureLimitException>(() => harness.Session.CaptureTree());
    }

    [Fact]
    public void Capture_requires_an_idle_owning_dispatcher_and_a_live_attachment()
    {
        harness.Host.Render<TreePage>();
        var exceptions = new List<Exception?>();
        var thread = new Thread(() => exceptions.Add(Record.Exception(() => harness.Session.CaptureTree())));
        thread.Start();
        thread.Join();
        Assert.IsType<InvalidOperationException>(Assert.Single(exceptions));
        void DuringBatch(object? sender, Nazor.Rendering.RenderBatch batch) => Assert.Throws<InvalidOperationException>(() => harness.Session.CaptureTree());
        harness.Host.Renderer.BatchCompleted += DuringBatch;
        harness.Host.Render<Empty>();
        harness.Host.Renderer.BatchCompleted -= DuringBatch;
        harness.Session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => harness.Session.CaptureTree());
    }
}
