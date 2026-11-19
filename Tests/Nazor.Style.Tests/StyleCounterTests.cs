using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class StyleCounterTests
{
    [Fact]
    public void Total_recomputations_include_lazy_and_unchanged_updates_and_survive_idle_updates()
    {
        using var host = new TestHost();
        var element = host.Render<Box>().Single("Box");
        using var engine = new StyleEngine(host.Renderer);
        Assert.Equal(0, engine.TotalRecomputedCount);
        var initial = engine.GetStyle(element);
        Assert.Equal(1, engine.TotalRecomputedCount);
        Assert.Equal(1, engine.LastRecomputedCount);
        engine.Update();
        Assert.Equal(0, engine.LastRecomputedCount);
        Assert.Equal(1, engine.TotalRecomputedCount);
        var changes = 0;
        engine.StyleChanged += (_, _) => changes++;
        engine.InvalidateAll();
        Assert.Equal(initial, engine.GetStyle(element));
        Assert.Equal(1, engine.LastRecomputedCount);
        Assert.Equal(2, engine.TotalRecomputedCount);
        Assert.Equal(0, changes);
        engine.Update();
        Assert.Equal(0, engine.LastRecomputedCount);
        Assert.Equal(2, engine.TotalRecomputedCount);
    }
}
