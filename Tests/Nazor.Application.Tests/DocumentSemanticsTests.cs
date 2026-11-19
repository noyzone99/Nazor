using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class DocumentSemanticsTests
{
    [Fact]
    public void Document_lists_keep_structure_without_selection_and_ignore_decorative_markers()
    {
        using var host = new TestHost();
        using var graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
            UseSystemFonts = false,
            DefaultFamily = "Fira Sans",
        });
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(500, 400) };
        using var input = new InputManager(host.Renderer, scene);
        using var tree = new SemanticTree(host.Renderer, scene, input);
        host.Render<Structure>();
        tree.Update();
        var list = Assert.Single(tree.Descendants(), node => node.Role == SemanticRole.DocumentList);
        Assert.False(list.CanSelectMultiple);
        var item = Assert.Single(list.Children);
        Assert.Equal(SemanticRole.DocumentListItem, item.Role);
        Assert.Equal("First", item.Name);
        Assert.Equal(1, item.PositionInSet);
        Assert.Equal(1, item.SizeOfSet);
        Assert.Null(item.IsChecked);
        Assert.False(tree.Select(item));
        var heading = Assert.Single(tree.Descendants(), node => node.Role == SemanticRole.Heading);
        Assert.Equal(2, heading.HeadingLevel);
        Assert.Equal("Section", heading.Name);
        var meter = Assert.Single(tree.Descendants(), node => node.Role == SemanticRole.Meter);
        Assert.NotNull(meter.Range);
        Assert.True(meter.Range.IsReadOnly);
        Assert.False(tree.SetRangeValue(meter, 50));
        var cell = Assert.Single(tree.Descendants(), node => node.Role == SemanticRole.Cell);
        Assert.Equal(0, cell.RowIndex);
        Assert.Equal(1, cell.ColumnIndex);
        Assert.Equal(2, cell.ColumnSpan);
        Assert.True(cell.IsRequired);
        Assert.True(cell.IsInvalid);
    }

    private sealed class Structure : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Heading");
            builder.AddAttribute(1, "role", "heading"); builder.AddAttribute(2, "level", 2);
            builder.AddContent(3, "Section"); builder.CloseElement();
            builder.OpenElement(4, "List"); builder.AddAttribute(5, "role", "list");
            builder.OpenElement(6, "ListItem"); builder.AddAttribute(7, "role", "listitem");
            builder.OpenElement(8, "Marker"); builder.AddAttribute(9, "semantic-hidden", true);
            builder.AddContent(10, "1. "); builder.CloseElement(); builder.AddContent(11, "First");
            builder.CloseElement(); builder.CloseElement();
            builder.OpenElement(12, "Meter"); builder.AddAttribute(13, "role", "meter");
            builder.AddAttribute(14, "range-min", 0d); builder.AddAttribute(15, "range-max", 100d);
            builder.AddAttribute(16, "range-value", 72d); builder.CloseElement();
            builder.OpenElement(17, "Table"); builder.AddAttribute(18, "role", "table");
            builder.OpenElement(19, "Row"); builder.AddAttribute(20, "role", "row");
            builder.OpenElement(21, "Cell"); builder.AddAttribute(22, "role", "cell");
            builder.AddAttribute(23, "row-index", 0); builder.AddAttribute(24, "column-index", 1);
            builder.AddAttribute(25, "column-span", 2); builder.AddAttribute(26, "required", true);
            builder.AddAttribute(27, "invalid", true); builder.AddContent(28, "Value");
            builder.CloseElement(); builder.CloseElement(); builder.CloseElement();
        }
    }
}
