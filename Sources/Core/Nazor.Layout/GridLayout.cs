using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout;

/// <summary>
/// CSS Grid Layout Level 1, as summarised in Docs/Guide/20-Layout-and-Scrolling.md: the explicit grid with automatic
/// repetitions (§7), line-based and automatic placement (§8), track sizing (§12) and the alignment of tracks and
/// items (§10). Line names, subgrid and baselines are not supported; when an item spans several tracks, the extra
/// space goes to them in equal shares.
/// </summary>
public static partial class LayoutEngine
{
    private sealed class GridItem
    {
        public required LayoutNode Node;
        public required BoxModel Box;
        public int? Row;
        public int RowSpan = 1;
        public int? Column;
        public int ColumnSpan = 1;
        public AlignSelf Justify;
        public AlignSelf Align;
    }

    private sealed class GridTrack
    {
        public TrackSize Size;
        public TrackSize Effective;
        public float Base;
        public float Growth;
        public bool Collapsed;
        public float Start;

        public bool IsFlexible => Effective.Max.Kind == TrackBreadthKind.Flex;

        public float Flex => (float)Effective.Max.Flex;
    }

    private readonly record struct Contribution(float Minimum, float MinContent, float MaxContent);

    private static Size LayoutGrid(LayoutNode node, BoxModel box, float? width, float? height,
        AvailableSpace availableWidth, AvailableSpace availableHeight, bool perform, bool definiteWidth, bool definiteHeight)
    {
        var style = node.Style;
        var edges = box.Padding + box.Border;
        var innerWidth = width.Map(w => Math.Max(0, w - edges.Horizontal));
        var innerHeight = height.Map(h => Math.Max(0, h - edges.Vertical));
        var percentWidth = definiteWidth ? innerWidth : null;
        var percentHeight = definiteHeight ? innerHeight : null;
        var columnGap = style.ColumnGap.At(percentWidth ?? 0);
        var rowGap = style.RowGap.At(percentHeight ?? 0);
        var maxInnerWidth = box.MaxWidth - edges.Horizontal;
        var maxInnerHeight = box.MaxHeight - edges.Vertical;

        var columnRepeats = Repetitions(style.GridTemplateColumns, innerWidth ?? (float.IsFinite(maxInnerWidth) ? maxInnerWidth : null), columnGap);
        var rowRepeats = Repetitions(style.GridTemplateRows, innerHeight ?? (float.IsFinite(maxInnerHeight) ? maxInnerHeight : null), rowGap);
        var templateColumns = style.GridTemplateColumns.Expand(columnRepeats);
        var templateRows = style.GridTemplateRows.Expand(rowRepeats);
        var areas = style.GridTemplateAreas;
        var explicitColumns = Math.Max(templateColumns.Count, areas.Columns);
        var explicitRows = Math.Max(templateRows.Count, areas.RowCount);

        var items = new List<GridItem>();
        foreach (var child in node.Style.TablePart == "table" ? Array.Empty<LayoutNode>() : node.Children)
        {
            if (child.Style.Display == Display.None)
            {
                child.Frame = Rect.Empty;
                continue;
            }
            if (child.Style.Position == Position.Absolute) continue;
            var childStyle = child.Style;
            var (column, columnSpan) = ResolveLines(childStyle.GridColumnStart, childStyle.GridColumnEnd, explicitColumns, areas, rows: false);
            var (row, rowSpan) = ResolveLines(childStyle.GridRowStart, childStyle.GridRowEnd, explicitRows, areas, rows: true);
            items.Add(new GridItem
            {
                Node = child,

                Box = ChildBox(child, null, null, AvailableSpace.MaxContent),
                Column = column,
                ColumnSpan = columnSpan,
                Row = row,
                RowSpan = rowSpan,
                Justify = childStyle.JustifySelf == AlignSelf.Auto ? (AlignSelf)((int)style.JustifyItems + 1) : childStyle.JustifySelf,
                Align = childStyle.AlignSelf == AlignSelf.Auto ? (AlignSelf)((int)style.AlignItems + 1) : childStyle.AlignSelf,
            });
        }

        var tableRows = node.Style.TablePart == "table" ? AddTableItems(node, items) : null;

        if (tableRows is not null)
        {
            explicitRows = Math.Max(explicitRows, tableRows.Count + (items.Any(item => item.Node.Style.TablePart == "caption") ? 1 : 0));
            explicitColumns = Math.Max(explicitColumns, node.TableColumns ?? 0);
        }
        if (items.Any(i => i.Node.Style.Order != 0)) items = [.. items.OrderBy(i => i.Node.Style.Order)];

        var (columnCount, rowCount, columnOffset, rowOffset) = PlaceGridItems(items, explicitColumns, explicitRows, style.GridAutoFlow);
        var columns = BuildTracks(templateColumns, style.GridAutoColumns, columnOffset, columnCount);
        var rows = BuildTracks(templateRows, style.GridAutoRows, rowOffset, rowCount);
        CollapseEmptyRepetitions(style.GridTemplateColumns, columnRepeats, columns, columnOffset, items, columnsAxis: true);
        CollapseEmptyRepetitions(style.GridTemplateRows, rowRepeats, rows, rowOffset, items, columnsAxis: false);

        var stretchColumns = style.JustifyContent is JustifyContent.Normal or JustifyContent.Stretch;
        var stretchRows = style.AlignContent is AlignContent.Normal or AlignContent.Stretch;
        Contribution ColumnContribution(GridItem item)
        {
            var minContent = Layout(item.Node, item.Box, null, null, AvailableSpace.MinContent, AvailableSpace.MaxContent, perform: false).Width;
            var maxContent = Layout(item.Node, item.Box, null, null, AvailableSpace.MaxContent, AvailableSpace.MaxContent, perform: false).Width;
            var scrolls = item.Node.Style.OverflowX != Overflow.Visible;
            var minimum = item.Box.MinWidth ?? (scrolls ? 0 : minContent);
            var margins = item.Box.Margin.Horizontal;
            return new Contribution(minimum + margins, minContent + margins, maxContent + margins);
        }
        float usedInnerWidth;
        if (innerWidth is { } knownInner)
        {
            SizeTracks(columns, items, true, AvailableSpace.Definite(knownInner), columnGap, percentWidth, stretchColumns, ColumnContribution);
            usedInnerWidth = knownInner;
        }
        else if (availableWidth.IsDefinite)
        {

            SizeTracks(columns, items, true, AvailableSpace.MaxContent, columnGap, null, stretchColumns, ColumnContribution);
            var maxContent = TracksSize(columns, columnGap);
            SizeTracks(columns, items, true, AvailableSpace.MinContent, columnGap, null, stretchColumns, ColumnContribution);
            var minContent = TracksSize(columns, columnGap);
            var fit = Math.Min(maxContent, Math.Max(minContent, availableWidth.Shrink(edges.Horizontal).Value));
            usedInnerWidth = Math.Max(0, ClampInner(fit, box.MinWidth, box.MaxWidth, edges.Horizontal));
            SizeTracks(columns, items, true, AvailableSpace.Definite(usedInnerWidth), columnGap, null, stretchColumns, ColumnContribution);
        }
        else
        {
            SizeTracks(columns, items, true, availableWidth, columnGap, null, stretchColumns, ColumnContribution);
            usedInnerWidth = Math.Max(0, ClampInner(TracksSize(columns, columnGap), box.MinWidth, box.MaxWidth, edges.Horizontal));
        }

        Contribution RowContribution(GridItem item)
        {
            var areaWidth = Span(columns, item.Column!.Value, item.ColumnSpan, columnGap);
            var itemBox = ChildBox(item.Node, areaWidth, null, AvailableSpace.Definite(areaWidth));
            var itemWidth = ItemSize(item.Node, itemBox, areaWidth, row: true, item.Justify);
            var contentHeight = Layout(item.Node, itemBox, itemWidth, null, AvailableSpace.Definite(itemWidth), AvailableSpace.MaxContent, perform: false).Height;
            var scrolls = item.Node.Style.OverflowY != Overflow.Visible;
            var minimum = itemBox.MinHeight ?? (scrolls ? 0 : contentHeight);
            var margins = itemBox.Margin.Vertical;
            return new Contribution(minimum + margins, contentHeight + margins, contentHeight + margins);
        }

        var rowSpace = innerHeight is { } knownHeight ? AvailableSpace.Definite(knownHeight)
            : availableHeight.Kind == AvailableKind.MinContent ? AvailableSpace.MinContent : AvailableSpace.MaxContent;
        SizeTracks(rows, items, false, rowSpace, rowGap, percentHeight, stretchRows, RowContribution);
        var usedInnerHeight = innerHeight ?? Math.Max(0, ClampInner(TracksSize(rows, rowGap), box.MinHeight, box.MaxHeight, edges.Vertical));

        var finalWidth = width ?? box.ClampWidth(usedInnerWidth + edges.Horizontal);
        var finalHeight = height ?? box.ClampHeight(usedInnerHeight + edges.Vertical);
        if (!perform) return new Size(finalWidth, finalHeight);

        PositionTracks(columns, finalWidth - edges.Horizontal, columnGap, ToDistribution(style.JustifyContent));
        PositionTracks(rows, finalHeight - edges.Vertical, rowGap, style.AlignContent);
        foreach (var item in items) PlaceGridItem(item, columns, rows, columnGap, rowGap, edges);
        if (tableRows is not null) PositionTableParts(node, items, tableRows, rows, finalWidth, edges);
        return new Size(finalWidth, finalHeight);
    }

