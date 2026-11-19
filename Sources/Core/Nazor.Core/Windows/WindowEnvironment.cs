namespace Nazor;

/// <summary>Client-local occlusion in logical units; a floating keyboard need not touch an edge.</summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct WindowOcclusion(float X, float Y, float Width, float Height);

/// <summary>Safe insets not already excluded from the viewport, and separate keyboard occlusion.</summary>
/// <param name="SafeLeft">The safe left (float).</param>
/// <param name="SafeTop">The safe top (float).</param>
/// <param name="SafeRight">The safe right (float).</param>
/// <param name="SafeBottom">The safe bottom (float).</param>
/// <param name="KeyboardOcclusion">The keyboard occlusion (WindowOcclusion?).</param>
public readonly record struct WindowEnvironment(float SafeLeft, float SafeTop, float SafeRight, float SafeBottom,
    WindowOcclusion? KeyboardOcclusion = null);
