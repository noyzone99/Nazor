using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class PseudoElementTests
{
    [Theory]
    [InlineData(".a::before", PseudoElement.Before)]
    [InlineData(".a::after", PseudoElement.After)]
    [InlineData(".a::placeholder", PseudoElement.Placeholder)]
    [InlineData(".a::selection", PseudoElement.Selection)]
    public void Targets_round_trip_without_matching_the_origin(string selector, PseudoElement target)
    {
        var sheet = Compile(selector + " { color: red; }");
        var rule = Assert.Single(sheet.Rules);
        Assert.Equal(target, rule.Selector.Subject.PseudoElement);
        Assert.Equal(new Specificity(0, 1, 1), rule.Selector.Specificity);
        Assert.Contains("PseudoElement." + target, CSharpEmitter.Emit(sheet));
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        Assert.Equal(sheet.Describe(), restored.Describe());
        using var host = new TestHost();
        var element = host.Render<Items>().Elements("Item").First();
        Assert.False(rule.Selector.Matches(element));
    }

    [Theory]
    [InlineData("Item::before:hover")]
    [InlineData("Item::before::after")]
    [InlineData("Item::before > Item")]
    [InlineData("Item::before .a")]
    [InlineData("Item::unknown")]
    [InlineData("Item::none")]
    [InlineData("Item:is(::before)")]
    public void Invalid_pseudo_element_positions_are_diagnosed(string selector)
    {
        var result = NssCompiler.Compile("invalid", selector + " { color: red; }");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Id == "NZS103");
    }

    [Fact]
    public void Generated_content_inherits_and_reacts_to_states_variables_and_sheet_replacement()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var element = root.Elements("Item").First();
        var originalChildren = element.Children.ToArray();
        engine.Application = Compile("""
            Item { color: blue; --label: "A" "B"; }
            Item { &::before { content: var(--label); color: inherit; } }
            Item:hover::before { color: red; }
            """);
        var box = Assert.IsType<ElementNode>(engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.Equal("AB", Assert.IsType<TextNode>(Assert.Single(box.Children)).Text);
        Assert.Equal(new Color(0, 0, 255, 255), engine.GetStyle(box).Color);
        Assert.Equal(originalChildren, element.Children);
        Assert.Same(element, box.ParentElement);
        element.SetState(ElementStates.Hover, true);
        Assert.Equal(new Color(255, 0, 0, 255), engine.GetStyle(box).Color);
        Assert.Equal(new Color(0, 0, 255, 255), engine.GetStyle(element).Color);
        engine.Application.Replace(Compile("Item::before { content: \"\"; width: 12px; }"));
        Assert.Same(box, engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.Empty(Assert.IsType<TextNode>(Assert.Single(box.Children)).Text);
        Assert.Null(engine.GetGeneratedElement(element, PseudoElement.After));
        engine.Application.Replace(Compile("Item::before { content: none; }"));
        Assert.Null(engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.True(box.IsRemoved);
        Assert.Equal(originalChildren, element.Children);
        engine.Application.Replace(Compile("Item::before { content: \"restored\"; }"));
        Assert.Same(box, engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.False(box.IsRemoved);
        ((Items)root.Component).Rows.RemoveAt(0);
        ((Items)root.Component).Refresh();
        host.RunPending();
        Assert.True(box.IsRemoved);
    }

    [Theory]
    [InlineData("url(icon.png)")]
    [InlineData("attr(title)")]
    [InlineData("counter(item)")]
    [InlineData("12")]
    [InlineData("none \"text\"")]
    public void Unsupported_content_values_fail_instead_of_becoming_literal_text(string value)
    {
        Assert.False(NssCompiler.Compile("invalid", $"Item::before {{ content: {value}; }}").Success);
    }

    [Fact]
    public void Text_portions_filter_properties_and_keep_inline_styles_on_the_origin()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var element = host.Render<Items>().Elements("Item").First();
        engine.Application = Compile("""
            Item { color: blue; font-size: 18px; }
            Item::selection { color: white; background-color: red; font-size: 80px; }
            Item::placeholder { color: green; font-size: 24px; width: 500px; opacity: .5; }
            """);
        var selection = engine.GetPseudoStyle(element, PseudoElement.Selection);
        Assert.Equal(Color.White, selection.Color);
        Assert.Equal(new Color(255, 0, 0, 255), selection.BackgroundColor);
        Assert.Equal(18, selection.FontSize);
        var placeholder = engine.GetPseudoStyle(element, PseudoElement.Placeholder);
        Assert.Equal(new Color(0, 128, 0, 255), placeholder.Color);
        Assert.Equal(24, placeholder.FontSize);
        Assert.Equal(SizeValue.Auto, placeholder.Get(StyleProperties.Width));
        Assert.Equal(.5, placeholder.Opacity);
        Assert.Equal(new Color(0, 0, 255, 255), engine.GetStyle(element).Color);
    }

    [Fact]
    public void Scoped_pseudo_rules_respect_component_boundaries_and_deep()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        StyleRegistry.RegisterScoped(typeof(Page), () => Compile(".inner::after { content: \"scoped\"; }"));
        try
        {
            var root = host.Render<Page>();
            var element = root.Elements().First(e => e.Class == "projected inner");
            var hidden = root.Elements().First(e => e.Class == "inner");
            Assert.NotNull(engine.GetGeneratedElement(element, PseudoElement.After));
            Assert.Null(engine.GetGeneratedElement(element, PseudoElement.Before));
            Assert.Null(engine.GetGeneratedElement(hidden, PseudoElement.After));
            StyleRegistry.RegisterScoped(typeof(Page), () => Compile(".outer ::deep .inner::after { content: \"deep\"; }"));
            Assert.NotNull(engine.GetGeneratedElement(hidden, PseudoElement.After));
        }
        finally { StyleRegistry.RegisterScoped(typeof(Page), () => Compile("")); }
    }

    private static StyleSheet Compile(string source)
    {
        var result = NssCompiler.Compile("pseudo", source);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Pseudo_only_viewport_lengths_and_media_changes_invalidate_cached_values()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var element = host.Render<Items>().Elements("Item").First();
        engine.Application = Compile("""
            Item::before { content: "wide"; width: 10vw; }
            @media (max-width: 500px) { Item::before { content: "narrow"; } }
            """);
        var box = engine.GetGeneratedElement(element, PseudoElement.Before)!;
        Assert.Equal(SizeValue.Fixed(Length.Px(80)), engine.GetStyle(box).Get(StyleProperties.Width));
        engine.Viewport = new Viewport(400, 600);
        Assert.Same(box, engine.GetGeneratedElement(element, PseudoElement.Before));
        Assert.Equal("narrow", Assert.IsType<TextNode>(Assert.Single(box.Children)).Text);
        Assert.Equal(SizeValue.Fixed(Length.Px(40)), engine.GetStyle(box).Get(StyleProperties.Width));
        engine.Viewport = new Viewport(300, 600);
        Assert.Equal(SizeValue.Fixed(Length.Px(30)), engine.GetStyle(box).Get(StyleProperties.Width));
    }
}
