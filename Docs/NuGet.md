# Nazor

Nazor is an open-source .NET UI framework for building native desktop applications with Razor components, C# and NSS stylesheets. This is an early alpha preview. Expect breaking changes in APIs, markup, styling and tooling.

Create an app with `Nazor.Templates` or use **Nazor application** in the Visual Studio extension. The template configures `Nazor.Sdk`, which restores the required runtime packages from NuGet.org. Basic is built in. Optional theme packages include Fluent, NazorLeaf, Adwaita, Breeze, Windows 9x, Windows XP, Windows 7 and CDE. Choose the alpha theme release with the same numeric version as the SDK. [Theme setup](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/16-Styling-and-Themes.md)

Windows x64 is the primary verification baseline. Linux has Wayland, X11 and explicit DRM/KMS backends with different prerequisites and capabilities. See the support guide for details.

- [Get started](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/02-Getting-Started.md)
- [Nazor.Templates 0.1.2-alpha](https://www.nuget.org/packages/Nazor.Templates/0.1.2-alpha)
- [Nazor for Visual Studio](https://marketplace.visualstudio.com/items?itemName=Noyzone99.nazorvisualstudio)
- [Developer guide](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/README.md)
- [API reference](https://github.com/noyzone99/Nazor/blob/main/Docs/API.md)
- [Support and limitations](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/32-Support-and-Limitations.md)

## NuGet packages

| Package | NuGet version |
| --- | --- |
| `Nazor.Sdk` | [![Nazor.Sdk 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Sdk/0.1.2-alpha) |
| `Nazor.Templates` | [![Nazor.Templates 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Templates/0.1.2-alpha) |
| `Nazor.Accessibility` | [![Nazor.Accessibility 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Accessibility/0.1.2-alpha) |
| `Nazor.Animation` | [![Nazor.Animation 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Animation/0.1.2-alpha) |
| `Nazor.Application` | [![Nazor.Application 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Application/0.1.2-alpha) |
| `Nazor.Controls` | [![Nazor.Controls 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Controls/0.1.2-alpha) |
| `Nazor.Core` | [![Nazor.Core 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Core/0.1.2-alpha) |
| `Nazor.DevTools.Agent` | [![Nazor.DevTools.Agent 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.DevTools.Agent/0.1.2-alpha) |
| `Nazor.DevTools.Protocol` | [![Nazor.DevTools.Protocol 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.DevTools.Protocol/0.1.2-alpha) |
| `Nazor.Diagnostics` | [![Nazor.Diagnostics 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Diagnostics/0.1.2-alpha) |
| `Nazor.Graphics` | [![Nazor.Graphics 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Graphics/0.1.2-alpha) |
| `Nazor.Hosting.Desktop` | [![Nazor.Hosting.Desktop 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Hosting.Desktop/0.1.2-alpha) |
| `Nazor.Hosting.Desktop.Linux` | [![Nazor.Hosting.Desktop.Linux 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Hosting.Desktop.Linux/0.1.2-alpha) |
| `Nazor.Hosting.Desktop.Windows` | [![Nazor.Hosting.Desktop.Windows 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Hosting.Desktop.Windows/0.1.2-alpha) |
| `Nazor.Hosting.Windows` | [![Nazor.Hosting.Windows 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Hosting.Windows/0.1.2-alpha) |
| `Nazor.Input` | [![Nazor.Input 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Input/0.1.2-alpha) |
| `Nazor.Layout` | [![Nazor.Layout 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Layout/0.1.2-alpha) |
| `Nazor.Platform` | [![Nazor.Platform 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Platform/0.1.2-alpha) |
| `Nazor.Platform.Linux` | [![Nazor.Platform.Linux 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Platform.Linux/0.1.2-alpha) |
| `Nazor.Platform.Windows` | [![Nazor.Platform.Windows 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Platform.Windows/0.1.2-alpha) |
| `Nazor.Presentation` | [![Nazor.Presentation 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Presentation/0.1.2-alpha) |
| `Nazor.Renderer.Skia` | [![Nazor.Renderer.Skia 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Renderer.Skia/0.1.2-alpha) |
| `Nazor.Style` | [![Nazor.Style 0.1.2-alpha](https://img.shields.io/badge/0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Style/0.1.2-alpha) |

### Optional themes

| Package | NuGet version |
| --- | --- |
| `Nazor.Themes.Adwaita` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Adwaita/0.1.2-alpha) |
| `Nazor.Themes.Breeze` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Breeze/0.1.2-alpha) |
| `Nazor.Themes.Cde` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Cde/0.1.2-alpha) |
| `Nazor.Themes.Fluent` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Fluent/0.1.2-alpha) |
| `Nazor.Themes.NazorLeaf` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.NazorLeaf/0.1.2-alpha) |
| `Nazor.Themes.Windows7` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Windows7/0.1.2-alpha) |
| `Nazor.Themes.Windows9x` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.Windows9x/0.1.2-alpha) |
| `Nazor.Themes.WindowsXP` | [0.1.2-alpha](https://www.nuget.org/packages/Nazor.Themes.WindowsXP/0.1.2-alpha) |
