using Nazor.Authoring;

namespace Nazor.LanguageServer.Tests;

public sealed class IndentationTests
{
    [Theory]
    [InlineData("@code {\n|", 4)]
    [InlineData("@style {\n    .a {\n|", 8)]
    [InlineData("@code {\n    void Go() {\n        Run();\n|", 8)]
    [InlineData("@code {\n    void Go() {\n        Run();\n    }\n|", 4)]
    [InlineData("@code {\n    void Go() {\n        Run();\n|    }\n}", 4)]
    [InlineData(".a {\n\tColor: Red;\n|", 4)]
    [InlineData(".a {\n    /* { */\n|", 4)]
    [InlineData("@code {\n    var value = @\"a\"\"{\";\n|", 4)]
    [InlineData("@code {\n    var value = \"\"\"{\"\"\";\n|", 4)]
    [InlineData("@code {\n    var value = $\"{Format(\"}\")}\";\n|", 4)]
    public void Native_and_server_share_indentation(string text, int expected)
    {
        var offset = text.IndexOf('|');
        Assert.Equal(expected, CodeIndentation.Desired(text.Remove(offset, 1), offset, 4));
    }
}
