using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class RenderingRegressionTests
{
    private static ApplicationRuntimeSettings Options(RenderingMode mode, bool fluent = true) => new()
    {
        Rendering = mode == RenderingMode.Gpu ? RenderingMode.Auto : mode, Width = 440, Height = 280,
        Theme = fluent ? Nazor.Themes.Themes.Basic : null,
        StyleSheet = NssCompiler.Compile("regression", """
            .caption { height: var(--nz-window-controls-height, 32px); display:flex; align-items:center; background:#e1edfa; }
            .body { display:flex; flex-direction:column; padding:24px; gap:12px; }
            """).StyleSheet,
    };

    private static void Prepare(WindowHarness harness, RenderingMode mode)
    {
        if (mode == RenderingMode.Gpu) NativeDesktop.RequireUnlocked();
        Assert.True(SetWindowPos(harness.Handle, 0, 30, 30, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
        harness.WaitUntil(w => w.FrameCount > 0, "initial paint");
        if (mode == RenderingMode.Gpu && harness.Read(w => w.Rendering.Actual) != RenderingMode.Gpu)
            Assert.Skip("Hardware GPU unavailable.");
        if (mode == RenderingMode.Gpu) harness.Read(w => { w.IsAlwaysOnTop = true; return 0; });
    }

    private static void Click(WindowHarness harness, string id)
    {
        var point = harness.Read(w =>
        {
            var bounds = w.Scene.GetBounds(w.Root.Elements("CheckBox").Single(e => e.Id == id));
            var logical = new Point(bounds.X + 8, bounds.Y + bounds.Height / 2);
            if (w.Rendering.Actual == RenderingMode.Gpu)
            {
                var screen = new POINT { X = (int)MathF.Round(logical.X * w.Scene.Scale), Y = (int)MathF.Round(logical.Y * w.Scene.Scale) };
                Assert.True(ClientToScreen(w.PlatformWindow.Handle, ref screen));
                Assert.Equal(w.PlatformWindow.Handle, WindowFromPoint(screen));
            }
            return logical;
        });
        harness.Click(point);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [Theory]
    [InlineData(RenderingMode.Cpu)]
    [InlineData(RenderingMode.Gpu)]
    public void Custom_caption_toggling_preserves_its_height_and_the_content_offset(RenderingMode mode)
    {
        using var harness = new WindowHarness(typeof(InteractionWindow), Options(mode));
        Prepare(harness, mode);
        for (var i = 0; i < 3; i++)
        {
            var before = harness.Read(w => { w.ApplyDecorations((true) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native()); return w.FrameCount; });
            harness.WaitUntil(w => w.FrameCount > before, "custom caption paint");
            harness.Read(w => { AssertCaption(w); return 0; });
            if (mode == RenderingMode.Gpu) AssertGpuCaption(harness);
            before = harness.Read(w => { w.ApplyDecorations((false) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native()); return w.FrameCount; });
            harness.WaitUntil(w => w.FrameCount > before, "system caption paint");
            Assert.Empty(harness.Read(w => w.Root.Elements("TitleBar").ToArray()));
        }
        Assert.Empty(harness.Read(w => w.Exceptions.ToArray()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cpu_checkbox_presents_mouse_and_keyboard_changes_without_another_pointer_message(bool fluent)
    {
        using var harness = new WindowHarness(typeof(InteractionWindow), Options(RenderingMode.Cpu, fluent));
        Prepare(harness, RenderingMode.Cpu);
        Click(harness, "value");
        harness.WaitUntil(w => w.Root.Elements("CheckBox").Single(e => e.Id == "value").Attributes.GetValueOrDefault("checked") is true, "checked state");
        Thread.Sleep(400);
        harness.Read(w => { AssertPresentedMark(w, "value"); return 0; });
        harness.Key(0x20, true);
        harness.Key(0x20, false);
        harness.WaitUntil(w => w.Root.Elements("CheckBox").Single(e => e.Id == "value").Attributes.GetValueOrDefault("checked") is false, "unchecked keyboard state");
        Thread.Sleep(400);
        harness.Read(w => { AssertPresentedMark(w, "value"); return 0; });
    }

    [Theory]
    [InlineData(RenderingMode.Cpu, "caption-toggle", false)]
    [InlineData(RenderingMode.Cpu, "buttons-toggle", false)]
    [InlineData(RenderingMode.Gpu, "caption-toggle", false)]
    [InlineData(RenderingMode.Gpu, "buttons-toggle", false)]
    [InlineData(RenderingMode.Cpu, "caption-toggle", true)]
    [InlineData(RenderingMode.Cpu, "buttons-toggle", true)]
    [InlineData(RenderingMode.Gpu, "caption-toggle", true)]
    [InlineData(RenderingMode.Gpu, "buttons-toggle", true)]
    public void Bound_window_checkbox_updates_caption_and_mark_without_mouse_motion(RenderingMode mode, string id, bool fluent)
    {
        using var harness = new WindowHarness(typeof(InteractionWindow), Options(mode, fluent));
        Prepare(harness, mode);
        if (id == "buttons-toggle") harness.Read(w => { w.ApplyDecorations((true) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native()); return 0; });
        Thread.Sleep(400);
        for (var i = 0; i < 3; i++)
        {
            var expected = harness.Read(w => id == "caption-toggle" ? !(w.AppliedDecorations?.Configuration.HasNativeTitleBar == false) : !(w.AppliedDecorations?.Configuration.CaptionButtons == WindowCaptionButtons.Native));
            Click(harness, id);
            harness.WaitUntil(w => (id == "caption-toggle" ? (w.AppliedDecorations?.Configuration.HasNativeTitleBar == false) : (w.AppliedDecorations?.Configuration.CaptionButtons == WindowCaptionButtons.Native)) == expected, "window property toggle");
            Thread.Sleep(400);
            harness.Read(w =>
            {
                Assert.Equal(expected, w.Root.Elements("CheckBox").Single(e => e.Id == id).Attributes.GetValueOrDefault("checked"));
                if ((w.AppliedDecorations?.Configuration.HasNativeTitleBar == false)) AssertCaption(w);
                if (mode == RenderingMode.Cpu) AssertPresentedMark(w, id);
                Assert.Empty(w.Exceptions);
                return 0;
            });
            if (mode == RenderingMode.Gpu && harness.Read(w => (w.AppliedDecorations?.Configuration.HasNativeTitleBar == false))) AssertGpuCaption(harness);
        }
    }

    private static void AssertGpuCaption(WindowHarness harness)
    {
        Thread.Sleep(400);
        NativeDesktop.RequireUnlocked();
        var actual = harness.Read(w => ScreenCapture.CaptureDesktopWindow(w.PlatformWindow.Handle));
        var scale = harness.Read(w => w.Scene.Scale);
        var capture = harness.Read(w => w.CaptureFrameAsync());
        Assert.True(capture.Wait(TimeSpan.FromSeconds(10)), "GPU capture timed out.");
        if (actual.GetPixel((int)(180 * scale), (int)(16 * scale)) != new Color(225, 237, 250, 255))
        {
            Directory.CreateDirectory("artifacts/caption-regression");
            using var encoder = new Skia.SkiaRenderer();
            File.WriteAllBytes("artifacts/caption-regression/desktop.png", encoder.EncodePng(actual));
            File.WriteAllBytes("artifacts/caption-regression/retained.png", encoder.EncodePng(capture.Result));
            File.WriteAllText("artifacts/caption-regression/layout.txt", harness.Read(w => w.Scene.DescribeLayout()));
        }
        Assert.Equal(new Color(225, 237, 250, 255), actual.GetPixel((int)(180 * scale), (int)(16 * scale)));
        Assert.Equal(new Color(225, 237, 250, 255), capture.Result.GetPixel((int)(180 * scale), (int)(16 * scale)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Gpu_partial_shadow_repaint_matches_a_full_repaint(bool inset)
    {
        var sheet = NssCompiler.Compile("shadow-regression", """
            .stage { width:100%; height:100%; background:#ffffff; }
            .marker { position:absolute; left:5px; top:5px; width:29.25px; height:20px; background:#000000; }
            .marker.changed { background:#ffffff; }
            .shadow { position:absolute; left:20.25px; top:20.5px; width:60px; height:50px; border-radius:10px; opacity:0.6;
            """ + $"box-shadow:{(inset ? "inset " : "")}2px 3px 12px 1px #0a1e32a0; }}").StyleSheet;
        using var harness = new WindowHarness(typeof(ShadowCornerWindow), Options(RenderingMode.Gpu, false) with { StyleSheet = sheet, ReducedMotion = true });
        Prepare(harness, RenderingMode.Gpu);
        var before = harness.Read(w =>
        {
            var frame = w.FrameCount;
            ((ShadowCornerWindow)w.Root.Component).Change();
            return frame;
        });
        harness.WaitUntil(w => w.FrameCount > before, "partial shadow paint");
        var partial = Capture();
        var width = harness.Read(w => w.Width);
        Resize(width + 4);
        Resize(width);
        var full = Capture();
        Assert.Equal(full.Width, partial.Width);
        Assert.Equal(full.Height, partial.Height);
        Assert.Equal(full.Pixels, partial.Pixels);

        RenderedImage Capture()
        {
            var capture = harness.Read(w => w.CaptureFrameAsync());
            Assert.True(capture.Wait(TimeSpan.FromSeconds(10)), "GPU shadow capture timed out.");
            return capture.GetAwaiter().GetResult();
        }

        void Resize(double value)
        {
            var frame = harness.Read(w => { var previous = w.FrameCount; w.Width = value; return previous; });
            harness.WaitUntil(w => w.FrameCount > frame, "full shadow paint after resize");
        }
    }

    private sealed class ShadowCornerWindow : ComponentBase
    {
        private bool changed;
        public void Change() { changed = true; StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Container>(0);
            builder.AddComponentParameter(1, "Class", "stage");
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<Container>(0);
                b.AddComponentParameter(1, "Class", changed ? "marker changed" : "marker");
                b.CloseComponent();
                b.OpenComponent<Container>(2);
                b.AddComponentParameter(3, "Class", "shadow");
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private static void AssertCaption(NazorWindow window)
    {
        var caption = window.Scene.GetBounds(Assert.Single(window.Root.Elements("TitleBar")));
        var body = window.Scene.GetBounds(window.Root.Elements("Container").Single(e => e.Class == "body"));
        Assert.True(caption.Height >= 30, window.Scene.DescribeLayout());
        Assert.True(body.Y >= caption.Bottom, window.Scene.DescribeLayout());
    }

    private static void AssertPresentedMark(NazorWindow window, string id)
    {
        var checkbox = window.Root.Elements("CheckBox").Single(e => e.Id == id);
        var bounds = window.Scene.GetBounds(checkbox.Elements().Single(e => e.Name == "CheckBoxMark"));
        var actual = ScreenCapture.CapturePresented(window.PlatformWindow.Handle);
        using var renderer = new Skia.SkiaRenderer();
        var expected = renderer.RenderToImage(window.Scene.BuildDisplayList(), window.Scene.Scale, window.Scene.Background);
        var different = 0;
        for (var y = (int)(bounds.Y * window.Scene.Scale); y < (int)(bounds.Bottom * window.Scene.Scale); y++)
            for (var x = (int)(bounds.X * window.Scene.Scale); x < (int)(bounds.Right * window.Scene.Scale); x++)
            {
                var a = expected.GetPixel(x, y); var b = actual.GetPixel(x, y);
                if (Math.Abs(a.R - b.R) > 1 || Math.Abs(a.G - b.G) > 1 || Math.Abs(a.B - b.B) > 1) different++;
            }
        Assert.True(different == 0, $"{id} has {different} stale pixels at {bounds} after {window.FrameCount} frames.");
    }

    private sealed class InteractionWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Native());
        }

        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenComponent<UserTitleBar>(0, typeof(InteractionWindow), this);
            builder.AddComponentParameter(1, "Class", "caption");
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Custom caption")));
            builder.CloseComponent();
            builder.OpenComponent<Container>(3, typeof(InteractionWindow), this);
            builder.AddComponentParameter(4, "Class", "body");
            builder.AddComponentParameter(5, "ChildContent", (RenderFragment)(b =>
            {
                AddCheckbox(b, 0, "value", null, null, "Value");
                AddCheckbox(b, 10, "caption-toggle", (Window.AppliedDecorations?.Configuration.HasNativeTitleBar == false),
                    value => Window.ApplyDecorations(value ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native()), "Custom caption");
                AddCheckbox(b, 20, "buttons-toggle", (Window.AppliedDecorations?.Configuration.CaptionButtons == WindowCaptionButtons.Native),
                    value => Window.ApplyDecorations(DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User, value)), "System buttons");
            }));
            builder.CloseComponent();
        }

        private void AddCheckbox(RenderTreeBuilder builder, int sequence, string id, bool? value, Action<bool>? changed, string label)
        {
            builder.OpenComponent<CheckBox>(sequence, typeof(InteractionWindow), this);
            builder.AddComponentParameter(sequence + 1, "Id", id);
            if (value is { } state)
            {
                builder.AddComponentParameter(sequence + 2, "IsChecked", state);
                builder.AddComponentParameter(sequence + 3, "IsCheckedChanged", EventCallback.Factory.Create<bool>(this, changed!));
            }
            builder.AddComponentParameter(sequence + 4, "ChildContent", (RenderFragment)(b => b.AddContent(0, label)));
            builder.CloseComponent();
        }
    }
}
