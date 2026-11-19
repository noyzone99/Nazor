using System.Diagnostics;
using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using static Nazor.Platform.Linux.DrmInputNative;

namespace Nazor.Platform.Linux;

internal enum DrmEventKind { Move, Press, Release, Scroll, KeyDown, KeyUp, Cancel }
internal readonly record struct DrmEvent(DrmEventKind Kind, Point Point = default, PointerButton Button = default,
    KeyModifiers Modifiers = default, string? Key = null, string? Text = null, bool Repeat = false,
    float DeltaX = 0, float DeltaY = 0, bool Touch = false);

internal sealed unsafe class DrmInput : IDisposable
{
    private readonly DrmSession session;
    private readonly Action<DrmEvent> deliver;
    private readonly int width, height;
    private readonly OpenDevice openDevice;
    private readonly CloseDevice closeDevice;
    private readonly Dictionary<uint, int> pressedKeys = [], pressedButtons = [];
    private nint udev, input, xkb, keymap, state, composeTable, compose;
    private Exception? callbackFailure;
    private double x, y;
    private int touchSlot = -1;
    private uint repeatCode;
    private string? repeatText;
    private bool repeatComposing, repeatComposed;
    private long repeatAt = long.MaxValue;
    internal int Fd => libinput_get_fd(input);

    internal DrmInput(DrmSession session, int width, int height, Action<DrmEvent> deliver)
    {
        this.session = session; this.width = width; this.height = height; this.deliver = deliver;
        x = width / 2.0; y = height / 2.0;
        openDevice = (path, flags, _) =>
        {
            try
            {
                var device = session.OpenDevice(Marshal.PtrToStringUTF8(path)!);
                if (device.Id < 0) return -Math.Max(1, Marshal.GetLastPInvokeError());
                var currentFlags = DrmNative.fcntl(device.Fd, 3, 0);
                if (currentFlags < 0 || DrmNative.fcntl(device.Fd, 4, currentFlags | (flags & 0x800)) < 0)
                {
                    var error = Marshal.GetLastPInvokeError();
                    session.CloseDevice(device.Id);
                    return -Math.Max(1, error);
                }
                return device.Fd;
            }
            catch (Exception error) { callbackFailure = error; return -5; }
        };
        closeDevice = (fd, _) =>
        {
            try { session.CloseDescriptor(fd); }
            catch (Exception error) { callbackFailure = error; }
        };
        try
        {
            xkb = xkb_context_new(0);
            if (xkb == 0) throw new InvalidOperationException("Cannot create the xkbcommon context.");
            keymap = xkb_keymap_new_from_names(xkb, 0, 0);
            if (keymap == 0) throw new InvalidOperationException("Cannot load the keyboard layout. Check xkeyboard-config and XKB_DEFAULT_LAYOUT.");
            ResetKeyboard();
            var locale = Environment.GetEnvironmentVariable("LC_ALL");
            if (string.IsNullOrEmpty(locale)) locale = Environment.GetEnvironmentVariable("LC_CTYPE");
            if (string.IsNullOrEmpty(locale)) locale = Environment.GetEnvironmentVariable("LANG");
            composeTable = xkb_compose_table_new_from_locale(xkb, string.IsNullOrEmpty(locale) ? "C.UTF-8" : locale, 0);
            if (composeTable != 0) compose = xkb_compose_state_new(composeTable, 0);
            udev = udev_new();
            if (udev == 0) throw new InvalidOperationException("Cannot create the input device monitor.");
            var callbacks = new InputInterface { Open = Marshal.GetFunctionPointerForDelegate(openDevice), Close = Marshal.GetFunctionPointerForDelegate(closeDevice) };
            input = libinput_udev_create_context(ref callbacks, 0, udev);
            if (input == 0 || libinput_udev_assign_seat(input, session.Name) != 0)
                throw new InvalidOperationException("Cannot initialize libinput for the active seat.");
            ThrowCallbackFailure();
        }
        catch { Dispose(); throw; }
    }

    private KeyModifiers Modifiers => (xkb_state_mod_name_is_active(state, "Shift", 8) > 0 ? KeyModifiers.Shift : 0) |
        (xkb_state_mod_name_is_active(state, "Control", 8) > 0 ? KeyModifiers.Control : 0) |
        (xkb_state_mod_name_is_active(state, "Mod1", 8) > 0 ? KeyModifiers.Alt : 0) |
        (xkb_state_mod_name_is_active(state, "Mod4", 8) > 0 ? KeyModifiers.Meta : 0);

