using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class PropertyMetadataTests
{
    [Fact]
    public void Metadata_is_published_once_to_concurrent_readers()
    {
        var property = new StyleProperty<string>(0, "sample", "auto", false, StyleEffects.None, false,
            "auto | <length-percentage>", _ => "auto");
        var shorthand = new ShorthandProperty("sample", [], "solid <color>", _ => []);
        var results = new (IReadOnlyList<string> Keywords, IReadOnlyList<string> Types,
            IReadOnlyList<string> ShorthandKeywords, IReadOnlyList<string> ShorthandTypes)[64];
        Parallel.For(0, results.Length, index =>
            results[index] = (property.Keywords, property.ValueTypes, shorthand.Keywords, shorthand.ValueTypes));
        Assert.All(results, result =>
        {
            Assert.Same(results[0].Keywords, result.Keywords);
            Assert.Same(results[0].Types, result.Types);
            Assert.Same(results[0].ShorthandKeywords, result.ShorthandKeywords);
            Assert.Same(results[0].ShorthandTypes, result.ShorthandTypes);
        });
        Assert.Equal(["auto"], property.Keywords);
        Assert.Equal(["length-percentage"], property.ValueTypes);
        Assert.Equal(["solid"], shorthand.Keywords);
        Assert.Equal(["color"], shorthand.ValueTypes);
    }

    private static bool Compiles(string declaration) => NssCompiler.Compile("test", $"TextBlock {{ {declaration} }}", "test.nss").Success;

    public static TheoryData<string, string> Keywords()
    {
        var data = new TheoryData<string, string>();
        foreach (var property in StyleProperties.All)
            foreach (var keyword in property.Keywords) data.Add(property.Name, keyword);
        foreach (var shorthand in StyleProperties.AllShorthands)
            foreach (var keyword in shorthand.Keywords) data.Add(shorthand.Name, keyword);
        return data;
    }

    [Theory]
    [MemberData(nameof(Keywords))]
    public void Every_keyword_of_a_syntax_compiles(string property, string keyword) =>
        Assert.True(Compiles($"{property}: {keyword};") || Compiles($"{property}: {keyword} 1;") || Compiles($"{property}: 1 {keyword};") || Compiles($"{property}: {keyword} 1px 1px;") ||
            property == "font-feature-settings" && Compiles($"{property}: 'kern' {keyword};"), $"{property}: {keyword}");

    [Fact]
    public void Every_property_has_a_syntax()
    {
        Assert.All(StyleProperties.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Syntax), p.Name));
        Assert.All(StyleProperties.AllShorthands, p => Assert.False(string.IsNullOrWhiteSpace(p.Syntax), p.Name));
    }

    [Fact]
    public void Keywords_and_value_types_come_from_the_syntax()
    {
        StyleProperties.TryGet("display", out var display);
        Assert.Equal(["block", "inline", "flex", "grid", "none"], display.Keywords);
        Assert.Empty(display.ValueTypes);
        StyleProperties.TryGet("width", out var width);
        Assert.Equal("auto | <length-percentage> | min-content | max-content | fit-content", width.Syntax);
        Assert.Equal(["auto", "min-content", "max-content", "fit-content"], width.Keywords);
        Assert.Equal(["length-percentage"], width.ValueTypes);
        StyleProperties.TryGetShorthand("border", out var border);
        Assert.Equal(["thin", "medium", "thick", "none", "solid", "dashed", "dotted"], border.Keywords);
        Assert.Equal(["length", "color"], border.ValueTypes);
        StyleProperties.TryGet("transition", out var transition);
        Assert.Equal(["none", "all"], transition.Keywords);
        Assert.Equal(["property", "time", "easing-function"], transition.ValueTypes);
    }

    [Fact]
    public void Color_names_are_the_colors_of_the_compiler()
    {
        Assert.Contains("rebeccapurple", StyleProperties.ColorNames);
        Assert.Contains("transparent", StyleProperties.ColorNames);
        Assert.Contains("currentcolor", StyleProperties.ColorNames);
        Assert.All(StyleProperties.ColorNames, name => Assert.True(Compiles($"color: {name};"), name));
        Assert.All(StyleProperties.GlobalKeywords, keyword => Assert.True(Compiles($"display: {keyword};"), keyword));
    }
}
