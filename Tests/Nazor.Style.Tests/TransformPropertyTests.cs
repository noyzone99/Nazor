using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class TransformPropertyTests
{
    [Theory]
    [InlineData("transform: none", "transform: none")]
    [InlineData("transform: translate(10px)", "transform: translate(10px, 0px)")]
    [InlineData("transform: translateX(50%) translateY(-1em)", "transform: translate(50%, 0px), translate(0px, -1em)")]
    [InlineData("transform: translate(calc(100% - 4px), 2vh)", "transform: translate(calc(100% - 4px), 2vh)")]
    [InlineData("transform: scale(1.5)", "transform: scale(1.5, 1.5)")]
    [InlineData("transform: scale(150%, 2) scaleX(0) scaleY(-1)", "transform: scale(1.5, 2), scale(0, 1), scale(1, -1)")]
    [InlineData("transform: rotate(45deg) rotate(0.25turn) rotate(0)", "transform: rotate(45deg), rotate(90deg), rotate(0deg)")]
    [InlineData("transform: skew(10deg) skewX(5deg) skewY(-5deg)", "transform: skew(10deg, 0deg), skew(5deg, 0deg), skew(0deg, -5deg)")]
    [InlineData("transform: matrix(1, 0, 0, 1, 10, 20)", "transform: matrix(1, 0, 0, 1, 10, 20)")]
    [InlineData("TRANSFORM: ROTATE(1DEG)", "transform: rotate(1deg)")]
    [InlineData("transform-origin: left top", "transform-origin: 0% 0%")]
    [InlineData("transform-origin: bottom", "transform-origin: 50% 100%")]
    [InlineData("transform-origin: 10px", "transform-origin: 10px 50%")]
    [InlineData("transform-origin: right 20%", "transform-origin: 100% 20%")]
    [InlineData("transform-origin: -1em 2rem", "transform-origin: -1em 2rem")]
    public void Transforms_compile(string declaration, string expected)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(expected, string.Join("; ", result.StyleSheet.Rules.Single().Declarations));
    }

    [Theory]
    [InlineData("transform: rotate(45)", "Expected an angle")]
    [InlineData("transform: translate(1px, 2px, 3px)", "takes one or two comma-separated values")]
    [InlineData("transform: translate(10px 20px)", "Unexpected '20px'")]
    [InlineData("transform: rotate()", "takes 1 comma-separated value")]
    [InlineData("transform: matrix(1, 0, 0, 1)", "takes 6 comma-separated values")]
    [InlineData("transform: rotateX(10deg)", "3D transform")]
    [InlineData("transform: perspective(100px)", "3D transform")]
    [InlineData("transform: spin(1deg)", "Unknown transform function 'spin()'")]
    [InlineData("transform: 10px", "Expected a transform function")]
    [InlineData("transform: none scale(2)", "Unexpected")]
    [InlineData("transform: scale(1px)", "Expected a number")]
    [InlineData("transform-origin: left top 10px", "Unexpected")]
    [InlineData("transform-origin: 10px left", "Invalid position")]
    public void Invalid_transforms_are_reported(string declaration, string message)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Relative_lengths_of_translations_and_origins_become_px()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Viewport = new Viewport(1000, 500) };
        var root = host.Render<Nested>();
        engine.Application = NssCompiler.Compile("t", "Top { font-size: 10px; transform: translate(2em, 1vh) rotate(5deg); transform-origin: 1em 50%; }").StyleSheet;
        var style = engine.GetStyle(root.Single("Top"));
        Assert.Equal("translate(20px, 5px), rotate(5deg)", style.Get(StyleProperties.Transform).ToString());
        Assert.Equal(new TransformOrigin(Length.Px(10), Length.Percent(50)), style.Get(StyleProperties.TransformOrigin));
    }
}
