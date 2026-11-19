using System.Runtime.InteropServices;
using static Nazor.Platform.Linux.DrmNative;

namespace Nazor.Platform.Linux;

internal interface IDrmBackend : IDisposable
{
    int Width { get; }
    int Height { get; }
    bool Active { get; }
    Action<bool>? ActiveChanged { get; set; }
    Action<DrmEvent>? Input { get; set; }
    IOpenGlWindowContext CreateOpenGlContext();
    void Present(PaintTarget target);
    void Dispatch(int timeout);
    void Wake();
}

internal sealed unsafe class DrmBackend : IDrmBackend
{
    private readonly object wakeGate = new();
    private DrmSession? session;
    private DrmDisplay? output;
    private DrmInput? input;
    private int wakeFd = -1;
    public int Width => output!.Width;
    public int Height => output!.Height;
    public bool Active => session!.Active;
    public Action<bool>? ActiveChanged { get; set; }
    public Action<DrmEvent>? Input { get; set; }

    internal DrmBackend(DrmOptions options)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("The DRM backend requires Linux.");
        if (RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64))
            throw new PlatformNotSupportedException("The DRM backend requires a 64-bit Linux process.");
        try
        {
            session = new();
            output = new(session, options);
            input = new(session, Width, Height, value => Input?.Invoke(value));
            wakeFd = eventfd(0, 0x80000 | 0x800);
            if (wakeFd < 0) throw DrmDisplay.Error("Cannot create the DRM dispatcher wake descriptor");
            session.ActiveChanged = active =>
            {
                if (!active)
                {
                    try { ActiveChanged?.Invoke(false); }
                    finally { try { output.Restore(); } finally { input.Suspend(); } }
                }
                else { input.Resume(); ActiveChanged?.Invoke(true); }
            };
        }
        catch (DllNotFoundException error)
        {
            Dispose();
            throw new InvalidOperationException("Cannot load the DRM runtime libraries. Install libdrm, GBM, EGL/GLES, libseat, libinput, libudev and xkbcommon.", error);
        }
        catch { Dispose(); throw; }
    }

    public IOpenGlWindowContext CreateOpenGlContext() => new DrmOpenGlContext(output!);
    public void Present(PaintTarget target) => output!.PresentSoftware(target);

    public void Dispatch(int timeout)
    {
        session!.Dispatch(0);
        var repeatTimeout = -1;
        if (Active) { input!.Dispatch(); repeatTimeout = input.Tick(); }
        timeout = timeout < 0 ? repeatTimeout : repeatTimeout < 0 ? timeout : Math.Min(timeout, repeatTimeout);
        PollFd* descriptors = stackalloc PollFd[3]
        {
            new() { Fd = wakeFd, Events = 1 },
            new() { Fd = session.Fd, Events = 1 },
            new() { Fd = Active ? input!.Fd : -1, Events = 1 }
        };
        var result = poll(descriptors, 3, timeout);
        if (result < 0 && Marshal.GetLastPInvokeError() != 4) throw DrmDisplay.Error("The DRM event loop failed");
        for (var index = 0; index < 3; index++)
            if ((descriptors[index].ReturnedEvents & (8 | 16 | 32)) != 0) throw new InvalidOperationException("A DRM session or input descriptor became unavailable.");
        if ((descriptors[0].ReturnedEvents & 1) != 0) { ulong value; read(wakeFd, &value, 8); }
        session.Dispatch(0);
        if (Active) { input!.Dispatch(); input.Tick(); }
    }

    public void Wake()
    {
        lock (wakeGate)
        {
            if (wakeFd < 0) return;
            ulong value = 1;
            write(wakeFd, &value, 8);
        }
    }

    public void Dispose()
    {
        ActiveChanged = null; Input = null;
        try { input?.Dispose(); }
        finally
        {
            input = null;
            try { output?.Dispose(); }
            finally
            {
                output = null;
                try { session?.Dispose(); }
                finally
                {
                    session = null;
                    lock (wakeGate) if (wakeFd >= 0) { close(wakeFd); wakeFd = -1; }
                }
            }
        }
    }
}
