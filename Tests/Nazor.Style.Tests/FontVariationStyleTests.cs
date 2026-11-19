using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class FontVariationStyleTests
{
    [Fact]
    public void Axis_coordinates_are_case_sensitive_sorted_and_last_duplicate_wins()
    {
        var result = NssCompiler.Compile("axes", "Leaf { font-variation-settings: 'wght' 450.25, 'opsz' 72, 'wght' 800, 'WGHT' -2; font-optical-sizing: none; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var declarations = Assert.Single(result.StyleSheet.Rules).Declarations.OfType<ValueDeclaration>().ToArray();
        var axes = Assert.IsType<ValueList<FontVariationSetting>>(declarations[0].Value);
        Assert.Equal(new[] { new FontVariationSetting("WGHT", -2), new("opsz", 72), new("wght", 800) }, axes.Items);
        Assert.Contains("FontVariationSetting(\"opsz\", 72d)", CSharpEmitter.Emit(result.StyleSheet));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        Assert.Equal(result.StyleSheet.Describe(), restored.Describe());
        var reparsed = NssCompiler.Compile("axes", "Leaf { " + string.Join("; ", declarations.Select(d => d.ToString())) + "; }");
        Assert.True(reparsed.Success, string.Join("\n", reparsed.Diagnostics));
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("'a b ' 1.25")]
    [InlineData("'\\\"\\\\xy' -1.5")]
    public void Normal_and_printable_escaped_axis_tags_round_trip(string value)
    {
        var result = NssCompiler.Compile("axes", $"Leaf {{ font-variation-settings: {value}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        var text = declaration.ToString();
        var restored = NssCompiler.Compile("axes", $"Leaf {{ {text}; }}");
        Assert.True(restored.Success, string.Join("\n", restored.Diagnostics));
        Assert.Equal(declaration.Value, Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(restored.StyleSheet.Rules).Declarations)).Value);
        if (value == "normal") Assert.Equal("font-variation-settings: normal", text);
    }

    [Theory]
    [InlineData("'abc' 1")]
    [InlineData("'abcde' 1")]
    [InlineData("'éabc' 1")]
    [InlineData("'ab\\a c' 1")]
    [InlineData("wght 1")]
    [InlineData("'wght' 1px")]
    [InlineData("'wght' 1e999")]
    [InlineData("'wght' 1,")]
    [InlineData("normal, 'wght' 1")]
    public void Invalid_axis_grammars_are_rejected(string value) => Assert.False(NssCompiler.Compile("axes", $"Leaf {{ font-variation-settings: {value}; }}").Success);

    [Theory]
    [InlineData("auto", FontOpticalSizing.Auto)]
    [InlineData("none", FontOpticalSizing.None)]
    [InlineData("native", FontOpticalSizing.Native)]
    public void Optical_modes_compile_and_round_trip(string keyword, FontOpticalSizing expected)
    {
        var result = NssCompiler.Compile("optical", $"Leaf {{ font-optical-sizing: {keyword}; }}");
        Assert.True(result.Success);
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        Assert.Equal(expected, declaration.Value);
        Assert.Equal($"font-optical-sizing: {keyword}", declaration.ToString());
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
    }

    [Fact]
    public void Both_properties_inherit_and_normal_clears_explicit_axes()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Probe>();
        var result = NssCompiler.Compile("axes", "Parent { font-optical-sizing: none; font-variation-settings: 'opsz' 72; } Reset { font-variation-settings: normal; font-optical-sizing: initial; }");
        Assert.True(result.Success);
        engine.Application = result.StyleSheet;
        Assert.Equal(FontOpticalSizing.None, engine.GetStyle(root.Single("Child")).Get(StyleProperties.FontOpticalSizing));
        Assert.Equal(new FontVariationSetting("opsz", 72), Assert.Single(engine.GetStyle(root.Single("Child")).Get(StyleProperties.FontVariationSettings).Items));
        Assert.Empty(engine.GetStyle(root.Single("Reset")).Get(StyleProperties.FontVariationSettings).Items);
        Assert.Equal(FontOpticalSizing.Auto, engine.GetStyle(root.Single("Reset")).Get(StyleProperties.FontOpticalSizing));
    }

    [Fact]
    public void Json_rejects_malformed_tags_before_reaching_a_font_backend()
    {
        var result = NssCompiler.Compile("axes", "Leaf { font-variation-settings: 'opsz' 72; }");
        var json = StyleSheetJson.Serialize(result.StyleSheet).Replace("\"opsz\"", "\"bad\"", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(json));
    }

    private sealed class Probe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Parent");
            builder.OpenElement(1, "Child"); builder.CloseElement();
            builder.OpenElement(2, "Reset"); builder.CloseElement();
            builder.CloseElement();
        }
    }
}
