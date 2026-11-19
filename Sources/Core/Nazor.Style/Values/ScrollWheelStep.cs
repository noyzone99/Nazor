using System.Globalization;

namespace Nazor.Style;

/// <summary>The unit used for one wheel detent.</summary>
public enum ScrollWheelStepKind : byte
{
    /// <summary>Uses the backend's system wheel settings.</summary>
    Auto,
    /// <summary>Uses a logical length.</summary>
    Length,
    /// <summary>Uses a multiple of the scroll container's computed line height.</summary>
    Lines,
}

/// <summary>A wheel distance; line counts define a distance, never a snapping grid.</summary>
/// <param name="Kind">The distance unit.</param>
/// <param name="Length">The length when Kind is Length.</param>
/// <param name="Lines">The line count when Kind is Lines.</param>
public readonly record struct ScrollWheelStep(ScrollWheelStepKind Kind, Length Length, double Lines)
{
    /// <summary>Uses the backend's system wheel settings.</summary>
    public static readonly ScrollWheelStep Auto = new(ScrollWheelStepKind.Auto, Length.Zero, 0);

    /// <summary>Returns the NSS representation.</summary>
    public override string ToString() => Kind switch
    {
        ScrollWheelStepKind.Auto => "auto",
        ScrollWheelStepKind.Lines => Lines.ToString("0.###", CultureInfo.InvariantCulture) + " lines",
        _ => Length.ToString(),
    };
}
