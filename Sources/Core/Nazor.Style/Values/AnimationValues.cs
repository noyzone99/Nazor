using System.Globalization;

namespace Nazor.Style;

/// <summary>Specifies playback order and whether successive animation iterations alternate.</summary>
public enum AnimationDirection : byte
{
    /// <summary>Plays every iteration from the first keyframe to the last.</summary>
    Normal,

    /// <summary>Plays every iteration from the last keyframe to the first.</summary>
    Reverse,

    /// <summary>Alternates direction, beginning with forward playback.</summary>
    Alternate,

    /// <summary>Alternates direction, beginning with reverse playback.</summary>
    AlternateReverse,
}

/// <summary>Specifies whether animation values apply before or after the active interval.</summary>
public enum AnimationFillMode : byte
{
    /// <summary>Applies animation values only during the active interval.</summary>
    None,

    /// <summary>Retains the ending animation value after the active interval.</summary>
    Forwards,

    /// <summary>Applies the starting animation value during the delay.</summary>
    Backwards,

    /// <summary>Applies both backwards and forwards fill behavior.</summary>
    Both,
}

/// <summary>
/// One entry of the <c>animation</c> list: the name of a <c>@keyframes</c> rule (null for <c>none</c>), duration,
/// timing function, delay (negative starts midway), iterations (<see cref="double.PositiveInfinity" /> for <c>infinite</c>),
/// direction, fill mode and play state.
/// </summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="Duration">The duration of the animation or transition.</param>
/// <param name="Easing">The easing (EasingFunction).</param>
/// <param name="Delay">The delay before the operation or animation starts.</param>
/// <param name="IterationCount">The iteration count (double).</param>
/// <param name="Direction">The direction (AnimationDirection).</param>
/// <param name="FillMode">The fill mode (AnimationFillMode).</param>
/// <param name="Paused">The paused (bool).</param>
public sealed record AnimationDefinition(string? Name, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay, double IterationCount,
    AnimationDirection Direction, AnimationFillMode FillMode, bool Paused)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var iterations = double.IsPositiveInfinity(IterationCount) ? "infinite" : IterationCount.ToString("0.###", CultureInfo.InvariantCulture);
        var direction = Direction switch
        {
            AnimationDirection.Reverse => "reverse",
            AnimationDirection.Alternate => "alternate",
            AnimationDirection.AlternateReverse => "alternate-reverse",
            _ => "normal",
        };
        return string.Create(CultureInfo.InvariantCulture,
            $"{Name ?? "none"} {Duration.TotalMilliseconds}ms {Easing} {Delay.TotalMilliseconds}ms {iterations} {direction} {FillMode.ToString().ToLowerInvariant()} {(Paused ? "paused" : "running")}");
    }
}
