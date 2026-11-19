using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Rendering.Tests;

public sealed class Paragraph : ComponentBase
{
    [Parameter] public string Text { get; set; } = "";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Paragraph));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<TextBlock>(3, typeof(Paragraph));
            page.AddComponentParameter(4, "Class", "t");
            page.AddAttribute(5, "ChildContent", (RenderFragment)(text => text.AddContent(6, Text)));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class TextStyleTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    private DrawText Show(string text, string css, float width = 400)
    {
        var result = NssCompiler.Compile("Paragraph", ".page { display: flex; flex-direction: column; align-items: flex-start; } .t {" + css + "}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Paragraph), () => sheet);
        harness.Host.Render<Paragraph>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Text"] = text }));
        harness.Render(width, 300, 1);
        return harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Last();
    }

    [Fact]
    public void Letter_spacing_adds_space_after_every_character()
    {
        var plain = Show("abc", "").Layout;
        var spaced = Show("abc", "letter-spacing: 10px;").Layout;
        Assert.Equal(plain.Width + 30, spaced.Width, 3);
        var tight = Show("abc", "letter-spacing: -1px;").Layout;
        Assert.Equal(plain.Width - 3, tight.Width, 3);
    }

    [Fact]
    public void A_zero_font_size_hides_the_text_without_failing()
    {
        // Themes hide the text of a part this way, for example to draw an icon in ::before instead.
        var hidden = Show("+", "font-size: 0;").Layout;
        Assert.True(hidden.Width < 0.1f);
        Assert.True(hidden.Height < 0.1f);
    }

    [Theory]
    [InlineData("uppercase", "Ciao, mondo città", "CIAO, MONDO CITTÀ")]
    [InlineData("lowercase", "Ciao MONDO", "ciao mondo")]
    [InlineData("capitalize", "l'ordine di prova-uno", "L'ordine Di Prova-Uno")]
    public void Text_transform_changes_the_displayed_text(string transform, string text, string expected) =>
        Assert.Equal(expected, Show(text, $"text-transform: {transform};").Layout.Text);

    [Fact]
    public void Pre_wrap_keeps_spaces_and_breaks_and_still_wraps()
    {
        var layout = Show("uno  due\ntre quattro cinque sei sette otto nove", "white-space: pre-wrap; width: 120px;").Layout;
        Assert.StartsWith("uno  due\ntre", layout.Text);
        Assert.True(layout.Lines.Count > 2, $"{layout.Lines.Count} lines");
        Assert.All(layout.Lines, l => Assert.True(l.Width <= 120.01f, $"{l.Width}"));
    }

    [Fact]
    public void Pre_line_collapses_spaces_but_keeps_breaks()
    {
        var layout = Show("uno   due  \n   tre", "white-space: pre-line;").Layout;
        Assert.Equal("uno due\ntre", layout.Text);
        Assert.Equal(2, layout.Lines.Count);
    }

    [Fact]
    public void Overflow_wrap_breaks_a_word_that_does_not_fit()
    {
        const string word = "Supercalifragilistichespiralidoso";
        var normal = Show(word, "width: 80px;").Layout;
        Assert.Single(normal.Lines);
        Assert.True(normal.Width > 80);

        var broken = Show(word, "width: 80px; overflow-wrap: break-word;").Layout;
        Assert.True(broken.Lines.Count > 2);
        Assert.All(broken.Lines, l => Assert.True(l.Width <= 80.01f, $"{l.Width}"));
        Assert.Equal(word, string.Concat(broken.Lines.Select(l => broken.Text[l.Start..l.End])));
    }

    [Fact]
    public void Text_decoration_reaches_the_text_with_the_color_where_it_was_declared()
    {
        var text = Show("Ordine", "text-decoration: underline line-through wavy #ff0000 2px;");
        Assert.Equal(new TextDecoration(DecorationLines.Underline | DecorationLines.LineThrough, new Nazor.Graphics.Color(255, 0, 0, 255), DecorationStyle.Wavy, 2), text.Decoration);

        var result = NssCompiler.Compile("Paragraph", ".page { color: #0000ff; text-decoration: underline; } .t { color: #00ff00; }");
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Paragraph), () => sheet);
        harness.Host.Render<Paragraph>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Text"] = "Cliente" }));
        harness.Render(400, 300, 1);
        var inherited = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Last();
        Assert.Equal(new Nazor.Graphics.Color(0, 0, 255, 255), inherited.Decoration!.Color);
        Assert.Equal(new Nazor.Graphics.Color(0, 255, 0, 255), inherited.Color);
    }

    [Fact]
    public void An_underline_is_drawn_below_the_baseline_across_spaces()
    {
        var text = Show("i     i", "text-decoration: underline; color: #000000; font-size: 20px;");
        var image = harness.Render(400, 300, 1);
        var line = text.Layout.Lines[0];
        var middle = (int)(text.Origin.X + line.Width / 2);
        var below = Enumerable.Range((int)(text.Origin.Y + line.Baseline), 6).Select(y => image.GetPixel(middle, y)).ToArray();
        Assert.Contains(below, p => p.R < 100);
        var plain = Show("i     i", "color: #000000; font-size: 20px;");
        var clear = harness.Render(400, 300, 1);
        Assert.All(Enumerable.Range((int)(plain.Origin.Y + line.Baseline), 6), y => Assert.True(clear.GetPixel(middle, y).R > 200));
    }

    [Fact]
    public void Text_overflow_ellipsis_cuts_the_line_that_does_not_fit()
    {
        var cut = Show("Ordine numero 2026-00042 del cliente", "width: 90px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;");
        Assert.EndsWith("…", cut.Layout.Text);
        Assert.StartsWith("Ordine", cut.Layout.Text);
        Assert.True(cut.Layout.Width <= 90.01f, $"{cut.Layout.Width}");

        Assert.Equal("Ordine numero 2026-00042 del cliente", Show("Ordine numero 2026-00042 del cliente", "width: 90px; white-space: nowrap; text-overflow: ellipsis;").Layout.Text);
        Assert.Equal("Breve", Show("Breve", "width: 90px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;").Layout.Text);
    }

    [Fact]
    public void Overflow_wrap_anywhere_shrinks_the_min_content_width()
    {
        const string word = "Precipitevolissimevolmente";
        var normal = Show(word, "").Layout;
        var anywhere = Show(word, "overflow-wrap: anywhere;").Layout;
        Assert.Equal(normal.MaxContentWidth, normal.MinContentWidth, 3);
        Assert.True(anywhere.MinContentWidth < 20, $"{anywhere.MinContentWidth}");
    }
}
