namespace Nazor.Graphics;

/// <summary>
/// A decoded image, kept by the renderer that decoded it: raster pixels or a vector picture. Its <see cref="Size" />
/// is in image pixels (in SVG user units for a vector image).
/// </summary>
public abstract class DecodedImage
{
    /// <summary>Gets the decoded image dimensions.</summary>
    public abstract Size Size { get; }

    /// <summary>A vector image is drawn sharp at any size.</summary>
    public abstract bool IsVector { get; }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{(IsVector ? "vector" : "raster")} {Size.Width:0.##}x{Size.Height:0.##}";
}

/// <summary>Decodes encoded images for a renderer; may be called from any thread.</summary>
public interface IImageDecoder
{
    /// <summary>The image, or null when the data is not an image this decoder reads.</summary>
    /// <param name="data">The data (ReadOnlySpan&lt;byte&gt;).</param>
    /// <returns>The resulting DecodedImage? value.</returns>
    DecodedImage? Decode(ReadOnlySpan<byte> data);
}

/// <summary>
/// Draws <paramref name="Image" /> stretched into <paramref name="Destination" />, clipped to
/// <paramref name="Clip" /> when set (an image that covers its box and overflows it). With <paramref name="Tint" />
/// every pixel takes that color and keeps its alpha (an icon in the text color).
/// </summary>
/// <param name="Image">The rendered or decoded image to process.</param>
/// <param name="Destination">The destination (Rect).</param>
/// <param name="Clip">The clip (Rect?).</param>
/// <param name="Tint">The tint (Color?).</param>
public sealed record DrawImage(DecodedImage Image, Rect Destination, Rect? Clip = null, Color? Tint = null) : DrawCommand;

/// <summary>
/// Fills <paramref name="Area" /> (with its rounded corners) with an image placed in <paramref name="Tile" /> and
/// repeated on the axes that repeat: a background image.
/// </summary>
/// <param name="Area">The area (Rect).</param>
/// <param name="Radius">The corner radius to apply uniformly.</param>
/// <param name="Image">The rendered or decoded image to process.</param>
/// <param name="Tile">The tile (Rect).</param>
/// <param name="RepeatX">The repeat x (bool).</param>
/// <param name="RepeatY">The repeat y (bool).</param>
public sealed record FillImage(Rect Area, CornerRadius Radius, DecodedImage Image, Rect Tile, bool RepeatX, bool RepeatY) : DrawCommand;
