using Nazor.Nss;

namespace Nazor.Style.Tests;

public sealed class NssApplicabilityTests
{
    [Theory]
    [InlineData("Button { object-fit: cover; }", 1)]
    [InlineData("Input { object-fit: cover; }", 1)]
    [InlineData("NumberField { stroke-width: 1px; }", 1)]
    [InlineData("Select { object-fit: cover; }", 1)]
    [InlineData("TextArea { font-size: 16px; }", 0)]
    [InlineData("Button { OBJECT-FIT: cover; }", 1)]
    [InlineData("Button { -windows-badge-border: 1px solid red; }", 1)]
    [InlineData("Custom { -windows-title-bar-height: tall; }", 1)]
    [InlineData(":root { -windows-badge-border: 1px solid red; }", 0)]
    [InlineData(".variable { object-fit: cover; }", 0)]
    [InlineData("Button, Image { object-fit: cover; }", 0)]
    [InlineData("Button, .variable { object-fit: cover; }", 0)]
    [InlineData("@media (min-width: 1px) { Button { &:hover { object-fit: cover; } } }", 1)]
    [InlineData("@keyframes sizing { from { stroke-width: 1px; } to { stroke-width: 2px; } }", 0)]
    public void Compiler_warns_only_for_known_inapplicable_subjects_and_preserves_the_rule(string text, int count)
    {
        var result = NssCompiler.Compile("test", text);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var warnings = result.Diagnostics.Where(d => d.Id == "NZS112").ToArray();
        Assert.Equal(count, warnings.Length);
        Assert.All(warnings, d => Assert.False(d.IsError));
        Assert.All(result.StyleSheet.Rules, r => Assert.NotEmpty(r.Declarations));
    }

    [Fact]
    public void Warnings_preserve_source_offsets_and_underline_the_property_name()
    {
        var result = NssCompiler.Compile("test", [new NssSource("Page.nazor", "\nButton { object-fit: cover; }", 8, 12)]);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(("NZS112", "Page.nazor", 9, 10, 10, false),
            (warning.Id, warning.Path, warning.Line, warning.Column, warning.Length, warning.IsError));
    }
}
