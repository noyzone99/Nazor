using Nazor.LanguageServer.Features;

namespace Nazor.LanguageServer.Tests;

public sealed class NssVariableDocumentationTests
{
    [Theory]
    [InlineData("/** <doc>Accent for actions</doc> */", "Accent for actions")]
    [InlineData("/**\r\n * <doc>Accent for actions\r\n *\r\n * Second paragraph</doc>\r\n */", "Accent for actions\n\nSecond paragraph")]
    [InlineData("/** <doc>A &amp; B</doc> */", "A & B")]
    public void Declaration_reference_and_completion_share_documentation(string comment, string expected)
    {
        var text = $".a {{\n    {comment}\n    --accent: red; color: var(--accent); }}";
        var declaration = text.IndexOf("--accent", StringComparison.Ordinal);
        var reference = text.LastIndexOf("--accent", StringComparison.Ordinal);
        Assert.Contains(expected, NssLanguage.Hover(text, declaration + 3)!.Value.Markdown);
        Assert.Equal(NssLanguage.Hover(text, declaration + 3)!.Value.Markdown, NssLanguage.Hover(text, reference + 3)!.Value.Markdown);
        var items = NssLanguage.Complete(text, reference + 3, [], text, o => o);
        Assert.Contains(expected, Assert.Single(items, i => i.Label == "--accent").Documentation!.Value);
    }

    [Theory]
    [InlineData("/* <doc>ignored</doc> */")]
    [InlineData("/** <doc></doc> */")]
    [InlineData("/** <doc>broken */")]
    [InlineData("/** <doc>ignored</doc> */ /* normal */")]
    [InlineData("/** <doc>ignored</doc> */ --other: blue;")]
    public void Unattached_empty_and_malformed_comments_fall_back(string prefix)
    {
        var text = $".a {{ {prefix} --accent: red; color: var(--accent); }}";
        Assert.Contains("a custom property", NssLanguage.Hover(text, text.LastIndexOf("--accent", StringComparison.Ordinal) + 3)!.Value.Markdown);
    }

    [Fact]
    public void Fake_declarations_inside_strings_and_comments_are_ignored()
    {
        const string text = ".a { /* --fake: red; */ content: '--string: blue;'; --real: red; color: var(--); }";
        var names = NssVariables.Read(text).Select(v => v.Name).ToArray();
        Assert.Equal(["--real"], names);
    }

    [Fact]
    public void Duplicate_names_use_local_declarations_and_ambiguous_external_help_falls_back()
    {
        const string text = ".a { /** <doc>First</doc> */ --tone: red; color: var(--tone); }\n.b { /** <doc>Second</doc> */ --tone: blue; color: var(--tone); }\n.c { color: var(--tone); }";
        Assert.Contains("First", NssLanguage.Hover(text, text.IndexOf("var(--tone)", StringComparison.Ordinal) + 6)!.Value.Markdown);
        Assert.Contains("Second", NssLanguage.Hover(text, text.IndexOf("var(--tone)", text.IndexOf(".b", StringComparison.Ordinal), StringComparison.Ordinal) + 6)!.Value.Markdown);
        Assert.Contains("a custom property", NssLanguage.Hover(text, text.LastIndexOf("var(--tone)", StringComparison.Ordinal) + 6)!.Value.Markdown);
    }

    [Fact]
    public void Inline_styles_and_unsaved_edits_resolve_current_text()
    {
        const string text = "<Container />\n@style { .a { /** <doc>Original</doc> */ --tone: red; color: var(--tone); } }";
        var offset = text.LastIndexOf("--tone", StringComparison.Ordinal) + 3;
        Assert.Contains("Original", NazorLanguage.Hover(text, offset, new([], []))!.Value.Markdown);
        var edited = text.Replace("Original", "Updated!", StringComparison.Ordinal);
        Assert.Contains("Updated", NazorLanguage.Hover(edited, offset, new([], []))!.Value.Markdown);
    }

    [Fact]
    public void Formatting_and_sidecar_round_trip_preserve_documentation_without_changing_values()
    {
        const string sheet = ".a { /** <doc>Accent for actions</doc> */ --accent:red; color:var(--accent); }";
        var formatted = NssFormatter.FormatDocument(sheet);
        Assert.Equal("Accent for actions", Assert.Single(NssVariables.Read(formatted)).Documentation);
        var compiled = Nazor.Nss.NssCompiler.Compile("docs", formatted);
        Assert.True(compiled.Success);
        Assert.Equal(2, Assert.Single(compiled.StyleSheet.Rules).Declarations.Count);
        const string path = @"C:\App\Card.nazor";
        var component = "<Container />\n@style {\n" + sheet + "\n}";
        var move = Assert.Single(CodeActions.For(path, component, component.IndexOf("@style", StringComparison.Ordinal), 0, null, _ => null));
        var sidecar = Assert.Single(move.Changes, c => c.Path == path + ".nss").NewText!;
        Assert.Equal("Accent for actions", Assert.Single(NssVariables.Read(sidecar)).Documentation);
        var remaining = Assert.Single(move.Changes, c => c.Path == path).NewText!;
        var back = Assert.Single(CodeActions.For(path, remaining, 0, 0, null, p => p == path + ".nss" ? sidecar : null));
        var restored = Assert.Single(back.Changes, c => c.Path == path).NewText!;
        var offset = restored.IndexOf("--accent", StringComparison.Ordinal) + 3;
        Assert.Contains("Accent for actions", NazorLanguage.Hover(restored, offset, new([], []))!.Value.Markdown);
    }
}
