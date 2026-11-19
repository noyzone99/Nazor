using Nazor.Rendering;

namespace Nazor;

/// <summary>
/// Base class for components written in <c>.nazor</c> files. Lifecycle and re-render rules follow Blazor's
/// ComponentBase: events handled by the component and parameter updates re-render it; changes made elsewhere (timers,
/// services) must call <see cref="StateHasChanged" /> through <see cref="InvokeAsync(Action)" />.
/// </summary>
public abstract class ComponentBase : IComponent, IHandleEvent, IHandleAfterRender
{
    /// <summary>Registers a window-owned caption declaration; null removes it during disposal.</summary>
    protected void SetWindowTitleBar(RenderFragment? content) => renderHandle.SetWindowTitleBar(content);

    /// <summary>Registers window-owned physical left and right caption-button fragments; null fragments remove the declaration.</summary>
    /// <param name="left">The physical left-side controls.</param>
    /// <param name="right">The physical right-side controls.</param>
    protected void SetWindowCaptionButtons(RenderFragment? left, RenderFragment? right) => renderHandle.SetWindowCaptionButtons(left, right);

    /// <summary>Opens a delegated component with this wrapper's original declaring markup scope and instance.</summary>
    /// <typeparam name="TComponent">The component used to implement this transparent wrapper.</typeparam>
    /// <param name="builder">The builder receiving the component frame.</param>
    /// <param name="sequence">The source-order sequence number.</param>
    protected void OpenDelegatedComponent<TComponent>(RenderTreeBuilder builder, int sequence) where TComponent : notnull, IComponent =>
        renderHandle.OpenDelegatedComponent<TComponent>(builder, sequence);
    private readonly RenderFragment renderFragment;
    private RenderHandle renderHandle;
    private bool initialized;
    private bool hasNeverRendered = true;
    private bool hasPendingQueuedRender;
    private bool hasCalledOnAfterRender;

    /// <summary>Initializes a new instance of ComponentBase.</summary>
    protected ComponentBase()
    {
        renderFragment = builder =>
        {
            hasPendingQueuedRender = false;
            hasNeverRendered = false;
            BuildRenderTree(builder);
        };
    }

    /// <summary>Writes the render frames for ComponentBase to the supplied builder.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    /// <summary>Initializes component state before the first parameter update completes.</summary>
    protected virtual void OnInitialized()
    {
    }

    /// <summary>Asynchronously initializes component state before the first parameter update completes.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after every parameter assignment, including the first. After a .NET Hot Reload delta every component
    /// receives its parameters again, so this runs once more per delta.
    /// </summary>
    protected virtual void OnParametersSet()
    {
    }

    /// <summary>
    /// Asynchronous counterpart of <see cref="OnParametersSet" />. After a hot reload delta it runs again and may
    /// overlap with a previous invocation still in progress, as in Blazor.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected virtual Task OnParametersSetAsync() => Task.CompletedTask;

    /// <summary>
    /// Whether a <see cref="StateHasChanged" /> renders; ignored for the first render and while a hot reload delta is
    /// applied.
    /// </summary>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    protected virtual bool ShouldRender() => true;

    /// <summary>Notifies the component after its rendered output has been applied.</summary>
    /// <param name="firstRender">Whether this is the first completed render of the component.</param>
    protected virtual void OnAfterRender(bool firstRender)
    {
    }

    /// <summary>Asynchronously notifies the component after its rendered output has been applied.</summary>
    /// <param name="firstRender">Whether this is the first completed render of the component.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

    /// <summary>Queues a render unless one is already pending. Must run on the dispatcher.</summary>
    protected void StateHasChanged()
    {
        if (hasPendingQueuedRender) return;
        if (hasNeverRendered || ShouldRender() || renderHandle.IsRenderingOnMetadataUpdate)
        {
            hasPendingQueuedRender = true;
            try
            {
                renderHandle.Render(renderFragment);
            }
            catch
            {
                hasPendingQueuedRender = false;
                throw;
            }
        }
    }

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected Task InvokeAsync(Action workItem) => renderHandle.Dispatcher.InvokeAsync(workItem);

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected Task InvokeAsync(Func<Task> workItem) => renderHandle.Dispatcher.InvokeAsync(workItem);

    void IComponent.Attach(RenderHandle handle)
    {
        if (renderHandle.IsInitialized)
            throw new InvalidOperationException($"The component '{GetType().FullName}' is already attached to a renderer.");
        renderHandle = handle;
    }

    /// <summary>Assigns component parameters and runs the initialization and parameter lifecycle callbacks.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (!initialized)
        {
            initialized = true;
            return RunInitAndSetParametersAsync();
        }
        return CallOnParametersSetAsync();
    }

    private async Task RunInitAndSetParametersAsync()
    {
        OnInitialized();
        var task = OnInitializedAsync();
        if (task.Status is not TaskStatus.RanToCompletion and not TaskStatus.Canceled)
        {

            StateHasChanged();
            try
            {
                await task;
            }
            catch when (task.IsCanceled)
            {
            }
        }
        await CallOnParametersSetAsync();
    }

    private Task CallOnParametersSetAsync()
    {
        OnParametersSet();
        var task = OnParametersSetAsync();
        var shouldAwait = task.Status is not TaskStatus.RanToCompletion and not TaskStatus.Canceled;
        StateHasChanged();
        return shouldAwait ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
    }

    private async Task CallStateHasChangedOnAsyncCompletion(Task task)
    {
        try
        {
            await task;
        }
        catch when (task.IsCanceled)
        {
            return;
        }
        StateHasChanged();
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        var task = callback.InvokeAsync(arg);
        var shouldAwait = task.Status is not TaskStatus.RanToCompletion and not TaskStatus.Canceled;
        StateHasChanged();
        return shouldAwait ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        var firstRender = !hasCalledOnAfterRender;
        hasCalledOnAfterRender = true;
        OnAfterRender(firstRender);
        return OnAfterRenderAsync(firstRender);
    }
}
