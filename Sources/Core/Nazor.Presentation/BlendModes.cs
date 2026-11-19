namespace Nazor.Presentation;

internal static class BlendModes
{
    internal static Nazor.Graphics.BlendMode Resolve(Nazor.Style.BlendMode mode) => mode switch
    {
        Nazor.Style.BlendMode.Normal => Nazor.Graphics.BlendMode.Normal,
        Nazor.Style.BlendMode.Multiply => Nazor.Graphics.BlendMode.Multiply,
        Nazor.Style.BlendMode.Screen => Nazor.Graphics.BlendMode.Screen,
        Nazor.Style.BlendMode.Overlay => Nazor.Graphics.BlendMode.Overlay,
        Nazor.Style.BlendMode.Darken => Nazor.Graphics.BlendMode.Darken,
        Nazor.Style.BlendMode.Lighten => Nazor.Graphics.BlendMode.Lighten,
        Nazor.Style.BlendMode.ColorDodge => Nazor.Graphics.BlendMode.ColorDodge,
        Nazor.Style.BlendMode.ColorBurn => Nazor.Graphics.BlendMode.ColorBurn,
        Nazor.Style.BlendMode.HardLight => Nazor.Graphics.BlendMode.HardLight,
        Nazor.Style.BlendMode.SoftLight => Nazor.Graphics.BlendMode.SoftLight,
        Nazor.Style.BlendMode.Difference => Nazor.Graphics.BlendMode.Difference,
        Nazor.Style.BlendMode.Exclusion => Nazor.Graphics.BlendMode.Exclusion,
        Nazor.Style.BlendMode.Hue => Nazor.Graphics.BlendMode.Hue,
        Nazor.Style.BlendMode.Saturation => Nazor.Graphics.BlendMode.Saturation,
        Nazor.Style.BlendMode.Color => Nazor.Graphics.BlendMode.Color,
        Nazor.Style.BlendMode.Luminosity => Nazor.Graphics.BlendMode.Luminosity,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
