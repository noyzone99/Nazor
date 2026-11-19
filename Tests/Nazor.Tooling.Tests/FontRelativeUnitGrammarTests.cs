namespace Nazor.Tooling.Tests;

public sealed class FontRelativeUnitGrammarTests
{
    [Fact]
    public void Font_metric_units_are_highlighted()
    {
        const string source = ".prose { max-width: 68ch; min-height: 3ex; }";
        var tokens = GrammarHarness.Instance.Tokenize("source.nss", source).Tokens;
        tokens.Scoped(source, "ch", "keyword.other.unit.nss");
        tokens.Scoped(source, "ex", "keyword.other.unit.nss");
    }
}
