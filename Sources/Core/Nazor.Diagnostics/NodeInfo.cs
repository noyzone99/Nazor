using Nazor.Rendering;

namespace Nazor.Diagnostics;

/// <summary>What a node of the element tree is.</summary>
public enum NodeKind
{
    /// <summary>A retained element node.</summary>
    Element,

    /// <summary>A retained text node.</summary>
    Text,
}

/// <summary>
/// A node of the element tree as developer tools show it: plain data with the stable id the session gave the node,
/// which a tool can keep and a remote client can receive.
/// </summary>
public sealed record NodeInfo
{
    /// <summary>Gets or sets the identifier used to refer to this node in the diagnostics session.</summary>
    public required int Id { get; init; }

    /// <summary>Gets or sets whether the snapshot describes an element or text node.</summary>
    public required NodeKind Kind { get; init; }

    /// <summary>The element name (<c>Button</c>), or <c>#text</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The content of a text node.</summary>
    public string? Text { get; init; }

    /// <summary>Gets or sets the NSS class names captured for this node.</summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    /// <summary>The <c>id</c> attribute (the <c>Id</c> of a control).</summary>
    public string? ElementId { get; init; }

    /// <summary>The <c>@key</c> of the element, as text.</summary>
    public string? Key { get; init; }

    /// <summary>The attributes with a value, as text; event handlers are in <see cref="Events" />.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    /// <summary>The attributes that hold an event handler (<c>onclick</c>…).</summary>
    public IReadOnlyList<string> Events { get; init; } = [];

    /// <summary>Gets or sets the interaction states recorded in this node snapshot.</summary>
    public ElementStates States { get; init; }

    /// <summary>The component whose render output contains the element.</summary>
    public string? Owner { get; init; }

    /// <summary>The component whose markup declared it: its scoped styles apply.</summary>
    public string? Scope { get; init; }

    /// <summary>Known producing instance identity after logical discovery; null when not yet captured.</summary>
    public int? ProducingComponentId { get; init; }

    /// <summary>Known lexical declaring instance identity after logical discovery.</summary>
    public int? DeclaringComponentId { get; init; }

    /// <summary>False when the element or an ancestor is not displayed (<c>display: none</c>, not visible).</summary>
    public bool IsRendered { get; init; } = true;

    /// <summary>The parent element; null for the top-level nodes of a root component.</summary>
    public int? ParentId { get; init; }

    /// <summary>The children elements and texts, in order.</summary>
    public IReadOnlyList<int> Children { get; init; } = [];
}

/// <summary>
/// The changes of one batch of renders among the nodes the session knows: nodes it never gave an id to are not
/// reported, except the new children of the known ones.
/// </summary>
/// <param name="Added">The added (IReadOnlyList&lt;int&gt;).</param>
/// <param name="Removed">The removed (IReadOnlyList&lt;int&gt;).</param>
/// <param name="Updated">The updated (IReadOnlyList&lt;int&gt;).</param>
/// <param name="ChildrenChanged">The children changed (IReadOnlyList&lt;int&gt;).</param>
/// <param name="RootsChanged">The roots changed (bool).</param>
public sealed record TreeChange(
    IReadOnlyList<int> Added,
    IReadOnlyList<int> Removed,
    IReadOnlyList<int> Updated,
    IReadOnlyList<int> ChildrenChanged,
    bool RootsChanged)
{
    /// <summary>A component render/removal may have changed the logical tree, even without physical changes.</summary>
    public bool ComponentsChanged { get; init; }

    /// <summary>Renderer-batch revision, for detecting intervening changes during capture scheduling.</summary>
    public long Revision { get; init; }
}
