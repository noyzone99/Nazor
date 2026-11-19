using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nazor.LanguageServer.Features;

namespace Nazor.LanguageServer.Tests;

public sealed class CodeActionTests
{
    private const string Path = @"C:\App\Counter.nazor";

    private static readonly string Component = N("""
        @using MyApp.Services
        <Button @onclick="Increment">@count</Button>

        @style {
            Button { padding: 4px; }
        }

        @code {
            private int count;

            private void Increment() => count++;
        }

        """);

    private const string Generated = """
        using System;
        using Nazor;
        using MyApp.Services;
        namespace MyApp
        {
            public partial class Counter : global::Nazor.ComponentBase
            {
            }
        }
        """;

    private static string N(string text) => text.ReplaceLineEndings("\n");

    private static List<CodeActionResult> Actions(string text, string marker, Dictionary<string, string>? files = null)
    {
        var offset = text.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(offset >= 0, marker);
        offset = CodeActions.Blocks(text).FirstOrDefault(b => b.Start <= offset && offset <= b.Close)?.Start ?? offset;
        return CodeActions.For(Path, text, offset, offset, Generated, path => files?.GetValueOrDefault(path));
    }

    private static string Apply(CodeActionResult action, string path) => action.Changes.Single(c => c.Path == path).NewText!;

    [Fact]
    public void The_code_moves_to_a_new_partial_class_without_unnecessary_imports()
    {
        var action = Assert.Single(Actions(Component, "private int"));
        Assert.Equal("Move the code to Counter.nazor.cs", action.Title);
        Assert.Equal(N("""
            @using MyApp.Services
            <Button @onclick="Increment">@count</Button>

            @style {
                Button { padding: 4px; }
            }

            """), Apply(action, Path));
        var created = action.Changes.Single(c => c.Path == Path + ".cs");
        Assert.Null(created.OldText);
        Assert.Equal(N("""
            namespace MyApp;

            public partial class Counter
            {
                private int count;

                private void Increment() => count++;
            }

            """), created.NewText);
    }

    [Fact]
    public void The_code_goes_at_the_end_of_an_existing_partial_class()
    {
        const string existing = "namespace MyApp;\n\npublic partial class Counter\n{\n    private string Label => \"x\";\n}\n";
        var action = Assert.Single(Actions(Component, "Increment() =>", new() { [Path + ".cs"] = existing }), a => a.Title.StartsWith("Move the code to", StringComparison.Ordinal));
        Assert.Equal(N("""
            namespace MyApp;

            public partial class Counter
            {
                private string Label => "x";

                /* <nazor-moved> */
                private int count;

                private void Increment() => count++;
                /* </nazor-moved> */
            }

            """), Apply(action, Path + ".cs"));
    }

    [Fact]
    public void Generated_directives_do_not_travel_with_required_alias_static_or_namespace_imports()
    {
        const string source = "@using System.Collections.Generic\n<Button />\n@code {\n    private List<int> items = [];\n    private Id id;\n    private double Value => Sqrt(4);\n}\n";
        const string generated = "using System.Collections.Generic\n#nullable disable\n;\nusing Id = System.Guid\n#line default\n;\nusing static System.Math\n#nullable restore\n;\nusing System.Text;\nnamespace MyApp { public partial class Counter { } }";
        var compilation = CSharpCompilation.Create("Imports", references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var forward = Assert.Single(CodeActions.For(Path, source, source.IndexOf("@code", StringComparison.Ordinal), source.Length, generated, _ => null, compilation));
        var sidecar = Apply(forward, Path + ".cs");
        Assert.Contains("using System.Collections.Generic;", sidecar);
        Assert.Contains("using Id = System.Guid;", sidecar);
        Assert.Contains("using static System.Math;", sidecar);
        Assert.DoesNotContain("using System.Text", sidecar);
        Assert.DoesNotContain("#nullable", sidecar);
        Assert.DoesNotContain("#line", sidecar);
        Assert.Empty(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sidecar, cancellationToken: TestContext.Current.CancellationToken))
            .GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
        var parent = Apply(forward, Path);
        Assert.StartsWith("@using System.Collections.Generic\n", parent);
        var inverse = Assert.Single(CodeActions.For(Path, parent, 0, 0, generated, p => p == Path + ".cs" ? sidecar : null, compilation));
        Assert.Equal(source.Replace("@code {", "\n@code {", StringComparison.Ordinal), Apply(inverse, Path));
        Assert.Null(inverse.Changes.Single(c => c.Path == Path + ".cs").NewText);
    }

