using Microsoft.Extensions.DependencyInjection;
using Nazor.Application.Tests.Fixtures;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class LeafShowcaseActionsTests : IDisposable
{
    private readonly GalleryShowcaseClock clock = new();
    private readonly ServiceProvider services = new ServiceCollection().AddSingleton<KeyBindings>().BuildServiceProvider();
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly Scene scene;
    private readonly InputManager input;
    private RootComponent root = null!;
    private LeafActionsProbe Probe => (LeafActionsProbe)root.Component;

    public LeafShowcaseActionsTests()
    {
        host = new TestHost(services, captureExceptions: true);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(1200, 1200) };
        scene.Styles.Theme = NazorLeafTheme.Theme;
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        scene.Transitions.ReducedMotion = true;
        input = new InputManager(host.Renderer, scene);
    }

    public void Dispose() { input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); services.Dispose(); }

    private void Show(bool data = false)
    {
        root = host.Render<LeafActionsProbe>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(LeafActionsProbe.Time)] = clock,
            [nameof(LeafActionsProbe.Data)] = data,
        }));
        Frame();
    }

    private void Frame()
    {
        for (var frame = 0; frame < 3; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Assert.Empty(host.Exceptions);
    }

    private ElementNode ById(string id) => root.Elements().Single(element => element.Id == id);
    private ElementNode Tile(int id) => ById($"showcase-tile-{id}");

    private void Activate(ElementNode element)
    {
        Assert.True(input.Activate(element));
        Frame();
    }

    private void Context(int id)
    {
        input.Focus(Tile(id), visible: true);
        input.ContextMenuRequested(KeyModifiers.None);
        Frame();
        Assert.True(Tile(id).Attributes.GetValueOrDefault("selected") is true);
    }

    private void Choose(string label) => Activate(root.Elements("MenuItem").Single(element =>
        !element.IsDisabled && element.Elements().Any(part => part.Name == "MenuItemLabel" && part.Text() == label)));

    private void Delete(int id) { Context(id); Choose("Elimina"); }

    [Fact]
    public void Tiles_select_from_keyboard_context_and_independent_undo_restores_original_order_without_duplicates()
    {
        Show();
        Activate(Tile(1));
        Assert.True(Tile(1).Attributes.GetValueOrDefault("selected") is true);
        Assert.False(Tile(0).Attributes.GetValueOrDefault("selected") is true);
        Delete(1);
        Delete(2);
        Assert.Equal(new[] { "showcase-tile-0", "showcase-tile-3" }, root.Elements("ShowcaseTile").Select(element => element.Id));
        var entry = Probe.Notifications.Visible.First();
        Assert.Contains("Copertina.png", entry.Options.Message);
        Assert.Equal(TimeSpan.FromMilliseconds(4200), entry.Options.Duration);
        Activate(root.Elements("Toast").First().Elements().Single(element => element.Name == "ToastAction"));
        Assert.Equal(new[] { "showcase-tile-0", "showcase-tile-1", "showcase-tile-3" }, root.Elements("ShowcaseTile").Select(element => element.Id));
        Activate(root.Elements("Toast").Single().Elements().Single(element => element.Name == "ToastAction"));
        Assert.Equal(new[] { "showcase-tile-0", "showcase-tile-1", "showcase-tile-2", "showcase-tile-3" }, root.Elements("ShowcaseTile").Select(element => element.Id));
        Assert.Empty(Probe.Notifications.Visible);
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Menu_history_undo_and_redo_use_the_same_tile_state_and_context_actions_use_the_opened_tile()
    {
        Show();
        Context(2);
        Choose("Apri");
        Assert.Equal("Apertura di Tema.json", Probe.Notifications.Visible.Single().Options.Message);
        Probe.Notifications.Clear();
        Frame();
        Delete(2);
        OpenBar("Modifica");
        Choose("Annulla");
        Assert.NotNull(Tile(2));
        OpenBar("Modifica");
        Choose("Ripeti");
        Assert.DoesNotContain(root.Elements("ShowcaseTile"), element => element.Id == "showcase-tile-2");
    }

    [Fact]
    public void Menu_commands_and_view_toggles_report_and_preserve_their_effects()
    {
        Show();
        OpenBar("File");
        Choose("Nuovo");
        Assert.Equal("Nuovo documento creato", Probe.Notifications.Visible.Single().Options.Message);
        OpenBar("Vista");
        Activate(root.Elements("MenuCheckItem").Single(element => element.Elements().Any(part => part.Name == "MenuItemLabel" && part.Text() == "Griglia")));
        Assert.Contains("Griglia disattivata", ById("showcase-view-settings").Text());
        OpenBar("Vista");
        Assert.False(root.Elements("MenuCheckItem").Single(element => element.Elements().Any(part => part.Name == "MenuItemLabel" && part.Text() == "Griglia")).Attributes.GetValueOrDefault("checked") is true);
    }

    private void OpenBar(string label) => Activate(root.Elements("MenuBarItem").Single(element =>
        element.Elements().Any(part => part.Name == "MenuBarItemLabel" && part.Text() == label)));

    [Fact]
    public void Reading_a_message_permanently_clears_its_unread_marker_after_selecting_another_message()
    {
        Show(data: true);
        Assert.Single(root.Elements(), element => element.Class == "demo-unread");
        var list = ById("showcase-messages");
        Activate(list.Elements().Single(element => element.Name == "ListViewItem" && element.Text().Contains("Marco Bellini", StringComparison.Ordinal)));
        Assert.DoesNotContain(root.Elements(), element => element.Class == "demo-unread");
        Activate(ById("showcase-messages").Elements().Single(element => element.Name == "ListViewItem" && element.Text().Contains("Giulia Ferri", StringComparison.Ordinal)));
        Assert.DoesNotContain(root.Elements(), element => element.Class == "demo-unread");
        Activate(ById("showcase-messages").Elements().Single(element => element.Name == "ListViewItem" && element.Text().Contains("Marco Bellini", StringComparison.Ordinal)));
        Assert.DoesNotContain(root.Elements(), element => element.Class == "demo-unread");
    }

    [Fact]
    public void Data_toolbar_settings_and_motion_row_have_observable_actions()
    {
        Show(data: true);
        Activate(ById("showcase-download-builds"));
        Assert.Equal("Download di 1 pacchetto avviato", Probe.Notifications.Visible.Last().Options.Message);
        Activate(ById("showcase-clear-builds"));
        Activate(ById("showcase-new-build"));
        Assert.Equal("Build avviata per tutte le piattaforme", Probe.Notifications.Visible.Last().Options.Message);
        Activate(ById("showcase-seed-settings"));
        Assert.Contains("Verde", Probe.Notifications.Visible.Last().Options.Message);
        Activate(ById("showcase-language-settings"));
        Assert.Contains("lingua e area geografica", Probe.Notifications.Visible.Last().Options.Message);
        Activate(root.Elements("ActionRow").Single(element => element.Elements().Any(part => part.Name == "ActionRowTitle" && part.Text() == "Animazioni")));
        Assert.False(Probe.Motion);
        Assert.Equal(1, Probe.MotionChanges);
    }
}
