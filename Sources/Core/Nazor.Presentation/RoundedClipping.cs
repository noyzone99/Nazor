using Nazor.Graphics;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private RoundedClip? InsetClip(ElementNode element, ComputedStyle style, Rect bounds)
    {
        var clip = Effective(element, style, StyleProperties.ClipPath);
        if (clip.IsNone) return null;
        var top = (float)clip.Top.Resolve(bounds.Height);
        var right = (float)clip.Right.Resolve(bounds.Width);
        var bottom = (float)clip.Bottom.Resolve(bounds.Height);
        var left = (float)clip.Left.Resolve(bounds.Width);
        var rectangle = new Rect(bounds.X + left, bounds.Y + top, Math.Max(0, bounds.Width - left - right), Math.Max(0, bounds.Height - top - bottom));
        Radius Resolve(RadiusValue value)
        {
            var x = Math.Max(0, (float)value.X.Resolve(bounds.Width));
            var y = Math.Max(0, (float)value.Y.Resolve(bounds.Height));
            return x <= 0 || y <= 0 ? Radius.Zero : new Radius(x, y);
        }
        var tl = Resolve(clip.TopLeftRadius);
        var tr = Resolve(clip.TopRightRadius);
        var br = Resolve(clip.BottomRightRadius);
        var bl = Resolve(clip.BottomLeftRadius);
        var scale = Math.Min(1, Math.Min(Math.Min(Factor(rectangle.Width, tl.X + tr.X), Factor(rectangle.Width, bl.X + br.X)),
            Math.Min(Factor(rectangle.Height, tl.Y + bl.Y), Factor(rectangle.Height, tr.Y + br.Y))));
        Radius Scale(Radius value) => new(value.X * scale, value.Y * scale);
        return new RoundedClip(rectangle, new CornerRadius(Scale(tl), Scale(tr), Scale(br), Scale(bl)));
        static float Factor(float extent, float sum) => sum > 0 ? extent / sum : 1;
    }

    private readonly record struct RoundedClip(Rect Bounds, CornerRadius Radius)
    {
        public bool Contains(Point point)
        {
            if (!Bounds.Contains(point)) return false;
            var left = point.X - Bounds.X;
            var top = point.Y - Bounds.Y;
            var right = Bounds.Right - point.X;
            var bottom = Bounds.Bottom - point.Y;
            return Inside(left, top, Radius.TopLeft) && Inside(right, top, Radius.TopRight) &&
                Inside(right, bottom, Radius.BottomRight) && Inside(left, bottom, Radius.BottomLeft);
        }

        private static bool Inside(float x, float y, Radius radius)
        {
            if (radius.IsZero || x >= radius.X || y >= radius.Y) return true;
            var dx = (x - radius.X) / radius.X;
            var dy = (y - radius.Y) / radius.Y;
            return dx * dx + dy * dy <= 1;
        }
    }
}
