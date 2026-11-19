using Nazor.Compiler;

namespace Nazor.Compiler.Tests;

public sealed class RequiredParameterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Explicit_requirements_are_errors_at_each_use_in_project_and_referenced_components(bool referenced)
    {
        var directory = Path.GetFullPath("required-contract");
        var page = Path.Combine(directory, "Page.nazor");
        var contract = Path.Combine(directory, "Contract.cs");
        var documents = new Dictionary<string, string>
        {
            [page] = "@using Nazor.Sample.Components\n<RequiredCard />\n<RequiredCard Label=\"ok\" Value=\"ok\" />",
            [contract] = """
                using Nazor;
                namespace Nazor.Sample.Components;
                public class RequiredCard : ComponentBase {
                    [Parameter, EditorRequired] public string? Label { get; set; } = "default";
                    [Parameter] public required string Value { get; set; }
                    [Parameter] public string Optional { get; set; } = "default";
                }
                """,
        };
        var compiler = new NazorCompiler(new()
        {
            ProjectDirectory = directory,
            References = referenced ? [Build.Core, Build.Controls, Build.Style, Build.Library] : [Build.Core, Build.Controls, Build.Style],
            CSharpSources = referenced ? [] : [contract],
            SourceText = path => documents.GetValueOrDefault(path),
        });
        var analysis = compiler.Analyze([page]);
        Assert.Equal(2, analysis.Diagnostics.Count);
        Assert.All(analysis.Diagnostics, diagnostic =>
        {
            Assert.Equal("RZ2012", diagnostic.Id);
            Assert.True(diagnostic.IsError);
            Assert.Equal(page, diagnostic.Path);
            Assert.Equal(2, diagnostic.Line);
            Assert.True(diagnostic.Length > 0);
        });
        Assert.Contains(analysis.Diagnostics, d => d.Message.Contains("Label"));
        Assert.Contains(analysis.Diagnostics, d => d.Message.Contains("Value"));
        Assert.Throws<NazorCompilationException>(() => compiler.Generate([page]));
        documents[page] = "@using Nazor.Sample.Components\n<RequiredCard Label=\"@((string?)null)\" Value=\"ok\" />";
        var generated = compiler.Generate([page]);
        compiler.Emit(generated, "RequiredUse");
    }

    [Fact]
    public void Unmarked_nullable_nonnullable_and_default_parameters_remain_optional()
    {
        var directory = Path.GetFullPath("optional-contract");
        var component = Path.Combine(directory, "Optional.nazor");
        var page = Path.Combine(directory, "Page.nazor");
        var documents = new Dictionary<string, string>
        {
            [component] = "@code { [Parameter] public string Text { get; set; } = default!; [Parameter] public string? Nullable { get; set; } [Parameter] public int Count { get; set; } = 3; }",
            [page] = "<Optional />",
        };
        var compiler = new NazorCompiler(new() { ProjectDirectory = directory, References = [Build.Core], SourceText = path => documents.GetValueOrDefault(path) });
        Assert.Empty(compiler.Analyze([component, page]).Diagnostics);
    }
}
