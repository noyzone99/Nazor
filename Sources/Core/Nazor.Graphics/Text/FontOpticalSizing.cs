namespace Nazor.Graphics.Text;

/// <summary>Controls automatic positioning of a variable font's optical-size axis.</summary>
public enum FontOpticalSizing : byte
{
    /// <summary>Converts logical pixels to typographic points, preserving native desktop text sizing.</summary>
    Native,

    /// <summary>Uses the logical font size directly, matching CSS optical sizing.</summary>
    Auto,

    /// <summary>Uses the font's default optical size unless explicitly overridden by a variation.</summary>
    None,
}
