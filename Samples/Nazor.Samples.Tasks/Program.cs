using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Nazor;
using Nazor.Samples.Tasks;
using Nazor.Samples.Tasks.Services;

// The composition root chooses lifetimes and platform services before any components are created.
var builder = NazorApplication.CreateBuilder(args);
builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
// A singleton deliberately preserves session data when changing views destroys and recreates their component instances.
// A real persistent store can implement the same interface without putting file or database access in the markup.
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();
// The custom palette is light, so request light control states even if Windows currently uses a dark color scheme.

// Build owns the service provider; disposing the app releases singleton services when the native event loop ends.
builder.Application.UseMainWindow<App>();
using var app = builder.Build();
app.Appearance.Theme = Nazor.Themes.Themes.Basic;
app.Appearance.ColorScheme = Nazor.Style.ColorScheme.Light;
return app.Run();
