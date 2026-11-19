using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FluentTabsPage : ComponentBase
{
    [Parameter] public TabsPresentation Presentation { get; set; } = TabsPresentation.Document;
    [Parameter] public TabWidthMode WidthMode { get; set; }
    [Parameter] public int Count { get; set; } = 3;
    [Parameter] public double Width { get; set; } = 600;
    public Tabs Strip { get; private set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Tabs>(0);
        builder.AddComponentParameter(1, nameof(Tabs.Presentation), Presentation);
        builder.AddComponentParameter(2, nameof(Tabs.WidthMode), WidthMode);
        builder.AddComponentParameter(3, nameof(Control.Style), FormattableString.Invariant($"width: {Width}px"));
        builder.AddComponentParameter(4, nameof(Tabs.ShowAddButton), true);
        builder.AddComponentParameter(5, nameof(Tabs.OnAdd), EventCallback.Factory.Create(this, () => { }));
        builder.AddComponentParameter(6, nameof(Tabs.ChildContent), (RenderFragment)(content =>
        {
            for (var index = 0; index < Count; index++)
            {
                content.OpenComponent<Tab>(0);
                content.AddComponentParameter(1, nameof(Tab.Label), index == 1 ? "A much longer title" : "Page " + index);
                content.AddComponentParameter(2, nameof(Tab.Key), "page-" + index);
                content.AddComponentParameter(3, nameof(Tab.Closable), true);
                content.AddComponentParameter(4, nameof(Tab.OnClose), EventCallback.Factory.Create(this, () => { }));
                content.AddComponentParameter(5, nameof(Tab.Leading), (RenderFragment)(mark => mark.AddContent(0, "D")));
                content.AddComponentParameter(6, nameof(Tab.ChildContent), (RenderFragment)(panel => panel.AddContent(0, "Document body")));
                content.CloseComponent();
            }
        }));
        builder.AddComponentReferenceCapture(7, component => Strip = (Tabs)component);
        builder.CloseComponent();
    }
}

