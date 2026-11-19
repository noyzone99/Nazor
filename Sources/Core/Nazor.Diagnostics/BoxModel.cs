using Nazor.Graphics;

namespace Nazor.Diagnostics;

/// <summary>
/// The boxes of an element as the box model of developer tools shows them, in logical units of the window, from the
/// margin box out to the content box in, with the widths of each ring. With a transform each box is the bounds of the
/// transformed one; scroll offsets are applied, as on the screen. <see cref="Physical" /> gives a box in pixels.
/// </summary>
/// <param name="MarginBox">The margin box (Rect).</param>
/// <param name="BorderBox">The border box (Rect).</param>
/// <param name="PaddingBox">The padding box (Rect).</param>
/// <param name="ContentBox">The content box (Rect).</param>
/// <param name="Margin">The margin (Thickness).</param>
/// <param name="Border">The border (Thickness).</param>
/// <param name="Padding">The padding (Thickness).</param>
/// <param name="Scale">The scale (float).</param>
public sealed record BoxModel(Rect MarginBox, Rect BorderBox, Rect PaddingBox, Rect ContentBox, Thickness Margin, Thickness Border, Thickness Padding,
    float Scale)
{
    /// <summary>A box of this model in physical pixels.</summary>
    /// <param name="box">The box (Rect).</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect Physical(Rect box) => new(box.X * Scale, box.Y * Scale, box.Width * Scale, box.Height * Scale);
}
