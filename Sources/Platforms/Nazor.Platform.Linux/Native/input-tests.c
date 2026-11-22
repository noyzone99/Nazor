#undef NDEBUG
#include "nazor-wayland.c"
#include <assert.h>
#include <sys/socket.h>

static size_t read_requests(nz_connection *connection,int socket,uint32_t *requests,size_t capacity) {
    assert(wl_display_flush(connection->display)>=0);
    size_t bytes=0;
    for (;;) {
        assert(bytes<capacity*sizeof(*requests));
        ssize_t count=recv(socket,(char*)requests+bytes,capacity*sizeof(*requests)-bytes,MSG_DONTWAIT);
        if (count<0 && errno==EINTR) continue;
        if (count<0 && (errno==EAGAIN || errno==EWOULDBLOCK)) break;
        assert(count>0);
        bytes+=(size_t)count;
    }
    assert(bytes%sizeof(*requests)==0);
    return bytes/sizeof(*requests);
}

static const uint32_t *expect_request(const uint32_t **next,const uint32_t *end,uint32_t id,uint32_t opcode) {
    while (*next<end) {
        const uint32_t *request=*next;
        assert(end-request>=2);
        size_t bytes=request[1]>>16;
        assert(bytes>=8 && bytes%4==0 && bytes/4<=(size_t)(end-request));
        *next+=bytes/4;
        if (request[0]==id) {
            assert((request[1]&UINT16_MAX)==opcode);
            return request+2;
        }
    }
    assert(!"Expected Wayland request was not sent");
    return NULL;
}

static void expect_remap_and_text_context(void) {
    int sockets[2];
    assert(socketpair(AF_UNIX,SOCK_STREAM,0,sockets)==0);
    nz_connection *c=calloc(1,sizeof(*c));
    assert(c && pthread_mutex_init(&c->render_lock,NULL)==0);
    c->clipboard_fd=-1; c->wake_fd=-1;
    c->display=wl_display_connect_to_fd(sockets[0]);
    assert(c->display);
    c->registry=wl_display_get_registry(c->display);
    global(c,c->registry,1,"wl_compositor",4);
    global(c,c->registry,2,"xdg_wm_base",6);
    global(c,c->registry,3,"wl_seat",5);
    global(c,c->registry,4,"zwp_text_input_manager_v3",1);

    nz_window *parent=nz_window_create(c,"Parent","org.nazor.Tests",640,480,NULL,1,0);
    nz_window *window=nz_window_create(c,"Original","org.nazor.Tests",320,240,parent,1,0);
    assert(parent && window);
    uint32_t requests[512];
    nz_window_show(parent,1);
    shell_configure(parent,parent->shell,1);
    nz_window_wayland_gpu_start(parent);
    assert(nz_window_wayland_gpu_prepare(parent,640,480)==1 && !parent->mapped);
    (void)read_requests(c,sockets[1],requests,512);
    wl_surface_commit(parent->surface);
    nz_window_wayland_gpu_presented(parent);
    assert(dispatch_pending(c)==0 && parent->mapped);
    const uint32_t *next=requests, *end=requests+read_requests(c,sockets[1],requests,512);
    uint32_t id=wl_proxy_get_id((struct wl_proxy*)window->toplevel);
    uint32_t parent_id=wl_proxy_get_id((struct wl_proxy*)parent->toplevel);
    expect_request(&next,end,wl_proxy_get_id((struct wl_proxy*)parent->surface),WL_SURFACE_COMMIT);
    assert(*expect_request(&next,end,id,XDG_TOPLEVEL_SET_PARENT)==parent_id);

    nz_window_operations(window,0,320,240);
    nz_window_show(window,1);
    nz_window_state(window,NZ_STATE_MAXIMIZED);
    window->mapped=1;
    nz_window_show(window,0);
    nz_window_title(window,"Reshown");

    (void)read_requests(c,sockets[1],requests,512);
    nz_window_show(window,1);
    next=requests; end=requests+read_requests(c,sockets[1],requests,512);
    assert(strcmp((const char*)(expect_request(&next,end,id,XDG_TOPLEVEL_SET_TITLE)+1),"Reshown")==0);
    assert(strcmp((const char*)(expect_request(&next,end,id,XDG_TOPLEVEL_SET_APP_ID)+1),"org.nazor.Tests")==0);
    const uint32_t *size=expect_request(&next,end,id,XDG_TOPLEVEL_SET_MIN_SIZE);
    assert(size[0]==320 && size[1]==240);
    size=expect_request(&next,end,id,XDG_TOPLEVEL_SET_MAX_SIZE);
    assert(size[0]==320 && size[1]==240);
    assert(*expect_request(&next,end,id,XDG_TOPLEVEL_SET_PARENT)==parent_id);
    expect_request(&next,end,id,XDG_TOPLEVEL_UNSET_FULLSCREEN);
    expect_request(&next,end,id,XDG_TOPLEVEL_SET_MAXIMIZED);

    c->text_window=window;
    nz_window_wayland_text_context(window,1,7,0,0,0,1,10,"abc",2,2);
    (void)read_requests(c,sockets[1],requests,512);
    nz_window_wayland_text_context(window,1,8,1,0,0,1,10,"never sent",0,0);
    next=requests; end=requests+read_requests(c,sockets[1],requests,512);
    id=wl_proxy_get_id((struct wl_proxy*)c->text_input);
    expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_DISABLE);
    expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_COMMIT);
    expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_ENABLE);
    const uint32_t *type=expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_SET_CONTENT_TYPE);
    assert(type[0]==(ZWP_TEXT_INPUT_V3_CONTENT_HINT_HIDDEN_TEXT|ZWP_TEXT_INPUT_V3_CONTENT_HINT_SENSITIVE_DATA));
    assert(type[1]==ZWP_TEXT_INPUT_V3_CONTENT_PURPOSE_PASSWORD);
    expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_SET_CURSOR_RECTANGLE);
    expect_request(&next,end,id,ZWP_TEXT_INPUT_V3_COMMIT);
    assert(next==end);

    nz_disconnect(c);
    close(sockets[1]);
}

