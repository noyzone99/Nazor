using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>Matches explicit logical collection metadata rather than counting realized siblings.</summary>
/// <param name="A">The step in An+B.</param>
/// <param name="B">The one-based offset.</param>
/// <param name="FromEnd">Whether to count from the complete known logical end.</param>
/// <param name="Name">The NSS spelling.</param>
public sealed record ItemPositionSelector(int A, int B, bool FromEnd, string Name) : PseudoSelector
{
    /// <summary>Tests the typed item-position attribute without enumerating application data.</summary>
    /// <param name="element">The realized target.</param>
    /// <returns>Whether a valid published position satisfies An+B for n at least zero.</returns>
    public override bool Matches(ElementNode element)
    {
        if (element.Attributes.GetValueOrDefault("item-position") is not LogicalItemPosition { Sequence: not null, Position: > 0, Count: > 0 } item || item.Position > item.Count) return false;
        var position = FromEnd ? (long)item.Count - item.Position + 1 : item.Position;
        var difference = position - B;
        return A == 0 ? difference == 0 : difference % A == 0 && difference / A >= 0;
    }

    /// <summary>Returns the NSS spelling.</summary>
    /// <returns>The selector text.</returns>
    public override string ToString() => Name;
}