    private static int Repetitions(GridTrackList list, float? size, float gap)
    {
        if (list.Repeat == GridAutoRepeat.None) return 0;
        if (size is not { } available) return 1;
        float Resolve(TrackSize track) => (float)(track.Max.IsFixed ? track.Max.Length : track.Min.Length).Resolve(available);
        var fixedSize = list.Tracks.Items.Sum(Resolve);
        var repeatSize = list.RepeatTracks.Items.Sum(Resolve);
        var fixedCount = list.Tracks.Items.Count;
        var repeatCount = list.RepeatTracks.Items.Count;
        bool Fits(int repetitions) => fixedSize + repetitions * repeatSize + gap * (fixedCount + repetitions * repeatCount - 1) <= available + 1e-3f;
        var result = 1;
        while (result < 10000 && repeatSize + gap * repeatCount > 0 && Fits(result + 1)) result++;
        return result;
    }

    private static (int? Start, int Span) ResolveLines(GridLine start, GridLine end, int explicitCount, GridAreas areas, bool rows)
    {
        int? Line(GridLine line, bool isEnd) => line.Kind switch
        {
            GridLineKind.Line => line.Value > 0 ? line.Value - 1 : explicitCount + 1 + line.Value,
            GridLineKind.Area when areas.TryGetArea(line.Name!, out var area) =>
                rows ? (isEnd ? area.RowEnd : area.RowStart) : (isEnd ? area.ColumnEnd : area.ColumnStart),
            _ => null,
        };
        var first = Line(start, false);
        var last = Line(end, true);
        var startSpan = start.Kind == GridLineKind.Span ? start.Value : 0;
        var endSpan = end.Kind == GridLineKind.Span ? end.Value : 0;
        if (first is { } a && last is { } b)
        {
            if (a == b) b = a + 1;
            if (b < a) (a, b) = (b, a);
            return (a, b - a);
        }
        if (first is { } s) return (s, endSpan > 0 ? endSpan : 1);

        if (last is { } e) return (e - (startSpan > 0 ? startSpan : 1), startSpan > 0 ? startSpan : 1);
        return (null, startSpan > 0 ? startSpan : endSpan > 0 ? endSpan : 1);
    }

