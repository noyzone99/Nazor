# 28. Libraries, packaging, and deployment

## Component libraries

A library uses the same SDK and normally omits an executable output type:

```xml
<Project Sdk="Nazor.Sdk/0.1.0-alpha">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Company.Components</RootNamespace>
    <IsPackable>true</IsPackable>
    <PackageId>Company.Components</PackageId>
    <Version>1.0.0</Version>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

The neutral target receives base runtime packages and global C# usings without referencing a desktop or Windows host. Libraries never emit the automatic platform bootstrap, including when they optionally use a Windows-specific TFM for native API access. The consuming executable receives the portable or RID-specific desktop hosting package and registers `DesktopPlatformHost`. Ordinary shared components need no multitargeting; see [target frameworks and native hosts](03-Projects-and-Builds.md#target-frameworks-and-native-hosts).

Reference it through a normal ProjectReference or PackageReference, then import its namespace in `_Imports.nazor`. Public components, parameters, callbacks, generic constraints, and scoped styles are discovered from the compiled assembly. Components require a public parameterless constructor. Document public contracts with XML docs and include the XML file when packaging.

A library can declare custom event metadata with `[EventHandler("onname", typeof(CustomEventArgs))]` in its namespace. Event dispatch still needs a corresponding control/runtime implementation; metadata alone does not create a native event source.

See the [DeviceRegistry component library](../../Tests/Fixtures/Projects/DeviceRegistry/Components/DeviceRegistry.Components.csproj) for an SDK consumer fixture.

## Resource ownership

Keep a unique RootNamespace for generated `StyleSheets` catalogs. If a library deliberately places public components in `Nazor.Controls`, use explicit C# namespace declarations or Razor `@namespace` while retaining its own generated-resource namespace. Transparent implementation wrappers can call the protected `OpenDelegatedComponent<TComponent>` helper to preserve the original caller's scoped styling; ordinary component composition keeps private scope boundaries.

Project assets are copied with relative paths, and project-referenced libraries can pass copied assets to applications. Do not assume that arbitrary content in a library NuGet package will automatically deploy under the same path. Embed library resources and use `ImageSource.FromResource`, or provide explicit package build/content metadata and verify a clean external consumer.

Linked external files need a destination and copy settings:

```xml
<ItemGroup>
  <None Include="../shared/Brand-Regular.ttf" Link="Fonts/Brand-Regular.ttf"
        CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

Ship licenses for third-party fonts/images. The sample source may link a test font, but a distributed app must include its own redistributable assets rather than depend on repository paths.

## Theme packages

A theme is a library with compiled standalone `.nss` files exposed through the generated `StyleSheets` class. It can provide a `Themes` catalog as a convenient public entry point. It needs no control-package reference, plugin interface, or service registration. Consumers choose a sheet explicitly through `app.Appearance.Theme`. Provide the full set of supported control states, forced colors, reduced-motion behavior, and required assets. Record supported framework versions and keep official SDK/runtime/theme versions aligned.

`Nazor.Themes.Fluent` packages its Regular and Filled Fluent System Icons TTFs and MIT notices under `Themes/Fluent/`. Register them before startup with `builder.Fonts.AddFluentFonts()`. Check that both TTFs and `LICENSE.txt`, `NOTICE.txt`, and `SOURCE.txt` reach the consumer's build and publish output. The generated `FluentSymbol` catalog selects the correct family independently of the active theme. [Fluent on NuGet](https://www.nuget.org/packages/Nazor.Themes.Fluent/0.1.0-alpha) · [Fluent icon setup](22-Images-Icons-and-Drawing.md#typed-fluent-symbols)

