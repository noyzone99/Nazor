namespace Nazor.Samples.Windowing;

/// <summary>One framework-owned tray, whose state survives opening and closing its windows.</summary>
public sealed class TraySession : TrayItem
{
    private readonly IApplicationLifetime lifetime;
    private readonly IWindowService windows;
    private readonly ISystemNotificationService notifications;
    private IWindow? window;
    private bool ready;
    private bool paused;
    private string message = "Hello from the Nazor tray!";
    private SystemNotificationIcon notificationIcon = SystemNotificationIcon.Information;

    public TraySession(IApplicationLifetime lifetime, IWindowService windows, ISystemNotificationService notifications)
    {
        this.lifetime = lifetime;
        this.windows = windows;
        this.notifications = notifications;
        Icon = "Assets/launcher.png";
        ToolTip = "Nazor — Windowing";
        RefreshMenu();
    }

    public event Action? Changed;
    public string Message
    {
        get => message;
        set { if (message != value) { message = value; Changed?.Invoke(); } }
    }
    public bool CanNotify => notifications.IsSupported && !Paused &&
        !string.IsNullOrWhiteSpace(Message) && Message.Length <= 255 && !Message.Contains('\0');
    public bool Paused
    {
        get => paused;
        set { if (paused != value) { paused = value; RefreshMenu(); } }
    }
    public SystemNotificationIcon NotificationIcon
    {
        get => notificationIcon;
        set { if (notificationIcon != value) { notificationIcon = value; RefreshMenu(); } }
    }

    private void RefreshMenu()
    {
        var open = new SystemTrayCommand("open", "Open window");
        open.Invoked += Open;
        var pause = new SystemTrayCheckBox("pause", "Pause notifications", paused);
        pause.CheckedChangeRequested += value => Paused = value;
        var icons = new SystemTrayRadioGroup<SystemNotificationIcon>(notificationIcon,
        [
            new(SystemNotificationIcon.Information, "Information"),
            new(SystemNotificationIcon.Warning, "Warning"),
            new(SystemNotificationIcon.Error, "Error")
        ]);
        icons.SelectionChangeRequested += value => NotificationIcon = value;
        var exit = new SystemTrayCommand("exit", "Exit");
        exit.Invoked += Exit;
        Menu = new SystemTrayMenu(
        [
            open,
            pause,
            new SystemTraySubmenu("Notification icon", new SystemTrayMenu([icons])),
            new SystemTraySeparator(),
            exit
        ]);
        Changed?.Invoke();
    }

    protected override void OnStarted()
    {
        notifications.Activated += Open;
        window = windows.MainWindow;
        if (window is not null) window.Closed += OnWindowClosed;
        ready = true;
        if (window is null && !IsVisible) Open();
    }
    protected override void OnActivated() => Open();
    protected override void OnVisibilityChanged()
    {
        Changed?.Invoke();
        if (ready && window is null && !IsVisible) Open();
    }

    public bool Notify()
    {
        Dispatcher.AssertAccess();
        return CanNotify && notifications.Show(new SystemNotification("Nazor Notification", Message, notificationIcon));
    }

    public void Open()
    {
        Dispatcher.AssertAccess();
        if (window is not { IsClosed: false })
        {
            window = windows.Open<App>(new Dictionary<string, object?> { [nameof(App.InitialTab)] = "desktop" });
            window.Closed += OnWindowClosed;
        }
        window.Show();
        window.Activate();
    }
    private void OnWindowClosed()
    {
        window!.Closed -= OnWindowClosed;
        window = null;
        if (!IsVisible) lifetime.Shutdown();
    }

    public void Exit() => lifetime.Shutdown();

    protected override void OnStopping()
    {
        ready = false;
        notifications.Activated -= Open;
        if (window is not null) window.Closed -= OnWindowClosed;
    }
}
