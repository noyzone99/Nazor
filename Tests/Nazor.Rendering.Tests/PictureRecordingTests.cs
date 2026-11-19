using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Skia;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace Nazor.Rendering.Tests;

public sealed class PictureRecordingTests
{
    [Fact]
    public void Public_raster_drawing_observes_changed_glyph_offsets_after_warmup()
    {
        using var renderer = new SkiaRenderer();
        var text = new TextLayout("Mutable glyph data", new TextStyle([], 13), null, renderer.Fonts, renderer.Shaper);
        DisplayList list = new(new(180, 40), [new DrawText(text, new(3, 4), Color.Black)]);
        renderer.RenderToImage(list, 1, Color.White);
        renderer.RenderToImage(list, 1, Color.White);
        foreach (var run in text.Runs)
            for (var i = 0; i < run.OffsetsX.Length; i++) run.OffsetsX[i] += 5;
        using var cold = new SkiaRenderer();
        Assert.Equal(cold.RenderToImage(list, 1, Color.White).Pixels, renderer.RenderToImage(list, 1, Color.White).Pixels);
    }

    private static RenderedImage RetainedImage(SkiaRenderer renderer, DisplayList list, float scale)
    {
        var width = (int)Math.Ceiling(list.Size.Width * scale);
        var height = (int)Math.Ceiling(list.Size.Height * scale);
        var pixels = new byte[width * height * 4];
        var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try { renderer.RenderRetained(list, pinned.AddrOfPinnedObject(), width, height, width * 4, scale, Color.White, null); }
        finally { pinned.Free(); }
        return new(width, height, pixels);
    }

    [Fact]
    public void Raster_drawing_observes_changed_polyline_points_after_warmup()
    {
        using var renderer = new SkiaRenderer();
        Point[] points = [new(3, 4), new(45, 4)];
        DisplayList list = new(new(50, 30), [new DrawPolyline(points, 2, Color.Black)]);
        renderer.RenderToImage(list, 1, Color.White);
        renderer.RenderToImage(list, 1, Color.White);
        points[0] = new(3, 20);
        points[1] = new(45, 20);
        using var cold = new SkiaRenderer();
        Assert.Equal(cold.RenderToImage(list, 1, Color.White).Pixels, renderer.RenderToImage(list, 1, Color.White).Pixels);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Raster_reuse_matches_cold_drawing_pixels_through_clip_color_and_text_changes(float scale)
    {
        using var renderer = new SkiaRenderer();
        var first = new TextLayout("Retained raster", new TextStyle([], 13), null, renderer.Fonts, renderer.Shaper);
        var second = new TextLayout("Updated raster", new TextStyle([], 13), null, renderer.Fonts, renderer.Shaper);
        for (var i = 0; i < 6; i++)
        {
            var text = i < 3 ? first : second;
            var color = i % 2 == 0 ? Color.Black : new Color(20, 40, 90, 255);
            DisplayList list = new(new(160, 60), [new PushTranslate(0.25f, 0.5f),
                new PushClip(new(1, 1, 145, 48), new CornerRadius(4, 7)),
                new PushOpacity(0.75f, new(1, 1, 145, 48)), new FillRectangle(new(2, 2, 140, 45), 5, color),
                new DrawText(text, new(4, 5), Color.White), Pop.Instance, Pop.Instance, Pop.Instance]);
            using var cold = new SkiaRenderer();
            var expected = cold.RenderToImage(list, scale, Color.White);
            var actual = RetainedImage(renderer, list, scale);
            Assert.Equal(expected.Pixels, actual.Pixels);
        }
    }

    [Fact]
    public void Retained_recordings_preserve_state_reuse_unchanged_drawings_and_own_their_resources()
    {
        var renderer = new SkiaRenderer();
        var text = new TextLayout("Retained", new TextStyle([],12), null, renderer.Fonts, renderer.Shaper);
        var unchanged = new FillRectangle(new(2,2,20,20), 4, new(220,30,40,255));
        DisplayList List(Color color) => new(new(80,60), [
            unchanged, new PushTranslate(30,10), new PushClip(new(0,0,25,25), 3),
            new PushOpacity(0.6f, new(0,0,25,25)), new FillRectangle(new(0,0,30,30), 8, color),
            Pop.Instance, Pop.Instance, Pop.Instance, new DrawText(text, new(2,40), Color.Black)]);
        using var first = renderer.Record(List(Color.Black));
        using var warm = renderer.Record(List(Color.Black));
        var list = List(new(20,180,70,255));
        var expected = renderer.RenderToImage(list, 1, Color.White);
        using var picture = renderer.Record(list);
        Assert.True(renderer.LastRecordingsReused > 0);
        Assert.Equal(0, renderer.LastRecordingsCreated);
        renderer.Dispose();
        using var bitmap = new SKBitmap(new SKImageInfo(80,60,SKColorType.Bgra8888,SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawPicture(picture);
        }
        var largestDifference = expected.Pixels.Zip(bitmap.Bytes, (a,b) => Math.Abs(a-b)).Max();
        Assert.True(largestDifference <= 1, $"Recording changed a channel by {largestDifference}; allowed rounding error is 1.");
    }

    [Fact]
    public void Continuously_changing_text_does_not_create_per_drawing_native_recordings()
    {
        using var renderer = new SkiaRenderer();
        for (var i = 0; i < 8; i++)
        {
            var text = new TextLayout($"Updated text {i}", new TextStyle([], 12), null, renderer.Fonts, renderer.Shaper);
            using var picture = renderer.Record(new(new(180, 40), [new DrawText(text, new(2, 2), Color.Black)]));
            Assert.Equal(0, renderer.LastRecordingsCreated);
        }
    }
}
