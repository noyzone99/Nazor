using Microsoft.CodeAnalysis.CSharp;
using Nazor.Compiler;
using Nazor.Compiler.Watch;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class LanguageVersionTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-language-").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private const string Component = """
        <TextBlock>@Count</TextBlock>
        @code {
            private readonly List<int> values = [with(capacity: 8), 1, 2, 3];
            public int Count => values.Count;
        }
        """;

    private NazorCompiler Compiler(LanguageVersion version) => new(new NazorCompilerOptions
    {
        References = [Build.Core, Build.Controls, Build.Style],
        ParseOptions = new CSharpParseOptions(version),
    });

    [Fact]
    public void CSharp15_collection_arguments_generate_and_execute_on_the_LTS_tool_runtime()
    {
        Assert.Equal(10, Environment.Version.Major);
        var path = Path.Combine(directory, "Modern.nazor");
        File.WriteAllText(path, Component);
        var compiler = Compiler(LanguageVersion.CSharp15);
        var component = compiler.Generate(path);
        var type = compiler.Load([component]).ComponentType("Modern");
        Assert.Equal(3, type.GetProperty("Count")!.GetValue(Activator.CreateInstance(type)));
        Assert.Contains(component.Mappings, m => Component.Substring(m.OriginalStart, m.Length).Contains("with(capacity", StringComparison.Ordinal));
    }

    [Fact]
    public void CSharp15_collection_arguments_work_inside_markup_expressions()
    {
        const string text = """
            <TextBlock>@Sum([with(capacity: 8), 1, 2, 3])</TextBlock>
            @code { private static int Sum(List<int> values) => values.Sum(); }
            """;
        var path = Path.Combine(directory, "Inline.nazor");
        File.WriteAllText(path, text);
        var compiler = Compiler(LanguageVersion.CSharp15);
        var type = compiler.Load([compiler.Generate(path)]).ComponentType("Inline");
        using var host = new TestHost();
        Assert.Equal("6", host.Render(type).Single("TextBlock").Text());
    }

    [Fact]
    public void Net11_components_are_compiled_against_target_references_without_loading_them_into_the_LTS_runtime()
    {
        var root = typeof(LanguageVersionTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Nazor.TestDotnetRoot").Value!;
        var pack = Directory.EnumerateDirectories(Path.Combine(root, "packs", "Microsoft.NETCore.App.Ref"), "11.*")
            .OrderDescending(StringComparer.Ordinal).First();
        var compiler = new NazorCompiler(new NazorCompilerOptions
        {
            References = [Build.Core, Build.Controls, Build.Style, .. Directory.GetFiles(Path.Combine(pack, "ref", "net11.0"), "*.dll")],
            ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp15),
        });
        var path = Path.Combine(directory, "Modern.nazor");
        File.WriteAllText(path, Component);
        using var image = new MemoryStream(compiler.Emit([compiler.Generate(path)], "Modern.Net11"));
        using var pe = new PEReader(image);
        var metadata = pe.GetMetadataReader();
        Assert.Contains(metadata.AssemblyReferences.Select(metadata.GetAssemblyReference), r =>
            metadata.GetString(r.Name) == "System.Runtime" && r.Version.Major == 11);
        Assert.Equal(10, Environment.Version.Major);
    }

    [Fact]
    public void An_explicit_older_language_version_rejects_CSharp15_syntax()
    {
        var path = Path.Combine(directory, "Modern.nazor");
        File.WriteAllText(path, Component);
        var compiler = Compiler(LanguageVersion.CSharp14);
        var error = Assert.Throws<NazorCompilationException>(() => compiler.Load([compiler.Generate(path)]));
        Assert.Contains(error.Diagnostics, d => d.Message.Contains("15.0", StringComparison.Ordinal));
    }

    [Fact]
    public void Watch_reloads_language_version_and_conditional_symbols_from_build_inputs()
    {
        var path = Path.Combine(directory, "Modern.nazor");
        File.WriteAllText(path, """
            <TextBlock>@Value</TextBlock>
            @code {
            #if MODERN
                [Parameter] public int Value { get; set; } = 15;
            #else
                [Parameter] public string Value { get; set; } = "legacy";
            #endif
            }
            """);
        var app = Path.Combine(directory, "App.nazor");
        File.WriteAllText(app, "<Modern Value=\"@(3)\" />");
        var response = Path.Combine(directory, "nazorc.rsp");
        var output = Path.Combine(directory, "obj", "Nazor");
        var session = new WatchSession(new NazorCompilerOptions(), output, directory, response);
        void Arguments(string version, string symbols) => File.WriteAllLines(response,
            ["generate", "--language-version", version, "--define", symbols, "--reference", Build.Core, "--reference", Build.Controls, "--reference", Build.Style]);
        Arguments("14.0", "LEGACY");
        Assert.Empty(session.Refresh().Diagnostics);
        Arguments("15.0", "MODERN");
        Assert.Empty(session.Refresh().Diagnostics);
        var modern = File.ReadAllText(Path.Combine(output, "Modern.nazor.g.cs"));
        var parsed = WatchSession.ParseResponseFile(File.ReadAllLines(response))!.Value;
        Assert.Equal(LanguageVersion.CSharp15, parsed.ParseOptions.LanguageVersion);
        Assert.Equal(["MODERN"], parsed.ParseOptions.PreprocessorSymbolNames);
        var probe = new NazorCompiler(new NazorCompilerOptions
        {
            References = [Build.Core, Build.Controls, Build.Style],
            ParseOptions = parsed.ParseOptions,
        });
        var type = probe.Load([new GeneratedComponent(path, "Modern.nazor", modern, [], []),
            new GeneratedComponent(app, "App.nazor", File.ReadAllText(Path.Combine(output, "App.nazor.g.cs")), [], [])]).ComponentType("Modern");
        Assert.Equal(15, type.GetProperty("Value")!.GetValue(Activator.CreateInstance(type)));
    }
}
