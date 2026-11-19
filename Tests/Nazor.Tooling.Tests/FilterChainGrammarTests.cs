namespace Nazor.Tooling.Tests;

public sealed class FilterChainGrammarTests
{
    [Theory]
    [InlineData("blur")]
    [InlineData("brightness")]
    [InlineData("contrast")]
    [InlineData("grayscale")]
    [InlineData("hue-rotate")]
    [InlineData("invert")]
    [InlineData("opacity")]
    [InlineData("saturate")]
    [InlineData("sepia")]
    public void Filter_property_and_functions_are_recognized(string function)
    {
        var source=$"Panel {{ filter:{function}(1); }}";
        var tokens=GrammarHarness.Instance.Tokenize("source.nss",source).Tokens;
        tokens.Scoped(source,"filter:","support.type.property-name.nss");
        tokens.Scoped(source,function,"support.function.misc.nss");
    }
}
