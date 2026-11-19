using Nazor.Graphics;
using Nazor.Input;
using static Nazor.Platform.Linux.LinuxNativeApi;

namespace Nazor.Platform.Linux;

public abstract unsafe partial class LinuxDesktopPlatform
{
    private readonly Dictionary<nint, LinuxDesktopWindow.PopupWindow> popupWindows = [];

    internal void AddPopup(nint handle, LinuxDesktopWindow.PopupWindow popup) => popupWindows.Add(handle, popup);
    internal void RemovePopup(nint handle) => popupWindows.Remove(handle);
}

internal sealed unsafe partial class LinuxDesktopWindow
{
    private uint popupSerial;
    private readonly List<PopupWindow> popupWindows = [];
    private bool keyboardFocused, popupFocusPending;
    bool INativePopupOwner.CanShowInactivePopups => false;

    internal void FlushPopupFocus()
    {
        if (!popupFocusPending || IsClosed) return;
        popupFocusPending = false;
        var focused = keyboardFocused || popupWindows.Any(popup => popup.KeyboardFocused);
        ReportKeyboardFocus(focused);
        if (!IsClosed && !platform.IsX11) host.OnActivationChanged(focused);
    }

    private void DismissMovingPopups()
    {
        foreach (var popup in popupWindows.AsEnumerable().Reverse().ToArray()) popup.OwnerBeginMove();
    }

    private void ReportKeyboardFocus(bool focused)
    {
        if (!focused)
        {
            StopRepeat();
            host.OnPointerCanceled();
            if (IsClosed) return;
            host.OnComposition("", 0);
            if (IsClosed) return;
        }
        host.OnFocusChanged(enabled && focused);
        if (!IsClosed && focused) SetTextInputCaret(textCaret);
    }

    Rect? INativePopupOwner.GetPopupWorkArea(Rect anchor)
    {
        PixelRect area;
        return Native.PopupWorkArea(Handle, (int)anchor.X, (int)anchor.Y + HeaderInset,
            (int)MathF.Ceiling(anchor.Width), (int)MathF.Ceiling(anchor.Height), &area) != 0
            ? new Rect(area.X, area.Y - HeaderInset, area.Width, area.Height)
            : null;
    }

    INativePopup INativePopupOwner.CreatePopup(IWindowHost host, INativePopup? parent, Action changed, Action dismissed) =>
        new PopupWindow(this, host, parent as PopupWindow, changed, dismissed);

    internal sealed class PopupWindow : INativePopup
    {
        private readonly LinuxDesktopWindow owner;
        private readonly IWindowHost host;
        private readonly PopupWindow? parent;
        private PopupWindow? surfaceParent;
        private readonly Action changed, dismissed;
        private NativePopupPlacement placement;
        private Rect? configuredRelativeBounds;
        private nint handle;
        private bool ready, presented, waitingForParent, closed, keyboardFocused;

        public PopupWindow(LinuxDesktopWindow owner, IWindowHost host, PopupWindow? parent, Action changed, Action dismissed)
        {
            this.owner = owner;
            this.host = host;
            this.parent = parent;
            this.changed = changed;
            this.dismissed = dismissed;
        }

        public bool KeyboardFocused => keyboardFocused && !closed;

        public void OwnerBeginMove()
        {
            if (!closed && placement.LightDismiss) dismissed();
        }

        private Rect? ConfiguredBounds => configuredRelativeBounds is { } bounds
            ? new Rect(bounds.X + ParentOrigin.X, bounds.Y + ParentOrigin.Y, bounds.Width, bounds.Height)
            : null;

        private Point ParentOrigin => surfaceParent is not null
            ? new(surfaceParent.ConfiguredBounds?.X ?? surfaceParent.placement.Bounds.X, surfaceParent.ConfiguredBounds?.Y ?? surfaceParent.placement.Bounds.Y)
            : new(0, -owner.HeaderInset);

