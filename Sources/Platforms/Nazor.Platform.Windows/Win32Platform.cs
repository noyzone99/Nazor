using System.Runtime.InteropServices;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal interface IMessageTarget
{
    nint OnMessage(nint hwnd, uint message, nint wParam, nint lParam);

    void OnFinalMessage();
}

/// <summary>
/// The Win32 backend: per-monitor DPI awareness, one window class for the process, a message-only window that wakes
/// the dispatcher with PostMessage (thread messages are lost in modal loops), and a GetMessage loop that sleeps when
/// there is nothing to do.
/// </summary>
public sealed unsafe class Win32Platform : IPlatform, IDisposable
{
    internal const string ClassName = "Nazor.Window";
    internal const string PopupClassName = "Nazor.Popup";
    private const uint WM_NAZOR_DISPATCH = WM_APP + 1;
    private const uint WM_NAZOR_QUIT = WM_APP + 2;
    private static readonly Lock RegistrationLock = new();
    private static ushort classAtom, popupClassAtom;

    private readonly MessageWindow messages;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private UIDispatcher? dispatcher;
    private int signalPending;
    private int exitCode;
    private Win32DesktopIntegration? desktop;
    private WindowsWindowing? windowing;
    private readonly Win32ApplicationLauncher launcher;

    /// <inheritdoc />
    public string BackendName => "Win32";

    /// <summary>Gets the Windows tray, notification and application launcher implementation.</summary>
    public IPlatformDesktopIntegration DesktopIntegration => desktop ??= new Win32DesktopIntegration(launcher: launcher);

    /// <summary>Gets the native window operations and decorations supported by this Windows backend.</summary>
    public WindowCapabilities Capabilities => new(true, true, true, true)
    {
        Decorations = Win32Window.DecorationSupport, CanSetWindowIcon = true, CanSetAlwaysOnTop = true,
        CanSetTaskbarVisibility = true, CanUseTransparentBackground = true,
        CanShowPopupsOutsideWindow = true,
        Minimize = new(true, NativeRestriction.Enforced),
        Maximize = new(true, NativeRestriction.Enforced),
        FullScreen = new(true),
        CanObserveMinimized = true,
        CanRestoreFromMinimized = true,
        CanSetScreenPosition = true,
        InteractiveResize = new(true, NativeRestriction.Enforced),
    };

    /// <summary>Initializes a new instance of Win32Platform and applies the entry assembly's Nazor.ApplicationId metadata to unpackaged processes before creating native windows.</summary>
    public Win32Platform()
    {
        WindowsApplicationIdentity.Apply();
        EnsureDpiAwareness();
        EnsureClass();
        launcher = new Win32ApplicationLauncher();
        messages = new MessageWindow(this);
    }

    /// <summary>True when the process uses per-monitor (v2) DPI awareness.</summary>
    public static bool IsPerMonitorDpiAware => AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    internal static nint Instance => GetModuleHandleW(null);

