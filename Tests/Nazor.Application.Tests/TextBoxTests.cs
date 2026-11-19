using Nazor.Graphics.Text;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class TextBoxTests : IDisposable
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
    private readonly Form form;

    public TextBoxTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 240) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<Form>();
        form = (Form)root.Component;
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Box(string id) => root.Elements("TextFieldInput").Single(e => e.Id == id + "-input");

    private TextEditState State(string id) => Box(id).GetFeature<TextEditState>()!;

    private void ClickAt(Point point)
    {
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
    }

    private void ClickEnd(string id)
    {
        var bounds = scene.GetContentBounds(Box(id));
        ClickAt(new Point(bounds.Right - 1, bounds.Y + bounds.Height / 2));
    }

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None) => Assert.True(input.KeyDown(key, modifiers, false), key);

    [Fact]
    public void Typing_updates_the_bound_value_and_the_caret()
    {
        ClickEnd("name");
        Assert.Same(Box("name"), input.FocusedTextBox);
        Assert.Equal(7, State("name").CaretIndex);

        input.TextInput(" 1");
        Assert.Equal("Sensore 1", form.Name);
        Assert.Equal("Nome: Sensore 1", root.Single("TextBlock").Text());
        Assert.Equal("Sensore 1", Box("name").Attributes["value"]);
        Assert.Equal(9, State("name").CaretIndex);
    }

    [Fact]
    public void The_maximum_length_cuts_typed_and_pasted_text_at_whole_characters()
    {
        form.FreeMaxLength = 5;
        form.Refresh();
        host.RunPending();
        ClickEnd("free");
        input.TextInput("abcdefg");
        Assert.Equal("abcde", State("free").Value);
        input.TextInput("x");
        Assert.Equal("abcde", State("free").Value);
        Key("ArrowLeft", KeyModifiers.Shift);
        Key("ArrowLeft", KeyModifiers.Shift);
        input.Clipboard.SetText("1\U0001F600");
        Key("v", KeyModifiers.Control);
        Assert.Equal("abc1", State("free").Value);
        input.TextInput("\U0001F600");
        Assert.Equal("abc1", State("free").Value);
        input.TextInput("2");
        Assert.Equal("abc12", State("free").Value);
    }

    [Fact]
    public void Undo_and_redo_go_back_and_forth_and_typing_in_a_row_is_one_step()
    {
        ClickEnd("free");
        input.TextInput("a");
        input.TextInput("b");
        input.TextInput("c");
        Key("Backspace");
        Key("ArrowLeft");
        input.TextInput("x");
        Assert.Equal("axb", State("free").Value);

        Key("z", KeyModifiers.Control);
        Assert.Equal("ab", State("free").Value);
        Assert.Equal(1, State("free").CaretIndex);
        Key("z", KeyModifiers.Control);
        Assert.Equal("abc", State("free").Value);
        Key("z", KeyModifiers.Control);
        Assert.Equal("", State("free").Value);
        Key("z", KeyModifiers.Control);
        Assert.Equal("", State("free").Value);

        Key("y", KeyModifiers.Control);
        Assert.Equal("abc", State("free").Value);
        Key("Z", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal("ab", State("free").Value);
        Assert.Equal(1, State("free").CaretIndex);
        input.TextInput("!");
        Key("y", KeyModifiers.Control);
        Assert.Equal("a!b", State("free").Value);

        ClickEnd("name");
        input.TextInput("?");
        form.Name = "Altro";
        form.Refresh();
        host.RunPending();
        scene.Update();
        Key("z", KeyModifiers.Control);
        Assert.Equal("Altro", State("name").Value);
    }

    [Fact]
    public void Unicode_text_is_inserted_and_deleted_by_grapheme()
    {
        ClickEnd("free");
        input.TextInput("è😀");
        input.TextInput("é");
        Assert.Equal("è😀é", Box("free").Attributes["value"]);
        Key("Backspace");
        Assert.Equal("è😀", Box("free").Attributes["value"]);
        Key("Backspace");
        Assert.Equal("è", Box("free").Attributes["value"]);
        Key("ArrowLeft");
        Assert.Equal(0, State("free").CaretIndex);
        Key("Delete");
        Assert.Equal("", Box("free").Attributes["value"]);
    }

    [Fact]
    public void Shift_and_control_navigation_select_and_replace()
    {
        ClickEnd("name");
        Key("ArrowLeft", KeyModifiers.Shift);
        Key("ArrowLeft", KeyModifiers.Shift);
        Assert.Equal((5, 7), (State("name").SelectionStart, State("name").SelectionEnd));
        input.TextInput("ri");
        Assert.Equal("Sensori", form.Name);

        input.TextInput(" laboratorio");
        Key("ArrowLeft", KeyModifiers.Control);
        Assert.Equal(8, State("name").CaretIndex);
        Key("Backspace", KeyModifiers.Control);
        Assert.Equal("laboratorio", form.Name);
        Key("a", KeyModifiers.Control);
        Key("Delete");
        Assert.Equal("", form.Name);
    }

    private ElementNode? TextMenu()
    {
        host.RunPending();
        scene.Update();
        return scene.Overlays.SingleOrDefault(o => o.Name == "ContextMenu");
    }

    private static string Items(ElementNode menu) =>
        string.Join(", ", menu.Children.OfType<ElementNode>().Select(i => i.Text() + (i.Attributes["disabled"] is true ? " (off)" : "")));

    [Fact]
    public void A_right_click_opens_the_text_menu_and_its_commands_keep_the_selection()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("it-IT");
        try
        {
            input.Clipboard = new MemoryClipboard();
            var box = scene.GetContentBounds(Box("name"));
            var point = new Point(box.X + 2, box.Y + box.Height / 2);
            input.PointerPressed(point, PointerButton.Secondary, KeyModifiers.None);
            input.PointerReleased(point, PointerButton.Secondary, KeyModifiers.None);
            var menu = TextMenu()!;
            Assert.Equal(0, State("name").CaretIndex);
            Assert.Equal("Annulla (off), Taglia (off), Copia (off), Incolla (off), Seleziona tutto", Items(menu));
            Assert.Equal(point, new Point(scene.GetBounds(menu).X, scene.GetBounds(menu).Y));
            Assert.Equal("Seleziona tutto", input.Focused!.Text());
            Key("Enter");
            Assert.Null(TextMenu());
            Assert.Same(Box("name"), input.FocusedTextBox);
            Assert.Equal((0, 7), (State("name").SelectionStart, State("name").SelectionEnd));

            input.KeyDown("F10", KeyModifiers.Shift, false);
            menu = TextMenu()!;
            Assert.Equal("Annulla (off), Taglia, Copia, Incolla (off), Seleziona tutto", Items(menu));
            Assert.Equal("Taglia", input.Focused!.Text());
            Key("ArrowDown");
            Key("Enter");
            Assert.Equal("Sensore", input.Clipboard.GetText());
            Assert.Equal((0, 7), (State("name").SelectionStart, State("name").SelectionEnd));
            input.KeyDown("ContextMenu", KeyModifiers.None, false);
            menu = TextMenu()!;
            Key("ArrowDown");
            Key("ArrowDown");
            Assert.Equal("Incolla", input.Focused!.Text());
            Key("Escape");
            Assert.Null(TextMenu());
            Assert.Equal("Sensore", form.Name);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [Fact]
    public void The_text_menu_waits_for_a_pending_change_and_disabled_boxes_have_none()
    {
        ClickEnd("free");
        input.TextInput("nuovo");
        input.KeyDown("F10", KeyModifiers.Shift, false);
        Assert.NotNull(TextMenu());
        Key("Escape");
        Assert.Null(TextMenu());
        Assert.Empty(form.Changes);
        Assert.Equal((5, 5), (State("free").SelectionStart, State("free").CaretIndex));
        Key("Tab", KeyModifiers.Shift);
        Assert.Equal(["nuovo"], form.Changes);

        var locked = scene.GetContentBounds(Box("locked"));
        input.PointerPressed(new Point(locked.X + 2, locked.Y + 2), PointerButton.Secondary, KeyModifiers.None);
        input.PointerReleased(new Point(locked.X + 2, locked.Y + 2), PointerButton.Secondary, KeyModifiers.None);
        Assert.Null(TextMenu());

        form.OwnContextMenu = true;
        form.Refresh();
        host.RunPending();
        var free = scene.GetContentBounds(Box("free"));
        input.PointerPressed(new Point(free.X + 2, free.Y + 2), PointerButton.Secondary, KeyModifiers.None);
        input.PointerReleased(new Point(free.X + 2, free.Y + 2), PointerButton.Secondary, KeyModifiers.None);
        Assert.Null(TextMenu());
        Assert.Equal(["nuovo", "menu"], form.Changes);
    }

    [Fact]
    public void Clipboard_copies_cuts_and_pastes_single_line_text()
    {
        var clipboard = new MemoryClipboard();
        input.Clipboard = clipboard;
        ClickEnd("name");
        Key("Home", KeyModifiers.Shift);
        Key("c", KeyModifiers.Control);
        Assert.Equal("Sensore", clipboard.GetText());

        Key("End");
        clipboard.SetText(" A\r\nB");
        Key("v", KeyModifiers.Control);
        Assert.Equal("Sensore A B", form.Name);

        Key("Home");
        Key("ArrowRight", KeyModifiers.Shift | KeyModifiers.Control);
        Key("x", KeyModifiers.Control);
        Assert.Equal(" A B", form.Name);
        Assert.Equal("Sensore", clipboard.GetText());
    }

    [Fact]
    public void Pointer_places_the_caret_and_double_click_selects_a_word()
    {
        ClickEnd("name");
        input.TextInput(" esterno");
        var bounds = scene.GetContentBounds(Box("name"));
        var layout = scene.GetEditableLayout(Box("name"))!;
        var x = bounds.X + layout.GetCaretX(3) - State("name").ScrollX;
        input.PointerPressed(new Point(bounds.X + 1, bounds.Y + 5), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(new Point(bounds.X + 1, bounds.Y + 5), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal(0, State("name").CaretIndex);

        input.PointerPressed(new Point(bounds.X + 1, bounds.Y + 11), PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(new Point(x, bounds.Y + 5), KeyModifiers.None);
        input.PointerReleased(new Point(x, bounds.Y + 5), PointerButton.Primary, KeyModifiers.None);
        Assert.Equal((0, 3), (State("name").SelectionStart, State("name").SelectionEnd));

        Thread.Sleep(550);
        var inSecondWord = new Point(bounds.X + layout.GetCaretX(10) - State("name").ScrollX, bounds.Y + 5);
        ClickAt(inSecondWord);
        ClickAt(inSecondWord);
        Assert.Equal("esterno", form.Name[State("name").SelectionStart..State("name").SelectionEnd]);
    }

    [Fact]
    public void Keyboard_focus_selects_all_and_disabled_boxes_are_skipped()
    {
        Key("Tab");
        Assert.Same(Box("name"), input.Focused);
        Assert.Equal((0, 7), (State("name").SelectionStart, State("name").SelectionEnd));
        Key("Tab");
        Assert.Same(Box("free"), input.Focused);
        Key("Tab");
        Assert.Same(Box("name"), input.Focused);
    }

    [Fact]
    public void Ime_composition_is_shown_inline_and_committed_once()
    {
        ClickEnd("free");
        input.TextInput("a");
        input.CompositionUpdate("にほ", 2);
        scene.Update();
        Assert.Equal("a", Box("free").Attributes["value"]);
        Assert.Equal("aにほ", scene.GetEditableLayout(Box("free"))!.Text);
        Assert.Equal(3, State("free").DisplayCaret);
        input.TextInput("x");
        Assert.Equal("a", Box("free").Attributes["value"]);

        input.CompositionCommit("日本");
        Assert.Equal("a日本", Box("free").Attributes["value"]);
        Assert.Null(State("free").Composition);

        input.CompositionUpdate("ご", 1);
        input.CompositionUpdate("", 0);
        scene.Update();
        Assert.Equal("a日本", scene.GetEditableLayout(Box("free"))!.Text);
    }

    [Fact]
    public void A_transformed_box_maps_clicks_and_the_caret()
    {
        scene.Styles.Application = Nazor.Nss.NssCompiler.Compile("t", "TextFieldInput#name-input { transform: translateX(40px) scale(2); transform-origin: 0 0; }").StyleSheet;
        scene.Update();
        var box = Box("name");
        var transform = scene.GetTransform(box);
        var content = scene.GetContentBox(box);
        var layout = scene.GetEditableLayout(box)!;
        var end = new Point(content.X + layout.GetCaretX(3), content.Y + content.Height / 2);
        ClickAt(transform.Transform(end));
        Assert.Equal(3, State("name").CaretIndex);
        var caret = input.GetTextCaretRect()!.Value;
        Assert.Equal(transform.Transform(end).X, caret.X, 0.01f);
        Assert.Equal(layout.GetCaretRect(3).Height * 2, caret.Height, 0.01f);
    }

    [Fact]
    public void Long_text_scrolls_horizontally_to_keep_the_caret_visible()
    {
        ClickEnd("free");
        input.TextInput("Una frase abbastanza lunga da superare la larghezza del campo");
        scene.Update();
        var state = State("free");
        var content = scene.GetContentBounds(Box("free"));
        Assert.True(state.ScrollX > 0);
        var caret = input.GetTextCaretRect()!.Value;
        Assert.InRange(caret.X, content.X, content.Right);

        Key("Home");
        Assert.Equal(0, state.ScrollX);
    }

    [Fact]
    public async Task External_value_changes_move_the_caret_to_the_end()
    {
        ClickEnd("name");
        Key("Home");
        await host.Dispatcher.InvokeAsync(() => { form.Name = "Nuovo valore"; form.Refresh(); });
        Assert.Equal(12, State("name").CaretIndex);
        input.TextInput("!");
        Assert.Equal("Nuovo valore!", form.Name);
    }

    [Fact]
    public void Caret_selection_and_preedit_are_drawn()
    {
        ClickEnd("name");
        Key("ArrowLeft", KeyModifiers.Shift);
        scene.Update();
        var commands = scene.BuildDisplayList().Commands;
        Assert.Contains(commands, c => c is FillRectangle { Color.A: 0x60 });
        var caretCommand = commands.OfType<FillRectangle>().Last(c => c.Rect.Width <= 1.01f);
        Assert.Equal(1, caretCommand.Rect.Width, 2);

        Key("End");
        input.CompositionUpdate("ab", 2);
        scene.Update();
        Assert.Contains(scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "Sensoreab");
    }
}
