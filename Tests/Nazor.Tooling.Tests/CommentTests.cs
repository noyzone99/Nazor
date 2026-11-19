using Nazor.VisualStudio;

namespace Nazor.Tooling.Tests;

public sealed class CommentTests
{
    private static string Run(string text, CommentCommand command, bool component = true)
    {
        int start, end;
        if (text.Contains('|'))
        {
            start = end = text.IndexOf('|');
            text = text.Remove(start, 1);
        }
        else
        {
            start = text.IndexOf('[');
            text = text.Remove(start, 1);
            end = text.IndexOf(']');
            text = text.Remove(end, 1);
        }
        foreach (var change in NazorComments.Edit(text, start, end, component, command).OrderByDescending(c => c.Start))
            text = text[..change.Start] + change.Text + text[(change.Start + change.Length)..];
        return text;
    }

    [Theory]
    [InlineData("<Container>\n    <TextBlock>A|</TextBlock>\n</Container>", "<Container>\n    @* <TextBlock>A</TextBlock> *@\n</Container>")]
    [InlineData("<Container>\n[    <TextBlock>A</TextBlock>\n    <Button />\n]</Container>", "<Container>\n    @* <TextBlock>A</TextBlock>\n    <Button /> *@\n</Container>")]
    [InlineData("<TextBlock>[Count] @count</TextBlock>", "<TextBlock>@* Count *@ @count</TextBlock>")]
    [InlineData("@code {\n    private int count;|\n}", "@code {\n    //private int count;\n}")]
    [InlineData("@code {\n[    private int count;\n\n        private void Go() { }\n]}", "@code {\n    //private int count;\n\n    //    private void Go() { }\n}")]
    [InlineData("@style {\n    .a { color: red; }|\n}", "@style {\n    /* .a { color: red; } */\n}")]
    [InlineData("<TextBlock />\r\n@code {\r\n    int a;|\r\n}\r\n", "<TextBlock />\r\n@code {\r\n    //int a;\r\n}\r\n")]
    public void Comment_selection(string text, string expected) => Assert.Equal(expected, Run(text, CommentCommand.Comment));

    [Theory]
    [InlineData("<Container>\n    @* <TextBlock>A</TextBlock> *@|\n</Container>", "<Container>\n    <TextBlock>A</TextBlock>\n</Container>")]
    [InlineData("@* <TextBlock>[A]</TextBlock> *@", "<TextBlock>A</TextBlock>")]
    [InlineData("@code {\n[    //private int count;\n    //    private void Go() { }\n]}", "@code {\n    private int count;\n        private void Go() { }\n}")]
    [InlineData("@code {\n    /* int a; |*/\n}", "@code {\n    int a;\n}")]
    [InlineData("@style {\n    /* .a { color: red; } */|\n}", "@style {\n    .a { color: red; }\n}")]
    [InlineData("<TextBlock>A|</TextBlock>", "<TextBlock>A</TextBlock>")]
    public void Uncomment_selection(string text, string expected) => Assert.Equal(expected, Run(text, CommentCommand.Uncomment));

    [Theory]
    [InlineData("@code {\n    int a;|\n}", "@code {\n    //int a;\n}")]
    [InlineData("@code {\n    //int a;|\n}", "@code {\n    int a;\n}")]
    [InlineData("<TextBlock />|", "@* <TextBlock /> *@")]
    [InlineData("@* <TextBlock /> *@|", "<TextBlock />")]
    public void Toggle_line_comment(string text, string expected) => Assert.Equal(expected, Run(text, CommentCommand.ToggleLine));

    [Theory]
    [InlineData("@code {\n    int [a] = 1;\n}", "@code {\n    int /* a */ = 1;\n}")]
    [InlineData("@code {\n    int /* a */| = 1;\n}", "@code {\n    int a = 1;\n}")]
    public void Toggle_block_comment(string text, string expected) => Assert.Equal(expected, Run(text, CommentCommand.ToggleBlock));

    [Theory]
    [InlineData(".a {\n    color: red;|\n}", ".a {\n    /* color: red; */\n}")]
    [InlineData("[.a { }\n.b { }]", "/* .a { }\n.b { } */")]
    public void Stylesheets_use_css_comments(string text, string expected) => Assert.Equal(expected, Run(text, CommentCommand.Comment, component: false));

    [Fact]
    public void Razor_comments_do_not_nest() => Assert.Equal("@* <TextBlock /> *@", Run("@* <TextBlock /> *@|", CommentCommand.Comment));

    [Theory]
    [InlineData("<A />|", NazorRegion.Markup)]
    [InlineData("@code { int |a; }", NazorRegion.CSharp)]
    [InlineData("@functions { int |a; }", NazorRegion.CSharp)]
    [InlineData("@{ var |a = 1; }", NazorRegion.CSharp)]
    [InlineData("@style { .a { color: |red; } }", NazorRegion.Style)]
    [InlineData("@* <A |/> *@", NazorRegion.Comment)]
    [InlineData("@code { string s = \"}\"; }\n<A |/>", NazorRegion.Markup)]
    [InlineData("@code { string s = \"\"\"}\"\"\"; int |a; }", NazorRegion.CSharp)]
    [InlineData("@code { string s = @\"a\"\"}\"; int |a; }", NazorRegion.CSharp)]
    [InlineData("@code { string s = $\"{Format(\"}\")}\"; int |a; }", NazorRegion.CSharp)]
    [InlineData("@code { string s = \"\"\"}\"\"\"; }\n<A |/>", NazorRegion.Markup)]
    public void Regions_of_a_component(string text, object region)
    {
        var offset = text.IndexOf('|');
        Assert.Equal((NazorRegion)region, NazorRegions.At(text.Remove(offset, 1), offset));
    }
}
