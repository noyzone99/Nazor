using Nazor.Animation;
using Nazor.Controls;
using Nazor.Input;
using Nazor.Graphics;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class NavigationViewRenderingPage : ComponentBase
{
    [Parameter] public bool Top { get; set; }
    public string Selected { get; private set; } = "Home";
    public void Select(string item) { Selected = item; StateHasChanged(); }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<NavigationView<string>>(0);
        builder.AddComponentParameter(1, nameof(NavigationView<string>.Items), new[] { "Home", "Projects", "Settings" });
        builder.AddComponentParameter(2, nameof(NavigationView<string>.SelectedItem), Selected);
        builder.AddComponentParameter(3, nameof(NavigationView<string>.PaneDisplayMode), Top ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Left);
        builder.AddComponentParameter(4, nameof(Control.Style), "width: 600px; height: 400px");
        builder.AddComponentParameter(5, nameof(NavigationView<string>.Header), (RenderFragment)(content => content.AddContent(0, "Workspace")));
        builder.AddComponentParameter(6, nameof(NavigationView<string>.ChildContent), (RenderFragment)(content => content.AddContent(0, "Persistent route content")));
        builder.AddComponentParameter(7, nameof(NavigationView<string>.Children), (Func<string, IReadOnlyList<string>?>)(item => item == "Projects" ? new[] { "Active", "Archive" } : null));
        builder.CloseComponent();
    }
}

