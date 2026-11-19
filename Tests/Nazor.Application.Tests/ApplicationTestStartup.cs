using Microsoft.Extensions.DependencyInjection;
using Nazor.Platform.Windows;

namespace Nazor.Application.Tests;

internal static class ApplicationTestStartup
{
    public static int Run<TComponent>(ApplicationRuntimeSettings? settings = null) where TComponent : IComponent
    {
        settings ??= new();
        using var services = settings.Services is null ? new ServiceCollection().AddNazorServices().BuildServiceProvider() : null;
        var host = settings.PlatformHost ?? new WindowsPlatformHost();
        return host.Run(platform => NazorApplication.RunApplication(platform, typeof(TComponent),
            settings with { Services = settings.Services ?? services }, null));
    }
}
