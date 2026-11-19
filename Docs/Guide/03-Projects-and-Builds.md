# 3. Projects and builds

## Application project

A package consumer uses the Nazor SDK with a neutral desktop target:

```xml
<Project Sdk="Nazor.Sdk/0.1.0-alpha">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <NazorApplicationId>org.example.myapp</NazorApplicationId>
  </PropertyGroup>
</Project>
```

The SDK imports `Microsoft.NET.Sdk`, enables nullable reference types and implicit usings when unspecified, and defaults both applications and libraries to `net10.0`. Defaults are applied after reading the project and do not override an explicit `TargetFramework` or `TargetFrameworks`. Desktop applications do not need multitargeting to select an operating-system backend.

Every target receives `Nazor.Application` at the SDK's version. Its transitive dependencies provide the base controls, styles, input, graphics, layout, renderer and other runtime services. Executables (`Exe` or `WinExe`) also receive a desktop hosting package selected by `RuntimeIdentifier`: `Nazor.Hosting.Desktop` without a RID, `Nazor.Hosting.Desktop.Windows` for `win-*`, or `Nazor.Hosting.Desktop.Linux` for `linux-*`. All three supply the same `Nazor.Hosting.Desktop.dll` and `DesktopPlatformHost` API; only one belongs in an application. Libraries receive no automatic desktop or Windows host, even when they explicitly target Windows. The renderer supplies native assets for both platforms in portable publications and only the target RID in specialized publications. Platform availability and verification limits are described in [Linux Wayland](33-Linux-Wayland.md).

The implicit C# usings are `Nazor`, `Nazor.Controls`, `Nazor.Input`, `Nazor.Style`, `Nazor.Graphics`, `Nazor.Accessibility`, `Nazor.Themes`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Localization`, `System.Globalization`, `System.Text.Json`, `System.Text.Json.Serialization`, and `System.Net.Http.Json`. They apply to all C# files in the consuming project, including code-behind. `Color` is an alias for `Nazor.Style.Color`; use `Nazor.Graphics.Color` explicitly for graphics operations. Set `ImplicitUsings` to `disable` to write explicit imports instead. Specialized namespaces such as `Nazor.Controls.Primitives` remain explicit.

In top-level startup code, qualify application stylesheets as `MyApp.StyleSheets.Theme` (using the actual root namespace), as the template does. The controls package also exposes a `StyleSheets` class, so importing both namespaces can make an unqualified name ambiguous. A C# using alias is another option.

Keep the SDK, official runtime packages, and optional themes on compatible versions.

The SDK supplies the `Nazor.Controls` package and imports its component namespace. Add `@using Nazor.Controls.Primitives` in markup that uses lower-level interaction components. See the [control catalog](09-Control-Catalog.md).

Samples and the development workspace import the local SDK directly:

```xml
<Project>
  <Import Project="../../Sources/Tools/Nazor.Sdk/Sdk/Sdk.props" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <NazorApplicationId>org.example.myapp</NazorApplicationId>
  </PropertyGroup>
  <Import Project="../../Sources/Tools/Nazor.Sdk/Sdk/Sdk.targets" />
</Project>
```

The SDK detects its source-tree layout and substitutes source project references for its automatic Nazor packages. It also builds the local compiler and resolves the repository icon and optional inspector assets. SDK defaults, implicit usings, component generation, bootstrap, validation and RID selection use the same files as packaged applications; no pack or publication step is needed. Optional themes remain explicit project references. External applications use `Sdk="Nazor.Sdk/0.1.0-alpha"`.

### Windows dependencies

For Windows or portable executables, Nazor.Sdk restores the original `Microsoft.WindowsAppSDK.InteractiveExperiences` and `Microsoft.Windows.SDK.NET.Ref` packages from NuGet.org and selects the required Windows assets. If you use package source mapping, allow both IDs from NuGet.org. Use Nazor.Sdk even when configuring the Windows host explicitly. If you set `NazorImplicitRuntimeReference=false` and reference a Windows-capable host, also set `NazorRestoreWindowsDependencies=true` in the executable. Linux-only executables skip this restore.

## Build tools and application targets

