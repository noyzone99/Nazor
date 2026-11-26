<h1><img src="Docs/Assets/nazor-banner.png" width="100%" alt="Nazor. Write Razor. Think Blazor. Ship native."></h1>

**Nazor is an open-source .NET UI framework for building native desktop apps with Razor.** It brings Blazor's component model to Windows and Linux. Compose reusable views, keep UI state in C#, and style your app with NSS, a CSS-inspired stylesheet language.

Apps open in real OS windows. Nazor draws its own controls with Skia, shapes text with HarfBuzz, and aligns layouts to physical pixels. The goal is to make the same tools practical for a small desktop utility and for an app with its own design system.

[**Visit the Nazor website**](https://noyzone99.github.io/Nazor/) · [Quick start](#quick-start) · [Build from source](#build-and-run-from-source)

[![NuGet 0.1.2-alpha](https://img.shields.io/badge/NuGet-0.1.2--alpha-004880?logo=nuget)](https://www.nuget.org/packages/Nazor.Templates/0.1.2-alpha)

> **Early preview.** Try Nazor in experiments and prototypes. Expect changes to APIs, markup, styling, and tooling.

## Explore the Nazor showcase

The website brings together the interactive Gallery carousel, the developer guide, and the API reference. In the carousel, switch between Windows 11, KDE Plasma, and GNOME, then drag the slider to compare the light and dark versions of the same app.

[![Nazor Gallery in light and dark on Windows 11, KDE Plasma, and GNOME](Docs/Assets/nazor-desktops.gif)](https://noyzone99.github.io/Nazor/)

### Help bring Nazor to macOS

I build Nazor in my spare time, and I want it to feel at home on every desktop it supports. macOS is on the roadmap. I don't have access to a Mac to develop and test on. Before announcing official support, I want to verify text rendering, display scaling, light and dark themes, and native window behavior on real Macs.

If you have a Mac, I'd love to have you contribute to the backend or test early builds as they take shape. [Introduce yourself in GitHub Discussions](https://github.com/noyzone99/Nazor/discussions).

Use [GitHub Discussions](https://github.com/noyzone99/Nazor/discussions) to ask questions, request features, and share what you're building. If something breaks, [open an issue](https://github.com/noyzone99/Nazor/issues/new) with your Nazor version, OS, and steps to reproduce it.

### Try the Gallery

The Gallery lets you try controls, switch themes, and read the Razor source beside each live example. Download `Nazor.Gallery-portable.zip` from the [`0.1.2-alpha` release](https://github.com/noyzone99/Nazor/releases/tag/v0.1.2-alpha), extract it, and run this command from the extracted folder with the [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed.

```sh
dotnet Nazor.Samples.Gallery.dll
```

Windows x64 is the primary verification baseline. On Linux, the portable build selects Wayland or X11. For a fullscreen DRM/KMS session on a configured Linux local console, add `--drm`. [Platform support](Docs/Guide/32-Support-and-Limitations.md) · [Linux desktop prerequisites](Docs/Guide/33-Linux-Wayland.md#runtime-dependencies) · [X11 prerequisites](Docs/Guide/35-Linux-X11.md#system-dependencies-and-behavior) · [DRM/KMS prerequisites](Docs/Guide/34-Linux-DRM.md#runtime-dependencies-and-local-session)

## Quick start

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and keep NuGet.org enabled. On Linux, also install the runtime dependencies for your desktop backend. [Wayland prerequisites](Docs/Guide/33-Linux-Wayland.md#runtime-dependencies) · [X11 prerequisites](Docs/Guide/35-Linux-X11.md#system-dependencies-and-behavior)

From an empty directory outside this repository, install the [`0.1.2-alpha` template](https://www.nuget.org/packages/Nazor.Templates/0.1.2-alpha), create an app, and run it.

```sh
dotnet new install Nazor.Templates@0.1.2-alpha
dotnet new nazor-app -n HelloNazor
dotnet run --project HelloNazor
```

In Visual Studio 2022 or 2026, install [Nazor for Visual Studio](https://marketplace.visualstudio.com/items?itemName=Noyzone99.nazorvisualstudio). Choose **Nazor application** in **Create a new project**, select a target framework and theme, and press **F5**. [Getting started guide](Docs/Guide/02-Getting-Started.md)

### What a component looks like

A `.nazor` file can hold the markup, C# state, and styles for one component. When you click **Increment**, the event handler updates the state and Nazor re-renders the component.

```razor
<Container Class="counter">
    <TextBlock>Count: @count</TextBlock>
    <Button OnClick="Increment">Increment</Button>
</Container>

@code {
    private int count;
    private void Increment() => count++;
}

@style {
    .counter { display: flex; flex-direction: column; gap: 8px; padding: 16px; }
}
```

Save the example as `Counter.nazor` next to `App.nazor`, then add `<Counter />` inside the app's existing container. [Run the Counter sample](Samples/Nazor.Samples.Counter/README.md) · [Learn components and Razor](Docs/Guide/05-Components-and-Razor.md)

From there, use [built-in controls](Docs/Guide/09-Control-Catalog.md) or compose your own from primitives. Start with the SDK's Basic theme, switch to an [optional theme](Docs/Guide/16-Styling-and-Themes.md), or write your own NSS. Use [DevTools](Docs/DevTools.md) to inspect the rendered tree, layout, and styles while the app runs.

## Build and run from source

On Windows x64, install the .NET SDK pinned in [`global.json`](global.json) (`10.0.404`) and .NET SDK `11.0.100` for the Razor and C# language services. On Linux, also follow the [native build prerequisites](Docs/Guide/33-Linux-Wayland.md#build-from-source).

Clone the repository and build the Gallery. This also builds the framework projects it uses.

```sh
git clone https://github.com/noyzone99/Nazor.git
cd Nazor
dotnet build Samples/Nazor.Samples.Gallery -c Release
dotnet run --project Samples/Nazor.Samples.Gallery -c Release --no-build
```

[Gallery launch and packaging options](Samples/Nazor.Samples.Gallery/README.md) · [Contributor checks](Docs/Guide/27-Testing-and-Diagnostics.md#contributor-checks) · [Renderer comparison](Tests/RendererComparison/README.md)

[Developer guide](Docs/Guide/README.md) · [API reference](Docs/API.md) · [NuGet packages](Docs/NuGet.md#nuget-packages) · [MIT license](LICENSE)
