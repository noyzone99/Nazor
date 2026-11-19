using Nazor.VisualStudio;

namespace Nazor.Tooling.Tests;

public sealed class HotReloadFilesTests
{
    [Theory]
    [InlineData("App.nazor", true)]
    [InlineData("_Imports.nazor", true)]
    [InlineData("Components/Badge.nazor.nss", true)]
    [InlineData("Styles/THEME.NSS", true)]
    [InlineData("App.nazor.cs", false)]
    [InlineData("obj/Debug/App.nazor.g.cs", false)]
    [InlineData("bin/Debug/Theme.nss", false)]
    [InlineData("nested/obj/Debug/App.nazor", false)]
    public void Only_source_markup_and_styles_trigger_a_refresh(string relative, bool expected) =>
        Assert.Equal(expected, HotReloadFiles.IsInput(Path.Combine(Path.GetTempPath(), relative)));

    [Fact]
    public void Nested_generated_documents_find_their_build_manifest_without_crossing_the_project_boundary()
    {
        var root = Directory.CreateTempSubdirectory("nazor-ide-").FullName;
        try
        {
            var output = Directory.CreateDirectory(Path.Combine(root, "app", "obj", "Debug", "net10.0", "Nazor")).FullName;
            var manifest = Path.Combine(output, "nazorc.ide");
            File.WriteAllText(manifest, "dotnet\ncompiler.dll\n");
            File.WriteAllText(Path.Combine(output, "Ignored.nazor"), "");
            File.WriteAllText(Path.Combine(root, "app", "App.nazor"), "");
            File.WriteAllText(Path.Combine(root, "app", "_Imports.nazor"), "");
            File.WriteAllText(Path.Combine(root, "app", "App.nazor.nss"), "");
            File.WriteAllText(Path.Combine(root, "app", "Theme.nss"), "");
            Assert.Equal([Path.Combine(root, "app", "App.nazor"), Path.Combine(root, "app", "Theme.nss")], HotReloadFiles.BuildInputs(Path.Combine(root, "app")));
            var project = Path.Combine(root, "app", "App.csproj");
            Assert.Equal(manifest, HotReloadFiles.FindManifest(Path.Combine(output, "Components", "Badge.nazor.g.cs"), project));
            Assert.Null(HotReloadFiles.FindManifest(Path.Combine(output, "Components", "Badge.nazor.g.cs"), Path.Combine(root, "other", "Other.csproj")));
            Assert.Null(HotReloadFiles.FindManifest(Path.Combine(root, "app", "Program.cs"), project));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
