using System.Text.Json.Nodes;

namespace Nazor.LanguageServer.Tests;

public sealed class CSharpTests : IAsyncLifetime
{
    private const string Counter = """
        <Container Class="counter">
            <TextBlock>Count: @count</TextBlock>
            <Button @onclick="Increment" Disabled="@(count > 9)">Go</Button>
        </Container>

        @style {
            .counter { gap: 4px; }
        }

        @code {
            private int count;
            private string label = "";

            private void Increment() => count++;
        }

        """;

    private readonly string directory = Directory.CreateTempSubdirectory("nazor-csharp-").FullName;
    private LspClient client = null!;
    private string[] legend = [];

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        client = LspClient.InProcess();
        var result = await client.InitializeAsVisualStudioAsync();
        legend = [.. result!["capabilities"]!["semanticTokensProvider"]!["legend"]!["tokenTypes"]!.AsArray().Select(t => t!.GetValue<string>())];
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

    private static (int Line, int Character) At(string text, string marker, int delta = 0)
    {
        var offset = text.IndexOf(marker, StringComparison.Ordinal) + delta;
        Assert.True(offset >= delta, $"'{marker}' not found");
        var position = Protocol.TextPositions.Position(text, offset);
        return (position.Line, position.Character);
    }

    private async Task<JsonArray> CompleteAsync(string path, (int Line, int Character) at) =>
        (await client.AtAsync("textDocument/completion", path, at.Line, at.Character))!["items"]!.AsArray();

