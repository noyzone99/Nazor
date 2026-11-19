namespace Nazor.Style;

/// <summary>Used horizontal font measurements in logical pixels for resolving ch and ex lengths.</summary>
/// <param name="ZeroAdvance">The unshaped advance of the font used for U+0030, or null when unavailable.</param>
/// <param name="XHeight">The first available font's x-height, or null when unavailable.</param>
/// <remarks>Each available value must be finite and nonnegative. Unavailable metrics independently use 0.5em. Vertical writing is not implemented.</remarks>
public readonly record struct FontRelativeMetrics(double? ZeroAdvance = null, double? XHeight = null);
