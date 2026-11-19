#define _GNU_SOURCE
#include "nazor-wayland.h"
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <locale.h>
#include <math.h>
#include <poll.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/eventfd.h>
#include <sys/mman.h>
#include <time.h>
#include <unistd.h>
#include <wayland-client.h>
#include <wayland-cursor.h>
#include <xkbcommon/xkbcommon.h>
#include <xkbcommon/xkbcommon-compose.h>
#include "xdg-shell-client-protocol.h"
#include "viewporter-client-protocol.h"
#include "fractional-scale-v1-client-protocol.h"
#include "xdg-activation-v1-client-protocol.h"
#include "xdg-decoration-unstable-v1-client-protocol.h"
#include "text-input-unstable-v3-client-protocol.h"
#include "xdg-toplevel-icon-v1-client-protocol.h"
#include "xdg-foreign-unstable-v2-client-protocol.h"
#include "ext-background-effect-v1-client-protocol.h"

struct nz_queued_event { nz_event value; struct nz_queued_event *next; };
struct nz_offer { struct wl_data_offer *offer; int utf8, plain, legacy_utf8; struct nz_offer *next; };
struct nz_transfer { int fd; char *text; size_t length, offset; struct nz_transfer *next; };
struct nz_output { struct wl_output *output; nz_connection *connection; uint32_t name; int scale; struct nz_output *next; };
struct nz_entered_output { struct nz_output *output; struct nz_entered_output *next; };
struct nz_buffer {
    struct wl_buffer *buffer;
    void *pixels;
    size_t bytes;
    int width, height, busy;
    struct nz_buffer *next;
};
struct nz_window {
    nz_connection *connection;
    struct wl_surface *surface;
    struct wl_surface *shadow_surface;
    struct wl_subsurface *shadow;
    struct nz_buffer *shadow_buffer;
    struct wl_surface *resize_surface;
    struct wl_subsurface *resize_subsurface;
    struct wp_viewport *resize_viewport;
    struct nz_buffer *resize_buffer;
    int resize_margin, resize_width, resize_height;
    struct xdg_surface *shell;
    struct xdg_toplevel *toplevel;
    struct xdg_popup *popup;
    nz_popup_placement popup_placement;
    int popup_x, popup_y, popup_width, popup_height, popup_dismissed;
    struct zxdg_toplevel_decoration_v1 *decoration;
    struct wp_viewport *viewport;
    struct wp_fractional_scale_v1 *fractional;
    struct wl_callback *frame;
    struct xdg_activation_token_v1 *activation_token;
    struct xdg_toplevel_icon_v1 *icon;
    struct zxdg_exported_v2 *exported;
    struct zxdg_imported_v2 *imported;
    struct ext_background_effect_surface_v1 *effect;
    struct nz_buffer *buffers, *acquired, *icon_buffer, *last_presented;
    int width, height, pending_width, pending_height, state, configured, visible, mapped, initial_commit;
    int scale, preferred_scale, decoration_mode, decoration_configured, custom_titlebar, borderless;
    int active, pending_active, tiled;
    uint32_t capabilities;
    struct nz_window *parent;
    struct nz_entered_output *outputs;
    struct nz_window *next;
};
struct nz_connection {
    struct wl_display *display;
    struct wl_registry *registry;
    struct wl_compositor *compositor;
    struct wl_subcompositor *subcompositor;
    struct wl_shm *shm;
    struct xdg_wm_base *shell;
    struct wl_seat *seat;
    uint32_t seat_name;
    struct wl_pointer *pointer;
    struct wl_keyboard *keyboard;
    struct wl_surface *cursor_surface;
    struct wl_cursor_theme *cursor_theme;
    struct wp_viewporter *viewporter;
    struct wp_fractional_scale_manager_v1 *fractional;
    struct xdg_activation_v1 *activation;
    struct zxdg_decoration_manager_v1 *decorations;
    struct zwp_text_input_manager_v3 *text_manager;
    struct zwp_text_input_v3 *text_input;
    struct xdg_toplevel_icon_manager_v1 *icons;
    struct zxdg_exporter_v2 *exporter;
    struct zxdg_importer_v2 *importer;
    struct ext_background_effect_manager_v1 *effects;
    uint32_t effect_capabilities;
    struct wl_data_device_manager *data_manager;
    struct wl_data_device *data_device;
    struct wl_data_source *data_source;
    struct nz_offer *offers, *selection;
    struct nz_transfer *transfers;
    struct nz_output *outputs;
    struct xkb_context *xkb;
    struct xkb_keymap *keymap;
    struct xkb_state *key_state;
    struct xkb_compose_table *compose_table;
    struct xkb_compose_state *compose;
    nz_window *windows, *pointer_window, *keyboard_window, *text_window;
    int pointer_on_resize;
    struct nz_queued_event *head, *tail, *current;
    uint32_t serial, pointer_serial, modifiers;
    double pointer_x, pointer_y;
    int wake_fd, failed, text_enabled;
    int clipboard_fd;
    char *clipboard, *received, *preedit, *commit;
    size_t received_length;
    int preedit_cursor, delete_before, delete_after;
    char error[512];
};

static void fail(nz_connection *c, const char *message) {
    if (!c->failed) snprintf(c->error, sizeof(c->error), "%s", message);
    c->failed = 1;
}
static void emit(nz_connection *c, nz_window *w, uint32_t type, uint32_t flags,
    double x, double y, int a, int b, int d, const char *text) {
    struct nz_queued_event *e = calloc(1, sizeof(*e));
    if (!e) { fail(c, "Cannot allocate a Wayland event."); return; }
    e->value = (nz_event){ .type=type, .flags=flags, .window=w, .x=x, .y=y, .a=a, .b=b, .c=d };
    if (text && !(e->value.text = strdup(text))) { free(e); fail(c, "Cannot allocate Wayland event text."); return; }
    if (c->tail) c->tail->next = e; else c->head = e;
    c->tail = e;
}
static nz_window *find_window(nz_connection *c, struct wl_surface *surface) {
    for (nz_window *w=c->windows; w; w=w->next) if (w->surface == surface) return w;
    return NULL;
}
static void free_event(struct nz_queued_event *e) { if (e) { free((void *)e->value.text); free(e); } }
uint32_t nz_abi_version(void) { return 5; }
int nz_decoration_support(nz_connection *c) { return c && c->decorations ? 1:0; }
int nz_window_decoration_mode(nz_window *w) { return !w ? 0:(!w->decoration ? 1:(w->decoration_configured ? w->decoration_mode:0)); }
size_t nz_event_size(void) { return sizeof(nz_event); }
const char *nz_error(nz_connection *c) { return c ? c->error : "No Wayland connection."; }
int nz_next_event(nz_connection *c, nz_event *out) {
    if (!c || !out) return 0;
    free_event(c->current); c->current = NULL;
    if (!c->head) return 0;
    c->current = c->head; c->head = c->head->next;
    if (!c->head) c->tail = NULL;
    *out = c->current->value;
    return 1;
}
void nz_wake(nz_connection *c) {
    if (c) {
        uint64_t value=1;
        ssize_t result;
        do { result=write(c->wake_fd, &value, sizeof(value)); } while (result<0 && errno==EINTR);
    }
}

