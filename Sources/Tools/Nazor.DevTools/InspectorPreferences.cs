using Nazor.Style;

namespace Nazor.DevTools;

public static class InspectorPreferences
{
    public static string FilePath => Path.Combine(Directory, "DevTools.appearance");
    /// <summary>Whether choosing a node in the inspector also highlights it in the app.</summary>
    public static string HighlightFilePath => Path.Combine(Directory, "DevTools.highlight");
    private static string Directory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nazor");

    public static ColorScheme Read(string path) =>
        Enum.TryParse<ColorScheme>(ReadText(path), out var scheme) && Enum.IsDefined(scheme) ? scheme : ColorScheme.System;

    public static bool Save(string path, ColorScheme scheme)
    {
        if (!Enum.IsDefined(scheme)) throw new ArgumentOutOfRangeException(nameof(scheme));
        return Write(path, scheme.ToString());
    }

    public static bool ReadFlag(string path, bool fallback) => bool.TryParse(ReadText(path), out var value) ? value : fallback;

    public static bool SaveFlag(string path, bool value) => Write(path, value ? "true" : "false");

    private static string? ReadText(string path)
    {
        try { return File.ReadAllText(path).Trim(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
    }

    private static bool Write(string path, string text)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary, text);
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