    internal void Dispatch()
    {
        if (libinput_dispatch(input) < 0) throw new InvalidOperationException("Cannot read input devices.");
        ThrowCallbackFailure();
        nint value;
        while ((value = libinput_get_event(input)) != 0)
        {
            try { Process(value, libinput_event_get_type(value)); }
            finally { libinput_event_destroy(value); }
        }
    }

    private void Process(nint value, int type)
    {
        if (type == 2) { Cancel(); ResetKeyboard(); return; }
        if (type == 300)
        {
            var keyboard = libinput_event_get_keyboard_event(value);
            Key(libinput_event_keyboard_get_key(keyboard), libinput_event_keyboard_get_key_state(keyboard) != 0);
            return;
        }
        if (type is >= 400 and <= 403)
        {
            var pointer = libinput_event_get_pointer_event(value);
            if (type == 400) { x += libinput_event_pointer_get_dx(pointer); y += libinput_event_pointer_get_dy(pointer); }
            if (type == 401) { x = libinput_event_pointer_get_absolute_x_transformed(pointer, (uint)width); y = libinput_event_pointer_get_absolute_y_transformed(pointer, (uint)height); }
            ClampPointer();
            if (type is 400 or 401) deliver(new(DrmEventKind.Move, new((float)x, (float)y), Modifiers: Modifiers));
            if (type == 402)
            {
                var down = libinput_event_pointer_get_button_state(pointer) != 0;
                var code = libinput_event_pointer_get_button(pointer);
                if (UpdatePressed(pressedButtons, code, down))
                {
                    var button = code switch { 272 => PointerButton.Primary, 273 => PointerButton.Secondary, 274 => PointerButton.Middle, _ => PointerButton.None };
                    if (button != PointerButton.None) deliver(new(down ? DrmEventKind.Press : DrmEventKind.Release, new((float)x, (float)y), button, Modifiers));
                }
            }
            if (type == 403)
                deliver(new(DrmEventKind.Scroll, new((float)x, (float)y), Modifiers: Modifiers,
                    DeltaX: libinput_event_pointer_has_axis(pointer, 1) != 0 ? (float)libinput_event_pointer_get_axis_value(pointer, 1) : 0,
                    DeltaY: libinput_event_pointer_has_axis(pointer, 0) != 0 ? (float)libinput_event_pointer_get_axis_value(pointer, 0) : 0));
        }
        if (type is >= 500 and <= 503)
        {
            var touch = libinput_event_get_touch_event(value);
            var slot = libinput_event_touch_get_seat_slot(touch);
            if (type == 503) { Cancel(); return; }
            if (type == 500 && touchSlot < 0) touchSlot = slot;
            if (touchSlot != slot) return;
            if (type is 500 or 502)
            {
                x = libinput_event_touch_get_x_transformed(touch, (uint)width); y = libinput_event_touch_get_y_transformed(touch, (uint)height); ClampPointer();
                deliver(new(DrmEventKind.Move, new((float)x, (float)y), Modifiers: Modifiers, Touch: true));
            }
            if (type is 500 or 501) deliver(new(type == 500 ? DrmEventKind.Press : DrmEventKind.Release, new((float)x, (float)y), PointerButton.Primary, Modifiers, Touch: true));
            if (type == 501) touchSlot = -1;
        }
    }

    private void ClampPointer() { x = Math.Clamp(x, 0, width - 1); y = Math.Clamp(y, 0, height - 1); }

