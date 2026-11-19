# 38. Build your first desktop application

Build a small project workspace from the Nazor template. You will create a reusable editor, validate its input, share state between routes, give the editor an accessible label and ship an image with the application. Each step leaves the app runnable.

## 1. Start with the template

Follow [Getting started](02-Getting-Started.md#create-an-application) through the first template run. Use the project name `HelloNazor`, leave `IncludeSamplePages` at its default `false`, and stop before replacing the markup. Run the build and publish commands from the parent directory of `HelloNazor`. Close the app before rebuilding.

**Check.** The template opens a window. Typing in the text field changes its greeting, and clicking the button updates the count.

## 2. Extract an editor

Create `HelloNazor/ProjectNameEditor.nazor`. This component owns the draft and reports a validated value to its parent.

<!-- tutorial-example: ProjectNameEditor.nazor -->
```razor
<Container Class="editor">
    <Field LabelText="Project name" Required="true" Invalid="invalid">
        <Description><TextBlock>Use a name that helps you recognize the project.</TextBlock></Description>
        <Error><TextBlock>Enter a project name.</TextBlock></Error>
        <ChildContent><TextField @bind-Value="draft" /></ChildContent>
    </Field>
    <Button OnClick="SaveAsync">Save project</Button>
</Container>

@code {
    [Parameter] public string InitialName { get; set; } = "";
    [Parameter] public EventCallback<string> OnSaved { get; set; }

    private string draft = "";
    private bool invalid;

    protected override void OnInitialized() => draft = InitialName;

    private async Task SaveAsync()
    {
        var name = draft.Trim();
        invalid = name.Length == 0;
        if (!invalid)
            await OnSaved.InvokeAsync(name);
    }
}
```

Create `HelloNazor/ProjectNameEditor.nazor.nss`. This sidecar is scoped to elements declared by the editor. Other components can use the same class name independently.

<!-- tutorial-example: ProjectNameEditor.nazor.nss -->
```css
.editor {
    display: flex;
    flex-direction: column;
    gap: 12px;
}
```

Create `HelloNazor/ProjectSession.cs`. The service keeps the saved name for the life of the window.

<!-- tutorial-example: ProjectSession.cs -->
```csharp
namespace HelloNazor;

public sealed class ProjectSession
{
    public string Name { get; set; } = "";
}
```

Add this line after the existing service registration in `HelloNazor/Program.cs`, before `builder.Build()`.

<!-- tutorial-example: register-session -->
```csharp
builder.Services.AddScoped<ProjectSession>();
```

Create `HelloNazor/OverviewPage.nazor`. The page gets the saved name from the service and updates it when the editor reports a valid draft.

<!-- tutorial-example: OverviewPage.nazor -->
```razor
@inject ProjectSession Project

<Container>
    <Heading Level="1">Project overview</Heading>
    <ProjectNameEditor InitialName="@Project.Name" OnSaved="RememberName" />
    @if (showConfirmation)
    {
        <InlineMessage Severity="Severity.Success">Saved @Project.Name.</InlineMessage>
    }
</Container>

@code {
    private bool showConfirmation;

    private void RememberName(string name)
    {
        Project.Name = name;
        showConfirmation = true;
    }
}
```

Replace `HelloNazor/App.nazor` with the window shell below. The template's code-behind can remain in the project. The editor uses its own fields and handler.

<!-- tutorial-example: App.nazor -->
```razor
@window

<Container Class="workspace">
    <OverviewPage />
</Container>

@code {
    protected override void OnInitialized()
    {
        Window.Title = "Project workspace";
        Window.Width = 640;
        Window.Height = 400;
    }
}

@style {
    .workspace { padding: 24px; }
}
```

**Check.** Run `dotnet run --project HelloNazor`. Tab to the editor and then to **Save project**. Activate the button with Enter while the field is blank. The field shows the error. Enter a name and save it to see the confirmation. `Field` associates the label, description and error with `TextField`. The editor's C# decides when the value is invalid, and `InlineMessage` exposes the confirmation as a live region. With a screen reader, verify the field name and description on focus and the confirmation after saving. [Input and accessibility](24-Input-and-Accessibility.md)

## 3. Add routes

Insert `@page "/"` as the first line of `HelloNazor/OverviewPage.nazor`. Create `HelloNazor/DetailsPage.nazor`.

<!-- tutorial-example: DetailsPage.nazor -->
```razor
@page "/details"
@inject ProjectSession Project

<Container>
    <Heading Level="1">Project details</Heading>
    <TextBlock>Saved project: @(Project.Name.Length == 0 ? "None" : Project.Name)</TextBlock>
</Container>
```

Create `HelloNazor/ProjectWorkspace.nazor`. `Frame` renders the page for the current route, and `NavigationManager` updates the route inside the same native surface.

<!-- tutorial-example: ProjectWorkspace.nazor -->
```razor
@inject NavigationManager Navigation

<Container Class="workspace">
    <Container Class="navigation">
        <Button OnClick="@(() => Navigation.NavigateTo("/"))">Overview</Button>
        <Button OnClick="@(() => Navigation.NavigateTo("/details"))">Details</Button>
    </Container>
    <Frame>
        <NotFound><TextBlock>Page not found.</TextBlock></NotFound>
    </Frame>
</Container>

@style {
    .workspace { display: flex; flex-direction: column; gap: 16px; padding: 24px; }
    .navigation { display: flex; flex-direction: row; gap: 8px; }
}
```

In `HelloNazor/App.nazor`, replace the `<Container Class="workspace">` block with `<ProjectWorkspace />` and remove the `@style` block. Keep `@window` and the `@code` block that configures the native window.

**Check.** Save a name, open **Details** and check that the name appears. Return to **Overview**. The editor shows the saved name because the new page receives the same window-scoped `ProjectSession`. An unsaved draft lives only in its editor instance and starts from the saved name when you return. Closing the application ends the session. [Navigation](12-Navigation.md) · [Lifecycle and state](08-Lifecycle-State-and-Threading.md)

## 4. Include an image

Create the folder `HelloNazor/Assets` and save this file as `HelloNazor/Assets/project.svg`.

<!-- tutorial-example: Assets/project.svg -->
```xml
<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 48 48">
  <rect x="6" y="6" width="36" height="36" rx="6" fill="#2063a7" />
  <path d="M14 24l6 6 14-14" fill="none" stroke="#ffffff" stroke-width="3" />
</svg>
```

Add this line directly after the heading in `HelloNazor/DetailsPage.nazor`.

<!-- tutorial-example: image -->
```razor
<Image Source="@ImageSource.FromFile("Assets/project.svg")" />
```

The image decorates the heading, so it has no `Alt` text. Give informative images `Alt` text that explains their purpose. The SDK copies recognized assets and preserves their relative paths. The image resolves from the application directory. [Images and resources](22-Images-Icons-and-Drawing.md#images-and-resources) · [SDK files and assets](03-Projects-and-Builds.md)

**Check.** Build and open **Details**. The check mark appears below the heading. The build output contains `Assets/project.svg`.

## 5. Publish and run on the target

Publish the Windows desktop application from the parent directory. The destination needs the .NET 10 runtime.

```powershell
dotnet publish HelloNazor -c Release -r win-x64 --self-contained false -o publish/win-x64
.\publish\win-x64\HelloNazor.exe
```

**Check.** Launch the published executable. Save a name, navigate to **Details** and verify that both the name and image appear. Try the keyboard-only flow again. Copy the whole output directory when deploying.

### Wayland and X11 desktops

For an x64 glibc Linux desktop, publish both desktop bridges. Use the matching [runtime identifier and dependencies](32-Support-and-Limitations.md#platform-and-distribution) for other supported targets. Install the target's .NET 10 runtime and native requirements. [Wayland prerequisites](33-Linux-Wayland.md#runtime-dependencies) · [X11 prerequisites](35-Linux-X11.md#system-dependencies-and-behavior)

```bash
dotnet publish HelloNazor -c Release -r linux-x64 --self-contained false -p:NazorLinuxNativeLibraries=All -o publish/linux-x64
```

Run these commands from the published directory on Linux. Run the check for the session available on the target.

```bash
chmod +x ./HelloNazor
NAZOR_LINUX_BACKEND=Wayland ./HelloNazor
NAZOR_LINUX_BACKEND=X11 ./HelloNazor
```

**Check.** The same workspace opens. Repeat the save, navigation and image checks. Each override requires the corresponding Wayland or X11 display connection. Running without an override lets the default host select the available backend. [Linux backend selection](35-Linux-X11.md#select-and-publish)

### Direct display with DRM/KMS

On a Linux device configured for [DRM/KMS](34-Linux-DRM.md), the same workspace can run as a single view. In a copy of the project, add `using Nazor.Platform.Linux;` to `HelloNazor/Program.cs` and replace `builder.Application.UseMainWindow<App>();` with these lines. Keep the `ProjectSession` registration.

<!-- tutorial-example: drm-startup -->
```csharp
builder.Platform.Host = new DrmPlatformHost();
builder.Application.UseMainView<ProjectWorkspace>();
```

The host starts `ProjectWorkspace` directly. `App.nazor` remains the desktop window definition. Use a separate publication directory and omit the desktop bridges.

```bash
dotnet publish HelloNazor -c Release -r linux-x64 --self-contained false -p:NazorLinuxNativeLibraries=None -o publish/linux-drm-x64
```

**Check.** Run the published app in the [configured local console session](34-Linux-DRM.md#local-session-and-console). The workspace fills the display. Check the editor, routing and image with the target's real input and display devices. Use Ctrl+C from the launching console to stop the app.

---

[Guide index](README.md) · [API reference](../API.md)
