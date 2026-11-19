using System.Text.Json.Nodes;
using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Tests;

public sealed class NssSelectorCompletionTests
{
    private const string Markup = """
        <Container Id="card" Class="card compact">
            <Button Id="save" Class="primary action" />
            <TextField Class="primary" />
        </Container>
        """;
    private static (string Text, int Offset) Caret(string text) => (text.Replace("|", ""), text.IndexOf('|'));
    private static List<CompletionItem> Complete(string source, string markup = Markup)
    {
        var (text, offset) = Caret(source);
        return NssLanguage.Complete(text, offset, [], text, o => o, selectorNames: () => NssSelectorNames.FromMarkup(markup, "Card.nazor"));
    }

    [Theory]
    [InlineData(".|", ".action,.card,.compact,.primary")]
    [InlineData(".pri|", ".primary")]
    [InlineData(".PRI|", "")]
    [InlineData("#|", "#card,#save")]
    [InlineData("#sa|", "#save")]
    [InlineData("Button.pri|mary:hover", ".primary")]
    [InlineData(".card > .pri|", ".primary")]
    [InlineData(".card { &.pri|", ".primary")]
    [InlineData("@media (min-width: 1px) { .pri|", ".primary")]
    [InlineData(":is(.pri|)", ".primary")]
    [InlineData(":IS(.pri|)", ".primary")]
    [InlineData(":not(#sa|)", "#save")]
    [InlineData(":where(Button, .pri|)", ".primary")]
    [InlineData(":nth-child(2 of .pri|)", ".primary")]
    [InlineData(":has(> .pri|)", ".primary")]
    [InlineData(":nth-last-child(even of #sa|)", "#save")]
    [InlineData(":nth-child(odd of :is(.pri|))", ".primary")]
    public void Literal_names_are_split_deduplicated_scoped_and_filtered_ordinally(string source, string expected) =>
        Assert.Equal(expected.Length == 0 ? [] : expected.Split(','), Complete(source).Select(i => i.Label));

    [Theory]
    [InlineData("Button.pri|mary:hover", ".primary", "Button.primary:hover")]
    [InlineData(".pri|bogus, #save", ".primary", ".primary, #save")]
    [InlineData("Button#sa|ve.primary", "#save", "Button#save.primary")]
    [InlineData(".card { & > .| }", ".primary", ".card { & > .primary }")]
    public void Committing_replaces_the_entire_token_with_one_prefix_and_preserves_the_selector(string source, string label, string expected)
    {
        var item = Assert.Single(Complete(source), i => i.Label == label);
        var text = Caret(source).Text;
        Assert.Equal(label, item.FilterText);
        Assert.Equal(expected, Apply(text, item.TextEdit!));
    }

    [Theory]
    [InlineData(".a { col| }")]
    [InlineData(".a { color: #| }")]
    [InlineData(".a { margin: .| }")]
    [InlineData(".a { content: \".|\" }")]
    [InlineData("/* .| */")]
    [InlineData("Button[Class=.|]")]
    [InlineData("Button[Id=\"#|\"]")]
    [InlineData("Button:nth-child(2n + .|)")]
    [InlineData("@keyframes flash { .| }")]
    public void Other_contexts_do_not_discover_or_offer_markup_names(string source)
    {
        var (text, offset) = Caret(source);
        NssLanguage.Complete(text, offset, [], text, o => o, selectorNames: () => throw new InvalidOperationException("Not a selector-name context"));
    }

    [Fact]
    public void Markup_reader_ignores_comments_code_attribute_contents_and_dynamic_values()
    {
        var names = NssSelectorNames.FromMarkup("""
            @* <Button Class="comment" Id="comment" /> *@
            <!-- <Button Class="html-comment" Id="html-comment" /> -->
            @code { string text = "<Button Class='code' Id='code' />"; }
            @{ string text = "<Button Class='statement' Id='statement' />"; }
            <Button Text="Class='attribute' Id='attribute'" Class="real" Id="&#x73;ave" />
            <Button Class="@(active ? "dynamic" : "other")" Id="@identifier" />
            <Button Class="mixed @dynamic" @attributes="attributes" />
            <Button Class Id />
            <Button Text="@("<TextField Class='nested' />")" Class="after-expression" />
            """, "Card.nazor");
        Assert.Equal(new[] { "after-expression", "real" }, names.Classes);
        Assert.Equal(new[] { "save" }, names.Ids);
    }

