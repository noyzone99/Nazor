# Nazor.Themes.WindowsXP

Reference the matching alpha release of `Nazor.Themes.WindowsXP`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.WindowsXP;

app.Appearance.Theme = WindowsXPTheme.Theme;
```

Windows XP draws on the Luna era with rounded borders, blue highlights and shaded controls. It uses a light palette. [Microsoft's visual style examples](https://learn.microsoft.com/en-us/dotnet/desktop-wpf/fundamentals/styles-templates-overview) · [Windows XP screenshots](https://guidebookgallery.org/screenshots/winxppro/)

The 16 px scrollbars reserve a gutter with end buttons and a rounded blue thumb. Inputs have pale blue spin and drop-down buttons, and the theme uses shaded Nazor-drawn caption controls when the window selects them. [Win32 scrollbar anatomy](https://learn.microsoft.com/en-us/windows/win32/controls/about-scroll-bars)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-windowsxptheme"></a>

### `WindowsXPTheme`

Namespace `Nazor.Themes.WindowsXP`. [Source](../../Sources/Themes/Nazor.Themes.WindowsXP/WindowsXPTheme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class WindowsXPTheme` | Provides Windows XP-inspired control styles. |
| `public static StyleSheet Theme { get; }` | Gets the Windows XP stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.WindowsXP`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet WindowsXP { get; }` | Gets the Windows XP stylesheet compiled from `WindowsXP.nss`. |
