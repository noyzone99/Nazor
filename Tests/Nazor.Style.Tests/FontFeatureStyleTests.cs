using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class FontFeatureStyleTests
{
    [Theory]
    [InlineData("font-feature-settings", "normal")]
    [InlineData("font-feature-settings", "'liga' off, 'kern', 'ss01' 2, 'LIGA' on, 'liga' 1")]
    [InlineData("font-feature-settings", "'abcd' 4294967295")]
    [InlineData("font-synthesis", "none")]
    [InlineData("font-synthesis", "weight")]
    [InlineData("font-synthesis", "style weight")]
    public void Typography_properties_roundtrip_canonical_syntax_and_both_codecs(string property,string value)
    {
        var result=NssCompiler.Compile("type",$"Panel {{ {property}:{value}; }}");
        Assert.True(result.Success,string.Join("\n",result.Diagnostics));
        var declaration=Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        var canonical=NssCompiler.Compile("type",$"Panel {{ {declaration}; }}");
        Assert.True(canonical.Success,string.Join("\n",canonical.Diagnostics));
        Assert.Equal(CSharpEmitter.Emit(result.StyleSheet),CSharpEmitter.Emit(canonical.StyleSheet));
        Assert.Equal(CSharpEmitter.Emit(result.StyleSheet),CSharpEmitter.Emit(StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet))));
        Assert.True(declaration.Property.Inherited); Assert.False(declaration.Property.Animatable);
    }

    [Theory]
    [InlineData("font-feature-settings", "'bad' on")]
    [InlineData("font-feature-settings", "'éabc' on")]
    [InlineData("font-feature-settings", "'kern' -1")]
    [InlineData("font-feature-settings", "'kern' 1.5")]
    [InlineData("font-feature-settings", "'kern' 4294967296")]
    [InlineData("font-feature-settings", "'kern' 1e999")]
    [InlineData("font-feature-settings", "normal, 'kern'")]
    [InlineData("font-synthesis", "weight weight")]
    [InlineData("font-synthesis", "small-caps")]
    [InlineData("font-synthesis", "none style")]
    public void Invalid_typography_is_diagnostic(string property,string value) =>
        Assert.False(NssCompiler.Compile("type",$"Panel {{ {property}:{value}; }}").Success);

    [Fact]
    public void Features_are_case_sensitive_sorted_and_last_duplicate_wins()
    {
        var result=NssCompiler.Compile("type","Panel { font-feature-settings:'liga' off,'kern','LIGA' 2,'liga' on; }");
        var declaration=Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        Assert.Equal(new[] {new FontFeatureSetting("LIGA",2),new("kern"),new("liga")},Assert.IsType<ValueList<FontFeatureSetting>>(declaration.Value).Items);
        Assert.Throws<InvalidDataException>(()=>StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet).Replace("\"kern\"","\"bad\"",StringComparison.Ordinal)));
        Assert.Equal(FontSynthesis.None,StyleProperties.FontSynthesis.InitialValue);
    }
}
