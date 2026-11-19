# 18. NSS syntax and values

## Syntax and case

Rules contain `property: value;` declarations. The final semicolon is optional. Comments use `/* ... */`; `//` is not an NSS comment. Strings can use single or double quotes and support the quoted escapes below. Identifiers have no escape syntax. `url()` accepts a quoted URL or a whitespace-free unquoted URL; only the quoted form decodes string escapes.

Property names, keywords, function names, named colors, pseudo-class names, and media feature names are case-insensitive. Element/component type selectors, classes, IDs, custom property names, and keyframe names are case-sensitive.

Numbers support signs, leading decimal points, and exponents. Units normalize case. A nonzero length needs a unit; `width: 10` is invalid while `width: 10px` and `width: 0` are valid.

## Quoted strings

Backslash followed by one to six hexadecimal digits denotes a Unicode scalar: `"\E004"` selects a private-use glyph and `"\1F600"` denotes an emoji. One following CSS whitespace unit terminates the escape and is consumed; CRLF counts as one. Use a terminator or six digits when the next character could be hexadecimal: `"\41 B"` means `"AB"`, while `"\41B"` denotes U+041B.

`\"`, `\'`, and `\\` preserve the corresponding character. Backslash followed by LF, CR, CRLF, or form feed continues the string without adding text. A raw line break is invalid. Hex escapes for zero, surrogates, or values above U+10FFFF become U+FFFD, as do literal NULL and unpaired UTF-16 surrogates in strings. Token positions continue to refer to the original UTF-16 source.

NSS keeps strict diagnostics for missing closing quotes, including a trailing backslash at EOF; it does not adopt CSS's EOF string recovery. Escaped identifiers and full unquoted URL escape syntax remain unsupported. The compiler, runtime variables, formatter, and editor tools share these quoted-string rules. Token serialization safely preserves decoded backslashes, quotes, and controls.

When migrating older NSS, a backslash followed by hexadecimal digits now denotes Unicode. Remove it if you meant plain letters, or double it to preserve a literal backslash.

## Property applicability in the editor

In both `@style` blocks and `.nss` files, property completion uses the subject of the selector when its type is certain. For example, `Button` excludes `object-fit` and Window-only native properties; `Image` and `Icon` offer `object-fit`, and `:root` offers native Window properties. Writing an inapplicable declaration produces warning `NZS112` on its property name. Shorthands follow their longhands. Selector lists retain a property if any target can use it.

An ID selector uses a unique literal `Id` from its owning component (`@style` or `.nazor.nss`). Standalone stylesheets use an ID only when its declaration is unique across the available project markup. Ambiguous IDs and IDs reached through another component or `::deep` do not constrain completion. Class-only selectors remain unrestricted. Custom rendering is treated conservatively; inherited properties and properties dependent on runtime layout remain available.

Declaring the same nonempty literal `Id` twice in one `.nazor` component produces compilation error `NZC017` on both declarations. IDs are case-sensitive. Nested component definitions have their own ID scopes, so their internal IDs can repeat those in the parent or in another component instance. Child content authored in the same file still belongs to that file's declaring component. Dynamic IDs are not checked for compile-time uniqueness.

## Units and value types

| Type | Values and interpretation |
| --- | --- |
| Length | `px`, `em`, `rem`, `ch`, `ex`, `vw`, `vh`, `vmin`, `vmax`, or zero; supported math functions |
| Length/percentage | Length or `%`, only where the property accepts it |
| Grid fraction | Nonnegative `fr` in grid tracks |
| Ratio | Positive `width / height` or a positive number with implicit denominator 1 |
| Intrinsic size | `min-content`, `max-content`, `fit-content`, where supported |
| Time | `ms`, `s`, or zero; animation delay can be negative, transition duration/delay cannot |
| Number | Property-specific range, such as opacity 0–1 |
| Angle | `deg`, `grad`, `rad`, `turn` in transforms/gradients; hue parsing supports degrees |

`px` is a logical UI unit, scaled by monitor DPI. Percentages need a defined reference size and are not interchangeable with viewport units. Vertical padding percentages use containing width. Unsupported units include pt, cm, lh, and container units.