Nazor.Sdk bundles one .NET 10 LTS compiler for both `.nazor` components and `.nss` styles. It runs separately from applications targeting .NET 8 or later, including .NET 9, 10 and 11. Install the .NET and ASP.NET Core 10 runtimes on development/build machines; the .NET 10 SDK includes them. The language server and DevTools inspector also run on .NET 10.

The compiler generates code against the consuming project's references. An application's target controls its runtime libraries and deployment requirements, independently of the compiler runtime. NuGet selects the matching Nazor runtime assemblies and [framework-specific dependencies](28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions). Deployment needs only the application's selected runtime, or a self-contained publication; compiler and NSS tooling are not distributed with the application.

Existing development environments must install the .NET/ASP.NET Core 10 runtimes when updating this SDK. Direct compiler integrations must use `tools/net10.0/Nazor.Compiler.dll` in the SDK package, or the `NazorCompilerPath` MSBuild property. The SDK resolves this path automatically for ordinary projects.

The bundled Roslyn/Razor services support C# 15 while running on .NET 10. Component generation, watch and both IDE clients use the application's evaluated `LangVersion` and `DefineConstants`, together with its resolved reference assemblies. SDK defaults correspond to C# 12/13/14/15 for .NET 8/9/10/11. An explicit older `LangVersion` retains its language restrictions; preview syntax requires `LangVersion=preview` and is limited to the bundled compiler's capabilities. Rebuild after changing project options so the editor receives evaluated inputs. Before the first build, the editor uses literal target/language properties as a fallback; imported or conditional properties require evaluated build inputs.

## Optional inspection in development

