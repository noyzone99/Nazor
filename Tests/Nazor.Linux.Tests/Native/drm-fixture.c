#define _GNU_SOURCE
#include <stdint.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <fcntl.h>
#include <pthread.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/eventfd.h>

typedef struct {
    uint32_t clock;
    uint16_t hdisplay, hsync_start, hsync_end, htotal, hskew;
    uint16_t vdisplay, vsync_start, vsync_end, vtotal, vscan;
    uint32_t vrefresh, flags, type;
    char name[32];
} drm_mode_t;
typedef struct {
    int count_fbs; uint32_t *fbs;
    int count_crtcs; uint32_t *crtcs;
    int count_connectors; uint32_t *connectors;
    int count_encoders; uint32_t *encoders;
    uint32_t min_width, max_width, min_height, max_height;
} resources_t;
typedef struct {
    uint32_t id, encoder, type, type_id;
    int connection; uint32_t width_mm, height_mm;
    int subpixel, count_modes; drm_mode_t *modes;
    int count_props; uint32_t *props; uint64_t *values;
    int count_encoders; uint32_t *encoders;
} connector_t;
typedef struct { uint32_t id, type, crtc, possible_crtcs, clones; } encoder_t;
typedef struct {
    uint32_t id, buffer, x, y, width, height;
    int mode_valid; drm_mode_t mode; int gamma;
} crtc_t;
typedef void (*seat_callback)(void *, void *);
typedef struct { seat_callback enable, disable; } listener_t;
typedef struct { int (*open)(const char *, int, void *); void (*close)(int, void *); } input_interface_t;
typedef struct { const input_interface_t *callbacks; void *user; } input_context_t;
typedef void (*flip_callback)(int, uint32_t, uint32_t, uint32_t, void *);
typedef struct { int version; void *vblank; flip_callback flip; void *flip2, *sequence; } events_t;
typedef struct { uint32_t height, width, bpp, flags, handle, pitch; uint64_t size; } dumb_t;
typedef struct { uint32_t handle, pad; uint64_t offset; } mapping_t;
typedef struct bo { int locked; uint32_t handle; void *user; void (*destroy)(struct bo *, void *); } bo_t;

_Static_assert(sizeof(drm_mode_t) == 68, "mode ABI");
_Static_assert(sizeof(resources_t) == 80, "resources ABI");
_Static_assert(sizeof(connector_t) == 88, "connector ABI");
_Static_assert(sizeof(crtc_t) == 100, "CRTC ABI");
_Static_assert(sizeof(events_t) == 40, "events ABI");

static int fault, counts[26], card_fd = -1, seat_fd = -1, seat_pending, seat_active;
static const listener_t *listener;
static uint32_t connector_id = 12, crtc_id = 42, encoder_id = 8;
static uint32_t current_fb = 99, pending_fb, next_fb = 100, next_handle = 1;
static void *pending_flip, *late_flip;
static const uint64_t tiled_modifier = (1ULL << 56) | 2;
static uint32_t fb_handles[256];
static drm_mode_t modes[2] = {
    {.hdisplay = 640, .vdisplay = 480, .vrefresh = 60},
    {.hdisplay = 300, .vdisplay = 180, .vrefresh = 60, .type = 8}
};
static bo_t bos[3] = {{.handle = 20}, {.handle = 21}, {.handle = 22}};

void nz_test_reset(void) {
    fault = 0; memset(counts, 0, sizeof(counts)); memset(fb_handles, 0, sizeof(fb_handles));
    current_fb = 99; pending_fb = 0; next_fb = 100; next_handle = 1;
    pending_flip = late_flip = NULL; seat_active = 0;
    for (int i = 0; i < 3; i++) bos[i].locked = 0;
}
void nz_test_fault(int value) { fault = value; }
int nz_test_count(int index) { return index == 21 ? card_fd >= 0 && fcntl(card_fd, F_GETFD) >= 0 : counts[index]; }
void nz_test_seat(int active) {
    if (fault == 14) seat_active = active;
    seat_pending = active ? 1 : -1;
    uint64_t value = 1; if (seat_fd >= 0) write(seat_fd, &value, sizeof(value));
}

