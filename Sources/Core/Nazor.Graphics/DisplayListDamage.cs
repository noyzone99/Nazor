namespace Nazor.Graphics;

/// <summary>
/// What changed on the surface between two display lists: the bounds of the drawings that one list has and the other
/// does not, each with the transform, clip and opacities it is drawn with. Painting the damaged rectangles again with
/// the new list gives the pixels of a full repaint. A drawing that moved before or after another one (a change of
/// paint order) is damaged too. Curved edge regions touched by damage, including pixel rounding and antialiasing,
/// are included whole so clipping a partial repaint does not change their edge coverage.
/// </summary>
public sealed class DisplayListDamage
{

    private const int MaxRectangles = 8;

    private List<Drawing> previous = [];
    private List<Drawing> current = [];
    private Size? previousSize;
    private float previousScale = 1;
    private readonly Dictionary<Drawing, int> first = new(DrawingComparer.Instance);
    private readonly Stack<(Matrix, Rect, Opacities?)> states = new();
    private int[] next = [];
    private bool[] matched = [];

    /// <summary>
    /// The damaged rectangles between two lists, in logical units of the surface, merged and clipped to it; empty
    /// when nothing changed, null when everything must be painted (no previous list, a new size, a drawing whose
    /// bounds are unknown). Uses raster scale 1; use the scale overload for other pixel densities.
    /// </summary>
    /// <param name="previous">The previous (DisplayList?).</param>
    /// <param name="current">The current navigation entry before the change.</param>
    /// <returns>The resulting IReadOnlyList&lt;Rect&gt;? value.</returns>
    public static IReadOnlyList<Rect>? Compute(DisplayList? previous, DisplayList current) => Compute(previous, current, 1);

    /// <summary>Computes logical damage at the specified raster scale, including curved edge regions touched by pixel rounding and antialiasing.</summary>
    /// <param name="previous">The previous display list, or null for a full repaint.</param>
    /// <param name="current">The display list to paint.</param>
    /// <param name="scale">A finite, positive number of physical pixels per logical unit.</param>
    /// <returns>Logical damage rectangles, or null when a full repaint is required.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The scale is nonpositive or non-finite.</exception>
    public static IReadOnlyList<Rect>? Compute(DisplayList? previous, DisplayList current, float scale)
    {
        var damage = new DisplayListDamage();
        if (previous is not null) damage.Next(previous, scale);
        return damage.Next(current, scale);
    }

    /// <summary>
    /// What <paramref name="list" /> changes from the list of the previous call at scale 1 (see <see cref="Compute(DisplayList, DisplayList)" />).
    /// </summary>
    /// <param name="list">The list (DisplayList).</param>
    /// <returns>The resulting IReadOnlyList&lt;Rect&gt;? value.</returns>
    public IReadOnlyList<Rect>? Next(DisplayList list) => Next(list, 1);

