using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Style.Tests;

public sealed class DecorationPropertyTests
{
    private static string Describe(string declarations)
    {
        var result = NssCompiler.Compile("test", $".a {{ {declarations} }}", "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        var rule = sheet.Describe().ReplaceLineEndings("\n").Trim();
        return rule[(rule.IndexOf('{') + 2)..^2];
    }

    private static string Error(string declarations) =>
        Assert.Single(NssCompiler.Compile("test", $".a {{ {declarations} }}", "test.nss").Diagnostics).Message;

    [Theory]
    [InlineData("border-color: red blue", "border-top-color: #ff0000; border-right-color: #0000ff; border-bottom-color: #ff0000; border-left-color: #0000ff")]
    [InlineData("border-style: solid dashed dotted", "border-top-style: solid; border-right-style: dashed; border-bottom-style: dotted; border-left-style: dashed")]
    [InlineData("border-left: 2px dotted", "border-left-width: 2px; border-left-style: dotted; border-left-color: currentColor")]
    [InlineData("border-top-color: #123456", "border-top-color: #123456")]
    [InlineData("border-radius: 4px", "border-top-left-radius: 4px; border-top-right-radius: 4px; border-bottom-right-radius: 4px; border-bottom-left-radius: 4px")]
    [InlineData("border-radius: 1px 2px 3px", "border-top-left-radius: 1px; border-top-right-radius: 2px; border-bottom-right-radius: 3px; border-bottom-left-radius: 2px")]
    [InlineData("border-radius: 10% / 5px 6px", "border-top-left-radius: 10% 5px; border-top-right-radius: 10% 6px; border-bottom-right-radius: 10% 5px; border-bottom-left-radius: 10% 6px")]
    [InlineData("border-top-left-radius: 8px 4px", "border-top-left-radius: 8px 4px")]
    [InlineData("outline: 2px solid #0078d4", "outline-width: 2px; outline-style: solid; outline-color: #0078d4")]
    [InlineData("outline: dashed", "outline-width: 3px; outline-style: dashed; outline-color: currentColor")]
    [InlineData("outline-offset: -2px", "outline-offset: -2px")]
    [InlineData("box-shadow: 0 2px 4px rgba(0, 0, 0, 0.25)", "box-shadow: 0px 2px 4px 0px #00000040")]
    [InlineData("box-shadow: inset #ff0000 1px -1px", "box-shadow: inset 1px -1px 0px 0px #ff0000")]
    [InlineData("box-shadow: 0 0 0 2px blue, 0 8px 16px -4px black", "box-shadow: 0px 0px 0px 2px #0000ff, 0px 8px 16px -4px #000000")]
    [InlineData("box-shadow: 1px 1px", "box-shadow: 1px 1px 0px 0px currentColor")]
    [InlineData("box-shadow: none", "box-shadow: none")]
    [InlineData("background-image: linear-gradient(red, blue)", "background-image: linear-gradient(180deg, #ff0000, #0000ff)")]
    [InlineData("background-image: linear-gradient(0.25turn, red 10%, blue 20px 40px)", "background-image: linear-gradient(90deg, #ff0000 10%, #0000ff 20px, #0000ff 40px)")]
    [InlineData("background-image: linear-gradient(to left top, red, blue)", "background-image: linear-gradient(to top left, #ff0000, #0000ff)")]
    [InlineData("background-image: radial-gradient(red, blue), linear-gradient(red, blue)", "background-image: radial-gradient(ellipse farthest-corner at 50% 50%, #ff0000, #0000ff), linear-gradient(180deg, #ff0000, #0000ff)")]
    [InlineData("background-image: radial-gradient(circle 20px at top left, red, blue)", "background-image: radial-gradient(circle 20px at 0% 0%, #ff0000, #0000ff)")]
    [InlineData("background-image: radial-gradient(closest-side ellipse at 10px 30%, red, blue)", "background-image: radial-gradient(ellipse closest-side at 10px 30%, #ff0000, #0000ff)")]
    [InlineData("background-image: radial-gradient(10% 20px at center bottom, red, blue)", "background-image: radial-gradient(ellipse 10% 20px at 50% 100%, #ff0000, #0000ff)")]
    [InlineData("background: linear-gradient(red, blue) #fff", "background-color: #ffffff; background-image: linear-gradient(180deg, #ff0000, #0000ff); background-position: 0% 0%; background-size: auto auto; background-repeat: Repeat")]
    [InlineData("background: #fff", "background-color: #ffffff; background-image: none; background-position: 0% 0%; background-size: auto auto; background-repeat: Repeat")]
    [InlineData("background: none", "background-color: #00000000; background-image: none; background-position: 0% 0%; background-size: auto auto; background-repeat: Repeat")]
    [InlineData("letter-spacing: -0.5px", "letter-spacing: -0.5px")]
    [InlineData("letter-spacing: normal", "letter-spacing: 0px")]
    [InlineData("text-transform: capitalize", "text-transform: capitalize")]
    [InlineData("white-space: pre-wrap", "white-space: pre-wrap")]
    [InlineData("white-space: pre-line", "white-space: pre-line")]
    [InlineData("overflow-wrap: break-word", "overflow-wrap: break-word")]
    [InlineData("text-decoration: underline", "text-decoration-line: underline; text-decoration-style: solid; text-decoration-color: currentColor; text-decoration-thickness: auto")]
    [InlineData("text-decoration: #f00 dotted line-through overline 3px", "text-decoration-line: overline line-through; text-decoration-style: dotted; text-decoration-color: #ff0000; text-decoration-thickness: 3px")]
    [InlineData("text-decoration: none", "text-decoration-line: none; text-decoration-style: solid; text-decoration-color: currentColor; text-decoration-thickness: auto")]
    [InlineData("text-decoration-line: underline line-through", "text-decoration-line: underline line-through")]
    [InlineData("text-overflow: ellipsis", "text-overflow: ellipsis")]
    [InlineData("pointer-events: none", "pointer-events: none")]
    [InlineData("user-select: none", "user-select: none")]
    public void Borders_radii_and_outlines_compile(string declaration, string expected) =>
        Assert.Equal(expected, Describe(declaration));

    [Theory]
    [InlineData(TextDecorationLine.None, "none")]
    [InlineData(TextDecorationLine.Underline, "underline")]
    [InlineData(TextDecorationLine.Overline, "overline")]
    [InlineData(TextDecorationLine.LineThrough, "line-through")]
    [InlineData(TextDecorationLine.Underline | TextDecorationLine.Overline, "underline overline")]
    [InlineData(TextDecorationLine.Underline | TextDecorationLine.LineThrough, "underline line-through")]
    [InlineData(TextDecorationLine.Overline | TextDecorationLine.LineThrough, "overline line-through")]
    [InlineData(TextDecorationLine.Underline | TextDecorationLine.Overline | TextDecorationLine.LineThrough, "underline overline line-through")]
    public void Every_decoration_line_combination_formats_as_reparseable_NSS(TextDecorationLine lines, string expected)
    {
        var declaration = new ValueDeclaration(StyleProperties.TextDecorationLine, lines);
        Assert.Equal("text-decoration-line: " + expected, declaration.ToString());
        var result = NssCompiler.Compile("test", ".a { " + declaration + " }", "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var parsed = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        Assert.Same(StyleProperties.TextDecorationLine, parsed.Property);
        Assert.Equal(lines, Assert.IsType<TextDecorationLine>(parsed.Value));
    }

    [Theory]
    [InlineData("border-style: double", "Expected one of")]
    [InlineData("border-radius: 1px 2px 3px 4px 5px", "Unexpected '5px'")]
    [InlineData("border-radius: 4px /", "Expected a vertical radius")]
    [InlineData("border-radius: -1px", "Negative values are not allowed")]
    [InlineData("border-top: 1px 2px", "Duplicate border width.")]
    [InlineData("outline: solid dashed", "Duplicate outline style.")]
    [InlineData("outline-offset: 10%", "Expected a length (px).")]
    [InlineData("box-shadow: 1px", "at least the x and y offsets")]
    [InlineData("box-shadow: 1px 1px -2px", "The blur radius cannot be negative.")]
    [InlineData("box-shadow: 1px 1px 1px 1px 1px", "at most four lengths")]
    [InlineData("box-shadow: inset inset 1px 1px", "Duplicate inset.")]
    [InlineData("background-image: linear-gradient(red)", "needs at least two color stops")]
    [InlineData("background-image: linear-gradient(red, 50%, blue)", "Color hints")]
    [InlineData("background-image: linear-gradient(to middle, red, blue)", "expected top, bottom, left or right")]
    [InlineData("background-image: linear-gradient(10px, red, blue)", "Expected an angle")]
    [InlineData("background-image: conic-gradient(red 10px, blue)", "Expected an angle")]
    [InlineData("background-image: url()", "Expected the URL of the image")]
    [InlineData("background-image: radial-gradient(circle 10%, red, blue)", "A circle has one radius, a length.")]
    [InlineData("background-image: radial-gradient(ellipse 10px, red, blue)", "An ellipse has two radii.")]
    [InlineData("background: red, blue", "Only the last background layer has a color.")]
    [InlineData("letter-spacing: 10%", "Expected a length (px).")]
    [InlineData("text-transform: full-width", "Expected one of")]
    [InlineData("text-decoration-line: underline underline", "Duplicate 'underline'.")]
    [InlineData("text-decoration-line: blink", "Expected underline, overline or line-through.")]
    [InlineData("text-overflow: fade", "Expected one of")]
    public void Invalid_decorations_are_reported(string declaration, string message) =>
        Assert.Contains(message, Error(declaration));
}
