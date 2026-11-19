using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using Color = Nazor.Style.Color;

namespace Nazor.Rendering.Tests;

public sealed class CollectionMotionPage : ComponentBase
{
    [Parameter] public string Kind { get; set; } = "list";
    public string[] Items { get; set; } = ["One", "Two", "Three", "Four", "Five"];
    public bool Disabled { get; set; }
    public Control View { get; private set; } = null!;
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var type = Kind switch { "grid" => typeof(GridView<string>), "tree" => typeof(TreeView<string>), _ => typeof(ListView<string>) };
        builder.OpenComponent(0, type);
        builder.AddComponentParameter(1, Kind == "tree" ? "Roots" : "Items", Items);
        builder.AddComponentParameter(2, "ItemText", (Func<string, string>)(item => item));
        builder.AddComponentParameter(3, "SelectedItem", "Two");
        builder.AddComponentParameter(4, "Style", "width: 320px; height: 260px");
        builder.AddComponentParameter(5, "Disabled", Disabled);
        builder.AddComponentParameter(6, "ShowCheckBoxes", true);
        if (Kind == "tree") builder.AddComponentParameter(7, "Children", (Func<string, IReadOnlyList<string>?>)(item => item.EndsWith(" child", StringComparison.Ordinal) ? [] : [item + " child"]));
        else builder.AddComponentParameter(8, "ItemKey", (Func<string, object>)(item => item));
        if (Kind == "grid")
        {
            builder.AddComponentParameter(9, "ItemWidth", 100d);
            builder.AddComponentParameter(10, "ItemHeight", 70d);
        }
        builder.AddComponentParameter(11, "ItemTemplate", (RenderFragment<string>)(item => content => content.AddContent(0, item)));
        builder.AddComponentReferenceCapture(12, component => View = (Control)component);
        builder.CloseComponent();
    }
}

