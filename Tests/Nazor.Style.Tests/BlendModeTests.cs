using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class BlendModeTests
{
    [Theory]
    [InlineData("mix-blend-mode", "multiply")]
    [InlineData("mix-blend-mode", "color-dodge")]
    [InlineData("isolation", "isolate")]
    [InlineData("background-blend-mode", "screen, multiply, luminosity")]
    public void Blend_properties_roundtrip_through_both_codecs(string property, string value)
    {
        var result = NssCompiler.Compile("blend", $"Panel {{ {property}: {value}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(CSharpEmitter.Emit(result.StyleSheet), CSharpEmitter.Emit(StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet))));
        var descriptor = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations)).Property;
        Assert.False(descriptor.Inherited); Assert.False(descriptor.Animatable);
    }

    [Theory]
    [InlineData("mix-blend-mode", "plus-lighter")]
    [InlineData("mix-blend-mode", "multiply, screen")]
    [InlineData("isolation", "none")]
    [InlineData("background-blend-mode", "screen,")]
    public void Unsupported_blend_syntax_is_diagnostic(string property, string value) =>
        Assert.False(NssCompiler.Compile("blend", $"Panel {{ {property}: {value}; }}").Success);
}