void *libseat_open_seat(const listener_t *callbacks, void *user) {
    (void)user; listener = callbacks; seat_pending = 1;
    seat_fd = eventfd(0, EFD_NONBLOCK | EFD_CLOEXEC); counts[0]++;
    return (void *)1;
}
int libseat_close_seat(void *seat) { (void)seat; close(seat_fd); seat_fd = -1; counts[0]--; return 0; }
int libseat_get_fd(void *seat) { (void)seat; return seat_fd; }
const char *libseat_seat_name(void *seat) { (void)seat; return "seat0"; }
int libseat_dispatch(void *seat, int timeout) {
    (void)timeout; uint64_t value; read(seat_fd, &value, sizeof(value));
    int action = seat_pending; seat_pending = 0;
    if (action == 1) { seat_active = 1; listener->enable(seat, NULL); }
    if (action == -1) {
        seat_active = 0;
        listener->disable(seat, NULL);
        if (fault == 14) current_fb = 99;
    }
    return 0;
}
int libseat_disable_seat(void *seat) { (void)seat; counts[13]++; return 0; }
int libseat_switch_session(void *seat, int number) { (void)seat; counts[14] = number; return 0; }
int libseat_open_device(void *seat, const char *path, int *fd) {
    (void)seat;
    if (fault == 1 || strstr(path, "card") == NULL) { errno = EACCES; return -1; }
    card_fd = memfd_create("nazor-test-drm", MFD_CLOEXEC);
    if (card_fd < 0) return -1;
    ftruncate(card_fd, 32 * 1024 * 1024); *fd = card_fd; counts[1]++;
    return 7;
}
int libseat_close_device(void *seat, int id) { (void)seat; (void)id; counts[1]--; return 0; }

input_context_t *libinput_udev_create_context(const input_interface_t *callbacks, void *user, void *udev) {
    (void)udev;
    pthread_attr_t attributes;
    if (pthread_getattr_np(pthread_self(), &attributes) != 0) return NULL;
    void *stack;
    size_t stack_size;
    int result = pthread_attr_getstack(&attributes, &stack, &stack_size);
    pthread_attr_destroy(&attributes);
    if (result != 0 || (uintptr_t)callbacks - (uintptr_t)stack < stack_size) return NULL;
    input_context_t *input = malloc(sizeof(*input));
    if (input) *input = (input_context_t){ callbacks, user };
    return input;
}
int libinput_udev_assign_seat(input_context_t *input, const char *name) { (void)input; return strcmp(name, "seat0"); }
input_context_t *libinput_unref(input_context_t *input) {
    int fd = input->callbacks->open("/dev/dri/card0", O_NONBLOCK, input->user);
    if (fd >= 0) {
        if (fcntl(fd, F_GETFL) & O_NONBLOCK) counts[24]++;
        input->callbacks->close(fd, input->user);
        if (fcntl(fd, F_GETFD) < 0 && errno == EBADF) counts[25]++;
    }
    free(input);
    return NULL;
}

