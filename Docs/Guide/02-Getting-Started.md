# 2. Getting started

Create a desktop app with the `0.1.0-alpha` template from NuGet.org or the template included with **Nazor for Visual Studio**. You can also run the Gallery from the source checkout. [NuGet packages](../../README.md#nuget-packages) · [Build and run the Gallery](../../README.md#build-and-run-from-source)

## Prerequisites

For this quickstart, use Windows 10 or 11 x64 and the .NET 10 SDK. It supplies the .NET/ASP.NET Core 10 runtimes used by the Razor/NSS compiler, language server and DevTools, and the runtime for the default application target. Applications can target .NET 8 or later; install the matching SDK and runtime for a newer application target such as .NET 11. On Linux, install the native prerequisites for [Wayland](33-Linux-Wayland.md#runtime-dependencies) or [X11](35-Linux-X11.md#system-dependencies-and-behavior). The commands below use PowerShell syntax.

For a packaged .NET 8 app in Visual Studio 2022 17.14, follow the [separate SDK and runtime requirements](03-Projects-and-Builds.md#visual-studio-2022-and-net-8).

## Create a project in Visual Studio

Install **Nazor for Visual Studio**, then choose **Create a new project** and search for **Nazor application**. Visual Studio suggests `NazorApp1` as the first project name. On the additional information page, select the framework and theme, enter the application ID, and use the checkboxes for sample pages and Nazor DevTools. The extension includes the template and Visual Studio restores the SDK, runtime, and any optional packages from NuGet.org. Keep that package source enabled. The framework choices depend on the installed Visual Studio and .NET SDK versions. [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=Noyzone99.nazorvisualstudio) · [Editor setup](26-Editors-and-Hot-Reload.md#install-editor-support) · [Template defaults and options](#template-options)

## Create an application

In an empty directory outside the Nazor checkout, install the `0.1.0-alpha` template with NuGet.org enabled. [Nazor.Templates on NuGet](https://www.nuget.org/packages/Nazor.Templates/0.1.0-alpha)

```powershell
dotnet new install Nazor.Templates@0.1.0-alpha
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
| `--IncludeDevTools` | `false` | Configures the bundled inspector for Debug builds on Windows x64 and Linux x64. Release builds keep DevTools disabled by default. |

```powershell
dotnet new nazor-app -n HelloNazorSamples --Theme fluent --IncludeSamplePages
```

Home opens at `/`. Counter demonstrates local component state and re-renders when you press **Increment**. Tasks demonstrates data binding and a scoped service. Check a task, navigate away, and return to see that the window keeps its progress. The Counter starts over when you return to its route. [Project structure](03-Projects-and-Builds.md#suggested-structure) · [Navigation and state](12-Navigation.md#history-and-state)

To inspect a new app, add `--IncludeDevTools` or select **Include Nazor DevTools** in Visual Studio. Run the app in Debug and press **F12** in its window to open the inspector. The inspector requires the .NET 10 runtime. [DevTools setup](../DevTools.md#start-and-connect)

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

## Build a local package feed

To test changes to Nazor itself, use PowerShell 7, the .NET SDK pinned in `global.json`, SDK `11.0.100` for the bundled language services, and Docker Buildx with x64 and ARM64 support. Run these commands from the checkout root. [Pinned SDK](../../global.json) · [Native build prerequisites](33-Linux-Wayland.md#build-from-source) · [Packaging options](../../Scripts/README.md#build-and-packaging)

```powershell
pwsh -NoProfile -File Scripts/Build/build-native-matrix.ps1
pwsh -NoProfile -File Scripts/Packaging/pack.ps1
$feed = (Resolve-Path artifacts/packages).Path
```

From the directory that will contain your test project, add the local feed and install the template from its package file. Keep NuGet.org available for third-party dependencies. Use a separate NuGet cache when testing a local package with the same version as a published package.

```powershell
dotnet new nugetconfig
dotnet nuget add source "$feed" --name NazorLocal --configfile ./nuget.config
dotnet new install "$feed/Nazor.Templates.0.1.0-alpha.nupkg"
```

If both feeds offer the same Nazor version, map `Nazor.*` to the local feed in the test project's `nuget.config`. [NuGet package source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping)

## Explore the samples

With a checkout of the repository, run from its root:

```powershell
dotnet run --project Samples/Nazor.Samples.Counter
```

Click the button and inspect [Counter.nazor](../../Samples/Nazor.Samples.Counter/Counter.nazor). Next run the [Tasks sample](../../Samples/Nazor.Samples.Tasks/README.md) to see components, views, services, and scoped styles in a larger application. The [sample index](../../Samples/README.md) lists the remaining examples.

---

[Guide index](README.md) · [API reference](../API.md)
