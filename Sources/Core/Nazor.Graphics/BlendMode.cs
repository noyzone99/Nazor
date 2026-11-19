namespace Nazor.Graphics;

/// <summary>Color blending of an isolated source group with its destination.</summary>
public enum BlendMode : byte
{
    /// <summary>Uses source-over without color blending.</summary>
    Normal,
    /// <summary>Multiplies source and backdrop channels.</summary>
    Multiply,
    /// <summary>Multiplies complementary channels.</summary>
    Screen,
    /// <summary>Uses multiply or screen according to backdrop luminance.</summary>
    Overlay,
    /// <summary>Chooses the darker channel.</summary>
    Darken,
    /// <summary>Chooses the lighter channel.</summary>
    Lighten,
    /// <summary>Brightens the backdrop according to the source.</summary>
    ColorDodge,
    /// <summary>Darkens the backdrop according to the source.</summary>
    ColorBurn,
    /// <summary>Uses multiply or screen according to source luminance.</summary>
    HardLight,
    /// <summary>Applies a soft contrast adjustment.</summary>
    SoftLight,
    /// <summary>Uses the absolute channel difference.</summary>
    Difference,
    /// <summary>Uses a low-contrast channel difference.</summary>
    Exclusion,
    /// <summary>Uses source hue with backdrop saturation and luminosity.</summary>
    Hue,
    /// <summary>Uses source saturation with backdrop hue and luminosity.</summary>
    Saturation,
    /// <summary>Uses source hue and saturation with backdrop luminosity.</summary>
    Color,
    /// <summary>Uses source luminosity with backdrop hue and saturation.</summary>
    Luminosity,
}
