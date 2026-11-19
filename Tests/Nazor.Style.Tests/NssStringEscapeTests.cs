using System.Text.RegularExpressions;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style.Syntax;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class NssStringEscapeTests
{
    [Theory]
    [InlineData(@"\4", "\u0004")]
    [InlineData(@"\41", "A")]
    [InlineData(@"\041", "A")]
    [InlineData(@"\0041", "A")]
    [InlineData(@"\00041", "A")]
    [InlineData(@"\000041", "A")]
    [InlineData(@"\E004", "\uE004")]
    [InlineData(@"\1f600", "😀")]
    [InlineData(@"\10FFFF", "\U0010FFFF")]
    [InlineData(@"\000041B", "AB")]
    [InlineData(@"\41B", "\u041B")]
    [InlineData(@"\0000000", "\uFFFD0")]
    [InlineData(@"\0", "\uFFFD")]
    [InlineData(@"\D800", "\uFFFD")]
    [InlineData(@"\dfff", "\uFFFD")]
    [InlineData(@"\110000", "\uFFFD")]
    [InlineData(@"\FFFFFF", "\uFFFD")]
    [InlineData(@"\\E004", @"\E004")]
    [InlineData(@"\g", "g")]
    public void Quoted_strings_decode_CSS_escapes_without_losing_source_spans(string body, string expected)
    {
        foreach (var quote in new[] { '"', '\'' })
        {
            var source = quote + body + quote;
            var tokens = StyleTokenizer.Tokenize(source);
            var token = Assert.Single(tokens, t => t.Kind == StyleTokenKind.String);
            Assert.Equal(expected, token.Value);
            Assert.Equal(0, token.Start);
            Assert.Equal(source.Length, token.Length);
            Assert.Equal(source, source.Substring(token.Start, token.Length));
            Assert.Equal(source.Length, tokens[^1].Start);
            Assert.Equal(StyleTokenKind.EndOfFile, tokens[^1].Kind);
            Assert.Equal(2, tokens.Count);
        }
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    public void Hex_terminators_consume_one_CSS_whitespace_unit(string whitespace)
    {
        Assert.Equal("AB", StringToken('"' + @"\41" + whitespace + "B\"").Value);
        Assert.Equal("A B", StringToken('"' + @"\41" + whitespace + " B\"").Value);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    public void Escaped_newlines_continue_both_quote_styles_without_adding_text(string newline)
    {
        foreach (var quote in new[] { '"', '\'' })
        {
            var source = quote + "a\\" + newline + "b" + quote;
            var token = StringToken(source);
            Assert.Equal("ab", token.Value);
            Assert.Equal(source.Length, token.Length);
        }
    }

    [Fact]
    public void Quoted_NULL_and_unpaired_surrogates_use_replacement_scalars()
    {
        foreach (var scalar in new[] { '\0', '\uD800', '\uDFFF' })
        {
            Assert.Equal("a\uFFFDb", StringToken("\"a" + scalar + "b\"").Value);
            Assert.Equal("a\uFFFDb", StringToken("\"a\\" + scalar + "b\"").Value);
        }
        Assert.Equal("😀😀", StringToken("\"😀\\😀\"").Value);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    public void Unescaped_newlines_remain_invalid_and_are_not_consumed_by_the_bad_string(string newline)
    {
        var tokens = StyleTokenizer.Tokenize("\"a" + newline + "rest");
        Assert.Equal(StyleTokenKind.BadString, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Value);
        Assert.Equal(2, tokens[0].Length);
        Assert.Equal(2, tokens[1].Start);
        Assert.Equal(StyleTokenKind.Whitespace, tokens[1].Kind);
    }

    [Theory]
    [InlineData("\"unterminated")]
    [InlineData("\"trailing\\")]
    [InlineData("'\\41")]
    public void NSS_keeps_strict_unterminated_string_diagnostics(string source)
    {
        var token = StyleTokenizer.Tokenize(source)[0];
        Assert.Equal(StyleTokenKind.BadString, token.Kind);
        Assert.Equal(source.Length, token.Length);
        Assert.False(NssCompiler.Compile("strict", "Item::before { content: " + source).Success);
    }

    [Fact]
    public void Escaped_delimiters_remain_one_string_and_do_not_create_syntax()
    {
        const string source = "\"\\22 \\27 \\3B \\7B \\7D \\A \\9 \\5C \"";
        var token = StringToken(source);
        Assert.Equal("\"';{}\n\t\\", token.Value);
        var serialized = token.ToString();
        Assert.Equal(token.Value, StringToken(serialized).Value);
        Assert.Equal(serialized, StringToken(serialized).ToString());
    }

    [Fact]
    public void Serialization_preserves_quotes_backslashes_controls_and_unicode()
    {
        const string expected = "a\"b'\\E004\n\r\f\t\u0001\u007F\uE004😀";
        var token = new StyleToken(StyleTokenKind.String, expected, 0, 0, 0);
        var serialized = token.ToString();
        Assert.DoesNotContain('\n', serialized);
        Assert.DoesNotContain('\r', serialized);
        Assert.Equal(expected, StringToken(serialized).Value);
        Assert.Equal(serialized, StyleToken.Serialize(StyleTokenizer.Tokenize(serialized)));
    }

    [Fact]
    public void Generated_content_and_variables_preserve_decoded_backslashes_across_substitution()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var element = host.Render<Items>().Elements("Item").First();
        engine.Application = Compile("""
            Item { --icon: "\E004\\41\A "; }
            Item::before { content: var(--icon); }
            """);
        var generated = Assert.IsType<ElementNode>(engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.Equal("\uE004\\41\n", Assert.IsType<TextNode>(Assert.Single(generated.Children)).Text);
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(engine.Application));
        engine.Application = restored;
        Assert.Equal("\uE004\\41\n", Assert.IsType<TextNode>(Assert.Single(generated.Children)).Text);
    }

    [Fact]
    public void Compiler_JSON_and_generated_code_agree_with_literal_glyphs()
    {
        var escaped = Compile("""Icon { font-symbol: "Leaf\20 Icons" "\E004"; --name: "\1F600"; }""");
        var literal = Compile("Icon { font-symbol: \"Leaf Icons\" \"\uE004\"; --name: \"😀\"; }");
        Assert.Equal(literal.Describe(), escaped.Describe());
        Assert.Equal(CSharpEmitter.Emit(literal), CSharpEmitter.Emit(escaped));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(escaped));
        Assert.Equal(literal.Describe(), restored.Describe());
        Assert.Equal(CSharpEmitter.Emit(literal), CSharpEmitter.Emit(restored));
    }

    [Fact]
    public void Quoted_URL_values_decode_before_relative_asset_resolution()
    {
        var escaped = NssCompiler.Compile("Styles/Theme.nss", "Icon { background: url(\"../images/\\61 .png\") no-repeat; }");
        var literal = NssCompiler.Compile("Styles/Theme.nss", "Icon { background: url(\"../images/a.png\") no-repeat; }");
        Assert.True(escaped.Success, string.Join("\n", escaped.Diagnostics));
        Assert.True(literal.Success, string.Join("\n", literal.Diagnostics));
        Assert.Equal(literal.StyleSheet.Describe(), escaped.StyleSheet.Describe());
    }

    [Fact]
    public void Leaf_theme_hex_and_literal_glyph_spellings_produce_identical_sheets()
    {
        var path = Path.Combine(RepositoryPaths.Root, "Sources", "Themes", "Nazor.Themes.NazorLeaf", "Themes", "NazorLeaf.nss");
        var source = File.ReadAllText(path);
        var literal = Regex.Replace(source, @"\\(E[0-9A-Fa-f]{3})(?=[""'])", match => char.ConvertFromUtf32(Convert.ToInt32(match.Groups[1].Value, 16)));
        var escaped = Regex.Replace(literal, "[\uE000-\uF8FF]", match => "\\" + ((int)match.Value[0]).ToString("X"));
        Assert.NotEqual(literal, escaped);
        var first = Compile(literal);
        var second = Compile(escaped);
        Assert.Equal(first.Describe(), second.Describe());
        Assert.Equal(CSharpEmitter.Emit(first), CSharpEmitter.Emit(second));
    }

    private static StyleToken StringToken(string source)
    {
        var tokens = StyleTokenizer.Tokenize(source);
        Assert.Equal(2, tokens.Count);
        Assert.Equal(StyleTokenKind.EndOfFile, tokens[1].Kind);
        Assert.Equal(StyleTokenKind.String, tokens[0].Kind);
        return tokens[0];
    }

    private static StyleSheet Compile(string source)
    {
        var result = NssCompiler.Compile("escapes", source);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }
}
