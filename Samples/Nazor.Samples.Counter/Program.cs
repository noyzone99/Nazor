using System.Globalization;

namespace Nazor.Samples.Counter;

internal static class Program
{
    // The alternate entry path exercises the same component without starting the native host.
    private static int Main(string[] args)
    {
        if (args.Contains("--headless")) return Headless.Run(args);

        var builder = NazorApplication.CreateBuilder(args);
        builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.CreateMainWindow<Counter>(window =>
        {
            window.Title = "Nazor — Counter";
            window.Width = 320;
            window.Height = 200;
        });
        using var app = builder.Build();
        return app.Run();
    }
}
