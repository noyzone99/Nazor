using Nazor.Compiler;
using Nazor.Testing;

namespace Nazor.Compiler.Tests;

public sealed class ParameterValidationTests
{
    [Theory]
    [InlineData("<Card Missing=\"\" />", "NZC014", "Missing")]
    [InlineData("<Card Date=\"\" />", "NZC015", "Date")]
    [InlineData("<Card Count='' />", "NZC015", "Count")]
    [InlineData("<Card Optional=\"\" />", "NZC015", "Optional")]
    [InlineData("<Card Enabled=\"\"></Card>", "NZC015", "Enabled")]
    [InlineData("<Card Changed=\"\" />", "NZC015", "Changed")]
    [InlineData("<Card Date />", "NZC016", "Date")]
    [InlineData("<Card Count></Card>", "NZC016", "Count")]
    [InlineData("<Card Optional />", "NZC016", "Optional")]
    [InlineData("<Card Text />", "NZC016", "Text")]
    [InlineData("<Card Changed />", "NZC016", "Changed")]
    [InlineData("<Card Maybe />", "NZC016", "Maybe")]
    [InlineData("<Card Missing />", "NZC014", "Missing")]
    public void Invalid_parameters_are_errors_in_the_original_tag(string markup, string id, string name)
    {
        var diagnostics = Analyze(markup).Diagnostics.Where(d => d.IsError).ToArray();
        var diagnostic = Assert.Single(diagnostics, d => d.Id == id);
        Assert.Contains(name, diagnostic.Message);
        Assert.Equal(2, diagnostic.Line);
        Assert.Equal(markup.IndexOf(name, StringComparison.Ordinal) + 1, diagnostic.Column);
        Assert.Equal(name.Length, diagnostic.Length);
    }

    [Theory]
    [InlineData("<Card Text=\"\" />")]
    [InlineData("<Card Text='' />")]
    [InlineData("<Card Enabled />")]
    [InlineData("<Card Date=\"DateTime.Now\" Count=\"0\" Enabled=\"false\" />")]
    [InlineData("<Card Optional=\"@((int?)null)\" />")]
    [InlineData("<Card @attributes=\"@(new Dictionary<string, object?> { [\"data-extra\"] = 1 })\" />")]
    public void Strings_explicit_values_and_splats_remain_valid(string markup) => Assert.DoesNotContain(Analyze(markup).Diagnostics, d => d.IsError);

    [Theory]
    [InlineData("string", false)]
    [InlineData("int", true)]
    public void Explicit_generic_arguments_determine_whether_empty_is_valid(string argument, bool error)
    {
        var analysis = Analyze($"<Generic TValue=\"{argument}\" Value=\"\" />");
        Assert.Equal(error, analysis.Diagnostics.Any(d => d.Id == "NZC015"));
    }

    [Theory]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("bool?")]
    public void Generic_non_boolean_parameters_require_an_assignment(string argument) =>
        Assert.Contains(Analyze($"<Generic TValue=\"{argument}\" Value />").Diagnostics, d => d.Id == "NZC016" && d.IsError);

    [Theory]
    [InlineData("bool", false)]
    [InlineData("System.Boolean", false)]
    [InlineData("bool", true)]
    [InlineData("System.Boolean", true)]
    public void Minimized_boolean_parameters_receive_true_at_runtime(string type, bool generic)
    {
        var directory = Path.GetFullPath("minimized-boolean");
        var page = Path.Combine(directory, "Page.nazor");
        var clock = Path.Combine(directory, "Clock.nazor");
        var texts = new Dictionary<string, string>
        {
            [page] = generic ? $"<Clock TValue=\"{type}\" InitialDate /><Clock TValue=\"{type}\" />" : "<Clock InitialDate /><Clock />",
            [clock] = (generic ? "@typeparam TValue\n" : "") + "<TextBlock>@InitialDate</TextBlock>\n@code { [Parameter] public " + (generic ? "TValue" : type) + " InitialDate { get; set; } }",
        };
        var compiler = new NazorCompiler(new() { ProjectDirectory = directory, References = [Build.Core, Build.Controls, Build.Style], SourceText = path => texts.GetValueOrDefault(path) });
        var assembly = compiler.Load(compiler.Generate([page, clock]));
        using var host = new TestHost();
        Assert.Equal("TrueFalse", host.Render(assembly.ComponentType("Page")).Text());
    }

    private static NazorAnalysis Analyze(string markup)
    {
        var directory = Path.GetFullPath("parameter-validation");
        var page = Path.Combine(directory, "Page.nazor");
        var contract = Path.Combine(directory, "Contract.cs");
        var texts = new Dictionary<string, string>
        {
            [page] = "@using Demo\n" + markup,
            [contract] = """
                using System;
                using System.Collections.Generic;
                using Nazor;
                namespace Demo;
                public class Card : ComponentBase {
                    [Parameter] public DateTime Date { get; set; }
                    [Parameter] public int Count { get; set; }
                    [Parameter] public int? Optional { get; set; }
                    [Parameter] public bool Enabled { get; set; }
                    [Parameter] public bool? Maybe { get; set; }
                    [Parameter] public string? Text { get; set; }
                    [Parameter] public EventCallback Changed { get; set; }
                    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object?>? Extra { get; set; }
                }
                public class Generic<TValue> : ComponentBase {
                    [Parameter] public TValue Value { get; set; } = default!;
                }
                """,
        };
        return new NazorCompiler(new() { RootNamespace = "Demo", ProjectDirectory = directory, CSharpSources = [contract],
            References = [Build.Core, Build.Controls, Build.Style], SourceText = path => texts.GetValueOrDefault(path) }).Analyze([page]);
    }
}
