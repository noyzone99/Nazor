# Framework source

Production runtime packages, platform integration and the build/editor toolchain. Directory names group implementation responsibilities; public namespaces and package contracts are documented in the [API reference](../Docs/API.md).

`Nazor.Platform.Linux` also contains the explicit [DRM/KMS single-view host](../Docs/Guide/34-Linux-DRM.md)
for applications that own a display without a desktop or compositor.

## Directory layout

| Directory | Responsibility | Projects and contents |
| --- | --- | --- |
| [Core](Core) | Shared runtime and rendering pipeline. | `Nazor.Core` implements components, reconciliation, retained nodes, dispatch and navigation. `Nazor.Controls` supplies application controls and interaction primitives, usable with Basic or None. `Nazor.Style` handles NSS properties, selectors and cascade; `Nazor.Animation` handles transitions and keyframes. `Nazor.Graphics`, `Nazor.Layout`, `Nazor.Presentation` and `Nazor.Renderer.Skia` cover drawing contracts, measurement, scene orchestration and raster rendering. `Nazor.Input`, `Nazor.Accessibility`, `Nazor.Diagnostics` and `Nazor.Application` handle interaction, semantics, inspection and application/window lifetime. |
| [Themes](Themes) | Optional alpha theme packages layered on Basic. | Fluent, NazorLeaf, Adwaita, Breeze, Windows 9x, Windows XP, Windows 7 and CDE. Theme catalogs expose compiled NSS sheets. Fluent, NazorLeaf and Adwaita include their own font assets. [Theme setup](../Docs/Guide/16-Styling-and-Themes.md) |
| [Hosts](Hosts) | Application startup and platform selection. | `Nazor.Hosting.Desktop` selects the native backend at runtime; `Nazor.Hosting.Windows` retains explicit Windows startup and compatibility APIs. |
| [Platforms](Platforms) | Backend contracts and native implementations. | `Nazor.Platform` defines shared contracts; `Nazor.Platform.Windows` provides Win32 windows, input, desktop integration and UI Automation; `Nazor.Platform.Linux` implements direct Wayland/X11 with independent C bridges, shared D-Bus desktop integration and AT-SPI, plus the explicit DRM/KMS host. |
| [Tools](Tools) | Compilation, SDK integration, optional inspection and editor support. | `Nazor.Compiler` compiles Razor components and provides watch/development commands. `Nazor.Nss.Compiler` compiles NSS. `Nazor.Sdk` integrates MSBuild and asset deployment; `Nazor.Templates` provides `nazor-app`. `Nazor.DevTools.Agent` supplies Program.cs `builder.UseDevTools()` and explicit per-window Diagnostics attachment and exports bounded retained-tree snapshots/deltas; ordinary runtime/host packages do not reference it. `Nazor.DevTools` is the external desktop inspector with its own scene/theme, retained cache and Styles/Computed/Frames panels; it references Protocol and ordinary runtime packages, without Agent, Diagnostics or IDE SDKs. `Nazor.LanguageServer`, `Nazor.Grammars`, `Nazor.VisualStudio`, `Nazor.VisualStudio.CodeLens` and `Nazor.VSCode` implement editor services and integrations. |
| [Shared](Shared) | Source files compiled into multiple projects. | `DevChannel` contains shared development-channel protocol and transport code. |

`Nazor.DevTools.Protocol` contains detached DTOs for .NET Standard 2.0 and stream messaging for .NET 8/10. It has no runtime/UI/IDE dependency; the optional Agent references it. The [wire reference](../Docs/DevTools-Protocol.md) defines negotiation, limits, provenance, revision ordering and cancellation. Neither package starts an inspector endpoint automatically. Explicit `EnableDevTools` creates one current-user local pipe per window; Protocol supplies private discovery and the client connector without a runtime dependency.

The runtime pipeline is component rendering, style resolution, layout, display-list construction and Skia rendering. Native windows use GPU recording/composition with CPU fallback; headless images use software rendering. Native hosts add message processing, presentation and OS services. See [architecture and extension points](../Docs/Guide/31-Architecture-and-Extension-Points.md) for package boundaries and extension contracts.

## Build and validate

Run from the repository root:

```powershell
dotnet build Nazor.slnx -c Release
pwsh -NoProfile -File ./Scripts/Build/build-editors.ps1
pwsh -NoProfile -File ./Scripts/Packaging/pack.ps1
```

Editor artifacts and packages are written under `artifacts/`; editor installation is manual. Edit maintained `.cs`, `.nazor`, `.nss`, project and SDK files. Generated sources under `obj/` and compiled outputs under `bin/` are build products.

Use the [test project map](../Tests/README.md) to select validation for a change. Compiler, SDK and reference-resolution changes also need [external SDK verification](../Scripts/README.md), since source-project builds alone do not exercise packaged consumers.

Run the source inspector with `dotnet run --project Sources/Tools/Nazor.DevTools -c Release`. See [standalone DevTools](../Docs/DevTools.md) for Agent opt-in, discovery, capture provenance and lifecycle. The optional Agent package bundles Windows/Linux x64 inspector payloads for F12 and IDE launch; the same guide documents prerequisites and debugger workflows.

The source SDK pin is .NET 10.0.404 with patch roll-forward. Runtime and theme assemblies target .NET 8/9/10; .NET 11 applications select compatible .NET 10 assets. Compiler and language-server Razor/Roslyn references are independently pinned to SDK 11.0.100 for C# 15 support, with runtime support libraries taken from the LTS SDK. Contributors install both SDKs; packages include the language-service assemblies. All standalone tools run on .NET 10 LTS. Their outputs retain MIT attribution and SDK third-party notices under `ThirdParty/`.
