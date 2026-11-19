using Nazor.Compiler;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;
using Range = Nazor.LanguageServer.Protocol.Range;

namespace Nazor.LanguageServer.Tests;

public sealed class ControlsCatalog
{
    public NazorCatalog Catalog { get; } = new NazorCompiler(new NazorCompilerOptions
    {
        References = [typeof(Nazor.ComponentBase).Assembly.Location, typeof(Nazor.Controls.Button).Assembly.Location, typeof(Nazor.Style.StyleSheet).Assembly.Location],
    }).Analyze([]).Catalog;
}

public sealed class NazorLanguageTests(ControlsCatalog controls) : IClassFixture<ControlsCatalog>
{
    [Fact]
    public void Window_snippet_needs_only_Base()
    {
        var snippet=EditorSnippets.Directive("@window",7);
        Assert.NotNull(snippet);
        var source="@"+snippet.Value.NewText.Replace("$0","").Replace("${1:Window title}","Window title");
        var path=Path.GetFullPath("BaseOnlyWindowSnippet.nazor");
        var compiler=new NazorCompiler(new NazorCompilerOptions {
            References=[typeof(ComponentBase).Assembly.Location,typeof(Nazor.Controls.Button).Assembly.Location,typeof(Nazor.Style.StyleSheet).Assembly.Location],
            SourceText=file=>file==path ? source : null,
        });
        Assert.NotEmpty(compiler.Emit([compiler.Generate(path)],"BaseOnlyWindowSnippet"));
    }

    [Theory]
    [InlineData("Link","NavigateUri")]
    [InlineData("SplitView","PaneSize")]
    [InlineData("Tooltip","Delay")]
    [InlineData("TextField","ReadOnly")]
    [InlineData("Field","Invalid")]
    [InlineData("TableCell","RowSpan")]
    [InlineData("SegmentedControl","SelectionMode")]
    [InlineData("Accordion","RequireOne")]
    [InlineData("MenuCheckItem","IsChecked")]
    [InlineData("MenuRadioItem","CloseOnSelect")]
    [InlineData("Submenu","Content")]
    [InlineData("AlertDialog","Actions")]
    public void Semantic_primitive_parameters_are_available_to_authoring_tools(string component,string parameter)
    {
        var catalog = controls.Catalog;
        var metadata = Assert.Single(Assert.Single(catalog.Find(component)).Parameters,item=>item.Name==parameter);
        if (metadata.Kind==NazorParameterKind.ChildContent) Assert.DoesNotContain(Complete($"<{component} |", catalog),item=>item.Label==parameter);
        else Assert.Contains(Complete($"<{component} |", catalog),item=>item.Label==parameter);
    }

    [Theory]
    [InlineData("@*\n@window (int Value)\n*@", "Comment")]
    [InlineData("<!--\n@window (int Value)\n-->", "Comment")]
    [InlineData("@window (Dictionary<string, int> Value)", "CSharp")]
    public void Window_parameter_context_respects_comments_and_generic_types(string source, string expected) =>
        Assert.Equal(expected, NazorLanguage.Classify(source, source.IndexOf("Value", StringComparison.Ordinal) + 2).Kind.ToString());
    private static (string Text, int Offset) Caret(string source) => (source.Replace("|", ""), source.IndexOf('|'));

    private static NazorContext Classify(string source)
    {
        var (text, offset) = Caret(source);
        return NazorLanguage.Classify(text, offset);
    }

    private List<CompletionItem> Complete(string source, NazorCatalog? catalog = null)
    {
        var (text, offset) = Caret(source);
        return NazorLanguage.Complete(text, offset, catalog ?? controls.Catalog);
    }

    private string? Hover(string source)
    {
        var (text, offset) = Caret(source);
        return NazorLanguage.Hover(text, offset, controls.Catalog)?.Markdown;
    }

