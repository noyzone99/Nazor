using System.Globalization;

namespace Nazor.Style;

/// <summary>A color stop of a gradient; <see cref="Position" /> null is placed by the CSS fix-up rules.</summary>
/// <param name="Color">The color (Color).</param>
/// <param name="Position">The position (Length?).</param>
public sealed record ColorStop(Color Color, Length? Position)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Position is { } position ? $"{Color} {position}" : Color.ToString();
}

/// <summary>A layer of <c>background-image</c>: a gradient or an image by URL.</summary>
public abstract record ImageValue;

/// <summary>
/// <c>url()</c>: an image file. The compiler resolves a relative URL against the folder of the style sheet, so the
/// value is relative to the project (and to the application folder, where the SDK copies the images); http and https
/// URLs stay as written.
/// </summary>
/// <param name="Url">The url (string).</param>
public sealed record UrlImage(string Url) : ImageValue
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"url(\"{Url}\")";
}

/// <summary>A gradient image.</summary>
/// <param name="Stops">The stops (ValueList&lt;ColorStop&gt;).</param>
public abstract record Gradient(ValueList<ColorStop> Stops) : ImageValue
{
    /// <summary>Whether the interval from the first to the last fixed-up stop repeats in both directions.</summary>
    public bool Repeating { get; init; }
}

/// <summary>How a background image repeats in its box.</summary>
public enum BackgroundRepeat : byte
{
    /// <summary>Tiles the background image along both axes.</summary>
    Repeat,

    /// <summary>Draws one background image without tiling.</summary>
    NoRepeat,

    /// <summary>Tiles the background image horizontally.</summary>
    RepeatX,

    /// <summary>Tiles the background image vertically.</summary>
    RepeatY,
}

/// <summary>Specifies explicit image dimensions or aspect-preserving cover and contain sizing.</summary>
public enum BackgroundSizeKind : byte
{
    /// <summary>Uses the declared horizontal and vertical image dimensions.</summary>
    Explicit,

    /// <summary>Scales the image proportionally until it covers the painting box.</summary>
    Cover,

    /// <summary>Scales the image proportionally to fit entirely inside the painting box.</summary>
    Contain,
}

/// <summary>
/// A value of <c>background-size</c>: <c>cover</c>, <c>contain</c> or a width and a height (auto keeps the
/// proportions).
/// </summary>
/// <param name="Kind">The background size kind represented by this value.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct BackgroundSize(BackgroundSizeKind Kind, SizeValue Width, SizeValue Height)
{
    /// <summary>Intrinsic background-image sizing, preserving the image's aspect ratio.</summary>
    public static readonly BackgroundSize Auto = new(BackgroundSizeKind.Explicit, SizeValue.Auto, SizeValue.Auto);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        BackgroundSizeKind.Cover => "cover",
        BackgroundSizeKind.Contain => "contain",
        _ => $"{Width} {Height}",
    };
}

/// <summary>
/// <c>linear-gradient()</c>: an angle in degrees (0 points up, clockwise), or a side or corner as <see cref="ToX" />
/// (−1 left, 1 right) and <see cref="ToY" /> (−1 top, 1 bottom), resolved against the box when drawn as CSS requires
/// for corners.
/// </summary>
/// <param name="Angle">The angle (double?).</param>
/// <param name="ToX">The to x (int).</param>
/// <param name="ToY">The to y (int).</param>
/// <param name="Stops">The stops (ValueList&lt;ColorStop&gt;).</param>
public sealed record LinearGradient(double? Angle, int ToX, int ToY, ValueList<ColorStop> Stops) : Gradient(Stops)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var direction = Angle is { } angle ? angle.ToString("0.###", CultureInfo.InvariantCulture) + "deg"
            : "to" + (ToY < 0 ? " top" : ToY > 0 ? " bottom" : "") + (ToX < 0 ? " left" : ToX > 0 ? " right" : "");
        return $"{(Repeating ? "repeating-" : "")}linear-gradient({direction}, {string.Join(", ", Stops.Items)})";
    }
}

/// <summary>Specifies whether a radial gradient uses a circle or ellipse.</summary>
public enum RadialShape : byte
{
    /// <summary>Uses independently resolved horizontal and vertical radii.</summary>
    Ellipse,

    /// <summary>Uses the same radius along both axes.</summary>
    Circle,
}

/// <summary>Specifies how a radial gradient's radii are derived from its center and painting box.</summary>
public enum RadialExtent : byte
{
    /// <summary>Extends the gradient to the farthest corner of its painting box.</summary>
    FarthestCorner,

    /// <summary>Extends the gradient to the closest corner of its painting box.</summary>
    ClosestCorner,

    /// <summary>Extends the gradient to the farthest side of its painting box.</summary>
    FarthestSide,

    /// <summary>Extends the gradient to the closest side of its painting box.</summary>
    ClosestSide,

    /// <summary>The radii are <see cref = "RadialGradient.SizeX"/> and <see cref = "RadialGradient.SizeY"/>.</summary>
    Explicit,
}

/// <summary>
/// <c>radial-gradient()</c>: an ellipse or a circle, sized by an extent keyword or by explicit radii (percentages of
/// the box for an ellipse), centered at <see cref="CenterX" />, <see cref="CenterY" />.
/// </summary>
/// <param name="Shape">The shape (RadialShape).</param>
/// <param name="Extent">The extent (RadialExtent).</param>
/// <param name="SizeX">The size x (Length).</param>
/// <param name="SizeY">The size y (Length).</param>
/// <param name="CenterX">The center x (Length).</param>
/// <param name="CenterY">The center y (Length).</param>
/// <param name="Stops">The stops (ValueList&lt;ColorStop&gt;).</param>
public sealed record RadialGradient(RadialShape Shape, RadialExtent Extent, Length SizeX, Length SizeY, Length CenterX, Length CenterY, ValueList<ColorStop> Stops)
    : Gradient(Stops)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var size = Extent == RadialExtent.Explicit
            ? Shape == RadialShape.Circle ? SizeX.ToString() : $"{SizeX} {SizeY}"
            : Extent switch
            {
                RadialExtent.ClosestCorner => "closest-corner",
                RadialExtent.FarthestSide => "farthest-side",
                RadialExtent.ClosestSide => "closest-side",
                _ => "farthest-corner",
            };
        return $"{(Repeating ? "repeating-" : "")}radial-gradient({Shape.ToString().ToLowerInvariant()} {size} at {CenterX} {CenterY}, {string.Join(", ", Stops.Items)})";
    }
}

/// <summary>A clockwise angular gradient, with zero pointing up. Stop positions use percentages of one turn; missing positions follow gradient stop fix-up.</summary>
/// <param name="Angle">Rotation in degrees.</param>
/// <param name="CenterX">Horizontal center relative to the gradient box.</param>
/// <param name="CenterY">Vertical center relative to the gradient box.</param>
/// <param name="Stops">Colors with optional percentage positions.</param>
public sealed record ConicGradient(double Angle, Length CenterX, Length CenterY, ValueList<ColorStop> Stops) : Gradient(Stops)
{
    /// <summary>Formats the conic image using canonical percentage stops.</summary>
    /// <returns>A compilable NSS gradient.</returns>
    public override string ToString() => $"{(Repeating ? "repeating-" : "")}conic-gradient(from {Angle.ToString("R", CultureInfo.InvariantCulture)}deg at {CenterX} {CenterY}, " +
        string.Join(", ", Stops.Items.Select(stop => stop.Position is { } position
            ? $"{stop.Color} {position.Value.ToString("R", CultureInfo.InvariantCulture)}%" : stop.Color.ToString())) + ")";
}