static void buffer_free(struct nz_buffer *b) {
    if (!b) return;
    if (b->buffer) wl_buffer_destroy(b->buffer);
    if (b->pixels && b->pixels != MAP_FAILED) munmap(b->pixels, b->bytes);
    free(b);
}
static void buffer_release(void *data, struct wl_buffer *buffer) {
    struct nz_buffer *b=data;
    b->busy=0;
}
static const struct wl_buffer_listener buffer_listener = { .release=buffer_release };
static struct nz_buffer *buffer_create(nz_connection *c, int width, int height) {
    if (width <= 0 || height <= 0 || width > INT_MAX/4 || height > INT_MAX/(width*4)) return NULL;
    struct nz_buffer *b=calloc(1, sizeof(*b));
    if (!b) return NULL;
    b->width=width; b->height=height; b->bytes=(size_t)width*height*4;
    int fd=memfd_create("nazor-wayland", MFD_CLOEXEC | MFD_ALLOW_SEALING);
    if (fd < 0) { free(b); return NULL; }
    if (ftruncate(fd, (off_t)b->bytes) < 0) { close(fd); free(b); return NULL; }
    (void)fcntl(fd, F_ADD_SEALS, F_SEAL_SHRINK);
    b->pixels=mmap(NULL, b->bytes, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 0);
    if (b->pixels != MAP_FAILED) {
        struct wl_shm_pool *pool=wl_shm_create_pool(c->shm, fd, (int)b->bytes);
        if (pool) { b->buffer=wl_shm_pool_create_buffer(pool, 0, width, height, width*4, WL_SHM_FORMAT_ARGB8888); wl_shm_pool_destroy(pool); }
    }
    close(fd);
    if (!b->buffer) { buffer_free(b); return NULL; }
    wl_buffer_add_listener(b->buffer, &buffer_listener, b);
    return b;
}
int nz_window_acquire(nz_window *w, int width, int height, nz_buffer_view *view) {
    if (!w || !view || !w->configured || !w->visible || w->acquired) return 0;
    struct nz_buffer **link=&w->buffers;
    int count=0;
    while (*link) {
        struct nz_buffer *b=*link;
        if (!b->busy && (b->width!=width || b->height!=height)) { if (w->last_presented==b) w->last_presented=NULL; *link=b->next; buffer_free(b); continue; }
        if (!b->busy && b->width==width && b->height==height && !w->acquired) w->acquired=b;
        count++; link=&b->next;
    }
    if (!w->acquired && count < 3) {
        struct nz_buffer *b=buffer_create(w->connection, width, height);
        if (!b) { fail(w->connection, "Cannot allocate a Wayland shared-memory buffer."); return -1; }
        b->next=w->buffers; w->buffers=b; w->acquired=b;
    }
    if (!w->acquired) return 0;
    *view=(nz_buffer_view){w->acquired->pixels,width,height,width*4};
    return 1;
}
static void frame_done(void *data, struct wl_callback *callback, uint32_t time) {
    nz_window *w=data;
    wl_callback_destroy(callback); w->frame=NULL;
    emit(w->connection,w,NZ_FRAME,0,0,0,0,0,0,NULL);
}
static const struct wl_callback_listener frame_listener = { .done=frame_done };
void *nz_window_native_display(nz_window *w) { return w ? w->connection->display : NULL; }
void *nz_window_native_surface(nz_window *w) { return w ? w->surface : NULL; }
void nz_window_cancel_frame(nz_window *w) {
    if (w && w->frame) { wl_callback_destroy(w->frame); w->frame=NULL; }
}
void nz_window_gpu_prepare(nz_window *w) {
    if (!w) return;
    if (w->viewport) { wl_surface_set_buffer_scale(w->surface,1); wp_viewport_set_destination(w->viewport,w->width,w->height); }
    else wl_surface_set_buffer_scale(w->surface,w->scale);
    if (!w->frame) { w->frame=wl_surface_frame(w->surface); wl_callback_add_listener(w->frame,&frame_listener,w); }
    int first_map=!w->mapped;
    w->mapped=1;
    if (first_map) for (nz_window *child=w->connection->windows; child; child=child->next)
        if (child->parent==w && child->toplevel && w->toplevel) xdg_toplevel_set_parent(child->toplevel,w->toplevel);
}
void nz_window_present(nz_window *w, float scale, int frame) {
    nz_window_present_damage(w,scale,frame,NULL,-1);
}
int nz_window_buffer_is_current(nz_window *w) { return w && w->acquired && w->acquired==w->last_presented; }
void nz_window_discard_buffer(nz_window *w) { if (w) w->acquired=NULL; }
void nz_window_present_damage(nz_window *w, float scale, int frame, const nz_damage_rect *rects, int count) {
    if (!w || !w->acquired) return;
    struct nz_buffer *b=w->acquired; w->acquired=NULL;
    if (w->viewport) { wl_surface_set_buffer_scale(w->surface,1); wp_viewport_set_destination(w->viewport,w->width,w->height); }
    else wl_surface_set_buffer_scale(w->surface,w->scale);
    wl_surface_attach(w->surface,b->buffer,0,0);
    if (!w->mapped || count < 0) wl_surface_damage_buffer(w->surface,0,0,b->width,b->height);
    else for (int i=0;rects && i<count;i++)
        if (rects[i].width>0 && rects[i].height>0) wl_surface_damage_buffer(w->surface,rects[i].x,rects[i].y,rects[i].width,rects[i].height);
    if (frame && !w->frame) { w->frame=wl_surface_frame(w->surface); wl_callback_add_listener(w->frame,&frame_listener,w); }
    b->busy=1; w->last_presented=b;
    int first_map=!w->mapped;
    w->mapped=1;
    wl_surface_commit(w->surface);
    if (first_map) for (nz_window *child=w->connection->windows; child; child=child->next)
        if (child->parent==w) xdg_toplevel_set_parent(child->toplevel,w->toplevel);
}
static void output_scale_for_window(nz_window *w) {
    if (wl_surface_get_version(w->surface)>=6) return;
    int scale=1;
    for (struct nz_entered_output *entry=w->outputs;entry;entry=entry->next) if (entry->output->scale>scale) scale=entry->output->scale;
    if (w->scale!=scale) { w->scale=scale; if (!w->fractional) emit(w->connection,w,NZ_SCALE,0,scale,0,0,0,0,NULL); }
}
static void surface_enter(void *data, struct wl_surface *surface, struct wl_output *output) {
    nz_window *w=data;
    for (struct nz_entered_output *entry=w->outputs;entry;entry=entry->next) if (entry->output->output==output) return;
    for (struct nz_output *o=w->connection->outputs;o;o=o->next) if (o->output==output) {
        struct nz_entered_output *entry=calloc(1,sizeof(*entry));
        if (!entry) { fail(w->connection,"Cannot track window output scale."); return; }
        entry->output=o; entry->next=w->outputs; w->outputs=entry; output_scale_for_window(w); return;
    }
}
static void surface_leave(void *data, struct wl_surface *surface, struct wl_output *output) {
    nz_window *w=data; struct nz_entered_output **link=&w->outputs;
    while (*link) { struct nz_entered_output *entry=*link; if (entry->output->output==output) { *link=entry->next; free(entry); } else link=&entry->next; }
    output_scale_for_window(w);
}
#ifdef WL_SURFACE_PREFERRED_BUFFER_SCALE_SINCE_VERSION
static void surface_scale(void *data, struct wl_surface *surface, int32_t scale) {
    nz_window *w=data;
    if (scale>0 && w->scale!=scale) { w->scale=scale; if (!w->fractional) emit(w->connection,w,NZ_SCALE,0,scale,0,0,0,0,NULL); }
}
static void surface_transform(void *data, struct wl_surface *surface, uint32_t transform) { }
#endif
static const struct wl_surface_listener surface_listener = {
    .enter=surface_enter,.leave=surface_leave
#ifdef WL_SURFACE_PREFERRED_BUFFER_SCALE_SINCE_VERSION
    ,.preferred_buffer_scale=surface_scale,.preferred_buffer_transform=surface_transform
#endif
};
static void preferred_scale(void *data, struct wp_fractional_scale_v1 *fractional, uint32_t scale) {
    nz_window *w=data;
    if (scale && w->preferred_scale!=(int)scale) { w->preferred_scale=(int)scale; emit(w->connection,w,NZ_SCALE,0,scale/120.0,0,0,0,0,NULL); }
}
static const struct wp_fractional_scale_v1_listener scale_listener = { .preferred_scale=preferred_scale };
static void shell_configure(void *data, struct xdg_surface *shell, uint32_t serial) {
    nz_window *w=data;
    xdg_surface_ack_configure(shell,serial);
    if (w->popup) {
        int shadow=w->popup_placement.shadow;
        w->width=w->popup_width+2*shadow; w->height=w->popup_height+2*shadow;
        xdg_surface_set_window_geometry(shell,shadow,shadow,w->popup_width,w->popup_height);
        struct wl_region *input=wl_compositor_create_region(w->connection->compositor);
        wl_region_add(input,shadow,shadow,w->popup_width,w->popup_height);
        wl_surface_set_input_region(w->surface,input); wl_region_destroy(input);
        w->configured=1;
        emit(w->connection,w,NZ_POPUP_CONFIGURE,0,w->popup_x,w->popup_y,w->popup_width,w->popup_height,0,NULL);
        return;
    }
    if (w->pending_width>0) w->width=w->pending_width;
    if (w->pending_height>0) w->height=w->pending_height;
    xdg_surface_set_window_geometry(shell,0,0,w->width,w->height);
    w->configured=1;
    emit(w->connection,w,NZ_CONFIGURE,w->tiled,0,0,w->width,w->height,w->state,NULL);
    if (w->active != w->pending_active) {
        w->active=w->pending_active;
        emit(w->connection,w,NZ_WINDOW_ACTIVATION,0,0,0,w->active,0,0,NULL);
    }
}
static const struct xdg_surface_listener shell_listener = { .configure=shell_configure };
static void toplevel_configure(void *data, struct xdg_toplevel *toplevel, int32_t width, int32_t height, struct wl_array *states) {
    nz_window *w=data; w->pending_width=width; w->pending_height=height; w->state=0; w->pending_active=0;
    w->tiled=0;
    uint32_t *state;
    wl_array_for_each(state,states) {
        if (*state==XDG_TOPLEVEL_STATE_MAXIMIZED) w->state=1;
        if (*state==XDG_TOPLEVEL_STATE_FULLSCREEN) w->state=3;
        if (*state==XDG_TOPLEVEL_STATE_ACTIVATED) w->pending_active=1;
        if (*state>=XDG_TOPLEVEL_STATE_TILED_LEFT && *state<=XDG_TOPLEVEL_STATE_TILED_BOTTOM) w->tiled=1;
    }
}
static void toplevel_close(void *data, struct xdg_toplevel *toplevel) { nz_window *w=data; emit(w->connection,w,NZ_CLOSE,0,0,0,0,0,0,NULL); }
static void toplevel_bounds(void *data, struct xdg_toplevel *toplevel, int32_t width, int32_t height) { }
static void toplevel_capabilities(void *data, struct xdg_toplevel *toplevel, struct wl_array *capabilities) {
    nz_window *w=data; w->capabilities=0; uint32_t *capability;
    wl_array_for_each(capability,capabilities) {
        if (*capability==XDG_TOPLEVEL_WM_CAPABILITIES_WINDOW_MENU) w->capabilities|=1u;
        if (*capability==XDG_TOPLEVEL_WM_CAPABILITIES_MAXIMIZE) w->capabilities|=2u;
        if (*capability==XDG_TOPLEVEL_WM_CAPABILITIES_FULLSCREEN) w->capabilities|=4u;
        if (*capability==XDG_TOPLEVEL_WM_CAPABILITIES_MINIMIZE) w->capabilities|=8u;
    }
    emit(w->connection,w,NZ_CAPABILITIES,0,0,0,0,0,0,NULL);
}
static const struct xdg_toplevel_listener toplevel_listener = {
    .configure=toplevel_configure,.close=toplevel_close,.configure_bounds=toplevel_bounds,.wm_capabilities=toplevel_capabilities
};
static void decoration_configure(void *data, struct zxdg_toplevel_decoration_v1 *decoration, uint32_t mode) {
    nz_window *w=data; w->decoration_mode=(int)mode; w->decoration_configured=1;
    emit(w->connection,w,NZ_CAPABILITIES,0,0,0,mode,0,0,NULL);
}
static const struct zxdg_toplevel_decoration_v1_listener decoration_listener = { .configure=decoration_configure };
static void ping(void *data, struct xdg_wm_base *shell, uint32_t serial) { xdg_wm_base_pong(shell,serial); }
static const struct xdg_wm_base_listener wm_listener = { .ping=ping };

