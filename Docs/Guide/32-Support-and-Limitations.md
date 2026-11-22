# 32. Support and limitations

This guide describes Nazor `0.1.0-alpha`, a public pre-alpha for evaluation and feedback. APIs, component markup, NSS and tooling may change between previews.

## Platform and distribution

Windows 10/11 x64 and .NET 10 are the primary verification baseline. Runtime packages contain .NET 8/9/10 assets and can be consumed by .NET 11 applications. The application and tool runtime requirements are separate. macOS, Android and iOS lack application hosts. [Visual Studio 2022 and .NET 8](03-Projects-and-Builds.md#visual-studio-2022-and-net-8)

Desktop applications and shared component libraries target .NET 8 or later. Executables receive portable or RID-specific desktop hosting with the same `DesktopPlatformHost` API. Portable publications include Windows and Linux Wayland/X11 and select at runtime; a Windows or Linux RID includes only that backend. Unsupported desktop RIDs produce `NZB003`; portable applications on unsupported operating systems produce `PlatformNotSupportedException`. Android/iOS require dedicated hosts and lifecycle integration that are not implemented. See [target frameworks and native hosts](03-Projects-and-Builds.md#target-frameworks-and-native-hosts).

Building the framework from this checkout uses the SDK `10.0.404` feature band with patch roll-forward from [global.json](../../global.json). For applications using NuGet packages, follow the [application prerequisites](02-Getting-Started.md#prerequisites) and keep official SDK, runtime and theme packages on matching versions. The supported deployment matrix for Windows x64 and Linux x64 covers directory and single-file publication, self-contained trimming without AOT in both modes, and Native AOT with .NET 8 or .NET 10 through `Nazor.Sdk` or the repository's Nazor targets. Single-file keeps standard .NET extraction defaults. AOT requires DevTools disabled and AOT-compatible application dependencies. This deployment matrix covers x64. Linux ARM64 assets are listed separately below. Signing and installer creation are outside the framework's scope. [Deployment](28-Libraries-Packaging-and-Deployment.md#publish-an-application)

.NET 11 compatibility uses Windows x64 packaged template/control consumers and native publication, selecting compatible .NET 10 Nazor assets. Source builds and compiler/language-server suites run on .NET 10 LTS. The tools bundle C# 15 Roslyn/Razor services sourced from SDK 11.0.100, independently of their runtime; see [language version selection](03-Projects-and-Builds.md#build-tools-and-application-targets). .NET 9 also has Windows native startup and UI Automation checks through VS2022/MSBuild. The existing .NET 8/10 Linux deployment matrix remains distinct; Linux .NET 11 publication, macOS/mobile hosting and physical embedded hardware require separate acceptance.

## Backend support matrix

For trimming and Native AOT, use explicit `PropertyGrid.Items`; enum choices use an AOT-compatible enumeration path. Supply `PropertyGridItem.ChoiceText` for custom enum labels. Reflection-based `Target` assignment and `PropertyGridItem.FromObject` carry `RequiresUnreferencedCode` because the application must preserve its inspected properties and enum label attributes. This warning belongs to reflection-based usage rather than ordinary explicit-item rendering.

Choose a RID for the architecture and C library on the target. `linux-x64` and `linux-arm64` use glibc. `linux-musl-x64` and `linux-musl-arm64` use musl. A Linux RID selects matching native assets and does not certify a distribution, board or GPU driver. Portable desktop packages ship both Windows and Linux hosts. Linux desktop packages can include Wayland, X11 or both. Choose the DRM host explicitly with `DrmPlatformHost` and `UseMainView`. [Linux desktop selection](35-Linux-X11.md#select-and-publish) · [DRM startup](34-Linux-DRM.md#application-startup)

Framework-dependent apps require the matching .NET runtime. Self-contained apps bundle .NET and still need their platform libraries, fonts and drivers. Nazor's package dependencies supply Skia and HarfBuzz assets. On Linux, install fontconfig and a usable system font family, or register private fonts with the app. [Linux font discovery](21-Typography-and-Fonts.md#linux-font-discovery)

| Backend | Runtime assets and dependencies | CPU and GPU presentation |
| --- | --- | --- |
| [Windows](14-Title-Bars-and-Platform-Integration.md) | `win-x64` is the verification baseline. Use Windows 10/11 with its system fonts and the interop libraries supplied in the Windows publication. | CPU uses Skia raster. GPU uses hardware OpenGL via WGL. Transparent windows and supported Windows system backdrops use raster presentation. |
| [Wayland](33-Linux-Wayland.md#runtime-dependencies) | All four Linux RIDs. Requires `libnazor-wayland.so`, `libwayland-client.so.0` (1.20 or later), `libwayland-cursor.so.0`, `libxkbcommon.so.0`, and a reachable Wayland compositor with `xdg_wm_base`. | CPU presents retained pixels through `wl_shm`. GPU uses hardware OpenGL ES through `libEGL.so.1` and `libwayland-egl.so.1`. Transparent windows use raster. |
| [X11](35-Linux-X11.md#system-dependencies-and-behavior) | All four Linux RIDs. Requires `libnazor-x11.so`, `libX11.so.6`, `libXcursor.so.1`, `libXrender.so.1`, `libXext.so.6`, `libXrandr.so.2`, and a reachable X server. | CPU presents damaged rectangles from retained pixels. GPU uses hardware OpenGL ES through `libEGL.so.1` with a matching X11 visual. Transparent windows use raster. |
| [DRM/KMS](34-Linux-DRM.md#runtime-dependencies-and-local-session) | Linux x64 or ARM64 with matching libc and renderer assets. Requires a KMS-capable device, an active seat, libdrm, libseat, libinput, libudev, xkbcommon and keyboard layout data. | CPU presents through DRM dumb buffers. GPU uses hardware OpenGL ES through GBM and EGL on a KMS scanout surface. |

| Capability | Windows | Wayland | X11 | DRM/KMS |
| --- | --- | --- | --- | --- |
| Windowing and frames | Native windows, custom frames, placement, fullscreen and per-monitor DPI | Native windows, runtime frame switching, compositor-managed placement and scaling | Native windows, runtime frame switching and EWMH window states. The connection uses `Xft.dpi` without live per-monitor updates | One fixed display and a single view with no native window frame |
| Transparency and backdrop | Transparent background and OS-dependent Acrylic/Mica materials | Transparent background and compositor-dependent blur | Transparent background with an ARGB visual and a compositing manager | Opaque single view |
| Pickers, tray and notifications | Native file pickers, shell tray and banners | XDG FileChooser portal, StatusNotifierItem/DBusMenu and freedesktop notifications when the corresponding services are available | Same session services as Wayland, with an X11 owner handle for portal dialogs | No desktop pickers, tray or system notifications |
| Launcher | Jump Lists and taskbar count or icon overlays | Numeric badges with a compatible host and static actions in a `.desktop` file | Same launcher services as Wayland | No desktop launcher |
| Input and accessibility | Native clipboard, IME and UI Automation | Clipboard through `wl_data_device`, optional `text-input-v3`, and AT-SPI through the accessibility bus | Native clipboard, XIM commits and AT-SPI through the accessibility bus | Local clipboard, libinput pointer/keyboard and one touch contact, with no native screen-reader provider |

Inspect [`Window.Capabilities`](14-Title-Bars-and-Platform-Integration.md#app-owned-buttons-and-capabilities) and the relevant [desktop-service capabilities](15-Tray-and-System-Notifications.md) before offering optional features. The session, compositor, window manager and hardware determine which operations are available.

Desktop decoration defaults explicitly prefer the complete native configuration, then the complete Nazor configuration. User title bars and controls require explicit selection. Windows supports client title-bar content with native controls while retaining the native exterior. Current Wayland and X11 backends negotiate complete server or client decorations and do not integrate native controls into client content. A Nazor frame can have custom NSS borders and independently permit or disable resizing. Single-view hosts do not provide desktop decorations. See the [combination matrix](14-Title-Bars-and-Platform-Integration.md#switch-the-frame-while-open).

### Verification boundaries

| Backend | Automated coverage | Interactive acceptance |
| --- | --- | --- |
| Windows | x64 native startup and CPU/GPU checks, with UI Automation and publication fixtures | Exercise mixed-DPI monitors, Snap, live IME, screen readers, system caption and shell behavior on the target OS. |
| Wayland | Private headless Weston and D-Bus fixtures, plus glibc/musl container runs | Exercise a real compositor, GPU driver, fractional scaling, optional desktop protocols and assistive tools. |
| X11 | Native and managed probes on isolated Xvfb/Openbox, including glibc/musl and emulated ARM64 clients; CPU decoration matrix on Weston Xwayland | Exercise the target display server, window manager, compositing manager, GPU driver and external IME. Weston Xwayland cannot change frame ownership on an open window. |
| DRM/KMS | Managed tests and an isolated native fixture for device lifetimes, input, rendering and fallback | Use an active local console and test GPU/CPU scanout, seat permissions, VT switches and real input on the device. |

Container and QEMU runs establish coverage for those environments. Physical ARM64, vendor graphics drivers and desktop behavior require separate target-machine acceptance. [Test suites](../../Tests/README.md) · [Verification scripts](../../Scripts/README.md)

## Components and controls

Generic components, fragments/templates, cascading values, callbacks, captured attributes, field bindings, route pages, and navigation history are implemented. HTML/static markup, browser APIs, JavaScript interop, Blazor render modes, error boundaries, generated forms and a model-validation framework are not supplied. Form/Field/Fieldset provide semantic scope and callbacks with application-owned models and validation.

Navigation recreates page instances; query values do not automatically bind to page parameters. Window close cannot be canceled. Native owner-modal dialogs do not disable every other application window.

The [control catalog](09-Control-Catalog.md) includes text/password/number editing, choices, dates, virtualized lists/trees, tabs, split panes, progress, commands, and overlays. Large list virtualization assumes fixed row heights. General scrolling Container content is not virtualized. Scrollbars support NSS styling, thumb dragging, track paging, optional end buttons and animated autohide; content scrolling supports NSS smooth behavior and configurable wheel distances. No general app Canvas, video player, or embedded WebView control is provided by that catalog.

App menus support Submenu, Separator, checked/radio items and caller-owned text/icon content. Automatic mnemonics are not implemented. Tooltip supports noninteractive Trigger/Content composition and a compact text property. Anchored popovers, menus and drop-downs can use external popup surfaces when `Window.AllowPopupsOutsideWindow` is enabled and `CanShowPopupsOutsideWindow` is true. Windows and Wayland support these surfaces. X11 requires an ARGB visual and a compositing manager. Popup surfaces use raster presentation and preserve NSS styling. Dialog and AlertDialog render inside the current window. DRM/KMS uses in-viewport overlays. Native typed tray menus separately support commands, separators, submenus, icons, checkboxes and typed radio groups on Windows and Linux. Linux rendering depends on the desktop host. [Popup surfaces](11-Commands-Menus-and-Popups.md#popup-surfaces-outside-the-window)

## NSS and rendering

Block, wrapped flex, grid, absolute/relative positioning, custom properties, structural/logical selectors, media preferences, deep component selectors, gradients, shadows, 2D transforms, rounded inset clipping, transitions, and keyframes are implemented. The [property reference](19-NSS-Property-Reference.md) defines supported grammars; CSS compatibility must not be inferred beyond it.

NSS supports decorative `::before`/`::after` with textual `content`, editable `::placeholder`, editable `::selection` colors, and explicitly exposed `Component::part(name)` targets. See [pseudo-element contracts and limits](17-Selectors-and-Media-Queries.md#pseudo-elements). Control/validation states, filtered sibling positions, `:has()`, explicit custom states, logical item positions and scoped component roots follow the [selector guide](17-Selectors-and-Media-Queries.md). `:lang()`/`:dir()` and inherited language/direction/bidi layout contracts are not implemented. Other pseudo-elements, general browser inline formatting, highlight inheritance, content images/counters/attributes, important/revert, most at-rules, font-face, fixed/sticky positioning, subgrid/named grid lines, 3D transforms, filters outside the documented ordered subset, compositing operators beyond source-over and the documented blend modes, advanced color forms beyond explicit rectangular Oklab mixing, and general animation timelines/events remain unsupported. The bounded imperative `TransitionEngine.Start` mechanism reuses registered style animations; it is not a general timeline/event API. [Animation contracts](23-Transitions-and-Animations.md). Font-size/line-height/letter-spacing do not animate.

Native hosts default to `AUTO`. Set `NAZOR_RENDERER=AUTO`, `GPU` or `CPU` before starting the application. `AUTO` tries a hardware context and falls back to CPU for recognized graphics failures or a software OpenGL device. `GPU` requires hardware when the surface supports GPU presentation and reports initialization failures. `CPU` skips GPU context creation. A transparent window or supported Windows system backdrop uses raster as described in the [backend matrix](#backend-support-matrix). Nazor reads the setting at startup and rejects invalid values.

GPU work runs on a shared rendering thread with at most one pending complete frame per window. `NazorWindow.Rendering` reports the requested and effective renderer, device and fallback reason. `CaptureFrameAsync()` explicitly reads back GPU pixels; normal presentation does not. Headless image rendering remains software rendering. Damage tracking and culling reduce painting, including when a rounded clip changes. Clean exposure paints reuse the existing scene/display list. A newly visible GPU window repaints its retained surface to account for frames that were dropped while hidden.

NSS animations and transitions containing only transform, transform-origin and opacity can run from detached snapshots on the compositor, without repeated UI layout or drawing construction. Layout-changing animations, mixed paint animations, smooth scrolling, scrollbar motion and manual clocks retain the UI-driven path. Input hit testing samples the current composited transform. Windows uses driver synchronization when available; unsynchronized surfaces use a 16 ms animation timer rather than claiming monitor-perfect pacing. Reduced motion and hidden windows stop autonomous animation.

`FramePresented` reports backend completion on the UI dispatcher and coalesces while the dispatcher is busy. Its UI and render-worker frame numbers are distinct during autonomous animations. Backend completion is not a measurement of physical display scanout. DevTools distinguish UI recording/submission from the last completed worker frame; worker time includes CPU submission and presentation wait, not GPU execution timestamps.

Text supports shaping/fallback and basic wrapping but remains LTR, without full Bidi, complete Unicode line breaking, or hyphenation. Horizontal text-only inline flow supports shared wrapping, font metrics, normalized selection/copy, fragment decoration and inline link input/accessibility in non-flex/non-grid text containers. General bidi/RTL, inline replaced elements and inline-block, mixed block/inline anonymous formatting boxes, generated inline pseudo-elements, decoration slicing, inline ellipsis, rich-text editing and partial-ligature caret/color subdivision remain unsupported. Keep a grapheme or ligature within one shaping/color style. Paragraph drag selection can cross links, but must start outside actionable inline links. [Running text](10-Text-Input-and-Data-Controls.md#format-running-text) SVG support is a shape/stroke subset rather than a browser SVG engine.

## Desktop integration

`IApplicationLauncher` supplies application-wide launcher tasks and mutually exclusive count/icon badges with independent capabilities. On Wayland and X11, configure static launcher actions in a `.desktop` file. The desktop shell controls whether a badge is visible. [Launcher support and limitations](15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges)

Custom title bars, effective operation capabilities, native caption controls, fullscreen, placement persistence, backdrops, tray icons, and Shell banners are implemented for Windows. Some caption styling and materials depend on Windows version and composition support. A backdrop request may become an opaque fallback.

Notifications have no delivery guarantee, durable history contract, actions/groups, or post-process activation. Tray visibility depends on shell policy and overflow placement. A windowless process needs explicit shutdown and a recovery path when desktop integration is unavailable.

Mouse, keyboard, focus, pointer capture, scrolling, clipboard, and IME integration are present. Dedicated touch/pen gesture handling, OS drag-and-drop, and automatic menu mnemonics are not established features.

## Accessibility, tooling, and verification

UI Automation provides names, descriptions, heading/field state, table/grid coordinates and header relationships, dialog/window semantics, selection/range/toggle/invoke/expand/scroll behavior, and field text access. Linux AT-SPI exposes the corresponding semantic table, table-cell, dialog, range and text contracts. Not every UIA virtualization, ScrollItem, or geometry-event pattern is implemented. Real screen-reader behavior, IME composition, Snap UI, native caption appearance, mixed-monitor DPI, Explorer restart recovery, and notification suppression require interactive validation.

Editor support includes language-server features, source mappings, available project analyzer/code-fix integration, and hot reload. Unsupported .NET edits can require restart. Terminal hot reload uses both dotnet watch and the Nazor watcher. The optional Agent bundles a separate .NET 10 [DevTools inspector](../DevTools.md) for Windows/Linux x64 with F12 and Visual Studio/VS Code launch, retained captures and current-stop field reads through the supported IDE adapters. Direct diagnostic-session APIs do not launch the inspector. IDE UI behavior cannot be certified solely by compiler/LSP tests.

For a specific problem, include the package and SDK versions, OS, RID, backend, effective renderer and a minimal reproduction. On Linux, include the compositor or window manager for a desktop issue and the seat provider for a DRM issue. [Troubleshooting](30-Troubleshooting.md)

---

[Guide index](README.md) · [API reference](../API.md)

Ordered element/content filters support blur and eight color adjustments; backdrop sampling operates within the current rendered surface; cross-window sampling and desktop materials remain separate native capabilities. [Backdrop contracts](22-Images-Icons-and-Drawing.md#element-backdrop-filters).

Explicit OpenType features and opt-in synthetic weight/style are supported through the shared shaping/raster pipeline; synthesis defaults to none and does not change advances. See [typography contracts](22-Images-Icons-and-Drawing.md#opentype-features-and-synthetic-styles). Bidi layout, language-aware shaping selection and arbitrary oblique angles remain separate work.
