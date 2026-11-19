using System.Globalization;

namespace Nazor.Graphics;

/// <summary>A point in logical units.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public readonly record struct Point(float X, float Y)
{
    /// <summary>The point with all components set to zero.</summary>
    public static readonly Point Zero = new(0, 0);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X:0.##}, {Y:0.##})");
}

/// <summary>A size in logical units.</summary>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct Size(float Width, float Height)
{
    /// <summary>The size with all components set to zero.</summary>
    public static readonly Size Zero = new(0, 0);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Width:0.##}x{Height:0.##}");
}

/// <summary>
/// An elliptical corner: <paramref name="X" /> along the width, <paramref name="Y" /> along the height.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public readonly record struct Radius(float X, float Y)
{
    /// <summary>The radius with all components set to zero.</summary>
    public static readonly Radius Zero = new(0, 0);

    /// <summary>Gets whether either radius axis is nonpositive, producing a square corner.</summary>
    public bool IsZero => X <= 0 || Y <= 0;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => X == Y
        ? X.ToString("0.##", CultureInfo.InvariantCulture)
        : string.Create(CultureInfo.InvariantCulture, $"{X:0.##}x{Y:0.##}");
}

/// <summary>
/// The radii of the rounded corners of a box, one elliptical <see cref="Radius" /> per corner as in CSS. Two numbers
/// or a single number give the same radius to the four corners.
/// </summary>
/// <param name="TopLeft">The top left (Radius).</param>
/// <param name="TopRight">The top right (Radius).</param>
/// <param name="BottomRight">The bottom right (Radius).</param>
/// <param name="BottomLeft">The bottom left (Radius).</param>
public readonly record struct CornerRadius(Radius TopLeft, Radius TopRight, Radius BottomRight, Radius BottomLeft) : IFormattable
{
    /// <summary>The corner radius with all components set to zero.</summary>
    public static readonly CornerRadius Zero = new(0, 0);

    /// <summary>Initializes a new instance of CornerRadius.</summary>
    /// <param name="x">The x (float).</param>
    /// <param name="y">The y (float).</param>
    public CornerRadius(float x, float y) : this(new Radius(x, y), new Radius(x, y), new Radius(x, y), new Radius(x, y))
    {
    }

    /// <summary>Converts a float value to CornerRadius.</summary>
    /// <param name="radius">The corner radius to apply uniformly.</param>
    /// <returns>The resulting CornerRadius value.</returns>
    public static implicit operator CornerRadius(float radius) => new(radius, radius);

    /// <summary>Gets whether all four corners are square.</summary>
    public bool IsZero => TopLeft.IsZero && TopRight.IsZero && BottomRight.IsZero && BottomLeft.IsZero;

    /// <summary>Gets whether all corner radii are equal.</summary>
    public bool IsUniform => TopLeft == TopRight && TopRight == BottomRight && BottomRight == BottomLeft;

    /// <summary>
    /// The radii of the inner edge of a border with these widths (CSS Backgrounds §5.2): each corner loses the widths
    /// of its two sides, horizontal and vertical.
    /// </summary>
    /// <param name="widths">The logical border thicknesses on the four edges.</param>
    /// <returns>The resulting CornerRadius value.</returns>
    public CornerRadius Deflate(Thickness widths) => new(
        Shrink(TopLeft, widths.Left, widths.Top), Shrink(TopRight, widths.Right, widths.Top),
        Shrink(BottomRight, widths.Right, widths.Bottom), Shrink(BottomLeft, widths.Left, widths.Bottom));

    /// <summary>
    /// The radii of an edge drawn <paramref name="amount" /> outside the box: rounded corners grow, square ones stay
    /// square.
    /// </summary>
    /// <param name="amount">The logical distance by which to grow, shrink, or offset the value.</param>
    /// <returns>The resulting CornerRadius value.</returns>
    public CornerRadius Inflate(float amount) => new(Grow(TopLeft, amount), Grow(TopRight, amount), Grow(BottomRight, amount), Grow(BottomLeft, amount));

    private static Radius Shrink(Radius radius, float x, float y) => new(Math.Max(0, radius.X - x), Math.Max(0, radius.Y - y));

    private static Radius Grow(Radius radius, float amount) =>
        radius.IsZero ? Radius.Zero : new Radius(Math.Max(0, radius.X + amount), Math.Max(0, radius.Y + amount));

    /// <summary>Returns a textual representation of this value.</summary>
    /// <param name="format">The numeric format string used for component values.</param>
    /// <param name="formatProvider">The provider used to format numeric values.</param>
    /// <returns>The textual representation of this value.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        string One(Radius r) => r.X == r.Y ? r.X.ToString(format, formatProvider) : r.X.ToString(format, formatProvider) + "x" + r.Y.ToString(format, formatProvider);
        return IsUniform ? One(TopLeft) : $"{One(TopLeft)},{One(TopRight)},{One(BottomRight)},{One(BottomLeft)}";
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>Distances from the four edges of a box.</summary>
/// <param name="Left">The value associated with the left side or operand.</param>
/// <param name="Top">The value associated with the top edge.</param>
/// <param name="Right">The value associated with the right side or operand.</param>
/// <param name="Bottom">The value associated with the bottom edge.</param>
public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    /// <summary>The thickness with all components set to zero.</summary>
    public static readonly Thickness Zero = new(0, 0, 0, 0);

    /// <summary>Gets the sum of the left and right edge distances.</summary>
    public float Horizontal => Left + Right;

    /// <summary>Gets the sum of the top and bottom edge distances.</summary>
    public float Vertical => Top + Bottom;

    /// <summary>Gets whether all four edge distances are zero.</summary>
    public bool IsZero => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>Adds the corresponding components of the two operands.</summary>
    /// <param name="a">The a (Thickness).</param>
    /// <param name="b">The blue channel in the range from zero to 255.</param>
    /// <returns>The resulting Thickness value.</returns>
    public static Thickness operator +(Thickness a, Thickness b) => new(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Left:0.##},{Top:0.##},{Right:0.##},{Bottom:0.##}");
}

/// <summary>An axis-aligned rectangle in logical units.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    /// <summary>An empty rect value.</summary>
    public static readonly Rect Empty = new(0, 0, 0, 0);

    /// <summary>Gets the horizontal coordinate of the rectangle's right edge.</summary>
    public float Right => X + Width;

    /// <summary>Gets the vertical coordinate of the rectangle's bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>Gets the top-left corner in logical units.</summary>
    public Point Position => new(X, Y);

    /// <summary>Gets the width and height in logical units.</summary>
    public Size Size => new(Width, Height);

    /// <summary>Gets whether the width or height is nonpositive.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Tests whether the point lies inside the rectangle, including the top and left edges and excluding the bottom
    /// and right edges.
    /// </summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>true if the point lies inside the rectangle's half-open bounds; otherwise, false.</returns>
    public bool Contains(Point point) => point.X >= X && point.Y >= Y && point.X < Right && point.Y < Bottom;

    /// <summary>
    /// Insets the rectangle by the supplied edge thicknesses, clamping its resulting dimensions at zero.
    /// </summary>
    /// <param name="thickness">The logical inset distances on the four edges.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect Deflate(Thickness thickness) =>
        new(X + thickness.Left, Y + thickness.Top, Math.Max(0, Width - thickness.Horizontal), Math.Max(0, Height - thickness.Vertical));

    /// <summary>Translates the rectangle without changing its dimensions.</summary>
    /// <param name="dx">The horizontal translation in logical units.</param>
    /// <param name="dy">The vertical translation in logical units.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect Offset(float dx, float dy) => new(X + dx, Y + dy, Width, Height);

    /// <summary>The rectangle grown by <paramref name="amount" /> on every side (shrunk when negative).</summary>
    /// <param name="amount">The logical distance by which to grow, shrink, or offset the value.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect Inflate(float amount) =>
        new(X - amount, Y - amount, Math.Max(0, Width + 2 * amount), Math.Max(0, Height + 2 * amount));

    /// <summary>
    /// Computes the overlapping rectangle, returning Empty when there is no positive-area intersection.
    /// </summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>The intersection rectangle, or Empty when there is no positive-area overlap.</returns>
    public Rect Intersect(Rect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= x || bottom <= y ? Empty : new Rect(x, y, right - x, bottom - y);
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{X:0.##}, {Y:0.##}, {Width:0.##}x{Height:0.##}]");
}

/// <summary>A rectangle of whole physical pixels of a surface.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    /// <summary>Gets the physical pixel coordinate immediately after the right edge.</summary>
    public int Right => X + Width;

    /// <summary>Gets the physical pixel coordinate immediately after the bottom edge.</summary>
    public int Bottom => Y + Height;
}

/// <summary>
/// A 2D affine transform with the layout of <c>System.Numerics.Matrix3x2</c> and of CSS <c>matrix(a, b, c, d, e, f)</c>:
/// x' = M11·x + M21·y + OffsetX, y' = M12·x + M22·y + OffsetY.
/// </summary>
/// <param name="M11">The m11 (float).</param>
/// <param name="M12">The m12 (float).</param>
/// <param name="M21">The m21 (float).</param>
/// <param name="M22">The m22 (float).</param>
/// <param name="OffsetX">The offset x (float).</param>
/// <param name="OffsetY">The offset y (float).</param>
public readonly record struct Matrix(float M11, float M12, float M21, float M22, float OffsetX, float OffsetY)
{
    /// <summary>The transform that preserves every point.</summary>
    public static readonly Matrix Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>Gets whether the transform leaves every point unchanged.</summary>
    public bool IsIdentity => this == Identity;

    /// <summary>Creates a transform that translates points by horizontal and vertical logical distances.</summary>
    /// <param name="x">The x (float).</param>
    /// <param name="y">The y (float).</param>
    /// <returns>The resulting Matrix value.</returns>
    public static Matrix Translation(float x, float y) => new(1, 0, 0, 1, x, y);

    /// <summary>Creates a transform that scales horizontal and vertical coordinates independently.</summary>
    /// <param name="x">The x (float).</param>
    /// <param name="y">The y (float).</param>
    /// <returns>The resulting Matrix value.</returns>
    public static Matrix Scaling(float x, float y) => new(x, 0, 0, y, 0, 0);

    /// <summary>Clockwise on the screen (y grows downwards), as CSS <c>rotate()</c>.</summary>
    /// <param name="degrees">The clockwise rotation angle in degrees in screen coordinates.</param>
    /// <returns>The resulting Matrix value.</returns>
    public static Matrix Rotation(double degrees)
    {
        var radians = degrees * Math.PI / 180;
        var (sin, cos) = ((float)Math.Sin(radians), (float)Math.Cos(radians));
        return new(cos, sin, -sin, cos, 0, 0);
    }

    /// <summary>Creates a transform that skews the horizontal and vertical axes by angles in degrees.</summary>
    /// <param name="xDegrees">The horizontal skew angle in degrees.</param>
    /// <param name="yDegrees">The vertical skew angle in degrees.</param>
    /// <returns>The resulting Matrix value.</returns>
    public static Matrix Skewing(double xDegrees, double yDegrees) =>
        new(1, (float)Math.Tan(yDegrees * Math.PI / 180), (float)Math.Tan(xDegrees * Math.PI / 180), 1, 0, 0);

    /// <summary>This transform followed by <paramref name="next" />.</summary>
    /// <param name="next">The next (Matrix).</param>
    /// <returns>The resulting Matrix value.</returns>
    public Matrix Then(Matrix next) => new(
        M11 * next.M11 + M12 * next.M21, M11 * next.M12 + M12 * next.M22,
        M21 * next.M11 + M22 * next.M21, M21 * next.M12 + M22 * next.M22,
        OffsetX * next.M11 + OffsetY * next.M21 + next.OffsetX, OffsetX * next.M12 + OffsetY * next.M22 + next.OffsetY);

    /// <summary>Gets the determinant of the transform's linear component.</summary>
    public float Determinant => M11 * M22 - M12 * M21;

    /// <summary>The inverse; false when the transform flattens the plane (a scale of zero).</summary>
    /// <param name="inverse">When this method returns, contains the inverse produced by the operation.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool TryInvert(out Matrix inverse)
    {
        var determinant = Determinant;
        if (Math.Abs(determinant) < 1e-12f)
        {
            inverse = Identity;
            return false;
        }
        var d = 1 / determinant;
        inverse = new(M22 * d, -M12 * d, -M21 * d, M11 * d,
            (M21 * OffsetY - M22 * OffsetX) * d, (M12 * OffsetX - M11 * OffsetY) * d);
        return true;
    }

    /// <summary>Applies this affine transform to the supplied point.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting Point value.</returns>
    public Point Transform(Point point) =>
        new(point.X * M11 + point.Y * M21 + OffsetX, point.X * M12 + point.Y * M22 + OffsetY);

    /// <summary>The axis-aligned bounds of the transformed rectangle.</summary>
    /// <param name="rect">The rect (Rect).</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect TransformBounds(Rect rect)
    {
        if (IsIdentity) return rect;
        var a = Transform(rect.Position);
        var b = Transform(new Point(rect.Right, rect.Y));
        var c = Transform(new Point(rect.X, rect.Bottom));
        var d = Transform(new Point(rect.Right, rect.Bottom));
        var left = Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X));
        var top = Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
        var right = Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X));
        var bottom = Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// The transform between two others, as CSS interpolates matrices (CSS Transforms 1, 2D decomposition):
    /// translation, scale, rotation by the short way and the remaining matrix move separately, so a rotation stays a
    /// rotation halfway.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The resulting Matrix value.</returns>
    public static Matrix Interpolate(Matrix from, Matrix to, double progress)
    {
        var a = Decomposed.From(from);
        var b = Decomposed.From(to);

        if ((a.ScaleX < 0 && b.ScaleY < 0) || (a.ScaleY < 0 && b.ScaleX < 0))
        {
            a = a with { ScaleX = -a.ScaleX, ScaleY = -a.ScaleY, Angle = a.Angle + (a.Angle < 0 ? 180 : -180) };
        }
        var angleA = a.Angle == 0 ? 360 : a.Angle;
        var angleB = b.Angle == 0 ? 360 : b.Angle;
        if (Math.Abs(angleA - angleB) > 180)
        {
            if (angleA > angleB) angleA -= 360;
            else angleB -= 360;
        }
        double Mix(double x, double y) => x + (y - x) * progress;
        return new Decomposed(Mix(a.TranslateX, b.TranslateX), Mix(a.TranslateY, b.TranslateY), Mix(a.ScaleX, b.ScaleX), Mix(a.ScaleY, b.ScaleY),
            Mix(angleA, angleB), Mix(a.R11, b.R11), Mix(a.R12, b.R12), Mix(a.R21, b.R21), Mix(a.R22, b.R22)).Compose();
    }

    private readonly record struct Decomposed(double TranslateX, double TranslateY, double ScaleX, double ScaleY, double Angle,
        double R11, double R12, double R21, double R22)
    {
        public static Decomposed From(Matrix m)
        {
            double r11 = m.M11, r12 = m.M12, r21 = m.M21, r22 = m.M22;
            var scaleX = Math.Sqrt(r11 * r11 + r12 * r12);
            var scaleY = Math.Sqrt(r21 * r21 + r22 * r22);

            if (r11 * r22 - r12 * r21 < 0)
            {
                if (r11 < r22) scaleX = -scaleX;
                else scaleY = -scaleY;
            }
            if (scaleX != 0)
            {
                r11 /= scaleX;
                r12 /= scaleX;
            }
            if (scaleY != 0)
            {
                r21 /= scaleY;
                r22 /= scaleY;
            }
            var angle = Math.Atan2(r12, r11);
            if (angle != 0)
            {

                var (sin, cos) = (-r12, r11);
                (r11, r12, r21, r22) = (cos * r11 + sin * r21, cos * r12 + sin * r22, -sin * r11 + cos * r21, -sin * r12 + cos * r22);
            }
            return new(m.OffsetX, m.OffsetY, scaleX, scaleY, angle * 180 / Math.PI, r11, r12, r21, r22);
        }

        public Matrix Compose()
        {

            var radians = Angle % 360 * Math.PI / 180;
            var (sin, cos) = (Math.Sin(radians), Math.Cos(radians));

            var (m11, m12, m21, m22) = (cos * R11 + sin * R21, cos * R12 + sin * R22, -sin * R11 + cos * R21, -sin * R12 + cos * R22);
            return new Matrix((float)(m11 * ScaleX), (float)(m12 * ScaleX), (float)(m21 * ScaleY), (float)(m22 * ScaleY), (float)TranslateX, (float)TranslateY);
        }
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"matrix({M11:0.###}, {M12:0.###}, {M21:0.###}, {M22:0.###}, {OffsetX:0.##}, {OffsetY:0.##})");
}

