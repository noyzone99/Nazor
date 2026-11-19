using System.Globalization;
using System.Text;

namespace Nazor.Style;

/// <summary>Specifies how a grid track's breadth is sized.</summary>
public enum TrackBreadthKind : byte
{
    /// <summary>A length or a percentage of the grid container's inner size.</summary>
    Length,

    /// <summary>A share of the leftover space (<c>fr</c>); only as a maximum.</summary>
    Flex,

    /// <summary>Sizes the track from the automatic contributions of its items.</summary>
    Auto,

    /// <summary>Uses the minimum intrinsic content contribution.</summary>
    MinContent,

    /// <summary>Uses the maximum intrinsic content contribution.</summary>
    MaxContent,
}

/// <summary>One side of a track size: the minimum or the maximum sizing function (CSS Grid §7.2.3).</summary>
/// <param name="Kind">The track breadth kind represented by this value.</param>
/// <param name="Length">The absolute, relative, or percentage length to use.</param>
/// <param name="Flex">The flex (double).</param>
public readonly record struct TrackBreadth(TrackBreadthKind Kind, Length Length, double Flex)
{
    /// <summary>Automatic grid-track sizing based on the contained items.</summary>
    public static readonly TrackBreadth Auto = new(TrackBreadthKind.Auto, Length.Zero, 0);

    /// <summary>Minimum-content intrinsic grid-track sizing.</summary>
    public static readonly TrackBreadth MinContent = new(TrackBreadthKind.MinContent, Length.Zero, 0);

    /// <summary>Maximum-content intrinsic grid-track sizing.</summary>
    public static readonly TrackBreadth MaxContent = new(TrackBreadthKind.MaxContent, Length.Zero, 0);

    /// <summary>Creates a track breadth from the supplied length.</summary>
    /// <param name="length">The absolute, relative, or percentage length to use.</param>
    /// <returns>The resulting TrackBreadth value.</returns>
    public static TrackBreadth Fixed(Length length) => new(TrackBreadthKind.Length, length, 0);

    /// <summary>Creates a flexible track breadth in fractional units.</summary>
    /// <param name="value">The numeric magnitude in the unit selected by this factory method.</param>
    /// <returns>The resulting TrackBreadth value.</returns>
    public static TrackBreadth Fr(double value) => new(TrackBreadthKind.Flex, Length.Zero, value);

    /// <summary>Gets whether the track breadth is represented by a length.</summary>
    public bool IsFixed => Kind == TrackBreadthKind.Length;

    /// <summary>Gets whether the track breadth uses automatic, minimum-content, or maximum-content sizing.</summary>
    public bool IsIntrinsic => Kind is TrackBreadthKind.Auto or TrackBreadthKind.MinContent or TrackBreadthKind.MaxContent;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        TrackBreadthKind.Length => Length.ToString(),
        TrackBreadthKind.Flex => Flex.ToString("0.###", CultureInfo.InvariantCulture) + "fr",
        TrackBreadthKind.Auto => "auto",
        TrackBreadthKind.MinContent => "min-content",
        _ => "max-content",
    };
}

/// <summary>
/// The size of a grid track: <c>minmax(Min, Max)</c>. A plain length, <c>auto</c> or keyword is the same on both
/// sides, <c>1fr</c> is <c>minmax(auto, 1fr)</c>; with <see cref="FitContent" /> the maximum is <c>max-content</c>
/// clamped by the length in <see cref="Max" /> (<c>fit-content()</c>).
/// </summary>
/// <param name="Min">The smallest accepted value.</param>
/// <param name="Max">The largest accepted value.</param>
/// <param name="FitContent">The fit content (bool).</param>
public readonly record struct TrackSize(TrackBreadth Min, TrackBreadth Max, bool FitContent = false)
{
    /// <summary>A grid track with automatic minimum and maximum breadth.</summary>
    public static readonly TrackSize Auto = new(TrackBreadth.Auto, TrackBreadth.Auto);

    /// <summary>Creates a grid track whose minimum and maximum use the supplied length.</summary>
    /// <param name="length">The absolute, relative, or percentage length to use.</param>
    /// <returns>The resulting TrackSize value.</returns>
    public static TrackSize Fixed(Length length) => new(TrackBreadth.Fixed(length), TrackBreadth.Fixed(length));

    /// <summary>Creates a flexible grid track using the supplied fractional weight.</summary>
    /// <param name="value">The numeric magnitude in the unit selected by this factory method.</param>
    /// <returns>The resulting TrackSize value.</returns>
    public static TrackSize Fr(double value) => new(TrackBreadth.Auto, TrackBreadth.Fr(value));

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        if (FitContent) return $"fit-content({Max.Length})";
        if (Min == Max) return Min.ToString();
        if (Min.Kind == TrackBreadthKind.Auto && Max.Kind == TrackBreadthKind.Flex) return Max.ToString();
        return $"minmax({Min}, {Max})";
    }
}

