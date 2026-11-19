using System.Globalization;

namespace Nazor.Style;

/// <summary>Identifies a binary arithmetic operator in a calculation expression.</summary>
public enum CalcOperator : byte
{
    /// <summary>Adds the left and right operands.</summary>
    Add,

    /// <summary>Subtracts the right operand from the left.</summary>
    Subtract,

    /// <summary>Multiplies the operands.</summary>
    Multiply,

    /// <summary>Divides the left operand by the right.</summary>
    Divide,
}

/// <summary>Identifies the minimum, maximum, or clamp calculation function.</summary>
public enum CalcFunctionKind : byte
{
    /// <summary>Selects the smallest evaluated argument.</summary>
    Min,

    /// <summary>Selects the largest evaluated argument.</summary>
    Max,

    /// <summary>Clamps the preferred value between the minimum and maximum bounds.</summary>
    Clamp,
}

/// <summary>
/// A node of a <c>calc()</c>, <c>min()</c>, <c>max()</c> or <c>clamp()</c> expression (CSS Values §10): a number, a
/// length leaf, an operation or a comparison function.
/// </summary>
public abstract record CalcNode
{
    private protected CalcNode()
    {
    }

    internal virtual int Precedence => 3;

    internal abstract double Evaluate(double basis);

    internal abstract CalcNode Map(Func<Length, Length> map);

    internal abstract IEnumerable<Length> Leaves();

    internal abstract CalcTerm? Reduce();
}

internal readonly record struct CalcTerm(bool IsNumber, double Number, double Px, double Percent)
{
    public static CalcTerm FromNumber(double value) => new(true, value, 0, 0);

    public static CalcTerm FromLength(double px, double percent) => new(false, 0, px, percent);

    public CalcTerm Scale(double factor) => IsNumber ? FromNumber(Number * factor) : FromLength(Px * factor, Percent * factor);
}

/// <summary>Represents a unitless numeric literal in an NSS calculation.</summary>
/// <param name="Value">The numeric value or payload represented by this record.</param>
public sealed record CalcNumber(double Value) : CalcNode
{
    internal override double Evaluate(double basis) => Value;

    internal override CalcNode Map(Func<Length, Length> map) => this;

    internal override IEnumerable<Length> Leaves() => [];

    internal override CalcTerm? Reduce() => CalcTerm.FromNumber(Value);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Value.ToString("0.######", CultureInfo.InvariantCulture);
}

/// <summary>A length leaf: px, a percentage or a relative unit, never another calculation.</summary>
/// <param name="Length">The absolute, relative, or percentage length to use.</param>
public sealed record CalcLength(Length Length) : CalcNode
{
    internal override double Evaluate(double basis) => Length.Resolve(basis);

    internal override CalcNode Map(Func<Length, Length> map)
    {
        var mapped = map(Length);
        return mapped == Length ? this : mapped.Expression is { } expression ? expression.Root : new CalcLength(mapped);
    }

    internal override IEnumerable<Length> Leaves() => [Length];

    internal override CalcTerm? Reduce() => Length.Unit switch
    {
        LengthUnit.Px => CalcTerm.FromLength(Length.Value, 0),
        LengthUnit.Percent => CalcTerm.FromLength(0, Length.Value),
        _ => null,
    };

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Length.ToString();
}

/// <summary>Represents a binary arithmetic operation between two calculation nodes.</summary>
/// <param name="Operator">The operator (CalcOperator).</param>
/// <param name="Left">The value associated with the left side or operand.</param>
/// <param name="Right">The value associated with the right side or operand.</param>
public sealed record CalcOperation(CalcOperator Operator, CalcNode Left, CalcNode Right) : CalcNode
{
    internal override int Precedence => Operator is CalcOperator.Add or CalcOperator.Subtract ? 1 : 2;

    internal override double Evaluate(double basis)
    {
        var left = Left.Evaluate(basis);
        var right = Right.Evaluate(basis);
        return Operator switch
        {
            CalcOperator.Add => left + right,
            CalcOperator.Subtract => left - right,
            CalcOperator.Multiply => left * right,
            _ => left / right,
        };
    }

    internal override CalcNode Map(Func<Length, Length> map)
    {
        var left = Left.Map(map);
        var right = Right.Map(map);
        return ReferenceEquals(left, Left) && ReferenceEquals(right, Right) ? this : new CalcOperation(Operator, left, right);
    }

    internal override IEnumerable<Length> Leaves() => Left.Leaves().Concat(Right.Leaves());

    internal override CalcTerm? Reduce()
    {
        if (Left.Reduce() is not { } left || Right.Reduce() is not { } right) return null;
        return Operator switch
        {
            CalcOperator.Add => left.IsNumber ? CalcTerm.FromNumber(left.Number + right.Number) : CalcTerm.FromLength(left.Px + right.Px, left.Percent + right.Percent),
            CalcOperator.Subtract => left.IsNumber ? CalcTerm.FromNumber(left.Number - right.Number) : CalcTerm.FromLength(left.Px - right.Px, left.Percent - right.Percent),
            CalcOperator.Multiply => left.IsNumber ? right.Scale(left.Number) : left.Scale(right.Number),
            _ => left.Scale(1 / right.Number),
        };
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var symbol = Operator switch
        {
            CalcOperator.Add => "+",
            CalcOperator.Subtract => "-",
            CalcOperator.Multiply => "*",
            _ => "/",
        };

        var left = Left.Precedence < Precedence ? $"({Left})" : Left.ToString();
        var right = Right.Precedence < Precedence || (Right.Precedence == Precedence && Operator is CalcOperator.Subtract or CalcOperator.Divide)
            ? $"({Right})" : Right.ToString();
        return $"{left} {symbol} {right}";
    }
}