    /// <summary>Creates a native window connected to the supplied Nazor window host.</summary>
    /// <param name="host">The Nazor window host that receives native lifecycle, painting, and input notifications.</param>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>The native platform window connected to the host.</returns>
    public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Windows must be created on the UI thread.");
        if (options.Decorations is { Frame: WindowFrameOwner.Native, HasNativeTitleBar: false })
            windowing ??= WindowsWindowing.TryInitialize();
        return new Win32Window(host, options, windowing is not null, () =>
        {
            windowing ??= WindowsWindowing.TryInitialize();
            return windowing is not null;
        }, launcher);
    }

    /// <summary>Displays a Windows file or folder dialog owned by the supplied native window.</summary>
    /// <param name="owner">The object that owns this transition or native window.</param>
    /// <param name="request">The native file-dialog operation and options.</param>
    /// <returns>The selected paths, or null if the user cancels the dialog.</returns>
    public IReadOnlyList<string>? ShowFileDialog(IPlatformWindow? owner, FileDialogRequest request)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("File dialogs are shown on the UI thread.");
        return Win32FileDialogs.Show(owner is { IsClosed: false } window ? window.Handle : 0, request);
    }

    /// <summary>Runs the Windows message loop using the supplied UI dispatcher.</summary>
    /// <param name="uiDispatcher">The dispatcher used to execute work inside the native event loop.</param>
    /// <returns>The exit code supplied when the Windows message loop terminates.</returns>
    public int Run(UIDispatcher uiDispatcher)
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("The message loop runs on the thread that created the platform.");
        dispatcher = uiDispatcher;
        uiDispatcher.WorkPosted += Signal;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(uiDispatcher.SynchronizationContext);
        try
        {
            uiDispatcher.RunPending();
            MSG message;
            while (true)
            {
                var result = GetMessageW(&message, 0, 0, 0);
                if (result == 0) break;
                if (result < 0) throw new InvalidOperationException($"GetMessage failed (error {Marshal.GetLastPInvokeError()}).");

                if (message.message is WM_KEYDOWN or WM_SYSKEYDOWN)
                {
                    if (DispatchMessageW(&message) != 1) TranslateMessage(&message);
                }
                else
                {
                    TranslateMessage(&message);
                    DispatchMessageW(&message);
                }
            }
            return exitCode;
        }
        finally
        {
            uiDispatcher.WorkPosted -= Signal;
            SynchronizationContext.SetSynchronizationContext(previous);
            dispatcher = null;
        }
    }

    /// <summary>Requests termination of the platform event loop with the specified exit code.</summary>
    /// <param name="code">The exit code reported when the platform loop terminates.</param>
    public void Quit(int code = 0)
    {
        exitCode = code;
        if (Environment.CurrentManagedThreadId == ownerThread) PostQuitMessage(code);
        else PostMessageW(messages.Handle, WM_NAZOR_QUIT, 0, 0);
    }

    private void Signal()
    {
        if (Interlocked.Exchange(ref signalPending, 1) == 0) PostMessageW(messages.Handle, WM_NAZOR_DISPATCH, 0, 0);
    }

    private static void EnsureDpiAwareness()
    {

        if (!IsPerMonitorDpiAware) SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    }

    private static void EnsureClass()
    {
        lock (RegistrationLock)
        {
            if (classAtom != 0) return;
            var iconDpi = GetDpiForSystem();
            fixed (char* name = ClassName)
            {
                var wc = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS | CS_OWNDC,
                    lpfnWndProc = &WindowProcedure,
                    hInstance = Instance,
                    hIcon = LoadImageW(Instance, 32512, 1, GetSystemMetricsForDpi(11, iconDpi), GetSystemMetricsForDpi(12, iconDpi), 0),
                    hIconSm = LoadImageW(Instance, 32512, 1, GetSystemMetricsForDpi(49, iconDpi), GetSystemMetricsForDpi(50, iconDpi), 0),
                    hCursor = LoadCursorW(0, IDC_ARROW),
                    lpszClassName = name,
                };
                classAtom = RegisterClassExW(&wc);
                if (classAtom == 0) throw new InvalidOperationException($"RegisterClassEx failed (error {Marshal.GetLastPInvokeError()}).");
            }
        }
    }

    internal static bool EnsurePopupClass()
    {
        lock (RegistrationLock)
        {
            if (popupClassAtom != 0) return true;
            fixed (char* name = PopupClassName)
            {
                var wc = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS_DBLCLKS,
                    lpfnWndProc = &WindowProcedure,
                    hInstance = Instance,
                    hCursor = LoadCursorW(0, IDC_ARROW),
                    lpszClassName = name,
                };
                popupClassAtom = RegisterClassExW(&wc);
                return popupClassAtom != 0;
            }
        }
    }

    [UnmanagedCallersOnly]
    private static nint WindowProcedure(nint hwnd, uint message, nint wParam, nint lParam)
    {
        try
        {
            if (message == WM_NCCREATE) SetWindowLongPtrW(hwnd, GWLP_USERDATA, ((CREATESTRUCTW*)lParam)->lpCreateParams);
            var data = GetWindowLongPtrW(hwnd, GWLP_USERDATA);

            if (data == 0) return DefWindowProcW(hwnd, message, wParam, lParam);
            var target = (IMessageTarget)GCHandle.FromIntPtr(data).Target!;
            var result = target.OnMessage(hwnd, message, wParam, lParam);
            if (message == WM_NCDESTROY)
            {
                SetWindowLongPtrW(hwnd, GWLP_USERDATA, 0);
                target.OnFinalMessage();
            }
            return result;
        }
        catch (Exception exception)
        {

            Console.Error.WriteLine($"Nazor: unhandled exception in the window procedure: {exception}");
            return DefWindowProcW(hwnd, message, wParam, lParam);
        }
    }

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        desktop?.Dispose();
        messages.Dispose();
        windowing?.Dispose();
        launcher.Dispose();
    }

    private sealed class MessageWindow : IMessageTarget, IDisposable
    {
        private readonly Win32Platform platform;
        private GCHandle self;

        public MessageWindow(Win32Platform platform)
        {
            this.platform = platform;
            self = GCHandle.Alloc(this);
            fixed (char* name = ClassName)
                Handle = CreateWindowExW(0, name, "Nazor.Dispatcher", 0, 0, 0, 0, 0, HWND_MESSAGE, 0, Instance, GCHandle.ToIntPtr(self));
            if (Handle == 0) throw new InvalidOperationException($"CreateWindowEx failed (error {Marshal.GetLastPInvokeError()}).");
        }

        public nint Handle { get; private set; }

        public nint OnMessage(nint hwnd, uint message, nint wParam, nint lParam)
        {
            switch (message)
            {
                case WM_NAZOR_DISPATCH:
                    Volatile.Write(ref platform.signalPending, 0);
                    platform.dispatcher?.RunPending();
                    return 0;
                case WM_NAZOR_QUIT:
                    PostQuitMessage(platform.exitCode);
                    return 0;
                default:
                    return DefWindowProcW(hwnd, message, wParam, lParam);
            }
        }

        public void OnFinalMessage()
        {
            Handle = 0;
            if (self.IsAllocated) self.Free();
        }

        public void Dispose()
        {
            if (Handle != 0) DestroyWindow(Handle);
        }
    }
}
