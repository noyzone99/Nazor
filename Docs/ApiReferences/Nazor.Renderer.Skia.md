# Nazor.Renderer.Skia

Skia rendering, font management, image decoding, HarfBuzz text shaping, and PNG export.

See [framework-specific dependency versions](../Guide/28-Libraries-Packaging-and-Deployment.md#framework-specific-dependency-versions) for the matched managed/native SkiaSharp and HarfBuzzSharp packages.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Native recordings

`SkiaRenderer.Record(DisplayList)` returns a caller-owned immutable `SKPicture` retaining native image/font references. `RecordComposition(DisplayList)` returns a caller-owned `RecordedComposition`, preserving detached animation states for render-thread playback. Recording runs on the UI thread and reuses unchanged drawings; `LastRecordingsReused` and `LastRecordingsCreated` describe the last recording call. Software raster rendering remains available independently.

`RecordedComposition` implements `IDisposable`. Its `HasAnimations` and `IsAnimating` properties report captured timelines and whether another animation frame is needed. `Draw(SKCanvas canvas)` replays native recordings with current detached samples on the render worker. Dispose after playback/replacement; do not dispose concurrently with drawing.

[Recording source](../../Sources/Core/Nazor.Renderer.Skia/RecordedComposition.cs)

## Font and renderer types

- [`HarfBuzzTextShaper`](#type-harfbuzztextshaper)
- [`SkiaFontProvider`](#type-skiafontprovider)
- [`SkiaRenderer`](#type-skiarenderer)

<a id="type-harfbuzztextshaper"></a>

## `HarfBuzzTextShaper`

Namespace: `Nazor.Skia`

```csharp
public sealed class HarfBuzzTextShaper : ITextShaper
```

Shapes with HarfBuzz: UTF-16 input with the whole text as context, clusters as string indices.

[Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaFonts.cs)

### Members

#### `Shape`

```csharp
public ShapedRun Shape(string text, int start, int length, FontFace face, float size)
```

Shapes a UTF-16 text segment into positioned glyphs using the specified font face and size.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |
| `start` | The start offset in the UTF-16 source text. |
| `length` | The number of UTF-16 code units in the text segment. |
| `face` | The font face used to shape or measure text. |
| `size` | The logical font size used for measurement or shaping. |

Returns: The positioned glyph run for the source text segment.


<a id="type-skiafontprovider"></a>

## `SkiaFontProvider`

Namespace: `Nazor.Skia`

```csharp
public sealed class SkiaFontProvider : IFontProvider, IDisposable
```

Resolves faces from private font files and, optionally, from the system font manager (DirectWrite on Windows).

[Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaFonts.cs)

### Members

#### `SkiaFontProvider`

```csharp
public SkiaFontProvider(FontOptions? options = null)
```

Initializes a new instance of SkiaFontProvider.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

#### `Version`

```csharp
public int Version { get; private set;}
```

Gets the font-cache version, incremented when the available font set changes.

#### `Register`

```csharp
public void Register(string fontFile)
```

Registers a font file as a private family; cached layouts become stale.

| Parameter | Description |
| --- | --- |
| `fontFile` | The font file (string). |

#### `Match`

```csharp
public FontFace Match(IReadOnlyList<string> families, int weight, bool italic)
```

Resolves a font face from the ordered family list, weight, and italic preference.

| Parameter | Description |
| --- | --- |
| `families` | The preferred font families, in fallback order. |
| `weight` | The requested numeric font weight. |
| `italic` | Whether an italic face is preferred. |

Returns: The resolved font face, using configured fallbacks when necessary.

#### `Match` with optical size

```csharp
public FontFace Match(IReadOnlyList<string> families, int weight, bool italic, float size)
```

Resolves a font at `size` logical pixels. Optical sizing uses points (`size * 72 / 96`), clamped to the font's `opsz` range. Static fonts use the existing face; variable instances are cached per selected base face and canonical actual coordinates. Non-finite, zero, and negative sizes throw `ArgumentOutOfRangeException`.

On Windows, the typographic Segoe UI Variable family can be loaded from the installed `SegUIVar.ttf` when the system font manager exposes only its named optical families. The requested weight and optical size remain variable; unavailable fonts follow the normal family fallback chain.

#### `Fallback`

```csharp
public FontFace? Fallback(int codepoint, FontFace preferred)
```

Finds a font face containing the requested character when the preferred face cannot supply it.

| Parameter | Description |
| --- | --- |
| `codepoint` | The Unicode scalar value of the character. |
| `preferred` | The preferred font face before glyph fallback. |

Returns: A fallback face containing the character, or null if no matching face is available.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.


### Variable-font style overloads

Private fonts match their native family name and valid Unicode OpenType name-table family/typographic-family aliases (IDs1/16). This lets CSS-style family names resolve before an available default font. Public FontFace.FamilyName retains the backend's native label. Style-aware fallback preserves the originally requested weight and italic preference.

| Declaration | Contract |
| --- | --- |
| `public FontFace Match(TextStyle style)` | Matches family/weight/italic, then applies Native point sizing, Auto logical-pixel sizing, or None, followed by explicit axes. Supported axes are clamped; unsupported tags are ignored. Positive finite font size required. |
| `public FontFace? Fallback(int codepoint, FontFace preferred, TextStyle style)` | Selects a covering face by the original requested weight/style and applies its supported optical/explicit coordinates. |

HarfBuzz metrics/shaping and Skia rasterization share the cloned typeface's final coordinates. Instances are cached by base face and ordinal actual coordinate content; dictionary identity and unsupported axes do not create duplicates. Explicit finite doubles are clamped before conversion to backend floats.

<a id="type-skiarenderer"></a>

## `SkiaRenderer`

Namespace: `Nazor.Skia`

```csharp
public sealed class SkiaRenderer : IRenderer
```

Replays display lists on a Skia raster canvas. The canvas is scaled once by the DPI factor: commands stay in logical units. Layout snaps box edges to physical pixels; circular decorations may retain their original square geometry. Text is drawn from the glyphs and positions of the `TextLayout` that was measured, never re-measured. Skia objects are used on the UI thread only.

Uniform solid circular borders use a closed antialiased ring contour and a constant-color shader to keep translucent coverage consistent under partial repaint clips. An adjacent circular padding-box fill and matching solid border of the same color are painted as one filled contour, avoiding a translucent seam between their antialiased boundaries. Classification uses the original square logical bounds and uniform circular radii, so fractional origins cannot bypass these paths through floating-point changes to Skia's rounded-rectangle type. Gradient, elliptical, asymmetric and dashed/dotted borders, and borders that consume the inner circle, retain their existing drawing paths. Selection is geometric and independent of control names, themes and operating systems. The shared scene applies NSS stroke width and path trimming to check/chevron glyphs before producing ordinary `DrawPolyline` commands; see [vector glyph strokes](../Guide/19-NSS-Property-Reference.md#vector-glyph-strokes).

[Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaRenderer.cs)

### Members

#### `SkiaRenderer`

```csharp
public SkiaRenderer(FontOptions? options = null)
```

Initializes a new instance of SkiaRenderer.

| Parameter | Description |
| --- | --- |
| `options` | The options used to configure the operation. |

#### `Fonts`

```csharp
public IFontProvider Fonts {get; }
```

Gets the provider used to resolve font faces and glyph fallbacks.

#### `Shaper`

```csharp
public ITextShaper Shaper { get; }
```

Gets the service used to shape text into positioned glyphs.

#### `Images`

```csharp
public IImageDecoder Images { get; }
```

Gets the service used to decode raster and vector image content.

#### `FontProvider`

```csharp
public SkiaFontProvider FontProvider {get; }
```

Gets the Skia font provider used by this renderer.

#### `RenderToImage`

```csharp
public RenderedImage RenderToImage(DisplayList displayList, float scale, Graphics.Color background)
```

Renders drawing commands into a newly allocated pixel image at the requested scale.

| Parameter | Description |
| --- | --- |
| `displayList` | The ordered drawing commands and logical surface size to render. |
| `scale` | The number of physical pixels per logical unit. |
| `background` | The background color used to clear the surface. |

Returns: The resulting RenderedImage value.

#### `Render`

```csharp
public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background)
```

Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.

| Parameter | Description |
| --- | --- |
| `displayList` | The ordered drawing commands and logical surface size to render. |
| `pixels` | The address of the top-down BGRA premultiplied pixel buffer. |
| `width` | The width of the target pixel buffer. |
| `height` | The height of the target pixel buffer. |
| `stride` | The number of bytes between the starts of successive pixel rows. |
| `scale` | The number of physical pixels per logical unit. |
| `background` | The background color used to clear the surface. |

#### `Render`

```csharp
public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background, IReadOnlyList<PixelRect>? damage)
```

Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.

| Parameter | Description |
| --- | --- |
| `displayList` | The ordered drawing commands and logical surface size to render. |
| `pixels` | The address of the top-down BGRA premultiplied pixel buffer. |
| `width` | The width of the target pixel buffer. |
| `height` | The height of the target pixel buffer. |
| `stride` | The number of bytes between the starts of successive pixel rows. |
| `scale` | The number of physical pixels per logical unit. |
| `background` | The background color used to clear the surface. |
| `damage` | The damage (IReadOnlyList<PixelRect>?). |

#### `EncodePng`

```csharp
public byte[] EncodePng(RenderedImage image)
```

Encodes the rendered pixel image as a PNG byte sequence.

| Parameter | Description |
| --- | --- |
| `image` | The rendered or decoded image to process. |

Returns: The PNG-encoded image bytes.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `HarfBuzzTextShaper`

Namespace `Nazor.Skia`. [Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaFonts.cs)

| Declaration | Contract |
| --- | --- |
| `public HarfBuzzTextShaper()` |  |

### `RecordedComposition`

Namespace `Nazor.Skia`. [Source](../../Sources/Core/Nazor.Renderer.Skia/RecordedComposition.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RecordedComposition : IDisposable` | Owns immutable native drawing recordings and detached animation states for render-thread playback. |
| `public void Draw(SKCanvas canvas)` | Draws native recordings with the current detached transform and opacity samples.<br>`canvas`. The render-thread canvas, owned by the caller. |
| `public void Dispose()` | Releases all native picture references after playback or replacement. |
| `public bool HasAnimations { get; }` | Whether this recording contains detached animation timelines. |
| `public bool IsAnimating { get; }` | Whether at least one captured timeline still needs another frame. |

### `SkiaFontProvider`

Namespace `Nazor.Skia`. [Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaFonts.cs)

| Declaration | Contract |
| --- | --- |
| `public int Version { get; }` | Gets the font-cache version, incremented when the available font set changes. |

### `SkiaRenderer`

Namespace `Nazor.Skia`. [Source](../../Sources/Core/Nazor.Renderer.Skia/SkiaRenderer.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class SkiaRenderer : IRenderer, IDisposable` | Replays display lists on a Skia raster canvas or records immutable pictures for GPU playback. The canvas is scaled once by the DPI factor: commands stay in logical units. Layout snaps box edges to physical pixels; circular decorations may retain their original square geometry. Text is drawn from the glyphs and positions of the `Nazor.Graphics.Text.TextLayout` that was measured, never re-measured. Renderer instances and their caches belong to the UI thread; resource-owning recordings can be transferred to a rendering worker. Uniform solid circular borders use a closed antialiased ring contour. Adjacent matching circular padding-box fills and borders share one coverage mask, including partial repaints. Circular classification uses logical bounds and radii, preserving coverage at fractional origins independently of Skia's rounded-rectangle classification. |
| `public RenderedImage RenderToImage(DisplayList displayList, float scale, Color background)` | Renders drawing commands into a newly allocated pixel image at the requested scale.<br>`displayList`. The ordered drawing commands and logical surface size to render.<br>`scale`. The number of physical pixels per logical unit.<br>`background`. The background color used to clear the surface.<br>**Returns**. The resulting RenderedImage value. |
| `public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background)` | Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.<br>`displayList`. The ordered drawing commands and logical surface size to render.<br>`pixels`. The address of the top-down BGRA premultiplied pixel buffer.<br>`width`. The width of the target pixel buffer.<br>`height`. The height of the target pixel buffer.<br>`stride`. The number of bytes between the starts of successive pixel rows.<br>`scale`. The number of physical pixels per logical unit.<br>`background`. The background color used to clear the surface. |
| `public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background, IReadOnlyList<PixelRect>? damage)` | Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.<br>`displayList`. The ordered drawing commands and logical surface size to render.<br>`pixels`. The address of the top-down BGRA premultiplied pixel buffer.<br>`width`. The width of the target pixel buffer.<br>`height`. The height of the target pixel buffer.<br>`stride`. The number of bytes between the starts of successive pixel rows.<br>`scale`. The number of physical pixels per logical unit.<br>`background`. The background color used to clear the surface.<br>`damage`. The damage (IReadOnlyList<PixelRect>?). |
| `public SKPicture Record(DisplayList displayList)` | Records a resource-owning immutable picture for playback on a rendering thread.<br>`displayList`. The drawing commands, read only on the calling UI thread.<br>**Returns**. A caller-owned picture retaining native image and font references independently of UI caches. |
| `public RecordedComposition RecordComposition(DisplayList displayList)` | Records static drawings once and preserves detached animated state for render-thread sampling.<br>`displayList`. The UI-thread display list, optionally containing composition snapshots.<br>**Returns**. A resource-owning recording, disposed by its render worker after use. |
| `public int LastRecordingsReused { get; }` | Gets the number of drawing recordings reused by the last Record call. |
| `public int LastRecordingsCreated { get; }` | Gets the number of drawing recordings created by the last Record call. |

### `HarfBuzzTextShaper`

Namespace `Nazor.Skia`.

Shapes with HarfBuzz: UTF-16 input with the whole text as context, clusters as string indices.

| Declaration | Contract |
| --- | --- |
| `public sealed class HarfBuzzTextShaper : ITextShaper` | Shapes with HarfBuzz: UTF-16 input with the whole text as context, clusters as string indices. |
| `public ShapedRun Shape(string text, int start, int length, FontFace face, float size)` | Shapes a UTF-16 text segment into positioned glyphs using the specified font face and size. The positioned glyph run for the source text segment. |
| `public ShapedRun Shape(string text, int start, int length, FontFace face, TextStyle style)` | Shapes explicit OpenType features and resolves opt-in outline synthesis without mutating the shared real face. The positioned glyph run with applied synthesis flags. |
| `public HarfBuzzTextShaper()` | Creates a new instance. |
