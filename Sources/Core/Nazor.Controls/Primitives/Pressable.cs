using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>
/// Turns any content into something pressable, without imposing a look: a click, Enter or Space invoke
/// <see cref="OnPress" />; with <see cref="RepeatInterval" /> the action repeats while the pointer holds the element, as
/// the arrows of a scroll bar or a number field; <see cref="OnLongPress" /> reports a press held for
/// <see cref="LongPressDelay" />, after which the release does not count as a click. The element is named by
/// <see cref="As" /> so it can be styled like any part.
/// </summary>
public sealed class Pressable : ContentControl, IDisposable
{
    private RepeatState? timer;
    private TimeProvider? timerTime;

    /// <summary>Gets or sets the element name, <c>Pressable</c> by default.</summary>
    [Parameter] public string As { get; set; } = "Pressable";

    /// <summary>Gets or sets the accessible role, <c>button</c> by default.</summary>
    [Parameter] public string Role { get; set; } = "button";

    /// <summary>Gets or sets the callback invoked by a click, Enter, Space and each repetition.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnPress { get; set; }

    /// <summary>Gets or sets the callback invoked when the pointer stays down for <see cref="LongPressDelay" />.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnLongPress { get; set; }

    /// <summary>Gets or sets how long a press must last to count as a long press.</summary>
    [Parameter] public TimeSpan LongPressDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Gets or sets the interval between held pointer or Enter/Space actions, or null for ordinary click activation. Repetition fires once on press and suppresses release activation.</summary>
    [Parameter] public TimeSpan? RepeatInterval { get; set; }

    /// <summary>Gets or sets the delay before the first repetition.</summary>
    [Parameter] public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Gets or sets whether the element rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the element is a Tab stop; true by default.</summary>
    [Parameter] public bool Focusable { get; set; } = true;

