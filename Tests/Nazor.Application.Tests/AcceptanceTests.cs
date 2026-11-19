using Nazor.Graphics.Text;
using DeviceRegistry;
using Microsoft.Extensions.DependencyInjection;
using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Color = Nazor.Style.Color;

namespace Nazor.Application.Tests;

public sealed class AcceptanceTests : IDisposable
{
    private readonly TestHost host = new(new ServiceCollection().AddDeviceRegistry().AddLogging().BuildServiceProvider());
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf"), Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Bold.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree tree;
    private readonly RootComponent root;
    private readonly App app;

    public AcceptanceTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(960, 640) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        scene.Transitions.ReducedMotion = true;
        input = new InputManager(host.Renderer, scene);
        tree = new SemanticTree(host.Renderer, scene, input);
        root = host.Render<App>();
        app = (App)root.Component;
        scene.Update();
    }

    public void Dispose()
    {
        tree.Dispose();
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        graphics.Dispose();
    }

    private ElementNode ById(string id) => root.Elements().Single(e => e.Id == id);

    private IEnumerable<ElementNode> Cards => root.Elements().Where(e => e.Id?.StartsWith("device-", StringComparison.Ordinal) == true);

    private ElementNode SelectButton(string cardId) => ById(cardId).Elements().Single(e => e.Name == "Button");

    private List<string> CardNames => tree.Descendants().Where(n => n.Role == SemanticRole.Group && n.AutomationId?.StartsWith("device-", StringComparison.Ordinal) == true).Select(n => n.Name).ToList();

    private string Status => ById("status").Elements().Any() ? "" : root.Elements().Single(e => e.Id == "status").Children.OfType<TextNode>().Single().Text;

    private void Click(ElementNode element)
    {
        scene.Update();
        var bounds = scene.GetBounds(element);
        var point = new Point(bounds.X + Math.Min(6, bounds.Width / 2), bounds.Y + bounds.Height / 2);
        input.PointerMoved(point, KeyModifiers.None);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        scene.Update();
    }

    private void Key(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        input.KeyDown(key, modifiers, false);
        input.KeyUp(key, modifiers);
        scene.Update();
    }

    private void Type(string text)
    {
        input.TextInput(text);
        scene.Update();
    }

    [Fact]
    public void Starts_with_three_devices_two_of_them_active()
    {
        Assert.Equal(["Sensore temperatura", "Centralina laboratorio", "Misuratore qualità aria"], CardNames);
        Assert.Equal("3 di 3 dispositivi", Status);
        Assert.Equal(2, app.Devices.Count(d => d.Active));
        Assert.Equal("Nazor — Registro dispositivi", tree.Root.Name);
    }

    [Fact]
    public void Filtering_by_name_and_by_state_and_clearing_the_filter()
    {
        Click(ById("filter"));
        Type("misur");
        Assert.Equal(["Misuratore qualità aria"], CardNames);
        Assert.Equal("1 di 3 dispositivi", Status);

        Key("Tab");
        Assert.Equal("active-only", input.Focused?.Id);
        Key(" ");
        Assert.Equal(["Misuratore qualità aria"], CardNames);

        Click(ById("filter"));
        Key("a", KeyModifiers.Control);
        Key("Delete");
        Assert.Equal(["Sensore temperatura", "Misuratore qualità aria"], CardNames);
        Assert.Equal("2 di 3 dispositivi", Status);
    }

    [Fact]
    public void Selecting_and_editing_with_unicode_updates_the_card_without_recreating_the_others()
    {
        var first = ById("device-1");
        var third = ById("device-3");
        Click(SelectButton("device-2"));
        Assert.Equal(2, app.Selected?.Id);
        Assert.Equal("Centralina laboratorio", ById("name-input").Attributes["value"]);

        Click(ById("name"));
        Key("End");
        Type(" è 😀");
        Assert.Equal("Centralina laboratorio è 😀", app.Devices[1].Name);
        Assert.Contains("Centralina laboratorio è 😀", CardNames);
        Click(ById("active"));
        Assert.True(app.Devices[1].Active);
        Assert.Equal("Attivo", ById("device-2").Elements().Where(e => e.Name == "TextBlock").ElementAt(1).Children.OfType<TextNode>().Single().Text);
        Assert.Same(first, ById("device-1"));
        Assert.Same(third, ById("device-3"));
    }

    [Fact]
    public void Reversing_keeps_element_identity_and_focus()
    {
        var cards = Cards.ToList();
        var button = SelectButton("device-1");
        input.Focus(button, visible: true);
        Click(ById("reverse"));
        Assert.Equal(["Misuratore qualità aria", "Centralina laboratorio", "Sensore temperatura"], CardNames);
        Assert.Equal(cards.AsEnumerable().Reverse(), Cards);
        Assert.Same(button, SelectButton("device-1"));
        input.Focus(button, visible: true);
        var reverse = ById("reverse");
        input.Focus(reverse, visible: true);
        Key("Enter");
        Assert.Equal(["Sensore temperatura", "Centralina laboratorio", "Misuratore qualità aria"], CardNames);
        Assert.Same(reverse, input.Focused);
        Assert.Same(button, SelectButton("device-1"));
    }

    [Fact]
    public void Removing_a_device_keeps_the_other_cards_and_clears_the_selection()
    {
        var first = ById("device-1");
        Click(SelectButton("device-3"));
        Click(ById("remove"));
        Assert.Equal(["Sensore temperatura", "Centralina laboratorio"], CardNames);
        Assert.Null(app.Selected);
        Assert.Same(first, ById("device-1"));
        Assert.Null(input.Focused);
        Assert.Equal("2 di 2 dispositivi", Status);
        Assert.Contains(root.Elements(), e => e.Children.OfType<TextNode>().Any(t => t.Text == "Seleziona un dispositivo"));
    }

    [Fact]
    public void Theme_and_width_change_style_and_layout()
    {
        var window = root.Elements().First(e => e.Name == "Window");
        Assert.Equal(Color.FromRgb(0xf3, 0xf3, 0xf3), scene.GetStyle(window).BackgroundColor);
        Click(ById("theme"));
        Assert.True(app.Dark);
        Assert.Equal(Color.FromRgb(0x1c, 0x1c, 0x1c), scene.GetStyle(window).BackgroundColor);
        Assert.Equal(Color.FromRgb(0xf0, 0xf0, 0xf0), scene.GetStyle(window).Color);

        var list = ById("list");
        var detail = root.Elements().Single(e => e.Attributes.GetValueOrDefault("accessible-name") is "Dettaglio dispositivo");
        Assert.True(scene.GetBounds(detail).X > scene.GetBounds(list).Right);
        scene.Viewport = new Size(700, 640);
        scene.Update();
        Assert.True(scene.GetBounds(detail).Y >= scene.GetBounds(list).Bottom, "below 720 units the detail goes under the list");
        scene.Viewport = new Size(800, 640);
        scene.Update();
        Assert.True(scene.GetBounds(detail).X > scene.GetBounds(list).Right);
    }

    [Fact]
    public void Many_devices_scroll_clip_and_hit_test_coherently()
    {
        for (var i = 0; i < 20; i++) Click(ById("add"));
        Assert.Equal(23, CardNames.Count);
        var list = ById("list");
        var scroll = scene.GetScrollState(list)!;
        Assert.True(scroll.CanScroll);
        Assert.True(tree.Find(ById("device-23"))!.IsOffscreen);

        var listBounds = scene.GetBounds(list);
        var probe = new Point(listBounds.X + 40, listBounds.Y + 30);
        input.PointerWheel(probe, -3, KeyModifiers.None);
        scene.Update();
        Assert.Equal(144, scroll.OffsetY);
        var hit = scene.HitTest(probe)!;
        var card = Cards.Single(c => IsWithin(hit, c));
        var cardBounds = scene.GetBounds(card);
        Assert.InRange(probe.Y, cardBounds.Y, cardBounds.Bottom);

        input.Focus(SelectButton("device-22"), visible: true);
        Key("Tab");
        Assert.Equal("device-23", input.Focused?.ParentElement?.Id);
        Assert.False(tree.Find(ById("device-23"))!.IsOffscreen);
    }

    [Fact]
    public void Tab_order_reaches_every_control_and_cards_are_named_groups()
    {
        var order = new List<string>();
        for (var i = 0; i < 9; i++)
        {
            Key("Tab");
            var focused = input.Focused!;
            order.Add(focused.Id ?? $"{focused.ParentElement?.Id}/button");
        }
        Assert.Equal(["filter-input", "active-only", "add", "reverse", "theme", "device-1/button", "device-2/button", "device-3/button", "filter-input"], order);

        var card = tree.Find(ById("device-2"))!;
        Assert.Equal((SemanticRole.Group, "Centralina laboratorio"), (card.Role, card.Name));
        Assert.Contains(card.Children, n => n.Role == SemanticRole.Button && n.Name == "Seleziona Centralina laboratorio");
        Assert.Equal("Filtra dispositivi", tree.Find(ById("filter-input"))!.Name);
    }

    private static bool IsWithin(ElementNode? element, ElementNode ancestor)
    {
        for (var current = element; current is not null; current = current.ParentElement)
            if (current == ancestor) return true;
        return false;
    }
}
