using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class PublicPartTests
{
    [Theory]
    [InlineData("PartPanel::part(surface)", "surface,other")]
    [InlineData("PartPanel::part(action)", "input")]
    [InlineData("PartInner::part(private-input)", "input")]
    [InlineData("PartPanel::part(private-input)", "")]
    [InlineData("PartPanel::part(unknown)", "")]
    public void Parts_select_existing_explicit_outputs_and_exports(string selector, string expected)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<PartPage>();
        var before = root.Elements().ToArray();
        var sheet = Compile(selector);
        engine.Application = sheet;
        Assert.Equal(expected, string.Join(",", before.Where(e => engine.GetStyle(e).Opacity < 1).Select(e => e.Id)));
        Assert.Equal(before, root.Elements().ToArray());
        Assert.Equal(sheet.Describe(), StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet)).Describe());
        Assert.Contains("PartSelector", CSharpEmitter.Emit(sheet));
        Assert.Equal(new Specificity(0, 0, 2), sheet.Rules[0].Selector.Specificity);
    }

    [Theory]
    [InlineData("::part(input)")]
    [InlineData("PartPanel::part()")]
    [InlineData("PartPanel::part(a,b)")]
    [InlineData("PartPanel::part(\"input\")")]
    [InlineData("PartPanel:hover::part(input)")]
    [InlineData("PartPanel.x::part(input)")]
    [InlineData("PartPanel::part(input).x")]
    [InlineData("PartPanel::part(input)::before")]
    [InlineData("PartPanel::part(input) > Box")]
    [InlineData(":is(PartPanel::part(input))")]
    [InlineData(":has(PartPanel::part(input))")]
    [InlineData("PartPanel::part(input):nth-child(1)")]
    public void Unsupported_part_forms_are_diagnosed(string selector) => Assert.False(NssCompiler.Compile("parts", selector + " { opacity: .5; }").Success);

    [Fact]
    public void Scoped_parts_require_public_export_at_each_private_boundary_and_preserve_projection_ownership()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var sheet = Compile("PartInner::part(private-input)");
        StyleRegistry.RegisterScoped(typeof(PartPage), () => sheet);
        sheet = StyleRegistry.GetScoped(typeof(PartPage))!;
        try
        {
            var root = host.Render<PartPage>();
            var input = root.Single("Input");
            Assert.Equal(1, engine.GetStyle(input).Opacity);
            sheet.Replace(Compile("PartPanel::part(action)"));
            Assert.Equal(.5, engine.GetStyle(input).Opacity);
            Assert.Equal(1, engine.GetStyle(root.Single("Projected")).Opacity);
            input.SetState(ElementStates.Focus, true);
            sheet.Replace(Compile(".page PartPanel::part(action):focus"));
            Assert.Equal(.5, engine.GetStyle(input).Opacity);
            sheet.Replace(Compile(".page ::deep PartInner::part(private-input):focus"));
            Assert.Equal(.5, engine.GetStyle(input).Opacity);
        }
        finally { StyleRegistry.RegisterScoped(typeof(PartPage), () => StyleSheet.Empty); }
    }

    [Fact]
    public void State_and_export_replacement_restyle_the_same_physical_target()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<PartPanel>();
        var panel = (PartPanel)root.Component;
        var input = root.Single("Input");
        engine.Application = Compile("PartPanel::part(action):hover");
        Assert.Equal(1, engine.GetStyle(input).Opacity);
        input.SetState(ElementStates.Hover, true);
        Assert.Equal(.5, engine.GetStyle(input).Opacity);
        panel.Export = false; panel.Refresh(); host.RunPending();
        Assert.Same(input, root.Single("Input"));
        Assert.Equal(1, engine.GetStyle(input).Opacity);
        panel.Export = true; panel.Refresh(); host.RunPending();
        Assert.Equal(.5, engine.GetStyle(input).Opacity);
        panel.Visible = false; panel.Refresh(); host.RunPending();
        Assert.Empty(root.Elements());
    }

    private static StyleSheet Compile(string selector)
    {
        var result = NssCompiler.Compile("parts", selector + " { opacity: .5; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }
}

public sealed class PartPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(PartPage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<PartPanel>(0, typeof(PartPage));
            content.AddComponentParameter(1, "ChildContent", (RenderFragment)(child => { child.OpenElement(0, "Projected"); child.AddAttribute(1, "id", "projection"); child.CloseElement(); }));
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class PartPanel : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    public bool Export = true, Visible = true;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Visible) return;
        foreach (var id in new[] { "surface", "other" })
        {
            builder.OpenElement(0, "Box"); builder.AddAttribute(1, "id", id); builder.AddAttribute(2, "style-parts", StylePartSet.Empty.Expose(this, "surface")); builder.CloseElement();
        }
        builder.OpenComponent<PartInner>(3, typeof(PartPanel));
        builder.AddComponentParameter(4, "Exports", Export ? StylePartSet.Empty.Expose(this, "action") : StylePartSet.Empty);
        builder.CloseComponent(); builder.AddContent(5, ChildContent);
    }
}

public sealed class PartInner : ComponentBase
{
    [Parameter] public StylePartSet Exports { get; set; } = StylePartSet.Empty;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var parts = StylePartSet.Empty.Expose(this, "private-input").Export(this, "private-input", Exports);
        builder.OpenElement(0, "Input"); builder.AddAttribute(1, "id", "input"); builder.AddAttribute(2, "style-parts", parts); builder.CloseElement();
    }
}
