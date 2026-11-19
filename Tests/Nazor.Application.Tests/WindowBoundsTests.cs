using Nazor.Graphics;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed unsafe partial class WindowBoundsTests
{
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Limits_clamp_programmatic_resize_and_maximize_and_can_be_removed(bool custom)
    {
        using var harness = new WindowHarness(custom ? typeof(SwitchableWindow) : typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            window.Style = "-windows-title-bar-background-color: #123456";
            window.RequestFrame();
            UpdateWindow(window.PlatformWindow.Handle);
            window.MinWidth = 360;
            window.MinHeight = 240;
            window.MaxWidth = 640;
            window.MaxHeight = 440;
            var tracking = new MINMAXINFO();
            SendMessageW(window.PlatformWindow.Handle, WM_GETMINMAXINFO, 0, (nint)(&tracking));
            Assert.True(GetWindowRect(window.PlatformWindow.Handle, out var outer));
            var scale = window.PlatformWindow.Scale;
            var frameWidth = outer.right - outer.left - (int)MathF.Round(window.PlatformWindow.ClientSize.Width * scale);
            var frameHeight = outer.bottom - outer.top - (int)MathF.Round(window.PlatformWindow.ClientSize.Height * scale);
            Assert.Equal((int)Math.Round(360 * scale) + frameWidth, tracking.ptMinTrackSize.X);
            Assert.Equal((int)Math.Round(240 * scale) + frameHeight, tracking.ptMinTrackSize.Y);
            Assert.Equal((int)Math.Round(640 * scale) + frameWidth, tracking.ptMaxTrackSize.X);
            Assert.Equal((int)Math.Round(440 * scale) + frameHeight, tracking.ptMaxTrackSize.Y);
            window.Width = 100;
            window.Height = 900;
            Assert.InRange(window.Width, 359, 361);
            Assert.InRange(window.Height, 439, 441);
            window.Maximize();
            Assert.Equal(WindowState.Maximized, window.State);
            Assert.InRange(window.PlatformWindow.ClientSize.Width, 359, 641);
            Assert.InRange(window.PlatformWindow.ClientSize.Height, 239, 441);
            window.MaxWidth = 520;
            window.MaxHeight = 340;
            Assert.InRange(window.PlatformWindow.ClientSize.Width, 359, 521);
            Assert.InRange(window.PlatformWindow.ClientSize.Height, 239, 341);
            window.Restore();
            window.MaxWidth = null;
            window.MaxHeight = null;
            window.Width = 800;
            window.Height = 600;
            Assert.InRange(window.Width, 799, 801);
            Assert.InRange(window.Height, 599, 601);
            Assert.Throws<ArgumentException>(() => window.MaxWidth = 200);
            Assert.Null(window.MaxWidth);
            Assert.Throws<ArgumentOutOfRangeException>(() => window.MinHeight = double.NaN);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    [InlineData(WindowState.Minimized)]
    [InlineData(WindowState.FullScreen)]
    public void Position_updates_screen_coordinates_or_restore_bounds_without_changing_state(WindowState state)
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            if (state == WindowState.Maximized) window.Maximize();
            if (state == WindowState.Minimized) window.Minimize();
            if (state == WindowState.FullScreen) window.EnterFullScreen();
            window.X = 130;
            window.Y = 170;
            Assert.Equal(state, window.State);
            Assert.Equal(130, window.X);
            Assert.Equal(170, window.Y);
            window.Restore();
            Assert.True(GetWindowRect(window.PlatformWindow.Handle, out var bounds));
            Assert.Equal(130, bounds.left);
            Assert.Equal(170, bounds.top);
            window.X = -20;
            Assert.Equal(-20, window.X);
            Assert.Equal(170, window.Y);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Fullscreen_ignores_limits_and_restore_reapplies_them()
    {
        using var harness = new WindowHarness(typeof(NativeAppearancePage));
        harness.Read(window =>
        {
            window.MaxWidth = 600;
            window.MaxHeight = 400;
            window.EnterFullScreen();
            var monitor = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            Assert.True(GetMonitorInfo(MonitorFromWindow(window.PlatformWindow.Handle, MONITOR_DEFAULTTONEAREST), ref monitor));
            Assert.Equal((monitor.rcMonitor.right - monitor.rcMonitor.left) / window.PlatformWindow.Scale, window.PlatformWindow.ClientSize.Width);
            window.Width = 1200;
            window.Height = 900;
            window.Restore();
            Assert.InRange(window.Width, 599, 601);
            Assert.InRange(window.Height, 399, 401);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Startup_limits_and_position_are_applied_before_showing()
    {
        using var harness = new WindowHarness(typeof(BoundedWindow));
        harness.Read(window =>
        {
            Assert.InRange(window.Width, 499, 501);
            Assert.InRange(window.Height, 299, 301);
            Assert.Equal(120, window.X);
            Assert.Equal(150, window.Y);
            return true;
        });
    }

    public sealed class BoundedWindow : WindowComponentBase
    {
        protected override void OnInitialized()
        {
            Window.MaxWidth = 500;
            Window.MaxHeight = 300;
            Window.X = 120;
            Window.Y = 150;
        }
    }
}
