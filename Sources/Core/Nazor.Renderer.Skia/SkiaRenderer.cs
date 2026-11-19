using Nazor.Graphics;
using Nazor.Graphics.Text;
using SkiaSharp;

namespace Nazor.Skia;

/// <summary>
/// Replays display lists on a Skia raster canvas or records immutable pictures for GPU playback. The canvas is scaled once by the DPI factor: commands stay in
/// logical units. Layout snaps box edges to physical pixels; circular decorations may retain their original square geometry. Text is drawn from the glyphs and positions
/// of the <see cref="TextLayout" /> that was measured, never re-measured. Renderer instances and their caches belong to the UI thread;
/// resource-owning recordings can be transferred to a rendering worker. Uniform solid circular borders use a closed antialiased ring contour. Adjacent matching circular padding-box
/// fills and borders share one coverage mask, including partial repaints. Circular classification uses logical bounds
/// and radii, preserving coverage at fractional origins independently of Skia's rounded-rectangle classification.
/// </summary>
public sealed partial class SkiaRenderer : IRenderer
{
    private readonly SkiaFontProvider fonts;
    private readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint imagePaint = new() { IsAntialias = true };
    private readonly SKRoundRect clipShape = new();
    private readonly Dictionary<(SkiaFontFace Face, float Size, FontSynthesis Synthesis), SKFont> skFonts = new();
    private readonly Dictionary<DrawCommand, SKPicture> recordings = [];
    private readonly HashSet<DrawCommand> usedRecordings = [];
    private HashSet<DrawCommand> candidates = [];
    private HashSet<DrawCommand> previousCandidates = [];

    /// <summary>Gets the number of drawing recordings reused by the last Record call.</summary>
    public int LastRecordingsReused { get; private set; }

    /// <summary>Gets the number of drawing recordings created by the last Record call.</summary>
    public int LastRecordingsCreated { get; private set; }

    /// <summary>Initializes a new instance of SkiaRenderer.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    public SkiaRenderer(FontOptions? options = null)
    {
        fonts = new SkiaFontProvider(options);
        Shaper = new HarfBuzzTextShaper();
    }

    /// <summary>Gets the provider used to resolve font faces and glyph fallbacks.</summary>
    public IFontProvider Fonts => fonts;

    /// <summary>Gets the service used to shape text into positioned glyphs.</summary>
    public ITextShaper Shaper { get; }

    /// <summary>Gets the service used to decode raster and vector image content.</summary>
    public IImageDecoder Images { get; } = new SkiaImageDecoder();

    /// <summary>Gets the Skia font provider used by this renderer.</summary>
    public SkiaFontProvider FontProvider => fonts;

    /// <summary>Renders drawing commands into a newly allocated pixel image at the requested scale.</summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    /// <returns>The resulting RenderedImage value.</returns>
    public RenderedImage RenderToImage(DisplayList displayList, float scale, Graphics.Color background)
    {
        var width = Math.Max(1, (int)MathF.Ceiling(displayList.Size.Width * scale));
        var height = Math.Max(1, (int)MathF.Ceiling(displayList.Size.Height * scale));
        var pixels = new byte[width * height * 4];
        unsafe
        {
            fixed (byte* pointer = pixels) Render(displayList, (nint)pointer, width, height, width * 4, scale, background);
        }
        return new RenderedImage(width, height, pixels);
    }

    /// <summary>Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.</summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="pixels">The address of the top-down BGRA premultiplied pixel buffer.</param>
    /// <param name="width">The width of the target pixel buffer.</param>
    /// <param name="height">The height of the target pixel buffer.</param>
    /// <param name="stride">The number of bytes between the starts of successive pixel rows.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background) =>
        Render(displayList, pixels, width, height, stride, scale, background, null);

    /// <summary>Renders drawing commands into the supplied top-down BGRA premultiplied pixel buffer.</summary>
    /// <param name="displayList">The ordered drawing commands and logical surface size to render.</param>
    /// <param name="pixels">The address of the top-down BGRA premultiplied pixel buffer.</param>
    /// <param name="width">The width of the target pixel buffer.</param>
    /// <param name="height">The height of the target pixel buffer.</param>
    /// <param name="stride">The number of bytes between the starts of successive pixel rows.</param>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="background">The background color used to clear the surface.</param>
    /// <param name="damage">The damage (IReadOnlyList&lt;PixelRect&gt;?).</param>
    public void Render(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background, IReadOnlyList<PixelRect>? damage) =>
        RenderCore(displayList, pixels, width, height, stride, scale, background, damage, retainText: false);