    private static (int Columns, int Rows, int ColumnOffset, int RowOffset) PlaceGridItems(List<GridItem> items, int explicitColumns, int explicitRows, GridAutoFlow flow)
    {
        var columnOffset = Math.Max(0, -items.Where(i => i.Column is not null).Select(i => i.Column!.Value).DefaultIfEmpty(0).Min());
        var rowOffset = Math.Max(0, -items.Where(i => i.Row is not null).Select(i => i.Row!.Value).DefaultIfEmpty(0).Min());
        foreach (var item in items)
        {
            item.Column += columnOffset;
            item.Row += rowOffset;
        }
        var byRow = flow is GridAutoFlow.Row or GridAutoFlow.RowDense;
        var dense = flow is GridAutoFlow.RowDense or GridAutoFlow.ColumnDense;

        int? Major(GridItem i) => byRow ? i.Row : i.Column;
        int? Minor(GridItem i) => byRow ? i.Column : i.Row;
        int MajorSpan(GridItem i) => byRow ? i.RowSpan : i.ColumnSpan;
        int MinorSpan(GridItem i) => byRow ? i.ColumnSpan : i.RowSpan;
        void Set(GridItem i, int major, int minor)
        {
            if (byRow) (i.Row, i.Column) = (major, minor);
            else (i.Column, i.Row) = (major, minor);
        }

        var occupied = new HashSet<(int, int)>();
        bool Fits(int major, int minor, int majorSpan, int minorSpan)
        {
            for (var a = major; a < major + majorSpan; a++)
                for (var b = minor; b < minor + minorSpan; b++)
                    if (occupied.Contains((a, b))) return false;
            return true;
        }
        void Occupy(GridItem i)
        {
            for (var a = Major(i)!.Value; a < Major(i)!.Value + MajorSpan(i); a++)
                for (var b = Minor(i)!.Value; b < Minor(i)!.Value + MinorSpan(i); b++)
                    occupied.Add((a, b));
        }

        foreach (var item in items.Where(i => Major(i) is not null && Minor(i) is not null)) Occupy(item);

        var cursors = new Dictionary<int, int>();
        foreach (var item in items.Where(i => Major(i) is not null && Minor(i) is null))
        {
            var major = Major(item)!.Value;
            var minor = dense ? 0 : cursors.GetValueOrDefault(major);
            while (!Fits(major, minor, MajorSpan(item), MinorSpan(item))) minor++;
            Set(item, major, minor);
            Occupy(item);
            cursors[major] = minor + MinorSpan(item);
        }

        var minorCount = Math.Max((byRow ? explicitColumns + columnOffset : explicitRows + rowOffset),
            items.Where(i => Minor(i) is not null).Select(i => Minor(i)!.Value + MinorSpan(i)).DefaultIfEmpty(0).Max());
        minorCount = Math.Max(minorCount, items.Where(i => Minor(i) is null).Select(MinorSpan).DefaultIfEmpty(0).Max());

        var cursorMajor = 0;
        var cursorMinor = 0;
        foreach (var item in items.Where(i => Major(i) is null))
        {
            var majorSpan = MajorSpan(item);
            var minorSpan = MinorSpan(item);
            if (dense) (cursorMajor, cursorMinor) = (0, 0);
            if (Minor(item) is { } minor)
            {
                if (!dense && minor < cursorMinor) cursorMajor++;
                cursorMinor = minor;
                while (!Fits(cursorMajor, minor, majorSpan, minorSpan)) cursorMajor++;
                Set(item, cursorMajor, minor);
            }
            else
            {
                while (true)
                {
                    while (cursorMinor + minorSpan <= minorCount && !Fits(cursorMajor, cursorMinor, majorSpan, minorSpan)) cursorMinor++;
                    if (cursorMinor + minorSpan <= minorCount) break;
                    cursorMajor++;
                    cursorMinor = 0;
                }
                Set(item, cursorMajor, cursorMinor);
            }
            Occupy(item);
        }
        var columns = Math.Max(explicitColumns + columnOffset, items.Select(i => i.Column!.Value + i.ColumnSpan).DefaultIfEmpty(0).Max());
        var rows = Math.Max(explicitRows + rowOffset, items.Select(i => i.Row!.Value + i.RowSpan).DefaultIfEmpty(0).Max());
        return (columns, rows, columnOffset, rowOffset);
    }

