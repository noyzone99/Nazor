using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Samples.Gallery;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GallerySampleTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
    private readonly IServiceScope scope;
    private readonly TestHost host;
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree tree;
    private readonly RootComponent root;
    private IApplicationAppearance Appearance => scope.ServiceProvider.GetRequiredService<IApplicationAppearance>();
    public GallerySampleTests()
    {
        scope = services.CreateScope(); host = new TestHost(scope.ServiceProvider);
        foreach (var file in Nazor.Themes.Fluent.Themes.FontFiles)
            ((SkiaFontProvider)graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images) { Viewport = new Size(1180,820) };
        input = new InputManager(host.Renderer,scene); tree = new SemanticTree(host.Renderer,scene,input);
        root = host.Render<App>(); Update();
    }
    public void Dispose() { tree.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); scope.Dispose(); services.Dispose(); graphics.Dispose(); }
    private void Update() { host.RunPending(); scene.Styles.Theme = Appearance.Theme ?? Themes.Themes.Basic; tree.Update(); }
    private ElementNode ById(string id) => root.Elements().Single(e=>Equals(e.Attributes.GetValueOrDefault("id"),id));
    private async Task Show(string id) { await host.ClickAsync(ById($"nav-{id}")); Update(); }
    private async Task Pick(string id,string text) { await host.ClickAsync(ById(id)); Update(); await host.ClickAsync(root.Elements("SelectOption").Single(e=>e.Text()==text)); Update(); }
    private async Task Click(string text) { await host.ClickAsync(ById("sample-preview").Elements().Single(e=>Equals(e.Attributes.GetValueOrDefault("role"),"button") && e.Text()==text)); Update(); }

    [Fact]
    public void Catalog_covers_each_family_with_unique_compiled_examples()
    {
        Assert.Equal(GalleryCatalog.Entries.Count, GalleryCatalog.Entries.Select(entry => entry.Id).Distinct().Count());
        Assert.Equal(GalleryCatalog.Categories, GalleryCatalog.Entries.Select(entry => entry.Category).Distinct());
        Assert.All(GalleryCatalog.Entries, entry =>
        {
            Assert.Same(typeof(Controls.Button).Assembly, entry.ComponentType.Assembly);
            Assert.Same(typeof(App).Assembly, entry.SampleType.Assembly);
            Assert.False(string.IsNullOrWhiteSpace(entry.Code));
        });
    }

    [Fact]
    public void Catalog_shows_every_public_concrete_control_in_searchable_example_source()
    {
        var assembly = typeof(Controls.Button).Assembly;
        var shown = GalleryCatalog.Entries.SelectMany(entry => entry.ComponentTypes.Select(type => (Entry: entry, Type: type)))
            .Where(pair => typeof(IComponent).IsAssignableFrom(pair.Type)).ToArray();
        var represented = shown.Select(pair => pair.Type.IsGenericType ? pair.Type.GetGenericTypeDefinition() : pair.Type).ToHashSet();
        var expected = assembly.GetExportedTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type));
        Assert.Empty(expected.Except(represented).Select(type => type.FullName));

        Assert.All(shown, pair =>
        {
            var name = pair.Type.Name.Split('`')[0];
            Assert.True(pair.Entry.Matches(name), $"{pair.Entry.Id} is not searchable by {name}.");
            Assert.Matches($@"<{Regex.Escape(name)}(?=[\s/>])", pair.Entry.Code);
        });
    }

    [Theory]
    [InlineData("Basic")][InlineData("Fluent")][InlineData("None")][InlineData("NazorLeaf")]
    public async Task Every_page_renders_with_actual_selectable_source_in_each_theme(string theme)
    {
        await Pick("gallery-theme",theme);
        foreach(var entry in GalleryCatalog.Entries)
        {
            await Show(entry.Id);
            Assert.Equal(entry.Title,ById("page-title").Text());
            Assert.Equal(entry.Code,ById("sample-source").Text());
            Assert.True(ById("sample-source").Attributes.GetValueOrDefault("selectable") is true);
            Assert.NotEmpty(ById("sample-preview").Children);
            Assert.True(scene.GetBounds(ById("sample-preview")).Width>150,entry.Id);
            await Save($"{theme}-{entry.Id}");
        }
    }

    [Fact]
    public async Task Theme_picker_changes_computed_styles_and_preserves_sample_state()
    {
        await Show("button"); await Click("Save");
        foreach(var theme in new[] {"None","Basic"})
        {
            await Pick("gallery-theme",theme);
            Assert.Contains("Saved: 1",ById("sample-preview").Text());
            var button=ById("sample-preview").Elements().Where(e=>e.Name=="Button").First();
            Assert.Equal(theme == "Basic" ? 1 : 0, scene.Styles.GetStyle(button).Get(StyleProperties.BorderLeftWidth).Value);
            Assert.True(scene.GetBounds(ById("gallery-theme")).Bottom<=scene.Viewport.Height);
        }
        await Pick("gallery-scheme","Dark"); Assert.Equal(ColorScheme.Dark,Appearance.ColorScheme);
    }

    [Fact]
    public async Task Live_theme_change_updates_an_open_surface_without_recreating_it()
    {
        await Show("dialog"); await Click("Medium dialog");
        var dialog=root.Single("Dialog"); var focus=input.Focused;
        Appearance.Theme=Themes.Themes.Basic; Update();
        Assert.Same(dialog,root.Single("Dialog")); Assert.Same(focus,input.Focused);
        await Click("Export"); Assert.Empty(scene.Overlays);
    }

    [Theory]
    [InlineData("DataGrid", "data-grid")]
    [InlineData("TableHeaderCell", "table")]
    public async Task Search_finds_controls_and_their_composition_parts(string term, string page)
    {
        await host.Renderer.DispatchEventAsync(ById("gallery-caption-search-input"), "oninput", new ChangeEventArgs { Value = term });
        Update();
        Assert.NotNull(ById($"card-{page}"));
        Assert.DoesNotContain(root.Elements(), element => element.Id == "card-button");
    }

    [Fact]
    public async Task Search_suggestions_example_highlights_model_fields_and_shows_custom_empty_content()
    {
        await Show("search-suggestions");
        var editor = ById("sample-preview").Elements().First(e => e.Name == "SearchFieldInput");
        Assert.Equal("Try Italy or Paris", editor.Attributes.GetValueOrDefault("placeholder-text"));
        await host.Renderer.DispatchEventAsync(editor, "oninput", new ChangeEventArgs { Value = "Italy" });
        Update();
        Assert.Equal(2, root.Elements("SearchFieldOption").Count());
        Assert.Equal(new[] { "Italy", "Italy" }, root.Elements("SearchFieldOption").SelectMany(e => e.Elements()).Where(e => e.Name == "Strong").Select(e => e.Text()));
        await host.ClickAsync(ById("sample-preview").Elements().Single(e => e.Name == "CheckBox"));
        Update();
        Assert.DoesNotContain(root.Elements("SearchFieldOption").SelectMany(e => e.Elements()), e => e.Name == "Strong");
        await host.Renderer.DispatchEventAsync(editor, "oninput", new ChangeEventArgs { Value = "Nowhere" });
        Update();
        Assert.Empty(root.Elements("SearchFieldOption"));
        Assert.Equal("No matching places", root.Single("SearchFieldEmpty").Text());
    }

    [Fact]
    public async Task Table_example_exposes_its_caption_and_merged_cells()
    {
        await Show("table");
        Assert.Contains(tree.Descendants(), node => node.Role == SemanticRole.Table && node.Name == "Quarterly sales");
        Assert.Contains(tree.Descendants(), node => node.Role == SemanticRole.RowHeader && node.Name == "North" && node.RowSpan == 2);
        Assert.Contains(tree.Descendants(), node => node.Role == SemanticRole.Cell && node.Name == "Audited" && node.ColumnSpan == 2);
    }

    [Fact]
    public async Task Frame_example_displays_the_selected_route()
    {
        await Show("frame");
        await Click("Overview");
        Assert.Contains("overview", root.Single("Frame").Text());
        await Click("Details");
        Assert.Contains("details", root.Single("Frame").Text());
        await Click("Back");
        Assert.Contains("overview", root.Single("Frame").Text());
    }

    [Fact]
    public async Task Examples_keep_selection_form_and_overlay_behavior()
    {
        await Pick("gallery-theme", "Basic");
        await Show("select");
        var select = ById("sample-preview").Elements().Where(element => element.Name == "Select").Last();
        await host.ClickAsync(select);
        Update();
        Assert.Single(scene.Overlays);
        await host.ClickAsync(root.Elements("SelectOption").Single(element => element.Text() == "Paris"));
        Update();
        Assert.Contains("city: Paris", ById("sample-preview").Text());
        Assert.Empty(scene.Overlays);
        await Show("form-layout");
        await Click("Submit");
        Assert.Contains("Submitted Giulia", ById("sample-preview").Text());
        await Show("dialog");
        await Click("Medium dialog");
        Assert.Equal("Export report", tree.Descendants().Single(node => node.Role == SemanticRole.Dialog).Name);
        Assert.Single(scene.Overlays, overlay => overlay.Name == "Dialog");
        await Click("Export");
        Assert.Empty(scene.Overlays);
    }

    [Theory]
    [InlineData(1180,820)][InlineData(720,640)]
    public async Task Gallery_fits_the_viewport(int width,int height)
    {
        scene.Viewport=new Size(width,height); Update(); await Save($"home-{width}"); await Show("text-field");
        Assert.True(scene.GetBounds(ById("sample-preview")).Right<=width+1);
    }
    [Theory]
    [InlineData(ColorScheme.Light, false)]
    [InlineData(ColorScheme.Dark, false)]
    [InlineData(ColorScheme.Light, true)]
    [InlineData(ColorScheme.Dark, true)]
    public async Task Fluent_examples_and_source_have_distinct_surface_depth_and_respect_forced_colors(ColorScheme scheme, bool forcedColors)
    {
        await Pick("gallery-theme", "Fluent");
        await Pick("gallery-scheme", scheme.ToString());
        scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors);
        await Show("text-field");
        await Save($"Fluent-depth-{scheme}-{forcedColors}");
        var preview = scene.GetBounds(ById("sample-preview"));
        var source = scene.GetBounds(root.Elements().Single(element => element.Class == "source-heading"));
        var page = scene.GetBounds(root.Elements().Single(element => element.Class == "main-scroll"));
        var image = graphics.RenderToImage(scene.BuildDisplayList(), 1, scene.Background);
        var examplePixel = image.GetPixel((int)preview.X + 5, (int)preview.Y + 5);
        var pagePixel = image.GetPixel((int)page.X + 5, (int)preview.Y + 5);
        var sourcePixel = image.GetPixel((int)source.X + 5, (int)source.Y + 5);
        if (forcedColors)
        {
            Assert.Equal(pagePixel, examplePixel);
            Assert.Equal(pagePixel, sourcePixel);
        }
        else if (scheme == ColorScheme.Dark)
        {
            Assert.True(examplePixel.R < pagePixel.R, $"Example {examplePixel}; page {pagePixel}.");
            Assert.True(pagePixel.R < sourcePixel.R, $"Page {pagePixel}; source {sourcePixel}.");
        }
        else
        {
            Assert.True(sourcePixel.R < pagePixel.R, $"Source {sourcePixel}; page {pagePixel}.");
            Assert.True(pagePixel.R < examplePixel.R, $"Page {pagePixel}; example {examplePixel}.");
        }
    }

    private async Task Save(string name)
    {
        var directory=Path.Combine(Path.GetFullPath("../../../../../artifacts/base-gallery-preview",AppContext.BaseDirectory),OperatingSystem.IsWindows() ? "Windows" : "Linux"); Directory.CreateDirectory(directory);
        var loaded=scene.WhenImagesLoaded(); host.RunUntil(()=>loaded.IsCompleted); await loaded; Update();
        var display=scene.BuildDisplayList();
        if(name.EndsWith("-image",StringComparison.Ordinal))
            Assert.Contains(display.Commands,command=>command is DrawImage);
        var image=graphics.RenderToImage(display,1,scene.Background);
        File.WriteAllBytes(Path.Combine(directory,name+".png"),graphics.EncodePng(image));
    }
}

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class GalleryWindowTests
{
    [Fact]
    public void Title_bar_example_opens_in_a_separate_window()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<App>(new() { Activate = false }));
        var gallery = harness.Window<App>();
        try
        {
            ApplicationHarness.On(gallery, () =>
            {
                Assert.True(gallery.Input.Activate(gallery.Root.Elements().Single(e => e.Id == "nav-window-chrome")));
                Assert.True(gallery.Input.Activate(gallery.Root.Elements("Button").Single(e => e.Text() == "Open title bar preview")));
                return true;
            });

            var preview = harness.Window<Nazor.Samples.Gallery.Examples.Frame.WindowChromePreview>();
            ApplicationHarness.On(preview, () =>
            {
                try
                {
                    Assert.Single(preview.Root.Elements("TitleBar"));
                    Assert.Single(preview.Root.Elements("SnapLayoutArea"));
                    Assert.Equal(2, preview.Root.Elements("CaptionButtonsButton").Count());
                    Assert.Empty(preview.Exceptions);
                    return true;
                }
                finally
                {
                    preview.Close();
                }
            });
            ApplicationHarness.On(gallery, () =>
            {
                Assert.Equal(WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native), gallery.AppliedDecorations!.Configuration);
                var surface = Assert.Single(gallery.Root.Elements().Single(element => element.Id == "sample-preview").Elements(), element => element.Name == "TitleBar");
                Assert.Contains("Composable title-bar surface", surface.Text());
                Assert.Contains(surface, gallery.Root.Elements().Single(element => element.Id == "sample-preview").Elements());
                return true;
            });
        }
        finally
        {
            ApplicationHarness.On(gallery, () => { gallery.Close(); return true; });
        }
        Assert.True(harness.WaitForExit());
    }
}
