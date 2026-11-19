# Developer guide

Start with [Getting started](02-Getting-Started.md) and follow the [first application tutorial](38-First-Desktop-Application.md) from the template to a published desktop app. If you already know Razor, Blazor or Avalonia, use the [concept map](01-Introduction.md#concepts-for-razor-and-avalonia-developers) to connect familiar ideas to Nazor. The remaining chapters are organized by task and link to package references and runnable examples where appropriate.

Examples use English names and UI text. Snippets labeled as a file can be placed in that file; shorter snippets demonstrate a particular API and assume the surrounding application or component described in the chapter.

| Chapter | Subject |
| --- | --- |
| [1. Introduction](01-Introduction.md) | What Nazor is, its goals, and its programming model |
| [2. Getting started](02-Getting-Started.md) | Prerequisites, package installation, templates, and a first application |
| [3. Projects and builds](03-Projects-and-Builds.md) | Project structure, SDK, application identity, platform files, imports, generated files, and assets |
| [4. Application hosting and services](04-Application-Hosting-and-Services.md) | Startup, configuration, logging, DI, and shutdown |
| [5. Components and Razor](05-Components-and-Razor.md) | `.nazor` syntax, directives, namespaces, and code-behind |
| [6. Parameters, events, and binding](06-Parameters-Events-and-Binding.md) | Component contracts, callbacks, binding, and captured attributes |
| [7. Generics, templates, and cascading values](07-Generics-Templates-and-Cascading-Values.md) | Reusable typed components and shared context |
| [8. Lifecycle, state, and threading](08-Lifecycle-State-and-Threading.md) | Rendering, async work, state ownership, and disposal |
| [9. Control catalog](09-Control-Catalog.md) | Every built-in control and how to choose it |
| [10. Text input and data controls](10-Text-Input-and-Data-Controls.md) | Editing, selection, dates, virtualization, and progress |
| [11. Commands, menus, and popups](11-Commands-Menus-and-Popups.md) | Commands, overlays, dismissal, and focus |
| [12. Navigation](12-Navigation.md) | Pages, routes, history, and navigation guards |
| [13. Windows and dialogs](13-Windows-and-Dialogs.md) | Window options, ownership, modal results, and file dialogs |
| [14. Title bars and platform integration](14-Title-Bars-and-Platform-Integration.md) | Custom chrome, capabilities, backdrops, and full screen |
| [15. Tray and system notifications](15-Tray-and-System-Notifications.md) | Tray items, notifications, application launcher tasks and badges |
| [16. Styling and themes](16-Styling-and-Themes.md) | NSS files, cascade, scope, themes, and design tokens |
| [17. Selectors and media queries](17-Selectors-and-Media-Queries.md) | Component boundaries, pseudo-elements, `::deep`, nesting, and responsive styling |
| [18. NSS syntax and values](18-NSS-Syntax-and-Values.md) | Units, colors, variables, and mathematical expressions |
| [19. NSS property reference](19-NSS-Property-Reference.md) | Supported longhands, shorthands, defaults, and native properties |
| [20. Layout and scrolling](20-Layout-and-Scrolling.md) | Block, flex, grid, positioning, sizing, and overflow |
| [21. Typography and fonts](21-Typography-and-Fonts.md) | Text layout, private fonts, wrapping, and truncation |
| [22. Images, icons, and drawing](22-Images-Icons-and-Drawing.md) | Assets, typed font symbols, SVG, gradients, shadows, transforms, and DPI |
| [23. Transitions and animations](23-Transitions-and-Animations.md) | Motion syntax, interpolation, timing, and reduced motion |
| [24. Input and accessibility](24-Input-and-Accessibility.md) | Keyboard, pointer, focus, semantics, and announcements |
| [25. Localization](25-Localization.md) | Resources, culture, built-in strings, dates, and numbers |
| [26. Editors and hot reload](26-Editors-and-Hot-Reload.md) | Visual Studio, VS Code, development channel, and live updates |
| [27. Testing and diagnostics](27-Testing-and-Diagnostics.md) | Headless rendering, deterministic tests, inspection, and performance |
| [28. Libraries, packaging, and deployment](28-Libraries-Packaging-and-Deployment.md) | Reusable libraries, resources, packages, single-file publishing, trimming, and Native AOT |
| [29. Application design and best practices](29-Application-Design-and-Best-Practices.md) | Maintainable structure, state boundaries, and a practical checklist |
| [30. Troubleshooting](30-Troubleshooting.md) | Compiler diagnostics and common runtime or tooling problems |
| [31. Architecture and extension points](31-Architecture-and-Extension-Points.md) | Rendering pipeline, package responsibilities, and custom hosts |
| [32. Support and limitations](32-Support-and-Limitations.md) | Implemented features, unsupported features, and validation boundaries |
| [33. Linux Wayland](33-Linux-Wayland.md) | Direct Wayland backend, desktop services, dependencies and VM validation |
| [34. Linux DRM/KMS](34-Linux-DRM.md) | Single-view hosting without a desktop, GBM/EGL rendering, input, seat ownership and verification |
| [35. Linux X11](35-Linux-X11.md) | Native X11 desktop, backend selection, system dependencies and verification |
| [36. Control composition](36-Advanced-Controls-and-Composition.md) | Control selection, content slots, NSS parts, and reusable interaction |
| [37. Semantic primitives](37-Semantic-Primitives.md) | Document lists/tables, fields/forms, coordinated choices, dialogs/tooltips/menus and reusable resizing |
| [38. Build your first desktop application](38-First-Desktop-Application.md) | Reusable components, validation, shared state, routes, accessibility, assets, and deployment |

For exact signatures, see the [API reference](../API.md). For an end-to-end project, read the [Tasks sample](../../Samples/Nazor.Samples.Tasks/README.md).

For .NET 11 setup and migration, see [.NET 11 applications](03-Projects-and-Builds.md#net-11-applications). Window interop is covered by [native window references](13-Windows-and-Dialogs.md#native-window-interop), with [automatic content scrolling](20-Layout-and-Scrolling.md#scrolling) configured through NSS.

See [framework-specific dependencies](28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions) for current managed/native package selection and the .NET 8 compatibility boundary.

The [build tools](03-Projects-and-Builds.md#build-tools-and-application-targets) run on .NET 10 LTS while applications and component libraries can target .NET 8 or later.
