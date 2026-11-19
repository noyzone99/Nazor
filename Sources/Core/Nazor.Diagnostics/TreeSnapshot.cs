namespace Nazor.Diagnostics;

/// <summary>A detached retained component instance, including components with no physical output.</summary>
/// <param name="Id">Session identity of the component node.</param>
/// <param name="ParentId">Nearest logical component ancestor, or null for a root.</param>
/// <param name="RootId">Owning root component identity.</param>
/// <param name="TypeName">Qualified runtime type name for display, never instance identity.</param>
/// <param name="Key">Safe scalar key text or a type placeholder for an unknown key.</param>
/// <param name="IsFramework">Whether the component belongs to Nazor.Core or Nazor.Controls.</param>
/// <param name="DeclaringComponentId">Known lexical declaring instance, independently of logical parentage.</param>
/// <param name="Children">Immediate logical component children in retained order.</param>
/// <param name="OutputNodes">Top-level physical outputs, shared with nonvisual wrappers when applicable.</param>
public sealed record ComponentNodeInfo(int Id, int? ParentId, int RootId, string TypeName, string? Key,
    bool IsFramework, int? DeclaringComponentId, IReadOnlyList<int> Children, IReadOnlyList<int> OutputNodes)
{
    /// <summary>Last directly supplied parameter values, safely formatted without reading component getters; excludes defaults and cascading values.</summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

/// <summary>Detached structural element/text data; no style/layout collection or user property evaluation occurs.</summary>
/// <param name="Id">Session identity of the retained physical node.</param>
/// <param name="ParentId">Nearest physical element parent, or null at a root output.</param>
/// <param name="RootId">Owning root component identity.</param>
/// <param name="Kind">Element or text.</param>
/// <param name="Name">Element name or #text.</param>
/// <param name="Text">Copied text content, otherwise null.</param>
/// <param name="Key">Safe element key display, otherwise null.</param>
/// <param name="ProducingComponentId">Component instance that produced the output.</param>
/// <param name="DeclaringComponentId">Known lexical declaring instance, otherwise null.</param>
/// <param name="IsRendered">Visibility when styles are current; null means unavailable, never an empty subtree.</param>
/// <param name="Children">Physical children in retained order.</param>
public sealed record StructuralNodeInfo(int Id, int? ParentId, int RootId, NodeKind Kind, string Name, string? Text,
    string? Key, int? ProducingComponentId, int? DeclaringComponentId, bool? IsRendered, IReadOnlyList<int> Children)
{
    /// <summary>Copied markup identifier, independently of the diagnostic instance identity.</summary>
    public string? ElementId { get; init; }
    /// <summary>Safely formatted retained attributes, excluding event callbacks; null for text nodes.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
    /// <summary>Copied class names, or null for text nodes.</summary>
    public IReadOnlyList<string>? Classes { get; init; }
    /// <summary>Copied interaction-state names, or null for text nodes.</summary>
    public IReadOnlyList<string>? States { get; init; }
    /// <summary>Event attribute names without callback instances, or null for text nodes.</summary>
    public IReadOnlyList<string>? Events { get; init; }
}

/// <summary>A complete immutable-by-collection snapshot, usable without further access to the dispatcher or renderer.</summary>
/// <param name="SessionId">Opaque identity of the diagnostics attachment.</param>
/// <param name="Revision">Renderer-batch revision at capture.</param>
/// <param name="CapturedAtUtc">UTC capture time.</param>
/// <param name="ComponentRoots">Root component identities in host order.</param>
/// <param name="ElementRoots">Root physical outputs in host order.</param>
/// <param name="Components">All component instances.</param>
/// <param name="Nodes">All physical elements/texts, including hidden nodes.</param>
public sealed record DiagnosticTreeSnapshot(string SessionId, long Revision, DateTimeOffset CapturedAtUtc,
    IReadOnlyList<int> ComponentRoots, IReadOnlyList<int> ElementRoots,
    IReadOnlyList<ComponentNodeInfo> Components, IReadOnlyList<StructuralNodeInfo> Nodes);

/// <summary>A structural capture exceeded its fixed work/data budget; no partial snapshot is returned.</summary>
public sealed class TreeCaptureLimitException : InvalidOperationException
{
    internal TreeCaptureLimitException() : base("The complete tree exceeds the diagnostic capture budget.") { }
}
