namespace Nazor.Platform.Linux;

/// <summary>Owns the selected native Linux desktop backend for one application run.</summary>
public sealed class LinuxPlatformHost : IPlatformHost
{
    private readonly LinuxDesktopBackend backend;

    /// <summary>Selects a display protocol from NAZOR_LINUX_BACKEND, the publication and current desktop session.</summary>
    public LinuxPlatformHost() : this(LinuxDesktopBackend.Auto) { }

    /// <summary>Captures an explicit display protocol or selects it from the publication and desktop session.</summary>
    /// <param name="backend">The display protocol, overridden by NAZOR_LINUX_BACKEND; Auto selects from the publication and session, with X11 fallback when both libraries are published.</param>
    public LinuxPlatformHost(LinuxDesktopBackend backend)
    {
        if (!Enum.IsDefined(backend)) throw new ArgumentOutOfRangeException(nameof(backend));
        this.backend = backend;
    }

    internal static LinuxDesktopBackend SelectBackend(LinuxDesktopBackend requested, string? libraries, string? session, string? wayland, string? socket, string? display, string? environmentBackend = null)
    {
        if (environmentBackend is not null)
        {
            var forced = environmentBackend switch
            {
                "X11" => LinuxDesktopBackend.X11,
                "Wayland" => LinuxDesktopBackend.Wayland,
                _ => throw new InvalidOperationException($"Invalid NAZOR_LINUX_BACKEND value '{environmentBackend}'. Expected X11 or Wayland.")
            };
            if (string.Equals(libraries, "None", StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(libraries, "X11", StringComparison.OrdinalIgnoreCase) && forced != LinuxDesktopBackend.X11) ||
                (string.Equals(libraries, "Wayland", StringComparison.OrdinalIgnoreCase) && forced != LinuxDesktopBackend.Wayland))
                throw new InvalidOperationException($"NAZOR_LINUX_BACKEND={environmentBackend} requires its native library, but NazorLinuxNativeLibraries={libraries} excludes it.");
            return forced;
        }
        if (requested != LinuxDesktopBackend.Auto) return requested;
        if (string.Equals(libraries, "None", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No Linux desktop native libraries were published (NazorLinuxNativeLibraries=None). Configure an explicit Platform.Host, such as DrmPlatformHost, or include Wayland/X11 native libraries.");
        if (string.Equals(libraries, "X11", StringComparison.OrdinalIgnoreCase)) return LinuxDesktopBackend.X11;
        if (string.Equals(libraries, "Wayland", StringComparison.OrdinalIgnoreCase)) return LinuxDesktopBackend.Wayland;
        if (string.Equals(session, "x11", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(display)) return LinuxDesktopBackend.X11;
        if (!string.IsNullOrEmpty(wayland) || !string.IsNullOrEmpty(socket)) return LinuxDesktopBackend.Wayland;
        return !string.IsNullOrEmpty(display) ? LinuxDesktopBackend.X11 : LinuxDesktopBackend.Wayland;
    }

    internal static T CreatePlatform<T>(LinuxDesktopBackend requested, LinuxDesktopBackend selected, string? libraries, string? environmentBackend, Func<LinuxDesktopBackend, T> create)
    {
        try { return create(selected); }
        catch (Exception waylandError) when (requested == LinuxDesktopBackend.Auto && selected == LinuxDesktopBackend.Wayland &&
            environmentBackend is null && string.Equals(libraries, "All", StringComparison.OrdinalIgnoreCase) && IsStartupFailure(waylandError))
        {
            try { return create(LinuxDesktopBackend.X11); }
            catch (Exception x11Error) when (IsStartupFailure(x11Error))
            {
                throw new InvalidOperationException("Wayland initialization and the X11 fallback both failed. See the inner exceptions for each backend's error.",
                    new AggregateException(waylandError, x11Error));
            }
        }
    }

    private static bool IsStartupFailure(Exception error) => error is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    /// <summary>Creates the selected desktop connection on the calling thread, invokes the application and releases the backend.</summary>
    /// <param name="application">The callback that creates its dispatcher and runs the application on this thread.</param>
    /// <returns>The exit code returned by the application.</returns>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Linux.</exception>
    /// <exception cref="InvalidOperationException">No desktop libraries are included or the native connection cannot be initialized.</exception>
    public int Run(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        var libraries = AppContext.GetData("Nazor.LinuxNativeLibraries") as string;
        var environmentBackend = Environment.GetEnvironmentVariable("NAZOR_LINUX_BACKEND");
        var selected = SelectBackend(backend, libraries,
            Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"),
            Environment.GetEnvironmentVariable("WAYLAND_SOCKET"), Environment.GetEnvironmentVariable("DISPLAY"), environmentBackend);
        using var platform = CreatePlatform<LinuxDesktopPlatform>(backend, selected, libraries, environmentBackend,
            selectedBackend => selectedBackend == LinuxDesktopBackend.X11 ? new X11Platform() : new WaylandPlatform());
        return application(platform);
    }

    /// <summary>Runs the application on a dedicated UI thread and completes after backend cleanup.</summary>
    /// <param name="application">The application callback, invoked on the new UI thread.</param>
    /// <returns>A task containing the exit code, or the initialization or application error.</returns>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    public Task<int> RunAsync(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return Task.Factory.StartNew(() => Run(application), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
