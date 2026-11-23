# Nazor.Themes.Cde

Reference the matching alpha release of `Nazor.Themes.Cde`. Select the theme after building the application. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Cde;

app.Appearance.Theme = CdeTheme.Theme;
```

CDE uses the Common Desktop Environment's Default (Broica) palette in Light and Charcoal in Dark. These palettes supply the application, input, menu and active and inactive title bar colors. Text colors account for the original palettes' midtone surfaces. [Default palette](https://raw.githubusercontent.com/cdesktopenv/cde/master/cde/programs/palettes/Default.dp) · [Charcoal palette](https://raw.githubusercontent.com/cdesktopenv/cde/master/cde/programs/palettes/Charcoal.dp) · [CDE palette roles](https://sourceforge.net/p/cdesktopenv/wiki/Creating%20A%20Palette/)

Motif details include raised square buttons, an extra ring around default pushbuttons, recessed inputs and diamond radio marks. Dialogs use 10 px spacing and consistent minimum action widths while retaining wrapping on narrow layouts. Nazor-drawn caption buttons take the active or inactive title bar palette. [Solaris CDE Text Editor](https://docs.oracle.com/cd/E19253-01/806-4743/806-4743.pdf#page=299) · [Solaris CDE Save As](https://docs.oracle.com/cd/E19253-01/806-4743/806-4743.pdf#page=301)

Scrollbars reserve 15 px, with a square beveled thumb, recessed trough and arrows on both ends. The thumb can shrink to 6 px. Holding an arrow starts repeating after 250 ms, then repeats every 50 ms. Forced colors use the Basic system-color presentation. [Motif scrollbar resources](https://manpages.debian.org/unstable/libmotif-dev/XmScrollBar.3.en.html)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-cdetheme"></a>

### `CdeTheme`

Namespace `Nazor.Themes.Cde`. [Source](../../Sources/Themes/Nazor.Themes.Cde/CdeTheme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class CdeTheme` | Provides CDE-inspired control styles. |
| `public static StyleSheet Theme { get; }` | Gets the CDE stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.Cde`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet Cde { get; }` | Gets the CDE stylesheet compiled from `Cde.nss`. |
