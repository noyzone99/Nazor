#define _POSIX_C_SOURCE 200809L
#include "nazor-wayland.h"
#include <X11/Xlib.h>
#include <X11/Xatom.h>
#include <X11/Xutil.h>
#include <X11/keysym.h>
#include <X11/extensions/shape.h>
#undef NDEBUG
#include <assert.h>
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static void verify_foreign_parent(nz_window *owner) {
    char error[512]={0}, token[64];
    nz_connection *other=nz_connect(error,sizeof(error)); assert(other);
    nz_window *child=nz_window_create(other,"Foreign child","org.nazor.Tests",120,80,NULL,1,0); assert(child);
    unsigned long id=(unsigned long)(uintptr_t)nz_window_native_surface(owner);
    snprintf(token,sizeof(token),"x11:%lx",id);
    assert(!nz_window_foreign_parent(child,"wayland:invalid"));
    assert(!nz_window_foreign_parent(child,"x11:invalid"));
    assert(nz_window_foreign_parent(child,token));
    Window actual=None;
    assert(XGetTransientForHint(nz_window_native_display(child),(Window)(uintptr_t)nz_window_native_surface(child),&actual));
    assert(actual==id);
    nz_window_destroy(child); nz_disconnect(other);
}

static double now(void) { struct timespec t; clock_gettime(CLOCK_MONOTONIC,&t); return t.tv_sec+t.tv_nsec/1e9; }
static void *wake_loop(void *arg) { struct timespec delay={0,30000000}; nanosleep(&delay,NULL); nz_wake(arg); return NULL; }

#ifdef NAZOR_X11_NESTED_TESTS
extern int XTestFakeMotionEvent(Display *, int, int, int, unsigned long);
extern int XTestFakeButtonEvent(Display *, unsigned int, Bool, unsigned long);

