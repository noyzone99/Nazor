using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Nazor;
using Nazor.Samples.Windowing;
using Nazor.Style;

// Consume sample modes before the Generic Host parses configuration switches.
var launcherMode = args.Contains("--launcher");
var trayMode = args.Contains("--tray") && !launcherMode;
var builder = NazorApplication.CreateBuilder(args.Where(argument => argument is not ("--launcher" or "--tray")).ToArray());
if (builder.Platform.Host is { } host)
{
    var backend = new SampleBackend(host);
    builder.Platform.Host = backend;
    builder.Services.AddSingleton(backend);
}
builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
// Register private font files before Build; paths are relative to the deployed app, not the working directory.
// The project copies the same files to build and publish output, so users do not need to install them.
builder.Fonts.Add("Fonts/FiraSans-Regular.ttf", "Fonts/FiraSans-Bold.ttf");
// A theme selected here applies to every window. The color-scheme service can still switch light and dark at runtime.
builder.Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
builder.Application.UseTrayItem<TraySession>();
if (launcherMode) builder.Application.UseMainWindow<LauncherWindow>();
else if (!trayMode) builder.Application.UseMainWindow<App>();
using var app = builder.Build();
app.Appearance.ColorScheme = ColorScheme.System;
app.Appearance.Theme = Nazor.Themes.Themes.Basic;
return app.Run();
