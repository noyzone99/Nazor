# Nazor.Themes.Windows9x

Reference the matching alpha release of `Nazor.Themes.Windows9x`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Windows9x;

app.Appearance.Theme = Windows9xTheme.Theme;
```

Windows 9x follows the Windows 95 and 98 standard scheme: a #c0c0c0 face, 8 pt sans text, navy selection and two-pixel bevels with white and black outer edges and #dfdfdf and #808080 inner edges. Accent and tonal buttons keep the face color, and the accent is bold. Nazor-drawn frames use the navy-to-blue active caption and 16 × 14 px beveled caption buttons. It uses a light palette. [Windows 95 screenshots](https://guidebookgallery.org/screenshots/win95) · [Microsoft's history of beveled buttons](https://devblogs.microsoft.com/oldnewthing/20040728-00/?p=38313)

The 16 px scrollbars reserve a gutter and show a square beveled thumb and two end buttons. Fields, check boxes and list views are white and recessed, the default button carries an extra black frame, and the progress bar fills with navy blocks. [Win32 scrollbar anatomy](https://learn.microsoft.com/en-us/windows/win32/controls/about-scroll-bars)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-windows9xtheme"></a>

### `Windows9xTheme`

Namespace `Nazor.Themes.Windows9x`. [Source](../../Sources/Themes/Nazor.Themes.Windows9x/Windows9xTheme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class Windows9xTheme` | Provides Windows 9x-inspired control styles. |
| `public static StyleSheet Theme { get; }` | Gets the Windows 9x stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.Windows9x`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet Windows9x { get; }` | Gets the Windows 9x stylesheet compiled from `Windows9x.nss`. |