    /// <summary>Tracks logical damage at the specified raster scale. Scale changes require a full repaint; curved edge regions touched by pixel damage are included whole.</summary>
    /// <param name="list">The current display list.</param>
    /// <param name="scale">A finite, positive number of physical pixels per logical unit.</param>
    /// <returns>Logical damage rectangles, or null when a full repaint is required.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The scale is nonpositive or non-finite.</exception>
    public IReadOnlyList<Rect>? Next(DisplayList list, float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        (previous, current) = (current, previous);
        var known = Flatten(list, current);
        var before = previous;
        var after = current;
        var size = previousSize;
        var lastScale = previousScale;
        previousSize = known ? list.Size : null;
        previousScale = scale;
        if (!known || size != list.Size || lastScale != scale) return null;

        first.Clear();
        if (next.Length < before.Count)
        {
            next = new int[before.Count * 2];
            matched = new bool[before.Count * 2];
        }
        for (var i = before.Count - 1; i >= 0; i--)
        {
            next[i] = first.TryGetValue(before[i], out var later) ? later : -1;
            first[before[i]] = i;
            matched[i] = false;
        }
        var damage = new List<Rect>();
        var last = -1;
        foreach (var drawing in after)
        {

            if (first.TryGetValue(drawing, out var index))
            {
                if (next[index] >= 0) first[drawing] = next[index];
                else first.Remove(drawing);
                matched[index] = true;
                if (index > last)
                {
                    last = index;
                    continue;
                }
            }
            Add(damage, drawing.Bounds);
        }
        for (var i = 0; i < before.Count; i++)
            if (!matched[i]) Add(damage, before[i].Bounds);
        var surface = new Rect(0, 0, list.Size.Width, list.Size.Height);
        var merged = Merge(damage, surface);
        Span<Rect> curves = stackalloc Rect[8];
        HashSet<Rect>? expandedCurves = null;
        for (var expanded = merged.Count > 0; expanded;)
        {
            expanded = false;
            foreach (var drawing in after)
            {
                if (drawing.Command is not (FillRectangle { Radius.IsZero: false } or FillGradient { Radius.IsZero: false } or FillImage { Radius.IsZero: false } or DrawBorder { Radius.IsZero: false } or DrawShadow or DrawPolyline)) continue;
                if (drawing.Command is DrawShadow shadow)
                {
                    // Preserve the mask raster origin when clipping rounded shadows or intermediate opacity layers.
                    if (drawing.Opacities is not null && merged.Any(r => !r.Inflate(2 / scale).Intersect(drawing.Bounds).IsEmpty))
                    {
                        var shadowBounds = drawing.Bounds.Inflate(shadow.Blur + 2 / scale);
                        var canonical = new Rect(0, 0, Math.Max(0, shadowBounds.Right), Math.Max(0, shadowBounds.Bottom)).Intersect(surface);
                        if (!merged.Any(r => r.Intersect(canonical) == canonical))
                        {
                            merged.Add(canonical);
                            merged = Merge(merged, surface);
                            expanded = true;
                        }
                    }
                    Corners(shadow.Rect, shadow.Radius, curves);
                    for (var corner = 0; corner < 4; corner++)
                    {
                        var edge = drawing.Transform.TransformBounds(curves[corner]).Intersect(drawing.Clip).Intersect(drawing.Bounds).Intersect(surface);
                        if (edge.IsEmpty || merged.Any(r => r.Intersect(edge) == edge)) continue;
                        if (merged.Any(r => !r.Inflate(2 / scale).Intersect(edge).IsEmpty))
                        {
                            var shadowBounds = drawing.Bounds.Inflate(shadow.Blur + 2 / scale);
                            merged.Add(new Rect(0, 0, Math.Max(0, shadowBounds.Right), Math.Max(0, shadowBounds.Bottom)));
                            merged = Merge(merged, surface);
                            expanded = true;
                            break;
                        }
                    }
                }
                var count = CurveRegions(drawing, curves);
                for (var region = 0; region < count; region++)
                {
                    if (curves[region].IsEmpty) continue;
                    var bounds = curves[region].Intersect(surface);
                    if (bounds.IsEmpty || expandedCurves?.Contains(bounds) == true) continue;
                    var covered = false;
                    var touches = false;
                    for (var i = 0; i < merged.Count; i++)
                    {
                        var rectangle = merged[i];
                        if (rectangle.Intersect(bounds) == bounds) { covered = true; break; }
                        if (!rectangle.Inflate(1 / scale).Intersect(bounds).IsEmpty) touches = true;
                    }
                    if (covered || !touches) continue;
                    // Floating-point unions can round an edge inward; expand each curve at most once.
                    (expandedCurves ??= []).Add(bounds);
                    merged.Add(bounds);
                    merged = Merge(merged, surface);
                    expanded = true;
                }
            }
        }
        return merged;
    }

    private static int CurveRegions(Drawing drawing, Span<Rect> regions)
    {
        // Intermediate opacity layers can change coverage when only part of a drawing is replayed.
        if (drawing.Opacities is not null || drawing.Command is DrawPolyline || drawing.Transform.M12 != 0 || drawing.Transform.M21 != 0)
        {
            regions[0] = drawing.Bounds;
            return 1;
        }
        var count = drawing.Command switch
        {
            FillRectangle fill => Corners(fill.Rect, fill.Radius, regions),
            FillGradient fill => Corners(fill.Rect, fill.Radius, regions),
            FillImage fill => Corners(fill.Area, fill.Radius, regions),
            DrawBorder border => Corners(border.Rect, border.Radius, regions),
            // Blur already expands its sampling area; extending its whole bounds links adjacent shadows.
            DrawShadow { Blur: 0 } shadow => Corners(shadow.Rect, shadow.Radius, regions) +
                Corners(shadow.Rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(shadow.Inset ? -shadow.Spread : shadow.Spread),
                    shadow.Radius.Inflate(shadow.Inset ? -shadow.Spread : shadow.Spread), regions[4..]),
            _ => 0,
        };
        for (var i = 0; i < count; i++) regions[i] = drawing.Transform.TransformBounds(regions[i]).Intersect(drawing.Clip);
        return count;
    }

