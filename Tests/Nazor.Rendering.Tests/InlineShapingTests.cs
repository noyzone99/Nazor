using Nazor.Graphics.Text;

namespace Nazor.Rendering.Tests;

public sealed class InlineShapingTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    [Theory]
    [InlineData("AVATAR is a word", 45)]
    [InlineData("Città e café e 日本語", 65)]
    [InlineData("A👩‍💻B and text", 40)]
    public void Semantic_boundaries_keep_shared_shaping_caret_positions_and_wrapping(string text, float width)
    {
        var style = new TextStyle(["Fira Sans"], 18, BreakWords: BreakWords.Anywhere);
        var plain = new TextLayout(text, style, width, harness.Graphics.Fonts, harness.Graphics.Shaper);
        var split = new TextLayout(new ShapedText([new TextRun(text[..1], style), new TextRun(text[1..], style)],
            style, harness.Graphics.Fonts, harness.Graphics.Shaper), width);
        Assert.Equal(plain.Lines.Select(line => (line.Start, line.End, line.Width)), split.Lines.Select(line => (line.Start, line.End, line.Width)));
        for (var index = 0; index <= text.Length; index = split.NextCaretIndex(index))
        {
            Assert.Equal(plain.GetCaretRect(index), split.GetCaretRect(index));
            if (index == text.Length) break;
        }
        Assert.Equal(plain.MaxContentWidth, split.MaxContentWidth);
        Assert.Equal(plain.MinContentWidth, split.MinContentWidth);
    }

    [Fact]
    public void No_wrap_segments_keep_spaces_and_ignore_emergency_breaks()
    {
        var style = new TextStyle(["Fira Sans"], 14, BreakWords: BreakWords.Anywhere);
        var runs = new[] { new TextRun("before ", style), new TextRun("Ctrl + Shift", style, NoWrap: true), new TextRun(" after", style) };
        var shaped = new ShapedText(runs, style, harness.Graphics.Fonts, harness.Graphics.Shaper);
        var keyWidth = new TextLayout("Ctrl + Shift", style, null, harness.Graphics.Fonts, harness.Graphics.Shaper).Width;
        var layout = new TextLayout(shaped, 20);
        Assert.Contains(layout.Lines, line => layout.Text[line.Start..line.ContentEnd] == "Ctrl + Shift");
        Assert.Equal(keyWidth, shaped.MinContentWidth, 0.01f);
    }

    [Fact]
    public void Mixed_fonts_shifts_and_box_insets_determine_a_shared_line_baseline()
    {
        var style = new TextStyle(["Fira Sans"], 14);
        var big = style with { FontSize = 30, FontWeight = 700 };
        var normal = new TextLayout("base", style, null, harness.Graphics.Fonts, harness.Graphics.Shaper);
        var shaped = new ShapedText([new TextRun("base", style), new TextRun("Big", big, 5, 4, 6, TopInset: 3, BottomInset: 2)],
            style, harness.Graphics.Fonts, harness.Graphics.Shaper);
        var layout = new TextLayout(shaped, null);
        Assert.True(layout.Baseline > normal.Baseline);
        Assert.True(layout.Height > normal.Height);
        Assert.Equal(normal.Width + new TextLayout("Big", big, null, harness.Graphics.Fonts, harness.Graphics.Shaper).Width + 10, layout.Width, 0.01f);
        var raised = Assert.Single(layout.Runs, run => run.Size == 30);
        Assert.All(raised.OffsetsY, value => Assert.Equal(5, value, 0.01f));
    }
}
