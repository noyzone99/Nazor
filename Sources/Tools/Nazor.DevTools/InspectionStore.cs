using System.Collections.ObjectModel;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public enum InspectorConnectionStatus { Disconnected, Connecting, Connected, Unresponsive }
public sealed record InspectorSelection(string Kind, string NodeId, CaptureStamp Capture);
public sealed record InspectionState(TargetState? Target, InspectorConnectionStatus Connection, bool NeedsTree,
    TreeSnapshot? Tree, InspectorSelection? Selection, IReadOnlyDictionary<string, StyleDetails> Styles,
    IReadOnlyDictionary<string, GeometryDetails> Geometry, FrameBatch? Frames, string? Message, bool IsInspecting = false, string? DetailMessage = null,
    DebuggerStatus? Debugger = null, DebuggerFields? DebuggerFields = null, string? ApplicationName = null)
{
    internal PagedTreeState? Paged { get; init; }
    public bool IsRetained => Tree is not null && (NeedsTree || Connection != InspectorConnectionStatus.Connected || Target is null || Target.State == "exited" ||
        Tree.Capture.SessionId != Target.Identity.SessionId || Tree.Capture.Generation != Target.Generation || Tree.Capture.StopId != Target.StopId ||
        Tree.Capture.Source == "retained" || Tree.Capture.Source == "live" && Target.State != "running");
}

public sealed class InspectionStore
{
    public const int MaxDetailEntries = 16;
    public string WindowTitle => TargetName is { } name ? "Nazor DevTools - " + name : "Nazor DevTools";
    /// <summary>The inspected application's name, its process when the name is unknown, or null without a target.</summary>
    public string? TargetName
    {
        get { lock (gate) return target is null ? null : applicationName ?? "Process " + target.Identity.ProcessId; }
    }
    private readonly object gate = new();
    private TargetState? target;
    private InspectorConnectionStatus connection;
    private TreeSnapshot? tree;
    private PagedTreeState? paged;
    private InspectorSelection? selection;
    private readonly Dictionary<string, StyleDetails> styles = [];
    private readonly Dictionary<string, GeometryDetails> geometry = [];
    private FrameBatch? frames;
    private DebuggerStatus? debugger;
    private DebuggerFields? debuggerFields;
    private bool needsTree = true;
    private bool frameGap;
    private string? message;
    private string? applicationName;
    private string? detailMessage;
    private bool isInspecting;
    private CaptureStamp? selectionCapture;
    private long? selectionRevision;
    private SelectionResult? remoteSelection;
    public event Action? Changed;
    public event Action? FramesChanged;
    public FrameBatch? ReadFrames() { lock (gate) return frames is null ? null : Copy(frames); }

    public InspectionState Read()
    {
        lock (gate) return new InspectionState(debugger is { State: "paused" } && target?.Identity.ProcessId == debugger.ProcessId ? target with { State = "paused", StopId = debugger.StopId } : target, connection, needsTree, tree is null ? null : Copy(tree), selection,
            new ReadOnlyDictionary<string, StyleDetails>(styles.ToDictionary(p => p.Key, p => Copy(p.Value))),
            new ReadOnlyDictionary<string, GeometryDetails>(geometry.ToDictionary()), frames is null ? null : Copy(frames), message, isInspecting, detailMessage, debugger,
            debuggerFields is null ? null : debuggerFields with { Fields = debuggerFields.Fields.ToArray() }, applicationName) { Paged = paged };
    }

    public (TargetState? Target, InspectorSelection? Selection, bool HasTree, InspectorConnectionStatus Connection) DebuggerContext()
    { lock (gate) return (target, selection, tree is not null, connection); }

    public void SetDebugger(DebuggerStatus state)
    {
        DevToolsJson.Write(state);
        lock (gate)
        {
            if (target?.Identity.ProcessId != state.ProcessId || debugger == state) return;
            debugger = state;
            if (state.State == "paused") isInspecting = false;
        }
        Changed?.Invoke();
    }
    public bool Apply(DebuggerFields fields)
    {
        DevToolsJson.Write(fields);
        lock (gate)
        {
            if (debugger is not { State: "paused" } || debugger.StopId != fields.Capture.StopId || target?.Identity.SessionId != fields.Capture.SessionId ||
                target.Generation != fields.Capture.Generation || tree?.Components.Any(c => c.Id == fields.ComponentId) != true) return false;
            debuggerFields = fields with { Fields = fields.Fields.ToArray() };
        }
        Changed?.Invoke();
        return true;
    }

