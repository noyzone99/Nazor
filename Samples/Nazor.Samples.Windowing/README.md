# Windowing and desktop integration

## Desktop services

The main window has three tabs. **Windows** exercises window commands, title bars, dialogs, and auxiliary windows. **Capabilities** reports the actual backend and renderer, current window properties, and every `WindowCapabilities` value. **Desktop services** reports tray host support and icon visibility separately, and lets you send or withdraw a notification and exercise launcher features.

`TraySession` owns the tray icon, notification text, pause state, and severity across windows. The tray menu or a notification activation can reopen the main window. Start with `--tray` to launch without a window when the desktop accepts the icon. If the tray is unavailable, the sample opens a window. Closing the main window keeps the process alive while the icon is visible. Use **Exit application** or **Exit** in the tray menu to close the process and its windows.

The project declares `NazorApplicationId=org.nazor.samples.windowing` and selects its desktop entry with `NazorLinuxDesktopEntry`. On Linux, install the published `org.nazor.samples.windowing.desktop` entry and its icon to associate the app with the same identity in notifications, the shell, and the taskbar. The icon asset is `Assets/launcher.png`. Set the entry's `Exec` and `Icon` to their installed locations or make them discoverable on PATH and in the icon theme. See the [Linux desktop identity guide](../../Docs/Guide/15-Tray-and-System-Notifications.md).

## Application launcher