    private static List<GridTrack> BuildTracks(IReadOnlyList<TrackSize> template, ValueList<TrackSize> auto, int offset, int count)
    {
        var tracks = new List<GridTrack>(count);
        var autoCount = auto.Items.Count;
        for (var i = 0; i < count; i++)
        {
            var index = i - offset;
            var size = index >= 0 && index < template.Count ? template[index]
                : autoCount == 0 ? TrackSize.Auto
                : index < 0 ? auto.Items[((index % autoCount) + autoCount) % autoCount]
                : auto.Items[(index - template.Count) % autoCount];
            tracks.Add(new GridTrack { Size = size });
        }
        return tracks;
    }

    private static void CollapseEmptyRepetitions(GridTrackList list, int repetitions, List<GridTrack> tracks, int offset, List<GridItem> items, bool columnsAxis)
    {
        if (list.Repeat != GridAutoRepeat.Fit) return;
        var first = offset + list.RepeatIndex;
        var last = first + repetitions * list.RepeatTracks.Items.Count;
        for (var i = first; i < last && i < tracks.Count; i++)
        {
            var used = items.Any(item => columnsAxis
                ? item.Column <= i && i < item.Column + item.ColumnSpan
                : item.Row <= i && i < item.Row + item.RowSpan);
            if (!used) tracks[i].Collapsed = true;
        }
    }