    [Fact]
    public void Composed_title_bar_and_snap_area_are_available_without_the_old_slot_parameters()
    {
        Assert.Contains(Complete("<|"), i => i.Label == "SnapLayoutArea");
        var attributes = Complete("<TitleBar |").Select(i => i.Label).ToArray();
        Assert.Contains("Title", attributes);
        Assert.DoesNotContain("ShowSystemButtons", attributes);
        Assert.DoesNotContain(attributes, name => name is "ShowMinimize" or "ShowMaximize" or
            "EnableSnapLayouts" or "WindowCloseButton" or "WindowMinimizeButton" or "WindowMaximizeButton");
        Assert.Contains(Complete("<SnapLayoutArea |"), i => i.Label == "Class");
    }

    [Theory]
    [InlineData("<Butt|", NazorContextKind.TagName)]
    [InlineData("<Container>\n    <|", NazorContextKind.TagName)]
    [InlineData("</Contai|", NazorContextKind.TagName)]
    [InlineData("<Button |", NazorContextKind.AttributeName)]
    [InlineData("<Button Disabled=\"true\" @on|", NazorContextKind.AttributeName)]
    [InlineData("<Button Disabled=\"tr|\"", NazorContextKind.AttributeValue)]
    [InlineData("<TextBlock>Count: |</TextBlock>", NazorContextKind.Content)]
    [InlineData("<TextBlock>@cou|", NazorContextKind.Content)]
    [InlineData("@inj|", NazorContextKind.Directive)]
    [InlineData("<Container />\n    @fo|", NazorContextKind.Directive)]
    [InlineData("@code {\n    private string s = \"<Butt|", NazorContextKind.CSharp)]
    [InlineData("@code { private int x; }\n<Butt|", NazorContextKind.TagName)]
    [InlineData("@* <Butt| *@", NazorContextKind.Comment)]
    [InlineData("@style {\n    .a { dis| }\n}", NazorContextKind.Style)]
    [InlineData("@style {\n    .a { color: red; }\n}\n<Butt|", NazorContextKind.TagName)]
    public void Positions_are_classified(string source, object kind) => Assert.Equal((NazorContextKind)kind, Classify(source).Kind);

    [Fact]
    public void Tags_and_attributes_are_known_to_the_context()
    {
        var context = Classify("<Button Disabled=\"true\" Class=\"x\" |");
        Assert.Equal("Button", context.Tag);
        Assert.Equal(["Disabled", "Class"], context.Attributes);
        Assert.Equal("Class", Classify("<TextBlock Class=\"ti|\"").Attribute);
    }

    [Fact]
    public void Elements_are_the_controls_and_components()
    {
        var items = Complete("<Container>\n    <Te|");
        var textBlock = Assert.Single(items, i => i.Label == "TextBlock");
        Assert.Equal(("Nazor.Controls.TextBlock", CompletionItemKind.Class), (textBlock.Detail, textBlock.Kind));
        Assert.Equal(new Range(new(1, 5), new(1, 7)), textBlock.TextEdit!.Range);
        Assert.Contains(items, i => i.Label == "Button");
        Assert.All(new[] { "TitleBar", "MenuBar", "Frame", "TextField" }, control => Assert.Contains(items, i => i.Label == control));
        Assert.DoesNotContain(items, i => i.Label is "Input" or "ToggleSwitch" or "TabView");
        Assert.DoesNotContain(items, i => i.Label.Contains('.'));
    }

    [Fact]
    public void Attributes_are_parameters_and_directive_attributes_not_yet_written()
    {
        var labels = Complete("<Button Disabled=\"true\" |").Select(i => i.Label).ToList();
        Assert.Contains("OnClick", labels);
        Assert.Contains("Class", labels);
        Assert.Contains("@onclick", labels);
        Assert.Contains("@key", labels);
        Assert.DoesNotContain("Disabled", labels);
        Assert.DoesNotContain("ChildContent", labels);
        Assert.DoesNotContain("@bind-Value", labels);
        Assert.Equal(CompletionItemKind.Event, Complete("<Button |").Single(i => i.Label == "OnClick").Kind);
        Assert.Contains(Complete("<TextField |"), i => i.Label == "@bind-Value");
        var (text, offset) = Caret("<Button @on|");
        var directives = NazorLanguage.Complete(text, offset, controls.Catalog);
        Assert.DoesNotContain(directives, i => i.Label == "OnClick");
        Assert.Equal(TextPositions.Range(text, offset - 3, offset), directives.Single(i => i.Label == "@onclick").TextEdit!.Range);
    }

