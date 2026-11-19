using System.Collections.ObjectModel;
using Nazor.Rendering;

namespace Nazor.Diagnostics;

public sealed partial class DevToolsSession
{
    /// <summary>Maximum combined component and physical node count in one complete capture.</summary>
    public const int MaxCaptureNodes = 4096;
    /// <summary>Maximum retained logical nodes, including transparent regions.</summary>
    public const int MaxCaptureLogicalNodes = 8192;
    /// <summary>Maximum logical nesting depth, including regions and components.</summary>
    public const int MaxCaptureDepth = 128;
    /// <summary>Maximum individual string length, measured in UTF-16 code units.</summary>
    public const int MaxCaptureStringLength = 16384;
    /// <summary>Maximum total copied string content in one structural snapshot.</summary>
    public const int MaxCaptureCharacters = 128 * 1024;
    /// <summary>Maximum component-child, output and physical-child links in one capture.</summary>
    public const int MaxCaptureLinks = 16384;
    /// <summary>Maximum retained parameters or attributes copied from an individual node.</summary>
    public const int MaxCaptureProperties = 128;

    /// <summary>
    /// Copies both retained trees on the owning UI thread outside a render batch. No user properties, unknown
    /// ToString implementations, style/layout updates or JSON serialization are invoked. Hidden nodes remain in
    /// the structure; visibility is unavailable when styles are dirty. Collections are detached and read-only.
    /// </summary>
    /// <returns>A complete snapshot that remains usable after target mutation or disposal.</returns>
    /// <exception cref="TreeCaptureLimitException">The complete snapshot exceeds the fixed capture budget.</exception>
    /// <exception cref="InvalidOperationException">The thread is not the UI owner, or a batch is still running.</exception>
    /// <exception cref="ObjectDisposedException">The session is disposed.</exception>
    public DiagnosticTreeSnapshot CaptureTree()
    {
        Renderer.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Renderer.IsBatchInProgress) throw new InvalidOperationException("Capture after the render batch, for example in a posted dispatcher callback.");
        var components = new Dictionary<int, CapturedComponent>();
        var physical = new Dictionary<int, CapturedPhysical>();
        var componentRoots = new List<int>();
        var elementRoots = new List<int>();
        var stack = new Stack<PendingNode>();
        var logicalCount = 0;
        var links = 0;
        var characters = 0;
        var stylesCurrent = !Scene.Styles.IsDirty;
        if (Renderer.RootComponents.Count > MaxCaptureNodes) throw new TreeCaptureLimitException();
        foreach (var root in Renderer.RootComponents.Reverse()) stack.Push(new(root.Node, null, null, 0, 0, []));
        while (stack.TryPop(out var pending))
        {
            if (++logicalCount > MaxCaptureLogicalNodes || pending.Depth > MaxCaptureDepth) throw new TreeCaptureLimitException();
            var node = pending.Node;
            if (node.IsRemoved) continue;
            var parentComponent = pending.Component;
            var parentElement = pending.Element;
            var rootId = pending.Root;
            var outputs = pending.Outputs;
            if (node is ComponentNode component)
            {
                CheckNodeCount();
                var id = IdOf(component);
                if (parentComponent is null) { rootId = id; componentRoots.Add(id); }
                else AddLink(components[parentComponent.Value].Children, id);
                components.Add(id, new(component, parentComponent, rootId));
                parentComponent = id;
                outputs = [.. outputs, id];
            }
            else if (node is ElementNode or TextNode)
            {
                CheckNodeCount();
                var id = IdOf(node);
                physical.Add(id, new(node, parentElement, rootId, parentComponent));
                if (parentElement is null) elementRoots.Add(id);
                else AddLink(physical[parentElement.Value].Children, id);
                foreach (var owner in outputs) AddLink(components[owner].Outputs, id);
                outputs = [];
                parentElement = node is ElementNode ? id : parentElement;
            }
            if (node.LogicalChildren is { } children)
            {
                if (children.Count + stack.Count + logicalCount > MaxCaptureLogicalNodes) throw new TreeCaptureLimitException();
                for (var i = children.Count - 1; i >= 0; i--)
                    stack.Push(new(children[i], parentComponent, parentElement, rootId, pending.Depth + 1, outputs));
            }
        }
        var componentData = components.Select(pair =>
        {
            var value = pair.Value;
            var type = value.Node.Component.GetType();
            return new ComponentNodeInfo(pair.Key, value.Parent, value.Root, Text(type.FullName ?? type.Name)!, Text(value.Node.Key is { } key ? Format(key) : null),
                type.Assembly == typeof(ComponentBase).Assembly || type.Assembly.GetName().Name == "Nazor.Controls",
                CapturedComponentId(value.Node.DeclaringComponent), ReadOnly(value.Children), ReadOnly(value.Outputs))
            { Parameters = Properties(value.Node.State.DirectParameters.Select(p => new KeyValuePair<string, object?>(p.Name, p.Value))) };
        }).ToArray();
        var physicalData = physical.Select(pair =>
        {
            var value = pair.Value;
            if (value.Node is TextNode text)
            {
                var declaring = value.Component is { } owner ? CapturedComponentId(components[owner].Node.DeclaringComponent) : null;
                return new StructuralNodeInfo(pair.Key, value.Parent, value.Root, NodeKind.Text, Text("#text")!, Text(text.Text), null,
                    value.Component, declaring, null, ReadOnly(value.Children));
            }
            var element = (ElementNode)value.Node;
            return new StructuralNodeInfo(pair.Key, value.Parent, value.Root, NodeKind.Element, Text(element.Name)!, null,
                Text(element.Key is { } key ? Format(key) : null), CapturedComponentId(element.Owner), CapturedComponentId(element.DeclaringComponent),
                stylesCurrent ? Scene.IsRendered(element) : null, ReadOnly(value.Children))
            {
                ElementId = Text(element.Id),
                Attributes = Properties(element.Attributes.Where(p => p.Value is not (IEventCallback or Delegate))),
                Classes = Texts(element.Class?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? []),
                States = Texts(Enum.GetValues<ElementStates>().Where(s => s != ElementStates.None && element.States.HasFlag(s)).Select(s => s.ToString().ToLowerInvariant())),
                Events = Texts(element.Attributes.Where(p => p.Value is IEventCallback or Delegate).Select(p => p.Key))
            };
        }).ToArray();
        return new(SessionId, TreeRevision, DateTimeOffset.UtcNow, ReadOnly(componentRoots), ReadOnly(elementRoots),
            Array.AsReadOnly(componentData), Array.AsReadOnly(physicalData));

