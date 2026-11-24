namespace Nazor.Website.Generator;

/// <summary>Maps the Markdown files in <c>Docs/</c> to website routes and navigation groups.</summary>
internal static class SiteLayout
{
    public const string GuideArea = "Guide";
    public const string ApiArea = "API";

    /// <summary>Guide chapter groups, keyed by the highest chapter number in the group.</summary>
    public static readonly (int Last, string Title)[] GuideGroups =
    [
        (4, "Foundations"),
        (8, "Components"),
        (15, "Controls and windows"),
        (23, "Styling and layout"),
        (30, "Input, tooling and delivery"),
        (35, "Architecture and platforms"),
        (38, "Composition and tutorials"),
    ];

    /// <summary>API reference groups in navigation order. Packages not listed here go to "More packages".</summary>
    public static readonly (string Title, string[] Packages)[] ApiGroups =
    [
        ("Application model", ["Nazor.Core", "Nazor.Application", "Nazor.Presentation", "Nazor.Controls"]),
        ("Styling and rendering", ["Nazor.Style", "Nazor.Layout", "Nazor.Graphics", "Nazor.Animation", "Nazor.Renderer.Skia"]),
        ("Input and accessibility", ["Nazor.Input", "Nazor.Accessibility"]),
        ("Hosting and platforms", ["Nazor.Hosting.Desktop", "Nazor.Hosting.Windows", "Nazor.Platform", "Nazor.Platform.Windows", "Nazor.Platform.Linux"]),
        ("Diagnostics", ["Nazor.Diagnostics", "Nazor.DevTools.Agent", "Nazor.DevTools.Protocol"]),
        ("Themes", ["Nazor.Themes.NazorLeaf", "Nazor.Themes.Fluent", "Nazor.Themes.Adwaita", "Nazor.Themes.Breeze", "Nazor.Themes.Windows9x", "Nazor.Themes.WindowsXP", "Nazor.Themes.Windows7", "Nazor.Themes.Cde"]),
    ];

    /// <summary>Pages that open an area, listed before its chapters or packages.</summary>
    public static readonly (string Source, string Route, string Area, string Group, string? Title)[] LeadingPages =
    [
        ("Docs/README.md", "docs", GuideArea, "Start here", "Documentation"),
        ("Docs/Guide/README.md", "guide", GuideArea, "Start here", null),
        ("Docs/API.md", "api", ApiArea, "Overview", null),
    ];

    /// <summary>Pages listed after the guide chapters. Other Markdown files in <c>Docs/</c> stay off the site.</summary>
    public static readonly (string Source, string Route, string Area, string Group, string? Title)[] TrailingPages =
    [
        ("Docs/DevTools.md", "devtools", GuideArea, "DevTools", null),
        ("Docs/DevTools-Protocol.md", "devtools/protocol", GuideArea, "DevTools", null),
    ];

    public static readonly string[] Areas = [GuideArea, ApiArea];

    public static string AreaTitle(string area) => area switch
    {
        GuideArea => "Developer guide",
        ApiArea => "API reference",
        _ => area,
    };
}