    public void Connecting(TargetIdentity identity, string? name = null)
    {
        DevToolsJson.Write(new TargetState(identity, 1, "running", null));
        lock (gate)
        {
            if (target?.Identity != identity) ClearData();
            applicationName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            target = target?.Identity == identity ? target : new(identity, 1, "running", null);
            connection = InspectorConnectionStatus.Connecting;
            needsTree = true;
            message = null;
        }
        Changed?.Invoke();
    }

    public bool Bind(TargetState state)
    {
        DevToolsJson.Write(state);
        lock (gate)
        {
            if (target?.Identity.SessionId == state.Identity.SessionId)
            {
                if (target.Identity != state.Identity) throw new DevToolsException(DevToolsErrors.StaleSession, "The target identity changed within a session.");
                if (state.Generation < target.Generation) return false;
                if (state.Generation == target.Generation && state != target)
                    throw new DevToolsException(DevToolsErrors.StaleSession, "Execution changes require a newer generation.");
            }
            if (target == state && connection is InspectorConnectionStatus.Connected or InspectorConnectionStatus.Unresponsive) return true;
            if (target?.Identity != state.Identity) ClearData();
            if (target != state) { needsTree = true; frameGap = false; isInspecting = false; selectionCapture = null; selectionRevision = null; remoteSelection = null; }
            target = state;
            connection = InspectorConnectionStatus.Connected;
            message = null;
        }
        Changed?.Invoke();
        return true;
    }

    public void Disconnected(string? reason = null)
    {
        lock (gate) { connection = InspectorConnectionStatus.Disconnected; message = reason; needsTree = true; isInspecting = false; }
        Changed?.Invoke();
    }

    public void Responsive()
    {
        lock (gate)
        {
            if (connection != InspectorConnectionStatus.Unresponsive) return;
            connection = InspectorConnectionStatus.Connected;
        }
        Changed?.Invoke();
    }

    public void Unresponsive(string reason)
    {
        lock (gate) { connection = InspectorConnectionStatus.Unresponsive; message = reason; isInspecting = false; }
        Changed?.Invoke();
    }

    public void RequireTree(string? reason = null)
    {
        lock (gate) { needsTree = true; message = reason; }
        Changed?.Invoke();
    }

    public RevisionOutcome Apply(TreeSnapshot snapshot)
    {
        DevToolsJson.Write(snapshot);
        return ApplyTree(snapshot, null);
    }

    internal RevisionOutcome ApplyPaged(PagedTreeCache cache)
    {
        var (snapshot, state) = cache.View();
        return ApplyTree(snapshot, state);
    }

    private RevisionOutcome ApplyTree(TreeSnapshot snapshot, PagedTreeState? state)
    {
        lock (gate)
        {
            if (!Matches(snapshot.Capture) || tree is not null && SameContext(tree.Capture, snapshot.Capture) && snapshot.Revision < tree.Revision) return RevisionOutcome.Stale;
            var same = tree is not null && SameContext(tree.Capture, snapshot.Capture);
            tree = state is null ? Copy(snapshot) : snapshot;
            paged = state;
            if (!same)
            {
                styles.Clear(); geometry.Clear(); detailMessage = null;
                if (frames is not null && !SameContext(frames.Capture, snapshot.Capture)) { frames = null; frameGap = false; }
            }
            Prune();
            needsTree = false;
            message = null;
            connection = InspectorConnectionStatus.Connected;
        }
        Changed?.Invoke();
        return RevisionOutcome.Accepted;
    }