    /// <summary>Gets or sets the cascading timer provider for held input; TimeProvider.System by default.</summary>
    [CascadingParameter] public TimeProvider Time { get; set; } = TimeProvider.System;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Validates the timing parameters.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(As)) throw new ArgumentException("Pressable requires an element name.", nameof(As));
        ArgumentNullException.ThrowIfNull(Time);
        if (RepeatInterval is { } interval) RepeatState.ValidateDuration(interval, nameof(RepeatInterval), positive: true);
        RepeatState.ValidateDuration(RepeatDelay, nameof(RepeatDelay));
        RepeatState.ValidateDuration(LongPressDelay, nameof(LongPressDelay));
        if (!ReferenceEquals(timerTime, Time)) { timer?.SetTime(Time); timerTime = Time; }
        if (Disabled || RepeatInterval is null && !OnLongPress.HasDelegate) timer?.Cancel();
    }

    /// <summary>Adds the role, the activation and the press tracking.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "focusable", Focusable && !Disabled);
        builder.AddAttribute(13, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, Click));
        if (RepeatInterval is not null || OnLongPress.HasDelegate)
        {
            timer ??= new RepeatState(work => InvokeAsync(work), args => OnPress.InvokeAsync(args), args => OnLongPress.InvokeAsync(args),
                Time, () => Element, () => !Disabled);
            builder.AddAttribute(14, "__internal_onpointerdown#press", EventCallback.Factory.Create<PointerEventArgs>(this, Down));
            builder.AddAttribute(15, "__internal_onpointerup#press", EventCallback.Factory.Create<PointerEventArgs>(this, timer.PointerUp));
            builder.AddAttribute(16, "__internal_onpointerleave#press", EventCallback.Factory.Create<PointerEventArgs>(this, () => timer?.Cancel()));
            builder.AddAttribute(17, "__internal_onlostpointercapture#press", EventCallback.Factory.Create<PointerEventArgs>(this, timer.Lost));
            builder.AddAttribute(18, "__internal_onblur#press", EventCallback.Factory.Create<FocusEventArgs>(this, () => timer?.Cancel()));
            builder.AddAttribute(19, "__internal_onkeydown#press", EventCallback.Factory.Create<KeyboardEventArgs>(this,
                e => timer.KeyDownAsync(e, RepeatInterval, RepeatDelay)));
            builder.AddAttribute(20, "__internal_onkeyup#press", EventCallback.Factory.Create<KeyboardEventArgs>(this, timer.KeyUp));
        }
    }

    private Task Click(PointerEventArgs e)
    {
        if (Disabled || Element.Element?.IsDisabled is true) return Task.CompletedTask;
        if (timer?.ConsumeClick() is true && !e.IsSynthetic) return Task.CompletedTask;
        return OnPress.InvokeAsync(e);
    }

    private Task Down(PointerEventArgs e)
    {
        return timer?.PointerDownAsync(e, RepeatInterval, RepeatDelay, LongPressDelay) ?? Task.CompletedTask;
    }

    /// <summary>Stops pending repetitions.</summary>
    public void Dispose() => timer?.Dispose();

    internal sealed class RepeatState(Func<Func<Task>, Task> dispatch, Func<PointerEventArgs, Task> action,
        Func<PointerEventArgs, Task>? longPress = null, TimeProvider? time = null,
        Func<ElementReference>? owner = null, Func<bool>? available = null) : IDisposable
    {
        private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(400);
        private TimeProvider clock = time ?? TimeProvider.System;
        private ITimer? timer;
        private bool suppressClick;
        private int generation;
        private bool held;
        private string? heldKey;

        private bool Available => available?.Invoke() != false && owner?.Invoke().Element?.IsDisabled != true;

        public void SetTime(TimeProvider value)
        {
            if (ReferenceEquals(clock, value)) return;
            Cancel();
            clock = value;
        }

        public static void ValidateDuration(TimeSpan value, string name, bool positive = false)
        {
            if (value < TimeSpan.Zero || positive && value == TimeSpan.Zero || value.TotalMilliseconds > uint.MaxValue - 1)
                throw new ArgumentOutOfRangeException(name);
        }

        public Task PointerDownAsync(PointerEventArgs e, TimeSpan? interval, TimeSpan delay, TimeSpan longDelay)
        {
            if (!Available || e.IsSynthetic || e.Button != PointerButton.Primary) return Task.CompletedTask;
            var element = owner?.Invoke();
            if (element?.Element is { } root && ControlTree.Ancestors(e.OriginalTarget).TakeWhile(node => node != root)
                .Any(node => node.Attributes.GetValueOrDefault("focusable") is true || node.Attributes.GetValueOrDefault("editable") is true)) return Task.CompletedTask;
            Cancel();
            if (element is { } target && !target.CapturePointer()) return Task.CompletedTask;
            if (interval is { } repeat) return Down(e, repeat, delay);
            Hold(e, longDelay);
            return Task.CompletedTask;
        }

        public void PointerUp(PointerEventArgs e)
        {
            if (e.Button != PointerButton.Primary) return;
            Up();
        }

        public void Lost(PointerEventArgs e)
        {
            if (owner?.Invoke().Element == e.OriginalTarget) Cancel();
        }

        public Task KeyDownAsync(KeyboardEventArgs e, TimeSpan? interval, TimeSpan delay)
        {
            if (e.Handled) return Task.CompletedTask;
            if (e.Key == "Escape" && held) { e.Handled = true; Cancel(); return Task.CompletedTask; }
            if (interval is null || !Available || e.Modifiers != KeyModifiers.None || e.Key is not ("Enter" or " ") ||
                owner?.Invoke().Element is { } root && !root.States.HasFlag(ElementStates.Focus)) return Task.CompletedTask;
            e.Handled = true;
            if (e.Repeat || heldKey == e.Key) return Task.CompletedTask;
            var task = Down(new PointerEventArgs { IsSynthetic = true }, interval.Value, delay);
            heldKey = e.Key;
            return task;
        }

        public void KeyUp(KeyboardEventArgs e)
        {
            if (e.Key != heldKey) return;
            e.Handled = true;
            Up();
        }

        public Task Down(PointerEventArgs e, TimeSpan interval, TimeSpan? delay = null)
        {
            Stop(keepSuppression: false);
            held = true;
            suppressClick = true;
            var current = generation;
            timer = clock.CreateTimer(_ => dispatch(() => InvokeAsync(current, action, e)), null, delay ?? DefaultDelay, interval);
            return InvokeAsync(current, action, e);
        }

        public void Hold(PointerEventArgs e, TimeSpan delay)
        {
            Stop(keepSuppression: false);
            held = true;
            var current = generation;
            timer = clock.CreateTimer(_ => dispatch(() =>
            {
                if (current != generation || longPress is null) return Task.CompletedTask;
                suppressClick = true;
                Stop(keepSuppression: true);
                return Available ? longPress(e) : Task.CompletedTask;
            }), null, delay, Timeout.InfiniteTimeSpan);
        }

        private Task InvokeAsync(int current, Func<PointerEventArgs, Task> callback, PointerEventArgs e)
        {
            if (current != generation) return Task.CompletedTask;
            if (!Available) { Cancel(); return Task.CompletedTask; }
            return callback(e);
        }

        public void Up() { Stop(keepSuppression: true); Release(); }

        public void Cancel() { suppressClick |= held; Stop(keepSuppression: true); Release(); }

        private void Release()
        {
            if (owner?.Invoke() is { } target && target.IsPointerCaptured) target.ReleasePointerCapture();
        }

        public bool ConsumeClick()
        {
            var suppress = suppressClick;
            suppressClick = false;
            return suppress;
        }

        private void Stop(bool keepSuppression)
        {
            generation++;
            held = false;
            heldKey = null;
            timer?.Dispose();
            timer = null;
            if (!keepSuppression) suppressClick = false;
        }

        public void Dispose() { Stop(keepSuppression: false); Release(); }
    }
}