static void verify_xwayland_outer_resize(nz_connection *c) {
    Display *outer=XOpenDisplay(getenv("NAZOR_TEST_OUTER_DISPLAY")); assert(outer);
    Window root=DefaultRootWindow(outer), parent, ignored, *children;
    unsigned count;
    assert(XQueryTree(outer,root,&ignored,&parent,&children,&count));
    int ox=0,oy=0,found=0;
    for(unsigned i=0;i<count;i++) {
        XWindowAttributes attributes;
        assert(XGetWindowAttributes(outer,children[i],&attributes));
        if(attributes.width==800 && attributes.height==600 && attributes.map_state==IsViewable) {
            assert(XTranslateCoordinates(outer,children[i],root,0,0,&ox,&oy,&ignored));
            found=1;
        }
    }
    XFree(children); assert(found);
    nz_window *w=nz_window_create(c,"Xwayland outer resize","org.nazor.Tests",240,160,NULL,1,0); assert(w);
    nz_window_show(w,1);
    nz_buffer_view view; assert(nz_window_acquire(w,240,160,&view)==1);
    for(int i=0;i<240*160;i++) ((uint32_t*)view.pixels)[i]=0xff336699;
    nz_window_present(w,1,1);
    nz_window_outer_resize(w,64);
    double deadline=now()+3;
    nz_event event;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
    nz_window_present(w,1,1);
    deadline=now()+.2;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
    Display *display=nz_window_native_display(w);
    Window id=(Window)(uintptr_t)nz_window_native_surface(w);
    int x,y;
    assert(XTranslateCoordinates(display,(Window)(uintptr_t)nz_window_render_surface(w),DefaultRootWindow(display),0,0,&x,&y,&ignored));
    XImage *pixel=XGetImage(outer,root,ox+x+120,oy+y+80,1,1,AllPlanes,ZPixmap); assert(pixel);
    assert((XGetPixel(pixel,0,0)&0xffffff)==0x336699); XDestroyImage(pixel);
    pixel=XGetImage(display,id,0,0,1,1,AllPlanes,ZPixmap); assert(pixel);
    assert(XGetPixel(pixel,0,0)==0); XDestroyImage(pixel);
    XTestFakeMotionEvent(outer,DefaultScreen(outer),ox+x+120,oy+y+80,0); XFlush(outer);
    deadline=now()+2;
    int inside=0;
    while(!inside && now()<deadline) {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&event)) if(event.window==w && (event.type==NZ_POINTER_ENTER || event.type==NZ_POINTER_MOTION) && !(event.flags&NZ_POINTER_FRAME_OUTSET)) inside=1;
    }
    assert(inside);
    XTestFakeMotionEvent(outer,DefaultScreen(outer),ox+x+120,oy+y+208,0); XFlush(outer);
    deadline=now()+2;
    int outside=0;
    while(!outside && now()<deadline) {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&event)) if(event.window==w && (event.type==NZ_POINTER_ENTER || event.type==NZ_POINTER_MOTION) && (event.flags&NZ_POINTER_FRAME_OUTSET)) {
            assert(event.x==120 && event.y==208);
            outside=1;
        }
    }
    assert(outside);
    XTestFakeMotionEvent(outer,DefaultScreen(outer),ox+x+121,oy+y+208,0); XFlush(outer);
    deadline=now()+.2;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
    XTestFakeButtonEvent(outer,1,True,0); XFlush(outer);
    deadline=now()+2;
    int pressed=0;
    while(!pressed && now()<deadline) {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&event)) if(event.window==w && event.type==NZ_POINTER_BUTTON && event.b && (event.flags&NZ_POINTER_FRAME_OUTSET)) {
            nz_window_interactive_resize(w,2);
            pressed=1;
        }
    }
    assert(pressed);
    deadline=now()+.3;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
    XTestFakeMotionEvent(outer,DefaultScreen(outer),ox+x+120,oy+y+232,0); XFlush(outer);
    deadline=now()+2;
    int resized=0;
    while(!resized && now()<deadline) {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&event)) if(event.window==w && event.type==NZ_CONFIGURE && event.b>160) resized=1;
    }
    XTestFakeButtonEvent(outer,1,False,0); XFlush(outer);
    assert(resized);
    deadline=now()+.3;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
    XWindowAttributes content;
    assert(XGetWindowAttributes(display,(Window)(uintptr_t)nz_window_render_surface(w),&content));
    int normal_width=content.width, normal_height=content.height;
    const int margins[]={0,8,16,64};
    for(unsigned i=0;i<sizeof(margins)/sizeof(margins[0]);i++) {
        nz_window_outer_resize(w,margins[i]);
        deadline=now()+.2;
        while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&event)) { } }
        assert(XGetWindowAttributes(display,(Window)(uintptr_t)nz_window_render_surface(w),&content));
        assert(content.width==normal_width && content.height==normal_height);
    }
    const int states[]={NZ_STATE_MAXIMIZED,NZ_STATE_NORMAL,NZ_STATE_FULLSCREEN,NZ_STATE_NORMAL};
    for(unsigned i=0;i<sizeof(states)/sizeof(states[0]);i++) {
        nz_window_state(w,states[i]);
        deadline=now()+3;
        int reported=-1;
        do {
            assert(nz_dispatch(c,20)==0);
            while(nz_next_event(c,&event)) if(event.window==w && event.type==NZ_CONFIGURE) {
                reported=event.c;
                nz_window_outer_resize(w,reported==NZ_STATE_NORMAL?64:0);
            }
        } while((reported!=states[i] || now()<deadline-2) && now()<deadline);
        assert(reported==states[i]);
        assert(XGetWindowAttributes(display,(Window)(uintptr_t)nz_window_render_surface(w),&content));
        if(states[i]==NZ_STATE_NORMAL) {
            assert(content.width==normal_width && content.height==normal_height);
            XSizeHints hints; long supplied;
            assert(XGetWMNormalHints(display,id,&hints,&supplied));
            assert(hints.min_width==129 && hints.min_height==129);
        }
    }
    nz_window_outer_resize(w,0);
    nz_window_destroy(w); XCloseDisplay(outer);
    puts("PASS: Xwayland compositor pointer input and interactive resize beyond the owner shadow.");
}
#endif

static void verify_minimize_restore(nz_connection *c,nz_window *w) {
    const int states[]={NZ_STATE_MINIMIZED,NZ_STATE_NORMAL};
    int reported=NZ_STATE_NORMAL;
    Display *display=nz_window_native_display(w);
    Window content=(Window)(uintptr_t)nz_window_render_surface(w);
    XWindowAttributes before,after;
    assert(XGetWindowAttributes(display,content,&before));
    nz_window_outer_resize(w,16);
    for(size_t i=0;i<sizeof(states)/sizeof(states[0]);i++) {
        nz_window_state(w,states[i]);
        double deadline=now()+3;
        do {
            assert(nz_dispatch(c,20)==0);
            nz_event event;
            while(nz_next_event(c,&event)) if(event.type==NZ_CONFIGURE && event.window==w) {
                reported=event.c;
                nz_window_outer_resize(w,reported==NZ_STATE_NORMAL?16:0);
            }
        } while(reported!=states[i] && now()<deadline);
        assert(reported==states[i]);
    }
    double settle=now()+.3;
    while(now()<settle) {
        assert(nz_dispatch(c,20)==0);
        nz_event event;
        while(nz_next_event(c,&event)) if(event.type==NZ_CONFIGURE && event.window==w) nz_window_outer_resize(w,event.c==NZ_STATE_NORMAL?16:0);
    }
    assert(XGetWindowAttributes(display,content,&after));
    assert(before.width==after.width && before.height==after.height);
    nz_window_outer_resize(w,0);
}

