using System.Runtime.ExceptionServices;

namespace Nazor.Platform.Windows;

/// <summary>Starts the Windows backend on an STA thread and releases it after the UI loop.</summary>
public sealed class WindowsPlatformHost : IPlatformHost
{
    /// <summary>Runs the application within the Windows UI-thread context and owns the platform lifetime.</summary>
    /// <param name="application">The callback invoked with the platform created by the host.</param>
    /// <returns>The application callback's exit code.</returns>
    /// <exception cref="ArgumentNullException">application is null.</exception>
    public int Run(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The Windows host requires Windows.");
        int RunCore()
        {
            using var platform = new Win32Platform();
            return application(platform);
        }
        return RunOnUiThread(RunCore);
    }

    /// <summary>Runs the application on a dedicated STA thread without blocking the caller.</summary>
    /// <param name="application">The callback invoked with the platform created by the host.</param>
    /// <returns>A task containing the exit code, completed after the platform is released; callback errors fault the task.</returns>
    /// <exception cref="ArgumentNullException">application is null.</exception>
    /// <exception cref="PlatformNotSupportedException">The current operating system is not Windows.</exception>
    public Task<int> RunAsync(Func<IPlatform, int> application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The Windows host requires Windows.");
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { completion.SetResult(Run(application)); }
            catch (Exception exception) { completion.SetException(exception); }
        }) { Name = "Nazor UI" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    /// <summary>Executes the callback on an STA thread, creating a dedicated UI thread when required.</summary>
    /// <param name="run">The application callback to execute on the UI thread.</param>
    /// <returns>The exit code returned by the callback.</returns>
    /// <exception cref="ArgumentNullException">run is null.</exception>
    public static int RunOnUiThread(Func<int> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("The Windows host requires Windows.");
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) return run();
        var exitCode = 0;
        ExceptionDispatchInfo? error = null;
        var thread = new Thread(() =>
        {
            try { exitCode = run(); }
            catch (Exception exception) { error = ExceptionDispatchInfo.Capture(exception); }
        }) { Name = "Nazor UI" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        error?.Throw();
        return exitCode;
    }
}
