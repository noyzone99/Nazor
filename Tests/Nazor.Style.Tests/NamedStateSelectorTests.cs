using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class NamedStateSelectorTests
{
    [Fact]
    public void State_snapshots_are_immutable_deduplicated_and_case_sensitive()
    {
        var input = new[] { "loading", "selected", "loading" };
        var states = new CustomStateSet(input);
        input[0] = "changed";
        Assert.Equal(["loading", "selected"], states.Names);
        Assert.False(states.Contains("Loading"));
        Assert.Same(states, states.With("loading"));
        Assert.Equal(CustomStateSet.Empty.With("selected"), states.With("loading", false));
        Assert.True(states.Contains("loading"));
        Assert.Equal(states.GetHashCode(), new CustomStateSet(["selected", "loading"]).GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("two states")]
    [InlineData("4state")]
    [InlineData("-state")]
    [InlineData("a.b")]
    public void Invalid_names_are_rejected_by_both_public_contracts(string name)
    {
        Assert.Throws<ArgumentException>(() => new CustomStateSet([name]));
        Assert.Throws<ArgumentException>(() => new NamedStateSelector(name));
    }

    [Theory]
    [InlineData(":state()")]
    [InlineData(":state(a,b)")]
    [InlineData(":state(a b)")]
    [InlineData(":state(\"loading\")")]
    [InlineData(":state(4)")]
    public void Invalid_state_arguments_are_diagnosed(string selector) => Assert.False(NssCompiler.Compile("state", selector + " { opacity: .5; }").Success);

    [Theory]
    [InlineData(":state(loading)", "first")]
    [InlineData(":state(Loading)", "")]
    [InlineData(":not(:state(loading))", "second,leaf")]
    [InlineData("Box:has(> :state(loading))", "")]
    public void States_target_only_explicit_physical_outputs(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<NamedStateOutputs>();
        var result = NssCompiler.Compile("state", selector + " { opacity: .5; }");
        Assert.True(result.Success);
        engine.Application = result.StyleSheet;
        Assert.Equal(expected, string.Join(",", root.Elements().Where(e => engine.GetStyle(e).Opacity < 1).Select(e => e.Id)));
        Assert.Equal(result.StyleSheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)).Describe());
        Assert.Contains("NamedStateSelector", CSharpEmitter.Emit(result.StyleSheet));
        Assert.Equal(new Specificity(0, 1, 0), new NamedStateSelector("loading").Specificity);
    }

    [Theory]
    [InlineData(":state(loading) > Leaf", "leaf")]
    [InlineData("Box:has(> :state(loading))", "second")]
    [InlineData("Box:nth-child(1 of :state(loading))", "second")]
    [InlineData("Box:has(+ :state(loading))", "first")]
    public void Replacement_and_recycling_clear_states_and_refresh_nonlocal_consumers(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<NamedStateOutputs>();
        var fixture = (NamedStateOutputs)root.Component;
        engine.Application = NssCompiler.Compile("state", selector + " { opacity: .5; }").StyleSheet;
        engine.Update();
        fixture.First = CustomStateSet.Empty;
        fixture.Second = CustomStateSet.Empty.With("loading");
        fixture.Child = CustomStateSet.Empty.With("loading");
        fixture.Refresh(); host.RunPending();
        Assert.Equal(expected, string.Join(",", root.Elements().Where(e => engine.GetStyle(e).Opacity < 1).Select(e => e.Id)));
        fixture.Second = fixture.Child = CustomStateSet.Empty;
        fixture.Refresh(); host.RunPending();
        Assert.All(root.Elements(), e => Assert.Equal(1, engine.GetStyle(e).Opacity));
    }
}

public sealed class NamedStateOutputs : ComponentBase
{
    public CustomStateSet First = CustomStateSet.Empty.With("loading");
    public CustomStateSet Second = CustomStateSet.Empty;
    public CustomStateSet Child = CustomStateSet.Empty;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Box"); builder.AddAttribute(1, "id", "first"); builder.AddAttribute(2, "custom-states", First); builder.CloseElement();
        builder.OpenElement(3, "Box"); builder.AddAttribute(4, "id", "second"); builder.AddAttribute(5, "custom-states", Second);
        builder.OpenElement(6, "Leaf"); builder.AddAttribute(7, "id", "leaf"); builder.AddAttribute(8, "custom-states", Child); builder.CloseElement();
        builder.CloseElement();
    }
}