static void verify_missing_frame_extents(nz_window *w, Display *display, Window id, int expected) {
    XGrabServer(display);
    Atom property=XInternAtom(display,"_NET_FRAME_EXTENTS",False),actual;
    int format; unsigned long count,remaining; unsigned char *data=NULL;
    assert(XGetWindowProperty(display,id,property,0,4,False,XA_CARDINAL,&actual,&format,&count,&remaining,&data)==Success);
    XDeleteProperty(display,id,property);
    XSync(display,False);
    int observed=nz_window_decoration_mode(w);
    if(actual==XA_CARDINAL && format==32 && count==4)
        XChangeProperty(display,id,property,actual,format,PropModeReplace,data,(int)count);
    if(data) XFree(data);
    XUngrabServer(display);
    XFlush(display);
    assert(observed==expected);
}

static void verify_client_frame(nz_connection *c, nz_window *w, Display *display, Window id, XWindowAttributes attributes) {
    verify_missing_frame_extents(w,display,id,1);
    float radii[]={18,18,0,0,12,12,0,0};
    assert(nz_window_x11_corners(w,radii));
    Bool bounding,clip;
    int x,y,clip_x,clip_y;
    unsigned width,height,clip_width,clip_height;
    assert(XShapeQueryExtents(display,id,&bounding,&x,&y,&width,&height,&clip,&clip_x,&clip_y,&clip_width,&clip_height));
    assert(bounding && width==(unsigned)attributes.width && height==(unsigned)attributes.height);
    assert(nz_window_x11_corners(w,NULL));

    Window root=DefaultRootWindow(display), child;
    assert(XTranslateCoordinates(display,id,root,0,0,&x,&y,&child));
    nz_window_outer_resize(w,8);
    XWarpPointer(display,None,root,0,0,0,0,x+attributes.width/2,y+attributes.height/2);
    XSync(display,False);
    assert(nz_dispatch(c,0)==0);
    nz_event event;
    while(nz_next_event(c,&event)) { }
    int outside_y=y+attributes.height+3;
    if(outside_y>=DisplayHeight(display,DefaultScreen(display))) outside_y=y-3;
    assert(outside_y>=0);
    XWarpPointer(display,None,root,0,0,0,0,x+attributes.width/2,outside_y);
    XFlush(display);
    double deadline=now()+3;
    int entered=0;
    do {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&event)) if(event.window==w && event.type==NZ_POINTER_ENTER && (event.flags&NZ_POINTER_FRAME_OUTSET)) {
            entered=1;
            assert(event.x>0 && event.y==(double)(outside_y-y));
        }
    } while(!entered && now()<deadline);
    assert(entered);
    nz_window_outer_resize(w,0);
}

