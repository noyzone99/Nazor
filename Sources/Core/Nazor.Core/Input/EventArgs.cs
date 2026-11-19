namespace Nazor.Input;

/// <summary>Identifies the logical pointer button associated with an event.</summary>
public enum PointerButton
{
    /// <summary>Indicates that no pointer button is associated with the event.</summary>
    None,

    /// <summary>Identifies the platform's primary activation button.</summary>
    Primary,

    /// <summary>Identifies the platform's secondary or context-menu button.</summary>
    Secondary,

    /// <summary>Identifies the middle pointer button.</summary>
    Middle,
}

/// <summary>Identifies the modifier keys held during input; values can be combined.</summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>Indicates that no modifier keys are pressed.</summary>
    None = 0,

    /// <summary>Indicates that Shift is pressed.</summary>
    Shift = 1,

    /// <summary>Indicates that Control is pressed.</summary>
    Control = 2,

    /// <summary>Indicates that Alt is pressed.</summary>
    Alt = 4,

    /// <summary>Indicates that the platform Meta or Windows key is pressed.</summary>
    Meta = 8,
}

/// <summary>Arguments of pointer events. Positions are logical units relative to the window.</summary>
public class PointerEventArgs : EventArgs
{
    /// <summary>Gets the element where the event originated; it remains unchanged while the event bubbles, or null before dispatch.</summary>
    public Rendering.ElementNode? OriginalTarget { get; internal set; }

    /// <summary>Gets or sets the horizontal pointer position in logical client coordinates.</summary>
    public double X { get; init; }

    /// <summary>Gets or sets the vertical pointer position in logical client coordinates.</summary>
    public double Y { get; init; }

    /// <summary>Gets or sets the pointer button associated with this event.</summary>
    public PointerButton Button { get; init; }

    /// <summary>Gets or sets the modifier keys pressed when the input event occurred.</summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>True when the event was produced by the keyboard or an accessibility action.</summary>
    public bool IsSynthetic { get; init; }
}

/// <summary>Arguments of keyboard events. <see cref="Key" /> uses names such as "Enter", "Tab", "a".</summary>
public class KeyboardEventArgs : EventArgs
{
    /// <summary>Gets or sets whether the component consumed this key, suppressing remaining handlers and built-in keyboard behavior. Set before awaiting asynchronous work.</summary>
    public bool Handled { get; set; }
    /// <summary>Gets or sets the normalized name of the keyboard key.</summary>
    public string Key { get; init; } = "";

    /// <summary>Gets or sets the modifier keys pressed when the input event occurred.</summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>Gets or sets whether this keyboard event is an auto-repeat.</summary>
    public bool Repeat { get; init; }
}

/// <summary>Describes a keyboard focus transition between elements.</summary>
public class FocusEventArgs : EventArgs
{
    /// <summary>Gets or sets the other element involved in the focus transition, or null when unavailable.</summary>
    public Rendering.ElementNode? RelatedTarget { get; init; }
}

/// <summary>
/// Scroll deltas in the explicit DeltaUnit: <see cref="DeltaY" /> positive away from the user, <see cref="DeltaX" />
/// positive to the right (horizontal wheel or tilt). Shift with the vertical wheel is reported as is: the default
/// action scrolls horizontally.
/// </summary>
public class WheelEventArgs : PointerEventArgs
{
    /// <summary>Gets or sets whether the component consumed this gesture, suppressing remaining handlers and built-in scrolling. Set before awaiting asynchronous work.</summary>
    public bool Handled { get; set; }

    /// <summary>Gets or sets the horizontal wheel or precision-scroll delta in the specified unit.</summary>
    public double DeltaX { get; init; }

    /// <summary>Gets or sets the vertical wheel or precision-scroll delta in the specified unit.</summary>
    public double DeltaY { get; init; }

    /// <summary>Gets or sets the unit used by the horizontal and vertical scroll deltas.</summary>
    public ScrollDeltaUnit DeltaUnit { get; init; }
}

/// <summary>
/// A scroll container moved, or its viewport or content changed size (after a layout): offsets and sizes in logical
/// units, as <c>scrollTop</c>, <c>clientHeight</c> and <c>scrollHeight</c> in the DOM.
/// </summary>
public class ScrollEventArgs : EventArgs
{
    /// <summary>Gets the element where this scroll event originated; it remains unchanged through bubbling or forwarding, or null before routing.</summary>
    public Rendering.ElementNode? OriginalTarget { get; internal set; }

    /// <summary>Gets or sets the horizontal scroll offset in logical units.</summary>
    public double ScrollLeft { get; init; }

    /// <summary>Gets or sets the vertical scroll offset in logical units.</summary>
    public double ScrollTop { get; init; }

    /// <summary>Gets or sets the logical width of the visible scroll viewport.</summary>
    public double ViewportWidth { get; init; }

    /// <summary>Gets or sets the logical height of the visible scroll viewport.</summary>
    public double ViewportHeight { get; init; }

    /// <summary>Gets or sets the logical width of the scrollable content.</summary>
    public double ContentWidth { get; init; }

    /// <summary>Gets or sets the logical height of the scrollable content.</summary>
    public double ContentHeight { get; init; }
}

/// <summary>
/// An element listening to <c>onresize</c> was laid out with a new size or a new position within its parent element,
/// as a <c>ResizeObserver</c> in the DOM. The first layout always reports. Values are logical units before pixel-grid
/// snapping, unaffected by scrolling and transforms. Moving across pixel boundaries does not change a constant size.
/// </summary>
public class ResizeEventArgs : EventArgs
{
    /// <summary>Gets the horizontal distance of the border box from its parent element's border box.</summary>
    public double OffsetLeft { get; init; }

    /// <summary>Gets the vertical distance of the border box from its parent element's border box.</summary>
    public double OffsetTop { get; init; }

    /// <summary>Gets the width of the border box.</summary>
    public double Width { get; init; }

    /// <summary>Gets the height of the border box.</summary>
    public double Height { get; init; }

    /// <summary>Gets the width of the content box, inside the padding and the border, as <c>contentRect</c> of a DOM
    /// <c>ResizeObserver</c>; layouts that place children inside the element use it.</summary>
    public double ContentWidth { get; init; }

    /// <summary>Gets the height of the content box, inside the padding and the border.</summary>
    public double ContentHeight { get; init; }
}

/// <summary>An image was loaded and decoded; its natural size in logical units.</summary>
public class ImageLoadEventArgs : EventArgs
{
    /// <summary>Gets or sets the successfully loaded image's intrinsic width.</summary>
    public double Width { get; init; }

    /// <summary>Gets or sets the successfully loaded image's intrinsic height.</summary>
    public double Height { get; init; }
}

/// <summary>An image could not be read or decoded.</summary>
public class ImageErrorEventArgs : EventArgs
{
    /// <summary>Gets or sets the image-loading error message.</summary>
    public string Message { get; init; } = "";
}

/// <summary>Arguments of value changes raised by input controls.</summary>
public class ChangeEventArgs : EventArgs
{
    /// <summary>Gets or sets the new value reported by the input or change event.</summary>
    public object? Value { get; init; }
}
