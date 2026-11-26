using System.Diagnostics;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class PagedInspectionTests
{
    [Fact]
    public async Task The_agent_pages_all_ten_thousand_siblings_and_keeps_unrelated_cursors()
    {
        using var target = await AgentProcess.StartAsync(wide: true);
        var token = TestContext.Current.CancellationToken;
        using var peer = await LocalDevTools.ConnectAsync(target.Descriptor, "Paged test", [DevToolsCapabilities.PagedTree, DevToolsCapabilities.Tree], token);
        var context = new RequestContext(peer.RemoteTarget!.Identity.SessionId, peer.RemoteTarget.Generation, null);
        var timings = new List<double>();
        var watch = Stopwatch.StartNew();
        var roots = await Browse(new(context, "element", "children", null));
        var rootTime = watch.Elapsed.TotalMilliseconds;
        Assert.NotEmpty(roots.Nodes);
        Assert.NotNull(roots.Cursor);
        var legacy = await Assert.ThrowsAsync<DevToolsException>(() => peer.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree,
            DevToolsJson.Write(new ContextRequest(context)), token));
        Assert.Equal(DevToolsErrors.Unavailable, legacy.Code);

        var search = new TreeBrowseRequest(context, "element", "search", null, Query: "wide-row-9999");
        TreeBrowsePage page;
        do { page = await Browse(search); search = search with { Cursor = page.Cursor }; } while (page.Nodes.Length == 0 && search.Cursor is not null);
        var last = Assert.Single(page.Nodes).Element!;
        var path = await Browse(new(context, "element", "path", last.Id));
        var parent = Assert.Single(path.Nodes, node => node.Element!.Name == "Wide").Element!;
        var children = new TreeBrowseRequest(context, "element", "children", parent.Id);
        page = await Browse(children);
        var time = Stopwatch.StartNew();
        var found = new List<string>();
        var packets = 0;
        await target.CommandAsync("change", "CHANGED");
        while (true)
        {
            found.AddRange(page.Nodes.Select(node => node.Element!.ElementId!));
            packets++;
            if (page.Cursor is null) break;
            page = await Browse(children with { Cursor = page.Cursor });
        }
        Assert.Equal(Enumerable.Range(0, 10000).Select(index => $"wide-row-{index}"), found);
        var sorted = timings.Order().ToArray();
        TestContext.Current.SendDiagnosticMessage($"First roots page {roots.Visited} visits in {rootTime:F1} ms ({roots.ReadMs:F2} ms on target); ten thousand siblings in {packets} pages and {time.Elapsed.TotalMilliseconds:F1} ms, target p95 {sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1]:F2} ms.");

        async Task<TreeBrowsePage> Browse(TreeBrowseRequest request)
        {
            var value = await peer.RequestAsync<TreeBrowsePage>(DevToolsProtocol.BrowseTree, DevToolsJson.Write(request), token);
            Assert.InRange(value.Visited, 1, 256);
            Assert.InRange(value.Nodes.Length, 0, 64);
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(DevToolsJson.Write(value).ToJsonString()) < DevToolsProtocol.MaxMessageBytes - 4096);
            timings.Add(value.ReadMs);
            return value;
        }
    }

    [Fact]
    public async Task Inspector_reveals_search_and_picker_results_and_recovers_a_reordered_branch()
    {
        using var target = await AgentProcess.StartAsync(wide: true);
        var token = TestContext.Current.CancellationToken;
        var store = new InspectionStore();
        using var client = new InspectorClient(store, target.Directory) { UsePagedTree = true };
        await client.ConnectAsync(target.Descriptor, token);
        Assert.True(store.Read().Paged is not null, store.Read().Message);
        Assert.DoesNotContain(store.Read().Tree!.Elements, node => node.ElementId == "wide-row-9999");

        var found = await client.SearchTreeAsync("element", "wide-row-9999", null, token);
        Assert.True(found is { Nodes.Length: 1 }, store.Read().Message ?? $"Search returned {found?.Nodes.Length ?? 0} nodes.");
        var last = found!.Nodes[0].Element!;
        Assert.Equal("wide-row-9999", last.ElementId);
        await client.RevealNodeAsync("element", last.Id, token);
        Assert.Equal(last.Id, store.Read().Selection?.NodeId);
        var parent = Assert.Single(store.Read().Tree!.Elements, node => node.Name == "Wide");
        var branch = new TreeBranch("element", parent.Id);
        await client.LoadBranchAsync(branch, false, token);
        Assert.Equal("wide-row-0", FirstChild());
        var loaded = store.Read();
        while (loaded.Tree!.Elements.Length <= DevToolsProtocol.MaxNodes)
        {
            Assert.Contains(branch, loaded.Paged!.More);
            var count = loaded.Tree.Elements.Length;
            await client.LoadBranchAsync(branch, true, token);
            loaded = store.Read();
            Assert.True(loaded.Tree!.Elements.Length > count, loaded.Message ?? "Loading the next page did not add any elements.");
        }
        await target.CommandAsync("change-wide", "CHANGED-WIDE");
        await AgentProcess.UntilAsync(() => FirstChild() == "wide-row-9999");
        Assert.Equal(last.Id, store.Read().Selection?.NodeId);

        await client.SetInspectModeAsync(true, token);
        await target.CommandAsync("pick", "PICKED");
        await AgentProcess.UntilAsync(() => store.Read().Selection is { NodeId: var id } &&
            store.Read().Tree!.Elements.Any(node => node.Id == id && node.Name == "Piece" && node.ProducingComponentId is not null));
        var state = store.Read();
        var picked = state.Tree!.Elements.Single(node => node.Id == state.Selection!.NodeId);
        Assert.NotNull(picked.ProducingComponentId);
        await client.RevealNodeAsync("component", picked.ProducingComponentId, token);
        Assert.Contains(picked.Id, store.Read().Tree!.Components.Single(node => node.Id == picked.ProducingComponentId).OutputElements);
        Assert.Equal(InspectorConnectionStatus.Connected, store.Read().Connection);

        string? FirstChild()
        {
            var tree = store.Read().Tree!;
            var current = tree.Elements.SingleOrDefault(node => node.Id == parent.Id);
            return tree.Elements.SingleOrDefault(node => node.Id == current?.Children.FirstOrDefault())?.ElementId;
        }
    }
}
