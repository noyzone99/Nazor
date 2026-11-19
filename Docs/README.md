# Nazor documentation

<img src="Assets/nazor.svg" alt="Nazor four-leaf logo" width="64" height="64">

Nazor is a .NET UI framework that compiles Razor components and NSS styles into native desktop applications. Start with the [developer guide](Guide/README.md), then use the [package API reference](API.md) for signatures and contracts.

The guide covers Nazor `0.1.0-alpha`, a public pre-alpha preview. Expect breaking changes between releases. Windows x64 is the primary verification baseline. See the [support guide](Guide/32-Support-and-Limitations.md) for Linux backends, architecture, libc and hardware coverage.

| Documentation | Purpose |
| --- | --- |
| [Developer guide](Guide/README.md) | A complete learning path: goals, setup, components, styling, controls, integration, testing, and deployment |
| [API reference](API.md) | Public and protected APIs, grouped by framework package |
| [NSS property reference](Guide/19-NSS-Property-Reference.md) | Property syntax, defaults, inheritance, animation, and applicability |
| [Control composition](Guide/36-Advanced-Controls-and-Composition.md) | Control selection, content slots, NSS parts, and reusable interaction |
| [Semantic primitives](Guide/37-Semantic-Primitives.md) | Structure, state, composition, keyboard, and accessibility contracts |
| [ApiReferences](ApiReferences) | Individual package reference pages linked from the API index |
| [Nazor identity](Assets/README.md) | Framework mark, application icon and editor artwork |
| [Standalone DevTools](DevTools.md) | Optional bundled inspector, F12 and IDE launch, retained captures and debugger integration |
| [DevTools protocol](DevTools-Protocol.md) | Versioned detached data, JSON-RPC profile, limits and independent client fixtures |
| [Samples](../Samples/README.md) | Runnable applications with English explanations of design decisions |

[Repository conventions](Repository-Conventions.md) define language, naming, source comments, and documentation ownership.

For repository maintenance, use the [source map](../Sources/README.md), [test map](../Tests/README.md), and [script reference](../Scripts/README.md). `Guide/` is organized by topic; `ApiReferences/` is organized by package. Maintain current usage in the guide and member contracts in the API reference.