    private static TrackSize Effective(TrackSize size, float? basis)
    {
        if (basis is not null) return size;
        if (size.FitContent && size.Max.Length.IsPercent) return new TrackSize(TrackBreadth.Auto, TrackBreadth.MaxContent);
        var min = size.Min.IsFixed && size.Min.Length.IsPercent ? TrackBreadth.Auto : size.Min;
        var max = !size.FitContent && size.Max.IsFixed && size.Max.Length.IsPercent ? TrackBreadth.Auto : size.Max;
        return size with { Min = min, Max = max };
    }

    private static void SizeTracks(List<GridTrack> tracks, List<GridItem> items, bool columns, AvailableSpace available, float gap, float? percentBasis,
        bool stretchAuto, Func<GridItem, Contribution> contribute)
    {

        foreach (var track in tracks)
        {
            if (track.Collapsed)
            {
                track.Base = track.Growth = 0;
                continue;
            }
            var size = track.Effective = Effective(track.Size, percentBasis);
            track.Base = size.Min.IsFixed ? size.Min.Length.At(percentBasis ?? 0) : 0;
            track.Growth = size.Max.IsFixed && !size.FitContent ? size.Max.Length.At(percentBasis ?? 0) : float.PositiveInfinity;
            if (track.Growth < track.Base) track.Growth = track.Base;
        }
        var gaps = gap * Math.Max(0, tracks.Count(t => !t.Collapsed) - 1);
        IReadOnlyList<GridTrack> Spanned(GridItem item)
        {
            var start = columns ? item.Column!.Value : item.Row!.Value;
            var span = columns ? item.ColumnSpan : item.RowSpan;
            return tracks.Skip(start).Take(span).Where(t => !t.Collapsed).ToArray();
        }
        float SpanGaps(IReadOnlyList<GridTrack> spanned) => gap * Math.Max(0, spanned.Count - 1);

        var crossingFlexible = new List<(GridItem Item, IReadOnlyList<GridTrack> Tracks)>();
        foreach (var group in items.Select(i => (Item: i, Tracks: Spanned(i))).Where(e => e.Tracks.Count > 0).GroupBy(e => e.Tracks.Count).OrderBy(g => g.Key))
        {
            foreach (var (item, spanned) in group)
            {
                if (spanned.Any(t => t.IsFlexible))
                {
                    crossingFlexible.Add((item, spanned));
                    continue;
                }
                var c = contribute(item);
                if (spanned.Count == 1)
                {
                    var track = spanned[0];
                    track.Base = Math.Max(track.Base, MinimumFor(track.Effective.Min, c, available));
                    if (MaximumFor(track.Effective, c, percentBasis) is { } limit)
                        track.Growth = float.IsPositiveInfinity(track.Growth) ? limit : Math.Max(track.Growth, limit);
                    if (track.Growth < track.Base) track.Growth = track.Base;
                    continue;
                }
                var intrinsicMin = spanned.Where(t => t.Effective.Min.IsIntrinsic).ToArray();
                if (intrinsicMin.Length > 0)
                {
                    var needed = intrinsicMin.Max(t => MinimumFor(t.Effective.Min, c, available));
                    var extra = needed - spanned.Sum(t => t.Base) - SpanGaps(spanned);
                    foreach (var track in intrinsicMin)
                    {
                        if (extra > 0) track.Base += extra / intrinsicMin.Length;
                        if (track.Growth < track.Base) track.Growth = track.Base;
                    }
                }
                var intrinsicMax = spanned.Where(t => t.Effective.Max.IsIntrinsic || t.Effective.FitContent).ToArray();
                if (intrinsicMax.Length > 0)
                {
                    var target = intrinsicMax.All(t => t.Effective.Max.Kind == TrackBreadthKind.MinContent && !t.Effective.FitContent) ? c.MinContent : c.MaxContent;
                    var current = spanned.Sum(t => float.IsPositiveInfinity(t.Growth) ? t.Base : t.Growth) + SpanGaps(spanned);
                    foreach (var track in intrinsicMax)
                    {
                        if (float.IsPositiveInfinity(track.Growth)) track.Growth = track.Base;
                        if (target > current) track.Growth += (target - current) / intrinsicMax.Length;
                    }
                }
            }
        }

        foreach (var (item, spanned) in crossingFlexible)
        {
            var flexible = spanned.Where(t => t.IsFlexible && t.Effective.Min.IsIntrinsic).ToArray();
            if (flexible.Length == 0) continue;
            var c = contribute(item);
            var needed = available.IsDefinite ? c.Minimum : c.MinContent;
            var extra = needed - spanned.Sum(t => t.Base) - SpanGaps(spanned);
            if (extra <= 0) continue;
            var factors = flexible.Sum(t => t.Flex);
            foreach (var track in flexible) track.Base += extra * (factors > 0 ? track.Flex / factors : 1f / flexible.Length);
        }
        foreach (var track in tracks)
            if (float.IsPositiveInfinity(track.Growth) || track.Growth < track.Base) track.Growth = track.Base;

        var free = available.Kind switch
        {
            AvailableKind.Definite => available.Value - tracks.Sum(t => t.Base) - gaps,
            AvailableKind.MaxContent => float.PositiveInfinity,
            _ => 0,
        };
        if (float.IsPositiveInfinity(free))
        {
            foreach (var track in tracks) track.Base = track.Growth;
        }
        else
        {
            for (var guard = 0; free > 1e-3f && guard <= tracks.Count; guard++)
            {
                var growing = tracks.Where(t => !t.Collapsed && t.Base < t.Growth).ToArray();
                if (growing.Length == 0) break;
                var share = free / growing.Length;
                foreach (var track in growing)
                {
                    var step = Math.Min(share, track.Growth - track.Base);
                    track.Base += step;
                    free -= step;
                }
            }
        }

        var flexTracks = tracks.Where(t => !t.Collapsed && t.IsFlexible).ToArray();
        if (flexTracks.Length > 0 && available.Kind != AvailableKind.MinContent)
        {
            float fraction;
            if (available.IsDefinite)
                fraction = available.Value - tracks.Sum(t => t.Base) - gaps <= 0 ? 0 : FrSize(tracks.Where(t => !t.Collapsed).ToArray(), available.Value - gaps);
            else
            {
                fraction = flexTracks.Max(t => t.Flex > 1 ? t.Base / t.Flex : t.Base);
                foreach (var (item, spanned) in crossingFlexible)
                    fraction = Math.Max(fraction, FrSize(spanned, contribute(item).MaxContent - SpanGaps(spanned)));
            }
            foreach (var track in flexTracks)
            {
                var size = fraction * track.Flex;
                if (size > track.Base) track.Base = track.Growth = size;
            }
        }

        if (stretchAuto && available.IsDefinite)
        {
            var remaining = available.Value - tracks.Sum(t => t.Base) - gaps;
            var autos = tracks.Where(t => !t.Collapsed && t.Effective.Max.Kind == TrackBreadthKind.Auto && !t.Effective.FitContent).ToArray();
            if (remaining > 0 && autos.Length > 0)
                foreach (var track in autos) track.Base += remaining / autos.Length;
        }
    }

