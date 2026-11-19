using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class DensityMediaTests
{
    [Theory]
    [InlineData(ControlDensity.Compact, true, false)]
    [InlineData(ControlDensity.Touch, false, true)]
    [InlineData(ControlDensity.Automatic, true, false)]
    public void Density_media_matches_resolved_choices_and_headless_automatic_falls_back_to_compact(ControlDensity density, bool compact, bool touch)
    {
        var result = NssCompiler.Compile("density", "@media (density: compact) { Top { width: 1px; } } @media (density: touch) { Top { width: 2px; } }", "density.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var preferences = new MediaPreferences(ColorScheme.Light, false, Density: density);
        Assert.Equal(compact, result.StyleSheet.Rules[0].Media!.Matches(new Viewport(800, 600), preferences));
        Assert.Equal(touch, result.StyleSheet.Rules[1].Media!.Matches(new Viewport(800, 600), preferences));
    }

    [Fact]
    public void Nested_density_conditions_contradict_and_round_trip_through_hot_reload_and_CSharp_emission()
    {
        var result = NssCompiler.Compile("density", "@media (density: touch) and (min-width: 300px) { Top { width: 2px; } @media (density: compact) { Top { height: 3px; } } }", "density.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet));
        Assert.Equal(result.StyleSheet.Describe(), sheet.Describe());
        Assert.Equal(ControlDensity.Touch, Assert.Single(sheet.Rules[0].Media!.Conditions).Density);
        Assert.False(sheet.Rules[1].Media!.Matches(new Viewport(800, 600), new MediaPreferences(ColorScheme.Light, false, Density: ControlDensity.Touch)));
        Assert.Contains("global::Nazor.ControlDensity.Touch", CSharpEmitter.Emit(sheet));
        Assert.Contains("(density: touch)", sheet.Rules[0].Media!.ToString());
    }

    [Fact]
    public void Automatic_is_an_application_choice_and_not_an_unresolved_media_condition_keyword()
    {
        var result = NssCompiler.Compile("density", "@media (density: automatic) { Top { width: 2px; } }", "density.nss");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains("compact or touch", StringComparison.Ordinal));
    }
}