    internal void RenderRetained(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background, IReadOnlyList<PixelRect>? damage) =>
        RenderCore(displayList, pixels, width, height, stride, scale, background, damage, retainText: true);

    private void RenderCore(DisplayList displayList, nint pixels, int width, int height, int stride, float scale, Graphics.Color background, IReadOnlyList<PixelRect>? damage, bool retainText)
    {
        if (damage is { Count: 0 }) return;
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info, pixels, stride)
            ?? throw new InvalidOperationException("Unable to create a Skia surface on the target buffer.");
        var canvas = surface.Canvas;
        if (damage is not null)
        {
            using var region = new SKRegion();
            foreach (var rect in damage) region.Op(new SKRectI(rect.X, rect.Y, rect.Right, rect.Bottom), SKRegionOperation.Union);
            canvas.ClipRegion(region);
        }

        canvas.Clear(ToSk(background));
        canvas.Scale(scale);
        if (retainText)
        {
            BeginRecording(recordStatistics: false);
            Draw(canvas, displayList, cacheRecordings: true, raster: true);
            PruneRecordings();
        }
        else Draw(canvas, displayList);
        canvas.Flush();
    }

    /// <summary>Encodes the rendered pixel image as a PNG byte sequence.</summary>
    /// <param name="image">The rendered or decoded image to process.</param>
    /// <returns>The PNG-encoded image bytes.</returns>
    public byte[] EncodePng(RenderedImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var skImage = SKImage.FromPixelCopy(info, image.Pixels, image.Stride);
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Records a resource-owning immutable picture for playback on a rendering thread.</summary>
    /// <param name="displayList">The drawing commands, read only on the calling UI thread.</param>
    /// <returns>A caller-owned picture retaining native image and font references independently of UI caches.</returns>
    public SKPicture Record(DisplayList displayList)
    {
        BeginRecording();
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(new SKRect(0, 0, displayList.Size.Width, displayList.Size.Height));
        Draw(canvas, displayList, cacheRecordings: true);
        PruneRecordings();
        return recorder.EndRecording();
    }

    private void BeginRecording(bool recordStatistics = true)
    {
        if (recordStatistics) LastRecordingsReused = LastRecordingsCreated = 0;
        usedRecordings.Clear();
        (candidates, previousCandidates) = (previousCandidates, candidates);
        candidates.Clear();
    }

    private void PruneRecordings()
    {
        foreach (var key in recordings.Keys.Where(key => !usedRecordings.Contains(key)).ToArray())
        {
            recordings[key].Dispose();
            recordings.Remove(key);
        }
        previousCandidates.Clear();
    }

    internal void Draw(SKCanvas canvas, DisplayList displayList, bool cacheRecordings = false, bool raster = false)
    {
        for (var commandIndex = 0; commandIndex < displayList.Commands.Count; commandIndex++)
        {
            var command = displayList.Commands[commandIndex];
            if (cacheRecordings && (!raster || command is DrawText) &&
                command is not (FillRectangle or DrawBorder or PushClip or PushOpacity or PushFilter or PushBlend or PushBackdrop or PushTranslate or PushTransform or PushComposition or Pop))
            {
                var key = command;
                if (candidates.Count < 4096) candidates.Add(key);
                if (recordings.TryGetValue(key, out var recorded))
                {
                    usedRecordings.Add(key);
                    if (!raster) LastRecordingsReused++;
                    canvas.DrawPicture(recorded);
                    continue;
                }
                if (previousCandidates.Contains(key) && recordings.Count < 4096)
                {
                    using var recorder = new SKPictureRecorder();
                    var content = recorder.BeginRecording(new SKRect(-1e9f, -1e9f, 1e9f, 1e9f));
                    Draw(content, new DisplayList(displayList.Size, [command]));
                    var picture = recorder.EndRecording();
                    if (!raster) LastRecordingsCreated++;
                    canvas.DrawPicture(picture);
                    recordings.Add(key, picture);
                    usedRecordings.Add(key);
                    continue;
                }
            }
            switch (command)
            {
                case FillRectangle f:
                    if (commandIndex + 1 < displayList.Commands.Count && displayList.Commands[commandIndex + 1] is DrawBorder adjacent &&
                        PaintCircularFill(canvas, f, adjacent))
                    {
                        commandIndex++;
                        break;
                    }
                    fill.Color = ToSk(f.Color);

                    PaintRectangle(canvas, f.Rect, f.Radius, fill);
                    break;
                case DrawBorder b:
                    DrawBorderSides(canvas, b);
                    break;
                case DrawShadow s:
                    DrawBoxShadow(canvas, s);
                    break;
                case FillGradient g:
                    DrawGradient(canvas, g);
                    break;
                case DrawText t:
                    DrawTextLayout(canvas, t);
                    break;
                case DrawPolyline l when l.Points.Count > 1:
                    using (var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = l.Thickness, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, Color = ToSk(l.Color) })
                    {

                        using var builder = new SKPathBuilder();
                        builder.MoveTo(l.Points[0].X, l.Points[0].Y);
                        for (var i = 1; i < l.Points.Count; i++) builder.LineTo(l.Points[i].X, l.Points[i].Y);
                        using var path = builder.Detach();
                        canvas.DrawPath(path, stroke);
                    }
                    break;
                case PushClip or PushOpacity or PushFilter or PushBlend or PushBackdrop or PushTranslate or PushTransform or PushComposition or Pop:
                    ApplyState(canvas, command, clipShape);
                    break;
                case DrawImage i:
                    DrawImage(canvas, i);
                    break;
                case FillImage f:
                    FillImage(canvas, f);
                    break;
            }
        }
    }

    /// <summary>Records static drawings once and preserves detached animated state for render-thread sampling.</summary>
    /// <param name="displayList">The UI-thread display list, optionally containing composition snapshots.</param>
    /// <returns>A resource-owning recording, disposed by its render worker after use.</returns>
    public RecordedComposition RecordComposition(DisplayList displayList)
    {
        if (!displayList.Commands.Any(command => command is PushComposition)) return new([new(Record(displayList), null)]);
        BeginRecording();
        var parts = new List<RecordedComposition.Part>();
        var commands = new List<DrawCommand>();
        void Flush()
        {
            if (commands.Count == 0) return;
            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(new SKRect(-1e9f,-1e9f,1e9f,1e9f));
            Draw(canvas,new DisplayList(displayList.Size,commands),cacheRecordings:true);
            parts.Add(new(recorder.EndRecording(),null));
            commands.Clear();
        }
        try
        {
            foreach (var command in displayList.Commands)
            {
                if (command is PushClip or PushOpacity or PushFilter or PushBlend or PushBackdrop or PushTranslate or PushTransform or PushComposition or Pop)
                {
                    Flush();
                    parts.Add(new(null,command));
                }
                else commands.Add(command);
            }
            Flush();
            PruneRecordings();
            return new(parts.ToArray());
        }
        catch { foreach (var part in parts) part.Picture?.Dispose(); throw; }
    }

    internal static void ApplyState(SKCanvas canvas, DrawCommand command, SKRoundRect? clipShape = null)
    {
        switch (command)
        {
            case PushClip clip:
                canvas.Save();
                if (!clip.Radius.IsZero)
                {
                    if (clipShape is not null)
                    {
                        SetRoundRect(clipShape, clip.Rect, clip.Radius);
                        canvas.ClipRoundRect(clipShape, SKClipOperation.Intersect, true);
                    }
                    else
                    {
                        using var shape = RoundRect(clip.Rect, clip.Radius);
                        canvas.ClipRoundRect(shape, SKClipOperation.Intersect, true);
                    }
                }
                else canvas.ClipRect(ToSk(clip.Rect),SKClipOperation.Intersect,false);
                break;
            case PushOpacity opacity:
                using (var paint = new SKPaint { Color = new SKColor(0,0,0,(byte)Math.Round(Math.Clamp(opacity.Opacity,0,1)*255)) })
                    canvas.SaveLayer(ToSk(opacity.Bounds),paint);
                break;
            case PushBackdrop backdrop:
                ApplyBackdrop(canvas, backdrop);
                break;
            case PushBlend blend:
                using (var paint = new SKPaint { BlendMode = ToSk(blend.Mode) }) canvas.SaveLayer(ToSk(blend.Bounds), paint);
                break;
            case PushFilter filter:
                ApplyFilter(canvas,filter);
                break;
            case PushTranslate translate:
                canvas.Save(); canvas.Translate(translate.X,translate.Y);
                break;
            case PushTransform transform:
                canvas.Save(); Concat(canvas,transform.Matrix);
                break;
            case PushComposition composition:
                var sample = composition.Timeline.Sample(System.Diagnostics.Stopwatch.GetElapsedTime(composition.Timeline.CapturedAt));
                if (sample.Opacity < 1)
                {
                    using var paint = new SKPaint { Color = new SKColor(0,0,0,(byte)Math.Round(sample.Opacity*255)) };
                    canvas.SaveLayer(paint);
                }
                else canvas.Save();
                Concat(canvas,sample.Transform);
                break;
            case Pop: canvas.Restore(); break;
        }
    }

    private static void Concat(SKCanvas canvas, Matrix value)
    {
        var matrix = new SKMatrix(value.M11,value.M21,value.OffsetX,value.M12,value.M22,value.OffsetY,0,0,1);
        canvas.Concat(in matrix);
    }

    private static bool PaintCircularFill(SKCanvas canvas, FillRectangle background, DrawBorder border)
    {
        var widths = border.Widths;
        if (border.Brush is not null || !border.IsUniform || border.Top.Style != LineStyle.Solid || background.Color != border.Top.Color ||
            !IsCircle(border.Rect, border.Radius) || widths.Top <= 0 || widths.Left != widths.Top || widths.Right != widths.Top || widths.Bottom != widths.Top ||
            background.Rect != border.Rect.Deflate(widths) || background.Radius != border.Radius.Deflate(widths)) return false;
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.White, Shader = SKShader.CreateColor(ToSk(background.Color)) };
        using var builder = new SKPathBuilder();
        Circle(builder, new SKPoint(border.Rect.X + border.Rect.Width / 2, border.Rect.Y + border.Rect.Height / 2), border.Rect.Width / 2);
        using var path = builder.Detach();
        canvas.DrawPath(path, paint);
        return true;
    }

    internal void DrawBorderSides(SKCanvas canvas, DrawBorder border)
    {
        var widths = border.Widths;
        if (widths.IsZero) return;
        using var outer = RoundRect(border.Rect, border.Radius);
        var equalWidths = widths.Left == widths.Top && widths.Top == widths.Right && widths.Right == widths.Bottom;
        if (IsCircle(border.Rect, border.Radius) && equalWidths && widths.Top > 0 &&
            widths.Top < border.Rect.Width / 2 && border.Brush is null && border.IsUniform && border.Top.Style == LineStyle.Solid)
        {
            using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.White, Shader = SKShader.CreateColor(ToSk(border.Top.Color)) };
            using var pathBuilder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
            var center = new SKPoint(border.Rect.X + border.Rect.Width / 2, border.Rect.Y + border.Rect.Height / 2);
            Circle(pathBuilder, center, border.Rect.Width / 2);
            Circle(pathBuilder, center, border.Rect.Width / 2 - widths.Top);
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, stroke);
            return;
        }
        using var inner = RoundRect(border.Rect.Deflate(widths), border.Radius.Deflate(widths));
        if (border.Brush is { } brush)
        {
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
            ConfigureGradient(paint, brush);
            canvas.DrawRoundRectDifference(outer, inner, paint);
            return;
        }
        if (border.IsUniform && (border.Top.Style == LineStyle.Solid || equalWidths))
        {
            DrawRing(canvas, border.Top, outer, inner, border.Rect, border.Radius, widths.Top);
            return;
        }
        var rect = border.Rect;
        var innerRect = rect.Deflate(widths);
        (BorderSide Side, float Width, SKPoint[] Trapezoid)[] sides =
        [
            (border.Top, widths.Top, [new(rect.X, rect.Y), new(rect.Right, rect.Y), new(innerRect.Right, innerRect.Y), new(innerRect.X, innerRect.Y)]),
            (border.Right, widths.Right, [new(rect.Right, rect.Y), new(rect.Right, rect.Bottom), new(innerRect.Right, innerRect.Bottom), new(innerRect.Right, innerRect.Y)]),
            (border.Bottom, widths.Bottom, [new(rect.Right, rect.Bottom), new(rect.X, rect.Bottom), new(innerRect.X, innerRect.Bottom), new(innerRect.Right, innerRect.Bottom)]),
            (border.Left, widths.Left, [new(rect.X, rect.Bottom), new(rect.X, rect.Y), new(innerRect.X, innerRect.Y), new(innerRect.X, innerRect.Bottom)]),
        ];
        foreach (var (side, width, trapezoid) in sides)
        {
            if (width <= 0 || side.Color.IsTransparent) continue;
            using var builder = new SKPathBuilder();
            builder.MoveTo(trapezoid[0]);
            for (var i = 1; i < trapezoid.Length; i++) builder.LineTo(trapezoid[i]);
            builder.Close();
            using var clip = builder.Detach();
            canvas.Save();
            canvas.ClipPath(clip, SKClipOperation.Intersect, antialias: true);
            DrawRing(canvas, side, outer, inner, rect, border.Radius, width);
            canvas.Restore();
        }
    }

    private static bool IsCircle(Rect bounds, CornerRadius radius) => bounds.Width > 0 && bounds.Width == bounds.Height &&
        radius.IsUniform && radius.TopLeft.X == radius.TopLeft.Y && radius.TopLeft.X >= bounds.Width / 2;

    private static void Circle(SKPathBuilder path, SKPoint center, float radius)
    {
        var k = radius * (float)(4 * (Math.Sqrt(2) - 1) / 3);
        var (x, y) = (center.X, center.Y);
        path.MoveTo(x + radius, y);
        path.CubicTo(x + radius, y + k, x + k, y + radius, x, y + radius);
        path.CubicTo(x - k, y + radius, x - radius, y + k, x - radius, y);
        path.CubicTo(x - radius, y - k, x - k, y - radius, x, y - radius);
        path.CubicTo(x + k, y - radius, x + radius, y - k, x + radius, y);
        path.Close();
    }

    private void DrawRing(SKCanvas canvas, BorderSide side, SKRoundRect outer, SKRoundRect inner, Rect rect, CornerRadius radius, float width)
    {
        if (side.Style == LineStyle.Solid)
        {
            fill.Color = ToSk(side.Color);
            canvas.DrawRoundRectDifference(outer, inner, fill);
            return;
        }
        var dotted = side.Style == LineStyle.Dotted;
        using var dash = SKPathEffect.CreateDash(dotted ? [0, 2 * width] : [3 * width, width], 0);
        using var stroke = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, Color = ToSk(side.Color), PathEffect = dash,
            StrokeCap = dotted ? SKStrokeCap.Round : SKStrokeCap.Butt,
        };

        using var middle = RoundRect(rect.Inflate(-width / 2), radius.Deflate(new Thickness(width / 2, width / 2, width / 2, width / 2)));
        canvas.Save();
        canvas.ClipRoundRect(outer, SKClipOperation.Intersect, antialias: true);
        canvas.DrawRoundRect(middle, stroke);
        canvas.Restore();
    }

    private static void DrawGradient(SKCanvas canvas, FillGradient gradient)
    {
        if (gradient.Brush.Stops.Count == 0) return;
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        ConfigureGradient(paint, gradient.Brush);
        if (!gradient.Radius.IsZero)
        {
            using var shape = RoundRect(gradient.Rect, gradient.Radius);
            canvas.DrawRoundRect(shape, paint);
        }
        else canvas.DrawRect(ToSk(gradient.Rect), paint);
    }

    private static void ConfigureGradient(SKPaint paint, GradientBrush brush)
    {
        var stops = brush.Stops;
        if (stops.Count == 0) return;
        var colors = stops.Select(s => ToSk(s.Color)).ToArray();
        var positions = stops.Select(s => s.Offset).ToArray();
        var mode = brush.Repeating ? SKShaderTileMode.Repeat : SKShaderTileMode.Clamp;
        switch (brush)
        {
            case LinearGradientBrush linear:
                paint.Shader = SKShader.CreateLinearGradient(new SKPoint(linear.Start.X, linear.Start.Y), new SKPoint(linear.End.X, linear.End.Y),
                    colors, positions, mode);
                break;
            case RadialGradientBrush radial when radial.RadiusX > 0 && radial.RadiusY > 0:

                var center = new SKPoint(radial.Center.X, radial.Center.Y);
                var matrix = SKMatrix.CreateScale(1, radial.RadiusY / radial.RadiusX, radial.Center.X, radial.Center.Y);
                paint.Shader = radial.StartRadius > 0
                    ? SKShader.CreateTwoPointConicalGradient(center, radial.StartRadius, center, radial.RadiusX, colors, positions, mode, matrix)
                    : SKShader.CreateRadialGradient(center, radial.RadiusX, colors, positions, mode, matrix);
                break;
            case ConicGradientBrush conic:
                paint.Shader = SKShader.CreateSweepGradient(new SKPoint(conic.Center.X, conic.Center.Y), colors, positions, mode,
                    conic.StartAngle, conic.EndAngle, SKMatrix.CreateRotationDegrees(conic.Rotation - 90, conic.Center.X, conic.Center.Y));
                break;
            default:

                paint.Color = colors[^1];
                break;
        }
    }

    private void DrawBoxShadow(SKCanvas canvas, DrawShadow shadow)
    {
        if (shadow.Color.IsTransparent) return;
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ToSk(shadow.Color) };
        if (shadow.Blur > 0) paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur / 2);
        using var box = RoundRect(shadow.Rect, shadow.Radius);
        canvas.Save();
        if (!shadow.Inset)
        {

            using var shape = RoundRect(shadow.Rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(shadow.Spread), shadow.Radius.Inflate(shadow.Spread));
            canvas.ClipRoundRect(box, SKClipOperation.Difference, antialias: true);
            canvas.DrawRoundRect(shape, paint);
        }
        else
        {

            using var hole = RoundRect(shadow.Rect.Offset(shadow.Offset.X, shadow.Offset.Y).Inflate(-shadow.Spread), shadow.Radius.Inflate(-shadow.Spread));
            var margin = shadow.Blur + Math.Abs(shadow.Spread) + Math.Abs(shadow.Offset.X) + Math.Abs(shadow.Offset.Y) + 1;
            using var outer = new SKRoundRect(ToSk(shadow.Rect.Inflate(margin)));
            canvas.ClipRoundRect(box, SKClipOperation.Intersect, antialias: true);
            canvas.DrawRoundRectDifference(outer, hole, paint);
        }
        canvas.Restore();
    }

    private static SKRoundRect RoundRect(Rect rect, CornerRadius radius)
    {
        var result = new SKRoundRect();
        SetRoundRect(result, rect, radius);
        return result;
    }

    private static void SetRoundRect(SKRoundRect result, Rect rect, CornerRadius radius)
    {
        ReadOnlySpan<SKPoint> radii = [
            new SKPoint(radius.TopLeft.X, radius.TopLeft.Y), new SKPoint(radius.TopRight.X, radius.TopRight.Y),
            new SKPoint(radius.BottomRight.X, radius.BottomRight.Y), new SKPoint(radius.BottomLeft.X, radius.BottomLeft.Y),
        ];
        result.SetRectRadii(ToSk(rect), radii);
    }

    private static void PaintRectangle(SKCanvas canvas, Rect rect, CornerRadius radius, SKPaint paint)
    {
        if (radius.IsZero) canvas.DrawRect(ToSk(rect), paint);
        else if (radius.IsUniform) canvas.DrawRoundRect(ToSk(rect), radius.TopLeft.X, radius.TopLeft.Y, paint);
        else
        {
            using var shape = RoundRect(rect, radius);
            canvas.DrawRoundRect(shape, paint);
        }
    }

    private void DrawTextLayout(SKCanvas canvas, DrawText command)
    {
        Span<SKPoint> points = stackalloc SKPoint[256];
        var rangeStart = Math.Clamp(command.TextStart, 0, command.Layout.Text.Length);
        var rangeEnd = command.TextLength is { } length ? rangeStart + Math.Clamp(length, 0, command.Layout.Text.Length - rangeStart) : command.Layout.Text.Length;
        var partial = rangeStart != 0 || rangeEnd != command.Layout.Text.Length;
        fill.Color = ToSk(command.Color);
        foreach (var line in command.Layout.Lines)
        {
            var x = command.Origin.X + command.LineOffset(line);
            var baseline = command.Origin.Y + line.Baseline;

            var area = command.Decoration is null ? Rect.Empty : !partial ? new Rect(0, line.Top, line.Width, line.Height) :
                command.Layout.GetSelectionRects(rangeStart, rangeEnd).FirstOrDefault(area => area.Y == line.Top);
            var decoration = line.Width > 0 && area.Width > 0 ? command.Decoration : null;
            void Decorate(bool through)
            {
                if (decoration is null) return;
                if (partial)
                {
                    canvas.Save();
                    canvas.ClipRect(new SKRect(x + area.X, command.Origin.Y + line.Top - line.Height,
                        x + Math.Min(area.Right, line.Width), command.Origin.Y + line.Top + 2 * line.Height));
                }
                DrawDecoration(canvas, decoration, line, x, baseline, command.Layout.Style.FontSize, through);
                if (partial) canvas.Restore();
            }
            Decorate(through: false);
            fill.Color = ToSk(command.Color);
            var lineX = x;
            foreach (var run in line.Runs)
            {
                if (run.Glyphs.Length == 0) continue;
                var positions = run.Glyphs.Length <= points.Length ? points[..run.Glyphs.Length] : new SKPoint[run.Glyphs.Length];
                var visible = rangeStart == 0 && rangeEnd == command.Layout.Text.Length ? null : new List<ushort>();
                var count = 0;
                for (var i = 0; i < run.Glyphs.Length; i++)
                {
                    if (visible is null || run.Clusters[i] >= rangeStart && run.Clusters[i] < rangeEnd)
                    {
                        positions[count++] = new SKPoint(x + run.OffsetsX[i], baseline - run.OffsetsY[i]);
                        visible?.Add(run.Glyphs[i]);
                    }
                    x += run.Advances[i];
                }
                if (count == 0) continue;
                using var builder = new SKTextBlobBuilder();
                builder.AddPositionedRun(visible?.ToArray() ?? run.Glyphs, FontFor(run), positions[..count]);
                using var blob = builder.Build();
                if (blob is not null) canvas.DrawText(blob, 0, 0, fill);
            }
            x = lineX;
            Decorate(through: true);
        }
    }

    private void DrawDecoration(SKCanvas canvas, TextDecoration decoration, TextLine line, float x, float baseline, float fontSize, bool through)
    {
        if (through ? (decoration.Lines & DecorationLines.LineThrough) == 0 : (decoration.Lines & (DecorationLines.Underline | DecorationLines.Overline)) == 0) return;
        var run = line.Runs.FirstOrDefault(r => r.Glyphs.Length > 0);
        var metrics = run is null ? default : FontFor(run).Metrics;
        var thickness = decoration.Thickness > 0 ? decoration.Thickness
            : metrics.UnderlineThickness is { } t && t > 0 ? t : Math.Max(1, fontSize / 14);
        var underline = metrics.UnderlinePosition is { } u && u > 0 ? u : fontSize * 0.1f;
        var strikeout = metrics.StrikeoutPosition is { } s && s < 0 ? s : -fontSize * 0.3f;
        using var paint = new SKPaint { IsAntialias = true, Color = ToSk(decoration.Color), Style = SKPaintStyle.Stroke, StrokeWidth = thickness };
        if (decoration.Style == DecorationStyle.Dotted)
        {
            paint.PathEffect = SKPathEffect.CreateDash([0, 2 * thickness], 0);
            paint.StrokeCap = SKStrokeCap.Round;
        }
        else if (decoration.Style == DecorationStyle.Dashed) paint.PathEffect = SKPathEffect.CreateDash([3 * thickness, 2 * thickness], 0);
        void Draw(float y)
        {
            if (decoration.Style == DecorationStyle.Wavy)
            {

                var step = Math.Max(2, 2 * thickness);
                using var builder = new SKPathBuilder();
                builder.MoveTo(x, y);
                var up = true;
                for (var at = x; at < x + line.Width; at += step, up = !up)
                    builder.QuadTo(at + step / 2, y + (up ? -1.5f : 1.5f) * thickness, at + step, y);
                using var path = builder.Detach();
                canvas.Save();
                canvas.ClipRect(new SKRect(x, y - 3 * thickness, x + line.Width, y + 3 * thickness));
                canvas.DrawPath(path, paint);
                canvas.Restore();
                return;
            }
            canvas.DrawLine(x, y, x + line.Width, y, paint);
            if (decoration.Style == DecorationStyle.Double) canvas.DrawLine(x, y + 2 * thickness, x + line.Width, y + 2 * thickness, paint);
        }
        if (through)
        {
            Draw(baseline + strikeout);
            return;
        }
        if ((decoration.Lines & DecorationLines.Underline) != 0) Draw(baseline + underline + thickness / 2);
        if ((decoration.Lines & DecorationLines.Overline) != 0) Draw(baseline - line.Ascent + thickness / 2);
    }

    private SKFont FontFor(ShapedRun run)
    {
        var face = (SkiaFontFace)run.Face;
        if (skFonts.TryGetValue((face, run.Size,run.Synthesis), out var font)) return font;
        font = new SKFont(face.Typeface, run.Size)
        {
            Subpixel = true,
            Hinting = SKFontHinting.Normal,
            Edging = SKFontEdging.Antialias,
            LinearMetrics = true,
            BaselineSnap = true,
            Embolden = (run.Synthesis & FontSynthesis.Weight) != 0,
            SkewX = (run.Synthesis & FontSynthesis.Style) != 0 ? -.21255656f : 0,
        };
        skFonts[(face, run.Size,run.Synthesis)] = font;
        return font;
    }

    private static SKRect ToSk(Rect rect) => new(rect.X, rect.Y, rect.Right, rect.Bottom);

    private static SKColor ToSk(Graphics.Color color) => new(color.R, color.G, color.B, color.A);

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        foreach (var picture in recordings.Values) picture.Dispose();
        recordings.Clear();
        usedRecordings.Clear();
        candidates.Clear();
        previousCandidates.Clear();
        foreach (var font in skFonts.Values) font.Dispose();
        skFonts.Clear();
        fill.Dispose();
        imagePaint.Dispose();
        clipShape.Dispose();
        fonts.Dispose();
    }

    private void FillImage(SKCanvas canvas, FillImage command)
    {
        var tile = command.Tile;
        var (modeX, modeY) = (command.RepeatX ? SKShaderTileMode.Repeat : SKShaderTileMode.Decal, command.RepeatY ? SKShaderTileMode.Repeat : SKShaderTileMode.Decal);
        using var shader = command.Image switch
        {
            SkiaRasterImage raster => raster.Image.ToShader(modeX, modeY, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear),
                SKMatrix.CreateScaleTranslation(tile.Width / raster.Image.Width, tile.Height / raster.Image.Height, tile.X, tile.Y)),
            SkiaVectorImage vector when vector.Size.Width > 0 && vector.Size.Height > 0 => vector.Picture.ToShader(modeX, modeY, SKFilterMode.Linear,
                SKMatrix.CreateScaleTranslation(tile.Width / vector.Size.Width, tile.Height / vector.Size.Height, tile.X, tile.Y),
                new SKRect(0, 0, vector.Size.Width, vector.Size.Height)),
            _ => null,
        };
        if (shader is null) return;
        using var paint = new SKPaint { IsAntialias = true, Shader = shader };
        PaintRectangle(canvas, command.Area, command.Radius, paint);
    }

    private void DrawImage(SKCanvas canvas, DrawImage command)
    {
        if (command.Image is not (SkiaRasterImage or SkiaVectorImage)) return;
        if (command.Clip is { } clip)
        {
            canvas.Save();
            canvas.ClipRect(ToSk(clip), SKClipOperation.Intersect, antialias: true);
        }

        using var filter = command.Tint is { } color
            ? SKColorFilter.CreateBlendMode(ToSk(color), SKBlendMode.SrcIn)
            : null;
        using var tint = filter is null ? null : new SKPaint { IsAntialias = true, ColorFilter = filter };
        var paint = tint ?? imagePaint;
        var destination = command.Destination;
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        switch (command.Image)
        {
            case SkiaRasterImage raster:
                canvas.DrawImage(raster.Image, ToSk(destination), sampling, paint);
                break;
            case SkiaVectorImage vector when vector.Size.Width > 0 && vector.Size.Height > 0:
                if (destination.Width <= 24 && destination.Height <= 24 && vector.SmallImage is { } icon)
                {
                    canvas.DrawImage(icon, ToSk(destination), sampling, paint);
                    break;
                }
                var matrix = SKMatrix.CreateScaleTranslation(destination.Width / vector.Size.Width, destination.Height / vector.Size.Height, destination.X, destination.Y);
                canvas.DrawPicture(vector.Picture, in matrix, tint);
                break;
        }
        if (command.Clip is not null) canvas.Restore();
    }
}
