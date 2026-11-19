namespace Nazor;

/// <summary>
/// An event callback whose argument type is known only at runtime, as stored in element attributes.
/// </summary>
public interface IEventCallback
{
    /// <summary>Gets whether a callback delegate has been assigned.</summary>
    bool HasDelegate { get; }

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <param name="arg">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task InvokeAsync(object? arg);
}

/// <summary>
/// A delegate bound to the component that should react to it. When the receiver handles events (as
/// <see cref="ComponentBase" /> does) invoking the callback also re-renders the receiver.
/// </summary>
public readonly struct EventCallback : IEventCallback, IEquatable<EventCallback>
{
    /// <summary>The shared factory used to create event callbacks and binding callbacks.</summary>
    public static readonly EventCallbackFactory Factory = new();

    /// <summary>An empty event callback value.</summary>
    public static readonly EventCallback Empty = default;

    internal readonly MulticastDelegate? Delegate;
    internal readonly IHandleEvent? Receiver;

    /// <summary>Initializes a new instance of EventCallback.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="delegate">The delegate to invoke when the callback is dispatched.</param>
    public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
    {
        Receiver = receiver;
        Delegate = @delegate;
    }

    /// <summary>Gets whether a callback delegate has been assigned.</summary>
    public bool HasDelegate => Delegate is not null;

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <param name="arg">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InvokeAsync(object? arg) => Receiver is null
        ? EventCallbackWorkItem.InvokeAsync(Delegate, arg)
        : Receiver.HandleEventAsync(new EventCallbackWorkItem(Delegate), arg);

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InvokeAsync() => InvokeAsync(null);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(EventCallback other) => ReferenceEquals(Receiver, other.Receiver) && Equals(Delegate, other.Delegate);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is EventCallback other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode() => HashCode.Combine(Receiver, Delegate);
}

/// <summary>An <see cref="EventCallback" /> with a typed argument.</summary>
/// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
public readonly struct EventCallback<TValue> : IEventCallback, IEquatable<EventCallback<TValue>>
{
    /// <summary>An empty event callback value.</summary>
    public static readonly EventCallback<TValue> Empty = default;

    internal readonly MulticastDelegate? Delegate;
    internal readonly IHandleEvent? Receiver;

    /// <summary>Initializes a new instance of EventCallback.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="delegate">The delegate to invoke when the callback is dispatched.</param>
    public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
    {
        Receiver = receiver;
        Delegate = @delegate;
    }

    /// <summary>Gets whether a callback delegate has been assigned.</summary>
    public bool HasDelegate => Delegate is not null;

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <param name="arg">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InvokeAsync(TValue arg) => Receiver is null
        ? EventCallbackWorkItem.InvokeAsync(Delegate, arg)
        : Receiver.HandleEventAsync(new EventCallbackWorkItem(Delegate), arg);

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InvokeAsync() => InvokeAsync(default!);

    Task IEventCallback.InvokeAsync(object? arg) => InvokeAsync((TValue)arg!);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(EventCallback<TValue> other) => ReferenceEquals(Receiver, other.Receiver) && Equals(Delegate, other.Delegate);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is EventCallback<TValue> other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode() => HashCode.Combine(Receiver, Delegate);
}

/// <summary>The delegate of an event callback, executed by the receiving component.</summary>
public readonly struct EventCallbackWorkItem
{
    /// <summary>An empty event callback work item value.</summary>
    public static readonly EventCallbackWorkItem Empty = new(null);

    private readonly MulticastDelegate? @delegate;

    /// <summary>Initializes a new instance of EventCallbackWorkItem.</summary>
    /// <param name="delegate">The delegate to invoke when the callback is dispatched.</param>
    public EventCallbackWorkItem(MulticastDelegate? @delegate) => this.@delegate = @delegate;

    /// <summary>
    /// Invokes the event callback through its receiver, using the supplied event argument when present.
    /// </summary>
    /// <param name="arg">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task InvokeAsync(object? arg) => InvokeAsync(@delegate, arg);

    internal static Task InvokeAsync<T>(MulticastDelegate? @delegate, T arg)
    {
        switch (@delegate)
        {
            case null:
                return Task.CompletedTask;
            case Action action:
                action();
                return Task.CompletedTask;
            case Action<T> actionWithArg:
                actionWithArg(arg);
                return Task.CompletedTask;
            case Func<Task> func:
                return func() ?? Task.CompletedTask;
            case Func<T, Task> funcWithArg:
                return funcWithArg(arg) ?? Task.CompletedTask;
            default:
                try
                {
                    return @delegate.DynamicInvoke(arg) as Task ?? Task.CompletedTask;
                }
                catch (System.Reflection.TargetInvocationException exception) when (exception.InnerException is not null)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    throw;
                }
        }
    }
}

/// <summary>Creates event callbacks; used by the code generated from markup.</summary>
public sealed class EventCallbackFactory
{
    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback Create(object receiver, EventCallback callback) => callback;

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback Create(object receiver, Action callback) => New(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback Create(object receiver, Action<object?> callback) => New(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback Create(object receiver, Func<Task> callback) => New(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback Create(object receiver, Func<object?, Task> callback) => New(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, EventCallback callback) => new(callback.Receiver, callback.Delegate);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, EventCallback<TValue> callback) => callback;

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Action callback) => New<TValue>(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Action<TValue> callback) => New<TValue>(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Func<Task> callback) => New<TValue>(receiver, callback);

    /// <summary>Creates an event callback associated with the supplied receiver.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The event callback associated with the receiver.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Func<TValue, Task> callback) => New<TValue>(receiver, callback);

    /// <summary>Creates a typed event callback using the value to infer the argument type.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The typed event callback associated with the receiver.</returns>
    public EventCallback<TValue> CreateInferred<TValue>(object receiver, Action<TValue> callback, TValue value) => Create(receiver, callback);

    /// <summary>Creates a typed event callback using the value to infer the argument type.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The typed event callback associated with the receiver.</returns>
    public EventCallback<TValue> CreateInferred<TValue>(object receiver, Func<TValue, Task> callback, TValue value) => Create(receiver, callback);

    private static EventCallback New(object receiver, MulticastDelegate callback) => new(receiver as IHandleEvent, callback);
    private static EventCallback<TValue> New<TValue>(object receiver, MulticastDelegate callback) => new(receiver as IHandleEvent, callback);
}
