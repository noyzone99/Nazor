using System.Text;
using Nazor.Graphics.Text;

namespace Nazor.Presentation;

internal static class FontSymbolResolution
{
    internal static FontSymbol? Resolve(IReadOnlyList<FontSymbol> candidates, TextStyle style, IFontProvider fonts)
    {
        for (var i = 0; i < Math.Min(candidates.Count, FontSymbols.MaximumCandidates); i++)
        {
            var symbol = candidates[i];
            var face = fonts.Match(style with { FontFamilies = new[] { symbol.FontFamily } });
            if (!string.Equals(face.FamilyName, symbol.FontFamily, StringComparison.OrdinalIgnoreCase)) continue;
            var covered = true;
            foreach (var rune in symbol.Glyph.EnumerateRunes())
                if (!face.HasGlyph(rune.Value)) { covered = false; break; }
            if (covered) return symbol;
        }
        return null;
    }
}
