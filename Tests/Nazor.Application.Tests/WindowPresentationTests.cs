using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class WindowPresentationTests
{
    [Fact]
    public void A_fixed_nazor_canvas_has_explicit_decorations_and_independent_resize_policy()
    {
        var platform = new PresentationPlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(CanvasWindow));
        Assert.Equal(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None), platform.Options!.Decorations);
        Assert.False(window.IsResizable);
        Assert.False(window.Capabilities.CanResize);
        Assert.Empty(window.Root.Elements("TitleBar"));
        window.IsResizable = true;
        Assert.True(window.Capabilities.CanResize);
        window.Width = 320;
        Assert.Equal(320, platform.Window!.ClientSize.Width);
        Assert.Empty(window.Exceptions);
    }

    [Fact]
    public void Presentation_changes_notify_Changed_but_only_effective_operation_changes_notify_CapabilitiesChanged()
    {
        var platform = new PresentationPlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(EmptyWindow));
        var changed = 0;
        var capabilitiesChanged = 0;
        window.Changed += () => changed++;
        window.CapabilitiesChanged += () => capabilitiesChanged++;
        window.IsAlwaysOnTop = true;
        window.IsVisibleInTaskbar = false;
        Assert.True(platform.Window!.AlwaysOnTop);
        Assert.False(platform.Window.VisibleInTaskbar);
        Assert.Equal(2, changed);
        Assert.Equal(0, capabilitiesChanged);
        window.IsMinimizable = false;
        Assert.False(window.Capabilities.CanMinimize);
        Assert.Equal(1, capabilitiesChanged);
        window.IsMinimizable = false;
        Assert.Equal(1, capabilitiesChanged);
        window.IsAlwaysOnTop = false;
        window.IsVisibleInTaskbar = true;
        Assert.False(platform.Window.AlwaysOnTop);
        Assert.True(platform.Window.VisibleInTaskbar);
    }

    [Fact]
    public void Unsupported_requests_fail_without_changing_configuration()
    {
        using var window = new NazorWindow(new PresentationPlatform(supported: false), new UIDispatcher(), typeof(EmptyWindow));
        Assert.Throws<PlatformNotSupportedException>(() => window.IsAlwaysOnTop = true);
        Assert.Throws<PlatformNotSupportedException>(() => window.IsVisibleInTaskbar = false);
        Assert.False(window.Capabilities.CanUseTransparentBackground);
        Assert.Equal(BackgroundMode.Default, window.BackgroundMode);
        Assert.False(window.IsAlwaysOnTop);
        Assert.True(window.IsVisibleInTaskbar);
        Assert.Throws<PlatformNotSupportedException>(() => new NazorWindow(new PresentationPlatform(false), new UIDispatcher(), typeof(EmptyWindow), new() { Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor()) }));
        Assert.Throws<PlatformNotSupportedException>(() => new NazorWindow(new PresentationPlatform(false), new UIDispatcher(), typeof(EmptyWindow),
            frame: new WindowOptions { BackgroundMode = BackgroundMode.Transparent }));
    }

    [Fact]
    public void Opening_options_reach_the_native_window_and_borderless_cannot_be_enabled_later()
    {
        var platform = new PresentationPlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(EmptyWindow), new()
        { IsAlwaysOnTop = true, IsVisibleInTaskbar = false }, frame: new WindowOptions());
        Assert.True(platform.Options!.IsAlwaysOnTop);
        Assert.False(platform.Options.IsVisibleInTaskbar);
        Assert.Throws<InvalidOperationException>(() => window.Decorations = WindowDecorationPolicy.Default);
    }

    [Fact]
    public void Command_support_is_independent_of_policy_and_restore_remains_available()
    {
        var platform = new PresentationPlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(EmptyWindow));

        window.Operations = window.Operations with { IsMaximizable = false };
        Assert.True(window.Capabilities.Maximize.IsSupported);
        Assert.Equal(NativeRestriction.Hint, window.Capabilities.Maximize.Restriction);
        Assert.False(window.Capabilities.CanMaximize);
        Assert.False(window.CanExecute(WindowCommand.Maximize));
        Assert.Equal(WindowCommandResult.DisabledByPolicy, window.Request(WindowCommand.Maximize));
        Assert.Equal(WindowState.Normal, window.State);

        window.Operations = window.Operations with { IsMaximizable = true };
        Assert.Equal(WindowCommandResult.Dispatched, window.Request(WindowCommand.Maximize));
        Assert.Equal(WindowState.Maximized, window.State);
        Assert.Equal(WindowCommandResult.AlreadyInState, window.Request(WindowCommand.Maximize));

        window.Operations = window.Operations with { IsMaximizable = false };
        Assert.Equal(WindowCommandResult.Dispatched, window.Request(WindowCommand.Restore));
        Assert.Equal(WindowState.Normal, window.State);

        Assert.Equal(WindowCommandResult.Dispatched, window.Request(WindowCommand.EnterFullScreen));
        window.Operations = window.Operations with { FullScreenEnabled = false };
        Assert.True(window.CanExecute(WindowCommand.ExitFullScreen));
        Assert.Equal(WindowCommandResult.Dispatched, window.Request(WindowCommand.ExitFullScreen));
        Assert.Equal(WindowState.Normal, window.State);
        Assert.Empty(window.Exceptions);
    }

    [Fact]
    public void The_backend_can_open_the_Nazor_caption_menu_and_mode_changes_dismiss_it()
    {
        var platform = new PresentationPlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(EmptyWindow));
        window.Operations = window.Operations with { IsMaximizable = false };
        var changes = 0;
        window.ChromeChanged += () => changes++;

        window.TitleBarMenuMode = WindowTitleBarMenuMode.Nazor;
        Assert.Equal(WindowChromeOwner.Nazor, window.Chrome.TitleBarMenu);
        platform.Window!.OpenTitleBarMenu();
        var menu = window.Renderer.RootComponents.Last();
        Assert.Equal(5, menu.Elements("MenuItem").Count());
        Assert.Equal(true, menu.Elements("MenuItem").ElementAt(2).Attributes["disabled"]);
        window.Operations = window.Operations with { IsMaximizable = true };
        Assert.Equal(false, menu.Elements("MenuItem").ElementAt(2).Attributes["disabled"]);
        Assert.Empty(window.Exceptions);

        window.TitleBarMenuMode = WindowTitleBarMenuMode.None;
        Assert.Equal(WindowChromeOwner.None, window.Chrome.TitleBarMenu);
        Assert.Equal(2, changes);
        Assert.Empty(menu.Elements("MenuItem"));
    }

    public sealed class EmptyWindow : WindowComponentBase;

    public sealed class CanvasWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None));
            Window.IsResizable = false;
        }
        protected override void BuildWindowContent(RenderTreeBuilder builder) { }
    }

    private sealed class PresentationPlatform(bool supported = true) : IPlatform
    {
        public WindowCapabilities Capabilities => new(true, true, true, true)
        {
            Decorations = new() { NativeFrame = WindowSupportStatus.Supported, NativeTitleBar = WindowSupportStatus.Supported,
                NazorFrame = supported ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                FrameChanges = WindowSupportStatus.Supported, CaptionChanges = WindowSupportStatus.Supported },
            CanSetAlwaysOnTop = supported,
            CanSetTaskbarVisibility = supported, Maximize = new(true, NativeRestriction.Hint),
        };
        public WindowOptions? Options { get; private set; }
        public PresentationWindow? Window { get; private set; }
        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
        {
            Options = options;
            return Window = new PresentationWindow(host, options, Capabilities);
        }
        public int Run(UIDispatcher dispatcher) => 0;
        public void Quit(int exitCode = 0) { }
    }

    private sealed class PresentationWindow(IWindowHost host, WindowOptions options, WindowCapabilities support) : IPlatformWindow
    {
        private WindowOperations operations = new(options.IsMinimizable ?? true, options.IsMaximizable ?? true,
            options.IsResizable ?? true, options.FullScreenEnabled ?? true);
        private WindowTitleBarMenuMode menuMode;
        public WindowState State { get; private set; }
        public WindowChrome Chrome => new(WindowChromeOwner.Nazor, WindowChromeOwner.Nazor, menuMode switch
        {
            WindowTitleBarMenuMode.Nazor => WindowChromeOwner.Nazor,
            WindowTitleBarMenuMode.System => WindowChromeOwner.System,
            _ => WindowChromeOwner.None,
        });

        public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default) => new(configuration);
        public void SetTitleBarMenuMode(WindowTitleBarMenuMode value) => menuMode = value;
        public void OpenTitleBarMenu() => host.OnTitleBarMenuRequested(new(30, 30));
        public void SetState(WindowState state)
        {
            State = state;
            host.OnStateChanged(state);
        }
        public void ExitFullScreen() => SetState(WindowState.Normal);
        public WindowCapabilities Capabilities => support with
        {
            CanMinimize = operations.IsMinimizable, CanMaximize = operations.IsMaximizable,
            CanResize = operations.IsResizable, CanEnterFullScreen = operations.FullScreenEnabled,
        };
        public nint Handle => 0;
        public float Scale => 1;
        public Size ClientSize { get; private set; } = new(800, 600);
        public string Title { get; set; } = options.Title;
        public bool IsClosed { get; private set; }
        public bool PrefersReducedMotion => false;
        public bool PrefersDarkColorScheme => false;
        public IClipboard Clipboard { get; } = new MemoryClipboard();
        public bool AlwaysOnTop { get; private set; } = options.IsAlwaysOnTop;
        public bool VisibleInTaskbar { get; private set; } = options.IsVisibleInTaskbar;
        public void SetAlwaysOnTop(bool value) => AlwaysOnTop = value;
        public void SetTaskbarVisibility(bool value) => VisibleInTaskbar = value;
        public void SetOperations(WindowOperations value) { operations = value; host.OnCapabilitiesChanged(); }
        public void Show(bool activate = true) { }
        public void Close() => IsClosed = true;
        public void Invalidate() { }
        public void RequestAnimationFrame(TimeSpan delay) { }
        public void Resize(Size size) => ClientSize = size;
        public void SetTextInputCaret(Rect? caret) { }
        public void Dispose() => IsClosed = true;
    }
}
