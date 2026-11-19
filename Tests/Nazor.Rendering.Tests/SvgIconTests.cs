using System.Text;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using ImageControl = Nazor.Controls.Image;

namespace Nazor.Rendering.Tests;

public sealed class IconBar : ComponentBase
{
    public ImageSource? Picture { get; set; }
    public ImageSource? Symbol { get; set; }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(IconBar));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<ImageControl>(3, typeof(IconBar));
            page.AddComponentParameter(4, "Class", "picture");
            page.AddComponentParameter(5, "Source", Picture);
            page.CloseComponent();
            page.OpenComponent<Icon>(6, typeof(IconBar));
            page.AddComponentParameter(7, "Class", "symbol");
            page.AddComponentParameter(8, "Source", Symbol);
            page.CloseComponent();
            page.OpenComponent<Icon>(9, typeof(IconBar));
            page.AddComponentParameter(10, "Class", "glyph");
            page.AddComponentParameter(11, "Glyph", "");
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class SvgIconTests : IDisposable
{
    private const string TwoShapes = """
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="12" viewBox="0 0 48 24">
          <title>Two shapes</title>
          <rect width="24" height="24" fill="red"/>
          <circle cx="36" cy="12" r="12" style="fill: #0000ff"/>
        </svg>
        """;

    private readonly RenderHarness harness = new();
    private RootComponent root = null!;

    public SvgIconTests()
    {
        var result = NssCompiler.Compile("IconBar", """
            .page { display: flex; flex-direction: column; align-items: flex-start; background: #ffffff; }
            .picture { width: 48px; }
            .symbol { color: #ff00ff; width: 24px; height: 24px; }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(IconBar), () => sheet);
    }

    public void Dispose() => harness.Dispose();

    private static ImageSource Svg(string text) => ImageSource.FromBytes(Encoding.UTF8.GetBytes(text));

    private DecodedImage? Decode(string text) => harness.Graphics.Images.Decode(Encoding.UTF8.GetBytes(text));

    private RenderedImage Show(ImageSource? picture, ImageSource? symbol)
    {
        root = harness.Host.Render<IconBar>();
        var bar = (IconBar)root.Component;
        bar.Picture = picture;
        bar.Symbol = symbol;
        bar.Refresh();
        harness.Host.RunPending();
        harness.Render(200, 200, 1);
        Assert.True(harness.Scene.WhenImagesLoaded().Wait(TimeSpan.FromSeconds(10)));
        return harness.Render(200, 200, 1);
    }

    private ElementNode Element(string name) => root.Elements().Single(e => e.Class == name);

    [Fact]
    public void An_svg_is_a_vector_image_with_its_size_and_view_box()
    {
        var image = Decode(TwoShapes)!;
        Assert.True(image.IsVector);
        Assert.Equal(new Graphics.Size(24, 12), image.Size);
        Assert.Equal(new Graphics.Size(48, 24), Decode("""<svg viewBox="0 0 48 24"/>""")!.Size);
        Assert.Equal(new Graphics.Size(10, 5), Decode("""<svg width="10px" viewBox="0 0 48 24"/>""")!.Size);
        Assert.Equal(new Graphics.Size(300, 150), Decode("<svg/>")!.Size);
        Assert.Null(Decode("<html><body/></html>"));
        Assert.Null(Decode("<svg"));
    }

    [Fact]
    public void An_svg_draws_sharp_at_the_size_of_its_box()
    {
        var image = Show(Svg(TwoShapes), null);
        Assert.Equal(new Graphics.Size(48, 24), harness.Scene.GetBounds(Element("picture")).Size);
        Assert.Equal(new Graphics.Color(255, 0, 0, 255), image.GetPixel(12, 12));
        Assert.Equal(new Graphics.Color(0, 0, 255, 255), image.GetPixel(36, 12));
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), image.GetPixel(25, 1));
    }

    [Fact]
    public void Strokes_transforms_groups_and_opacity()
    {
        var image = Show(Svg("""
            <svg width="40" height="20" viewBox="0 0 40 20">
              <g fill="none" stroke="rgb(0, 128, 0)" stroke-width="4">
                <line x1="0" y1="2" x2="20" y2="2"/>
              </g>
              <rect x="0" y="0" width="10" height="10" fill="#000" transform="translate(30 10)"/>
              <g opacity="0.5"><rect x="20" y="10" width="10" height="10" fill="black"/></g>
              <rect x="0" y="10" width="10" height="10" fill="red" visibility="hidden"/>
            </svg>
            """), null);
        Assert.Equal(new Graphics.Color(0, 128, 0, 255), image.GetPixel(10, 2));
        Assert.Equal(new Graphics.Color(0, 0, 0, 255), image.GetPixel(42, 18));
        var half = image.GetPixel(25, 15);
        Assert.InRange(half.R, 120, 135);
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), image.GetPixel(5, 15));
    }

    [Fact]
    public void An_icon_draws_its_image_in_its_color()
    {
        var image = Show(null, Svg("""<svg viewBox="0 0 10 10"><rect width="10" height="10"/></svg>"""));
        var draw = harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single();
        Assert.Equal(new Graphics.Color(255, 0, 255, 255), draw.Tint);
        Assert.Equal(new Graphics.Size(24, 24), harness.Scene.GetBounds(Element("symbol")).Size);
        var bounds = harness.Scene.GetBounds(Element("symbol"));
        Assert.Equal(new Graphics.Color(255, 0, 255, 255), image.GetPixel((int)bounds.X + 12, (int)bounds.Y + 12));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void A_small_svg_preserves_partial_coverage_along_a_thin_diagonal(float scale)
    {
        using var icon = Assert.IsType<Nazor.Skia.SkiaVectorImage>(Decode("""
            <svg viewBox="0 0 96 96">
                <path d="M 8 10 L 88 86" stroke="black" stroke-width="3"/>
            </svg>
            """));
        var list = new DisplayList(new Size(16, 16), [new DrawImage(icon, new Rect(1.25f, 1.25f, 12, 12), null, null)]);
        var image = harness.Graphics.RenderToImage(list, scale, Graphics.Color.White);
        var rowsWithEdge = Enumerable.Range((int)(3 * scale), (int)(8 * scale))
            .Count(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).R is > 0 and < 240));
        Assert.Equal((int)(8 * scale), rowsWithEdge);
    }

    [Fact]
    public void A_glyph_icon_is_one_em_of_the_icon_font()
    {
        Show(null, null);
        var glyph = Element("glyph");
        Assert.Equal("", glyph.Text());
        var em = (float)harness.Scene.GetStyle(glyph).Get(StyleProperties.FontSize).Value;
        Assert.Equal(new Graphics.Size(em, em), harness.Scene.GetBounds(glyph).Size);
        Assert.Equal(["Segoe Fluent Icons", "Segoe MDL2 Assets"], harness.Scene.GetStyle(glyph).Get(StyleProperties.FontFamily).Items);
    }

    [Fact]
    public void Documents_with_entities_read_no_files()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nazor-secret-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "secret");
        try
        {
            var image = Decode($"""<?xml version="1.0"?><!DOCTYPE svg [<!ENTITY x SYSTEM "{new Uri(path)}">]><svg width="1" height="1"><title>&x;</title></svg>""");
            Assert.True(image is null || image.IsVector);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
