using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class FontSymbolStyleTests
{
    [Theory]
    [InlineData("none")]
    [InlineData("\"Native Icons\" \"A\", \"Portable Icons\" \"😀\"")]
    [InlineData("\"A \\\"quoted\\\" family\\\\name\" \"B\"")]
    public void Paired_symbols_round_trip_through_NSS_JSON_and_generated_code(string value)
    {
        var result = NssCompiler.Compile("symbols", "Icon { font-symbol: " + value + "; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        Assert.Equal(result.StyleSheet.Describe(), restored.Describe());
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(restored.Rules).Declarations));
        Assert.Equal(StyleProperties.FontSymbol, declaration.Property);
        var recompiled = NssCompiler.Compile("symbols", "Icon { " + declaration + "; }");
        Assert.True(recompiled.Success, string.Join("\n", recompiled.Diagnostics));
        Assert.Equal(restored.Describe(), recompiled.StyleSheet.Describe());
        var code = CSharpEmitter.Emit(restored);
        Assert.Contains("ValueList<global::Nazor.FontSymbol>", code);
        Assert.Equal(code, CSharpEmitter.Emit(recompiled.StyleSheet));
        Assert.False(StyleProperties.FontSymbol.Inherited);
        Assert.False(StyleProperties.FontSymbol.Animatable);
    }

    [Theory]
    [InlineData("\"Family\"")]
    [InlineData("Family \"A\"")]
    [InlineData("\"\" \"A\"")]
    [InlineData("\"Family\" \" \"")]
    [InlineData("\"Family\" \"A\tB\"")]
    [InlineData("\"Family\" \"A\" \"B\"")]
    [InlineData("none, \"Family\" \"A\"")]
    public void Malformed_pairs_are_rejected(string value) =>
        Assert.False(NssCompiler.Compile("symbols", "Icon { font-symbol: " + value + "; }").Success);

    [Fact]
    public void Candidate_lists_are_bounded()
    {
        var pair = "\"Family\" \"A\"";
        Assert.True(NssCompiler.Compile("symbols", "Icon { font-symbol: " + string.Join(", ", Enumerable.Repeat(pair, 32)) + "; }").Success);
        Assert.False(NssCompiler.Compile("symbols", "Icon { font-symbol: " + string.Join(", ", Enumerable.Repeat(pair, 33)) + "; }").Success);
    }

    [Fact]
    public void Invalid_control_text_cannot_silently_serialize_to_a_different_glyph()
    {
        Assert.Throws<InvalidOperationException>(() => new FontSymbol("Family", "A\nB").ToString());
        var result = NssCompiler.Compile("symbols", "Icon { font-symbol: \"Family\" \"A\"; }");
        var json = StyleSheetJson.ToJson(result.StyleSheet);
        json["rules"]![0]!["declarations"]![0]!["value"]!["items"]![0]!["glyph"] = "A\nB";
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.FromJson(json));
    }
}