    private static float MinimumFor(TrackBreadth min, Contribution c, AvailableSpace available) => min.Kind switch
    {
        TrackBreadthKind.MinContent => c.MinContent,
        TrackBreadthKind.MaxContent => c.MaxContent,

        TrackBreadthKind.Auto => available.IsDefinite ? c.Minimum : c.MinContent,
        _ => 0,
    };

    private static float? MaximumFor(TrackSize size, Contribution c, float? basis)
    {
        if (size.FitContent) return Math.Min(c.MaxContent, size.Max.Length.At(basis ?? 0));
        return size.Max.Kind switch
        {
            TrackBreadthKind.MinContent => c.MinContent,
            TrackBreadthKind.MaxContent or TrackBreadthKind.Auto => c.MaxContent,
            _ => null,
        };
    }

    private static float FrSize(IReadOnlyList<GridTrack> tracks, float space)
    {
        var inflexible = new HashSet<GridTrack>(tracks.Where(t => !t.IsFlexible));
        while (true)
        {
            var leftover = space - tracks.Where(inflexible.Contains).Sum(t => t.Base);
            var flexible = tracks.Where(t => !inflexible.Contains(t)).ToArray();
            if (flexible.Length == 0) return 0;
            var factors = Math.Max(1, flexible.Sum(t => t.Flex));
            var size = leftover / factors;
            var violating = flexible.Where(t => size * t.Flex < t.Base).ToArray();
            if (violating.Length == 0) return Math.Max(0, size);
            foreach (var track in violating) inflexible.Add(track);
        }
    }

