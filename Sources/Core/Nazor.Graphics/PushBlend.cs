namespace Nazor.Graphics;

/// <summary>Begins an isolated transparent source group until Pop, then blends its complete contents with the destination.</summary>
/// <param name="Mode">The color blend formula; Normal explicitly isolates without color blending.</param>
/// <param name="Bounds">The group extent including overflowing content.</param>
public sealed record PushBlend(BlendMode Mode, Rect Bounds) : DrawCommand;
