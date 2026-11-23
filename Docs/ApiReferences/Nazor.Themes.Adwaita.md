# Nazor.Themes.Adwaita

Reference the matching alpha release of `Nazor.Themes.Adwaita`. Register the packaged fonts before building the application, then select the theme. [Theme package versions](../Guide/16-Styling-and-Themes.md#theme-package-versions)

```csharp
using Nazor.Themes.Adwaita;

builder.Fonts.AddAdwaitaFonts();
using var app = builder.Build();
app.Appearance.Theme = AdwaitaTheme.Theme;
```

The theme follows the libadwaita 1.10.0 control palette and measurements from GNOME 51. The light and dark palettes follow the application's resolved color scheme. Accent buttons use a darker blue for label contrast. The 3 px overlay thumb expands to 8 px when the pointer enters its scrollbar lane or drags the thumb. [GNOME 51 applications](https://release.gnome.org/51/) · [Libadwaita 1.10.0 color definitions](https://raw.githubusercontent.com/GNOME/libadwaita/1.10.0/src/stylesheet/_colors.scss) · [Libadwaita 1.10.0 scrollbar metrics](https://raw.githubusercontent.com/GNOME/libadwaita/1.10.0/src/stylesheet/widgets/_scrolling.scss)

The package copies the GNOME 51 release of Adwaita Sans and Adwaita Mono to `Themes/Adwaita/` in build and publish output, together with the SIL Open Font License and source notice. Adwaita Sans ships variable regular and italic faces. Adwaita Mono ships regular, italic, bold and bold italic faces. [Adwaita Fonts 51.0](https://download.gnome.org/sources/adwaita-fonts/51/)

See [desktop-inspired themes](../Guide/16-Styling-and-Themes.md#desktop-inspired-themes) for scheme selection, font discovery and forced colors.

## API signature index

<a id="type-adwaitatheme"></a>

### `AdwaitaTheme`

Namespace `Nazor.Themes.Adwaita`. [Source](../../Sources/Themes/Nazor.Themes.Adwaita/AdwaitaTheme.cs)

| Declaration | Contract |
| --- | --- |
| `public static class AdwaitaTheme` | Provides control styles based on Libadwaita 1.10 and registers the packaged Adwaita fonts. |
| `public const string AssetDirectory = "Themes/Adwaita"` | Gets the application-relative directory containing the fonts and their license and source notices. |
| `public static IReadOnlyList<string> FontFiles { get; }` | Gets the packaged Adwaita Sans and Adwaita Mono font paths. |
| `public static FontOptions AddAdwaitaFonts(this FontOptions fonts)` | Registers any missing private font paths before startup and returns the same options. Throws `ArgumentNullException` if `fonts` is null. |
| `public static StyleSheet Theme { get; }` | Gets the Adwaita stylesheet layered over Basic. |

<a id="type-stylesheets"></a>

### `StyleSheets`

Namespace `Nazor.Themes.Adwaita`.

| Declaration | Contract |
| --- | --- |
| `public static class StyleSheets` | Provides the project's compiled standalone stylesheets. |
| `public static StyleSheet Adwaita { get; }` | Gets the Adwaita stylesheet compiled from `Adwaita.nss`. |
