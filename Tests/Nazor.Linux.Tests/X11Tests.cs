using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

[Collection("Linux desktop")]
public sealed class X11Tests
{
    public static bool HasDisplay => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("NAZOR_TEST_X11") == "1";

    [Theory]
    [InlineData(null, null, null, null, ":0", LinuxDesktopBackend.X11)]
    [InlineData("All", "wayland", "wayland-0", null, ":0", LinuxDesktopBackend.Wayland)]
    [InlineData(null, null, null, "4", ":0", LinuxDesktopBackend.Wayland)]
    [InlineData(null, "x11", "stale", null, ":0", LinuxDesktopBackend.X11)]
    [InlineData("X11", "wayland", "wayland-0", null, ":0", LinuxDesktopBackend.X11)]
    [InlineData("Wayland", "x11", null, null, ":0", LinuxDesktopBackend.Wayland)]
    public void Session_selection_respects_published_modules_and_never_takes_over_DRM(string? published, string? session, string? wayland, string? socket, string? display, LinuxDesktopBackend expected)
    {
        Assert.Equal(expected, LinuxPlatformHost.SelectBackend(LinuxDesktopBackend.Auto, published, session, wayland, socket, display));
        Assert.Equal(LinuxDesktopBackend.X11, LinuxPlatformHost.SelectBackend(LinuxDesktopBackend.X11, published, session, wayland, socket, display));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinuxPlatformHost((LinuxDesktopBackend)123));
    }

    [Fact]
    public void No_published_desktop_libraries_requires_an_explicit_application_host()
    {
        var error = Assert.Throws<InvalidOperationException>(() => LinuxPlatformHost.SelectBackend(
            LinuxDesktopBackend.Auto, "None", "wayland", "wayland-0", null, ":0"));
        Assert.Contains("NazorLinuxNativeLibraries=None", error.Message);
        Assert.Contains("Platform.Host", error.Message);
        Assert.Equal(LinuxDesktopBackend.X11, LinuxPlatformHost.SelectBackend(
            LinuxDesktopBackend.X11, "None", null, null, null, ":0"));
    }

    [Fact(SkipUnless = nameof(HasDisplay), Skip = "Requires an isolated X11 server and EWMH window manager.")]
    public void Windows_render_resize_switch_captions_and_cancel_close_without_changing_handles()
    {
        using var platform = new X11Platform();
        var host = new Host();
        using var window = platform.CreateWindow(host, new("X11 native window", 240, 160) { Decorations = WindowDecorations.Nazor() });
        var handle = window.Handle;
        Assert.Equal("X11", window.NativeHandle?.Descriptor);
        Assert.NotEqual(0, window.NativeHandle?.Handle ?? 0);
        var resized = false;
        host.Painted = () =>
        {
            if (!resized) { resized = true; window.Resize(new(280, 190)); _ = window.SetDecorationsAsync(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None)); window.Title = "Nazor è 😀"; return; }
            if (window.ClientSize != new Size(280, 190)) return;
            Assert.Equal(handle, window.Handle);
            Assert.True(window.Capabilities.CanSetWindowIcon);
            window.Close(); Assert.False(window.IsClosed);
            host.AllowClose = true; window.Close(); platform.Quit(42);
        };
        window.Show(false);
        using var timeout = new Timer(_ => platform.Quit(99), null, 8000, Timeout.Infinite);
        Assert.Equal(42, platform.Run(new UIDispatcher()));
        Assert.True(host.Paints >= 2); Assert.Equal(1, host.Closed);
    }

    [Fact(SkipUnless = nameof(HasDisplay), Skip = "Requires an isolated X11 server and EWMH window manager.")]
    public void Window_manager_confirms_maximize_fullscreen_and_restore()
    {
        using var platform = new X11Platform();
        var host = new Host();
        using var window = platform.CreateWindow(host, new("X11 EWMH", 240, 160) { Decorations = WindowDecorations.Nazor() });
        var stage = 0;
        host.Painted = () => { if (stage == 0) { stage = 1; Assert.True(window.Capabilities.CanMaximize); window.SetState(WindowState.Maximized); } };
        host.StateChanged = state =>
        {
            if (stage == 1 && state == WindowState.Maximized) { stage = 2; window.SetState(WindowState.FullScreen); }
            else if (stage == 2 && state == WindowState.FullScreen) { stage = 3; window.SetState(WindowState.Normal); }
            else if (stage == 3 && state == WindowState.Normal) { stage = 4; platform.Quit(42); }
        };
        window.Show(false);
        using var timeout = new Timer(_ => platform.Quit(99), null, 8000, Timeout.Infinite);
        Assert.Equal(42, platform.Run(new UIDispatcher())); Assert.Equal(4, stage);
    }

    [Fact(SkipUnless = nameof(HasDisplay), Skip = "Requires an isolated X11 server.")]
    public async Task Clipboard_transfers_large_UTF8_between_independent_event_loops()
    {
        var text = string.Concat(Enumerable.Repeat("Nazor è 😀\n", 20000));
        var ready = new TaskCompletionSource<LinuxDesktopPlatform>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerTask = new LinuxPlatformHost(LinuxDesktopBackend.X11).RunAsync(platform =>
        {
            using var owner = platform.CreateWindow(new Host(), new("Clipboard owner", 100, 60));
            Assert.True(owner.Clipboard.SetText(text)); ready.SetResult((LinuxDesktopPlatform)platform);
            return platform.Run(new UIDispatcher());
        });
        var ownerPlatform = await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        try
        {
            using var reader = new X11Platform();
            using var window = reader.CreateWindow(new Host(), new("Clipboard reader", 100, 60));
            Assert.Equal(text, window.Clipboard.GetText());
        }
        finally { ownerPlatform.Quit(42); }
        Assert.Equal(42, await ownerTask.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Theory(SkipUnless = nameof(HasDisplay), Skip = "Requires X11 and Mesa EGL/OpenGL ES.")]
    [InlineData(0)]
    [InlineData(16)]
    public void Egl_context_matches_the_X11_visual_and_releases_its_display(int outset)
    {
        using var platform = new X11Platform();
        using var window = platform.CreateWindow(new Host(), new("X11 EGL", 160, 100) { Decorations = WindowDecorations.Nazor() });
        window.Show(false);
        unsafe { platform.Native.OuterResize(window.Handle, outset); }
        var context = window.CreateOpenGlContext()!;
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                try { context.Initialize(); context.MakeCurrent(160, 100); Assert.NotEqual(0, context.GetProcAddress("glClear")); }
                finally { context.Dispose(); }
            }
            catch (Exception error) { failure = error; }
        });
        worker.Start(); Assert.True(worker.Join(10000));
        context.ReleaseDisplay();
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private sealed class Host : IWindowHost
    {
        internal int Paints, Closed;
        internal bool AllowClose;
        internal Action? Painted;
        internal Action<WindowState>? StateChanged;
        public void OnResized(Size size, float scale) { }
        public unsafe IReadOnlyList<PixelRect>? OnPaint(PaintTarget target) { new Span<byte>((void*)target.Pixels, target.Stride * target.Height).Fill(255); Paints++; Painted?.Invoke(); return null; }
        public void OnStateChanged(WindowState state) => StateChanged?.Invoke(state);
        public void OnPointerMoved(Point point, KeyModifiers modifiers) { }
        public void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerCanceled() { }
        public bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat) => false;
        public bool OnKeyUp(string key, KeyModifiers modifiers) => false;
        public void OnTextInput(string text) { }
        public void OnComposition(string text, int cursor) { }
        public void OnCompositionCommit(string text) { }
        public void OnFocusChanged(bool focused) { }
        public void OnVisibilityChanged(bool visible) { }
        public void OnSystemSettingsChanged() { }
        public bool OnClosing() => AllowClose;
        public void OnClosed() => Closed++;
        public PlatformCursor GetCursor(Point point) => PlatformCursor.Arrow;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }
}
