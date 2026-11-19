using System.Globalization;
using Nazor.Themes.Fluent;

namespace Nazor.Samples.Theming;

internal static class Program
{
    private static int Main(string[] args)
    {
        var builder = NazorApplication.CreateBuilder(args);
        builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        // The Fluent theme draws message box icons from its packaged icon font.
        builder.Fonts.AddFluentFonts();
        // This comparison deliberately swaps complete themes at window level. Start with None so the
        // previous theme cannot leave lower-priority decorations behind. An app normally sets app.Appearance.Theme once.
        builder.Application.UseMainWindow<App>();
        using var app = builder.Build();
        app.Appearance.Theme = Nazor.Themes.Themes.None;
        return app.Run();
    }
}
