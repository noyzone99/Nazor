using Nazor.Platform.Windows;

namespace Nazor;

/// <summary>
/// Convenient startup for Windows applications; the application runtime remains platform independent.
/// </summary>
/// <remarks>Retained for explicit Windows startup and compatibility. SDK applications can use
/// <see cref="NazorApplication.CreateBuilder" /> with the automatically registered platform host.</remarks>
public static class WindowsApplication
{
    /// <summary>Creates an application builder configured with the Windows platform host.</summary>
    /// <param name="args">The command-line arguments supplied to the application.</param>
    /// <returns>The configured application builder.</returns>
    public static NazorApplicationBuilder CreateBuilder(string[]? args = null)
    {
        return NazorApplication.CreateBuilder(args, new WindowsPlatformHost());
    }

}
