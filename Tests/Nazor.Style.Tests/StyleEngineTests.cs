using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class Box : ComponentBase
{
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Box");
        builder.AddAttribute(1, "class", Class);
        builder.AddAttribute(2, "id", Id);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}

public sealed class Card : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(Card));
        builder.AddComponentParameter(1, "Class", "inner");
        builder.AddComponentParameter(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}

public sealed class Page : ComponentBase
{
    public string OuterClass { get; set; } = "outer";

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(Page));
        builder.AddComponentParameter(1, "Class", OuterClass);
        builder.AddComponentParameter(2, "Id", "main");
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<Card>(4, typeof(Page));
            b.AddComponentParameter(5, "ChildContent", (RenderFragment)(c =>
            {
                c.OpenComponent<Box>(6, typeof(Page));
                c.AddComponentParameter(7, "Class", "projected inner");
                c.CloseComponent();
            }));
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Badge : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(Badge));
        builder.AddComponentParameter(1, "Class", "inner");
        builder.CloseComponent();
    }
}

public sealed class Siblings : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Card>(0, typeof(Siblings));
        builder.CloseComponent();
        builder.OpenComponent<Badge>(1, typeof(Siblings));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class StyleEngineTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;

    public StyleEngineTests()
    {
        engine = new StyleEngine(host.Renderer);
        Scoped<Page>("");
        Scoped<Card>("");
        Scoped<Badge>("");
    }

    public void Dispose()
    {
        engine.Dispose();
        host.Dispose();
    }

    private static StyleSheet Sheet(string text)
    {
        var result = NssCompiler.Compile("test", text, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private static void Scoped<T>(string text)
    {
        var sheet = Sheet(text);
        StyleRegistry.RegisterScoped(typeof(T), () => sheet);
    }

    private (RootComponent Root, ElementNode Outer, ElementNode CardInner, ElementNode Projected) RenderPage()
    {
        var root = host.Render<Page>();
        var boxes = root.Elements("Box").ToArray();
        return (root, boxes[0], boxes.Single(b => b.Class == "inner"), boxes.Single(b => b.Class == "projected inner"));
    }

    [Fact]
    public void Global_scrollbar_defaults_are_inherited_and_component_rules_override_locally()
    {
        engine.Theme = Sheet(":root { scrollbar-width: 9px; scrollbar-thumb-color: red; scrollbar-visibility: autohide; }");
        Scoped<Card>(".inner { scrollbar-width: 5px; scrollbar-thumb-color: blue; scrollbar-visibility: visible; }");
        var (_, outer, cardInner, projected) = RenderPage();
        engine.Update();
        Assert.Equal(Length.Px(9), engine.GetStyle(outer).Get(StyleProperties.ScrollbarWidth));
        Assert.Equal(ScrollbarVisibility.AutoHide, engine.GetStyle(outer).Get(StyleProperties.ScrollbarVisibility));
        Assert.Equal(Length.Px(5), engine.GetStyle(cardInner).Get(StyleProperties.ScrollbarWidth));
        Assert.Equal(new Color(0, 0, 255, 255), engine.GetStyle(cardInner).Get(StyleProperties.ScrollbarThumbColor));
        Assert.Equal(Length.Px(5), engine.GetStyle(projected).Get(StyleProperties.ScrollbarWidth));
    }

    [Theory]
    [InlineData(".outer ::deep .inner { padding: 7px; }")]
    [InlineData(".outer { ::deep .inner { padding: 7px; } }")]
    [InlineData("::deep .inner { padding: 7px; }")]
    public void Deep_crosses_child_scopes_only_below_an_anchor_owned_by_the_sheet(string nss)
    {
        Scoped<Page>(nss);
        var (_, outer, inner, projected) = RenderPage();
        var outside = host.Render<Badge>().Elements("Box").Single();
        Assert.Equal(Length.Px(7), engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(7), engine.GetStyle(projected).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Zero, engine.GetStyle(outside).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Zero, engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
        Assert.Contains(engine.Trace(inner).Rules, rule => rule.Selector.Contains("::deep"));
        Scoped<Page>(".inner ::deep .projected { padding: 9px; }");
        Assert.Equal(Length.Zero, engine.GetStyle(projected).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Deep_uses_specificity_then_nearest_scope_for_ties_and_trace_agrees()
    {
        Scoped<Page>("::deep .inner { padding: 7px; }");
        Scoped<Card>(".inner { padding: 3px; }");
        var (_, _, inner, _) = RenderPage();
        Assert.Equal(Length.Px(3), engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        Assert.Equal(".inner", engine.Trace(inner).Rules[^1].Selector);
        Scoped<Page>(".outer ::deep .inner { padding: 9px; }");
        Assert.Equal(Length.Px(9), engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        Assert.Equal(".outer ::deep .inner", engine.Trace(inner).Rules[^1].Selector);
    }

    [Fact]
    public void Deep_rematches_states_attributes_media_and_hot_reload()
    {
        Scoped<Page>("@media (min-width: 700px) { .outer:hover ::deep .inner { padding: 7px; } }");
        var (root, outer, inner, _) = RenderPage();
        Assert.Equal(Length.Zero, engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        outer.SetState(ElementStates.Hover, true);
        Assert.Equal(Length.Px(7), engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        engine.Viewport = new(600, 400);
        Assert.Equal(Length.Zero, engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        engine.Viewport = new(800, 400);
        var page = (Page)root.Component;
        page.OuterClass = "other";
        page.Refresh();
        host.RunPending();
        Assert.Equal(Length.Zero, engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
        Scoped<Page>(".other ::deep .inner { padding: 3px; }");
        Assert.Equal(Length.Px(3), engine.GetStyle(inner).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Scoped_rules_apply_to_elements_declared_by_the_component_including_projected_content()
    {
        Scoped<Page>(".inner { padding: 4px; } #main { padding: 2px; }");
        Scoped<Card>(".inner { margin: 1px; }");
        var (_, outer, cardInner, projected) = RenderPage();

        Assert.Equal(Length.Px(2), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(4), engine.GetStyle(projected).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Zero, engine.GetStyle(cardInner).Get(StyleProperties.PaddingTop));
        Assert.Equal(SizeValue.Fixed(Length.Px(1)), engine.GetStyle(cardInner).Get(StyleProperties.MarginTop));
        Assert.Equal(SizeValue.Fixed(Length.Zero), engine.GetStyle(projected).Get(StyleProperties.MarginTop));
    }

    [Fact]
    public void Sibling_components_with_the_same_class_names_stay_isolated()
    {
        Scoped<Card>(".inner { padding: 1px; }");
        Scoped<Badge>(".inner { padding: 2px; }");
        Scoped<Siblings>(".inner { padding: 9px; }");
        var root = host.Render<Siblings>();
        var boxes = root.Elements("Box").ToArray();

        Assert.Equal(typeof(Card), boxes[0].Scope);
        Assert.Equal(typeof(Badge), boxes[1].Scope);
        Assert.Equal(Length.Px(1), engine.GetStyle(boxes[0]).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(2), engine.GetStyle(boxes[1]).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Ancestors_of_a_scoped_subject_may_belong_to_other_components()
    {
        Scoped<Card>(".dark .inner { color: white; }");
        engine.Application = Sheet("");
        var (root, outer, cardInner, _) = RenderPage();
        Assert.Equal(Color.Black, engine.GetStyle(cardInner).Color);

        var page = (Page)root.Component;
        host.Dispatcher.InvokeAsync(() => { page.OuterClass = "outer dark"; page.Refresh(); });

        Assert.Equal(Color.White, engine.GetStyle(cardInner).Color);
        Assert.Equal("outer dark", outer.Class);
    }

    [Fact]
    public void Levels_win_over_specificity()
    {
        engine.Theme = Sheet("#main.outer { padding: 1px; }");
        engine.Application = Sheet("#main { padding: 2px; }");
        engine.Window = Sheet(".outer { padding: 3px; }");
        var (_, outer, _, _) = RenderPage();
        Assert.Equal(Length.Px(3), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));

        Scoped<Page>("Box { padding: 4px; }");
        engine.Window = Sheet(".outer { padding: 5px; }");
        Assert.Equal(Length.Px(4), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Specificity_then_source_order_decide_within_a_level()
    {
        engine.Application = Sheet("#main { padding: 9px; } .outer { padding: 1px; } Box { padding: 2px; } .outer { padding: 3px; }");
        var (_, outer, _, _) = RenderPage();
        Assert.Equal(Length.Px(9), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));

        engine.Application = Sheet(".outer { padding: 1px; } Box { padding: 2px; } .outer { padding: 3px; }");
        Assert.Equal(Length.Px(3), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Inherited_properties_flow_down_and_others_do_not()
    {
        engine.Application = Sheet(".outer { color: #ff0000; padding: 8px; font-size: 20px; } .inner { border: 2px solid; } .projected { padding: inherit; color: initial; }");
        var (_, outer, cardInner, projected) = RenderPage();

        Assert.Equal(Color.FromRgb(255, 0, 0), engine.GetStyle(cardInner).Color);
        Assert.Equal(Length.Px(20), engine.GetStyle(cardInner).Get(StyleProperties.FontSize));
        Assert.Equal(Length.Zero, engine.GetStyle(cardInner).Get(StyleProperties.PaddingTop));
        Assert.Equal(Color.FromRgb(255, 0, 0), engine.GetStyle(cardInner).Get(StyleProperties.BorderTopColor));
        Assert.Equal(Length.Zero, engine.GetStyle(projected).Get(StyleProperties.PaddingTop));
        Assert.Equal(Color.Black, engine.GetStyle(projected).Color);
        Assert.Equal(Length.Px(8), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
    }

    [Fact]
    public void Custom_properties_are_inherited_substituted_and_replaceable_for_themes()
    {
        engine.Window = Sheet("Box { background: var(--surface, #ffffff); padding: var(--space) 1px; }");
        engine.Application = Sheet("#main { --surface: #202020; --space: 6px; }");
        var (_, outer, cardInner, _) = RenderPage();

        Assert.Equal(Color.FromRgb(0x20, 0x20, 0x20), engine.GetStyle(cardInner).BackgroundColor);
        Assert.Equal(Length.Px(6), engine.GetStyle(cardInner).Get(StyleProperties.PaddingTop));
        Assert.Equal(Length.Px(1), engine.GetStyle(cardInner).Get(StyleProperties.PaddingRight));

        var changes = new List<(ElementNode, StyleEffects)>();
        engine.StyleChanged += (element, effects) => changes.Add((element, effects));
        engine.Application = Sheet("#main { --surface: #fafafa; --space: 6px; }");
        engine.Update();

        Assert.Equal(Color.FromRgb(0xfa, 0xfa, 0xfa), engine.GetStyle(cardInner).BackgroundColor);
        Assert.Contains((cardInner, StyleEffects.Paint), changes);
        Assert.Contains((outer, StyleEffects.Paint), changes);
    }

    [Fact]
    public void Background_positions_with_math_functions_survive_variable_substitution()
    {
        engine.Application = Sheet("""
            #main { --inset: 2px; --edge: calc(100% - 2px); --middle: 50%; }
            .outer { background: url(a.png) calc(100% - var(--inset)) var(--middle) / 4px 4px no-repeat #00f; }
            .inner { background: url(a.png) var(--edge) min(var(--middle), 10px) no-repeat; }
            .projected { background-position: var(--edge) max(var(--inset), 1%); }
            """);
        var (_, outer, cardInner, projected) = RenderPage();

        Assert.Equal("calc(100% - 2px) 50%", engine.GetStyle(outer).Get(StyleProperties.BackgroundPosition).ToString());
        Assert.Equal(Color.FromRgb(0, 0, 255), engine.GetStyle(outer).BackgroundColor);
        Assert.Equal("calc(100% - 2px) min(50%, 10px)", engine.GetStyle(cardInner).Get(StyleProperties.BackgroundPosition).ToString());
        Assert.Equal("calc(100% - 2px) max(2px, 1%)", engine.GetStyle(projected).Get(StyleProperties.BackgroundPosition).ToString());
    }

    [Fact]
    public void Missing_or_cyclic_variables_use_the_fallback_or_become_unset()
    {
        engine.Application = Sheet("""
            #main { --a: var(--b); --b: var(--a); --ok: 3px; }
            .outer { background: var(--a, #00ff00); padding: var(--missing); margin: var(--ok); color: var(--a); }
            """);
        var (_, outer, _, _) = RenderPage();
        var style = engine.GetStyle(outer);

        Assert.Equal(Color.FromRgb(0, 255, 0), style.BackgroundColor);
        Assert.Equal(Length.Zero, style.Get(StyleProperties.PaddingTop));
        Assert.Equal(SizeValue.Fixed(Length.Px(3)), style.Get(StyleProperties.MarginTop));
        Assert.Equal(Color.Black, style.Color);
        Assert.False(style.CustomProperties.ContainsKey("--a"));
    }

    [Fact]
    public void Media_queries_follow_the_viewport()
    {
        engine.Application = Sheet(".outer { flex-direction: row; @media (max-width: 720px) { flex-direction: column; } }");
        var (_, outer, _, _) = RenderPage();
        engine.Viewport = new Viewport(1024, 768);
        Assert.Equal(FlexDirection.Row, engine.GetStyle(outer).Get(StyleProperties.FlexDirection));

        var changes = new List<StyleEffects>();
        engine.StyleChanged += (_, effects) => changes.Add(effects);
        engine.Viewport = new Viewport(700, 768);
        Assert.Equal(FlexDirection.Column, engine.GetStyle(outer).Get(StyleProperties.FlexDirection));
        Assert.Equal([StyleEffects.Layout], changes);

        engine.Viewport = new Viewport(650, 768);
        Assert.False(engine.IsDirty);
    }

    [Fact]
    public void Media_queries_follow_the_color_scheme_and_reduced_motion()
    {
        engine.Application = Sheet("""
            .outer { background: #ffffff; transition: opacity 200ms; }
            @media (prefers-color-scheme: dark) { .outer { background: #202020; } }
            @media (prefers-reduced-motion) { .outer { transition: none; } }
            """);
        var (_, outer, _, _) = RenderPage();
        Assert.Equal(Color.White, engine.GetStyle(outer).BackgroundColor);
        Assert.Single(engine.GetStyle(outer).Get(StyleProperties.Transition).Items);

        engine.Preferences = new MediaPreferences(ColorScheme.Dark, ReducedMotion: false);
        Assert.True(engine.IsDirty);
        Assert.Equal(Color.FromRgb(0x20, 0x20, 0x20), engine.GetStyle(outer).BackgroundColor);
        engine.Preferences = new MediaPreferences(ColorScheme.Dark, ReducedMotion: true);
        Assert.Empty(engine.GetStyle(outer).Get(StyleProperties.Transition).Items);

        engine.Application = Sheet(".outer { background: #ffffff; }");
        engine.Update();
        engine.Preferences = default;
        Assert.True(engine.IsDirty);
        engine.Update();
        engine.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        Assert.False(engine.IsDirty);
    }

    [Fact]
    public void Interaction_states_select_pseudo_class_rules_on_the_element_and_its_descendants()
    {
        engine.Application = Sheet(".outer:hover { background: #0078d4; } .outer:hover .inner { color: #ffffff; } .inner:active:focus { opacity: 0.5; }");
        var (_, outer, cardInner, _) = RenderPage();
        Assert.Equal(Color.Transparent, engine.GetStyle(outer).BackgroundColor);

        outer.SetState(ElementStates.Hover, true);
        Assert.True(engine.IsDirty);
        Assert.Equal(Color.FromRgb(0, 0x78, 0xd4), engine.GetStyle(outer).BackgroundColor);
        Assert.Equal(Color.White, engine.GetStyle(cardInner).Color);

        cardInner.SetState(ElementStates.Pressed, true);
        Assert.Equal(1d, engine.GetStyle(cardInner).Opacity);
        cardInner.SetState(ElementStates.Focus, true);
        Assert.Equal(0.5, engine.GetStyle(cardInner).Opacity);

        outer.SetState(ElementStates.Hover, false);
        Assert.Equal(Color.Black, engine.GetStyle(cardInner).Color);
    }

    [Fact]
    public void Style_changes_report_layout_paint_and_composite_effects()
    {
        engine.Application = Sheet(".outer { padding: 1px; }");
        var (_, outer, _, _) = RenderPage();
        engine.Update();
        var changes = new List<StyleEffects>();
        engine.StyleChanged += (element, effects) => { if (element == outer) changes.Add(effects); };

        engine.Application = Sheet(".outer { padding: 2px; }");
        engine.Update();
        engine.Application = Sheet(".outer { padding: 2px; background: red; }");
        engine.Update();
        engine.Application = Sheet(".outer { padding: 2px; background: red; opacity: 0.5; }");
        engine.Update();

        Assert.Equal([StyleEffects.Layout, StyleEffects.Paint, StyleEffects.Composite], changes);
    }
}

[CollectionDefinition("StyleRegistry", DisableParallelization = true)]
public sealed class StyleRegistryCollection;
