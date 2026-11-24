# 30. Troubleshooting

## Package installation and restore

Check `dotnet --version` against the SDK required by [getting started](02-Getting-Started.md). A parent directory's `global.json` can select a different SDK.

If `nazor-app` is missing from the CLI, install it with `dotnet new install Nazor.Templates@0.1.1-alpha`. For **Create a new project** in Visual Studio, install **Nazor for Visual Studio** and search for **Nazor application**. [Nazor.Templates on NuGet](https://www.nuget.org/packages/Nazor.Templates/0.1.1-alpha) · [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=Noyzone99.nazorvisualstudio)

For package-not-found or SDK-resolution errors, inspect `dotnet nuget list source` and the applicable `nuget.config` files. Enable NuGet.org and verify that the selected Nazor version is available there. If you use package source mapping, map `Nazor.*` to NuGet.org or to the feed that contains your locally built packages. Restore the application after correcting the configuration. [NuGet packages](../NuGet.md#nuget-packages) · [Build a local package feed](02-Getting-Started.md#build-a-local-package-feed)

Windows and portable executables also need the Microsoft windowing packages from NuGet.org. Use Nazor.Sdk in the executable project and allow both package IDs from NuGet.org in your source mapping. [Windows dependencies](03-Projects-and-Builds.md#windows-dependencies)

## Compiler diagnostics

| Code | Meaning and action |
| --- | --- |
| `NZB001` | Compiler executable missing; restore/check the SDK package and compiler path |
| `NZB002` | The target requires a platform host that Nazor does not implement. Use a [supported desktop target](03-Projects-and-Builds.md#target-frameworks-and-native-hosts). |
| `NZB003` | The RID has no desktop backend. Select a supported Windows or Linux RID or omit it for a portable app. |
| `NZB004` | A selected Linux bridge is missing for the exact RID. Check the Nazor Linux package and `NazorLinuxNativeLibraries`. For a source build, build the required native assets. |
| `NZB005` | DevTools conflicts with AOT or production publish validation, or the project references the Agent with DevTools disabled. Remove the Agent dependency for production. For a development publication, use the documented DevTools settings. |
| `NZB008` | `NazorEnableDevTools` or `NazorEnablePublishValidation` has an invalid value. Set it to `true` or `false`. |
| `NZB009` | Inspector assets are missing for the selected RID. Restore matching `Nazor.DevTools.Agent` packages. Bundled inspector platforms are `win-x64` and `linux-x64`. |
| `NZB011` | The value of `NazorLinuxNativeLibraries` is invalid. Use `All`, `Wayland`, `X11`, or `None`. |
| `NZC000` | General compiler/I/O/input failure; inspect the full message and source path |
| `NZC001` | Component discovery contract invalid: public class, namespace, public parameterless constructor |
| `NZC002` | Parameter is not a public writable instance property; `init` is not supported |
| `NZC010`, `NZC011` | HTML element or static markup; use Nazor components and text controls |
| `NZC012` | Unsupported event modifier; supported modifiers are preventDefault and stopPropagation |
| `NZC013` | Unsupported bind modifier; supported modifiers are after, get, and set |
| `NZC014` | Unknown explicit parameter; correct the name or use an attributes dictionary for extra metadata |
| `NZC015` | Empty non-string parameter; supply a typed value/expression |
| `NZC016` | Assignment omitted for a non-bool parameter; assign a value, including explicit null when intended |
| `NZC017` | Duplicate literal ID in one declaring component; each conflicting declaration is marked. Nested component definitions have separate ID scopes |
| `NZC020` | Styles require a reference to Nazor.Style |
| `RZ10012` | Unknown component; import its namespace and build/update its reference |
| `RZ2012` | Missing EditorRequired parameter; supply it explicitly |
| Other `RZ...`, `CS...` | Razor/C# diagnostics, mapped to the original component where possible |

Nazor promotes unknown components and missing required attributes to errors. A component can be generic; a generic declaration itself is not an invalid-component error.

## Explicit startup definition errors

Select at most one initial surface with `UseMainWindow`, `CreateMainWindow` or `UseMainView`. Desktop startup requires a main window or at least one `UseTrayItem` registration. `UseMainWindow` and secondary window APIs require exactly one top-level `Window`; `CreateMainWindow` and `UseMainView` require content without a `Window`. Tray-only startup requires `OnExplicitShutdown`. Tray registrations cannot be combined with `UseMainView`. Windows desktop does not support `UseMainView`. See [application hosting](04-Application-Hosting-and-Services.md) for the current startup contracts.

## Missing platform host at startup

SDK desktop executables target .NET 8 or later and use `NazorApplication.CreateBuilder(args)` with an automatically registered `DesktopPlatformHost`. If startup reports that no host is configured, check the project SDK, executable output type and `NazorImplicitRuntimeReference`. For explicit host selection, reference the appropriate desktop package and follow the [Windows dependency setup](03-Projects-and-Builds.md#windows-dependencies) if applicable. If `DesktopPlatformHost.Run` reports an unsupported OS, check that the publication includes that backend. Omit the RID for both desktop backends or select the matching Windows or Linux RID. `NZB003` identifies an unsupported desktop RID. `NZB004` identifies a missing Linux bridge for the exact RID. `NZB011` rejects an unsupported `NazorLinuxNativeLibraries` value. [Application hosting](04-Application-Hosting-and-Services.md)

## Linux desktop startup

Check that the publication contains the selected `libnazor-wayland.so` and/or `libnazor-x11.so` for its exact architecture/libc and that the bridge implements ABI version 5. `NazorLinuxNativeLibraries=All|Wayland|X11|None` controls distributed assets. `None` requires an explicit host such as `DrmPlatformHost`; it never causes automatic display takeover.

`NAZOR_LINUX_BACKEND` accepts exactly `X11` or `Wayland`, overrides constructor/session selection and disables startup fallback. Unset it to restore automatic selection; an empty value is invalid. Wayland needs a reachable compositor through its display/socket settings; X11 needs a reachable `DISPLAY` and its system libraries. With Auto, `All` and no override, a Wayland connection failure attempts X11; if both fail, inspect both errors in the inner `AggregateException`. Callback and event-loop failures are never retried. See [selection and dependencies](35-Linux-X11.md).

`NAZOR_RENDERER=AUTO|GPU|CPU` controls rendering independently. For a font discovery failure, follow [Linux fonts](21-Typography-and-Fonts.md#linux-font-discovery). For native load failures after copying a publication, also check [directory permissions](33-Linux-Wayland.md#file-access-after-copying-to-linux).

## NSS diagnostics

| Code | Meaning |
| --- | --- |
| `NZS001` | `@style` has no opening brace |
| `NZS002` | Unterminated style block, string, or comment |
| `NZS003` | Style block placed inside markup or code instead of top-level |
| `NZS100` | Rule/declaration syntax error |
| `NZS101` | Unknown property, often with a suggested spelling |
| `NZS102` | Invalid value/unit/expression/grid specification |
| `NZS103` | Invalid selector or combinator/pseudo-element usage |
| `NZS104` | Unsupported/malformed at-rule or media query; invalid keyframe placement |
| `NZS105` | `!important` is unsupported |
| `NZS106` | Top-level `&` or initial combinator without a parent rule |
| `NZS107` | Unsupported pseudo-class |
| `NZS108` | Invalid keyframe name/frame/declaration |
| `NZS109` | `Window` is not a supported NSS type selector; use `:root` for the current window |
| `NZS110` | Standalone stylesheet filenames produce a duplicate property name |
| `NZS111` | Component sidecar treated as a standalone stylesheet |
| `NZS112` | Warning: property does not apply to the statically known selector subject; use an applicable property or target |

NSS errors refer to source file/line/column and compilation continues collecting further diagnostics. A valid CSS construct is not necessarily supported NSS. Check [syntax](18-NSS-Syntax-and-Values.md), [selectors](17-Selectors-and-Media-Queries.md), and the [property catalog](19-NSS-Property-Reference.md).

## Watcher and IDE diagnostics

`NZW001` means a source change was not applied because compilation failed. `NZW002` means the app lacks .NET hot-reload support, so only styles can apply live. `NZW003` means no matching running app was found. Start the app in Debug with its launch profile and development channel, then run the matching project/configuration watcher.

`NZVS002`/`NZVS003` report IDE refresh/delivery conditions, including an unavailable style channel; read the specific message in Error List and Nazor Hot Reload output. An editor warning does not automatically mean the normal build failed. C# rude edits are handled by the .NET debugger and may require restart.

## Styles do not apply

First check activation: a standalone sheet must be assigned. Then inspect level, specificity, scope, variable values, and media conditions. A parent sheet does not automatically style child-component internals. Use diagnostics Trace to inspect matched rules and winning declarations. Check that a custom component qualifier is not being mistaken for a real element.

For an invisible border, check both width and style. For unexpected hidden interaction, remember opacity zero still receives input. For native caption height, read the measured variable only when native integrated controls are visible and provide a fallback when absent.

## Content does not shrink or scroll

Give the scroll viewport a constrained size; check ancestor flex minimums and set min-height/min-width to zero where needed. Long unbreakable text can impose an intrinsic minimum; use overflow-wrap or a zero-minimum grid track. `gap` does not apply in block layout. A scrolling Container does not virtualize its child components.

## UI does not refresh

Check state ownership and notification. Component events request renders; external service events and timer callbacks need dispatcher transfer plus StateHasChanged. Mutating an arbitrary object does not automatically notify consumers. A notifying cascading source needs NotifyChangedAsync. Avoid stale asynchronous results overwriting a newer state.

## Package restore and editor metadata

Build the app after changing referenced projects/packages. Rebuild and reinstall the editor extension when its bundled server is outdated, then restart the editor/component tabs. A source build does not replace installed extension files.

Confirm the SDK version in the project declaration and the versions of explicit package references, then restore and rebuild. If an extracted package is damaged, use `dotnet nuget locals global-packages --list` to find the cache, close processes using it, remove only the affected package/version directory, and restore. A fresh project directory still uses the same NuGet cache.

## Runtime failures and platform behavior

Component/dispatcher exceptions are logged under the Nazor category, retained in `NazorWindow.Exceptions`, and affect the exit code. When reporting a problem, include the exception type, message, stack trace, package/SDK versions, and a minimal reproduction; redact credentials, personal paths, and application data first. Check font paths, resource names, current working directory assumptions, and service lifetimes before changing framework internals.

Fullscreen, native caption styling, backdrops, tray visibility, and notifications depend on backend and desktop policy. Use the Capabilities and Desktop services tabs in the [Windowing sample](../../Samples/Nazor.Samples.Windowing/README.md) to inspect the current backend and exercise its native requests.

---

[Guide index](README.md) · [API reference](../API.md)
