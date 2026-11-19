namespace Nazor.Platform.Linux;

/// <summary>Owns a DRM/KMS single-view backend without a desktop or compositor. Select explicitly through Platform.Host.</summary>
public sealed class DrmPlatformHost : IPlatformHost
{
    private readonly DrmOptions options;

    /// <summary>Captures display options. Native devices are opened only when Run or RunAsync starts.</summary>
    /// <param name="options">Optional primary card, connector and scale selection.</param>
    /// <exception cref="ArgumentException">The selected card path or scale is invalid.</exception>
    public DrmPlatformHost(DrmOptions? options = null) => this.options = (options ?? new()).Validate() with { };

    /// <summary>Acquires the local seat and display on the calling thread, invokes the application and restores its resources.</summary>
    /// <param name="application">The application callback, which must select UseMainView and run on this thread.</param>
    /// <returns>The application exit code.</returns>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="PlatformNotSupportedException">The system is not 64-bit Linux.</exception>
    /// <exception cref="InvalidOperationException">The active seat, native libraries or connected output are unavailable.</exception>
    public int Run(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        using var platform = new DrmPlatform(options);
        return application(platform);
    }

    /// <summary>Runs the application on a dedicated UI thread and completes after device and console restoration.</summary>
    /// <param name="application">The application callback invoked on the new UI thread.</param>
    /// <returns>The application exit code after cleanup.</returns>
    public Task<int> RunAsync(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return Task.Factory.StartNew(() => Run(application), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
