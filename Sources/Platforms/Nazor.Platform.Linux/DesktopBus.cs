using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

[DBusInterface("com.canonical.Unity.LauncherEntry")]
internal interface ILauncherEntry : IDBusObject
{
    Task<IDisposable> WatchUpdateAsync(Action<(string AppUri, IDictionary<string, object> Properties)> handler);
}

[DBusInterface("org.freedesktop.Notifications")]
internal interface INotifications : IDBusObject
{
    Task<uint> NotifyAsync(string appName, uint replacesId, string appIcon, string summary, string body, string[] actions, IDictionary<string, object> hints, int expireTimeout);
    Task CloseNotificationAsync(uint id);
    Task<IDisposable> WatchActionInvokedAsync(Action<(uint Id, string Action)> handler);
    Task<IDisposable> WatchNotificationClosedAsync(Action<(uint Id, uint Reason)> handler);
}

[DBusInterface("org.kde.StatusNotifierWatcher")]
internal interface IStatusNotifierWatcher : IDBusObject
{
    Task RegisterStatusNotifierItemAsync(string service);
    Task<object> GetAsync(string property);
}

[DBusInterface("org.kde.StatusNotifierItem")]
internal interface IStatusNotifierItem : IDBusObject
{
    Task ActivateAsync(int x, int y);
    Task SecondaryActivateAsync(int x, int y);
    Task ContextMenuAsync(int x, int y);
    Task ScrollAsync(int delta, string orientation);
    Task<object> GetAsync(string property);
    Task<IDictionary<string, object>> GetAllAsync();
    Task<IDisposable> WatchNewIconAsync(Action handler);
    Task<IDisposable> WatchNewToolTipAsync(Action handler);
    Task<IDisposable> WatchNewStatusAsync(Action<string> handler);
}

[DBusInterface("com.canonical.dbusmenu")]
internal interface ITrayMenu : IDBusObject
{
    Task<(uint Revision, (int Id, IDictionary<string, object> Properties, object[] Children) Layout)> GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames);
    Task<(int Id, IDictionary<string, object> Properties)[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames);
    Task<object> GetPropertyAsync(int id, string name);
    Task EventAsync(int id, string eventId, object data, uint timestamp);
    Task<int[]> EventGroupAsync((int Id, string EventId, object Data, uint Timestamp)[] events);
    Task<bool> AboutToShowAsync(int id);
    Task<(int[] Updates, int[] Errors)> AboutToShowGroupAsync(int[] ids);
    Task<object> GetAsync(string property);
    Task<IDictionary<string, object>> GetAllAsync();
    Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint Revision, int Parent)> handler);
}

[DBusInterface("org.freedesktop.portal.FileChooser")]
internal interface IFileChooser : IDBusObject
{
    Task<ObjectPath> OpenFileAsync(string parentWindow, string title, IDictionary<string, object> options);
    Task<ObjectPath> SaveFileAsync(string parentWindow, string title, IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.portal.Request")]
internal interface IPortalRequest : IDBusObject
{
    Task CloseAsync();
    Task<IDisposable> WatchResponseAsync(Action<(uint Response, IDictionary<string, object> Results)> handler);
}

[DBusInterface("org.freedesktop.portal.Settings")]
internal interface IDesktopSettings : IDBusObject
{
    Task<object> ReadAsync(string @namespace, string key);
    Task<IDictionary<string, IDictionary<string, object>>> ReadAllAsync(string[] namespaces);
    Task<IDisposable> WatchSettingChangedAsync(Action<(string Namespace, string Key, object Value)> handler);
}

internal sealed class BusSubscription(Action unsubscribe) : IDisposable
{
    private Action? action = unsubscribe;
    public void Dispose() => Interlocked.Exchange(ref action, null)?.Invoke();
}
