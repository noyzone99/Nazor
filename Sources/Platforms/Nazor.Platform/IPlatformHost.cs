namespace Nazor.Platform;

/// <summary>Owns the platform and its UI-thread startup rules for one application run.</summary>
public interface IPlatformHost
{
    /// <summary>Creates the platform and invokes the application within its required UI-thread context.</summary>
    /// <param name="application">The callback invoked with the platform created by the host.</param>
    /// <returns>The application callback's exit code.</returns>
    int Run(Func<IPlatform, int> application);

    /// <summary>Runs the application without blocking the caller, completing after the platform has been released.</summary>
    /// <param name="application">The callback invoked with the platform created by the host.</param>
    /// <returns>A task containing the application callback's exit code.</returns>
    /// <remarks>The default implementation calls Run on a dedicated worker thread. Hosts with specific thread
    /// requirements should override this method to supply their native UI-thread context.</remarks>
    Task<int> RunAsync(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return Task.Factory.StartNew(() => Run(application), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
