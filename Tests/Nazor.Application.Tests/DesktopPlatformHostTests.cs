using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

[Collection("Windows")]
public sealed class DesktopPlatformHostTests
{
    [Fact]
    public void Desktop_startup_selects_Windows_on_the_running_OS_and_owns_the_STA_thread()
    {
        var host = new DesktopPlatformHost();
        var result = host.Run(platform =>
        {
            Assert.IsType<Win32Platform>(platform);
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            return 42;
        });
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Async_desktop_startup_uses_a_dedicated_STA_and_preserves_the_exit_code()
    {
        var caller = Environment.CurrentManagedThreadId;
        using var release = new ManualResetEventSlim();
        Task<int>? run = null;
        var result = WindowsPlatformHost.RunOnUiThread(() =>
        {
            var uiCaller = Environment.CurrentManagedThreadId;
            run = new DesktopPlatformHost().RunAsync(platform =>
            {
                Assert.IsType<Win32Platform>(platform);
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                Assert.NotEqual(caller, Environment.CurrentManagedThreadId);
                Assert.NotEqual(uiCaller, Environment.CurrentManagedThreadId);
                Assert.True(release.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                return 42;
            });
            Assert.False(run.IsCompleted);
            release.Set();
            return 0;
        });

        Assert.Equal(0, result);
        Assert.Equal(42, await run!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(PlatformKind.MacOS)]
    [InlineData(PlatformKind.Android)]
    [InlineData(PlatformKind.IOS)]
    public void Unimplemented_backends_fail_with_an_actionable_error(PlatformKind platform)
    {
        var error = Assert.Throws<PlatformNotSupportedException>(() => DesktopPlatformHost.CreatePlatformHost(platform));
        Assert.Contains(platform.ToString(), error.Message);
        Assert.Contains("Windows and Linux Wayland", error.Message);
        Assert.Contains("Platform.Host", error.Message);
    }

    [Fact]
    public void Unknown_systems_are_rejected_without_initializing_Windows()
    {
        Assert.Throws<PlatformNotSupportedException>(() => DesktopPlatformHost.CreatePlatformHost(null));
    }

    [Fact]
    public async Task Windows_selection_returns_the_native_host_and_null_callbacks_are_rejected()
    {
        Assert.IsType<WindowsPlatformHost>(DesktopPlatformHost.CreatePlatformHost(PlatformKind.Windows));
        Assert.IsType<Nazor.Platform.Linux.LinuxPlatformHost>(DesktopPlatformHost.CreatePlatformHost(PlatformKind.Linux));
        Assert.Throws<ArgumentNullException>(() => new DesktopPlatformHost().Run(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new DesktopPlatformHost().RunAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new WindowsPlatformHost().RunAsync(null!));
    }
}
