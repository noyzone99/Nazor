# 22. Images, icons, and drawing

The gallery's dialog sample uses the packaged `FluentSymbol.ArrowDownload20Regular` with its explicit font family. Use typed symbols for portable samples instead of unqualified private-use characters whose meaning changes with the active theme's icon font.

## Images and resources

Place `photo.jpg` and `heart.svg` in your app's `Assets` directory before using this example.

```razor
<Image Source="@ImageSource.FromFile("Assets/photo.jpg")" Alt="The main warehouse" Class="photo" />
<Icon Source="@ImageSource.FromFile("Assets/heart.svg")" />

@style {
    .photo { width: 240px; height: 160px; object-fit: cover; border-radius: 8px; }
}
```

`ImageSource` supports files, embedded resources, streams, bytes, and URIs. In C#, a string converts to a file source or HTTP(S) source. In Razor markup, use an expression for the `ImageSource` parameter, such as `@ImageSource.FromFile(...)`. File paths are relative to the application directory. Loading and decoding happen away from the UI thread. The element receives its natural dimensions when loading completes. `OnLoad` reports size, `OnError` reports failure, and repeated sources share loading and decoding.

Supported formats include PNG, JPEG with EXIF orientation, GIF's first frame, WebP, BMP, ICO, and a subset of SVG. A source without NSS dimensions uses its natural dimensions. One explicit dimension normally preserves aspect ratio. `object-fit` supports fill, contain, cover, none, and scale-down; `object-position` positions within remaining space.

For DPI variants, put `logo@1.5x.png` and `logo@2x.png` alongside `logo.png`. The loader chooses the smallest available scale at or above the requested scale. If none reaches it, the loader chooses the largest available scale below it. For example, a request for 175% uses the 2× image, and a request for 250% uses the same 2× image when no larger variant is available. The filename scale determines the logical size of the chosen image. Embedded resources support the same suffixes before the extension.

`Alt` supplies an image's accessible name. Images without it are decorative. An icon-only button needs a name on the button itself. Embedded resources are useful for packaged libraries: `ImageSource.FromResource(assembly, resourceName)` avoids assuming consumer asset copying.

## SVG and icon fonts

The SVG reader supports basic shapes/path/grouping, fills and strokes, opacity, transforms, and centered viewBox scaling. It does not implement SVG text, gradients, nested images, filters, masks, clip paths, use references, CSS stylesheets, or dashed stroke patterns. Validate supplied artwork against this subset.

`Icon` draws a typed font symbol, raw glyph, or image mask in the current text color, typically at 1em. When more than one is supplied, precedence is `Source`, then `Symbol`, then `Glyph`.

### Typed Fluent symbols

Reference the alpha release of `Nazor.Themes.Fluent` that matches your SDK's numeric version. Register its packaged icon fonts before `Build()`.

```csharp
using Nazor.Themes.Fluent;

var builder = NazorApplication.CreateBuilder(args);
builder.Fonts.AddFluentFonts();
```

Keep the rest of the application's normal startup configuration. Font registration does not activate the Fluent theme: these symbols work with Basic, Fluent, NazorLeaf, or an application theme.

```razor
@using Nazor.Themes.Fluent

<Icon Symbol="@FluentSymbol.Add20Regular" />
<Icon Symbol="@FluentSymbol.Star20Filled" Class="favorite" />

@style {
    .favorite { font-size: 24px; color: AccentColor; }
}
```

Each `FluentSymbol` carries its Unicode glyph and matching font family. Names include the source artwork's design size and Regular/Filled variant; localized artwork also has a locale suffix. The name's size does not set `font-size`. The package ships Microsoft's MIT-licensed Fluent UI System Icons TTFs and notices under `Themes/Fluent` in build and publish output. Keep this directory with the application; installing a system icon font is unnecessary. The icon fonts do not provide general interface text fonts. [Full symbol catalog and package API](../ApiReferences/Nazor.Themes.Fluent.md#fluentsymbol)

### Native-preferred WinUI symbols

