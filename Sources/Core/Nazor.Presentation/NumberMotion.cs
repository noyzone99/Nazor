using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private readonly Dictionary<ElementNode, NumberMotionState> numberPresentations = [];
    private bool HasNumberMotion => !Transitions.Suspended && !Transitions.ReducedMotion && !Styles.Preferences.ReducedMotion &&
        numberPresentations.Any(pair => pair.Value.Running && !pair.Key.IsRemoved && !pair.Value.Source.IsDisposed);

    private readonly record struct NumberPolicy(NumberMotionKind Kind, TimeSpan Duration, double Decay, double Tolerance, double Boundary);

    private sealed class NumberMotionState(NumberPresentation source, TimeSpan now)
    {
        public NumberPresentation Source { get; } = source;
        public NumberPolicy Policy { get; set; }
        public double From { get; set; } = source.Value;
        public double To { get; set; } = source.Value;
        public TimeSpan Started { get; set; } = now;
        public TimeSpan Duration { get; set; }
        public bool Running { get; set; }

        public double Sample(TimeSpan time)
        {
            if (!Running) return To;
            var elapsed = time > Started ? time - Started : TimeSpan.Zero;
            double progress;
            if (Policy.Kind == NumberMotionKind.Exponential)
            {
                var frames = elapsed.TotalSeconds * 60;
                var remaining = Math.Pow(1 - Policy.Decay, frames);
                var previousDistance = Math.Abs(To - From) * Math.Pow(1 - Policy.Decay, Math.Max(0, frames - 1));
                if (frames > 0 && previousDistance < Policy.Tolerance) { Running = false; return To; }
                progress = 1 - remaining;
            }
            else
            {
                progress = Duration == TimeSpan.Zero ? 1 : Math.Clamp(elapsed / Duration, 0, 1);
            }
            var value = From * (1 - progress) + To * progress;
            if (!double.IsFinite(value) || progress >= 1 || value == To || Policy.Kind == NumberMotionKind.Exponential && elapsed.TotalSeconds >= 1d / 60 && value == From)
            {
                Running = false;
                return To;
            }
            return value;
        }
    }

    private void AttachNumberPresentation(ElementNode element)
    {
        if (element.Attributes.GetValueOrDefault("number-presentation") is NumberPresentation { IsDisposed: false } source)
        {
            if (!numberPresentations.TryGetValue(element, out var state) || !ReferenceEquals(state.Source, source))
                numberPresentations[element] = new(source, Transitions.Clock.Now);
        }
        else numberPresentations.Remove(element);
    }

    private bool TickNumberPresentations()
    {
        var changed = false;
        var now = Transitions.Clock.Now;
        foreach (var (element, state) in numberPresentations.ToArray())
        {
            var source = state.Source;
            if (element.IsRemoved || source.IsDisposed)
            {
                numberPresentations.Remove(element);
                continue;
            }
            var style = GetStyle(element);
            var visible = IsRendered(element);
            var immediate = !visible || Transitions.ReducedMotion || Transitions.Suspended || Styles.Preferences.ReducedMotion;
            var policy = new NumberPolicy(immediate ? NumberMotionKind.None : style.Get(StyleProperties.NumberMotion),
                style.Get(StyleProperties.NumberMotionDuration), style.Get(StyleProperties.NumberMotionDecay),
                style.Get(StyleProperties.NumberMotionTolerance), style.Get(StyleProperties.NumberBoundaryTolerance));
            var value = Math.Clamp(state.Sample(now), source.Minimum, source.Maximum);
            if (state.To != source.Target || state.Policy != policy)
            {
                state.From = value;
                state.To = source.Target;
                state.Started = now;
                state.Policy = policy;
                state.Running = value != source.Target && (policy.Kind switch
                {
                    NumberMotionKind.Linear => policy.Duration > TimeSpan.Zero,
                    NumberMotionKind.LinearIncrease => policy.Duration > TimeSpan.Zero && source.Target > value,
                    NumberMotionKind.Exponential => policy.Decay is > 0 and < 1,
                    _ => false,
                });
                if (state.Running && policy.Kind is NumberMotionKind.Linear or NumberMotionKind.LinearIncrease)
                {
                    var ticks = policy.Duration.Ticks * Math.Abs(source.Target - value);
                    if (!double.IsFinite(ticks) || ticks > TimeSpan.FromMilliseconds(uint.MaxValue - 1).Ticks)
                        throw new ArgumentOutOfRangeException(nameof(NumberPresentation.Target), "Number motion exceeds the supported duration range.");
                    state.Duration = TimeSpan.FromTicks((long)Math.Round(ticks));
                    state.Running = state.Duration > TimeSpan.Zero;
                }
            }
            if (!state.Running) value = source.Target;
            var boundary = Math.Min(policy.Boundary, (source.Maximum - source.Minimum) / 2);
            changed |= source.Publish(value, value <= source.Minimum + boundary, value >= source.Maximum - boundary);
            if (!visible) numberPresentations.Remove(element);
        }
        return changed;
    }
}