static void verify_nested_frame_observation(nz_connection *c, Display *display) {
    nz_window *w=nz_window_create(c,"Nested wrapper","org.nazor.Tests",160,100,NULL,1,0); assert(w);
    Window id=(Window)(uintptr_t)nz_window_native_surface(w);
    XSetWindowAttributes attributes={.override_redirect=True};
    Window frame=XCreateWindow(display,DefaultRootWindow(display),0,0,200,160,0,CopyFromParent,InputOutput,CopyFromParent,CWOverrideRedirect,&attributes);
    Window wrapper=XCreateSimpleWindow(display,frame,10,30,160,100,0,0,0);
    XReparentWindow(display,id,wrapper,0,0);
    Atom state=XInternAtom(display,"WM_STATE",False);
    unsigned long values[]={NormalState,None};
    XChangeProperty(display,id,state,state,32,PropModeReplace,(unsigned char*)values,2);
    XMapWindow(display,id); XMapWindow(display,wrapper); XMapWindow(display,frame);
    XSync(display,False);
    assert(nz_window_decoration_mode(w)==0);
    XMoveWindow(display,wrapper,0,0); XSync(display,False);
    assert(nz_window_decoration_mode(w)==0);
    XResizeWindow(display,frame,160,100); XSync(display,False);
    assert(nz_window_decoration_mode(w)==1);
    XDeleteProperty(display,id,state); XSync(display,False);
    assert(nz_window_decoration_mode(w)==0);
    assert(nz_dispatch(c,0)==0);
    nz_event event;
    while(nz_next_event(c,&event)) { }
    XChangeProperty(display,id,state,state,32,PropModeReplace,(unsigned char*)values,2); XSync(display,False);
    assert(nz_window_decoration_mode(w)==1);
    int notified=0;
    assert(nz_dispatch(c,0)==0);
    while(nz_next_event(c,&event)) if(event.window==w && event.type==NZ_CAPABILITIES) notified=1;
    assert(notified);
    XUnmapWindow(display,frame); XSync(display,False);
    assert(nz_window_decoration_mode(w)==0);
    nz_window_destroy(w);
    XDestroyWindow(display,frame);
}

static void verify_argb(nz_connection *c,Display *display) {
    char name[32];
    snprintf(name,sizeof(name),"_NET_WM_CM_S%d",DefaultScreen(display));
    Atom selection=XInternAtom(display,name,False);
    Window marker=None;
    if (XGetSelectionOwner(display,selection)==None) {
        assert(!nz_argb_supported(c));
        assert(!nz_window_create_argb(c,"Unsupported","org.nazor.Tests",80,60,NULL,1,0));
        marker=XCreateSimpleWindow(display,DefaultRootWindow(display),0,0,1,1,0,0,0);
        XSetSelectionOwner(display,selection,marker,CurrentTime);
    }
    assert(nz_argb_supported(c));
    nz_window *w=nz_window_create_argb(c,"ARGB","org.nazor.Tests",80,60,NULL,1,0);
    assert(w);
    Window id=(Window)(uintptr_t)nz_window_native_surface(w);
    nz_window_show(w,1);
    XWindowAttributes attributes;
    double deadline=now()+3;
    do {
        assert(nz_dispatch(c,20)==0);
        assert(XGetWindowAttributes(display,id,&attributes));
    } while(attributes.map_state!=IsViewable && now()<deadline);
    assert(attributes.map_state==IsViewable && attributes.depth==32);
    uint32_t shadow[96*76]={0};
    assert(nz_window_frame_shadow(w,shadow,96,76,8,1));
    assert(nz_dispatch(c,0)==0);
    assert(nz_window_frame_shadow(w,NULL,0,0,0,1));
    nz_buffer_view view;
    assert(nz_window_acquire(w,80,60,&view)==1);
    uint32_t *pixels=view.pixels;
    pixels[0]=0x00000000; pixels[1]=0x80800000; pixels[2]=0xff0000ff;
    nz_window_present(w,1,0);
    XImage *image=XGetImage(display,id,0,0,3,1,AllPlanes,ZPixmap);
    assert(image);
    for(int x=0;x<3;x++) assert(XGetPixel(image,x,0)==pixels[x]);
    XDestroyImage(image);
    nz_window_destroy(w);
    if (marker!=None) {
        XDestroyWindow(display,marker);
        assert(!nz_argb_supported(c));
    }
}

