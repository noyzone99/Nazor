using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Platform;
using SkiaSharp;

namespace Nazor;

/// <summary>UI-thread context for one native tray item. Created and disposed by the application runtime.</summary>
public sealed class TrayItemContext : IDisposable
{
    private readonly IPlatformTrayItem? native;
    private Dictionary<int, Action> commands = [];
    private CancellationTokenSource? imageLoad;
    private readonly Dictionary<(string Key, int Size), RenderedImage> imageCache = [];
    private int nextId;
    private int generation;
    private bool disposed, requestedVisible;
    /// <summary>Gets the UI dispatcher. It is available after the runtime context is attached.</summary>
    public Dispatcher Dispatcher { get; }
    /// <summary>Gets whether a tray host is currently available.</summary>
    public bool IsSupported => !disposed && native?.IsSupported is true;
    /// <summary>Gets visibility known to the backend; shell overflow placement is not guaranteed.</summary>
    public bool IsVisible => !disposed && native?.IsVisible is true;
    /// <summary>Occurs for primary activation, typically a left click or keyboard activation.</summary>
    public event Action? Activated;
    /// <summary>Occurs for alternative activation, typically a middle click, when reported by the desktop.</summary>
    public event Action? SecondaryActivated;
    /// <summary>Occurs when the backend detects a visibility change.</summary>
    public event Action? VisibilityChanged;
    /// <summary>Occurs synchronously before menu presentation when reported by the desktop. Cancellation is backend-dependent.</summary>
    public event Action<TrayMenuOpeningEventArgs>? MenuOpening;
    internal TrayItemContext(IPlatformTrayItem? native, UIDispatcher dispatcher)
    {
        this.native = native;
        Dispatcher = dispatcher;
        if (native is null) return;
        native.Activated += OnActivated;
        native.SecondaryActivated += OnSecondaryActivated;
        native.VisibilityChanged += OnVisibilityChanged;
        native.MenuOpening += OnMenuOpening;
        native.MenuItemInvoked += Invoke;
    }
    private void OnActivated() { if (!disposed) Activated?.Invoke(); }
    private void OnSecondaryActivated() { if (!disposed) SecondaryActivated?.Invoke(); }
    private void OnVisibilityChanged() { if (!disposed) VisibilityChanged?.Invoke(); }
    private void OnMenuOpening(TrayMenuOpeningEventArgs args) { if (!disposed) MenuOpening?.Invoke(args); }
    private void Invoke(int id) { if (!disposed && requestedVisible && commands.TryGetValue(id, out var callback)) callback(); }

