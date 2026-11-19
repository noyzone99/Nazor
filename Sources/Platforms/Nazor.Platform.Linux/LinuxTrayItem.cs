using System.Runtime.InteropServices;
using Nazor.Graphics;
using SkiaSharp;
using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

/// <summary>One SNI and DBusMenu pair. A separate bus connection lets Hide remove only this registration.</summary>
internal sealed class LinuxTrayItem : IPlatformTrayItem, IStatusNotifierItem
{
    private readonly LinuxDesktopPlatform platform;
    private readonly LinuxDesktopIntegration desktop;
    private readonly TrayMenu menu;
    private TrayIconOptions? options;
    private DesktopConnection? bus;
    private readonly string itemId;
    internal string BusName { get; private set; } = "";
    private bool exported, disposed;
    private int registrationVersion, publicationVersion;
    private (int Width, int Height, byte[] Bytes)[] pixmaps = [];
    private Action? newIcon, newToolTip;
    private Action<string>? newStatus;
    public ObjectPath ObjectPath { get; }
    public ObjectPath MenuPath => menu.ObjectPath;
    public bool IsSupported => !disposed && desktop.SupportsTray;
    public bool IsVisible { get; private set; }
    public event Action? VisibilityChanged;
    public event Action? Activated;
    public event Action? SecondaryActivated;
    public event Action<int>? MenuItemInvoked;
    public event Action<TrayMenuOpeningEventArgs>? MenuOpening;

    internal LinuxTrayItem(LinuxDesktopPlatform platform, LinuxDesktopIntegration desktop, int id)
    {
        this.platform = platform;
        this.desktop = desktop;
        itemId = LinuxDesktopWindow.AppId + "." + id;
        ObjectPath = new("/StatusNotifierItem");
        menu = new(this, new("/Menu"));
    }
    public bool Show(TrayIconOptions value)
    {
        platform.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        value = value.Snapshot();
        if (desktop.Bus is null) return false;
        var publication = ++publicationVersion;
        try
        {
            if (!exported)
            {
                var created = platform.Wait(ExportAsync());
                if (disposed || publication != publicationVersion) { created.Connection.Dispose(); return false; }
                bus = created.Connection;
                BusName = created.Name;
                exported = true;
            }
            options = value;
            pixmaps = value.Icon is null ? [] : [(value.Icon.Width, value.Icon.Height, Argb(value.Icon))];
            menu.Update(value.Menu);
            newIcon?.Invoke();
            newToolTip?.Invoke();
            newStatus?.Invoke("Active");
            return desktop.SupportsTray && platform.Wait(RegisterAsync());
        }
        catch (Exception error) when (error is DBusErrorReplyException or DBusConnectionException or TimeoutException or IOException or ObjectDisposedException)
        {
            if (!disposed && publication == publicationVersion)
            { SetVisible(false); Console.Error.WriteLine($"Nazor: tray registration failed: {error.Message}"); }
            return false;
        }
    }
    private async Task<(DesktopConnection Connection, string Name)> ExportAsync()
    {
        var connection = new DesktopConnection(DBusAddress.Session!);
        try
        {
            var name = (await connection.ConnectAsync().ConfigureAwait(false)).LocalName;
            await connection.RegisterObjectsAsync([this, menu]).ConfigureAwait(false);
            return (connection, name);
        }
        catch { connection.Dispose(); throw; }
    }
    private async Task<bool> RegisterAsync()
    {
        var version = ++registrationVersion;
        var watcher = bus!.CreateProxy<IStatusNotifierWatcher>("org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher");
        await watcher.RegisterStatusNotifierItemAsync(ObjectPath.ToString()).ConfigureAwait(false);
        var visible = (bool)await watcher.GetAsync("IsStatusNotifierHostRegistered").ConfigureAwait(false);
        return await platform.InvokeAsync(() =>
        {
            if (disposed || options is null || version != registrationVersion || !desktop.SupportsTray) return false;
            SetVisible(visible);
            return visible;
        }).ConfigureAwait(false);
    }
    internal void WatcherChanged()
    {
        ++registrationVersion;
        SetVisible(false);
        if (!disposed && options is not null && desktop.SupportsTray) desktop.Observe(RegisterAsync());
    }
    private void SetVisible(bool value)
    {
        if (IsVisible == value) return;
        IsVisible = value;
        VisibilityChanged?.Invoke();
    }
    public void Hide()
    {
        platform.AssertAccess();
        ++registrationVersion;
        ++publicationVersion;
        options = null;
        if (exported)
        {
            bus?.UnregisterObject(this);
            bus?.UnregisterObject(menu);
            // SNI has no unregister method. Releasing this connection removes the item from the watcher.
            bus?.Dispose();
            bus = null;
            BusName = "";
            exported = false;
        }
        menu.Update([]);
        SetVisible(false);
    }
    public void Dispose()
    {
        if (disposed) return;
        platform.AssertAccess();
        disposed = true;
        try { Hide(); } finally { desktop.RemoveTray(this); }
    }
    public Task ActivateAsync(int x, int y) => platform.InvokeAsync(() => { if (!disposed && IsVisible) Activated?.Invoke(); return true; });
    public Task SecondaryActivateAsync(int x, int y) => platform.InvokeAsync(() => { if (!disposed && IsVisible) SecondaryActivated?.Invoke(); return true; });
    public Task ContextMenuAsync(int x, int y) => platform.InvokeAsync(() => { if (!disposed && IsVisible) MenuOpening?.Invoke(new(false)); return true; });
    public Task ScrollAsync(int delta, string orientation) => Task.CompletedTask;
    public Task<object> GetAsync(string property) => platform.InvokeAsync(() => Properties()[property]);
    public Task<IDictionary<string, object>> GetAllAsync() => platform.InvokeAsync<IDictionary<string, object>>(Properties);
    private Dictionary<string, object> Properties() => new()
    {
        ["Category"] = "ApplicationStatus", ["Id"] = itemId, ["Title"] = options?.ToolTip ?? "Nazor",
        ["Status"] = options is null ? "Passive" : "Active", ["WindowId"] = 0,
        ["IconName"] = options?.Icon is null ? "application-x-executable" : "", ["IconPixmap"] = pixmaps,
        ["OverlayIconName"] = "", ["OverlayIconPixmap"] = Array.Empty<(int,int,byte[])>(),
        ["AttentionIconName"] = "", ["AttentionIconPixmap"] = Array.Empty<(int,int,byte[])>(),
        ["AttentionMovieName"] = "", ["ItemIsMenu"] = false, ["Menu"] = menu.ObjectPath,
        ["ToolTip"] = ("", pixmaps, options?.ToolTip ?? "Nazor", "")
    };
    public Task<IDisposable> WatchNewIconAsync(Action handler) { newIcon += handler; return Task.FromResult<IDisposable>(new BusSubscription(() => newIcon -= handler)); }
    public Task<IDisposable> WatchNewToolTipAsync(Action handler) { newToolTip += handler; return Task.FromResult<IDisposable>(new BusSubscription(() => newToolTip -= handler)); }
    public Task<IDisposable> WatchNewStatusAsync(Action<string> handler) { newStatus += handler; return Task.FromResult<IDisposable>(new BusSubscription(() => newStatus -= handler)); }

