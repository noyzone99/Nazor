using Nazor.Samples.Counter;
using Nazor.Style;

namespace Nazor.Diagnostics.Tests;

public sealed class TraceTests : IDisposable
{
    private readonly DiagnosticsHarness harness = new();

    public void Dispose() => harness.Dispose();

    [Fact]
    public void The_button_of_the_counter_takes_its_background_from_the_rule_of_its_component()
    {
        harness.Host.Render<Counter>();
        var session = harness.Session;
        var button = session.Get(session.Roots[0])!.Children[1];
        var trace = session.Trace(button)!;
        var background = trace.Values.Single(v => v.Property == "background-color");
        Assert.Equal((ValueSource.Rule, "#303030"), (background.Source, background.Value));
        var winner = trace.Rules[background.Rule!.Value];
        Assert.Equal((StyleLevel.Component, ".counter Button"), (winner.Level, winner.Selector));
        var theme = trace.Rules.Where(rule => rule.Level == StyleLevel.Theme && rule.Selector == "Button")
            .SelectMany(rule => rule.Declarations).First(d => d.Longhands.Contains("background-color"));
        Assert.Equal("background", theme.Property);
        Assert.DoesNotContain("background-color", theme.Winning);
        var label = session.Get(button)!.Children[0];
        Assert.Equal("ButtonLabel", session.Get(label)!.Name);
        Assert.Null(session.Trace(session.Get(label)!.Children[0]));
        Assert.Null(session.Trace(9999));
    }
}
