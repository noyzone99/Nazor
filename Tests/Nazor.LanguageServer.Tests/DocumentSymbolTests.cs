using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class DocumentSymbolTests : IAsyncLifetime
{
    private const string Orders = """
        @using System.Collections.Generic

        <Container Class="orders">
            <Button OnClick="Add">Aggiungi</Button>
        </Container>

        @style {
            .orders {
                gap: 4px;

                Button:hover { opacity: 0.8; }
            }

            @media (prefers-color-scheme: dark) {
                .orders { background: black; }
            }
        }

        @code {
            [Parameter] public string Title { get; set; } = "";
            private readonly List<string> items = [];
            private int count, total;
            private void Add() => items.Add("x");
            private Task SaveAsync(string name, int retries) => Task.CompletedTask;

            private enum Mode { View, Edit }
        }

        """;

    private readonly string directory = Directory.CreateTempSubdirectory("nazor-symbols-").FullName;
    private LspClient client = null!;

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        client = LspClient.InProcess();
        var result = await client.InitializeAsync(Hierarchical());
        Assert.True(result!["capabilities"]!["documentSymbolProvider"]!.GetValue<bool>());
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    private static JsonObject Hierarchical() => new()
    {
        ["textDocument"] = new JsonObject { ["documentSymbol"] = new JsonObject { ["hierarchicalDocumentSymbolSupport"] = true } },
    };

    private static async Task<JsonArray> SymbolsAsync(LspClient client, string path) =>
        (await client.RequestAsync("textDocument/documentSymbol", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } }))!.AsArray();

    private static string Outline(JsonArray symbols, string indent = "")
    {
        var lines = new List<string>();
        foreach (var symbol in symbols)
        {
            lines.Add($"{indent}{symbol!["name"]!.GetValue<string>()} ({symbol["kind"]!.GetValue<int>()})");
            if (symbol["children"] is JsonArray children) lines.Add(Outline(children, indent + "  "));
        }
        return string.Join("\n", lines);
    }

    [Fact]
    public async Task A_component_is_its_class_with_the_members_of_its_code_and_its_style_rules()
    {
        var path = Path.Combine(directory, "Orders.nazor");
        var text = Orders.ReplaceLineEndings("\r\n");
        await client.OpenAsync(path, text);
        var symbols = await SymbolsAsync(client, path);
        Assert.Equal("""
            Orders (5)
              @style (3)
                .orders (5)
                  Button:hover (5)
                @media (prefers-color-scheme: dark) (2)
                  .orders (5)
              Title (7)
              items (8)
              count (8)
              total (8)
              Add() (6)
              SaveAsync(string, int) (6)
              Mode (10)
                View (22)
                Edit (22)
            """.ReplaceLineEndings("\n"), Outline(symbols));

        var add = symbols[0]!["children"]!.AsArray().Single(s => s!["name"]!.GetValue<string>() == "Add()")!;
        var line = text.Split("\r\n").ToList().FindIndex(l => l.Contains("void Add()", StringComparison.Ordinal));
        Assert.Equal(line, add["selectionRange"]!["start"]!["line"]!.GetValue<int>());
        Assert.Equal(text.Split("\r\n")[line].IndexOf("Add", StringComparison.Ordinal), add["selectionRange"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal(4, add["range"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal("void", add["detail"]!.GetValue<string>());
        var title = symbols[0]!["children"]!.AsArray().Single(s => s!["name"]!.GetValue<string>() == "Title")!;
        Assert.Equal("string", title["detail"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_style_sheet_is_its_rules()
    {
        var path = Path.Combine(directory, "Theme.nss");
        await client.OpenAsync(path, "/* a { } */\n.a { color: red; }\n\n@keyframes spin {\n    from { opacity: 0; }\n    to { opacity: 1; }\n}\n");
        Assert.Equal("""
            .a (5)
            @keyframes spin (2)
              from (5)
              to (5)
            """.ReplaceLineEndings("\n"), Outline(await SymbolsAsync(client, path)));
    }

    [Fact]
    public async Task Clients_without_hierarchies_get_the_container_by_name()
    {
        await using var flat = LspClient.InProcess();
        await flat.InitializeAsync();
        var path = Path.Combine(directory, "Orders.nazor");
        await flat.OpenAsync(path, Orders);
        var symbols = await SymbolsAsync(flat, path);
        Assert.All(symbols, s => Assert.Null(s!["children"]));
        Assert.Equal("Orders", symbols.Single(s => s!["name"]!.GetValue<string>() == "Add()")!["containerName"]!.GetValue<string>());
        Assert.Equal(".orders", symbols.Single(s => s!["name"]!.GetValue<string>() == "Button:hover")!["containerName"]!.GetValue<string>());
        Assert.Null(symbols.Single(s => s!["name"]!.GetValue<string>() == "Orders")!["containerName"]);
        Assert.NotNull(symbols[0]!["location"]!["range"]);
    }
}
