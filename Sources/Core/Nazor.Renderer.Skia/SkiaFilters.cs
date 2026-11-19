using Nazor.Graphics;
using SkiaSharp;

namespace Nazor.Skia;

public sealed partial class SkiaRenderer
{
    private static SKImageFilter? CreateFilters(IReadOnlyList<ImageFilter> filters, SKShaderTileMode edges)
    {
        if (filters.Count > 64) throw new ArgumentException("A filter chain supports at most64 operations.", nameof(filters));
        SKImageFilter? current=null;
        try
        {
            foreach (var operation in filters)
            {
                using var input=current; current=null;
                switch (operation)
                {
                    case GaussianBlurFilter blur:
                        if (!float.IsFinite(blur.Sigma) || blur.Sigma < 0) throw new ArgumentOutOfRangeException(nameof(filters));
                        current=SKImageFilter.CreateBlur(Math.Min(blur.Sigma,256),Math.Min(blur.Sigma,256),edges,input);
                        break;
                    case ColorMatrixFilter matrix:
                        var values=matrix.Values.ToArray();
                        using (var color=SKColorFilter.CreateColorMatrix(values)) current=SKImageFilter.CreateColorFilter(color,input);
                        break;
                    default: throw new NotSupportedException($"Unsupported image filter {operation?.GetType().Name}.");
                }
                if (current is null) throw new InvalidOperationException("Unable to create an image filter.");
            }
            return current;
        }
        catch { current?.Dispose(); throw; }
    }

    private static void ApplyFilter(SKCanvas canvas,PushFilter value)
    {
        if (!float.IsFinite(value.Opacity) || value.Opacity < 0 || value.Opacity > 1) throw new ArgumentOutOfRangeException(nameof(value));
        using var filter=CreateFilters(value.Filters,SKShaderTileMode.Decal);
        using var paint=new SKPaint { ColorF=new SKColorF(1,1,1,value.Opacity),ImageFilter=filter };
        canvas.SaveLayer(ToSk(value.Bounds),paint);
    }
}
