using System.Net.Http.Json;
using Nazor.Website.Shared;

namespace Nazor.Website.Services;

/// <summary>Loads the generated manifest and page fragments and caches them for the session.</summary>
public sealed class SiteContent(HttpClient http)
{
    private readonly Dictionary<string, string> pages = new(StringComparer.Ordinal);
    private Task<SiteManifest>? manifest;
    private Dictionary<string, SitePage>? byRoute;

    public Task<SiteManifest> GetManifestAsync() => manifest ??= LoadManifestAsync();

    public async Task<SitePage?> FindPageAsync(string route)
    {
        await GetManifestAsync();
        var key = route.Trim('/').ToLowerInvariant();
        return byRoute!.GetValueOrDefault(key);
    }

    public async Task<SitePage?> GetPageAsync(string? route) => route is null ? null : await FindPageAsync(route);

    public async Task<string> GetHtmlAsync(SitePage page)
    {
        if (!pages.TryGetValue(page.Route, out var html))
        {
            html = await http.GetStringAsync(page.Content);
            pages[page.Route] = html;
        }

        return html;
    }

    private async Task<SiteManifest> LoadManifestAsync()
    {
        var loaded = await http.GetFromJsonAsync("content/manifest.json", SiteJsonContext.Default.SiteManifest)
            ?? throw new InvalidOperationException("The site manifest is empty.");
        byRoute = loaded.Pages.ToDictionary(page => page.Route, StringComparer.Ordinal);
        return loaded;
    }
}
