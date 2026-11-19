using Nazor.Compiler;

namespace Nazor.Compiler.Tests;

public sealed class WindowDirectiveTests
{
    private static (NazorCompiler Compiler, string[] Files) Compiler(params (string File, string Text)[] sources)
    {
        var directory = Path.GetFullPath("window-directive-fixtures");
        var texts = sources.ToDictionary(s => Path.Combine(directory, s.File), s => s.Text);
        return (new(new() { ProjectDirectory = directory, References = [Build.Core, Build.Controls, Build.Style], SourceText = path => texts.GetValueOrDefault(path) }), [.. texts.Keys]);
    }

    [Fact]
    public void Directive_generates_a_window_with_normal_reflection_parameters_and_defaults()
    {
        var (compiler, files) = Compiler(("Orders.nazor", """
            @window (int OrderId, string? CustomerName = null)
            <UserTitleBar><TextBlock>@Window.Title</TextBlock></UserTitleBar>
            <TextBlock>@OrderId</TextBlock>
            @code {
                [Parameter] public bool ReadOnly { get; set; }
                protected override void OnInitialized() { Window.Title = $"Order {OrderId}"; }
            }
            """));
        var generated = compiler.Generate(files);
        var type = compiler.Load(generated).ComponentType("Orders");
        Assert.True(typeof(WindowComponentBase).IsAssignableFrom(type));
        Assert.True(type.GetProperty("OrderId")!.IsDefined(typeof(ParameterAttribute), true));
        Assert.True(type.GetProperty("OrderId")!.IsDefined(typeof(EditorRequiredAttribute), true));
        Assert.Null(type.GetProperty("CustomerName")!.GetValue(Activator.CreateInstance(type)));
        Assert.Contains(generated[0].Mappings, m => m.OriginalStart == "@window (int ".Length);
    }

    [Theory]
    [InlineData("@window \"Title\"", "NZC030")]
    [InlineData("@window\n@window", "NZC030")]
    [InlineData("@window (int Id, int id)", "NZC030")]
    [InlineData("@window (int Window)", "NZC030")]
    [InlineData("@window\n@inherits ComponentBase", "NZC030")]
    [InlineData("@if (true) {\n@window\n}", "NZC030")]
    [InlineData("<UserTitleBar />", "NZC032")]
    [InlineData("<UserCaptionButtons />", "NZC032")]
    [InlineData("<Window />", "NZC033")]
    public void Invalid_declarations_are_reported_in_analysis(string text, string diagnostic)
    {
        var (compiler, files) = Compiler(("Invalid.nazor", text));
        Assert.Contains(compiler.Analyze(files).Diagnostics, d => d.Id == diagnostic && d.IsError);
    }

    [Fact]
    public void A_window_cannot_be_used_as_a_child()
    {
        var (compiler, files) = Compiler(("Child.nazor", "@window\n<TextBlock>Child</TextBlock>"), ("Parent.nazor", "@window\n<Child />"));
        Assert.Contains(compiler.Analyze(files).Diagnostics, d => d.Id == "NZC031");
    }

    [Fact]
    public void The_surface_primitive_can_be_composed_without_declaring_a_window()
    {
        var (compiler, files) = Compiler(("CaptionSurface.nazor", "<TitleBar Title=\"Preview\" />"));
        Assert.DoesNotContain(compiler.Analyze(files).Diagnostics, diagnostic => diagnostic.Id == "NZC032");
    }

    [Fact]
    public void Parameters_can_combine_with_explicit_properties_but_not_duplicate_them()
    {
        var (compiler, files) = Compiler(("Duplicate.nazor", "@window (int Id = 0)\n@code { [Parameter] public int id { get; set; } }"));
        Assert.Contains(compiler.Analyze(files).Diagnostics, d => d.Id == "NZC034");
    }
}
