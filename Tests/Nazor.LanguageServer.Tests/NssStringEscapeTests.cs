using Nazor.LanguageServer.Features;
using Nazor.Nss;
using Nazor.Style.Syntax;

namespace Nazor.LanguageServer.Tests;

public sealed class NssStringEscapeTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    public void Quoted_escapes_do_not_create_editor_blocks_variables_or_colors(string newline)
    {
        var quoted = "\"\\E004\\22 \\\"; --fake: #f00; } {\\" + newline + "tail\"";
        var source = ":root { --icon: " + quoted + "; color: #123456; } Label { opacity: .5; }";
        var strings = StyleTokenizer.Tokenize(source).Where(t => t.Kind == StyleTokenKind.String).ToArray();
        var token = Assert.Single(strings);
        Assert.Equal(quoted, source.Substring(token.Start, token.Length));
        Assert.Equal("\uE004\"\"; --fake: #f00; } {tail", token.Value);
        Assert.Equal(NssContextKind.None, NssLanguage.Classify(source, source.IndexOf("--fake", StringComparison.Ordinal)).Kind);
        var context = NssLanguage.Classify(source, source.IndexOf("#123456", StringComparison.Ordinal));
        Assert.Equal(NssContextKind.Value, context.Kind);
        Assert.Equal("color", context.Property);
        Assert.Equal("--icon", Assert.Single(NssVariables.Read(source)).Name);
        var color = Assert.Single(NssColors.Find(source, component: false));
        Assert.Equal("#123456", source[color.Start..color.End]);
        var blocks = NssBlocks.At(source, 0, source.Length);
        Assert.Equal(2, blocks.Count);
        Assert.Equal(":root", blocks[0].Prelude(source));
        Assert.Equal("Label", blocks[1].Prelude(source));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Formatter_preserves_escape_spellings_compiled_values_and_idempotence(string newline)
    {
        var quoted = "\"\\E004\\\\E005\\22 \\\";{}\\" + newline + "tail\"";
        var source = "Item{--label:" + quoted + ";color:red}Item::before{content:var(--label)}";
        var formatted = NssFormatter.FormatDocument(source);
        Assert.Contains(quoted, formatted);
        Assert.Equal(formatted, NssFormatter.FormatDocument(formatted));
        var before = NssCompiler.Compile("format", source);
        var after = NssCompiler.Compile("format", formatted);
        Assert.True(before.Success, string.Join("\n", before.Diagnostics));
        Assert.True(after.Success, string.Join("\n", after.Diagnostics));
        Assert.Equal(before.StyleSheet.Describe(), after.StyleSheet.Describe());
        Assert.Equal(CSharpEmitter.Emit(before.StyleSheet), CSharpEmitter.Emit(after.StyleSheet));
    }
}
