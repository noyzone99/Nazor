using System.Globalization;
using Nazor.Themes.Adwaita;
using Nazor.Themes.Fluent;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Samples.Theming;

internal static class Program
{
    private static int Main(string[] args)
    {
        var builder = NazorApplication.CreateBuilder(args);
        builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        builder.Fonts.AddFluentFonts().AddNazorLeafFonts().AddAdwaitaFonts();
        builder.Application.UseMainWindow<App>();
        using var app = builder.Build();
        return app.Run();
    }
}
