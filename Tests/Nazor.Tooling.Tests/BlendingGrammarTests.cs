namespace Nazor.Tooling.Tests;

public sealed class BlendingGrammarTests
{
    [Theory]
    [InlineData("mix-blend-mode", "multiply")]
    [InlineData("isolation", "isolate")]
    [InlineData("background-blend-mode", "color-dodge")]
    public void Blending_properties_are_recognized(string property, string value)
    {
        var source=$"Panel {{ {property}: {value}; }}";
        var tokens=GrammarHarness.Instance.Tokenize("source.nss",source).Tokens;
        tokens.Scoped(source,property+":","support.type.property-name.nss");
    }
}
