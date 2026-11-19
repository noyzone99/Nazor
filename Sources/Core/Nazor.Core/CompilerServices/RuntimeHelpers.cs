namespace Nazor.CompilerServices;

/// <summary>Helpers called by the code generated from <c>.nazor</c> markup. Not intended for direct use.</summary>
public static class RuntimeHelpers
{
    /// <summary>
    /// Returns the supplied value while enforcing its type at compile time in generated component code.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The supplied value, unchanged.</returns>
    public static T TypeCheck<T>(T value) => value;

    /// <summary>Creates an event callback whose argument type is inferred from the supplied value.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The typed event callback associated with the receiver.</returns>
    public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Action<T> callback, T value) =>
        EventCallback.Factory.Create(receiver, callback);

    /// <summary>Creates an event callback whose argument type is inferred from the supplied value.</summary>
    /// <param name="receiver">The object that receives the callback and controls event dispatch.</param>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The typed event callback associated with the receiver.</returns>
    public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Func<T, Task> callback, T value) =>
        EventCallback.Factory.Create(receiver, callback);

    /// <summary>Adapts a binding setter to an asynchronous delegate, using the value to infer its type.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>An asynchronous setter delegate for the inferred value type.</returns>
    public static Func<T, Task> CreateInferredBindSetter<T>(Func<T, Task> callback, T value) => callback;

    /// <summary>Adapts a binding setter to an asynchronous delegate, using the value to infer its type.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="value">The value to use.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>An asynchronous setter delegate for the inferred value type.</returns>
    public static Func<T, Task> CreateInferredBindSetter<T>(Action<T> callback, T value) => value =>
    {
        callback(value);
        return Task.CompletedTask;
    };

    /// <summary>Invokes a synchronous binding callback.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    public static void InvokeSynchronousDelegate(Action callback) => callback();

    /// <summary>Invokes a binding callback and returns its completion task.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public static Task InvokeAsynchronousDelegate(Action callback)
    {
        callback();
        return Task.CompletedTask;
    }

    /// <summary>Invokes a binding callback and returns its completion task.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public static Task InvokeAsynchronousDelegate(Func<Task> callback) => callback();
}