Try Jump List and badge actions in **Desktop services**. Use **Open launcher example with native badge styles** or launch with `--launcher` to publish from a window whose root demonstrates `-windows-badge-*` NSS styles with a gradient background, a bundled font, independent border widths, rotation and an SVG alpha mask. Controls follow the singleton `IApplicationLauncher` capabilities. Count and icon share one badge slot across the process. See the [badge property reference](../../Docs/Guide/19-NSS-Property-Reference.md#windows-launcher-badges).

On Windows, publish the launcher tasks and invoke one from the taskbar menu to open the selected sample mode. The tasks persist after exit. Use **Remove launcher tasks** to remove them. Exercise count-to-icon replacement, clearing, opening and closing independent windows, mixed DPI, and Explorer restart.

Linux numeric badges require a compatible launcher and an installed `org.nazor.samples.windowing.desktop` entry. Jump List and icon capabilities are false; configure launcher actions manually in that file. See the [launcher guide](../../Docs/Guide/15-Tray-and-System-Notifications.md#application-launcher-jump-lists-and-badges) for support and acceptance semantics.

## Run the sample

```powershell
dotnet run --project Samples/Nazor.Samples.Windowing
dotnet run --project Samples/Nazor.Samples.Windowing -- --system-titlebar
dotnet run --project Samples/Nazor.Samples.Windowing -- --tray
```

Start with `Program.cs`, then read the commands in `App.nazor`. Each native definition starts with `@window` and configures its associated `Window` in OnInitialized. The main window demonstrates runtime system/custom frame switching, requested operations,
native and app-drawn caption controls, independent windows, an owned modeless tool window, and dialogs returning values.
Each tab scrolls on small windows. Use **Refresh values** in Capabilities to read the current size and scale after moving or resizing the window.

The **Native title bar colors and size limits** command opens [AppearanceWindow.nazor](AppearanceWindow.nazor) on Windows. It demonstrates all twelve AppWindowTitleBar colors through `-windows-title-bar-*` NSS properties, logical client `MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight`, constrained maximization, and `X`/`Y` screen-pixel positioning. Native colors require Windows 11. Set a color to `auto`, or a size limit to `null`, to restore its default.

## Keep requested operations separate from effective capabilities

The checkboxes set the four independent flags in `Window.Operations`. The Capabilities tab shows backend support, app policy, effective capability, and native restriction for each operation. Command buttons call `Request` and display its result alongside the window state reported by the backend.

Cycle **Title bar menu request** through System, Nazor and None. In custom title bar mode, right-click the drag area after disabling Minimize, Maximize or Full screen. The Nazor menu uses the same policy as the app buttons. The readout shows the effective menu owner. If the desktop owns the frame and handles the click itself, its menu remains available under its own rules.

`TitleBar` is declared in each window file and reserves measured native control insets at the fixed top destination. `WindowTitleBar.nazor` supplies reusable contents inside those declarations; it is an ordinary component. System mode suppresses the custom contents automatically.
When app-drawn buttons are selected, `CaptionButtons` renders `SystemButtonsLayout.Left` and `.Right` separately,
preserving physical left-to-right order on each side instead of assuming all controls belong on the right.
`CloseOnly` filters these lists without moving their remaining controls.

The maximize/restore button supplies the native Snap Layouts hit area. Each caption button has a localized accessible name. Restore remains available if a maximized window later loses permission to maximize again.
The full-screen command stays in the content because the title bar is hidden in full screen.
Standard/Tall compares native caption heights with the equivalent custom title bar.
The root `Window` enables the client grip with `-window-resize-grip: auto`. The main window uses the accent color for the grip, and the effective resize capability controls when it appears. See the [window frame properties](../../Docs/Guide/19-NSS-Property-Reference.md#window-frame-and-resize).

## Choose ownership and modality deliberately

An independent `Open<App>` call omits Owner and supplies its own initial size and centering.
The tool window sets `Owner = CurrentWindow`, staying above its owner without disabling it.
Hide and Show keep its component state and notes; Close disposes them, and reopening creates a fresh instance.
The tool window assigns `Window.Icon = "Assets/tools.svg"`. On Windows this supplies its own native window icon;
other windows keep the application icon. A custom title bar can display `Window.EffectiveIcon` separately in its content.

Both dialog variants use `NameDialog`, whose `@window (bool CustomTitlebar = false, string Name = "")` generates ordinary Parameter properties backed by the existing reflection binder. `ShowDialogAsync<NameDialog, string>` disables the owner while awaiting `Close(result)`.
`nameof` keys connect initial parameters to the component's contract. Confirm returns trimmed text;
Cancel, Esc, or closing without a result leaves the original name unchanged.
`SizeToContent` adapts dialog height, and dialog defaults prevent minimize, maximize, and interactive resize.
The message command separately demonstrates `ShowMessageAsync` and its standard result enumeration.

Light/Dark/System changes the application-wide `IApplicationAppearance.ColorScheme`, including windows and dialogs already open.

## Deploy private fonts with the app

The project links Fira Sans regular and bold from `Tests/Assets/Fonts`, including their SIL OFL license,
and copies them to both build and publish output. This reuses repository assets for the demonstration;
a standalone app should include its own licensed `.ttf` or `.otf` files under `Fonts/`.
Nazor's SDK copies project-local font assets automatically. For an external linked file, declare the copy behavior explicitly:

```xml
<None Include="../shared/FiraSans-Regular.ttf" Link="Fonts/FiraSans-Regular.ttf"
      CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
```

Register faces before Build and Run:

```csharp
builder.Fonts.Add("Fonts/FiraSans-Regular.ttf", "Fonts/FiraSans-Bold.ttf");
```

Paths resolve from `AppContext.BaseDirectory`, not the current working directory.
NSS uses the font's internal family name, `Fira Sans`, and chooses weight among the registered faces before falling back to system fonts.
The sample includes regular and bold, not an italic face, and does not use `@font-face`.
The framework owns the font resources; an unreadable registered file reports its path and stops startup.

Try native and custom controls, Snap, full screen, capability changes while maximized, both dialog variants,
tool-window hide/show, theme changes, DPI changes, and screen-reader navigation.
For deployment, publish and verify that both font files and `Fonts/OFL.txt` are present:

```powershell
dotnet publish Samples/Nazor.Samples.Windowing -c Release -o artifacts/windowing
```

See [window options](../../Docs/Guide/13-Windows-and-Dialogs.md) and [themes](../../Docs/Guide/16-Styling-and-Themes.md)
for the supporting contracts.

The splash commands open `SplashWindow` with an opaque or translucent client surface, depending on the command and backend support. The preview selects a Nazor frame without a title bar or caption buttons, sets `IsResizable = false`, and configures `BackgroundMode` before native creation, disables window operations, and requests topmost and taskbar exclusion when supported. The main window also exposes runtime topmost and taskbar toggles.
