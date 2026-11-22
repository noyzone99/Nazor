# 34. Linux DRM/KMS

Nazor can own one Linux display directly, without X11, Wayland, GTK or Qt. Select
`DrmPlatformHost` from `Nazor.Platform.Linux` explicitly. The ordinary desktop
host selects Wayland or X11 from the available libraries and session; missing Wayland variables never cause
automatic display takeover.

Publish with a Linux RID and `-p:NazorLinuxNativeLibraries=None` to omit both desktop bridges and their build requirements. The default `All` includes both Wayland and X11 bridges. Portable publications apply the same native selection. The property does not replace the explicit application host selection below.

## Application startup

The packaged `Nazor.Sdk/0.1.0-alpha` supplies the Linux platform assembly in
portable and Linux publications. An application can replace the default host:

```csharp
using Nazor;
using Nazor.Platform.Linux;

var builder = NazorApplication.CreateBuilder(args);
builder.Platform.Host = new DrmPlatformHost(new DrmOptions
{
    Scale = 1.5f
});
builder.Application.UseMainView<App>();
using var app = builder.Build();
return app.Run();
```

`App` must contain content without `@window` or a top-level `Window`. The display
determines the viewport; `Scale` converts physical pixels to logical units.
`DevicePath` can select `/dev/dri/card0` and `ConnectorId` a connected output on
that card. Defaults search connected cards and choose a preferred video mode.
Options are captured by the host, validated before opening devices and remain
fixed for that run. A host can be constructed on another OS without initializing
Linux libraries; running it requires 64-bit Linux.

Use `UseMainView` only with this host. Desktop-window creation, secondary windows,
native message boxes and tray startup are unavailable. Reuse popup, menu and
overlay controls inside the same view. Use application lifetime to stop the app.
The [DRM sample](../../Samples/Nazor.Samples.Drm/README.md) switches the same
content between desktop development and DRM deployment.

## Display and rendering

