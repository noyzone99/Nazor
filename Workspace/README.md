# Development workspace

Local application for interactive experiments against the framework source. Use this area to reproduce behavior, inspect controls and try component or styling changes during development. Teaching examples live in [Samples](../Samples/README.md); repeatable regression cases belong in [Tests](../Tests/README.md).

## Directory layout

| Directory | Contents |
| --- | --- |
| [Nazor.Workspace](Nazor.Workspace) | Desktop executable project using the local Nazor SDK, which resolves the runtime and desktop host from source. `Program.cs` configures startup, `App.nazor` defines the experimental UI, `_Imports.nazor` supplies shared imports, and `Components/` holds local reusable components. |

Run from the repository root:

```powershell
dotnet run --project Workspace/Nazor.Workspace
```

The workspace is included in [Nazor.slnx](../Nazor.slnx). It uses the local `Nazor.Sdk` props and targets and starts with `NazorApplication.CreateBuilder(args)`, `builder.Application.UseMainWindow<App>()`, and awaited `app.RunAsync()`. Its window markup owns title and dimensions; the built application configures the Basic theme and initial dark color scheme through `app.Appearance` before startup. The application is disposed asynchronously after shutdown. The imported targets generate a bootstrap that registers `DesktopPlatformHost`, which selects the native backend when run. The same targets supply the shared SDK Windows DPI manifest without a local copy or an explicit `ApplicationManifest` property. See [manifest customization](../Docs/Guide/03-Projects-and-Builds.md#windows-application-manifest) for overrides. The desktop host selects the supported Windows or Linux backend; no manual `PlatformHost` assignment is needed. Generated component sources and the bootstrap are written under `obj/`; runtime binaries are written under `bin/`.

The experimental UI is a popup laboratory. Shared component imports live in `_Imports.nazor`. The SDK supplies the same C# global usings as packaged applications, including the `Color` alias for `Nazor.Style.Color`. The project inherits the repository's nullable checks.

## Try desktop popups

Set `Nazor.Workspace` as the Visual Studio startup project and press F5, or use the command above. The window starts with external popups requested. The capability line reports whether the active backend can provide them, and the External popups checkbox switches the setting live. Windows and Wayland support this path. X11 requires a compositing manager and an ARGB visual.

1. Open the bottom grouped Select with `Auto` placement and six preferred rows. Toggle External popups and compare its position against the window edge. Resize or move the window near the monitor bottom to compare window space with display work-area space.
2. Choose `Top` or `Bottom` and change the preferred counts. Use Compact / large window to reduce the space. The count preferences yield to available space, and remaining options scroll. Vienna is disabled and group headings consume space alongside the options.
3. Type `Rome` in the bottom ComboBox. Check filtering, editor focus, ArrowDown/Enter selection and Escape dismissal. The resolved side stays stable while it remains usable.
4. Right-click near the context target's right edge and open More commands. Test arrow keys, nested Escape dismissal, an outside click and moving the owner. Menus follow their normal light-dismiss policy.
5. Open the persistent popover, then drag the title bar or resize the window. Close it with its close button. Its outside-dismiss policy is disabled to allow this experiment.
6. Switch Light / dark to inspect the NSS borders, rounded corners and shadows on each surface.

The placement and count selectors also use the shared popup implementation. See the [popup guide](../Docs/Guide/11-Commands-Menus-and-Popups.md#popup-surfaces-outside-the-window).

The Visual Studio extension bundles the language server and control metadata. Rebuild it after framework API changes with `Scripts/Build/build-editors.ps1`. Close Visual Studio before installing the resulting `.vsix`, then reopen the solution. The popup laboratory is supplied with extension version 0.9.52.

Run `pwsh -NoProfile -File Scripts/Tests/verify-samples.ps1` to verify native startup alongside all samples. Use [SDK verification](../Scripts/README.md) to validate packaged applications.

## Inspect from Visual Studio

Set Nazor.Workspace as startup project and run Debug. Its project enables NazorEnableDevTools only in Debug; Program.cs calls builder.UseDevTools() under NAZOR_DEVTOOLS through the ordinary Nazor namespace, without a conditional using. No instrumentation code belongs in App.nazor. Every opened window receives a separate optional Agent with window-owned cleanup.

Start the standalone inspector from another terminal with `dotnet run --project Sources/Tools/Nazor.DevTools -c Release`, then choose Discover, the workspace window and Connect. Close an older source-built inspector before rebuilding its output. See [DevTools setup and panels](../Docs/DevTools.md) for capabilities and retained-data behavior. The Program.cs call starts Agent attachment only; launching the graphical inspector and current-stop IDE reads remain separate.
