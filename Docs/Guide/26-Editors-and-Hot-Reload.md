# 26. Editors and hot reload

`@window` is recognized in Visual Studio and Visual Studio Code, including optional C# opening parameter declarations. The shared language server maps declared types, property names and defaults for hover, navigation, diagnostics and refactoring. Window members such as `Decorations` and `EffectiveIcon` complete through the associated `Window` property. The window snippet generates a minimal window with `OnInitialized` configuration. Select Nazor decorations for automatic theme chrome, or declare `UserTitleBar` and explicitly select User content. Static window nesting and `UserTitleBar` and `UserCaptionButtons` declarations in ordinary components produce the same diagnostics as the build.

Extension version 0.1.0 bundles C# 15 Roslyn/Razor services for `.nazor` files while keeping the language-server runtime on .NET 10 LTS. Both clients use the project's evaluated language version, conditional symbols and reference assemblies. Rebuild after changing these options. Use the Microsoft .NET 11 SDK and a compatible IDE to build `net11.0` apps. Visual Studio's native C# editor services remain those provided by the installed IDE.

## Editor capabilities

The project SDK supplies build integration. Nazor's Visual Studio and Visual Studio Code extensions add `.nazor`/`.nss` highlighting, language-server diagnostics, completion, hover, formatting, source navigation, and refactoring. The shared server reads evaluated project inputs and rebuilt reference metadata; build the application after changing package references.

Quoted NSS strings, Unicode escapes, and NumberPresenter motion properties share syntax highlighting in standalone `.nss` files and embedded `@style` blocks. Visual Studio preserves string-escape coloring without the Web workload; with that workload, NSS values use its CSS colors.

Use `:root` to style the current window. The language server reports `NZS109` when a `Window` type selector is used. Scoped and inline `:root` references navigate to the owner's `@window`; common sheets list all declaring components in the owning project. See [window-root selectors and specificity](17-Selectors-and-Media-Queries.md#state-and-structural-pseudo-classes).

NSS selector completion/hover covers control and interaction-aware validation states, filtered sibling positions, relational :has(), explicit :state(), public ::part(), logical item positions and scoped component roots. Runtime-only references are marked partial. Both IDEs share the compiler's accepted names and diagnostics; see [control-state semantics](17-Selectors-and-Media-Queries.md#state-and-structural-pseudo-classes).

