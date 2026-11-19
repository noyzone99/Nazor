using System.Runtime.InteropServices;

namespace Nazor.Platform.Windows;

internal static unsafe partial class Win32MenuTheme
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress")]
    private static partial nint Ordinal(nint module, nint ordinal);

    private static readonly nint theme = OperatingSystem.IsWindows()
        ? NativeLibrary.Load("uxtheme.dll", typeof(Win32MenuTheme).Assembly, DllImportSearchPath.System32) : 0;
    private static int lastMode = -1;
    private static readonly Lock gate = new();

    public static bool Apply(nint hwnd, bool dark, bool highContrast)
    {

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) || OperatingSystem.IsWindowsVersionAtLeast(10, 0, 30000)) return false;
        var preferredMode = (delegate* unmanaged[Stdcall]<int, int>)Ordinal(theme, 135);
        var allowWindow = (delegate* unmanaged[Stdcall]<nint, byte, byte>)Ordinal(theme, 133);
        var flush = (delegate* unmanaged[Stdcall]<void>)Ordinal(theme, 136);
        if (preferredMode == null || allowWindow == null || flush == null) return false;
        lock (gate)
        {
            var mode = highContrast ? 0 : dark ? 2   : 3  ;
            if (mode != lastMode)
            {
                preferredMode(mode);
                flush();
                lastMode = mode;
            }
            return allowWindow(hwnd, dark && !highContrast ? (byte)1 : (byte)0) != 0;
        }
    }
}
