using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Nazor.Diagnostics;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed record TreeCapture(TargetIdentity Target, TreeSnapshot Snapshot, TreeDelta? Delta);

public sealed class TreeSnapshotExporter(DevToolsSession session)
{
    private DiagnosticTreeSnapshot? previous;
    private long generation;
    private long revision;
    private TargetIdentity? identity;

    public TreeCapture Capture(TargetState target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.State == "paused" || target.StopId is not null) throw new DevToolsException(DevToolsErrors.TargetPaused, "Live capture is unavailable while paused.");
        if (target.State != "running") throw new DevToolsException(DevToolsErrors.Unavailable, "The target is not running.");
        if (target.Identity.SessionId != session.SessionId || target.Generation <= 0 || target.Generation > DevToolsProtocol.MaxSequence || target.Generation < generation ||
            identity is not null && identity != target.Identity)
            throw new DevToolsException(DevToolsErrors.StaleSession, "The capture belongs to a different session or generation.");
        DiagnosticTreeSnapshot data;
        try { data = session.CaptureTree(); }
        catch (TreeCaptureLimitException) { throw new DevToolsException(DevToolsErrors.Unavailable, "A complete tree exceeds the capture budget."); }
        catch (ObjectDisposedException) { throw new DevToolsException(DevToolsErrors.Unavailable, "The diagnostics attachment was disposed."); }
        var baseline = generation == target.Generation ? previous : null;
        var nextRevision = baseline is null ? 1 : checked(revision + 1);
        if (nextRevision > DevToolsProtocol.MaxSequence) throw new DevToolsException(DevToolsErrors.ResyncRequired, "Start a new capture generation.");
        var stamp = new CaptureStamp(data.SessionId, target.Generation, "live", null, data.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        var components = data.Components.Select(Component).ToArray();
        var elements = data.Nodes.Select(Element).ToArray();
        var snapshot = new TreeSnapshot(stamp, nextRevision, data.ComponentRoots.Select(Id).ToArray(), data.ElementRoots.Select(Id).ToArray(), components, elements);
        TreeDelta? delta = null;
        if (baseline is not null)
        {
            var oldComponents = baseline.Components.ToDictionary(n => n.Id);
            var oldNodes = baseline.Nodes.ToDictionary(n => n.Id);
            var currentComponents = data.Components.ToDictionary(n => n.Id);
            var currentNodes = data.Nodes.ToDictionary(n => n.Id);
            delta = new(stamp, revision, nextRevision, snapshot.ComponentRoots.ToArray(), snapshot.ElementRoots.ToArray(),
                data.Components.Where(n => !oldComponents.TryGetValue(n.Id, out var old) || !Same(n, old)).Select(Component).ToArray(),
                data.Nodes.Where(n => !oldNodes.TryGetValue(n.Id, out var old) || !SameNode(n, old)).Select(Element).ToArray(),
                baseline.Components.Where(n => !currentComponents.ContainsKey(n.Id)).Select(n => Id(n.Id)).ToArray(),
                baseline.Nodes.Where(n => !currentNodes.ContainsKey(n.Id)).Select(n => Id(n.Id)).ToArray());
        }
        previous = data;
        identity = target.Identity;
        generation = target.Generation;
        revision = nextRevision;
        return new(target.Identity, snapshot, delta);
    }

    public static Task<JsonNode> SerializeAsync(TreeCapture capture, bool changesOnly = false, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (changesOnly && capture.Delta is null) throw new DevToolsException(DevToolsErrors.ResyncRequired, "A complete snapshot is required.");
        JsonNode payload;
        try { payload = changesOnly ? DevToolsJson.Write(capture.Delta!) : DevToolsJson.Write(capture.Snapshot); }
        catch (DevToolsException exception) when (exception.Code == DevToolsErrors.InvalidParams)
        { throw new DevToolsException(DevToolsErrors.Unavailable, "The complete capture cannot fit the wire contract."); }
        if (Encoding.UTF8.GetByteCount(payload.ToJsonString()) > DevToolsProtocol.MaxMessageBytes - 4096)
            throw new DevToolsException(DevToolsErrors.Unavailable, "A complete tree exceeds the wire payload budget.");
        return payload;
    }, cancellationToken);

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
    private static string? OptionalId(int? id) => id is { } value ? Id(value) : null;
    internal static ComponentInfo Component(ComponentNodeInfo node) => new(Id(node.Id), OptionalId(node.ParentId), node.TypeName, node.Key, node.IsFramework,
        node.Children.Select(Id).ToArray(), node.OutputNodes.Select(Id).ToArray())
    { RootId = node.RootId == 0 ? null : Id(node.RootId), DeclaringComponentId = OptionalId(node.DeclaringComponentId),
        Parameters = node.Parameters?.Select(p => new PropertyValue(p.Key, p.Value, "supplied")).ToArray() };
    internal static ElementInfo Element(StructuralNodeInfo node) => new(Id(node.Id), OptionalId(node.ParentId), node.Kind == NodeKind.Element ? "element" : "text",
        node.Name, node.Text, OptionalId(node.ProducingComponentId), OptionalId(node.DeclaringComponentId), node.Children.Select(Id).ToArray())
    { RootId = node.RootId == 0 ? null : Id(node.RootId), Key = node.Key, IsRendered = node.IsRendered, ElementId = node.ElementId,
        Attributes = node.Attributes?.Select(p => new PropertyValue(p.Key, p.Value, "attribute")).ToArray(),
        Classes = node.Classes?.ToArray(), States = node.States?.ToArray(), Events = node.Events?.ToArray() };
    private static bool Same(ComponentNodeInfo current, ComponentNodeInfo old) => current.Id == old.Id && current.ParentId == old.ParentId && current.RootId == old.RootId &&
        current.TypeName == old.TypeName && current.Key == old.Key && current.IsFramework == old.IsFramework && current.DeclaringComponentId == old.DeclaringComponentId &&
        current.Children.SequenceEqual(old.Children) && current.OutputNodes.SequenceEqual(old.OutputNodes) && SameProperties(current.Parameters, old.Parameters);
    private static bool SameNode(StructuralNodeInfo current, StructuralNodeInfo old) => current.Id == old.Id && current.ParentId == old.ParentId && current.RootId == old.RootId &&
        current.Kind == old.Kind && current.Name == old.Name && current.Text == old.Text && current.Key == old.Key && current.ProducingComponentId == old.ProducingComponentId &&
        current.DeclaringComponentId == old.DeclaringComponentId && current.IsRendered == old.IsRendered && current.Children.SequenceEqual(old.Children) && current.ElementId == old.ElementId &&
        SameProperties(current.Attributes, old.Attributes) && SameValues(current.Classes, old.Classes) && SameValues(current.States, old.States) && SameValues(current.Events, old.Events);
    private static bool SameProperties(IReadOnlyDictionary<string, string>? a, IReadOnlyDictionary<string, string>? b) =>
        a is null ? b is null : b is not null && a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var value) && value == p.Value);
    private static bool SameValues(IReadOnlyList<string>? a, IReadOnlyList<string>? b) => a is null ? b is null : b is not null && a.SequenceEqual(b);
}