static uint32_t modifiers(nz_connection *c) {
    if (!c->key_state) return 0;
    return (xkb_state_mod_name_is_active(c->key_state,XKB_MOD_NAME_SHIFT,XKB_STATE_MODS_EFFECTIVE)>0 ? 1u:0u) |
        (xkb_state_mod_name_is_active(c->key_state,XKB_MOD_NAME_CTRL,XKB_STATE_MODS_EFFECTIVE)>0 ? 2u:0u) |
        (xkb_state_mod_name_is_active(c->key_state,XKB_MOD_NAME_ALT,XKB_STATE_MODS_EFFECTIVE)>0 ? 4u:0u) |
        (xkb_state_mod_name_is_active(c->key_state,XKB_MOD_NAME_LOGO,XKB_STATE_MODS_EFFECTIVE)>0 ? 8u:0u);
}
static void keyboard_keymap(void *data, struct wl_keyboard *keyboard, uint32_t format, int32_t fd, uint32_t size) {
    nz_connection *c=data;
    if (format!=WL_KEYBOARD_KEYMAP_FORMAT_XKB_V1 || size==0 || size>16*1024*1024) { close(fd); return; }
    char *map=mmap(NULL,size,PROT_READ,MAP_PRIVATE,fd,0); close(fd);
    if (map==MAP_FAILED) { fail(c,"Cannot map the keyboard keymap."); return; }
    struct xkb_keymap *keymap=NULL;
    if (map[size-1]=='\0') keymap=xkb_keymap_new_from_string(c->xkb,map,XKB_KEYMAP_FORMAT_TEXT_V1,XKB_KEYMAP_COMPILE_NO_FLAGS);
    munmap(map,size);
    if (!keymap) { fail(c,"The compositor supplied an invalid keyboard keymap."); return; }
    struct xkb_state *state=xkb_state_new(keymap);
    if (!state) { xkb_keymap_unref(keymap); fail(c,"Cannot create keyboard state."); return; }
    xkb_state_unref(c->key_state); xkb_keymap_unref(c->keymap); c->keymap=keymap; c->key_state=state;
}
static void keyboard_enter(void *data, struct wl_keyboard *keyboard, uint32_t serial, struct wl_surface *surface, struct wl_array *keys) {
    nz_connection *c=data; c->serial=serial; c->keyboard_window=find_window(c,surface);
    if (c->keyboard_window) emit(c,c->keyboard_window,NZ_FOCUS,0,0,0,1,0,0,NULL);
}
static void keyboard_leave(void *data, struct wl_keyboard *keyboard, uint32_t serial, struct wl_surface *surface) {
    nz_connection *c=data; nz_window *w=find_window(c,surface);
    if (w) emit(c,w,NZ_FOCUS,0,0,0,0,0,0,NULL);
    c->keyboard_window=NULL; if (c->compose) xkb_compose_state_reset(c->compose);
}
static void keyboard_key(void *data, struct wl_keyboard *keyboard, uint32_t serial, uint32_t time, uint32_t key, uint32_t state) {
    nz_connection *c=data; c->serial=serial;
    if (!c->keyboard_window || !c->key_state) return;
    xkb_keysym_t sym=xkb_state_key_get_one_sym(c->key_state,key+8);
    char name[128];
    uint32_t unicode=xkb_keysym_to_utf32(sym);
    if (unicode>=32 && unicode!=127) xkb_keysym_to_utf8(sym,name,sizeof(name));
    else xkb_keysym_get_name(sym,name,sizeof(name));
    int repeats=xkb_keymap_key_repeats(c->keymap,key+8);
    emit(c,c->keyboard_window,NZ_KEY,c->modifiers,serial,0,state,key,repeats,name);
    if (state!=WL_KEYBOARD_KEY_STATE_PRESSED || (c->modifiers & (2u|8u))) return;
    char text[128]={0};
    if (c->compose) {
        xkb_compose_state_feed(c->compose,sym);
        enum xkb_compose_status status=xkb_compose_state_get_status(c->compose);
        if (status==XKB_COMPOSE_COMPOSING) return;
        if (status==XKB_COMPOSE_COMPOSED) { xkb_compose_state_get_utf8(c->compose,text,sizeof(text)); xkb_compose_state_reset(c->compose); }
        else if (status==XKB_COMPOSE_CANCELLED) { xkb_compose_state_reset(c->compose); return; }
        else xkb_state_key_get_utf8(c->key_state,key+8,text,sizeof(text));
    } else xkb_state_key_get_utf8(c->key_state,key+8,text,sizeof(text));
    if ((unsigned char)text[0]>=32 && text[0]!=127) emit(c,c->keyboard_window,NZ_TEXT,0,0,0,0,0,0,text);
}
static void keyboard_modifiers(void *data, struct wl_keyboard *keyboard, uint32_t serial, uint32_t depressed, uint32_t latched, uint32_t locked, uint32_t group) {
    nz_connection *c=data; c->serial=serial;
    if (c->key_state) { xkb_state_update_mask(c->key_state,depressed,latched,locked,0,0,group); c->modifiers=modifiers(c); }
}
static void keyboard_repeat(void *data, struct wl_keyboard *keyboard, int32_t rate, int32_t delay) {
    nz_connection *c=data; emit(c,NULL,NZ_REPEAT,0,0,0,rate,delay,0,NULL);
}
static const struct wl_keyboard_listener keyboard_listener = {
    .keymap=keyboard_keymap,.enter=keyboard_enter,.leave=keyboard_leave,.key=keyboard_key,.modifiers=keyboard_modifiers,.repeat_info=keyboard_repeat
};
static void pointer_enter(void *data, struct wl_pointer *pointer, uint32_t serial, struct wl_surface *surface, wl_fixed_t x, wl_fixed_t y) {
    nz_connection *c=data; c->pointer_serial=serial; c->pointer_window=find_window(c,surface);
    c->pointer_on_resize=0;
    if (!c->pointer_window && surface) {
        for (nz_window *w=c->windows; w; w=w->next) if (w->resize_surface==surface) {
            c->pointer_window=w; c->pointer_on_resize=1; break;
        }
    }
    int margin=c->pointer_on_resize ? c->pointer_window->resize_margin:0;
    c->pointer_x=wl_fixed_to_double(x)-margin; c->pointer_y=wl_fixed_to_double(y)-margin;
    if (c->pointer_window) emit(c,c->pointer_window,NZ_POINTER_ENTER,c->modifiers|(c->pointer_on_resize?NZ_POINTER_FRAME_OUTSET:0),c->pointer_x,c->pointer_y,0,0,0,NULL);
}
static void pointer_leave(void *data, struct wl_pointer *pointer, uint32_t serial, struct wl_surface *surface) {
    nz_connection *c=data;
    if (c->pointer_window) emit(c,c->pointer_window,NZ_POINTER_LEAVE,0,0,0,0,0,0,NULL);
    c->pointer_window=NULL; c->pointer_on_resize=0;
}
static void pointer_motion(void *data, struct wl_pointer *pointer, uint32_t time, wl_fixed_t x, wl_fixed_t y) {
    nz_connection *c=data;
    int margin=c->pointer_on_resize && c->pointer_window ? c->pointer_window->resize_margin:0;
    c->pointer_x=wl_fixed_to_double(x)-margin; c->pointer_y=wl_fixed_to_double(y)-margin;
    if (c->pointer_window) emit(c,c->pointer_window,NZ_POINTER_MOTION,c->modifiers|(c->pointer_on_resize?NZ_POINTER_FRAME_OUTSET:0),c->pointer_x,c->pointer_y,0,0,0,NULL);
}
static void pointer_button(void *data, struct wl_pointer *pointer, uint32_t serial, uint32_t time, uint32_t button, uint32_t state) {
    nz_connection *c=data; c->serial=serial;
    if (c->pointer_window) {
        struct nz_queued_event *previous=c->tail;
        emit(c,c->pointer_window,NZ_POINTER_BUTTON,c->modifiers|(c->pointer_on_resize?NZ_POINTER_FRAME_OUTSET:0),c->pointer_x,c->pointer_y,button,state,time,NULL);
        if (c->tail!=previous) c->tail->value.d=(int32_t)serial;
    }
}
static void pointer_axis(void *data, struct wl_pointer *pointer, uint32_t time, uint32_t axis, wl_fixed_t value) {
    nz_connection *c=data;
    if (c->pointer_window && !c->pointer_on_resize) { emit(c,c->pointer_window,NZ_POINTER_AXIS,c->modifiers,c->pointer_x,c->pointer_y,axis,0,0,NULL); if (c->tail) c->tail->value.d=value; }
}
static void pointer_frame(void *data, struct wl_pointer *pointer) { }
static void pointer_axis_source(void *data, struct wl_pointer *pointer, uint32_t source) { }
static void pointer_axis_stop(void *data, struct wl_pointer *pointer, uint32_t time, uint32_t axis) { }
static void pointer_axis_discrete(void *data, struct wl_pointer *pointer, uint32_t axis, int32_t discrete) { }
static const struct wl_pointer_listener pointer_listener = {
    .enter=pointer_enter,.leave=pointer_leave,.motion=pointer_motion,.button=pointer_button,.axis=pointer_axis,
    .frame=pointer_frame,.axis_source=pointer_axis_source,.axis_stop=pointer_axis_stop,.axis_discrete=pointer_axis_discrete
};

