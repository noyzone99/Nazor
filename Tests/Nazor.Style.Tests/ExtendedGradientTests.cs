using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class ExtendedGradientTests
{
    [Theory]
    [InlineData("conic-gradient(red, blue)")]
    [InlineData("conic-gradient(from -.25turn at right bottom, red -90deg 90deg, blue 75% 450deg)")]
    [InlineData("conic-gradient(at 2ch 3ex, currentcolor 0grad, blue 6.283185307179586rad)")]
    [InlineData("repeating-conic-gradient(from 90deg, red 0 10%, transparent 10% 25%)")]
    [InlineData("repeating-linear-gradient(to right, red -10px 5px, blue 15px 20px)")]
    [InlineData("repeating-radial-gradient(ellipse 40px 20px at 30% 70%, red 5px, blue 15px)")]
    public void Gradient_forms_round_trip_through_json_csharp_and_canonical_nss(string value)
    {
        var first = NssCompiler.Compile("gradients", $"Panel {{ background-image: {value}; }}");
        Assert.True(first.Success, string.Join("\n", first.Diagnostics));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(first.StyleSheet));
        Assert.Equal(CSharpEmitter.Emit(first.StyleSheet), CSharpEmitter.Emit(restored));
        var declaration = Assert.Single(Assert.Single(restored.Rules).Declarations);
        var parsed = NssCompiler.Compile("gradients", $"Panel {{ {declaration}; }}");
        Assert.True(parsed.Success, string.Join("\n", parsed.Diagnostics));
        Assert.Equal(CSharpEmitter.Emit(first.StyleSheet), CSharpEmitter.Emit(parsed.StyleSheet));
    }

    [Theory]
    [InlineData("conic-gradient(from 2px, red, blue)")]
    [InlineData("conic-gradient(at, red, blue)")]
    [InlineData("conic-gradient(red 3px, blue)")]
    [InlineData("conic-gradient(red, 90deg, blue)")]
    [InlineData("repeating-conic-gradient(red)")]
    [InlineData("repeating-linear-gradient(red 10deg, blue)")]
    [InlineData("conic-gradient(in oklab, red, blue)")]
    [InlineData("conic-gradient(from 1e999deg, red, blue)")]
    [InlineData("conic-gradient(from 1e308turn, red, blue)")]
    public void Unsupported_or_incomplete_gradient_grammar_remains_diagnostic(string value) =>
        Assert.False(NssCompiler.Compile("invalid", $"Panel {{ background-image: {value}; }}").Success);
}
