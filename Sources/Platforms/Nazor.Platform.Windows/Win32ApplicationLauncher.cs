using System.Globalization;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed partial class Win32ApplicationLauncher : IDisposable
{
    private sealed record Badge(int? Count, RenderedImage[] Images, string Description);
    private readonly List<Win32Window> windows = [];
    private readonly Win32LauncherShell shell = new();
    private readonly Func<nint, nint, string, bool> setOverlay;
    private Badge? badge;
    private bool badgeRequested;
    private Win32Window? target;
    private bool disposed;
    internal static readonly uint TaskbarButtonCreated = RegisterWindowMessageW("TaskbarButtonCreated");
    internal ApplicationLauncherCapabilities Capabilities => disposed ? default : new(true, true, true);

    internal Win32ApplicationLauncher(Func<nint, nint, string, bool>? setOverlay = null) => this.setOverlay = setOverlay ?? shell.SetOverlay;
    internal bool SetJumpList(IReadOnlyList<JumpListTask> tasks) => !disposed && shell.SetJumpList(tasks);
    internal void Register(Win32Window window) { windows.Add(window); Refresh(); }
    internal void Remove(Win32Window window) { windows.Remove(window); Refresh(); }
    internal void BeforeHide(Win32Window window)
    {
        if (disposed) return;
        if (target == window)
        {
            setOverlay(window.Handle, 0, "");
            target = null;
        }
        window.TaskbarReady = false;
    }
    internal void RestartTaskbar()
    {
        target = null;
        foreach (var window in windows) window.TaskbarReady = false;
        shell.ResetTaskbar();
    }

    internal bool SetBadgeCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Replace(count == 0 ? null : new(count, [], count.ToString(CultureInfo.InvariantCulture)));
    }

    internal bool SetBadgeIcon(IReadOnlyList<RenderedImage> images, string description)
    {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (description.Contains('\0') || images.Count == 0) throw new ArgumentException("Badge icons require images and accessible text without NUL.");
        var snapshot = images.Select(image =>
        {
            ArgumentNullException.ThrowIfNull(image);
            if (image.Width <= 0 || image.Height <= 0 || image.Pixels.LongLength != (long)image.Width * image.Height * 4)
                throw new ArgumentException("Badge images must contain packed BGRA pixels.", nameof(images));
            return new RenderedImage(image.Width, image.Height, image.Pixels.ToArray());
        }).ToArray();
        return Replace(new(null, snapshot, description));
    }

    internal bool ClearBadge() => Replace(null);
    private bool Replace(Badge? value)
    {
        if (disposed) return false;
        if (value is null && !badgeRequested) return true;
        var next = target is not null && Eligible(target) ? target : windows.FirstOrDefault(Eligible);
        if (next is not null && !Apply(next, value)) return false;
        if (target != next && target is { IsClosed: false }) setOverlay(target.Handle, 0, "");
        target = next;
        badge = value;
        badgeRequested = true;
        return true;
    }

    internal void Refresh()
    {
        if (disposed || !badgeRequested) return;
        var next = target is not null && Eligible(target) ? target : windows.FirstOrDefault(Eligible);
        if (target != next)
        {
            if (target is { IsClosed: false }) setOverlay(target.Handle, 0, "");
            target = next;
        }
        if (target is not null) Apply(target, badge);
    }
    private bool Eligible(Win32Window window) => windows.Contains(window) && !window.IsClosed && window.IsVisibleInTaskbar && window.TaskbarReady &&
        GetWindow(window.Handle, GW_OWNER) == 0 && IsWindowVisible(window.Handle);

    private bool Apply(Win32Window window, Badge? value)
    {
        if (value is null) return setOverlay(window.Handle, 0, "");
        var size = Math.Max(16, (int)MathF.Ceiling(16 * window.Scale));
        var source = value.Count is null ? value.Images.OrderBy(candidate => Math.Abs(Math.Max(candidate.Width, candidate.Height) - size)).First() : null;
        var image = WindowsBadgeRenderer.Render(value.Count, source, window.BadgeAppearance, size);
        var icon = Win32Window.CreateIcon(image);
        try { return setOverlay(window.Handle, icon, value.Description); }
        finally { DestroyIcon(icon); }
    }

    internal static RenderedImage RenderCount(int count, int size) => WindowsBadgeRenderer.Render(count, null, new(), size);

    public void Dispose()
    {
        if (disposed) return;
        ClearBadge();
        disposed = true;
        windows.Clear();
        shell.Dispose();
    }
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)] private static partial uint RegisterWindowMessageW(string message);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DestroyIcon(nint icon);
}