static void verify_popups(nz_connection *c, nz_window *owner, Display *display) {
    nz_popup_placement placement={.x=100,.y=100,.width=100,.height=80,.anchor_x=100,.anchor_y=80,.anchor_width=20,.anchor_height=20,.shadow=8};
    nz_damage_rect area;
    assert(nz_popup_work_area(owner,100,80,20,20,&area));
    assert(area.width>0 && area.height>0);
    nz_window *popup=nz_popup_create(owner,&placement); assert(popup);
    assert(XGrabPointer(display,DefaultRootWindow(display),False,0,GrabModeAsync,GrabModeAsync,None,None,CurrentTime)==GrabSuccess);
    XUngrabPointer(display,CurrentTime); XSync(display,False);
    nz_window_pointer_capture(owner,0);
    assert(XGrabPointer(display,DefaultRootWindow(display),False,0,GrabModeAsync,GrabModeAsync,None,None,CurrentTime)==GrabSuccess);
    XUngrabPointer(display,CurrentTime); XSync(display,False);
    Window id=(Window)(uintptr_t)nz_window_native_surface(popup);
    XWindowAttributes attributes; XSync(display,False);
    assert(XGetWindowAttributes(display,id,&attributes) && attributes.override_redirect);
    nz_buffer_view view; assert(nz_window_acquire(popup,116,96,&view)>0);
    memset(view.pixels,0xff,(size_t)view.stride*view.height); nz_window_present(popup,1,0);
    nz_popup_placement nested={.x=90,.y=20,.width=60,.height=40,.anchor_x=80,.anchor_y=20,.anchor_width=20,.anchor_height=20,.shadow=8,.side=2};
    nz_window *child=nz_popup_create(popup,&nested); assert(child);
    nz_window_destroy(child);
    placement.y=120; assert(nz_popup_place(popup,&placement));
    nz_window_destroy(popup);
    Window active=(Window)(uintptr_t)nz_window_native_surface(owner);
    XChangeProperty(display,DefaultRootWindow(display),XInternAtom(display,"_NET_ACTIVE_WINDOW",False),XA_WINDOW,32,PropModeReplace,(unsigned char*)&active,1);
    XSync(display,False);
    int activated=0; double deadline=now()+2; nz_event event;
    while(!activated && now()<deadline) {
        assert(nz_dispatch(c,10)==0);
        while(nz_next_event(c,&event)) if(event.window==owner && event.type==NZ_WINDOW_ACTIVATION && event.a==1) activated=1;
    }
    assert(activated);
    active=None;
    XChangeProperty(display,DefaultRootWindow(display),XInternAtom(display,"_NET_ACTIVE_WINDOW",False),XA_WINDOW,32,PropModeReplace,(unsigned char*)&active,1);
    XSync(display,False);
    int deactivated=0; deadline=now()+2;
    while(!deactivated && now()<deadline) {
        assert(nz_dispatch(c,10)==0);
        while(nz_next_event(c,&event)) if(event.window==owner && event.type==NZ_WINDOW_ACTIVATION && event.a==0) deactivated=1;
    }
    assert(deactivated);
    XSync(display,False);
    assert(nz_dispatch(c,0)==0);
}

