using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;

namespace Nazor.Platform;

/// <summary>
/// Normal window bounds with an explicit coordinate representation and optional portable client size. Missing
/// coordinate metadata preserves the interpretation of legacy Windows saves.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
/// <param name="IsMaximized">The is maximized (bool).</param>
public sealed record WindowPlacement(int X, int Y, int Width, int Height, bool IsMaximized)
{

    /// <summary>Gets or sets the coordinate representation of the saved bounds.</summary>
    public WindowPlacementCoordinates Coordinates { get; init; } = WindowPlacementCoordinates.WindowsWorkAreaPixels;

    /// <summary>Gets or sets the pixel scale at which this placement was captured.</summary>
    public float Scale { get; init; } = 1;

    /// <summary>Gets or sets the optional identifier of the display containing the saved window.</summary>
    public string? ScreenId { get; init; }

    /// <summary>Gets or sets the portable logical client size, when it is available.</summary>
    public Size? LogicalClientSize { get; init; }
}

/// <summary>What a point of a window with the content in the title bar is for the system.</summary>
public enum WindowHitTest
{
    /// <summary>Content: the host handles the pointer.</summary>
    Client,

    /// <summary>The title bar: dragging moves the window, a double click maximizes it.</summary>
    Caption,

    /// <summary>The maximize button: Windows 11 shows the snap layouts over it.</summary>
    MaximizeButton,
}

/// <summary>Where a window opens.</summary>
public enum WindowCentering
{
    /// <summary>Where the system puts new windows.</summary>
    None,

    /// <summary>Centered on its owner (on the screen without one).</summary>
    Owner,

    /// <summary>Centered on the work area of the screen.</summary>
    Screen,
}

/// <summary>Initial properties of a window; sizes are logical units of the client area.</summary>
/// <param name="Title">The title (string).</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public sealed record WindowOptions(string Title = "Nazor", double Width = 800, double Height = 600)
{
    /// <summary>The window this one belongs to: it stays above it and is minimized with it.</summary>
    public IPlatformWindow? Owner { get; init; }

    /// <summary>
    /// A dialog frame; defaults disable minimize, maximize and user resize, independently overridable.
    /// </summary>
    public bool IsDialog { get; init; }

    /// <summary>Keeps the window above non-topmost windows without activating it.</summary>
    public bool IsAlwaysOnTop { get; init; }
    /// <summary>The complete decoration configuration requested at native creation.</summary>
    public WindowDecorations Decorations { get; init; } = WindowDecorations.Native();
    /// <summary>How the client surface is composited.</summary>
    public BackgroundMode BackgroundMode { get; init; }
    /// <summary>Requests taskbar participation; false can also exclude task switching.</summary>
    public bool IsVisibleInTaskbar { get; init; } = true;

    /// <summary>Null inherits the window or dialog defaults.</summary>
    public bool? IsMinimizable { get; init; }

    /// <summary>
    /// Gets or sets the requested maximization policy; null uses the standard window or dialog default.
    /// </summary>
    public bool? IsMaximizable { get; init; }

    /// <summary>
    /// Gets or sets the requested interactive resize policy; null uses the standard window or dialog default.
    /// </summary>
    public bool? IsResizable { get; init; }

    /// <summary>
    /// Gets or sets the requested full-screen policy; null uses the standard window or dialog default.
    /// </summary>
    public bool? FullScreenEnabled { get; init; }

    /// <summary>Gets or sets how the native window is centered when first shown.</summary>
    public WindowCentering Centering { get; init; }
}

/// <summary>Identifies the native pointer cursor requested by the window host.</summary>
public enum PlatformCursor
{
    /// <summary>Displays a cursor indicating the default arrow.</summary>
    Arrow,

    /// <summary>Displays a cursor indicating a link-selection hand.</summary>
    Hand,

    /// <summary>Displays a cursor indicating a text-selection I-beam.</summary>
    IBeam,

    /// <summary>Displays a cursor indicating horizontal resizing.</summary>
    SizeWestEast,

    /// <summary>Displays a cursor indicating vertical resizing.</summary>
    SizeNorthSouth,

    /// <summary>Displays a cursor indicating movement in all directions.</summary>
    SizeAll,

    /// <summary>Displays a cursor indicating an unavailable operation.</summary>
    No,

    /// <summary>Displays a cursor indicating a busy operation.</summary>
    Wait,