`ch` uses the horizontal advance of U+0030 in the font used for that character. With explicit `font-feature-settings`, zero is shaped with those features; otherwise the unshaped advance is used. `ex` uses the first available font's x-height. Font weight, family and supported font settings affect these measurements; letter spacing does not. Unavailable measurements independently use 0.5em. Font-size uses the parent font metrics; other lengths and line-height use the element's computed font. Scene installs its actual font provider and invalidates these lengths when fonts are registered. Standalone StyleEngine callers can supply FontMetricsResolver or use the documented fallback. Vertical writing is not implemented. Media query lengths remain limited to px/em/rem; metric units require an element context.

`em` is the element font size, except in `font-size`, where it refers to the parent. `rem` is the root font size; root `font-size` resolves rem against initial 14px. Viewport units are percentages of logical client dimensions. Relative font units become computed logical lengths before inheritance, so inheriting a 2em value does not double it again. Percentages generally remain for layout; font-size and line-height resolve against font metrics.

Negative values are allowed only where meaningful, including margins, positioning offsets, letter spacing, outline offset, and shadow offsets/spread.

## Variable-font coordinates

`font-optical-sizing` and `font-variation-settings` inherit. `auto` uses the computed logical font size for the `opsz` axis, following CSS; `none` leaves that axis at the font default. The NSS extension `native` converts logical pixels to typographic points (`size * 72 / 96`). NSS initially uses `auto`; Basic explicitly chooses `native`, which Fluent inherits, while NazorLeaf chooses `auto`.

```css
.page-title {
    font-optical-sizing: auto;
    font-variation-settings: "opsz" 72, "wght" 800;
}
.body-copy { font-variation-settings: normal; }
```

Axis tags are case-sensitive strings of exactly four printable ASCII characters, including spaces. Coordinates may be fractional or negative but must be finite. The last duplicate tag wins; computed declarations store unique tags in ordinal order. Unsupported axes are ignored, supported coordinates are clamped to the selected font's range, and explicit coordinates override `font-weight` and automatic optical sizing. `normal` clears the inherited explicit axis list. Font matching still uses the requested family, weight and italic preference before applying explicit coordinates; glyph fallback uses the same requested settings.

Skia uses the same resolved face and coordinates for glyph metrics, shaping and rasterization. Shape caches include optical policy and an immutable copy of axis contents. Graphics callers can initialize `TextStyle.OpticalSizing` and `TextStyle.FontVariations`; directly constructed graphics styles default to `Native` to preserve native desktop sizing. Custom font providers may ignore unsupported axes through the default interface overloads.

