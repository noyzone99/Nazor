using Nazor.Graphics;
using SkiaSharp;

namespace Nazor.Platform;

internal static class WindowFrameShadow
{
    internal static SKBitmap? Create(Size frameSize, float scale, CornerRadius radius, IReadOnlyList<DrawShadow> shadows, out int margin)
    {
        margin = 0;
        if (shadows.Count == 0) return null;

        var extent = shadows.Max(shadow => Math.Max(Math.Abs(shadow.Offset.X), Math.Abs(shadow.Offset.Y)) + shadow.Spread + shadow.Blur * 2);
        margin = (int)Math.Ceiling(Math.Clamp(float.IsFinite(extent) ? extent : 128, 1, 128));
        var pixelMargin = (int)Math.Ceiling(margin * scale);
        var width = (int)Math.Ceiling(frameSize.Width * scale) + pixelMargin * 2;
        var height = (int)Math.Ceiling(frameSize.Height * scale) + pixelMargin * 2;
        if (width <= 0 || height <= 0 || (long)width * height > 16 * 1024 * 1024) return null;

        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        if (bitmap.GetPixels() == 0)
        {
            bitmap.Dispose();
            return null;
        }

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(pixelMargin, pixelMargin);
        canvas.Scale(scale);
        var frame = new SKRect(0, 0, frameSize.Width, frameSize.Height);
        using var clip = RoundRect(frame, radius);
        canvas.ClipRoundRect(clip, SKClipOperation.Difference, antialias: true);
        for (var i = shadows.Count - 1; i >= 0; i--)
        {
            var shadow = shadows[i];
            var bounds = frame;
            bounds.Inflate(shadow.Spread, shadow.Spread);
            bounds.Offset(shadow.Offset.X, shadow.Offset.Y);
            if (bounds.IsEmpty || !float.IsFinite(bounds.Left + bounds.Top + bounds.Right + bounds.Bottom)) continue;
            using var blur = shadow.Blur > 0 ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, Math.Min(shadow.Blur / 2, 64)) : null;
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(shadow.Color.R, shadow.Color.G, shadow.Color.B, shadow.Color.A),
                MaskFilter = blur,
            };
            using var shape = RoundRect(bounds, radius.Inflate(shadow.Spread));
            canvas.DrawRoundRect(shape, paint);
        }
        return bitmap;
    }

    internal static SKRoundRect RoundRect(SKRect rect, CornerRadius radius, float scale = 1)
    {
        SKPoint ToSkia(Radius corner) => new(corner.X * scale, corner.Y * scale);
        var result = new SKRoundRect();
        result.SetRectRadii(rect, [ToSkia(radius.TopLeft), ToSkia(radius.TopRight), ToSkia(radius.BottomRight), ToSkia(radius.BottomLeft)]);
        return result;
    }
}