    private static float TracksSize(List<GridTrack> tracks, float gap) =>
        tracks.Sum(t => t.Base) + gap * Math.Max(0, tracks.Count(t => !t.Collapsed) - 1);

    private static float Span(List<GridTrack> tracks, int start, int span, float gap)
    {
        var spanned = tracks.Skip(start).Take(span).Where(t => !t.Collapsed).ToArray();
        return spanned.Sum(t => t.Base) + gap * Math.Max(0, spanned.Length - 1);
    }

    private static AlignContent ToDistribution(JustifyContent justify) => justify switch
    {
        JustifyContent.Normal => AlignContent.Normal,
        JustifyContent.Stretch => AlignContent.Stretch,
        JustifyContent.FlexEnd => AlignContent.FlexEnd,
        JustifyContent.Center => AlignContent.Center,
        JustifyContent.SpaceBetween => AlignContent.SpaceBetween,
        JustifyContent.SpaceAround => AlignContent.SpaceAround,
        JustifyContent.SpaceEvenly => AlignContent.SpaceEvenly,
        _ => AlignContent.FlexStart,
    };

    private static void PositionTracks(List<GridTrack> tracks, float inner, float gap, AlignContent distribution)
    {
        var count = tracks.Count(t => !t.Collapsed);
        var free = inner - TracksSize(tracks, gap);
        var start = 0f;
        var between = gap;
        if (free > 0)
        {
            switch (distribution)
            {
                case AlignContent.FlexEnd:
                    start = free;
                    break;
                case AlignContent.Center:
                    start = free / 2;
                    break;
                case AlignContent.SpaceBetween when count > 1:
                    between += free / (count - 1);
                    break;
                case AlignContent.SpaceAround when count > 0:
                    between += free / count;
                    start = free / count / 2;
                    break;
                case AlignContent.SpaceEvenly when count > 0:
                    between += free / (count + 1);
                    start = free / (count + 1);
                    break;
            }
        }
        else if (distribution == AlignContent.Center) start = free / 2;
        else if (distribution == AlignContent.FlexEnd) start = free;
        var position = start;
        var first = true;
        foreach (var track in tracks)
        {
            if (track.Collapsed)
            {
                track.Start = position;
                continue;
            }
            if (!first) position += between;
            first = false;
            track.Start = position;
            position += track.Base;
        }
    }

