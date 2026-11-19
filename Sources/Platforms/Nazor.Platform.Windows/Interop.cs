using System.Runtime.InteropServices;

namespace Nazor.Platform.Windows;

internal static unsafe partial class Win32
{
    public const uint WS_EX_TOOLWINDOW = 0x00000080, WS_EX_APPWINDOW = 0x00040000, WS_EX_TOPMOST = 0x00000008;
    public const uint WM_GETMINMAXINFO = 0x0024;
    [StructLayout(LayoutKind.Sequential)] public struct MINMAXINFO { public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize; }
    public const uint WM_DESTROY = 0x0002, WM_SIZE = 0x0005, WM_ACTIVATE = 0x0006, WM_SETFOCUS = 0x0007, WM_KILLFOCUS = 0x0008,
        WM_PAINT = 0x000F, WM_CLOSE = 0x0010, WM_QUIT = 0x0012, WM_ERASEBKGND = 0x0014, WM_SETCURSOR = 0x0020, WM_GETOBJECT = 0x003D,
        WM_NCCREATE = 0x0081, WM_NCDESTROY = 0x0082, WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_CHAR = 0x0102,
        WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105, WM_SYSCHAR = 0x0106, WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202, WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205, WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208,
        WM_MOUSEWHEEL = 0x020A, WM_MOUSEHWHEEL = 0x020E, WM_CAPTURECHANGED = 0x0215, WM_MOUSELEAVE = 0x02A3, WM_DPICHANGED = 0x02E0, WM_APP = 0x8000;
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const uint WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_DISABLED = 0x08000000;
    public const uint WS_EX_NOPARENTNOTIFY = 0x00000004, WS_EX_TRANSPARENT = 0x00000020;
    public const uint WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
    public const nint HWND_BOTTOM = 1;
    public const uint WS_MINIMIZE = 0x20000000, WS_MAXIMIZE = 0x01000000;
    public const uint CS_VREDRAW = 0x0001, CS_HREDRAW = 0x0002, CS_OWNDC = 0x0020;
    public const uint CS_DBLCLKS = 0x0008, WM_LBUTTONDBLCLK = 0x0203, WM_RBUTTONDBLCLK = 0x0206, WM_MBUTTONDBLCLK = 0x0209;
    public const int CW_USEDEFAULT = unchecked((int)0x80000000);
    public const int GWLP_USERDATA = -21;
    public const int SW_SHOWNOACTIVATE = 4, SW_SHOW = 5, SW_RESTORE = 9;
    public const uint GW_OWNER = 4;
    public const uint WM_APPCOMMAND = 0x0319;
    public const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_CAPTION_COLOR = 35, DWMWA_SYSTEMBACKDROP_TYPE = 38;
    public const uint DWMWA_CAPTION_BUTTON_BOUNDS = 5;
    public const uint WS_CAPTION = 0x00C00000;
    public const int HTMINBUTTON = 8, HTCLOSE = 20;
    [LibraryImport("dwmapi.dll")][return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DwmDefWindowProc(nint hwnd, uint message, nint wParam, nint lParam, out nint result);
    [StructLayout(LayoutKind.Sequential)] public struct MARGINS { public int Left, Right, Top, Bottom; }
    [LibraryImport("dwmapi.dll")] public static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, void* value, uint size);
    [LibraryImport("dwmapi.dll")] public static partial int DwmGetWindowAttribute(nint hwnd, uint attribute, void* value, uint size);
    [LibraryImport("dwmapi.dll")] public static partial int DwmExtendFrameIntoClientArea(nint hwnd, in MARGINS margins);
    public const int SW_HIDE = 0, SW_SHOWMAXIMIZED = 3;
    public const uint MONITOR_DEFAULTTONULL = 0;
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT { public uint length, flags, showCmd; public POINT ptMinPosition, ptMaxPosition; public RECT rcNormalPosition; }
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GetWindowPlacement(nint hwnd, ref WINDOWPLACEMENT placement);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SetWindowPlacement(nint hwnd, in WINDOWPLACEMENT placement);
    [LibraryImport("user32.dll")] public static partial nint MonitorFromRect(in RECT rect, uint flags);
    public const long APPCOMMAND_BROWSER_BACKWARD = 1, APPCOMMAND_BROWSER_FORWARD = 2;
    public const uint CLSCTX_INPROC_SERVER = 1;
    [LibraryImport("ole32.dll")] public static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SHCreateItemFromParsingName(string path, nint bindContext, in Guid iid, out nint item);
    public const uint WS_DIALOGWINDOW = 0x00C80000, WS_EX_DLGMODALFRAME = 0x00000001, MONITOR_DEFAULTTONEAREST = 2;
    [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public uint cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
    [StructLayout(LayoutKind.Sequential)] internal struct MONITORINFOEX { public MONITORINFO Info; public fixed char Device[32]; }
    public const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
    public const uint SIZE_MINIMIZED = 1;
    public const uint WM_TIMER = 0x0113, WM_SHOWWINDOW = 0x0018, WM_SETTINGCHANGE = 0x001A;
    public const uint SPI_GETCLIENTAREAANIMATION = 0x1042, SPI_SETCLIENTAREAANIMATION = 0x1043;
    public const uint SPI_GETHIGHCONTRAST = 0x0042, SPI_SETHIGHCONTRAST = 0x0043, HCF_HIGHCONTRASTON = 0x1, WM_SYSCOLORCHANGE = 0x0015;
    public const uint WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
    public const int COLOR_WINDOW = 5, COLOR_WINDOWTEXT = 8, COLOR_HIGHLIGHT = 13, COLOR_HIGHLIGHTTEXT = 14, COLOR_BTNFACE = 15, COLOR_GRAYTEXT = 17,
        COLOR_BTNTEXT = 18, COLOR_INFOTEXT = 23, COLOR_INFOBK = 24, COLOR_HOTLIGHT = 26;

    [StructLayout(LayoutKind.Sequential)] public struct HIGHCONTRASTW { public uint cbSize, dwFlags; public nint lpszDefaultScheme; }

    [LibraryImport("user32.dll")] public static partial uint GetSysColor(int index);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nuint SetTimer(nint hwnd, nuint id, uint elapse, nint callback);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool KillTimer(nint hwnd, nuint id);
    [LibraryImport("kernel32.dll")] internal static partial nint CreateWaitableTimerExW(nint attributes, nint name, uint flags, uint access);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool SetWaitableTimer(nint timer, long* due, int period, nint callback, nint context, int resume);
    [LibraryImport("kernel32.dll")] internal static partial nint CreateThreadpoolWait(delegate* unmanaged<nint, nint, nint, uint, void> callback, nint context, nint environment);
    [LibraryImport("kernel32.dll")] internal static partial void SetThreadpoolWait(nint wait, nint handle, nint timeout);
    [LibraryImport("kernel32.dll")] internal static partial void WaitForThreadpoolWaitCallbacks(nint wait, int cancelPending);
    [LibraryImport("kernel32.dll")] internal static partial void CloseThreadpoolWait(nint wait);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool CloseHandle(nint handle);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SystemParametersInfoW(uint action, uint param, void* value, uint winIni);
    [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16)] public static partial int RegGetValueW(nint key, string subKey, string value, uint flags, uint* type, void* data, uint* dataSize);
    public const nint HKEY_CURRENT_USER = unchecked((int)0x80000001);
    public const uint RRF_RT_REG_DWORD = 0x00000010, RRF_RT_REG_BINARY = 0x00000008;
    public const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
    public const nint HWND_MESSAGE = -3;
    public const nint HWND_TOPMOST = -1, HWND_NOTOPMOST = -2;
    public const int IDC_ARROW = 32512, IDC_IBEAM = 32513, IDC_WAIT = 32514, IDC_CROSS = 32515, IDC_SIZEWE = 32644, IDC_SIZENS = 32645, IDC_SIZENWSE = 32642, IDC_SIZENESW = 32643,
        IDC_SIZEALL = 32646, IDC_NO = 32648, IDC_HAND = 32649, IDC_APPSTARTING = 32650, IDC_HELP = 32651;
    public const int HTCLIENT = 1, HTCAPTION = 2, HTMAXBUTTON = 9, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
    public const uint WM_NCCALCSIZE = 0x0083, WM_NCHITTEST = 0x0084, WM_NCMOUSEMOVE = 0x00A0, WM_NCLBUTTONDOWN = 0x00A1, WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3, WM_NCRBUTTONDOWN = 0x00A4, WM_NCRBUTTONUP = 0x00A5, WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7, WM_NCMBUTTONUP = 0x00A8, WM_NCMBUTTONDBLCLK = 0x00A9, WM_NCMOUSELEAVE = 0x02A2;
    public const uint TME_NONCLIENT = 0x10, SWP_FRAMECHANGED = 0x0020;
    public const uint WM_CONTEXTMENU = 0x007B, WM_SYSCOMMAND = 0x0112;
    [LibraryImport("user32.dll")] public static partial nint GetSystemMenu(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool revert);
    [LibraryImport("user32.dll")] public static partial uint EnableMenuItem(nint menu, uint command, uint flags);
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)] public static partial bool AppendMenuW(nint menu, uint flags, nuint command, string text);
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] public static partial bool RemoveMenu(nint menu, uint command, uint flags);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);
    public const int SM_CXSIZEFRAME = 32, SM_CYSIZEFRAME = 33, SM_CXPADDEDBORDER = 92;
    public const int SW_MINIMIZE = 6, SW_MAXIMIZE = 3;
    public const uint SIZE_RESTORED = 0, SIZE_MAXIMIZED = 2;
    [StructLayout(LayoutKind.Sequential)] public struct NCCALCSIZE_PARAMS { public RECT rgrc0, rgrc1, rgrc2; public nint lppos; }
    [LibraryImport("user32.dll")] public static partial int GetSystemMetricsForDpi(int index, uint dpi);
    [LibraryImport("user32.dll")] public static partial uint GetDpiForSystem();
    public const uint TME_LEAVE = 0x2;
    public const uint MK_SHIFT = 0x4, MK_CONTROL = 0x8;
    public const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_LWIN = 0x5B, VK_RWIN = 0x5C;
    public const uint DIB_RGB_COLORS = 0, BI_RGB = 0;
    public const uint GR_GDIOBJECTS = 0, GR_USEROBJECTS = 1;
    public const uint WM_IME_STARTCOMPOSITION = 0x010D, WM_IME_ENDCOMPOSITION = 0x010E, WM_IME_COMPOSITION = 0x010F,
        WM_IME_SETCONTEXT = 0x0281, WM_IME_CHAR = 0x0286;
    public const uint GCS_COMPSTR = 0x0008, GCS_CURSORPOS = 0x0080, GCS_RESULTSTR = 0x0800;
    public const uint CFS_POINT = 0x0002, CFS_EXCLUDE = 0x0080;
    public const uint IACE_DEFAULT = 0x0010;
    public const uint ISC_SHOWUICOMPOSITIONWINDOW = 0x80000000;
    public const uint CF_UNICODETEXT = 13, GMEM_MOVEABLE = 0x0002;

    [StructLayout(LayoutKind.Sequential)] public struct CANDIDATEFORM { public uint dwIndex, dwStyle; public POINT ptCurrentPos; public RECT rcArea; }

    [StructLayout(LayoutKind.Sequential)] public struct COMPOSITIONFORM { public uint dwStyle; public POINT ptCurrentPos; public RECT rcArea; }

    [LibraryImport("imm32.dll")] public static partial nint ImmGetContext(nint hwnd);
    [LibraryImport("imm32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ImmReleaseContext(nint hwnd, nint himc);
    [LibraryImport("imm32.dll")] public static partial int ImmGetCompositionStringW(nint himc, uint index, void* buffer, uint bufferBytes);
    [LibraryImport("imm32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ImmSetCandidateWindow(nint himc, CANDIDATEFORM* form);
    [LibraryImport("imm32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ImmSetCompositionWindow(nint himc, COMPOSITIONFORM* form);
    [LibraryImport("imm32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ImmAssociateContextEx(nint hwnd, nint himc, uint flags);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool CreateCaret(nint hwnd, nint bitmap, int width, int height);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SetCaretPos(int x, int y);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool DestroyCaret();
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool OpenClipboard(nint hwnd);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool CloseClipboard();
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool EmptyClipboard();
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nint SetClipboardData(uint format, nint memory);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nint GetClipboardData(uint format);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsClipboardFormatAvailable(uint format);
    [LibraryImport("kernel32.dll", SetLastError = true)] public static partial nint GlobalAlloc(uint flags, nuint bytes);
    [LibraryImport("kernel32.dll", SetLastError = true)] public static partial void* GlobalLock(nint memory);
    [LibraryImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GlobalUnlock(nint memory);
    [LibraryImport("kernel32.dll", SetLastError = true)] public static partial nint GlobalFree(nint memory);
    [LibraryImport("kernel32.dll", SetLastError = true)] public static partial nuint GlobalSize(nint memory);

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public delegate* unmanaged<nint, uint, nint, nint, nint> lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public nint hInstance, hIcon, hCursor, hbrBackground;
        public char* lpszMenuName, lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)] public struct MSG { public nint hwnd; public uint message; public nint wParam, lParam; public uint time; public POINT pt; }

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public nint hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore, fIncUpdate;
        public fixed byte rgbReserved[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)] public struct CREATESTRUCTW { public nint lpCreateParams; }

    [StructLayout(LayoutKind.Sequential)] public struct TRACKMOUSEEVENT { public uint cbSize, dwFlags; public nint hwndTrack; public uint dwHoverTime; }

    [LibraryImport("user32.dll", SetLastError = true)] public static partial ushort RegisterClassExW(WNDCLASSEXW* wc);
    [LibraryImport("user32.dll")] public static partial nint LoadImageW(nint instance, nint name, uint type, int width, int height, uint flags);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool UnregisterClassW(char* className, nint hInstance);
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowExW(uint exStyle, char* className, string? windowName, uint style, int x, int y, int w, int h, nint parent, nint menu, nint hInstance, nint param);
    [LibraryImport("user32.dll")] public static partial nint DefWindowProcW(nint hwnd, uint msg, nint w, nint l);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool DestroyWindow(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsWindow(nint hwnd);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial int GetMessageW(MSG* msg, nint hwnd, uint min, uint max);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool TranslateMessage(MSG* msg);
    [LibraryImport("user32.dll")] public static partial nint DispatchMessageW(MSG* msg);
    [LibraryImport("user32.dll")] public static partial void PostQuitMessage(int code);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool PostMessageW(nint hwnd, uint msg, nint w, nint l);
    public const uint SMTO_NORMAL = 0x0000, SMTO_ERRORONEXIT = 0x0020;
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nint SendMessageTimeoutW(nint hwnd, uint msg, nint w, nint l, uint flags, uint timeout, out nint result);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nint SetWindowLongPtrW(nint hwnd, int idx, nint value);
    [LibraryImport("user32.dll", SetLastError = true)] public static partial nint GetWindowLongPtrW(nint hwnd, int idx);
    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)][return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowTextW(nint hwnd, string? text);
    [LibraryImport("user32.dll")] public static partial nint BeginPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool EndPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")] public static partial nint GetDC(nint hwnd);
    public const uint DCX_CACHE = 0x0002, DCX_CLIPCHILDREN = 0x0008, DCX_CLIPSIBLINGS = 0x0010;
    [LibraryImport("user32.dll")] public static partial nint GetDCEx(nint hwnd, nint clip, uint flags);
    [LibraryImport("user32.dll")] public static partial int ReleaseDC(nint hwnd, nint hdc);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GetUpdateRect(nint hwnd, RECT* rect, [MarshalAs(UnmanagedType.Bool)] bool erase);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool InvalidateRect(nint hwnd, RECT* rect, [MarshalAs(UnmanagedType.Bool)] bool erase);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ValidateRect(nint hwnd, RECT* rect);

    public const uint RDW_INTERNALPAINT = 0x0002, RDW_NOINTERNALPAINT = 0x0010;

    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool RedrawWindow(nint hwnd, RECT* update, nint region, uint flags);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool UpdateWindow(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GetClientRect(nint hwnd, out RECT rc);
    [LibraryImport("user32.dll")] public static partial uint GetDpiForWindow(nint hwnd);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SetProcessDpiAwarenessContext(nint ctx);
    [LibraryImport("user32.dll")] public static partial nint GetThreadDpiAwarenessContext();
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool AreDpiAwarenessContextsEqual(nint a, nint b);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustWindowRectExForDpi(ref RECT rc, uint style, [MarshalAs(UnmanagedType.Bool)] bool menu, uint exStyle, uint dpi);
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ShowWindow(nint hwnd, int cmd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsIconic(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsWindowVisible(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsZoomed(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool EnableWindow(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool IsWindowEnabled(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GetWindowRect(nint hwnd, out RECT rect);
    [LibraryImport("user32.dll")] public static partial nint MonitorFromWindow(nint hwnd, uint flags);
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool GetMonitorInfo(nint monitor, ref MONITORINFO info);
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool GetMonitorInfo(nint monitor, ref MONITORINFOEX info);
    [LibraryImport("gdi32.dll")] internal static partial nint CreateDCW(char* driver, char* device, nint port, nint mode);
    [LibraryImport("gdi32.dll")] internal static partial int GetDeviceCaps(nint dc, int index);
    [LibraryImport("gdi32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool DeleteDC(nint dc);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool SetForegroundWindow(nint hwnd);
    [LibraryImport("user32.dll")] public static partial nint GetWindow(nint hwnd, uint command);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)][return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetPropW(nint hwnd, string name, nint value);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint RemovePropW(nint hwnd, string name);
    [LibraryImport("user32.dll")] public static partial nint SetCapture(nint hwnd);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ReleaseCapture();
    [LibraryImport("user32.dll")] public static partial nint GetCapture();
    [LibraryImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static partial bool TrackMouseEvent(TRACKMOUSEEVENT* tme);
    [LibraryImport("user32.dll")] public static partial short GetKeyState(int vk);
    [LibraryImport("user32.dll")] public static partial nint LoadCursorW(nint hInst, nint name);
    [LibraryImport("user32.dll")] public static partial nint SetCursor(nint cursor);
    [LibraryImport("user32.dll")] public static partial nint SetFocus(nint hwnd);
    [LibraryImport("user32.dll")] public static partial nint GetFocus();
    [LibraryImport("user32.dll")] public static partial nint GetForegroundWindow();
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ScreenToClient(nint hwnd, POINT* point);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ScreenToClient(nint hwnd, ref POINT point);
    [LibraryImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static partial bool ClientToScreen(nint hwnd, ref POINT point);
    [LibraryImport("user32.dll")] public static partial uint GetGuiResources(nint process, uint flags);
    [LibraryImport("gdi32.dll")]
    public static partial int SetDIBitsToDevice(nint hdc, int x, int y, uint w, uint h, int xs, int ys, uint start, uint lines, void* bits, BITMAPINFOHEADER* bmi, uint usage);
    [LibraryImport("kernel32.dll")] public static partial nint GetModuleHandleW(char* name);
    [LibraryImport("kernel32.dll")] public static partial nint GetCurrentProcess();

    public static int LowSigned(nint value) => unchecked((short)(long)value);
    public static int HighSigned(nint value) => unchecked((short)((long)value >> 16));
}
