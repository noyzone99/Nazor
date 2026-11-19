using Nazor.Graphics.Text;

namespace Nazor.Rendering.Tests;

public sealed class ShapedTextTests : IDisposable
{
    private const string Text = "Il gestionale degli ordini mostra per ogni cliente gli ordini aperti e le consegne previste.";

    private readonly RenderHarness harness = new();

    public void Dispose() => harness.Dispose();

    private static string Lines(TextLayout layout) =>
        string.Join("|", layout.Lines.Select(l => $"{l.Start}-{l.End}:{l.Width:F2}@{l.Top:F2}"));

    [Fact]
    public void Layouts_of_shaped_text_are_the_ones_of_text_shaped_for_them()
    {
        var style = new TextStyle(["Fira Sans"], 14);
        var shaped = new ShapedText(Text, style, harness.Graphics.Fonts, harness.Graphics.Shaper);
        foreach (float? width in new float?[] { null, 300, 180, 90, 20 })
        {
            var reused = new TextLayout(shaped, width);
            var fresh = new TextLayout(Text, style, width, harness.Graphics.Fonts, harness.Graphics.Shaper);
            Assert.Equal(Lines(fresh), Lines(reused));
            Assert.Equal(fresh.MinContentWidth, reused.MinContentWidth);
            Assert.Equal(fresh.MaxContentWidth, reused.MaxContentWidth);
            Assert.Same(shaped.Runs, reused.Runs);
        }
        Assert.Equal(shaped.MaxContentWidth, new TextLayout(shaped, null).Width, 0.01f);
    }

    [Fact]
    public void Equal_styles_have_equal_hashes()
    {
        var a = new TextStyle(["Fira Sans", "Segoe UI"], 14, 700);
        var b = new TextStyle(new List<string> { "Fira Sans", "Segoe UI" }, 14, 700);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), (a with { FontFamilies = ["Segoe UI", "Fira Sans"] }).GetHashCode());
    }
}
