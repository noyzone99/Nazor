using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Nazor.Input;
using static Nazor.Platform.Linux.LinuxNativeApi;

namespace Nazor.Platform.Linux;

/// <summary>Shared native Linux desktop backend. Owns a compositor connection, native windows and the UI event loop.</summary>
/// <remarks>All operations except Quit must run on the creating thread. The system must provide the selected display protocol.
/// The compositor or window manager determines effective window capabilities.</remarks>
public abstract unsafe partial class LinuxDesktopPlatform : IPlatform, IDisposable
{
    internal LinuxNativeApi Native { get; }
    internal bool IsX11 { get; }
    /// <inheritdoc />
    public string BackendName { get; }
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly Dictionary<nint, LinuxDesktopWindow> windows = [];
    private readonly object lifetimeGate = new();
    private readonly ConcurrentQueue<(Action Run, Action? Cancel)> posted = new();
    private readonly LinuxDesktopClipboard clipboard;
    private nint connection;
    private int quitting, exitCode;
    private bool running, disposing;
    private UIDispatcher? dispatcher;
    private LinuxDesktopIntegration? desktop;

    /// <summary>Connects to the selected native desktop display.</summary>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Linux.</exception>
    /// <exception cref="InvalidOperationException">The bridge ABI or compositor connection is unavailable.</exception>
    private protected unsafe LinuxDesktopPlatform(string backend, string library)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException($"The {backend} backend requires Linux.");
        if (RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64))
            throw new PlatformNotSupportedException($"The Nazor {backend} runtime requires an x64 or ARM64 Linux process.");
        IsX11 = backend == "X11";
        BackendName = backend;
        try { Native = new(library); }
        catch (DllNotFoundException error)
        {
            throw new InvalidOperationException($"Cannot load the Nazor {backend} bridge. Install its matching native asset and system display libraries.", error);
        }
        try
        {
            if (Native.nz_abi_version() != 5 || Native.nz_event_size() != (nuint)sizeof(Event))
                throw new InvalidOperationException($"The Nazor {backend} bridge ABI does not match the managed backend.");
            Span<byte> error = stackalloc byte[512];
            fixed (byte* pointer = error)
            {
                connection = Native.nz_connect(pointer, (nuint)error.Length);
                if (connection == 0) throw new InvalidOperationException(Marshal.PtrToStringUTF8((nint)pointer));
            }
        }
        catch { Native.Dispose(); throw; }
        clipboard = new(this);
    }

    /// <summary>Gets the potential window operations. Individual windows refine optional protocol support.</summary>
    public WindowCapabilities Capabilities => new(true, true, true, true)
    { Decorations = DecorationSupport, CanUseTransparentBackground = SupportsTransparentBackground, CanSetAlwaysOnTop = SupportsAlwaysOnTop,
        CanShowPopupsOutsideWindow = SupportsNativePopups };

    internal WindowDecorationCapabilities DecorationSupport => connection == 0 ? WindowDecorationCapabilities.None : new()
    {
        NativeFrame = Native.DecorationSupport(connection) != 0 ? WindowSupportStatus.Unknown : WindowSupportStatus.Unsupported,
        NativeTitleBar = Native.DecorationSupport(connection) != 0 ? WindowSupportStatus.Unknown : WindowSupportStatus.Unsupported,
        NazorFrame = Native.DecorationSupport(connection) != 0 ? WindowSupportStatus.Unknown : WindowSupportStatus.Supported,
        FrameChanges = (Native.DecorationSupport(connection) & 2) != 0 ? WindowSupportStatus.Unsupported : WindowSupportStatus.Supported,
        CaptionChanges = WindowSupportStatus.Supported,
    };

    internal bool SupportsNativePopups => connection != 0 && Native.HasPopups && SupportsTransparentBackground;

    internal bool SupportsAlwaysOnTop => connection != 0 && Native.X11Above != null && Native.X11AboveSupported != null &&
        Native.X11AboveSupported(connection) != 0;

    // Wayland's shared-memory buffers already use ARGB8888.
    internal bool SupportsTransparentBackground => connection != 0 && (!IsX11 || Native.SupportsArgb(connection));

    /// <summary>Gets session D-Bus services for tray icons, notifications and desktop preferences.</summary>
    public IPlatformDesktopIntegration DesktopIntegration => Desktop;
    internal LinuxDesktopIntegration Desktop => desktop ??= new(this);

    internal nint Connection => connection;
    internal int RepeatRate { get; private set; } = 25;
    internal int RepeatDelay { get; private set; } = 600;

    /// <summary>Creates an initially hidden Linux desktop window on the UI thread.</summary>
    /// <param name="host">The Nazor lifecycle, input and painting receiver.</param>
    /// <param name="options">Initial title, size, owner and operation policy.</param>
    /// <returns>The native window adapter owned by the caller.</returns>
    public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
    {
        AssertAccess();
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(options);
        if (options.BackgroundMode == BackgroundMode.Transparent && !SupportsTransparentBackground)
            throw new PlatformNotSupportedException("The Linux display does not support transparent window backgrounds.");
        if (options.Owner is not null && (options.Owner is not LinuxDesktopWindow owner || owner.PlatformOwner != this || owner.IsClosed))
            throw new ArgumentException("The owner must be an open window on this Linux desktop connection.", nameof(options));
        var window = new LinuxDesktopWindow(this, host, options, clipboard);
        windows.Add(window.Handle, window);
        return window;
    }

    /// <summary>Runs native events, dispatcher work and animation frames on the creating thread.</summary>
    /// <param name="uiDispatcher">The dispatcher created on the same thread.</param>
    /// <returns>The exit code passed to Quit.</returns>
    public int Run(UIDispatcher uiDispatcher)
    {
        AssertAccess();
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        uiDispatcher.AssertAccess();
        if (running) throw new InvalidOperationException("The Linux desktop loop is already running.");
        running = true;
        dispatcher = uiDispatcher;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(uiDispatcher.SynchronizationContext);
        uiDispatcher.WorkPosted += Wake;
        try
        {
            while (Volatile.Read(ref quitting) == 0)
            {
                uiDispatcher.RunPending();
                RunPosted();
                if (Volatile.Read(ref quitting) != 0) break;
                Dispatch(0);
                var timeout = -1;
                foreach (var window in windows.Values.ToArray()) timeout = MinimumTimeout(timeout, window.Tick());
                if (Volatile.Read(ref quitting) == 0) Dispatch(uiDispatcher.HasPendingWork || !posted.IsEmpty ? 0 : timeout);
            }
            return exitCode;
        }
        finally
        {
            uiDispatcher.WorkPosted -= Wake;
            SynchronizationContext.SetSynchronizationContext(previous);
            dispatcher = null;
            running = false;
        }
    }

    /// <summary>Wakes and stops the native event loop. May be called from another thread.</summary>
    /// <param name="code">The exit code returned by Run.</param>
    public void Quit(int code = 0)
    {
        exitCode = code;
        Volatile.Write(ref quitting, 1);
        Wake();
    }

    internal void AssertAccess()
    {
        ObjectDisposedException.ThrowIf(connection == 0, this);
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Linux desktop operations must run on the creating UI thread.");
    }

    internal void Wake() { lock (lifetimeGate) { if (connection != 0) Native.nz_wake(connection); } }
    internal void Remove(LinuxDesktopWindow window) => windows.Remove(window.Handle);
    internal void Post(Action action) => Post(action, null);
    private void Post(Action action, Action? canceled)
    {
        lock (lifetimeGate)
        {
            if (connection == 0) { canceled?.Invoke(); return; }
            posted.Enqueue((action, canceled));
            Native.nz_wake(connection);
        }
    }
    internal Task<T> InvokeAsync<T>(Func<T> action)
    {
        if (Environment.CurrentManagedThreadId == ownerThread) { AssertAccess(); return Task.FromResult(action()); }
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() => { try { completion.SetResult(action()); } catch (Exception error) { completion.SetException(error); } },
            () => completion.TrySetException(new ObjectDisposedException(GetType().Name)));
        return completion.Task;
    }
    private void RunPosted() { while (posted.TryDequeue(out var action)) action.Run(); }
    internal T Wait<T>(Task<T> task, TimeSpan? timeout = null)
    {
        var end = Stopwatch.GetTimestamp() + (long)((timeout ?? TimeSpan.FromSeconds(5)).TotalSeconds * Stopwatch.Frequency);
        while (!task.IsCompleted)
        {
            if (Stopwatch.GetTimestamp() >= end) throw new TimeoutException("The Linux desktop service did not respond in time.");
            RunPosted(); dispatcher?.RunPending(); Dispatch(20);
        }
        return task.GetAwaiter().GetResult();
    }
    internal void SettingsChanged() { foreach (var window in windows.Values.ToArray()) window.SettingsChanged(); }
    internal void RemoveAccessibility(LinuxDesktopWindow window) => desktop?.RemoveAccessibility(window);

    /// <summary>Shows a file or directory picker through XDG Desktop Portal.</summary>
    /// <param name="owner">The optional owner on this Linux desktop connection.</param>
    /// <param name="request">Picker kind, filters and initial path.</param>
    /// <returns>The chosen local paths, or null when canceled.</returns>
    /// <exception cref="NotSupportedException">The session has no file chooser portal.</exception>
    public IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)
    {
        AssertAccess(); ArgumentNullException.ThrowIfNull(request);
        if (owner is not null && (owner is not LinuxDesktopWindow window || window.PlatformOwner != this || window.IsClosed))
            throw new ArgumentException("The dialog owner must belong to this Linux desktop connection.", nameof(owner));
        return Desktop.ShowFileDialog(owner as LinuxDesktopWindow, request);
    }
    internal static int MinimumTimeout(int left, int right) => left < 0 ? right : right < 0 ? left : Math.Min(left, right);
    internal void Dispatch(int timeout)
    {
        if (Native.nz_dispatch(connection, timeout) < 0) throw new InvalidOperationException(Marshal.PtrToStringUTF8(Native.nz_error(connection)));
        while (connection != 0 && Native.nz_next_event(connection, out var value) != 0)
        {
            if (value.Type == 16) { RepeatRate = Math.Clamp(value.A, 0, 1000); RepeatDelay = Math.Max(0, value.B); }
            else if (value.Type == 17) clipboard.Deliver(value.Text == 0 ? null : Marshal.PtrToStringUTF8(value.Text));
            else if (popupWindows.TryGetValue(value.Window, out var popup)) popup.OnEvent(value);
            else if (windows.TryGetValue(value.Window, out var window)) window.OnEvent(value);
        }
        foreach (var window in windows.Values.ToArray()) window.FlushPopupFocus();
    }

    /// <summary>Closes remaining windows and releases the native connection on the UI thread.</summary>
    public void Dispose()
    {
        if (connection == 0 || disposing) return;
        AssertAccess();
        disposing = true;
        Volatile.Write(ref quitting, 1);
        List<Exception>? failures = null;
        foreach (var window in windows.Values.ToArray())
        {
            try { window.Dispose(); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
        try { desktop?.Dispose(); }
        catch (Exception error) { (failures ??= []).Add(error); }
        finally
        {
            lock (lifetimeGate)
            {
                try { Native.nz_disconnect(connection); }
                finally
                {
                    connection = 0;
                    Native.Dispose();
                    while (posted.TryDequeue(out var action)) action.Cancel?.Invoke();
                }
            }
        }
        if (failures is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is not null) throw new AggregateException(failures);
    }

    private sealed class LinuxDesktopClipboard(LinuxDesktopPlatform platform) : IClipboard
    {
        private string? received;
        private bool complete, reading;
        internal void Deliver(string? text) { received = text; complete = true; }
        public string? GetText()
        {
            platform.AssertAccess();
            if (reading) return null;
            reading = true;
            var requested = false;
            try
            {
                complete = false;
                requested = platform.Native.nz_clipboard_request(platform.connection) != 0;
                if (!requested) return null;
                var deadline = Stopwatch.GetTimestamp() + 2 * Stopwatch.Frequency;
                while (!complete && Stopwatch.GetTimestamp() < deadline) platform.Dispatch(20);
                return complete ? received : null;
            }
            finally
            {
                if (requested && !complete && platform.connection != 0 && platform.Native.ClipboardCancel != null)
                    platform.Native.ClipboardCancel(platform.connection);
                reading = false;
            }
        }
        public bool SetText(string text)
        {
            platform.AssertAccess();
            ArgumentNullException.ThrowIfNull(text);
            return platform.Native.nz_clipboard_set(platform.connection, text) != 0;
        }
    }
}
