using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class BackdropFilterTests
{
    [Theory]
    [InlineData("none")]
    [InlineData("blur(6px)")]
    [InlineData("saturate(0)")]
    [InlineData("blur(26px) saturate(170%)")]
    [InlineData("saturate(.8) blur(6px)")]
    [InlineData("blur(calc(1em + 2px)) saturate(1.5)")]
    public void Filter_values_preserve_order_and_units_through_both_compiler_outputs(string text)
    {
        var result = NssCompiler.Compile("backdrop", $"Panel {{ backdrop-filter: {text}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        var value = Assert.IsType<FilterValue>(declaration.Value);
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        Assert.Equal(value, Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(restored.Rules).Declarations)).Value);
        Assert.Equal(CSharpEmitter.Emit(result.StyleSheet), CSharpEmitter.Emit(restored));
        var canonical = NssCompiler.Compile("backdrop", $"Panel {{ {declaration}; }}");
        Assert.True(canonical.Success, string.Join("\n", canonical.Diagnostics));
        Assert.Equal(value, Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(canonical.StyleSheet.Rules).Declarations)).Value);
    }

    [Theory]
    [InlineData("blur(-1px)")]
    [InlineData("blur(1%)")]
    [InlineData("saturate(-1)")]
    [InlineData("contrast(-1)")]
    [InlineData("opacity(-1)")]
    [InlineData("drop-shadow(2px 2px)")]
    [InlineData("blur(1px, 2px)")]
    [InlineData("url(filter.svg)")]
    public void Unsupported_or_unbounded_operation_lists_are_diagnostics(string text) =>
        Assert.False(NssCompiler.Compile("backdrop", $"Panel {{ backdrop-filter: {text}; }}").Success);

    [Fact]
    public void Filtering_defaults_to_none_and_does_not_inherit_or_animate()
    {
        Assert.Equal(FilterValue.None, StyleProperties.BackdropFilter.InitialValue);
        Assert.False(StyleProperties.BackdropFilter.Inherited);
        Assert.False(StyleProperties.BackdropFilter.Animatable);
    }
}