    private static float ItemSize(LayoutNode node, BoxModel box, float area, bool row, AlignSelf align)
    {
        var margins = row ? box.Margin.Horizontal : box.Margin.Vertical;
        var space = Math.Max(0, area - margins);
        if ((row ? box.Width : box.Height) is { } preferred) return row ? box.ClampWidth(preferred) : box.ClampHeight(preferred);
        var autoMargins = row ? box.AutoMarginLeft || box.AutoMarginRight : box.AutoMarginTop || box.AutoMarginBottom;
        if (align == AlignSelf.Stretch && !autoMargins && (row ? box.AutoWidth : box.AutoHeight)) return row ? box.ClampWidth(space) : box.ClampHeight(space);
        if (!row) return space;
        var minContent = Layout(node, box, null, null, AvailableSpace.MinContent, AvailableSpace.MaxContent, perform: false).Width;
        var maxContent = Layout(node, box, null, null, AvailableSpace.MaxContent, AvailableSpace.MaxContent, perform: false).Width;
        return box.ClampWidth(Math.Min(maxContent, Math.Max(minContent, space)));
    }

    private static void PlaceGridItem(GridItem item, List<GridTrack> columns, List<GridTrack> rows, float columnGap, float rowGap, Thickness edges)
    {
        var column = item.Column!.Value;
        var row = item.Row!.Value;
        var areaX = columns[column].Start;
        var areaY = rows[row].Start;
        var areaWidth = Span(columns, column, item.ColumnSpan, columnGap);
        var areaHeight = Span(rows, row, item.RowSpan, rowGap);
        var node = item.Node;
        var box = ChildBox(node, areaWidth, areaHeight, AvailableSpace.Definite(areaWidth));
        node.Padding = box.Padding;
        node.Border = box.Border;
        var width = ItemSize(node, box, areaWidth, row: true, item.Justify);
        float height;
        var autoVertical = box.AutoMarginTop || box.AutoMarginBottom;
        if (box.Height is { } preferred) height = box.ClampHeight(preferred);
        else if (box.AspectRatio > 0 && !box.IntrinsicHeight) height = box.ClampHeight(box.HeightFor(width));
        else if (item.Align == AlignSelf.Stretch && !autoVertical && box.AutoHeight) height = box.ClampHeight(Math.Max(0, areaHeight - box.Margin.Vertical));
        else height = Layout(node, box, width, null, AvailableSpace.Definite(width), AvailableSpace.Definite(areaHeight), perform: false).Height;

        var (marginLeft, marginRight) = AlignInArea(box.Margin.Left, box.Margin.Right, box.AutoMarginLeft, box.AutoMarginRight, areaWidth - width - box.Margin.Horizontal, item.Justify, out var offsetX);
        var (marginTop, marginBottom) = AlignInArea(box.Margin.Top, box.Margin.Bottom, box.AutoMarginTop, box.AutoMarginBottom, areaHeight - height - box.Margin.Vertical, item.Align, out var offsetY);
        var x = edges.Left + areaX + offsetX + marginLeft;
        var y = edges.Top + areaY + offsetY + marginTop;
        if (node.Style.Position == Position.Relative)
        {
            var (dx, dy) = RelativeOffset(node.Style, areaWidth, areaHeight);
            x += dx;
            y += dy;
        }
        node.Frame = new Rect(x, y, width, height);
        node.Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom);
        Layout(node, box, width, height, AvailableSpace.Definite(width), AvailableSpace.Definite(height), perform: true);
    }

    private static (float Start, float End) AlignInArea(float start, float end, bool autoStart, bool autoEnd, float free, AlignSelf align, out float offset)
    {
        offset = 0;
        if (autoStart || autoEnd)
        {
            var share = Math.Max(0, free);
            if (autoStart && autoEnd) return (share / 2, share / 2);
            return autoStart ? (share, end) : (start, share);
        }
        offset = align switch
        {
            AlignSelf.FlexEnd => free,
            AlignSelf.Center => free / 2,
            _ => 0,
        };
        return (start, end);
    }
}