        public Rect? Place(NativePopupPlacement value)
        {
            if (closed || owner.IsClosed || parent is { closed: true }) return null;
            if (handle != 0 && (value.Grab != placement.Grab || value.LightDismiss != placement.LightDismiss))
            {
                Dispose();
                return null;
            }
            if (handle == 0) surfaceParent = value.Grab && parent is not null && !parent.placement.Grab ? null : parent;
            placement = value;
            waitingForParent = surfaceParent is { presented: false };
            if (waitingForParent) return value.Bounds;
            var origin = ParentOrigin;
            var parentSize = surfaceParent is null ? new Size(owner.surfaceWidth, owner.surfaceHeight)
                : new Size(surfaceParent.ConfiguredBounds?.Width ?? surfaceParent.placement.Bounds.Width, surfaceParent.ConfiguredBounds?.Height ?? surfaceParent.placement.Bounds.Height);
            var ax = Math.Clamp((int)MathF.Floor(value.Anchor.X - origin.X), 0, Math.Max(0, (int)parentSize.Width - 1));
            var ay = Math.Clamp((int)MathF.Floor(value.Anchor.Y - origin.Y), 0, Math.Max(0, (int)parentSize.Height - 1));
            var native = new PopupPlacement
            {
                X = (int)MathF.Round(value.Bounds.X - origin.X), Y = (int)MathF.Round(value.Bounds.Y - origin.Y),
                Width = Math.Max(1, (int)MathF.Ceiling(value.Bounds.Width)), Height = Math.Max(1, (int)MathF.Ceiling(value.Bounds.Height)),
                AnchorX = ax, AnchorY = ay,
                AnchorWidth = Math.Clamp((int)MathF.Ceiling(value.Anchor.Width), 1, Math.Max(1, (int)parentSize.Width - ax)),
                AnchorHeight = Math.Clamp((int)MathF.Ceiling(value.Anchor.Height), 1, Math.Max(1, (int)parentSize.Height - ay)),
                Shadow = (int)value.Shadow,
                Side = value.Side switch { "top" => 1, "right" => 2, "left" => 3, _ => 0 },
                Alignment = value.Alignment switch { "center" => 1, "end" => 2, _ => 0 },
                Offset = (int)MathF.Round(value.Offset),
                Flags = (value.Grab ? 1u : 0u) | (value.Menu ? 2u : 0u) | (value.LightDismiss ? 4u : 0u), Serial = owner.popupSerial,
            };
            if (handle == 0)
            {
                handle = owner.Native.PopupCreate(surfaceParent?.handle ?? owner.Handle, &native);
                if (handle == 0) { closed = true; return null; }
                owner.popupWindows.Add(this);
                owner.platform.AddPopup(handle, this);
                ready = owner.platform.IsX11;
            }
            else if (owner.Native.PopupPlace(handle, &native) == 0)
            {
                Dispose();
                return null;
            }
            return ConfiguredBounds ?? value.Bounds;
        }

        public bool Present(RenderedImage image, float scale)
        {
            if (closed) return false;
            if (waitingForParent) return true;
            if (handle == 0) return false;
            if (!ready) return true;
            if (owner.Native.nz_window_acquire(handle, image.Width, image.Height, out var buffer) <= 0)
            {
                changed();
                return true;
            }
            fixed (byte* pixels = image.Pixels)
                for (var row = 0; row < image.Height; row++)
                    Buffer.MemoryCopy(pixels + row * image.Width * 4, (byte*)buffer.Pixels + row * buffer.Stride, buffer.Stride, image.Width * 4);
            owner.Native.nz_window_present(handle, scale, 0);
            if (!presented) { presented = true; changed(); }
            return true;
        }

        public void SetInputBounds(Rect bounds)
        {
            if (handle == 0 || closed) return;
            var origin = ConfiguredBounds ?? placement.Bounds;
            var x = (int)MathF.Floor(bounds.X - origin.X + placement.Shadow);
            var y = (int)MathF.Floor(bounds.Y - origin.Y + placement.Shadow);
            owner.Native.PopupInputRegion(handle, x, y, (int)MathF.Ceiling(bounds.Width), (int)MathF.Ceiling(bounds.Height));
        }

        public void OnEvent(Event value)
        {
            if (value.Type == 9)
            {
                keyboardFocused = value.A != 0;
                owner.popupFocusPending = true;
                return;
            }
            if (closed) return;
            var bounds = ConfiguredBounds ?? placement.Bounds;
            var point = new Point(bounds.X - placement.Shadow + (float)value.X, bounds.Y - placement.Shadow + (float)value.Y);
            var modifiers = (KeyModifiers)(value.Flags & 0xffff);
            switch (value.Type)
            {
                case 23:
                    configuredRelativeBounds = new((float)value.X, (float)value.Y, value.A, value.B);
                    ready = true;
                    changed();
                    break;
                case 24:
                    closed = true;
                    owner.popupFocusPending = true;
                    dismissed();
                    changed();
                    break;
                case 3 or 5: host.OnPointerMoved(point, modifiers); break;
                case 4: host.OnPointerLeft(); break;
                case 6:
                    if (value.B != 0) owner.popupSerial = (uint)value.D;
                    var button = value.A switch { 272 => Input.PointerButton.Primary, 273 => Input.PointerButton.Secondary, 274 => Input.PointerButton.Middle, _ => Input.PointerButton.None };
                    if (button == Input.PointerButton.None) break;
                    if (value.B != 0) host.OnPointerPressed(point, button, modifiers);
                    else host.OnPointerReleased(point, button, modifiers);
                    break;
                case 7:
                    host.OnPointerScroll(point, value.A == 1 ? value.D / 256f : 0, value.A == 0 ? value.D / 256f : 0,
                        ScrollDeltaUnit.LogicalPixels, modifiers);
                    break;
                case 8 or 10 or 11 or 12: owner.OnEvent(value); break;
                case 14 or 20: changed(); break;
            }
        }

        public void Dispose()
        {
            closed = true;
            if (handle == 0) return;
            foreach (var child in owner.popupWindows.Where(popup => popup.parent == this).Reverse().ToArray()) child.Dispose();
            var previous = handle;
            handle = 0;
            owner.platform.RemovePopup(previous);
            owner.popupWindows.Remove(this);
            owner.Native.nz_window_destroy(previous);
        }
    }
}
