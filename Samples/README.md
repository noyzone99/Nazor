# Learn Nazor through samples

Each project is a runnable example with explanations in the source. Start with Counter, then choose the topic you need.
The samples start in English, including built-in control labels and formatting. Localization also accepts an explicit culture.

| Sample | What you will learn | Suggested starting point |
| --- | --- | --- |
| [Counter](Nazor.Samples.Counter/README.md) | Component state, event callbacks, scoped NSS, and headless rendering. | `Counter.nazor`, then `Headless.cs`. |
| [DRM](Nazor.Samples.Drm/README.md) | Reuse content in a desktop window or a Linux DRM/KMS single view. | `Program.cs`, then `App.nazor`. |
| [Tasks](Nazor.Samples.Tasks/README.md) | Hosting, DI lifetimes, reusable components, templates, keys, keyboard input, and a scrollbar/smooth-scrolling playground. | `Program.cs`, `Views/TasksView.nazor`, then `Views/ScrollbarsView.nazor`. |
| [Gallery](Nazor.Samples.Gallery/README.md) | Searchable control catalog with compiled example sources, live theme switching and an interactive NazorLeaf showcase. | `Catalog/`, `Examples/`, `Showcase/`. |
| [Theming](Nazor.Samples.Theming/README.md) | Complete themes, color schemes, system colors, and backdrop requests. | `Program.cs`, then `App.nazor.nss`. |
| [Localization](Nazor.Samples.Localization/README.md) | Resource lookup, culture-aware formatting, and language fallback. | `Program.cs`, then `App.resx`. |
| [Windowing](Nazor.Samples.Windowing/README.md) | Window commands and capabilities, dialogs, system tray, notifications, and launcher integration. | `Program.cs`, then `App.nazor`. |

Run commands from the repository root, for example:

```powershell
dotnet run --project Samples/Nazor.Samples.Counter
```

The graphical examples target `net10.0` and use `NazorApplication.CreateBuilder(args)`. They import the local `Nazor.Sdk` props and targets, which automatically reference the runtime and desktop host from source and supply the same implicit usings and bootstrap as the packaged SDK; the registered desktop host selects the native backend at runtime. Windows and Linux Wayland/X11 are implemented; see the [backend support boundaries](../Docs/Guide/32-Support-and-Limitations.md). Counter explicitly uses builder.Application.CreateMainWindow for its reusable content and can also render without a native host. The other graphical samples select builder.Application.UseMainWindow; Tray registers builder.Application.UseTrayItem<TraySession>() with explicit shutdown. Appearance belongs to the built application.
Each executable declares a stable `NazorApplicationId`, used by the Linux and unpackaged Windows backends without changing assembly names. The Windowing sample also selects an optional `NazorLinuxDesktopEntry`; the SDK copies it unchanged for Linux and portable output.

The shared SDK targets compile `.nazor` components and `.nss` stylesheets and supply the shared SDK Windows DPI manifest. No sample needs a local manifest or a reference to another sample's manifest; see [manifest customization](../Docs/Guide/03-Projects-and-Builds.md#windows-application-manifest).
Edit the source files, not the generated C# under `obj/`.

Run `pwsh -NoProfile -File Scripts/Tests/verify-samples.ps1` to build the samples, verify their shared manifest, and check native startup through the development channel. The script closes the applications it starts; the tray application is stopped after its UI loop responds. See the [script reference](../Scripts/README.md).

The DRM sample uses `CreateMainWindow` for desktop development and explicitly selects `DrmPlatformHost` with `UseMainView` when passed `--drm`. See its [startup and dependency guide](../Docs/Guide/34-Linux-DRM.md).

The comments explain why each example uses a particular pattern; they are not requirements to copy every pattern into every app.
For the complete learning path, use the [developer guide](../Docs/Guide/README.md).
For signatures and contracts, use the [package API references](../Docs/API.md).
Projects used only as test fixtures live under `Tests/Fixtures/Projects`.

Samples retain their .NET 10 default. For a .NET 11 application using the same components, create a project with `dotnet new nazor-app -n MySample11 --framework net11.0`, then copy the relevant sample components and styles into it. Set `TargetFramework` in the application project; the source Razor/NSS compiler and inspector use .NET 10 LTS, independently of the application target. See [application framework selection](../Docs/Guide/03-Projects-and-Builds.md#net-11-applications).
