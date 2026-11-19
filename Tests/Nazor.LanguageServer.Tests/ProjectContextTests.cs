using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Tests;

public sealed class ProjectContextTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-project-").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private string Write(string relative, string text = "")
    {
        var path = Path.GetFullPath(Path.Combine(directory, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void The_response_file_of_the_build_gives_references_sources_and_components()
    {
        Write("Demo.csproj", "<Project Sdk=\"Nazor.Sdk/0.1.0\" />");
        var app = Write("App.nazor", "<Container />");
        var codeBehind = Write("App.nazor.cs");
        var core = typeof(Nazor.ComponentBase).Assembly.Location;
        Write("obj/Debug/net10.0/Nazor/nazorc.rsp", string.Join("\n",
            "generate", "--out", Path.Combine(directory, "obj"), "--root-namespace", "Demo.App", "--project-dir", directory,
            "--reference", core, "--reference", Path.Combine(directory, "missing.dll"), "--source", codeBehind,
            "--source", Path.Combine(directory, "Deleted.cs"), app));

        var context = ProjectContext.ForDocument(Write("Pages/Page.nazor"));
        Assert.Equal(directory, context.Directory);
        Assert.Equal("Demo.App", context.RootNamespace);
        Assert.Equal([core], context.References);
        Assert.Equal([codeBehind], context.CSharpSources);
        Assert.Equal([app], context.Components);
        Assert.EndsWith("nazorc.rsp", context.ResponseFile);
    }

    [Fact]
    public void A_project_that_was_never_built_uses_the_controls_and_its_files()
    {
        Write("Demo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>Demo.Root</RootNamespace></PropertyGroup></Project>");
        var app = Write("App.nazor");
        var page = Write("Pages/Page.nazor");
        Write("bin/Debug/Copy.nazor");
        Write("obj/Debug/Generated.cs");
        var code = Write("App.nazor.cs");

        var context = ProjectContext.ForDocument(app);
        Assert.Equal("Demo.Root", context.RootNamespace);
        Assert.Null(context.ResponseFile);
        Assert.Contains(typeof(Nazor.Controls.Button).Assembly.Location, context.References);
        Assert.Equal([app, page], context.Components);
        Assert.Equal([code], context.CSharpSources);
    }

    [Fact]
    public void The_newest_response_file_wins()
    {
        Write("Demo.csproj");
        var debug = Write("obj/Debug/net10.0/Nazor/nazorc.rsp", "generate\n--root-namespace\nDebug");
        var release = Write("obj/Release/net10.0/Nazor/nazorc.rsp", "generate\n--root-namespace\nRelease");
        File.SetLastWriteTimeUtc(debug, DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(release, ProjectContext.LatestResponseFile(directory).Path);
        Assert.Equal("Release", ProjectContext.ForDocument(Path.Combine(directory, "App.nazor")).RootNamespace);
    }

    [Fact]
    public void A_build_with_unchanged_arguments_can_switch_the_project_configuration()
    {
        Write("Demo.csproj");
        var app = Write("App.nazor");
        var debug = Write("obj/Debug/net10.0/Nazor/nazorc.rsp", "generate\n--root-namespace\nDebug");
        var release = Write("obj/Release/net10.0/Nazor/nazorc.rsp", "generate\n--root-namespace\nRelease");
        var debugStamp = Write("obj/Debug/net10.0/Nazor/nazor.stamp");
        var releaseStamp = Write("obj/Release/net10.0/Nazor/nazor.stamp");
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(debug, now.AddMinutes(-10));
        File.SetLastWriteTimeUtc(debugStamp, now.AddMinutes(-9));
        File.SetLastWriteTimeUtc(release, now.AddMinutes(-6));
        File.SetLastWriteTimeUtc(releaseStamp, now.AddMinutes(-5));
        Assert.Equal("Release", ProjectContext.ForDocument(app).RootNamespace);

        File.SetLastWriteTimeUtc(debugStamp, now);
        Assert.Equal(debug, ProjectContext.LatestResponseFile(directory).Path);
        Assert.Equal("Debug", ProjectContext.ForDocument(app).RootNamespace);
    }

    [Fact]
    public void Build_output_is_not_source()
    {
        Assert.True(ProjectContext.IsBuildOutput(directory, Path.Combine(directory, "bin", "x.nazor")));
        Assert.True(ProjectContext.IsBuildOutput(directory, Path.Combine(directory, "OBJ", "a", "x.cs")));
        Assert.False(ProjectContext.IsBuildOutput(directory, Path.Combine(directory, "binary", "x.nazor")));
    }
}
