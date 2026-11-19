using System.Globalization;
using Nazor.Style;

namespace Nazor.Controls.Primitives;

/// <summary>A color in the hue, saturation and value (brightness) model, with straight alpha.</summary>
/// <param name="H">The hue in degrees, from 0 (inclusive) to 360 (exclusive).</param>
/// <param name="S">The saturation, from 0 to 1.</param>
/// <param name="V">The value or brightness, from 0 to 1.</param>
/// <param name="A">The opacity, from 0 to 1.</param>
public readonly record struct HsvColor(double H, double S, double V, double A = 1)
{
    /// <summary>Returns the color with every component brought into its range; non-finite components become zero.</summary>
    /// <returns>The clamped color, with the hue wrapped into [0, 360).</returns>
    public HsvColor Clamp() => new(ColorModel.WrapHue(H), ColorModel.Unit(S), ColorModel.Unit(V), ColorModel.Unit(A));
}

/// <summary>A color in the hue, saturation and lightness model, with straight alpha.</summary>
/// <param name="H">The hue in degrees, from 0 (inclusive) to 360 (exclusive).</param>
/// <param name="S">The saturation, from 0 to 1.</param>
/// <param name="L">The lightness, from 0 to 1.</param>
/// <param name="A">The opacity, from 0 to 1.</param>
public readonly record struct HslColor(double H, double S, double L, double A = 1)
{
    /// <summary>Returns the color with every component brought into its range; non-finite components become zero.</summary>
    /// <returns>The clamped color, with the hue wrapped into [0, 360).</returns>
    public HslColor Clamp() => new(ColorModel.WrapHue(H), ColorModel.Unit(S), ColorModel.Unit(L), ColorModel.Unit(A));
}

/// <summary>
/// The color arithmetic behind color pickers, independent of how they are drawn: conversions between RGB
/// (<see cref="Color" />), <see cref="HsvColor" /> and <see cref="HslColor" />, hexadecimal parsing and formatting with
/// alpha, clamping and contrast. Conversions to RGB round each channel to the nearest byte.
/// </summary>
public static class ColorModel
{
    /// <summary>Converts an RGB color to hue, saturation and value; gray colors get hue zero.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The equivalent HSV color.</returns>
    public static HsvColor ToHsv(Color color)
    {
        var (r, g, b) = (color.R / 255.0, color.G / 255.0, color.B / 255.0);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        return new HsvColor(Hue(r, g, b, max, delta), max == 0 ? 0 : delta / max, max, color.A / 255.0);
    }

    /// <summary>Converts a hue, saturation and value color to RGB, clamping its components first.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The nearest RGB color.</returns>
    public static Color FromHsv(HsvColor color)
    {
        var c = color.Clamp();
        var chroma = c.V * c.S;
        var (r, g, b) = Sector(c.H, chroma);
        var m = c.V - chroma;
        return new Color(Byte(r + m), Byte(g + m), Byte(b + m), Byte(c.A));
    }

