using System.Runtime.InteropServices;
using Nazor.Platform;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

[Collection("Windows")]
public sealed class ApplicationIdentityTests
{
    [Fact]
    public void Identity_comes_from_entry_assembly_metadata_without_renaming_the_assembly()
    {
        Assert.Equal("org.nazor.applicationtests", ApplicationIdentity.Id);
        Assert.Equal("Nazor.Application.Tests", System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name);
    }

    [Fact]
    public void Windows_applies_the_identity_before_creating_windows()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var platform = new Win32Platform();
        Assert.Equal(0, GetCurrentProcessExplicitAppUserModelID(out var pointer));
        try { Assert.Equal(ApplicationIdentity.Id, Marshal.PtrToStringUni(pointer)); }
        finally { Marshal.FreeCoTaskMem(pointer); }
    }

    [DllImport("shell32.dll")]
    private static extern int GetCurrentProcessExplicitAppUserModelID(out nint appId);
}
