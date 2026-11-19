using Nazor.Graphics.Text;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class EventInputTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false, DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly EventForm form;

    public EventInputTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(400, 350) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<EventForm>();
        form = (EventForm)root.Component;
        scene.Update();
    }

    private Point Center(string id)
    {
        scene.Update();
        var bounds = scene.GetBounds(root.Elements().Single(e => e.Id == id));
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Fact]
    public void Keyboard_bubbles_in_order_and_modifiers_have_independent_effects()
    {
        input.Focus(form.Button.Element.Element!, true);
        form.Log.Clear();
        Assert.False(input.KeyDown("a", KeyModifiers.Shift, true));
        Assert.Equal(["button:keydown:a:Shift:True", "parent:keydown:a:Shift:True"], form.Log);
        form.StopKey = true;
        form.Refresh();
        form.Log.Clear();
        input.KeyDown("Enter", KeyModifiers.None, false);
        Assert.Equal(["button:keydown:Enter:None:False", "button:click", "parent:click"], form.Log);
        form.PreventKey = true;
        form.Refresh();
        form.Log.Clear();
        input.KeyDown("Enter", KeyModifiers.None, false);
        Assert.Equal(["button:keydown:Enter:None:False"], form.Log);
        input.KeyUp("Enter", KeyModifiers.Control);
        Assert.Contains("button:keyup:Enter:Control:False", form.Log);
        Assert.Contains("parent:keyup:Enter:Control:False", form.Log);
    }

    [Fact]
    public void Focus_events_have_related_target_and_only_in_out_bubble()
    {
        input.Focus(form.Box.InputElement.Element!, true);
        Assert.Equal(["box:focus", "box:focusin", "parent:focusin"], form.Log);
        form.Log.Clear();
        input.Focus(form.Button.Element.Element!, true);
        Assert.Equal(["box:blur", "box:focusout", "parent:focusout", "button:focus", "button:focusin", "parent:focusin"], form.Log);
        Assert.Same(form.Box.InputElement.Element, form.RelatedTarget);
    }

    [Fact]
    public async Task Tab_orders_positive_then_zero_and_negative_remains_programmatically_focusable()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(form.Box.InputElement.Element, input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(form.Button.Element.Element, input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Equal("last", input.Focused!.Id);
        input.KeyDown("Tab", KeyModifiers.Shift, false);
        Assert.Same(form.Button.Element.Element, input.Focused);
        await form.Check.FocusAsync();
        Assert.Same(form.Check.Element.Element, input.Focused);
    }

    [Fact]
    public void Shortcuts_precede_focused_editing_consume_repeat_and_unregister()
    {
        input.Focus(form.Box.InputElement.Element!, false);
        form.Log.Clear();
        var shortcut = input.KeyBindings.Register("a", KeyModifiers.Control,
            EventCallback.Factory.Create<KeyboardEventArgs>(form, () => form.Log.Add("shortcut")));
        input.KeyDown("A", KeyModifiers.Control, false);
        input.KeyDown("a", KeyModifiers.Control, true);
        Assert.Equal(["shortcut"], form.Log);
        shortcut.Dispose();
        input.KeyDown("a", KeyModifiers.Control, false);
        Assert.Contains("parent:keydown:a:Control:False", form.Log);
    }

    [Fact]
    public void Pointer_events_capture_outside_bounds_and_release_or_cancel()
    {
        var point = Center("button");
        input.PointerMoved(point, KeyModifiers.None);
        Assert.Equal(["parent:enter", "button:enter", "button:move", "parent:move"], form.Log);
        form.Log.Clear();
        form.CaptureOnDown = true;
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.Control);
        Assert.Same(form.Button.Element.Element, input.CapturedPointer);
        input.PointerMoved(new Point(600, 600), KeyModifiers.None);
        Assert.Contains("button:move", form.Log);
        input.PointerReleased(new Point(600, 600), PointerButton.Primary, KeyModifiers.None);
        Assert.Contains("button:up", form.Log);
        Assert.Null(input.CapturedPointer);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        form.ShowButton = false;
        form.Refresh();
        Assert.Null(input.CapturedPointer);
        Assert.Throws<InvalidOperationException>(() => form.Button.CapturePointer());
    }

    [Fact]
    public void Double_click_context_menu_and_wheel_reach_ancestors()
    {
        var point = Center("button");
        input.PointerDoubleClicked(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Secondary, KeyModifiers.None);
        input.PointerWheel(point, 2, KeyModifiers.Shift);
        Assert.Equal(["button:double", "parent:double", "button:up", "parent:up", "button:context", "parent:context", "button:wheel:2", "parent:wheel:2"], form.Log);
    }

    [Fact]
    public void Text_input_is_live_and_change_commits_once_on_enter_or_blur()
    {
        input.Focus(form.Box.InputElement.Element!, false);
        form.Log.Clear();
        input.TextInput("one");
        input.TextInput("two");
        Assert.Equal("onetwo", form.Text);
        Assert.Equal(["box:input:one:one", "parent:input:one", "box:input:onetwo:onetwo", "parent:input:onetwo"], form.Log);
        input.KeyDown("Enter", KeyModifiers.None, false);
        input.KeyDown("Enter", KeyModifiers.None, false);
        Assert.Equal(1, form.Log.Count(e => e == "box:change:onetwo"));
        input.TextInput("!");
        input.Focus(form.Button.Element.Element!, false);
        Assert.Contains("box:change:onetwo!", form.Log);
        Assert.Contains("parent:change:onetwo!", form.Log);
    }

    [Fact]
    public void CheckBox_bind_and_events_coexist_and_preventDefault_cancels_toggle()
    {
        input.Activate(form.Check.Element.Element!);
        Assert.True(form.Checked);
        Assert.Equal(["check:click", "parent:click", "check:input:True:True", "parent:input:True", "check:change:True", "parent:change:True"], form.Log);
        form.PreventClick = true;
        form.Refresh();
        form.Log.Clear();
        input.Activate(form.Check.Element.Element!);
        Assert.True(form.Checked);
        Assert.Equal(["check:click", "parent:click"], form.Log);
    }

    [Fact]
    public void Event_route_is_snapshotted_when_handler_removes_target()
    {
        form.RemoveOnClick = true;
        input.Activate(form.Button.Element.Element!);
        Assert.Equal(["button:click", "parent:click"], form.Log);
        Assert.True(form.Button.Element.Element!.IsRemoved);
    }

    [Fact]
    public void Window_focus_loss_commits_text_and_emits_blur_then_restores_focus()
    {
        input.Focus(form.Box.InputElement.Element!, false);
        input.TextInput("edited");
        form.Log.Clear();
        input.WindowFocusChanged(false);
        Assert.Equal(["box:change:edited", "parent:change:edited", "box:blur", "box:focusout", "parent:focusout"], form.Log);
        form.Log.Clear();
        input.WindowFocusChanged(true);
        Assert.Equal(["box:focus", "box:focusin", "parent:focusin"], form.Log);
    }

    public void Dispose()
    {
        input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose();
    }
}

