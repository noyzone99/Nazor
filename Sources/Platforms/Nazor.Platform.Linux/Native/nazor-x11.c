#define _POSIX_C_SOURCE 200809L
#include "nazor-wayland.h"
#include <X11/Xlib.h>
#include <X11/Xatom.h>
#include <X11/Xutil.h>
#include <X11/Xresource.h>
#include <X11/XKBlib.h>
#include <X11/Xcursor/Xcursor.h>
#include <X11/extensions/Xrender.h>
#include <X11/extensions/Xrandr.h>
#include <X11/extensions/shape.h>
#include <X11/keysym.h>
#include <errno.h>
#include <locale.h>
#include <math.h>
#include <poll.h>
#include <pthread.h>
#include <stdio.h>
#include <stdatomic.h>
#include <stdlib.h>
#include <string.h>
#include <sys/eventfd.h>
#include <unistd.h>

#define CLIP_LIMIT (32u*1024u*1024u)
#define CLIP_CHUNK 32768u
#define MAX_CLIENT_PIXELS 32768
enum { MOTIF_FUNCTIONS=1, MOTIF_DECORATIONS=2 };
enum { MOTIF_RESIZE=2, MOTIF_MOVE=4, MOTIF_MINIMIZE=8, MOTIF_MAXIMIZE=16, MOTIF_CLOSE=32 };
typedef struct queued_event { nz_event event; char *text; struct queued_event *next; } queued_event;
typedef struct transfer { Window requestor; Atom property, type; char *data; size_t length, offset; struct transfer *next; } transfer;
struct nz_connection {
    Display *display;
    int screen, wake;
    Window root, selection;
    Atom compositor_selection;
    Visual *argb_visual;
    int shape_input, root_events;
    XIM im;
    float scale;
    char error[512], *clipboard, *received;
    atomic_uint protocol_error;
    size_t received_size;
    int receiving, incremental, latin1;
    Time selection_time;
    Atom selection_time_property;
    unsigned char keys[256];
    queued_event *head, *tail, *delivered;
    nz_window *windows;
    transfer *transfers;
    nz_connection *next;
};
struct nz_window {
    nz_connection *connection;
    Window id;
    Window content;
    Colormap frame_colormap;
    int frame_margin, restore_margin, resizable;
    float frame_radii[8];
    Window resize_window, shadow_window, companion_owner;
    Colormap shadow_colormap;
    int resize_margin, resize_width, resize_height;
    int shadow_margin, shadow_width, shadow_height;
    Visual *visual;
    int depth;
    Colormap colormap;
    XIC ic;
    GC gc;
    XImage *image;
    unsigned char *pixels;
    int width, height, stride, image_width, image_height, state, visible, custom, borderless, current;
    int pixel_width, pixel_height;
    unsigned long functions;
    int above;
    nz_window *popup_parent;
    nz_popup_placement popup_placement;
    int popup, popup_grabbed, popup_positioned, popup_root_x, popup_root_y, popup_pixel_width, popup_pixel_height;
    nz_window *next;
};
static void popup_position(nz_window *w);
static void resize_content(nz_window *w, int width, int height);
static int initialized;
static pthread_mutex_t connections_lock = PTHREAD_MUTEX_INITIALIZER;
static nz_connection *connections;
static XErrorHandler previous_error;
static Atom atom(nz_connection *c, const char *name) { return XInternAtom(c->display,name,False); }
static int client_pixels(nz_connection *c, int logical) { return (int)fmin(MAX_CLIENT_PIXELS,fmax(1,lroundf(logical*c->scale))); }
static int x_error(Display *display, XErrorEvent *error) {
    pthread_mutex_lock(&connections_lock);
    nz_connection *c=connections;
    while (c && c->display!=display) c=c->next;
    if (c && error->error_code!=BadWindow && error->error_code!=BadDrawable && !(error->error_code==BadMatch && error->request_code==42))
        atomic_store_explicit(&c->protocol_error,(unsigned)error->error_code|((unsigned)error->request_code<<8),memory_order_release);
    pthread_mutex_unlock(&connections_lock);
    return !c && previous_error ? previous_error(display,error) : 0;
}
static void initialize(void) { XInitThreads(); XrmInitialize(); previous_error=XSetErrorHandler(x_error); }
static void emit(nz_connection *c, nz_window *w, uint32_t type, uint32_t flags, double x, double y, int a, int b, int d, const char *text) {
    queued_event *e=calloc(1,sizeof(*e));
    if (!e) { snprintf(c->error,sizeof(c->error),"Cannot allocate an X11 event."); return; }
    if (text && !(e->text=strdup(text))) { free(e); snprintf(c->error,sizeof(c->error),"Cannot allocate X11 event text."); return; }
    e->event=(nz_event){.type=type,.flags=flags,.window=w,.x=x,.y=y,.a=a,.b=b,.c=d,.text=e->text};
    if (c->tail) c->tail->next=e; else c->head=e;
    c->tail=e;
}
static uint32_t modifiers(unsigned state) { return (state&ShiftMask?1u:0u)|(state&ControlMask?2u:0u)|(state&Mod1Mask?4u:0u)|(state&Mod4Mask?8u:0u); }
static void pointer_event(nz_window *w, uint32_t type, unsigned state, int x, int y, int a, int b, int d) {
    nz_connection *c=w->connection;
    int client_x=x-w->frame_margin, client_y=y-w->frame_margin;
    uint32_t flags=modifiers(state);
    if(w->frame_margin && (client_x<0 || client_y<0 || client_x>=w->pixel_width || client_y>=w->pixel_height))
        flags|=NZ_POINTER_FRAME_OUTSET;
    emit(c,w,type,flags,client_x/c->scale,client_y/c->scale,a,b,d,NULL);
}
static int companion_event(XEvent *event, Window root) {
    switch(event->type) {
        case ConfigureNotify: return event->xconfigure.event==root;
        case CirculateNotify: return event->xcirculate.event==root;
        case MapNotify: return event->xmap.event==root;
        case UnmapNotify: return event->xunmap.event==root;
        case ReparentNotify: return event->xreparent.event==root;
        default: return 0;
    }
}
static int companion_restack_needed(nz_connection *c, XEvent *event, Window id) {
    if(event->type!=ConfigureNotify) return 1;
    Window above=event->xconfigure.above;
    for(nz_window *w=c->windows;w;w=w->next) if(w->resize_window || w->shadow_window) {
        if(id==w->companion_owner || (above && (above==w->companion_owner || above==w->shadow_window || above==w->resize_window))) return 1;
    }
    return 0;
}
static Window companion_window(nz_window *w, int depth, int type, Visual *visual, Colormap colormap, long events) {
    nz_connection *c=w->connection;
    if(!c->root_events) {
        XSelectInput(c->display,c->root,SubstructureNotifyMask|PropertyChangeMask);
        c->root_events=1;
    }
    XSetWindowAttributes attributes={.override_redirect=True,.event_mask=events,.colormap=colormap,.border_pixel=0};
    unsigned long mask=CWOverrideRedirect|CWEventMask|(colormap ? CWColormap|CWBorderPixel:0);
    return XCreateWindow(c->display,c->root,0,0,1,1,0,depth,type,visual,mask,&attributes);
}
static Window root_child(Display *display, Window root, Window window) {
    for(int levels=0; levels<16; levels++) {
        Window parent, actual_root, *children=NULL;
        unsigned count;
        if(!XQueryTree(display,window,&actual_root,&parent,&children,&count)) return None;
        if(children) XFree(children);
        if(parent==root) return window;
        if(parent==None) return None;
        window=parent;
    }
    return None;
}
static void stack_companion(Display *display, Window window, Window sibling, int mode, Window *children, unsigned count) {
    Window upper=mode==Above?window:sibling, lower=mode==Above?sibling:window;
    for(unsigned i=1;i<count;i++) if(children[i]==upper && children[i-1]==lower) return;
    XWindowChanges changes={.sibling=sibling,.stack_mode=mode};
    XConfigureWindow(display,window,CWSibling|CWStackMode,&changes);
}
static void position_companion(Display *display, Window window, int x, int y, int width, int height) {
    XWindowAttributes attributes;
    if(!XGetWindowAttributes(display,window,&attributes)) return;
    if(attributes.x!=x || attributes.y!=y || attributes.width!=width || attributes.height!=height)
        XMoveResizeWindow(display,window,x,y,(unsigned)width,(unsigned)height);
    if(attributes.map_state!=IsViewable) XMapWindow(display,window);
}
static void sync_companions(nz_window *w) {
    if(!w->resize_window && !w->shadow_window) return;
    nz_connection *c=w->connection;
    Display *display=c->display;
    XLockDisplay(display);
    XGrabServer(display);
    Window sibling=None, ignored, *children=NULL;
    unsigned count=0;
    XWindowAttributes attributes;
    int x,y;
    if(w->visible && (w->custom || w->borderless) && w->state==NZ_STATE_NORMAL &&
        XGetWindowAttributes(display,w->id,&attributes) && attributes.map_state==IsViewable &&
        XTranslateCoordinates(display,w->id,c->root,0,0,&x,&y,&ignored))
        sibling=root_child(display,c->root,w->id);
    if(sibling!=None && !XQueryTree(display,c->root,&ignored,&ignored,&children,&count)) sibling=None;
    w->companion_owner=sibling;
    if(sibling==None) {
        if(w->resize_window) XUnmapWindow(display,w->resize_window);
        if(w->shadow_window) XUnmapWindow(display,w->shadow_window);
    } else {
        if(w->shadow_window) {
            int margin=w->shadow_margin;
            position_companion(display,w->shadow_window,x+w->frame_margin-margin,y+w->frame_margin-margin,w->shadow_width,w->shadow_height);
            stack_companion(display,w->shadow_window,sibling,Below,children,count);
        }
        if(w->resize_window && attributes.width+w->resize_margin<32768 && attributes.height+w->resize_margin<32768) {
            int margin=w->resize_margin, width=attributes.width, height=attributes.height;
            if(width!=w->resize_width || height!=w->resize_height) {
                XRectangle strips[]={
                    {0,0,(unsigned short)(width+2*margin),(unsigned short)margin},
                    {0,(short)margin,(unsigned short)margin,(unsigned short)height},
                    {(short)(width+margin),(short)margin,(unsigned short)margin,(unsigned short)height},
                    {0,(short)(height+margin),(unsigned short)(width+2*margin),(unsigned short)margin},
                };
                XShapeCombineRectangles(display,w->resize_window,ShapeInput,0,0,strips,4,ShapeSet,Unsorted);
                w->resize_width=width; w->resize_height=height;
            }
            position_companion(display,w->resize_window,x-margin,y-margin,width+2*margin,height+2*margin);
            stack_companion(display,w->resize_window,sibling,Above,children,count);
        } else if(w->resize_window) XUnmapWindow(display,w->resize_window);
    }
    if(children) XFree(children);
    XUngrabServer(display);
    XFlush(display);
    XUnlockDisplay(display);
}
static int property_has(nz_connection *c, Window window, const char *property, Atom value) {
    Atom actual; int format; unsigned long count, remaining; unsigned char *data=NULL; int found=0;
    if (XGetWindowProperty(c->display,window,atom(c,property),0,4096,False,XA_ATOM,&actual,&format,&count,&remaining,&data)==Success && format==32) {
        for (unsigned long i=0;i<count;i++) if (((Atom*)data)[i]==value) found=1;
    }
    if (data) XFree(data);
    return found;
}
static int weston_manager(nz_connection *c) {
    Atom actual; int format; unsigned long count, remaining; unsigned char *data=NULL;
    Window manager=None;
    if(XGetWindowProperty(c->display,c->root,atom(c,"_NET_SUPPORTING_WM_CHECK"),0,1,False,XA_WINDOW,&actual,&format,&count,&remaining,&data)==Success && actual==XA_WINDOW && format==32 && count==1)
        manager=((Window*)data)[0];
    if(data) XFree(data);
    if(!manager) return 0;
    data=NULL;
    int weston=XGetWindowProperty(c->display,manager,atom(c,"_NET_WM_NAME"),0,64,False,atom(c,"UTF8_STRING"),&actual,&format,&count,&remaining,&data)==Success &&
        format==8 && count==9 && data && !memcmp(data,"Weston WM",9);
    if(data) XFree(data);
    return weston;
}
int nz_decoration_support(nz_connection *c) {
    if(!c) return 0;
    Atom actual; int format; unsigned long count, remaining; unsigned char *data=NULL;
    int found=XGetWindowProperty(c->display,c->root,atom(c,"_NET_SUPPORTING_WM_CHECK"),0,1,False,XA_WINDOW,&actual,&format,&count,&remaining,&data)==Success && actual==XA_WINDOW && format==32 && count==1;
    if(data) XFree(data);
    return found ? 1 | (weston_manager(c) ? 2 : 0) : 0;
}
static int weston_shadow_frame(nz_window *w, const unsigned long *edges) {
    nz_connection *c=w->connection;
    if(!weston_manager(c)) return 0;
    Window root,parent,*children=NULL; unsigned child_count;
    if(!XQueryTree(c->display,w->id,&root,&parent,&children,&child_count)) return 0;
    if(children) XFree(children);
    if(parent==None || parent==c->root) return 0;
    XWindowAttributes client,frame;
    if(!XGetWindowAttributes(c->display,w->id,&client) || client.map_state!=IsViewable ||
       !XGetWindowAttributes(c->display,parent,&frame)) return 0;
    int margin=client.x;
    return margin>=0 && client.y==margin && client.border_width==0 &&
        frame.width-client.width==2*margin && frame.height-client.height==2*margin &&
        (unsigned long)margin<edges[0] && (unsigned long)margin<edges[1] &&
        (unsigned long)margin<edges[2] && (unsigned long)margin<edges[3];
}
static int mapped_without_frame(nz_window *w) {
    nz_connection *c=w->connection;
    XWindowAttributes client,frame;
    if(!XGetWindowAttributes(c->display,w->id,&client) || client.map_state!=IsViewable || client.border_width!=0) return 0;
    Atom state=atom(c,"WM_STATE"),actual; int format; unsigned long count,remaining; unsigned char *data=NULL;
    int managed=XGetWindowProperty(c->display,w->id,state,0,2,False,state,&actual,&format,&count,&remaining,&data)==Success &&
        actual==state && format==32 && count>=1 && ((long*)data)[0]==NormalState;
    if(data) XFree(data);
    if(!managed) return 0;
    Window current=w->id;
    for(int depth=0;depth<32;depth++) {
        Window root,parent,*children=NULL; unsigned child_count;
        if(!XQueryTree(c->display,current,&root,&parent,&children,&child_count)) return 0;
        if(children) XFree(children);
        if(parent==c->root) return 1;
        if(parent==None || client.x!=0 || client.y!=0 || !XGetWindowAttributes(c->display,parent,&frame) ||
           frame.border_width!=0 || frame.width!=client.width || frame.height!=client.height) return 0;
        current=parent;
        client=frame;
    }
    return 0;
}
int nz_window_decoration_mode(nz_window *w) {
    if(!w) return 0;
    nz_connection *c=w->connection;
    Atom actual; int format; unsigned long count, remaining; unsigned char *data=NULL; int mode=0;
    if(XGetWindowProperty(c->display,w->id,atom(c,"_NET_FRAME_EXTENTS"),0,4,False,XA_CARDINAL,&actual,&format,&count,&remaining,&data)==Success && actual==XA_CARDINAL && format==32 && count==4 && !remaining) {
        unsigned long *edges=(unsigned long*)data;
        mode=(edges[0] || edges[1] || edges[2] || edges[3]) ? 2:1;
        if(mode==2 && weston_shadow_frame(w,edges)) mode=1;
    }
    if(data) XFree(data);
    if(!mode && (!nz_decoration_support(c) || mapped_without_frame(w))) mode=1;
    return mode;
}
static void emit_configure(nz_window *w) {
    emit(w->connection,w,NZ_CONFIGURE,0,w->pixel_width,w->pixel_height,w->width,w->height,w->state,NULL);
}
static int update_size(nz_window *w, int width, int height) {
    resize_content(w,width,height);
    int pixel_width=(int)fmax(1,width-2*w->frame_margin), pixel_height=(int)fmax(1,height-2*w->frame_margin);
    int logical_width=(int)fmax(1,lroundf(pixel_width/w->connection->scale));
    int logical_height=(int)fmax(1,lroundf(pixel_height/w->connection->scale));
    int changed=pixel_width!=w->pixel_width || pixel_height!=w->pixel_height || logical_width!=w->width || logical_height!=w->height;
    w->pixel_width=pixel_width; w->pixel_height=pixel_height;
    w->width=logical_width; w->height=logical_height;
    return changed;
}
static void configure(nz_window *w) {
    nz_connection *c=w->connection;
    Atom wm_state=atom(c,"WM_STATE"), actual;
    int format; unsigned long count, remaining; unsigned char *data=NULL;
    int minimized=XGetWindowProperty(c->display,w->id,wm_state,0,1,False,wm_state,&actual,&format,&count,&remaining,&data)==Success &&
        format==32 && count>0 && ((long*)data)[0]==IconicState;
    if(data) XFree(data);
    int fullscreen=property_has(c,w->id,"_NET_WM_STATE",atom(c,"_NET_WM_STATE_FULLSCREEN"));
    int maximized=property_has(c,w->id,"_NET_WM_STATE",atom(c,"_NET_WM_STATE_MAXIMIZED_VERT")) && property_has(c,w->id,"_NET_WM_STATE",atom(c,"_NET_WM_STATE_MAXIMIZED_HORZ"));
    int previous=w->state;
    w->state=minimized ? NZ_STATE_MINIMIZED : fullscreen ? NZ_STATE_FULLSCREEN : maximized ? NZ_STATE_MAXIMIZED : NZ_STATE_NORMAL;
    if(previous==NZ_STATE_NORMAL && w->state!=NZ_STATE_NORMAL) w->restore_margin=w->frame_margin;
    if(previous!=NZ_STATE_NORMAL && w->state==NZ_STATE_NORMAL && w->restore_margin) {
        w->frame_margin=w->restore_margin;
        w->restore_margin=0;
        unsigned long margin=(unsigned long)w->frame_margin, extents[]={margin,margin,margin,margin};
        XChangeProperty(c->display,w->id,atom(c,"_GTK_FRAME_EXTENTS"),XA_CARDINAL,32,PropModeReplace,(unsigned char*)extents,4);
    }
    XWindowAttributes attributes;
    if(XGetWindowAttributes(c->display,w->id,&attributes)) update_size(w,attributes.width,attributes.height);
    if(previous!=NZ_STATE_NORMAL && w->state==NZ_STATE_NORMAL && w->frame_margin) nz_window_operations(w,w->resizable,w->width,w->height);
    emit_configure(w);
}
static void send_message(nz_window *w, const char *name, long a, long b, long d, long e, long f) {
    nz_connection *c=w->connection;
    XEvent event={0}; event.xclient=(XClientMessageEvent){.type=ClientMessage,.display=c->display,.window=w->id,.message_type=atom(c,name),.format=32};
    event.xclient.data.l[0]=a; event.xclient.data.l[1]=b; event.xclient.data.l[2]=d; event.xclient.data.l[3]=e; event.xclient.data.l[4]=f;
    XSendEvent(c->display,c->root,False,SubstructureRedirectMask|SubstructureNotifyMask,&event); XFlush(c->display);
}
static void read_scale(nz_connection *c) {
    c->scale=1;
    char *resources=XResourceManagerString(c->display);
    if (!resources) return;
    XrmDatabase db=XrmGetStringDatabase(resources);
    if (!db) return;
    char *type=NULL; XrmValue value;
    if (XrmGetResource(db,"Xft.dpi","Xft.Dpi",&type,&value) && value.addr) {
        char *end; double dpi=strtod(value.addr,&end);
        if (end!=value.addr && dpi>=48 && dpi<=768) c->scale=(float)(dpi/96);
    }
    XrmDestroyDatabase(db);
}
static Visual *find_argb_visual(nz_connection *c) {
    XRenderPictFormat *argb=XRenderFindStandardFormat(c->display,PictStandardARGB32);
    if (!argb) return NULL;
    XVisualInfo match={.screen=c->screen,.depth=32,.class=TrueColor};
    int count;
    XVisualInfo *visuals=XGetVisualInfo(c->display,VisualScreenMask|VisualDepthMask|VisualClassMask,&match,&count);
    Visual *visual=NULL;
    for (int i=0; visuals && i<count; i++) {
        XRenderPictFormat *format=XRenderFindVisualFormat(c->display,visuals[i].visual);
        if (format && format->id==argb->id) { visual=visuals[i].visual; break; }
    }
    if (visuals) XFree(visuals);
    return visual;
}
static void input_method_destroyed(XIM im, XPointer data, XPointer unused) {
    nz_connection *c=(nz_connection*)data;
    c->im=NULL;
    for(nz_window *w=c->windows;w;w=w->next) w->ic=NULL;
}
uint32_t nz_abi_version(void) { return 5; }
size_t nz_event_size(void) { return sizeof(nz_event); }
nz_connection *nz_connect(char *error, size_t size) {
    pthread_mutex_lock(&connections_lock);
    if(!initialized) { initialize(); initialized=1; }
    pthread_mutex_unlock(&connections_lock);
    nz_connection *c=calloc(1,sizeof(*c));
    if (!c) { if (size) snprintf(error,size,"Cannot allocate an X11 connection."); return NULL; }
    c->wake=-1;
    c->display=XOpenDisplay(NULL);
    if (!c->display) { if(size) snprintf(error,size,"Cannot connect to the X11 display selected by DISPLAY."); free(c); return NULL; }
    pthread_mutex_lock(&connections_lock); c->next=connections; connections=c; pthread_mutex_unlock(&connections_lock);
    c->screen=DefaultScreen(c->display); c->root=RootWindow(c->display,c->screen);
    char selection_name[32];
    snprintf(selection_name,sizeof(selection_name),"_NET_WM_CM_S%d",c->screen);
    c->compositor_selection=atom(c,selection_name);
    c->argb_visual=find_argb_visual(c);
    int shape_event, shape_error, shape_major, shape_minor;
    c->shape_input=XShapeQueryExtension(c->display,&shape_event,&shape_error) &&
        XShapeQueryVersion(c->display,&shape_major,&shape_minor) && (shape_major>1 || (shape_major==1 && shape_minor>=1));
    c->wake=eventfd(0,EFD_CLOEXEC|EFD_NONBLOCK);
    if (c->wake<0) { if(size) snprintf(error,size,"Cannot allocate the X11 wake descriptor."); nz_disconnect(c); return NULL; }
    c->selection=XCreateSimpleWindow(c->display,c->root,0,0,1,1,0,0,0);
    c->selection_time_property=atom(c,"_NAZOR_SELECTION_TIME");
    XSelectInput(c->display,c->selection,PropertyChangeMask);
    XSelectInput(c->display,c->root,SubstructureNotifyMask|PropertyChangeMask);
    c->root_events=1;
    XSetLocaleModifiers(""); c->im=XOpenIM(c->display,NULL,NULL,NULL);
    if (!c->im) { XSetLocaleModifiers("@im=none"); c->im=XOpenIM(c->display,NULL,NULL,NULL); }
    if(c->im) {
        XIMCallback destroy={.client_data=(XPointer)c,.callback=input_method_destroyed};
        XSetIMValues(c->im,XNDestroyCallback,&destroy,NULL);
    }
    read_scale(c);
    XkbSetDetectableAutoRepeat(c->display,True,NULL);
    emit(c,NULL,NZ_REPEAT,0,0,0,0,0,0,NULL);
    return c;
}
const char *nz_error(nz_connection *c) { return c?c->error:"No X11 connection."; }
void nz_wake(nz_connection *c) { if (c && c->wake>=0) { uint64_t one=1; ssize_t result; do { result=write(c->wake,&one,sizeof(one)); } while(result<0 && errno==EINTR); } }
int nz_next_event(nz_connection *c, nz_event *event) {
    if (!c || !event) return 0;
    if (c->delivered) { free(c->delivered->text); free(c->delivered); c->delivered=NULL; }
    if (!c->head) return 0;
    c->delivered=c->head; c->head=c->head->next;
    if(!c->head) c->tail=NULL;
    *event=c->delivered->event;
    return 1;
}
static void clipboard_result(nz_connection *c, const char *value) { emit(c,NULL,NZ_CLIPBOARD,0,0,0,0,0,0,value); c->receiving=c->incremental=0; free(c->received); c->received=NULL; c->received_size=0; }
static void receive_selection(nz_connection *c, int incremental) {
    Atom actual; int format; unsigned long count, remaining; unsigned char *data=NULL; Atom property=atom(c,"_NAZOR_CLIPBOARD");
    if (XGetWindowProperty(c->display,c->selection,property,0,CLIP_LIMIT/4,True,AnyPropertyType,&actual,&format,&count,&remaining,&data)!=Success || remaining) { if(data) XFree(data); clipboard_result(c,NULL); return; }
    if (actual==atom(c,"INCR") && !incremental) c->incremental=1;
    else if (format==8 && (actual==atom(c,"UTF8_STRING") || actual==XA_STRING)) {
        size_t bytes=count;
        if(actual==XA_STRING) for(unsigned long i=0;i<count;i++) if(data[i]>=128) bytes++;
        if (c->received_size+bytes>CLIP_LIMIT) { if(data) XFree(data); clipboard_result(c,NULL); return; }
        char *received=realloc(c->received,c->received_size+bytes+1);
        if (!received) { if(data) XFree(data); clipboard_result(c,NULL); return; }
        c->received=received;
        if(actual==XA_STRING) { for(unsigned long i=0;i<count;i++) { if(data[i]>=128) received[c->received_size++]=(char)(0xc0|(data[i]>>6)); received[c->received_size++]=(char)(data[i]>=128?0x80|(data[i]&63):data[i]); } }
        else { memcpy(received+c->received_size,data,count); c->received_size+=count; }
        received[c->received_size]=0;
        if (!incremental || !count) clipboard_result(c,received);
    } else clipboard_result(c,NULL);
    if(data) XFree(data);
}
static void selection_request(nz_connection *c, XSelectionRequestEvent *request) {
    XEvent reply={0}; reply.xselection=(XSelectionEvent){.type=SelectionNotify,.display=c->display,.requestor=request->requestor,.selection=request->selection,.target=request->target,.time=request->time,.property=None};
    Atom property=request->property?request->property:request->target;
    if (request->selection==atom(c,"CLIPBOARD") && c->clipboard) {
        if (request->target==atom(c,"TARGETS")) {
            Atom targets[]={atom(c,"TARGETS"),atom(c,"UTF8_STRING"),XA_STRING,atom(c,"TIMESTAMP")};
            XChangeProperty(c->display,request->requestor,property,XA_ATOM,32,PropModeReplace,(unsigned char*)targets,4); reply.xselection.property=property;
        } else if(request->target==atom(c,"TIMESTAMP")) {
            unsigned long timestamp=c->selection_time;
            XChangeProperty(c->display,request->requestor,property,XA_INTEGER,32,PropModeReplace,(unsigned char*)&timestamp,1); reply.xselection.property=property;
        } else if (request->target==atom(c,"UTF8_STRING") || request->target==XA_STRING) {
            char *converted=NULL; const char *text=c->clipboard;
            if(request->target==XA_STRING) {
                converted=malloc(strlen(text)+1);
                if(!converted) goto selection_reply;
                const unsigned char *input=(const unsigned char*)text; unsigned char *out=(unsigned char*)converted;
                while(*input) { unsigned value=*input++; if(value<128) *out++=(unsigned char)value; else if((value==0xc2 || value==0xc3) && (*input&0xc0)==0x80) *out++=(unsigned char)(((value&3)<<6)|(*input++&63)); else { while((*input&0xc0)==0x80) input++; *out++='?'; } }
                *out=0; text=converted;
            }
            size_t length=strlen(text);
            if (length<=CLIP_CHUNK) { XChangeProperty(c->display,request->requestor,property,request->target,8,PropModeReplace,(const unsigned char*)text,(int)length); reply.xselection.property=property; }
            else {
                int transfers=0; for(transfer *active=c->transfers;active;active=active->next) transfers++;
                if(transfers>=8) { free(converted); goto selection_reply; }
                transfer *t=calloc(1,sizeof(*t));
                if(t && (t->data=strdup(text))) {
                    t->requestor=request->requestor; t->property=property; t->type=request->target; t->length=length; t->next=c->transfers; c->transfers=t;
                    XSelectInput(c->display,t->requestor,PropertyChangeMask|StructureNotifyMask);
                    unsigned long amount=length; XChangeProperty(c->display,t->requestor,property,atom(c,"INCR"),32,PropModeReplace,(unsigned char*)&amount,1); reply.xselection.property=property;
                } else free(t);
            }
            free(converted);
        }
    }
selection_reply:
    XSendEvent(c->display,request->requestor,False,0,&reply);
}
static void advance_transfer(nz_connection *c, XEvent *event) {
    transfer **p=&c->transfers;
    while (*p) {
        transfer *t=*p;
        if (event->type==DestroyNotify && event->xdestroywindow.window==t->requestor) { *p=t->next; free(t->data); free(t); continue; }
        if (event->type==PropertyNotify && event->xproperty.window==t->requestor && event->xproperty.atom==t->property && event->xproperty.state==PropertyDelete) {
            size_t length=t->length-t->offset; if(length>CLIP_CHUNK) length=CLIP_CHUNK;
            XChangeProperty(c->display,t->requestor,t->property,t->type,8,PropModeReplace,(unsigned char*)t->data+t->offset,(int)length); t->offset+=length;
            if (!length) { *p=t->next; free(t->data); free(t); continue; }
        }
        p=&(*p)->next;
    }
}
static void keyboard(nz_connection *c, nz_window *w, XKeyEvent *key, int down) {
    KeySym symbol=NoSymbol;
    int composition=key->keycode==0;
    if(!composition) {
        unsigned consumed=0;
        XkbLookupKeySym(c->display,(KeyCode)key->keycode,key->state,&consumed,&symbol);
        char *name=XKeysymToString(symbol);
        int repeat=down && c->keys[key->keycode&255]; c->keys[key->keycode&255]=(unsigned char)down;
        emit(c,w,NZ_KEY,modifiers(key->state),0,0,down,(int)key->keycode,0,name?name:"");
        if(c->tail) c->tail->event.d=repeat;
    }
    if (!down) return;
    Status status; int length=0; char buffer[128], latin1_utf8[2*sizeof(buffer)], *text=buffer;
    if (w->ic) {
        length=Xutf8LookupString(w->ic,key,buffer,sizeof(buffer)-1,&symbol,&status);
        if (status==XBufferOverflow && length>0 && length<1024*1024) {
            text=malloc((size_t)length+1); if(!text) return;
            length=Xutf8LookupString(w->ic,key,text,length,&symbol,&status);
        }
        if(status!=XLookupChars && status!=XLookupBoth) length=0;
    } else if(!composition) {
        int bytes=XLookupString(key,buffer,sizeof(buffer)-1,&symbol,NULL);
        for(int i=0;i<bytes;i++) {
            unsigned char character=(unsigned char)buffer[i];
            if(character>=128) latin1_utf8[length++]=(char)(0xc0|(character>>6));
            latin1_utf8[length++]=(char)(character>=128?0x80|(character&63):character);
        }
        text=latin1_utf8;
    }
    if(length>0 && (composition || ((unsigned char)text[0]>=32 && text[0]!=127 && !(key->state&ControlMask)))) {
        text[length]=0;
        emit(c,w,NZ_TEXT,composition?1u:0u,0,0,0,0,0,text);
    }
    if(text!=buffer && text!=latin1_utf8) free(text);
}
int nz_dispatch(nz_connection *c, int timeout) {
    if(!c) return -1;
    unsigned protocol_error=atomic_load_explicit(&c->protocol_error,memory_order_acquire);
    if(protocol_error) { snprintf(c->error,sizeof(c->error),"X11 protocol error %u (request %u).",protocol_error&255,protocol_error>>8); return -1; }
    if(c->error[0]) return -1;
    if(!c->head && !XPending(c->display)) {
        struct pollfd descriptors[]={{ConnectionNumber(c->display),POLLIN,0},{c->wake,POLLIN,0}};
        int result; do { result=poll(descriptors,2,timeout); } while(result<0 && errno==EINTR);
        if(result<0 || (descriptors[0].revents&(POLLERR|POLLHUP|POLLNVAL))) { snprintf(c->error,sizeof(c->error),"The X11 display connection was lost."); return -1; }
        if(descriptors[1].revents&POLLIN) { uint64_t value; ssize_t count; do { count=read(c->wake,&value,sizeof(value)); } while(count<0 && errno==EINTR); }
    }
    int budget=4096, restack=0;
    while(budget-- && XPending(c->display)) {
        XEvent event; XNextEvent(c->display,&event);
        if(XFilterEvent(&event,None)) continue;
        if(event.type==MappingNotify) { XRefreshKeyboardMapping(&event.xmapping); continue; }
        advance_transfer(c,&event);
        if(event.type==SelectionRequest) { selection_request(c,&event.xselectionrequest); continue; }
        if(event.type==SelectionClear && event.xselectionclear.window==c->selection) {
            if(XGetSelectionOwner(c->display,event.xselectionclear.selection)!=c->selection) {
                free(c->clipboard); c->clipboard=NULL;
            }
            continue;
        }
        if(event.type==SelectionNotify && c->receiving && event.xselection.requestor==c->selection) {
            if(event.xselection.property==None && !c->latin1) { c->latin1=1; XConvertSelection(c->display,atom(c,"CLIPBOARD"),XA_STRING,atom(c,"_NAZOR_CLIPBOARD"),c->selection,CurrentTime); }
            else if(event.xselection.property==None) clipboard_result(c,NULL); else receive_selection(c,0);
            continue;
        }
        if(event.type==PropertyNotify && c->receiving && c->incremental && event.xproperty.window==c->selection && event.xproperty.atom==atom(c,"_NAZOR_CLIPBOARD") && event.xproperty.state==PropertyNewValue) { receive_selection(c,1); continue; }
        if(event.type==PropertyNotify && event.xproperty.window==c->root && event.xproperty.atom==atom(c,"_NET_ACTIVE_WINDOW")) {
            Atom type; int format; unsigned long count,remaining; unsigned char *data=NULL;
            Window active=None;
            if(XGetWindowProperty(c->display,c->root,event.xproperty.atom,0,1,False,XA_WINDOW,&type,&format,&count,&remaining,&data)==Success && format==32 && count==1)
                active=*(Window*)data;
            if(data) XFree(data);
            for(nz_window *owner=c->windows;owner;owner=owner->next)
                if(!owner->popup) emit(c,owner,NZ_WINDOW_ACTIVATION,0,0,0,owner->id==active,0,0,NULL);
            continue;
        }
        Window id=event.type==ConfigureNotify?event.xconfigure.window:event.type==MapNotify?event.xmap.window:event.type==UnmapNotify?event.xunmap.window:
            event.type==ReparentNotify?event.xreparent.window:event.type==CirculateNotify?event.xcirculate.window:event.xany.window;
        nz_window *w=c->windows; while(w && w->id!=id && w->content!=id && w->resize_window!=id && w->shadow_window!=id) w=w->next;
        if(c->root_events && companion_event(&event,c->root)) {
            if((!w || w->id==id) && companion_restack_needed(c,&event,id)) restack=1;
            continue;
        }
        if(!w) continue;
        if(w->content==id) {
            if(event.type==Expose && !event.xexpose.count) emit(c,w,20,0,0,0,0,0,0,NULL);
            continue;
        }
        if(w->resize_window==id) {
            int margin=w->resize_margin;
            if(event.type==EnterNotify || event.type==LeaveNotify)
                emit(c,w,event.type==EnterNotify?NZ_POINTER_ENTER:NZ_POINTER_LEAVE,modifiers(event.xcrossing.state)|NZ_POINTER_FRAME_OUTSET,
                    (event.xcrossing.x-margin)/c->scale,(event.xcrossing.y-margin)/c->scale,0,0,0,NULL);
            else if(event.type==MotionNotify)
                emit(c,w,NZ_POINTER_MOTION,modifiers(event.xmotion.state)|NZ_POINTER_FRAME_OUTSET,
                    (event.xmotion.x-margin)/c->scale,(event.xmotion.y-margin)/c->scale,0,0,0,NULL);
            else if((event.type==ButtonPress || event.type==ButtonRelease) && event.xbutton.button==1)
                emit(c,w,NZ_POINTER_BUTTON,modifiers(event.xbutton.state)|NZ_POINTER_FRAME_OUTSET,
                    (event.xbutton.x-margin)/c->scale,(event.xbutton.y-margin)/c->scale,272,event.type==ButtonPress,(int)event.xbutton.time,NULL);
            continue;
        }
        if(w->id!=id) continue;
        switch(event.type) {
            case ConfigureNotify: {
                emit(c,w,NZ_CAPABILITIES,0,0,0,0,0,0,NULL);
                if (!w->popup) {
                    int has_popups=0;
                    for(nz_window *child=c->windows;child;child=child->next)
                        if(child->popup) { popup_position(child); has_popups=1; }
                    if(has_popups) {
                        Window root,child; int rx,ry,x,y; unsigned mask=0;
                        XQueryPointer(c->display,w->id,&root,&child,&rx,&ry,&x,&y,&mask);
                        emit(c,w,NZ_POPUP_OWNER_MOVED,0,0,0,(mask&Button1Mask)!=0,0,0,NULL);
                    }
                }
                if(w->resize_window || w->shadow_window) restack=1;
                if(update_size(w,event.xconfigure.width,event.xconfigure.height)) emit_configure(w);
                break;
            }
            case MapNotify: w->visible=1; configure(w); emit(c,w,NZ_CAPABILITIES,0,0,0,0,0,0,NULL); if(w->resize_window || w->shadow_window) restack=1; break;
            case ReparentNotify: emit(c,w,20,0,0,0,1,0,0,NULL); if(w->resize_window || w->shadow_window) restack=1; break;
            case UnmapNotify: if(w->visible) { if(w->state==NZ_STATE_NORMAL) w->restore_margin=w->frame_margin; w->state=NZ_STATE_MINIMIZED; emit_configure(w); } if(w->resize_window || w->shadow_window) restack=1; break;
            case PropertyNotify:
                if(event.xproperty.atom==atom(c,"_NET_FRAME_EXTENTS") || event.xproperty.atom==atom(c,"WM_STATE")) emit(c,w,NZ_CAPABILITIES,0,0,0,0,0,0,NULL);
                if(event.xproperty.atom==atom(c,"_NET_WM_STATE")) {
                    int above=property_has(c,w->id,"_NET_WM_STATE",atom(c,"_NET_WM_STATE_ABOVE"));
                    if(w->above!=above) {
                        w->above=above;
                        emit(c,w,NZ_ALWAYS_ON_TOP,0,0,0,above,0,0,NULL);
                    }
                }
                if(w->visible && (event.xproperty.atom==atom(c,"_NET_WM_STATE") || event.xproperty.atom==atom(c,"WM_STATE"))) {
                    configure(w);
                    if(w->resize_window || w->shadow_window) restack=1;
                }
                break;
            case Expose: if(!event.xexpose.count) emit(c,w,20,0,0,0,0,0,0,NULL); break;
            case ClientMessage:
                if(event.xclient.message_type==atom(c,"WM_PROTOCOLS") && (Atom)event.xclient.data.l[0]==atom(c,"WM_DELETE_WINDOW")) emit(c,w,NZ_CLOSE,0,0,0,0,0,0,NULL);
                else if(event.xclient.message_type==atom(c,"WM_PROTOCOLS") && (Atom)event.xclient.data.l[0]==atom(c,"WM_TAKE_FOCUS")) {
                    XWindowAttributes attributes;
                    if(XGetWindowAttributes(c->display,w->id,&attributes) && attributes.map_state==IsViewable) XSetInputFocus(c->display,w->id,RevertToParent,(Time)event.xclient.data.l[1]);
                }
                break;
            case FocusIn: if(event.xfocus.mode!=NotifyNormal) break; if(w->ic) XSetICFocus(w->ic); emit(c,w,NZ_FOCUS,0,0,0,1,0,0,NULL); break;
            case FocusOut: if(event.xfocus.mode!=NotifyNormal) break; if(w->ic) XUnsetICFocus(w->ic); memset(c->keys,0,sizeof(c->keys)); emit(c,w,NZ_FOCUS,0,0,0,0,0,0,NULL); break;
            case EnterNotify: case LeaveNotify:
                if(event.type==LeaveNotify && event.xcrossing.detail==NotifyInferior) break;
                pointer_event(w,event.type==EnterNotify?NZ_POINTER_ENTER:NZ_POINTER_LEAVE,event.xcrossing.state,event.xcrossing.x,event.xcrossing.y,0,0,0); break;
            case MotionNotify: pointer_event(w,NZ_POINTER_MOTION,event.xmotion.state,event.xmotion.x,event.xmotion.y,0,0,0); break;
            case ButtonPress: case ButtonRelease: {
                XButtonEvent *b=&event.xbutton;
                if(b->button>=4 && b->button<=7) {
                    if(event.type==ButtonPress) { pointer_event(w,NZ_POINTER_AXIS,b->state,b->x,b->y,b->button>=6?1:0,0,0); if(c->tail) c->tail->event.d=(b->button==4 || b->button==6?1:-1)*48*256; }
                } else pointer_event(w,NZ_POINTER_BUTTON,b->state,b->x,b->y,b->button==1?272:b->button==3?273:b->button==2?274:0,event.type==ButtonPress,(int)b->time);
                break;
            }
            case KeyPress: keyboard(c,w,&event.xkey,1); break;
            case KeyRelease:
                if(XPending(c->display)) { XEvent next; XPeekEvent(c->display,&next); if(next.type==KeyPress && next.xkey.keycode==event.xkey.keycode && next.xkey.time==event.xkey.time) break; }
                keyboard(c,w,&event.xkey,0); break;
        }
    }
    if(restack) for(nz_window *w=c->windows;w;w=w->next) sync_companions(w);
    XFlush(c->display);
    return c->error[0]?-1:0;
}
int nz_argb_supported(nz_connection *c) {
    return c && c->argb_visual && XGetSelectionOwner(c->display,c->compositor_selection)!=None;
}
static nz_window *window_create(nz_connection *c, const char *title, const char *app_id, int width, int height, nz_window *parent, int custom, int borderless, int argb) {
    if(!c || width<1 || height<1 || width>32768 || height>32768) return NULL;
    if (argb && !nz_argb_supported(c)) return NULL;
    nz_window *w=calloc(1,sizeof(*w)); if(!w) return NULL;
    w->connection=c; w->width=width; w->height=height; w->custom=custom; w->borderless=borderless;
    w->functions=MOTIF_RESIZE|MOTIF_MOVE|MOTIF_MINIMIZE|MOTIF_MAXIMIZE|MOTIF_CLOSE;
    w->visual=argb?c->argb_visual:DefaultVisual(c->display,c->screen);
    w->depth=argb?32:DefaultDepth(c->display,c->screen);
    unsigned pixel_width=(unsigned)client_pixels(c,width), pixel_height=(unsigned)client_pixels(c,height);
    w->pixel_width=(int)pixel_width; w->pixel_height=(int)pixel_height;
    if (argb) w->colormap=XCreateColormap(c->display,c->root,w->visual,AllocNone);
    // Keep the last pixels while the next application frame is being drawn.
    XSetWindowAttributes attributes={.colormap=w->colormap,.background_pixmap=None,.bit_gravity=NorthWestGravity,.border_pixel=0};
    unsigned long mask=CWBackPixmap|CWBitGravity|CWBorderPixel;
    if(argb) mask|=CWColormap;
    if(nz_argb_supported(c)) {
        w->frame_colormap=XCreateColormap(c->display,c->root,c->argb_visual,AllocNone);
        XSetWindowAttributes frame_attributes={.colormap=w->frame_colormap,.background_pixel=0,.border_pixel=0};
        w->id=XCreateWindow(c->display,c->root,0,0,pixel_width,pixel_height,0,32,InputOutput,c->argb_visual,CWColormap|CWBackPixel|CWBorderPixel,&frame_attributes);
        attributes.colormap=argb?w->colormap:DefaultColormap(c->display,c->screen);
        mask|=CWColormap;
        w->content=XCreateWindow(c->display,w->id,0,0,pixel_width,pixel_height,0,w->depth,InputOutput,w->visual,mask,&attributes);
        XSelectInput(c->display,w->content,ExposureMask);
        XMapWindow(c->display,w->content);
    } else w->id=XCreateWindow(c->display,c->root,0,0,pixel_width,pixel_height,0,w->depth,InputOutput,w->visual,mask,&attributes);
    w->gc=XCreateGC(c->display,w->content?w->content:w->id,0,NULL);
    w->next=c->windows; c->windows=w;
    long events=StructureNotifyMask|ExposureMask|PropertyChangeMask|FocusChangeMask|KeyPressMask|KeyReleaseMask|
        ButtonPressMask|ButtonReleaseMask|PointerMotionMask|EnterWindowMask|LeaveWindowMask;
    if(c->im) {
        w->ic=XCreateIC(c->im,XNInputStyle,XIMPreeditNothing|XIMStatusNothing,XNClientWindow,w->id,XNFocusWindow,w->id,NULL);
        long filter_events=0;
        if(w->ic && !XGetICValues(w->ic,XNFilterEvents,&filter_events,NULL)) events|=filter_events;
    }
    XSelectInput(c->display,w->id,events);
    Atom protocols[]={atom(c,"WM_DELETE_WINDOW"),atom(c,"WM_TAKE_FOCUS")}; XSetWMProtocols(c->display,w->id,protocols,2);
    XClassHint class_hint={(char*)app_id,(char*)app_id}; XSetClassHint(c->display,w->id,&class_hint);
    if(parent && parent->connection==c) XSetTransientForHint(c->display,w->id,parent->id);
    XWMHints hints={.flags=InputHint|StateHint,.input=True,.initial_state=NormalState}; XSetWMHints(c->display,w->id,&hints);
    nz_window_operations(w,1,width,height);
    nz_window_title(w,title); nz_window_titlebar(w,custom);
    emit(c,w,NZ_SCALE,0,c->scale,0,0,0,0,NULL);
    char exported[32]; snprintf(exported,sizeof(exported),"%lx",w->id); emit(c,w,18,0,0,0,0,0,0,exported);
    return w;
}
static void popup_position(nz_window *w) {
    if (!w->popup_parent) return;
    if (w->popup_parent->popup) popup_position(w->popup_parent);
    nz_connection *c=w->connection;
    nz_popup_placement *p=&w->popup_placement;
    int parent_shadow=w->popup_parent->popup?w->popup_parent->popup_placement.shadow:0;
    Window ignored; int x,y;
    if (XTranslateCoordinates(c->display,w->popup_parent->id,c->root,
        (int)((p->x+parent_shadow-p->shadow)*c->scale)+w->popup_parent->frame_margin,
        (int)((p->y+parent_shadow-p->shadow)*c->scale)+w->popup_parent->frame_margin,&x,&y,&ignored)) {
        int width=client_pixels(c,p->width+2*p->shadow), height=client_pixels(c,p->height+2*p->shadow);
        if(!w->popup_positioned || x!=w->popup_root_x || y!=w->popup_root_y || width!=w->popup_pixel_width || height!=w->popup_pixel_height) {
            XMoveResizeWindow(c->display,w->id,x,y,(unsigned)width,(unsigned)height);
            w->popup_positioned=1; w->popup_root_x=x; w->popup_root_y=y; w->popup_pixel_width=width; w->popup_pixel_height=height;
        }
    }
}

