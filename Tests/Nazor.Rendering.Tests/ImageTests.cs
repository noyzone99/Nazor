using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;
using SkiaSharp;
using ImageControl = Nazor.Controls.Image;

namespace Nazor.Rendering.Tests;

public sealed class Album : ComponentBase
{
    public List<(string Class, ImageSource? Source)> Photos { get; set; } = [];
    public List<string> Events { get; } = [];

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Album));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            for (var i = 0; i < Photos.Count; i++)
            {
                var (name, source) = Photos[i];
                page.OpenComponent<ImageControl>(3, typeof(Album));
                page.SetKey(i);
                page.AddComponentParameter(4, "Class", name);
                page.AddComponentParameter(5, "Source", source);
                page.AddComponentParameter(6, "Alt", name);
                page.AddComponentParameter(7, "OnLoad", EventCallback.Factory.Create<ImageLoadEventArgs>(this, e => Events.Add($"{name} load {e.Width}x{e.Height}")));
                page.AddComponentParameter(8, "OnError", EventCallback.Factory.Create<ImageErrorEventArgs>(this, e => Events.Add($"{name} error")));
                page.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class ImageTests : IDisposable
{
    private static readonly Graphics.Color Red = new(255, 0, 0, 255);
    private static readonly Graphics.Color Blue = new(0, 0, 255, 255);
    private readonly RenderHarness harness = new();
    private Album album = null!;
    private RootComponent root = null!;

    public ImageTests() => Use("");

    public void Dispose() => harness.Dispose();

    private void Use(string extra)
    {
        var result = NssCompiler.Compile("Album", """
            .page { display: flex; flex-direction: column; align-items: flex-start; background: #ffffff; }
            """ + extra);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Album), () => sheet);
        harness.Scene.Styles.InvalidateAll();
    }

    private byte[] Png(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var left = x < width / 2;
                (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = left ? ((byte)0, (byte)0, (byte)255, (byte)255) : ((byte)255, (byte)0, (byte)0, (byte)255);
            }
        return harness.Graphics.EncodePng(new RenderedImage(width, height, pixels));
    }

    private void Show(params (string Class, ImageSource? Source)[] photos)
    {
        root = harness.Host.Render<Album>();
        album = (Album)root.Component;
        album.Photos = [.. photos];
        album.Refresh();
        harness.Host.RunPending();
    }

    private RenderedImage Loaded(float width = 200, float height = 200, float scale = 1)
    {
        harness.Render(width, height, scale);
        WaitFor(harness.Scene.WhenImagesLoaded());
        harness.Host.RunPending();
        var image = harness.Render(width, height, scale);
        harness.Host.RunPending();
        return image;
    }

    private static void WaitFor(Task task) => Assert.True(task.Wait(TimeSpan.FromSeconds(10)), "The images did not load in time.");

    private ElementNode Photo(string name) => root.Elements("Image").Single(e => e.Class == name);

    [Fact]
    public void An_image_takes_its_natural_size_and_reports_the_load()
    {
        Show(("plain", ImageSource.FromBytes(Png(40, 20))));
        Assert.Equal(new Rect(0, 0, 0, 0), harness.Scene.GetBounds(Photo("plain")));
        var image = Loaded();
        Assert.Equal(new Rect(0, 0, 40, 20), harness.Scene.GetBounds(Photo("plain")));
        Assert.Equal(Red, image.GetPixel(5, 10));
        Assert.Equal(Blue, image.GetPixel(35, 10));
        Assert.Equal(["plain load 40x20"], album.Events);
        var draw = harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single();
        Assert.Equal(new Graphics.Size(40, 20), draw.Image.Size);
    }

    [Fact]
    public void A_set_width_keeps_the_natural_proportions()
    {
        Use(".wide { width: 100px; } .tall { height: 10px; } .square { width: 30px; aspect-ratio: 1; }");
        var png = Png(40, 20);
        Show(("wide", ImageSource.FromBytes(png)), ("tall", ImageSource.FromBytes(png)), ("square", ImageSource.FromBytes(png)));
        Loaded();
        Assert.Equal(new Graphics.Size(100, 50), harness.Scene.GetBounds(Photo("wide")).Size);
        Assert.Equal(new Graphics.Size(20, 10), harness.Scene.GetBounds(Photo("tall")).Size);
        Assert.Equal(new Graphics.Size(30, 30), harness.Scene.GetBounds(Photo("square")).Size);
    }

    [Fact]
    public void In_block_flow_an_image_keeps_its_natural_width_and_in_a_flex_column_it_stretches()
    {
        Use(".page { display: block; } .stretched { display: block; }");
        Show(("block", ImageSource.FromBytes(Png(40, 20))));
        Loaded();
        Assert.Equal(new Graphics.Size(40, 20), harness.Scene.GetBounds(Photo("block")).Size);
        Use(".page { align-items: stretch; }");
        Loaded();
        Assert.Equal(new Graphics.Size(200, 100), harness.Scene.GetBounds(Photo("block")).Size);
    }

    [Theory]
    [InlineData("fill", 0, 0, 40, 40, false)]
    [InlineData("contain", 0, 10, 40, 20, false)]
    [InlineData("cover", -20, 0, 80, 40, true)]
    [InlineData("none", 18, 19, 4, 2, false)]
    [InlineData("scale-down", 18, 19, 4, 2, false)]
    public void Object_fit_places_the_image_in_its_box(string fit, float x, float y, float width, float height, bool clipped)
    {
        Use($".box {{ width: 40px; height: 40px; object-fit: {fit}; }}");
        Show(("box", ImageSource.FromBytes(Png(4, 2))));
        Loaded();
        var draw = harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single();
        Assert.Equal(new Rect(x, y, width, height), draw.Destination);
        Assert.Equal(clipped ? new Rect(0, 0, 40, 40) : null, draw.Clip);
    }

    [Fact]
    public void Object_position_and_rounded_corners()
    {
        Use(".box { width: 40px; height: 40px; object-fit: contain; object-position: left 25%; border-radius: 8px; }");
        Show(("box", ImageSource.FromBytes(Png(4, 2))));
        Loaded();
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Equal(new Rect(0, 5, 40, 20), commands.OfType<DrawImage>().Single().Destination);
        var clip = commands.OfType<PushClip>().Single();
        Assert.Equal(8, clip.Radius.TopLeft.X);
    }

    [Fact]
    public void Files_have_variants_by_display_scale()
    {
        var directory = Directory.CreateTempSubdirectory("nazor-images-");
        try
        {
            File.WriteAllBytes(Path.Combine(directory.FullName, "logo.png"), Png(20, 10));
            File.WriteAllBytes(Path.Combine(directory.FullName, "logo@2x.png"), Png(40, 20));
            Show(("logo", Path.Combine(directory.FullName, "logo.png")));
            Loaded();
            Assert.Equal(new Graphics.Size(20, 10), harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single().Image.Size);
            Loaded(scale: 2);
            Assert.Equal(new Graphics.Size(40, 20), harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single().Image.Size);
            Assert.Equal(new Graphics.Size(20, 10), harness.Scene.GetBounds(Photo("logo")).Size);
            Loaded(scale: 1.5f);
            Assert.Equal(new Graphics.Size(40, 20), harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>().Single().Image.Size);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void A_missing_or_broken_image_reports_an_error_and_stays_empty()
    {
        Use(".sized { width: 30px; height: 10px; }");
        Show(("missing", ImageSource.FromFile("does-not-exist.png")), ("broken", ImageSource.FromBytes([1, 2, 3])), ("sized", ImageSource.FromBytes([4])));
        Loaded();
        Assert.Equal(["broken error", "missing error", "sized error"], album.Events.Order());
        Assert.Equal(0, harness.Scene.GetBounds(Photo("missing")).Width);
        Assert.Equal(new Graphics.Size(30, 10), harness.Scene.GetBounds(Photo("sized")).Size);
        Assert.Empty(harness.Scene.BuildDisplayList().Commands.OfType<DrawImage>());
    }

    [Fact]
    public void One_source_is_read_and_decoded_once()
    {
        var decoder = new CountingDecoder(harness.Graphics.Images);
        using var host = new TestHost();
        using var scene = new Scene(host.Renderer, harness.Graphics.Fonts, harness.Graphics.Shaper, images: decoder) { Viewport = new Graphics.Size(200, 200) };
        var root = host.Render<Album>();
        var photos = (Album)root.Component;
        var path = Path.Combine(Path.GetTempPath(), $"nazor-once-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, Png(8, 4));
        try
        {
            photos.Photos = [("a", path), ("b", path), ("c", path)];
            photos.Refresh();
            host.RunPending();
            scene.Update();
            WaitFor(scene.WhenImagesLoaded());
            scene.Update();
            Assert.Equal(1, decoder.Count);
            Assert.Equal(3, scene.BuildDisplayList().Commands.OfType<DrawImage>().Count());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void Jpeg_and_webp_decode(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(16, 8);
        bitmap.Erase(new SKColor(0, 128, 0));
        using var data = SKImage.FromBitmap(bitmap).Encode(format, 95);
        var image = harness.Graphics.Images.Decode(data.ToArray());
        Assert.NotNull(image);
        Assert.Equal(new Graphics.Size(16, 8), image.Size);
        Assert.False(image.IsVector);
    }

    [Fact]
    public void Strings_convert_to_sources()
    {
        ImageSource relative = "images/logo.png";
        Assert.Equal("file:" + Path.Combine(AppContext.BaseDirectory, "images", "logo.png"), relative.Key);
        ImageSource web = "https://example.org/a.png";
        Assert.Equal("https://example.org/a.png", web.Key);
        Assert.False(web.HasVariants);
        Assert.Equal(relative, ImageSource.FromFile(Path.Combine(AppContext.BaseDirectory, "images/logo.png")));
        Assert.NotEqual(ImageSource.FromBytes([1]), ImageSource.FromBytes([1]));
    }

    private sealed class CountingDecoder(IImageDecoder inner) : IImageDecoder
    {
        private int count;

        public int Count => count;

        public DecodedImage? Decode(ReadOnlySpan<byte> data)
        {
            Interlocked.Increment(ref count);
            return inner.Decode(data);
        }
    }
}
