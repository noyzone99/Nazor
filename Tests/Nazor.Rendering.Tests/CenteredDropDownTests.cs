using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class CenteredDropDownPage : ComponentBase
{
    [Parameter] public bool Editable { get; set; }
    [Parameter] public int Selected { get; set; }
    [Parameter] public int Count { get; set; }=4;
    [Parameter] public int Top { get; set; }=200;
    [Parameter] public DropDownPlacement Placement { get; set; }=DropDownPlacement.Center;
    private Select<string>? select;
    private ComboBox<string>? combo;
    public Task OpenAsync()=>Editable ? combo!.ToggleAsync() : select!.OpenAsync();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var items=Enumerable.Range(0,Count).Select(i=>$"Item {i:00}").ToArray();
        builder.OpenComponent(0,Editable ? typeof(ComboBox<string>) : typeof(Select<string>));
        builder.AddComponentParameter(1,"Items",items);
        builder.AddComponentParameter(2,"Value",Selected<0 ? null : items[Selected]);
        builder.AddComponentParameter(3,"DropDownPlacement",Placement);
        builder.AddComponentParameter(4,"PreferredMaxVisibleItems",4);
        builder.AddComponentParameter(5,"Style",$"position:absolute; left:20px; top:{Top}px; width:200px; height:32px;");
        builder.AddComponentReferenceCapture(6,component=>{select=component as Select<string>;combo=component as ComboBox<string>;});
        builder.CloseComponent();
    }
}

