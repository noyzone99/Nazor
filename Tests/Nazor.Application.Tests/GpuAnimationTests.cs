using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GpuMotionContent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0,"Container");
        builder.AddAttribute(1,"class","motion");
        builder.CloseElement();
    }
}

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class GpuAnimationTests
{
    [Fact]
    public async Task A_running_transition_takes_priority_over_a_delayed_animation_on_windows()
    {
        var sheet = NssCompiler.Compile("animation-deadline", """
            .motion { width: 50px; height: 50px; background: red; opacity: 1;
                animation: late 1s 5s linear; transition: opacity 2s linear; }
            .motion:active { opacity: 0; }
            @keyframes late { from { transform: translateX(0px); } to { transform: translateX(50px); } }
            """);
        Assert.Empty(sheet.Diagnostics);
        using var harness = new WindowHarness(typeof(GpuMotionContent), new()
        {
            Rendering = RenderingMode.Cpu, StyleSheet = sheet.StyleSheet, ReducedMotion = false,
        });
        harness.WaitUntil(window => window.FrameCount > 0, "initial frame and delayed animation");
        var before = harness.Read(window =>
        {
            window.Root.Elements("Container").Single().SetState(ElementStates.Pressed, true);
            window.RequestFrame();
            return window.FrameCount;
        });
        harness.WaitUntil(window => window.FrameCount > before, "start of the opacity transition");

        var cancellation = TestContext.Current.CancellationToken;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(1))
        {
            var opacity = harness.Read(window => window.Scene.GetEffectiveValue(window.Root.Elements("Container").Single(), StyleProperties.Opacity));
            if (opacity < 0.9) return;
            await Task.Delay(20, cancellation);
        }
        Assert.Fail("The opacity transition did not advance while the delayed animation was pending.");
    }

    [Fact]
    public void Layout_motion_keeps_the_ui_pipeline_instead_of_exporting_an_incomplete_snapshot()
    {
        var sheet=NssCompiler.Compile("mixed-motion","""
            .motion { height:50px; background:#f00000; animation:grow 1000ms linear both; }
            @keyframes grow { from { width:20px; opacity:0; } to { width:120px; opacity:1; } }
            """);
        using var harness=new WindowHarness(typeof(GpuMotionContent),new() { Rendering=RenderingMode.Auto, StyleSheet=sheet.StyleSheet, ReducedMotion=false });
        harness.WaitUntil(window => window.FrameCount>0,"mixed animation");
        Assert.False(harness.Read(window => window.Scene.CanAnimateOnCompositor));
        var before=harness.Read(window => window.FrameCount);
        harness.WaitUntil(window => window.FrameCount>before,"a subsequent UI frame during the animation");
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
    }

    [Fact]
    public async Task Nss_transform_and_opacity_keep_animating_while_the_ui_thread_is_busy()
    {
        var sheet = NssCompiler.Compile("compositor-motion", """
            .motion { width:50px; height:50px; background:#f00000; animation:move 1200ms linear both; }
            @keyframes move { from { transform:translateX(0px); opacity:0; } to { transform:translateX(120px); opacity:1; } }
            """);
        Assert.Empty(sheet.Diagnostics);
        using var harness = new WindowHarness(typeof(GpuMotionContent),new()
        {
            Rendering=RenderingMode.Auto, StyleSheet=sheet.StyleSheet, ColorScheme=ColorScheme.Light,
        });
        if (harness.Read(window => window.Rendering.Actual) == RenderingMode.Cpu) Assert.Skip("Hardware GPU unavailable.");
        harness.WaitUntil(window => window.GpuFramesPresented > 1 || window.Exceptions.Count > 0,"compositor animation");
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
        var capture = harness.Read(window =>
        {
            var uiFrames = window.FrameCount;
            var gpuFrames = window.GpuFramesPresented;
            Thread.Sleep(180);
            var readback = window.CaptureFrameAsync();
            Thread.Sleep(80);
            Assert.Equal(uiFrames,window.FrameCount);
            Assert.True(window.GpuFramesPresented > gpuFrames+1,"GPU frames must advance without another UI paint.");
            Assert.True(readback.IsCompletedSuccessfully,"Readback must complete without calling the UI thread.");
            return readback;
        });
        var image = await capture;
        var colored = Enumerable.Range(0,image.Width).Where(x =>
        {
            var pixel = image.GetPixel(x,25);
            return pixel.R > pixel.G+20 && pixel.R > pixel.B+20;
        }).ToArray();
        Assert.True(colored.Length >= 40,"The initially transparent content must become visible during render-thread animation.");
        Assert.True(colored[0] > 1,"The initially stationary content must move during render-thread animation.");
        var currentTransform=harness.Read(window => window.Scene.GetTransform(window.Root.Elements("Container").Single()).OffsetX*window.Scene.Scale);
        Assert.True(currentTransform>=colored[0]-1,"Input geometry must sample at least the position visible in the captured frame.");
        harness.WaitUntil(window => !window.Scene.Transitions.IsRunning,"animation completion");
        Assert.Empty(harness.Read(window => window.Exceptions.ToArray()));
    }
}
