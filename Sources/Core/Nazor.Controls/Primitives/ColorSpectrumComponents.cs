namespace Nazor.Controls.Primitives;

/// <summary>The horizontal and vertical HSV channels of a rectangular <see cref="ColorArea" />.</summary>
public enum ColorSpectrumComponents
{
    /// <summary>Saturation grows rightwards; value grows upwards. This preserves ColorArea's original plane.</summary>
    SaturationValue,
    /// <summary>Hue grows rightwards; saturation grows upwards. The plane is displayed at full value.</summary>
    HueSaturation,
    /// <summary>Hue grows rightwards; value grows upwards.</summary>
    HueValue,
    /// <summary>Saturation grows leftwards; hue grows downwards. The plane is displayed at full value.</summary>
    SaturationHue,
    /// <summary>Value grows leftwards; hue grows downwards.</summary>
    ValueHue,
    /// <summary>Value grows leftwards; saturation grows downwards.</summary>
    ValueSaturation,
}