static void text_enter(void *data, struct zwp_text_input_v3 *input, struct wl_surface *surface) {
    nz_connection *c=data; c->text_window=find_window(c,surface); c->text_enabled=0;
    if (c->text_window) emit(c,c->text_window,19,0,0,0,0,0,0,NULL);
}
static void text_leave(void *data, struct zwp_text_input_v3 *input, struct wl_surface *surface) {
    nz_connection *c=data;
    if (c->text_window) emit(c,c->text_window,NZ_PREEDIT,0,0,0,0,0,0,"");
    c->text_window=NULL; c->text_enabled=0;
}
static void text_preedit(void *data, struct zwp_text_input_v3 *input, const char *text, int32_t begin, int32_t end) {
    nz_connection *c=data; free(c->preedit); c->preedit=strdup(text ? text:""); c->preedit_cursor=begin;
}
static void text_commit(void *data, struct zwp_text_input_v3 *input, const char *text) {
    nz_connection *c=data; free(c->commit); c->commit=strdup(text ? text:"");
}
static void text_delete(void *data, struct zwp_text_input_v3 *input, uint32_t before, uint32_t after) {
    nz_connection *c=data; c->delete_before=before>INT_MAX?INT_MAX:(int)before; c->delete_after=after>INT_MAX?INT_MAX:(int)after;
}
static void text_done(void *data, struct zwp_text_input_v3 *input, uint32_t serial) {
    nz_connection *c=data;
    if (c->text_window) {
        if (c->delete_before || c->delete_after) emit(c,c->text_window,NZ_TEXT_DELETE,0,0,0,c->delete_before,c->delete_after,0,NULL);
        if (c->commit) emit(c,c->text_window,NZ_TEXT,1,0,0,0,0,0,c->commit);
        if (c->preedit) emit(c,c->text_window,NZ_PREEDIT,0,0,0,c->preedit_cursor,0,0,c->preedit);
    }
    free(c->commit); c->commit=NULL; free(c->preedit); c->preedit=NULL;
    c->delete_before=0; c->delete_after=0;
}
static const struct zwp_text_input_v3_listener text_listener = {
    .enter=text_enter,.leave=text_leave,.preedit_string=text_preedit,.commit_string=text_commit,.delete_surrounding_text=text_delete,.done=text_done
};
static void setup_text(nz_connection *c) {
    if (c->seat && c->text_manager && !c->text_input) {
        c->text_input=zwp_text_input_manager_v3_get_text_input(c->text_manager,c->seat);
        zwp_text_input_v3_add_listener(c->text_input,&text_listener,c);
    }
}
void nz_window_text_input(nz_window *w, int enabled, int x, int y, int width, int height) {
    if (!w) return;
    nz_connection *c=w->connection;
    if (!c->text_input || c->text_window!=w) return;
    if (enabled) {
        if (!c->text_enabled) zwp_text_input_v3_enable(c->text_input);
        zwp_text_input_v3_set_content_type(c->text_input,ZWP_TEXT_INPUT_V3_CONTENT_HINT_NONE,ZWP_TEXT_INPUT_V3_CONTENT_PURPOSE_NORMAL);
        zwp_text_input_v3_set_cursor_rectangle(c->text_input,x,y,width>0?width:1,height>0?height:1);
    } else if (c->text_enabled) zwp_text_input_v3_disable(c->text_input);
    c->text_enabled=enabled;
    zwp_text_input_v3_commit(c->text_input);
}
void nz_window_surrounding(nz_window *w, const char *text, int cursor, int anchor, int password) {
    if (!w) return;
    nz_connection *c=w->connection;
    if (!c->text_input || c->text_window!=w || !c->text_enabled) return;
    if (password) zwp_text_input_v3_set_content_type(c->text_input,
        ZWP_TEXT_INPUT_V3_CONTENT_HINT_HIDDEN_TEXT|ZWP_TEXT_INPUT_V3_CONTENT_HINT_SENSITIVE_DATA,ZWP_TEXT_INPUT_V3_CONTENT_PURPOSE_PASSWORD);
    else {
        zwp_text_input_v3_set_content_type(c->text_input,ZWP_TEXT_INPUT_V3_CONTENT_HINT_NONE,ZWP_TEXT_INPUT_V3_CONTENT_PURPOSE_NORMAL);
        if (text && cursor>=0 && anchor>=0 && (size_t)cursor<=strlen(text) && (size_t)anchor<=strlen(text))
            zwp_text_input_v3_set_surrounding_text(c->text_input,text,cursor,anchor);
    }
    zwp_text_input_v3_commit(c->text_input);
}

static void source_target(void *data, struct wl_data_source *source, const char *mime) { }
static void source_send(void *data, struct wl_data_source *source, const char *mime, int32_t fd) {
    nz_connection *c=data;
    int count=0; for (struct nz_transfer *t=c->transfers;t;t=t->next) count++;
    if (!c->clipboard || count>=64 || fcntl(fd,F_SETFL,O_NONBLOCK)<0) { close(fd); return; }
    struct nz_transfer *t=calloc(1,sizeof(*t));
    if (!t) { close(fd); return; }
    t->text=strdup(c->clipboard);
    if (!t->text) { free(t); close(fd); return; }
    t->fd=fd; t->length=strlen(t->text); t->next=c->transfers; c->transfers=t;
}
static void source_cancelled(void *data, struct wl_data_source *source) {
    nz_connection *c=data;
    if (c->data_source==source) c->data_source=NULL;
    wl_data_source_destroy(source);
}
static const struct wl_data_source_listener source_listener = { .target=source_target,.send=source_send,.cancelled=source_cancelled };
static void offer_mime(void *data, struct wl_data_offer *offer, const char *mime) {
    struct nz_offer *o=data;
    if (strcmp(mime,"text/plain;charset=utf-8")==0) o->utf8=1;
    if (strcmp(mime,"UTF8_STRING")==0) o->legacy_utf8=1;
    if (strcmp(mime,"text/plain")==0) o->plain=1;
}
static const struct wl_data_offer_listener offer_listener = { .offer=offer_mime };
static void data_offer(void *data, struct wl_data_device *device, struct wl_data_offer *offer) {
    nz_connection *c=data; struct nz_offer *o=calloc(1,sizeof(*o));
    if (!o) { wl_data_offer_destroy(offer); fail(c,"Cannot allocate a clipboard offer."); return; }
    o->offer=offer; o->next=c->offers; c->offers=o; wl_data_offer_add_listener(offer,&offer_listener,o);
}
static void data_enter(void *data, struct wl_data_device *device, uint32_t serial, struct wl_surface *surface, wl_fixed_t x, wl_fixed_t y, struct wl_data_offer *offer) { }
static void data_leave(void *data, struct wl_data_device *device) { }
static void data_motion(void *data, struct wl_data_device *device, uint32_t time, wl_fixed_t x, wl_fixed_t y) { }
static void data_drop(void *data, struct wl_data_device *device) { }
static void data_selection(void *data, struct wl_data_device *device, struct wl_data_offer *offer) {
    nz_connection *c=data; c->selection=NULL;
    struct nz_offer **link=&c->offers;
    while (*link) {
        struct nz_offer *o=*link;
        if (o->offer==offer) { c->selection=o; link=&o->next; }
        else { *link=o->next; wl_data_offer_destroy(o->offer); free(o); }
    }
}
static const struct wl_data_device_listener data_listener = { .data_offer=data_offer,.enter=data_enter,.leave=data_leave,.motion=data_motion,.drop=data_drop,.selection=data_selection };
static void setup_data(nz_connection *c) {
    if (c->seat && c->data_manager && !c->data_device) {
        c->data_device=wl_data_device_manager_get_data_device(c->data_manager,c->seat);
        wl_data_device_add_listener(c->data_device,&data_listener,c);
    }
}
int nz_clipboard_set(nz_connection *c, const char *text) {
    if (!c || !text || !c->data_device || !c->serial || strlen(text)>16*1024*1024) return 0;
    char *copy=strdup(text); if (!copy) return 0;
    free(c->clipboard); c->clipboard=copy;
    if (c->data_source) wl_data_source_destroy(c->data_source);
    c->data_source=wl_data_device_manager_create_data_source(c->data_manager);
    if (!c->data_source) return 0;
    wl_data_source_add_listener(c->data_source,&source_listener,c);
    wl_data_source_offer(c->data_source,"text/plain;charset=utf-8"); wl_data_source_offer(c->data_source,"text/plain");
    wl_data_device_set_selection(c->data_device,c->data_source,c->serial);
    return 1;
}
int nz_clipboard_request(nz_connection *c) {
    if (!c || c->clipboard_fd>=0) return 0;
    if (c->data_source) { emit(c,NULL,NZ_CLIPBOARD,0,0,0,0,0,0,c->clipboard); return 1; }
    if (!c->selection || (!c->selection->utf8 && !c->selection->plain && !c->selection->legacy_utf8)) { emit(c,NULL,NZ_CLIPBOARD,0,0,0,0,0,0,NULL); return 1; }
    int fds[2]; if (pipe2(fds,O_CLOEXEC)<0) return 0;
    fcntl(fds[0],F_SETFL,O_NONBLOCK);
    wl_data_offer_receive(c->selection->offer,c->selection->utf8 ? "text/plain;charset=utf-8":c->selection->plain ? "text/plain":"UTF8_STRING",fds[1]);
    close(fds[1]); c->clipboard_fd=fds[0]; free(c->received); c->received=NULL; c->received_length=0;
    return 1;
}
static void clipboard_read(nz_connection *c) {
    char chunk[4096];
    for (;;) {
        ssize_t n=read(c->clipboard_fd,chunk,sizeof(chunk));
        if (n<0 && errno==EINTR) continue;
        if (n<0 && (errno==EAGAIN || errno==EWOULDBLOCK)) return;
        if (n<=0) { close(c->clipboard_fd); c->clipboard_fd=-1; emit(c,NULL,NZ_CLIPBOARD,0,0,0,0,0,0,c->received?c->received:""); return; }
        if (c->received_length+(size_t)n>16*1024*1024) { close(c->clipboard_fd); c->clipboard_fd=-1; emit(c,NULL,NZ_CLIPBOARD,0,0,0,0,0,0,NULL); return; }
        char *p=realloc(c->received,c->received_length+(size_t)n+1);
        if (!p) { close(c->clipboard_fd); c->clipboard_fd=-1; fail(c,"Cannot allocate clipboard text."); return; }
        c->received=p; memcpy(p+c->received_length,chunk,(size_t)n); c->received_length+=(size_t)n; p[c->received_length]=0;
    }
}
static void transfer_write(nz_connection *c, struct nz_transfer *transfer) {
    sigset_t blocked, previous, pending;
    sigemptyset(&blocked); sigaddset(&blocked,SIGPIPE);
    sigprocmask(SIG_BLOCK,&blocked,&previous); sigpending(&pending);
    int had_signal=sigismember(&pending,SIGPIPE);
    ssize_t n=write(transfer->fd,transfer->text+transfer->offset,transfer->length-transfer->offset);
    int error=errno;
    if (n<0 && error==EPIPE && !had_signal) { struct timespec immediate={0}; sigtimedwait(&blocked,NULL,&immediate); }
    sigprocmask(SIG_SETMASK,&previous,NULL);
    if (n>0) transfer->offset+=(size_t)n;
    if (transfer->offset==transfer->length || (n<0 && error!=EINTR && error!=EAGAIN && error!=EWOULDBLOCK)) {
        struct nz_transfer **link=&c->transfers;
        while (*link && *link!=transfer) link=&(*link)->next;
        if (*link) *link=transfer->next;
        close(transfer->fd); free(transfer->text); free(transfer);
    }
}

