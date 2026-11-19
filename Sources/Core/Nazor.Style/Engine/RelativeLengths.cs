namespace Nazor.Style;

internal sealed class RelativeLengths(double fontSize, double rootFontSize, Viewport viewport, Func<FontRelativeMetrics>? fontMetrics = null)
{
    private Func<FontRelativeMetrics>? fontMetrics = fontMetrics;
    private FontRelativeMetrics? measured;

    public Func<FontRelativeMetrics>? FontMetrics
    {
        set { fontMetrics = value; measured = null; }
    }

    public double FontSize { get; set; } = fontSize;

    public double RootFontSize { get; set; } = rootFontSize;

    public bool UsedViewport { get; private set; }

    public static bool NeedsComputing(StyleProperty property, object value)
    {
        if (ReferenceEquals(property, StyleProperties.FontSize)) return value is Length { IsRelative: true } or Length { IsPercent: true };
        if (ReferenceEquals(property, StyleProperties.LineHeight) && value is LineHeight { Kind: LineHeightKind.Length, Length.IsPercent: true }) return true;
        var relative = false;
        Map(value, length =>
        {
            relative |= length.IsRelative;
            return length;
        });
        return relative;
    }

    public object Compute(StyleProperty property, object value)
    {
        if (ReferenceEquals(property, StyleProperties.FontSize)) return Length.Px(Convert((Length)value).Resolve(FontSize));
        var mapped = Map(value, Convert);
        if (mapped is LineHeight { Kind: LineHeightKind.Length } line && line.Length.IsPercent)
            return LineHeight.Fixed(Length.Px(line.Length.Resolve(FontSize)));
        return mapped;
    }

    private Length Convert(Length length) => length.Unit switch
    {
        LengthUnit.Em => Length.Px(length.Value * FontSize),
        LengthUnit.Rem => Length.Px(length.Value * RootFontSize),
        LengthUnit.Ch => Length.Px(length.Value * Metric(zero: true)),
        LengthUnit.Ex => Length.Px(length.Value * Metric(zero: false)),
        LengthUnit.Vw => OfViewport(length.Value, viewport.Width),
        LengthUnit.Vh => OfViewport(length.Value, viewport.Height),
        LengthUnit.Vmin => OfViewport(length.Value, Math.Min(viewport.Width, viewport.Height)),
        LengthUnit.Vmax => OfViewport(length.Value, Math.Max(viewport.Width, viewport.Height)),
        LengthUnit.Calc when length.Expression!.IsRelative => length.Expression.Map(Convert),
        _ => length,
    };

    private double Metric(bool zero)
    {
        measured ??= fontMetrics?.Invoke() ?? new FontRelativeMetrics();
        var value = zero ? measured.Value.ZeroAdvance : measured.Value.XHeight;
        if (value is null) return FontSize * 0.5;
        if (!double.IsFinite(value.Value) || value < 0)
            throw new InvalidOperationException("Font-relative measurements must be finite and nonnegative.");
        return value.Value;
    }

    private Length OfViewport(double percent, double side)
    {
        UsedViewport = true;
        return Length.Px(percent * side / 100);
    }

    internal static object Map(object value, Func<Length, Length> map) => value switch
    {
        Length length => map(length),
        SizeValue size => size with { Length = map(size.Length) },
        RadiusValue radius => new RadiusValue(map(radius.X), map(radius.Y)),
        FilterValue filters => new FilterValue(new ValueList<FilterFunction>(filters.Functions.Items.Select(function => function with { Radius = map(function.Radius) }))),
        ClipPathValue clip => clip with
        {
            Top = map(clip.Top), Right = map(clip.Right), Bottom = map(clip.Bottom), Left = map(clip.Left),
            TopLeftRadius = (RadiusValue)Map(clip.TopLeftRadius, map), TopRightRadius = (RadiusValue)Map(clip.TopRightRadius, map),
            BottomRightRadius = (RadiusValue)Map(clip.BottomRightRadius, map), BottomLeftRadius = (RadiusValue)Map(clip.BottomLeftRadius, map),
        },
        LineHeight line => line with { Length = map(line.Length) },
        ScrollWheelStep step => step with { Length = map(step.Length) },
        WindowFrameShadows { Shadows: { } shadows } => new WindowFrameShadows((ValueList<BoxShadow>)Map(shadows, map)),
        ValueList<BoxShadow> shadows => new ValueList<BoxShadow>(shadows.Items.Select(s => s with { X = map(s.X), Y = map(s.Y), Blur = map(s.Blur), Spread = map(s.Spread) })),
        ValueList<ImageValue> images => new ValueList<ImageValue>(images.Items.Select(i => i is Gradient g ? Map(g, map) : i)),
        ValueList<PositionValue> positions => new ValueList<PositionValue>(positions.Items.Select(p => new PositionValue(map(p.X), map(p.Y)))),
        ValueList<BackgroundSize> sizes => new ValueList<BackgroundSize>(sizes.Items.Select(s =>
            s with { Width = s.Width with { Length = map(s.Width.Length) }, Height = s.Height with { Length = map(s.Height.Length) } })),
        ValueList<TrackSize> tracks => Map(tracks, map),
        GridTrackList list => list with { Tracks = Map(list.Tracks, map), RepeatTracks = Map(list.RepeatTracks, map) },
        ValueList<TransformFunction> transforms => new ValueList<TransformFunction>(transforms.Items.Select(f =>
            f is TranslateFunction translate ? translate with { X = map(translate.X), Y = map(translate.Y) } : f)),
        TransformOrigin origin => new TransformOrigin(map(origin.X), map(origin.Y)),
        PositionValue position => new PositionValue(map(position.X), map(position.Y)),
        _ => value,
    };

    private static ValueList<TrackSize> Map(ValueList<TrackSize> tracks, Func<Length, Length> map) =>
        new(tracks.Items.Select(t => t with { Min = t.Min with { Length = map(t.Min.Length) }, Max = t.Max with { Length = map(t.Max.Length) } }));

    private static Gradient Map(Gradient gradient, Func<Length, Length> map)
    {
        var stops = new ValueList<ColorStop>(gradient.Stops.Items.Select(s => s.Position is { } position ? s with { Position = map(position) } : s));
        return gradient switch
        {
            ConicGradient conic => conic with { CenterX = map(conic.CenterX), CenterY = map(conic.CenterY), Stops = stops },
            RadialGradient radial => radial with
            {
                SizeX = map(radial.SizeX), SizeY = map(radial.SizeY), CenterX = map(radial.CenterX), CenterY = map(radial.CenterY), Stops = stops,
            },
            _ => gradient with { Stops = stops },
        };
    }
}
