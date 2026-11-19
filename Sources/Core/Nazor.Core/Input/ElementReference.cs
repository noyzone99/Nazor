using Nazor.Rendering;

namespace Nazor;

/// <summary>An element retained by the renderer. Interaction is supplied by the window's input manager.</summary>
/// <param name="element">The element to inspect or update.</param>
public readonly struct ElementReference(ElementNode element)
{
    /// <summary>Gets the retained element associated with this control or element reference.</summary>
    public ElementNode? Element { get; } = element;

    /// <summary>Requests keyboard focus for the target element, optionally preserving scroll offsets.</summary>
    /// <param name="preventScroll">Whether focusing should preserve the current scroll position.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task FocusAsync(bool preventScroll = false) => Interaction.FocusAsync(Element!, preventScroll);

    /// <summary>Captures pointer input for the target element until capture is released or canceled.</summary>
    /// <returns>true if pointer capture was acquired; otherwise, false.</returns>
    public bool CapturePointer() => Interaction.CapturePointer(Element!);

    /// <summary>Gets whether this element currently owns pointer capture; false when it is detached or capture was canceled.</summary>
    public bool IsPointerCaptured => Element is { IsRemoved: false } node &&
        node.GetFeature<IElementInteraction>() is { } input && input.HasPointerCapture(node);

    /// <summary>Releases pointer capture held by the target element.</summary>
    public void ReleasePointerCapture() => Interaction.ReleasePointerCapture(Element!);

    /// <summary>
    /// Scrolls a scroll container to an offset (null keeps that axis), within its limits; <c>onscroll</c> follows.
    /// False when nothing moved, or when no window shows the element yet.
    /// </summary>
    /// <param name="left">The requested horizontal scroll offset, or null to keep it unchanged.</param>
    /// <param name="top">The requested vertical scroll offset, or null to keep it unchanged.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollTo(double? left, double? top) =>
        Element is { IsRemoved: false } node && node.GetFeature<IElementInteraction>() is { } input && input.ScrollTo(node, left, top);

    /// <summary>
    /// Gets the element's border box in logical window coordinates, the space of pointer event positions, as last laid
    /// out; null before the first layout or when no window shows the element. Two-dimensional pickers use it to map a
    /// pointer position to a value.
    /// </summary>
    /// <returns>The left and top edges, the width and the height, or null.</returns>
    public (double X, double Y, double Width, double Height)? GetBounds() =>
        Element is { IsRemoved: false } node && node.GetFeature<IElementInteraction>() is { } input ? input.GetBounds(node) : null;

    /// <summary>Converts a logical window point to logical coordinates relative to this element's untransformed border-box origin, undoing ancestor scrolling and transforms.</summary>
    /// <param name="windowX">The horizontal logical window coordinate.</param>
    /// <param name="windowY">The vertical logical window coordinate.</param>
    /// <returns>The local point, or null for nonfinite coordinates, a detached/hidden/unlaid-out element, a singular transform or an interaction provider without conversion support.</returns>
    public (double X, double Y)? GetLocalPoint(double windowX, double windowY) =>
        double.IsFinite(windowX) && double.IsFinite(windowY) && Element is { IsRemoved: false } node &&
        node.GetFeature<IElementInteraction>() is { } input ? input.GetLocalPoint(node, windowX, windowY) : null;

    /// <summary>
    /// Scrolls every scroll container around the element so that it becomes visible; <c>onscroll</c> follows. False
    /// when nothing moved, or when no window shows the element yet.
    /// </summary>
    /// <returns>true when a container scrolled; otherwise, false.</returns>
    public bool ScrollIntoView() =>
        Element is { IsRemoved: false } node && node.GetFeature<IElementInteraction>() is { } input && input.BringIntoView(node);

    private IElementInteraction Interaction => Element is { IsRemoved: false } node && node.GetFeature<IElementInteraction>() is { } input
        ? input : throw new InvalidOperationException("The element is not attached to an active input manager.");
}

/// <summary>
/// Window interaction implemented by Nazor.Input; keeps the component runtime platform independent.
/// </summary>
public interface IElementInteraction
{
    /// <summary>Requests keyboard focus for the target element, optionally preserving scroll offsets.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="preventScroll">Whether focusing should preserve the current scroll position.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task FocusAsync(ElementNode element, bool preventScroll);

    /// <summary>Captures pointer input for the target element until capture is released or canceled.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if pointer capture was acquired; otherwise, false.</returns>
    bool CapturePointer(ElementNode element);

    /// <summary>Gets whether the supplied element currently owns pointer capture.</summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>true while the element owns capture; false otherwise or when the interaction provider does not expose capture state.</returns>
    bool HasPointerCapture(ElementNode element) => false;

    /// <summary>Releases pointer capture held by the target element.</summary>
    /// <param name="element">The element to inspect or update.</param>
    void ReleasePointerCapture(ElementNode element);

    /// <summary>Requests scrolling of the element to the supplied logical offsets.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="left">The requested horizontal scroll offset, or null to keep it unchanged.</param>
    /// <param name="top">The requested vertical scroll offset, or null to keep it unchanged.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool ScrollTo(ElementNode element, double? left, double? top);

    /// <summary>Gets the element's border box in logical window coordinates, or null when it is not laid out.</summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>The left and top edges, the width and the height, or null.</returns>
    (double X, double Y, double Width, double Height)? GetBounds(ElementNode element) => null;

    /// <summary>Converts a logical window point to coordinates relative to the element's untransformed border-box origin, accounting for scrolling and transforms.</summary>
    /// <param name="element">The element whose local coordinate space receives the point.</param>
    /// <param name="windowX">The horizontal logical window coordinate.</param>
    /// <param name="windowY">The vertical logical window coordinate.</param>
    /// <returns>The local point, or null for invalid coordinates, detached/hidden/unlaid-out elements, singular transforms or unavailable support. The default implementation returns null.</returns>
    (double X, double Y)? GetLocalPoint(ElementNode element, double windowX, double windowY) => null;

    /// <summary>Brings an element inside the visible area of its scrolling ancestors.</summary>
    /// <param name="element">The element to bring into view.</param>
    /// <returns>true when a container scrolled; otherwise, false.</returns>
    bool BringIntoView(ElementNode element);
}
