namespace Nazor;

/// <summary>How a window is shown.</summary>
public enum WindowState
{
    /// <summary>Shows the window at its normal restored bounds.</summary>
    Normal,

    /// <summary>Expands the window to the screen work area.</summary>
    Maximized,

    /// <summary>Minimizes the window.</summary>
    Minimized,

    /// <summary>Uses the full screen without normal window decorations.</summary>
    FullScreen,
}

/// <summary>
/// A window of the application: the one a component lives in (<c>[Inject] IWindow</c>), or one opened with <c>IWindowService</c>.
/// </summary>
public interface IWindow
{
    /// <summary>Gets the borrowed native identifier on the UI dispatcher, or null before creation, after closing, or on unsupported backends. Do not cache it beyond the window lifetime or destroy it.</summary>
    NativeWindowHandle? NativeHandle => null;
    /// <summary>Opening decoration policy. Change an open window with SetDecorationsAsync instead.</summary>
    WindowDecorationPolicy Decorations { get => WindowDecorationPolicy.Default; set => throw new NotSupportedException(); }
    /// <summary>The accepted configuration and selected alternative, or null while opening negotiation is pending.</summary>
    WindowDecorationResult? AppliedDecorations => null;
    /// <summary>Applies a complete policy on the UI thread without replacing the native window.</summary>
    /// <param name="policy">The ordered configurations, including desktop defaults when created with Prefer.</param>
    /// <param name="cancellationToken">Cancels before native dispatch; dispatched negotiation settles before completion.</param>
    /// <returns>The accepted configuration. Throws PlatformNotSupportedException when every option in the policy is unsupported.</returns>
    ValueTask<WindowDecorationResult> SetDecorationsAsync(WindowDecorationPolicy policy, CancellationToken cancellationToken = default) =>
        ValueTask.FromException<WindowDecorationResult>(new PlatformNotSupportedException("Decoration changes are unavailable."));
    /// <summary>Space-separated NSS classes on the implicit window root.</summary>
    string? Class { get => null; set => throw new NotSupportedException(); }
    /// <summary>Inline NSS declarations on the implicit window root.</summary>
    string? Style { get => null; set => throw new NotSupportedException(); }
    /// <summary>Window-local control density. Automatic inherits the application choice and then the host's input preference. An explicit compact or touch choice wins over later application or host changes.</summary>
    ControlDensity ControlDensity { get => ControlDensity.Automatic; set => throw new NotSupportedException(); }
    /// <summary>Requested logical client width, or the normal width to restore in a non-normal state.</summary>
    double Width { get => 800; set => throw new NotSupportedException(); }
    /// <summary>Requested logical client height, or the normal height to restore in a non-normal state.</summary>
    double Height { get => 600; set => throw new NotSupportedException(); }
    /// <summary>Minimum logical client width; null uses the system minimum.</summary>
    double? MinWidth { get => null; set => throw new NotSupportedException(); }
    /// <summary>Minimum logical client height; null uses the system minimum.</summary>
    double? MinHeight { get => null; set => throw new NotSupportedException(); }
    /// <summary>Maximum logical client width, including when maximized; null removes the limit.</summary>
    double? MaxWidth { get => null; set => throw new NotSupportedException(); }
    /// <summary>Maximum logical client height, including when maximized; null removes the limit.</summary>
    double? MaxHeight { get => null; set => throw new NotSupportedException(); }
    /// <summary>Outer window left edge in screen pixels; null before creation lets the system position it. Non-normal states update the restore position. Unsupported backends throw on assignment.</summary>
    int? X { get => null; set => throw new PlatformNotSupportedException(); }
    /// <summary>Outer window top edge in screen pixels; null before creation lets the system position it. Non-normal states update the restore position. Unsupported backends throw on assignment.</summary>
    int? Y { get => null; set => throw new PlatformNotSupportedException(); }
    /// <summary>Requests a position above non-topmost windows without changing focus.</summary>
    bool IsAlwaysOnTop { get => false; set => throw new PlatformNotSupportedException(); }

    /// <summary>Opening setting for the client surface. Transparent mode takes precedence over the system backdrop.</summary>
    BackgroundMode BackgroundMode
    {
        get => BackgroundMode.Default;
        set { if (value != BackgroundMode.Default) throw new PlatformNotSupportedException(); }
    }
    /// <summary>Requests taskbar participation; hiding may also exclude the window from the task switcher.</summary>
    bool IsVisibleInTaskbar { get => true; set => throw new PlatformNotSupportedException(); }
    /// <summary>Requested portable or platform-specific backdrop selection.</summary>
    BackdropSelection Backdrop { get => default; set => throw new NotSupportedException(); }
    /// <summary>The backdrop accepted by the backend.</summary>
    BackdropResult AppliedBackdrop => default;
    /// <summary>Window-local stylesheet, independent of application and component styles.</summary>
    IStyleSheet? StyleSheet { get => null; set => throw new NotSupportedException(); }
    /// <summary>Placement persistence key; may only be changed before native creation.</summary>
    string? PlacementKey { get => null; set => throw new NotSupportedException(); }
    /// <summary>The explicit icon or the application icon when its source can be resolved.</summary>
    ImageSource? EffectiveIcon => Icon;
    /// <summary>Raised on the UI thread when presentation, state, or capabilities change.</summary>
    event Action? Changed { add { } remove { } }
    /// <summary>
    /// The native title; changing it also invalidates the declared window's presentation.
    /// </summary>
    string Title { get; set; }

