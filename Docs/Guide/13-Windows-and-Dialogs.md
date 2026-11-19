# 13. Windows and dialogs

The SDK includes native windows and the window-local `Dialog`, `AlertDialog` and `DialogButton` controls.

A `.nazor` file with `@window` defines a native window. Its generated base supplies `Window : IWindow` before parameters and initialization. Set the title and initial properties in `OnInitialized`; use the same object for later changes. The directive does not take a title. A window definition cannot be used as a child tag: open it through `IWindowService` instead. Static nesting is a compiler error; dynamic nesting is rejected before construction.

## Opening parameters

```razor
@window (int OrderId, string? CustomerName = null)

<TextBlock>@($"Order {OrderId} — {CustomerName}")</TextBlock>
<Button OnClick='() => Window.Title = $"Updated order {OrderId}"'>Change title</Button>

@code {
    [Parameter] public bool ReadOnly { get; set; }

    protected override void OnInitialized()
    {
        Window.Title = $"Order {OrderId}";
        Window.Width = 900;
        Window.Height = 600;
    }
}
```

`Windows.Open<OrderDetails>(new Dictionary<string, object?> { ["OrderId"] = 42, ["ReadOnly"] = true })` passes values to this definition. Directive parameters become ordinary `[Parameter]` properties and use the existing reflection binder. A parameter without a default is required, including nullable parameters; explicit `0`, `false` and permitted `null` count as supplied values. Conventional `[Parameter]` properties remain optional unless marked `[EditorRequired]`. Missing required values, incompatible types and invalid nulls are rejected before construction. Names must be distinct ignoring case; `Window` is reserved. Use one `@window` per file, outside markup, never in `_Imports.nazor`. It cannot be combined with `@inherits`.

## Configuration and runtime changes

Omitted values use framework defaults (title Nazor, 800 by 600 logical client units, standard operations, system title bar). Dialogs default to disabled minimize, maximize and resize. Each window owns its configuration; secondary windows do not inherit the main window's geometry. `WindowOpenOptions` supplies owner, activation, positioning and size-to-content.

| `IWindow` setting | Contract |
| --- | --- |
| `Title` | Sets the initial native title and updates it while open; empty remains empty |
| `Width`, `Height` | Initial client size and runtime resize requests in logical units; non-normal states retain normal restore dimensions |
| `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight` | Nullable logical client size limits; null uses the system minimum or removes the maximum. Windows clamps interactive/programmatic resizing and maximization. Fullscreen fills the monitor and preserves constrained restore dimensions |
| `X`, `Y` | Nullable outer-frame coordinates in physical screen pixels, like AppWindow.Position. Negative coordinates allow monitors left/above the primary display. Assign before opening or while open; maximized/minimized/fullscreen windows update their restore position. Wayland does not support absolute screen positioning |
| `Operations` | Independent application rules for minimize, maximize, interactive resize and fullscreen. The individual properties read and write the same rules |
| `TitleBarMenuMode`, `Chrome` | Requested menu for title bar clicks handled by Nazor, and the effective owners of the frame, caption buttons and menu |
| `IsAlwaysOnTop` | Requests a position above non-topmost windows without taking focus; requires `Capabilities.CanSetAlwaysOnTop` |
| `IsVisibleInTaskbar` | Requests taskbar participation; requires `Capabilities.CanSetTaskbarVisibility` to change it |
| `Backdrop`, `AppliedBackdrop` | Requested selection and the actual backend result |
| `Icon`, `EffectiveIcon` | Optional icon override and resolved source for rendering |
| `Class`, `Style`, `StyleSheet` | Classes, inline styles and window-level stylesheet of the implicit root |
| `PlacementKey` | Configure before native creation; a different value is rejected after creation |

The host creates the retained window root and a vertical flex content area beneath the fixed caption. Use `:root` in NSS to style the window and configure root classes and styles through `IWindow`. Put application layout in your own `Container` and use `align-self: flex-start` for content that must keep its natural width. The native accessible name follows `Title`. Generic control attributes such as Id, TabIndex, tooltips, captured events and additional accessibility labels belong on content controls. Use `Activate()` for window focus and `KeyBindings` for window-wide shortcuts.

Changes run on the UI thread. The window component rerenders for presentation configuration, state and capability changes; caption measurements update the caption destination separately through `SystemButtonsChanged`. Ordinary child components can subscribe to `IWindow.Changed`, `StateChanged`, `CapabilitiesChanged` or `SystemButtonsChanged` and must unsubscribe on disposal. `IStyleSheet` keeps the Core contract independent from the style engine; supply a compiled `Nazor.Style.StyleSheet`. Replacing `Window.StyleSheet` schedules a frame so native appearance and frame input geometry update even when the component's content does not change.

