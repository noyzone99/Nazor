using Microsoft.Extensions.DependencyInjection;
using Nazor.Controls;
using Nazor.Platform;

namespace Nazor.Application.Tests;

public sealed class TrayApiTests
{
    private sealed class Resource : IDisposable { public bool Disposed { get; private set; } public void Dispose() => Disposed = true; }
    private sealed class FirstTray(Resource resource) : TrayItem
    {
        public Resource Resource => resource;
        public List<string> Calls { get; } = [];
        protected override void OnStarted() => Calls.Add("started");
        protected override void OnStopping() => Calls.Add("stopping");
        protected override void OnStopped() => Calls.Add("stopped");
        protected override void Dispose(bool disposing) => Calls.Add("disposed");
    }
    private sealed class SecondTray(Resource resource) : TrayItem { public Resource Resource => resource; }
    private sealed class MissingTray : TrayItem { }
    private sealed class FailingTray : TrayItem { protected override void OnStarted() => throw new InvalidOperationException("startup failure"); }

    private static ServiceProvider Services() => new ServiceCollection().AddNazorServices()
        .AddScoped<Resource>().AddScoped<FirstTray>().AddScoped<SecondTray>().AddScoped<FailingTray>().BuildServiceProvider();

    [Fact]
    public void Items_have_independent_native_registrations_scopes_and_a_terminal_lifetime()
    {
        using var services = Services();
        var desktop = services.GetRequiredService<DesktopServices>();
        Assert.Throws<InvalidOperationException>(() => desktop.Get<FirstTray>());
        var native = new TrayTestDesktop();
        var dispatcher = new UIDispatcher();
        desktop.Start(native, dispatcher);
        desktop.StartItems([typeof(FirstTray), typeof(SecondTray)], services);
        var first = desktop.Get<FirstTray>();
        var second = desktop.Get<SecondTray>();
        Assert.Same(first, desktop.Get<FirstTray>());
        Assert.NotSame(first.Resource, second.Resource);
        Assert.Equal(2, native.Items.Count);
        Assert.All(native.Items, item => Assert.True(item.IsVisible));
        Assert.Throws<InvalidOperationException>(() => desktop.Get<MissingTray>());
        var stopped = 0;
        first.Stopped += () => stopped++;
        first.Hide();
        Assert.False(first.IsVisible);
        Assert.True(second.IsVisible);
        Assert.True(first.IsStarted);
        Assert.True(first.Show());
        Assert.Throws<InvalidOperationException>(() => ((ITrayItem)first).Start(new TrayItemContext(null, dispatcher)));
        desktop.Stop();
        desktop.Stop();
        Assert.Equal(["started", "stopping", "stopped", "disposed"], first.Calls);
        Assert.Equal(1, stopped);
        Assert.True(first.Resource.Disposed);
        Assert.True(second.Resource.Disposed);
        Assert.All(native.Items, item => Assert.True(item.Disposed));
        Assert.False(first.IsSupported);
        Assert.Throws<ObjectDisposedException>(() => first.Show());
        Assert.Throws<InvalidOperationException>(() => desktop.Get<FirstTray>());
    }

    [Fact]
    public void Typed_events_request_values_without_mutating_state_and_stale_or_disabled_commands_are_ignored()
    {
        using var services = Services();
        var desktop = services.GetRequiredService<DesktopServices>();
        desktop.Start(new TrayTestDesktop(), new UIDispatcher());
        // Use a direct context so the prepared native IDs can be inspected.
        var shell = new TrayTestDesktop();
        var native = (TrayTestNative)shell.CreateTrayItem();
        using var item = new MissingTray();
        var requests = new List<string>();
        var command = new SystemTrayCommand("open", "Open");
        command.Invoked += () => requests.Add("open");
        var checkbox = new SystemTrayCheckBox("pause", "Pause", false);
        checkbox.CheckedChangeRequested += value => requests.Add("pause:" + value);
        var radio = new SystemTrayRadioGroup<DayOfWeek>(DayOfWeek.Monday, [new(DayOfWeek.Monday, "Monday"), new(DayOfWeek.Tuesday, "Tuesday")]);
        radio.SelectionChangeRequested += value => requests.Add(value.ToString());
        var disabled = new SystemTrayCommand("disabled", "Disabled") { Enabled = false };
        disabled.Invoked += () => requests.Add("invalid");
        var hidden = new SystemTrayCommand("hidden", "Hidden") { Visible = false };
        hidden.Invoked += () => requests.Add("invalid");
        var original = new List<SystemTrayMenuItem> { command, checkbox, new SystemTraySubmenu("Days", new([radio])), disabled, hidden };
        item.Menu = new(original);
        original.Clear();
        ((ITrayItem)item).Start(new(native, new UIDispatcher()));
        var entries = native.Options!.Menu;
        native.Invoke(entries[0].Id);
        native.Invoke(entries[1].Id);
        native.Invoke(entries[2].Children[1].Id);
        native.Invoke(entries[3].Id);
        native.Invoke(entries[4].Id);
        Assert.Equal(["open", "pause:True", "Tuesday"], requests);
        Assert.False(checkbox.IsChecked);
        Assert.Equal(DayOfWeek.Monday, radio.SelectedValue);
        item.Menu = new([new SystemTrayCommand("replacement", "Replacement")]);
        native.Invoke(entries[0].Id);
        Assert.Equal(3, requests.Count);
        var current = native.Options.Menu[0].Id;
        item.Hide();
        native.Invoke(current);
        Assert.Equal(3, requests.Count);
        desktop.Stop();
    }

