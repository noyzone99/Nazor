using System.Diagnostics;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using SkiaSharp;

namespace Nazor.Platform.Linux;

internal sealed unsafe class DrmView(DrmPlatform platform, IDrmBackend backend, IWindowHost host, float scale) : IPlatformWindow
{
    private readonly IClipboard clipboard = new MemoryClipboard();
    private nint pixels, presentation;
    private bool shown, enabled = true, dirty = true, cursorVisible;
    private long animationAt = long.MaxValue, lastClickTime;
    private Point pointer, lastClickPoint;
    private PointerButton lastClickButton;
    private PlatformCursor cursor;
    public bool IsClosed { get; private set; }
    public PlatformKind? Platform => PlatformKind.Linux;
    public nint Handle => 0;
    public float Scale => scale;
    public Size ClientSize => new(backend.Width / scale, backend.Height / scale);
    public string Title { get; set; } = "Nazor";
    public WindowState State => WindowState.FullScreen;
    public WindowCapabilities Capabilities => default;
    public bool PrefersReducedMotion => false;
    public bool PrefersDarkColorScheme => false;
    public IClipboard Clipboard => clipboard;
    public IOpenGlWindowContext CreateOpenGlContext() => backend.CreateOpenGlContext();

    public void Show(bool activate = true)
    {
        Access(); shown = true;
        host.OnResized(ClientSize, Scale);
        host.OnStateChanged(State);
        host.OnVisibilityChanged(backend.Active);
        host.OnFocusChanged(backend.Active && enabled);
        Invalidate();
    }

    public void Hide()
    {
        Access(); shown = false; host.OnPointerCanceled(); host.OnFocusChanged(false); host.OnVisibilityChanged(false);
    }

    public void Activate() { Access(); if (shown && backend.Active && enabled) host.OnFocusChanged(true); }
    public void Close() { Access(); if (host.OnClosing()) Dispose(); }

    public void Resize(Size logicalSize)
    {
        Access();
        if (logicalSize != ClientSize) throw new PlatformNotSupportedException("The DRM view size is determined by its display mode.");
    }

    public void SetTextInputCaret(Rect? caret) { Access(); }
    public void SetEnabled(bool value)
    {
        Access(); enabled = value;
        if (!value) host.OnPointerCanceled();
        host.OnFocusChanged(value && shown && backend.Active);
    }

    public void Invalidate() { Access(); dirty = true; platform.Wake(); }
    public void RequestAnimationFrame(TimeSpan delay)
    {
        Access();
        var milliseconds = Math.Clamp(delay.TotalMilliseconds, 0, int.MaxValue);
        animationAt = Math.Min(animationAt, Stopwatch.GetTimestamp() + (long)(milliseconds * Stopwatch.Frequency / 1000));
        platform.Wake();
    }

    internal void SetSessionActive(bool active)
    {
        if (IsClosed) return;
        if (!active) host.OnPointerCanceled();
        host.OnFocusChanged(active && shown && enabled);
        host.OnVisibilityChanged(active && shown);
        if (active) Invalidate();
    }

