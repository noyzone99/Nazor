using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nazor.Accessibility;
using Nazor.Platform;
using Nazor.Style;

namespace Nazor;

/// <summary>When an application with several windows ends.</summary>
public enum ShutdownMode
{
    /// <summary>When the main window (the first) closes; the other windows close with it.</summary>
    OnMainWindowClose,

    /// <summary>When the last window closes.</summary>
    OnLastWindowClose,

    /// <summary>Only an explicit shutdown ends the application; zero windows is allowed.</summary>
    OnExplicitShutdown,
}

/// <summary>Where a window opens.</summary>
public enum WindowStartupLocation
{
    /// <summary>Where Windows puts new windows; dialogs are centered on their owner.</summary>
    Default,

    /// <summary>Centers the window on its owner, falling back to the screen when no owner is available.</summary>
    CenterOwner,

    /// <summary>Centers the window in the display's work area.</summary>
    CenterScreen,
}

/// <summary>How <see cref="IWindowService" /> opens a window.</summary>
public sealed record WindowOpenOptions
{
    /// <summary>The window it belongs to: it stays above it and is minimized with it.</summary>
    public IWindow? Owner { get; init; }

    /// <summary>Whether it takes focus when shown; null uses the normal activation behavior.</summary>
    public bool? Activate { get; init; }

    /// <summary>Gets or sets how the new window is positioned relative to its owner or screen.</summary>
    public WindowStartupLocation StartupLocation { get; init; }

    /// <summary>
    /// The height follows the content of the root (the bottom of the children of its element), as a message box.
    /// </summary>
    public bool SizeToContent { get; init; }
}

/// <summary>
/// Opens windows, each with a component as its root, its own renderer and its own DI scope (the scoped services live
/// as long as the window). Inject it with <c>[Inject] IWindowService</c>.
/// </summary>
public interface IWindowService
{
    /// <summary>The open windows, in opening order.</summary>
    IReadOnlyList<IWindow> Windows { get; }

    /// <summary>The first window of the application.</summary>
    IWindow? MainWindow { get; }

    /// <summary>Opens a window containing the specified component type, parameters, and window options.</summary>
    /// <param name="component">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>The newly opened window.</returns>
    IWindow Open(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null);

    /// <summary>Opens a window containing the specified component type, parameters, and window options.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    /// <returns>The newly opened window.</returns>
    IWindow Open<TComponent>(IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) where TComponent : IComponent =>
        Open(typeof(TComponent), parameters, options);

    /// <summary>Opens a modal component window and awaits the result supplied when it closes.</summary>
    /// <param name="component">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>A task returning the dialog result, or the result type's default value if no compatible result is
    /// supplied.</returns>
    Task<object?> ShowDialogAsync(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null);

    /// <summary>Opens a modal component window and awaits the result supplied when it closes.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>A task returning the dialog result, or the result type's default value if no compatible result is
    /// supplied.</returns>
    async Task<TResult?> ShowDialogAsync<TComponent, TResult>(IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null)
        where TComponent : IComponent =>
        await ShowDialogAsync(typeof(TComponent), parameters, options).ConfigureAwait(true) is TResult result ? result : default;

    /// <summary>
    /// Shows a message in a modal dialog drawn with the theme of the application, with the buttons of
    /// <paramref name="buttons" /> (the first is the default one, for Enter); the task completes with the button
    /// chosen. Esc and the close button answer Cancel when there is one, OK for a message with OK only, and None
    /// otherwise.
    /// </summary>
    /// <param name="message">The message (string).</param>
    /// <param name="title">The title (string?).</param>
    /// <param name="buttons">The buttons (MessageBoxButtons).</param>
    /// <param name="icon">The icon (MessageBoxIcon).</param>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    Task<MessageBoxResult> ShowMessageAsync(string message, string? title = null, MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None);
}