Select `NAZOR_RENDERER=AUTO`, `GPU` or `CPU` before startup. GPU mode creates an
EGL/OpenGL ES context on a GBM scanout surface. The graphics worker draws
immutable Skia recordings and presents through KMS page flips. Explicit frame
capture reads back pixels. Normal GPU presentation stays on the graphics
device. [Renderer selection](32-Support-and-Limitations.md#nss-and-rendering)

CPU mode renders into a retained BGRA buffer and copies complete frames into
alternating DRM dumb buffers, honoring the kernel-supplied pitch. A staging
buffer is used when the software cursor must be drawn. `AUTO` can use
this path after a recognized GPU failure, including a software OpenGL device.
The current KMS path uses `drmModeSetCrtc`/`drmModePageFlip`; atomic KMS is not
required. GBM buffers stay locked until scanout completion and framebuffer
registrations include the buffer's explicit tiling or compression modifier and
per-plane metadata when GBM provides them. Registrations follow native buffer
destruction. Presentation waits are bounded;
a disconnected or failed device reports an error rather than blocking forever.

## Runtime dependencies and local session

The DRM host uses libdrm, libseat, libinput, libudev, xkbcommon and keyboard
layout data. GPU mode adds GBM, EGL, OpenGL ES and a compatible driver. These
are system dependencies for framework-dependent and self-contained apps. DRM
uses direct native calls and runs independently of the Wayland and X11 bridges,
desktop compositor and session D-Bus. [Backend support matrix](32-Support-and-Limitations.md#backend-support-matrix)

**At least one usable font is required**:
install a font family or register application-private font files. Installing
fontconfig alone does not install fonts, and self-contained publication does not
bundle system fonts. See [Linux font discovery and troubleshooting](21-Typography-and-Fonts.md#linux-font-discovery).

### Distribution packages

The commands below install runtime libraries and an example font for a
`linux-x64` application using a Mesa-compatible GPU. They do not require a
desktop environment, a compiler or development/header packages. Package managers
also install their dependencies; packages already present are reused. A font
package can be omitted when another usable family is installed or the app
registers private fonts. Proprietary NVIDIA graphics need their corresponding
compatible driver stack instead of assuming the Mesa example fits every GPU.

These are package installation recipes, not hardware certification for every
distribution or release. Use a release supported by the application's .NET
version and check its configured repositories. Derivatives follow their base
distribution and release, rather than their desktop environment.

#### Debian, Ubuntu and derivatives

For Debian 12/13 and Ubuntu 22.04/24.04/26.04 LTS:

```bash
sudo apt update
sudo apt install libdrm2 libgbm1 libegl1 libgles2 libgl1-mesa-dri \
  libseat1 libinput10 libudev1 libxkbcommon0 xkb-data fontconfig fonts-dejavu-core
```

On Ubuntu, enable the official `universe` repository if `libseat1` cannot be
found. Linux Mint, Pop!_OS and Zorin OS use the packages for their Ubuntu base;
LMDE uses its Debian base. `libegl1` pulls in the Mesa EGL implementation.
For forced CPU rendering, omit `libgbm1 libegl1 libgles2 libgl1-mesa-dri`;
keep `libdrm2` and the seat, input, keyboard and font packages.

Package references: [Debian GBM](https://packages.debian.org/trixie/libgbm1),
[Ubuntu EGL](https://packages.ubuntu.com/noble/libegl1),
[Ubuntu libseat](https://packages.ubuntu.com/libseat1) and
[Debian fonts](https://packages.debian.org/trixie/fonts-dejavu-core).

#### Fedora, RHEL, Rocky Linux, AlmaLinux and CentOS Stream

Fedora supplies the packages below in its repositories. On RHEL-compatible 9/10
systems, `libseat` is supplied by EPEL: first configure the repositories for the
specific distribution/release, including EPEL and its CRB/CodeReady Builder
prerequisites as described in the [EPEL installation guide](https://docs.fedoraproject.org/en-US/epel/getting-started/).

```bash
sudo dnf install libdrm mesa-libgbm mesa-libEGL libglvnd-gles mesa-dri-drivers \
  libseat libinput systemd-libs libxkbcommon xkeyboard-config fontconfig dejavu-sans-fonts
```

`libseat` is the library package; installing the separate `seatd` daemon is
unnecessary when using logind. For forced CPU rendering, omit `mesa-libgbm
mesa-libEGL libglvnd-gles mesa-dri-drivers`, retaining `libdrm` and the remaining
packages. This recipe targets conventional mutable systems; Fedora Atomic
variants require their image/package-layering workflow rather than host `dnf`.

Package references: [Mesa packages](https://packages.fedoraproject.org/pkgs/mesa/),
[GLES](https://packages.fedoraproject.org/pkgs/libglvnd/libglvnd-gles/),
[libseat and EPEL availability](https://packages.fedoraproject.org/pkgs/seatd/libseat/)
and [DejaVu Sans](https://packages.fedoraproject.org/pkgs/dejavu-fonts/dejavu-sans-fonts/).

#### openSUSE Leap and Tumbleweed

For Leap 16.0 and Tumbleweed with their official OSS repositories:

```bash
sudo zypper install libdrm2 libgbm1 Mesa-libEGL1 libglvnd Mesa-dri \
  libseat1 libinput10 libudev1 libxkbcommon0 xkeyboard-config fontconfig dejavu-fonts
```

`libglvnd` supplies the EGL/GLES dispatch libraries. For forced CPU rendering,
omit `libgbm1 Mesa-libEGL1 libglvnd Mesa-dri`, keeping the other packages.
Older Leap releases and SUSE Linux Enterprise have different package/repository
availability; check their release repositories before using this command.

Package references: official [Leap 16.0](https://download.opensuse.org/distribution/leap/16.0/repo/oss/)
and [Tumbleweed](https://download.opensuse.org/tumbleweed/repo/oss/) repositories.

#### Arch Linux

For Arch Linux x86_64 with a Mesa-compatible GPU, install the runtime packages
and an example font family:

```bash
sudo pacman -Syu --needed mesa seatd libinput libxkbcommon fontconfig ttf-dejavu
```

`mesa` supplies GBM and the Mesa driver; it also pulls in `libdrm` and `libglvnd`
(EGL/OpenGL ES). `seatd` supplies `libseat` and depends on `systemd-libs`
(including libudev). `libxkbcommon` pulls in `xkeyboard-config`. These transitive
dependencies do not need to be named separately in the install command.
`ttf-dejavu` can be omitted if another usable font family is already installed or
the application registers its own font files. For forced CPU rendering, replace
`mesa` with `libdrm`; the other input, seat and font requirements remain.

Install `dotnet-runtime` as well for a framework-dependent .NET 10 publication,
or `dotnet-runtime-8.0` for .NET 8. Arch derivatives such as EndeavourOS and
Manjaro use the same package names; follow their own repository/update policy.

Package references: [Mesa](https://archlinux.org/packages/extra/x86_64/mesa/),
[seatd](https://archlinux.org/packages/extra/x86_64/seatd/) and
[.NET installation](https://wiki.archlinux.org/title/.NET).

### Other distributions and publication compatibility

| Distribution | Additional requirements |
| --- | --- |
| Gentoo | Install the corresponding runtime packages through Portage and enable Mesa GBM/EGL/GLES and a suitable libseat backend through the package USE flags. Provide libudev, keyboard data, Fontconfig and a font family as well. See [Gentoo libseat options](https://packages.gentoo.org/packages/sys-auth/seatd). Source-built configurations require native validation. |
| NixOS | Package the application with its native library paths, graphics drivers and fonts configured, or use an appropriate FHS/nix-ld environment. Installing libraries into a profile alone does not make their sonames discoverable by an ordinary Linux binary. See [NixOS binary compatibility](https://wiki.nixos.org/wiki/Nix-ld). No ready-made Nazor NixOS derivation is supplied. |
| Alpine and other musl systems | A `linux-x64` publication uses glibc and cannot be made musl-compatible by installing runtime packages. Use `linux-musl-x64` or `linux-musl-arm64` and matching renderer/native assets. The repository supplies musl desktop bridge assets and container checks; physical DRM/seat/input acceptance on the intended musl system remains separate. See the [.NET RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog). |

For a framework-dependent publication on any distribution, install the matching
.NET runtime major version, using the [.NET Linux installation instructions](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
for that release. The sample's `FolderProfile` targets .NET 10. A self-contained
publication includes .NET but still needs the native libraries and fonts above,
plus the [.NET native OS dependencies](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#dependencies).
An SDK is needed to build an application, not just to run a published one.

### Local session and console

From an active local TTY on a system using logind, run the published sample as
the logged-in user:

```bash
LIBSEAT_BACKEND=logind XKB_DEFAULT_LAYOUT=it NAZOR_RENDERER=GPU ./Nazor.Samples.Drm --drm
```

With logind, the `seatd` daemon need not be enabled, even on distributions such
as Arch that package it with libseat. Use `NAZOR_RENDERER=AUTO` for automatic CPU
fallback, or `CPU` to select software rendering explicitly. A KMS-capable kernel driver
must expose a connected primary device under `/dev/dri/card*`.

Start from an active local console with logind or an accessible seatd service.
libseat mediates graphics and input access, so the application need not run as
root when its seat service is correctly configured. Standard libseat settings,
including `LIBSEAT_BACKEND`, select that service. `renderD*` nodes cannot drive a
display. A remote SSH shell without local seat ownership is insufficient.
On systems without logind, configure and start seatd according to the
distribution's service/access policy and use `LIBSEAT_BACKEND=seatd` instead.

The backend puts an available console TTY into graphics/raw-input mode and
restores its saved attributes and mode on normal shutdown or managed failure.
Seat deactivation drains rendering, suspends input and acknowledges release.
The backend restores the previous display configuration if the session still
owns the device. Reactivation resumes input and reestablishes scanout on the next
frame.
Ctrl+Alt+F1 through F12 requests a VT switch through libseat. Orderly session
switches are supported; recovery after forced device revocation or GPU removal
is not guaranteed. SIGKILL or power loss cannot execute managed cleanup.

## Input and capabilities

libinput uses udev seat discovery and hotplug. xkbcommon handles keyboard layouts,
modifiers, dead-key composition and key repeat. Set the standard `XKB_DEFAULT_*`
variables before startup to select a layout, for example `XKB_DEFAULT_LAYOUT=it`.
The compose locale follows `LC_ALL`, `LC_CTYPE`, then `LANG`.

Mouse motion, buttons, scrolling, double clicks and one active touch contact map
to the existing logical-coordinate pointer contract. The cursor is drawn into
the application surface; arrow, text and crosshair shapes are provided and other
cursor requests use the arrow. Full multitouch/gestures, an external IME and an
on-screen keyboard are not supplied. Clipboard text is local to the process.

The view is always full screen with no native window handle, title bar, system
buttons, placement or interactive resize. It exposes no desktop window
capabilities. Tray, portals, system notifications and an AT-SPI provider are not
supplied by this host. Shared controls still produce their semantic tree; this
does not establish native screen-reader support. Theme and motion preferences
use generic defaults and can be configured through the application appearance.

## Build, publish and verification

Source builds use the normal Nazor solution and .NET 8/10 runtime targets. The
DRM adapter uses managed interop into system libraries, with no additional NuGet
package or private native bridge. An application build/publish with
`NazorLinuxNativeLibraries=None` skips desktop bridge compilation and publication.
Packing the shared Linux package still requires all eight Wayland/X11 assets
described in the [native build guide](33-Linux-Wayland.md#build-from-source).
Use the existing Linux RID publication workflow; no DRM-specific RID is introduced.
The interop accepts x64 and ARM64 processes with the target's matching libc
and renderer assets. The native fixture exercises managed and native contracts
with simulated KMS, GBM, EGL and seat services. Use the separate console test
below for physical DRM scanout, seat and input acceptance. [Verification boundaries](32-Support-and-Limitations.md#verification-boundaries)

Run deterministic managed/native integration checks on Linux:

```bash
bash Scripts/Tests/verify-drm.sh Release
```

The script needs GCC plus the system input libraries/data above. It builds a C
fixture that simulates DRM/GBM/EGL/libseat, then verifies display selection,
buffer lifetimes, failure cleanup, CPU fallback, real xkbcommon/input setup and
an actual Skia-rendered single-view application. A separate test process simulates
libinput callbacks during teardown. The script runs without acquiring a display.
An optional second argument supplies a prebuilt `Nazor.Linux.Tests.dll`, useful
when managed assemblies were built on Windows and executed with the Linux runtime.
Logs/results are written below `artifacts/drm-tests`.

Physical acceptance is a separate, explicitly enabled test from a local console:

```bash
NAZOR_TEST_DRM=1 NAZOR_RENDERER=GPU dotnet Tests/Nazor.Linux.Tests/bin/Release/net10.0/Nazor.Linux.Tests.dll -class '*DrmHardwareTests'
```

Repeat with `NAZOR_RENDERER=CPU` and `AUTO`. Optionally select the test card with
`NAZOR_TEST_DRM_DEVICE=/dev/dri/card1`. Also exercise the sample with actual input,
VT switching, shutdown, cable disconnect and the intended GPU/driver. Automated
fixtures do not certify hardware rendering, touch calibration, display hotplug,
console recovery, ARM64 or performance. One fixed display is supported; runtime
rotation, mode changes, multiple displays and automatic reconnection are not.

---

[Guide index](README.md) · [Support and limitations](32-Support-and-Limitations.md)
