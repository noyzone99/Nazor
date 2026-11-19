using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class ToolingRegressionTests : IAsyncLifetime
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-tooling-").FullName;
    private LspClient client = null!;

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        client = LspClient.InProcess();
        await client.InitializeAsync(new JsonObject { ["workspace"] = new JsonObject { ["workspaceEdit"] = new JsonObject { ["documentChanges"] = true } } });
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    private Task<JsonNode?> ActionsAsync(string path, int line = 0, int character = 0) => client.RequestAsync("textDocument/codeAction", new JsonObject
    {
        ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
        ["range"] = new JsonObject { ["start"] = new JsonObject { ["line"] = line, ["character"] = character }, ["end"] = new JsonObject { ["line"] = line, ["character"] = character } },
        ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() },
    });

    [Fact]
    public async Task Clients_without_documentChanges_receive_the_plain_changes_fallback()
    {
        await client.InitializeAsync(new JsonObject { ["workspace"] = new JsonObject() });
        var path = Path.Combine(directory, "Plain.nazor");
        await client.OpenAsync(path + ".nss", ".sample { color: red; }\n");
        await client.OpenAsync(path, "<Container />\n");
        var edit = Assert.Single((await ActionsAsync(path))!.AsArray())!["edit"]!;
        Assert.Null(edit["documentChanges"]);
        Assert.Equal("", edit["changes"]![LspClient.Uri(path + ".nss")]![0]!["newText"]!.GetValue<string>());
        Assert.Contains(".sample", edit["changes"]![LspClient.Uri(path)]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_client_without_file_operations_gets_a_versioned_clear_instead_of_an_ignored_delete()
    {
        var path = Path.Combine(directory, "Panel.nazor");
        File.WriteAllText(path + ".nss", ".disk { color: red; }\n");
        await client.OpenAsync(path + ".nss", ".unsaved { color: blue; }\n", 7);
        await client.OpenAsync(path, "<Container />\n");
        var action = Assert.Single((await ActionsAsync(path))!.AsArray())!;
        var changes = action["edit"]!["documentChanges"]!.AsArray();
        Assert.DoesNotContain(changes, c => c!["kind"] is not null);
        var sidecar = Assert.Single(changes, c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path + ".nss"))!;
        Assert.Equal(7, sidecar["textDocument"]!["version"]!.GetValue<int>());
        Assert.Equal("", sidecar["edits"]![0]!["newText"]!.GetValue<string>());
        var parent = Assert.Single(changes, c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!;
        Assert.Contains(".unsaved", parent["edits"]![0]!["newText"]!.GetValue<string>());
        Assert.DoesNotContain(".disk", parent["edits"]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_created_code_behind_is_analyzed_without_a_rebuild_or_opening_its_editor()
    {
        await client.InitializeAsync();
        var path = Path.Combine(directory, "Counter.nazor");
        await client.OpenAsync(path, "<TextBlock>@count</TextBlock>\n@code { private int count = 1; }\n");
        var action = (await ActionsAsync(path, 1, 1))!.AsArray().Single(a => a!["title"]!.GetValue<string>().Contains(" to Counter.nazor.cs", StringComparison.Ordinal))!;
        var changes = action["edit"]!["documentChanges"]!.AsArray();
        var parent = changes.Single(c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!;
        var sidecar = changes.Single(c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path + ".cs"))!;
        File.WriteAllText(path + ".cs", sidecar["edits"]![0]!["newText"]!.GetValue<string>());
        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, parent["edits"]![0]!["newText"]!.GetValue<string>(), 2);
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Fact]
    public async Task The_external_CSharp_buffer_is_used_for_the_inverse_transfer_and_can_be_released()
    {
        var path = Path.Combine(directory, "Panel.nazor");
        File.WriteAllText(path + ".cs", "namespace Demo; public partial class Panel { private int disk; }\n");
        await client.OpenAsync(path, "<Container />\n");
        await client.NotifyAsync("nazor/csharpDocuments", new JsonObject
        {
            ["projectDirectory"] = directory,
            ["documents"] = new JsonArray(new JsonObject { ["path"] = path + ".cs", ["text"] = "namespace Demo; public partial class Panel { private int unsaved; }\n" }),
        });
        var action = Assert.Single((await ActionsAsync(path))!.AsArray())!;
        var parent = action["edit"]!["documentChanges"]!.AsArray().Single(c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!;
        Assert.Contains("int unsaved", parent["edits"]![0]!["newText"]!.GetValue<string>());
        Assert.DoesNotContain("int disk", parent["edits"]![0]!["newText"]!.GetValue<string>());
        await client.NotifyAsync("nazor/csharpDocuments", new JsonObject { ["projectDirectory"] = directory, ["documents"] = new JsonArray() });
        action = Assert.Single((await ActionsAsync(path))!.AsArray())!;
        parent = action["edit"]!["documentChanges"]!.AsArray().Single(c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!;
        Assert.Contains("int disk", parent["edits"]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task Creating_a_sidecar_requires_the_clients_create_capability()
    {
        var path = Path.Combine(directory, "New.nazor");
        await client.OpenAsync(path, "<Container />\n@style { Container { color: red; } }\n");
        Assert.Empty((await ActionsAsync(path, 1, 1))!.AsArray());
    }

    [Theory]
    [InlineData(".cs", "@code { private int count; }")]
    [InlineData(".nss", "@style { Container { color: red; } }")]
    public async Task Visual_studio_creates_and_fills_the_destination_before_removing_the_source(string suffix, string block)
    {
        await client.InitializeAsVisualStudioAsync();
        var path = Path.Combine(directory, "Transfer.nazor");
        await client.OpenAsync(path, "<Container />\n" + block + "\n");
        var action = (await ActionsAsync(path, 1, 1))!.AsArray().Single(a => a!["title"]!.GetValue<string>().EndsWith("Transfer.nazor" + suffix, StringComparison.Ordinal))!;
        var edits = action["edit"]!["documentChanges"]!.AsArray();
        Assert.Equal("create", edits[0]!["kind"]!.GetValue<string>());
        Assert.True(edits[0]!["options"]!["ignoreIfExists"]!.GetValue<bool>());
        Assert.False(edits[0]!["options"]!["overwrite"]!.GetValue<bool>());
        Assert.Equal(LspClient.Uri(path + suffix), edits[1]!["textDocument"]!["uri"]!.GetValue<string>());
        Assert.Equal(LspClient.Uri(path), edits[2]!["textDocument"]!["uri"]!.GetValue<string>());
        Assert.DoesNotContain(edits.Take(1), e => e!["textDocument"] is not null);
    }

    [Theory]
    [InlineData(".cs", "namespace Demo; public partial class Transfer { private int count; }\n")]
    [InlineData(".nss", ".original { color: blue; }\n")]
    public async Task Visual_studio_inverse_deletes_empty_sidecars_with_a_command_after_its_text_edits(string suffix, string text)
    {
        await client.InitializeAsVisualStudioAsync();
        var path = Path.Combine(directory, "Transfer.nazor");
        File.WriteAllText(path + suffix, text);
        await client.OpenAsync(path, "<Container />\n");
        var action = Assert.Single((await ActionsAsync(path))!.AsArray())!;
        var edits = action["edit"]!["documentChanges"]!.AsArray();
        Assert.Equal(LspClient.Uri(path), edits[0]!["textDocument"]!["uri"]!.GetValue<string>());
        Assert.Equal("", edits[1]!["edits"]![0]!["newText"]!.GetValue<string>());
        Assert.DoesNotContain(edits, e => e!["kind"] is not null);
        Assert.Equal("nazor.deleteEmptySidecars", action["command"]!["command"]!.GetValue<string>());
        var arguments = action["command"]!["arguments"]![0]!;
        Assert.Equal(path, arguments["component"]!.GetValue<string>());
        var removed = Assert.Single(arguments["changes"]!.AsArray(), c => c!["delete"]!.GetValue<bool>())!;
        Assert.Equal(path + suffix, removed["path"]!.GetValue<string>());
        Assert.Equal("", removed["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_real_generated_component_moves_only_members_and_the_imports_they_need()
    {
        await client.InitializeAsync();
        var path = Path.Combine(directory, "Culture.nazor");
        const string source = "@using System.Globalization\n@using System.Text\n<TextBlock>@Value</TextBlock>\n\n@code {\n    private string Value => CultureInfo.InvariantCulture.Name;\n}\n";
        await client.OpenAsync(path, source);
        var action = Assert.Single((await ActionsAsync(path, 4, 1))!.AsArray(), a => a!["kind"]!.GetValue<string>() == "refactor.move")!;
        var edits = action["edit"]!["documentChanges"]!.AsArray();
        var sidecar = edits.Single(e => e!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path + ".cs"))!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.StartsWith("using System.Globalization;\n\nnamespace Demo;", sidecar);
        Assert.DoesNotContain("using System.Text", sidecar);
        Assert.DoesNotContain("#nullable", sidecar);
        Assert.DoesNotContain("#line", sidecar);
        var parent = edits.Single(e => e!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.StartsWith("@using System.Globalization\n@using System.Text\n", parent);
        File.WriteAllText(path + ".cs", sidecar);
        await client.ChangeAsync(path, parent, 2);
        Assert.Empty(await client.DiagnosticsAsync(path));
        var inverse = Assert.Single((await ActionsAsync(path))!.AsArray(), a => a!["kind"]!.GetValue<string>() == "refactor.move")!;
        var back = inverse["edit"]!["documentChanges"]!.AsArray().Single(e => e!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.Equal(source, back);
    }

    [Fact]
    public async Task Moving_code_preserves_extension_method_imports_without_duplicate_member_binding()
    {
        await client.InitializeAsync();
        var path = Path.Combine(directory, "SumPanel.nazor");
        const string source = "<TextBlock>@Total</TextBlock>\n@code {\n    private int[] values = [1, 2];\n    private int Total => values.Sum();\n}\n";
        await client.OpenAsync(path, source);
        var action = Assert.Single((await ActionsAsync(path, 1, 1))!.AsArray(), a => a!["kind"]!.GetValue<string>() == "refactor.move")!;
        var edits = action["edit"]!["documentChanges"]!.AsArray();
        var sidecar = edits.Single(e => e!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path + ".cs"))!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.Contains("using System.Linq;", sidecar);
        var parent = edits.Single(e => e!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path))!["edits"]![0]!["newText"]!.GetValue<string>();
        File.WriteAllText(path + ".cs", sidecar);
        await client.ChangeAsync(path, parent, 2);
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Fact]
    public async Task Class_completion_uses_current_associated_styles_and_replaces_only_the_current_word()
    {
        var path = Path.Combine(directory, "Panel.nazor");
        File.WriteAllText(path + ".nss", ".disk { color: red; }\n");
        File.WriteAllText(Path.Combine(directory, "Other.nazor.nss"), ".unrelated { color: red; }\n");
        await client.OpenAsync(path + ".nss", ".sidecar { } /* .comment { } */\n@media (min-width: 1px) { .nested { } }\n.a[Text='.fake'] { background-image: url('image.png'); }\n");
        await client.OpenAsync(path, "<Button Class=\"inline si\" />\n@style { .inline { } }\n@* @style { .commented { } } *@\n");
        var items = (await client.AtAsync("textDocument/completion", path, 0, 23))!["items"]!.AsArray();
        var names = items.Select(i => i!["label"]!.GetValue<string>()).ToArray();
        Assert.Contains("sidecar", names);
        Assert.Contains("nested", names);
        Assert.DoesNotContain("inline", names);
        Assert.DoesNotContain("unrelated", names);
        Assert.DoesNotContain("disk", names);
        Assert.DoesNotContain("fake", names);
        Assert.DoesNotContain("comment", names);
        Assert.DoesNotContain("commented", names);
        Assert.DoesNotContain("png", names);
        var sidecar = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "sidecar")!;
        Assert.Equal(22, sidecar["textEdit"]!["range"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal(24, sidecar["textEdit"]!["range"]!["end"]!["character"]!.GetValue<int>());
        await client.ChangeAsync(path + ".nss", ".changed { }", 2);
        items = (await client.AtAsync("textDocument/completion", path, 0, 23))!["items"]!.AsArray();
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "changed");
        Assert.DoesNotContain(items, i => i!["label"]!.GetValue<string>() == "sidecar");
    }
}
