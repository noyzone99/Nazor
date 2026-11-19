# 2. Getting started

To try the preview immediately, build and run an existing sample from the checkout. The app template and SDK are available through a local package feed built from the same source while the NuGet.org release is being prepared. [Build and run the Gallery](../../README.md#build-and-run-from-source)

## Prerequisites

For this quickstart, use Windows 10 or 11 x64 and the .NET 10 SDK. It supplies the .NET/ASP.NET Core 10 runtimes used by the Razor/NSS compiler, language server and DevTools, and the runtime for the default application target. Applications can target .NET 8 or later; install the matching SDK and runtime for a newer application target such as .NET 11. On Linux, install the native prerequisites for [Wayland](33-Linux-Wayland.md#runtime-dependencies) or [X11](35-Linux-X11.md#system-dependencies-and-behavior). The commands below use PowerShell syntax.

Building Nazor itself requires .NET SDK `10.0.404`, pinned in [global.json](../../global.json), plus SDK `11.0.100` as the fixed source of Roslyn/Razor language-service assemblies. The build and standalone tools remain on .NET 10 LTS; packaged tools include these assemblies. For a packaged .NET 8 app in Visual Studio 2022 17.14, follow the [separate SDK and runtime requirements](03-Projects-and-Builds.md#visual-studio-2022-and-net-8).

## Prepare the package source

Use a folder containing the complete Nazor `0.1.0-alpha` package set and assign its absolute path to `$feed`. To generate the local feed from this checkout, use PowerShell 7, the pinned SDKs, and Docker Buildx with x64 and ARM64 support. Build the [Linux native library matrix](33-Linux-Wayland.md#build-from-source) and pack the framework from the repository root.

```powershell
pwsh -NoProfile -File Scripts/Build/build-native-matrix.ps1
pwsh -NoProfile -File Scripts/Packaging/pack.ps1
$feed = (Resolve-Path artifacts/packages).Path
```

The feed includes the SDK, templates, runtime, optional DevTools packages and both optional themes. Keep NuGet.org enabled for third-party dependencies.

## Create an application

In an empty directory outside the Nazor checkout, register the local package feed and install its prerelease template. Keep `nuget.config` next to the new project directory so the project inherits it. Set `$feed` to the absolute package directory again if you switched terminals.

```powershell
dotnet new nugetconfig
dotnet nuget add source "$feed" --name Nazor --configfile ./nuget.config
dotnet new install "$feed/Nazor.Templates.0.1.0-alpha.nupkg"
```

Create and run the application.

```powershell
dotnet new nazor-app -n HelloNazor
dotnet run --project HelloNazor
```

The template contains a startup program, root component and its C# code-behind, configuration, development launch profile, and stylesheet. The SDK supplies the default Windows DPI manifest. The project uses `Nazor.Sdk/0.1.0-alpha` and targets `net10.0`. The SDK adds global C# usings, runtime packages, and `Nazor.Hosting.Desktop`. It also generates a bootstrap that registers the desktop host. `Program.cs` selects `App` as the main window and configures its appearance before `app.Run()`. The host selects the native backend at runtime. This quickstart uses Windows. [Projects and builds](03-Projects-and-Builds.md) · [Application hosting](04-Application-Hosting-and-Services.md) · [Wayland](33-Linux-Wayland.md) · [X11](35-Linux-X11.md) · [DRM/KMS](34-Linux-DRM.md)

## Template options

Run `dotnet new nazor-app --help` to see the available options.

| Option | Default | Effect |
| --- | --- | --- |
| `--framework`, `-f` | `net10.0` (LTS) | Sets the application target. Choose `net8.0`, `net9.0`, `net10.0`, or `net11.0`. See the [Visual Studio 2022 setup](03-Projects-and-Builds.md#visual-studio-2022-and-net-8). |
| `--ApplicationId` | `org.example.myapp` | Sets the stable [application identity](03-Projects-and-Builds.md#application-identity). Use your own reverse-DNS identifier for distribution. |
| `--Theme` | `basic` | Accepts `basic`, `fluent`, or `nazorleaf`. Fluent and NazorLeaf add their NuGet package at the SDK's version and register the theme's fonts in `Program.cs`. |
| `--IncludeSamplePages` | `false` | Adds a navigation view and Home, Counter, and Tasks pages. |

```powershell
dotnet new nazor-app -n HelloNazorSamples --Theme fluent --IncludeSamplePages
```

Home opens at `/`. Counter demonstrates local component state and re-renders when you press **Increment**. Tasks demonstrates data binding and a scoped service. Check a task, navigate away, and return to see that the window keeps its progress. The Counter starts over when you return to its route. [Project structure](03-Projects-and-Builds.md#suggested-structure) · [Navigation and state](12-Navigation.md#history-and-state)

## Write your first component

Start with the default `HelloNazor` project. Replace `HelloNazor/App.nazor` with the following markup. The example uses the state and click handler in the template's `HelloNazor/App.nazor.cs` code-behind.

```razor
@window
<Container Class="page">
    <TextBlock Class="heading">Hello, @Name</TextBlock>
    <TextField @bind-Value="name" AccessibleName="Your name" />
    <Button OnClick="Increment">Count: @count</Button>
</Container>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "Hello Nazor";
        Window.Width = 480;
        Window.Height = 320;
    }
}

@style {
    .page { display: flex; flex-direction: column; gap: 12px; padding: 24px; }
    .heading { font-size: 24px; font-weight: bold; }
}
```

`@window` identifies a native window definition. Set its title and client size through `Window` in `OnInitialized`. `Container` arranges the controls. `@bind-Value` writes text changes to the code-behind's `name` field. The `Name` property returns `Nazor` while that field is blank. Clicking the button invokes `Increment`, updates `count` and triggers rendering. The `@style` block applies to elements declared by this component.

Run the application again. The window shows **Hello, Nazor** and **Count: 0**. Edit the text field to update the greeting, then click the button to increment the count. Source errors point to `.nazor` or `.nss` files. Generated C# under `obj` is build output.

Publish for Windows x64 from the same directory.

```powershell
dotnet publish HelloNazor -c Release -r win-x64 --self-contained false -o HelloNazor-publish
```

Run `HelloNazor.exe` from `HelloNazor-publish`. For Linux, use `linux-x64` on an x64 glibc system and follow the [deployment guide](28-Libraries-Packaging-and-Deployment.md#publish-an-application) for other targets and deployment modes.

## Continue developing

Follow the [first application tutorial](38-First-Desktop-Application.md) to reuse this template for a project workspace with validation, navigation and a published asset.

Install [editor support](26-Editors-and-Hot-Reload.md) for completion, source navigation, diagnostics, and formatting. Use Debug builds for [hot reload](26-Editors-and-Hot-Reload.md#terminal-hot-reload). Learn the [SDK's file conventions](03-Projects-and-Builds.md) before extracting components or creating a library.

Keep the SDK, runtime packages and optional themes on the same version. If installation or restore fails, see [package installation and restore](30-Troubleshooting.md#package-installation-and-restore).

## Explore the samples

With a checkout of the repository, run from its root:

```powershell
dotnet run --project Samples/Nazor.Samples.Counter
```

Click the button and inspect [Counter.nazor](../../Samples/Nazor.Samples.Counter/Counter.nazor). Next run the [Tasks sample](../../Samples/Nazor.Samples.Tasks/README.md) to see components, views, services, and scoped styles in a larger application. The [sample index](../../Samples/README.md) lists the remaining examples.

---

[Guide index](README.md) · [API reference](../API.md)
