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

public sealed class OverlayCompositionTests : IDisposable
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
    public OverlayCompositionTests()
    {
        scene = new Scene(host.Renderer,graphics.Fonts,graphics.Shaper) { Viewport = new Size(600,600) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer,scene); semantics = new SemanticTree(host.Renderer,scene,input);
    }
    public void Dispose() { semantics.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }
    private void Update() { host.RunPending(); semantics.Update(); }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dialog_editors_submit_only_a_form_inside_the_surface(bool innerForm)
    {
        var root = host.Render<FormDialogFixture>(ParameterView.FromDictionary(new Dictionary<string,object?> { ["InnerForm"]=innerForm })); Update();
        var fixture = (FormDialogFixture)root.Component;
        input.Focus(root.Elements("Button").Single(node=>node.Id=="open"),visible:true);
        input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal("TextFieldInput",input.Focused!.Name);
        input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal(0,fixture.OuterSubmits); Assert.Equal(innerForm ? 1 : 0,fixture.InnerSubmits);
    }

    [Fact]
    public void Composable_popover_adds_no_trigger_tab_stop_and_restores_focus()
    {
        var root = host.Render<PopoverFixture>(); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        Assert.False(root.Single("PopoverTrigger").Attributes.ContainsKey("focusable"));
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Single(scene.Overlays, overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal));
        Assert.Same(root.Single("TextFieldInput"),input.Focused);
        Assert.True(semantics.Descendants().Single(node=>node.AutomationId=="open").IsExpanded);
        Assert.True(input.KeyDown("Escape",KeyModifiers.None,false)); Update();
        Assert.Empty(scene.Overlays); Assert.Same(trigger,input.Focused);
        Assert.False(semantics.Descendants().Single(node=>node.AutomationId=="open").IsExpanded);
    }

    [Fact]
    public async Task Closing_a_popover_dismisses_its_separately_hosted_context_menu()
    {
        var root = host.Render<PopoverFixture>(); var fixture = (PopoverFixture)root.Component; Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        input.KeyDown("ContextMenu",KeyModifiers.None,false); Update();
        Assert.Equal(2,scene.Overlays.Count(overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal)));
        await fixture.CloseAsync(); Update();
        Assert.Empty(scene.Overlays); Assert.Same(trigger,input.Focused);
    }

    [Fact]
    public void Dialog_names_describes_and_confines_keyboard_and_accessibility_actions()
    {
        var root = host.Render<DialogFixture>(); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        var dialog = Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
        Assert.Equal("Review",dialog.Name); Assert.Equal("Check the details",dialog.Description);
        Assert.Equal("title",dialog.LabeledBy!.AutomationId);
        Assert.Same(root.Single("TextFieldInput"),input.Focused);
        var background = semantics.Descendants().Single(node=>node.AutomationId=="background");
        Assert.False(background.IsEnabled); Assert.False(semantics.Invoke(background));
        input.Focus(background.Element!,visible:true); Assert.Same(root.Single("TextFieldInput"),input.Focused);
        Assert.False(input.Activate(background.Element!));
        input.PointerPressed(new Point(5,5),PointerButton.Primary,KeyModifiers.None);
        input.PointerReleased(new Point(5,5),PointerButton.Primary,KeyModifiers.None); Update();
        Assert.Single(scene.Overlays, overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal));
        var close = root.Elements("Button").Single(node=>node.Id=="close");
        input.Focus(close,visible:true); input.KeyDown("Tab",KeyModifiers.None,false);
        Assert.Same(root.Single("TextFieldInput"),input.Focused);
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Empty(scene.Overlays); Assert.Same(trigger,input.Focused);
        Assert.True(background.IsEnabled);
    }

    [Fact]
    public void Dialog_resolves_initial_focus_after_rendering_and_refreshes_changed_title_ids()
    {
        var root = host.Render<DialogFixture>(); var fixture = (DialogFixture)root.Component;
        fixture.PreferClose(); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal("close",input.Focused!.Id);
        fixture.RenameTitle(); Update();
        var dialog = Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
        Assert.Equal("renamed-title",dialog.LabeledBy!.AutomationId);
        Assert.Equal("Review",dialog.Name);
    }

    [Fact]
    public void Dialog_allows_its_editors_context_menu_and_restores_focus_inside_the_modal_scope()
    {
        var root = host.Render<DialogFixture>(); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        var editor = root.Single("TextFieldInput"); Assert.Same(editor,input.Focused);
        input.KeyDown("ContextMenu",KeyModifiers.None,false); Update();
        Assert.Equal(2,scene.Overlays.Count(overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal)));
        Assert.Equal("menuitem",input.Focused!.Attributes["role"]);
        Assert.True(semantics.Descendants().Single(node=>node.Element==input.Focused).IsEnabled);
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Single(scene.Overlays, overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal)); Assert.Same(editor,input.Focused);
        Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
    }

    [Fact]
    public async Task Closing_a_dialog_dismisses_its_separately_hosted_context_menu()
    {
        var root = host.Render<DialogFixture>(); var fixture = (DialogFixture)root.Component; Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        input.KeyDown("ContextMenu",KeyModifiers.None,false); Update();
        Assert.Equal(2,scene.Overlays.Count(overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal)));
        await fixture.CloseAsync(); Update();
        Assert.Empty(scene.Overlays); Assert.Same(trigger,input.Focused);
    }

    [Fact]
    public async Task Closing_an_owner_dialog_also_closes_a_separately_declared_child_dialog()
    {
        var root = host.Render<DialogFixture>(); var fixture = (DialogFixture)root.Component; Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        var innerTrigger = root.Elements("Button").Single(node=>node.Id=="open-inner");
        input.Focus(innerTrigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal(2,scene.Overlays.Count(overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal))); Assert.Equal("inner-close",input.Focused!.Id);
        await fixture.CloseAsync(); Update();
        Assert.Empty(scene.Overlays); Assert.Same(trigger,input.Focused);
    }

    [Fact]
    public void Alert_dialog_prefers_cancel_routes_escape_through_it_and_keeps_acknowledgements_open()
    {
        var root = host.Render<DialogFixture>(); var fixture = (DialogFixture)root.Component;
        fixture.Configure(alert:true,cancel:true); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.AlertDialog);
        Assert.Equal("Cancel",input.Focused!.Text());
        input.PointerPressed(new Point(5,5),PointerButton.Primary,KeyModifiers.None);
        input.PointerReleased(new Point(5,5),PointerButton.Primary,KeyModifiers.None); Update();
        Assert.Single(scene.Overlays, overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal));
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Equal(1,fixture.Cancels); Assert.Equal(0,fixture.Actions); Assert.Empty(scene.Overlays);
        fixture.Configure(alert:true,cancel:false); Update();
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Single(scene.Overlays, overlay => !overlay.Name.EndsWith("Scrim", StringComparison.Ordinal));
        var alert = Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.AlertDialog);
        Assert.False(semantics.Dismiss(alert));
        var action = root.Elements("Button").Single(node=>node.Text()=="Confirm");
        input.Focus(action,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        Assert.Equal(1,fixture.Actions); Assert.Empty(scene.Overlays);
    }

    [Fact]
    public void Alert_cancel_button_needs_no_extra_click_handler()
    {
        var root = host.Render<DialogFixture>(); var fixture = (DialogFixture)root.Component;
        fixture.Configure(alert:true,cancel:true,cancelHandler:false); Update();
        var trigger = root.Elements("Button").Single(node=>node.Id=="open");
        input.Focus(trigger,visible:true); input.KeyDown("Enter",KeyModifiers.None,false); Update();
        input.KeyDown("Escape",KeyModifiers.None,false); Update();
        Assert.Empty(scene.Overlays); Assert.Equal(0,fixture.Cancels); Assert.Same(trigger,input.Focused);
    }

    private sealed class FormDialogFixture : ComponentBase
    {
        [Parameter] public bool InnerForm { get; set; }
        public int InnerSubmits,OuterSubmits;
        private bool open;
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Controls.Form>(0); b.AddComponentParameter(1,"OnSubmit",EventCallback.Factory.Create(this,()=>OuterSubmits++));
            b.AddComponentParameter(2,"ChildContent",(RenderFragment)(outer=>
            {
                Button(outer, 0, "open", "Open", () => { open = true; StateHasChanged(); });
                outer.OpenComponent<Dialog>(10); outer.AddComponentParameter(1,"AccessibleName","Edit");
                outer.AddComponentParameter(2,"IsOpen",open);
                outer.AddComponentParameter(3,"ChildContent",(RenderFragment)(content=>
                {
                    if (InnerForm)
                    {
                        content.OpenComponent<Controls.Form>(0); content.AddComponentParameter(1,"OnSubmit",EventCallback.Factory.Create(this,()=>InnerSubmits++));
                        content.AddComponentParameter(2,"ChildContent",(RenderFragment)Editor); content.CloseComponent();
                    }
                    else Editor(content);
                })); outer.CloseComponent();
            })); b.CloseComponent();
        }
        private static void Editor(RenderTreeBuilder b) { b.OpenComponent<Controls.TextField>(0); b.CloseComponent(); }
    }

    private sealed class PopoverFixture : ComponentBase
    {
        private Popover? popover;
        public Task CloseAsync() => popover!.CloseAsync();
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Popover>(0); b.AddComponentParameter(1,"Trigger",(RenderFragment)(trigger=>Button(trigger,0,"open","Open",null)));
            b.AddComponentParameter(2,"ChildContent",(RenderFragment)(content=> { content.OpenComponent<Controls.TextField>(0); content.AddComponentParameter(1,"Value","example"); content.CloseComponent(); }));
            b.AddComponentReferenceCapture(3,component=>popover=(Popover)component); b.CloseComponent();
        }
    }

    internal sealed class DialogFixture : ComponentBase
    {
        private bool alert,cancel=true;
        private bool cancelHandler=true;
        private bool preferClose;
        private string titleId="title";
        private Controls.Button? close;
        private DialogBase? dialog;
        private Controls.Button? opener;
        private Dialog? inner;
        public int Cancels,Actions;
        public void Configure(bool alert,bool cancel,bool cancelHandler=true) { this.alert=alert; this.cancel=cancel; this.cancelHandler=cancelHandler; StateHasChanged(); }
        public void PreferClose() { preferClose=true; StateHasChanged(); }
        public void RenameTitle() { titleId="renamed-title"; StateHasChanged(); }
        public Task CloseAsync() => dialog!.CloseAsync();
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            Button(b, 0, "open", "Open", () => _ = dialog!.OpenAsync(), value => opener = value);
            b.OpenComponent(10, alert ? typeof(AlertDialog) : typeof(Dialog));
            b.AddComponentParameter(11, "ReturnFocus", opener?.Element);
            b.AddComponentParameter(12, "InitialFocus", (Func<ElementReference?>)(() => preferClose ? close?.Element : null));
            if (alert)
            {
                b.AddComponentParameter(13, "Title", "Review");
                b.AddComponentParameter(14, "Message", "Check the details");
                b.AddComponentParameter(15, "PreferCancel", true);
                b.AddComponentParameter(16, "Actions", (RenderFragment)(actions =>
                {
                    DialogAction(actions, 0, "confirm", "Confirm", DialogButtonRole.Primary, () => Actions++);
                    if (cancel) DialogAction(actions, 10, "cancel", "Cancel", DialogButtonRole.Cancel, cancelHandler ? () => Cancels++ : null);
                }));
            }
            else
            {
                b.AddComponentParameter(17, "ShowCloseButton", false);
                b.AddComponentParameter(18, "LabeledBy", titleId);
                b.AddComponentParameter(19, "DescribedBy", "details");
                b.AddComponentParameter(20, "ChildContent", (RenderFragment)(content =>
                {
                    content.OpenComponent<Heading>(0);
                    content.AddComponentParameter(1, "Id", titleId);
                    content.AddComponentParameter(2, "ChildContent", Text("Review"));
                    content.CloseComponent();
                    content.OpenComponent<TextBlock>(3);
                    content.AddComponentParameter(4, "Id", "details");
                    content.AddComponentParameter(5, "ChildContent", Text("Check the details"));
                    content.CloseComponent();
                    content.OpenComponent<Controls.TextField>(6);
                    content.AddComponentParameter(7, "AccessibleName", "Name");
                    content.AddComponentParameter(8, "Value", "example");
                    content.CloseComponent();
                    Button(content, 10, "open-inner", "Inner", () => _ = inner!.OpenAsync());
                    Button(content, 20, "close", "Close", () => _ = dialog!.CloseAsync(), value => close = value);
                }));
            }
            b.AddComponentReferenceCapture(21, component => dialog = (DialogBase)component);
            b.CloseComponent();
            Button(b, 30, "background", "Background", () => Actions++);
            b.OpenComponent<Dialog>(40);
            b.AddComponentParameter(41, "AccessibleName", "Inner");
            b.AddComponentParameter(42, "ShowCloseButton", false);
            b.AddComponentParameter(43, "ChildContent", (RenderFragment)(content =>
                Button(content, 0, "inner-close", "Close inner", () => _ = inner!.CloseAsync())));
            b.AddComponentReferenceCapture(44, component => inner = (Dialog)component);
            b.CloseComponent();
        }

        private static void DialogAction(RenderTreeBuilder b, int sequence, string id, string text, DialogButtonRole role, Action? action)
        {
            b.OpenComponent<DialogButton>(sequence);
            b.AddComponentParameter(sequence + 1, "Id", id);
            b.AddComponentParameter(sequence + 2, "Role", role);
            b.AddComponentParameter(sequence + 3, "ChildContent", Text(text));
            if (action is not null) b.AddComponentParameter(sequence + 4, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(action, action));
            b.CloseComponent();
        }
    }
    private static RenderFragment Text(string text) => b=>b.AddContent(0,text);
    private static void Button(RenderTreeBuilder b,int sequence,string id,string text,Action? action,Action<Controls.Button>? capture=null)
    {
        b.OpenComponent<Controls.Button>(sequence); b.AddComponentParameter(sequence+1,"Id",id);
        if (action is not null) b.AddComponentParameter(sequence+2,"OnClick",EventCallback.Factory.Create<PointerEventArgs>(action,_=>action()));
        b.AddComponentParameter(sequence+3,"ChildContent",Text(text));
        if (capture is not null) b.AddComponentReferenceCapture(sequence+4,component=>capture((Controls.Button)component));
        b.CloseComponent();
    }
}
