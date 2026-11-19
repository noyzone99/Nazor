namespace Nazor.Style.Syntax;

internal readonly record struct ColorMixValue(double R, double G, double B, double A)
{
    internal static ColorMixValue FromColor(Color value) => new(value.R / 255.0, value.G / 255.0, value.B / 255.0, value.A / 255.0);
    internal Color ToColor() => new(Quantize(R), Quantize(G), Quantize(B), Quantize(A));
    private static byte Quantize(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255, MidpointRounding.AwayFromZero);
}

internal static class OklabColorMix
{
    internal static ColorMixValue Mix(ColorMixValue first, ColorMixValue second, double firstPercent, double secondPercent)
    {
        var firstWeight = first.A * firstPercent;
        var secondWeight = second.A * secondPercent;
        var alphaWeight = firstWeight + secondWeight;
        if (alphaWeight == 0) return default;
        var a = ToOklab(first);
        var b = ToOklab(second);
        double Interpolate(double firstValue, double secondValue) => (firstValue * firstWeight + secondValue * secondWeight) / alphaWeight;
        var sum = firstPercent + secondPercent;
        var alpha = alphaWeight / sum * Math.Min(sum, 100) / 100;
        return ToSrgb(Interpolate(a.L, b.L), Interpolate(a.A, b.A), Interpolate(a.B, b.B), alpha);
    }

    private static (double L, double A, double B) ToOklab(ColorMixValue color)
    {
        var r = Linear(color.R);
        var g = Linear(color.G);
        var b = Linear(color.B);
        var x = 506752.0 / 1228815 * r + 87881.0 / 245763 * g + 12673.0 / 70218 * b;
        var y = 87098.0 / 409605 * r + 175762.0 / 245763 * g + 12673.0 / 175545 * b;
        var z = 7918.0 / 409605 * r + 87881.0 / 737289 * g + 1001167.0 / 1053270 * b;
        var l = Math.Cbrt(0.8190224379967030 * x + 0.3619062600528904 * y - 0.1288737815209879 * z);
        var m = Math.Cbrt(0.0329836539323885 * x + 0.9292868615863434 * y + 0.0361446663506424 * z);
        var s = Math.Cbrt(0.0481771893596242 * x + 0.2642395317527308 * y + 0.6335478284694309 * z);
        return (0.2104542683093140 * l + 0.7936177747023054 * m - 0.0040720430116193 * s,
            1.9779985324311684 * l - 2.4285922420485799 * m + 0.4505937096174110 * s,
            0.0259040424655478 * l + 0.7827717124575296 * m - 0.8086757549230774 * s);
    }

    private static ColorMixValue ToSrgb(double lightness, double a, double b, double alpha)
    {
        var l = Cube(lightness + 0.3963377773761749 * a + 0.2158037573099136 * b);
        var m = Cube(lightness - 0.1055613458156586 * a - 0.0638541728258133 * b);
        var s = Cube(lightness - 0.0894841775298119 * a - 1.2914855480194092 * b);
        var x = 1.2268798758459243 * l - 0.5578149944602171 * m + 0.2813910456659647 * s;
        var y = -0.0405757452148008 * l + 1.1122868032803170 * m - 0.0717110580655164 * s;
        var z = -0.0763729366746601 * l - 0.4214933324022432 * m + 1.5869240198367816 * s;
        return new ColorMixValue(Encoded(12831.0 / 3959 * x - 329.0 / 214 * y - 1974.0 / 3959 * z),
            Encoded(-851781.0 / 878810 * x + 1648619.0 / 878810 * y + 36519.0 / 878810 * z),
            Encoded(705.0 / 12673 * x - 2585.0 / 12673 * y + 705.0 / 667 * z), alpha);
    }

    private static double Cube(double value) => value * value * value;
    private static double Linear(double value)
    {
        var absolute = Math.Abs(value);
        return absolute <= 0.04045 ? value / 12.92 : Math.CopySign(Math.Pow((absolute + 0.055) / 1.055, 2.4), value);
    }
    private static double Encoded(double value)
    {
        var absolute = Math.Abs(value);
        var encoded = absolute > 0.0031308 ? Math.CopySign(1.055 * Math.Pow(absolute, 1 / 2.4) - 0.055, value) : 12.92 * value;
        return encoded;
    }
}