nz_window *nz_popup_create(nz_window *parent, const nz_popup_placement *p) {
    if (!parent || !p || p->width<1 || p->height<1 || p->width>32768 || p->height>32768 || p->shadow<0 || p->shadow>4096) return NULL;
    nz_connection *c=parent->connection;
    nz_window *w=window_create(c,"","org.nazor.Popup",p->width+2*p->shadow,p->height+2*p->shadow,parent,1,1,nz_argb_supported(c));
    if (!w) return NULL;
    w->popup=1; w->popup_parent=parent; w->popup_placement=*p;
    XSetWindowAttributes attributes={.override_redirect=True};
    XChangeWindowAttributes(c->display,w->id,CWOverrideRedirect,&attributes);
    XWMHints hints={.flags=InputHint,.input=False}; XSetWMHints(c->display,w->id,&hints);
    Atom type=atom(c,p->flags&2?"_NET_WM_WINDOW_TYPE_POPUP_MENU":"_NET_WM_WINDOW_TYPE_COMBO");
    XChangeProperty(c->display,w->id,atom(c,"_NET_WM_WINDOW_TYPE"),XA_ATOM,32,PropModeReplace,(unsigned char*)&type,1);
    nz_popup_place(w,p);
    XMapRaised(c->display,w->id); w->visible=1;
    if(p->flags&5) {
        int grab=XGrabPointer(c->display,w->id,True,ButtonPressMask|ButtonReleaseMask|PointerMotionMask,GrabModeAsync,GrabModeAsync,None,None,CurrentTime);
        if (grab!=GrabSuccess) { nz_window_destroy(w); return NULL; }
        w->popup_grabbed=1;
    }
    if (p->flags&1) XGrabKeyboard(c->display,w->id,True,GrabModeAsync,GrabModeAsync,CurrentTime);
    XFlush(c->display);
    return w;
}
int nz_popup_place(nz_window *w, const nz_popup_placement *p) {
    if (!w || !w->popup || !w->popup_parent || !p || p->shadow<0 || p->shadow>4096 ||
        p->width<1 || p->height<1 || p->width>32768-2*p->shadow || p->height>32768-2*p->shadow) return 0;
    w->popup_placement=*p; w->width=p->width+2*p->shadow; w->height=p->height+2*p->shadow;
    popup_position(w);
    XRectangle input={(short)(p->shadow*w->connection->scale),(short)(p->shadow*w->connection->scale),
        (unsigned short)(p->width*w->connection->scale),(unsigned short)(p->height*w->connection->scale)};
    XShapeCombineRectangles(w->connection->display,w->id,ShapeInput,0,0,&input,1,ShapeSet,Unsorted);
    XFlush(w->connection->display);
    return 1;
}
void nz_popup_input_region(nz_window *w, int x, int y, int width, int height) {
    if(!w || !w->popup || width<0 || height<0) return;
    float scale=w->connection->scale;
    XRectangle input={(short)(x*scale),(short)(y*scale),(unsigned short)(width*scale),(unsigned short)(height*scale)};
    XShapeCombineRectangles(w->connection->display,w->id,ShapeInput,0,0,&input,width>0 && height>0?1:0,ShapeSet,Unsorted);
}
int nz_popup_work_area(nz_window *owner, int x, int y, int width, int height, nz_damage_rect *area) {
    if (!owner || !area) return 0;
    nz_connection *c=owner->connection;
    Window ignored; int ox,oy;
    if (!XTranslateCoordinates(c->display,owner->id,c->root,owner->frame_margin,owner->frame_margin,&ox,&oy,&ignored)) return 0;
    long left=0,top=0,right=DisplayWidth(c->display,c->screen),bottom=DisplayHeight(c->display,c->screen);
    int major=0,minor=0;
    if (XRRQueryVersion(c->display,&major,&minor) && (major>1 || (major==1 && minor>=5))) {
        int monitor_count=0;
        XRRMonitorInfo *monitors=XRRGetMonitors(c->display,c->root,True,&monitor_count);
        double best=HUGE_VAL, ax=ox+(x+width/2.0)*c->scale, ay=oy+(y+height/2.0)*c->scale;
        for (int i=0;monitors && i<monitor_count;i++) {
            XRRMonitorInfo *m=&monitors[i];
            double dx=fmax(fmax(m->x-ax,0),ax-m->x-m->width), dy=fmax(fmax(m->y-ay,0),ay-m->y-m->height);
            double distance=dx*dx+dy*dy;
            if(distance<best) { best=distance; left=m->x; top=m->y; right=left+m->width; bottom=top+m->height; }
        }
        if(monitors) XRRFreeMonitors(monitors);
    }
    unsigned char *data=NULL; Atom type; int format; unsigned long count,remaining;
    unsigned long desktop=0;
    if(XGetWindowProperty(c->display,c->root,atom(c,"_NET_CURRENT_DESKTOP"),0,1,False,XA_CARDINAL,&type,&format,&count,&remaining,&data)==Success && format==32 && count==1)
        desktop=*(unsigned long*)data;
    if(data) { XFree(data); data=NULL; }
    if (desktop<65536 && XGetWindowProperty(c->display,c->root,atom(c,"_NET_WORKAREA"),(long)desktop*4,4,False,XA_CARDINAL,&type,&format,&count,&remaining,&data)==Success && format==32 && count>=4) {
        unsigned long *work=(unsigned long*)data;
        left=(long)fmax(left,(long)work[0]); top=(long)fmax(top,(long)work[1]);
        right=(long)fmin(right,(long)work[0]+(long)work[2]); bottom=(long)fmin(bottom,(long)work[1]+(long)work[3]);
    }
    if(data) XFree(data);
    *area=(nz_damage_rect){(int)((left-ox)/c->scale),(int)((top-oy)/c->scale),
        (int)((right-left)/c->scale),(int)((bottom-top)/c->scale)};
    return area->width>0 && area->height>0;
}

