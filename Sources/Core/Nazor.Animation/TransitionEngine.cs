using System.Text;
using Nazor.Style;

namespace Nazor.Animation;

/// <summary>One property of one owner moving from <see cref="From" /> to <see cref="To" />.</summary>
public sealed class RunningTransition
{
    internal RunningTransition(StyleProperty property, object from, object to, TimeSpan start, TimeSpan duration, EasingFunction easing,
        object reversingAdjustedStartValue, double reversingShorteningFactor)
    {
        Property = property;
        From = from;
        To = to;
        Start = start;
        Duration = duration;
        EasingFunction = easing;
        ReversingAdjustedStartValue = reversingAdjustedStartValue;
        ReversingShorteningFactor = reversingShorteningFactor;
    }

    /// <summary>Gets the style property targeted by this value or declaration.</summary>
    public StyleProperty Property { get; }

    /// <summary>Gets the starting value of the transition.</summary>
    public object From { get; }

    /// <summary>Gets the destination value of the transition.</summary>
    public object To { get; }

    /// <summary>When the value starts to move (the style change plus the delay).</summary>
    public TimeSpan Start { get; }

    /// <summary>Gets the effective duration of the transition.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the easing function applied to transition progress.</summary>
    public EasingFunction EasingFunction { get; }

    /// <summary>The value a reversal must reach to count as going back (CSS Transitions §3).</summary>
    public object ReversingAdjustedStartValue { get; }

    /// <summary>Gets the shortening factor used when reversing an interrupted transition.</summary>
    public double ReversingShorteningFactor { get; }

    /// <summary>Gets the clock time at which the transition completes.</summary>
    public TimeSpan End => Start + Duration;

    /// <summary>Eased progress at <paramref name="time" />: 0 during the delay, 1 at the end.</summary>
    /// <param name="time">The animation clock time at which to evaluate the value.</param>
    /// <returns>The resulting double value.</returns>
    public double Progress(TimeSpan time)
    {
        if (time < Start) return 0;
        if (time >= End || Duration <= TimeSpan.Zero) return 1;
        return Easing.Evaluate(EasingFunction, (time - Start) / Duration);
    }

    /// <summary>Computes the transition value at the specified clock time using its easing function.</summary>
    /// <param name="time">The animation clock time at which to evaluate the value.</param>
    /// <returns>The interpolated property value at the requested clock time.</returns>
    public object ValueAt(TimeSpan time) => Interpolation.Lerp(From, To, Progress(time));

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Property.Name}: {From} -> {To} ({Duration.TotalMilliseconds:0}ms)";
}

/// <summary>
/// CSS-like transitions and keyframe animations of animatable style values, keyed by owner (an element) and property.
/// <see cref="StyleChanged" /> receives every change of an owner's computed style and starts, retargets or cancels
/// transitions; <see cref="GetValue{T}" /> gives the value to draw, so the computed style stays untouched (computed
/// style → transitions → effective value).
/// </summary>
/// <remarks>A transition starts from the value visible at the moment of the change: changing the destination midway
/// continues from there without jumps, and going back to where a transition started takes proportionally less time
/// (the CSS "reversing shortening factor"). Values are sampled at <see cref="FrameTime" />, fixed by
/// <see cref="Tick" /> once per frame.</remarks>
public sealed class TransitionEngine
{
    private readonly Dictionary<object, Dictionary<StyleProperty, RunningTransition>> running = new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<object, List<RunningAnimation>> animations = new(ReferenceEqualityComparer.Instance);
    private bool reducedMotion;
    private bool suspended;

    /// <summary>Initializes a new instance of TransitionEngine.</summary>
    /// <param name="clock">The clock used to measure animation time.</param>
    public TransitionEngine(IAnimationClock clock) => Clock = clock;

    /// <summary>Gets the clock used to evaluate animation progress.</summary>
    public IAnimationClock Clock { get; }

    /// <summary>The time values are sampled at; set by <see cref="Tick" />.</summary>
    public TimeSpan FrameTime { get; private set; }

    /// <summary>
    /// Some value moves and needs frames: a transition, or an animation that is not paused or finished.
    /// </summary>
    public bool IsRunning => running.Count > 0 || (!reducedMotion && animations.Values.Any(list => list.Any(a => a.IsMoving(Clock.Now))));

    /// <summary>Some owner has keyframe animations, running, paused or finished with a fill.</summary>
    public bool HasAnimations => animations.Count > 0;

    /// <summary>Whether all moving style animations can be represented by transform, transform-origin and opacity snapshots.</summary>
    public bool IsCompositionOnly => !Instant && IsRunning &&
        running.Values.All(properties => properties.Keys.All(IsCompositionProperty)) &&
        animations.Values.All(list => list.All(animation => !animation.IsMoving(Clock.Now) ||
            animation.Keyframes.All(frame => frame.Values.Keys.All(IsCompositionProperty))));