static void seat_capabilities(void *data, struct wl_seat *seat, uint32_t capabilities) {
    nz_connection *c=data;
    if ((capabilities&WL_SEAT_CAPABILITY_POINTER) && !c->pointer) { c->pointer=wl_seat_get_pointer(seat); wl_pointer_add_listener(c->pointer,&pointer_listener,c); }
    if (!(capabilities&WL_SEAT_CAPABILITY_POINTER) && c->pointer) { wl_pointer_release(c->pointer); c->pointer=NULL; c->pointer_window=NULL; }
    if ((capabilities&WL_SEAT_CAPABILITY_KEYBOARD) && !c->keyboard) { c->keyboard=wl_seat_get_keyboard(seat); wl_keyboard_add_listener(c->keyboard,&keyboard_listener,c); }
    if (!(capabilities&WL_SEAT_CAPABILITY_KEYBOARD) && c->keyboard) { wl_keyboard_release(c->keyboard); c->keyboard=NULL; c->keyboard_window=NULL; }
}
static void seat_name(void *data, struct wl_seat *seat, const char *name) { }
static const struct wl_seat_listener seat_listener = { .capabilities=seat_capabilities,.name=seat_name };
static void effect_capabilities(void *data, struct ext_background_effect_manager_v1 *manager, uint32_t flags) {
    nz_connection *c=data; c->effect_capabilities=flags;
}
static const struct ext_background_effect_manager_v1_listener effect_listener = { .capabilities=effect_capabilities };
static void output_geometry(void *data, struct wl_output *output, int32_t x, int32_t y, int32_t width, int32_t height, int32_t subpixel, const char *make, const char *model, int32_t transform) { }
static void output_mode(void *data, struct wl_output *output, uint32_t flags, int32_t width, int32_t height, int32_t refresh) { }
static void output_done(void *data, struct wl_output *output) {
    struct nz_output *o=data; for (nz_window *w=o->connection->windows;w;w=w->next) output_scale_for_window(w);
}
static void output_scale(void *data, struct wl_output *output, int32_t factor) { struct nz_output *o=data; o->scale=factor>0?factor:1; }
static const struct wl_output_listener output_listener = { .geometry=output_geometry,.mode=output_mode,.done=output_done,.scale=output_scale };
static void global(void *data, struct wl_registry *registry, uint32_t name, const char *interface, uint32_t version) {
    nz_connection *c=data;
    /* Runtime interface metadata can be newer than the compiled surface listener. */
    uint32_t compositor_version=4;
#ifdef WL_SURFACE_PREFERRED_BUFFER_SCALE_SINCE_VERSION
    compositor_version=6;
#endif
#define BIND(member, type, max_version) if (!c->member && strcmp(interface,#type)==0) { uint32_t supported=(uint32_t)type##_interface.version; if (supported>max_version) supported=max_version; c->member=wl_registry_bind(registry,name,&type##_interface,version<supported?version:supported); }
    BIND(compositor,wl_compositor,compositor_version)
    else BIND(subcompositor,wl_subcompositor,1)
    else BIND(shm,wl_shm,1)
    else BIND(shell,xdg_wm_base,6)
    else BIND(viewporter,wp_viewporter,1)
    else BIND(fractional,wp_fractional_scale_manager_v1,1)
    else BIND(activation,xdg_activation_v1,1)
    else BIND(decorations,zxdg_decoration_manager_v1,1)
    else BIND(text_manager,zwp_text_input_manager_v3,1)
    else BIND(icons,xdg_toplevel_icon_manager_v1,1)
    else BIND(exporter,zxdg_exporter_v2,1)
    else BIND(importer,zxdg_importer_v2,1)
    else if (!c->effects && strcmp(interface,"ext_background_effect_manager_v1")==0) {
        c->effects=wl_registry_bind(registry,name,&ext_background_effect_manager_v1_interface,1);
        ext_background_effect_manager_v1_add_listener(c->effects,&effect_listener,c);
    }
    else BIND(data_manager,wl_data_device_manager,1)
    else if (!c->seat && strcmp(interface,"wl_seat")==0 && version>=5) {
        c->seat=wl_registry_bind(registry,name,&wl_seat_interface,5); c->seat_name=name;
        wl_seat_add_listener(c->seat,&seat_listener,c);
    }
    else if (strcmp(interface,"wl_output")==0 && version>=2) {
        struct nz_output *o=calloc(1,sizeof(*o));
        if (!o) { fail(c,"Cannot allocate a Wayland output."); return; }
        o->name=name; o->scale=1; o->connection=c; o->output=wl_registry_bind(registry,name,&wl_output_interface,2);
        o->next=c->outputs; c->outputs=o; wl_output_add_listener(o->output,&output_listener,o);
    }
#undef BIND
    setup_text(c); setup_data(c);
}
static void global_remove(void *data, struct wl_registry *registry, uint32_t name) {
    nz_connection *c=data;
    struct nz_output **output_link=&c->outputs;
    while (*output_link) {
        struct nz_output *o=*output_link;
        if (o->name==name) {
            for (nz_window *w=c->windows;w;w=w->next) surface_leave(w,w->surface,o->output);
            *output_link=o->next; wl_output_destroy(o->output); free(o); break;
        }
        output_link=&o->next;
    }
    if (c->seat_name==name) {
        if (c->pointer) wl_pointer_release(c->pointer);
        if (c->keyboard) wl_keyboard_release(c->keyboard);
        if (c->text_input) zwp_text_input_v3_destroy(c->text_input);
        if (c->data_device) wl_data_device_destroy(c->data_device);
        wl_seat_release(c->seat); c->seat=NULL; c->pointer=NULL; c->keyboard=NULL; c->text_input=NULL; c->data_device=NULL;
        c->keyboard_window=NULL; c->pointer_window=NULL; c->text_window=NULL; c->seat_name=0;
    }
}
static const struct wl_registry_listener registry_listener = { .global=global,.global_remove=global_remove };
static void exported_handle(void *data, struct zxdg_exported_v2 *exported, const char *handle) {
    nz_window *w=data; emit(w->connection,w,18,0,0,0,0,0,0,handle);
}
static const struct zxdg_exported_v2_listener exported_listener = { .handle=exported_handle };
nz_connection *nz_connect(char *error, size_t error_size) {
    nz_connection *c=calloc(1,sizeof(*c));
    if (!c) { snprintf(error,error_size,"Cannot allocate the Wayland connection."); return NULL; }
    c->wake_fd=-1; c->clipboard_fd=-1;
    c->display=wl_display_connect(NULL);
    if (!c->display) { snprintf(error,error_size,"Cannot connect to Wayland. Check WAYLAND_DISPLAY and XDG_RUNTIME_DIR."); free(c); return NULL; }
    c->wake_fd=eventfd(0,EFD_CLOEXEC|EFD_NONBLOCK);
    c->xkb=xkb_context_new(XKB_CONTEXT_NO_FLAGS);
    c->registry=wl_display_get_registry(c->display);
    if (c->wake_fd<0 || !c->xkb || !c->registry) fail(c,"Cannot initialize Wayland resources.");
    if (!c->failed) {
        wl_registry_add_listener(c->registry,&registry_listener,c);
        if (wl_display_roundtrip(c->display)<0) fail(c,"The Wayland compositor disconnected during initialization.");
        if (!c->compositor || wl_compositor_get_version(c->compositor)<4 || !c->shm || !c->shell) fail(c,"The compositor must provide wl_compositor version 4, wl_shm and xdg_wm_base.");
    }
    if (!c->failed) {
        xdg_wm_base_add_listener(c->shell,&wm_listener,c); setup_text(c); setup_data(c);
        c->cursor_theme=wl_cursor_theme_load(NULL,24,c->shm); c->cursor_surface=wl_compositor_create_surface(c->compositor);
        const char *locale=getenv("LC_ALL"); if (!locale || !*locale) locale=getenv("LC_CTYPE"); if (!locale || !*locale) locale=getenv("LANG"); if (!locale || !*locale) locale="C.UTF-8";
        c->compose_table=xkb_compose_table_new_from_locale(c->xkb,locale,XKB_COMPOSE_COMPILE_NO_FLAGS);
        if (c->compose_table) c->compose=xkb_compose_state_new(c->compose_table,XKB_COMPOSE_STATE_NO_FLAGS);
        if (wl_display_roundtrip(c->display)<0) fail(c,"The compositor disconnected while binding input services.");
    }
    if (c->failed) { snprintf(error,error_size,"%s",c->error); nz_disconnect(c); return NULL; }
    return c;
}
int nz_dispatch(nz_connection *c, int timeout_ms) {
    if (!c || c->failed) return -1;
    while (wl_display_prepare_read(c->display)!=0) {
        if (wl_display_dispatch_pending(c->display)<0) { fail(c,"The Wayland connection failed while dispatching events."); return -1; }
    }
    int flush=wl_display_flush(c->display);
    if (flush<0 && errno!=EAGAIN) { wl_display_cancel_read(c->display); fail(c,"Cannot flush the Wayland connection."); return -1; }
    struct pollfd fds[67]={{wl_display_get_fd(c->display),POLLIN|(flush<0?POLLOUT:0),0},{c->wake_fd,POLLIN,0},{c->clipboard_fd,POLLIN,0}};
    struct nz_transfer *transfers[64]; int transfer_count=0;
    for (struct nz_transfer *t=c->transfers;t && transfer_count<64;t=t->next) {
        transfers[transfer_count]=t; fds[3+transfer_count]=(struct pollfd){t->fd,POLLOUT,0}; transfer_count++;
    }
    int result=poll(fds,(nfds_t)(3+transfer_count),c->head ? 0:timeout_ms);
    int saved_errno=errno;
    if (result>0 && (fds[0].revents&POLLIN)) { if (wl_display_read_events(c->display)<0) fail(c,"Cannot read Wayland events."); }
    else wl_display_cancel_read(c->display);
    if (result<0 && saved_errno!=EINTR) fail(c,"Cannot poll the Wayland event loop.");
    if (fds[0].revents&(POLLERR|POLLHUP|POLLNVAL)) fail(c,"The Wayland compositor disconnected.");
    if (fds[1].revents&POLLIN) {
        uint64_t value;
        ssize_t result;
        do { result=read(c->wake_fd,&value,sizeof(value)); } while (result<0 && errno==EINTR);
    }
    if (c->clipboard_fd>=0 && (fds[2].revents&(POLLIN|POLLHUP|POLLERR))) clipboard_read(c);
    for (int i=0;i<transfer_count;i++) if (fds[3+i].revents&(POLLOUT|POLLHUP|POLLERR)) transfer_write(c,transfers[i]);
    if (!c->failed && wl_display_dispatch_pending(c->display)<0) fail(c,"The compositor rejected a Wayland request.");
    if (c->failed) return -1;
    return 0;
}
static void popup_configure(void *data, struct xdg_popup *popup, int32_t x, int32_t y, int32_t width, int32_t height) {
    nz_window *w=data; w->popup_x=x; w->popup_y=y; w->popup_width=width; w->popup_height=height;
}
static void popup_done(void *data, struct xdg_popup *popup) {
    nz_window *w=data; w->popup_dismissed=1; w->mapped=0;
    emit(w->connection,w,NZ_POPUP_DONE,0,0,0,0,0,0,NULL);
}
static void popup_repositioned(void *data, struct xdg_popup *popup, uint32_t token) { }
static const struct xdg_popup_listener popup_listener = { .configure=popup_configure,.popup_done=popup_done,.repositioned=popup_repositioned };

