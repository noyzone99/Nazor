using MyNazorApp;
using MyNazorApp.Services;
#if (Theme == "fluent")
using Nazor.Themes.Fluent;
#elif (Theme == "nazorleaf")
using Nazor.Themes.NazorLeaf;
#endif

var builder = NazorApplication.CreateBuilder(args);

#if (IncludeSamplePages)
builder.Services.AddScoped<SampleTaskStore>();
#else
builder.Services.AddSingleton<Greeter>();
#endif

#if (Theme == "fluent")
builder.Fonts.AddFluentFonts();
#elif (Theme == "nazorleaf")
builder.Fonts.AddNazorLeafFonts();
#endif

builder.Application.UseMainWindow<App>();

#if (IncludeDevTools)
//-:cnd:noEmit
#if NAZOR_DEVTOOLS
builder.UseDevTools();
#endif
//+:cnd:noEmit

#endif
using var app = builder.Build();
#if (Theme == "fluent")
app.Appearance.Theme = Nazor.Themes.Fluent.Themes.Fluent;
#elif (Theme == "nazorleaf")
app.Appearance.Theme = NazorLeafTheme.Theme;
#endif
app.Appearance.StyleSheet = MyNazorApp.StyleSheets.Theme;

return app.Run();
