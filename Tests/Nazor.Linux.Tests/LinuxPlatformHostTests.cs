using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class LinuxPlatformHostTests
{
    [Theory]
    [InlineData("X11", LinuxDesktopBackend.Wayland, "All", LinuxDesktopBackend.X11)]
    [InlineData("Wayland", LinuxDesktopBackend.X11, "All", LinuxDesktopBackend.Wayland)]
    [InlineData("X11", LinuxDesktopBackend.Auto, "X11", LinuxDesktopBackend.X11)]
    [InlineData("Wayland", LinuxDesktopBackend.Auto, "Wayland", LinuxDesktopBackend.Wayland)]
    [InlineData("X11", LinuxDesktopBackend.Auto, null, LinuxDesktopBackend.X11)]
    public void Environment_override_precedes_constructor_and_session(string value, LinuxDesktopBackend requested, string? libraries, LinuxDesktopBackend expected)
    {
        Assert.Equal(expected, LinuxPlatformHost.SelectBackend(requested, libraries, "x11", "wayland-0", null, ":0", value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Auto")]
    [InlineData("DRM")]
    [InlineData("x11")]
    [InlineData("wayland")]
    [InlineData(" Wayland ")]
    [InlineData("1")]
    public void Invalid_environment_override_fails_even_with_an_explicit_host(string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => LinuxPlatformHost.SelectBackend(
            LinuxDesktopBackend.X11, "All", null, null, null, null, value));
        Assert.Contains("NAZOR_LINUX_BACKEND", error.Message);
        Assert.Contains("Expected X11 or Wayland", error.Message);
    }

    [Theory]
    [InlineData("X11", "Wayland")]
    [InlineData("Wayland", "X11")]
    [InlineData("X11", "None")]
    [InlineData("Wayland", "None")]
    public void Environment_override_requires_the_published_backend(string value, string libraries)
    {
        var error = Assert.Throws<InvalidOperationException>(() => LinuxPlatformHost.SelectBackend(
            LinuxDesktopBackend.Auto, libraries, null, null, null, null, value));
        Assert.Contains($"NAZOR_LINUX_BACKEND={value}", error.Message);
        Assert.Contains($"NazorLinuxNativeLibraries={libraries}", error.Message);
    }

    [Fact]
    public void Successful_Wayland_startup_does_not_attempt_X11()
    {
        var attempts = new List<LinuxDesktopBackend>();
        var result = LinuxPlatformHost.CreatePlatform(LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "All", null, backend =>
        {
            attempts.Add(backend);
            return backend;
        });
        Assert.Equal(LinuxDesktopBackend.Wayland, result);
        Assert.Equal([LinuxDesktopBackend.Wayland], attempts);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(DllNotFoundException))]
    [InlineData(typeof(EntryPointNotFoundException))]
    [InlineData(typeof(BadImageFormatException))]
    public void Auto_with_both_libraries_falls_back_after_Wayland_startup_failure(Type errorType)
    {
        var attempts = new List<LinuxDesktopBackend>();
        var failure = (Exception)Activator.CreateInstance(errorType)!;
        var result = LinuxPlatformHost.CreatePlatform(LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "All", null, backend =>
        {
            attempts.Add(backend);
            if (backend == LinuxDesktopBackend.Wayland) throw failure;
            return backend;
        });
        Assert.Equal(LinuxDesktopBackend.X11, result);
        Assert.Equal([LinuxDesktopBackend.Wayland, LinuxDesktopBackend.X11], attempts);
    }

    [Theory]
    [InlineData(LinuxDesktopBackend.Wayland, LinuxDesktopBackend.Wayland, "All", null)]
    [InlineData(LinuxDesktopBackend.X11, LinuxDesktopBackend.X11, "All", null)]
    [InlineData(LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "Wayland", null)]
    [InlineData(LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, null, null)]
    [InlineData(LinuxDesktopBackend.Auto, LinuxDesktopBackend.X11, "All", null)]
    [InlineData(LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "All", "Wayland")]
    [InlineData(LinuxDesktopBackend.Auto, LinuxDesktopBackend.X11, "All", "X11")]
    public void Explicit_or_single_backend_startup_does_not_fall_back(LinuxDesktopBackend requested, LinuxDesktopBackend selected, string? libraries, string? environmentBackend)
    {
        var failure = new InvalidOperationException("Backend unavailable");
        var attempts = new List<LinuxDesktopBackend>();
        var error = Assert.Throws<InvalidOperationException>(() => LinuxPlatformHost.CreatePlatform<LinuxDesktopBackend>(requested, selected, libraries, environmentBackend, backend =>
        {
            attempts.Add(backend);
            throw failure;
        }));
        Assert.Same(failure, error);
        Assert.Equal([selected], attempts);
    }

    [Fact]
    public void Both_startup_errors_are_preserved_when_X11_also_fails()
    {
        var waylandError = new InvalidOperationException("Cannot connect to Wayland");
        var x11Error = new DllNotFoundException("Missing X11 library");
        var attempts = new List<LinuxDesktopBackend>();
        var error = Assert.Throws<InvalidOperationException>(() => LinuxPlatformHost.CreatePlatform<LinuxDesktopBackend>(
            LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "All", null, backend =>
            {
                attempts.Add(backend);
                throw backend == LinuxDesktopBackend.Wayland ? waylandError : x11Error;
            }));
        Assert.Equal([LinuxDesktopBackend.Wayland, LinuxDesktopBackend.X11], attempts);
        Assert.Equal([waylandError, x11Error], Assert.IsType<AggregateException>(error.InnerException).InnerExceptions);
    }

    [Theory]
    [InlineData(typeof(PlatformNotSupportedException))]
    [InlineData(typeof(OutOfMemoryException))]
    public void Unsupported_platforms_and_unrelated_errors_do_not_trigger_fallback(Type errorType)
    {
        var failure = (Exception)Activator.CreateInstance(errorType)!;
        var attempts = 0;
        var error = Record.Exception(() => LinuxPlatformHost.CreatePlatform<LinuxDesktopBackend>(
            LinuxDesktopBackend.Auto, LinuxDesktopBackend.Wayland, "All", null, _ =>
            {
                attempts++;
                throw failure;
            }));
        Assert.Same(failure, error);
        Assert.Equal(1, attempts);
    }
}
