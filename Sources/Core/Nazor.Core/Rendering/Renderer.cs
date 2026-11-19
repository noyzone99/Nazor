using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Nazor.Rendering;

internal sealed class ComponentState(Renderer renderer, int id, IComponent component, ComponentState? parent, Type? declaringScope, IComponent? declaringComponent)
{
    public int Id { get; } = id;
    public IComponent Component { get; } = component;
    public ComponentState? Parent { get; } = parent;
    public Type? DeclaringScope { get; } = declaringScope;
    public IComponent? DeclaringComponent { get; } = declaringComponent;
    public ComponentNode Node { get; set; } = null!;
    public RenderTreeFrame[] Frames { get; set; } = [];
    public bool IsDisposed { get; set; }
    private ParameterView directParameters;
    internal ParameterView DirectParameters => directParameters;
    private List<(PropertyInfo Property, ICascadingValueSupplier Supplier)>? cascadingParameters;
    private readonly List<IDisposable> subscriptions = [];

    public void SetDirectParameters(ParameterView parameters)
    {
        directParameters = parameters;
        SupplyParameters();
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Nazor.Sdk preserves component properties and base types in components.xml.")]
    public void SupplyParameters()
    {
        Task task;
        try
        {
            if (cascadingParameters is null)
            {
                cascadingParameters = [];
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var subscribed = new HashSet<ICascadingValueSupplier>();
                for (var type = Component.GetType(); type is not null; type = type.BaseType)
                    foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (!seen.Add(property.Name) || property.GetCustomAttribute<CascadingParameterAttribute>(true) is not { } attribute) continue;
                        if (property.SetMethod is null) throw new InvalidOperationException($"Cascading parameter '{property.Name}' must be writable.");
                        ICascadingValueSupplier? supplier = null;
                        for (var ancestor = Parent; ancestor is not null && supplier is null; ancestor = ancestor.Parent)
                            if (ancestor.Component is ICascadingValueSupplier candidate && candidate.CanSupply(property.PropertyType, attribute.Name)) supplier = candidate;
                        supplier ??= renderer.RootCascadingValues.FirstOrDefault(s => s.CanSupply(property.PropertyType, attribute.Name));
                        if (supplier is null) continue;
                        cascadingParameters.Add((property, supplier));
                        if (!supplier.IsFixed && subscribed.Add(supplier))
                            subscriptions.Add(supplier.Subscribe(() => renderer.Dispatcher.InvokeAsync(() => renderer.NotifyCascadingValueChanged(this))));
                    }
            }
            var values = directParameters.Concat(cascadingParameters.Select(p => new ParameterValue(p.Property.Name, p.Supplier.CurrentValue) { Cascading = true })).ToArray();
            task = Component.SetParametersAsync(ParameterView.FromValues(values));
        }
        catch (Exception exception)
        {
            renderer.ReportException(exception);
            return;
        }
        renderer.ObserveLifecycle(task);
    }

    public void Unsubscribe()
    {
        foreach (var subscription in subscriptions) subscription.Dispose();
        subscriptions.Clear();
    }
}

/// <summary>
/// Owns component instances, runs their renders in batches and keeps the retained node tree in sync with the render
/// output. Matching follows Blazor's rules: siblings correspond when they have the same sequence number and kind, or
/// the same <c>@key</c>; matched nodes and component instances are preserved, the others are created or removed (and
/// disposed). Everything runs on <see cref="Dispatcher" />.
/// </summary>
public class Renderer : IDisposable
{
    private readonly Dictionary<int, ComponentState> components = [];
    private readonly Queue<(ComponentState State, RenderFragment Fragment)> renderQueue = new();
    private readonly List<ComponentState> rendered = [];
    private readonly HashSet<ComponentState> renderedSet = [];
    private readonly List<RootComponent> roots = [];
    private readonly RenderTreeBuilder builder = new();
    private RenderTreeFrame[]? spareFrames;
    private HashSet<object>? availableKeys;
    private readonly IServiceProvider? services;
    private readonly IWindow? windowContext;
    private RenderBatchBuilder batch = new();
    private int nextComponentId;
    private bool batchInProgress;
    private bool disposed;
    private List<ExceptionDispatchInfo>? pendingExceptions;

    /// <summary>Initializes a new instance of Renderer.</summary>
    /// <param name="dispatcher">The dispatcher that owns the UI thread.</param>
    /// <param name="services">The dependency injection service collection or provider used by the application.</param>
    /// <param name="window">Optional owning window for native and headless window hosts.</param>
    public Renderer(Dispatcher dispatcher, IServiceProvider? services = null, IWindow? window = null)
    {
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.services = services;
        windowContext = window;
        RootCascadingValues = (services?.GetService(typeof(IEnumerable<ICascadingValueSupplier>)) as IEnumerable<ICascadingValueSupplier>)?.ToArray() ?? [];
    }

