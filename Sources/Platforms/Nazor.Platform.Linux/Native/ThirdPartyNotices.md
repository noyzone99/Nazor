# Wayland protocol definitions

The XML files in `Protocols/` are copied from wayland-protocols 1.49. Each file retains its upstream copyright and permissive license. Generated C bindings retain the same notices.

Upstream: https://gitlab.freedesktop.org/wayland/wayland-protocols

The bridge dynamically links the system Wayland client, Wayland cursor and xkbcommon libraries. It does not link GTK or Qt. The runtime distribution must provide those system libraries and their associated notices.

The X11 bridge dynamically links the system Xlib, Xcursor, Xrender, Xext and Xrandr libraries. Their X.Org permissive license notices are supplied by the runtime distribution with those libraries.
