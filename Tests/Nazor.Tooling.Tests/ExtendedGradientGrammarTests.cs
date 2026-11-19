namespace Nazor.Tooling.Tests;

public sealed class ExtendedGradientGrammarTests
{
    [Theory]
    [InlineData("conic-gradient")]
    [InlineData("repeating-linear-gradient")]
    [InlineData("repeating-radial-gradient")]
    [InlineData("repeating-conic-gradient")]
    public void Gradient_functions_are_highlighted_as_whole_function_names(string function)
    {
        var source=$"Panel {{ background-image: {function}(red, blue); }}";
        var tokens=GrammarHarness.Instance.Tokenize("source.nss",source).Tokens;
        tokens.Scoped(source,function,"support.function.misc.nss");
    }
}
