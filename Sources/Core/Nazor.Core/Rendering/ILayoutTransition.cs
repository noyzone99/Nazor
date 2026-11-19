namespace Nazor.Rendering;

/// <summary>
/// A one-use snapshot of an element's visible border box in a presentation scene's logical window coordinates.
/// It retains geometry only, without retaining the source element or its component state.
/// </summary>
public sealed class LayoutTransitionOrigin
{
    internal LayoutTransitionOrigin(object coordinateSpace, double x, double y, double width, double height) =>
        (CoordinateSpace, X, Y, Width, Height) = (coordinateSpace, x, y, width, height);

    internal object CoordinateSpace { get; }
    internal bool Consumed { get; set; }

    /// <summary>Gets the visible border box's left edge.</summary>
    public double X { get; }

    /// <summary>Gets the visible border box's top edge.</summary>
    public double Y { get; }

    /// <summary>Gets the visible border box's width.</summary>
    public double Width { get; }

    /// <summary>Gets the visible border box's height.</summary>
    public double Height { get; }
}

/// <summary>Presentation-scene geometry support attached to elements that request layout transitions.</summary>
public interface ILayoutTransition
{
    /// <summary>
    /// Captures an element's visible border box for a destination in the same scene. Call on the UI thread before
    /// removing the source. Only invertible, axis-aligned ancestor transforms are supported.
    /// </summary>
    /// <param name="element">The attached layout-transition element to capture.</param>
    /// <returns>A one-use origin, or null when the element is removed, hidden or has an empty box.</returns>
    /// <exception cref="InvalidOperationException">The element or its ancestors use rotation, skew or a singular transform.</exception>
    LayoutTransitionOrigin? CaptureOrigin(ElementNode element);
}
