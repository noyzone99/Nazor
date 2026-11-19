using System.Text.Json.Nodes;
using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Tests;

public sealed class IdeBacklogTests : IAsyncLifetime
{
    private readonly string directory = Directory.CreateTempSubdirectory("nazor-ide-backlog-").FullName;
    private readonly LspClient client = LspClient.InProcess();
    private string[] legend = [];
    private string Page => Path.Combine(directory, "Page.nazor");

    public async ValueTask InitializeAsync()
    {
        File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var initialized = await client.InitializeAsVisualStudioAsync();
        legend = [.. initialized!["capabilities"]!["semanticTokensProvider"]!["legend"]!["tokenTypes"]!.AsArray().Select(t => t!.GetValue<string>())];
    }

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await LspClient.DeleteDirectoryAsync(directory);
    }

    private async Task<JsonArray> Actions(string text, string marker)
    {
        var at = TextPositions.Position(text, text.IndexOf(marker, StringComparison.Ordinal));
        return (await client.RequestAsync("textDocument/codeAction", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Page) },
            ["range"] = new JsonObject
            {
                ["start"] = new JsonObject { ["line"] = at.Line, ["character"] = at.Character },
                ["end"] = new JsonObject { ["line"] = at.Line, ["character"] = at.Character },
            },
            ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() },
        }))!.AsArray();
    }

    [Theory]
    [InlineData(" />")]
    [InlineData("></Clock>")]
    public async Task Editor_classifies_only_real_parameters_and_reports_unknown_and_empty_values(string ending)
    {
        await client.OpenAsync(Path.Combine(directory, "Clock.nazor"), "<TextBlock />\n@code { [Parameter] public DateTime InitialDate { get; set; } [Parameter] public string Text { get; set; } = \"\"; }");
        var source = "<Clock InitialDate=\"\" param1=\"\" Text=\"\"" + ending;
        await client.OpenAsync(Page, source);
        var diagnostics = await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "NZC014"));
        Assert.Contains(diagnostics, d => d!["code"]!.GetValue<string>() == "NZC015");
        var spans = (await client.RequestAsync("nazor/componentParameters", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Page) }, ["text"] = source,
        }))!.AsArray();
        var actual = spans.ToDictionary(s => source.Substring(s!["start"]!.GetValue<int>(), s["length"]!.GetValue<int>()), s => s!["known"]!.GetValue<bool>());
        Assert.Equal(new Dictionary<string, bool> { ["InitialDate"] = true, ["param1"] = false, ["Text"] = true }, actual);
        client.ForgetDiagnostics(Page);
        const string valid = "<Clock InitialDate=\"DateTime.Now\" Text=\"\" />";
        await client.ChangeAsync(Page, valid, 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Page), d => d!["code"]!.GetValue<string>() is "NZC014" or "NZC015");
    }

    [Theory]
    [InlineData("DateTime", " />")]
    [InlineData("string", "></Clock>")]
    [InlineData("int", " />")]
    [InlineData("bool?", " />")]
    public async Task Only_boolean_parameters_can_omit_the_assignment(string type, string ending)
    {
        await client.OpenAsync(Path.Combine(directory, "Clock.nazor"), "<TextBlock />\n@code { [Parameter] public " + type + " InitialDate { get; set; } [Parameter] public bool Enabled { get; set; } }");
        var source = "<Clock InitialDate" + ending;
        await client.OpenAsync(Page, source);
        var diagnostics = await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "NZC016"));
        var error = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "NZC016")!;
        Assert.Equal(1, error["severity"]!.GetValue<int>());
        Assert.Equal(7, error["range"]!["start"]!["character"]!.GetValue<int>());
        Assert.Equal(18, error["range"]!["end"]!["character"]!.GetValue<int>());
        client.ForgetDiagnostics(Page);
        await client.ChangeAsync(Page, "<Clock Enabled />", 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Page), d => d!["severity"]!.GetValue<int>() == 1);
    }

    [Theory]
    [InlineData("code", "private int value;")]
    [InlineData("style", "TextBlock { color: red; }")]
    public void Move_hints_are_limited_to_the_directive_text_and_outer_braces(string kind, string body)
    {
        var text = "<TextBlock />\n@" + kind + "  {\n    " + body + "\n}\n";
        var block = Assert.Single(CodeActions.Blocks(text));
        const string generated = "namespace Demo { public partial class Page { } }";
        for (var at = 0; at < text.Length; at++)
        {
            var offered = CodeActions.For(Page, text, at, at, generated, _ => null).Any(a => a.Title.Contains(" to ", StringComparison.Ordinal));
            Assert.Equal(at >= block.Start && at < block.Start + kind.Length + 1 || at == block.Open || at == block.Close, offered);
        }
    }

    [Theory]
    [InlineData("<Card />")]
    [InlineData("<Card></Card>")]
    public async Task Required_parameters_have_source_diagnostics_and_quick_fixes(string markup)
    {
        await client.OpenAsync(Path.Combine(directory, "Card.nazor"), "<TextBlock />\n@code { [Parameter, EditorRequired] public string Title { get; set; } = \"\"; }");
        await client.OpenAsync(Page, markup);
        var diagnostics = await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "RZ2012"));
        var required = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "RZ2012")!;
        Assert.Contains("Title", required["message"]!.GetValue<string>());
        Assert.Equal(0, required["range"]!["start"]!["line"]!.GetValue<int>());
        var fix = Assert.Single(await Actions(markup, "Card"), a => a!["title"]!.GetValue<string>() == "Add required parameter Title")!;
        Assert.Equal("quickfix", fix["kind"]!.GetValue<string>());
        var updated = fix["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>();
        client.ForgetDiagnostics(Page);
        await client.ChangeAsync(Page, updated, 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Page), d => d!["code"]!.GetValue<string>() == "RZ2012");
    }

    [Theory]
    [InlineData("string", "", " />")]
    [InlineData("int", "@(default!)", " />")]
    [InlineData("DateTime", "@(default!)", "></Card>")]
    [InlineData("bool", "@(default!)", " />")]
    [InlineData("int?", "@(default!)", "></Card>")]
    [InlineData("DayOfWeek", "@(default!)", " />")]
    [InlineData("EventCallback", "@(default(global::Nazor.EventCallback))", "></Card>")]
    [InlineData("EventCallback<int>", "@(default(global::Nazor.EventCallback))", " />")]
    [InlineData("RenderFragment", "@(default!)", " />")]
    [InlineData("RenderFragment<int>", "@(default!)", "></Card>")]
    [InlineData("Action", "@(default!)", " />")]
    public async Task Required_parameter_fixes_do_not_introduce_empty_value_or_csharp_errors(string type, string value, string ending)
    {
        var card = Path.Combine(directory, "Card.nazor");
        var declaration = "@code { [Parameter, EditorRequired] public " + type + " Value { get; set; } = default!; }";
        await client.OpenAsync(card, declaration);
        var source = "<Card" + ending;
        await client.OpenAsync(Page, source);
        await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "RZ2012"));
        var action = Assert.Single(await Actions(source, "Card"), a => a!["title"]!.GetValue<string>() == "Add required parameter Value")!;
        var updated = action["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>();
        Assert.Contains("Value=\"" + value + "\"", updated);
        client.ForgetDiagnostics(Page);
        await client.ChangeAsync(Page, updated, 2);
        var diagnostics = await client.DiagnosticsAsync(Page);
        Assert.True(!diagnostics.Any(d => d!["severity"]!.GetValue<int>() == 1), diagnostics.ToJsonString());
        var compiler = new NazorCompiler(new()
        {
            ProjectDirectory = directory,
            References = [typeof(ComponentBase).Assembly.Location],
            SourceText = path => path == card ? declaration : path == Page ? updated : null,
        });
        compiler.Emit(compiler.Generate([card, Page]), "RequiredParameterFix");
    }

    [Theory]
    [InlineData("int")]
    [InlineData("string")]
    public async Task Required_parameter_fix_compiles_with_an_explicit_generic_type(string type)
    {
        var card = Path.Combine(directory, "Card.nazor");
        const string declaration = "@typeparam T\n@code { [Parameter, EditorRequired] public T Value { get; set; } = default!; }";
        await client.OpenAsync(card, declaration);
        var source = "<Card T=\"" + type + "\" />";
        await client.OpenAsync(Page, source);
        await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "RZ2012"));
        var action = Assert.Single(await Actions(source, "Card"), a => a!["title"]!.GetValue<string>() == "Add required parameter Value")!;
        var updated = action["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>();
        client.ForgetDiagnostics(Page);
        await client.ChangeAsync(Page, updated, 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Page), d => d!["severity"]!.GetValue<int>() == 1);
        var compiler = new NazorCompiler(new()
        {
            ProjectDirectory = directory,
            References = [typeof(ComponentBase).Assembly.Location],
            SourceText = path => path == card ? declaration : path == Page ? updated : null,
        });
        compiler.Emit(compiler.Generate([card, Page]), "GenericRequiredParameterFix");
    }

    [Fact]
    public async Task Missing_component_import_can_be_explicit_or_qualify_both_tags()
    {
        await client.OpenAsync(Path.Combine(directory, "Card.nazor"), "@namespace Widgets\n<TextBlock />");
        const string text = "<Card></Card>";
        await client.OpenAsync(Page, text);
        var actions = await Actions(text, "Card");
        Assert.DoesNotContain(actions, a => a!["title"]!.GetValue<string>() == "Use self-closing tag");
        Assert.Contains(actions, a => a!["title"]!.GetValue<string>() == "Add @using Widgets");
        var qualify = Assert.Single(actions, a => a!["title"]!.GetValue<string>() == "Use Widgets.Card")!;
        Assert.Equal("<Widgets.Card></Widgets.Card>", qualify["edit"]!["documentChanges"]![0]!["edits"]![0]!["newText"]!.GetValue<string>());
    }

    [Fact]
    public async Task Classification_follows_import_resolution_and_parameter_names()
    {
        await client.OpenAsync(Path.Combine(directory, "Card.nazor"), "@namespace Widgets\n<TextBlock />\n@code { [Parameter] public string Title { get; set; } = \"\"; }");
        const string text = "<Card Title=\"x\" Other=\"y\"></Card>\n<div />";
        await client.OpenAsync(Page, text);
        await client.DiagnosticsAsync(Page, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "RZ10012"));
        async Task<string?> TypeAt(string source, int offset)
        {
            var response = await client.RequestAsync("textDocument/semanticTokens/full", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(Page) },
            });
            var data = response!["data"]!.AsArray().Select(d => d!.GetValue<int>()).ToArray();
            var line = 0;
            var column = 0;
            for (var i = 0; i < data.Length; i += 5)
            {
                column = data[i] == 0 ? column + data[i + 1] : data[i + 1];
                line += data[i];
                var start = TextPositions.Offset(source, new(line, column));
                if (start <= offset && offset < start + data[i + 2]) return legend[data[i + 3]];
            }
            return null;
        }
        Assert.Equal(SemanticTokens.UnknownComponent, await TypeAt(text, 1));
        Assert.Equal(SemanticTokens.UnknownComponent, await TypeAt(text, text.IndexOf("</Card", StringComparison.Ordinal) + 2));
        var catalog = new NazorCatalog([new("Card", "Widgets.Card", "Widgets", null, [new("Title", "string", NazorParameterKind.Value, null)])], []);
        var unknown = SemanticTokens.Markup(text, catalog, _ => false, _ => true);
        Assert.Contains(unknown, t => t.Start == 1 && t.Type == SemanticTokens.UnknownComponent);
        client.ForgetDiagnostics(Page);
        await client.ChangeAsync(Page, "@using Widgets\n" + text, 2);
        Assert.DoesNotContain(await client.DiagnosticsAsync(Page), d => d!["code"]!.GetValue<string>() == "RZ10012");
        var imported = "@using Widgets\n" + text;
        Assert.Equal(SemanticTokens.ComponentElement, await TypeAt(imported, imported.IndexOf("<Card", StringComparison.Ordinal) + 1));
        Assert.Equal(SemanticTokens.ComponentParameter, await TypeAt(imported, imported.IndexOf("Title", StringComparison.Ordinal)));
        var valid = SemanticTokens.Markup(text, catalog, _ => false);
        Assert.Contains(valid, t => t.Start == 1 && t.Type == SemanticTokens.ComponentElement);
        Assert.Contains(valid, t => t.Start == text.IndexOf("Title", StringComparison.Ordinal) && t.Type == SemanticTokens.ComponentParameter);
        Assert.Contains(valid, t => t.Start == text.IndexOf("Other", StringComparison.Ordinal) && t.Type == SemanticTokens.UnknownParameter);
        Assert.Contains(valid, t => t.Start == text.IndexOf("div", StringComparison.Ordinal) && t.Type == SemanticTokens.Element);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Formatting_edits_preserve_all_code_tokens_and_reconstruct_the_result(string newline)
    {
        var before = "<TextBlock/>\n@code {\nvoid Run(){var value=1;value++;}\n}\n".ReplaceLineEndings(newline);
        var after = NazorFormatter.Format(before);
        var edits = TextEdits.Formatting(before, after);
        Assert.NotEmpty(edits);
        foreach (var edit in edits)
        {
            var start = TextPositions.Offset(before, edit.Range.Start);
            var end = TextPositions.Offset(before, edit.Range.End);
            Assert.True(string.IsNullOrWhiteSpace(before[start..end]));
            Assert.True(string.IsNullOrWhiteSpace(edit.NewText));
        }
        var result = before;
        foreach (var edit in edits.OrderByDescending(e => TextPositions.Offset(before, e.Range.Start)))
            result = result[..TextPositions.Offset(before, edit.Range.Start)] + edit.NewText + result[TextPositions.Offset(before, edit.Range.End)..];
        Assert.Equal(after, result);
        Assert.Empty(TextEdits.Formatting(after, after));
    }

    [Theory]
    [InlineData("<Icon A=\"x\"></Icon>", true)]
    [InlineData("<Icon> </Icon>", false)]
    [InlineData("<Icon>@*keep*@</Icon>", false)]
    [InlineData("<Icon><Icon /></Icon>", false)]
    [InlineData("<Missing></Missing>", false)]
    [InlineData("<!-- <Icon></Icon> -->", false)]
    public void Self_closing_never_removes_content(string text, bool offered)
    {
        var catalog = new NazorCatalog([new("Icon", "Demo.Icon", "Demo", null, [])], []);
        var actions = MarkupActions.For(Page, text, 1, 1, catalog, _ => null);
        Assert.Equal(offered, actions.Any(a => a.Title == "Use self-closing tag"));
        if (offered) Assert.Equal("<Icon A=\"x\" />", actions.Single().Changes.Single().NewText);
    }

    [Fact]
    public void Imports_move_without_duplicates_and_comments_in_blocks_stay()
    {
        const string text = "@using System.Text\n@using System.Globalization\n@*\n@using Keep.Comment\n*@\n<TextBlock />";
        const string existing = "@using System.Text\n";
        var action = Assert.Single(MarkupActions.For(Page, text, 2, 2, NazorCatalog.Empty, _ => existing));
        Assert.Equal(existing + "@using System.Globalization\n", action.Changes[0].NewText);
        Assert.Contains("@using Keep.Comment", action.Changes[1].NewText);
        Assert.DoesNotContain("@using System.Text", action.Changes[1].NewText);
        Assert.Empty(MarkupActions.For(Path.Combine(directory, "_Imports.nazor"), text, 2, 2, NazorCatalog.Empty, _ => null));
        var commented = Assert.Single(MarkupActions.For(Page, "@using System.Text\n", 2, 2, NazorCatalog.Empty, _ => "@*\n@using System.Text\n*@\n"));
        Assert.EndsWith("*@\n@using System.Text\n", commented.Changes[0].NewText);
    }

    [Theory]
    [InlineData("@code", "code")]
    [InlineData("@style", "style")]
    [InlineData("@if", "if")]
    public async Task Directive_shortcuts_expand_through_the_editor_protocol(string text, string expected)
    {
        await client.OpenAsync(Page, text);
        var snippet = await client.AtAsync("nazor/snippet", Page, 0, text.Length);
        Assert.StartsWith(expected, snippet!["snippet"]!.GetValue<string>());
        Assert.Contains("$0", snippet["snippet"]!.GetValue<string>());
        Assert.Equal(1, snippet["range"]!["start"]!["character"]!.GetValue<int>());
    }

    [Fact]
    public void Surround_preserves_selection_and_excludes_literals_and_partial_statements()
    {
        const string markup = "<TextBlock>@($\"{value}\")</TextBlock>";
        var wrappers = EditorSnippets.Surround(markup, 0, markup.Length, csharp: false);
        Assert.Equal(2, wrappers.Count);
        Assert.Contains("${1:Container}", wrappers[0].Edit.NewText);
        Assert.Contains("\\$", wrappers[0].Edit.NewText);
        const string text = "@code { void Run() { Console.WriteLine(\"x;\"); value++; } }";
        var start = text.IndexOf("Console", StringComparison.Ordinal);
        var end = text.IndexOf("value++;", StringComparison.Ordinal) + "value++;".Length;
        Assert.Equal(2, EditorSnippets.Surround(text, start, end, csharp: true).Count);
        Assert.Empty(EditorSnippets.Surround(text, text.IndexOf("x;", StringComparison.Ordinal), text.IndexOf("x;", StringComparison.Ordinal) + 2, csharp: true));
        Assert.Empty(EditorSnippets.Surround(text, start + 1, end, csharp: true));
        Assert.Equal(2, EditorSnippets.Surround("  " + markup + "\n", 0, markup.Length + 3, csharp: false).Count);
    }

    [Fact]
    public void Moving_code_is_not_offered_from_markup_or_for_an_unrelated_partial_class()
    {
        const string text = "<TextBlock />\n@code { private int value; }";
        const string generated = "namespace Demo { public partial class Page { } }";
        Assert.DoesNotContain(CodeActions.For(Page, text, 1, text.Length, generated, _ => null), a => a.Title.StartsWith("Move the code", StringComparison.Ordinal));
        Assert.DoesNotContain(CodeActions.For(Page, text, 1, 1, generated, path => path.EndsWith(".cs", StringComparison.Ordinal) ? "namespace Demo; public partial class Other { private int unrelated; }" : null),
            a => a.Title.Contains("into the component", StringComparison.Ordinal));
        var inside = text.IndexOf("private", StringComparison.Ordinal);
        Assert.DoesNotContain(CodeActions.For(Page, text, inside, inside, generated, _ => null), a => a.Title.StartsWith("Move the code", StringComparison.Ordinal));
        foreach (var at in new[] { text.IndexOf("@code", StringComparison.Ordinal), text.IndexOf('{'), text.LastIndexOf('}') })
            Assert.Contains(CodeActions.For(Page, text, at, at, generated, _ => null), a => a.Title.StartsWith("Move the code", StringComparison.Ordinal));
    }

    [Fact]
    public void The_visual_studio_template_generates_a_component_with_matching_class_name()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var folder = Path.Combine(root, "Sources/Tools/Nazor.VisualStudio/ItemTemplates/NazorComponent");
        var template = System.Xml.Linq.XDocument.Load(Path.Combine(folder, "Component.vstemplate"));
        System.Xml.Linq.XNamespace schema = "http://schemas.microsoft.com/developer/vstemplate/2005";
        Assert.Equal("Component.nazor", template.Descendants(schema + "DefaultName").Single().Value);
        Assert.Equal("$fileinputname$.nazor", template.Descendants(schema + "ProjectItem").Single().Attribute("TargetFileName")!.Value);
        var text = File.ReadAllText(Path.Combine(folder, "Component.nazor")).Replace("$safeitemname$", "NewCard", StringComparison.Ordinal);
        var path = Path.Combine(directory, "NewCard.nazor");
        var analysis = new NazorCompiler(new() { ProjectDirectory = directory, RootNamespace = "Demo", SourceText = p => p == path ? text : null,
            References = [typeof(ComponentBase).Assembly.Location, typeof(Nazor.Controls.Container).Assembly.Location, typeof(Nazor.Style.StyleSheet).Assembly.Location] }).Analyze([path]);
        Assert.DoesNotContain(analysis.Diagnostics, d => d.IsError);
        Assert.Contains("partial class NewCard", Assert.Single(analysis.Components).CSharp);
    }
}
