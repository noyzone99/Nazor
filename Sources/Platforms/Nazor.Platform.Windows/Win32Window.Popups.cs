using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    private readonly List<PopupWindow> popupWindows = [];
    bool INativePopupOwner.CanShowInactivePopups => true;

    Rect? INativePopupOwner.GetPopupWorkArea(Rect anchor)
    {
        POINT origin = default;
        if (!ClientToScreen(hwnd, ref origin)) return null;
        var anchorPixels = new RECT
        {
            left = origin.X + (int)MathF.Floor(anchor.X * Scale),
            top = origin.Y + (int)MathF.Floor(anchor.Y * Scale),
            right = origin.X + (int)MathF.Ceiling(anchor.Right * Scale),
            bottom = origin.Y + (int)MathF.Ceiling(anchor.Bottom * Scale),
        };
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfo(MonitorFromRect(anchorPixels, MONITOR_DEFAULTTONEAREST), ref info)) return null;
        return new Rect((info.rcWork.left - origin.X) / Scale, (info.rcWork.top - origin.Y) / Scale,
            (info.rcWork.right - info.rcWork.left) / Scale, (info.rcWork.bottom - info.rcWork.top) / Scale);
    }

    INativePopup INativePopupOwner.CreatePopup(IWindowHost host, INativePopup? parent, Action changed, Action dismissed) =>
        new PopupWindow(this, host, parent as PopupWindow, changed, dismissed);

    private sealed class PopupWindow : INativePopup, IMessageTarget
    {
        private const uint WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3, HTTRANSPARENT = -1;
        private readonly Win32Window owner;
        private readonly IWindowHost host;
        private readonly Action changed, dismissed;
        private GCHandle self;
        private nint handle;
        private NativePopupPlacement placement;
        private Rect inputBounds;
        private (int X, int Y) screenPosition;
        private bool shown, tracking;

        public PopupWindow(Win32Window owner, IWindowHost host, PopupWindow? parent, Action changed, Action dismissed)
        {
            this.owner = owner;
            this.host = host;
            this.changed = changed;
            this.dismissed = dismissed;
            if (!Win32Platform.EnsurePopupClass()) return;
            self = GCHandle.Alloc(this);
            fixed (char* name = Win32Platform.PopupClassName)
                handle = CreateWindowExW(WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW, name, null, WS_POPUP,
                    0, 0, 0, 0, parent?.handle ?? owner.hwnd, 0, Win32Platform.Instance, GCHandle.ToIntPtr(self));
            if (handle != 0) owner.popupWindows.Add(this);
            else if (self.IsAllocated) self.Free();
        }

        public Rect? Place(NativePopupPlacement value)
        {
            if (handle == 0) return null;
            placement = value;
            screenPosition = ScreenPosition();
            return placement.Bounds;
        }

        public void SetInputBounds(Rect bounds) => inputBounds = bounds;

        public bool Present(RenderedImage image, float scale)
        {
            if (handle == 0) return false;
            fixed (byte* pixels = image.Pixels)
                if (!PresentLayeredPixels(handle, pixels, image.Width, image.Height, image.Width * 4, screenPosition.X, screenPosition.Y)) return false;
            if (!shown)
            {
                shown = true;
                ShowWindow(handle, SW_SHOWNOACTIVATE);
            }
            return true;
        }

        private (int X, int Y) ScreenPosition()
        {
            POINT origin = default;
            ClientToScreen(owner.hwnd, ref origin);
            return (origin.X + (int)MathF.Round((placement.Bounds.X - placement.Shadow) * owner.Scale),
                origin.Y + (int)MathF.Round((placement.Bounds.Y - placement.Shadow) * owner.Scale));
        }

        public void OwnerMoved()
        {
            if (handle == 0) return;
            var position = ScreenPosition();
            if (position == screenPosition) return;
            screenPosition = position;
            if (shown) SetWindowPos(handle, 0, position.X, position.Y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            changed();
        }

        public void OwnerBeginMove()
        {
            if (placement.LightDismiss) dismissed();
        }

        private Point PointFrom(nint packed) => new(placement.Bounds.X - placement.Shadow + LowSigned(packed) / owner.Scale,
            placement.Bounds.Y - placement.Shadow + HighSigned(packed) / owner.Scale);

        public nint OnMessage(nint window, uint message, nint wParam, nint lParam)
        {
            switch (message)
            {
                case WM_MOUSEACTIVATE: return MA_NOACTIVATE;
                case WM_NCHITTEST:
                    return inputBounds.Contains(owner.ClientPointFromScreen(lParam)) ? HTCLIENT : HTTRANSPARENT;
                case WM_MOUSEMOVE:
                    if (!tracking)
                    {
                        var track = new TRACKMOUSEEVENT { cbSize = (uint)sizeof(TRACKMOUSEEVENT), dwFlags = TME_LEAVE, hwndTrack = window };
                        tracking = TrackMouseEvent(&track);
                    }
                    host.OnPointerMoved(PointFrom(lParam), Modifiers());
                    return 0;
                case WM_MOUSELEAVE:
                    tracking = false;
                    host.OnPointerLeft();
                    return 0;
                case WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN:
                    host.OnPointerPressed(PointFrom(lParam), ButtonOf(message), Modifiers());
                    return 0;
                case WM_LBUTTONUP or WM_RBUTTONUP or WM_MBUTTONUP:
                    host.OnPointerReleased(PointFrom(lParam), ButtonOf(message), Modifiers());
                    return 0;
                case WM_MOUSEWHEEL:
                    host.OnPointerWheel(owner.ClientPointFromScreen(lParam), HighSigned(wParam) / 120f, Modifiers());
                    return 0;
                case WM_MOUSEHWHEEL:
                    host.OnPointerHorizontalWheel(owner.ClientPointFromScreen(lParam), HighSigned(wParam) / 120f, Modifiers());
                    return 0;
                case WM_GETOBJECT:
                    return owner.OnMessage(window, message, wParam, lParam);
                default: return DefWindowProcW(window, message, wParam, lParam);
            }
        }

        public void OnFinalMessage()
        {
            handle = 0;
            owner.popupWindows.Remove(this);
            if (self.IsAllocated) self.Free();
        }

        public void Dispose()
        {
            if (handle != 0) DestroyWindow(handle);
        }
    }
}
