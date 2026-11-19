using System.Text.Json.Nodes;
using Nazor.LanguageServer.Features;

namespace Nazor.LanguageServer.Tests;

public sealed class NssColorTests
{
    private static List<(string Text, string Color)> Colors(string text, bool component = false) =>
        [.. NssColors.Find(text, component).Select(c => (text[c.Start..c.End], c.Color.ToString()))];

    [Fact]
    public void Dynamic_system_colors_and_palette_variables_have_no_fixed_color_picker()
    {
        var declarations = string.Join(" ", Nazor.Style.SystemColors.Names.Select(name => $"color: {name};"));
        Assert.Empty(Colors($".a {{ {declarations} background: var(--nz-system-canvas); }}"));
        Assert.Equal([("pink", "#ffc0cb")], Colors(".a { --nz-system-canvas: pink; background: Canvas; }"));
    }

    [Fact]
    public void Colors_are_found_in_values_with_the_parser_of_nss()
    {
        const string text = """
            .card {
                color: red;
                background: #0067c0;
                border-color: rgba(0, 0, 0, 0.5);
                --accent: hsl(120, 100%, 50%);
                outline-color: var(--accent, #fff);
                border: 1px solid currentColor;
                transition: background 150ms ease-out;
            }

            """;
        Assert.Equal(
            [("red", "#ff0000"), ("#0067c0", "#0067c0"), ("rgba(0, 0, 0, 0.5)", "#00000080"), ("hsl(120, 100%, 50%)", "#00ff00"), ("#fff", "#ffffff")],
            Colors(text));
    }

    [Fact]
    public void Selectors_and_names_are_not_colors()
    {
        const string text = """
            .red, Button:hover { color: blue; }
            Tan { &:checked { color: tan; } }
            /* red */
            .a { font-family: "red"; }

            """;
        Assert.Equal([("blue", "#0000ff"), ("tan", "#d2b48c")], Colors(text));
    }

    [Fact]
    public void Components_have_colors_only_in_style_blocks()
    {
        const string text = """
            <TextBlock Class="red">red</TextBlock>

            @style {
                .red { color: red; }
            }

            @code {
                private string color = "red";
            }

            """;
        var colors = NssColors.Find(text, component: true);
        var color = Assert.Single(colors);
        Assert.Equal(text.IndexOf("red; }", StringComparison.Ordinal), color.Start);
    }

    [Fact]
    public async Task Colors_and_their_presentations_through_the_protocol()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-colors-").FullName;
        try
        {
            await using var client = LspClient.InProcess();
            var capabilities = (await client.InitializeAsync())!["capabilities"]!;
            Assert.True(capabilities["colorProvider"]!.GetValue<bool>());
            var path = Path.Combine(directory, "Theme.nss");
            await client.OpenAsync(path, ".a { color: #ff000080; }");
            var colors = (await client.RequestAsync("textDocument/documentColor", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } }))!.AsArray();
            var color = Assert.Single(colors)!;
            Assert.Equal(12, color["range"]!["start"]!["character"]!.GetValue<int>());
            Assert.Equal(21, color["range"]!["end"]!["character"]!.GetValue<int>());
            Assert.Equal(1.0, color["color"]!["red"]!.GetValue<double>());
            Assert.Equal(128 / 255.0, color["color"]!["alpha"]!.GetValue<double>(), 3);

            var presentations = (await client.RequestAsync("textDocument/colorPresentation", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
                ["color"] = new JsonObject { ["red"] = 0.0, ["green"] = 0.4, ["blue"] = 0.75, ["alpha"] = 1.0 },
                ["range"] = color["range"]!.DeepClone(),
            }))!.AsArray();
            Assert.Equal(["#0066bf", "rgb(0, 102, 191)"], presentations.Select(p => p!["label"]!.GetValue<string>()));
            Assert.Equal("#0066bf", presentations[0]!["textEdit"]!["newText"]!.GetValue<string>());
        }
        finally
        {
            await LspClient.DeleteDirectoryAsync(directory);
        }
    }

    [Fact]
    public async Task Visual_studio_does_not_advertise_inline_colors_but_explicit_color_requests_remain_available()
    {
        await using var client = LspClient.InProcess();
        var capabilities = (await client.InitializeAsVisualStudioAsync())!["capabilities"]!;
        Assert.Null(capabilities["colorProvider"]);
        var colors = (await client.RequestAsync("textDocument/documentColor", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Path.Combine(Path.GetTempPath(), "Unsaved.nazor")) },
            ["text"] = "<TextBlock />\n@style {\n    .a { color: navy; }\n}\n",
        }))!.AsArray();
        Assert.Equal(2, Assert.Single(colors)!["range"]!["start"]!["line"]!.GetValue<int>());
    }
}
