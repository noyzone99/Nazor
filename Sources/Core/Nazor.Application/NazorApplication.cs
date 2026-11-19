using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nazor.Development;
using Nazor.Platform;
using Nazor.Style;

namespace Nazor;

/// <summary>Provides platform-independent application startup using the configured platform host.</summary>
public static class NazorApplication
{
    internal static Func<IPlatformHost>? PlatformHostFactory;

    /// <summary>
    /// Registers the process-wide default host factory used by subsequent builders.
    /// The SDK calls this from the executable's generated bootstrap before application startup.
    /// </summary>
    /// <param name="factory">Creates a platform host for each builder. Must not return null.</param>
    /// <remarks>Registration replaces the previous default. An explicit platformHost passed to CreateBuilder takes precedence.</remarks>
    /// <exception cref="ArgumentNullException">factory is null.</exception>
    public static void RegisterPlatformHost(Func<IPlatformHost> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Volatile.Write(ref PlatformHostFactory, factory);
    }

    /// <summary>
    /// Creates a builder for an application with a host: dependency injection, configuration (<c>appsettings.json</c>,
    /// environment variables, command line) and logging, as in <c>WebApplication.CreateBuilder</c>. Then
    /// <see cref="NazorApplicationBuilder.Build" /> and <see cref="NazorApp.Run" />.
    /// </summary>
    /// <param name="args">The command-line arguments supplied to the application.</param>
    /// <param name="platformHost">Optional explicit host, bypassing the registered default factory.</param>
    /// <returns>The application builder configured with the registered default platform host, when available.</returns>
    /// <remarks>The SDK registers the desktop host automatically; it selects a native backend when run. Without a registration, set the builder's
    /// Platform.Host before Build.</remarks>
    /// <exception cref="InvalidOperationException">The registered host factory returned null.</exception>
    public static NazorApplicationBuilder CreateBuilder(string[]? args = null, IPlatformHost? platformHost = null)
    {
        var builder = new NazorApplicationBuilder(args);
        builder.Platform.Host = platformHost ?? CreateDefaultPlatformHost();
        return builder;
    }

    private static IPlatformHost? CreateDefaultPlatformHost()
    {
        var factory = Volatile.Read(ref PlatformHostFactory);
        return factory is null ? null : factory() ?? throw new InvalidOperationException("The registered Nazor platform host factory returned null.");
    }

    internal static InvalidOperationException MissingPlatformHost() => new(
        "No Nazor platform host is configured. Build a desktop executable with Nazor.Sdk targeting net10.0, " +
        "set Platform.Host explicitly, or use WindowsApplication for Windows startup.");

    internal static int RunApplication(IPlatform platform, Type? rootComponent, ApplicationRuntimeSettings options, Func<IDisposable?>? onStarted)
    {
        var requestedRenderer = options.Rendering ?? RendererSelection.ReadEnvironment();
        options = options with { Rendering = requestedRenderer };
        var logger = (options.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger("Nazor");
        StartupLog.Platform(logger, platform.BackendName);
        var dispatcher = new UIDispatcher();

        var devChannel = IsDevChannelEnabled(options);
        if (devChannel) StyleRegistry.TrackingEnabled = true;
        var windows = options.Services?.GetService(typeof(WindowManager)) as WindowManager ?? new WindowManager(new ApplicationAppearance());
        windows.Start(platform, dispatcher, options);
        var desktop = options.Services?.GetService(typeof(DesktopServices)) as DesktopServices ?? new DesktopServices();
        try
        {
            desktop.Start(platform.DesktopIntegration, dispatcher);
            var window = rootComponent is null ? null : windows.OpenMain(rootComponent, options);
            using var channel = devChannel ? StartDevChannel(dispatcher, options) : null;
            window?.Show(options.Activate);
            using var subscription = onStarted?.Invoke();
            if (options.Services is not null) desktop.StartItems(options.TrayItems, options.Services);
            if (!windows.ShutdownRequested && rootComponent is null && options.TrayItems.Count != 0 && windows.Windows.Count == 0 && !options.TrayItems.Any(type => desktop.IsItemVisible(type)))
                throw new InvalidOperationException("No tray icon was accepted. Open a fallback window from OnStarted or provide a working tray host.");
            if (window is null && windows.Windows.Count == 0 && !windows.ShutdownRequested)
                StartupLog.RendererDeferred(logger, requestedRenderer);
            return platform.Run(dispatcher);
        }
        finally
        {
            try { desktop.Stop(); }
            finally { windows.DisposeAll(); }
        }
    }

    internal static bool IsDevChannelEnabled(ApplicationRuntimeSettings options) =>
        options.DevChannel ?? (Environment.GetEnvironmentVariable(DevChannelProtocol.EnableVariable) is { } value &&
            (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)));

    private static DevChannelServer? StartDevChannel(UIDispatcher dispatcher, ApplicationRuntimeSettings options)
    {
        var logger = (options.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger(DevChannelServer.LoggerCategory);
        var server = new DevChannelServer(dispatcher, () => NazorWindow.OpenWindows, logger);
        try
        {
            server.Start();
            return server;
        }
        catch (Exception exception)
        {
            if (logger is not null) logger.LogWarning(exception, "The development channel could not start.");
            else Console.Error.WriteLine($"Nazor: the development channel could not start: {exception.Message}");
            server.Dispose();
            return null;
        }
    }

}
