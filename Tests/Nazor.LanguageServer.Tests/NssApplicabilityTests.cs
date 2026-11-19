using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Tests;

public sealed class NssApplicabilityTests
{
    [Theory]
    [InlineData("TextBlock { |", "object-fit", false)]
    [InlineData("Button { |", "-windows-badge-size", false)]
    [InlineData("Button { |", "-windows-badge-border", false)]
    [InlineData(":root { |", "-windows-badge-border", true)]
    [InlineData("Button { |", "-windows-badge-mask-image", false)]
    [InlineData("Button { |", "-windows-badge-transform", false)]
    [InlineData(":root { |", "-windows-badge-background", true)]
    [InlineData(":root { |", "-windows-badge-border-left-width", true)]
    [InlineData("Image { |", "object-fit", true)]
    [InlineData("Icon { |", "object-position", true)]
    [InlineData("Progress { |", "stroke-width", true)]
    [InlineData("Button { |", "stroke-width", false)]
    [InlineData(".variable { |", "object-fit", true)]
    [InlineData("Custom { |", "object-fit", true)]
    [InlineData("Button, Image { |", "object-fit", true)]
    [InlineData("Button, TextBlock { |", "object-fit", false)]
    [InlineData("Button, .variable { |", "object-fit", true)]
    [InlineData(".outer { Button { &:hover { |", "object-fit", false)]
    [InlineData("@media (min-width: 1px) { TextBlock { |", "object-fit", false)]
    [InlineData("Button { |", "font-size", true)]
    [InlineData("Container { |", "scrollbar-width", true)]
    [InlineData("Button { |", "grid-template-columns", true)]
    [InlineData("Button { transition: |", "stroke-width", false)]
    [InlineData("Progress { transition: |", "stroke-width", true)]
    [InlineData(".variable { transition: |", "stroke-width", true)]
    [InlineData("Button { object-fit: cover; } @media (min-width: 1px) { |", "object-fit", true)]
    [InlineData("Button { object-fit: cover; } @keyframes sizing { from { |", "stroke-width", true)]
    public void Completion_filters_only_certain_subjects_and_keeps_inherited_and_display_dependent_properties(string source, string property, bool offered)
    {
        var offset = source.IndexOf('|');
        var text = source.Replace("|", "");
        var items = NssLanguage.Complete(text, offset, [], text, o => o);
        Assert.Equal(offered, items.Any(i => i.Label == property && i.Kind == CompletionItemKind.Property));
    }

    [Theory]
    [InlineData("#save { |", false)]
    [InlineData("Container #save { |", false)]
    [InlineData("#picture { |", true)]
    [InlineData("#duplicate { |", true)]
    [InlineData("#dynamic { |", true)]
    [InlineData("#missing { |", true)]
    [InlineData("Custom #save { |", true)]
    [InlineData("Custom ::deep #save { |", true)]
    [InlineData("#save { &:hover { |", false)]
    public void Id_completion_uses_only_a_unique_literal_id_in_the_owning_component(string source, bool offered)
    {
        const string markup = """
            <Button Id="save" /><Image Id="picture" /><Button Id="duplicate" /><Image Id="duplicate" /><Button Id="@dynamic" />
            """;
        var offset = source.IndexOf('|');
        var text = source.Replace("|", "");
        var items = NssLanguage.Complete(text, offset, [], text, o => o,
            selectorNames: () => NssSelectorNames.FromMarkup(markup, "Page.nazor"));
        Assert.Equal(offered, items.Any(i => i.Label == "object-fit"));
    }

    [Fact]
    public void Inline_completion_uses_the_same_filter_and_original_edit_positions()
    {
        const string source = "<Button Id=\"save\" />\n@style { #save { | } }";
        var text = source.Replace("|", "");
        var offset = source.IndexOf('|');
        var items = NazorLanguage.Complete(text, offset, NazorCatalog.Empty);
        Assert.DoesNotContain(items, i => i.Label == "object-fit");
        Assert.Contains(items, i => i.Label == "padding" && TextPositions.Offset(text, i.TextEdit!.Range.Start) == offset);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Editor_warnings_and_completion_follow_unsaved_owner_types(bool inline, bool visualStudio)
    {
        var directory = Directory.CreateTempSubdirectory("nazor-applicability-").FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var owner = Path.Combine(directory, "Card.nazor");
            var path = inline ? owner : owner + ".nss";
            const string rule = "#save { object-fit: cover; }";
            var markup = "<Button Id=\"save\" /><Inner />" + (inline ? "\n@style { " + rule + " }" : "");
            if (visualStudio) await client.InitializeAsVisualStudioAsync();
            else await client.InitializeAsync();
            await client.OpenAsync(Path.Combine(directory, "Inner.nazor"), "<Image Id=\"save\" />");
            await client.OpenAsync(owner, markup);
            if (!inline) await client.OpenAsync(path, rule);
            var diagnostics = await client.DiagnosticsAsync(path, ds => ds.Any(d => d!["code"]!.GetValue<string>() == "NZS112"));
            var warning = Assert.Single(diagnostics, d => d!["code"]!.GetValue<string>() == "NZS112")!;
            Assert.Equal(2, warning["severity"]!.GetValue<int>());
            Assert.Equal(10, warning["range"]!["end"]!["character"]!.GetValue<int>() - warning["range"]!["start"]!["character"]!.GetValue<int>());
            var text = inline ? markup : rule;
            var offset = text.IndexOf("object-fit", StringComparison.Ordinal) + "object-fit".Length;
            var position = TextPositions.Position(text, offset);
            var items = (await client.AtAsync("textDocument/completion", path, position.Line, position.Character))!["items"]!.AsArray();
            Assert.DoesNotContain(items, i => i!["label"]!.GetValue<string>() == "object-fit");
            client.ForgetDiagnostics(path);
            await client.ChangeAsync(owner, markup.Replace("Button", "Image"), 2);
            await client.DiagnosticsAsync(path, ds => ds.All(d => d!["code"]!.GetValue<string>() != "NZS112"));
            items = (await client.AtAsync("textDocument/completion", path, position.Line, position.Character))!["items"]!.AsArray();
            Assert.Contains(items, i => i!["label"]!.GetValue<string>() == "object-fit");
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }

    [Fact]
    public async Task Editor_underlines_both_duplicate_id_declarations_and_clears_them_after_rename()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-duplicate-ids-").FullName;
        var client = LspClient.InProcess();
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            var path = Path.Combine(directory, "Page.nazor");
            const string text = "<Button Id=\"save\" />\n<TextBlock Id=\"save\" />";
            await client.InitializeAsync();
            await client.OpenAsync(path, text);
            var diagnostics = await client.DiagnosticsAsync(path, ds => ds.Count(d => d!["code"]!.GetValue<string>() == "NZC017") == 2);
            var duplicates = diagnostics.Where(d => d!["code"]!.GetValue<string>() == "NZC017").ToArray();
            Assert.All(duplicates, d => Assert.Equal(1, d!["severity"]!.GetValue<int>()));
            Assert.Equal(new[] { 0, 1 }, duplicates.Select(d => d!["range"]!["start"]!["line"]!.GetValue<int>()).Order());
            client.ForgetDiagnostics(path);
            await client.ChangeAsync(path, text.Replace("TextBlock Id=\"save\"", "TextBlock Id=\"label\""), 2);
            await client.DiagnosticsAsync(path, ds => ds.All(d => d!["code"]!.GetValue<string>() != "NZC017"));
        }
        finally { await client.DisposeAsync(); await LspClient.DeleteDirectoryAsync(directory); }
    }
}
