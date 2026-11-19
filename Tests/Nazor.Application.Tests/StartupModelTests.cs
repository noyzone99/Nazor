using Microsoft.Extensions.DependencyInjection;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;
[Collection("Windows")]
public sealed class StartupModelTests
{
    private static NazorApplicationBuilder Builder(RecordingPlatform platform) => NazorApplication.CreateBuilder(platformHost: new RecordingHost(platform));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dialog_close_completes_the_result_and_reenables_only_a_live_owner(bool disposeOwnerFirst)
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        builder.Application.UseMainWindow<EmptyWindow>();
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        using var app = builder.Build();
        Task<object?>? completion = null;
        platform.OnRun = () =>
        {
            var windows = app.Services.GetRequiredService<IWindowService>();
            var owner = (NazorWindow)windows.MainWindow!;
            var surface = (Surface)owner.PlatformWindow;
            completion = windows.ShowDialogAsync(typeof(EmptyWindow), options: new() { Owner = owner, Activate = false });
            var dialog = (NazorWindow)windows.Windows.Last();
            Assert.False(surface.Enabled);
            if (disposeOwnerFirst) owner.Dispose();

            dialog.Close("accepted");

            Assert.True(completion.IsCompletedSuccessfully);
            Assert.Equal(!disposeOwnerFirst, surface.Enabled);
            Assert.True(dialog.IsClosed);
        };
        Assert.Equal(37, app.Run());
        Assert.Equal("accepted", await completion!);
    }

    [Fact]
    public async Task Application_shutdown_with_nested_dialogs_completes_all_dialog_tasks()
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        builder.Application.UseMainWindow<EmptyWindow>();
        using var app = builder.Build();
        Task<object?>? first = null, second = null;
        IWindow[] opened = [];
        var windows = app.Services.GetRequiredService<IWindowService>();
        platform.OnRun = () =>
        {
            first = windows.ShowDialogAsync(typeof(EmptyWindow), options: new() { Owner = windows.MainWindow, Activate = false });
            second = windows.ShowDialogAsync(typeof(EmptyWindow), options: new() { Owner = windows.Windows.Last(), Activate = false });
            opened = windows.Windows.ToArray();
            Assert.Equal(3, opened.Length);
        };

        Assert.Equal(37, app.Run());

        Assert.True(first!.IsCompletedSuccessfully);
        Assert.True(second!.IsCompletedSuccessfully);
        Assert.Null(await first!);
        Assert.Null(await second!);
        Assert.All(opened, window => Assert.True(window.IsClosed));
        Assert.Empty(windows.Windows);
        Assert.Null(windows.MainWindow);
    }

    [Fact]
    public void Startup_is_required_unique_and_captured_at_build()
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        WindowConfiguration? definition = null;
        builder.Application.CreateMainWindow<TextBlock>(window =>
        {
            definition = window;
            window.Title = "Explicit";
            window.Width = 321;
        });
        Assert.Throws<InvalidOperationException>(() => builder.Application.UseMainWindow<DeclaredWindow>());
        using var app = builder.Build();
        Assert.Equal(0, platform.Created);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        definition!.Width = 999;
        builder.Platform.Host = null;
        Assert.Equal(37, app.Run());
        Assert.Equal("Explicit", platform.Options!.Title);
        Assert.Equal(321, platform.Options.Width);
        Assert.Throws<InvalidOperationException>(() => app.Run());
    }

    [Theory]
    [InlineData(typeof(TextBlock))]
    [InlineData(typeof(NestedWindow))]
    [InlineData(typeof(TwoWindows))]
    public void Declared_mode_requires_exactly_one_top_level_window(Type component)
    {
        var platform = new RecordingPlatform();
        using var services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
        var error = Assert.Throws<InvalidOperationException>(() => new NazorWindow(platform, new UIDispatcher(), component, new() { DeclaredWindow = true, Services = services }));
        Assert.Contains("@window", error.Message);
        Assert.Equal(0, platform.Created);
    }

    [Fact]
    public void Explicit_content_window_rejects_a_window_in_the_component()
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        builder.Application.CreateMainWindow<DeclaredWindow>();
        using var app = builder.Build();
        Assert.Contains("Content-only hosting", Assert.Throws<InvalidOperationException>(() => app.Run()).Message);
        app.Appearance.ColorScheme = ColorScheme.Dark;
        Assert.Equal(0, platform.Created);
    }

    [Fact]
    public void A_declared_window_owns_its_geometry_and_secondary_windows_do_not_inherit_main_geometry()
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        builder.Application.UseMainWindow<DeclaredWindow>();
        using var app = builder.Build();
        platform.OnRun = () =>
        {
            Assert.Equal("Declared", platform.Options!.Title);
            Assert.Equal(444, platform.Options.Width);
            var windows = app.Services.GetRequiredService<IWindowService>();
            var child = windows.Open<EmptyWindow>();
            Assert.Equal(800, platform.Options.Width);
            Assert.Equal(600, platform.Options.Height);
            Assert.Equal("Nazor", platform.Options.Title);
            Assert.Throws<InvalidOperationException>(() => windows.Open<TextBlock>());
            child.Close();
        };
        Assert.Equal(37, app.Run());
    }

    [Fact]
    public void Appearance_is_shared_live_and_inherited_by_future_windows()
    {
        var platform = new RecordingPlatform();
        var builder = Builder(platform);
        builder.Application.UseMainWindow<DeclaredWindow>();
        using var app = builder.Build();
        Assert.Same(app.Appearance, app.Services.GetRequiredService<IApplicationAppearance>());
        app.Appearance.Theme = Controls.StyleSheets.None;
        app.Appearance.ColorScheme = ColorScheme.Dark;
        platform.OnRun = () =>
        {
            var windows = app.Services.GetRequiredService<IWindowService>();
            var main = Assert.Single(NazorWindow.OpenWindows);
            Assert.Same(Controls.StyleSheets.None, main.Scene.Styles.Theme);
            Assert.Equal(ColorScheme.Dark, main.Scene.Styles.Preferences.ColorScheme);
            app.Appearance.Theme = Controls.StyleSheets.Default;
            app.Appearance.StyleSheet = Controls.StyleSheets.None;
            app.Appearance.ColorScheme = ColorScheme.Light;
            app.Appearance.ReducedMotion = true;
            app.Appearance.HighContrast = true;
            var child = (NazorWindow)windows.Open<DeclaredWindow>();
            foreach (var window in new[]
            {
                main,
                child
            }

            )
            {
                Assert.Same(Controls.StyleSheets.Default, window.Scene.Styles.Theme);
                Assert.Same(Controls.StyleSheets.None, window.Scene.Styles.Application);
                Assert.Equal(ColorScheme.Light, window.Scene.Styles.Preferences.ColorScheme);
                Assert.True(window.Scene.Transitions.ReducedMotion);
            }

            Exception? error = null;
            var worker = new Thread(() => error = Record.Exception(() => app.Appearance.ColorScheme = ColorScheme.Dark));
            worker.Start();
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
            Assert.IsType<InvalidOperationException>(error);
            Assert.Equal(ColorScheme.Light, app.Appearance.ColorScheme);
            child.Close();
        };
        Assert.Equal(37, app.Run());
        app.Appearance.ColorScheme = ColorScheme.System;
    }

    [Fact]
    public void Single_view_requires_a_capable_backend_and_creates_no_desktop_window()
    {
        var unsupported = Builder(new RecordingPlatform());
        unsupported.Application.UseMainView<TextBlock>();
        using (var app = unsupported.Build())
            Assert.Throws<PlatformNotSupportedException>(() => app.Run());
        var platform = new SingleViewPlatform();
        var builder = Builder(platform);
        builder.Application.UseMainView<TextBlock>();
        using var single = builder.Build();
        platform.OnRun = () => Assert.Throws<PlatformNotSupportedException>(() => single.Services.GetRequiredService<IWindowService>().Open<DeclaredWindow>());
        Assert.Equal(37, single.Run());
        Assert.Equal(0, platform.Created);
        Assert.Equal(1, platform.Views);
    }

    [Fact]
    public void Font_policy_can_be_replaced_and_null_configuration_is_rejected_before_build()
    {
        var builder = Builder(new RecordingPlatform());
        builder.Application.CreateMainWindow<TextBlock>();
        builder.Fonts = null!;
        Assert.Throws<ArgumentNullException>(() => builder.Build());
        builder.Fonts = new()
        {
            DefaultFamily = "sans-serif",
            UseSystemFonts = false
        };
        using var app = builder.Build();
        Assert.Equal(37, app.Run());
    }

    [Fact]
    public void Old_public_options_and_implicit_run_overloads_are_removed()
    {
        Assert.Null(typeof(NazorApplicationBuilder).GetProperty("Options"));
        Assert.Null(typeof(NazorApplication).Assembly.GetType("Nazor.NazorApplicationOptions"));
        Assert.DoesNotContain(typeof(NazorApplication).GetMethods(), method => method.Name == "Run");
        Assert.Single(typeof(NazorApp).GetMethods(), method => method.Name == "Run");
        Assert.Null(typeof(WindowOpenOptions).GetProperty("Width"));
        Assert.Null(typeof(IWindowService).GetProperty("ColorScheme"));
        foreach (var name in new[] { "UseMainWindow", "CreateMainWindow", "UseMainView", "UseTrayItem" })
        {
            Assert.DoesNotContain(typeof(NazorApplicationBuilder).GetMethods(), method => method.Name == name);
            Assert.Single(typeof(ApplicationBuilder).GetMethods(), method => method.Name == name);
        }
    }

    [Fact]
    public void Application_configuration_is_a_data_snapshot_separate_from_the_sub_builder()
    {
        var builder = Builder(new RecordingPlatform());
        builder.Application.CreateMainWindow<TextBlock>();
        builder.Application.Culture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
        builder.Application.UICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        builder.Application.ShutdownMode = ShutdownMode.OnLastWindowClose;
        using var app = builder.Build();
        builder.Application.Culture = null;
        builder.Application.UICulture = null;
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var configuration = app.Services.GetRequiredService<ApplicationConfiguration>();
        Assert.Equal("it-IT", configuration.Culture?.Name);
        Assert.Equal("en-US", configuration.UICulture?.Name);
        Assert.Equal(ShutdownMode.OnLastWindowClose, configuration.ShutdownMode);
        Assert.Null(app.Services.GetService<ApplicationBuilder>());
        Assert.Throws<InvalidOperationException>(() => builder.Application.UseMainWindow<DeclaredWindow>());
        Assert.Throws<InvalidOperationException>(() => builder.Application.CreateMainWindow<TextBlock>());
        Assert.Throws<InvalidOperationException>(() => builder.Application.UseMainView<TextBlock>());
    }

    public sealed class DeclaredWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
        }

        protected override void OnInitialized()
        {
            Window.Title = "Declared";
            Window.Width = 444d;
        }
    }

    public sealed class NestedWindow : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DeclaredWindow>(0);
            builder.CloseComponent();
        }

        protected override void OnInitialized()
        {
        }
    }

    public sealed class EmptyWindow : WindowComponentBase { }

    public sealed class TwoWindows : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DeclaredWindow>(0, typeof(TwoWindows));
            builder.CloseComponent();
            builder.OpenComponent<DeclaredWindow>(1, typeof(TwoWindows));
            builder.CloseComponent();
        }
    }

    private sealed class RecordingHost(RecordingPlatform platform) : IPlatformHost
    {
        public int Run(Func<IPlatform, int> application) => application(platform);
    }

    private class RecordingPlatform : IPlatform
    {
        public WindowCapabilities Capabilities => new(false, false, false, false) { Decorations = Nazor.Testing.TestWindow.DecorationSupport };
        public WindowOptions? Options { get; private set; }
        public int Created { get; private set; }
        public Action? OnRun { get; set; }

        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
        {
            Created++;
            Options = options;
            return new Surface(host);
        }

        public int Run(UIDispatcher dispatcher)
        {
            OnRun?.Invoke();
            return 37;
        }

        public void Quit(int exitCode = 0)
        {
        }
    }

    private sealed class SingleViewPlatform : RecordingPlatform, ISingleViewPlatform
    {
        public int Views { get; private set; }

        public IPlatformWindow CreateView(IWindowHost host)
        {
            Views++;
            return new Surface(host);
        }
    }

    private sealed class Surface(IWindowHost host) : IPlatformWindow
    {
        public WindowCapabilities Capabilities => new(false, false, false, false) { Decorations = TestWindow.DecorationSupport };
        public nint Handle => 0;
        public float Scale => 1;
        public Size ClientSize => new(640, 480);
        public string Title { get; set; } = "Surface";
        public bool IsClosed { get; private set; }
        public bool Enabled { get; private set; } = true;
        public void SetEnabled(bool value)
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            Enabled = value;
        }
        public bool PrefersReducedMotion => false;
        public bool PrefersDarkColorScheme => false;
        public IClipboard Clipboard { get; } = new MemoryClipboard();

        public void Show(bool activate = true)
        {
        }

        public void Close() { if (!IsClosed && host.OnClosing()) Dispose(); }
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

        public void Dispose()
        {
            if (IsClosed) return;
            IsClosed = true;
            host.OnClosed();
        }
    }
}
