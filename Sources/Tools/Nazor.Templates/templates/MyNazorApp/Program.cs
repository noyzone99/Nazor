using MyNazorApp;

var builder = NazorApplication.CreateBuilder(args);

builder.Services.AddSingleton<Greeter>();

builder.Application.UseMainWindow<App>();

using var app = builder.Build();
app.Appearance.StyleSheet = MyNazorApp.StyleSheets.Theme;

return app.Run();