public sealed class CenteredDropDownTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Fluent_centered_reveal_expands_in_both_directions(bool editable)
    {
        var clock=new Nazor.Animation.ManualAnimationClock();
        using var h=new RenderHarness(clock:clock);using var input=new InputManager(h.Host.Renderer,h.Scene);
        h.Scene.Styles.Theme=Nazor.Themes.Fluent.Themes.Fluent;
        h.Scene.Styles.Preferences=new MediaPreferences(ColorScheme.Light,ReducedMotion:false);
        h.Scene.Transitions.ReducedMotion=false;
        var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Editable",editable}}));
        Frame(h);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h);
        var popup=root.Single(Prefix(editable)+"Popup");
        var start=h.Scene.GetEffectiveValue(popup,StyleProperties.ClipPath);
        Assert.Equal(50,start.Top.Resolve(100),5);Assert.Equal(50,start.Bottom.Resolve(100),5);
        clock.Advance(125);Frame(h);
        var middle=h.Scene.GetEffectiveValue(popup,StyleProperties.ClipPath);
        Assert.InRange(middle.Top.Resolve(100),.001,49.999);Assert.Equal(middle.Top,middle.Bottom);
    }

    private static void Frame(RenderHarness h,float scale=1,float width=360)
    {
        for(var i=0;i<3;i++){h.Host.RunPending();h.Render(width,h.Scene.Viewport.Height==0 ? 480 : h.Scene.Viewport.Height,scale);}
    }
    private static string Prefix(bool editable)=>editable ? "ComboBox" : "Select";

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Selected_row_overlays_the_field_without_changing_default_side_modes(bool editable)
    {
        foreach(var scale in new[]{1f,1.5f,2f})
        {
            using var h=new RenderHarness();h.Scene.Styles.Theme=Nazor.Themes.Fluent.Themes.Fluent;
            using var input=new InputManager(h.Host.Renderer,h.Scene);
            var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Editable",editable}}));
            Frame(h,scale);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h,scale);
            var field=h.Scene.GetBounds(root.Single(Prefix(editable)));
            var popup=h.Scene.GetBounds(root.Single(Prefix(editable)+"Popup"));
            var selected=Assert.Single(root.Elements(Prefix(editable)+"Option"),item=>item.Attributes.GetValueOrDefault("selected") is true);
            var row=h.Scene.GetBounds(selected);
            Assert.InRange(Math.Abs(row.Y+row.Height/2-field.Y-field.Height/2),0,1/scale);
            Assert.True(popup.Y<field.Bottom && popup.Bottom>field.Y);
            Assert.Equal(field.X,popup.X,2);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Unset_selection_centers_the_whole_list(bool editable)
    {
        using var h=new RenderHarness();
        using var input=new InputManager(h.Host.Renderer,h.Scene);
        var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Editable",editable},{"Selected",-1}}));
        Frame(h);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h);
        var field=h.Scene.GetBounds(root.Single(Prefix(editable)));var popup=h.Scene.GetBounds(root.Single(Prefix(editable)+"Popup"));
        Assert.Equal(field.Y+field.Height/2,popup.Y+popup.Height/2,1);
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)]
    [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,-1)] [InlineData(true,-1)]
    public async Task Client_bounds_native_work_area_and_rejected_native_popups_share_sizing_and_scroll(bool editable,int native)
    {
        using var h=new RenderHarness();using var input=new InputManager(h.Host.Renderer,h.Scene);
        h.Scene.Viewport=new Size(360,120);
        var requests=0;
        if(native!=0)
        {
            h.Scene.NativePopupWorkArea=_=>new Rect(-50,-50,500,500);
            h.Scene.ArrangeNativePopup=(_,request)=>
            {
                requests++;Assert.Equal("bottom",request.Side);
                Assert.Equal(request.Bounds.Y-request.Anchor.Bottom,request.Offset,1);
                return native>0 ? request.Bounds : null;
            };
        }
        var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>
        { {"Editable",editable},{"Top",45},{"Count",20},{"Selected",18} }));
        Frame(h);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h);
        var popup=root.Single(Prefix(editable)+"Popup");var bounds=h.Scene.GetBounds(popup);
        var scroll=h.Scene.GetScrollState(popup);Assert.NotNull(scroll);Assert.True(scroll.MaxOffsetY>0);
        var selected=Assert.Single(root.Elements(Prefix(editable)+"Option"),item=>item.Attributes.GetValueOrDefault("selected") is true);
        Assert.False(h.Scene.GetBounds(selected).Intersect(bounds).IsEmpty);
        if(native<=0)
        {
            Assert.InRange(bounds.Y,0,120);Assert.True(bounds.Bottom<=120.01f);Assert.Null(h.Scene.GetNativePopupBounds(popup));
        }
        else { Assert.NotNull(h.Scene.GetNativePopupBounds(popup));Assert.True(bounds.Y<0 || bounds.Bottom>120); }
        if(native!=0)Assert.True(requests>0);
        if(editable) {input.Focus(root.Single("ComboBoxInput"),visible:true);Frame(h);Assert.Same(root.Single("ComboBoxInput"),input.Focused);}
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public async Task Oversized_width_and_height_are_reduced_to_the_client_viewport(int native)
    {
        using var h=new RenderHarness();using var input=new InputManager(h.Host.Renderer,h.Scene);
        h.Scene.Viewport=new Size(100,100);
        if(native<0){h.Scene.NativePopupWorkArea=_=>new Rect(-50,-50,500,500);h.Scene.ArrangeNativePopup=(_,_)=>null;}
        var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Count",20},{"Top",30}}));
        Frame(h,width:100);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h,width:100);
        var popup=h.Scene.GetBounds(root.Single("SelectPopup"));
        Assert.InRange(popup.X,0,100);Assert.True(popup.Right<=100.01f);Assert.True(popup.Bottom<=100.01f);
        Assert.True(h.Scene.GetScrollState(root.Single("SelectPopup"))!.MaxOffsetY>0);
    }

    [Theory]
    [InlineData(DropDownPlacement.Top)] [InlineData(DropDownPlacement.Bottom)] [InlineData(DropDownPlacement.Auto)]
    public async Task Existing_side_preferences_retain_their_placement(DropDownPlacement placement)
    {
        using var h=new RenderHarness();
        var root=h.Host.Render<CenteredDropDownPage>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Placement",placement}}));
        Frame(h);await ((CenteredDropDownPage)root.Component).OpenAsync();Frame(h);
        var field=h.Scene.GetBounds(root.Single("Select"));var popup=h.Scene.GetBounds(root.Single("SelectPopup"));
        Assert.Equal(placement==DropDownPlacement.Top ? field.Y : field.Bottom,placement==DropDownPlacement.Top ? popup.Bottom : popup.Y,1);
    }
}
