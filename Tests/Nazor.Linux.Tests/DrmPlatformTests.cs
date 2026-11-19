using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class DrmPlatformTests
{
    [Fact]
    public async Task Host_validates_without_opening_devices_and_rejects_missing_callbacks()
    {
        var host = new DrmPlatformHost();
        Assert.Throws<ArgumentNullException>(() => host.Run(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => host.RunAsync(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrmPlatformHost(new() { Scale = 0 }));
        if (!OperatingSystem.IsLinux()) Assert.Throws<PlatformNotSupportedException>(() => host.Run(_ => 0));
    }

    [Fact]
    public void One_display_view_has_no_desktop_window_capabilities()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new() { Scale = 1.5f }, backend);
        var host = new Host();
        using var view = platform.CreateView(host);
        Assert.Equal(new Size(200, 120), view.ClientSize);
        Assert.Equal(1.5f, view.Scale);
        Assert.Equal(WindowState.FullScreen, view.State);
        Assert.Equal(default, platform.Capabilities);
        Assert.Equal(default, view.Capabilities);
        Assert.Null(((IPlatform)platform).DesktopIntegration);
        Assert.Throws<PlatformNotSupportedException>(() => platform.CreateWindow(host, new()));
        Assert.Throws<InvalidOperationException>(() => platform.CreateView(host));
        Assert.Throws<PlatformNotSupportedException>(() => view.Resize(new(10, 10)));
        view.Resize(view.ClientSize);
        view.Show();
        Assert.Equal(view.ClientSize, host.Size);
        Assert.True(host.Visible);
        Assert.True(view.Clipboard.SetText("local clipboard"));
        Assert.Equal("local clipboard", view.Clipboard.GetText());
    }

    [Fact]
    public void Software_paints_retain_the_same_target_and_present_complete_frames_only_when_dirty()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new(), backend);
        var host = new Host();
        using var view = (DrmView)platform.CreateView(host);
        view.Show(); view.Tick();
        var first = host.Target;
        view.Tick();
        Assert.Equal(1, host.Paints);
        Assert.Equal(1, backend.Presented);
        view.Invalidate(); view.Invalidate(); view.Tick();
        Assert.Equal(first, host.Target);
        Assert.Equal(2, backend.Presented);
        Assert.Equal(2, backend.LastPixels[0]);
        Assert.Equal(255, backend.LastPixels[^1]);
        Assert.Equal(2, host.Completions);
    }

    [Fact]
    public void Gpu_paints_use_surface_descriptions_and_the_existing_context_contract()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new(), backend);
        var host = new Host { UsesGpuRendering = true };
        using var view = (DrmView)platform.CreateView(host);
        Assert.Throws<GraphicsDeviceException>(() => view.CreateOpenGlContext());
        view.Show(); view.Tick();
        Assert.Equal(0, host.Target.Pixels);
        Assert.Equal(0, host.Target.Stride);
        Assert.Equal(300, host.Target.Width);
        Assert.Equal(0, backend.Presented);
    }

    [Fact]
    public void Seat_deactivation_suspends_frames_and_focus_and_resume_invalidates_the_view()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new(), backend);
        var host = new Host();
        using var view = (DrmView)platform.CreateView(host);
        view.Show(); view.Tick();
        backend.SetActive(false);
        Assert.False(host.Visible); Assert.False(host.Focused);
        Assert.Equal(1, host.Cancels);
        view.RequestAnimationFrame(TimeSpan.Zero); view.Tick();
        Assert.Equal(1, host.Paints);
        backend.Emit(new(DrmEventKind.KeyDown, Key: "a", Text: "a"));
        Assert.Empty(host.Text);
        backend.SetActive(true); view.Tick();
        Assert.True(host.Visible); Assert.True(host.Focused);
        Assert.Equal(2, host.Paints);
        view.Hide(); backend.SetActive(false); backend.SetActive(true);
        Assert.False(host.Visible); Assert.False(host.Focused);
    }

    [Fact]
    public void Input_uses_logical_coordinates_and_respects_key_handling_and_disabled_state()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new() { Scale = 2 }, backend);
        var host = new Host();
        using var view = platform.CreateView(host);
        view.Show();
        backend.Emit(new(DrmEventKind.Move, new(80, 60)));
        Assert.Equal(new Point(40, 30), host.Pointer);
        backend.Emit(new(DrmEventKind.Scroll, new(80, 60), DeltaX: 3, DeltaY: 8));
        Assert.Equal((3f, -8f, ScrollDeltaUnit.LogicalPixels), host.Scroll);
        backend.Emit(new(DrmEventKind.KeyDown, Key: "a", Text: "a"));
        host.HandleKeys = true;
        backend.Emit(new(DrmEventKind.KeyDown, Key: "b", Text: "b", Repeat: true));
        Assert.Equal("a", host.Text);
        Assert.True(host.Repeat);
        view.SetEnabled(false);
        backend.Emit(new(DrmEventKind.Move, new(100, 100)));
        Assert.Equal(new Point(40, 30), host.Pointer);
    }

    [Fact]
    public void Closing_during_paint_does_not_present_freed_pixels_and_stops_rendering_once()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new(), backend);
        var host = new Host();
        var view = (DrmView)platform.CreateView(host);
        host.Painting = view.Close;
        view.Show(); view.Tick();
        view.Dispose();
        Assert.True(view.IsClosed);
        Assert.Equal(0, backend.Presented);
        Assert.Equal(1, host.Stops);
        Assert.Equal(1, host.Closed);
        Assert.Throws<InvalidOperationException>(() => platform.CreateView(host));
    }

    [Fact]
    public void Rendering_is_drained_before_backend_resources_are_disposed_even_when_stopping_throws()
    {
        var backend = new Backend();
        var platform = new DrmPlatform(new(), backend);
        var host = new Host { Stopping = () => { Assert.False(backend.Disposed); throw new InvalidOperationException("Stop failure"); } };
        platform.CreateView(host);
        Assert.Throws<InvalidOperationException>(platform.Dispose);
        Assert.True(backend.Disposed);
        Assert.Equal(1, host.Closed);
        platform.Dispose();
    }

    [Fact]
    public void Cross_thread_work_and_quit_wake_the_loop_and_restore_its_synchronization_context()
    {
        var backend = new Backend();
        using var platform = new DrmPlatform(new(), backend);
        var dispatcher = new UIDispatcher();
        var previous = SynchronizationContext.Current;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                Assert.True(backend.Waiting.Wait(5000));
                Assert.Throws<InvalidOperationException>(() => platform.CreateView(new Host()));
                dispatcher.Post(() => platform.Quit(42));
            }
            catch (Exception failure) { error = failure; platform.Quit(99); }
        });
        thread.Start();
        Assert.Equal(42, platform.Run(dispatcher));
        Assert.True(thread.Join(5000));
        Assert.Null(error);
        Assert.Same(previous, SynchronizationContext.Current);
    }

    [Fact]
    public void Multiple_keys_buttons_and_devices_preserve_chords_until_the_last_release()
    {
        var pressed = new Dictionary<uint, int>();
        Assert.True(DrmInput.UpdatePressed(pressed, 29, true));
        Assert.True(DrmInput.UpdatePressed(pressed, 30, true));
        Assert.False(DrmInput.UpdatePressed(pressed, 29, true));
        Assert.False(DrmInput.UpdatePressed(pressed, 29, false));
        Assert.True(DrmInput.UpdatePressed(pressed, 30, false));
        Assert.True(DrmInput.UpdatePressed(pressed, 29, false));
        Assert.False(DrmInput.UpdatePressed(pressed, 29, false));
        Assert.Empty(pressed);
    }

    internal sealed class Backend : IDrmBackend
    {
        private readonly AutoResetEvent wake = new(false);
        internal readonly ManualResetEventSlim Waiting = new();
        public int Width => 300;
        public int Height => 180;
        public bool Active { get; private set; } = true;
        public Action<bool>? ActiveChanged { get; set; }
        public Action<DrmEvent>? Input { get; set; }
        internal int Presented;
        internal byte[] LastPixels = [];
        internal bool Disposed;
        internal void Emit(DrmEvent value) => Input?.Invoke(value);
        internal void SetActive(bool active) { Active = active; ActiveChanged?.Invoke(active); }
        public IOpenGlWindowContext CreateOpenGlContext() => throw new GraphicsDeviceException("No test GPU");
        public void Present(PaintTarget target)
        {
            LastPixels = new byte[target.Stride * target.Height];
            Marshal.Copy(target.Pixels, LastPixels, 0, LastPixels.Length);
            Presented++;
        }
        public void Dispatch(int timeout) { if (timeout != 0) { Waiting.Set(); wake.WaitOne(5000); } }
        public void Wake() { if (!Disposed) wake.Set(); }
        public void Dispose() { Disposed = true; wake.Dispose(); Waiting.Dispose(); }
    }

    internal sealed class Host : IWindowHost
    {
        internal int Paints, Stops, Closed, Completions, Cancels;
        internal bool Visible, Focused, HandleKeys, Repeat;
        internal Size Size;
        internal PaintTarget Target;
        internal Point Pointer;
        internal (float, float, ScrollDeltaUnit) Scroll;
        internal string Text = "";
        internal Action? Painting, Stopping;
        public bool UsesGpuRendering { get; init; }
        public void OnResized(Size size, float scale) => Size = size;
        public unsafe IReadOnlyList<PixelRect>? OnPaint(PaintTarget target)
        {
            Target = target;
            if (target.Pixels != 0)
            {
                var bytes = new Span<byte>((void*)target.Pixels, target.Stride * target.Height);
                if (Paints == 0) bytes.Fill(255);
                bytes[0] = (byte)(Paints + 1);
            }
            Paints++; Painting?.Invoke(); return null;
        }
        public void OnRenderingStopping() { Stops++; Stopping?.Invoke(); }
        public void OnPresented(TimeSpan duration) => Completions++;
        public void OnPointerMoved(Point point, KeyModifiers modifiers) => Pointer = point;
        public void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers) { }
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers) { }
        public void OnPointerScroll(Point point, float dx, float dy, ScrollDeltaUnit unit, KeyModifiers modifiers) => Scroll = (dx, dy, unit);
        public void OnPointerCanceled() => Cancels++;
        public bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat) { Repeat = repeat; return HandleKeys; }
        public bool OnKeyUp(string key, KeyModifiers modifiers) => false;
        public void OnTextInput(string text) => Text += text;
        public void OnComposition(string text, int cursor) { }
        public void OnCompositionCommit(string text) { }
        public void OnFocusChanged(bool focused) => Focused = focused;
        public void OnVisibilityChanged(bool visible) => Visible = visible;
        public void OnSystemSettingsChanged() { }
        public bool OnClosing() => true;
        public void OnClosed() => Closed++;
        public PlatformCursor GetCursor(Point point) => PlatformCursor.Arrow;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }
}
