using System.Globalization;
using System.Diagnostics;
using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Rendering;

namespace Nazor.DevTools;

internal sealed class TreeBrowser(DevToolsSession session)
{
    private readonly Dictionary<string, (TreeBrowseRequest Request, DevToolsSession.TreeWalk Walk)> cursors = [];

    internal TreeBrowsePage Read(TreeBrowseRequest request, TargetState target, CancellationToken token)
    {
        var started = Stopwatch.GetTimestamp();
        var revision = session.TreeRevision;
        DevToolsSession.TreeWalk walk;
        if (request.Cursor is { } cursor)
        {
            if (!cursors.Remove(cursor, out var saved) || saved.Request != (request with { Cursor = null }))
                throw new DevToolsException(DevToolsErrors.ResyncRequired, "The browse cursor expired. Reload this branch.");
            walk = saved.Walk;
        }
        else
        {
            Node? start = null;
            if (request.NodeId is { } id && (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                (start = session.NodeOf(number)) is null || (request.Kind == "component" ? start is not ComponentNode : start is not (ElementNode or TextNode))))
                throw new DevToolsException(DevToolsErrors.Unavailable, "The inspected node has been removed or is unavailable.");
            walk = new(session, request.Kind, request.Mode, start, request.Query, request.IncludeFramework);
        }

        var page = walk.Read(token);
        string? next = null;
        if (page.HasMore)
        {
            if (cursors.Count == 128) cursors.Remove(cursors.Keys.First());
            next = Guid.NewGuid().ToString("N");
            cursors.Add(next, (request with { Cursor = null }, walk));
        }
        var nodes = page.Nodes.Select(node => new TreeBrowseNode(node.Component is { } component ? TreeSnapshotExporter.Component(component) : null,
            node.Element is { } element ? TreeSnapshotExporter.Element(element) : null, node.HasChildren)).ToArray();
        return new(DetailExporter.Stamp(target), revision, nodes, next, page.Visited, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    internal void Invalidate(TreeChange change)
    {
        if (cursors.Count == 0) return;
        if (change.BrowseOverflow || change.Removed.Count > 256) { cursors.Clear(); return; }
        var branches = change.BrowseParents.Concat(change.Removed).Select(id => id.ToString(CultureInfo.InvariantCulture)).ToHashSet();
        foreach (var (key, saved) in cursors.ToArray())
            if (saved.Request.Mode != "children" ? change.BrowseStructureChanged :
                saved.Request.NodeId is null ? change.RootsChanged : branches.Contains(saved.Request.NodeId)) cursors.Remove(key);
    }
}
