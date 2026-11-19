using System.Runtime.InteropServices;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class WindowIconTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_windows_use_the_executable_icons_for_both_caption_styles(bool custom)
    {
        using var harness = new ApplicationHarness(() => custom
            ? ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false })
            : ApplicationTestStartup.Run<OrderBook>(new() { Activate = false }));
        var window = custom ? harness.Window<ChromeWindow>() : harness.Window<OrderBook>();
        ApplicationHarness.On(window, () =>
        {
            Assert.NotEqual(0, GetClassLongPtrW(window.PlatformWindow.Handle, -14));
            Assert.NotEqual(0, GetClassLongPtrW(window.PlatformWindow.Handle, -34));
            return true;
        });
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetClassLongPtrW(nint window, int index);
}
