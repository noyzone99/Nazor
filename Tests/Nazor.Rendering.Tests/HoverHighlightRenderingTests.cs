using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class HoverGridPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<DataGrid<int>>(0);
        builder.AddComponentParameter(1, nameof(DataGrid<int>.Items), Enumerable.Range(0, 12).ToArray());
        builder.AddComponentParameter(2, nameof(DataGrid<int>.Style), "width: 230px; height: 260px");
        builder.AddComponentParameter(3, nameof(DataGrid<int>.RowHeight), 48d);
        builder.AddComponentParameter(4, nameof(DataGrid<int>.Striped), true);
        builder.AddComponentParameter(5, nameof(DataGrid<int>.ChildContent), (RenderFragment)(columns =>
        {
            Column(0, "fixed", true);
            Column(10, "normal", false);
            void Column(int sequence, string key, bool frozen)
            {
                columns.OpenComponent<DataGridColumn<int>>(sequence);
                columns.AddComponentParameter(sequence + 1, nameof(DataGridColumn<int>.Key), key);
                columns.AddComponentParameter(sequence + 2, nameof(DataGridColumn<int>.Header), key);
                columns.AddComponentParameter(sequence + 3, nameof(DataGridColumn<int>.Width), 140d);
                columns.AddComponentParameter(sequence + 4, nameof(DataGridColumn<int>.Frozen), frozen);
                columns.AddComponentParameter(sequence + 5, nameof(DataGridColumn<int>.CellTemplate), (RenderFragment<int>)(value => content => content.AddContent(0, value.ToString())));
                columns.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

public sealed class HoverHighlightRenderingTests
{
    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public void Striped_grid_hover_paints_ordinary_and_opaque_frozen_cells_keeps_the_header_clear_and_preserves_scroll_coordinates(ColorScheme scheme)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Styles.Preferences = new MediaPreferences(scheme, true);
        var root = h.Host.Render<HoverGridPage>();
        RenderedImage Frame() { h.Host.RunPending(); h.Render(400, 320, 1); h.Host.RunPending(); return h.Render(400, 320, 1); }
        var baseline = Frame();
        var row = root.Elements("DataGridRow").ElementAt(1);
        var cells = row.Elements().Where(element => element.Name == "DataGridCell").ToArray();
        var frozen = h.Scene.GetBounds(cells[0]);
        var normal = h.Scene.GetBounds(cells[1]);
        var frozenPixel = new Point(frozen.X + 90, frozen.Y + 40);
        var ordinaryPixel = new Point(normal.X + 45, normal.Y + 40);
        var header = h.Scene.GetBounds(root.Elements("DataGridColumnHeader").First());
        input.PointerMoved(ordinaryPixel, KeyModifiers.None);
        var hovered = Frame();
        Assert.NotEqual(baseline.GetPixel((int)ordinaryPixel.X, (int)ordinaryPixel.Y), hovered.GetPixel((int)ordinaryPixel.X, (int)ordinaryPixel.Y));
        Assert.NotEqual(baseline.GetPixel((int)frozenPixel.X, (int)frozenPixel.Y), hovered.GetPixel((int)frozenPixel.X, (int)frozenPixel.Y));
        Assert.Equal(baseline.GetPixel((int)header.X + 90, (int)header.Y + 28), hovered.GetPixel((int)header.X + 90, (int)header.Y + 28));
        var body = root.Single("DataGridBody");
        var indicator = root.Single("HoverIndicator");
        Assert.Equal(true, indicator.Attributes["semantic-hidden"]);
        Assert.Equal(PointerEvents.None, h.Scene.Styles.GetStyle(indicator).Get(StyleProperties.PointerEvents));
        Assert.True(input.ScrollBy(body, 0, 24));
        Frame();
        Assert.Same(indicator, root.Single("HoverIndicator"));
        Assert.Equal(true, body.Attributes["hover-active"]);
        var active = root.Elements("DataGridRow").Single(element => (element.States & ElementStates.Hover) != 0);
        Assert.InRange(Math.Abs(h.Scene.GetBounds(indicator).Y - h.Scene.GetBounds(active).Y), 0, .1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Menu_bar_entry_snaps_then_glides_and_exit_fades_with_reduced_motion_support(bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(new MenuServices(), clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced);
        var root = h.Host.Render<HoverMenuPage>();
        void Frame() { h.Host.RunPending(); h.Render(400, 200, 1); h.Host.RunPending(); h.Render(400, 200, 1); }
        void Hover(int index)
        {
            var bounds = h.Scene.GetBounds(root.Elements("MenuBarItem").ElementAt(index));
            input.PointerMoved(new(bounds.X + 20, bounds.Y + 10), KeyModifiers.None); Frame();
        }
        Frame(); Hover(0);
        var indicator = root.Single("HoverIndicator");
        var first = h.Scene.GetBounds(indicator).X;
        clock.Advance(250); Frame();
        Hover(1);
        var target = h.Scene.GetBounds(root.Elements("MenuBarItem").ElementAt(1)).X;
        clock.Advance(100); Frame();
        var middle = h.Scene.GetBounds(indicator).X;
        if (reduced) Assert.InRange(Math.Abs(middle - target), 0, .1);
        else Assert.InRange(middle, first + .1f, target - .1f);
        input.PointerMoved(new(390, 190), KeyModifiers.None); Frame();
        clock.Advance(220); Frame();
        var style = h.Scene.Styles.GetStyle(indicator);
        Assert.Equal(0, h.Scene.Transitions.GetValue(indicator, StyleProperties.Opacity, style.Opacity));
        Assert.Same(indicator, root.Single("HoverIndicator"));
    }

    private sealed class MenuServices : IServiceProvider
    {
        private readonly KeyBindings bindings = new();
        public object? GetService(Type serviceType) => serviceType == typeof(KeyBindings) ? bindings : null;
    }
}

public sealed class HoverMenuPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MenuBar>(0);
        builder.AddComponentParameter(1, nameof(MenuBar.ChildContent), (RenderFragment)(items =>
        {
            foreach (var label in new[] { "File", "Edit", "View" })
            {
                items.OpenComponent<MenuBarItem>(0);
                items.AddComponentParameter(1, nameof(MenuBarItem.ChildContent), (RenderFragment)(content => content.AddContent(0, label)));
                items.AddComponentParameter(2, nameof(MenuBarItem.Content), (RenderFragment)(content =>
                {
                    content.OpenComponent<MenuItem>(0);
                    content.AddComponentParameter(1, nameof(MenuItem.ChildContent), (RenderFragment)(text => text.AddContent(0, "Command")));
                    content.CloseComponent();
                }));
                items.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}