static void expect_configure_and_gpu_retry(void) {
    nz_connection c={.render_lock=PTHREAD_MUTEX_INITIALIZER,.wake_fd=eventfd(0,EFD_CLOEXEC|EFD_NONBLOCK)};
    assert(c.wake_fd>=0);
    nz_window w={.connection=&c,.width=641,.height=480,.normal_width=641,.normal_height=480,.scale=2,.visible=1};
    struct wl_array states;
    wl_array_init(&states);
    uint32_t *maximized=wl_array_add(&states,sizeof(*maximized));
    assert(maximized);
    *maximized=XDG_TOPLEVEL_STATE_MAXIMIZED;
    toplevel_configure(&w,NULL,1024,768,&states);
    shell_configure(&w,NULL,1);
    wl_array_release(&states);
    assert(w.width==1024 && w.height==768);
    wl_array_init(&states);
    toplevel_configure(&w,NULL,0,0,&states);
    shell_configure(&w,NULL,2);
    assert(w.width==641 && w.height==480 && w.configure_pending && w.configure_serial==2);

    nz_window_wayland_gpu_start(&w);
    assert(nz_window_wayland_gpu_prepare(&w,641,480)==0);
    assert(w.gpu_retry && w.gpu_frame_requested && w.configure_pending);
    uint64_t wake;
    assert(read(c.wake_fd,&wake,sizeof(wake))==(ssize_t)sizeof(wake));
    nz_event value;
    while (nz_next_event(&c,&value)) { }
    close(c.wake_fd);
    pthread_mutex_destroy(&c.render_lock);
}

static void expect_clipboard_cancellation_releases_the_pipe(void) {
    int pipes[2];
    assert(pipe(pipes)==0);
    nz_connection c={.clipboard_fd=pipes[0],.received=strdup("partial"),.received_length=7};
    assert(c.received && nz_clipboard_request(&c)==0);
    nz_clipboard_cancel(&c);
    assert(fcntl(pipes[0],F_GETFD)==-1 && errno==EBADF);
    assert(c.clipboard_fd==-1 && !c.received && c.received_length==0);
    assert(nz_clipboard_request(&c)==1);
    nz_event value;
    assert(nz_next_event(&c,&value) && value.type==NZ_CLIPBOARD);
    assert(!nz_next_event(&c,&value));
    close(pipes[1]);
}

static void expect_compositor_binding(void) {
    int sockets[2];
    assert(socketpair(AF_UNIX,SOCK_STREAM,0,sockets)==0);
    nz_connection connection={0};
    connection.display=wl_display_connect_to_fd(sockets[0]);
    assert(connection.display);
    connection.registry=wl_display_get_registry(connection.display);
    global(&connection,connection.registry,1,"wl_compositor",6);
    assert(connection.compositor);
    /* Run this old-header binary with a newer libwayland to catch runtime-only version checks. */
    if (wl_compositor_get_version(connection.compositor)>=6)
        assert(sizeof(surface_listener)>=4*sizeof(void (*)(void)));
    wl_compositor_destroy(connection.compositor);
    wl_registry_destroy(connection.registry);
    wl_display_disconnect(connection.display);
    close(sockets[1]);
}