    [Fact]
    public void Menu_opening_is_synchronous_sticky_and_separate_from_primary_and_secondary_activation()
    {
        var shell = new TrayTestDesktop();
        var native = (TrayTestNative)shell.CreateTrayItem();
        using var item = new MissingTray();
        ((ITrayItem)item).Start(new(native, new UIDispatcher()));
        var activations = 0;
        var secondary = 0;
        var handlers = 0;
        item.Activated += () => activations++;
        item.SecondaryActivated += () => secondary++;
        item.MenuOpening += (_, args) => { handlers++; Assert.True(args.TryCancel()); };
        item.MenuOpening += (_, args) => { handlers++; Assert.True(args.IsCanceled); };
        native.Activate();
        native.ActivateSecondary();
        Assert.True(native.OpenMenu(true).IsCanceled);
        Assert.Equal((1, 1, 2), (activations, secondary, handlers));
        var unsupported = new TrayMenuOpeningEventArgs(false);
        Assert.False(unsupported.TryCancel());
        Assert.False(unsupported.IsCanceled);
    }

    [Fact]
    public void Menu_validation_rejects_ambiguous_ids_and_invalid_radio_values_before_replacing_the_menu()
    {
        Assert.Throws<ArgumentException>(() => new SystemTrayRadioGroup<int>(1, [new(1, "One"), new(1, "Duplicate")]));
        Assert.Throws<ArgumentException>(() => new SystemTrayRadioGroup<int>(2, [new(1, "One")]));
        Assert.Throws<ArgumentException>(() => new SystemTrayCommand("id", "bad\0text"));
        var native = (TrayTestNative)new TrayTestDesktop().CreateTrayItem();
        using var item = new MissingTray();
        ((ITrayItem)item).Start(new(native, new UIDispatcher()));
        var before = item.Menu;
        Assert.Throws<ArgumentException>(() => item.Menu = new(
        [
            new SystemTrayCommand("same", "One"),
            new SystemTraySubmenu("Nested", new([new SystemTrayCheckBox("same", "Two", true)]))
        ]));
        Assert.Same(before, item.Menu);
    }

    [Fact]
    public void Unsupported_desktops_still_run_the_startup_hook_and_cleanup_failed_startup()
    {
        using var services = Services();
        var desktop = services.GetRequiredService<DesktopServices>();
        desktop.Start(null, new UIDispatcher());
        desktop.StartItems([typeof(FirstTray)], services);
        var first = desktop.Get<FirstTray>();
        Assert.False(first.IsSupported);
        Assert.False(first.Show());
        Assert.Equal(["started"], first.Calls);
        desktop.Stop();
        Assert.True(first.Resource.Disposed);

        var shell = new TrayTestDesktop();
        desktop.Start(shell, new UIDispatcher());
        Assert.Throws<InvalidOperationException>(() => desktop.StartItems([typeof(FirstTray), typeof(FailingTray)], services));
        var started = desktop.Get<FirstTray>();
        desktop.Stop();
        Assert.True(started.Resource.Disposed);
        Assert.All(shell.Items, native => Assert.True(native.Disposed));
    }

