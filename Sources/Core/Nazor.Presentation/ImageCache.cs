using Nazor.Graphics;

namespace Nazor.Presentation;

/// <summary>A decoded image and the display scale of the variant it came from.</summary>
/// <param name="Image">The rendered or decoded image to process.</param>
/// <param name="Scale">The scale (float).</param>
public sealed record LoadedImage(DecodedImage Image, float Scale)
{
    /// <summary>The size in logical units: "logo@2x.png" of 200 pixels is 100 wide.</summary>
    public Size NaturalSize => new(Image.Size.Width / Scale, Image.Size.Height / Scale);
}

internal sealed class ImageCache(IImageDecoder? decoder)
{
    private readonly Dictionary<(string Key, float Scale), Task<LoadedImage>> entries = [];

    public event Action? Completed;

    public int Count => entries.Count;

    public Task<LoadedImage> Load(ImageSource source, float scale)
    {

        var key = (source.Key, source.HasVariants ? scale : 1);
        if (entries.TryGetValue(key, out var task)) return task;
        task = LoadCore(source, scale);
        entries[key] = task;
        task.ContinueWith(_ => Completed?.Invoke(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return task;
    }

    public void Forget(ImageSource source, float scale) => entries.Remove((source.Key, source.HasVariants ? scale : 1));

    private async Task<LoadedImage> LoadCore(ImageSource source, float scale)
    {
        if (decoder is null) throw new InvalidOperationException("This scene has no image decoder.");
        var data = await source.LoadAsync(scale).ConfigureAwait(false);

        var image = decoder.Decode(data.Bytes) ?? throw new InvalidDataException($"'{source}' is not an image in a supported format.");
        return new LoadedImage(image, data.Scale);
    }
}

internal sealed class ImageState
{
    public ImageSource? Source;
    public float Scale;
    public Task<LoadedImage>? Task;

    public LoadedImage? Shown;

    public bool Reported;
}
