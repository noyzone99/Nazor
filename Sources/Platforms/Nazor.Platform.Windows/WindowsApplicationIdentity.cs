using System.Runtime.InteropServices;

namespace Nazor.Platform.Windows;

internal static partial class WindowsApplicationIdentity
{
    internal static void Apply()
    {
        if (ApplicationIdentity.Id is not { } id) return;
        uint length = 0;
        if (GetCurrentPackageFullName(ref length, 0) != 15700) return;
        Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(id));
    }

    [LibraryImport("kernel32.dll")]
    private static partial int GetCurrentPackageFullName(ref uint length, nint name);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetCurrentProcessExplicitAppUserModelID(string appId);
}
