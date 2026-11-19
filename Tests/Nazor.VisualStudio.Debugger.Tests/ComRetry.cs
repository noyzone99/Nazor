using System.Runtime.InteropServices;

namespace Nazor.VisualStudio.Debugger.Tests;

[ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IOleMessageFilter
{
    [PreserveSig] int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info);
    [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int rejection);
    [PreserveSig] int MessagePending(IntPtr callee, int ticks, int pending);
}

internal sealed class ComRetry : IOleMessageFilter, IDisposable
{
    private readonly IOleMessageFilter? previous;
    internal ComRetry() => CoRegisterMessageFilter(this, out previous);
    public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) => 0;
    public int RetryRejectedCall(IntPtr callee, int ticks, int rejection) => ticks < 5000 && rejection is 1 or 2 ? 100 : -1;
    public int MessagePending(IntPtr callee, int ticks, int pending) => 2;
    public void Dispose() => CoRegisterMessageFilter(previous, out _);
    [DllImport("ole32.dll")] private static extern int CoRegisterMessageFilter(IOleMessageFilter? filter, out IOleMessageFilter? previous);
}