`Nazor.Themes.NazorLeaf` supplies private fonts and a texture under `Themes/NazorLeaf/` in build and publish output. The application registers the fonts before building its host. Keep the font licenses and texture together with the fonts when distributing the app. The icon font is `NazorLeafIcons.otf`; `LeafSymbol` selects it without depending on a system icon font. [NazorLeaf on NuGet](https://www.nuget.org/packages/Nazor.Themes.NazorLeaf/0.1.0-alpha) · [NazorLeaf setup](16-Styling-and-Themes.md#nazorleaf)

Both theme packages copy their assets during the build. In Visual Studio, the packages appear under **Dependencies**, and their asset folders appear in the build and publish outputs.

To check a theme as a package consumer, start with `HelloNazor` from the quickstart in a directory outside the checkout. Apply the NazorLeaf setup from the styling guide before publishing. Verify `Themes/NazorLeaf` in the publish output, including the four font files, their notices and paper-grain image. Run the published application from that directory to check font and texture loading.

```powershell
$project = 'HelloNazor/HelloNazor.csproj'
$nazorVersion = ([xml](Get-Content -Raw $project)).Project.Sdk.Split('/')[1]
dotnet add $project package Nazor.Themes.NazorLeaf --version $nazorVersion
dotnet publish HelloNazor -c Release -r win-x64 --self-contained false -o HelloNazor-theme-publish
```

## Development inspector payload

NazorEnableDevTools=true includes Nazor.DevTools.Agent and copies its Windows/Linux x64 inspector assets into `Nazor.DevTools/` beside an executable. The tool runs independently with .NET 10 while the app may target .NET 8 or 10. `UseDevTools()` enables plain F12 and automatic window targeting. No separate inspector installation is required. Keep the complete subfolder and matching versions when copying a development app. Unsupported bundle RIDs report NZB009. See [DevTools](../DevTools.md).

## Production without DevTools

Release publication enables the SDK's dependency and payload validation by default:

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -r win-x64 --self-contained false -o artifacts/production/HelloNazor
```

`NazorEnablePublishValidation` defaults to `true` in Release and `false` in Debug and other configurations. An explicit project, publish-profile or command-line value overrides the default. The `NazorProduction` profile remains available to enable the same validation in any configuration. These checks run during publication; ordinary Release builds can still include DevTools. With validation enabled, `NazorEnableDevTools` must remain false, and the guard rejects forbidden `Nazor.Diagnostics`, `Nazor.DevTools` and `Nazor.DevTools.*` dependencies from the resolved package/project graph, including transitive and `PrivateAssets` dependencies.

To publish an optimized development build with DevTools, explicitly opt out of release validation as well as enabling the tools:

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -p:NazorEnableDevTools=true -p:NazorEnablePublishValidation=false -o artifacts/development/HelloNazor
```

Enabling DevTools never disables validation automatically. Disabling validation does not enable DevTools or remove Native AOT's requirement to keep them disabled.

Before copying files, validation inspects publish inputs and destinations, dependency manifests, managed assembly identities and assembly references. After publication it scans the final directory again, including stale files. A renamed diagnostic DLL or an application built with Agent references is rejected. A failed directory is not a deployable result; correct the dependency or stale files and republish into a fresh directory. Build and publish with the same disabled setting, including when using `--no-build`.

| Diagnostic | Meaning |
| --- | --- |
| NZB004 | Missing or mismatched native Linux asset; portable publication requires each selected module for glibc/musl x64/ARM64 |
| NZB005 | Release validation or AOT requested with DevTools enabled, or an Agent reference compiled without `NazorEnableDevTools=true` |
| NZB006 | Forbidden dependency/payload/reference, missing inspection input or invalid metadata |
| NZB008 | `NazorEnableDevTools` or `NazorEnablePublishValidation` is not `true` or `false` |
| NZB011 | Unknown applicable `NazorLinuxNativeLibraries` value; use All, Wayland, X11 or None |

Directory publication supports framework-dependent and self-contained deployment. Self-contained directory and single-file publications also support trimming without AOT on Windows x64 and Linux x64; Native AOT is supported separately. Validation inspects the original managed inputs before trimming, bundling or native compilation and scans the final directory for stale development payloads. It applies through `Nazor.Sdk` and the repository's `build/Nazor.targets` import; a bare `Microsoft.NET.Sdk` project does not acquire this policy or the component metadata preservation automatically. It checks known DevTools identities and destinations, not arbitrary embedded or renamed non-assembly data.

Runtime settings cannot restore the missing Agent/Diagnostics integration. The existing Hot Reload development channel is part of Application and remains independent of this boundary. Set `builder.Diagnostics.DevChannel = false` explicitly to disable that endpoint, including when `NAZOR_DEVCHANNEL` is set in the environment. This profile does not remove Hot Reload code from the runtime.

## Publish an application

Use the ordinary .NET publish workflow for the supported Windows target:

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish/HelloNazor
```

Framework-dependent deployment requires the matching .NET runtime on the destination machine. A self-contained publish (`--self-contained true`) includes its runtime and produces a larger output. Validate both managed assemblies and native Skia/HarfBuzz/backend dependencies in the actual publish directory. The full publish directory, required data files, fonts, resources, and native libraries form the deployable application.

Omit `-r` for a portable publication containing both desktop backends. `-r win-x64` selects Windows hosting and `-r linux-x64` selects Linux hosting. The SDK selects the matching package before restore. If you configure the host explicitly, use the matching desktop package and follow the [Windows dependency setup](03-Projects-and-Builds.md#windows-dependencies) for Windows or portable output. Source-project consumers use `Nazor.Hosting.Desktop` and `build/Nazor.targets`. Publish each variant into a fresh directory to avoid stale files. Run `Scripts/Tests/verify-runtime-publish.ps1` to verify portable, Windows and Linux outputs from both source and NuGet on .NET 8 and .NET 10.

Windows RID publications place all Windows native libraries, windowing resources and `Windowing.manifest` beside the executable, without pruning individual libraries. `WindowsAppSDK.LICENSE.txt` and `ThirdPartyNotices.md` also reside in that root directory. Portable publications retain `runtimes/<rid>/native` so identically named libraries for different architectures remain separate, with one copy of each Windows notice in the root. Linux RID publications do not include these Windows assets or notices. This layout also applies to the external files of single-file and Native AOT publications; single-file extraction still follows the explicit .NET settings described below.

The SDK supplies an embedded Windows DPI manifest by default; a separate `app.manifest` file is not required for deployment. See [manifest customization](03-Projects-and-Builds.md#windows-application-manifest) to replace it. Verify DPI awareness, run from another working directory, and test on a clean target machine. Asset paths should resolve from the application directory, not the shell's current directory. Set development-channel behavior deliberately for a release package and do not distribute internal logs or test artifacts as app content.

Executables also embed the blue Nazor application icon by default. Use [application icon configuration](03-Projects-and-Builds.md#windows-application-icon) to supply your application's own branding. All official Nazor NuGet packages and editor extensions use the shared [Nazor four-leaf mark](../Assets/README.md).

macOS bundles and `Info.plist`, Linux desktop entries, and platform installers are not generated by the SDK. `NazorLinuxDesktopEntry` copies a supplied file unchanged to Linux and portable output; installation and path resolution remain separate. See [application identity and desktop entries](03-Projects-and-Builds.md#application-identity). Platform packaging remains separate from the desktop runtime bootstrap.

Signing, installer creation and other architectures are not established supported deployment configurations here. See [support](32-Support-and-Limitations.md).

When Visual Studio or another build process is restoring the same project in parallel, use `--artifacts-path artifacts/build/my-publish` to isolate publication intermediates. The default `obj/project.assets.json` is shared across configurations; a background development restore can replace the production dependency graph and trigger NZB006 for DevTools even when the publish command disables them. `-o` selects only the final publication directory and does not isolate these intermediates. Use distinct artifact directories for concurrent publications.

### Single-file

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/single
```

Use `linux-x64` for Linux. Nazor keeps the standard .NET single-file defaults: managed assemblies are bundled, while native libraries and copied file assets remain alongside the executable. Distribute the complete publish directory, including Skia/HarfBuzz, windowing manifests and application content. Resolve file assets relative to `AppContext.BaseDirectory`.

Extraction is an explicit application choice. Set `IncludeNativeLibrariesForSelfExtract=true` to bundle native libraries for extraction; copied content can still remain external. To bundle and extract the complete payload, explicitly set both `IncludeAllContentForSelfExtract=true` and `IncludeNativeLibrariesForSelfExtract=true`. That mode can run with only the executable; `ExcludeFromSingleFile` content remains external. Extraction requires a writable runtime cache; configure `DOTNET_BUNDLE_EXTRACT_BASE_DIR` if necessary. Keep user data outside the cache. When configuration is bundled, changing it beside the executable requires an explicitly configured external configuration path.

### Trimming without AOT

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false -p:PublishTrimmed=true -o artifacts/trimmed
```

Add `-p:PublishSingleFile=true` for a trimmed single-file publish; its extraction settings retain the same standard .NET defaults. Use `linux-x64` for Linux. Nazor generates the component preservation descriptor for trimming as well as AOT, retaining constructors, parameters, inherited/private injection, route discovery and generic component contracts. The application continues to use the JIT and keeps .NET's dynamic-code support. Window-parameter nullability metadata remains enabled; other framework feature switches follow the normal trimming defaults. Use an untrimmed development build for Hot Reload.

For JIT trimming, the SDK conservatively copies `Microsoft.Windows.SDK.NET` and `WinRT.Runtime` intact to preserve CsWinRT's reflective ABI fallback. Known IL2081 constructor-annotation warnings are scoped to these two preserved assemblies through linker attributes; application trim warnings and other dependency warnings remain enabled. The rest of the application uses the requested .NET trim mode. Resolve warnings from application dependencies and preserve any additional application-specific reflection contracts.

### Native AOT

```powershell
dotnet publish HelloNazor/HelloNazor.csproj -c Release -r win-x64 -p:PublishAot=true -o artifacts/aot
```

Use `linux-x64` and publish on Linux for Linux AOT. Install the .NET Native AOT native toolchain for the target OS (Visual C++ tools on Windows; clang and development libraries on Linux). Keep DevTools disabled. Use an ordinary build for debugging and Hot Reload.

Nazor's SDK generates `obj/.../Nazor/components.xml` from the application and its references before native compilation. It preserves the existing reflection contracts for Razor and C# components: constructors, inherited/private injection, parameters, cascading values, generic components and pages discovered through routes. Explicit parameterless struct constructors are preserved for null/default parameter assignment, including generic parameters. Window parameter nullability validation remains enabled. Components in newly loaded managed assemblies and runtime-generated closed generic types are subject to Native AOT's standard limitations. Application dependencies must also support AOT; resolve their analyzer warnings rather than suppressing them globally.

Native AOT does not use the single-file bundle extractor. Distribute the complete AOT publish directory, including native Skia/HarfBuzz, Windows windowing or the selected Linux desktop assets and application content. Choose `PublishAot` separately from `PublishSingleFile`; AOT does not promise one physical distribution file.

The repository's `Scripts/Tests/verify-deployment.ps1` and `verify-deployment.sh` compare a native window's rendered pixels and behavior against ordinary directory publication. Modes `trim` and `trim-single` verify trimming without AOT; `single` is untrimmed and `aot` checks native compilation. Single-file uses standard .NET defaults; `-SingleFileExtraction All` on Windows or `NAZOR_TEST_SINGLE_FILE_EXTRACTION=All` on Linux explicitly tests complete extraction from a directory containing only the executable. The probes cover routing, generic/cascading parameters, plain-object parameter assignment, private inherited injection, required window parameters and secondary windows. The Linux script also runs D-Bus client/server probes after trimming or AOT for desktop protocol payloads and signals. Both scripts accept .NET 8, 9, 10 and 11 application targets; use a matching SDK for a newer application framework. Use `verify-packaged-deployment.ps1` or `verify-packaged-deployment.sh` with a fresh workspace and a package feed to repeat the matrix with the actual SDK/runtime NuGet packages.

## Package a component library

Pack the library project with a version that identifies its contents:

```powershell
dotnet pack Company.Components/Company.Components.csproj -c Release -o artifacts/packages
```

Before distributing the package, test it from a separate application using a `PackageReference`. Check component discovery, scoped and standalone stylesheets, XML documentation, and required assets without relying on source-tree paths. Publish a new package version for each release.

Framework contributors can use the [packaging and SDK verification scripts](../../Scripts/README.md) to check the framework package set and external template/library consumers.

The [package API reference](../API.md) covers runtime/framework APIs. Compiler and editor tooling have their own source and tests; they are not additional runtime API-reference packages in this documentation set.

## Framework-specific dependency versions

Nazor's .NET 10 runtime assets use Microsoft.Extensions.Hosting and Microsoft.Extensions.Localization 11.0.0, the newest stable release supporting that framework. The .NET 8/9 assets use the latest compatible 10.x servicing release, 10.0.15; Extensions 11 excludes those application frameworks. Windows SDK projections use 10.0.28000.87 (net9.0 asset) on .NET 9/10 and 10.0.26100.86 (net8.0 asset) on .NET 8. .NET 11 applications select the .NET 10 Nazor assets and their current dependencies.

The rendering stack uses matching SkiaSharp managed/native packages at 4.156.0 and matching HarfBuzzSharp managed/native packages at 14.4.0.100. Windows windowing assets come from Microsoft.WindowsAppSDK.InteractiveExperiences 2.6.5. Publication includes the required native libraries and notices.

This selection applies to application/runtime dependencies. The Razor/NSS compiler and language server run separately on .NET 10 LTS. Visual Studio SDK and LSP Client dependencies belong to the Visual Studio extension, which targets .NET Framework 4.7.2; they do not vary with the application's .NET target. Their selected stable versions remain in use because newer packages require dependencies unavailable from the public NuGet feed.

---

[Guide index](README.md) · [API reference](../API.md)
