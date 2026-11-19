using Nazor.Rendering;

namespace Nazor;

/// <summary>Allows a component to schedule its rendering on the renderer that owns it.</summary>
public readonly struct RenderHandle
{
    private readonly Renderer? renderer;
    private readonly int componentId;

    internal RenderHandle(Renderer renderer, int componentId)
    {
        this.renderer = renderer;
        this.componentId = componentId;
    }

    /// <summary>Gets whether the render handle has been attached to a renderer.</summary>
    public bool IsInitialized => renderer is not null;

    /// <summary>Gets the UI dispatcher associated with this renderer or window.</summary>
    public Dispatcher Dispatcher => (renderer ?? throw NotInitialized()).Dispatcher;

    internal bool IsRenderingOnMetadataUpdate => renderer?.IsRenderingOnMetadataUpdate ?? false;

    internal void OpenDelegatedComponent<TComponent>(RenderTreeBuilder builder, int sequence) where TComponent : notnull, IComponent
    {
        var declaration = (renderer ?? throw NotInitialized()).ComponentDeclaration(componentId);
        builder.OpenComponent<TComponent>(sequence, declaration.Scope, declaration.Component);
    }
    internal void SetWindowTitleBar(RenderFragment? content) =>
        (renderer ?? throw NotInitialized()).SetWindowTitleBar(componentId, content);

    internal void SetWindowCaptionButtons(RenderFragment? left, RenderFragment? right) =>
        (renderer ?? throw NotInitialized()).SetWindowCaptionButtons(componentId, left, right);

    /// <summary>Queues a render of the component; requests for a disposed component are ignored.</summary>
    /// <param name="renderFragment">The fragment that writes the component's render output.</param>
    public void Render(RenderFragment renderFragment) =>
        (renderer ?? throw NotInitialized()).AddToRenderQueue(componentId, renderFragment);

    private static InvalidOperationException NotInitialized() =>
        new("The render handle is not yet assigned: the component has not been attached to a renderer.");
}
