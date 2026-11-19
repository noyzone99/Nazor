using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Presentation;

public sealed partial class Scene
{
    private static float FilterOutset(FilterValue value) => value.Functions.Items.Where(function => function.Kind == FilterKind.Blur)
        .Sum(function => (float)Math.Clamp(function.Radius.Resolve(0), 0, 256) * 3);

    private static IReadOnlyList<ImageFilter> FilterOperations(FilterValue value) => value.Functions.Items.Select<FilterFunction,ImageFilter>(function =>
    {
        if (!Enum.IsDefined(function.Kind) || !double.IsFinite(function.Amount) || function.Kind != FilterKind.HueRotate && function.Amount < 0)
            throw new ArgumentException("Filter operations require a supported kind and finite nonnegative amount.", nameof(value));
        if (function.Kind == FilterKind.Blur) return new GaussianBlurFilter((float)Math.Clamp(function.Radius.Resolve(0),0,256));
        var amount = function.Kind == FilterKind.HueRotate ? function.Amount % 360 : Math.Min(function.Amount,1e20);
        double[] matrix = [1,0,0,0,0, 0,1,0,0,0, 0,0,1,0,0, 0,0,0,1,0];
        switch (function.Kind)
        {
            case FilterKind.Brightness:
                matrix[0]=matrix[6]=matrix[12]=amount; break;
            case FilterKind.Contrast:
                matrix[0]=matrix[6]=matrix[12]=amount;
                matrix[4]=matrix[9]=matrix[14]=.5*(1-amount); break;
            case FilterKind.Invert:
                amount=Math.Min(amount,1); matrix[0]=matrix[6]=matrix[12]=1-2*amount;
                matrix[4]=matrix[9]=matrix[14]=amount; break;
            case FilterKind.Opacity:
                matrix[18]=Math.Min(amount,1); break;
            case FilterKind.Grayscale or FilterKind.Saturate:
                var saturation=function.Kind==FilterKind.Grayscale ? 1-Math.Min(amount,1) : amount;
                var inverse=1-saturation;
                var r=function.Kind==FilterKind.Grayscale ? .2126 : .213;
                var g=function.Kind==FilterKind.Grayscale ? .7152 : .715;
                var b=function.Kind==FilterKind.Grayscale ? .0722 : .072;
                matrix=[r*inverse+saturation,g*inverse,b*inverse,0,0,
                    r*inverse,g*inverse+saturation,b*inverse,0,0,
                    r*inverse,g*inverse,b*inverse+saturation,0,0,0,0,0,1,0]; break;
            case FilterKind.Sepia:
                amount=Math.Min(amount,1);
                matrix=[1-.607*amount,.769*amount,.189*amount,0,0,
                    .349*amount,1-.314*amount,.168*amount,0,0,
                    .272*amount,.534*amount,1-.869*amount,0,0,0,0,0,1,0]; break;
            case FilterKind.HueRotate:
                var c=Math.Cos(amount*Math.PI/180); var s=Math.Sin(amount*Math.PI/180);
                matrix=[.213+.787*c-.213*s,.715-.715*c-.715*s,.072-.072*c+.928*s,0,0,
                    .213-.213*c+.143*s,.715+.285*c+.140*s,.072-.072*c-.283*s,0,0,
                    .213-.213*c-.787*s,.715-.715*c+.715*s,.072+.928*c+.072*s,0,0,0,0,0,1,0]; break;
        }
        return new ColorMatrixFilter(matrix.Select(coefficient=>(float)coefficient));
    }).ToArray();
}