nz_window *nz_window_create(nz_connection *c, const char *title, const char *app_id, int width, int height, nz_window *parent, int custom, int borderless) {
    return window_create(c,title,app_id,width,height,parent,custom,borderless,0);
}
nz_window *nz_window_create_argb(nz_connection *c, const char *title, const char *app_id, int width, int height, nz_window *parent, int custom, int borderless) {
    return window_create(c,title,app_id,width,height,parent,custom,borderless,1);
}
void nz_window_destroy(nz_window *w) {
    if(!w) return;
    nz_connection *c=w->connection; nz_window **p=&c->windows;
    for (;;) {
        nz_window *child=c->windows;
        while (child && child->popup_parent!=w) child=child->next;
        if (!child) break;
        nz_window_destroy(child);
    }
    while(*p && *p!=w) p=&(*p)->next;
    if(*p) *p=w->next;
    queued_event **q=&c->head; c->tail=NULL;
    while(*q) { queued_event *e=*q; if(e->event.window==w) { *q=e->next; free(e->text); free(e); } else { c->tail=e; q=&e->next; } }
    if (w->popup_grabbed) {
        nz_window *next=c->windows;
        while(next && (!next->popup_grabbed || !next->visible)) next=next->next;
        if(next) {
            XGrabPointer(c->display,next->id,True,ButtonPressMask|ButtonReleaseMask|PointerMotionMask,GrabModeAsync,GrabModeAsync,None,None,CurrentTime);
            if(next->popup_placement.flags&1) XGrabKeyboard(c->display,next->id,True,GrabModeAsync,GrabModeAsync,CurrentTime);
            else XUngrabKeyboard(c->display,CurrentTime);
        } else { XUngrabPointer(c->display,CurrentTime); XUngrabKeyboard(c->display,CurrentTime); }
    }
    nz_window_outer_resize(w,0);
    nz_window_frame_shadow(w,NULL,0,0,0,1);
    if(w->ic) XDestroyIC(w->ic);
    if(w->image) XDestroyImage(w->image);
    free(w->pixels); XFreeGC(c->display,w->gc); XDestroyWindow(c->display,w->id);
    if(w->colormap) XFreeColormap(c->display,w->colormap);
    if(w->frame_colormap) XFreeColormap(c->display,w->frame_colormap);
    free(w);
}
void nz_disconnect(nz_connection *c) {
    if(!c) return;
    while(c->windows) nz_window_destroy(c->windows);
    nz_event e; while(nz_next_event(c,&e)) { }
    while(c->transfers) { transfer *t=c->transfers; c->transfers=t->next; free(t->data); free(t); }
    if(c->im) XCloseIM(c->im);
    if(c->selection) XDestroyWindow(c->display,c->selection);
    XCloseDisplay(c->display);
    pthread_mutex_lock(&connections_lock); nz_connection **p=&connections; while(*p && *p!=c) p=&(*p)->next; if(*p) *p=c->next; pthread_mutex_unlock(&connections_lock);
    if(c->wake>=0) close(c->wake);
    free(c->clipboard); free(c->received); free(c);
}
void nz_window_show(nz_window *w,int visible) {
    w->visible=visible;
    if(visible) XMapWindow(w->connection->display,w->id);
    else {
        nz_window_outer_resize(w,0);
        nz_window_frame_shadow(w,NULL,0,0,0,1);
        XWithdrawWindow(w->connection->display,w->id,w->connection->screen);
    }
    XFlush(w->connection->display);
}
void nz_window_title(nz_window *w,const char *title) { nz_connection *c=w->connection; XStoreName(c->display,w->id,title); XChangeProperty(c->display,w->id,atom(c,"_NET_WM_NAME"),atom(c,"UTF8_STRING"),8,PropModeReplace,(const unsigned char*)title,(int)strlen(title)); }
static void set_motif_hints(nz_window *w) {
    nz_connection *c=w->connection;
    Atom property=atom(c,"_MOTIF_WM_HINTS");
    unsigned long hints[]={MOTIF_FUNCTIONS|MOTIF_DECORATIONS,w->functions,!w->custom && !w->borderless,0,0};
    XChangeProperty(c->display,w->id,property,property,32,PropModeReplace,(unsigned char*)hints,5);
}
void nz_window_titlebar(nz_window *w,int custom) {
    w->custom=custom;
    set_motif_hints(w);
    sync_companions(w);
    emit(w->connection,w,NZ_CAPABILITIES,0,0,0,0,0,0,NULL);
}
void nz_window_x11_functions(nz_window *w,int minimize,int maximize) {
    unsigned long functions=MOTIF_RESIZE|MOTIF_MOVE|MOTIF_CLOSE;
    if(minimize) functions|=MOTIF_MINIMIZE;
    if(maximize) functions|=MOTIF_MAXIMIZE;
    if(w->functions==functions) return;
    w->functions=functions;
    set_motif_hints(w);
}
int nz_x11_above_supported(nz_connection *c) {
    return property_has(c,c->root,"_NET_SUPPORTED",atom(c,"_NET_WM_STATE_ABOVE"));
}
void nz_window_x11_above(nz_window *w,int above) {
    nz_connection *c=w->connection;
    Atom above_atom=atom(c,"_NET_WM_STATE_ABOVE");
    if(w->visible) {
        send_message(w,"_NET_WM_STATE",above,above_atom,0,1,0);
        return;
    }
    Atom state=atom(c,"_NET_WM_STATE"), actual;
    int format;
    unsigned long count, remaining;
    unsigned char *data=NULL;
    if(XGetWindowProperty(c->display,w->id,state,0,4096,False,XA_ATOM,&actual,&format,&count,&remaining,&data)!=Success)
        return;
    Atom *states=(Atom*)data;
    unsigned long kept=0;
    for(unsigned long i=0;format==32 && i<count;i++)
        if(states[i]!=above_atom) states[kept++]=states[i];
    XChangeProperty(c->display,w->id,state,XA_ATOM,32,PropModeReplace,data,(int)kept);
    if(above)
        XChangeProperty(c->display,w->id,state,XA_ATOM,32,PropModeAppend,(unsigned char*)&above_atom,1);
    if(data) XFree(data);
}
void nz_window_state(nz_window *w,int state) {
    nz_connection *c=w->connection;
    if(state==NZ_STATE_MINIMIZED) { XIconifyWindow(c->display,w->id,c->screen); XFlush(c->display); return; }
    if(!w->visible) nz_window_show(w,1);
    send_message(w,"_NET_WM_STATE",state==NZ_STATE_FULLSCREEN?1:0,(long)atom(c,"_NET_WM_STATE_FULLSCREEN"),0,1,0);
    send_message(w,"_NET_WM_STATE",state==NZ_STATE_MAXIMIZED?1:0,(long)atom(c,"_NET_WM_STATE_MAXIMIZED_VERT"),(long)atom(c,"_NET_WM_STATE_MAXIMIZED_HORZ"),1,0);
    if(w->state==NZ_STATE_MINIMIZED) XMapWindow(c->display,w->id);
}
void nz_window_operations(nz_window *w,int resize,int width,int height) {
    w->resizable=resize;
    int padding=2*w->frame_margin;
    int fixed_width=client_pixels(w->connection,width)+padding, fixed_height=client_pixels(w->connection,height)+padding;
    XSizeHints hints={.flags=PMinSize|PMaxSize|PWinGravity,.win_gravity=StaticGravity,
        .min_width=resize?1+padding:fixed_width,.min_height=resize?1+padding:fixed_height,
        .max_width=resize?MAX_CLIENT_PIXELS+padding:fixed_width,.max_height=resize?MAX_CLIENT_PIXELS+padding:fixed_height};
    XSetWMNormalHints(w->connection->display,w->id,&hints);
}
void nz_window_resize(nz_window *w,int width,int height) {
    if(width>0 && height>0 && width<=32768 && height<=32768)
        XResizeWindow(w->connection->display,w->id,(unsigned)(client_pixels(w->connection,width)+2*w->frame_margin),
            (unsigned)(client_pixels(w->connection,height)+2*w->frame_margin));
}
void nz_window_activate(nz_window *w) { send_message(w,"_NET_ACTIVE_WINDOW",1,CurrentTime,0,0,0); }
static void move_resize(nz_window *w,int direction) { Window root,child; int x,y,local_x,local_y; unsigned mask; nz_connection *c=w->connection; if(XQueryPointer(c->display,w->id,&root,&child,&x,&y,&local_x,&local_y,&mask)) { XUngrabPointer(c->display,CurrentTime); send_message(w,"_NET_WM_MOVERESIZE",x,y,direction,1,1); } }
void nz_window_move(nz_window *w) { move_resize(w,8); }
int nz_window_menu(nz_window *w,uint32_t serial,int x,int y) {
    nz_connection *c=w->connection;
    if(!property_has(c,c->root,"_NET_SUPPORTED",atom(c,"_GTK_SHOW_WINDOW_MENU"))) return 0;
    int root_x,root_y;
    Window child;
    if(!XTranslateCoordinates(c->display,w->id,c->root,(int)(x*c->scale)+w->frame_margin,(int)(y*c->scale)+w->frame_margin,&root_x,&root_y,&child)) return 0;
    XUngrabPointer(c->display,CurrentTime);
    send_message(w,"_GTK_SHOW_WINDOW_MENU",0,root_x,root_y,0,0);
    return 1;
}
void nz_window_interactive_resize(nz_window *w,uint32_t edges) { int direction=(edges&1)?((edges&4)?0:(edges&8)?2:1):(edges&2)?((edges&4)?6:(edges&8)?4:5):(edges&4)?7:3; move_resize(w,direction); }
void nz_window_outer_resize(nz_window *w,int margin) {
    if(!w) return;
    nz_connection *c=w->connection;
    if(w->content && !w->popup) {
        int next=margin>0 && margin<=128 && w->state==NZ_STATE_NORMAL && (w->borderless || w->custom) ? (int)ceil(margin*c->scale):0;
        if(w->frame_margin==next) return;
        XWindowAttributes attributes; Window ignored; int x,y;
        if(!XGetWindowAttributes(c->display,w->id,&attributes) || !XTranslateCoordinates(c->display,w->id,c->root,0,0,&x,&y,&ignored)) return;
        int previous=w->frame_margin, delta=next-previous;
        w->frame_margin=next;
        nz_window_operations(w,w->resizable,w->width,w->height);
        unsigned long extents[]={(unsigned long)next,(unsigned long)next,(unsigned long)next,(unsigned long)next};
        XChangeProperty(c->display,w->id,atom(c,"_GTK_FRAME_EXTENTS"),XA_CARDINAL,32,PropModeReplace,(unsigned char*)extents,4);
        int width=attributes.width+2*delta, height=attributes.height+2*delta;
        if(w->state!=NZ_STATE_NORMAL) { width=attributes.width; height=attributes.height; delta=0; }
        XMoveResizeWindow(c->display,w->id,x-delta,y-delta,(unsigned)width,(unsigned)height);
        if(w->state!=NZ_STATE_NORMAL) {
            update_size(w,width,height);
            emit_configure(w);
        } else resize_content(w,width,height);
        XClearWindow(c->display,w->id);
        sync_companions(w);
        return;
    }
    if(margin<=0 || margin>128 || !c->shape_input || (!w->borderless && !w->custom)) {
        if(w->resize_window) XDestroyWindow(c->display,w->resize_window);
        w->resize_window=None; w->resize_margin=0;
        return;
    }
    int pixels=(int)ceil(margin*c->scale);
    if(w->resize_margin==pixels) return;
    if(!w->resize_window)
        w->resize_window=companion_window(w,0,InputOnly,CopyFromParent,None,ButtonPressMask|ButtonReleaseMask|PointerMotionMask|EnterWindowMask|LeaveWindowMask);
    w->resize_margin=pixels; w->resize_width=0; w->resize_height=0;
    sync_companions(w);
}
static int corner_cut(float x, float y, double row) {
    if(x<=0 || y<=0 || row>=y) return 0;
    double ratio=1-row/y;
    return (int)floor(x*(1-sqrt(1-ratio*ratio))+.5);
}
int nz_window_x11_corners(nz_window *w,const float *radii) {
    if(!w || !w->connection->shape_input) return 0;
    Display *display=w->connection->display;
    XWindowAttributes attributes;
    Window target=w->content?w->content:w->id;
    if(!radii) memset(w->frame_radii,0,sizeof(w->frame_radii));
    else if(radii!=w->frame_radii) memcpy(w->frame_radii,radii,sizeof(w->frame_radii));
    radii=w->frame_radii;
    int rounded=0;
    for(int i=0;i<8;i++) if(isfinite(radii[i]) && radii[i]>0) rounded=1;
    if(!rounded) {
        XShapeCombineMask(display,target,ShapeBounding,0,0,None,ShapeSet);
        if(w->content) XShapeCombineMask(display,w->id,ShapeBounding,0,0,None,ShapeSet);
        return 1;
    }
    if(!XGetWindowAttributes(display,target,&attributes) || attributes.width>32760 || attributes.height>32760) return 0;
    int width=attributes.width, height=attributes.height, count=0;
    float bounded[8];
    for(int i=0;i<8;i++) bounded[i]=isfinite(radii[i]) ? fminf(fmaxf(0,radii[i]),i%2?height:width):0;
    XRectangle *bands=malloc((size_t)height*sizeof(*bands));
    if(!bands) return 0;
    for(int y=0;y<height;y++) {
        double top=y+.5, bottom=height-top;
        int left=corner_cut(bounded[0],bounded[1],top)+corner_cut(bounded[6],bounded[7],bottom);
        int right=corner_cut(bounded[2],bounded[3],top)+corner_cut(bounded[4],bounded[5],bottom);
        int span=width-left-right;
        if(span<=0) continue;
        XRectangle *last=count?&bands[count-1]:NULL;
        if(last && last->x==left && last->width==span && last->y+last->height==y) last->height++;
        else bands[count++]=(XRectangle){(short)left,(short)y,(unsigned short)span,1};
    }
    XShapeCombineRectangles(display,target,ShapeBounding,0,0,bands,count,ShapeSet,YXBanded);
    if(w->content) {
        int margin=w->frame_margin;
        XShapeCombineShape(display,w->id,ShapeBounding,margin,margin,w->content,ShapeBounding,ShapeSet);
        if(margin) {
            XRectangle ring[]={ {0,0,(unsigned short)(width+2*margin),(unsigned short)margin},
                {0,(short)margin,(unsigned short)margin,(unsigned short)height},
                {(short)(width+margin),(short)margin,(unsigned short)margin,(unsigned short)height},
                {0,(short)(height+margin),(unsigned short)(width+2*margin),(unsigned short)margin} };
            XShapeCombineRectangles(display,w->id,ShapeBounding,0,0,ring,4,ShapeUnion,Unsorted);
        }
    }
    free(bands);
    return 1;
}
static void resize_content(nz_window *w, int width, int height) {
    if(!w->content) return;
    int margin=w->frame_margin;
    XMoveResizeWindow(w->connection->display,w->content,margin,margin,(unsigned)(width>2*margin?width-2*margin:1),(unsigned)(height>2*margin?height-2*margin:1));
    nz_window_x11_corners(w,w->frame_radii);
}
void nz_window_cursor(nz_window *w,const char *name) {
    Display *display=w->connection->display;
    Cursor cursor=XcursorLibraryLoadCursor(display,name);
    if(cursor) {
        XDefineCursor(display,w->id,cursor);
        if(w->resize_window) XDefineCursor(display,w->resize_window,cursor);
        XFreeCursor(display,cursor);
    }
}
void nz_window_text_input(nz_window *w,int enabled,int x,int y,int width,int height) { if(w->ic) { if(enabled) XSetICFocus(w->ic); else XUnsetICFocus(w->ic); } }
void nz_window_surrounding(nz_window *w,const char *text,int cursor,int anchor,int password) { }
uint32_t nz_window_capabilities(nz_window *w) { nz_connection *c=w->connection; return 16u|(property_has(c,c->root,"_NET_SUPPORTED",atom(c,"_NET_WM_STATE_MAXIMIZED_VERT"))?2u:0u)|(property_has(c,c->root,"_NET_SUPPORTED",atom(c,"_NET_WM_STATE_FULLSCREEN"))?4u:0u)|(property_has(c,c->root,"_NET_SUPPORTED",atom(c,"_NET_WM_STATE_HIDDEN"))?8u:0u); }
int nz_window_acquire(nz_window *w,int width,int height,nz_buffer_view *view) {
    if(!w || !view || width<1 || height<1 || width>32768 || height>32768 || (size_t)width*height>CLIP_LIMIT*4u) return -1;
    if(w->image_width!=width || w->image_height!=height) {
        if(w->image) { XDestroyImage(w->image); w->image=NULL; }
        free(w->pixels); w->pixels=NULL; w->current=0;
        w->pixels=calloc((size_t)width*height,4);
        nz_connection *c=w->connection;
        w->image=XCreateImage(c->display,w->visual,(unsigned)w->depth,ZPixmap,0,NULL,(unsigned)width,(unsigned)height,32,0);
        if(!w->pixels || !w->image || !(w->image->data=calloc((size_t)w->image->bytes_per_line,height))) { snprintf(c->error,sizeof(c->error),"Cannot allocate an X11 image."); return -1; }
        w->image_width=width; w->image_height=height; w->stride=width*4;
    }
    *view=(nz_buffer_view){w->pixels,width,height,w->stride}; return 1;
}
static unsigned long component(unsigned char value,unsigned long mask) { unsigned shift=0; if(!mask) return 0; while(!(mask&1)) { mask>>=1; shift++; } return (((unsigned long)value*mask+127)/255)<<shift; }
int nz_window_frame_shadow(nz_window *w,const void *pixels,int width,int height,int margin,float scale) {
    if(!w) return 0;
    nz_connection *c=w->connection;
    Display *display=c->display;
    if(!pixels) {
        if(w->shadow_window) XDestroyWindow(display,w->shadow_window);
        if(w->shadow_colormap) XFreeColormap(display,w->shadow_colormap);
        w->shadow_window=None; w->shadow_colormap=None;
        return 1;
    }
    if(!c->shape_input || !nz_argb_supported(c) || width<=0 || height<=0 || width>32760 || height>32760 ||
        (size_t)width*height>16u*1024u*1024u || margin<=0 || margin>128 || !isfinite(scale) || scale<0.5f || scale>8) return 0;
    XImage *image=XCreateImage(display,c->argb_visual,32,ZPixmap,0,NULL,(unsigned)width,(unsigned)height,32,0);
    if(!image) return 0;
    image->data=calloc((size_t)image->bytes_per_line,height);
    if(!image->data) { XDestroyImage(image); return 0; }
    const unsigned char *source=pixels;
    for(int y=0;y<height;y++,source+=width*4) {
        if(image->bits_per_pixel==32 && image->byte_order==LSBFirst && image->red_mask==0xff0000 && image->green_mask==0xff00 && image->blue_mask==0xff)
            memcpy(image->data+y*image->bytes_per_line,source,(size_t)width*4);
        else for(int x=0;x<width;x++) {
            const unsigned char *pixel=source+x*4;
            XPutPixel(image,x,y,component(pixel[2],image->red_mask)|component(pixel[1],image->green_mask)|component(pixel[0],image->blue_mask)|(unsigned long)pixel[3]<<24);
        }
    }
    if(!w->shadow_window) {
        w->shadow_colormap=XCreateColormap(display,c->root,c->argb_visual,AllocNone);
        w->shadow_window=companion_window(w,32,InputOutput,c->argb_visual,w->shadow_colormap,0);
        XShapeCombineRectangles(display,w->shadow_window,ShapeInput,0,0,NULL,0,ShapeSet,Unsorted);
    }
    Pixmap background=XCreatePixmap(display,w->shadow_window,(unsigned)width,(unsigned)height,32);
    GC gc=XCreateGC(display,background,0,NULL);
    XPutImage(display,background,gc,image,0,0,0,0,(unsigned)width,(unsigned)height);
    XSetWindowBackgroundPixmap(display,w->shadow_window,background);
    XFreeGC(display,gc);
    XFreePixmap(display,background);
    XDestroyImage(image);
    w->shadow_margin=(int)ceil(margin*scale);
    w->shadow_width=width; w->shadow_height=height;
    sync_companions(w);
    XClearWindow(display,w->shadow_window);
    XFlush(display);
    return 1;
}
static void present_rect(nz_window *w,int x,int y,int width,int height) {
    if(x<0) { width+=x; x=0; } if(y<0) { height+=y; y=0; }
    if(width>w->image_width-x) width=w->image_width-x;
    if(height>w->image_height-y) height=w->image_height-y;
    if(width<=0 || height<=0) return;
    for(int row=y;row<y+height;row++) {
        unsigned char *source=w->pixels+row*w->stride+x*4;
        if(w->image->bits_per_pixel==32 && w->image->byte_order==LSBFirst && w->image->red_mask==0xff0000 && w->image->green_mask==0xff00 && w->image->blue_mask==0xff) memcpy(w->image->data+row*w->image->bytes_per_line+x*4,source,(size_t)width*4);
        else for(int col=x;col<x+width;col++,source+=4) XPutPixel(w->image,col,row,component(source[2],w->image->red_mask)|component(source[1],w->image->green_mask)|component(source[0],w->image->blue_mask)|(w->colormap?(unsigned long)source[3]<<24:0));
    }
    XPutImage(w->connection->display,w->content?w->content:w->id,w->gc,w->image,x,y,x,y,(unsigned)width,(unsigned)height);
}
void nz_window_present_damage(nz_window *w,float scale,int frame,const nz_damage_rect *rects,int count) { if(w->image) { if(!rects) present_rect(w,0,0,w->image_width,w->image_height); else for(int i=0;i<count;i++) present_rect(w,rects[i].x,rects[i].y,rects[i].width,rects[i].height); w->current=1; } XFlush(w->connection->display); if(frame) emit(w->connection,w,NZ_FRAME,0,0,0,0,0,0,NULL); }
void nz_window_present(nz_window *w,float scale,int frame) { nz_window_present_damage(w,scale,frame,NULL,0); }
int nz_window_buffer_is_current(nz_window *w) { return w->current; }
void nz_window_discard_buffer(nz_window *w) { }
void *nz_window_native_display(nz_window *w) { return w->connection->display; }
void *nz_window_native_surface(nz_window *w) { return (void*)(uintptr_t)w->id; }
NZ_API void *nz_window_render_surface(nz_window *w) { return (void*)(uintptr_t)(w->content?w->content:w->id); }
void nz_window_gpu_prepare(nz_window *w) { emit(w->connection,w,NZ_FRAME,0,0,0,0,0,0,NULL); }
void nz_window_cancel_frame(nz_window *w) { }
int nz_window_icon(nz_window *w,const void *pixels,int width,int height) {
    nz_connection *c=w->connection; Atom icon=atom(c,"_NET_WM_ICON");
    if(!pixels || width<=0 || height<=0) { XDeleteProperty(c->display,w->id,icon); return 1; }
    if(width>256 || height>256) return 0;
    size_t count=(size_t)width*height; unsigned long *data=malloc((count+2)*sizeof(*data)); if(!data) return 0;
    data[0]=(unsigned)width; data[1]=(unsigned)height; const uint32_t *source=pixels;
    for(size_t i=0;i<count;i++) { uint32_t p=source[i]; unsigned a=p>>24; unsigned r=a?((p>>16)&255)*255/a:0,g=a?((p>>8)&255)*255/a:0,b=a?(p&255)*255/a:0; data[i+2]=(unsigned long)((a<<24)|((r>255?255:r)<<16)|((g>255?255:g)<<8)|(b>255?255:b)); }
    XChangeProperty(c->display,w->id,icon,XA_CARDINAL,32,PropModeReplace,(unsigned char*)data,(int)(count+2)); free(data); return 1;
}
int nz_window_blur(nz_window *w,int enabled) { return !enabled; }
static Bool timestamp_event(Display *display,XEvent *event,XPointer data) {
    nz_connection *c=(nz_connection*)data;
    return event->type==PropertyNotify && event->xproperty.window==c->selection && event->xproperty.atom==c->selection_time_property;
}
int nz_clipboard_set(nz_connection *c,const char *text) {
    size_t length=strlen(text); if(length>CLIP_LIMIT) return 0;
    char *copy=strdup(text); if(!copy) return 0;
    unsigned long value=0;
    XChangeProperty(c->display,c->selection,c->selection_time_property,XA_INTEGER,32,PropModeReplace,(unsigned char*)&value,1);
    XEvent event; XIfEvent(c->display,&event,timestamp_event,(XPointer)c); c->selection_time=event.xproperty.time;
    free(c->clipboard); c->clipboard=copy;
    XSetSelectionOwner(c->display,atom(c,"CLIPBOARD"),c->selection,c->selection_time); XFlush(c->display);
    return XGetSelectionOwner(c->display,atom(c,"CLIPBOARD"))==c->selection;
}
int nz_clipboard_request(nz_connection *c) { if(XGetSelectionOwner(c->display,atom(c,"CLIPBOARD"))==c->selection) { clipboard_result(c,c->clipboard); return 1; } free(c->received); c->received=NULL; c->received_size=0; c->receiving=1; c->incremental=0; c->latin1=0; XConvertSelection(c->display,atom(c,"CLIPBOARD"),atom(c,"UTF8_STRING"),atom(c,"_NAZOR_CLIPBOARD"),c->selection,CurrentTime); XFlush(c->display); return 1; }
NZ_API void nz_window_pointer_capture(nz_window *w,int capture) {
    if(capture) XGrabPointer(w->connection->display,w->id,False,ButtonPressMask|ButtonReleaseMask|PointerMotionMask,GrabModeAsync,GrabModeAsync,None,None,CurrentTime);
    else {
        nz_window *popup=w->connection->windows;
        while(popup && (!popup->popup_grabbed || !popup->visible)) popup=popup->next;
        if(popup) XGrabPointer(w->connection->display,popup->id,True,ButtonPressMask|ButtonReleaseMask|PointerMotionMask,GrabModeAsync,GrabModeAsync,None,None,CurrentTime);
        else XUngrabPointer(w->connection->display,CurrentTime);
    }
}
NZ_API int nz_window_native_visual(nz_window *w) { return (int)XVisualIDFromVisual(w->visual); }

int nz_window_foreign_parent(nz_window *w, const char *handle) {
    if (!w || !handle || strncmp(handle,"x11:",4) || !handle[4]) return 0;
    char *end=NULL;
    errno=0;
    unsigned long owner=strtoul(handle+4,&end,16);
    if (errno || !end || *end || !owner || owner==w->id) return 0;
    XSetTransientForHint(w->connection->display,w->id,(Window)owner);
    XFlush(w->connection->display);
    return 1;
}
