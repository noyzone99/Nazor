# Visual Studio icons

The project icon uses a window outline and the Nazor four-leaf mark. A folded document with the mark identifies components. A folded document with blue braces and rules identifies stylesheets. The [Nazor identity](../../../../Docs/Assets/README.md) supplies the extension's marketplace icon.

The component and stylesheet document outlines use Microsoft Visual Studio ImageCatalog geometry, attributed to Microsoft Corporation. See the [Visual Studio Image Library](https://learn.microsoft.com/en-us/visualstudio/designers/the-visual-studio-image-library?view=visualstudio) and [Image Service and Catalog](https://learn.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog?view=visualstudio).

`Light`, `Dark`, `HighContrastLight` and `HighContrastDark` are explicit Image Service source variants. The compiled XAML vectors share the same geometry across themes. High-contrast variants use a single foreground color. The stylesheet braces use filled outlines with bevel joins.

The [image manifest](../NazorIcons.imagemanifest) assigns project `1`, component `2` and stylesheet `3` under GUID `df813fe5-43b7-40ab-9ba5-4ac6892fdbb6`. Both `.nss` and `.nazor.nss` use the stylesheet association. The [CPS provider](../NazorProjectIcons.cs) applies the project moniker to collapsed and expanded roots with the `Nazor & CSharp` capabilities.

The component and stylesheet item templates use PNG renders of the light vectors. The extension registers a binding path so Image Service can resolve the vectors before package load. The editor build checks VSIX contents, theme and DPI rendering, cold image resolution and project-root selection.