`PlacementKey` stores geometry through `IWindowPlacementStore`. The default store uses `%LOCALAPPDATA%/<application>/Nazor/windows.json`. A valid saved placement overrides initial geometry; `SizeToContent` then adjusts height and recenters when applicable. Unavailable monitors trigger fallback positioning and, where available, reuse of logical client size. Register another store for application-specific persistence.

## Window commands and native support

Set `Operations` to change several rules together. Use `CanExecute` for commands in buttons and menus. Use `Request` to check the current rules and dispatch a command on the UI thread.

```csharp
Window.Operations = Window.Operations with { IsMaximizable = false };
var canEnter = Window.CanExecute(WindowCommand.EnterFullScreen);
var result = Window.Request(WindowCommand.EnterFullScreen);
```

`Dispatched` means the request was sent. Read `State` and observe `StateChanged` for the result reported by the backend. `Restore` and `ExitFullScreen` can leave an existing state even when the app no longer allows entering that state. If `CanObserveMinimized` is false, the backend does not report minimization as a window state. `CanRestoreFromMinimized` indicates whether the backend supports requesting that transition.

`Capabilities.Minimize`, `Maximize`, `InteractiveResize` and `FullScreen` describe backend support independently of `Operations`. Their `Restriction` describes how a disabled operation can be applied to standard native commands. `None` limits it to commands and UI owned by Nazor, `Hint` leaves the decision to the window manager, and `Enforced` applies it to the native commands controlled by the backend. Windows fullscreen is a Nazor command and uses `Operations.FullScreenEnabled`. The effective `CanMinimize`, `CanMaximize`, `CanResize` and `CanEnterFullScreen` flags combine support with the app rules. Observe `CapabilitiesChanged` when support or these effective flags change.

## Size limits and screen position

```csharp
protected override void OnInitialized()
{
    Window.MinWidth = 400;
    Window.MinHeight = 280;
    Window.MaxWidth = 900;
    Window.MaxHeight = 650;
    Window.X = 160;
    Window.Y = 120;
}
```

Limits use the same logical client units as `Width`/`Height`. Each is nullable: assign `null` to remove it. Explicit limits must be finite and between 1 and 32768, with minimum no larger than maximum. Resizing from code or user input is clamped; maximization keeps its normal maximize/restore state while stopping at the configured maximum dimensions. Fullscreen still fills the monitor, and resizing or moving while fullscreen updates the restore geometry. Limits changed on a maximized window take effect immediately. The Windows backend implements these constraints; other backends reject non-null limits until they support them.

`X`/`Y` use physical screen pixels and the outside edge of the frame, matching `AppWindow.Move`. An omitted axis keeps the system/default coordinate; a runtime assignment preserves the other axis. Explicit startup coordinates override restored placement on that axis, and size limits constrain restored dimensions. Coordinates can be negative for displays to the left/above the primary monitor. Check `Capabilities.CanSetScreenPosition` before setting `X` or `Y`. The Linux backends do not expose absolute positioning. `WindowOpenOptions.SizeToContent` and centering run after initial placement, as before.

## Per-window icons

```razor
@window
<TextBlock>Tools</TextBlock>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "Tools";
        Window.Icon = "Assets/tools.svg";
    }
}
```

`IWindow.Icon` accepts `ImageSource`: a file path string, embedded resource, bytes, stream, or URI.
Assign `Window.Icon = "Assets/tools.svg"` using the implicit string conversion, or supply an `ImageSource`.
The same raster and SVG decoder used by images is used for window icons, including PNG and ICO.
File and resource sources select display-scale variants. Icons retain their aspect ratio inside a transparent square.
Loading is asynchronous; until a replacement succeeds, the last applied icon remains visible. Loading or decoding
failures use the window's normal exception reporting. Replacing an icon or closing the window cancels pending work.

Omitting the override, or assigning null, uses the application's icon. `IWindow.EffectiveIcon` returns the override or a backend-resolved application source; it may be null when unavailable. On Windows the default source comes from the executable icon resource. On Linux desktop the backend embeds the blue Nazor application tile, so the standard Nazor title bar displays it automatically without a separate asset file or desktop entry. Set `Window.Icon` to replace it; assigning null restores the default. This client title-bar image is available even when the compositor cannot accept native window icons. An unrelated render preserves the assigned override. Icons are independent of other windows and owners.
Check `IWindow.Capabilities.CanSetWindowIcon`: Windows implements this feature, and Wayland uses the compositor icon protocol when advertised; unsupported backends
retain the request without loading it or changing native decoration. Support and placement depend on the backend;
the contract does not promise a window-specific Dock or launcher icon on every platform.

