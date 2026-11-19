using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class DeepSelectorTests
{
    [Theory]
    [InlineData(".parent ::deep .child")]
    [InlineData("::deep .child")]
    [InlineData(".parent::deep .child > Button:hover")]
    public void Deep_roundtrips_in_compiled_and_hot_reload_sheets(string selector)
    {
        var result = NssCompiler.Compile("deep", selector + " { padding: 4px; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.True(Assert.Single(result.StyleSheet.Rules).Selector.HasDeep);
        Assert.Contains("Combinator.Deep", CSharpEmitter.Emit(result.StyleSheet));
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
    }

    [Theory]
    [InlineData(".parent ::deep")]
    [InlineData(".parent ::deep > .child")]
    [InlineData(".parent > ::deep .child")]
    [InlineData(".parent ::deep ::deep .child")]
    [InlineData(".parent ::deep .child ::deep Button")]
    [InlineData(":is(.parent ::deep Button)")]
    [InlineData("::unknown Button")]
    public void Malformed_deep_selectors_are_diagnostics(string selector)
    {
        Assert.False(NssCompiler.Compile("deep", selector + " { padding: 4px; }").Success);
    }
}
