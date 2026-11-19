namespace Nazor;

/// <summary>How the window's client surface is composited.</summary>
public enum BackgroundMode
{
    /// <summary>Uses the backend's normal surface and honors the requested system backdrop.</summary>
    Default,

    /// <summary>Composites the application's per-pixel alpha with the desktop.</summary>
    Transparent,
}

/// <summary>Independent requested operations; effective support is reported by WindowCapabilities.</summary>
/// <param name="IsMinimizable">The can minimize (bool).</param>
/// <param name="IsMaximizable">The can maximize (bool).</param>
/// <param name="IsResizable">The can resize (bool).</param>
/// <param name="FullScreenEnabled">The can full screen (bool).</param>
public readonly record struct WindowOperations(bool IsMinimizable, bool IsMaximizable, bool IsResizable, bool FullScreenEnabled)
{
    /// <summary>
    /// Gets the standard operation defaults, with minimize, maximize, resize, and full screen enabled.
    /// </summary>
    public static WindowOperations Default => new(true, true, true, true);

    /// <summary>Gets the default operation policy for a dialog window.</summary>
    public static WindowOperations Dialog => new(false, false, false, true);
}

/// <summary>Preferred native caption-button height; the platform reports the actual logical size.</summary>
public enum WindowsTitleBarHeight
{
    /// <summary>Uses the standard native caption-button height.</summary>
    Standard,

    /// <summary>Uses the taller native caption-button height.</summary>
    Tall,
}

/// <summary>Effective operations and native decoration support of this window's backend.</summary>
/// <param name="CanMinimize">The can minimize (bool).</param>
/// <param name="CanMaximize">The can maximize (bool).</param>
/// <param name="CanResize">The can resize (bool).</param>
/// <param name="CanEnterFullScreen">The can full screen (bool).</param>
public readonly record struct WindowCapabilities(bool CanMinimize, bool CanMaximize, bool CanResize,
    bool CanEnterFullScreen)
{
    private readonly WindowDecorationCapabilities? decorations;
    /// <summary>Complete decoration support, independent of application templates and window-operation restrictions.</summary>
    public WindowDecorationCapabilities Decorations
    {
        get => decorations ?? WindowDecorationCapabilities.None;
        init => decorations = value;
    }
    private readonly WindowOperationSupport? minimize, maximize, resize, fullScreen;

    /// <summary>Whether and how the backend supports minimization, independently of app policy.</summary>
    public WindowOperationSupport Minimize { get => minimize ?? new(CanMinimize); init => minimize = value; }

    /// <summary>Whether and how the backend supports maximization, independently of app policy.</summary>
    public WindowOperationSupport Maximize { get => maximize ?? new(CanMaximize); init => maximize = value; }

    /// <summary>Whether and how the backend supports interactive resizing, independently of app policy.</summary>
    public WindowOperationSupport InteractiveResize { get => resize ?? new(CanResize); init => resize = value; }

    /// <summary>Whether and how the backend supports fullscreen, independently of app policy.</summary>
    public WindowOperationSupport FullScreen { get => fullScreen ?? new(CanEnterFullScreen); init => fullScreen = value; }

    /// <summary>Whether the backend can report that a window is minimized.</summary>
    public bool CanObserveMinimized { get; init; }

    /// <summary>Whether the backend can restore a minimized window by request.</summary>
    public bool CanRestoreFromMinimized { get; init; }

    /// <summary>Whether the backend supports absolute screen positioning.</summary>
    public bool CanSetScreenPosition { get; init; }

    /// <summary>Whether the backend can apply an icon to an individual native window.</summary>
    public bool CanSetWindowIcon { get; init; }
    /// <summary>Whether the backend can keep a window above non-topmost windows.</summary>
    public bool CanSetAlwaysOnTop { get; init; }
    /// <summary>Whether the backend supports a client surface with per-pixel alpha.</summary>
    public bool CanUseTransparentBackground { get; init; }

    /// <summary>Gets whether the backend can show anchored popups outside this window.</summary>
    public bool CanShowPopupsOutsideWindow { get; init; }
    /// <summary>Whether the backend can change a window's taskbar participation.</summary>
    public bool CanSetTaskbarVisibility { get; init; }
}

/// <summary>Occupied native control areas in logical units, zero when hidden or unsupported.</summary>
/// <param name="LeftInset">The left inset (float).</param>
/// <param name="RightInset">The right inset (float).</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct WindowButtonsMetrics(float LeftInset, float RightInset, float Height)
{
    /// <summary>Gets the total logical width reserved by the left and right caption-control insets.</summary>
    public float Width => LeftInset + RightInset;
}