    /// <summary>Gets the UI dispatcher associated with this renderer or window.</summary>
    public Dispatcher Dispatcher { get; }
    internal IReadOnlyList<ICascadingValueSupplier> RootCascadingValues { get; }

    internal void NotifyCascadingValueChanged(ComponentState state)
    {
        if (disposed || state.IsDisposed) return;
        RunBatch(state.SupplyParameters);
    }

    /// <summary>Gets the root components attached to this renderer.</summary>
    public IReadOnlyList<RootComponent> RootComponents => roots;

    /// <summary>Number of live component instances; useful to verify that removal releases components.</summary>
    public int ComponentCount => components.Count;

    internal bool IsRenderingOnMetadataUpdate { get; private set; }
    internal bool IsBatchInProgress => batchInProgress;

    internal (Type? Scope, IComponent? Component) ComponentDeclaration(int componentId)
    {
        var state = components[componentId];
        return (state.DeclaringScope, state.DeclaringComponent);
    }

    /// <summary>Raised on the dispatcher after each batch, once the tree is consistent.</summary>
    public event EventHandler<RenderBatch>? BatchCompleted;

    /// <summary>
    /// Receives exceptions from rendering, lifecycle methods, event handlers and disposal. Without a subscriber the
    /// exception is rethrown to the caller of the operation (or to the dispatcher for asynchronous work that
    /// completes later).
    /// </summary>
    public event EventHandler<Exception>? UnhandledException;

    /// <summary>Instantiates and attaches a root component, then supplies its initial parameters.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <typeparam name="TComponent">The component type to instantiate or render.</typeparam>
    /// <returns>The root component handle attached to the renderer.</returns>
    public RootComponent AddRootComponent<TComponent>(ParameterView parameters = default) where TComponent : IComponent =>
        AddRootComponent(typeof(TComponent), parameters);

