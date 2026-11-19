using Nazor.Platform;
#if NAZOR_WINDOWS
using Nazor.Platform.Windows;
#endif
#if NAZOR_LINUX
using Nazor.Platform.Linux;
#endif

namespace Nazor;

/// <summary>Selects the native desktop backend at runtime and delegates UI-thread startup to that backend.</summary>
/// <remarks>Portable builds include Windows and Linux Wayland/X11; RID-specific builds include only the matching backend. Creating this host does not initialize native
/// services; platform selection happens when Run or RunAsync is called. Explicit application hosts can replace it.</remarks>
public sealed class DesktopPlatformHost : IPlatformHost
{
    /// <summary>Runs the application using the backend for the current operating system.</summary>
    /// <param name="application">The callback invoked with the platform created by the selected backend.</param>
    /// <returns>The exit code returned by the application callback.</returns>
    /// <exception cref="ArgumentNullException">application is null.</exception>
    /// <exception cref="PlatformNotSupportedException">No desktop backend is implemented for the current operating system.</exception>
    public int Run(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return CreateCurrentPlatformHost().Run(application);
    }

    /// <summary>Runs the application asynchronously using the backend for the current operating system.</summary>
    /// <param name="application">The callback invoked with the platform created by the selected backend.</param>
    /// <returns>A task containing the callback's exit code, completed after the backend is released.</returns>
    /// <exception cref="ArgumentNullException">application is null.</exception>
    /// <exception cref="PlatformNotSupportedException">No desktop backend is implemented for the current operating system.</exception>
    public Task<int> RunAsync(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return CreateCurrentPlatformHost().RunAsync(application);
    }

    private static IPlatformHost CreateCurrentPlatformHost() => CreatePlatformHost(
        OperatingSystem.IsWindows() ? PlatformKind.Windows
        : OperatingSystem.IsMacOS() ? PlatformKind.MacOS
        : OperatingSystem.IsLinux() ? PlatformKind.Linux : null);

    internal static IPlatformHost CreatePlatformHost(PlatformKind? platform) => platform switch
    {
#if NAZOR_WINDOWS
        PlatformKind.Windows => CreateWindowsHost(),
#endif
#if NAZOR_LINUX
        PlatformKind.Linux => new LinuxPlatformHost(),
#endif
        _ => throw new PlatformNotSupportedException(
            $"Nazor desktop has no backend for '{platform?.ToString() ?? "this operating system"}'. " +
            "Windows and Linux Wayland/X11 are supported. Publish for the matching RuntimeIdentifier or omit it for a portable application, or configure an explicit Platform.Host."),
    };

#if NAZOR_WINDOWS
    private static IPlatformHost CreateWindowsHost() => new WindowsPlatformHost();
#endif
}
