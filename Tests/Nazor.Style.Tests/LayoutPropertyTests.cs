using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Style.Tests;

public sealed class LayoutPropertyTests
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

    private static string Error(string declarations)
    {
        var diagnostics = NssCompiler.Compile("test", $".a {{ {declarations} }}", "test.nss").Diagnostics;
        return Assert.Single(diagnostics).Message;
    }

    [Theory]
    [InlineData("flex-wrap: wrap", "flex-wrap: wrap")]
    [InlineData("flex-wrap: wrap-reverse", "flex-wrap: wrap-reverse")]
    [InlineData("align-content: space-between", "align-content: space-between")]
    [InlineData("align-content: start", "align-content: flex-start")]
    [InlineData("order: -2", "order: -2")]
    [InlineData("flex-flow: column wrap", "flex-direction: column; flex-wrap: wrap")]
    [InlineData("flex-flow: wrap-reverse", "flex-direction: row; flex-wrap: wrap-reverse")]
    [InlineData("flex-flow: row-reverse", "flex-direction: row-reverse; flex-wrap: nowrap")]
    public void Flex_lines_and_order_compile(string declaration, string expected) =>
        Assert.Equal(expected, Describe(declaration));

    [Theory]
    [InlineData("order: 1.5", "Expected an integer.")]
    [InlineData("order: auto", "Expected an integer.")]
    [InlineData("flex-flow: wrap nowrap", "Unexpected 'nowrap' in flex-flow.")]
    [InlineData("flex-wrap: reverse", "Expected one of")]
    public void Invalid_values_are_reported(string declaration, string message) =>
        Assert.Contains(message, Error(declaration));

    [Theory]
    [InlineData("position: absolute", "position: absolute")]
    [InlineData("top: -4px; left: 10%", "top: -4px; left: 10%")]
    [InlineData("inset: 0", "top: 0px; right: 0px; bottom: 0px; left: 0px")]
    [InlineData("inset: 1px auto", "top: 1px; right: auto; bottom: 1px; left: auto")]
    [InlineData("inset: 1px 2px 3px 4px", "top: 1px; right: 2px; bottom: 3px; left: 4px")]
    [InlineData("z-index: 10", "z-index: 10")]
    [InlineData("z-index: -1", "z-index: -1")]
    [InlineData("z-index: auto", "z-index: auto")]
    public void Positioning_compiles(string declaration, string expected) =>
        Assert.Equal(expected, Describe(declaration));

    [Theory]
    [InlineData("position: fixed", "Expected one of: static, relative, absolute.")]
    [InlineData("z-index: 1.5", "Expected an integer.")]
    [InlineData("top: 10", "A length needs a unit")]
    [InlineData("inset: 1px 2px 3px 4px 5px", "Unexpected '5px'")]
    public void Invalid_positioning_is_reported(string declaration, string message) =>
        Assert.Contains(message, Error(declaration));

    [Theory]
    [InlineData("display: grid", "display: grid")]
    [InlineData("grid-template-columns: 100px 1fr auto", "grid-template-columns: 100px 1fr auto")]
    [InlineData("grid-template-columns: repeat(3, 1fr 2fr)", "grid-template-columns: 1fr 2fr 1fr 2fr 1fr 2fr")]
    [InlineData("grid-template-columns: 50px repeat(auto-fill, minmax(100px, 1fr)) 20%", "grid-template-columns: 50px repeat(auto-fill, minmax(100px, 1fr)) 20%")]
    [InlineData("grid-template-rows: min-content max-content fit-content(120px)", "grid-template-rows: min-content max-content fit-content(120px)")]
    [InlineData("grid-template-rows: minmax(auto, 2fr) none", null)]
    [InlineData("grid-template-areas: \"a a\" \"b .\"", "grid-template-areas: \"a a\" \"b .\"")]
    [InlineData("grid-template-areas: \"  head   head \" \"... side\"", "grid-template-areas: \"head head\" \"... side\"")]
    [InlineData("grid-auto-rows: 40px auto", "grid-auto-rows: 40px, auto")]
    [InlineData("grid-auto-flow: dense column", "grid-auto-flow: column dense")]
    [InlineData("grid-auto-flow: dense", "grid-auto-flow: dense")]
    [InlineData("grid-column: 2 / -1", "grid-column-start: 2; grid-column-end: -1")]
    [InlineData("grid-row: span 3", "grid-row-start: span 3; grid-row-end: auto")]
    [InlineData("grid-column: main", "grid-column-start: main; grid-column-end: main")]
    [InlineData("grid-area: header", "grid-row-start: header; grid-column-start: header; grid-row-end: header; grid-column-end: header")]
    [InlineData("grid-area: 1 / 2 / 3", "grid-row-start: 1; grid-column-start: 2; grid-row-end: 3; grid-column-end: auto")]
    [InlineData("justify-items: center; justify-self: end", "justify-items: center; justify-self: flex-end")]
    [InlineData("place-items: center start", "align-items: center; justify-items: flex-start")]
    [InlineData("place-self: end", "align-self: flex-end; justify-self: flex-end")]
    [InlineData("place-content: space-between center", "align-content: space-between; justify-content: center")]
    [InlineData("justify-content: stretch", "justify-content: stretch")]
    public void Grid_compiles(string declaration, string? expected)
    {
        if (expected is null) Assert.NotEmpty(NssCompiler.Compile("test", $".a {{ {declaration} }}", "test.nss").Diagnostics);
        else Assert.Equal(expected, Describe(declaration));
    }

    [Theory]
    [InlineData(GridAutoFlow.Row, "row")]
    [InlineData(GridAutoFlow.Column, "column")]
    [InlineData(GridAutoFlow.RowDense, "dense")]
    [InlineData(GridAutoFlow.ColumnDense, "column dense")]
    public void Every_grid_auto_flow_mode_formats_as_reparseable_NSS(GridAutoFlow flow, string expected)
    {
        var declaration = new ValueDeclaration(StyleProperties.GridAutoFlow, flow);
        Assert.Equal("grid-auto-flow: " + expected, declaration.ToString());
        var result = NssCompiler.Compile("test", ".a { " + declaration + " }", "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var parsed = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(result.StyleSheet.Rules).Declarations));
        Assert.Same(StyleProperties.GridAutoFlow, parsed.Property);
        Assert.Equal(flow, Assert.IsType<GridAutoFlow>(parsed.Value));
    }

    [Theory]
    [InlineData("grid-template-columns: [main] 1fr", "Line names ([name]) are not supported")]
    [InlineData("grid-template-columns: minmax(1fr, 100px)", "fr is not allowed as the minimum of minmax().")]
    [InlineData("grid-template-columns: repeat(auto-fill, 1fr)", "need fixed sizes")]
    [InlineData("grid-template-columns: auto repeat(auto-fit, 100px)", "every track needs a fixed size")]
    [InlineData("grid-template-columns: repeat(auto-fill, 10px) repeat(auto-fit, 10px)", "at most one repeat(auto-fill)")]
    [InlineData("grid-template-columns: 2pt", "Unit 'pt' is not supported: use px, em, rem, ch, ex, vw, vh, vmin, vmax or %.")]
    [InlineData("grid-template-areas: \"a b\" \"a\"", "the same number of cells")]
    [InlineData("grid-template-areas: \"a b a\"", "is not a rectangle")]
    [InlineData("grid-template-areas: \"a+ b\"", "is not a valid area name")]
    [InlineData("grid-column-start: 0", "There is no grid line 0")]
    [InlineData("grid-row: span main", "named lines are not supported")]
    [InlineData("grid-auto-flow: row column", "Unexpected 'column'")]
    public void Invalid_grids_are_reported(string declaration, string message) =>
        Assert.Contains(message, Error(declaration));

    [Theory]
    [InlineData("aspect-ratio: 16 / 9", "aspect-ratio: 1.7778")]
    [InlineData("aspect-ratio: 2", "aspect-ratio: 2")]
    [InlineData("aspect-ratio: auto 4/3", "aspect-ratio: 1.3333")]
    [InlineData("aspect-ratio: 1 auto", "aspect-ratio: 1")]
    [InlineData("width: min-content; min-width: max-content; max-width: fit-content", "width: min-content; min-width: max-content; max-width: fit-content")]
    [InlineData("height: max-content", "height: max-content")]
    [InlineData("flex-basis: fit-content", "flex-basis: fit-content")]
    public void Ratios_and_intrinsic_sizes_compile(string declaration, string expected) =>
        Assert.Equal(expected, Describe(declaration));

    [Theory]
    [InlineData("aspect-ratio: 0 / 1", "Both sides of a ratio must be positive.")]
    [InlineData("aspect-ratio: 16 9", "Unexpected '9'")]
    [InlineData("width: fit-content(100px)", "fit-content() is only for grid tracks")]
    [InlineData("min-height: min-content", "Expected a length")]
    public void Invalid_ratios_and_sizes_are_reported(string declaration, string message) =>
        Assert.Contains(message, Error(declaration));

    [Fact]
    public void A_misspelled_shorthand_is_suggested()
    {
        Assert.Contains("Did you mean 'flex-flow'?", Error("flex-flw: row"));
    }
}