    private static int Corners(Rect rect, CornerRadius radius, Span<Rect> regions)
    {
        regions[0] = new Rect(rect.X, rect.Y, radius.TopLeft.X, radius.TopLeft.Y);
        regions[1] = new Rect(rect.Right - radius.TopRight.X, rect.Y, radius.TopRight.X, radius.TopRight.Y);
        regions[2] = new Rect(rect.X, rect.Bottom - radius.BottomLeft.Y, radius.BottomLeft.X, radius.BottomLeft.Y);
        regions[3] = new Rect(rect.Right - radius.BottomRight.X, rect.Bottom - radius.BottomRight.Y, radius.BottomRight.X, radius.BottomRight.Y);
        return 4;
    }

    /// <summary>Forgets the last list: the next one is painted whole.</summary>
    public void Reset() => previousSize = null;

    /// <summary>
    /// The damaged rectangles in whole physical pixels, grown by one pixel for antialiasing, inside the surface.
    /// </summary>
    /// <param name="damage">The damage (IReadOnlyList&lt;Rect&gt;).</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="width">The width of the target pixel buffer.</param>
    /// <param name="height">The height of the target pixel buffer.</param>
    /// <returns>The resulting IReadOnlyList&lt;PixelRect&gt; value.</returns>
    public static IReadOnlyList<PixelRect> ToPixels(IReadOnlyList<Rect> damage, float scale, int width, int height)
    {
        var pixels = new List<PixelRect>(damage.Count);
        foreach (var rect in damage)
        {
            var left = Math.Clamp((int)MathF.Floor(rect.X * scale) - 1, 0, width);
            var top = Math.Clamp((int)MathF.Floor(rect.Y * scale) - 1, 0, height);
            var right = Math.Clamp((int)MathF.Ceiling(rect.Right * scale) + 1, 0, width);
            var bottom = Math.Clamp((int)MathF.Ceiling(rect.Bottom * scale) + 1, 0, height);
            if (right > left && bottom > top) pixels.Add(new PixelRect(left, top, right - left, bottom - top));
        }
        return pixels;
    }

    private static void Add(List<Rect> damage, Rect bounds)
    {
        if (!bounds.IsEmpty) damage.Add(bounds);
    }

    private static List<Rect> Merge(List<Rect> damage, Rect surface)
    {
        var clipped = damage.Select(r => r.Intersect(surface)).Where(r => !r.IsEmpty).ToList();
        if (clipped.Count <= 1) return clipped;
        var bounds = clipped.Aggregate(Union);
        var area = clipped.Sum(r => r.Width * r.Height);
        if (clipped.Count > MaxRectangles || area >= bounds.Width * bounds.Height * 0.7f) return [bounds];

        for (var joined = true; joined;)
        {
            joined = false;
            for (var i = 0; i < clipped.Count && !joined; i++)
                for (var j = i + 1; j < clipped.Count && !joined; j++)
                {
                    if (clipped[i].Intersect(clipped[j]).IsEmpty) continue;
                    clipped[i] = Union(clipped[i], clipped[j]);
                    clipped.RemoveAt(j);
                    joined = true;
                }
        }
        return clipped;
    }

    private static Rect Union(Rect a, Rect b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        return new Rect(left, top, Math.Max(a.Right, b.Right) - left, Math.Max(a.Bottom, b.Bottom) - top);
    }

    private sealed record Opacities(float Opacity, Opacities? Outer);

    private readonly record struct Drawing(DrawCommand Command, Matrix Transform, Rect Clip, Opacities? Opacities, Rect Bounds);

    private bool Flatten(DisplayList list, List<Drawing> drawings)
    {
        drawings.Clear();
        states.Clear();
        var transform = Matrix.Identity;
        var clip = new Rect(-1e9f, -1e9f, 2e9f, 2e9f);
        Opacities? opacities = null;
        foreach (var command in list.Commands)
        {
            switch (command)
            {
                case PushClip push:
                    states.Push((transform, clip, opacities));
                    clip = clip.Intersect(transform.TransformBounds(push.Rect));
                    continue;
                case PushOpacity push:
                    states.Push((transform, clip, opacities));
                    opacities = new Opacities(push.Opacity, opacities);
                    continue;
                case PushTranslate push:
                    states.Push((transform, clip, opacities));
                    transform = Matrix.Translation(push.X, push.Y).Then(transform);
                    continue;
                case PushTransform push:
                    states.Push((transform, clip, opacities));
                    transform = push.Matrix.Then(transform);
                    continue;
                case Pop:
                    (transform, clip, opacities) = states.Pop();
                    continue;
            }
            if (LocalBounds(command) is not { } local) return false;
            drawings.Add(new Drawing(command, transform, clip, opacities, transform.TransformBounds(local).Intersect(clip)));
        }
        return true;
    }

