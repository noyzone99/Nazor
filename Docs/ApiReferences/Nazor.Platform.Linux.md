# Nazor.Platform.Linux

Desktop window, tray and launcher identity uses the entry assembly's `Nazor.ApplicationId` metadata supplied by `NazorApplicationId`. Explicit non-SDK hosts without metadata retain the legacy assembly-derived ID. See [application identity and desktop entries](../Guide/03-Projects-and-Builds.md#application-identity).

Native Linux Wayland/X11 desktop hosting and explicit DRM/KMS single-view hosting
without GTK or Qt. Namespace: `Nazor.Platform.Linux`.

## Desktop hosting

Wayland and X11 windows expose the embedded blue Nazor application tile through `IPlatformWindow.ApplicationIcon`. The standard Nazor title bar displays it automatically through `IWindow.EffectiveIcon`. Setting `Window.Icon` replaces the image; null restores the default. The asset is embedded in the managed backend assembly and requires no deployment-side image file. This default is for client title-bar content; native decoration and launcher icons remain controlled by the desktop and its icon protocols or desktop entries.

Decoration capabilities describe the active desktop provider. Weston Xwayland supports selecting native or Nazor decorations at startup, but reports `FrameChanges = Unsupported` for an open window. Native frame extents are checked against Weston's actual geometry to distinguish its shadow-only client wrapper. See [X11 behavior](../Guide/35-Linux-X11.md#system-dependencies-and-behavior).

If initial X11 frame extents are absent, Nazor can confirm client ownership from mapped ICCCM state and a complete ancestor chain that contributes no external geometry. Unmapped windows and larger or offset wrappers remain unknown.

