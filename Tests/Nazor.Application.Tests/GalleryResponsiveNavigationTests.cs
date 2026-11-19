using Microsoft.Extensions.DependencyInjection;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GalleryResponsiveNavigationTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
    private readonly IServiceScope scope;
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly ManualAnimationClock clock = new();
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;

    public GalleryResponsiveNavigationTests()
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
    private ElementNode Strip => root.Elements().Single(element => element.Class == "demo-mnav");
    private ElementNode[] VisibleNavigation => root.Elements("Button").Where(element =>
        (element.Id?.StartsWith("leaf-nav-", StringComparison.Ordinal) is true || element.Id?.StartsWith("leaf-strip-", StringComparison.Ordinal) is true) &&
        scene.IsRendered(element)).ToArray();
    private void Key(string key) { input.KeyDown(key, KeyModifiers.None, false); input.KeyUp(key, KeyModifiers.None); Frame(); }

    [Fact]
    public async Task The_860px_breakpoint_shares_selection_and_leaves_one_visible_navigation_Tab_stop()
    {
        await host.ClickAsync(ById("leaf-nav-6"));
        Frame();
        Assert.Single(VisibleNavigation, element => element.Attributes.GetValueOrDefault("tabindex") is 0);
        Assert.All(VisibleNavigation, element => Assert.StartsWith("leaf-nav-", element.Id));
        scene.Viewport = new Size(860, 820);
        Frame();
        Assert.All(VisibleNavigation, element => Assert.StartsWith("leaf-strip-", element.Id));
        var activeStrip = Assert.Single(VisibleNavigation, element => element.Attributes.GetValueOrDefault("tabindex") is 0);
        var activeIndex = activeStrip.Id!["leaf-strip-".Length..];
        Assert.Contains("current", ById("leaf-nav-" + activeIndex).Class!);
        await host.ClickAsync(ById("leaf-strip-6"));
        Frame();
        activeStrip = ById("leaf-strip-6");
        Assert.Contains("current", ById("leaf-nav-6").Class!);
        var selected = scene.GetBounds(activeStrip);
        var viewport = scene.GetBounds(Strip);
        var scroller = scene.GetScrollState(Strip)!;
        Assert.True(scroller.OffsetX > 0, $"Selected {selected}; strip {viewport}; horizontal offset {scroller.OffsetX}/{scroller.MaxOffsetX}.");
        Assert.InRange(selected.X, viewport.X, viewport.Right - selected.Width);
        scene.Viewport = new Size(861, 820);
        Frame();
        var activeRail = Assert.Single(VisibleNavigation, element => element.Attributes.GetValueOrDefault("tabindex") is 0);
        Assert.Contains("current", ById("leaf-strip-" + activeRail.Id!["leaf-nav-".Length..]).Class!);
        Assert.All(VisibleNavigation, element => Assert.StartsWith("leaf-nav-", element.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Narrow_keyboard_navigation_reveals_the_shared_section_without_focusing_the_hidden_rail(bool reduced)
    {
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reduced);
        scene.Viewport = new Size(620, 760);
        Frame();
        input.Focus(ById("leaf-strip-0"), visible: true);
        Key("End");
        Assert.Same(ById("leaf-strip-10"), input.Focused);
        Assert.Contains("current", ById("leaf-nav-10").Class!);
        clock.Advance(700);
        Frame();
        var selected = scene.GetBounds(ById("leaf-strip-10"));
        var viewport = scene.GetBounds(Strip);
        Assert.InRange(selected.X, viewport.X, viewport.Right - selected.Width + .1f);
        Assert.True(scene.GetScrollState(Strip)!.OffsetX > 0);
        Key("ArrowLeft");
        Assert.Same(ById("leaf-strip-9"), input.Focused);
        Assert.Contains("current", ById("leaf-nav-9").Class!);
        Key("Home");
        clock.Advance(700);
        Frame();
        Assert.Same(ById("leaf-strip-0"), input.Focused);
        Assert.InRange(scene.GetScrollState(Strip)!.OffsetX, 0, .1f);
        Key("Tab");
        Assert.DoesNotContain(input.Focused, VisibleNavigation);
    }
}