    [Fact]
    public void Names_follow_the_existing_nss_identifier_grammar_without_inventing_escapes()
    {
        const string markup = "<Button Class=\"_private -dash --custom café 123bad bad:name escaped\\name\" Id=\"123\" />";
        var names = NssSelectorNames.FromMarkup(markup, "Card.nazor");
        Assert.Equal(new[] { "--custom", "-dash", "_private", "café" }, names.Classes);
        Assert.Equal(new[] { "123" }, names.Ids);
        var item = Assert.Single(Complete(".caf|é", markup));
        Assert.Equal(".café", Apply(".café", item.TextEdit!));
        Assert.Equal("#123", Assert.Single(Complete("#12|3", markup)).Label);
    }

    [Theory]
    [InlineData("<Button Class=\"primary\" Id=\"save\"")]
    [InlineData("<Button Class='primary' Id='save' />")]
    [InlineData("<Button class=primary id=save />")]
    public void Complete_literal_attributes_survive_incomplete_tags_and_supported_quoting(string markup)
    {
        var names = NssSelectorNames.FromMarkup(markup, "Card.nazor");
        Assert.Equal(new[] { "primary" }, names.Classes);
        Assert.Equal(new[] { "save" }, names.Ids);
    }

    [Fact]
    public void Inline_styles_use_owner_markup_and_original_document_edit_ranges_in_multiple_blocks()
    {
        var source = Markup + "\r\n@style { .card { opacity: 1; } }\r\n@style { Button.pri|mary:hover { opacity: 1; } }";
        var (text, offset) = Caret(source);
        var items = NazorLanguage.Complete(text, offset, new NazorCatalog([], []));
        var item = Assert.Single(items, i => i.Label == ".primary");
        Assert.Equal(text, Apply(text, item.TextEdit!));
        Assert.Null(NazorLanguage.Hover(text, offset, new NazorCatalog([], [])));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Protocol_scoped_completion_uses_unsaved_owner_only_and_tracks_attribute_renames(bool inline)
    {
        var directory = Directory.CreateTempSubdirectory("nazor-selector-owner-").FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var page = Path.Combine(directory, "Card.nazor");
            var sheet = page + ".nss";
            var owner = Markup + "\n<Inner />" + (inline ? "\n@style { .| }" : "");
            File.WriteAllText(page, "<Button Class=\"old-disk-value\" />");
            var initialized = await client.InitializeAsVisualStudioAsync();
            Assert.Contains(initialized!["capabilities"]!["completionProvider"]!["triggerCharacters"]!.AsArray(), c => c!.GetValue<string>() == "#");
            await client.OpenAsync(Path.Combine(directory, "Other.nazor"), "<Button Class=\"search-field\" Id=\"search\" />");
            await client.OpenAsync(Path.Combine(directory, "Inner.nazor"), "<Button Class=\"inner-secret\" />");
            await client.OpenAsync(page, Caret(owner).Text);
            if (!inline) await client.OpenAsync(sheet, ".");
            Assert.Equal(new[] { ".action", ".card", ".compact", ".primary" }, await Labels(client, inline ? page : sheet, inline ? owner : ".|"));
            owner = owner.Replace("primary", "accent").Replace("Id=\"save\"", "Id=\"submit\"");
            await client.ChangeAsync(page, Caret(owner).Text, 2);
            Assert.Equal(new[] { ".accent", ".action", ".card", ".compact" }, await Labels(client, inline ? page : sheet, inline ? owner : ".|"));
            var idSource = inline ? owner.Replace(".|", "#|") : "#|";
            await client.ChangeAsync(inline ? page : sheet, Caret(idSource).Text, 3);
            Assert.Equal(new[] { "#card", "#submit" }, await Labels(client, inline ? page : sheet, idSource));
            var orphan = Path.Combine(directory, "Missing.nazor.nss");
            await client.OpenAsync(orphan, ".");
            Assert.Empty(await Labels(client, orphan, ".|"));
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }

    [Fact]
    public async Task General_styles_without_a_project_use_only_their_own_directory()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Nazor.slnx"))) root = root.Parent;
        var directory = Directory.CreateDirectory(Path.Combine(root!.FullName, "artifacts", "nazor-selector-directory-" + Guid.NewGuid().ToString("N"))).FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Page.nazor"), "<Button Class=\"local\" />");
            var subfolder = Directory.CreateDirectory(Path.Combine(directory, "Child")).FullName;
            var child = Path.Combine(subfolder, "Other.nazor");
            await client.InitializeAsync();
            await client.OpenAsync(child, "<Button Class=\"foreign\" />");
            var sheet = Path.Combine(directory, "Theme.nss");
            Assert.Null(Nazor.LanguageServer.Workspace.ProjectContext.ForDocument(sheet).ProjectFile);
            await client.OpenAsync(sheet, ".");
            Assert.Equal(new[] { ".local" }, await Labels(client, sheet, ".|"));
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }

    [Fact]
    public async Task Global_completion_stays_in_the_owning_project_and_refreshes_open_and_watched_sources()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-selector-project-").FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var nested = Directory.CreateDirectory(Path.Combine(directory, "Nested")).FullName;
            File.WriteAllText(Path.Combine(nested, "Other.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(nested, "Unrelated.nazor"), "<Button Class=\"unrelated\" />");
            var page = Path.Combine(directory, "Page.nazor");
            File.WriteAllText(page, "<Button Class=\"one\" Id=\"first\" />");
            var sheet = Path.Combine(directory, "Theme.nss");
            await client.InitializeAsync();
            await client.OpenAsync(sheet, ".");
            Assert.Equal(new[] { ".one" }, await Labels(client, sheet, ".|"));
            var added = Path.Combine(directory, "Added.nazor");
            File.WriteAllText(added, "<Button Class=\"added one\" />");
            await Watch(added, 1);
            Assert.Equal(new[] { ".added", ".one" }, await Labels(client, sheet, ".|"));
            await client.OpenAsync(page, "<Button Class=\"unsaved\" Id=\"second\" />");
            Assert.Equal(new[] { ".added", ".one", ".unsaved" }, await Labels(client, sheet, ".|"));
            await client.ChangeAsync(page, "<Button Class=\"renamed\" Id=\"second\" />", 2);
            Assert.Equal(new[] { ".added", ".one", ".renamed" }, await Labels(client, sheet, ".|"));
            await client.ChangeAsync(sheet, "#", 2);
            Assert.Equal(new[] { "#second" }, await Labels(client, sheet, "#|"));
            await client.ChangeAsync(sheet, ".", 3);
            await client.CloseAsync(page);
            Assert.Equal(new[] { ".added", ".one" }, await Labels(client, sheet, ".|"));
            var moved = Path.Combine(directory, "Moved.nazor");
            File.Move(added, moved);
            File.WriteAllText(moved, "<Button Class=\"moved\" />");
            await Watch(added, 3); await Watch(moved, 1);
            Assert.Equal(new[] { ".moved", ".one" }, await Labels(client, sheet, ".|"));
            File.Delete(moved);
            await Watch(moved, 3);
            Assert.Equal(new[] { ".one" }, await Labels(client, sheet, ".|"));
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }

        Task Watch(string path, int type) => client.NotifyAsync("workspace/didChangeWatchedFiles", new JsonObject
        { ["changes"] = new JsonArray(new JsonObject { ["uri"] = LspClient.Uri(path), ["type"] = type }) });
    }

    private static string Apply(string text, TextEdit edit) => text[..TextPositions.Offset(text, edit.Range.Start)] + edit.NewText + text[TextPositions.Offset(text, edit.Range.End)..];
    private static async Task<string[]> Labels(LspClient client, string path, string source)
    {
        var (text, offset) = Caret(source);
        var position = TextPositions.Position(text, offset);
        var response = (await client.AtAsync("textDocument/completion", path, position.Line, position.Character))!;
        Assert.True(response["isIncomplete"]!.GetValue<bool>());
        return response["items"]!.AsArray().Select(i => i!["label"]!.GetValue<string>()).ToArray();
    }
}
