using System.Diagnostics;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Platform;
using SkiaSharp;
using Nazor.Skia;

namespace Nazor;

internal sealed class OpenGlCompositor : IDisposable
{
    internal sealed record Presentation(long Number, TimeSpan Duration, int UiFrame);
    private sealed record Frame(RecordedComposition Picture, PaintTarget Target, Color Background, PixelRect[]? Damage, int UiFrame) : IDisposable
    {
        public void Dispose() => Picture.Dispose();
    }

    private readonly Lock gate = new();
    private readonly Action<Exception> failed;
    private readonly Action? animationCompleted;
    private readonly Action<Presentation>? presentedFrame;
    private readonly Timer animationTimer;
    private IOpenGlWindowContext? native;
    private GRGlInterface? gl;
    private GRContext? context;
    private SKSurface? retained;
    private (int Width, int Height) size;
    private int sampleCount, stencilBits;
    private Frame? pending;
    private Frame? active;
    private bool animationNotified;
    private TaskCompletionSource<RenderedImage>? capture;
    private bool stopping, suspended, queued;
    private Exception? fault;
    private long presented, replaced;
    private Presentation? lastPresentation;

    internal string Device { get; }
    internal string Backend => native?.IsEmbedded == true ? "Skia OpenGL ES / EGL" : "Skia OpenGL / WGL";
    internal Presentation? LastPresentation => Volatile.Read(ref lastPresentation);
    internal long Presented => LastPresentation?.Number ?? 0;
    internal long Replaced => Interlocked.Read(ref replaced);
    internal TimeSpan LastFrameDuration => LastPresentation?.Duration ?? TimeSpan.Zero;

    internal OpenGlCompositor(Func<IOpenGlWindowContext?> createContext, Action<Exception> failed, Action? animationCompleted = null, Action<Presentation>? presentedFrame = null)
    {
        this.failed = failed;
        this.animationCompleted = animationCompleted;
        this.presentedFrame = presentedFrame;
        animationTimer = new Timer(_ =>
        {
            lock (gate) if (!stopping && !suspended && fault is null) Schedule();
        });
        try
        {
            native = createContext() ?? throw new GraphicsDeviceException("The window backend does not provide a GPU surface.");
            Device = RenderWorker.Invoke(() =>
            {
                native.Initialize();
                native.MakeCurrent(1, 1);
                var device = ReadDevice(native);
                (sampleCount,stencilBits)=ReadFramebufferFormat(native);
                gl = native.IsEmbedded ? GRGlInterface.CreateGles(native.GetProcAddress) : GRGlInterface.CreateOpenGl(native.GetProcAddress);
                context = gl is null ? null : GRContext.CreateGl(gl);
                if (context is null) throw new GraphicsDeviceException("The installed Skia library cannot initialize this OpenGL device.");
                context.SetResourceCacheLimit(64 * 1024 * 1024);
                return device;
            });
        }
        catch { Dispose(); throw; }
    }

    internal void Submit(RecordedComposition picture, PaintTarget target, Color background, IReadOnlyList<PixelRect>? damage, int uiFrame)
    {
        lock (gate)
        {
            if (stopping || suspended || fault is not null) { picture.Dispose(); return; }
            if (pending is not null)
            {
                pending.Dispose();
                damage = null;
                Interlocked.Increment(ref replaced);
            }
            pending = new(picture, target, background, damage?.ToArray(),uiFrame);
            Schedule();
        }
    }

    internal void SetVisible(bool visible)
    {
        lock (gate)
        {
            suspended = !visible;
            if (!visible) { pending?.Dispose(); pending = null; animationTimer.Change(Timeout.Infinite,Timeout.Infinite); }
        }
        if (!visible) RenderWorker.Invoke(() => true);
    }

    internal Task<RenderedImage> CaptureAsync()
    {
        lock (gate)
        {
            if (stopping) return Task.FromException<RenderedImage>(new ObjectDisposedException(nameof(OpenGlCompositor)));
            if (fault is not null) return Task.FromException<RenderedImage>(fault);
            capture ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
            Schedule();
            return capture.Task;
        }
    }

    private void Schedule()
    {
        if (queued) return;
        queued = true;
        RenderWorker.Post(Render);
    }