    [Theory]
    [InlineData(".cs", "namespace MyApp; public partial class Counter {\n}\n", "private int", true)]
    [InlineData(".cs", "// Keep this comment\nnamespace MyApp; public partial class Counter {\n}\n", "private int", false)]
    [InlineData(".nss", " \n\t", "padding", true)]
    [InlineData(".nss", "/* Keep this comment */\n", "padding", false)]
    public void Returning_a_transfer_removes_only_empty_sidecars(string suffix, string original, string marker, bool delete)
    {
        var forward = Actions(Component, marker, new() { [Path + suffix] = original }).Single(a => a.Title.Contains(" to ", StringComparison.Ordinal));
        var parent = Apply(forward, Path);
        var sidecar = Apply(forward, Path + suffix);
        var inverse = Assert.Single(CodeActions.For(Path, parent, 0, 0, Generated, p => p == Path + suffix ? sidecar : null));
        var remaining = inverse.Changes.Single(c => c.Path == Path + suffix).NewText;
        Assert.Equal(delete, remaining is null);
        if (!delete) Assert.Contains("Keep this comment", remaining);
    }

    [Fact]
    public void The_styles_move_to_the_stylesheet_of_the_component_after_its_rules()
    {
        var action = Assert.Single(Actions(Component, "padding"));
        Assert.Equal("Move the styles to Counter.nazor.nss", action.Title);
        Assert.DoesNotContain("@style", Apply(action, Path));
        Assert.Contains("@code {", Apply(action, Path));
        Assert.Equal("Button { padding: 4px; }\n", Apply(action, Path + ".nss"));
        var appended = Assert.Single(Actions(Component, "padding", new() { [Path + ".nss"] = ".a { gap: 1px; }\n" }), a => a.Title.StartsWith("Move the styles to", StringComparison.Ordinal));
        Assert.Equal(".a { gap: 1px; }\n\n/* <nazor-moved> */\nButton { padding: 4px; }\n/* </nazor-moved> */\n", Apply(appended, Path + ".nss"));
    }

    [Fact]
    public void Code_and_styles_come_back_into_the_component_and_their_files_go()
    {
        var moved = Apply(Assert.Single(Actions(Component, "private int")), Path);
        var codeBehind = Assert.Single(Actions(Component, "private int")).Changes.Single(c => c.Path == Path + ".cs").NewText!;
        var back = Assert.Single(CodeActions.For(Path, moved, 0, 0, Generated, p => p == Path + ".cs" ? codeBehind : null),
            a => a.Title == "Move the code of Counter.nazor.cs into the component");
        Assert.Equal(Component, Apply(back, Path));
        Assert.Null(back.Changes.Single(c => c.Path == Path + ".cs").NewText);

        const string behind = "using System.Globalization;\nusing MyApp.Services;\n\nnamespace MyApp;\n\npublic partial class Counter\n{\n    private CultureInfo culture = CultureInfo.InvariantCulture;\n}\n";
        var text = "@using MyApp.Services\n<Button>Go</Button>\n\n@style {\n    Button { gap: 2px; }\n}\n";
        var actions = CodeActions.For(Path, text, 0, 0, Generated, p => p == Path + ".cs" ? behind : p == Path + ".nss" ? ".a {\n    gap: 1px;\n}\n" : null);
        var code = Apply(actions.Single(a => a.Title.StartsWith("Move the code of", StringComparison.Ordinal)), Path);
        Assert.Equal("@using MyApp.Services\n@using System.Globalization\n<Button>Go</Button>\n\n@style {\n    Button { gap: 2px; }\n}\n\n@code {\n    private CultureInfo culture = CultureInfo.InvariantCulture;\n}\n", code);
        var styles = actions.Single(a => a.Title.StartsWith("Move the styles of", StringComparison.Ordinal));
        Assert.Equal("@using MyApp.Services\n<Button>Go</Button>\n\n@style {\n    .a {\n        gap: 1px;\n    }\n}\n\n@style {\n    Button { gap: 2px; }\n}\n", Apply(styles, Path));
        Assert.Null(styles.Changes.Single(c => c.Path == Path + ".nss").NewText);
    }

