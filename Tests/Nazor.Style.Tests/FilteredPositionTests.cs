using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class FilteredPositionTests
{
    [Theory]
    [InlineData("Item:nth-child(2 of .a)", "2")]
    [InlineData("Item:nth-child(odd of .a)", "0")]
    [InlineData("Item:nth-last-child(1 of .a)", "2")]
    [InlineData("Item:nth-last-child(2 of .a, .c)", "2")]
    [InlineData("Item:nth-child(even of Item:not([disabled]))", "1")]
    [InlineData("Item:nth-child(-n+2 of .a, .b)", "0,1")]
    [InlineData("Item:nth-child(1 of .absent)", "")]
    public void Positions_count_only_siblings_matching_the_filter(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var sheet = NssCompiler.Compile("filter", selector + " { opacity: .5; }");
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        engine.Application = sheet.StyleSheet;
        Assert.Equal(expected, string.Join(",", root.Elements("Item").Select((e, i) => (e, i)).Where(p => engine.GetStyle(p.e).Opacity < 1).Select(p => p.i)));
        Assert.Equal(sheet.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet.StyleSheet)).Describe());
        Assert.Contains("Filter =", CSharpEmitter.Emit(sheet.StyleSheet));
    }

    [Theory]
    [InlineData("Item:nth-of-type(2 of .a)")]
    [InlineData("Item:nth-last-of-type(2 of .a)")]
    [InlineData("Item:nth-child(2 of)")]
    [InlineData("Item:nth-child(of .a)")]
    [InlineData("Item:nth-child(2 of .a,)")]
    [InlineData("Item:nth-child(2 of > .a)")]
    [InlineData("Item:nth-child(2 of &)")]
    [InlineData("Item:nth-child(2 of ::before)")]
    [InlineData("Item:nth-child(2 of .a ::deep .b)")]
    public void Invalid_filters_are_diagnosed(string selector) => Assert.False(NssCompiler.Compile("filter", selector + " { color: red; }").Success);

    [Fact]
    public void Specificity_includes_the_greatest_filter_even_when_another_branch_matches()
    {
        var sheet = NssCompiler.Compile("filter", ":nth-child(2 of .a, #id) { opacity: .5; } :nth-child(1 of :where(#id)) { opacity: .5; }").StyleSheet;
        Assert.Equal(new Specificity(1, 1, 0), sheet.Rules[0].Selector.Specificity);
        Assert.Equal(new Specificity(0, 1, 0), sheet.Rules[1].Selector.Specificity);
    }

    [Fact]
    public void Filter_membership_updates_after_attribute_state_insertion_and_removal_changes()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var items = (Items)root.Component;
        engine.Application = NssCompiler.Compile("filter", "Item:nth-child(2 of [data-state]) { opacity: .5; }").StyleSheet;
        var elements = root.Elements("Item").ToArray();
        Assert.Equal(.5, engine.GetStyle(elements[3]).Opacity);
        items.Rows[0] = ("a", "changed", "one");
        items.Refresh();
        host.RunPending();
        Assert.Equal(.5, engine.GetStyle(elements[1]).Opacity);
        Assert.Equal(1, engine.GetStyle(elements[3]).Opacity);
        engine.Application = NssCompiler.Compile("filter", "Item:nth-child(1 of :hover) { opacity: .5; }").StyleSheet;
        engine.Update();
        elements[1].SetState(ElementStates.Hover, true);
        Assert.Equal(.5, engine.GetStyle(elements[1]).Opacity);
        elements[0].SetState(ElementStates.Hover, true);
        Assert.Equal(1, engine.GetStyle(elements[1]).Opacity);
        Assert.Equal(.5, engine.GetStyle(elements[0]).Opacity);
        elements[0].SetState(ElementStates.Hover, false);
        Assert.Equal(.5, engine.GetStyle(elements[1]).Opacity);
        engine.Application = NssCompiler.Compile("filter", "Item:nth-last-child(1 of .a) { opacity: .5; }").StyleSheet;
        items.Rows.Add(("a", null, "last"));
        items.Refresh();
        host.RunPending();
        Assert.Equal(.5, engine.GetStyle(root.Elements("Item").Last()).Opacity);
        items.Rows.RemoveAt(items.Rows.Count - 1);
        items.Refresh();
        host.RunPending();
        Assert.Equal(.5, engine.GetStyle(root.Elements("Item").Last(e => e.Class == "a")).Opacity);
    }

    [Fact]
    public void Scoped_filters_count_declared_siblings_and_deep_filters_reach_foreign_origins()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        StyleRegistry.RegisterScoped(typeof(Page), () => NssCompiler.Compile("filter", ".outer ::deep .inner:nth-child(1 of .inner) { opacity: .5; }").StyleSheet);
        try
        {
            var root = host.Render<Page>();
            Assert.Equal(.5, engine.GetStyle(root.Elements("Box").First(e => e.Class == "inner")).Opacity);
        }
        finally { StyleRegistry.RegisterScoped(typeof(Page), () => StyleSheet.Empty); }
    }

    [Fact]
    public void Filters_preserve_owner_scope_until_an_explicit_deep_combinator()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var sheet = NssCompiler.Compile("filter", ".inner:nth-child(1 of .inner) { opacity: .5; }").StyleSheet;
        StyleRegistry.RegisterScoped(typeof(FilteredScopeFixture), () => sheet);
        sheet = StyleRegistry.GetScoped(typeof(FilteredScopeFixture))!;
        try
        {
            var root = host.Render<FilteredScopeFixture>();
            var foreign = root.Elements("Box").Single(e => e.Class == "inner");
            var own = root.Elements("Box").Single(e => e.Class == "inner own");
            Assert.Equal(1, engine.GetStyle(foreign).Opacity);
            Assert.Equal(.5, engine.GetStyle(own).Opacity);
            sheet.Replace(NssCompiler.Compile("filter", ".outer ::deep .inner:nth-child(1 of .inner) { opacity: .5; }").StyleSheet);
            Assert.Equal(.5, engine.GetStyle(foreign).Opacity);
            Assert.Equal(1, engine.GetStyle(own).Opacity);
        }
        finally { StyleRegistry.RegisterScoped(typeof(FilteredScopeFixture), () => StyleSheet.Empty); }
    }

    [Fact]
    public void Empty_text_filter_membership_updates_when_text_changes_without_child_insertion()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Items>();
        var items = (Items)root.Component;
        engine.Application = NssCompiler.Compile("filter", "Item:nth-child(1 of :empty) { opacity: .5; }").StyleSheet;
        var first = root.Elements("Item").First();
        Assert.Equal(1, engine.GetStyle(first).Opacity);
        items.Rows[0] = ("a", null, "");
        items.Refresh();
        host.RunPending();
        Assert.Equal(.5, engine.GetStyle(first).Opacity);
    }

    [Fact]
    public void Filtered_counting_does_not_allocate_a_sibling_list_or_candidate_closure()
    {
        using var host = new TestHost();
        var root = host.Render<Items>();
        var items = (Items)root.Component;
        items.Rows = Enumerable.Range(0, 100).Select(i => ("row", (string?)null, (string?)i.ToString())).ToList();
        items.Refresh();
        host.RunPending();
        var last = root.Elements("Item").Last();
        var selector = NssCompiler.Compile("filter", "Item:nth-child(100 of .row) { opacity: .5; }").StyleSheet.Rules[0].Selector;
        for (var i = 0; i < 10; i++) Assert.True(selector.Matches(last));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var matches = 0;
        for (var i = 0; i < 1000; i++) if (selector.Matches(last)) matches++;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1000, matches);
        Assert.InRange(allocated, 0, 512_000);
    }
}
