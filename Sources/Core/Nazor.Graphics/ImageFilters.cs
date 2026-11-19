namespace Nazor.Graphics;

/// <summary>A backend-neutral image operation applied in an ordered filter group.</summary>
public abstract record ImageFilter;

/// <summary>A transparent-edge Gaussian blur for element content, or mirrored-edge blur for captured backdrops.</summary>
/// <param name="Sigma">Finite nonnegative standard deviation in logical pixels; Skia bounds it to256.</param>
public sealed record GaussianBlurFilter(float Sigma) : ImageFilter;

/// <summary>A copied4×5 row-major RGBA matrix. Components and translation columns use normalized0..1 units; input/output remain straight-alpha colors.</summary>
public sealed record ColorMatrixFilter : ImageFilter
{
    /// <summary>Copies a finite20-coefficient matrix.</summary>
    /// <param name="values">Four rows of five coefficients.</param>
    /// <exception cref="ArgumentException">The count is not20 or a coefficient is nonfinite.</exception>
    public ColorMatrixFilter(IEnumerable<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Length != 20 || copy.Any(value => !float.IsFinite(value))) throw new ArgumentException("A color matrix requires20 finite coefficients.", nameof(values));
        Values = Array.AsReadOnly(copy);
    }

    /// <summary>The immutable copied matrix coefficients.</summary>
    public IReadOnlyList<float> Values { get; }
}

/// <summary>Filters all following foreground drawing through an isolated group until Pop, then composites it once with Opacity.</summary>
public sealed record PushFilter : DrawCommand
{
    /// <summary>Copies an ordered operation list.</summary>
    /// <param name="filters">Operations in application order.</param>
    /// <param name="bounds">Layer extent including filter overflow.</param>
    /// <param name="opacity">Finite group opacity from zero to one.</param>
    public PushFilter(IEnumerable<ImageFilter> filters, Rect bounds, float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(filters);
        Filters = Array.AsReadOnly(filters.ToArray()); Bounds = bounds; Opacity = opacity;
    }

    /// <summary>The immutable copied operation list.</summary>
    public IReadOnlyList<ImageFilter> Filters { get; }
    /// <summary>Layer extent including filtered overflow.</summary>
    public Rect Bounds { get; }
    /// <summary>Group opacity from zero to one.</summary>
    public float Opacity { get; }
}