On composited X11, an explicit frame resize outset uses transparent padding within the native top-level surface, preserving compositor-owned interactive dragging under Xwayland. Client sizes, CPU/GPU rendering and input coordinates exclude this padding. Noncomposited X11 retains an input-only companion. See [X11 frame behavior](../Guide/35-Linux-X11.md#system-dependencies-and-behavior).

`LinuxPlatformHost()` selects a protocol from the distributed native libraries and session. `LinuxPlatformHost(LinuxDesktopBackend)` accepts Auto, Wayland or X11; invalid enum values are rejected. At startup, `NAZOR_LINUX_BACKEND` overrides constructor/session selection and accepts only the exact values `X11` and `Wayland`; an invalid value or an excluded native bridge throws `InvalidOperationException`. A forced backend never falls back. Without the variable, Auto with `NazorLinuxNativeLibraries=All` retries X11 if Wayland initialization fails; failure of both backends produces `InvalidOperationException` with an inner `AggregateException` containing both causes. No fallback occurs for a single-module publication or an explicit constructor backend. `Run` owns the connection on its calling thread; `RunAsync` owns a dedicated UI thread. Neither host construction nor desktop selection opens native resources.

`WaylandPlatform()` and `X11Platform()` derive from `LinuxDesktopPlatform`, implement `IPlatform`/`IDisposable`, and own their native connection, windows and event loop. All operations except `Quit(int)` belong to the creating thread. The shared adapter preserves dispatcher, painting, clipboard and D-Bus integration behavior. See [Wayland](../Guide/33-Linux-Wayland.md), [X11](../Guide/35-Linux-X11.md) and [LinuxPlatformHost source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxPlatformHost.cs).

`WindowCapabilities.CanShowPopupsOutsideWindow` reports availability of native popup surfaces. Wayland uses XDG popup positioning and compositor configuration. X11 requires an ARGB visual and a compositing manager and uses RandR monitor information for placement. The window setting `IWindow.AllowPopupsOutsideWindow` opts in independently. Native assets use the portable Linux libc and process-architecture RID, including on distribution-built .NET runtimes.

## `DrmPlatformHost` and `DrmPlatform`

`DrmPlatformHost(DrmOptions? options = null)` implements `IPlatformHost` and opens
devices only during `Run`/`RunAsync`. Select it explicitly with `builder.Platform.Host`
and `builder.Application.UseMainView<T>()`. Its asynchronous run owns a dedicated
UI thread. Options are validated and captured at host construction.

`DrmPlatform(DrmOptions? options = null)` implements `ISingleViewPlatform` and
`IDisposable`. It acquires a local libseat session and one connected DRM output.
`CreateView(IWindowHost)` creates exactly one surface with its size derived from
the display and configured scale. `CreateWindow` rejects desktop windows. Native
window capabilities are false and desktop integration is unavailable. The view
has no native window handle; its state is full screen. Its clipboard is local to
the process. Input and painting run on the UI owner; OpenGL work runs on Nazor's
render worker. `Run(UIDispatcher)` drains dispatcher work, input and frames;
`Quit(int)` may wake it from another thread. Disposal must run on the owner thread
and drains graphics before releasing devices and restoring the display/console.
Seat deactivation suspends rendering and input; reactivation invalidates the view.

```csharp
public WindowCapabilities Capabilities { get; } // All desktop operations are false.
public IPlatformWindow CreateView(IWindowHost host);
public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options);
public int Run(UIDispatcher dispatcher);
public void Quit(int code = 0);
public void Dispose();
```

`CreateView` rejects null hosts and a second surface creation, including after the first view closes. `CreateWindow` always throws `PlatformNotSupportedException`. `Run` rejects a null dispatcher, a dispatcher owned by another thread and a nested event loop. The host exposes `int Run(Func<IPlatform, int> application)` and `Task<int> RunAsync(Func<IPlatform, int> application)`; both reject null callbacks and release the platform after the callback completes.

The backend requires 64-bit Linux, libdrm, libseat, libinput, libudev, xkbcommon,
keyboard data and fonts. GPU rendering additionally requires GBM and EGL/GLES.
Missing native dependencies or inaccessible seats/outputs fail explicitly.

[Host source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmPlatformHost.cs) ·
[Platform source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmPlatform.cs)

## `DrmOptions`

Display selection for the DRM/KMS single-view host. `DevicePath` selects a primary
`/dev/dri/cardN` device; null searches connected cards. `ConnectorId` optionally
selects one connected output. The preferred video mode is used. `Scale` is the
physical-pixel to logical-unit ratio and must be finite and between 0.5 and 8.
Invalid values are rejected before native devices are opened. Render selection
continues to use the application's existing `NAZOR_RENDERER` policy.

```csharp
public sealed record DrmOptions
{
    public string? DevicePath { get; init; } // Default: null.
    public uint? ConnectorId { get; init; } // Default: null; zero is invalid.
    public float Scale { get; init; } = 1;
}
```

`DevicePath`, when supplied, must have the form `/dev/dri/card` followed by ASCII digits. Invalid device paths throw `ArgumentException`; zero connector IDs and out-of-range or non-finite scale throw `ArgumentOutOfRangeException`.

[Source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmOptions.cs)

## `LinuxPlatformHost`

Namespace `Nazor.Platform.Linux`.

`public sealed class LinuxPlatformHost : IPlatformHost`

`Run(Func<IPlatform, int> application)` reads `NAZOR_LINUX_BACKEND` and the session, creates the selected `WaylandPlatform` or `X11Platform` on the calling thread, invokes the callback and disposes the platform after it returns. Automatic fallback is limited to native platform initialization; callback, window creation and event-loop errors propagate without retrying the application. The callback must create its dispatcher on that thread. Returns the callback's exit code. Rejects null callbacks and non-Linux operating systems; failed native initialization produces an actionable error.

`RunAsync(Func<IPlatform, int> application)` invokes the same lifecycle on a dedicated thread. The returned task completes after cleanup and preserves callback/initialization errors. Null callbacks are rejected before a thread starts.

[Source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxPlatformHost.cs)

## `LinuxDesktopPlatform`, `WaylandPlatform` and `X11Platform`

```csharp
public abstract unsafe class LinuxDesktopPlatform : IPlatform, IDisposable { /* Shared members below. */ }
public sealed class WaylandPlatform : LinuxDesktopPlatform { public WaylandPlatform(); }
public sealed class X11Platform : LinuxDesktopPlatform { public X11Platform(); }
```

`WaylandPlatform()` opens the connection selected by `WAYLAND_DISPLAY`/`WAYLAND_SOCKET` and `XDG_RUNTIME_DIR`; `X11Platform()` connects through `DISPLAY`. Both check bridge ABI version 5 and require an x64 or ARM64 Linux process plus the native libraries listed in the respective [Wayland](../Guide/33-Linux-Wayland.md) and [X11](../Guide/35-Linux-X11.md) guides. Each instance owns an independent connection. Missing libraries, ABI mismatches and connection failures report initialization errors; unsupported operating systems or process architectures throw `PlatformNotSupportedException`. The base constructor is private protected; applications use the concrete platform constructors or `LinuxPlatformHost`.

The following members are declared by `LinuxDesktopPlatform` and inherited by both concrete desktop platforms:

| Member | Contract |
| --- | --- |
| `Capabilities` | `WindowCapabilities` describing potential native operations. Each window refines effective support from optional protocols, window-manager policy and application policy. |
| `DesktopIntegration` | Session D-Bus tray/notification services and numeric launcher badges, with independently reported support. Runtime Jump List and badge-icon capabilities are false. The platform owns their lifetime. See the [launcher contract](../Guide/15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges). |
| `CreateWindow(IWindowHost host, WindowOptions options)` | Creates an initially hidden window on the creating thread. Rejects null arguments and owners from another connection or already closed owners. The caller owns the adapter. |
| `ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)` | Runs an XDG portal file/folder picker and returns selected local paths, or null on cancellation. Requires a portal service; a supplied owner must belong to this connection. Continues native and dispatcher processing while waiting. |
| `Run(UIDispatcher uiDispatcher)` | Runs native input, dispatcher work and animation presentation on the creating thread. Rejects nested runs and dispatchers owned by another thread. Returns the code passed to `Quit`. |
| `Quit(int code = 0)` | Wakes the event loop and requests its termination. May be called from another thread. |
| `Dispose()` | Disposes remaining windows and desktop services, then closes the native connection on the creating thread. Repeated disposal is harmless. |

All window/service operations except `Quit` must run on the creating thread. Optional or restricted operations follow the capability table in the guide; global position and Windows decorations are not emulated as successful native operations.

[Shared platform source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxDesktopPlatform.cs) · [Wayland source](../../Sources/Platforms/Nazor.Platform.Linux/WaylandPlatform.cs) · [X11 source](../../Sources/Platforms/Nazor.Platform.Linux/X11Platform.cs)

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `DrmOptions`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrmOptions : IEquatable<DrmOptions>` | Display selection for a Linux DRM/KMS single-view application. Captured by the platform host. |
| `public override string ToString()` |  |
| `public static bool operator !=(DrmOptions? left, DrmOptions? right)` |  |
| `public static bool operator ==(DrmOptions? left, DrmOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(DrmOptions? other)` |  |
| `public DrmOptions()` |  |
| `public string? DevicePath { get; init; }` | The primary DRM device path, such as /dev/dri/card0; null searches connected cards. |
| `public uint? ConnectorId { get; init; }` | The connector identifier on the selected card; null selects its first connected output. |
| `public float Scale { get; init; }` | The number of physical pixels per logical unit. Must be finite and between 0.5 and 8. |

### `DrmPlatform`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmPlatform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DrmPlatform : ISingleViewPlatform, IPlatform, IDisposable` | One DRM/KMS display surface, libinput devices and a local seat. All operations except Quit own the creating UI thread. |
| `public DrmPlatform(DrmOptions? options = null)` | Acquires the configured display and active seat. Does not create application content.<br>`options`. Display selection and logical scale; null uses the first connected output at scale one.<br>**Throws** `System.PlatformNotSupportedException`. The system is not 64-bit Linux.<br>**Throws** `System.InvalidOperationException`. Native libraries, the seat or a connected output are unavailable. |
| `public IPlatformWindow CreateView(IWindowHost host)` | Creates the application's single surface. Its dimensions come from the selected display mode.<br>`host`. UI-thread rendering, input and lifecycle callbacks.<br>**Returns**. The display surface, owned by the application.<br>**Throws** `System.InvalidOperationException`. A surface has already been created. |
| `public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)` | Rejects desktop window creation. Configure the application with UseMainView instead.<br>`host`. The requested window callbacks.<br>`options`. The requested desktop window configuration.<br>**Returns**. This method always throws.<br>**Throws** `System.PlatformNotSupportedException`. DRM has no desktop windows. |
| `public int Run(UIDispatcher dispatcher)` | Runs dispatcher work, input and display frames on the creating thread until Quit is called.<br>`dispatcher`. The UI dispatcher owned by the calling thread.<br>**Returns**. The exit code supplied to Quit. |
| `public void Quit(int code = 0)` | Requests loop termination and wakes a blocked dispatcher. May be called from another thread.<br>`code`. The application exit code. |
| `public void Dispose()` | Drains rendering before releasing input, display, seat and console resources on the UI thread. |
| `public string BackendName { get; }` |  |
| `public WindowCapabilities Capabilities { get; }` | DRM has no desktop window operations. Its surface already fills the configured display. |

### `DrmPlatformHost`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/Drm/DrmPlatformHost.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DrmPlatformHost : IPlatformHost` | Owns a DRM/KMS single-view backend without a desktop or compositor. Select explicitly through Platform.Host. |
| `public DrmPlatformHost(DrmOptions? options = null)` | Captures display options. Native devices are opened only when Run or RunAsync starts.<br>`options`. Optional primary card, connector and scale selection.<br>**Throws** `System.ArgumentException`. The selected card path or scale is invalid. |
| `public int Run(Func<IPlatform, int> application)` | Acquires the local seat and display on the calling thread, invokes the application and restores its resources.<br>`application`. The application callback, which must select UseMainView and run on this thread.<br>**Returns**. The application exit code.<br>**Throws** `System.ArgumentNullException`. The callback is null.<br>**Throws** `System.PlatformNotSupportedException`. The system is not 64-bit Linux.<br>**Throws** `System.InvalidOperationException`. The active seat, native libraries or connected output are unavailable. |
| `public Task<int> RunAsync(Func<IPlatform, int> application)` | Runs the application on a dedicated UI thread and completes after device and console restoration.<br>`application`. The application callback invoked on the new UI thread.<br>**Returns**. The application exit code after cleanup. |

### `LinuxDesktopBackend`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxDesktopBackend.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LinuxDesktopBackend` | Selects a Linux desktop display protocol. DRM/KMS uses its separate explicit platform host. |
| `Auto = 0` | Selects a protocol from NAZOR_LINUX_BACKEND, the published libraries and session; falls back from Wayland to X11 only when both are published and no environment override is set. |
| `Wayland = 1` | Connects to a Wayland compositor. |
| `X11 = 2` | Connects to an X11 display server. |

### `LinuxDesktopPlatform`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxDesktopPlatform.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class LinuxDesktopPlatform : IPlatform, IDisposable` | Shared native Linux desktop backend. Owns a compositor connection, native windows and the UI event loop.<br>All operations except Quit must run on the creating thread. The system must provide the selected display protocol. The compositor or window manager determines effective window capabilities. |
| `public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)` | Creates an initially hidden Linux desktop window on the UI thread.<br>`host`. The Nazor lifecycle, input and painting receiver.<br>`options`. Initial title, size, owner and operation policy.<br>**Returns**. The native window adapter owned by the caller. |
| `public int Run(UIDispatcher uiDispatcher)` | Runs native events, dispatcher work and animation frames on the creating thread.<br>`uiDispatcher`. The dispatcher created on the same thread.<br>**Returns**. The exit code passed to Quit. |
| `public void Quit(int code = 0)` | Wakes and stops the native event loop. May be called from another thread.<br>`code`. The exit code returned by Run. |
| `public IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)` | Shows a file or directory picker through XDG Desktop Portal.<br>`owner`. The optional owner on this Linux desktop connection.<br>`request`. Picker kind, filters and initial path.<br>**Returns**. The chosen local paths, or null when canceled.<br>**Throws** `System.NotSupportedException`. The session has no file chooser portal. |
| `public void Dispose()` | Closes remaining windows and releases the native connection on the UI thread. |
| `public string BackendName { get; }` |  |
| `public WindowCapabilities Capabilities { get; }` | Gets the potential window operations. Individual windows refine optional protocol support. |
| `public IPlatformDesktopIntegration DesktopIntegration { get; }` | Gets session D-Bus services for tray icons, notifications and desktop preferences. |

### `LinuxPlatformHost`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/LinuxPlatformHost.cs)

| Declaration | Contract |
| --- | --- |
| `public LinuxPlatformHost()` | Selects a display protocol from NAZOR_LINUX_BACKEND, the publication and current desktop session. |
| `public LinuxPlatformHost(LinuxDesktopBackend backend)` | Captures an explicit display protocol or selects it from the publication and desktop session.<br>`backend`. The display protocol, overridden by NAZOR_LINUX_BACKEND; Auto selects from the publication and session, with X11 fallback when both libraries are published. |
| `public int Run(Func<IPlatform, int> application)` | Creates the selected desktop connection on the calling thread, invokes the application and releases the backend.<br>`application`. The callback that creates its dispatcher and runs the application on this thread.<br>**Returns**. The exit code returned by the application.<br>**Throws** `System.ArgumentNullException`. The callback is null.<br>**Throws** `System.PlatformNotSupportedException`. The operating system is not Linux.<br>**Throws** `System.InvalidOperationException`. No desktop libraries are included or the native connection cannot be initialized. |
| `public Task<int> RunAsync(Func<IPlatform, int> application)` | Runs the application on a dedicated UI thread and completes after backend cleanup.<br>`application`. The application callback, invoked on the new UI thread.<br>**Returns**. A task containing the exit code, or the initialization or application error.<br>**Throws** `System.ArgumentNullException`. The callback is null. |

### `WaylandPlatform`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/WaylandPlatform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class WaylandPlatform : LinuxDesktopPlatform` | Owns a Wayland connection and the native desktop event loop. |
| `public WaylandPlatform()` | Connects to the compositor selected by WAYLAND_DISPLAY and XDG_RUNTIME_DIR. |

### `X11Platform`

Namespace `Nazor.Platform.Linux`. [Source](../../Sources/Platforms/Nazor.Platform.Linux/X11Platform.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class X11Platform : LinuxDesktopPlatform` | Owns an X11 display connection, native windows and the desktop event loop. |
| `public X11Platform()` | Connects to the X server selected by DISPLAY. |
