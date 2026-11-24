# 14. Title bars and platform integration

Select complete decorations through `Window.Decorations` during initialization. Desktop defaults prefer the complete native configuration and then the complete Nazor configuration. User content is always an explicit choice. Root NSS controls the border, radius and shadow of a Nazor frame independently of interactive resizing.

In every theme the control gallery prefers `Native(User, Native)` and falls back to `Nazor(User, Nazor)`, using a tall `UserTitleBar` that holds the component search. Theme changes preserve that search and its focus. Fluent requests `CommonBackdrop.Solid`, which is Mica on Windows; Basic, NazorLeaf and None use `CommonBackdrop.None`. All use `BackgroundMode.Default`. Custom frame and transparency examples are explicit demonstrations; unsupported native decorations still use the negotiated fallback.

## Compose a title bar

For a ready-to-use title bar select `WindowDecorations.Nazor()`: Nazor composes the title, application icon when available and standard caption controls. Windows resolves the default icon from the executable; Linux desktop supplies the embedded blue Nazor application tile. `Window.Icon` overrides either default, and null restores it. See [per-window icons](13-Windows-and-Dialogs.md#per-window-icons). For application content, declare `UserTitleBar` in the same `.nazor` file as `@window`:

```razor
@window

<UserTitleBar Class="caption">
    <TextBlock>@Window.Title</TextBlock>
    <TextBlock>Workspace</TextBlock>
</UserTitleBar>

<Container><TextBlock>Application content</TextBlock></Container>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "My application";
        Window.Decorations = WindowDecorationPolicy.Require(
            WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Nazor));
    }
}
```

The host anchors the selected title bar above the application content. A `UserTitleBar` declaration is allowed while another provider is selected: its children remain unmounted, occupy no space and receive no input. Changing back can recreate those children; keep persistent state outside the caption subtree. At most one declaration is allowed per window render. A selected User provider requires its declaration. Missing active content is an authoring error, not a reason to select a fallback.

`UserTitleBar` inherits the composable surface's slots: Leading, AppIcon, Title/TitleContent, Subtitle, Center and Trailing. `TitleBar` itself is the ordinary surface primitive, not a window declaration or a policy switch. Empty caption areas drag the window; interactive children retain their input. On Windows, pointer movement over client caption drag areas also updates NSS hover, so groups retain their hover styling across gaps between buttons. Fullscreen excludes the caption.

Custom controls participate through their normal input contracts: focusable elements, pointer callbacks (including internal/default callbacks and pointer event modifiers), tooltips, selectable text and scrollable content retain client input inside any title bar or `DragRegion`. Pointer capture keeps input in the client until released. `SnapLayoutArea` retains its maximize behavior when there is no active capture. Ordinary labels and empty spacing remain draggable.

On Windows 11, the backend marks the application-owned native caption strip as a Windows App SDK passthrough region, excluding the native caption button insets. This lets actual desktop input reach Nazor's hit testing, which still distinguishes controls from draggable gaps. The region follows the native client width, title-bar height and button insets during resize and DPI changes, and is cleared when the original system caption is restored. If Windows denies registration during a frame change, the backend keeps Win32 hit testing active and can retry at the next caption refresh. See [Windows interactive title-bar content](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar#interactive-content).

Compound fields forward primary presses on padding and decorative content to their editor. Repeated presses preserve focus; independent embedded buttons and other focusable children retain their own focus behavior. A synchronous focus request made by a pointer handler is preserved instead of being replaced by automatic pointer focus, and `FocusAsync` requests during pointer-down handling use pointer focus cues. Tooltip hover, dismissal, keyboard access, context menus and suggestion popups use the same routing as application content. A tooltip stays open while the pointer moves over its noninteractive content; that surface still lets clicks pass through.

Client bars emit `TitleBar[source="nazor"]` or `TitleBar[source="user"]`. The shared parts include TitleBarLeading, TitleBarAppIcon, TitleBarHeading, TitleBarTitle, TitleBarSubtitle, TitleBarCenter and TitleBarTrailing. Use an application stylesheet for global rules; component scope and deliberate deep selectors still apply across component boundaries.

## Switch the frame while open

```csharp
await Window.SetDecorationsAsync(WindowDecorationPolicy.Prefer(
    WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native)));
```

`Require` authorizes one exact configuration and reports `PlatformNotSupportedException` if it is unsupported.

`Prefer` tries the primary configuration and any supplied alternatives in order, then automatically appends `WindowDecorations.Native()` (system frame, title bar and controls) and `WindowDecorations.Nazor()` (Nazor frame, title bar and controls). Duplicate configurations keep their first position. This also applies when only one preference is supplied. In the example, unavailable integrated native controls fall back to complete system decorations, then complete Nazor decorations. To prioritize a client frame before system decorations, supply `WindowDecorations.Nazor()` as an explicit alternative. Failure of every option still reports `PlatformNotSupportedException`; missing active User content remains an authoring error. `Require` never adds these defaults. Windows supports integrated controls on a native frame; the current Linux desktop backends do not.

| Complete request | Windows | Wayland and X11 |
| --- | --- | --- |
| `Native()` | Original system frame, title bar and controls | Requires confirmed server decorations |
| `Native(Nazor or User, Native)` | Client content with original native controls | Unsupported |
| `Native(Nazor or User, Nazor or User or None)` | Client content and the selected client controls | Unsupported |
| `Native(None, None)` | Native exterior without a caption | Unsupported |
| `Nazor(Nazor or User, Nazor or User or None)` | Client frame, selected content and controls | Client decorations require desktop acceptance |
| `Nazor(None, None)` | Client frame without a caption | Client decorations require desktop acceptance |
| `Nazor(Nazor or User, Native)` | Unsupported | Unsupported |

The factory rejects `None` title-bar content with visible caption controls. Original system title bars and their controls are an indivisible request. A User declaration does not add backend capabilities. These rows describe implemented desktop backends; query the current window for support rather than identifying the operating system in application code. Single-view hosts do not provide desktop decorations.

Changes run on the UI thread without replacing the native window, root content, owner, services or placement. `AppliedDecorations` identifies the accepted option and remains null during pending opening negotiation. Its `OptionIndex` addresses the policy's immutable `Options` list, including default fallbacks and with duplicates removed for `Prefer`. `Window.Chrome` reports actual owners; observe ChromeChanged and CapabilitiesChanged. Normal, maximized, minimized and fullscreen transitions preserve the accepted configuration. Native desktop constraints can reject a requested change.

Requests are serialized. The caller's cancellation token cancels before native dispatch; an already dispatched negotiation settles before the request completes. Closing the window cancels outstanding callers even when the native event loop is stopping. A failed runtime change attempts to restore the previous accepted configuration; a restoration failure reports both errors.

On Linux, changing the frame owner requires leaving fullscreen first. The accepted owner remains supported while its decorations are temporarily hidden; client content and controls can change on that same owner during fullscreen.

Weston Xwayland permits choosing the frame owner at startup but reports `FrameChanges = Unsupported` while the window is open. Query `GetChangeSupport` before offering a runtime owner switch; `Require` rejects an unsupported transition and preserves the accepted configuration.

## App-owned buttons and capabilities

`WindowCaptionButtons` selects Native, Nazor, User or None. Native means original desktop controls, not a Nazor imitation. Nazor adds the active standard group. User instantiates only `UserCaptionButtons`; None adds no group. Declare app controls separately so they can accompany either the Nazor or User title bar:

```razor
<UserCaptionButtons>
    <Right>
        <Button OnClick="() => Window.Minimize()">Minimize</Button>
        <Button OnClick="() => Window.Close()">Close</Button>
    </Right>
</UserCaptionButtons>
```

Left and Right are physical sides, independent of text direction. ChildContent is shorthand for Right and cannot be supplied together with it. The application supplies accessible names, enabled states, commands and optional Snap areas. It can explicitly reuse CaptionButtons or WindowCommands. Inactive fragments are not instantiated. A selected User group requires a declaration containing at least one supplied fragment; at most one declaration is allowed per render.

`Window.Capabilities.Decorations.GetSupport(configuration)` checks the complete configuration; GetChangeSupport additionally checks the transition from the accepted configuration. Supported, Unsupported and Unknown distinguish confirmed backend support, rejection and required negotiation. General NativeFrame, NazorFrame, NativeTitleBar and IntegratedNativeCaptionButtons indicators are informational: they cannot be combined to infer full-configuration support.

The Theming sample lets you choose the title bar and frame independently, shows why a pair or transition is unavailable, and applies the exact pair through `Require`. [Theming sample](../../Samples/Nazor.Samples.Theming/README.md)

Use `CanExecute(WindowCommand)` and `Request(WindowCommand)` for caption commands and Close() to close. WindowCommands follows state and capability changes. SystemButtonsLayout reports physical desktop conventions even when controls are hidden; SystemButtonsMetrics measures only occupied integrated native control areas. Nazor and User controls participate in the client layout. CaptionButtons supplies localized names, tooltips, maximize/restore state, default non-Tab-stop behavior and the native Snap area where supported.

SnapLayoutArea marks a supported maximize hit region; it does not execute a command. Its child runs maximize once. On unsupported backends it is an ordinary container. DragRegion provides additional explicit draggable client areas. A frame with no visible border can still resize when permitted by operations and backend support.

## Title bar context menu

Window.TitleBarMenuMode selects System, Nazor or None for caption clicks handled by the backend. The desktop can own caption interactions outside the client. The menu's effective owner is reported separately in Window.Chrome. Native menu injection and desktop shortcuts remain backend-specific.

## Native NSS and measurements

```css
:root {
    -windows-title-bar-height: tall;
    -windows-title-bar-button-foreground-color: white;
    -windows-title-bar-button-hover-foreground-color: white;
    -windows-title-bar-button-pressed-foreground-color: white;
}
TitleBar { min-height: var(--nz-window-controls-height, 32px); }
```

All `-windows-title-bar-*` declarations belong to the root `Window` and use the normal NSS cascade. `standard` and `tall` are Windows 11 integrated caption preferences. The twelve native colors allow each button state to be specified independently. Use `auto` to remove an explicit override. On a custom title bar, native buttons have transparent backgrounds at rest in both active and inactive windows. In high contrast, the backend defers to the native system colors when there is no explicit color. Without a backdrop, the Windows 11 backend supplies an opaque system caption (`#eef4f9` light, `#1e2123` dark), independently of the app theme. Windows still draws the text and buttons. NazorLeaf and Fluent explicitly set button colors only on `:root[client-titlebar]`, and application styles can override them.

With no application backdrop, the backend uses the solid caption color for the current light or dark mode. Requesting Mica, Mica Alt or Acrylic clears that color and applies the material behind the window, including the system title bar and caption buttons. An accepted Windows backdrop uses Skia raster so the DWM material remains visible through the client surface. Clearing it restores the requested renderer policy, opaque client background and solid caption. Explicit NSS caption colors or opaque custom backgrounds can cover the material. The optional `Nazor.Themes.NazorLeaf` package provides a tinted caption style. Register its packaged fonts before `Build()` and set `app.Appearance.Theme = NazorLeafTheme.Theme` on the built app. [NazorLeaf API](../ApiReferences/Nazor.Themes.NazorLeaf.md) · [Theme selection](16-Styling-and-Themes.md)

Ordinary `color`, `background` and `height` on `TitleBar` style the content painted by Nazor. Windows 10 and other backends ignore Windows 11 native appearance properties.

`--nz-window-controls-height` is supplied only while integrated buttons occupy a visible custom title bar. It is absent for an external system title bar, hidden/minimized/fullscreen/windows without integrated native controls, hidden buttons, or unsupported backends. Use a fallback where needed. An explicit zero differs from an absent value. Overriding the variable does not resize native buttons.

## Resize grip and frame edges

Replace `-windows-window-resize-grip` with `-window-resize-grip` when upgrading an existing stylesheet. The portable property accepts the same `auto` and `none` values.

Set `-window-resize-grip: auto` on `:root` to show a Nazor-drawn diagonal grip in the lower-right corner of the client area on supported desktop backends. The glyph is painted over the client background, adds no padding, and can use `-window-resize-grip-color` to follow the window's foreground or a theme token. The grip can be used with a system title bar when the window is resizable and in its normal state. The backend starts the platform's interactive resize.

```css
:root {
    -window-resize-grip: auto;
    -window-resize-grip-color: var(--nz-accent, currentColor);
}
```

The [frame resize properties](19-NSS-Property-Reference.md#window-frame-and-resize) adjust where a pointer can grab a Nazor-managed frame edge. Use `Window.Chrome.Frame` to distinguish the owner, including when Wayland falls back to a Nazor-drawn frame. System-owned edge zones follow the desktop's rules. On Windows, the Nazor frame's native rounded region preserves the antialiased border edge, including fractional corner radii.

## Native system title bar colors

Select `WindowDecorationPolicy.Require(WindowDecorations.Native())` to retain the system title bar and style its colors on the implicit root:

```css
:root {
    -windows-title-bar-background-color: #eff6fc;
    -windows-title-bar-foreground-color: #202e38;
    -windows-title-bar-inactive-background-color: #e2e8f0;
    -windows-title-bar-inactive-foreground-color: #6b7280;
    -windows-title-bar-button-background-color: #3981f6;
    -windows-title-bar-button-foreground-color: white;
    -windows-title-bar-button-hover-background-color: #2563eb;
    -windows-title-bar-button-pressed-background-color: #1d4ed8;
}
```

All twelve WinUI Gallery colors are supported. See the [property reference](19-NSS-Property-Reference.md#windows-native-properties). Use `auto` to restore the default color. On the system title bar, the active and inactive button backgrounds default to the corresponding title bar background. Windows cannot show the window through system caption buttons, so a translucent button background (rest, inactive, hover or pressed) is composited over the opaque title bar color. A `transparent` button therefore shows the bar's own color. Windows 10 and other backends ignore these declarations. On a custom title bar, the button colors still apply to integrated native controls, while the title and bar background remain app content. Button backgrounds support transparency, and Windows controls native text alpha and the close-button hover/pressed background. See [AppearanceWindow.nazor](../../Samples/Nazor.Samples.Windowing/AppearanceWindow.nazor) for all colors, size limits and screen positioning.

## Full screen

`EnterFullScreen()` occupies the current monitor without a frame or taskbar allocation and without changing display resolution. `WindowState.FullScreen` differs from maximized. `ExitFullScreen()` or `Restore()` returns to the saved normal/maximized state; exiting outside a fullscreen session does nothing.

The custom title bar hides in full screen. Provide an exit command in the content or a registered shortcut. Minimize preserves the fullscreen session; native restore can resume it, while `Restore()` ends it. `Maximize()` exits fullscreen and maximizes. Placement persistence retains the pre-fullscreen placement. Observe actual `StateChanged` events instead of assuming a request always succeeded.

## Backdrops and environment

Set `Window.BackgroundMode = BackgroundMode.Transparent` in the window's `OnInitialized` to composite its NSS backgrounds with the desktop using per-pixel alpha. Check `Window.Capabilities.CanUseTransparentBackground` before selecting the mode on multiple platforms. A fully transparent root exposes the desktop, and colors, gradients, or images on the root can supply their own alpha.

On Windows this mode uses the Skia raster renderer and DirectComposition. On Windows 11, a custom title bar can keep the native buttons with `WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native)`. The title bar's NSS background controls the color behind the buttons. `BackgroundMode.Default` uses the ordinary presentation path and honors `Window.Backdrop`. `Transparent` takes precedence over a requested backdrop.

On Wayland and X11, transparent windows use the Skia raster renderer. X11 requires an ARGB visual and a running compositing manager. The controls that Nazor draws on the client side sit over the same transparent surface. Decorations drawn by the compositor or window manager follow its own appearance.

`IWindow.Backdrop` accepts a `BackdropSelection`. Portable presets resolve as follows, when supported:

| Preset | Windows | Wayland |
| --- | --- | --- |
| `CommonBackdrop.None` | No material; opaque light/dark system caption | No compositor effect |
| `CommonBackdrop.Blur` | Acrylic | Background blur |
| `CommonBackdrop.Solid` | Mica | No compositor effect |

Explicit platform selection can refine a preset:

```razor
@window
<TextBlock>Material-backed content</TextBlock>

@code {
    protected override void OnInitialized()
    {
        Window.Backdrop = Backdrop.ForPlatform(CommonBackdrop.Blur, Windows: WindowsBackdrop.Mica);
    }
}
```

You can also select mappings without a base preset, for example `Backdrop.ForPlatform(Windows: WindowsBackdrop.Acrylic, Linux: LinuxBackdrop.Blur, Default: CommonBackdrop.None)`. Windows materials are `Acrylic`, `Mica`, and `MicaAlt`; the Linux material is `Blur`. `None` belongs only to `CommonBackdrop`. To enable an effect on just one platform, use `CommonBackdrop.None` as the preset and supply that platform's override, for example `Backdrop.ForPlatform(CommonBackdrop.None, Linux: LinuxBackdrop.Blur)`.

The active backend resolves the request. A missing platform mapping uses the preset or default. An unsupported material uses `Default`, with opaque content as the final fallback. `CommonBackdrop.Solid` requests Mica on Windows. Wayland blur requires `ext-background-effect-v1` on the connected compositor. X11 supports per-pixel transparency with a compositing manager and has no blur integration. `IWindow.AppliedBackdrop` reports the accepted result. The compositor controls the visible effect.

Windows materials depend on OS version, system settings, window activity, and content transparency. Keep an opaque fallback. The native frame follows the app's effective color scheme.

`IWindow.Environment` exposes safe insets and a separate keyboard-occlusion rectangle; `EnvironmentChanged` reports updates. These contracts avoid assuming all viewport edges and input conventions are identical on every future backend. Insets depend on the backend; current desktop backends report their available geometry.

API: [Nazor.Core](../ApiReferences/Nazor.Core.md), [Nazor.Platform](../ApiReferences/Nazor.Platform.md), [Nazor.Platform.Windows](../ApiReferences/Nazor.Platform.Windows.md).

---

[Guide index](README.md) · [API reference](../API.md)

With optional DevTools enabled, Windows adds **Open Nazor DevTools** to the native window menu by default. Its displayed shortcut follows `DevToolsOptions`; menu inclusion and keyboard activation can be disabled independently. On Wayland the compositor-owned system menu is retained without injected entries. See [launch configuration](../DevTools.md#shortcut-and-system-menu).