    [Fact]
    public void Images_are_prepared_for_native_menus_and_late_loads_cannot_resurrect_hidden_items()
    {
        using var bitmap = new SkiaSharp.SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SkiaSharp.SKColors.Red);
        using var encoded = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        var source = ImageSource.FromBytes(encoded.ToArray());
        var native = (TrayTestNative)new TrayTestDesktop().CreateTrayItem();
        var dispatcher = new UIDispatcher();
        using var item = new MissingTray { Icon = source, Menu = new([new SystemTrayCommand("open", "Open") { Icon = source }]) };
        ((ITrayItem)item).Start(new(native, dispatcher));
        Assert.True(dispatcher.RunUntil(() => native.Options!.Icon is not null && native.Options.Menu[0].Icon is not null, TimeSpan.FromSeconds(5)));
        var original = native.Options!.Icon;
        Assert.Equal(32, original!.Width);
        Assert.Equal(16, native.Options.Menu[0].Icon!.Width);
        item.ToolTip = "Updated without replacing the icon";
        Assert.Same(original, native.Options.Icon);

        var pending = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken loading = default;
        item.Icon = ImageSource.FromStream(token => { loading = token; return pending.Task; }, "pending-tray-test");
        Assert.False(loading.IsCancellationRequested);
        item.Hide();
        Assert.True(loading.IsCancellationRequested);
        pending.SetResult(new MemoryStream(encoded.ToArray()));
        dispatcher.RunPending();
        Assert.False(item.IsVisible);
        Assert.False(native.IsVisible);
    }

    [Fact]
    public void Wrong_thread_operations_do_not_dispose_a_live_item()
    {
        var native = (TrayTestNative)new TrayTestDesktop().CreateTrayItem();
        using var item = new MissingTray();
        ((ITrayItem)item).Start(new(native, new UIDispatcher()));
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Assert.Throws<InvalidOperationException>(() => item.Hide());
                Assert.Throws<InvalidOperationException>(() => item.Menu = SystemTrayMenu.Empty);
                Assert.Throws<InvalidOperationException>(() => item.Dispose());
            }
            catch (Exception error) { failure = error; }
        });
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
        Assert.Null(failure);
        Assert.True(item.IsStarted);
        Assert.True(item.Show());
        Assert.False(native.Disposed);
    }

    [Fact]
    public void Builder_validates_composable_startup_registration_and_shutdown_mode()
    {
        var builder = NazorApplication.CreateBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        builder.Application.UseTrayItem<MissingTray>();
        Assert.Throws<InvalidOperationException>(() => builder.Application.UseTrayItem<MissingTray>());
        Assert.Throws<ArgumentException>(() => builder.Application.UseTrayItem<TrayItem>());
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        using var app = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Application.UseTrayItem<SecondTray>());

        var mixed = NazorApplication.CreateBuilder();
        mixed.Application.UseTrayItem<MissingTray>();
        mixed.Application.CreateMainWindow<TextBlock>();
        using var mixedApp = mixed.Build();

        var singleView = NazorApplication.CreateBuilder();
        singleView.Application.UseMainView<TextBlock>();
        singleView.Application.UseTrayItem<MissingTray>();
        Assert.Throws<InvalidOperationException>(() => singleView.Build());
    }

    private sealed class ImmediateHost(TrayTestDesktop desktop) : IPlatformHost
    {
        public int Run(Func<IPlatform, int> application) => application(new ImmediatePlatform(desktop));
        private sealed class ImmediatePlatform(TrayTestDesktop desktop) : IPlatform
        {
            public IPlatformDesktopIntegration DesktopIntegration => desktop;
            public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options) => throw new NotSupportedException();
            public int Run(UIDispatcher dispatcher) { dispatcher.RunPending(); return 0; }
            public void Quit(int exitCode = 0) { }
        }
    }

    [Fact]
    public void Tray_only_startup_without_an_accepted_icon_fails_after_releasing_its_scope()
    {
        var desktop = new TrayTestDesktop { SupportsTray = false };
        var builder = NazorApplication.CreateBuilder(platformHost: new ImmediateHost(desktop));
        var resource = new Resource();
        builder.Services.AddScoped(_ => resource);
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Application.UseTrayItem<FirstTray>();
        using var app = builder.Build();
        Assert.Contains("No tray icon was accepted", Assert.Throws<InvalidOperationException>(() => app.Run()).Message);
        Assert.True(resource.Disposed);
        Assert.True(Assert.Single(desktop.Items).Disposed);
    }

    [Fact]
    public void Notification_service_remains_singleton_and_unavailable_outside_Run()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        Assert.Same(services.GetRequiredService<ISystemTray>(), scope.ServiceProvider.GetRequiredService<ISystemTray>());
        var desktop = services.GetRequiredService<DesktopServices>();
        Assert.Throws<InvalidOperationException>(() => desktop.Show(new("Title", "Body")));
        desktop.Start(null, new UIDispatcher());
        Assert.False(desktop.Show(new("Title", "Body")));
        desktop.Withdraw();
        desktop.Stop();
    }
}
