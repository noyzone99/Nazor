using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class OverlayExitDurationTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("180ms", 180)]
    [InlineData("1.5s", 1500)]
    [InlineData("4294967294ms", 4294967294)]
    public void Durations_compile_round_trip_and_emit_as_times(string value, double milliseconds)
    {
        var compiled = NssCompiler.Compile("exit-duration", $"Surface {{ overlay-exit-duration: {value}; }}");
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        var declaration = Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(compiled.StyleSheet.Rules).Declarations));
        Assert.Same(StyleProperties.OverlayExitDuration, declaration.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), declaration.Value);
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(compiled.StyleSheet));
        Assert.Equal(compiled.StyleSheet.Describe(), restored.Describe());
        Assert.Contains("OverlayExitDuration", CSharpEmitter.Emit(restored));
    }

    [Theory]
    [InlineData("-1ms")]
    [InlineData("4294967295ms")]
    [InlineData("5000000s")]
    [InlineData("1e300s")]
    [InlineData("1e999ms")]
    [InlineData("2px")]
    public void Invalid_or_unsupported_intervals_report_diagnostics_instead_of_overflowing(string value)
    {
        var compiled = NssCompiler.Compile("invalid-exit-duration", $"Surface {{ overlay-exit-duration: {value}; }}");
        Assert.False(compiled.Success);
        Assert.NotEmpty(compiled.Diagnostics);
    }
}