    private static bool IsCompositionProperty(StyleProperty property) => property == StyleProperties.Transform ||
        property == StyleProperties.TransformOrigin || property == StyleProperties.Opacity;

    /// <summary>Number of running transitions (diagnostics and tests).</summary>
    public int Count => running.Values.Sum(p => p.Count);

    /// <summary>
    /// The user prefers reduced motion: values change at once, running transitions end, keyframe animations do not
    /// run and show only the final values of a forwards fill.
    /// </summary>
    public bool ReducedMotion
    {
        get => reducedMotion;
        set
        {
            reducedMotion = value;
            if (value) FinishAll();
        }
    }

    /// <summary>The surface is not visible (for example a minimized window): nothing animates meanwhile.</summary>
    public bool Suspended
    {
        get => suspended;
        set
        {
            suspended = value;
            if (value) FinishAll();
        }
    }

    private bool Instant => reducedMotion || suspended;

    /// <summary>Starts, retargets or cancels the transitions of an owner whose computed style changed.</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="before">The before (ComputedStyle).</param>
    /// <param name="after">The after (ComputedStyle).</param>
    public void StyleChanged(object owner, ComputedStyle before, ComputedStyle after)
    {
        var now = Clock.Now;
        var definitions = after.Get(StyleProperties.Transition).Items;
        if (definitions.Count == 0 && !running.ContainsKey(owner)) return;
        foreach (var property in StyleProperties.All)
        {
            if (!property.Animatable) continue;
            var target = after.Get(property);
            var definition = Match(definitions, property);
            var active = running.TryGetValue(owner, out var properties) && properties.TryGetValue(property, out var transition) ? transition : null;
            if (active is not null)
            {

                if (Equals(active.To, target)) continue;
                var current = RemovedAnimationValue(owner, property, active.ValueAt(now), now, after);
                Remove(owner, property);
                if (Instant || definition is null || definition.Duration + definition.Delay <= TimeSpan.Zero || Equals(current, target) ||
                    !Interpolation.CanInterpolate(current, target)) continue;
                if (Equals(active.ReversingAdjustedStartValue, target))
                {

                    var factor = Math.Clamp(Math.Abs(active.Progress(now) * active.ReversingShorteningFactor + 1 - active.ReversingShorteningFactor), 0, 1);
                    var delay = definition.Delay < TimeSpan.Zero ? definition.Delay * factor : definition.Delay;
                    Add(owner, new RunningTransition(property, current, target, now + delay, definition.Duration * factor, definition.Easing, active.To, factor));
                }
                else
                {
                    Add(owner, new RunningTransition(property, current, target, now + definition.Delay, definition.Duration, definition.Easing, current, 1));
                }
                continue;
            }
            var from = RemovedAnimationValue(owner, property, before.Get(property), now, after);
            if (Instant || definition is null || Equals(from, target) || definition.Duration + definition.Delay <= TimeSpan.Zero) continue;
            if (!Interpolation.CanInterpolate(from, target)) continue;
            Add(owner, new RunningTransition(property, from, target, now + definition.Delay, definition.Duration, definition.Easing, from, 1));
        }
    }

    private object RemovedAnimationValue(object owner, StyleProperty property, object underlying, TimeSpan time, ComputedStyle after)
    {
        if (!animations.TryGetValue(owner, out var list)) return underlying;
        var retained = after.Get(StyleProperties.Animation).Items;
        for (var index = list.Count - 1; index >= 0; index--)
            if (!retained.Any(definition => definition.Name == list[index].Definition.Name) &&
                list[index].Progress(time, reducedMotion) is { } progress && list[index].ValueAt(property, progress, underlying) is { } value) return value;
        return underlying;
    }