    private static byte[] Argb(RenderedImage image)
    {
        var bytes = new byte[image.Pixels.Length];
        for (var offset = 0; offset < bytes.Length; offset += 4)
        {
            var alpha = image.Pixels[offset + 3];
            bytes[offset] = alpha;
            for (var component = 0; component < 3; component++)
                bytes[offset + 1 + component] = alpha == 0 ? (byte)0 : (byte)Math.Min(255, image.Pixels[offset + 2 - component] * 255 / alpha);
        }
        return bytes;
    }

    private sealed class TrayMenu(LinuxTrayItem owner, ObjectPath path) : ITrayMenu
    {
        private sealed record Entry(NativeTrayMenuItem Item, bool CanInvoke, int[] Children, byte[]? Icon);
        private Dictionary<int, Entry> entries = [];
        private int[] roots = [];
        private uint revision;
        private Action<(uint Revision, int Parent)>? changed;
        public ObjectPath ObjectPath => path;
        internal void Update(IReadOnlyList<NativeTrayMenuItem> items)
        {
            var next = new Dictionary<int, Entry>();

            int[] Add(IReadOnlyList<NativeTrayMenuItem> children, bool parentEnabled)
            {
                return children.Select(item =>
                {
                    var id = item.Id;
                    var enabled = parentEnabled && item.Visible && item.Enabled;
                    next.Add(id, new(item, enabled, Add(item.Children, enabled), item.Icon is null ? null : Png(item.Icon)));
                    return id;
                }).ToArray();
            }
            roots = Add(items, true);
            entries = next;
            changed?.Invoke((++revision, 0));
        }
        private static byte[] Png(RenderedImage image)
        {
            using var bitmap = new SKBitmap(new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            Marshal.Copy(image.Pixels, 0, bitmap.GetPixels(), image.Pixels.Length);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        private Entry Find(int id) => entries.TryGetValue(id, out var entry) ? entry
            : throw new DBusErrorReplyException("com.canonical.dbusmenu.Error.InvalidMenuItem", "Unknown menu item.");
        private Dictionary<string, object> Properties(int id)
        {
            if (id == 0) return new() { ["children-display"] = "submenu" };
            var entry = Find(id);
            var item = entry.Item;
            if (item.Kind == NativeTrayMenuItemKind.Separator) return new() { ["type"] = "separator" };
            var result = new Dictionary<string, object> { ["label"] = item.Text, ["enabled"] = item.Enabled, ["visible"] = item.Visible };
            if (item.Kind == NativeTrayMenuItemKind.Submenu) result["children-display"] = "submenu";
            if (item.Kind is NativeTrayMenuItemKind.CheckBox or NativeTrayMenuItemKind.Radio)
            {
                result["toggle-type"] = item.Kind == NativeTrayMenuItemKind.Radio ? "radio" : "checkmark";
                result["toggle-state"] = item.IsChecked ? 1 : 0;
            }
            if (entry.Icon is not null) result["icon-data"] = entry.Icon;
            return result;
        }
        private (int, IDictionary<string, object>, object[]) Layout(int id, int depth, string[] names) =>
            (id, Filter(Properties(id), names), depth == 0 ? [] : (id == 0 ? roots : Find(id).Children).Select(child => (object)Layout(child, depth < 0 ? -1 : depth - 1, names)).ToArray());
        private static IDictionary<string, object> Filter(Dictionary<string, object> values, string[] names) =>
            names.Length == 0 ? values : values.Where(pair => names.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        public Task<(uint, (int, IDictionary<string, object>, object[]))> GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames) =>
            owner.platform.InvokeAsync(() => (revision, Layout(parentId, recursionDepth, propertyNames)));
        public Task<(int, IDictionary<string, object>)[]> GetGroupPropertiesAsync(int[] ids, string[] names) =>
            owner.platform.InvokeAsync(() => (ids.Length == 0 ? new[] { 0 }.Concat(entries.Keys) : ids).Select(id => (id, Filter(Properties(id), names))).ToArray());
        public Task<object> GetPropertyAsync(int id, string name) => owner.platform.InvokeAsync(() => Properties(id)[name]);
        public Task EventAsync(int id, string eventId, object data, uint timestamp) => owner.platform.InvokeAsync(() =>
        {
            if (!owner.disposed && owner.IsVisible && eventId == "clicked" && entries.TryGetValue(id, out var entry) && entry.CanInvoke
                && entry.Item.Kind is NativeTrayMenuItemKind.Command or NativeTrayMenuItemKind.CheckBox or NativeTrayMenuItemKind.Radio)
                owner.MenuItemInvoked?.Invoke(entry.Item.Id);
            return true;
        });
        public async Task<int[]> EventGroupAsync((int Id, string EventId, object Data, uint Timestamp)[] events)
        { foreach (var value in events) await EventAsync(value.Id, value.EventId, value.Data, value.Timestamp).ConfigureAwait(false); return []; }
        public Task<bool> AboutToShowAsync(int id) => owner.platform.InvokeAsync(() =>
        {
            var previous = revision;
            if (id == 0 && !owner.disposed && owner.IsVisible) owner.MenuOpening?.Invoke(new(false));
            return revision != previous;
        });
        public async Task<(int[], int[])> AboutToShowGroupAsync(int[] ids)
        {
            var updated = new List<int>();
            foreach (var id in ids) if (await AboutToShowAsync(id).ConfigureAwait(false)) updated.Add(id);
            return (updated.ToArray(), []);
        }
        public Task<object> GetAsync(string property) => Task.FromResult(MenuProperties()[property]);
        public Task<IDictionary<string, object>> GetAllAsync() => Task.FromResult<IDictionary<string, object>>(MenuProperties());
        private static Dictionary<string, object> MenuProperties() => new() { ["Version"] = 3u, ["TextDirection"] = "ltr", ["Status"] = "normal", ["IconThemePath"] = Array.Empty<string>() };
        public Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint Revision, int Parent)> handler)
        { changed += handler; return Task.FromResult<IDisposable>(new BusSubscription(() => changed -= handler)); }
    }
}
