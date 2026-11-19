using System.Text;
using Nazor.Graphics.Text;

namespace Nazor.Graphics;

/// <summary>A drawing operation in logical units. Push commands are balanced by <see cref="Pop" />.</summary>
public abstract record DrawCommand;

/// <summary>Fills a rectangle, with rounded corners when <paramref name="Radius" /> is not zero.</summary>
/// <param name="Rect">The rect (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
/// <param name="Color">The color (Color).</param>
public sealed record FillRectangle(Rect Rect, CornerRadius Radius, Color Color) : DrawCommand;

/// <summary>Specifies the pattern used to draw a border line.</summary>
public enum LineStyle : byte
{
    /// <summary>Draws an uninterrupted border line.</summary>
    Solid,

    /// <summary>Dashes three times as long as the width, with gaps as long as the width.</summary>
    Dashed,

    /// <summary>Round dots as wide as the line, one width apart.</summary>
    Dotted,
}

/// <summary>The color and line style of one side of a border.</summary>
/// <param name="Color">The color (Color).</param>
/// <param name="Style">The style (LineStyle).</param>
public readonly record struct BorderSide(Color Color, LineStyle Style = LineStyle.Solid)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Style == LineStyle.Solid ? Color.ToString() : $"{Style.ToString().ToLowerInvariant()} {Color}";
}

/// <summary>
/// Fills the area between the outer box and the box inset by the border widths (CSS borders): the inner corners lose
/// the widths of their sides. When the sides differ, each one is the trapezoid between the outer and the inner edge,
/// joined diagonally at the corners.
/// </summary>
/// <param name="Rect">The rect (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
/// <param name="Widths">The logical border thicknesses on the four edges.</param>
/// <param name="Top">The value associated with the top edge.</param>
/// <param name="Right">The value associated with the right side or operand.</param>
/// <param name="Bottom">The value associated with the bottom edge.</param>
/// <param name="Left">The value associated with the left side or operand.</param>
public sealed record DrawBorder(Rect Rect, CornerRadius Radius, Thickness Widths, BorderSide Top, BorderSide Right, BorderSide Bottom, BorderSide Left) : DrawCommand
{
    /// <summary>An optional gradient spanning the border ring instead of solid side colors.</summary>
    public GradientBrush? Brush { get; init; }

    /// <summary>Initializes a new instance of DrawBorder.</summary>
    /// <param name="rect">The rect (Rect).</param>
    /// <param name="radius">The corner radius to apply uniformly.</param>
    /// <param name="widths">The logical border thicknesses on the four edges.</param>
    /// <param name="color">The color (Color).</param>
    /// <param name="style">The style (LineStyle).</param>
    public DrawBorder(Rect rect, CornerRadius radius, Thickness widths, Color color, LineStyle style = LineStyle.Solid)
        : this(rect, radius, widths, new BorderSide(color, style), new BorderSide(color, style), new BorderSide(color, style), new BorderSide(color, style))
    {
    }

    /// <summary>The four sides have the same color and style.</summary>
    public bool IsUniform => Top == Right && Right == Bottom && Bottom == Left;

    /// <summary>The color of the top side, the color of every side when <see cref="IsUniform" />.</summary>
    public Color Color => Top.Color;
}

/// <summary>
/// A CSS box shadow of the box <paramref name="Rect" />: the shape moved by <paramref name="Offset" />, grown by
/// <paramref name="Spread" /> and blurred (standard deviation half of <paramref name="Blur" />). An outer shadow is
/// not drawn under the box; an inset one is drawn only inside it.
/// </summary>
/// <param name="Rect">The rect (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
/// <param name="Offset">The offset (Point).</param>
/// <param name="Blur">The blur (float).</param>
/// <param name="Spread">The spread (float).</param>
/// <param name="Color">The color (Color).</param>
/// <param name="Inset">The inset (bool).</param>
public sealed record DrawShadow(Rect Rect, CornerRadius Radius, Point Offset, float Blur, float Spread, Color Color, bool Inset) : DrawCommand;

/// <summary>A color at a position of a gradient, from 0 at its start to 1 at its end.</summary>
/// <param name="Offset">The offset (float).</param>
/// <param name="Color">The color (Color).</param>
public readonly record struct GradientStop(float Offset, Color Color);

