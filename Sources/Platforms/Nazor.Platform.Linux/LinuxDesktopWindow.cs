using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using SkiaSharp;
using static Nazor.Platform.Linux.LinuxNativeApi;

namespace Nazor.Platform.Linux;

internal sealed unsafe partial class LinuxDesktopWindow : IPlatformWindow, INativePopupOwner
{
    private static readonly ImageSource applicationIcon = ImageSource.FromResource(typeof(LinuxDesktopWindow).Assembly,
        "Nazor.Platform.Linux.ApplicationIcon.png");
    public ImageSource ApplicationIcon => applicationIcon;

    private LinuxNativeApi Native => platform.Native;
    private readonly bool transparentBackground;
    private EglWindowContext? egl;
    public IOpenGlWindowContext? CreateOpenGlContext() =>
        transparentBackground ? null : egl = new EglWindowContext(Handle, Native, platform.IsX11);

    public DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper, Color background)
    {
        var radius = ClientFrameRadius;
        if (topInset == 0 && radius.IsZero) return content;
        var builder = new DisplayListBuilder();
        var frameSize = new Size(content.Size.Width, content.Size.Height + topInset);
        if (!radius.IsZero)
        {
            var bounds = new Rect(0, 0, frameSize.Width, frameSize.Height);
            builder.Add(new PushClip(bounds, radius));
            if (!background.IsTransparent) builder.Add(new FillRectangle(bounds, 0, background));
        }
        builder.Add(new PushTranslate(0, topInset));
        foreach (var command in content.Commands) builder.Add(command);
        builder.Add(new Pop());
        if (!radius.IsZero) builder.Add(new Pop());
        return builder.Build(frameSize);
    }
    private readonly LinuxDesktopPlatform platform;
    private readonly IWindowHost host;
    private bool customTitlebar;
    private WindowOperations operations;
    private WindowState beforeFullScreen;
    private Size normalClientSize;
    private bool pendingNormalResize;
    private string title;
    private bool visible, configured, enabled = true, dirty = true, framePending, serverDecorations;
    private bool dark, suppressText, decorationPress, pointerCaptured, disposing;
    private uint nativeCapabilities;
    private WindowTitleBarMenuMode titleBarMenuMode;
    private int surfaceWidth, surfaceHeight, bufferWidth, bufferHeight;
    private nint pixels;
    private bool softwarePresented;
    private bool repaintExposed;
    private bool tiled, shadowVisible;
    private CornerRadius configuredRadius;
    private readonly bool x11ShapeAvailable;
    private (CornerRadius Radius, int Width, int Height, float Scale)? appliedShape;
    private bool paintedFrameBorder;
    private float resizeInset = 4, resizeOutset;
    private float? resizeCornerLength;
    private bool showResizeGrip;
    private IReadOnlyList<DrawShadow> frameShadows = [];
    private (int Width, int Height, float Scale)? shadowSize;
    private long animationAt = long.MaxValue, repeatAt = long.MaxValue;
    private string? repeatKey, repeatText;
    private uint repeatCode;
    private KeyModifiers repeatModifiers;
    private uint lastClickTime;
    private Point lastClickPoint;
    private PointerButton lastClickButton;
    private Rect? textCaret;
    private PlatformCursor? cursor;
    public string ExportedHandle { get; private set; } = "";

    public bool SetOwner(string ownerHandle)
    {
        Access();
        if (Native.ForeignParent == null || string.IsNullOrEmpty(ownerHandle) || ownerHandle.Length > 4096) return false;
        var bytes = System.Text.Encoding.UTF8.GetBytes(ownerHandle + '\0');
        fixed (byte* token = bytes) return Native.ForeignParent(Handle, token) != 0;
    }

    internal LinuxDesktopPlatform PlatformOwner => platform;
    public nint Handle { get; private set; }
    public NativeWindowHandle? NativeHandle => IsClosed ? null : new(Native.nz_window_native_surface(Handle), platform.IsX11 ? "X11" : "wl_surface");
    public float Scale { get; private set; } = 1;
    public PlatformKind? Platform => PlatformKind.Linux;
    public bool IsClosed => Handle == 0;
    public Size ClientSize => new(surfaceWidth, Math.Max(1, surfaceHeight - HeaderInset));
    public WindowState State { get; private set; }
    private bool activationReported;
    public bool ReportsActivationSeparately => !platform.IsX11 || activationReported;
    public bool UsesClientFrameAppearance => true;
    public bool HasDefaultClientFrameAppearance => !platform.IsX11;
    public bool DrawClientFrame => decorations?.Frame != WindowFrameOwner.Native && !serverDecorations && !tiled && State == WindowState.Normal;
    private Size FrameSize => new(surfaceWidth, surfaceHeight);
    public CornerRadius ClientFrameRadius => DrawClientFrame && (!platform.IsX11 || x11ShapeAvailable || transparentBackground)
        ? WindowFrameGeometry.NormalizeRadius(configuredRadius, FrameSize) : default;
    public bool HasRoundedClientFrame => !ClientFrameRadius.IsZero && (!platform.IsX11 || transparentBackground);

    public void SetClientFrameAppearance(CornerRadius radius, IReadOnlyList<DrawShadow> shadows)
    {
        if (configuredRadius == radius && frameShadows.SequenceEqual(shadows)) return;
        configuredRadius = radius;
        frameShadows = shadows;
        shadowSize = null;
        softwarePresented = false;
    }
    public void SetFrameResizeGeometry(float? inset, float? outset, float? cornerLength)
    {
        resizeInset = inset ?? 4;
        resizeOutset = Native.OuterResize != null && (!platform.IsX11 || x11ShapeAvailable) && outset is { } value && value > 0
            ? MathF.Ceiling(Math.Min(value, 128)) : 0;
        resizeCornerLength = cornerLength;
        SyncOuterResize();
    }
    public void SetResizeGrip(bool value) => showResizeGrip = value;
    private bool CanResizeFrame => visible && enabled && !tiled && operations.IsResizable && State == WindowState.Normal;
    public Rect ResizeGripBounds => showResizeGrip && CanResizeFrame
        ? WindowFrameGeometry.ResizeGripBounds(ClientSize, ClientFrameRadius) : default;
    private void SyncOuterResize()
    {
        if (Native.OuterResize != null)
            Native.OuterResize(Handle, CanResizeFrame && !serverDecorations ? (int)resizeOutset : 0);
    }
    public IClipboard Clipboard { get; }
    public bool PrefersReducedMotion => platform.Desktop.ReducedMotion;
    public bool PrefersDarkColorScheme => platform.Desktop.Dark;
    public bool PrefersHighContrast => platform.Desktop.HighContrast;
    public SystemColorProfile ColorProfile => DesktopProfile(System.Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
    internal static SystemColorProfile DesktopProfile(string? desktop) => desktop?.Contains("KDE", StringComparison.OrdinalIgnoreCase) == true ? SystemColorProfile.KDE :
        desktop?.Contains("GNOME", StringComparison.OrdinalIgnoreCase) == true ? SystemColorProfile.GNOME : SystemColorProfile.Generic;
    public WindowPlacementCapabilities PlacementCapabilities => new(true, false, false, false);
    private int HeaderInset => 0;
    public WindowButtonsMetrics SystemButtonsMetrics => default;

    public WindowCapabilities Capabilities
    {
        get
        {
            var native = nativeCapabilities = IsClosed ? 0 : Native.nz_window_capabilities(Handle);
            var motif = Native.X11Functions != null ? NativeRestriction.Hint : NativeRestriction.None;
            return new(operations.IsMinimizable && (native & 8) != 0, operations.IsMaximizable && (native & 2) != 0,
                operations.IsResizable, operations.FullScreenEnabled && (native & 4) != 0)
                {
                    Decorations = CurrentDecorationSupport, CanSetWindowIcon = (native & 16) != 0,
                    CanUseTransparentBackground = !IsClosed && platform.SupportsTransparentBackground,
                    CanShowPopupsOutsideWindow = !IsClosed && platform.SupportsNativePopups,
                    CanSetAlwaysOnTop = !IsClosed && platform.SupportsAlwaysOnTop,
                    Minimize = new((native & 8) != 0, motif),
                    Maximize = new((native & 2) != 0, motif),
                    FullScreen = new((native & 4) != 0),
                    InteractiveResize = new(true, NativeRestriction.Hint),
                    CanObserveMinimized = platform.IsX11,
                    CanRestoreFromMinimized = platform.IsX11,
                };
        }
    }
    public string Title
    {
        get => title;
        set { Access(); ArgumentNullException.ThrowIfNull(value); title = value; Native.nz_window_title(Handle, value); Invalidate(); }
    }

    internal LinuxDesktopWindow(LinuxDesktopPlatform platform, IWindowHost host, WindowOptions options, IClipboard clipboard)
    {
        ArgumentNullException.ThrowIfNull(options.Decorations);
        this.platform = platform; this.host = host; Clipboard = clipboard; title = options.Title;
        if (options.IsAlwaysOnTop && !platform.SupportsAlwaysOnTop)
            throw new PlatformNotSupportedException("The window manager does not offer programmatic always-on-top.");
        if (!options.IsVisibleInTaskbar)
            throw new PlatformNotSupportedException("The Linux backend does not support explicit taskbar visibility.");
        decorations = options.Decorations;
        transparentBackground = options.BackgroundMode == BackgroundMode.Transparent;
        customTitlebar = !decorations.HasNativeTitleBar;
        normalClientSize = ValidateSize(new((float)options.Width, (float)options.Height));
        surfaceWidth = (int)Math.Ceiling(normalClientSize.Width);
        surfaceHeight = checked((int)Math.Ceiling(normalClientSize.Height) + HeaderInset);
        var defaults = options.IsDialog ? WindowOperations.Dialog : WindowOperations.Default;
        operations = new(options.IsMinimizable ?? defaults.IsMinimizable, options.IsMaximizable ?? defaults.IsMaximizable,
            options.IsResizable ?? defaults.IsResizable, options.FullScreenEnabled ?? defaults.FullScreenEnabled);
        Handle = Native.nz_window_create(platform.Connection, title, AppId, surfaceWidth, surfaceHeight, options.Owner?.Handle ?? 0,
            customTitlebar ? 1 : 0, 0, platform.IsX11 && transparentBackground);
        if (Handle == 0)
        {
            if (platform.IsX11 && transparentBackground)
                throw new InvalidOperationException("Cannot create a transparent X11 window. An ARGB visual and a compositing manager are required.");
            var error = Marshal.PtrToStringUTF8(Native.nz_error(platform.Connection));
            throw new InvalidOperationException(string.IsNullOrEmpty(error) ? "Cannot create a Linux toplevel window." : error);
        }
        if (platform.IsX11)
        {
            ExportedHandle = "x11:" + ((nuint)Native.nz_window_native_surface(Handle)).ToString("x");
            x11ShapeAvailable = Native.X11Corners != null && Native.X11Corners(Handle, null) != 0;
        }
        Native.nz_window_operations(Handle, Capabilities.CanResize ? 1 : 0, surfaceWidth, surfaceHeight);
        ApplyX11Functions();
        if (options.IsAlwaysOnTop) SetAlwaysOnTop(true);
    }

    internal static string AppId => ApplicationIdentity.Id ?? "org.nazor." + string.Concat((System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Application")
        .Select(character => char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_'));
    internal static Size ValidateSize(Size size)
    {
        if (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width < 1 || size.Height < 1 || size.Width > 32768 || size.Height > 32768)
            throw new ArgumentOutOfRangeException(nameof(size), "Window dimensions must be finite and between 1 and 32768 logical units.");
        return size;
    }
    private void Access() { platform.AssertAccess(); ObjectDisposedException.ThrowIf(IsClosed, this); }
    public void Show(bool activate = true)
    {
        Access(); visible = true; dirty = true; Native.nz_window_show(Handle, 1); host.OnVisibilityChanged(true);
        if (IsClosed) return;
        if (activate) Native.nz_window_activate(Handle);
        platform.Wake();
    }
    public void Hide() { Access(); host.OnVisibilityChanged(false); if (IsClosed) return; visible = false; configured = false; framePending = false; softwarePresented = false; shadowVisible = false; shadowSize = null; Native.nz_window_show(Handle, 0); StopRepeat(); }

    public void ResetRendering()
    {
        Access();
        Native.nz_window_cancel_frame(Handle);
        framePending = false;
        softwarePresented = false;
        Invalidate();
    }
    public void Activate() { Access(); if (!visible) Show(); else Native.nz_window_activate(Handle); }
    public void Close() { Access(); if (host.OnClosing()) Dispose(); }
    public void SetEnabled(bool value)
    {
        Access(); enabled = value; SyncOuterResize(); Invalidate();
        if (!value) { StopRepeat(); host.OnPointerCanceled(); if (!IsClosed) host.OnFocusChanged(false); }
    }
    public void SetAlwaysOnTop(bool value)
    {
        Access();
        if (!platform.SupportsAlwaysOnTop) throw new PlatformNotSupportedException("The window manager does not offer programmatic always-on-top.");
        Native.X11Above(Handle, value ? 1 : 0);
    }
    public void SetPointerCapture(bool capture) { Access(); pointerCaptured = capture; if (Native.PointerCapture != null) Native.PointerCapture(Handle, capture ? 1 : 0); }
    public WindowChrome Chrome
    {
        get
        {
            if (IsClosed || State == WindowState.FullScreen) return default;
            return DecorationChrome;
        }
    }

    public void SetTitleBarMenuMode(WindowTitleBarMenuMode value)
    {
        Access();
        titleBarMenuMode = value;
    }

    public void SetOperations(WindowOperations value)
    {
        Access(); if (value == operations) return;
        operations = value; Native.nz_window_operations(Handle, Capabilities.CanResize ? 1 : 0, surfaceWidth, surfaceHeight);
        SyncOuterResize();
        ApplyX11Functions();
        repaintExposed = true;
        host.OnCapabilitiesChanged(); Invalidate();
    }
    private void ApplyX11Functions()
    {
        if (Native.X11Functions != null)
            Native.X11Functions(Handle, operations.IsMinimizable ? 1 : 0, operations.IsMaximizable ? 1 : 0);
    }
    private void ResizeForHeaderChange(int previousInset)
    {
        if (previousInset == HeaderInset || State != WindowState.Normal) return;
        var width = (int)Math.Ceiling(normalClientSize.Width);
        surfaceHeight = checked((int)Math.Ceiling(normalClientSize.Height) + HeaderInset);
        Native.nz_window_resize(Handle, width, surfaceHeight);
        Native.nz_window_operations(Handle, Capabilities.CanResize ? 1 : 0, width, surfaceHeight);
    }
    public void SetState(WindowState value)
    {
        Access();
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == WindowState.Minimized && !Capabilities.CanMinimize || value == WindowState.Maximized && !Capabilities.CanMaximize ||
            value == WindowState.FullScreen && !Capabilities.CanEnterFullScreen) return;
        if (value == WindowState.FullScreen && State != WindowState.FullScreen) beforeFullScreen = State;
        Native.nz_window_state(Handle, (int)value);
    }
    public void ExitFullScreen() { Access(); if (State == WindowState.FullScreen) Native.nz_window_state(Handle, (int)(beforeFullScreen == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal)); }
    public void SetDarkFrame(bool value) { Access(); dark = value; Invalidate(); }
    public BackdropResult SetBackdrop(BackdropSelection selection)
    {
        Access();
        var result = ResolveBackdrop(selection, enabled => Native.nz_window_blur(Handle, enabled ? 1 : 0) != 0);
        Invalidate();
        return result;
    }

    internal static BackdropResult ResolveBackdrop(BackdropSelection selection, Func<bool, bool> apply)
    {
        var enabled = selection.Linux == LinuxBackdrop.Blur || (selection.Linux is null && (selection.Preset ?? selection.Default) == CommonBackdrop.Blur);
        if (apply(enabled) || !enabled) return enabled ? new(true, "WaylandBlur") : default;
        var fallback = selection.Default == CommonBackdrop.Blur;
        if (apply(fallback)) return fallback ? new(true, "WaylandBlur") : default;
        apply(false);
        return default;
    }
    public void Resize(Size logicalSize)
    {
        Access(); logicalSize = ValidateSize(logicalSize);
        normalClientSize = logicalSize;
        if (State != WindowState.Normal) { pendingNormalResize = true; return; }
        var width=(int)Math.Ceiling(logicalSize.Width);
        var height=checked((int)Math.Ceiling(logicalSize.Height) + HeaderInset);
        Native.nz_window_resize(Handle, width, height);
        Native.nz_window_operations(Handle, Capabilities.CanResize ? 1 : 0, width, height);
    }
    public WindowPlacement GetPlacement() => new(0, 0, (int)normalClientSize.Width, (int)normalClientSize.Height, State == WindowState.Maximized)
    { Coordinates = WindowPlacementCoordinates.LogicalClient, Scale = Scale, LogicalClientSize = normalClientSize };
    public bool SetPlacement(WindowPlacement placement)
    {
        Access(); ArgumentNullException.ThrowIfNull(placement);
        var size = placement.LogicalClientSize ?? (placement.Coordinates == WindowPlacementCoordinates.LogicalClient ? new Size(placement.Width, placement.Height) : (Size?)null);
        if (size is null) return false;
        try { ValidateSize(size.Value); } catch (ArgumentOutOfRangeException) { return false; }
        Resize(size.Value); if (placement.IsMaximized) SetState(WindowState.Maximized);
        return true;
    }
    public void Invalidate() { if (IsClosed) return; platform.AssertAccess(); dirty = true; platform.Wake(); }
    public void RequestAnimationFrame(TimeSpan delay)
    {
        Access(); if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        animationAt = Math.Min(animationAt, Stopwatch.GetTimestamp() + (long)(Math.Min(delay.TotalSeconds, 86400) * Stopwatch.Frequency));
        platform.Wake();
    }
    public void SetTextInputCaret(Rect? caret)
    {
        Access(); textCaret = caret;
        var rect = caret ?? default;
        Native.nz_window_text_input(Handle, caret.HasValue ? 1 : 0, (int)rect.X, (int)rect.Y + HeaderInset, (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height));
        if (caret.HasValue && host.GetSemanticTree() is { Focused: { } focused })
        {
            var text = focused.IsPassword ? "" : focused.Value ?? "";
            var selection = focused.TextSelection;
            var cursor = Math.Clamp(selection?.Caret ?? 0, 0, text.Length);
            var anchor = Math.Clamp(selection?.Caret == selection?.Start ? selection?.End ?? cursor : selection?.Start ?? cursor, 0, text.Length);
            var surrounding = SurroundingText(text,cursor,anchor);
            Native.nz_window_surrounding(Handle, surrounding.Text, surrounding.Cursor, surrounding.Anchor, focused.IsPassword ? 1 : 0);
        }
    }
    internal static (string Text,int Cursor,int Anchor) SurroundingText(string text,int cursor,int anchor)
    {
        cursor=Math.Clamp(cursor,0,text.Length); anchor=Math.Clamp(anchor,0,text.Length);
        var start=Math.Max(0,cursor-600); var end=Math.Min(text.Length,cursor+600);
        if (start>0 && start<text.Length && char.IsLowSurrogate(text[start])) start++;
        if (end>0 && end<text.Length && char.IsHighSurrogate(text[end-1])) end--;
        if (anchor<start || anchor>end) anchor=cursor;
        return (text[start..end],Encoding.UTF8.GetByteCount(text.AsSpan(start,cursor-start)),Encoding.UTF8.GetByteCount(text.AsSpan(start,anchor-start)));
    }
    public void SetIcon(IReadOnlyList<RenderedImage>? images)
    {
        Access();
        if (images is { Count: 0 }) throw new ArgumentException("Supply icon images or null.", nameof(images));
        if (images is null) { Native.nz_window_icon(Handle, null, 0, 0); return; }
        foreach (var image in images)
            if (image.Width <= 0 || image.Height <= 0 || image.Pixels.LongLength != (long)image.Width * image.Height * 4)
                throw new ArgumentException("Icon images require packed BGRA pixels and positive dimensions.", nameof(images));
        var source = images.OrderByDescending(image => image.Width).First();
        var size = Math.Min(256, Math.Max(source.Width, source.Height));
        using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent);
        fixed (byte* pointer = source.Pixels)
        {
            using var input = SKSurface.Create(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul), (nint)pointer);
            using var snapshot = input.Snapshot();
            canvas.DrawImage(snapshot, new SKRect(0, 0, size, size), new SKSamplingOptions(SKFilterMode.Linear));
        }
        Native.nz_window_icon(Handle, (byte*)bitmap.GetPixels(), size, size);
    }
    internal int Tick()
    {
        if (IsClosed) return -1;
        var now = Stopwatch.GetTimestamp();
        if (repeatKey is not null && repeatAt <= now)
        {
            var handled = host.OnKeyDown(repeatKey, repeatModifiers, true);
            if (IsClosed) return -1;
            if (!handled && repeatText is not null) host.OnTextInput(repeatText);
            if (IsClosed) return -1;
            repeatAt = now + Stopwatch.Frequency / Math.Max(1, platform.RepeatRate);
        }
        if (animationAt <= now) { animationAt = long.MaxValue; dirty = true; }
        if (visible && configured && dirty && !framePending) Paint();
        var next = Math.Min(animationAt, repeatAt);
        return next == long.MaxValue ? -1 : (int)Math.Clamp(Math.Ceiling((next - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency), 0, int.MaxValue);
    }
    private void Paint()
    {
        var width = checked((int)Math.Ceiling(surfaceWidth * Scale));
        var height = checked((int)Math.Ceiling(surfaceHeight * Scale));
        if (host.UsesGpuRendering)
        {
            dirty = false;
            Native.nz_window_gpu_prepare(Handle);
            framePending = true;
            var fullRedraw = repaintExposed;
            repaintExposed = false;
            host.OnPaint(new(0, width, height, 0, Scale)
                { TopInset = (int)Math.Ceiling(HeaderInset * Scale), ForceFullRedraw = fullRedraw });
            if (!IsClosed)
            {
                UpdateFrameShape();
                UpdateFrameShadow();
                platform.Desktop.SyncAccessibility(this, host.GetSemanticTree());
            }
            return;
        }
        var result = Native.nz_window_acquire(Handle, width, height, out var native);
        if (result < 0) throw new InvalidOperationException(Marshal.PtrToStringUTF8(Native.nz_error(platform.Connection)));
        if (result == 0) return;
        var bytes = checked((nuint)width * (nuint)height * 4);
        if (width != bufferWidth || height != bufferHeight)
        {
            NativeMemory.Free((void*)pixels); pixels = 0;
            pixels = (nint)NativeMemory.AllocZeroed(bytes); bufferWidth = width; bufferHeight = height;
            host.OnResized(ClientSize, Scale);
            if (IsClosed) return;
        }
        var inset = (int)Math.Ceiling(HeaderInset * Scale);
        if (repaintExposed)
        {
            repaintExposed = false;
            softwarePresented = false;
        }
        dirty = false;
        IReadOnlyList<PixelRect>? damage;
        try { damage = host.OnPaint(new(pixels + inset * width * 4, width, height - inset, width * 4, Scale) { SeparateTopDecoration = inset > 0 }); }
        catch { Native.nz_window_discard_buffer(Handle); throw; }
        damage = ClipDamage(damage,width,height-inset);
        if (IsClosed) return;
        platform.Desktop.SyncAccessibility(this, host.GetSemanticTree());
        if (IsClosed) return;
        UpdateFrameShape();
        UpdateFrameShadow();
        var presentationStarted=Stopwatch.GetTimestamp();
        var frameBorder = inset > 0 ? host.GetClientFrameBorder(new(width / Scale, height / Scale)) : null;
        if (frameBorder is not null || paintedFrameBorder) damage = null;
        paintedFrameBorder = frameBorder is not null;
        if (damage is { Count: 0 } && softwarePresented) { Native.nz_window_discard_buffer(Handle); host.OnPresented(TimeSpan.Zero); return; }
        if (!softwarePresented) damage = null;
        if (damage is null || Native.nz_window_buffer_is_current(Handle) == 0) NativeMemory.Copy((void*)pixels, (void*)native.Pixels, bytes);
        else
        {
            foreach (var rectangle in damage)
            {
                for (var row=rectangle.Y; row<rectangle.Bottom; row++)
                {
                    var offset = checked(((long)row+inset)*width*4+rectangle.X*4L);
                    NativeMemory.Copy((byte*)pixels+offset,(byte*)native.Pixels+offset,checked((nuint)rectangle.Width*4));
                }
            }
        }
        if (frameBorder is not null) host.PaintClientFrameBorder(new(native.Pixels, width, height, native.Stride, Scale), frameBorder);
        if (HasRoundedClientFrame)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul), native.Pixels, native.Stride);
            using var clip = WindowFrameShadow.RoundRect(new SKRect(0, 0, width, height), ClientFrameRadius, Scale);
            surface.Canvas.ClipRoundRect(clip, SKClipOperation.Difference, antialias: true);
            surface.Canvas.Clear(SKColors.Transparent);
            damage = null;
        }
        if (damage is null) Native.nz_window_present(Handle,Scale,1);
        else
        {
            var regions = damage.Select(rectangle => rectangle with { Y = rectangle.Y+inset }).ToList();
            fixed (PixelRect* rectangles = regions.ToArray()) Native.nz_window_present_damage(Handle,Scale,1,rectangles,regions.Count);
        }
        softwarePresented = true; framePending = true;
        host.OnPresented(Stopwatch.GetElapsedTime(presentationStarted));
    }

    private void UpdateFrameShape()
    {
        if (!x11ShapeAvailable || transparentBackground) return;
        var radius = ClientFrameRadius;
        var shape = (radius, surfaceWidth, surfaceHeight, Scale);
        if (appliedShape == shape) return;
        appliedShape = shape;
        if (radius.IsZero) { Native.X11Corners(Handle, null); return; }
        float* radii = stackalloc float[]
        {
            radius.TopLeft.X * Scale, radius.TopLeft.Y * Scale, radius.TopRight.X * Scale, radius.TopRight.Y * Scale,
            radius.BottomRight.X * Scale, radius.BottomRight.Y * Scale, radius.BottomLeft.X * Scale, radius.BottomLeft.Y * Scale,
        };
        Native.X11Corners(Handle, radii);
    }

    private void UpdateFrameShadow()
    {
        if (!Native.HasFrameShadow) return;
        if (!DrawClientFrame || frameShadows.Count == 0 || platform.IsX11 && (!x11ShapeAvailable || !platform.SupportsTransparentBackground))
        {
            if (shadowVisible) Native.PresentFrameShadow(Handle, 0, 0, 0, 0, 1);
            shadowVisible = false;
            shadowSize = null;
            return;
        }

        var scale = platform.IsX11 ? Scale : Math.Max(1, (int)Math.Ceiling(Scale));
        var size = (surfaceWidth, surfaceHeight, scale);
        if (shadowSize == size) return;
        using var bitmap = WindowFrameShadow.Create(FrameSize, scale, ClientFrameRadius, frameShadows, out var margin);
        if (bitmap is null)
        {
            if (shadowVisible) Native.PresentFrameShadow(Handle, 0, 0, 0, 0, 1);
            shadowVisible = false;
            shadowSize = size;
            return;
        }
        if (Native.PresentFrameShadow(Handle, bitmap.GetPixels(), bitmap.Width, bitmap.Height, margin, scale) != 0)
        {
            shadowVisible = true;
            shadowSize = size;
        }
    }

    internal static IReadOnlyList<PixelRect>? ClipDamage(IReadOnlyList<PixelRect>? damage, int width, int height)
    {
        if (damage is null || damage.Count > 128) return null;
        var clipped = new List<PixelRect>();
        foreach (var rectangle in damage)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0) continue;
            var left = Math.Clamp(rectangle.X,0,width);
            var top = Math.Clamp(rectangle.Y,0,height);
            var right = (int)Math.Clamp((long)rectangle.X+rectangle.Width,0,width);
            var bottom = (int)Math.Clamp((long)rectangle.Y+rectangle.Height,0,height);
            if (right > left && bottom > top) clipped.Add(new(left,top,right-left,bottom-top));
        }
        return clipped;
    }
    internal void OnEvent(Event value)
    {
        if (IsClosed) return;
        if (value.Type == 6 && value.B != 0) popupSerial = (uint)value.D;
        if (value.Type == 8 && value.A != 0) popupSerial = (uint)value.X;
        var text = value.Text == 0 ? "" : Marshal.PtrToStringUTF8(value.Text) ?? "";
        var surfacePoint = new Point((float)value.X, (float)value.Y);
        var point = new Point(surfacePoint.X, surfacePoint.Y - HeaderInset);
        const uint exteriorFlag = 1u << 16;
        var exterior = (value.Flags & exteriorFlag) != 0;
        var modifiers = (KeyModifiers)(value.Flags & ~exteriorFlag);
        switch (value.Type)
        {
            case 1:
                var wasRounded = HasRoundedClientFrame;
                State = (WindowState)value.C;
                tiled = !platform.IsX11 && (value.Flags & 1u) != 0;
                surfaceWidth = Math.Max(1, value.A); surfaceHeight = Math.Max(HeaderInset + 1, value.B); configured = true;
                SyncOuterResize();
                if (wasRounded != HasRoundedClientFrame) softwarePresented = false;
                if (State == WindowState.Normal && pendingNormalResize)
                {
                    pendingNormalResize = false;
                    Resize(normalClientSize);
                    break;
                }
                if (State == WindowState.Normal) normalClientSize = ClientSize;
                host.OnResized(ClientSize, Scale); if (IsClosed) break; host.OnStateChanged(State); Invalidate(); break;
            case 2: Close(); break;
            case 3: cursor = null; goto case 5;
            case 4: if (enabled && !pointerCaptured) host.OnPointerLeft(); break;
            case 5:
                if (!enabled) break;
                var edge = ResizeEdgeAt(surfacePoint);
                var nextCursor = edge switch
                {
                    1 or 2 => PlatformCursor.SizeNorthSouth,
                    4 or 8 => PlatformCursor.SizeWestEast,
                    5 or 10 => PlatformCursor.SizeNorthwestSoutheast,
                    6 or 9 => PlatformCursor.SizeNortheastSouthwest,
                    _ => exterior || point.Y < 0 ? PlatformCursor.Arrow : host.GetCursor(point)
                };
                if (IsClosed) break;
                if (cursor != nextCursor) { cursor = nextCursor; Native.nz_window_cursor(Handle, CursorName(nextCursor)); }
                if (!exterior && (point.Y >= 0 || pointerCaptured)) host.OnPointerMoved(point, modifiers);
                break;
            case 6: if (enabled) PointerButton(value, point, surfacePoint, modifiers, exterior); break;
            case 7:
                if (enabled && !exterior && point.Y >= 0)
                {
                    var delta = value.D / 256f;
                    host.OnPointerScroll(point, value.A == 1 ? delta : 0, value.A == 0 ? delta : 0, ScrollDeltaUnit.LogicalPixels, modifiers);
                }
                break;
            case 8:
                if (!enabled) break;
                var key = KeyName(text);
                if (value.A != 0)
                {
                    suppressText = host.OnKeyDown(key, modifiers, value.D != 0); repeatText = null;
                    if (value.C != 0 && platform.RepeatRate > 0) { repeatKey = key; repeatCode = (uint)value.B; repeatModifiers = modifiers; repeatAt = Stopwatch.GetTimestamp() + platform.RepeatDelay * Stopwatch.Frequency / 1000; }
                }
                else { host.OnKeyUp(key, modifiers); if (repeatCode == (uint)value.B) StopRepeat(); }
                break;
            case 9:
                keyboardFocused = value.A != 0;
                if (popupWindows.Count > 0)
                {
                    popupFocusPending = true;
                    break;
                }
                ReportKeyboardFocus(keyboardFocused);
                break;
            case 25:
                if (value.A != 0) DismissMovingPopups();
                host.OnResized(ClientSize, Scale);
                Invalidate();
                break;
            case 10:
                if (enabled && (value.Flags != 0 || !suppressText)) { if (value.Flags != 0) host.OnCompositionCommit(text); else { host.OnTextInput(text); repeatText = text; } }
                suppressText = false; break;
            case 11:
                if (enabled) { var count = Math.Clamp(value.A, 0, Encoding.UTF8.GetByteCount(text)); host.OnComposition(text, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text).AsSpan(0,count)).Length); }
                break;
            case 12:
                if (enabled && host.GetSemanticTree() is { Focused: { IsPassword: false, Value: { } current, TextSelection: { } selection } focused } semantic)
                {
                    var bytes = Encoding.UTF8.GetBytes(current);
                    var cursorByte = Encoding.UTF8.GetByteCount(current.AsSpan(0, Math.Clamp(selection.Caret,0,current.Length)));
                    var first = Math.Clamp(cursorByte - value.A,0,bytes.Length);
                    var last = Math.Clamp((long)cursorByte + value.B,0,bytes.Length);
                    if ((first == bytes.Length || (bytes[first] & 0xc0) != 0x80) && (last == bytes.Length || (bytes[(int)last] & 0xc0) != 0x80))
                    {
                        var start = Encoding.UTF8.GetCharCount(bytes.AsSpan(0,first));
                        var end = Encoding.UTF8.GetCharCount(bytes.AsSpan(0,(int)last));
                        if (semantic.SetValue(focused,current.Remove(start,end-start))) semantic.SelectText(focused,start,start);
                    }
                }
                break;
            case 13: framePending = false; break;
            case 14: if (double.IsFinite(value.X) && value.X >= 0.5 && value.X <= 8) { Scale = (float)value.X; host.OnResized(ClientSize, Scale); Invalidate(); } break;
            case 15:
                var previousInset = HeaderInset;
                softwarePresented = false;
                serverDecorations = Native.DecorationMode(Handle) == 2;
                CompleteDecorationNegotiation(value.A != 0);
                SyncOuterResize();
                ResizeForHeaderChange(previousInset);
                host.OnCapabilitiesChanged(); if (IsClosed) break;
                host.OnStateChanged(State); if (IsClosed) break;
                host.OnResized(ClientSize, Scale); Invalidate(); break;
            case 18: ExportedHandle = (platform.IsX11 ? "x11:" : "wayland:") + text; break;
            case 20:
                CompleteDecorationNegotiation(false);
                if (value.A != 0) egl?.InvalidateDrawable();
                repaintExposed = true; Invalidate(); break;
            case 21: host.OnAlwaysOnTopChanged(value.A != 0); break;
            case 22: activationReported = true; host.OnActivationChanged(value.A != 0); break;
            case 19: SetTextInputCaret(textCaret); break;
        }
    }
    private void PointerButton(Event value, Point point, Point surfacePoint, KeyModifiers modifiers, bool exterior)
    {
        var button = value.A switch { 272 => Input.PointerButton.Primary, 273 => Input.PointerButton.Secondary, 274 => Input.PointerButton.Middle, _ => Input.PointerButton.None };
        if (button == Input.PointerButton.None) return;
        if (value.B != 0)
        {
            if (exterior)
            {
                var edge = button == Input.PointerButton.Primary ? ResizeEdgeAt(surfacePoint) : 0;
                if (edge != 0) StartResize(edge, (uint)value.D);
                return;
            }
            decorationPress = button switch
            {
                Input.PointerButton.Primary => HandleDecorationPress(surfacePoint, (uint)value.C, (uint)value.D),
                Input.PointerButton.Secondary => HandleDecorationMenu(value),
                _ => false
            };
            if (IsClosed) return;
            if (decorationPress) { if (!IsClosed) host.OnPointerCanceled(); return; }
            if (point.Y < 0) return;
            host.OnPointerPressed(point, button, modifiers);
            if (IsClosed) return;
            var elapsed = unchecked((uint)value.C - lastClickTime);
            if (button == lastClickButton && elapsed <= 500 && Math.Abs(point.X-lastClickPoint.X) <= 4 && Math.Abs(point.Y-lastClickPoint.Y) <= 4)
            { host.OnPointerDoubleClicked(point,button,modifiers); lastClickTime=0; }
            else { lastClickTime=(uint)value.C; lastClickButton=button; lastClickPoint=point; }
        }
        else if (!exterior && !decorationPress && (point.Y >= 0 || pointerCaptured)) host.OnPointerReleased(point,button,modifiers);
    }
    private bool HandleDecorationMenu(Event value)
    {
        if (serverDecorations || State == WindowState.FullScreen) return false;
        var x = (float)value.X;
        var y = (float)value.Y;
        if (y >= HeaderInset && (!customTitlebar || host.HitTest(new(x, y)) != WindowHitTest.Caption)) return false;
        if (IsClosed) return true;
        if (titleBarMenuMode == WindowTitleBarMenuMode.Nazor)
        {
            host.OnTitleBarMenuRequested(new(x, Math.Max(0, y - HeaderInset)));
            return true;
        }
        return titleBarMenuMode == WindowTitleBarMenuMode.None ||
            (Native.WindowMenu != null && Native.WindowMenu(Handle, (uint)value.D, (int)x, (int)y) != 0);
    }
    private bool HandleDecorationPress(Point surfacePoint, uint time, uint serial)
    {
        if (pointerCaptured) return false;
        var (x, y) = surfacePoint;
        var edge = ResizeEdgeAt(surfacePoint);
        if (edge != 0) { StartResize(edge, serial); return true; }
        if (State != WindowState.FullScreen && (y < HeaderInset || customTitlebar && host.HitTest(new(x,y)) == WindowHitTest.Caption))
        {
            if (IsClosed) return true;
            if (time-lastClickTime <= 500 && Math.Abs(x-lastClickPoint.X) <= 4 && Math.Abs(y-lastClickPoint.Y) <= 4)
            { SetState(State == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized); lastClickTime=0; }
            else { lastClickTime=time; lastClickPoint=new(x,y); DismissMovingPopups(); Native.nz_window_move(Handle); }
            return true;
        }
        return false;
    }
    private void StartResize(uint edge, uint serial)
    {
        if (Native.WaylandResize != null) Native.WaylandResize(Handle, edge, serial);
        else Native.nz_window_interactive_resize(Handle, edge);
    }
    private uint ResizeEdgeAt(Point point)
    {
        if (!CanResizeFrame || pointerCaptured) return 0;
        var clientPoint = new Point(point.X, point.Y - HeaderInset);
        var grip = ResizeGripBounds;
        if (grip.Width > 0 && grip.Height > 0 && grip.Contains(clientPoint)) return 10;
        if (serverDecorations) return 0;
        return WindowFrameGeometry.ResizeEdgeAt(FrameSize, point, resizeInset, resizeOutset, resizeCornerLength);
    }
    internal static string KeyName(string name) => LinuxKeyboard.KeyName(name);
    private static string CursorName(PlatformCursor value) => value switch
    {
        PlatformCursor.Hand => "pointer", PlatformCursor.IBeam => "text", PlatformCursor.SizeWestEast => "ew-resize", PlatformCursor.SizeNorthSouth => "ns-resize",
        PlatformCursor.SizeAll => "move", PlatformCursor.No => "not-allowed", PlatformCursor.Wait => "wait", PlatformCursor.AppStarting => "progress", PlatformCursor.Help => "help", PlatformCursor.Cross => "crosshair",
        PlatformCursor.SizeNorthwestSoutheast => "nwse-resize", PlatformCursor.SizeNortheastSouthwest => "nesw-resize", _ => "default"
    };
    private void StopRepeat() { repeatKey = null; repeatText = null; repeatAt = long.MaxValue; }
    internal void SettingsChanged() { if (!IsClosed) { host.OnSystemSettingsChanged(); Invalidate(); } }
    public void Dispose()
    {
        if (IsClosed || disposing) return;
        platform.AssertAccess(); StopRepeat();
        foreach (var popup in popupWindows.AsEnumerable().Reverse().ToArray()) popup.Dispose();
        disposing = true;
        decorationCompletion?.TrySetException(new ObjectDisposedException(nameof(LinuxDesktopWindow)));
        try { host.OnRenderingStopping(); }
        finally
        {
            var handle = Handle;
            try { platform.RemoveAccessibility(this); }
            finally
            {
                platform.Remove(this); Handle = 0; ExportedHandle = "";
                try { Native.nz_window_destroy(handle); }
                finally { NativeMemory.Free((void*)pixels); pixels = 0; host.OnClosed(); }
            }
        }
    }
}
