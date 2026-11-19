using Nazor.Style;

namespace Nazor.Tooling.Tests;

public sealed class SpanSplitTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData(" a\tb\r\nc\fd ")]
    [InlineData("a a  b")]
    [InlineData("a\u00a0b")]
    public void Net8_class_splitting_matches_the_framework(string text)
    {
        var actual = new List<string>();
        foreach (var range in SpanSplitExtensions.SplitAny(text.AsSpan(), " \t\r\n\f"))
            actual.Add(text[range]);
        Assert.Equal(text.Split([' ', '\t', '\r', '\n', '\f']), actual);
    }
}
