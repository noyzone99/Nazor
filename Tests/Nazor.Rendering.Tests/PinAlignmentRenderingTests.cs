using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class PinAlignmentPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<PinField>(0);
        builder.AddComponentParameter(1, nameof(PinField.Id), "pin");
        builder.AddComponentParameter(2, nameof(PinField.Length), 4);
        builder.AddComponentParameter(3, nameof(PinField.Value), "5555");
        builder.AddComponentParameter(4, nameof(PinField.Mask), true);
        builder.CloseComponent();
    }
}

public sealed class PinAlignmentRenderingTests
{
    [Theory]
    [InlineData("basic", 1f)]
    [InlineData("basic", 1.5f)]
    [InlineData("basic", 2f)]
    [InlineData("fluent", 1f)]
    [InlineData("fluent", 1.5f)]
    [InlineData("fluent", 2f)]
    [InlineData("leaf", 1f)]
    [InlineData("leaf", 1.5f)]
    [InlineData("leaf", 2f)]
    public void Pin_masks_and_editing_geometry_are_centered_at_normal_and_enlarged_font_sizes(string theme, float scale)
    {
        using var harness = new RenderHarness();
        foreach (var file in NazorLeafTheme.FontFiles)
            ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        var maskFace = harness.Graphics.Fonts.Match(["Geist Mono"], 400, false);
        Assert.True(maskFace.HasGlyph('●'), "The deterministic PIN font must contain the default mask character.");
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = theme switch
        {
            "leaf" => NazorLeafTheme.Theme,
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            _ => Nazor.Themes.Themes.Basic,
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<PinAlignmentPage>();

        foreach (var font in new[] { "", "font-size: 32px;" })
        {
            input.Focus(null, visible: false);
            var style = NssCompiler.Compile("pin-alignment", $$"""
                PinField { padding: 12px; }
                PinFieldCell { font-family: "Geist Mono"; color: #000000; background: #ffffff; border-color: transparent; box-shadow: none; {{font}} }
                PinFieldCell:focus { outline-style: none; border-color: transparent; box-shadow: none; }
                """);
            Assert.True(style.Success, string.Join("\n", style.Diagnostics));
            harness.Scene.Styles.Application = style.StyleSheet;
            var image = harness.Render(360, 180, scale);
            var imagePath = SaveImage(harness, image, $"pin-{theme}-{scale:0.0}-{(font.Length == 0 ? "normal" : "large")}");

            foreach (var cell in root.Elements("PinFieldCell"))
            {
                var content = harness.Scene.GetContentBox(cell);
                var layout = harness.Scene.GetEditableLayout(cell)!;
                Assert.InRange(MathF.Abs(content.Height - layout.Height), 0, 1 / scale);
                var pixels = new List<Point>();
                for (var y = (int)MathF.Ceiling(content.Y * scale); y < content.Bottom * scale; y++)
                for (var x = (int)MathF.Ceiling(content.X * scale); x < content.Right * scale; x++)
                {
                    var color = image.GetPixel(x, y);
                    if (color.R < 100 && color.G < 100 && color.B < 100) pixels.Add(new Point(x + 0.5f, y + 0.5f));
                }
                Assert.True(pixels.Count > 0, $"No mask pixels in {content}; font {layout.Style.FontSize}, {theme}, scale {scale}, image {imagePath}.");
                var centerX = (pixels.Min(point => point.X) + pixels.Max(point => point.X)) / 2 / scale;
                var centerY = (pixels.Min(point => point.Y) + pixels.Max(point => point.Y)) / 2 / scale;
                Assert.True(MathF.Abs(centerX - content.X - content.Width / 2) <= 2,
                    $"Mask center {centerX} differs from cell center {content.X + content.Width / 2}; font {layout.Style.FontSize}, {theme}, scale {scale}, image {imagePath}.");
                Assert.True(MathF.Abs(centerY - content.Y - content.Height / 2) <= 2,
                    $"Mask center {centerY} differs from cell center {content.Y + content.Height / 2}; font {layout.Style.FontSize}, {theme}, scale {scale}, image {imagePath}.");

                input.Focus(cell, visible: false);
                harness.Render(360, 180, scale);
                var caret = input.GetTextCaretRect()!.Value;
                Assert.InRange(MathF.Abs(content.Y + content.Height / 2 - caret.Y - caret.Height / 2), 0, 1 / scale);
                var offset = harness.Scene.GetEditableLineOffset(cell, layout.Lines[0]);
                Assert.Equal(0, input.GetTextIndexAt(cell, new Point(content.X + offset, content.Y + content.Height / 2)));
                Assert.Equal(1, input.GetTextIndexAt(cell, new Point(content.X + offset + layout.Width, content.Y + content.Height / 2)));
            }
        }
    }

    private static string SaveImage(RenderHarness harness, RenderedImage image, string name,
        [System.Runtime.CompilerServices.CallerFilePath] string source = "")
    {
        var directory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../../artifacts/rendering"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".png");
        File.WriteAllBytes(path, harness.Graphics.EncodePng(image));
        return path;
    }
}
