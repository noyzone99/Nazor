using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed partial class InspectorClient
{
    private HashSet<TreeBranch>? visibleBranches;

    internal void SetVisibleBranches(string kind, IEnumerable<string> expanded)
    {
        var visible = expanded.Select(id => new TreeBranch(kind, id)).ToHashSet();
        lock (gate) visibleBranches = visible;
    }

    internal Task LoadBranchAsync(TreeBranch branch, bool more, CancellationToken token) => WithPagedAsync(async (active, cancel) =>
    {
        var cache = active.Paged;
        var previous = cache.Branches.GetValueOrDefault(branch);
        var cursor = more ? previous?.Cursor : null;
        TreeBrowsePage page;
        try { page = await BrowseAsync(active, branch.Kind, "children", branch.Parent, cursor, null, cancel).ConfigureAwait(false); }
        catch (DevToolsException error) when (cursor is not null && error.Code == DevToolsErrors.ResyncRequired)
        {
            cursor = null;
            page = await BrowseAsync(active, branch.Kind, "children", branch.Parent, null, null, cancel).ConfigureAwait(false);
        }
        Branch(cache, branch, page, cursor is null ? [] : previous!.Nodes);
        PublishPaged(active);
    }, token);

    internal Task SetFrameworkAsync(bool include, CancellationToken token) => WithPagedAsync(async (active, cancel) =>
    {
        var cache = active.Paged;
        if (cache.IncludeFramework == include) return;
        cache.IncludeFramework = include;
        foreach (var branch in cache.Branches.Keys.Where(branch => branch.Kind == "component").ToArray()) cache.Branches.Remove(branch);
        cache.ClearPaths();
        var root = new TreeBranch("component", null);
        Branch(cache, root, await BrowseAsync(active, root.Kind, "children", null, null, null, cancel).ConfigureAwait(false), []);
        if (Store.DebuggerContext().Selection is { } selected && selected.Kind == "component")
            await RevealPagedAsync(active, selected.Kind, selected.NodeId, cancel).ConfigureAwait(false);
        PublishPaged(active);
    }, token);

    internal Task RevealNodeAsync(string kind, string id, CancellationToken token) => WithPagedAsync(async (active, cancel) =>
    {
        await RevealPagedAsync(active, kind, id, cancel, revealFramework: true).ConfigureAwait(false);
        PublishPaged(active);
        lock (gate) if (Active(active) && !debuggerPaused) Store.Select(kind, id);
    }, token);

    internal Task LoadOutputsAsync(string id, CancellationToken token) => WithPagedAsync(async (active, cancel) =>
    {
        if (active.Paged.Detail is not { Component: { } component } detail || component.Id != id || active.Paged.OutputCursor is not { } cursor) return;
        try
        {
            var page = await BrowseAsync(active, "component", "outputs", id, cursor, null, cancel).ConfigureAwait(false);
            active.Paged.UpdateDetail(detail with { Component = component with { OutputElements = component.OutputElements.Concat(page.Nodes.Select(PagedTreeCache.Id)).Distinct().ToArray() } });
            active.Paged.OutputCursor = page.Cursor;
        }
        catch (DevToolsException error) when (error.Code == DevToolsErrors.ResyncRequired)
        {
            await RevealPagedAsync(active, "component", id, cancel).ConfigureAwait(false);
        }
        PublishPaged(active);
    }, token);

    internal async Task<TreeBrowsePage?> SearchTreeAsync(string kind, string query, string? cursor, CancellationToken token)
    {
        TreeBrowsePage? result = null;
        await WithPagedAsync(async (active, cancel) =>
        {
            do
            {
                result = await BrowseAsync(active, kind, "search", null, cursor, query, cancel).ConfigureAwait(false);
                cursor = result.Cursor;
            } while (result.Nodes.Length == 0 && cursor is not null);
        }, token).ConfigureAwait(false);
        return result;
    }

    private Task RefreshPagedAsync(Connection active, CancellationToken token) => WithPagedAsync(async (_, cancel) =>
    {
        active.Paged = new() { IncludeFramework = active.Paged.IncludeFramework };
        foreach (var kind in new[] { "component", "element" })
            Branch(active.Paged, new(kind, null), await BrowseAsync(active, kind, "children", null, null, null, cancel).ConfigureAwait(false), []);

        var local = Store.DebuggerContext().Selection;
        if (active.Peer!.Supports(DevToolsCapabilities.Selection))
        {
            var selection = await active.Peer.RequestAsync<SelectionResult>(DevToolsProtocol.GetSelection, DevToolsJson.Write(new ContextRequest(Context(active))), cancel).ConfigureAwait(false);
            if (selection.NodeId is { } id) await RevealIfAvailableAsync(active, selection.Kind, id, cancel).ConfigureAwait(false);
            else if (local is not null) await RevealIfAvailableAsync(active, local.Kind, local.NodeId, cancel).ConfigureAwait(false);
            PublishPaged(active);
            lock (gate) if (Active(active) && !debuggerPaused) Store.Apply(selection, keepLocalSelection: true);
        }
        else PublishPaged(active);
    }, token, active);

    private Task ApplyPagedSelectionAsync(Connection active, SelectionResult result, bool keepLocalSelection, CancellationToken token) => WithPagedAsync(async (_, cancel) =>
    {
        lock (gate)
        {
            var outcome = Store.Apply(result, keepLocalSelection);
            if (outcome == RevisionOutcome.Stale || result.NodeId is null) return;
            if (outcome == RevisionOutcome.Accepted && (Store.DebuggerContext().Selection?.NodeId != result.NodeId ||
                active.Paged.Detail is { } detail && PagedTreeCache.Id(detail) == result.NodeId)) return;
        }
        await RevealIfAvailableAsync(active, result.Kind, result.NodeId, cancel).ConfigureAwait(false);
        PublishPaged(active);
        lock (gate) if (Active(active) && !debuggerPaused) Store.Apply(result, keepLocalSelection);
    }, token, active);

    private Task InvalidatePagedAsync(Connection active, TreeInvalidation change) => WithPagedAsync(async (_, cancel) =>
    {
        var cache = active.Paged;
        var affected = cache.Branches.Where(pair => pair.Value.Revision < change.Revision &&
            (change.All || pair.Key.Parent is null && change.Roots ||
             pair.Key.Parent is { } id && (change.Parents.Contains(id) || change.Nodes.Contains(id)) ||
             pair.Value.Nodes.Any(node => change.Nodes.Contains(PagedTreeCache.Id(node))))).Select(pair => pair.Key).ToArray();
        if (affected.Length == 0) return;
        var selected = Store.DebuggerContext().Selection;
        HashSet<TreeBranch>? visible;
        lock (gate) visible = visibleBranches;
        cache.ClearPaths();
        foreach (var branch in affected.OrderBy(branch => branch.Parent is null ? 0 : 1))
        {
            if (branch.Parent is not null && visible is not null && !visible.Contains(branch))
            {
                cache.Branches.Remove(branch);
                continue;
            }
            try { Branch(cache, branch, await BrowseAsync(active, branch.Kind, "children", branch.Parent, null, null, cancel).ConfigureAwait(false), []); }
            catch (DevToolsException error) when (error.Code == DevToolsErrors.Unavailable) { cache.Branches.Remove(branch); }
        }
        if (selected is not null) await RevealIfAvailableAsync(active, selected.Kind, selected.NodeId, cancel).ConfigureAwait(false);
        PublishPaged(active);
    }, active.Token, active);

    private static async Task RevealIfAvailableAsync(Connection active, string kind, string id, CancellationToken token)
    {
        try { await RevealPagedAsync(active, kind, id, token).ConfigureAwait(false); }
        catch (DevToolsException error) when (error.Code is DevToolsErrors.Unavailable or DevToolsErrors.ResyncRequired) { }
    }

    private static async Task RevealPagedAsync(Connection active, string kind, string id, CancellationToken token, bool revealFramework = false)
    {
        var details = await BrowseAsync(active, kind, "details", id, null, null, token).ConfigureAwait(false);
        if (details.Nodes is not [var detail] || PagedTreeCache.Id(detail) != id)
            throw new DevToolsException(DevToolsErrors.InvalidParams, "The selected node description does not match the request.");
        if (revealFramework && detail.Component is { IsFramework: true } && !active.Paged.IncludeFramework)
        {
            active.Paged.IncludeFramework = true;
            foreach (var branch in active.Paged.Branches.Keys.Where(branch => branch.Kind == "component").ToArray()) active.Paged.Branches.Remove(branch);
            active.Paged.ClearPaths();
            Branch(active.Paged, new(kind, null), await BrowseAsync(active, kind, "children", null, null, null, token).ConfigureAwait(false), []);
        }
        var path = new List<TreeBrowseNode>();
        string? cursor = null;
        do
        {
            var page = await BrowseAsync(active, kind, "path", id, cursor, null, token).ConfigureAwait(false);
            path.AddRange(page.Nodes);
            cursor = page.Cursor;
        } while (cursor is not null);
        if (path.Count == 0 || PagedTreeCache.Id(path[0]) != id) return;

        string? outputCursor = null;
        if (detail.Component is { } component)
        {
            var output = await BrowseAsync(active, kind, "outputs", id, null, null, token).ConfigureAwait(false);
            detail = detail with { Component = component with { OutputElements = output.Nodes.Select(PagedTreeCache.Id).ToArray() } };
            outputCursor = output.Cursor;
        }
        else
        {
            // Walking the path binds any producing components before we ask for their IDs.
            details = await BrowseAsync(active, kind, "details", id, null, null, token).ConfigureAwait(false);
            detail = details.Nodes.Single();
        }

        var cache = active.Paged;
        cache.AddPath(kind, path, detail);
        cache.OutputCursor = outputCursor;
        cache.Capture = details.Capture;
        cache.Revision = Math.Max(cache.Revision, details.Revision);
    }

    private async Task WithPagedAsync(Func<Connection, CancellationToken, Task> work, CancellationToken token, Connection? expected = null)
    {
        Connection active;
        CancellationToken execution;
        lock (gate)
        {
            if (debuggerPaused || current is not { } found || expected is not null && expected != found ||
                found.Peer?.Supports(DevToolsCapabilities.PagedTree) != true) return;
            active = found;
            execution = active.Execution.Token;
        }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, active.Token, execution);
        try
        {
            await active.Requests.WaitAsync(lifetime.Token).ConfigureAwait(false);
            try { await work(active, lifetime.Token).ConfigureAwait(false); }
            finally { active.Requests.Release(); }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or DevToolsException or OperationCanceledException)
        {
            lock (gate)
                if (Active(active) && !debuggerPaused && !lifetime.IsCancellationRequested)
                    Store.Report(error is DevToolsException { Code: DevToolsErrors.ResyncRequired }
                        ? "The tree changed while reading this page. Start the request again." : Failure(error));
        }
    }

    private static RequestContext Context(Connection active)
    {
        var target = active.Peer!.RemoteTarget!;
        return new(target.Identity.SessionId, target.Generation, target.StopId);
    }

    private static async Task<TreeBrowsePage> BrowseAsync(Connection active, string kind, string mode, string? id, string? cursor, string? query, CancellationToken token)
    {
        var request = new TreeBrowseRequest(Context(active), kind, mode, id, cursor, query, active.Paged.IncludeFramework);
        var page = await active.Peer!.RequestAsync<TreeBrowsePage>(DevToolsProtocol.BrowseTree, DevToolsJson.Write(request), DateTimeOffset.UtcNow.AddSeconds(2), token).ConfigureAwait(false);
        if (page.Nodes.Any(node => (mode == "outputs" || kind == "element") != (node.Element is not null) ||
            mode == "children" && ((node.Component?.ParentId ?? node.Element?.ParentId) != id || PagedTreeCache.Id(node) == id)))
            throw new DevToolsException(DevToolsErrors.InvalidParams, "The browse page belongs to a different branch.");
        return page;
    }

    private static void Branch(PagedTreeCache cache, TreeBranch branch, TreeBrowsePage page, TreeBrowseNode[] before)
    {
        cache.Capture = page.Capture;
        cache.Revision = Math.Max(cache.Revision, page.Revision);
        cache.Branches[branch] = new(page.Revision, before.Concat(page.Nodes).DistinctBy(PagedTreeCache.Id).ToArray(), page.Cursor);
    }

    private void PublishPaged(Connection active)
    {
        lock (gate) if (Active(active) && !debuggerPaused && active.Paged.Capture is not null) Store.ApplyPaged(active.Paged);
    }
}
