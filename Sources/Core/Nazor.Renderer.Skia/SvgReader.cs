using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Nazor.Graphics;
using SkiaSharp;

namespace Nazor.Skia;

internal sealed class SkiaVectorImage(SKPicture picture, Size size) : DecodedImage, IDisposable
{
    private readonly Lazy<SKImage?> smallImage = new(() =>
    {
        // Keep several samples per device pixel for icons, even on high-DPI displays.
        const int resolution = 128;
        var longest = Math.Max(size.Width, size.Height);
        var width = Math.Clamp((int)MathF.Ceiling(size.Width / longest * resolution), 1, resolution);
        var height = Math.Clamp((int)MathF.Ceiling(size.Height / longest * resolution), 1, resolution);
        return SKImage.FromPicture(picture, new SKSizeI(width, height), SKMatrix.CreateScale(width / size.Width, height / size.Height));
    });

    public SKPicture Picture { get; } = picture;

    public override Size Size { get; } = size;

    public override bool IsVector => true;

    public SKImage? SmallImage => smallImage.Value;

    public void Dispose()
    {
        if (smallImage.IsValueCreated) smallImage.Value?.Dispose();
        Picture.Dispose();
    }
}

internal static partial class SvgReader
{
    private static readonly Dictionary<string, SKColor> Named = typeof(SKColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(SKColor))
        .ToDictionary(f => f.Name.ToLowerInvariant(), f => (SKColor)f.GetValue(null)!);

    public static SkiaVectorImage? Read(ReadOnlySpan<byte> data)
    {
        XElement? root;
        try
        {

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using var reader = XmlReader.Create(new MemoryStream(data.ToArray()), settings);
            root = XDocument.Load(reader).Root;
        }
        catch (XmlException)
        {
            return null;
        }
        if (root is null || root.Name.LocalName != "svg") return null;
        var viewBox = ViewBox(root.Attribute("viewBox")?.Value);
        var width = Number(root.Attribute("width")?.Value);
        var height = Number(root.Attribute("height")?.Value);

        if (viewBox is { Width: > 0, Height: > 0 } box)
        {
            if (width is null && height is { } h) width = h * box.Width / box.Height;
            if (height is null && width is { } w) height = w * box.Height / box.Width;
        }
        var size = new SKSize(width ?? viewBox?.Width ?? 300, height ?? viewBox?.Height ?? 150);
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(new SKRect(0, 0, size.Width, size.Height));
        if (viewBox is { Width: > 0, Height: > 0 } view)
        {
            var scale = Math.Min(size.Width / view.Width, size.Height / view.Height);
            canvas.Translate((size.Width - view.Width * scale) / 2, (size.Height - view.Height * scale) / 2);
            canvas.Scale(scale);
            canvas.Translate(-view.Left, -view.Top);
        }
        foreach (var child in root.Elements()) Draw(canvas, child, Paint.Initial.With(Properties(root)));
        return new SkiaVectorImage(recorder.EndRecording(), new Size(size.Width, size.Height));
    }