NSS variables support `/** <doc>description</doc> */` comments in hover and completion; see [documentation lookup and fallback](16-Styling-and-Themes.md#document-custom-properties-in-the-editor). Visual Studio editing support includes C#-aware brace completion and indentation, automatic closing markup tags and attributes, overtype, paired Backspace, grouped Undo/Redo, and full-document formatting. C# formatting follows IDE preferences and `.editorconfig` for the logical `.nazor.cs` path; NSS and markup use their own indentation rules.

Visual Studio shows a window icon for Nazor C# projects, a four-leaf document for components and a document with braces for stylesheets. VS Code supplies matching component and stylesheet icons for file icon themes that use language icons.

Visual Studio's Add New Item chooser and Quick Add provide **Nazor Component** and **Nazor Style Sheet** templates. Use `.nazor` for components, `.nss` for standalone stylesheets, and `.nazor.nss` for component sidecars. A standalone stylesheet is not automatically assigned to the application; a sidecar requires an existing component owner.

The full VSIX also makes **Nazor application** available in **Create a new project**. It bundles the same template as `dotnet new nazor-app`, with framework, theme, application ID and sample-page options. [Create a project in Visual Studio](02-Getting-Started.md#create-a-project-in-visual-studio)

Inline C# analysis honors the project's warning/error policy and effective `.editorconfig` settings for the equivalent `.nazor.cs` file, including nested configurations. Diagnostic descriptions retain Roslyn messages, help links and unnecessary-code metadata for clients that support fading. Configuration is projected in memory; no C# sidecar is created.

## Install editor support

Use the editor artifacts for the same Nazor release as your project's SDK. The extension descriptions identify the `0.1.0-alpha` release, and both manifests use the numeric version `0.1.0` required by the editor distribution formats. Uninstall a higher-numbered development build or an older copy with the same version before installing a rebuilt extension. Install the .NET/ASP.NET Core 10 runtimes for the bundled compiler and language server. The source-build steps below produce the editor artifacts locally.

| Editor | Installation |
| --- | --- |
| Visual Studio | Close Visual Studio, open `Nazor.VisualStudio.vsix`, and complete the VSIX installer for the intended instances. |
| Visual Studio, syntax highlighting only | Copy the `Nazor` folder from the syntax-only distribution to `%USERPROFILE%/.vs/Extensions/Nazor`. |
| Visual Studio Code | Read `version` from the supplied `nazor/package.json`. Copy the complete `nazor` folder to `%USERPROFILE%/.vscode/extensions/nazor.nazor-<version>` on Windows or `~/.vscode/extensions/nazor.nazor-<version>` on Linux. Restart VS Code. |

Build the application after changing package references so the server sees evaluated project inputs and rebuilt reference metadata. Visual Studio integration targets Visual Studio 2022 and 2026. VS Code starts its bundled server through `dotnet`. Set `nazor.dotnetPath` to select another executable.

### Build editor artifacts from source

Contributors can run the following command from the repository root with PowerShell 7 and the pinned SDK. The outputs are `artifacts/editors/visualstudio/Nazor.VisualStudio.vsix`, `artifacts/editors/visualstudio-folder/Nazor`, and `artifacts/editors/vscode/nazor`. [Build prerequisites and options](../../Scripts/README.md#build-and-packaging)

```powershell
pwsh -NoProfile -File ./Scripts/Build/build-editors.ps1
```

## DevTools in the editor

With `NazorEnableDevTools=true` and `builder.UseDevTools()`, plain F12 in a running application opens the bundled inspector. Use **Tools > Nazor DevTools** in VS2022/2026 on Windows, or **Nazor: Open DevTools** in VS Code desktop on Windows/Linux, to open it while already paused. VS Code coreclr configurations need `"nazorDevTools": true` for validated current-stop field reads. Install the .NET 10 runtime for the inspector independently of the app's .NET 8/10 runtime. See [DevTools setup, breakpoint behavior and limits](../DevTools.md).
## Editing workflow

Visual Studio provides C# semantic classification, signature help, parameter/directive completion, source-positioned errors, definition/reference navigation, rename, code-behind switching, formatting, snippets, and code actions. Typical commands are F12 for definition, F7 for code-behind, Ctrl+R Ctrl+R for rename, Ctrl+K Ctrl+D for formatting, and Ctrl+. for actions.

Code actions can move code/styles between inline blocks and sidecars, move shared imports, simplify tags, add required parameters/imports, and extract components. Review multi-file edits. Transfers preserve pre-existing sidecar content and stylesheet order; clients without file deletion support may leave an empty sidecar.

Project analyzers and available code fixes load after build/design-time evaluation. Only fixes whose edits map back to source are offered. Fix All and arbitrary project-structure edits are not supported. Formatting uses the Nazor formatter.

NSS completion includes grammar-aware values/functions, custom properties, classes from component sidecars, component qualifiers, `::deep`, pseudo-elements, and color samples. Both extensions recognize `::before`, `::after`, `::placeholder`, `::selection`, and `content`; hover explains their supported behavior. Property completion follows pseudo-element applicability: selection colors, placeholder text styles, and generated-box properties. Visual Studio shows completion color swatches without inline picker adornments; VS Code uses its color-picker support. Do not infer support for an NSS feature solely from TextMate highlighting: build diagnostics are authoritative.





Server logs are under `%LOCALAPPDATA%/Nazor/LanguageServer`. Reference/CodeLens mapping sends generated-source locations back to components where a valid mapping exists. `NAZOR_REFERENCE_MAPPING=0` disables the optional Visual Studio reference mapping before startup. Interactive editor behavior needs verification in the installed IDE.

## Terminal hot reload

Use Debug in two terminals from the directory containing the application folder:

```powershell
# Terminal 1
dotnet watch run --project HelloNazor -c Debug
```

```powershell
# Terminal 2
dotnet build HelloNazor/HelloNazor.csproj -c Debug -t:NazorWatch -tl:off
```

The launch profile should enable `hotReloadEnabled` and set `NAZOR_DEVCHANNEL=1`, as the template does. The target regenerates component C# and sends style updates; dotnet watch applies code deltas. Run the watcher on a project file in the same configuration as the application. `-tl:off` makes watcher output visible immediately. Ctrl+C ends the watch target and cancels its long-running build, which is expected.

| Saved change | Mechanism and state |
| --- | --- |
| NSS sidecar, standalone sheet, or `@style` content | Sheet replacement through the development channel; component state, focus, text, and scroll remain |
| Markup, `@code`, sidecar C#, ordinary C# | Regenerated C# plus .NET hot reload; supported deltas preserve component state |
| Unsupported .NET edit or structural change requiring rebuild | Restart/rebuild; state may be lost |

Adding/removing markup can recreate later elements because render identity also depends on generated sequence positions. `@key` protects supported list identity, not every structural hot-reload edit. Do not treat hot reload as a persistence mechanism.

`dotnet watch` alone does not regenerate Nazor source in normal hot-reload mode. `--no-hot-reload` provides a rebuild/restart workflow for watched source changes. Keep the last successful generated output when fixing Razor/NSS errors; C# errors are reported by the .NET build/debugger.

## Visual Studio hot reload

F5 starts the VSIX watcher for startup Nazor projects and dependencies. Hot Reload saves documents, refreshes generated source, synchronizes Roslyn, sends styles, and requests .NET code changes. With Apply Hot Reload on File Save enabled, saving supported Nazor files triggers that workflow; otherwise use the button. C# files retain native debugger handling.

Check Error List and Output > Nazor Hot Reload for diagnostics or unavailable style delivery. Native debugger messages report C# errors and rude edits. Restart after changes that cannot apply. Ctrl+F5 can use the terminal watcher workflow.

## Inspect the running app

The .NET 10 LTS compiler distributed in Nazor.Sdk also provides a development CLI:

```powershell
$compiler = "$env:USERPROFILE/.nuget/packages/nazor.sdk/0.1.0-alpha/tools/net10.0/Nazor.Compiler.dll"
dotnet $compiler dev HelloNazor info
dotnet $compiler dev HelloNazor dump --styles
```

Use the actual NuGet package root if `NUGET_PACKAGES` or cache settings differ. The target can be a process ID or project directory. Commands include info, dump, and render; results are JSON. Discovery uses the per-user development directory and pipe, normally `%LOCALAPPDATA%/Nazor/Dev`. `NAZOR_DEV_DIRECTORY` can override discovery storage.

The development channel defaults off unless the launch environment or options enable it. Keep it a development feature. See [diagnostics](27-Testing-and-Diagnostics.md) and [troubleshooting](30-Troubleshooting.md).

---

[Guide index](README.md) · [API reference](../API.md)


## External DevTools while debugging

The source Visual Studio extension provides Tools > Nazor DevTools for an attached development app with builder.UseDevTools() enabled. The external UI keeps retained navigation available at breakpoints and shows supported current-stop component fields through the existing debugger without invoking target functions. Follow the [DevTools workflow](../DevTools.md#visual-studio-debugging) for setup, provenance, unsupported values and lifecycle. This separate inspection bridge does not replace Hot Reload or the debugger's own step/continue controls. VS Code requires its own debugger adapter integration; language-server support alone does not supply it.
