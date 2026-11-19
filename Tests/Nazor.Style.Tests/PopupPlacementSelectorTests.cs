using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class PopupPlacementSelectorTests
{
    [Theory]
    [InlineData("popup-above", "PopupAbove")]
    [InlineData("popup-left", "PopupLeft")]
    [InlineData("popup-right", "PopupRight")]
    public void Resolved_popup_side_selectors_round_trip_and_emit_the_presentation_state(string selector, string state)
    {
        var result = NssCompiler.Compile("popup-side", $"SelectPopup:{selector} {{ --opening-top: 100%; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
        Assert.Contains("ElementStates." + state, CSharpEmitter.Emit(result.StyleSheet));
    }
}