/// <summary>Specifies how repeated grid tracks fill the available space.</summary>
public enum GridAutoRepeat : byte
{
    /// <summary>Uses the explicit track repetition count.</summary>
    None,

    /// <summary>Creates as many repeated tracks as fit the available space.</summary>
    Fill,

    /// <summary>Creates fitting repeated tracks and collapses unused tracks.</summary>
    Fit,
}

/// <summary>
/// The value of <c>grid-template-columns</c> and <c>grid-template-rows</c>: explicit tracks, with repetitions of a
/// fixed count already expanded and at most one automatic repetition inserted before track <see cref="RepeatIndex" />.
/// <see cref="None" /> is the keyword <c>none</c>.
/// </summary>
/// <param name="Tracks">The track definitions in declaration order.</param>
/// <param name="Repeat">The repeat (GridAutoRepeat).</param>
/// <param name="RepeatIndex">The repeat index (int).</param>
/// <param name="RepeatTracks">The repeat tracks (ValueList&lt;TrackSize&gt;).</param>
public sealed record GridTrackList(ValueList<TrackSize> Tracks, GridAutoRepeat Repeat, int RepeatIndex, ValueList<TrackSize> RepeatTracks)
{
    /// <summary>An empty explicit track definition.</summary>
    public static readonly GridTrackList None = new(ValueList<TrackSize>.Empty, GridAutoRepeat.None, 0, ValueList<TrackSize>.Empty);

    /// <summary>Initializes a new instance of GridTrackList.</summary>
    /// <param name="tracks">The track definitions in declaration order.</param>
    public GridTrackList(IEnumerable<TrackSize> tracks) : this(new ValueList<TrackSize>(tracks), GridAutoRepeat.None, 0, ValueList<TrackSize>.Empty)
    {
    }

    /// <summary>Gets whether the explicit track list is empty and has no automatic repetition.</summary>
    public bool IsNone => Tracks.Items.Count == 0 && Repeat == GridAutoRepeat.None;

    /// <summary>The tracks with the automatic repetition repeated <paramref name="repetitions" /> times.</summary>
    /// <param name="repetitions">The repetitions (int).</param>
    /// <returns>The resulting IReadOnlyList&lt;TrackSize&gt; value.</returns>
    public IReadOnlyList<TrackSize> Expand(int repetitions)
    {
        if (Repeat == GridAutoRepeat.None) return Tracks.Items;
        var result = new List<TrackSize>(Tracks.Items.Count + RepeatTracks.Items.Count * repetitions);
        result.AddRange(Tracks.Items.Take(RepeatIndex));
        for (var i = 0; i < repetitions; i++) result.AddRange(RepeatTracks.Items);
        result.AddRange(Tracks.Items.Skip(RepeatIndex));
        return result;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        if (IsNone) return "none";
        var parts = Tracks.Items.Select(t => t.ToString()).ToList();
        if (Repeat != GridAutoRepeat.None)
            parts.Insert(RepeatIndex, $"repeat({(Repeat == GridAutoRepeat.Fill ? "auto-fill" : "auto-fit")}, {string.Join(' ', RepeatTracks.Items)})");
        return string.Join(' ', parts);
    }
}

/// <summary>Distinguishes automatic placement, explicit grid lines, spans, and named areas.</summary>
public enum GridLineKind : byte
{
    /// <summary>Places the item using the grid auto-placement algorithm.</summary>
    Auto,

    /// <summary>A line number: from 1 at the start of the explicit grid, negative from its end.</summary>
    Line,

    /// <summary><c>span N</c>: the item covers N tracks from the other line.</summary>
    Span,

    /// <summary>The start or end line of a named area of <c>grid-template-areas</c>.</summary>
    Area,
}

/// <summary>One side of a grid item's placement: <c>grid-row-start</c>, <c>grid-column-end</c>...</summary>
/// <param name="Kind">The grid line kind represented by this value.</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
public readonly record struct GridLine(GridLineKind Kind, int Value, string? Name)
{
    /// <summary>Automatic grid-item placement without an explicit line or span.</summary>
    public static readonly GridLine Auto = new(GridLineKind.Auto, 0, null);

    /// <summary>
    /// Creates a grid placement at the specified one-based line, with negative values counting from the end.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting GridLine value.</returns>
    public static GridLine Line(int value) => new(GridLineKind.Line, value, null);

    /// <summary>Creates a grid placement spanning the specified number of tracks.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting GridLine value.</returns>
    public static GridLine Span(int value) => new(GridLineKind.Span, value, null);

    /// <summary>Creates a grid placement that references a named area.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting GridLine value.</returns>
    public static GridLine Area(string name) => new(GridLineKind.Area, 0, name);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        GridLineKind.Auto => "auto",
        GridLineKind.Line => Value.ToString(CultureInfo.InvariantCulture),
        GridLineKind.Span => "span " + Value.ToString(CultureInfo.InvariantCulture),
        _ => Name!,
    };
}

