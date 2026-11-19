using Nazor.Graphics;

namespace Nazor.Platform;

/// <summary>Shared geometry for desktop window frames drawn by Nazor.</summary>
public static class WindowFrameGeometry
{
    /// <summary>Gets the resize grip bounds at the lower-right of the logical client area.</summary>
    /// <param name="clientSize">The size of the client area in logical pixels.</param>
    /// <param name="radius">The effective corner radii of the visible frame.</param>
    /// <returns>The rectangle used to draw and hit test the grip, or an empty rectangle when the client has no usable area.</returns>
    public static Rect ResizeGripBounds(Size clientSize, CornerRadius radius = default)
    {
        const float diagonalInset = 0.3f;
        var right = clientSize.Width - radius.BottomRight.X * diagonalInset;
        var bottom = clientSize.Height - radius.BottomRight.Y * diagonalInset;
        var size = Math.Min(16, Math.Min(right, bottom));
        return size > 0 ? new(right - size, bottom - size, size, size) : default;
    }

    /// <summary>Returns the resize edge at a logical frame point, using the top, bottom, left and right bits 1, 2, 4 and 8.</summary>
    /// <param name="frameSize">The size of the visible frame in logical pixels.</param>
    /// <param name="point">The point relative to the top-left of the frame, which may be outside it.</param>
    /// <param name="inset">The nonnegative width of the inner resize band.</param>
    /// <param name="outset">The nonnegative width of the outer resize band.</param>
    /// <param name="cornerLength">The nonnegative distance along each edge assigned to its corner, or null to use the overlap of the bands.</param>
    /// <returns>The edge bits, or zero outside the configured bands.</returns>
    public static uint ResizeEdgeAt(Size frameSize, Point point, float inset, float outset, float? cornerLength)
    {
        var width = frameSize.Width;
        var height = frameSize.Height;
        if (width <= 0 || height <= 0 || point.X < -outset || point.Y < -outset ||
            point.X >= width + outset || point.Y >= height + outset) return 0;

        var inside = point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height;
        if (inside && inset <= 0) return 0;
        var xInset = Math.Min(inset, width / 2);
        var yInset = Math.Min(inset, height / 2);
        var horizontal = point.X < xInset ? 4u : point.X >= width - xInset ? 8u : 0;
        var vertical = point.Y < yInset ? 1u : point.Y >= height - yInset ? 2u : 0;
        if ((horizontal | vertical) == 0 || cornerLength is not { } length) return horizontal | vertical;

        var xCorner = Math.Min(length, width / 2);
        var yCorner = Math.Min(length, height / 2);
        var fromSide = point.X < xCorner ? 4u : point.X >= width - xCorner ? 8u : 0;
        var fromEnd = point.Y < yCorner ? 1u : point.Y >= height - yCorner ? 2u : 0;
        if (horizontal == 0) return vertical | fromSide;
        if (vertical == 0) return horizontal | fromEnd;
        if (fromSide != 0 && fromEnd != 0) return horizontal | vertical;
        return Math.Min(point.X, width - point.X) <= Math.Min(point.Y, height - point.Y) ? horizontal : vertical;
    }

    /// <summary>Scales corner radii proportionally when adjacent corners would overlap.</summary>
    /// <param name="radius">The requested corner radii in logical pixels.</param>
    /// <param name="frameSize">The size of the visible frame in logical pixels.</param>
    /// <returns>The corner radii that fit the frame.</returns>
    public static CornerRadius NormalizeRadius(CornerRadius radius, Size frameSize)
    {
        var factor = 1f;
        Fit(frameSize.Width, radius.TopLeft.X + radius.TopRight.X);
        Fit(frameSize.Width, radius.BottomLeft.X + radius.BottomRight.X);
        Fit(frameSize.Height, radius.TopLeft.Y + radius.BottomLeft.Y);
        Fit(frameSize.Height, radius.TopRight.Y + radius.BottomRight.Y);
        return factor == 1 ? radius : new(Scale(radius.TopLeft), Scale(radius.TopRight), Scale(radius.BottomRight), Scale(radius.BottomLeft));

        void Fit(float available, float needed)
        {
            if (needed > 0) factor = Math.Min(factor, Math.Max(0, available) / needed);
        }

        Radius Scale(Radius corner) => new(corner.X * factor, corner.Y * factor);
    }
}
