using System.Globalization;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Skia;
using SkiaSharp;

namespace Nazor.Platform.Windows;

internal static class WindowsBadgeRenderer
{
    internal static RenderedImage Render(int? count, RenderedImage? image, WindowsBadgeAppearance style, int size)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(size / 16f);
        var transform = style.Transform;
        canvas.Concat(new SKMatrix(transform.M11, transform.M21, transform.OffsetX, transform.M12, transform.M22, transform.OffsetY, 0, 0, 1));
        if (style.Mask is not null) canvas.SaveLayer();
        using var decorations = new SkiaRenderer();
        var width = Math.Clamp(style.Width, 0, 16);
        var height = Math.Clamp(style.Height, 0, 16);
        var rect = new Rect((16 - width) / 2 + style.Offset.X, (16 - height) / 2 + style.Offset.Y, width, height);
        using var box = RoundRect(rect, style.Radius);
        if (width > 0 && height > 0)
        {
            var border = style.BorderWidths ?? new Thickness(style.BorderWidth, style.BorderWidth, style.BorderWidth, style.BorderWidth);
            border = new(Math.Clamp(border.Left, 0, width / 2), Math.Clamp(border.Top, 0, height / 2),
                Math.Clamp(border.Right, 0, width / 2), Math.Clamp(border.Bottom, 0, height / 2));
            foreach (var shadow in style.Shadows.Reverse().Where(shadow => !shadow.Inset)) DrawShadow(canvas, rect, style.Radius, shadow);
            using var paint = new SKPaint { IsAntialias = true, Color = ToSk(count is null
                ? style.ImageBackgroundColor ?? style.BackgroundColor ?? default
                : style.BackgroundColor ?? new Color(196, 43, 28, 255)) };
            using var backgroundBox = style.ClipBackgroundToPadding ? RoundRect(rect.Deflate(border), style.Radius.Deflate(border)) : RoundRect(rect, style.Radius);
            canvas.DrawRoundRect(backgroundBox, paint);
            if (style.Background is { } background) decorations.Draw(canvas, background);
            foreach (var shadow in style.Shadows.Reverse().Where(shadow => shadow.Inset)) DrawShadow(canvas, rect, style.Radius, shadow);
            var side = new BorderSide(style.BorderColor, style.BorderStyle);
            decorations.DrawBorderSides(canvas, new(rect, style.Radius, border,
                style.BorderTop ?? side, style.BorderRight ?? side, style.BorderBottom ?? side, style.BorderLeft ?? side));
            var padding = style.Padding;
            var content = new SKRect(rect.X + border.Left + padding.Left, rect.Y + border.Top + padding.Top,
                rect.X + width - border.Right - padding.Right, rect.Y + height - border.Bottom - padding.Bottom);
            if (content.Width > 0 && content.Height > 0)
            {
                canvas.Save();
                if (count is { } number)
                {
                    canvas.ClipRect(content);
                    DrawCount(canvas, number, content, style);
                }
                else if (image is not null)
                {
                    if (style.ClipImage) canvas.ClipRoundRect(box, antialias: true);
                    DrawImage(canvas, image, content, style, size / 16f);
                }
                canvas.Restore();
            }
        }
        if (style.Mask is { } mask)
        {
            var maskPixels = decorations.RenderToImage(mask, size / 16f, default);
            using var alpha = new SKBitmap(new SKImageInfo(maskPixels.Width, maskPixels.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            Marshal.Copy(maskPixels.Pixels, 0, alpha.GetPixels(), maskPixels.Pixels.Length);
            using var maskPaint = new SKPaint { BlendMode = SKBlendMode.DstIn };
            canvas.DrawBitmap(alpha, new SKRect(0, 0, 16, 16), new SKSamplingOptions(SKFilterMode.Linear), maskPaint);
            canvas.Restore();
        }
        canvas.Flush();
        var pixels = new byte[checked(size * size * 4)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        var opacity = Math.Clamp(style.Opacity, 0, 1);
        if (opacity < 1)
            for (var index = 0; index < pixels.Length; index++) pixels[index] = (byte)MathF.Round(pixels[index] * opacity);
        return new(size, size, pixels);
    }

    private static void DrawCount(SKCanvas canvas, int count, SKRect content, WindowsBadgeAppearance style)
    {
        using var fontStyle = new SKFontStyle(style.FontWeight, (int)SKFontStyleWidth.Normal, style.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var systemFace = style.Fonts is null ? style.FontFamilies.Select(family => SKFontManager.Default.MatchFamily(family, fontStyle)).FirstOrDefault(face => face is not null)
            ?? SKTypeface.FromFamilyName(null, fontStyle) : null;
        FontFace? primaryFace = null;
        SKTypeface Typeface(float size)
        {
            if (style.Fonts is null) return systemFace!;
            primaryFace = style.Fonts.Match(style.FontFamilies, style.FontWeight, style.Italic, Math.Max(0.01f, size));
            return primaryFace is SkiaFontFace face ? face.Typeface : throw new NotSupportedException("Windows badges require a Skia font provider.");
        }
        using var font = new SKFont(Typeface(style.FontSize), Math.Max(0, style.FontSize));
        using var fallbackFont = new SKFont();
        SKFont GlyphFont(char character)
        {
            if (primaryFace is null || primaryFace.HasGlyph(character) || style.Fonts!.Fallback(character, primaryFace) is not SkiaFontFace fallback) return font;
            fallbackFont.Typeface = fallback.Typeface;
            fallbackFont.Size = font.Size;
            return fallbackFont;
        }
        var text = count.ToString(CultureInfo.InvariantCulture);
        var spacing = style.LetterSpacing;
        float Measure() => text.Sum(character => GlyphFont(character).MeasureText(character.ToString())) + spacing * (text.Length - 1);
        var width = Measure();
        if (style.TextFit && font.Size > 0)
        {
            var metrics = font.Metrics;
            var factor = Math.Min(1, Math.Min(content.Width * 0.85f / Math.Max(0.01f, width), content.Height / Math.Max(0.01f, metrics.Descent - metrics.Ascent)));
            font.Size *= factor;
            font.Typeface = Typeface(font.Size);
            spacing *= factor;
            width = Measure();
        }
        var x = content.Left + (content.Width - width) * style.TextAlign;
        var fm = font.Metrics;
        var y = content.Top - fm.Ascent + (content.Height - fm.Descent + fm.Ascent) * style.VerticalAlign;
        using var paint = new SKPaint { IsAntialias = true };
        void Draw(float offsetX, float offsetY)
        {
            var cursor = x + offsetX;
            foreach (var character in text)
            {
                var glyph = character.ToString();
                var glyphFont = GlyphFont(character);
                canvas.DrawText(glyph, cursor, y + offsetY, SKTextAlign.Left, glyphFont, paint);
                cursor += glyphFont.MeasureText(glyph) + spacing;
            }
        }
        foreach (var shadow in style.TextShadows.Reverse())
        {
            using var blur = shadow.Blur > 0 ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur / 2) : null;
            paint.MaskFilter = blur;
            paint.Color = ToSk(shadow.Color);
            Draw(shadow.Offset.X, shadow.Offset.Y);
        }
        paint.MaskFilter = null;
        if (style.TextStrokeWidth > 0)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = style.TextStrokeWidth;
            paint.Color = ToSk(style.TextStrokeColor);
            Draw(0, 0);
        }
        paint.Style = SKPaintStyle.Fill;
        paint.Color = ToSk(style.Color);
        Draw(0, 0);
    }

    private static void DrawImage(SKCanvas canvas, RenderedImage image, SKRect content, WindowsBadgeAppearance style, float scale)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        Marshal.Copy(image.Pixels, 0, bitmap.GetPixels(), image.Pixels.Length);
        var width = image.Width / scale;
        var height = image.Height / scale;
        var factor = style.ImageFit switch
        {
            WindowsBadgeImageFit.Cover => Math.Max(content.Width / width, content.Height / height),
            WindowsBadgeImageFit.None => 1,
            WindowsBadgeImageFit.ScaleDown => Math.Min(1, Math.Min(content.Width / width, content.Height / height)),
            _ => Math.Min(content.Width / width, content.Height / height),
        };
        width = style.ImageFit == WindowsBadgeImageFit.Fill ? content.Width : width * factor;
        height = style.ImageFit == WindowsBadgeImageFit.Fill ? content.Height : height * factor;
        var position = style.ImagePositionResolver?.Invoke(new(content.Width - width, content.Height - height))
            ?? new Point((content.Width - width) * style.ImagePosition.X + style.ImageOffset.X,
                (content.Height - height) * style.ImagePosition.Y + style.ImageOffset.Y);
        var left = content.Left + position.X;
        var top = content.Top + position.Y;
        canvas.ClipRect(content);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)MathF.Round(Math.Clamp(style.ImageOpacity, 0, 1) * 255)) };
        canvas.DrawBitmap(bitmap, new SKRect(left, top, left + width, top + height), new SKSamplingOptions(SKFilterMode.Linear), paint);
    }

    private static void DrawShadow(SKCanvas canvas, Rect rect, CornerRadius radius, DrawShadow shadow)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = ToSk(shadow.Color) };
        using var blur = shadow.Blur > 0 ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur / 2) : null;
        paint.MaskFilter = blur;
        using var box = RoundRect(rect, radius);
        canvas.Save();
        if (!shadow.Inset)
        {
            using var shape = RoundRect(rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(shadow.Spread), radius.Inflate(shadow.Spread));
            canvas.ClipRoundRect(box, SKClipOperation.Difference, antialias: true);
            canvas.DrawRoundRect(shape, paint);
        }
        else
        {
            using var hole = RoundRect(rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(-shadow.Spread), radius.Inflate(-shadow.Spread));
            var margin = shadow.Blur + Math.Abs(shadow.Spread) + Math.Abs(shadow.Offset.X) + Math.Abs(shadow.Offset.Y) + 1;
            using var outer = RoundRect(rect.Inflate(margin), default);
            canvas.ClipRoundRect(box, antialias: true);
            canvas.DrawRoundRectDifference(outer, hole, paint);
        }
        canvas.Restore();
    }

    private static SKColor ToSk(Color color) => new(color.R, color.G, color.B, color.A);
    private static SKRoundRect RoundRect(Rect rect, CornerRadius radius)
    {
        var result = new SKRoundRect();
        result.SetRectRadii(new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height),
        [new(radius.TopLeft.X, radius.TopLeft.Y), new(radius.TopRight.X, radius.TopRight.Y),
            new(radius.BottomRight.X, radius.BottomRight.Y), new(radius.BottomLeft.X, radius.BottomLeft.Y)]);
        return result;
    }
}
