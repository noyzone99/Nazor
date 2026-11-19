namespace Nazor.Style;

/// <summary>The path followed by a ring vector glyph, selected by the NSS ring-shape property.</summary>
public enum RingShape : byte
{
    /// <summary>A circle centered in the content box; the default ring geometry.</summary>
    Circle,
    /// <summary>A clockwise rounded rectangle following the border box and its border-radius, starting on the top edge nearest its center.</summary>
    BorderBox,
}
