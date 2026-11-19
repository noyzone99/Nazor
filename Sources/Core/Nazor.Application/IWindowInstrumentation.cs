using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;

namespace Nazor;

/// <summary>
/// Optional window pipeline integration. The window invokes every member on its UI dispatcher and disposes the
/// integration when it is detached, replaced or the window is disposed. Without an integration no frame metrics
/// are collected and input follows the ordinary application path.
/// </summary>
public interface IWindowInstrumentation : IDisposable
{
    /// <summary>Notifies the integration when its native window is available, including attachment after creation.</summary>
    /// <param name="window">The native window, accessed on its UI thread.</param>
    void NativeWindowCreated(IPlatformWindow window) { }

    /// <summary>Whether wheel and double-click input is consumed before reaching application controls.</summary>
    bool InterceptsPointer { get; }

    /// <summary>A pointer cursor override, or null to use the application's cursor.</summary>
    PlatformCursor? PointerCursor { get; }

    /// <summary>Processes pointer movement; returns true to consume it.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    bool PointerMoved(Point point);

    /// <summary>Processes a pointer press; returns true to consume it.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pressed button.</param>
    bool PointerPressed(Point point, PointerButton button);

    /// <summary>Processes a pointer release; returns true to consume it.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The released button.</param>
    bool PointerReleased(Point point, PointerButton button);

    /// <summary>Processes the pointer leaving the window; returns true to consume it.</summary>
    bool PointerLeft();

    /// <summary>Cancels interception before the input manager cancels application pointer capture.</summary>
    void PointerCanceled();

    /// <summary>Processes a key press before application commands; returns true to consume it.</summary>
    /// <param name="key">The normalized key name.</param>
    bool KeyDown(string key);

    /// <summary>Processes a key press with modifiers and repeat information before application commands.</summary>
    /// <param name="key">The normalized key name.</param>
    /// <param name="modifiers">The active key modifiers.</param>
    /// <param name="repeat">Whether this is an automatic repeat.</param>
    /// <returns>True to consume the key; the default preserves the existing key interception.</returns>
    bool KeyDown(string key, KeyModifiers modifiers, bool repeat) => KeyDown(key);

    /// <summary>Processes a key release; returns true to consume it.</summary>
    /// <param name="key">The normalized key name.</param>
    bool KeyUp(string key);

    /// <summary>Appends overlay commands before damage calculation and rasterization.</summary>
    /// <param name="list">The completed application display list. Returned commands participate in damage tracking.</param>
    DisplayList Compose(DisplayList list);

    /// <summary>Receives metrics after UI drawing/recording and window bookkeeping, before backend completion.</summary>
    /// <param name="frame">The completed frame. Scene counters are captured before overlay/accessibility queries.</param>
    void RecordFrame(WindowFrameMetrics frame);
}
