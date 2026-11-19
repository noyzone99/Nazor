using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.Compiler.Tests;

public sealed class ProductionPublishVerifierTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "nazor-production-" + Guid.NewGuid().ToString("N"));
    private string Assets => Path.Combine(directory, "project.assets.json");
    private string Inputs => Path.Combine(directory, "inputs.txt");

    public ProductionPublishVerifierTests()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Assets, """{"libraries":{"Nazor.Application/1.0":{"type":"package"}}}""");
        File.WriteAllText(Inputs, typeof(ComponentBase).Assembly.Location + "|Nazor.Core.dll");
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void Ordinary_inputs_and_a_clean_directory_are_accepted()
    {
        var output = Path.Combine(directory, "publish");
        Directory.CreateDirectory(output);
        File.Copy(typeof(ComponentBase).Assembly.Location, Path.Combine(output, "Nazor.Core.dll"));
        Assert.Equal(0, ProductionPublishVerifier.Run([Assets, Inputs]));
        Assert.Equal(0, ProductionPublishVerifier.Run([Assets, Inputs, output]));
    }

    [Theory]
    [InlineData("Nazor.Diagnostics")]
    [InlineData("Nazor.DevTools.Agent")]
    [InlineData("Nazor.DevTools.Protocol")]
    [InlineData("nazor.devtools")]
    public void Resolved_dependencies_are_rejected_even_without_a_published_assembly(string name)
    {
        File.WriteAllText(Assets, "{\"libraries\":{\"" + name + "/1.0\":{\"type\":\"package\"}}}");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
    }

    [Fact]
    public void Publish_destination_paths_and_stale_output_bundles_are_checked()
    {
        File.WriteAllText(Inputs, typeof(ComponentBase).Assembly.Location + "|Nazor.DevTools/cache/library.dll");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
        File.WriteAllText(Inputs, typeof(ComponentBase).Assembly.Location + "|Nazor.Core.dll");
        var output = Path.Combine(directory, "publish", "Nazor.DevTools");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "bundle.js"), "payload");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs, Path.GetDirectoryName(output)!]));
    }

    [Fact]
    public void Renaming_an_assembly_does_not_hide_its_identity_or_references()
    {
        var forbidden = Emit("Nazor.DevTools.Protocol", "public class Contract { }");
        var wrapper = Emit("Application", "public class App { public Contract? Value; }", forbidden);
        var renamed = Path.Combine(directory, "renamed.dll");
        File.Copy(forbidden, renamed);
        File.WriteAllText(Inputs, renamed + "|renamed.dll");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
        File.WriteAllText(Inputs, wrapper + "|Application.dll");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
    }

    [Fact]
    public void Stale_dependency_manifests_are_rejected()
    {
        var path = Path.Combine(directory, "Application.deps.json");
        File.WriteAllText(path, """{"libraries":{"Nazor.DevTools.Agent/1.0":{}}}""");
        File.WriteAllText(Inputs, path + "|Application.deps.json");
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Missing_or_invalid_inputs_fail_closed(string assets)
    {
        File.WriteAllText(Assets, assets);
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
        File.Delete(Assets);
        Assert.Equal(1, ProductionPublishVerifier.Run([Assets, Inputs]));
    }

    private string Emit(string name, string source, params string[] references)
    {
        var path = Path.Combine(directory, name + ".dll");
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source)],
            new[] { typeof(object).Assembly.Location }.Concat(references).Select(file => MetadataReference.CreateFromFile(file)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = compilation.Emit(path);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }
}
