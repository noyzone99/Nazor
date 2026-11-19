using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Skia;
using SkiaSharp;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class ExtendedGradientRenderingTests
{
    public sealed class Probe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Panel"); builder.CloseElement();
        }
    }

    private static RenderedImage Render(RenderHarness h, string gradient, float scale)
    {
        var result = NssCompiler.Compile("gradient", $"Panel {{ width:100px; height:100px; background-color:white; background-image:{gradient}; }}");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        h.Scene.Styles.Application = result.StyleSheet;
        return h.Render(100, 100, scale);
    }

    [Theory]
    [InlineData(1f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Conic_hard_sectors_start_at_the_top_rotate_and_replay_after_warmup(float scale)
    {
        using var h = new RenderHarness(); h.Host.Render<Probe>();
        var image = Render(h, "conic-gradient(red 0 25%, blue 25% 100%)", scale);
        Assert.Equal(new Color(255,0,0,255), image.GetPixel((int)(75*scale),(int)(25*scale)));
        Assert.Equal(new Color(0,0,255,255), image.GetPixel((int)(25*scale),(int)(75*scale)));
        using var first = h.Graphics.RecordComposition(h.Scene.BuildDisplayList());
        using var recorded = h.Graphics.RecordComposition(h.Scene.BuildDisplayList());
        using var bitmap = new SKBitmap(new SKImageInfo(image.Width,image.Height,SKColorType.Bgra8888,SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap)) { canvas.Clear(SKColors.White); canvas.Scale(scale); recorded.Draw(canvas); }
        Assert.Equal(image.Pixels,bitmap.Bytes);
        var rotated = Render(h,"conic-gradient(from 90deg, red 0 25%, blue 25% 100%)",scale);
        Assert.Equal(new Color(0,0,255,255),rotated.GetPixel((int)(75*scale),(int)(25*scale)));
        Assert.Equal(new Color(255,0,0,255),rotated.GetPixel((int)(75*scale),(int)(75*scale)));
    }

    [Theory]
    [InlineData("repeating-linear-gradient(to right, red 5px 10px, blue 10px 15px)", 7,40,17,40,12,40)]
    [InlineData("repeating-radial-gradient(circle, red 5px 10px, blue 10px 15px)", 57,50,67,50,62,50)]
    [InlineData("repeating-radial-gradient(ellipse 40px 20px, red 5px 10px, blue 10px 15px)", 57,50,67,50,62,50)]
    [InlineData("repeating-conic-gradient(red 0 45deg, blue 45deg 90deg)",75,10,90,75,90,25)]
    public void Repetition_preserves_nonzero_phase_and_hard_stop_colors(string gradient,int ax,int ay,int bx,int by,int cx,int cy)
    {
        using var h = new RenderHarness(); h.Host.Render<Probe>();
        foreach (var scale in new[] { 1f,1.5f,2f })
        {
            var image = Render(h,gradient,scale);
            Color Pixel(int x,int y) => image.GetPixel((int)(x*scale),(int)(y*scale));
            Assert.Equal(new Color(255,0,0,255),Pixel(ax,ay));
            Assert.Equal(Pixel(ax,ay),Pixel(bx,by));
            Assert.Equal(new Color(0,0,255,255),Pixel(cx,cy));
        }
    }

    [Fact]
    public void Relative_centers_currentcolor_transparency_and_rounded_clipping_share_existing_painting_rules()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var result=NssCompiler.Compile("relative", "Panel { width:100px; height:100px; font-size:20px; color:red; border-radius:20px; background:conic-gradient(at 1em 2em,currentcolor 0 50%,transparent 50% 100%) white; }");
        Assert.True(result.Success,string.Join("\n",result.Diagnostics)); h.Scene.Styles.Application=result.StyleSheet;
        var image=h.Render(100,100,1.5f);
        var command=Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<FillGradient>());
        Assert.Equal(new Point(20,40),Assert.IsType<ConicGradientBrush>(command.Brush).Center);
        Assert.Equal(Color.White,image.GetPixel(0,0));
        Assert.Equal(new Color(255,0,0,255),image.GetPixel(105,60));
        Assert.Equal(Color.White,image.GetPixel(15,60));
    }

    [Theory]
    [InlineData("repeating-linear-gradient(red 0px, white 0px, blue 0px)")]
    [InlineData("repeating-radial-gradient(red 0px, white 0px, blue 0px)")]
    [InlineData("repeating-conic-gradient(red 0deg, white 0deg, blue 0deg)")]
    public void Zero_period_uses_the_premultiplied_average_instead_of_the_last_color(string gradient)
    {
        using var h = new RenderHarness(); h.Host.Render<Probe>(); var image = Render(h,gradient,1);
        Assert.Equal(new Color(191,128,191,255),image.GetPixel(23,47));
    }

    [Fact]
    public void Damage_observes_period_origin_and_rotation_changes()
    {
        GradientStop[] stops = [new(0,new Color(255,0,0,255)),new(1,new Color(0,0,255,255))];
        DisplayList List(GradientBrush brush) => new(new Size(100,100),[new FillGradient(new Rect(0,0,100,100),0,brush)]);
        var damage = new DisplayListDamage();
        var normal = new LinearGradientBrush(new Point(0,0),new Point(20,0),stops);
        Assert.Null(damage.Next(List(normal))); Assert.Empty(damage.Next(List(normal))!);
        Assert.NotEmpty(damage.Next(List(normal with { Repeating=true }))!);
        var conic = new ConicGradientBrush(new Point(50,50),0,90,stops) { Repeating=true };
        damage.Next(List(conic)); Assert.NotEmpty(damage.Next(List(conic with { Rotation=90 }))!);
        var radial = new RadialGradientBrush(new Point(50,50),20,20,stops) { Repeating=true };
        damage.Next(List(radial)); Assert.NotEmpty(damage.Next(List(radial with { StartRadius=5 }))!);
    }
}