/// <summary>Specifies the axis and packing strategy used to place implicit grid items.</summary>
public enum GridAutoFlow : byte
{
    /// <summary>Places auto-positioned items by filling rows.</summary>
    Row,

    /// <summary>Places auto-positioned items by filling columns.</summary>
    Column,

    /// <summary>Fills rows and backfills earlier gaps.</summary>
    RowDense,

    /// <summary>Fills columns and backfills earlier gaps.</summary>
    ColumnDense,
}

/// <summary>A named area of <c>grid-template-areas</c>: 0-based tracks, end exclusive.</summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="RowStart">The row start (int).</param>
/// <param name="RowEnd">The row end (int).</param>
/// <param name="ColumnStart">The column start (int).</param>
/// <param name="ColumnEnd">The column end (int).</param>
public readonly record struct GridArea(string Name, int RowStart, int RowEnd, int ColumnStart, int ColumnEnd);

/// <summary>
/// The value of <c>grid-template-areas</c>: one string per row, cells separated by spaces, a run of dots for an empty
/// cell. Every name must form a filled rectangle; the constructor rejects the rest with
/// <see cref="ArgumentException" /> (the NSS compiler reports it as NZS102).
/// </summary>
public sealed class GridAreas : IEquatable<GridAreas>
{
    /// <summary>An empty named-grid-area definition.</summary>
    public static readonly GridAreas None = new([]);

    private readonly Dictionary<string, GridArea> areas = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of GridAreas.</summary>
    /// <param name="rows">The named-area rows, with whitespace separating cells and dots indicating empty cells.</param>
    public GridAreas(IEnumerable<string> rows)
    {
        var cells = rows.Select(r => r.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
        Rows = new ValueList<string>(cells.Select(c => string.Join(' ', c)));
        if (cells.Any(c => c.Length == 0)) throw new ArgumentException("A row of grid-template-areas needs at least one cell.");
        Columns = cells.Length == 0 ? 0 : cells[0].Length;
        if (cells.Any(c => c.Length != Columns)) throw new ArgumentException("Every row of grid-template-areas needs the same number of cells.");
        for (var row = 0; row < cells.Length; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var name = cells[row][column];
                if (name.All(c => c == '.')) continue;
                if (!IsAreaName(name)) throw new ArgumentException($"'{name}' is not a valid area name: use letters, digits, '-' and '_', or dots for an empty cell.");
                if (areas.TryGetValue(name, out var area))
                {
                    areas[name] = area with
                    {
                        RowStart = Math.Min(area.RowStart, row), RowEnd = Math.Max(area.RowEnd, row + 1),
                        ColumnStart = Math.Min(area.ColumnStart, column), ColumnEnd = Math.Max(area.ColumnEnd, column + 1),
                    };
                }
                else areas[name] = new GridArea(name, row, row + 1, column, column + 1);
            }
        }
        foreach (var area in areas.Values)
            for (var row = area.RowStart; row < area.RowEnd; row++)
                for (var column = area.ColumnStart; column < area.ColumnEnd; column++)
                    if (cells[row][column] != area.Name)
                        throw new ArgumentException($"The area '{area.Name}' of grid-template-areas is not a rectangle.");
    }

    private static bool IsAreaName(string name) =>
        name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' || c > 127) &&
        !char.IsAsciiDigit(name[0]) && !(name.Length > 1 && name[0] == '-' && char.IsAsciiDigit(name[1]));

    /// <summary>The rows with their cells separated by one space.</summary>
    public ValueList<string> Rows { get; }

    /// <summary>Gets the number of rows in the grid-area definition.</summary>
    public int RowCount => Rows.Items.Count;

    /// <summary>Gets the number of columns in the grid-area definition.</summary>
    public int Columns { get; }

    /// <summary>Gets the named grid areas and their row and column bounds.</summary>
    public IReadOnlyCollection<GridArea> Areas => areas.Values;

    /// <summary>Looks up the row and column bounds of a named grid area.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="area">When this method returns, contains the area produced by the operation.</param>
    /// <returns>true if the named area exists; otherwise, false.</returns>
    public bool TryGetArea(string name, out GridArea area) => areas.TryGetValue(name, out area);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(GridAreas? other) => other is not null && Rows.Equals(other.Rows);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is GridAreas other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode() => Rows.GetHashCode();

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        if (RowCount == 0) return "none";
        var builder = new StringBuilder();
        foreach (var row in Rows.Items) builder.Append(builder.Length > 0 ? " " : "").Append('"').Append(row).Append('"');
        return builder.ToString();
    }
}
