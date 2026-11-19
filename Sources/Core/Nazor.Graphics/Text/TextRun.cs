namespace Nazor.Graphics.Text;

/// <summary>A styled UTF-16 segment in a shared line-breaking and selection layout.</summary>
/// <param name="Text">The segment's text; segments are concatenated in order.</param>
/// <param name="Style">Font and line metrics for this segment.</param>
/// <param name="BaselineShift">Logical distance above the shared baseline; negative values lower the segment.</param>
/// <param name="LeadingAdvance">Additional logical space before the first glyph, for inline box edges.</param>
/// <param name="TrailingAdvance">Additional logical space after the last glyph, for inline box edges.</param>
/// <param name="NoWrap">Whether spaces within the segment are excluded from soft line-breaking opportunities.</param>
/// <param name="TopInset">Additional line-box space above the segment for inline decoration.</param>
/// <param name="BottomInset">Additional line-box space below the segment for inline decoration.</param>
public sealed record TextRun(string Text, TextStyle Style, float BaselineShift = 0,
    float LeadingAdvance = 0, float TrailingAdvance = 0, bool NoWrap = false, float TopInset = 0, float BottomInset = 0);