/// <summary>A gradient with its geometry resolved for one box.</summary>
/// <param name="Stops">The stops (IReadOnlyList&lt;GradientStop&gt;).</param>
public abstract record GradientBrush(IReadOnlyList<GradientStop> Stops)
{
    /// <summary>Whether the resolved stop interval repeats instead of extending its end colors.</summary>
    public bool Repeating { get; init; }
    /// <summary>Formats the gradient stops for diagnostic output.</summary>
    /// <returns>The resulting string value.</returns>
    protected string DescribeStops() => string.Join(", ", Stops.Select(s => $"{s.Color} {s.Offset:0.###}"));
}

/// <summary>
/// Colors along the line from <paramref name="Start" /> to <paramref name="End" />, constant beyond its ends.
/// </summary>
/// <param name="Start">The gradient's start point in logical coordinates.</param>
/// <param name="End">The gradient's end point in logical coordinates.</param>
/// <param name="Stops">The stops (IReadOnlyList&lt;GradientStop&gt;).</param>
public sealed record LinearGradientBrush(Point Start, Point End, IReadOnlyList<GradientStop> Stops) : GradientBrush(Stops)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{(Repeating ? "repeat " : "")}linear {Start}->{End} [{DescribeStops()}]";
}

/// <summary>
/// Colors along the radii of an ellipse centered at <paramref name="Center" />, constant beyond it.
/// </summary>
/// <param name="Center">The center (Point).</param>
/// <param name="RadiusX">The radius x (float).</param>
/// <param name="RadiusY">The radius y (float).</param>
/// <param name="Stops">The stops (IReadOnlyList&lt;GradientStop&gt;).</param>
public sealed record RadialGradientBrush(Point Center, float RadiusX, float RadiusY, IReadOnlyList<GradientStop> Stops) : GradientBrush(Stops)
{
    /// <summary>The inner horizontal radius of a repeating interval; zero for an ordinary radial gradient.</summary>
    public float StartRadius { get; init; }
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{(Repeating ? "repeat " : "")}radial {Center} {RadiusX:0.##}x{RadiusY:0.##}" +
        (StartRadius == 0 ? "" : $" from {StartRadius:0.##}") + $" [{DescribeStops()}]";
}

/// <summary>A clockwise angular brush with zero pointing up, evaluated over one turn before optional repetition.</summary>
/// <param name="Center">The center in logical coordinates.</param>
/// <param name="StartAngle">The first stop's angle in degrees, relative to Rotation.</param>
/// <param name="EndAngle">The last stop's angle, greater than StartAngle.</param>
/// <param name="Stops">Normalized stops over the declared angular interval.</param>
public sealed record ConicGradientBrush(Point Center, float StartAngle, float EndAngle, IReadOnlyList<GradientStop> Stops) : GradientBrush(Stops)
{
    /// <summary>Clockwise rotation in degrees; zero starts at the top.</summary>
    public float Rotation { get; init; }

    /// <summary>Describes the angular interval, rotation and stop colors.</summary>
    /// <returns>A diagnostic brush description.</returns>
    public override string ToString() => $"{(Repeating ? "repeat " : "")}conic {Center} {StartAngle:0.##}->{EndAngle:0.##} rotate={Rotation:0.##} [{DescribeStops()}]";
}

/// <summary>Fills a rectangle, rounded like <see cref="FillRectangle" />, with a gradient.</summary>
/// <param name="Rect">The rect (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
/// <param name="Brush">The brush (GradientBrush).</param>
public sealed record FillGradient(Rect Rect, CornerRadius Radius, GradientBrush Brush) : DrawCommand;

/// <summary>Specifies text placement along the inline axis.</summary>
public enum TextAlignment : byte
{
    /// <summary>Aligns text to the start of the available inline space.</summary>
    Start,

    /// <summary>Centers text in the available inline space.</summary>
    Center,

    /// <summary>Aligns text to the end of the available inline space.</summary>
    End,
}

/// <summary>Specifies text-decoration positions; values can be combined.</summary>
[Flags]
public enum DecorationLines : byte
{
    /// <summary>Draws no text decoration.</summary>
    None = 0,

    /// <summary>Draws a line below the text.</summary>
    Underline = 1,

    /// <summary>Draws a line above the text.</summary>
    Overline = 2,

    /// <summary>Draws a line through the text.</summary>
    LineThrough = 4,
}

/// <summary>Specifies the stroke pattern used for text decorations.</summary>
public enum DecorationStyle : byte
{
    /// <summary>Draws a continuous decoration line.</summary>
    Solid,

    /// <summary>Draws two parallel decoration lines.</summary>
    Double,

    /// <summary>Draws the decoration as dots.</summary>
    Dotted,

    /// <summary>Draws the decoration as dashes.</summary>
    Dashed,

