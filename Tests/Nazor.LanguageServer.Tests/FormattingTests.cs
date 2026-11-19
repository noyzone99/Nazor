using System.Text.Json.Nodes;
using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Workspace;
using Nazor.Nss;

namespace Nazor.LanguageServer.Tests;

public sealed class FormattingTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    public static TheoryData<string> RepositoryFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, "Samples"), "*.*", SearchOption.AllDirectories)
                     .Where(f => (f.EndsWith(".nazor", StringComparison.Ordinal) || f.EndsWith(".nss", StringComparison.Ordinal)) && !ProjectContext.IsBuildOutput(Path.GetDirectoryName(f)!, f) &&
                                 !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
            data.Add(Path.GetRelativePath(Root, file));
        return data;
    }

    [Fact]
    public void Stylesheets_get_one_declaration_per_line()
    {
        const string source = ".page{display:flex;   flex-direction : column;\ngap:12px}\n/* Buttons */\nButton:hover   >   TextBlock{color:red; /* main */\nbackground : var( --accent , white ) ;}\n\n\n@media (max-width: 720px){.page{gap:4px;}}\n";
        const string expected = """
            .page {
                display: flex;
                flex-direction: column;
                gap: 12px;
            }
            /* Buttons */
            Button:hover > TextBlock {
                color: red; /* main */
                background: var(--accent, white);
            }

            @media (max-width: 720px) {
                .page {
                    gap: 4px;
                }
            }

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), NssFormatter.FormatDocument(source));
        Assert.Equal("a {\n\tcolor: red;\n}\n", NssFormatter.FormatDocument("a { color: red }\n", "\t"));
        Assert.Equal("a {\r\n    color: red;\r\n}\r\n", NssFormatter.FormatDocument("a{color:red}\r\n"));
        Assert.Equal("a {\n    color: red;\n}", NssFormatter.FormatDocument("a{color:red}"));
        Assert.Equal(".a {\n    content: \"x  ;  y{\";\n}\n", NssFormatter.FormatDocument(".a{content:\"x  ;  y{\"}\n"));
    }

    [Fact]
    public void Spaces_inside_tags_are_normalized_and_nothing_else_moves()
    {
        Assert.Equal("<Button Class=\"primary\">", NazorFormatter.NormalizeTags("<Button  Class=\"primary\" >"));
        Assert.Equal("<TextField/>", NazorFormatter.NormalizeTags("<TextField  />"));
        Assert.Equal("</Button>", NazorFormatter.NormalizeTags("</Button >"));
        Assert.Equal("<Button Class=\"a\" Disabled=\"@true\" @onclick=\"Go\">Vai</Button>",
            NazorFormatter.NormalizeTags("<Button   Class = \"a\"\tDisabled=\"@true\"   @onclick=\"Go\" >Vai</Button >"));
        const string values = "<TextBlock Tooltip=\"due  spazi\" Class=\"@($\"a  {x}  b\")\">testo   libero</TextBlock>";
        Assert.Equal(values, NazorFormatter.NormalizeTags(values.Replace("<TextBlock ", "<TextBlock   ")));
        const string lines = "<Button\n    Class=\"a\"\n    Disabled\n    @onclick=\"Go\" />";
        Assert.Equal("<Button\n    Class=\"a\"\n    Disabled\n    @onclick=\"Go\"/>", NazorFormatter.NormalizeTags(lines));
        const string code = "@for (var i = 0; i<count  ; i++)\n{\n    <TextBlock  >@i</TextBlock>\n}";
        Assert.Equal("@for (var i = 0; i<count  ; i++)\n{\n    <TextBlock>@i</TextBlock>\n}", NazorFormatter.NormalizeTags(code));
        const string blocks = "@* <Button  > *@\n@code {\n    RenderFragment f = @<Button  />;\n}\n@style {\n    Button  > TextBlock { gap: 1px; }\n}";
        Assert.Equal(blocks, NazorFormatter.NormalizeTags(blocks));

        var once = NazorFormatter.Format("<Container  Class=\"page\" >\n<Button  Class=\"a\"  />\n</Container >\n");
        Assert.Equal("<Container Class=\"page\">\n    <Button Class=\"a\"/>\n</Container>\n", once);
        Assert.Equal(once, NazorFormatter.Format(once));
    }

    [Fact]
    public void Components_are_indented_like_Razor()
    {
        const string source = """
            @using Nazor.Controls
            <Container Class="page">
            <TextBlock>Hello</TextBlock>
                  <Button @onclick="Go"
            Disabled="true">Go</Button>
                @if (show)
                {
                <TextBlock>Shown</TextBlock>
                }
            </Container>

            @style {
            .page{gap:4px}
            }

            @code {
            private bool show;
            private void Go(){show=!show;}
            }

            """;
        const string expected = """
            @using Nazor.Controls
            <Container Class="page">
                <TextBlock>Hello</TextBlock>
                <Button @onclick="Go"
                    Disabled="true">Go</Button>
                @if (show)
                {
                    <TextBlock>Shown</TextBlock>
                }
            </Container>

            @style {
                .page {
                    gap: 4px;
                }
            }

            @code {
                private bool show;
                private void Go() { show = !show; }
            }

            """;
        Assert.Equal(expected, NazorFormatter.Format(source));
    }

    [Theory]
    [MemberData(nameof(RepositoryFiles))]
    public void Formatting_the_repository_files_is_stable_and_harmless(string file)
    {
        var path = Path.Combine(Root, file);
        var text = File.ReadAllText(path);
        var nss = file.EndsWith(".nss", StringComparison.Ordinal);
        var once = nss ? NssFormatter.FormatDocument(text) : NazorFormatter.Format(text);
        var twice = nss ? NssFormatter.FormatDocument(once) : NazorFormatter.Format(once);
        Assert.Equal(once, twice);
        if (nss)
        {
            Assert.True(NssCompiler.Compile("formatted", once).Success);
            return;
        }
        var context = ProjectContext.ForDocument(path);
        IEnumerable<string> Errors(string source) => new NazorCompiler(new NazorCompilerOptions
        {
            RootNamespace = context.RootNamespace,
            ProjectDirectory = context.Directory,
            References = context.References,
            CSharpSources = context.CSharpSources,
            SourceText = p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase) ? source : null,
        }).Analyze([.. context.Components.Append(path).Distinct(StringComparer.OrdinalIgnoreCase)]).Diagnostics.Where(d => d.IsError).Select(d => d.Id).Order();
        Assert.Equal(Errors(text), Errors(once));
    }

    [Fact]
    public async Task The_server_formats_documents_with_minimal_edits()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-format-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            await using var client = LspClient.InProcess();
            await client.InitializeAsync();
            var path = Path.Combine(directory, "Theme.nss");
            const string text = "a {\n    color: red;\n}\n\nb{color:blue}\n";
            await client.OpenAsync(path, text);
            var edits = (await client.RequestAsync("textDocument/formatting", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
                ["options"] = new JsonObject { ["tabSize"] = 4, ["insertSpaces"] = true },
            }))!.AsArray();
            var edit = Assert.Single(edits)!;
            Assert.Equal(4, edit["range"]!["start"]!["line"]!.GetValue<int>());
            var start = Protocol.TextPositions.Offset(text, new(edit["range"]!["start"]!["line"]!.GetValue<int>(), edit["range"]!["start"]!["character"]!.GetValue<int>()));
            var end = Protocol.TextPositions.Offset(text, new(edit["range"]!["end"]!["line"]!.GetValue<int>(), edit["range"]!["end"]!["character"]!.GetValue<int>()));
            Assert.Equal(NssFormatter.FormatDocument(text), text[..start] + edit["newText"]!.GetValue<string>() + text[end..]);

            var none = (await client.RequestAsync("textDocument/rangeFormatting", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(path) },
                ["range"] = new JsonObject { ["start"] = new JsonObject { ["line"] = 0, ["character"] = 0 }, ["end"] = new JsonObject { ["line"] = 1, ["character"] = 0 } },
                ["options"] = new JsonObject { ["tabSize"] = 4, ["insertSpaces"] = true },
            }))!.AsArray();
            Assert.Empty(none);
        }
        finally
        {
            await LspClient.DeleteDirectoryAsync(directory);
        }
    }

    [Fact]
    public void Line_edits_change_only_the_lines_of_the_range()
    {
        const string before = "a\nb\nc\nd\n";
        static string Apply(string text, Protocol.TextEdit[] edits)
        {
            foreach (var edit in edits.OrderByDescending(e => e.Range.Start.Line))
                text = text[..Protocol.TextPositions.Offset(text, edit.Range.Start)] + edit.NewText + text[Protocol.TextPositions.Offset(text, edit.Range.End)..];
            return text;
        }
        Assert.Equal("a\nB\nc\nd\n", Apply(before, TextEdits.Lines(before, "a\nB\nc\nD\nE\n", 1, 1)));
        Assert.Equal("a\nb\nc\nD\nE\n", Apply(before, TextEdits.Lines(before, "a\nB\nc\nD\nE\n", 3, 3)));
        Assert.Empty(TextEdits.Lines(before, "a\nB\nc\nD\nE\n", 0, 0));
        Assert.Equal("a\nc\nd\n", Apply(before, TextEdits.Lines(before, "a\nc\nd\n", 1, 2)));
        Assert.Empty(TextEdits.Lines(before, before, 0, 3));
    }

    [Fact]
    public async Task Typing_a_semicolon_or_a_brace_formats_what_it_ends()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-format-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "Demo.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            await using var client = LspClient.InProcess();
            var capabilities = (await client.InitializeAsync())!["capabilities"]!["documentOnTypeFormattingProvider"]!;
            Assert.Equal("}", capabilities["firstTriggerCharacter"]!.GetValue<string>());
            var path = Path.Combine(directory, "Counter.nazor");
            var text = """
                <Container>
                <TextBlock>@count</TextBlock>
                </Container>

                @code {
                    private int count;

                    private void Add()
                    {
                  count++;
                            count  +=  2;
                    }
                }

                """.ReplaceLineEndings("\n");
            await client.OpenAsync(path, text);
            var lines = text.Split('\n');
            async Task<string[]> TypedAsync(string file, string document, int line, string typed)
            {
                var edits = (await client.RequestAsync("textDocument/onTypeFormatting", new JsonObject
                {
                    ["textDocument"] = new JsonObject { ["uri"] = LspClient.Uri(file) },
                    ["position"] = new JsonObject { ["line"] = line, ["character"] = document.Split('\n')[line].LastIndexOf(typed[0]) + 1 },
                    ["ch"] = typed,
                    ["options"] = new JsonObject { ["tabSize"] = 4, ["insertSpaces"] = true },
                }))!.AsArray();
                foreach (var edit in edits.OrderByDescending(e => e!["range"]!["start"]!["line"]!.GetValue<int>())
                    .ThenByDescending(e => e!["range"]!["start"]!["character"]!.GetValue<int>()))
                {
                    var start = Protocol.TextPositions.Offset(document, new(edit!["range"]!["start"]!["line"]!.GetValue<int>(), edit["range"]!["start"]!["character"]!.GetValue<int>()));
                    var end = Protocol.TextPositions.Offset(document, new(edit["range"]!["end"]!["line"]!.GetValue<int>(), edit["range"]!["end"]!["character"]!.GetValue<int>()));
                    document = document[..start] + edit["newText"]!.GetValue<string>() + document[end..];
                }
                return document.Split('\n');
            }

            var semicolon = await TypedAsync(path, text, 9, ";");
            Assert.Equal("        count++;", semicolon[9]);
            Assert.Equal(lines[10], semicolon[10]);
            Assert.Equal(lines[1], semicolon[1]);

            var brace = await TypedAsync(path, text, 11, "}");
            Assert.Equal(["        count++;", "        count += 2;"], brace[9..11]);
            Assert.Equal(lines[1], brace[1]);

            var sheet = Path.Combine(directory, "Theme.nss");
            const string rules = "a {\ncolor:red;\n}\nb{color:blue}\n";
            await client.OpenAsync(sheet, rules);
            var rule = await TypedAsync(sheet, rules, 2, "}");
            Assert.Equal(["a {", "    color: red;", "}", "b{color:blue}"], rule[..4]);
        }
        finally
        {
            await LspClient.DeleteDirectoryAsync(directory);
        }
    }
}
