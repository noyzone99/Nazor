using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nazor.Platform;

namespace Nazor;

/// <summary>
/// Where the windows with a <c>PlacementKey</c> remember their position and size. Replace it in the services to keep
/// them elsewhere (the settings of the application, a test folder).
/// </summary>
public interface IWindowPlacementStore
{
    /// <summary>Loads saved bounds and state for the specified window key.</summary>
    /// <param name="key">The key (string).</param>
    /// <returns>The saved placement, or null if none is available.</returns>
    WindowPlacement? Load(string key);

    /// <summary>Persists normal bounds and maximized state for the specified window key.</summary>
    /// <param name="key">The key (string).</param>
    /// <param name="placement">The normal window bounds and state to save or restore.</param>
    void Save(string key, WindowPlacement placement);
}

/// <summary>
/// Keeps the placements in a JSON file, by default <c>%LOCALAPPDATA%\&lt;application&gt;\Nazor\windows.json</c>. The
/// file is written whole into a temporary file and then moved, so a crash never leaves it half written.
/// </summary>
/// <param name="path">The file path used by this operation.</param>
public sealed class JsonWindowPlacementStore(string path) : IWindowPlacementStore
{
    private readonly Lock gate = new();

    /// <summary>Gets the absolute path of the JSON placement file.</summary>
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

    /// <summary>
    /// The file of the application: under the local application data, in a folder named after the entry assembly.
    /// </summary>
    /// <returns>The resulting JsonWindowPlacementStore value.</returns>
    public static JsonWindowPlacementStore ForApplication()
    {
        var application = Assembly.GetEntryAssembly()?.GetName().Name ?? "Nazor";
        return new(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application, "Nazor", "windows.json"));
    }

    /// <summary>Reads the saved placement for a window key from the JSON store.</summary>
    /// <param name="key">The key (string).</param>
    /// <returns>The saved placement, or null if no valid entry can be read.</returns>
    public WindowPlacement? Load(string key)
    {
        lock (gate) return Read().GetValueOrDefault(key);
    }

    /// <summary>Writes the window placement under the specified key in the JSON store.</summary>
    /// <param name="key">The key (string).</param>
    /// <param name="placement">The normal window bounds and state to save or restore.</param>
    public void Save(string key, WindowPlacement placement)
    {
        lock (gate)
        {
            var all = Read();
            all[key] = placement;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!);
            var temporary = Path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(all, WindowPlacementJson.Default.DictionaryStringWindowPlacement));
            File.Move(temporary, Path, overwrite: true);
        }
    }

    private Dictionary<string, WindowPlacement> Read()
    {
        if (!File.Exists(Path)) return [];
        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(Path), WindowPlacementJson.Default.DictionaryStringWindowPlacement) ?? [];
        }
        catch (JsonException)
        {

            return [];
        }
    }
}

[JsonSerializable(typeof(Dictionary<string, WindowPlacement>))]
internal partial class WindowPlacementJson : JsonSerializerContext;
