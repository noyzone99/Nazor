<h1><img src="Docs/Assets/nazor-banner.png" width="100%" alt="Nazor. Write Razor. Think Blazor. Ship native."></h1>

**Nazor stands for Native-Bl/Razor.** Build cross-platform .NET desktop apps with Razor syntax and a Blazor-inspired component model. Nazor opens native windows, aligns layouts to physical pixels, and draws its own controls without a WebView or a wrapper around another UI framework.

Nazor will always be free to use and open source. That commitment covers the framework, its tools, and everything I release from this repository.

> **`0.1.0-alpha` is an early preview for experiments and prototypes.** Build from source to try it today. Expect changes to the APIs, markup, styling, and tooling.

[Run the Gallery](#build-and-run-from-source) · [Explore the samples](Samples/README.md) · [Report an issue](https://github.com/noyzone99/Nazor/issues)

![The Nazor Gallery control catalog running with the optional NazorLeaf theme](Docs/Assets/gallery.png)

## Why I started Nazor

I started Nazor because I wanted to write a desktop window the way I write a Blazor component. I wanted to compose the UI from small, reusable pieces, keep state close to the controls that use it, and put application logic in ordinary .NET services. I also wanted the result to feel at home on the desktop, with native windows, familiar keyboard behavior, and access to platform services.

Blazor inspired the component model. Avalonia inspired the separation between platform integration and a shared UI renderer. [Concepts for Razor and Avalonia developers](Docs/Guide/01-Introduction.md#concepts-for-razor-and-avalonia-developers)

## From the native window to the smallest detail

- **Native windows without a WebView.** Nazor's platform backends handle the OS window, input, and desktop services. Nazor draws its own controls with Skia and shapes text with HarfBuzz. The control tree runs without wrapped OS widgets. A shared layout engine snaps box edges to physical pixels, and the shared renderer gives controls a consistent look across supported backends. DPI, fonts, and native window chrome can still vary by platform. [Architecture overview](Docs/Guide/31-Architecture-and-Extension-Points.md)
- **Razor syntax and CSS-inspired styling.** Write `.nazor` components with C# and style them with NSS, Nazor's stylesheet language. App UI authoring needs no XAML. Use shared stylesheets for app-wide design tokens and scoped styles for an individual component. [Components and Razor](Docs/Guide/05-Components-and-Razor.md) · [NSS property reference](Docs/Guide/19-NSS-Property-Reference.md)
- **Design the whole window or start with a theme.** Keep the system frame, compose your own title bar and frame where the backend supports it, or customize individual control parts with NSS. Basic ships with the SDK. The Gallery also lets you try optional themes such as Fluent, inspired by WinUI, and NazorLeaf, with its own visual style. [Window customization](Docs/Guide/14-Title-Bars-and-Platform-Integration.md) · [Styling and themes](Docs/Guide/16-Styling-and-Themes.md)

## What I'm working toward

The goal is to make that freedom practical for a small utility or a larger desktop app. Compose a new control from layout and interaction primitives, or start with ready-to-use editors, menus, lists, and tables. Headless tests and DevTools already let you inspect what a component renders, how it lays out, and which styles apply. [Control catalog](Docs/Guide/09-Control-Catalog.md) · [DevTools](Docs/DevTools.md)

Windows 10 and 11 x64 are the primary verification baseline. Linux has Wayland and X11 desktop backends and a DRM/KMS single-view host for embedded systems. Platform features and verification vary by backend. macOS, Android, and iOS are longer-term targets. [Support and limitations](Docs/Guide/32-Support-and-Limitations.md)

You can run the visual regression tests and benchmarks yourself. The Windows comparison runs the same text, paint, and layout workloads in Nazor and Avalonia, with CPU and GPU runs, screenshot checks, and raw results. The protocol describes what those measurements cover. [Testing and benchmarks](Docs/Guide/27-Testing-and-Diagnostics.md#contributor-checks) · [Renderer comparison](Tests/RendererComparison/README.md)

## Help shape the first release

I build Nazor on my own, outside working hours, just for fun. Getting it stable enough for the first real apps is hard work, especially when it comes to keyboard input, accessibility, rendering, and the differences between desktops. Feedback from people trying Nazor on their own machines will help it mature faster.

Running the Gallery for a few minutes is already a contribution. If a control behaves oddly, the build fails on your setup, or a page leaves you wondering what to do next, open an issue. A short description of what you tried, what happened, and which OS and .NET SDK you used is enough to start. [Open an issue](https://github.com/noyzone99/Nazor/issues/new)

## Build and run from source

For now, build Nazor from this repository. The SDK, runtime and theme packages, along with project templates, are coming soon to NuGet.org.

On Windows x64, install the .NET SDK pinned in [`global.json`](global.json) (`10.0.404`) and .NET SDK `11.0.100` for the Razor and C# language services. Clone this repository and run the following from its root. Building the Gallery also builds the framework projects it uses.

```powershell
dotnet build Samples/Nazor.Samples.Gallery -c Release
dotnet run --project Samples/Nazor.Samples.Gallery -c Release --no-build
```

Pick a control in the Gallery to try it and see its source. For a smaller example of state, events, and scoped styling, start with [Counter](Samples/Nazor.Samples.Counter/README.md). On Linux, follow the [native build prerequisites](Docs/Guide/33-Linux-Wayland.md#build-from-source).

[Developer guide](Docs/Guide/README.md) · [API reference](Docs/API.md) · [MIT license](LICENSE)