    private static Rect? LocalBounds(DrawCommand command) => command switch
    {
        FillRectangle fill => fill.Rect,
        DrawBorder border => border.Rect,
        FillGradient gradient => gradient.Rect,
        FillImage image => image.Area,
        DrawImage image => image.Destination,

        DrawShadow { Inset: true } shadow => shadow.Rect,
        DrawShadow shadow => shadow.Rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(shadow.Spread + shadow.Blur * 1.5f + 1),
        DrawPolyline line => PolylineBounds(line),
        DrawText text => TextBounds(text),
        _ => null,
    };

    private static Rect PolylineBounds(DrawPolyline line)
    {
        if (line.Points.Count == 0) return Rect.Empty;
        float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
        foreach (var point in line.Points)
        {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }
        return new Rect(left, top, right - left, bottom - top).Inflate(line.Thickness);
    }

    private static Rect TextBounds(DrawText text)
    {
        var layout = text.Layout;
        var start = text.Alignment switch
        {
            TextAlignment.Center => Math.Min(0, (text.Width - layout.Width) / 2),
            TextAlignment.End => Math.Min(0, text.Width - layout.Width),
            _ => 0,
        };
        var width = Math.Max(text.Width, layout.Width) - start;
        return new Rect(text.Origin.X + start, text.Origin.Y, width, layout.Height).Inflate(layout.Style.FontSize / 2 + 2 + layout.InkOutset);
    }

    private sealed class DrawingComparer : IEqualityComparer<Drawing>
    {
        public static readonly DrawingComparer Instance = new();

        public bool Equals(Drawing x, Drawing y) =>
            x.Transform == y.Transform && x.Clip == y.Clip && Equals(x.Opacities, y.Opacities) && Same(x.Command, y.Command);

        public int GetHashCode(Drawing drawing) => HashCode.Combine(Hash(drawing.Command), drawing.Transform, drawing.Clip);

        private static bool Same(DrawCommand a, DrawCommand b) => (a, b) switch
        {
            (DrawPolyline x, DrawPolyline y) => x.Thickness == y.Thickness && x.Color == y.Color && x.Points.SequenceEqual(y.Points),
            (FillGradient x, FillGradient y) => x.Rect == y.Rect && x.Radius == y.Radius && SameBrush(x.Brush, y.Brush),
            (DrawBorder x, DrawBorder y) => x.Rect == y.Rect && x.Radius == y.Radius && x.Widths == y.Widths
                && x.Top == y.Top && x.Right == y.Right && x.Bottom == y.Bottom && x.Left == y.Left
                && (x.Brush is null ? y.Brush is null : y.Brush is not null && SameBrush(x.Brush, y.Brush)),
            _ => a.Equals(b),
        };

        private static bool SameBrush(GradientBrush a, GradientBrush b) => a.Repeating == b.Repeating && ((a, b) switch
        {
            (LinearGradientBrush x, LinearGradientBrush y) => x.Start == y.Start && x.End == y.End && x.Stops.SequenceEqual(y.Stops),
            (RadialGradientBrush x, RadialGradientBrush y) => x.Center == y.Center && x.RadiusX == y.RadiusX && x.RadiusY == y.RadiusY && x.StartRadius == y.StartRadius && x.Stops.SequenceEqual(y.Stops),
            (ConicGradientBrush x, ConicGradientBrush y) => x.Center == y.Center && x.StartAngle == y.StartAngle && x.EndAngle == y.EndAngle && x.Rotation == y.Rotation && x.Stops.SequenceEqual(y.Stops),
            _ => a.Equals(b),
        });

        private static int Hash(DrawCommand command) => command switch
        {
            DrawPolyline line => HashCode.Combine(line.Thickness, line.Color, line.Points.Count),
            FillGradient gradient => HashCode.Combine(gradient.Rect, gradient.Radius, gradient.Brush.Stops.Count),
            DrawBorder border => HashCode.Combine(border.Rect, border.Radius, border.Widths, border.Top, border.Right, border.Bottom, border.Left),
            _ => command.GetHashCode(),
        };
    }
}
