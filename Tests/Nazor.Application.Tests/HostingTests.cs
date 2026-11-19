using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nazor.Controls;
using Nazor.DevTools;
using Nazor.Rendering;

namespace Nazor.Application.Tests;

public interface IGreeter
{
    string Greet(string name);
}

public sealed class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello, {name}!";
}

public sealed class HostProbe
{
    public TaskCompletionSource<bool> Rendered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string? Greeting { get; set; }
    public ApartmentState Apartment { get; set; }
    public int ThreadId { get; set; }
    public ScopedResource? Resource { get; set; }
    public bool ThrowAfterRender { get; set; }
    public List<string> Events { get; } = [];
}

public sealed class ScopedResource : IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose() => Disposed = true;
}

public sealed class RecordingService(HostProbe probe) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (probe.Events) probe.Events.Add("start");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (probe.Events) probe.Events.Add("stop");
        return Task.CompletedTask;
    }
}

public sealed class HostedComponent : ComponentBase
{
    [Inject] public IGreeter Greeter { get; set; } = null!;
    [Inject] public HostProbe Probe { get; set; } = null!;
    [Inject] public ScopedResource Resource { get; set; } = null!;
    [Inject] public IConfiguration Configuration { get; set; } = null!;
    [Inject] public ILogger<HostedComponent> Logger { get; set; } = null!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TextBlock>(0, typeof(HostedComponent));
        builder.AddAttribute(1, "ChildContent", (RenderFragment)(c => c.AddContent(2, Greeter.Greet(Configuration["Name"] ?? "?"))));
        builder.CloseComponent();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        Probe.Greeting = Greeter.Greet(Configuration["Name"] ?? "?");
        Probe.Apartment = Thread.CurrentThread.GetApartmentState();
        Probe.ThreadId = Environment.CurrentManagedThreadId;
        Probe.Resource = Resource;
        Logger.LogInformation("Rendered for {Name}", Configuration["Name"]);
        Probe.Rendered.TrySetResult(true);
        if (Probe.ThrowAfterRender) throw new InvalidOperationException("Failure after render (test).");
    }
}

