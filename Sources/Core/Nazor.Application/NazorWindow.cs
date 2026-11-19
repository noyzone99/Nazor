using Nazor.Graphics.Text;
using System.Diagnostics;
using Nazor.Accessibility;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Rendering;
using Nazor.Style;
using Microsoft.Extensions.Logging;

namespace Nazor;

/// <summary>
/// One application window: the component renderer, its scene (style, layout, display list), input and the native
/// window. A frame is requested only when something changed (a render batch, an interaction state, a resize); an idle
/// window draws nothing.
/// </summary>
public sealed partial class NazorWindow : IWindowHost, IWindow, IDisposable
{
    private static readonly List<NazorWindow> openWindows = [];
    private static readonly Lock openWindowsGate = new();
    private readonly SkiaRenderer graphics;
    private OpenGlCompositor? compositor;
    private OpenGlCompositor.Presentation? finalGpuPresentation;
    private long finalGpuFramesReplaced;
    private readonly ILogger? logger;
    private readonly Stopwatch frameTimer = new();
    private IPlatformWindow? platformWindow;
    private ElementNode? windowElement;
    private RootComponent? contentFrame;
    private WindowComponentBase? DecorationOwner => (contentFrame ?? Root)?.Component as WindowComponentBase;
    private SemanticTree? semantics;
    private bool? reducedMotion;
    private ColorScheme colorScheme;
    private bool? highContrast;
    private ControlDensity applicationDensity;
    private ControlDensity densityOverride;
    private readonly IReadOnlyDictionary<string, Nazor.Graphics.Color>? systemColors;
    private string pendingTitle = "";
    private string? placementKey;
    private readonly IWindowPlacementStore? placementStore;
    private bool placementSaved;
    private bool disposed;
    private bool isVisible;
    private readonly bool? declaredWindow;
    private WindowOperations operations;
    private BackdropSelection? declaredBackdrop;
    private bool backdropUsesRaster;
    private readonly WindowCapabilities backendCapabilities;

