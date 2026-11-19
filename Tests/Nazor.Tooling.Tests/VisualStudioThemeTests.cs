using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Nazor.Tooling.Tests;

public sealed class VisualStudioThemeTests
{
    private static readonly string RepositoryRoot = Nazor.Testing.RepositoryPaths.Root;
    private static readonly string[] ThemeFiles = ["nazor.tmLanguage.tmTheme", "nss.tmLanguage.tmTheme"];

    private static VisualStudioTheme Theme(string scopeName, bool web) => VisualStudioTheme.Load(
        Path.Combine(GrammarHarness.GrammarDirectory, scopeName == "source.nss" ? "nss.tmLanguage.tmTheme" : "nazor.tmLanguage.tmTheme"),
        web ? VisualStudioClassifications.InCoreOrWeb : VisualStudioClassifications.InCore);

    private static IEnumerable<(string Scope, string Classification)> Rules(string themeFile)
    {
        var root = XElement.Load(Path.Combine(GrammarHarness.GrammarDirectory, themeFile)).Element("dict")!;
        var settings = root.Elements("key").First(k => k.Value == "settings").ElementsAfterSelf().First();
        foreach (var rule in settings.Elements("dict"))
        {
            var scope = rule.Elements("key").First(k => k.Value == "scope").ElementsAfterSelf().First().Value;
            var classification = rule.Descendants("key").First(k => k.Value == "vsclassificationtype").ElementsAfterSelf().First().Value;
            foreach (var part in scope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                yield return (part, classification);
        }
    }

    private static HashSet<string> GrammarScopes(string file)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);
        var grammar = JsonDocument.Parse(File.ReadAllText(Path.Combine(GrammarHarness.GrammarDirectory, file))).RootElement;
        Walk(grammar.GetProperty("patterns"));
        Walk(grammar.GetProperty("repository"));
        return scopes;