Use `WinUISymbol.Search`, `Calendar`, `RevealPassword`, `CalendarPrevious`, `CalendarNext`, `Add`, `Cancel`, `Information`, `Warning`, `Error` or `Help` when the icon should prefer native WinUI artwork:

```razor
@using Nazor.Themes.Fluent

<Icon Symbol="@WinUISymbol.Search" />
<Icon Symbol="@WinUISymbol.Calendar" />
```

Keep `AddFluentFonts()` in startup. Each symbol pairs a Segoe Fluent Icons glyph with explicit Segoe MDL2 Assets and packaged Fluent System Icons alternatives. The active font provider must match the exact family and cover every Unicode scalar before a pair is used; family and code point always change together. This respects `UseSystemFonts = false` and private font registration. It does not infer availability from the operating system. Font registration invalidates cached selection.

The Segoe fonts are not distributed by Nazor. The packaged fallback has different artwork; native selection does not certify identical rendering across platforms or font versions. `FluentSymbol` continues to select fixed portable artwork. Interface text separately prefers Segoe UI Variable, Segoe UI Variable Text and Segoe UI, then installed sans-serif families. Text metrics, optical sizing and rasterization follow the resolved font and renderer.

### Typed NazorLeaf symbols

The generated font retains the source SVG's hollow regions on both Windows and Linux font backends. Keep the font and its typed catalog from the same theme package when updating an application.

Reference `Nazor.Themes.NazorLeaf` and call `builder.Fonts.AddNazorLeafFonts()` before `Build()`. This registers the three text fonts and the packaged `NazorLeaf Icons` OpenType font:

```razor
@using Nazor.Themes.NazorLeaf

<Icon Symbol="@LeafSymbol.Plus" />
<Icon Symbol="@LeafSymbol.Search" />
<Icon Symbol="@LeafSymbol.Leaf" />
```

