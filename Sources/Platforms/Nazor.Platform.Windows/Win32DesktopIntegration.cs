using System.ComponentModel;
using System.Runtime.InteropServices;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32DesktopIntegration : IPlatformDesktopIntegration, IPlatformTrayItem, IMessageTarget, IDisposable
{
    private readonly Win32ApplicationLauncher? launcher;
    public ApplicationLauncherCapabilities LauncherCapabilities => launcher?.Capabilities ?? default;
    public bool SetJumpList(IReadOnlyList<JumpListTask> tasks) { AssertAccess(); return launcher?.SetJumpList(tasks) is true; }
    public bool SetBadgeCount(int count) { AssertAccess(); return launcher?.SetBadgeCount(count) is true; }
    public bool SetBadgeIcon(IReadOnlyList<Nazor.Graphics.RenderedImage> images, string description) { AssertAccess(); return launcher?.SetBadgeIcon(images, description) is true; }
    public bool ClearBadge() { AssertAccess(); return launcher?.ClearBadge() is true; }
    public bool SupportsTray => true;
    public bool IsSupported => true;
    public IPlatformTrayItem CreateTrayItem() { AssertAccess(); return new Win32DesktopIntegration(notify, trackMenu); }
    public bool SupportsNotifications => true;

    internal const uint CallbackMessage = WM_APP + 3;
    internal const uint TrayId = 1;
    private const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIM_SETFOCUS = 3, NIM_SETVERSION = 4;
    private const uint NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4, NIF_INFO = 0x10, NIF_SHOWTIP = 0x80;
    private const uint NIN_SELECT = 0x400, NIN_KEYSELECT = 0x401, WM_CONTEXTMENU = 0x7B;
    internal const uint BalloonHide = 0x403, BalloonTimeout = 0x404, BalloonClick = 0x405;
    internal delegate bool NotifyIcon(uint command, ref IconData data);
    internal delegate uint TrackMenu(nint menu, int x, int y, nint owner);
    private readonly NotifyIcon notify;
    private readonly TrackMenu trackMenu;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    internal readonly uint TaskbarCreated = RegisterWindowMessageW("TaskbarCreated");
    private GCHandle self;
    private nint handle;
    private nint trayIcon;
    private bool ownsTrayIcon;
    private TrayIconOptions? trayOptions;
    private uint notificationId;
    private ushort nextNotificationId = 1;
    private bool disposed;

    internal Win32DesktopIntegration(NotifyIcon? notify = null, TrackMenu? trackMenu = null, Win32ApplicationLauncher? launcher = null)
    {
        this.launcher = launcher;
        this.notify = notify ?? NativeNotify;
        this.trackMenu = trackMenu ?? ((menu, x, y, owner) => TrackPopupMenuEx(menu, 0x100 | 0x2, x, y, owner, 0));
        self = GCHandle.Alloc(this);
        fixed (char* name = Win32Platform.ClassName)
            handle = CreateWindowExW(0x80, name, "Nazor.Desktop", 0, 0, 0, 0, 0, 0, 0, Win32Platform.Instance, GCHandle.ToIntPtr(self));
        if (handle == 0)
        {
            if (self.IsAllocated) self.Free();
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    internal nint Handle => handle;
    public bool IsVisible { get; private set; }
    public event Action? VisibilityChanged;
    public event Action? Activated;
    public event Action<int>? MenuItemInvoked;
    public event Action? SecondaryActivated;
    public event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    public event Action? NotificationActivated;

    public bool Show(TrayIconOptions options)
    {
        AssertAccess();
        ArgumentNullException.ThrowIfNull(options);
        options = options.Snapshot();
        var newIcon = options.Icon is null ? LoadIconW(0, 32512) : Win32Window.CreateIcon(options.Icon);
        var data = Data(TrayId);
        data.Flags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.Icon = newIcon;
        Copy(options.ToolTip, data.Tip);
        var accepted = notify(IsVisible ? NIM_MODIFY : NIM_ADD, ref data);
        if (!accepted)
        {
            if (options.Icon is not null) DestroyIcon(newIcon);
            return false;
        }
        if (!IsVisible && !SetVersion(ref data))
        {
            if (options.Icon is not null) DestroyIcon(newIcon);
            return false;
        }
        if (ownsTrayIcon) DestroyIcon(trayIcon);
        trayIcon = newIcon;
        ownsTrayIcon = options.Icon is not null;
        trayOptions = options;
        SetVisible(true);
        return true;
    }

    public void Hide()
    {
        AssertAccess();
        var data = Data(TrayId);
        notify(NIM_DELETE, ref data);
        trayOptions = null;
        if (ownsTrayIcon) DestroyIcon(trayIcon);
        trayIcon = 0;
        ownsTrayIcon = false;
        SetVisible(false);
    }

    public bool ShowNotification(SystemNotification notification)
    {

        AssertAccess();
        ArgumentNullException.ThrowIfNull(notification);
        ValidateText(notification.Title, 63, nameof(notification.Title));
        ValidateText(notification.Message, 255, nameof(notification.Message));
        if (!Enum.IsDefined(notification.Icon)) throw new ArgumentOutOfRangeException(nameof(notification));
        WithdrawNotification();

        if (++nextNotificationId < 2) nextNotificationId = 2;
        var data = Data(nextNotificationId);
        data.Flags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_INFO | NIF_SHOWTIP;
        data.Icon = LoadIconW(0, 32512);
        data.InfoFlags = (uint)notification.Icon | 0x80;
        Copy(notification.Title, data.Tip);
        Copy(notification.Title, data.InfoTitle);
        Copy(notification.Message, data.Info);
        if (!notify(NIM_ADD, ref data)) return false;
        notificationId = data.Id;
        if (!SetVersion(ref data)) { notificationId = 0; return false; }
        return true;
    }

    public void WithdrawNotification()
    {
        AssertAccess();
        if (notificationId == 0) return;
        var data = Data(notificationId);
        notificationId = 0;

        notify(NIM_DELETE, ref data);
    }

    public nint OnMessage(nint hwnd, uint message, nint wParam, nint lParam)
    {
        if (message == TaskbarCreated && TaskbarCreated != 0)
        {
            launcher?.RestartTaskbar();
            notificationId = 0;
            var wasVisible = IsVisible;
            IsVisible = false;
            if (trayOptions is not null)
            {
                var data = Data(TrayId);
                data.Flags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
                data.Icon = trayIcon;
                Copy(trayOptions.ToolTip, data.Tip);
                IsVisible = notify(NIM_ADD, ref data);
                if (IsVisible) IsVisible = SetVersion(ref data);
            }
            if (wasVisible != IsVisible) VisibilityChanged?.Invoke();
            return 0;
        }
        if (message != CallbackMessage) return DefWindowProcW(hwnd, message, wParam, lParam);
        var kind = (uint)(ulong)lParam & 0xFFFF;
        var id = ((uint)(ulong)lParam >> 16) & 0xFFFF;
        if (id == TrayId && IsVisible)
        {
            if (kind is NIN_SELECT or NIN_KEYSELECT) Activated?.Invoke();
            else if (kind == 0x208 /* WM_MBUTTONUP */) SecondaryActivated?.Invoke();
            else if (kind == WM_CONTEXTMENU) ShowMenu(wParam);
        }
        else if (id != 0 && id == notificationId)
        {
            if (kind is BalloonHide or BalloonTimeout or BalloonClick)
            {
                WithdrawNotification();
                if (kind == BalloonClick) NotificationActivated?.Invoke();
            }
        }
        return 0;
    }

    private void ShowMenu(nint coordinates)
    {
        var opening = new TrayMenuOpeningEventArgs(true);
        MenuOpening?.Invoke(opening);
        if (opening.IsCanceled || disposed || !IsVisible || trayOptions is not { } options || options.Menu.Count == 0) return;
        var bitmaps = new List<nint>();
        nint menu = 0;
        var commands = new HashSet<int>();
        try
        {
            menu = BuildMenu(options.Menu, true);
            var x = (short)((long)coordinates & 0xFFFF);
            var y = (short)(((long)coordinates >> 16) & 0xFFFF);
            SetForegroundWindow(handle);
            var command = trackMenu(menu, x, y, handle);
            if (disposed) return;
            PostMessageW(handle, 0, 0, 0);
            var data = Data(TrayId);
            notify(NIM_SETFOCUS, ref data);
            if (command <= int.MaxValue && commands.Contains((int)command) && trayOptions is not null && CanInvoke(trayOptions.Menu, (int)command))
                MenuItemInvoked?.Invoke((int)command);
        }
        finally
        {
            if (menu != 0) DestroyMenu(menu);
            foreach (var bitmap in bitmaps) DeleteObject(bitmap);
        }
        static bool CanInvoke(IReadOnlyList<NativeTrayMenuItem> items, int id) => items.Any(item => item.Visible && item.Enabled &&
            (item.Kind == NativeTrayMenuItemKind.Submenu ? CanInvoke(item.Children, id) : item.Id == id && item.Kind != NativeTrayMenuItemKind.Separator));

        nint BuildMenu(IReadOnlyList<NativeTrayMenuItem> items, bool parentEnabled)
        {
            var result = CreatePopupMenu();
            if (result == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            try
            {
                uint position = 0;
                foreach (var item in items.Where(item => item.Visible))
                {
                    var enabled = parentEnabled && item.Enabled;
                    var info = new MenuItemInfo
                    {
                        Size = (uint)sizeof(MenuItemInfo), Mask = 0x100 | 0x40 | 0x2 | 0x1,
                        Type = item.Kind == NativeTrayMenuItemKind.Separator ? 0x800u : item.Kind == NativeTrayMenuItemKind.Radio ? 0x200u : 0,
                        State = (enabled ? 0u : 3u) | (item.IsChecked ? 8u : 0), Id = (uint)item.Id
                    };
                    if (item.Icon is not null) { info.Bitmap = CreateMenuBitmap(item.Icon); bitmaps.Add(info.Bitmap); info.Mask |= 0x80; }
                    if (item.Kind == NativeTrayMenuItemKind.Submenu)
                    { info.Submenu = BuildMenu(item.Children, enabled); info.Mask |= 4; }
                    else if (enabled && item.Kind != NativeTrayMenuItemKind.Separator) commands.Add(item.Id);
                    fixed (char* text = item.Text)
                    {
                        info.Text = text;
                        if (!InsertMenuItemW(result, position++, true, ref info))
                        {
                            if (info.Submenu != 0) DestroyMenu(info.Submenu);
                            throw new Win32Exception(Marshal.GetLastPInvokeError());
                        }
                    }
                }
                return result;
            }
            catch { DestroyMenu(result); throw; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MenuItemInfo
    {
        public uint Size, Mask, Type, State, Id;
        public nint Submenu, CheckedBitmap, UncheckedBitmap;
        public nuint Data;
        public char* Text;
        public uint TextLength;
        public nint Bitmap;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, ImageSize;
        public int XPixelsPerMeter, YPixelsPerMeter;
        public uint ColorsUsed, ImportantColors;
    }
    private static nint CreateMenuBitmap(Nazor.Graphics.RenderedImage image)
    {
        var header = new BitmapHeader { Size = (uint)sizeof(BitmapHeader), Width = image.Width, Height = -image.Height, Planes = 1, BitCount = 32 };
        var bitmap = CreateDIBSection(0, &header, 0, out var pixels, 0, 0);
        if (bitmap == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        Marshal.Copy(image.Pixels, 0, pixels, image.Pixels.Length);
        return bitmap;
    }
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InsertMenuItemW(nint menu, uint position, [MarshalAs(UnmanagedType.Bool)] bool byPosition, ref MenuItemInfo info);
    [LibraryImport("gdi32.dll", SetLastError = true)] private static partial nint CreateDIBSection(nint dc, BitmapHeader* header, uint usage, out nint bits, nint section, uint offset);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeleteObject(nint bitmap);

    private IconData Data(uint id) => new() { Size = (uint)sizeof(IconData), Window = handle, Id = id, Callback = CallbackMessage };
    private bool SetVersion(ref IconData data)
    {
        data.Version = 4;
        if (notify(NIM_SETVERSION, ref data)) return true;
        notify(NIM_DELETE, ref data);
        return false;
    }

    private void SetVisible(bool value)
    {
        if (IsVisible == value) return;
        IsVisible = value;
        VisibilityChanged?.Invoke();
    }

    private static void ValidateText(string text, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, name);
        if (text.Length > maximum || text.Contains('\0')) throw new ArgumentException($"Expected at most {maximum} UTF-16 characters without NUL.", name);
    }

    private static void Copy(string text, char* destination)
    {
        text.AsSpan().CopyTo(new Span<char>(destination, text.Length));
        destination[text.Length] = '\0';
    }

    private void AssertAccess()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Desktop services require the UI thread.");
    }

    public void Dispose()
    {
        if (disposed) return;
        AssertAccess();
        Hide();
        WithdrawNotification();
        disposed = true;
        if (handle != 0) DestroyWindow(handle);
    }

    public void OnFinalMessage()
    {
        handle = 0;
        if (self.IsAllocated) self.Free();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IconData
    {
        public uint Size;
        public nint Window;
        public uint Id, Flags, Callback;
        public nint Icon;
        public fixed char Tip[128];
        public uint State, StateMask;
        public fixed char Info[256];
        public uint Version;
        public fixed char InfoTitle[64];
        public uint InfoFlags;
        public Guid Guid;
        public nint BalloonIcon;
    }

    private static bool NativeNotify(uint command, ref IconData data) => Shell_NotifyIconW(command, ref data);
    [LibraryImport("shell32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool Shell_NotifyIconW(uint command, ref IconData data);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial uint RegisterWindowMessageW(string name);
    [LibraryImport("user32.dll")] private static partial nint LoadIconW(nint instance, nint name);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DestroyIcon(nint icon);
    [LibraryImport("user32.dll", SetLastError = true)] private static partial nint CreatePopupMenu();
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DestroyMenu(nint menu);
    [LibraryImport("user32.dll")] private static partial uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);
}
