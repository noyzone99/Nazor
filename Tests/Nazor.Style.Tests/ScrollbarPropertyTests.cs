using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class ScrollbarPropertyTests
{
    [Fact]
    public void Wheel_scrolling_selectors_round_trip_and_emit_the_state_flag()
    {
        var result = NssCompiler.Compile("scrollbar", "Viewer:wheel-scrolling { scrollbar-visibility: none; }");
        Assert.True(result.Success);
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
        Assert.Contains("ElementStates.WheelScrolling", CSharpEmitter.Emit(result.StyleSheet));
    }

    [Fact]
    public void Serialized_scrollbar_times_cannot_be_negative()
    {
        var result = NssCompiler.Compile("scrollbar", "Viewer { scrollbar-wheel-timeout: 100ms; }");
        var json = StyleSheetJson.ToJson(result.StyleSheet);
        json["rules"]![0]!["declarations"]![0]!["value"]!["ticks"] = -1;
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.FromJson(json));
    }

    [Theory]
    [InlineData("scrollbar-visibility: none")]
    [InlineData("scrollbar-visibility: visible")]
    [InlineData("scrollbar-visibility: autohide")]
    [InlineData("scrollbar-width: 8px; scrollbar-inset: 0; scrollbar-radius: 4px; scrollbar-min-thumb-length: 30px")]
    [InlineData("scrollbar-thumb-color: currentColor; scrollbar-track-color: Canvas")]
    [InlineData("scrollbar-thumb-width: 0.25em; scrollbar-thumb-expanded-width: auto; scrollbar-thumb-border-width: 1px; scrollbar-thumb-border-color: currentColor")]
    [InlineData("scrollbar-thumb-width: auto; scrollbar-thumb-expanded-width: 8px; scrollbar-thumb-hover-color: auto; scrollbar-thumb-active-color: red; scrollbar-track-hover-color: Canvas")]
    [InlineData("scrollbar-thumb-border-highlight-color: white")]
    [InlineData("scrollbar-width: 0.5em; scrollbar-radius: calc(2px + 1px)")]
    [InlineData("scrollbar-fade-in-duration: 120ms; scrollbar-fade-out-duration: .2s; scrollbar-fade-out-delay: 0; scrollbar-wheel-timeout: 600ms")]
    [InlineData("scrollbar-fade-timing-function: cubic-bezier(.2, 0, .8, 1); scrollbar-reveal-distance: 24px; scrollbar-opacity: .5")]
    [InlineData("scrollbar-placement: reserve; scrollbar-buttons: visible; scrollbar-button-length: 16px; scrollbar-button-step: 2em")]
    [InlineData("scrollbar-track-radius: auto; scrollbar-track-border-width: 1px; scrollbar-track-border-color: red; scrollbar-button-radius: 2px")]
    [InlineData("scrollbar-track-radius: 4px; scrollbar-button-background: blue; scrollbar-button-hover-background: red; scrollbar-button-active-background: green; scrollbar-button-disabled-color: gray; scrollbar-button-color: white")]
    [InlineData("scrollbar-button-repeat-delay: .4s; scrollbar-button-repeat-interval: 50ms")]
    public void Scrollbar_values_compile_serialize_and_emit(string declaration)
    {
        var result = NssCompiler.Compile("scrollbar", "Viewer { " + declaration + "; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
        Assert.Contains("StyleProperties.Scrollbar", CSharpEmitter.Emit(result.StyleSheet));
    }

    [Theory]
    [InlineData("scrollbar-visibility: hidden")]
    [InlineData("scrollbar-width: -2px")]
    [InlineData("scrollbar-thumb-width: 20%")]
    [InlineData("scrollbar-thumb-expanded-width: -3px")]
    [InlineData("scrollbar-thumb-active-color: 3px")]
    [InlineData("scrollbar-thumb-border-width: -1px")]
    [InlineData("scrollbar-inset: -1px")]
    [InlineData("scrollbar-radius: 20%")]
    [InlineData("scrollbar-min-thumb-length: auto")]
    [InlineData("scrollbar-track-color: invalid")]
    [InlineData("scrollbar-fade-in-duration: -1ms")]
    [InlineData("scrollbar-fade-out-duration: 100px")]
    [InlineData("scrollbar-fade-out-delay: -1s")]
    [InlineData("scrollbar-wheel-timeout: infinite")]
    [InlineData("scrollbar-fade-timing-function: cubic-bezier(-1, 0, 1, 1)")]
    [InlineData("scrollbar-reveal-distance: -1px")]
    [InlineData("scrollbar-placement: beside")]
    [InlineData("scrollbar-buttons: auto")]
    [InlineData("scrollbar-button-length: -1px")]
    [InlineData("scrollbar-button-step: -1px")]
    [InlineData("scrollbar-button-repeat-interval: -1ms")]
    [InlineData("scrollbar-track-border-width: 10%")]
    public void Invalid_scrollbar_values_are_diagnostics(string declaration) =>
        Assert.False(NssCompiler.Compile("scrollbar", "Viewer { " + declaration + "; }").Success);
}
