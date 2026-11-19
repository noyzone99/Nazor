using System.Runtime.InteropServices;
using System.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform.Windows.Automation;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window : IPlatformWindow, IMessageTarget, INativePopupOwner
{
    private nint gpuWindow;
    private (int Width, int Height) gpuWindowSize;

    public IOpenGlWindowContext CreateOpenGlContext()
    {
        if (gpuWindow != 0) throw new InvalidOperationException("A GPU surface already exists for this window.");
        GetClientRect(hwnd, out var client);
        fixed (char* name = Win32Platform.ClassName)
            gpuWindow = CreateWindowExW(WS_EX_NOPARENTNOTIFY | WS_EX_TRANSPARENT, name, null, WS_CHILD | WS_VISIBLE | WS_DISABLED,
                0, 0, Math.Max(1, client.right), Math.Max(1, client.bottom), hwnd, 0, Win32Platform.Instance, 0);
        if (gpuWindow == 0) throw new GraphicsDeviceException("Cannot create the OpenGL presentation window.");
        gpuWindowSize = (Math.Max(1, client.right), Math.Max(1, client.bottom));
        if (!SetPropW(gpuWindow, "UIA_WindowVisibilityOverridden", 2) ||
            !SetWindowPos(gpuWindow, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE))
        {
            RemovePropW(gpuWindow, "UIA_WindowVisibilityOverridden");
            DestroyWindow(gpuWindow);
            gpuWindow = 0;
            throw new GraphicsDeviceException("Cannot configure the OpenGL presentation window.");
        }
        return new WglWindowContext(gpuWindow, () =>
        {
            if (gpuWindow != 0)
            {
                RemovePropW(gpuWindow, "UIA_WindowVisibilityOverridden");
                DestroyWindow(gpuWindow);
            }
            gpuWindow = 0;
            gpuWindowSize = default;
        });
    }

    public void ResetRendering()
    {
        GetClientRect(hwnd, out var client);
        if (client.right > 0 && client.bottom > 0) EnsureBuffer(client.right, client.bottom);
        Invalidate();
    }
    [LibraryImport("comctl32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(nint hwnd, delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nuint, nuint, nint> callback, nuint id, nuint data);
    [LibraryImport("comctl32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveWindowSubclass(nint hwnd, delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nuint, nuint, nint> callback, nuint id);
    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint hwnd, uint message, nint wParam, nint lParam);

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static nint CaptionProcedure(nint hwnd, uint message, nint wParam, nint lParam, nuint id, nuint data)
    {
        try
        {
            var window = (Win32Window)GCHandle.FromIntPtr((nint)data).Target!;

            if (message == WM_NCHITTEST && !window.IsNativeCaptionPoint(lParam))
                return window.fullScreen ? HTCLIENT : window.HitTest(hwnd, wParam, lParam);
            if (message == WM_NCMOUSEMOVE && wParam == HTCAPTION && !window.IsNativeCaptionPoint(lParam))
                return window.OnMessage(hwnd, message, wParam, lParam);
            if ((message == WM_NCRBUTTONUP && wParam == HTCAPTION) ||
                (wParam == HTMAXBUTTON && !window.IsNativeCaptionPoint(lParam) &&
                    message is WM_NCMOUSEMOVE or WM_NCLBUTTONDOWN or WM_NCLBUTTONUP or WM_NCLBUTTONDBLCLK or
                        WM_NCRBUTTONDOWN or WM_NCRBUTTONUP or WM_NCRBUTTONDBLCLK or WM_NCMBUTTONDOWN or WM_NCMBUTTONUP or WM_NCMBUTTONDBLCLK))
                return window.OnMessage(hwnd, message, wParam, lParam);
            if (message == WM_NCDESTROY) RemoveWindowSubclass(hwnd, &CaptionProcedure, id);
        }
        catch (Exception exception) { Console.Error.WriteLine($"Nazor: caption input failed: {exception}"); }
        return DefSubclassProc(hwnd, message, wParam, lParam);
    }
    private readonly IWindowHost host;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private readonly Dictionary<uint, Action> systemMenuCommands = [];
    private uint nextSystemMenuCommand = 0x1000;
    private WindowTitleBarMenuMode titleBarMenuMode;
    private readonly Win32ApplicationLauncher? launcher;
    internal bool TaskbarReady { get; set; }
    private GCHandle self;
    private nint hwnd;
    private uint dpi;
    private byte* pixels;
    private int bufferWidth;
    private int bufferHeight;
    private bool trackingMouse;
    private bool releasingCapture;
    private bool explicitPointerCapture;
    private char pendingHighSurrogate;
    private Point lastPointer;
    private string title;
    private Rect? textCaret;
    private bool imeEnabled = true;
    private bool systemCaret;
    private bool composing;
    private AutomationBridge? automation;

    internal const uint WM_NAZOR_AUTOMATION = WM_APP + 10;

    internal const uint WM_NAZOR_AUTOMATION_CALL = WM_APP + 11;

    private const nuint AnimationTimer = 1;
    private const uint WM_NAZOR_ANIMATION = WM_APP + 12;
    private long? animationDeadline;
    private long paintStarted;
    private nint animationTimer, animationWait, animationMonitor;
    private bool useLegacyAnimationTimer;
    private TimeSpan refreshInterval = TimeSpan.FromSeconds(1d / 60);
    private bool minimized;
    private readonly uint style;
    private readonly uint exStyle;
    private bool clientFrame;
    private bool isAlwaysOnTop;
    internal bool IsVisibleInTaskbar { get; private set; } = true;
    private readonly nint owner;
    private bool showMaximized;
    private bool extended;
    private bool modernWindowing;
    private readonly Func<bool>? initializeWindowing;
    private bool showSystemButtons;
    private bool darkFrame;
    private WindowOperations operations;
    private bool showResizeGrip;
    private WindowsTitleBar? nativeTitleBar;
    private WindowsTitleBarHeight titleBarHeight;
    private WindowsTitleBarColors titleBarColors = new();
    private WindowsBackdrop? backdrop;
    private readonly bool transparentBackground;
    private Win32AlphaSurface? alphaSurface;
    private bool trackingNonClient;
    private WindowState state;
    private bool fullScreen;
    private bool changingState;
    private WINDOWPLACEMENT beforeFullScreen;
    private nint savedStyle;
    private nint savedExStyle;
    private uint FrameExStyle => hwnd == 0 ? exStyle : fullScreen ? (uint)savedExStyle : (uint)GetWindowLongPtrW(hwnd, -20);

    public Win32Window(IWindowHost host, WindowOptions options, bool modernWindowing = false, Func<bool>? initializeWindowing = null, Win32ApplicationLauncher? launcher = null)
    {
        ArgumentNullException.ThrowIfNull(options.Decorations);
        this.launcher = launcher;
        this.host = host;
        this.initializeWindowing = initializeWindowing;
        title = options.Title;
        decorations = options.Decorations;
        clientFrame = decorations?.Frame == WindowFrameOwner.Nazor;
        suppressFrameInput = false;
        transparentBackground = options.BackgroundMode == BackgroundMode.Transparent;
        isAlwaysOnTop = options.IsAlwaysOnTop;
        IsVisibleInTaskbar = options.IsVisibleInTaskbar;
        self = GCHandle.Alloc(this);

        (style, exStyle) = options.IsDialog ? (WS_DIALOGWINDOW, WS_EX_DLGMODALFRAME) : (WS_OVERLAPPEDWINDOW, 0u);
        if (clientFrame) (style, exStyle) = (0x80080000u, 0u); // A popup has no native frame or shadow.
        if (!IsVisibleInTaskbar) exStyle |= WS_EX_TOOLWINDOW;
        if (options.IsAlwaysOnTop) exStyle |= WS_EX_TOPMOST;
        if (transparentBackground) exStyle |= WS_EX_NOREDIRECTIONBITMAP;

        style |= 0x02000000  ;
        var defaults = options.IsDialog ? WindowOperations.Dialog : WindowOperations.Default;
        operations = new(options.IsMinimizable ?? defaults.IsMinimizable, options.IsMaximizable ?? defaults.IsMaximizable,
            options.IsResizable ?? defaults.IsResizable, options.FullScreenEnabled ?? defaults.FullScreenEnabled);
        style = OperationsStyle(style, operations with { IsResizable = operations.IsResizable && !suppressFrameInput });
        extended = !options.Decorations.HasNativeTitleBar;
        this.modernWindowing = modernWindowing;
        showSystemButtons = options.Decorations.CaptionButtons == WindowCaptionButtons.Native;
        if (extended && !showSystemButtons && !modernWindowing) style &= ~WS_CAPTION;
        fixed (char* name = Win32Platform.ClassName)

            hwnd = CreateWindowExW(exStyle, name, options.Title, style, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
                options.Owner?.Handle ?? 0, 0, Win32Platform.Instance, GCHandle.ToIntPtr(self));
        if (hwnd == 0)
        {
            self.Free();
            throw new InvalidOperationException($"CreateWindowEx failed (error {Marshal.GetLastPInvokeError()}).");
        }
        try
        {
            if (transparentBackground) alphaSurface = new Win32AlphaSurface(hwnd);
            dpi = Math.Max(96, GetDpiForWindow(hwnd));
            Clipboard = new Win32Clipboard(this);
            Resize(new Size((float)options.Width, (float)options.Height));
            if (extended && !clientFrame && modernWindowing)
            {
                nativeTitleBar = WindowsTitleBar.Create(hwnd);
                ApplyNativeCaption();
                if (!SetWindowSubclass(hwnd, &CaptionProcedure, 1, (nuint)GCHandle.ToIntPtr(self)))
                    throw new InvalidOperationException("Cannot attach the custom title bar input handler.");
            }

            if (extended) SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            ApplyFrame();
            owner = options.Owner?.Handle ?? 0;
            Center(options.Centering);
            SetTextInputCaret(null);
            launcher?.Register(this);
        }
        catch { DestroyWindow(hwnd); throw; }
    }

    public void Center(WindowCentering centering)
    {
        if (centering == WindowCentering.None || hwnd == 0 || IsClosed) return;
        var on = centering == WindowCentering.Owner ? owner : 0;
        GetWindowRect(hwnd, out var own);
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfo(MonitorFromWindow(on != 0 ? on : hwnd, MONITOR_DEFAULTTONEAREST), ref info)) return;
        var target = on != 0 && GetWindowRect(on, out var ownerRect) ? ownerRect : info.rcWork;
        var (width, height) = (own.right - own.left, own.bottom - own.top);
        var x = Math.Clamp(target.left + (target.right - target.left - width) / 2, info.rcWork.left, Math.Max(info.rcWork.left, info.rcWork.right - width));
        var y = Math.Clamp(target.top + (target.bottom - target.top - height) / 2, info.rcWork.top, Math.Max(info.rcWork.top, info.rcWork.bottom - height));
        SetWindowPos(hwnd, 0, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    public void SetEnabled(bool enabled)
    {
        if (hwnd != 0 && !IsClosed) EnableWindow(hwnd, enabled);
    }

    public IClipboard Clipboard { get; }

    public void SetPointerCapture(bool capture)
    {
        explicitPointerCapture = capture;
        if (capture) SetCapture(hwnd);
        else
        {
            releasingCapture = true;
            if (GetCapture() == hwnd) ReleaseCapture();
            releasingCapture = false;
        }
    }

    public void SetTextInputCaret(Rect? caret)
    {
        textCaret = caret;
        if (hwnd == 0) return;
        if (caret is null)
        {
            if (systemCaret) DestroyCaret();
            systemCaret = false;

            if (imeEnabled) ImmAssociateContextEx(hwnd, 0, 0);
            imeEnabled = false;
            return;
        }
        if (!imeEnabled) ImmAssociateContextEx(hwnd, 0, IACE_DEFAULT);
        imeEnabled = true;
        PositionCaret();
    }

    private void PositionCaret()
    {
        if (textCaret is not { } caret || hwnd == 0) return;
        var x = (int)MathF.Round(caret.X * Scale);
        var top = (int)MathF.Round(caret.Y * Scale);
        var bottom = (int)MathF.Round(caret.Bottom * Scale);

        if (!systemCaret && GetFocus() == hwnd) systemCaret = CreateCaret(hwnd, 0, 1, Math.Max(1, bottom - top));
        if (systemCaret) SetCaretPos(x, top);
        var himc = ImmGetContext(hwnd);
        if (himc == 0) return;
        try
        {
            var candidate = new CANDIDATEFORM
            {
                dwStyle = CFS_EXCLUDE,
                ptCurrentPos = new POINT { X = x, Y = top },
                rcArea = new RECT { left = x, top = top, right = x + 1, bottom = bottom + 1 },
            };
            ImmSetCandidateWindow(himc, &candidate);
            var composition = new COMPOSITIONFORM { dwStyle = CFS_POINT, ptCurrentPos = new POINT { X = x, Y = top } };
            ImmSetCompositionWindow(himc, &composition);
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }

    private string? ReadComposition(uint kind)
    {
        var himc = ImmGetContext(hwnd);
        if (himc == 0) return null;
        try
        {
            var bytes = ImmGetCompositionStringW(himc, kind, null, 0);
            if (bytes <= 0) return bytes == 0 ? "" : null;
            var buffer = new char[bytes / 2];
            fixed (char* pointer = buffer) bytes = ImmGetCompositionStringW(himc, kind, pointer, (uint)bytes);
            return bytes > 0 ? new string(buffer, 0, bytes / 2) : "";
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }

    private int ReadCompositionCursor()
    {
        var himc = ImmGetContext(hwnd);
        if (himc == 0) return 0;
        try
        {
            return Math.Max(0, ImmGetCompositionStringW(himc, GCS_CURSORPOS, null, 0));
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }

    public nint Handle => hwnd;
    public NativeWindowHandle? NativeHandle => IsClosed || hwnd == 0 ? null : new(hwnd, "HWND");
    public string? ExportedHandle => IsClosed ? null : "win32:" + ((nuint)hwnd).ToString("x");
    public bool SetOwner(string ownerHandle)
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Window ownership requires the UI thread.");
        if (IsClosed || !ownerHandle.StartsWith("win32:", StringComparison.Ordinal) ||
            !nuint.TryParse(ownerHandle.AsSpan(6), System.Globalization.NumberStyles.HexNumber, null, out var owner) ||
            owner == 0 || (nint)owner == hwnd || !IsWindow((nint)owner)) return false;
        Marshal.SetLastPInvokeError(0);
        return SetWindowLongPtrW(hwnd, -8, (nint)owner) != 0 || Marshal.GetLastPInvokeError() == 0;
    }

    public float Scale => dpi / 96f;

    public bool IsClosed { get; private set; }

    public Size ClientSize
    {
        get
        {
            if (hwnd == 0) return Size.Zero;
            GetClientRect(hwnd, out var rc);
            return new Size((rc.right - rc.left) / Scale, (rc.bottom - rc.top) / Scale);
        }
    }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            if (hwnd != 0) SetWindowTextW(hwnd, value);
        }
    }

    public void Show(bool activate = true)
    {

        ShowWindow(hwnd, showMaximized ? SW_SHOWMAXIMIZED : activate ? SW_SHOW : SW_SHOWNOACTIVATE);
        showMaximized = false;
        UpdateWindow(hwnd);
        launcher?.Refresh();
    }

    public void Hide()
    {
        launcher?.BeforeHide(this);
        if (hwnd != 0 && !IsClosed) ShowWindow(hwnd, SW_HIDE);
        launcher?.Refresh();
    }

    public void SetDarkFrame(bool dark)
    {
        if (hwnd == 0 || IsClosed) return;
        darkFrame = dark;
        var value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &value, sizeof(int));
        Win32MenuTheme.Apply(hwnd, dark, PrefersHighContrast);
        ApplyNativeCaption();
    }

    public WindowPlacementCapabilities PlacementCapabilities => new(true, true, false, true);
    public float WheelScrollDistance => WheelLines() is var lines && lines != uint.MaxValue ? lines * 16 : 0;
    public bool WheelScrollByPage => WheelLines() == uint.MaxValue;
    private static uint WheelLines()
    {
        uint lines = 3;
        SystemParametersInfoW(0x0068  , 0, &lines, 0);
        return lines;
    }

    public bool UsesWindowsDecorations => true;

    public BackdropResult SetBackdrop(BackdropSelection request) => ResolveBackdrop(request, TrySetBackdrop);

    internal static BackdropResult ResolveBackdrop(BackdropSelection request, Func<WindowsBackdrop?, bool> apply)
    {
        static WindowsBackdrop? Map(CommonBackdrop value) => value switch
        {
            CommonBackdrop.Solid => WindowsBackdrop.Mica,
            CommonBackdrop.Blur => WindowsBackdrop.Acrylic,
            _ => null,
        };
        var selected = request.Windows ?? Map(request.Preset ?? request.Default);
        if (apply(selected)) return new(selected.HasValue, selected?.ToString().ToLowerInvariant());
        var fallback = Map(request.Default);
        if (apply(fallback)) return new(fallback.HasValue, fallback?.ToString().ToLowerInvariant());
        apply(null);
        return default;
    }

    private bool TrySetBackdrop(WindowsBackdrop? material)
    {
        if (hwnd == 0 || IsClosed) return false;

        if (material.HasValue && !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621)) return false;
        var type = material switch { WindowsBackdrop.Mica => 2, WindowsBackdrop.MicaAlt => 4, WindowsBackdrop.Acrylic => 3, _ => 1 };
        if (DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, &type, sizeof(int)) < 0 && material.HasValue) return false;
        backdrop = material;
        ApplyFrame();
        ApplyNativeCaption();
        return true;
    }

    private void ApplyFrame()
    {
        if (hwnd == 0 || IsClosed) return;
        if (clientFrame)
        {
            clientFrameApplied = true;
            var disabled = 1; // DWMNCRP_DISABLED also prevents non-client shadows.
            DwmSetWindowAttribute(hwnd, 2, &disabled, sizeof(int));
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var square = 1; // DWMWCP_DONOTROUND
                var noBorder = 0xfffffffeu; // DWMWA_COLOR_NONE
                DwmSetWindowAttribute(hwnd, 33, &square, sizeof(int));
                DwmSetWindowAttribute(hwnd, 34, &noBorder, sizeof(uint));
            }
            DwmExtendFrameIntoClientArea(hwnd, default);
            return;
        }
        if (clientFrameApplied)
        {
            ApplyFrameRegion(0, 0, default);
            HideClientFrameShadow();
            HideResizeOutset();
            var policy = 0;
            DwmSetWindowAttribute(hwnd, 2, &policy, sizeof(int));
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var corner = 0;
                var border = 0xffffffffu;
                DwmSetWindowAttribute(hwnd, 33, &corner, sizeof(int));
                DwmSetWindowAttribute(hwnd, 34, &border, sizeof(uint));
            }
            clientFrameApplied = false;
        }
        var margin = backdrop is null ? 0 : -1;
        // AppWindow supplies the integrated controls; the DWM extension would put an opaque strip behind them.
        var top = margin == -1 ? -1 : extended && showSystemButtons && !fullScreen && !(transparentBackground && nativeTitleBar is not null)
            ? GetSystemMetricsForDpi(4  , dpi) + FrameThickness : 0;
        DwmExtendFrameIntoClientArea(hwnd, new MARGINS { Left = margin, Right = margin, Top = top, Bottom = margin });
        ApplySystemCaptionColor();
    }

    public PlatformKind? Platform => PlatformKind.Windows;

    private static readonly WindowButtonsLayout LeftControls = new([WindowButton.Close, WindowButton.Maximize, WindowButton.Minimize], []);

    public WindowButtonsLayout SystemButtonsLayout
    {
        get
        {

            var rtl = ((long)GetWindowLongPtrW(hwnd, -20  ) & 0x400000  ) != 0;
            return rtl ? LeftControls : WindowButtonsLayout.Default;
        }
    }

    public WindowButtonsMetrics SystemButtonsMetrics
    {
        get
        {
            if (!extended || !showSystemButtons || fullScreen || IsIconic(hwnd) || !IsWindowVisible(hwnd)) return default;
            if (nativeTitleBar is not null) return nativeTitleBar.Size(Scale);
            var bounds = NativeCaptionBounds();
            var measured = bounds.right > bounds.left && bounds.bottom > bounds.top;
            var width = measured ? (bounds.right - bounds.left) / Scale
                : GetSystemMetricsForDpi(30  , dpi) * ((style & 0x30000) != 0 ? 3 : 1) / Scale;
            var height = measured ? (bounds.bottom - bounds.top) / Scale : GetSystemMetricsForDpi(4  , dpi) / Scale;

            var left = measured && GetWindowRect(hwnd, out var outer)
                ? bounds.left + bounds.right < outer.right - outer.left
                : ((long)GetWindowLongPtrW(hwnd, -20  ) & 0x400000  ) != 0;
            return left ? new(width, 0, height) : new(0, width, height);
        }
    }

    public (float Width, float Height) SystemButtonsSize => (SystemButtonsMetrics.Width, SystemButtonsMetrics.Height);

    public WindowCapabilities Capabilities => new(operations.IsMinimizable, operations.IsMaximizable,
        operations.IsResizable && !suppressFrameInput, operations.FullScreenEnabled)
        {
            Decorations = DecorationSupport with { Current = decorations },
            CanSetWindowIcon = true, CanSetAlwaysOnTop = true,
            CanSetTaskbarVisibility = true, CanUseTransparentBackground = true,
            CanShowPopupsOutsideWindow = !IsClosed,
            Minimize = new(true, NativeRestriction.Enforced),
            Maximize = new(true, NativeRestriction.Enforced),
            FullScreen = new(true),
            CanObserveMinimized = true,
            CanRestoreFromMinimized = true,
            CanSetScreenPosition = true,
            InteractiveResize = new(!suppressFrameInput, NativeRestriction.Enforced),
        };

    public WindowChrome Chrome
    {
        get
        {
            if (IsClosed || suppressFrameInput || fullScreen) return default;
            if (decorations is { } configured)
                return new(configured.Frame == WindowFrameOwner.Native ? WindowChromeOwner.System : WindowChromeOwner.Nazor,
                    configured.CaptionButtons switch { WindowCaptionButtons.Native => WindowChromeOwner.System, WindowCaptionButtons.Nazor => WindowChromeOwner.Nazor, WindowCaptionButtons.User => WindowChromeOwner.User, _ => WindowChromeOwner.None },
                    titleBarMenuMode switch { WindowTitleBarMenuMode.Nazor => WindowChromeOwner.Nazor, WindowTitleBarMenuMode.None => WindowChromeOwner.None, _ => WindowChromeOwner.System })
                { TitleBar = configured.HasNativeTitleBar ? WindowChromeOwner.System : configured.TitleBar switch { WindowTitleBarContent.Nazor => WindowChromeOwner.Nazor, WindowTitleBarContent.User => WindowChromeOwner.User, _ => WindowChromeOwner.None } };
            var menu = titleBarMenuMode switch
            {
                WindowTitleBarMenuMode.Nazor => WindowChromeOwner.Nazor,
                WindowTitleBarMenuMode.None => WindowChromeOwner.None,
                _ => WindowChromeOwner.System,
            };
            return new(extended ? WindowChromeOwner.Nazor : WindowChromeOwner.System,
                !extended || showSystemButtons ? WindowChromeOwner.System : WindowChromeOwner.None, menu);
        }
    }

    public void SetTitleBarMenuMode(WindowTitleBarMenuMode value) => titleBarMenuMode = value;

    private static uint OperationsStyle(uint value, WindowOperations requested)
    {
        value &= ~0x70000u;
        if (requested.IsMinimizable) value |= 0x20000;
        if (requested.IsMaximizable) value |= 0x10000;
        if (requested.IsResizable) value |= 0x40000;
        return value;
    }

    public void SetOperations(WindowOperations requested)
    {
        if (operations == requested || hwnd == 0 || IsClosed) return;
        operations = requested;
        var effective = requested with { IsResizable = requested.IsResizable && !clientFrame };
        if (fullScreen) savedStyle = (nint)OperationsStyle((uint)savedStyle, effective);
        else
        {
            var size = ClientSize;
            SetWindowLongPtrW(hwnd, -16, (nint)OperationsStyle((uint)GetWindowLongPtrW(hwnd, -16), effective));
            SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            Resize(size);
        }
        ApplyNativeCaption();
        host.OnCapabilitiesChanged();
        Invalidate();
    }

    public void SetResizeGrip(bool visible) => showResizeGrip = visible;

    public bool SetWindowsTitleBarHeight(WindowsTitleBarHeight height)
    {
        if (clientFrame) return false;
        if (!Enum.IsDefined(height)) throw new ArgumentOutOfRangeException(nameof(height));
        if (titleBarHeight == height) return nativeTitleBar is not null;
        titleBarHeight = height;
        ApplyNativeCaption();
        return nativeTitleBar is not null;
    }

    private void ApplyNativeCaption()
    {
        var colors = titleBarColors;
        if (extended && !PrefersHighContrast)
        {
            var clear = new Color(0, 0, 0, 0);
            colors = colors with
            {
                ButtonBackgroundColor = colors.ButtonBackgroundColor ?? clear,
                ButtonInactiveBackgroundColor = colors.ButtonInactiveBackgroundColor ?? clear,
            };
        }
        else if (!extended)
        {
            Color? defaultBackground = backdrop is null && !PrefersHighContrast ? SolidCaptionColor : null;
            var background = colors.BackgroundColor ?? defaultBackground;
            var inactiveBackground = colors.InactiveBackgroundColor ?? defaultBackground;
            // The system caption has no surface behind its buttons: Windows paints a translucent button background
            // over black or white. Compose it over the bar, so a transparent button sits on the bar's own color.
            colors = colors with
            {
                BackgroundColor = background,
                InactiveBackgroundColor = inactiveBackground,
                ButtonBackgroundColor = Over(colors.ButtonBackgroundColor, background) ?? background,
                ButtonInactiveBackgroundColor = Over(colors.ButtonInactiveBackgroundColor, inactiveBackground) ?? inactiveBackground,
                ButtonHoverBackgroundColor = Over(colors.ButtonHoverBackgroundColor, background),
                ButtonPressedBackgroundColor = Over(colors.ButtonPressedBackgroundColor, background),
            };
        }
        nativeTitleBar?.SetOperations(fullScreen ? new(false, false, false, operations.FullScreenEnabled) : operations);
        nativeTitleBar?.Apply(extended, showSystemButtons && !fullScreen, titleBarHeight, colors, darkFrame);
        ApplySystemCaptionColor();
    }

    /// <summary>Composes a translucent color over an opaque one; an opaque, missing or unbacked color is returned as is.</summary>
    internal static Color? Over(Color? top, Color? bottom)
    {
        if (top is not { } color || color.A == 255 || bottom is not { A: 255 } under) return top;
        var alpha = color.A / 255f;
        byte Mix(byte over, byte below) => (byte)MathF.Round(over * alpha + below * (1 - alpha));
        return new Color(Mix(color.R, under.R), Mix(color.G, under.G), Mix(color.B, under.B), 255);
    }

    private Color SolidCaptionColor => darkFrame ? new(0x1e, 0x21, 0x23, 255) : new(0xee, 0xf4, 0xf9, 255);

    private void ApplySystemCaptionColor()
    {
        if (hwnd == 0 || IsClosed || clientFrame || nativeTitleBar is not null || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        if (titleBarColors.BackgroundColor is not null || titleBarColors.InactiveBackgroundColor is not null) return;
        // A solid caption without a backdrop; COLOR_DEFAULT lets an explicit material cover it.
        var solid = SolidCaptionColor;
        var color = !extended && backdrop is null && !PrefersHighContrast
            ? (uint)(solid.R | solid.G << 8 | solid.B << 16)
            : 0xffffffffu;
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, &color, sizeof(uint));
    }

    public void SetWindowsTitleBarColors(WindowsTitleBarColors colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (clientFrame) return;
        if (titleBarColors == colors) return;
        titleBarColors = colors;
        // Release AppWindow's caption renderer when custom-theme rules stop matching.
        if (!extended && nativeTitleBar is not null && colors == new WindowsTitleBarColors())
            ConfigureTitleBar();
        if (nativeTitleBar is null && colors != new WindowsTitleBarColors() && !fullScreen)
        {
            if (!modernWindowing) modernWindowing = initializeWindowing?.Invoke() ?? false;
            if (modernWindowing) ConfigureTitleBar();
        }
        ApplyNativeCaption();
    }

    internal WindowsBadgeAppearance BadgeAppearance { get; private set; } = new();

    public void SetWindowsBadgeAppearance(WindowsBadgeAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        var previous = BadgeAppearance;
        if (previous == appearance with { FontFamilies = previous.FontFamilies, Shadows = previous.Shadows, TextShadows = previous.TextShadows }
            && previous.FontFamilies.SequenceEqual(appearance.FontFamilies) && previous.Shadows.SequenceEqual(appearance.Shadows)
            && previous.TextShadows.SequenceEqual(appearance.TextShadows)) return;
        BadgeAppearance = appearance with { FontFamilies = appearance.FontFamilies.ToArray(), Shadows = appearance.Shadows.ToArray(), TextShadows = appearance.TextShadows.ToArray() };
        launcher?.Refresh();
    }

    private RECT NativeCaptionBounds()
    {
        RECT bounds = default;
        if (hwnd != 0) DwmGetWindowAttribute(hwnd, DWMWA_CAPTION_BUTTON_BOUNDS, &bounds, (uint)sizeof(RECT));
        return bounds;
    }

    private bool IsNativeCaptionPoint(nint coordinates)
    {
        if (!showSystemButtons || fullScreen || !GetWindowRect(hwnd, out var window)) return false;
        if (nativeTitleBar is not null)
        {
            var point = ClientPointFromScreen(coordinates);
            var metrics = SystemButtonsMetrics;
            return point.Y >= 0 && point.Y < metrics.Height && point.X >= 0 && point.X < ClientSize.Width &&
                (point.X < metrics.LeftInset || point.X >= ClientSize.Width - metrics.RightInset);
        }
        var bounds = NativeCaptionBounds();
        var x = LowSigned(coordinates) - window.left;
        var y = HighSigned(coordinates) - window.top;
        return x >= bounds.left && x < bounds.right && y >= bounds.top && y < bounds.bottom;
    }

    public WindowState State => hwnd == 0 ? WindowState.Normal : IsIconic(hwnd) ? WindowState.Minimized : fullScreen ? WindowState.FullScreen : IsZoomed(hwnd) ? WindowState.Maximized : WindowState.Normal;

    public void SetState(WindowState target)
    {
        if (hwnd == 0 || IsClosed) return;
        if (!Enum.IsDefined(target)) throw new ArgumentOutOfRangeException(nameof(target));
        if ((target == WindowState.Minimized && !Capabilities.CanMinimize) ||
            (target == WindowState.Maximized && !Capabilities.CanMaximize) ||
            (target == WindowState.FullScreen && !fullScreen && !Capabilities.CanEnterFullScreen)) return;
        if (target == WindowState.FullScreen)
        {
            if (fullScreen)
            {
                if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
                FitFullScreen();
                return;
            }
            beforeFullScreen = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
            if (!GetWindowPlacement(hwnd, ref beforeFullScreen)) throw new InvalidOperationException("Cannot save window placement for full screen.");

            beforeFullScreen.showCmd = (uint)(IsZoomed(hwnd) || (IsIconic(hwnd) && (beforeFullScreen.flags & 2) != 0) ? SW_SHOWMAXIMIZED : SW_SHOWNOACTIVATE);
            savedStyle = GetWindowLongPtrW(hwnd, -16  );
            savedExStyle = GetWindowLongPtrW(hwnd, -20  );
            changingState = true;
            try
            {
                ShowWindow(hwnd, SW_RESTORE);
                fullScreen = true;

                SetWindowLongPtrW(hwnd, -16, (nint)((long)savedStyle & ~(WS_OVERLAPPEDWINDOW | 0x21000000L  )));
                SetWindowLongPtrW(hwnd, -20, (nint)((long)savedExStyle & ~WS_EX_DLGMODALFRAME));
                FitFullScreen();
            }
            finally { changingState = false; }
            ApplyFrame();
            ApplyNativeCaption();
            ReportState();
            return;
        }
        if (fullScreen && target != WindowState.Minimized)
        {
            ExitFullScreen();
            if (target == WindowState.Normal) return;
        }
        ShowWindow(hwnd, target switch { WindowState.Maximized => SW_MAXIMIZE, WindowState.Minimized => SW_MINIMIZE, _ => SW_RESTORE });
        ReportState();
    }

    private void FitFullScreen(nint monitorHandle = 0)
    {
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfo(monitorHandle != 0 ? monitorHandle : MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref monitor)) return;
        var area = monitor.rcMonitor;
        SetWindowPos(hwnd, 0, area.left, area.top, area.right - area.left, area.bottom - area.top,
            SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    public void ExitFullScreen()
    {
        if (!fullScreen || hwnd == 0 || IsClosed) return;
        changingState = true;
        try
        {
            fullScreen = false;
            ConfigureTitleBar();
            SetWindowLongPtrW(hwnd, -16, savedStyle);
            SetWindowLongPtrW(hwnd, -20, savedExStyle);

            var normal = beforeFullScreen.rcNormalPosition;
            if (MonitorFromRect(normal, MONITOR_DEFAULTTONULL) == 0)
            {
                var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
                if (GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref monitor))
                {
                    var width = Math.Min(normal.right - normal.left, monitor.rcWork.right - monitor.rcWork.left);
                    var height = Math.Min(normal.bottom - normal.top, monitor.rcWork.bottom - monitor.rcWork.top);
                    normal = new RECT { left = monitor.rcMonitor.left, top = monitor.rcMonitor.top,
                        right = monitor.rcMonitor.left + width, bottom = monitor.rcMonitor.top + height };
                    beforeFullScreen.rcNormalPosition = normal;
                }
            }
            if (!operations.IsMaximizable)
            {
                beforeFullScreen.showCmd = SW_SHOWNOACTIVATE;
                beforeFullScreen.flags &= ~2u;
            }
            SetWindowPlacement(hwnd, beforeFullScreen);
            SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
        finally { changingState = false; }
        ApplyFrame();
        ApplyNativeCaption();
        ReportState();
    }

    private int FrameThickness => GetSystemMetricsForDpi(SM_CYSIZEFRAME, dpi) + GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi);

    private Point ClientPointFromScreen(nint lParam)
    {
        var point = new POINT { X = LowSigned(lParam), Y = HighSigned(lParam) };
        ScreenToClient(hwnd, &point);
        return new Point(point.X / Scale, point.Y / Scale);
    }

    private nint HitTest(nint window, nint wParam, nint lParam)
    {
        var hit = DefWindowProcW(window, WM_NCHITTEST, wParam, lParam);
        if (!clientFrame && hit != HTCLIENT && (!extended || hit != HTCAPTION && hit != 3)) return hit;
        var point = new POINT { X = LowSigned(lParam), Y = HighSigned(lParam) };
        ScreenToClient(window, &point);
        var logicalPoint = new Point(point.X / Scale, point.Y / Scale);
        if (Capabilities.CanResize && !IsZoomed(window) && !IsIconic(window) && IsWindowEnabled(window) && GetCapture() == 0)
        {
            if (hit == HTCLIENT && ResizeGripBounds.Contains(logicalPoint)) return HTBOTTOMRIGHT;
            if (extended)
            {
                var resizeHit = ClientResizeHit(logicalPoint, point);
                if (resizeHit != 0) return resizeHit;
            }
        }
        if (!extended) return hit;
        return host.HitTest(logicalPoint) switch
        {
            WindowHitTest.Caption => HTCAPTION,
            WindowHitTest.MaximizeButton when Capabilities.CanMaximize && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) => HTMAXBUTTON,
            _ => HTCLIENT,
        };
    }

    private void ReportState()
    {
        if (changingState) return;
        var current = State;
        if (current == state) return;
        state = current;
        host.OnStateChanged(current);
    }

    public WindowPlacement? GetPlacement()
    {
        if (hwnd == 0 || IsClosed) return null;
        var placement = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (fullScreen) placement = beforeFullScreen;
        else if (!GetWindowPlacement(hwnd, ref placement)) return null;
        var normal = placement.rcNormalPosition;
        var border = new RECT();
        if (!clientFrame) AdjustWindowRectExForDpi(ref border, fullScreen ? (uint)savedStyle : (uint)GetWindowLongPtrW(hwnd, -16), false, FrameExStyle, dpi);
        if (extended) border.top = 0;
        var logicalSize = new Size((normal.right - normal.left - (border.right - border.left)) / Scale,
            (normal.bottom - normal.top - (border.bottom - border.top)) / Scale);
        if ((FrameExStyle & WS_EX_TOOLWINDOW) != 0)
        {
            var offset = MonitorWorkAreaOffset(normal);
            normal.left -= offset.X; normal.right -= offset.X;
            normal.top -= offset.Y; normal.bottom -= offset.Y;
        }
        return new WindowPlacement(normal.left, normal.top, normal.right - normal.left, normal.bottom - normal.top,
            placement.showCmd == SW_SHOWMAXIMIZED || (placement.flags & 2) != 0 || IsZoomed(hwnd)) { Scale = Scale, LogicalClientSize = logicalSize };
    }

    public bool SetPlacement(WindowPlacement placement)
    {
        if (hwnd == 0 || IsClosed || fullScreen || placement.Width <= 0 || placement.Height <= 0 || !Enum.IsDefined(placement.Coordinates) || !float.IsFinite(placement.Scale) || placement.Scale <= 0) return false;
        if (placement.Coordinates != WindowPlacementCoordinates.WindowsWorkAreaPixels)
            return RestoreLogicalSize(placement.LogicalClientSize);
        if (placement.X > int.MaxValue - (long)placement.Width || placement.Y > int.MaxValue - (long)placement.Height) return false;
        var normal = new RECT { left = placement.X, top = placement.Y, right = placement.X + placement.Width, bottom = placement.Y + placement.Height };

        if (MonitorFromRect(normal, MONITOR_DEFAULTTONULL) == 0) return RestoreLogicalSize(placement.LogicalClientSize);
        if ((FrameExStyle & WS_EX_TOOLWINDOW) != 0)
        {
            var offset = MonitorWorkAreaOffset(normal);
            normal.left += offset.X; normal.right += offset.X;
            normal.top += offset.Y; normal.bottom += offset.Y;
        }
        var value = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT), showCmd = SW_HIDE, rcNormalPosition = normal };
        if (!SetWindowPlacement(hwnd, value)) return false;
        if (GetPlacement()?.LogicalClientSize is { } restoredSize) Resize(restoredSize);
        showMaximized = placement.IsMaximized && operations.IsMaximizable;
        return true;
    }

    private bool RestoreLogicalSize(Size? size)
    {
        if (size is not { } logical || !float.IsFinite(logical.Width) || !float.IsFinite(logical.Height) || logical.Width <= 0 || logical.Height <= 0) return false;
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref monitor))
            logical = new Size(Math.Min(logical.Width, (monitor.rcWork.right - monitor.rcWork.left) / Scale),
                Math.Min(logical.Height, (monitor.rcWork.bottom - monitor.rcWork.top) / Scale));
        Resize(logical);
        return true;
    }

    public void Close()
    {
        if (hwnd != 0) PostMessageW(hwnd, WM_CLOSE, 0, 0);
    }

    public void Activate()
    {
        if (hwnd == 0 || IsClosed) return;
        if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
        SetForegroundWindow(hwnd);
    }

    public void Invalidate()
    {

        if (hwnd != 0) RedrawWindow(hwnd, null, 0, RDW_INTERNALPAINT);
    }

    public void RequestAnimationFrame(TimeSpan delay)
    {
        if (hwnd == 0) return;
        var now = Stopwatch.GetTimestamp();
        if (delay <= TimeSpan.Zero)
        {
            UpdateRefreshInterval();
            delay = refreshInterval - Stopwatch.GetElapsedTime(paintStarted, now);
        }
        delay = TimeSpan.FromMilliseconds(Math.Clamp(delay.TotalMilliseconds, 1, int.MaxValue));
        var deadline = now + (long)(delay.TotalSeconds * Stopwatch.Frequency);
        if (animationDeadline is { } pending && pending <= deadline) return;
        if (TryArmAnimationTimer(deadline))
        {
            animationDeadline = deadline;
            return;
        }
        var milliseconds = (uint)Math.Clamp(Math.Ceiling(delay.TotalMilliseconds), 10, int.MaxValue);
        if (SetTimer(hwnd, AnimationTimer, milliseconds, 0) != 0) animationDeadline = deadline;
        else { animationDeadline = null; Invalidate(); }
    }

    private bool TryArmAnimationTimer(long deadline)
    {
        if (useLegacyAnimationTimer) return false;
        if (animationTimer == 0)
        {
            const uint highResolution = 2, synchronizeAndModify = 0x00100002;
            animationTimer = CreateWaitableTimerExW(0, 0, highResolution, synchronizeAndModify);
            if (animationTimer != 0) animationWait = CreateThreadpoolWait(&AnimationReady, hwnd, 0);
        }
        if (animationWait != 0)
        {
            var due = -Math.Max(1, Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), deadline).Ticks);
            if (SetWaitableTimer(animationTimer, &due, 0, 0, 0, 0))
            {
                SetThreadpoolWait(animationWait, animationTimer, 0);
                return true;
            }
        }
        ReleaseAnimationTimer();
        useLegacyAnimationTimer = true;
        return false;
    }

    [UnmanagedCallersOnly]
    private static void AnimationReady(nint instance, nint window, nint wait, uint result) =>
        PostMessageW(window, WM_NAZOR_ANIMATION, 0, 0);

    private void ReleaseAnimationTimer()
    {
        if (animationWait != 0)
        {
            SetThreadpoolWait(animationWait, 0, 0);
            WaitForThreadpoolWaitCallbacks(animationWait, 1);
            CloseThreadpoolWait(animationWait);
            animationWait = 0;
        }
        if (animationTimer != 0)
        {
            CloseHandle(animationTimer);
            animationTimer = 0;
        }
    }

    private void UpdateRefreshInterval()
    {
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (animationMonitor == monitor) return;
        animationMonitor = monitor;
        refreshInterval = TimeSpan.FromSeconds(1d / 60);
        var info = new MONITORINFOEX { Info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFOEX) } };
        if (!GetMonitorInfo(monitor, ref info)) return;
        var dc = CreateDCW(null, info.Device, 0, 0);
        if (dc == 0) return;
        const int verticalRefresh = 116;
        var rate = GetDeviceCaps(dc, verticalRefresh);
        DeleteDC(dc);
        if (rate > 1) refreshInterval = TimeSpan.FromSeconds(1d / rate);
    }

    public bool PrefersReducedMotion
    {
        get
        {

            int enabled = 1;
            return SystemParametersInfoW(SPI_GETCLIENTAREAANIMATION, 0, &enabled, 0) && enabled == 0;
        }
    }

    public bool PrefersDarkColorScheme
    {
        get
        {

            uint light = 1, size = sizeof(uint);
            return RegGetValueW(HKEY_CURRENT_USER, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme",
                RRF_RT_REG_DWORD, null, &light, &size) == 0 && light == 0;
        }
    }

    public bool PrefersHighContrast
    {
        get
        {

            var contrast = new HIGHCONTRASTW { cbSize = (uint)sizeof(HIGHCONTRASTW) };
            return SystemParametersInfoW(SPI_GETHIGHCONTRAST, contrast.cbSize, &contrast, 0) && (contrast.dwFlags & HCF_HIGHCONTRASTON) != 0;
        }
    }

    public IReadOnlyDictionary<string, Color> SystemColors
    {
        get
        {
            static Color System(int index)
            {
                var color = GetSysColor(index);
                return new Color((byte)color, (byte)(color >> 8), (byte)(color >> 16), 255);
            }
            var (window, text, link, face, buttonText, highlight, highlightText) = (System(COLOR_WINDOW), System(COLOR_WINDOWTEXT),
                System(COLOR_HOTLIGHT), System(COLOR_BTNFACE), System(COLOR_BTNTEXT), System(COLOR_HIGHLIGHT), System(COLOR_HIGHLIGHTTEXT));
            var colors = new Dictionary<string, Color>
            {
                ["Canvas"] = window, ["CanvasText"] = text, ["Field"] = window, ["FieldText"] = text,
                ["ButtonFace"] = face, ["ButtonText"] = buttonText, ["ButtonBorder"] = buttonText,
                ["Highlight"] = highlight, ["HighlightText"] = highlightText, ["SelectedItem"] = highlight, ["SelectedItemText"] = highlightText,
                ["GrayText"] = System(COLOR_GRAYTEXT), ["LinkText"] = link, ["VisitedText"] = link, ["ActiveText"] = link,
                ["AccentColor"] = highlight, ["AccentColorText"] = highlightText,
                ["Tooltip"] = System(COLOR_INFOBK), ["TooltipText"] = System(COLOR_INFOTEXT),
            };
            if (!PrefersHighContrast)
            {
                colors.Clear();
                if (AccentPalette() is { } palette)
                {
                    colors["AccentColor"] = palette[3];
                    (colors["AccentLight3"], colors["AccentLight2"], colors["AccentLight1"]) = (palette[0], palette[1], palette[2]);
                    (colors["AccentDark1"], colors["AccentDark2"], colors["AccentDark3"]) = (palette[4], palette[5], palette[6]);
                }
            }
            return colors;
        }
    }

    private static Color[]? AccentPalette()
    {
        var bytes = stackalloc byte[32];
        uint size = 32;
        if (RegGetValueW(HKEY_CURRENT_USER, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentPalette",
            RRF_RT_REG_BINARY, null, bytes, &size) != 0 || size != 32) return null;
        var palette = new Color[8];
        for (var i = 0; i < 8; i++) palette[i] = new Color(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2], 255);
        return palette;
    }

    public void Resize(Size logicalSize)
    {
        logicalSize = Constrain(logicalSize);
        var rc = new RECT { right = (int)MathF.Round(logicalSize.Width * Scale), bottom = (int)MathF.Round(logicalSize.Height * Scale) };
        if (!clientFrame) AdjustWindowRectExForDpi(ref rc, fullScreen ? (uint)savedStyle : (uint)GetWindowLongPtrW(hwnd, -16), false, FrameExStyle, dpi);

        if (extended) rc.top = 0;
        if (fullScreen || IsZoomed(hwnd) || IsIconic(hwnd))
        {
            var placement = fullScreen ? beforeFullScreen : new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
            if (!fullScreen && !GetWindowPlacement(hwnd, ref placement)) throw new InvalidOperationException("Cannot read normal window placement.");
            placement.rcNormalPosition.right = placement.rcNormalPosition.left + rc.right - rc.left;
            placement.rcNormalPosition.bottom = placement.rcNormalPosition.top + rc.bottom - rc.top;
            if (fullScreen) beforeFullScreen = placement;
            else SetWindowPlacement(hwnd, placement);
            return;
        }
        SetWindowPos(hwnd, 0, 0, 0, rc.right - rc.left, rc.bottom - rc.top, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private void ConfigureTitleBar()
    {
        if (nativeTitleBar is not null)
        {
            nativeTitleBar.SetOperations(operations);
            nativeTitleBar.Reset();
            nativeTitleBar = null;
            RemoveWindowSubclass(hwnd, &CaptionProcedure, 1);
        }
        if (clientFrame || !modernWindowing || (!extended && titleBarColors == new WindowsTitleBarColors())) return;
        nativeTitleBar = WindowsTitleBar.Create(hwnd, extended);
        if (extended && !SetWindowSubclass(hwnd, &CaptionProcedure, 1, (nuint)GCHandle.ToIntPtr(self)))
            throw new InvalidOperationException("Cannot attach the custom title bar input handler.");
        ApplyNativeCaption();
    }

    public nint OnMessage(nint window, uint message, nint wParam, nint lParam)
    {
        if (message == Win32ApplicationLauncher.TaskbarButtonCreated && message != 0)
        {
            TaskbarReady = true;
            launcher?.Refresh();
        }
        if (message == WM_SYSCOMMAND && systemMenuCommands.TryGetValue((uint)((long)wParam & 0xFFF0), out var systemCommand))
        {
            systemCommand();
            return 0;
        }
        if (message == WM_SYSCOMMAND && (((long)wParam & 0xFFF0) switch
        {
            0xF000 => !Capabilities.CanResize,
            0xF010 => suppressFrameInput,
            0xF020 => !Capabilities.CanMinimize,
            0xF030 => !Capabilities.CanMaximize,
            _ => false,
        })) return 0;
        if (message == WM_NCMOUSELEAVE && trackingNonClient)
        {
            trackingNonClient = false;
            host.OnPointerLeft();
        }
        var customSnap = message >= WM_NCMOUSEMOVE && message <= WM_NCMBUTTONDBLCLK &&
            wParam == HTMAXBUTTON && !IsNativeCaptionPoint(lParam);
        var captionHover = message == WM_NCMOUSEMOVE && wParam == HTCAPTION && !IsNativeCaptionPoint(lParam);
        var captionMenu = message == WM_NCRBUTTONUP && wParam == HTCAPTION;
        if (extended && showSystemButtons && !fullScreen && !customSnap && !captionHover && !captionMenu && message is not (WM_NCCALCSIZE or WM_PAINT) &&
            (message != WM_NCHITTEST || IsNativeCaptionPoint(lParam)) &&
            DwmDefWindowProc(window, message, wParam, lParam, out var dwmResult)) return dwmResult;
        switch (message)
        {
            case WM_ACTIVATE:
            case 0x031E  :
                ApplyFrame();
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_NCRBUTTONUP when !clientFrame && !fullScreen && wParam == HTCAPTION && titleBarMenuMode != WindowTitleBarMenuMode.System:
                if (titleBarMenuMode == WindowTitleBarMenuMode.Nazor)
                    host.OnTitleBarMenuRequested(ClientPointFromScreen(lParam));
                return 0;
            case WM_NCRBUTTONUP when extended && !fullScreen && wParam == HTCAPTION:
            {

                SetForegroundWindow(window);
                var menu = GetSystemMenu(window, false);
                var current = (long)GetWindowLongPtrW(window, -16);
                var zoomed = IsZoomed(window);
                var iconic = IsIconic(window);
                void Enable(uint command, bool enabled) => EnableMenuItem(menu, command, enabled ? 0u : 1u);
                Enable(0xF120  , zoomed || iconic);
                Enable(0xF010  , !zoomed && !iconic);
                Enable(0xF000  , !zoomed && !iconic && (current & 0x40000  ) != 0);
                Enable(0xF020  , !iconic && (current & 0x20000  ) != 0);
                Enable(0xF030  , !zoomed && (current & 0x10000  ) != 0);
                var command = TrackPopupMenuEx(menu, 0x100 | 0x2  ,
                    LowSigned(lParam), HighSigned(lParam), window, 0);
                if (command != 0) PostMessageW(window, WM_SYSCOMMAND, (nint)command, lParam);
                return 0;
            }
            case WM_GETMINMAXINFO:
                var minMaxResult = DefWindowProcW(window, message, wParam, lParam);
                ApplySizeLimits(window, (MINMAXINFO*)lParam);
                return minMaxResult;
            case WM_ENTERSIZEMOVE:
                foreach (var popup in popupWindows.ToArray()) popup.OwnerBeginMove();
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_WINDOWPOSCHANGED:
                var positionResult = DefWindowProcW(window, message, wParam, lParam);
                if (UsesClientFrameAppearance) UpdateClientFrame();
                foreach (var popup in popupWindows.ToArray()) popup.OwnerMoved();
                return positionResult;
            case WM_SIZE:
                ReportState();
                if ((uint)wParam == SIZE_MINIMIZED)
                {
                    if (!minimized) host.OnVisibilityChanged(false);
                    minimized = true;
                    return 0;
                }
                if (minimized)
                {
                    minimized = false;
                    host.OnVisibilityChanged(true);
                    if (fullScreen && !changingState) FitFullScreen();
                }
                var width = (int)((long)lParam & 0xFFFF);
                var height = (int)(((long)lParam >> 16) & 0xFFFF);
                if (width == 0 || height == 0) return 0;
                EnsureBuffer(width, height);
                host.OnResized(new Size(width / Scale, height / Scale), Scale);
                InvalidateRect(window, null, false);
                return 0;
            case WM_DPICHANGED:
                dpi = (uint)((long)wParam & 0xFFFF);
                launcher?.Refresh();
                ApplyFrame();
                var suggested = *(RECT*)lParam;
                if (fullScreen) { FitFullScreen(MonitorFromRect(suggested, MONITOR_DEFAULTTONEAREST)); return 0; }
                if (IsZoomed(window)) { UpdateMaximizedBounds(); return 0; }
                var frame = ClientFrame(dpi);
                var constrained = Constrain(new Size((suggested.right - suggested.left - frame.right + frame.left) / Scale,
                    (suggested.bottom - suggested.top - frame.bottom + frame.top) / Scale));
                SetWindowPos(window, 0, suggested.left, suggested.top,
                    (int)MathF.Round(constrained.Width * Scale) + frame.right - frame.left,
                    (int)MathF.Round(constrained.Height * Scale) + frame.bottom - frame.top, SWP_NOZORDER | SWP_NOACTIVATE);
                return 0;
            case WM_SYSCOMMAND when fullScreen:
                switch ((long)wParam & 0xFFF0)
                {
                    case 0xF000:
                    case 0xF010:
                        return 0;
                    case 0xF030:
                        SetState(WindowState.Maximized);
                        return 0;
                    case 0xF120:
                        if (IsIconic(window)) SetState(WindowState.FullScreen);
                        else ExitFullScreen();
                        return 0;
                    default:
                        return DefWindowProcW(window, message, wParam, lParam);
                }
            case WM_PAINT:
                Paint(window);
                return 0;
            case 0x007E: // WM_DISPLAYCHANGE
                animationMonitor = 0;
                if (!fullScreen) return DefWindowProcW(window, message, wParam, lParam);
                if (!IsIconic(window)) FitFullScreen();
                return 0;
            case WM_NCCALCSIZE when fullScreen || clientFrame:
                return 0;
            case WM_NCHITTEST when fullScreen || suppressFrameInput:
                return HTCLIENT;
            case WM_NCCALCSIZE when extended && !modernWindowing && wParam != 0:
            {

                var parameters = (NCCALCSIZE_PARAMS*)lParam;
                var top = parameters->rgrc0.top;
                var result = DefWindowProcW(window, message, wParam, lParam);
                parameters->rgrc0.top = top + (IsZoomed(window) ? FrameThickness : 0);
                return result;
            }
            case WM_NCHITTEST when extended || showResizeGrip || clientFrame:
                return HitTest(window, wParam, lParam);

            case WM_NCMOUSEMOVE when extended && (wParam == HTMAXBUTTON || wParam == HTCAPTION) && !IsNativeCaptionPoint(lParam):
                if (!trackingNonClient)
                {
                    var track = new TRACKMOUSEEVENT { cbSize = (uint)sizeof(TRACKMOUSEEVENT), dwFlags = TME_LEAVE | TME_NONCLIENT, hwndTrack = window };
                    trackingNonClient = TrackMouseEvent(&track);
                }
                lastPointer = ClientPointFromScreen(lParam);
                host.OnPointerMoved(lastPointer, Modifiers());
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_NCLBUTTONDOWN or WM_NCLBUTTONDBLCLK or WM_NCRBUTTONDOWN or WM_NCRBUTTONDBLCLK or
                WM_NCMBUTTONDOWN or WM_NCMBUTTONDBLCLK when extended && wParam == HTMAXBUTTON && !IsNativeCaptionPoint(lParam):
                SetFocus(window);
                SetCapture(window);
                lastPointer = ClientPointFromScreen(lParam);
                host.OnPointerPressed(lastPointer, ButtonOf(message), Modifiers());
                if (message is WM_NCLBUTTONDBLCLK or WM_NCRBUTTONDBLCLK or WM_NCMBUTTONDBLCLK)
                    host.OnPointerDoubleClicked(lastPointer, ButtonOf(message), Modifiers());
                return 0;
            case WM_NCLBUTTONUP or WM_NCRBUTTONUP or WM_NCMBUTTONUP when extended && wParam == HTMAXBUTTON && !IsNativeCaptionPoint(lParam):
                lastPointer = ClientPointFromScreen(lParam);
                host.OnPointerReleased(lastPointer, ButtonOf(message), Modifiers());
                if (!explicitPointerCapture) SetPointerCapture(false);
                return 0;
            case WM_NCMOUSELEAVE when extended:
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_ERASEBKGND:
                return 1;
            case WM_MOUSEMOVE:
                if (!trackingMouse)
                {
                    var track = new TRACKMOUSEEVENT { cbSize = (uint)sizeof(TRACKMOUSEEVENT), dwFlags = TME_LEAVE, hwndTrack = window };
                    trackingMouse = TrackMouseEvent(&track);
                }
                lastPointer = PointFrom(lParam);
                host.OnPointerMoved(lastPointer, Modifiers());
                return 0;
            case WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN:
                SetFocus(window);
                SetCapture(window);
                lastPointer = PointFrom(lParam);
                host.OnPointerPressed(lastPointer, ButtonOf(message), Modifiers());
                return 0;
            case WM_LBUTTONDBLCLK or WM_RBUTTONDBLCLK or WM_MBUTTONDBLCLK:
                SetFocus(window);
                SetCapture(window);
                lastPointer = PointFrom(lParam);
                var doubleButton = message == WM_LBUTTONDBLCLK ? PointerButton.Primary : message == WM_RBUTTONDBLCLK ? PointerButton.Secondary : PointerButton.Middle;
                host.OnPointerPressed(lastPointer, doubleButton, Modifiers());
                host.OnPointerDoubleClicked(lastPointer, doubleButton, Modifiers());
                return 0;
            case WM_LBUTTONUP or WM_RBUTTONUP or WM_MBUTTONUP:
                lastPointer = PointFrom(lParam);
                host.OnPointerReleased(lastPointer, ButtonOf(message), Modifiers());
                if (!explicitPointerCapture) SetPointerCapture(false);
                return 0;
            case WM_MOUSEWHEEL or WM_MOUSEHWHEEL:
            {

                var point = new POINT { X = LowSigned(lParam), Y = HighSigned(lParam) };
                ScreenToClient(window, &point);
                lastPointer = new Point(point.X / Scale, point.Y / Scale);
                var notches = HighSigned(wParam) / 120f;
                if (message == WM_MOUSEWHEEL) host.OnPointerWheel(lastPointer, notches, Modifiers());
                else host.OnPointerHorizontalWheel(lastPointer, notches, Modifiers());
                return 0;
            }
            case WM_MOUSELEAVE:
                trackingMouse = false;
                if (GetCapture() != window) host.OnPointerLeft();
                return 0;
            case WM_CAPTURECHANGED:
                if (!releasingCapture && lParam != window)
                {
                    explicitPointerCapture = false;
                    host.OnPointerCanceled();
                }
                return 0;
            case WM_KEYDOWN or WM_SYSKEYDOWN:
            {
                var handled = host.OnKeyDown(KeyNames.FromVirtualKey((int)wParam, (Modifiers() & KeyModifiers.Shift) != 0), Modifiers(), ((long)lParam & 0x40000000) != 0);
                return handled ? 1 : message == WM_KEYDOWN ? 0 : DefWindowProcW(window, message, wParam, lParam);
            }
            case WM_KEYUP or WM_SYSKEYUP:
            {
                var handled = host.OnKeyUp(KeyNames.FromVirtualKey((int)wParam, (Modifiers() & KeyModifiers.Shift) != 0), Modifiers());
                return handled || message == WM_KEYUP ? 0 : DefWindowProcW(window, message, wParam, lParam);
            }
            case WM_CHAR:
                if (!composing) OnCharacter((char)(ushort)wParam);
                return 0;
            case WM_APPCOMMAND:
            {

                var key = (((long)lParam >> 16) & 0x0FFF) switch
                {
                    APPCOMMAND_BROWSER_BACKWARD => "BrowserBack",
                    APPCOMMAND_BROWSER_FORWARD => "BrowserForward",
                    _ => null,
                };
                if (key is null || !host.OnKeyDown(key, Modifiers(), false)) return DefWindowProcW(window, message, wParam, lParam);
                host.OnKeyUp(key, Modifiers());
                return 1;
            }
            case WM_IME_SETCONTEXT:

                return DefWindowProcW(window, message, wParam, (nint)((long)lParam & ~(long)ISC_SHOWUICOMPOSITIONWINDOW));
            case WM_IME_STARTCOMPOSITION:
                composing = true;
                PositionCaret();
                return 0;
            case WM_IME_COMPOSITION:
            {
                var flags = (uint)(long)lParam;

                if ((flags & GCS_RESULTSTR) != 0 && ReadComposition(GCS_RESULTSTR) is { Length: > 0 } result) host.OnCompositionCommit(result);
                if ((flags & GCS_COMPSTR) != 0) host.OnComposition(ReadComposition(GCS_COMPSTR) ?? "", ReadCompositionCursor());
                else if (flags == 0) host.OnComposition("", 0);
                PositionCaret();
                return 0;
            }
            case WM_IME_ENDCOMPOSITION:
                composing = false;
                host.OnComposition("", 0);
                return 0;
            case WM_IME_CHAR:
                return 0;
            case WM_SETFOCUS:
                host.OnFocusChanged(true);
                PositionCaret();
                return 0;
            case WM_KILLFOCUS:
                pendingHighSurrogate = '\0';
                composing = false;
                if (systemCaret) DestroyCaret();
                systemCaret = false;
                host.OnFocusChanged(false);
                return 0;
            case WM_SETCURSOR when ((long)lParam & 0xFFFF) == HTCLIENT:
                SetCursor(LoadCursorW(0, host.GetCursor(lastPointer) switch
                {
                    PlatformCursor.Hand => IDC_HAND,
                    PlatformCursor.IBeam => IDC_IBEAM,
                    PlatformCursor.SizeWestEast => IDC_SIZEWE,
                    PlatformCursor.SizeNorthSouth => IDC_SIZENS,
                    PlatformCursor.SizeAll => IDC_SIZEALL,
                    PlatformCursor.No => IDC_NO,
                    PlatformCursor.Wait => IDC_WAIT,
                    PlatformCursor.AppStarting => IDC_APPSTARTING,
                    PlatformCursor.Help => IDC_HELP,
                    PlatformCursor.Cross => IDC_CROSS,
                    PlatformCursor.SizeNorthwestSoutheast => IDC_SIZENWSE,
                    PlatformCursor.SizeNortheastSouthwest => IDC_SIZENESW,
                    _ => IDC_ARROW,
                }));
                return 1;
            case WM_CLOSE:
                if (host.OnClosing()) { host.OnRenderingStopping(); DestroyWindow(window); }
                return 0;
            case WM_GETOBJECT:

                if ((int)(uint)(ulong)lParam == Uia.UiaRootObjectId && !IsClosed && (automation ??= CreateAutomation()) is { } bridge)
                    return Uia.UiaReturnRawElementProvider(window, wParam, lParam, bridge.Root);
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_NAZOR_AUTOMATION:
                automation?.FlushEvents();
                return 0;
            case WM_TIMER when wParam == (nint)AnimationTimer:
                KillTimer(window, AnimationTimer);
                goto case WM_NAZOR_ANIMATION;
            case WM_NAZOR_ANIMATION:
                if (animationDeadline is not { } due || (message == WM_NAZOR_ANIMATION &&
                    (useLegacyAnimationTimer || Stopwatch.GetTimestamp() < due))) return 0;
                animationDeadline = null;
                RedrawWindow(window, null, 0, RDW_NOINTERNALPAINT);
                Paint(window);
                return 0;
            case WM_SHOWWINDOW:
                if (wParam == 0) { HideClientFrameShadow(); HideResizeOutset(); }
                if (!changingTaskbarVisibility) host.OnVisibilityChanged(wParam != 0);
                return DefWindowProcW(window, message, wParam, lParam);

            case WM_SETTINGCHANGE:
            case 0x007D  :

            case WM_SYSCOLORCHANGE:
            case WM_DWMCOLORIZATIONCOLORCHANGED:
                host.OnSystemSettingsChanged();
                return 0;
            case WM_NAZOR_AUTOMATION_CALL:
                AutomationBridge.RunCall((int)lParam);
                return 1;
            case WM_DESTROY:
                host.OnRenderingStopping();
                IsClosed = true;
                DestroyClientFrameShadow();
                DestroyResizeOutset();
                systemMenuCommands.Clear();
                launcher?.Remove(this);
                if (useLegacyAnimationTimer && animationDeadline is not null) KillTimer(window, AnimationTimer);
                ReleaseAnimationTimer();
                animationDeadline = null;
                if (automation is not null)
                {

                    Uia.UiaReturnRawElementProvider(window, 0, 0, null);
                    automation.Dispose();
                    automation = null;
                }
                host.OnClosed();
                FreeBuffer();
                return 0;
            default:
                return DefWindowProcW(window, message, wParam, lParam);
        }
    }

    internal AutomationBridge? Automation => automation;

    private AutomationBridge? CreateAutomation() => host.GetSemanticTree() is { } tree ? new AutomationBridge(this, tree) : null;

    public void OnFinalMessage()
    {
        alphaSurface?.Dispose();
        alphaSurface = null;
        nativeTitleBar = null;
        hwnd = 0;
        ReleaseIcons();
        if (self.IsAllocated) self.Free();
    }

    private void Paint(nint window)
    {
        paintStarted = Stopwatch.GetTimestamp();
        if (host.UsesGpuRendering)
        {
            ValidateRect(window, null);
            GetClientRect(window, out var client);
            if (gpuWindow != 0 && gpuWindowSize != (client.right, client.bottom) && client.right > 0 && client.bottom > 0)
            {
                if (!SetWindowPos(gpuWindow, HWND_BOTTOM, 0, 0, client.right, client.bottom, SWP_NOACTIVATE))
                    throw new GraphicsDeviceException("Cannot resize the OpenGL presentation window.");
                gpuWindowSize = (client.right, client.bottom);
            }
            if (client.right > 0 && client.bottom > 0)
                host.OnPaint(new PaintTarget(0, client.right, client.bottom, 0, Scale)
                    { TransparentRegion = TransparentCaptionRegion(client.right, client.bottom) });
            return;
        }
        PAINTSTRUCT ps = default;
        var hasUpdate = GetUpdateRect(window, null, false);
        var paintDC = hasUpdate ? BeginPaint(window, &ps) : 0;

        nint hdc = 0;
        var presentationStarted = 0L;
        try
        {
            if (hasUpdate && paintDC == 0) return;
            if (pixels == null) return;
            var changed = host.OnPaint(new PaintTarget((nint)pixels, bufferWidth, bufferHeight, bufferWidth * 4, Scale));
            if (IsClosed) return;
            if (alphaSurface is { } surface)
            {
                var started = Stopwatch.GetTimestamp();
                if (surface.Present(pixels, bufferWidth, bufferHeight, changed is not { Count: 0 }))
                    presentationStarted = started;
                return;
            }
            hdc = GetDCEx(window, 0, DCX_CACHE | DCX_CLIPCHILDREN | DCX_CLIPSIBLINGS);
            if (hdc == 0) return;
            presentationStarted = Stopwatch.GetTimestamp();
            if (TransparentCaptionRegion(bufferWidth,bufferHeight) is { } caption)
            {
                for (var row = caption.Y; row < caption.Bottom; row++)
                    new Span<uint>((uint*)pixels + (long)row * bufferWidth + caption.X, caption.Width).Clear();
            }
            if (changed is null)
            {
                Present(hdc, 0, 0, bufferWidth, bufferHeight);
                return;
            }

            foreach (var rect in changed) Present(hdc, rect.X, rect.Y, rect.Width, rect.Height);
            var asked = ps.rcPaint;
            var left = Math.Clamp(asked.left, 0, bufferWidth);
            var top = Math.Clamp(asked.top, 0, bufferHeight);
            var right = Math.Clamp(asked.right, 0, bufferWidth);
            var bottom = Math.Clamp(asked.bottom, 0, bufferHeight);
            if (right > left && bottom > top) Present(hdc, left, top, right - left, bottom - top);
        }
        finally
        {
            if (hdc != 0) ReleaseDC(window, hdc);

            if (hasUpdate) EndPaint(window, &ps);
            if (presentationStarted != 0) host.OnPresented(Stopwatch.GetElapsedTime(presentationStarted));
        }
    }

    private PixelRect? TransparentCaptionRegion(int width,int height)
    {
        if (!extended || !showSystemButtons || fullScreen || nativeTitleBar is not null) return null;
        var bounds=NativeCaptionBounds();
        var origin=new POINT();
        ClientToScreen(hwnd,ref origin);
        GetWindowRect(hwnd,out var outer);
        var left=Math.Clamp(bounds.left+outer.left-origin.X,0,width);
        var right=Math.Clamp(bounds.right+outer.left-origin.X,0,width);
        var top=Math.Clamp(bounds.top+outer.top-origin.Y,0,height);
        var bottom=Math.Clamp(bounds.bottom+outer.top-origin.Y,0,height);
        return new(left,top,Math.Max(0,right-left),Math.Max(0,bottom-top));
    }

    private void Present(nint hdc, int x, int y, int width, int height)
    {
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = bufferWidth,
            biHeight = -height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = BI_RGB,
        };
        SetDIBitsToDevice(hdc, x, y, (uint)width, (uint)height, x, 0, 0, (uint)height, pixels + (long)y * bufferWidth * 4, &header, DIB_RGB_COLORS);
    }

    private void OnCharacter(char c)
    {
        if (char.IsHighSurrogate(c))
        {
            pendingHighSurrogate = c;
            return;
        }
        if (char.IsLowSurrogate(c))
        {
            if (pendingHighSurrogate != '\0') host.OnTextInput(new string([pendingHighSurrogate, c]));
            pendingHighSurrogate = '\0';
            return;
        }
        pendingHighSurrogate = '\0';
        if (c < 0x20 || c == 0x7F) return;
        host.OnTextInput(c.ToString());
    }

    private void EnsureBuffer(int width, int height)
    {
        if (host.UsesGpuRendering)
        {
            FreeBuffer(); bufferWidth = width; bufferHeight = height; return;
        }
        if (width == bufferWidth && height == bufferHeight && pixels != null) return;
        FreeBuffer();
        pixels = (byte*)NativeMemory.AlignedAlloc((nuint)(width * height * 4), 64);
        bufferWidth = width;
        bufferHeight = height;
    }

    private void FreeBuffer()
    {
        if (pixels == null) return;
        NativeMemory.AlignedFree(pixels);
        pixels = null;
        bufferWidth = bufferHeight = 0;
    }

    private Point PointFrom(nint lParam) => new(LowSigned(lParam) / Scale, HighSigned(lParam) / Scale);

    private static PointerButton ButtonOf(uint message) => message switch
    {
        WM_LBUTTONDOWN or WM_LBUTTONUP or WM_NCLBUTTONDOWN or WM_NCLBUTTONUP or WM_NCLBUTTONDBLCLK => PointerButton.Primary,
        WM_RBUTTONDOWN or WM_RBUTTONUP or WM_NCRBUTTONDOWN or WM_NCRBUTTONUP or WM_NCRBUTTONDBLCLK => PointerButton.Secondary,
        _ => PointerButton.Middle,
    };

    private static KeyModifiers Modifiers()
    {
        var modifiers = KeyModifiers.None;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= KeyModifiers.Shift;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= KeyModifiers.Control;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) modifiers |= KeyModifiers.Alt;
        if (((GetKeyState(VK_LWIN) | GetKeyState(VK_RWIN)) & 0x8000) != 0) modifiers |= KeyModifiers.Meta;
        return modifiers;
    }

    public void Dispose()
    {
        host.OnRenderingStopping();
        if (hwnd != 0 && !IsClosed) DestroyWindow(hwnd);
        FreeBuffer();
    }

    public IDisposable? AddSystemMenuCommand(string text, Action invoked)
    {
        AssertSystemMenuAccess();
        ObjectDisposedException.ThrowIf(IsClosed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(invoked);
        if (text.Length > 256 || text.Contains('\0')) throw new ArgumentException("Expected a menu label of at most 256 characters without NUL.", nameof(text));
        var menu = GetSystemMenu(hwnd, false);
        if (menu == 0) return null;
        if (systemMenuCommands.Count >= 128 || nextSystemMenuCommand >= 0xF000) throw new InvalidOperationException("The native system menu command budget is exhausted.");
        var id = nextSystemMenuCommand;
        nextSystemMenuCommand += 0x10;
        if (!AppendMenuW(menu, 0, id, text)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        systemMenuCommands.Add(id, invoked);
        return new SystemMenuRegistration(this, menu, id);
    }
    private void AssertSystemMenuAccess()
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("System menu commands require the UI thread.");
    }
    private sealed class SystemMenuRegistration(Win32Window owner, nint menu, uint id) : IDisposable
    {
        public void Dispose()
        {
            owner.AssertSystemMenuAccess();
            if (owner.systemMenuCommands.Remove(id) && !owner.IsClosed) RemoveMenu(menu, id, 0);
        }
    }
}

internal static class KeyNames
{
    public static string FromVirtualKey(int vk, bool shift) => vk switch
    {
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0D => "Enter",
        0x10 => "Shift",
        0x11 => "Control",
        0x12 => "Alt",
        0x14 => "CapsLock",
        0x1B => "Escape",
        0x20 => " ",
        0x21 => "PageUp",
        0x22 => "PageDown",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "ArrowLeft",
        0x26 => "ArrowUp",
        0x27 => "ArrowRight",
        0x28 => "ArrowDown",
        0x2D => "Insert",
        0x2E => "Delete",
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x41 and <= 0x5A => (shift ? (char)vk : char.ToLowerInvariant((char)vk)).ToString(),
        0x5B or 0x5C => "Meta",
        0x5D => "ContextMenu",
        >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),
        0xA6 => "BrowserBack",
        0xA7 => "BrowserForward",
        _ => "Unidentified",
    };
}
