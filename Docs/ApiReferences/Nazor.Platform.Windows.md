# Nazor.Platform.Windows

The backend reads `Nazor.ApplicationId` metadata from the entry assembly and sets the process AppUserModelID before creating native windows in unpackaged processes. Packaged processes retain their assigned package identity. See [application identity](../Guide/03-Projects-and-Builds.md#application-identity).

Windows platform implementation and UI-thread hosting.

For a client title bar on a Windows 11 native frame, the adapter passes the application-owned caption strip through the Windows App SDK input sink to Nazor's shared hit testing. Native caption buttons keep their own input. Passthrough geometry is refreshed with native size/DPI metrics and cleared when restoring the system caption. If Windows denies passthrough registration during a frame change, Win32 hit testing remains active and the next caption refresh can retry registration. See [title-bar input](../Guide/14-Title-Bars-and-Platform-Integration.md#compose-a-title-bar).

The executable's SDK restores the Microsoft packages that supply Windows projection and windowing assets. See [Windows dependencies](../Guide/03-Projects-and-Builds.md#windows-dependencies) and [framework-specific dependency versions](../Guide/28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions).

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Types

- [`Win32Platform`](#type-win32platform)
- [`WindowsPlatformHost`](#type-windowsplatformhost)

<a id="type-win32platform"></a>

## `Win32Platform`

Namespace: `Nazor.Platform.Windows`

```csharp
public sealed unsafe class Win32Platform : IPlatform, IDisposable
```

The Win32 backend: per-monitor DPI awareness, one window class for the process, a message-only window that wakes the dispatcher with PostMessage (thread messages are lost in modal loops), and a GetMessage loop that sleeps when there is nothing to do.

Window geometry limits are enforced through `WM_GETMINMAXINFO` and checked resize/placement paths. Logical client sizes are converted using the window DPI and current native frame. Maximized bounds use the target monitor work area and honor configured maximum dimensions; fullscreen retains independent visible bounds. Screen positioning converts the normal `WINDOWPLACEMENT` work-area coordinates to the physical screen coordinates exposed by `IWindow.X`/`Y`.

GPU presentation uses a disabled, input-transparent child window sized to the client area, behind native caption controls and resize grips. The UI owner creates, resizes and releases this presentation window; the render worker exclusively initializes, uses and disposes its WGL resources. Caption transitions therefore resize the actual GPU drawable independently of the top-level window frame. Software presentation uses a separate cached display context, so native paint-region clipping does not restrict application damage updates.

Native title bar color overrides use AppWindowTitleBar on Windows 11, including when the default system caption is retained. With no backdrop, the backend selects immersive light/dark mode, sets `DWMWA_SYSTEMBACKDROP_TYPE` to `DWMSBT_NONE` and uses an opaque caption (`#eef4f9` light, `#1e2123` dark). System captions without NSS overrides are drawn by DWM using `DWMWA_CAPTION_COLOR`. Returning from a custom caption resets and releases the AppWindow caption renderer when its custom-theme rules no longer match, so it cannot repaint over the system frame. Explicit NSS colors continue to use AppWindow, with matching solid caption/button defaults when there is no material. High contrast retains Windows' native colors. Explicit Mica, Mica Alt and Acrylic clear the solid caption override and extend across the entire window. Explicit NSS caption colors take precedence. The platform caches the relevant projections, clears window references on native destruction, and finalizes abandoned projections before shutting down its dispatcher queue. Windows windowing modules are pinned for the process lifetime so remaining WinRT COM references cannot call unloaded code after platform disposal. Platform shutdown performs a garbage collection and waits for pending finalizers before queue teardown.

[Source](../../Sources/Platforms/Nazor.Platform.Windows/Win32Platform.cs)

### Members

#### `DesktopIntegration`

```csharp
public IPlatformDesktopIntegration DesktopIntegration {get; }
```

Supplies independently owned tray registrations, notifications and application-wide launcher tasks/count/icon badges. Launcher state is shared across windows; the backend publishes overlays through an eligible taskbar window. The publishing window supplies the resolved `WindowsBadgeAppearance`, including application/private fonts, layered backgrounds, independent borders, full mathematical image positioning, static transforms and raster/SVG alpha masks; changing that window's appearance redraws the retained badge. NSS dimensions stay within the shell's fixed 16 × 16 logical pixel overlay. See the [launcher contract](../Guide/15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges) and [Windows badge styling](../Guide/19-NSS-Property-Reference.md#windows-launcher-badges).

Gets the Windows tray, notification and application launcher implementation.

#### `Capabilities`

```csharp
public WindowCapabilities Capabilities {get; }
```

Gets the native window operations and decorations supported by this Windows backend.

#### `Win32Platform`

```csharp
public Win32Platform()
```

Initializes a new instance of Win32Platform and applies the entry assembly's Nazor.ApplicationId metadata to unpackaged processes before creating native windows.

#### `IsPerMonitorDpiAware`

```csharp
public static bool IsPerMonitorDpiAware {get; }
```

True when the process uses per-monitor (v2) DPI awareness.

#### `CreateWindow`

```csharp
public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
```

Creates a native window connected to the supplied Nazor window host.

| Parameter | Description |
| --- | --- |
| `host` | The Nazor window host that receives native lifecycle, painting, and input notifications. |
| `options` | The options used to configure the operation. |

Returns: The native platform window connected to the host.

#### `ShowFileDialog`

```csharp
public IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)
```

Displays a Windows file or folder dialog owned by the supplied native window.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `request` | The native file-dialog operation and options. |

Returns: The selected paths, or null if the user cancels the dialog.

#### `Run`

```csharp
public int Run(UIDispatcher uiDispatcher)
```

Runs the Windows message loop using the supplied UI dispatcher.

| Parameter | Description |
| --- | --- |
| `uiDispatcher` | The dispatcher used to execute work inside the native event loop. |

Returns: The exit code supplied when the Windows message loop terminates.

#### `Quit`

```csharp
public void Quit(int code = 0)
```

Requests termination of the platform event loop with the specified exit code.

| Parameter | Description |
| --- | --- |
| `code` | The exit code reported when the platform loop terminates. |

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.


<a id="type-windowsplatformhost"></a>

## `WindowsPlatformHost`

Namespace: `Nazor.Platform.Windows`

```csharp
public sealed class WindowsPlatformHost : IPlatformHost
```

Starts the Windows backend on an STA thread and releases it after the UI loop.

[Source](../../Sources/Platforms/Nazor.Platform.Windows/WindowsPlatformHost.cs)

### Members

#### `Run`

```csharp
public int Run(Func<IPlatform, int> application)
```

Runs the application within the Windows UI-thread context and owns the platform lifetime.

| Parameter | Description |
| --- | --- |
| `application` | The callback invoked with the platform created by the host. |

Returns: The application callback's exit code.

Throws `ArgumentNullException`: application is null.

#### `RunAsync`

```csharp
public Task<int> RunAsync(Func<IPlatform, int> application)
```

Runs the application on a dedicated STA thread, even when called from an STA thread, without blocking the caller. Completes with the callback's exit code after the Windows backend is released. Callback exceptions fault the returned task.

Throws `ArgumentNullException` for a null callback or `PlatformNotSupportedException` outside Windows.

#### `RunOnUiThread`

```csharp
public static int RunOnUiThread(Func<int> run)
```

Executes the callback on an STA thread, creating a dedicated UI thread when required.

| Parameter | Description |
| --- | --- |
| `run` | The application callback to execute on the UI thread. |

Returns: The exit code returned by the callback.

Throws `ArgumentNullException`: run is null.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `Win32Platform`

Namespace `Nazor.Platform.Windows`. [Source](../../Sources/Platforms/Nazor.Platform.Windows/Win32Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Win32Platform : IPlatform, IDisposable` | The Win32 backend: per-monitor DPI awareness, one window class for the process, a message-only window that wakes the dispatcher with PostMessage (thread messages are lost in modal loops), and a GetMessage loop that sleeps when there is nothing to do. |
| `public string BackendName { get; }` |  |

### `WindowsPlatformHost`

Namespace `Nazor.Platform.Windows`. [Source](../../Sources/Platforms/Nazor.Platform.Windows/WindowsPlatformHost.cs)

| Declaration | Contract |
| --- | --- |
| `public WindowsPlatformHost()` |  |
