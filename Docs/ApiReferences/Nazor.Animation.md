# Nazor.Animation

Animation clocks, interpolation, keyframe animations, and style transitions.

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Detached composition sampling

`TransitionEngine.IsCompositionOnly` reports whether all moving properties are transform, transform-origin or opacity, with reduced motion/suspension disabled. It does not establish renderer or scroll eligibility; the scene decides that boundary.

`RunningAnimation.Snapshot()` returns a detached timeline with copied keyframe dictionaries, definition, trigger and pause state. Later changes to the original animation do not change the copy. Snapshot value graphs follow the immutable computed-style contract and must remain unchanged during render-thread sampling.

## Animation types

- [`Easing`](#type-easing)
- [`IAnimationClock`](#type-ianimationclock)
- [`Interpolation`](#type-interpolation)
- [`ManualAnimationClock`](#type-manualanimationclock)
- [`RunningAnimation`](#type-runninganimation)
- [`RunningTransition`](#type-runningtransition)
- [`SystemAnimationClock`](#type-systemanimationclock)
- [`TransitionEngine`](#type-transitionengine)

<a id="type-easing"></a>

## `Easing`

Namespace: `Nazor.Animation`

```csharp
public static class Easing
```

Timing functions: CSS cubic Bézier curves from (0,0) to (1,1).

[Source](../../Sources/Core/Nazor.Animation/Clock.cs)

### Members

#### `Evaluate`

```csharp
public static double Evaluate(EasingFunction function, double progress)
```

Output progress for an input progress in [0, 1] (the curve's y for the given x).

| Parameter | Description |
| --- | --- |
| `function` | The function (EasingFunction). |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The resulting double value.


<a id="type-ianimationclock"></a>

## `IAnimationClock`

Namespace: `Nazor.Animation`

```csharp
public interface IAnimationClock
```

A monotonic time source for animations; tests replace it with `ManualAnimationClock`.

[Source](../../Sources/Core/Nazor.Animation/Clock.cs)

### Members

#### `Now`

```csharp
TimeSpan Now { get; }
```

Gets the elapsed time reported by the animation clock.


<a id="type-interpolation"></a>

## `Interpolation`

Namespace: `Nazor.Animation`

```csharp
public static class Interpolation
```

Interpolation of the animatable value types.

[Source](../../Sources/Core/Nazor.Animation/Clock.cs)

### Members

#### `Lerp`

```csharp
public static double Lerp(double from, double to, double progress)
```

Interpolates between the start and end values at the supplied progress.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The interpolated value at the specified progress.

#### `Lerp`

```csharp
public static Color Lerp(Color from, Color to, double progress)
```

Interpolates between the start and end values at the supplied progress.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The interpolated value at the specified progress.

#### `IsInterpolable`

```csharp
public static bool IsInterpolable(Type type)
```

True when values of `type` can be interpolated; some pairs still cannot (`CanInterpolate`).

| Parameter | Description |
| --- | --- |
| `type` | The type (Type). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `CanInterpolate`

```csharp
public static bool CanInterpolate(object from, object to)
```

True when a value can move smoothly from `from` to `to`: `auto` and a length cannot, nor shadow lists with an inset shadow against an outer one. `ClipPathValue` inset lengths and both axes of all four optional corner radii interpolate component by component when both values are insets; `none` changes discretely. Mixed pixel and percentage insets retain their box-dependent calculation until painting.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Lerp`

```csharp
public static object Lerp(object from, object to, double progress)
```

Interpolates between the start and end values at the supplied progress.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The interpolated value at the specified progress.

#### `Lerp`

```csharp
public static Length Lerp(Length from, Length to, double progress)
```

Interpolates between the start and end values at the supplied progress.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The interpolated value at the specified progress.

#### `Lerp`

```csharp
public static ValueList<TransformFunction> Lerp(ValueList<TransformFunction> from, ValueList<TransformFunction> to, double progress)
```

Interpolates between the start and end values at the supplied progress.

| Parameter | Description |
| --- | --- |
| `from` | The starting value. |
| `to` | The destination value. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |

Returns: The interpolated value at the specified progress.


<a id="type-manualanimationclock"></a>

## `ManualAnimationClock`

Namespace: `Nazor.Animation`

```csharp
public sealed class ManualAnimationClock : IAnimationClock
```

A clock that moves only when told to: time-dependent behaviour becomes deterministic.

[Source](../../Sources/Core/Nazor.Animation/Clock.cs)

### Members

#### `Now`

```csharp
public TimeSpan Now { get; private set;}
```

Gets the elapsed time reported by the animation clock.

#### `Advance`

```csharp
public void Advance(TimeSpan delta)
```

Advances the clock by the supplied elapsed time.

| Parameter | Description |
| --- | --- |
| `delta` | The elapsed time by which to advance the clock. |

#### `Advance`

```csharp
public void Advance(double milliseconds)
```

Advances the clock by the supplied elapsed time.

| Parameter | Description |
| --- | --- |
| `milliseconds` | The elapsed time, in milliseconds, by which to advance the clock. |


<a id="type-runninganimation"></a>

## `RunningAnimation`

Namespace: `Nazor.Animation`

```csharp
public sealed class RunningAnimation
```

A keyframe animation of one owner (CSS Animations 1): its definition, its keyframes resolved for the owner and its timeline, which starts when the animation is applied plus the delay.

[Source](../../Sources/Core/Nazor.Animation/RunningAnimation.cs)

### Members

#### `Definition`

```csharp
public AnimationDefinition Definition { get; internal set;}
```

Gets the animation definition currently applied to this element.

#### `Keyframes`

```csharp
public IReadOnlyList<ResolvedKeyframe> Keyframes { get; internal set;}
```

Gets the ordered keyframes used to evaluate this animation.

#### `Trigger`

```csharp
public TimeSpan Trigger { get; internal set;}
```

When the animation was applied; the delay counts from here.

#### `Elapsed`

```csharp
public TimeSpan Elapsed(TimeSpan now)
```

Time since the start of the first iteration: negative during the delay.

| Parameter | Description |
| --- | --- |
| `now` | The current animation clock time. |

Returns: The resulting TimeSpan value.

#### `IsMoving`

```csharp
public bool IsMoving(TimeSpan now)
```

The animation still moves at `now` (or will, after its delay): frames are needed.

| Parameter | Description |
| --- | --- |
| `now` | The current animation clock time. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Progress`

```csharp
public double? Progress(TimeSpan now, bool instant)
```

The progress in the current iteration at `now`, after the direction (0 is the first keyframe), or null when the animation shows nothing: before the start without a backwards fill, after the end without a forwards fill. With `instant` (reduced motion) only the final values of a forwards fill show.

| Parameter | Description |
| --- | --- |
| `now` | The current animation clock time. |
| `instant` | The instant (bool). |

Returns: The resulting double? value.

#### `Animates`

```csharp
public bool Animates(StyleProperty property)
```

The keyframes set this property.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `ValueAt`

```csharp
public object? ValueAt(StyleProperty property, double progress, object underlying)
```

The value of `property` at an iteration progress, or null when no keyframe sets it. Missing 0% and 100% keyframes take `underlying`, the value without the animation; each interval eases with the timing function of the keyframe where it starts.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |
| `progress` | The interpolation progress, where zero is the start and one is the end. |
| `underlying` | The underlying (object). |

Returns: The resulting object? value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-runningtransition"></a>

## `RunningTransition`

Namespace: `Nazor.Animation`

```csharp
public sealed class RunningTransition
```

One property of one owner moving from `From` to `To`.

[Source](../../Sources/Core/Nazor.Animation/TransitionEngine.cs)

### Members

#### `Property`

```csharp
public StyleProperty Property { get; }
```

Gets the style property targeted by this value or declaration.

#### `From`

```csharp
public object From { get; }
```

Gets the starting value of the transition.

#### `To`

```csharp
public object To { get; }
```

Gets the destination value of the transition.

#### `Start`

```csharp
public TimeSpan Start { get; }
```

When the value starts to move (the style change plus the delay).

#### `Duration`

```csharp
public TimeSpan Duration { get; }
```

Gets the effective duration of the transition.

#### `EasingFunction`

```csharp
public EasingFunction EasingFunction { get; }
```

Gets the easing function applied to transition progress.

#### `ReversingAdjustedStartValue`

```csharp
public object ReversingAdjustedStartValue { get; }
```

The value a reversal must reach to count as going back (CSS Transitions §3).

#### `ReversingShorteningFactor`

```csharp
public double ReversingShorteningFactor { get; }
```

Gets the shortening factor used when reversing an interrupted transition.

#### `End`

```csharp
public TimeSpan End {get; }
```

Gets the clock time at which the transition completes.

#### `Progress`

```csharp
public double Progress(TimeSpan time)
```

Eased progress at `time`: 0 during the delay, 1 at the end.

| Parameter | Description |
| --- | --- |
| `time` | The animation clock time at which to evaluate the value. |

Returns: The resulting double value.

#### `ValueAt`

```csharp
public object ValueAt(TimeSpan time)
```

Computes the transition value at the specified clock time using its easing function.

| Parameter | Description |
| --- | --- |
| `time` | The animation clock time at which to evaluate the value. |

Returns: The interpolated property value at the requested clock time.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-systemanimationclock"></a>

## `SystemAnimationClock`

Namespace: `Nazor.Animation`

```csharp
public sealed class SystemAnimationClock : IAnimationClock
```

Monotonic time from `Stopwatch`: it never goes backwards and ignores changes of the wall clock.

[Source](../../Sources/Core/Nazor.Animation/Clock.cs)

### Members

#### `Now`

```csharp
public TimeSpan Now {get; }
```

Gets the elapsed time reported by the animation clock.


<a id="type-transitionengine"></a>

## `TransitionEngine`

Namespace: `Nazor.Animation`

```csharp
public sealed class TransitionEngine
```

CSS-like transitions and keyframe animations of animatable style values, keyed by owner (an element) and property. `StyleChanged` receives every change of an owner's computed style and starts, retargets or cancels transitions; `GetValue{T}` gives the value to draw, so the computed style stays untouched (computed style → transitions → effective value).

A transition starts from the value visible at the moment of the change: changing the destination midway continues from there without jumps, and going back to where a transition started takes proportionally less time (the CSS "reversing shortening factor"). Values are sampled at `FrameTime`, fixed by `Tick` once per frame.

[Source](../../Sources/Core/Nazor.Animation/TransitionEngine.cs)

### Members

#### `TransitionEngine`

```csharp
public TransitionEngine(IAnimationClock clock)
```

Initializes a new instance of TransitionEngine.

| Parameter | Description |
| --- | --- |
| `clock` | The clock used to measure animation time. |

#### `Clock`

```csharp
public IAnimationClock Clock { get; }
```

Gets the clock used to evaluate animation progress.

#### `FrameTime`

```csharp
public TimeSpan FrameTime { get; private set;}
```

The time values are sampled at; set by `Tick`.

#### `IsRunning`

```csharp
public bool IsRunning {get; }
```

Some value moves and needs frames: a transition, or an animation that is not paused or finished.

#### `HasAnimations`

```csharp
public bool HasAnimations {get; }
```

Some owner has keyframe animations, running, paused or finished with a fill.

#### `Count`

```csharp
public int Count {get; }
```

Number of running transitions (diagnostics and tests).

#### `ReducedMotion`

```csharp
public bool ReducedMotion { get ; set ; }
```

The user prefers reduced motion: values change at once, running transitions end, keyframe animations do not run and show only the final values of a forwards fill.

#### `Suspended`

```csharp
public bool Suspended { get ; set ; }
```

The surface is not visible (for example a minimized window): nothing animates meanwhile.

#### `StyleChanged`

```csharp
public void StyleChanged(object owner, ComputedStyle before, ComputedStyle after)
```

Starts, retargets or cancels the transitions of an owner whose computed style changed.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `before` | The before (ComputedStyle). |
| `after` | The after (ComputedStyle). |

#### `Start`

```csharp
public bool Start<T>(object owner, StyleProperty<T> property, T from, T to, TransitionDefinition definition) where T : notnull
```

Starts an explicit sampled transition, replacing that property's running transition. The caller supplies the visible `from` sample when retargeting and uses `to` as the computed fallback after completion. Keyframe animations keep precedence. Instant, equal-value and zero-duration requests cancel the transition and return false. The property/values must interpolate; invalid values throw `ArgumentException`. Timing must be nonnegative with a combined maximum of `4294967294ms`; easing coordinates must be finite and x coordinates between zero and one, or `ArgumentOutOfRangeException` is thrown. Null owner/property/definition throws `ArgumentNullException`.

When a computed style removes an entrance keyframe and starts an exit transition, `StyleChanged` samples the removed animation at the current clock time. Retained animation names do not create unrelated underlying transitions.

#### `GetValue`

```csharp
public T GetValue<T>(object owner, StyleProperty<T> property, T computed) where T : notnull
```

The value to draw at `FrameTime`: a keyframe animation's (the last of the list that sets the property), otherwise the transition's, otherwise the computed one.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `property` | The style property to read or update. |
| `computed` | The computed (T). |

Returns: The resulting T value.

#### `UpdateAnimations`

```csharp
public void UpdateAnimations(object owner, ComputedStyle style, Func<string, IReadOnlyList<ResolvedKeyframe>?> resolve)
```

Starts, updates or cancels the keyframe animations of an owner whose style was computed, the first time too. An animation keeps its timeline while its name stays in the list (CSS Animations §4); `paused` freezes it. `resolve` gives the keyframes of a name for the owner, null when no rule has it.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `style` | The style (ComputedStyle). |
| `resolve` | The resolve (Func<string, IReadOnlyList<ResolvedKeyframe>?>). |

#### `GetAnimations`

```csharp
public IReadOnlyList<RunningAnimation> GetAnimations(object owner)
```

The keyframe animations of an owner, in the order of its list.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |

Returns: The resulting IReadOnlyList<RunningAnimation> value.

#### `HasAnimationsOn`

```csharp
public bool HasAnimationsOn(object owner)
```

The owner has keyframe animations.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `IsAnimating`

```csharp
public bool IsAnimating(StyleEffects effects)
```

A transition or an animation moves a property with these effects, for any owner.

| Parameter | Description |
| --- | --- |
| `effects` | The effects (StyleEffects). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `IsAnimating`

```csharp
public bool IsAnimating(object owner, StyleEffects effects)
```

A transition or an animation of the owner moves a property with these effects (for example `StyleEffects.Layout`).

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `effects` | The effects (StyleEffects). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Find`

```csharp
public RunningTransition? Find(object owner, StyleProperty property)
```

Finds the running transition for the specified owner and style property.

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |
| `property` | The style property to read or update. |

Returns: The matching active transition, or null when none exists.

#### `Tick`

```csharp
public bool Tick()
```

Samples the clock for the next frame and drops the transitions that ended; true while some run.

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `TimeUntilNextFrame`

```csharp
public TimeSpan? TimeUntilNextFrame { get ; }
```

How long the next frame can wait: zero while a value is moving, the time to the first start while all transitions are still in their delay, null when nothing runs.

#### `Remove`

```csharp
public void Remove(object owner)
```

Cancels the transitions and animations of an owner (removed from the tree).

| Parameter | Description |
| --- | --- |
| `owner` | The object that owns this transition or native window. |

#### `Prune`

```csharp
public int Prune(Func<object, bool> isGone)
```

Cancels the transitions and animations of the owners that no longer exist; returns how many owners were dropped.

| Parameter | Description |
| --- | --- |
| `isGone` | The is gone (Func<object, bool>). |

Returns: The resulting int value.

#### `FinishAll`

```csharp
public void FinishAll()
```

Ends every transition: the computed values show.

#### `Describe`

```csharp
public string Describe()
```

Formats the active transitions for diagnostic inspection.

Returns: A textual description of the active transitions.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `ManualAnimationClock`

Namespace `Nazor.Animation`. [Source](../../Sources/Core/Nazor.Animation/Clock.cs)

| Declaration | Contract |
| --- | --- |
| `public ManualAnimationClock()` |  |
| `public TimeSpan Now { get; }` | Gets the elapsed time reported by the animation clock. |

### `RunningAnimation`

Namespace `Nazor.Animation`. [Source](../../Sources/Core/Nazor.Animation/RunningAnimation.cs)

| Declaration | Contract |
| --- | --- |
| `public RunningAnimation Snapshot()` | Copies the timeline and keyframe dictionaries for immutable sampling on a rendering worker.<br>**Returns**. A detached animation; changing the original definition or pause state does not change the copy. |
| `public AnimationDefinition Definition { get; }` | Gets the animation definition currently applied to this element. |
| `public IReadOnlyList<ResolvedKeyframe> Keyframes { get; }` | Gets the ordered keyframes used to evaluate this animation. |
| `public TimeSpan Trigger { get; }` | When the animation was applied; the delay counts from here. |

### `SystemAnimationClock`

Namespace `Nazor.Animation`. [Source](../../Sources/Core/Nazor.Animation/Clock.cs)

| Declaration | Contract |
| --- | --- |
| `public SystemAnimationClock()` |  |

### `TransitionEngine`

Namespace `Nazor.Animation`. [Source](../../Sources/Core/Nazor.Animation/TransitionEngine.cs)

| Declaration | Contract |
| --- | --- |
| `public TimeSpan FrameTime { get; }` | The time values are sampled at; set by `Nazor.Animation.TransitionEngine.Tick`. |
| `public bool IsCompositionOnly { get; }` | Whether all moving style animations can be represented by transform, transform-origin and opacity snapshots. |
