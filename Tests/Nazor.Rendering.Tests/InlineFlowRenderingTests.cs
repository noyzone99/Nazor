using System.Globalization;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class InlineFlowRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    public static IEnumerable<object[]> ThemesAndScales()
    {
        foreach (var theme in new[] { "basic", "fluent", "leaf" })
        foreach (var scale in new[] { 1f, 1.5f, 2f }) yield return [theme, scale];
    }

    [Theory]
    [MemberData(nameof(ThemesAndScales))]
    public void Mixed_text_uses_one_shared_layout_and_real_retained_inline_geometry(string theme, float scale)
    {
        harness.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        var root = harness.Host.Render<InlineProbe>();
        harness.Render(230, 220, scale);
        var paragraph = root.Elements().Single(element => element.Id == "paragraph");
        var text = harness.Scene.GetEditableLayout(paragraph)!;
        Assert.Equal("Before Strong Ctrl after marked H2O and x2.", text.Text);
        Assert.True(text.Lines.Count > 1);
        var commands = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Where(command => command.Layout.Text == text.Text).ToArray();
        Assert.NotEmpty(commands);
        Assert.All(commands, command => Assert.Same(text, command.Layout));
        Assert.Contains(commands, command => command.TextLength is > 0 && command.TextStart > 0);
        Assert.Contains(commands, command => command.Decoration?.Lines == DecorationLines.LineThrough);
        var strong = harness.Scene.GetBounds(root.Elements().Single(element => element.Id == "strong"));
        var key = harness.Scene.GetBounds(root.Elements().Single(element => element.Id == "key"));
        Assert.True(strong.Right <= key.Right);
        Assert.True(strong.Y < harness.Scene.GetBounds(paragraph).Bottom);
        Assert.True(key.Width > 20);
        Assert.Equal(MathF.Round(key.X * scale), key.X * scale, 0.001f);
        Assert.Equal(MathF.Round(key.Y * scale), key.Y * scale, 0.001f);
        var lower = text.Runs.Single(run => run.Start == text.Text.IndexOf('2'));
        Assert.Contains(lower.OffsetsY, value => value < 0);
        Assert.Contains(text.Runs, run => run.Size < text.Style.FontSize && run.OffsetsY.Any(value => value > 0));
    }

    [Theory]
    [InlineData("it-IT")]
    [InlineData("tr-TR")]
    public void Inline_whitespace_preserves_nonbreaking_spaces_and_explicit_breaks_in_each_culture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var root = harness.Host.Render<WhitespaceProbe>();
            harness.Render(250, 160, 1);
            var text = harness.Scene.GetEditableLayout(root.Single("Paragraph"))!;
            Assert.Equal("Città café A B\nnext", text.Text);
            Assert.Equal(2, text.Lines.Count);
            var accented = text.Text.IndexOf('e');
            Assert.Equal(accented + 2, text.NextCaretIndex(accented));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Raised_nested_text_remains_hittable_outside_its_inline_parent_strut()
    {
        var root = harness.Host.Render<RaisedProbe>();
        harness.Render(260, 130, 1.5f);
        var link = root.Single("Link");
        var child = root.Single("Strong");
        var parentBounds = harness.Scene.GetBounds(link);
        var bounds = harness.Scene.GetBounds(child);
        Assert.True(bounds.Y + 2 < parentBounds.Y);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + 2);
        Assert.False(parentBounds.Contains(point));
        Assert.Same(child, harness.Scene.HitTest(point));
    }

    [Fact]
    public void Source_range_paint_reuses_kerning_and_clips_decorations_to_the_selected_run()
    {
        var style = new TextStyle(["Fira Sans"], 30);
        var text = new TextLayout("AV", style, null, harness.Graphics.Fonts, harness.Graphics.Shaper);
        var builder = new DisplayListBuilder();
        builder.Add(new DrawText(text, new Point(5, 5), new Color(255, 0, 0, 255)) { TextLength = 1 });
        builder.Add(new DrawText(text, new Point(5, 5), new Color(0, 128, 0, 255), Decoration:
            new TextDecoration(DecorationLines.Underline, new Color(0, 0, 255, 255), DecorationStyle.Solid, 2)) { TextStart = 1, TextLength = 1 });
        var image = harness.Graphics.RenderToImage(builder.Build(new Size(100, 70)), 1, Color.White);
        var reds = 0;
        var greens = 0;
        var blues = new List<int>();
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var pixel = image.GetPixel(x, y);
            if (pixel.R > 180 && pixel.G < 90 && pixel.B < 90) reds++;
            if (pixel.G > 80 && pixel.R < 90 && pixel.B < 90) greens++;
            if (pixel.B > 180 && pixel.R < 90 && pixel.G < 90) blues.Add(x);
        }
        Assert.True(reds > 10 && greens > 10);
        Assert.NotEmpty(blues);
        Assert.True(blues.Min() >= MathF.Floor(5 + text.GetCaretX(1)));
        Assert.True(blues.Max() <= MathF.Ceiling(5 + text.Width));
    }

    private sealed class InlineProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Paragraph");
            builder.AddAttribute(1, "id", "paragraph");
            builder.AddAttribute(2, "style", "font-family: Fira Sans; font-size: 16px; line-height: 1.5; margin: 0; padding: 4px");
            builder.AddContent(3, "Before ");
            Inline(builder, 10, "Strong", "Strong", "strong");
            builder.AddContent(20, " "); Inline(builder, 21, "Kbd", "Ctrl", "key");
            builder.AddContent(30, " after "); Inline(builder, 31, "Mark", "marked", "mark");
            builder.AddContent(40, " H"); Inline(builder, 41, "Subscript", "2", "lower");
            builder.AddContent(50, "O and x"); Inline(builder, 51, "Superscript", "2", "upper");
            Inline(builder, 61, "Deleted", ".", "deleted");
            builder.CloseElement();
        }

        private static void Inline(RenderTreeBuilder builder, int sequence, string name, string text, string id)
        {
            builder.OpenElement(sequence, name); builder.AddAttribute(sequence + 1, "id", id);
            builder.AddContent(sequence + 2, text); builder.CloseElement();
        }
    }

    private sealed class WhitespaceProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Paragraph"); builder.AddAttribute(1, "style", "margin: 0; font-family: Fira Sans");
            builder.AddContent(2, "  Città\n "); builder.OpenElement(3, "Strong"); builder.AddContent(4, " café  "); builder.CloseElement();
            builder.OpenElement(5, "Span"); builder.AddContent(6, " A B "); builder.CloseElement();
            builder.OpenElement(7, "LineBreak"); builder.AddAttribute(8, "line-break", true); builder.CloseElement();
            builder.AddContent(9, " next  "); builder.CloseElement();
        }
    }

    private sealed class RaisedProbe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Paragraph");
            builder.AddAttribute(1, "style", "margin: 0; font-family: Fira Sans; font-size: 14px; line-height: 1.2");
            builder.AddContent(2, "Before "); builder.OpenElement(3, "Link");
            builder.OpenElement(4, "Strong");
            builder.AddAttribute(5, "style", "font-size: 32px; position: relative; top: -10px");
            builder.AddContent(6, "Raised"); builder.CloseElement(); builder.CloseElement(); builder.CloseElement();
        }
    }
}
