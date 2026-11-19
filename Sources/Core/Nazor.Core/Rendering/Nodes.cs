namespace Nazor.Rendering;

/// <summary>
/// A node of the retained tree produced by the renderer. Logical nodes mirror the render frames (components and
/// regions included); the physical tree used by styling, layout, drawing and accessibility contains only
/// <see cref="ElementNode" /> and <see cref="TextNode" />. Matched frames keep their node across renders, so node
/// identity is element identity.
/// </summary>
public abstract class Node
{
    private object?[]? features;

    /// <summary>The logical parent: an element, a component, a region or null for a root component.</summary>
    public Node? Parent { get; internal set; }

    /// <summary>The nearest element ancestor, i.e. the physical parent.</summary>
    public ElementNode? ParentElement
    {
        get
        {
            for (var current = Parent; current is not null; current = current.Parent)
                if (current is ElementNode element) return element;
            return null;
        }
    }

    /// <summary>The root component that contains this node.</summary>
    public RootComponent? Root
    {
        get
        {
            Node current = this;
            while (current.Parent is not null) current = current.Parent;
            return (current as ComponentNode)?.RootOwner;
        }
    }

    /// <summary>Gets whether the node has been removed from the retained tree.</summary>
    public bool IsRemoved { get; internal set; }

    internal virtual List<Node>? LogicalChildren => null;

    /// <summary>Per-node data owned by other subsystems (style, layout, visuals, semantics).</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The resulting T? value.</returns>
    public T? GetFeature<T>() where T : class
    {
        var slot = FeatureSlot<T>.Index;
        return features is not null && slot < features.Length ? (T?)features[slot] : null;
    }

    /// <summary>Stores or clears subsystem-owned data of the specified feature type on this node.</summary>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    public void SetFeature<T>(T? value) where T : class
    {
        var slot = FeatureSlot<T>.Index;
        if (features is null || slot >= features.Length) Array.Resize(ref features, Math.Max(slot + 1, 4));
        features[slot] = value;
    }

    private static class FeatureSlotCounter
    {
        public static int Next = -1;
    }

    private static class FeatureSlot<T>
    {
        public static readonly int Index = Interlocked.Increment(ref FeatureSlotCounter.Next);
    }
}

/// <summary>Text content rendered by <c>AddContent</c>.</summary>
public sealed class TextNode : Node
{
    internal TextNode(string text) => Text = text;

    /// <summary>Gets the text represented by this object.</summary>
    public string Text { get; internal set; }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"\"{Text}\"";
}

/// <summary>Interaction states maintained by input and controls, observed by styling.</summary>
[Flags]
public enum ElementStates
{
    /// <summary>No interaction state is active.</summary>
    None = 0,

    /// <summary>The pointer is over the element or its relevant descendant.</summary>
    Hover = 1,

    /// <summary>The element is in its active pressed state.</summary>
    Pressed = 2,

    /// <summary>The element has keyboard focus.</summary>
    Focus = 4,

    /// <summary>The element has focus that should display a keyboard focus indicator.</summary>
    FocusVisible = 8,

    /// <summary>The element is disabled for user interaction.</summary>
    Disabled = 16,

    /// <summary>The element represents a checked or selected value.</summary>
    Checked = 32,

    /// <summary>The element or one of its descendants has the focus (<c>:focus-within</c>).</summary>
    FocusWithin = 64,

    /// <summary>The element is receiving wheel scrolling, including its configured idle timeout.</summary>
    WheelScrolling = 128,

    /// <summary>The desktop presents this window as active.</summary>
    WindowActive = 256,

    /// <summary>The desktop presents this window as inactive.</summary>
    WindowInactive = 512,

    /// <summary>The text editor is presenting a nonempty IME composition; placeholder text is suppressed.</summary>
    Composing = 1024,

    /// <summary>Validation feedback is eligible after interaction or a form submission attempt. Does not imply invalidity.</summary>
    ValidationEligible = 2048,

    /// <summary>The anchored overlay was arranged above its anchor; maintained by presentation for visual styling.</summary>
    PopupAbove = 4096,

    /// <summary>The anchored overlay was arranged to the left of its anchor; maintained by presentation for visual styling.</summary>
    PopupLeft = 8192,

    /// <summary>The anchored overlay was arranged to the right of its anchor; maintained by presentation for visual styling.</summary>
    PopupRight = 16384,

    /// <summary>The arranged overlay's right edge is nearer its anchor than its left edge; equal distances follow end alignment. Maintained by presentation for animation origins.</summary>
    PopupOriginRight = 32768,

    /// <summary>The arranged overlay's bottom edge is nearer its anchor than its top edge; equal distances follow end alignment. Maintained by presentation for animation origins.</summary>
    PopupOriginBottom = 65536,
}

/// <summary>A control's element: a name used by type selectors, attributes and physical children.</summary>
public sealed class ElementNode : Node
{
    /// <summary>Gets whether this element or an ancestor disables user interaction.</summary>
    public bool IsDisabled
    {
        get
        {
            for (var node = this; node is not null; node = node.ParentElement)
                if ((node.States & ElementStates.Disabled) != 0 || node.Attributes.GetValueOrDefault("disabled") is true) return true;
            return false;
        }
    }

    private readonly Dictionary<string, object?> attributes = new(StringComparer.Ordinal);
    private readonly List<Node> logicalChildren = [];
    private readonly List<Node> children = [];
    private bool childrenDirty = true;
    private ElementStates states;

    internal ElementNode(string name, Type? scope, IComponent? owner, IComponent? declaringComponent = null)
    {
        Name = name;
        Scope = scope;
        Owner = owner;
        DeclaringComponent = declaringComponent;
    }

