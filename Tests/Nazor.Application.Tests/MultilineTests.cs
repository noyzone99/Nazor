using Nazor.Graphics.Text;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class MultilineTests : IDisposable
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
    private readonly Notes notes;

    public MultilineTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 400) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<Notes>();
        notes = (Notes)root.Component;
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode Box(string id) => root.Elements("TextAreaInput").Single(e => e.Id == id + "-input");

    private TextEditState State(string id) => Box(id).GetFeature<TextEditState>()!;

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Assert.True(input.KeyDown(key, modifiers, false), key);
        host.RunPending();
        scene.Update();
    }

    private void Type(string text)
    {
        input.TextInput(text);
        host.RunPending();
        scene.Update();
    }

    private void Focus(string id)
    {
        input.Focus(Box(id), visible: false);
        scene.Update();
    }

    [Fact]
    public void Enter_breaks_the_line_and_the_change_waits_for_the_focus_to_leave()
    {
        Focus("note");
        Type("uno");
        Key("Enter");
        Type("due");
        Assert.Equal("uno\ndue", notes.Note);
        Assert.Empty(notes.Changes);
        Assert.Equal(2, scene.GetEditableLayout(Box("note"))!.Lines.Count);
        var height = scene.GetBounds(Box("note")).Height;
        Assert.True(height >= scene.GetEditableLayout(Box("note"))!.Height);
        Key("Enter");
        Key("Enter");
        Key("Enter");
        Type("cinque");
        Assert.True(scene.GetBounds(Box("note")).Height > height);
        Key("Tab");
        Assert.Equal(["uno\ndue\n\n\ncinque"], notes.Changes);
    }

    [Fact]
    public void The_arrows_move_between_lines_keeping_the_column_and_home_and_end_stay_on_the_line()
    {
        Focus("note");
        Type("abcde\nx\nabcde");
        Assert.Equal(13, State("note").CaretIndex);
        Key("ArrowUp");
        Assert.Equal(7, State("note").CaretIndex);
        Key("ArrowUp");
        Assert.Equal(5, State("note").CaretIndex);
        Key("ArrowUp");
        Assert.Equal(0, State("note").CaretIndex);
        Key("ArrowDown", KeyModifiers.Shift);
        Assert.Equal((0, 6), (State("note").SelectionStart, State("note").SelectionEnd));
        Key("End");
        Assert.Equal(7, State("note").CaretIndex);
        Key("Home");
        Assert.Equal(6, State("note").CaretIndex);
        Key("End", KeyModifiers.Control);
        Assert.Equal(13, State("note").CaretIndex);
        Key("ArrowDown");
        Assert.Equal(13, State("note").CaretIndex);
        Key("Home", KeyModifiers.Control);
        Assert.Equal(0, State("note").CaretIndex);
    }

    [Fact]
    public void Long_text_wraps_a_fixed_box_scrolls_to_the_caret_and_pasted_lines_stay()
    {
        Focus("fixed");
        input.Clipboard = new MemoryClipboard();
        input.Clipboard.SetText("a\r\nb\rc");
        Key("v", KeyModifiers.Control);
        Assert.Equal("a\nb\nc", State("fixed").Value);
        Type("\nuna riga lunga che non sta nella larghezza del campo");
        var layout = scene.GetEditableLayout(Box("fixed"))!;
        Assert.True(layout.Lines.Count >= 5);
        Assert.True(layout.Width <= scene.GetContentBox(Box("fixed")).Width + 0.5f);
        Assert.Equal(72, scene.GetBounds(root.Elements("TextArea").Single(element => element.Id == "fixed")).Height, 0.5f);
        Assert.True(State("fixed").ScrollY > 0);
        var caret = input.GetTextCaretRect()!.Value;
        var content = scene.GetContentBounds(Box("fixed"));
        Assert.True(caret.Bottom <= content.Bottom + 0.5f && caret.Y >= content.Y - 0.5f);
        input.PointerPressed(new Point(content.X + 1, content.Y + 2), PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(new Point(content.X + 1, content.Y + 2), PointerButton.Primary, KeyModifiers.None);
        Assert.True(State("fixed").CaretIndex > 0);
    }
}