    /// <summary>Draws the decoration as a wavy line.</summary>
    Wavy,
}

/// <summary>
/// Lines drawn with a text (CSS text-decoration): under, over or through each line of text, from its start to the end
/// of its content. A <paramref name="Thickness" /> of 0 takes the one of the font.
/// </summary>
/// <param name="Lines">The lines (DecorationLines).</param>
/// <param name="Color">The color (Color).</param>
/// <param name="Style">The style (DecorationStyle).</param>
/// <param name="Thickness">The logical inset distances on the four edges.</param>
public sealed record TextDecoration(DecorationLines Lines, Color Color, DecorationStyle Style = DecorationStyle.Solid, float Thickness = 0)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Lines} {Style} {Color}" + (Thickness > 0 ? $" {Thickness:0.##}" : "");
}

/// <summary>
/// Draws a laid-out text with its top-left corner at <paramref name="Origin" />; each line is aligned within
/// <paramref name="Width" />, with its <paramref name="Decoration" /> if any.
/// </summary>
/// <param name="Layout">The layout (TextLayout).</param>
/// <param name="Origin">The origin (Point).</param>
/// <param name="Color">The color (Color).</param>
/// <param name="Alignment">The alignment (TextAlignment).</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Decoration">The decoration (TextDecoration?).</param>
public sealed record DrawText(TextLayout Layout, Point Origin, Color Color, TextAlignment Alignment = TextAlignment.Start, float Width = 0,
    TextDecoration? Decoration = null) : DrawCommand
{
    /// <summary>Gets the first UTF-16 source offset whose existing shaped glyphs are painted; zero by default.</summary>
    public int TextStart { get; init; }

    /// <summary>Gets the maximum UTF-16 source length painted, or null to include the remaining text. The layout, kerning and glyph positions remain unchanged.</summary>
    public int? TextLength { get; init; }

    /// <summary>Horizontal offset of a line inside the text box.</summary>
    /// <param name="line">The line (TextLine).</param>
    /// <returns>The resulting float value.</returns>
    public float LineOffset(TextLine line) => Alignment switch
    {
        TextAlignment.Center => (Width - line.Width) / 2,
        TextAlignment.End => Width - line.Width,
        _ => 0,
    };
}

/// <summary>Strokes connected segments with round caps and joins (check marks and similar vector parts).</summary>
/// <param name="Points">The points (IReadOnlyList&lt;Point&gt;).</param>
/// <param name="Thickness">The logical inset distances on the four edges.</param>
/// <param name="Color">The color (Color).</param>
public sealed record DrawPolyline(IReadOnlyList<Point> Points, float Thickness, Color Color) : DrawCommand;

/// <summary>Intersects the current drawing clip with a rounded rectangle until the matching Pop command.</summary>
/// <param name="Rect">The rect (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
public sealed record PushClip(Rect Rect, CornerRadius Radius) : DrawCommand;

/// <summary>Composites the following commands with an opacity, within <paramref name="Bounds" />.</summary>
/// <param name="Opacity">The alpha multiplier, clamped to the range from zero to one.</param>
/// <param name="Bounds">The bounds (Rect).</param>
public sealed record PushOpacity(float Opacity, Rect Bounds) : DrawCommand;

/// <summary>Begins an isolated layer initialized with the filtered existing canvas inside a rounded border box. Later content may overflow the box; Pop composites the whole group with Opacity. This samples the current surface, not pixels outside its native window.</summary>
/// <param name="Rect">The border box receiving the filtered backdrop.</param>
/// <param name="Radius">The border-box corner radii.</param>
/// <param name="Bounds">The layer extent, including foreground overflow.</param>
/// <param name="Blur">The finite nonnegative Gaussian standard deviation in logical units; the renderer bounds the sampling radius to256.</param>
/// <param name="Saturation">The finite nonnegative saturation multiplier.</param>
/// <param name="SaturationFirst">Whether saturation precedes blur.</param>
/// <param name="Opacity">The group alpha multiplier in the range zero to one.</param>
public sealed record PushBackdrop(Rect Rect, CornerRadius Radius, Rect Bounds, float Blur, float Saturation = 1, bool SaturationFirst = false, float Opacity = 1) : DrawCommand
{
    private IReadOnlyList<ImageFilter>? filters;
    /// <summary>An optional copied ordered chain replacing the legacy Blur/Saturation pair. Null retains that pair.</summary>
    public IReadOnlyList<ImageFilter>? Filters { get => filters; init => filters = value is null ? null : Array.AsReadOnly(value.ToArray()); }
}

