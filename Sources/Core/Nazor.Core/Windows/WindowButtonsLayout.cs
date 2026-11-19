namespace Nazor;

/// <summary>A system title-bar control; Maximize also represents Restore when maximized.</summary>
public enum WindowButton
{
    /// <summary>Identifies the native minimize caption control.</summary>
    Minimize,

    /// <summary>Identifies the native maximize or restore caption control.</summary>
    Maximize,

    /// <summary>Identifies the native close caption control.</summary>
    Close,
}

/// <summary>
/// System control placement for custom title bars, independent of native control visibility. Both physical sides are
/// ordered from left to right. An empty layout has no controls.
/// </summary>
public sealed record WindowButtonsLayout
{
    /// <summary>Fallback when the backend has no system layout setting.</summary>
    public static WindowButtonsLayout Default { get; } = new([], [WindowButton.Minimize, WindowButton.Maximize, WindowButton.Close]);

    /// <summary>Gets a caption layout without controls on either side.</summary>
    public static WindowButtonsLayout Empty { get; } = new([], []);

    /// <summary>Initializes a new instance of WindowButtonsLayout.</summary>
    /// <param name="left">The caption controls on the left, ordered physically from left to right.</param>
    /// <param name="right">The caption controls on the right, ordered physically from left to right.</param>
    public WindowButtonsLayout(IReadOnlyList<WindowButton> left, IReadOnlyList<WindowButton> right)
    {
        Left = Array.AsReadOnly(left.ToArray());
        Right = Array.AsReadOnly(right.ToArray());
    }

    /// <summary>Gets the caption controls on the left, ordered physically from left to right.</summary>
    public IReadOnlyList<WindowButton> Left { get; }

    /// <summary>Gets the caption controls on the right, ordered physically from left to right.</summary>
    public IReadOnlyList<WindowButton> Right { get; }

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(WindowButtonsLayout? other) => other is not null &&
        Left.SequenceEqual(other.Left) && Right.SequenceEqual(other.Right);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Left.Count);
        foreach (var control in Left) hash.Add(control);
        foreach (var control in Right) hash.Add(control);
        return hash.ToHashCode();
    }
}