    internal NazorWindow(IPlatform platform, UIDispatcher dispatcher, Type rootComponent, ApplicationRuntimeSettings? options = null, ParameterView parameters = default,
        WindowOptions? frame = null)
    {
        options ??= new ApplicationRuntimeSettings();
        declaredWindow = options.DeclaredWindow;
        if (!Enum.IsDefined(options.ColorScheme)) throw new ArgumentOutOfRangeException(nameof(options));
        if (!Enum.IsDefined(options.ControlDensity)) throw new ArgumentOutOfRangeException(nameof(options));
        var defaults = frame?.IsDialog is true ? WindowOperations.Dialog with { FullScreenEnabled = options.FullScreenEnabled } : new WindowOperations(options.IsMinimizable, options.IsMaximizable, options.IsResizable, options.FullScreenEnabled);
        operations = new(frame?.IsMinimizable ?? defaults.IsMinimizable, frame?.IsMaximizable ?? defaults.IsMaximizable,
            frame?.IsResizable ?? defaults.IsResizable, frame?.FullScreenEnabled ?? defaults.FullScreenEnabled);
        backendCapabilities = platform.Capabilities;
        RefreshCapabilities();
        Dispatcher = dispatcher;
        IsAlwaysOnTop = options.IsAlwaysOnTop;
        BackgroundMode = frame?.BackgroundMode ?? BackgroundMode.Default;
        IsVisibleInTaskbar = options.IsVisibleInTaskbar;
        decorationPolicy = options.Decorations;
        pendingTitle = options.Title;
        width = options.Width;
        height = options.Height;
        IsDialog = frame?.IsDialog ?? false;

        if (options.Services?.GetService(typeof(WindowContext)) is WindowContext context) context.Window = this;

        logger = (options.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger("Nazor");
        Renderer = new Renderer(dispatcher, options.Services, this);
        Renderer.UnhandledException += (_, exception) => OnException(exception);
        dispatcher.UnhandledException += OnDispatcherException;
        graphics = new SkiaRenderer(options.Fonts);
        Scene = new Scene(Renderer, graphics.Fonts, graphics.Shaper, clock: options.AnimationClock, images: graphics.Images);

        Scene.ImageLoaded += () => dispatcher.Post(() => RequestFrame(WindowFrameCause.Image));
        Scene.Styles.Theme = options.Theme ?? Themes.Themes.Basic;
        Scene.Styles.Application = options.StyleSheet;
        Input = new InputManager(Renderer, Scene, options.Services?.GetService(typeof(KeyBindings)) as KeyBindings);
        Input.VisualStateChanged += OnVisualStateChanged;
        Renderer.BatchCompleted += OnBatchCompleted;

        try
        {
            if (!options.SingleView && !typeof(WindowComponentBase).IsAssignableFrom(rootComponent) && options.DeclaredWindow is not true)
            {
                contentFrame = Renderer.AddRootComponent<ContentWindow>(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ContentWindow.ComponentType)] = rootComponent,
                    [nameof(ContentWindow.ContentParameters)] = parameters,
                }));
                var element = contentFrame.Nodes.OfType<ElementNode>().Single();
                var content = element.LogicalChildren.OfType<ElementNode>().Single(node => node.Name == "WindowContent");
                Root = new RootComponent(Renderer, content.LogicalChildren.OfType<ComponentNode>().Single(), parameters);
            }
            else Root = Renderer.AddRootComponent(rootComponent, parameters);
            windowElement = ValidateWindowRoot(options.DeclaredWindow);
            ValidateTitleBars();
            var nativeOptions = (frame ?? new WindowOptions()) with
            {
                Decorations = InitialDecorations(options.SingleView) ?? WindowDecorations.Native(),
                Title = pendingTitle,
                Width = width,
                Height = height,

                IsMinimizable = operations.IsMinimizable,
                IsMaximizable = operations.IsMaximizable,
                IsResizable = operations.IsResizable,
                FullScreenEnabled = operations.FullScreenEnabled,

                IsAlwaysOnTop = isAlwaysOnTop,
                BackgroundMode = backgroundMode,
                IsVisibleInTaskbar = isVisibleInTaskbar,
            };
            platformWindow = options.SingleView
                ? ((ISingleViewPlatform)platform).CreateView(this)
                : platform.CreateWindow(this, nativeOptions);
            platformWindow.SetTitleBarMenuMode(titleBarMenuMode);
            ApplyWindowActivation();
            var rendererFallback = InitializeRendering(options.Rendering ?? RendererSelection.ReadEnvironment());
            instrumentation?.NativeWindowCreated(platformWindow);

            RefreshIcon();
            ApplyBackdrop();

            placementStore = placementKey is null ? null : options.Services?.GetService(typeof(IWindowPlacementStore)) as IWindowPlacementStore;
            RestorePlacement();
            platformWindow.SetSizeLimits(minWidth, minHeight, maxWidth, maxHeight);
            if (x.HasValue || y.HasValue) platformWindow.Move(x, y);
            Input.PointerCaptureChanged += PlatformWindow.SetPointerCapture;
            Scene.Scale = PlatformWindow.Scale;
            Scene.Viewport = PlatformWindow.ClientSize;
            Input.Clipboard = PlatformWindow.Clipboard;
            ApplyInputPreferences();
            reducedMotion = options.ReducedMotion;
            colorScheme = (options.Services?.GetService(typeof(WindowManager)) as WindowManager)?.ColorScheme ?? options.ColorScheme;
            highContrast = options.HighContrast;
            applicationDensity = options.ControlDensity;
            systemColors = options.SystemColors;
            ApplyPreferences();
            RefreshSystemButtons();
            RefreshCapabilities();

            NotifyPresentation();
            lock (openWindowsGate) openWindows.Add(this);
            HotReloadManager.DeltaApplied += OnDeltaApplied;
            InitializeDecorations();
            options.ConfigureWindow?.Invoke(this);
            StartupLog.Renderer(logger, Rendering, rendererFallback);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// The windows created and not yet disposed, in creation order (a snapshot); development tools serve them.
    /// </summary>
    public static IReadOnlyList<NazorWindow> OpenWindows
    {
        get
        {
            lock (openWindowsGate) return openWindows.ToArray();
        }
    }

    /// <summary>Gets the UI dispatcher associated with this renderer or window.</summary>
    public UIDispatcher Dispatcher { get; }

    /// <summary>Gets the renderer that owns the component tree.</summary>
    public Renderer Renderer { get; }

    /// <summary>Gets the scene that provides layout, hit testing, and drawing for the window.</summary>
    public Scene Scene { get; }

    /// <summary>Gets the requested and effective renderer, device and automatic fallback reason.</summary>
    public RenderingInfo Rendering { get; private set; } = new(RenderingMode.Cpu, RenderingMode.Cpu, "Skia raster");

    /// <summary>Gets the number of GPU frames actually presented by this window's render worker.</summary>
    public long GpuFramesPresented => compositor?.Presented ?? finalGpuPresentation?.Number ?? 0;

    /// <summary>Gets how many pending complete GPU frames were replaced by newer frames before rendering.</summary>
    public long GpuFramesReplaced => compositor?.Replaced ?? finalGpuFramesReplaced;

    /// <summary>Gets the last render worker's CPU submission and presentation wait time; excludes UI work and is not a GPU execution timestamp.</summary>
    public TimeSpan LastGpuFrameDuration => compositor?.LastFrameDuration ?? finalGpuPresentation?.Duration ?? TimeSpan.Zero;

    /// <summary>Reports backend submission completion on the UI dispatcher; busy dispatchers coalesce to the latest completed frame.</summary>
    /// <remarks>The GPU worker frame number differs from the UI frame number during compositor animations. Completion does not measure physical scanout.</remarks>
    public event Action<PresentedFrame>? FramePresented;

    private PresentedFrame? pendingPresentation;
    private int presentationQueued;

    private void ReportPresentation(PresentedFrame frame)
    {
        if (FramePresented is null) return;
        Interlocked.Exchange(ref pendingPresentation,frame);
        if (Interlocked.Exchange(ref presentationQueued,1)!=0) return;
        Dispatcher.Post(() =>
        {
            Volatile.Write(ref presentationQueued,0);
            var latest=Interlocked.Exchange(ref pendingPresentation,null);
            if (!disposed && latest is not null) FramePresented?.Invoke(latest);
        });
    }

    void IWindowHost.OnPresented(TimeSpan duration)
    {
        if (FramePresented is not null) ReportPresentation(new(FrameCount,RenderingMode.Cpu,null,duration));
    }

    /// <summary>Captures the current frame, reading GPU pixels only when explicitly requested.</summary>
    /// <returns>The GPU framebuffer with recorded client decorations, excluding separate native OS child windows, or a software-rendered image.</returns>
    /// <remarks>Call on the UI dispatcher and await without blocking it. GPU readback runs on the rendering thread.</remarks>
    public Task<RenderedImage> CaptureFrameAsync()
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (FrameCount==0) return Task.FromException<RenderedImage>(new InvalidOperationException("The window has not submitted a frame yet."));
        return compositor?.CaptureAsync() ?? Task.FromResult(graphics.RenderToImage(Scene.BuildDisplayList(), Scene.Scale, Scene.Background));
    }

    bool IWindowHost.UsesGpuRendering => compositor is not null;

    void IWindowHost.OnRenderingStopping() => StopCompositor();

    private void StopCompositor()
    {
        var previous=compositor;
        compositor = null;
        if (previous is null) return;
        try { previous.Dispose(); }
        finally
        {
            finalGpuPresentation=previous.LastPresentation;
            finalGpuFramesReplaced=previous.Replaced;
            Scene.ResetComposition();
        }
    }

    private bool InitializeRendering(RenderingMode requested)
    {
        Rendering = new(requested, RenderingMode.Cpu, "Skia raster");
        if (requested == RenderingMode.Cpu) return false;
        if (backgroundMode == BackgroundMode.Transparent)
        {
            Rendering = new(requested, RenderingMode.Cpu, "Skia raster",
                FallbackReason: "Transparent window presentation uses the raster surface.");
            return false;
        }
        try
        {
            compositor = new OpenGlCompositor(PlatformWindow.CreateOpenGlContext, exception => Dispatcher.Post(() =>
            {
                if (disposed) return;
                StopCompositor();
                frameDamage.Reset();
                if (Rendering.Requested == RenderingMode.Auto && exception is GraphicsDeviceException)
                {
                    Rendering = new(Rendering.Requested, RenderingMode.Cpu, "Skia raster", FallbackReason: exception.Message);
                    PlatformWindow.ResetRendering();
                    if (logger is null) Log(LogLevel.Warning, $"GPU rendering failed; using CPU: {exception.Message}");
                    else StartupLog.Renderer(logger, Rendering, failed: true);
                    RequestFrame();
                }
                else { OnException(exception); Close(); }
            }), () => Dispatcher.Post(() => { if (!disposed) RequestFrame(WindowFrameCause.Animation); }),
                frame => { if (FramePresented is not null) ReportPresentation(new(frame.UiFrame,RenderingMode.Gpu,frame.Number,frame.Duration)); });
            Rendering = new(requested, RenderingMode.Gpu, compositor.Backend, compositor.Device);
            PlatformWindow.ResetRendering();
            return false;
        }
        catch (Exception exception) when (requested == RenderingMode.Auto && exception is GraphicsDeviceException or DllNotFoundException or EntryPointNotFoundException)
        {
            Rendering = new(requested, RenderingMode.Cpu, "Skia raster", FallbackReason: exception.Message);
            if (logger is null) Log(LogLevel.Warning, $"Renderer AUTO -> CPU: {exception.Message}");
            return true;
        }
    }

    /// <summary>Gets the input manager associated with this window.</summary>
    public InputManager Input { get; }

    private IWindowInstrumentation? instrumentation;

    /// <summary>
    /// Gets or replaces the optional input, overlay and frame-metrics integration. Set on the UI dispatcher.
    /// The window owns and disposes the previous integration before replacement. Detaching requests a frame to
    /// erase its overlay; attaching alone starts no listener or timer. Null is allowed during window disposal;
    /// attaching to a disposed window throws <see cref="ObjectDisposedException" />.
    /// </summary>
    public IWindowInstrumentation? Instrumentation
    {
        get => instrumentation;
        set
        {
            Dispatcher.AssertAccess();
            if (value is not null) ObjectDisposedException.ThrowIf(disposed, this);
            if (ReferenceEquals(instrumentation, value)) return;
            var previous = instrumentation;
            instrumentation = null;
            try { previous?.Dispose(); }
            finally { if (previous is not null) RequestFrame(); }
            instrumentation = value;
            try { if (platformWindow is not null) value?.NativeWindowCreated(platformWindow); }
            catch { instrumentation = null; value?.Dispose(); throw; }
        }
    }

    /// <summary>Gets the root component hosted by this window.</summary>
    public RootComponent Root { get; }

    /// <summary>Gets the native backend window used by this host.</summary>
    public IPlatformWindow PlatformWindow => platformWindow ?? throw new InvalidOperationException("The native window is not created yet.");

    /// <inheritdoc />
    public NativeWindowHandle? NativeHandle
    {
        get { Dispatcher.AssertAccess(); return IsClosed ? null : platformWindow?.NativeHandle; }
    }

    /// <summary>
    /// The accessible view of the window, created on first use (the first request of an assistive technology): until
    /// then a window does no accessibility work.
    /// </summary>
    public SemanticTree Semantics => semantics ??= new SemanticTree(Renderer, Scene, Input);

    /// <summary>True once <see cref="Semantics" /> exists.</summary>
    public bool HasSemantics => semantics is not null;

    /// <summary>
    /// Asks the screen reader to speak a message ("Saved", "3 results"). A window that no assistive technology has
    /// asked about has nobody to tell: nothing happens.
    /// </summary>
    /// <param name="message">The message (string).</param>
    /// <param name="priority">The priority (AnnouncementPriority).</param>
    public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite) => semantics?.Announce(message, priority);

    /// <summary>UI frames submitted so far; autonomous compositor frames are counted by GpuFramesPresented.</summary>
    public int FrameCount { get; private set; }

    /// <summary>Gets the last UI paint-handler duration, excluding asynchronous GPU work and native presentation.</summary>
    public TimeSpan LastFrameDuration { get; private set; }

    /// <summary>Exceptions reported by components, event handlers and dispatched work.</summary>
    public List<Exception> Exceptions { get; } = [];

    /// <summary>How many .NET Hot Reload deltas this window re-rendered for (failed re-renders included).</summary>
    public int HotReloadCount { get; private set; }

    /// <summary>
    /// Raised on the UI thread after the window re-rendered for a hot reload delta, with the updated types (null when
    /// unknown); the development channel forwards it to the tools.
    /// </summary>
    public event Action<Type[]?>? HotReloadApplied;

    /// <summary>Occurs when closed is reported.</summary>
    public event Action? Closed;

    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    public string Title
    {
        get => platformWindow?.Title ?? pendingTitle;
        set
        {
            AccessConfiguration();
            ArgumentNullException.ThrowIfNull(value);
            if (value == pendingTitle) return;
            pendingTitle = value;
            if (platformWindow is { IsClosed: false } window) window.Title = value;
            NotifyPresentation();
        }
    }

    /// <summary>Gets whether the native window has closed.</summary>
    public bool IsClosed => disposed || platformWindow is { IsClosed: true };

    /// <summary>Shows the window and optionally activates it.</summary>
    /// <param name="activate">Whether showing the window should also activate it.</param>
    public void Show(bool activate = true) => PlatformWindow.Show(activate);

    /// <summary>Hides the window while keeping its components and services alive.</summary>
    public void Hide() => platformWindow?.Hide();

    /// <summary>Brings the window to the front and gives it keyboard focus.</summary>
    public void Activate() => platformWindow?.Activate();

    /// <summary>Gets the current normal, minimized, maximized, or full-screen window state.</summary>
    public WindowState State => platformWindow is { IsClosed: false } window ? window.State : WindowState.Normal;

    /// <summary>Gets or sets whether the application allows the window to be minimized.</summary>
    public bool IsMinimizable { get => operations.IsMinimizable; set => SetOperations(operations with { IsMinimizable = value }); }

    /// <summary>Gets or sets whether the application allows the window to be maximized.</summary>
    public bool IsMaximizable { get => operations.IsMaximizable; set => SetOperations(operations with { IsMaximizable = value }); }

    /// <summary>Gets or sets whether the application allows interactive window resizing.</summary>
    public bool IsResizable { get => operations.IsResizable; set => SetOperations(operations with { IsResizable = value }); }

    /// <summary>Gets or sets whether the application allows entry into full screen.</summary>
    public bool FullScreenEnabled { get => operations.FullScreenEnabled; set => SetOperations(operations with { FullScreenEnabled = value }); }

    private void SetOperations(WindowOperations value)
    {
        Dispatcher.AssertAccess();
        if (operations == value) return;
        operations = value;
        var previousCapabilities = Capabilities;
        platformWindow?.SetOperations(value);
        RefreshCapabilities();
        RefreshSystemButtons();
        if (Capabilities == previousCapabilities) NotifyPresentation();
    }


    /// <summary>
    /// Gets the window operations and decorations supported by the current backend and application settings.
    /// </summary>
    public WindowCapabilities Capabilities { get; private set; }

    /// <summary>Occurs when capabilities changes.</summary>
    public event Action? CapabilitiesChanged;

    private void RefreshCapabilities()
    {
        var capabilities = platformWindow?.Capabilities ?? backendCapabilities with
        {
            Minimize = backendCapabilities.Minimize,
            Maximize = backendCapabilities.Maximize,
            InteractiveResize = backendCapabilities.InteractiveResize,
            FullScreen = backendCapabilities.FullScreen,
            CanMinimize = backendCapabilities.CanMinimize && operations.IsMinimizable,
            CanMaximize = backendCapabilities.CanMaximize && operations.IsMaximizable,
            CanResize = backendCapabilities.CanResize && operations.IsResizable,
            CanEnterFullScreen = backendCapabilities.CanEnterFullScreen && operations.FullScreenEnabled,



        };
        capabilities = capabilities with { Decorations = capabilities.Decorations with { Current = AppliedDecorations?.Configuration } };
        RefreshChrome();
        if (capabilities == Capabilities)
        {
            UpdateNativePopupPolicy();
            return;
        }
        Capabilities = capabilities;
        UpdateNativePopupPolicy();
        CapabilitiesChanged?.Invoke();
        NotifyPresentation();
    }

    void IWindowHost.OnCapabilitiesChanged()
    {
        var supported = Capabilities.CanSetWindowIcon;
        RefreshCapabilities();
        if (supported != Capabilities.CanSetWindowIcon) RefreshIcon();
    }

    /// <summary>Gets the physical placement and order of the native caption controls.</summary>
    public WindowButtonsLayout SystemButtonsLayout { get; private set; } = WindowButtonsLayout.Default;

    /// <summary>Gets the logical native-caption insets and height for the custom client title bar.</summary>
    public WindowButtonsMetrics SystemButtonsMetrics { get; private set; }

    /// <summary>Gets the total logical width and height reserved for integrated native caption controls.</summary>
    public (float Width, float Height) SystemButtonsSize => (SystemButtonsMetrics.Width, SystemButtonsMetrics.Height);

    /// <summary>Occurs when system controls changes.</summary>
    public event Action? SystemButtonsChanged;

    private void RefreshSystemButtons()
    {
        RefreshChrome();
        var size = isVisible ? platformWindow?.SystemButtonsMetrics ?? default : default;
        var layout = platformWindow?.SystemButtonsLayout ?? WindowButtonsLayout.Default;
        Scene.Styles.SystemButtonsHeight = size.Width > 0 ? size.Height : null;
        if (size == SystemButtonsMetrics && layout == SystemButtonsLayout) return;
        SystemButtonsMetrics = size;
        SystemButtonsLayout = layout;
        lastFrame = null;
        SystemButtonsChanged?.Invoke();
    }

    /// <summary>Requests that the window enter the minimized state.</summary>
    public void Minimize() => Request(WindowCommand.Minimize);

    /// <summary>Requests that the window enter the maximized state.</summary>
    public void Maximize() => Request(WindowCommand.Maximize);

    /// <summary>Requests native full screen while preserving the previous placement and state.</summary>
    public void EnterFullScreen() => Request(WindowCommand.EnterFullScreen);

    /// <summary>Leaves full screen and restores the saved placement and state.</summary>
    public void ExitFullScreen() => Request(WindowCommand.ExitFullScreen);

    /// <summary>Leaves full screen or restores the window from its maximized or minimized state.</summary>
    public void Restore() => Request(WindowCommand.Restore);

    /// <summary>Occurs when state changes.</summary>
    public event Action? StateChanged;

    /// <summary>Closes the window and completes its lifetime.</summary>
    public void Close() => Close(null);

    /// <summary>
    /// Closes the window; a dialog completes its <c>ShowDialogAsync</c> with <paramref name="result" />.
    /// </summary>
    /// <param name="result">The value returned to the caller awaiting the dialog.</param>
    public void Close(object? result)
    {
        if (IsClosed) return;
        DialogResult = result;
        PlatformWindow.Close();
    }

    /// <summary>The backdrop accepted by this window's backend.</summary>
    public BackdropResult AppliedBackdrop { get; private set; }

    private void ApplyBackdrop()
    {
        if (platformWindow is null) return;
        var requested = backgroundMode == BackgroundMode.Transparent ? default : backdropRequest;
        if (declaredBackdrop != requested)
        {
            declaredBackdrop = requested;
            AppliedBackdrop = platformWindow.SetBackdrop(requested);
            if (platformWindow.Platform == PlatformKind.Windows)
            {
                if (AppliedBackdrop.IsActive && compositor is not null)
                {
                    // The WGL child presents an opaque surface over the parent window's DWM material.
                    StopCompositor();
                    Rendering = new(Rendering.Requested, RenderingMode.Cpu, "Skia raster",
                        FallbackReason: "Windows backdrop compositing requires the raster surface.");
                    frameDamage.Reset();
                    platformWindow.ResetRendering();
                    backdropUsesRaster = true;
                }
                else if (!AppliedBackdrop.IsActive && backdropUsesRaster)
                {
                    backdropUsesRaster = false;
                    frameDamage.Reset();
                    var fellBack = InitializeRendering(Rendering.Requested);
                    StartupLog.Renderer(logger, Rendering, fellBack);
                }
            }
        }
        Scene.Background = AppliedBackdrop.IsActive || backgroundMode == BackgroundMode.Transparent
            ? new Graphics.Color(0, 0, 0, 0) : Graphics.Color.White;
        if (windowElement is not null)
        {
            if (AppliedBackdrop.IsActive) windowElement.SetAttribute("backdrop", AppliedBackdrop.Material ?? "native");
            else windowElement.RemoveAttribute("backdrop");
            Scene.Styles.InvalidateAll();
        }
    }

    /// <summary>A modal dialog: Esc closes it.</summary>
    public bool IsDialog { get; }

    /// <summary>The result given to <see cref="Close(object?)" />.</summary>
    public object? DialogResult { get; private set; }

    internal event Action? Closing;

    internal void FitHeightToContent()
    {
        Scene.Viewport = PlatformWindow.ClientSize;
        Scene.Scale = PlatformWindow.Scale;
        Scene.Update();
        ApplyCaptionAppearance();
        if (Root.Nodes.OfType<ElementNode>().FirstOrDefault() is not { } root) return;
        var top = Scene.GetBounds(root).Y;
        var children = root.Children.OfType<ElementNode>().SelectMany(child => child.Name == "WindowContent" ? child.Children.OfType<ElementNode>() : [child]);
        var bottom = children.Select(child => Scene.GetBounds(child).Bottom).DefaultIfEmpty(top).Max();
        if (bottom > top) PlatformWindow.Resize(new Size(PlatformWindow.ClientSize.Width, MathF.Ceiling(bottom - top)));
    }

    /// <summary>Schedules a frame, recording the supplied cause when specified.</summary>
    public void RequestFrame() => RequestFrame(WindowFrameCause.Explicit);

    /// <summary>Schedules a frame, recording the supplied cause when specified.</summary>
    /// <param name="cause">The combined reasons reported to an attached instrumentation integration.</param>
    public void RequestFrame(WindowFrameCause cause)
    {
        pendingFrameCause |= cause;

        if (!disposed && platformWindow is { IsClosed: false } window) window.Invalidate();
    }

    private WindowFrameCause pendingFrameCause;
    private bool animationFramePending;
    private void OnVisualStateChanged() => RequestFrame(WindowFrameCause.Interaction);

    void IWindowHost.OnResized(Size logicalSize, float scale)
    {
        if (platformWindow?.State == WindowState.Normal) { width = logicalSize.Width; height = logicalSize.Height; }
        Scene.Viewport = logicalSize;
        Scene.Scale = scale;
        if (iconScale != scale) RefreshIcon();
        RefreshSystemButtons();
        pendingFrameCause |= WindowFrameCause.Resize;
    }

    IReadOnlyList<PixelRect>? IWindowHost.OnPaint(PaintTarget target)
    {
        frameTimer.Restart();
        Scene.Viewport = new Size(target.Width / target.Scale, (target.Height - target.TopInset) / target.Scale);
        Scene.Scale = target.Scale;
        RefreshSystemButtons();
        var observer = Instrumentation;
        var cause = pendingFrameCause;
        pendingFrameCause = WindowFrameCause.None;
        if (animationFramePending || Scene.TimeUntilNextFrame is not null) cause |= WindowFrameCause.Animation;
        animationFramePending = false;
        if (cause == WindowFrameCause.None) cause = WindowFrameCause.Paint;
        var sameGeometry = cachedDisplayList is { } cached && cached.Size == Scene.Viewport && cachedScale == target.Scale &&
            cachedFontVersion == graphics.Fonts.Version && !Scene.Styles.IsDirty;
        var reuse = sameGeometry && cause == WindowFrameCause.Paint;
        if (!reuse)
        {
            if (sameGeometry && cause == WindowFrameCause.Animation) Scene.UpdateMotion();
            else Scene.Update();
            ApplyCaptionAppearance();
            if (Scene.Styles.IsDirty) Scene.Update();
        }
        var work = reuse ? (StylesRecomputed: 0, LayoutComputations: 0) : Scene.LastUpdate;
        var durations = reuse ? (Styles: TimeSpan.Zero, Layout: TimeSpan.Zero) : Scene.LastUpdateDuration;
        var drawingStarted = observer is null ? 0 : Stopwatch.GetTimestamp();
        var gpuFrame = compositor is not null && target.Pixels == 0;
        var displayList = reuse ? cachedDisplayList! : gpuFrame ? Scene.BuildCompositionList() : Scene.BuildDisplayList();
        cachedDisplayList = displayList;
        cachedScale = target.Scale;
        cachedFontVersion = graphics.Fonts.Version;
        if (!reuse && nativePopups.Count != 0) PaintNativePopups();
        var drawingDuration = observer is null ? TimeSpan.Zero : Stopwatch.GetElapsedTime(drawingStarted);
        var overlayStarted = observer is null ? 0 : Stopwatch.GetTimestamp();
        if (observer is not null) displayList = observer.Compose(displayList);
        displayList = ComposeResizeGrip(displayList);
        if (gpuFrame) displayList = PlatformWindow.ComposeFrame(displayList, target.TopInset / target.Scale, graphics.Fonts, graphics.Shaper, Scene.Background);
        if (!target.SeparateTopDecoration && GetClientFrameBorder(displayList.Size) is { } border)
            displayList = new(displayList.Size, [.. displayList.Commands, border]);
        var overlayDuration = observer is null ? TimeSpan.Zero : Stopwatch.GetElapsedTime(overlayStarted);

        var frameBackground = gpuFrame && PlatformWindow.HasRoundedClientFrame ? Graphics.Color.Transparent : Scene.Background;
        var geometry = target with { ForceFullRedraw = false };
        if (target.ForceFullRedraw || lastFrame is not { } last || last.Target != geometry || last.Background != frameBackground) frameDamage.Reset();
        var damage = frameDamage.Next(displayList, target.Scale) is { } changed ? DisplayListDamage.ToPixels(changed, target.Scale, target.Width, target.Height) : null;
        var rasterStarted = observer is null ? 0 : Stopwatch.GetTimestamp();
        if (compositor is { } gpu && gpuFrame) gpu.Submit(graphics.RecordComposition(displayList), target, frameBackground, damage,FrameCount+1);
        else graphics.RenderRetained(displayList, target.Pixels, target.Width, target.Height, target.Stride, target.Scale, Scene.Background, damage);
        var rasterDuration = observer is null ? TimeSpan.Zero : Stopwatch.GetElapsedTime(rasterStarted);
        lastFrame = (geometry, frameBackground);
        LastFrameDamage = damage;
        FrameCount++;

        PlatformWindow.SetTextInputCaret(Input.GetTextCaretRect());

        semantics?.UpdateAfterScene();

        var autonomousMotion=gpuFrame && Scene.CanAnimateOnCompositor && displayList.Commands.Any(command => command is PushComposition);
        if (Scene.TimeUntilNextFrame is { } delay && !autonomousMotion)
        {
            animationFramePending = true;
            PlatformWindow.RequestAnimationFrame(delay);
        }
        LastFrameDuration = frameTimer.Elapsed;
        var presentation = compositor?.LastPresentation;
        observer?.RecordFrame(new WindowFrameMetrics(FrameCount, cause, LastFrameDuration, durations.Styles, durations.Layout,
            drawingDuration, overlayDuration, rasterDuration, work.StylesRecomputed, work.LayoutComputations, damage)
        {
            Rendering = gpuFrame || Rendering.Actual == RenderingMode.Cpu ? Rendering : new(Rendering.Requested,RenderingMode.Cpu,"Skia raster capture"),
            RenderWorkerFrame = presentation?.Number, RenderWorkerDuration = presentation?.Duration,
            GpuFramesReplaced = compositor?.Replaced,
        });
        return damage;
    }

    private (PaintTarget Target, Nazor.Graphics.Color Background)? lastFrame;
    private DisplayList? cachedDisplayList;
    private float cachedScale;
    private int cachedFontVersion;
    private readonly DisplayListDamage frameDamage = new();

    private static readonly DrawShadow DefaultClientFrameShadow = new(default, default, new(0, 5), 24, 0, new(0, 0, 0, 88), false);
    private (Rect Bounds, Graphics.Color Color, DrawCommand[] Commands)? resizeGripDrawing;

    private ComputedStyle WindowRootStyle => windowElement is null ? ComputedStyle.Initial : Scene.GetStyle(windowElement);

    private DisplayList ComposeResizeGrip(DisplayList content)
    {
        var bounds = PlatformWindow.ResizeGripBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return content;
        var color = ToGraphicsColor(WindowRootStyle.Get(StyleProperties.WindowResizeGripColor));
        if (color.IsTransparent) return content;
        if (resizeGripDrawing is not { } drawing || drawing.Bounds != bounds || drawing.Color != color)
        {
            var scale = Math.Min(bounds.Width, bounds.Height) / 16;
            var right = bounds.Right - 3 * scale;
            var bottom = bounds.Bottom - 3 * scale;
            DrawPolyline Mark(float length) => new([new(right - length * scale, bottom), new(right, bottom - length * scale)], scale, color);
            resizeGripDrawing = drawing = (bounds, color, [Mark(3), Mark(6), Mark(9)]);
        }
        return new(content.Size, [.. content.Commands, .. drawing.Commands]);
    }

    DrawBorder? IWindowHost.GetClientFrameBorder(Size frameSize) => GetClientFrameBorder(frameSize);

    private DrawBorder? GetClientFrameBorder(Size frameSize)
    {
        if (!PlatformWindow.DrawClientFrame) return null;
        var style = WindowRootStyle;
        var lineStyle = style.Get(StyleProperties.WindowFrameBorderStyle);
        if (lineStyle == BorderStyle.None) return null;
        var width = Math.Min(NonnegativeLength(style.Get(StyleProperties.WindowFrameBorderWidth)), Math.Min(frameSize.Width, frameSize.Height) / 2);
        var color = ToGraphicsColor(style.Get(StyleProperties.WindowFrameBorderColor));
        if (width <= 0 || color.IsTransparent) return null;
        var line = lineStyle switch { BorderStyle.Dashed => LineStyle.Dashed, BorderStyle.Dotted => LineStyle.Dotted, _ => LineStyle.Solid };
        return new(new(0, 0, frameSize.Width, frameSize.Height), PlatformWindow.ClientFrameRadius, new(width, width, width, width), color, line);
    }

    void IWindowHost.PaintClientFrameBorder(PaintTarget target, DrawBorder border)
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(target.Width, target.Height,
            SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul), target.Pixels, target.Stride)
            ?? throw new InvalidOperationException("Unable to create a Skia surface on the window frame buffer.");
        surface.Canvas.Scale(target.Scale);
        graphics.DrawBorderSides(surface.Canvas, border);
        surface.Canvas.Flush();
    }

    /// <summary>The rectangles the last frame drew (physical pixels), or null when it drew everything.</summary>
    public IReadOnlyList<PixelRect>? LastFrameDamage { get; private set; }

    void IWindowHost.OnPointerMoved(Point point, KeyModifiers modifiers)
    {
        if (Instrumentation?.PointerMoved(point) != true) Input.PointerMoved(point, modifiers);
    }

    void IWindowHost.OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers)
    {
        if (Instrumentation?.PointerPressed(point, button) != true) Input.PointerPressed(point, button, modifiers);
    }

    void IWindowHost.OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers)
    {
        if (Instrumentation?.PointerReleased(point, button) != true) Input.PointerReleased(point, button, modifiers);
    }

    void IWindowHost.OnPointerLeft()
    {
        if (Instrumentation?.PointerLeft() != true) Input.PointerLeft();
    }

    void IWindowHost.OnPointerDoubleClicked(Point point, PointerButton button, KeyModifiers modifiers)
    {
        if (Instrumentation?.InterceptsPointer != true) Input.PointerDoubleClicked(point, button, modifiers);
    }

    void IWindowHost.OnPointerWheel(Point point, float notches, KeyModifiers modifiers)
    {
        if (Instrumentation?.InterceptsPointer != true) Input.PointerWheel(point, notches, modifiers);
    }

    void IWindowHost.OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers)
    {
        if (Instrumentation?.InterceptsPointer != true) Input.PointerWheel(point, notches, 0, modifiers);
    }

    void IWindowHost.OnPointerCanceled()
    {
        Instrumentation?.PointerCanceled();
        Input.PointerCanceled();
    }

    bool IWindowHost.OnKeyDown(string key, KeyModifiers modifiers, bool repeat)
    {
        if (Instrumentation?.KeyDown(key, modifiers, repeat) == true) return true;
        if (Input.KeyDown(key, modifiers, repeat)) return true;
        if (key == "F11" && modifiers == KeyModifiers.None && (State == WindowState.FullScreen || Capabilities.CanEnterFullScreen))
        {
            if (!repeat)
            {
                if (State == WindowState.FullScreen) ExitFullScreen();
                else EnterFullScreen();
            }
            return true;
        }

        if (!IsDialog || key != "Escape" || modifiers != KeyModifiers.None || repeat) return false;
        Close(null);
        return true;
    }

    bool IWindowHost.OnKeyUp(string key, KeyModifiers modifiers) => Instrumentation?.KeyUp(key) == true || Input.KeyUp(key, modifiers);

    void IWindowHost.OnTextInput(string text) => Input.TextInput(text);

    void IWindowHost.OnComposition(string text, int cursor) => Input.CompositionUpdate(text, cursor);

    void IWindowHost.OnCompositionCommit(string text) => Input.CompositionCommit(text);

    private bool windowActive;

    private void ApplyWindowActivation()
    {
        if (platformWindow is null) return;
        windowElement?.SetState(ElementStates.WindowActive, windowActive);
        windowElement?.SetState(ElementStates.WindowInactive, !windowActive);
    }

    void IWindowHost.OnActivationChanged(bool active)
    {
        if (windowActive == active) return;
        windowActive = active;
        if (!active) DismissNativePopups();
        if (platformWindow is INativePopupOwner { CanShowInactivePopups: false })
        {
            UpdateNativePopupPolicy();
        }
        ApplyWindowActivation();
        RequestFrame();
    }

    void IWindowHost.OnFocusChanged(bool focused)
    {
        Input.WindowFocusChanged(focused);
        if (platformWindow is { ReportsActivationSeparately: false })
            ((IWindowHost)this).OnActivationChanged(focused);
    }

    void IWindowHost.OnVisibilityChanged(bool visible)
    {

        isVisible = visible;
        if (!visible) CloseNativePopups();
        compositor?.SetVisible(visible);
        Scene.Transitions.Suspended = !visible;
        RefreshSystemButtons();
        if (visible) RequestFrame();
    }

    void IWindowHost.OnSystemSettingsChanged()
    {
        if (platformWindow is null) return;
        ApplyPreferences();
        ApplyInputPreferences();
        RefreshSystemButtons();
        RequestFrame(WindowFrameCause.SystemSettings);
    }

    private void ApplyInputPreferences()
    {
        Input.TextPolicy = PlatformWindow.TextInputPolicy;
        Input.WheelScrollDistance = PlatformWindow.WheelScrollDistance;
        Input.WheelScrollByPage = PlatformWindow.WheelScrollByPage;
    }

    /// <summary>Gets the safe-area insets and keyboard occlusion relative to the client viewport.</summary>
    public WindowEnvironment Environment => platformWindow?.Environment ?? default;

    /// <summary>Occurs when environment changes.</summary>
    public event Action? EnvironmentChanged;
    void IWindowHost.OnEnvironmentChanged()
    {
        EnvironmentChanged?.Invoke();
        RequestFrame(WindowFrameCause.Resize);
    }

    void IWindowHost.OnPointerScroll(Point point, float deltaX, float deltaY, ScrollDeltaUnit unit, KeyModifiers modifiers)
    {
        if (Instrumentation?.InterceptsPointer != true) Input.PointerWheel(point, deltaX, deltaY, modifiers, unit);
    }

    internal void SetColorScheme(ColorScheme value)
    {
        Dispatcher.AssertAccess();
        colorScheme = value;
        ApplyPreferences();
        RequestFrame(WindowFrameCause.SystemSettings);
    }

    private void ApplyPreferences()
    {
        var reduced = reducedMotion ?? PlatformWindow.PrefersReducedMotion;
        Scene.Transitions.ReducedMotion = reduced;
        var scheme = colorScheme == global::Nazor.Style.ColorScheme.System
            ? (PlatformWindow.PrefersDarkColorScheme ? global::Nazor.Style.ColorScheme.Dark : global::Nazor.Style.ColorScheme.Light) : colorScheme;
        var contrast = highContrast ?? PlatformWindow.PrefersHighContrast;
        var colors = (systemColors ?? PlatformWindow.SystemColors).ToDictionary(p => p.Key,
            p => new Style.Color(p.Value.R, p.Value.G, p.Value.B, p.Value.A), StringComparer.OrdinalIgnoreCase);
        Scene.Styles.Preferences = new MediaPreferences(scheme, reduced, contrast,
            global::Nazor.Style.SystemColors.Resolve(colors, scheme, PlatformWindow.ColorProfile), PlatformWindow.Platform, ResolvedDensity);

        PlatformWindow.SetDarkFrame(scheme == global::Nazor.Style.ColorScheme.Dark);
    }

    WindowHitTest IWindowHost.HitTest(Point point) => Input.RegionAt(point) switch
    {
        WindowRegion.Caption => WindowHitTest.Caption,
        WindowRegion.MaximizeButton => WindowHitTest.MaximizeButton,
        _ => WindowHitTest.Client,
    };

    void IWindowHost.OnStateChanged(WindowState state)
    {
        RefreshSystemButtons();
        StateChanged?.Invoke();
        NotifyPresentation();
    }

    void IWindowHost.OnAlwaysOnTopChanged(bool value)
    {
        if (isAlwaysOnTop == value) return;
        isAlwaysOnTop = value;
        NotifyPresentation();
    }

    bool IWindowHost.OnClosing()
    {
        SavePlacement();
        Closing?.Invoke();
        return true;
    }

    private void RestorePlacement()
    {
        if (placementKey is null || placementStore is null) return;
        try
        {
            if (placementStore.Load(placementKey) is { } placement) PlatformWindow.SetPlacement(placement);
        }
        catch (Exception exception)
        {
            Log(LogLevel.Warning, $"The placement of the window '{placementKey}' could not be read.", exception);
        }
    }

    private void SavePlacement()
    {
        if (placementSaved || placementKey is null || placementStore is null || platformWindow is not { IsClosed: false } window) return;
        placementSaved = true;
        try
        {
            if (window.GetPlacement() is { } placement) placementStore.Save(placementKey, placement);
        }
        catch (Exception exception)
        {
            Log(LogLevel.Warning, $"The placement of the window '{placementKey}' could not be saved.", exception);
        }
    }

    void IWindowHost.OnClosed()
    {
        Dispose();
        Closed?.Invoke();
    }

    SemanticTree? IWindowHost.GetSemanticTree() => disposed || platformWindow is null ? null : Semantics;

    PlatformCursor IWindowHost.GetCursor(Point point) => Instrumentation?.PointerCursor ?? Input.CursorAt(point) switch
    {
        Cursor.Pointer => PlatformCursor.Hand,
        Cursor.Text => PlatformCursor.IBeam,
        Cursor.ResizeHorizontal => PlatformCursor.SizeWestEast,
        Cursor.ResizeVertical => PlatformCursor.SizeNorthSouth,
        Cursor.Move => PlatformCursor.SizeAll,
        Cursor.NotAllowed => PlatformCursor.No,
        Cursor.Wait => PlatformCursor.Wait,
        Cursor.Progress => PlatformCursor.AppStarting,
        Cursor.Help => PlatformCursor.Help,
        Cursor.Crosshair => PlatformCursor.Cross,
        Cursor.ResizeNorthwestSoutheast => PlatformCursor.SizeNorthwestSoutheast,
        Cursor.ResizeNortheastSouthwest => PlatformCursor.SizeNortheastSouthwest,
        _ => PlatformCursor.Arrow,
    };

    private void ApplyCaptionAppearance()
    {
        if (platformWindow is null) return;
        var rootStyle = WindowRootStyle;
        if (platformWindow.UsesClientFrameAppearance)
        {
            var useDefaults = platformWindow.HasDefaultClientFrameAppearance;
            var radius = new CornerRadius(Corner(StyleProperties.WindowFrameBorderTopLeftRadius), Corner(StyleProperties.WindowFrameBorderTopRightRadius),
                Corner(StyleProperties.WindowFrameBorderBottomRightRadius), Corner(StyleProperties.WindowFrameBorderBottomLeftRadius));
            var shadows = rootStyle.Get(StyleProperties.WindowFrameBoxShadow).Shadows is { } value
                ? value.Items.Where(shadow => !shadow.Inset).Select(shadow => new DrawShadow(default, default,
                    new((float)shadow.X.Resolve(0), (float)shadow.Y.Resolve(0)), NonnegativeLength(shadow.Blur), (float)shadow.Spread.Resolve(0),
                    ToGraphicsColor(shadow.Color), false)).ToArray()
                : useDefaults ? [DefaultClientFrameShadow] : Array.Empty<DrawShadow>();
            platformWindow.SetClientFrameAppearance(radius, shadows);

            Radius Corner(StyleProperty<SizeValue> property)
            {
                var length = FrameLength(rootStyle.Get(property)) ?? (useDefaults ? 10 : 0);
                return new(length, length);
            }
        }
        platformWindow.SetFrameResizeGeometry(FrameLength(rootStyle.Get(StyleProperties.WindowFrameResizeInset)),
            FrameLength(rootStyle.Get(StyleProperties.WindowFrameResizeOutset)), FrameLength(rootStyle.Get(StyleProperties.WindowFrameResizeCornerLength)));
        platformWindow.SetResizeGrip(rootStyle.Get(StyleProperties.WindowResizeGrip) && rootStyle.Get(StyleProperties.WindowResizeGripColor).A != 0);
        if (platformWindow.UsesWindowsDecorations && windowElement is not null)
        {
            platformWindow.SetWindowsTitleBarHeight(rootStyle.Get(StyleProperties.WindowsTitleBarHeight));
            ApplyBadgeAppearance(rootStyle);
            static Graphics.Color? Native(Style.NativeColor value) => value.Value is { } color
                ? new Graphics.Color(color.R, color.G, color.B, color.A) : null;
            platformWindow.SetWindowsTitleBarColors(new()
            {
                BackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarBackgroundColor)),
                ForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarForegroundColor)),
                InactiveBackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarInactiveBackgroundColor)),
                InactiveForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarInactiveForegroundColor)),
                ButtonBackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonBackgroundColor)),
                ButtonForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonForegroundColor)),
                ButtonInactiveBackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonInactiveBackgroundColor)),
                ButtonInactiveForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonInactiveForegroundColor)),
                ButtonHoverBackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonHoverBackgroundColor)),
                ButtonHoverForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonHoverForegroundColor)),
                ButtonPressedBackgroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonPressedBackgroundColor)),
                ButtonPressedForegroundColor = Native(rootStyle.Get(StyleProperties.WindowsTitleBarButtonPressedForegroundColor)),
            });
        }
        RefreshSystemButtons();
        RefreshCapabilities();
    }

    private static float? FrameLength(SizeValue value) => value.Kind == SizeKind.Fixed ? NonnegativeLength(value.Length) : null;

    private static float NonnegativeLength(Length length) => length.Resolve(0) is var value && double.IsFinite(value)
        ? (float)Math.Clamp(value, 0, float.MaxValue) : 0;

    private static Graphics.Color ToGraphicsColor(Style.Color color) => new(color.R, color.G, color.B, color.A);

    private ElementNode? ValidateWindowRoot(bool? declared)
    {
        if (contentFrame is not null) return contentFrame.Nodes.OfType<ElementNode>().Single();
        if (Root.Component is WindowComponentBase)
        {
            if (declared is false) throw new InvalidOperationException("Content-only hosting cannot use a component with @window.");
            return Root.Nodes.OfType<ElementNode>().Single();
        }
        if (declared is true) throw new InvalidOperationException("UseMainWindow and Open require a component with @window.");
        return null;
    }

    internal void UpdateAppearance(IApplicationAppearance appearance)
    {
        Dispatcher.AssertAccess();
        Scene.Styles.Theme = appearance.Theme ?? Themes.Themes.Basic;
        Scene.Styles.Application = appearance.StyleSheet;
        colorScheme = appearance.ColorScheme;
        reducedMotion = appearance.ReducedMotion;
        highContrast = appearance.HighContrast;
        applicationDensity = appearance.ControlDensity;
        ApplyPreferences();
        NotifyPresentation();
        RequestFrame(WindowFrameCause.SystemSettings);
    }

    private void ValidateTitleBars()
    {
        if (DecorationOwner is { } component)
        {
            if (component.TitleBarCount > 1) throw new InvalidOperationException("A window can declare at most one UserTitleBar per render.");
            if (component.CaptionButtonsCount > 1) throw new InvalidOperationException("A window can declare at most one UserCaptionButtons per render.");
            return;
        }

    }

    private void OnBatchCompleted(object? sender, RenderBatch batch)
    {
        if (Root is not null)
        {
            windowElement = ValidateWindowRoot(declaredWindow);
            ApplyWindowActivation();
        }
        if (DecorationOwner is not null)
        {
            ValidateTitleBars();
            if (AppliedDecorations is { Configuration: var configuration }) ValidateDecorationContent(configuration);
            if (windowElement is not null) windowElement.SetAttribute("stylesheet", windowStyleSheet);
            if (!batch.IsEmpty) RequestFrame(WindowFrameCause.RenderBatch);
            return;
        }

        if (!batch.IsEmpty) RequestFrame(WindowFrameCause.RenderBatch);
    }

    private void OnDispatcherException(object? sender, Exception exception) => OnException(exception);

    private void OnException(Exception exception)
    {
        Exceptions.Add(exception);
        if (logger is not null) logger.LogError(exception, "Unhandled exception in a component, an event handler or dispatched work.");
        else Console.Error.WriteLine($"Nazor: unhandled exception: {exception}");
    }

    private void OnDeltaApplied(Type[]? updatedTypes) => Dispatcher.Post(() =>
    {
        if (disposed) return;
        try
        {

            Scene.Styles.InvalidateAll();
            Renderer.RenderOnHotReload();
        }
        catch (Exception exception)
        {
            Log(LogLevel.Warning, "Hot reload: the window could not be re-rendered.", exception);
        }
        finally
        {
            HotReloadCount++;
        }
        RequestFrame(WindowFrameCause.HotReload);
        Log(LogLevel.Information, updatedTypes is null ? "Hot reload applied (all types)." : $"Hot reload applied ({updatedTypes.Length} type(s)).");
        if (StyleHotReload.LastError is { } styleError) Log(LogLevel.Warning, "Hot reload: a scoped stylesheet could not be refreshed.", styleError);
        if (HotReloadManager.LastError is { } hostError) Log(LogLevel.Warning, "Hot reload: a subscriber failed.", hostError);
        HotReloadApplied?.Invoke(updatedTypes);
    });

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (logger is not null) logger.Log(level, exception, "{Message}", message);
        else if (level >= LogLevel.Warning) Console.Error.WriteLine(exception is null ? $"Nazor: {message}" : $"Nazor: {message} {exception}");
    }

    /// <summary>Removes the root component (disposing every component) and releases rendering resources.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        decorationLifetime.Cancel();
        Chrome = default;
        FramePresented = null;
        Interlocked.Exchange(ref pendingPresentation,null);
        cachedDisplayList = null;
        CloseNativePopups();
        StopCompositor();
        iconLoad?.Cancel();
        iconLoad?.Dispose();
        iconLoad = null;

        SavePlacement();

        HotReloadManager.DeltaApplied -= OnDeltaApplied;
        Dispatcher.UnhandledException -= OnDispatcherException;
        lock (openWindowsGate) openWindows.Remove(this);
        Renderer.BatchCompleted -= OnBatchCompleted;
        Instrumentation = null;
        semantics?.Dispose();
        Input.VisualStateChanged -= OnVisualStateChanged;
        Input.Dispose();
        if (platformWindow is not null) Input.PointerCaptureChanged -= platformWindow.SetPointerCapture;
        Renderer.Dispose();
        Scene.Dispose();
        graphics.Dispose();
        windowElement = null;
        if (platformWindow is { IsClosed: false }) platformWindow.Dispose();
    }
}
