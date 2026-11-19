# Nazor.Platform

Platform backend contracts for native windows, input delivery, file dialogs, desktop services, and event loops.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Graphics surfaces and completion

`IPlatformWindow.CreateOpenGlContext()` optionally prepares an `IOpenGlWindowContext` on the UI owner; null means unsupported. This permits UI-owned native presentation surfaces. Graphics initialization, context operations and disposal run exclusively on the render worker. The caller disposes the context before native window destruction, then releases display references and presentation windows through `ReleaseDisplay()` on the UI owner. `ResetRendering()` resets native paint scheduling after a renderer transition. `ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper)` records client decorations on the UI thread; its default returns content unchanged.

`IOpenGlWindowContext` exposes `IsEmbedded`, `IsPresentationSynchronized`, `Initialize()`, `GetProcAddress(string)`, `MakeCurrent(int width, int height)`, `Present()`, `Dispose()` and `ReleaseDisplay()`. Context operations/disposal run on the shared render worker. `ReleaseDisplay` runs on the UI owner after queued context cleanup. `GraphicsDeviceException(string message, Exception? innerException = null)` identifies recognized native device failures; arbitrary drawing errors must not be treated as GPU fallback.

`IWindowHost.UsesGpuRendering` tells the backend that a CPU pixel buffer is unnecessary. `OnRenderingStopping()` releases rendering resources before native destruction. `OnPresented(TimeSpan duration)` reports software backend completion after painting, including a no-op paint. `PaintTarget.TopInset` is the physical top decoration inset included in a GPU target's height. `PaintTarget.TransparentRegion` optionally clears a physical region after GPU drawing for native caption composition; changes participate in target invalidation.

[Source](../../Sources/Platforms/Nazor.Platform/IOpenGlWindowContext.cs)

## Window and platform types