    private sealed class MenuBudget { internal int Count; }
    internal void Validate(SystemTrayMenu menu) => Build(menu, new HashSet<string>(StringComparer.Ordinal), new(), new(), 0, false, true, new());
    /// <summary>Publishes an icon and typed menu on the UI thread. Images load asynchronously.</summary>
    public bool Show(ImageSource? icon, string tip, SystemTrayMenu menu)
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(menu);
        SystemTrayLabeledItem.ValidateText(tip, nameof(tip));
        if (tip.Length > 127) throw new ArgumentException("Tray tooltips support at most 127 UTF-16 units.", nameof(tip));
        var callbacks = new Dictionary<int, Action>();
        var images = new Dictionary<int, ImageSource>();
        var entries = Build(menu, new HashSet<string>(StringComparer.Ordinal), callbacks, images, 0, true, true, new());
        imageLoad?.Cancel();
        imageLoad?.Dispose();
        imageLoad = null;
        var current = ++generation;
        requestedVisible = true;
        commands = callbacks;
        var wanted = images.Values.Select(source => (source.Key, 16)).ToHashSet();
        if (icon is not null) wanted.Add((icon.Key, 32));
        foreach (var key in imageCache.Keys.Where(key => !wanted.Contains(key)).ToArray()) imageCache.Remove(key);
        RenderedImage? Cached(ImageSource? source, int size) => source is not null && imageCache.TryGetValue((source.Key, size), out var image) ? image : null;
        NativeTrayMenuItem[] Decorate(IReadOnlyList<NativeTrayMenuItem> items) => items.Select(item => item with
        {
            Icon = images.TryGetValue(item.Id, out var source) ? Cached(source, 16) : null,
            Children = Decorate(item.Children)
        }).ToArray();
        var options = new TrayIconOptions { Icon = Cached(icon, 32), ToolTip = tip, Menu = Decorate(entries) };
        var accepted = native?.Show(options) is true;
        if (disposed || !requestedVisible || generation != current) return false;
        var missing = images.Where(pair => !imageCache.ContainsKey((pair.Value.Key, 16))).ToDictionary(pair => pair.Key, pair => pair.Value);
        var missingIcon = icon is not null && options.Icon is null ? icon : null;
        if (native is not null && (missingIcon is not null || missing.Count != 0))
        {
            imageLoad = new CancellationTokenSource();
            _ = LoadImagesAsync(options, missingIcon, missing, current, imageLoad.Token);
        }
        return accepted;
    }

    private NativeTrayMenuItem[] Build(SystemTrayMenu menu, HashSet<string> ids, Dictionary<int, Action> callbacks,
        Dictionary<int, ImageSource> images, int depth, bool allocate, bool enabled, MenuBudget budget)
    {
        if (depth > 32) throw new ArgumentException("Tray menus cannot exceed 32 nested levels.", nameof(menu));
        var result = new List<NativeTrayMenuItem>();
        foreach (var item in menu.Items)
        {
            if (item is SystemTrayRadioGroup group)
            {
                foreach (var choice in group.Choices())
                    Add(choice.Item, NativeTrayMenuItemKind.Radio, choice.Selected, choice.Invoke);
                continue;
            }
            switch (item)
            {
                case SystemTraySeparator:
                    Count();
                    result.Add(new(allocate ? checked(++nextId) : 0, NativeTrayMenuItemKind.Separator));
                    break;
                case SystemTrayCommand command:
                    Unique(command.Id);
                    Add(command, NativeTrayMenuItemKind.Command, false, command.Invoke);
                    break;
                case SystemTrayCheckBox check:
                    Unique(check.Id);
                    Add(check, NativeTrayMenuItemKind.CheckBox, check.IsChecked, check.Invoke);
                    break;
                case SystemTraySubmenu submenu:
                    var children = Build(submenu.Menu, ids, callbacks, images, depth + 1, allocate, enabled && submenu.Enabled && submenu.Visible, budget);
                    Add(submenu, NativeTrayMenuItemKind.Submenu, false, null, children);
                    break;
                default:
                    throw new ArgumentException("Unknown tray menu entry.", nameof(menu));
            }
        }
        return result.ToArray();

        void Count() { if (++budget.Count > ushort.MaxValue) throw new ArgumentException("Too many tray menu items.", nameof(menu)); }
        void Unique(string id)
        {
            if (!ids.Add(id)) throw new ArgumentException($"Duplicate tray command ID '{id}'.", nameof(menu));
        }
        void Add(ITrayLabel item, NativeTrayMenuItemKind kind, bool selected, Action? invoke, NativeTrayMenuItem[]? children = null)
        {
            Count();
            var id = allocate ? checked(++nextId) : 0;
            if (allocate && invoke is not null && enabled && item.Enabled && item.Visible) callbacks.Add(id, invoke);
            if (allocate && item.Icon is not null) images.Add(id, item.Icon);
            result.Add(new(id, kind, item.Text) { Enabled = item.Enabled, Visible = item.Visible, IsChecked = selected, Children = children ?? [] });
        }
    }

    private async Task LoadImagesAsync(TrayIconOptions options, ImageSource? icon, Dictionary<int, ImageSource> sources, int current, CancellationToken cancellation)
    {
        try
        {
            ImageData? trayImage = icon is null ? null : await icon.LoadAsync(1, cancellation).ConfigureAwait(false);
            var loaded = new Dictionary<int, ImageData>();
            foreach (var pair in sources) loaded.Add(pair.Key, await pair.Value.LoadAsync(1, cancellation).ConfigureAwait(false));
            await Dispatcher.InvokeAsync(() =>
            {
                if (disposed || cancellation.IsCancellationRequested || generation != current || !requestedVisible) return;
                if (trayImage is not null) imageCache[(icon!.Key, 32)] = Decode(trayImage.Bytes, 32);
                foreach (var pair in loaded) imageCache[(sources[pair.Key].Key, 16)] = Decode(pair.Value.Bytes, 16);
                var menu = Decorate(options.Menu);
                native!.Show(options with { Icon = icon is null ? options.Icon : imageCache[(icon.Key, 32)], Menu = menu });
                NativeTrayMenuItem[] Decorate(IReadOnlyList<NativeTrayMenuItem> items) => items.Select(item => item with
                {
                    Icon = sources.TryGetValue(item.Id, out var source) ? imageCache[(source.Key, 16)] : item.Icon,
                    Children = Decorate(item.Children)
                }).ToArray();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            ((UIDispatcher)Dispatcher).Post(() =>
            {
                if (!disposed && !cancellation.IsCancellationRequested && generation == current)
                    throw new InvalidOperationException("Could not load a tray image.", error);
            });
        }
    }
    private static RenderedImage Decode(byte[] bytes, int size)
    {
        using var source = SKBitmap.Decode(bytes) ?? throw new InvalidDataException("Invalid tray image.");
        using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        var scale = (float)size / Math.Max(source.Width, source.Height);
        var width = source.Width * scale;
        var height = source.Height * scale;
        canvas.DrawBitmap(source, new SKRect((size - width) / 2, (size - height) / 2, (size + width) / 2, (size + height) / 2), new SKSamplingOptions(SKFilterMode.Linear));
        canvas.Flush();
        var pixels = new byte[checked(size * size * 4)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        return new RenderedImage(size, size, pixels);
    }
    /// <summary>Hides this registration without disposing the context.</summary>
    public void Hide()
    {
        Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        requestedVisible = false;
        ++generation;
        commands.Clear();
        imageLoad?.Cancel();
        native?.Hide();
    }
    /// <summary>Stops and releases this instance. Repeated calls do nothing.</summary>
    public void Dispose()
    {
        if (disposed) return;
        Dispatcher.AssertAccess();
        disposed = true;
        ++generation;
        commands.Clear();
        imageCache.Clear();
        imageLoad?.Cancel();
        imageLoad?.Dispose();
        if (native is null) return;
        native.Activated -= OnActivated;
        native.SecondaryActivated -= OnSecondaryActivated;
        native.VisibilityChanged -= OnVisibilityChanged;
        native.MenuOpening -= OnMenuOpening;
        native.MenuItemInvoked -= Invoke;
        native.Dispose();
    }
}