    internal void Key(uint evdevCode, bool down)
    {
        var code = evdevCode + 8;
        if (!UpdatePressed(pressedKeys, code, down)) return;
        xkb_state_update_key(state, code, down ? 1 : 0);
        var symbol = xkb_state_key_get_one_sym(state, code);
        var modifiers = Modifiers;
        if (down && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt))
        {
            var terminal = symbol is >= 0xffbe and <= 0xffc9 ? (int)(symbol - 0xffbe + 1) :
                symbol is >= 0x1008fe01 and <= 0x1008fe0c ? (int)(symbol - 0x1008fe01 + 1) : 0;
            if (terminal != 0) { session.Switch(terminal); return; }
        }
        var text = down ? Text(symbol, modifiers) : null;
        deliver(new(down ? DrmEventKind.KeyDown : DrmEventKind.KeyUp, Modifiers: modifiers, Key: KeyName(symbol), Text: text));
        if (down && xkb_keymap_key_repeats(keymap, code) != 0)
        {
            repeatCode = code; repeatText = text;
            repeatComposing = compose != 0 && xkb_compose_state_get_status(compose) == 1;
            repeatComposed = text is not null && text != PrintableText(symbol);
            repeatAt = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 600 / 1000;
        }
        if (!down && code == repeatCode) { repeatCode = 0; repeatAt = long.MaxValue; }
    }

    internal int Tick()
    {
        var now = Stopwatch.GetTimestamp();
        if (repeatCode != 0 && repeatAt <= now)
        {
            RepeatKey();
            repeatAt = now + Stopwatch.Frequency / 25;
        }
        return repeatAt == long.MaxValue ? -1 : (int)Math.Clamp(Math.Ceiling((repeatAt - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency), 0, int.MaxValue);
    }

    internal void RepeatKey()
    {
        if (repeatCode == 0) return;
        var modifiers = Modifiers;
        var symbol = xkb_state_key_get_one_sym(state, repeatCode);
        var text = repeatComposing || (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0 ? null :
            repeatComposed ? repeatText : PrintableText(symbol);
        deliver(new(DrmEventKind.KeyDown, Modifiers: modifiers, Key: KeyName(symbol), Text: text, Repeat: true));
    }

    private string? Text(uint symbol, KeyModifiers modifiers)
    {
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0) return null;
        byte* buffer = stackalloc byte[128];
        if (compose != 0)
        {
            xkb_compose_state_feed(compose, symbol);
            switch (xkb_compose_state_get_status(compose))
            {
                case 1: return null;
                case 2:
                    var length = xkb_compose_state_get_utf8(compose, buffer, 128);
                    xkb_compose_state_reset(compose);
                    return length > 0 && length < 128 ? Marshal.PtrToStringUTF8((nint)buffer) : null;
                case 3: xkb_compose_state_reset(compose); return null;
            }
        }
        return PrintableText(symbol);
    }

    private static string? PrintableText(uint symbol)
    {
        var unicode = xkb_keysym_to_utf32(symbol);
        return unicode >= 32 && unicode != 127 && unicode <= 0x10ffff && unicode is not (>= 0xd800 and <= 0xdfff) ? char.ConvertFromUtf32((int)unicode) : null;
    }

    private static string KeyName(uint symbol)
    {
        byte* buffer = stackalloc byte[128];
        buffer[0] = 0; buffer[127] = 0;
        var unicode = xkb_keysym_to_utf32(symbol);
        if (unicode >= 32 && unicode != 127) xkb_keysym_to_utf8(symbol, buffer, 128);
        else xkb_keysym_get_name(symbol, buffer, 128);
        return LinuxKeyboard.KeyName(Marshal.PtrToStringUTF8((nint)buffer) ?? "Unidentified");
    }

    private void ResetKeyboard()
    {
        pressedKeys.Clear(); repeatCode = 0; repeatAt = long.MaxValue;
        if (compose != 0) xkb_compose_state_reset(compose);
        if (state != 0) xkb_state_unref(state);
        state = xkb_state_new(keymap);
        if (state == 0) throw new InvalidOperationException("Cannot create keyboard state.");
    }

    internal static bool UpdatePressed(Dictionary<uint, int> pressed, uint code, bool down)
    {
        pressed.TryGetValue(code, out var count);
        if (down) { pressed[code] = count + 1; return count == 0; }
        if (count == 0) return false;
        if (count == 1) pressed.Remove(code);
        else pressed[code] = count - 1;
        return count == 1;
    }

    private void Cancel() { pressedButtons.Clear(); touchSlot = -1; repeatCode = 0; repeatAt = long.MaxValue; deliver(new(DrmEventKind.Cancel)); }

    internal void Suspend() { Cancel(); libinput_suspend(input); ResetKeyboard(); ThrowCallbackFailure(); }
    internal void Resume()
    {
        if (libinput_resume(input) != 0) throw new InvalidOperationException("Cannot resume input devices.");
        ThrowCallbackFailure();
    }

    private void ThrowCallbackFailure()
    {
        if (callbackFailure is { } error) { callbackFailure = null; System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw(); }
    }

    public void Dispose()
    {
        if (input != 0) { libinput_unref(input); input = 0; }
        if (udev != 0) { udev_unref(udev); udev = 0; }
        if (compose != 0) { xkb_compose_state_unref(compose); compose = 0; }
        if (composeTable != 0) { xkb_compose_table_unref(composeTable); composeTable = 0; }
        if (state != 0) { xkb_state_unref(state); state = 0; }
        if (keymap != 0) { xkb_keymap_unref(keymap); keymap = 0; }
        if (xkb != 0) { xkb_context_unref(xkb); xkb = 0; }
        GC.KeepAlive(openDevice); GC.KeepAlive(closeDevice);
    }
}
