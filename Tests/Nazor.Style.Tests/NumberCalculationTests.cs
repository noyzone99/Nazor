using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class NumberCalculationTests
{
    [Fact]
    public void Unitless_calculations_resolve_variables_and_recompute_inherited_targets()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Nested>();
        var first = NssCompiler.Compile("fractions", "Top { --progress: 0.42; } Leaf { stroke-trim-start: min(1, calc(var(--progress) + 0.07)); stroke-trim-end: calc(1 - 0.07); }");
        Assert.True(first.Success, string.Join("\n", first.Diagnostics));
        engine.Application = first.StyleSheet;
        var leaf = root.Single("Leaf");
        Assert.Equal(0.49, engine.GetStyle(leaf).Get(StyleProperties.StrokeTrimStart), 10);
        Assert.Equal(0.93, engine.GetStyle(leaf).Get(StyleProperties.StrokeTrimEnd), 10);
        var changed = NssCompiler.Compile("fractions", "Top { --progress: 0.98; } Leaf { stroke-trim-start: min(1, calc(var(--progress) + 0.07)); }");
        Assert.True(changed.Success, string.Join("\n", changed.Diagnostics));
        engine.Application = changed.StyleSheet;
        Assert.Equal(1d, engine.GetStyle(leaf).Get(StyleProperties.StrokeTrimStart));
    }

    [Theory]
    [InlineData("calc(2 * 3 - 4)", 2)]
    [InlineData("max(0.25, min(0.7, 0.8))", 0.7)]
    [InlineData("clamp(0, calc((0.25 + 0.5) / 2), 1)", 0.375)]
    public void Ordinary_number_properties_share_the_calculation_parser(string expression, double expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Nested>();
        var result = NssCompiler.Compile("numbers", $"Leaf {{ flex-grow: {expression}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        engine.Application = result.StyleSheet;
        Assert.Equal(expected, engine.GetStyle(root.Single("Leaf")).Get(StyleProperties.FlexGrow), 10);
    }
}
