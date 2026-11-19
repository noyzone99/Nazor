namespace Nazor;

/// <summary>An immutable glyph and the single font family that gives it meaning.</summary>
/// <param name="FontFamily">The font's internal family name.</param>
/// <param name="Glyph">The nonblank Unicode glyph text.</param>
/// <remarks>The default value is empty and cannot be used as an active icon symbol.</remarks>
public readonly record struct FontSymbol(string FontFamily, string Glyph) : IFontSymbol
{
    /// <summary>Formats this pair as two quoted NSS strings.</summary>
    /// <returns>The quoted font family followed by the quoted glyph text.</returns>
    /// <exception cref="InvalidOperationException">The family or glyph contains control characters that NSS cannot represent.</exception>
    public override string ToString()
    {
        if (FontFamily?.Any(char.IsControl) is true || Glyph?.Any(char.IsControl) is true)
            throw new InvalidOperationException("NSS font-symbol pairs must not contain control characters.");
        return Quote(FontFamily) + " " + Quote(Glyph);
    }

    private static string Quote(string? value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}

internal static class FontSymbols
{
    internal const int MaximumCandidates = 32;

    internal static IReadOnlyList<FontSymbol> Expand(IFontSymbol symbol)
    {
        var candidates = new List<FontSymbol>();
        var pending = new Stack<IFontSymbol>();
        var visited = new HashSet<IFontSymbol>();
        pending.Push(symbol);
        while (pending.Count > 0 && visited.Count < MaximumCandidates)
        {
            var current = pending.Pop();
            if (!visited.Add(current)) continue;
            ArgumentException.ThrowIfNullOrWhiteSpace(current.FontFamily, nameof(symbol));
            ArgumentException.ThrowIfNullOrWhiteSpace(current.Glyph, nameof(symbol));
            if (current.FontFamily.Any(char.IsControl))
                throw new ArgumentException("A font symbol's family must not contain control characters.", nameof(symbol));
            candidates.Add(new(current.FontFamily, current.Glyph));
            var fallbacks = current.Fallbacks ?? throw new ArgumentException("A font symbol's fallbacks must not be null.", nameof(symbol));
            for (var i = Math.Min(fallbacks.Count, MaximumCandidates) - 1; i >= 0; i--)
                pending.Push(fallbacks[i] ?? throw new ArgumentException("A font symbol's fallbacks must not contain null.", nameof(symbol)));
        }
        return candidates;
    }
}
