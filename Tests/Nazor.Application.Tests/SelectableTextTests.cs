using Nazor.Graphics.Text;
using System.Globalization;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class SelectableTextTests : IDisposable
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

    public SelectableTextTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 300) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene) { Clipboard = new MemoryClipboard() };
        root = host.Render<Article>();
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Element(string id) => root.Elements().Single(e => e.Id == id);

    private TextEditState? State(string id) => Element(id).GetFeature<TextEditState>();

    private Point At(string id, int index)
    {
        scene.Update();
        var content = scene.GetContentBounds(Element(id));
        var layout = scene.GetEditableLayout(Element(id))!;
        return new Point(content.X + layout.GetCaretX(index), content.Y + layout.Lines[0].Height / 2);
    }

    private void Click(Point point, int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
            input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        }
        host.RunPending();
        scene.Update();
    }

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        input.KeyDown(key, modifiers, false);
        host.RunPending();
        scene.Update();
    }

    [Fact]
    public void A_drag_selects_text_that_ctrl_c_copies_and_a_click_elsewhere_drops()
    {
        input.PointerPressed(At("body", 7), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(At("body", 13), KeyModifiers.None);
        input.PointerReleased(At("body", 13), PointerButton.Primary, KeyModifiers.None);
        Assert.Same(Element("body"), input.Focused);
        Assert.Null(input.FocusedTextBox);
        Assert.Equal((7, 13), (State("body")!.SelectionStart, State("body")!.SelectionEnd));
        Key("c", KeyModifiers.Control);
        Assert.Equal("numero", input.Clipboard.GetText());
        var highlights = scene.BuildDisplayList().Commands.OfType<FillRectangle>().Count(f => f.Color.A == 0x60);
        Assert.Equal(1, highlights);

        var ok = scene.GetBounds(Element("ok"));
        Click(new Point(ok.X + 4, ok.Y + 4));
        Assert.False(State("body")!.HasSelection);
    }

    [Fact]
    public void Double_and_triple_clicks_select_a_word_and_all_and_tab_does_not_stop()
    {
        Click(At("body", 9), times: 2);
        Assert.Equal("numero", State("body")!.Value[State("body")!.SelectionStart..State("body")!.SelectionEnd]);
        Click(At("body", 9), times: 3);
        Assert.Equal((0, 26), (State("body")!.SelectionStart, State("body")!.SelectionEnd));
        Key("Tab");
        Assert.Same(Element("ok"), input.Focused);
    }

    [Fact]
    public void User_select_none_and_all_and_the_short_menu()
    {
        input.PointerPressed(At("locked", 0), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(At("locked", 5), KeyModifiers.None);
        input.PointerReleased(At("locked", 5), PointerButton.Primary, KeyModifiers.None);
        Assert.Null(State("locked"));
        Key("c", KeyModifiers.Control);
        Assert.Null(input.Clipboard.GetText());

        Click(At("code", 3));
        Assert.Equal((0, 8), (State("code")!.SelectionStart, State("code")!.SelectionEnd));
        Click(At("plain", 3));
        Assert.Null(State("plain"));

        var culture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("it-IT");
        try
        {
            Click(At("code", 3));
            input.PointerPressed(At("code", 3), PointerButton.Secondary, KeyModifiers.None);
            input.PointerReleased(At("code", 3), PointerButton.Secondary, KeyModifiers.None);
            host.RunPending();
            scene.Update();
            var menu = scene.Overlays.Single(o => o.Name == "ContextMenu");
            Assert.Equal(["Copia", "Seleziona tutto"], menu.Children.OfType<ElementNode>().Select(i => i.Text()));
            Key("Enter");
            Assert.Equal("AB-77-XY", input.Clipboard.GetText());
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
