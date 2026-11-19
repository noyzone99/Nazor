using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Skia;
using SkiaSharp;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class BlendingRenderingTests
{
    private sealed class Probe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0,"Page"); builder.OpenElement(1,"Group");
            builder.OpenElement(2,"Foreground"); builder.AddContent(3,"Hi");
            builder.CloseElement(); builder.CloseElement(); builder.CloseElement();
        }
    }

    [Theory]
    [InlineData(BlendMode.Normal,128,64,32)]
    [InlineData(BlendMode.Multiply,32,24,16)]
    [InlineData(BlendMode.Screen,160,136,144)]
    [InlineData(BlendMode.Overlay,64,48,33)]
    [InlineData(BlendMode.Darken,64,64,32)]
    [InlineData(BlendMode.Lighten,128,96,128)]
    [InlineData(BlendMode.ColorDodge,129,128,146)]
    [InlineData(BlendMode.ColorBurn,0,0,0)]
    [InlineData(BlendMode.HardLight,65,48,32)]
    [InlineData(BlendMode.SoftLight,64,66,80)]
    [InlineData(BlendMode.Difference,64,32,96)]
    [InlineData(BlendMode.Exclusion,128,112,128)]
    public void Separable_modes_match_independent_channel_equations(BlendMode mode,int r,int g,int b)
    {
        using var renderer = new SkiaRenderer(new FontOptions { UseSystemFonts=false });
        var list = new DisplayList(new Size(40,40),[
            new FillRectangle(new Rect(0,0,40,40),0,new Color(64,96,128,255)),
            new PushBlend(mode,new Rect(0,0,40,40)),
            new FillRectangle(new Rect(0,0,40,40),0,new Color(128,64,32,255)),Pop.Instance]);
        foreach (var scale in new[] {1f,1.5f,2f})
        {
            var pixel=renderer.RenderToImage(list,scale,Color.Transparent).GetPixel((int)(20*scale),(int)(20*scale));
            Assert.InRange(Math.Abs(pixel.R-r),0,1); Assert.InRange(Math.Abs(pixel.G-g),0,1); Assert.InRange(Math.Abs(pixel.B-b),0,1);
        }
    }

    [Theory]
    [InlineData(BlendMode.Hue)] [InlineData(BlendMode.Saturation)]
    [InlineData(BlendMode.Color)] [InlineData(BlendMode.Luminosity)]
    public void Nonseparable_modes_preserve_the_selected_luminosity_and_transparent_destination(BlendMode mode)
    {
        using var renderer = new SkiaRenderer(new FontOptions { UseSystemFonts=false });
        var source = new Color(128,64,32,255); var backdrop = new Color(64,96,128,255);
        var commands = new DrawCommand[] {new PushBlend(mode,new Rect(0,0,40,40)),new FillRectangle(new Rect(0,0,40,40),0,source),Pop.Instance};
        var transparent=renderer.RenderToImage(new DisplayList(new Size(40,40),commands),1,Color.Transparent).GetPixel(20,20);
        Assert.Equal(source,transparent);
        var list = new DisplayList(new Size(40,40),new DrawCommand[] {new FillRectangle(new Rect(0,0,40,40),0,backdrop)}.Concat(commands).ToArray());
        var actual=renderer.RenderToImage(list,1,Color.Transparent).GetPixel(20,20);
        double Lum(Color color) => .3*color.R+.59*color.G+.11*color.B;
        Assert.InRange(Math.Abs(Lum(actual)-Lum(mode==BlendMode.Luminosity ? source : backdrop)),0,1);
        Assert.NotEqual(source,actual); Assert.NotEqual(backdrop,actual);
    }

    private static RenderedImage Scene(RenderHarness h,string group,string foreground="")
    {
        var result=NssCompiler.Compile("blend", "Page { width:100px; height:80px; background:blue; } Group { width:60px; height:50px; margin:10px; "+group+" } Foreground { width:60px; height:50px; "+foreground+" }");
        Assert.True(result.Success,string.Join("\n",result.Diagnostics)); h.Scene.Styles.Application=result.StyleSheet;
        return h.Render(100,80,1.5f);
    }

    [Fact]
    public void Blend_alpha_uses_source_over_and_group_opacity_is_applied_once()
    {
        using var renderer=new SkiaRenderer(new FontOptions {UseSystemFonts=false});
        var list=new DisplayList(new Size(40,40),[
            new FillRectangle(new Rect(0,0,40,40),0,new Color(64,96,128,128)),
            new PushBlend(BlendMode.Multiply,new Rect(0,0,40,40)),new PushOpacity(.5f,new Rect(0,0,40,40)),
            new FillRectangle(new Rect(0,0,40,40),0,new Color(128,64,32,255)),
            new FillRectangle(new Rect(10,10,20,20),0,new Color(128,64,32,255)),Pop.Instance,Pop.Instance]);
        var image=renderer.RenderToImage(list,1,Color.Transparent);
        var actual=image.GetPixel(20,20); Assert.Equal(actual,image.GetPixel(5,5));
        Assert.InRange(actual.R,74,76); Assert.InRange(actual.G,60,62); Assert.InRange(actual.B,58,60); Assert.InRange(actual.A,191,192);
    }

    [Fact]
    public void Explicit_isolation_separates_descendant_blends_from_earlier_page_pixels()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var automatic=Scene(h,"isolation:auto;","background:red; mix-blend-mode:screen;");
        var isolated=Scene(h,"isolation:isolate;","background:red; mix-blend-mode:screen;");
        Assert.Equal(new Color(255,0,255,255),automatic.GetPixel(60,60));
        Assert.Equal(new Color(255,0,0,255),isolated.GetPixel(60,60));
        Scene(h,"", "background:red;"); Assert.Empty(h.Scene.BuildDisplayList().Commands.OfType<PushBlend>());
    }

    [Fact]
    public void Background_layers_blend_with_their_own_color_and_never_the_page()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var image=Scene(h,"background:#406080; background-image:linear-gradient(#804020,#804020); background-blend-mode:multiply;");
        Assert.Equal(new Color(32,24,16,255),image.GetPixel(60,60));
        var transparent=Scene(h,"background:linear-gradient(red,red); background-blend-mode:multiply;");
        Assert.Equal(new Color(255,0,0,255),transparent.GetPixel(60,60));
        var list=Scene(h,"background:linear-gradient(red,red),linear-gradient(blue,blue) white; background-blend-mode:screen,normal;");
        Assert.Equal(new Color(255,0,255,255),list.GetPixel(60,60));
    }

    [Fact]
    public void Blended_filter_text_clip_transform_and_opacity_replay_after_renderer_disposal()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var expected=Scene(h,"opacity:.5; filter:invert(.2); clip-path:inset(2px round 5px); transform:translateX(3px); mix-blend-mode:multiply;", "background:red; color:white;");
        var list=h.Scene.BuildDisplayList(); Assert.Contains(list.Commands,command=>command is DrawText);
        var damage=new DisplayListDamage(); Assert.Null(damage.Next(list)); Assert.Null(damage.Next(list));
        using var first=h.Graphics.RecordComposition(list); using var recording=h.Graphics.RecordComposition(list); h.Graphics.Dispose();
        using var bitmap=new SKBitmap(new SKImageInfo(expected.Width,expected.Height,SKColorType.Bgra8888,SKAlphaType.Premul));
        using(var canvas=new SKCanvas(bitmap)) { canvas.Clear(SKColors.White); canvas.Scale(1.5f); recording.Draw(canvas); }
        Assert.Equal(expected.Pixels,bitmap.Bytes);
    }
}
