using Nazor.Diagnostics;
using Nazor.DevTools;
using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class DiagnosticsWindowTests
{
    [Fact]
    public void Inspection_consumes_the_real_pointer_and_records_frames_without_animating_at_rest()
    {
        using var harness = new WindowHarness(typeof(Counter), new ApplicationRuntimeSettings { Width = 320, Height = 200, ReducedMotion = true });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        Assert.Null(harness.Read(w => w.GetDiagnostics()));
        var point = harness.Read(w =>
        {
            var session = w.EnableDiagnostics();
            Assert.Same(session, w.EnableDiagnostics());
            session.SetInspectMode(true);
            var bounds = w.Scene.GetBounds(w.Root.Single("Button"));
            return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        });
        Assert.Equal(PlatformCursor.Cross, harness.Read(w => ((IWindowHost)w).GetCursor(point)));
        harness.Click(point);
        harness.WaitUntil(w => w.GetDiagnostics() is { SelectedId: not null, IsInspecting: false }, "selection");
        Assert.Equal("Count: 0", harness.Read(w => w.Root.Single("TextBlock").Text()));
        Assert.Null(harness.Read(w => w.Input.Focused));
        harness.WaitUntil(w => w.GetDiagnostics()!.Frames.Any(f => f.Cause.HasFlag(FrameCause.DevTools)), "overlay frame");
        var frames = harness.Read(w =>
        {
            Nazor.Platform.Windows.Win32.UpdateWindow(w.PlatformWindow.Handle);
            return w.FrameCount;
        });
        Thread.Sleep(150);
        Assert.Equal(frames, harness.Read(w => w.FrameCount));
        var recorded = harness.Read(w => w.GetDiagnostics()!.Frames.Last());
        Assert.True(recorded.Total >= recorded.Styles + recorded.Layout + recorded.DisplayList + recorded.Overlay + recorded.Raster);
        Assert.Equal(frames, recorded.Number);
        harness.Read(w => { w.DisableDiagnostics(); return true; });
        harness.WaitUntil(w => w.FrameCount > frames, "frame without diagnostics");
        Assert.Null(harness.Read(w => w.GetDiagnostics()));
        harness.Click(point);
        harness.WaitUntil(w => w.Root.Single("TextBlock").Text() == "Count: 1", "ordinary click");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Overlay_pixels_appear_and_are_erased_after_detaching_tools()
    {
        NativeDesktop.RequireUnlocked();
        using var harness = new WindowHarness(typeof(Counter), new ApplicationRuntimeSettings { Width = 320, Height = 200, ReducedMotion = true });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var (probe, frames) = harness.Read(w =>
        {
            var session = w.EnableDiagnostics();
            var button = w.Root.Single("Button");
            session.Highlight(session.IdOf(button));
            var box = session.Box(session.IdOf(button))!;
            return (new Point((box.PaddingBox.X + 4) * box.Scale, (box.PaddingBox.Y + box.PaddingBox.Height / 2) * box.Scale), w.FrameCount);
        });
        harness.WaitUntil(w => w.FrameCount > frames, "highlight paint");
        var plain = new Color(0x30, 0x30, 0x30, 255);
        var image = ScreenCapture.Capture(harness.Handle);
        static bool IsPaddingColor(Color color) => color.G > color.R && color.G > color.B && color.R > 48;
        for (var attempt = 0; attempt < 100 && !IsPaddingColor(image.GetPixel((int)probe.X, (int)probe.Y)); attempt++)
        {
            Thread.Sleep(20);
            image = ScreenCapture.Capture(harness.Handle);
        }
        Assert.True(IsPaddingColor(image.GetPixel((int)probe.X, (int)probe.Y)));
        var artifacts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/diagnostics"));
        Directory.CreateDirectory(artifacts);
        using (var graphics = new Nazor.Skia.SkiaRenderer())
            File.WriteAllBytes(Path.Combine(artifacts, "counter-inspect.png"), graphics.EncodePng(image));
        frames = harness.Read(w => { w.DisableDiagnostics(); return w.FrameCount; });
        harness.WaitUntil(w => w.FrameCount > frames, "erased overlay");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            image = ScreenCapture.Capture(harness.Handle);
            if (image.GetPixel((int)probe.X, (int)probe.Y) == plain) break;
            Thread.Sleep(20);
        }
        Assert.Equal(plain, image.GetPixel((int)probe.X, (int)probe.Y));
    }

    [Fact]
    public void Frame_causes_combine_and_resize_has_counters_from_the_paint_update()
    {
        using var harness = new WindowHarness(typeof(Counter), new ApplicationRuntimeSettings { Width = 320, Height = 200, ReducedMotion = true });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        harness.Read(w =>
        {
            w.EnableDiagnostics();
            w.RequestFrame(WindowFrameCause.Overlay);
            w.RequestFrame(WindowFrameCause.HotReload);
            return true;
        });
        harness.WaitUntil(w => w.GetDiagnostics()!.Frames.Any(f => f.Cause.HasFlag(FrameCause.DevTools | FrameCause.HotReload)), "combined causes");
        harness.Read(w => { w.PlatformWindow.Resize(new Size(360, 220)); return true; });
        harness.WaitUntil(w => w.GetDiagnostics()!.Frames.Any(f => f.Cause.HasFlag(FrameCause.Resize)), "resize recorded");
        var resize = harness.Read(w => w.GetDiagnostics()!.Frames.Last(f => f.Cause.HasFlag(FrameCause.Resize)));
        Assert.True(resize.LayoutComputations > 0);
        Assert.Null(resize.Damage);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }
}
