#ifndef NAZOR_WAYLAND_H
#define NAZOR_WAYLAND_H
#include <stddef.h>
#include <stdint.h>
#define NZ_API __attribute__((visibility("default")))
typedef struct nz_connection nz_connection;
typedef struct nz_window nz_window;
enum nz_window_state {
    NZ_STATE_NORMAL, NZ_STATE_MAXIMIZED, NZ_STATE_MINIMIZED, NZ_STATE_FULLSCREEN
};
enum nz_event_type {
    NZ_CONFIGURE = 1, NZ_CLOSE, NZ_POINTER_ENTER, NZ_POINTER_LEAVE, NZ_POINTER_MOTION,
    NZ_POINTER_BUTTON, NZ_POINTER_AXIS, NZ_KEY, NZ_FOCUS, NZ_TEXT, NZ_PREEDIT,
    NZ_TEXT_DELETE, NZ_FRAME, NZ_SCALE, NZ_CAPABILITIES, NZ_REPEAT, NZ_CLIPBOARD,
    NZ_ALWAYS_ON_TOP = 21, NZ_WINDOW_ACTIVATION, NZ_POPUP_CONFIGURE, NZ_POPUP_DONE, NZ_POPUP_OWNER_MOVED
};
enum { NZ_POINTER_FRAME_OUTSET = 1u << 16 };
typedef struct nz_event {
    uint32_t type, flags;
    nz_window *window;
    double x, y;
    int32_t a, b, c, d;
    const char *text;
} nz_event;
typedef struct nz_buffer_view {
    void *pixels;
    int32_t width, height, stride;
} nz_buffer_view;
typedef struct nz_damage_rect { int32_t x, y, width, height; } nz_damage_rect;
typedef struct nz_popup_placement {
    int32_t x, y, width, height, anchor_x, anchor_y, anchor_width, anchor_height, shadow, side, alignment, offset;
    uint32_t flags, serial;
} nz_popup_placement;
NZ_API nz_window *nz_popup_create(nz_window *parent, const nz_popup_placement *placement);
NZ_API int nz_popup_place(nz_window *popup, const nz_popup_placement *placement);
NZ_API void nz_popup_input_region(nz_window *popup, int x, int y, int width, int height);
NZ_API void nz_window_pointer_capture(nz_window *window, int capture);
NZ_API int nz_popup_work_area(nz_window *owner, int x, int y, int width, int height, nz_damage_rect *area);
NZ_API int nz_decoration_support(nz_connection *connection);
NZ_API int nz_window_decoration_mode(nz_window *window);
NZ_API uint32_t nz_abi_version(void);
NZ_API size_t nz_event_size(void);
NZ_API nz_connection *nz_connect(char *error, size_t error_size);
NZ_API void nz_disconnect(nz_connection *connection);
NZ_API const char *nz_error(nz_connection *connection);
NZ_API int nz_dispatch(nz_connection *connection, int timeout_ms);
NZ_API void nz_wake(nz_connection *connection);
NZ_API int nz_next_event(nz_connection *connection, nz_event *event);
NZ_API nz_window *nz_window_create(nz_connection *connection, const char *title, const char *app_id,
    int width, int height, nz_window *parent, int custom_titlebar, int borderless);
/* Optional X11 exports. Wayland already creates ARGB surfaces. */
NZ_API int nz_argb_supported(nz_connection *connection);
NZ_API nz_window *nz_window_create_argb(nz_connection *connection, const char *title, const char *app_id,
    int width, int height, nz_window *parent, int custom_titlebar, int borderless);
NZ_API void nz_window_destroy(nz_window *window);
NZ_API void nz_window_show(nz_window *window, int visible);
NZ_API void nz_window_title(nz_window *window, const char *title);
NZ_API void nz_window_state(nz_window *window, int state);
NZ_API void nz_window_titlebar(nz_window *window, int custom_titlebar);
NZ_API void nz_window_operations(nz_window *window, int resize, int width, int height);
NZ_API void nz_window_x11_functions(nz_window *window, int minimize, int maximize);
NZ_API int nz_x11_above_supported(nz_connection *connection);
NZ_API void nz_window_x11_above(nz_window *window, int above);
NZ_API void nz_window_resize(nz_window *window, int width, int height);
NZ_API void nz_window_activate(nz_window *window);
NZ_API void nz_window_move(nz_window *window);
NZ_API int nz_window_menu(nz_window *window, uint32_t serial, int x, int y);
NZ_API void nz_window_interactive_resize(nz_window *window, uint32_t edges);
NZ_API void nz_window_wayland_resize(nz_window *window, uint32_t edges, uint32_t serial);
NZ_API void nz_window_outer_resize(nz_window *window, int margin);
NZ_API void nz_window_cursor(nz_window *window, const char *name);
NZ_API void nz_window_text_input(nz_window *window, int enabled, int x, int y, int width, int height);
NZ_API void nz_window_surrounding(nz_window *window, const char *text, int cursor, int anchor, int password);
NZ_API void nz_window_wayland_text_context(nz_window *window, int enabled, int field, int password,
    int x, int y, int width, int height, const char *text, int cursor, int anchor);
NZ_API uint32_t nz_window_capabilities(nz_window *window);
NZ_API int nz_window_acquire(nz_window *window, int width, int height, nz_buffer_view *view);
NZ_API void nz_window_present(nz_window *window, float scale, int frame);
NZ_API int nz_window_buffer_is_current(nz_window *window);
NZ_API void nz_window_discard_buffer(nz_window *window);
NZ_API void nz_window_present_damage(nz_window *window, float scale, int frame, const nz_damage_rect *rects, int count);
NZ_API void *nz_window_native_display(nz_window *window);
NZ_API void *nz_window_native_surface(nz_window *window);
NZ_API void *nz_window_render_surface(nz_window *window);
NZ_API void nz_window_gpu_prepare(nz_window *window);
NZ_API void nz_window_wayland_gpu_start(nz_window *window);
NZ_API int nz_window_wayland_gpu_prepare(nz_window *window, int width, int height);
NZ_API void nz_window_wayland_gpu_presented(nz_window *window);
NZ_API void nz_window_cancel_frame(nz_window *window);
NZ_API int nz_window_icon(nz_window *window, const void *pixels, int width, int height);
NZ_API int nz_window_wayland_shadow(nz_window *window, const void *pixels, int width, int height, int margin, int scale);
NZ_API int nz_window_frame_shadow(nz_window *window, const void *pixels, int width, int height, int margin, float scale);
NZ_API int nz_window_x11_corners(nz_window *window, const float *radii);
NZ_API int nz_window_blur(nz_window *window, int enabled);
NZ_API int nz_clipboard_set(nz_connection *connection, const char *text);
NZ_API int nz_clipboard_request(nz_connection *connection);
NZ_API void nz_clipboard_cancel(nz_connection *connection);
NZ_API int nz_window_foreign_parent(nz_window *window, const char *handle);
#endif
