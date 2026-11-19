using Nazor.Graphics;
using Nazor.Style;
using GraphicsColor = Nazor.Graphics.Color;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Presentation;

internal static class GradientGeometry
{
    public static GradientBrush? Resolve(Gradient gradient, Rect box, StyleColor currentColor) => gradient switch
    {
        LinearGradient linear => Linear(linear, box, currentColor),
        RadialGradient radial => Radial(radial, box, currentColor),
        ConicGradient conic => Conic(conic, box, currentColor),
        _ => null,
    };

    private static LinearGradientBrush Linear(LinearGradient gradient, Rect box, StyleColor currentColor)
    {
        float dx, dy;
        if (gradient.Angle is { } degrees)
        {
            var radians = degrees % 360 * Math.PI / 180;
            (dx, dy) = ((float)Math.Sin(radians), (float)-Math.Cos(radians));
        }
        else
        {
            (dx, dy) = gradient.ToX != 0 && gradient.ToY != 0 ? (gradient.ToX * box.Height, gradient.ToY * box.Width) : (gradient.ToX, gradient.ToY);
            var norm = MathF.Sqrt(dx * dx + dy * dy);
            if (norm > 0) (dx, dy) = (dx / norm, dy / norm);
        }
        var length = MathF.Abs(box.Width * dx) + MathF.Abs(box.Height * dy);
        var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
        var start = new Point(center.X - dx * length / 2, center.Y - dy * length / 2);
        var end = new Point(center.X + dx * length / 2, center.Y + dy * length / 2);
        var (stops, first, last) = Stops(gradient.Stops.Items, length, currentColor);

        if (gradient.Repeating && last <= first)
            return new LinearGradientBrush(start, end, Solid(Average(gradient, stops, currentColor, evenly: true)));
        if (gradient.Repeating || first < 0 || last > 1)
        {
            Point At(float t) => new(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
            (start, end) = (At(first), At(last));
            stops = Remap(stops, first, last);
        }
        return new LinearGradientBrush(start, end, stops) { Repeating = gradient.Repeating };
    }

    private static RadialGradientBrush Radial(RadialGradient gradient, Rect box, StyleColor currentColor)
    {
        var cx = box.X + (float)gradient.CenterX.Resolve(box.Width);
        var cy = box.Y + (float)gradient.CenterY.Resolve(box.Height);
        float left = cx - box.X, right = box.Right - cx, top = cy - box.Y, bottom = box.Bottom - cy;
        var circle = gradient.Shape == RadialShape.Circle;
        float rx, ry;
        switch (gradient.Extent)
        {
            case RadialExtent.Explicit:
                rx = (float)gradient.SizeX.Resolve(box.Width);
                ry = circle ? rx : (float)gradient.SizeY.Resolve(box.Height);
                break;
            case RadialExtent.ClosestSide or RadialExtent.FarthestSide:
            {
                var closest = gradient.Extent == RadialExtent.ClosestSide;
                var sideX = closest ? MathF.Min(MathF.Abs(left), MathF.Abs(right)) : MathF.Max(MathF.Abs(left), MathF.Abs(right));
                var sideY = closest ? MathF.Min(MathF.Abs(top), MathF.Abs(bottom)) : MathF.Max(MathF.Abs(top), MathF.Abs(bottom));
                (rx, ry) = circle ? (closest ? MathF.Min(sideX, sideY) : MathF.Max(sideX, sideY), 0) : (sideX, sideY);
                if (circle) ry = rx;
                break;
            }
            default:
            {

                var closest = gradient.Extent == RadialExtent.ClosestCorner;
                var cornerX = closest ? MathF.Min(MathF.Abs(left), MathF.Abs(right)) : MathF.Max(MathF.Abs(left), MathF.Abs(right));
                var cornerY = closest ? MathF.Min(MathF.Abs(top), MathF.Abs(bottom)) : MathF.Max(MathF.Abs(top), MathF.Abs(bottom));
                if (circle || cornerX <= 0 || cornerY <= 0)
                {
                    rx = ry = MathF.Sqrt(cornerX * cornerX + cornerY * cornerY);
                }
                else
                {

                    rx = cornerX * MathF.Sqrt(2);
                    ry = cornerY * MathF.Sqrt(2);
                }
                break;
            }
        }
        var (stops, first, last) = Stops(gradient.Stops.Items, rx, currentColor);
        if (gradient.Repeating)
        {
            var span = last - first;
            if (span <= 0 || rx <= 0 || ry <= 0)
                return new RadialGradientBrush(new Point(cx, cy), rx, ry, Solid(Average(gradient, stops, currentColor, evenly: span <= 0)));
            var phase = first - MathF.Floor(first / span) * span;
            return new RadialGradientBrush(new Point(cx, cy), rx * (phase + span), ry * (phase + span), Remap(stops, first, last))
                { StartRadius = rx * phase, Repeating = true };
        }

        if (last > 1)
        {
            rx *= last;
            ry *= last;
            stops = Remap(stops, 0, last);
        }
        return new RadialGradientBrush(new Point(cx, cy), rx, ry, stops.Select(s => s with { Offset = Math.Clamp(s.Offset, 0, 1) }).ToArray());
    }

    private static ConicGradientBrush Conic(ConicGradient gradient, Rect box, StyleColor currentColor)
    {
        var center = new Point(box.X + (float)gradient.CenterX.Resolve(box.Width), box.Y + (float)gradient.CenterY.Resolve(box.Height));
        var (stops, first, last) = Stops(gradient.Stops.Items, 1, currentColor);
        if (last <= first)
        {
            if (gradient.Repeating) stops = Solid(Average(gradient, stops, currentColor, evenly: true));
            else if (first <= 0) stops = Solid(stops[^1].Color);
            else if (first >= 1) stops = Solid(stops[0].Color);
            else stops = [new(0, stops[0].Color), new(first, stops[0].Color), new(first, stops[^1].Color), new(1, stops[^1].Color)];
            return new ConicGradientBrush(center, 0, 360, stops) { Rotation = (float)(gradient.Angle % 360) };
        }
        return new ConicGradientBrush(center, first * 360, last * 360, Remap(stops, first, last))
            { Rotation = (float)(gradient.Angle % 360), Repeating = gradient.Repeating };
    }

    private static List<GradientStop> Solid(GraphicsColor color) => [new(0, color), new(1, color)];

    private static GraphicsColor Average(Gradient gradient, IReadOnlyList<GradientStop> stops, StyleColor currentColor, bool evenly)
    {
        if (evenly)
            stops = gradient.Stops.Items.Select((stop, index) =>
            {
                var c = stop.Color.IsCurrentColor ? currentColor : stop.Color;
                return new GradientStop((float)index / (gradient.Stops.Items.Count - 1), new GraphicsColor(c.R, c.G, c.B, c.A));
            }).ToArray();
        var span = stops[^1].Offset - stops[0].Offset;
        double r = 0, g = 0, b = 0, a = 0;
        for (var i = 1; i < stops.Count; i++)
        {
            var weight = (stops[i].Offset - stops[i - 1].Offset) / span / 2;
            foreach (var color in new[] { stops[i - 1].Color, stops[i].Color })
            {
                var alpha = color.A / 255d * weight;
                a += alpha; r += color.R * alpha; g += color.G * alpha; b += color.B * alpha;
            }
        }
        return a <= 0 ? GraphicsColor.Transparent : new GraphicsColor((byte)Math.Clamp(Math.Round(r / a), 0, 255),
            (byte)Math.Clamp(Math.Round(g / a), 0, 255), (byte)Math.Clamp(Math.Round(b / a), 0, 255), (byte)Math.Clamp(Math.Round(a * 255), 0, 255));
    }

    private static (List<GradientStop> Stops, float First, float Last) Stops(IReadOnlyList<ColorStop> stops, float length, StyleColor currentColor)
    {
        var count = stops.Count;
        var offsets = new float?[count];
        for (var i = 0; i < count; i++)
            if (stops[i].Position is { } position)
                offsets[i] = position.Unit == LengthUnit.Percent ? (float)position.Value / 100 : length > 0 ? (float)position.Resolve(length) / length : 0;
        offsets[0] ??= 0;
        offsets[count - 1] ??= 1;
        var max = offsets[0]!.Value;
        for (var i = 1; i < count; i++)
        {
            if (offsets[i] is not { } offset) continue;
            if (offset < max) offsets[i] = max;
            else max = offset;
        }
        for (var i = 1; i < count; i++)
        {
            if (offsets[i] is not null) continue;
            var next = i + 1;
            while (offsets[next] is null) next++;
            var from = offsets[i - 1]!.Value;
            var to = offsets[next]!.Value;
            for (var k = i; k < next; k++) offsets[k] = from + (to - from) * (k - i + 1) / (next - i + 1);
        }
        GraphicsColor ColorOf(int i)
        {
            var color = stops[i].Color.IsCurrentColor ? currentColor : stops[i].Color;
            return new GraphicsColor(color.R, color.G, color.B, color.A);
        }
        var result = new List<GradientStop>();
        for (var i = 0; i < count; i++)
        {
            var color = ColorOf(i);
            var offset = offsets[i]!.Value;
            if (color.A > 0)
            {
                result.Add(new GradientStop(offset, color));
                continue;
            }
            if (i > 0) result.Add(new GradientStop(offset, ColorOf(i - 1) with { A = 0 }));
            if (i < count - 1) result.Add(new GradientStop(offset, ColorOf(i + 1) with { A = 0 }));
            if (count == 1) result.Add(new GradientStop(offset, color));
        }
        return (result, offsets[0]!.Value, offsets[count - 1]!.Value);
    }

    private static List<GradientStop> Remap(List<GradientStop> stops, float first, float last)
    {
        var span = last - first;
        return span <= 0 ? stops : stops.Select(s => s with { Offset = (s.Offset - first) / span }).ToList();
    }
}
