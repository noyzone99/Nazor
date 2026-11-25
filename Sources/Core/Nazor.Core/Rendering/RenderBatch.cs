namespace Nazor.Rendering;

/// <summary>
/// The retained-tree changes applied by one batch of renders. Subsystems (styling, layout, drawing, accessibility)
/// consume it after the tree is consistent again. Nodes both created and removed inside the same batch are not
/// reported.
/// </summary>
public sealed class RenderBatch
{
    internal RenderBatch(IReadOnlyCollection<ElementNode> added, IReadOnlyCollection<Node> removed, IReadOnlyCollection<ElementNode> updated,
        IReadOnlyList<(ElementNode Element, string Name)> updatedAttributes,
        IReadOnlyCollection<TextNode> texts, IReadOnlyCollection<ElementNode> childrenChanged, IReadOnlyCollection<Node> logicalParentsChanged,
        bool rootNodesChanged, int renderedComponents)
    {
        AddedElements = added;
        RemovedNodes = removed;
        UpdatedElements = updated;
        UpdatedAttributes = updatedAttributes;
        UpdatedTexts = texts;
        ChildrenChanged = childrenChanged;
        LogicalParentsChanged = logicalParentsChanged;
        RootNodesChanged = rootNodesChanged;
        RenderedComponentCount = renderedComponents;
    }

    /// <summary>Gets the elements inserted by this render batch.</summary>
    public IReadOnlyCollection<ElementNode> AddedElements { get; }

    /// <summary>Gets physical and component nodes removed by this render batch, excluding transient creations.</summary>
    public IReadOnlyCollection<Node> RemovedNodes { get; }

    /// <summary>Existing elements whose attributes changed.</summary>
    public IReadOnlyCollection<ElementNode> UpdatedElements { get; }
    internal IReadOnlyList<(ElementNode Element, string Name)> UpdatedAttributes { get; }

    /// <summary>Gets the text nodes whose content changed in this batch.</summary>
    public IReadOnlyCollection<TextNode> UpdatedTexts { get; }

    /// <summary>Elements whose physical children were inserted, removed or moved.</summary>
    public IReadOnlyCollection<ElementNode> ChildrenChanged { get; }
    internal IReadOnlyCollection<Node> LogicalParentsChanged { get; }

    /// <summary>Gets whether the set or order of root physical nodes changed in this render batch.</summary>
    public bool RootNodesChanged { get; }

    /// <summary>Gets the number of components rendered while producing this batch.</summary>
    public int RenderedComponentCount { get; }

    /// <summary>Gets whether the batch contains no reported retained-tree changes.</summary>
    public bool IsEmpty => AddedElements.Count == 0 && RemovedNodes.Count == 0 && UpdatedElements.Count == 0 &&
        UpdatedTexts.Count == 0 && ChildrenChanged.Count == 0 && !RootNodesChanged;
}

internal sealed class RenderBatchBuilder
{
    private readonly HashSet<ElementNode> added = [];
    private readonly HashSet<Node> removed = [];
    private readonly HashSet<ElementNode> updated = [];
    private readonly List<(ElementNode Element, string Name)> updatedAttributes = [];
    private readonly HashSet<TextNode> texts = [];
    private readonly HashSet<ElementNode> childrenChanged = [];
    private HashSet<Node>? logicalParentsChanged;
    private readonly HashSet<Node> createdText = [];
    private readonly HashSet<ComponentNode> createdComponents = [];

    public bool RootNodesChanged { get; set; }
    public int RenderedComponents { get; set; }

    public void Added(ElementNode element) => added.Add(element);
    public void AddedText(TextNode text) => createdText.Add(text);
    public void AddedComponent(ComponentNode component) => createdComponents.Add(component);
    public void Updated(ElementNode element, string name)
    {
        if (added.Contains(element)) return;
        updated.Add(element);
        updatedAttributes.Add((element, name));
    }
    public void UpdatedText(TextNode text) => texts.Add(text);
    public void ChildrenOf(ElementNode element) => childrenChanged.Add(element);
    internal void LogicalChildrenOf(Node node) => (logicalParentsChanged ??= []).Add(node);

    public void Removed(Node node)
    {
        if (node is ElementNode element && added.Remove(element)) return;
        if (node is TextNode && createdText.Remove(node)) return;
        if (node is ComponentNode component && createdComponents.Remove(component)) return;
        removed.Add(node);
    }

    public RenderBatch Build() => new(
        added.ToArray(),
        removed.ToArray(),
        updated.Where(e => !e.IsRemoved && !added.Contains(e)).ToArray(),
        updatedAttributes.Where(change => !change.Element.IsRemoved).ToArray(),
        texts.Where(t => !t.IsRemoved && !createdText.Contains(t)).ToArray(),
        childrenChanged.Where(e => !e.IsRemoved).ToArray(),
        logicalParentsChanged is null ? [] : logicalParentsChanged.Where(node => !node.IsRemoved).ToArray(),
        RootNodesChanged,
        RenderedComponents);
}
