using SkiaSharp;

namespace Nazor.Skia;

public sealed partial class SkiaRenderer
{
    private static SKBlendMode ToSk(Nazor.Graphics.BlendMode mode) => mode switch
    {
        Nazor.Graphics.BlendMode.Normal => SKBlendMode.SrcOver,
        Nazor.Graphics.BlendMode.Multiply => SKBlendMode.Multiply,
        Nazor.Graphics.BlendMode.Screen => SKBlendMode.Screen,
        Nazor.Graphics.BlendMode.Overlay => SKBlendMode.Overlay,
        Nazor.Graphics.BlendMode.Darken => SKBlendMode.Darken,
        Nazor.Graphics.BlendMode.Lighten => SKBlendMode.Lighten,
        Nazor.Graphics.BlendMode.ColorDodge => SKBlendMode.ColorDodge,
        Nazor.Graphics.BlendMode.ColorBurn => SKBlendMode.ColorBurn,
        Nazor.Graphics.BlendMode.HardLight => SKBlendMode.HardLight,
        Nazor.Graphics.BlendMode.SoftLight => SKBlendMode.SoftLight,
        Nazor.Graphics.BlendMode.Difference => SKBlendMode.Difference,
        Nazor.Graphics.BlendMode.Exclusion => SKBlendMode.Exclusion,
        Nazor.Graphics.BlendMode.Hue => SKBlendMode.Hue,
        Nazor.Graphics.BlendMode.Saturation => SKBlendMode.Saturation,
        Nazor.Graphics.BlendMode.Color => SKBlendMode.Color,
        Nazor.Graphics.BlendMode.Luminosity => SKBlendMode.Luminosity,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
