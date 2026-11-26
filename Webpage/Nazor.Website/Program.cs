using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Nazor.Website;
using Nazor.Website.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<SiteContent>();
builder.Services.AddScoped<SiteInterop>();
var address = new SiteAddress();
builder.Services.AddSingleton(address);

var host = builder.Build();
var js = host.Services.GetRequiredService<IJSRuntime>();
var initialDocument = await js.InvokeAsync<string?>("nazorSite.initialDocument");
if (initialDocument is not null)
{
    var navigation = host.Services.GetRequiredService<NavigationManager>();
    var route = navigation.ToBaseRelativePath(navigation.Uri).Split('?', '#')[0].Trim('/').ToLowerInvariant();
    var content = host.Services.GetRequiredService<SiteContent>();
    content.SeedHtml(route, initialDocument);
    await content.GetManifestAsync();
}

address.PublicUrl = await js.InvokeAsync<string?>("nazorSite.activateHead") ?? SiteMetadata.PublicUrl;
await host.RunAsync();