/// <summary><c>min()</c>, <c>max()</c> or <c>clamp(min, value, max)</c>.</summary>
/// <param name="Kind">The calc function kind represented by this value.</param>
/// <param name="Arguments">The arguments (ValueList&lt;CalcNode&gt;).</param>
public sealed record CalcFunction(CalcFunctionKind Kind, ValueList<CalcNode> Arguments) : CalcNode
{
    internal override double Evaluate(double basis) => Apply(Arguments.Items.Select(a => a.Evaluate(basis)).ToArray());

    internal override CalcNode Map(Func<Length, Length> map)
    {
        var mapped = Arguments.Items.Select(a => a.Map(map)).ToArray();
        return mapped.Select((a, i) => ReferenceEquals(a, Arguments.Items[i])).All(same => same) ? this : new CalcFunction(Kind, new ValueList<CalcNode>(mapped));
    }

    internal override IEnumerable<Length> Leaves() => Arguments.Items.SelectMany(a => a.Leaves());

    internal override CalcTerm? Reduce()
    {
        var terms = new CalcTerm[Arguments.Items.Count];
        for (var i = 0; i < terms.Length; i++)
        {
            if (Arguments.Items[i].Reduce() is not { } term) return null;
            terms[i] = term;
        }
        if (terms[0].IsNumber) return CalcTerm.FromNumber(Apply(terms.Select(t => t.Number).ToArray()));

        if (terms.All(t => t.Percent == 0)) return CalcTerm.FromLength(Apply(terms.Select(t => t.Px).ToArray()), 0);
        if (terms.All(t => t.Px == 0)) return CalcTerm.FromLength(0, Apply(terms.Select(t => t.Percent).ToArray()));
        return null;
    }

    private double Apply(double[] values) => Kind switch
    {
        CalcFunctionKind.Min => values.Min(),
        CalcFunctionKind.Max => values.Max(),

        _ => Math.Max(values[0], Math.Min(values[1], values[2])),
    };

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}({string.Join(", ", Arguments.Items)})";
}

/// <summary>
/// A length computed by an expression. <see cref="NonNegative" /> clamps the result at zero, for properties that do
/// not accept negative lengths. Expressions without relative units are reduced when parsed: <c>calc(10px + 2 * 5px)</c>
/// is 20px, <c>calc(100% - 16px)</c> stays an expression.
/// </summary>
/// <param name="Root">The root (CalcNode).</param>
/// <param name="NonNegative">The non negative (bool).</param>
public sealed record CalcExpression(CalcNode Root, bool NonNegative)
{
    /// <summary>The expression depends on the percentage basis.</summary>
    public bool HasPercent { get; } = Root.Leaves().Any(l => l.Unit == LengthUnit.Percent);

    /// <summary>The expression has em, rem or viewport units, converted by the style engine.</summary>
    public bool IsRelative { get; } = Root.Leaves().Any(l => l.IsRelative);

    /// <summary>Evaluates the calculation against a reference dimension for percentages.</summary>
    /// <param name="basis">The reference dimension used to resolve percentage values.</param>
    /// <returns>The resulting double value.</returns>
    public double Evaluate(double basis)
    {
        var value = Root.Evaluate(basis);
        return NonNegative ? Math.Max(0, value) : value;
    }

    /// <summary>Replaces the leaves (for example em with px), then reduces the expression.</summary>
    /// <param name="map">The map (Func&lt;Length, Length&gt;).</param>
    /// <returns>The resulting Length value.</returns>
    public Length Map(Func<Length, Length> map) => Simplify(Root.Map(map), NonNegative);

    /// <summary>
    /// A plain length when the expression reduces to px or to a percentage, otherwise an expression.
    /// </summary>
    /// <param name="root">The root (CalcNode).</param>
    /// <param name="nonNegative">The non negative (bool).</param>
    /// <returns>The resulting Length value.</returns>
    public static Length Simplify(CalcNode root, bool nonNegative)
    {
        if (root.Reduce() is { IsNumber: false } term)
        {
            if (term.Percent == 0) return Length.Px(nonNegative ? Math.Max(0, term.Px) : term.Px);
            if (term.Px == 0) return Length.Percent(nonNegative ? Math.Max(0, term.Percent) : term.Percent);
            root = new CalcOperation(term.Px < 0 ? CalcOperator.Subtract : CalcOperator.Add,
                new CalcLength(Length.Percent(term.Percent)), new CalcLength(Length.Px(Math.Abs(term.Px))));
        }
        return Length.Calc(new CalcExpression(root, nonNegative));
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Root is CalcFunction ? Root.ToString() : $"calc({Root})";
}
