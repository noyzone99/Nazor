using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class DensityPreferencePage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.CloseElement();
    }
}

public sealed class DensityPreferenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Window_then_application_overrides_win_over_live_host_preferences_and_automatic_resumes_inheritance(bool touch)
    {
        var platform = new DensityPlatform(touch);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(DensityPreferencePage));
        var appearance = new ApplicationAppearance();
        window.UpdateAppearance(appearance);
        var native = platform.Window!;
        Assert.Equal(touch ? ControlDensity.Touch : ControlDensity.Compact, window.Scene.Styles.Preferences.Density);
        appearance.ControlDensity = ControlDensity.Touch;
        window.UpdateAppearance(appearance);
        Assert.Equal(ControlDensity.Touch, window.Scene.Styles.Preferences.Density);
        window.ControlDensity = ControlDensity.Compact;
        appearance.ColorScheme = ColorScheme.Dark;
        window.UpdateAppearance(appearance);
        native.PrefersTouchInput = true;
        ((IWindowHost)window).OnSystemSettingsChanged();
        Assert.Equal(ControlDensity.Compact, window.Scene.Styles.Preferences.Density);
        window.ControlDensity = ControlDensity.Automatic;
        Assert.Equal(ControlDensity.Touch, window.Scene.Styles.Preferences.Density);
        appearance.ControlDensity = ControlDensity.Automatic;
        window.UpdateAppearance(appearance);
        native.PrefersTouchInput = false;
        ((IWindowHost)window).OnSystemSettingsChanged();
        Assert.Equal(ControlDensity.Compact, window.Scene.Styles.Preferences.Density);
        native.PrefersTouchInput = true;
        ((IWindowHost)window).OnSystemSettingsChanged();
        Assert.Equal(ControlDensity.Touch, window.Scene.Styles.Preferences.Density);
        Assert.Throws<ArgumentOutOfRangeException>(() => window.ControlDensity = (ControlDensity)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => appearance.ControlDensity = (ControlDensity)99);
    }

    private sealed class DensityPlatform(bool touch) : IPlatform
    {
        public WindowCapabilities Capabilities => new(false, false, false, false) { Decorations = TestWindow.DecorationSupport };
        public DensityWindow? Window { get; private set; }
        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options) => Window = new DensityWindow(touch);
        public int Run(UIDispatcher dispatcher) => 0;
        public void Quit(int exitCode = 0) { }
    }

    private sealed class DensityWindow(bool touch) : IPlatformWindow
    {
        public nint Handle => 0;
        public float Scale => 1;
        public Size ClientSize => new(800, 600);
        public string Title { get; set; } = "Density";
        public bool IsClosed { get; private set; }
        public bool PrefersReducedMotion => false;
        public bool PrefersDarkColorScheme => false;
        public bool PrefersTouchInput { get; set; } = touch;
        public IClipboard Clipboard { get; } = new MemoryClipboard();
        public WindowCapabilities Capabilities => new(false, false, false, false) { Decorations = TestWindow.DecorationSupport };
        public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default) => new(configuration);
        public void Show(bool activate = true) { }
        public void Close() => IsClosed = true;
        public void Invalidate() { }
        public void RequestAnimationFrame(TimeSpan delay) { }
        public void Resize(Size logicalSize) { }
        public void SetTextInputCaret(Rect? caret) { }
        public void Dispose() => IsClosed = true;
    }
}
