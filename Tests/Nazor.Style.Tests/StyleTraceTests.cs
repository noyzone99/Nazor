using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class Traced : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Top");
        builder.AddAttribute(1, "class", "card big");
        builder.AddAttribute(2, "style", "padding: 2px; --gap: 3px");
        builder.OpenElement(3, "Leaf");
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class StyleTraceTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public StyleTraceTests()
    {
        engine = new StyleEngine(host.Renderer)
        {
            Theme = Compile("Top { color: black; margin: 1px; } Top.card { color: gray; }", "theme.nss"),
            Application = Compile("""
                .card { color: red; border-width: var(--w); }
                @media (min-width: 9000px) { .card { color: blue; } }
                .card { --w: 2px; }
                Leaf { width: 5px; }
                """, "app.nss"),
        };
        root = host.Render<Traced>();
    }

    public void Dispose()
    {
        engine.Dispose();
        host.Dispose();
    }

    private static StyleSheet Compile(string nss, string path)
    {
        var result = NssCompiler.Compile(path, nss, path);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private StyleTrace Trace() => engine.Trace(root.Single("Top"));

    [Fact]
    public void Rules_are_in_cascade_order_with_their_level_sheet_selector_and_media()
    {
        var rules = Trace().Rules;
        Assert.Equal(["Top", "Top.card", ".card", ".card", ".card"], rules.Select(r => r.Selector));
        Assert.Equal([StyleLevel.Theme, StyleLevel.Theme, StyleLevel.Application, StyleLevel.Application, StyleLevel.Application], rules.Select(r => r.Level));
        Assert.Equal(["theme.nss", "theme.nss", "app.nss", "app.nss", "app.nss"], rules.Select(r => r.Sheet));
        Assert.Equal(new Specificity(0, 1, 1), rules[1].Specificity);
        Assert.Equal(("(min-width: 9000px)", false), (rules[3].Media, rules[3].MediaMatches));
        Assert.True(rules[3].Declarations.Single().IsOverridden);
        Assert.DoesNotContain(root.Single("Leaf").Name, rules.Select(r => r.Selector));
    }

    [Fact]
    public void Each_value_knows_the_declaration_that_set_it_or_where_it_comes_from()
    {
        var trace = Trace();
        TracedValue Value(string name) => trace.Values.Single(v => v.Property == name);
        var color = Value("color");
        Assert.Equal((ValueSource.Rule, 2, 0), (color.Source, color.Rule, color.Declaration));
        Assert.Equal(engine.GetStyle(root.Single("Top")).Color.ToString(), color.Value);
        Assert.True(trace.Rules[1].Declarations.Single(d => d.Property == "color").IsOverridden);
        Assert.Equal(("1px", 0), (Value("margin-top").Value, Value("margin-top").Rule));
        var widths = trace.Rules[2].Declarations.Single(d => d.Property == "border-width");
        Assert.Equal(["border-top-width", "border-right-width", "border-bottom-width", "border-left-width"], widths.Winning);
        Assert.Equal("2px", Value("border-left-width").Value);
        Assert.Equal((ValueSource.Inline, (int?)null, 0), (Value("padding-top").Source, Value("padding-top").Rule, Value("padding-top").Declaration));
        Assert.Equal(["padding", "--gap"], trace.Inline.Select(d => d.Property));
        Assert.Equal(ValueSource.Inherited, Value("font-size").Source);
        Assert.Equal(ValueSource.Initial, Value("width").Source);
    }

    [Fact]
    public void Custom_properties_come_from_rules_the_style_attribute_or_the_parent()
    {
        var custom = Trace().CustomProperties;
        Assert.Equal((ValueSource.Rule, "2px"), (custom.Single(c => c.Property == "--w").Source, custom.Single(c => c.Property == "--w").Value));
        Assert.Equal(ValueSource.Inline, custom.Single(c => c.Property == "--gap").Source);
        Assert.Equal(ValueSource.Inherited, custom.Single(c => c.Property == "--nz-system-canvas").Source);
        Assert.Equal(ValueSource.Rule, engine.Trace(root.Single("Leaf")).Values.Single(v => v.Property == "width").Source);
    }

    [Fact]
    public void A_removed_element_has_no_trace()
    {
        var top = root.Single("Top");
        host.Renderer.RemoveRootComponent(root);
        Assert.Throws<ArgumentException>(() => engine.Trace(top));
    }
}