        IReadOnlyDictionary<string, string> Properties(IEnumerable<KeyValuePair<string, object?>> values)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var count = 0;
            foreach (var pair in values)
            {
                if (++count > MaxCaptureProperties) throw new TreeCaptureLimitException();
                result[Text(pair.Key)!] = Text(pair.Value is null ? "null" : Format(pair.Value))!;
            }
            return new ReadOnlyDictionary<string, string>(result);
        }
        IReadOnlyList<string> Texts(IEnumerable<string> values)
        {
            var result = new List<string>();
            foreach (var value in values)
            {
                if (result.Count >= MaxCaptureProperties) throw new TreeCaptureLimitException();
                result.Add(Text(value)!);
            }
            return result.AsReadOnly();
        }
        void CheckNodeCount() { if (components.Count + physical.Count >= MaxCaptureNodes) throw new TreeCaptureLimitException(); }
        int? CapturedComponentId(IComponent? component)
        {
            var id = KnownComponentId(component);
            return id is { } value && components.ContainsKey(value) ? value : null;
        }
        void AddLink(List<int> target, int id) { if (++links > MaxCaptureLinks) throw new TreeCaptureLimitException(); target.Add(id); }
        string? Text(string? text)
        {
            if (text is null) return null;
            if (text.Length > MaxCaptureStringLength || (characters += text.Length) > MaxCaptureCharacters) throw new TreeCaptureLimitException();
            return text;
        }
    }

    private int? KnownComponentId(IComponent? component) => component is not null && knownComponents.TryGetValue(component, out var node) && !node.IsRemoved
        ? ids.GetValueOrDefault(node) : null;
    private static IReadOnlyList<int> ReadOnly(List<int> values) => Array.AsReadOnly(values.ToArray());
    private sealed record PendingNode(Node Node, int? Component, int? Element, int Root, int Depth, int[] Outputs);
    private sealed record CapturedComponent(ComponentNode Node, int? Parent, int Root)
    {
        internal List<int> Children { get; } = [];
        internal List<int> Outputs { get; } = [];
    }
    private sealed record CapturedPhysical(Node Node, int? Parent, int Root, int? Component)
    {
        internal List<int> Children { get; } = [];
    }
}