        void Walk(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in node.EnumerateObject())
                {
                    if (property.Name is "name" or "contentName" && property.Value.ValueKind == JsonValueKind.String)
                        scopes.UnionWith(property.Value.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    else Walk(property.Value);
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray()) Walk(item);
            }
        }
    }

    private static bool Colorable(string scope) =>
        !scope.StartsWith("meta.", StringComparison.Ordinal) && !scope.StartsWith("source.", StringComparison.Ordinal) &&
        !scope.StartsWith("text.", StringComparison.Ordinal) && !scope.StartsWith("invalid.", StringComparison.Ordinal);

    [Theory]
    [InlineData("nazor.tmLanguage.tmTheme")]
    [InlineData("nss.tmLanguage.tmTheme")]
    public void Themes_name_classifications_that_exist(string theme)
    {
        Assert.All(Rules(theme), rule => Assert.True(VisualStudioClassifications.InCoreOrWeb(rule.Classification), $"{rule.Classification} ({rule.Scope}) is not a known classification."));
    }

    [Theory]
    [InlineData("nazor.tmLanguage.tmTheme")]
    [InlineData("nss.tmLanguage.tmTheme")]
    public void Themes_use_only_plain_selectors(string theme)
    {
        Assert.All(Rules(theme), rule => Assert.Matches(@"^[\w.-]+$", rule.Scope));
        Assert.All(Rules(theme), rule => Assert.DoesNotContain(" - ", rule.Scope));
    }

    [Theory]
    [InlineData("nazor.tmLanguage.tmTheme")]
    [InlineData("nss.tmLanguage.tmTheme")]
    public void Web_classifications_follow_one_of_every_Visual_Studio(string theme)
    {
        var rules = Rules(theme).ToList();
        for (var i = 0; i < rules.Count; i++)
        {
            if (VisualStudioClassifications.InCore(rules[i].Classification)) continue;
            var scope = rules[i].Scope;
            Assert.True(rules.Take(i).Any(r => r.Scope == scope && VisualStudioClassifications.InCore(r.Classification)),
                $"{scope} ({rules[i].Classification}) has no earlier rule with a classification of every Visual Studio.");
        }
    }

    [Theory]
    [InlineData("nazor.tmLanguage.tmTheme")]
    [InlineData("nss.tmLanguage.tmTheme")]
    public void Theme_scopes_are_produced_by_the_grammars(string theme)
    {
        var produced = GrammarScopes("nazor.tmLanguage.json").Concat(GrammarScopes("nss.tmLanguage.json")).Concat(GrammarScopes("csharp.tmLanguage.json")).ToList();
        Assert.All(Rules(theme), rule => Assert.True(produced.Any(s => s == rule.Scope || s.StartsWith(rule.Scope + ".", StringComparison.Ordinal)), $"No grammar produces {rule.Scope}."));
    }

    [Fact]
    public void The_Nazor_theme_includes_the_NSS_theme()
    {
        var root = XElement.Load(Path.Combine(GrammarHarness.GrammarDirectory, "nazor.tmLanguage.tmTheme")).Element("dict")!;
        var includes = root.Elements("key").Single(k => k.Value == "includes").ElementsAfterSelf().First().Value;
        Assert.Equal("nss.tmLanguage.tmTheme", includes);
        Assert.All(ThemeFiles, file => Assert.True(File.Exists(Path.Combine(GrammarHarness.GrammarDirectory, Path.ChangeExtension(file, ".json")))));
    }

    [Theory]
    [InlineData("text.nazor", "nazor.tmLanguage.json", false)]
    [InlineData("text.nazor", "nazor.tmLanguage.json", true)]
    [InlineData("text.nazor", "nss.tmLanguage.json", false)]
    [InlineData("text.nazor", "nss.tmLanguage.json", true)]
    [InlineData("source.nss", "nss.tmLanguage.json", false)]
    [InlineData("source.nss", "nss.tmLanguage.json", true)]
    public void Every_scope_of_the_grammars_has_a_classification(string document, string grammar, bool web)
    {
        var theme = Theme(document, web);
        Assert.All(theme.Missing, missing => Assert.True(!web && VisualStudioClassifications.Web.Contains(missing), missing));
        Assert.All(GrammarScopes(grammar).Where(Colorable), scope => Assert.True(theme.Classify([document, scope]) is not null, $"{scope} has no classification."));
    }

    [Theory]
    [MemberData(nameof(GrammarConsistencyTests.SourceFiles), MemberType = typeof(GrammarConsistencyTests))]
    public void Repository_sources_are_colored_in_Visual_Studio(string file)
    {
        var scope = file.EndsWith(".nss", StringComparison.Ordinal) ? "source.nss" : "text.nazor";
        var (tokens, _) = GrammarHarness.Instance.Tokenize(scope, File.ReadAllText(Path.Combine(RepositoryRoot, file)));
        foreach (var web in new[] { false, true })
        {
            var theme = Theme(scope, web);
            var uncolored = tokens.Where(t => t.Text.Trim().Length > 0 && t.Scopes.Skip(1).Any(Colorable) && theme.Classify(t.Scopes) is null);
            Assert.Empty(uncolored.Select(t => $"line {t.Line}: {t}"));
        }
    }

    private static void Classified(string scope, string source, bool web, params (string Text, string Classification, int Occurrence)[] expected)
    {
        var (tokens, _) = GrammarHarness.Instance.Tokenize(scope, source);
        var theme = Theme(scope, web);
        Assert.All(expected, e =>
        {
            var token = tokens.At(source, e.Text, e.Occurrence);
            Assert.True(e.Classification == theme.Classify(token.Scopes), $"{token}: {theme.Classify(token.Scopes) ?? "no classification"} instead of {e.Classification}.");
        });
    }

    [Fact]
    public void The_template_component_has_the_colors_of_Razor()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.Templates/templates/MyNazorApp/App.nazor"));
        Classified("text.nazor", source, web: true,
            ("@inject", "RazorDirective", 1), ("inject", "RazorDirective", 1), ("Greeter", "class name", 1), ("Greeter", "property name", 2),
            ("@window", "RazorDirective", 1), ("<Container", "HTML Tag Delimiter", 1), ("Container", "HTML Element Name", 1), ("Class", "HTML Attribute Name", 1),
            ("@Greeter", "RazorDirective", 1), ("Greeter.Greet", "local name", 1), ("Greet(", "method name", 1), ("Name)", "identifier", 1),
            ("@bind-Value", "RazorDirective", 1), ("bind-Value", "HTML Attribute Name", 1), ("name\" Acc", "identifier", 1),
            ("Increment", "identifier", 1), ("count", "identifier", 1),
            ("@style", "RazorDirective", 1), (".page", "CSS Selector", 1), ("page {", "CSS Selector", 1),
            ("display", "CSS Property Name", 1), ("flex;", "CSS Property Value", 1), ("12", "CSS Property Value", 1), ("px", "CSS Property Value", 1),
            ("bold", "CSS Property Value", 1), ("var(", "CSS Property Value", 1), ("--accent", "CSS Property Name", 1));
    }

    [Fact]
    public void Without_the_web_workload_the_template_component_keeps_its_colors()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.Templates/templates/MyNazorApp/App.nazor"));
        Classified("text.nazor", source, web: false,
            ("@inject", "keyword", 1), ("Greeter", "class name", 1), ("@window", "keyword", 1), ("Container", "markup node", 1), ("Class", "markup attribute", 1),
            ("Greet(", "method name", 1),
            (".page", "symbol reference", 1), ("display", "type", 1), ("flex;", "string", 1), ("12", "number", 1), ("var(", "keyword", 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Single_quoted_attribute_values_use_the_markup_classification(bool web)
    {
        const string source = """<ActionRow Title='A "quoted" title' OnClick='() => Open("Details")' />""";
        var classification = web ? "HTML Attribute Value" : "markup attribute value";
        Classified("text.nazor", source, web, ("quoted", classification, 1), ("Details", classification, 1));
    }

    [Fact]
    public void Code_blocks_and_control_flow_have_the_colors_of_CSharp()
    {
        const string source = """
            @using Nazor.Controls
            @* A comment *@
            <Container>
                @if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        <TextBlock Text="@item.Name" />
                    }
                }
                <TextBlock>Fish &amp; chips</TextBlock>
            </Container>

            @code {
                // Items to show.
                private readonly List<Item> items = [];

                private int Total => items.Count + 1;

                private void Add(string name)
                {
                    if (name is null) return;
                    items.Add(new Item { Name = $"{name}!" });
                }
            }
            """;
        Classified("text.nazor", source, web: true,
            ("@using", "RazorDirective", 1), ("Nazor.Controls", "namespace name", 1), ("@*", "comment", 1), ("A comment", "comment", 1),
            ("@if", "RazorDirective", 1), ("if (", "keyword - control", 1), ("items.Count", "local name", 1), ("Count >", "property name", 1),
            ("> 0", "operator", 1), ("0)", "number", 1), ("foreach", "keyword - control", 1), ("var", "keyword", 1), ("item in", "local name", 1),
            ("in items", "keyword - control", 1), ("Text=", "HTML Attribute Name", 1), ("&amp;", "literal", 1),
            ("@code", "RazorDirective", 1), ("code {", "RazorDirective", 1), ("// Items", "comment", 1), ("Items to", "comment", 1),
            ("private readonly", "keyword", 1), ("readonly", "keyword", 1), ("List<", "class name", 1), ("Item>", "class name", 1),
            ("items =", "field name", 1), ("int", "keyword", 1), ("Total", "property name", 1), ("void", "keyword", 1),
            ("Add(string", "method name", 1), ("string name", "keyword", 1), ("name)", "parameter name", 1),
            ("if (name", "keyword - control", 1), ("is null", "keyword", 1), ("null", "keyword", 1), ("return", "keyword - control", 1),
            ("new", "keyword", 1), ("$\"", "string", 1));
    }

    [Fact]
    public void Stylesheets_have_the_colors_of_CSS()
    {
        const string source = """
            /* Buttons */
            Button:hover > TextBlock {
                background: #ff0000;
                transition: background 200ms ease-out;
                --accent: white;
            }

            @media (max-width: 720px) {
                .title { padding: 4px; }
            }
            """;
        Classified("source.nss", source, web: true,
            ("/*", "CSS Comment", 1), ("Buttons", "CSS Comment", 1), ("Button", "CSS Selector", 2), (":hover", "CSS Selector", 1),
            (">", "CSS Selector", 1), ("TextBlock", "CSS Selector", 1), ("background", "CSS Property Name", 1), ("#ff0000", "CSS Property Value", 1),
            ("transition", "CSS Property Name", 1), ("background", "CSS Property Value", 2), ("200", "CSS Property Value", 1),
            ("ms", "CSS Property Value", 1), ("ease-out", "CSS Property Value", 1), ("--accent", "CSS Property Name", 1), ("white", "CSS Property Value", 1),
            ("@media", "CSS Keyword", 1), ("max-width", "CSS Property Name", 1), (".title", "CSS Selector", 1), ("padding", "CSS Property Name", 1));
        Classified("source.nss", source, web: false,
            ("/*", "comment", 1), ("Button", "markup node", 2), (":hover", "symbol reference", 1), ("background", "type", 1),
            ("#ff0000", "number", 1), ("background", "string", 2), ("200", "number", 1), ("@media", "keyword", 1));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Quoted_NSS_escapes_have_classifications_in_stylesheets_and_embedded_styles(bool web, bool embedded)
    {
        const string style = """Item { --icon: "\E004\22 "; color: red; }""";
        var source = embedded ? "@style { " + style + " }" : style;
        Classified(embedded ? "text.nazor" : "source.nss", source, web,
            (@"\E004", web ? "CSS Property Value" : "string - escape character", 1),
            (@"\22 ", web ? "CSS Property Value" : "string - escape character", 1),
            ("color", web ? "CSS Property Name" : "type", 1));
    }

    [Fact]
    public void Missing_classifications_are_dropped_like_in_Visual_Studio()
    {
        var file = Path.Combine(Path.GetTempPath(), $"nazor-theme-{Guid.NewGuid():N}.tmTheme");
        File.WriteAllText(file, """
            <plist version="1.0"><dict><key>settings</key><array>
              <dict><key>scope</key><string>a.b</string><key>settings</key><dict><key>vsclassificationtype</key><string>keyword</string></dict></dict>
              <dict><key>scope</key><string>a.b</string><key>settings</key><dict><key>vsclassificationtype</key><string>CSS Keyword</string></dict></dict>
              <dict><key>scope</key><string>c</string><key>settings</key><dict><key>vsclassificationtype</key><string>string</string></dict></dict>
              <dict><key>scope</key><string>x c</string><key>settings</key><dict><key>vsclassificationtype</key><string>number</string></dict></dict>
              <dict><key>scope</key><string>y</string><key>settings</key><dict><key>vsclassificationtype</key><string>comment</string></dict></dict>
            </array></dict></plist>
            """);
        try
        {
            var core = VisualStudioTheme.Load(file, VisualStudioClassifications.InCore);
            var web = VisualStudioTheme.Load(file, VisualStudioClassifications.InCoreOrWeb);
            Assert.Equal("keyword", core.Classify(["root", "a.b.c"]));
            Assert.Equal(["CSS Keyword"], core.Missing);
            Assert.Equal("CSS Keyword", web.Classify(["root", "a.b.c"]));
            Assert.Equal("string", core.Classify(["root", "c"]));
            Assert.Equal("comment", core.Classify(["root", "y", "c"]));
            Assert.Equal("number", core.Classify(["root", "x", "c"]));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void The_CSharp_grammar_is_the_one_of_dotnet_csharp_tmLanguage_without_file_types()
    {
        var grammar = JsonDocument.Parse(File.ReadAllText(Path.Combine(GrammarHarness.GrammarDirectory, "csharp.tmLanguage.json"))).RootElement;
        Assert.Equal("source.cs", grammar.GetProperty("scopeName").GetString());
        Assert.False(grammar.TryGetProperty("fileTypes", out _));
        Assert.Contains("dotnet/csharp-tmLanguage", grammar.GetProperty("version").GetString());
        var license = File.ReadAllText(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.Grammars/csharp.tmLanguage.LICENSE.txt"));
        Assert.Contains("MIT License", license);
        var keys = grammar.GetProperty("repository").EnumerateObject().Select(p => p.Name).ToHashSet();
        var included = Regex.Matches(File.ReadAllText(Path.Combine(GrammarHarness.GrammarDirectory, "nazor.tmLanguage.json")), "\"source\\.cs#(?<key>[^\"]+)\"").Select(m => m.Groups["key"].Value).Distinct().ToList();
        Assert.NotEmpty(included);
        Assert.All(included, key => Assert.Contains(key, keys));
    }
}