    /// <summary>The requested native window icon; null restores the application icon. See WindowCapabilities for support.</summary>
    ImageSource? Icon { get => null; set { } }

    /// <summary>True once the window is closed: its components are disposed.</summary>
    bool IsClosed { get; }

    /// <summary>Shows a hidden window, preserving its components and services.</summary>
    /// <param name="activate">Whether showing the window should also activate it.</param>
    void Show(bool activate = true);

    /// <summary>Hides without closing or ending the application.</summary>
    void Hide();

    /// <summary>Normal, maximized, minimized or full screen.</summary>
    WindowState State { get; }

    /// <summary>Safe areas and keyboard occlusion not already removed from the client viewport.</summary>
    WindowEnvironment Environment => default;

    /// <summary>Occurs when environment changes.</summary>
    event Action? EnvironmentChanged { add { } remove { } }

    /// <summary>The application's rules for window commands and interactive resize.</summary>
    WindowOperations Operations
    {
        get => new(IsMinimizable, IsMaximizable, IsResizable, FullScreenEnabled);
        set
        {
            IsMinimizable = value.IsMinimizable;
            IsMaximizable = value.IsMaximizable;
            IsResizable = value.IsResizable;
            FullScreenEnabled = value.FullScreenEnabled;
        }
    }

    /// <summary>The menu used for title bar clicks that the backend can handle.</summary>
    WindowTitleBarMenuMode TitleBarMenuMode
    {
        get => WindowTitleBarMenuMode.System;
        set { if (value != WindowTitleBarMenuMode.System) throw new PlatformNotSupportedException(); }
    }

    /// <summary>The effective owner of the frame, its buttons and its context menu.</summary>
    WindowChrome Chrome => default;

    /// <summary>Raised when the effective frame, caption buttons or title bar menu changes owner.</summary>
    event Action? ChromeChanged { add { } remove { } }

    /// <summary>Whether the command can be sent with the current policy, state and backend support.</summary>
    bool CanExecute(WindowCommand command) => false;

    /// <summary>Checks and dispatches a command without implying that the desktop applied it.</summary>
    WindowCommandResult Request(WindowCommand command) => WindowCommandResult.Unsupported;

    /// <summary>Whether the app allows minimization.</summary>
    bool IsMinimizable { get => true; set { } }

    /// <summary>Gets or sets whether the application allows the window to be maximized.</summary>
    bool IsMaximizable { get => true; set { } }

    /// <summary>Gets or sets whether the application allows interactive window resizing.</summary>
    bool IsResizable { get => true; set { } }

    /// <summary>Gets or sets whether the application allows entry into full screen.</summary>
    bool FullScreenEnabled { get => true; set { } }

    /// <summary>Operations allowed by the application and backend; unsupported decorations are false.</summary>
    WindowCapabilities Capabilities => default;

    /// <summary>
    /// Gets or sets whether anchored popups may extend beyond the window. Defaults to false. The UI dispatcher applies
    /// changes to open popups on the next frame. Backends without <see cref="WindowCapabilities.CanShowPopupsOutsideWindow" />
    /// retain the in-window overlay.
    /// </summary>
    bool AllowPopupsOutsideWindow { get => false; set { } }

    /// <summary>Raised on the UI thread after effective operations or decoration support changes.</summary>
    event Action? CapabilitiesChanged { add { } remove { } }

    /// <summary>
    /// System control placement for custom title bars, available even when native controls are hidden.
    /// </summary>
    WindowButtonsLayout SystemButtonsLayout => WindowButtonsLayout.Default;

    /// <summary>The native caption-button area in logical units; zero when hidden or unsupported.</summary>
    WindowButtonsMetrics SystemButtonsMetrics => default;

    /// <summary>Gets the total logical width and height reserved for integrated native caption controls.</summary>
    (float Width, float Height) SystemButtonsSize => (SystemButtonsMetrics.Width, SystemButtonsMetrics.Height);

    /// <summary>Raised on the UI thread after system control metrics or layout changed.</summary>
    event Action? SystemButtonsChanged { add { } remove { } }

    /// <summary>Brings the window to the front and gives it the keyboard focus.</summary>
    void Activate();

    /// <summary>Requests that the window enter the minimized state.</summary>
    void Minimize();

    /// <summary>Requests that the window enter the maximized state.</summary>
    void Maximize();

    /// <summary>Requests native full screen; the backend reports the actual state.</summary>
    void EnterFullScreen();

    /// <summary>
    /// Restore the bounds and normal/maximized state saved before full screen. Otherwise no effect.
    /// </summary>
    void ExitFullScreen();

    /// <summary>Exit full screen to the saved state, or restore normally from maximized/minimized.</summary>
    void Restore();

    /// <summary>Closes the window.</summary>
    void Close();

    /// <summary>
    /// Closes the window; a dialog completes its <c>ShowDialogAsync</c> with <paramref name="result" />.
    /// </summary>
    /// <param name="result">The value returned to the caller awaiting the dialog.</param>
    void Close(object? result);

    /// <summary>Raised on the UI thread after <see cref="State" /> changed (by the user or by the code).</summary>
    event Action? StateChanged;

    /// <summary>Raised on the UI thread after the window closed and its components were disposed.</summary>
    event Action? Closed;
}
