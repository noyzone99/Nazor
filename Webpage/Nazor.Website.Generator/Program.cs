using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nazor.Website.Generator;
using Nazor.Website.Shared;

// Converts the Markdown documentation in Docs/ into HTML fragments and a manifest for the website.
// Usage: Nazor.Website.Generator <repository-root> <output-directory>
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Nazor.Website.Generator <repository-root> <output-directory>");
    return 2;
}

var repository = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
const string repositoryUrl = "https://github.com/noyzone99/Nazor";

if (!File.Exists(Path.Combine(repository, "Docs", "Guide", "README.md")))
{
    Console.Error.WriteLine($"Docs/Guide/README.md was not found under '{repository}'.");
    return 1;
}

if (Directory.Exists(output))
{
    Directory.Delete(output, recursive: true);
}

Directory.CreateDirectory(output);

var plans = PlanPages(repository);
var routes = plans.ToDictionary(plan => plan.Source, plan => plan.Route, StringComparer.OrdinalIgnoreCase);
var converter = new MarkdownConverter(repository, output, repositoryUrl, routes);
var pages = new List<SitePage>();

foreach (var plan in plans)
{
    var markdown = File.ReadAllText(Path.Combine(repository, plan.Source));
    var result = converter.Convert(markdown, plan.Source, plan.Route);
    var content = $"content/pages/{plan.Route}.html";
    var target = Path.Combine(output, "pages", plan.Route.Replace('/', Path.DirectorySeparatorChar) + ".html");
    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
    File.WriteAllText(target, result.Html);

    // Guide chapters take their number from the file name; a "N. " prefix in the heading is removed.
    var title = plan.Title ?? result.Title;
    var number = plan.Area == SiteLayout.GuideArea && plan.Order > 0 ? plan.Order.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
    var numbered = Regex.Match(title, @"^(\d+)\.\s+(.+)$");
    if (numbered.Success)
    {
        number ??= numbered.Groups[1].Value;
        title = numbered.Groups[2].Value;
    }

    pages.Add(new SitePage
    {
        Route = plan.Route,
        Content = content,
        Source = plan.Source,
        Title = title,
        Number = number,
        Area = plan.Area,
        Group = plan.Group,
        Summary = result.Summary,
        Toc = result.Toc,
    });
}

var navigation = BuildNavigation(pages);
LinkNeighbors(pages, navigation);
CopyStaticAssets(repository, output);

var manifest = new SiteManifest
{
    Version = ReadVersion(repository),
    Repository = repositoryUrl,
    Pages = pages,
    Navigation = navigation,
};

File.WriteAllText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, SiteJsonContext.Default.SiteManifest));
Console.WriteLine($"Generated {pages.Count} pages into {output}");
return 0;

static List<PagePlan> PlanPages(string repository)
{
    var plans = new List<PagePlan>();
    AddSingles(SiteLayout.LeadingPages);

    foreach (var file in Directory.EnumerateFiles(Path.Combine(repository, "Docs", "Guide"), "*.md").Order(StringComparer.Ordinal))
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var match = Regex.Match(name, @"^(\d+)-(.+)$");
        if (!match.Success)
        {
            continue;
        }

        var chapter = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var group = SiteLayout.GuideGroups.FirstOrDefault(g => chapter <= g.Last).Title ?? "More chapters";
        plans.Add(new PagePlan($"Docs/Guide/{name}.md", "guide/" + match.Groups[2].Value.ToLowerInvariant(), SiteLayout.GuideArea, group, null, chapter));
    }

    AddSingles(SiteLayout.TrailingPages);

    var packages = Directory.EnumerateFiles(Path.Combine(repository, "Docs", "ApiReferences"), "*.md")
        .Select(Path.GetFileNameWithoutExtension)
        .OfType<string>()
        .ToList();

    var order = 0;
    foreach (var (title, members) in SiteLayout.ApiGroups)
    {
        foreach (var package in members.Where(packages.Contains))
        {
            plans.Add(ApiPlan(package, title, ++order));
            packages.Remove(package);
        }
    }

    foreach (var package in packages.Order(StringComparer.Ordinal))
    {
        plans.Add(ApiPlan(package, "More packages", ++order));
    }

    return plans;

    void AddSingles((string Source, string Route, string Area, string Group, string? Title)[] singles)
    {
        foreach (var page in singles.Where(page => File.Exists(Path.Combine(repository, page.Source))))
        {
            plans.Add(new PagePlan(page.Source, page.Route, page.Area, page.Group, page.Title, 0));
        }
    }

    static PagePlan ApiPlan(string package, string group, int order) =>
        new($"Docs/ApiReferences/{package}.md", "api/" + package.ToLowerInvariant().Replace('.', '-'), SiteLayout.ApiArea, group, null, order);
}

static List<NavSection> BuildNavigation(List<SitePage> pages)
{
    var sections = new List<NavSection>();
    foreach (var area in SiteLayout.Areas)
    {
        var section = new NavSection { Area = area, Title = SiteLayout.AreaTitle(area) };
        foreach (var page in pages.Where(page => page.Area == area))
        {
            var group = section.Groups.FirstOrDefault(g => g.Title == page.Group);
            if (group is null)
            {
                group = new NavGroup { Title = page.Group };
                section.Groups.Add(group);
            }

            group.Routes.Add(page.Route);
        }

        sections.Add(section);
    }

    return sections;
}

static void LinkNeighbors(List<SitePage> pages, List<NavSection> navigation)
{
    foreach (var section in navigation)
    {
        var ordered = section.Groups.SelectMany(group => group.Routes).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var page = pages.First(p => p.Route == ordered[i]);
            page.Previous = i > 0 ? ordered[i - 1] : null;
            page.Next = i < ordered.Count - 1 ? ordered[i + 1] : null;
        }
    }
}

static void CopyStaticAssets(string repository, string output)
{
    // The framework identity and screenshots.
    var assets = Path.Combine(output, "assets");
    Directory.CreateDirectory(assets);
    foreach (var file in Directory.EnumerateFiles(Path.Combine(repository, "Docs", "Assets")))
    {
        if (!file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(file, Path.Combine(assets, Path.GetFileName(file)), overwrite: true);
        }
    }

    // The NazorLeaf theme fonts with their licenses, so the site matches the theme without a font CDN.
    var fonts = Path.Combine(output, "fonts");
    Directory.CreateDirectory(fonts);
    var themeFonts = Path.Combine(repository, "Sources", "Themes", "Nazor.Themes.NazorLeaf", "Fonts");
    foreach (var name in new[] { "Geist.ttf", "GeistMono.ttf", "BricolageGrotesque.ttf", "OFL-Geist.txt", "OFL-BricolageGrotesque.txt" })
    {
        var source = Path.Combine(themeFonts, name);
        if (File.Exists(source))
        {
            File.Copy(source, Path.Combine(fonts, name), overwrite: true);
        }
    }
}

static string ReadVersion(string repository)
{
    var props = Path.Combine(repository, "Directory.Build.props");
    if (!File.Exists(props))
    {
        return string.Empty;
    }

    return XDocument.Load(props).Descendants("NazorVersion").FirstOrDefault()?.Value ?? string.Empty;
}

internal sealed record PagePlan(string Source, string Route, string Area, string Group, string? Title, int Order);
