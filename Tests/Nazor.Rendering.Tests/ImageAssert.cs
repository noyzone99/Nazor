using System.Runtime.CompilerServices;
using Nazor.Graphics;
using Nazor.Skia;
using SkiaSharp;

namespace Nazor.Rendering.Tests;

internal static class ImageAssert
{
    private const int ChannelTolerance = 8;
    private const double PixelFraction = 0.005;

    public static void MatchesBaseline(SkiaRenderer renderer, RenderedImage image, string name, [CallerFilePath] string source = "")
    {
        var directory = Path.GetDirectoryName(source);
        var hasSource = !string.IsNullOrEmpty(directory) && Directory.Exists(directory);
        var baselines = Path.Combine(hasSource ? directory! : AppContext.BaseDirectory, "Baselines");
        if (OperatingSystem.IsLinux()) baselines = Path.Combine(baselines, "Linux");
        var expectedPath = Path.Combine(baselines, name + ".png");
        var artifacts = hasSource ? Path.GetFullPath(Path.Combine(directory!, "../../artifacts/rendering"))
            : Path.Combine(AppContext.BaseDirectory, "artifacts", "rendering");
        Directory.CreateDirectory(artifacts);
        var png = renderer.EncodePng(image);
        File.WriteAllBytes(Path.Combine(artifacts, name + ".actual.png"), png);
        if (Environment.GetEnvironmentVariable("NAZOR_UPDATE_BASELINES") == "1")
        {
            Directory.CreateDirectory(baselines);
            File.WriteAllBytes(expectedPath, png);
            return;
        }
        Assert.True(File.Exists(expectedPath), $"Missing baseline {expectedPath}: run the tests with NAZOR_UPDATE_BASELINES=1 and review the image.");
        using var expected = SKBitmap.Decode(expectedPath);
        Assert.Equal((image.Width, image.Height), (expected.Width, expected.Height));
        var different = 0;
        using var diff = new SKBitmap(image.Width, image.Height);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var a = image.GetPixel(x, y);
                var b = expected.GetPixel(x, y);
                var delta = Math.Max(Math.Max(Math.Abs(a.R - b.Red), Math.Abs(a.G - b.Green)), Math.Max(Math.Abs(a.B - b.Blue), Math.Abs(a.A - b.Alpha)));
                if (delta > ChannelTolerance) different++;
                diff.SetPixel(x, y, delta > ChannelTolerance ? SKColors.Red : new SKColor(0, 0, 0, (byte)Math.Min(255, delta * 16)));
            }
        }
        if (different > image.Width * image.Height * PixelFraction)
        {
            using var data = diff.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(artifacts, name + ".diff.png"), data.ToArray());
            Assert.Fail($"{name}: {different} pixels differ from the baseline (see {artifacts}).");
        }
    }
}
