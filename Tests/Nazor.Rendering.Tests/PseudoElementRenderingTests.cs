using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PseudoContentPage : ComponentBase
{
    public string Value { get; set; } = "";
    public ImageSource? Source { get; set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.OpenElement(1, "Label");
        builder.AddAttribute(2, "source", Source);
        builder.AddContent(2, "middle");
        builder.CloseElement();
        builder.OpenComponent<EditableText>(3, typeof(PseudoContentPage));
        builder.AddComponentParameter(4, "As", "Editor");
        builder.AddComponentParameter(5, "Value", Value);
        builder.AddComponentParameter(6, "Placeholder", "Type here");
        builder.CloseComponent();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class PseudoElementRenderingTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Generated_boxes_layout_paint_hit_test_and_disappear_without_changing_authored_children(float scale)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        var label = root.Elements("Label").Single();
        var authored = label.Children.ToArray();
        var sheet = Sheet("""
            Label { display: flex; width: 240px; height: 40px; }
            Label::before { content: "before"; width: 60px; background-color: red; }
            Label::after { content: "after"; width: 60px; background-color: blue; }
            Label:hover::before { content: "hover"; }
            """);
        harness.Scene.Styles.Application = sheet;
        harness.Render(300, 160, scale);
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Equal(new[] { "before", "middle", "after", "Type here" }, commands.OfType<DrawText>().Select(t => t.Layout.Text));
        var before = harness.Scene.Styles.GetGeneratedElement(label, PseudoElement.Before)!;
        var bounds = harness.Scene.GetBounds(before);
        Assert.Equal(60, bounds.Width);
        Assert.Same(label, harness.Scene.HitTest(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2)));
        Assert.Equal(authored, label.Children);
        label.SetState(ElementStates.Hover, true);
        harness.Render(300, 160, scale);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "hover");
        sheet.Replace(Sheet("Label::before { content: none; } Label::after { content: normal; }"));
        harness.Render(300, 160, scale);
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text is "before" or "after" or "hover");
        Assert.Equal(authored, label.Children);
    }

    [Fact]
    public void Empty_content_creates_a_decorative_box_and_replaced_inputs_do_not_generate_children()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        harness.Scene.Styles.Application = Sheet("""
            Label { display: flex; }
            Label::before { content: ""; width: 14px; height: 10px; background-color: #ff0000; }
            Editor::before { content: "do not show"; }
            """);
        harness.Render(300, 160, 1);
        var before = harness.Scene.Styles.GetGeneratedElement(root.Elements("Label").Single(), PseudoElement.Before)!;
        Assert.Equal(14, harness.Scene.GetBounds(before).Width);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<FillRectangle>(), r => r.Color == new Nazor.Graphics.Color(255, 0, 0, 255));
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "do not show");
    }

    [Fact]
    public void Placeholder_text_styles_stop_applying_when_value_or_composition_is_present()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        var editor = root.Elements("Editor").Single();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Application = Sheet("""
            Editor { color: blue; font-size: 14px; width: 220px; }
            Editor::placeholder { color: red; font-size: 24px; opacity: .5; text-decoration: underline; text-transform: uppercase; text-align: right; }
            """);
        harness.Render(300, 160, 1);
        var placeholder = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "TYPE HERE");
        Assert.Equal(new Nazor.Graphics.Color(255, 0, 0, 128), placeholder.Color);
        Assert.Equal(128, placeholder.Decoration!.Color.A);
        Assert.Equal(Nazor.Graphics.TextAlignment.End, placeholder.Alignment);
        input.Focus(editor, visible: false);
        input.CompositionUpdate("draft", 2);
        harness.Render(300, 160, 1);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "draft" && t.Color == new Nazor.Graphics.Color(0, 0, 255, 255));
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "Type here");
        input.CompositionUpdate("", 0);
        input.TextInput("value");
        harness.Render(300, 160, 1);
        var value = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "value");
        Assert.Equal(new Nazor.Graphics.Color(0, 0, 255, 255), value.Color);
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "Type here");
    }

    [Theory]
    [InlineData("start")]
    [InlineData("center")]
    [InlineData("right")]
    public void Selection_paints_foreground_only_inside_selection_and_uses_configured_background(string alignment)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        var page = (PseudoContentPage)root.Component;
        page.Value = "selected text";
        page.Refresh();
        harness.Host.RunPending();
        var editor = root.Elements("Editor").Single();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Application = Sheet($$"""
            Editor { color: blue; width: 240px; text-align: {{alignment}}; }
            Editor::selection { color: white; background-color: #ff0000; }
            """);
        harness.Render(300, 160, 1);
        input.Focus(editor, visible: false);
        input.KeyDown("Home", KeyModifiers.None, false);
        input.KeyDown("ArrowRight", KeyModifiers.Shift, false);
        harness.Render(300, 160, 1);
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Contains(commands.OfType<FillRectangle>(), r => r.Color == new Nazor.Graphics.Color(255, 0, 0, 255));
        Assert.Contains(commands.OfType<DrawText>(), t => t.Layout.Text == "selected text" && t.Color == Nazor.Graphics.Color.White);
        Assert.Contains(commands.OfType<DrawText>(), t => t.Layout.Text == "selected text" && t.Color == new Nazor.Graphics.Color(0, 0, 255, 255));
        foreach (var index in Enumerable.Range(0, commands.Count).Where(i => commands[i] is DrawText t && t.Layout.Text == "selected text"))
            Assert.IsType<PushClip>(commands[index - 1]);
        Assert.Equal(1, editor.GetFeature<TextEditState>()!.SelectionEnd);
    }

    [Fact]
    public void Wrapped_selection_uses_per_line_rectangles_and_an_explicit_transparent_background()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        var page = (PseudoContentPage)root.Component;
        page.Value = "several words that wrap across multiple lines";
        page.Refresh();
        harness.Host.RunPending();
        var editor = root.Elements("Editor").Single();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Application = Sheet("""
            Editor { width: 90px; white-space: pre-wrap; }
            Editor::selection { color: #ff0000; background-color: transparent; }
            """);
        harness.Render(300, 300, 1);
        input.Focus(editor, visible: false);
        input.KeyDown("a", KeyModifiers.Control, false);
        harness.Render(300, 300, 1);
        var commands = harness.Scene.BuildDisplayList().Commands;
        var lines = harness.Scene.GetEditableLayout(editor)!.Lines.Count;
        Assert.True(lines > 1);
        Assert.Equal(lines, commands.OfType<DrawText>().Count(t => t.Color == new Nazor.Graphics.Color(255, 0, 0, 255)));
        Assert.Equal(lines, commands.OfType<FillRectangle>().Count(r => r.Color == Nazor.Graphics.Color.Transparent));
        Assert.Equal(page.Value, editor.GetFeature<TextEditState>()!.Value);
    }

    private static StyleSheet Sheet(string source)
    {
        var result = NssCompiler.Compile("pseudo-render", source);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Switching_to_a_replaced_image_removes_previously_projected_boxes()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        harness.Scene.Styles.Application = Sheet("Label::before { content: \"decoration\"; }");
        harness.Render(300, 160, 1);
        Assert.Contains(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "decoration");
        var page = (PseudoContentPage)root.Component;
        page.Source = ImageSource.FromBytes(System.Text.Encoding.UTF8.GetBytes("<svg width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"blue\"/></svg>"));
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(300, 160, 1);
        var loading = harness.Scene.WhenImagesLoaded();
        harness.Host.RunUntil(() => loading.IsCompleted, 10000);
        Assert.True(loading.IsCompletedSuccessfully, loading.Exception?.ToString());
        harness.Render(300, 160, 1);
        Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), t => t.Layout.Text == "decoration");
    }

    [Fact]
    public void Generated_box_background_images_participate_in_resource_loading()
    {
        var name = "pseudo-background-" + Guid.NewGuid().ToString("N") + ".svg";
        var path = Path.Combine(AppContext.BaseDirectory, name);
        File.WriteAllText(path, "<svg width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"blue\"/></svg>");
        try
        {
            using var harness = new RenderHarness();
            harness.Host.Render<PseudoContentPage>();
            harness.Scene.Styles.Application = Sheet($$"""
                Label::before { content: ""; width: 10px; height: 10px; background-image: url("{{name}}"); }
                """);
            harness.Render(300, 160, 1);
            var loading = harness.Scene.WhenImagesLoaded();
            harness.Host.RunUntil(() => loading.IsCompleted, 10000);
            Assert.True(loading.IsCompletedSuccessfully, loading.Exception?.ToString());
            harness.Render(300, 160, 1);
            Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<FillImage>());
        }
        finally { File.Delete(path); }
    }
}