/// <summary>
/// Speaks messages that are not on the screen through the screen reader of the user, as <c>aria-live</c> or the
/// notifications of WinUI: "Saved", "3 results", "Connection lost". Inject it in a component of a window; text that
/// is on the screen and changes is better a live region (<c>live="polite"</c>, an <c>InlineMessage</c>).
/// </summary>
public interface IAnnouncer
{
    /// <summary>Speaks <paramref name="message" />: after the current speech, or at once when assertive.</summary>
    /// <param name="message">The message (string).</param>
    /// <param name="priority">The priority (AnnouncementPriority).</param>
    void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite);
}

internal sealed class Announcer(WindowContext context) : IAnnouncer
{
    public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite) => context.Window?.Announce(message, priority);
}

internal sealed class WindowContext
{
    public NazorWindow? Window { get; set; }
}

internal sealed class WindowService(WindowManager manager, WindowContext context) : IWindowService
{
    public IReadOnlyList<IWindow> Windows => manager.Windows;

    public IWindow? MainWindow => manager.MainWindow;

    public IWindow Open(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) =>
        manager.Open(component, parameters, options);

    public Task<object?> ShowDialogAsync(Type component, IReadOnlyDictionary<string, object?>? parameters = null, WindowOpenOptions? options = null) =>
        manager.ShowDialog(component, parameters, options, context.Window);

    public async Task<MessageBoxResult> ShowMessageAsync(string message, string? title = null, MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        var result = await ShowDialogAsync(typeof(MessageBoxView), new Dictionary<string, object?>
        {
            [nameof(MessageBoxView.Message)] = message,
            [nameof(MessageBoxView.Title)] = title ?? context.Window?.Title ?? "",
            [nameof(MessageBoxView.Buttons)] = buttons,
            [nameof(MessageBoxView.Icon)] = icon,
        }, new WindowOpenOptions { SizeToContent = true }).ConfigureAwait(true);
        return result is MessageBoxResult chosen ? chosen : MessageBoxView.Dismissed(buttons);
    }
}

internal sealed class WindowManager : IApplicationLifetime
{
    private readonly List<NazorWindow> windows = [];
    private readonly Dictionary<NazorWindow, IServiceScope> scopes = [];
    private IPlatform? platform;
    private UIDispatcher? dispatcher;
    private ApplicationRuntimeSettings template = new();
    private int exitCode;
    private readonly ApplicationAppearance appearance;
    public WindowManager(ApplicationAppearance appearance) => this.appearance = appearance;
    private bool stopping;
    private int shutdownRequested;
    internal bool ShutdownRequested => Volatile.Read(ref shutdownRequested) != 0;

    Dispatcher IApplicationLifetime.Dispatcher => Dispatcher;

    public void Shutdown(int code = 0)
    {
        var uiDispatcher = Dispatcher;
        if (Interlocked.Exchange(ref shutdownRequested, 1) != 0) return;
        uiDispatcher.Post(() =>
        {
            if (stopping) return;
            stopping = true;
            exitCode = code;
            Platform.Quit(code);
        });
    }

    public ColorScheme ColorScheme => appearance.ColorScheme;

    private void UpdateAppearance()
    {
        foreach (var window in windows.ToArray()) window.UpdateAppearance(appearance);
    }

    private ApplicationRuntimeSettings WithAppearance(ApplicationRuntimeSettings settings) => settings with
    {
        Theme = appearance.Theme, StyleSheet = appearance.StyleSheet, ColorScheme = appearance.ColorScheme,
        ReducedMotion = appearance.ReducedMotion, HighContrast = appearance.HighContrast,
        ControlDensity = appearance.ControlDensity,
    };

    public IReadOnlyList<IWindow> Windows => [.. windows];

    public NazorWindow? MainWindow { get; private set; }

