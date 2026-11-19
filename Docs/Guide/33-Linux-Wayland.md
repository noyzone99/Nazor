# 33. Linux Wayland

The Linux backend connects directly to Wayland. It uses a small C bridge for generated protocol bindings, native object lifetimes, input and shared-memory buffers. The C# backend implements Nazor window contracts, rendering integration, dispatcher scheduling and desktop services. Neither layer uses GTK or Qt. Controls and themes continue to be drawn by Nazor.

`DesktopPlatformHost` selects `LinuxPlatformHost` automatically on Linux; that host selects Wayland or X11 from the publication and desktop session. In Auto mode with both libraries published, a Wayland initialization failure attempts X11. Set `NAZOR_LINUX_BACKEND=X11` or `NAZOR_LINUX_BACKEND=Wayland` to force a backend without fallback; these are the only accepted values (case-sensitive), and a missing/excluded or unavailable backend fails explicitly. The variable overrides constructor selection and session detection. See [runtime selection and fallback](35-Linux-X11.md#select-and-publish). An application targeting `net8.0` or `net10.0` keeps its neutral startup and components. X11-only sessions use the separate [X11 backend](35-Linux-X11.md).

## Runtime dependencies

Use the [backend support matrix](32-Support-and-Limitations.md#backend-support-matrix) for runtime libraries, fonts, graphics drivers and verification boundaries. `NazorLinuxNativeLibraries` defaults to `All`. Set it to `Wayland` to publish a Wayland-only application and use a fresh output directory for the selected native payload. Both desktop backends share `Nazor.Platform.Linux.dll`. The publish setting selects native libraries and leaves their managed code in place. [Publication settings](35-Linux-X11.md#select-and-publish)

The native library is `libnazor-wayland.so`, with separate assets for `linux-x64`, `linux-arm64`, `linux-musl-x64` and `linux-musl-arm64`. The target system needs `libwayland-client.so.0` (1.20 or newer), `libwayland-cursor.so.0`, `libxkbcommon.so.0`, fontconfig and usable fonts. GPU rendering additionally requires `libEGL.so.1`, `libwayland-egl.so.1` and a working hardware OpenGL ES driver; AUTO falls back to CPU when that context cannot initialize. Software OpenGL devices such as llvmpipe do not satisfy GPU mode. See [renderer selection](32-Support-and-Limitations.md#nss-and-rendering). Skia and HarfBuzz Linux native assets are delivered through the renderer's NuGet dependencies. DRM uses direct native calls and does not load this bridge.

Portable source and NuGet publications retain the selected native libraries in `runtimes/<rid>/native/`, with no bridge in the root. An explicit Linux RID publication uses the standard .NET flat native layout beside the executable. `linux-x64` and `linux-arm64` use glibc; `linux-musl-x64` and `linux-musl-arm64` use musl. NZB004 rejects a missing exact-RID library, including a glibc fallback for musl. NuGet packing requires both bridges for all four RIDs under `artifacts/native/<rid>/`. The native MSBuild target builds for its host SDK RID and rejects relabeling through `NazorNativeRid`; use `NazorSkipNativeBuild=true` to consume prebuilt assets from another host.

Build both libraries for all four RIDs with `pwsh Scripts/Build/build-native-matrix.ps1` on Windows or `bash Scripts/Build/build-native-matrix.sh` on Linux/WSL with Docker available. Both use Docker Buildx, Ubuntu 22.04 for glibc and Alpine 3.21 for musl. The image/package baseline is part of binary compatibility; the ELF version requirements recorded in `artifacts/native/<rid>/native-build.txt` must be met by the deployment system. Older BSPs may require rebuilding with a matching sysroot and matching Skia/HarfBuzz assets. ARM64 builds and C tests can run through QEMU when the builder supports it; this does not certify .NET or GPU behavior on ARM64 hardware. Build metadata, native test results and binary hashes accompany each library. A host-only build uses `bash Scripts/Build/build-native.sh [Debug|Release] [expected-rid]` and an isolated cache per RID. ARM32 and other architectures are outside this build matrix.

When packing on Linux after a container matrix build, pass `-p:NazorSkipNativeBuild=true` to retain the container-built baseline instead of rebuilding the host asset with the local system toolchain. Runtime requirements include the library's actual ELF symbol versions and its system dependencies; a RID alone does not certify every Linux distribution or embedded BSP.

The software path retains its CPU framebuffer, copies only clipped damaged rows when the acquired compositor buffer contains the preceding frame, and fully initializes other buffers. Wayland receives explicit physical damage including client decorations. GPU frames render into EGL surfaces without copying the normal framebuffer through shared memory. WSLg driver selection is an environment concern: a software EGL renderer can coexist with a usable D3D12 driver selected explicitly by the environment.

`WAYLAND_DISPLAY` and `XDG_RUNTIME_DIR` must identify an accessible compositor. The compositor must expose `wl_compositor` version 4 or newer, `wl_shm` and `xdg_wm_base`. Input requires a seat with keyboard/pointer support. A session D-Bus connection provides the desktop integrations described below. The application does not install or embed those session services.

Install at least one font family in addition to fontconfig, or register private
font files with the application. See [Linux font discovery](21-Typography-and-Fonts.md#linux-font-discovery)
for the `No font is available.` startup error.

## Feature mapping

| Nazor feature | Wayland/Linux implementation and conditions |
| --- | --- |
| Windows, owned windows, resize, hide/show and shutdown | `xdg-shell`; a parent is assigned after its first buffer is committed. Application-modal input disabling is handled by Nazor. |
| Maximize, minimize and fullscreen | `xdg_toplevel` requests, refined by compositor-advertised capabilities. Wayland does not report the minimized state or permit arbitrary forced activation. |
| Standard and custom title bars | Complete native decorations require compositor acceptance. The default explicitly prefers native decorations, then Nazor client decorations. `SetDecorationsAsync` switches the existing surface; `Require` rejects unsupported combinations and `Prefer` tries its supplied alternatives, then complete native and Nazor defaults. Both desktop bridges use ABI 5. |
| Placement persistence | Restores portable client size and maximized state. Absolute screen position, selecting a monitor and centering cannot be promised through `xdg-shell`; the corresponding placement capabilities are false. |
| Rendering and animations | CPU uses Skia raster and copies damaged pixels to available `wl_shm` buffers. Busy compositor buffers remain intact. GPU uses hardware OpenGL ES through EGL, with Skia recordings submitted to the graphics worker. The Wayland surface receives frame callbacks, and eligible transform and opacity animations can run on the GPU worker. [Renderer selection](32-Support-and-Limitations.md#nss-and-rendering) |
| DPI | Preferred surface scale, output scale fallback, and fractional scale with `viewporter` when offered. Mixed-monitor behavior requires interactive validation. |
| Pointer, keyboard, scrolling and cursors | Wayland seat events, xkbcommon keymaps/compose, key repeat, logical-pixel scrolling and system cursor themes. Wayland supplies an implicit pointer grab while buttons are pressed; arbitrary global capture is not offered. |
| Clipboard | UTF-8 `wl_data_device` offers and nonblocking pipe transfers. Publishing selection requires an input serial; setting text before user input can return false. Clipboard reads have a bounded wait. |
| IME | `text-input-v3` preedit, commit, surrounding text and deletion; compose sequences remain available without that optional protocol. Passwords are marked sensitive and their contents are not sent as surrounding text. |
| Per-window icons | `xdg-toplevel-icon-v1` when advertised. Without it `CanSetWindowIcon` is false; the desktop can associate the app ID with a separately installed desktop entry. |
| Backdrops | Blur through `ext-background-effect-v1` when the compositor advertises it. Unsupported materials return an inactive result and preserve the framework's opaque fallback. Windows-specific NSS decorations remain Windows-only. |
| File and directory dialogs | XDG Desktop Portal FileChooser, including filters, multiple selection, save names and initial directory. `xdg-foreign-v2` supplies the owner handle when available. Only local file paths are returned. |
| Tray and menu | StatusNotifierItem and DBusMenu. Requires a StatusNotifierWatcher and a host supplied by the desktop or an extension; desktops without a tray host report it unavailable. Each UseTrayItem registration exports its own item and typed menu, with commands, separators, submenus, icons, checkboxes and radio groups. Watcher restarts trigger registration again independently for each visible item. MenuOpening is reported through DBusMenu AboutToShow when the host supplies it; cancellation is unavailable. |
| Notifications | `org.freedesktop.Notifications`, including default activation and withdrawal. Delivery remains subject to shell policy. |
| Application launcher | Numeric badges use `com.canonical.Unity.LauncherEntry.Update` when a compatible host is detected. Runtime Jump List and icon badge capabilities are false; configure launcher actions in the installed `.desktop` file. See the [launcher contract and desktop-entry example](15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges). |
| Dark mode, contrast and motion | XDG portal settings and change signals. KDE/GNOME palette profiles are selected from `XDG_CURRENT_DESKTOP`; unknown desktops use the generic profile. |
| Accessibility | An AT-SPI provider over the accessibility bus, derived from the shared semantic tree: accessible objects/cache, state changes, actions, focus, selection, ranges, editable text, text bounds and announcements. Password text is not exposed. Global screen coordinates cannot be supplied by this Wayland backend. |

This table describes implemented mappings, not certified Windows feature parity on every compositor. Optional protocols and desktop services are negotiated independently. Headless protocol tests do not certify real IMEs, screen readers, compositor materials or desktop-shell behavior.

## Build from source

Install the .NET SDK pinned in `global.json`, a C11 compiler, CMake, pkg-config, the Wayland development libraries and scanner, and xkbcommon development files. The default `All` build also requires X11, Xcursor, Xrender and Xext development files (`pkg-config x11 xcursor xrender xext` must succeed). `Native/Protocols` contains the protocol XML and retained upstream license notices, so building does not fetch protocol definitions from the network.

On Fedora, the Wayland development packages include `gcc cmake wayland-devel libxkbcommon-devel fontconfig`. Add pkg-config, `libX11-devel`, `libXcursor-devel`, `libXrender-devel`, and `libXext-devel` for `All`. On Debian/Ubuntu, the default build uses `build-essential cmake pkg-config libwayland-dev libwayland-bin libxkbcommon-dev libfontconfig1 libx11-dev libxcursor-dev libxrender-dev libxext-dev`.

```bash
bash Scripts/Build/build-native.sh Release
dotnet build Nazor.slnx -c Release
dotnet run --project Samples/Nazor.Samples.Counter
```

Linux project builds compile the selected bridges automatically. A Wayland-only build can use `bash Scripts/Build/build-native.sh Release linux-x64 Wayland` and `-p:NazorLinuxNativeLibraries=Wayland` on the application build/publish, avoiding X11 headers and assets. Replace the RID for another host architecture/libc; the helper rejects a RID that does not match its compiler. Ordinary Windows execution does not load a Linux bridge.

For Windows-to-Linux publication, build each selected exact-RID asset in Linux/WSL first. Portable publication requires the selected modules for all four RIDs. Packing `Nazor.Platform.Linux` always requires both modules for `linux-x64`, `linux-arm64`, `linux-musl-x64` and `linux-musl-arm64`; build this eight-asset matrix with `pwsh -NoProfile -File Scripts/Build/build-native-matrix.ps1` or `bash Scripts/Build/build-native-matrix.sh` before packing. See the [native build script reference](../../Scripts/README.md#build-and-packaging).

The packaged Razor compiler is invoked with ReadyToRun disabled on non-Windows systems, allowing its managed IL to run when the SDK package was produced on Windows. This setting is limited to the compiler process.

The checked-in [native container build](../../Scripts/Build/Native.Dockerfile) uses Ubuntu 22.04 for glibc and Alpine 3.21 for musl, on x64 and ARM64. Buildx writes native test/build evidence with each asset under `artifacts/native/<rid>`. Use `NazorSkipNativeBuild=true` when publishing or packing those prebuilt assets on Linux so an automatic host build does not replace their baseline. An ad hoc build on a newer distribution can require a newer glibc; native target-machine acceptance remains separate from container/emulation checks.

## Publish from Windows

Build both native bridges in an x64 glibc WSL distribution with the development dependencies above, or use the native matrix, then publish the application from PowerShell at the repository root:

```powershell
wsl -- bash Scripts/Build/build-native.sh Release
dotnet publish Samples/Nazor.Samples.Counter -c Release -r linux-x64 --self-contained true -o artifacts/publish/linux-x64
```

The compiler used during this build runs on Windows. Runtime identifier and self-contained application settings are removed when building and locating that compiler, so publishing a Linux application does not turn the compiler into a Linux tool.

Copy the entire publication directory to the VM. Run `chmod u+x Nazor.Samples.Counter` followed by `./Nazor.Samples.Counter` from a terminal inside the Wayland session. The self-contained publication includes .NET; Wayland, xkbcommon, fontconfig and fonts are still system dependencies. Copying ordinary Windows `bin` output instead leaves a Windows apphost and an unspecialized native-runtime layout.

Run `pwsh -NoProfile -File Scripts/Tests/verify-linux-publish.ps1` on Windows to exercise this publication path and verify the Linux apphost, bridge, rendering libraries, bundled runtime and dependency manifest. It writes a fresh directory under `artifacts/linux-publish` and does not start a GUI.

## File access after copying to Linux

A native-library load failure containing `Permission denied` can mean that an intermediate directory cannot be searched, even when the application's top-level files are readable. Inspect the full path with `namei -l runtimes/linux-x64/native/libSkiaSharp.so`, or use `namei -l libSkiaSharp.so` for the flat publication layout. Every directory needs search permission and the library needs read permission for the application user. A directory copied as `root:root` with mode `750` blocks other users. Correct the ownership and permissions of the copied application directory; making only its top level executable does not repair nested directories. Launch the application as the desktop user.

## Verification without a GUI

Install Weston and `dbus-run-session` in addition to the build prerequisites, then run:

```bash
bash Scripts/Tests/verify-wayland.sh Debug
```

This builds the solution into separate artifacts, runs the Core, Style, Layout, Compiler and Tooling suites, runs Counter headlessly, starts a private Weston headless compositor and private session bus, and runs native and managed integration tests. The managed services are controlled test implementations of the desktop D-Bus contracts. The script checks that the bridge does not link GTK or Qt. It does not attach to an existing graphical session. Set `NAZOR_LINUX_ARTIFACTS` and `NAZOR_NATIVE_BUILD` to put build output on the Linux filesystem when the repository is on a mounted Windows drive.

Before relying on the backend in a VM, exercise Counter, Gallery, Localization, Theming, Windowing, Tasks and Tray. Check keyboard layouts/AltGr/dead keys, real IME composition and passwords, clipboard transfer with another application, drag selection outside a window, mixed DPI, fullscreen restoration, owner-modal dialogs, portal pickers, tray-host restart, notification activation and Orca. Repeat on the GNOME/KDE compositors intended for support. Packaging must include a suitable desktop entry for app-ID association.

---

[Guide index](README.md) · [Support and limitations](32-Support-and-Limitations.md)

## Borderless presentation

When Nazor draws the frame of a normal, untiled Wayland window, the [window frame properties](19-NSS-Property-Reference.md#window-frame-and-resize) style its outline, shadow, corners, and resize zones. Use `:root:window-active` or `:root:window-inactive` to vary the appearance. Resize hit testing and shadow rendering are independent. Server decorations retain the compositor's appearance and edge handling.

Request `WindowDecorationPolicy.Require(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None))` for a client frame without a caption. Set `Window.IsResizable = false` separately for a fixed canvas. Client frames can use NSS borders, radius and shadow. Drag and resize behavior follow the explicit content and operation policy. The desktop bridges must match the managed backend. See the [shared ABI contract](../../Sources/Platforms/Nazor.Platform.Linux/Native/README.md).

Window commands and caption ownership are separate. The compositor advertises whether maximize, minimize and fullscreen can be requested. Nazor applies `Operations` to its own buttons and commands. `TitleBarMenuMode.Nazor` also applies those rules to right-clicks on a caption handled by Nazor. A compositor-owned title bar, desktop menu or shortcut keeps its own rules. The backend reports `CanObserveMinimized` and `CanRestoreFromMinimized` as false.

The standard xdg-shell backend cannot request global always-on-top or explicit per-window taskbar exclusion. It reports `CanSetAlwaysOnTop` and `CanSetTaskbarVisibility` as false and rejects unsupported requests. An owner/parent relationship remains distinct from either setting.
