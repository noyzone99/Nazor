namespace Nazor.Rendering;

/// <summary>
/// Collects the frames produced by one render of a component. The method set matches what the Razor component
/// compiler emits, so generated code targets it unchanged after namespace adaptation.
/// </summary>
public sealed class RenderTreeBuilder
{
    private RenderTreeFrame[] frames = new RenderTreeFrame[32];
    private int count;
    private int[] openFrames = new int[16];
    private bool[] openHasChildren = new bool[16];
    private int openCount;

    /// <summary>Gets the number of render frames stored in the builder.</summary>
    public int FrameCount => count;

    /// <summary>Gets a read-only span over the frames currently stored in the builder.</summary>
    /// <returns>A read-only span over the current render frames.</returns>
    public ReadOnlySpan<RenderTreeFrame> GetFrames() => frames.AsSpan(0, count);

    /// <summary>Removes all frames and resets the builder for reuse.</summary>
    public void Clear()
    {
        Array.Clear(frames, 0, count);
        count = 0;
        openCount = 0;
    }

    /// <summary>Starts an element frame with the supplied element name.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="elementName">The element name exposed to NSS type selectors.</param>
    public void OpenElement(int sequence, string elementName)
    {
        ArgumentException.ThrowIfNullOrEmpty(elementName);
        Open(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.Element, Name = elementName });
    }

    /// <summary>Starts an element owned by a declaration rendered at a host-managed destination.</summary>
    /// <param name="sequence">The source sequence number.</param>
    /// <param name="elementName">The retained element name.</param>
    /// <param name="owner">The component owning the element and its control reference.</param>
    public void OpenElement(int sequence, string elementName, IComponent owner)
    {
        ArgumentException.ThrowIfNullOrEmpty(elementName);
        ArgumentNullException.ThrowIfNull(owner);
        Open(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.Element, Name = elementName, DeclaringComponent = owner });
    }

    /// <summary>Completes the currently open element frame.</summary>
    public void CloseElement() => Close(RenderTreeFrameType.Element);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    public void OpenComponent<TComponent>(int sequence) where TComponent : notnull, IComponent =>
        OpenComponent(sequence, typeof(TComponent), null);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="declaringScope">The type whose markup declares the component, used for scoped selectors.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    public void OpenComponent<TComponent>(int sequence, Type? declaringScope) where TComponent : notnull, IComponent =>
        OpenComponent(sequence, typeof(TComponent), declaringScope);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="declaringScope">The type whose markup declares the component, used for scoped selectors.</param>
    /// <param name="declaringComponent">The component instance whose markup declares the content.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    public void OpenComponent<TComponent>(int sequence, Type? declaringScope, IComponent? declaringComponent) where TComponent : notnull, IComponent =>
        OpenComponent(sequence, typeof(TComponent), declaringScope, declaringComponent);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="componentType">The component type whose scoped styles or render output are being requested.</param>
    public void OpenComponent(int sequence, Type componentType) => OpenComponent(sequence, componentType, null);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="componentType">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="declaringScope">The type whose markup declares the component, used for scoped selectors.</param>
    public void OpenComponent(int sequence, Type componentType, Type? declaringScope) =>
        OpenComponent(sequence, componentType, declaringScope, null);

    /// <summary>
    /// Starts a component frame, optionally recording the declaring markup type and instance for scoped selectors.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="componentType">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="declaringScope">The type whose markup declares the component, used for scoped selectors.</param>
    /// <param name="declaringComponent">The component instance whose markup declares the content.</param>
    /// <exception cref="ArgumentNullException">componentType is null.</exception>
    public void OpenComponent(int sequence, Type componentType, Type? declaringScope, IComponent? declaringComponent)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"The type '{componentType.FullName}' does not implement {nameof(IComponent)}.", nameof(componentType));
        Open(new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.Component,
            ComponentType = componentType,
            DeclaringScope = declaringScope,
            DeclaringComponent = declaringComponent,
        });
    }

    /// <summary>Completes the currently open component frame.</summary>
    public void CloseComponent() => Close(RenderTreeFrameType.Component);

    /// <summary>Starts a region with an independent sequence-number scope.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    public void OpenRegion(int sequence) => Open(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.Region });

    /// <summary>Completes the currently open region frame.</summary>
    public void CloseRegion() => Close(RenderTreeFrameType.Region);

    /// <summary>Adds the child render frames used by RenderTreeBuilder.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="textContent">The content to append as a text frame.</param>
    public void AddContent(int sequence, string? textContent)
    {
        EnsureChildAllowed();
        Append(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.Text, TextContent = textContent ?? string.Empty, SubtreeLength = 1 });
    }

    /// <summary>Adds the child render frames used by RenderTreeBuilder.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="textContent">The content to append as a text frame.</param>
    public void AddContent(int sequence, object? textContent) => AddContent(sequence, textContent?.ToString());

    /// <summary>
    /// Renders a fragment inside a region, so its sequence numbers do not collide with the caller's.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="fragment">The render fragment to invoke.</param>
    public void AddContent(int sequence, RenderFragment? fragment)
    {
        if (fragment is null) return;
        OpenRegion(sequence);
        fragment(this);
        CloseRegion();
    }

    /// <summary>Adds the child render frames used by RenderTreeBuilder.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="fragment">The render fragment to invoke.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    public void AddContent<TValue>(int sequence, RenderFragment<TValue>? fragment, TValue value)
    {
        if (fragment is not null) AddContent(sequence, fragment(value));
    }

    /// <summary>
    /// Razor emits text as markup when it contains characters it would HTML-encode (non-ASCII letters, entities). The
    /// compiler rejects elements, so markup here is text: it is decoded into a text frame. Whitespace between
    /// controls produces no frame.
    /// </summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="markupContent">The markup content (string?).</param>
    public void AddMarkupContent(int sequence, string? markupContent)
    {
        if (string.IsNullOrWhiteSpace(markupContent)) return;
        if (markupContent.Contains('<'))
            throw new NotSupportedException("Static markup is not supported by Nazor; use controls such as TextBlock.");
        AddContent(sequence, System.Net.WebUtility.HtmlDecode(markupContent));
    }

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    public void AddAttribute(int sequence, string name) => AddAttribute(sequence, name, (object?)true);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddAttribute(int sequence, string name, bool value) => AddAttribute(sequence, name, (object?)value);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddAttribute(int sequence, string name, string? value) => AddAttribute(sequence, name, (object?)value);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddAttribute(int sequence, string name, MulticastDelegate? value) => AddAttribute(sequence, name, (object?)value);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddAttribute(int sequence, string name, EventCallback value) => AddAttribute(sequence, name, (object?)value);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="TArgument">The type of the event argument.</typeparam>
    public void AddAttribute<TArgument>(int sequence, string name, EventCallback<TArgument> value) => AddAttribute(sequence, name, (object?)value);

    /// <summary>Adds an element attribute or component parameter to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddAttribute(int sequence, string name, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var parentType = EnsureAttributeAllowed();
        if (value is null && parentType == RenderTreeFrameType.Element) return;
        Append(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.Attribute, Name = name, AttributeValue = value, SubtreeLength = 1 });
    }

    /// <summary>Adds a named parameter to the currently open component frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    public void AddComponentParameter(int sequence, string name, object? value)
    {
        if (CurrentOpenType() != RenderTreeFrameType.Component)
            throw new InvalidOperationException($"{nameof(AddComponentParameter)} can only be used inside a component frame.");
        AddAttribute(sequence, name, value);
    }

    /// <summary>Adds a sequence of attributes or component parameters to the currently open frame.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="attributes">The attributes or selector constraints to apply.</param>
    public void AddMultipleAttributes(int sequence, IEnumerable<KeyValuePair<string, object?>>? attributes)
    {
        if (attributes is null) return;
        foreach (var (name, value) in attributes) AddAttribute(sequence, name, value);
    }

    /// <summary>Sets whether the event's default behavior should be prevented.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="value">The value to use.</param>
    public void AddEventPreventDefaultAttribute(int sequence, string eventName, bool value) =>
        AddAttribute(sequence, "__internal_preventDefault_" + eventName, value);

    /// <summary>Sets whether the event should stop propagating to ancestor elements.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="value">The value to use.</param>
    public void AddEventStopPropagationAttribute(int sequence, string eventName, bool value) =>
        AddAttribute(sequence, "__internal_stopPropagation_" + eventName, value);

    /// <summary>Captures a reference to the rendered element through the supplied callback.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="capture">The callback that receives the rendered element reference.</param>
    /// <exception cref="ArgumentNullException">capture is null.</exception>
    public void AddElementReferenceCapture(int sequence, Action<ElementReference> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (CurrentOpenType() != RenderTreeFrameType.Element)
            throw new InvalidOperationException("Element references require an open element.");
        Append(new RenderTreeFrame { Sequence = sequence, FrameType = RenderTreeFrameType.ElementReferenceCapture,
            ElementReferenceCaptureAction = capture, SubtreeLength = 1 });
    }

    /// <summary>Sets the identity of the open element or component among its siblings.</summary>
    /// <param name="value">The value to use.</param>
    public void SetKey(object? value)
    {
        if (value is null) return;
        if (openCount == 0 || frames[openFrames[openCount - 1]].FrameType is not (RenderTreeFrameType.Element or RenderTreeFrameType.Component))
            throw new InvalidOperationException("@key can only be applied to a control or a component.");
        frames[openFrames[openCount - 1]].Key = value;
    }

    /// <summary>Captures the instantiated component through the supplied callback.</summary>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="componentReferenceCaptureAction">The callback that receives the instantiated component.</param>
    /// <exception cref="ArgumentNullException">componentReferenceCaptureAction is null.</exception>
    public void AddComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction)
    {
        ArgumentNullException.ThrowIfNull(componentReferenceCaptureAction);
        if (CurrentOpenType() != RenderTreeFrameType.Component)
            throw new InvalidOperationException("@ref can only be applied to a component.");
        Append(new RenderTreeFrame
        {
            Sequence = sequence,
            FrameType = RenderTreeFrameType.ComponentReferenceCapture,
            ComponentReferenceCaptureAction = componentReferenceCaptureAction,
            SubtreeLength = 1,
        });
    }

    /// <summary>Emitted by Razor for element bindings; Nazor controls bind through parameters instead.</summary>
    /// <param name="updatesAttributeName">The updates attribute name (string).</param>
    public void SetUpdatesAttributeName(string updatesAttributeName)
    {
    }

    internal RenderTreeFrame[] ToArray(RenderTreeFrame[]? buffer = null)
    {
        if (openCount != 0)
            throw new InvalidOperationException($"Render output is incomplete: {frames[openFrames[openCount - 1]].FrameType} frame was not closed.");
        if (buffer is null || buffer.Length != count) return frames.AsSpan(0, count).ToArray();
        frames.AsSpan(0, count).CopyTo(buffer);
        return buffer;
    }

    private void Open(RenderTreeFrame frame)
    {
        EnsureChildAllowed();
        if (openCount == openFrames.Length)
        {
            Array.Resize(ref openFrames, openFrames.Length * 2);
            Array.Resize(ref openHasChildren, openFrames.Length);
        }
        openFrames[openCount] = count;
        openHasChildren[openCount++] = false;
        Append(frame);
    }

    private void Close(RenderTreeFrameType type)
    {
        if (openCount == 0 || frames[openFrames[openCount - 1]].FrameType != type)
            throw new InvalidOperationException($"Close{type} does not match the open frame.");
        var index = openFrames[--openCount];
        frames[index].SubtreeLength = count - index;
    }

    private RenderTreeFrameType CurrentOpenType() => openCount == 0 ? RenderTreeFrameType.None : frames[openFrames[openCount - 1]].FrameType;

    private RenderTreeFrameType EnsureAttributeAllowed()
    {
        var parentType = CurrentOpenType();
        if (parentType is not (RenderTreeFrameType.Element or RenderTreeFrameType.Component) || openHasChildren[openCount - 1])
            throw new InvalidOperationException("Attributes may only be added immediately after an element or component frame.");
        return parentType;
    }

    private void EnsureChildAllowed()
    {
        if (CurrentOpenType() == RenderTreeFrameType.Component)
            throw new InvalidOperationException("Component frames cannot contain children; pass content through a RenderFragment parameter.");
        if (openCount > 0) openHasChildren[openCount - 1] = true;
    }

    private void Append(in RenderTreeFrame frame)
    {
        if (count == frames.Length) Array.Resize(ref frames, frames.Length * 2);
        frames[count++] = frame;
    }
}
