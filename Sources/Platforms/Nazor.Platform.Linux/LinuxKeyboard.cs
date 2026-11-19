namespace Nazor.Platform.Linux;

internal static class LinuxKeyboard
{
    internal static string KeyName(string name) => name switch
    {
        "Return" or "KP_Enter" => "Enter", "BackSpace" => "Backspace", "Escape" => "Escape", "Tab" or "ISO_Left_Tab" => "Tab",
        "Left" or "KP_Left" => "ArrowLeft", "Right" or "KP_Right" => "ArrowRight", "Up" or "KP_Up" => "ArrowUp", "Down" or "KP_Down" => "ArrowDown",
        "Prior" or "KP_Prior" => "PageUp", "Next" or "KP_Next" => "PageDown", "KP_Home" => "Home", "KP_End" => "End",
        "KP_Delete" => "Delete", "KP_Insert" => "Insert", "space" => " ", "Shift_L" or "Shift_R" => "Shift",
        "Control_L" or "Control_R" => "Control", "Alt_L" or "Alt_R" => "Alt", "Super_L" or "Super_R" or "Meta_L" or "Meta_R" => "Meta",
        _ => name
    };
}