With a custom title bar, the native icon still identifies the window to Windows, but the title bar's content must
include its own `Image` or `Icon` control if it should display that artwork.
The executable's icon remains configured separately with `ApplicationIcon` in the project file.

## Open a window

```razor
@inject IWindowService Windows
@inject IWindow CurrentWindow

<Button OnClick="OpenDetails">Open details</Button>

@code {
    private void OpenDetails() => Windows.Open<OrderDetails>(
        new Dictionary<string, object?> { ["OrderId"] = 42 },
        new WindowOpenOptions
        {
            Owner = CurrentWindow,
            StartupLocation = WindowStartupLocation.CenterOwner,
        });
}
```

The component type and its parameters are application-defined. Each new window owns a renderer, scene, root component, and DI scope. Ownership keeps a tool window associated with its owner without making it modal. All windows share the application's UI thread.

`IWindow.Show()`, `Hide()`, `Activate()`, and `Close()` act on the native window. Hide retains its scope and component state; Close disposes them. Observe `Closed`, release subscriptions, and stop reusing closed instances. Closing is currently not cancelable.

## Window-local dialogs

`Dialog` renders within the current native window. Its default `Size="DialogSize.Auto"` follows the active theme. Fluent measures centered content between 320 and 548 logical units wide and between 184 and 756 high, constrained by a 24-unit window inset. The body scrolls while header and actions stay in place. Basic and NazorLeaf keep their ordinary widths. Select `Small`, `Medium` or `Large` for an explicit theme width; fullscreen and edge placement remain available.

Choose the Enter/accent action explicitly with `DefaultButton="DialogButtonRole.Primary"`, `Secondary` or `Cancel`. The default null selects none. Body controls receive initial focus; without them, focus prefers the enabled default action and then the first footer control. A focused button still activates on Enter. Multiline editors and content that handles the key suppress the default action.

`DialogButton.IsDefault` overrides that button's role policy. To move a Primary default to an explicit Secondary button, set Primary's `IsDefault="false"` and Secondary's `IsDefault="true"`; a dialog rejects multiple effective defaults. `AlertDialog`, `Drawer` and other `DialogBase` controls retain their Primary policy. Fluent orders Primary/Destructive before Secondary and Cancel, preserves Help at the start, and equally sizes automatic dialog actions except Help.

Migration: an existing generic dialog that relied on an implicit Primary default must set `DefaultButton="DialogButtonRole.Primary"`. Set `Size="DialogSize.Medium"` to preserve its former explicit ordinary width. These changes concern the window-local control; native window decorations and `ShowDialogAsync` sizing are configured separately.

## Modal dialogs and results

From a component with `IWindowService Windows`:

```csharp
var result = await Windows.ShowDialogAsync<NameDialog, string>(
    new Dictionary<string, object?> { ["Name"] = currentName });
if (result is not null) currentName = result;
```

The dialog uses `IWindow.Close(result)` to complete the task. Escape or native close returns no result. The typed overload returns the result type's default if the value is absent or incompatible; prefer a nullable/reference result when cancellation must be distinguishable.

Create `NameDialog.nazor`:

```razor
@window
@inject IWindow CurrentWindow

<TextField @bind-Value="Name" AccessibleName="Name" />
<Button IsDefault="true" OnClick="() => CurrentWindow.Close(Name)">OK</Button>
<Button OnClick="() => CurrentWindow.Close()">Cancel</Button>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "Rename";
        Window.Width = 360;
        Window.Height = 200;
    }

    [Parameter] public string Name { get; set; } = "";
}
```

Only the owner is disabled while the modal window is open. Nazor does not run a nested application message loop for component dialogs; other windows and asynchronous work continue. Dialog defaults disable minimize, maximize, and resize, with fullscreen allowed unless the window definition overrides it.

`ShowMessageAsync(message, title, buttons, icon)` supplies a themed confirmation window. It returns `MessageBoxResult`. The first button is the default; labels follow UI culture. Handle cancellation or `None` explicitly rather than interpreting every non-Yes result as success.

## System file dialogs

Inject `IFileDialogService`. `OpenFileAsync`, `SaveFileAsync`, and `PickFolderAsync` return a selected path or null on cancellation. `OpenFilesAsync` returns the selected paths, or an empty list on cancellation. Options include title, initial directory, filename, extension, and filters:

```csharp
var path = await Files.SaveFileAsync(new SaveFileOptions
{
    FileName = "orders",
    DefaultExtension = "csv",
    Filters = [new FileFilter("CSV files", "*.csv")],
});
if (path is not null) await ExportAsync(path);
```

