using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The state and the caption commands of the current window, for applications that draw their own minimize, maximize
/// and close buttons: it renders no element, only <see cref="ChildContent" />, which receives this component as its
/// context (<c>&lt;WindowCommands Context="window"&gt;</c>) and is rendered again whenever the window's state,
/// capabilities or system buttons change. <see cref="CaptionButtons" /> is built on it.
/// </summary>
public sealed class WindowCommands : ComponentBase, IDisposable
{
    private IWindow? subscribed;

    /// <summary>Gets or sets the window, supplied automatically.</summary>
    [Inject] public IWindow Window { get; set; } = null!;

    /// <summary>Gets or sets the content that uses the window's state and commands.</summary>
    [Parameter] public RenderFragment<WindowCommands>? ChildContent { get; set; }

    /// <summary>Gets the window state.</summary>
    public WindowState State => Window.State;

    /// <summary>Gets whether the window is maximized; the maximize command then restores it.</summary>
    public bool IsMaximized => Window.State == WindowState.Maximized;

    /// <summary>Gets whether the window is in full screen.</summary>
    public bool IsFullScreen => Window.State == WindowState.FullScreen;

    /// <summary>Gets whether the window can be minimized.</summary>
    public bool CanMinimize => Window.Capabilities.CanMinimize;

    /// <summary>Gets whether the maximize command is available: the window can be maximized, or is maximized and can be restored.</summary>
    public bool CanMaximize => Window.Capabilities.CanMaximize || IsMaximized;

    /// <summary>Gets whether the window can enter full screen.</summary>
    public bool CanEnterFullScreen => Window.Capabilities.CanEnterFullScreen;

    /// <summary>
    /// Gets whether the window shows the system's own caption buttons in its custom title bar, in which case drawing
    /// other caption buttons would duplicate them.
    /// </summary>
    public bool ShowsSystemButtons => Window.AppliedDecorations?.Configuration.CaptionButtons == WindowCaptionButtons.Native;

    /// <summary>Gets the system's placement of the caption buttons, in physical left-to-right order on each side.</summary>
    public WindowButtonsLayout Layout => Window.SystemButtonsLayout;

    /// <summary>Subscribes to the window's changes.</summary>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(subscribed, Window)) return;
        Unsubscribe();
        subscribed = Window;
        subscribed.StateChanged += Refresh;
        subscribed.CapabilitiesChanged += Refresh;
        subscribed.SystemButtonsChanged += Refresh;
        subscribed.Changed += Refresh;
    }

    /// <summary>Renders the child content with this component as its context.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null) builder.AddContent(0, ChildContent(this));
    }

    /// <summary>Minimizes the window when it can be minimized.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task MinimizeAsync()
    {
        if (CanMinimize) Window.Minimize();
        return Task.CompletedTask;
    }

    /// <summary>Maximizes the window when it can be maximized.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task MaximizeAsync()
    {
        if (Window.Capabilities.CanMaximize) Window.Maximize();
        return Task.CompletedTask;
    }

    /// <summary>Restores the window from the maximized, minimized or full-screen state.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task RestoreAsync()
    {
        if (Window.State != WindowState.Normal) Window.Restore();
        return Task.CompletedTask;
    }

    /// <summary>Restores a maximized window, or maximizes it when it can be maximized.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task ToggleMaximizeAsync() => IsMaximized ? RestoreAsync() : MaximizeAsync();

    /// <summary>Enters full screen, or leaves it when the window is already in full screen.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task ToggleFullScreenAsync()
    {
        if (IsFullScreen) Window.ExitFullScreen();
        else if (CanEnterFullScreen) Window.EnterFullScreen();
        return Task.CompletedTask;
    }

    /// <summary>Closes the window.</summary>
    /// <returns>A completed task, so the method can be an event callback.</returns>
    public Task CloseAsync()
    {
        Window.Close();
        return Task.CompletedTask;
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    private void Unsubscribe()
    {
        if (subscribed is null) return;
        subscribed.StateChanged -= Refresh;
        subscribed.CapabilitiesChanged -= Refresh;
        subscribed.SystemButtonsChanged -= Refresh;
        subscribed.Changed -= Refresh;
        subscribed = null;
    }

    /// <summary>Stops following the window.</summary>
    public void Dispose() => Unsubscribe();
}
