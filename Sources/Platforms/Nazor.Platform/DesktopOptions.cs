using Nazor.Graphics;

namespace Nazor;

/// <summary>A prepared native tray snapshot. Images contain packed premultiplied BGRA pixels.</summary>
public sealed record TrayIconOptions
{
    /// <summary>Gets or sets the requested icon. Null selects the application default; image loading is asynchronous.</summary>
    public RenderedImage? Icon { get; init; }
    /// <summary>Gets or sets the nonempty tooltip, limited to 127 UTF-16 units without NUL.</summary>
    public string ToolTip { get; init; } = "Nazor";
    /// <summary>Gets or sets the menu snapshot. Replacing it updates a visible item.</summary>
    public IReadOnlyList<NativeTrayMenuItem> Menu { get; init; } = [];

    /// <summary>Validates and copies a complete native menu tree before changing shell state.</summary>
    public TrayIconOptions Snapshot()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ToolTip);
        if (ToolTip.Length > 127 || ToolTip.Contains('\0')) throw new ArgumentException("Invalid tray tooltip.");
        ValidateImage(Icon);
        var ids = new HashSet<int>();
        var count = 0;
        return this with { Menu = Copy(Menu, 0) };
        NativeTrayMenuItem[] Copy(IReadOnlyList<NativeTrayMenuItem> items, int depth)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0) return [];
            if (depth > 32) throw new ArgumentException("Tray menu nesting exceeds 32 levels.");
            return items.Select(item =>
            {
                ArgumentNullException.ThrowIfNull(item);
                if (++count > ushort.MaxValue) throw new ArgumentException("Too many tray entries.");
                if (!Enum.IsDefined(item.Kind)) throw new ArgumentException("Invalid tray item kind.");
                if (item.Id <= 0 || !ids.Add(item.Id)) throw new ArgumentException("Native tray IDs must be positive and unique.");
                if (item.Kind != NativeTrayMenuItemKind.Separator)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(item.Text);
                    if (item.Text.Contains('\0')) throw new ArgumentException("Menu text cannot contain NUL.");
                }
                ArgumentNullException.ThrowIfNull(item.Children);
                if (item.Kind != NativeTrayMenuItemKind.Submenu && item.Children.Count != 0) throw new ArgumentException("Only submenus can have children.");
                ValidateImage(item.Icon);
                return item with { Children = Copy(item.Children, depth + 1) };
            }).ToArray();
        }
    }
    private static void ValidateImage(RenderedImage? image)
    {
        if (image is not null && (image.Width <= 0 || image.Height <= 0 || image.Pixels.LongLength != (long)image.Width * image.Height * 4))
            throw new ArgumentException("Tray images require packed BGRA pixels and positive dimensions.");
    }

}

/// <summary>Native menu representation; applications use the typed SystemTrayMenu API.</summary>
public enum NativeTrayMenuItemKind
{
    /// <summary>An executable command.</summary>
    Command,
    /// <summary>A visual separator.</summary>
    Separator,
    /// <summary>A nested menu.</summary>
    Submenu,
    /// <summary>An independent boolean choice.</summary>
    CheckBox,
    /// <summary>An exclusive choice indicator.</summary>
    Radio,
}

/// <summary>One immutable native menu entry. IDs are private to an individual tray registration.</summary>
public sealed record NativeTrayMenuItem(int Id, NativeTrayMenuItemKind Kind, string Text = "")
{
    /// <summary>Gets whether this entry can be invoked.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Gets whether this entry is included in the menu.</summary>
    public bool Visible { get; init; } = true;
    /// <summary>Gets the declared checked state; activation requests a change without mutating it.</summary>
    public bool IsChecked { get; init; }
    /// <summary>Gets the prepared native menu icon; null means no icon.</summary>
    public RenderedImage? Icon { get; init; }
    /// <summary>Gets the nested native menu entries.</summary>
    public IReadOnlyList<NativeTrayMenuItem> Children { get; init; } = [];
}

/// <summary>A synchronous menu-opening request. Cancellation is sticky and available only when CanCancel is true.</summary>
public sealed class TrayMenuOpeningEventArgs(bool canCancel) : EventArgs
{
    /// <summary>Gets whether this native presentation path supports cancellation.</summary>
    public bool CanCancel { get; } = canCancel;
    /// <summary>Gets whether cancellation has been accepted; it cannot be revoked.</summary>
    public bool IsCanceled { get; private set; }
    /// <summary>Cancels the default menu opening when supported. Returns false without changing state otherwise.</summary>
    public bool TryCancel()
    {
        if (!CanCancel) return false;
        IsCanceled = true;
        return true;
    }
}

/// <summary>Specifies the standard system-notification icon.</summary>
public enum SystemNotificationIcon
{
    /// <summary>No standard notification icon.</summary>
    None,
    /// <summary>An information icon.</summary>
    Information,
    /// <summary>A warning icon.</summary>
    Warning,
    /// <summary>An error icon.</summary>
    Error,
}

/// <summary>A transient notification. Windows limits titles/messages to 63/255 UTF-16 units.</summary>
public sealed record SystemNotification(string Title, string Message, SystemNotificationIcon Icon = SystemNotificationIcon.Information);