    private void Render()
    {
        Frame? frame;
        TaskCompletionSource<RenderedImage>? readback;
        var keepAnimating = false;
        lock (gate)
        {
            if (stopping || (suspended && capture is null)) { queued = false; return; }
            frame = pending; pending = null;
            readback = capture; capture = null;
        }
        try
        {
            if (frame is not null)
            {
                active?.Dispose(); active = frame;
                animationNotified = false;
                lock (gate) if (!stopping) animationTimer.Change(Timeout.Infinite,Timeout.Infinite);
            }
            {
                if (active is { } current && !suspended && (frame is not null || current.Picture.HasAnimations)) Draw(current);
                if (readback is not null)
                {
                    if (retained is null) readback.TrySetException(new InvalidOperationException("The window has not rendered a GPU frame yet."));
                    else
                    {
                        native!.MakeCurrent(size.Width, size.Height);
                        using var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
                        if (!retained.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
                            throw new GraphicsDeviceException("Cannot capture the GPU framebuffer.");
                        var pixels = new byte[checked(size.Width * size.Height * 4)];
                        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
                        readback.TrySetResult(new(size.Width, size.Height, pixels));
                    }
                }
            }
            lock (gate)
            {
                if (!stopping && !suspended && active is { } animation)
                {
                    keepAnimating = animation.Picture.IsAnimating;
                    if (!keepAnimating && animation.Picture.HasAnimations && !animationNotified)
                    {
                        animationNotified = true;
                        animationCompleted?.Invoke();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            readback?.TrySetException(exception);
            var notify=false;
            lock (gate)
            {
                fault = exception;
                if (!stopping) { animationTimer.Change(Timeout.Infinite,Timeout.Infinite); notify=true; }
                pending?.Dispose(); pending = null;
                capture?.TrySetException(exception); capture = null;
            }
            if (notify) failed(exception);
        }
        finally
        {
            lock (gate)
            {
                queued = false;
                if (!stopping && fault is null)
                {
                    if (pending is not null || capture is not null || (keepAnimating && !suspended && native!.IsPresentationSynchronized)) Schedule();
                    else if (keepAnimating && !suspended) animationTimer.Change(16, Timeout.Infinite);
                }
            }
        }
    }

    private void Draw(Frame frame)
    {
        var started = Stopwatch.GetTimestamp();
        native!.MakeCurrent(frame.Target.Width, frame.Target.Height);
        if (context!.IsAbandoned) throw new GraphicsDeviceException("The GPU graphics context was lost.");
        var full = size != (frame.Target.Width, frame.Target.Height);
        if (full)
        {
            retained?.Dispose(); retained = null;
            retained = SKSurface.Create(context, false, new SKImageInfo(frame.Target.Width, frame.Target.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
                ?? throw new GraphicsDeviceException("Cannot allocate the retained GPU framebuffer.");
            size = (frame.Target.Width, frame.Target.Height);
        }
        var canvas = retained!.Canvas;
        if (full || frame.Picture.HasAnimations || frame.Damage is null || frame.Damage.Length > 0)
        {
            canvas.Save();
            try
            {
                if (!full && !frame.Picture.HasAnimations && frame.Damage is { } damage)
                {
                    using var pathBuilder = new SKPathBuilder();
                    foreach (var rect in damage) pathBuilder.AddRect(new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom));
                    using var path = pathBuilder.Detach();
                    canvas.ClipPath(path, SKClipOperation.Intersect, false);
                }
                canvas.Clear(new SKColor(frame.Background.R, frame.Background.G, frame.Background.B, frame.Background.A));
                canvas.Scale(frame.Target.Scale);
                frame.Picture.Draw(canvas);
                if (frame.Target.TransparentRegion is { } transparent)
                {
                    canvas.ResetMatrix();
                    using var paint=new SKPaint { Color=SKColors.Transparent, BlendMode=SKBlendMode.Src };
                    canvas.DrawRect(new SKRect(transparent.X,transparent.Y,transparent.Right,transparent.Bottom),paint);
                }
            }
            finally { canvas.Restore(); }
        }
        using var backend = new GRBackendRenderTarget(size.Width, size.Height, sampleCount, stencilBits, new GRGlFramebufferInfo(0, 0x8058));
        using var surface = SKSurface.Create(context, backend, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888)
            ?? throw new GraphicsDeviceException("Cannot wrap the GPU window framebuffer.");
        using var image = retained.Snapshot();
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);
        context.Flush();
        context.Submit();
        if (native is IConditionalOpenGlPresentation conditional)
        {
            if (!conditional.TryPresent()) return;
        }
        else native.Present();
        var presentation=new Presentation(++presented,Stopwatch.GetElapsedTime(started),frame.UiFrame);
        Volatile.Write(ref lastPresentation,presentation);
        presentedFrame?.Invoke(presentation);
    }

    private static unsafe (int Samples,int StencilBits) ReadFramebufferFormat(IOpenGlWindowContext context)
    {
        var address=context.GetProcAddress("glGetIntegerv");
        if (address==0) throw new GraphicsDeviceException("OpenGL framebuffer format queries are unavailable.");
        var query=(delegate* unmanaged<uint,int*,void>)address;
        var samples=0; var stencil=0;
        query(0x80A9,&samples); query(0x0D57,&stencil);
        if (samples<0 || stencil is not (0 or 8 or 16)) throw new GraphicsDeviceException("The OpenGL window framebuffer format is unsupported.");
        return (samples,stencil);
    }

    private static unsafe string ReadDevice(IOpenGlWindowContext context)
    {
        var address = context.GetProcAddress("glGetString");
        if (address == 0) throw new GraphicsDeviceException("OpenGL device identification is unavailable.");
        var device = Marshal.PtrToStringAnsi(((delegate* unmanaged<uint, nint>)address)(0x1F01));
        if (string.IsNullOrWhiteSpace(device)) throw new GraphicsDeviceException("OpenGL did not identify a rendering device.");
        if (new[] { "llvmpipe", "softpipe", "swiftshader", "GDI Generic", "software rasterizer", "Microsoft Basic Render Driver" }
            .Any(name => device.Contains(name, StringComparison.OrdinalIgnoreCase)))
            throw new GraphicsDeviceException($"OpenGL selected a software device ({device}); hardware rendering is unavailable.");
        return device;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (stopping) return;
            stopping = true;
            pending?.Dispose(); pending = null;
            capture?.TrySetException(new ObjectDisposedException(nameof(OpenGlCompositor))); capture = null;
        }
        animationTimer.Dispose();
        try
        {
            RenderWorker.Invoke(() =>
            {
                try
                {
                    if (context is not null && !context.IsAbandoned)
                    {
                        try { native!.MakeCurrent(Math.Max(1,size.Width), Math.Max(1,size.Height)); }
                        catch (GraphicsDeviceException) { context.AbandonContext(false); }
                    }
                    retained?.Dispose();
                    active?.Dispose(); active = null;
                    context?.Dispose();
                    gl?.Dispose();
                }
                finally { native?.Dispose(); }
                return true;
            });
        }
        finally { native?.ReleaseDisplay(); }
    }
}
