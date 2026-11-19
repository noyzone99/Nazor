using Microsoft.Extensions.DependencyInjection;
using Nazor.Platform;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

internal sealed class TrayTestDesktop : IPlatformDesktopIntegration
{
    public bool SupportsTray { get; set; } = true;
    public bool SupportsNotifications => true;
    public List<TrayTestNative> Items { get; } = [];
    public List<SystemNotification> Notifications { get; } = [];
    public event Action? NotificationActivated;
    public IPlatformTrayItem CreateTrayItem()
    {
        var item = new TrayTestNative(this);
        Items.Add(item);
        return item;
    }
    public void ActivateNotification() => NotificationActivated?.Invoke();
    public bool ShowNotification(SystemNotification notification) { Notifications.Add(notification); return true; }
    public void WithdrawNotification() { }
}

internal sealed class TrayTestNative(TrayTestDesktop desktop) : IPlatformTrayItem
{
    public bool IsSupported => desktop.SupportsTray;
    public bool IsVisible { get; private set; }
    public bool Disposed { get; private set; }
    public TrayIconOptions? Options { get; private set; }
    public event Action? VisibilityChanged;
    public event Action? Activated;
    public event Action? SecondaryActivated;
    public event Action<int>? MenuItemInvoked;
    public event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    public bool Show(TrayIconOptions options)
    {
        Options = options.Snapshot();
        SetVisible(IsSupported);
        return IsVisible;
    }
    public void SetVisible(bool value) { if (IsVisible != value) { IsVisible = value; VisibilityChanged?.Invoke(); } }
    public void Hide() => SetVisible(false);
    public void Activate() => Activated?.Invoke();
    public void ActivateSecondary() => SecondaryActivated?.Invoke();
    public void Invoke(int id) => MenuItemInvoked?.Invoke(id);
    public TrayMenuOpeningEventArgs OpenMenu(bool canCancel)
    {
        var args = new TrayMenuOpeningEventArgs(canCancel);
        MenuOpening?.Invoke(args);
        return args;
    }
    public void Dispose() { if (Disposed) return; Disposed = true; Hide(); }
}

/// <summary>Real Windows windows and dispatcher with controlled native tray acceptance.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class TrayTestHost(TrayTestDesktop desktop) : IPlatformHost
{
    public TaskCompletionSource<bool> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Run(Func<IPlatform, int> application) =>
        new WindowsPlatformHost().Run(platform => application(new Wrapped(platform, desktop, Ready)));
    private sealed class Wrapped(IPlatform platform, TrayTestDesktop desktop, TaskCompletionSource<bool> ready) : IPlatform
    {
        public WindowCapabilities Capabilities => platform.Capabilities;
        public IPlatformDesktopIntegration DesktopIntegration => desktop;
        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options) => platform.CreateWindow(host, options);
        public int Run(UIDispatcher dispatcher) { ready.TrySetResult(true); return platform.Run(dispatcher); }
        public void Quit(int exitCode = 0) => platform.Quit(exitCode);
    }
}
