using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class LogicalPositionAndRootTests
{
    [Theory]
    [InlineData(":nth-item(even)", "b")]
    [InlineData(":nth-item(-n+2)", "a,b")]
    [InlineData(":nth-item(3n+1)", "a")]
    [InlineData(":first-item", "a")]
    [InlineData(":last-item", "")]
    public void Logical_positions_ignore_missing_realization_and_spacers(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<LogicalRows>();
        var sheet = Compile(selector);
        engine.Application = sheet;
        Assert.Equal(expected, string.Join(",", root.Elements().Where(e => engine.GetStyle(e).Opacity < 1).Select(e => e.Id)));
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        Assert.Contains("ItemPositionSelector", CSharpEmitter.Emit(sheet));
        Assert.Equal(new Specificity(0, 1, 0), sheet.Rules[0].Selector.Specificity);
    }

    [Fact]
    public void Recycled_targets_replace_logical_positions_and_refresh_relational_dependencies()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<LogicalRows>();
        var fixture = (LogicalRows)root.Component;
        engine.Application = Compile("Rows:has(> :last-item)");
        var rows = root.Single("Rows");
        Assert.Equal(1, engine.GetStyle(rows).Opacity);
        fixture.First = 39; fixture.Last = 40; fixture.Refresh(); host.RunPending();
        Assert.Equal(.5, engine.GetStyle(rows).Opacity);
        fixture.Publish = false; fixture.Refresh(); host.RunPending();
        Assert.Equal(1, engine.GetStyle(rows).Opacity);
        Assert.False(new ItemPositionSelector(1, 0, false, ":nth-item(n)").Matches(root.Single("Item")));
    }

    [Fact]
    public void Position_contract_rejects_incomplete_or_out_of_range_metadata()
    {
        Assert.Throws<ArgumentNullException>(() => new LogicalItemPosition(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalItemPosition(this, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalItemPosition(this, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalItemPosition(this, 1, 0));
        Assert.False(NssCompiler.Compile("positions", ":nth-item(2 of .item) { opacity: .5; }").Success);
        Assert.False(NssCompiler.Compile("positions", ":nth-item(2147483648n+1) { opacity: .5; }").Success);
        Assert.False(NssCompiler.Compile("positions", ":nth-child(2147483648) { opacity: .5; }").Success);
    }

    [Fact]
    public void Component_roots_are_scoped_instance_outputs_with_no_implicit_host()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var sheet = Compile(":component-root");
        StyleRegistry.RegisterScoped(typeof(ComponentRoots), () => sheet);
        try
        {
            var root = host.Render<ComponentRoots>();
            Assert.Equal("first,second", string.Join(",", root.Elements().Where(e => engine.GetStyle(e).Opacity < 1).Select(e => e.Class)));
            Assert.False(new ComponentRootSelector().Matches(root.Elements().First()));
            Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
            Assert.Contains("ComponentRootSelector", CSharpEmitter.Emit(sheet));
            var fixture = (ComponentRoots)root.Component;
            fixture.Visible = false; fixture.Refresh(); host.RunPending();
            Assert.Empty(root.Elements());
        }
        finally { StyleRegistry.RegisterScoped(typeof(ComponentRoots), () => StyleSheet.Empty); }
    }

    private static StyleSheet Compile(string selector)
    {
        var result = NssCompiler.Compile("positions", selector + " { opacity: .5; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }
}

public sealed class LogicalRows : ComponentBase
{
    public int First = 1, Last = 2;
    public bool Publish = true;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Rows");
        builder.OpenElement(1, "Spacer"); builder.CloseElement();
        for (var i = 0; i < 2; i++)
        {
            builder.OpenElement(2, i == 0 ? "Item" : "Other"); builder.AddAttribute(3, "id", i == 0 ? "a" : "b");
            if (Publish) builder.AddAttribute(4, "item-position", new LogicalItemPosition(this, i == 0 ? First : Last, 40));
            builder.CloseElement();
        }
        builder.OpenElement(5, "Spacer"); builder.AddAttribute(6, "item-position", default(LogicalItemPosition)); builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class ComponentRoots : ComponentBase
{
    public bool Visible = true;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Visible) return;
        builder.OpenComponent<Box>(0, typeof(ComponentRoots)); builder.AddComponentParameter(1, "Class", "first");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(child =>
        {
            child.OpenComponent<Box>(0, typeof(ComponentRoots)); child.AddComponentParameter(1, "Class", "nested"); child.CloseComponent();
        })); builder.CloseComponent();
        builder.OpenComponent<Box>(3, typeof(ComponentRoots)); builder.AddComponentParameter(4, "Class", "second"); builder.CloseComponent();
        builder.OpenComponent<RootInner>(5, typeof(ComponentRoots));
        builder.AddComponentParameter(6, "ChildContent", (RenderFragment)(child =>
        {
            child.OpenComponent<Box>(0, typeof(ComponentRoots)); child.AddComponentParameter(1, "Class", "projected"); child.CloseComponent();
        })); builder.CloseComponent();
    }
}

public sealed class RootInner : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(RootInner)); builder.AddComponentParameter(1, "Class", "foreign"); builder.AddComponentParameter(2, "ChildContent", ChildContent); builder.CloseComponent();
    }
}
