
using Nazor.Samples.Gallery;
using Nazor.Themes.Adwaita;
using Nazor.Themes.Fluent;
using Nazor.Themes.NazorLeaf;

var drm = args.Contains("--drm", StringComparer.Ordinal);
var builder = NazorApplication.CreateBuilder(args.Where(arg => arg != "--drm").ToArray());
builder.Fonts.AddAdwaitaFonts();
builder.Fonts.AddNazorLeafFonts();
builder.Fonts.AddFluentFonts();
builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
if (drm)
{
#if GALLERY_DRM
    if (OperatingSystem.IsLinux())
    {
        builder.Platform.Host = new Nazor.Platform.Linux.DrmPlatformHost();
        builder.Application.UseMainView<GalleryView>();
    }
    else
#endif
    {
        Console.Error.WriteLine("Gallery --drm requires Linux and a portable or Linux publication.");
        return 1;
    }
}
else
{
    builder.Application.UseMainWindow<App>();
}
using var app = builder.Build();
// --theme uses the same identifiers as the sidebar picker.
var theme = args.SkipWhile(arg => arg != "--theme").Skip(1).FirstOrDefault();
app.Appearance.Theme = GalleryThemes.Find(theme) ?? App.LeafTheme;
// --scheme light|dark selects the starting color scheme instead of following the system.
var scheme = args.SkipWhile(arg => arg != "--scheme").Skip(1).FirstOrDefault();
app.Appearance.ColorScheme = scheme switch
{
    "light" => Nazor.Style.ColorScheme.Light,
    "dark" => Nazor.Style.ColorScheme.Dark,
    _ => Nazor.Style.ColorScheme.System,
};
return await app.RunAsync();
