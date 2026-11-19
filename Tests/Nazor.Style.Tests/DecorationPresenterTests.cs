using Nazor.Style;

namespace Nazor.Style.Tests;

public sealed class DecorationPresenterTests
{
    [Fact]
    public void Theme_composition_preserves_last_presenters_without_mutating_rule_only_sheets()
    {
        var first = new WindowDecorationPresenters(TitleBar: _ => builder => builder.AddContent(0, "First"));
        var second = new WindowDecorationPresenters(TitleBar: _ => builder => builder.AddContent(0, "Second"));
        var decorated = StyleSheet.Empty.WithWindowDecorations(first);
        Assert.Null(StyleSheet.Empty.WindowDecorations);
        Assert.Same(first, decorated.WindowDecorations);
        Assert.Same(first, StyleSheet.Combine(decorated, StyleSheet.Empty).WindowDecorations);
        var combined = StyleSheet.Combine(decorated, StyleSheet.Empty.WithWindowDecorations(second));
        Assert.Same(second, combined.WindowDecorations);
        Assert.Empty(combined.Rules);
        Assert.Same(first, decorated.WindowDecorations);
    }
}
