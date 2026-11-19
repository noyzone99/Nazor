using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class ClipPathTests
{
    [Theory]
    [InlineData("none", "none")]
    [InlineData("inset(2px)", "inset(2px 2px 2px 2px)")]
    [InlineData("inset(25% 0)", "inset(25% 0px 25% 0px)")]
    [InlineData("inset(1px 2px 3px)", "inset(1px 2px 3px 2px)")]
    [InlineData("inset(1px 2px 3px -4px)", "inset(1px 2px 3px -4px)")]
    [InlineData("inset(calc(50% - 2px) 0)", "inset(calc(50% - 2px) 0px calc(50% - 2px) 0px)")]
    [InlineData("inset(10px round 4px)", "inset(10px 10px 10px 10px round 4px 4px 4px 4px)")]
    [InlineData("inset(-60px round 60px)", "inset(-60px -60px -60px -60px round 60px 60px 60px 60px)")]
    [InlineData("inset(0 round 16px 4px 16px 4px)", "inset(0px 0px 0px 0px round 16px 4px 16px 4px)")]
    [InlineData("inset(5% 1px round 10% 2px 3px / 4px 5% 6px 7px)", "inset(5% 1px 5% 1px round 10% 2px 3px 2px / 4px 5% 6px 7px)")]
    public void Insets_compile_and_round_trip_without_losing_axes_or_negative_values(string value, string expected)
    {
        var result = NssCompiler.Compile("clip", $"Box {{ clip-path: {value}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(sheet.Rules).Declarations));
        Assert.Equal(expected, Assert.IsType<ClipPathValue>(declaration.Value).ToString());
    }

    [Theory]
    [InlineData("inset()")]
    [InlineData("inset(1px, 2px)")]
    [InlineData("inset(1px 2px 3px 4px 5px)")]
    [InlineData("inset(10px round)")]
    [InlineData("inset(10px round -4px)")]
    [InlineData("inset(10px round 4px /)")]
    [InlineData("inset(10px round / 4px)")]
    [InlineData("inset(10px round 1px 2px 3px 4px 5px)")]
    [InlineData("inset(10px round 1px / 2px 3px 4px 5px 6px)")]
    [InlineData("inset(10px round 1px) border-box")]
    [InlineData("inset(1)")]
    [InlineData("inset(1px")]
    [InlineData("circle(50%)")]
    [InlineData("url(mask.svg)")]
    public void Unsupported_shapes_and_malformed_insets_fail_at_compilation(string value) =>
        Assert.False(NssCompiler.Compile("clip", $"Box {{ clip-path: {value}; }}").Success);

    [Fact]
    public void Existing_constructor_deconstruction_and_legacy_JSON_keep_square_corners()
    {
        var expected = new ClipPathValue(Length.Px(1), Length.Px(2), Length.Px(3), Length.Px(4));
        var (top, right, bottom, left, none) = expected;
        Assert.Equal((Length.Px(1), Length.Px(2), Length.Px(3), Length.Px(4), false), (top, right, bottom, left, none));
        var sheet = NssCompiler.Compile("clip", "Box { clip-path: inset(1px 2px 3px 4px); }").StyleSheet;
        var json = StyleSheetJson.ToJson(sheet);
        var value = json["rules"]![0]!["declarations"]![0]!["value"]!.AsObject();
        foreach (var name in new[] { "topLeftRadius", "topRightRadius", "bottomRightRadius", "bottomLeftRadius" }) value.Remove(name);
        var restored = StyleSheetJson.FromJson(json);
        Assert.Equal(expected, Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(restored.Rules).Declarations)).Value);
    }

    [Fact]
    public void Generated_CSharp_keeps_all_elliptical_radius_axes()
    {
        var result = NssCompiler.Compile("clip", "Box { clip-path: inset(1px round 2px 3px 4px 5px / 6px 7px 8px 9px); }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var code = CSharpEmitter.Emit(result.StyleSheet);
        foreach (var name in new[] { "TopLeftRadius", "TopRightRadius", "BottomRightRadius", "BottomLeftRadius" }) Assert.Contains(name + " = new global::Nazor.Style.RadiusValue", code);
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        Assert.Equal(code, CSharpEmitter.Emit(restored));
    }
}