public sealed class NavigationViewRenderingTests
{
    [Theory]
    [InlineData("basic", false)]
    [InlineData("basic", true)]
    [InlineData("fluent", false)]
    [InlineData("fluent", true)]
    [InlineData("leaf", false)]
    [InlineData("leaf", true)]
    public void Selection_marker_has_real_geometry_and_retargets_without_duplicate_selected_markers(string theme, bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        SetTheme(h, theme, reduced);
        var root = h.Host.Render<NavigationViewRenderingPage>();
        void Frame() { for (var pass = 0; pass < 6; pass++) { h.Host.RunPending(); h.Render(800, 500, 1); } }
        Frame();
        var marker = root.Single("NavigationViewTreeIndicator");
        Assert.InRange(h.Scene.GetBounds(marker).Width, theme == "leaf" ? 4.9 : 2.9, theme == "leaf" ? 5.1 : 3.1);
        Assert.InRange(h.Scene.GetBounds(marker).Height, theme == "leaf" ? 11.9 : 15.9, theme == "leaf" ? 12.1 : 16.1);
        var selected = root.Elements("TreeViewItem").Single(node => node.Attributes.GetValueOrDefault("selected") is true);
        var staticMarker = h.Scene.Styles.GetGeneratedElement(selected, PseudoElement.Before);
        Assert.True(staticMarker is null || h.Scene.GetStyle(staticMarker).Display == Display.None || staticMarker.Text().Length == 0);
        var start = h.Scene.GetTransform(marker).OffsetY;
        ((NavigationViewRenderingPage)root.Component).Select("Settings"); Frame();
        var immediate = h.Scene.GetTransform(marker).OffsetY;
        clock.Advance(60); Frame();
        var middle = h.Scene.GetTransform(marker).OffsetY;
        clock.Advance(500); Frame();
        var end = h.Scene.GetTransform(marker).OffsetY;
        Assert.True(end > start);
        if (reduced || theme == "basic") { Assert.Equal(end, immediate); Assert.Equal(end, middle); }
        else { Assert.Equal(start, immediate); Assert.InRange(middle, start + .1, end - .1); }
        var row = root.Elements("TreeViewItem").Single(node => node.Attributes.GetValueOrDefault("selected") is true);
        var rowBounds = h.Scene.GetBounds(row); var markerBounds = h.Scene.GetBounds(marker);
        var centerDifference = markerBounds.Y + markerBounds.Height / 2 - rowBounds.Y - rowBounds.Height / 2;
        var target = Nazor.Presentation.Scene.Compose(h.Scene.GetStyle(marker).Get(StyleProperties.Transform), h.Scene.GetLayoutNode(marker)!.RoundedFrame);
        Assert.True(Math.Abs(centerDifference) <= .2, $"Marker {markerBounds}, row {rowBounds}, visualY {end}, targetY {target.OffsetY}, start {start}, middle {middle}, running {h.Scene.Transitions.Find(marker, StyleProperties.Transform)?.End}.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fluent_uses_source_backed_pane_header_row_icon_and_selection_dimensions(bool top)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        SetTheme(h, "fluent", true);
        var root = h.Host.Render<NavigationViewRenderingPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(NavigationViewRenderingPage.Top)] = top }));
        for (var pass = 0; pass < 8; pass++) { h.Host.RunPending(); h.Render(800, 500, 1); }
        var header = h.Scene.GetBounds(root.Single("NavigationViewHeader"));
        Assert.InRange(header.Height, 51.9, 52.1);
        var icon = root.Elements("NavigationViewItemIcon").First(node => h.Scene.GetBounds(node).Width > 0);
        Assert.InRange(h.Scene.GetBounds(icon).Width, 15.9, 16.1);
        if (top)
        {
            Assert.InRange(h.Scene.GetBounds(root.Single("SplitViewPane")).Height, 47.9, 48.1);
            Assert.InRange(header.Y, 47.9, 48.1);
            var marker = root.Single("NavigationViewRootIndicator");
            Assert.InRange(h.Scene.GetBounds(marker).Width, 15.9, 16.1);
            Assert.True(Math.Abs(h.Scene.GetBounds(marker).Height - 3) <= .1,
                $"Marker {h.Scene.GetBounds(marker)}, layout {h.Scene.GetLayoutNode(marker)!.RoundedFrame}, raw {h.Scene.GetLayoutNode(marker)!.AbsoluteFrame}, height {h.Scene.GetStyle(marker).Get(StyleProperties.Height)}, matrix {h.Scene.GetTransform(marker)}.");
        }
        else
        {
            Assert.InRange(h.Scene.GetBounds(root.Single("SplitViewPane")).Width, 319.9, 320.1);
            var rows = root.Elements("TreeViewItem").ToArray();
            Assert.InRange(h.Scene.GetBounds(rows[1]).Y - h.Scene.GetBounds(rows[0]).Y, 39.9, 40.1);
            var toggle = root.Single("NavigationViewToggle");
            Assert.InRange(h.Scene.GetBounds(toggle).Width, 39.9, 40.1);
            Assert.InRange(h.Scene.GetBounds(toggle).Height, 35.9, 36.1);
            var glyph = h.Scene.Styles.GetGeneratedElement(toggle, PseudoElement.Before)!;
            Assert.Equal("\uF560", glyph.Text());
            var alternatives = h.Scene.GetStyle(glyph).Get(StyleProperties.FontSymbol);
            Assert.Equal(3, alternatives.Items.Count);
            Assert.Equal("Segoe Fluent Icons", alternatives.Items[0].FontFamily);
            Assert.Equal("\uE700", alternatives.Items[0].Glyph);
            var drawing = Assert.Single(h.Scene.BuildDisplayList().Commands.OfType<DrawText>(), command => command.Layout.Text == "\uF560");
            Assert.All(drawing.Layout.Shaped.Runs, run => Assert.Equal("FluentSystemIcons-Regular", run.Face.FamilyName));
        }
    }

    private static void SetTheme(RenderHarness h, string theme, bool reduced)
    {
        h.Scene.Styles.Theme = theme switch { "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced);
        h.Scene.Transitions.ReducedMotion = reduced;
        if (theme == "fluent") foreach (var file in Nazor.Themes.Fluent.Themes.FontFiles) h.Graphics.FontProvider.Register(Path.Combine(AppContext.BaseDirectory, file));
    }

    [Theory]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Hierarchy_expansion_and_collapse_keep_the_selected_marker_at_the_final_row_center(string theme)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        SetTheme(h, theme, false);
        var root = h.Host.Render<NavigationViewRenderingPage>();
        void Frame() { for (var pass = 0; pass < 6; pass++) { h.Host.RunPending(); h.Render(800, 500, 1); } }
        Frame(); ((NavigationViewRenderingPage)root.Component).Select("Settings"); Frame(); clock.Advance(500); Frame();
        var projects = root.Elements("TreeViewItem").Single(node => node.Attributes.GetValueOrDefault("accessible-name") is "Projects");
        var expander = projects.Elements().Single(node => node.Name == "TreeViewExpander");
        for (var change = 0; change < 2; change++)
        {
            var bounds = h.Scene.GetBounds(expander); var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None); input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); Frame();
            clock.Advance(500); Frame();
            Assert.Equal(change == 0, projects.Attributes.GetValueOrDefault("expanded") is true);
            var marker = h.Scene.GetBounds(root.Single("NavigationViewTreeIndicator"));
            var row = h.Scene.GetBounds(root.Elements("TreeViewItem").Single(node => node.Attributes.GetValueOrDefault("selected") is true));
            Assert.InRange(marker.Y + marker.Height / 2 - row.Y - row.Height / 2, -.2, .2);
        }
    }
}
