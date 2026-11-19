using Microsoft.Extensions.DependencyInjection;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery.Examples.Navigation;
using Nazor.Samples.Gallery.Examples.Primitives;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class GalleryMotionPrimitiveSampleTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddSingleton<KeyBindings>().BuildServiceProvider();
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly ManualAnimationClock clock = new();
    private readonly Scene scene;
    private readonly InputManager input;
    private RootComponent root = null!;

    public GalleryMotionPrimitiveSampleTests()
    {
        host = new TestHost(services, captureExceptions: true);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images, clock: clock) { Viewport = new Size(1200, 1200) };
        input = new InputManager(host.Renderer, scene);
    }

    public void Dispose() { input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); services.Dispose(); }

    private void Show<T>(string theme, bool reduced = true) where T : IComponent
    {
        scene.Styles.Theme = theme switch { "fluent" => Nazor.Themes.Fluent.Themes.Fluent, "leaf" => NazorLeafTheme.Theme, _ => Nazor.Themes.Themes.Basic };
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reduced);
        scene.Transitions.ReducedMotion = reduced;
        root = host.Render<T>();
        Frame();
    }

    private void Frame()
    {
        for (var frame = 0; frame < 6; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Assert.Empty(host.Exceptions);
    }

    private ElementNode ById(string id) => root.Elements().Single(node => node.Id == id);
    private ElementNode Button(string text) => root.Elements("Button").Single(node => node.Text() == text);
    private void Click(string text) { Assert.True(input.Activate(Button(text))); Frame(); }
    private void Press(string key, KeyModifiers modifiers) { input.KeyDown(key, modifiers, false); input.KeyUp(key, modifiers); Frame(); }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public async Task Document_tab_sample_reorders_actual_keys_preserving_edited_state_then_closes_and_adds(string theme)
    {
        Show<TabsSample>(theme);
        var strip = ById("sample-document-tabs");
        string[] Labels() => strip.Elements().Where(node => node.Name == "TabLabel").Select(node => node.Text()).ToArray();
        var notes = strip.Elements().Single(node => node.Name == "Tab" && node.Elements().Any(part => part.Name == "TabLabel" && part.Text() == "Notes"));
        var editor = ById("notes-editor-input");
        await host.Renderer.DispatchEventAsync(editor, "oninput", new ChangeEventArgs { Value = "Keep this draft" });
        Frame();
        Assert.Equal(new[] { "Notes", "Schedule", "Budget" }, Labels());

        input.Focus(notes, visible: true);
        Press("ArrowRight", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal(new[] { "Schedule", "Notes", "Budget" }, Labels());
        Assert.Same(notes, strip.Elements().Single(node => node.Name == "Tab" && node.Attributes.GetValueOrDefault("selected") is true));
        Assert.Same(editor, ById("notes-editor-input"));
        Assert.Equal("Keep this draft", editor.Attributes["value"]);

        Press("F4", KeyModifiers.Control);
        Assert.Equal(new[] { "Schedule", "Budget" }, Labels());
        Assert.DoesNotContain(root.Elements(), node => node.Id == "notes-editor-input");
        Press("t", KeyModifiers.Control);
        Assert.Equal(new[] { "Schedule", "Budget", "Document 1" }, Labels());
        Assert.NotNull(ById("document-1-editor-input"));
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public void Transition_sample_replaces_keyed_pages_preserves_reordered_items_and_connects_measured_layouts(string theme)
    {
        Show<TransitionsSample>(theme);
        var firstPage = root.Single("TextFieldInput");
        Assert.Equal("Editable state on page 1", firstPage.Attributes["value"]);
        Click("Next page");
        Assert.NotSame(firstPage, root.Single("TextFieldInput"));
        Assert.Equal("Editable state on page 2", root.Single("TextFieldInput").Attributes["value"]);
        Click("Previous page");
        Assert.Equal("Editable state on page 1", root.Single("TextFieldInput").Attributes["value"]);

        var item = Button("Remove item 2");
        Click("Reverse order");
        Assert.Same(item, Button("Remove item 2"));
        Assert.Equal(new[] { "Remove item 3", "Remove item 2", "Remove item 1" }, root.Elements("ItemsTransitionItem").Select(node => node.Text()));
        Click("Remove item 2");
        Assert.DoesNotContain(root.Elements("Button"), node => node.Text() == "Remove item 2");
        Click("Add item");
        Assert.NotNull(Button("Remove item 4"));

        var source = root.Elements("LayoutTransition").Single(node => node.Text() == "Connected source");
        Assert.Equal(140, scene.GetBounds(source).Width);
        Click("Connect source to destination");
        var destination = root.Elements("LayoutTransition").Single(node => node.Text() == "Connected destination");
        Assert.NotSame(source, destination);
        Assert.Equal(220, scene.GetBounds(destination).Width);
        Click("Reset connected layout");
        Assert.Equal(140, scene.GetBounds(root.Elements("LayoutTransition").Single(node => node.Text() == "Connected source")).Width);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public async Task Number_presenter_sample_distinguishes_target_from_display_and_retains_editable_child(string theme)
    {
        Show<NumberPresenterSample>(theme, reduced: false);
        clock.Advance(500); Frame();
        Assert.StartsWith("Displayed: 0.250", ById("number-presenter-value").Text());
        var editor = ById("number-presenter-editor-input");
        await host.Renderer.DispatchEventAsync(editor, "oninput", new ChangeEventArgs { Value = "Persistent editor" });
        Frame();
        Click("Target three quarters");
        Assert.Equal("Application target: 0.750", ById("number-presenter-target").Text());
        Assert.StartsWith("Displayed: 0.250", ById("number-presenter-value").Text());
        clock.Advance(250); Frame();
        Assert.StartsWith("Displayed: 0.375", ById("number-presenter-value").Text());
        Assert.Same(editor, ById("number-presenter-editor-input"));
        Assert.Equal("Persistent editor", editor.Attributes["value"]);
        Click("Immediate");
        Assert.StartsWith("Displayed: 0.750", ById("number-presenter-value").Text());
        Click("Target complete");
        Assert.Equal("Displayed: 1.000; start: False; end: True", ById("number-presenter-value").Text());
        Assert.Same(editor, ById("number-presenter-editor-input"));
    }
}
