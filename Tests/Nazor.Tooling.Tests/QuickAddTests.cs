using System.Text.RegularExpressions;
using Nazor.VisualStudio;

namespace Nazor.Tooling.Tests;

public sealed class QuickAddTests
{
    private static readonly Guid CSharp = new("fae04ec0-301f-11d3-bf4b-00c04f79efbc");

    [Fact]
    public void Style_template_starts_with_a_root_rule_and_guidance_without_setting_properties()
    {
        var source = File.ReadAllText(Path.Combine(Nazor.Testing.RepositoryPaths.Root,
            "Sources/Tools/Nazor.VisualStudio/ItemTemplates/NazorStyleSheet/StyleSheet.nss"));
        Assert.Matches(@"^\s*:root\s*\{\s*/\*[^*]+\*/\s*\}\s*$", source);
        Assert.StartsWith(":root {", source);
        Assert.DoesNotContain("$", source);
    }

    [Fact]
    public void Component_mapping_preserves_host_defaults_and_is_idempotent_across_upgrade()
    {
        var original = new Dictionary<Guid, List<(string, string)>> { [CSharp] = [(".*\\.cs$", "Class.vstemplate")] };
        var first = QuickAddMappings.Merge(original, "old");
        var updated = QuickAddMappings.Merge(first, "new");
        Assert.Single(original[CSharp]);
        foreach (var project in updated.Values)
        {
            Assert.Equal(3, project.Count);
            var template = Assert.Single(project, item => Regex.IsMatch("Card.NAZOR", item.Item1));
            Assert.Equal(Path.Combine("new", "ItemTemplates", "CSharp", "Nazor", "NazorComponent", "Component.vstemplate"), template.Item2);
            Assert.Equal("Class.vstemplate", Assert.Single(project, item => Regex.IsMatch("Card.cs", item.Item1)).Item2);
            Assert.DoesNotContain(project, item => Regex.IsMatch("Card.nazor.cs", item.Item1) && item.Item2.Contains("NazorComponent"));
        }
    }

    [Fact]
    public void User_associations_are_retained()
    {
        var original = new Dictionary<Guid, List<(string, string)>> { [CSharp] = [(".*\\.nazor$", "custom")] };
        Assert.Equal("custom", Assert.Single(QuickAddMappings.Merge(original, "extension")[CSharp], item => Regex.IsMatch("Card.nazor", item.Item1)).Item2);
    }

    [Theory]
    [InlineData("profile", "Extensions")]
    [InlineData("install", "Common7/IDE/Extensions")]
    public void Catalog_ids_are_relative_to_the_installation_or_profile_and_upgrade_absolute_mappings(string owner, string relative)
    {
        var root = Path.GetFullPath(owner);
        var relativeFolder = Path.Combine(relative.Replace('/', Path.DirectorySeparatorChar), "extension");
        var folder = Path.Combine(root, relativeFolder);
        var original = QuickAddMappings.Merge(new Dictionary<Guid, List<(string, string)>>(), folder);
        var updated = QuickAddMappings.Merge(original, folder, Path.GetFullPath("other"), root);
        foreach (var project in updated.Values)
        {
            Assert.Equal(2, project.Count);
            Assert.All(project, item => Assert.False(Path.IsPathRooted(item.Item2)));
            Assert.Equal(Path.Combine(relativeFolder, "ItemTemplates", "CSharp", "Nazor", "NazorComponent", "Component.vstemplate"),
                Assert.Single(project, item => Regex.IsMatch("Card.nazor", item.Item1)).Item2);
        }
    }

    [Fact]
    public void Similar_folder_prefix_does_not_strip_an_unrelated_root()
    {
        var root = Path.GetFullPath("profile");
        var folder = root + "-other";
        var map = QuickAddMappings.Merge(new Dictionary<Guid, List<(string, string)>>(), folder, root);
        Assert.All(map[CSharp], item => Assert.StartsWith(folder, item.Item2));
    }

    [Fact]
    public void Uppercase_assembly_paths_resolve_to_the_exact_case_sensitive_catalog_ids()
    {
        var root = Path.Combine(QuickAddMappings.CanonicalDirectory(Path.GetTempPath()), "NazorQuickAdd-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(root, "Extensions", "MixedCaseInstall");
        Directory.CreateDirectory(folder);
        try
        {
            var location = OperatingSystem.IsWindows() ? folder.ToUpperInvariant() : folder;
            var canonical = QuickAddMappings.CanonicalDirectory(location);
            Assert.Equal(folder, canonical);
            var updated = QuickAddMappings.Merge(new Dictionary<Guid, List<(string, string)>>(), canonical, root);
            var catalog = new HashSet<string>(StringComparer.Ordinal)
            {
                Path.Combine("Extensions", "MixedCaseInstall", "ItemTemplates", "CSharp", "Nazor", "NazorComponent", "Component.vstemplate"),
                Path.Combine("Extensions", "MixedCaseInstall", "ItemTemplates", "CSharp", "Nazor", "NazorStyleSheet", "StyleSheet.vstemplate"),
            };
            Assert.All(updated[CSharp], item => Assert.Contains(item.Item2, catalog));
            if (OperatingSystem.IsWindows())
                Assert.All(QuickAddMappings.Merge(new Dictionary<Guid, List<(string, string)>>(), location, root)[CSharp], item => Assert.DoesNotContain(item.Item2, catalog));
        }
        finally
        {
            Directory.Delete(folder);
            Directory.Delete(Path.GetDirectoryName(folder)!);
            Directory.Delete(root);
        }
    }

    [Theory]
    [InlineData("Theme.nss")]
    [InlineData("Card.nazor.nss")]
    [InlineData("Theme.NSS")]
    public void Standalone_and_scoped_styles_use_the_same_template(string name)
    {
        var map = QuickAddMappings.Merge(new Dictionary<Guid, List<(string, string)>>(), "extension");
        Assert.EndsWith(Path.Combine("NazorStyleSheet", "StyleSheet.vstemplate"), Assert.Single(map[CSharp], item => Regex.IsMatch(name, item.Item1)).Item2);
    }
}