Inherited `font-feature-settings` selects explicit OpenType features with quoted four-character tags and optional on/off/unsigned integer values. `normal` clears the overrides. `font-synthesis` accepts none, weight, style or both weight and style; its initial none preserves existing unsynthesized rendering. See [feature, synthesis and cache contracts](22-Images-Icons-and-Drawing.md#opentype-features-and-synthetic-styles).

Image values support linear, radial and conic gradients, including repeating variants. Content/backdrop effects use ordered filter-function lists, while blend/isolation properties configure group composition. Their grammars and initial values are in the [property reference](19-NSS-Property-Reference.md), with rendering semantics in the [drawing guide](22-Images-Icons-and-Drawing.md).

## Length expressions

```css
.card {
    width: calc(100% - 2 * 16px);
    padding: clamp(8px, 2vw, 24px);
    min-width: min(320px, 100%);
}
```

`calc`, `min`, `max`, and `clamp` work in length grammars, including compatible shorthand, shadow, gradient, radius, and grid-track positions. Addition/subtraction need spaces on both sides. Multiplication needs a unitless number on one side; division needs a nonzero unitless denominator. Numbers cannot be added to lengths. Functions can nest and use `var()`.

Mixed percentage/length expressions remain available until layout supplies the reference size. A negative result for a nonnegative property is clamped to zero. If clamp's minimum exceeds its maximum, the minimum wins. These functions do not provide general arithmetic for opacity, z-index, time, angles, or colors.

## Colors

| Form | Example |
| --- | --- |
| Hex RGB/RGBA | `#0af`, `#0af8`, `#00aaff`, `#00aaff80`; alpha is last |
| RGB | `rgb(0, 170, 255)`, `rgb(0 170 255 / 50%)` |
| HSL | `hsl(200deg 100% 50% / 0.5)` |
| Named and special | CSS named colors, `transparent`, `currentColor` |
| Mixing | `color-mix(in srgb, var(--accent) 90%, white)`, `color-mix(in oklab, var(--accent) 4%, #f5f6f8)` |
| System | `Canvas`, `CanvasText`, `ButtonFace`, `ButtonText`, `ButtonBorder`, `Field`, `FieldText`, `Highlight`, `HighlightText`, `SelectedItem`, `SelectedItemText`, `GrayText`, `LinkText`, `VisitedText`, `ActiveText`, `Mark`, `MarkText`, `AccentColor`, `AccentColorText` |

RGB channels accept numbers or percentages; alpha accepts 0–1 or a percentage. Comma and space/slash forms are supported, with `rgba`/`hsla` aliases. Use percentage saturation/lightness in HSL. Named CSS colors include gray/grey variants. `currentColor` uses the element's computed text color; on `color` itself it uses the inherited value.

`color-mix` supports explicit `in srgb` and rectangular `in oklab`. Omitted percentages use the remaining proportion or an equal split; totals below 100% reduce alpha and totals above 100% normalize the proportions. Mixing premultiplies all three components by alpha in the selected space, interpolates, then removes premultiplication before the final alpha multiplier. The result is converted to straight-alpha 8-bit sRGB; out-of-range encoded RGB components are clipped, not chroma-gamut-mapped. Nested Oklab mixes retain precision and out-of-range RGB until the outer Oklab result is converted to `Color`; sRGB mixes retain their existing byte-channel behavior. Variables and system-color aliases remain dynamic through normal custom-property resolution.

`currentColor` inside a mix is still rejected; it remains valid as an ordinary color value. Direct Lab/Oklab/OKLCH functions, polar/hue interpolation, HWB, generic `color()`, missing-channel `none`, omitted color-space clauses and CSS Color 4 wide-gamut gamut mapping remain unsupported. Oklab mixing does not change the space used by ordinary color transitions.

System colors update with platform preferences. Accent shades are exposed as `--nz-system-accent-light-1` through `-light-3` and `--nz-system-accent-dark-1` through `-dark-3`. In forced colors, use the user's system palette rather than assuming branded colors remain readable.

System keywords are aliases for inherited root custom properties: `Canvas` means `var(--nz-system-canvas)`, and `CanvasText` means `var(--nz-system-canvastext)`. These are ordinary, overridable NSS variables; overriding one also changes its keyword alias. Prefer theme tokens such as `--nz-surface` for application customization. See [system color keywords and theme tokens](16-Styling-and-Themes.md#system-color-keywords-and-theme-tokens) for the contract and examples.

## Custom properties

```css
:root { --surface: #fff; --accent: #6941c6; }
.card { background: var(--surface); border: 1px solid var(--border, #ccc); }
```

Names begin with `--` followed by at least one character. Values are preserved as tokens and can be empty. Variables inherit across component boundaries. `var(--name, fallback)` can nest and can reference another variable. Cycles invalidate the involved variables; a reference then uses its fallback. A substituted property value that cannot parse becomes `unset` rather than keeping an earlier cascade candidate.

Variables work in declaration values, not selectors or media conditions. Custom properties themselves cannot be animated and there is no `@property`. Use `:root` to define tokens on the current window. See [variable documentation](16-Styling-and-Themes.md#document-custom-properties-in-the-editor) for hover and completion descriptions.

## Generated content values

The `content` property accepts `normal`, `none`, or one or more quoted strings, including `""`. Adjacent strings concatenate after `var()` substitution. It generates only before/after boxes; it does not replace ordinary content. See [pseudo-elements](17-Selectors-and-Media-Queries.md#pseudo-elements).

## Global keywords

`inherit` takes the parent's computed value; `initial` takes the descriptor's initial value; `unset` means inherit for inherited properties and initial otherwise. They apply to longhands and shorthands. `revert`, `revert-layer`, `all`, and `!important` are unsupported.

Continue with the [property reference](19-NSS-Property-Reference.md) and [layout rules](20-Layout-and-Scrolling.md). API: [Nazor.Style](../ApiReferences/Nazor.Style.md).

---

[Guide index](README.md) · [API reference](../API.md)
