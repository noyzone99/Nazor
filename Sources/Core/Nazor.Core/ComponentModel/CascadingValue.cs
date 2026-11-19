using Nazor.Rendering;

namespace Nazor;

internal interface ICascadingValueSupplier
{
    bool IsFixed { get; }
    bool CanSupply(Type valueType, string? name);
    object? CurrentValue { get; }
    IDisposable Subscribe(Func<Task> subscriber);
}

/// <summary>A root cascading value. Non-fixed sources notify only their subscribed components.</summary>
/// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
public sealed class CascadingValueSource<TValue> : ICascadingValueSupplier
{
    private readonly Lock gate = new();
    private readonly List<Subscription> subscribers = [];
    private TValue currentValue;
    private readonly string? name;

    /// <summary>Initializes a new instance of CascadingValueSource.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="isFixed">Whether consumers can treat the value as immutable and skip change subscriptions.</param>
    public CascadingValueSource(TValue value, bool isFixed) : this(null, value, isFixed) { }

    /// <summary>Initializes a new instance of CascadingValueSource.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="isFixed">Whether consumers can treat the value as immutable and skip change subscriptions.</param>
    public CascadingValueSource(string? name, TValue value, bool isFixed)
    {
        this.name = name;
        currentValue = value;
        IsFixed = isFixed;
    }

    /// <summary>Gets whether consumers can treat this source as immutable.</summary>
    public bool IsFixed { get; }
    object? ICascadingValueSupplier.CurrentValue { get { lock (gate) return currentValue; } }

    bool ICascadingValueSupplier.CanSupply(Type valueType, string? parameterName) =>
        valueType.IsAssignableFrom(typeof(TValue)) && string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase);

    IDisposable ICascadingValueSupplier.Subscribe(Func<Task> subscriber)
    {
        if (IsFixed) throw new InvalidOperationException("A fixed cascading value cannot have subscriptions.");
        var subscription = new Subscription(this, subscriber);
        lock (gate) subscribers.Add(subscription);
        return subscription;
    }

    internal void SetCurrentValue(TValue value) { lock (gate) currentValue = value; }

    /// <summary>
    /// Notifies subscribed components that the cascading value has changed, optionally replacing the current value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task NotifyChangedAsync(TValue value)
    {
        if (IsFixed) throw new InvalidOperationException("A fixed cascading value cannot issue notifications.");
        SetCurrentValue(value);
        return NotifyChangedAsync();
    }

    /// <summary>
    /// Notifies subscribed components that the cascading value has changed, optionally replacing the current value.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task NotifyChangedAsync()
    {
        if (IsFixed) throw new InvalidOperationException("A fixed cascading value cannot issue notifications.");
        Subscription[] snapshot;
        lock (gate) snapshot = subscribers.ToArray();
        return Task.WhenAll(snapshot.Select(s => s.Notify()));
    }

    private sealed class Subscription(CascadingValueSource<TValue> source, Func<Task> notify) : IDisposable
    {
        public Task Notify() => notify();
        public void Dispose() { lock (source.gate) source.subscribers.Remove(this); }
    }
}

/// <summary>Supplies a typed value to descendants, matched by type and optional name.</summary>
/// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
public sealed class CascadingValue<TValue> : ComponentBase, ICascadingValueSupplier
{
    private CascadingValueSource<TValue>? source;
    private string? initialName;
    private bool initialFixed;
    private TValue? previousValue;

    /// <summary>Gets or sets the value supplied to descendant components.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets the optional name used to match descendant cascading parameters.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether the cascading value is fixed and consumers can skip change subscriptions.
    /// </summary>
    [Parameter] public bool IsFixed { get; set; }

    /// <summary>Gets or sets the render fragment supplied between the component's opening and closing tags.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Assigns component parameters and runs the initialization and parameter lifecycle callbacks.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!parameters.Any(p => string.Equals(p.Name, nameof(Value), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("CascadingValue requires a Value parameter.");
        return base.SetParametersAsync(parameters);
    }

    /// <summary>Asynchronously updates component state after parameter values have been assigned.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override Task OnParametersSetAsync()
    {
        if (source is null)
        {
            initialName = Name;
            initialFixed = IsFixed;
            source = new(Name, Value, IsFixed);
            previousValue = Value;
            return Task.CompletedTask;
        }
        if (initialName != Name || initialFixed != IsFixed)
            throw new InvalidOperationException("Name and IsFixed cannot change after a CascadingValue is initialized.");
        var changed = Renderer.MayHaveChanged(previousValue, Value);
        previousValue = Value;
        source.SetCurrentValue(Value);
        return changed && !IsFixed ? source.NotifyChangedAsync() : Task.CompletedTask;
    }

    /// <summary>Writes the render frames for CascadingValue to the supplied builder.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);

    bool ICascadingValueSupplier.IsFixed => IsFixed;
    bool ICascadingValueSupplier.CanSupply(Type type, string? name) => ((ICascadingValueSupplier)source!).CanSupply(type, name);
    object? ICascadingValueSupplier.CurrentValue => Value;
    IDisposable ICascadingValueSupplier.Subscribe(Func<Task> subscriber) => ((ICascadingValueSupplier)source!).Subscribe(subscriber);
}
