using Nazor.Compiler;

namespace Nazor.Compiler.Tests;

public sealed class IdValidationTests
{
    private static (NazorCompiler Compiler, string[] Files) Compiler(params (string File, string Text)[] sources)
    {
        var directory = Path.GetFullPath("id-validation-fixtures");
        var texts = sources.ToDictionary(s => Path.Combine(directory, s.File), s => s.Text);
        return (new(new() { ProjectDirectory = directory, References = [Build.Core, Build.Controls, Build.Style], SourceText = path => texts.GetValueOrDefault(path) }),
            [.. texts.Keys.Where(p => p.EndsWith(".nazor", StringComparison.Ordinal))]);
    }

    [Theory]
    [InlineData("<Button Id=\"same\" />\n<TextBlock Id=\"same\" />")]
    [InlineData("<Container Id=\"same\">\n<Button Id=\"same\" />\n</Container>")]
    [InlineData("<Button Id='same' />\n<Button Id=\"s&#x61;me\" />")]
    public void Duplicate_literal_ids_report_both_declarations_and_fail_generation(string text)
    {
        var (compiler, files) = Compiler(("Duplicate.nazor", text));
        var errors = compiler.Analyze(files).Diagnostics.Where(d => d.Id == "NZC017").OrderBy(d => d.Line).ToArray();
        Assert.Equal(2, errors.Length);
        Assert.All(errors, d => { Assert.True(d.IsError); Assert.Equal(files[0], d.Path); Assert.True(d.Length > 0); });
        Assert.Equal(new[] { 1, 2 }, errors.Select(d => d.Line));
        Assert.Throws<NazorCompilationException>(() => compiler.Generate(files));
    }

    [Fact]
    public void Nested_component_definitions_and_repeated_instances_have_independent_id_scopes()
    {
        var (compiler, files) = Compiler(("Parent.nazor", "<Button Id=\"save\" /><Child /><Child />"),
            ("Child.nazor", "<Button Id=\"save\" />"));
        Assert.DoesNotContain(compiler.Analyze(files).Diagnostics, d => d.Id == "NZC017");
        Assert.Equal(2, compiler.Generate(files).Count);
    }

    [Fact]
    public void Dynamic_values_comments_and_csharp_strings_are_not_static_duplicate_ids()
    {
        var (compiler, files) = Compiler(("Dynamic.nazor", """
            @* <Button Id="save" /> *@
            <Button Id="save" /><Button Id="Save" /><Button Id="" /><Button Id="" />
            <Button Id="@id" /><Button Id="@id" />
            @code { string id = "save"; string markup = "<Button Id='save' />"; }
            """));
        Assert.DoesNotContain(compiler.Analyze(files).Diagnostics, d => d.Id == "NZC017");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Inapplicable_id_rules_are_warnings_at_the_property_in_inline_and_sidecar_styles(bool sidecar)
    {
        const string styles = "#save { object-fit: cover; -windows-badge-size: 12px; } .variable { object-fit: cover; }";
        var sources = new List<(string, string)> { ("Page.nazor", "<Button Id=\"save\" />" + (sidecar ? "" : "\n@style { " + styles + " }")) };
        if (sidecar) sources.Add(("Page.nazor.nss", styles));
        var (compiler, files) = Compiler([.. sources]);
        var warnings = compiler.Analyze(files).Diagnostics.Where(d => d.Id == "NZS112").ToArray();
        Assert.Equal(2, warnings.Length);
        Assert.All(warnings, d => { Assert.False(d.IsError); Assert.EndsWith(sidecar ? ".nazor.nss" : ".nazor", d.Path); });
        var text = sources.Single(s => s.Item1.EndsWith(sidecar ? ".nss" : ".nazor", StringComparison.Ordinal)).Item2;
        var line = text.Split('\n')[warnings[0].Line - 1];
        Assert.Equal("object-fit", line.Substring(warnings[0].Column - 1, warnings[0].Length));
        Assert.Equal(2, Assert.Single(compiler.Generate(files)).Warnings.Count(d => d.Id == "NZS112"));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void Standalone_stylesheets_report_id_warnings_only_for_unique_project_ids(bool ambiguous, int expected)
    {
        var sources = new List<(string, string)>
        {
            ("Page.nazor", "<Button Id=\"save\" />"), ("Global.nss", "#save { object-fit: cover; }")
        };
        if (ambiguous) sources.Add(("Other.nazor", "<Image Id=\"save\" />"));
        var (compiler, files) = Compiler([.. sources]);
        compiler.Generate(files);
        var warnings = new List<NazorDiagnostic>();
        compiler.GenerateStyleSheets([Path.Combine(Path.GetDirectoryName(files[0])!, "Global.nss")], warnings);
        Assert.Equal(expected, warnings.Count(d => d.Id == "NZS112"));
    }
}
