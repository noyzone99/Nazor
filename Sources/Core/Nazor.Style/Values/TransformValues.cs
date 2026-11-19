using System.Globalization;

namespace Nazor.Style;

/// <summary>One function of <c>transform</c> (CSS Transforms 1, in 2D); angles are in degrees.</summary>
public abstract record TransformFunction
{
    private protected TransformFunction()
    {
    }

    private protected static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}

/// <summary>
/// <c>translate()</c>, <c>translateX()</c>, <c>translateY()</c>: percentages of the element's border box.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public sealed record TranslateFunction(Length X, Length Y) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"translate({X}, {Y})";
}

/// <summary><c>scale()</c>, <c>scaleX()</c>, <c>scaleY()</c>.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public sealed record ScaleFunction(double X, double Y) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"scale({Number(X)}, {Number(Y)})";
}

/// <summary><c>rotate()</c>, clockwise.</summary>
/// <param name="Angle">The angle (double).</param>
public sealed record RotateFunction(double Angle) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"rotate({Number(Angle)}deg)";
}

/// <summary><c>skew()</c>, <c>skewX()</c>, <c>skewY()</c>.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public sealed record SkewFunction(double X, double Y) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"skew({Number(X)}deg, {Number(Y)}deg)";
}

/// <summary><c>matrix(a, b, c, d, e, f)</c>: x' = a·x + c·y + e, y' = b·x + d·y + f, translations in px.</summary>
/// <param name="A">The alpha channel in the range from zero to 255.</param>
/// <param name="B">The blue channel in the range from zero to 255.</param>
/// <param name="C">The c (double).</param>
/// <param name="D">The d (double).</param>
/// <param name="E">The e (double).</param>
/// <param name="F">The f (double).</param>
public sealed record MatrixFunction(double A, double B, double C, double D, double E, double F) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"matrix({Number(A)}, {Number(B)}, {Number(C)}, {Number(D)}, {Number(E)}, {Number(F)})";
}

/// <summary>
/// The value of a transition between two transform lists that do not match function by function (CSS Transforms 1
/// §9): their matrices, which depend on the element's box, are interpolated by decomposition when the element is
/// drawn. Never written in NSS.
/// </summary>
/// <param name="From">The from (ValueList&lt;TransformFunction&gt;).</param>
/// <param name="To">The to (ValueList&lt;TransformFunction&gt;).</param>
/// <param name="Progress">The progress (double).</param>
public sealed record MixFunction(ValueList<TransformFunction> From, ValueList<TransformFunction> To, double Progress) : TransformFunction
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"mix({From}; {To}; {Number(Progress)})";
}

/// <summary>How an image fills its content box (<c>object-fit</c>).</summary>
public enum ObjectFit : byte
{
    /// <summary>Stretches the image to fill the destination box.</summary>
    Fill,

    /// <summary>Preserves the aspect ratio while fitting the whole image inside the box.</summary>
    Contain,

    /// <summary>Preserves the aspect ratio while filling and cropping to the box.</summary>
    Cover,

    /// <summary>Uses the intrinsic image dimensions.</summary>
    None,

    /// <summary>Uses the smaller result of intrinsic sizing and contain sizing.</summary>
    ScaleDown,
}

/// <summary>
/// A point of a box from its top left corner, as <c>object-position</c> writes it: percentages are of the space left
/// over (50% centers), lengths are offsets.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public readonly record struct PositionValue(Length X, Length Y)
{
    /// <summary>A centered axis position at 50 percent of the reference dimension.</summary>
    public static readonly PositionValue Center = new(Length.Percent(50), Length.Percent(50));

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{X} {Y}";
}

/// <summary>
/// The point <c>transform</c> turns around: lengths or percentages of the border box from its top left corner.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public readonly record struct TransformOrigin(Length X, Length Y)
{
    /// <summary>A transform origin centered horizontally and vertically in the element's box.</summary>
    public static readonly TransformOrigin Center = new(Length.Percent(50), Length.Percent(50));

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{X} {Y}";
}
