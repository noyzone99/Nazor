using Microsoft.Extensions.DependencyInjection;
using Nazor.Platform;

namespace Nazor;

/// <summary>Transient native notifications, independent of windows and IAnnouncer. UI thread only.</summary>
public interface ISystemNotificationService
{
    /// <summary>Gets whether the platform supports this service.</summary>
    bool IsSupported { get; }

    /// <summary>Occurs when activated is reported.</summary>
    event Action? Activated;

    /// <summary>Replaces the current notification. True means accepted, not necessarily displayed.</summary>
    /// <param name="notification">The notification (SystemNotification).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool Show(SystemNotification notification);

    /// <summary>Withdraws the current banner; does not clear notification-center history.</summary>
    void Withdraw();
}

/// <summary>Application lifetime, also usable when no windows remain.</summary>
public interface IApplicationLifetime
{
    /// <summary>Available during Run. Use InvokeAsync for desktop services called from a worker.</summary>
    Dispatcher Dispatcher { get; }

    /// <summary>Ends Run and disposes all windows and native resources. May be called from any thread.</summary>
    /// <param name="exitCode">The exit code reported when the application event loop terminates.</param>
    void Shutdown(int exitCode = 0);
}

/// <summary>Application registry of independently owned tray items. Get never creates an instance.</summary>
public interface ISystemTray
{
    /// <summary>Gets whether a tray host is currently available.</summary>
    bool IsSupported { get; }
    /// <summary>Returns the active registered instance of this exact tray type; never constructs an item.</summary>
    T Get<T>() where T : class, ITrayItem;
}

internal sealed class DesktopServices : ISystemTray, ISystemNotificationService
{
    internal ApplicationLauncher Launcher { get; } = new();
    private IPlatformDesktopIntegration? backend;
    private UIDispatcher? dispatcher;
    private readonly Dictionary<Type, ITrayItem> items = [];
    private readonly List<(IServiceScope Scope, ITrayItem Item, TrayItemContext Context)> owned = [];
    public bool IsSupported => backend?.SupportsTray is true;
    bool ISystemNotificationService.IsSupported => backend?.SupportsNotifications is true;
    public event Action? Activated;

    public T Get<T>() where T : class, ITrayItem
    {
        AssertRunning();
        return items.TryGetValue(typeof(T), out var item) && item.IsStarted
            ? (T)item : throw new InvalidOperationException($"Tray item '{typeof(T).Name}' is not active.");
    }
    internal void Start(IPlatformDesktopIntegration? integration, UIDispatcher uiDispatcher)
    {
        backend = integration;
        dispatcher = uiDispatcher;
        Launcher.Start(integration, uiDispatcher);
        if (backend is not null) backend.NotificationActivated += OnNotificationActivated;
    }
    internal void StartItems(IReadOnlyList<Type> types, IServiceProvider services)
    {
        AssertRunning();
        foreach (var type in types)
        {
            var scope = services.CreateScope();
            TrayItemContext? context = null;
            try
            {
                var item = (ITrayItem)scope.ServiceProvider.GetRequiredService(type);
                context = new TrayItemContext(backend?.CreateTrayItem(), dispatcher!);
                owned.Add((scope, item, context));
                items.Add(type, item);
            }
            catch { context?.Dispose(); scope.Dispose(); throw; }
        }
        foreach (var entry in owned) entry.Item.Start(entry.Context);
    }
    internal bool IsItemVisible(Type type) => items.TryGetValue(type, out var item) && item.IsVisible;
    internal void Stop()
    {
        if (dispatcher is null) return;
        dispatcher.AssertAccess();
        var errors = new List<Exception>();
        foreach (var entry in owned.AsEnumerable().Reverse())
        {
            try { entry.Item.Stop(); } catch (Exception error) { errors.Add(error); }
            try { entry.Context.Dispose(); } catch (Exception error) { errors.Add(error); }
            try { entry.Scope.Dispose(); } catch (Exception error) { errors.Add(error); }
        }
        owned.Clear();
        items.Clear();
        try { Launcher.Stop(); } catch (Exception error) { errors.Add(error); }
        if (backend is not null) backend.NotificationActivated -= OnNotificationActivated;
        backend = null;
        dispatcher = null;
        if (errors.Count != 0) throw new AggregateException("Could not stop all tray items.", errors);
    }
    public bool Show(SystemNotification notification)
    {
        AssertRunning();
        ArgumentNullException.ThrowIfNull(notification);
        return backend?.SupportsNotifications is true && backend.ShowNotification(notification);
    }
    public void Withdraw() { AssertRunning(); backend?.WithdrawNotification(); }
    private void AssertRunning() => (dispatcher ?? throw new InvalidOperationException("Desktop services are available during Run.")).AssertAccess();
    private void OnNotificationActivated() => dispatcher?.Post(() => { if (dispatcher is not null) Activated?.Invoke(); });
}
