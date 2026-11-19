namespace Nazor.Input;

/// <summary>Text clipboard used by editable controls; the platform provides the system one.</summary>
public interface IClipboard
{
    /// <summary>The clipboard text, or null when it holds no text or cannot be opened.</summary>
    /// <returns>The resulting string? value.</returns>
    string? GetText();

    /// <summary>Replaces the clipboard content; returns false when the clipboard is busy.</summary>
    /// <param name="text">The text to process.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    bool SetText(string text);
}

/// <summary>A clipboard local to the process, for headless hosts and tests.</summary>
public sealed class MemoryClipboard : IClipboard
{
    private string? text;

    /// <summary>Gets the text stored in this in-memory clipboard.</summary>
    /// <returns>The current clipboard text, or null when no text is stored.</returns>
    public string? GetText() => text;

    /// <summary>Replaces the text stored in this in-memory clipboard.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetText(string value)
    {
        text = value;
        return true;
    }
}
