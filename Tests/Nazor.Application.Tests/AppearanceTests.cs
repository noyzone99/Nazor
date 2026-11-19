using Microsoft.Extensions.DependencyInjection;
using Nazor.Controls;
using Nazor.Platform;
using Nazor.Platform.Windows;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;
[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class AppearanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void F11_preserves_placement_ignores_repeats_and_respects_app_bindings_and_capabilities(bool custom, bool maximized)
    {
        using var harness = new ApplicationHarness(() => custom ? ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false }) : ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var window = custom ? harness.Window<ChromeWindow>() : harness.Window<OrderBook>();
        ApplicationHarness.On(window, () =>
        {
            if (maximized)
                window.Maximize();
            var priorState = window.State;
            var placement = window.PlatformWindow.GetPlacement();
            var host = (IWindowHost)window;
            Assert.False(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.Shift, false));
            Assert.True(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.None, false));
            Assert.Equal(WindowState.FullScreen, window.State);
            Assert.True(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.None, true));
            Assert.Equal(WindowState.FullScreen, window.State);
            window.FullScreenEnabled = false;
            Assert.True(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.None, false));
            Assert.Equal(priorState, window.State);
            Assert.Equal(placement, window.PlatformWindow.GetPlacement());
            Assert.False(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.None, false));
            window.FullScreenEnabled = true;
            var calls = 0;
            window.Input.KeyBindings.Register("F11", Nazor.Input.KeyModifiers.None, EventCallback.Factory.Create<Nazor.Input.KeyboardEventArgs>(window, () => calls++));
            Assert.True(host.OnKeyDown("F11", Nazor.Input.KeyModifiers.None, false));
            Assert.Equal(1, calls);
            Assert.Equal(priorState, window.State);
            Assert.Equal(PlatformKind.Windows, window.Scene.Styles.Preferences.Platform);
            return true;
        });
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial nint GetActiveWindow();
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);
    [Fact]
    public unsafe void Color_scheme_is_shared_by_scopes_and_future_windows_and_System_resumes_the_OS()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var windows = ((OrderBook)main.Root.Component).Windows;
            var appearance = ((OrderBook)main.Root.Component).Services.GetRequiredService<IApplicationAppearance>();
            Assert.Equal(ColorScheme.System, appearance.ColorScheme);
            var child = (NazorWindow)windows.Open<OrderDetail>();
            var otherService = ((OrderDetail)child.Root.Component).Services.GetRequiredService<IApplicationAppearance>();
            Assert.Same(appearance, otherService);
            foreach (var request in new[]
            {
                ColorScheme.Dark,
                ColorScheme.Light,
                ColorScheme.System
            }

            )
            {
                otherService.ColorScheme = request;
                Assert.Equal(request, appearance.ColorScheme);
                foreach (var window in new[]
                {
                    main,
                    child
                }

                )
                {
                    var expected = request == ColorScheme.System ? (window.PlatformWindow.PrefersDarkColorScheme ? ColorScheme.Dark : ColorScheme.Light) : request;
                    ((IWindowHost)window).OnSystemSettingsChanged();
                    Assert.Equal(expected, window.Scene.Styles.Preferences.ColorScheme);
                    int dark = -1;
                    Assert.Equal(0, DwmGetWindowAttribute(window.PlatformWindow.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, &dark, 4));
                    Assert.Equal(expected == ColorScheme.Dark ? 1 : 0, dark);
                }

                var later = (NazorWindow)windows.Open<OrderDetail>();
                Assert.Equal(child.Scene.Styles.Preferences.ColorScheme, later.Scene.Styles.Preferences.ColorScheme);
                later.Close();
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => appearance.ColorScheme = (ColorScheme)99);
            Assert.Equal(ColorScheme.System, appearance.ColorScheme);
            return true;
        });
    }

    [Fact]
    public void Activation_is_per_opening_and_does_not_inherit_the_main_window_setting()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var windows = ((OrderBook)main.Root.Component).Windows;
            var first = (NazorWindow)windows.Open<OrderDetail>();
            Assert.Equal(first.PlatformWindow.Handle, GetActiveWindow());
            var second = (NazorWindow)windows.Open<OrderDetail>(options: new WindowOpenOptions());
            Assert.Equal(second.PlatformWindow.Handle, GetActiveWindow());
            var active = (NazorWindow)windows.Open<OrderDetail>(options: new WindowOpenOptions { Activate = true });
            Assert.Equal(active.PlatformWindow.Handle, GetActiveWindow());
            windows.Open<OrderDetail>(options: new WindowOpenOptions { Activate = false });
            Assert.Equal(active.PlatformWindow.Handle, GetActiveWindow());
            return true;
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public unsafe void Full_screen_covers_the_monitor_and_restores_native_chrome_placement_and_state(bool custom, bool maximized)
    {
        using var harness = new ApplicationHarness(() => custom ? ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false }) : ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var window = custom ? harness.Window<ChromeWindow>() : harness.Window<OrderBook>();
        ApplicationHarness.On(window, () =>
        {
            var native = window.PlatformWindow;
            var handle = native.Handle;
            if (maximized)
                window.Maximize();
            var priorState = window.State;
            var prior = native.GetPlacement();
            var style = GetWindowLongPtrW(handle, -16);
            var states = new List<WindowState>();
            window.StateChanged += () => states.Add(window.State);
            window.EnterFullScreen();
            window.EnterFullScreen();
            Assert.Equal(WindowState.FullScreen, window.State);
            Assert.Equal(prior, native.GetPlacement());
            var monitor = new MONITORINFO
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };
            Assert.True(GetMonitorInfo(MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST), ref monitor));
            Assert.True(GetWindowRect(handle, out var actual));
            Assert.Equal((monitor.rcMonitor.left, monitor.rcMonitor.top, monitor.rcMonitor.right, monitor.rcMonitor.bottom), (actual.left, actual.top, actual.right, actual.bottom));
            Assert.Equal(0, (long)GetWindowLongPtrW(handle, -16) & (WS_OVERLAPPEDWINDOW | WS_MINIMIZE | WS_MAXIMIZE));
            Assert.Equal((nint)1, SendMessageW(handle, 0x0084, 0, (actual.top << 16) | (actual.left & 0xffff)));
            window.Minimize();
            Assert.Equal(WindowState.Minimized, window.State);
            window.EnterFullScreen();
            Assert.Equal(WindowState.FullScreen, window.State);
            window.Restore();
            Assert.Equal(priorState, window.State);
            Assert.Equal(prior, native.GetPlacement());
            Assert.Equal(style, GetWindowLongPtrW(handle, -16));
            Assert.Equal(new[] { WindowState.FullScreen, WindowState.Minimized, WindowState.FullScreen, priorState }, states);
            window.ExitFullScreen();
            Assert.Equal(priorState, window.State);
            window.EnterFullScreen();
            window.Maximize();
            Assert.Equal(WindowState.Maximized, window.State);
            return true;
        });
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(2, false)]
    public void Invalid_title_bar_cardinality_is_rejected_before_native_creation(int bars, bool custom)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var windows = ((OrderBook)main.Root.Component).Windows;
            var error = Assert.Throws<InvalidOperationException>(() => windows.Open<ChromeContract>(new Dictionary<string, object?> { [nameof(ChromeContract.Bars)] = bars, [nameof(ChromeContract.Custom)] = custom }));
            Assert.Contains("TitleBar", error.Message);
            Assert.Single(windows.Windows);
            return true;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Entering_from_minimized_restores_the_previously_normal_or_maximized_state(bool maximized)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            if (maximized)
                main.Maximize();
            var expected = main.State;
            var placement = main.PlatformWindow.GetPlacement();
            main.Minimize();
            main.EnterFullScreen();
            Assert.Equal(WindowState.FullScreen, main.State);
            Assert.False(IsIconic(main.PlatformWindow.Handle));
            Assert.False(IsZoomed(main.PlatformWindow.Handle));
            main.ExitFullScreen();
            Assert.Equal(expected, main.State);
            Assert.Equal(placement, main.PlatformWindow.GetPlacement());
            return true;
        });
    }

    [Fact]
    public void Chrome_validation_accepts_interactive_content_and_reports_later_duplicate_bars()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var windows = ((OrderBook)main.Root.Component).Windows;
            var parameters = new Dictionary<string, object?>
            {
                [nameof(ChromeContract.Bars)] = 1,
                [nameof(ChromeContract.Custom)] = true,
                [nameof(ChromeContract.InteractiveContent)] = true
            };
            var window = (NazorWindow)windows.Open<ChromeContract>(parameters);
            Assert.Single(window.Root.Elements("Button"));
            Assert.Empty(window.Exceptions);
            ((ChromeContract)window.Root.Component).AddBar();
            Assert.Contains(window.Exceptions, e => e.Message.Contains("TitleBar", StringComparison.Ordinal));
            return true;
        });
    }

    [Fact]
    public void The_default_theme_is_Basic_and_live_choices_apply_to_existing_and_future_windows()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var appearance = ((OrderBook)main.Root.Component).Services.GetRequiredService<IApplicationAppearance>();
            var windows = ((OrderBook)main.Root.Component).Windows;
            Assert.Same(Themes.Themes.Basic, main.Scene.Styles.Theme);
            foreach (var theme in new[] { Themes.Themes.Basic, Themes.Themes.None, null })
            {
                appearance.Theme = theme;
                var expected = theme ?? Themes.Themes.Basic;
                Assert.Same(expected, main.Scene.Styles.Theme);
                var child = (NazorWindow)windows.Open<OrderDetail>();
                Assert.Same(expected, child.Scene.Styles.Theme);
                child.Close();
            }
            return true;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Windowing_starts_with_private_fonts_and_composed_title_bar_with_Basic_or_None(bool basic)
    {
        var options = new ApplicationRuntimeSettings
        {
            Activate = false,
            Theme = basic ? Themes.Themes.Basic : Themes.Themes.None
        };
        options.Fonts.Add("Fonts/FiraSans-Regular.ttf", "Fonts/FiraSans-Bold.ttf");
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Nazor.Samples.Windowing.App>(options));
        var window = harness.Window<Nazor.Samples.Windowing.App>();
        try
        {
            ApplicationHarness.On(window, () =>
            {
                window.Scene.Update();
                Assert.Empty(window.Exceptions);
                Assert.Same(options.Theme, window.Scene.Styles.Theme);
                Assert.Single(window.Root.Elements("TitleBar"));
                var bar = window.Root.Elements("TitleBar").Single();
                Assert.DoesNotContain(bar.Elements(), element => element.Name == "CaptionButtonsButton");
                Assert.True(window.SystemButtonsSize.Width > 0);
                var insets = window.Scene.GetLayoutNode(bar)!.Style.ContentInsets;
                Assert.Equal(window.SystemButtonsSize.Width, insets.Left + insets.Right);
                ((Nazor.Samples.Windowing.App)window.Root.Component).ToggleSystemButtons();
                Assert.Empty(window.Exceptions);
                window.Scene.Update();
                var area = Assert.Single(window.Root.Elements("CaptionButtonsButton"), element => element.Attributes.GetValueOrDefault("kind") is "maximize");
                if (basic)
                {
                    Assert.True(window.Scene.GetBounds(bar).Height > 0);
                    Assert.Equal(46, window.Scene.GetBounds(area).Width);
                }
                Assert.Equal(3, bar.Elements().Count(e => e.Name == "CaptionButtonsButton"));
                var text = window.Scene.BuildDisplayList().Commands.OfType<Nazor.Graphics.DrawText>().Select(d => d.Layout.Primary).ToArray();
                Assert.Contains(text, face => face.FamilyName == "Fira Sans" && face.Weight == 400);
                Assert.Contains(text, face => face.FamilyName == "Fira Sans" && face.Weight == 700);
                window.EnterFullScreen();
                window.ExitFullScreen();
                Assert.Equal(WindowState.Normal, window.State);
                Assert.Empty(window.Exceptions);
                return true;
            });
        }
        finally
        {
            ApplicationHarness.On(window, () =>
            {
                window.Close();
                return true;
            });
            Assert.True(harness.WaitForExit());
        }
    }
}

public sealed class ChromeContract : WindowComponentBase
{
    [Parameter]
    public int Bars { get; set; }

    [Parameter]
    public bool Custom { get; set; }

    [Parameter]
    public bool InteractiveContent { get; set; }

    public void AddBar()
    {
        Bars++;
        StateHasChanged();
    }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(b =>
        {
            for (var i = 0; i < Bars; i++)
            {
                b.OpenComponent<UserTitleBar>(0, typeof(ChromeContract));
                if (InteractiveContent)
                    b.AddComponentParameter(1, "ChildContent", (RenderFragment)(content =>
                    {
                        content.OpenComponent<Button>(0, typeof(ChromeContract));
                        content.CloseComponent();
                    }));
                b.CloseComponent();
            }
        }));
    }

    protected override void OnInitialized()
    {
        Window.Decorations = WindowDecorationPolicy.Require(Custom ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native());
    }
}
