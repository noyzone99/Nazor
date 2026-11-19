# Nazor.Graphics

Drawing commands, geometry, colors, images, font resolution, text shaping, and text layout.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Renderer selection and composition

`Nazor.Graphics.RenderingMode` has `Auto`, `Gpu` and `Cpu` values. `RenderingInfo(Requested, Actual, Backend, Device = null, FallbackReason = null)` is an immutable description of the requested and selected renderer. AUTO is a selection preference; an effective native renderer is GPU or CPU.

`CompositionTimeline` is an abstract detached, render-thread-safe animation snapshot. It exposes `long CapturedAt` (a Stopwatch timestamp), `TimeSpan Duration` (including delay; MaxValue for unbounded repetition), and `(Matrix Transform, float Opacity) Sample(TimeSpan elapsed)`. Implementations must not access UI objects or mutable animation state. `PushComposition(CompositionTimeline Timeline)` pushes its animated group state and requires one matching `Pop`.

[Rendering source](../../Sources/Core/Nazor.Graphics/RenderingMode.cs) · [Composition source](../../Sources/Core/Nazor.Graphics/CompositionTimeline.cs)

## Drawing and text types

- [`BorderSide`](#type-borderside)
- [`BreakWords`](#type-breakwords)
- [`Color`](#type-color)
- [`CornerRadius`](#type-cornerradius)
- [`DecodedImage`](#type-decodedimage)
- [`DecorationLines`](#type-decorationlines)
- [`DecorationStyle`](#type-decorationstyle)
- [`DisplayList`](#type-displaylist)
- [`DisplayListBuilder`](#type-displaylistbuilder)
- [`DisplayListDamage`](#type-displaylistdamage)
- [`DrawBorder`](#type-drawborder)
- [`DrawCommand`](#type-drawcommand)
- [`DrawImage`](#type-drawimage)
- [`DrawPolyline`](#type-drawpolyline)
- [`DrawShadow`](#type-drawshadow)
- [`DrawText`](#type-drawtext)
- [`FillGradient`](#type-fillgradient)
- [`FillImage`](#type-fillimage)
- [`FillRectangle`](#type-fillrectangle)
- [`FontFace`](#type-fontface)
- [`FontMetrics`](#type-fontmetrics)
- [`FontOptions`](#type-fontoptions)
- [`GradientBrush`](#type-gradientbrush)
- [`GradientStop`](#type-gradientstop)
- [`IFontProvider`](#type-ifontprovider)
- [`IImageDecoder`](#type-iimagedecoder)
- [`IRenderer`](#type-irenderer)
- [`ITextShaper`](#type-itextshaper)
- [`LinearGradientBrush`](#type-lineargradientbrush)
- [`LineHeightKind`](#type-lineheightkind)
- [`LineStyle`](#type-linestyle)
- [`Matrix`](#type-matrix)
- [`PixelRect`](#type-pixelrect)
- [`Point`](#type-point)
- [`Pop`](#type-pop)
- [`PushClip`](#type-pushclip)
- [`PushOpacity`](#type-pushopacity)
- [`PushTransform`](#type-pushtransform)
- [`PushTranslate`](#type-pushtranslate)
- [`RadialGradientBrush`](#type-radialgradientbrush)
- [`Radius`](#type-radius)
- [`Rect`](#type-rect)
- [`RenderedImage`](#type-renderedimage)
- [`ShapedRun`](#type-shapedrun)
- [`ShapedText`](#type-shapedtext)
- [`Size`](#type-size)
- [`TextAlignment`](#type-textalignment)
- [`TextDecoration`](#type-textdecoration)
- [`TextLayout`](#type-textlayout)
- [`TextLine`](#type-textline)
- [`TextRun`](#type-textrun)
- [`TextStyle`](#type-textstyle)
- [`Thickness`](#type-thickness)

<a id="type-borderside"></a>

## `BorderSide`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct BorderSide(Color Color, LineStyle Style = LineStyle.Solid)
```

The color and line style of one side of a border.

| Parameter | Description |
| --- | --- |
| `Color` | The color (Color). |
| `Style` | The style (LineStyle). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-breakwords"></a>

## `BreakWords`

Namespace: `Nazor.Graphics.Text`

```csharp
public enum BreakWords : byte
```

Where a line may break inside a word that does not fit (CSS overflow-wrap).

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `Never`

```csharp
Never
```

Only after spaces: a long word overflows.

#### `WhenOverflowing`

```csharp
WhenOverflowing
```

Between graphemes of a word that does not fit on a line of its own; min-content stays the word.

#### `Anywhere`

```csharp
Anywhere
```

As `WhenOverflowing`, and min-content is the widest grapheme.


<a id="type-color"></a>

## `Color`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Color(byte R, byte G, byte B, byte A)
```

An sRGB color with straight alpha; converted to the backend format only by the renderer.

| Parameter | Description |
| --- | --- |
| `R` | The red channel in the range from zero to 255. |
| `G` | The green channel in the range from zero to 255. |
| `B` | The blue channel in the range from zero to 255. |
| `A` | The alpha channel in the range from zero to 255. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Transparent`

```csharp
public static readonly Color Transparent ;
```

Fully transparent black, with all RGBA channels set to zero.

#### `Black`

```csharp
public static readonly Color Black ;
```

Opaque black, with zero RGB channels and full alpha.

#### `White`

```csharp
public static readonly Color White ;
```

Opaque white, with all RGBA channels set to 255.

#### `IsTransparent`

```csharp
public bool IsTransparent {get; }
```

Gets whether the alpha channel is zero.

#### `WithOpacity`

```csharp
public Color WithOpacity(float opacity)
```

Multiplies the alpha channel by the opacity, clamped to the range from zero to one.

| Parameter | Description |
| --- | --- |
| `opacity` | The alpha multiplier, clamped to the range from zero to one. |

Returns: The resulting Color value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-cornerradius"></a>

## `CornerRadius`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct CornerRadius(Radius TopLeft, Radius TopRight, Radius BottomRight, Radius BottomLeft) : IFormattable
```

The radii of the rounded corners of a box, one elliptical `Radius` per corner as in CSS. Two numbers or a single number give the same radius to the four corners.

| Parameter | Description |
| --- | --- |
| `TopLeft` | The top left (Radius). |
| `TopRight` | The top right (Radius). |
| `BottomRight` | The bottom right (Radius). |
| `BottomLeft` | The bottom left (Radius). |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Zero`

```csharp
public static readonly CornerRadius Zero ;
```

The corner radius with all components set to zero.

#### `CornerRadius`

```csharp
public CornerRadius(float x, float y)
```

Initializes a new instance of CornerRadius.

| Parameter | Description |
| --- | --- |
| `x` | The x (float). |
| `y` | The y (float). |

#### `implicit operator CornerRadius`

```csharp
public static implicit operator CornerRadius(float radius)
```

Converts a float value to CornerRadius.

| Parameter | Description |
| --- | --- |
| `radius` | The corner radius to apply uniformly. |

Returns: The resulting CornerRadius value.

#### `IsZero`

```csharp
public bool IsZero {get; }
```

Gets whether all four corners are square.

#### `IsUniform`

```csharp
public bool IsUniform {get; }
```

Gets whether all corner radii are equal.

#### `Deflate`

```csharp
public CornerRadius Deflate(Thickness widths)
```

The radii of the inner edge of a border with these widths (CSS Backgrounds §5.2): each corner loses the widths of its two sides, horizontal and vertical.

| Parameter | Description |
| --- | --- |
| `widths` | The logical border thicknesses on the four edges. |

Returns: The resulting CornerRadius value.

#### `Inflate`

```csharp
public CornerRadius Inflate(float amount)
```

The radii of an edge drawn `amount` outside the box: rounded corners grow, square ones stay square.

| Parameter | Description |
| --- | --- |
| `amount` | The logical distance by which to grow, shrink, or offset the value. |

Returns: The resulting CornerRadius value.

#### `ToString`

```csharp
public string ToString(string? format, IFormatProvider? formatProvider)
```

Returns a textual representation of this value.

| Parameter | Description |
| --- | --- |
| `format` | The numeric format string used for component values. |
| `formatProvider` | The provider used to format numeric values. |

Returns: The textual representation of this value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-decodedimage"></a>

## `DecodedImage`

Namespace: `Nazor.Graphics`

```csharp
public abstract class DecodedImage
```

A decoded image, kept by the renderer that decoded it: raster pixels or a vector picture. Its `Size` is in image pixels (in SVG user units for a vector image).

[Source](../../Sources/Core/Nazor.Graphics/Images.cs)

### Members

#### `Size`

```csharp
public abstract Size Size { get; }
```

Gets the decoded image dimensions.

#### `IsVector`

```csharp
public abstract bool IsVector { get; }
```

A vector image is drawn sharp at any size.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-decorationlines"></a>

## `DecorationLines`

Namespace: `Nazor.Graphics`

```csharp
[Flags] public enum DecorationLines : byte
```

Specifies text-decoration positions; values can be combined.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `None`

```csharp
None = 0
```

Draws no text decoration.

#### `Underline`

```csharp
Underline = 1
```

Draws a line below the text.

#### `Overline`

```csharp
Overline = 2
```

Draws a line above the text.

#### `LineThrough`

```csharp
LineThrough = 4
```

Draws a line through the text.


<a id="type-decorationstyle"></a>

## `DecorationStyle`

Namespace: `Nazor.Graphics`

```csharp
public enum DecorationStyle : byte
```

Specifies the stroke pattern used for text decorations.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Solid`

```csharp
Solid
```

Draws a continuous decoration line.

#### `Double`

```csharp
Double
```

Draws two parallel decoration lines.

#### `Dotted`

```csharp
Dotted
```

Draws the decoration as dots.

#### `Dashed`

```csharp
Dashed
```

Draws the decoration as dashes.

#### `Wavy`

```csharp
Wavy
```

Draws the decoration as a wavy line.


<a id="type-displaylist"></a>

## `DisplayList`

Namespace: `Nazor.Graphics`

```csharp
public sealed class DisplayList(Size size, IReadOnlyList<DrawCommand> commands)
```

An immutable list of drawing commands for a surface of `Size` logical units.

| Parameter | Description |
| --- | --- |
| `size` | The logical dimensions of the surface or content. |
| `commands` | The ordered drawing commands. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Size`

```csharp
public Size Size { get; }
```

Gets the logical dimensions of the drawing surface.

#### `Commands`

```csharp
public IReadOnlyList<DrawCommand> Commands { get; }
```

Gets the ordered drawing commands to execute on the surface.

#### `Describe`

```csharp
public string Describe()
```

Readable form for tests: one command per line, indented by push depth.

Returns: The resulting string value.


<a id="type-displaylistbuilder"></a>

## `DisplayListBuilder`

Namespace: `Nazor.Graphics`

```csharp
public sealed class DisplayListBuilder
```

Accumulates drawing commands and creates a display list for a logical surface.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Add`

```csharp
public void Add(DrawCommand command)
```

Appends a drawing command to the display list being built.

| Parameter | Description |
| --- | --- |
| `command` | The drawing or keyboard command to add or execute. |

#### `Build`

```csharp
public DisplayList Build(Size size)
```

Creates a display list from the accumulated commands and the logical surface size.

| Parameter | Description |
| --- | --- |
| `size` | The logical dimensions of the surface or content. |

Returns: The display list containing the accumulated commands.


<a id="type-displaylistdamage"></a>

## `DisplayListDamage`

Namespace: `Nazor.Graphics`

```csharp
public sealed class DisplayListDamage
```

What changed on the surface between two display lists: the bounds of the drawings that one list has and the other does not, each with the transform, clip and opacities it is drawn with. Painting the damaged rectangles again with the new list gives the pixels of a full repaint. A drawing that moved before or after another one (a change of paint order) is damaged too.

Damage includes whole curved drawings touched by the dirty area, including the physical pixel rounding and antialiasing margin. This prevents clipping a partial repaint from changing their edge coverage. Overlapping curves can extend that area further; unaffected distant drawings remain outside it. Pass the actual raster scale to the scale overloads, then use the same scale with `ToPixels` and the renderer. The original overloads use scale 1.

[Source](../../Sources/Core/Nazor.Graphics/DisplayListDamage.cs)

### Members

#### `Compute`

```csharp
public static IReadOnlyList<Rect>? Compute(DisplayList? previous, DisplayList current)
```

The damaged rectangles between two lists, in logical units of the surface, merged and clipped to it; empty when nothing changed, null when everything must be painted (no previous list, a new size, a drawing whose bounds are unknown).

| Parameter | Description |
| --- | --- |
| `previous` | The previous (DisplayList?). |
| `current` | The current navigation entry before the change. |

Returns: The resulting IReadOnlyList<Rect>? value.

#### `Compute` at a raster scale

```csharp
public static IReadOnlyList<Rect>? Compute(DisplayList? previous, DisplayList current, float scale)
```

Computes logical damage for the supplied pixel density. `previous: null` requires a full repaint. `scale` is the finite, positive number of physical pixels per logical unit; invalid scales throw `ArgumentOutOfRangeException`. The rounding guard accounts for up to one physical pixel from rounding and another from antialiasing.

#### `Next`

```csharp
public IReadOnlyList<Rect>? Next(DisplayList list)
```

What `list` changes from the list of the previous call (see `Compute`).

| Parameter | Description |
| --- | --- |
| `list` | The list (DisplayList). |

Returns: The resulting IReadOnlyList<Rect>? value.

#### `Next` at a raster scale

```csharp
public IReadOnlyList<Rect>? Next(DisplayList list, float scale)
```

Tracks logical damage with the same curve and pixel-margin rules as `Compute`. Changing the raster scale requires a full repaint even if the display list is unchanged. Nonpositive or non-finite scales throw `ArgumentOutOfRangeException`.

#### `Reset`

```csharp
public void Reset()
```

Forgets the last list: the next one is painted whole.

#### `ToPixels`

```csharp
public static IReadOnlyList<PixelRect> ToPixels(IReadOnlyList<Rect> damage, float scale, int width, int height)
```

The damaged rectangles in whole physical pixels, grown by one pixel for antialiasing, inside the surface.

| Parameter | Description |
| --- | --- |
| `damage` | The damage (IReadOnlyList<Rect>). |
| `scale` | The number of physical pixels per logical unit. |
| `width` | The width of the target pixel buffer. |
| `height` | The height of the target pixel buffer. |

Returns: The resulting IReadOnlyList<PixelRect> value.


<a id="type-drawborder"></a>

## `DrawBorder`

Namespace: `Nazor.Graphics`

```csharp
public sealed record DrawBorder(Rect Rect, CornerRadius Radius, Thickness Widths, BorderSide Top, BorderSide Right, BorderSide Bottom, BorderSide Left) : DrawCommand
```

Fills the area between the outer box and the box inset by the border widths (CSS borders): the inner corners lose the widths of their sides. When the sides differ, each one is the trapezoid between the outer and the inner edge, joined diagonally at the corners.

| Parameter | Description |
| --- | --- |
| `Rect` | The rect (Rect). |
| `Radius` | The corner radius to apply uniformly. |
| `Widths` | The logical border thicknesses on the four edges. |
| `Top` | The value associated with the top edge. |
| `Right` | The value associated with the right side or operand. |
| `Bottom` | The value associated with the bottom edge. |
| `Left` | The value associated with the left side or operand. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `DrawBorder`

```csharp
public DrawBorder(Rect rect, CornerRadius radius, Thickness widths, Color color, LineStyle style = LineStyle.Solid)
```

Initializes a new instance of DrawBorder.

| Parameter | Description |
| --- | --- |
| `rect` | The rect (Rect). |
| `radius` | The corner radius to apply uniformly. |
| `widths` | The logical border thicknesses on the four edges. |
| `color` | The color (Color). |
| `style` | The style (LineStyle). |

#### `IsUniform`

```csharp
public bool IsUniform {get; }
```

The four sides have the same color and style.

#### `Brush`

```csharp
public GradientBrush? Brush { get; init; }
```

An optional gradient spanning the border ring instead of solid side colors. The outer and inner rounded edges still follow `Rect`, `Radius`, and `Widths`.

#### `Color`

```csharp
public Color Color {get; }
```

The color of the top side, the color of every side when `IsUniform`.


<a id="type-drawcommand"></a>

## `DrawCommand`

Namespace: `Nazor.Graphics`

```csharp
public abstract record DrawCommand
```

A drawing operation in logical units. Push commands are balanced by `Pop`.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-drawimage"></a>

## `DrawImage`

Namespace: `Nazor.Graphics`

```csharp
public sealed record DrawImage(DecodedImage Image, Rect Destination, Rect? Clip = null, Color? Tint = null) : DrawCommand
```

Draws `Image` stretched into `Destination`, clipped to `Clip` when set (an image that covers its box and overflows it). With `Tint` every pixel takes that color and keeps its alpha (an icon in the text color).

| Parameter | Description |
| --- | --- |
| `Image` | The rendered or decoded image to process. |
| `Destination` | The destination (Rect). |
| `Clip` | The clip (Rect?). |
| `Tint` | The tint (Color?). |

[Source](../../Sources/Core/Nazor.Graphics/Images.cs)


<a id="type-drawpolyline"></a>

## `DrawPolyline`

Namespace: `Nazor.Graphics`

```csharp
public sealed record DrawPolyline(IReadOnlyList<Point> Points, float Thickness, Color Color) : DrawCommand
```

Strokes connected segments with round caps and joins (check marks and similar vector parts).

| Parameter | Description |
| --- | --- |
| `Points` | The points (IReadOnlyList<Point>). |
| `Thickness` | The logical inset distances on the four edges. |
| `Color` | The color (Color). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-drawshadow"></a>

## `DrawShadow`

Namespace: `Nazor.Graphics`

```csharp
public sealed record DrawShadow(Rect Rect, CornerRadius Radius, Point Offset, float Blur, float Spread, Color Color, bool Inset) : DrawCommand
```

A CSS box shadow of the box `Rect`: the shape moved by `Offset`, grown by `Spread` and blurred (standard deviation half of `Blur`). An outer shadow is not drawn under the box; an inset one is drawn only inside it.

| Parameter | Description |
| --- | --- |
| `Rect` | The rect (Rect). |
| `Radius` | The corner radius to apply uniformly. |
| `Offset` | The offset (Point). |
| `Blur` | The blur (float). |
| `Spread` | The spread (float). |
| `Color` | The color (Color). |
| `Inset` | The inset (bool). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-drawtext"></a>

## `DrawText`

Namespace: `Nazor.Graphics`

```csharp
public sealed record DrawText(TextLayout Layout, Point Origin, Color Color, TextAlignment Alignment = TextAlignment.Start, float Width = 0, TextDecoration? Decoration = null) : DrawCommand
```

Draws a laid-out text with its top-left corner at `Origin`; each line is aligned within `Width`, with its `Decoration` if any.

| Parameter | Description |
| --- | --- |
| `Layout` | The layout (TextLayout). |
| `Origin` | The origin (Point). |
| `Color` | The color (Color). |
| `Alignment` | The alignment (TextAlignment). |
| `Width` | The width of the value in its coordinate system. |
| `Decoration` | The decoration (TextDecoration?). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `TextStart`

```csharp
public int TextStart { get; init; }
```

First UTF-16 source offset whose existing shaped glyphs are painted; zero by default. Skia clamps source offsets to the layout's text range.

#### `TextLength`

```csharp
public int? TextLength { get; init; }
```

Maximum UTF-16 source length painted, or null to include the remaining text. Layout, kerning and glyph positions remain unchanged. Negative lengths paint an empty range. A glyph cluster is painted by the range containing its source cluster start; partial-ligature color/caret subdivision is not provided.

#### `LineOffset`

```csharp
public float LineOffset(TextLine line)
```

Horizontal offset of a line inside the text box.

| Parameter | Description |
| --- | --- |
| `line` | The line (TextLine). |

Returns: The resulting float value.


<a id="type-fillgradient"></a>

## `FillGradient`

Namespace: `Nazor.Graphics`

```csharp
public sealed record FillGradient(Rect Rect, CornerRadius Radius, GradientBrush Brush) : DrawCommand
```

Fills a rectangle, rounded like `FillRectangle`, with a gradient.

| Parameter | Description |
| --- | --- |
| `Rect` | The rect (Rect). |
| `Radius` | The corner radius to apply uniformly. |
| `Brush` | The brush (GradientBrush). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-fillimage"></a>

## `FillImage`

Namespace: `Nazor.Graphics`

```csharp
public sealed record FillImage(Rect Area, CornerRadius Radius, DecodedImage Image, Rect Tile, bool RepeatX, bool RepeatY) : DrawCommand
```

Fills `Area` (with its rounded corners) with an image placed in `Tile` and repeated on the axes that repeat: a background image.

| Parameter | Description |
| --- | --- |
| `Area` | The area (Rect). |
| `Radius` | The corner radius to apply uniformly. |
| `Image` | The rendered or decoded image to process. |
| `Tile` | The tile (Rect). |
| `RepeatX` | The repeat x (bool). |
| `RepeatY` | The repeat y (bool). |

[Source](../../Sources/Core/Nazor.Graphics/Images.cs)


<a id="type-fillrectangle"></a>

## `FillRectangle`

Namespace: `Nazor.Graphics`

```csharp
public sealed record FillRectangle(Rect Rect, CornerRadius Radius, Color Color) : DrawCommand
```

Fills a rectangle, with rounded corners when `Radius` is not zero.

| Parameter | Description |
| --- | --- |
| `Rect` | The rect (Rect). |
| `Radius` | The corner radius to apply uniformly. |
| `Color` | The color (Color). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-fontface"></a>

## `FontFace`

Namespace: `Nazor.Graphics.Text`

```csharp
public abstract class FontFace
```

A font face resolved by an `IFontProvider`: opaque to layout, which uses only the metrics and glyph coverage. The backend that created it rasterizes it and shapes text with it, so measuring and drawing use the same file.

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `FamilyName`

```csharp
public abstract string FamilyName { get; }
```

Gets the resolved font family name.

#### `Weight`

```csharp
public abstract int Weight { get; }
```

Gets the numeric weight of the font face.

#### `Italic`

```csharp
public abstract bool Italic { get; }
```

Gets whether the font face uses italic styling.

#### `Metrics`

```csharp
public abstract FontMetrics Metrics { get; }
```

Gets the font's unscaled vertical metrics and design units.

#### `HasGlyph`

```csharp
public abstract bool HasGlyph(int codepoint)
```

Determines whether the font contains a glyph for the specified Unicode scalar value.

| Parameter | Description |
| --- | --- |
| `codepoint` | The Unicode scalar value of the character. |

Returns: true if this face contains the requested character; otherwise, false.

#### `Ascent`

```csharp
public float Ascent(float size)
```

Scales the font ascent to the specified logical font size.

| Parameter | Description |
| --- | --- |
| `size` | The logical font size used for measurement or shaping. |

Returns: The ascent in logical units at the requested font size.

#### `Descent`

```csharp
public float Descent(float size)
```

Scales the font descent to the specified logical font size.

| Parameter | Description |
| --- | --- |
| `size` | The logical font size used for measurement or shaping. |

Returns: The descent in logical units at the requested font size.

#### `LineGap`

```csharp
public float LineGap(float size)
```

Scales the font line gap to the specified logical font size.

| Parameter | Description |
| --- | --- |
| `size` | The logical font size used for measurement or shaping. |

Returns: The line gap in logical units at the requested font size.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-fontmetrics"></a>

## `FontMetrics`

Namespace: `Nazor.Graphics.Text`

```csharp
public readonly record struct FontMetrics(int UnitsPerEm, int Ascender, int Descender, int LineGap)
```

Font metrics in design units (ascender and descender both positive).

| Parameter | Description |
| --- | --- |
| `UnitsPerEm` | The units per em (int). |
| `Ascender` | The ascender (int). |
| `Descender` | The descender (int). |
| `LineGap` | The line gap (int). |

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)


<a id="type-fontoptions"></a>

## `FontOptions`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed record FontOptions
```

Private font files and fallback policy, independent of the renderer. Configure before startup.

[Source](../../Sources/Core/Nazor.Graphics/Text/FontOptions.cs)

### Members

#### `PrivateFontFiles`

```csharp
public IReadOnlyList<string> PrivateFontFiles { get; set;}
```

Paths relative to AppContext.BaseDirectory, or absolute paths. Private families precede system fonts.

#### `UseSystemFonts`

```csharp
public bool UseSystemFonts { get; init;}
```

Include installed fonts in matching and fallback. Disable for deterministic tests.

#### `DefaultFamily`

```csharp
public string DefaultFamily { get; init;}
```

Gets or sets the fallback family used when no requested family can be resolved.

#### `Add`

```csharp
public FontOptions Add(params string[] paths)
```

Add font files, including each required weight and style, once before startup.

| Parameter | Description |
| --- | --- |
| `paths` | The paths (string[]). |

Returns: The resulting FontOptions value.

Throws `ArgumentNullException`: paths is null.


<a id="type-gradientbrush"></a>

## `GradientBrush`

Namespace: `Nazor.Graphics`

```csharp
public abstract record GradientBrush(IReadOnlyList<GradientStop> Stops)
```

A gradient with its geometry resolved for one box.

| Parameter | Description |
| --- | --- |
| `Stops` | The stops (IReadOnlyList<GradientStop>). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `DescribeStops`

```csharp
protected string DescribeStops()
```

Formats the gradient stops for diagnostic output.

Returns: The resulting string value.


<a id="type-gradientstop"></a>

## `GradientStop`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct GradientStop(float Offset, Color Color)
```

A color at a position of a gradient, from 0 at its start to 1 at its end.

| Parameter | Description |
| --- | --- |
| `Offset` | The offset (float). |
| `Color` | The color (Color). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-ifontprovider"></a>

## `IFontProvider`

Namespace: `Nazor.Graphics.Text`

```csharp
public interface IFontProvider
```

Resolves families to faces and finds fallback faces for characters the face lacks.

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `Match`

```csharp
FontFace Match(IReadOnlyList<string> families, int weight, bool italic)
```

The first available family of the list, or the provider's default face.

| Parameter | Description |
| --- | --- |
| `families` | The preferred font families, in fallback order. |
| `weight` | The requested numeric font weight. |
| `italic` | Whether an italic face is preferred. |

Returns: The resulting FontFace value.

#### `Match` with optical size

```csharp
FontFace Match(IReadOnlyList<string> families, int weight, bool italic, float size)
```

Resolves a face at `size` logical pixels, including optical sizing when supported. The default interface implementation delegates to the three-argument overload, preserving existing providers. The Skia provider converts logical pixels to points for the `opsz` axis, clamps to the font's range, and uses identical variation coordinates for shaping and rasterization.

#### `Match` with text style

```csharp
FontFace Match(TextStyle style)
```

Resolves optical policy and explicit variation coordinates when the provider supports them. The default implementation uses the unsized match for `None` and the existing sized match otherwise; custom providers can override this overload for CSS `Auto` and explicit axes.

#### `Fallback` with text style

```csharp
FontFace? Fallback(int codepoint, FontFace preferred, TextStyle style)
```

Finds a covering face using the original requested selection settings and supported coordinates. The default implementation delegates to the existing fallback overload.


#### `Fallback`

```csharp
FontFace? Fallback(int codepoint, FontFace preferred)
```

A face that contains `codepoint`, or null when none is available.

| Parameter | Description |
| --- | --- |
| `codepoint` | The Unicode scalar value of the character. |
| `preferred` | The preferred font face before glyph fallback. |

Returns: The resulting FontFace? value.

#### `Version`

```csharp
int Version { get; }
```

Increments when fonts are registered: cached layouts become stale.


<a id="type-iimagedecoder"></a>

## `IImageDecoder`

Namespace: `Nazor.Graphics`

```csharp
public interface IImageDecoder
```

Decodes encoded images for a renderer; may be called from any thread.

[Source](../../Sources/Core/Nazor.Graphics/Images.cs)

### Members

#### `Decode`

```csharp
DecodedImage? Decode(ReadOnlySpan<byte> data)
```

The image, or null when the data is not an image this decoder reads.

| Parameter | Description |
| --- | --- |
| `data` | The data (ReadOnlySpan<byte>). |

Returns: The resulting DecodedImage? value.


<a id="type-irenderer"></a>

## `IRenderer`

Namespace: `Nazor.Graphics`

```csharp
public interface IRenderer : IDisposable
```

Draws display lists. The renderer also provides the font and shaping services, so text is measured with the same faces it is rasterized with.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Fonts`

```csharp
IFontProvider Fonts { get; }
```

Gets the provider used to resolve font faces and glyph fallbacks.

#### `Shaper`

```csharp
ITextShaper Shaper { get; }
```

Gets the service used to shape text into positioned glyphs.

#### `Images`

```csharp
IImageDecoder Images { get; }
```

Decodes images into images this renderer draws.

#### `RenderToImage`

```csharp
RenderedImage RenderToImage(DisplayList displayList, float scale, Color background)
```

Renders into a new image of `ceil(size × scale)` physical pixels.

| Parameter | Description |
| --- | --- |
| `displayList` | The ordered drawing commands and logical surface size to render. |
| `scale` | The number of physical pixels per logical unit. |
| `background` | The background color used to clear the surface. |

Returns: The resulting RenderedImage value.

#### `Render`

```csharp
void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background)
```

Renders into memory owned by the caller (for example a window buffer), BGRA premultiplied.

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
void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background, IReadOnlyList<PixelRect>? damage)
```

Renders only inside `damage` (physical pixels); the rest of the buffer keeps what it had. Null renders everything, an empty list nothing.

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
byte[] EncodePng(RenderedImage image)
```

Encodes the rendered pixel image as a PNG byte sequence.

| Parameter | Description |
| --- | --- |
| `image` | The rendered or decoded image to process. |

Returns: The PNG-encoded image bytes.


<a id="type-itextshaper"></a>

## `ITextShaper`

Namespace: `Nazor.Graphics.Text`

```csharp
public interface ITextShaper
```

Shapes a UTF-16 range with a face; clusters are indices of the source string.

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `Shape`

```csharp
ShapedRun Shape(string text, int start, int length, FontFace face, float size)
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


<a id="type-lineargradientbrush"></a>

## `LinearGradientBrush`

Namespace: `Nazor.Graphics`

```csharp
public sealed record LinearGradientBrush(Point Start, Point End, IReadOnlyList<GradientStop> Stops) : GradientBrush(Stops)
```

Colors along the line from `Start` to `End`, constant beyond its ends.

| Parameter | Description |
| --- | --- |
| `Start` | The gradient's start point in logical coordinates. |
| `End` | The gradient's end point in logical coordinates. |
| `Stops` | The stops (IReadOnlyList<GradientStop>). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-lineheightkind"></a>

## `LineHeightKind`

Namespace: `Nazor.Graphics.Text`

```csharp
public enum LineHeightKind : byte
```

Specifies the available line height kind values.

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `Normal`

```csharp
Normal
```

Uses the font's normal line metrics.

#### `Multiplier`

```csharp
Multiplier
```

Multiplies the font size by a unitless line-height factor.

#### `Fixed`

```csharp
Fixed
```

Uses an explicitly resolved line height.


<a id="type-linestyle"></a>

## `LineStyle`

Namespace: `Nazor.Graphics`

```csharp
public enum LineStyle : byte
```

Specifies the pattern used to draw a border line.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Solid`

```csharp
Solid
```

Draws an uninterrupted border line.

#### `Dashed`

```csharp
Dashed
```

Dashes three times as long as the width, with gaps as long as the width.

#### `Dotted`

```csharp
Dotted
```

Round dots as wide as the line, one width apart.


<a id="type-matrix"></a>

## `Matrix`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Matrix(float M11, float M12, float M21, float M22, float OffsetX, float OffsetY)
```

A 2D affine transform with the layout of `System.Numerics.Matrix3x2` and of CSS `matrix(a, b, c, d, e, f)`: x' = M11·x + M21·y + OffsetX, y' = M12·x + M22·y + OffsetY.

| Parameter | Description |
| --- | --- |
| `M11` | The m11 (float). |
| `M12` | The m12 (float). |
| `M21` | The m21 (float). |
| `M22` | The m22 (float). |
| `OffsetX` | The offset x (float). |
| `OffsetY` | The offset y (float). |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Identity`

```csharp
public static readonly Matrix Identity ;
```

The transform that preserves every point.

#### `IsIdentity`

```csharp
public bool IsIdentity {get; }
```

Gets whether the transform leaves every point unchanged.

#### `Translation`

```csharp
public static Matrix Translation(float x, float y)
```

Creates a transform that translates points by horizontal and vertical logical distances.

| Parameter | Description |
| --- | --- |
| `x` | The x (float). |
| `y` | The y (float). |

Returns: The resulting Matrix value.

#### `Scaling`

```csharp
public static Matrix Scaling(float x, float y)
```

Creates a transform that scales horizontal and vertical coordinates independently.

| Parameter | Description |
| --- | --- |
| `x` | The x (float). |
| `y` | The y (float). |

Returns: The resulting Matrix value.

#### `Rotation`

```csharp
public static Matrix Rotation(double degrees)
```

Clockwise on the screen (y grows downwards), as CSS `rotate()`.

| Parameter | Description |
| --- | --- |
| `degrees` | The clockwise rotation angle in degrees in screen coordinates. |

Returns: The resulting Matrix value.

#### `Skewing`

```csharp
public static Matrix Skewing(double xDegrees, double yDegrees)
```

Creates a transform that skews the horizontal and vertical axes by angles in degrees.

| Parameter | Description |
| --- | --- |
| `xDegrees` | The horizontal skew angle in degrees. |
| `yDegrees` | The vertical skew angle in degrees. |

Returns: The resulting Matrix value.

#### `Then`

```csharp
public Matrix Then(Matrix next)
```

This transform followed by `next`.

| Parameter | Description |
| --- | --- |
| `next` | The next (Matrix). |

Returns: The resulting Matrix value.

#### `Determinant`

```csharp
public float Determinant {get; }
```

Gets the determinant of the transform's linear component.

#### `TryInvert`

```csharp
public bool TryInvert(out Matrix inverse)
```

The inverse; false when the transform flattens the plane (a scale of zero).

| Parameter | Description |
| --- | --- |
| `inverse` | When this method returns, contains the inverse produced by the operation. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Transform`

```csharp
public Point Transform(Point point)
```

Applies this affine transform to the supplied point.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: The resulting Point value.

#### `TransformBounds`

```csharp
public Rect TransformBounds(Rect rect)
```

The axis-aligned bounds of the transformed rectangle.

| Parameter | Description |
| --- | --- |
| `rect` | The rect (Rect). |

Returns: The resulting Rect value.

#### `Interpolate`

```csharp
public static Matrix Interpolate(Matrix from, Matrix to, double progress)
```

The transform between two others, as CSS interpolates matrices (CSS Transforms 1, 2D decomposition): translation, scale, rotation by the short way and the remaining matrix move separately, so a rotation stays a rotation halfway.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The resulting Matrix value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-pixelrect"></a>

## `PixelRect`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
```

A rectangle of whole physical pixels of a surface.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Right`

```csharp
public int Right {get; }
```

Gets the physical pixel coordinate immediately after the right edge.

#### `Bottom`

```csharp
public int Bottom {get; }
```

Gets the physical pixel coordinate immediately after the bottom edge.


<a id="type-point"></a>

## `Point`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Point(float X, float Y)
```

A point in logical units.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Zero`

```csharp
public static readonly Point Zero ;
```

The point with all components set to zero.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-pop"></a>

## `Pop`

Namespace: `Nazor.Graphics`

```csharp
public sealed record Pop : DrawCommand
```

Restores the drawing state saved by the most recent push command.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Instance`

```csharp
public static readonly Pop Instance ;
```

The shared drawing-state restoration command.


<a id="type-pushclip"></a>

## `PushClip`

Namespace: `Nazor.Graphics`

```csharp
public sealed record PushClip(Rect Rect, CornerRadius Radius) : DrawCommand
```

Intersects the current drawing clip with a rounded rectangle until the matching Pop command.

| Parameter | Description |
| --- | --- |
| `Rect` | The rect (Rect). |
| `Radius` | The corner radius to apply uniformly. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-pushopacity"></a>

## `PushOpacity`

Namespace: `Nazor.Graphics`

```csharp
public sealed record PushOpacity(float Opacity, Rect Bounds) : DrawCommand
```

Composites the following commands with an opacity, within `Bounds`.

| Parameter | Description |
| --- | --- |
| `Opacity` | The alpha multiplier, clamped to the range from zero to one. |
| `Bounds` | The bounds (Rect). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-pushtransform"></a>

## `PushTransform`

Namespace: `Nazor.Graphics`

```csharp
public sealed record PushTransform(Matrix Matrix) : DrawCommand
```

Transforms the following commands (CSS `transform`), in the current coordinates.

| Parameter | Description |
| --- | --- |
| `Matrix` | The matrix (Matrix). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-pushtranslate"></a>

## `PushTranslate`

Namespace: `Nazor.Graphics`

```csharp
public sealed record PushTranslate(float X, float Y) : DrawCommand
```

Translates subsequent drawing commands until the matching Pop command.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)


<a id="type-radialgradientbrush"></a>

## `RadialGradientBrush`

Namespace: `Nazor.Graphics`

```csharp
public sealed record RadialGradientBrush(Point Center, float RadiusX, float RadiusY, IReadOnlyList<GradientStop> Stops) : GradientBrush(Stops)
```

Colors along the radii of an ellipse centered at `Center`, constant beyond it.

| Parameter | Description |
| --- | --- |
| `Center` | The center (Point). |
| `RadiusX` | The radius x (float). |
| `RadiusY` | The radius y (float). |
| `Stops` | The stops (IReadOnlyList<GradientStop>). |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-radius"></a>

## `Radius`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Radius(float X, float Y)
```

An elliptical corner: `X` along the width, `Y` along the height.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Zero`

```csharp
public static readonly Radius Zero ;
```

The radius with all components set to zero.

#### `IsZero`

```csharp
public bool IsZero {get; }
```

Gets whether either radius axis is nonpositive, producing a square corner.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-rect"></a>

## `Rect`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Rect(float X, float Y, float Width, float Height)
```

An axis-aligned rectangle in logical units.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Empty`

```csharp
public static readonly Rect Empty ;
```

An empty rect value.

#### `Right`

```csharp
public float Right {get; }
```

Gets the horizontal coordinate of the rectangle's right edge.

#### `Bottom`

```csharp
public float Bottom {get; }
```

Gets the vertical coordinate of the rectangle's bottom edge.

#### `Position`

```csharp
public Point Position {get; }
```

Gets the top-left corner in logical units.

#### `Size`

```csharp
public Size Size {get; }
```

Gets the width and height in logical units.

#### `IsEmpty`

```csharp
public bool IsEmpty {get; }
```

Gets whether the width or height is nonpositive.

#### `Contains`

```csharp
public bool Contains(Point point)
```

Tests whether the point lies inside the rectangle, including the top and left edges and excluding the bottom and right edges.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: true if the point lies inside the rectangle's half-open bounds; otherwise, false.

#### `Deflate`

```csharp
public Rect Deflate(Thickness thickness)
```

Insets the rectangle by the supplied edge thicknesses, clamping its resulting dimensions at zero.

| Parameter | Description |
| --- | --- |
| `thickness` | The logical inset distances on the four edges. |

Returns: The resulting Rect value.

#### `Offset`

```csharp
public Rect Offset(float dx, float dy)
```

Translates the rectangle without changing its dimensions.

| Parameter | Description |
| --- | --- |
| `dx` | The horizontal translation in logical units. |
| `dy` | The vertical translation in logical units. |

Returns: The resulting Rect value.

#### `Inflate`

```csharp
public Rect Inflate(float amount)
```

The rectangle grown by `amount` on every side (shrunk when negative).

| Parameter | Description |
| --- | --- |
| `amount` | The logical distance by which to grow, shrink, or offset the value. |

Returns: The resulting Rect value.

#### `Intersect`

```csharp
public Rect Intersect(Rect other)
```

Computes the overlapping rectangle, returning Empty when there is no positive-area intersection.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: The intersection rectangle, or Empty when there is no positive-area overlap.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-renderedimage"></a>

## `RenderedImage`

Namespace: `Nazor.Graphics`

```csharp
public sealed class RenderedImage(int width, int height, byte[] pixels)
```

Pixels in BGRA order with premultiplied alpha, rows top-down.

| Parameter | Description |
| --- | --- |
| `width` | The width of the target pixel buffer. |
| `height` | The height of the target pixel buffer. |
| `pixels` | The address of the top-down BGRA premultiplied pixel buffer. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Width`

```csharp
public int Width { get; }
```

Gets the image width in physical pixels.

#### `Height`

```csharp
public int Height { get; }
```

Gets the image height in physical pixels.

#### `Pixels`

```csharp
public byte[] Pixels { get; }
```

Gets the top-down BGRA premultiplied pixel buffer.

#### `Stride`

```csharp
public int Stride {get; }
```

Gets the number of bytes between the starts of successive pixel rows.

#### `GetPixel`

```csharp
public Color GetPixel(int x, int y)
```

The color of a pixel, un-premultiplied.

| Parameter | Description |
| --- | --- |
| `x` | The x (int). |
| `y` | The y (int). |

Returns: The resulting Color value.


<a id="type-shapedrun"></a>

## `ShapedRun`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed class ShapedRun
```

Glyphs of a text range shaped with one face, in logical units for the given font size.

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `ShapedRun`

```csharp
public ShapedRun(FontFace face, float size, int start, int length, ushort[] glyphs, float[] advances, float[] offsetsX, float[] offsetsY, int[] clusters)
```

Initializes a new instance of ShapedRun.

| Parameter | Description |
| --- | --- |
| `face` | The font face used to shape or measure text. |
| `size` | The logical font size used for measurement or shaping. |
| `start` | The start offset in the UTF-16 source text. |
| `length` | The number of UTF-16 code units in the text segment. |
| `glyphs` | The font-specific glyph identifiers in drawing order. |
| `advances` | The horizontal advance of each glyph in logical units. |
| `offsetsX` | The horizontal offset of each glyph from its nominal position. |
| `offsetsY` | The vertical offset of each glyph from its nominal position. |
| `clusters` | The source-text UTF-16 cluster offsets associated with the glyphs. |

#### `Face`

```csharp
public FontFace Face { get; }
```

Gets the font face used by this glyph run.

#### `Size`

```csharp
public float Size { get; }
```

Gets the logical font size used to shape the run.

#### `Start`

```csharp
public int Start { get; }
```

UTF-16 range of the source text covered by the run.

#### `Length`

```csharp
public int Length { get; }
```

Gets the length of the source text segment in UTF-16 code units.

#### `Glyphs`

```csharp
public ushort[] Glyphs { get; }
```

Gets the font-specific glyph identifiers in drawing order.

#### `Advances`

```csharp
public float[] Advances { get; }
```

Gets the horizontal advance of each glyph in logical units.

#### `OffsetsX`

```csharp
public float[] OffsetsX { get; }
```

Gets the horizontal positioning adjustment for each glyph.

#### `OffsetsY`

```csharp
public float[] OffsetsY { get; }
```

Gets the vertical positioning adjustment for each glyph.

#### `Clusters`

```csharp
public int[] Clusters { get; }
```

For each glyph, the UTF-16 index (in the source text) where its cluster starts.

#### `Width`

```csharp
public float Width { get; }
```

Gets the total horizontal advance of the run in logical units.

#### `Slice`

```csharp
public ShapedRun Slice(int start, int end)
```

The glyphs whose clusters fall in [start, end), without shaping again.

| Parameter | Description |
| --- | --- |
| `start` | The start offset in the UTF-16 source text. |
| `end` | The end offset in the UTF-16 source text. |

Returns: The resulting ShapedRun value.


<a id="type-shapedtext"></a>

## `ShapedText`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed class ShapedText
```

Text shaped once for a style: its runs of glyphs, the advance of every character and its widths without wrapping (max-content) and of its widest unbreakable word (min-content). Layouts of any width break it into lines without shaping it again. Characters missing from the primary face use fallback faces, chosen per grapheme.

[Source](../../Sources/Core/Nazor.Graphics/Text/TextLayout.cs)

### Members

#### `ShapedText`

```csharp
public ShapedText(string text, TextStyle style, IFontProvider fonts, ITextShaper shaper)
```

Initializes a new instance of ShapedText.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |
| `style` | The style (TextStyle). |
| `fonts` | The provider used to resolve font faces and fallback glyphs. |
| `shaper` | The service that converts text into positioned glyphs. |

#### `ShapedText` (styled runs)

```csharp
public ShapedText(IReadOnlyList<TextRun> runs, TextStyle style, IFontProvider fonts, ITextShaper shaper)
```

Shapes a copied run collection into one UTF-16 coordinate space. Adjacent segments with equal shaping styles retain shared kerning. The parent style supplies the wrapping policy and line strut; styled fonts, baseline shifts and insets contribute to each line's metrics.

| Parameter | Description |
| --- | --- |
| `runs` | Styled segments concatenated in source order; the collection is copied. |
| `style` | Parent line strut and wrapping policy. |
| `fonts` | Font face and fallback provider. |
| `shaper` | Positioned glyph shaping service. |

Run advances/insets must be nonnegative and all geometry finite; invalid values throw ArgumentOutOfRangeException. Avoid splitting a grapheme or ligature across different styles; this overload retains the existing horizontal LTR shaping model.

#### `Text`

```csharp
public string Text { get; }
```

Gets the text represented by this object.

#### `Style`

```csharp
public TextStyle Style { get; }
```

Gets the text style used for shaping and font resolution.

#### `FontVersion`

```csharp
public int FontVersion { get; }
```

Gets the font-provider version used to shape this text.

#### `Primary`

```csharp
public FontFace Primary { get; }
```

Gets the primary resolved font face for the text.

#### `Runs`

```csharp
public IReadOnlyList<ShapedRun> Runs { get; }
```

The shaped runs of the whole text, before line breaking.

#### `MaxContentWidth`

```csharp
public float MaxContentWidth { get; }
```

Width without wrapping: the widest paragraph.

#### `MinContentWidth`

```csharp
public float MinContentWidth { get; }
```

Width of the widest unbreakable word.


<a id="type-size"></a>

## `Size`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Size(float Width, float Height)
```

A size in logical units.

| Parameter | Description |
| --- | --- |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Zero`

```csharp
public static readonly Size Zero ;
```

The size with all components set to zero.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-textalignment"></a>

## `TextAlignment`

Namespace: `Nazor.Graphics`

```csharp
public enum TextAlignment : byte
```

Specifies text placement along the inline axis.

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `Start`

```csharp
Start
```

Aligns text to the start of the available inline space.

#### `Center`

```csharp
Center
```

Centers text in the available inline space.

#### `End`

```csharp
End
```

Aligns text to the end of the available inline space.


<a id="type-textdecoration"></a>

## `TextDecoration`

Namespace: `Nazor.Graphics`

```csharp
public sealed record TextDecoration(DecorationLines Lines, Color Color, DecorationStyle Style = DecorationStyle.Solid, float Thickness = 0)
```

Lines drawn with a text (CSS text-decoration): under, over or through each line of text, from its start to the end of its content. A `Thickness` of 0 takes the one of the font.

| Parameter | Description |
| --- | --- |
| `Lines` | The lines (DecorationLines). |
| `Color` | The color (Color). |
| `Style` | The style (DecorationStyle). |
| `Thickness` | The logical inset distances on the four edges. |

[Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-textlayout"></a>

## `TextLayout`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed class TextLayout
```

Shaped text broken into lines; the same instance serves measuring, drawing and hit-testing, so what is measured is what is drawn. Lines break at '\n' and, when a maximum width is given and wrapping is allowed, after spaces. Only left-to-right text layout is supported.

[Source](../../Sources/Core/Nazor.Graphics/Text/TextLayout.cs)

### Members

#### `TextLayout`

```csharp
public TextLayout(string text, TextStyle style, float? maxWidth, IFontProvider fonts, ITextShaper shaper, bool wrap = true)
```

Initializes a new instance of TextLayout.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |
| `style` | The style (TextStyle). |
| `maxWidth` | The optional maximum logical width available for text layout. |
| `fonts` | The provider used to resolve font faces and fallback glyphs. |
| `shaper` | The service that converts text into positioned glyphs. |
| `wrap` | Whether text may wrap onto additional lines. |

#### `TextLayout`

```csharp
public TextLayout(ShapedText shaped, float? maxWidth, bool wrap = true)
```

A layout of text already shaped: only the lines are broken for this width.

| Parameter | Description |
| --- | --- |
| `shaped` | The shaped (ShapedText). |
| `maxWidth` | The optional maximum logical width available for text layout. |
| `wrap` | Whether text may wrap onto additional lines. |

#### `Shaped`

```csharp
public ShapedText Shaped {get; }
```

The shaped text, shared by the layouts of other widths.

#### `Text`

```csharp
public string Text {get; }
```

Gets the text represented by this object.

#### `Style`

```csharp
public TextStyle Style {get; }
```

Gets the text style used for shaping, line breaking, and alignment.

#### `MaxWidth`

```csharp
public float? MaxWidth { get; }
```

Gets the optional logical width constraint used to wrap the text.

#### `FontVersion`

```csharp
public int FontVersion {get; }
```

Gets the font-provider version used to shape this text.

#### `Primary`

```csharp
public FontFace Primary {get; }
```

Gets the primary resolved font face for the text.

#### `Runs`

```csharp
public IReadOnlyList<ShapedRun> Runs {get; }
```

The shaped runs of the whole text, before line breaking.

#### `Lines`

```csharp
public IReadOnlyList<TextLine> Lines { get; }
```

Gets the ordered laid-out text lines.

#### `Width`

```csharp
public float Width { get; }
```

Width of the widest line, trailing spaces excluded.

#### `Height`

```csharp
public float Height { get; }
```

Gets the total logical height of the laid-out text.

#### `MaxContentWidth`

```csharp
public float MaxContentWidth {get; }
```

Width without wrapping: the widest paragraph.

#### `MinContentWidth`

```csharp
public float MinContentWidth {get; }
```

Width of the widest unbreakable word.

#### `Baseline`

```csharp
public float Baseline {get; }
```

Gets the baseline offset of the first text line.

#### `Size`

```csharp
public Size Size {get; }
```

Gets the logical bounds occupied by the text layout.

#### `GetCaretX`

```csharp
public float GetCaretX(int index)
```

The x offset of the caret before `index` on its line.

| Parameter | Description |
| --- | --- |
| `index` | The zero-based collection position. |

Returns: The resulting float value.

#### `GetCaretRect`

```csharp
public Rect GetCaretRect(int index)
```

The caret rectangle (zero width) at `index`.

| Parameter | Description |
| --- | --- |
| `index` | The zero-based collection position. |

Returns: The resulting Rect value.

#### `HitTest`

```csharp
public int HitTest(Point point)
```

The caret index closest to `point`, at grapheme boundaries.

| Parameter | Description |
| --- | --- |
| `point` | The position in logical viewport coordinates. |

Returns: The resulting int value.

#### `GetSelectionRects`

```csharp
public IEnumerable<Rect> GetSelectionRects(int start, int end)
```

One rectangle per line covering [start, end).

| Parameter | Description |
| --- | --- |
| `start` | The start offset in the UTF-16 source text. |
| `end` | The end offset in the UTF-16 source text. |

Returns: The resulting IEnumerable<Rect> value.

#### `NextCaretIndex`

```csharp
public int NextCaretIndex(int index)
```

The next caret position after `index` (next grapheme boundary).

| Parameter | Description |
| --- | --- |
| `index` | The zero-based collection position. |

Returns: The resulting int value.

#### `PreviousCaretIndex`

```csharp
public int PreviousCaretIndex(int index)
```

The previous caret position before `index`.

| Parameter | Description |
| --- | --- |
| `index` | The zero-based collection position. |

Returns: The resulting int value.

#### `LineAt`

```csharp
public TextLine LineAt(int index)
```

The line of the caret before `index`: a line break or a wrap starts the next line.

| Parameter | Description |
| --- | --- |
| `index` | The zero-based collection position. |

Returns: The resulting TextLine value.


<a id="type-textline"></a>

## `TextLine`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed class TextLine
```

A line of a `TextLayout`; coordinates are relative to the layout's top-left corner.

[Source](../../Sources/Core/Nazor.Graphics/Text/TextLayout.cs)

### Members

#### `Start`

```csharp
public int Start { get; }
```

UTF-16 range of the line, including trailing spaces and the line break.

#### `End`

```csharp
public int End { get; }
```

Gets the exclusive UTF-16 end offset of the line's source text.

#### `ContentEnd`

```csharp
public int ContentEnd { get; }
```

End of the visible content: trailing spaces hang outside the line width.

#### `Runs`

```csharp
public IReadOnlyList<ShapedRun> Runs { get; }
```

Gets the shaped glyph runs displayed on this line.

#### `Width`

```csharp
public float Width { get; }
```

Gets the line width in logical units.

#### `Top`

```csharp
public float Top { get; }
```

Gets the line's top offset within the text layout in logical units.

#### `Height`

```csharp
public float Height { get; }
```

Gets the line's logical height.

#### `Ascent`

```csharp
public float Ascent { get; }
```

Gets the logical distance from the line's top to its baseline.

#### `Descent`

```csharp
public float Descent { get; }
```

Gets the logical distance below the baseline.

#### `Baseline`

```csharp
public float Baseline { get; }
```

Distance of the baseline from the top of the layout.



<a id="type-textrun"></a>

## `TextRun`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed record TextRun(string Text, TextStyle Style, float BaselineShift = 0, float LeadingAdvance = 0, float TrailingAdvance = 0, bool NoWrap = false, float TopInset = 0, float BottomInset = 0)
```

A styled UTF-16 segment in a shared line-breaking and selection layout. The record is immutable; its positional parameters define init-only properties. [Source](../../Sources/Core/Nazor.Graphics/Text/TextRun.cs)

| Parameter | Description |
| --- | --- |
| `Text` | Segment text, concatenated in source order. |
| `Style` | Font and line metrics for this segment. |
| `BaselineShift` | Logical distance above the shared baseline; negative lowers the text. |
| `LeadingAdvance` | Extra logical space before the first glyph for box edges. |
| `TrailingAdvance` | Extra logical space after the last glyph for box edges. |
| `NoWrap` | Excludes soft and emergency line breaks inside the segment; explicit newlines remain hard breaks. |
| `TopInset` | Additional line-box space above the text for decoration. |
| `BottomInset` | Additional line-box space below the text for decoration. |

### `TextRun` declarations

| Declaration | Contract |
| --- | --- |
| `public sealed record TextRun : IEquatable<TextRun>` | Immutable styled source segment. |
| `public TextRun(string Text, TextStyle Style, float BaselineShift = 0, float LeadingAdvance = 0, float TrailingAdvance = 0, bool NoWrap = false, float TopInset = 0, float BottomInset = 0)` | Creates a styled UTF-16 segment. Geometry is validated when ShapedText consumes the run. |
| `public override string ToString()` | Formats the record and its values. |
| `public static bool operator !=(TextRun? left, TextRun? right)` | Compares the record values for inequality. |
| `public static bool operator ==(TextRun? left, TextRun? right)` | Compares the record values for equality. |
| `public override int GetHashCode()` | Hashes the record values. |
| `public override bool Equals(object? obj)` | Compares another object with this record. |
| `public bool Equals(TextRun? other)` | Compares another styled segment by value. |
| `public void Deconstruct(out string Text, out TextStyle Style, out float BaselineShift, out float LeadingAdvance, out float TrailingAdvance, out bool NoWrap, out float TopInset, out float BottomInset)` | Deconstructs the styled segment. |
| `public string Text { get; init; }` | Segment source text. |
| `public TextStyle Style { get; init; }` | Segment font and line metrics. |
| `public float BaselineShift { get; init; }` | Logical distance above the shared baseline. |
| `public float LeadingAdvance { get; init; }` | Additional logical advance before the first glyph. |
| `public float TrailingAdvance { get; init; }` | Additional logical advance after the last glyph. |
| `public bool NoWrap { get; init; }` | Suppresses soft breaks inside this segment. |
| `public float TopInset { get; init; }` | Additional upper line-box space. |
| `public float BottomInset { get; init; }` | Additional lower line-box space. |

<a id="type-textstyle"></a>

## `TextStyle`

Namespace: `Nazor.Graphics.Text`

```csharp
public sealed record TextStyle(IReadOnlyList<string> FontFamilies, float FontSize, int FontWeight = 400, bool Italic = false, LineHeightKind LineHeightKind = LineHeightKind.Normal, float LineHeight = 0, float LetterSpacing = 0, BreakWords BreakWords = BreakWords.Never)
```

The text properties that affect shaping and line metrics (color is a paint property).

| Parameter | Description |
| --- | --- |
| `FontFamilies` | The font families (IReadOnlyList<string>). |
| `FontSize` | The font size (float). |
| `FontWeight` | The font weight (int). |
| `Italic` | Whether an italic face is preferred. |
| `LineHeightKind` | The line height kind (LineHeightKind). |
| `LineHeight` | The line height (float). |
| `LetterSpacing` | The letter spacing (float). |
| `BreakWords` | The break words (BreakWords). |

[Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

### Members

#### `Equals`

```csharp
public bool Equals(TextStyle? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.


<a id="type-thickness"></a>

## `Thickness`

Namespace: `Nazor.Graphics`

```csharp
public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
```

Distances from the four edges of a box.

| Parameter | Description |
| --- | --- |
| `Left` | The value associated with the left side or operand. |
| `Top` | The value associated with the top edge. |
| `Right` | The value associated with the right side or operand. |
| `Bottom` | The value associated with the bottom edge. |

[Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

### Members

#### `Zero`

```csharp
public static readonly Thickness Zero ;
```

The thickness with all components set to zero.

#### `Horizontal`

```csharp
public float Horizontal {get; }
```

Gets the sum of the left and right edge distances.

#### `Vertical`

```csharp
public float Vertical {get; }
```

Gets the sum of the top and bottom edge distances.

#### `IsZero`

```csharp
public bool IsZero {get; }
```

Gets whether all four edge distances are zero.

#### `operator +`

```csharp
public static Thickness operator +(Thickness a, Thickness b)
```

Adds the corresponding components of the two operands.

| Parameter | Description |
| --- | --- |
| `a` | The a (Thickness). |
| `b` | The blue channel in the range from zero to 255. |

Returns: The resulting Thickness value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `BorderSide`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct BorderSide : IEquatable<BorderSide>` | The color and line style of one side of a border.<br>`Color`. The color (Color).<br>`Style`. The style (LineStyle). |
| `public BorderSide()` |  |
| `public BorderSide(Color Color, LineStyle Style = LineStyle.Solid)` | The color and line style of one side of a border.<br>`Color`. The color (Color).<br>`Style`. The style (LineStyle). |
| `public static bool operator !=(BorderSide left, BorderSide right)` |  |
| `public static bool operator ==(BorderSide left, BorderSide right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(BorderSide other)` |  |
| `public void Deconstruct(out Color Color, out LineStyle Style)` |  |
| `public Color Color { get; init; }` | The color (Color). |
| `public LineStyle Style { get; init; }` | The style (LineStyle). |

### `Color`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Color : IEquatable<Color>` | An sRGB color with straight alpha; converted to the backend format only by the renderer.<br>`R`. The red channel in the range from zero to 255.<br>`G`. The green channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`A`. The alpha channel in the range from zero to 255. |
| `public Color()` |  |
| `public Color(byte R, byte G, byte B, byte A)` | An sRGB color with straight alpha; converted to the backend format only by the renderer.<br>`R`. The red channel in the range from zero to 255.<br>`G`. The green channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`A`. The alpha channel in the range from zero to 255. |
| `public static bool operator !=(Color left, Color right)` |  |
| `public static bool operator ==(Color left, Color right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Color other)` |  |
| `public void Deconstruct(out byte R, out byte G, out byte B, out byte A)` |  |
| `public byte R { get; init; }` | The red channel in the range from zero to 255. |
| `public byte G { get; init; }` | The green channel in the range from zero to 255. |
| `public byte B { get; init; }` | The blue channel in the range from zero to 255. |
| `public byte A { get; init; }` | The alpha channel in the range from zero to 255. |

### `CompositionTimeline`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/CompositionTimeline.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract class CompositionTimeline` | An immutable snapshot of a visual transform and group opacity animation, safe for a render worker. |
| `public abstract (Matrix Transform, float Opacity) Sample(TimeSpan elapsed)` | Samples this detached timeline without accessing UI objects.<br>`elapsed`. The elapsed time since capture.<br>**Returns**. The visual transform and clamped group opacity. |
| `protected CompositionTimeline()` |  |
| `public abstract long CapturedAt { get; }` | Gets the monotonic Stopwatch timestamp of the snapshot. |
| `public abstract TimeSpan Duration { get; }` | Gets the remaining animation duration, including delay; MaxValue represents unbounded repetition. |

### `CornerRadius`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct CornerRadius : IFormattable, IEquatable<CornerRadius>` | The radii of the rounded corners of a box, one elliptical `Nazor.Graphics.Radius` per corner as in CSS. Two numbers or a single number give the same radius to the four corners.<br>`TopLeft`. The top left (Radius).<br>`TopRight`. The top right (Radius).<br>`BottomRight`. The bottom right (Radius).<br>`BottomLeft`. The bottom left (Radius). |
| `public CornerRadius()` |  |
| `public CornerRadius(Radius TopLeft, Radius TopRight, Radius BottomRight, Radius BottomLeft)` | The radii of the rounded corners of a box, one elliptical `Nazor.Graphics.Radius` per corner as in CSS. Two numbers or a single number give the same radius to the four corners.<br>`TopLeft`. The top left (Radius).<br>`TopRight`. The top right (Radius).<br>`BottomRight`. The bottom right (Radius).<br>`BottomLeft`. The bottom left (Radius). |
| `public static bool operator !=(CornerRadius left, CornerRadius right)` |  |
| `public static bool operator ==(CornerRadius left, CornerRadius right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(CornerRadius other)` |  |
| `public void Deconstruct(out Radius TopLeft, out Radius TopRight, out Radius BottomRight, out Radius BottomLeft)` |  |
| `public Radius TopLeft { get; init; }` | The top left (Radius). |
| `public Radius TopRight { get; init; }` | The top right (Radius). |
| `public Radius BottomRight { get; init; }` | The bottom right (Radius). |
| `public Radius BottomLeft { get; init; }` | The bottom left (Radius). |

### `DecodedImage`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Images.cs)

| Declaration | Contract |
| --- | --- |
| `protected DecodedImage()` |  |

### `DecorationLines`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DecorationLines` | Specifies text-decoration positions; values can be combined. |

### `DecorationStyle`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public enum DecorationStyle` | Specifies the stroke pattern used for text decorations. |
| `Solid = 0` | Draws a continuous decoration line. |
| `Double = 1` | Draws two parallel decoration lines. |
| `Dotted = 2` | Draws the decoration as dots. |
| `Dashed = 3` | Draws the decoration as dashes. |
| `Wavy = 4` | Draws the decoration as a wavy line. |

### `DisplayList`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class DisplayList` | An immutable list of drawing commands for a surface of `Nazor.Graphics.DisplayList.Size` logical units.<br>`size`. The logical dimensions of the surface or content.<br>`commands`. The ordered drawing commands. |
| `public DisplayList(Size size, IReadOnlyList<DrawCommand> commands)` | An immutable list of drawing commands for a surface of `Nazor.Graphics.DisplayList.Size` logical units.<br>`size`. The logical dimensions of the surface or content.<br>`commands`. The ordered drawing commands. |

### `DisplayListBuilder`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public DisplayListBuilder()` |  |

### `DisplayListDamage`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayListDamage.cs)

| Declaration | Contract |
| --- | --- |
| `public DisplayListDamage()` |  |

### `DrawBorder`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrawBorder : DrawCommand, IEquatable<DrawBorder>` | Fills the area between the outer box and the box inset by the border widths (CSS borders): the inner corners lose the widths of their sides. When the sides differ, each one is the trapezoid between the outer and the inner edge, joined diagonally at the corners.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Widths`. The logical border thicknesses on the four edges.<br>`Top`. The value associated with the top edge.<br>`Right`. The value associated with the right side or operand.<br>`Bottom`. The value associated with the bottom edge.<br>`Left`. The value associated with the left side or operand. |
| `public DrawBorder(Rect Rect, CornerRadius Radius, Thickness Widths, BorderSide Top, BorderSide Right, BorderSide Bottom, BorderSide Left)` | Fills the area between the outer box and the box inset by the border widths (CSS borders): the inner corners lose the widths of their sides. When the sides differ, each one is the trapezoid between the outer and the inner edge, joined diagonally at the corners.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Widths`. The logical border thicknesses on the four edges.<br>`Top`. The value associated with the top edge.<br>`Right`. The value associated with the right side or operand.<br>`Bottom`. The value associated with the bottom edge.<br>`Left`. The value associated with the left side or operand. |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawBorder? left, DrawBorder? right)` |  |
| `public static bool operator ==(DrawBorder? left, DrawBorder? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(DrawBorder? other)` |  |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius, out Thickness Widths, out BorderSide Top, out BorderSide Right, out BorderSide Bottom, out BorderSide Left)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Rect { get; init; }` | The rect (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |
| `public Thickness Widths { get; init; }` | The logical border thicknesses on the four edges. |
| `public BorderSide Top { get; init; }` | The value associated with the top edge. |
| `public BorderSide Right { get; init; }` | The value associated with the right side or operand. |
| `public BorderSide Bottom { get; init; }` | The value associated with the bottom edge. |
| `public BorderSide Left { get; init; }` | The value associated with the left side or operand. |

### `DrawCommand`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record DrawCommand : IEquatable<DrawCommand>` | A drawing operation in logical units. Push commands are balanced by `Nazor.Graphics.Pop`. |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawCommand? left, DrawCommand? right)` |  |
| `public static bool operator ==(DrawCommand? left, DrawCommand? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(DrawCommand? other)` |  |
| `protected DrawCommand(DrawCommand original)` |  |
| `protected DrawCommand()` |  |
| `protected virtual Type EqualityContract { get; }` |  |

### `DrawImage`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Images.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrawImage : DrawCommand, IEquatable<DrawImage>` | Draws `Image` stretched into `Destination`, clipped to `Clip` when set (an image that covers its box and overflows it). With `Tint` every pixel takes that color and keeps its alpha (an icon in the text color).<br>`Image`. The rendered or decoded image to process.<br>`Destination`. The destination (Rect).<br>`Clip`. The clip (Rect?).<br>`Tint`. The tint (Color?). |
| `public DrawImage(DecodedImage Image, Rect Destination, Rect? Clip = null, Color? Tint = null)` | Draws `Image` stretched into `Destination`, clipped to `Clip` when set (an image that covers its box and overflows it). With `Tint` every pixel takes that color and keeps its alpha (an icon in the text color).<br>`Image`. The rendered or decoded image to process.<br>`Destination`. The destination (Rect).<br>`Clip`. The clip (Rect?).<br>`Tint`. The tint (Color?). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawImage? left, DrawImage? right)` |  |
| `public static bool operator ==(DrawImage? left, DrawImage? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(DrawImage? other)` |  |
| `public void Deconstruct(out DecodedImage Image, out Rect Destination, out Rect? Clip, out Color? Tint)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public DecodedImage Image { get; init; }` | The rendered or decoded image to process. |
| `public Rect Destination { get; init; }` | The destination (Rect). |
| `public Rect? Clip { get; init; }` | The clip (Rect?). |
| `public Color? Tint { get; init; }` | The tint (Color?). |

### `DrawPolyline`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrawPolyline : DrawCommand, IEquatable<DrawPolyline>` | Strokes connected segments with round caps and joins (check marks and similar vector parts).<br>`Points`. The points (IReadOnlyList<Point>).<br>`Thickness`. The logical inset distances on the four edges.<br>`Color`. The color (Color). |
| `public DrawPolyline(IReadOnlyList<Point> Points, float Thickness, Color Color)` | Strokes connected segments with round caps and joins (check marks and similar vector parts).<br>`Points`. The points (IReadOnlyList<Point>).<br>`Thickness`. The logical inset distances on the four edges.<br>`Color`. The color (Color). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawPolyline? left, DrawPolyline? right)` |  |
| `public static bool operator ==(DrawPolyline? left, DrawPolyline? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(DrawPolyline? other)` |  |
| `public void Deconstruct(out IReadOnlyList<Point> Points, out float Thickness, out Color Color)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public IReadOnlyList<Point> Points { get; init; }` | The points (IReadOnlyList<Point>). |
| `public float Thickness { get; init; }` | The logical inset distances on the four edges. |
| `public Color Color { get; init; }` | The color (Color). |

### `DrawShadow`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrawShadow : DrawCommand, IEquatable<DrawShadow>` | A CSS box shadow of the box `Rect`: the shape moved by `Offset`, grown by `Spread` and blurred (standard deviation half of `Blur`). An outer shadow is not drawn under the box; an inset one is drawn only inside it.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Offset`. The offset (Point).<br>`Blur`. The blur (float).<br>`Spread`. The spread (float).<br>`Color`. The color (Color).<br>`Inset`. The inset (bool). |
| `public DrawShadow(Rect Rect, CornerRadius Radius, Point Offset, float Blur, float Spread, Color Color, bool Inset)` | A CSS box shadow of the box `Rect`: the shape moved by `Offset`, grown by `Spread` and blurred (standard deviation half of `Blur`). An outer shadow is not drawn under the box; an inset one is drawn only inside it.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Offset`. The offset (Point).<br>`Blur`. The blur (float).<br>`Spread`. The spread (float).<br>`Color`. The color (Color).<br>`Inset`. The inset (bool). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawShadow? left, DrawShadow? right)` |  |
| `public static bool operator ==(DrawShadow? left, DrawShadow? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(DrawShadow? other)` |  |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius, out Point Offset, out float Blur, out float Spread, out Color Color, out bool Inset)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Rect { get; init; }` | The rect (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |
| `public Point Offset { get; init; }` | The offset (Point). |
| `public float Blur { get; init; }` | The blur (float). |
| `public float Spread { get; init; }` | The spread (float). |
| `public Color Color { get; init; }` | The color (Color). |
| `public bool Inset { get; init; }` | The inset (bool). |

### `DrawText`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record DrawText : DrawCommand, IEquatable<DrawText>` | Draws a laid-out text with its top-left corner at `Origin`; each line is aligned within `Width`, with its `Decoration` if any.<br>`Layout`. The layout (TextLayout).<br>`Origin`. The origin (Point).<br>`Color`. The color (Color).<br>`Alignment`. The alignment (TextAlignment).<br>`Width`. The width of the value in its coordinate system.<br>`Decoration`. The decoration (TextDecoration?). |
| `public DrawText(TextLayout Layout, Point Origin, Color Color, TextAlignment Alignment = TextAlignment.Start, float Width = 0, TextDecoration? Decoration = null)` | Draws a laid-out text with its top-left corner at `Origin`; each line is aligned within `Width`, with its `Decoration` if any.<br>`Layout`. The layout (TextLayout).<br>`Origin`. The origin (Point).<br>`Color`. The color (Color).<br>`Alignment`. The alignment (TextAlignment).<br>`Width`. The width of the value in its coordinate system.<br>`Decoration`. The decoration (TextDecoration?). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(DrawText? left, DrawText? right)` |  |
| `public static bool operator ==(DrawText? left, DrawText? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(DrawText? other)` |  |
| `public void Deconstruct(out TextLayout Layout, out Point Origin, out Color Color, out TextAlignment Alignment, out float Width, out TextDecoration? Decoration)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public TextLayout Layout { get; init; }` | The layout (TextLayout). |
| `public Point Origin { get; init; }` | The origin (Point). |
| `public Color Color { get; init; }` | The color (Color). |
| `public TextAlignment Alignment { get; init; }` | The alignment (TextAlignment). |
| `public float Width { get; init; }` | The width of the value in its coordinate system. |
| `public TextDecoration? Decoration { get; init; }` | The decoration (TextDecoration?). |
| `public int TextStart { get; init; }` | First UTF-16 source cluster offset painted; defaults to zero. |
| `public int? TextLength { get; init; }` | Maximum source length painted; null includes the remaining text. |

### `FillGradient`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FillGradient : DrawCommand, IEquatable<FillGradient>` | Fills a rectangle, rounded like `Nazor.Graphics.FillRectangle`, with a gradient.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Brush`. The brush (GradientBrush). |
| `public FillGradient(Rect Rect, CornerRadius Radius, GradientBrush Brush)` | Fills a rectangle, rounded like `Nazor.Graphics.FillRectangle`, with a gradient.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Brush`. The brush (GradientBrush). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(FillGradient? left, FillGradient? right)` |  |
| `public static bool operator ==(FillGradient? left, FillGradient? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(FillGradient? other)` |  |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius, out GradientBrush Brush)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Rect { get; init; }` | The rect (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |
| `public GradientBrush Brush { get; init; }` | The brush (GradientBrush). |

### `FillImage`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Images.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FillImage : DrawCommand, IEquatable<FillImage>` | Fills `Area` (with its rounded corners) with an image placed in `Tile` and repeated on the axes that repeat: a background image.<br>`Area`. The area (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Image`. The rendered or decoded image to process.<br>`Tile`. The tile (Rect).<br>`RepeatX`. The repeat x (bool).<br>`RepeatY`. The repeat y (bool). |
| `public FillImage(Rect Area, CornerRadius Radius, DecodedImage Image, Rect Tile, bool RepeatX, bool RepeatY)` | Fills `Area` (with its rounded corners) with an image placed in `Tile` and repeated on the axes that repeat: a background image.<br>`Area`. The area (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Image`. The rendered or decoded image to process.<br>`Tile`. The tile (Rect).<br>`RepeatX`. The repeat x (bool).<br>`RepeatY`. The repeat y (bool). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(FillImage? left, FillImage? right)` |  |
| `public static bool operator ==(FillImage? left, FillImage? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(FillImage? other)` |  |
| `public void Deconstruct(out Rect Area, out CornerRadius Radius, out DecodedImage Image, out Rect Tile, out bool RepeatX, out bool RepeatY)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Area { get; init; }` | The area (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |
| `public DecodedImage Image { get; init; }` | The rendered or decoded image to process. |
| `public Rect Tile { get; init; }` | The tile (Rect). |
| `public bool RepeatX { get; init; }` | The repeat x (bool). |
| `public bool RepeatY { get; init; }` | The repeat y (bool). |

### `FillRectangle`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FillRectangle : DrawCommand, IEquatable<FillRectangle>` | Fills a rectangle, with rounded corners when `Radius` is not zero.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Color`. The color (Color). |
| `public FillRectangle(Rect Rect, CornerRadius Radius, Color Color)` | Fills a rectangle, with rounded corners when `Radius` is not zero.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly.<br>`Color`. The color (Color). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(FillRectangle? left, FillRectangle? right)` |  |
| `public static bool operator ==(FillRectangle? left, FillRectangle? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(FillRectangle? other)` |  |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius, out Color Color)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Rect { get; init; }` | The rect (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |
| `public Color Color { get; init; }` | The color (Color). |

### `GradientBrush`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record GradientBrush : IEquatable<GradientBrush>` | A gradient with its geometry resolved for one box.<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `protected GradientBrush(IReadOnlyList<GradientStop> Stops)` | A gradient with its geometry resolved for one box.<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(GradientBrush? left, GradientBrush? right)` |  |
| `public static bool operator ==(GradientBrush? left, GradientBrush? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(GradientBrush? other)` |  |
| `protected GradientBrush(GradientBrush original)` |  |
| `public void Deconstruct(out IReadOnlyList<GradientStop> Stops)` |  |
| `protected virtual Type EqualityContract { get; }` |  |
| `public IReadOnlyList<GradientStop> Stops { get; init; }` | The stops (IReadOnlyList<GradientStop>). |

### `GradientStop`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct GradientStop : IEquatable<GradientStop>` | A color at a position of a gradient, from 0 at its start to 1 at its end.<br>`Offset`. The offset (float).<br>`Color`. The color (Color). |
| `public GradientStop()` |  |
| `public GradientStop(float Offset, Color Color)` | A color at a position of a gradient, from 0 at its start to 1 at its end.<br>`Offset`. The offset (float).<br>`Color`. The color (Color). |
| `public override string ToString()` |  |
| `public static bool operator !=(GradientStop left, GradientStop right)` |  |
| `public static bool operator ==(GradientStop left, GradientStop right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(GradientStop other)` |  |
| `public void Deconstruct(out float Offset, out Color Color)` |  |
| `public float Offset { get; init; }` | The offset (float). |
| `public Color Color { get; init; }` | The color (Color). |

### `LineStyle`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LineStyle` | Specifies the pattern used to draw a border line. |
| `Solid = 0` | Draws an uninterrupted border line. |
| `Dashed = 1` | Dashes three times as long as the width, with gaps as long as the width. |
| `Dotted = 2` | Round dots as wide as the line, one width apart. |

### `LinearGradientBrush`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LinearGradientBrush : GradientBrush, IEquatable<LinearGradientBrush>` | Colors along the line from `Start` to `End`, constant beyond its ends.<br>`Start`. The gradient's start point in logical coordinates.<br>`End`. The gradient's end point in logical coordinates.<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `public LinearGradientBrush(Point Start, Point End, IReadOnlyList<GradientStop> Stops)` | Colors along the line from `Start` to `End`, constant beyond its ends.<br>`Start`. The gradient's start point in logical coordinates.<br>`End`. The gradient's end point in logical coordinates.<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(LinearGradientBrush? left, LinearGradientBrush? right)` |  |
| `public static bool operator ==(LinearGradientBrush? left, LinearGradientBrush? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(GradientBrush? other)` |  |
| `public bool Equals(LinearGradientBrush? other)` |  |
| `public void Deconstruct(out Point Start, out Point End, out IReadOnlyList<GradientStop> Stops)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Point Start { get; init; }` | The gradient's start point in logical coordinates. |
| `public Point End { get; init; }` | The gradient's end point in logical coordinates. |

### `Matrix`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Matrix : IEquatable<Matrix>` | A 2D affine transform with the layout of `System.Numerics.Matrix3x2` and of CSS `matrix(a, b, c, d, e, f)`: x' = M11·x + M21·y + OffsetX, y' = M12·x + M22·y + OffsetY.<br>`M11`. The m11 (float).<br>`M12`. The m12 (float).<br>`M21`. The m21 (float).<br>`M22`. The m22 (float).<br>`OffsetX`. The offset x (float).<br>`OffsetY`. The offset y (float). |
| `public Matrix()` |  |
| `public Matrix(float M11, float M12, float M21, float M22, float OffsetX, float OffsetY)` | A 2D affine transform with the layout of `System.Numerics.Matrix3x2` and of CSS `matrix(a, b, c, d, e, f)`: x' = M11·x + M21·y + OffsetX, y' = M12·x + M22·y + OffsetY.<br>`M11`. The m11 (float).<br>`M12`. The m12 (float).<br>`M21`. The m21 (float).<br>`M22`. The m22 (float).<br>`OffsetX`. The offset x (float).<br>`OffsetY`. The offset y (float). |
| `public static bool operator !=(Matrix left, Matrix right)` |  |
| `public static bool operator ==(Matrix left, Matrix right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Matrix other)` |  |
| `public void Deconstruct(out float M11, out float M12, out float M21, out float M22, out float OffsetX, out float OffsetY)` |  |
| `public float M11 { get; init; }` | The m11 (float). |
| `public float M12 { get; init; }` | The m12 (float). |
| `public float M21 { get; init; }` | The m21 (float). |
| `public float M22 { get; init; }` | The m22 (float). |
| `public float OffsetX { get; init; }` | The offset x (float). |
| `public float OffsetY { get; init; }` | The offset y (float). |

### `PixelRect`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct PixelRect : IEquatable<PixelRect>` | A rectangle of whole physical pixels of a surface.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public PixelRect()` |  |
| `public PixelRect(int X, int Y, int Width, int Height)` | A rectangle of whole physical pixels of a surface.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public override string ToString()` |  |
| `public static bool operator !=(PixelRect left, PixelRect right)` |  |
| `public static bool operator ==(PixelRect left, PixelRect right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(PixelRect other)` |  |
| `public void Deconstruct(out int X, out int Y, out int Width, out int Height)` |  |
| `public int X { get; init; }` | The horizontal component of the value. |
| `public int Y { get; init; }` | The vertical component of the value. |
| `public int Width { get; init; }` | The width of the value in its coordinate system. |
| `public int Height { get; init; }` | The height of the value in its coordinate system. |

### `Point`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Point : IEquatable<Point>` | A point in logical units.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public Point()` |  |
| `public Point(float X, float Y)` | A point in logical units.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public static bool operator !=(Point left, Point right)` |  |
| `public static bool operator ==(Point left, Point right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Point other)` |  |
| `public void Deconstruct(out float X, out float Y)` |  |
| `public float X { get; init; }` | The horizontal component of the value. |
| `public float Y { get; init; }` | The vertical component of the value. |

### `Pop`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record Pop : DrawCommand, IEquatable<Pop>` | Restores the drawing state saved by the most recent push command. |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(Pop? left, Pop? right)` |  |
| `public static bool operator ==(Pop? left, Pop? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(Pop? other)` |  |
| `public Pop()` |  |
| `protected override Type EqualityContract { get; }` |  |

### `PushClip`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PushClip : DrawCommand, IEquatable<PushClip>` | Intersects the current drawing clip with a rounded rectangle until the matching Pop command.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly. |
| `public PushClip(Rect Rect, CornerRadius Radius)` | Intersects the current drawing clip with a rounded rectangle until the matching Pop command.<br>`Rect`. The rect (Rect).<br>`Radius`. The corner radius to apply uniformly. |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PushClip? left, PushClip? right)` |  |
| `public static bool operator ==(PushClip? left, PushClip? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(PushClip? other)` |  |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Rect Rect { get; init; }` | The rect (Rect). |
| `public CornerRadius Radius { get; init; }` | The corner radius to apply uniformly. |

### `PushComposition`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/CompositionTimeline.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PushComposition : DrawCommand, IEquatable<PushComposition>` | Pushes a detached animated transform and opacity; Pop restores the previous state.<br>`Timeline`. The immutable, render-thread-safe animation snapshot. |
| `public PushComposition(CompositionTimeline Timeline)` | Pushes a detached animated transform and opacity; Pop restores the previous state.<br>`Timeline`. The immutable, render-thread-safe animation snapshot. |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PushComposition? left, PushComposition? right)` |  |
| `public static bool operator ==(PushComposition? left, PushComposition? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(PushComposition? other)` |  |
| `public void Deconstruct(out CompositionTimeline Timeline)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public CompositionTimeline Timeline { get; init; }` | The immutable, render-thread-safe animation snapshot. |

### `PushOpacity`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PushOpacity : DrawCommand, IEquatable<PushOpacity>` | Composites the following commands with an opacity, within `Bounds`.<br>`Opacity`. The alpha multiplier, clamped to the range from zero to one.<br>`Bounds`. The bounds (Rect). |
| `public PushOpacity(float Opacity, Rect Bounds)` | Composites the following commands with an opacity, within `Bounds`.<br>`Opacity`. The alpha multiplier, clamped to the range from zero to one.<br>`Bounds`. The bounds (Rect). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PushOpacity? left, PushOpacity? right)` |  |
| `public static bool operator ==(PushOpacity? left, PushOpacity? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(PushOpacity? other)` |  |
| `public void Deconstruct(out float Opacity, out Rect Bounds)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public float Opacity { get; init; }` | The alpha multiplier, clamped to the range from zero to one. |
| `public Rect Bounds { get; init; }` | The bounds (Rect). |

### `PushTransform`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PushTransform : DrawCommand, IEquatable<PushTransform>` | Transforms the following commands (CSS `transform`), in the current coordinates.<br>`Matrix`. The matrix (Matrix). |
| `public PushTransform(Matrix Matrix)` | Transforms the following commands (CSS `transform`), in the current coordinates.<br>`Matrix`. The matrix (Matrix). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PushTransform? left, PushTransform? right)` |  |
| `public static bool operator ==(PushTransform? left, PushTransform? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(PushTransform? other)` |  |
| `public void Deconstruct(out Matrix Matrix)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Matrix Matrix { get; init; }` | The matrix (Matrix). |

### `PushTranslate`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record PushTranslate : DrawCommand, IEquatable<PushTranslate>` | Translates subsequent drawing commands until the matching Pop command.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public PushTranslate(float X, float Y)` | Translates subsequent drawing commands until the matching Pop command.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PushTranslate? left, PushTranslate? right)` |  |
| `public static bool operator ==(PushTranslate? left, PushTranslate? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(DrawCommand? other)` |  |
| `public bool Equals(PushTranslate? other)` |  |
| `public void Deconstruct(out float X, out float Y)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public float X { get; init; }` | The horizontal component of the value. |
| `public float Y { get; init; }` | The vertical component of the value. |

### `RadialGradientBrush`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RadialGradientBrush : GradientBrush, IEquatable<RadialGradientBrush>` | Colors along the radii of an ellipse centered at `Center`, constant beyond it.<br>`Center`. The center (Point).<br>`RadiusX`. The radius x (float).<br>`RadiusY`. The radius y (float).<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `public RadialGradientBrush(Point Center, float RadiusX, float RadiusY, IReadOnlyList<GradientStop> Stops)` | Colors along the radii of an ellipse centered at `Center`, constant beyond it.<br>`Center`. The center (Point).<br>`RadiusX`. The radius x (float).<br>`RadiusY`. The radius y (float).<br>`Stops`. The stops (IReadOnlyList<GradientStop>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(RadialGradientBrush? left, RadialGradientBrush? right)` |  |
| `public static bool operator ==(RadialGradientBrush? left, RadialGradientBrush? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(GradientBrush? other)` |  |
| `public bool Equals(RadialGradientBrush? other)` |  |
| `public void Deconstruct(out Point Center, out float RadiusX, out float RadiusY, out IReadOnlyList<GradientStop> Stops)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Point Center { get; init; }` | The center (Point). |
| `public float RadiusX { get; init; }` | The radius x (float). |
| `public float RadiusY { get; init; }` | The radius y (float). |

### `Radius`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Radius : IEquatable<Radius>` | An elliptical corner: `X` along the width, `Y` along the height.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public Radius()` |  |
| `public Radius(float X, float Y)` | An elliptical corner: `X` along the width, `Y` along the height.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public static bool operator !=(Radius left, Radius right)` |  |
| `public static bool operator ==(Radius left, Radius right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Radius other)` |  |
| `public void Deconstruct(out float X, out float Y)` |  |
| `public float X { get; init; }` | The horizontal component of the value. |
| `public float Y { get; init; }` | The vertical component of the value. |

### `Rect`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Rect : IEquatable<Rect>` | An axis-aligned rectangle in logical units.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public Rect()` |  |
| `public Rect(float X, float Y, float Width, float Height)` | An axis-aligned rectangle in logical units.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public static bool operator !=(Rect left, Rect right)` |  |
| `public static bool operator ==(Rect left, Rect right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Rect other)` |  |
| `public void Deconstruct(out float X, out float Y, out float Width, out float Height)` |  |
| `public float X { get; init; }` | The horizontal component of the value. |
| `public float Y { get; init; }` | The vertical component of the value. |
| `public float Width { get; init; }` | The width of the value in its coordinate system. |
| `public float Height { get; init; }` | The height of the value in its coordinate system. |

### `RenderedImage`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class RenderedImage` | Pixels in BGRA order with premultiplied alpha, rows top-down.<br>`width`. The width of the target pixel buffer.<br>`height`. The height of the target pixel buffer.<br>`pixels`. The address of the top-down BGRA premultiplied pixel buffer. |
| `public RenderedImage(int width, int height, byte[] pixels)` | Pixels in BGRA order with premultiplied alpha, rows top-down.<br>`width`. The width of the target pixel buffer.<br>`height`. The height of the target pixel buffer.<br>`pixels`. The address of the top-down BGRA premultiplied pixel buffer. |

### `RenderingInfo`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/RenderingMode.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RenderingInfo : IEquatable<RenderingInfo>` | Describes the requested renderer and the renderer actually selected for a window.<br>`Requested`. The startup preference.<br>`Actual`. The selected GPU or CPU mode.<br>`Backend`. The graphics API or software implementation.<br>`Device`. The device reported by the graphics driver, when available.<br>`FallbackReason`. The reason automatic GPU initialization failed, when applicable. |
| `public RenderingInfo(RenderingMode Requested, RenderingMode Actual, string Backend, string? Device = null, string? FallbackReason = null)` | Describes the requested renderer and the renderer actually selected for a window.<br>`Requested`. The startup preference.<br>`Actual`. The selected GPU or CPU mode.<br>`Backend`. The graphics API or software implementation.<br>`Device`. The device reported by the graphics driver, when available.<br>`FallbackReason`. The reason automatic GPU initialization failed, when applicable. |
| `public override string ToString()` |  |
| `public static bool operator !=(RenderingInfo? left, RenderingInfo? right)` |  |
| `public static bool operator ==(RenderingInfo? left, RenderingInfo? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(RenderingInfo? other)` |  |
| `public void Deconstruct(out RenderingMode Requested, out RenderingMode Actual, out string Backend, out string? Device, out string? FallbackReason)` |  |
| `public RenderingMode Requested { get; init; }` | The startup preference. |
| `public RenderingMode Actual { get; init; }` | The selected GPU or CPU mode. |
| `public string Backend { get; init; }` | The graphics API or software implementation. |
| `public string? Device { get; init; }` | The device reported by the graphics driver, when available. |
| `public string? FallbackReason { get; init; }` | The reason automatic GPU initialization failed, when applicable. |

### `RenderingMode`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/RenderingMode.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RenderingMode` | Selects hardware rendering, software rendering, or hardware with an initialization fallback. |
| `Auto = 0` | Uses a hardware GPU when available and otherwise uses the CPU. |
| `Gpu = 1` | Requires a hardware GPU; initialization failure is an error. |
| `Cpu = 2` | Uses the software rasterizer without creating a GPU context. |

### `Size`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Size : IEquatable<Size>` | A size in logical units.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public Size()` |  |
| `public Size(float Width, float Height)` | A size in logical units.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public static bool operator !=(Size left, Size right)` |  |
| `public static bool operator ==(Size left, Size right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Size other)` |  |
| `public void Deconstruct(out float Width, out float Height)` |  |
| `public float Width { get; init; }` | The width of the value in its coordinate system. |
| `public float Height { get; init; }` | The height of the value in its coordinate system. |

### `TextAlignment`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextAlignment` | Specifies text placement along the inline axis. |
| `Start = 0` | Aligns text to the start of the available inline space. |
| `Center = 1` | Centers text in the available inline space. |
| `End = 2` | Aligns text to the end of the available inline space. |

### `TextDecoration`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TextDecoration : IEquatable<TextDecoration>` | Lines drawn with a text (CSS text-decoration): under, over or through each line of text, from its start to the end of its content. A `Thickness` of 0 takes the one of the font.<br>`Lines`. The lines (DecorationLines).<br>`Color`. The color (Color).<br>`Style`. The style (DecorationStyle).<br>`Thickness`. The logical inset distances on the four edges. |
| `public TextDecoration(DecorationLines Lines, Color Color, DecorationStyle Style = DecorationStyle.Solid, float Thickness = 0)` | Lines drawn with a text (CSS text-decoration): under, over or through each line of text, from its start to the end of its content. A `Thickness` of 0 takes the one of the font.<br>`Lines`. The lines (DecorationLines).<br>`Color`. The color (Color).<br>`Style`. The style (DecorationStyle).<br>`Thickness`. The logical inset distances on the four edges. |
| `public static bool operator !=(TextDecoration? left, TextDecoration? right)` |  |
| `public static bool operator ==(TextDecoration? left, TextDecoration? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TextDecoration? other)` |  |
| `public void Deconstruct(out DecorationLines Lines, out Color Color, out DecorationStyle Style, out float Thickness)` |  |
| `public DecorationLines Lines { get; init; }` | The lines (DecorationLines). |
| `public Color Color { get; init; }` | The color (Color). |
| `public DecorationStyle Style { get; init; }` | The style (DecorationStyle). |
| `public float Thickness { get; init; }` | The logical inset distances on the four edges. |

### `Thickness`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/Primitives.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Thickness : IEquatable<Thickness>` | Distances from the four edges of a box.<br>`Left`. The value associated with the left side or operand.<br>`Top`. The value associated with the top edge.<br>`Right`. The value associated with the right side or operand.<br>`Bottom`. The value associated with the bottom edge. |
| `public Thickness()` |  |
| `public Thickness(float Left, float Top, float Right, float Bottom)` | Distances from the four edges of a box.<br>`Left`. The value associated with the left side or operand.<br>`Top`. The value associated with the top edge.<br>`Right`. The value associated with the right side or operand.<br>`Bottom`. The value associated with the bottom edge. |
| `public static bool operator !=(Thickness left, Thickness right)` |  |
| `public static bool operator ==(Thickness left, Thickness right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Thickness other)` |  |
| `public void Deconstruct(out float Left, out float Top, out float Right, out float Bottom)` |  |
| `public float Left { get; init; }` | The value associated with the left side or operand. |
| `public float Top { get; init; }` | The value associated with the top edge. |
| `public float Right { get; init; }` | The value associated with the right side or operand. |
| `public float Bottom { get; init; }` | The value associated with the bottom edge. |

### `BreakWords`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BreakWords` | Where a line may break inside a word that does not fit (CSS overflow-wrap). |
| `Never = 0` | Only after spaces: a long word overflows. |
| `WhenOverflowing = 1` | Between graphemes of a word that does not fit on a line of its own; min-content stays the word. |
| `Anywhere = 2` | As `Nazor.Graphics.Text.BreakWords.WhenOverflowing`, and min-content is the widest grapheme. |

### `FontFace`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

| Declaration | Contract |
| --- | --- |
| `protected FontFace()` |  |
| `public virtual float? GetGlyphAdvance(int codepoint, float size)` | Unshaped horizontal glyph advance in logical pixels, or null if unavailable. Valid Unicode scalar and finite nonnegative size required. |
| `public virtual float? GetXHeight(float size)` | Used x-height in logical pixels, or null if unavailable. Finite nonnegative size required. Skia reads OpenType metrics, then the x glyph extent when needed. |

### `FontMetrics`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct FontMetrics : IEquatable<FontMetrics>` | Font metrics in design units (ascender and descender both positive).<br>`UnitsPerEm`. The units per em (int).<br>`Ascender`. The ascender (int).<br>`Descender`. The descender (int).<br>`LineGap`. The line gap (int). |
| `public FontMetrics()` |  |
| `public FontMetrics(int UnitsPerEm, int Ascender, int Descender, int LineGap)` | Font metrics in design units (ascender and descender both positive).<br>`UnitsPerEm`. The units per em (int).<br>`Ascender`. The ascender (int).<br>`Descender`. The descender (int).<br>`LineGap`. The line gap (int). |
| `public override string ToString()` |  |
| `public static bool operator !=(FontMetrics left, FontMetrics right)` |  |
| `public static bool operator ==(FontMetrics left, FontMetrics right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(FontMetrics other)` |  |
| `public void Deconstruct(out int UnitsPerEm, out int Ascender, out int Descender, out int LineGap)` |  |
| `public int UnitsPerEm { get; init; }` | The units per em (int). |
| `public int Ascender { get; init; }` | The ascender (int). |
| `public int Descender { get; init; }` | The descender (int). |
| `public int LineGap { get; init; }` | The line gap (int). |

### `FontOptions`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/FontOptions.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record FontOptions : IEquatable<FontOptions>` | Private font files and fallback policy, independent of the renderer. Configure before startup. |
| `public override string ToString()` |  |
| `public static bool operator !=(FontOptions? left, FontOptions? right)` |  |
| `public static bool operator ==(FontOptions? left, FontOptions? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(FontOptions? other)` |  |
| `public FontOptions()` |  |

### `LineHeightKind`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LineHeightKind` | Specifies the available line height kind values. |
| `Normal = 0` | Uses the font's normal line metrics. |
| `Multiplier = 1` | Multiplies the font size by a unitless line-height factor. |
| `Fixed = 2` | Uses an explicitly resolved line height. |

### `TextStyle`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/Fonts.cs)

| Declaration | Contract |
| --- | --- |
| `public FontOpticalSizing OpticalSizing { get; init; }` | Native by default; Auto uses CSS logical pixels, None disables automatic optical sizing. Undefined enum values throw. |
| `public IReadOnlyDictionary<string, double> FontVariations { get; init; }` | Copied, ordinal-sorted, read-only axis coordinates. Four printable ASCII characters per case-sensitive tag; finite double coordinates. Explicit axes override optical policy. Equality/hash include contents. |
| `public sealed record TextStyle : IEquatable<TextStyle>` | The text properties that affect shaping and line metrics (color is a paint property).<br>`FontFamilies`. The font families (IReadOnlyList<string>).<br>`FontSize`. The font size (float).<br>`FontWeight`. The font weight (int).<br>`Italic`. Whether an italic face is preferred.<br>`LineHeightKind`. The line height kind (LineHeightKind).<br>`LineHeight`. The line height (float).<br>`LetterSpacing`. The letter spacing (float).<br>`BreakWords`. The break words (BreakWords). |
| `public TextStyle(IReadOnlyList<string> FontFamilies, float FontSize, int FontWeight = 400, bool Italic = false, LineHeightKind LineHeightKind = LineHeightKind.Normal, float LineHeight = 0, float LetterSpacing = 0, BreakWords BreakWords = BreakWords.Never)` | The text properties that affect shaping and line metrics (color is a paint property).<br>`FontFamilies`. The font families (IReadOnlyList<string>).<br>`FontSize`. The font size (float).<br>`FontWeight`. The font weight (int).<br>`Italic`. Whether an italic face is preferred.<br>`LineHeightKind`. The line height kind (LineHeightKind).<br>`LineHeight`. The line height (float).<br>`LetterSpacing`. The letter spacing (float).<br>`BreakWords`. The break words (BreakWords). |
| `public override string ToString()` |  |
| `public static bool operator !=(TextStyle? left, TextStyle? right)` |  |
| `public static bool operator ==(TextStyle? left, TextStyle? right)` |  |
| `public override bool Equals(object? obj)` |  |
| `public void Deconstruct(out IReadOnlyList<string> FontFamilies, out float FontSize, out int FontWeight, out bool Italic, out LineHeightKind LineHeightKind, out float LineHeight, out float LetterSpacing, out BreakWords BreakWords)` |  |
| `public IReadOnlyList<string> FontFamilies { get; init; }` | The font families (IReadOnlyList<string>). |
| `public float FontSize { get; init; }` | The font size (float). |
| `public int FontWeight { get; init; }` | The font weight (int). |
| `public bool Italic { get; init; }` | Whether an italic face is preferred. |
| `public LineHeightKind LineHeightKind { get; init; }` | The line height kind (LineHeightKind). |
| `public float LineHeight { get; init; }` | The line height (float). |
| `public float LetterSpacing { get; init; }` | The letter spacing (float). |
| `public BreakWords BreakWords { get; init; }` | The break words (BreakWords). |


<a id="type-fontopticalsizing"></a>

## `FontOpticalSizing`

Namespace `Nazor.Graphics.Text`. [Source](../../Sources/Core/Nazor.Graphics/Text/FontOpticalSizing.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FontOpticalSizing` | Automatic variable-font optical-size policy. |
| `Native = 0` | Uses typographic points; default for directly constructed graphics styles. |
| `Auto = 1` | Uses the logical font size directly, following CSS. |
| `None = 2` | Leaves the selected font's default optical coordinate unless explicitly overridden. |

<a id="type-pushbackdrop"></a>

### `PushBackdrop`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

Begins an isolated layer initialized with the filtered existing canvas inside a rounded border box. Later content may overflow the box; Pop composites the whole group with Opacity. This samples the current surface, not pixels outside its native window.

| Declaration | Contract |
| --- | --- |
| `public IReadOnlyList<ImageFilter>? Filters { get; init; }` | Optional copied chain; null uses the Blur/Saturation pair, empty uses identity backdrop sampling. |
| `public sealed record PushBackdrop : DrawCommand, IEquatable<PushBackdrop>` | Begins an isolated layer initialized with the filtered existing canvas inside a rounded border box. Later content may overflow the box; Pop composites the whole group with Opacity. This samples the current surface, not pixels outside its native window. |
| `public PushBackdrop(Rect Rect, CornerRadius Radius, Rect Bounds, float Blur, float Saturation = 1, bool SaturationFirst = false, float Opacity = 1)` | Begins an isolated layer initialized with the filtered existing canvas inside a rounded border box. Later content may overflow the box; Pop composites the whole group with Opacity. This samples the current surface, not pixels outside its native window. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(PushBackdrop? left, PushBackdrop? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(PushBackdrop? left, PushBackdrop? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(DrawCommand? other)` | Standard .NET value or component semantics. |
| `public bool Equals(PushBackdrop? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out Rect Rect, out CornerRadius Radius, out Rect Bounds, out float Blur, out float Saturation, out bool SaturationFirst, out float Opacity)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public Rect Rect { get; init; }` | The border box receiving the filtered backdrop. |
| `public CornerRadius Radius { get; init; }` | The border-box corner radii. |
| `public Rect Bounds { get; init; }` | The layer extent, including foreground overflow. |
| `public float Blur { get; init; }` | The finite nonnegative Gaussian standard deviation in logical units; the renderer bounds the sampling radius to256. |
| `public float Saturation { get; init; }` | The finite nonnegative saturation multiplier. |
| `public bool SaturationFirst { get; init; }` | Whether saturation precedes blur. |
| `public float Opacity { get; init; }` | The group alpha multiplier in the range zero to one. |

<a id="type-conicgradientbrush"></a>

### `ConicGradientBrush`

Namespace `Nazor.Graphics`. [Source](../../Sources/Core/Nazor.Graphics/DisplayList.cs)

A clockwise angular brush with zero pointing up, evaluated over one turn before optional repetition.

| Declaration | Contract |
| --- | --- |
| `public sealed record ConicGradientBrush : GradientBrush, IEquatable<ConicGradientBrush>` | A clockwise angular brush with zero pointing up, evaluated over one turn before optional repetition. |
| `public ConicGradientBrush(Point Center, float StartAngle, float EndAngle, IReadOnlyList<GradientStop> Stops)` | A clockwise angular brush with zero pointing up, evaluated over one turn before optional repetition. |
| `public override string ToString()` | Describes the angular interval, rotation and stop colors. A diagnostic brush description. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(ConicGradientBrush? left, ConicGradientBrush? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(ConicGradientBrush? left, ConicGradientBrush? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(GradientBrush? other)` | Standard .NET value or component semantics. |
| `public bool Equals(ConicGradientBrush? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out Point Center, out float StartAngle, out float EndAngle, out IReadOnlyList<GradientStop> Stops)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public Point Center { get; init; }` | The center in logical coordinates. |
| `public float StartAngle { get; init; }` | The first stop's angle in degrees, relative to Rotation. |
| `public float EndAngle { get; init; }` | The last stop's angle, greater than StartAngle. |
| `public float Rotation { get; init; }` | Clockwise rotation in degrees; zero starts at the top. |

### `GradientBrush`

Namespace `Nazor.Graphics`.

| Declaration | Contract |
| --- | --- |
| `public bool Repeating { get; init; }` | Repeats the resolved interval instead of extending end colors; false by default. |

### `RadialGradientBrush`

Namespace `Nazor.Graphics`.

| Declaration | Contract |
| --- | --- |
| `public float StartRadius { get; init; }` | Inner horizontal radius of a repeating radial interval; zero by default. |

### `ImageFilter`

Namespace `Nazor.Graphics`.

A backend-neutral image operation applied in an ordered filter group.

| Declaration | Contract |
| --- | --- |
| `public abstract record ImageFilter : IEquatable<ImageFilter>` | A backend-neutral image operation applied in an ordered filter group. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected virtual bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(ImageFilter? left, ImageFilter? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(ImageFilter? left, ImageFilter? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public virtual bool Equals(ImageFilter? other)` | Standard .NET value or component semantics. |
| `protected ImageFilter(ImageFilter original)` | Creates a new instance. |
| `protected ImageFilter()` | Creates a new instance. |
| `protected virtual Type EqualityContract { get; }` | Standard .NET value or component semantics. |

### `GaussianBlurFilter`

Namespace `Nazor.Graphics`.

A transparent-edge Gaussian blur for element content, or mirrored-edge blur for captured backdrops.

| Declaration | Contract |
| --- | --- |
| `public sealed record GaussianBlurFilter : ImageFilter, IEquatable<GaussianBlurFilter>` | A transparent-edge Gaussian blur for element content, or mirrored-edge blur for captured backdrops. |
| `public GaussianBlurFilter(float Sigma)` | A transparent-edge Gaussian blur for element content, or mirrored-edge blur for captured backdrops. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(GaussianBlurFilter? left, GaussianBlurFilter? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(GaussianBlurFilter? left, GaussianBlurFilter? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(ImageFilter? other)` | Standard .NET value or component semantics. |
| `public bool Equals(GaussianBlurFilter? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out float Sigma)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public float Sigma { get; init; }` | Finite nonnegative standard deviation in logical pixels; Skia bounds it to256. |

### `ColorMatrixFilter`

Namespace `Nazor.Graphics`.

A copied4×5 row-major RGBA matrix. Components and translation columns use normalized0..1 units; input/output remain straight-alpha colors.

| Declaration | Contract |
| --- | --- |
| `public sealed record ColorMatrixFilter : ImageFilter, IEquatable<ColorMatrixFilter>` | A copied4×5 row-major RGBA matrix. Components and translation columns use normalized0..1 units; input/output remain straight-alpha colors. |
| `public ColorMatrixFilter(IEnumerable<float> values)` | Copies a finite20-coefficient matrix. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(ColorMatrixFilter? left, ColorMatrixFilter? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(ColorMatrixFilter? left, ColorMatrixFilter? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(ImageFilter? other)` | Standard .NET value or component semantics. |
| `public bool Equals(ColorMatrixFilter? other)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public IReadOnlyList<float> Values { get; }` | The immutable copied matrix coefficients. |

### `PushFilter`

Namespace `Nazor.Graphics`.

Filters all following foreground drawing through an isolated group until Pop, then composites it once with Opacity.

| Declaration | Contract |
| --- | --- |
| `public sealed record PushFilter : DrawCommand, IEquatable<PushFilter>` | Filters all following foreground drawing through an isolated group until Pop, then composites it once with Opacity. |
| `public PushFilter(IEnumerable<ImageFilter> filters, Rect bounds, float opacity = 1)` | Copies an ordered operation list. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(PushFilter? left, PushFilter? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(PushFilter? left, PushFilter? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(DrawCommand? other)` | Standard .NET value or component semantics. |
| `public bool Equals(PushFilter? other)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public IReadOnlyList<ImageFilter> Filters { get; }` | The immutable copied operation list. |
| `public Rect Bounds { get; }` | Layer extent including filtered overflow. |
| `public float Opacity { get; }` | Group opacity from zero to one. |

### `BlendMode`

Namespace `Nazor.Graphics`.

Color blending of an isolated source group with its destination.

| Declaration | Contract |
| --- | --- |
| `public enum BlendMode` | Color blending of an isolated source group with its destination. |
| `Normal = 0` | Uses source-over without color blending. |
| `Multiply = 1` | Multiplies source and backdrop channels. |
| `Screen = 2` | Multiplies complementary channels. |
| `Overlay = 3` | Uses multiply or screen according to backdrop luminance. |
| `Darken = 4` | Chooses the darker channel. |
| `Lighten = 5` | Chooses the lighter channel. |
| `ColorDodge = 6` | Brightens the backdrop according to the source. |
| `ColorBurn = 7` | Darkens the backdrop according to the source. |
| `HardLight = 8` | Uses multiply or screen according to source luminance. |
| `SoftLight = 9` | Applies a soft contrast adjustment. |
| `Difference = 10` | Uses the absolute channel difference. |
| `Exclusion = 11` | Uses a low-contrast channel difference. |
| `Hue = 12` | Uses source hue with backdrop saturation and luminosity. |
| `Saturation = 13` | Uses source saturation with backdrop hue and luminosity. |
| `Color = 14` | Uses source hue and saturation with backdrop luminosity. |
| `Luminosity = 15` | Uses source luminosity with backdrop hue and saturation. |

### `PushBlend`

Namespace `Nazor.Graphics`.

Begins an isolated transparent source group until Pop, then blends its complete contents with the destination.

| Declaration | Contract |
| --- | --- |
| `public sealed record PushBlend : DrawCommand, IEquatable<PushBlend>` | Begins an isolated transparent source group until Pop, then blends its complete contents with the destination. |
| `public PushBlend(BlendMode Mode, Rect Bounds)` | Begins an isolated transparent source group until Pop, then blends its complete contents with the destination. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(PushBlend? left, PushBlend? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(PushBlend? left, PushBlend? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(DrawCommand? other)` | Standard .NET value or component semantics. |
| `public bool Equals(PushBlend? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out BlendMode Mode, out Rect Bounds)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public BlendMode Mode { get; init; }` | The color blend formula; Normal explicitly isolates without color blending. |
| `public Rect Bounds { get; init; }` | The group extent including overflowing content. |

### `FontSynthesis`

Namespace `Nazor.Graphics.Text`.

Permitted synthetic font styles when a real face or variation axis is unavailable. None preserves unsynthesized rendering.

| Declaration | Contract |
| --- | --- |
| `public enum FontSynthesis` | Permitted synthetic font styles when a real face or variation axis is unavailable. None preserves unsynthesized rendering. |
| `None = 0` | Uses only real font faces and variation axes. |
| `Weight = 1` | Permits simulated bold for a requested weight of at least 600 when the selected face is lighter. |
| `Style = 2` | Permits a 12-degree synthetic oblique when italic is requested but unavailable. |

### `ShapedRun`

Namespace `Nazor.Graphics.Text`.

Glyphs of a text range shaped with one face, in logical units for the given font size.

| Declaration | Contract |
| --- | --- |
| `public sealed class ShapedRun` | Glyphs of a text range shaped with one face, in logical units for the given font size. |
| `public ShapedRun(FontFace face, float size, int start, int length, ushort[] glyphs, float[] advances, float[] offsetsX, float[] offsetsY, int[] clusters)` | Initializes a new instance of ShapedRun. |
| `public ShapedRun Slice(int start, int end)` | The glyphs whose clusters fall in [start, end), without shaping again. The resulting ShapedRun value. |
| `public FontFace Face { get; }` | Gets the font face used by this glyph run. |
| `public FontSynthesis Synthesis { get; init; }` | Actually applied outline synthesis, independent of the resolved real face. |
| `public float InkOutset { get; }` | Conservative additional ink extent for synthetic styling; advances and line metrics are unchanged. |
| `public float Size { get; }` | Gets the logical font size used to shape the run. |
| `public int Start { get; }` | UTF-16 range of the source text covered by the run. |
| `public int Length { get; }` | Gets the length of the source text segment in UTF-16 code units. |
| `public ushort[] Glyphs { get; }` | Gets the font-specific glyph identifiers in drawing order. |
| `public float[] Advances { get; }` | Gets the horizontal advance of each glyph in logical units. |
| `public float[] OffsetsX { get; }` | Gets the horizontal positioning adjustment for each glyph. |
| `public float[] OffsetsY { get; }` | Gets the vertical positioning adjustment for each glyph. |
| `public int[] Clusters { get; }` | For each glyph, the UTF-16 index (in the source text) where its cluster starts. |
| `public float Width { get; }` | Gets the total horizontal advance of the run in logical units. |

### `ITextShaper`

Namespace `Nazor.Graphics.Text`.

Shapes a UTF-16 range with a face; clusters are indices of the source string.

| Declaration | Contract |
| --- | --- |
| `public interface ITextShaper` | Shapes a UTF-16 range with a face; clusters are indices of the source string. |
| `ShapedRun Shape(string text, int start, int length, FontFace face, float size)` | Shapes a UTF-16 text segment into positioned glyphs using the specified font face and size. The positioned glyph run for the source text segment. |
| `ShapedRun Shape(string text, int start, int length, FontFace face, TextStyle style)` | Shapes with explicit features and permitted synthesis. Existing shapers may delegate to the size-only path. The positioned glyph run. |

### `TextStyle`

Namespace `Nazor.Graphics.Text`.

The text properties that affect shaping and line metrics (color is a paint property).

| Declaration | Contract |
| --- | --- |
| `public sealed record TextStyle : IEquatable<TextStyle>` | The text properties that affect shaping and line metrics (color is a paint property). |
| `public TextStyle(IReadOnlyList<string> FontFamilies, float FontSize, int FontWeight = 400, bool Italic = false, LineHeightKind LineHeightKind = LineHeightKind.Normal, float LineHeight = 0, float LetterSpacing = 0, BreakWords BreakWords = BreakWords.Never)` | The text properties that affect shaping and line metrics (color is a paint property). |
| `public bool Equals(TextStyle? other)` | Determines whether the supplied value is equal to this instance. true if the supplied value satisfies the comparison; otherwise, false. |
| `public override int GetHashCode()` | Returns a hash code consistent with equality for this value. The hash code for this value. |
| `public override string ToString()` | Standard .NET value or component semantics. |
| `public static bool operator !=(TextStyle? left, TextStyle? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(TextStyle? left, TextStyle? right)` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out IReadOnlyList<string> FontFamilies, out float FontSize, out int FontWeight, out bool Italic, out LineHeightKind LineHeightKind, out float LineHeight, out float LetterSpacing, out BreakWords BreakWords)` | Standard .NET value or component semantics. |
| `public IReadOnlyList<string> FontFamilies { get; init; }` | The font families (IReadOnlyList&lt;string&gt;). |
| `public float FontSize { get; init; }` | The font size (float). |
| `public int FontWeight { get; init; }` | The font weight (int). |
| `public bool Italic { get; init; }` | Whether an italic face is preferred. |
| `public LineHeightKind LineHeightKind { get; init; }` | The line height kind (LineHeightKind). |
| `public float LineHeight { get; init; }` | The line height (float). |
| `public float LetterSpacing { get; init; }` | The letter spacing (float). |
| `public BreakWords BreakWords { get; init; }` | The break words (BreakWords). |
| `public FontSynthesis Synthesis { get; init; }` | Gets or initializes permitted synthetic styling. None is the default. |
| `public IReadOnlyDictionary<string, uint> FontFeatures { get; init; }` | Gets or initializes copied, sorted, case-sensitive OpenType feature selections. |
| `public FontOpticalSizing OpticalSizing { get; init; }` | Gets or initializes automatic optical sizing. Native is the default for directly constructed graphics styles. |
| `public IReadOnlyDictionary<string, double> FontVariations { get; init; }` | Gets or initializes explicit, case-sensitive OpenType axes. Initialization copies and sorts the input; explicit axes override automatic sizing. |

### `TextLayout`

Namespace `Nazor.Graphics.Text`.

Shaped text broken into lines; the same instance serves measuring, drawing and hit-testing, so what is measured is what is drawn. Lines break at '\n' and, when a maximum width is given and wrapping is allowed, after spaces. Only left-to-right text layout is supported.

| Declaration | Contract |
| --- | --- |
| `public sealed class TextLayout` | Shaped text broken into lines; the same instance serves measuring, drawing and hit-testing, so what is measured is what is drawn. Lines break at '\n' and, when a maximum width is given and wrapping is allowed, after spaces. Only left-to-right text layout is supported. |
| `public TextLayout(string text, TextStyle style, float? maxWidth, IFontProvider fonts, ITextShaper shaper, bool wrap = true)` | Initializes a new instance of TextLayout. |
| `public TextLayout(ShapedText shaped, float? maxWidth, bool wrap = true)` | A layout of text already shaped: only the lines are broken for this width. |
| `public float GetCaretX(int index)` | The x offset of the caret before `index` on its line. The resulting float value. |
| `public Rect GetCaretRect(int index)` | The caret rectangle (zero width) at `index`. The resulting Rect value. |
| `public int HitTest(Point point)` | The caret index closest to `point`, at grapheme boundaries. The resulting int value. |
| `public IEnumerable<Rect> GetSelectionRects(int start, int end)` | One rectangle per line covering [start, end). The resulting IEnumerable&lt;Rect&gt; value. |
| `public int NextCaretIndex(int index)` | The next caret position after `index` (next grapheme boundary). The resulting int value. |
| `public int PreviousCaretIndex(int index)` | The previous caret position before `index`. The resulting int value. |
| `public TextLine LineAt(int index)` | The line of the caret before `index`: a line break or a wrap starts the next line. The resulting TextLine value. |
| `public ShapedText Shaped { get; }` | The shaped text, shared by the layouts of other widths. |
| `public string Text { get; }` | Gets the text represented by this object. |
| `public TextStyle Style { get; }` | Gets the text style used for shaping, line breaking, and alignment. |
| `public float? MaxWidth { get; }` | Gets the optional logical width constraint used to wrap the text. |
| `public int FontVersion { get; }` | Gets the font-provider version used to shape this text. |
| `public FontFace Primary { get; }` | Gets the primary resolved font face for the text. |
| `public IReadOnlyList<ShapedRun> Runs { get; }` | The shaped runs of the whole text, before line breaking. |
| `public IReadOnlyList<TextLine> Lines { get; }` | Gets the ordered laid-out text lines. |
| `public float Width { get; }` | Width of the widest line, trailing spaces excluded. |
| `public float Height { get; }` | Gets the total logical height of the laid-out text. |
| `public float MaxContentWidth { get; }` | Width without wrapping: the widest paragraph. |
| `public float MinContentWidth { get; }` | Width of the widest unbreakable word. |
| `public float Baseline { get; }` | Gets the baseline offset of the first text line. |
| `public Size Size { get; }` | Gets the logical bounds occupied by the text layout. |
| `public float InkOutset { get; }` | Conservative extra ink extent for the synthesized runs, without changing layout advances. |
