using Nazor.Website.Shared;

namespace Nazor.Website.Services;

public sealed record SiteMetadata(string Route, string Title, string Description)
{
    public const string PublicUrl = "https://noyzone99.github.io/Nazor/";
    public const string Slogan = "Write Razor. Think Blazor. Ship native.";

    public static SiteMetadata Home { get; } = new(
        "", $"Nazor · {Slogan}",
        "Nazor is a .NET UI framework that compiles Razor components and NSS stylesheets into native desktop applications drawn with Skia.");

    public static SiteMetadata GettingStarted { get; } = new(
        "getting-started", "Getting started · Nazor",
        "Install Nazor for Visual Studio or the .NET CLI, create your first native desktop application and run it.");

    public static SiteMetadata NotFound { get; } = new("not-found", "Page not found · Nazor", "");

    public string Path => Route.Length == 0 ? "" : Route + "/";

    public static SiteMetadata ForDocument(SitePage page) => new(
        page.Route, $"{page.Title} · Nazor {page.Area}",
        string.IsNullOrWhiteSpace(page.Summary) ? $"Read {page.Title} in the Nazor {page.Area}." : page.Summary);
}

public sealed class SiteAddress
{
    public string PublicUrl { get; set; } = SiteMetadata.PublicUrl;
}
