using System.Text.Json.Nodes;
using Nazor.LanguageServer.Features;

namespace Nazor.LanguageServer.Tests;

public sealed class OklabColorLanguageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Literal_mixes_have_one_full_span_preview_in_standalone_and_embedded_styles(bool component)
    {
        const string value = "color-mix(in oklab, red, blue)";
        var declarations = ".sample { background: " + value + "; }";
        var source = component ? "<TextBlock />\n@style {\n" + declarations + "\n}" : declarations;
        var color = Assert.Single(NssColors.Find(source, component));
        Assert.Equal(value, source[color.Start..color.End]);
        Assert.Equal("#8c53a2", color.Color.ToString());
    }

    [Fact]
    public void Nested_mixes_keep_one_result_preview_instead_of_unrelated_inner_swatches()
    {
        const string value = "color-mix(in oklab, color-mix(in oklab, red, blue), transparent)";
        var source = ".sample { color: " + value + "; }";
        var color = Assert.Single(NssColors.Find(source, component: false));
        Assert.Equal(value, source[color.Start..color.End]);
        Assert.Equal("#8c53a280", color.Color.ToString());
    }

    [Theory]
    [InlineData("color-mix(in oklab, Canvas, #fff)")]
    [InlineData("color-mix(in oklab, currentColor, #f00)")]
    [InlineData("color-mix(in oklab, var(--accent), #00f)")]
    public void Dynamic_mixes_are_not_advertised_as_fixed_colors(string value) =>
        Assert.Empty(NssColors.Find(".sample { color: " + value + "; }", component: false));

    [Fact]
    public async Task Protocol_color_values_and_ranges_use_the_shared_mix_parser()
    {
        await using var client = LspClient.InProcess();
        await client.InitializeAsync();
        const string value = "color-mix(in oklab, red, blue)";
        var colors = (await client.RequestAsync("textDocument/documentColor", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Path.Combine(Path.GetTempPath(), "OklabUnsaved.nss")) },
            ["text"] = ".sample { color: " + value + "; }",
        }))!.AsArray();
        var color = Assert.Single(colors)!;
        Assert.Equal(17, color["range"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal(17 + value.Length, color["range"]!["end"]!["character"]!.GetValue<int>());
        Assert.Equal(140 / 255.0, color["color"]!["red"]!.GetValue<double>(), 8);
        Assert.Equal(83 / 255.0, color["color"]!["green"]!.GetValue<double>(), 8);
        Assert.Equal(162 / 255.0, color["color"]!["blue"]!.GetValue<double>(), 8);
        Assert.Equal(1.0, color["color"]!["alpha"]!.GetValue<double>());
    }
}
