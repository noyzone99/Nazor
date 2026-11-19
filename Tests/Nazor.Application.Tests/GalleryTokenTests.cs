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

public sealed class GalleryTokenPage : ComponentBase
{
    [Parameter] public Scene Scene { get; set; } = null!;
    public string Seed { get; set; } = "seed-blue";
    public string? Override { get; set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "class", Seed);
        builder.AddAttribute(2, "style", Override);
        builder.OpenComponent<CascadingValue<Scene>>(10);
        builder.AddComponentParameter(11, nameof(CascadingValue<Scene>.Name), "gallery-scene");
        builder.AddComponentParameter(12, nameof(CascadingValue<Scene>.Value), Scene);
        builder.AddComponentParameter(13, nameof(CascadingValue<Scene>.IsFixed), true);
        builder.AddComponentParameter(14, nameof(CascadingValue<Scene>.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<NavigationSection>(0);
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class GalleryTokenTests
{
    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public async Task Readouts_follow_all_seed_palettes_custom_tokens_and_live_shape_radii_without_render_loops(ColorScheme scheme)
    {
        using var host = new TestHost();
        using var graphics = new SkiaRenderer(new FontOptions { UseSystemFonts = true });
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(900, 800) };
        using var input = new InputManager(host.Renderer, scene);
        scene.Styles.Theme = Samples.Gallery.App.LeafTheme;
        scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true);
        scene.Transitions.ReducedMotion = true;
        var root = host.Render<GalleryTokenPage>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(GalleryTokenPage.Scene)] = scene }));
        var page = (GalleryTokenPage)root.Component;
        void Frame() { host.RunPending(); scene.Update(); host.RunPending(); scene.Update(); }
        string RecentBatches() => string.Join("; ", host.Batches.TakeLast(8).Select(batch =>
            $"renders={batch.RenderedComponentCount}, added=[{string.Join(",", batch.AddedElements.Select(element => element.Name).Distinct().Take(8))}], updated=[{string.Join(",", batch.UpdatedElements.Select(element => element.Name).Distinct().Take(8))}], children=[{string.Join(",", batch.ChildrenChanged.Select(element => element.Name).Distinct().Take(8))}]"));
        void Settle()
        {
            var quiet = 0;
            for (var frame = 0; frame < 12 && quiet < 3; frame++)
            {
                var before = host.Batches.Count;
                Frame();
                quiet = host.Batches.Count == before ? quiet + 1 : 0;
            }
            Assert.True(quiet == 3, "Measurement did not settle within 12 frames: " + RecentBatches());
        }
        string Text(string id) => root.Elements().Single(element => element.Id == id).Text();
        var dark = scheme == ColorScheme.Dark;
        foreach (var (seed, primary, onContainer) in new[]
        {
            ("seed-blue", dark ? "#A8C7FA" : "#0B57D0", dark ? "#D3E3FD" : "#041E49"),
            ("seed-green", dark ? "#6DD58C" : "#146C2E", dark ? "#C4EED0" : "#072711"),
            ("seed-red", dark ? "#FFB4AB" : "#C5221F", dark ? "#FFDAD6" : "#410E0B"),
            ("seed-yellow", dark ? "#FDD663" : "#F9AB00", dark ? "#FFE08A" : "#2A1E00"),
        })
        {
            page.Seed = seed;
            page.Refresh();
            Frame();
            Assert.Equal(primary, Text("token-primary"));
            Assert.Equal(onContainer, Text("token-on-pc"));
            Assert.Equal(12, root.Elements().Count(element => element.Id?.StartsWith("token-", StringComparison.Ordinal) is true));
        }
        page.Override = "--leaf-s1: #123abc; --leaf-r-ctl: 18px 3px 18px 3px; --leaf-r-ovl: 26px 4px 26px 4px; --leaf-r-pane: 36px 6px 36px 6px; --leaf-r-win: 46px 8px 46px 8px";
        page.Refresh();
        Frame();
        Assert.Equal("#123ABC", Text("token-s1"));
        await host.ClickAsync(root.Elements().Single(element => element.Attributes.GetValueOrDefault("role") is "tab" && element.Text() == "Forme"));
        Frame();
        Assert.Equal("18px / 3px", Text("radius-ctl"));
        Assert.Equal("26px / 4px", Text("radius-ovl"));
        Assert.Equal("36px / 6px", Text("radius-pane"));
        Assert.Equal("46px / 8px", Text("radius-win"));
        Settle();
        var stable = host.Batches.Count;
        for (var frame = 0; frame < 4; frame++) Frame();
        Assert.True(stable == host.Batches.Count, "Settled content rendered again: " + RecentBatches());
        host.Renderer.RemoveRootComponent(root);
        Frame();
        Assert.Empty(host.Renderer.RootComponents);
    }
}
