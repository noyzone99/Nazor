using Nazor.LanguageServer.Features;
using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class AutoInsertTests
{
    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public async Task Native_editing_owns_Visual_Studio_auto_insertion(bool visualStudio, bool nativeEditing, bool serverProvider)
    {
        await using var client = LspClient.InProcess();
        var result = await client.InitializeAsync(new JsonObject { ["_vs_supportsVisualStudioExtensions"] = visualStudio },
            new JsonObject { ["nativeEditing"] = nativeEditing });
        Assert.Equal(serverProvider, result!["capabilities"]!["_vs_onAutoInsertProvider"] is not null);
        Assert.Equal(serverProvider, result["capabilities"]!["documentOnTypeFormattingProvider"] is not null);
        Assert.NotNull(result["capabilities"]!["completionProvider"]);
    }

    private static string? Type(string text, char character, bool component = true, bool insertSpaces = true)
    {
        var offset = text.IndexOf('|');
        text = text.Remove(offset, 1);
        if (AutoInsert.OnType(text, offset, character, component, 4, insertSpaces) is not { } edit) return null;
        Assert.True(edit.Start <= offset && offset <= edit.End);
        return text[..edit.Start] + edit.NewText.Replace("$0", "|") + text[edit.End..];
    }

    [Theory]
    [InlineData("<Container>|", "<Container>|</Container>")]
    [InlineData("<Button Class=\"a\" @onclick=\"Go\">|", "<Button Class=\"a\" @onclick=\"Go\">|</Button>")]
    [InlineData("<Container>\n    <Container>|\n</Container>", "<Container>\n    <Container>|</Container>\n</Container>")]
    [InlineData("<Nazor.Controls.Button>|", "<Nazor.Controls.Button>|</Nazor.Controls.Button>")]
    public void The_end_tag_follows_a_start_tag(string text, string expected) => Assert.Equal(expected, Type(text, '>'));

    [Theory]
    [InlineData("<Container>|</Container>")]
    [InlineData("<Container>|\n    <TextBlock />\n</Container>")]
    [InlineData("<Button />|")]
    [InlineData("<Container></Container>|")]
    [InlineData("<Button Text=\"a>|\" />")]
    [InlineData("<Button @onclick=\"() =>|\" />")]
    [InlineData("@code {\n    bool Big => count >|\n}")]
    [InlineData("@* <Container>| *@")]
    [InlineData("@style {\n    .a >|\n}")]
    public void Nothing_is_closed_where_no_element_starts(string text) => Assert.Null(Type(text, '>'));

    [Theory]
    [InlineData("<Button Text=|", "<Button Text=\"|\"")]
    [InlineData("<Button @onclick=| />", "<Button @onclick=\"|\" />")]
    [InlineData("<TextField @bind-Value=|", "<TextField @bind-Value=\"|\"")]
    public void Attribute_values_get_their_quotes(string text, string expected) => Assert.Equal(expected, Type(text, '='));

    [Theory]
    [InlineData("<Button Text=|\"a\" />")]
    [InlineData("<Button Text =|")]
    [InlineData("<TextBlock>a=|</TextBlock>")]
    [InlineData("@code {\n    int a=|\n}")]
    [InlineData("<Button Text=\"a=|\" />")]
    public void Quotes_only_after_an_attribute_name(string text) => Assert.Null(Type(text, '='));

    [Theory]
    [InlineData("@code {\r\n    void Go()\r\n    {\r\n|}\r\n}", "@code {\r\n    void Go()\r\n    {\r\n        |\r\n    }\r\n}")]
    [InlineData("@code {\r\n    void Go()\r\n    {\r\n|\r\n    }\r\n}", "@code {\r\n    void Go()\r\n    {\r\n        |\r\n    }\r\n}")]
    [InlineData("@code {\n    void Go() {\n    |    }\n}", "@code {\n    void Go() {\n        |\n    }\n}")]
    [InlineData("@code {\n    void Go()\n    {\n|\n        count++;\n    }\n}", "@code {\n    void Go()\n    {\n        |\n        count++;\n    }\n}")]
    [InlineData("@code {\n|\n}", "@code {\n    |\n}")]
    [InlineData("@style {\n    .a {\n|}\n}", "@style {\n    .a {\n        |\n    }\n}")]
    [InlineData("<Container>\n|</Container>", "<Container>\n    |\n</Container>")]
    [InlineData("<Container>\n    <Container Class=\"a\">\n|\n    </Container>\n</Container>", "<Container>\n    <Container Class=\"a\">\n        |\n    </Container>\n</Container>")]
    [InlineData("<Container>\n|\n    <TextBlock />\n</Container>", "<Container>\n    |\n    <TextBlock />\n</Container>")]
    [InlineData("<Button Class=\"a\"\n|\n<TextBlock />", "<Button Class=\"a\"\n    |\n<TextBlock />")]
    [InlineData("@code {   \n|}", "@code {\n    |\n}")]
    [InlineData("@code {\n    private int count;\n|", "@code {\n    private int count;\n    |")]
    [InlineData("@code {\n    void Go() {\n        Run();\n|\n    }\n}", "@code {\n    void Go() {\n        Run();\n        |\n    }\n}")]
    [InlineData("@style {\n    .a {\n        Color: Red;\n|\n    }\n}", "@style {\n    .a {\n        Color: Red;\n        |\n    }\n}")]
    [InlineData("@code {\n    Run(\n|)\n}", "@code {\n    Run(\n        |\n    )\n}")]
    [InlineData("@code {\n    int[] values = [\n|]\n}", "@code {\n    int[] values = [\n        |\n    ]\n}")]
    [InlineData("@code {\n|", "@code {\n    |")]
    public void A_new_line_is_indented(string text, string expected) => Assert.Equal(expected, Type(text, '\n'));

    [Theory]
    [InlineData(".a {\n|}", ".a {\n    |\n}")]
    [InlineData("@media (max-width: 720px) {\n    .a {\n|\n    }\n}", "@media (max-width: 720px) {\n    .a {\n        |\n    }\n}")]
    public void A_new_line_is_indented_in_stylesheets(string text, string expected) => Assert.Equal(expected, Type(text, '\n', component: false));

    [Fact]
    public void Tabs_indent_with_tabs() => Assert.Equal("@code {\n\tvoid Go()\n\t{\n\t\t|\n\t}\n}", Type("@code {\n\tvoid Go()\n\t{\n|}\n}", '\n', insertSpaces: false));

    [Theory]
    [InlineData("<Container>\n    <TextBlock />\n|")]
    [InlineData("<Container>\n    |\n</Container>")]
    [InlineData("<Container>\n    a|b\n</Container>")]
    [InlineData("@* {\n|\n*@")]
    [InlineData("<Button />\n|")]
    public void Other_new_lines_are_left_to_the_editor(string text) => Assert.Null(Type(text, '\n'));

    [Theory]
    [InlineData("@code {\n    var text = \"{\";\n|", "@code {\n    var text = \"{\";\n    |")]
    [InlineData("@code {\n    // {\n|", "@code {\n    // {\n    |")]
    [InlineData("@code {\n    var text = \"\"\"{\"\"\";\n|", "@code {\n    var text = \"\"\"{\"\"\";\n    |")]
    public void Braces_in_literals_and_comments_do_not_increase_indent(string text, string expected) => Assert.Equal(expected, Type(text, '\n'));
}
