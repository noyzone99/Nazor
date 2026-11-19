using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nazor.Platform;

namespace Nazor;

/// <summary>
/// A built Nazor application: the host with its services and a UI loop, with an initial window or desktop-only
/// startup. Created by <see cref="NazorApplicationBuilder.Build" />.
/// </summary>
public sealed class NazorApp : IHost, IAsyncDisposable
{
    private readonly IHost host;
    private readonly ApplicationRuntimeSettings options;
    private readonly Type? rootComponent;
    private int running;

    internal NazorApp(IHost host, ApplicationRuntimeSettings options, Type? rootComponent)
    {
        this.host = host;
        this.options = options;
        this.rootComponent = rootComponent;
    }

    /// <summary>Gets the application's built service provider.</summary>
    public IServiceProvider Services => host.Services;

    /// <summary>Gets the configuration supplied by the Generic Host.</summary>
    public IConfiguration Configuration => host.Services.GetRequiredService<IConfiguration>();

    /// <summary>Gets the Generic Host environment and application identity.</summary>
    public IHostEnvironment Environment => host.Services.GetRequiredService<IHostEnvironment>();

    /// <summary>Gets the lifetime notifications and shutdown control of the Generic Host.</summary>
    public IHostApplicationLifetime Lifetime => host.Services.GetRequiredService<IHostApplicationLifetime>();

    /// <summary>Gets the application logger.</summary>
    public ILogger Logger => host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Environment.ApplicationName ?? "Nazor");

    /// <summary>Live application appearance, shared with IApplicationAppearance in dependency injection.</summary>
    public IApplicationAppearance Appearance => host.Services.GetRequiredService<IApplicationAppearance>();

    /// <summary>Starts the configured backend and explicit startup mode. Call once; blocks until shutdown.</summary>
    /// <returns>The native event loop exit code.</returns>
    public int Run()
    {
        BeginRun();
        return (options.PlatformHost ?? throw NazorApplication.MissingPlatformHost()).Run(RunApplication);
    }

    /// <summary>Starts the configured backend without blocking the caller and waits for shutdown and service cleanup.</summary>
    /// <param name="cancellationToken">Requests application shutdown. A token canceled before startup prevents the run.</param>
    /// <returns>The native event loop exit code after shutdown. Cancellation during the event loop requests a normal shutdown.</returns>
    /// <remarks>Run and RunAsync share a single-use lifetime. The backend supplies the UI-thread context.
    /// Await this method before disposing the application. StartAsync alone starts only Generic Host services.</remarks>
    /// <exception cref="OperationCanceledException">The token is already canceled or host startup is canceled.</exception>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginRun();
        using var cancellation = cancellationToken.Register(Lifetime.StopApplication);
        return await (options.PlatformHost ?? throw NazorApplication.MissingPlatformHost()).RunAsync(RunApplication).ConfigureAwait(false);
    }

    private void BeginRun()
    {
        if (Interlocked.Exchange(ref running, 1) != 0) throw new InvalidOperationException("An application can only run once.");
    }

    private int RunApplication(IPlatform platform)
    {
        if (options.SingleView && platform is not ISingleViewPlatform)
            throw new PlatformNotSupportedException("UseMainView requires a single-view backend. The selected desktop backend supports windows; use UseMainWindow or CreateMainWindow.");
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        try
        {
            if (options.Culture is not null) CultureInfo.CurrentCulture = options.Culture;
            if (options.UICulture is not null) CultureInfo.CurrentUICulture = options.UICulture;
            host.StartAsync().GetAwaiter().GetResult();
            var windowOptions = options with { Services = host.Services, Appearance = host.Services.GetRequiredService<ApplicationAppearance>() };
            return NazorApplication.RunApplication(platform, rootComponent, windowOptions, () =>
            {
                return Lifetime.ApplicationStopping.Register(() =>
                    host.Services.GetRequiredService<IApplicationLifetime>().Shutdown());
            });
        }
        finally
        {
            try { host.StopAsync().GetAwaiter().GetResult(); }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }
    }

    /// <summary>Starts the application's Generic Host services.</summary>
    /// <param name="cancellationToken">A token used to request cancellation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default) => host.StartAsync(cancellationToken);

    /// <summary>Stops the application's Generic Host services.</summary>
    /// <param name="cancellationToken">A token used to request cancellation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default) => host.StopAsync(cancellationToken);

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose() => host.Dispose();

    /// <summary>Asynchronously releases the resources owned by this instance.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public ValueTask DisposeAsync() => host is IAsyncDisposable disposable ? disposable.DisposeAsync() : ValueTask.CompletedTask;
}
