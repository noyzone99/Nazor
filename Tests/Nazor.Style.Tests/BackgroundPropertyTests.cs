using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class BackgroundPropertyTests
{
    [Theory]
    [InlineData("background-image: url(images/a.png)", "background-image: url(\"images/a.png\")")]
    [InlineData("background-image: url( \"a b.png\" ), linear-gradient(red, blue)", "background-image: url(\"a b.png\"), linear-gradient(180deg, #ff0000, #0000ff)")]
    [InlineData("background-image: url(https://example.org/a.png)", "background-image: url(\"https://example.org/a.png\")")]
    [InlineData("background-size: cover, 10px, 50% auto", "background-size: cover, 10px auto, 50% auto")]
    [InlineData("background-position: center, right 10px", "background-position: 50% 50%, 100% 10px")]
    [InlineData("background-repeat: no-repeat, repeat-x", "background-repeat: NoRepeat, RepeatX")]
    [InlineData("background: url(a.png) center / contain no-repeat, #fff",
        "background-color: #ffffff; background-image: url(\"a.png\"); background-position: 50% 50%; background-size: contain; background-repeat: NoRepeat")]
    [InlineData("background: repeat-y url(a.png) 10px 20px / 5px",
        "background-color: #00000000; background-image: url(\"a.png\"); background-position: 10px 20px; background-size: 5px auto; background-repeat: RepeatY")]
    public void Backgrounds_compile(string declaration, string expected)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(expected, string.Join("; ", result.StyleSheet.Rules.Single().Declarations));
    }

    [Theory]
    [InlineData("background: url(a.png) calc(100% - 2px) 50% / 4px 4px no-repeat #00f", "calc(100% - 2px) 50%", "4px 4px", "#0000ff")]
    [InlineData("background: url(a.png) 50% calc(100% - 2px) no-repeat", "50% calc(100% - 2px)", "auto auto", "#00000000")]
    [InlineData("background: url(a.png) calc(100% - 2px) calc(50% + 1px)", "calc(100% - 2px) calc(50% + 1px)", "auto auto", "#00000000")]
    [InlineData("background: url(a.png) left calc(50% + 1px) / 4px", "0% calc(50% + 1px)", "4px auto", "#00000000")]
    [InlineData("background: url(a.png) calc(100% - 2px) center red", "calc(100% - 2px) 50%", "auto auto", "#ff0000")]
    [InlineData("background: url(a.png) calc(100% - 2px) red", "calc(100% - 2px) 50%", "auto auto", "#ff0000")]
    [InlineData("background: url(a.png) min(10px, 5%) max(1px, 2%) / 4px", "min(10px, 5%) max(1px, 2%)", "4px auto", "#00000000")]
    [InlineData("background: url(a.png) clamp(0px, 50%, 10px) calc(min(1px, 2%) + 3px) repeat-x", "clamp(0px, 50%, 10px) calc(min(1px, 2%) + 3px)", "auto auto", "#00000000")]
    public void Math_functions_count_as_one_component_of_a_layer_position(string declaration, string position, string size, string color)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var declarations = result.StyleSheet.Rules.Single().Declarations.Select(d => d.ToString()).ToArray();
        Assert.Contains($"background-position: {position}", declarations);
        Assert.Contains($"background-size: {size}", declarations);
        Assert.Contains($"background-color: {color}", declarations);
    }

    [Fact]
    public void A_radial_gradient_layer_takes_a_calc_position()
    {
        var result = NssCompiler.Compile("x",
            "a { background: radial-gradient(circle closest-side, #fff 70%, transparent 100%) calc(100% - 2px) 50% / 4px 4px no-repeat #00f; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var declarations = result.StyleSheet.Rules.Single().Declarations.Select(d => d.ToString()).ToArray();
        Assert.Contains("background-position: calc(100% - 2px) 50%", declarations);
        Assert.Contains("background-color: #0000ff", declarations);
    }

    [Theory]
    [InlineData("background-position: calc(100% - 2px) 50%, min(1px, 2%) clamp(0px, 50%, 10px)",
        "background-position: calc(100% - 2px) 50%, min(1px, 2%) clamp(0px, 50%, 10px)")]
    [InlineData("background-position: right calc(50% - 1px)", "background-position: 100% calc(50% - 1px)")]
    public void The_background_position_longhand_takes_math_functions(string declaration, string expected)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(expected, result.StyleSheet.Rules.Single().Declarations.Single().ToString());
    }

    [Theory]
    [InlineData("Styles/Theme.nss", "../images/a.png", "images/a.png")]
    [InlineData("Components/Card.nazor", "icons/x.svg", "Components/icons/x.svg")]
    [InlineData("Components/Card.nazor", "./x.svg", "Components/x.svg")]
    [InlineData("t", "a.png", "a.png")]
    [InlineData("Styles/Theme.nss", "/images/a.png", "images/a.png")]
    [InlineData("Styles/Theme.nss", "https://example.org/a.png", "https://example.org/a.png")]
    [InlineData("Styles/Theme.nss", "file:///tmp/a.png", "file:///tmp/a.png")]
    [InlineData("Styles\\Theme.nss", "..\\images\\a.png", "images/a.png")]
    public void Relative_urls_are_resolved_against_the_folder_of_the_sheet(string sheet, string url, string expected) =>
        Assert.Equal(expected, NssCompiler.ResolveUrl(sheet, url));

    [Fact]
    public void The_compiled_sheet_has_project_relative_urls()
    {
        var sheet = NssCompiler.Compile("Styles/Theme.nss", ".x { background: url(../images/a.png) no-repeat; }").StyleSheet;
        Assert.Contains("url(\"images/a.png\")", sheet.Describe(), StringComparison.Ordinal);
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
    }

    [Theory]
    [InlineData("background-size: 10px 10px 10px", "Unexpected")]
    [InlineData("background-repeat: round", "Expected one of")]
    [InlineData("background: url(a.png) url(b.png)", "one image")]
    [InlineData("background-position: 10px left", "Invalid position")]
    [InlineData("background: url(a.png) calc(100% - 2px) left", "Invalid position")]
    [InlineData("background: url(a.png) 1px calc(2px + 1px) 3px", "found '3px'")]
    [InlineData("background: red, url(a.png)", "Only the last background layer has a color")]
    public void Invalid_backgrounds_are_reported(string declaration, string message)
    {
        var result = NssCompiler.Compile("t", $".x {{ {declaration}; }}", "t.nss");
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(message, StringComparison.Ordinal));
    }
}
