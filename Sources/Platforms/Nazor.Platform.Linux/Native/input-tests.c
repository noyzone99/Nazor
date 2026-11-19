#undef NDEBUG
#include "nazor-wayland.c"
#include <assert.h>
#include <sys/socket.h>

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
    puts("Compositor binding, keyboard fallback, shortcuts, compose and IME transaction tests passed.");
    return 0;
}