The [55-member catalog](../ApiReferences/Nazor.Themes.NazorLeaf.md#leafsymbol) uses the theme's original monochrome artwork on a 24 by 24 design grid. The symbols carry their private font family and work with any active theme. `font-size` sets the displayed size. The NazorLeaf theme matches icon font sizes to its button, toolbar and floating-action icon boxes. Font rasterization can differ slightly from SVG antialiasing at small sizes.

The NazorLeaf catalog also includes the original Mail, Chat, At, Pause, Dot and Dock symbols. Their appended U+E031–U+E036 mappings preserve all 49 earlier values. Select typed `LeafSymbol.Mail`, `LeafSymbol.Chat`, `LeafSymbol.At`, `LeafSymbol.Pause`, `LeafSymbol.Dot` and `LeafSymbol.Dock` through Icon.Symbol instead of substituting unrelated artwork or ordinary text. The packaged source SVGs/font keep the MIT notices.

Keep `Themes/NazorLeaf/NazorLeafIcons.otf`, `LICENSE-NazorLeaf-Icons.txt` and `SOURCE-NazorLeaf-Icons.txt` with the application. The icon font and artwork use the MIT license. NazorLeaf text fonts retain their separate Open Font License notices. Raw codes also work: `LeafSymbol.Plus.Glyph` is `\uE01F`; select the `NazorLeaf Icons` family explicitly outside the NazorLeaf theme. Within that theme it is the default `Icon` family. Applications using raw Segoe codes under NazorLeaf should switch to `LeafSymbol` or explicitly select their original font family. The four-leaf `LeafMark` component remains available for the multicolor brand and loading animation.

### Other icon fonts and raw codes

`Symbol` accepts any implementation of `Nazor.IFontSymbol`. A font package can expose a readonly struct with named static properties, providing a nonblank `Glyph` string and its internal `FontFamily` name. Glyph strings support supplementary Unicode characters, not only a single UTF-16 `char`. Register the font as a private font before startup. Symbols describe the font; they do not load or install it. `FontSymbol` is an immutable family/glyph pair for application-defined symbols.

When `Symbol` is active, its family is applied after the caller's inline style so a theme or `font-family` declaration cannot accidentally select another icon mapping. Color, size and other NSS styling remain available. Blank glyphs, blank families, and families containing control characters are rejected; `default(FluentSymbol)` is empty and invalid as an active symbol. The symbol's family is treated as one literal family name, not NSS declarations or a fallback list.

An `IFontSymbol` can expose ordered `Fallbacks` with equivalent meanings in other fonts. The default list is empty, retaining ordinary single-font behavior. Resolution visits at most 32 distinct symbols in depth-first order and skips cycles. Invalid visited candidates are rejected. If a symbol with alternatives has no exact covered candidate, its icon box remains but draws no symbol ink. Typed symbols take priority over `font-symbol` NSS mapping, and image `Source` still takes priority over all symbol metadata.

For decorative theme parts, NSS can select equivalent pairs without adding behavior to the control:

```css
.search-mark::before {
    content: "A";
    font-symbol: "My Native Icons" "A", "My Portable Icons" "B";
}
```

This requires both fonts to use those glyphs for the same icon. `content` still creates the generated box. The noninherited `font-symbol` property accepts up to 32 quoted family/glyph pairs; `none` uses ordinary text. It is ignored for editable text. If no pair is covered, no symbol ink is drawn. The one-em `Icon` box uses a line height of one, so an icon font's line gap does not add leading inside it.

For direct Unicode use, keep `Glyph` and specify its font through NSS:

```razor
<Icon Glyph="&#xF109;" Class="add-icon" />

@style {
    .add-icon { font-family: "FluentSystemIcons-Regular"; }
}
```

This example still requires `AddFluentFonts()`. Raw glyphs without an explicit font use the theme's `Icon` family; Basic currently requests Segoe Fluent Icons followed by Segoe MDL2 Assets, whose availability is platform-dependent. Those Segoe fonts have different mappings from Fluent System Icons. Supply an SVG mask through `Source` for artwork that does not use a font. An icon-only button needs an accessible name on the button; a decorative symbol is not named automatically from its catalog member.

Bind a ToggleButton's IsPressed value and select `LeafSymbol.StarFilled` when true or `LeafSymbol.Star` when false. The selected artwork is application-owned content; no font-family change or new toggle API is required. Both glyphs share the same original MIT-licensed path and round stroke. NazorLeaf adds a 450ms pop to pressed icon-only icons and suppresses it for reduced motion, forced colors, disabled or busy controls. Existing icon codepoints retain their mappings.

## Backgrounds and gradients

```css
.hero {
    background: linear-gradient(135deg, #6941c6, #1570ef);
    border-radius: 16px;
}
.texture { background: url(../Assets/pattern.png) center / cover no-repeat #202020; }
```

A compiled NSS `url()` resolves relative to the source `.nss`/`.nazor` file, or relative to project root when it begins with `/`. The compiler rewrites the deployment-relative asset path. An URL supplied through a variable is instead relative to the app directory. HTTP(S) image URLs load asynchronously.

Background layers can contain images and linear/radial/conic gradients, including repeating variants. The first layer is on top; the final color is beneath all images. Position uses remaining space, size supports auto/cover/contain or lengths/percentages, and repeat supports repeat/no-repeat/repeat-x/repeat-y. Shorter position/size/repeat lists cycle to cover the image list.

Linear gradients support angles or `to` directions; radial gradients support circle/ellipse, standard closest/farthest sizing, explicit radii, and position. Stops can have zero, one, or two positions. Conic gradients take optional `from <angle>` and `at <position>`; zero points up and angles increase clockwise. Their stops accept angles or percentages of a turn, including positions outside that turn. Canonical NSS writes angular stops as percentages. Standalone interpolation hints and gradient color-space clauses remain unsupported.

`repeating-linear-gradient`, `repeating-radial-gradient` and `repeating-conic-gradient` repeat the interval from the first to the last fixed-up stop in both directions; a nonzero first stop shifts the pattern. Equal-position boundaries retain hard edges. A zero-length period uses a premultiplied weighted average rather than the last color. Existing gradient color/raster conventions remain unchanged; this is not wide-gamut or configurable interpolation support. The same image values work in gradient borders and launcher backgrounds.

```css
.dial-face { background: conic-gradient(from 90deg at center, #1570ef 0 25%, #6941c6 25% 75%, #1570ef 75%); }
.stripe { background: repeating-linear-gradient(135deg, #101828 0 8px, #344054 8px 16px); }
.rings { background: repeating-radial-gradient(circle at center, #1570ef 4px 8px, transparent 8px 12px); }
```

Backgrounds use the border box, including rounded corners; this differs from some CSS background-origin defaults. Background layers do not animate between images/gradients.

## Decoration and transforms

Borders have per-side width/style/color and per-corner elliptical radii. Outline draws outside the border without changing layout and is suitable for focus. Shadows support multiple outer/inset entries, offsets, blur, spread, and color; they occupy no layout space. Supported shadow lists can transition/animate.

Opacity groups an element and all descendants. Zero opacity does not disable input or remove focus. Use visibility/display when hiding interactive UI.

2D transforms include translate, scale, rotate, skew, their axis forms, and matrix. Functions apply right-to-left around transform-origin. Transforms do not move siblings in layout; pointer hit testing follows the transformed shape. Bounds returned for accessibility/inspection enclose the transformed geometry. `clip-path: inset(...)`, including supported rounded corners, clips painting and pointer hits without changing layout; see [inset clipping](19-NSS-Property-Reference.md#rectangular-clipping). Other clipping paths, 3D transforms and perspective are unsupported. Ordered element filters and blending are described below.

Paint ordering is resolved among siblings, considering negative z-index, normal flow, positioned/flex items, and positive z-index. Each element paints with its subtree; a descendant z-index cannot escape that branch as a fully general browser stacking-context system might allow.

## DPI and rendering

Logical coordinates scale to physical pixels through the backend's effective scale. On Windows, retain Per-Monitor V2 DPI awareness and check the application on displays with different scales. The scene retains layout, culls offscreen content and tracks damaged regions.

Skia can render on a native GPU surface or rasterize on the CPU. `NAZOR_RENDERER=AUTO` tries hardware rendering and falls back for supported initialization failures. Use `GPU` to require hardware or `CPU` to select software. Headless image rendering uses software. `NazorWindow.Rendering` reports the effective renderer and any fallback reason. [Rendering pipeline](31-Architecture-and-Extension-Points.md#pipeline-and-trees) · [Renderer support](32-Support-and-Limitations.md#nss-and-rendering)

For low-level drawing, use graphics/display-list and scene APIs rather than assuming an application-level Canvas control exists. See [architecture](31-Architecture-and-Extension-Points.md) and the [graphics API](../ApiReferences/Nazor.Graphics.md).

---

[Guide index](README.md) · [API reference](../API.md)

## Element backdrop filters

`backdrop-filter: blur(6px) saturate(0.8)` filters earlier pixels on the current drawing surface behind the element's rounded border box. Foreground drawing stays sharp. `filter: blur(2px) contrast(1.2)` instead filters the complete element subtree, including text and overflowing children. Both properties share an ordered chain of up to 64 functions, permit repetitions, and default to `none`. They neither inherit nor interpolate. Order matters: `brightness(2) contrast(.5)` differs from the reverse because each operation clamps its output.

Supported functions are `blur`, `brightness`, `contrast`, `grayscale`, `hue-rotate`, `invert`, `opacity`, `saturate` and `sepia`. Blur takes a nonnegative length without percentages; hue rotation takes an angle. Other functions take finite nonnegative numbers or percentages. Empty arguments default to zero for blur/hue rotation and one for the others. Grayscale, invert, opacity and sepia clamp to one. Gaussian standard deviation is bounded to 256 logical pixels; other unbounded factors are capped at 1e20 for backend arithmetic. URL/SVG filters and `drop-shadow()` are not implemented; use box-shadow for box effects.

Element opacity composites the complete group once. Foreground blur expands retained ink/culling bounds with transparent edges; backdrop blur mirrors captured edges. Ordinary clipping and transforms still apply. Filtered lists conservatively request full-surface damage, and opacity/transform animation on filtered elements stays on the UI frame path, including mixed animated scenes. Recorded drawing retains native effect resources independently of its renderer.

Backdrop sampling reads only the current surface. An external native popup cannot blur its parent window or the desktop through this property. Use an opaque popup surface or a supported native window material for that case. Element filters do not enable native window transparency or replace system frames.

Low-level drawing uses copied `ImageFilter` sequences on `PushFilter` or `PushBackdrop.Filters`, balanced by `Pop`. `ColorMatrixFilter` accepts 20 finite coefficients in row-major order over straight RGBA components, with normalized 0..1 translation offsets. `GaussianBlurFilter` takes logical-pixel sigma. A null `PushBackdrop.Filters` retains the direct Blur/Saturation constructor behavior; an empty chain overrides that pair with identity backdrop sampling.

Pre-release migration: `Nazor.Style.BackdropFilterValue` is replaced by `FilterValue(new ValueList<FilterFunction>(...))`, using `FilterKind`, `Amount` and `Radius`. Replace the old `None` with `FilterValue.None`; regenerate serialized compiled styles using the new ordered representation. Existing NSS blur/saturate expressions remain valid.

## Color blending and isolation

`mix-blend-mode` combines the complete element subtree with earlier destination pixels. The default `normal` uses ordinary source-over. Supported modes are multiply, screen, overlay, darken, lighten, color-dodge, color-burn, hard-light, soft-light, difference, exclusion, hue, saturation, color and luminosity. Source-over alpha composition still applies, including transparent destinations. Element opacity applies to the group once; filters modify its source before the final blend. These properties change discretely, do not inherit and do not change layout or hit testing.

`isolation: isolate` starts a transparent group, so descendant blending samples only pixels already painted inside it. `auto` adds no group; existing opacity, filter and nonnormal blend groups already isolate their contents. Nazor retains its branch-based paint ordering rather than implementing a browser's full stacking-context model. Backdrop filters inside an isolated group can sample only that group's destination.

`background-blend-mode` takes a comma-separated list cycled over background images in their declared order. Each image blends against lower background images and the element's background color, inside a dedicated isolated background group. Backgrounds never blend with the page through this property. Borders, shadows, text and children remain outside this background-only group. Empty/unloaded image layers retain their list index.

Normal/auto drawing adds no blend layer. Low-level drawing can use `PushBlend(BlendMode, Bounds)`, followed by drawing and a balancing `Pop`; Normal requests explicit isolation. Blended display lists currently use conservative full damage. Recordings preserve blend state, clipping and transforms. Porter-Duff operators, plus-lighter/plus-darker and wide-gamut blending are outside this grammar.

## OpenType features and synthetic styles

`font-feature-settings: "kern" off, "liga" on, "ss01" 2` applies explicit OpenType features during shaping. Tags are exactly four printable ASCII characters and case-sensitive; values are unsigned 32-bit integers, or on/off. An omitted selection means one. Last duplicate wins. Unsupported font features have no effect. `normal` clears explicit selections and retains the shaper's usual defaults. Features inherit and affect glyphs, advances, carets, wrapping and shaping-cache identity; the `ch` metric also shapes zero with the selected features. They do not interpolate.

`font-synthesis: weight style` permits synthetic bold and a 12-degree oblique when the requested real style is unavailable. Either keyword can be used alone; `none` disables both. Nazor defaults to none to preserve existing rendering, unlike browser defaults. Real faces and variable wght/ital/slnt axes take precedence. Set ital/slnt explicitly through font-variation-settings; automatic style-axis selection is not part of this increment. Weight synthesis applies only when weight 600 or greater is requested and the selected static face is below 600. Synthesis modifies outlines without changing real-face identity, advances or line metrics. Retained ink/damage bounds include conservative additional extent. This is not typographic small-caps or general oblique-angle support.

Direct drawing sets copied `TextStyle.FontFeatures` and `Synthesis`. Shaping-cache equality includes their contents and policy. Applied `ShapedRun.Synthesis` survives wrapping, slicing, letter spacing and mixed inline text; renderer font-cache identity includes it. The style-aware `ITextShaper.Shape` overload passes these settings to the backend while its default implementation delegates to the existing size-only overload for custom shapers. HarfBuzz applies features per resolved/fallback run without mutating shared faces; Skia owns separate raster font instances for each synthesis variant. Existing size-only calls retain their defaults.
