using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nazor.Style;

namespace Nazor.Tooling.Tests;

public sealed class GrammarConsistencyTests
{
    private static readonly string RepositoryRoot = Nazor.Testing.RepositoryPaths.Root;

    [Fact]
    public void Editor_artifacts_match_the_nazor_release()
    {
        var frameworkVersion = XDocument.Load(Path.Combine(RepositoryRoot, "Directory.Build.props")).Descendants("NazorVersion").Single().Value;
        var manifest = XDocument.Load(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.VisualStudio/source.extension.vsixmanifest"));
        var vsixVersion = manifest.Descendants().Single(element => element.Name.LocalName == "Identity").Attribute("Version")!.Value;
        using var vscode = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.VSCode/package.json")));
        using var dependencyLock = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Sources/Tools/Nazor.VSCode/package-lock.json")));
        Assert.Equal(frameworkVersion.Split('-', '+')[0], vsixVersion);
        Assert.Equal(vsixVersion, vscode.RootElement.GetProperty("version").GetString());
        Assert.Equal(vsixVersion, dependencyLock.RootElement.GetProperty("version").GetString());
        Assert.Equal(vsixVersion, dependencyLock.RootElement.GetProperty("packages").GetProperty("").GetProperty("version").GetString());
        Assert.Matches(@"^\d+\.\d+\.\d+$", vsixVersion);
        Assert.Contains(frameworkVersion, manifest.Descendants().Single(element => element.Name.LocalName == "Description").Value);
        Assert.Contains(frameworkVersion, vscode.RootElement.GetProperty("description").GetString());
    }

    private static JsonElement NssRepository() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(GrammarHarness.GrammarDirectory, "nss.tmLanguage.json"))).RootElement.GetProperty("repository");

    private static HashSet<string> Alternatives(string pattern)
    {
        var body = Regex.Match(pattern, @"\(\?:(?<names>[^()]*)\)").Groups["names"].Value;
        return body.Split('|').Select(n => Regex.Unescape(n)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string PatternNamed(JsonElement element, string scope)
    {
        foreach (var match in Find(element))
            return match;
        throw new InvalidOperationException($"No pattern named {scope}.");

        IEnumerable<string> Find(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (node.TryGetProperty("name", out var name) && name.GetString() == scope && node.TryGetProperty("match", out var match))
                    yield return match.GetString()!;
                foreach (var property in node.EnumerateObject())
                    foreach (var found in Find(property.Value)) yield return found;
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                    foreach (var found in Find(item)) yield return found;
            }
        }
    }

    [Fact]
    public void Property_names_are_the_ones_of_the_NSS_compiler()
    {
        var grammar = Alternatives(PatternNamed(NssRepository(), "support.type.property-name.nss"));
        var shorthands = ((System.Collections.IDictionary)typeof(StyleProperties)
            .GetField("Shorthands", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Keys.Cast<string>();
        var compiler = StyleProperties.All.Select(p => p.Name).Concat(shorthands).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(compiler.Order(), grammar.Order());
    }

    [Fact]
    public void Color_names_are_the_ones_of_the_NSS_compiler()
    {
        var grammar = Alternatives(PatternNamed(NssRepository(), "support.constant.color.nss").Replace("(?i)", ""));
        var colors = (System.Collections.IDictionary)typeof(StyleProperties).Assembly.GetType("Nazor.Style.NamedColors")!
            .GetField("Colors", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var compiler = colors.Keys.Cast<string>().Append("currentcolor").ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(compiler.Order(), grammar.Order());
    }

    [Fact]
    public void System_colors_are_the_ones_of_the_NSS_compiler()
    {
        var grammar = Alternatives(PatternNamed(NssRepository(), "support.constant.color.system.nss").Replace("(?i)", ""));
        Assert.Equal(SystemColors.Names.Order(StringComparer.OrdinalIgnoreCase), grammar.Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Every_value_keyword_of_the_NSS_compiler_is_colored_as_a_value()
    {
        var grammar = Alternatives(PatternNamed(NssRepository().GetProperty("value"), "support.constant.property-value.nss"));
        var compiler = StyleProperties.All.SelectMany(p => p.Keywords).Concat(StyleProperties.AllShorthands.SelectMany(s => s.Keywords))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Empty(compiler.Except(grammar, StringComparer.OrdinalIgnoreCase).Order());
    }

    [Fact]
    public void Pseudo_classes_are_the_ones_of_the_NSS_compiler()
    {
        var pattern = PatternNamed(NssRepository(), "entity.other.attribute-name.pseudo-class.nss");
        var grammar = Regex.Match(pattern, @"\(\?:(?<names>[^()]*)\)").Groups["names"].Value.Split('|').ToHashSet();
        Assert.Equal(PseudoClasses.All.Except(PseudoClasses.Functional).Order(), grammar.Order());

        var functional = NssRepository().GetProperty("selector-items").GetProperty("patterns").EnumerateArray()
            .Single(p => p.TryGetProperty("name", out var name) && name.GetString() == "meta.function.pseudo-class.nss").GetProperty("begin").GetString()!;
        var functions = Regex.Match(functional, @"\(:\)\((?<names>[^()]*)\)").Groups["names"].Value.Split('|');
        Assert.Equal(PseudoClasses.Functional.Order(), functions.Order());
    }

    public static TheoryData<string> SourceFiles()
    {
        var data = new TheoryData<string>();
        foreach (var folder in new[] { "Samples", "Sources", "Tests/Nazor.Compiler.Tests/Fixtures" })
            foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot, folder), "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nazor", StringComparison.Ordinal) || f.EndsWith(".nss", StringComparison.Ordinal))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(f => !Path.GetFileName(f).StartsWith("Invalid", StringComparison.Ordinal) && !Path.GetFileName(f).StartsWith("Unknown", StringComparison.Ordinal)))
                data.Add(Path.GetRelativePath(RepositoryRoot, file));
        return data;
    }

    [Theory]
    [MemberData(nameof(SourceFiles))]
    public void Repository_sources_tokenize_cleanly(string file)
    {
        var scope = file.EndsWith(".nss", StringComparison.Ordinal) ? "source.nss" : "text.nazor";
        var (tokens, depth) = GrammarHarness.Instance.Tokenize(scope, File.ReadAllText(Path.Combine(RepositoryRoot, file)));
        Assert.Empty(tokens.Where(t => t.Has("invalid")).Select(t => $"line {t.Line}: {t}"));
        Assert.Equal(1, depth);
    }
}