    /// <summary>Displays a cursor indicating background progress.</summary>
    AppStarting,

    /// <summary>Displays a cursor indicating contextual help.</summary>
    Help,

    /// <summary>Displays a cursor indicating a precision crosshair.</summary>
    Cross,

    /// <summary>Displays a cursor indicating diagonal resizing from the top-left or bottom-right corner.</summary>
    SizeNorthwestSoutheast,

    /// <summary>Displays a cursor indicating diagonal resizing from the top-right or bottom-left corner.</summary>
    SizeNortheastSouthwest,
}

/// <summary>A backend-owned paint target: top-down BGRA premultiplied CPU pixels, or a GPU surface description.</summary>
/// <param name="Pixels">The address of the CPU pixel buffer, or zero for GPU submission.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
/// <param name="Stride">The number of bytes between the starts of successive pixel rows.</param>
/// <param name="Scale">The scale (float).</param>
public readonly record struct PaintTarget(nint Pixels, int Width, int Height, int Stride, float Scale)
{
    /// <summary>Gets the native top decoration inset in physical pixels; GPU surfaces include it in Height.</summary>
    public int TopInset { get; init; }
    /// <summary>Whether a CPU backend paints its caption above the supplied client buffer.</summary>
    public bool SeparateTopDecoration { get; init; }
    /// <summary>Gets an optional physical pixel region cleared to transparent after GPU drawing, for native caption composition.</summary>
    public PixelRect? TransparentRegion { get; init; }
    /// <summary>Whether the window system needs a complete frame even if the application scene did not change.</summary>
    public bool ForceFullRedraw { get; init; }
}

/// <summary>
/// Implemented by the Nazor side of a window; the backend calls it on the UI thread with input already normalised:
/// logical coordinates relative to the client area, DOM-like key names.
/// </summary>
public interface IWindowHost
{
    /// <summary>Whether OnPaint submits a GPU frame and does not require a CPU pixel buffer.</summary>
    bool UsesGpuRendering => false;

    /// <summary>Stops rendering resources before the backend destroys its native surface.</summary>
    void OnRenderingStopping() { }

    /// <summary>Reports software backend submission completion after OnPaint, including a no-op paint.</summary>
    /// <param name="duration">The backend's CPU submission/presentation duration.</param>
    void OnPresented(TimeSpan duration) { }

    /// <summary>Updates the host with the logical client size and physical pixel scale.</summary>
    /// <param name="logicalSize">The client-area dimensions in logical units.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    void OnResized(Size logicalSize, float scale);

    /// <summary>
    /// Draws into the target and says what changed: the rectangles to show (physical pixels), or null when the whole
    /// buffer changed. The rest of the buffer keeps the previous frame.
    /// </summary>
    /// <param name="target">The component, element, or navigation entry targeted by the operation.</param>
    /// <returns>The resulting IReadOnlyList&lt;PixelRect&gt;? value.</returns>
    IReadOnlyList<PixelRect>? OnPaint(PaintTarget target);

    /// <summary>Gets the optional border of the full client-drawn frame in logical coordinates.</summary>
    DrawBorder? GetClientFrameBorder(Size frameSize) => null;

    /// <summary>Paints a border over a CPU frame whose caption and content were drawn separately.</summary>
    void PaintClientFrameBorder(PaintTarget target, DrawBorder border) { }

    /// <summary>Processes the pointer moved notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerMoved(Point point, KeyModifiers modifiers);

    /// <summary>Processes the pointer pressed notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers);

    /// <summary>Processes the pointer released notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers);

    /// <summary>Processes the pointer left notification in logical viewport coordinates.</summary>
    void OnPointerLeft();

    /// <summary>Processes the pointer double clicked notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerDoubleClicked(Point point, PointerButton button, KeyModifiers modifiers) { }

    /// <summary>Vertical wheel rotation in notches; positive values scroll up (away from the user).</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="notches">The number of wheel detents delivered by the backend.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerWheel(Point point, float notches, KeyModifiers modifiers);

    /// <summary>The horizontal wheel (or tilt) moved by <paramref name="notches" />, positive to the right.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="notches">The number of wheel detents delivered by the backend.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers);

    /// <summary>Processes the pointer canceled notification in logical viewport coordinates.</summary>
    void OnPointerCanceled();

