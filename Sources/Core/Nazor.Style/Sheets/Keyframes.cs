using System.Globalization;

namespace Nazor.Style;

/// <summary>
/// One keyframe of a <c>@keyframes</c> rule: its offset in the iteration (0 for <c>from</c>, 1 for <c>to</c>), its
/// declarations (animatable properties only) and, with <c>animation-timing-function</c>, the timing function of the
/// interval that starts here.
/// </summary>
/// <param name="offset">The offset (double).</param>
/// <param name="declarations">The declarations assigned by the rule or keyframe.</param>
/// <param name="easing">The easing (EasingFunction?).</param>
public sealed class Keyframe(double offset, IReadOnlyList<StyleDeclaration> declarations, EasingFunction? easing = null)
{
    /// <summary>Gets the normalized keyframe position, where zero is the start and one is the end.</summary>
    public double Offset { get; } = offset;

    /// <summary>Gets the style declarations applied by this rule or keyframe.</summary>
    public IReadOnlyList<StyleDeclaration> Declarations { get; } = declarations;

    /// <summary>Gets the optional easing function used for the segment starting at this keyframe.</summary>
    public EasingFunction? Easing { get; } = easing;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var parts = Declarations.Select(d => d.ToString()).ToList();
        if (Easing is not null) parts.Add($"animation-timing-function: {Easing}");
        return string.Create(CultureInfo.InvariantCulture, $"{Offset * 100:0.###}% {{ {string.Join("; ", parts)} }}");
    }
}

/// <summary>A <c>@keyframes</c> rule: a name and its keyframes by offset (equal offsets in source order).</summary>
/// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="keyframes">The keyframe animation definitions supplied by the stylesheet.</param>
public sealed class KeyframesRule(string name, IReadOnlyList<Keyframe> keyframes)
{
    /// <summary>Gets the identifier referenced by animation-name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the ordered keyframes used to evaluate this animation.</summary>
    public IReadOnlyList<Keyframe> Keyframes { get; } = keyframes;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"@keyframes {Name} {{ {string.Join(" ", Keyframes)} }}";
}

/// <summary>A keyframe resolved for one element: its offset, timing function and computed values.</summary>
/// <param name="Offset">The offset (double).</param>
/// <param name="Easing">The easing (EasingFunction?).</param>
/// <param name="Values">The values (IReadOnlyDictionary&lt;StyleProperty, object&gt;).</param>
public sealed record ResolvedKeyframe(double Offset, EasingFunction? Easing, IReadOnlyDictionary<StyleProperty, object> Values);
