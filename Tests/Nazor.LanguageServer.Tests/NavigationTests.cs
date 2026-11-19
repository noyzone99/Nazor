using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class NavigationTests : IAsyncLifetime
{
    private const string Counter = """
        <Container Class="counter">
            <TextBlock>Count: @count</TextBlock>
            <Button @onclick="Increment" Disabled="@(count > 9)">Go</Button>
            <Button @onclick="Reset">Reset</Button>
            <Card Title="Total" />
        </Container>

        @code {
            private int count;

            private void Increment() => count++;
        }

        """;

    private const string CodeBehind = """
        namespace Demo;

        public partial class Counter
        {
            private void Reset() => count = 0;
        }

        """;

    private const string Card = """
        <TextBlock>@Title</TextBlock>

        @code {
            [Parameter] public string? Title { get; set; }
        }

        """;

    private readonly string directory = Directory.CreateTempSubdirectory("nazor-navigation-").FullName;
    private LspClient client = null!;
    private JsonNode capabilities = null!;

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(directory, "Counter.nazor.cs"), CodeBehind.ReplaceLineEndings("\r\n"));
        File.WriteAllText(Path.Combine(directory, "Card.nazor"), Card.ReplaceLineEndings("\r\n"));
        client = LspClient.InProcess();
        capabilities = (await client.InitializeAsVisualStudioAsync())!["capabilities"]!;
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    private async Task<(string Path, string Text)> OpenAsync(string text, string name = "Counter.nazor")
    {
        var path = Path.Combine(directory, name);
        text = text.ReplaceLineEndings("\r\n");
        await client.OpenAsync(path, text);
        return (path, text);
    }

    private static (int Line, int Character) At(string text, string marker, int delta = 0, int occurrence = 0)
    {
        var offset = -1;
        for (var i = 0; i <= occurrence; i++) offset = text.IndexOf(marker, offset + 1, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"'{marker}' not found");
        var position = Protocol.TextPositions.Position(text, offset + delta);
        return (position.Line, position.Character);
    }

    private static (int Line, int Character) Start(JsonNode? range) => (range!["start"]!["line"]!.GetValue<int>(), range["start"]!["character"]!.GetValue<int>());

    private Task<JsonNode?> RequestAtAsync(string method, string path, (int Line, int Character) at) => client.AtAsync(method, path, at.Line, at.Character);

    [Fact]
    public async Task End_tags_have_the_quick_info_of_their_element()
    {
        var (path, text) = await OpenAsync(Counter);
        var hover = await RequestAtAsync("textDocument/hover", path, At(text, "</Container>", 4));
        Assert.Contains("Nazor.Controls.Container", hover!["contents"]!["value"]!.GetValue<string>());
        Assert.Equal(At(text, "</Container>", 2), Start(hover["range"]));
        Assert.Equal(At(text, "</Container>", 11).Character, hover["range"]!["end"]!["character"]!.GetValue<int>());
    }

    [Fact]
    public async Task Go_to_definition_finds_members_code_behind_and_components()
    {
        var (path, text) = await OpenAsync(Counter);

        var increment = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "\"Increment", 3)))!.AsArray());
        Assert.Equal(LspClient.Uri(path), increment!["uri"]!.GetValue<string>(), ignoreCase: true);
        Assert.Equal(At(text, "Increment()"), Start(increment["range"]));

        var count = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "@count", 2)))!.AsArray());
        Assert.Equal(At(text, "count;"), Start(count!["range"]));

        var reset = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "\"Reset", 2)))!.AsArray());
        Assert.EndsWith("Counter.nazor.cs", reset!["uri"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(At(CodeBehind.ReplaceLineEndings("\r\n"), "Reset"), Start(reset["range"]));

        var card = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "<Card", 2)))!.AsArray());
        Assert.EndsWith("Card.nazor", card!["uri"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal((0, 0), Start(card["range"]));

        var title = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "Title=", 1)))!.AsArray());
        Assert.EndsWith("Card.nazor", title!["uri"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(At(Card.ReplaceLineEndings("\r\n"), "Title {"), Start(title["range"]));

        var button = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "<Button", 2)))!.AsArray());
        var file = new Uri(button!["uri"]!.GetValue<string>()).LocalPath;
        Assert.Equal("Button.cs", Path.GetFileName(file));
        Assert.True(File.GetAttributes(file).HasFlag(FileAttributes.ReadOnly));
        var lines = File.ReadAllText(file).Split('\n');
        var (line, character) = Start(button["range"]);
        Assert.StartsWith("public sealed class Button : ButtonBase", lines[line], StringComparison.Ordinal);
        Assert.Equal("Button", lines[line].Substring(character, 6));
        Assert.Contains("namespace Nazor.Controls;", lines);

        var disabled = Assert.Single((await RequestAtAsync("textDocument/definition", path, At(text, "Disabled=", 1)))!.AsArray());
        var parameterFile = new Uri(disabled!["uri"]!.GetValue<string>()).LocalPath;
        Assert.Equal("ButtonBase.cs", Path.GetFileName(parameterFile));
        lines = File.ReadAllLines(parameterFile);
        (line, character) = Start(disabled["range"]);
        Assert.Equal("    public bool Disabled { get; set; }", lines[line]);
        Assert.Equal("    [Parameter]", lines[line - 1]);
        Assert.Equal("Disabled", lines[line].Substring(character, 8));
    }

    [Fact]
    public async Task Members_are_renamed_wherever_the_project_uses_them()
    {
        var (path, text) = await OpenAsync(Counter);

        var prepare = await RequestAtAsync("textDocument/prepareRename", path, At(text, "count++", 2));
        Assert.Equal("count", prepare!["placeholder"]!.GetValue<string>());
        Assert.Equal(At(text, "count++"), Start(prepare["range"]));
        Assert.Equal("Card", (await RequestAtAsync("textDocument/prepareRename", path, At(text, "<Card", 2)))!["placeholder"]!.GetValue<string>());
        Assert.Null(await RequestAtAsync("textDocument/prepareRename", path, At(text, "<Button", 2)));

        var rename = await client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = At(text, "count++").Line, ["character"] = At(text, "count++").Character },
            ["newName"] = "clicks",
        });
        var changes = rename!["changes"]!.AsObject();
        var counter = Apply(text, changes.Single(c => c.Key.EndsWith("Counter.nazor", StringComparison.OrdinalIgnoreCase)).Value!.AsArray());
        Assert.Equal(text.Replace("@count", "@clicks").Replace("(count", "(clicks").Replace("int count;", "int clicks;").Replace("=> count++", "=> clicks++"), counter);
        var codeBehind = Apply(CodeBehind.ReplaceLineEndings("\r\n"), changes.Single(c => c.Key.EndsWith("Counter.nazor.cs", StringComparison.OrdinalIgnoreCase)).Value!.AsArray());
        Assert.Contains("Reset() => clicks = 0;", codeBehind);

        var title = await RequestAtAsync("textDocument/prepareRename", path, At(text, "Title=", 1));
        Assert.Equal("Title", title!["placeholder"]!.GetValue<string>());
        var renamed = await client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = At(text, "Title=").Line, ["character"] = At(text, "Title=").Character + 1 },
            ["newName"] = "Caption",
        });
        var titleChanges = renamed!["changes"]!.AsObject();
        Assert.Contains("<Card Caption=\"Total\" />", Apply(text, titleChanges.Single(c => c.Key.EndsWith("Counter.nazor", StringComparison.OrdinalIgnoreCase)).Value!.AsArray()));
        var card = Apply(Card.ReplaceLineEndings("\r\n"), titleChanges.Single(c => c.Key.EndsWith("Card.nazor", StringComparison.OrdinalIgnoreCase)).Value!.AsArray());
        Assert.Contains("public string? Caption { get; set; }", card);
        Assert.Contains("@Caption", card);
    }

    [Fact]
    public async Task A_component_is_renamed_with_its_tags_and_its_files()
    {
        File.WriteAllText(Path.Combine(directory, "Card.nazor.nss"), "TextBlock { color: red; }\r\n");
        var (path, text) = await OpenAsync("""
            <Container>
                <Card Title="Uno" />
                <Card Title="Due"></Card>
            </Container>

            @code {
                private System.Type kind = typeof(Card);
            }

            """, "Panel.nazor");

        var prepare = await RequestAtAsync("textDocument/prepareRename", path, At(text, "</Card>", 3));
        Assert.Equal("Card", prepare!["placeholder"]!.GetValue<string>());
        Assert.Equal(At(text, "</Card>", 2), Start(prepare["range"]));

        var rename = await client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = At(text, "<Card", 2).Line, ["character"] = At(text, "<Card", 2).Character },
            ["newName"] = "Label",
        });
        var operations = rename!["documentChanges"]!.AsArray();
        var panel = operations.Single(o => o!["textDocument"]?["uri"]?.GetValue<string>().EndsWith("Panel.nazor", StringComparison.OrdinalIgnoreCase) == true)!;
        Assert.Equal(text.Replace("Card", "Label"), Apply(text, panel["edits"]!.AsArray()));
        var renames = operations.Where(o => o!["kind"]?.GetValue<string>() == "rename").ToList();
        Assert.True(operations.IndexOf(renames[0]) > operations.IndexOf(panel));
        Assert.Equal(["Card.nazor -> Label.nazor", "Card.nazor.nss -> Label.nazor.nss"],
            renames.Select(r => $"{Path.GetFileName(new Uri(r!["oldUri"]!.GetValue<string>()).LocalPath)} -> {Path.GetFileName(new Uri(r!["newUri"]!.GetValue<string>()).LocalPath)}"));

        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(() => client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = At(text, "<Card", 2).Line, ["character"] = At(text, "<Card", 2).Character },
            ["newName"] = "1Card",
        }));
        Assert.Contains("-32803", invalid.Message, StringComparison.Ordinal);
        Assert.Contains("not a valid component name", invalid.Message, StringComparison.Ordinal);
        var taken = await Assert.ThrowsAsync<InvalidOperationException>(() => client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = At(text, "<Card", 2).Line, ["character"] = At(text, "<Card", 2).Character },
            ["newName"] = "Panel",
        }));
        Assert.Contains("Panel.nazor already exists", taken.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_renamed_component_takes_its_code_behind_with_it()
    {
        File.WriteAllText(Path.Combine(directory, "Counter.nazor"), Counter.ReplaceLineEndings("\r\n"));
        var (path, text) = await OpenAsync("<Counter />\r\n", "Panel.nazor");
        var rename = await client.RequestAsync("textDocument/rename", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["position"] = new JsonObject { ["line"] = 0, ["character"] = 2 },
            ["newName"] = "Clicker",
        });
        var operations = rename!["documentChanges"]!.AsArray();
        string Edited(string name, string before) => Apply(before, operations.Single(o =>
            o!["textDocument"]?["uri"]?.GetValue<string>().EndsWith(name, StringComparison.OrdinalIgnoreCase) == true)!["edits"]!.AsArray());
        Assert.Equal("<Clicker />\r\n", Edited("Panel.nazor", text));
        Assert.Contains("public partial class Clicker", Edited("Counter.nazor.cs", CodeBehind.ReplaceLineEndings("\r\n")), StringComparison.Ordinal);
        Assert.Equal(["Counter.nazor", "Counter.nazor.cs"], operations.Where(o => o!["kind"]?.GetValue<string>() == "rename")
            .Select(r => Path.GetFileName(new Uri(r!["oldUri"]!.GetValue<string>()).LocalPath)));
    }

    [Fact]
    public async Task Visual_studio_auto_insert_closes_tags()
    {
        Assert.Contains(">", capabilities["_vs_onAutoInsertProvider"]!["_vs_triggerCharacters"]!.AsArray().Select(c => c!.GetValue<string>()));
        var (path, text) = await OpenAsync("<Container>\r\n    <Container>\r\n</Container>\r\n");
        var at = At(text, "<Container>", 11, occurrence: 1);
        var insert = await client.RequestAsync("textDocument/_vs_onAutoInsert", new JsonObject
        {
            ["_vs_textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["_vs_position"] = new JsonObject { ["line"] = at.Line, ["character"] = at.Character },
            ["_vs_ch"] = ">",
            ["_vs_options"] = new JsonObject { ["tabSize"] = 4, ["insertSpaces"] = true },
        });
        Assert.Equal(2, insert!["_vs_textEditFormat"]!.GetValue<int>());
        Assert.Equal("$0</Container>", insert["_vs_textEdit"]!["newText"]!.GetValue<string>());
        Assert.Equal(at, Start(insert["_vs_textEdit"]!["range"]));
    }

    [Fact]
    public async Task Csharp_snippets_expand_where_their_shortcut_is()
    {
        var (path, text) = await OpenAsync(Counter.Replace("    private int count;", "    private int count;\n    prop\n\n    void Go()\n    {\n        for\n    }"));

        var prop = await RequestAtAsync("nazor/snippet", path, At(text, "prop\r", 4));
        Assert.Equal("public ${1:int} ${2:MyProperty} { get; set; }$0", prop!["snippet"]!.GetValue<string>());
        Assert.Equal(At(text, "prop\r"), Start(prop["range"]));
        Assert.Equal(At(text, "prop\r", 4), (prop["range"]!["end"]!["line"]!.GetValue<int>(), prop["range"]!["end"]!["character"]!.GetValue<int>()));

        var loop = (await RequestAtAsync("nazor/snippet", path, At(text, "for\r", 3)))!["snippet"]!.GetValue<string>().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        Assert.StartsWith("for (int ${1:i} = 0;", loop[0]);
        Assert.Equal(["        {", "            $0", "        }"], loop[1..]);

        Assert.Null(await RequestAtAsync("nazor/snippet", path, At(text, "count;", 5)));
    }

    [Fact]
    public async Task Classic_snippets_of_visual_studio_expand_where_they_fit()
    {
        var (path, text) = await OpenAsync(Counter.Replace("    private int count;", """
                private int count;
                propfull

                void Go()
                {
                    try
                }

                void Stop()
                {
                    if (count > 0) { }
                    switch
                }
            """));
        string[] Lines(JsonNode? snippet) => snippet!["snippet"]!.GetValue<string>().Split('\n');

        var propfull = await RequestAtAsync("nazor/snippet", path, At(text, "propfull", 8));
        Assert.Equal(At(text, "propfull"), Start(propfull!["range"]));
        Assert.Equal(["private ${1:int} ${2:myVar};", "", "    public ${1:int} ${3:MyProperty}", "    {", "        get { return ${2:myVar}; }",
            "        set { ${2:myVar} = value; }", "    }$0"], Lines(propfull));

        Assert.Equal(["try", "        {", "            $0", "        }", "        catch (${1:Exception})", "        {", "            throw;", "        }"],
            Lines(await RequestAtAsync("nazor/snippet", path, At(text, "try\r", 3))));
        Assert.Equal("switch (${1:switch_on})", Lines(await RequestAtAsync("nazor/snippet", path, At(text, "switch\r", 6)))[0]);

        var (other, otherText) = await OpenAsync(Counter.Replace("    private int count;", "    private int count;\n    tryf\n\n    void Go()\n    {\n        indexer\n    }"), "Other.nazor");
        Assert.Null(await RequestAtAsync("nazor/snippet", other, At(otherText, "tryf", 4)));
        Assert.Null(await RequestAtAsync("nazor/snippet", other, At(otherText, "indexer", 7)));

        async Task<List<string>> Labels(string file, (int Line, int Character) at) =>
            [.. (await RequestAtAsync("textDocument/completion", file, at))!["items"]!.AsArray().Select(i => i!["label"]!.GetValue<string>())];
        var members = await Labels(path, At(text, "propfull", 5));
        Assert.Contains("propfull", members);
        Assert.DoesNotContain("tryf", members);
        var statements = await Labels(path, At(text, "try\r", 2));
        Assert.Contains("tryf", statements);
        Assert.DoesNotContain("propfull", statements);
    }

    [Fact]
    public async Task A_rename_in_csharp_files_reaches_the_components()
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);

        await client.NotifyAsync("nazor/generatedChanged", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"),
            ["oldText"] = built,
            ["changes"] = new JsonArray([.. Occurrences(built, "Increment").Select(start => new JsonObject { ["start"] = start, ["length"] = "Increment".Length, ["text"] = "Add" })]),
        });
        var apply = await client.MessageAsync("workspace/applyEdit");
        Assert.NotNull(apply["id"]);
        var changes = apply["params"]!["edit"]!["changes"]!.AsObject();
        Assert.Equal(text.Replace("Increment", "Add"), Apply(text, Assert.Single(changes).Value!.AsArray()));
    }

    [Fact]
    public async Task A_rename_in_csharp_files_reaches_components_changed_after_the_build()
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);
        var changed = text.Replace("<Button @onclick=\"Reset\">Reset</Button>", "<Button @onclick=\"Reset\">Reset</Button>\r\n    <Button @onclick=\"Increment\">Again</Button>");
        await client.ChangeAsync(path, changed, 2);

        await client.NotifyAsync("nazor/generatedChanged", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"),
            ["oldText"] = built,
            ["changes"] = new JsonArray([.. Occurrences(built, "Increment").Select(start => new JsonObject { ["start"] = start, ["length"] = "Increment".Length, ["text"] = "Add" })]),
        });
        var apply = await client.MessageAsync("workspace/applyEdit");
        var changes = apply["params"]!["edit"]!["changes"]!.AsObject();
        Assert.Equal(changed.Replace("Increment", "Add"), Apply(changed, Assert.Single(changes).Value!.AsArray()));
    }

    [Fact]
    public async Task Positions_of_the_built_csharp_lead_to_the_component()
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Release", path);
        var built = Generated(path, text);
        JsonObject Request(string marker, string csharp) => new()
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"),
            ["text"] = csharp,
            ["position"] = new JsonObject
            {
                ["line"] = Protocol.TextPositions.Position(csharp, csharp.IndexOf(marker, StringComparison.Ordinal)).Line,
                ["character"] = Protocol.TextPositions.Position(csharp, csharp.IndexOf(marker, StringComparison.Ordinal)).Character,
            },
        };

        var original = await client.RequestAsync("nazor/originalPosition", Request("count++", built));
        Assert.Equal(LspClient.Uri(path), original!["uri"]!.GetValue<string>(), ignoreCase: true);
        Assert.Equal(At(text, "count++"), Start(original["range"]));
        Assert.Null(await client.RequestAsync("nazor/originalPosition", Request("partial class Counter", built)));
        Assert.Null(await client.RequestAsync("nazor/originalPosition", Request("count++", built.Replace("count++", "count += 1"))));
    }

    [Theory]
    [InlineData("count++")]
    [InlineData("count > 9")]
    [InlineData("Increment", true)]
    [InlineData("Increment", true, true)]
    public async Task Reference_rows_have_the_original_file_position_and_preview(string marker, bool markup = false, bool fullSpan = false)
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);
        var offset = built.IndexOf(marker, StringComparison.Ordinal);
        var position = Protocol.TextPositions.Position(built, offset);
        var reference = await client.RequestAsync("nazor/referencePosition", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"), ["text"] = built,
            ["position"] = new JsonObject { ["line"] = position.Line, ["character"] = position.Character },
            ["length"] = fullSpan ? marker.Length : 0,
        });
        Assert.NotNull(reference);
        Assert.Equal(LspClient.Uri(path), reference["uri"]!.GetValue<string>(), ignoreCase: true);
        Assert.Equal(At(text, marker), Start(reference["range"]));
        Assert.Equal(Start(reference["range"]).Character + (fullSpan ? marker.Length : 0), reference["range"]!["end"]!["character"]!.GetValue<int>());
        var preview = reference["preview"]!.GetValue<string>();
        Assert.Contains(marker, preview);
        if (markup) Assert.Contains("<Button", preview);
        Assert.DoesNotContain("__builder", preview);
    }

    [Fact]
    public async Task Native_codelens_partial_component_declaration_maps_to_the_source_start_as_razor_does()
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);
        var offset = built.IndexOf("partial class Counter", StringComparison.Ordinal) + "partial class ".Length;
        var position = Protocol.TextPositions.Position(built, offset);
        var reference = await client.RequestAsync("nazor/referencePosition", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"), ["text"] = built, ["length"] = "Counter".Length,
            ["position"] = new JsonObject { ["line"] = position.Line, ["character"] = position.Character },
        });
        Assert.NotNull(reference);
        Assert.Equal(LspClient.Uri(path), reference["uri"]!.GetValue<string>(), ignoreCase: true);
        Assert.Equal((0, 0), Start(reference["range"]));
        Assert.Contains("<Container", reference["preview"]!.GetValue<string>());
    }

    [Fact]
    public async Task Reference_rows_follow_an_unchanged_method_after_unsaved_lines_are_inserted()
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);
        var position = Protocol.TextPositions.Position(built, built.IndexOf("count++", StringComparison.Ordinal));
        var changed = "@* Unsaved heading *@\r\n\r\n" + text;
        await client.ChangeAsync(path, changed, 2);
        var reference = await client.RequestAsync("nazor/referencePosition", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"), ["text"] = built,
            ["position"] = new JsonObject { ["line"] = position.Line, ["character"] = position.Character },
        });
        Assert.NotNull(reference);
        Assert.Equal(At(changed, "count++"), Start(reference["range"]));
        Assert.Contains("count++", reference["preview"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("private void Increment() { }")]
    [InlineData("private void Increment() => total++;")]
    public async Task Reference_rows_do_not_guess_a_changed_or_deleted_reference(string replacement)
    {
        var (path, text) = await OpenAsync(Counter);
        var output = Build("Debug", path);
        var built = Generated(path, text);
        var position = Protocol.TextPositions.Position(built, built.IndexOf("count++", StringComparison.Ordinal));
        await client.ChangeAsync(path, text.Replace("private void Increment() => count++;", replacement), 2);
        var reference = await client.RequestAsync("nazor/referencePosition", new JsonObject
        {
            ["path"] = Path.Combine(output, "Counter.nazor.g.cs"), ["text"] = built,
            ["position"] = new JsonObject { ["line"] = position.Line, ["character"] = position.Character },
        });
        Assert.Null(reference);
    }

    [Fact]
    public void Only_changes_to_the_csharp_of_the_component_as_it_is_reach_it()
    {
        var path = Path.Combine(directory, "Counter.nazor");
        var before = Counter.ReplaceLineEndings("\r\n");
        var built = Generated(path, before);
        var component = Component(path, before);
        Assert.Empty(Features.CSharpLanguage.GeneratedEdits(component, built, Occurrences(built, "partial class Counter").Select(start => (start, 7, "sealed "))));

        var twice = before.Replace("    private void Increment() => count++;", "    private void Increment() => count++;\r\n    private void Twice() => count++;");
        var edited = twice.Replace("Increment() => count++", "Increment() => clicks++");
        var regenerated = Generated(path, twice);
        Assert.Empty(Features.CSharpLanguage.GeneratedEdits(Component(path, edited), regenerated, Occurrences(regenerated, "count++").Take(1).Select(start => (start, 5, "clicks"))));
    }

    [Fact]
    public async Task Only_a_rename_reaches_a_component_changed_after_the_build()
    {
        var path = Path.Combine(directory, "Counter.nazor");
        var before = Counter.ReplaceLineEndings("\r\n");
        var built = Generated(path, before);
        var store = new Workspace.DocumentStore();
        store.Set(LspClient.Uri(path), before.Replace("Go</Button>", "Vai</Button>"), 1);
        var snapshot = new Workspace.ProjectAnalyzer(Workspace.ProjectContext.ForDirectory(directory), store).Analyze();
        var component = snapshot.Component(path)!;
        var occurrences = Occurrences(built, "Increment").ToList();
        var cancellation = TestContext.Current.CancellationToken;

        Assert.Empty(await Features.CSharpLanguage.StaleRenameEditsAsync(snapshot, component, built, [(occurrences[0], 9, "Add"), (occurrences[1], 9, "Plus")], cancellation));
        Assert.Empty(await Features.CSharpLanguage.StaleRenameEditsAsync(snapshot, component, built, [.. occurrences.Select(o => (o, 9, "Add()"))], cancellation));
        var edits = await Features.CSharpLanguage.StaleRenameEditsAsync(snapshot, component, built, [.. occurrences.Select(o => (o, 9, "Add"))], cancellation);
        Assert.Equal(2, edits.Count);
        Assert.All(edits, e => Assert.Equal("Add", e.NewText));
    }

    private Workspace.ComponentDocument Component(string path, string text)
    {
        var store = new Workspace.DocumentStore();
        store.Set(LspClient.Uri(path), text, 1);
        return new Workspace.ProjectAnalyzer(Workspace.ProjectContext.ForDirectory(directory), store).Analyze().Component(path)!;
    }

    private string Build(string configuration, string component)
    {
        var output = Directory.CreateDirectory(Path.Combine(directory, "obj", configuration, "net10.0", "Nazor")).FullName;
        string[] references = [typeof(Nazor.ComponentBase).Assembly.Location, typeof(Nazor.Controls.Button).Assembly.Location, typeof(Nazor.Style.StyleSheet).Assembly.Location];
        File.WriteAllLines(Path.Combine(output, "nazorc.rsp"), ["generate", "--out", output, "--root-namespace", "Demo", "--project-dir", directory,
            .. references.SelectMany(r => new[] { "--reference", r }), "--source", Path.Combine(directory, "Counter.nazor.cs"), component, Path.Combine(directory, "Card.nazor")]);
        return output;
    }

    private string Generated(string path, string text)
    {
        var store = new Workspace.DocumentStore();
        store.Set(LspClient.Uri(path), text, 1);
        return new Workspace.ProjectAnalyzer(Workspace.ProjectContext.ForDirectory(directory), store).Analyze().Component(path)!.Generated.CSharp;
    }

    private static IEnumerable<int> Occurrences(string text, string value)
    {
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + 1, StringComparison.Ordinal)) yield return i;
    }

    private static string Apply(string text, JsonArray edits)
    {
        foreach (var edit in edits.Select(e => e!).OrderByDescending(e => Protocol.TextPositions.Offset(text, new(e["range"]!["start"]!["line"]!.GetValue<int>(), e["range"]!["start"]!["character"]!.GetValue<int>()))))
        {
            var start = Protocol.TextPositions.Offset(text, new(edit["range"]!["start"]!["line"]!.GetValue<int>(), edit["range"]!["start"]!["character"]!.GetValue<int>()));
            var end = Protocol.TextPositions.Offset(text, new(edit["range"]!["end"]!["line"]!.GetValue<int>(), edit["range"]!["end"]!["character"]!.GetValue<int>()));
            text = text[..start] + edit["newText"]!.GetValue<string>() + text[end..];
        }
        return text;
    }
}