    private static void Draw(SKCanvas canvas, XElement element, Paint inherited)
    {
        var name = element.Name.LocalName;
        if (name is not ("g" or "a" or "svg" or "path" or "rect" or "circle" or "ellipse" or "line" or "polyline" or "polygon")) return;
        var properties = Properties(element);
        if (properties.GetValueOrDefault("display") == "none") return;
        var paint = inherited.With(properties);
        var opacity = Math.Clamp(Number(properties.GetValueOrDefault("opacity")) ?? 1, 0, 1);
        canvas.Save();
        if (Transform(element.Attribute("transform")?.Value) is { } transform) canvas.Concat(in transform);
        if (opacity < 1)
        {
            using var layer = new SKPaint { Color = new SKColor(0, 0, 0, (byte)Math.Round(opacity * 255)) };
            canvas.SaveLayer(layer);
        }
        if (name is "g" or "a" or "svg")
        {
            foreach (var child in element.Elements()) Draw(canvas, child, paint);
        }
        else if (paint.Visible && Shape(element, name) is { } path)
        {
            using (path)
            {
                path.FillType = paint.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
                if (paint.Fill is { } fill && name is not "line")
                {
                    using var brush = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill.WithAlpha((byte)Math.Round(fill.Alpha * paint.FillOpacity)) };
                    canvas.DrawPath(path, brush);
                }
                if (paint.Stroke is { } stroke && paint.StrokeWidth > 0)
                {
                    using var pen = new SKPaint
                    {
                        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = paint.StrokeWidth, StrokeCap = paint.Cap, StrokeJoin = paint.Join,
                        StrokeMiter = paint.MiterLimit, Color = stroke.WithAlpha((byte)Math.Round(stroke.Alpha * paint.StrokeOpacity)),
                    };
                    canvas.DrawPath(path, pen);
                }
            }
        }
        canvas.Restore();
        if (opacity < 1) canvas.Restore();
    }

    private static SKPath? Shape(XElement element, string name)
    {
        float Get(string attribute, float fallback = 0) => Number(element.Attribute(attribute)?.Value) ?? fallback;
        if (name == "path") return element.Attribute("d")?.Value is { Length: > 0 } d ? SKPath.ParseSvgPathData(d) : null;
        using var builder = new SKPathBuilder();
        switch (name)
        {
            case "rect":
            {
                var rect = SKRect.Create(Get("x"), Get("y"), Get("width"), Get("height"));
                if (rect.Width <= 0 || rect.Height <= 0) return null;
                var rx = Number(element.Attribute("rx")?.Value);
                var ry = Number(element.Attribute("ry")?.Value);
                var (x, y) = (Math.Min(rx ?? ry ?? 0, rect.Width / 2), Math.Min(ry ?? rx ?? 0, rect.Height / 2));
                if (x > 0 && y > 0) builder.AddRoundRect(new SKRoundRect(rect, x, y));
                else builder.AddRect(rect);
                break;
            }
            case "circle":
                if (Get("r") <= 0) return null;
                builder.AddCircle(Get("cx"), Get("cy"), Get("r"));
                break;
            case "ellipse":
                if (Get("rx") <= 0 || Get("ry") <= 0) return null;
                builder.AddOval(new SKRect(Get("cx") - Get("rx"), Get("cy") - Get("ry"), Get("cx") + Get("rx"), Get("cy") + Get("ry")));
                break;
            case "line":
                builder.MoveTo(Get("x1"), Get("y1"));
                builder.LineTo(Get("x2"), Get("y2"));
                break;
            default:
                var numbers = Numbers(element.Attribute("points")?.Value);
                if (numbers.Count < 4) return null;
                builder.MoveTo(numbers[0], numbers[1]);
                for (var i = 2; i + 1 < numbers.Count; i += 2) builder.LineTo(numbers[i], numbers[i + 1]);
                if (name == "polygon") builder.Close();
                break;
        }
        return builder.Detach();
    }

    private static Dictionary<string, string> Properties(XElement element)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in element.Attributes())
            if (attribute.Name.NamespaceName.Length == 0) properties[attribute.Name.LocalName] = attribute.Value.Trim();
        if (element.Attribute("style")?.Value is { } style)
            foreach (var declaration in style.Split(';'))
                if (declaration.Split(':', 2) is [var key, var value]) properties[key.Trim().ToLowerInvariant()] = value.Trim();
        return properties;
    }

    private sealed record Paint(SKColor? Fill, float FillOpacity, bool EvenOdd, SKColor? Stroke, float StrokeOpacity, float StrokeWidth,
        SKStrokeCap Cap, SKStrokeJoin Join, float MiterLimit, bool Visible)
    {
        public static readonly Paint Initial = new(SKColors.Black, 1, false, null, 1, 1, SKStrokeCap.Butt, SKStrokeJoin.Miter, 4, true);

        public Paint With(Dictionary<string, string> p) => new(
            p.TryGetValue("fill", out var fill) ? Color(fill, Fill) : Fill,
            Opacity(p.GetValueOrDefault("fill-opacity")) ?? FillOpacity,
            p.TryGetValue("fill-rule", out var rule) ? rule == "evenodd" : EvenOdd,
            p.TryGetValue("stroke", out var stroke) ? Color(stroke, Stroke) : Stroke,
            Opacity(p.GetValueOrDefault("stroke-opacity")) ?? StrokeOpacity,
            Number(p.GetValueOrDefault("stroke-width")) ?? StrokeWidth,
            p.GetValueOrDefault("stroke-linecap") switch { "round" => SKStrokeCap.Round, "square" => SKStrokeCap.Square, "butt" => SKStrokeCap.Butt, _ => Cap },
            p.GetValueOrDefault("stroke-linejoin") switch { "round" => SKStrokeJoin.Round, "bevel" => SKStrokeJoin.Bevel, "miter" => SKStrokeJoin.Miter, _ => Join },
            Number(p.GetValueOrDefault("stroke-miterlimit")) ?? MiterLimit,
            p.GetValueOrDefault("visibility") switch { "hidden" or "collapse" => false, "visible" => true, _ => Visible });
    }

    private static float? Opacity(string? value) =>
        value is null ? null : value.EndsWith('%') ? Number(value[..^1]) / 100 : Number(value) is { } number ? Math.Clamp(number, 0, 1) : null;

    private static SKColor? Color(string value, SKColor? inherited)
    {
        var text = value.Trim().ToLowerInvariant();
        if (text is "none" or "transparent") return null;
        if (text == "currentcolor") return SKColors.Black;
        if (text.StartsWith('#'))
        {
            var hex = text[1..];
            if (hex.Length is 3 or 4) hex = string.Concat(hex.Select(c => new string(c, 2)));
            if (hex.Length == 6) hex += "ff";
            return hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba)
                ? new SKColor((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba) : inherited;
        }
        if (RgbFunction().Match(text) is { Success: true } rgb)
        {
            byte Channel(string part) => part.EndsWith('%')
                ? (byte)Math.Clamp(Math.Round((Number(part[..^1]) ?? 0) * 2.55), 0, 255)
                : (byte)Math.Clamp(Math.Round(Number(part) ?? 0), 0, 255);
            var parts = rgb.Groups["args"].Value.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return inherited;
            var alpha = parts.Length > 3 ? (byte)Math.Round((Opacity(parts[3]) ?? 1) * 255) : (byte)255;
            return new SKColor(Channel(parts[0]), Channel(parts[1]), Channel(parts[2]), alpha);
        }
        return Named.TryGetValue(text, out var named) ? named : inherited;
    }

    private static SKMatrix? Transform(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var matrix = SKMatrix.Identity;
        foreach (Match function in TransformFunction().Matches(value))
        {
            var a = Numbers(function.Groups["args"].Value);
            float At(int i, float fallback = 0) => i < a.Count ? a[i] : fallback;
            var next = function.Groups["name"].Value switch
            {
                "matrix" when a.Count == 6 => new SKMatrix(a[0], a[2], a[4], a[1], a[3], a[5], 0, 0, 1),
                "translate" => SKMatrix.CreateTranslation(At(0), At(1)),
                "scale" => SKMatrix.CreateScale(At(0, 1), At(1, At(0, 1))),
                "rotate" => SKMatrix.CreateRotationDegrees(At(0), At(1), At(2)),
                "skewX" => SKMatrix.CreateSkew((float)Math.Tan(At(0) * Math.PI / 180), 0),
                "skewY" => SKMatrix.CreateSkew(0, (float)Math.Tan(At(0) * Math.PI / 180)),
                _ => SKMatrix.Identity,
            };
            matrix = matrix.PreConcat(next);
        }
        return matrix;
    }

    private static SKRect? ViewBox(string? value) =>
        Numbers(value) is [var x, var y, var width, var height] ? SKRect.Create(x, y, width, height) : null;

    private static float? Number(string? value)
    {
        if (value is null) return null;
        var text = value.Trim();
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase)) text = text[..^2];
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    private static List<float> Numbers(string? value) =>
        value is null ? [] : NumberPattern().Matches(value).Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();

    [GeneratedRegex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"(?<name>matrix|translate|scale|rotate|skewX|skewY)\s*\((?<args>[^)]*)\)")]
    private static partial Regex TransformFunction();

    [GeneratedRegex(@"^rgba?\((?<args>[^)]*)\)$")]
    private static partial Regex RgbFunction();
}