    /// <summary>Delivers a normalized key press and reports whether Nazor handled it.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <param name="repeat">Whether this key event is generated by auto-repeat.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat);

    /// <summary>Delivers a normalized key release and reports whether Nazor handled it.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool OnKeyUp(string key, KeyModifiers modifiers);

    /// <summary>Delivers text committed by the platform's text input system.</summary>
    /// <param name="text">The text to process.</param>
    void OnTextInput(string text);

    /// <summary>IME preedit text and caret inside it; an empty text ends the composition without inserting.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="cursor">The cursor (int).</param>
    void OnComposition(string text, int cursor);

    /// <summary>Text committed by the IME.</summary>
    /// <param name="text">The text to process.</param>
    void OnCompositionCommit(string text);

    /// <summary>Notifies the host that the native window has gained or lost keyboard focus.</summary>
    /// <param name="focused">Whether the native window has keyboard focus.</param>
    void OnFocusChanged(bool focused);

    /// <summary>The window was minimized or hidden (false), or shown again (true).</summary>
    /// <param name="visible">Whether the window, control, or native decoration should be visible.</param>
    void OnVisibilityChanged(bool visible);

    /// <summary>A system setting the host may depend on changed (for example the animation preference).</summary>
    void OnSystemSettingsChanged();

    /// <summary>Safe areas or keyboard occlusion changed; data is already relative to the client.</summary>
    void OnEnvironmentChanged() { }

    /// <summary>Processes the pointer scroll notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="deltaX">The horizontal scroll delta in the specified unit.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <param name="unit">The unit used for the scroll deltas.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    void OnPointerScroll(Point point, float deltaX, float deltaY, ScrollDeltaUnit unit, KeyModifiers modifiers) { }

    /// <summary>The user asked to close the window; return false to keep it open.</summary>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool OnClosing();

    /// <summary>What a point of the client area is, for a window with the content in the title bar.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting WindowHitTest value.</returns>
    WindowHitTest HitTest(Point point) => WindowHitTest.Client;

    /// <summary>The window was maximized, minimized or restored.</summary>
    /// <param name="state">The state (WindowState).</param>
    void OnStateChanged(WindowState state) { }
    /// <summary>Reports the window manager's effective always-on-top state.</summary>
    void OnAlwaysOnTopChanged(bool value) { }
    /// <summary>Reports the desktop's active decoration state when it is separate from keyboard focus.</summary>
    void OnActivationChanged(bool active) { }

    /// <summary>Shows the Nazor title bar menu at a point in logical viewport coordinates.</summary>
    void OnTitleBarMenuRequested(Point point) { }

    /// <summary>Notifies the host that the effective window capabilities have changed.</summary>
    void OnCapabilitiesChanged() { }

    /// <summary>The native window is gone; release what depends on it.</summary>
    void OnClosed();

    /// <summary>Gets the cursor to display at the specified client position.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting PlatformCursor value.</returns>
    PlatformCursor GetCursor(Point point);

    /// <summary>
    /// The accessible view of the window, requested when an assistive technology first asks for it; null while the
    /// window is not ready or after it closed.
    /// </summary>
    /// <returns>The resulting SemanticTree? value.</returns>
    SemanticTree? GetSemanticTree();
}

/// <summary>A native window driven by a backend.</summary>
public interface IPlatformWindow : IDisposable
{
    /// <summary>Prepares an OpenGL window context on the UI thread, or returns null when unsupported. Graphics initialization runs on the rendering thread.</summary>
    /// <returns>A context owned by the caller, disposed before the native window is destroyed.</returns>
    IOpenGlWindowContext? CreateOpenGlContext() => null;

    /// <summary>Resets backend paint scheduling after a renderer transition; called on the UI thread.</summary>
    void ResetRendering() => Invalidate();

