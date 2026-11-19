using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class RootSelectorTests
{
    [Theory]
    [InlineData("Window { color: red; }")]
    [InlineData("Window:hover { color: red; }")]
    [InlineData("Window > Button { color: red; }")]
    [InlineData(".a { Window { color: red; } }")]
    [InlineData(":is(Window, .a) { color: red; }")]
    [InlineData(":where(.a, Window) { color: red; }")]
    [InlineData(":not(Window) { color: red; }")]
    public void Legacy_window_type_selectors_have_an_actionable_error(string source)
    {
        var result = NssCompiler.Compile("legacy", source);
        var error = Assert.Single(result.Diagnostics, d => d.Id == "NZS109");
        Assert.False(result.Success);
        Assert.Equal(6, error.Length);
        Assert.Contains("Use :root", error.Message);
    }

    [Fact]
    public void Class_id_attribute_and_string_names_are_not_type_selectors()
    {
        var result = NssCompiler.Compile("names", ".Window, #Window, [name=Window], [name='Window'] { --label: 'Window'; color: red; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(4, result.StyleSheet.Rules.Count);
    }

    [Fact]
    public void Root_preserves_pseudo_class_specificity_cascade_nesting_and_serialization()
    {
        const string source = "Button { color: blue; } :root { color: red; > Button { color: green; } } .Window { color: blue; } #main { color: white; }";
        var result = NssCompiler.Compile("root", source);
        Assert.True(result.Success);
        Assert.Equal(new Specificity(0, 1, 0), result.StyleSheet.Rules[1].Selector.Specificity);
        Assert.Equal(new Specificity(0, 1, 1), result.StyleSheet.Rules[2].Selector.Specificity);
        Assert.Contains("RootSelector", CSharpEmitter.Emit(result.StyleSheet));
        using var host = new TestHost();
        var root = host.Render<RootPage>();
        using var styles = new StyleEngine(host.Renderer) { Application = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(result.StyleSheet)) };
        Assert.Equal(Color.FromRgb(255, 255, 255), styles.GetStyle(root.Elements("Window").Single()).Color);
        Assert.Equal(Color.FromRgb(0, 128, 0), styles.GetStyle(root.Elements("Button").Single()).Color);
        styles.Application = NssCompiler.Compile("ties", ".Window { color: blue; } :root { color: red; }").StyleSheet;
        Assert.Equal(Color.FromRgb(255, 0, 0), styles.GetStyle(root.Elements("Window").Single()).Color);
    }

    public sealed class RootPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Window");
            builder.AddAttribute(0, "role", "window");
            builder.AddAttribute(1, "class", "Window");
            builder.AddAttribute(2, "id", "main");
            builder.OpenElement(3, "Button");
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
