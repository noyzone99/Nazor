using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TextPositions = Nazor.LanguageServer.Protocol.TextPositions;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Tests;

public sealed class LanguageVersionTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-language-server-").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private const string Component = """
        <TextBlock>@values.Count</TextBlock>
        @code {
            private readonly List<int> values = [with(capacity: 8), 1, 2, 3];
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CSharp15_diagnostics_hover_and_completion_work_for_both_IDE_clients(bool visualStudio)
    {
        Assert.Equal(10, Environment.Version.Major);
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        await using var client = LspClient.InProcess();
        if (visualStudio) await client.InitializeAsVisualStudioAsync();
        else await client.InitializeAsync();
        var path = Path.Combine(directory, "Modern.nazor");
        await client.OpenAsync(path, Component);
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.DoesNotContain(diagnostics, d => d!["severity"]!.GetValue<int>() == 1);
        var position = TextPositions.Position(Component, Component.IndexOf("values.Count", StringComparison.Ordinal) + 2);
        var hover = await client.AtAsync("textDocument/hover", path, position.Line, position.Character);
        Assert.Contains("List", hover!.ToJsonString());
        position = TextPositions.Position(Component, Component.IndexOf("values.Count", StringComparison.Ordinal) + "values.".Length);
        var completion = await client.AtAsync("textDocument/completion", path, position.Line, position.Character);
        Assert.Contains(completion!["items"]!.AsArray(), i => i!["label"]!.GetValue<string>() == "Count");
    }

    [Fact]
    public async Task Changing_build_language_options_recreates_the_compiler_and_editor_project()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project />");
        var obj = Directory.CreateDirectory(Path.Combine(directory, "obj", "Nazor")).FullName;
        File.WriteAllLines(Path.Combine(obj, "nazorc.rsp"), ["generate", "--project-dir", directory,
            "--reference", typeof(ComponentBase).Assembly.Location, "--reference", typeof(Nazor.Controls.Button).Assembly.Location,
            "--reference", typeof(Nazor.Style.StyleSheet).Assembly.Location]);
        var options = Path.Combine(obj, "nazorc.roslyn.rsp");
        File.WriteAllLines(options, ["/langversion:14.0", "/define:LEGACY"]);
        var documents = new DocumentStore();
        documents.Set(LspClient.Uri(Path.Combine(directory, "Modern.nazor")), Component, 1);
        var analyzer = new ProjectAnalyzer(ProjectContext.ForDirectory(directory), documents);
        var old = analyzer.Current();
        Assert.Contains((await old.CSharp.GetCompilationAsync(TestContext.Current.CancellationToken))!.GetDiagnostics(TestContext.Current.CancellationToken), d => d.Severity == DiagnosticSeverity.Error);
        File.WriteAllLines(options, ["/langversion:15.0", "/define:MODERN"]);
        var current = analyzer.Current();
        Assert.NotSame(old, current);
        Assert.DoesNotContain((await current.CSharp.GetCompilationAsync(TestContext.Current.CancellationToken))!.GetDiagnostics(TestContext.Current.CancellationToken), d => d.Severity == DiagnosticSeverity.Error);
        var parseOptions = Assert.IsType<CSharpParseOptions>(current.CSharp.ParseOptions);
        Assert.Equal(LanguageVersion.CSharp15, parseOptions.LanguageVersion);
        Assert.Equal(["MODERN"], parseOptions.PreprocessorSymbolNames);
    }
}
