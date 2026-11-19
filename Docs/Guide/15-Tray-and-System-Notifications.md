# 15. Tray and system notifications

Nazor owns independently scoped tray items, each with its own native icon, typed menu and lifecycle. Windows draws tray menus with Win32. Wayland and X11 share StatusNotifierItem and DBusMenu integration, with the desktop drawing the tray menus. DRM/KMS has no desktop tray or notification service. [Backend support matrix](32-Support-and-Limitations.md#backend-support-matrix)

## Register and obtain an item

```csharp
var builder = NazorApplication.CreateBuilder(args);
builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
builder.Application.UseTrayItem<NotificationsTray>();
using var app = builder.Build();
return app.Run();
```

`NotificationsTray` implements `ITrayItem`, normally by deriving from `TrayItem`. Constructor dependencies are resolved in a scope owned by that item. Do not also register it as a singleton or resolve its type from a window scope: that would be a different scope and instance. Inject `ISystemTray` and call `Get<NotificationsTray>()` to access the active instance. Get never constructs an item and throws before Run, after stop, or for an unregistered/inactive type.

UseTrayItem is additive: combine UseMainWindow or CreateMainWindow with any number of distinct tray types. Each type can be registered once. Desktop startup requires an initial window or at least one tray. Tray-only startup requires explicit shutdown; Build rejects other policies rather than silently changing them. UseMainView is a separate single-view-host mode and cannot be combined with tray items.

Register a `TrayItem` with `builder.Application.UseTrayItem<T>()` to make it part of desktop startup. Put tray state and startup work in that item's lifecycle. Hosted services remain available for background work but do not constitute a desktop startup surface.

## Typed native menus

```csharp
var open = new SystemTrayCommand("open", "Open");
open.Invoked += OpenWindow;

var pause = new SystemTrayCheckBox("pause", "Pause", paused);
pause.CheckedChangeRequested += value =>
{
    paused = value;
    RefreshMenu();
};

var modes = new SystemTrayRadioGroup<SyncMode>(mode,
[
    new(SyncMode.Automatic, "Automatic"),
    new(SyncMode.Manual, "Manual")
]);
modes.SelectionChangeRequested += value =>
{
    mode = value;
    RefreshMenu();
};

Menu = new SystemTrayMenu(
[
    open,
    pause,
    new SystemTraySubmenu("Mode", new SystemTrayMenu([modes])),
    new SystemTraySeparator()
]);
```

This example belongs in a TrayItem with application-defined OpenWindow, RefreshMenu, state and SyncMode enum. A submenu requires a separate SystemTrayMenu. A radio group expands inline into its parent menu; it does not implicitly create a submenu.

Commands, checkboxes, submenu headings and radio options expose Text, Enabled, Visible and optional ImageSource Icon. Command/checkbox IDs must be unique throughout one tray's menu tree, including submenus. Native IDs are generated internally and are not application event payloads. Separators have no command state. Radio values are unique and SelectedValue must match exactly one option.

Checkbox and radio events request a value; they do not mutate IsChecked or SelectedValue. Publish a replacement menu to accept the change. A radio handler receives TValue directly. Applications may reject a request without leaving a misleading native selection.

Menus and option collections copy their inputs into read-only lists. Updating a list later does not mutate the published menu. Replace Menu to update it; the framework rejects commands from replaced menus, hidden/disabled entries, disabled ancestors and stopped items. There is no parent MenuItemInvoked event: subscribe to each typed item.

## Icon, visibility and lifecycle

TrayItem provides Icon, ToolTip, Menu, IsSupported, IsVisible, IsStarted and Dispatcher. Configure properties in the constructor without creating native resources. Icon defaults to the application/system icon. Custom ImageSource icons, including menu icons, load asynchronously; stale or canceled loads cannot restore a hidden/stopped item. Image failures are reported on the UI dispatcher. Relative file sources resolve from the executable directory. Linux hosts can choose whether to display menu icons.

ToolTip must be nonempty, contain no NUL and fit 127 UTF-16 units. Menu labels and command IDs must be nonempty without NUL. Menu nesting is limited to 32 levels and one snapshot to 65,535 native entries.

During Run the framework creates all item scopes, attaches a native context and attempts to show each item before OnStarted. Inspect IsVisible there and open fallback UI if necessary. IsSupported can change with desktop-service availability. A false Show result means the shell did not accept/show the registration. IsVisible does not guarantee placement outside the shell overflow area.

Show publishes the current properties. Hide removes the native registration while retaining state and scope; changing properties while hidden does not show it. OnStopping runs before native resources are detached; OnStopped follows detachment. Stop is terminal, Dispose is idempotent, and the host disposes each item's scope before disposing the platform. Hooks and external lifecycle events are separate: overriding a hook does not require base.OnStarted to keep external subscribers working.

Desktop operations and callbacks run on the UI thread. Marshal worker calls through Dispatcher.InvokeAsync. Start and Stop on ITrayItem are infrastructure methods called by the framework; TrayItem implements them explicitly.

Native tray registrations recover independently after Explorer/watcher restarts. Offer a fallback window or an explicit exit if recovery fails. Startup with no accepted tray icon, no fallback window and no shutdown request fails clearly. Hiding an item later does not implicitly end an explicit application. Window closing follows the configured shutdown policy even if tray items exist.

## Icon interaction and canceling a menu

ITrayItem exposes Started, Stopped, Activated, SecondaryActivated, VisibilityChanged and MenuOpening, with corresponding protected hooks on TrayItem plus OnStopping.

Activated means primary activation, typically left click or keyboard. SecondaryActivated typically means middle click; it is not the right-click event. MenuOpening is a separate presentation request. The Linux desktop can map gestures differently, so raw left/right/double click are not a portable contract.

```csharp
protected override void OnActivated() => OpenWindow();

protected override void OnMenuOpening(TrayMenuOpeningEventArgs args)
{
    if (args.TryCancel())
        OpenWindow();
}
```

MenuOpening uses EventHandler<TrayMenuOpeningEventArgs>. On Windows it runs synchronously before TrackPopupMenuEx and can prevent the default opening. On Linux the desktop draws the exported menu; AboutToShow reports opening when supplied by the host, with CanCancel=false. No universal Linux veto is promised.

TryCancel returns false when cancellation is unsupported. Accepted cancellation is sticky, does not stop other subscribers, and prevents the default menu rather than changing command behavior. Make the decision before returning; cancellation after an await is too late. The hook runs before external subscribers.

## Native notifications

ISystemNotificationService remains application-wide and independent of tray items and windows.

```csharp
notifications.Show(new SystemNotification("Export complete", "The file is ready."));
```

On Windows the service uses a separate temporary Shell icon and transient banners. Titles/messages are nonempty without NUL and limited to 63/255 UTF-16 units. Linux uses org.freedesktop.Notifications, including default activation and withdrawal. Each Show replaces the current request. Withdraw does not promise to clear notification history.

Acceptance does not guarantee display; desktop policy and Do Not Disturb can suppress a banner. The API does not promise durable delivery, arbitrary action buttons, grouping or activation after process exit. Use `InlineMessage` for persistent in-app feedback and `IAnnouncer` for screen-reader announcements.

## Application launcher: Jump Lists and badges

Inject the singleton `IApplicationLauncher` to manage the application's taskbar/Dock entry. It is separate from tray items, system notifications and `IWindow`. All windows share its state and capabilities. Call its methods on the UI dispatcher during `Run`; before or after the run loop they throw `InvalidOperationException`. `Capabilities` is empty outside that lifetime. `CapabilitiesChanged` reports backend support changes on the UI thread.

```csharp
if (launcher.Capabilities.SupportsJumpList)
{
    launcher.SetJumpList(
    [
        new("New window", ["--new-window"]),
        new("Open tasks", ["--show-tasks"])
    ]);
}

if (launcher.Capabilities.SupportsBadgeCount)
    launcher.SetBadgeCount(unreadCount);

if (launcher.Capabilities.SupportsBadgeIcon)
    launcher.SetBadgeIcon(ImageSource.FromFile("Assets/attention.png"), "Synchronization needs attention");

launcher.ClearBadge();
```

Here `launcher` is the injected service and `unreadCount` is application state. These are independent operations illustrating the API; each accepted badge request replaces the previous one.

| Runtime capability | Windows | Linux Wayland and X11 | Linux DRM/KMS |
| --- | --- | --- | --- |
| `SupportsJumpList` | Application tasks through `ICustomDestinationList` | False. Configure launcher actions in the installed `.desktop` file. | False |
| `SupportsBadgeCount` | Numeric overlay on the application's taskbar group | `LauncherEntry.Update` when a compatible host is detected | False |
| `SupportsBadgeIcon` | Custom taskbar overlay image | False | False |

On Linux, detection observes the session-bus names `com.canonical.Unity` and `org.kde.plasmashell`. A detected host does not prove the user enabled badges or installed a matching desktop entry. Without such a host the numeric capability is false, including an unextended GNOME session. The backend retains the last accepted count while the host is absent and republishes it when a host returns. The persistent application D-Bus connection owns the signal source; no external `gdbus` process is launched. macOS has no Nazor backend yet.

### Jump List task contract

`SetJumpList` replaces the tasks published by this API. An empty collection removes them. Titles are nonempty without NUL; argument collections and individual strings are non-null and contain no NUL. Empty arguments are valid. Collections are copied before publication. Windows command lines are limited to 32,767 UTF-16 units, including the executable and framework-dependent `dotnet` host prefix.

Tasks launch the current executable with individually quoted arguments; a framework-dependent `dotnet application.dll` launch preserves the application DLL prefix. Arguments are not shell commands. The application must recognize these arguments in its startup code, before passing unrelated custom switches to the Generic Host configuration parser. Tasks can be invoked after the original process exits and normally start a new process. Nazor does not forward them to an existing instance.

The Windows shell associates the list with the current application identity, including an explicit process AppUserModelID when one was configured by the host. Use a consistent installed shortcut identity. Lists remain after shutdown until replaced or removed; normal application shutdown does not erase them. Pinned destinations remain under user control. This API does not manage recent/frequent destinations, custom categories, arbitrary executables or separators.

### Badge contract

Count and icon share one badge slot. `SetBadgeCount(0)` and `ClearBadge()` remove either kind; negative counts throw `ArgumentOutOfRangeException`. Nazor does not abbreviate or cap counts. Windows draws every digit of the supplied count and uses the same value as the accessible description; its default text-fit style shrinks long counts within the fixed overlay. Other desktop shells control their own presentation. Applications choose any limit before calling, for example `launcher.SetBadgeCount(Math.Min(unreadCount, 99))`; use a custom badge icon for a formatted label such as `99+`. `SetBadgeIcon` accepts an `ImageSource` and a nonempty accessible description without NUL. Raster images are loaded asynchronously, using scale variants at 100%, 125%, 150%, 200%, 300% and 400%. Prepared premultiplied BGRA images retain the source aspect ratio and transparency; Windows composes them into its square overlay using the selected window's badge style. Unsupported encodings or unreadable images are diagnosed to standard error and preserve the previously published badge.

A successful icon call accepts the asynchronous load, not its eventual native publication. A subsequent accepted count, icon, clear or application stop cancels the earlier load; stale completions cannot restore it. A failed or unsupported request returns false and leaves the prior badge/request intact. Invalid arguments throw even when the feature is unsupported. All successful native operations report acceptance, not guaranteed display; shell preferences and grouping still apply.

Windows publishes through a real, visible, unowned taskbar window after `TaskbarButtonCreated`. A request made before a suitable window is ready is retained for later publication. The backend selects one window for the application group, transfers the retained badge when that window closes or hides, and reapplies it after Explorer restarts or the selected window changes DPI. The badge is not independently visible per window. With no taskbar window it cannot be shown, even if the application has tray icons. Shutdown cancels image loads and clears the runtime badge.

### Styling Windows badges

Nazor draws the Windows overlay; the shell places it on the taskbar icon. Define `-windows-badge-*` NSS properties on the root `Window`. The application-wide badge uses the style of the window currently publishing it. When that window hides or closes, the replacement publisher supplies its own computed style. Style changes, themes, font registrations on the next requested frame, asynchronous background/mask completion and DPI changes redraw the retained badge without calling `SetBadgeCount` or `SetBadgeIcon` again. Linux ignores these Windows properties and lets the launcher draw its numeric badge.

```css
:root {
    -windows-badge-background-color: #005fb8;
    -windows-badge-background-image: linear-gradient(to bottom, #1685dd, #00519d);
    -windows-badge-color: white;
    -windows-badge-size: 14px;
    -windows-badge-border: 1px solid white;
    -windows-badge-border-bottom-width: 2px;
    -windows-badge-border-radius: 50%;
    -windows-badge-font-family: "Segoe UI";
    -windows-badge-font-size: 10px;
    -windows-badge-image-background-color: white;
    -windows-badge-image-fit: contain;
    -windows-badge-image-clip: border-box;
}
```

The image remains the source passed from code to `SetBadgeIcon`. NSS independently supplies decorative URL/gradient backgrounds, per-side borders, composition style, static transforms and an optional raster/SVG alpha mask. Count text uses the owning application's fonts, including bundled fonts; no system installation is required. The shell reserves a 16 × 16 logical pixel overlay: size, dimensions, padding, shadows and offsets operate inside that area, and content outside it is clipped. NSS cannot enlarge the shell overlay or move its taskbar placement. See the [complete badge property reference](19-NSS-Property-Reference.md#windows-launcher-badges) for defaults, units and supported styles.

### Linux desktop-file actions

Use a desktop entry whose filename matches `NazorApplicationId` plus `.desktop` for consistent window and launcher association. Linux reads the entry assembly's `Nazor.ApplicationId` metadata, using it as the Wayland toplevel app ID and X11 `WM_CLASS`. Explicit hosts without SDK metadata keep the legacy assembly-derived ID. For `NazorApplicationId=org.example.myapp`, install `org.example.myapp.desktop` with this content.

```ini
[Desktop Entry]
Type=Application
Name=My application
Exec=/opt/myapp/MyApp
Icon=myapp
StartupWMClass=org.example.myapp
Actions=NewWindow;

[Desktop Action NewWindow]
Name=New window
Exec=/opt/myapp/MyApp --new-window
```

Place it in an appropriate XDG applications directory, such as `~/.local/share/applications/`, and adapt executable/icon paths for the installation. This file is also how the desktop associates numeric badge signals with the app. Nazor does not generate or modify it. Set `NazorLinuxDesktopEntry` to copy your file into Linux and portable build/publish output; a different filename only produces a warning. See [SDK desktop entry configuration](03-Projects-and-Builds.md#linux-desktop-entry). `SetJumpList` always returns false on Linux. See the [Desktop Entry specification](https://specifications.freedesktop.org/desktop-entry/latest/extra-actions.html).

The Windowing sample selects a matching desktop entry through `NazorLinuxDesktopEntry`. Install the entry in `~/.local/share/applications/` and set its `Exec` to the installed sample path if the executable is outside the desktop session's `PATH`. Install the supplied PNG (`Assets/launcher.png` for Windowing) in the icon theme under the entry's `Icon` name, or set `Icon` to the absolute path of the installed PNG.

See the [Windowing desktop services sample](../../Samples/Nazor.Samples.Windowing/README.md#desktop-services), [launcher example](../../Samples/Nazor.Samples.Windowing/README.md#application-launcher), and [desktop APIs](../ApiReferences/Nazor.Platform.md).

---

[Guide index](README.md) · [API reference](../API.md)
