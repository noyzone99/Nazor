using System.Diagnostics;
using Nazor.Style;

namespace Nazor.Animation;

/// <summary>
/// A monotonic time source for animations; tests replace it with <see cref="ManualAnimationClock" />.
/// </summary>
public interface IAnimationClock
{
    /// <summary>Gets the elapsed time reported by the animation clock.</summary>
    TimeSpan Now { get; }
}

/// <summary>
/// Monotonic time from <see cref="Stopwatch" />: it never goes backwards and ignores changes of the wall clock.
/// </summary>
public sealed class SystemAnimationClock : IAnimationClock
{
    private readonly long origin = Stopwatch.GetTimestamp();

    /// <summary>Gets the elapsed time reported by the animation clock.</summary>
    public TimeSpan Now => Stopwatch.GetElapsedTime(origin);
}

/// <summary>A clock that moves only when told to: time-dependent behaviour becomes deterministic.</summary>
public sealed class ManualAnimationClock : IAnimationClock
{
    /// <summary>Gets the elapsed time reported by the animation clock.</summary>
    public TimeSpan Now { get; private set; }

    /// <summary>Advances the clock by the supplied elapsed time.</summary>
    /// <param name="delta">The elapsed time by which to advance the clock.</param>
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta), "A monotonic clock cannot go back.");
        Now += delta;
    }

    /// <summary>Advances the clock by the supplied elapsed time.</summary>
    /// <param name="milliseconds">The elapsed time, in milliseconds, by which to advance the clock.</param>
    public void Advance(double milliseconds) => Advance(TimeSpan.FromMilliseconds(milliseconds));
}

/// <summary>Timing functions: CSS cubic Bézier curves from (0,0) to (1,1).</summary>
public static class Easing
{
    /// <summary>Output progress for an input progress in [0, 1] (the curve's y for the given x).</summary>
    /// <param name="function">The function (EasingFunction).</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The resulting double value.</returns>
    public static double Evaluate(EasingFunction function, double progress)
    {
        if (progress <= 0) return 0;
        if (progress >= 1) return 1;
        if (function == EasingFunction.Linear) return progress;

        var cx = 3 * function.X1;
        var bx = 3 * (function.X2 - function.X1) - cx;
        var ax = 1 - cx - bx;
        var cy = 3 * function.Y1;
        var by = 3 * (function.Y2 - function.Y1) - cy;
        var ay = 1 - cy - by;
        var t = SolveCurveX(progress, ax, bx, cx);
        return ((ay * t + by) * t + cy) * t;
    }

    private static double SolveCurveX(double x, double ax, double bx, double cx)
    {
        const double Epsilon = 1e-7;

        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = ((ax * t + bx) * t + cx) * t - x;
            if (Math.Abs(error) < Epsilon) return t;
            var derivative = (3 * ax * t + 2 * bx) * t + cx;
            if (Math.Abs(derivative) < 1e-6) break;
            t -= error / derivative;
        }

        double low = 0, high = 1;
        t = x;
        while (high - low > Epsilon)
        {
            var value = ((ax * t + bx) * t + cx) * t;
            if (Math.Abs(value - x) < Epsilon) return t;
            if (value < x) low = t;
            else high = t;
            t = (low + high) / 2;
        }
        return t;
    }
}

/// <summary>Interpolation of the animatable value types.</summary>
public static class Interpolation
{
    /// <summary>Interpolates between the start and end values at the supplied progress.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The interpolated value at the specified progress.</returns>
    public static double Lerp(double from, double to, double progress) => from + (to - from) * progress;

