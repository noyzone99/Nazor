using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class InlineFlowInputTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly RootComponent root;

    public InlineFlowInputTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(160, 300) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene) { Clipboard = new MemoryClipboard() };
        root = host.Render<Article>();
        scene.Update();
    }

    public void Dispose()
    {
        input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose();
    }

    private ElementNode Element(string id) => root.Elements().Single(element => element.Id == id);

    [Fact]
    public void Long_mixed_prose_keeps_normalized_copy_and_measured_geometry_across_wrapped_lines()
    {
        const int count = 80;
        var article = host.Render<LongArticle>();
        scene.Scale = 1.5f;
        scene.Update();
        var paragraph = article.Single("Paragraph");
        var text = scene.GetEditableLayout(paragraph)!;
        var expected = string.Join(" ", Enumerable.Range(0, count).Select(index => $"entry{index} café A B Ctrl"));
        Assert.Equal(expected, text.Text);
        Assert.True(text.Lines.Count > 20);
        Assert.All(text.Lines, line => Assert.True(float.IsFinite(line.Width) && float.IsFinite(line.Height) && line.Height > 0));
        for (var index = 1; index < text.Lines.Count; index++)
            Assert.Equal(text.Lines[index - 1].Top + text.Lines[index - 1].Height, text.Lines[index].Top, 0.001f);
        var strong = article.Elements("Strong").ToArray();
        Assert.Equal(count, strong.Length);
        Assert.True(scene.GetBounds(strong[^1]).Y > scene.GetBounds(strong[0]).Y);
        Assert.All(article.Elements("Kbd"), key => Assert.True(scene.GetBounds(key).Width > 0));
        Assert.True(scene.GetBounds(paragraph).Height + 1 >= text.Height);
        input.Focus(paragraph, false);
        input.KeyDown("a", KeyModifiers.Control, false);
        input.KeyDown("c", KeyModifiers.Control, false);
        Assert.Equal(expected, input.Clipboard.GetText());
    }

    [Fact]
    public void Selection_and_copy_use_the_normalized_shared_text_across_styles_and_hard_breaks()
    {
        var paragraph = Element("copy");
        var text = scene.GetEditableLayout(paragraph)!;
        Assert.Equal("Copy styled café A B\nnext", text.Text);
        var content = scene.GetContentBounds(paragraph);
        var first = text.GetCaretRect(0);
        var last = text.GetCaretRect(text.Text.Length);
        var start = new Point(content.X + first.X, content.Y + first.Y + first.Height / 2);
        var end = new Point(content.X + last.X + 2, content.Y + last.Y + last.Height / 2);
        input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(end, KeyModifiers.None);
        input.PointerReleased(end, PointerButton.Primary, KeyModifiers.None);
        input.KeyDown("c", KeyModifiers.Control, false);
        Assert.Equal(text.Text, input.Clipboard.GetText());
        var state = paragraph.GetFeature<TextEditState>()!;
        Assert.Equal((0, text.Text.Length), (state.SelectionStart, state.SelectionEnd));
        Assert.Contains(scene.BuildDisplayList().Commands.OfType<FillRectangle>(), rectangle => rectangle.Color.A == 0x60);
    }

    [Fact]
    public void Wrapped_links_hit_each_fragment_but_not_empty_regions_of_their_union()
    {
        var paragraph = Element("links");
        var link = Element("link");
        var text = scene.GetEditableLayout(paragraph)!;
        var content = scene.GetContentBounds(paragraph);
        var start = text.Text.IndexOf("long", StringComparison.Ordinal);
        var end = text.Text.IndexOf(" after", StringComparison.Ordinal);
        var fragments = text.GetSelectionRects(start, end).ToArray();
        Assert.True(fragments.Length > 1);
        Assert.True(fragments[0].X > fragments[1].X + 2);
        var gap = new Point(content.X + fragments[1].X + 1, content.Y + fragments[0].Y + fragments[0].Height / 2);
        Assert.True(scene.GetBounds(link).Contains(gap));
        Assert.DoesNotContain(link, Ancestors(scene.HitTest(gap)));
        Click(gap);
        Assert.Equal(0, ((Article)root.Component).Activations);
        var area = fragments[0];
        Click(new Point(content.X + area.X + area.Width / 2, content.Y + area.Y + area.Height / 2));
        Assert.Equal(1, ((Article)root.Component).Activations);
        input.Focus(null, false);
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(link, input.Focused);
        input.KeyDown("Enter", KeyModifiers.None, false);
        host.RunPending();
        scene.Update();
        Assert.Equal(2, ((Article)root.Component).Activations);
        var outlines = scene.BuildDisplayList().Commands.OfType<DrawBorder>().Where(border => border.Top.Color.A > 0).ToArray();
        Assert.NotEmpty(outlines);
    }

    [Fact]
    public void Accessible_paragraph_keeps_its_link_action_without_duplicate_decorative_text_nodes()
    {
        using var tree = new SemanticTree(host.Renderer, scene, input);
        tree.Update();
        var paragraph = Assert.Single(tree.Descendants(), node => node.Element == Element("links"));
        Assert.Equal(SemanticRole.Text, paragraph.Role);
        Assert.Equal("Before long styled link text that wraps after.", paragraph.Name);
        var link = Assert.Single(paragraph.Children);
        Assert.Equal(SemanticRole.Hyperlink, link.Role);
        Assert.Equal("long styled link text that wraps", link.Name);
        Assert.Same(Element("link"), link.Element);
        Assert.Empty(link.Children);
    }

    private void Click(Point point)
    {
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        host.RunPending(); scene.Update();
    }

    [Fact]
    public void Described_abbreviations_expose_the_expansion_without_adding_a_tab_stop()
    {
        using var tree = new SemanticTree(host.Renderer, scene, input);
        tree.Update();
        var paragraph = Assert.Single(tree.Descendants(), node => node.Element == Element("abbreviations"));
        Assert.Equal("WCAG API plain", paragraph.Name);
        Assert.Equal(2, paragraph.Children.Count);
        var wcag = paragraph.Children[0];
        Assert.Equal(SemanticRole.Text, wcag.Role);
        Assert.Equal("WCAG", wcag.Name);
        Assert.Equal("Web Content Accessibility Guidelines", wcag.Description);
        Assert.False(wcag.IsFocusable);
        var api = paragraph.Children[1];
        Assert.Equal("API", api.Name);
        Assert.Equal("Application programming interface", api.Description);
        Assert.False(api.IsFocusable);
    }

    private static IEnumerable<ElementNode> Ancestors(ElementNode? element)
    {
        for (var current = element; current is not null; current = current.ParentElement) yield return current;
    }

    private sealed class Article : ComponentBase
    {
        public int Activations { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Paragraph"); builder.AddAttribute(1, "id", "copy");
            builder.AddAttribute(2, "role", "text"); builder.AddAttribute(3, "selectable", true);
            builder.AddAttribute(4, "focusable", true); builder.AddAttribute(5, "tabindex", -1);
            builder.AddAttribute(6, "style", "margin: 0; font-family: Fira Sans; font-size: 14px; line-height: 1.5");
            builder.AddContent(7, "  Copy \n "); builder.OpenElement(8, "Strong"); builder.AddContent(9, "styled café  "); builder.CloseElement();
            builder.OpenElement(10, "Span"); builder.AddContent(11, " A B "); builder.CloseElement();
            builder.OpenElement(12, "LineBreak"); builder.AddAttribute(13, "line-break", true); builder.CloseElement();
            builder.AddContent(14, " next  "); builder.CloseElement();

            builder.OpenElement(20, "Paragraph"); builder.AddAttribute(21, "id", "links"); builder.AddAttribute(22, "role", "text");
            builder.AddAttribute(23, "style", "margin: 10px 0 0; font-family: Fira Sans; font-size: 16px; line-height: 1.8");
            builder.AddContent(24, "Before "); builder.OpenElement(25, "Link"); builder.AddAttribute(26, "id", "link");
            builder.AddAttribute(27, "role", "link"); builder.AddAttribute(28, "focusable", true);
            builder.AddAttribute(29, "onclick", EventCallback.Factory.Create<PointerEventArgs>(this, _ => Activations++));
            builder.AddContent(30, "long "); builder.OpenElement(31, "Span"); builder.AddContent(32, "styled link"); builder.CloseElement();
            builder.AddContent(33, " text that wraps"); builder.CloseElement(); builder.AddContent(34, " after.");
            builder.CloseElement();

            builder.OpenElement(40, "Paragraph"); builder.AddAttribute(41, "id", "abbreviations"); builder.AddAttribute(42, "role", "text");
            builder.OpenComponent<Abbreviation>(43);
            builder.AddComponentParameter(44, nameof(Control.AccessibleDescription), "Web Content Accessibility Guidelines");
            builder.AddComponentParameter(45, nameof(Abbreviation.ChildContent), (RenderFragment)(text => text.AddContent(0, "WCAG")));
            builder.CloseComponent(); builder.AddContent(46, " ");
            builder.OpenComponent<Abbreviation>(47);
            builder.AddComponentParameter(48, nameof(Control.Tooltip), "Application programming interface");
            builder.AddComponentParameter(49, nameof(Abbreviation.ChildContent), (RenderFragment)(text => text.AddContent(0, "API")));
            builder.CloseComponent(); builder.AddContent(50, " ");
            builder.OpenComponent<Abbreviation>(51);
            builder.AddComponentParameter(52, nameof(Abbreviation.ChildContent), (RenderFragment)(text => text.AddContent(0, "plain")));
            builder.CloseComponent(); builder.CloseElement();
        }
    }

    private sealed class LongArticle : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Paragraph"); builder.AddAttribute(1, "role", "text");
            builder.AddAttribute(2, "selectable", true); builder.AddAttribute(3, "focusable", true); builder.AddAttribute(4, "tabindex", -1);
            builder.AddAttribute(5, "style", "font-family: Fira Sans; font-size: 14px; line-height: 1.5; margin: 0");
            for (var index = 0; index < 80; index++)
            {
                builder.AddContent(10, $"  entry{index}\n ");
                builder.OpenElement(11, "Strong"); builder.AddContent(12, "café A B "); builder.CloseElement();
                builder.OpenElement(13, "Kbd"); builder.AddContent(14, "Ctrl"); builder.CloseElement();
            }
            builder.CloseElement();
        }
    }
}
