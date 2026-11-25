# Nazor.Themes.Breeze

Reference the matching alpha release of `Nazor.Themes.Breeze`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Breeze;

app.Appearance.Theme = BreezeTheme.Theme;
```

Breeze follows KDE's Qt Widgets style with 5 px control corners, tinted default actions, outlined editors and 20 px selection indicators. It prefers the installed Noto Sans font at 10 pt. The light and dark palettes follow the application's resolved color scheme. Nazor-drawn frames use the header-colored title bar with a centered title and the round Breeze caption buttons; toolbars sit on the window color above a separator, and views use the view color with accent-tinted hover and selection. [KDE Breeze palettes](https://github.com/KDE/breeze/tree/master/colors) · [KDE widget metrics](https://github.com/KDE/breeze/blob/master/kstyle/breezemetrics.h) · [Plasma screenshots](https://kde.org/announcements/plasma/5/5.23.0/)

Scrollable controls reserve a 21 px rail with a centered 8 px thumb and a minimum thumb length of 20 px. Hovering the thumb highlights it. The default configuration leaves out the arrow buttons. [KDE scrollbar rendering](https://github.com/KDE/breeze/blob/master/kstyle/breezestyle.cpp#L5805-L5864) · [KDE widget defaults](https://github.com/KDE/breeze/blob/master/kstyle/breeze.kcfg#L67-L74)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-breezetheme"></a>

### `BreezeTheme`

Namespace `Nazor.Themes.Breeze`. [Source](../../Sources/Themes/Nazor.Themes.Breeze/BreezeTheme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class BreezeTheme` | Provides Breeze-inspired control styles. |
| `public static StyleSheet Theme { get; }` | Gets the Breeze stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.Breeze`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet Breeze { get; }` | Gets the Breeze stylesheet compiled from `Breeze.nss`. |
