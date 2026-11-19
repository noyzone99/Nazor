using System.Globalization;

namespace Nazor.Style;

/// <summary>A supported image-filter function. Operations are applied in declared order.</summary>
public enum FilterKind : byte
{
    /// <summary>Gaussian blur with a logical-length standard deviation.</summary>
    Blur,
    /// <summary>Scales RGB components.</summary>
    Brightness,
    /// <summary>Scales RGB distance from middle gray.</summary>
    Contrast,
    /// <summary>Interpolates toward grayscale.</summary>
    Grayscale,
    /// <summary>Rotates hue by an angle in degrees.</summary>
    HueRotate,
    /// <summary>Interpolates toward inverted RGB components.</summary>
    Invert,
    /// <summary>Scales alpha.</summary>
    Opacity,
    /// <summary>Scales color saturation.</summary>
    Saturate,
    /// <summary>Interpolates toward sepia.</summary>
    Sepia,
}

/// <summary>One filter function. Blur uses Radius; other functions use Amount. HueRotate permits negative degrees; other amounts must be finite and nonnegative.</summary>
/// <param name="Kind">The operation.</param>
/// <param name="Amount">A scalar, or degrees for HueRotate.</param>
/// <param name="Radius">Gaussian standard deviation, resolved from a nonnegative length.</param>
public sealed record FilterFunction(FilterKind Kind, double Amount = 1, Length Radius = default)
{
    /// <summary>Returns the canonical NSS function.</summary>
    /// <returns>A function expression.</returns>
    public override string ToString() => Kind == FilterKind.Blur ? $"blur({Radius})" :
        $"{(Kind == FilterKind.HueRotate ? "hue-rotate" : Kind.ToString().ToLowerInvariant())}({Amount.ToString("R", CultureInfo.InvariantCulture)}{(Kind == FilterKind.HueRotate ? "deg" : "")})";
}

/// <summary>An ordered list shared by filter and backdrop-filter. Empty means none. NSS accepts at most64 operations, including repetitions.</summary>
/// <param name="Functions">The ordered function values.</param>
public sealed record FilterValue(ValueList<FilterFunction> Functions)
{
    /// <summary>The initial unfiltered value.</summary>
    public static readonly FilterValue None = new(ValueList<FilterFunction>.Empty);

    /// <summary>Whether there are no operations.</summary>
    public bool IsNone => Functions.Items.Count == 0;

    /// <summary>Formats the ordered chain or none.</summary>
    /// <returns>A canonical NSS value.</returns>
    public override string ToString() => IsNone ? "none" : string.Join(" ", Functions.Items);
}
