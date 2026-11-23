# Nazor.Themes.Windows7

Reference the matching alpha release of `Nazor.Themes.Windows7`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Windows7;

app.Appearance.Theme = Windows7Theme.Theme;
```

Windows 7 draws on Aero with pale blue-gray surfaces, soft gradients and blue interaction states. It uses a light palette. [Microsoft's visual style examples](https://learn.microsoft.com/en-us/windows/win32/controls/visual-styles-overview)

The 16 px scrollbars reserve a pale track with end buttons. Their narrower thumb changes from slate blue to cyan during pointer interaction. Fields have cool blue hover borders, and Nazor-drawn caption controls use a light gradient. [Aero scrollbar states](https://learn.microsoft.com/en-us/windows/win32/controls/aero-style-classes-parts-and-states)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-windows7theme"></a>

### `Windows7Theme`

Namespace `Nazor.Themes.Windows7`. [Source](../../Sources/Themes/Nazor.Themes.Windows7/Windows7Theme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class Windows7Theme` | Provides Windows 7-inspired control styles. |
| `public static StyleSheet Theme { get; }` | Gets the Windows 7 stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.Windows7`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet Windows7 { get; }` | Gets the Windows 7 stylesheet compiled from `Windows7.nss`. |
