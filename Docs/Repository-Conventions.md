# Repository conventions

These rules apply to maintained source, scripts and documentation. The [repository index](../README.md) maps the top-level directories; the [source map](../Sources/README.md) distinguishes package responsibilities.

## Language

Use clear technical English for identifiers, diagnostics, exceptions, logs, CLI output, neutral application resources and documentation. Use consistent terms: component, retained element, computed style, scene, display list, native backend, code-behind and sidecar stylesheet.

Translated `.resx` files and deliberate culture/Unicode inputs remain in their original languages. This includes expected localized control output, date/number formats, text-shaping corpora and accessibility fixtures. Translating those values can invalidate the behavior being tested. Keep source and assertions paired when changing ordinary fixture labels. Preserve upstream license/attribution text.

## Code comments and API documentation

Explanatory source comments belong in `Samples/`. Outside samples, retain XML documentation only for the public/protected framework API in `Sources/Core`, `Sources/Hosts`, `Sources/Platforms` and `Sources/Themes`. Internal/private implementation details, tooling code, tests, templates and scripts are explained in their canonical technical document instead of inline commentary.

Compiler/editor tests may contain comment syntax as input data, including strings, raw strings and parser fixture files. Generated-code markers, sidecar ownership markers and required preprocessor/PowerShell directives are functional syntax, not implementation commentary. Do not remove them or change a parser test to satisfy a text search.

Public API documentation describes contracts, parameters, results, ownership, errors and threading where relevant. Changes to public declarations must update their XML documentation and affected package reference pages. Source directory grouping alone must not rename a public namespace.

## Platform design

Treat Windows, Linux and macOS as architectural targets when choosing APIs, dependencies, protocols, project boundaries, packaging and tooling. macOS delivery may follow Windows/Linux implementation, but every current design must preserve a viable macOS implementation path. Evaluate candidate dependencies for all three targets and isolate native OS/IDE behavior behind the existing platform boundaries. Keep shared runtime/tooling contracts independent of Windows-only APIs and editor SDKs; check paths, case sensitivity, input conventions, IPC and resource lifetime where relevant. Platform-specific implementations are appropriate within their own adapters.

Distinguish design portability, implementation status and verification evidence. Missing hardware may defer native acceptance, but must not silently remove a target from architectural decisions. Use portable/headless verification where applicable and record remaining native checks explicitly. Planned macOS support is not a claim that a backend is already implemented or tested; the [support guide](Guide/32-Support-and-Limitations.md#platform-and-distribution) defines current availability.

## Naming

- Use English `PascalCase` for .NET types and public/protected members, `camelCase` for parameters and local/private fields, and an `I` prefix for interfaces.
- Follow the established English sentence-style test method names, including underscores used by the xUnit suites.
- Match component filenames and partial classes; retain `.nazor.cs` and `.nazor.nss` sidecar suffixes and `_Imports.nazor` for shared imports.
- Keep `Nazor.*` project/package names and intentional public namespace choices. Framework directories group responsibilities; application/sample/test namespaces normally follow their folders. Diagnostic enforcement follows the checked-in `.editorconfig`.
- Group PowerShell scripts by purpose under `Scripts/`; use lowercase hyphenated command names. The existing `UiaClient.ps1` matches its shared test helper name.
- Use English directory names and descriptive Markdown filenames. `README.md` is the directory entry point.

Do not rename platform API contracts, generated protocol names, resource keys or diagnostic IDs merely to match a spelling preference; trace all consumers and compatibility implications first.

## Documentation ownership

For NSS colors, preserve the dynamic alias contract: CSS system color keywords such as `Canvas` resolve through the corresponding inherited `--nz-system-*` custom properties. These properties can be overridden; they are not read-only or protected. Recommend theme tokens such as `--nz-surface` and `--nz-accent` for application customization. Distinguish native, derived and fallback palette values, and never present a dynamic system color as a fixed editor swatch. The [system palette guide](Guide/16-Styling-and-Themes.md#system-color-keywords-and-theme-tokens) defines the usage contract; keep compiler/runtime, language-server help and both editor packages consistent with it.

| Location | Authoritative content |
| --- | --- |
| Root and directory READMEs | Orientation, responsibilities, subdirectories and links to detailed documents |
| `Docs/Guide` | Current application usage, supported behavior and limitations |
| `Docs/API.md`, `Docs/ApiReferences` | Package index and public/protected member contracts |
| `Samples` | Teaching explanations beside runnable examples |
| `Scripts/README.md`, `Tests/README.md` | Automation options, suite selection, fixtures and outputs |

Maintain one authoritative description per topic. Link instead of copying feature tables, backlogs or current-status paragraphs. Keep licenses/notices beside the governed assets; they are not duplicate usage manuals. Generated build outputs, logs and temporary audit tools belong under ignored `artifacts/` or project `bin/`/`obj/`, never the maintained documentation tree.

Write installation instructions for released packages, using package IDs and explicit versions. Keep source-build instructions in contributor sections. Public documentation and API XML comments must be self-contained: exclude unpublished planning references, personal paths, credentials, and links to documents unavailable to readers. Describe supported behavior and limitations for the documented version.

## Verification

Validate moved paths and Markdown links, parse scripts and project metadata, build the solution, and run the affected suites. Comment-only edits must preserve executable tokens. Compiler/SDK changes also require an external package consumer. Record exact commands and skips; keep manual IDE, screen-reader and OS integration checks distinct from automated results.

### Documentation release checks

Set the framework version in `Directory.Build.props` through `NazorVersion`. Packing derives the runtime and SDK package versions, SDK metadata, and the SDK reference in the project template from that value. Optional theme packages use `NazorThemeVersion`, which retains an existing alpha label or adds `-alpha` to the framework's numeric version. Template theme references use the same value. Update versioned installation commands and examples to match their packages. Public pages describe the release's current contracts.

The repository owner chooses every version increment. Use the approved `NazorVersion` for the framework, runtime, SDK, templates, and editor extension descriptions. Keep optional theme packages in the alpha channel while native visual verification is pending. Use the framework's numeric major, minor, and patch in the Visual Studio and VS Code manifests and the VS Code lockfile.

After producing the release packages, run both checks from the repository root with the same feed used by the release. Substitute the pack output directory if it differs.

```powershell
dotnet run --project Scripts/Tests/Documentation -c Release -- . artifacts/packages
pwsh -NoProfile -File Scripts/Tests/verify-doc-tutorial.ps1 -Feed artifacts/packages
```

- The documentation check reads the packaged managed assemblies for each target framework. It checks visible types and declarations, overloads, generated members, and the shared API of the desktop hosting variants against the package references. Review the API diff for declarations removed from the packages and verify platform-specific usage contracts.
- The same check compares the NuGet index with the release feed and checks versioned installation commands, package and release links, and editor manifests against the approved versions.
- The same check compares NSS registrations, defaults, inheritance and animation flags, and shorthand expansions. It checks window-root restrictions and compiles the fenced NSS examples. Review backend prerequisites and component scope against the runtime and compiler when those change.
- The tutorial check creates an external app from the packed template and builds the exact examples in the tutorial. Keep native startup, keyboard interaction, screen-reader checks, and Linux hardware acceptance separate from a successful build or cross-publication.
- Open the deployed documentation for the release and repeat the navigation checks there. Start at the public index and follow the tutorial, API type links, and NSS applicability anchors. Check the brand image and sample assets, including path case. Resolve broken links or missing assets on the deployed site before signing off. The automated link check covers paths and Markdown anchors in the checkout.
