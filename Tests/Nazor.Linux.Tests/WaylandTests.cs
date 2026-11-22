using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Linux;
using SkiaSharp;

namespace Nazor.Linux.Tests;

public sealed class WaylandTests
{
    [Theory]
    [InlineData("linux-x64", Architecture.X64, "linux-x64")]
    [InlineData("fedora.44-x64", Architecture.X64, "linux-x64")]
    [InlineData("ubuntu.22.04-arm64", Architecture.Arm64, "linux-arm64")]
    [InlineData("linux-musl-arm64", Architecture.Arm64, "linux-musl-arm64")]
    [InlineData("alpine.3.21-x64", Architecture.X64, "linux-musl-x64")]
    public void Native_assets_use_portable_RIDs_on_distribution_built_runtimes(string runtime, Architecture architecture, string expected) =>
        Assert.Equal(expected, LinuxNativeApi.NativeAssetRid(runtime, architecture));

    [Fact]
    public void Partial_presentation_clips_damage_before_copying_rows_from_native_memory()
    {
        var clipped = LinuxDesktopWindow.ClipDamage([
            new(-5,-3,10,8), new(95,75,int.MaxValue,int.MaxValue),
            new(int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue), new(0,0,-1,3)],100,80);
        Assert.Equal(new[] { new PixelRect(0,0,5,5), new PixelRect(95,75,5,5) },clipped);
        Assert.Null(LinuxDesktopWindow.ClipDamage(Enumerable.Repeat(new PixelRect(0,0,1,1),129).ToArray(),100,80));
    }

    [Fact]
    public void An_interior_damage_rect_does_not_clear_already_composited_rounded_edge_pixels()
    {
        using var bitmap = new SKBitmap(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var rounded = new SKRoundRect(new(0, 0, 32, 32), 10, 10);
        canvas.Clear(SKColors.White);
        LinuxDesktopWindow.ApplyRoundedFrameClip(canvas, rounded, null, 0);
        var first = bitmap.Pixels;
        Assert.Contains(first, pixel => pixel.Alpha is > 0 and < 255);
        LinuxDesktopWindow.ApplyRoundedFrameClip(canvas, rounded, [new(16, 16, 1, 1)], 0);
        Assert.Equal(first, bitmap.Pixels);
    }

    public static bool HasCompositor => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("NAZOR_TEST_COMPOSITOR") == "1";

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public void Runtime_caption_switching_preserves_the_mapped_surface_and_state()
    {
        using var platform = new WaylandPlatform();
        var dispatcher = new UIDispatcher();
        var host = new RecordingHost();
        using var window = (LinuxDesktopWindow)platform.CreateWindow(host, new("Switching", 240, 160) { Decorations = WindowDecorations.Nazor() });
        var handle = window.Handle;
        Assert.Equal("wl_surface", window.NativeHandle?.Descriptor);
        Assert.NotEqual(0, window.NativeHandle?.Handle ?? 0);
        var switched = false;
        host.Painted = async () =>
        {
            if (switched) return;
            switched = true;
            Assert.True((window.Capabilities.Decorations.FrameChanges == WindowSupportStatus.Supported));
            for (var index = 0; index < 3; index++)
            {
                await window.SetDecorationsAsync(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None));
                Assert.Equal(default, window.SystemButtonsMetrics);
                await window.SetDecorationsAsync(WindowDecorations.Nazor());
                Assert.Equal(default, window.SystemButtonsMetrics);
                Assert.Equal(handle, window.Handle);
                Assert.Equal(WindowState.Normal, window.State);
            }
            platform.Quit(42);
        };
        window.Show(false);
        using var timeout = new Timer(_ => platform.Quit(99), null, 5000, Timeout.Infinite);
        Assert.Equal(42, platform.Run(dispatcher));
        Assert.True(switched);
    }

