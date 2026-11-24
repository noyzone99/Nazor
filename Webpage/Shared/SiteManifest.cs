using System.Text.Json.Serialization;

namespace Nazor.Website.Shared;

/// <summary>Describes every generated documentation page and the site navigation.</summary>
public sealed class SiteManifest
{
    public string Version { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public List<SitePage> Pages { get; set; } = [];

    public List<NavSection> Navigation { get; set; } = [];
}

/// <summary>A documentation page generated from a Markdown file in <c>Docs/</c>.</summary>
public sealed class SitePage
{
    /// <summary>The route relative to the site base address, such as <c>guide/getting-started</c>.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>The generated HTML fragment relative to the site base address.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>The Markdown source relative to the repository root.</summary>
    public string Source { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>The chapter number of a guide page, if any.</summary>
    public string? Number { get; set; }

    /// <summary>The navigation area that owns the page, such as <c>Guide</c> or <c>API reference</c>.</summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>The navigation group inside the area.</summary>
    public string Group { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? Previous { get; set; }

    public string? Next { get; set; }

    public List<TocEntry> Toc { get; set; } = [];
}

public sealed class TocEntry
{
    public string Id { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public int Level { get; set; }
}

public sealed class NavSection
{
    public string Title { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public List<NavGroup> Groups { get; set; } = [];
}

public sealed class NavGroup
{
    public string Title { get; set; } = string.Empty;

    /// <summary>The routes of the pages in the group, in navigation order.</summary>
    public List<string> Routes { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SiteManifest))]
public sealed partial class SiteJsonContext : JsonSerializerContext;
