# Nazor for Visual Studio Code

Editor setup, capabilities, and hot reload are documented in the [developer guide](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/26-Editors-and-Hot-Reload.md).

The extension supports `.nazor` components and `.nss` stylesheets through TextMate grammars and a bundled language server. It uses the .NET executable on `PATH`; set `nazor.dotnetPath` in VS Code settings to select another executable.

The extension for Nazor `0.1.0-alpha` uses manifest version `0.1.0`. It bundles C# 15 Roslyn/Razor services for `.nazor` components while the language server runs on .NET 10 LTS. Generation and editor analysis follow the project's evaluated language version, conditional symbols and references. Rebuild after changing project settings. Both completion and highlighting use the supported NSS selectors. [NSS selector guide](https://github.com/noyzone99/Nazor/blob/main/Docs/Guide/17-Selectors-and-Media-Queries.md)

The extension supplies light and dark icons for components and stylesheets. File icon themes can use these language icons or supply their own.

To build the distribution from source, run `pwsh -NoProfile -File ./Scripts/Build/build-editors.ps1` from the repository root. See the [script reference](https://github.com/noyzone99/Nazor/blob/main/Scripts/README.md#build-and-packaging) for prerequisites and outputs.

## DevTools (Windows and Linux desktop)

Build the app with `NazorEnableDevTools=true` and call `builder.UseDevTools()` inside `#if NAZOR_DEVTOOLS` (namespace `Nazor`). Press plain F12 in the running app or run **Nazor: Open DevTools** from the command palette. The Agent package supplies the inspector for Windows/Linux x64; install the .NET 10 runtime. The command opens the external inspector for a window of the active debug session, or lets you choose a running opted-in window. It reuses the inspector for that session, including one opened through F12. The debugger bridge is advertised automatically during a coreclr session. It works without opening a `.nazor` file first.

For current-stop fields, use Microsoft's C# extension and a `coreclr` launch/attach configuration with `"nazorDevTools": true`. The Nazor configuration provider disables implicit function evaluation, `ToString` and fast evaluation, and enables raw values for that session. Restart debugging after changing the configuration. Without these validated options, fields are explicitly unavailable; retained data stays navigable. Use the normal debugger controls to step/continue. Breakpoints never resume implicitly. Browser/remote extension hosts and other C# adapter types are outside the tested desktop workflow.

Verified with C# 2.160.4, VS Code 1.141.2 on Windows and native VS Code 1.143.1 on Fedora 44/WSLg, with .NET 10 targets. The Razor/NSS compiler, language server and inspector use .NET 10 LTS. Source builds locate the inspector under `artifacts/devtools-host` on Windows or `artifacts/devtools-host-linux` on Linux; bundled app payloads take precedence.
