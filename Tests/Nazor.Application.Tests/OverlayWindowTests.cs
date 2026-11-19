using Nazor.Graphics;
using Nazor.Testing;
using Nazor.Nss;
using System.Runtime.InteropServices;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class OverlayWindowTests
{
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindowExW(nint parent, nint after, string className, string? title);

    private static nint NativePopup(nint owner)
    {
        for (nint popup = 0; (popup = FindWindowExW(0, popup, "Nazor.Popup", null)) != 0;)
            if (GetWindow(popup, 4) == owner) return popup;
        return 0;
    }

    [Fact]
    public void An_external_menu_crosses_the_owner_edge_follows_movement_and_routes_option_input()
    {
        var style = NssCompiler.Compile("ExternalMenu", "Menu { border-radius: 6px; box-shadow: 0 4px 8px #0008; transform: translateX(40px); }");
        Assert.True(style.Success);
        using var harness = new WindowHarness(typeof(MenuWindow), Options with { StyleSheet = style.StyleSheet });
        Assert.True(harness.Read(w =>
        {
            w.AllowPopupsOutsideWindow = true;
            return w.Capabilities.CanShowPopupsOutsideWindow;
        }));
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var point = new Point(280, 195);
        harness.Mouse(WM_RBUTTONDOWN, point, 2);
        harness.Mouse(WM_RBUTTONUP, point);
        harness.WaitUntil(w => HasOverlay(w, "Menu") && NativePopup(harness.Handle) != 0 &&
            IsWindowVisible(NativePopup(harness.Handle)), "the native menu");

        var popup = NativePopup(harness.Handle);
        Assert.True(GetWindowRect(popup, out var before));
        Assert.True(GetWindowRect(harness.Handle, out var ownerBefore));
        Assert.True(before.bottom > ownerBefore.bottom);
        Assert.True(before.right - before.left > harness.Read(w => w.Scene.GetBounds(w.Scene.Overlays.Single()).Width * w.Scene.Scale));
        Assert.False(harness.Read(w => w.Semantics.Find(w.Root.Elements().Single(element => element.Id == "copy"))!.IsOffscreen));
        harness.Read(w => { w.PlatformWindow.Move(ownerBefore.left + 30, ownerBefore.top + 20); return true; });
        harness.WaitUntil(w => GetWindowRect(popup, out var moved) && moved.left == before.left + 30 && moved.top == before.top + 20,
            "the menu following its owner");

        var option = harness.Read(w => w.Scene.GetBounds(w.Root.Elements().Single(element => element.Id == "copy")));
        var scale = harness.Read(w => w.Scene.Scale);
        var origin = new POINT();
        Assert.True(ClientToScreen(harness.Handle, ref origin));
        Assert.True(GetWindowRect(popup, out var actual));
        var menu = harness.Read(w => w.Scene.GetBounds(w.Scene.Overlays.Single()));
        Assert.True(actual.right >= origin.X + MathF.Ceiling(menu.Right * scale));
        var px = origin.X + (int)((option.X + option.Width / 2) * scale) - actual.left;
        var py = origin.Y + (int)((option.Y + option.Height / 2) * scale) - actual.top;
        var coordinates = (nint)((py << 16) | (px & 0xffff));
        PostMessageW(popup, WM_LBUTTONDOWN, 1, coordinates);
        PostMessageW(popup, WM_LBUTTONUP, 0, coordinates);
        harness.WaitUntil(w => ((MenuWindow)w.Root.Component).Copies == 1 && !HasOverlay(w, "Menu"), "the selected command");
        harness.WaitUntil(w => NativePopup(harness.Handle) == 0, "native menu cleanup");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }
    private static readonly ApplicationRuntimeSettings Options = new() { Title = "Nazor test — Menu", Width = 320, Height = 200 };

    private static bool HasOverlay(NazorWindow window, string name) => window.Scene.Overlays.Any(o => o.Name == name);

    [Fact]
    public void External_menus_fit_the_monitor_work_area_near_its_bottom_right_edge()
    {
        using var harness = new WindowHarness(typeof(MenuWindow), Options);
        var monitor = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        Assert.True(GetMonitorInfo(MonitorFromWindow(harness.Handle, MONITOR_DEFAULTTONEAREST), ref monitor));
        Assert.True(GetWindowRect(harness.Handle, out var owner));
        harness.Read(w =>
        {
            w.AllowPopupsOutsideWindow = true;
            w.PlatformWindow.Move(monitor.rcWork.right - (owner.right - owner.left), monitor.rcWork.bottom - (owner.bottom - owner.top));
            return true;
        });
        var point = new Point(300, 190);
        harness.Mouse(WM_RBUTTONDOWN, point, 2);
        harness.Mouse(WM_RBUTTONUP, point);
        harness.WaitUntil(w => NativePopup(harness.Handle) != 0 && IsWindowVisible(NativePopup(harness.Handle)), "the edge-constrained menu");
        Assert.True(GetWindowRect(NativePopup(harness.Handle), out var bounds));
        Assert.True(bounds.left >= monitor.rcWork.left && bounds.right <= monitor.rcWork.right);
        Assert.True(bounds.top >= monitor.rcWork.top && bounds.bottom <= monitor.rcWork.bottom);
        var anchor = new POINT { X = (int)(point.X * harness.Read(w => w.Scene.Scale)), Y = (int)(point.Y * harness.Read(w => w.Scene.Scale)) };
        Assert.True(ClientToScreen(harness.Handle, ref anchor));
        Assert.True(bounds.bottom <= anchor.Y + 1);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Native_submenus_close_in_order_and_the_window_setting_rehosts_an_open_menu()
    {
        using var harness = new WindowHarness(typeof(MenuWindow), Options);
        harness.Read(w => { w.AllowPopupsOutsideWindow = true; return true; });
        var point = new Point(160, 60);
        harness.Mouse(WM_RBUTTONDOWN, point, 2);
        harness.Mouse(WM_RBUTTONUP, point);
        harness.WaitUntil(w => NativePopup(harness.Handle) != 0 && w.Input.Focused?.Id == "copy", "the native parent menu");
        var parent = NativePopup(harness.Handle);
        harness.Key(0x28, down: true);
        harness.Key(0x28, down: false);
        harness.WaitUntil(w => w.Input.Focused?.Id == "more", "the submenu item");
        harness.Key(0x27, down: true);
        harness.Key(0x27, down: false);
        harness.WaitUntil(w => w.Scene.Overlays.Count == 2 && NativePopup(parent) != 0, "the native submenu");
        harness.Key(0x1B, down: true);
        harness.Key(0x1B, down: false);
        harness.WaitUntil(w => w.Scene.Overlays.Count == 1 && NativePopup(parent) == 0, "submenu cleanup");
        harness.Read(w => { w.AllowPopupsOutsideWindow = false; return true; });
        harness.WaitUntil(w => NativePopup(harness.Handle) == 0 && w.Scene.Overlays.Count == 1, "the internal menu");
        var bounds = harness.Read(w => w.Scene.GetBounds(w.Scene.Overlays.Single()));
        Assert.True(bounds.X >= 0 && bounds.Y >= 0 && bounds.Right <= 320 && bounds.Bottom <= 200);
        harness.Key(0x1B, down: true);
        harness.Key(0x1B, down: false);
        harness.WaitUntil(w => w.Scene.Overlays.Count == 0, "menu dismissal");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_right_click_opens_a_context_menu_at_the_pointer_and_a_click_outside_closes_it()
    {
        using var harness = new WindowHarness(typeof(MenuWindow), Options);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var point = new Point(160, 60);
        harness.Mouse(WM_MOUSEMOVE, point);
        harness.Mouse(WM_RBUTTONDOWN, point, 2);
        harness.Mouse(WM_RBUTTONUP, point);
        harness.WaitUntil(w => HasOverlay(w, "Menu") && w.Input.Focused?.Id == "copy", "the menu with the focus on its first item");
        var menu = harness.Read(w => w.Scene.GetBounds(w.Scene.Overlays.Single()));
        Assert.Equal(point.X, menu.X, 0.5f);
        Assert.Equal(point.Y, menu.Y, 0.5f);

        var frames = harness.Read(w => w.FrameCount);
        harness.Click(new Point(40, 160));
        harness.WaitUntil(w => !HasOverlay(w, "Menu"), "the menu closed by the click outside");
        harness.WaitUntil(w => w.FrameCount > frames, "a frame without the menu");
        Assert.Equal(0, harness.Read(w => ((MenuWindow)w.Root.Component).Copies));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void The_keyboard_focus_shows_a_tooltip_after_the_delay_and_escape_hides_it()
    {
        using var harness = new WindowHarness(typeof(MenuWindow), Options);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        harness.Key(0x09, down: true);
        harness.Key(0x09, down: false);
        harness.WaitUntil(w => w.Input.Focused?.Id == "save", "the focus on the button");
        harness.WaitUntil(w => HasOverlay(w, "Tooltip"), "the tooltip after the delay");
        harness.Key(0x1B, down: true);
        harness.Key(0x1B, down: false);
        harness.WaitUntil(w => !HasOverlay(w, "Tooltip"), "the tooltip hidden by Esc");
        Assert.Equal("save", harness.Read(w => w.Input.Focused?.Id));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }
}