    /// <summary>Interpolates between the start and end values at the supplied progress.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The interpolated value at the specified progress.</returns>
    public static Color Lerp(Color from, Color to, double progress)
    {
        var fromAlpha = from.A / 255.0;
        var toAlpha = to.A / 255.0;
        var alpha = Lerp(fromAlpha, toAlpha, progress);
        if (alpha <= 0) return Color.Transparent;
        byte Channel(byte a, byte b) => ToByte(Lerp(a * fromAlpha, b * toAlpha, progress) / alpha);
        return new Color(Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B), ToByte(alpha * 255));
    }

    /// <summary>
    /// True when values of <paramref name="type" /> can be interpolated; some pairs still cannot (<see cref="CanInterpolate" />).
    /// </summary>
    /// <param name="type">The type (Type).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool IsInterpolable(Type type) =>
        type == typeof(Color) || type == typeof(double) || type == typeof(Length) || type == typeof(SizeValue) || type == typeof(RadiusValue) || type == typeof(ClipPathValue) ||
        type == typeof(ValueList<BoxShadow>) || type == typeof(ValueList<TransformFunction>) || type == typeof(TransformOrigin) || type == typeof(PositionValue);

    /// <summary>
    /// True when a value can move smoothly from <paramref name="from" /> to <paramref name="to" />: <c>auto</c> and a
    /// length cannot, nor shadow lists with an inset shadow against an outer one. Rectangular clips interpolate
    /// only when both values are insets; <c>none</c> changes discretely.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool CanInterpolate(object from, object to) => (from, to) switch
    {
        (Color, Color) or (double, double) or (Length, Length) or (RadiusValue, RadiusValue) or (TransformOrigin, TransformOrigin) or (PositionValue, PositionValue) => true,
        (ValueList<TransformFunction>, ValueList<TransformFunction>) => true,
        (ClipPathValue a, ClipPathValue b) => !a.IsNone && !b.IsNone,
        (SizeValue a, SizeValue b) => a.IsFixed && b.IsFixed,
        (ValueList<BoxShadow> a, ValueList<BoxShadow> b) => PadShadows(a, b) is not null,
        _ => false,
    };

    /// <summary>Interpolates between the start and end values at the supplied progress.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The interpolated value at the specified progress.</returns>
    public static object Lerp(object from, object to, double progress) => (from, to) switch
    {
        (Color a, Color b) => Lerp(a, b, progress),
        (double a, double b) => Lerp(a, b, progress),
        (Length a, Length b) => Lerp(a, b, progress),
        (SizeValue { IsFixed: true } a, SizeValue { IsFixed: true } b) => SizeValue.Fixed(Lerp(a.Length, b.Length, progress)),
        (RadiusValue a, RadiusValue b) => new RadiusValue(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        (ClipPathValue { IsNone: false } a, ClipPathValue { IsNone: false } b) => new ClipPathValue(
            Lerp(a.Top, b.Top, progress), Lerp(a.Right, b.Right, progress), Lerp(a.Bottom, b.Bottom, progress), Lerp(a.Left, b.Left, progress))
        {
            TopLeftRadius = (RadiusValue)Lerp(a.TopLeftRadius, b.TopLeftRadius, progress), TopRightRadius = (RadiusValue)Lerp(a.TopRightRadius, b.TopRightRadius, progress),
            BottomRightRadius = (RadiusValue)Lerp(a.BottomRightRadius, b.BottomRightRadius, progress), BottomLeftRadius = (RadiusValue)Lerp(a.BottomLeftRadius, b.BottomLeftRadius, progress),
        },
        (TransformOrigin a, TransformOrigin b) => new TransformOrigin(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        (PositionValue a, PositionValue b) => new PositionValue(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        (ValueList<TransformFunction> a, ValueList<TransformFunction> b) => Lerp(a, b, progress),
        (ValueList<BoxShadow> a, ValueList<BoxShadow> b) when PadShadows(a, b) is var (x, y) =>
            new ValueList<BoxShadow>(x.Zip(y, (s, t) => new BoxShadow(s.Inset, Lerp(s.X, t.X, progress), Lerp(s.Y, t.Y, progress),
                Lerp(s.Blur, t.Blur, progress), Lerp(s.Spread, t.Spread, progress), Lerp(s.Color, t.Color, progress)))),
        _ => progress < 0.5 ? from : to,
    };

    /// <summary>Interpolates between the start and end values at the supplied progress.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The interpolated value at the specified progress.</returns>
    public static Length Lerp(Length from, Length to, double progress)
    {
        if (from.Unit == to.Unit && from.Unit is LengthUnit.Px or LengthUnit.Percent) return new Length(Lerp(from.Value, to.Value, progress), from.Unit);
        static CalcNode Node(Length length) => length.Expression?.Root ?? new CalcLength(length);
        return CalcExpression.Simplify(new CalcOperation(CalcOperator.Add,
            new CalcOperation(CalcOperator.Multiply, new CalcNumber(1 - progress), Node(from)),
            new CalcOperation(CalcOperator.Multiply, new CalcNumber(progress), Node(to))), nonNegative: false);
    }

    /// <summary>Interpolates between the start and end values at the supplied progress.</summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The destination value.</param>
    /// <param name="progress">The interpolation progress, where zero is the start and one is the end.</param>
    /// <returns>The interpolated value at the specified progress.</returns>
    public static ValueList<TransformFunction> Lerp(ValueList<TransformFunction> from, ValueList<TransformFunction> to, double progress)
    {
        IReadOnlyList<TransformFunction> a = from.Items, b = to.Items;
        if (a.Count == 0 && b.Count == 0) return from;
        if (a.Count == 0) a = b.Select(Identity).ToArray();
        else if (b.Count == 0) b = a.Select(Identity).ToArray();
        if (a.Count == b.Count && a.Zip(b).All(p => p.First.GetType() == p.Second.GetType() && p.First is TranslateFunction or ScaleFunction or RotateFunction or SkewFunction))
            return new ValueList<TransformFunction>(a.Zip(b, (x, y) => Lerp(x, y, progress)));
        return new ValueList<TransformFunction>([new MixFunction(new ValueList<TransformFunction>(a), new ValueList<TransformFunction>(b), progress)]);
    }

    private static TransformFunction Lerp(TransformFunction from, TransformFunction to, double progress) => (from, to) switch
    {
        (TranslateFunction a, TranslateFunction b) => new TranslateFunction(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        (ScaleFunction a, ScaleFunction b) => new ScaleFunction(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        (RotateFunction a, RotateFunction b) => new RotateFunction(Lerp(a.Angle, b.Angle, progress)),
        (SkewFunction a, SkewFunction b) => new SkewFunction(Lerp(a.X, b.X, progress), Lerp(a.Y, b.Y, progress)),
        _ => progress < 0.5 ? from : to,
    };

    private static TransformFunction Identity(TransformFunction function) => function switch
    {
        TranslateFunction => new TranslateFunction(Length.Zero, Length.Zero),
        ScaleFunction => new ScaleFunction(1, 1),
        RotateFunction => new RotateFunction(0),
        SkewFunction => new SkewFunction(0, 0),
        _ => new MatrixFunction(1, 0, 0, 1, 0, 0),
    };

    private static (IReadOnlyList<BoxShadow>, IReadOnlyList<BoxShadow>)? PadShadows(ValueList<BoxShadow> from, ValueList<BoxShadow> to)
    {
        var a = from.Items.ToList();
        var b = to.Items.ToList();
        static BoxShadow None(BoxShadow other) => new(other.Inset, Length.Zero, Length.Zero, Length.Zero, Length.Zero, Color.Transparent);
        while (a.Count < b.Count) a.Add(None(b[a.Count]));
        while (b.Count < a.Count) b.Add(None(a[b.Count]));
        return a.Zip(b).All(p => p.First.Inset == p.Second.Inset) ? (a, b) : null;
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
