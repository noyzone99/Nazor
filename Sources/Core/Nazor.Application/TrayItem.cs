using Nazor.Platform;

namespace Nazor;

/// <summary>One application tray item. Native operations and callbacks run on Dispatcher during Run.</summary>
public interface ITrayItem : IDisposable
{
    /// <summary>Gets or sets the requested icon; null selects the application default.</summary>
    ImageSource? Icon { get; set; }
    /// <summary>Gets or sets the nonempty tooltip, limited to 127 UTF-16 units.</summary>
    string ToolTip { get; set; }
    /// <summary>Gets or sets the typed immutable menu.</summary>
    SystemTrayMenu Menu { get; set; }
    /// <summary>Gets whether a tray host is currently available.</summary>
    bool IsSupported { get; }
    /// <summary>Gets visibility known to the backend; shell overflow placement is not guaranteed.</summary>
    bool IsVisible { get; }
    /// <summary>Gets whether the framework has started this instance and has not stopped it.</summary>
    bool IsStarted { get; }
    /// <summary>Gets the UI dispatcher after startup.</summary>
    Dispatcher Dispatcher { get; }
    /// <summary>Occurs after initial publication and the startup hook complete.</summary>
    event Action? Started;
    /// <summary>Occurs after native resources are detached.</summary>
    event Action? Stopped;
    /// <summary>Occurs for primary activation, typically a left click or keyboard activation.</summary>
    event Action? Activated;
    /// <summary>Occurs for alternative activation, typically a middle click, when reported by the desktop.</summary>
    event Action? SecondaryActivated;
    /// <summary>Occurs when the backend detects a visibility change.</summary>
    event Action? VisibilityChanged;
    /// <summary>Occurs synchronously before menu presentation when reported by the desktop. Cancellation is backend-dependent.</summary>
    event EventHandler<TrayMenuOpeningEventArgs>? MenuOpening;
    /// <summary>Publishes the current icon and menu. True means accepted by the shell, not guaranteed placement.</summary>
    bool Show();
    /// <summary>Removes the native icon while retaining the managed item and its state.</summary>
    void Hide();
    /// <summary>Attaches a runtime context and starts this item once. Called by the framework on the UI thread.</summary>
    void Start(TrayItemContext context);
    /// <summary>Stops this item permanently. Called by the framework on the UI thread; repeated calls do nothing.</summary>
    void Stop();
}

/// <summary>Framework-managed tray lifetime, independent of windows. Hide preserves the instance; Stop is terminal.</summary>
public abstract class TrayItem : ITrayItem
{
    private TrayItemContext? context;
    private ImageSource? icon;
    private string toolTip = "Nazor";
    private SystemTrayMenu menu = SystemTrayMenu.Empty;
    private bool startedOnce, stopping, disposed;
    private bool requestedVisible;
    /// <summary>Gets or sets the requested icon. Null selects the application default; image loading is asynchronous.</summary>
    public ImageSource? Icon { get => icon; set { AssertMutable(); if (Equals(icon, value)) return; icon = value; Refresh(); } }
    /// <summary>Gets or sets the nonempty tooltip, limited to 127 UTF-16 units.</summary>
    public string ToolTip
    {
        get => toolTip;
        set
        {
            AssertMutable();
            SystemTrayLabeledItem.ValidateText(value, nameof(value));
            if (value.Length > 127) throw new ArgumentException("Tray tooltips support at most 127 UTF-16 units.", nameof(value));
            toolTip = value;
            Refresh();
        }
    }
    /// <summary>Gets or sets the typed menu, updating a visible item.</summary>
    public SystemTrayMenu Menu
    {
        get => menu;
        set { AssertMutable(); ArgumentNullException.ThrowIfNull(value); context?.Validate(value); menu = value; Refresh(); }
    }
    /// <summary>Gets whether a tray host is currently available.</summary>
    public bool IsSupported => context?.IsSupported is true;
    /// <summary>Gets visibility known to the backend; shell overflow placement is not guaranteed.</summary>
    public bool IsVisible => context?.IsVisible is true;
    /// <summary>Gets whether the framework has started this instance and has not stopped it.</summary>
    public bool IsStarted { get; private set; }
    /// <summary>Gets the UI dispatcher. It is available after the runtime context is attached.</summary>
    public Dispatcher Dispatcher => context?.Dispatcher ?? throw new InvalidOperationException("The tray item has not started.");
    /// <summary>Occurs after initial publication and the startup hook complete.</summary>
    public event Action? Started;
    /// <summary>Occurs after native resources are detached.</summary>
    public event Action? Stopped;
    /// <summary>Occurs for primary activation, typically a left click or keyboard activation.</summary>
    public event Action? Activated;
    /// <summary>Occurs for alternative activation, typically a middle click, when reported by the desktop.</summary>
    public event Action? SecondaryActivated;
    /// <summary>Occurs when the backend detects a visibility change.</summary>
    public event Action? VisibilityChanged;
    /// <summary>Occurs synchronously before menu presentation when reported by the desktop. Cancellation is backend-dependent.</summary>
    public event EventHandler<TrayMenuOpeningEventArgs>? MenuOpening;

