using System.Runtime.InteropServices;
using System.Text;

namespace Nazor.Application.Tests;

internal static class NativeDesktop
{
    internal static void RequireUnlocked()
    {
        var input = OpenInputDesktop(0, false, 1);
        if (input == 0) Assert.Skip("Native pixel verification requires an unlocked interactive desktop.");
        try
        {
            var current = Name(GetThreadDesktop(GetCurrentThreadId()));
            if (current is null || current != Name(input))
                Assert.Skip("Native pixel verification requires an unlocked interactive desktop.");
        }
        finally { CloseDesktop(input); }
    }

    internal static void KeepDisplayActive() => SetThreadExecutionState(0x80000002);
    internal static void ReleaseDisplay() => SetThreadExecutionState(0x80000000);

    private static string? Name(nint desktop)
    {
        var name = new StringBuilder(256);
        return GetUserObjectInformation(desktop, 2, name, (uint)(name.Capacity * sizeof(char)), out _) ? name.ToString() : null;
    }

    [DllImport("user32.dll")]
    private static extern nint OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll")]
    private static extern nint GetThreadDesktop(uint thread);
    [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserObjectInformation(nint handle, int index, StringBuilder value, uint bytes, out uint needed);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint state);
}
