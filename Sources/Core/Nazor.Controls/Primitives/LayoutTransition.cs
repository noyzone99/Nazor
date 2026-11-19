using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// Animates a stable subtree's layout relocation or resize using the theme's NSS <c>transition: transform ...</c>.
/// Layout reaches its destination immediately; the visual border box moves from its current position without changing
/// component identity, focus or scroll offsets. Keep collection item keys stable. The wrapper owns its transform;
/// put authored transforms on its content. Rotation, skew and singular ancestor transforms are rejected when motion is requested.
/// </summary>
public sealed class LayoutTransition : ContentControl
{
    /// <summary>Gets or sets the element name, <c>LayoutTransition</c> by default.</summary>
    [Parameter] public string As { get; set; } = "LayoutTransition";

    /// <summary>Gets or sets whether layout changes animate; disabling cancels an active relocation.</summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a captured origin for a connected destination in the same scene. Each snapshot is consumed at most
    /// once, even when reassigned. Content is supplied by the destination; no source pixels or component are copied.
    /// </summary>
    [Parameter] public LayoutTransitionOrigin? From { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Reserves the wrapper's transform and top-left transform origin.</summary>
    protected override string? ElementStyle => Css.Join(Style, "transform: none; transform-origin: 0 0");

    /// <summary>Adds the shared layout-motion marker and optional one-use origin.</summary>
    /// <param name="builder">The builder receiving the attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "layout-transition", IsEnabled);
        builder.AddAttribute(11, "layout-transition-origin", From);
    }

    /// <summary>Captures the current visible box before navigating or removing this source.</summary>
    /// <returns>A one-use origin, or null before layout or while hidden or detached.</returns>
    /// <exception cref="InvalidOperationException">The source or its ancestors use an unsupported transform.</exception>
    public LayoutTransitionOrigin? CaptureOrigin() => Element.Element is { IsRemoved: false } element
        ? element.GetFeature<ILayoutTransition>()?.CaptureOrigin(element) : null;
}
