# Nazor identity

Nazor uses a four-leaf mark in blue and light blue. The standalone mark identifies the framework. The blue tile identifies Nazor applications.

| Asset | Use |
| --- | --- |
| [nazor.svg](nazor.svg) | Vector mark for documentation and scalable artwork |
| [nazor.png](nazor.png) | Transparent 128 × 128 mark for NuGet and the editor extensions |
| [nazor-app.svg](nazor-app.svg) | Vector source for the blue application tile |
| [nazor-app.png](nazor-app.png) | Transparent 128 × 128 application tile for image surfaces |
| [nazor.ico](nazor.ico) | Windows application tile with 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixel images |
| [gallery.png](gallery.png) | The [Gallery sample](../../Samples/Nazor.Samples.Gallery/README.md) captured on Windows with the optional NazorLeaf theme |

Generate the framework PNG from the mark SVG and the application PNG and ICO from the tile SVG. Keep the square canvas and transparent padding.

`Sources/Directory.Build.props` includes the mark PNG in packable framework projects. The Visual Studio and VS Code extension builds use the same PNG. The [Visual Studio icon catalog](../../Sources/Tools/Nazor.VisualStudio/Icons/README.md) defines the project and file artwork. The [DevTools assets](../../Sources/Tools/Nazor.DevTools/Assets/README.md) define the inspector identity.

`Nazor.Sdk` embeds the application ICO in Windows executables by default. The Windows backend uses the executable's icon for native windows. See [application icon configuration](../Guide/03-Projects-and-Builds.md#windows-application-icon) for app-specific artwork.

`Nazor.Platform.Linux` embeds `nazor-app.png` as its desktop application icon source. The standard Nazor title bar uses this tile unless `Window.Icon` supplies an override; no separate image file is needed at deployment.
