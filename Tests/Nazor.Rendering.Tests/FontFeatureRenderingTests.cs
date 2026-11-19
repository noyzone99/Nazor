using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Skia;
using SkiaSharp;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class FontFeatureRenderingTests
{
    private static FontOptions Options(params string[] extra) => new()
    {
        PrivateFontFiles=[Path.Combine(RenderHarness.FontDirectory,"FiraSans-Regular.ttf"),..extra],
        DefaultFamily="Fira Sans",UseSystemFonts=false,
    };
    private static TextStyle Style => new(["Fira Sans"],40);

    [Fact]
    public void Text_style_feature_inputs_are_copied_sorted_and_part_of_cache_identity()
    {
        var input=new Dictionary<string,uint> {["kern"]=0,["liga"]=1};
        var first=Style with {FontFeatures=input}; input["kern"]=1;
        var second=Style with {FontFeatures=new Dictionary<string,uint>{["liga"]=1,["kern"]=0}};
        Assert.Equal(first,second); Assert.Equal(first.GetHashCode(),second.GetHashCode()); Assert.Equal(0u,first.FontFeatures["kern"]);
        Assert.NotEqual(first,first with {Synthesis=FontSynthesis.Style});
        Assert.NotEqual(first,Style); Assert.Throws<NotSupportedException>(()=>((IDictionary<string,uint>)first.FontFeatures).Add("smcp",1));
        Assert.Throws<ArgumentException>(()=>Style with {FontFeatures=new Dictionary<string,uint>{["bad"]=1}});
        Assert.Throws<ArgumentOutOfRangeException>(()=>Style with {Synthesis=(FontSynthesis)128});
    }

    [Fact]
    public void Actual_private_font_features_change_ligatures_kerning_and_caret_advances()
    {
        using var fonts=new SkiaFontProvider(Options()); var shaper=new HarfBuzzTextShaper();
        TextLayout Layout(string text,TextStyle style)=>new(text,style,null,fonts,shaper);
        var ligature=Layout("ffi",Style); var separate=Layout("ffi",Style with {FontFeatures=new Dictionary<string,uint>{["liga"]=0}});
        Assert.True(ligature.Runs.Sum(run=>run.Glyphs.Length)<separate.Runs.Sum(run=>run.Glyphs.Length));
        var kerned=Layout("AV",Style); var unkerned=Layout("AV",Style with {FontFeatures=new Dictionary<string,uint>{["kern"]=0}});
        Assert.True(kerned.Width<unkerned.Width); Assert.Equal(unkerned.Width,unkerned.GetCaretX(2),4);
        Assert.Equal(ligature.Runs.SelectMany(run=>run.Glyphs),Layout("ffi",Style with {FontFeatures=new Dictionary<string,uint>{["ZZZZ"]=1}}).Runs.SelectMany(run=>run.Glyphs));
    }

    [Theory]
    [InlineData(FontSynthesis.Weight)] [InlineData(FontSynthesis.Style)] [InlineData(FontSynthesis.Weight|FontSynthesis.Style)]
    public void Synthetic_outline_changes_raster_without_changing_real_face_or_advances(FontSynthesis synthesis)
    {
        var renderer=new SkiaRenderer(Options());
        var style=Style with {FontWeight=700,Italic=true,Synthesis=synthesis,LetterSpacing=1};
        var layout=new TextLayout("HHH HHH",style,95,renderer.Fonts,renderer.Shaper);
        var plain=new TextLayout("HHH HHH",style with {Synthesis=FontSynthesis.None},95,renderer.Fonts,renderer.Shaper);
        Assert.Same(plain.Primary,layout.Primary); Assert.Equal(400,layout.Primary.Weight); Assert.False(layout.Primary.Italic);
        Assert.Equal(plain.Width,layout.Width); Assert.Equal(plain.Height,layout.Height); Assert.True(layout.InkOutset>0);
        Assert.All(layout.Lines.SelectMany(line=>line.Runs),run=>Assert.Equal(synthesis,run.Synthesis));
        var mixed=new ShapedText([new TextRun("HH",style),new TextRun("HH",style)],style,renderer.Fonts,renderer.Shaper);
        Assert.All(mixed.Runs,run=>Assert.Equal(synthesis,run.Synthesis));
        DisplayList List(TextLayout value)=>new(new Size(220,160),[new DrawText(value,new Point(20,20),Color.Black,TextAlignment.Start,200)]);
        var expected=renderer.RenderToImage(List(layout),1.5f,Color.White);
        Assert.NotEqual(renderer.RenderToImage(List(plain),1.5f,Color.White).Pixels,expected.Pixels);
        using var warm=renderer.RecordComposition(List(layout)); using var recording=renderer.RecordComposition(List(layout)); renderer.Dispose();
        using var bitmap=new SKBitmap(new SKImageInfo(expected.Width,expected.Height,SKColorType.Bgra8888,SKAlphaType.Premul));
        using(var canvas=new SKCanvas(bitmap)){canvas.Clear(SKColors.White);canvas.Scale(1.5f);recording.Draw(canvas);}
        Assert.Equal(expected.Pixels,bitmap.Bytes);
    }

    [Fact]
    public void Real_bold_and_variable_axes_take_precedence_over_synthesis_and_fallback_keeps_requested_style()
    {
        var bold=Path.Combine(RenderHarness.FontDirectory,"FiraSans-Bold.ttf");
        var variable=Path.Combine(AppContext.BaseDirectory,"Themes","NazorLeaf","BricolageGrotesque.ttf");
        var icons=Path.Combine(AppContext.BaseDirectory,"Themes","NazorLeaf","NazorLeafIcons.otf");
        using var fonts=new SkiaFontProvider(Options(bold,variable,icons));var shaper=new HarfBuzzTextShaper();
        var style=Style with {FontWeight=700,Italic=true,Synthesis=FontSynthesis.Weight|FontSynthesis.Style};
        var real=new TextLayout("HH",style,null,fonts,shaper);
        Assert.All(real.Runs,run=> {Assert.Equal(700,run.Face.Weight);Assert.Equal(FontSynthesis.Style,run.Synthesis);});
        var varied=new TextLayout("HH",style with {Italic=false,FontFamilies=new[]{"Bricolage Grotesque"},FontVariations=new Dictionary<string,double>{["wght"]=400}},null,fonts,shaper);
        Assert.All(varied.Runs,run=>Assert.Equal(FontSynthesis.None,run.Synthesis));
        var fallback=new TextLayout("HH",style with {FontFamilies=new[]{"NazorLeaf Icons"}},null,fonts,shaper);
        Assert.All(fallback.Runs,run=>{Assert.Equal(700,run.Face.Weight);Assert.Equal(FontSynthesis.Style,run.Synthesis);});
    }

    private sealed class Probe : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0,"Page");builder.OpenElement(1,"FeatureText");builder.AddContent(2,"AV ffi");builder.CloseElement();builder.CloseElement();
        }
    }

    [Fact]
    public void Inherited_feature_updates_invalidate_scene_shaping_and_ch_uses_the_selected_feature()
    {
        using var h=new RenderHarness();h.Host.Render<Probe>();
        void Render(string features)
        {
            var result=NssCompiler.Compile("type","Page {font-size:40px; font-feature-settings:"+features+";} FeatureText {width:10ch;}");
            Assert.True(result.Success);h.Scene.Styles.Application=result.StyleSheet;h.Render(400,100,1);
        }
        Render("'kern' on,'liga' on,'tnum' on"); var first=Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        var zero=h.Graphics.Shaper.Shape("0",0,1,first.Layout.Primary,first.Layout.Style).Width;
        Assert.Equal(zero*10,first.Width,3);
        Render("'kern' off,'liga' off");var second=Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.NotSame(first.Layout.Shaped,second.Layout.Shaped);Assert.NotEqual(first.Layout.Width,second.Layout.Width);
    }
}
