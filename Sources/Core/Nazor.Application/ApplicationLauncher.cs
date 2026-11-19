using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Platform;
using SkiaSharp;

namespace Nazor;

/// <summary>Application-wide launcher tasks and one mutually exclusive count or icon badge. Operations require the UI thread during Run.</summary>
public interface IApplicationLauncher
{
    /// <summary>Gets backend support, independently of shell presentation and user preferences.</summary>
    ApplicationLauncherCapabilities Capabilities { get; }
    /// <summary>Occurs on the UI thread when the backend reports changed support.</summary>
    event Action? CapabilitiesChanged;
    /// <summary>Replaces the application's task snapshot. Empty removes its tasks. Returns acceptance, not guaranteed display.</summary>
    /// <param name="tasks">Tasks copied before publication. Invalid labels, null arguments and NUL characters are rejected.</param>
    /// <returns>False when unsupported or rejected, preserving the previous task list.</returns>
    bool SetJumpList(IReadOnlyList<JumpListTask> tasks);
    /// <summary>Replaces the badge with a nonnegative count. Zero clears either kind of badge.</summary>
    /// <remarks>Nazor does not abbreviate or cap the count. Windows draws every digit; other shells control presentation. Apply any application-specific limit before calling.</remarks>
    /// <param name="count">The count; negative values throw ArgumentOutOfRangeException.</param>
    /// <returns>Whether the backend accepted the request. Rejection preserves the previous badge.</returns>
    bool SetBadgeCount(int count);
    /// <summary>Schedules image loading and replacement of the badge. Failure is diagnosed without clearing the previous badge.</summary>
    /// <param name="icon">The image source, loaded asynchronously with display-scale variants.</param>
    /// <param name="description">Nonempty accessible description without NUL characters.</param>
    /// <returns>Whether loading was accepted. Native publication follows asynchronously and is not guaranteed.</returns>
    bool SetBadgeIcon(ImageSource icon, string description);
    /// <summary>Clears either badge kind without modifying the application icon or launcher tasks.</summary>
    /// <returns>Whether clearing was accepted. Unsupported backends return false.</returns>
    bool ClearBadge();
}

internal sealed class ApplicationLauncher : IApplicationLauncher
{
    private IPlatformDesktopIntegration? backend;
    private UIDispatcher? dispatcher;
    private CancellationTokenSource? imageLoad;
    private bool badgeRequested;
    public ApplicationLauncherCapabilities Capabilities => backend?.LauncherCapabilities ?? default;
    public event Action? CapabilitiesChanged;

    internal void Start(IPlatformDesktopIntegration? integration, UIDispatcher uiDispatcher)
    {
        backend = integration;
        dispatcher = uiDispatcher;
        if (backend is not null) backend.LauncherCapabilitiesChanged += OnCapabilitiesChanged;
    }

    internal void Stop()
    {
        if (dispatcher is null) return;
        dispatcher.AssertAccess();
        CancelImage();
        try
        {
            if (backend is not null)
            {
                backend.LauncherCapabilitiesChanged -= OnCapabilitiesChanged;
                if (badgeRequested) backend.ClearBadge();
            }
        }
        finally { backend = null; dispatcher = null; badgeRequested = false; }
    }

    public bool SetJumpList(IReadOnlyList<JumpListTask> tasks)
    {
        AssertRunning();
        ArgumentNullException.ThrowIfNull(tasks);
        var snapshot = tasks.Select(task =>
        {
            ArgumentNullException.ThrowIfNull(task);
            ValidateText(task.Title, nameof(tasks));
            ArgumentNullException.ThrowIfNull(task.Arguments);
            var arguments = task.Arguments.ToArray();
            foreach (var argument in arguments)
                if (argument is null || argument.Contains('\0')) throw new ArgumentException("Launcher arguments must be non-null and contain no NUL.", nameof(tasks));
            return task with { Arguments = Array.AsReadOnly(arguments) };
        }).ToArray();
        return Capabilities.SupportsJumpList && backend!.SetJumpList(Array.AsReadOnly(snapshot));
    }

    public bool SetBadgeCount(int count)
    {
        AssertRunning();
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return ClearBadge();
        if (!Capabilities.SupportsBadgeCount || !backend!.SetBadgeCount(count)) return false;
        CancelImage();
        badgeRequested = true;
        return true;
    }

    public bool ClearBadge()
    {
        AssertRunning();
        if ((!Capabilities.SupportsBadgeCount && !Capabilities.SupportsBadgeIcon) || !backend!.ClearBadge()) return false;
        CancelImage();
        badgeRequested = false;
        return true;
    }

    public bool SetBadgeIcon(ImageSource icon, string description)
    {
        AssertRunning();
        ArgumentNullException.ThrowIfNull(icon);
        ValidateText(description, nameof(description));
        if (!Capabilities.SupportsBadgeIcon) return false;
        CancelImage();
        badgeRequested = true;
        imageLoad = new CancellationTokenSource();
        _ = LoadIconAsync(icon, description, backend!, dispatcher!, imageLoad.Token);
        return true;
    }

    private static async Task LoadIconAsync(ImageSource icon, string description, IPlatformDesktopIntegration native,
        UIDispatcher uiDispatcher, CancellationToken cancellation)
    {
        try
        {
            var images = new List<RenderedImage>();
            ImageData? shared = null;
            foreach (var size in new[] { 16, 20, 24, 32, 48, 64 })
            {
                var data = shared ?? await icon.LoadAsync(size / 16f, cancellation).ConfigureAwait(false);
                if (!icon.HasVariants) shared = data;
                cancellation.ThrowIfCancellationRequested();
                images.Add(Decode(data.Bytes, size));
            }
            await uiDispatcher.InvokeAsync(() =>
            {
                if (!cancellation.IsCancellationRequested && !native.SetBadgeIcon(images, description))
                    Console.Error.WriteLine("Nazor: the desktop rejected the badge icon.");
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!cancellation.IsCancellationRequested) Console.Error.WriteLine($"Nazor: could not load the badge icon: {error.Message}");
        }
    }

    private static RenderedImage Decode(byte[] bytes, int size)
    {
        using var source = SKBitmap.Decode(bytes) ?? throw new InvalidDataException("Invalid badge image.");
        var factor = (float)size / Math.Max(source.Width, source.Height);
        var width = Math.Max(1, (int)MathF.Round(source.Width * factor));
        var height = Math.Max(1, (int)MathF.Round(source.Height * factor));
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear));
        canvas.Flush();
        var pixels = new byte[checked(width * height * 4)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        return new(width, height, pixels);
    }

    private void CancelImage() { imageLoad?.Cancel(); imageLoad?.Dispose(); imageLoad = null; }
    private void AssertRunning() => (dispatcher ?? throw new InvalidOperationException("Launcher services are available during Run.")).AssertAccess();
    private static void ValidateText(string value, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        if (value.Contains('\0')) throw new ArgumentException("Launcher text cannot contain NUL.", parameter);
    }
    private void OnCapabilitiesChanged()
    {
        var current = dispatcher;
        current?.Post(() => { if (dispatcher == current) CapabilitiesChanged?.Invoke(); });
    }
}
