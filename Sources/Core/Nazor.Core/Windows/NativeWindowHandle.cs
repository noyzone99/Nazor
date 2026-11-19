namespace Nazor;

/// <summary>A borrowed native window identifier. Nazor retains ownership and the identifier expires when the window closes.</summary>
/// <param name="Handle">The native identifier; never destroy or release it.</param>
/// <param name="Descriptor">The backend identifier: HWND on Windows, X11 for an XID, or wl_surface for a Wayland surface pointer. Other backends may define additional descriptors.</param>
public readonly record struct NativeWindowHandle(nint Handle, string Descriptor);
