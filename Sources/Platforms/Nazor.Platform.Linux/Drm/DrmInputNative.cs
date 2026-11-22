using System.Runtime.InteropServices;

namespace Nazor.Platform.Linux;

internal static unsafe class DrmInputNative
{
    private const string Input = "libinput.so.10", Xkb = "libxkbcommon.so.0";
    [StructLayout(LayoutKind.Sequential)] internal struct InputInterface { internal nint Open, Close; }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int OpenDevice(nint path, int flags, nint user);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void CloseDevice(int fd, nint user);
    [DllImport("libudev.so.1")] internal static extern nint udev_new();
    [DllImport("libudev.so.1")] internal static extern nint udev_unref(nint udev);
    [DllImport(Input)] internal static extern nint libinput_udev_create_context(InputInterface* callbacks, nint user, nint udev);
    [DllImport(Input)] internal static extern int libinput_udev_assign_seat(nint input, [MarshalAs(UnmanagedType.LPUTF8Str)] string seat);
    [DllImport(Input)] internal static extern int libinput_get_fd(nint input);
    [DllImport(Input)] internal static extern int libinput_dispatch(nint input);
    [DllImport(Input)] internal static extern nint libinput_get_event(nint input);
    [DllImport(Input)] internal static extern int libinput_event_get_type(nint value);
    [DllImport(Input)] internal static extern void libinput_event_destroy(nint value);
    [DllImport(Input)] internal static extern nint libinput_unref(nint input);
    [DllImport(Input)] internal static extern void libinput_suspend(nint input);
    [DllImport(Input)] internal static extern int libinput_resume(nint input);
    [DllImport(Input)] internal static extern nint libinput_event_get_pointer_event(nint value);
    [DllImport(Input)] internal static extern double libinput_event_pointer_get_dx(nint value);
    [DllImport(Input)] internal static extern double libinput_event_pointer_get_dy(nint value);
    [DllImport(Input)] internal static extern double libinput_event_pointer_get_absolute_x_transformed(nint value, uint width);
    [DllImport(Input)] internal static extern double libinput_event_pointer_get_absolute_y_transformed(nint value, uint height);
    [DllImport(Input)] internal static extern uint libinput_event_pointer_get_button(nint value);
    [DllImport(Input)] internal static extern int libinput_event_pointer_get_button_state(nint value);
    [DllImport(Input)] internal static extern uint libinput_event_pointer_get_seat_button_count(nint value);
    [DllImport(Input)] internal static extern int libinput_event_pointer_has_axis(nint value, uint axis);
    [DllImport(Input)] internal static extern double libinput_event_pointer_get_axis_value(nint value, uint axis);
    [DllImport(Input)] internal static extern nint libinput_event_get_touch_event(nint value);
    [DllImport(Input)] internal static extern int libinput_event_touch_get_seat_slot(nint value);
    [DllImport(Input)] internal static extern double libinput_event_touch_get_x_transformed(nint value, uint width);
    [DllImport(Input)] internal static extern double libinput_event_touch_get_y_transformed(nint value, uint height);
    [DllImport(Input)] internal static extern nint libinput_event_get_keyboard_event(nint value);
    [DllImport(Input)] internal static extern uint libinput_event_keyboard_get_key(nint value);
    [DllImport(Input)] internal static extern int libinput_event_keyboard_get_key_state(nint value);
    [DllImport(Input)] internal static extern uint libinput_event_keyboard_get_seat_key_count(nint value);
    [DllImport(Xkb)] internal static extern nint xkb_context_new(int flags);
    [DllImport(Xkb)] internal static extern void xkb_context_unref(nint context);
    [DllImport(Xkb)] internal static extern nint xkb_keymap_new_from_names(nint context, nint names, int flags);
    [DllImport(Xkb)] internal static extern void xkb_keymap_unref(nint keymap);
    [DllImport(Xkb)] internal static extern nint xkb_state_new(nint keymap);
    [DllImport(Xkb)] internal static extern void xkb_state_unref(nint state);
    [DllImport(Xkb)] internal static extern uint xkb_state_update_key(nint state, uint key, int direction);
    [DllImport(Xkb)] internal static extern uint xkb_state_key_get_one_sym(nint state, uint key);
    [DllImport(Xkb)] internal static extern int xkb_keymap_key_repeats(nint keymap, uint key);
    [DllImport(Xkb)] internal static extern int xkb_state_mod_name_is_active(nint state, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int type);
    [DllImport(Xkb)] internal static extern uint xkb_keysym_to_utf32(uint symbol);
    [DllImport(Xkb)] internal static extern int xkb_keysym_to_utf8(uint symbol, byte* buffer, nuint size);
    [DllImport(Xkb)] internal static extern int xkb_keysym_get_name(uint symbol, byte* buffer, nuint size);
    [DllImport(Xkb)] internal static extern nint xkb_compose_table_new_from_locale(nint context, [MarshalAs(UnmanagedType.LPUTF8Str)] string locale, int flags);
    [DllImport(Xkb)] internal static extern void xkb_compose_table_unref(nint table);
    [DllImport(Xkb)] internal static extern nint xkb_compose_state_new(nint table, int flags);
    [DllImport(Xkb)] internal static extern void xkb_compose_state_unref(nint compose);
    [DllImport(Xkb)] internal static extern int xkb_compose_state_feed(nint compose, uint symbol);
    [DllImport(Xkb)] internal static extern int xkb_compose_state_get_status(nint compose);
    [DllImport(Xkb)] internal static extern int xkb_compose_state_get_utf8(nint compose, byte* buffer, nuint size);
    [DllImport(Xkb)] internal static extern void xkb_compose_state_reset(nint compose);
}
