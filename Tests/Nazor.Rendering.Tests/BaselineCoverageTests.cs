namespace Nazor.Rendering.Tests;

public sealed class BaselineCoverageTests
{
    [Fact]
    public void Every_visual_scenario_has_a_native_Linux_reference()
    {
        var baselines = Path.Combine(AppContext.BaseDirectory, "Baselines");
        var shared = Directory.GetFiles(baselines, "*.png").Select(Path.GetFileName).Order().ToArray();
        var linux = Directory.GetFiles(Path.Combine(baselines, "Linux"), "*.png").Select(Path.GetFileName).Order().ToArray();
        Assert.NotEmpty(shared);
        Assert.Equal(shared, linux);
    }
}
