namespace Nazor.Graphics.Text;

/// <summary>Private font files and fallback policy, independent of the renderer. Configure before startup.</summary>
public sealed record FontOptions
{
    /// <summary>
    /// Paths relative to AppContext.BaseDirectory, or absolute paths. Private families precede system fonts.
    /// </summary>
    public IReadOnlyList<string> PrivateFontFiles { get; set; } = [];

    /// <summary>Include installed fonts in matching and fallback. Disable for deterministic tests.</summary>
    public bool UseSystemFonts { get; init; } = true;

    /// <summary>Gets or sets the fallback family used when no requested family can be resolved.</summary>
    public string DefaultFamily { get; init; } = "Segoe UI";

    /// <summary>Add font files, including each required weight and style, once before startup.</summary>
    /// <param name="paths">The paths (string[]).</param>
    /// <returns>The resulting FontOptions value.</returns>
    /// <exception cref="ArgumentNullException">paths is null.</exception>
    public FontOptions Add(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        foreach (var path in paths) ArgumentException.ThrowIfNullOrWhiteSpace(path);
        PrivateFontFiles = [.. PrivateFontFiles, .. paths];
        return this;
    }
}
