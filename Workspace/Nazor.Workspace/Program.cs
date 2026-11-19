using Nazor.Workspace;

var builder = NazorApplication.CreateBuilder(args);

builder.Application.UseMainWindow<App>();

#if NAZOR_DEVTOOLS
builder.UseDevTools(options: new()
{
    ShortcutKey = "D",
    ShortcutModifiers = KeyModifiers.Control | KeyModifiers.Shift,
    ShowInSystemMenu = true
});
#endif

await using var app = builder.Build();
app.Appearance.ColorScheme = ColorScheme.System;
app.Appearance.Theme = Nazor.Themes.Fluent.Themes.Fluent;

//app.Appearance.Theme = Themes.Fluent;
await app.RunAsync();
