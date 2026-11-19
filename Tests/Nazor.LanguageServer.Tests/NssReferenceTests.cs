using System.Text.Json.Nodes;
using Nazor.Compiler;
using Nazor.LanguageServer.Features;

namespace Nazor.LanguageServer.Tests;

public sealed class NssReferenceTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "nazor-static-references");
    private static readonly NazorCatalog Catalog = new(
        [.. new[] { "Container", "Button", "TextBlock", "TextField", "TextArea", "PasswordField", "NumberField", "Select", "SelectItem" }.Select(n => new NazorComponentInfo(n, "Nazor.Controls." + n, "Nazor.Controls", null, []))], []);
    private static NssReferenceDocument Document(string name, string text) => new(Path.Combine(Root, name), text);
    private static IReadOnlyList<NssReferenceRule> Query(string styles, params NssReferenceDocument[] sources) =>
        NssReferences.Query(Path.Combine(Root, "Page.nazor.nss"), styles, sources, Catalog);

    [Theory]
    [InlineData("<TextField />", "TextField", 1)]
    [InlineData("<TextField />", "TextBox", 0)]
    [InlineData("<PasswordField />", "PasswordField", 1)]
    [InlineData("<TextArea />", "TextArea", 1)]
    [InlineData("<NumberField />", "NumberField", 1)]
    [InlineData("<Select />", "Select", 1)]
    public void Controls_reference_their_current_root_selectors(string markup, string selector, int count)
    {
        var rule = Assert.Single(Query(selector + " { color: red; }", Document("Page.nazor", markup)));
        Assert.Equal(count, rule.References.Count);
        Assert.False(rule.Partial);
    }

    [Theory]
    [InlineData(":root")]
    [InlineData(":root:hover")]
    [InlineData(":root[role=window]")]
    [InlineData(":is(:root)")]
    [InlineData(":where(:root)")]
    [InlineData(":root, :root:focus")]
    public void Root_forms_reference_only_actual_window_directives(string selector)
    {
        var page = Document("Page.nazor", "@window\n<Container><Button /></Container>");
        var rule = Assert.Single(Query(selector + " { opacity: 1; }", page));
        Assert.Equal("@window", Assert.Single(rule.References).Preview);
        Assert.False(rule.Partial);
        var plain = Document("Page.nazor", "<Container><Button /></Container>");
        var empty = Assert.Single(Query(selector + " { opacity: 1; }", plain));
        Assert.Empty(empty.References);
        Assert.False(empty.Partial);
    }

    [Fact]
    public void Scoped_and_inline_window_selectors_navigate_to_the_owning_directive()
    {
        var page = Document("Page.nazor", "@using Nazor\r\n  @window (string Title)\r\n<Container><Button /></Container>\r\n@style { :root:hover, :root { opacity: 1; } }");
        var other = Document("Other.nazor", "@window\n<Button />");
        foreach (var rule in new[] {
            Assert.Single(Query(":root:hover, :root { opacity: 1; }", page, other)),
            Assert.Single(NssReferences.Query(page.Path, page.Text, [page, other], Catalog)),
        })
        {
            var reference = Assert.Single(rule.References);
            Assert.Equal(new Uri(page.Path).AbsoluteUri, reference.Uri);
            Assert.Equal(1, reference.Range.Start.Line);
            Assert.Equal(2, reference.Range.Start.Character);
            Assert.Equal(9, reference.Range.End.Character);
            Assert.Contains("@window", reference.Preview);
            Assert.False(rule.Partial);
        }
        var descendants = Query(":root > Container { opacity: 1; } :root Button { opacity: 1; }", page);
        Assert.All(descendants, rule => Assert.Single(rule.References));
    }

    [Fact]
    public void Global_window_rules_reference_all_window_components_once_and_exclude_plain_components()
    {
        var page = Document("Page.nazor", "@window\n<Button /><Dialog /><Dialog />");
        var dialog = Document("Dialog.nazor", "@window\n<TextField />");
        var plain = Document("Plain.nazor", "<Button />");
        var rule = Assert.Single(NssReferences.Query(Path.Combine(Root, "Theme.nss"), ":root:focus { opacity: 1; }", [page, dialog, plain], Catalog));
        Assert.True(rule.Global);
        Assert.Equal(2, rule.References.Count);
        Assert.Equal(new[] { new Uri(dialog.Path).AbsoluteUri, new Uri(page.Path).AbsoluteUri }, rule.References.Select(r => r.Uri));
        Assert.All(rule.References, r => Assert.Equal("@window", r.Preview));
        Assert.Empty(Assert.Single(Query(":root { opacity: 1; }", plain)).References);
    }

    [Theory]
    [InlineData("@*\n@window\n*@\n<Button />")]
    [InlineData("<!--\n@window\n-->\n<Button />")]
    [InlineData("@code { string text = \"\"\"\n@window\n\"\"\"; }\n<Button />")]
    [InlineData("<Container>\n@window\n</Container>")]
    public void Window_text_in_comments_code_or_nested_markup_is_not_a_page_root(string text)
    {
        Assert.Empty(Assert.Single(Query(":root { opacity: 1; }", Document("Page.nazor", text))).References);
    }

    [Fact]
    public void Scoped_counts_match_source_elements_and_grouped_selectors_are_deduplicated()
    {
        var page = Document("Page.nazor", """
            <Container Class="toolbar">
                <Button Id="save" Class="primary" Text="Save" />
                <TextField Class="primary" />
            </Container>
            """);
        var unrelated = Document("Other.nazor", "<Button Class=\"primary\" />");
        var rules = Query("""
            .primary { color: white; }
            #save { opacity: 1; }
            Button { padding: 8px; }
            Button.primary { min-width: 100px; }
            .toolbar Button.primary:hover { opacity: 0.8; }
            .primary, #save { color: blue; }
            .missing { color: blue; }
            """, page, unrelated);
        Assert.Equal(new[] { 2, 1, 1, 1, 1, 2, 0 }, rules.Select(r => r.References.Count));
        Assert.All(rules, r => { Assert.False(r.Partial); Assert.False(r.Global); });
        var button = rules[1].References.Single();
        Assert.EndsWith("Page.nazor", button.Uri);
        Assert.Equal(1, button.Range.Start.Line);
        Assert.Equal(5, button.Range.Start.Character);
        Assert.Contains("Id=\"save\"", button.Preview);
    }

    [Theory]
    [InlineData(".toolbar > Button.primary", 1)]
    [InlineData("Button + TextField.primary", 1)]
    [InlineData("Button ~ TextField.primary", 2)]
    [InlineData(".PRIMARY", 0)]
    [InlineData("button", 0)]
    [InlineData(".primary:hover:focus", 3)]
    [InlineData(".primary:not(Button)", 2)]
    [InlineData(":is(Button, TextField).primary", 3)]
    [InlineData(":where(Button, .absent)", 1)]
    [InlineData("[Id=save]", 1)]
    public void Matching_preserves_compounds_structure_functional_pseudos_and_case(string selector, int count)
    {
        var rules = Query(selector + " { color: red; }", Document("Page.nazor", """
            <Container Class="toolbar">
              <Button Class="primary" Id="save" />
              <TextField Class="primary" />
              <TextField Class="primary" />
            </Container>
            """));
        var rule = Assert.Single(rules);
        Assert.Equal(count, rule.References.Count);
        Assert.False(rule.Partial);
    }

    [Fact]
    public void Qualified_markup_resolves_the_correct_component_definition()
    {
        var source = Document("Page.nazor", "<One.Card />");
        var one = new NssReferenceDocument(Path.Combine(Root, "One", "Card.nazor"), "<Button Class=\"primary\" />", "One.Card");
        var two = new NssReferenceDocument(Path.Combine(Root, "Two", "Card.nazor"), "<TextField Class=\"primary\" />", "Two.Card");
        var rule = Assert.Single(Query("Card .primary { color: red; }", source, one, two));
        Assert.Single(rule.References);
        Assert.Equal(new Uri(one.Path).AbsoluteUri, rule.References[0].Uri);
        Assert.False(rule.Partial);
    }

    [Fact]
    public void Ambiguous_short_component_names_are_not_assumed_to_bind_to_a_local_definition()
    {
        var catalog = Catalog with { Components = [.. Catalog.Components, new("Button", "App.Button", "App", null, [])] };
        var page = Document("Page.nazor", "<Button Class=\"primary\" />");
        var definition = Document("Button.nazor", "<TextField Class=\"primary\" />");
        var rule = Assert.Single(NssReferences.Query(page.Path + ".nss", ".primary { color: red; }", [page, definition], catalog));
        Assert.True(rule.Partial);
        Assert.Empty(rule.References);
    }

    [Fact]
    public void Recursive_components_do_not_multiply_source_locations_or_recurse_forever()
    {
        var rule = Assert.Single(Query(".primary { color: red; }",
            Document("Page.nazor", "<Button Class=\"primary\" /><Page />")));
        Assert.True(rule.Partial);
        Assert.Single(rule.References);
    }

    [Fact]
    public void Attribute_reader_preserves_quotes_and_boolean_attributes_without_parsing_nested_text_as_attributes()
    {
        var rules = Query("[Disabled] { color: red; } .primary { color: red; }", Document("Page.nazor", """
            <Button Disabled Class="primary" Text="don't treat Class='secondary' as an attribute" />
            <Button Disabled="false" Text='Class="primary"' />
            """));
        Assert.Equal(new[] { 1, 1 }, rules.Select(r => r.References.Count));
        Assert.All(rules, r => Assert.False(r.Partial));
    }

    [Fact]
    public async Task Global_results_follow_added_renamed_and_deleted_files_and_exclude_nested_projects()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-nss-project-scope-").FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var nested = Directory.CreateDirectory(Path.Combine(directory, "Nested")).FullName;
            File.WriteAllText(Path.Combine(nested, "Other.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(nested, "Unrelated.nazor"), "<Button Class=\"primary\" />");
            var page = Path.Combine(directory, "Page.nazor");
            File.WriteAllText(page, "<Button Class=\"primary\" />");
            var sheet = Path.Combine(directory, "Theme.nss");
            await client.InitializeAsync();
            await client.OpenAsync(sheet, ".primary { color: red; }");
            async Task<JsonArray> References() => (await client.RequestAsync("nazor/nssReferences", new JsonObject
            { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(sheet) } }))!.AsArray().Single()!["references"]!.AsArray();
            Assert.Single(await References());
            var added = Path.Combine(directory, "Added.nazor");
            File.WriteAllText(added, "<TextField Class=\"primary\" />");
            Assert.Equal(2, (await References()).Count);
            var renamed = Path.Combine(directory, "Renamed.nazor");
            File.Move(added, renamed);
            var references = await References();
            Assert.Contains(references, r => r!["uri"]!.GetValue<string>() == LspClient.Uri(renamed));
            Assert.DoesNotContain(references, r => r!["uri"]!.GetValue<string>() == LspClient.Uri(added));
            File.Delete(page);
            Assert.Single(await References());
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }

    [Theory]
    [InlineData(".primary:first-child")]
    [InlineData("Button::part(input)")]
    [InlineData(".primary:has(> TextBlock)")]
    [InlineData(".primary:nth-child(2)")]
    [InlineData(".primary:not(:hover)")]
    public void Unsupported_structure_and_state_negation_are_explicitly_partial(string selector)
    {
        var rule = Assert.Single(Query(selector + " { color: red; }", Document("Page.nazor", "<Button Class=\"primary\" />")));
        Assert.Empty(rule.References);
        Assert.True(rule.Partial);
    }

    [Fact]
    public void Dynamic_attributes_are_not_literal_matches_and_do_not_prove_zero_references()
    {
        var rule = Assert.Single(Query(".primary { color: red; }", Document("Page.nazor", """
            <Button Class="@(flag ? "primary" : "other")" />
            <Button @attributes="attributes" />
            <Button Class="primary" />
            <Button Text='Class="primary"' />
            """)));
        Assert.True(rule.Partial);
        Assert.Single(rule.References);
    }

    [Fact]
    public void Compiler_parses_nested_rules_without_counting_at_rules_or_keyframes()
    {
        var rules = Query("""
            @media (min-width: 10px) {
                .toolbar {
                    color: blue;
                    & > Button:hover { opacity: 1; }
                }
            }
            @keyframes flash { from { opacity: 0; } to { opacity: 1; } }
            """, Document("Page.nazor", "<Container Class=\"toolbar\"><Button /></Container><Button />"));
        Assert.Equal(new[] { ".toolbar", "& > Button:hover" }, rules.Select(r => r.Selector));
        Assert.All(rules, r => Assert.Single(r.References));
    }

    [Fact]
    public void Inline_styles_keep_original_offsets_and_ignore_comments_code_strings_and_closing_tags()
    {
        var source = Document("Page.nazor", """
            @* <Button Class="primary" /> *@
            <!-- <Button Class="primary" /> -->
            @code { string text = "<Button Class='primary' />"; }
            @{ string text = "<Button Class='primary' />"; }
            @foreach (var item in items) {
                <Button Class="primary"></Button>
            }
            @style {
                .primary:hover { color: red; }
            }
            """);
        var rule = Assert.Single(NssReferences.Query(source.Path, source.Text, [source], Catalog));
        Assert.Single(rule.References);
        Assert.Equal(source.Text.IndexOf(".primary:hover", StringComparison.Ordinal), rule.Start);
        Assert.Equal(5, rule.References[0].Range.Start.Line);
    }

    [Fact]
    public void Global_rules_are_potential_project_references_in_multiple_sources()
    {
        var docs = new[] { Document("Page.nazor", "<Button Class=\"primary\" />"), Document("Other.nazor", "<TextField Class=\"primary\" />") };
        var rule = Assert.Single(NssReferences.Query(Path.Combine(Root, "Theme.nss"), ".primary { color: red; }", docs, Catalog));
        Assert.True(rule.Global);
        Assert.Equal(2, rule.References.Count);
    }

    [Theory]
    [InlineData(".primary", "Page.nazor", 1)]
    [InlineData("Card .primary", "Card.nazor", 1)]
    [InlineData("Card ::deep .primary", "Card.nazor", 2)]
    [InlineData("Card.primary", "Card.nazor", 0)]
    public void Component_qualifiers_and_deep_preserve_logical_boundaries(string selector, string firstFile, int count)
    {
        var rules = Query(selector + " { color: red; }",
            Document("Page.nazor", "<Container><Button Class=\"primary\" /><Card /></Container>"),
            Document("Card.nazor", "<Container><Button Class=\"primary\" /><Inner /></Container>"),
            Document("Inner.nazor", "<TextField Class=\"primary\" />"));
        var rule = Assert.Single(rules);
        Assert.Equal(count, rule.References.Count);
        if (count > 0) Assert.EndsWith(firstFile, rule.References[0].Uri);
        Assert.False(rule.Partial);
    }

    [Fact]
    public async Task Protocol_count_and_navigation_share_unsaved_results_and_refresh_after_changes()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-nss-references-").FullName;
        await using var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            await client.InitializeAsVisualStudioAsync();
            var component = Path.Combine(directory, "Page.nazor");
            var sheet = component + ".nss";
            await client.OpenAsync(component, "<Button Class=\"primary\" /><TextField Class=\"primary\" />");
            await client.OpenAsync(sheet, ".primary:hover { color: red; }");
            var request = new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(sheet) } };
            var data = (await client.RequestAsync("nazor/nssReferences", (JsonObject)request.DeepClone()))!.AsArray();
            Assert.Equal(2, data.Single()!["references"]!.AsArray().Count);
            var lenses = (await client.RequestAsync("textDocument/codeLens", (JsonObject)request.DeepClone()))!.AsArray();
            Assert.Equal("2 references", lenses.Single()!["command"]!["title"]!.GetValue<string>());
            Assert.Equal("nazor.showNssReferences", lenses.Single()!["command"]!["command"]!.GetValue<string>());
            var locations = (await client.AtAsync("textDocument/references", sheet, 0, 2))!.AsArray();
            Assert.Equal(2, locations.Count);
            Assert.All(locations, location => Assert.Equal(LspClient.Uri(component), location!["uri"]!.GetValue<string>()));
            await client.ChangeAsync(component, "<Button Class=\"other\" />", 2);
            data = (await client.RequestAsync("nazor/nssReferences", (JsonObject)request.DeepClone()))!.AsArray();
            Assert.Empty(data.Single()!["references"]!.AsArray());
            Assert.Null(await client.AtAsync("textDocument/hover", sheet, 0, 3));
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }
}
