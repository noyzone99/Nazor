using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout.Tests;

public sealed class ArrangedChildrenTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Measured_content_can_arrange_retained_children_without_stacking_them(float scale)
    {
        var first = new LayoutNode();
        var second = new LayoutNode();
        var content = new LayoutNode
        {
            Measure = (_, _, _, _) => new Size(120, 40),
            ArrangeChildren = _ => [new Rect(10, 2, 30, 18), new Rect(50, 2, 40, 18)],
        };
        content.Add(first);
        content.Add(second);
        var root = new LayoutNode { Style = new LayoutStyle { PaddingLeft = Length.Px(7), PaddingTop = Length.Px(5) } };
        root.Add(content);
        LayoutEngine.Compute(root, new Size(160, 100), scale);
        Assert.Equal(40, content.Frame.Height);
        Assert.Equal(new Rect(17, 7, 30, 18), first.AbsoluteFrame);
        Assert.Equal(new Rect(57, 7, 40, 18), second.AbsoluteFrame);
        Assert.Equal(first.RoundedFrame.Y, second.RoundedFrame.Y);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Invalid_child_arrangements_fail_at_the_layout_boundary(bool wrongCount)
    {
        var root = new LayoutNode { ArrangeChildren = _ => wrongCount ? [] : [new Rect(0, 0, float.NaN, 10)] };
        root.Add(new LayoutNode());
        Assert.Throws<InvalidOperationException>(() => LayoutEngine.Compute(root, new Size(100, 100)));
    }
}
