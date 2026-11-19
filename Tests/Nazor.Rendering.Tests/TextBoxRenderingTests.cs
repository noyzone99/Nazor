using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Fields : ComponentBase
{
    public string Name { get; set; } = "Sensore 12";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Fields));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<TextField>(3, typeof(Fields));
            page.AddComponentParameter(4, "Id", "name");
            page.AddComponentParameter(5, "Value", Name);
            page.AddComponentParameter(6, "ValueChanged", EventCallback.Factory.Create<string>(this, (string value) => Name = value));
            page.CloseComponent();
            page.OpenComponent<TextField>(7, typeof(Fields));
            page.AddComponentParameter(8, "Id", "notes");
            page.AddComponentParameter(9, "Value", "Una nota più lunga della casella di testo");
            page.CloseComponent();
            page.OpenComponent<TextField>(10, typeof(Fields));
            page.AddComponentParameter(11, "Id", "locked");
            page.AddComponentParameter(12, "Value", "Non modificabile");
            page.AddComponentParameter(13, "Disabled", true);
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class TextBoxRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;

    public TextBoxRenderingTests()
    {
        var sheet = NssCompiler.Compile("Fields", """
            .page { display: flex; flex-direction: column; gap: 10px; padding: 12px; }
            """).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Fields), () => sheet);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<Fields>();
        harness.Render(260, 140, 1);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Box(string id) => root.Elements("TextFieldInput").Single(e => e.Id == id + "-input");

    [Theory]
    [InlineData("center")]
    [InlineData("right")]
    public void Editing_preserves_text_alignment_and_uses_it_for_caret_and_pointer_coordinates(string alignment)
    {
        harness.Scene.Styles.Application = NssCompiler.Compile("alignment", $"TextFieldInput {{ text-align: {alignment}; }}").StyleSheet;
        var before = harness.Render(260, 140, 1);
        var box = Box("name");
        var content = harness.Scene.GetContentBox(box);
        var drawing = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text == "Sensore 12");
        var offset = drawing.LineOffset(drawing.Layout.Lines[0]);
        input.Focus(box, visible: false);
        input.KeyDown("Home", KeyModifiers.None, false);
        harness.Render(260, 140, 1);
        Assert.InRange(input.GetTextCaretRect()!.Value.X, content.X + offset - 0.01f, content.X + offset + 0.01f);
        Assert.Equal(0, input.GetTextIndexAt(box, new Point(content.X + offset, content.Y + content.Height / 2)));
        input.KeyDown("End", KeyModifiers.None, false);
        Assert.Equal("Sensore 12".Length, input.GetTextIndexAt(box,
            new Point(content.X + offset + drawing.Layout.Width, content.Y + content.Height / 2)));
        input.Focus(null, visible: false);
        var after = harness.Render(260, 140, 1);
        Assert.Equal(before.Pixels, after.Pixels);
    }


    [Fact]
    public void Mac_editing_uses_Command_and_Option_without_relabeling_modifiers()
    {
        input.TextPolicy = TextInputPolicy.MacOS;
        input.KeyDown("Tab", KeyModifiers.None, false);
        input.KeyDown("End", KeyModifiers.None, false);
        input.KeyDown("ArrowLeft", KeyModifiers.Alt, false);
        input.KeyDown("Delete", KeyModifiers.None, false);
        input.KeyDown("z", KeyModifiers.Meta, false);
        input.KeyDown("a", KeyModifiers.Meta, false);
        input.TextInput("Changed");
        Assert.Equal("Changed", ((Fields)root.Component).Name);
        input.KeyDown("z", KeyModifiers.Meta, false);
        Assert.Equal("Sensore 12", ((Fields)root.Component).Name);
        input.KeyDown("z", KeyModifiers.Meta | KeyModifiers.Shift, false);
        Assert.Equal("Changed", ((Fields)root.Component).Name);
    }

    [Fact]
    public void Focused_box_with_selection_matches_the_reference_image()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        input.KeyDown("End", KeyModifiers.None, false);
        input.KeyDown("ArrowLeft", KeyModifiers.Shift | KeyModifiers.Control, false);
        Assert.Same(Box("name"), input.FocusedTextBox);
        var image = harness.Render(260, 140, 1.5f);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "textboxes-260x140@1.5x");
    }

    [Fact]
    public void Preedit_is_underlined_at_the_caret()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        input.KeyDown("End", KeyModifiers.None, false);
        input.CompositionUpdate("日本", 2);
        harness.Render(260, 140, 1);
        var commands = harness.Scene.BuildDisplayList().Commands;
        var text = Assert.Single(commands.OfType<DrawText>(), t => t.Layout.Text.StartsWith("Sensore", StringComparison.Ordinal));
        Assert.Equal("Sensore 12日本", text.Layout.Text);
        Assert.Contains(commands.OfType<FillRectangle>(), r => r.Rect.Height == 1 && r.Rect.Width > 10);
    }
}
