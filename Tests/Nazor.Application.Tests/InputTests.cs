using Nazor.Graphics.Text;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class InputTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly Toolbar toolbar;

    public InputTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(300, 200) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<Toolbar>();
        toolbar = (Toolbar)root.Component;
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Button(string id) => root.Elements("Button").Single(b => b.Id == id);

    private Point Center(string id)
    {
        var bounds = scene.GetBounds(Button(id));
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Fact]
    public void Hover_marks_the_element_and_its_ancestors()
    {
        input.PointerMoved(Center("first"), KeyModifiers.None);
        Assert.True(Button("first").States.HasFlag(ElementStates.Hover));
        Assert.True(root.Single("Container").States.HasFlag(ElementStates.Hover));
        Assert.Equal(Nazor.Style.Cursor.Default, input.CursorAt(Center("first")));

        input.PointerMoved(Center("second"), KeyModifiers.None);
        Assert.False(Button("first").States.HasFlag(ElementStates.Hover));
        Assert.True(Button("second").States.HasFlag(ElementStates.Hover));

        input.PointerLeft();
        Assert.Equal(ElementStates.None, root.Single("Container").States & ElementStates.Hover);
    }

    [Fact]
    public void Click_requires_press_and_release_on_the_same_element()
    {
        input.PointerPressed(Center("first"), PointerButton.Primary, KeyModifiers.None);
        Assert.True(Button("first").States.HasFlag(ElementStates.Pressed));
        input.PointerMoved(Center("second"), KeyModifiers.None);
        Assert.False(Button("first").States.HasFlag(ElementStates.Pressed));
        input.PointerReleased(Center("second"), PointerButton.Primary, KeyModifiers.None);
        Assert.Empty(toolbar.Clicks);

        input.PointerPressed(Center("second"), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(Center("second"), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(["second"], toolbar.Clicks);
        Assert.Same(Button("second"), input.Focused);
        Assert.False(Button("second").States.HasFlag(ElementStates.FocusVisible));
    }

    [Fact]
    public void Secondary_button_and_cancellation_do_not_click()
    {
        input.PointerPressed(Center("first"), PointerButton.Secondary, KeyModifiers.None);
        input.PointerReleased(Center("first"), PointerButton.Secondary, KeyModifiers.None);
        input.PointerPressed(Center("first"), PointerButton.Primary, KeyModifiers.None);
        input.PointerCanceled();
        input.PointerReleased(Center("first"), PointerButton.Primary, KeyModifiers.None);
        Assert.Empty(toolbar.Clicks);
        Assert.False(Button("first").States.HasFlag(ElementStates.Pressed));
    }

    [Fact]
    public void Tab_moves_focus_in_tree_order_skipping_disabled_controls()
    {
        Assert.True(input.KeyDown("Tab", KeyModifiers.None, false));
        Assert.Same(Button("first"), input.Focused);
        Assert.True(Button("first").States.HasFlag(ElementStates.FocusVisible));
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Button("second"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Button("first"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.Shift, false);
        Assert.Same(Button("second"), input.Focused);
        Assert.False(Button("first").States.HasFlag(ElementStates.Focus));
    }

    private void Style(string nss)
    {
        scene.Styles.Application = Nazor.Nss.NssCompiler.Compile("Test", nss).StyleSheet;
        scene.Update();
    }

    [Theory]
    [InlineData("#second { display: none; }")]
    [InlineData("#second { visibility: hidden; }")]
    public void Tab_skips_controls_that_are_not_shown(string nss)
    {
        Style(nss);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Button("first"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Button("first"), input.Focused);
        input.KeyDown("Tab", KeyModifiers.Shift, false);
        Assert.Same(Button("first"), input.Focused);
    }

    [Fact]
    public void Controls_inside_a_hidden_container_are_not_in_the_tab_order()
    {
        Style(".toolbar { display: none; }");
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Null(input.Focused);
        input.Focus(Button("first"), visible: true);
        Assert.Null(input.Focused);
    }

    [Theory]
    [InlineData("#first { display: none; }")]
    [InlineData("#first { visibility: hidden; }")]
    [InlineData(".toolbar { display: none; }")]
    public void A_focused_control_that_stops_being_shown_loses_the_focus(string nss)
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Button("first"), input.Focused);
        Style(nss);
        Assert.Null(input.Focused);
        Assert.False(Button("first").States.HasFlag(ElementStates.Focus));
        Assert.False(Button("first").States.HasFlag(ElementStates.FocusVisible));
    }

    [Fact]
    public void Transparent_controls_still_receive_the_pointer_and_the_focus()
    {
        Style("#first { opacity: 0; }");
        Assert.Same(Button("first"), scene.HitTest(Center("first")));
        input.PointerPressed(Center("first"), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(Center("first"), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(["first"], toolbar.Clicks);
        Assert.Same(Button("first"), input.Focused);

        var center = Center("first");
        Style("#first { visibility: hidden; }");
        Assert.NotSame(Button("first"), scene.HitTest(center));
    }

    [Fact]
    public void Enter_activates_on_key_down_and_space_on_key_up()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        input.KeyDown("Enter", KeyModifiers.None, false);
        input.KeyDown("Enter", KeyModifiers.None, repeat: true);
        Assert.Equal(["first:key"], toolbar.Clicks);

        input.KeyDown(" ", KeyModifiers.None, false);
        Assert.True(Button("first").States.HasFlag(ElementStates.Pressed));
        Assert.Single(toolbar.Clicks);
        input.KeyUp(" ", KeyModifiers.None);
        Assert.Equal(["first:key", "first:key"], toolbar.Clicks);

        input.KeyDown(" ", KeyModifiers.None, false);
        input.KeyDown("Escape", KeyModifiers.None, false);
        input.KeyUp(" ", KeyModifiers.None);
        Assert.Equal(2, toolbar.Clicks.Count);
    }

    [Fact]
    public async Task Disabled_state_follows_the_attribute_and_blocks_interaction()
    {
        Assert.True(Button("third").States.HasFlag(ElementStates.Disabled));
        input.PointerPressed(Center("third"), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(Center("third"), PointerButton.Primary, KeyModifiers.None);
        Assert.Empty(toolbar.Clicks);
        Assert.Null(input.Focused);

        await host.Dispatcher.InvokeAsync(() => { toolbar.ThirdDisabled = false; toolbar.Refresh(); });
        scene.Update();
        Assert.False(Button("third").States.HasFlag(ElementStates.Disabled));
        input.PointerPressed(Center("third"), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(Center("third"), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(["third"], toolbar.Clicks);
    }

    [Fact]
    public void State_changes_notify_the_host_and_restyle_the_element()
    {
        var notifications = 0;
        input.VisualStateChanged += () => notifications++;
        var normal = scene.GetStyle(Button("first")).Get(StyleProperties.BorderLeftColor);
        input.PointerMoved(Center("first"), KeyModifiers.None);
        Assert.True(notifications > 0);
        Assert.NotEqual(normal, scene.GetStyle(Button("first")).Get(StyleProperties.BorderLeftColor));
    }
}