    public RevisionOutcome Apply(TreeDelta delta)
    {
        DevToolsJson.Write(delta);
        RevisionOutcome result;
        lock (gate)
        {
            if (!Matches(delta.Capture) || tree is not null && SameContext(tree.Capture, delta.Capture) && delta.Revision <= tree.Revision) return RevisionOutcome.Stale;
            if (needsTree || tree is null || !SameContext(tree.Capture, delta.Capture) || delta.BaseRevision != tree.Revision)
            { needsTree = true; result = RevisionOutcome.ResyncRequired; }
            else
            {
                var components = tree.Components.ToDictionary(c => c.Id);
                var elements = tree.Elements.ToDictionary(e => e.Id);
                foreach (var id in delta.RemovedComponents) components.Remove(id);
                foreach (var id in delta.RemovedElements) elements.Remove(id);
                foreach (var node in delta.Components) components[node.Id] = node;
                foreach (var node in delta.Elements) elements[node.Id] = node;
                var candidate = new TreeSnapshot(delta.Capture, delta.Revision, delta.ComponentRoots, delta.ElementRoots,
                    components.Values.ToArray(), elements.Values.ToArray());
                try
                {
                    DevToolsJson.Write(candidate);
                    tree = Copy(candidate);
                    Prune();
                    message = null;
                    connection = InspectorConnectionStatus.Connected;
                    result = RevisionOutcome.Accepted;
                }
                catch (DevToolsException)
                {
                    needsTree = true;
                    message = "The tree update was incomplete. A full snapshot is required.";
                    result = RevisionOutcome.ResyncRequired;
                }
            }
        }
        Changed?.Invoke();
        return result;
    }

    public void ReportDetails(string? reason)
    {
        lock (gate) { if (detailMessage == reason) return; detailMessage = reason; }
        Changed?.Invoke();
    }

    public void Report(string reason)
    {
        lock (gate) message = reason;
        Changed?.Invoke();
    }

    /// <param name="keepLocalSelection">True when this inspector cleared the app overlay itself, so the tree keeps its selection.</param>
    public RevisionOutcome Apply(SelectionResult result, bool keepLocalSelection = false)
    {
        DevToolsJson.Write(result);
        lock (gate)
        {
            if (!Matches(result.Capture) || selectionCapture is not null && SameContext(result.Capture, selectionCapture) &&
                (result.Revision is { } revision && selectionRevision is { } old ? revision < old : Older(result.Capture, selectionCapture))) return RevisionOutcome.Stale;
            if (needsTree || tree is null || !SameContext(tree.Capture, result.Capture) || result.NodeId is { } id &&
                !(result.Kind == "component" ? tree.Components.Any(c => c.Id == id) : tree.Elements.Any(e => e.Id == id))) return RevisionOutcome.ResyncRequired;
            var changed = remoteSelection?.Kind != result.Kind || remoteSelection.NodeId != result.NodeId || remoteSelection.IsInspecting == true && result.IsInspecting == false;
            if (result.NodeId is not null && changed) selection = new(result.Kind, result.NodeId, result.Capture);
            else if (result.NodeId is null && !keepLocalSelection && selection?.Kind == remoteSelection?.Kind && selection?.NodeId == remoteSelection?.NodeId) selection = null;
            remoteSelection = result;
            isInspecting = result.IsInspecting == true;
            selectionCapture = result.Capture;
            selectionRevision = result.Revision;
        }
        Changed?.Invoke();
        return RevisionOutcome.Accepted;
    }

    public bool Select(string kind, string? id)
    {
        lock (gate)
        {
            if (id is null) selection = null;
            else
            {
                if (tree is null || !(kind == "component" ? tree.Components.Any(c => c.Id == id) : kind == "element" && tree.Elements.Any(e => e.Id == id))) return false;
                selection = new(kind, id, tree.Capture);
            }
        }
        Changed?.Invoke();
        return true;
    }

    public bool Apply(StyleDetails details)
    {
        DevToolsJson.Write(details);
        lock (gate)
        {
            if (!CurrentElement(details.Capture, details.ElementId) || styles.TryGetValue(details.ElementId, out var old) && Older(details.Capture, old.Capture)) return false;
            Keep(styles, details.ElementId, Copy(details));
        }
        Changed?.Invoke();
        return true;
    }

    public bool Apply(GeometryDetails details)
    {
        DevToolsJson.Write(details);
        lock (gate)
        {
            if (!CurrentElement(details.Capture, details.ElementId) || geometry.TryGetValue(details.ElementId, out var old) && Older(details.Capture, old.Capture)) return false;
            Keep(geometry, details.ElementId, details);
        }
        Changed?.Invoke();
        return true;
    }

