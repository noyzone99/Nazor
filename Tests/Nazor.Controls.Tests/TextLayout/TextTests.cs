using Nazor.Controls.Tests.TextLayout.Fixtures;
using Nazor.Graphics;
using Nazor.Style;
using Color = Nazor.Style.Color;

namespace Nazor.Controls.Tests.TextLayout;

[Collection("Controls")]
public sealed class TextTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private TextProbe Probe() => h.Component<TextProbe>();

    private void Refresh()
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(Probe().Refresh);
        h.Frame();
    }

    private static string? Marker(ElementNode item) =>
        Nodes.Text(item.Children.OfType<ElementNode>().Single(e => e.Name == "ListItemMarker"));

    [Fact]
    public void Paragraphs_are_text_spaced_from_each_other_and_optionally_selectable()
    {
        h.Render<TextProbe>();
        var first = h.ByClass("p1");
        var second = h.ByClass("p2");
        Assert.Equal("text", first.Attributes["role"]);
        Assert.Equal(false, first.Attributes["selectable"]);
        Assert.False(first.Attributes.ContainsKey("tabindex"));
        Assert.Equal(true, second.Attributes["selectable"]);
        Assert.Equal(-1, second.Attributes["tabindex"]);
        Assert.True(h.Bounds(second).Y - h.Bounds(first).Bottom >= 8);
    }

    [Fact]
    public void A_key_cap_is_text_with_a_border_and_a_monospaced_font()
    {
        h.Render<TextProbe>();
        var key = h.ByClass("key");
        Assert.Equal("text", key.Attributes["role"]);
        var style = h.Style(key);
        Assert.Equal(2, style.Get(StyleProperties.BorderBottomWidth).Value, 1);
        Assert.Equal(1, style.Get(StyleProperties.BorderTopWidth).Value, 1);
        Assert.True(h.Bounds(key).Width < 60);
    }

    [Fact]
    public void A_blockquote_is_a_quote_group_with_an_optional_source()
    {
        h.Render<TextProbe>();
        var quote = h.ByClass("quote");
        Assert.Equal("group", quote.Attributes["role"]);
        Assert.Equal("quote", quote.Attributes["role-description"]);
        var parts = quote.Children.OfType<ElementNode>().Select(e => e.Name).ToArray();
        Assert.Equal(["BlockquoteContent", "BlockquoteCite"], parts);
        Assert.Equal("Edsger W. Dijkstra", Nodes.Text(parts.Length == 2 ? quote.Children.OfType<ElementNode>().Last() : quote));
        Assert.Equal(["BlockquoteContent"], h.ByClass("bare").Children.OfType<ElementNode>().Select(e => e.Name));
        Assert.Equal(3, h.Style(quote).Get(StyleProperties.BorderLeftWidth).Value, 1);
    }

    [Fact]
    public void An_unordered_list_uses_discs_then_circles_then_squares_when_nested()
    {
        h.Render<TextProbe>();
        var bullets = h.ByClass("bullets");
        Assert.Equal("list", bullets.Attributes["role"]);
        Assert.Equal(1, bullets.Attributes["level"]);
        Assert.Equal("disc", bullets.Attributes["marker-style"]);
        Assert.Equal("•", Marker(h.ByClass("b1")));
        Assert.Equal(2, h.ByClass("nested").Attributes["level"]);
        Assert.Equal("◦", Marker(h.ByClass("n1")));
        Assert.Equal("square", h.ByClass("deeper").Attributes["marker-style"]);
        Assert.Equal("▪", Marker(h.ByClass("d1")));
        var marker = h.ByClass("b1").Children.OfType<ElementNode>().First();
        Assert.Equal("ListItemMarker", marker.Name);
        Assert.Equal(true, marker.Attributes["semantic-hidden"]);
        Assert.Equal("ListItemContent", h.ByClass("b1").Children.OfType<ElementNode>().Last().Name);
    }

    [Fact]
    public void List_items_know_their_position_and_the_size_of_their_own_list()
    {
        h.Render<TextProbe>();
        Assert.Equal("listitem", h.ByClass("b2").Attributes["role"]);
        Assert.Equal(2, h.ByClass("b2").Attributes["position-in-set"]);
        Assert.Equal(3, h.ByClass("b2").Attributes["size-of-set"]);
        Assert.Equal(1, h.ByClass("n1").Attributes["size-of-set"]);
        Probe().ShowThird = false;
        Refresh();
        Assert.Equal(2, h.ByClass("b1").Attributes["size-of-set"]);
        Probe().ShowThird = true;
        Refresh();
        Assert.Equal(3, h.ByClass("b3").Attributes["position-in-set"]);
        Assert.Equal(3, h.ByClass("b1").Attributes["size-of-set"]);
    }

    [Fact]
    public void An_ordered_list_numbers_from_start_honors_values_and_counts_down_when_reversed()
    {
        h.Render<TextProbe>();
        Assert.Equal(["1.", "2.", "3."], new[] { "o1", "o2", "o3" }.Select(c => Marker(h.ByClass(c))));
        Assert.Equal(true, h.ByClass("numbers").Attributes["ordered"]);
        Probe().Start = 5;
        Refresh();
        Assert.Equal(["5.", "6.", "7."], new[] { "o1", "o2", "o3" }.Select(c => Marker(h.ByClass(c))));
        Probe().Override = 10;
        Refresh();
        Assert.Equal(["5.", "10.", "11."], new[] { "o1", "o2", "o3" }.Select(c => Marker(h.ByClass(c))));
        Probe().Override = null;
        Probe().Start = null;
        Probe().Reversed = true;
        Refresh();
        Assert.Equal(["3.", "2.", "1."], new[] { "o1", "o2", "o3" }.Select(c => Marker(h.ByClass(c))));
        Assert.Equal(true, h.ByClass("numbers").Attributes["reversed"]);
    }

    [Theory]
    [InlineData(ListMarkerStyle.LowerAlpha, "a.", "b.", "lower-alpha")]
    [InlineData(ListMarkerStyle.UpperAlpha, "A.", "B.", "upper-alpha")]
    [InlineData(ListMarkerStyle.LowerRoman, "i.", "ii.", "lower-roman")]
    [InlineData(ListMarkerStyle.UpperRoman, "I.", "II.", "upper-roman")]
    [InlineData(ListMarkerStyle.Square, "▪", "▪", "square")]
    [InlineData(ListMarkerStyle.Decimal, "1.", "2.", "decimal")]
    public void Marker_styles_format_the_numbers(ListMarkerStyle style, string first, string second, string name)
    {
        h.Render<TextProbe>();
        Probe().Style = style;
        Refresh();
        Assert.Equal(first, Marker(h.ByClass("o1")));
        Assert.Equal(second, Marker(h.ByClass("o2")));
        Assert.Equal(name, h.ByClass("numbers").Attributes["marker-style"]);
    }

    [Fact]
    public void Marker_formatting_handles_large_and_out_of_range_numbers()
    {
        Assert.Equal("AA.", List.Marker(ListMarkerStyle.UpperAlpha, 27));
        Assert.Equal("az.", List.Marker(ListMarkerStyle.LowerAlpha, 52));
        Assert.Equal("MCMXCIV.", List.Marker(ListMarkerStyle.UpperRoman, 1994));
        Assert.Equal("0.", List.Marker(ListMarkerStyle.LowerRoman, 0));
        Assert.Equal("4000.", List.Marker(ListMarkerStyle.UpperRoman, 4000));
        Assert.Equal("-1.", List.Marker(ListMarkerStyle.LowerAlpha, -1));
        Assert.Equal("", List.Marker(ListMarkerStyle.None, 3));
    }

    [Fact]
    public void A_list_without_markers_hides_the_marker_part_but_stays_a_list()
    {
        h.Render<TextProbe>();
        Assert.Equal("list", h.ByClass("silent").Attributes["role"]);
        var marker = h.ByClass("s1").Children.OfType<ElementNode>().First();
        Assert.Equal(Display.None, h.Style(marker).Display);
    }

    [Fact]
    public void A_list_item_outside_a_list_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => h.Render<OrphanItemProbe>());
    }

    [Fact]
    public void A_definition_list_has_terms_and_definitions_stacked_or_inline()
    {
        h.Render<TextProbe>();
        var list = h.ByClass("terms");
        Assert.Equal("definitionlist", list.Attributes["role"]);
        Assert.Equal("stacked", list.Attributes["layout"]);
        Assert.Equal("term", h.ByClass("t1").Attributes["role"]);
        Assert.Equal("definition", h.ByClass("dd1").Attributes["role"]);
        Assert.True(h.Bounds(h.ByClass("dd1")).Y >= h.Bounds(h.ByClass("t1")).Bottom - 0.5);
        Probe().Layout = DefinitionListLayout.Inline;
        Refresh();
        Assert.Equal("inline", list.Attributes["layout"]);
        Assert.Equal(h.Bounds(h.ByClass("t1")).Y, h.Bounds(h.ByClass("dd1")).Y, 1);
        Assert.True(h.Bounds(h.ByClass("dd1")).X > h.Bounds(h.ByClass("t1")).Right);
        Assert.True(h.Bounds(h.ByClass("dd2")).Y > h.Bounds(h.ByClass("dd1")).Bottom - 0.5);
    }

    [Fact]
    public void A_code_block_normalizes_the_code_and_numbers_its_lines()
    {
        h.Render<TextProbe>();
        var block = h.ByClass("code");
        Assert.Equal("csharp", block.Attributes["language"]);
        Assert.False(block.Attributes.ContainsKey("role"));
        var parts = block.Children.OfType<ElementNode>().ToArray();
        Assert.Equal(["CodeBlockLineNumbers", "CodeBlockCode"], parts.Select(p => p.Name));
        Assert.Equal("1\n2\n3", Nodes.Text(parts[0]));
        Assert.Equal(true, parts[0].Attributes["semantic-hidden"]);
        Assert.Equal("var a = 1;\nvar b = 2;\nvar c = 3;", parts[1].Children.OfType<TextNode>().Single().Text);
        Assert.Equal("text", parts[1].Attributes["role"]);
        Assert.Equal(true, parts[1].Attributes["selectable"]);
        Assert.Equal(-1, parts[1].Attributes["tabindex"]);
        Probe().First = 10;
        Refresh();
        Assert.Equal("10\n11\n12", Nodes.Text(block.Children.OfType<ElementNode>().First()));
        Probe().Numbers = false;
        Probe().Wrap = true;
        Refresh();
        Assert.Equal(["CodeBlockCode"], block.Children.OfType<ElementNode>().Select(p => p.Name));
        Assert.Equal(true, block.Attributes["wrap"]);
    }

    [Fact]
    public void A_code_block_counts_the_lines_of_its_content_and_becomes_a_group_when_named()
    {
        h.Render<TextProbe>();
        var block = h.ByClass("code-child");
        Assert.Equal("group", block.Attributes["role"]);
        Assert.Equal("code", block.Attributes["role-description"]);
        Assert.Equal("1\n2\n3", Nodes.Text(block.Children.OfType<ElementNode>().First()));
        var family = h.Style(block.Children.OfType<ElementNode>().Last()).Get(StyleProperties.FontFamily);
        Assert.Contains("Consolas", family.ToString());
    }

    [Fact]
    public void A_figure_is_named_by_its_caption()
    {
        h.Render<TextProbe>();
        var figure = h.ByClass("figure");
        var caption = h.ByClass("caption");
        Assert.Equal("group", figure.Attributes["role"]);
        Assert.Equal("figure", figure.Attributes["role-description"]);
        Assert.Equal("text", caption.Attributes["role"]);
        Assert.Equal(caption.Attributes["id"], figure.Attributes["labeled-by"]);
        Probe().ShowCaption = false;
        Refresh();
        Assert.False(figure.Attributes.ContainsKey("labeled-by"));
    }

    [Fact]
    public void The_text_parts_render_with_the_basic_theme()
    {
        h.Render<TextProbe>();
        var image = h.Image();
        Assert.True(image.Width > 0);
        var code = h.Bounds(h.ByClass("code"));
        var numbers = h.Bounds(h.ByClass("code").Children.OfType<ElementNode>().First());
        Assert.True(numbers.Width > 8 && numbers.Right < code.Right);
        Assert.NotEqual(Color.Transparent, h.Style(h.ByClass("code")).BackgroundColor);
    }
}
