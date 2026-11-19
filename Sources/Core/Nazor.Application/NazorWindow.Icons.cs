using Nazor.Graphics;

namespace Nazor;

public sealed partial class NazorWindow
{
    private ImageSource? icon;
    private CancellationTokenSource? iconLoad;
    private float iconScale;
    private bool iconApplied;

    /// <summary>The requested native icon; null restores the application icon. Changes apply asynchronously.</summary>
    public ImageSource? Icon
    {
        get => icon;
        set
        {
            Dispatcher.AssertAccess();
            if (disposed || Equals(icon, value)) return;
            icon = value;
            RefreshIcon();
            NotifyPresentation();
        }
    }

    private void RefreshIcon()
    {
        if (disposed || platformWindow is null) return;
        iconLoad?.Cancel();
        iconLoad?.Dispose();
        iconLoad = null;
        iconScale = platformWindow.Scale;
        if (!platformWindow.Capabilities.CanSetWindowIcon) return;
        if (icon is null)
        {
            if (iconApplied) platformWindow.SetIcon(null);
            iconApplied = false;
            return;
        }
        iconLoad = new CancellationTokenSource();
        _ = LoadIconAsync(icon, iconScale, iconLoad.Token);
    }

    private async Task LoadIconAsync(ImageSource source, float scale, CancellationToken cancellation)
    {
        try
        {
            var data = await source.LoadAsync(scale, cancellation).ConfigureAwait(false);
            if (cancellation.IsCancellationRequested) return;
            Dispatcher.Post(() =>
            {
                if (disposed || cancellation.IsCancellationRequested || platformWindow is not { IsClosed: false }) return;
                var image = graphics.Images.Decode(data.Bytes)
                    ?? throw new InvalidDataException($"'{source}' is not an image in a supported format.");
                try
                {
                    if (!(image.Size.Width > 0 && image.Size.Height > 0))
                        throw new InvalidDataException($"'{source}' has invalid image dimensions.");
                    var sizes = new[] { (int)MathF.Ceiling(16 * scale), (int)MathF.Ceiling(32 * scale),
                        (int)MathF.Ceiling(48 * scale), 256 }.Distinct();
                    var images = sizes.Select(size =>
                    {
                        var factor = size / MathF.Max(image.Size.Width, image.Size.Height);
                        var width = image.Size.Width * factor;
                        var height = image.Size.Height * factor;
                        var list = new DisplayList(new Size(size, size),
                            [new DrawImage(image, new Rect((size - width) / 2, (size - height) / 2, width, height))]);
                        return graphics.RenderToImage(list, 1, default);
                    }).ToArray();
                    platformWindow.SetIcon(images);
                    iconApplied = true;
                }
                finally { (image as IDisposable)?.Dispose(); }
            });
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Dispatcher.Post(() =>
            {
                if (!disposed && !cancellation.IsCancellationRequested) OnException(exception);
            });
        }
    }
}