    /// <summary>Adds native client decorations to a GPU frame without accessing rendering-thread state.</summary>
    /// <param name="content">The application content in client coordinates.</param>
    /// <param name="topInset">The top decoration inset in logical units.</param>
    /// <param name="fonts">The UI-thread font provider.</param>
    /// <param name="shaper">The UI-thread text shaper.</param>
    /// <returns>A complete window display list.</returns>
    DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper) => content;

    /// <summary>Composes the window frame with the current client background.</summary>
    DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper, Color background) =>
        ComposeFrame(content, topInset, fonts, shaper);

    /// <summary>Whether this backend accepts client frame style from the window root.</summary>
    bool UsesClientFrameAppearance => false;

    /// <summary>Whether the backend uses the default Nazor corner radius and shadow for this window.</summary>
    bool HasDefaultClientFrameAppearance => false;

    /// <summary>Whether the backend is currently showing a client-drawn frame.</summary>
    bool DrawClientFrame => false;

    /// <summary>Gets the effective client frame corner radii in logical pixels.</summary>
    CornerRadius ClientFrameRadius => default;

    /// <summary>Whether the current frame requires an alpha clear around its rounded corners.</summary>
    bool HasRoundedClientFrame => false;

    /// <summary>Applies the window root's exterior shadow and corner radius.</summary>
    void SetClientFrameAppearance(CornerRadius radius, IReadOnlyList<DrawShadow> shadows) { }

    /// <summary>Applies logical widths to the client resize zones; null uses the backend default for that zone.</summary>
    void SetFrameResizeGeometry(float? inset, float? outset, float? cornerLength) { }

    /// <summary>The platform whose UI conventions this backend implements; null when unspecified.</summary>
    PlatformKind? Platform => null;

    /// <summary>Gets the backend's internal identifier. Use NativeHandle for operating-system interop.</summary>
    nint Handle { get; }

    /// <summary>Gets the borrowed operating-system window identifier on the UI thread, or null when unavailable or closed. It may differ from Handle and must not be destroyed by callers.</summary>
    NativeWindowHandle? NativeHandle => null;

    /// <summary>Gets an opaque cross-process owner token, or null when unavailable. A token expires with this window; unlike NativeHandle it never exposes a process-local pointer to another process.</summary>
    string? ExportedHandle => null;

    /// <summary>Requests a transient owner exported by another window on the same desktop, on the UI thread. Returns false when the backend or desktop cannot accept the token. This does not transfer ownership or enforce lifetime.</summary>
    /// <param name="ownerHandle">The opaque token returned by the owner's ExportedHandle.</param>
    /// <returns>Whether the backend accepted the ownership request; desktop policy may affect its presentation.</returns>
    bool SetOwner(string ownerHandle) => false;

    /// <summary>Gets the number of physical pixels per logical unit.</summary>
    float Scale { get; }

    /// <summary>Gets the keyboard editing conventions supplied by the platform.</summary>
    TextInputPolicy TextInputPolicy => TextInputPolicy.Default;

    /// <summary>Gets the logical scroll distance per wheel detent.</summary>
    float WheelScrollDistance => 48;

    /// <summary>Gets whether a wheel detent scrolls by a viewport page rather than a fixed distance.</summary>
    bool WheelScrollByPage => false;

    /// <summary>Gets the safe-area insets and keyboard occlusion relative to the client viewport.</summary>
    WindowEnvironment Environment => default;

    /// <summary>Gets the placement representations and restore operations supported by the backend.</summary>
    WindowPlacementCapabilities PlacementCapabilities => default;

    /// <summary>Gets the size of the native client area in logical units.</summary>
    Size ClientSize { get; }

    /// <summary>Outer window position in physical screen pixels, or null when unsupported.</summary>
    (int X, int Y)? Position => null;
    /// <summary>Moves the outer frame in screen pixels; null axes preserve the current coordinate. Non-normal windows update restore bounds.</summary>
    void Move(int? x, int? y) => throw new PlatformNotSupportedException("Screen positioning is unavailable on this backend.");
    /// <summary>Sets logical client size limits; null removes the corresponding limit.</summary>
    void SetSizeLimits(double? minWidth, double? minHeight, double? maxWidth, double? maxHeight)
    {
        if (minWidth.HasValue || minHeight.HasValue || maxWidth.HasValue || maxHeight.HasValue)
            throw new PlatformNotSupportedException("Window size limits are unavailable on this backend.");
    }
    /// <summary>Applies native Windows title bar colors; ignored on other backends.</summary>
    void SetWindowsTitleBarColors(WindowsTitleBarColors colors) { }

    /// <summary>Adds a command to the native system menu when supported. Call and dispose the registration on the UI thread.</summary>
    /// <param name="text">The menu label, optionally followed by a tab and shortcut hint.</param>
    /// <param name="invoked">The callback invoked on the UI thread when the command is selected.</param>
    /// <returns>A registration that removes the command, or null when native menu customization is unavailable.</returns>
    IDisposable? AddSystemMenuCommand(string text, Action invoked) => null;

    /// <summary>Updates the style used when this window publishes the application launcher badge. Other platforms ignore it.</summary>
    /// <param name="appearance">Resolved appearance in logical pixels. Called on the UI thread.</param>
    void SetWindowsBadgeAppearance(WindowsBadgeAppearance appearance) { }

    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    string Title { get; set; }
    /// <summary>Application icon source when the backend can expose it to application content.</summary>
    ImageSource? ApplicationIcon => null;
    /// <summary>Gets the owner of the frame and the window controls.</summary>
    WindowChrome Chrome => default;

    /// <summary>Chooses a context menu for title bar clicks delivered to the backend.</summary>
    void SetTitleBarMenuMode(WindowTitleBarMenuMode value)
    {
        if (value != WindowTitleBarMenuMode.System) throw new PlatformNotSupportedException("Title bar menu selection is unavailable.");
    }


    /// <summary>Applies an exact decoration request and completes after native acceptance; unsupported requests must not substitute parts.</summary>
    /// <param name="configuration">The complete request.</param>
    /// <param name="cancellationToken">Cancels a request before dispatch or the caller's wait for native negotiation.</param>
    /// <returns>The configuration accepted by the backend.</returns>
    ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (configuration == WindowDecorations.Native()) return new(configuration);
        throw new PlatformNotSupportedException("The backend cannot apply the requested decorations.");
    }

    /// <summary>
    /// Sets candidate window icons as top-down premultiplied BGRA bitmaps. The backend copies the selected data
    /// before returning; null restores the application icon. Ignored unless CanSetWindowIcon is true.
    /// </summary>
    /// <param name="images">Candidate images at different pixel sizes, or null to reset the icon.</param>
    void SetIcon(IReadOnlyList<RenderedImage>? images) { }

    /// <summary>Gets whether the native window has closed.</summary>
    bool IsClosed { get; }

    /// <summary>Shows the window without necessarily activating it.</summary>
    /// <param name="activate">Whether showing the window should also activate it.</param>
    void Show(bool activate = true);

    /// <summary>Hides without destroying the window.</summary>
    void Hide() { }

    /// <summary>Asks the window to close (the host can still cancel).</summary>
    void Close();

    /// <summary>Brings the window to the front with the keyboard focus.</summary>
    void Activate() { }

    /// <summary>Gets the current normal, minimized, maximized, or full-screen window state.</summary>
    WindowState State => WindowState.Normal;
    /// <summary>Whether the backend reports activation independently of focus.</summary>
    bool ReportsActivationSeparately => false;

    /// <summary>
    /// System control placement, independent of native visibility; standard right-hand layout when unspecified.
    /// </summary>
    WindowButtonsLayout SystemButtonsLayout => WindowButtonsLayout.Default;

    /// <summary>
    /// Integrated native caption-button insets in logical units on a custom client title bar; zero when hidden or
    /// unsupported.
    /// </summary>
    WindowButtonsMetrics SystemButtonsMetrics => default;

    /// <summary>Gets the total logical width and height reserved for integrated native caption controls.</summary>
    (float Width, float Height) SystemButtonsSize => (SystemButtonsMetrics.Width, SystemButtonsMetrics.Height);

    /// <summary>
    /// Gets the window operations and decorations supported by the current backend and application settings.
    /// </summary>
    WindowCapabilities Capabilities => default;

    /// <summary>Updates the user operations requested for this window.</summary>
    /// <param name="operations">The user window operations requested by the application.</param>
    void SetOperations(WindowOperations operations) { }
    /// <summary>Changes topmost status without activating the window.</summary>
    void SetAlwaysOnTop(bool value) => throw new PlatformNotSupportedException("Always-on-top windows are unavailable.");
    /// <summary>Changes taskbar participation, preserving native state and activation.</summary>
    void SetTaskbarVisibility(bool value) => throw new PlatformNotSupportedException("Taskbar visibility is unavailable.");

    /// <summary>Enables hit testing for the client resize grip drawn by Nazor when the window can resize.</summary>
    /// <param name="visible">Whether the grip is requested by the window style.</param>
    void SetResizeGrip(bool visible) { }

    /// <summary>Gets the effective grip area in client coordinates, or an empty rectangle when hidden.</summary>
    Rect ResizeGripBounds => default;

    /// <summary>Requests the integrated Windows title bar height; colors are configured separately.</summary>
    /// <param name="height">The preferred standard or tall height.</param>
    /// <returns>Whether the native title bar is available.</returns>
    bool SetWindowsTitleBarHeight(WindowsTitleBarHeight height) => false;

    /// <summary>Maximizes, minimizes or restores the window.</summary>
    /// <param name="state">The state (WindowState).</param>
    void SetState(WindowState state) { }

    /// <summary>Exit full screen to the previously saved placement and state; otherwise no effect.</summary>
    void ExitFullScreen() { }

    /// <summary>The frame (title bar, borders) of the system in its dark colors, for a dark application.</summary>
    /// <param name="dark">The dark (bool).</param>
    void SetDarkFrame(bool dark) { }

    /// <summary>
    /// Resolves the backdrop request and fallback for this backend. An inactive result keeps the opaque theme
    /// background; acceptance does not guarantee compositor visibility.
    /// </summary>
    /// <param name="backdrop">The backdrop (BackdropSelection).</param>
    /// <returns>The resulting BackdropResult value.</returns>
    BackdropResult SetBackdrop(BackdropSelection backdrop) => default;

    /// <summary>True for the Windows NSS native decorations and metrics.</summary>
    bool UsesWindowsDecorations => false;

    /// <summary>Enables or disables input to the window (the owner of a modal dialog is disabled).</summary>
    /// <param name="enabled">Whether the feature or interaction is enabled.</param>
    void SetEnabled(bool enabled) { }

    /// <summary>Moves the window to the center of its owner or of the screen, inside the work area.</summary>
    /// <param name="centering">The centering (WindowCentering).</param>
    void Center(WindowCentering centering) { }

    /// <summary>Where the window is (its normal bounds, even while maximized), or null when unknown.</summary>
    /// <returns>The resulting WindowPlacement? value.</returns>
    WindowPlacement? GetPlacement() => null;

    /// <summary>
    /// Puts a window not shown yet where <paramref name="placement" /> says (maximized when it shows); false, and
    /// nothing changes, when that place is on no screen any more.
    /// </summary>
    /// <param name="placement">The normal window bounds and state to save or restore.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool SetPlacement(WindowPlacement placement) => false;

    /// <summary>Schedules a paint; repeated requests before the paint are merged.</summary>
    void Invalidate();

    /// <summary>
    /// Schedules a paint for the next animation frame, not sooner than <paramref name="delay" />: frames are paced by
    /// the backend instead of following each other as fast as possible.
    /// </summary>
    /// <param name="delay">The delay before the operation or animation starts.</param>
    void RequestAnimationFrame(TimeSpan delay);

    /// <summary>The user turned animations off in the system settings.</summary>
    bool PrefersReducedMotion { get; }

    /// <summary>The system asks applications for a dark color scheme (the app mode of Windows).</summary>
    bool PrefersDarkColorScheme { get; }

    /// <summary>A contrast theme is on: applications draw with the system colors only (CSS forced colors).</summary>
    bool PrefersHighContrast => false;

    /// <summary>Whether the host's primary input preference calls for larger touch targets. False is the compact fallback when this preference is unavailable; it does not claim native touch-event classification. Notify OnSystemSettingsChanged when it changes.</summary>
    bool PrefersTouchInput => false;

    /// <summary>
    /// Available CSS system colors, accent shades and PascalCase semantic palette roles (Tooltip, TooltipText, Header, HeaderText, etc.).
    /// Supply only available values; missing colors are resolved for ColorProfile and the effective light/dark scheme.
    /// Supply paired foreground/background colors and use OnSystemSettingsChanged when the palette changes.
    /// </summary>
    IReadOnlyDictionary<string, Color> SystemColors => new Dictionary<string, Color>();

    /// <summary>The fallback palette for this desktop. Linux backends should identify KDE or GNOME when known.</summary>
    SystemColorProfile ColorProfile => Platform switch
    {
        PlatformKind.Windows => SystemColorProfile.Windows,
        PlatformKind.MacOS => SystemColorProfile.MacOS,
        PlatformKind.Android => SystemColorProfile.Android,
        PlatformKind.IOS => SystemColorProfile.IOS,
        _ => SystemColorProfile.Generic,
    };

    /// <summary>Changes the window's logical client size.</summary>
    /// <param name="logicalSize">The client-area dimensions in logical units.</param>
    void Resize(Size logicalSize);

    /// <summary>The system clipboard, owned by this window.</summary>
    IClipboard Clipboard { get; }

    /// <summary>
    /// The caret of the focused text input in logical client coordinates: positions the IME windows and the
    /// (invisible) system caret. Null disables the IME, as no text input has the focus.
    /// </summary>
    /// <param name="caret">The caret (Rect?).</param>
    void SetTextInputCaret(Rect? caret);

    /// <summary>Enables or releases native pointer capture for this window.</summary>
    /// <param name="capture">The callback that receives the rendered element reference.</param>
    void SetPointerCapture(bool capture) { }
}