public sealed class CollectionMotionTests
{
    [Theory]
    [InlineData("basic", 13)]
    [InlineData("fluent", 20)]
    [InlineData("leaf", 18)]
    public async Task Tree_check_artwork_and_stable_chevron_follow_theme_geometry_and_expansion(string theme, int checkSize)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = theme switch { "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, false);
        h.Scene.Transitions.ReducedMotion = false;
        var root = h.Host.Render<CollectionMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(CollectionMotionPage.Kind)] = "tree" }));
        void Frame() { for (var pass = 0; pass < 3; pass++) { h.Host.RunPending(); h.Render(450, 350, 1.5f); } }
        Frame(); clock.Advance(600); Frame();
        var row = root.Elements("TreeViewItem").First();
        var chevron = row.Elements().Single(element => element.Name == "TreeViewExpander");
        var check = row.Elements().Single(element => element.Name == "TreeViewItemCheck");
        Assert.InRange(Math.Abs(h.Scene.GetBounds(check).Width - checkSize), 0, .7);
        var start = h.Scene.GetTransform(chevron);
        await ((TreeView<string>)((CollectionMotionPage)root.Component).View).SetExpandedAsync("One", true); Frame();
        clock.Advance(30); Frame();
        var middle = h.Scene.GetTransform(chevron);
        clock.Advance(600); Frame();
        var end = h.Scene.GetTransform(chevron);
        Assert.Same(chevron, row.Elements().Single(element => element.Name == "TreeViewExpander"));
        Assert.Equal("chevron-right", chevron.Attributes["glyph"]);
        Assert.NotEqual(start, end);
        if (theme != "basic") { Assert.NotEqual(start, middle); Assert.NotEqual(end, middle); }
    }

    [Theory]
    [InlineData("fluent", "list", false)]
    [InlineData("fluent", "grid", false)]
    [InlineData("fluent", "tree", false)]
    [InlineData("leaf", "list", false)]
    [InlineData("leaf", "grid", false)]
    [InlineData("leaf", "tree", false)]
    [InlineData("fluent", "list", true)]
    [InlineData("leaf", "tree", true)]
    public void Retained_rows_relocate_from_current_geometry_and_remain_bounded_on_insertion_removal_and_reversal(string theme, string kind, bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = theme == "fluent" ? Nazor.Themes.Fluent.Themes.Fluent : NazorLeafTheme.Theme;
        h.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, reduced);
        h.Scene.Transitions.ReducedMotion = reduced;
        var root = h.Host.Render<CollectionMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(CollectionMotionPage.Kind)] = kind }));
        var page = (CollectionMotionPage)root.Component;
        var rowName = kind switch { "grid" => "GridViewItem", "tree" => "TreeViewItem", _ => "ListViewItem" };
        void Frame() { for (var pass = 0; pass < 3; pass++) { h.Host.RunPending(); h.Render(450, 350, 1); } }
        ElementNode Two() => root.Elements(rowName).Single(row => row.Attributes.GetValueOrDefault("accessible-name") as string == "Two");
        Frame(); clock.Advance(600); Frame();
        var retained = Two();
        var before = h.Scene.GetBounds(retained);
        page.Items = ["One", "Three", "Four", "Two", "Five"]; page.Refresh(); Frame();
        var start = h.Scene.GetBounds(Two());
        Assert.Same(retained, Two());
        if (!reduced) Assert.True(Distance(before, start) < .2);
        clock.Advance(30); Frame();
        var middle = h.Scene.GetBounds(Two());
        clock.Advance(600); Frame();
        var end = h.Scene.GetBounds(Two());
        Assert.True(Distance(before, end) > 10);
        if (!reduced) { Assert.True(Distance(before, middle) > .1); Assert.True(Distance(middle, end) > .1); }
        else Assert.True(Distance(start, end) < .2);
        page.Items = ["New", "One", "Three", "Four", "Two", "Five"]; page.Refresh(); Frame();
        clock.Advance(25); Frame();
        var interrupted = h.Scene.GetBounds(Two());
        page.Items = ["One", "Two", "Three"]; page.Refresh(); Frame();
        if (!reduced) Assert.True(Distance(interrupted, h.Scene.GetBounds(Two())) < .2);
        clock.Advance(600); Frame();
        Assert.Same(retained, Two());
        Assert.Equal(3, root.Elements(rowName).Count());
        Assert.True(Two().Attributes.GetValueOrDefault("selected") is true);
    }

    [Theory]
    [InlineData("list", ColorScheme.Light, false)]
    [InlineData("list", ColorScheme.Dark, false)]
    [InlineData("tree", ColorScheme.Light, false)]
    [InlineData("tree", ColorScheme.Dark, false)]
    [InlineData("grid", ColorScheme.Light, false)]
    [InlineData("grid", ColorScheme.Dark, false)]
    [InlineData("list", ColorScheme.Dark, true)]
    [InlineData("tree", ColorScheme.Dark, true)]
    [InlineData("grid", ColorScheme.Dark, true)]
    public void Fluent_selected_hover_pressed_disabled_and_focus_states_use_neutral_backplates_and_distinct_accessible_focus(string kind, ColorScheme scheme, bool forced)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
        var colors = SystemColors.Default with { Highlight = Color.FromRgb(0xd6, 0xb4, 0xfd) };
        h.Scene.Styles.Preferences = new MediaPreferences(scheme, true, forced, Colors: colors);
        var root = h.Host.Render<CollectionMotionPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(CollectionMotionPage.Kind)] = kind }));
        var rowName = kind switch { "grid" => "GridViewItem", "tree" => "TreeViewItem", _ => "ListViewItem" };
        void Frame() { h.Host.RunPending(); h.Render(450, 350, 1); h.Host.RunPending(); h.Render(450, 350, 1); }
        Frame();
        var row = root.Elements(rowName).Single(item => item.Attributes.GetValueOrDefault("selected") is true);
        var normal = h.Scene.GetStyle(row).BackgroundColor;
        Assert.NotEqual(Color.Transparent, normal);
        var bounds = h.Scene.GetBounds(row);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        input.PointerMoved(point, KeyModifiers.None); Frame();
        var hover = h.Scene.GetStyle(row).BackgroundColor;
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None); Frame();
        var pressed = h.Scene.GetStyle(row).BackgroundColor;
        if (forced)
        {
            Assert.Equal(colors.Highlight, normal);
            Assert.Equal(colors.Highlight, hover);
            Assert.Equal(colors.Highlight, pressed);
        }
        else Assert.NotEqual(hover, pressed);
        input.PointerCanceled(); Frame();
        input.KeyDown("Tab", KeyModifiers.None, false); Frame();
        var focused = root.Elements(rowName).First(item => item.States.HasFlag(ElementStates.Focus));
        Assert.True(h.Scene.GetStyle(focused).Get(StyleProperties.OutlineWidth).Value >= 2);
        ((CollectionMotionPage)root.Component).Disabled = true;
        ((CollectionMotionPage)root.Component).Refresh(); Frame();
        Assert.Equal(Color.Transparent, h.Scene.GetStyle(row).BackgroundColor);
        Assert.DoesNotContain(root.Elements(rowName), item => item.States.HasFlag(ElementStates.Focus));
    }

    private static double Distance(Rect a, Rect b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