    internal int Tick()
    {
        if (IsClosed || !shown || !backend.Active) return -1;
        if (animationAt <= Stopwatch.GetTimestamp()) { animationAt = long.MaxValue; dirty = true; }
        if (dirty) Paint();
        return animationAt == long.MaxValue ? -1 : (int)Math.Clamp(Math.Ceiling((animationAt - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency), 0, int.MaxValue);
    }

    private void Paint()
    {
        dirty = false;
        if (host.UsesGpuRendering) { host.OnPaint(new(0, backend.Width, backend.Height, 0, Scale)); return; }
        var stride = checked(backend.Width * 4);
        var bytes = checked((nuint)stride * (nuint)backend.Height);
        if (pixels == 0) pixels = (nint)NativeMemory.AllocZeroed(bytes);
        var target = new PaintTarget(pixels, backend.Width, backend.Height, stride, Scale);
        host.OnPaint(target);
        if (IsClosed) return;
        var started = Stopwatch.GetTimestamp();
        if (cursorVisible)
        {
            if (presentation == 0) presentation = (nint)NativeMemory.Alloc(bytes);
            NativeMemory.Copy((void*)pixels, (void*)presentation, bytes);
            using var surface = SKSurface.Create(new SKImageInfo(backend.Width, backend.Height, SKColorType.Bgra8888, SKAlphaType.Premul), presentation, stride);
            surface.Canvas.Scale(Scale);
            using var path = CreateCursorPath();
            using var pen = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, Color = SKColors.Black, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            surface.Canvas.DrawPath(path, pen);
            pen.Color = SKColors.White; pen.StrokeWidth = 1;
            surface.Canvas.DrawPath(path, pen);
            surface.Canvas.Flush();
            target = target with { Pixels = presentation };
        }
        if (backend.Present(target)) host.OnPresented(Stopwatch.GetElapsedTime(started));
        else dirty = true;
    }

    private Point[] CursorPoints() => cursor switch
    {
        PlatformCursor.IBeam => [new(-3, -7), new(3, -7), new(0, -7), new(0, 7), new(-3, 7), new(3, 7)],
        PlatformCursor.Cross => [new(-7, 0), new(7, 0), new(0, 0), new(0, -7), new(0, 7)],
        _ => [new(0, 0), new(0, 16), new(4, 12), new(8, 19), new(11, 17), new(7, 10), new(13, 10), new(0, 0)]
    };

    private SKPath CreateCursorPath()
    {
        using var path = new SKPathBuilder();
        var points = CursorPoints();
        path.MoveTo(pointer.X + points[0].X, pointer.Y + points[0].Y);
        foreach (var point in points.Skip(1)) path.LineTo(pointer.X + point.X, pointer.Y + point.Y);
        return path.Detach();
    }

    public DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper)
    {
        if (!cursorVisible) return content;
        var builder = new DisplayListBuilder();
        foreach (var command in content.Commands) builder.Add(command);
        var points = CursorPoints().Select(point => new Point(pointer.X + point.X, pointer.Y + point.Y)).ToArray();
        builder.Add(new DrawPolyline(points, 3, Color.Black));
        builder.Add(new DrawPolyline(points, 1, Color.White));
        return builder.Build(content.Size);
    }

    internal void OnInput(DrmEvent value)
    {
        if (IsClosed || !shown || !backend.Active || !enabled) return;
        var point = new Point(value.Point.X / Scale, value.Point.Y / Scale);
        if (value.Kind is DrmEventKind.Move or DrmEventKind.Press or DrmEventKind.Release or DrmEventKind.Scroll)
        { pointer = point; cursorVisible = !value.Touch; }
        switch (value.Kind)
        {
            case DrmEventKind.Move: host.OnPointerMoved(point, value.Modifiers); break;
            case DrmEventKind.Press:
                host.OnPointerPressed(point, value.Button, value.Modifiers);
                if (IsClosed) return;
                var now = Environment.TickCount64;
                if (value.Button == lastClickButton && now - lastClickTime <= 500 && Math.Abs(point.X - lastClickPoint.X) <= 4 && Math.Abs(point.Y - lastClickPoint.Y) <= 4)
                { host.OnPointerDoubleClicked(point, value.Button, value.Modifiers); lastClickTime = 0; }
                else { lastClickButton = value.Button; lastClickPoint = point; lastClickTime = now; }
                break;
            case DrmEventKind.Release: host.OnPointerReleased(point, value.Button, value.Modifiers); break;
            case DrmEventKind.Scroll: host.OnPointerScroll(point, value.DeltaX, -value.DeltaY, ScrollDeltaUnit.LogicalPixels, value.Modifiers); break;
            case DrmEventKind.KeyDown:
                if (!host.OnKeyDown(value.Key!, value.Modifiers, value.Repeat) && !IsClosed && !string.IsNullOrEmpty(value.Text)) host.OnTextInput(value.Text);
                break;
            case DrmEventKind.KeyUp: host.OnKeyUp(value.Key!, value.Modifiers); break;
            case DrmEventKind.Cancel:
                host.OnPointerCanceled(); host.OnFocusChanged(false);
                if (!IsClosed) host.OnFocusChanged(true);
                break;
        }
        if (!IsClosed) { cursor = host.GetCursor(pointer); Invalidate(); }
    }

    private void Access() { platform.AssertAccess(); ObjectDisposedException.ThrowIf(IsClosed, this); }

    public void Dispose()
    {
        if (IsClosed) return;
        Access();
        try { host.OnRenderingStopping(); }
        finally
        {
            IsClosed = true;
            NativeMemory.Free((void*)presentation); presentation = 0;
            NativeMemory.Free((void*)pixels); pixels = 0;
            host.OnClosed();
        }
    }
}