static void expect_key(nz_connection *connection, uint32_t code, const char *text) {
    nz_event event;
    keyboard_key(connection,NULL,1,1,code,WL_KEYBOARD_KEY_STATE_PRESSED);
    assert(nz_next_event(connection,&event) && event.type==NZ_KEY);
    if (text) {
        assert(nz_next_event(connection,&event) && event.type==NZ_TEXT);
        assert(strcmp(event.text,text)==0);
    }
    assert(!nz_next_event(connection,&event));
}

static void expect_outer_resize_coordinates(void) {
    nz_connection connection={0};
    nz_window window={.connection=&connection,.resize_margin=8};
    struct wl_surface *ring=(struct wl_surface *)&window;
    window.resize_surface=ring;
    connection.windows=&window;
    pointer_enter(&connection,NULL,1,ring,wl_fixed_from_int(3),wl_fixed_from_int(20));
    pointer_motion(&connection,NULL,2,wl_fixed_from_int(4),wl_fixed_from_int(21));
    pointer_button(&connection,NULL,3,4,272,1);
    pointer_axis(&connection,NULL,5,0,wl_fixed_from_int(10));
    pointer_leave(&connection,NULL,6,ring);
    nz_event event;
    assert(nz_next_event(&connection,&event) && event.type==NZ_POINTER_ENTER && event.window==&window);
    assert(event.flags==NZ_POINTER_FRAME_OUTSET && event.x==-5 && event.y==12);
    assert(nz_next_event(&connection,&event) && event.type==NZ_POINTER_MOTION && event.x==-4 && event.y==13);
    assert(nz_next_event(&connection,&event) && event.type==NZ_POINTER_BUTTON && event.d==3);
    assert(event.flags==NZ_POINTER_FRAME_OUTSET && event.x==-4 && event.y==13);
    assert(nz_next_event(&connection,&event) && event.type==NZ_POINTER_LEAVE);
    assert(!nz_next_event(&connection,&event));
}

int main(void) {
    expect_compositor_binding();
    expect_outer_resize_coordinates();
    expect_remap_and_text_context();
    expect_configure_and_gpu_retry();
    expect_clipboard_cancellation_releases_the_pipe();
    nz_connection connection={0};
    nz_window window={.connection=&connection};
    connection.keyboard_window=&window;
    connection.text_window=&window;
    connection.xkb=xkb_context_new(XKB_CONTEXT_NO_FLAGS);
    assert(connection.xkb);
    struct xkb_rule_names names={.layout="us",.variant="intl"};
    connection.keymap=xkb_keymap_new_from_names(connection.xkb,&names,XKB_KEYMAP_COMPILE_NO_FLAGS);
    assert(connection.keymap);
    connection.key_state=xkb_state_new(connection.keymap);
    assert(connection.key_state);
    expect_key(&connection,30,"a");
    connection.text_enabled=1;
    expect_key(&connection,30,"a");
    connection.modifiers=2;
    expect_key(&connection,30,NULL);
    connection.modifiers=0;
    connection.compose_table=xkb_compose_table_new_from_locale(connection.xkb,"en_US.UTF-8",XKB_COMPOSE_COMPILE_NO_FLAGS);
    assert(connection.compose_table);
    connection.compose=xkb_compose_state_new(connection.compose_table,XKB_COMPOSE_STATE_NO_FLAGS);
    assert(connection.compose);
    expect_key(&connection,40,NULL);
    expect_key(&connection,18,"\xc3\xa9");

    text_preedit(&connection,NULL,"\xe3\x81\x82",3,3);
    text_commit(&connection,NULL,"\xe6\x97\xa5");
    text_delete(&connection,NULL,2,1);
    nz_event event;
    assert(!nz_next_event(&connection,&event));
    text_done(&connection,NULL,1);
    assert(nz_next_event(&connection,&event) && event.type==NZ_TEXT_DELETE && event.a==2 && event.b==1);
    assert(nz_next_event(&connection,&event) && event.type==NZ_TEXT && event.flags==1 && strcmp(event.text,"\xe6\x97\xa5")==0);
    assert(nz_next_event(&connection,&event) && event.type==NZ_PREEDIT && event.a==3 && strcmp(event.text,"\xe3\x81\x82")==0);
    assert(!nz_next_event(&connection,&event));
    text_done(&connection,NULL,2);
    assert(!nz_next_event(&connection,&event));
    xkb_compose_state_unref(connection.compose);
    xkb_compose_table_unref(connection.compose_table);
    xkb_state_unref(connection.key_state);
    xkb_keymap_unref(connection.keymap);
    xkb_context_unref(connection.xkb);
    puts("Wayland remapping, GPU configure, clipboard, keyboard and IME tests passed.");
    return 0;
}
