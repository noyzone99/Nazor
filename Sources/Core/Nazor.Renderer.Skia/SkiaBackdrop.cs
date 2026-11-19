using Nazor.Graphics;
using SkiaSharp;

namespace Nazor.Skia;

public sealed partial class SkiaRenderer
{
    private static void ApplyBackdrop(SKCanvas canvas, PushBackdrop value)
    {
        if (!float.IsFinite(value.Opacity) || value.Opacity < 0 || value.Opacity > 1) throw new ArgumentOutOfRangeException(nameof(value));
        using var filter = value.Filters is { } operations
            ? CreateFilters(operations,SKShaderTileMode.Mirror) ?? LegacyBackdropFilter(value with { Blur=0,Saturation=1,SaturationFirst=false })
            : LegacyBackdropFilter(value);
        using var paint = new SKPaint { ColorF = new SKColorF(1, 1, 1, value.Opacity) };
        var layer = new SKCanvasSaveLayerRec { Bounds = ToSk(value.Bounds), Backdrop = filter, Paint = paint };
        canvas.SaveLayer(in layer);
        canvas.Save();
        using var shape = RoundRect(value.Rect, value.Radius);
        canvas.ClipRoundRect(shape, SKClipOperation.Difference, true);
        canvas.Clear(SKColors.Transparent);
        canvas.Restore();
    }

    private static SKImageFilter LegacyBackdropFilter(PushBackdrop value)
    {
        if (!float.IsFinite(value.Blur) || value.Blur < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (!float.IsFinite(value.Saturation) || value.Saturation < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var saturation = value.Saturation;
        var inverse = 1 - saturation;
        using var colors = SKColorFilter.CreateColorMatrix(new float[]
        {
            .213f * inverse + saturation, .715f * inverse, .072f * inverse, 0, 0,
            .213f * inverse, .715f * inverse + saturation, .072f * inverse, 0, 0,
            .213f * inverse, .715f * inverse, .072f * inverse + saturation, 0, 0,
            0, 0, 0, 1, 0,
        });
        var sigma = Math.Min(value.Blur, 256);
        using var first = value.SaturationFirst
            ? SKImageFilter.CreateColorFilter(colors)
            : SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Mirror);
        return value.SaturationFirst
            ? SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Mirror, first)
            : SKImageFilter.CreateColorFilter(colors, first);
    }
}