public sealed class EventForm : ComponentBase
{
    public List<string> Log { get; } = [];
    public bool PreventKey { get; set; }
    public bool StopKey { get; set; }
    public bool PreventClick { get; set; }
    public bool CaptureOnDown { get; set; }
    public bool RemoveOnClick { get; set; }
    public bool ShowButton { get; set; } = true;
    public string Text { get; private set; } = "";
    public bool Checked { get; private set; }
    public ElementNode? RelatedTarget { get; private set; }
    public Button Button { get; private set; } = null!;
    public global::Nazor.Controls.TextField Box { get; private set; } = null!;
    public CheckBox Check { get; private set; } = null!;
    public void Refresh() => StateHasChanged();

    private Dictionary<string, object?> Events(string name)
    {
        var events = new Dictionary<string, object?>();
        foreach (var (attribute, label) in new[] { ("onpointerenter", "enter"), ("onpointerleave", "leave"), ("onpointermove", "move"),
                     ("onpointerdown", "down"), ("onpointerup", "up"), ("ondblclick", "double"), ("oncontextmenu", "context"), ("onclick", "click") })
            events[attribute] = EventCallback.Factory.Create<PointerEventArgs>(this, () =>
            {
                Log.Add(name + ":" + label);
                if (name == "button" && label == "down" && CaptureOnDown) Button.CapturePointer();
                if (name == "button" && label == "click" && RemoveOnClick) ShowButton = false;
            });
        foreach (var attribute in new[] { "onkeydown", "onkeyup" })
            events[attribute] = EventCallback.Factory.Create<KeyboardEventArgs>(this, e => Log.Add($"{name}:{attribute[2..]}:{e.Key}:{e.Modifiers}:{e.Repeat}"));
        foreach (var attribute in new[] { "onfocus", "onblur", "onfocusin", "onfocusout" })
            events[attribute] = EventCallback.Factory.Create<FocusEventArgs>(this, e =>
            {
                Log.Add(name + ":" + attribute[2..]);
                RelatedTarget = e.RelatedTarget;
            });
        events["onwheel"] = EventCallback.Factory.Create<WheelEventArgs>(this, e => Log.Add($"{name}:wheel:{e.DeltaY}"));
        foreach (var attribute in new[] { "oninput", "onchange" })
            events[attribute] = EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                Log.Add($"{name}:{attribute[2..]}:{e.Value}" + (attribute == "oninput" && name is "box" or "check" ? ":" + (name == "box" ? Text : Checked) : "")));
        if (name == "button")
        {
            events["__internal_preventDefault_onkeydown"] = PreventKey;
            events["__internal_stopPropagation_onkeydown"] = StopKey;
        }
        if (name == "parent") events["__internal_preventDefault_onclick"] = PreventClick;
        return events;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddMultipleAttributes(1, Events("parent"));
        builder.OpenComponent<global::Nazor.Controls.TextField>(10);
        builder.AddAttribute(11, "Id", "box");
        builder.AddAttribute(12, "TabIndex", 1);
        builder.AddAttribute(13, "Value", Text);
        builder.AddAttribute(14, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Text = value));
        builder.AddAttribute(15, "AdditionalAttributes", Events("box"));
        builder.AddComponentReferenceCapture(16, component => Box = (global::Nazor.Controls.TextField)component);
        builder.CloseComponent();
        if (ShowButton)
        {
            builder.OpenComponent<Button>(20);
            builder.AddAttribute(21, "Id", "button");
            builder.AddAttribute(22, "TabIndex", 2);
            builder.AddAttribute(23, "AdditionalAttributes", Events("button"));
            builder.AddAttribute(24, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Button")));
            builder.AddComponentReferenceCapture(25, component => Button = (Button)component);
            builder.CloseComponent();
        }
        builder.OpenComponent<CheckBox>(30);
        builder.AddAttribute(31, "Id", "check");
        builder.AddAttribute(32, "TabIndex", -1);
        builder.AddAttribute(33, "IsChecked", Checked);
        builder.AddAttribute(34, "IsCheckedChanged", EventCallback.Factory.Create<bool>(this, value => Checked = value));
        builder.AddAttribute(35, "AdditionalAttributes", Events("check"));
        builder.AddAttribute(36, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Check")));
        builder.AddComponentReferenceCapture(37, component => Check = (CheckBox)component);
        builder.CloseComponent();
        builder.OpenComponent<Button>(40);
        builder.AddAttribute(41, "Id", "last");
        builder.AddAttribute(42, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Last")));
        builder.CloseComponent();
        builder.CloseElement();
    }
}