    public void Start(IPlatform platform, UIDispatcher dispatcher, ApplicationRuntimeSettings options)
    {
        this.platform = platform;
        this.dispatcher = dispatcher;
        template = options;
        stopping = false;
        shutdownRequested = 0;
        exitCode = 0;
        if (!Enum.IsDefined(options.ShutdownMode)) throw new ArgumentOutOfRangeException(nameof(options));
        if (!Enum.IsDefined(options.ColorScheme)) throw new ArgumentOutOfRangeException(nameof(options));
        if (!Enum.IsDefined(options.ControlDensity)) throw new ArgumentOutOfRangeException(nameof(options));
        if (options.Appearance is null)
        {
            appearance.Theme = options.Theme;
            appearance.StyleSheet = options.StyleSheet;
            appearance.ColorScheme = options.ColorScheme;
            appearance.ReducedMotion = options.ReducedMotion;
            appearance.HighContrast = options.HighContrast;
            appearance.ControlDensity = options.ControlDensity;
        }
        appearance.Attach(dispatcher);
        appearance.Changed += UpdateAppearance;
    }

    public NazorWindow OpenMain(Type component, ApplicationRuntimeSettings options)
    {
        var scope = (options.Services?.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory)?.CreateScope();
        try
        {
            var window = new NazorWindow(Platform, Dispatcher, component, WithAppearance(options) with { Services = scope?.ServiceProvider ?? options.Services },
                frame: new WindowOptions { Owner = options.Opening?.Owner is NazorWindow { IsClosed: false } owner ? owner.PlatformWindow : null,
                Centering = options.Opening?.StartupLocation switch
                {
                    WindowStartupLocation.CenterScreen => WindowCentering.Screen,
                    WindowStartupLocation.CenterOwner => WindowCentering.Owner,
                    _ => WindowCentering.None,
                } });
            MainWindow = window;
            Track(window, scope);
            if (options.Opening?.SizeToContent is true)
            {
                window.FitHeightToContent();
                if (options.Opening.StartupLocation != WindowStartupLocation.Default)
                    window.PlatformWindow.Center(options.Opening.StartupLocation == WindowStartupLocation.CenterScreen ? WindowCentering.Screen : WindowCentering.Owner);
            }
            return window;
        }
        catch { scope?.Dispose(); throw; }
    }

    public Task<object?> ShowDialog(Type component, IReadOnlyDictionary<string, object?>? parameters, WindowOpenOptions? open, NazorWindow? opener)
    {
        var owner = open?.Owner as NazorWindow ?? opener;
        if (owner is { IsClosed: true }) owner = null;
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = Open(component, parameters, (open ?? new WindowOpenOptions()) with { Owner = owner }, dialog: true);
        owner?.PlatformWindow.SetEnabled(false);

        void Release()
        {
            if (owner is { IsClosed: false }) owner.PlatformWindow.SetEnabled(true);
        }
        window.Closing += Release;
        window.Closed += () =>
        {
            Release();
            completion.TrySetResult(window.DialogResult);
        };
        return completion.Task;
    }

    public NazorWindow Open(Type component, IReadOnlyDictionary<string, object?>? parameters, WindowOpenOptions? open, bool dialog = false)
    {
        ArgumentNullException.ThrowIfNull(component);
        Dispatcher.AssertAccess();
        if (stopping) throw new InvalidOperationException("The application is shutting down.");
        if (template.SingleView) throw new PlatformNotSupportedException("This application uses a single view and cannot open desktop windows or dialogs.");
        var scope = (template.Services?.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory)?.CreateScope();
        try
        {
            var options = WithAppearance(template) with
            {
                Services = scope?.ServiceProvider ?? template.Services,
                Title = "Nazor", Width = 800, Height = 600,
                IsMinimizable = true, IsMaximizable = true, IsResizable = true, FullScreenEnabled = true,
                IsAlwaysOnTop = false, IsVisibleInTaskbar = true,
                Activate = open?.Activate ?? true, DeclaredWindow = true,
            };
            var owner = open?.Owner is NazorWindow { IsClosed: false } ownerWindow ? ownerWindow.PlatformWindow : null;
            var centering = (open?.StartupLocation ?? WindowStartupLocation.Default) switch
            {
                WindowStartupLocation.CenterOwner => WindowCentering.Owner,
                WindowStartupLocation.CenterScreen => WindowCentering.Screen,
                _ => dialog ? WindowCentering.Owner : WindowCentering.None,
            };
            var frame = new WindowOptions { Owner = owner, IsDialog = dialog, Centering = centering };
            var window = new NazorWindow(Platform, Dispatcher, component, options,
                parameters is null ? default : ParameterView.FromDictionary(parameters), frame);
            Track(window, scope);
            if (open?.SizeToContent is true)
            {
                window.FitHeightToContent();

                if (centering != WindowCentering.None) window.PlatformWindow.Center(centering);
            }
            window.Show(options.Activate);
            return window;
        }
        catch
        {
            scope?.Dispose();
            throw;
        }
    }