    [Theory]
    [InlineData("Left", "ArrowLeft")]
    [InlineData("KP_Enter", "Enter")]
    [InlineData("ISO_Left_Tab", "Tab")]
    [InlineData("space", " ")]
    [InlineData("BackSpace", "Backspace")]
    [InlineData("Super_R", "Meta")]
    public void Native_key_names_follow_the_existing_input_contract(string native, string expected) => Assert.Equal(expected, LinuxDesktopWindow.KeyName(native));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(float.NaN, 100)]
    [InlineData(float.PositiveInfinity, 100)]
    [InlineData(100, -1)]
    [InlineData(32769, 100)]
    public void Invalid_dimensions_are_rejected_before_allocating_native_resources(float width, float height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LinuxDesktopWindow.ValidateSize(new(width, height)));

    [Fact]
    public void Long_IME_context_is_bounded_without_splitting_surrogates()
    {
        var text=string.Concat(Enumerable.Repeat("😀è",2000));
        var context=LinuxDesktopWindow.SurroundingText(text,3000,3000);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(context.Text)<=4000);
        Assert.Equal(context.Cursor,context.Anchor);
        Assert.False(char.IsLowSurrogate(context.Text[0]));
        Assert.False(char.IsHighSurrogate(context.Text[^1]));
    }

    [Theory]
    [InlineData("KDE",SystemColorProfile.KDE)]
    [InlineData("ubuntu:GNOME",SystemColorProfile.GNOME)]
    [InlineData("sway",SystemColorProfile.Generic)]
    public void Desktop_palette_does_not_assume_GNOME_for_unknown_compositors(string desktop,SystemColorProfile expected) =>
        Assert.Equal(expected,LinuxDesktopWindow.DesktopProfile(desktop));

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public unsafe void Managed_and_native_event_layouts_match()
    {
        using var platform = new WaylandPlatform();
        Assert.Equal(5u, platform.Native.nz_abi_version());
        Assert.Equal((nuint)Marshal.SizeOf<LinuxNativeApi.Event>(), platform.Native.nz_event_size());
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public void A_fixed_client_frame_uses_the_whole_surface_without_disabling_frame_support()
    {
        using var platform = new WaylandPlatform();
        Assert.Equal(WindowSupportStatus.Supported, platform.Capabilities.Decorations.NazorFrame);
        Assert.False(platform.Capabilities.CanSetAlwaysOnTop);
        Assert.False(platform.Capabilities.CanSetTaskbarVisibility);
        using var window = platform.CreateWindow(new RecordingHost(), new("Splash", 200, 100)
        { Decorations = WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None), IsResizable = false });
        Assert.Equal(new Size(200, 100), window.ClientSize);
        Assert.False(window.Capabilities.CanResize);
        Assert.Equal(WindowSupportStatus.Unsupported, window.Capabilities.Decorations.IntegratedNativeCaptionButtons);
        Assert.Equal(default, window.SystemButtonsMetrics);
        Assert.Throws<PlatformNotSupportedException>(() => platform.CreateWindow(new RecordingHost(), new() { IsAlwaysOnTop = true }));
        Assert.Throws<PlatformNotSupportedException>(() => platform.CreateWindow(new RecordingHost(), new() { IsVisibleInTaskbar = false }));
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public void Multiple_windows_render_resize_hide_and_release_their_resources()
    {
        using var platform = new WaylandPlatform();
        var first = new RecordingHost(); var second = new RecordingHost();
        using var window = platform.CreateWindow(first, new("First",160,100));
        using var owned = platform.CreateWindow(second, new("Second",120,80) { Owner=window, IsDialog=true });
        Assert.Equal(PlatformKind.Linux, window.Platform);
        Assert.False(window.PlacementCapabilities.CanRestorePosition);
        Assert.True(window.PlacementCapabilities.CanRestoreSize);
        window.Show(); owned.Show(false);
        var dispatcher = new UIDispatcher();
        dispatcher.Post(() =>
        {
            window.Resize(new(200,140));
            owned.Hide(); owned.Show(false);
        });
        first.Painted = () => { if (second.Paints > 0) platform.Quit(42); };
        second.Painted = () => { if (first.Paints > 0) platform.Quit(42); };
        using var timeout = new Timer(_ => platform.Quit(99), null, 5000, Timeout.Infinite);
        Assert.Equal(42, platform.Run(dispatcher));
        Assert.True(first.Paints > 0); Assert.True(second.Paints > 0);
        Assert.Equal(200, window.ClientSize.Width);
        Assert.Equal(140, window.ClientSize.Height);
        window.Close();
        Assert.True(window.IsClosed); Assert.Equal(1, first.Closed);
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public void Captured_drag_movement_and_release_are_forwarded_outside_the_client_area()
    {
        using var platform = new WaylandPlatform();
        var host = new RecordingHost();
        using var window = (LinuxDesktopWindow)platform.CreateWindow(host, new("Drag",160,100) { Decorations = WindowDecorations.Nazor() });
        window.SetPointerCapture(true);
        window.OnEvent(new() { Type=5, X=-8, Y=-8 });
        window.OnEvent(new() { Type=6, A=272, B=0, X=-8, Y=-8 });
        Assert.Equal(new Point(-8,-8),host.LastMoved);
        Assert.Equal(host.LastMoved,host.LastReleased);
        window.SetPointerCapture(false);
        window.OnEvent(new() { Type=5, X=-5, Y=-5 });
        Assert.Equal(new Point(-8,-8),host.LastMoved);
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public async Task Async_host_owns_a_dedicated_thread_and_preserves_errors_and_exit_codes()
    {
        var caller = Environment.CurrentManagedThreadId;
        var host = new LinuxPlatformHost();
        Assert.Equal(42, await host.RunAsync(platform => { Assert.NotEqual(caller,Environment.CurrentManagedThreadId); return 42; }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunAsync(_ => throw new InvalidOperationException("Application failure.")));
    }

    [Fact(SkipUnless = nameof(HasCompositor), Skip = "Requires the isolated Wayland test compositor.")]
    public void Cross_thread_dispatch_and_quit_wake_the_sleeping_native_loop()
    {
        using var platform = new WaylandPlatform();
        var dispatcher = new UIDispatcher();
        var thread = new Thread(() => dispatcher.Post(() => platform.Quit(42)));
        thread.Start();
        Assert.Equal(42, platform.Run(dispatcher));
        Assert.True(thread.Join(5000));
    }

    private sealed class RecordingHost : IWindowHost
    {
        internal int Paints, Closed;
        internal Point? LastMoved, LastReleased;
        internal Action? Painted;
        public void OnResized(Size size,float scale) { }
        public unsafe IReadOnlyList<PixelRect>? OnPaint(PaintTarget target)
        { new Span<byte>((void*)target.Pixels,target.Stride*target.Height).Fill(255); Paints++; Painted?.Invoke(); return null; }
        public void OnPointerMoved(Point point, KeyModifiers modifiers) => LastMoved=point;
        public void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers) => LastReleased=point;
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point,float notches,KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point,float notches,KeyModifiers modifiers) { }
        public void OnPointerCanceled() { }
        public bool OnKeyDown(string key,KeyModifiers modifiers,bool repeat) => false;
        public bool OnKeyUp(string key,KeyModifiers modifiers) => false;
        public void OnTextInput(string text) { }
        public void OnComposition(string text,int cursor) { }
        public void OnCompositionCommit(string text) { }
        public void OnFocusChanged(bool focused) { }
        public void OnVisibilityChanged(bool visible) { }
        public void OnSystemSettingsChanged() { }
        public bool OnClosing() => true;
        public void OnClosed() => Closed++;
        public PlatformCursor GetCursor(Point point) => PlatformCursor.Arrow;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }
}