/// <summary>Translates subsequent drawing commands until the matching Pop command.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public sealed record PushTranslate(float X, float Y) : DrawCommand;

/// <summary>Transforms the following commands (CSS <c>transform</c>), in the current coordinates.</summary>
/// <param name="Matrix">The matrix (Matrix).</param>
public sealed record PushTransform(Matrix Matrix) : DrawCommand;

/// <summary>Restores the drawing state saved by the most recent push command.</summary>
public sealed record Pop : DrawCommand
{
    /// <summary>The shared drawing-state restoration command.</summary>
    public static readonly Pop Instance = new();
}

/// <summary>An immutable list of drawing commands for a surface of <see cref="Size" /> logical units.</summary>
/// <param name="size">The logical dimensions of the surface or content.</param>
/// <param name="commands">The ordered drawing commands.</param>
public sealed class DisplayList(Size size, IReadOnlyList<DrawCommand> commands)
{
    /// <summary>Gets the logical dimensions of the drawing surface.</summary>
    public Size Size { get; } = size;

    /// <summary>Gets the ordered drawing commands to execute on the surface.</summary>
    public IReadOnlyList<DrawCommand> Commands { get; } = commands;

    /// <summary>Readable form for tests: one command per line, indented by push depth.</summary>
    /// <returns>The resulting string value.</returns>
    public string Describe()
    {
        var builder = new StringBuilder();
        var depth = 0;
        foreach (var command in Commands)
        {
            if (command is Pop) depth--;
            builder.Append(' ', depth * 2).Append(command switch
            {
                FillRectangle f => $"Fill {f.Rect} r={f.Radius:0.##} {f.Color}",
                DrawBorder b => $"Border {b.Rect} r={b.Radius:0.##} w={b.Widths} {(b.IsUniform ? b.Top.ToString() : $"{b.Top} {b.Right} {b.Bottom} {b.Left}")}",
                DrawText t => $"Text \"{t.Layout.Text}\" at {t.Origin} {t.Color}" +
                    (t.TextStart != 0 || t.TextLength is not null ? $" range={t.TextStart}+{t.TextLength?.ToString() ?? "end"}" : "") + (t.Decoration is { } d ? $" {d}" : ""),
                FillGradient g => $"Gradient {g.Rect} r={g.Radius:0.##} {g.Brush}",
                DrawShadow s => $"Shadow{(s.Inset ? " inset" : "")} {s.Rect} r={s.Radius:0.##} {s.Offset} blur={s.Blur:0.##} spread={s.Spread:0.##} {s.Color}",
                DrawPolyline l => $"Polyline {string.Join(" ", l.Points)} w={l.Thickness:0.##} {l.Color}",
                FillImage f => $"Fill {f.Area} r={f.Radius:0.##} with {f.Image} tile {f.Tile}{(f.RepeatX ? " repeat-x" : "")}{(f.RepeatY ? " repeat-y" : "")}",
                DrawImage i => $"Image {i.Image} into {i.Destination}" + (i.Clip is { } clip ? $" clip {clip}" : "") + (i.Tint is { } tint ? $" tint {tint}" : ""),
                PushClip c => $"Clip {c.Rect} r={c.Radius:0.##}",
                PushOpacity o => $"Opacity {o.Opacity:0.##}",
                PushBlend b => $"Blend {b.Mode} {b.Bounds}",
                PushFilter f => $"Filter [{string.Join(", ",f.Filters)}] opacity={f.Opacity:0.##}",
                PushBackdrop b => $"Backdrop {b.Rect} r={b.Radius} blur={b.Blur:0.##} saturation={b.Saturation:0.##} first={b.SaturationFirst} opacity={b.Opacity:0.##}",
                PushTranslate t => $"Translate {t.X:0.##},{t.Y:0.##}",
                PushTransform m => $"Transform {m.Matrix}",
                PushComposition => "Composition",
                Pop => "Pop",
                _ => command.ToString(),
            }).Append('\n');
            if (command is PushClip or PushOpacity or PushFilter or PushBlend or PushBackdrop or PushTranslate or PushTransform or PushComposition) depth++;
        }
        return builder.ToString();
    }
}

/// <summary>Accumulates drawing commands and creates a display list for a logical surface.</summary>
public sealed class DisplayListBuilder
{
    private readonly List<DrawCommand> commands = [];
    private int depth;

    internal void Reset()
    {
        commands.Clear();
        depth = 0;
    }

