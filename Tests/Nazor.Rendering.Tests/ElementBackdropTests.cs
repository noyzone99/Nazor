using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Rendering;
using SkiaSharp;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class ElementBackdropPage : ComponentBase
{
    public string Filter { get; set; } = "blur(4px) saturate(0)";
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page"); builder.AddAttribute(1, "style", "width: 100px; height: 80px; position: relative; background: red;");
        builder.OpenElement(2, "Stripe"); builder.AddAttribute(3, "style", "position: absolute; left: 50px; top: 0; width: 50px; height: 80px; background: blue;"); builder.CloseElement();
        builder.OpenElement(4, "Glass"); builder.AddAttribute(5, "style", "position: absolute; left: 20px; top: 20px; width: 60px; height: 40px; border-radius: 12px; backdrop-filter:" + Filter);
        builder.OpenElement(6, "Foreground"); builder.AddAttribute(7, "style", "position: absolute; left: 25px; top: 25px; width: 10px; height: 10px; background: lime;"); builder.CloseElement();
        builder.CloseElement(); builder.CloseElement();
    }
}

public sealed class ElementBackdropTests
{
    [Fact]
    public void Ordered_backdrop_colors_leave_foreground_unfiltered_and_none_removes_the_layer()
    {
        using var h = new RenderHarness();
        var root = h.Host.Render<ElementBackdropPage>();
        var page = (ElementBackdropPage)root.Component;
        page.Filter = "invert() brightness(.5)"; page.Refresh(); h.Host.RunPending();
        var image = h.Render(100,80,1.5f);
        var actual = image.GetPixel(45,60);
        Assert.Equal((byte)0,actual.R); Assert.InRange(actual.G,127,128); Assert.Equal(actual.G,actual.B);
        Assert.Equal(new Color(0,255,0,255),image.GetPixel(73,73));
        Assert.Equal(2,Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<PushBackdrop>()).Filters!.Count);
    }

    [Theory]
    [InlineData(1f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Backdrop_filters_earlier_pixels_inside_rounded_box_and_leaves_foreground_sharp(float scale)
    {
        using var h = new RenderHarness(); var root = h.Host.Render<ElementBackdropPage>();
        var image = h.Render(100, 80, scale);
        var dark = image.GetPixel((int)(30 * scale), (int)(40 * scale));
        Assert.InRange(dark.R, 52, 56); Assert.Equal(dark.R, dark.G); Assert.Equal(dark.R, dark.B);
        Assert.Equal(new Color(255, 0, 0, 255), image.GetPixel((int)(21 * scale), (int)(21 * scale)));
        Assert.Equal(new Color(0, 255, 0, 255), image.GetPixel((int)(49 * scale), (int)(49 * scale)));
        Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<PushBackdrop>());
        var page = (ElementBackdropPage)root.Component; page.Filter = "none"; page.Refresh(); h.Host.RunPending();
        var plain = h.Render(100, 80, scale);
        Assert.Equal(new Color(255, 0, 0, 255), plain.GetPixel((int)(30 * scale), (int)(40 * scale)));
        Assert.Empty(h.Scene.BuildDisplayList().Commands.OfType<PushBackdrop>());
    }

    [Theory]
    [InlineData(0f)] [InlineData(.5f)] [InlineData(1f)]
    public void Whole_group_opacity_and_overflow_are_composited_once(float opacity)
    {
        using var graphics = new SkiaRenderer(new FontOptions { UseSystemFonts = false });
        var list = new DisplayList(new Size(100, 80), new DrawCommand[]
        {
            new FillRectangle(new Rect(0,0,100,80),0,Color.White),
            new PushBackdrop(new Rect(20,20,60,40),12,new Rect(10,10,80,60),0,0,false,opacity),
            new FillRectangle(new Rect(10,10,10,10),0,new Color(0,255,0,255)),
            new FillRectangle(new Rect(40,30,20,20),0,new Color(0,255,0,255)),Pop.Instance,
        });
        var image = graphics.RenderToImage(list, 1, Color.White);
        var plain = graphics.RenderToImage(new DisplayList(list.Size, list.Commands.Select(command => command is PushBackdrop ? new PushOpacity(opacity, new Rect(10,10,80,60)) : command).ToArray()), 1, Color.White);
        foreach (var point in new[] { new Point(15,15), new Point(50,40) })
        {
            var pixel = image.GetPixel((int)point.X,(int)point.Y);
            Assert.Equal(plain.GetPixel((int)point.X,(int)point.Y),pixel);
            Assert.Equal((byte)255,pixel.G);
        }
    }

    [Fact]
    public void Backdrop_lists_use_full_damage_to_preserve_sampling_of_earlier_content()
    {
        var commands = new DrawCommand[] { new FillRectangle(new Rect(0,0,100,80),0,Color.White), new PushBackdrop(new Rect(10,10,60,40),0,new Rect(10,10,60,40),4), Pop.Instance };
        var list = new DisplayList(new Size(100,80),commands);
        var damage = new DisplayListDamage(); Assert.Null(damage.Next(list)); Assert.Null(damage.Next(list));
    }

    [Theory]
    [InlineData(1f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Nested_transformed_backdrops_replay_after_cache_warmup_and_renderer_disposal(float scale)
    {
        var renderer = new SkiaRenderer(new FontOptions { UseSystemFonts = false });
        var list = new DisplayList(new Size(100, 80), new DrawCommand[]
        {
            new FillRectangle(new Rect(0,0,100,80),0,new Color(240,60,40,255)),
            new FillRectangle(new Rect(50,0,50,80),0,new Color(40,60,240,255)),
            new PushTranslate(3,2), new PushClip(new Rect(8,8,84,64),8),
            new PushBackdrop(new Rect(10,10,75,50),12,new Rect(8,8,84,64),3,.5f,false,.8f),
            new FillRectangle(new Rect(30,20,35,30),0,new Color(0,255,0,180)),
            new PushBackdrop(new Rect(40,25,30,30),6,new Rect(40,25,30,30),2,1.5f,true,.6f),
            new FillRectangle(new Rect(52,35,8,8),0,Color.White),
            Pop.Instance, Pop.Instance, Pop.Instance, Pop.Instance,
        });
        var expected = renderer.RenderToImage(list,scale,Color.White);
        using var first = renderer.RecordComposition(list);
        using var recording = renderer.RecordComposition(list);
        renderer.Dispose();
        using var bitmap = new SKBitmap(new SKImageInfo(expected.Width,expected.Height,SKColorType.Bgra8888,SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White); canvas.Scale(scale); recording.Draw(canvas);
        }
        Assert.Equal(expected.Pixels,bitmap.Bytes);
    }
}
