using System.Globalization;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;

namespace Nazor.Diagnostics;

/// <summary>
/// Exposes a window's element tree to developer tools as plain data with stable IDs. It is built on
/// the renderer, the scene and the input of the window, runs on the UI thread, and costs nothing to a window until it
/// is created. A node gets its id the first time the session meets it and keeps it while it lives; a removed node
/// loses it, and ids are never reused.
/// </summary>
public sealed partial class DevToolsSession : IDisposable
{
    private readonly Dictionary<Node, int> ids = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int, Node> nodes = [];
    private readonly Dictionary<IComponent, ComponentNode> knownComponents = new(ReferenceEqualityComparer.Instance);
    private int nextId = 1;
    private bool disposed;

    /// <summary>Inspects a live window's component tree, visual state, and input selection.</summary>
    /// <param name="renderer">The renderer that owns the component tree.</param>
    /// <param name="scene">The scene used for layout, hit testing, and drawing.</param>
    /// <param name="input">The input manager associated with the scene.</param>
    public DevToolsSession(Renderer renderer, Scene scene, InputManager input)
    {
        Renderer = renderer;
        Scene = scene;
        Input = input;
        renderer.BatchCompleted += OnBatchCompleted;
        input.VisualStateChanged += OnVisualStateChanged;
    }

    /// <summary>Gets the renderer that owns the component tree.</summary>
    public Renderer Renderer { get; }

    /// <summary>Gets the scene that provides layout, hit testing, and drawing for the window.</summary>
    public Scene Scene { get; }

    /// <summary>Gets the input manager associated with this window.</summary>
    public InputManager Input { get; }

    /// <summary>Opaque identity of this attachment, unchanged until it is disposed.</summary>
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Revision advanced after every renderer batch observed by this attachment.</summary>
    public long TreeRevision { get; private set; } = 1;

    /// <summary>
    /// The top-level nodes of every root component of the window (the window, the tooltip…), in order.
    /// </summary>
    public IReadOnlyList<int> Roots
    {
        get
        {
            Renderer.Dispatcher.AssertAccess();
            return [.. Renderer.RootComponents.SelectMany(root => root.Nodes).Where(n => n is ElementNode or TextNode).Select(IdOf)];
        }
    }

    /// <summary>Nodes were added, removed or changed by a batch of renders.</summary>
    public event Action<TreeChange>? TreeChanged;

    /// <summary>States of elements (hover, focus, pressed…) may have changed.</summary>
    public event Action? StatesChanged;

