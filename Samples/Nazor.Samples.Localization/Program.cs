using System.Globalization;
using Microsoft.Extensions.Localization;

// IStringLocalizer<T> resolves resource names relative to the root namespace, which may differ from
// the assembly name. Declare that namespace explicitly so App.resx is found after project renaming.
[assembly: RootNamespace("Nazor.Samples.Localization")]

namespace Nazor.Samples.Localization;

internal static class Program
{
    // English is the documented starting point; formatting and resource language can be overridden independently.
    private static int Main(string[] args)
    {
        CultureInfo ReadCulture(string option)
        {
            var index = Array.IndexOf(args, option);
            return CultureInfo.GetCultureInfo(index >= 0 && index + 1 < args.Length ? args[index + 1] : "en-US");
        }

        var builder = NazorApplication.CreateBuilder(args);
        builder.Application.Culture = ReadCulture("--culture");
        builder.Application.UICulture = ReadCulture("--ui-culture");
        builder.Application.UseMainWindow<App>();
        using var app = builder.Build();
        return app.Run();
    }
}