    [Fact]
    public async Task Window_parameters_have_hover_navigation_and_symbols_and_Window_members_complete()
    {
        var (path, text) = await OpenAsync("""
            @window (int OrderId, string? CustomerName = null)
            <TextBlock>@OrderId</TextBlock>
            @code {
                protected override void OnInitialized() { Window.Title = CustomerName ?? "Order"; }
            }
            """, "Orders.nazor");
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.True(diagnostics.All(d => d!["severity"]!.GetValue<int>() > 2), diagnostics.ToJsonString());
        var hover = await client.AtAsync("textDocument/hover", path, At(text, "OrderId", 2).Line, At(text, "OrderId", 2).Character);
        Assert.Contains("OrderId", hover!.ToJsonString());
        var symbols = await client.RequestAsync("textDocument/documentSymbol", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } });
        Assert.Contains("OrderId", symbols!.ToJsonString());
        var usage = text.LastIndexOf("@OrderId", StringComparison.Ordinal) + 2;
        var at = Protocol.TextPositions.Position(text, usage);
        var definition = await client.AtAsync("textDocument/definition", path, at.Line, at.Character);
        Assert.Contains("\"line\":0", definition!.ToJsonString());
        var items = await CompleteAsync(path, At(text, "Window.Title", "Window.".Length));
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "Decorations");
        foreach (var name in new[] { "MinWidth", "MinHeight", "MaxWidth", "MaxHeight", "X", "Y" })
            Assert.Contains(items, i => i!["label"]!.GetValue<string>() == name);
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "EffectiveIcon");
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("none")]
    public async Task Resize_grip_in_embedded_styles_keeps_CSharp_colors_hover_and_completions(string value)
    {
        var (path, text) = await OpenAsync($$"""
            @window
            <TextBlock>@label.Length</TextBlock>
            @style { :root { -window-resize-grip: {{value}}; } }
            @code { private string label = "Nazor"; }
            """);
        Assert.Empty(await client.DiagnosticsAsync(path));
        var items = await CompleteAsync(path, At(text, "label.", "label.".Length));
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "Length");
        var at = At(text, "@label", 2);
        Assert.NotNull(await client.AtAsync("textDocument/hover", path, at.Line, at.Character));
        var tokens = await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
        });
        Assert.NotEmpty(tokens!["data"]!.AsArray());
        var values = await CompleteAsync(path, At(text, "resize-grip: ", "resize-grip: ".Length));
        Assert.Contains(values, i => i!["label"]!.GetValue<string>() == "auto");
        Assert.Contains(values, i => i!["label"]!.GetValue<string>() == "none");
    }

    [Theory]
    [InlineData("<Button Disabled=\"\" />", "Disabled=\"", "true")]
    [InlineData("<CheckBox IsChecked=\"\" />", "IsChecked=\"", "false")]
    [InlineData("<CheckBox IsChecked=\"\" />", "IsChecked=\"", "true")]
    public async Task Attribute_values_offer_the_parameter_type_even_without_a_CSharp_mapping(string text, string marker, string expected)
    {
        var (path, source) = await OpenAsync(text);
        var items = await CompleteAsync(path, At(source, marker, marker.Length));
        var item = Assert.Single(items, i => i!["label"]!.GetValue<string>() == expected)!;
        Assert.Equal(expected, item["textEdit"]!["newText"]!.GetValue<string>());
        Assert.StartsWith("0000_", item["sortText"]!.GetValue<string>());
        var range = item["textEdit"]!["range"]!;
        var replacedStart = Protocol.TextPositions.Offset(source, new(range["start"]!["line"]!.GetValue<int>(), range["start"]!["character"]!.GetValue<int>()));
        var replacedEnd = Protocol.TextPositions.Offset(source, new(range["end"]!["line"]!.GetValue<int>(), range["end"]!["character"]!.GetValue<int>()));
        Assert.Equal(source.IndexOf('"') + 1, replacedStart);
        Assert.Equal(source.LastIndexOf('"'), replacedEnd);
    }

    [Fact]
    public async Task Enum_completion_leaves_imports_to_explicit_quick_fixes()
    {
        File.WriteAllText(Path.Combine(directory, "Mode.cs"), "namespace Choices; public enum ViewMode { List, Grid }");
        await OpenAsync("@namespace Widgets\n@code { [Parameter] public Choices.ViewMode Mode { get; set; } }", "Choice.nazor");
        var (path, source) = await OpenAsync("@using Widgets\n<Choice Mode=\"\" />", "Page.nazor");
        var item = Assert.Single(await CompleteAsync(path, At(source, "Mode=\"", "Mode=\"".Length)),
            i => i!["label"]!.GetValue<string>() == "ViewMode.Grid")!;
        Assert.Null(item["additionalTextEdits"]);
        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, "@using Choices\r\n" + source.Replace("Mode=\"\"", "Mode=\"ViewMode.Grid\"", StringComparison.Ordinal), 2);
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Fact]
    public async Task Non_generic_parameter_values_are_completed_on_generic_components()
    {
        var (path, source) = await OpenAsync("<Select TValue=\"string\" Disabled=\"\" />");
        var items = await CompleteAsync(path, At(source, "Disabled=\"", "Disabled=\"".Length));
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "false" && i["detail"]!.GetValue<string>() == "bool");
    }

    [Fact]
    public async Task Component_completion_leaves_imports_to_explicit_quick_fixes()
    {
        await OpenAsync("@namespace Widgets\n<TextBlock>Order</TextBlock>", "OrderCard.nazor");
        var (path, source) = await OpenAsync("<OrderCard />", "Page.nazor");
        var item = Assert.Single(await CompleteAsync(path, At(source, "OrderCard", 4)),
            i => i!["label"]!.GetValue<string>() == "OrderCard")!;
        Assert.Null(item["additionalTextEdits"]);
        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, "@using Widgets\n" + source, 2);
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Fact]
    public async Task Unknown_types_offer_a_missing_import_quick_fix()
    {
        var (path, text) = await OpenAsync("@code { private CultureInfo culture = null!; }");
        await client.DiagnosticsAsync(path, d => d.Any(x => x!["code"]?.GetValue<string>() == "CS0246"));
        var at = At(text, "CultureInfo", 2);
        var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["range"] = new JsonObject
            {
                ["start"] = new JsonObject { ["line"] = at.Line, ["character"] = at.Character },
                ["end"] = new JsonObject { ["line"] = at.Line, ["character"] = at.Character },
            },
            ["context"] = new JsonObject { ["diagnostics"] = new JsonArray(), ["only"] = new JsonArray("quickfix") },
        }))!.AsArray();
        var action = Assert.Single(actions, a => a!["title"]!.GetValue<string>() == "Add @using System.Globalization")!;
        Assert.Equal("quickfix", action["kind"]!.GetValue<string>());
        Assert.StartsWith("@using System.Globalization\n", action["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task Generic_components_are_recognized_in_unsaved_markup_with_completion_hover_and_colors()
    {
        await OpenAsync("@typeparam TItem\n<TextBlock>@Item</TextBlock>\n@code { [Parameter] public TItem Item { get; set; } = default!; }", "GenericCard.nazor");
        var (path, text) = await OpenAsync("<GenericCard Item=\"value\" />\n@code { private string value = \"hello\"; }", "Page.nazor");
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.True(diagnostics.Count == 0, diagnostics.ToJsonString());
        var completion = await CompleteAsync(path, At(text, "<GenericCard", 3));
        Assert.Contains(completion, item => item!["label"]!.GetValue<string>() == "GenericCard");
        var hoverAt = At(text, "<GenericCard", 3);
        var hover = await client.AtAsync("textDocument/hover", path, hoverAt.Line, hoverAt.Character);
        Assert.NotNull(hover);
        Assert.Contains("GenericCard", hover["contents"]!["value"]!.GetValue<string>());
        var tokens = await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = new Uri(path).AbsoluteUri },
        });
        var decoded = Decode(text, tokens!["data"]!.AsArray().Select(item => item!.GetValue<int>()).ToArray());
        var tagOffset = text.IndexOf("GenericCard", StringComparison.Ordinal);
        Assert.Contains(decoded, token => token.Start <= tagOffset && token.Start + token.Length > tagOffset && token.Type == "razorComponentElement");
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("")]
    public async Task Generic_template_context_completes_members_and_has_rich_hover(string member)
    {
        await OpenAsync("""
            @typeparam TItem
            @code {
                [Parameter] public IEnumerable<TItem> Items { get; set; } = [];
                [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = null!;
            }
            """, "ItemList.nazor");
        var (path, text) = await OpenAsync("""
            <ItemList Items="items">
                <ItemTemplate Context="item">
                    <TextBlock>@item.Name</TextBlock>
                </ItemTemplate>
            </ItemList>
            @code {
                private List<TaskItem> items = [];
                public record TaskItem(string Name);
            }
            """.Replace("@item.Name", "@item." + member), "Page.nazor");
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.True(diagnostics.Count == 0, diagnostics.ToJsonString());
        var items = await CompleteAsync(path, At(text, "@item.", 6));
        Assert.Contains(items, item => item!["label"]!.GetValue<string>() == "Name");
        var at = At(text, "@item.", 2);
        var hover = await client.AtAsync("textDocument/hover", path, at.Line, at.Character);
        Assert.NotNull(hover);
        Assert.Contains("TaskItem", hover["contents"]!["value"]!.GetValue<string>());
        Assert.NotNull(hover["_vs_rawContent"]);
    }

    [Fact]
    public async Task Generic_component_tag_has_rich_hover_in_visual_studio()
    {
        await OpenAsync("@typeparam TItem\n@code { [Parameter] public TItem Item { get; set; } = default!; }", "GenericCard.nazor");
        var (path, text) = await OpenAsync("<GenericCard Item=\"value\" />\n@code { private string value = \"hello\"; }", "Page.nazor");
        var at = At(text, "<GenericCard", 3);
        var hover = await client.AtAsync("textDocument/hover", path, at.Line, at.Character);
        Assert.NotNull(hover);
        Assert.Contains("GenericCard", hover["contents"]!["value"]!.GetValue<string>());
        Assert.NotNull(hover["_vs_rawContent"]);
    }

    [Fact]
    public async Task Generic_template_with_multiple_parameters_and_code_behind_has_csharp_editor_services()
    {
        async Task<(string Path, string Text)> OpenSavedAsync(string content, string name)
        {
            File.WriteAllText(Path.Combine(directory, name), content);
            return await OpenAsync(content, name);
        }
        File.WriteAllText(Path.Combine(directory, "Page.nazor.cs"), """
            using System.Collections.Generic;
            namespace Demo;
            public partial class Page
            {
                private IEnumerable<TaskItem> VisibleTasks => [];
                private void CompleteTask(int id, bool completed) { }
            }
            public record TaskItem(int Id, string Name);
            """);
        await OpenSavedAsync("""
            @typeparam TItem
            @code {
                [Parameter] public IEnumerable<TItem> Items { get; set; } = [];
                [Parameter] public Func<TItem, object>? ItemKey { get; set; }
                [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = null!;
                [Parameter] public RenderFragment? EmptyContent { get; set; }
                [Parameter] public string? Class { get; set; }
            }
            """, "ItemList.nazor");
        await OpenSavedAsync("@code { [Parameter] public TaskItem Item { get; set; } = null!; [Parameter] public EventCallback<bool> CompletedChanged { get; set; } }", "TaskCard.nazor");
        var (path, text) = await OpenSavedAsync("""
            <ItemList Items="VisibleTasks" ItemKey="item => item.Id" Class="scroll-fill">
                <ItemTemplate Context="item">
                    <TaskCard Item="item" CompletedChanged="completed => CompleteTask(item.Id, completed)" />
                    <TextBlock>@item.Name</TextBlock>
                </ItemTemplate>
                <EmptyContent><TextBlock>No items</TextBlock></EmptyContent>
            </ItemList>
            """, "Page.nazor");
        var diagnostics = await client.DiagnosticsAsync(path);
        Assert.True(diagnostics.Count == 0, diagnostics.ToJsonString());
        foreach (var (marker, delta) in new[] { ("<ItemList", 3), ("Items=", 2), ("ItemKey=", 2), ("@item.Name", 3), ("item.Id, completed", 6), ("<EmptyContent", 3), ("</EmptyContent", 3), ("<ItemTemplate", 3) })
        {
            var at = At(text, marker, delta);
            var hover = await client.AtAsync("textDocument/hover", path, at.Line, at.Character);
            Assert.True(hover?["_vs_rawContent"] is not null, $"Missing C# hover at {marker}: {hover}");
        }
        var items = await CompleteAsync(path, At(text, "item.Id, completed", 5));
        Assert.Contains(items, item => item!["label"]!.GetValue<string>() == "Id");
        var tokens = await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject
        { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } });
        var decoded = Decode(text, tokens!["data"]!.AsArray().Select(item => item!.GetValue<int>()).ToArray());
        var itemOffset = text.IndexOf("item.Id, completed", StringComparison.Ordinal);
        Assert.Contains(decoded, t => t.Start == itemOffset && t.Type == "parameter name");
        foreach (var child in new[] { "ItemTemplate", "EmptyContent" })
        {
            Assert.Contains(decoded, t => t.Start == text.IndexOf("<" + child, StringComparison.Ordinal) + 1 && t.Type == "razorComponentElement");
            Assert.Contains(decoded, t => t.Start == text.IndexOf("</" + child, StringComparison.Ordinal) + 2 && t.Type == "razorComponentElement");
        }
        Assert.Contains(await CompleteAsync(path, At(text, "<EmptyContent", 3)), item => item!["label"]!.GetValue<string>() == "EmptyContent");
    }

    [Fact]
    public async Task Code_blocks_complete_members_with_the_images_of_csharp()
    {
        var (path, text) = await OpenAsync(Counter.Replace("private void Increment() => count++;", "private void Increment() => this.;"));
        var items = await CompleteAsync(path, At(text, "this.", 5));
        var count = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "count")!;
        Assert.Equal(5, count["kind"]!.GetValue<int>());
        Assert.Equal(1220, count["_vs_icon"]!["ImageId"]!["Id"]!.GetValue<int>());
        Assert.Equal("ImageElement", count["_vs_icon"]!["_vs_type"]!.GetValue<string>());
        var increment = Assert.Single(items, i => i!["label"]!.GetValue<string>() == "Increment")!;
        Assert.Equal(1878, increment["_vs_icon"]!["ImageId"]!["Id"]!.GetValue<int>());
        Assert.Equal(At(text, "this.", 5).Character, count["textEdit"]!["range"]!["start"]!["character"]!.GetValue<int>());

        var resolved = await client.RequestAsync("completionItem/resolve", count.DeepClone().AsObject());
        var runs = resolved!["_vs_description"]!["Runs"]!.AsArray().Select(r => r!["Text"]!.GetValue<string>()).ToList();
        Assert.Contains("int", runs);
        Assert.Contains("count", runs);
        Assert.Contains(resolved["_vs_description"]!["Runs"]!.AsArray(), r => r!["ClassificationTypeName"]!.GetValue<string>() == "keyword");
    }

    [Fact]
    public async Task Expressions_and_attribute_values_complete_csharp()
    {
        var (path, text) = await OpenAsync(Counter.Replace("Count: @count", "Count: @cou").Replace("\"Increment\"", "\"Incr\""));
        Assert.Contains(await CompleteAsync(path, At(text, "@cou", 4)), i => i!["label"]!.GetValue<string>() == "count");
        Assert.Contains(await CompleteAsync(path, At(text, "\"Incr\"", 5)), i => i!["label"]!.GetValue<string>() == "Increment");
        var members = await CompleteAsync(path, At(text, "count > 9", 0));
        Assert.Contains(members, i => i!["label"]!.GetValue<string>() == "label");
    }

    [Fact]
    public async Task Member_access_completes_the_members_of_the_type()
    {
        var (path, text) = await OpenAsync(Counter.Replace("private void Increment() => count++;", "private void Increment() => label.;"));
        var items = await CompleteAsync(path, At(text, "label.;", 6));
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "Length");
        Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "Substring");
    }

    [Fact]
    public async Task Quick_info_is_the_one_of_csharp()
    {
        var (path, text) = await OpenAsync(Counter);
        var field = await client.AtAsync("textDocument/hover", path, At(text, "@count", 2).Line, At(text, "@count", 2).Character);
        var raw = field!["_vs_rawContent"]!;
        Assert.Equal("ContainerElement", raw["_vs_type"]!.GetValue<string>());
        var description = raw["Elements"]![0]!;
        Assert.Equal("ContainerElement", description["_vs_type"]!.GetValue<string>());
        Assert.Equal(1220, description["Elements"]![0]!["ImageId"]!["Id"]!.GetValue<int>());
        var text1 = string.Concat(description["Elements"]![1]!["Runs"]!.AsArray().Select(r => r!["Text"]!.GetValue<string>()));
        Assert.Contains("int", text1);
        Assert.Contains("count", text1);
        Assert.Contains("count", field["contents"]!["value"]!.GetValue<string>());
        Assert.Equal(At(text, "@count", 1).Character, field["range"]!["start"]!["character"]!.GetValue<int>());
    }

    [Fact]
    public async Task Components_and_parameters_have_the_quick_info_of_their_class_and_property()
    {
        var (path, text) = await OpenAsync(Counter);
        var button = await client.AtAsync("textDocument/hover", path, At(text, "<Button", 3).Line, At(text, "<Button", 3).Character);
        var buttonText = button!["contents"]!["value"]!.GetValue<string>();
        Assert.Contains("class", buttonText);
        Assert.Contains("Nazor.Controls.Button", buttonText);
        Assert.Equal(473, button["_vs_rawContent"]!["Elements"]![0]!["Elements"]![0]!["ImageId"]!["Id"]!.GetValue<int>());

        var disabled = await client.AtAsync("textDocument/hover", path, At(text, "Disabled", 2).Line, At(text, "Disabled", 2).Character);
        var disabledText = disabled!["contents"]!["value"]!.GetValue<string>();
        Assert.Contains("bool", disabledText);
        Assert.Contains("Disabled", disabledText);
    }

    [Fact]
    public async Task Csharp_errors_are_reported_on_the_nazor_file()
    {
        var (path, text) = await OpenAsync(Counter.Replace("Count: @count", "Count: @cout"));
        var diagnostics = await client.DiagnosticsAsync(path, d => d.Any(e => e!["code"]!.GetValue<string>() == "CS0103"));
        var error = diagnostics.Single(e => e!["code"]!.GetValue<string>() == "CS0103")!;
        Assert.Equal(At(text, "@cout", 1), (error["range"]!["start"]!["line"]!.GetValue<int>(), error["range"]!["start"]!["character"]!.GetValue<int>()));
        Assert.Equal(At(text, "@cout", 5).Character, error["range"]!["end"]!["character"]!.GetValue<int>());
        Assert.Contains("cout", error["message"]!.GetValue<string>());
    }

    [Fact]
    public async Task Wrong_values_and_parameters_of_attributes_are_reported_where_they_are_written()
    {
        var (path, text) = await OpenAsync("""
            <Button @onclick="Go" @onclick:stopPropagation="123" @onkeydown:colore="true" TabIndex="abc">Go</Button>
            <TextField @bind-Value="name" @bind-Text:colore="name" />
            <Button OnClick="Wrong">Ok</Button>

            @code {
                private string name = "";
                private void Go() { }
                private void Wrong(int value) { }
            }
            """, "Values.nazor");
        var diagnostics = await client.DiagnosticsAsync(path, d => d.Count >= 5);
        (int Line, int Character) Start(string code, string marker) => diagnostics
            .Select(d => (Code: d!["code"]!.GetValue<string>(), Line: d["range"]!["start"]!["line"]!.GetValue<int>(), Character: d["range"]!["start"]!["character"]!.GetValue<int>()))
            .Where(d => d.Code == code && d.Line == At(text, marker).Line).Select(d => (d.Line, d.Character)).Single();
        Assert.Equal(At(text, "123"), Start("CS1503", "123"));
        Assert.Equal(At(text, "abc"), Start("CS0103", "abc"));
        Assert.Equal(At(text, "onkeydown:colore").Line, Start("NZC012", "onkeydown:colore").Line);
        Assert.Equal(At(text, "bind-Text:colore"), Start("NZC013", "bind-Text:colore"));
        Assert.Contains(diagnostics, d => d!["code"]!.GetValue<string>() is "CS1503" or "CS1661" &&
            d["range"]!["start"]!["line"]!.GetValue<int>() == At(text, "\"Wrong\"").Line);
        Assert.DoesNotContain(diagnostics, d => d!["message"]!.GetValue<string>().Contains("__builder", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Colors_are_the_ones_of_Blazor()
    {
        var (path, text) = await OpenAsync(Counter);
        var data = (await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } }))!["data"]!
            .AsArray().Select(n => n!.GetValue<int>()).ToArray();
        var tokens = Decode(text, data);
        string? TypeAt(string marker, int delta = 0)
        {
            var offset = text.IndexOf(marker, StringComparison.Ordinal) + delta;
            return tokens.FirstOrDefault(t => t.Start <= offset && offset < t.Start + t.Length).Type;
        }
        Assert.Equal("razorComponentElement", TypeAt("Container"));
        Assert.Equal("markupTagDelimiter", TypeAt("<Container"));
        Assert.Equal("property name", TypeAt("Class="));
        Assert.Equal("markupOperator", TypeAt("=\"counter"));
        Assert.Equal("markupAttributeQuote", TypeAt("\"counter"));
        Assert.Equal("markupAttributeValue", TypeAt("counter\""));
        Assert.Equal("razorTransition", TypeAt("@count"));
        Assert.Equal("field name", TypeAt("@count", 1));
        Assert.Equal("razorTransition", TypeAt("@onclick"));
        Assert.Equal("razorDirectiveAttribute", TypeAt("@onclick", 1));
        Assert.Equal("method name", TypeAt("\"Increment", 1));
        Assert.Equal("razorTransition", TypeAt("@(count"));
        Assert.Equal("razorTransition", TypeAt("@(count", 1));
        Assert.Equal("operator", TypeAt("> 9"));
        Assert.Equal("number", TypeAt("9)"));
        Assert.Equal("razorDirective", TypeAt("@code", 1));
        Assert.Equal("razorTransition", TypeAt("@code {", 6));
        Assert.Equal("keyword", TypeAt("private int"));
        Assert.Equal("keyword", TypeAt("int count"));
        Assert.Equal("field name", TypeAt("count;"));
        Assert.Equal("string", TypeAt("\"\";"));
        Assert.Equal("method name", TypeAt("Increment()"));
        Assert.Equal("razorDirective", TypeAt("@style", 1));
        Assert.Null(TypeAt("gap"));
        Assert.Null(TypeAt("Count:"));
        Assert.Null(TypeAt("Go<"));

        var range = (await client.RequestAsync("textDocument/semanticTokens/range", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["range"] = new JsonObject { ["start"] = new JsonObject { ["line"] = 10, ["character"] = 0 }, ["end"] = new JsonObject { ["line"] = 11, ["character"] = 0 } },
        }))!["data"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray();
        Assert.All(Decode(text, range), t => Assert.Equal(10, Protocol.TextPositions.Position(text, t.Start).Line));
        Assert.NotEmpty(Decode(text, range));
    }

    [Fact]
    public async Task Strings_and_expressions_in_attribute_values_keep_their_colors()
    {
        var (path, text) = await OpenAsync("""
            <CheckBox IsChecked="@Item.IsCompleted" IsCheckedChanged="CompletedChanged" AccessibleName="@($"Completa {Item.Title}")"/>
            <TextBlock Tooltip="@($"{Item.Count} di {(Item.Done ? "fatto" : "da fare")}")" Class="riga">Fine</TextBlock>
            <Button Title="Ordine @Item.Count di @(Item.Total)" @onclick="@(() => Log("ok"))">Apri</Button>

            @code {
                public sealed class Row
                {
                    public bool IsCompleted { get; set; }
                    public string Title { get; set; } = "";
                    public int Count { get; set; }
                    public int Total { get; set; }
                    public bool Done { get; set; }
                }

                private Row Item = new();
                private void CompletedChanged(bool? value) { }
                private void Log(string message) { }
            }
            """, "Rows.nazor");
        var data = (await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } }))!["data"]!
            .AsArray().Select(n => n!.GetValue<int>()).ToArray();
        var tokens = Decode(text, data);
        string? TypeAt(string marker, int delta = 0, int occurrence = 0)
        {
            var offset = -1;
            for (var i = 0; i <= occurrence; i++) offset = text.IndexOf(marker, offset + 1, StringComparison.Ordinal);
            offset += delta;
            return tokens.FirstOrDefault(t => t.Start <= offset && offset < t.Start + t.Length).Type;
        }
        Assert.Equal("string", TypeAt("Completa"));
        Assert.Equal("field name", TypeAt("Item.Title"));
        Assert.Equal("property name", TypeAt("Title}"));
        Assert.Equal("markupAttributeQuote", TypeAt("\")\"/>", 2));
        Assert.Equal("markupTagDelimiter", TypeAt("/>"));
        Assert.Equal("string", TypeAt("\"fatto\""));
        Assert.Equal("property name", TypeAt("Done"));
        Assert.Equal("property name", TypeAt("Class="));
        Assert.Equal("markupAttributeValue", TypeAt("riga"));
        Assert.Null(TypeAt("Fine"));
        Assert.Equal("markupAttributeValue", TypeAt("Ordine"));
        Assert.Equal("razorTransition", TypeAt("@Item.Count di"));
        Assert.Equal("field name", TypeAt("@Item.Count di", 1));
        Assert.Equal("markupAttributeValue", TypeAt(" di @("));
        Assert.Equal("razorTransition", TypeAt("@(Item.Total"));
        Assert.Equal("property name", TypeAt("Total)"));
        Assert.Equal("method name", TypeAt("Log(\""));
        Assert.Equal("string", TypeAt("\"ok\""));
        Assert.Null(TypeAt("Apri"));
    }

    private const string Calls = """
        <TextBlock>@Format(count, "pz")</TextBlock>

        @code {
            private int count;

            /// <summary>The count with its unit.</summary>
            /// <param name="unit">What is counted.</param>
            private string Format(int value, string unit) => $"{value} {unit}";

            private string Format(int value) => value.ToString();

            private void Run()
            {
                var builder = new System.Text.StringBuilder(16);
                var list = new List<int> { 1 };
                var first = list[0];
                var named = Format(unit: "kg", value: 2);
            }
        }

        """;

    private async Task<JsonNode?> SignaturesAsync(string path, (int Line, int Character) at) =>
        await client.AtAsync("textDocument/signatureHelp", path, at.Line, at.Character);

    [Fact]
    public async Task Signature_help_shows_the_overloads_with_the_parameter_being_written()
    {
        var (path, text) = await OpenAsync(Calls, "Signatures.nazor");
        await client.DiagnosticsAsync(path);

        var help = (await SignaturesAsync(path, At(text, "@Format(count, ", "@Format(count, ".Length)))!;
        var signatures = help["signatures"]!.AsArray();
        Assert.Equal(["string Signatures.Format(int value)", "string Signatures.Format(int value, string unit)"],
            signatures.Select(s => s!["label"]!.GetValue<string>()));
        Assert.Equal(1, help["activeSignature"]!.GetValue<int>());
        Assert.Equal(1, help["activeParameter"]!.GetValue<int>());
        var active = signatures[1]!;
        Assert.Equal("The count with its unit.", active["documentation"]!["value"]!.GetValue<string>());
        var unit = active["parameters"]![1]!;
        var label = active["label"]!.GetValue<string>();
        Assert.Equal("string unit", label[unit["label"]![0]!.GetValue<int>()..unit["label"]![1]!.GetValue<int>()]);
        Assert.Equal("What is counted.", unit["documentation"]!["value"]!.GetValue<string>());

        help = (await SignaturesAsync(path, At(text, "value: 2", 0)))!;
        Assert.Equal(0, help["activeParameter"]!.GetValue<int>());
        help = (await SignaturesAsync(path, At(text, "unit: \"kg\"", 0)))!;
        Assert.Equal(1, help["activeParameter"]!.GetValue<int>());
    }

    [Fact]
    public async Task Signature_help_covers_constructors_and_indexers_of_libraries()
    {
        var (path, text) = await OpenAsync(Calls, "Signatures.nazor");
        await client.DiagnosticsAsync(path);

        var help = (await SignaturesAsync(path, At(text, "StringBuilder(16", "StringBuilder(".Length)))!;
        var labels = help["signatures"]!.AsArray().Select(s => s!["label"]!.GetValue<string>()).ToList();
        Assert.Contains("System.Text.StringBuilder()", labels);
        Assert.Equal("System.Text.StringBuilder(int capacity)", labels[help["activeSignature"]!.GetValue<int>()]);

        help = (await SignaturesAsync(path, At(text, "list[0]", "list[".Length)))!;
        Assert.Equal("int List<int>[int index]", Assert.Single(help["signatures"]!.AsArray())!["label"]!.GetValue<string>());

        Assert.Null(await SignaturesAsync(path, At(text, "Run()", "Run()".Length)));
        Assert.Null(await SignaturesAsync(path, At(text, "@Format", 1)));
    }

    [Fact]
    public async Task Stylesheets_have_no_semantic_tokens()
    {
        var path = Path.Combine(directory, "Theme.nss");
        await client.OpenAsync(path, ".a { color: red; }");
        var data = (await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) } }))!["data"]!.AsArray();
        Assert.Empty(data);
    }

    private List<(int Start, int Length, string Type)> Decode(string text, int[] data)
    {
        var tokens = new List<(int, int, string)>();
        var line = 0;
        var character = 0;
        for (var i = 0; i < data.Length; i += 5)
        {
            line += data[i];
            character = data[i] == 0 ? character + data[i + 1] : data[i + 1];
            tokens.Add((Protocol.TextPositions.Offset(text, new(line, character)), data[i + 2], legend[data[i + 3]]));
        }
        return tokens;
    }
}
