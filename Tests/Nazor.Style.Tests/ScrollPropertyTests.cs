using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class ScrollPropertyTests
{
    [Theory]
    [InlineData("scroll-behavior: auto; scroll-wheel-step: auto")]
    [InlineData("scroll-behavior: smooth; scroll-duration: 180ms; scroll-timing-function: ease-out; scroll-wheel-step: 24.5px")]
    [InlineData("scroll-wheel-step: 3 lines")]
    [InlineData("scroll-wheel-step: 1.5lines")]
    [InlineData("scroll-wheel-step: 0")]
    [InlineData("scroll-wheel-step: 0 lines")]
    [InlineData("scroll-wheel-step: calc(1em + 8px); scroll-duration: .25s; scroll-timing-function: cubic-bezier(.2, 0, .8, 1)")]
    public void Scroll_values_compile_serialize_and_emit(string declaration)
    {
        var result = NssCompiler.Compile("scroll", "Container { " + declaration + "; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
        Assert.Contains("StyleProperties.Scroll", CSharpEmitter.Emit(result.StyleSheet));
    }

    [Theory]
    [InlineData("scroll-behavior: instant")]
    [InlineData("scroll-wheel-step: -1px")]
    [InlineData("scroll-wheel-step: -1 lines")]
    [InlineData("scroll-wheel-step: -1lines")]
    [InlineData("scroll-wheel-step: 3")]
    [InlineData("scroll-wheel-step: 3 rows")]
    [InlineData("scroll-wheel-step: 20%")]
    [InlineData("scroll-wheel-step: calc(20% + 1px)")]
    [InlineData("scroll-duration: -1ms")]
    [InlineData("scroll-timing-function: cubic-bezier(-1, 0, 1, 1)")]
    public void Invalid_scroll_values_are_diagnostics(string declaration) =>
        Assert.False(NssCompiler.Compile("scroll", "Container { " + declaration + "; }").Success);

    [Fact]
    public void Serialized_line_counts_cannot_be_negative()
    {
        var result = NssCompiler.Compile("scroll", "Container { scroll-wheel-step: 3 lines; }");
        var json = StyleSheetJson.ToJson(result.StyleSheet);
        json["rules"]![0]!["declarations"]![0]!["value"]!["lines"] = -1;
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.FromJson(json));
    }
}