    /// <summary>Instantiates and attaches a root component, then supplies its initial parameters.</summary>
    /// <param name="componentType">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <returns>The root component handle attached to the renderer.</returns>
    public RootComponent AddRootComponent(Type componentType, ParameterView parameters = default)
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        RootComponent? root = null;
        if (typeof(WindowComponentBase).IsAssignableFrom(componentType)) WindowParameters.Validate(componentType, parameters);
        RunBatch(() =>
        {
            var state = CreateComponentState(componentType, parent: null, declaringScope: null);
            root = new RootComponent(this, state.Node, parameters);
            roots.Add(root);
            batch.RootNodesChanged = true;
            state.SetDirectParameters(parameters);
        });
        return root!;
    }

    /// <summary>
    /// Re-renders every root component after a .NET Hot Reload delta: each root receives the parameters it was added
    /// with, and the render cascades to every child whether or not its parameters changed. Lifecycle methods (<c>OnParametersSet</c>
    /// and the async variant) run again, as in Blazor. Exceptions reach <see cref="UnhandledException" /> as for any
    /// render.
    /// </summary>
    public void RenderOnHotReload()
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        IsRenderingOnMetadataUpdate = true;
        try
        {
            RunBatch(() =>
            {
                foreach (var root in roots.ToArray())
                    if (!root.Node.State.IsDisposed) root.Node.State.SetDirectParameters(root.Parameters);
            });
        }
        finally
        {
            IsRenderingOnMetadataUpdate = false;
        }
    }

    /// <summary>Removes a root component and disposes its component subtree.</summary>
    /// <param name="root">The root (RootComponent).</param>
    public void RemoveRootComponent(RootComponent root)
    {
        Dispatcher.AssertAccess();
        if (!roots.Remove(root)) return;
        RunBatch(() =>
        {
            RemoveNode(root.Node);
            batch.RootNodesChanged = true;
        });
    }

    /// <summary>Dispatches the event callback through its receiver and observes asynchronous completion.</summary>
    /// <remarks>Disabled elements still receive noninteractive resize notifications and focus/pointer cleanup; activation is suppressed.</remarks>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="eventArgs">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task DispatchEventAsync(ElementNode element, string eventName, object? eventArgs)
    {
        Dispatcher.AssertAccess();
        return DispatchEventAsync(element, eventName, eventArgs, out _);
    }

    /// <summary>Dispatches the event callback through its receiver and observes asynchronous completion.</summary>
    /// <remarks>Disabled elements still receive noninteractive resize notifications and focus/pointer cleanup; activation is suppressed.</remarks>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="eventArgs">The event argument passed to the handler.</param>
    /// <param name="preventDefault">When this method returns, contains the prevent default produced by the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task DispatchEventAsync(ElementNode element, string eventName, object? eventArgs, out bool preventDefault)
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        preventDefault = false;
        if (element.IsRemoved || (element.IsDisabled && eventName is not ("onblur" or "onfocusout" or "onpointerleave" or "onlostpointercapture" or "onresize"))) return Task.CompletedTask;
        if (eventArgs is Input.PointerEventArgs pointer) pointer.OriginalTarget ??= element;
        if (eventArgs is Input.ScrollEventArgs scroll) scroll.OriginalTarget ??= element;
        if (eventName is "oninput" or "onchange" || eventName == "onblur" &&
            (element.Attributes.GetValueOrDefault("validation-control") is true || element.Attributes.GetValueOrDefault("role") is
                "textbox" or "searchbox" or "checkbox" or "radio" or "radiogroup" or "switch" or "combobox" or "spinbutton" or "slider" or "listbox" or "tree" or "togglebutton"))
            ValidationFeedback.MarkInteracted(element);
        var callbacks = new List<IEventCallback>();
        IEventCallback? defaultCallback = null;
        ElementNode? defaultElement = null;

        var bubbles = eventName is not ("onfocus" or "onblur" or "onpointerenter" or "onpointerleave" or "ondismiss" or "onresize");
        for (var current = element; current is not null; current = bubbles ? current.ParentElement : null)
        {
            if (defaultCallback is null && current.Attributes.GetValueOrDefault("__default_" + eventName) is IEventCallback action)
            {
                defaultCallback = action;
                defaultElement = current;
            }
            preventDefault |= current.Attributes.GetValueOrDefault("__internal_preventDefault_" + eventName) is true;
            if (current.Attributes.GetValueOrDefault("__internal_" + eventName) is IEventCallback { HasDelegate: true } internalCallback) callbacks.Add(internalCallback);
            foreach (var (key, value) in current.Attributes)
                if (value is IEventCallback { HasDelegate: true } tagged && key.Length > eventName.Length + 12 && key[eventName.Length + 11] == '#' &&
                    key.StartsWith("__internal_", StringComparison.Ordinal) && string.CompareOrdinal(key, 11, eventName, 0, eventName.Length) == 0)
                    callbacks.Add(tagged);
            if (current.Attributes.GetValueOrDefault(eventName) is IEventCallback { HasDelegate: true } callback) callbacks.Add(callback);
            if (current.Attributes.GetValueOrDefault("__internal_stopPropagation_" + eventName) is true) break;
        }
        var tasks = new List<Task>();
        var runDefault = !preventDefault;
        RunBatch(() =>
        {
            foreach (var callback in callbacks)
            {
                if (eventArgs is Input.KeyboardEventArgs { Handled:true } or Input.WheelEventArgs { Handled:true }) break;
                try { tasks.Add(callback.InvokeAsync(eventArgs)); }
                catch (Exception exception) { ReportException(exception); }
            }
            if (runDefault && eventArgs is not (Input.KeyboardEventArgs { Handled:true } or Input.WheelEventArgs { Handled:true }) && defaultElement is { IsRemoved: false } && defaultCallback is { HasDelegate: true })
            {
                try { tasks.Add(defaultCallback.InvokeAsync(eventArgs)); }
                catch (Exception exception) { ReportException(exception); }
            }
        });
        preventDefault |= eventArgs is Input.WheelEventArgs { Handled:true };
        return ObserveEvent(Task.WhenAll(tasks));
    }

    /// <summary>Dispatches the event callback through its receiver and observes asynchronous completion.</summary>
    /// <param name="callback">The delegate to invoke when the callback is dispatched.</param>
    /// <param name="eventArgs">The event argument passed to the handler.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task DispatchEventAsync(IEventCallback callback, object? eventArgs)
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        var task = Task.CompletedTask;
        RunBatch(() => task = callback.InvokeAsync(eventArgs));
        return ObserveEvent(task);
    }

    internal void AddToRenderQueue(int componentId, RenderFragment fragment)
    {
        Dispatcher.AssertAccess();
        if (disposed || !components.TryGetValue(componentId, out var state) || state.IsDisposed) return;
        renderQueue.Enqueue((state, fragment));
        if (!batchInProgress) RunBatch(static () => { });
    }

    internal void ReportException(Exception exception)
    {
        if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate) exception = aggregate.InnerExceptions[0];
        if (!RaiseUnhandled(exception)) (pendingExceptions ??= []).Add(ExceptionDispatchInfo.Capture(exception));
    }

    internal void ObserveLifecycle(Task task)
    {
        if (task.IsCompletedSuccessfully || task.IsCanceled) return;
        if (task.IsFaulted)
        {
            ReportException(task.Exception!);
            return;
        }
        _ = ObserveLaterAsync(task);
    }

    private async Task ObserveLaterAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException) when (task.IsCanceled)
        {
        }
        catch (Exception exception) when (!RaiseUnhandled(exception))
        {
            if (Dispatcher is UIDispatcher dispatcher) dispatcher.Post(() => ExceptionDispatchInfo.Throw(exception));
            else throw;
        }
    }

    private async Task ObserveEvent(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception) when (RaiseUnhandled(exception))
        {
        }
    }

    private bool RaiseUnhandled(Exception exception)
    {
        var handler = UnhandledException;
        if (handler is null) return false;
        handler(this, exception);
        return true;
    }

    private void RunBatch(Action action)
    {
        if (batchInProgress)
        {
            action();
            return;
        }
        batchInProgress = true;

        var previousContext = SynchronizationContext.Current;
        if (Dispatcher.SynchronizationContext is { } context) SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                ReportException(exception);
            }
            ProcessRenderQueue();
        }
        finally
        {
            batchInProgress = false;
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
        if (pendingExceptions is { } failures)
        {
            pendingExceptions = null;
            if (failures.Count == 1) failures[0].Throw();
            throw new AggregateException(failures.Select(f => f.SourceException));
        }
    }

    private void ProcessRenderQueue()
    {
        do
        {
            while (renderQueue.TryDequeue(out var item))
            {
                if (!item.State.IsDisposed) RenderComponent(item.State, item.Fragment);
            }
            var completed = batch.Build();
            batch = new RenderBatchBuilder();
            var renderedNow = rendered.ToArray();
            rendered.Clear();
            renderedSet.Clear();
            if (!completed.IsEmpty || renderedNow.Length > 0)
            {
                try
                {
                    foreach (var window in roots.Select(r => r.Component).OfType<WindowComponentBase>())
                    {
                        if (window.TitleBarCount > 1) throw new InvalidOperationException("A window can declare at most one UserTitleBar per render.");
                        if (window.CaptionButtonsCount > 1) throw new InvalidOperationException("A window can declare at most one UserCaptionButtons per render.");
                    }
                    BatchCompleted?.Invoke(this, completed);
                }
                catch (Exception exception)
                {
                    ReportException(exception);
                }
            }
            foreach (var state in renderedNow)
            {
                if (state.IsDisposed || state.Component is not IHandleAfterRender afterRender) continue;
                try
                {
                    ObserveLifecycle(afterRender.OnAfterRenderAsync());
                }
                catch (Exception exception)
                {
                    ReportException(exception);
                }
            }
        }
        while (renderQueue.Count > 0);
    }

    private void RenderComponent(ComponentState state, RenderFragment fragment)
    {
        RenderTreeFrame[] frames;
        try
        {
            fragment(builder);
            frames = builder.ToArray(spareFrames);
            spareFrames = null;
            ValidateKeys(frames, 0, frames.Length);
        }
        catch (Exception exception)
        {
            ReportException(exception);
            return;
        }
        finally
        {
            builder.Clear();
        }
        var previous = state.Frames;
        state.Frames = frames;
        ReconcileChildren(state, state.Node, state.Node.LogicalChildren, previous, 0, previous.Length, frames, 0, frames.Length);
        Array.Clear(previous);
        spareFrames = previous;
        batch.RenderedComponents++;
        if (renderedSet.Add(state)) rendered.Add(state);
    }

    private ComponentState CreateComponentState(Type componentType, ComponentState? parent, Type? declaringScope, IComponent? declaringComponent = null)
    {
        if (parent is not null && typeof(WindowComponentBase).IsAssignableFrom(componentType))
            throw new InvalidOperationException($"Window component '{componentType.FullName}' cannot be nested. Use IWindowService.Open instead.");
        var component = Instantiate(componentType);
        if (component is WindowComponentBase) { declaringScope = componentType; declaringComponent = component; }
        if (declaringScope is null) declaringComponent ??= parent?.Component;

        for (var ancestor = parent; declaringComponent is null && ancestor is not null; ancestor = ancestor.Parent)
        {
            var type = ancestor.Component.GetType();
            if (declaringScope == type || type.IsGenericType && declaringScope == type.GetGenericTypeDefinition()) declaringComponent = ancestor.Component;
        }
        var state = new ComponentState(this, ++nextComponentId, component, parent, declaringScope, declaringComponent);
        state.Node = new ComponentNode(state);
        batch.AddedComponent(state.Node);
        components.Add(state.Id, state);
        component.Attach(new RenderHandle(this, state.Id));
        if (component is WindowComponentBase window) window.Connect();
        return state;
    }

    internal void SetWindowTitleBar(int id, RenderFragment? content)
    {
        if (!components.TryGetValue(id, out var state)) return;
        var root = state;
        while (root.Parent is not null) root = root.Parent;
        if (root.Component is not WindowComponentBase window || state.DeclaringComponent != window)
            throw new InvalidOperationException("UserTitleBar must be declared in the Nazor file containing @window.");
        window.RegisterTitleBar(id, content);
    }

    internal void SetWindowCaptionButtons(int id, RenderFragment? left, RenderFragment? right)
    {
        if (!components.TryGetValue(id, out var state)) return;
        var root = state;
        while (root.Parent is not null) root = root.Parent;
        if (root.Component is not WindowComponentBase window || state.DeclaringComponent != window)
            throw new InvalidOperationException("UserCaptionButtons must be declared in the Nazor file containing @window.");
        window.RegisterCaptionButtons(id, left, right);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Nazor.Sdk preserves component constructors in components.xml.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Nazor.Sdk preserves all component injection properties in components.xml.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Nazor.Sdk preserves component base types in components.xml.")]
    private IComponent Instantiate(Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType) || componentType.IsAbstract)
            throw new InvalidOperationException($"'{componentType.FullName}' is not a concrete component type.");
        var component = (IComponent)(Activator.CreateInstance(componentType)
            ?? throw new InvalidOperationException($"Unable to create '{componentType.FullName}'."));
        for (var type = componentType; type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.GetCustomAttribute<InjectAttribute>() is null) continue;
                var service = (property.PropertyType == typeof(IWindow) ? windowContext : null) ?? services?.GetService(property.PropertyType)
                    ?? throw new InvalidOperationException(
                        $"Cannot provide a value for property '{property.Name}' on type '{componentType.FullName}': no service of type '{property.PropertyType.FullName}' is registered.");
                property.SetValue(component, service);
            }
        }
        return component;
    }

    private void ValidateKeys(RenderTreeFrame[] frames, int start, int end)
    {
        HashSet<object>? keys = null;
        try
        {
            for (var i = start; i < end; i += Math.Max(frames[i].SubtreeLength, 1))
            {
                ref readonly var frame = ref frames[i];
                if (frame.Key is { } key)
                {
                    if (keys is null) { keys = availableKeys ?? []; availableKeys = null; }
                    if (!keys.Add(key))
                        throw new InvalidOperationException($"More than one sibling has the same @key value '{key}'. Key values must be unique among siblings.");
                }
                if (frame.FrameType is RenderTreeFrameType.Element or RenderTreeFrameType.Region)
                    ValidateKeys(frames, i + 1, i + frame.SubtreeLength);
            }
        }
        finally
        {
            if (keys is not null)
            {
                if (keys.Count <= 1024) availableKeys = keys;
                keys.Clear();
            }
        }
    }

    private void ReconcileChildren(ComponentState owner, Node parent, List<Node> nodes,
        RenderTreeFrame[] oldFrames, int oldStart, int oldEnd, RenderTreeFrame[] newFrames, int newStart, int newEnd)
    {
        if (ChildrenMatch(oldFrames, oldStart, oldEnd, newFrames, newStart, newEnd))
        {
            int o = oldStart, n = newStart;
            while (true)
            {
                o = NextChild(oldFrames, o, oldEnd);
                n = NextChild(newFrames, n, newEnd);
                if (n == newEnd) return;
                var node = oldFrames[o].Node!;
                UpdateNode(owner, node, oldFrames, o, newFrames, n);
                newFrames[n].Node = node;
                o += Math.Max(oldFrames[o].SubtreeLength, 1);
                n += Math.Max(newFrames[n].SubtreeLength, 1);
            }
        }
        var oldChildren = ChildIndices(oldFrames, oldStart, oldEnd);
        var newChildren = ChildIndices(newFrames, newStart, newEnd);
        var matches = new int[newChildren.Count];
        var used = new bool[oldChildren.Count];
        Dictionary<object, int>? oldKeys = null;
        for (var o = 0; o < oldChildren.Count; o++)
            if (oldFrames[oldChildren[o]].Key is { } key) (oldKeys ??= []).TryAdd(key, o);

        var cursor = 0;
        for (var n = 0; n < newChildren.Count; n++)
        {
            ref readonly var frame = ref newFrames[newChildren[n]];
            matches[n] = -1;
            if (frame.Key is { } key)
            {
                if (oldKeys is not null && oldKeys.TryGetValue(key, out var o) && !used[o] && Compatible(oldFrames, oldChildren[o], newFrames, newChildren[n]))
                {
                    matches[n] = o;
                    used[o] = true;
                }
                continue;
            }

            for (var o = cursor; o < oldChildren.Count; o++)
            {
                ref readonly var old = ref oldFrames[oldChildren[o]];
                if (used[o] || old.Key is not null || old.Sequence != frame.Sequence || !Compatible(oldFrames, oldChildren[o], newFrames, newChildren[n])) continue;
                matches[n] = o;
                used[o] = true;
                cursor = o + 1;
                break;
            }
        }

        var structureChanged = oldChildren.Count != newChildren.Count;
        for (var o = 0; o < oldChildren.Count; o++)
        {
            if (used[o]) continue;
            RemoveNode(oldFrames[oldChildren[o]].Node!);
            structureChanged = true;
        }

        nodes.Clear();
        var lastMatched = -1;
        for (var n = 0; n < newChildren.Count; n++)
        {
            var newIndex = newChildren[n];
            Node node;
            if (matches[n] >= 0)
            {
                var oldIndex = oldChildren[matches[n]];
                node = oldFrames[oldIndex].Node!;
                if (matches[n] < lastMatched) structureChanged = true;
                lastMatched = matches[n];
                UpdateNode(owner, node, oldFrames, oldIndex, newFrames, newIndex);
            }
            else
            {
                node = CreateNode(owner, parent, newFrames, newIndex);
                structureChanged = true;
            }
            newFrames[newIndex].Node = node;
            nodes.Add(node);
        }
        if (structureChanged) ChildrenChanged(parent);
    }

    private bool ChildrenMatch(RenderTreeFrame[] oldFrames, int o, int oldEnd, RenderTreeFrame[] newFrames, int n, int newEnd)
    {
        while (true)
        {
            o = NextChild(oldFrames, o, oldEnd);
            n = NextChild(newFrames, n, newEnd);
            if (o == oldEnd || n == newEnd) return o == oldEnd && n == newEnd;
            if (!Equals(oldFrames[o].Key, newFrames[n].Key) ||
                (newFrames[n].Key is null && oldFrames[o].Sequence != newFrames[n].Sequence) ||
                !Compatible(oldFrames, o, newFrames, n)) return false;
            o += Math.Max(oldFrames[o].SubtreeLength, 1);
            n += Math.Max(newFrames[n].SubtreeLength, 1);
        }
    }

    private static int NextChild(RenderTreeFrame[] frames, int index, int end)
    {
        while (index < end && frames[index].FrameType is not (RenderTreeFrameType.Element or RenderTreeFrameType.Text or RenderTreeFrameType.Component or RenderTreeFrameType.Region))
            index += Math.Max(frames[index].SubtreeLength, 1);
        return index;
    }

    private static List<int> ChildIndices(RenderTreeFrame[] frames, int start, int end)
    {
        var result = new List<int>();
        for (var i = start; i < end; i += Math.Max(frames[i].SubtreeLength, 1))
        {
            if (frames[i].FrameType is RenderTreeFrameType.Element or RenderTreeFrameType.Text or RenderTreeFrameType.Component or RenderTreeFrameType.Region)
                result.Add(i);
        }
        return result;
    }

    private bool Compatible(RenderTreeFrame[] oldFrames, int oldIndex, RenderTreeFrame[] newFrames, int newIndex)
    {
        ref readonly var old = ref oldFrames[oldIndex];
        ref readonly var frame = ref newFrames[newIndex];
        if (old.FrameType != frame.FrameType) return false;
        return frame.FrameType switch
        {
            RenderTreeFrameType.Element => old.Name == frame.Name && ReferenceEquals(old.DeclaringComponent, frame.DeclaringComponent),
            RenderTreeFrameType.Component => old.ComponentType == frame.ComponentType && old.DeclaringScope == frame.DeclaringScope &&
                ReferenceEquals(old.DeclaringComponent, frame.DeclaringComponent) &&
                (!IsRenderingOnMetadataUpdate || !LostParameters(oldFrames, oldIndex, newFrames, newIndex)),
            _ => true,
        };
    }

    private static bool LostParameters(RenderTreeFrame[] oldFrames, int oldIndex, RenderTreeFrame[] newFrames, int newIndex)
    {
        var oldEnd = oldIndex + oldFrames[oldIndex].SubtreeLength;
        var newEnd = newIndex + newFrames[newIndex].SubtreeLength;
        for (var o = oldIndex + 1; o < oldEnd; o++)
        {
            if (oldFrames[o].FrameType != RenderTreeFrameType.Attribute) continue;
            var found = false;
            for (var n = newIndex + 1; n < newEnd && !found; n++)
                found = newFrames[n].FrameType == RenderTreeFrameType.Attribute && newFrames[n].Name == oldFrames[o].Name;
            if (!found) return true;
        }
        return false;
    }

    private void UpdateNode(ComponentState owner, Node node, RenderTreeFrame[] oldFrames, int oldIndex, RenderTreeFrame[] newFrames, int newIndex)
    {
        ref readonly var frame = ref newFrames[newIndex];
        switch (frame.FrameType)
        {
            case RenderTreeFrameType.Text:
                var text = (TextNode)node;
                if (text.Text != frame.TextContent)
                {
                    text.Text = frame.TextContent ?? string.Empty;
                    batch.UpdatedText(text);
                }
                break;
            case RenderTreeFrameType.Element:
                var element = (ElementNode)node;
                element.Key = frame.Key;
                UpdateAttributes(element, newFrames, newIndex);
                ReconcileChildren(owner, element, element.LogicalChildren, oldFrames, oldIndex + 1, oldIndex + oldFrames[oldIndex].SubtreeLength,
                    newFrames, newIndex + 1, newIndex + frame.SubtreeLength);
                break;
            case RenderTreeFrameType.Component:
                ((ComponentNode)node).Key = frame.Key;
                if (IsRenderingOnMetadataUpdate || ParametersMayHaveChanged(oldFrames, oldIndex, newFrames, newIndex))
                    ((ComponentNode)node).State.SetDirectParameters(Parameters(newFrames, newIndex));
                break;
            case RenderTreeFrameType.Region:
                ReconcileChildren(owner, node, node.LogicalChildren!, oldFrames, oldIndex + 1, oldIndex + oldFrames[oldIndex].SubtreeLength,
                    newFrames, newIndex + 1, newIndex + frame.SubtreeLength);
                break;
        }
    }

    private Node CreateNode(ComponentState owner, Node parent, RenderTreeFrame[] frames, int index)
    {
        ref readonly var frame = ref frames[index];
        switch (frame.FrameType)
        {
            case RenderTreeFrameType.Text:
                var text = new TextNode(frame.TextContent ?? string.Empty) { Parent = parent };
                batch.AddedText(text);
                return text;
            case RenderTreeFrameType.Element:
                var element = new ElementNode(frame.Name!, owner.DeclaringScope, frame.DeclaringComponent ?? owner.Component, owner.DeclaringComponent) { Parent = parent, Key = frame.Key };
                for (var i = index + 1; i < index + frame.SubtreeLength && frames[i].FrameType == RenderTreeFrameType.Attribute; i++)
                    element.SetAttribute(frames[i].Name!, frames[i].AttributeValue);
                batch.Added(element);
                ReconcileChildren(owner, element, element.LogicalChildren, [], 0, 0, frames, index + 1, index + frame.SubtreeLength);
                for (var i = index + 1; i < index + frame.SubtreeLength; i += Math.Max(frames[i].SubtreeLength, 1))
                    if (frames[i].FrameType == RenderTreeFrameType.ElementReferenceCapture)
                        frames[i].ElementReferenceCaptureAction!(new ElementReference(element));
                return element;
            case RenderTreeFrameType.Component:
                ComponentState state;
                try
                {
                    state = CreateComponentState(frame.ComponentType!, owner, frame.DeclaringScope, frame.DeclaringComponent);
                }
                catch (Exception exception)
                {

                    ReportException(exception);
                    return new RegionNode { Parent = parent };
                }
                state.Node.Parent = parent;
                state.Node.Key = frame.Key;
                for (var i = index + 1; i < index + frame.SubtreeLength; i++)
                {
                    if (frames[i].FrameType != RenderTreeFrameType.ComponentReferenceCapture) continue;
                    try { frames[i].ComponentReferenceCaptureAction!(state.Component); }
                    catch (Exception exception) { ReportException(exception); }
                }
                state.SetDirectParameters(Parameters(frames, index));
                return state.Node;
            default:
                var region = new RegionNode { Parent = parent };
                ReconcileChildren(owner, region, region.LogicalChildren, [], 0, 0, frames, index + 1, index + frame.SubtreeLength);
                return region;
        }
    }

    private void UpdateAttributes(ElementNode element, RenderTreeFrame[] frames, int index)
    {
        var end = index + frames[index].SubtreeLength;
        var attributeEnd = index + 1;
        for (var i = index + 1; i < end && frames[i].FrameType == RenderTreeFrameType.Attribute; i++)
        {
            var name = frames[i].Name!;
            attributeEnd = i + 1;
            var value = frames[i].AttributeValue;
            if (!element.Attributes.TryGetValue(name, out var previous) || !Equals(previous, value)) batch.Updated(element, name);
            element.SetAttribute(name, value);
        }
        List<string>? removed = null;
        foreach (var name in element.AttributeValues.Keys)
        {
            var present = false;
            for (var i = index + 1; i < attributeEnd; i++)
                if (frames[i].Name == name) { present = true; break; }
            if (!present) (removed ??= []).Add(name);
        }
        foreach (var name in removed ?? [])
        {
            element.RemoveAttribute(name);
            batch.Updated(element, name);
        }
    }

    private static ParameterView Parameters(RenderTreeFrame[] frames, int index)
    {
        var values = new List<ParameterValue>();
        for (var i = index + 1; i < index + frames[index].SubtreeLength; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Attribute) values.Add(new(frames[i].Name!, frames[i].AttributeValue));
        return ParameterView.FromValues(values.ToArray());
    }

    private static bool ParametersMayHaveChanged(RenderTreeFrame[] oldFrames, int oldIndex, RenderTreeFrame[] newFrames, int newIndex)
    {
        var oldEnd = oldIndex + oldFrames[oldIndex].SubtreeLength;
        var newEnd = newIndex + newFrames[newIndex].SubtreeLength;
        int o = oldIndex + 1, n = newIndex + 1;
        while (true)
        {
            while (o < oldEnd && oldFrames[o].FrameType != RenderTreeFrameType.Attribute) o++;
            while (n < newEnd && newFrames[n].FrameType != RenderTreeFrameType.Attribute) n++;
            if (o >= oldEnd || n >= newEnd) return o < oldEnd || n < newEnd;
            if (oldFrames[o].Name != newFrames[n].Name || MayHaveChanged(oldFrames[o].AttributeValue, newFrames[n].AttributeValue)) return true;
            o++;
            n++;
        }
    }

    internal static bool MayHaveChanged(object? oldValue, object? newValue)
    {
        if (oldValue is null || newValue is null) return oldValue is not null || newValue is not null;
        var type = oldValue.GetType();
        if (type != newValue.GetType() || !IsKnownImmutable(type)) return true;
        return !oldValue.Equals(newValue);
    }

    private static bool IsKnownImmutable(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || typeof(Type).IsAssignableFrom(type) ||
        type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));

    private void RemoveNode(Node node)
    {
        switch (node)
        {
            case ComponentNode component:
                DisposeComponent(component.State);
                foreach (var child in component.LogicalChildren) RemoveNode(child);
                batch.Removed(component);
                break;
            case ElementNode element:
                foreach (var child in element.LogicalChildren) RemoveNode(child);
                batch.Removed(element);
                break;
            case TextNode text:
                batch.Removed(text);
                break;
            default:
                foreach (var child in node.LogicalChildren ?? []) RemoveNode(child);
                break;
        }
        node.IsRemoved = true;
    }

    private void DisposeComponent(ComponentState state)
    {
        if (state.IsDisposed) return;
        state.IsDisposed = true;
        state.Unsubscribe();
        if (state.Component is WindowComponentBase window) window.Disconnect();
        if (state.DeclaringComponent is WindowComponentBase owner)
        {
            owner.RegisterTitleBar(state.Id, null);
            owner.RegisterCaptionButtons(state.Id, null, null);
        }
        components.Remove(state.Id);
        try
        {
            if (state.Component is IAsyncDisposable asyncDisposable) ObserveLifecycle(asyncDisposable.DisposeAsync().AsTask());
            else if (state.Component is IDisposable disposable) disposable.Dispose();
        }
        catch (Exception exception)
        {
            ReportException(exception);
        }
    }

    private void ChildrenChanged(Node parent)
    {
        for (Node? current = parent; current is not null; current = current.Parent)
        {
            if (current is ElementNode element)
            {
                element.MarkChildrenDirty();
                batch.ChildrenOf(element);
                return;
            }
            if (current is ComponentNode { Parent: null, RootOwner: { } root })
            {
                root.MarkNodesDirty();
                batch.RootNodesChanged = true;
                return;
            }
        }
    }

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        if (disposed) return;
        Dispatcher.AssertAccess();
        try
        {
            foreach (var root in roots.ToArray()) RemoveRootComponent(root);
        }
        finally
        {
            disposed = true;
            renderQueue.Clear();
            spareFrames = null;
            availableKeys = null;
        }
        GC.SuppressFinalize(this);
    }
}
