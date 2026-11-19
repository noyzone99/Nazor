using Nazor.Graphics.Text;
using System.Globalization;
using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class NumberBoxTests : IDisposable
{
    private readonly CultureInfo culture = CultureInfo.CurrentCulture;
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
    private readonly Quantities form;

    public NumberBoxTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("it-IT");
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 240) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        root = host.Render<Quantities>();
        form = (Quantities)root.Component;
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
        CultureInfo.CurrentCulture = culture;
    }

    private ElementNode Box => root.Elements("NumberFieldInput").Single();

    private string Text => Box.Text();

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        input.KeyDown(key, modifiers, false);
        input.KeyUp(key, modifiers);
        host.RunPending();
        scene.Update();
    }

    private void Type(string text)
    {
        Key("a", KeyModifiers.Control);
        input.TextInput(text);
        host.RunPending();
    }

    [Fact]
    public void Typed_text_becomes_the_value_when_committed_and_bad_text_goes_back()
    {
        Key("Tab");
        Assert.Same(Box, input.FocusedTextBox);
        Assert.Equal("2", Text);
        Type("12,5");
        Assert.Equal(2, form.Quantity);
        Key("Enter");
        Assert.Equal(12.5, form.Quantity);
        Assert.Equal("12,5", Text);

        Type("dodici");
        Key("Enter");
        Assert.Equal(12.5, form.Quantity);
        Assert.Equal("12,5", Text);

        Type("1.500");
        Key("Tab");
        Assert.Equal(100, form.Quantity);
        Assert.Equal("100", Text);

        Key("Tab", KeyModifiers.Shift);
        Key("a", KeyModifiers.Control);
        Key("Delete");
        Key("Enter");
        Assert.Null(form.Quantity);
        Assert.Equal("", Text);
    }

    [Fact]
    public void Arrows_and_pages_step_the_value_inside_the_range()
    {
        Key("Tab");
        Key("ArrowUp");
        Assert.Equal(3, form.Quantity);
        Assert.Equal("3", Text);
        Key("PageUp");
        Assert.Equal(13, form.Quantity);
        Key("PageDown");
        Key("PageDown");
        Assert.Equal(0, form.Quantity);
        Key("ArrowDown");
        Assert.Equal(0, form.Quantity);
        Key("End");
        Key("ArrowLeft");
        Assert.Equal(0, Box.GetFeature<TextEditState>()!.CaretIndex);
    }

    [Fact]
    public void Assistive_technologies_read_and_set_the_number()
    {
        using var tree = new SemanticTree(host.Renderer, scene, input);
        tree.Update();
        var node = tree.Descendants().Single(n => n.AutomationId == "quantity-input");
        Assert.Equal(SemanticRole.TextBox, node.Role);
        Assert.Equal("2", node.Value);
        Assert.Equal(new RangeInfo(0, 100, 2, 1, 10, false), node.Range);
        Assert.True(tree.SetRangeValue(node, 42.5));
        host.RunPending();
        Assert.Equal(42.5, form.Quantity);
        tree.Update();
        Assert.Equal("42,5", node.Value);
    }
}
