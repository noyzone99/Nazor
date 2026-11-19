using Nazor.Input;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

internal sealed unsafe class Win32Clipboard(Win32Window window) : IClipboard
{
    public string? GetText()
    {
        if (!IsClipboardFormatAvailable(CF_UNICODETEXT) || !TryOpen()) return null;
        try
        {
            var memory = GetClipboardData(CF_UNICODETEXT);
            if (memory == 0) return null;
            var text = (char*)GlobalLock(memory);
            if (text == null) return null;
            try
            {
                var max = (int)(GlobalSize(memory) / sizeof(char));
                var length = 0;
                while (length < max && text[length] != '\0') length++;
                return new string(text, 0, length);
            }
            finally
            {
                GlobalUnlock(memory);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public bool SetText(string text)
    {
        if (!TryOpen()) return false;
        try
        {
            if (!EmptyClipboard()) return false;
            var bytes = (nuint)((text.Length + 1) * sizeof(char));
            var memory = GlobalAlloc(GMEM_MOVEABLE, bytes);
            if (memory == 0) return false;
            var target = (char*)GlobalLock(memory);
            fixed (char* source = text) Buffer.MemoryCopy(source, target, (long)bytes, text.Length * sizeof(char));
            target[text.Length] = '\0';
            GlobalUnlock(memory);
            if (SetClipboardData(CF_UNICODETEXT, memory) != 0) return true;
            GlobalFree(memory);
            return false;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private bool TryOpen()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (OpenClipboard(window.Handle)) return true;
            Thread.Sleep(20);
        }
        return false;
    }
}