    /// <summary>The id of a node of the tree, given now if the session has not met it yet.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>The resulting int value.</returns>
    public int IdOf(Node node)
    {
        Renderer.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsRemoved) throw new ArgumentException("The node was removed from the tree.", nameof(node));
        if (ids.TryGetValue(node, out var id)) return id;
        id = checked(nextId++);
        ids.Add(node, id);
        nodes.Add(id, node);
        if (node is ComponentNode component) knownComponents[component.Component] = component;
        return id;
    }

    /// <summary>The node with an id; null when it was removed or the id was never given.</summary>
    /// <param name="id">The diagnostic identifier of the node.</param>
    /// <returns>The resulting Node? value.</returns>
    public Node? NodeOf(int id) => nodes.GetValueOrDefault(id) is { IsRemoved: false } node ? node : null;

    /// <summary>The element with an id; null for a text, a removed node or an unknown id.</summary>
    /// <param name="id">The diagnostic identifier of the node.</param>
    /// <returns>The resulting ElementNode? value.</returns>
    public ElementNode? ElementOf(int id) => NodeOf(id) as ElementNode;

    /// <summary>The node with an id as data; null when it is not in the tree.</summary>
    /// <param name="id">The diagnostic identifier of the node.</param>
    /// <returns>The resulting NodeInfo? value.</returns>
    public NodeInfo? Get(int id)
    {
        Renderer.Dispatcher.AssertAccess();
        return NodeOf(id) is ElementNode or TextNode ? Describe(NodeOf(id)!, id) : null;
    }

    /// <summary>
    /// How the style of an element came to be: its rules in cascade order with the declarations that win, and where
    /// every value comes from; null for a text or a node that is not in the tree.
    /// </summary>
    /// <param name="id">The diagnostic identifier of the element to trace.</param>
    /// <returns>The resulting Nazor.Style.StyleTrace? value.</returns>
    public Nazor.Style.StyleTrace? Trace(int id)
    {
        Renderer.Dispatcher.AssertAccess();
        return ElementOf(id) is { } element ? Scene.Styles.Trace(element) : null;
    }

    /// <summary>
    /// The boxes of an element where the window shows them; null for a text, a node that is not in the tree, or an
    /// element that is not displayed.
    /// </summary>
    /// <param name="id">The diagnostic identifier of the element to inspect.</param>
    /// <returns>The resulting BoxModel? value.</returns>
    public BoxModel? Box(int id)
    {
        Renderer.Dispatcher.AssertAccess();
        if (ElementOf(id) is not { } element) return null;
        Scene.Update();
        return ReadBox(element);
    }

    private BoxModel? ReadBox(ElementNode element)
    {
        if (!Scene.IsRendered(element) || Scene.GetLayoutNode(element) is not { } layout) return null;
        var transform = Scene.GetTransform(element);
        var (border, margin) = (layout.RoundedFrame, layout.Margin);
        var padding = border.Deflate(layout.Border);
        var outer = new Rect(border.X - margin.Left, border.Y - margin.Top, border.Width + margin.Left + margin.Right, border.Height + margin.Top + margin.Bottom);
        return new BoxModel(transform.TransformBounds(outer), transform.TransformBounds(border), transform.TransformBounds(padding),
            transform.TransformBounds(padding.Deflate(layout.Padding)), margin, layout.Border, layout.Padding, Scene.Scale);
    }

    private NodeInfo Describe(Node node, int id)
    {
        var parent = node.ParentElement is { } element ? IdOf(element) : (int?)null;
        if (node is TextNode text)
            return new NodeInfo { Id = id, Kind = NodeKind.Text, Name = "#text", Text = text.Text, ParentId = parent };

        var target = (ElementNode)node;
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var events = new List<string>();
        foreach (var (name, value) in target.Attributes)
        {
            if (value is null) continue;
            if (value is IEventCallback or Delegate) events.Add(name);
            else attributes[name] = Format(value);
        }
        events.Sort(StringComparer.Ordinal);
        if (Scene.Styles.IsDirty) Scene.Update();
        return new NodeInfo
        {
            Id = id,
            Kind = NodeKind.Element,
            Name = target.Name,
            Classes = target.Class?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [],
            ElementId = target.Id,
            Key = target.Key is { } key ? Format(key) : null,
            Attributes = attributes,
            Events = events,
            States = target.States,
            Owner = target.Owner is { } owner ? TypeName(owner.GetType()) : null,
            Scope = target.Scope is { } scope ? TypeName(scope) : null,
            ProducingComponentId = KnownComponentId(target.Owner),
            DeclaringComponentId = KnownComponentId(target.DeclaringComponent),
            IsRendered = Scene.IsRendered(target),
            ParentId = parent,
            Children = [.. target.Children.Where(c => c is ElementNode or TextNode).Select(IdOf)],
        };
    }

    private static string Format(object value) => value switch
    {
        string text => text,
        bool flag => flag ? "true" : "false",
        char character => character.ToString(),
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or DateTime or DateTimeOffset or TimeSpan or Guid
            => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
        Enum number => number.ToString(),
        _ => "<" + value.GetType().Name + ">",
    };

    /// <summary>A type as code writes it: <c>ListView&lt;String&gt;</c>, without its namespace.</summary>
    /// <param name="type">The type (Type).</param>
    /// <returns>The resulting string value.</returns>
    public static string TypeName(Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    private void OnBatchCompleted(object? sender, RenderBatch batch)
    {
        TreeRevision = checked(TreeRevision + 1);
        var removed = new List<int>();
        foreach (var node in batch.RemovedNodes)
            if (ids.Remove(node, out var id))
            {
                nodes.Remove(id);
                if (node is ComponentNode component) knownComponents.Remove(component.Component);
                removed.Add(id);
            }
        if (HighlightedId is { } highlighted && removed.Contains(highlighted)) Highlight(null);
        if (SelectedId is { } selected && removed.Contains(selected)) Select(null);

        var added = batch.AddedElements.Where(e => !e.IsRemoved && (e.ParentElement is null ? batch.RootNodesChanged : ids.ContainsKey(e.ParentElement)))
            .Select(IdOf).ToList();
        var updated = batch.UpdatedElements.Cast<Node>().Concat(batch.UpdatedTexts).Where(ids.ContainsKey).Select(n => ids[n]).ToList();
        var childrenChanged = batch.ChildrenChanged.Where(ids.ContainsKey).Select(e => ids[e]).ToList();
        var logicalChanged = batch.RenderedComponentCount > 0 || batch.RemovedNodes.Any(n => n is ComponentNode);
        if (removed.Count + added.Count + updated.Count + childrenChanged.Count == 0 && !batch.RootNodesChanged && !logicalChanged) return;
        var (browseParents, overflow) = browseTracking ? BrowseParents(batch, childrenChanged) : (Array.Empty<int>(), false);
        TreeChanged?.Invoke(new TreeChange(added, removed, updated, childrenChanged, batch.RootNodesChanged)
        {
            ComponentsChanged = logicalChanged, Revision = TreeRevision, BrowseParents = browseParents, BrowseOverflow = overflow,
            BrowseStructureChanged = batch.RootNodesChanged || batch.LogicalParentsChanged.Count != 0,
        });
    }

    private (int[] Parents, bool Overflow) BrowseParents(RenderBatch batch, List<int> childrenChanged)
    {
        if (childrenChanged.Count > 256) return ([], true);
        var known = new HashSet<int>(childrenChanged);
        var visited = 0;
        foreach (var parent in batch.LogicalParentsChanged)
        {
            for (var node = parent; node is not null; node = node.Parent)
            {
                if (++visited > 256) return ([], true);
                if (node is not ComponentNode component) continue;
                if (ids.TryGetValue(component, out var id)) known.Add(id);
                if (!IsFramework(component.Component.GetType())) break;
            }
        }
        return (known.ToArray(), false);
    }

    private bool browseTracking;

    internal void TrackBrowseChanges(bool enabled)
    {
        if (browseTracking == enabled) return;
        browseTracking = enabled;
        Renderer.InspectionBranchReaders += enabled ? 1 : -1;
    }

    private void OnVisualStateChanged() => StatesChanged?.Invoke();

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        if (disposed) return;
        TrackBrowseChanges(false);
        disposed = true;
        IsInspecting = false;
        HighlightedId = null;
        SelectedId = null;
        inspectButtons.Clear();
        labelLayout = null;
        frames.Clear();
        OverlayChanged?.Invoke();
        Renderer.BatchCompleted -= OnBatchCompleted;
        Input.VisualStateChanged -= OnVisualStateChanged;
        ids.Clear();
        nodes.Clear();
        knownComponents.Clear();
    }
}
