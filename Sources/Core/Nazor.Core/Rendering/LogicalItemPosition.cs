namespace Nazor.Rendering;

/// <summary>An explicit one-based position in a known logical collection after filtering/sorting, independent of physical realization. Publish as item-position.</summary>
public readonly record struct LogicalItemPosition
{
    /// <summary>Creates a complete logical position.</summary>
    /// <param name="sequence">The explicit sequence identity, including any group/parent identity.</param>
    /// <param name="position">The one-based position in that sequence.</param>
    /// <param name="count">The complete known count, including unrealized items.</param>
    /// <exception cref="ArgumentNullException">sequence is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">position or count is outside 1..count.</exception>
    public LogicalItemPosition(object sequence, int position, int count)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        if (position < 1 || position > count) throw new ArgumentOutOfRangeException(nameof(position));
        Sequence = sequence;
        Position = position;
        Count = count;
    }

    /// <summary>Gets the sequence identity supplied by the control, or null for the default invalid value.</summary>
    public object? Sequence { get; }

    /// <summary>Gets the one-based logical position.</summary>
    public int Position { get; }

    /// <summary>Gets the complete known logical count.</summary>
    public int Count { get; }
}