    [Fact]
    public void Class_values_offer_the_classes_of_the_component_styles()
    {
        const string source = "<TextBlock Class=\"|\" />\n\n@style {\n    .title { font-size: 20px; }\n    .page { gap: 4px; .title:hover { } }\n}";
        Assert.Equal(["page", "title"], Complete(source).Select(i => i.Label));
        Assert.Empty(Complete("<Button Disabled=\"|\" />"));
    }

    [Fact]
    public void Directives_follow_an_at_sign_at_the_start_of_a_line()
    {
        var items = Complete("@|");
        Assert.Contains(items, i => i.Label == "code");
        Assert.Contains(items, i => i.Label == "inject" && i.Detail!.Contains("service"));
        Assert.Contains(items, i => i.Label == "style");
        Assert.Contains(items, i => i.Label == "foreach");
        Assert.Empty(Complete("<TextBlock>@cou|</TextBlock>"));
        Assert.Empty(Complete("@code {\n    private void Go() { <Bu| }\n}"));
    }

    [Fact]
    public void Styles_complete_like_NSS_in_document_coordinates()
    {
        const string source = "<Container />\n\n@style {\n    .page {\n        flex-d|\n    }\n}";
        var item = Assert.Single(Complete(source), i => i.Label == "flex-direction");
        Assert.Equal(new Range(new(4, 8), new(4, 14)), item.TextEdit!.Range);
        Assert.Contains(Complete("@style {\n    Bu|\n}"), i => i.Label == "Button");
    }

    [Fact]
    public void After_a_colon_only_the_parameters_of_the_directive_are_proposed_replacing_what_follows_it()
    {
        var (text, offset) = Caret("<Button @onclick=\"Go\" @onclick:|");
        var items = NazorLanguage.Complete(text, offset, controls.Catalog);
        Assert.Equal(["preventDefault", "stopPropagation"], items.Select(i => i.Label).Order());
        Assert.All(items, i => Assert.Equal(TextPositions.Range(text, offset, offset), i.TextEdit!.Range));
        Assert.All(items, i => Assert.Equal("bool", i.Detail));
        (text, offset) = Caret("<Container @onkeydown:sto|pPro />");
        var partial = NazorLanguage.Complete(text, offset, controls.Catalog);
        var stop = Assert.Single(partial, i => i.Label == "stopPropagation");
        Assert.Equal(TextPositions.Range(text, text.IndexOf("sto", StringComparison.Ordinal), text.IndexOf(" />", StringComparison.Ordinal)), stop.TextEdit!.Range);
        Assert.DoesNotContain(Complete("<Button @onclick:stopPropagation @onclick:|"), i => i.Label == "stopPropagation");

        Assert.Equal(["after", "get", "set"], Complete("<TextField @bind-Value=\"name\" @bind-Value:|").Select(i => i.Label).Order());
        Assert.Empty(Complete("<Button @bind-Value:|"));
        Assert.Contains("after", Hover("<TextField @bind-Value:af|ter=\"Saved\" />"));
        Assert.Contains("@onclick:stopPropagation", Hover("<Button @onclick:stopProp|agation />"));
    }

    [Fact]
    public void Hover_describes_elements_parameters_and_styles()
    {
        var button = Hover("<Container>\n    <Butt|on OnClick=\"Go\" />\n</Container>")!;
        Assert.Contains("**Button** (`Nazor.Controls.Button`)", button);
        Assert.Contains("- `OnClick`: `Nazor.EventCallback<Nazor.Input.PointerEventArgs>`", button);
        Assert.Contains("**Disabled**: `bool`", Hover("<Button Disab|led=\"true\" />"));
        Assert.Contains("@onclick", Hover("<Button @oncl|ick=\"Go\" />"));
        Assert.Contains("**display**", Hover("<Container />\n@style {\n    .a { disp|lay: flex; }\n}"));
        Assert.Null(Hover("<Unknown|Control />"));
        Assert.Null(Hover("@code { private int cou|nt; }"));
    }
}
