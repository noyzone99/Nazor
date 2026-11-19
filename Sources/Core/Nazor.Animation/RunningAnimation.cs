using Nazor.Style;

namespace Nazor.Animation;

/// <summary>
/// A keyframe animation of one owner (CSS Animations 1): its definition, its keyframes resolved for the owner and its
/// timeline, which starts when the animation is applied plus the delay.
/// </summary>
public sealed class RunningAnimation
{
    /// <summary>Copies the timeline and keyframe dictionaries for immutable sampling on a rendering worker.</summary>
    /// <returns>A detached animation; changing the original definition or pause state does not change the copy.</returns>
    public RunningAnimation Snapshot() => new(Definition,
        Keyframes.Select(frame => new ResolvedKeyframe(frame.Offset, frame.Easing, new Dictionary<StyleProperty,object>(frame.Values))).ToArray(), Trigger)
        { PausedElapsed = PausedElapsed };

    internal RunningAnimation(AnimationDefinition definition, IReadOnlyList<ResolvedKeyframe> keyframes, TimeSpan trigger)
    {
        Definition = definition;
        Keyframes = keyframes;
        Trigger = trigger;
        if (definition.Paused) PausedElapsed = -definition.Delay;
    }

    /// <summary>Gets the animation definition currently applied to this element.</summary>
    public AnimationDefinition Definition { get; internal set; }

    /// <summary>Gets the ordered keyframes used to evaluate this animation.</summary>
    public IReadOnlyList<ResolvedKeyframe> Keyframes { get; internal set; }

    /// <summary>When the animation was applied; the delay counts from here.</summary>
    public TimeSpan Trigger { get; internal set; }

    internal TimeSpan? PausedElapsed { get; set; }

    /// <summary>Time since the start of the first iteration: negative during the delay.</summary>
    /// <param name="now">The current animation clock time.</param>
    /// <returns>The resulting TimeSpan value.</returns>
    public TimeSpan Elapsed(TimeSpan now) => PausedElapsed ?? now - Trigger - Definition.Delay;

    private double ActiveMilliseconds =>
        Definition.Duration <= TimeSpan.Zero ? 0 : double.IsPositiveInfinity(Definition.IterationCount) ? double.PositiveInfinity
            : Definition.Duration.TotalMilliseconds * Definition.IterationCount;

    /// <summary>
    /// The animation still moves at <paramref name="now" /> (or will, after its delay): frames are needed.
    /// </summary>
    /// <param name="now">The current animation clock time.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool IsMoving(TimeSpan now) => PausedElapsed is null && Elapsed(now).TotalMilliseconds < ActiveMilliseconds;

    /// <summary>
    /// The progress in the current iteration at <paramref name="now" />, after the direction (0 is the first
    /// keyframe), or null when the animation shows nothing: before the start without a backwards fill, after the end
    /// without a forwards fill. With <paramref name="instant" /> (reduced motion) only the final values of a forwards
    /// fill show.
    /// </summary>
    /// <param name="now">The current animation clock time.</param>
    /// <param name="instant">The instant (bool).</param>
    /// <returns>The resulting double? value.</returns>
    public double? Progress(TimeSpan now, bool instant)
    {
        var fill = Definition.FillMode;
        var forwards = fill is AnimationFillMode.Forwards or AnimationFillMode.Both;
        var backwards = fill is AnimationFillMode.Backwards or AnimationFillMode.Both;
        if (instant) return forwards && !double.IsPositiveInfinity(Definition.IterationCount) ? EndProgress() : null;
        var elapsed = Elapsed(now).TotalMilliseconds;
        if (elapsed < 0) return backwards ? Directed(0, 0) : null;
        if (elapsed >= ActiveMilliseconds) return forwards ? EndProgress() : null;
        var iterations = elapsed / Definition.Duration.TotalMilliseconds;
        var iteration = Math.Floor(iterations);
        return Directed(iterations - iteration, iteration);
    }

    private double EndProgress()
    {
        var count = Definition.IterationCount;
        var whole = Math.Floor(count);
        return count > 0 && count == whole ? Directed(1, count - 1) : Directed(count - whole, whole);
    }

    private double Directed(double progress, double iteration)
    {
        var odd = iteration % 2 == 1;
        return Definition.Direction switch
        {
            AnimationDirection.Reverse => 1 - progress,
            AnimationDirection.Alternate => odd ? 1 - progress : progress,
            AnimationDirection.AlternateReverse => odd ? progress : 1 - progress,
            _ => progress,
        };
    }

    /// <summary>The keyframes set this property.</summary>
    /// <param name="property">The style property to read or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Animates(StyleProperty property) => Keyframes.Any(k => k.Values.ContainsKey(property));

    /// <summary>
    /// The value of <paramref name="property" /> at an iteration progress, or null when no keyframe sets it. Missing
    /// 0% and 100% keyframes take <paramref name="underlying" />, the value without the animation; each interval
    /// eases with the timing function of the keyframe where it starts.
    /// </summary>
    /// <param name="property">The style property to read or update.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <param name="underlying">The underlying (object).</param>
    /// <returns>The resulting object? value.</returns>
    public object? ValueAt(StyleProperty property, double progress, object underlying)
    {
        var frames = new List<(double Offset, EasingFunction? Easing, object Value)>();
        foreach (var keyframe in Keyframes)
            if (keyframe.Values.TryGetValue(property, out var value)) frames.Add((keyframe.Offset, keyframe.Easing, value));
        if (frames.Count == 0) return null;
        if (frames[0].Offset > 0) frames.Insert(0, (0, null, underlying));
        if (frames[^1].Offset < 1) frames.Add((1, null, underlying));

        var start = 0;
        for (var i = 0; i < frames.Count; i++)
            if (frames[i].Offset <= progress) start = i;
        if (start == frames.Count - 1) return frames[start].Value;
        var (from, to) = (frames[start], frames[start + 1]);
        var local = to.Offset > from.Offset ? (progress - from.Offset) / (to.Offset - from.Offset) : 1;
        var eased = Easing.Evaluate(from.Easing ?? Definition.Easing, local);
        return Interpolation.CanInterpolate(from.Value, to.Value) ? Interpolation.Lerp(from.Value, to.Value, eased) : eased < 0.5 ? from.Value : to.Value;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"animation {Definition}";
}