    /// <summary>Gets the element name matched by NSS type selectors.</summary>
    public string Name { get; }

    /// <summary>The component whose markup declared the control that produced this element.</summary>
    public Type? Scope { get; }

    /// <summary>The component whose render output contains this element.</summary>
    public IComponent? Owner { get; }

    /// <summary>The instance whose markup declared the control, including projected content.</summary>
    public IComponent? DeclaringComponent { get; }

    /// <summary>Gets the key used to retain identity across render updates.</summary>
    public object? Key { get; internal set; }

    /// <summary>Gets the element attributes or attribute-selector constraints.</summary>
    public IReadOnlyDictionary<string, object?> Attributes => attributes;
    internal Dictionary<string, object?> AttributeValues => attributes;

    /// <summary>Gets the space-separated NSS classes applied to the element.</summary>
    public string? Class => attributes.GetValueOrDefault("class") as string;

    /// <summary>Gets the element's id attribute, or null when no identifier is assigned.</summary>
    public string? Id => attributes.GetValueOrDefault("id") as string;

    /// <summary>Gets the element's current interaction state flags.</summary>
    public ElementStates States => states;

    /// <summary>Raised after <see cref="States" /> changes; styling uses it for pseudo-classes.</summary>
    public event Action<ElementNode, ElementStates>? StatesChanged;

    /// <summary>Adds or removes the specified interaction state and notifies the renderer when it changes.</summary>
    /// <param name="state">The state (ElementStates).</param>
    /// <param name="value">The value to use.</param>
    public void SetState(ElementStates state, bool value)
    {
        var updated = value ? states | state : states & ~state;
        if (updated == states) return;
        var previous = states;
        states = updated;
        StatesChanged?.Invoke(this, previous);
    }

    /// <summary>Physical children: elements and text, with components and regions flattened.</summary>
    public IReadOnlyList<Node> Children
    {
        get
        {
            if (childrenDirty)
            {
                children.Clear();
                Flatten(logicalChildren, children);
                childrenDirty = false;
            }
            return children;
        }
    }

    internal override List<Node> LogicalChildren => logicalChildren;

    internal void SetAttribute(string name, object? value) => attributes[name] = value;

    internal bool RemoveAttribute(string name) => attributes.Remove(name);

    internal void MarkChildrenDirty() => childrenDirty = true;

    internal static void Flatten(List<Node> logical, List<Node> physical)
    {
        foreach (var node in logical)
        {
            if (node is ElementNode or TextNode) physical.Add(node);
            else if (node.LogicalChildren is { } nested) Flatten(nested, physical);
        }
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Class is null ? Name : $"{Name}.{Class.Replace(' ', '.')}";
}

/// <summary>A component instance in the logical tree; its children are its render output.</summary>
public sealed class ComponentNode : Node
{
    private readonly List<Node> logicalChildren = [];

    internal ComponentNode(ComponentState state)
    {
        State = state;
        StyleName = state.Component.GetType().Name.Split('`')[0];
    }

    internal ComponentState State { get; }

    internal object? Key { get; set; }

    /// <summary>Gets the component instance represented or hosted by this node.</summary>
    public IComponent Component => State.Component;

    /// <summary>Gets the type whose markup declares this component.</summary>
    public Type? DeclaringScope => State.DeclaringScope;

    /// <summary>Gets the component instance whose markup declares the content.</summary>
    public IComponent? DeclaringComponent => State.DeclaringComponent;

    /// <summary>The unqualified component name, without generic arity.</summary>
    public string StyleName { get; }

    /// <summary>Controls already represented by a named element keep ordinary element-selector semantics.</summary>
    public bool HasNamedElement => HasNamedElementIn(logicalChildren);

    private bool HasNamedElementIn(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is ElementNode element && ReferenceEquals(element.Owner, Component) && element.Name == StyleName) return true;
            if (node is RegionNode region && HasNamedElementIn(region.LogicalChildren)) return true;
        }
        return false;
    }

    internal RootComponent? RootOwner { get; set; }

    internal override List<Node> LogicalChildren => logicalChildren;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"<{Component.GetType().Name}>";
}

internal sealed class RegionNode : Node
{
    private readonly List<Node> logicalChildren = [];

    internal override List<Node> LogicalChildren => logicalChildren;
}

/// <summary>A component added directly by the host, with the physical nodes it produces.</summary>
public sealed class RootComponent
{
    private readonly List<Node> nodes = [];
    private bool nodesDirty = true;

    internal RootComponent(Renderer renderer, ComponentNode node, ParameterView parameters)
    {
        Renderer = renderer;
        Node = node;
        Parameters = parameters;
        if (node.Parent is null) node.RootOwner = this;
    }

    /// <summary>Gets the renderer that owns the component tree.</summary>
    public Renderer Renderer { get; }

    /// <summary>Gets the component instance represented or hosted by this node.</summary>
    public IComponent Component => Node.Component;

    /// <summary>
    /// The parameters passed to <see cref="Renderer.AddRootComponent(Type, ParameterView)" />; set again by
    /// <see cref="Renderer.RenderOnHotReload" />.
    /// </summary>
    public ParameterView Parameters { get; }

    internal ComponentNode Node { get; }

    /// <summary>Top-level physical nodes rendered by the root component.</summary>
    public IReadOnlyList<Node> Nodes
    {
        get
        {
            if (nodesDirty || Node.Parent is not null)
            {
                nodes.Clear();
                ElementNode.Flatten(Node.LogicalChildren, nodes);
                nodesDirty = false;
            }
            return nodes;
        }
    }

    internal void MarkNodesDirty() => nodesDirty = true;
}
