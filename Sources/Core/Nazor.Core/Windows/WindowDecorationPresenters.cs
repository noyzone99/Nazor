using Nazor.Rendering;

namespace Nazor;

/// <summary>Context supplied to a statically registered theme decoration presenter.</summary>
/// <param name="Window">The owning window.</param>
/// <param name="Owner">The declaring window component, when present.</param>
/// <param name="IsLeft">Whether a button presenter renders the physical left-side group.</param>
public sealed record WindowDecorationContext(IWindow Window, WindowComponentBase? Owner, bool IsLeft = false)
{
    /// <summary>Opens a presenter component in the owning window's declaring scope, preserving local surface styling.</summary>
    /// <typeparam name="TComponent">The statically referenced component type.</typeparam>
    /// <param name="builder">The render destination.</param>
    /// <param name="sequence">The source-order sequence.</param>
    public void OpenComponent<TComponent>(RenderTreeBuilder builder, int sequence) where TComponent : notnull, IComponent =>
        builder.OpenComponent<TComponent>(sequence, Owner?.GetType(), Owner);
}

/// <summary>Optional theme composition, separate from NSS rules and window-decoration policy.</summary>
/// <param name="TitleBar">The theme's title-bar surface; null uses the common presenter.</param>
/// <param name="CaptionButtons">The theme's physical-side caption group; null uses the common presenter.</param>
/// <remarks>Presenters render requested content only. They must not change decoration policy or select native controls based on operating-system identity.</remarks>
public sealed record WindowDecorationPresenters(
    RenderFragment<WindowDecorationContext>? TitleBar = null,
    RenderFragment<WindowDecorationContext>? CaptionButtons = null);
