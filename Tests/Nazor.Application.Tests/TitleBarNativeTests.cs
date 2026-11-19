using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class TitleBarNativeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Leaf_caption_group_stays_hovered_over_the_drag_gap_between_buttons(bool nativeFrame)
    {
        using var harness = new WindowHarness(typeof(WindowPresentationTests.EmptyWindow), new()
        {
            Rendering = RenderingMode.Cpu, Theme = NazorLeafTheme.Theme,
            Decorations = WindowDecorationPolicy.Require(nativeFrame
                ? WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Nazor) : WindowDecorations.Nazor()),
        });
        harness.WaitUntil(window => window.FrameCount > 0, "Leaf caption paint");
        harness.Read(window =>
        {
            var group = Assert.Single(window.Root.Elements("LeafWindowButtons"));
            var buttons = window.Root.Elements("LeafWindowButton").ToArray();
            var first = window.Scene.GetBounds(buttons[0]);
            var next = window.Scene.GetBounds(buttons[1]);
            window.Input.PointerMoved(new Point(first.X + first.Width / 2, first.Y + first.Height / 2), KeyModifiers.None);
            var colors = window.Root.Elements("LeafWindowButtonLeaf").Select(leaf =>
                window.Scene.GetStyle(leaf).Get(Style.StyleProperties.BackgroundColor)).ToArray();
            var gap = new Point((first.Right + next.X) / 2, first.Y + first.Height / 2);
            Assert.Equal(WindowRegion.Caption, window.Input.RegionAt(gap));
            var screen = new POINT { X = (int)MathF.Round(gap.X * window.Scene.Scale), Y = (int)MathF.Round(gap.Y * window.Scene.Scale) };
            Assert.True(ClientToScreen(harness.Handle, ref screen));
            var coordinates = (nint)(((long)screen.Y << 16) | (ushort)screen.X);
            SendMessageW(harness.Handle, WM_MOUSELEAVE, 0, 0);
            SendMessageW(harness.Handle, WM_NCMOUSEMOVE, HTCAPTION, coordinates);
            Assert.True((group.States & ElementStates.Hover) != 0);
            Assert.Equal(colors, window.Root.Elements("LeafWindowButtonLeaf").Select(leaf =>
                window.Scene.GetStyle(leaf).Get(Style.StyleProperties.BackgroundColor)).ToArray());
            return true;
        });
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Native_presenter_tracks_initial_and_runtime_operation_permissions(bool minimize, bool maximize)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "AppWindow requires Windows 11.");
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new()
        {
            Activate = false, IsMinimizable = minimize, IsMaximizable = maximize,
        }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(window.PlatformWindow.Handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
            var presenter = Assert.IsType<Microsoft.UI.Windowing.OverlappedPresenter>(appWindow.Presenter);
            Assert.Equal(minimize, presenter.IsMinimizable);
            Assert.Equal(maximize, presenter.IsMaximizable);
            window.IsMinimizable = !minimize;
            window.IsMaximizable = !maximize;
            Assert.Equal(!minimize, presenter.IsMinimizable);
            Assert.Equal(!maximize, presenter.IsMaximizable);
            window.EnterFullScreen();
            window.IsMinimizable = minimize;
            window.IsMaximizable = maximize;
            window.ExitFullScreen();
            Assert.Equal(minimize, presenter.IsMinimizable);
            Assert.Equal(maximize, presenter.IsMaximizable);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);

    private static nint Pack(int x, int y) => (nint)((y << 16) | (x & 0xFFFF));

    [Fact]
    public void System_controls_layout_survives_hidden_controls_and_notifies_without_metrics_changes()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            IWindow api = window;
            var expected = new WindowButtonsLayout([], [WindowButton.Minimize, WindowButton.Maximize, WindowButton.Close]);
            Assert.True((api.Capabilities.Decorations.IntegratedNativeCaptionButtons == WindowSupportStatus.Supported));
            Assert.Equal(expected, api.SystemButtonsLayout);
            ((ChromeWindow)window.Root.Component).ToggleSystemButtons();
            window.Scene.Update();
            Assert.Equal(default, api.SystemButtonsMetrics);
            var changes = 0;
            api.SystemButtonsChanged += () => changes++;
            ((IWindowHost)window).OnSystemSettingsChanged();
            Assert.Equal(0, changes);

            window.Hide();
            Assert.Equal(expected, api.SystemButtonsLayout);
            window.Show(activate: false);
            window.EnterFullScreen();
            Assert.Equal(expected, api.SystemButtonsLayout);
            window.ExitFullScreen();
            Assert.Equal(0, changes);

            api.IsMinimizable = false;
            Assert.Equal(expected, api.SystemButtonsLayout);
            api.IsMaximizable = false;
            Assert.Equal(expected, api.SystemButtonsLayout);
            Assert.Equal(default, api.SystemButtonsMetrics);
            Assert.Equal(0, changes);
            api.IsMaximizable = true;
            Assert.Equal(expected, api.SystemButtonsLayout);
            Assert.Equal(0, changes);

            var handle = window.PlatformWindow.Handle;
            var exStyle = GetWindowLongPtrW(handle, -20  );
            try
            {
                SetWindowLongPtrW(handle, -20, (nint)((long)exStyle | 0x400000  ));
                Assert.Equal(new WindowButtonsLayout([WindowButton.Close, WindowButton.Maximize, WindowButton.Minimize], []), api.SystemButtonsLayout);
                Assert.Equal(1, changes);
            }
            finally { SetWindowLongPtrW(handle, -20, exStyle); }
            Assert.Equal(expected, api.SystemButtonsLayout);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    private static Point Center(NazorWindow window, ElementNode element)
    {
        var bounds = window.Scene.GetBounds(element);
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Fact]
    public void Non_client_snap_input_reaches_the_app_button_once_and_empty_padding_does_nothing()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Nazor.Samples.Windowing.App>(new() { Activate = false }));
        var window = harness.Window<Nazor.Samples.Windowing.App>();
        try
        {
            ApplicationHarness.On(window, () =>
            {
                ((Nazor.Samples.Windowing.App)window.Root.Component).ToggleSystemButtons();
                window.Scene.Update();
                var button = window.Root.Elements("CaptionButtonsButton").Single(e => e.Attributes.GetValueOrDefault("kind") is "maximize");
                var point = Center(window, button);
                var native = window.PlatformWindow;
                var screen = new POINT { X = (int)(point.X * native.Scale), Y = (int)(point.Y * native.Scale) };
                Assert.True(ClientToScreen(native.Handle, ref screen));
                var coordinates = Pack(screen.X, screen.Y);
                var hit = SendMessageW(native.Handle, WM_NCHITTEST, 0, coordinates);
                Assert.Equal(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) ? 9 : 1, hit);
                if (hit == 9)
                {
                    SendMessageW(native.Handle, WM_NCMOUSEMOVE, hit, coordinates);
                    Assert.True((button.States & ElementStates.Hover) != 0);
                    SendMessageW(native.Handle, WM_NCLBUTTONDOWN, hit, coordinates);
                    Assert.Equal(native.Handle, GetCapture());
                    SendMessageW(native.Handle, WM_LBUTTONUP, 0,
                        Pack((int)(point.X * native.Scale), (int)(point.Y * native.Scale)));
                    Assert.Equal((nint)0, GetCapture());
                    Assert.Equal(WindowState.Maximized, window.State);
                    window.Restore();
                    window.Scene.Update();
                    SendMessageW(native.Handle, WM_NCRBUTTONDOWN, 9, coordinates);
                    SendMessageW(native.Handle, WM_NCRBUTTONUP, 9, coordinates);
                    window.Scene.Update();
                    Assert.Empty(window.Scene.Overlays);
                    Assert.Equal(WindowState.Normal, window.State);
                    var bounds = window.Scene.GetBounds(window.Root.Elements().Single(e => e.Class == "drag-space"));
                    var padding = new POINT { X = (int)((bounds.X + 2) * native.Scale), Y = (int)((bounds.Y + 2) * native.Scale) };
                    Assert.True(ClientToScreen(native.Handle, ref padding));
                    var empty = Pack(padding.X, padding.Y);
                    SendMessageW(native.Handle, WM_NCLBUTTONDOWN, 9, empty);
                    SendMessageW(native.Handle, WM_NCLBUTTONUP, 9, empty);
                    Assert.Equal(WindowState.Normal, window.State);
                    Assert.Equal((nint)0, GetCapture());
                }
                Assert.Empty(window.Exceptions);
                return true;
            });
        }
        finally
        {
            ApplicationHarness.On(window, () => { window.Close(); return true; });
            Assert.True(harness.WaitForExit());
        }
    }

}
