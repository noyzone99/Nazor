using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Text;
using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal sealed class LinuxDesktopIntegration : IPlatformDesktopIntegration, IDisposable
{
    private const string WatcherName = "org.kde.StatusNotifierWatcher", NotificationsName = "org.freedesktop.Notifications", PortalName = "org.freedesktop.portal.Desktop";
    private readonly LinuxDesktopPlatform platform;
    private readonly List<IDisposable> subscriptions = [];
    private DesktopConnection? bus;
    private INotifications? notifications;
    private readonly List<LinuxTrayItem> trayItems = [];
    private int nextTrayId;
    private string uniqueName = "";
    private string watcherOwner = "", notificationsOwner = "";
    private bool disposed, watcherPresent, notificationsPresent;
    private uint notificationId;
    private readonly LinuxLauncherEntry launcher = new();
    private readonly Dictionary<string, string> launcherHosts = new() { ["com.canonical.Unity"] = "", ["org.kde.plasmashell"] = "" };
    private int? badgeCount;
    public ApplicationLauncherCapabilities LauncherCapabilities => new(false, !disposed && bus is not null && launcherHosts.Values.Any(owner => owner.Length != 0), false);
    public event Action? LauncherCapabilitiesChanged;
    public bool SetJumpList(IReadOnlyList<JumpListTask> tasks) => false;
    public bool SetBadgeIcon(IReadOnlyList<Nazor.Graphics.RenderedImage> images, string description) => false;
    public bool ClearBadge() => SetBadgeCount(0);
    public bool SetBadgeCount(int count)
    {
        platform.AssertAccess();
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (!LauncherCapabilities.SupportsBadgeCount) return false;
        try { launcher.SetCount(count); badgeCount = count; return true; }
        catch (Exception error) when (error is DBusErrorReplyException or DBusConnectionException or ObjectDisposedException)
        { Console.Error.WriteLine($"Nazor: launcher badge failed: {error.Message}"); return false; }
    }
    private AccessibilityBridge? accessibility;
    private bool accessibilityAttempted;
    internal bool Dark { get; private set; }
    internal bool ReducedMotion { get; private set; }
    internal bool HighContrast { get; private set; }
    internal WindowButtonsLayout CaptionLayout { get; private set; } = WindowButtonsLayout.Default;
    private Process? captionSettings;
    internal DesktopConnection? Bus => bus;
    internal string BusName => uniqueName;
    internal void SyncAccessibility(LinuxDesktopWindow window, Nazor.Accessibility.SemanticTree? tree)
    {
        if (bus is null || tree is null) return;
        if (!accessibilityAttempted)
        {
            accessibilityAttempted = true;
            try { accessibility = new AccessibilityBridge(platform, bus); }
            catch (Exception error) when (error is DBusErrorReplyException or DBusConnectionException or TimeoutException)
            { Console.Error.WriteLine($"Nazor: accessibility bus unavailable: {error.Message}"); }
        }
        accessibility?.Sync(window, tree);
    }
    internal void RemoveAccessibility(LinuxDesktopWindow window) => accessibility?.Remove(window);
    public bool SupportsTray => watcherPresent;
    public bool SupportsNotifications => notificationsPresent;
    public IPlatformTrayItem CreateTrayItem()
    {
        platform.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        var item = new LinuxTrayItem(platform, this, ++nextTrayId);
        trayItems.Add(item);
        return item;
    }
    internal void RemoveTray(LinuxTrayItem item) => trayItems.Remove(item);
    public event Action? NotificationActivated;

    internal LinuxDesktopIntegration(LinuxDesktopPlatform platform)
    {
        this.platform = platform;
        if (LinuxDesktopWindow.DesktopProfile(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")) == SystemColorProfile.GNOME)
            WatchCaptionLayout();
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"))) return;
        try { platform.Wait(Task.Run(InitializeAsync)); }
        catch (Exception error) when (error is DBusErrorReplyException or DBusConnectionException or TimeoutException or IOException)
        {
            foreach (var subscription in subscriptions) subscription.Dispose();
            subscriptions.Clear();
            bus?.Dispose(); bus = null;
            watcherPresent = notificationsPresent = false;
            Console.Error.WriteLine($"Nazor: desktop services unavailable: {error.Message}");
        }
    }

    private void WatchCaptionLayout()
    {
        try
        {
            captionSettings = Start("monitor");
            captionSettings.OutputDataReceived += (_, args) =>
            {
                const string prefix = "button-layout:";
                if (args.Data is { } line && line.StartsWith(prefix, StringComparison.Ordinal))
                    platform.Post(() =>
                    {
                        if (disposed) return;
                        CaptionLayout = ParseCaptionLayout(line[prefix.Length..]);
                        platform.SettingsChanged();
                    });
            };
            captionSettings.BeginOutputReadLine();
            using var initial = Start("get");
            if (initial.WaitForExit(500) && initial.ExitCode == 0)
                CaptionLayout = ParseCaptionLayout(initial.StandardOutput.ReadToEnd());
            else if (!initial.HasExited) initial.Kill();
        }
        catch (Win32Exception) { }

        static Process Start(string operation) => Process.Start(new ProcessStartInfo("gsettings")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { operation, "org.gnome.desktop.wm.preferences", "button-layout" }
        })!;
    }

    internal static WindowButtonsLayout ParseCaptionLayout(string value)
    {
        var sides = value.Trim().Trim('\'').Split(':', 2);
        if (sides.Length != 2) return WindowButtonsLayout.Default;
        var seen = new HashSet<WindowButton>();
        return new(ParseSide(sides[0]), ParseSide(sides[1]));

        WindowButton[] ParseSide(string side) => side.Split(',', StringSplitOptions.TrimEntries)
            .Select<string, WindowButton?>(name => name switch
            {
                "minimize" => WindowButton.Minimize,
                "maximize" => WindowButton.Maximize,
                "close" => WindowButton.Close,
                _ => null
            }).OfType<WindowButton>().Where(seen.Add).ToArray();
    }

    private async Task<bool> InitializeAsync()
    {
        bus = new DesktopConnection(DBusAddress.Session!);
        uniqueName = (await bus.ConnectAsync().ConfigureAwait(false)).LocalName;
        await bus.RegisterObjectsAsync([launcher]).ConfigureAwait(false);
        foreach (var name in launcherHosts.Keys.ToArray())
        {
            launcherHosts[name] = await bus.ResolveServiceOwnerAsync(name).ConfigureAwait(false) ?? "";
            subscriptions.Add(await bus.ResolveServiceOwnerAsync(name,
                value => platform.Post(() => LauncherOwnerChanged(value.ServiceName, value.NewOwner ?? "")),
                _ => platform.Post(() => LauncherOwnerChanged(name, ""))).ConfigureAwait(false));
        }
        watcherOwner = await bus.ResolveServiceOwnerAsync(WatcherName).ConfigureAwait(false) ?? "";
        notificationsOwner = await bus.ResolveServiceOwnerAsync(NotificationsName).ConfigureAwait(false) ?? "";
        watcherPresent = watcherOwner.Length != 0;
        notificationsPresent = notificationsOwner.Length != 0;
        notifications = bus.CreateProxy<INotifications>(NotificationsName, "/org/freedesktop/Notifications");
        foreach (var name in new[] { WatcherName, NotificationsName })
            subscriptions.Add(await bus.ResolveServiceOwnerAsync(name,
                value => platform.Post(() => OwnerChanged(value.ServiceName, value.NewOwner ?? "")),
                _ => platform.Post(() => OwnerChanged(name, ""))).ConfigureAwait(false));
        subscriptions.Add(await notifications.WatchActionInvokedAsync(value => platform.Post(() => { if (value.Id == notificationId && !disposed) NotificationActivated?.Invoke(); })).ConfigureAwait(false));
        subscriptions.Add(await notifications.WatchNotificationClosedAsync(value => platform.Post(() => { if (value.Id == notificationId) notificationId = 0; })).ConfigureAwait(false));
        try
        {
            var settings = bus.CreateProxy<IDesktopSettings>(PortalName, "/org/freedesktop/portal/desktop");
            var values = await settings.ReadAllAsync(["org.freedesktop.appearance", "org.gnome.desktop.interface"]).ConfigureAwait(false);
            foreach (var group in values) foreach (var value in group.Value) ApplySetting(group.Key, value.Key, value.Value);
            subscriptions.Add(await settings.WatchSettingChangedAsync(value => platform.Post(() => { if (!disposed) { ApplySetting(value.Namespace,value.Key,value.Value); platform.SettingsChanged(); } })).ConfigureAwait(false));
        }
        catch (DBusErrorReplyException) { }
        return true;
    }

    private void ApplySetting(string group, string key, object value)
    {
        if (group == "org.freedesktop.appearance")
        {
            if (key == "color-scheme") Dark = value is uint scheme && scheme == 1;
            if (key == "contrast") HighContrast = value is uint contrast && contrast == 1;
            if (key == "reduced-motion") ReducedMotion = value is uint motion && motion == 1 || value is true;
        }
        if (group == "org.gnome.desktop.interface" && key == "enable-animations" && value is bool animations) ReducedMotion = !animations;
    }

    private void OwnerChanged(string name, string owner)
    {
        if (disposed || bus is null) return;
        if (name == WatcherName && owner != watcherOwner)
        {
            watcherOwner = owner;
            watcherPresent = owner.Length != 0;
            foreach (var item in trayItems.ToArray()) item.WatcherChanged();
        }
        if (name == NotificationsName && owner != notificationsOwner) { notificationsOwner = owner; notificationsPresent = owner.Length != 0; notificationId = 0; }
    }
    private void LauncherOwnerChanged(string name, string owner)
    {
        if (disposed || bus is null || launcherHosts[name] == owner) return;
        var previous = LauncherCapabilities;
        launcherHosts[name] = owner;
        if (previous != LauncherCapabilities) LauncherCapabilitiesChanged?.Invoke();
        if (owner.Length != 0 && badgeCount is { } count) SetBadgeCount(count);
    }
    internal void Observe(Task task) => _ = task.ContinueWith(completed =>
    { if (completed.Exception is { } error) Console.Error.WriteLine($"Nazor: desktop operation failed: {error.GetBaseException().Message}"); }, TaskScheduler.Default);

    public bool ShowNotification(SystemNotification notification)
    {
        platform.AssertAccess(); ArgumentNullException.ThrowIfNull(notification);
        if (!SupportsNotifications || notifications is null) return false;
        try
        {
            var appId = LinuxDesktopWindow.AppId;
            var hints = new Dictionary<string, object> { ["desktop-entry"] = appId };
            var image = notification.Icon switch
            {
                SystemNotificationIcon.Error => "dialog-error",
                SystemNotificationIcon.Warning => "dialog-warning",
                SystemNotificationIcon.Information => "dialog-information",
                _ => null
            };
            if (image is not null) hints["image-path"] = image;
            notificationId = platform.Wait(notifications.NotifyAsync("Nazor", notificationId, appId,
                notification.Title, SecurityElement.Escape(notification.Message) ?? "", ["default", "Open"], hints, -1));
            return true;
        }
        catch (Exception error) when (error is DBusErrorReplyException or DBusConnectionException or TimeoutException)
        { Console.Error.WriteLine($"Nazor: notification failed: {error.Message}"); return false; }
    }
    public void WithdrawNotification()
    {
        platform.AssertAccess();
        if (notificationId != 0 && notifications is not null) Observe(notifications.CloseNotificationAsync(notificationId));
        notificationId = 0;
    }

    internal IReadOnlyList<string>? ShowFileDialog(LinuxDesktopWindow? owner, FileDialogRequest request)
    {
        if (bus is null) throw new NotSupportedException("File dialogs require the XDG Desktop Portal session service.");
        var chooser = bus.CreateProxy<IFileChooser>(PortalName, "/org/freedesktop/portal/desktop");
        var token = "nazor_" + Guid.NewGuid().ToString("N");
        var responsePath = new ObjectPath("/org/freedesktop/portal/desktop/request/" + uniqueName.TrimStart(':').Replace('.','_') + "/" + token);
        var portalRequest = bus.CreateProxy<IPortalRequest>(PortalName,responsePath);
        var completion = new TaskCompletionSource<(uint Response, IDictionary<string, object> Results)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = platform.Wait(portalRequest.WatchResponseAsync(value => completion.TrySetResult(value)));
        using (subscription)
        {
            var options = FileOptions(request,token);
            var parent = owner?.ExportedHandle ?? "";
            try
            {
                var actualPath = platform.Wait(request.Kind == FileDialogKind.Save
                    ? chooser.SaveFileAsync(parent,request.Title ?? "Save file",options)
                    : chooser.OpenFileAsync(parent,request.Title ?? (request.Kind == FileDialogKind.Folder ? "Select folder" : "Open file"),options));
                if (actualPath != responsePath) throw new InvalidOperationException("The portal returned an unexpected request path.");
                var response = platform.Wait(completion.Task,TimeSpan.FromDays(1));
                if (response.Response != 0) return null;
                if (!response.Results.TryGetValue("uris",out var result) || result is not string[] uris) return null;
                var selected=uris.Select(uri => new Uri(uri,UriKind.Absolute)).ToArray();
                if (selected.Any(uri => !uri.IsFile)) throw new NotSupportedException("The file picker returned a non-local URI; Nazor file dialogs require local paths.");
                return selected.Select(uri => uri.LocalPath).ToArray();
            }
            catch
            {
                Observe(portalRequest.CloseAsync());
                throw;
            }
        }
    }
    internal static Dictionary<string,object> FileOptions(FileDialogRequest request, string token)
    {
        var result = new Dictionary<string,object> { ["handle_token"]=token,["modal"]=true,["multiple"]=request.Multiple && request.Kind == FileDialogKind.Open,["directory"]=request.Kind == FileDialogKind.Folder };
        if (!string.IsNullOrEmpty(request.InitialDirectory)) result["current_folder"] = Encoding.UTF8.GetBytes(Path.GetFullPath(request.InitialDirectory) + "\0");
        if (!string.IsNullOrEmpty(request.FileName))
        {
            var name = Path.GetFileName(request.FileName);
            if (request.Kind == FileDialogKind.Save && string.IsNullOrEmpty(Path.GetExtension(name)) && !string.IsNullOrWhiteSpace(request.DefaultExtension)) name += "." + request.DefaultExtension.TrimStart('.');
            if (request.Kind == FileDialogKind.Save) result["current_name"]=name;
            else result["current_file"]=Encoding.UTF8.GetBytes(Path.GetFullPath(Path.Combine(request.InitialDirectory ?? AppContext.BaseDirectory,name)) + "\0");
        }
        if (request.Filters.Count != 0) result["filters"] = request.Filters.Select(filter => (filter.Name,filter.Patterns.Select(pattern => (0u,pattern)).ToArray())).ToArray();
        return result;
    }

    public void Dispose()
    {
        if (!disposed && badgeCount is not null) ClearBadge();
        if (disposed) return; disposed = true;
        if (captionSettings is { } settings)
        {
            try { if (!settings.HasExited) settings.Kill(); }
            catch (Exception error) when (error is InvalidOperationException or Win32Exception) { }
            settings.Dispose();
        }
        foreach (var subscription in subscriptions) subscription.Dispose();
        subscriptions.Clear();
        foreach (var item in trayItems.ToArray()) item.Dispose();
        accessibility?.Dispose(); bus?.Dispose(); bus = null;
    }

}
