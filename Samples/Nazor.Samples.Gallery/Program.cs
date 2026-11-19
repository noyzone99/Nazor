
using Nazor.Samples.Gallery;
using Nazor.Themes.Fluent;
using Nazor.Themes.NazorLeaf;

var builder = NazorApplication.CreateBuilder(args);
builder.Fonts.AddNazorLeafFonts();
builder.Fonts.AddFluentFonts();
builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
builder.Application.UseMainWindow<App>();
using var app = builder.Build();
// --theme basic|fluent|nazor-leaf|none selects the starting theme; the sidebar switches it at run time.
var theme = args.SkipWhile(arg => arg != "--theme").Skip(1).FirstOrDefault();
app.Appearance.Theme = theme switch
{
    "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
    "basic" => Nazor.Themes.Themes.Basic,
    "none" => Nazor.Themes.Themes.None,
    _ => App.LeafTheme,
};
// --scheme light|dark selects the starting color scheme instead of following the system.
var scheme = args.SkipWhile(arg => arg != "--scheme").Skip(1).FirstOrDefault();
app.Appearance.ColorScheme = scheme switch
{
    "light" => Nazor.Style.ColorScheme.Light,
    "dark" => Nazor.Style.ColorScheme.Dark,
    _ => Nazor.Style.ColorScheme.System,
};
await app.RunAsync();
