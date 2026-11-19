using Nazor.Graphics;
using Nazor.Style;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private static void PaintBorderRing(Rect box, CornerRadius corners, float inset, float thickness, double start, double end, StyleColor color, DisplayListBuilder builder)
    {
        var path = new Rect(box.X + inset, box.Y + inset, box.Width - inset * 2, box.Height - inset * 2);
        if (path.IsEmpty) return;
        corners = RingCorners(corners, path);
        var (tl, tr, br, bl) = (corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft);
        var topLeft = path.X + tl.X;
        var startX = Math.Clamp(path.X + path.Width / 2, topLeft, Math.Max(topLeft, path.Right - tr.X));
        var points = new List<Point>(72) { new(startX, path.Y), new(path.Right - tr.X, path.Y) };
        Arc(path.Right - tr.X, path.Y + tr.Y, tr, -Math.PI / 2);
        points.Add(new Point(path.Right, path.Bottom - br.Y));
        Arc(path.Right - br.X, path.Bottom - br.Y, br, 0);
        points.Add(new Point(path.X + bl.X, path.Bottom));
        Arc(path.X + bl.X, path.Bottom - bl.Y, bl, Math.PI / 2);
        points.Add(new Point(path.X, path.Y + tl.Y));
        Arc(path.X + tl.X, path.Y + tl.Y, tl, Math.PI);
        points.Add(new Point(startX, path.Y));
        PaintPolyline([.. points], thickness, color, start, end, builder);

        void Arc(float centerX, float centerY, Radius corner, double from)
        {
            for (var index = 1; index <= 16; index++)
            {
                var angle = from + Math.PI / 2 * index / 16;
                points.Add(new Point(centerX + corner.X * (float)Math.Cos(angle), centerY + corner.Y * (float)Math.Sin(angle)));
            }
        }
    }

    private static CornerRadius RingCorners(CornerRadius corners, Rect box)
    {
        var scale = Math.Min(1, Math.Min(Math.Min(Factor(box.Width, corners.TopLeft.X + corners.TopRight.X), Factor(box.Width, corners.BottomLeft.X + corners.BottomRight.X)),
            Math.Min(Factor(box.Height, corners.TopLeft.Y + corners.BottomLeft.Y), Factor(box.Height, corners.TopRight.Y + corners.BottomRight.Y))));
        Radius Scale(Radius radius) => radius.IsZero ? Radius.Zero : new Radius(radius.X * scale, radius.Y * scale);
        return new CornerRadius(Scale(corners.TopLeft), Scale(corners.TopRight), Scale(corners.BottomRight), Scale(corners.BottomLeft));
        static float Factor(float extent, float sum) => sum > 0 ? extent / sum : 1;
    }
}