public sealed class FluentTabsTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness h;
    private readonly InputManager input;
    private RootComponent root = null!;
    public FluentTabsTests()
    {
        h = new RenderHarness(clock: clock);
        h.Scene.Styles.Theme = FluentThemes.Fluent;
        h.Scene.Viewport = new Size(900, 500);
        input = new InputManager(h.Host.Renderer, h.Scene);
        foreach (var file in FluentThemes.FontFiles) ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
    }
    public void Dispose() { input.Dispose(); h.Dispose(); }
    private ElementNode[] Tabs => root.Elements("Tab").ToArray();
    private void Frame()
    {
        for (var pass = 0; pass < 5; pass++) { h.Host.RunPending(); h.Scene.Update(); h.Scene.BuildDisplayList(); }
    }
    private void Show(TabsPresentation presentation = TabsPresentation.Document, TabWidthMode widthMode = TabWidthMode.Equal,
        double width = 600, int count = 3, ColorScheme scheme = ColorScheme.Light, bool reduced = true, bool forced = false)
    {
        h.Scene.Styles.Preferences = new MediaPreferences(scheme, reduced, ForcedColors: forced);
        h.Scene.Transitions.ReducedMotion = reduced;
        root = h.Host.Render<FluentTabsPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(FluentTabsPage.Presentation)] = presentation, [nameof(FluentTabsPage.WidthMode)] = widthMode,
            [nameof(FluentTabsPage.Width)] = width, [nameof(FluentTabsPage.Count)] = count,
        }));
        Frame();
    }

    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public void Document_sheet_geometry_and_real_add_close_glyphs_follow_fluent_resources(ColorScheme scheme)
    {
        Show(scheme: scheme);
        var selected = Tabs[0];
        Assert.InRange(h.Scene.GetBounds(root.Single("TabsHeader")).Height, 39.9, 40.1);
        Assert.All(Tabs, tab => Assert.InRange(h.Scene.GetBounds(tab).Height, 31.9, 32.1));
        Assert.Equal(Length.Px(8), h.Scene.GetStyle(selected).Get(StyleProperties.BorderTopLeftRadius).X);
        var leading = selected.Elements().Single(element => element.Name == "TabLeading");
        Assert.InRange(h.Scene.GetBounds(leading).Width, 15.9, 16.1);
        Assert.InRange(h.Scene.GetBounds(leading).Height, 15.9, 16.1);
        Assert.InRange(h.Scene.GetBounds(root.Single("TabsAdd")).Width, 31.9, 32.1);
        var close = selected.Elements().Single(element => element.Name == "TabClose");
        Assert.InRange(h.Scene.GetBounds(close).Width, 31.9, 32.1);
        Assert.InRange(h.Scene.GetBounds(close).Height, 23.9, 24.1);
        Assert.Equal(Display.None, h.Scene.GetStyle(root.Single("TabsIndicator")).Display);
        Assert.Equal(h.Scene.GetStyle(root.Single("TabsPanel")).BackgroundColor, h.Scene.GetStyle(selected).BackgroundColor);
        Assert.NotEqual(h.Scene.GetStyle(selected).BackgroundColor, h.Scene.GetStyle(Tabs[1]).BackgroundColor);
        AssertGlyph(root.Single("TabsAdd"), "\uF107");
        AssertGlyph(close, "\uF367");
        var wing = h.Scene.Styles.GetGeneratedElement(selected, PseudoElement.Before)!;
        Assert.Equal(SizeValue.Fixed(Length.Px(4)), h.Scene.GetStyle(wing).Get(StyleProperties.Width));

        void AssertGlyph(ElementNode element, string glyph)
        {
            var generated = h.Scene.Styles.GetGeneratedElement(element, PseudoElement.Before)!;
            Assert.Equal(glyph, generated.Text());
            Assert.Equal(new[] { "FluentSystemIcons-Regular" }, h.Scene.GetStyle(generated).Get(StyleProperties.FontFamily).Items);
            Assert.True(h.Scene.GetBounds(generated).Width > 0);
        }
    }

    [Theory]
    [InlineData(900, 3, 240)]
    [InlineData(400, 5, 100)]
    public void Equal_document_headers_respect_shared_width_limits_and_keep_add_visible(double width, int count, double expected)
    {
        Show(width: width, count: count);
        Assert.All(Tabs, tab => Assert.InRange(h.Scene.GetBounds(tab).Width, expected - .5, expected + .5));
        var add = h.Scene.GetBounds(root.Single("TabsAdd"));
        Assert.True(add.Right <= width + .5);
        Assert.True(add.Width > 0);
    }

    [Fact]
    public void Compact_hides_only_unselected_visual_labels_and_preserves_document_actions()
    {
        Show(widthMode: TabWidthMode.Compact);
        var selected = Tabs[0];
        var other = Tabs[1];
        Assert.NotEqual(Display.None, h.Scene.GetStyle(selected.Elements().Single(element => element.Name == "TabLabel")).Display);
        Assert.Equal(Display.None, h.Scene.GetStyle(other.Elements().Single(element => element.Name == "TabLabel")).Display);
        Assert.True(h.Scene.GetBounds(other).Width < h.Scene.GetBounds(selected).Width);
        Assert.Equal("A much longer title", other.Attributes["accessible-name"]);
        Assert.True(h.Scene.GetBounds(other.Elements().Single(element => element.Name == "TabClose")).Width > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Navigation_indicator_moves_from_the_previous_position_and_respects_reduced_motion(bool reduced)
    {
        Show(TabsPresentation.Navigation, reduced: reduced);
        var indicator = root.Single("TabsIndicator");
        var start = h.Scene.GetTransform(indicator).OffsetX;
        Assert.InRange(h.Scene.GetBounds(indicator).Height, 2.9, 3.1);
        Assert.Equal(0, h.Scene.GetStyle(Tabs[0]).BackgroundColor.A);
        await ((FluentTabsPage)root.Component).Strip.SelectAsync(1); Frame();
        var immediate = h.Scene.GetTransform(indicator).OffsetX;
        clock.Advance(60); Frame();
        var middle = h.Scene.GetTransform(indicator).OffsetX;
        clock.Advance(300); Frame();
        var end = h.Scene.GetTransform(indicator).OffsetX;
        Assert.True(end > start);
        if (reduced) { Assert.Equal(end, immediate); Assert.Equal(end, middle); }
        else { Assert.Equal(start, immediate); Assert.InRange(middle, start + .1, end - .1); }
    }

    [Fact]
    public void Forced_colors_keep_selected_sheet_boundaries_visible()
    {
        Show(forced: true);
        Assert.Equal(Length.Px(1), h.Scene.GetStyle(Tabs[0]).Get(StyleProperties.BorderTopWidth));
        Assert.Equal(Display.None, h.Scene.GetStyle(root.Single("TabsIndicator")).Display);
        Assert.NotEqual(h.Scene.GetStyle(Tabs[0]).BackgroundColor, h.Scene.GetStyle(Tabs[0]).Get(StyleProperties.BorderTopColor));
    }
}
