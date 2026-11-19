namespace Nazor.Platform;

/// <summary>Native desktop services owned by the platform. Operations run on the UI thread.</summary>
public interface IPlatformDesktopIntegration
{
    /// <summary>Gets independent application launcher features; unsupported hosts return no capabilities.</summary>
    ApplicationLauncherCapabilities LauncherCapabilities => default;
    /// <summary>Occurs when launcher support changes. Callbacks run on the UI thread.</summary>
    event Action? LauncherCapabilitiesChanged { add { } remove { } }
    /// <summary>Publishes a validated task snapshot for the current application identity.</summary>
    bool SetJumpList(IReadOnlyList<JumpListTask> tasks) => false;
    /// <summary>Replaces the application's badge with a count; zero clears either badge kind.</summary>
    bool SetBadgeCount(int count) => false;
    /// <summary>Publishes premultiplied BGRA icon variants and accessible text. The backend owns a copy after acceptance.</summary>
    bool SetBadgeIcon(IReadOnlyList<Nazor.Graphics.RenderedImage> images, string description) => false;
    /// <summary>Clears the application's badge without changing launcher tasks.</summary>
    bool ClearBadge() => false;
    /// <summary>Gets whether the desktop currently provides a native tray host.</summary>
    bool SupportsTray => false;
    /// <summary>Gets whether native notifications are currently available.</summary>
    bool SupportsNotifications => false;
    /// <summary>Creates a separately owned native tray item. The caller must dispose it on the UI thread.</summary>
    IPlatformTrayItem? CreateTrayItem() => null;
    /// <summary>Occurs when the user activates the current system notification.</summary>
    event Action? NotificationActivated;
    /// <summary>Replaces the current notification. Acceptance does not guarantee display.</summary>
    bool ShowNotification(SystemNotification notification);
    /// <summary>Withdraws the current notification without promising to clear shell history.</summary>
    void WithdrawNotification();
}

/// <summary>One independently owned native tray registration. Callbacks run synchronously on the UI thread.</summary>
public interface IPlatformTrayItem : IDisposable
{
    /// <summary>Gets whether a tray host is currently available.</summary>
    bool IsSupported { get; }
    /// <summary>Gets visibility known to the backend; shell overflow placement is not guaranteed.</summary>
    bool IsVisible { get; }
    /// <summary>Occurs when the backend detects a visibility change.</summary>
    event Action? VisibilityChanged;
    /// <summary>Occurs for primary activation, typically a left click or keyboard activation.</summary>
    event Action? Activated;
    /// <summary>Occurs for alternative activation, typically a middle click, when reported by the desktop.</summary>
    event Action? SecondaryActivated;
    /// <summary>Reports a native command ID from the current menu snapshot on the UI thread.</summary>
    event Action<int>? MenuItemInvoked;
    /// <summary>Occurs synchronously before menu presentation when reported by the desktop. Cancellation is backend-dependent.</summary>
    event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    /// <summary>Publishes the current icon and menu. True means accepted by the shell, not guaranteed placement.</summary>
    bool Show(TrayIconOptions options);
    /// <summary>Removes the native icon while retaining the managed item and its state.</summary>
    void Hide();
}
