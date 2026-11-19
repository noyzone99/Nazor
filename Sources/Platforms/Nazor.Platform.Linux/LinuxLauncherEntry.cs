using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal sealed class LinuxLauncherEntry : ILauncherEntry
{
    private Action<(string AppUri, IDictionary<string, object> Properties)>? update;
    public ObjectPath ObjectPath => new("/com/canonical/unity/launcherentry");
    internal void SetCount(int count) => update?.Invoke(("application://" + LinuxDesktopWindow.AppId + ".desktop",
        new Dictionary<string, object> { ["count"] = (long)count, ["count-visible"] = count > 0 }));
    public Task<IDisposable> WatchUpdateAsync(Action<(string AppUri, IDictionary<string, object> Properties)> handler)
    {
        update += handler;
        return Task.FromResult<IDisposable>(new BusSubscription(() => update -= handler));
    }
}
