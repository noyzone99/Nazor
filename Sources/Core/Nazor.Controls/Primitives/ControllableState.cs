namespace Nazor.Controls.Primitives;

/// <summary>
/// A value that a component owns locally but that its parent can also control through a parameter, the pattern behind
/// every <c>@bind-</c> parameter of the built-in controls. Call <see cref="Sync" /> from <c>OnParametersSet</c>: a
/// parameter that changed since the previous call replaces the local value, an unchanged one keeps the value the user
/// chose. Call <see cref="Set" /> when the user changes the value, then report it through the matching callback.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="initial">The value before the first <see cref="Sync" />.</param>
public sealed class ControllableState<T>(T initial)
{
    private readonly IEqualityComparer<T> comparer = EqualityComparer<T>.Default;
    private T lastParameter = initial;
    private bool synchronized;

    /// <summary>Gets the current value, chosen locally or received from the parameter.</summary>
    public T Value { get; private set; } = initial;

    /// <summary>Adopts the parameter value when it differs from the one received at the previous call.</summary>
    /// <param name="parameter">The current value of the component parameter.</param>
    /// <returns>true if the local value changed.</returns>
    public bool Sync(T parameter)
    {
        var changed = !synchronized || !comparer.Equals(parameter, lastParameter);
        lastParameter = parameter;
        synchronized = true;
        if (!changed || comparer.Equals(Value, parameter)) return false;
        Value = parameter;
        return true;
    }

    /// <summary>Replaces the local value after a user action.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>true if the value changed and should be reported.</returns>
    public bool Set(T value)
    {
        if (comparer.Equals(Value, value)) return false;
        Value = value;
        return true;
    }
}
