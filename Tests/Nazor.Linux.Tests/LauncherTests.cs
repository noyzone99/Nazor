using Nazor.Platform;
using Nazor.Platform.Linux;
using Tmds.DBus.Protocol;

namespace Nazor.Linux.Tests;

[Collection("Linux desktop")]
public sealed class LauncherTests
{
    public static bool HasSession => DesktopServicesTests.HasSession;

    [Fact]
    public async Task Launcher_signal_uses_the_window_desktop_identity_and_int64_counts()
    {
        var entry = new LinuxLauncherEntry();
        var updates = new List<(string AppUri, IDictionary<string, object> Properties)>();
        using var subscription = await entry.WatchUpdateAsync(updates.Add);
        entry.SetCount(5);
        entry.SetCount(0);
        Assert.Equal("application://" + LinuxDesktopWindow.AppId + ".desktop", updates[0].AppUri);
        Assert.Equal(5L, Assert.IsType<long>(updates[0].Properties["count"]));
        Assert.True((bool)updates[0].Properties["count-visible"]);
        Assert.False((bool)updates[1].Properties["count-visible"]);
    }

    [Fact(SkipUnless = nameof(HasSession), Skip = "Requires isolated Wayland and D-Bus services.")]
    public void Numeric_badges_follow_host_availability_and_replay_after_host_restart()
    {
        using var bus = new DesktopConnection(DBusAddress.Session!);
        using var platform = new WaylandPlatform();
        platform.Wait(bus.ConnectAsync());
        platform.Wait(Complete(bus.Raw.RequestNameAsync("com.canonical.Unity")));
        var desktop = platform.Desktop;
        Assert.Equal(new ApplicationLauncherCapabilities(false, true, false), desktop.LauncherCapabilities);
        Assert.False(desktop.SetJumpList([new("Unavailable", [])]));
        Assert.False(desktop.SetBadgeIcon([], "Unavailable"));
        var changed = 0;
        desktop.LauncherCapabilitiesChanged += () => changed++;
        var updates = new List<(string AppUri, IDictionary<string, object> Properties)>();
        var proxy = bus.CreateProxy<ILauncherEntry>(desktop.BusName, "/com/canonical/unity/launcherentry");
        using var subscription = platform.Wait(proxy.WatchUpdateAsync(update => platform.Post(() => updates.Add(update))));
        Assert.True(desktop.SetBadgeCount(6));
        Assert.True(platform.Wait(UntilAsync(() => updates.Count == 1)));
        Assert.Equal(6L, updates[0].Properties["count"]);
        Assert.True(platform.Wait(bus.Raw.ReleaseNameAsync("com.canonical.Unity")));
        Assert.True(platform.Wait(UntilAsync(() => !desktop.LauncherCapabilities.SupportsBadgeCount)));
        Assert.False(desktop.SetBadgeCount(9));
        platform.Wait(Complete(bus.Raw.RequestNameAsync("com.canonical.Unity")));
        Assert.True(platform.Wait(UntilAsync(() => updates.Count == 2)));
        Assert.Equal(6L, updates[1].Properties["count"]);
        Assert.Equal(2, changed);
        Assert.True(desktop.ClearBadge());
        Assert.True(platform.Wait(UntilAsync(() => updates.Count == 3)));
        Assert.False((bool)updates[2].Properties["count-visible"]);
    }

    private static async Task<bool> Complete(Task task) { await task.ConfigureAwait(false); return true; }
    private static async Task<bool> UntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) return false;
            await Task.Delay(10, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        return true;
    }
}