`NazorEnableDevTools` defaults to false independently of configuration. Set it to true in an executable project or pass `-p:NazorEnableDevTools=true` to include the optional Agent and define `NAZOR_DEVTOOLS`. Guard application attachment code with that symbol; inclusion alone does not attach a session. This also works in optimized Release builds. Release publication enables `NazorEnablePublishValidation=true` by default; publishing with DevTools additionally requires an explicit `-p:NazorEnablePublishValidation=false`. Debug and other configurations default validation to false. Source applications importing `build/Nazor.targets` follow the same policy. See [window inspection](27-Testing-and-Diagnostics.md#window-inspection) for setup and [production publication](28-Libraries-Packaging-and-Deployment.md#production-without-devtools) for dependency and payload checks.

## Visual Studio 2022 and .NET 8

Runtime packages and the optional Fluent theme contain `net8.0`, `net9.0` and `net10.0` assemblies. Create a .NET 8 application with:

```powershell
dotnet new nazor-app -n HelloNazor8 --Framework net8.0
```

Use `--Framework net9.0` for a .NET 9 application with the same VS2022 setup. The packaged compiler, language server and DevTools still use .NET 10 LTS. Contributors can verify that variant with `verify-net8.ps1 -Framework net9.0 -Workspace <fresh-path>`.

Use Visual Studio 2022 17.14 with the .NET 9 SDK, the application's .NET 8 or .NET 9 runtime, and the .NET/ASP.NET Core 10 runtimes for the compiler and language server. Build tools run separately on .NET 10 LTS; deployment needs only the application's runtime. Keep the default `net10.0` template for Visual Studio 2026.

Select an SDK compatible with VS2022 by placing this `global.json` beside your application's solution:

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

This selects an installed .NET 9 SDK without selecting .NET 10. An application project uses `<TargetFramework>net8.0</TargetFramework>`; the rest of its SDK configuration and startup code are unchanged. Build once after installing [editor support](26-Editors-and-Hot-Reload.md), then open the solution in VS2022 and press F5. Interactive debugger and Hot Reload behavior still require IDE validation.

The framework repository itself uses the .NET 10.0.404 SDK pin to build its compilers and language server. It is not a VS2022 application solution. Contributors can run `pwsh -NoProfile -File ./Scripts/Tests/verify-net8.ps1` to pack the framework, create an isolated application solution under `artifacts/net8-vs2022`, and verify VS2022 build/design-time inputs, native startup, the development channel and UI Automation. The script preserves that solution for opening in VS2022 and refuses to overwrite an existing workspace.

## Target frameworks and native hosts

`NazorApplication.CreateBuilder(args)` receives `DesktopPlatformHost` from the generated bootstrap. Without a `RuntimeIdentifier`, the application includes Windows and Linux Wayland/X11 backends and selects one at startup. With a Windows or Linux RID, only that platform's host and dependencies are included. The host still checks the running operating system before delegating native initialization and UI-thread startup. Windows selects `WindowsPlatformHost`; Linux selects `LinuxPlatformHost` for a Wayland or X11 connection. The target framework and build machine do not select the backend. Use `NazorLinuxNativeLibraries=All|Wayland|X11|None` to select the distributed Linux bridges, including for portable applications. Windows-only publications ignore this property. DRM hosting remains an explicit application choice. See [X11](35-Linux-X11.md).

A RID such as `win-x64` or `linux-x64` selects both the managed backend and applicable native assets. This works with `dotnet publish -r`, a project property, or a Visual Studio publish profile. `SelfContained=false` does not imply portable deployment: portability here means omitting the RID. Unsupported desktop RIDs such as `osx-arm64` fail with `NZB003`; portable applications running on an unsupported OS fail with `PlatformNotSupportedException`. A single desktop project keeps its  .NET 8-or-later target while producing separate deployment outputs. Windows x64 is the established deployment baseline. Linux has glibc/musl x64/ARM64 native assets and automated headless Wayland/X11 validation; ARM64 emulation and physical hardware verification are distinguished in the Linux guides. See the Linux guide.

```powershell
# Portable: both implemented backends and their native assets.
dotnet publish HelloNazor.csproj -c Release --self-contained false
# Specialized: Linux backend, without Windows hosting or WinRT assemblies.
dotnet publish HelloNazor.csproj -c Release -r linux-x64 --self-contained false
```

Use a separate output directory for each publication. Publishing into an existing folder does not remove stale files from a previous portable or different-RID publication. Changes to backend selection require a normal restore and build; `--no-build` cannot specialize an existing portable assembly.

Use a Windows-specific TFM such as `net10.0-windows` only when your own code requires its platform API surface. The SDK still selects desktop hosting by RID; this optional TFM does not select a different Nazor backend. Explicit library multitargeting remains possible, but is not required for ordinary desktop or shared component projects.

Mobile is a separate hosting concern. Future Android and iOS applications require dedicated entry projects, platform workloads, lifecycle integration and hosts, referencing shared components in a neutral project. Those hosts are not implemented. With automatic runtime references enabled, unsupported platform-specific targets such as `net10.0-android` and `net10.0-ios` produce `NZB002`. A platform workload can report its own validation error first. Changing the desktop RID does not create a mobile application.

## Automatic platform bootstrap

For a desktop `Exe` or `WinExe` with automatic runtime references enabled, the SDK generates `NazorPlatformBootstrap.g.cs` under `obj/<configuration>/<framework>/Nazor/` and includes it in normal and design-time compilation. Its module initializer calls `NazorApplication.RegisterPlatformHost` with a factory for `DesktopPlatformHost` before application startup. The bootstrap registers the desktop router; it does not hard-code the current operating system or initialize a native backend. Backend selection is deferred until the host runs.

Libraries do not emit a bootstrap or register a process default. The initializer also checks that its assembly is the process entry assembly: referencing an executable's components does not replace the consuming process's host. `NazorImplicitRuntimeReference=false` disables bootstrap generation; configure the host explicitly when using that opt-out.

Samples and the workspace use the local SDK, which adds the appropriate desktop source project automatically and passes the publication RID to it. They use the same [platform bootstrap generation](../../Sources/Tools/Nazor.Sdk/Sdk/Nazor.PlatformBootstrap.targets) and backend selection as packaged applications. Source references keep development independent of published packages.

Use `NazorApplication.CreateBuilder(args)` in desktop applications. Override the host through `builder.Platform.Host` or use `WindowsApplication.CreateBuilder(args)` for explicit Windows startup. See [application hosting](04-Application-Hosting-and-Services.md) for factory lifetime and manual configuration.

## Application identity

Every executable using Nazor SDK targets must declare `NazorApplicationId`. Libraries do not need an application ID. Use a stable reverse-DNS value with at least three dot-separated segments. Each segment starts with a lowercase ASCII letter and contains only lowercase ASCII letters and digits. The complete ID must not exceed 128 characters. Empty, uppercase, underscore, hyphen and otherwise invalid values fail the build with `NZB012`, including portable builds and publication without rebuilding.

```xml
<PropertyGroup>
  <NazorApplicationId>org.example.myapp</NazorApplicationId>
</PropertyGroup>
```

The SDK embeds the value as `AssemblyMetadata("Nazor.ApplicationId", "org.example.myapp")` in the entry assembly, independently of `GenerateAssemblyInfo`. It does not change `AssemblyName`, executable filenames, namespaces or NuGet package IDs. Linux uses the metadata for Wayland app IDs, X11 `WM_CLASS`, tray identity and launcher badge destinations. On unpackaged Windows, the backend applies the value as the process AppUserModelID before creating native windows; packaged Windows processes retain their system-assigned identity. Explicit non-SDK hosts without this metadata retain their existing platform behavior.

The format is deliberately shared with future macOS/iOS bundle identifiers and Android application IDs. It does not implement those hosts, generate platform packages or reserve an ID in any store. Keep the ID unchanged across versions, frameworks and architectures. Publication with `--no-build` must use the same ID as the preceding build; it does not rewrite compiled metadata. The template supplies `org.example.myapp`; pass `--ApplicationId org.yourdomain.yourapp` to `dotnet new nazor-app`, or edit the generated project before distribution. Existing executable projects must add the property; update installed Linux desktop entries to match the new runtime identity.

## Linux desktop entry

The SDK generates no `.desktop` file. To copy an existing entry unchanged, select a path relative to the project (absolute paths are also accepted):

```xml
<PropertyGroup>
  <NazorLinuxDesktopEntry>Platforms/Linux/org.example.myapp.desktop</NazorLinuxDesktopEntry>
</PropertyGroup>
```

For executable Linux RID and portable builds, the file is copied to the root of both build and publish output, preserving its filename and bytes. It remains external in single-file publications, including complete content extraction. Windows RID outputs and libraries ignore this property. Selection follows the target RID, not the build machine. A missing selected file fails with `NZB013`. A filename differing from `<NazorApplicationId>.desktop` produces warning `NZB014`, never an SDK validation error; the file is still copied unchanged. The contents are not parsed, merged or rewritten.

The SDK does not install the entry, resolve `Exec`/`Icon`, copy referenced icons implicitly, or change permissions. Supply valid commands and icon references for the actual installation. A differently named desktop entry can launch the app but may not associate with Wayland windows or launcher badges. See [Linux desktop integration](15-Tray-and-System-Notifications.md#linux-desktop-file-actions).

## Windows application manifest

For executable projects (`Exe` or `WinExe`), the SDK supplies a Windows manifest with `PerMonitorV2` DPI awareness. The template needs no local `app.manifest` or `ApplicationManifest` property. Repository source applications using the local SDK use the same [manifest defaults](../../Sources/Tools/Nazor.Sdk/Sdk/Nazor.WindowsManifest.targets) and manifest file; the samples and workspace keep no duplicate copies. The .NET build embeds the manifest in the application; no separate manifest file needs to be distributed.

To use your own manifest, set `NazorWindowsAppManifest` to a path relative to the project:

```xml
<PropertyGroup>
  <NazorWindowsAppManifest>Platforms/Windows/app.manifest</NazorWindowsAppManifest>
</PropertyGroup>
```

The custom file replaces the SDK default completely; settings are not merged. Include the DPI settings you need in that file. A missing file fails the build through the .NET compiler diagnostics. The standard .NET `ApplicationManifest` property remains supported. If both properties are set, they must resolve to the same file; conflicting paths fail with `NZB015`. Relative and absolute paths are normalized consistently by both `dotnet` MSBuild and Visual Studio MSBuild. To start from the SDK default, copy `Sdk/Nazor.Windows.manifest` from the installed `Nazor.Sdk` package into your project and customize the copy.

`NoWin32Manifest=true` prevents the SDK from supplying its default and disables manifest embedding through .NET's standard switch. Remove explicit `NazorWindowsAppManifest` and `ApplicationManifest` settings when using that opt-out. Combining an explicit manifest with `NoWin32Manifest=true` or `Win32Resource` fails with `NZB015`. Custom `Win32Resource` suppresses the SDK default manifest. Libraries receive no default Nazor manifest. These defaults are independent of `NazorImplicitRuntimeReference` and do not select a runtime backend.

This is a Windows executable resource, not a universal packaging format. The SDK does not create macOS `.app` bundles, `Info.plist` files, Linux `.desktop` files, installers or signing configurations. Those remain separate packaging work; Linux desktop entries can only be copied through `NazorLinuxDesktopEntry`.

## Windows application icon

Executable projects (`Exe` or `WinExe`) receive the blue [Nazor application icon](../Assets/README.md) by default. The SDK ships a multiresolution `.ico` and embeds it in the executable; no separate icon file is required when deploying the application. Samples and workspace applications importing `build/Nazor.targets` use the same asset. The Windows backend also uses the executable's icon for native windows.

To replace it, set the standard .NET property to your own `.ico` file:

```xml
<PropertyGroup>
  <ApplicationIcon>Assets/MyApplication.ico</ApplicationIcon>
</PropertyGroup>
```

An explicit `ApplicationIcon` always takes precedence. A project supplying `Win32Resource` keeps control of its own resources. Set `NazorUseDefaultApplicationIcon=false` to disable the default without providing a replacement. Libraries receive no application icon. The template needs no local copy; these defaults apply to both build and publish.

## Suggested structure

```text
HelloNazor/
  HelloNazor.csproj
  Program.cs
  appsettings.json
  Properties/launchSettings.json
  _Imports.nazor
  App.nazor
  App.nazor.nss
  Components/
    TaskCard.nazor
    TaskCard.nazor.cs
    TaskCard.nazor.nss
  Views/
    TasksView.nazor
  Models/
    TaskItem.cs
  Services/
    ITaskStore.cs
    TaskStore.cs
  Styles/
    Theme.nss
  Assets/
    logo.png
  Fonts/
    Family-Regular.ttf
```

These folders are conventions rather than mandatory layers. Introduce a folder when it groups a real responsibility. For a small application, a root component and one stylesheet can be enough.

## Source conventions

| File | Meaning |
| --- | --- |
| `Name.nazor` | Component class `Name`; its default namespace follows the root namespace plus folders |
| `Name.nazor.cs` | Optional partial class, with exactly the same namespace and class name |
| `Name.nazor.nss` | Scoped styles for `Name.nazor`, read before its inline style blocks |
| `_Imports.nazor` | Shared Razor directives for its directory and descendant directories |
| `Theme.nss` | Standalone compiled sheet, exposed as `StyleSheets.Theme` in the root namespace |
| `App.resx`, `App.it.resx` | Neutral and translated resources used through a localizer |
| `app.manifest` | Optional custom Windows manifest selected through `NazorWindowsAppManifest` or `ApplicationManifest`; replaces the SDK default |
| `launchSettings.json` | Development profile; enables the channel and .NET hot reload |

Standalone sheet property names derive from the filename, not the folder: `Styles/high-contrast.nss` becomes `StyleSheets.HighContrast`. Filenames that produce the same property name conflict. A component sidecar is never a standalone sheet.

For example, a root `_Imports.nazor` can contain:

```razor
@using HelloNazor.Components
@using HelloNazor.Models
@using HelloNazor.Services
```

Use `_Imports.nazor` for application and library component namespaces. Razor imports are separate from C# global usings: the compiler supplies `Nazor`, `Nazor.Input`, and, when referenced, `Nazor.Controls`, plus its standard System/component imports automatically. Additional Razor imports remain explicit; ordinary `.cs` files receive the SDK's global usings listed above.

## Build pipeline

The SDK discovers `.nazor` components and `.nss` stylesheets, resolves project references, and runs the compiler before C# compilation. Components are processed together so they can refer to each other. Generated files, response files, and the successful-generation stamp live under `obj/<configuration>/<framework>/Nazor/`.

Generation is incremental and considers component, style, C#, and reference inputs. Edit the original source. Changes to generated files are overwritten, and they can disagree with the editor's source mappings. A failed design-time generation may leave the last successful output available to IntelliSense; a normal build still reports the failure.

## SDK customization

| Property or item | Purpose |
| --- | --- |
| `NazorRootNamespace` | Overrides the root used for component namespaces and the `StyleSheets` class; defaults to `RootNamespace` |
| `NazorImplicitRuntimeReference=false` | Disables the automatic base/host packages, executable bootstrap, associated Nazor/hosting usings and host validation; explicit runtime references and host setup become your responsibility |
| `NazorRestoreWindowsDependencies=true` | Restores the Windows dependencies for a Windows or portable executable that disables implicit runtime references |
| `ImplicitUsings=disable` | Disables implicit C# usings while retaining automatic runtime and host packages; does not change Razor imports |
| `NazorApplicationId` | Required stable cross-platform ID for executable projects; see [application identity](#application-identity) |
| `NazorLinuxDesktopEntry` | Copies an optional custom `.desktop` into Linux and portable output; see [Linux desktop entry](#linux-desktop-entry) |
| `NazorWindowsAppManifest`, `ApplicationManifest` | Select a custom Windows manifest instead of the SDK executable default; conflicting paths are errors; see [Windows application manifest](#windows-application-manifest) |
| `NoWin32Manifest=true` | Disables the default Windows manifest and manifest embedding |
| `NazorCopyAppSettings=false` | Disables automatic output copying of `appsettings*.json` |
| `EnableDefaultNazorAssets=false` | Disables default image and font discovery |
| `NazorAsset` | Adds or removes assets from the SDK's asset set |
| `NazorProjectMetadata=false` | Disables Debug project-directory metadata used by development tools |
| `NazorCompilerPath`, `NazorDotnetHost` | Override compiler or dotnet host location for specialized build integrations |

The asset set includes PNG, JPEG, GIF, WebP, BMP, ICO, SVG, TTF, OTF, and TTC. Files keep their project-relative path in output and publish directories. For external linked files, set `Link` and copy metadata explicitly; see [deployment](28-Libraries-Packaging-and-Deployment.md).

`NazorWatch` runs the style/component watcher for a project. Start it with `dotnet build <project.csproj> -t:NazorWatch -tl:off` in the same configuration as the application. See [hot reload](26-Editors-and-Hot-Reload.md).

Implementation: [Sdk.props](../../Sources/Tools/Nazor.Sdk/Sdk/Sdk.props), [Sdk.targets](../../Sources/Tools/Nazor.Sdk/Sdk/Sdk.targets), and [generation targets](../../Sources/Tools/Nazor.Sdk/Sdk/Nazor.Generation.targets).


## .NET 11 applications

Use the .NET 11 SDK and select `net11.0` explicitly; the template and SDK default remain .NET 10:

```powershell
dotnet new nazor-app -n HelloNazor11 --Framework net11.0
dotnet build HelloNazor11
dotnet publish HelloNazor11 -c Release -r win-x64 --self-contained false
```

Existing projects migrate by changing `TargetFramework` to `net11.0` and selecting an installed .NET 11 SDK in their `global.json`, if present. Application startup, component authoring and NSS syntax are unchanged. NuGet selects the compatible .NET 10 runtime, host, control, theme and optional Agent assets; no dedicated .NET 11 Nazor binaries are required. The compiler, language server and DevTools remain on .NET 10 LTS. Deployment machines need the application runtime, or a self-contained publication, with no authoring tools. Later application frameworks can use the same compatible assets; their SDK, language features and native publication must be verified separately.

The source build uses SDK 10.0.404 (patch roll-forward). Independently, `NazorLanguageServicesSdkVersion` pins Roslyn/Razor to SDK 11.0.100: contributors install both SDKs, but the compiler and language server are built for and run on .NET 10 LTS. Roslyn language services come from the fixed SDK's `dotnet-format` directory; runtime support libraries such as System.Composition remain on LTS versions. Packaged applications and IDE extensions include these language-service assemblies and do not need SDK 11 merely to run the tools. Building a `net11.0` application still requires a compatible Microsoft .NET SDK. MIT attribution and SDK third-party notices accompany compiler and language-server outputs. See [verified support](32-Support-and-Limitations.md#platform-and-distribution).

---

[Guide index](README.md) · [API reference](../API.md)
