# Nazor.Themes.Windows7

Reference the matching alpha release of `Nazor.Themes.Windows7`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Windows7;

app.Appearance.Theme = Windows7Theme.Theme;
```

Windows 7 follows the Aero style with the Sky frame color: Segoe UI 9 pt, #f0f0f0 dialogs, two-tone glossy push buttons that turn blue under the pointer, soft #abadb3 field frames, button-style drop-down lists, the green progress bar and the light-blue item highlight. Nazor-drawn frames use a light-blue glass caption and border without blur, with grouped caption buttons that hang from the top edge and a wide red close button. It uses a light palette. [Microsoft's visual style examples](https://learn.microsoft.com/en-us/windows/win32/controls/visual-styles-overview)

The 17 px scrollbars reserve a pale track with end buttons; the thumb turns light blue during pointer interaction. Menus keep the gray icon gutter. [Aero scrollbar states](https://learn.microsoft.com/en-us/windows/win32/controls/aero-style-classes-parts-and-states)

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
