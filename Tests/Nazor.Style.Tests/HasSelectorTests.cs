using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class HasSelectorTests(Xunit.ITestOutputHelper output)
{
    [Theory]
    [InlineData("Item:has(+ .b)", "0")]
    [InlineData("Item:has(~ .c)", "0,1,2")]
    [InlineData("Item:has(+ .b + .a)", "0")]
    [InlineData("Item:has(+ .b ~ .c)", "0")]
    [InlineData("Item:has(+ .absent, + Footer)", "3")]
    [InlineData("Item:not(:has(+ Item))", "3")]
    [InlineData("Item:nth-child(2 of :has(~ .c))", "1")]
    public void Relative_lists_anchor_the_leftmost_compound(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var sheet = Compile(selector);
        engine.Application = sheet;
        Assert.Equal(expected, string.Join(",", root.Elements("Item").Select((e, i) => (e, i)).Where(p => engine.GetStyle(p.e).Opacity < 1).Select(p => p.i)));
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        Assert.Contains("RelativeSelector", CSharpEmitter.Emit(sheet));
    }

    [Theory]
    [InlineData(":has()")]
    [InlineData(":has(.a,)")]
    [InlineData(":has(,.a)")]
    [InlineData(":has(:has(.a))")]
    [InlineData(":has(:is(:has(.a)))")]
    [InlineData(":has(:nth-child(1 of :has(.a)))")]
    [InlineData(":has(::before)")]
    [InlineData(":has(.a ::deep .b)")]
    [InlineData(":has(::deep .b)")]
    [InlineData(":has(&)")]
    [InlineData(":has(>)")]
    public void Unsupported_arguments_are_diagnosed(string selector) => Assert.False(NssCompiler.Compile("has", selector + " { opacity: .5; }").Success);

    [Fact]
    public void Specificity_uses_the_greatest_argument_without_a_pseudo_class_unit()
    {
        Assert.Equal(new Specificity(1, 1, 0), Compile(".card:has(> #id, .leaf)").Rules[0].Selector.Specificity);
        Assert.Equal(new Specificity(0, 1, 0), Compile(".card:has(:where(#id))").Rules[0].Selector.Specificity);
    }

    [Theory]
    [InlineData("List:has(Item[data-state=done])")]
    [InlineData("List:has(> Item[data-state=done])")]
    public void Descendant_origins_update_after_attribute_and_removal_changes(string selector)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var items = (Items)root.Component;
        engine.Application = Compile(selector);
        var list = root.Single("List");
        Assert.Equal(.5, engine.GetStyle(list).Opacity);
        items.Rows[1] = ("b", "todo", "two");
        items.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(list).Opacity);
        items.Rows.Add(("new", "done", "new"));
        items.Refresh(); host.RunPending();
        Assert.Equal(.5, engine.GetStyle(list).Opacity);
        items.Rows.RemoveAt(items.Rows.Count - 1);
        items.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(list).Opacity);
    }

    [Theory]
    [InlineData("Item:has(+ Item:hover)", "0")]
    [InlineData("Item:has(~ Item:hover)", "0")]
    [InlineData("Item:has(+ Item:hover) + Item", "1")]
    [InlineData("Item:nth-child(1 of :has(+ Item:hover))", "0")]
    [InlineData("Item:is(:has(+ Item:hover))", "0")]
    public void Following_sibling_state_changes_refresh_origins_and_dependents(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        engine.Application = Compile(selector);
        var elements = root.Elements("Item").ToArray();
        engine.Update();
        elements[1].SetState(ElementStates.Hover, true);
        Assert.Equal(expected, string.Join(",", elements.Select((e, i) => (e, i)).Where(p => engine.GetStyle(p.e).Opacity < 1).Select(p => p.i)));
        elements[1].SetState(ElementStates.Hover, false);
        Assert.All(elements, e => Assert.Equal(1, engine.GetStyle(e).Opacity));
    }

    [Fact]
    public void Sibling_removal_and_text_changes_refresh_the_previous_origin()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var items = (Items)root.Component;
        engine.Application = Compile("Item:has(+ Item:empty)");
        var first = root.Elements("Item").First();
        Assert.Equal(1, engine.GetStyle(first).Opacity);
        items.Rows[1] = ("b", "done", ""); items.Refresh(); host.RunPending();
        Assert.Equal(.5, engine.GetStyle(first).Opacity);
        items.Rows.RemoveAt(1); items.Refresh(); host.RunPending();
        Assert.Equal(.5, engine.GetStyle(first).Opacity);
        items.Rows[1] = ("a", null, "filled"); items.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(first).Opacity);
    }

    [Fact]
    public void Scoped_has_preserves_ownership_and_explicit_component_qualifiers()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var sheet = Compile(".outer:has(.inner)");
        StyleRegistry.RegisterScoped(typeof(FilteredScopeFixture), () => sheet);
        sheet = StyleRegistry.GetScoped(typeof(FilteredScopeFixture))!;
        try
        {
            var root = host.Render<FilteredScopeFixture>();
            var outer = root.Elements("Box").Single(e => e.Class == "outer");
            Assert.Equal(.5, engine.GetStyle(outer).Opacity);
            sheet.Replace(Compile(".outer:has(.inner:not(.own))"));
            Assert.Equal(1, engine.GetStyle(outer).Opacity);
            sheet.Replace(Compile(".outer:has(Badge .inner)"));
            Assert.Equal(.5, engine.GetStyle(outer).Opacity);
        }
        finally { StyleRegistry.RegisterScoped(typeof(FilteredScopeFixture), () => StyleSheet.Empty); }
    }

    [Fact]
    public void Simple_child_state_invalidation_is_bounded_and_idle_updates_do_no_work()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<HasCards>();
        engine.Application = Compile(".card:has(> .leaf:hover)");
        engine.Update();
        var leaf = root.Elements("Leaf").Skip(500).First();
        var before = GC.GetAllocatedBytesForCurrentThread();
        leaf.SetState(ElementStates.Hover, true);
        engine.Update();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(engine.LastRecomputedCount, 2, 3);
        Assert.Equal(.5, engine.GetStyle(leaf.ParentElement!).Opacity);
        Assert.InRange(allocated, 0, 128_000);
        engine.Update();
        Assert.Equal(0, engine.LastRecomputedCount);
    }

    private static StyleSheet Compile(string selector)
    {
        var result = NssCompiler.Compile("has", selector + " { opacity: .5; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Json_rejects_deep_and_nested_relational_arguments()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(StyleSheetJson.Serialize(Compile(".card:has(.leaf)")))!;
        var selector = json["rules"]![0]!["selector"]!;
        var relative = selector["compounds"]![0]!["pseudos"]![0]!["selectors"]![0]!;
        relative["relation"] = "Deep";
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(json.ToJsonString()));
        relative["relation"] = "Descendant";
        relative["selector"] = selector.DeepClone();
        Assert.Throws<InvalidDataException>(() => StyleSheetJson.Deserialize(json.ToJsonString()));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void Broad_following_sibling_probe_records_the_expected_larger_search(int count)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<HasCards>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = count }));
        engine.Application = Compile(".card:has(~ .card:hover)");
        engine.Update();
        var card = root.Elements("Card").Last();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        card.SetState(ElementStates.Hover, true);
        engine.Update();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        output.WriteLine($"cards={count}; broad-sibling-input; us={System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMicroseconds:F2}; bytes={allocated}; recomputed={engine.LastRecomputedCount}");
        Assert.Equal(2 * count, engine.LastRecomputedCount);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Relational_cost_probe_reports_matching_idle_and_input_work(int count)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<HasCards>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = count }));
        var sheet = Compile(".card:has(> .leaf:hover)");
        engine.Application = sheet;
        engine.Update();
        var leaf = root.Elements("Leaf").Skip(count / 2).First();
        for (var i = 0; i < 10; i++) { leaf.SetState(ElementStates.Hover, (i & 1) == 0); engine.Update(); }
        Measure("match", () => sheet.Rules[0].Selector.Matches(leaf.ParentElement!));
        Measure("idle", () => engine.Update());
        Assert.Equal(0, engine.LastRecomputedCount);
        var active = false;
        Measure("input", () => { active = !active; leaf.SetState(ElementStates.Hover, active); engine.Update(); });
        Assert.InRange(engine.LastRecomputedCount, 2, 3);

        void Measure(string operation, Action action)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++) action();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            output.WriteLine($"cards={count}; {operation}; us/op={System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMicroseconds / 100:F2}; bytes/op={allocated / 100}; recomputed={engine.LastRecomputedCount}");
        }
    }
}

public sealed class HasCards : ComponentBase
{
    [Parameter] public int Count { get; set; } = 1000;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Cards");
        for (var i = 0; i < Count; i++)
        {
            builder.OpenElement(1, "Card"); builder.AddAttribute(2, "class", "card");
            builder.OpenElement(3, "Leaf"); builder.AddAttribute(4, "class", "leaf"); builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
