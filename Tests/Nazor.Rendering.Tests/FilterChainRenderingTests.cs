using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Skia;
using SkiaSharp;
using Color=Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class FilterChainRenderingTests
{
    [Theory]
    [InlineData("blur()")]
    [InlineData("blur(0px)")]
    [InlineData("blur(0px) invert(0) brightness(1)")]
    public void Identity_filters_render_without_requiring_a_native_effect(string filters)
    {
        using var h=new RenderHarness();h.Host.Render<Probe>();
        var actual=Render(h,filters);
        var plain=Render(h,"none");
        Assert.Equal(plain.Pixels,actual.Pixels);
        Render(h,"none",1,"backdrop-filter:"+filters+";");
    }

    [Fact]
    public void An_empty_backdrop_chain_overrides_the_pair_with_identity_sampling()
    {
        using var renderer=new SkiaRenderer(new FontOptions {UseSystemFonts=false});
        var identity=new PushBackdrop(new Rect(5,5,30,30),0,new Rect(5,5,30,30),0);
        var empty=identity with {Blur=10,Saturation=0,Filters=[]};
        DisplayList List(PushBackdrop backdrop)=>new(new Size(40,40),[
            new FillRectangle(new Rect(0,0,40,40),0,new Color(255,0,0,128)),backdrop,Pop.Instance]);
        Assert.Equal(renderer.RenderToImage(List(identity),1,Color.Transparent).Pixels,renderer.RenderToImage(List(empty),1,Color.Transparent).Pixels);
    }

    public sealed class Probe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0,"Panel"); builder.AddContent(1,"Hi"); builder.CloseElement();
        }
    }

    private static RenderedImage Render(RenderHarness h,string filters,float scale=1,string extra="")
    {
        var result=NssCompiler.Compile("filters",$"Panel {{ width:60px; height:60px; background:#804020; filter:{filters}; {extra} }}");
        Assert.True(result.Success,string.Join("\n",result.Diagnostics));
        h.Scene.Styles.Application=result.StyleSheet; return h.Render(100,100,scale);
    }

    [Theory]
    [InlineData("brightness(.5)",64,32,16)]
    [InlineData("contrast(0)",128,128,128)]
    [InlineData("grayscale()",75,75,75)]
    [InlineData("invert()",127,191,223)]
    [InlineData("opacity(.5)",192,160,144)]
    [InlineData("saturate(0)",75,75,75)]
    [InlineData("sepia(1)",106,94,73)]
    [InlineData("hue-rotate(180deg)",23,87,119)]
    public void Filter_pixels_follow_independent_color_arithmetic(string filters,int red,int green,int blue)
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        foreach (var scale in new[] {1f,1.5f,2f})
        {
            var image=Render(h,filters,scale); var actual=image.GetPixel((int)(30*scale),(int)(30*scale));
            Assert.InRange(Math.Abs(actual.R-red),0,1); Assert.InRange(Math.Abs(actual.G-green),0,1); Assert.InRange(Math.Abs(actual.B-blue),0,1);
        }
    }

    [Fact]
    public void Repeated_operations_keep_order_and_intermediate_clamping()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var first=Render(h,"brightness(2) contrast(.5)").GetPixel(30,30);
        var second=Render(h,"contrast(.5) brightness(2)").GetPixel(30,30);
        Assert.InRange(first.R,190,192); Assert.Equal((byte)255,second.R);
        Assert.Equal(2,Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<PushFilter>()).Filters.Count);
    }

    [Fact]
    public void Blur_extends_ink_beyond_the_element_and_uses_transparent_edges()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var image=Render(h,"blur(3px)",1,"width:20px; height:20px; margin:30px;");
        var outside=image.GetPixel(27,40);
        Assert.True(outside.G<255); Assert.True(outside.R>outside.G);
        Assert.Equal(Color.White,image.GetPixel(0,0));
        var filter=Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<PushFilter>());
        Assert.True(filter.Bounds.X<30 && filter.Bounds.Right>50);
        var damage=new DisplayListDamage(); Assert.Null(damage.Next(h.Scene.BuildDisplayList())); Assert.Null(damage.Next(h.Scene.BuildDisplayList()));
    }

    [Fact]
    public void Filtering_preserves_text_subtree_paint_and_visible_blur_from_offscreen_sources()
    {
        using var h=new RenderHarness(); h.Host.Render<Probe>();
        var image=Render(h,"invert()",1,"font-size:24px; color:black;");
        Assert.Contains(h.Scene.BuildDisplayList().Commands,command=>command is DrawText);
        var white=0;
        for(var y=0;y<30;y++) for(var x=0;x<30;x++) if(image.GetPixel(x,y)==Color.White) white++;
        Assert.True(white>3);
        var offscreen=Render(h,"blur(6px)",1,"width:10px; height:20px; margin-left:-20px; margin-top:30px;");
        Assert.True(offscreen.GetPixel(0,40).G<255);
        Render(h,"blur(.25em)",1,"font-size:20px;");
        Assert.Equal(5,Assert.IsType<GaussianBlurFilter>(Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<PushFilter>()).Filters[0]).Sigma);
        Render(h,"none");
        Assert.Empty(h.Scene.BuildDisplayList().Commands.OfType<PushFilter>());
    }

    [Fact]
    public void Foreground_filter_applies_group_opacity_once_to_overlapping_and_overflowing_children()
    {
        using var renderer=new SkiaRenderer(new FontOptions {UseSystemFonts=false});
        var invert=new ColorMatrixFilter([-1,0,0,0,1,0,-1,0,0,1,0,0,-1,0,1,0,0,0,1,0]);
        var actual=new DisplayList(new Size(100,80),[new FillRectangle(new Rect(0,0,100,80),0,Color.White),
            new PushFilter([invert],new Rect(10,10,80,60),.5f),new FillRectangle(new Rect(20,20,60,40),0,new Color(255,0,0,255)),
            new FillRectangle(new Rect(10,10,30,30),0,new Color(0,0,255,255)),Pop.Instance]);
        var image=renderer.RenderToImage(actual,1,Color.White);
        var overlap=image.GetPixel(30,30); var overflow=image.GetPixel(15,15); var lower=image.GetPixel(60,50);
        Assert.Equal(overlap,overflow);
        Assert.Equal((byte)255,overlap.R); Assert.Equal((byte)255,overlap.G); Assert.InRange(overlap.B,127,128);
        Assert.InRange(lower.R,127,128); Assert.Equal((byte)255,lower.G); Assert.Equal((byte)255,lower.B);
    }

    [Fact]
    public void Ordered_matrices_are_copied_and_recordings_own_native_filter_resources()
    {
        var values=new float[] {1,0,0,0,.1f,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0};
        var operation=new ColorMatrixFilter(values); values[4]=0;
        var renderer=new SkiaRenderer(new FontOptions {UseSystemFonts=false});
        var list=new DisplayList(new Size(100,80),[new FillRectangle(new Rect(0,0,100,80),0,Color.White),
            new PushTranslate(3,2),new PushClip(new Rect(10,10,75,55),8),
            new PushFilter([operation,new GaussianBlurFilter(2)],new Rect(10,10,80,60),.5f),
            new FillRectangle(new Rect(25,25,30,30),4,new Color(64,128,192,255)),Pop.Instance,Pop.Instance,Pop.Instance]);
        var expected=renderer.RenderToImage(list,1,Color.White);
        using var first=renderer.RecordComposition(list); using var recording=renderer.RecordComposition(list); renderer.Dispose();
        using var bitmap=new SKBitmap(new SKImageInfo(100,80,SKColorType.Bgra8888,SKAlphaType.Premul));
        using(var canvas=new SKCanvas(bitmap)){canvas.Clear(SKColors.White);recording.Draw(canvas);}
        Assert.Equal(expected.Pixels,bitmap.Bytes);
        Assert.Equal(.1f,operation.Values[4]);
        Assert.Throws<ArgumentException>(()=>new ColorMatrixFilter([1,2]));
    }
}