static struct xdg_positioner *popup_positioner(nz_window *parent, const nz_popup_placement *p) {
    if (!p || p->width<1 || p->height<1 || p->width>32768 || p->height>32768 || p->shadow<0 || p->shadow>4096) return NULL;
    struct xdg_positioner *positioner=xdg_wm_base_create_positioner(parent->connection->shell);
    if (!positioner) return NULL;
    xdg_positioner_set_size(positioner,p->width,p->height);
    int aw=p->anchor_width>0?p->anchor_width:1, ah=p->anchor_height>0?p->anchor_height:1;
    xdg_positioner_set_anchor_rect(positioner,p->anchor_x,p->anchor_y,aw,ah);
    uint32_t anchor, gravity;
    if (p->side==2 || p->side==3) {
        anchor=p->side==2?XDG_POSITIONER_ANCHOR_RIGHT:XDG_POSITIONER_ANCHOR_LEFT;
        gravity=p->side==2?XDG_POSITIONER_GRAVITY_RIGHT:XDG_POSITIONER_GRAVITY_LEFT;
        if (p->alignment==0) { anchor=p->side==2?XDG_POSITIONER_ANCHOR_TOP_RIGHT:XDG_POSITIONER_ANCHOR_TOP_LEFT; gravity=p->side==2?XDG_POSITIONER_GRAVITY_BOTTOM_RIGHT:XDG_POSITIONER_GRAVITY_BOTTOM_LEFT; }
        else if (p->alignment==2) { anchor=p->side==2?XDG_POSITIONER_ANCHOR_BOTTOM_RIGHT:XDG_POSITIONER_ANCHOR_BOTTOM_LEFT; gravity=p->side==2?XDG_POSITIONER_GRAVITY_TOP_RIGHT:XDG_POSITIONER_GRAVITY_TOP_LEFT; }
    } else {
        anchor=p->side==1?XDG_POSITIONER_ANCHOR_TOP:XDG_POSITIONER_ANCHOR_BOTTOM;
        gravity=p->side==1?XDG_POSITIONER_GRAVITY_TOP:XDG_POSITIONER_GRAVITY_BOTTOM;
        if (p->alignment==0) { anchor=p->side==1?XDG_POSITIONER_ANCHOR_TOP_LEFT:XDG_POSITIONER_ANCHOR_BOTTOM_LEFT; gravity=p->side==1?XDG_POSITIONER_GRAVITY_TOP_RIGHT:XDG_POSITIONER_GRAVITY_BOTTOM_RIGHT; }
        else if (p->alignment==2) { anchor=p->side==1?XDG_POSITIONER_ANCHOR_TOP_RIGHT:XDG_POSITIONER_ANCHOR_BOTTOM_RIGHT; gravity=p->side==1?XDG_POSITIONER_GRAVITY_TOP_LEFT:XDG_POSITIONER_GRAVITY_BOTTOM_LEFT; }
    }
    xdg_positioner_set_anchor(positioner,anchor); xdg_positioner_set_gravity(positioner,gravity);
    xdg_positioner_set_offset(positioner,p->side==2?p->offset:p->side==3?-p->offset:0,p->side==0?p->offset:p->side==1?-p->offset:0);
    xdg_positioner_set_constraint_adjustment(positioner,XDG_POSITIONER_CONSTRAINT_ADJUSTMENT_FLIP_Y|XDG_POSITIONER_CONSTRAINT_ADJUSTMENT_FLIP_X|
        XDG_POSITIONER_CONSTRAINT_ADJUSTMENT_SLIDE_X|XDG_POSITIONER_CONSTRAINT_ADJUSTMENT_RESIZE_X|XDG_POSITIONER_CONSTRAINT_ADJUSTMENT_RESIZE_Y);
    if (xdg_positioner_get_version(positioner)>=3) xdg_positioner_set_reactive(positioner);
    return positioner;
}

nz_window *nz_popup_create(nz_window *parent, const nz_popup_placement *p) {
    if (!parent || !parent->mapped || !p || ((p->flags&1) && (!p->serial || !parent->connection->seat))) return NULL;
    struct xdg_positioner *positioner=popup_positioner(parent,p);
    if (!positioner) return NULL;
    nz_connection *c=parent->connection;
    nz_window *w=calloc(1,sizeof(*w));
    if (!w) { xdg_positioner_destroy(positioner); return NULL; }
    w->connection=c; w->parent=parent; w->popup_placement=*p;
    w->width=p->width+2*p->shadow; w->height=p->height+2*p->shadow; w->scale=parent->scale; w->preferred_scale=parent->preferred_scale;
    w->surface=wl_compositor_create_surface(c->compositor);
    if (w->surface) {
        wl_surface_add_listener(w->surface,&surface_listener,w);
        w->shell=xdg_wm_base_get_xdg_surface(c->shell,w->surface);
    }
    if (w->shell) {
        xdg_surface_add_listener(w->shell,&shell_listener,w);
        w->popup=xdg_surface_get_popup(w->shell,parent->shell,positioner);
    }
    xdg_positioner_destroy(positioner);
    if (!w->popup) { nz_window_destroy(w); return NULL; }
    xdg_popup_add_listener(w->popup,&popup_listener,w);
    if (p->flags&1) xdg_popup_grab(w->popup,c->seat,p->serial);
    if (c->viewporter) w->viewport=wp_viewporter_get_viewport(c->viewporter,w->surface);
    if (c->fractional && w->viewport) { w->fractional=wp_fractional_scale_manager_v1_get_fractional_scale(c->fractional,w->surface); wp_fractional_scale_v1_add_listener(w->fractional,&scale_listener,w); }
    w->next=c->windows; c->windows=w; w->visible=1; w->initial_commit=1;
    xdg_surface_set_window_geometry(w->shell,p->shadow,p->shadow,p->width,p->height);
    struct wl_region *input=wl_compositor_create_region(c->compositor);
    wl_region_add(input,p->shadow,p->shadow,p->width,p->height); wl_surface_set_input_region(w->surface,input); wl_region_destroy(input);
    wl_surface_commit(w->surface);
    return w;
}
int nz_popup_place(nz_window *w, const nz_popup_placement *p) {
    if (!w || !w->popup || w->popup_dismissed) return 0;
    if (!memcmp(&w->popup_placement,p,offsetof(nz_popup_placement,serial))) return 1;
    if (xdg_popup_get_version(w->popup)<3) return 0;
    struct xdg_positioner *positioner=popup_positioner(w->parent,p);
    if (!positioner) return 0;
    w->popup_placement=*p;
    xdg_popup_reposition(w->popup,positioner,1);
    xdg_positioner_destroy(positioner);
    return 1;
}
void nz_popup_input_region(nz_window *w, int x, int y, int width, int height) {
    if(!w || !w->popup || w->popup_dismissed || width<0 || height<0) return;
    struct wl_region *input=wl_compositor_create_region(w->connection->compositor);
    if(width>0 && height>0) wl_region_add(input,x,y,width,height);
    wl_surface_set_input_region(w->surface,input); wl_region_destroy(input);
}
int nz_popup_work_area(nz_window *owner, int x, int y, int width, int height, nz_damage_rect *area) { return 0; }

