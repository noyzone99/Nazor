using System.Diagnostics;
using System.Runtime.InteropServices;
using static Nazor.Platform.Linux.DrmNative;

namespace Nazor.Platform.Linux;

internal sealed unsafe class DrmSession : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SeatCallback(nint seat, nint user);

    [StructLayout(LayoutKind.Sequential)]
    private struct Listener { internal nint Enable, Disable; }

    private readonly SeatCallback enable, disable;
    private nint listener;
    private readonly Dictionary<int, int> devices = [];
    private readonly byte[] terminalAttributes = new byte[256];
    private nint seat;
    private int terminal = -1, terminalMode;
    private bool terminalSaved, terminalModeSaved;
    private volatile bool active;
    private Exception? failure;
    internal bool Active => active;
    internal Action<bool>? ActiveChanged { get; set; }
    internal int Fd => libseat_get_fd(seat);
    internal string Name => Marshal.PtrToStringUTF8(libseat_seat_name(seat)) ?? "seat0";

    internal DrmSession()
    {
        enable = (_, _) => ChangeActive(true);
        disable = (_, _) =>
        {
            ChangeActive(false);
            if (libseat_disable_seat(seat) < 0) failure ??= DrmDisplay.Error("Cannot acknowledge seat deactivation");
        };
        listener = (nint)NativeMemory.Alloc((nuint)sizeof(Listener));
        *(Listener*)listener = new() { Enable = Marshal.GetFunctionPointerForDelegate(enable), Disable = Marshal.GetFunctionPointerForDelegate(disable) };
        try
        {
            seat = libseat_open_seat(listener, 0);
            if (seat == 0) throw DrmDisplay.Error("Cannot acquire a local seat through libseat (seatd or logind)");
            Dispatch(0);
            var started = Stopwatch.GetTimestamp();
            while (!Active && Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(2)) Dispatch(50);
            if (!Active) throw new InvalidOperationException("The DRM backend requires an active local seat.");
            terminal = open("/dev/tty", 0x80002);
            if (terminal >= 0 && ioctl(terminal, 0x4b3b, ref terminalMode) == 0)
            {
                terminalModeSaved = true;
                fixed (byte* saved = terminalAttributes)
                {
                    if (tcgetattr(terminal, saved) == 0)
                    {
                        terminalSaved = true;
                        var raw = stackalloc byte[256];
                        NativeMemory.Copy(saved, raw, 256);
                        cfmakeraw(raw);
                        if (tcsetattr(terminal, 0, raw) != 0) throw DrmDisplay.Error("Cannot configure the console input mode");
                    }
                }
                if (ioctl_value(terminal, 0x4b3a, 1) < 0) throw DrmDisplay.Error("Cannot switch the console to graphics mode");
            }
        }
        catch { Dispose(); throw; }
    }

    private void ChangeActive(bool active)
    {
        this.active = active;
        try { ActiveChanged?.Invoke(active); }
        catch (Exception error) { failure ??= error; }
    }

    internal void Dispatch(int timeout)
    {
        if (libseat_dispatch(seat, timeout) < 0) throw DrmDisplay.Error("Cannot dispatch seat events");
        if (failure is { } error) { failure = null; System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw(); }
    }

    internal (int Id, int Fd) OpenDevice(string path)
    {
        var id = libseat_open_device(seat, path, out var fd);
        if (id >= 0) devices.Add(fd, id);
        return (id, fd);
    }

    internal void CloseDevice(int id)
    {
        var descriptor = devices.FirstOrDefault(value => value.Value == id, new(-1, -1)).Key;
        if (descriptor < 0) return;
        devices.Remove(descriptor);
        try { libseat_close_device(seat, id); }
        finally { close(descriptor); }
    }

    internal void CloseDescriptor(int fd)
    {
        if (devices.TryGetValue(fd, out var id)) CloseDevice(id);
    }

    internal void Switch(int number)
    {
        if (libseat_switch_session(seat, number) < 0) throw DrmDisplay.Error("Cannot switch the virtual terminal");
    }

    public void Dispose()
    {
        if (terminal >= 0)
        {
            if (terminalSaved) fixed (byte* saved = terminalAttributes) tcsetattr(terminal, 0, saved);
            if (terminalModeSaved) ioctl_value(terminal, 0x4b3a, terminalMode);
            close(terminal); terminal = -1;
        }
        if (seat != 0)
        {
            ActiveChanged = null;
            foreach (var id in devices.Values.ToArray()) CloseDevice(id);
            libseat_close_seat(seat); seat = 0;
        }
        NativeMemory.Free((void*)listener); listener = 0;
        GC.KeepAlive(enable); GC.KeepAlive(disable);
    }

    [DllImport("libseat.so.1", SetLastError = true)] private static extern nint libseat_open_seat(nint listener, nint user);
    [DllImport("libseat.so.1")] private static extern int libseat_close_seat(nint seat);
    [DllImport("libseat.so.1", SetLastError = true)] private static extern int libseat_disable_seat(nint seat);
    [DllImport("libseat.so.1")] private static extern int libseat_get_fd(nint seat);
    [DllImport("libseat.so.1")] private static extern nint libseat_seat_name(nint seat);
    [DllImport("libseat.so.1", SetLastError = true)] private static extern int libseat_dispatch(nint seat, int timeout);
    [DllImport("libseat.so.1", SetLastError = true)] private static extern int libseat_switch_session(nint seat, int number);
    [DllImport("libseat.so.1", SetLastError = true)] private static extern int libseat_open_device(nint seat, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, out int fd);
    [DllImport("libseat.so.1")] private static extern int libseat_close_device(nint seat, int id);
}