    /// <summary>Publishes the current icon and menu. True means accepted by the shell, not guaranteed placement.</summary>
    public bool Show()
    {
        AssertRunning();
        requestedVisible = true;
        return context!.Show(icon, toolTip, menu);
    }
    /// <summary>Removes the native icon while retaining the managed item and its state.</summary>
    public void Hide()
    {
        AssertRunning();
        requestedVisible = false;
        context!.Hide();
    }
    private void Refresh() { if (IsStarted && !stopping && requestedVisible) Show(); }
    private void AssertMutable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        context?.Dispatcher.AssertAccess();
        if (stopping || startedOnce && !IsStarted) throw new InvalidOperationException("The tray item has stopped.");
    }
    private void AssertRunning()
    {
        AssertMutable();
        if (!IsStarted) throw new InvalidOperationException("Tray operations are available during Run.");
    }

    /// <summary>Attaches a runtime context and starts this item once. Called by the framework on the UI thread.</summary>
    void ITrayItem.Start(TrayItemContext value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (startedOnce || disposed) throw new InvalidOperationException("A tray item can only start once.");
        value.Dispatcher.AssertAccess();
        startedOnce = true;
        context = value;
        context.Activated += HandleActivated;
        context.SecondaryActivated += HandleSecondaryActivated;
        context.VisibilityChanged += HandleVisibilityChanged;
        context.MenuOpening += HandleMenuOpening;
        IsStarted = true;
        Show();
        OnStarted();
        if (IsStarted && !stopping) Started?.Invoke();
    }
    /// <summary>Stops this item permanently. Called by the framework on the UI thread; repeated calls do nothing.</summary>
    void ITrayItem.Stop()
    {
        if (!IsStarted || stopping) return;
        Dispatcher.AssertAccess();
        stopping = true;
        try { OnStopping(); }
        finally
        {
            IsStarted = false;
            requestedVisible = false;
            context!.Activated -= HandleActivated;
            context.SecondaryActivated -= HandleSecondaryActivated;
            context.VisibilityChanged -= HandleVisibilityChanged;
            context.MenuOpening -= HandleMenuOpening;
            try { context.Dispose(); }
            finally { OnStopped(); Stopped?.Invoke(); }
        }
    }
    private void HandleActivated() { if (IsStarted && !stopping) { OnActivated(); Activated?.Invoke(); } }
    private void HandleSecondaryActivated() { if (IsStarted && !stopping) { OnSecondaryActivated(); SecondaryActivated?.Invoke(); } }
    private void HandleVisibilityChanged() { if (IsStarted && !stopping) { OnVisibilityChanged(); VisibilityChanged?.Invoke(); } }
    private void HandleMenuOpening(TrayMenuOpeningEventArgs args)
    {
        if (!IsStarted || stopping) return;
        OnMenuOpening(args);
        MenuOpening?.Invoke(this, args);
    }
    /// <summary>Runs after the initial show attempt; inspect IsVisible to provide fallback UI.</summary>
    protected virtual void OnStarted() { }
    /// <summary>Runs before native resources are detached. Release subscriptions here.</summary>
    protected virtual void OnStopping() { }
    /// <summary>Runs after native resources are detached, before the Stopped event.</summary>
    protected virtual void OnStopped() { }
    /// <summary>Handles primary activation before external subscribers are notified.</summary>
    protected virtual void OnActivated() { }
    /// <summary>Handles alternative activation before external subscribers are notified.</summary>
    protected virtual void OnSecondaryActivated() { }
    /// <summary>Handles detected shell visibility changes; not called during teardown.</summary>
    protected virtual void OnVisibilityChanged() { }
    /// <summary>Handles a synchronous menu-opening request before external subscribers. Cancel before returning.</summary>
    protected virtual void OnMenuOpening(TrayMenuOpeningEventArgs args) { }
    /// <summary>Stops and releases this instance. Repeated calls do nothing.</summary>
    protected virtual void Dispose(bool disposing) { }
    /// <summary>Stops and releases this instance. Repeated calls do nothing.</summary>
    public void Dispose()
    {
        if (disposed) return;
        if (IsStarted) Dispatcher.AssertAccess();
        try { ((ITrayItem)this).Stop(); }
        finally { disposed = true; Dispose(true); GC.SuppressFinalize(this); }
    }
}
