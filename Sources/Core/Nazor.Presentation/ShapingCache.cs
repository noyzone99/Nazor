using Nazor.Graphics.Text;

namespace Nazor.Presentation;

internal sealed class ShapingCache(IFontProvider fonts, ITextShaper shaper, int capacity = 4096)
{
    private readonly Dictionary<(string Text, TextStyle Style), WeakReference<ShapedText>> shapes = [];

    public ShapedText Get(string text, TextStyle style)
    {
        if (shapes.TryGetValue((text, style), out var cached) && cached.TryGetTarget(out var current) && current.FontVersion == fonts.Version) return current;
        if (shapes.Count >= capacity) shapes.Clear();
        var shaped = new ShapedText(text, style, fonts, shaper);
        shapes[(text, style)] = new(shaped);
        return shaped;
    }
}