int main(int argc,char **argv) {
    assert(nz_abi_version()==5 && nz_event_size()==sizeof(nz_event));
    assert(nz_decoration_support(NULL)==0 && nz_window_decoration_mode(NULL)==0);
    if(argc>1 && !strcmp(argv[1],"--no-display")) setenv("DISPLAY","unix/:59999",1);
    char error[512]; nz_connection *c=nz_connect(error,sizeof(error));
    if(argc>1 && !strcmp(argv[1],"--no-display")) { assert(!c && strstr(error,"X11")); puts("PASS: X11 missing-display diagnostic and ABI."); return 0; }
    assert(c);
#ifdef NAZOR_X11_NESTED_TESTS
    if(argc>1 && !strcmp(argv[1],"--outer-resize")) { verify_xwayland_outer_resize(c); nz_disconnect(c); return 0; }
#endif
    nz_window *w=nz_window_create(c,"Nazor X11 native test","org.nazor.Tests",160,100,NULL,1,0); assert(w);
    nz_window *owned=nz_window_create(c,"Owned","org.nazor.Tests",80,60,w,1,0); assert(owned);
    nz_window_show(w,1);
    int configured=0; nz_event e; double deadline=now()+30;
    while(!configured && now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) if(e.type==NZ_CONFIGURE && e.window==w) configured=1; }
    assert(configured);
    nz_window_show(owned,1); nz_window_show(owned,0);
    Display *display=nz_window_native_display(w); Window id=(Window)(uintptr_t)nz_window_native_surface(w);
    XWindowAttributes attributes;
    deadline=now()+3;
    while(now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) { } XGetWindowAttributes(display,id,&attributes); if(attributes.map_state==IsViewable) break; }
    assert(attributes.map_state==IsViewable);
    verify_client_frame(c,w,display,id,attributes);
    nz_window_titlebar(w,0);
    deadline=now()+3;
    while(nz_window_decoration_mode(w)!=2 && now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) { } }
    assert(nz_window_decoration_mode(w)==2);
    verify_missing_frame_extents(w,display,id,0);
    nz_window_titlebar(w,1);
    deadline=now()+3;
    while(nz_window_decoration_mode(w)!=1 && now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) { } }
    assert(nz_window_decoration_mode(w)==1);
    verify_nested_frame_observation(c,display);
    nz_window *client=nz_window_create(c,"Nazor frame resize","org.nazor.Tests",160,100,NULL,1,1); assert(client);
    assert(nz_window_decoration_mode(client)==0);
    nz_window_show(client,1);
    Window client_id=(Window)(uintptr_t)nz_window_native_surface(client);
    XWindowAttributes client_attributes;
    deadline=now()+3;
    do {
        assert(nz_dispatch(c,20)==0);
        while(nz_next_event(c,&e)) { }
        assert(XGetWindowAttributes(display,client_id,&client_attributes));
    } while(client_attributes.map_state!=IsViewable && now()<deadline);
    assert(client_attributes.map_state==IsViewable);
    verify_client_frame(c,client,display,client_id,client_attributes);
    nz_window_destroy(client);
    nz_buffer_view view; assert(nz_window_acquire(w,160,100,&view)==1); assert(view.width==160 && view.stride==640);
    uint32_t *pixels=view.pixels; for(int i=0;i<160*100;i++) pixels[i]=0xff336699;
    nz_window_present(w,1,1); assert(nz_window_buffer_is_current(w));
    XSync(display,False); XImage *image=XGetImage(display,id,0,0,1,1,AllPlanes,ZPixmap); assert(image); assert((XGetPixel(image,0,0)&0xffffff)==0x336699); XDestroyImage(image);
    pixels[0]=0xffaa5522; nz_damage_rect rect={0,0,1,1}; nz_window_present_damage(w,1,1,&rect,1); XSync(display,False);
    image=XGetImage(display,id,0,0,2,1,AllPlanes,ZPixmap); assert((XGetPixel(image,0,0)&0xffffff)==0xaa5522 && (XGetPixel(image,1,0)&0xffffff)==0x336699); XDestroyImage(image);
    verify_minimize_restore(c,w);
    assert(nz_clipboard_set(c,"Nazor è 😀")); assert(nz_clipboard_request(c));
    int clipboard=0; while(nz_next_event(c,&e)) if(e.type==NZ_CLIPBOARD) { assert(!strcmp(e.text,"Nazor è 😀")); clipboard=1; } assert(clipboard);
    XEvent key={0}; key.xkey=(XKeyEvent){.type=KeyPress,.display=display,.window=id,.root=DefaultRootWindow(display),.keycode=XKeysymToKeycode(display,XK_a),.same_screen=True};
    XSendEvent(display,id,False,KeyPressMask,&key); XSendEvent(display,id,False,KeyPressMask,&key);
    key.xkey.type=KeyRelease; XSendEvent(display,id,False,KeyReleaseMask,&key); XFlush(display);
    int keys=0,repeat=0; deadline=now()+3;
    while(keys<3 && now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) if(e.type==NZ_KEY && e.window==w) { keys++; repeat+=e.d; assert(!strcmp(e.text,"a")); } }
    assert(keys==3 && repeat==1);
    XEvent close={0}; close.xclient.type=ClientMessage; close.xclient.window=id; close.xclient.message_type=XInternAtom(display,"WM_PROTOCOLS",False); close.xclient.format=32; close.xclient.data.l[0]=(long)XInternAtom(display,"WM_DELETE_WINDOW",False); XSendEvent(display,id,False,0,&close); XFlush(display);
    int closed=0; deadline=now()+3; while(!closed && now()<deadline) { assert(nz_dispatch(c,20)==0); while(nz_next_event(c,&e)) if(e.type==NZ_CLOSE && e.window==w) closed=1; } assert(closed);
    verify_argb(c,display);
    verify_foreign_parent(w);
    verify_popups(c,w,display);
    nz_window_destroy(owned); nz_window_destroy(w);
    while(nz_next_event(c,&e)) { }
    pthread_t thread; assert(!pthread_create(&thread,NULL,wake_loop,c)); double started=now(); assert(nz_dispatch(c,1000)==0); assert(now()-started<.8); pthread_join(thread,NULL);
    XChangeProperty(display,DefaultRootWindow(display),XInternAtom(display,"_NAZOR_INVALID_TEST",False),XA_STRING,7,PropModeReplace,NULL,0); XSync(display,False);
    assert(nz_dispatch(c,0)<0 && strstr(nz_error(c),"X11 protocol error"));
    nz_disconnect(c); puts("PASS: X11 windows, CPU and ARGB pixels, damage, keyboard/repeat, clipboard, close, wake and protocol errors.");
}