public sealed class ListLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(string Category, LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new ListLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class ListLogger(ListLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            provider.Entries.Enqueue((category, logLevel, formatter(state, exception), exception));
    }
}

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class HostingTests
{
    private static (NazorApplicationBuilder Builder, HostProbe Probe, ListLoggerProvider Logs) CreateBuilder(params string[] args)
    {
        var builder = WindowsApplication.CreateBuilder(args);
        var probe = new HostProbe();
        var logs = new ListLoggerProvider();
        builder.Services.AddSingleton<IGreeter, Greeter>();
        builder.Services.AddSingleton(probe);
        builder.Services.AddScoped<ScopedResource>();
        builder.Services.AddHostedService<RecordingService>();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.Application.CreateMainWindow<HostedComponent>(window => { window.Width = 320; window.Height = 200; window.Title = "Hosted"; window.Activate = false; });
        builder.Diagnostics.DevChannel = false;
        return (builder, probe, logs);
    }

    [Fact]
    public void The_builder_exposes_services_configuration_and_environment_before_run()
    {
        var (builder, _, _) = CreateBuilder("--Name=Zoë", "--environment=Staging");

        using var app = builder.Build();

        Assert.Equal("Hello, Zoë!", app.Services.GetRequiredService<IGreeter>().Greet(app.Configuration["Name"]!));
        Assert.Equal("Staging", app.Environment.EnvironmentName);
        Assert.Equal(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), app.Environment.ContentRootPath.TrimEnd(Path.DirectorySeparatorChar));
        Assert.Same(app.Appearance, app.Services.GetRequiredService<IApplicationAppearance>());
        Assert.IsAssignableFrom<IHostApplicationBuilder>(builder);
    }

    [Fact]
    public void Builder_DevTools_configuration_is_captured_and_attaches_to_main_and_later_windows()
    {
        var (builder, _, _) = CreateBuilder();
        var directory = Path.Combine(Path.GetTempPath(), "nazor-builder-" + Guid.NewGuid().ToString("N"));
        var configured = 0;
        builder.Diagnostics.ConfigureWindow = window =>
        {
            window.Dispatcher.AssertAccess();
            Assert.NotNull(window.PlatformWindow);
            Assert.True(window.Scene.Viewport.Width > 0);
            configured++;
        };
        Assert.Same(builder, builder.UseDevTools(directory));
        using var app = builder.Build();
        builder.Diagnostics.ConfigureWindow = _ => throw new InvalidOperationException("Configuration changed after Build.");
        using var harness = new ApplicationHarness(app.Run);
        DevToolsAgent? mainAgent = null;
        try
        {
            var main = harness.Window<HostedComponent>();
            mainAgent = ApplicationHarness.On(main, () => main.GetDevTools());
            Assert.NotNull(mainAgent);
            Assert.True(File.Exists(mainAgent.DiscoveryFile));
            var secondary = ApplicationHarness.On(main, () => (NazorWindow)app.Services.GetRequiredService<IWindowService>()
                .Open<PortableWindowPage>(options: new WindowOpenOptions { Activate = false }));
            var secondaryAgent = ApplicationHarness.On(secondary, () => secondary.GetDevTools());
            Assert.NotNull(secondaryAgent);
            Assert.NotEqual(mainAgent.Descriptor.Identity.SessionId, secondaryAgent.Descriptor.Identity.SessionId);
            Assert.Equal(2, ApplicationHarness.On(main, () => configured));
            using var closed = new ManualResetEventSlim();
            ApplicationHarness.On(secondary, () => { secondary.Closed += closed.Set; secondary.Close(); return true; });
            Assert.True(closed.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(secondaryAgent.IsDisposed);
            Assert.False(File.Exists(secondaryAgent.DiscoveryFile));
            Assert.False(mainAgent.IsDisposed);
        }
        finally
        {
            app.Lifetime.StopApplication();
            Assert.True(harness.WaitForExit());
        }
        Assert.True(mainAgent.IsDisposed);
        Assert.False(File.Exists(mainAgent.DiscoveryFile));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Run_shows_the_window_with_injected_services_on_an_STA_thread_and_stops_the_host(bool asynchronous, bool cancel)
    {
        var (builder, probe, logs) = CreateBuilder("--Name=Nazor");
        builder.Platform.Host = new DesktopPlatformHost();
        using var app = builder.Build();
        using var cancellation = new CancellationTokenSource();
        var caller = Environment.CurrentManagedThreadId;

        var run = asynchronous ? app.RunAsync(cancellation.Token) : Task.Run(() => app.Run(), TestContext.Current.CancellationToken);
        try
        {
            await probe.Rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.False(run.IsCompleted);
            Assert.Equal("Hello, Nazor!", probe.Greeting);
            Assert.Equal(ApartmentState.STA, probe.Apartment);
            Assert.NotEqual(caller, probe.ThreadId);
            Assert.False(probe.Resource!.Disposed);
            lock (probe.Events) Assert.Equal(["start"], probe.Events);
            Assert.Throws<InvalidOperationException>(() => app.Run());
            await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            if (cancel) cancellation.Cancel();
            else app.Lifetime.StopApplication();
            await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        var exitCode = await run;
        Assert.Equal(0, exitCode);
        Assert.True(probe.Resource.Disposed);
        lock (probe.Events) Assert.Equal(["start", "stop"], probe.Events);
        Assert.Contains(logs.Entries, e => e.Category == typeof(HostedComponent).FullName && e.Message == "Rendered for Nazor");
    }

    [Fact]
    public async Task Async_cancellation_before_the_UI_loop_is_not_lost()
    {
        using var cancellation = new CancellationTokenSource();
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Diagnostics.DevChannel = false;
        builder.Services.AddSingleton(new TrayStartupAction(cancellation.Cancel));
        builder.Platform.Host = new TrayTestHost(new TrayTestDesktop());
        builder.Application.UseTrayItem<StartupCallbackTray>();
        await using var app = builder.Build();

        Assert.Equal(0, await app.RunAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.True(app.Lifetime.ApplicationStopped.IsCancellationRequested);
    }

    [Fact]
    public async Task Async_startup_exceptions_propagate_after_host_cleanup()
    {
        var builder = WindowsApplication.CreateBuilder();
        var probe = new HostProbe();
        builder.Services.AddSingleton(probe);
        builder.Services.AddHostedService<RecordingService>();
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Diagnostics.DevChannel = false;
        var expected = new InvalidOperationException("Startup failed.");
        builder.Services.AddSingleton(new TrayStartupAction(() => throw expected));
        builder.Platform.Host = new TrayTestHost(new TrayTestDesktop());
        builder.Application.UseTrayItem<StartupCallbackTray>();
        await using var app = builder.Build();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        lock (probe.Events) Assert.Equal(["start", "stop"], probe.Events);
    }

    [Fact]
    public async Task Async_cancellation_during_host_startup_cancels_startup_and_stops_services()
    {
        var builder = WindowsApplication.CreateBuilder();
        var service = new WaitingStartupService();
        builder.Services.AddSingleton<IHostedService>(service);
        builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        builder.Services.AddSingleton(new TrayStartupAction(() => throw new InvalidOperationException("Canceled startup must not create the UI.")));
        builder.Platform.Host = new TrayTestHost(new TrayTestDesktop());
        builder.Application.UseTrayItem<StartupCallbackTray>();
        await using var app = builder.Build();
        using var cancellation = new CancellationTokenSource();

        var run = app.RunAsync(cancellation.Token);
        try
        {
            await service.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.True(service.Stopped);
    }

    private sealed class WaitingStartupService : IHostedService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Stopped { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Started.SetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Component_exceptions_go_to_the_host_logging()
    {
        var (builder, probe, logs) = CreateBuilder();
        probe.ThrowAfterRender = true;
        using var app = builder.Build();
        var run = Task.Run(() => app.Run(), TestContext.Current.CancellationToken);
        await probe.Rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!logs.Entries.Any(e => e.Category == "Nazor" && e.Level == LogLevel.Error) && DateTime.UtcNow < deadline)
            await Task.Delay(20, TestContext.Current.CancellationToken);
        app.Lifetime.StopApplication();
        await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var error = Assert.Single(logs.Entries, e => e.Category == "Nazor" && e.Level == LogLevel.Error);
        Assert.Equal("Failure after render (test).", error.Exception?.Message);
    }
}

internal sealed record TrayStartupAction(Action Callback);
internal sealed class StartupCallbackTray(TrayStartupAction startup) : TrayItem
{
    protected override void OnStarted() => startup.Callback();
}
