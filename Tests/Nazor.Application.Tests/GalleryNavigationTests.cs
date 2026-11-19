using Microsoft.Extensions.DependencyInjection;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Showcase;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GalleryNavigationPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "display: flex; width: 100%; height: 100%; min-height: 0");
        builder.OpenComponent<LeafShowcase>(10);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class GalleryNavigationTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
    private readonly IServiceScope scope;
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly ManualAnimationClock clock = new();
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;

    public GalleryNavigationTests()
    {
        scope = services.CreateScope();
        host = new TestHost(scope.ServiceProvider, captureExceptions: true);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images, clock: clock)
        {
            Viewport = new Size(1180, 820),
        };
        scene.Styles.Theme = Samples.Gallery.App.LeafTheme;
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        input = new InputManager(host.Renderer, scene);
        root = host.Render<GalleryNavigationPage>();
        Frame();
    }

    public void Dispose() { input.Dispose(); scene.Dispose(); host.Dispose(); scope.Dispose(); services.Dispose(); graphics.Dispose(); }

    private void Frame()
    {
        for (var frame = 0; frame < 4; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Assert.Empty(host.Exceptions);
    }

    private ElementNode ById(string id) => root.Elements().Single(element => element.Id == id);
    private ElementNode ByClass(string name) => root.Elements().Single(element => element.Class?.Split(' ').Contains(name) is true);
    private ElementNode Scroll => ById("leaf-sections");
    private ElementNode Current => root.Elements("Button").Single(element => element.Id?.StartsWith("leaf-nav-", StringComparison.Ordinal) is true && element.Class?.Split(' ').Contains("current") is true);

    private void ScrollToSection(int section)
    {
        var state = scene.GetScrollState(Scroll)!;
        var target = scene.GetBounds(ById($"leaf-section-{section}"));
        var viewport = scene.GetBounds(Scroll);
        Assert.True(scene.ScrollTo(Scroll, null, state.OffsetY + target.Y - viewport.Y, immediate: true));
        Frame();
    }

    [Fact]
    public void Manual_scrolling_updates_section_title_rail_and_compact_header_and_settles_without_render_loops()
    {
        Assert.Equal("leaf-nav-0", Current.Id);
        Assert.DoesNotContain("compact", ByClass("demo-page-head").Class!);
        ScrollToSection(3);
        Assert.Equal("leaf-nav-3", Current.Id);
        Assert.Equal("Menu", ByClass("demo-page-title").Text());
        Assert.Contains("compact", ByClass("demo-page-head").Class!);
        Assert.Equal(20, scene.Styles.GetStyle(ByClass("demo-page-title")).Get(StyleProperties.FontSize).Value);
        var indicator = root.Single("ShowcaseRailIndicator");
        var slot = ById("leaf-nav-3").Elements().Single(element => element.Class == "demo-slot");
        Assert.InRange(Math.Abs(scene.GetBounds(indicator).Y - scene.GetBounds(slot).Y), 0, .1);

        Assert.True(scene.ScrollTo(Scroll, null, 0, immediate: true));
        Frame();
        Assert.Equal("leaf-nav-0", Current.Id);
        Assert.DoesNotContain("compact", ByClass("demo-page-head").Class!);
        Assert.Equal(34, scene.Styles.GetStyle(ByClass("demo-page-title")).Get(StyleProperties.FontSize).Value);
        var settled = host.Batches.Count;
        Frame();
        Assert.Equal(settled, host.Batches.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rail_indicator_keeps_its_identity_travels_for_550ms_and_reduced_motion_finishes_immediately(bool reduced)
    {
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reduced);
        Frame();
        clock.Advance(1000);
        Frame();
        var indicator = root.Single("ShowcaseRailIndicator");
        var start = scene.GetBounds(indicator).Y;
        await host.ClickAsync(ById("leaf-nav-4"));
        Frame();
        Assert.Same(indicator, root.Single("ShowcaseRailIndicator"));
        Assert.Equal("leaf-nav-4", Current.Id);
        var slot = ById("leaf-nav-4").Elements().Single(element => element.Class == "demo-slot");
        var target = scene.GetBounds(slot).Y;
        Assert.True(target > start);
        clock.Advance(150);
        Frame();
        var during = scene.GetBounds(indicator).Y;
        if (reduced) Assert.InRange(Math.Abs(during - target), 0, .1);
        else Assert.InRange(during, start + .1f, target - .1f);
        clock.Advance(410);
        Frame();
        Assert.InRange(Math.Abs(scene.GetBounds(indicator).Y - scene.GetBounds(slot).Y), 0, .1);
        Assert.Same(indicator, root.Single("ShowcaseRailIndicator"));
        Assert.Equal("leaf-nav-4", Current.Id);
    }

    [Fact]
    public void Section_detection_uses_resized_geometry_and_the_final_section_can_be_selected_at_the_scroll_limit()
    {
        ScrollToSection(2);
        scene.Viewport = new Size(980, 650);
        Frame();
        ScrollToSection(2);
        Assert.Equal("leaf-nav-2", Current.Id);
        Assert.True(scene.ScrollTo(Scroll, null, scene.GetScrollState(Scroll)!.MaxOffsetY, immediate: true));
        Frame();
        Assert.Equal("leaf-nav-10", Current.Id);
        Assert.Equal("Marchio", ByClass("demo-page-title").Text());
    }
}