/// <summary>The dialogs of the system for files and folders.</summary>
public enum FileDialogKind
{
    /// <summary>Prompts for an existing file or files.</summary>
    Open,

    /// <summary>Prompts for a destination file path.</summary>
    Save,

    /// <summary>Prompts for an existing folder.</summary>
    Folder,
}

/// <summary>A choice of file types in a file dialog: a name and its patterns (<c>*.csv</c>).</summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="Patterns">The patterns (IReadOnlyList&lt;string&gt;).</param>
public sealed record FileDialogFilter(string Name, IReadOnlyList<string> Patterns);

/// <summary>What a file dialog of the system shows.</summary>
/// <param name="Kind">The file dialog kind represented by this value.</param>
public sealed record FileDialogRequest(FileDialogKind Kind)
{
    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    public string? Title { get; init; }

    /// <summary>Gets or sets the directory initially displayed by the native picker.</summary>
    public string? InitialDirectory { get; init; }

    /// <summary>The file name proposed (to save, or selected to open).</summary>
    public string? FileName { get; init; }

    /// <summary>The extension added to a saved name without one.</summary>
    public string? DefaultExtension { get; init; }

    /// <summary>Gets or sets the file-type filters offered by the native picker.</summary>
    public IReadOnlyList<FileDialogFilter> Filters { get; init; } = [];

