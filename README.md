<img src="Docs/Assets/nazor-banner.png" width="100%" alt="Nazor. Write Razor. Think Blazor. Ship native.">

# Nazor

**Nazor is an open-source .NET UI framework for building native Windows and Linux desktop apps with Razor and C#.** It brings Blazor-style components to desktop apps. Style your interface with NSS and let Nazor handle layout and draw controls with SkiaSharp.

**No WebView. No XAML. No native control wrappers.**

[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet 0.1.2-alpha](https://img.shields.io/badge/NuGet-0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Templates/0.1.2-alpha)
[![.NET 10 tooling](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![CI build](https://github.com/noyzone99/Nazor/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/noyzone99/Nazor/actions/workflows/tests.yml?query=branch%3Amain)
[![Alpha release](https://img.shields.io/badge/status-alpha-orange.svg)](https://github.com/noyzone99/Nazor/releases/tag/v0.1.2-alpha)
[![Download Gallery](https://img.shields.io/badge/Gallery-download-blue.svg)](https://github.com/noyzone99/Nazor/releases/download/v0.1.2-alpha/Nazor.Gallery-portable.zip)

[**Website**](https://noyzone99.github.io/Nazor/) · [Documentation](Docs/Guide/README.md) · [API Reference](Docs/API.md) · [Download Gallery](https://github.com/noyzone99/Nazor/releases/download/v0.1.2-alpha/Nazor.Gallery-portable.zip) · [Discussions](https://github.com/noyzone99/Nazor/discussions)

[![Nazor Gallery in light and dark on Windows 11, KDE Plasma, and GNOME](Docs/Assets/nazor-desktops.gif)](https://noyzone99.github.io/Nazor/)

Windows 11 · KDE Plasma · GNOME. Click the preview to open the interactive carousel on the website.

## Key Features

- **Razor-like syntax and reactive UI.** Compose reusable components with declarative markup and C#. Nazor updates the UI after component events change state.
- **NSS styling.** Use CSS-inspired layouts, scoped styles, and design tokens. Start with the built-in Basic theme, install an optional theme, or write your own.
- **Native rendering.** SkiaSharp draws Nazor's controls, HarfBuzz shapes text, and layout snaps box edges to physical pixels.
- **Custom window decorations.** Compose your own title-bar content and choose between native and Nazor-drawn decorations where supported. [Windowing guide](Docs/Guide/14-Title-Bars-and-Platform-Integration.md)
- **Windows and Linux.** Build desktop apps for Windows, Wayland, and X11. Use DRM/KMS for single-view kiosk or embedded hosting.
- **Developer tooling.** Start from a Visual Studio template, iterate with Hot Reload, and inspect live UI with Runtime DevTools.
- **Native AOT.** Publish native executables for Windows x64 and Linux x64. [Deployment guide](Docs/Guide/28-Libraries-Packaging-and-Deployment.md#native-aot)

## Quick Start

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and enable NuGet.org. On Linux, install the runtime dependencies for your desktop backend. [Wayland prerequisites](Docs/Guide/33-Linux-Wayland.md#runtime-dependencies) · [X11 prerequisites](Docs/Guide/35-Linux-X11.md#system-dependencies-and-behavior)

Open an empty directory outside this repository and run:

```sh
dotnet new install Nazor.Templates@0.1.2-alpha
dotnet new nazor-app -n HelloNazor
dotnet run --project HelloNazor
```

In Visual Studio 2022 or 2026, install **Nazor for Visual Studio**, choose **Nazor application** in **Create a new project**, and press **F5**. [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=Noyzone99.nazorvisualstudio) · [Getting started guide](Docs/Guide/02-Getting-Started.md)

## Code Example

`Counter.nazor` combines markup and C# state. Clicking **Increment** updates the counter and re-renders the component.

```razor
<Container Class="counter">
    <TextBlock>Count: @count</TextBlock>
    <Button OnClick="Increment">Increment</Button>
</Container>

@code {
    private int count;
    private void Increment() => count++;
}
```

`Counter.nazor.nss` styles the component.

```css
.counter {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 16px;
}
```

Place both files next to `App.nazor`, then add `<Counter />` inside its existing container. [Run the Counter sample](Samples/Nazor.Samples.Counter/README.md) · [Components and Razor](Docs/Guide/05-Components-and-Razor.md)

## Why Nazor?

I wanted to build a desktop window the way I write a Blazor component, with reusable UI pieces, state close to the controls that use it, and application logic in ordinary .NET services. Avalonia inspired the split between the shared renderer and platform backends. Nazor aims to make that approach practical for a small utility and for an app with its own design system.

## Platform Support

- **Windows 10 and 11 x64** are the primary verification baseline. Desktop integration includes native window controls, custom title bars, and tray icons. Features such as title-bar styling and backdrops depend on Windows version and composition support.
- **Linux Wayland and X11** provide desktop windows and session integration. **DRM/KMS** runs a fullscreen single view from a configured local console for kiosks and embedded devices.

[Support and limitations](Docs/Guide/32-Support-and-Limitations.md) · [Wayland](Docs/Guide/33-Linux-Wayland.md) · [X11](Docs/Guide/35-Linux-X11.md) · [DRM/KMS](Docs/Guide/34-Linux-DRM.md)

### Try the Gallery

Explore controls, switch themes, and read the Razor source beside each live example. Download `Nazor.Gallery-portable.zip`, extract it, and run the following command from the extracted folder with the .NET 10 Runtime installed. [Gallery release](https://github.com/noyzone99/Nazor/releases/tag/v0.1.2-alpha) · [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

```sh
dotnet Nazor.Samples.Gallery.dll
```

The portable build selects Wayland or X11 on Linux. Add `--drm` to run it from a configured local console. [DRM/KMS prerequisites](Docs/Guide/34-Linux-DRM.md#runtime-dependencies-and-local-session)

## Tooling

- **Editors.** Use the Visual Studio extension or VS Code support for Razor and NSS.
- **Hot Reload.** Apply supported markup, style, and C# changes in a running development app. Some .NET edits require a restart.
- **Runtime DevTools.** Opt in to the inspector on Windows and Linux x64. Press **F12** to inspect the rendered tree, layout, computed styles, and frame timings.

[Editor setup and Hot Reload](Docs/Guide/26-Editors-and-Hot-Reload.md) · [Runtime DevTools](Docs/DevTools.md)

## Documentation

[First desktop app tutorial](Docs/Guide/38-First-Desktop-Application.md) · [Developer guide](Docs/Guide/README.md) · [API Reference](Docs/API.md) · [NSS reference](Docs/Guide/19-NSS-Property-Reference.md) · [NuGet packages](Docs/NuGet.md#nuget-packages)

## Performance

The renderer comparison runs the same text, paint, and layout workloads in Nazor and Avalonia, with Windows CPU and GPU runs, screenshot checks, and raw results. [Benchmark protocol and results](Tests/RendererComparison/README.md)

**Native AOT** is verified for .NET 8 and .NET 10 apps on Windows x64 and Linux x64. Publish on the target OS with its native toolchain, disable DevTools, and use AOT-compatible dependencies. [Native AOT requirements](Docs/Guide/28-Libraries-Packaging-and-Deployment.md#native-aot)

## Roadmap

Nazor is in alpha. Expect changes to APIs, markup, styling, and tooling as the project matures. The desktop focus is on stabilizing the existing backends and bringing up macOS. Android and iOS are longer-term targets. [Current platform status](Docs/Guide/32-Support-and-Limitations.md#platform-and-distribution)

## Contributing

I build Nazor in my spare time. Try the Gallery, ask a question, propose a feature, or share what you're building. For a bug, include the Nazor version, OS, backend, and steps to reproduce it. [GitHub Discussions](https://github.com/noyzone99/Nazor/discussions) · [Open an issue](https://github.com/noyzone99/Nazor/issues/new)

### Help bring Nazor to macOS

I don't have a Mac to develop and test on. Before announcing official support, I want to verify text rendering, display scaling, light and dark themes, and native window behavior on real Macs. If you have a Mac and want to help build the backend or test early builds, I'd love to hear from you. [Introduce yourself in Discussions](https://github.com/noyzone99/Nazor/discussions)

### Build and run from source

Install the .NET SDK pinned in [`global.json`](global.json) (`10.0.404`) and .NET SDK `11.0.100` for the Razor and C# language services. On Linux, also follow the native build prerequisites. [Linux build prerequisites](Docs/Guide/33-Linux-Wayland.md#build-from-source)

```sh
git clone https://github.com/noyzone99/Nazor.git
cd Nazor
dotnet build Samples/Nazor.Samples.Gallery -c Release
dotnet run --project Samples/Nazor.Samples.Gallery -c Release --no-build
```

[Gallery launch and packaging options](Samples/Nazor.Samples.Gallery/README.md) · [Contributor checks](Docs/Guide/27-Testing-and-Diagnostics.md#contributor-checks)

## License

Nazor and its tools are free and open source under the [MIT license](LICENSE).
