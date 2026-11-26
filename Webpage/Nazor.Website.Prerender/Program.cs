using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Nazor.Website.Components;
using Nazor.Website.Layout;
using Nazor.Website.Pages;
using Nazor.Website.Services;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: Nazor.Website.Prerender <publish-wwwroot> <source-wwwroot> <public-url-or-empty>");
    return 2;
}

var output = Path.GetFullPath(args[0]);
var source = Path.GetFullPath(args[1]);
var publicUrl = new Uri((string.IsNullOrEmpty(args[2]) ? SiteMetadata.PublicUrl : args[2]).TrimEnd('/') + "/");
var basePath = string.IsNullOrEmpty(args[2]) ? "/" : publicUrl.AbsolutePath;
var baseTag = $"<base href=\"{HtmlEncoder.Default.Encode(basePath)}\" />";
var template = (await File.ReadAllTextAsync(Path.Combine(source, "index.html")))
    .Replace("<base href=\"/\" />", baseTag, StringComparison.Ordinal)
    .Replace("<html lang=\"en\" data-accent=\"blue\">",
        $"<html lang=\"en\" data-accent=\"blue\" data-public-url=\"{HtmlEncoder.Default.Encode(publicUrl.AbsoluteUri)}\">",
        StringComparison.Ordinal);

var navigation = new RenderNavigation(publicUrl);
using var http = new HttpClient(new PublishedContent(output, publicUrl)) { BaseAddress = publicUrl };
await using var services = new ServiceCollection()
    .AddLogging()
    .AddSingleton(http)
    .AddSingleton<NavigationManager>(navigation)
    .AddSingleton<IJSRuntime, StaticJsRuntime>()
    .AddSingleton(new SiteAddress { PublicUrl = publicUrl.AbsoluteUri })
    .AddSingleton<SiteContent>()
    .AddSingleton<SiteInterop>()
    .BuildServiceProvider();

var manifest = await services.GetRequiredService<SiteContent>().GetManifestAsync();
var urls = new List<Uri>();
await WritePageAsync<Home>(SiteMetadata.Home);
await WritePageAsync<GettingStarted>(SiteMetadata.GettingStarted);
foreach (var page in manifest.Pages)
{
    await WritePageAsync<DocPage>(SiteMetadata.ForDocument(page), page.Route);
}

var errorPage = (await File.ReadAllTextAsync(Path.Combine(source, "404.html")))
    .Replace("<base href=\"/\" />", baseTag, StringComparison.Ordinal);
await File.WriteAllTextAsync(Path.Combine(output, "404.html"), errorPage);
Directory.CreateDirectory(Path.Combine(output, "docs"));
await File.WriteAllTextAsync(Path.Combine(output, "docs", "index.html"), $"""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8" />
      {baseTag}
      <title>Developer guide · Nazor</title>
      <link rel="canonical" href="{HtmlEncoder.Default.Encode(new Uri(publicUrl, "guide/").AbsoluteUri)}" />
      <meta http-equiv="refresh" content="0; url=guide/" />
      <script>location.replace(new URL('guide/' + location.search + location.hash, document.baseURI));</script>
    </head>
    <body><a href="guide/">Open the developer guide</a></body>
    </html>
    """);

XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
var sitemap = new XDocument(new XElement(sitemapNamespace + "urlset", urls.Select(url =>
    new XElement(sitemapNamespace + "url", new XElement(sitemapNamespace + "loc", url.AbsoluteUri)))));
sitemap.Save(Path.Combine(output, "sitemap.xml"));
await File.WriteAllTextAsync(Path.Combine(output, "robots.txt"),
    $"User-agent: *\nAllow: /\nSitemap: {new Uri(publicUrl, "sitemap.xml").AbsoluteUri}\n");
Console.WriteLine($"Prerendered {urls.Count} pages into {output}");
return 0;

async Task WritePageAsync<TPage>(SiteMetadata metadata, string? documentRoute = null) where TPage : IComponent
{
    var url = new Uri(publicUrl, metadata.Path);
    navigation.SetPage(url);
    await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
    var (head, body) = await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var head = await renderer.RenderComponentAsync<PageHead>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { [nameof(PageHead.Metadata)] = metadata, [nameof(PageHead.Static)] = true }));
        RenderFragment page = builder =>
        {
            builder.OpenComponent<TPage>(0);
            if (documentRoute is not null)
            {
                builder.AddComponentParameter(1, nameof(DocPage.Path), documentRoute);
            }
            builder.CloseComponent();
        };
        var body = await renderer.RenderComponentAsync<LayoutView>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { [nameof(LayoutView.Layout)] = typeof(MainLayout), [nameof(LayoutView.ChildContent)] = page }));
        return (head.ToHtmlString(), body.ToHtmlString());
    });

    var directory = Path.Combine(output, metadata.Route);
    Directory.CreateDirectory(directory);
    await File.WriteAllTextAsync(Path.Combine(directory, "index.html"), template
        .Replace("<title>Nazor</title>", head, StringComparison.Ordinal)
        .Replace("<!-- prerendered-page -->", body, StringComparison.Ordinal));
    urls.Add(url);
}

internal sealed class RenderNavigation : NavigationManager
{
    public RenderNavigation(Uri baseUri) => Initialize(baseUri.AbsoluteUri, baseUri.AbsoluteUri);

    public void SetPage(Uri page) => Uri = page.AbsoluteUri;
}

internal sealed class PublishedContent(string directory, Uri baseUri) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = baseUri.MakeRelativeUri(request.RequestUri!).OriginalString;
        var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, path), cancellationToken);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    }
}

internal sealed class StaticJsRuntime : IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) =>
        throw new NotSupportedException($"Cannot invoke '{identifier}' during static rendering.");

    public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<T>(identifier, args);
}