    /// <summary>Several files can be opened at once.</summary>
    public bool Multiple { get; init; }
}

/// <summary>An operating-system backend: windows and the UI message loop.</summary>
public interface IPlatform
{
    /// <summary>The active platform backend name used in diagnostics.</summary>
    string BackendName => GetType().Name;

    /// <summary>
    /// Backend support before a native window exists; individual windows apply their requested operations.
    /// </summary>
    WindowCapabilities Capabilities => default;

    /// <summary>Null when the backend has no native tray or system notifications.</summary>
    IPlatformDesktopIntegration? DesktopIntegration => null;

    /// <summary>Creates a native window connected to the supplied Nazor window host.</summary>
    /// <param name="host">The Nazor window host that receives native lifecycle, painting, and input notifications.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>The native platform window connected to the host.</returns>
    IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options);

    /// <summary>
    /// Shows a file dialog of the system, modal to <paramref name="owner" />, and returns the chosen paths, or null
    /// when the user cancelled. It runs its own message loop: call it outside renders.
    /// </summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="request">The native file-dialog operation and options.</param>
    /// <returns>The resulting IReadOnlyList&lt;string&gt;? value.</returns>
    IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request) =>
        throw new NotSupportedException("This platform has no file dialogs.");

    /// <summary>
    /// Runs the message loop on the calling thread, which owns <paramref name="dispatcher" />, until
    /// <see cref="Quit" />.
    /// </summary>
    /// <param name="dispatcher">The dispatcher that owns the UI thread.</param>
    /// <returns>The resulting int value.</returns>
    int Run(UIDispatcher dispatcher);

    /// <summary>Requests termination of the platform event loop with the specified exit code.</summary>
    /// <param name="exitCode">The exit code reported when the application event loop terminates.</param>
    void Quit(int exitCode = 0);
}

/// <summary>Optional platform contract for one display surface without desktop window management.</summary>
public interface ISingleViewPlatform : IPlatform
{
    /// <summary>Creates the display surface on the UI thread. Its client size is supplied by the display.</summary>
    /// <param name="host">The surface rendering and input callbacks.</param>
    /// <returns>The surface adapter. Unsupported desktop operations must be reflected in its capabilities.</returns>
    IPlatformWindow CreateView(IWindowHost host);
}
