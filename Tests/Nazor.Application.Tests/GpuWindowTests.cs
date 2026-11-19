using Nazor.Graphics;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class GpuWindowTests
{
    [Fact]
    public async Task Gpu_native_composition_regions_remain_transparent_after_drawing()
    {
        using var harness=new WindowHarness(typeof(NativeAppearancePage),new() { Rendering=RenderingMode.Auto });
        if (harness.Read(window => window.Rendering.Actual)==RenderingMode.Cpu) Assert.Skip("Hardware GPU unavailable.");
        harness.WaitUntil(window => window.GpuFramesPresented>0,"first frame");
        var capture=harness.Read(window =>
        {
            var scale=window.Scene.Scale;
            var size=window.PlatformWindow.ClientSize;
            ((Nazor.Platform.IWindowHost)window).OnPaint(new(0,(int)Math.Round(size.Width*scale),(int)Math.Round(size.Height*scale),0,scale)
                { TransparentRegion=new(10,10,20,20) });
            var task=window.CaptureFrameAsync();
            Assert.True(SpinWait.SpinUntil(() => task.IsCompleted,TimeSpan.FromSeconds(3)));
            return task;
        });
        var image=await capture;
        Assert.Equal(Color.Transparent,image.GetPixel(15,15));
        Assert.Equal(255,image.GetPixel(35,35).A);
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
    }

    [Theory]
    [InlineData(RenderingMode.Cpu)]
    [InlineData(RenderingMode.Auto)]
    public void Backend_completion_identifies_the_submitted_ui_frame(RenderingMode mode)
    {
        using var harness=new WindowHarness(typeof(NativeAppearancePage),new() { Rendering=mode });
        PresentedFrame? presented=null;
        var before=harness.Read(window =>
        {
            window.FramePresented += frame => presented=frame;
            window.RequestFrame();
            return window.FrameCount;
        });
        harness.WaitUntil(_ => presented is { } frame && frame.UiFrame>before,"backend completion notification");
        var completed=harness.Read(_ => presented!);
        Assert.True(completed.BackendDuration>=TimeSpan.Zero);
        Assert.Equal(mode==RenderingMode.Cpu ? RenderingMode.Cpu : harness.Read(window => window.Rendering.Actual),completed.Renderer);
        Assert.Equal(completed.Renderer==RenderingMode.Gpu,completed.RenderWorkerFrame is > 0);
    }

    [Fact]
    public void An_exposed_clean_window_reuses_its_scene_and_drawing_commands()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage),new() { Rendering=RenderingMode.Cpu });
        harness.WaitUntil(window => window.FrameCount > 0,"initial scene");
        var updates = 0;
        var frames = harness.Read(window =>
        {
            window.Scene.Updated += () => updates++;
            window.PlatformWindow.Invalidate();
            return window.FrameCount;
        });
        harness.WaitUntil(window => window.FrameCount > frames,"exposure repaint");
        var sceneUpdates=harness.Read(window => (updates,window.HasSemantics));
        Assert.Equal(sceneUpdates.HasSemantics ? 1 : 0,sceneUpdates.updates);
        Assert.Empty(harness.Read(window => window.LastFrameDamage!));
    }

    [Fact]
    public void Unsupported_gpu_surface_fails_explicitly_and_does_not_hide_programming_errors()
    {
        Assert.Throws<Nazor.Platform.GraphicsDeviceException>(() => new OpenGlCompositor(() => null, _ => Assert.Fail("Initialization failure is synchronous.")));
        var error = new InvalidOperationException("Application initialization error");
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => new OpenGlCompositor(() => throw error, _ => { })));
    }

    [Fact]
    public void Closing_one_hardware_window_preserves_the_other_context_on_the_shared_worker()
    {
        using var first = new WindowHarness(typeof(NativeAppearancePage), new() { Rendering = RenderingMode.Auto });
        if (first.Read(w => w.Rendering.Actual) == RenderingMode.Cpu) Assert.Skip("Hardware GPU unavailable.");
        using var second = new WindowHarness(typeof(NativeAppearancePage), new() { Rendering = RenderingMode.Gpu });
        first.WaitUntil(w => w.GpuFramesPresented > 0, "first GPU window");
        second.WaitUntil(w => w.GpuFramesPresented > 0, "second GPU window");
        var closed=first.Read(window => window);
        first.Close();
        Assert.True(closed.GpuFramesPresented>0);
        var frames = second.Read(w => w.GpuFramesPresented);
        second.Read(w => { w.RequestFrame(); return 0; });
        second.WaitUntil(w => w.GpuFramesPresented > frames, "remaining GPU context");
        Assert.Empty(second.Read(w => w.Exceptions.ToArray()));
    }

    [Fact]
    public void Forced_cpu_never_creates_a_gpu_worker()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage), new() { Rendering = RenderingMode.Cpu });
        harness.WaitUntil(w => w.FrameCount > 0, "software paint");
        var info = harness.Read(w => w.Rendering);
        Assert.Equal(RenderingMode.Cpu, info.Actual);
        Assert.Null(info.FallbackReason);
        Assert.Equal(0, harness.Read(w => w.GpuFramesPresented));
    }

    [Fact]
    public async Task Automatic_hardware_rendering_presents_resizes_updates_and_stops_before_window_destruction()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage), new() { Rendering = RenderingMode.Auto });
        var info = harness.Read(w => w.Rendering);
        Console.WriteLine($"Nazor renderer: {info}");
        if (info.Actual == RenderingMode.Cpu)
        {
            Assert.False(string.IsNullOrWhiteSpace(info.FallbackReason));
            Assert.Skip($"Hardware GPU unavailable: {info.FallbackReason}");
        }
        harness.WaitUntil(w => w.GpuFramesPresented > 0 || w.Exceptions.Count > 0, "first hardware presentation");
        Assert.Empty(harness.Read(w => w.Exceptions.ToArray()));
        var before = harness.Read(w => w.GpuFramesPresented);
        harness.Read(w =>
        {
            w.PlatformWindow.Resize(new(720, 420));
            ((NativeAppearancePage)w.Root.Component).ChangeTitle();
            w.RequestFrame();
            return 0;
        });
        harness.WaitUntil(w => w.GpuFramesPresented > before || w.Exceptions.Count > 0, "resized hardware presentation");
        Assert.Empty(harness.Read(w => w.Exceptions.ToArray()));
        Assert.Equal(RenderingMode.Gpu, harness.Read(w => w.Rendering.Actual));
        var capture = await harness.Read(w => w.CaptureFrameAsync());
        Assert.True(capture.Width > 0 && capture.Height > 0);
        var expected = harness.Read(w =>
        {
            using var graphics = new Nazor.Skia.SkiaRenderer();
            return graphics.RenderToImage(w.Scene.BuildDisplayList(), w.Scene.Scale, w.Scene.Background);
        });
        Assert.Equal(expected.GetPixel(0, 0), capture.GetPixel(0, 0));
        harness.Close();
    }
}
