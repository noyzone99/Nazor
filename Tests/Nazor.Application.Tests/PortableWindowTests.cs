using Nazor.Controls;
using Nazor.DevTools;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Graphics.Color;

namespace Nazor.Application.Tests;
public sealed class PortableWindowPage : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
    }

    protected override void OnInitialized()
    {
        Window.Backdrop = (BackdropSelection)CommonBackdrop.Blur;
        Window.Style = "min-height: var(--nz-window-controls-height)";
    }
}

public sealed class PortableWindowTests
{
    [Fact]
    public void Native_reference_is_optional_dispatcher_bound_and_expires_on_close()
    {
        var platform = new PortablePlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage));
        IWindow contract = window;
        Assert.Null(contract.NativeHandle);
        platform.Window!.NativeHandle = new(42, "test-window");
        Assert.Equal(new NativeWindowHandle(42, "test-window"), contract.NativeHandle);
        Exception? error = null;
        var worker = new Thread(() => error = Record.Exception(() => _ = contract.NativeHandle));
        worker.Start();
        worker.Join();
        Assert.IsType<InvalidOperationException>(error);
        window.Dispose();
        Assert.Null(contract.NativeHandle);
    }
    [Fact]
    public void External_popup_opt_in_preserves_an_unsupported_hosts_capability()
    {
        var platform = new PortablePlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage));
        Assert.False(window.AllowPopupsOutsideWindow);
        Assert.False(window.Capabilities.CanShowPopupsOutsideWindow);
        window.AllowPopupsOutsideWindow = true;
        Assert.True(window.AllowPopupsOutsideWindow);
        Assert.False(window.Capabilities.CanShowPopupsOutsideWindow);
    }

    [Fact]
    public void Ordinary_runtime_assembly_graph_excludes_diagnostics_tools_and_IDE_SDKs()
    {
        var pending = new Stack<System.Reflection.Assembly>();
        var visited = new HashSet<string>();
        pending.Push(typeof(NazorWindow).Assembly);
        while (pending.TryPop(out var assembly))
        {
            if (!visited.Add(assembly.FullName!)) continue;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.False(reference.Name == "Nazor.Diagnostics" || reference.Name!.StartsWith("Nazor.DevTools", StringComparison.Ordinal)
                    || reference.Name.StartsWith("Microsoft.VisualStudio", StringComparison.Ordinal) || reference.Name.StartsWith("EnvDTE", StringComparison.Ordinal),
                    $"{assembly.GetName().Name} references {reference.Name}.");
                if (reference.Name.StartsWith("Nazor.", StringComparison.Ordinal)) pending.Push(System.Reflection.Assembly.Load(reference));
            }
        }
        Assert.Contains("Nazor.Application", visited.Select(name => new System.Reflection.AssemblyName(name).Name));
    }

    [Fact]
    public void Optional_diagnostics_attach_once_and_release_the_session_on_detach_and_window_disposal()
    {
        var platform = new PortablePlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage));
        Assert.Null(window.Instrumentation);
        Assert.Null(window.PlatformWindow.AddSystemMenuCommand("Unsupported action", () => throw new InvalidOperationException("Unsupported menus cannot invoke actions.")));
        Assert.Null(window.GetDiagnostics());
        var session = window.EnableDiagnostics();
        Assert.Same(session, window.GetDiagnostics());
        Assert.Same(session, window.EnableDiagnostics());
        session.SetInspectMode(true);
        window.DisableDiagnostics();
        window.DisableDiagnostics();
        Assert.Null(window.Instrumentation);
        Assert.Null(window.GetDiagnostics());
        Assert.False(session.IsInspecting);
        Assert.Throws<ObjectDisposedException>(() => session.Highlight(null));
        var next = window.EnableDiagnostics();
        Assert.NotSame(session, next);
        next.SetInspectMode(true);
        window.Dispose();
        Assert.Null(window.Instrumentation);
        Assert.False(next.IsInspecting);
        Assert.Throws<ObjectDisposedException>(() => next.Highlight(null));
        Assert.Throws<ObjectDisposedException>(() => window.EnableDiagnostics());
    }

    [Fact]
    public void Failed_window_instrumentation_configuration_disposes_the_window_and_attached_agent()
    {
        var directory = OperatingSystem.IsWindows() ? Path.Combine(Path.GetTempPath(), "nazor-config-" + Guid.NewGuid().ToString("N"))
            : Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
        var platform = new PortablePlatform();
        NazorWindow? configured = null;
        DevToolsAgent? agent = null;
        var expected = new InvalidOperationException("Instrumentation startup failed.");
        var actual = Assert.Throws<InvalidOperationException>(() => new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage),
            new ApplicationRuntimeSettings { ConfigureWindow = window =>
            {
                configured = window;
                agent = window.EnableDevTools(directory);
                throw expected;
            } }));
        Assert.Same(expected, actual);
        Assert.NotNull(configured);
        Assert.True(configured.IsClosed);
        Assert.True(platform.Window!.IsClosed);
        Assert.DoesNotContain(configured, NazorWindow.OpenWindows);
        Assert.NotNull(agent);
        Assert.True(agent.IsDisposed);
        Assert.False(File.Exists(agent.DiscoveryFile));
    }

    [Fact]
    public void Explicit_agents_have_separate_window_sessions_and_close_with_their_window()
    {
        var directory = OperatingSystem.IsWindows() ? Path.Combine(Path.GetTempPath(), "nazor-agent-" + Guid.NewGuid().ToString("N"))
            : Path.Combine(Path.GetTempPath(), "ndt-" + Guid.NewGuid().ToString("N")[..12]);
        using var first = new NazorWindow(new PortablePlatform(), new UIDispatcher(), typeof(PortableWindowPage));
        using var second = new NazorWindow(new PortablePlatform(), new UIDispatcher(), typeof(PortableWindowPage));
        Assert.Null(first.GetDevTools());
        Assert.False(Directory.Exists(directory));
        var a = first.EnableDevTools(directory);
        var b = second.EnableDevTools(directory);
        Assert.Same(a, first.EnableDevTools(directory));
        Assert.NotEqual(a.Descriptor.Identity.WindowId, b.Descriptor.Identity.WindowId);
        Assert.NotEqual(a.Descriptor.Identity.SessionId, b.Descriptor.Identity.SessionId);
        Assert.Equal(a.Descriptor.Identity.ApplicationId, b.Descriptor.Identity.ApplicationId);
        Assert.Equal(2, Nazor.DevTools.Protocol.LocalDevTools.Discover(directory).Count);
        first.GetDiagnostics()!.SetInspectMode(true);
        first.Dispose();
        Assert.True(a.IsDisposed);
        Assert.False(File.Exists(a.DiscoveryFile));
        Assert.False(b.IsDisposed);
        Assert.Single(Nazor.DevTools.Protocol.LocalDevTools.Discover(directory));
        second.DisableDevTools();
        Assert.Empty(Nazor.DevTools.Protocol.LocalDevTools.Discover(directory));
        var next = second.EnableDevTools(directory);
        Assert.NotEqual(b.Descriptor.Identity.SessionId, next.Descriptor.Identity.SessionId);
        second.Dispose();
        Assert.True(next.IsDisposed);
    }

    [Fact]
    public void Optional_attachment_preserves_frame_cause_bits()
    {
        foreach (var cause in Enum.GetValues<FrameCause>())
        {
            var runtimeName = cause == FrameCause.DevTools ? nameof(WindowFrameCause.Overlay) : cause.ToString();
            Assert.Equal((int)cause, (int)Enum.Parse<WindowFrameCause>(runtimeName));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Effective_icon_resolves_the_override_and_returns_to_the_application_source(bool nativeIcons)
    {
        var platform = new PortablePlatform(icons: nativeIcons);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage));
        var source = IconPropertyWindow.Blue;
        platform.Window!.ApplicationIcon = source;
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor()), TestContext.Current.CancellationToken);
        Assert.Null(window.Icon);
        Assert.Same(source, window.EffectiveIcon);
        Assert.Same(source, window.Root.Elements("Image").Single().Attributes["source"]);
        window.Icon = IconPropertyWindow.Red;
        Assert.Same(IconPropertyWindow.Red, window.EffectiveIcon);
        Assert.Same(IconPropertyWindow.Red, window.Root.Elements("Image").Single().Attributes["source"]);
        window.Icon = null;
        Assert.Same(source, window.EffectiveIcon);
        Assert.Same(source, window.Root.Elements("Image").Single().Attributes["source"]);
    }
    [Fact]
    public void Icon_contract_supplies_pixels_refreshes_for_dpi_and_discards_superseded_work()
    {
        var platform = new PortablePlatform(icons: true);
        var dispatcher = new UIDispatcher();
        using var window = new NazorWindow(platform, dispatcher, typeof(IconPropertyWindow));
        window.Icon = IconPropertyWindow.Blue;
        dispatcher.RunPending();
        Assert.All(platform.Window!.Icons!, i => Assert.Equal(new Color(0, 0, 255, 255), i.GetPixel(0, 0)));
        Assert.Contains(platform.Window.Icons!, i => i.Width == 16);
        platform.Window.Scale = 2;
        ((IWindowHost)window).OnResized(new Size(200, 100), 2);
        dispatcher.RunPending();
        Assert.Contains(platform.Window.Icons!, i => i.Width == 96);
        Assert.DoesNotContain(platform.Window.Icons!, i => i.Width == 16);
        window.Icon = IconPropertyWindow.Red;
        window.Icon = null;
        dispatcher.RunPending();
        Assert.Null(platform.Window.Icons);
        window.Icon = IconPropertyWindow.Red;
        var applied = platform.Window.IconChanges;
        window.Dispose();
        dispatcher.RunPending();
        Assert.Equal(applied, platform.Window.IconChanges);
        Assert.Empty(window.Exceptions);
    }

    [Fact]
    public void Pending_icon_streams_are_canceled_on_replacement_and_close()
    {
        var platform = new PortablePlatform(icons: true);
        var dispatcher = new UIDispatcher();
        using var window = new NazorWindow(platform, dispatcher, typeof(IconPropertyWindow));
        var pending = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken loading = default;
        window.Icon = ImageSource.FromStream(token =>
        {
            loading = token;
            return pending.Task;
        }, "pending-icon");
        window.Icon = IconPropertyWindow.Blue;
        Assert.True(loading.IsCancellationRequested);
        pending.SetCanceled(TestContext.Current.CancellationToken);
        dispatcher.RunPending();
        Assert.All(platform.Window!.Icons!, i => Assert.Equal(new Color(0, 0, 255, 255), i.GetPixel(0, 0)));
        var closing = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Icon = ImageSource.FromStream(token =>
        {
            loading = token;
            return closing.Task;
        }, "closing-icon");
        window.Dispose();
        Assert.True(loading.IsCancellationRequested);
        closing.SetCanceled(TestContext.Current.CancellationToken);
        dispatcher.RunPending();
        Assert.Empty(window.Exceptions);
    }

    [Fact]
    public void Unsupported_backend_retains_the_requested_icon_without_loading_it()
    {
        var loads = 0;
        var source = ImageSource.FromStream(_ =>
        {
            loads++;
            throw new InvalidOperationException("An unsupported backend must not load the icon.");
        }, "unsupported-window-icon");
        var platform = new PortablePlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(IconPropertyWindow));
        Assert.False(window.Capabilities.CanSetWindowIcon);
        Assert.Same(IconPropertyWindow.Red, window.Icon);
        ((IWindow)window).Icon = source;
        Assert.Same(source, window.Icon);
        Assert.Equal(0, loads);
        Assert.Empty(window.Exceptions);
    }

    [Fact]
    public void Unsupported_backend_stays_opaque_reports_effective_operations_and_omits_unavailable_metrics()
    {
        var platform = new PortablePlatform();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PortableWindowPage));
        Assert.False(window.AppliedBackdrop.IsActive);
        Assert.Equal(255, window.Scene.Background.A);
        Assert.False(window.Capabilities.CanMinimize);
        Assert.False(window.Capabilities.CanMaximize);
        Assert.False(window.Capabilities.CanResize);
        Assert.False(window.Capabilities.CanEnterFullScreen);
        Assert.True(window.IsMaximizable);
        Assert.True(platform.Options!.IsMaximizable);
        Assert.Null(window.Scene.Styles.SystemButtonsHeight);
        Assert.Null(window.Scene.Styles.Preferences.Platform);
        var root = window.Root.Elements("Window").Single();
        Assert.False(root.Attributes.ContainsKey("backdrop"));
        Assert.Equal(SizeValue.Auto, window.Scene.GetStyle(root).Get(StyleProperties.MinHeight));
        var changed = 0;
        window.EnvironmentChanged += () => changed++;
        platform.Window!.Environment = new(0, 20, 0, 10, new WindowOcclusion(30, 50, 100, 40));
        ((IWindowHost)window).OnEnvironmentChanged();
        Assert.Equal(1, changed);
        Assert.Equal(20, ((IWindow)window).Environment.SafeTop);
        Assert.Equal(new WindowOcclusion(30, 50, 100, 40), window.Environment.KeyboardOcclusion);
        Assert.DoesNotContain(typeof(NazorApplication).Assembly.GetReferencedAssemblies(), a => a.Name == "Nazor.Platform.Windows");
    }

    private sealed class PortablePlatform(bool icons = false) : IPlatform
    {
        public WindowCapabilities Capabilities => new(false, false, false, false) { Decorations = TestWindow.DecorationSupport };
        public WindowOptions? Options { get; private set; }
        public PortablePlatformWindow? Window { get; private set; }

        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
        {
            Options = options;
            return Window = new PortablePlatformWindow(icons);
        }

        public int Run(UIDispatcher dispatcher) => 0;
        public void Quit(int exitCode = 0)
        {
        }
    }

    private sealed class PortablePlatformWindow(bool icons) : IPlatformWindow
    {
        public NativeWindowHandle? NativeHandle { get; set; }
        public nint Handle => 0;
        public float Scale { get; set; } = 1;
        public WindowCapabilities Capabilities => default(WindowCapabilities)with
        {
            Decorations = TestWindow.DecorationSupport,
            CanSetWindowIcon = icons
        };
        public IReadOnlyList<RenderedImage>? Icons { get; private set; }
        public ImageSource? ApplicationIcon { get; set; }
        public int IconChanges { get; private set; }

        public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(configuration);
        }

        public void SetIcon(IReadOnlyList<RenderedImage>? images)
        {
            Icons = images;
            IconChanges++;
        }

        public Size ClientSize => new(200, 100);
        public string Title { get; set; } = "Portable";
        public bool IsClosed { get; private set; }
        public WindowEnvironment Environment { get; set; }
        public WindowButtonsMetrics SystemButtonsMetrics => new(12, 24, 36);
        public bool PrefersReducedMotion => false;
        public bool PrefersDarkColorScheme => false;
        public IClipboard Clipboard { get; } = new MemoryClipboard();

        public void Show(bool activate = true)
        {
        }

        public void Close() => IsClosed = true;
        public void Invalidate()
        {
        }

        public void RequestAnimationFrame(TimeSpan delay)
        {
        }

        public void Resize(Size logicalSize)
        {
        }

        public void SetTextInputCaret(Rect? caret)
        {
        }

        public void Dispose() => IsClosed = true;
    }
}