/// <summary>An sRGB color with straight alpha; converted to the backend format only by the renderer.</summary>
/// <param name="R">The red channel in the range from zero to 255.</param>
/// <param name="G">The green channel in the range from zero to 255.</param>
/// <param name="B">The blue channel in the range from zero to 255.</param>
/// <param name="A">The alpha channel in the range from zero to 255.</param>
public readonly record struct Color(byte R, byte G, byte B, byte A)
{
    /// <summary>Fully transparent black, with all RGBA channels set to zero.</summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);

    /// <summary>Opaque black, with zero RGB channels and full alpha.</summary>
    public static readonly Color Black = new(0, 0, 0, 255);

    /// <summary>Opaque white, with all RGBA channels set to 255.</summary>
    public static readonly Color White = new(255, 255, 255, 255);

    /// <summary>Gets whether the alpha channel is zero.</summary>
    public bool IsTransparent => A == 0;

    /// <summary>Multiplies the alpha channel by the opacity, clamped to the range from zero to one.</summary>
    /// <param name="opacity">The alpha multiplier, clamped to the range from zero to one.</param>
    /// <returns>The resulting Color value.</returns>
    public Color WithOpacity(float opacity) => this with { A = (byte)Math.Round(A * Math.Clamp(opacity, 0, 1)) };

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => A == 255 ? $"#{R:x2}{G:x2}{B:x2}" : $"#{R:x2}{G:x2}{B:x2}{A:x2}";
}
