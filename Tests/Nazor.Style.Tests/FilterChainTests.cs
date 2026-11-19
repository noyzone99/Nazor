using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class FilterChainTests
{
    [Theory]
    [InlineData("none")]
    [InlineData("blur() brightness() contrast() grayscale() hue-rotate() invert() opacity() saturate() sepia()")]
    [InlineData("blur(2em) brightness(150%) contrast(.5) grayscale(.4) hue-rotate(-.25turn) invert(20%) opacity(50%) saturate(2) sepia(.2)")]
    [InlineData("blur(2px) blur(3px) invert(1) brightness(2) invert(.5)")]
    public void Both_properties_share_ordered_repeatable_round_tripping(string expression)
    {
        foreach (var property in new[] { "filter","backdrop-filter" })
        {
            var result=NssCompiler.Compile("effects",$"Panel {{ {property}:{expression}; }}");
            Assert.True(result.Success,string.Join("\n",result.Diagnostics));
            var value=Assert.IsType<FilterValue>(Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations)).Value);
            var restored=StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
            Assert.Equal(CSharpEmitter.Emit(result.StyleSheet),CSharpEmitter.Emit(restored));
            var canonical=NssCompiler.Compile("effects",$"Panel {{ {property}:{value}; }}");
            Assert.True(canonical.Success,string.Join("\n",canonical.Diagnostics));
            Assert.Equal(CSharpEmitter.Emit(result.StyleSheet),CSharpEmitter.Emit(canonical.StyleSheet));
        }
    }

    [Theory]
    [InlineData("blur(1%)")]
    [InlineData("brightness(-1)")]
    [InlineData("hue-rotate(30px)")]
    [InlineData("invert(1,2)")]
    [InlineData("url(filter.svg)")]
    [InlineData("none blur(2px)")]
    [InlineData("contrast(1e999)")]
    public void Invalid_filter_inputs_are_diagnostics(string expression) =>
        Assert.False(NssCompiler.Compile("effects",$"Panel {{ filter:{expression}; }}").Success);

    [Fact]
    public void Chains_have_a_bounded_operation_count_and_discrete_noninherited_defaults()
    {
        Assert.True(NssCompiler.Compile("effects","Panel { filter:"+string.Join(" ",Enumerable.Repeat("invert(0)",64))+"; }").Success);
        Assert.False(NssCompiler.Compile("effects","Panel { filter:"+string.Join(" ",Enumerable.Repeat("invert(0)",65))+"; }").Success);
        Assert.Equal(FilterValue.None,StyleProperties.Filter.InitialValue);
        Assert.False(StyleProperties.Filter.Inherited); Assert.False(StyleProperties.Filter.Animatable);
    }
}
