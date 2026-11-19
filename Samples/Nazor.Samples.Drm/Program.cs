using Nazor.Platform.Linux;

namespace Nazor.Samples.Drm;

internal static class Program
{
    private static int Main(string[] args)
    {
        var drm = args.Contains("--drm", StringComparer.Ordinal);
        var builder = NazorApplication.CreateBuilder(args.Where(argument => argument != "--drm").ToArray());
        // Both paths host the same content component, which deliberately has no @window.
        if (drm)
        {
            builder.Platform.Host = new DrmPlatformHost();
            builder.Application.UseMainView<App>();
        }
        else
        {
            builder.Application.CreateMainWindow<App>(window =>
            {
                window.Title = "Nazor — DRM single view";
                window.Width = 800;
                window.Height = 480;
            });
        }
        using var app = builder.Build();
        return app.Run();
    }
}
