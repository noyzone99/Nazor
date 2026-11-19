using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;
using Nazor.Samples.Gallery.Examples.Feedback;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class BadgeSemanticPage : ComponentBase
{
    public bool Custom { get; set; }
    public int? Count { get; set; }
    public string? Name { get; set; } = "Build paused";
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Badge>(0);
        builder.AddComponentParameter(1, nameof(Badge.Count), Count);
        builder.AddComponentParameter(2, nameof(Badge.Icon), new FontSymbol("Fira Sans", "!"));
        builder.AddComponentParameter(3, nameof(Badge.AccessibleName), Name);
        if (Custom) builder.AddComponentParameter(4, nameof(Badge.IndicatorContent), (RenderFragment)(content =>
        {
            content.OpenComponent<Button>(0);
            content.AddComponentParameter(1, nameof(Button.AccessibleName), "Hidden action");
            content.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Do not announce")));
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class BadgeSemanticTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false, DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree semantics;
    public BadgeSemanticTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(400, 300) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene); semantics = new SemanticTree(host.Renderer, scene, input);
    }
    public void Dispose() { semantics.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }
    private void Frame() { host.RunPending(); scene.Update(); host.RunPending(); scene.Update(); semantics.Update(); }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Icon_and_custom_badges_expose_one_named_owner_and_no_visual_child_controls(bool custom)
    {
        var root = host.Render<BadgeSemanticPage>(); var page = (BadgeSemanticPage)root.Component; page.Custom = custom; page.Refresh(); Frame();
        Assert.Equal(SemanticRole.Window, semantics.Root.Role);
        var owner = Assert.Single(semantics.Root.Children);
        Assert.Equal(SemanticRole.Text, owner.Role); Assert.Equal("Build paused", owner.Name);
        Assert.Empty(owner.Children);
        Assert.DoesNotContain(semantics.Descendants(), node => node.Name is "Hidden action" or "Do not announce");
    }

    [Fact]
    public void Count_accessibility_reports_the_current_uncapped_value_once_without_intermediate_visual_values()
    {
        var root = host.Render<BadgeSemanticPage>(); var page = (BadgeSemanticPage)root.Component;
        page.Name = null; page.Count = 150; page.Refresh(); Frame();
        Assert.Equal("99+", root.Single("BadgeValue").Text());
        Assert.Equal("150 new", Assert.Single(semantics.Root.Children).Name);
        page.Count = 151; page.Refresh(); Frame();
        Assert.Equal("151 new", Assert.Single(semantics.Root.Children).Name);
        page.Count = 0; page.Refresh(); Frame(); Assert.Empty(semantics.Root.Children);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Gallery_examples_use_the_typed_icon_and_custom_content_contracts_with_named_indicators(string theme)
    {
        ((SkiaFontProvider)graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, "Fonts", "NazorLeafIcons.otf"));
        scene.Styles.Theme = theme switch { "leaf" => NazorLeafTheme.Theme, "fluent" => Nazor.Themes.Fluent.Themes.Fluent, _ => Nazor.Themes.Themes.Basic };
        var root = host.Render<AvatarsBadgesSample>(); Frame();
        var iconBadge = root.Elements().Single(element => element.Id == "sample-icon-badge");
        Assert.Equal(LeafSymbol.At.Glyph, iconBadge.Elements().Single(element => element.Name == "Icon").Text());
        Assert.Equal("Mentions", Assert.Single(semantics.Descendants(), node => node.Name == "Mentions").Name);
        var custom = root.Elements().Single(element => element.Id == "sample-custom-badge");
        Assert.True(custom.Elements().Single(element => element.Name == "BadgeIndicatorContent").Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.Equal("Synchronization needs attention", Assert.Single(semantics.Descendants(), node => node.Name == "Synchronization needs attention").Name);
    }
}
