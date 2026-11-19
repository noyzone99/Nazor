using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nazor.Style;
using Nazor.Graphics.Text;

namespace Nazor;

/// <summary>
/// Configures a Nazor application on the .NET Generic Host: services, configuration, logging and explicit startup,
/// then <see cref="Build" />. It is an <see cref="IHostApplicationBuilder" />, so the extension methods of libraries
/// that register themselves on a host work unchanged.
/// </summary>
public sealed class NazorApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder host;

    internal NazorApplicationBuilder(string[]? args)
    {

        host = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name,
        });
        host.Services.AddNazorServices();
        Application = new(this);

        if (NazorApplication.IsDevChannelEnabled(new() { DevChannel = Diagnostics.DevChannel })) StyleRegistry.TrackingEnabled = true;
    }

    private bool startupSelected;
    private bool built;
    private Type? rootComponent;
    private bool? declaredWindow;
    private bool singleView;
    private WindowConfiguration window = new();
    private readonly List<Type> trayItems = [];

    /// <summary>Application startup, culture overrides and shutdown policy, captured at Build.</summary>
    public ApplicationBuilder Application { get; }
    /// <summary>Backend host selection, captured at Build.</summary>
    public PlatformConfiguration Platform { get; } = new();
    /// <summary>Development and instrumentation configuration, captured at Build.</summary>
    public DiagnosticsConfiguration Diagnostics { get; } = new();
    /// <summary>Private fonts and matching policy, captured at Build.</summary>
    public FontOptions Fonts { get; set; } = new();

    /// <summary>Selects a component that declares <c>@window</c>.</summary>
    /// <typeparam name="TComponent">The component declaring the main window.</typeparam>
    /// <param name="opening">Optional activation and positioning context.</param>
    internal void UseMainWindow<TComponent>(WindowOpenOptions? opening = null) where TComponent : IComponent
    {
        SelectStartup(typeof(TComponent));
        declaredWindow = true;
        window.Activate = opening?.Activate ?? true;
        mainOpening = opening is null ? null : opening with { };
    }

    private WindowOpenOptions? mainOpening;

    /// <summary>Explicitly creates a main window for content that does not declare <c>@window</c>.</summary>
    /// <typeparam name="TComponent">The content component.</typeparam>
    /// <param name="configure">Configures this window only; values are copied at Build.</param>
    internal void CreateMainWindow<TComponent>(Action<WindowConfiguration>? configure = null) where TComponent : IComponent
    {
        var definition = new WindowConfiguration();
        configure?.Invoke(definition);
        SelectStartup(typeof(TComponent));
        declaredWindow = false;
        window = definition;
    }

    /// <summary>Selects content for an ISingleViewPlatform surface without desktop windows.</summary>
    /// <typeparam name="TComponent">The root view component, without <c>@window</c>.</typeparam>
    internal void UseMainView<TComponent>() where TComponent : IComponent
    {
        SelectStartup(typeof(TComponent));
        declaredWindow = false;
        singleView = true;
    }

    /// <summary>Registers one independently scoped tray item of this type, created during Run.</summary>
    internal void UseTrayItem<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class, ITrayItem
    {
        if (built) throw new InvalidOperationException("The application has already been built.");
        var type = typeof(T);
        if (type.IsAbstract || type.ContainsGenericParameters) throw new ArgumentException("Tray items must be concrete closed types.");
        if (trayItems.Contains(type)) throw new InvalidOperationException($"Tray item '{type.Name}' is already registered.");
        trayItems.Add(type);
        Services.AddScoped<T>();
    }

    private void SelectStartup(Type? component)
    {
        if (built) throw new InvalidOperationException("The application has already been built.");
        if (startupSelected) throw new InvalidOperationException("Select at most one initial surface: UseMainWindow, CreateMainWindow or UseMainView.");
        if (component is not null && (component.IsAbstract || component.ContainsGenericParameters))
            throw new ArgumentException("The root component must be a concrete closed type.", nameof(component));
        startupSelected = true;
        rootComponent = component;
    }

    /// <summary>Gets the service registrations used to build the application provider.</summary>
    public IServiceCollection Services => host.Services;

    /// <summary>
    /// <c>appsettings.json</c>, <c>appsettings.{Environment}.json</c>, environment variables and command line.
    /// </summary>
    public ConfigurationManager Configuration => host.Configuration;

    IConfigurationManager IHostApplicationBuilder.Configuration => host.Configuration;

    /// <summary>Gets the builder for application logging providers and filters.</summary>
    public ILoggingBuilder Logging => host.Logging;

    /// <summary>Gets the Generic Host environment being configured.</summary>
    public IHostEnvironment Environment => host.Environment;

    /// <summary>Gets the builder for application metrics.</summary>
    public IMetricsBuilder Metrics => host.Metrics;

    /// <summary>Gets the shared host-builder property dictionary.</summary>
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)host).Properties;

    /// <summary>
    /// Configures the application's dependency injection container factory and optional builder callback.
    /// </summary>
    /// <param name="factory">The service-provider factory used to create the dependency injection container.</param>
    /// <param name="configure">The optional callback used to configure the container builder.</param>
    /// <typeparam name="TContainerBuilder">The service container builder type.</typeparam>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull => host.ConfigureContainer(factory, configure);

    /// <summary>Builds services and snapshots the explicit startup definition. Native creation is deferred to Run.</summary>
    /// <returns>The application, whose live Appearance service can be configured before Run.</returns>
    public NazorApp Build()
    {
        if (built) throw new InvalidOperationException("The application has already been built.");
        if (!startupSelected && trayItems.Count == 0) throw new InvalidOperationException("Configure an initial surface or at least one UseTrayItem before Build.");
        if (singleView && trayItems.Count != 0) throw new InvalidOperationException("Tray items require a desktop host, not UseMainView.");
        ArgumentNullException.ThrowIfNull(Fonts);
        ArgumentNullException.ThrowIfNull(Fonts.PrivateFontFiles);
        if (!Enum.IsDefined(Application.ShutdownMode)) throw new ArgumentOutOfRangeException(nameof(Application.ShutdownMode));
        if (rootComponent is null && Application.ShutdownMode != ShutdownMode.OnExplicitShutdown)
            throw new InvalidOperationException("Tray-only startup requires ShutdownMode.OnExplicitShutdown.");
        if (!double.IsFinite(window.Width) || window.Width <= 0 || !double.IsFinite(window.Height) || window.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(window), "Window dimensions must be finite and positive.");
        var settings = new ApplicationRuntimeSettings
        {
            Rendering = RendererSelection.ReadEnvironment(),
            Culture = Application.Culture, UICulture = Application.UICulture,
            ShutdownMode = Application.ShutdownMode, TrayItems = trayItems.ToArray(),
            PlatformHost = Platform.Host, DevChannel = Diagnostics.DevChannel, AnimationClock = Diagnostics.AnimationClock,
            ConfigureWindow = Diagnostics.ConfigureWindow,
            Fonts = Fonts with { PrivateFontFiles = Fonts.PrivateFontFiles.ToArray() },
            Title = window.Title, Width = window.Width, Height = window.Height,
            IsMinimizable = window.IsMinimizable, IsMaximizable = window.IsMaximizable,
            IsResizable = window.IsResizable, FullScreenEnabled = window.FullScreenEnabled, Activate = window.Activate,
            Decorations = window.Decorations, IsAlwaysOnTop = window.IsAlwaysOnTop, IsVisibleInTaskbar = window.IsVisibleInTaskbar,
            DeclaredWindow = declaredWindow, SingleView = singleView, Opening = mainOpening,
        };
        host.Services.AddSingleton(Application.Configuration with { });
        built = true;
        return new NazorApp(host.Build(), settings, rootComponent);
    }
}