    public RevisionOutcome Apply(FrameBatch batch, bool resynchronize = false)
    {
        DevToolsJson.Write(batch);
        lock (gate)
        {
            if (!Matches(batch.Capture) || frames is not null && SameContext(frames.Capture, batch.Capture) && batch.Revision <= frames.Revision) return RevisionOutcome.Stale;
            var previous = frames is not null && SameContext(frames.Capture, batch.Capture) ? frames : null;
            if (!resynchronize && (frameGap || batch.BaseRevision != (previous?.Revision ?? 0))) { frameGap = true; return RevisionOutcome.ResyncRequired; }
            if (!resynchronize && previous is { Frames.Length: > 0 } && batch.Frames.Length > 0 && batch.Frames[0].Number <= previous.Frames[^1].Number)
            { frameGap = true; return RevisionOutcome.ResyncRequired; }
            frames = Copy(batch with { Frames = resynchronize ? batch.Frames : (previous?.Frames ?? []).Concat(batch.Frames).TakeLast(DevToolsProtocol.MaxFrames).ToArray() });
            frameGap = false;
        }
        FramesChanged?.Invoke();
        return RevisionOutcome.Accepted;
    }

    public void Clear()
    {
        lock (gate) { ClearData(); target = null; connection = InspectorConnectionStatus.Disconnected; message = null; }
        Changed?.Invoke();
    }

    private void ClearData() { applicationName = null; debugger = null; debuggerFields = null; tree = null; paged = null; selection = null; isInspecting = false; selectionCapture = null; selectionRevision = null; remoteSelection = null; styles.Clear(); geometry.Clear(); detailMessage = null; frames = null; needsTree = true; frameGap = false; }
    private bool Matches(CaptureStamp stamp) => target is not null && stamp.SessionId == target.Identity.SessionId && stamp.Generation == target.Generation && stamp.StopId == target.StopId;
    private bool CurrentElement(CaptureStamp capture, string id) => Matches(capture) && tree is not null && SameContext(tree.Capture, capture) && tree.Elements.Any(e => e.Id == id);
    private static bool SameContext(CaptureStamp a, CaptureStamp b) => a.SessionId == b.SessionId && a.Generation == b.Generation && a.StopId == b.StopId;
    private static bool Older(CaptureStamp a, CaptureStamp b) => DateTimeOffset.Parse(a.CapturedAtUtc, System.Globalization.CultureInfo.InvariantCulture) < DateTimeOffset.Parse(b.CapturedAtUtc, System.Globalization.CultureInfo.InvariantCulture);
    private static void Keep<T>(Dictionary<string, T> cache, string id, T data)
    {
        cache.Remove(id);
        if (cache.Count >= MaxDetailEntries) cache.Remove(cache.Keys.First());
        cache[id] = data;
    }
    private void Prune()
    {
        var elements = tree!.Elements.Select(n => n.Id).ToHashSet();
        foreach (var id in styles.Keys.Where(id => !elements.Contains(id)).ToArray()) styles.Remove(id);
        foreach (var id in geometry.Keys.Where(id => !elements.Contains(id)).ToArray()) geometry.Remove(id);
        if (selection is not null)
        {
            var exists = selection.Kind == "component" ? tree.Components.Any(c => c.Id == selection.NodeId) : elements.Contains(selection.NodeId);
            selection = exists ? selection with { Capture = tree.Capture } : null;
        }
    }
    private static TreeSnapshot Copy(TreeSnapshot data) => data with
    {
        ComponentRoots = data.ComponentRoots.ToArray(), ElementRoots = data.ElementRoots.ToArray(),
        Components = data.Components.Select(c => c with { Children = c.Children.ToArray(), OutputElements = c.OutputElements.ToArray(), Parameters = c.Parameters?.Select(Copy).ToArray() }).ToArray(),
        Elements = data.Elements.Select(e => e with { Children = e.Children.ToArray(), Attributes = e.Attributes?.Select(Copy).ToArray(), Classes = e.Classes?.ToArray(), States = e.States?.ToArray(), Events = e.Events?.ToArray() }).ToArray()
    };
    private static PropertyValue Copy(PropertyValue p) => p with { Longhands = p.Longhands?.ToArray(), Winning = p.Winning?.ToArray() };
    private static StyleDetails Copy(StyleDetails data) => data with { Computed = data.Computed.Select(Copy).ToArray(), CustomProperties = data.CustomProperties?.Select(Copy).ToArray(), Rules = data.Rules.Select(r => r with { Declarations = r.Declarations.Select(Copy).ToArray() }).ToArray() };
    private static FrameBatch Copy(FrameBatch data) => data with { Frames = data.Frames.Select(f => f with { Causes = f.Causes.ToArray(), Damage = f.Damage.ToArray() }).ToArray() };
}
