using Nazor.Compiler;

namespace Nazor.Compiler.Tests;

public sealed class AnalysisTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-analysis-").FullName;
    private readonly Dictionary<string, string> documents = new(StringComparer.OrdinalIgnoreCase);

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private NazorCompiler Compiler() => new(new NazorCompilerOptions
    {
        References = [Build.Core, Build.Controls, Build.Style],
        CSharpSources = documents.Keys.Where(path=>path.EndsWith(".cs",StringComparison.OrdinalIgnoreCase)).ToArray(),
        ProjectDirectory = directory,
        SourceText = path => documents.GetValueOrDefault(path),
    });

    private string Open(string name, string text)
    {
        var path = Path.Combine(directory, name);
        documents[path] = text;
        return path;
    }

    [Fact]
    public void Inherited_parameters_can_have_the_same_name_as_a_generic_component()
    {
        Open("Shared.cs","""
            namespace Test;
            public class Shared<T> : Nazor.ComponentBase
            {
                [Nazor.Parameter] public T NameClash { get; set; } = default!;
            }
            public class NameClash<T> : Shared<T> { }
            """);
        var page = Open("Page.nazor","@using Test\n<NameClash T=\"int\" NameClash=\"7\" />");
        var result = Compiler().Analyze([page]);
        Assert.DoesNotContain(result.Diagnostics,diagnostic=>diagnostic.Severity==NazorDiagnosticSeverity.Error);
        Assert.Contains(Assert.Single(result.Catalog.Find("NameClash")).Parameters,parameter=>parameter.Name=="NameClash");
        Assert.Empty(result.Catalog.Find("__NazorFacadeParameters0"));
    }

    [Fact]
    public void Unknown_parameters_of_directive_attributes_and_modifiers_that_are_not_booleans_are_errors()
    {
        var path = Open("Values.nazor", """
            <TextField @bind-Value="name" @bind-Value:colore="x" />
            <Button @onclick="Go" @onclick:stopPropagation="123" @onkeydown:colore="true">Go</Button>
            <Button @onclick="Go" @onclick:stopPropagation @onclick:preventDefault="false">Ok</Button>
            @code {
                private string name = "";
                private string x = "";
                private void Go() { }
            }
            """.ReplaceLineEndings("\n"));
        var compiler = Compiler();
        var analysis = compiler.Analyze([path]);
        var bind = Assert.Single(analysis.Diagnostics, d => d.Id == "NZC013");
        Assert.Equal((1, 32), (bind.Line, bind.Column));
        Assert.Contains("'colore' is not a parameter of '@bind-Value'", bind.Message);
        var key = Assert.Single(analysis.Diagnostics, d => d.Id == "NZC012");
        Assert.Equal(2, key.Line);
        Assert.Contains("'@onkeydown'", key.Message);

        documents[path] = """
            <Button @onclick="Go" @onclick:stopPropagation="123">Go</Button>
            @code {
                private void Go() { }
            }
            """.ReplaceLineEndings("\n");
        var generated = compiler.Generate([path]);
        var error = Assert.Throws<NazorCompilationException>(() => compiler.Emit(generated, "Values"));
        var conversion = Assert.Single(error.Diagnostics, d => d.Id == "CS1503");
        Assert.Equal((path, 1, 49), (conversion.Path, conversion.Line, conversion.Column));
    }

    [Fact]
    public void Errors_and_warnings_are_returned_without_throwing()
    {
        var analysis = Build.Compiler().Analyze([Build.Nazor("HtmlElement")]);
        var html = Assert.Single(analysis.Diagnostics, d => d.Id == "NZC010" && d.Line == 2);
        Assert.Equal(5, html.Column);
        Assert.True(html.IsError);
    }

    [Fact]
    public void Open_documents_are_compiled_instead_of_the_files()
    {
        var path = Open("Draft.nazor", """
            <Button @onclick="Go">Go</Button>
            @code { private void Go() { } }
            """);
        var analysis = Compiler().Analyze([path]);
        Assert.Empty(analysis.Diagnostics);
        Assert.Equal(path, Assert.Single(analysis.Components).SourcePath);

        documents[path] = "<Buton />";
        var unknown = Assert.Single(Compiler().Analyze([path]).Diagnostics, d => d.Id == "RZ10012");
        Assert.Equal((1, 1), (unknown.Line, unknown.Column));
        Assert.Equal("<Buton />".Length, unknown.Length);
    }

    [Fact]
    public void Style_errors_of_open_documents_have_position_and_length()
    {
        var path = Open("Styled.nazor", "<TextBlock Class=\"x\" />\n\n@style {\n    .x { colr: red; }\n}\n");
        var error = Assert.Single(Compiler().Analyze([path]).Diagnostics);
        Assert.Equal(("NZS101", 4, 10, 4), (error.Id, error.Line, error.Column, error.Length));
        Assert.Equal(path, error.Path);
    }

    [Fact]
    public void A_file_whose_styles_cannot_be_extracted_is_still_compiled()
    {
        var broken = Open("Broken.nazor", "<TextBlock />\n@style missing\n");
        var fine = Open("Fine.nazor", "<Container><TextBlock /></Container>");
        var analysis = Compiler().Analyze([broken, fine]);
        Assert.Contains(analysis.Diagnostics, d => d.Id == "NZS001" && d.Path == broken && d.Line == 2);
        Assert.Equal([broken, fine], analysis.Components.Select(c => c.SourcePath).Order());
    }

    [Theory]
    [InlineData("@style {\n    .x { gap: 4px; }\n    .y {\n\n@code {\n    private int count;\n}\n", "NZS002")]
    [InlineData("@code {\n    [Parameter] public Strin Title { get; set; }\n}\n", null)]
    [InlineData("@code {\n    [Parameter] public string Title { get; }\n}\n", "NZC002")]
    public void A_component_being_edited_is_still_compiled(string code, string? error)
    {
        var path = Open("Edited.nazor", "<Container>\n    <TextBlock />\n</Container>\n\n" + code);
        var analysis = Compiler().Analyze([path]);
        var component = Assert.Single(analysis.Components);
        Assert.Equal(path, component.SourcePath);
        if (error is null) Assert.Empty(analysis.Diagnostics);
        else Assert.Equal(error, analysis.Diagnostics[0].Id);
        Assert.NotEmpty(analysis.Catalog.Find("TextBlock"));
        if (error == "NZS002") Assert.Contains("private int count;", component.CSharp);
    }

    [Fact]
    public void Components_of_the_project_see_each_other_and_their_unsaved_parameters()
    {
        var card = Open("Card.nazor", "<TextBlock>@Title</TextBlock>\n@code { [Parameter] public string? Title { get; set; } }");
        var page = Open("Page.nazor", "<Card Title=\"Hello\" />");
        var analysis = Compiler().Analyze([card, page]);
        Assert.Empty(analysis.Diagnostics);
        var component = Assert.Single(analysis.Catalog.Find("Card"));
        Assert.Equal(("Title", "string"), (Assert.Single(component.Parameters).Name, component.Parameters[0].TypeName));

        documents[card] = "<TextBlock>@Caption</TextBlock>\n@code { [Parameter] public string? Caption { get; set; } }";
        Assert.Equal("Caption", Assert.Single(Assert.Single(Compiler().Analyze([card, page]).Catalog.Find("Card")).Parameters).Name);
    }

    [Fact]
    public void The_catalog_describes_controls_parameters_and_directive_attributes()
    {
        var path = Open("Page.nazor", "<Container />");
        var catalog = Compiler().Analyze([path]).Catalog;

        var button = Assert.Single(catalog.Find("Button"));
        Assert.Equal("Nazor.Controls.Button", button.TypeName);
        Assert.Equal("Nazor.Controls", button.Namespace);
        Assert.Contains(button.Parameters, p => p.Name == "OnClick" && p.Kind == NazorParameterKind.EventCallback && p.TypeName.StartsWith("Nazor.EventCallback<", StringComparison.Ordinal));
        Assert.Contains(button.Parameters, p => p.Name == "ChildContent" && p.Kind == NazorParameterKind.ChildContent);
        Assert.Contains(button.Parameters, p => p.Name == "Disabled" && p.Kind == NazorParameterKind.Value && p.TypeName == "bool");
        Assert.Single(catalog.Find("Nazor.Controls.Button"));

        Assert.Contains(catalog.DirectiveAttributesFor("Button"), a => a.Name == "@onclick");
        Assert.Contains(catalog.DirectiveAttributesFor("Container"), a => a.Name == "@onkeydown:preventDefault");
        Assert.Contains(catalog.DirectiveAttributesFor("Container"), a => a.Name == "@onkeydown:stopPropagation");
        Assert.Contains(catalog.DirectiveAttributesFor("TextField"), a => a.Name == "@oninput");
        Assert.Contains(catalog.DirectiveAttributesFor("Button"), a => a.Name == "@key");
        Assert.Contains(catalog.DirectiveAttributesFor("TextField"), a => a.Name == "@bind-Value");
        Assert.DoesNotContain(catalog.DirectiveAttributesFor("Button"), a => a.Name == "@bind-Value");
        Assert.DoesNotContain(catalog.Components, c => c.TypeName.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Mappings_locate_the_csharp_of_the_file_in_the_generated_code()
    {
        const string text = "<Container Class=\"counter\">\r\n    <TextBlock>Count: @count</TextBlock>\r\n    <Button @onclick=\"Increment\" Disabled=\"@(count > 9)\">Go</Button>\r\n</Container>\r\n\r\n@style {\r\n    .counter { gap: 4px; }\r\n}\r\n\r\n@code {\r\n    private int count;\r\n    private void Increment() => count++;\r\n}\r\n";
        var path = Open("Mapped.nazor", text);
        var component = Assert.Single(Compiler().Analyze([path]).Components);
        Assert.NotEmpty(component.Mappings);
        Assert.All(component.Mappings, m => Assert.Equal(text.Substring(m.OriginalStart, m.Length), component.CSharp.Substring(m.GeneratedStart, m.Length)));
        Assert.Equal(component.Mappings.OrderBy(m => m.OriginalStart), component.Mappings);

        bool Mapped(string code, string prefix = "")
        {
            var start = text.IndexOf(prefix + code, StringComparison.Ordinal) + prefix.Length;
            return component.Mappings.Any(m => m.OriginalStart <= start && start + code.Length <= m.OriginalEnd);
        }
        foreach (var (prefix, code) in (ReadOnlySpan<(string, string)>)[("@", "count<"), ("\"", "Increment\""), ("@(", "count > 9"), ("", "private int count;"), ("", "private void Increment() => count++;")])
        {
            var csharp = code.TrimEnd('<', '"');
            Assert.True(Mapped(csharp, prefix), $"'{csharp}' is not mapped: {string.Join(", ", component.Mappings.Select(m => $"'{text.Substring(m.OriginalStart, m.Length).ReplaceLineEndings(" ")}'"))}");
        }
        Assert.True(Mapped("Container"));
        Assert.True(Mapped("Disabled"));
        Assert.False(Mapped("Go"));
        Assert.False(Mapped("gap: 4px"));
    }

    [Fact]
    public void Roslyn_is_the_version_the_Razor_compiler_is_built_against()
    {
        var razor = System.Reflection.Assembly.Load("Microsoft.CodeAnalysis.Razor.Compiler");
        var expected = razor.GetReferencedAssemblies().Single(a => a.Name == "Microsoft.CodeAnalysis").Version;
        Assert.Equal(expected, typeof(Microsoft.CodeAnalysis.Compilation).Assembly.GetName().Version);
    }

    [Fact]
    public void A_parameter_razor_cannot_set_is_reported_and_left_out()
    {
        var path = Open("Bad.nazor", "<TextBlock />\n@code { [Parameter] public string? Title { get; init; } }");
        var analysis = Compiler().Analyze([path]);
        Assert.Contains(analysis.Diagnostics, d => d.Id == "NZC002" && d.Path == path && d.Line == 2);
        Assert.Empty(Assert.Single(analysis.Catalog.Find("Bad")).Parameters);
    }

    [Fact]
    public void A_failed_discovery_keeps_the_components_of_the_last_one()
    {
        var path = Open("Edited.nazor", "<TextBlock />\n@code { [Parameter] public string? Title { get; set; } }");
        var compiler = Compiler();
        Assert.Single(compiler.Analyze([path]).Components);
        documents[path] = "<TextBlock />\n@code { [Parameter] public Hidden? Title { get; set; } private class Hidden { } }";
        Assert.Empty(Compiler().Analyze([path]).Components);
        var analysis = compiler.Analyze([path]);
        Assert.Single(analysis.Components);
        Assert.NotEmpty(analysis.Diagnostics);
        Assert.NotEmpty(analysis.Catalog.Find("TextBlock"));
    }
}
