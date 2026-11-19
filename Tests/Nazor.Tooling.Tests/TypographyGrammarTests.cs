namespace Nazor.Tooling.Tests;

public sealed class TypographyGrammarTests
{
    [Theory]
    [InlineData("font-synthesis", "weight style")]
    [InlineData("font-feature-settings", "'liga' off")]
    public void Typography_properties_are_recognized(string property,string value)
    {
        var source=$"Panel {{ {property}: {value}; }}";
        var tokens=GrammarHarness.Instance.Tokenize("source.nss",source).Tokens;
        tokens.Scoped(source,property+":","support.type.property-name.nss");
    }
}
