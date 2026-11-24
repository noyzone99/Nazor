namespace Nazor.Website.Generator;

/// <summary>Maps the Markdown files in <c>Docs/</c> to website routes and navigation groups.</summary>
internal static class SiteLayout
{
    public const string GuideArea = "Guide";
    public const string ApiArea = "API";
    public const string ToolsArea = "Tools";
    public const string ProjectArea = "Project";

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

    /// <summary>Single pages outside the guide and the API reference.</summary>
    public static readonly (string Source, string Route, string Area, string Group, string? Title)[] StandalonePages =
    [
        ("Docs/README.md", "docs", GuideArea, "Start here", "Documentation"),
        ("Docs/Guide/README.md", "guide", GuideArea, "Start here", null),
        ("Docs/API.md", "api", ApiArea, "Overview", null),
        ("Docs/DevTools.md", "devtools", ToolsArea, "DevTools", null),
        ("Docs/DevTools-Protocol.md", "devtools/protocol", ToolsArea, "DevTools", null),
        ("Docs/NuGet.md", "nuget", ToolsArea, "Packages", "NuGet packages"),
        ("Docs/Repository-Conventions.md", "conventions", ProjectArea, "Contributing", null),
        ("Docs/Assets/README.md", "brand", ProjectArea, "Identity", null),
    ];

    public static readonly string[] Areas = [GuideArea, ApiArea, ToolsArea, ProjectArea];

    public static string AreaTitle(string area) => area switch
    {
        GuideArea => "Developer guide",
        ApiArea => "API reference",
        ToolsArea => "Tools",
        ProjectArea => "Project",
        _ => area,
    };
}
