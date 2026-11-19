using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class MenuCompositionTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory,"Fonts","FiraSans-Regular.ttf")],
        UseSystemFonts = false, DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree semantics;
    private readonly RootComponent root;
    private readonly MenuFixture fixture;
    public MenuCompositionTests()
    {
        scene = new Scene(host.Renderer,graphics.Fonts,graphics.Shaper) { Viewport = new Size(800,800) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer,scene); semantics = new SemanticTree(host.Renderer,scene,input);
        root = host.Render<MenuFixture>(); fixture = (MenuFixture)root.Component; Update();
    }
    public void Dispose() { semantics.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }
    private void Update() { host.RunPending(); semantics.Update(); }
    private ElementNode Element(string id) => root.Elements().Single(node=>node.Id==id);
    private void Key(string key,KeyModifiers modifiers=KeyModifiers.None) { input.KeyDown(key,modifiers,false); input.KeyUp(key,modifiers); Update(); }
    private void Open()
    {
        input.Focus(Element("open"),visible:true); Key("ContextMenu");
        Assert.Single(scene.Overlays); Assert.Equal("check",input.Focused!.Id);
    }
    private void EnterSubmenu()
    {
        input.Focus(root.Single("Submenu"),visible:true); Key("ArrowRight");
        Assert.Equal(2,scene.Overlays.Count); Assert.Equal("nested",input.Focused!.Id);
    }

    [Fact]
    public void Context_trigger_keeps_left_click_and_opens_with_the_context_key_or_secondary_pointer()
    {
        input.Focus(Element("open"),visible:true); Key("Enter");
        Assert.Equal(1,fixture.Clicks); Assert.Empty(scene.Overlays);
        var bounds = scene.GetBounds(Element("open")); var point = new Point(bounds.X+2,bounds.Y+2);
        input.PointerPressed(point,PointerButton.Secondary,KeyModifiers.None);
        input.PointerReleased(point,PointerButton.Secondary,KeyModifiers.None); Update();
        Assert.Single(scene.Overlays); Assert.Equal(1,fixture.Clicks);
        Key("Escape"); Assert.Empty(scene.Overlays); Assert.Equal("open",input.Focused!.Id);
        Open();
    }

    [Fact]
    public void Checked_and_radio_items_share_state_but_arrow_navigation_does_not_select()
    {
        Open();
        var check = semantics.Descendants().Single(node=>node.AutomationId=="check");
        Assert.Equal(SemanticRole.MenuCheckBox,check.Role); Assert.False(check.IsChecked);
        Key(" "); Assert.Single(scene.Overlays);
        check = semantics.Descendants().Single(node=>node.AutomationId=="check");
        Assert.True(check.IsChecked); Assert.Equal(1,fixture.CheckChanges);
        Key("ArrowDown"); Assert.Equal("a",input.Focused!.Id);
        Key("ArrowDown"); Assert.Equal("b",input.Focused!.Id);
        Assert.True(semantics.Descendants().Single(node=>node.AutomationId=="a").IsChecked);
        Assert.False(semantics.Descendants().Single(node=>node.AutomationId=="b").IsChecked);
        Key("Enter"); Assert.Single(scene.Overlays);
        Assert.Equal("b",fixture.Selection);
        Assert.True(semantics.Descendants().Single(node=>node.AutomationId=="b").IsChecked);
        Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Separator);
        Key("ArrowDown"); Assert.Same(root.Single("Submenu"),input.Focused);
        Key("End"); Assert.Equal("last",input.Focused!.Id);
        Key("ArrowDown"); Assert.Equal("last",input.Focused!.Id);
        Key("Home"); Assert.Equal("check",input.Focused!.Id);
        Key("ArrowUp"); Assert.Equal("check",input.Focused!.Id);
    }

    [Theory]
    [InlineData("ArrowLeft")]
    [InlineData("Escape")]
    public void Closing_one_level_returns_to_its_trigger_and_a_leaf_command_closes_the_chain(string key)
    {
        Open(); EnterSubmenu(); Key(key);
        Assert.Single(scene.Overlays); Assert.Same(root.Single("Submenu"),input.Focused);
        EnterSubmenu(); Key("Enter");
        Assert.Equal(1,fixture.Commands); Assert.Empty(scene.Overlays); Assert.Equal("open",input.Focused!.Id);
    }

    [Fact]
    public void Hover_opens_a_level_and_moving_to_a_parent_sibling_closes_it()
    {
        Open(); var trigger = root.Single("Submenu");
        var bounds = scene.GetBounds(trigger);
        input.PointerMoved(new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2),KeyModifiers.None);
        host.RunUntil(() =>
        {
            semantics.Update();
            return scene.Overlays.Count == 2;
        });
        Assert.Equal(2,scene.Overlays.Count);
        bounds = scene.GetBounds(Element("last"));
        input.PointerMoved(new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2),KeyModifiers.None); Update();
        Assert.Single(scene.Overlays); Assert.Equal("last",input.Focused!.Id);
    }

    [Fact]
    public void Clicking_a_parent_sibling_activates_it_in_one_click()
    {
        Open(); EnterSubmenu(); var bounds = scene.GetBounds(Element("last"));
        var point = new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2);
        input.PointerPressed(point,PointerButton.Primary,KeyModifiers.None);
        input.PointerReleased(point,PointerButton.Primary,KeyModifiers.None); Update();
        Assert.Equal(1,fixture.Commands); Assert.Empty(scene.Overlays);
    }

    [Theory]
    [InlineData(false,"after")]
    [InlineData(true,"before")]
    public void Tab_dismisses_all_levels_and_moves_out_of_the_menu(bool reverse,string expected)
    {
        Open(); EnterSubmenu(); Key("Tab",reverse ? KeyModifiers.Shift : KeyModifiers.None);
        Assert.Empty(scene.Overlays); Assert.Equal(expected,input.Focused!.Id);
    }

    private sealed class MenuFixture : ComponentBase
    {
        public int Clicks,Commands,CheckChanges;
        public string Selection="a";
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            Button(b,0,"before",null);
            b.OpenComponent<ContextMenu>(10);
            b.AddComponentParameter(11,"Trigger",(RenderFragment)(t=>Button(t,0,"open",()=>Clicks++)));
            b.AddComponentParameter(12,"ChildContent",(RenderFragment)(menu=>
            {
                menu.OpenComponent<MenuCheckItem>(0); menu.AddComponentParameter(1,"Id","check");
                menu.AddComponentParameter(3,"CloseOnSelect",false);
                menu.AddComponentParameter(4,"IsCheckedChanged",EventCallback.Factory.Create<bool>(this,_=>CheckChanges++));
                menu.AddComponentParameter(5,"ChildContent",Text("Check")); menu.CloseComponent();
                menu.OpenComponent<MenuRadioGroup<string>>(10); menu.AddComponentParameter(11,"Value",Selection);
                menu.AddComponentParameter(12,"ValueChanged",EventCallback.Factory.Create<string>(this,value=>Selection=value));
                menu.AddComponentParameter(13,"ChildContent",(RenderFragment)(radios=>
                {
                    Radio(radios,0,"a"); Radio(radios,10,"b");
                })); menu.CloseComponent();
                menu.OpenComponent<MenuSeparator>(20); menu.CloseComponent();
                menu.OpenComponent<Submenu>(30); menu.AddComponentParameter(31,"ChildContent",Text("More"));
                menu.AddComponentParameter(32,"Content",(RenderFragment)(nested=>Command(nested,0,"nested"))); menu.CloseComponent();
                Command(menu,40,"last");
            })); b.CloseComponent();
            Button(b,20,"after",null);
        }
        private static RenderFragment Text(string text) => b=>b.AddContent(0,text);
        private void Button(RenderTreeBuilder b,int sequence,string id,Action? action)
        {
            b.OpenComponent<Controls.Button>(sequence); b.AddComponentParameter(sequence+1,"Id",id);
            b.AddComponentParameter(sequence+2,"ChildContent",Text(id));
            if (action is not null) b.AddComponentParameter(sequence+3,"OnClick",EventCallback.Factory.Create<PointerEventArgs>(this,_=>action()));
            b.CloseComponent();
        }
        private static void Radio(RenderTreeBuilder b,int sequence,string id)
        {
            b.OpenComponent<MenuRadioItem<string>>(sequence); b.AddComponentParameter(sequence+1,"Id",id);
            b.AddComponentParameter(sequence+2,"Value",id); b.AddComponentParameter(sequence+3,"CloseOnSelect",false);
            b.AddComponentParameter(sequence+4,"ChildContent",Text(id)); b.CloseComponent();
        }
        private void Command(RenderTreeBuilder b,int sequence,string id)
        {
            b.OpenComponent<MenuItem>(sequence); b.AddComponentParameter(sequence+1,"Id",id);
            b.AddComponentParameter(sequence+2,"ChildContent",Text(id));
            b.AddComponentParameter(sequence+3,"OnClick",EventCallback.Factory.Create<PointerEventArgs>(this,_=>Commands++)); b.CloseComponent();
        }
    }
}
