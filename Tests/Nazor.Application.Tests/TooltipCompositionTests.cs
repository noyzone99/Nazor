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

public sealed class TooltipCompositionTests : IDisposable
{
    private readonly TestHost host = new(captureExceptions:true);
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory,"Fonts","FiraSans-Regular.ttf")],
        UseSystemFonts = false, DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree semantics;
    private readonly RootComponent root;
    private readonly Fixture fixture;
    public TooltipCompositionTests()
    {
        scene = new Scene(host.Renderer,graphics.Fonts,graphics.Shaper) { Viewport = new Size(600,500) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer,scene) { TooltipDelay=TimeSpan.FromMinutes(1) };
        semantics = new SemanticTree(host.Renderer,scene,input);
        root = host.Render<Fixture>(); fixture = (Fixture)root.Component; Update();
    }
    public void Dispose() { semantics.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }
    private void Update() { host.RunPending(); semantics.Update(); }
    private ElementNode Button(string id) => root.Elements("Button").Single(node=>node.Id==id);
    private Point Center(ElementNode element) { var bounds=scene.GetBounds(element); return new(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2); }
    private void Wait() { host.RunUntil(()=>input.TooltipTarget is not null); Update(); }

    [Fact]
    public void Keyboard_tooltips_preserve_name_focus_and_description_and_close_with_escape()
    {
        var button = semantics.Descendants().Single(node=>node.AutomationId=="save");
        Assert.Equal("Save",button.Name); Assert.Equal("Save the document.",button.Description);
        Assert.DoesNotContain(semantics.Descendants(),node=>node.Role==SemanticRole.ToolTip);
        Assert.False(root.Single("TooltipTrigger").Attributes.ContainsKey("focusable"));
        input.Focus(Button("save"),visible:true); Wait();
        Assert.Same(Button("save"),input.Focused);
        Assert.Equal("Save the document.",Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.ToolTip).Name);
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Null(input.TooltipTarget); Assert.Empty(scene.Overlays);
        Assert.Same(Button("save"),input.Focused);
        Assert.Equal("Save the document.",button.Description);
        Assert.Single(root.Elements("Tooltip"));
    }

    [Fact]
    public void Pointer_can_move_from_trigger_to_rich_content_before_leaving()
    {
        input.PointerMoved(Center(Button("save")),KeyModifiers.None); Wait();
        var tooltip=root.Single("Tooltip");
        var contentPoint=Center(tooltip);
        input.PointerMoved(contentPoint,KeyModifiers.None); Update();
        Assert.NotNull(input.TooltipTarget); Assert.Single(scene.Overlays);
        input.PointerMoved(Center(Button("outside")),KeyModifiers.None); Update();
        Assert.Null(input.TooltipTarget); Assert.Empty(scene.Overlays); Assert.Null(input.Focused);
    }

    [Fact]
    public void Tooltip_content_adds_no_tab_stop()
    {
        input.Focus(Button("save"),visible:true); Wait();
        input.KeyDown("Tab",KeyModifiers.None,false); Update();
        Assert.Same(Button("outside"),input.Focused); Assert.Empty(scene.Overlays);
    }

    [Fact]
    public void Keyboard_activation_closes_the_tooltip_and_runs_the_original_command()
    {
        input.Focus(Button("save"),visible:true); Wait();
        input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal(1,fixture.Actions); Assert.Null(input.TooltipTarget); Assert.Empty(scene.Overlays);
        Assert.Same(Button("save"),input.Focused);
    }

    [Fact]
    public async Task Programmatic_close_clears_the_active_scheduled_tooltip()
    {
        input.Focus(Button("save"),visible:true); Wait();
        await fixture.CloseAsync(); Update();
        Assert.Null(input.TooltipTarget); Assert.Empty(scene.Overlays);
    }

    [Fact]
    public void Retained_tooltip_content_does_not_become_its_document_ancestors_name()
    {
        host.Render<HeadingFixture>(); Update();
        Assert.Equal("Title",Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Heading).Name);
        var link=semantics.Descendants().Single(node=>node.Element?.Id=="title-text");
        Assert.Equal(SemanticRole.Hyperlink,link.Role); Assert.Equal("Explanation",link.Description);
    }

    [Fact]
    public async Task A_modal_dialog_suppresses_background_tooltips_until_it_closes()
    {
        input.PointerMoved(Center(Button("save")),KeyModifiers.None); Wait();
        await fixture.OpenDialogAsync(); Update();
        Assert.Null(input.TooltipTarget);
        input.PointerMoved(Center(Button("save")),KeyModifiers.None); Update();
        Assert.Single(scene.Overlays, overlay => overlay.Name == "Dialog"); Assert.Null(input.TooltipTarget);
        await fixture.CloseDialogAsync(); Update();
        input.PointerMoved(Center(Button("save")),KeyModifiers.None); Wait();
        Assert.NotNull(input.TooltipTarget);
    }

    [Fact]
    public void Removing_pending_and_open_tooltips_cancels_work_without_disposed_callbacks()
    {
        fixture.Configure(delay:TimeSpan.FromMilliseconds(80)); Update();
        input.PointerMoved(Center(Button("save")),KeyModifiers.None);
        fixture.Remove(); Update();
        Assert.False(host.Dispatcher.RunUntil(()=>input.TooltipTarget is not null,TimeSpan.FromMilliseconds(180)));
        fixture.Restore(); Update();
        input.Focus(Button("save"),visible:true); Wait();
        fixture.Remove(); Update();
        Assert.Null(input.TooltipTarget); Assert.Empty(scene.Overlays); Assert.Empty(host.Exceptions);
    }

    [Fact]
    public void Disabled_tooltips_close_and_interactive_content_is_rejected()
    {
        input.Focus(Button("save"),visible:true); Wait();
        fixture.Configure(disabled:true); Update();
        Assert.Empty(scene.Overlays); Assert.Null(input.TooltipTarget);
        fixture.Configure(disabled:false,interactive:true); Update();
        Assert.Contains(host.Exceptions,error=>error.GetBaseException().Message.Contains("Tooltip content must be noninteractive",StringComparison.Ordinal));
    }

    [Fact]
    public void Tooltip_delay_validation_rejects_invalid_scheduled_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>input.TooltipDelay=TimeSpan.FromMilliseconds(-2));
        fixture.Configure(delay:TimeSpan.FromMilliseconds(-2)); Update();
        Assert.Contains(host.Exceptions,error=>error.GetBaseException() is ArgumentOutOfRangeException);
    }

    private sealed class Fixture : ComponentBase
    {
        private bool shown=true,disabled,interactive;
        private TimeSpan delay=TimeSpan.Zero;
        private Nazor.Controls.Primitives.TooltipContainer? tooltip;
        private Dialog? dialog;
        public int Actions;
        public void Configure(TimeSpan? delay=null,bool disabled=false,bool interactive=false)
        {
            if (delay is { } value) this.delay=value;
            this.disabled=disabled; this.interactive=interactive; StateHasChanged();
        }
        public void Remove() { shown=false; StateHasChanged(); }
        public void Restore() { shown=true; delay=TimeSpan.Zero; StateHasChanged(); }
        public Task CloseAsync() => tooltip!.CloseAsync();
        public Task OpenDialogAsync() => dialog!.OpenAsync();
        public Task CloseDialogAsync() => dialog!.CloseAsync();
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            if (shown)
            {
                b.OpenComponent<Nazor.Controls.Primitives.TooltipContainer>(0); b.AddComponentParameter(1,"Delay",delay); b.AddComponentParameter(2,"Disabled",disabled);
                b.AddComponentParameter(3,"As","Tooltip");
                b.AddComponentParameter(5,"Trigger",(RenderFragment)(trigger=>BuildButton(trigger,0,"save","Save",null,()=>Actions++)));
                b.AddComponentParameter(6,"ChildContent",(RenderFragment)(content=>
                {
                    content.OpenComponent<TextBlock>(0); content.AddComponentParameter(1,"ChildContent",(RenderFragment)(text=>text.AddContent(0,"Save the document."))); content.CloseComponent();
                    if (interactive) BuildButton(content,10,"invalid","Action",null);
                })); b.AddComponentReferenceCapture(7,component=>tooltip=(Nazor.Controls.Primitives.TooltipContainer)component); b.CloseComponent();
            }
            BuildButton(b,20,"outside","Outside","margin-top: 150px");
            b.OpenComponent<Dialog>(30); b.AddComponentParameter(31,"AccessibleName","Dialog");
            b.AddComponentParameter(32,"ChildContent",(RenderFragment)(content=>BuildButton(content,0,"dialog-button","Action",null)));
            b.AddComponentReferenceCapture(33,component=>dialog=(Dialog)component); b.CloseComponent();
        }
        private static void BuildButton(RenderTreeBuilder b,int sequence,string id,string text,string? style,Action? action=null)
        {
            b.OpenComponent<Controls.Button>(sequence); b.AddComponentParameter(sequence+1,"Id",id); b.AddComponentParameter(sequence+2,"Style",style);
            b.AddComponentParameter(sequence+3,"ChildContent",(RenderFragment)(content=>content.AddContent(0,text)));
            if (action is not null) b.AddComponentParameter(sequence+4,"OnClick",EventCallback.Factory.Create<PointerEventArgs>(action,_=>action()));
            b.CloseComponent();
        }
    }

    private sealed class HeadingFixture : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Heading>(0); b.AddComponentParameter(1,"ChildContent",(RenderFragment)(heading=>
            {
                heading.OpenComponent<Tooltip>(0);
                heading.AddComponentParameter(1,"Trigger",(RenderFragment)(trigger=>
                {
                    trigger.OpenComponent<Link>(0); trigger.AddComponentParameter(1,"Id","title-text");
                    trigger.AddComponentParameter(2,"ChildContent",(RenderFragment)(text=>text.AddContent(0,"Title"))); trigger.CloseComponent();
                }));
                heading.AddComponentParameter(2,"ChildContent",(RenderFragment)(text=>text.AddContent(0,"Explanation"))); heading.CloseComponent();
            })); b.CloseComponent();
        }
    }
}
