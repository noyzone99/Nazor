using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Windows;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NativeRenderingContextTests
{
    [Fact]
    public void Native_surface_preparation_and_release_stay_on_the_owner_while_graphics_initialization_and_cleanup_use_the_worker()
    {
        var owner = Environment.CurrentManagedThreadId;
        var context = new FailingContext();
        Assert.Throws<GraphicsDeviceException>(() => new OpenGlCompositor(() =>
        {
            Assert.Equal(owner, Environment.CurrentManagedThreadId);
            return context;
        }, _ => Assert.Fail("Initialization failure must be synchronous.")));
        Assert.NotEqual(owner, context.InitializedOn);
        Assert.Equal(context.InitializedOn, context.DisposedOn);
        Assert.Equal(owner, context.ReleasedOn);
    }

    [Fact]
    public unsafe void Gpu_paint_validates_exposure_without_changing_the_render_workers_private_context_clip()
    {
        WindowsPlatformHost.RunOnUiThread(() =>
        {
            using var platform = new Win32Platform();
            var host = new GpuHost();
            using var window = platform.CreateWindow(host, new("GPU context isolation", 320, 220));
            window.Show(activate: false);
            Assert.True(SetWindowPos(window.Handle, 0, 30, 30, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
            var dc = GetDC(window.Handle);
            Assert.NotEqual(nint.Zero, dc);
            try
            {
                Assert.NotEqual(0, GetClipBox(dc, out var before));
                host.Context = dc;
                Assert.True(ValidateRect(window.Handle, null));
                var exposure = new RECT { left = 20, top = 90, right = 50, bottom = 110 };
                Assert.True(InvalidateRect(window.Handle, &exposure, false));
                Assert.True(UpdateWindow(window.Handle));
                Assert.True(host.Paints > 0);
                Assert.Equal(nint.Zero, host.Target.Pixels);
                Assert.Equal((before.left, before.top, before.right, before.bottom), host.ClipDuringPaint);
                Assert.False(GetUpdateRect(window.Handle, null, false));
                Assert.NotEqual(0, GetClipBox(dc, out var after));
                Assert.Equal((before.left, before.top, before.right, before.bottom), (after.left, after.top, after.right, after.bottom));
            }
            finally { ReleaseDC(window.Handle, dc); }
            return 0;
        });
    }

    [DllImport("gdi32.dll")]
    private static extern int GetClipBox(nint dc, out RECT bounds);

    private sealed class FailingContext : IOpenGlWindowContext
    {
        public bool IsEmbedded => false;
        public int InitializedOn { get; private set; }
        public int DisposedOn { get; private set; }
        public int ReleasedOn { get; private set; }
        public void Initialize() { InitializedOn = Environment.CurrentManagedThreadId; throw new GraphicsDeviceException("Device unavailable."); }
        public void Dispose() => DisposedOn = Environment.CurrentManagedThreadId;
        public void ReleaseDisplay() => ReleasedOn = Environment.CurrentManagedThreadId;
        public nint GetProcAddress(string name) => throw new NotSupportedException();
        public void MakeCurrent(int width, int height) => throw new NotSupportedException();
        public void Present() => throw new NotSupportedException();
    }

    private sealed class GpuHost : IWindowHost
    {
        public bool UsesGpuRendering => true;
        public int Paints { get; private set; }
        public PaintTarget Target { get; private set; }
        public nint Context { get; set; }
        public (int, int, int, int) ClipDuringPaint { get; private set; }
        public IReadOnlyList<PixelRect>? OnPaint(PaintTarget target)
        {
            Paints++;
            Target = target;
            GetClipBox(Context, out var clip);
            ClipDuringPaint = (clip.left, clip.top, clip.right, clip.bottom);
            return [];
        }
        public void OnResized(Size size, float scale) { }
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
        public bool OnClosing() => true;
        public void OnClosed() { }
        public PlatformCursor GetCursor(Point point) => PlatformCursor.Arrow;
        public Nazor.Accessibility.SemanticTree? GetSemanticTree() => null;
    }
}
