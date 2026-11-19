using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Controls.Tests.Foundation;

public sealed class VirtualizerTests
{
    [Fact]
    public async Task Scrolling_within_the_realized_range_does_not_rebuild_rows()
    {
        using var host = new TestHost();
        var builds = 0;
        var changes = 0;
        var root = host.Render<Virtualizer<int>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Items"] = Enumerable.Range(0, 1000).ToArray(),
            ["ItemTemplate"] = (RenderFragment<VirtualItem<int>>)(item => builder => { builds++; builder.AddContent(0, item.Item); }),
            ["OnRangeChanged"] = EventCallback.Factory.Create<VirtualRange>(this, _ => changes++),
        }));
        var viewport = root.Single("Virtualizer");
        async Task Scroll(double offset)
        {
            await host.Renderer.DispatchEventAsync(viewport, "onscroll", new ScrollEventArgs { ScrollTop = offset, ViewportHeight = 96 });
            host.RunPending();
        }

        await Scroll(0);
        var settled = builds;
        var range = ((Virtualizer<int>)root.Component).Range;
        await Scroll(1);
        Assert.Equal(settled, builds);
        Assert.Equal(range, ((Virtualizer<int>)root.Component).Range);
        Assert.Equal(1, changes);
        await Scroll(160);
        Assert.True(builds > settled);
        Assert.True(((Virtualizer<int>)root.Component).Range.First > range.First);
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task Repeated_item_measurements_do_not_rebuild_rows_but_changed_sizes_do()
    {
        using var host = new TestHost();
        var builds = 0;
        var root = host.Render<Virtualizer<int>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Items"] = Enumerable.Range(0, 100).ToArray(),
            ["MeasureItems"] = true,
            ["ItemTemplate"] = (RenderFragment<VirtualItem<int>>)(item => builder => { builds++; builder.AddContent(0, item.Item); }),
        }));
        var row = root.Elements("VirtualizerItem").First();
        var initial = builds;
        await host.Renderer.DispatchEventAsync(row, "onresize", new ResizeEventArgs { Height = 32 });
        host.RunPending();
        Assert.Equal(initial, builds);
        await host.Renderer.DispatchEventAsync(row, "onresize", new ResizeEventArgs { Height = 64 });
        host.RunPending();
        Assert.True(builds > initial);
    }
}
