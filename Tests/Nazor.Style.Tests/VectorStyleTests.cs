using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class VectorStyleTests
{
    [Theory]
    [InlineData("border-image-source: linear-gradient(to top, red 1px, blue 3px)")]
    [InlineData("background-clip: padding-box")]
    [InlineData("stroke-width: 9.375%; stroke-radius: 43.75%; stroke-trim-start: 0.1; stroke-trim-end: 0.5")]
    [InlineData("outline-inner-width: 1px; outline-inner-color: #ffffffb3")]
    [InlineData("ring-shape: border-box; stroke-trim-start: min(1, calc(0.42 + 0.07)); stroke-trim-end: calc(1 - 0.07)")]
    [InlineData("stroke-trim-end: clamp(0, calc((0.2 + 0.3) * 2), 1)")]
    [InlineData("number-motion: linear-increase; number-motion-duration: 2s; number-motion-decay: 0.14; number-motion-tolerance: 0.0005; number-boundary-tolerance: 0.003")]
    [InlineData("cursor: ew-resize; flex-direction: column-reverse; scroll-duration: 180ms")]
    public void Vector_border_and_focus_values_compile_and_survive_serialization(string declaration)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var serialized = string.Join("; ", result.StyleSheet.Rules.Single().Declarations);
        var roundTrip = NssCompiler.Compile("t", $".x {{ {serialized}; }}");
        Assert.True(roundTrip.Success, string.Join("\n", roundTrip.Diagnostics));
        Assert.Equal(serialized, string.Join("; ", roundTrip.StyleSheet.Rules.Single().Declarations));
        var json = StyleSheetJson.Serialize(result.StyleSheet);
        Assert.Equal(json, StyleSheetJson.Serialize(StyleSheetJson.Deserialize(json)));
    }

    [Theory]
    [InlineData("border-image-source: url(a.png)")]
    [InlineData("border-image-source: linear-gradient(red, blue), linear-gradient(red, blue)")]
    [InlineData("background-clip: content-box")]
    [InlineData("stroke-trim-end: 1.1")]
    [InlineData("stroke-trim-start: -0.1")]
    [InlineData("stroke-width: -1px")]
    [InlineData("outline-inner-width: -1px")]
    [InlineData("ring-shape: leaf")]
    [InlineData("stroke-trim-start: calc(1px + 0.1)")]
    [InlineData("stroke-trim-end: calc(1px / 2)")]
    [InlineData("stroke-trim-end: calc(1 / 0)")]
    [InlineData("stroke-trim-end: calc(0.8 + 0.3)")]
    [InlineData("stroke-trim-end: calc(10% + 0.1)")]
    [InlineData("number-motion: spring")]
    [InlineData("number-motion-duration: -1ms")]
    [InlineData("number-motion-decay: 1.1")]
    [InlineData("number-motion-tolerance: -0.01")]
    public void Unsupported_or_out_of_range_vector_values_are_rejected(string declaration)
    {
        Assert.False(NssCompiler.Compile("t", $".x {{ {declaration}; }}").Success);
    }
}
