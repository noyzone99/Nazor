namespace Nazor.Rendering;

/// <summary>Identifies the kind of data stored in a render-tree frame.</summary>
public enum RenderTreeFrameType : byte
{
    /// <summary>An uninitialized or empty frame.</summary>
    None,

    /// <summary>The opening frame of a retained element.</summary>
    Element,

    /// <summary>A frame containing text content.</summary>
    Text,

    /// <summary>An element attribute or component parameter frame.</summary>
    Attribute,

    /// <summary>The opening frame of a component instance.</summary>
    Component,

    /// <summary>A region with an independent sequence-number scope.</summary>
    Region,

    /// <summary>A callback frame that captures a component instance.</summary>
    ComponentReferenceCapture,

    /// <summary>A callback frame that captures an element reference.</summary>
    ElementReferenceCapture,
}

/// <summary>
/// One entry of a component's render output. Elements, components and regions own the <see cref="SubtreeLength" /> -
/// 1 frames that follow them: attributes first, then children. Sequence numbers come from source positions and
/// identify frames across renders.
/// </summary>
public struct RenderTreeFrame
{
    /// <summary>Gets the callback used to capture the rendered element reference.</summary>
    public Action<ElementReference>? ElementReferenceCaptureAction { get; internal set; }

    /// <summary>Gets the source-order sequence number used to match frames across renders.</summary>
    public int Sequence { get; internal set; }

    /// <summary>Gets the kind of render frame represented by this value.</summary>
    public RenderTreeFrameType FrameType { get; internal set; }

    /// <summary>For elements, components and regions: this frame plus all descendants.</summary>
    public int SubtreeLength { get; internal set; }

    /// <summary>Element name or attribute name.</summary>
    public string? Name { get; internal set; }

    /// <summary>Gets the text carried by a text frame.</summary>
    public string? TextContent { get; internal set; }

    /// <summary>Gets the value carried by an attribute frame.</summary>
    public object? AttributeValue { get; internal set; }

    /// <summary>Gets the component type instantiated by a component frame.</summary>
    public Type? ComponentType { get; internal set; }

    /// <summary>The component whose markup declared this component frame; it scopes styles.</summary>
    public Type? DeclaringScope { get; internal set; }

    /// <summary>Gets the component instance whose markup declares the content.</summary>
    public IComponent? DeclaringComponent { get; internal set; }

    /// <summary>Gets the key used to retain identity across render updates.</summary>
    public object? Key { get; internal set; }

    /// <summary>Gets the callback used to capture the instantiated component.</summary>
    public Action<object>? ComponentReferenceCaptureAction { get; internal set; }

    internal Node? Node;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override readonly string ToString() => FrameType switch
    {
        RenderTreeFrameType.Element => $"{Sequence}: Element {Name} [{SubtreeLength}]",
        RenderTreeFrameType.Text => $"{Sequence}: Text \"{TextContent}\"",
        RenderTreeFrameType.Attribute => $"{Sequence}: Attribute {Name}={AttributeValue}",
        RenderTreeFrameType.Component => $"{Sequence}: Component {ComponentType?.Name} [{SubtreeLength}]",
        RenderTreeFrameType.Region => $"{Sequence}: Region [{SubtreeLength}]",
        _ => $"{Sequence}: {FrameType}",
    };
}