    /// <summary>Converts an RGB color to hue, saturation and lightness; gray colors get hue zero.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The equivalent HSL color.</returns>
    public static HslColor ToHsl(Color color)
    {
        var (r, g, b) = (color.R / 255.0, color.G / 255.0, color.B / 255.0);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var lightness = (max + min) / 2;
        var saturation = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * lightness - 1));
        return new HslColor(Hue(r, g, b, max, delta), Unit(saturation), lightness, color.A / 255.0);
    }

    /// <summary>Converts a hue, saturation and lightness color to RGB, clamping its components first.</summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The nearest RGB color.</returns>
    public static Color FromHsl(HslColor color) => FromHsv(ToHsv(color));

    /// <summary>Converts between the two cylindrical models without going through bytes, so the hue is kept exactly.</summary>
    /// <param name="color">The HSL color.</param>
    /// <returns>The equivalent HSV color.</returns>
    public static HsvColor ToHsv(HslColor color)
    {
        var c = color.Clamp();
        var value = c.L + c.S * Math.Min(c.L, 1 - c.L);
        return new HsvColor(c.H, value == 0 ? 0 : 2 * (1 - c.L / value), value, c.A);
    }

    /// <summary>Converts between the two cylindrical models without going through bytes, so the hue is kept exactly.</summary>
    /// <param name="color">The HSV color.</param>
    /// <returns>The equivalent HSL color.</returns>
    public static HslColor ToHsl(HsvColor color)
    {
        var c = color.Clamp();
        var lightness = c.V * (1 - c.S / 2);
        var saturation = lightness is 0 or 1 ? 0 : (c.V - lightness) / Math.Min(lightness, 1 - lightness);
        return new HslColor(c.H, Unit(saturation), lightness, c.A);
    }

    /// <summary>
    /// Parses a hexadecimal color: <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>, with or without the
    /// leading <c>#</c>, ignoring surrounding white space and case.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="color">The parsed color, or transparent black when parsing fails.</param>
    /// <returns>true if the text is a hexadecimal color.</returns>
    public static bool TryParseHex(string? text, out Color color)
    {
        color = Color.Transparent;
        if (text is null) return false;
        var span = text.AsSpan().Trim();
        if (span.StartsWith("#")) span = span[1..];
        if (span.Length is not (3 or 4 or 6 or 8)) return false;
        foreach (var c in span) if (!char.IsAsciiHexDigit(c)) return false;
        if (span.Length <= 4)
        {
            Span<char> expanded = stackalloc char[span.Length * 2];
            for (var i = 0; i < span.Length; i++) expanded[2 * i] = expanded[2 * i + 1] = span[i];
            return TryParseHex(new string(expanded), out color);
        }
        var value = uint.Parse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        color = span.Length == 6 ? Color.FromRgba((value << 8) | 0xff) : Color.FromRgba(value);
        return true;
    }

    /// <summary>Parses a hexadecimal color as <see cref="TryParseHex" /> does.</summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException">The text is not a hexadecimal color.</exception>
    public static Color ParseHex(string text) =>
        TryParseHex(text, out var color) ? color : throw new FormatException($"'{text}' is not a hexadecimal color.");

    /// <summary>Formats a color as lowercase hexadecimal <c>#rrggbb</c>, or <c>#rrggbbaa</c> when it is translucent.</summary>
    /// <param name="color">The color to format.</param>
    /// <param name="alpha">true to always include the alpha byte, false to never include it, null to include it only when it is not opaque.</param>
    /// <returns>The hexadecimal text.</returns>
    public static string ToHex(Color color, bool? alpha = null)
    {
        var rgb = string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}");
        return alpha ?? color.A != 255 ? rgb + color.A.ToString("x2", CultureInfo.InvariantCulture) : rgb;
    }

    /// <summary>Formats a color as a CSS <c>rgb()</c> or <c>rgba()</c> value, usable in NSS declarations.</summary>
    /// <param name="color">The color to format.</param>
    /// <returns>The CSS text.</returns>
    public static string ToCss(Color color) => color.A == 255 ? ToHex(color, false)
        : string.Create(CultureInfo.InvariantCulture, $"rgba({color.R}, {color.G}, {color.B}, {Math.Round(color.A / 255.0, 3)})");

    /// <summary>Returns the color with another opacity.</summary>
    /// <param name="color">The color.</param>
    /// <param name="alpha">The opacity, from 0 to 1; it is clamped.</param>
    /// <returns>The color with the new alpha byte.</returns>
    public static Color WithAlpha(Color color, double alpha) => color with { A = Byte(Unit(alpha)), IsCurrentColor = false };

    /// <summary>Computes the relative luminance of a color as defined by WCAG, ignoring alpha.</summary>
    /// <param name="color">The color.</param>
    /// <returns>The luminance, from 0 for black to 1 for white.</returns>
    public static double Luminance(Color color)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    /// <summary>Computes the WCAG contrast ratio between two colors, ignoring alpha.</summary>
    /// <param name="first">The first color.</param>
    /// <param name="second">The second color.</param>
    /// <returns>The ratio, from 1 to 21.</returns>
    public static double Contrast(Color first, Color second)
    {
        var (a, b) = (Luminance(first), Luminance(second));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Clamps a number into [0, 1]; non-finite numbers become zero.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The clamped number.</returns>
    public static double Unit(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;

    /// <summary>Wraps a hue into [0, 360); non-finite hues become zero.</summary>
    /// <param name="hue">The hue in degrees.</param>
    /// <returns>The wrapped hue.</returns>
    public static double WrapHue(double hue)
    {
        if (!double.IsFinite(hue)) return 0;
        var wrapped = hue % 360;
        if (wrapped < 0) wrapped += 360;
        return wrapped >= 360 ? 0 : wrapped;
    }

    private static double Hue(double r, double g, double b, double max, double delta)
    {
        if (delta == 0) return 0;
        var hue = max == r ? 60 * ((g - b) / delta % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        return WrapHue(hue);
    }

    private static (double R, double G, double B) Sector(double hue, double chroma)
    {
        var h = hue / 60;
        var x = chroma * (1 - Math.Abs(h % 2 - 1));
        return (int)h switch
        {
            0 => (chroma, x, 0),
            1 => (x, chroma, 0),
            2 => (0, chroma, x),
            3 => (0, x, chroma),
            4 => (x, 0, chroma),
            _ => (chroma, 0, x),
        };
    }

    private static byte Byte(double unit) => (byte)Math.Round(Unit(unit) * 255, MidpointRounding.AwayFromZero);
}