    [Fact]
    public void A_code_behind_with_more_than_the_partial_class_does_not_come_back()
    {
        const string behind = "namespace MyApp;\n\npublic partial class Counter\n{\n}\n\npublic sealed class Helper { }\n";
        Assert.DoesNotContain(CodeActions.For(Path, "<Button />\n", 0, 0, Generated, p => p == Path + ".cs" ? behind : null),
            a => a.Title.StartsWith("Move the code of", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("private int", ".cs", "namespace MyApp;\npublic partial class Counter\n{\n    private string Label => \"kept\";\n}\n", "Label =>", "private int count")]
    [InlineData("padding", ".nss", ".kept { color: red; }\n", ".kept", "Button { padding")]
    public void Returning_a_transfer_preserves_preexisting_sidecar_content(string marker, string suffix, string original, string kept, string transferred)
    {
        var forward = Actions(Component, marker, new() { [Path + suffix] = original }).Single(a => a.Title.StartsWith("Move the", StringComparison.Ordinal) && a.Title.Contains(" to ", StringComparison.Ordinal));
        var moved = Apply(forward, Path);
        var sidecar = Apply(forward, Path + suffix);
        var back = CodeActions.For(Path, moved, 0, 0, Generated, p => p == Path + suffix ? sidecar : null)
            .Single(a => a.Title.Contains(" into the component", StringComparison.Ordinal));
        Assert.Contains(transferred, Apply(back, Path));
        Assert.DoesNotContain(kept, Apply(back, Path));
        Assert.Contains(kept, Apply(back, Path + suffix));
        Assert.DoesNotContain(transferred, Apply(back, Path + suffix));
        Assert.DoesNotContain("nazor-moved", Apply(back, Path + suffix));
    }

    [Theory]
    [InlineData("namespace MyApp; public class Other { }")]
    [InlineData("namespace Other; public partial class Counter { }")]
    [InlineData("this is not C#")]
    public void Moving_code_never_overwrites_an_unrelated_or_invalid_existing_sidecar(string existing)
    {
        Assert.DoesNotContain(Actions(Component, "private int", new() { [Path + ".cs"] = existing }), a => a.Title.Contains(" to Counter.nazor.cs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\r\n   ")]
    public void Moving_code_reuses_an_empty_sidecar_left_by_the_VisualStudio_inverse(string existing)
    {
        var action = Actions(Component, "private int", new() { [Path + ".cs"] = existing })
            .Single(a => a.Title == "Move the code to Counter.nazor.cs");
        var sidecar = Assert.Single(action.Changes, c => c.Path == Path + ".cs");
        Assert.Equal(existing, sidecar.OldText);
        Assert.Contains("private int count;", sidecar.NewText);
        Assert.Contains("partial class Counter", sidecar.NewText);
    }

    [Fact]
    public void Style_moves_preserve_the_cascade_of_multiple_inline_blocks()
    {
        const string text = "<Button />\n@style { Button { color: red; } }\n@style { Button { color: blue; } }\n@style { Button { color: green; } }\n";
        var forward = Actions(text, "blue").Single();
        var sheet = Apply(forward, Path + ".nss");
        Assert.True(sheet.IndexOf("red", StringComparison.Ordinal) < sheet.IndexOf("blue", StringComparison.Ordinal));
        var remaining = Apply(forward, Path);
        Assert.DoesNotContain("red", remaining);
        Assert.DoesNotContain("blue", remaining);
        Assert.Contains("green", remaining);
        var back = CodeActions.For(Path, remaining, 0, 0, Generated, p => p == Path + ".nss" ? sheet : null).Single();
        Assert.True(Apply(back, Path).IndexOf("blue", StringComparison.Ordinal) < Apply(back, Path).IndexOf("green", StringComparison.Ordinal));
    }
}

public sealed class CodeActionServerTests : IAsyncLifetime
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-actions-").FullName;
    private LspClient client = null!;

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(System.IO.Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        client = LspClient.InProcess();
        var result = await client.InitializeAsVisualStudioAsync();
        Assert.Equal("refactor.move", result!["capabilities"]!["codeActionProvider"]!["codeActionKinds"]![0]!.GetValue<string>());
        Assert.Equal("nazor.deleteEmptySidecars", result["capabilities"]!["executeCommandProvider"]!["commands"]![0]!.GetValue<string>());
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    [Fact]
    public async Task A_style_block_offers_its_move_as_a_workspace_edit()
    {
        var path = System.IO.Path.Combine(directory, "Panel.nazor");
        const string text = "<Container />\n\n@style {\n    Container { gap: 4px; }\n}\n";
        await client.OpenAsync(path, text);
        var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["range"] = new JsonObject { ["start"] = new JsonObject { ["line"] = 2, ["character"] = 1 }, ["end"] = new JsonObject { ["line"] = 2, ["character"] = 1 } },
            ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() },
        }))!.AsArray();
        var action = Assert.Single(actions)!;
        Assert.Equal("Move the styles to Panel.nazor.nss", action["title"]!.GetValue<string>());
        var changes = action["edit"]!["documentChanges"]!.AsArray();
        Assert.Equal("create", changes.Single(c => c!["kind"] is not null)!["kind"]!.GetValue<string>());
        var created = changes.Single(c => c!["textDocument"]?["uri"]?.GetValue<string>().EndsWith(".nss", StringComparison.Ordinal) == true)!;
        Assert.Equal("Container { gap: 4px; }\n", created["edits"]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task Whole_elements_are_extracted_with_the_members_they_use_as_parameters()
    {
        var path = System.IO.Path.Combine(directory, "Orders.nazor");
        var text = """
            <Container>
                <TextBlock>@title</TextBlock>
                <Button OnClick="Increment">@count</Button>
            </Container>

            @code {
                private string title = "Ordini";
                private int count;
                private void Increment() => count++;
            }

            """.ReplaceLineEndings("\n");
        await client.OpenAsync(path, text);
        await client.DiagnosticsAsync(path, d => d.Count == 0);

        var action = (await ExtractAsync(path, 2, 0, 3, 0))!;
        Assert.Equal("Extract component ExtractedComponent", action["title"]!.GetValue<string>());
        Assert.Equal("refactor.extract", action["kind"]!.GetValue<string>());
        var (parent, extracted) = NewTexts(action, path);
        Assert.Equal("""
            <Button OnClick="Increment">@count</Button>

            @code {
                [Parameter] public EventCallback Increment { get; set; }
                [Parameter] public int count { get; set; } = default!;
            }

            """.ReplaceLineEndings("\n"), extracted.Text);
        Assert.Contains("    <ExtractedComponent Increment=\"Increment\" count=\"@count\" />\n</Container>", parent, StringComparison.Ordinal);

        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, parent, 2);
        await client.OpenAsync(extracted.Path, extracted.Text);
        var extractedDiagnostics = await client.DiagnosticsAsync(extracted.Path);
        Assert.True(extractedDiagnostics.Count == 0, extracted.Text + "\n" + extractedDiagnostics.ToJsonString());
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Fact]
    public async Task The_variable_of_a_foreach_around_the_selection_becomes_a_parameter()
    {
        var path = System.IO.Path.Combine(directory, "List.nazor");
        var text = """
            @using System.Collections.Generic

            <Container>
                @foreach (var order in orders)
                {
                    <TextBlock>@order.ToUpperInvariant()</TextBlock>
                }
            </Container>

            @code {
                private List<string> orders = [];
            }

            """.ReplaceLineEndings("\n");
        await client.OpenAsync(path, text);
        File.WriteAllText(System.IO.Path.Combine(directory, "ExtractedComponent.nazor"), "<Container />\n");

        var action = (await ExtractAsync(path, 5, 0, 6, 0))!;
        Assert.Equal("Extract component ExtractedComponent2", action["title"]!.GetValue<string>());
        var (parent, extracted) = NewTexts(action, path);
        Assert.Equal("""
            @using System.Collections.Generic

            <TextBlock>@order.ToUpperInvariant()</TextBlock>

            @code {
                [Parameter] public string order { get; set; } = default!;
            }

            """.ReplaceLineEndings("\n"), extracted.Text);
        Assert.Contains("        <ExtractedComponent2 order=\"@order\" />\n", parent, StringComparison.Ordinal);

        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, parent, 2);
        await client.OpenAsync(extracted.Path, extracted.Text);
        var extractedDiagnostics = await client.DiagnosticsAsync(extracted.Path);
        Assert.True(extractedDiagnostics.Count == 0, extracted.Text + "\n" + extractedDiagnostics.ToJsonString());
        var parentDiagnostics = await client.DiagnosticsAsync(path);
        Assert.True(parentDiagnostics.Count == 0, parentDiagnostics.ToJsonString());
    }

    [Fact]
    public async Task The_rules_that_select_the_extracted_elements_go_with_them()
    {
        var path = System.IO.Path.Combine(directory, "Styled.nazor");
        var text = """
            <Container Class="orders">
                <TextBlock Class="title">@title</TextBlock>
                <Button Class="primary">Salva</Button>
            </Container>

            @style {
                .orders { gap: 4px; }

                .title { font-size: 20px; }

                TextBlock, Button { margin: 2px; }

                Button.primary {
                    background: #0078d4;
                    animation: pulse 1s;
                }

                @keyframes pulse {
                    from { opacity: 0.5; }
                    to { opacity: 1; }
                }
            }

            @code {
                private string title = "Ordini";
            }

            """.ReplaceLineEndings("\n");
        await client.OpenAsync(path, text);
        await client.DiagnosticsAsync(path, d => d.Count == 0);

        var (parent, extracted) = NewTexts((await ExtractAsync(path, 2, 0, 3, 0))!, path);
        Assert.Equal("""
            <Button Class="primary">Salva</Button>

            @style {
                TextBlock, Button { margin: 2px; }

                Button.primary {
                    background: #0078d4;
                    animation: pulse 1s;
                }

                @keyframes pulse {
                    from { opacity: 0.5; }
                    to { opacity: 1; }
                }
            }

            """.ReplaceLineEndings("\n"), extracted.Text);
        Assert.Equal("""
            <Container Class="orders">
                <TextBlock Class="title">@title</TextBlock>
                <ExtractedComponent />
            </Container>

            @style {
                .orders { gap: 4px; }

                .title { font-size: 20px; }

                TextBlock, Button { margin: 2px; }
            }

            @code {
                private string title = "Ordini";
            }

            """.ReplaceLineEndings("\n"), parent);

        client.ForgetDiagnostics(path);
        await client.ChangeAsync(path, parent, 2);
        await client.OpenAsync(extracted.Path, extracted.Text);
        Assert.Empty(await client.DiagnosticsAsync(extracted.Path));
        Assert.Empty(await client.DiagnosticsAsync(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_class_written_in_CSharp_takes_every_class_rule_and_the_emptied_sheet_is_cleared_or_deleted(bool visualStudio)
    {
        if (!visualStudio) await client.InitializeAsync();
        var path = System.IO.Path.Combine(directory, "Lamp.nazor");
        var text = """
            <Container>
                <TextBlock Class="@(active ? "on" : "off")">Stato</TextBlock>
            </Container>

            @style {
                .on { color: green; }
            }

            @code {
                private bool active;
            }

            """.ReplaceLineEndings("\n");
        File.WriteAllText(path + ".nss", ".off { color: gray; }\n");
        await client.OpenAsync(path, text);

        var action = (await ExtractAsync(path, 1, 0, 2, 0))!;
        var (parent, extracted) = NewTexts(action, path);
        Assert.Equal("""
            <TextBlock Class="@(active ? "on" : "off")">Stato</TextBlock>

            @style {
                .off { color: gray; }

                .on { color: green; }
            }

            @code {
                [Parameter] public bool active { get; set; } = default!;
            }

            """.ReplaceLineEndings("\n"), extracted.Text);
        Assert.Equal("""
            <Container>
                <ExtractedComponent active="@active" />
            </Container>

            @code {
                private bool active;
            }

            """.ReplaceLineEndings("\n"), parent);
        var changes = action["edit"]!["documentChanges"]!.AsArray();
        var clear = Assert.Single(changes, c => c!["textDocument"]?["uri"]?.GetValue<string>() == LspClient.Uri(path + ".nss"))!;
        Assert.Equal("", clear["edits"]![0]!["newText"]!.GetValue<string>());
        if (visualStudio) Assert.DoesNotContain(changes, c => c!["kind"]?.GetValue<string>() == "delete");
        else Assert.Equal(LspClient.Uri(path + ".nss"), Assert.Single(changes, c => c!["kind"]?.GetValue<string>() == "delete")!["uri"]!.GetValue<string>());
    }

    [Fact]
    public async Task Part_of_an_element_is_not_extracted()
    {
        var path = System.IO.Path.Combine(directory, "Half.nazor");
        await client.OpenAsync(path, "<Container>\n    <TextBlock>Uno</TextBlock>\n</Container>\n");
        Assert.Null(await ExtractAsync(path, 0, 0, 1, 30));
        Assert.Null(await ExtractAsync(path, 1, 4, 1, 15));
    }

    private async Task<JsonNode?> ExtractAsync(string path, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        var actions = (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
            ["range"] = new JsonObject
            {
                ["start"] = new JsonObject { ["line"] = startLine, ["character"] = startCharacter },
                ["end"] = new JsonObject { ["line"] = endLine, ["character"] = endCharacter },
            },
            ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() },
        }))!.AsArray();
        return actions.FirstOrDefault(a => a!["kind"]!.GetValue<string>() == "refactor.extract");
    }

    private static (string Parent, (string Path, string Text) Extracted) NewTexts(JsonNode action, string path)
    {
        var edits = action["edit"]!["documentChanges"]!.AsArray().Where(c => c!["textDocument"] is not null)
            .ToDictionary(c => c!["textDocument"]!["uri"]!.GetValue<string>(), c => c!["edits"]![0]!["newText"]!.GetValue<string>());
        var parent = edits[LspClient.Uri(path)];
        var (uri, text) = edits.Single(e => e.Key != LspClient.Uri(path) && e.Key.EndsWith(".nazor", StringComparison.Ordinal));
        return (parent, (new Uri(uri).LocalPath, text));
    }
}
