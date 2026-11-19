using System.Globalization;
using Nazor.Controls.Tests.Fields.Fixtures;
using Nazor.Input;

namespace Nazor.Controls.Tests.Fields;

[Collection("Controls")]
public sealed class NumberExpressionTests
{
    [Theory]
    [InlineData("2 + 3 * 4", "en-US", 14)]
    [InlineData("(2 + 3) * 4", "en-US", 20)]
    [InlineData("2 ^ 3 ^ 2", "en-US", 64)]
    [InlineData("-2 ^ 2", "en-US", 4)]
    [InlineData("2 ^ -2", "en-US", 0.25)]
    [InlineData("1--2", "en-US", 3)]
    [InlineData("-(2 + 3) * 2", "en-US", -10)]
    [InlineData("1e-3 + 2e+1", "en-US", 20.001)]
    [InlineData("1,234.5 + 5.5", "en-US", 1240)]
    [InlineData("(1.234,5 + 5,5) / 2", "it-IT", 620)]
    [InlineData("1\u202f234,5 + 0,5", "fr-FR", 1235)]
    [InlineData("2 * 3 kg", "en-US", 6)]
    public void Expressions_use_number_culture_and_WinUI_operator_precedence(string text, string culture, double expected)
    {
        var field = new NumberField { AcceptsExpression = true, Culture = CultureInfo.GetCultureInfo(culture), Unit = "kg" };
        Assert.True(field.TryRead(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1 / 0")]
    [InlineData("0 / 0")]
    [InlineData("1e308 * 2")]
    [InlineData("(-1) ^ 0.5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1 +")]
    [InlineData("(1 + 2")]
    [InlineData("1 + 2)")]
    [InlineData("()")]
    [InlineData("2(3)")]
    [InlineData("1 2")]
    [InlineData("1 + + + 2")]
    [InlineData("2 kg + 3 kg")]
    [InlineData("Math.Pow(2, 3)")]
    [InlineData("new Process()")]
    public void Invalid_or_nonfinite_expressions_are_rejected_without_evaluation(string text)
    {
        var field = new NumberField { AcceptsExpression = true, Culture = CultureInfo.InvariantCulture, Unit = "kg" };
        Assert.False(field.TryRead(text, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Expressions_are_opt_in_and_empty_input_still_means_no_value()
    {
        var field = new NumberField { Culture = CultureInfo.GetCultureInfo("en-US") };
        Assert.False(field.TryRead("2 + 3", out _));
        Assert.True(field.TryRead("1,234.5", out var number));
        Assert.Equal(1234.5, number);
        field.AcceptsExpression = true;
        Assert.True(field.TryRead(" ", out number));
        Assert.Null(number);
    }

    [Fact]
    public void Expression_length_and_parenthesis_depth_are_bounded()
    {
        var field = new NumberField { AcceptsExpression = true, Culture = CultureInfo.InvariantCulture };
        Assert.True(field.TryRead(new string('(', 64) + "1" + new string(')', 64), out var value));
        Assert.Equal(1d, value);
        Assert.False(field.TryRead(new string('(', 65) + "1" + new string(')', 65), out _));
        Assert.True(field.TryRead(string.Concat(Enumerable.Repeat("1+", 2047)) + "1", out value));
        Assert.Equal(2048d, value);
        Assert.False(field.TryRead(string.Concat(Enumerable.Repeat("1+", 2048)) + "1", out _));
    }

    [Fact]
    public void Enter_and_focus_loss_evaluate_once_then_format_the_committed_value()
    {
        using var h = new ControlHarness();
        h.Render<NumberExpressionProbe>();
        var probe = h.Component<NumberExpressionProbe>();
        h.Focus(h.One("NumberFieldInput"));
        Type("2 + 3 * 4");
        Assert.Equal(2d, probe.Value);
        Assert.Equal("2 + 3 * 4", probe.Field.Text);
        h.Press("Enter");
        Assert.Equal(14d, probe.Value);
        Assert.Equal("14.00", probe.Field.Text);
        Assert.Equal([14d], probe.Changes);

        Type("(2 + 3) / 2");
        h.Click(h.ByClass("outside"));
        Assert.Equal(2.5, probe.Value);
        Assert.Equal("2.50", probe.Field.Text);
        Assert.Equal([14d, 2.5], probe.Changes);

        h.Focus(h.One("NumberFieldInput"));
        Type("1 / 0");
        h.Press("Enter");
        Assert.Equal(2.5, probe.Value);
        Assert.Equal("2.50", probe.Field.Text);
        Assert.Equal(2, probe.Changes.Count);

        Type("100 * 2");
        h.Press("Enter");
        Assert.Equal(100d, probe.Value);
        Assert.Equal("100.00", probe.Field.Text);

        Type("1 / 3");
        h.Press("Enter");
        Assert.Equal(1d / 3, probe.Value);
        Assert.Equal("0.33", probe.Field.Text);

        void Type(string text)
        {
            h.Press("a", KeyModifiers.Control);
            h.Type(text);
        }
    }

    [Fact]
    public void Stepping_evaluates_the_pending_expression_before_applying_the_step()
    {
        using var h = new ControlHarness();
        h.Render<NumberExpressionProbe>();
        var probe = h.Component<NumberExpressionProbe>();
        h.Focus(h.One("NumberFieldInput"));
        h.Press("a", KeyModifiers.Control);
        h.Type("2 * 3");
        h.Press("ArrowUp");
        Assert.Equal(6.5, probe.Value);
        Assert.Equal("6.50", probe.Field.Text);
    }
}
