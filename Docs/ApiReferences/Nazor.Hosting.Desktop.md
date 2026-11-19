# Nazor.Hosting.Desktop

Desktop hosting for Windows and Linux Wayland/X11. Portable publications include both backends; RID-specific publications include only the matching backend. The SDK selects `Nazor.Hosting.Desktop`, `Nazor.Hosting.Desktop.Windows`, or `Nazor.Hosting.Desktop.Linux`; all provide the same assembly and public API and must not be referenced together.

## `DesktopPlatformHost`

Namespace: `Nazor`

```csharp
public sealed class DesktopPlatformHost : IPlatformHost
```

Selects the native desktop backend at runtime and delegates UI-thread startup to that backend. Creating the host does not initialize native services. SDK desktop executables register a factory for this host automatically; applications can replace it with an explicit `Platform.Host`.

[Source](../../Sources/Hosts/Nazor.Hosting.Desktop/DesktopPlatformHost.cs)

### `Run`

```csharp
public int Run(Func<IPlatform, int> application)
```

Runs the application using the included backend for the current operating system. Windows selects `WindowsPlatformHost`, which owns backend lifetime and STA-thread startup. Linux selects `LinuxPlatformHost` for a direct Wayland or X11 connection, with automatic Wayland-to-X11 startup fallback when both bridges are published. `NAZOR_LINUX_BACKEND=X11|Wayland` forces the Linux desktop protocol without fallback. See [Linux desktop hosting](Nazor.Platform.Linux.md#desktop-hosting). Unsupported or excluded systems fail before native initialization. The publication RID determines which backends are included; the target framework does not.

| Parameter | Description |
| --- | --- |
| `application` | Callback invoked with the platform created by the selected backend. |

Returns: The exit code returned by the application callback.

Throws `ArgumentNullException` if `application` is null, or `PlatformNotSupportedException` when no desktop backend exists for the current OS.

### `RunAsync`

```csharp
public Task<int> RunAsync(Func<IPlatform, int> application)
```

Selects the same backend as `Run` and delegates asynchronous UI-thread startup to it. On Windows, the callback runs on a dedicated STA thread; Linux uses a dedicated UI thread for the selected Wayland or X11 connection. The task contains its exit code and completes after backend cleanup. Callback errors propagate through the task. Null callbacks and unsupported operating systems are rejected as for `Run`.

Mobile hosts and lifecycles are separate and are not supplied by this package. See [target frameworks and native hosts](../Guide/03-Projects-and-Builds.md#target-frameworks-and-native-hosts).

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `DesktopPlatformHost`

Namespace `Nazor`. [Source](../../Sources/Hosts/Nazor.Hosting.Desktop/DesktopPlatformHost.cs)

| Declaration | Contract |
| --- | --- |
| `public DesktopPlatformHost()` |  |
