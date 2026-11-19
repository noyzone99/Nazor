using Nazor.Platform.Linux;
using Tmds.DBus.Protocol;
using P = Tmds.DBus.Protocol;

using var server = new DesktopConnection(DBusAddress.Session!);
var name = (await server.ConnectAsync()).LocalName;
var notifications = new Notifications();
var chooser = new Chooser();
var cache = new Cache();
var watcher = new Watcher();
await server.RegisterObjectsAsync([notifications, chooser, cache, watcher]);
using var client = new DesktopConnection(DBusAddress.Session!);
await client.ConnectAsync();
var proxy = client.CreateProxy<INotifications>(name, notifications.ObjectPath);
var signal = new TaskCompletionSource<(uint, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
using var subscription = await proxy.WatchActionInvokedAsync(value => signal.TrySetResult(value));
var id = await proxy.NotifyAsync("app", 0, "", "title", "body", ["default"], new Dictionary<string, object> { ["test"] = true }, 10);
if (id != 42 || !notifications.Called) throw new InvalidOperationException("The AOT method/variant payload changed.");
try
{
    await proxy.CloseNotificationAsync(uint.MaxValue);
    throw new InvalidOperationException("The remote D-Bus error was lost.");
}
catch (DBusErrorReplyException error) when (error.ErrorName == "org.freedesktop.DBus.Error.InvalidArgs" && error.ErrorMessage == "Invalid notification ID.") { }
notifications.Emit();
if (await signal.Task.WaitAsync(TimeSpan.FromSeconds(5)) != (42u, "default")) throw new InvalidOperationException("The AOT signal payload changed.");
var picker = client.CreateProxy<IFileChooser>(name, chooser.ObjectPath);
var path = await picker.OpenFileAsync("", "files", new Dictionary<string, object> { ["filters"] = new[] { ("Text", new[] { (0u, "*.txt") }) }, ["current_folder"] = new byte[] { 47, 0 } });
if (path != new ObjectPath("/request/1") || !chooser.Called) throw new InvalidOperationException("The AOT portal variant payload changed.");
var cacheProxy = client.CreateProxy<IAccessibleCache>(name, cache.ObjectPath);
var values = await cacheProxy.GetItemsAsync();
if (values.Length != 1 || values[0].Name != "accessible" || values[0].Reference.Item2 != new ObjectPath("/accessible/1") || values[0].States[0] != 3)
    throw new InvalidOperationException("The AOT accessibility struct payload changed.");
using var missing = await client.ResolveServiceOwnerAsync("org.nazor.Deployment.Missing", _ => { }, _ => { });
if (await client.ResolveServiceOwnerAsync("org.nazor.Deployment.Missing") is not null) throw new InvalidOperationException("The missing-owner result changed.");
server.UnregisterObject(cache);
if ((bool)await client.CreateProxy<IStatusNotifierWatcher>(name, watcher.ObjectPath).GetAsync("IsStatusNotifierHostRegistered") != true)
    throw new InvalidOperationException("The AOT property getter changed.");
var xml = await client.Call(Introspection(client, name, watcher.ObjectPath), static (message, _) => message.GetBodyReader().ReadString());
if (!xml.Contains("org.freedesktop.DBus.Properties", StringComparison.Ordinal)) throw new InvalidOperationException("Property introspection was lost.");
Console.WriteLine("PASS: D-Bus methods, errors, variants, tuple arrays, signals, accessibility structs and owner lookup.");
return 0;

static P.MessageBuffer Introspection(DesktopConnection client, string service, ObjectPath path)
{
    using var writer = client.Raw.GetMessageWriter();
    writer.WriteMethodCallHeader(destination: service, path: path.ToString(), @interface: "org.freedesktop.DBus.Introspectable", member: "Introspect");
    return writer.CreateMessage();
}

sealed class Watcher : IStatusNotifierWatcher
{
    public ObjectPath ObjectPath => new("/watcher");
    public Task RegisterStatusNotifierItemAsync(string service) => Task.CompletedTask;
    public Task<object> GetAsync(string property) => Task.FromResult<object>(property == "IsStatusNotifierHostRegistered");
}

sealed class Notifications : INotifications
{
    public ObjectPath ObjectPath => new("/notifications");
    public bool Called;
    private Action<(uint Id, string Action)>? action;
    public Task<uint> NotifyAsync(string appName, uint replacesId, string appIcon, string summary, string body, string[] actions, IDictionary<string, object> hints, int expireTimeout)
    { Called = appName == "app" && summary == "title" && body == "body" && actions.Single() == "default" && hints["test"] is true && expireTimeout == 10; return Task.FromResult(42u); }
    public Task CloseNotificationAsync(uint id) => id == uint.MaxValue
        ? throw new DBusErrorReplyException("org.freedesktop.DBus.Error.InvalidArgs", "Invalid notification ID.") : Task.CompletedTask;
    public Task<IDisposable> WatchActionInvokedAsync(Action<(uint Id, string Action)> handler)
    { action += handler; return Task.FromResult<IDisposable>(new BusSubscription(() => action -= handler)); }
    public Task<IDisposable> WatchNotificationClosedAsync(Action<(uint Id, uint Reason)> handler) => Task.FromResult<IDisposable>(new BusSubscription(() => { }));
    public void Emit() => action?.Invoke((42, "default"));
}
sealed class Chooser : IFileChooser
{
    public ObjectPath ObjectPath => new("/chooser");
    public bool Called;
    public Task<ObjectPath> OpenFileAsync(string parentWindow, string title, IDictionary<string, object> options)
    {
        Called = title == "files" && options["filters"] is object[] { Length: 1 } filters && filters[0] is object[] filter &&
            filter[0] is "Text" && filter[1] is object[] { Length: 1 } patterns && patterns[0] is object[] pattern &&
            pattern[0] is 0u && pattern[1] is "*.txt" && options["current_folder"] is byte[] { Length: 2 };
        return Task.FromResult(new ObjectPath("/request/1"));
    }
    public Task<ObjectPath> SaveFileAsync(string parentWindow, string title, IDictionary<string, object> options) => OpenFileAsync(parentWindow, title, options);
}
sealed class Cache : IAccessibleCache
{
    public ObjectPath ObjectPath => new("/cache");
    public Task<AccessibleCacheItem[]> GetItemsAsync() => Task.FromResult(new[] { new AccessibleCacheItem { Reference = ("app", new("/accessible/1")), Application = ("app", new("/application")), Parent = ("", new("/null")), Interfaces = ["org.a11y.atspi.Accessible"], Name = "accessible", Description = "description", Role = 1, States = [3] } });
    public Task<IDisposable> WatchAddAccessibleAsync(Action<AccessibleCacheItem> handler) => Task.FromResult<IDisposable>(new BusSubscription(() => { }));
    public Task<IDisposable> WatchRemoveAccessibleAsync(Action<(string, ObjectPath)> handler) => Task.FromResult<IDisposable>(new BusSubscription(() => { }));
}
