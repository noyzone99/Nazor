using Nazor.Animation;
using Nazor.Style;

namespace Nazor.Rendering.Tests;

public sealed class ImperativeTransitionTests
{
    [Fact]
    public void Explicit_retargeting_uses_the_supplied_sample_and_instant_requests_cancel_only_that_property()
    {
        var clock = new ManualAnimationClock();
        var engine = new TransitionEngine(clock);
        var owner = new object();
        var definition = new TransitionDefinition("opacity", TimeSpan.FromMilliseconds(1000), EasingFunction.Linear, TimeSpan.Zero);
        Assert.True(engine.Start(owner, StyleProperties.Opacity, 0d, 1d, definition));
        clock.Advance(400);
        engine.Tick();
        var sample = engine.GetValue(owner, StyleProperties.Opacity, 1d);
        Assert.Equal(0.4, sample, 4);
        Assert.True(engine.Start(owner, StyleProperties.Opacity, sample, 0d, definition));
        engine.Tick();
        Assert.Equal(sample, engine.GetValue(owner, StyleProperties.Opacity, 0d));
        clock.Advance(500);
        engine.Tick();
        Assert.Equal(0.2, engine.GetValue(owner, StyleProperties.Opacity, 0d), 4);
        engine.Suspended = true;
        Assert.False(engine.Start(owner, StyleProperties.Opacity, 0.2d, 1d, definition));
        Assert.Equal(1, engine.GetValue(owner, StyleProperties.Opacity, 1d));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1000, -1)]
    [InlineData(4294967294, 1)]
    public void Explicit_timing_rejects_negative_or_overflowing_totals(double duration, double delay)
    {
        var engine = new TransitionEngine(new ManualAnimationClock());
        var definition = new TransitionDefinition("opacity", TimeSpan.FromMilliseconds(duration), EasingFunction.Linear, TimeSpan.FromMilliseconds(delay));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Start(new object(), StyleProperties.Opacity, 0d, 1d, definition));
    }

    [Fact]
    public void Explicit_timing_rejects_invalid_easing_before_it_can_publish_nonfinite_geometry()
    {
        var engine = new TransitionEngine(new ManualAnimationClock());
        var definition = new TransitionDefinition("opacity", TimeSpan.FromMilliseconds(100), new EasingFunction(double.NaN, 0, 1, 1), TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Start(new object(), StyleProperties.Opacity, 0d, 1d, definition));
    }
}
