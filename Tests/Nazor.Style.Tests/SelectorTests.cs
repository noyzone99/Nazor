using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class Items : ComponentBase
{
    public List<(string Class, string? State, string? Text)> Rows { get; set; } =
        [("a", null, "uno"), ("b", "done", "due"), ("a", null, null), ("c", "todo", "quattro")];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "List");
        builder.AddAttribute(1, "class", "list");
        for (var i = 0; i < Rows.Count; i++)
        {
            var (name, state, text) = Rows[i];
            builder.OpenElement(2, "Item");
            builder.SetKey(i + ":" + name);
            builder.AddAttribute(3, "class", name);
            builder.AddAttribute(4, "data-state", state);
            builder.AddAttribute(5, "disabled", state == "todo");
            if (text is not null) builder.AddContent(6, text);
            builder.CloseElement();
        }
        builder.OpenElement(7, "Footer");
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class SelectorTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;
    private readonly Items items;

    public SelectorTests()
    {
        engine = new StyleEngine(host.Renderer);
        root = host.Render<Items>();
        items = (Items)root.Component;
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
    }

    private string Matched()
    {
        var all = root.Elements().Where(e => e.Name is "Item" or "Footer").ToList();
        return string.Join(",", all.Select((e, i) => (e, i)).Where(p => engine.GetStyle(p.e).Opacity < 1).Select(p => p.i));
    }

    [Theory]
    [InlineData("Item:first-child", "0")]
    [InlineData("Item:last-child", "")]
    [InlineData(":last-child", "4")]
    [InlineData("Item:last-of-type", "3")]
    [InlineData("Item:nth-child(2n+1)", "0,2")]
    [InlineData("Item:nth-child(odd)", "0,2")]
    [InlineData("Item:nth-child(even)", "1,3")]
    [InlineData("Item:nth-child(-n+2)", "0,1")]
    [InlineData("Item:nth-last-child(2)", "3")]
    [InlineData("Item:nth-of-type(3)", "2")]
    [InlineData(".a + Item", "1,3")]
    [InlineData(".a ~ .c", "3")]
    [InlineData(".b + .a + .c", "3")]
    [InlineData("Item:empty", "2")]
    [InlineData("Footer:empty", "4")]
    [InlineData("Item:not(.a)", "1,3")]
    [InlineData("Item:not(.a, [data-state])", "")]
    [InlineData(":is(.b, .c)", "1,3")]
    [InlineData(":where(.list > .a:first-child)", "0")]
    [InlineData("[data-state]", "1,3")]
    [InlineData("[data-state=done]", "1")]
    [InlineData("[data-state^=to]", "3")]
    [InlineData("[data-state$=\"ne\"]", "1")]
    [InlineData("[data-state*=od]", "3")]
    [InlineData("[DATA-STATE=DONE i]", "1")]
    [InlineData("[disabled]", "3")]
    [InlineData("Item[class~=a]", "0,2")]
    [InlineData("Item[class~='A' i]", "0,2")]
    [InlineData("Item[class~='']", "")]
    [InlineData("Item[data-state|='done']", "1")]
    [InlineData("Item[data-state|='do']", "")]
    public void Selectors_match_by_position_siblings_content_and_attributes(string selector, string expected)
    {
        Use(selector + " { opacity: 0.5; }");
        Assert.Equal(expected, Matched());
    }

    [Fact]
    public void Root_matches_the_top_element_and_specificity_follows_selectors_4()
    {
        Use(":root { opacity: 0.5; }");
        Assert.Equal(0.5, engine.GetStyle(root.Single("List")).Opacity);

        var where = NssCompiler.Compile("t", ":where(#x, .y) .a { color: red; }").StyleSheet.Rules[0].Selector.Specificity;
        var isSelector = NssCompiler.Compile("t", ":is(#x, .y) .a { color: red; }").StyleSheet.Rules[0].Selector.Specificity;
        var structural = NssCompiler.Compile("t", "Item:nth-child(2)[data-state] { color: red; }").StyleSheet.Rules[0].Selector.Specificity;
        Assert.Equal(new Specificity(0, 1, 0), where);
        Assert.Equal(new Specificity(1, 1, 0), isSelector);
        Assert.Equal(new Specificity(0, 2, 1), structural);
    }

    [Fact]
    public void Adding_and_removing_items_restyles_their_siblings()
    {
        Use("Item:last-of-type { opacity: 0.5; }");
        Assert.Equal("3", Matched());
        items.Rows.Add(("d", null, "cinque"));
        items.Refresh();
        host.RunPending();
        Assert.Equal("4", Matched());
        items.Rows.RemoveAt(4);
        items.Rows.RemoveAt(3);
        items.Refresh();
        host.RunPending();
        Assert.Equal("2", Matched());
    }

    [Fact]
    public void Focus_within_follows_the_focus()
    {
        Use(".list:focus-within { opacity: 0.5; }");
        var list = root.Single("List");
        Assert.Equal(1, engine.GetStyle(list).Opacity);
        list.SetState(ElementStates.FocusWithin, true);
        Assert.Equal(0.5, engine.GetStyle(list).Opacity);
    }

    [Theory]
    [InlineData("Item:nth-child(foo) { color: red; }", "Expected odd, even or An+B")]
    [InlineData("Item:matches(.a) { color: red; }", "Pseudo-class ':matches' is not supported")]
    [InlineData("Item[data-state=] { color: red; }", "Expected a value")]
    [InlineData("Item[data-state==x] { color: red; }", "Expected a value")]
    [InlineData("Item[ { color: red; }", "Missing ']'")]
    [InlineData(".a + { color: red; }", "cannot end with a combinator")]
    [InlineData(":is(& .a) { color: red; }", "inside :is()")]
    public void Invalid_selectors_are_reported(string nss, string message) =>
        Assert.Contains(NssCompiler.Compile("t", nss, "t.nss").Diagnostics, d => d.Message.Contains(message, StringComparison.Ordinal));

    [Fact]
    public void Selectors_round_trip_through_json()
    {
        var sheet = NssCompiler.Compile("t", ".a + .b ~ Item:not(:first-child, [x|=y i]):nth-last-of-type(2n-1):empty:root:focus-within > .c { color: red; }").StyleSheet;
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
    }
}
