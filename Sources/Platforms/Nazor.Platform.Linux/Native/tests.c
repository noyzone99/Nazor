#include "nazor-wayland.h"
#undef NDEBUG
#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static double monotonic_seconds(void) {
    struct timespec value;
    clock_gettime(CLOCK_MONOTONIC,&value);
    return value.tv_sec+value.tv_nsec/1000000000.0;
}

static void verify_popups(nz_connection *c, nz_window *owner) {
    nz_popup_placement placement={.x=20,.y=100,.width=120,.height=80,.anchor_x=20,.anchor_y=80,.anchor_width=80,.anchor_height=20,.shadow=8};
    nz_window *popup=nz_popup_create(owner,&placement);
    assert(popup);
    nz_event event; int configured=0,framed=0;
    double deadline=monotonic_seconds()+5;
    while(!framed && monotonic_seconds()<deadline) {
        assert(nz_dispatch(c,30)==0);
        while(nz_next_event(c,&event)) {
            if(event.window==popup && event.type==NZ_POPUP_CONFIGURE) {
                assert(event.a>0 && event.b>0);
                nz_buffer_view view; assert(nz_window_acquire(popup,event.a+16,event.b+16,&view)>0);
                memset(view.pixels,0xff,(size_t)view.stride*view.height);
                nz_window_present(popup,1,1); configured=1;
            }
            if(event.window==popup && event.type==NZ_FRAME) framed=1;
        }
    }
    assert(configured && framed);
    placement.anchor_x=80; placement.anchor_y=20; placement.anchor_width=20; placement.anchor_height=20;
    placement.width=60; placement.height=50; placement.side=2;
    nz_window *child=nz_popup_create(popup,&placement); assert(child);
    configured=0; deadline=monotonic_seconds()+5;
    while(!configured && monotonic_seconds()<deadline) {
        assert(nz_dispatch(c,30)==0);
        while(nz_next_event(c,&event)) if(event.window==child && event.type==NZ_POPUP_CONFIGURE) configured=1;
    }
    assert(configured);
    nz_window_destroy(child);
    placement.width=100; placement.side=0; assert(nz_popup_place(popup,&placement));
    assert(nz_dispatch(c,30)==0);
    nz_window_destroy(popup);
    assert(nz_dispatch(c,0)==0);
    placement.flags=1; placement.serial=0; assert(!nz_popup_create(owner,&placement));
}

int main(void) {
    assert(nz_abi_version()==5);
    assert(!nz_window_foreign_parent(NULL,NULL));
    assert(nz_decoration_support(NULL)==0);
    assert(nz_window_decoration_mode(NULL)==0);
    assert(nz_event_size()==sizeof(nz_event));
    nz_event event;
    assert(!nz_next_event(NULL,&event));
    char error[512]={0};
    nz_connection *c=nz_connect(error,sizeof(error));
    if (!getenv("NAZOR_TEST_COMPOSITOR")) {
        assert(!c);
        assert(strstr(error,"Wayland"));
        puts("ABI and missing-compositor diagnostics passed.");
        return 0;
    }
    if (!c) { fprintf(stderr,"%s\n",error); return 1; }
    nz_window *w=nz_window_create(c,"Nazor native test","org.nazor.Tests",160,100,NULL,1,0);
    assert(w);
    nz_window *owned=nz_window_create(c,"Owned test","org.nazor.Tests",120,80,w,1,0);
    assert(owned);
    nz_window *splash=nz_window_create(c,"Borderless test","org.nazor.Tests",100,60,NULL,0,1);
    assert(splash);
    nz_window_show(splash,1);
    nz_window_show(w,1);
    nz_window_show(owned,1);
    nz_window_resize(w,200,140);
    nz_window_show(owned,0);
    nz_window_show(owned,1);
    int configured=0, frames=0, submitted=0;
    char exported[4096]={0};
    double deadline=monotonic_seconds()+5;
    while (!frames && monotonic_seconds()<deadline) {
        assert(nz_dispatch(c,50)==0);
        while (nz_next_event(c,&event)) {
            if (event.type==18 && event.window==w && event.text) snprintf(exported,sizeof(exported),"wayland:%s",event.text);
            if (event.type==NZ_CONFIGURE) configured=1;
            if (event.type==NZ_FRAME) frames++;
        }
        if (configured && !submitted) {
            nz_buffer_view view;
            int result=nz_window_acquire(w,160,100,&view);
            assert(result>=0);
            if (result) { memset(view.pixels,0xff,(size_t)view.stride*view.height); nz_window_present(w,1,1); submitted=1; }
        }
    }
    assert(configured && frames);
    assert(!nz_window_foreign_parent(owned,"x11:1"));
    if (exported[0]) (void)nz_window_foreign_parent(owned,exported);
    assert(nz_dispatch(c,50)==0);
    verify_popups(c,w);
    nz_window_outer_resize(w,8);
    assert(nz_dispatch(c,50)==0);
    nz_window_outer_resize(w,0);
    assert(nz_dispatch(c,50)==0);
    assert((nz_window_capabilities(splash)&32u)==0); /* No negotiated server decoration. */
    nz_window_destroy(splash);
    nz_window_show(w,0);
    nz_window_show(w,1);
    nz_window_destroy(owned);
    nz_window_destroy(w);
    nz_dispatch(c,0);
    nz_disconnect(c);
    puts("Configure, shared-memory presentation, frame callback and cleanup passed.");
    return 0;
}