`Files` and `ExportAsync` belong to the surrounding component. The save dialog requests overwrite confirmation. A selected path is not an open stream; handle application I/O errors normally. These are native OS dialogs, so their appearance and language follow the system.

Example: [Windowing](../../Samples/Nazor.Samples.Windowing/README.md). API: [Nazor.Application](../ApiReferences/Nazor.Application.md), [Nazor.Core](../ApiReferences/Nazor.Core.md).

---

[Guide index](README.md) · [API reference](../API.md)

## Client frames, topmost and taskbar presentation

Configure a splash in `OnInitialized`, before the native window is created:

```csharp
Window.Decorations = WindowDecorationPolicy.Require(
    WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None));
Window.Width = 420;
Window.Height = 240;
Window.IsMinimizable = false;
Window.IsMaximizable = false;
Window.IsResizable = false;
Window.FullScreenEnabled = false;
if (Window.Capabilities.CanSetAlwaysOnTop) Window.IsAlwaysOnTop = true;
if (Window.Capabilities.CanSetTaskbarVisibility) Window.IsVisibleInTaskbar = false;
```

The external frame belongs either to the desktop or to Nazor. A Nazor frame without a title bar is suitable for a splash. Set `IsResizable = false` for a fixed canvas; the frame owner does not disable window operations. Root NSS can draw an outline, radius and shadow on supported backends. These visual effects do not themselves enable input regions.

Set `Window.Decorations` during initialization. An open window changes through `await Window.SetDecorationsAsync(policy)`. `Require` accepts one complete configuration; `Prefer` tries its explicit preferences, then complete native and Nazor defaults, keeping only the first occurrence of each configuration. Desktop defaults prefer the complete native configuration and then the complete Nazor configuration. User content is selected only explicitly. `AppliedDecorations` is null while initial negotiation is pending and then identifies the accepted configuration and option index. `Window.Chrome` describes the actual current owners; fullscreen can temporarily hide them without changing the accepted configuration.

`Window.Capabilities.Decorations.GetSupport(configuration)` checks an opening configuration, and `GetChangeSupport(configuration)` additionally checks the transition from the accepted configuration. Unknown support requires negotiation. Failure of all authorized options throws `PlatformNotSupportedException`. Runtime rejection retains the previous policy and restores its configuration when possible. Changes run on the UI thread and preserve native identity, root content, services and placement. Cancellation before dispatch prevents the request; dispatched native negotiation settles before completion. Single-view hosts do not accept desktop decoration policies.

`IsAlwaysOnTop` defaults to false and places Windows windows above non-topmost windows without taking focus. It is independent of frame ownership and can change at runtime. Other topmost windows may cover it. Wayland's standard xdg-shell backend reports `CanSetAlwaysOnTop = false`.

`IsVisibleInTaskbar` defaults to true and requests taskbar participation. On Windows, false applies the tool-window style and also excludes Alt+Tab; changing it while visible requires an internal hide/show. Owned windows still follow shell ownership conventions. Hidden windows remain hidden, and taskbar-hidden windows are not badge targets. Wayland reports `CanSetTaskbarVisibility = false`. Unsupported non-default presentation requests throw `PlatformNotSupportedException` without changing the property.

`CapabilitiesChanged` fires only after the effective `WindowCapabilities` value changes. `Changed` covers presentation changes such as topmost/taskbar settings; setting topmost does not alter whether the backend supports topmost. Query `Capabilities.Decorations.GetSupport` for a complete decoration configuration.

See the [splash preview](../../Samples/Nazor.Samples.Windowing/SplashWindow.nazor).

## Native window interop

Read `IWindow.NativeHandle` on the UI dispatcher after native creation (for example, from a user event). It returns a nullable `NativeWindowHandle` with `Handle` and `Descriptor`. Windows supplies `HWND`, X11 supplies the top-level window XID with descriptor `X11`, and Wayland supplies a process-local `wl_surface` pointer. Linux never exposes Nazor's internal window pointer through this property. Unsupported hosts, including headless and DRM single-view hosts, return null, as do windows before native creation and after close.

```csharp
if (Window.NativeHandle is { Descriptor: "HWND" } native)
{
    UseWindowsInterop(native.Handle);
}
```

The reference is borrowed: Nazor owns it. Use only matching platform APIs, never destroy or release the handle, and stop using saved values when the window closes. A Wayland pointer cannot be passed to another process. Other platform backends may define further descriptors; branch on the descriptor instead of assuming the current OS identifies a windowing backend.

The implicit content viewport scrolls automatically when content exceeds the available area; configure `WindowContent` through NSS as described in [scrolling](20-Layout-and-Scrolling.md#scrolling).