    /// <summary>Appends a drawing command to the display list being built.</summary>
    /// <param name="command">The drawing or keyboard command to add or execute.</param>
    public void Add(DrawCommand command)
    {
        if (command is Pop)
        {
            if (depth == 0) throw new InvalidOperationException("Pop without a matching push.");
            depth--;
        }
        else if (command is PushClip or PushOpacity or PushFilter or PushBlend or PushBackdrop or PushTranslate or PushTransform or PushComposition) depth++;
        commands.Add(command);
    }

    /// <summary>Creates a display list from the accumulated commands and the logical surface size.</summary>
    /// <param name="size">The logical dimensions of the surface or content.</param>
    /// <returns>The display list containing the accumulated commands.</returns>
    public DisplayList Build(Size size)
    {
        if (depth != 0) throw new InvalidOperationException("Unbalanced push commands.");
        return new DisplayList(size, commands.ToArray());
    }
}

/// <summary>Pixels in BGRA order with premultiplied alpha, rows top-down.</summary>
/// <param name="width">The width of the target pixel buffer.</param>
/// <param name="height">The height of the target pixel buffer.</param>
/// <param name="pixels">The address of the top-down BGRA premultiplied pixel buffer.</param>
public sealed class RenderedImage(int width, int height, byte[] pixels)
{
    /// <summary>Gets the image width in physical pixels.</summary>
    public int Width { get; } = width;

    /// <summary>Gets the image height in physical pixels.</summary>
    public int Height { get; } = height;

    /// <summary>Gets the top-down BGRA premultiplied pixel buffer.</summary>
    public byte[] Pixels { get; } = pixels;

    /// <summary>Gets the number of bytes between the starts of successive pixel rows.</summary>
    public int Stride => Width * 4;

    /// <summary>The color of a pixel, un-premultiplied.</summary>
    /// <param name="x">The x (int).</param>
    /// <param name="y">The y (int).</param>
    /// <returns>The resulting Color value.</returns>
    public Color GetPixel(int x, int y)
    {
        var i = (y * Width + x) * 4;
        var a = Pixels[i + 3];
        if (a == 0) return Color.Transparent;
        byte Unmultiply(byte value) => (byte)Math.Min(255, Math.Round(value * 255.0 / a));
        return new Color(Unmultiply(Pixels[i + 2]), Unmultiply(Pixels[i + 1]), Unmultiply(Pixels[i]), a);
    }
}

/// <summary>
/// Draws display lists. The renderer also provides the font and shaping services, so text is measured with the same
/// faces it is rasterized with.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>Gets the provider used to resolve font faces and glyph fallbacks.</summary>
    IFontProvider Fonts { get; }

    /// <summary>Gets the service used to shape text into positioned glyphs.</summary>
    ITextShaper Shaper { get; }

    /// <summary>Decodes images into images this renderer draws.</summary>
    IImageDecoder Images { get; }

    /// <summary>Renders into a new image of <c>ceil(size × scale)</c> physical pixels.</summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    /// <returns>The resulting RenderedImage value.</returns>
    RenderedImage RenderToImage(DisplayList displayList, float scale, Color background);

    /// <summary>Renders into memory owned by the caller (for example a window buffer), BGRA premultiplied.</summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="pixels">The address of the top-down BGRA premultiplied pixel buffer.</param>
    /// <param name="width">The width of the target pixel buffer.</param>
    /// <param name="height">The height of the target pixel buffer.</param>
    /// <param name="stride">The number of bytes between the starts of successive pixel rows.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background);

    /// <summary>
    /// Renders only inside <paramref name="damage" /> (physical pixels); the rest of the buffer keeps what it had.
    /// Null renders everything, an empty list nothing.
    /// </summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="pixels">The address of the top-down BGRA premultiplied pixel buffer.</param>
    /// <param name="width">The width of the target pixel buffer.</param>
    /// <param name="height">The height of the target pixel buffer.</param>
    /// <param name="stride">The number of bytes between the starts of successive pixel rows.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    /// <param name="damage">The damage (IReadOnlyList&lt;PixelRect&gt;?).</param>
    void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Color background, IReadOnlyList<PixelRect>? damage)
    {
        if (damage is null) Render(displayList, pixels, width, height, stride, scale, background);
        else if (damage.Count > 0) Render(displayList, pixels, width, height, stride, scale, background);
    }

    /// <summary>Encodes the rendered pixel image as a PNG byte sequence.</summary>
    /// <param name="image">The rendered or decoded image to process.</param>
    /// <returns>The PNG-encoded image bytes.</returns>
    byte[] EncodePng(RenderedImage image);
}