- [`ApplicationLauncherCapabilities`](#type-applicationlaunchercapabilities)
- [`JumpListTask`](#type-jumplisttask)
- [`TrayMenuOpeningEventArgs`](#type-traymenuopeningeventargs)
- [`NativeTrayMenuItem`](#type-nativetraymenuitem)
- [`IPlatformTrayItem`](#type-iplatformtrayitem)
- [`WindowsTitleBarColors`](#type-windowstitlebarcolors)
- [`WindowsBadgeAppearance`](#type-windowsbadgeappearance)
- [`WindowsBadgeImageFit`](#type-windowsbadgeimagefit)

- [`ISingleViewPlatform`](#type-isingleviewplatform)
- [`FileDialogFilter`](#type-filedialogfilter)
- [`FileDialogKind`](#type-filedialogkind)
- [`FileDialogRequest`](#type-filedialogrequest)
- [`IPlatform`](#type-iplatform)
- [`IPlatformDesktopIntegration`](#type-iplatformdesktopintegration)
- [`IPlatformHost`](#type-iplatformhost)
- [`IPlatformWindow`](#type-iplatformwindow)
- [`IWindowHost`](#type-iwindowhost)
- [`PaintTarget`](#type-painttarget)
- [`PlatformCursor`](#type-platformcursor)
- [`SystemNotification`](#type-systemnotification)
- [`SystemNotificationIcon`](#type-systemnotificationicon)
- [`TrayIconOptions`](#type-trayiconoptions)
- [`WindowCentering`](#type-windowcentering)
- [`WindowHitTest`](#type-windowhittest)
- [`WindowOptions`](#type-windowoptions)
- [`WindowPlacement`](#type-windowplacement)
- [`WindowPlacementCapabilities`](#type-windowplacementcapabilities)
- [`WindowPlacementCoordinates`](#type-windowplacementcoordinates)

<a id="type-applicationlaunchercapabilities"></a>

## `ApplicationLauncherCapabilities`

Namespace: `Nazor`

```csharp
public readonly record struct ApplicationLauncherCapabilities(
    bool SupportsJumpList, bool SupportsBadgeCount, bool SupportsBadgeIcon);
```

Independent runtime support for the application's launcher entry. Default means no supported features. These are application capabilities, not `WindowCapabilities`; accepted operations remain subject to shell policy and availability of a matching launcher/taskbar entry.

[Source](../../Sources/Platforms/Nazor.Platform/ApplicationLauncherOptions.cs)

<a id="type-jumplisttask"></a>

## `JumpListTask`

Namespace: `Nazor`

```csharp
public sealed record JumpListTask(string Title, IReadOnlyList<string> Arguments);
```

One task that starts the current application. Title must be nonempty without NUL. Arguments are individual non-null strings without NUL; empty strings are allowed. `IApplicationLauncher.SetJumpList` validates and copies both the task list and argument lists before native publication. The application handles its arguments on startup; callbacks into an existing process are not implied.

[Source](../../Sources/Platforms/Nazor.Platform/ApplicationLauncherOptions.cs)

<a id="type-filedialogfilter"></a>

## `FileDialogFilter`

Namespace: `Nazor.Platform`

```csharp
public sealed record FileDialogFilter(string Name, IReadOnlyList<string> Patterns)
```

A choice of file types in a file dialog: a name and its patterns (`*.csv`).

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `Patterns` | The patterns (IReadOnlyList<string>). |

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)


<a id="type-filedialogkind"></a>

## `FileDialogKind`

Namespace: `Nazor.Platform`

```csharp
public enum FileDialogKind
```

The dialogs of the system for files and folders.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `Open`

```csharp
Open
```

Prompts for an existing file or files.

#### `Save`

```csharp
Save
```

Prompts for a destination file path.

#### `Folder`

```csharp
Folder
```

Prompts for an existing folder.


<a id="type-filedialogrequest"></a>

## `FileDialogRequest`

Namespace: `Nazor.Platform`

```csharp
public sealed record FileDialogRequest(FileDialogKind Kind)
```

What a file dialog of the system shows.

| Parameter | Description |
| --- | --- |
| `Kind` | The file dialog kind represented by this value. |

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `Title`

```csharp
public string? Title { get; init;}
```

Gets or sets the title displayed by this window, dialog, or message.

#### `InitialDirectory`

```csharp
public string? InitialDirectory { get; init;}
```

Gets or sets the directory initially displayed by the native picker.

#### `FileName`

```csharp
public string? FileName { get; init;}
```

The file name proposed (to save, or selected to open).

#### `DefaultExtension`

```csharp
public string? DefaultExtension { get; init;}
```

The extension added to a saved name without one.

#### `Filters`

```csharp
public IReadOnlyList<FileDialogFilter> Filters { get; init;}
```

Gets or sets the file-type filters offered by the native picker.

#### `Multiple`

```csharp
public bool Multiple { get; init;}
```

Several files can be opened at once.


<a id="type-iplatform"></a>

## `IPlatform`

Namespace: `Nazor.Platform`

```csharp
public interface IPlatform
```

An operating-system backend: windows and the UI message loop.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `BackendName`

```csharp
string BackendName {get; }
```

The active platform backend name used in diagnostics. The default uses the implementation type name.

#### `Capabilities`

```csharp
WindowCapabilities Capabilities {get; }
```

Backend support before a native window exists; individual windows apply their requested operations.

#### `DesktopIntegration`

```csharp
IPlatformDesktopIntegration? DesktopIntegration {get; }
```

Null when the backend has no native tray or system notifications.

#### `CreateWindow`

```csharp
IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
```

Creates a native window connected to the supplied Nazor window host.

| Parameter | Description |
| --- | --- |
| `host` | The Nazor window host that receives native lifecycle, painting, and input notifications. |
| `options` | The options used to configure the operation. |

Returns: The native platform window connected to the host.

#### `ShowFileDialog`

```csharp
IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)
```

Shows a file dialog of the system, modal to `owner`, and returns the chosen paths, or null when the user cancelled. It runs its own message loop: call it outside renders.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `request` | The native file-dialog operation and options. |

Returns: The resulting IReadOnlyList<string>? value.

#### `Run`

```csharp
int Run(UIDispatcher dispatcher)
```

Runs the message loop on the calling thread, which owns `dispatcher`, until `Quit`.

| Parameter | Description |
| --- | --- |
| `dispatcher` | The dispatcher that owns the UI thread. |

Returns: The resulting int value.

#### `Quit`

```csharp
void Quit(int exitCode = 0)
```

Requests termination of the platform event loop with the specified exit code.

| Parameter | Description |
| --- | --- |
| `exitCode` | The exit code reported when the application event loop terminates. |


<a id="type-iplatformdesktopintegration"></a>

## `IPlatformDesktopIntegration`

Namespace: `Nazor.Platform`

```csharp
public interface IPlatformDesktopIntegration
{
    ApplicationLauncherCapabilities LauncherCapabilities { get; }
    event Action? LauncherCapabilitiesChanged;
    bool SetJumpList(IReadOnlyList<JumpListTask> tasks);
    bool SetBadgeCount(int count);
    bool SetBadgeIcon(IReadOnlyList<RenderedImage> images, string description);
    bool ClearBadge();
    bool SupportsTray { get; }
    bool SupportsNotifications { get; }
    IPlatformTrayItem? CreateTrayItem();
    event Action? NotificationActivated;
    bool ShowNotification(SystemNotification notification);
    void WithdrawNotification();
}
```

Optional native desktop services. CreateTrayItem creates one separately owned registration, or returns null when the backend has no tray implementation. Tray state and events belong to IPlatformTrayItem; system notifications remain independent. Called on the platform UI thread.

Launcher members manage one application identity. Default interface implementations report no launcher capabilities, return false and accept no event subscriptions, so existing custom/headless backends remain compatible. SetJumpList receives a validated task snapshot. SetBadgeIcon receives prepared premultiplied BGRA scale variants preserving the source aspect ratio; an accepting backend owns a pixel copy. Count and image replace each other; zero/clear removes either. A backend must preserve previously accepted state on rejection and issue capability callbacks on its UI thread. Windows retains accepted badge state until an eligible taskbar window is ready and across window/shell recreation; Linux supplies only numeric signals when a compatible host is detected. See the [launcher guide](../Guide/15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges).

[Source](../../Sources/Platforms/Nazor.Platform/IDesktopIntegration.cs)

<a id="type-iplatformhost"></a>

## `IPlatformHost`

Namespace: `Nazor.Platform`

```csharp
public interface IPlatformHost
```

Owns the platform and its UI-thread startup rules for one application run.

[Source](../../Sources/Platforms/Nazor.Platform/IPlatformHost.cs)

### Members

#### `Run`

```csharp
int Run(Func<IPlatform, int> application)
```

Creates the platform and invokes the application within its required UI-thread context.

| Parameter | Description |
| --- | --- |
| `application` | The callback invoked with the platform created by the host. |

Returns: The application callback's exit code.


#### `RunAsync`

```csharp
Task<int> RunAsync(Func<IPlatform, int> application)
```

Runs the callback without blocking the caller. The task completes with its exit code after the platform has been released, or propagates the startup/callback exception. A null callback throws `ArgumentNullException`.

The default interface implementation invokes `Run` on a dedicated worker thread, preserving existing custom hosts. Hosts with native thread requirements should override this method to provide their UI-thread context. Application cancellation is coordinated by `NazorApp` through the host lifetime rather than by abandoning this task.


<a id="type-iwindowhost"></a>

## `IWindowHost`

Namespace: `Nazor.Platform`

```csharp
public interface IWindowHost
```

Implemented by the Nazor side of a window; the backend calls it on the UI thread with input already normalised: logical coordinates relative to the client area, DOM-like key names.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `OnResized`

```csharp
void OnResized(Size logicalSize, float scale)
```

Updates the host with the logical client size and physical pixel scale.

| Parameter | Description |
| --- | --- |
| `logicalSize` | The client-area dimensions in logical units. |
| `scale` | The number of physical pixels per logical unit. |

#### `OnPaint`

```csharp
IReadOnlyList<PixelRect>? OnPaint(PaintTarget target)
```

Draws into the target and says what changed: the rectangles to show (physical pixels), or null when the whole buffer changed. The rest of the buffer keeps the previous frame.

| Parameter | Description |
| --- | --- |
| `target` | The component, element, or navigation entry targeted by the operation. |

Returns: The resulting IReadOnlyList<PixelRect>? value.

#### `OnPointerMoved`

```csharp
void OnPointerMoved(Point point, KeyModifiers modifiers)
```

Processes the pointer moved notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerPressed`

```csharp
void OnPointerPressed(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer pressed notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `button` | The pointer button associated with the event. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerReleased`

```csharp
void OnPointerReleased(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer released notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `button` | The pointer button associated with the event. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerLeft`

```csharp
void OnPointerLeft()
```

Processes the pointer left notification in logical viewport coordinates.

#### `OnPointerDoubleClicked`

```csharp
void OnPointerDoubleClicked(Point point, PointerButton button, KeyModifiers modifiers)
```

Processes the pointer double clicked notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `button` | The pointer button associated with the event. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerWheel`

```csharp
void OnPointerWheel(Point point, float notches, KeyModifiers modifiers)
```

Vertical wheel rotation in notches; positive values scroll up (away from the user).

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `notches` | The number of wheel detents delivered by the backend. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerHorizontalWheel`

```csharp
void OnPointerHorizontalWheel(Point point, float notches, KeyModifiers modifiers)
```

The horizontal wheel (or tilt) moved by `notches`, positive to the right.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `notches` | The number of wheel detents delivered by the backend. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnPointerCanceled`

```csharp
void OnPointerCanceled()
```

Processes the pointer canceled notification in logical viewport coordinates.

#### `OnKeyDown`

```csharp
bool OnKeyDown(string key, KeyModifiers modifiers, bool repeat)
```

Delivers a normalized key press and reports whether Nazor handled it.

| Parameter | Description |
| --- | --- |
| `key` | The normalized name of the key. |
| `modifiers` | The modifier keys pressed when the input occurred. |
| `repeat` | Whether this key event is generated by auto-repeat. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `OnKeyUp`

```csharp
bool OnKeyUp(string key, KeyModifiers modifiers)
```

Delivers a normalized key release and reports whether Nazor handled it.

| Parameter | Description |
| --- | --- |
| `key` | The normalized name of the key. |
| `modifiers` | The modifier keys pressed when the input occurred. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `OnTextInput`

```csharp
void OnTextInput(string text)
```

Delivers text committed by the platform's text input system.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |

#### `OnComposition`

```csharp
void OnComposition(string text, int cursor)
```

IME preedit text and caret inside it; an empty text ends the composition without inserting.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |
| `cursor` | The cursor (int). |

#### `OnCompositionCommit`

```csharp
void OnCompositionCommit(string text)
```

Text committed by the IME.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |

#### `OnFocusChanged`

```csharp
void OnFocusChanged(bool focused)
```

Notifies the host that the native window has gained or lost keyboard focus.

| Parameter | Description |
| --- | --- |
| `focused` | Whether the native window has keyboard focus. |

#### `OnVisibilityChanged`

```csharp
void OnVisibilityChanged(bool visible)
```

The window was minimized or hidden (false), or shown again (true).

| Parameter | Description |
| --- | --- |
| `visible` | Whether the window, control, or native decoration should be visible. |

#### `OnSystemSettingsChanged`

```csharp
void OnSystemSettingsChanged()
```

A system setting the host may depend on changed (for example the animation preference).

#### `OnEnvironmentChanged`

```csharp
void OnEnvironmentChanged()
```

Safe areas or keyboard occlusion changed; data is already relative to the client.

#### `OnPointerScroll`

```csharp
void OnPointerScroll(Point point, float deltaX, float deltaY, ScrollDeltaUnit unit, KeyModifiers modifiers)
```

Processes the pointer scroll notification in logical viewport coordinates.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |
| `deltaX` | The horizontal scroll delta in the specified unit. |
| `deltaY` | The vertical scroll delta in the specified unit. |
| `unit` | The unit used for the scroll deltas. |
| `modifiers` | The modifier keys pressed when the input occurred. |

#### `OnClosing`

```csharp
bool OnClosing()
```

The user asked to close the window; return false to keep it open.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `HitTest`

```csharp
WindowHitTest HitTest(Point point)
```

What a point of the client area is, for a window with the content in the title bar.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: The resulting WindowHitTest value.

#### `OnStateChanged`

```csharp
void OnStateChanged(WindowState state)
```

The window was maximized, minimized or restored.

| Parameter | Description |
| --- | --- |
| `state` | The state (WindowState). |

#### `OnCapabilitiesChanged`

```csharp
void OnCapabilitiesChanged()
```

Notifies the host that the effective window capabilities have changed.

#### `OnClosed`

```csharp
void OnClosed()
```

The native window is gone; release what depends on it.

#### `GetCursor`

```csharp
PlatformCursor GetCursor(Point point)
```

Gets the cursor to display at the specified client position.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: The resulting PlatformCursor value.

#### `GetSemanticTree`

```csharp
SemanticTree? GetSemanticTree()
```

The accessible view of the window, requested when an assistive technology first asks for it; null while the window is not ready or after it closed.

Returns: The resulting SemanticTree? value.


<a id="type-painttarget"></a>

## `PaintTarget`

Namespace: `Nazor.Platform`

```csharp
public readonly record struct PaintTarget(nint Pixels, int Width, int Height, int Stride, float Scale)
```

Pixels to draw into during a paint: BGRA premultiplied, top-down, owned by the backend.

| Parameter | Description |
| --- | --- |
| `Pixels` | The address of the top-down BGRA premultiplied pixel buffer. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |
| `Stride` | The number of bytes between the starts of successive pixel rows. |
| `Scale` | The scale (float). |

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)


<a id="type-platformcursor"></a>

## `PlatformCursor`

Namespace: `Nazor.Platform`

```csharp
public enum PlatformCursor
```

Identifies the native pointer cursor requested by the window host.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `Arrow`

```csharp
Arrow
```

Displays a cursor indicating the default arrow.

#### `Hand`

```csharp
Hand
```

Displays a cursor indicating a link-selection hand.

#### `IBeam`

```csharp
IBeam
```

Displays a cursor indicating a text-selection I-beam.

#### `SizeWestEast`

```csharp
SizeWestEast
```

Displays a cursor indicating horizontal resizing.

#### `SizeNorthSouth`

```csharp
SizeNorthSouth
```

Displays a cursor indicating vertical resizing.

#### `SizeAll`

```csharp
SizeAll
```

Displays a cursor indicating movement in all directions.

#### `No`

```csharp
No
```

Displays a cursor indicating an unavailable operation.

#### `Wait`

```csharp
Wait
```

Displays a cursor indicating a busy operation.

#### `AppStarting`

```csharp
AppStarting
```

Displays a cursor indicating background progress.

#### `Help`

```csharp
Help
```

Displays a cursor indicating contextual help.

#### `Cross`

```csharp
Cross
```

Displays a cursor indicating a precision crosshair.


<a id="type-systemnotification"></a>

## `SystemNotification`

Namespace: `Nazor`

```csharp
public sealed record SystemNotification(string Title, string Message, SystemNotificationIcon Icon = SystemNotificationIcon.Information)
```

A transient native notification. Windows Shell supports 63 title and 255 message UTF-16 characters.

| Parameter | Description |
| --- | --- |
| `Title` | The title (string). |
| `Message` | The message (string). |
| `Icon` | The icon (SystemNotificationIcon). |

[Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)


<a id="type-systemnotificationicon"></a>

## `SystemNotificationIcon`

Namespace: `Nazor`

```csharp
public enum SystemNotificationIcon
```

Specifies the standard icon shown with a system notification.

[Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

### Members

#### `None`

```csharp
None
```

Displays no standard icon.

#### `Information`

```csharp
Information
```

Displays the standard information icon.

#### `Warning`

```csharp
Warning
```

Displays the standard warning icon.

#### `Error`

```csharp
Error
```

Displays the standard error icon.


<a id="type-trayiconoptions"></a>

## `TrayIconOptions`

Namespace: `Nazor`

```csharp
public sealed record TrayIconOptions
{
    public RenderedImage? Icon { get; init; }
    public string ToolTip { get; init; }
    public IReadOnlyList<NativeTrayMenuItem> Menu { get; init; }
    public TrayIconOptions Snapshot();
}
```

Prepared backend snapshot, not the application-facing typed menu. Snapshot validates and copies the complete tree before changing shell state. RenderedImage pixels use premultiplied packed BGRA. Applications configure TrayItem.Icon and TrayItem.Menu instead.

[Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

<a id="type-windowcentering"></a>

## `WindowCentering`

Namespace: `Nazor.Platform`

```csharp
public enum WindowCentering
```

Where a window opens.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `None`

```csharp
None
```

Where the system puts new windows.

#### `Owner`

```csharp
Owner
```

Centered on its owner (on the screen without one).

#### `Screen`

```csharp
Screen
```

Centered on the work area of the screen.


<a id="type-windowhittest"></a>

## `WindowHitTest`

Namespace: `Nazor.Platform`

```csharp
public enum WindowHitTest
```

What a point of a window with the content in the title bar is for the system.

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `Client`

```csharp
Client
```

Content: the host handles the pointer.

#### `Caption`

```csharp
Caption
```

The title bar: dragging moves the window, a double click maximizes it.

#### `MaximizeButton`

```csharp
MaximizeButton
```

The maximize button: Windows 11 shows the snap layouts over it.


<a id="type-windowplacement"></a>

## `WindowPlacement`

Namespace: `Nazor.Platform`

```csharp
public sealed record WindowPlacement(int X, int Y, int Width, int Height, bool IsMaximized)
```

Normal window bounds with an explicit coordinate representation and optional portable client size. Without coordinate metadata, placement bounds use Windows native coordinates.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |
| `IsMaximized` | The is maximized (bool). |

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

### Members

#### `Coordinates`

```csharp
public WindowPlacementCoordinates Coordinates { get; init;}
```

Gets or sets the coordinate representation of the saved bounds.

#### `Scale`

```csharp
public float Scale { get; init;}
```

Gets or sets the pixel scale at which this placement was captured.

#### `ScreenId`

```csharp
public string? ScreenId { get; init;}
```

Gets or sets the optional identifier of the display containing the saved window.

#### `LogicalClientSize`

```csharp
public Size? LogicalClientSize { get; init;}
```

Gets or sets the portable logical client size, when it is available.


<a id="type-windowplacementcapabilities"></a>

## `WindowPlacementCapabilities`

Namespace: `Nazor.Platform`

```csharp
public readonly record struct WindowPlacementCapabilities(bool CanRestoreSize, bool CanRestorePosition, bool CanSelectScreen, bool CanCenter)
```

Independent placement functions offered by a backend.

| Parameter | Description |
| --- | --- |
| `CanRestoreSize` | The can restore size (bool). |
| `CanRestorePosition` | The can restore position (bool). |
| `CanSelectScreen` | The can select screen (bool). |
| `CanCenter` | The can center (bool). |

[Source](../../Sources/Platforms/Nazor.Platform/WindowEnvironment.cs)


<a id="type-windowplacementcoordinates"></a>

## `WindowPlacementCoordinates`

Namespace: `Nazor.Platform`

```csharp
public enum WindowPlacementCoordinates
```

Coordinate representation stored with placements so other backends do not reinterpret pixels.

[Source](../../Sources/Platforms/Nazor.Platform/WindowEnvironment.cs)

### Members

#### `WindowsWorkAreaPixels`

```csharp
WindowsWorkAreaPixels
```

Stores native Windows window bounds in work-area physical pixels.

#### `LogicalClient`

```csharp
LogicalClient
```

Stores placement using portable logical client coordinates.

<a id="type-windowstitlebarcolors"></a>

## `WindowsTitleBarColors`

Namespace: `Nazor.Platform`

```csharp
public sealed record WindowsTitleBarColors
```

Native Windows title bar colors supplied by the computed root Window style. All properties use `Nazor.Graphics.Color?` and default to null. Windows 11 supports all twelve colors for the system caption; custom captions use only button colors. See [native NSS colors](../Guide/14-Title-Bars-and-Platform-Integration.md#native-system-title-bar-colors) for the NSS names, scope, reset behavior and OS restrictions.

[Source](../../Sources/Platforms/Nazor.Platform/WindowAppearance.cs)

### Members

#### `BackgroundColor`

```csharp
public Color? BackgroundColor { get; init; }
```

background color; null restores the default for this caption mode.

#### `ForegroundColor`

```csharp
public Color? ForegroundColor { get; init; }
```

foreground color; null restores the default for this caption mode.

#### `InactiveBackgroundColor`

```csharp
public Color? InactiveBackgroundColor { get; init; }
```

inactive background color; null restores the default for this caption mode.

#### `InactiveForegroundColor`

```csharp
public Color? InactiveForegroundColor { get; init; }
```

inactive foreground color; null restores the default for this caption mode.

#### `ButtonBackgroundColor`

```csharp
public Color? ButtonBackgroundColor { get; init; }
```

button background color; null restores the default for this caption mode.

#### `ButtonForegroundColor`

```csharp
public Color? ButtonForegroundColor { get; init; }
```

button foreground color; null restores the default for this caption mode.

#### `ButtonInactiveBackgroundColor`

```csharp
public Color? ButtonInactiveBackgroundColor { get; init; }
```

button inactive background color; null restores the default for this caption mode.

#### `ButtonInactiveForegroundColor`

```csharp
public Color? ButtonInactiveForegroundColor { get; init; }
```

button inactive foreground color; null restores the default for this caption mode.

#### `ButtonHoverBackgroundColor`

```csharp
public Color? ButtonHoverBackgroundColor { get; init; }
```

button hover background color; null restores the default for this caption mode.

#### `ButtonHoverForegroundColor`

```csharp
public Color? ButtonHoverForegroundColor { get; init; }
```

button hover foreground color; null restores the default for this caption mode.

#### `ButtonPressedBackgroundColor`

```csharp
public Color? ButtonPressedBackgroundColor { get; init; }
```

button pressed background color; null restores the default for this caption mode.

#### `ButtonPressedForegroundColor`

```csharp
public Color? ButtonPressedForegroundColor { get; init; }
```

button pressed foreground color; null restores the default for this caption mode.


<a id="type-isingleviewplatform"></a>

## `ISingleViewPlatform`

Namespace: `Nazor.Platform`

```csharp
public interface ISingleViewPlatform : IPlatform
{
    IPlatformWindow CreateView(IWindowHost host);
}
```

Optional backend contract for a single display surface without desktop windows. CreateView is called on the UI thread with rendering/input callbacks and returns a surface adapter whose ClientSize comes from the display. Unsupported window operations must be reflected in its capabilities. Nazor's UseMainView mode uses this method instead of CreateWindow and rejects subsequent desktop-window/dialog requests. Linux `DrmPlatform` implements this contract; select its explicit host as described in the [DRM/KMS guide](../Guide/34-Linux-DRM.md).

[Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

<a id="type-iplatformtrayitem"></a>

## `IPlatformTrayItem`

Namespace: `Nazor.Platform`

```csharp
public interface IPlatformTrayItem : IDisposable
{
    bool IsSupported { get; }
    bool IsVisible { get; }
    event Action? VisibilityChanged;
    event Action? Activated;
    event Action? SecondaryActivated;
    event Action<int>? MenuItemInvoked;
    event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    bool Show(TrayIconOptions options);
    void Hide();
}
```

One native registration with synchronous UI-thread callbacks. Native IDs are scoped to this item and mapped to typed menu callbacks by the application runtime. MenuOpening must run before presentation: Windows supports cancellation; an exported Linux menu reports CanCancel=false. Dispose removes only this registration.

[Source](../../Sources/Platforms/Nazor.Platform/IDesktopIntegration.cs)


<a id="type-nativetraymenuitem"></a>

## `NativeTrayMenuItem`

Namespace: `Nazor`

```csharp
public enum NativeTrayMenuItemKind { Command, Separator, Submenu, CheckBox, Radio }
public sealed record NativeTrayMenuItem(int Id, NativeTrayMenuItemKind Kind, string Text = "")
{
    public bool Enabled { get; init; }
    public bool Visible { get; init; }
    public bool IsChecked { get; init; }
    public RenderedImage? Icon { get; init; }
    public IReadOnlyList<NativeTrayMenuItem> Children { get; init; }
}
```

Backend representation produced by the typed application API. Enabled and Visible default to true. Submenus carry children; checkbox/radio state is drawn by the native host. IDs are distinct positive integers for invokable entries.

[Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)


<a id="type-traymenuopeningeventargs"></a>

## `TrayMenuOpeningEventArgs`

Namespace: `Nazor`

```csharp
public sealed class TrayMenuOpeningEventArgs(bool canCancel) : EventArgs
{
    public bool CanCancel { get; }
    public bool IsCanceled { get; }
    public bool TryCancel();
}
```

A synchronous default-action cancellation request. TryCancel returns false when unsupported; accepted cancellation is sticky and does not suppress other event subscribers. Decide before returning, not after an await. Menu presentation on Windows is cancellable; Linux DBusMenu AboutToShow has no veto.

[Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

<a id="type-windowsbadgeappearance"></a>

## `WindowsBadgeAppearance`

Namespace: `Nazor.Platform`

```csharp
public sealed record WindowsBadgeAppearance
{
    /// <summary>Borrowed application font provider returning Skia font faces, used on the UI thread; null uses installed system fonts. The owning window must keep it alive while publishing.</summary>
    public IFontProvider? Fonts { get; init; }
    /// <summary>Font provider version captured when this snapshot was created, so registrations invalidate retained badges.</summary>
    public int FontVersion { get; init; }
    /// <summary>Resolved decorative background layers. Images are borrowed from the owning renderer and must remain alive while publishing.</summary>
    public DisplayList? Background { get; init; }
    /// <summary>Whether background color and layers use the padding box inside the border; false uses the border box.</summary>
    public bool ClipBackgroundToPadding { get; init; }
    /// <summary>Optional border widths by side, overriding the uniform width. Hidden sides must have zero width.</summary>
    public Thickness? BorderWidths { get; init; }
    /// <summary>Optional top border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderTop { get; init; }
    /// <summary>Optional right border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderRight { get; init; }
    /// <summary>Optional bottom border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderBottom { get; init; }
    /// <summary>Optional left border appearance, overriding the uniform appearance.</summary>
    public BorderSide? BorderLeft { get; init; }
    /// <summary>Transform of the complete badge, including shadows and mask, within the fixed overlay.</summary>
    public Matrix Transform { get; init; } = Matrix.Identity;
    /// <summary>Optional alpha mask drawn in overlay coordinates after badge composition and before transformation. Null disables masking; an empty list hides the badge. Images are borrowed from the owning renderer.</summary>
    public DisplayList? Mask { get; init; }
    /// <summary>Optional resolver called on the UI thread with free space after image fitting, which may be negative. Overrides ImagePosition and ImageOffset and must be deterministic for this snapshot.</summary>
    public Func<Size, Point>? ImagePositionResolver { get; init; }
    /// <summary>Container background; null uses red for counts and transparency for icons.</summary>
    public Color? BackgroundColor { get; init; }
    /// <summary>Count foreground.</summary>
    public Color Color { get; init; } = new(255, 255, 255, 255);
    /// <summary>Container width, constrained to the overlay area.</summary>
    public float Width { get; init; } = 16;
    /// <summary>Container height, constrained to the overlay area.</summary>
    public float Height { get; init; } = 16;
    /// <summary>Container offset from the overlay center.</summary>
    public Point Offset { get; init; }
    /// <summary>Content padding inside the border.</summary>
    public Thickness Padding { get; init; }
    /// <summary>Complete badge opacity from zero to one.</summary>
    public float Opacity { get; init; } = 1;
    /// <summary>Uniform border width; zero hides the border.</summary>
    public float BorderWidth { get; init; }
    /// <summary>Border color.</summary>
    public Color BorderColor { get; init; }
    /// <summary>Border pattern.</summary>
    public LineStyle BorderStyle { get; init; }
    /// <summary>Container corner radii.</summary>
    public CornerRadius Radius { get; init; } = new(8, 8);
    /// <summary>Count font families in fallback order. The caller must not mutate this collection.</summary>
    public IReadOnlyList<string> FontFamilies { get; init; } = ["Segoe UI"];
    /// <summary>Count font size before shrinking.</summary>
    public float FontSize { get; init; } = 11.2f;
    /// <summary>Count font weight from 1 to 1000.</summary>
    public int FontWeight { get; init; } = 700;
    /// <summary>Whether the count uses an italic font.</summary>
    public bool Italic { get; init; }
    /// <summary>Extra spacing between count characters.</summary>
    public float LetterSpacing { get; init; }
    /// <summary>Horizontal alignment: zero is left, one is right.</summary>
    public float TextAlign { get; init; } = 0.5f;
    /// <summary>Vertical alignment: zero is top, one is bottom.</summary>
    public float VerticalAlign { get; init; } = 0.5f;
    /// <summary>Whether the count shrinks to fit the content box.</summary>
    public bool TextFit { get; init; } = true;
    /// <summary>Count outline width.</summary>
    public float TextStrokeWidth { get; init; }
    /// <summary>Count outline color.</summary>
    public Color TextStrokeColor { get; init; }
    /// <summary>Optional image background override; null uses the container background.</summary>
    public Color? ImageBackgroundColor { get; init; }
    /// <summary>How the image fits the content box.</summary>
    public WindowsBadgeImageFit ImageFit { get; init; } = WindowsBadgeImageFit.Contain;
    /// <summary>Image alignment as a fraction of the space left after fitting.</summary>
    public Point ImagePosition { get; init; } = new(0.5f, 0.5f);
    /// <summary>Additional image position in logical pixels.</summary>
    public Point ImageOffset { get; init; }
    /// <summary>Image opacity from zero to one, excluding the container.</summary>
    public float ImageOpacity { get; init; } = 1;
    /// <summary>Whether the image is clipped to the rounded container.</summary>
    public bool ClipImage { get; init; }
    /// <summary>Container shadows in painting order, first on top. The caller must not mutate this collection.</summary>
    public IReadOnlyList<DrawShadow> Shadows { get; init; } = [];
    /// <summary>Count shadows; inset and spread are unsupported. The caller must not mutate this collection.</summary>
    public IReadOnlyList<DrawShadow> TextShadows { get; init; } = [];
}
```

Resolved native badge snapshot in logical pixels. Collection members must not be mutated after submission; the Windows backend copies them. Width and height are constrained to 0–16 and centered with Offset. ImagePosition is a free-space fraction; ImageOffset adds an absolute offset. A non-null ImagePositionResolver overrides both and evaluates full mathematical positions after fitting, including negative free space. Fonts is a borrowed application provider (the Windows renderer accepts Skia font faces); FontVersion captures its registration version for equality and redraw. Background and Mask are immutable borrowed display lists; keep their image resources alive until the publishing window releases the snapshot. BorderWidths and individual BorderSide values override the uniform defaults. Transform includes the resolved origin and transforms the complete composition; Mask supplies alpha before that transform. An empty mask hides all ink, while null disables masking. Shadow Rect and Radius fields are supplied by the renderer from the badge container; text shadows use only Offset, Blur and Color. Source defaults preserve the red count badge and transparent icon badge. The application runtime resolves NSS lengths, percentages, currentColor and shorthands before submitting this snapshot. See the [badge property reference](../Guide/19-NSS-Property-Reference.md#windows-launcher-badges) for rendering details.

[Source](../../Sources/Platforms/Nazor.Platform/WindowsBadgeAppearance.cs)

<a id="type-windowsbadgeimagefit"></a>

## `WindowsBadgeImageFit`

Namespace: `Nazor.Platform`

```csharp
public enum WindowsBadgeImageFit : byte { Fill, Contain, Cover, None, ScaleDown }
```

Fill stretches to the content box. Contain fits the whole image while preserving its aspect ratio; Cover fills and crops it. None uses the prepared variant's logical size; ScaleDown selects the smaller of that size and Contain. The shell overlay always remains square.

[Source](../../Sources/Platforms/Nazor.Platform/WindowsBadgeAppearance.cs)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `ApplicationLauncherCapabilities`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/ApplicationLauncherOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ApplicationLauncherCapabilities : IEquatable<ApplicationLauncherCapabilities>` | Independent runtime features of the application's taskbar or Dock entry.<br>`SupportsJumpList`. Whether runtime launcher tasks can be published.<br>`SupportsBadgeCount`. Whether a numeric badge can be requested.<br>`SupportsBadgeIcon`. Whether a custom image badge can be requested. |
| `public ApplicationLauncherCapabilities()` |  |
| `public ApplicationLauncherCapabilities(bool SupportsJumpList, bool SupportsBadgeCount, bool SupportsBadgeIcon)` | Independent runtime features of the application's taskbar or Dock entry.<br>`SupportsJumpList`. Whether runtime launcher tasks can be published.<br>`SupportsBadgeCount`. Whether a numeric badge can be requested.<br>`SupportsBadgeIcon`. Whether a custom image badge can be requested. |
| `public override string ToString()` |  |
| `public static bool operator !=(ApplicationLauncherCapabilities left, ApplicationLauncherCapabilities right)` |  |
| `public static bool operator ==(ApplicationLauncherCapabilities left, ApplicationLauncherCapabilities right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ApplicationLauncherCapabilities other)` |  |
| `public void Deconstruct(out bool SupportsJumpList, out bool SupportsBadgeCount, out bool SupportsBadgeIcon)` |  |
| `public bool SupportsJumpList { get; init; }` | Whether runtime launcher tasks can be published. |
| `public bool SupportsBadgeCount { get; init; }` | Whether a numeric badge can be requested. |
| `public bool SupportsBadgeIcon { get; init; }` | Whether a custom image badge can be requested. |

### `JumpListTask`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/ApplicationLauncherOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record JumpListTask : IEquatable<JumpListTask>` | A launcher task that starts the current application with individual command-line arguments.<br>`Title`. The nonempty, localized task label, without NUL characters.<br>`Arguments`. Individual arguments, including empty strings when needed. No argument may contain NUL. |
| `public JumpListTask(string Title, IReadOnlyList<string> Arguments)` | A launcher task that starts the current application with individual command-line arguments.<br>`Title`. The nonempty, localized task label, without NUL characters.<br>`Arguments`. Individual arguments, including empty strings when needed. No argument may contain NUL. |
| `public override string ToString()` |  |
| `public static bool operator !=(JumpListTask? left, JumpListTask? right)` |  |
| `public static bool operator ==(JumpListTask? left, JumpListTask? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(JumpListTask? other)` |  |
| `public void Deconstruct(out string Title, out IReadOnlyList<string> Arguments)` |  |
| `public string Title { get; init; }` | The nonempty, localized task label, without NUL characters. |
| `public IReadOnlyList<string> Arguments { get; init; }` | Individual arguments, including empty strings when needed. No argument may contain NUL. |

### `NativeTrayMenuItem`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record NativeTrayMenuItem : IEquatable<NativeTrayMenuItem>` | One immutable native menu entry. IDs are private to an individual tray registration. |
| `public NativeTrayMenuItem(int Id, NativeTrayMenuItemKind Kind, string Text = "")` | One immutable native menu entry. IDs are private to an individual tray registration. |
| `public override string ToString()` |  |
| `public static bool operator !=(NativeTrayMenuItem? left, NativeTrayMenuItem? right)` |  |
| `public static bool operator ==(NativeTrayMenuItem? left, NativeTrayMenuItem? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(NativeTrayMenuItem? other)` |  |
| `public void Deconstruct(out int Id, out NativeTrayMenuItemKind Kind, out string Text)` |  |
| `public int Id { get; init; }` |  |
| `public NativeTrayMenuItemKind Kind { get; init; }` |  |
| `public string Text { get; init; }` |  |

### `NativeTrayMenuItemKind`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public enum NativeTrayMenuItemKind` | Native menu representation; applications use the typed SystemTrayMenu API. |
| `Command = 0` | An executable command. |
| `Separator = 1` | A visual separator. |
| `Submenu = 2` | A nested menu. |
| `CheckBox = 3` | An independent boolean choice. |
| `Radio = 4` | An exclusive choice indicator. |

### `SystemNotification`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SystemNotification : IEquatable<SystemNotification>` | A transient notification. Windows limits titles/messages to 63/255 UTF-16 units. |
| `public SystemNotification(string Title, string Message, SystemNotificationIcon Icon = SystemNotificationIcon.Information)` | A transient notification. Windows limits titles/messages to 63/255 UTF-16 units. |
| `public override string ToString()` |  |
| `public static bool operator !=(SystemNotification? left, SystemNotification? right)` |  |
| `public static bool operator ==(SystemNotification? left, SystemNotification? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SystemNotification? other)` |  |
| `public void Deconstruct(out string Title, out string Message, out SystemNotificationIcon Icon)` |  |
| `public string Title { get; init; }` |  |
| `public string Message { get; init; }` |  |
| `public SystemNotificationIcon Icon { get; init; }` |  |

### `SystemNotificationIcon`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | No standard notification icon. |
| `Information = 1` | An information icon. |
| `Warning = 2` | A warning icon. |
| `Error = 3` | An error icon. |

### `TrayIconOptions`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TrayIconOptions : IEquatable<TrayIconOptions>` | A prepared native tray snapshot. Images contain packed premultiplied BGRA pixels. |
| `public override string ToString()` |  |
| `public static bool operator !=(TrayIconOptions? left, TrayIconOptions? right)` |  |
| `public static bool operator ==(TrayIconOptions? left, TrayIconOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TrayIconOptions? other)` |  |
| `public TrayIconOptions()` |  |

### `TrayMenuOpeningEventArgs`

Namespace `Nazor`. [Source](../../Sources/Platforms/Nazor.Platform/DesktopOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TrayMenuOpeningEventArgs : EventArgs` | A synchronous menu-opening request. Cancellation is sticky and available only when CanCancel is true. |
| `public TrayMenuOpeningEventArgs(bool canCancel)` | A synchronous menu-opening request. Cancellation is sticky and available only when CanCancel is true. |

### `FileDialogFilter`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FileDialogFilter : IEquatable<FileDialogFilter>` | A choice of file types in a file dialog: a name and its patterns (`*.csv`).<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Patterns`. The patterns (IReadOnlyList<string>). |
| `public FileDialogFilter(string Name, IReadOnlyList<string> Patterns)` | A choice of file types in a file dialog: a name and its patterns (`*.csv`).<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Patterns`. The patterns (IReadOnlyList<string>). |
| `public override string ToString()` |  |
| `public static bool operator !=(FileDialogFilter? left, FileDialogFilter? right)` |  |
| `public static bool operator ==(FileDialogFilter? left, FileDialogFilter? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FileDialogFilter? other)` |  |
| `public void Deconstruct(out string Name, out IReadOnlyList<string> Patterns)` |  |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public IReadOnlyList<string> Patterns { get; init; }` | The patterns (IReadOnlyList<string>). |

### `FileDialogKind`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `Open = 0` | Prompts for an existing file or files. |
| `Save = 1` | Prompts for a destination file path. |
| `Folder = 2` | Prompts for an existing folder. |

### `FileDialogRequest`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FileDialogRequest : IEquatable<FileDialogRequest>` | What a file dialog of the system shows.<br>`Kind`. The file dialog kind represented by this value. |
| `public FileDialogRequest(FileDialogKind Kind)` | What a file dialog of the system shows.<br>`Kind`. The file dialog kind represented by this value. |
| `public override string ToString()` |  |
| `public static bool operator !=(FileDialogRequest? left, FileDialogRequest? right)` |  |
| `public static bool operator ==(FileDialogRequest? left, FileDialogRequest? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FileDialogRequest? other)` |  |
| `public void Deconstruct(out FileDialogKind Kind)` |  |
| `public FileDialogKind Kind { get; init; }` | The file dialog kind represented by this value. |

### `GraphicsDeviceException`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/IOpenGlWindowContext.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class GraphicsDeviceException : Exception` | A recognized failure to initialize or use a native graphics device. |
| `public GraphicsDeviceException(string message, Exception? innerException = null)` | Creates a device error with its native failure description.<br>`message`. The device failure.<br>`innerException`. The underlying native error, if available. |

### `IOpenGlWindowContext`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/IOpenGlWindowContext.cs)

| Declaration | Contract |
| --- | --- |
| `public interface IOpenGlWindowContext : IDisposable` | An OpenGL window context prepared by the UI owner. Initialization, graphics operations and disposal run exclusively on the rendering thread; the native window must outlive disposal. Other graphics APIs can use separate contracts. |
| `void Initialize()` | Initializes native resources on the rendering thread before MakeCurrent is called. |
| `void ReleaseDisplay()` | Releases display driver references and UI-owned presentation resources after the render worker has released the context.<br>Called by the UI owner, including failed initialization. The shared worker lives until process exit. |
| `nint GetProcAddress(string name)` | Resolves a graphics entry point for the current context.<br>`name`. The native function name.<br>**Returns**. The entry point, or zero when unavailable. |
| `void MakeCurrent(int width, int height)` | Makes the default window framebuffer current and resizes it to physical pixel dimensions.<br>`width`. The framebuffer width.<br>`height`. The framebuffer height. |
| `void Present()` | Presents the default framebuffer, synchronized to the display when supported by the driver. |
| `bool IsEmbedded { get; }` | Whether this context implements OpenGL ES rather than desktop OpenGL. |
| `bool IsPresentationSynchronized { get; }` | Whether Present waits for display synchronization, allowing an immediate next animation frame. |

### `WindowFrameGeometry`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowFrameGeometry.cs)

| Declaration | Contract |
| --- | --- |
| `public static class WindowFrameGeometry` | Shared geometry for desktop window frames drawn by Nazor. |
| `public static Rect ResizeGripBounds(Size clientSize, CornerRadius radius = default(CornerRadius))` | Gets the logical client grip rectangle near the lower-right corner, accounting for corner rounding. |
| `public static uint ResizeEdgeAt(Size frameSize, Point point, float inset, float outset, float? cornerLength)` | Returns the hit edge bits top `1`, bottom `2`, left `4` and right `8`, or `0` outside the resize bands. Coordinates and distances are logical. Null corner length uses the overlapping edge bands. |
| `public static CornerRadius NormalizeRadius(CornerRadius radius, Size frameSize)` | Scales corner radii proportionally when adjacent corners would overlap. |

### `IWindowHost`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `void OnRenderingStopping()` | Stops rendering resources before the backend destroys its native surface. |
| `void OnPresented(TimeSpan duration)` | Reports software backend submission completion after OnPaint, including a no-op paint.<br>`duration`. The backend's CPU submission/presentation duration. |
| `void OnAlwaysOnTopChanged(bool value)` | Reports the window manager's effective always-on-top state. |
| `void OnActivationChanged(bool active)` | Reports the desktop's active decoration state when it is separate from keyboard focus. |
| `DrawBorder? GetClientFrameBorder(Size frameSize)` | Gets the optional border of the full client-drawn frame in logical coordinates. |
| `void PaintClientFrameBorder(PaintTarget target, DrawBorder border)` | Paints the border over a CPU frame whose caption and content were drawn separately. |
| `void OnTitleBarMenuRequested(Point point)` | Shows the Nazor title bar menu at a point in logical viewport coordinates. |
| `bool UsesGpuRendering { get; }` | Whether OnPaint submits a GPU frame and does not require a CPU pixel buffer. |

### `PaintTarget`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct PaintTarget : IEquatable<PaintTarget>` | A backend-owned paint target: top-down BGRA premultiplied CPU pixels, or a GPU surface description.<br>`Pixels`. The address of the CPU pixel buffer, or zero for GPU submission.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system.<br>`Stride`. The number of bytes between the starts of successive pixel rows.<br>`Scale`. The scale (float). |
| `public PaintTarget()` |  |
| `public PaintTarget(nint Pixels, int Width, int Height, int Stride, float Scale)` | A backend-owned paint target: top-down BGRA premultiplied CPU pixels, or a GPU surface description.<br>`Pixels`. The address of the CPU pixel buffer, or zero for GPU submission.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system.<br>`Stride`. The number of bytes between the starts of successive pixel rows.<br>`Scale`. The scale (float). |
| `public override string ToString()` |  |
| `public static bool operator !=(PaintTarget left, PaintTarget right)` |  |
| `public static bool operator ==(PaintTarget left, PaintTarget right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(PaintTarget other)` |  |
| `public void Deconstruct(out nint Pixels, out int Width, out int Height, out int Stride, out float Scale)` |  |
| `public nint Pixels { get; init; }` | The address of the CPU pixel buffer, or zero for GPU submission. |
| `public int Width { get; init; }` | The width of the value in its coordinate system. |
| `public int Height { get; init; }` | The height of the value in its coordinate system. |
| `public int Stride { get; init; }` | The number of bytes between the starts of successive pixel rows. |
| `public float Scale { get; init; }` | The scale (float). |
| `public int TopInset { get; init; }` | Gets the native top decoration inset in physical pixels; GPU surfaces include it in Height. |
| `public bool SeparateTopDecoration { get; init; }` | Whether a CPU backend paints its caption above the supplied client buffer. |
| `public PixelRect? TransparentRegion { get; init; }` | Gets an optional physical pixel region cleared to transparent after GPU drawing, for native caption composition. |
| `public bool ForceFullRedraw { get; init; }` | Whether the window system needs a complete frame even if the application scene did not change. |

### `PlatformCursor`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `Arrow = 0` | Displays a cursor indicating the default arrow. |
| `Hand = 1` | Displays a cursor indicating a link-selection hand. |
| `IBeam = 2` | Displays a cursor indicating a text-selection I-beam. |
| `SizeWestEast = 3` | Displays a cursor indicating horizontal resizing. |
| `SizeNorthSouth = 4` | Displays a cursor indicating vertical resizing. |
| `SizeAll = 5` | Displays a cursor indicating movement in all directions. |
| `No = 6` | Displays a cursor indicating an unavailable operation. |
| `Wait = 7` | Displays a cursor indicating a busy operation. |
| `AppStarting = 8` | Displays a cursor indicating background progress. |
| `Help = 9` | Displays a cursor indicating contextual help. |
| `Cross = 10` | Displays a cursor indicating a precision crosshair. |
| `SizeNorthwestSoutheast = 11` | Displays a cursor indicating diagonal resizing from the top-left or bottom-right corner. |
| `SizeNortheastSouthwest = 12` | Displays a cursor indicating diagonal resizing from the top-right or bottom-left corner. |

### `WindowCentering`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `None = 0` | Where the system puts new windows. |
| `Owner = 1` | Centered on its owner (on the screen without one). |
| `Screen = 2` | Centered on the work area of the screen. |

### `WindowHitTest`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `Client = 0` | Content: the host handles the pointer. |
| `Caption = 1` | The title bar: dragging moves the window, a double click maximizes it. |
| `MaximizeButton = 2` | The maximize button: Windows 11 shows the snap layouts over it. |

### `WindowPlacement`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowPlacement : IEquatable<WindowPlacement>` | Normal window bounds with an explicit coordinate representation and optional portable client size. Without coordinate metadata, placement bounds use Windows native coordinates.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system.<br>`IsMaximized`. The is maximized (bool). |
| `public WindowPlacement(int X, int Y, int Width, int Height, bool IsMaximized)` | Normal window bounds with an explicit coordinate representation and optional portable client size. Without coordinate metadata, placement bounds use Windows native coordinates.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system.<br>`IsMaximized`. The is maximized (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowPlacement? left, WindowPlacement? right)` |  |
| `public static bool operator ==(WindowPlacement? left, WindowPlacement? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WindowPlacement? other)` |  |
| `public void Deconstruct(out int X, out int Y, out int Width, out int Height, out bool IsMaximized)` |  |
| `public int X { get; init; }` | The horizontal component of the value. |
| `public int Y { get; init; }` | The vertical component of the value. |
| `public int Width { get; init; }` | The width of the value in its coordinate system. |
| `public int Height { get; init; }` | The height of the value in its coordinate system. |
| `public bool IsMaximized { get; init; }` | The is maximized (bool). |

### `WindowPlacementCapabilities`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowEnvironment.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowPlacementCapabilities : IEquatable<WindowPlacementCapabilities>` | Independent placement functions offered by a backend.<br>`CanRestoreSize`. The can restore size (bool).<br>`CanRestorePosition`. The can restore position (bool).<br>`CanSelectScreen`. The can select screen (bool).<br>`CanCenter`. The can center (bool). |
| `public WindowPlacementCapabilities()` |  |
| `public WindowPlacementCapabilities(bool CanRestoreSize, bool CanRestorePosition, bool CanSelectScreen, bool CanCenter)` | Independent placement functions offered by a backend.<br>`CanRestoreSize`. The can restore size (bool).<br>`CanRestorePosition`. The can restore position (bool).<br>`CanSelectScreen`. The can select screen (bool).<br>`CanCenter`. The can center (bool). |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowPlacementCapabilities left, WindowPlacementCapabilities right)` |  |
| `public static bool operator ==(WindowPlacementCapabilities left, WindowPlacementCapabilities right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(WindowPlacementCapabilities other)` |  |
| `public void Deconstruct(out bool CanRestoreSize, out bool CanRestorePosition, out bool CanSelectScreen, out bool CanCenter)` |  |
| `public bool CanRestoreSize { get; init; }` | The can restore size (bool). |
| `public bool CanRestorePosition { get; init; }` | The can restore position (bool). |
| `public bool CanSelectScreen { get; init; }` | The can select screen (bool). |
| `public bool CanCenter { get; init; }` | The can center (bool). |

### `WindowPlacementCoordinates`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowEnvironment.cs)

| Declaration | Contract |
| --- | --- |
| `WindowsWorkAreaPixels = 0` | Stores native Windows window bounds in work-area physical pixels. |
| `LogicalClient = 1` | Stores placement using portable logical client coordinates. |

### `WindowsBadgeAppearance`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowsBadgeAppearance.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowsBadgeAppearance : IEquatable<WindowsBadgeAppearance>` | Resolved Windows launcher badge style in logical pixels. The shell reserves a fixed 16 by 16 logical pixel overlay. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowsBadgeAppearance? left, WindowsBadgeAppearance? right)` |  |
| `public static bool operator ==(WindowsBadgeAppearance? left, WindowsBadgeAppearance? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WindowsBadgeAppearance? other)` |  |
| `public WindowsBadgeAppearance()` |  |
| `public IFontProvider? Fonts { get; init; }` | Borrowed application font provider returning Skia font faces, used on the UI thread; null uses installed system fonts. The owning window must keep it alive while publishing. |
| `public int FontVersion { get; init; }` | Font provider version captured when this snapshot was created, so registrations invalidate retained badges. |
| `public DisplayList? Background { get; init; }` | Resolved decorative background layers. Images are borrowed from the owning renderer and must remain alive while publishing. |
| `public bool ClipBackgroundToPadding { get; init; }` | Whether background color and layers use the padding box inside the border; false uses the border box. |
| `public Thickness? BorderWidths { get; init; }` | Optional border widths by side, overriding the uniform width. Hidden sides must have zero width. |
| `public BorderSide? BorderTop { get; init; }` | Optional top border appearance, overriding the uniform appearance. |
| `public BorderSide? BorderRight { get; init; }` | Optional right border appearance, overriding the uniform appearance. |
| `public BorderSide? BorderBottom { get; init; }` | Optional bottom border appearance, overriding the uniform appearance. |
| `public BorderSide? BorderLeft { get; init; }` | Optional left border appearance, overriding the uniform appearance. |
| `public Matrix Transform { get; init; }` | Transform of the complete badge, including shadows and mask, within the fixed overlay. |
| `public DisplayList? Mask { get; init; }` | Optional alpha mask drawn in overlay coordinates after badge composition and before transformation. Null disables masking; an empty list hides the badge. Images are borrowed from the owning renderer. |
| `public Func<Size, Point>? ImagePositionResolver { get; init; }` | Optional resolver called on the UI thread with free space after image fitting, which may be negative. Overrides ImagePosition and ImageOffset and must be deterministic for this snapshot. |
| `public Color Color { get; init; }` | Count foreground. |
| `public float Width { get; init; }` | Container width, constrained to the overlay area. |
| `public float Height { get; init; }` | Container height, constrained to the overlay area. |
| `public float Opacity { get; init; }` | Complete badge opacity from zero to one. |
| `public CornerRadius Radius { get; init; }` | Container corner radii. |
| `public IReadOnlyList<string> FontFamilies { get; init; }` | Count font families in fallback order. The caller must not mutate this collection. |
| `public float FontSize { get; init; }` | Count font size before shrinking. |
| `public int FontWeight { get; init; }` | Count font weight from 1 to 1000. |
| `public float TextAlign { get; init; }` | Horizontal alignment: zero is left, one is right. |
| `public float VerticalAlign { get; init; }` | Vertical alignment: zero is top, one is bottom. |
| `public bool TextFit { get; init; }` | Whether the count shrinks to fit the content box. |
| `public WindowsBadgeImageFit ImageFit { get; init; }` | How the image fits the content box. |
| `public Point ImagePosition { get; init; }` | Image alignment as a fraction of the space left after fitting. |
| `public float ImageOpacity { get; init; }` | Image opacity from zero to one, excluding the container. |
| `public IReadOnlyList<DrawShadow> Shadows { get; init; }` | Container shadows in painting order, first on top. The caller must not mutate this collection. |
| `public IReadOnlyList<DrawShadow> TextShadows { get; init; }` | Count shadows; inset and spread are unsupported. The caller must not mutate this collection. |

### `WindowsBadgeImageFit`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowsBadgeAppearance.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WindowsBadgeImageFit` | Image fitting modes for a Windows launcher badge. |
| `Fill = 0` | Stretch to fill the content box. |
| `Contain = 1` | Fit the complete image, preserving its aspect ratio. |
| `Cover = 2` | Fill the content box, cropping excess image content. |
| `None = 3` | Use the prepared image's logical size. |
| `ScaleDown = 4` | Contain the image without enlarging it. |

### `WindowsTitleBarColors`

Namespace `Nazor.Platform`. [Source](../../Sources/Platforms/Nazor.Platform/WindowAppearance.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowsTitleBarColors : IEquatable<WindowsTitleBarColors>` | Native Windows title bar colors. Null restores the system color; custom title bars use only button colors. |
| `public override string ToString()` |  |
| `public static bool operator !=(WindowsTitleBarColors? left, WindowsTitleBarColors? right)` |  |
| `public static bool operator ==(WindowsTitleBarColors? left, WindowsTitleBarColors? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(WindowsTitleBarColors? other)` |  |
| `public WindowsTitleBarColors()` |  |


<a id="type-windowoptions"></a>

### `WindowOptions`

Namespace `Nazor.Platform`.

Initial properties of a window; sizes are logical units of the client area.

| Declaration | Contract |
| --- | --- |
| `public sealed record WindowOptions : IEquatable<WindowOptions>` | Initial properties of a window; sizes are logical units of the client area. |
| `public WindowOptions(string Title = "Nazor", double Width = 800, double Height = 600)` | Initial properties of a window; sizes are logical units of the client area. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(WindowOptions? left, WindowOptions? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(WindowOptions? left, WindowOptions? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(WindowOptions? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out string Title, out double Width, out double Height)` | Standard .NET value or component semantics. |
| `public string Title { get; init; }` | The title (string). |
| `public double Width { get; init; }` | The width of the value in its coordinate system. |
| `public double Height { get; init; }` | The height of the value in its coordinate system. |
| `public IPlatformWindow? Owner { get; init; }` | The window this one belongs to: it stays above it and is minimized with it. |
| `public bool IsDialog { get; init; }` | A dialog frame; defaults disable minimize, maximize and user resize, independently overridable. |
| `public bool IsAlwaysOnTop { get; init; }` | Keeps the window above non-topmost windows without activating it. |
| `public WindowDecorations Decorations { get; init; }` | The complete decoration configuration requested at native creation. |
| `public BackgroundMode BackgroundMode { get; init; }` | How the client surface is composited. |
| `public bool IsVisibleInTaskbar { get; init; }` | Requests taskbar participation; false can also exclude task switching. |
| `public bool? IsMinimizable { get; init; }` | Null inherits the window or dialog defaults. |
| `public bool? IsMaximizable { get; init; }` | Gets or sets the requested maximization policy; null uses the standard window or dialog default. |
| `public bool? IsResizable { get; init; }` | Gets or sets the requested interactive resize policy; null uses the standard window or dialog default. |
| `public bool? FullScreenEnabled { get; init; }` | Gets or sets the requested full-screen policy; null uses the standard window or dialog default. |
| `public WindowCentering Centering { get; init; }` | Gets or sets how the native window is centered when first shown. |

<a id="type-iplatformwindow"></a>

### `IPlatformWindow`

Namespace `Nazor.Platform`.

A native window driven by a backend.

| Declaration | Contract |
| --- | --- |
| `public interface IPlatformWindow : IDisposable` | A native window driven by a backend. |
| `NativeWindowHandle? NativeHandle { get; }` | Borrowed operating-system identifier on the UI dispatcher; null before creation, after close, or when unsupported. Do not destroy or release it. See [native interop](../Guide/13-Windows-and-Dialogs.md#native-window-interop). |
| `string? ExportedHandle { get; }` | Opaque cross-process owner token, or null when unavailable. Expires with its window; Wayland exports a token instead of its process-local surface pointer. |
| `bool SetOwner(string ownerHandle)` | UI-thread transient-parent request for a token from another window on the same desktop. Returns false when unsupported or rejected. Desktop policy determines presentation; this does not transfer ownership or enforce lifetime. |
| `IOpenGlWindowContext? CreateOpenGlContext()` | Prepares an OpenGL window context on the UI thread, or returns null when unsupported. Graphics initialization runs on the rendering thread. A context owned by the caller, disposed before the native window is destroyed. |
| `void ResetRendering()` | Resets backend paint scheduling after a renderer transition; called on the UI thread. |
| `DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper)` | Adds native client decorations to a GPU frame without accessing rendering-thread state. A complete window display list. |
| `DisplayList ComposeFrame(DisplayList content, float topInset, IFontProvider fonts, ITextShaper shaper, Color background)` | Composes the window frame with the current client background. |
| `void SetClientFrameAppearance(CornerRadius radius, IReadOnlyList<DrawShadow> shadows)` | Applies the window root's exterior shadow and corner radius. |
| `void SetFrameResizeGeometry(float? inset, float? outset, float? cornerLength)` | Applies logical widths to the client resize zones; null uses the backend default for that zone. |
| `void Move(int? x, int? y)` | Moves the outer frame in screen pixels; null axes preserve the current coordinate. Non-normal windows update restore bounds. |
| `void SetSizeLimits(double? minWidth, double? minHeight, double? maxWidth, double? maxHeight)` | Sets logical client size limits; null removes the corresponding limit. |
| `void SetWindowsTitleBarColors(WindowsTitleBarColors colors)` | Applies native Windows title bar colors; ignored on other backends. |
| `IDisposable? AddSystemMenuCommand(string text, Action invoked)` | Adds a command to the native system menu when supported. Call and dispose the registration on the UI thread. A registration that removes the command, or null when native menu customization is unavailable. |
| `void SetWindowsBadgeAppearance(WindowsBadgeAppearance appearance)` | Updates the style used when this window publishes the application launcher badge. Other platforms ignore it. |
| `void SetTitleBarMenuMode(WindowTitleBarMenuMode value)` | Chooses a context menu for title bar clicks delivered to the backend. |
| `ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default(CancellationToken))` | Applies an exact decoration request and completes after native acceptance; unsupported requests must not substitute parts. The configuration accepted by the backend. |
| `void SetIcon(IReadOnlyList<RenderedImage>? images)` | Sets candidate window icons as top-down premultiplied BGRA bitmaps. The backend copies the selected data before returning; null restores the application icon. Ignored unless CanSetWindowIcon is true. |
| `void Show(bool activate = true)` | Shows the window without necessarily activating it. |
| `void Hide()` | Hides without destroying the window. |
| `void Close()` | Asks the window to close (the host can still cancel). |
| `void Activate()` | Brings the window to the front with the keyboard focus. |
| `void SetOperations(WindowOperations operations)` | Updates the user operations requested for this window. |
| `void SetAlwaysOnTop(bool value)` | Changes topmost status without activating the window. |
| `void SetTaskbarVisibility(bool value)` | Changes taskbar participation, preserving native state and activation. |
| `void SetResizeGrip(bool visible)` | Enables hit testing for the client resize grip drawn by Nazor when the window can resize. |
| `bool SetWindowsTitleBarHeight(WindowsTitleBarHeight height)` | Requests the integrated Windows title bar height; colors are configured separately. Whether the native title bar is available. |
| `void SetState(WindowState state)` | Maximizes, minimizes or restores the window. |
| `void ExitFullScreen()` | Exit full screen to the previously saved placement and state; otherwise no effect. |
| `void SetDarkFrame(bool dark)` | The frame (title bar, borders) of the system in its dark colors, for a dark application. |
| `BackdropResult SetBackdrop(BackdropSelection backdrop)` | Resolves the backdrop request and fallback for this backend. An inactive result keeps the opaque theme background; acceptance does not guarantee compositor visibility. The resulting BackdropResult value. |
| `void SetEnabled(bool enabled)` | Enables or disables input to the window (the owner of a modal dialog is disabled). |
| `void Center(WindowCentering centering)` | Moves the window to the center of its owner or of the screen, inside the work area. |
| `WindowPlacement? GetPlacement()` | Where the window is (its normal bounds, even while maximized), or null when unknown. The resulting WindowPlacement? value. |
| `bool SetPlacement(WindowPlacement placement)` | Puts a window not shown yet where `placement` says (maximized when it shows); false, and nothing changes, when that place is on no screen any more. true when the operation succeeds or the condition is satisfied; otherwise, false. |
| `void Invalidate()` | Schedules a paint; repeated requests before the paint are merged. |
| `void RequestAnimationFrame(TimeSpan delay)` | Schedules a paint for the next animation frame, not sooner than `delay`: frames are paced by the backend instead of following each other as fast as possible. |
| `void Resize(Size logicalSize)` | Changes the window's logical client size. |
| `void SetTextInputCaret(Rect? caret)` | The caret of the focused text input in logical client coordinates: positions the IME windows and the (invisible) system caret. Null disables the IME, as no text input has the focus. |
| `void SetPointerCapture(bool capture)` | Enables or releases native pointer capture for this window. |
| `bool UsesClientFrameAppearance { get; }` | Whether this backend accepts client frame style from the window root. |
| `bool HasDefaultClientFrameAppearance { get; }` | Whether the backend uses the default Nazor corner radius and shadow for this window. |
| `bool DrawClientFrame { get; }` | Whether the backend is currently showing a client-drawn frame. |
| `CornerRadius ClientFrameRadius { get; }` | Gets the effective client frame corner radii in logical pixels. |
| `bool HasRoundedClientFrame { get; }` | Whether the current frame requires an alpha clear around its rounded corners. |
| `PlatformKind? Platform { get; }` | The platform whose UI conventions this backend implements; null when unspecified. |
| `nint Handle { get; }` | Gets the backend's internal identifier; use NativeHandle for operating-system interop. |
| `float Scale { get; }` | Gets the number of physical pixels per logical unit. |
| `TextInputPolicy TextInputPolicy { get; }` | Gets the keyboard editing conventions supplied by the platform. |
| `float WheelScrollDistance { get; }` | Gets the logical scroll distance per wheel detent. |
| `bool WheelScrollByPage { get; }` | Gets whether a wheel detent scrolls by a viewport page rather than a fixed distance. |
| `WindowEnvironment Environment { get; }` | Gets the safe-area insets and keyboard occlusion relative to the client viewport. |
| `WindowPlacementCapabilities PlacementCapabilities { get; }` | Gets the placement representations and restore operations supported by the backend. |
| `Size ClientSize { get; }` | Gets the size of the native client area in logical units. |
| `(int X, int Y)? Position { get; }` | Outer window position in physical screen pixels, or null when unsupported. |
| `string Title { get; set; }` | Gets or sets the title displayed by this window, dialog, or message. |
| `ImageSource? ApplicationIcon { get; }` | Application icon source when the backend can expose it to application content. |
| `WindowChrome Chrome { get; }` | Gets the owner of the frame and the window controls. |
| `bool IsClosed { get; }` | Gets whether the native window has closed. |
| `WindowState State { get; }` | Gets the current normal, minimized, maximized, or full-screen window state. |
| `bool ReportsActivationSeparately { get; }` | Whether the backend reports activation independently of focus. |
| `WindowButtonsLayout SystemButtonsLayout { get; }` | System control placement, independent of native visibility; standard right-hand layout when unspecified. |
| `WindowButtonsMetrics SystemButtonsMetrics { get; }` | Integrated native caption-button insets in logical units on a custom client title bar; zero when hidden or unsupported. |
| `(float Width, float Height) SystemButtonsSize { get; }` | Gets the total logical width and height reserved for integrated native caption controls. |
| `WindowCapabilities Capabilities { get; }` | Gets the window operations and decorations supported by the current backend and application settings. |
| `Rect ResizeGripBounds { get; }` | Gets the effective grip area in client coordinates, or an empty rectangle when hidden. |
| `bool UsesWindowsDecorations { get; }` | True for the Windows NSS native decorations and metrics. |
| `bool PrefersReducedMotion { get; }` | The user turned animations off in the system settings. |
| `bool PrefersDarkColorScheme { get; }` | The system asks applications for a dark color scheme (the app mode of Windows). |
| `bool PrefersHighContrast { get; }` | A contrast theme is on: applications draw with the system colors only (CSS forced colors). |
| `bool PrefersTouchInput { get; }` | Host preference for larger touch targets, false by default when unavailable. This does not classify native pointer events. Hosts notify OnSystemSettingsChanged when the preference changes. |
| `IReadOnlyDictionary<string, Color> SystemColors { get; }` | Available CSS system colors, accent shades and PascalCase semantic palette roles (Tooltip, TooltipText, Header, HeaderText, etc.). Supply only available values; missing colors are resolved for ColorProfile and the effective light/dark scheme. Supply paired foreground/background colors and use OnSystemSettingsChanged when the palette changes. |
| `SystemColorProfile ColorProfile { get; }` | The fallback palette for this desktop. Linux backends should identify KDE or GNOME when known. |
| `IClipboard Clipboard { get; }` | The system clipboard, owned by this window. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.
