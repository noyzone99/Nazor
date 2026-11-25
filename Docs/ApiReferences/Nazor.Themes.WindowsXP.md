# Nazor.Themes.WindowsXP

Reference the matching alpha release of `Nazor.Themes.WindowsXP`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.WindowsXP;

app.Appearance.Theme = WindowsXPTheme.Theme;
```

Windows XP follows the Luna Blue visual style: a #ece9d8 face, Tahoma 8 pt, #003c74 rounded push buttons with an orange hover glow and a blue default glow, #7f9db9 field frames, green check marks and progress blocks, and #316ac5 selection. Nazor-drawn frames use the glossy blue caption with rounded top corners and white-bordered caption buttons, with a red close button. It uses a light palette. [Microsoft's visual style examples](https://learn.microsoft.com/en-us/dotnet/desktop-wpf/fundamentals/styles-templates-overview) · [Windows XP screenshots](https://guidebookgallery.org/screenshots/winxppro/)

The 17 px scrollbars reserve a gutter with end buttons and a rounded pale blue thumb. Drop-down and spin buttons use the same pale blue, and selected tabs carry the orange top line. [Win32 scrollbar anatomy](https://learn.microsoft.com/en-us/windows/win32/controls/about-scroll-bars)

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
