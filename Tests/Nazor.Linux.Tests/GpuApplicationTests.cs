using Nazor.Graphics;
using Nazor.Platform.Linux;
using Nazor.Rendering;

namespace Nazor.Linux.Tests;

public sealed class GpuContent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "width:100%;height:100%;background:#d02030");
        builder.AddContent(2, "Nazor hardware rendering");
        builder.CloseElement();
    }
}

[Collection("Linux desktop")]
public sealed class GpuApplicationTests
{
    public static bool HasCompositor => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("NAZOR_TEST_COMPOSITOR") == "1";
    public static bool HasX11 => X11Tests.HasDisplay;

    [Fact(SkipUnless = nameof(HasX11), Skip = "Requires an isolated X11 display.")]
    public async Task X11_outset_preserves_client_rendering_through_live_style_and_size_changes()
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost(LinuxDesktopBackend.X11));
        builder.Application.UseMainWindow<OutsetWindow>();
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var busy = false;
        var updated = false;
        builder.Diagnostics.ConfigureWindow = window => window.FramePresented += frame =>
        {
            if (busy || complete.Task.IsCompleted || updated && window.PlatformWindow.ClientSize != new Size(280, 180)) return;
            busy = true;
            _ = window.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    Assert.Empty(window.Exceptions);
                    var requested = Environment.GetEnvironmentVariable("NAZOR_RENDERER");
                    if (requested == "GPU") Assert.Equal(RenderingMode.Gpu, frame.Renderer);
                    if (requested == "CPU") Assert.Equal(RenderingMode.Cpu, frame.Renderer);
                    var size = updated ? new Size(280, 180) : new Size(240, 160);
                    Assert.Equal(size, window.PlatformWindow.ClientSize);
                    var image = await window.CaptureFrameAsync();
                    Assert.Equal((int)Math.Ceiling(size.Width * window.Scene.Scale), image.Width);
                    Assert.Equal((int)Math.Ceiling(size.Height * window.Scene.Scale), image.Height);
                    Assert.Equal(new Color(208, 32, 48, 255), image.GetPixel(image.Width / 2, image.Height / 2));
                    if (!updated)
                    {
                        updated = true;
                        window.Style = "background:#d02030; -window-frame-resize-outset:32px";
                        window.PlatformWindow.Resize(new(280, 180));
                        window.RequestFrame();
                    }
                    else { complete.TrySetResult(); window.Close(); }
                }
                catch (Exception error) { complete.TrySetException(error); window.Close(); }
                finally { busy = false; }
            });
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        await complete.Task.WaitAsync(timeout.Token);
    }

    public sealed class OutsetWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
            Window.Width = 240;
            Window.Height = 160;
            Window.Style = "background:#d02030; -window-frame-resize-outset:16px";
        }
        protected override void BuildWindowContent(RenderTreeBuilder builder) { }
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires a Wayland compositor and explicit native-test opt-in.")]
    public async Task Composition_frames_and_capture_continue_while_the_wayland_ui_thread_is_busy()
    {
        var sheet=Nazor.Nss.NssCompiler.Compile("wayland-motion","""
            Container { animation:move 1200ms linear both; }
            @keyframes move { from { transform:translateX(0px); opacity:0; } to { transform:translateX(120px); opacity:1; } }
            """);
        Assert.Empty(sheet.Diagnostics);
        var builder=NazorApplication.CreateBuilder(platformHost:new LinuxPlatformHost());
        builder.Application.CreateMainWindow<GpuContent>(window => { window.Width=320; window.Height=160; window.Activate=false; });
        Action<NazorWindow,PresentedFrame>? probe=null;
        builder.Diagnostics.ConfigureWindow=window =>
        {
            window.Scene.Styles.Application=sheet.StyleSheet;
            window.FramePresented += frame => probe?.Invoke(window,frame);
        };
        using var app=builder.Build();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var ran=false;
        Exception? failure=null;
        probe=(window,frame) =>
        {
            if (ran || failure is not null) return;
            try
            {
                if (frame.Renderer!=RenderingMode.Gpu) { Assert.Skip("Hardware EGL unavailable."); return; }
                if (window.GpuFramesPresented<2) return;
                ran=true;
                var before=window.GpuFramesPresented;
                var uiFrames=window.FrameCount;
                Thread.Sleep(180);
                var capture=window.CaptureFrameAsync();
                Thread.Sleep(80);
                Assert.Equal(uiFrames,window.FrameCount);
                Assert.True(window.GpuFramesPresented>before+1);
                Assert.True(capture.IsCompletedSuccessfully);
                var image=capture.GetAwaiter().GetResult();
                var row=Math.Clamp(image.Height-(int)Math.Round(window.Scene.Viewport.Height*window.Scene.Scale)+25,0,image.Height-1);
                Assert.Contains(Enumerable.Range(0,image.Width),x => image.GetPixel(x,row).R>image.GetPixel(x,row).G+20);
                Assert.Empty(window.Exceptions);
                app.Lifetime.StopApplication();
            }
            catch (Exception exception) { failure=exception; app.Lifetime.StopApplication(); }
        };
        Assert.Equal(0,await app.RunAsync(timeout.Token));
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.True(ran,"No autonomous composition frames were observed.");
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires a Wayland compositor and explicit native-test opt-in.")]
    public async Task Application_selects_presents_resizes_captures_and_closes_the_requested_renderer()
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost());
        builder.Application.CreateMainWindow<GpuContent>(window => { window.Width = 240; window.Height = 160; window.Activate = false; });
        NazorWindow? target = null;
        builder.Diagnostics.ConfigureWindow = window => target = window;
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Exception? failure = null;
        var complete = false;
        var resized = false;
        long frames = -1;
        Task<RenderedImage>? capture = null;
        using var poll = new Timer(_ =>
        {
            var window = target;
            if (window is null) return;
            window.Dispatcher.Post(() =>
            {
                if (complete || failure is not null) return;
                try
                {
                    Assert.Empty(window.Exceptions);
                    var gpu = window.Rendering.Actual == RenderingMode.Gpu;
                    if (window.FrameCount == 0 || (gpu && window.GpuFramesPresented == 0)) return;
                    if (!resized)
                    {
                        Console.WriteLine($"Nazor Wayland renderer: {window.Rendering}");
                        frames = gpu ? window.GpuFramesPresented : window.FrameCount;
                        window.PlatformWindow.Resize(new(320, 200));
                        window.RequestFrame();
                        resized = true;
                        return;
                    }
                    if ((gpu ? window.GpuFramesPresented : window.FrameCount) <= frames) return;
                    capture ??= window.CaptureFrameAsync();
                    if (!capture.IsCompleted) return;
                    var image = capture.GetAwaiter().GetResult();
                    if (image.Width < 320 || image.Height < 200) { capture = null; return; }
                    Assert.Equal(new Color(208,32,48,255), image.GetPixel(5, image.Height-5));
                    complete = true;
                    app.Lifetime.StopApplication();
                }
                catch (Exception exception) { failure = exception; app.Lifetime.StopApplication(); }
            });
        }, null, 20, 20);
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.True(complete, "The renderer did not complete resize and framebuffer capture before the timeout.");
    }
}
