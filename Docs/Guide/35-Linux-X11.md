# 35. Linux X11

Nazor connects directly to X11 through `libnazor-x11.so`, without GTK or Qt. `X11Platform` and `WaylandPlatform` share `LinuxDesktopPlatform`, the managed window adapter, Skia rendering, dispatcher, D-Bus desktop services and accessibility. Controls, Razor components and NSS styles use the same rendering path on both protocols.

## Select and publish

```powershell
dotnet publish MyApp.csproj -c Release -r linux-x64 --self-contained true -p:NazorLinuxNativeLibraries=X11 -o artifacts/publish/linux-x64-x11
```

`NazorLinuxNativeLibraries` selects which native Linux desktop bridges to distribute. It accepts `All` (default), `Wayland`, `X11` and `None`. DRM is not a library-selection value.

| Value | Native Linux payload |
| --- | --- |
| `All` | `libnazor-wayland.so` and `libnazor-x11.so` |
| `Wayland` | `libnazor-wayland.so` |
| `X11` | `libnazor-x11.so` |
| `None` | Neither bridge |

Linux RID publications use the module for that exact architecture/libc. Portable publications apply the same selection to every supported Linux RID and retain `runtimes/<rid>/native/`. Windows-only and other non-Linux RID publications ignore the property; their existing platform support rules remain unchanged. Unknown values, including DRM, fail with NZB011 when the selection applies. Renderer dependencies and the shared managed Linux platform remain available with `None`.

The generated bootstrap records which libraries are included. Without an environment override or explicit backend, `LinuxPlatformHost` selects from that set: a single distributed module selects its protocol; `All` follows the desktop session. `XDG_SESSION_TYPE=x11` with `DISPLAY` selects X11; otherwise Wayland variables (`WAYLAND_DISPLAY` or `WAYLAND_SOCKET`) take precedence over `DISPLAY`. If only `DISPLAY` exists, X11 is selected; if none of these display variables exists, Wayland is attempted. With `None`, automatic Linux desktop startup reports that an explicit host is required; it never selects DRM automatically. Configure `DrmPlatformHost`/`UseMainView` or a custom application host explicitly.

With `LinuxDesktopBackend.Auto`, `NazorLinuxNativeLibraries=All` and no `NAZOR_LINUX_BACKEND` override, a Wayland initialization failure automatically attempts X11. This includes missing/unloadable native libraries, missing bridge exports, ABI mismatches and compositor connection failures. X11 still requires its native dependencies and a reachable `DISPLAY`. If X11 also fails, the exception retains both startup errors. Fallback applies only while creating the native connection: application callbacks, window creation and later event-loop failures do not restart the application. There is no X11-to-Wayland fallback. A single-module publication or an explicit backend never falls back.

Set `NAZOR_LINUX_BACKEND` to force the protocol at runtime:

```bash
NAZOR_LINUX_BACKEND=X11 ./MyApp
NAZOR_LINUX_BACKEND=Wayland ./MyApp
```

The only accepted values are exactly `X11` and `Wayland` (case-sensitive). An unset variable leaves automatic/programmatic selection in control; an empty or other value fails with an actionable error. The variable takes precedence over the `LinuxPlatformHost` constructor and session detection, disables fallback and fails if the requested bridge was excluded by `NazorLinuxNativeLibraries` or cannot initialize. This variable controls the Linux desktop protocol; `NAZOR_RENDERER` continues to control CPU/GPU rendering independently. Custom hosts such as `DrmPlatformHost` are unaffected.

Without the environment override, an explicit desktop host can also be configured:

```csharp
builder.Platform.Host = new LinuxPlatformHost(LinuxDesktopBackend.X11);
```

RID publications select the exact glibc/musl and architecture module beside the executable. Portable outputs retain separate `runtimes/<rid>/native/` directories. Both modules are packaged for `linux-x64`, `linux-arm64`, `linux-musl-x64` and `linux-musl-arm64`; NZB004 rejects a missing exact-RID module. Directory, standard .NET single-file, trimming and Native AOT use the same native selection. Use a fresh publish directory when changing it. Single-file extraction defaults remain those of .NET.

## System dependencies and behavior

Decoration ownership is confirmed from the desktop's frame extents or, when those are absent, from a mapped ICCCM-managed window whose complete wrapper chain adds no frame geometry. Missing metadata by itself does not confirm either owner. This supports client-decorated startup on window managers that omit initial zero extents.

