using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Linux;
using Nazor.Rendering;
using Nazor.Controls;

namespace Nazor.Linux.Tests;

[Collection("Linux desktop")]
public sealed class DesktopLifecycleTests
{
    public static bool HasDesktop => WaylandTests.HasCompositor || X11Tests.HasDisplay;
    public static bool HasX11 => X11Tests.HasDisplay;
    private static LinuxDesktopPlatform Platform() => X11Tests.HasDisplay ? new X11Platform() : new WaylandPlatform();

    [Fact]
    public async Task Default_application_icon_is_packaged_as_a_decodable_image()
    {
        var source = ImageSource.FromResource(typeof(LinuxDesktopPlatform).Assembly, "Nazor.Platform.Linux.ApplicationIcon.png");
        var data = await source.LoadAsync(1, TestContext.Current.CancellationToken);
        using var renderer = new Nazor.Skia.SkiaRenderer();
        var image = Assert.IsAssignableFrom<DecodedImage>(renderer.Images.Decode(data.Bytes));
        try { Assert.Equal(new Size(128, 128), image.Size); }
        finally { (image as IDisposable)?.Dispose(); }
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public async Task Desktop_windows_expose_the_packaged_default_application_icon()
    {
        using var platform = Platform();
        using var window = platform.CreateWindow(new Host(), new("Default icon", 240, 160) { Decorations = WindowDecorations.Nazor() });
        var source = Assert.IsAssignableFrom<ImageSource>(window.ApplicationIcon);
        Assert.Equal(ImageSource.FromResource(typeof(LinuxDesktopPlatform).Assembly, "Nazor.Platform.Linux.ApplicationIcon.png"), source);
        Assert.NotEmpty((await source.LoadAsync(1, TestContext.Current.CancellationToken)).Bytes);
    }

    [Theory(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Complete_decoration_combinations_match_capabilities_and_preserve_the_mapped_window(bool startWithNazor)
    {
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost(HasX11 ? LinuxDesktopBackend.X11 : LinuxDesktopBackend.Wayland));
        if (startWithNazor) builder.Application.UseMainWindow<NazorDecorationWindow>();
        else builder.Application.UseMainWindow<DecorationWindow>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationToken = TestContext.Current.CancellationToken;
        var started = false;
        builder.Diagnostics.ConfigureWindow = window => window.FramePresented += frame =>
        {
            if (started) return;
            started = true;
            _ = window.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var handle = window.PlatformWindow.Handle;
                    var root = window.Root.Component;
                    var configurations = new List<WindowDecorations> { WindowDecorations.Native() };
                    foreach (var frame in Enum.GetValues<WindowFrameOwner>())
                    foreach (var title in Enum.GetValues<WindowTitleBarContent>())
                    foreach (var buttons in Enum.GetValues<WindowCaptionButtons>())
                    {
                        if (title == WindowTitleBarContent.None && buttons != WindowCaptionButtons.None) continue;
                        configurations.Add(frame == WindowFrameOwner.Native ? WindowDecorations.Native(title, buttons) : WindowDecorations.Nazor(title, buttons));
                    }
                    foreach (var configuration in configurations)
                    {
                        var previous = window.AppliedDecorations;
                        var policy = WindowDecorationPolicy.Require(configuration);
                        if (window.Capabilities.Decorations.GetChangeSupport(configuration).Status == WindowSupportStatus.Unsupported)
                        {
                            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => window.SetDecorationsAsync(policy, cancellationToken).AsTask());
                            Assert.Equal(previous, window.AppliedDecorations);
                            continue;
                        }
                        Assert.Equal(configuration, (await window.SetDecorationsAsync(policy, cancellationToken)).Configuration);
                        Assert.Equal(WindowSupportStatus.Supported, window.Capabilities.Decorations.GetSupport(configuration).Status);
                        Assert.Equal(handle, window.PlatformWindow.Handle);
                        Assert.Same(root, window.Root.Component);
                        Assert.Equal(configuration.Frame == WindowFrameOwner.Native ? WindowChromeOwner.System : WindowChromeOwner.Nazor, window.Chrome.Frame);
                        Assert.Empty(window.Exceptions);
                        if (configuration.HasNativeTitleBar)
                        {
                            window.EnterFullScreen();
                            while (window.State != WindowState.FullScreen) await Task.Delay(10, cancellationToken);
                            Assert.Equal(WindowSupportStatus.Supported, window.Capabilities.Decorations.GetSupport(configuration).Status);
                            Assert.Equal(WindowSupportStatus.Unsupported, window.Capabilities.Decorations.FrameChanges);
                            await window.SetDecorationsAsync(policy, cancellationToken);
                            window.ExitFullScreen();
                            while (window.State != WindowState.Normal) await Task.Delay(10, cancellationToken);
                        }
                    }
                    completion.TrySetResult();
                }
                catch (Exception error) { completion.TrySetException(error); }
                finally { window.Close(); }
            });
        };
        using var app = builder.Build();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        await completion.Task.WaitAsync(timeout.Token);
    }

    public sealed class NazorDecorationWindow : DecorationWindow
    {
        protected override void OnInitialized() => Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
    }

    public class DecorationWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenComponent<UserTitleBar>(0);
            builder.AddComponentParameter(1, nameof(UserTitleBar.Title), "User caption");
            builder.CloseComponent();
            builder.OpenComponent<UserCaptionButtons>(2);
            builder.AddComponentParameter(3, nameof(UserCaptionButtons.ChildContent), (RenderFragment)(buttons => buttons.AddContent(0, "User controls")));
            builder.CloseComponent();
            builder.AddContent(4, "Retained content");
        }
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public async Task Transparent_windows_present_raster_frames_with_per_pixel_alpha()
    {
        if (HasX11)
        {
            using var platform = Platform();
            Assert.SkipUnless(platform.Capabilities.CanUseTransparentBackground, "X11 requires a compositing manager and an ARGB visual.");
        }

        var backend = HasX11 ? LinuxDesktopBackend.X11 : LinuxDesktopBackend.Wayland;
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost(backend));
        builder.Application.UseMainWindow<TransparentWindow>();
        var capture = new TaskCompletionSource<RenderedImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Diagnostics.ConfigureWindow = window => window.FramePresented += frame =>
        {
            if (capture.Task.IsCompleted) return;
            try
            {
                Assert.True(window.Capabilities.CanUseTransparentBackground);
                Assert.Equal(RenderingMode.Cpu, frame.Renderer);
                capture.SetResult(window.CaptureFrameAsync().GetAwaiter().GetResult());
            }
            catch (Exception error) { capture.SetException(error); }
            finally { window.Close(); }
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        var image = await capture.Task.WaitAsync(timeout.Token);
        Assert.Equal((byte)0, image.GetPixel(image.Width / 6, image.Height / 2).A);
        Assert.Equal((byte)128, image.GetPixel(image.Width / 2, image.Height / 2).A);
        Assert.Equal(new Color(255, 0, 0, 255), image.GetPixel(image.Width * 5 / 6, image.Height / 2));
    }

    public sealed class TransparentWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.BackgroundMode = BackgroundMode.Transparent;
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
            Window.Width = 240;
            Window.Height = 160;
            Window.Style = "background: linear-gradient(to right, transparent 0%, transparent 33%, #ff000080 33%, #ff000080 66%, red 66%, red 100%)";
        }
    }

    [Theory(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    [InlineData("resize", 1, 1)]
    [InlineData("focus", 9, 1)]
    [InlineData("capabilities", 15, 1)]
    [InlineData("cursor", 5, 1)]
    [InlineData("cancel", 9, 0)]
    [InlineData("composition", 9, 0)]
    public void Closing_from_a_native_notification_stops_using_the_window(string callback, uint type, int value)
    {
        using var platform = Platform();
        var host = new Host();
        using var window = (LinuxDesktopWindow)platform.CreateWindow(host, new("Callback close", 160, 100) { Decorations = WindowDecorations.Nazor() });
        host.Callback = point =>
        {
            Assert.False(window.IsClosed, "A callback ran after the window was destroyed.");
            if (point == callback) window.Dispose();
        };
        window.OnEvent(new() { Type = type, A = value, B = 100, X = 10, Y = 50 });
        Assert.True(window.IsClosed);
        Assert.Equal(1, host.Closed);
    }

    [Fact(SkipUnless = nameof(HasX11), Skip = "Requires an isolated X11 display.")]
    public void Closing_from_buffer_resize_does_not_paint_freed_memory()
    {
        using var platform = Platform();
        var host = new Host();
        using var window = (LinuxDesktopWindow)platform.CreateWindow(host, new("Resize close", 160, 100) { Decorations = WindowDecorations.Nazor() });
        window.Show(false);
        window.OnEvent(new() { Type = 1, A = 160, B = 100 });
        host.Callback = point => { if (point == "resize") window.Dispose(); };
        window.Tick();
        Assert.True(window.IsClosed);
        Assert.Equal(0, host.Paints);
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public void Failing_render_shutdown_still_destroys_the_window()
    {
        using var platform = Platform();
        var host = new Host();
        using var window = platform.CreateWindow(host, new("Shutdown failure", 160, 100));
        host.Stopping = () => throw new InvalidOperationException("render shutdown failed");
        try
        {
            Assert.Equal("render shutdown failed", Assert.Throws<InvalidOperationException>(window.Dispose).Message);
            Assert.True(window.IsClosed);
            Assert.Equal(1, host.Closed);
        }
        finally { host.Stopping = null; }
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public void Reentrant_render_shutdown_destroys_the_window_once()
    {
        using var platform = Platform();
        var host = new Host();
        using var window = platform.CreateWindow(host, new("Reentrant shutdown", 160, 100));
        var reentered = false;
        host.Stopping = () => { if (!reentered) { reentered = true; window.Dispose(); } };
        window.Dispose();
        Assert.True(window.IsClosed);
        Assert.Equal(1, host.Closed);
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public void Platform_shutdown_releases_all_windows_when_one_callback_fails()
    {
        using var platform = Platform();
        var firstHost = new Host { Stopping = () => throw new InvalidOperationException("shutdown failed") };
        var secondHost = new Host();
        var first = platform.CreateWindow(firstHost, new("First", 160, 100));
        var second = platform.CreateWindow(secondHost, new("Second", 160, 100));
        Assert.Equal("shutdown failed", Assert.Throws<InvalidOperationException>(platform.Dispose).Message);
        Assert.True(first.IsClosed);
        Assert.True(second.IsClosed);
        Assert.Equal(1, firstHost.Closed);
        Assert.Equal(1, secondHost.Closed);
    }

    [Theory(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    [InlineData(false)]
    [InlineData(true)]
    public void Background_work_is_completed_with_an_error_when_the_platform_is_disposed(bool disposeFirst)
    {
        using var platform = Platform();
        Task<int>? pending = null;
        var ran = false;
        if (disposeFirst) platform.Dispose();
        var worker = new Thread(() => pending = platform.InvokeAsync(() => { ran = true; return 42; }));
        worker.Start();
        Assert.True(worker.Join(5000));
        platform.Dispose();
        Assert.NotNull(pending);
        Assert.True(pending.IsCompleted);
        Assert.Throws<ObjectDisposedException>(() => pending.GetAwaiter().GetResult());
        Assert.False(ran);
        Assert.Throws<ObjectDisposedException>(() => { _ = platform.InvokeAsync(() => 42); });
    }

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public void Disposal_from_queued_work_stops_the_loop_without_using_the_native_connection()
    {
        using var platform = Platform();
        platform.Post(platform.Dispose);
        using var timeout = new Timer(_ => platform.Quit(99), null, 5000, Timeout.Infinite);
        Assert.Equal(0, platform.Run(new UIDispatcher()));
    }

    private sealed class Host : IWindowHost
    {
        internal Action<string>? Callback;
        internal Action? Stopping;
        internal int Closed, Paints;
        public void OnResized(Size size, float scale) => Callback?.Invoke("resize");
        public IReadOnlyList<PixelRect>? OnPaint(PaintTarget target) { Paints++; return null; }
        public void OnRenderingStopping() => Stopping?.Invoke();
        public void OnStateChanged(WindowState state) => Callback?.Invoke("state");
        public void OnCapabilitiesChanged() => Callback?.Invoke("capabilities");
        public void OnFocusChanged(bool focused) => Callback?.Invoke("focus");
        public PlatformCursor GetCursor(Point point) { Callback?.Invoke("cursor"); return PlatformCursor.Arrow; }
        public void OnPointerMoved(Point point, KeyModifiers modifiers) => Callback?.Invoke("pointer");
        public void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerCanceled() => Callback?.Invoke("cancel");
        public bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat) => false;
        public bool OnKeyUp(string key, KeyModifiers modifiers) => false;
        public void OnTextInput(string text) { }
        public void OnComposition(string text, int cursor) => Callback?.Invoke("composition");
        public void OnCompositionCommit(string text) { }
        public void OnVisibilityChanged(bool visible) { }
        public void OnSystemSettingsChanged() { }
        public bool OnClosing() => true;
        public void OnClosed() => Closed++;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }
}
