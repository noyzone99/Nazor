namespace Nazor.Platform.Linux;

/// <summary>One DRM/KMS display surface, libinput devices and a local seat. All operations except Quit own the creating UI thread.</summary>
public sealed class DrmPlatform : ISingleViewPlatform, IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly DrmOptions options;
    private readonly IDrmBackend backend;
    private DrmView? view;
    private int quitting, exitCode;
    private bool created, running, disposed;

    /// <inheritdoc />
    public string BackendName => "DRM/KMS";

    /// <summary>Acquires the configured display and active seat. Does not create application content.</summary>
    /// <param name="options">Display selection and logical scale; null uses the first connected output at scale one.</param>
    /// <exception cref="PlatformNotSupportedException">The system is not 64-bit Linux.</exception>
    /// <exception cref="InvalidOperationException">Native libraries, the seat or a connected output are unavailable.</exception>
    public DrmPlatform(DrmOptions? options = null)
    {
        this.options = (options ?? new()).Validate() with { };
        backend = new DrmBackend(this.options);
        Attach();
    }

    internal DrmPlatform(DrmOptions options, IDrmBackend backend)
    {
        this.options = options.Validate(); this.backend = backend;
        Attach();
    }

    private void Attach()
    {
        backend.Input = value => view?.OnInput(value);
        backend.ActiveChanged = active => view?.SetSessionActive(active);
    }

    /// <summary>DRM has no desktop window operations. Its surface already fills the configured display.</summary>
    public WindowCapabilities Capabilities => default;

    /// <summary>Creates the application's single surface. Its dimensions come from the selected display mode.</summary>
    /// <param name="host">UI-thread rendering, input and lifecycle callbacks.</param>
    /// <returns>The display surface, owned by the application.</returns>
    /// <exception cref="InvalidOperationException">A surface has already been created.</exception>
    public IPlatformWindow CreateView(IWindowHost host)
    {
        AssertAccess(); ArgumentNullException.ThrowIfNull(host);
        if (created) throw new InvalidOperationException("The DRM backend supports one application view.");
        view = new(this, backend, host, options.Scale);
        created = true;
        return view;
    }

    /// <summary>Rejects desktop window creation. Configure the application with UseMainView instead.</summary>
    /// <param name="host">The requested window callbacks.</param>
    /// <param name="options">The requested desktop window configuration.</param>
    /// <returns>This method always throws.</returns>
    /// <exception cref="PlatformNotSupportedException">DRM has no desktop windows.</exception>
    public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
    {
        AssertAccess();
        throw new PlatformNotSupportedException("The DRM backend requires UseMainView and cannot open desktop windows or dialogs.");
    }

    /// <summary>Runs dispatcher work, input and display frames on the creating thread until Quit is called.</summary>
    /// <param name="dispatcher">The UI dispatcher owned by the calling thread.</param>
    /// <returns>The exit code supplied to Quit.</returns>
    public int Run(UIDispatcher dispatcher)
    {
        AssertAccess(); ArgumentNullException.ThrowIfNull(dispatcher); dispatcher.AssertAccess();
        if (running) throw new InvalidOperationException("The DRM event loop is already running.");
        running = true;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(dispatcher.SynchronizationContext);
        dispatcher.WorkPosted += backend.Wake;
        try
        {
            while (Volatile.Read(ref quitting) == 0)
            {
                dispatcher.RunPending();
                backend.Dispatch(0);
                var timeout = view?.Tick() ?? -1;
                if (Volatile.Read(ref quitting) == 0) backend.Dispatch(dispatcher.HasPendingWork ? 0 : timeout);
            }
            return exitCode;
        }
        finally
        {
            dispatcher.WorkPosted -= backend.Wake;
            SynchronizationContext.SetSynchronizationContext(previous);
            running = false;
        }
    }

    /// <summary>Requests loop termination and wakes a blocked dispatcher. May be called from another thread.</summary>
    /// <param name="code">The application exit code.</param>
    public void Quit(int code = 0)
    {
        Volatile.Write(ref exitCode, code); Volatile.Write(ref quitting, 1); backend.Wake();
    }

    internal void Wake() => backend.Wake();
    internal void AssertAccess()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("DRM operations must run on the creating UI thread.");
    }

    /// <summary>Drains rendering before releasing input, display, seat and console resources on the UI thread.</summary>
    public void Dispose()
    {
        if (disposed) return;
        AssertAccess();
        try { view?.Dispose(); }
        finally { disposed = true; backend.Dispose(); }
    }
}
