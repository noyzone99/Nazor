# 1. Introduction

Nazor lets .NET developers build desktop interfaces with Razor component syntax, C# application logic, and NSS stylesheets. A component describes the controls that should exist for its current state. The runtime retains the resulting element tree, updates it when state changes, resolves styles, lays out the interface, and draws it with Skia. HarfBuzz shapes text; Windows and Linux desktop backends supply native windows, desktop integration and input, with UI Automation on Windows and AT-SPI on Linux.

## Goals

The framework brings a familiar component workflow to desktop development: small reusable components, typed parameters and callbacks, dependency injection, scoped styles, and a build that reports errors against the original source. Layout and appearance belong in NSS; business operations belong in ordinary C# services. The host manages application lifetime, while each window owns a component tree and a service scope.

Nazor aims to make the same UI understandable through several views: the component source, the retained elements, the calculated layout, the rendered image, and the accessibility tree. Headless rendering and diagnostic APIs make those stages observable in tests and development tools.

The core packages separate application behavior from backend-specific window operations. The repository implements Windows, [Linux Wayland](33-Linux-Wayland.md), [Linux X11](35-Linux-X11.md) and an explicit [DRM/KMS single-view host](34-Linux-DRM.md). Capabilities and verification differ by backend; see [support and limitations](32-Support-and-Limitations.md). macOS and mobile hosts remain unimplemented.

## What “native UI” means here

On desktop, the application runs in native OS windows and uses the available platform services for input, window management, the clipboard, file dialogs, tray services, and accessibility. Nazor lays out and draws its own application controls with Skia. The control pipeline needs no WebView, DOM, JavaScript runtime, or wrapped OS widgets. System caption buttons and file dialogs can still use the native platform UI. Font selection, DPI, and native decorations can affect the final image. [Backend capabilities](32-Support-and-Limitations.md#backend-support-matrix)

Razor provides the authoring syntax. Nazor's component runtime provides `ComponentBase`, parameters, callbacks, fragments, and rendering. Familiar Blazor patterns apply where documented, but browser-specific components and APIs do not become available by using Razor syntax. Use `Container`, `TextBlock`, and other Nazor controls instead of `div`, `span`, or HTML inputs.

## Three source files, three responsibilities

| File | Responsibility |
| --- | --- |
| `TaskCard.nazor` | Composes controls, binds values, and connects callbacks |
| `TaskCard.nazor.cs` | Optional C# partial class for component behavior |
| `TaskCard.nazor.nss` | Scoped appearance and layout for that component |

Small components can keep C# in `@code` and NSS in `@style`. Larger components can use sidecar files without changing their public contract. An application stylesheet such as `Theme.nss` supplies shared rules and tokens.

## Concepts for Razor and Avalonia developers

Blazor is a reference for component composition and lifecycle. Avalonia is a reference for sharing control rendering across native platform backends. The table maps familiar authoring concepts to Nazor. [Blazor components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/) · [Avalonia cross-platform architecture](https://docs.avaloniaui.net/docs/fundamentals/cross-platform-architecture)

| Concept | From Razor and Blazor | From Avalonia | In Nazor |
| --- | --- | --- | --- |
| UI declaration | `.razor` components with HTML and C# expressions | AXAML views and controls | `.nazor` components declare Nazor controls with Razor syntax and C# expressions. `@window` declares the native window shell. |
| Inputs and events | `[Parameter]`, `EventCallback`, `@bind` | Styled or direct properties, bindings and commands | Typed parameters and callbacks connect components. `@bind-Value` pairs `Value` with `ValueChanged`. Bind to a field, a property or an explicit setter. |
| State and lifecycle | Component fields and `OnInitialized` | View model, data context and view lifecycle | Keep drafts in a component and shared state in an injected service. Components request a render as state changes. Release subscriptions when a component is disposed. |
| Layout | CSS on DOM elements | Panels such as `StackPanel` and `Grid` | Use `Container` with NSS block, flex or grid layout. The layout engine measures retained elements in logical pixels. |
| Styling | App stylesheet and component-scoped CSS | Selectors, styles and resources | NSS supplies global and component-scoped rules. A `.nazor.nss` sidecar targets elements declared by its component. Design tokens are NSS custom properties. |
| Navigation | A route selects a component | A view host selects a view | `@page` declares a route. A `Frame` in the native window displays the routed component. |
| Platform services | Browser or server services | Desktop services and storage providers | Inject window and platform services for work such as file picking. Check the capabilities of the selected backend before offering a platform-specific action. |

The component tree records instances and their state. The retained element tree supplies the boxes and interactive parts used for layout, drawing and accessibility. A component can declare several elements or delegate its rendering to other components. Read the [developer guide](README.md) for topic references or build the [project workspace](38-First-Desktop-Application.md) to practice these concepts together.

## From input to pixels

1. The platform translates native input into Nazor events.
2. A callback updates component or service state.
3. The component renders and the runtime reconciles retained elements.
4. Styles and layout update where required.
5. The scene produces a display list and Skia paints damaged regions.
6. The platform presents the result and exposes semantic information to assistive technology.

An idle window does not continuously redraw. Animations, input, asynchronous results, resizing, and other invalidations request the frames they need.

## Learning path

Start with [getting started](02-Getting-Started.md), then follow the [first application tutorial](38-First-Desktop-Application.md). Use [project structure](03-Projects-and-Builds.md), the [component model](05-Components-and-Razor.md), the [control catalog](09-Control-Catalog.md) and the [styling chapter](16-Styling-and-Themes.md) when you need more detail. The [samples](../../Samples/README.md) demonstrate complete applications.

Nazor is in pre-alpha. Expect breaking changes as the framework develops. See [support and limitations](32-Support-and-Limitations.md) for the current platform and feature boundaries.

---

[Guide index](README.md) · [API reference](../API.md)
