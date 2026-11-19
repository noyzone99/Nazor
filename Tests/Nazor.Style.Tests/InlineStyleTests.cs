using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class InlineStyled : ComponentBase
{
    public string? Inline { get; set; } = "width: 40%; --tinta: #ff0000";

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Box");
        builder.AddAttribute(1, "class", "box");
        builder.AddAttribute(2, "style", Inline);
        builder.OpenElement(3, "Leaf");
        builder.CloseElement();
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class InlineStyleTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly StyleEngine engine;
    private readonly RootComponent root;

    public InlineStyleTests()
    {
        engine = new StyleEngine(host.Renderer);
        root = host.Render<InlineStyled>();
        var result = NssCompiler.Compile("test", """
            .box { width: 10px; height: 20px; color: blue; }
            Leaf { background: var(--tinta, black); }
            """, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        engine.Application = result.StyleSheet;
    }

    public void Dispose()
    {
        engine.Dispose();
        host.Dispose();
    }

    private ComputedStyle Style(string name) => engine.GetStyle(root.Single(name));

    private void Change(string? inline)
    {
        var component = (InlineStyled)root.Component;
        component.Inline = inline;
        component.Refresh();
        host.RunPending();
        engine.Update();
    }

    [Theory]
    [InlineData("width: 40%; --tinta: red", "width: 40% | --tinta: red")]
    [InlineData("  WIDTH : 10px ;; ", "width: 10px")]
    [InlineData("margin: 1px 2px; grid-template-columns: repeat(2, 1fr)", "margin: 1px 2px | grid-template-columns: repeat(2, 1fr)")]
    [InlineData("background: url(\"a;b.png\"); color: red", "background: url(\"a;b.png\") | color: red")]
    [InlineData("colour: red; width; height: ; : 1px; color: green", "color: green")]
    [InlineData("", "")]
    public void Declarations_are_read_in_order_and_unknown_ones_are_skipped(string text, string expected) =>
        Assert.Equal(expected, string.Join(" | ", InlineStyle.Parse(text)));

    [Fact]
    public void The_same_text_is_read_once() =>
        Assert.Same(InlineStyle.Parse("width: 1px; height: 2px"), InlineStyle.Parse("width: 1px; height: 2px"));

    [Fact]
    public void Repeated_single_and_multiple_property_changes_keep_previous_styles_immutable()
    {
        var snapshots = new List<(ComputedStyle Style, string Description)>();
        for (var i = 0; i < 24; i++)
        {
            Change($"width: {i % 3 + 1}px; --tinta: #ff0000;" + (i % 4 == 0 ? "height: 37px; background: red;" : ""));
            var current = Style("Box");
            Assert.Equal(Length.Px(i % 3 + 1), current.Get(StyleProperties.Width).Length);
            Assert.Equal(Length.Px(i % 4 == 0 ? 37 : 20), current.Get(StyleProperties.Height).Length);
            snapshots.Add((current, current.Describe()));
            Assert.All(snapshots, snapshot => Assert.Equal(snapshot.Description, snapshot.Style.Describe()));
        }
    }

    [Fact]
    public void Inline_declarations_win_over_the_rules_and_variables_reach_the_children()
    {
        Assert.Equal(Length.Percent(40), Style("Box").Get(StyleProperties.Width).Length);
        Assert.Equal(Length.Px(20), Style("Box").Get(StyleProperties.Height).Length);
        Assert.Equal(Color.FromRgb(255, 0, 0), Style("Leaf").BackgroundColor);
    }

    [Fact]
    public void Changing_the_attribute_restyles_and_invalid_values_are_unset()
    {
        Change("width: calc(50% - 4px); height: nonsense; color: var(--manca)");
        Assert.Equal("calc(50% - 4px)", Style("Box").Get(StyleProperties.Width).Length.ToString());
        Assert.Equal(StyleProperties.Height.InitialValue, Style("Box").Get(StyleProperties.Height));
        Assert.Equal(ComputedStyle.Initial.Color, Style("Box").Color);
        Change(null);
        Assert.Equal(Length.Px(10), Style("Box").Get(StyleProperties.Width).Length);
        Assert.Equal(Color.FromRgb(0, 0, 0), Style("Leaf").BackgroundColor);
    }
}