nz_window *nz_window_create(nz_connection *c, const char *title, const char *app_id, int width, int height, nz_window *parent, int custom_titlebar, int borderless) {
    if (!c || c->failed || width<=0 || height<=0 || (parent && parent->connection!=c)) return NULL;
    nz_window *w=calloc(1,sizeof(*w)); if (!w) return NULL;
    w->connection=c; w->width=width; w->height=height; w->scale=1; w->preferred_scale=120; w->capabilities=15;
    w->custom_titlebar=custom_titlebar; w->decoration_mode=1; w->borderless=borderless;
    w->surface=wl_compositor_create_surface(c->compositor);
    if (!w->surface) { free(w); return NULL; }
    wl_surface_add_listener(w->surface,&surface_listener,w);
    w->shell=xdg_wm_base_get_xdg_surface(c->shell,w->surface);
    if (!w->shell) { nz_window_destroy(w); return NULL; }
    xdg_surface_add_listener(w->shell,&shell_listener,w);
    w->toplevel=xdg_surface_get_toplevel(w->shell);
    if (!w->toplevel) { nz_window_destroy(w); return NULL; }
    xdg_toplevel_add_listener(w->toplevel,&toplevel_listener,w);
    if (c->exporter) { w->exported=zxdg_exporter_v2_export_toplevel(c->exporter,w->surface); zxdg_exported_v2_add_listener(w->exported,&exported_listener,w); }
    xdg_toplevel_set_title(w->toplevel,title ? title:""); xdg_toplevel_set_app_id(w->toplevel,app_id ? app_id:"org.nazor.Application");
    w->parent=parent;
    if (parent && parent->mapped) xdg_toplevel_set_parent(w->toplevel,parent->toplevel);
    if (c->decorations && !borderless) {
        w->decoration=zxdg_decoration_manager_v1_get_toplevel_decoration(c->decorations,w->toplevel);
        zxdg_toplevel_decoration_v1_add_listener(w->decoration,&decoration_listener,w);
        zxdg_toplevel_decoration_v1_set_mode(w->decoration,custom_titlebar ? 1:2);
    }
    if (c->viewporter) w->viewport=wp_viewporter_get_viewport(c->viewporter,w->surface);
    if (c->fractional && w->viewport) { w->fractional=wp_fractional_scale_manager_v1_get_fractional_scale(c->fractional,w->surface); wp_fractional_scale_v1_add_listener(w->fractional,&scale_listener,w); }
    w->next=c->windows; c->windows=w;
    return w;
}
void nz_window_titlebar(nz_window *w, int custom_titlebar) {
    if (!w || w->borderless) return;
    w->custom_titlebar=custom_titlebar;
    if (w->decoration) zxdg_toplevel_decoration_v1_set_mode(w->decoration,custom_titlebar ? 1:2);
    if (w->mapped) wl_surface_commit(w->surface);
}
void nz_window_show(nz_window *w, int visible) {
    if (!w || w->visible==visible) return;
    w->visible=visible;
    if (!visible) {
        nz_window_outer_resize(w,0);
        nz_window_wayland_shadow(w,NULL,0,0,0,1);
        if (w->frame) { wl_callback_destroy(w->frame); w->frame=NULL; }
        if (w->mapped) {
            wl_surface_attach(w->surface,NULL,0,0); wl_surface_commit(w->surface);
            w->configured=0; w->mapped=0; w->initial_commit=0;
        }
    } else if (!w->initial_commit) {
        xdg_surface_set_window_geometry(w->shell,0,0,w->width,w->height); wl_surface_commit(w->surface); w->initial_commit=1;
    } else if (w->configured) emit(w->connection,w,NZ_CONFIGURE,w->tiled,0,0,w->width,w->height,w->state,NULL);
}
void nz_window_title(nz_window *w, const char *title) { if (w) xdg_toplevel_set_title(w->toplevel,title ? title:""); }
void nz_window_state(nz_window *w, int state) {
    if (!w) return;
    if (state!=3) xdg_toplevel_unset_fullscreen(w->toplevel);
    if (state==1) xdg_toplevel_set_maximized(w->toplevel);
    else if (state==2) xdg_toplevel_set_minimized(w->toplevel);
    else if (state==3) xdg_toplevel_set_fullscreen(w->toplevel,NULL);
    else xdg_toplevel_unset_maximized(w->toplevel);
}
void nz_window_operations(nz_window *w, int resize, int width, int height) {
    if (!w) return;
    xdg_toplevel_set_min_size(w->toplevel,resize ? 1:width,resize ? 1:height);
    xdg_toplevel_set_max_size(w->toplevel,resize ? 0:width,resize ? 0:height);
}
void nz_window_resize(nz_window *w, int width, int height) {
    if (!w || width<=0 || height<=0) return;
    w->width=width; w->height=height;
    xdg_surface_set_window_geometry(w->shell,0,0,width,height);
    emit(w->connection,w,NZ_CONFIGURE,w->tiled,0,0,width,height,w->state,NULL);
}
static void token_done(void *data, struct xdg_activation_token_v1 *token, const char *value) {
    nz_window *w=data; xdg_activation_v1_activate(w->connection->activation,value,w->surface);
    xdg_activation_token_v1_destroy(token); w->activation_token=NULL;
}
static const struct xdg_activation_token_v1_listener token_listener = { .done=token_done };
void nz_window_activate(nz_window *w) {
    if (!w || !w->connection->activation || w->activation_token) return;
    nz_connection *c=w->connection;
    const char *startup_token=getenv("XDG_ACTIVATION_TOKEN");
    if (startup_token && *startup_token) { xdg_activation_v1_activate(c->activation,startup_token,w->surface); unsetenv("XDG_ACTIVATION_TOKEN"); return; }
    w->activation_token=xdg_activation_v1_get_activation_token(c->activation);
    xdg_activation_token_v1_add_listener(w->activation_token,&token_listener,w);
    if (c->seat && c->serial) xdg_activation_token_v1_set_serial(w->activation_token,c->serial,c->seat);
    nz_window *requestor=c->keyboard_window ? c->keyboard_window:c->pointer_window;
    xdg_activation_token_v1_set_surface(w->activation_token,requestor ? requestor->surface:w->surface); xdg_activation_token_v1_commit(w->activation_token);
}
void nz_window_move(nz_window *w) { if (w && w->connection->seat && w->connection->serial) xdg_toplevel_move(w->toplevel,w->connection->seat,w->connection->serial); }
int nz_window_menu(nz_window *w, uint32_t serial, int x, int y) {
    if (!w || !serial || !w->connection->seat || !(w->capabilities&1u)) return 0;
    xdg_toplevel_show_window_menu(w->toplevel,w->connection->seat,serial,x,y);
    return 1;
}
void nz_window_wayland_resize(nz_window *w, uint32_t edges, uint32_t serial) { if (w && w->connection->seat && serial) xdg_toplevel_resize(w->toplevel,w->connection->seat,serial,edges); }
void nz_window_interactive_resize(nz_window *w, uint32_t edges) { if (w) nz_window_wayland_resize(w,edges,w->connection->serial); }
void nz_window_cursor(nz_window *w, const char *name) {
    if (!w) return;
    nz_connection *c=w->connection;
    if (!c->pointer || c->pointer_window!=w || !c->cursor_theme || !c->cursor_surface) return;
    struct wl_cursor *cursor=wl_cursor_theme_get_cursor(c->cursor_theme,name);
    if (!cursor) cursor=wl_cursor_theme_get_cursor(c->cursor_theme,"left_ptr");
    if (!cursor || !cursor->image_count) return;
    struct wl_cursor_image *image=cursor->images[0]; struct wl_buffer *buffer=wl_cursor_image_get_buffer(image);
    if (!buffer) return;
    wl_pointer_set_cursor(c->pointer,c->pointer_serial,c->cursor_surface,image->hotspot_x,image->hotspot_y);
    wl_surface_attach(c->cursor_surface,buffer,0,0); wl_surface_damage(c->cursor_surface,0,0,INT_MAX,INT_MAX); wl_surface_commit(c->cursor_surface);
}
uint32_t nz_window_capabilities(nz_window *w) {
    if (!w) return 0;
    return w->capabilities | (w->connection->icons ? 16u:0u) | (w->decoration_mode==2 ? 32u:0u);
}
int nz_window_icon(nz_window *w, const void *pixels, int width, int height) {
    if (!w || !w->connection->icons) return 0;
    struct xdg_toplevel_icon_v1 *icon=NULL; struct nz_buffer *b=NULL;
    if (pixels) {
        b=buffer_create(w->connection,width,height); if (!b) return 0;
        memcpy(b->pixels,pixels,b->bytes);
        icon=xdg_toplevel_icon_manager_v1_create_icon(w->connection->icons);
        xdg_toplevel_icon_v1_add_buffer(icon,b->buffer,1);
    }
    xdg_toplevel_icon_manager_v1_set_icon(w->connection->icons,w->toplevel,icon);
    if (w->icon) xdg_toplevel_icon_v1_destroy(w->icon);
    buffer_free(w->icon_buffer); w->icon=icon; w->icon_buffer=b;
    return 1;
}
int nz_window_wayland_shadow(nz_window *w,const void *pixels,int width,int height,int margin,int scale) {
    if (!w) return 0;
    if (!pixels) {
        if (w->shadow) wl_subsurface_destroy(w->shadow);
        if (w->shadow_surface) wl_surface_destroy(w->shadow_surface);
        buffer_free(w->shadow_buffer);
        w->shadow=NULL;
        w->shadow_surface=NULL;
        w->shadow_buffer=NULL;
        return 1;
    }
    nz_connection *c=w->connection;
    if (!c->subcompositor || !w->configured || !w->visible) return 0;
    struct nz_buffer *buffer=buffer_create(c,width,height);
    if (!buffer) return 0;
    if (!w->shadow) {
        w->shadow_surface=wl_compositor_create_surface(c->compositor);
        if (w->shadow_surface)
            w->shadow=wl_subcompositor_get_subsurface(c->subcompositor,w->shadow_surface,w->surface);
        if (!w->shadow) {
            nz_window_wayland_shadow(w,NULL,0,0,0,1);
            buffer_free(buffer);
            return 0;
        }
        struct wl_region *empty=wl_compositor_create_region(c->compositor);
        if (!empty) {
            nz_window_wayland_shadow(w,NULL,0,0,0,1);
            buffer_free(buffer);
            return 0;
        }
        wl_surface_set_input_region(w->shadow_surface,empty);
        wl_region_destroy(empty);
        wl_subsurface_place_below(w->shadow,w->surface);
    }
    memcpy(buffer->pixels,pixels,buffer->bytes);
    wl_subsurface_set_position(w->shadow,-margin,-margin);
    wl_surface_set_buffer_scale(w->shadow_surface,scale);
    wl_surface_attach(w->shadow_surface,buffer->buffer,0,0);
    wl_surface_damage_buffer(w->shadow_surface,0,0,width,height);
    wl_surface_commit(w->shadow_surface);
    if (w->mapped) wl_surface_commit(w->surface);
    buffer_free(w->shadow_buffer);
    w->shadow_buffer=buffer;
    return 1;
}
int nz_window_frame_shadow(nz_window *w,const void *pixels,int width,int height,int margin,float scale) {
    if (!isfinite(scale) || scale<1 || scale>8) return 0;
    return nz_window_wayland_shadow(w,pixels,width,height,margin,(int)scale);
}
void nz_window_outer_resize(nz_window *w,int margin) {
    if (!w) return;
    nz_connection *c=w->connection;
    if (margin<=0 || !c->subcompositor || !w->visible || !w->configured || w->borderless || w->decoration_mode==2 ||
        w->tiled || w->state!=NZ_STATE_NORMAL) {
        if (!w->resize_surface) return;
        if (c->pointer_window==w && c->pointer_on_resize) {
            emit(c,w,NZ_POINTER_LEAVE,0,0,0,0,0,0,NULL);
            c->pointer_window=NULL;
            c->pointer_on_resize=0;
        }
        if (w->resize_viewport) wp_viewport_destroy(w->resize_viewport);
        if (w->resize_subsurface) wl_subsurface_destroy(w->resize_subsurface);
        wl_surface_destroy(w->resize_surface);
        buffer_free(w->resize_buffer);
        w->resize_viewport=NULL; w->resize_subsurface=NULL; w->resize_surface=NULL; w->resize_buffer=NULL;
        w->resize_margin=0;
        if (w->mapped) wl_surface_commit(w->surface);
        return;
    }
    if (margin>128 || w->width>32768 || w->height>32768) { nz_window_outer_resize(w,0); return; }
    int width=w->width+2*margin, height=w->height+2*margin;
    if (w->resize_margin==margin && w->resize_width==width && w->resize_height==height) return;
    if (!c->viewporter && (size_t)width*height>16u*1024u*1024u) { nz_window_outer_resize(w,0); return; }
    if (!w->resize_surface) {
        w->resize_surface=wl_compositor_create_surface(c->compositor);
        if (!w->resize_surface) return;
        w->resize_subsurface=wl_subcompositor_get_subsurface(c->subcompositor,w->resize_surface,w->surface);
        if (c->viewporter) w->resize_viewport=wp_viewporter_get_viewport(c->viewporter,w->resize_surface);
        if (!w->resize_subsurface || (c->viewporter && !w->resize_viewport)) { nz_window_outer_resize(w,0); return; }
        wl_subsurface_place_below(w->resize_subsurface,w->surface);
    }
    if (!w->resize_buffer || (!w->resize_viewport && (width!=w->resize_width || height!=w->resize_height))) {
        struct nz_buffer *buffer=buffer_create(c,w->resize_viewport?1:width,w->resize_viewport?1:height);
        if (!buffer) { nz_window_outer_resize(w,0); return; }
        wl_surface_attach(w->resize_surface,buffer->buffer,0,0);
        wl_surface_damage_buffer(w->resize_surface,0,0,buffer->width,buffer->height);
        buffer_free(w->resize_buffer);
        w->resize_buffer=buffer;
    }
    struct wl_region *region=wl_compositor_create_region(c->compositor);
    if (!region) { nz_window_outer_resize(w,0); return; }
    wl_region_add(region,0,0,width,height);
    wl_region_subtract(region,margin,margin,w->width,w->height);
    wl_surface_set_input_region(w->resize_surface,region);
    wl_region_destroy(region);
    if (w->resize_viewport) wp_viewport_set_destination(w->resize_viewport,width,height);
    wl_subsurface_set_position(w->resize_subsurface,-margin,-margin);
    w->resize_margin=margin; w->resize_width=width; w->resize_height=height;
    wl_surface_commit(w->resize_surface);
    if (w->mapped) wl_surface_commit(w->surface);
}
int nz_window_blur(nz_window *w, int enabled) {
    if (!w || !w->connection->effects || !(w->connection->effect_capabilities & EXT_BACKGROUND_EFFECT_MANAGER_V1_CAPABILITY_BLUR)) return 0;
    if (!w->effect) w->effect=ext_background_effect_manager_v1_get_background_effect(w->connection->effects,w->surface);
    if (enabled) {
        struct wl_region *region=wl_compositor_create_region(w->connection->compositor);
        wl_region_add(region,0,0,INT_MAX,INT_MAX); ext_background_effect_surface_v1_set_blur_region(w->effect,region); wl_region_destroy(region);
    } else ext_background_effect_surface_v1_set_blur_region(w->effect,NULL);
    return 1;
}
void nz_window_destroy(nz_window *w) {
    if (!w) return;
    nz_connection *c=w->connection;
    for (;;) {
        nz_window *child=c->windows;
        while (child && !(child->parent==w && child->popup)) child=child->next;
        if (!child) break;
        nz_window_destroy(child);
    }
    for (nz_window *child=c->windows; child; child=child->next) if (child->parent==w) child->parent=NULL;
    if (c->pointer_window==w) c->pointer_window=NULL;
    if (c->keyboard_window==w) c->keyboard_window=NULL;
    if (c->text_window==w) c->text_window=NULL;
    for (struct nz_queued_event *e=c->head; e; e=e->next) if (e->value.window==w) e->value.window=NULL;
    nz_window **link=&c->windows; while (*link && *link!=w) link=&(*link)->next; if (*link) *link=w->next;
    if (w->activation_token) xdg_activation_token_v1_destroy(w->activation_token);
    if (w->exported) zxdg_exported_v2_destroy(w->exported);
    if (w->imported) zxdg_imported_v2_destroy(w->imported);
    if (w->effect) ext_background_effect_surface_v1_destroy(w->effect);
    if (w->frame) wl_callback_destroy(w->frame);
    nz_window_outer_resize(w,0);
    nz_window_wayland_shadow(w,NULL,0,0,0,1);
    if (w->icon) xdg_toplevel_icon_v1_destroy(w->icon);
    buffer_free(w->icon_buffer);
    if (w->fractional) wp_fractional_scale_v1_destroy(w->fractional);
    if (w->viewport) wp_viewport_destroy(w->viewport);
    if (w->decoration) zxdg_toplevel_decoration_v1_destroy(w->decoration);
    if (w->popup) xdg_popup_destroy(w->popup);
    if (w->toplevel) xdg_toplevel_destroy(w->toplevel);
    if (w->shell) xdg_surface_destroy(w->shell);
    if (w->surface) wl_surface_destroy(w->surface);
    while (w->outputs) { struct nz_entered_output *entry=w->outputs; w->outputs=entry->next; free(entry); }
    while (w->buffers) { struct nz_buffer *b=w->buffers; w->buffers=b->next; buffer_free(b); }
    free(w);
}
void nz_disconnect(nz_connection *c) {
    if (!c) return;
    while (c->windows) nz_window_destroy(c->windows);
    if (c->clipboard_fd>=0) close(c->clipboard_fd);
    while (c->transfers) { struct nz_transfer *t=c->transfers; c->transfers=t->next; close(t->fd); free(t->text); free(t); }
    while (c->offers) { struct nz_offer *o=c->offers; c->offers=o->next; wl_data_offer_destroy(o->offer); free(o); }
    if (c->data_source) wl_data_source_destroy(c->data_source);
    if (c->data_device) wl_data_device_destroy(c->data_device);
    if (c->text_input) zwp_text_input_v3_destroy(c->text_input);
    if (c->pointer) wl_pointer_release(c->pointer);
    if (c->keyboard) wl_keyboard_release(c->keyboard);
    if (c->cursor_surface) wl_surface_destroy(c->cursor_surface);
    if (c->cursor_theme) wl_cursor_theme_destroy(c->cursor_theme);
    if (c->seat) wl_seat_release(c->seat);
    if (c->icons) xdg_toplevel_icon_manager_v1_destroy(c->icons);
    if (c->exporter) zxdg_exporter_v2_destroy(c->exporter);
    if (c->importer) zxdg_importer_v2_destroy(c->importer);
    if (c->effects) ext_background_effect_manager_v1_destroy(c->effects);
    if (c->data_manager) wl_data_device_manager_destroy(c->data_manager);
    if (c->text_manager) zwp_text_input_manager_v3_destroy(c->text_manager);
    if (c->decorations) zxdg_decoration_manager_v1_destroy(c->decorations);
    if (c->activation) xdg_activation_v1_destroy(c->activation);
    if (c->fractional) wp_fractional_scale_manager_v1_destroy(c->fractional);
    if (c->viewporter) wp_viewporter_destroy(c->viewporter);
    if (c->shell) xdg_wm_base_destroy(c->shell);
    if (c->shm) wl_shm_destroy(c->shm);
    if (c->subcompositor) wl_subcompositor_destroy(c->subcompositor);
    if (c->compositor) wl_compositor_destroy(c->compositor);
    if (c->registry) wl_registry_destroy(c->registry);
    while (c->outputs) { struct nz_output *o=c->outputs; c->outputs=o->next; wl_output_destroy(o->output); free(o); }
    if (c->display) { wl_display_flush(c->display); wl_display_disconnect(c->display); }
    if (c->wake_fd>=0) close(c->wake_fd);
    xkb_compose_state_unref(c->compose); xkb_compose_table_unref(c->compose_table);
    xkb_state_unref(c->key_state); xkb_keymap_unref(c->keymap); xkb_context_unref(c->xkb);
    free(c->clipboard); free(c->received); free(c->preedit); free(c->commit);
    free_event(c->current); while (c->head) { struct nz_queued_event *e=c->head; c->head=e->next; free_event(e); }
    free(c);
}

static void imported_destroyed(void *data, struct zxdg_imported_v2 *imported) {
    nz_window *w=data;
    zxdg_imported_v2_destroy(imported);
    if (w->imported==imported) w->imported=NULL;
}
static const struct zxdg_imported_v2_listener imported_listener = { .destroyed=imported_destroyed };
int nz_window_foreign_parent(nz_window *w, const char *handle) {
    if (!w || !w->toplevel || !handle || strncmp(handle,"wayland:",8) || !handle[8] || !w->connection->importer) return 0;
    struct zxdg_imported_v2 *imported=zxdg_importer_v2_import_toplevel(w->connection->importer,handle+8);
    if (!imported) return 0;
    if (w->imported) zxdg_imported_v2_destroy(w->imported);
    w->imported=imported;
    zxdg_imported_v2_add_listener(imported,&imported_listener,w);
    zxdg_imported_v2_set_parent_of(imported,w->surface);
    wl_display_flush(w->connection->display);
    return 1;
}
