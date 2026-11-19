# 21. Typography and fonts

Text uses HarfBuzz shaping, glyph fallback by grapheme, and Skia rendering. The default family is Segoe UI at 14 logical pixels. Text properties such as color, font, line height, alignment, and white-space generally inherit.

The Fluent theme prefers Segoe UI Variable, Segoe UI Variable Text and Segoe UI, followed by installed sans-serif fonts. The Skia font provider converts the requested logical pixel size to points (72/96) for a variable font's `opsz` axis, then clamps it to the font's supported range, following [DirectWrite optical sizing](https://github.com/microsoft/WindowsAppSDK/blob/main/specs/dwritecore/DWriteCore.md). Shaping and rasterization use the same variation coordinates. Static and private fonts retain their existing fallback behavior.

Basic gives `Heading` levels 1–6 bold text (weight 700) and browser-like sizes of `2em`, `1.5em`, `1.17em`, `1em`, `0.83em` and `0.67em`. These sizes follow the parent's font size and can be overridden by application styles. None supplies no heading typography.

## Private fonts

Place the required faces in the application and register them before startup:

```csharp
builder.Fonts.Add("Fonts/FiraSans-Regular.ttf", "Fonts/FiraSans-Bold.ttf");
```

```css
:root { font-family: "Fira Sans", sans-serif; }
.heading { font-size: 24px; font-weight: bold; }
```

The NSS family is the font's internal family name, not the filename. Include the necessary weight/italic faces and their distribution license. Relative paths start at `AppContext.BaseDirectory`. A failed private-font load stops startup with the offending path.

The SDK copies TTF, OTF, and TTC files to output and publish paths. `FontOptions.DefaultFamily` sets the last fallback; `UseSystemFonts` controls system discovery. Names such as `sans-serif` are treated as family names rather than a complete browser-style generic-family mapping. `@font-face` is unsupported.

The [Windowing sample](../../Samples/Nazor.Samples.Windowing/README.md) demonstrates linked font assets and publish verification. Font configuration is renderer-independent application code.

## Linux font discovery

Applications using system font discovery need fontconfig **and at least one
installed font family**. Fontconfig manages discovery and caching; it does not
provide a font family itself. A minimal Linux installation may contain no fonts.
Self-contained .NET deployment does not supply system fonts either. Application
private fonts registered as above are an alternative to installing system fonts.

If startup fails with `No font is available.`, install `fontconfig` and a family
using the distribution's package manager. Example DejaVu packages:

| Distribution | Font package |
| --- | --- |
| Debian, Ubuntu, Linux Mint, Pop!_OS, Zorin OS | `fonts-dejavu-core` |
| Fedora, RHEL, Rocky Linux, AlmaLinux, CentOS Stream | `dejavu-sans-fonts` |
| openSUSE Leap/Tumbleweed | `dejavu-fonts` |
| Arch Linux, EndeavourOS, Manjaro | `ttf-dejavu` |

The [DRM distribution package recipes](34-Linux-DRM.md#distribution-packages)
include both Fontconfig and a font family. After installation, refresh discovery
and verify that a face is available:

```bash
fc-cache -f
fc-match sans-serif
```

The last command should report an installed face, such as
`DejaVuSans.ttf: "DejaVu Sans" "Book"`. Fully restart the application after
installing fonts. If discovery still fails, run `fc-match sans-serif` and
`fc-list | head` as the same user that starts the app. Check that font files are
readable and Fontconfig can find them. These requirements apply to both Wayland
and DRM applications, with either GPU or CPU rendering. X11 uses the same Linux font discovery.

NazorLeaf ships Geist for the interface, Geist Mono for code and shortcuts, Bricolage Grotesque for headings, and NazorLeaf Icons for `LeafSymbol`. `builder.Fonts.AddNazorLeafFonts()` registers all four as application-private families from `Themes/NazorLeaf`. Register them before `Build()` and keep the font license files with the distributed assets. The text fonts are variable; the icon font is a static OpenType font. [Theme setup](16-Styling-and-Themes.md#nazorleaf) · [Typed NazorLeaf symbols](22-Images-Icons-and-Drawing.md#typed-nazorleaf-symbols)

The optional Fluent theme package ships Regular and Filled Fluent UI System Icons fonts. `builder.Fonts.AddFluentFonts()` registers both as private families from `Themes/Fluent`, without requiring a system icon font or activating a particular theme. They supply icon glyphs, not interface text. Keep the packaged license and attribution files with the TTFs. [Typed icon symbols](22-Images-Icons-and-Drawing.md#typed-fluent-symbols)

## Wrapping and whitespace

| Value | Behavior |
| --- | --- |
| `white-space: normal` | Collapse whitespace and wrap |
| `nowrap` | Collapse whitespace without wrapping |
| `pre` | Preserve spaces and newlines without wrapping |
| `pre-wrap` | Preserve spaces/newlines and wrap |
| `pre-line` | Collapse spaces, preserve newlines, and wrap |

Line breaking uses spaces and explicit newlines. `overflow-wrap: break-word` can break an otherwise oversized word between graphemes; `anywhere` additionally reduces its intrinsic minimum width. There is no full bidirectional layout, hyphenation, or complete Unicode line-breaking implementation. Text is laid out left-to-right; right alignment does not enable RTL shaping/layout semantics.

`line-height` accepts normal, a multiplier, or a length/percentage. A unitless multiplier follows inherited font sizes; resolved lengths stay lengths. `letter-spacing` adds spacing, including after the final character, without disabling ligatures.

## Truncation and decoration

```css
.filename {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    min-width: 0;
}
```

Ellipsis applies to a text-only element whose horizontal overflow clips. In a flex button, wrap the label in a `TextBlock`; an anonymous text item does not receive block text-overflow behavior. Accessibility retains the complete original text.

Text decoration supports underline, overline, and line-through; solid, double, dotted, dashed, and wavy styles; color and thickness. NSS models decoration through inheritance, preserving the declaring color. Decorations and text-transform do not modify editable field contents.

`text-transform` uses invariant-culture casing for displayed text. It is not localization. Assistive technology receives the original text. For language-specific wording or formatting, use [resources and culture](25-Localization.md).

## Selectable display text

`TextBlock IsTextSelectionEnabled="true"` allows pointer selection and copying, including word/all selection and a context menu. It focuses on click without becoming a normal Tab stop. `user-select: none` disables selection and `all` selects all on click. Selectable text has narrower alignment/truncation support, so verify that mode instead of assuming normal TextBlock rendering behavior.

API: [Nazor.Graphics](../ApiReferences/Nazor.Graphics.md), [Nazor.Renderer.Skia](../ApiReferences/Nazor.Renderer.Skia.md).

---

[Guide index](README.md) · [API reference](../API.md)
