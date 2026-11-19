using Microsoft.Extensions.DependencyInjection;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Samples.Windowing;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class TraySampleTests
{
    private static (NazorApp App, TrayTestHost Host) Build(TrayTestDesktop desktop, bool startInTray = true)
    {
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var host = new TrayTestHost(desktop);
        builder.Platform.Host = host;
        builder.Application.UseTrayItem<TraySession>();
        if (!startInTray) builder.Application.UseMainWindow<App>();
        return (builder.Build(), host);
    }

    private static void On(IApplicationLifetime lifetime, Action action) =>
        ApplicationHarness.Result(lifetime.Dispatcher.InvokeAsync(() => { action(); return true; }));

    private static void ReplaceText(NazorWindow window, string text)
    {
        window.Scene.Update();
        window.Input.Focus(window.Root.Single("TextFieldInput"), false);
        window.Input.KeyDown("a", KeyModifiers.Control, false);
        window.Input.TextInput(text);
    }

    private static ElementNode SendButton(NazorWindow window) =>
        window.Root.Elements("Button").Single(button => button.Text() == "Send notification");

    [Fact]
    public async Task Tray_startup_opens_one_interface_sends_edited_text_and_survives_closing_that_interface()
    {
        var desktop = new TrayTestDesktop();
        var built = Build(desktop);
        using var app = built.App;
        using var harness = new ApplicationHarness(app.Run);
        await built.Host.Ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var lifetime = app.Services.GetRequiredService<IApplicationLifetime>();
        var native = Assert.Single(desktop.Items);
        try
        {
            On(lifetime, () =>
            {
                Assert.Empty(NazorWindow.OpenWindows);
                Assert.Contains(native.Options!.Menu, item => item.Text == "Open window");
                native.Invoke(native.Options!.Menu.Single(item => item.Text == "Open window").Id);
            });
            var first = harness.Window<App>();
            On(lifetime, () =>
            {
                native.Activate();
                Assert.Same(first, Assert.Single(NazorWindow.OpenWindows));
                native.Invoke(native.Options!.Menu.Single(item => item.Text == "Pause notifications").Id);
                Assert.Equal(true, SendButton(first).Attributes["disabled"]);
                native.Invoke(native.Options!.Menu.Single(item => item.Text == "Pause notifications").Id);
                Assert.NotEqual(true, SendButton(first).Attributes.GetValueOrDefault("disabled"));
                var choices = native.Options.Menu.Single(item => item.Text == "Notification icon").Children;
                native.Invoke(choices.Single(item => item.Text == "Warning").Id);
                ReplaceText(first, "Message typed into the text box: città 日本語");
            });
            ApplicationHarness.Result(first.Dispatcher.InvokeAsync(async () =>
            {
                await first.Renderer.DispatchEventAsync(SendButton(first), "onclick", new PointerEventArgs());
                return true;
            }));
            Assert.Equal("Message typed into the text box: città 日本語", Assert.Single(desktop.Notifications).Message);
            Assert.Equal(SystemNotificationIcon.Warning, desktop.Notifications[0].Icon);
            On(lifetime, () =>
            {
                ReplaceText(first, " ");
                Assert.Equal(true, SendButton(first).Attributes["disabled"]);
                Assert.False(app.Services.GetRequiredService<ISystemTray>().Get<TraySession>().Notify());
                ReplaceText(first, new string('x', 300));
                Assert.Equal(255, app.Services.GetRequiredService<ISystemTray>().Get<TraySession>().Message.Length);
                first.Close();
            });
            Assert.True(SpinWait.SpinUntil(() => first.IsClosed, TimeSpan.FromSeconds(5)));
            Assert.False(harness.HasExited);
            On(lifetime, () => native.Invoke(native.Options!.Menu.Single(item => item.Text == "Open window").Id));
            var second = harness.Window<App>();
            Assert.NotSame(first, second);
            On(lifetime, () =>
            {
                Assert.Equal(new string('x', 255), second.Root.Single("TextFieldInput").Attributes["value"]);
                native.Invoke(native.Options!.Menu.Single(item => item.Text == "Exit").Id);
            });
            Assert.True(harness.WaitForExit());
            Assert.True(second.IsClosed);
            Assert.Equal(0, harness.ExitCode);
        }
        finally { if (!harness.HasExited) { lifetime.Shutdown(); harness.WaitForExit(); } }
    }

    [Fact]
    public async Task Windowing_startup_reuses_its_main_window_and_can_reopen_it_from_the_tray()
    {
        var desktop = new TrayTestDesktop();
        var built = Build(desktop, startInTray: false);
        using var app = built.App;
        using var harness = new ApplicationHarness(app.Run);
        await built.Host.Ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var lifetime = app.Services.GetRequiredService<IApplicationLifetime>();
        try
        {
            var first = harness.Window<App>();
            On(lifetime, () =>
            {
                desktop.Items.Single().Activate();
                Assert.Same(first, Assert.Single(NazorWindow.OpenWindows));
                first.Close();
            });
            Assert.True(SpinWait.SpinUntil(() => first.IsClosed, TimeSpan.FromSeconds(5)));
            Assert.False(harness.HasExited);
            On(lifetime, () => desktop.Items.Single().Activate());
            Assert.NotSame(first, harness.Window<App>());
        }
        finally { lifetime.Shutdown(); Assert.True(harness.WaitForExit()); }
    }

    [Fact]
    public async Task Unavailable_or_lost_tray_keeps_an_interface_as_an_exit_path()
    {
        foreach (var availableAtStartup in new[] { false, true })
        {
            var desktop = new TrayTestDesktop { SupportsTray = availableAtStartup };
            var built = Build(desktop);
            using var app = built.App;
            using var harness = new ApplicationHarness(app.Run);
            await built.Host.Ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            var lifetime = app.Services.GetRequiredService<IApplicationLifetime>();
            try
            {
                if (availableAtStartup) On(lifetime, () =>
                {
                    desktop.SupportsTray = false;
                    desktop.Items.Single().SetVisible(false);
                });
                var window = harness.Window<App>();
                On(lifetime, window.Close);
                Assert.True(harness.WaitForExit());
                Assert.Equal(0, harness.ExitCode);
            }
            finally { if (!harness.HasExited) { lifetime.Shutdown(); harness.WaitForExit(); } }
        }
    }

    [Fact]
    public async Task Tray_only_start_requires_explicit_shutdown_and_host_stop_works_without_windows()
    {
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.UseTrayItem<TraySession>();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        var built = Build(new TrayTestDesktop());
        using var app = built.App;
        using var harness = new ApplicationHarness(app.Run);
        await built.Host.Ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        app.Lifetime.StopApplication();
        Assert.True(harness.WaitForExit());
        Assert.Equal(0, harness.ExitCode);
    }
}
