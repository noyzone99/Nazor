using Nazor.Rendering;

namespace Nazor;

/// <summary>A unit of UI that receives parameters and renders through its <see cref="RenderHandle" />.</summary>
public interface IComponent
{
    /// <summary>Binds the component to the renderer that owns it. Called once, before any parameters.</summary>
    /// <param name="renderHandle">The handle connecting the component to its owning renderer.</param>
    void Attach(RenderHandle renderHandle);

    /// <summary>Supplies the parameters from the parent frame, or from the host for a root component.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SetParametersAsync(ParameterView parameters);
}

/// <summary>Implemented by components that decide how event callbacks targeting them are executed.</summary>
public interface IHandleEvent
{
    /// <summary>
    /// Executes an event callback with the supplied event argument according to the component's event policy.
    /// </summary>
    /// <param name="item">The event callback work item to execute.</param>
    /// <param name="arg">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task HandleEventAsync(EventCallbackWorkItem item, object? arg);
}

/// <summary>Implemented by components that need a notification after their output has been applied.</summary>
public interface IHandleAfterRender
{
    /// <summary>Asynchronously notifies the component after its rendered output has been applied.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task OnAfterRenderAsync();
}

/// <summary>Marks a component property that can be set by the parent through markup.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ParameterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether this parameter collects attributes that do not match another component parameter.
    /// </summary>
    public bool CaptureUnmatchedValues { get; set; }
}

/// <summary>Requires an explicit assignment when a component is used in Nazor markup.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EditorRequiredAttribute : Attribute;

/// <summary>Marks a component property that is resolved from the renderer's service provider.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InjectAttribute : Attribute;

/// <summary>A value supplied by the nearest matching CascadingValue or a root DI registration.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CascadingParameterAttribute : Attribute
{
    /// <summary>Gets or sets the optional name used to match the cascading value source.</summary>
    public string? Name { get; set; }
}

/// <summary>Propagates a named generic type parameter to descendant components in markup.</summary>
/// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CascadingTypeParameterAttribute(string name) : Attribute
{
    /// <summary>Gets the generic type parameter name propagated to descendant components.</summary>
    public string Name { get; } = name;
}

/// <summary>Content rendered into a <see cref="RenderTreeBuilder" />.</summary>
/// <param name="builder">The builder that receives the render frames.</param>
public delegate void RenderFragment(RenderTreeBuilder builder);

/// <summary>A template that produces content for a value.</summary>
/// <param name="value">The value to use.</param>
/// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
/// <returns>The resulting RenderFragment value.</returns>
public delegate RenderFragment RenderFragment<TValue>(TValue value);

/// <summary>
/// Declares a directive event attribute such as <c>@onclick</c> and the type of its arguments. The compiler makes the
/// attribute available in markup; the value reaches the component parameter with the same name, compared
/// case-insensitively (for example <c>OnClick</c>).
/// </summary>
/// <param name="attributeName">The directive event attribute name recognized by the compiler.</param>
/// <param name="eventArgsType">The CLR type used for this event's arguments.</param>
/// <param name="enableStopPropagation">Whether markup may request that this event stop propagating.</param>
/// <param name="enablePreventDefault">Whether markup may request prevention of the event's default action.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class EventHandlerAttribute(string attributeName, Type eventArgsType,
    bool enableStopPropagation, bool enablePreventDefault) : Attribute
{
    /// <summary>Initializes a new instance of EventHandlerAttribute.</summary>
    /// <param name="attributeName">The directive event attribute name recognized by the compiler.</param>
    /// <param name="eventArgsType">The CLR type used for this event's arguments.</param>
    public EventHandlerAttribute(string attributeName, Type eventArgsType) : this(attributeName, eventArgsType, true, true) { }

    /// <summary>Gets the directive event attribute name recognized in markup.</summary>
    public string AttributeName { get; } = attributeName;

    /// <summary>Gets the CLR type used for the event's arguments.</summary>
    public Type EventArgsType { get; } = eventArgsType;

    /// <summary>Gets whether the markup event supports the stopPropagation modifier.</summary>
    public bool EnableStopPropagation { get; } = enableStopPropagation;

    /// <summary>Gets whether the markup event supports the preventDefault modifier.</summary>
    public bool EnablePreventDefault { get; } = enablePreventDefault;
}