    /// <summary>
    /// Starts a transition from an explicitly sampled value using a theme definition. Replaces the owner's transition
    /// for this property; the caller supplies the currently visible value when retargeting. Instant or zero-duration
    /// requests cancel that transition and return false. Keyframe animations retain their normal precedence.
    /// </summary>
    /// <param name="owner">The object whose property changes.</param>
    /// <param name="property">The animatable property.</param>
    /// <param name="from">The explicitly sampled starting value.</param>
    /// <param name="to">The destination, also supplied as the computed fallback after completion.</param>
    /// <param name="definition">The duration, delay and easing to use.</param>
    /// <typeparam name="T">The property's value type.</typeparam>
    /// <returns>Whether an interpolated transition started.</returns>
    /// <exception cref="ArgumentException">The property cannot animate or the values cannot interpolate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Timing exceeds the supported range or easing contains non-finite values or x control points outside zero to one.</exception>
    public bool Start<T>(object owner, StyleProperty<T> property, T from, T to, TransitionDefinition definition) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(definition);
        if (!property.Animatable || !Interpolation.CanInterpolate(from, to))
            throw new ArgumentException("The property and values must support interpolation.", nameof(property));
        if (definition.Duration < TimeSpan.Zero || definition.Delay < TimeSpan.Zero ||
            definition.Duration.TotalMilliseconds + definition.Delay.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(definition));
        var easing = definition.Easing;
        if (easing is null || !double.IsFinite(easing.X1) || !double.IsFinite(easing.Y1) || !double.IsFinite(easing.X2) || !double.IsFinite(easing.Y2) ||
            easing.X1 is < 0 or > 1 || easing.X2 is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(definition));
        Remove(owner, property);
        if (Instant || definition.Duration == TimeSpan.Zero || Equals(from, to)) return false;
        Add(owner, new RunningTransition(property, from, to, Clock.Now + definition.Delay, definition.Duration, definition.Easing, from, 1));
        return true;
    }

    /// <summary>
    /// The value to draw at <see cref="FrameTime" />: a keyframe animation's (the last of the list that sets the
    /// property), otherwise the transition's, otherwise the computed one.
    /// </summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="property">The style property to read or update.</param>
    /// <param name="computed">The computed (T).</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The resulting T value.</returns>
    public T GetValue<T>(object owner, StyleProperty<T> property, T computed) where T : notnull
    {
        var value = running.TryGetValue(owner, out var properties) && properties.TryGetValue(property, out var transition)
            ? (T)transition.ValueAt(FrameTime)
            : computed;
        if (!property.Animatable || !animations.TryGetValue(owner, out var list)) return value;
        for (var i = list.Count - 1; i >= 0; i--)
            if (list[i].Progress(FrameTime, reducedMotion) is { } progress && list[i].ValueAt(property, progress, value) is T animated) return animated;
        return value;
    }

    /// <summary>
    /// Starts, updates or cancels the keyframe animations of an owner whose style was computed, the first time too.
    /// An animation keeps its timeline while its name stays in the list (CSS Animations §4); <c>paused</c> freezes
    /// it. <paramref name="resolve" /> gives the keyframes of a name for the owner, null when no rule has it.
    /// </summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="style">The style (ComputedStyle).</param>
    /// <param name="resolve">The resolve (Func&lt;string, IReadOnlyList&lt;ResolvedKeyframe&gt;?&gt;).</param>
    public void UpdateAnimations(object owner, ComputedStyle style, Func<string, IReadOnlyList<ResolvedKeyframe>?> resolve)
    {
        var definitions = style.Get(StyleProperties.Animation).Items;
        animations.TryGetValue(owner, out var current);
        if (definitions.Count == 0)
        {
            if (current is not null) animations.Remove(owner);
            return;
        }
        var now = Clock.Now;
        var available = current is null ? [] : new List<RunningAnimation>(current);
        var next = new List<RunningAnimation>();
        foreach (var definition in definitions)
        {
            if (definition.Name is not { } name || resolve(name) is not { } keyframes) continue;
            var existing = available.Find(a => a.Definition.Name == name);
            if (existing is null)
            {
                next.Add(new RunningAnimation(definition, keyframes, now));
                continue;
            }
            available.Remove(existing);
            if (definition.Paused && existing.PausedElapsed is null) existing.PausedElapsed = existing.Elapsed(now);
            else if (!definition.Paused && existing.PausedElapsed is { } frozen)
            {
                existing.Trigger = now - frozen - definition.Delay;
                existing.PausedElapsed = null;
            }
            existing.Definition = definition;
            existing.Keyframes = keyframes;
            next.Add(existing);
        }
        if (next.Count == 0) animations.Remove(owner);
        else animations[owner] = next;
    }

    /// <summary>The keyframe animations of an owner, in the order of its list.</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <returns>The resulting IReadOnlyList&lt;RunningAnimation&gt; value.</returns>
    public IReadOnlyList<RunningAnimation> GetAnimations(object owner) => animations.TryGetValue(owner, out var list) ? list : [];

    /// <summary>The owner has keyframe animations.</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool HasAnimationsOn(object owner) => animations.ContainsKey(owner);

    /// <summary>A transition or an animation moves a property with these effects, for any owner.</summary>
    /// <param name="effects">The effects (StyleEffects).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool IsAnimating(StyleEffects effects)
    {
        foreach (var properties in running.Values)
            foreach (var property in properties.Keys)
                if ((property.Effects & effects) != 0) return true;
        foreach (var owner in animations.Keys)
            if (AnimatesEffects(owner, effects)) return true;
        return false;
    }

    /// <summary>
    /// A transition or an animation of the owner moves a property with these effects (for example
    /// <see cref="StyleEffects.Layout" />).
    /// </summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="effects">The effects (StyleEffects).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool IsAnimating(object owner, StyleEffects effects)
    {
        if (running.TryGetValue(owner, out var properties))
            foreach (var property in properties.Keys)
                if ((property.Effects & effects) != 0) return true;
        return AnimatesEffects(owner, effects);
    }

    private bool AnimatesEffects(object owner, StyleEffects effects)
    {
        if (!animations.TryGetValue(owner, out var list)) return false;
        foreach (var animation in list)
            for (var i = 0; i < animation.Keyframes.Count; i++)
                foreach (var property in animation.Keyframes[i].Values.Keys)
                    if ((property.Effects & effects) != 0) return true;
        return false;
    }

    /// <summary>Finds the running transition for the specified owner and style property.</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="property">The style property to read or update.</param>
    /// <returns>The matching active transition, or null when none exists.</returns>
    public RunningTransition? Find(object owner, StyleProperty property) =>
        running.TryGetValue(owner, out var properties) ? properties.GetValueOrDefault(property) : null;

    /// <summary>
    /// Samples the clock for the next frame and drops the transitions that ended; true while some run.
    /// </summary>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Tick()
    {
        FrameTime = Clock.Now;
        List<object>? emptied = null;
        foreach (var (owner, properties) in running)
        {
            foreach (var property in properties.Where(p => FrameTime >= p.Value.End).Select(p => p.Key).ToList()) properties.Remove(property);
            if (properties.Count == 0) (emptied ??= []).Add(owner);
        }
        if (emptied is not null)
            foreach (var owner in emptied) running.Remove(owner);
        return IsRunning;
    }

    /// <summary>
    /// How long the next frame can wait: zero while a value is moving, the time to the first start while all
    /// transitions are still in their delay, null when nothing runs.
    /// </summary>
    public TimeSpan? TimeUntilNextFrame
    {
        get
        {
            var now = Clock.Now;
            TimeSpan? next = null;
            if (running.Count > 0)
            {
                var earliest = running.Values.SelectMany(p => p.Values).Min(t => t.Start);
                next = earliest <= now ? TimeSpan.Zero : earliest - now;
            }

            if (reducedMotion || suspended) return next;
            foreach (var animation in animations.Values.SelectMany(list => list))
            {
                if (!animation.IsMoving(now)) continue;
                var elapsed = animation.Elapsed(now);
                var wait = elapsed < TimeSpan.Zero ? -elapsed : TimeSpan.Zero;
                if (next is null || wait < next) next = wait;
            }
            return next;
        }
    }

    /// <summary>Cancels the transitions and animations of an owner (removed from the tree).</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    public void Remove(object owner)
    {
        running.Remove(owner);
        animations.Remove(owner);
    }

    /// <summary>
    /// Cancels the transitions and animations of the owners that no longer exist; returns how many owners were
    /// dropped.
    /// </summary>
    /// <param name="isGone">The is gone (Func&lt;object, bool&gt;).</param>
    /// <returns>The resulting int value.</returns>
    public int Prune(Func<object, bool> isGone)
    {
        var gone = running.Keys.Concat(animations.Keys).Distinct(ReferenceEqualityComparer.Instance).Where(isGone).ToList();
        foreach (var owner in gone!) Remove(owner!);
        return gone.Count;
    }

    /// <summary>Ends every transition: the computed values show.</summary>
    public void FinishAll() => running.Clear();

    /// <summary>Formats the active transitions for diagnostic inspection.</summary>
    /// <returns>A textual description of the active transitions.</returns>
    public string Describe()
    {
        var text = new StringBuilder();
        foreach (var (owner, properties) in running)
            foreach (var transition in properties.Values)
                text.Append(owner).Append(' ').Append(transition).Append('\n');
        foreach (var (owner, list) in animations)
            foreach (var animation in list)
                text.Append(owner).Append(' ').Append(animation).Append('\n');
        return text.ToString();
    }

    private void Add(object owner, RunningTransition transition)
    {
        if (!running.TryGetValue(owner, out var properties)) running.Add(owner, properties = []);
        properties[transition.Property] = transition;
    }

    private void Remove(object owner, StyleProperty property)
    {
        if (!running.TryGetValue(owner, out var properties)) return;
        properties.Remove(property);
        if (properties.Count == 0) running.Remove(owner);
    }

    private static TransitionDefinition? Match(IReadOnlyList<TransitionDefinition> definitions, StyleProperty property)
    {
        for (var i = definitions.Count - 1; i >= 0; i--)
        {
            var name = definitions[i].Property;
            if (name == "all" || name == property.Name) return definitions[i];
            if (StyleProperties.TryGetShorthand(name, out var shorthand) && shorthand.Longhands.Contains(property)) return definitions[i];
        }
        return null;
    }
}