The target requires an X server reachable through `DISPLAY`, `libX11.so.6`, `libXcursor.so.1`, `libXrender.so.1`, `libXext.so.6`, and `libXrandr.so.2`, plus the renderer's Skia and HarfBuzz assets and usable fonts. GPU rendering additionally requires `libEGL.so.1`, OpenGL ES and a compatible driver. EGL matches the X11 window's native visual. `AUTO` can fall back to CPU and `GPU` requires a hardware context. The X11 and Wayland bridges link their own display libraries independently. [Backend support matrix](32-Support-and-Limitations.md#backend-support-matrix)

ICCCM window ownership and focus, EWMH maximize, minimize and fullscreen, runtime frame switching, icons, pointer capture, cursors, keyboard layouts and server autorepeat are supported. The window manager determines effective state and capabilities. Weston Xwayland supports selecting native or Nazor ownership at startup, but reports `FrameChanges = Unsupported` on open windows. Its shadow-only wrappers are distinguished from native decorations through observed geometry; an ownership request alone does not confirm support. CPU presentation retains pixels and updates damaged rectangles. Expose events request a repaint. Clipboard supports UTF-8 and `STRING` conversion, `TARGETS`, `TIMESTAMP` and incremental transfers, with a 32 MiB text limit. Clipboard ownership lasts until another owner replaces it or the application exits. [Frame switching](14-Title-Bars-and-Platform-Integration.md#switch-the-frame-while-open)

XIM uses input-method-managed preedit/status and UTF-8 commits. Inline Nazor preedit, surrounding-text updates and IME caret placement are not implemented for X11. Scale is read from `Xft.dpi` when connecting; live per-monitor scaling is not implemented. CPU and GPU rendering use the actual client drawable size in pixels, including resizes that leave the rounded logical size unchanged. Default presentation uses the display's default visual. `BackgroundMode.Transparent` uses a 32-bit ARGB visual when a compositing manager owns the screen's `_NET_WM_CM_Sn` selection. Compositor blur, absolute placement, taskbar visibility and explicit size limits are not advertised.

`IsAlwaysOnTop` uses `_NET_WM_STATE_ABOVE` when the window manager advertises it. Changes from the desktop window menu are reported back to Nazor. Right-clicking a draggable part of a Nazor title bar requests the desktop window menu on window managers that advertise `_GTK_SHOW_WINDOW_MENU`.

The [window frame and resize properties](19-NSS-Property-Reference.md#window-frame-and-resize) apply to the supported parts of Nazor-drawn frames. System decorations and their resize zones follow the window manager. With an ARGB visual and a compositing manager, the exterior resize band is transparent padding in the window's own native surface. This also supports interactive dragging under Xwayland: the initiating press and the resized window belong to the same compositor surface. Client layout, rendering dimensions and input coordinates exclude the padding. Without compositing, X11 uses an X Shape input ring in a separate input-only window. The exterior shadow requires an ARGB visual and a compositing manager. The portable resize grip starts a resize through the window manager when it supports interactive resize requests. Window placement remains subject to window-manager policy when the resize band changes.

With a compositing manager and an ARGB visual, `Window.AllowPopupsOutsideWindow` enables independent popup windows with NSS styling. RandR 1.5 supplies the anchor's monitor bounds, combined with the desktop work area. Older servers use the available desktop bounds. Without compositing, popups use the in-window overlay. [Popup surfaces](11-Commands-Menus-and-Popups.md#popup-surfaces-outside-the-window)

Wayland and X11 use the same session D-Bus integration for portal file pickers, tray icons and menus, notifications, launcher count badges and desktop preferences. X11 supplies an `x11:` owner handle to the FileChooser portal. AT-SPI exposes the shared semantic tree when the accessibility bus is available. The display protocol and the availability of each desktop service are independent. [Shared desktop-service capabilities](32-Support-and-Limitations.md#backend-support-matrix) · [Tray, launcher and notifications](15-Tray-and-System-Notifications.md)

## Build and verify

Build all eight native assets with `pwsh Scripts/Build/build-native-matrix.ps1` or `bash Scripts/Build/build-native-matrix.sh`. A host-only build accepts `bash Scripts/Build/build-native.sh Release linux-x64 X11`; CMake `NAZOR_WAYLAND=OFF` builds X11 without Wayland headers, and `NAZOR_X11=OFF` builds Wayland without X11 headers. On Linux, use `NazorSkipNativeBuild=true` to preserve the prebuilt container baseline during managed publication or packing.

`verify-linux-backend-publish.ps1` covers source/NuGet module selection, profiles, single-file, invalid values and exact RIDs. `verify-linux-desktop.sh` runs managed X11 or Wayland tests, including D-Bus regressions. `verify-x11-client.ps1` runs published .NET probes or native C probes against an isolated Xvfb/Openbox server over a shared Unix socket. Build its four runtime images from the repository root using `Scripts/Tests/X11Runtime.Dockerfile`, `LIBC=glibc|musl`, matching Docker platforms, and tags `nazor-x11-test:glibc`, `:glibc-arm64`, `:musl`, `:musl-arm64`.

Native build containers run ABI and input tests. GUI client tests run separately from BuildKit and cover fractional-DPI drawable sizes, clipboard ownership changes and the placement of nested popups with shadows. The managed EGL tests cover both the default visual and the transparent visual when a compositing manager is available. ARM64 clients can run in emulation against the x64 server, with the window manager running on x64. Test physical ARM64 boards, vendor GPU drivers and external input-method servers on the deployment image and device. [Verification boundaries](32-Support-and-Limitations.md#verification-boundaries)

Protocol references: [ICCCM](https://www.x.org/releases/X11R7.7/doc/xorg-docs/icccm/icccm.pdf), [EWMH](https://specifications.freedesktop.org/wm/latest-single/), [Xlib](https://www.x.org/releases/X11R7.6/doc/libX11/specs/libX11/libX11.pdf).
