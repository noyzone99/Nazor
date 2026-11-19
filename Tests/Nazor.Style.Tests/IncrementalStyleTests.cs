using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class IncrementalStyleTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public IncrementalStyleTests()
    {
        engine = new StyleEngine(host.Renderer) { Viewport = new Viewport(1000, 500) };
        root = host.Render<Nested>();
    }

    public void Dispose()
    {
        engine.Dispose();
        host.Dispose();
    }

    private void Use(string nss)
    {
        var result = NssCompiler.Compile("test", nss, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        engine.Application = result.StyleSheet;
        engine.Update();
    }

    private ComputedStyle Style(string name) => engine.GetStyle(root.Single(name));

    [Fact]
    public void Shared_cascades_preserve_element_dependencies_and_invalidate_between_passes()
    {
        var siblings = host.Render<Siblings>();
        Use("Item { width: 10vw; color: var(--ink, red); } Item:hover { width: inherit; } Parent { width: 123px; }");
        var items = siblings.Elements("Item").ToArray();
        Assert.Same(engine.GetStyle(items[0]), engine.GetStyle(items[1]));
        items[0].SetState(ElementStates.Hover, true);
        engine.Update();
        Assert.Equal(SizeValue.Fixed(Length.Px(123)), engine.GetStyle(items[0]).Get(StyleProperties.Width));
        Assert.Equal(SizeValue.Fixed(Length.Px(100)), engine.GetStyle(items[1]).Get(StyleProperties.Width));
        engine.Viewport = new(800, 400);
        engine.Update();
        Assert.Equal(SizeValue.Fixed(Length.Px(80)), engine.GetStyle(items[1]).Get(StyleProperties.Width));
        Use("Parent { --ink: blue; } Item { color: var(--ink); }");
        Assert.All(items, item => Assert.Equal(new Color(0, 0, 255, 255), engine.GetStyle(item).Color));
    }

    private sealed class Siblings : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Parent");
            for (var i = 0; i < 2; i++)
            {
                builder.OpenElement(1, "Item");
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }

    [Fact]
    public void A_restyle_with_the_same_values_keeps_the_same_style()
    {
        Use("Top { color: #ff0000; } Top:hover { color: #ff0000; }");
        var before = Style("Top");
        var middle = Style("Middle");
        root.Single("Top").SetState(ElementStates.Hover, true);
        engine.Update();
        Assert.Same(before, Style("Top"));
        Assert.Equal(1, engine.LastRecomputedCount);
        Assert.Same(middle, Style("Middle"));
    }

    [Theory]
    [InlineData("Box[edge] { background-color: red; }", 1)]
    [InlineData("Item:is(Box[edge] Item) { background-color: red; }", 2)]
    [InlineData("Box[edge] { color: red; }", 2)]
    public void An_attribute_restyles_descendants_only_when_they_can_depend_on_it(string css, int count)
    {
        var tree = host.Render<EdgeTree>();
        Use(css);
        var item = tree.Single("Item");
        var original = engine.GetStyle(item);
        ((EdgeTree)tree.Component).SetEdge();
        host.RunPending();
        engine.Update();
        Assert.Equal(count, engine.LastRecomputedCount);
        Assert.Equal(count == 1, ReferenceEquals(original, engine.GetStyle(item)));
    }

    private sealed class EdgeTree : ComponentBase
    {
        private bool edge;

        public void SetEdge()
        {
            edge = true;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Box");
            if (edge) builder.AddAttribute(1, "edge", true);
            builder.OpenElement(2, "Item");
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    [Fact]
    public void Children_compute_again_when_what_they_inherit_changes()
    {
        Use("Top { width: 50vw; }");
        var middle = Style("Middle");
        engine.Viewport = new Viewport(800, 400);
        engine.Update();
        Assert.Equal(1, engine.LastRecomputedCount);
        Assert.Same(middle, Style("Middle"));

        Use("Top { font-size: 2vw; } Leaf { width: 2em; }");
        engine.Viewport = new Viewport(1000, 500);
        engine.Update();
        Assert.Equal(3, engine.LastRecomputedCount);
        Assert.Equal(SizeValue.Fixed(Length.Px(40)), Style("Leaf").Get(StyleProperties.Width));
    }

    [Fact]
    public void Inherit_follows_the_parent_for_properties_that_are_not_inherited()
    {
        Use("Top { width: 50vw; } Middle { width: inherit; }");
        Assert.Equal(SizeValue.Fixed(Length.Px(500)), Style("Middle").Get(StyleProperties.Width));
        engine.Viewport = new Viewport(800, 400);
        Assert.Equal(SizeValue.Fixed(Length.Px(400)), Style("Middle").Get(StyleProperties.Width));
    }

    [Fact]
    public void A_state_tested_before_the_subject_restyles_the_descendants_it_can_be_about()
    {
        Use("Top:hover Leaf { width: 5px; } Middle:hover > Leaf { height: 5px; }");
        root.Single("Top").SetState(ElementStates.Hover, true);
        engine.Update();
        Assert.Equal(SizeValue.Fixed(Length.Px(5)), Style("Leaf").Get(StyleProperties.Width));
        Assert.Equal(3, engine.LastRecomputedCount);

        root.Single("Top").SetState(ElementStates.Pressed, true);
        engine.Update();
        Assert.Equal(1, engine.LastRecomputedCount);

        root.Single("Middle").SetState(ElementStates.Hover, true);
        engine.Update();
        Assert.Equal(SizeValue.Fixed(Length.Px(5)), Style("Leaf").Get(StyleProperties.Height));
        Assert.Equal(2, engine.LastRecomputedCount);
    }

    [Fact]
    public void States_inside_logical_pseudo_classes_count_too()
    {
        Use("Leaf:is(Top:hover *) { width: 7px; }");
        root.Single("Top").SetState(ElementStates.Hover, true);
        engine.Update();
        Assert.Equal(SizeValue.Fixed(Length.Px(7)), Style("Leaf").Get(StyleProperties.Width));
    }

    [Fact]
    public void Nothing_dirty_means_nothing_recomputed()
    {
        Use("Top { width: 1px; }");
        Assert.False(engine.IsDirty);
        engine.Update();
        Assert.Equal(0, engine.LastRecomputedCount);
    }
}
