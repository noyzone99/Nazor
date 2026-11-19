using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Nazor.Platform;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed unsafe partial class DesktopIntegrationTests
{
    private sealed record Call(uint Command, uint Id, uint Flags, string Tip, string Message);

    private sealed class Shell
    {
        public List<Call> Calls { get; } = [];
        public bool Available { get; set; } = true;
        public bool Version4 { get; set; } = true;
        public bool Notify(uint command, ref Win32DesktopIntegration.IconData data)
        {
            fixed (char* tip = data.Tip, message = data.Info)
                Calls.Add(new(command, data.Id, data.Flags, new string(tip), new string(message)));
            return Available && (command != 4 || Version4);
        }
    }

    private static void OnUi(Action action) => Nazor.Platform.Windows.WindowsPlatformHost.RunOnUiThread(() =>
    {
        using var platform = new Win32Platform();
        action();
        return 0;
    });

    private static void Callback(Win32DesktopIntegration desktop, uint id, uint kind) =>
        desktop.OnMessage(desktop.Handle, Win32DesktopIntegration.CallbackMessage, 0, (nint)((id << 16) | kind));

    [Fact]
    public void Shell_layout_is_the_Unicode_Windows_SDK_layout()
    {
        Assert.Equal(IntPtr.Size == 8 ? 976 : 956, sizeof(Win32DesktopIntegration.IconData));
        Assert.Equal(IntPtr.Size == 8 ? 40 : 24, Marshal.OffsetOf<Win32DesktopIntegration.IconData>(nameof(Win32DesktopIntegration.IconData.Tip)).ToInt32());
    }

    [Fact]
    public void Tray_add_update_hide_and_restart_preserve_only_the_requested_icon()
    {
        OnUi(() =>
        {
            var shell = new Shell();
            using var desktop = new Win32DesktopIntegration(shell.Notify);
            var changes = 0;
            var activations = 0;
            desktop.VisibilityChanged += () => changes++;
            desktop.Activated += () => activations++;
            Assert.True(desktop.Show(new() { ToolTip = "Prima", Menu = [new(1, NativeTrayMenuItemKind.Command, "Apri"), new(2, NativeTrayMenuItemKind.Separator), new(3, NativeTrayMenuItemKind.Command, "Esci")] }));
            Assert.True(desktop.IsVisible);
            Assert.Equal(new uint[] { 0, 4 }, shell.Calls.Select(c => c.Command));
            Assert.True(desktop.Show(new() { ToolTip = "Aggiornata" }));
            Assert.Equal(1u, shell.Calls[^1].Command);
            Callback(desktop, 1, 0x400);
            Callback(desktop, 1, 0x401);
            Assert.Equal(2, activations);
            shell.Calls.Clear();
            desktop.OnMessage(desktop.Handle, desktop.TaskbarCreated, 0, 0);
            Assert.Equal("Aggiornata", shell.Calls[0].Tip);
            Assert.Equal(1, changes);
            desktop.Hide();
            Assert.False(desktop.IsVisible);
            Assert.Equal(2, changes);
            shell.Calls.Clear();
            desktop.OnMessage(desktop.Handle, desktop.TaskbarCreated, 0, 0);
            Assert.Empty(shell.Calls);
        });
    }

    [Fact]
    public void Unsupported_shell_and_failed_version_negotiation_do_not_claim_visibility()
    {
        OnUi(() =>
        {
            var shell = new Shell { Available = false };
            using var desktop = new Win32DesktopIntegration(shell.Notify);
            Assert.False(desktop.Show(new()));
            Assert.False(desktop.IsVisible);
            Assert.False(desktop.ShowNotification(new("Titolo", "Messaggio")));
            shell.Available = true;
            shell.Version4 = false;
            Assert.False(desktop.Show(new()));
            Assert.Equal(2u, shell.Calls[^1].Command);
            Assert.False(desktop.IsVisible);
            shell.Version4 = true;
            Assert.True(desktop.Show(new()));
            shell.Available = false;
            desktop.OnMessage(desktop.Handle, desktop.TaskbarCreated, 0, 0);
            Assert.False(desktop.IsVisible);
            shell.Available = true;
            desktop.OnMessage(desktop.Handle, desktop.TaskbarCreated, 0, 0);
            Assert.True(desktop.IsVisible);
        });
    }

    [LibraryImport("user32.dll")] private static partial int GetMenuItemCount(nint menu);
    [LibraryImport("user32.dll")] private static partial uint GetMenuState(nint menu, uint item, uint flags);

    [Fact]
    public void Tray_menu_uses_native_separators_disabled_commands_and_a_snapshot_of_items()
    {
        OnUi(() =>
        {
            var shell = new Shell();
            uint selection = 4;
            using var desktop = new Win32DesktopIntegration(shell.Notify, (menu, x, y, owner) =>
            {
                Assert.Equal(-120, x);
                Assert.Equal(240, y);
                Assert.Equal(4, GetMenuItemCount(menu));
                Assert.NotEqual(0u, GetMenuState(menu, 1, 0x400) & 0x800);
                Assert.NotEqual(0u, GetMenuState(menu, 2, 0x400) & 3);
                return selection;
            });
            var items = new List<NativeTrayMenuItem> { new(1, NativeTrayMenuItemKind.Command, "Apri"), new(2, NativeTrayMenuItemKind.Separator), new(3, NativeTrayMenuItemKind.Command, "Disabilitata") { Enabled = false }, new(4, NativeTrayMenuItemKind.Command, "Esci") };
            Assert.True(desktop.Show(new() { Menu = items }));
            items.Clear();
            var commands = new List<int>();
            desktop.MenuItemInvoked += commands.Add;
            var position = (nint)((240 << 16) | unchecked((ushort)-120));
            desktop.OnMessage(desktop.Handle, Win32DesktopIntegration.CallbackMessage, position, (nint)((1 << 16) | 0x7B));
            Assert.Equal([4], commands);
            selection = 3;
            desktop.OnMessage(desktop.Handle, Win32DesktopIntegration.CallbackMessage, position, (nint)((1 << 16) | 0x7B));
            Assert.Equal([4], commands);
        });
    }

    [Fact]
    public void Notifications_have_their_own_icon_and_ignore_callbacks_from_replaced_banners()
    {
        OnUi(() =>
        {
            var shell = new Shell();
            using var desktop = new Win32DesktopIntegration(shell.Notify);
            Assert.True(desktop.ShowNotification(new("Prima", "Testo Unicode: città 日本語")));
            Assert.False(desktop.IsVisible);
            var first = shell.Calls[0].Id;
            Assert.NotEqual(1u, first);
            Assert.Equal("Testo Unicode: città 日本語", shell.Calls[0].Message);
            shell.Calls.Clear();
            Assert.True(desktop.ShowNotification(new("Seconda", "Aggiornamento")));
            Assert.Equal((2u, first), (shell.Calls[0].Command, shell.Calls[0].Id));
            var second = shell.Calls[1].Id;
            var activated = 0;
            desktop.NotificationActivated += () => activated++;
            shell.Calls.Clear();
            Callback(desktop, first, Win32DesktopIntegration.BalloonTimeout);
            Assert.Empty(shell.Calls);
            Callback(desktop, second, Win32DesktopIntegration.BalloonClick);
            Assert.Equal(1, activated);
            Assert.Equal((2u, second), (shell.Calls[^1].Command, shell.Calls[^1].Id));
            Assert.True(desktop.ShowNotification(new("Terza", "Da ritirare")));
            desktop.WithdrawNotification();
            Assert.Equal(2u, shell.Calls[^1].Command);
            shell.Calls.Clear();
            desktop.WithdrawNotification();
            desktop.OnMessage(desktop.Handle, desktop.TaskbarCreated, 0, 0);
            Assert.Empty(shell.Calls);
        });
    }

    [Fact]
    public void Invalid_native_inputs_are_rejected_before_calling_the_shell()
    {
        OnUi(() =>
        {
            var shell = new Shell();
            using var desktop = new Win32DesktopIntegration(shell.Notify);
            Assert.Throws<ArgumentException>(() => desktop.Show(new() { ToolTip = new string('x', 128) }));
            Assert.Throws<ArgumentException>(() => desktop.Show(new() { ToolTip = "bad\0tip" }));
            Assert.Throws<ArgumentException>(() => desktop.Show(new() { Menu = [new(1, NativeTrayMenuItemKind.Command, "Uno"), new(1, NativeTrayMenuItemKind.Command, "Due")] }));
            Assert.Throws<ArgumentException>(() => desktop.Show(new() { Icon = new Nazor.Graphics.RenderedImage(1, 1, []) }));
            Assert.Throws<ArgumentException>(() => desktop.ShowNotification(new(new string('x', 64), "test")));
            Assert.Throws<ArgumentException>(() => desktop.ShowNotification(new("test", new string('x', 256))));
            Assert.Throws<ArgumentException>(() => desktop.ShowNotification(new("test", "")));
            Assert.Throws<ArgumentOutOfRangeException>(() => desktop.ShowNotification(new("test", "test", (SystemNotificationIcon)42)));
            Assert.Empty(shell.Calls);
            desktop.Dispose();
            Assert.Throws<ObjectDisposedException>(() => desktop.Show(new()));
        });
    }

    [LibraryImport("user32.dll")] private static partial nint GetSubMenu(nint menu, int position);

    [Fact]
    public void Native_submenus_checkboxes_radio_icons_and_cancellation_are_independent_per_tray()
    {
        OnUi(() =>
        {
            var shell = new Shell();
            var tracks = 0;
            using var first = new Win32DesktopIntegration(shell.Notify, (menu, _, _, _) =>
            {
                tracks++;
                Assert.Equal(1, GetMenuItemCount(menu));
                var nested = GetSubMenu(menu, 0);
                Assert.NotEqual(0, nested);
                Assert.Equal(3, GetMenuItemCount(nested));
                Assert.NotEqual(0u, GetMenuState(nested, 0, 0x400) & 8);
                Assert.NotEqual(0u, GetMenuState(nested, 1, 0x400) & 8);
                return 12;
            });
            using var second = (Win32DesktopIntegration)first.CreateTrayItem();
            Assert.NotEqual(first.Handle, second.Handle);
            var pixel = new Nazor.Graphics.RenderedImage(1, 1, [0, 0, 255, 255]);
            Assert.True(first.Show(new() { Icon = pixel, Menu =
            [
                new(10, NativeTrayMenuItemKind.Submenu, "Settings") { Children =
                [
                    new(11, NativeTrayMenuItemKind.CheckBox, "Pause") { IsChecked = true, Icon = pixel },
                    new(12, NativeTrayMenuItemKind.Radio, "Work") { IsChecked = true },
                    new(13, NativeTrayMenuItemKind.Radio, "Hidden") { Visible = false },
                    new(14, NativeTrayMenuItemKind.Radio, "Home")
                ] }
            ] }));
            Assert.True(second.Show(new() { ToolTip = "Second" }));
            var selected = 0;
            first.MenuItemInvoked += id => selected = id;
            Callback(first, 1, 0x7B);
            Assert.Equal(12, selected);
            Assert.Equal(1, tracks);
            var notified = 0;
            first.MenuOpening += args => { Assert.True(args.TryCancel()); notified++; };
            first.MenuOpening += args => { Assert.True(args.IsCanceled); notified++; };
            Callback(first, 1, 0x7B);
            Assert.Equal(2, notified);
            Assert.Equal(1, tracks);
            var secondary = 0;
            first.SecondaryActivated += () => secondary++;
            Callback(first, 1, 0x208);
            Assert.Equal(1, secondary);
            first.Hide();
            Assert.True(second.IsVisible);
        });
    }

    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool IsWindowVisible(nint hwnd);

    [Fact]
    public void Explicit_shutdown_keeps_the_loop_alive_after_closing_the_main_scope_and_can_open_a_new_window()
    {
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.UseMainWindow<OrderBook>(new() { Activate = false });
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Services.AddScoped<ScopedResource>();
        using var app = builder.Build();
        using var harness = new ApplicationHarness(() => app.Run());
        var main = harness.Window<OrderBook>();
        var lifetime = app.Services.GetRequiredService<IApplicationLifetime>();
        try
        {
            var resource = ApplicationHarness.On(main, () =>
            {
                main.Hide();
                Assert.False(IsWindowVisible(main.PlatformWindow.Handle));
                Assert.False(main.IsClosed);
                main.Show(false);
                Assert.True(IsWindowVisible(main.PlatformWindow.Handle));
                var value = ((OrderBook)main.Root.Component).Services.GetRequiredService<ScopedResource>();
                main.Close();
                return value;
            });
            Assert.True(SpinWait.SpinUntil(() => resource.Disposed, TimeSpan.FromSeconds(5)));
            Assert.False(harness.HasExited);
            var reopened = ApplicationHarness.Result(lifetime.Dispatcher.InvokeAsync(() =>
            {
                using var scope = app.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IWindowService>().Open<OrderDetail>();
            }));
            Assert.False(reopened.IsClosed);
            lifetime.Shutdown(7);
            Assert.True(harness.WaitForExit());
            Assert.Equal(7, harness.ExitCode);
            Assert.True(reopened.IsClosed);
        }
        finally { if (!harness.HasExited) { lifetime.Shutdown(); harness.WaitForExit(); } }
    }

    [Fact]
    public void Host_stop_also_exits_an_explicit_application_with_zero_windows()
    {
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.UseMainWindow<OrderBook>(new() { Activate = false });
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        using var app = builder.Build();
        using var harness = new ApplicationHarness(() => app.Run());
        var main = harness.Window<OrderBook>();
        try
        {
            ApplicationHarness.On(main, () => { main.Close(); return true; });
            Assert.True(SpinWait.SpinUntil(() => main.IsClosed, TimeSpan.FromSeconds(5)));
            Assert.False(harness.HasExited);
            app.Lifetime.StopApplication();
            Assert.True(harness.WaitForExit());
            Assert.Equal(0, harness.ExitCode);
        }
        finally { if (!harness.HasExited) { app.Lifetime.StopApplication(); harness.WaitForExit(); } }
    }
}