    private void Track(NazorWindow window, IServiceScope? scope)
    {
        windows.Add(window);
        if (scope is not null) scopes[window] = scope;
        window.Closed += () => Closed(window);
    }

    private void Closed(NazorWindow window)
    {
        windows.Remove(window);

        if (scopes.Remove(window, out var scope)) scope.Dispose();
        if (window.Exceptions.Count > 0) exitCode = 1;
        if (stopping) return;
        if (template.ShutdownMode == ShutdownMode.OnMainWindowClose && window == MainWindow)
        {
            foreach (var other in windows.ToArray()) other.Close();
            Platform.Quit(exitCode);
        }
        else if (template.ShutdownMode != ShutdownMode.OnExplicitShutdown && windows.Count == 0) Platform.Quit(exitCode);
    }

    public Task<IReadOnlyList<string>?> ShowFileDialog(NazorWindow? owner, FileDialogRequest request)
    {
        var completion = new TaskCompletionSource<IReadOnlyList<string>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.Post(() =>
        {
            try
            {
                completion.SetResult(Platform.ShowFileDialog(owner is { IsClosed: false } window ? window.PlatformWindow : null, request));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    public void DisposeAll()
    {
        stopping = true;
        foreach (var window in windows.ToArray())
        {
            window.Dispose();
            if (scopes.Remove(window, out var scope)) scope.Dispose();
        }
        windows.Clear();
        MainWindow = null;
        appearance.Changed -= UpdateAppearance;
        appearance.Detach();
        platform = null;
        dispatcher = null;
    }

    private IPlatform Platform => platform ?? throw new InvalidOperationException("The application is not running.");

    private UIDispatcher Dispatcher => dispatcher ?? throw new InvalidOperationException("The application is not running.");
}

internal static class NazorServiceCollectionExtensions
{
    public static IServiceCollection AddNazorServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ApplicationAppearance>();
        services.TryAddSingleton<IApplicationAppearance>(provider => provider.GetRequiredService<ApplicationAppearance>());
        services.TryAddSingleton<WindowManager>();
        services.TryAddSingleton<IApplicationLifetime>(provider => provider.GetRequiredService<WindowManager>());
        services.TryAddSingleton<DesktopServices>();
        services.TryAddSingleton<IApplicationLauncher>(provider => provider.GetRequiredService<DesktopServices>().Launcher);
        services.TryAddSingleton<ISystemTray>(provider => provider.GetRequiredService<DesktopServices>());
        services.TryAddSingleton<ISystemNotificationService>(provider => provider.GetRequiredService<DesktopServices>());
        services.TryAddScoped<WindowContext>();
        services.TryAddScoped<IWindow>(provider => provider.GetRequiredService<WindowContext>().Window ??
            throw new InvalidOperationException("This scope belongs to no window."));
        services.TryAddScoped<IWindowService, WindowService>();
        services.TryAddScoped<IFileDialogService, FileDialogService>();
        services.TryAddScoped<IAnnouncer, Announcer>();

        services.AddLogging();
        services.AddLocalization();
        services.TryAddScoped<NavigationManager>();
        services.TryAddSingleton<IWindowPlacementStore>(_ => JsonWindowPlacementStore.ForApplication());
        services.TryAddScoped<Input.KeyBindings>();
        return services;
    }
}
