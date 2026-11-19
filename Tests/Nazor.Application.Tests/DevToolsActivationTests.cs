using System.Runtime.InteropServices;
using System.Text;
using Nazor.DevTools;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Samples.Counter;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class DevToolsActivationTests
{
    [LibraryImport("user32.dll")] private static partial int GetMenuItemCount(nint menu);
    [LibraryImport("user32.dll")] private static partial uint GetMenuItemID(nint menu, int position);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetMenuStringW(nint menu, uint item, StringBuilder text, int maximum, uint flags);
    [LibraryImport("user32.dll")] private static partial nint SendMessageW(nint window, uint message, nint wParam, nint lParam);

    private static (uint Id, string Text)[] Items(nint window)
    {
        var menu = GetSystemMenu(window, false);
        return Enumerable.Range(0, GetMenuItemCount(menu)).Select(position =>
        {
            var text = new StringBuilder(256);
            GetMenuStringW(menu, (uint)position, text, text.Capacity, 0x400);
            return (GetMenuItemID(menu, position), text.ToString());
        }).ToArray();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_system_menu_registrations_dispatch_and_retire_without_replacing_standard_commands(bool custom)
    {
        using var harness = new WindowHarness(typeof(Counter));
        harness.Read(window =>
        {
            window.ApplyDecorations((custom) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.Nazor) : WindowDecorations.Native());
            var before = Items(harness.Handle);
            var calls = 0;
            var registration = window.PlatformWindow.AddSystemMenuCommand("Test native action", () => calls++);
            Assert.NotNull(registration);
            var item = Items(harness.Handle).Single(item => item.Text == "Test native action");
            Assert.InRange(item.Id, 1u, 0xEFFFu);
            SendMessageW(harness.Handle, WM_SYSCOMMAND, (nint)(item.Id | 3), 0);
            Assert.Equal(1, calls);
            registration.Dispose(); registration.Dispose();
            Assert.Equal(before, Items(harness.Handle));
            SendMessageW(harness.Handle, WM_SYSCOMMAND, (nint)item.Id, 0);
            Assert.Equal(1, calls);
            return true;
        });
    }

    [Fact]
    public void Defaults_add_one_menu_command_and_reconfiguration_releases_F12_to_application_bindings()
    {
        using var harness = new WindowHarness(typeof(Counter));
        harness.Read(window =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "nazor-menu-" + Guid.NewGuid().ToString("N"));
            var initial = Items(harness.Handle);
            window.EnableDiagnostics();
            Assert.Equal(initial, Items(harness.Handle));
            var agent = window.EnableDevTools(directory);
            Assert.Same(agent, window.EnableDevTools(directory));
            Assert.Single(Items(harness.Handle), item => item.Text == "Open Nazor DevTools\tF12");
            var host = (IWindowHost)window;
            Assert.True(host.OnKeyDown("F12", KeyModifiers.None, true));
            Assert.True(host.OnKeyUp("F12", KeyModifiers.None));
            var calls = 0;
            using var binding = window.Input.KeyBindings.Register("F12", KeyModifiers.None,
                EventCallback.Factory.Create<KeyboardEventArgs>(window, () => calls++), allowRepeat: true);
            var options = new DevToolsOptions { DiscoveryDirectory = directory, ShortcutKey = "D", ShortcutModifiers = KeyModifiers.Control | KeyModifiers.Shift };
            Assert.Same(agent, window.EnableDevTools(options));
            Assert.Single(Items(harness.Handle), item => item.Text == "Open Nazor DevTools\tCtrl+Shift+D");
            Assert.True(host.OnKeyDown("F12", KeyModifiers.None, false));
            Assert.Equal(1, calls);
            Assert.True(host.OnKeyDown("d", KeyModifiers.Control | KeyModifiers.Shift, true));
            Assert.True(host.OnKeyUp("D", KeyModifiers.None));
            window.EnableDevTools(options with { ShortcutKey = null });
            Assert.Single(Items(harness.Handle), item => item.Text == "Open Nazor DevTools");
            Assert.False(host.OnKeyDown("D", KeyModifiers.Control | KeyModifiers.Shift, false));
            window.EnableDevTools(options with { ShortcutKey = null, ShowInSystemMenu = false });
            Assert.Equal(initial, Items(harness.Handle));
            window.EnableDevTools(options);
            agent.Dispose();
            Assert.Equal(initial, Items(harness.Handle));
            var replacement = window.EnableDevTools(options);
            Assert.Single(Items(harness.Handle), item => item.Text == "Open Nazor DevTools\tCtrl+Shift+D");
            window.DisableDevTools();
            Assert.True(agent.IsDisposed);
            Assert.True(replacement.IsDisposed);
            Assert.Equal(initial, Items(harness.Handle));
            return true;
        });
    }

    [Fact]
    public void Attachment_during_component_initialization_waits_for_the_native_window_before_adding_its_menu()
    {
        using var harness = new WindowHarness(typeof(EarlyAttachment));
        harness.Read(window =>
        {
            Assert.NotNull(window.GetDevTools());
            Assert.Single(Items(harness.Handle), item => item.Text == "Open Nazor DevTools\tF12");
            window.DisableDevTools();
            Assert.DoesNotContain(Items(harness.Handle), item => item.Text.StartsWith("Open Nazor DevTools", StringComparison.Ordinal));
            return true;
        });
    }

    public sealed class EarlyAttachment : WindowComponentBase
    {
        protected override void OnInitialized() => ((NazorWindow)Window).EnableDevTools(Path.Combine(Path.GetTempPath(), "nazor-early-" + Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public void Invalid_options_leave_existing_diagnostics_and_menu_untouched()
    {
        using var harness = new WindowHarness(typeof(Counter));
        harness.Read(window =>
        {
            var before = Items(harness.Handle);
            Assert.Throws<ArgumentException>(() => window.EnableDevTools(new DevToolsOptions { ShortcutKey = "" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => window.EnableDevTools(new DevToolsOptions { ShortcutModifiers = (KeyModifiers)16 }));
            Assert.Null(window.GetDiagnostics());
            Assert.Equal(before, Items(harness.Handle));
            return true;
        });
    }
}