resources_t *drmModeGetResources(int fd) {
    (void)fd; if (fault == 2) return NULL;
    resources_t *result = calloc(1, sizeof(*result));
    result->count_crtcs = result->count_connectors = result->count_encoders = 1;
    result->crtcs = &crtc_id; result->connectors = &connector_id; result->encoders = &encoder_id;
    counts[2]++; return result;
}
void drmModeFreeResources(resources_t *value) { free(value); counts[2]--; }
connector_t *drmModeGetConnector(int fd, uint32_t id) {
    (void)fd; (void)id; connector_t *result = calloc(1, sizeof(*result));
    result->id = connector_id; result->encoder = encoder_id; result->connection = fault == 3 ? 2 : 1;
    result->count_modes = 2; result->modes = modes; result->count_encoders = 1; result->encoders = &encoder_id;
    counts[3]++; return result;
}
void drmModeFreeConnector(connector_t *value) { free(value); counts[3]--; }
encoder_t *drmModeGetEncoder(int fd, uint32_t id) {
    (void)fd; (void)id; if (fault == 4) return NULL;
    encoder_t *result = calloc(1, sizeof(*result)); result->id = encoder_id; result->crtc = crtc_id; result->possible_crtcs = 1;
    counts[4]++; return result;
}
void drmModeFreeEncoder(encoder_t *value) { free(value); counts[4]--; }
crtc_t *drmModeGetCrtc(int fd, uint32_t id) {
    (void)fd; crtc_t *result = calloc(1, sizeof(*result));
    result->id = id; result->buffer = 99; result->mode_valid = 1; result->mode = modes[0];
    counts[5]++; return result;
}
void drmModeFreeCrtc(crtc_t *value) { free(value); counts[5]--; }
int drmModeAddFB2(int fd, uint32_t width, uint32_t height, uint32_t format, uint32_t *handles, uint32_t *pitches, uint32_t *offsets, uint32_t *fb, uint32_t flags) {
    (void)fd; (void)width; (void)height; (void)format; (void)pitches; (void)flags;
    if (fault == 10 || fault == 13 || offsets[0] || offsets[1] || offsets[2] || offsets[3]) { errno = EINVAL; return -1; }
    *fb = next_fb++; fb_handles[*fb] = handles[0]; counts[6]++; return 0;
}
int drmModeAddFB2WithModifiers(int fd, uint32_t width, uint32_t height, uint32_t format, uint32_t *handles, uint32_t *pitches, uint32_t *offsets, uint64_t *modifiers, uint32_t *fb, uint32_t flags) {
    (void)fd;
    if (width != 300 || height != 180 || format != 0x34325258 || flags != 2 ||
        !handles[0] || handles[1] != handles[0] + 100 || pitches[0] != 1280 || pitches[1] != 320 ||
        offsets[0] != 4096 || offsets[1] != 65536 || modifiers[0] != tiled_modifier || modifiers[1] != tiled_modifier) {
        errno = EINVAL; return -1;
    }
    for (int plane = 2; plane < 4; plane++) {
        if (handles[plane] || pitches[plane] || offsets[plane] || modifiers[plane]) { errno = EINVAL; return -1; }
    }
    *fb = next_fb++; fb_handles[*fb] = handles[0]; counts[6]++; counts[22]++; return 0;
}
int drmModeRmFB(int fd, uint32_t fb) {
    (void)fd; if (current_fb == fb || pending_fb == fb) counts[15]++;
    fb_handles[fb] = 0; counts[6]--; return 0;
}
int drmModeSetCrtc(int fd, uint32_t crtc, uint32_t fb, uint32_t x, uint32_t y, uint32_t *connectors, int count, drm_mode_t *mode) {
    (void)fd; (void)crtc; (void)x; (void)y;
    if (fault == 7 && fb != 99) { errno = EIO; return -1; }
    if (fault == 14 && !seat_active) { errno = EACCES; return -1; }
    if (count != 1 || connectors[0] != connector_id || mode == NULL) { errno = EINVAL; return -1; }
    if (pending_fb) { late_flip = pending_flip; pending_flip = NULL; pending_fb = 0; }
    current_fb = fb;
    if (fb == 99) counts[12]++;
    else { counts[16] = mode->hdisplay; counts[17] = mode->vdisplay; }
    return 0;
}
int drmModePageFlip(int fd, uint32_t crtc, uint32_t fb, uint32_t flags, void *user) {
    (void)fd; (void)crtc; (void)flags;
    if (fault == 8) { errno = EBUSY; return -1; }
    if (fault == 14 && !seat_active) { errno = EACCES; return -1; }
    pending_flip = user; pending_fb = fb; counts[11]++; return 0;
}
int drmHandleEvent(int fd, events_t *events) {
    if (events->version != 2 || !events->flip) { errno = EINVAL; return -1; }
    if (fault == 12) { usleep(1000); return 0; }
    if (fault == 16) {
        fault = 14; nz_test_seat(0);
        return 0;
    }
    if (fault == 14 && !seat_active) return 0;
    if (late_flip) {
        events->flip(fd, 0, 0, 0, late_flip); late_flip = NULL;
        return 0;
    }
    current_fb = pending_fb; pending_fb = 0; counts[23]++;
    events->flip(fd, 0, 0, 0, pending_flip); pending_flip = NULL; return 0;
}
int drmIoctl(int fd, unsigned long request, void *value) {
    (void)fd;
    if (request == 0xc02064b2UL) {
        dumb_t *allocation = value; allocation->handle = next_handle++;
        allocation->pitch = allocation->width * 4 + 16; allocation->size = (uint64_t)allocation->pitch * allocation->height;
        counts[7]++; return 0;
    }
    if (request == 0xc01064b3UL) { mapping_t *map = value; map->offset = (uint64_t)map->handle * 1024 * 1024; return 0; }
    if (request == 0xc00464b4UL) { counts[7]--; return 0; }
    errno = EINVAL; return -1;
}
void *gbm_create_device(int fd) { (void)fd; if (fault == 5) return NULL; counts[8]++; return (void *)2; }
void gbm_device_destroy(void *device) { (void)device; counts[8]--; }
void *gbm_surface_create(void *device, uint32_t width, uint32_t height, uint32_t format, uint32_t flags) {
    (void)device; (void)width; (void)height; (void)format; (void)flags; counts[9]++; return (void *)3;
}
void gbm_surface_destroy(void *surface) { (void)surface; counts[9]--; }
void *gbm_surface_lock_front_buffer(void *surface) {
    (void)surface;
    for (int i = 0; i < 3; i++) if (!bos[i].locked) { bos[i].locked = 1; counts[10]++; return &bos[i]; }
    return NULL;
}
void gbm_surface_release_buffer(void *surface, bo_t *bo) {
    (void)surface;
    if (fb_handles[current_fb] == bo->handle || (pending_fb && fb_handles[pending_fb] == bo->handle)) counts[15]++;
    bo->locked = 0; counts[10]--;
    if (fault == 11 && bo->destroy) { bo->destroy(bo, bo->user); bo->user = NULL; bo->destroy = NULL; }
}
void gbm_bo_set_user_data(bo_t *bo, void *user, void (*destroy)(bo_t *, void *)) { bo->user = user; bo->destroy = destroy; }
uint64_t gbm_bo_get_modifier(bo_t *bo) { (void)bo; return fault == 13 ? tiled_modifier : (1ULL << 56) - 1; }
int gbm_bo_get_plane_count(bo_t *bo) { (void)bo; return fault == 13 ? 2 : 1; }
uint64_t gbm_bo_get_handle(bo_t *bo) { return bo->handle; }
uint32_t gbm_bo_get_stride(bo_t *bo) { (void)bo; return 1200; }
uint64_t gbm_bo_get_handle_for_plane(bo_t *bo, int plane) { return bo->handle + 100 * plane; }
uint32_t gbm_bo_get_stride_for_plane(bo_t *bo, int plane) { (void)bo; return fault == 13 ? plane == 0 ? 1280 : 320 : 1200; }
uint32_t gbm_bo_get_offset(bo_t *bo, int plane) { (void)bo; return fault == 13 ? plane == 0 ? 4096 : 65536 : 0; }
uint32_t gbm_bo_get_format(bo_t *bo) { (void)bo; return 0x34325258; }
void *eglGetPlatformDisplay(uint32_t platform, void *device, intptr_t *attributes) {
    (void)device; (void)attributes; if (platform != 0x31d7) return NULL; return (void *)4;
}
int eglInitialize(void *display, int *major, int *minor) { (void)display; *major = 1; *minor = 5; counts[18]++; return 1; }
int eglTerminate(void *display) { (void)display; counts[18]--; return 1; }
int eglBindAPI(uint32_t api) { return api == 0x30a0; }
int eglChooseConfig(void *display, int *attributes, void **configs, int size, int *count) {
    (void)display; (void)attributes; (void)size; configs[0] = (void *)10; configs[1] = (void *)11; *count = 2; return 1;
}
int eglGetConfigAttrib(void *display, void *config, int attribute, int *value) {
    (void)display; (void)attribute; *value = config == (void *)11 ? 0x34325258 : 0; return 1;
}
void *eglCreateContext(void *display, void *config, void *share, int *attributes) {
    (void)display; (void)share; (void)attributes;
    if (fault == 6 || config != (void *)11) return NULL;
    counts[19]++; return (void *)12;
}
int eglDestroyContext(void *display, void *context) { (void)display; (void)context; counts[19]--; return 1; }
void *eglCreateWindowSurface(void *display, void *config, void *window, int *attributes) {
    (void)display; (void)config; (void)window; (void)attributes; counts[20]++; return (void *)13;
}
int eglDestroySurface(void *display, void *surface) { (void)display; (void)surface; counts[20]--; return 1; }
int eglMakeCurrent(void *display, void *draw, void *read, void *context) { (void)display; (void)draw; (void)read; (void)context; return 1; }
int eglSwapBuffers(void *display, void *surface) { (void)display; (void)surface; return 1; }
int eglSwapInterval(void *display, int interval) { (void)display; (void)interval; return 1; }
int eglGetError(void) { return 0x3001; }
int eglReleaseThread(void) { return 1; }
void *eglGetProcAddress(const char *name) { (void)name; return NULL; }
