using Nazor.Style.Syntax;
using static Nazor.Style.Syntax.ValueParsers;

namespace Nazor.Style;

/// <summary>
/// Provides the registered NSS longhand properties. Each descriptor specifies its grammar, initial value,
/// inheritance, and invalidation effects. Unsupported property names are rejected during stylesheet compilation.
/// </summary>
public static partial class StyleProperties
{
    private static readonly List<StyleProperty> AllProperties = [];
    private static readonly Dictionary<string, StyleProperty> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ShorthandProperty> Shorthands = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Display> DisplayKeywords = new()
    {
        ["block"] = Style.Display.Block, ["inline"] = Style.Display.Inline, ["flex"] = Style.Display.Flex, ["grid"] = Style.Display.Grid, ["none"] = Style.Display.None,
    };
    private static readonly Dictionary<string, BackgroundRepeat> RepeatKeywords = new()
    {
        ["repeat"] = Style.BackgroundRepeat.Repeat, ["no-repeat"] = Style.BackgroundRepeat.NoRepeat,
        ["repeat-x"] = Style.BackgroundRepeat.RepeatX, ["repeat-y"] = Style.BackgroundRepeat.RepeatY,
    };

    private static readonly Dictionary<string, BlendMode> BlendKeywords = new()
    {
        ["normal"] = Style.BlendMode.Normal,
        ["multiply"] = Style.BlendMode.Multiply,
        ["screen"] = Style.BlendMode.Screen,
        ["overlay"] = Style.BlendMode.Overlay,
        ["darken"] = Style.BlendMode.Darken,
        ["lighten"] = Style.BlendMode.Lighten,
        ["color-dodge"] = Style.BlendMode.ColorDodge,
        ["color-burn"] = Style.BlendMode.ColorBurn,
        ["hard-light"] = Style.BlendMode.HardLight,
        ["soft-light"] = Style.BlendMode.SoftLight,
        ["difference"] = Style.BlendMode.Difference,
        ["exclusion"] = Style.BlendMode.Exclusion,
        ["hue"] = Style.BlendMode.Hue,
        ["saturation"] = Style.BlendMode.Saturation,
        ["color"] = Style.BlendMode.Color,
        ["luminosity"] = Style.BlendMode.Luminosity,
    };
    private static readonly Dictionary<string, Isolation> IsolationKeywords = new()
    {
        ["auto"] = Style.Isolation.Auto, ["isolate"] = Style.Isolation.Isolate,
    };

    private static readonly Dictionary<string, ObjectFit> ObjectFitKeywords = new()
    {
        ["fill"] = Style.ObjectFit.Fill, ["contain"] = Style.ObjectFit.Contain, ["cover"] = Style.ObjectFit.Cover,
        ["none"] = Style.ObjectFit.None, ["scale-down"] = Style.ObjectFit.ScaleDown,
    };

    private static readonly Dictionary<string, FlexDirection> DirectionKeywords = new()
    {
        ["row"] = Style.FlexDirection.Row, ["row-reverse"] = Style.FlexDirection.RowReverse,
        ["column"] = Style.FlexDirection.Column, ["column-reverse"] = Style.FlexDirection.ColumnReverse,
    };
    private static readonly Dictionary<string, FlexWrap> WrapKeywords = new()
    {
        ["nowrap"] = Style.FlexWrap.NoWrap, ["wrap"] = Style.FlexWrap.Wrap, ["wrap-reverse"] = Style.FlexWrap.WrapReverse,
    };
    private static readonly Dictionary<string, AlignContent> AlignContentKeywords = new()
    {
        ["normal"] = Style.AlignContent.Normal, ["stretch"] = Style.AlignContent.Stretch,
        ["flex-start"] = Style.AlignContent.FlexStart, ["start"] = Style.AlignContent.FlexStart,
        ["flex-end"] = Style.AlignContent.FlexEnd, ["end"] = Style.AlignContent.FlexEnd, ["center"] = Style.AlignContent.Center,
        ["space-between"] = Style.AlignContent.SpaceBetween, ["space-around"] = Style.AlignContent.SpaceAround,
        ["space-evenly"] = Style.AlignContent.SpaceEvenly,
    };
    private static readonly Dictionary<string, JustifyContent> JustifyKeywords = new()
    {
        ["normal"] = Style.JustifyContent.Normal, ["stretch"] = Style.JustifyContent.Stretch,
        ["flex-start"] = Style.JustifyContent.FlexStart, ["start"] = Style.JustifyContent.FlexStart,
        ["flex-end"] = Style.JustifyContent.FlexEnd, ["end"] = Style.JustifyContent.FlexEnd, ["center"] = Style.JustifyContent.Center,
        ["space-between"] = Style.JustifyContent.SpaceBetween, ["space-around"] = Style.JustifyContent.SpaceAround,
        ["space-evenly"] = Style.JustifyContent.SpaceEvenly,
    };
    private static readonly Dictionary<string, AlignItems> AlignItemsKeywords = new()
    {
        ["normal"] = Style.AlignItems.Stretch, ["stretch"] = Style.AlignItems.Stretch, ["flex-start"] = Style.AlignItems.FlexStart, ["start"] = Style.AlignItems.FlexStart,
        ["flex-end"] = Style.AlignItems.FlexEnd, ["end"] = Style.AlignItems.FlexEnd, ["center"] = Style.AlignItems.Center,
    };
    private static readonly Dictionary<string, AlignSelf> AlignSelfKeywords = new()
    {
        ["auto"] = Style.AlignSelf.Auto, ["normal"] = Style.AlignSelf.Stretch, ["stretch"] = Style.AlignSelf.Stretch, ["flex-start"] = Style.AlignSelf.FlexStart,
        ["start"] = Style.AlignSelf.FlexStart, ["flex-end"] = Style.AlignSelf.FlexEnd, ["end"] = Style.AlignSelf.FlexEnd,
        ["center"] = Style.AlignSelf.Center,
    };
    private static readonly Dictionary<string, Position> PositionKeywords = new()
    {
        ["static"] = Style.Position.Static, ["relative"] = Style.Position.Relative, ["absolute"] = Style.Position.Absolute,
    };
    private static readonly Dictionary<string, BoxSizing> BoxSizingKeywords = new() { ["border-box"] = Style.BoxSizing.BorderBox, ["content-box"] = Style.BoxSizing.ContentBox };
    private static readonly Dictionary<string, BorderStyle> BorderStyleKeywords = new()
    {
        ["none"] = Style.BorderStyle.None, ["solid"] = Style.BorderStyle.Solid, ["dashed"] = Style.BorderStyle.Dashed, ["dotted"] = Style.BorderStyle.Dotted,
    };
    private static readonly Dictionary<string, Overflow> OverflowKeywords = new()
    {
        ["visible"] = Style.Overflow.Visible, ["hidden"] = Style.Overflow.Hidden, ["scroll"] = Style.Overflow.Scroll, ["auto"] = Style.Overflow.Auto,
    };
    private static readonly Dictionary<string, ScrollbarVisibility> ScrollbarVisibilityKeywords = new()
    {
        ["none"] = Style.ScrollbarVisibility.None, ["visible"] = Style.ScrollbarVisibility.Visible, ["autohide"] = Style.ScrollbarVisibility.AutoHide,
    };
    private static readonly Dictionary<string, FontStyle> FontStyleKeywords = new() { ["normal"] = Style.FontStyle.Normal, ["italic"] = Style.FontStyle.Italic };
    private static readonly Dictionary<string, FontOpticalSizing> FontOpticalSizingKeywords = new() { ["auto"] = Style.FontOpticalSizing.Auto, ["none"] = Style.FontOpticalSizing.None, ["native"] = Style.FontOpticalSizing.Native };
    private static readonly Dictionary<string, TextAlign> TextAlignKeywords = new()
    {
        ["start"] = Style.TextAlign.Start, ["end"] = Style.TextAlign.End, ["left"] = Style.TextAlign.Left,
        ["right"] = Style.TextAlign.Right, ["center"] = Style.TextAlign.Center,
    };
    private static readonly Dictionary<string, WhiteSpace> WhiteSpaceKeywords = new()
    {
        ["normal"] = Style.WhiteSpace.Normal, ["nowrap"] = Style.WhiteSpace.NoWrap, ["pre"] = Style.WhiteSpace.Pre,
        ["pre-wrap"] = Style.WhiteSpace.PreWrap, ["pre-line"] = Style.WhiteSpace.PreLine,
    };
    private static readonly Dictionary<string, TextDecorationStyle> DecorationStyleKeywords = new()
    {
        ["solid"] = Style.TextDecorationStyle.Solid, ["double"] = Style.TextDecorationStyle.Double, ["dotted"] = Style.TextDecorationStyle.Dotted,
        ["dashed"] = Style.TextDecorationStyle.Dashed, ["wavy"] = Style.TextDecorationStyle.Wavy,
    };
    private static readonly Dictionary<string, TextOverflow> TextOverflowKeywords = new() { ["clip"] = Style.TextOverflow.Clip, ["ellipsis"] = Style.TextOverflow.Ellipsis };
    private static readonly Dictionary<string, OverflowWrap> OverflowWrapKeywords = new()
    {
        ["normal"] = Style.OverflowWrap.Normal, ["break-word"] = Style.OverflowWrap.BreakWord, ["anywhere"] = Style.OverflowWrap.Anywhere,
    };
    private static readonly Dictionary<string, TextTransform> TextTransformKeywords = new()
    {
        ["none"] = Style.TextTransform.None, ["uppercase"] = Style.TextTransform.Uppercase, ["lowercase"] = Style.TextTransform.Lowercase,
        ["capitalize"] = Style.TextTransform.Capitalize,
    };
    private static readonly Dictionary<string, PointerEvents> PointerEventsKeywords = new() { ["auto"] = Style.PointerEvents.Auto, ["none"] = Style.PointerEvents.None };
    private static readonly Dictionary<string, UserSelect> UserSelectKeywords = new()
    {
        ["auto"] = Style.UserSelect.Auto, ["none"] = Style.UserSelect.None, ["text"] = Style.UserSelect.Text, ["all"] = Style.UserSelect.All,
    };
    private static readonly Dictionary<string, Cursor> CursorKeywords = new()
    {
        ["default"] = Style.Cursor.Default, ["pointer"] = Style.Cursor.Pointer, ["text"] = Style.Cursor.Text,
        ["col-resize"] = Style.Cursor.ResizeHorizontal, ["ew-resize"] = Style.Cursor.ResizeHorizontal,
        ["row-resize"] = Style.Cursor.ResizeVertical, ["ns-resize"] = Style.Cursor.ResizeVertical,
        ["move"] = Style.Cursor.Move, ["not-allowed"] = Style.Cursor.NotAllowed, ["wait"] = Style.Cursor.Wait,
        ["progress"] = Style.Cursor.Progress, ["help"] = Style.Cursor.Help, ["crosshair"] = Style.Cursor.Crosshair,
        ["nwse-resize"] = Style.Cursor.ResizeNorthwestSoutheast, ["nesw-resize"] = Style.Cursor.ResizeNortheastSouthwest,
    };
    private static readonly Dictionary<string, Visibility> VisibilityKeywords = new() { ["visible"] = Style.Visibility.Visible, ["hidden"] = Style.Visibility.Hidden };

    private const string LineWidth = "thin | medium | thick | <length>";
    private const string BorderSideSyntax = "[thin | medium | thick | <length>] || [none | solid | dashed | dotted] || <color>";
    private const string Intrinsic = "min-content | max-content | fit-content";
    private const string GridLineSyntax = "auto | <integer> | span <integer> | <custom-ident>";

    private static string Choice<T>(Dictionary<string, T> keywords) where T : notnull => string.Join(" | ", keywords.Keys);

    /// <summary>Gets the descriptor for the display NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<Display> Display = Add("display", Style.Display.Block, false, StyleEffects.Layout, r => ParseKeyword(r, DisplayKeywords), syntax: Choice(DisplayKeywords));

    /// <summary>
    /// Gets the descriptor for the flex-direction NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<FlexDirection> FlexDirection = Add("flex-direction", Style.FlexDirection.Row, false, StyleEffects.Layout, r => ParseKeyword(r, DirectionKeywords), syntax: Choice(DirectionKeywords));

    /// <summary>
    /// Gets the descriptor for the flex-wrap NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<FlexWrap> FlexWrap = Add("flex-wrap", Style.FlexWrap.NoWrap, false, StyleEffects.Layout, r => ParseKeyword(r, WrapKeywords), syntax: Choice(WrapKeywords));

    /// <summary>
    /// Gets the descriptor for the justify-content NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<JustifyContent> JustifyContent = Add("justify-content", Style.JustifyContent.Normal, false, StyleEffects.Layout, r => ParseKeyword(r, JustifyKeywords), syntax: Choice(JustifyKeywords));

    /// <summary>
    /// Gets the descriptor for the align-content NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AlignContent> AlignContent = Add("align-content", Style.AlignContent.Normal, false, StyleEffects.Layout, r => ParseKeyword(r, AlignContentKeywords), syntax: Choice(AlignContentKeywords));

    /// <summary>
    /// Gets the descriptor for the align-items NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AlignItems> AlignItems = Add("align-items", Style.AlignItems.Stretch, false, StyleEffects.Layout, r => ParseKeyword(r, AlignItemsKeywords), syntax: Choice(AlignItemsKeywords));

    /// <summary>
    /// Gets the descriptor for the align-self NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AlignSelf> AlignSelf = Add("align-self", Style.AlignSelf.Auto, false, StyleEffects.Layout, r => ParseKeyword(r, AlignSelfKeywords), syntax: Choice(AlignSelfKeywords));

    /// <summary>
    /// Gets the descriptor for the flex-grow NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<double> FlexGrow = Add("flex-grow", 0d, false, StyleEffects.Layout, r => ParseNumber(r, 0), animatable: true, syntax: "<number>");

    /// <summary>
    /// Gets the descriptor for the flex-shrink NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<double> FlexShrink = Add("flex-shrink", 1d, false, StyleEffects.Layout, r => ParseNumber(r, 0), animatable: true, syntax: "<number>");

    /// <summary>
    /// Gets the descriptor for the flex-basis NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> FlexBasis = Add("flex-basis", SizeValue.Auto, false, StyleEffects.Layout, ParseFlexBasis, animatable: true, syntax: $"auto | content | <length-percentage> | {Intrinsic}");

    /// <summary>Gets the descriptor for the order NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<int> Order = Add("order", 0, false, StyleEffects.Layout, ParseInteger, syntax: "<integer>");

    /// <summary>
    /// Gets the descriptor for the grid-template-columns NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridTrackList> GridTemplateColumns = Add("grid-template-columns", GridTrackList.None, false, StyleEffects.Layout, ParseTrackList, syntax: "none | <track-list>");

    /// <summary>
    /// Gets the descriptor for the grid-template-rows NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridTrackList> GridTemplateRows = Add("grid-template-rows", GridTrackList.None, false, StyleEffects.Layout, ParseTrackList, syntax: "none | <track-list>");

    /// <summary>
    /// Gets the descriptor for the grid-template-areas NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridAreas> GridTemplateAreas = Add("grid-template-areas", GridAreas.None, false, StyleEffects.Layout, ParseAreas, syntax: "none | <string>+");

    /// <summary>
    /// Gets the descriptor for the grid-auto-columns NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<TrackSize>> GridAutoColumns = Add("grid-auto-columns", new ValueList<TrackSize>([TrackSize.Auto]), false, StyleEffects.Layout, ParseTrackSizes, syntax: "<track-size>+");

    /// <summary>
    /// Gets the descriptor for the grid-auto-rows NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<TrackSize>> GridAutoRows = Add("grid-auto-rows", new ValueList<TrackSize>([TrackSize.Auto]), false, StyleEffects.Layout, ParseTrackSizes, syntax: "<track-size>+");

    /// <summary>
    /// Gets the descriptor for the grid-auto-flow NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridAutoFlow> GridAutoFlow = Add("grid-auto-flow", Style.GridAutoFlow.Row, false, StyleEffects.Layout, ParseAutoFlow, syntax: "[row | column] || dense");

    /// <summary>
    /// Gets the descriptor for the grid-row-start NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridLine> GridRowStart = Add("grid-row-start", GridLine.Auto, false, StyleEffects.Layout, ParseGridLine, syntax: GridLineSyntax);

    /// <summary>
    /// Gets the descriptor for the grid-row-end NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridLine> GridRowEnd = Add("grid-row-end", GridLine.Auto, false, StyleEffects.Layout, ParseGridLine, syntax: GridLineSyntax);

    /// <summary>
    /// Gets the descriptor for the grid-column-start NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridLine> GridColumnStart = Add("grid-column-start", GridLine.Auto, false, StyleEffects.Layout, ParseGridLine, syntax: GridLineSyntax);

    /// <summary>
    /// Gets the descriptor for the grid-column-end NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<GridLine> GridColumnEnd = Add("grid-column-end", GridLine.Auto, false, StyleEffects.Layout, ParseGridLine, syntax: GridLineSyntax);

    /// <summary>
    /// Gets the descriptor for the justify-items NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AlignItems> JustifyItems = Add("justify-items", Style.AlignItems.Stretch, false, StyleEffects.Layout, r => ParseKeyword(r, AlignItemsKeywords), syntax: Choice(AlignItemsKeywords));

    /// <summary>
    /// Gets the descriptor for the justify-self NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AlignSelf> JustifySelf = Add("justify-self", Style.AlignSelf.Auto, false, StyleEffects.Layout, r => ParseKeyword(r, AlignSelfKeywords), syntax: Choice(AlignSelfKeywords));

    /// <summary>Gets the descriptor for the row-gap NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<Length> RowGap = Add("row-gap", Length.Zero, false, StyleEffects.Layout, ParseGap, animatable: true, syntax: "normal | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the column-gap NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> ColumnGap = Add("column-gap", Length.Zero, false, StyleEffects.Layout, ParseGap, animatable: true, syntax: "normal | <length-percentage>");

    /// <summary>Gets the descriptor for the width NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Width = Add("width", SizeValue.Auto, false, StyleEffects.Layout, r => ParseSize(r, "auto"), animatable: true, syntax: $"auto | <length-percentage> | {Intrinsic}");

    /// <summary>Gets the descriptor for the height NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Height = Add("height", SizeValue.Auto, false, StyleEffects.Layout, r => ParseSize(r, "auto"), animatable: true, syntax: $"auto | <length-percentage> | {Intrinsic}");

    /// <summary>
    /// Gets the descriptor for the min-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MinWidth = Add("min-width", SizeValue.Auto, false, StyleEffects.Layout, r => ParseSize(r, "auto"), animatable: true, syntax: $"auto | <length-percentage> | {Intrinsic}");

    /// <summary>
    /// Gets the descriptor for the min-height NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MinHeight = Add("min-height", SizeValue.Auto, false, StyleEffects.Layout, ParseSizeOrAuto, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the max-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MaxWidth = Add("max-width", SizeValue.None, false, StyleEffects.Layout, r => ParseSize(r, "none"), animatable: true, syntax: $"none | <length-percentage> | {Intrinsic}");

    /// <summary>
    /// Gets the descriptor for the max-height NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MaxHeight = Add("max-height", SizeValue.None, false, StyleEffects.Layout, ParseSizeOrNone, animatable: true, syntax: "none | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the padding-top NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> PaddingTop = Add("padding-top", Length.Zero, false, StyleEffects.Layout, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>
    /// Gets the descriptor for the padding-right NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> PaddingRight = Add("padding-right", Length.Zero, false, StyleEffects.Layout, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>
    /// Gets the descriptor for the padding-bottom NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> PaddingBottom = Add("padding-bottom", Length.Zero, false, StyleEffects.Layout, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>
    /// Gets the descriptor for the padding-left NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> PaddingLeft = Add("padding-left", Length.Zero, false, StyleEffects.Layout, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>
    /// Gets the descriptor for the margin-top NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MarginTop = Add("margin-top", SizeValue.Fixed(Length.Zero), false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the margin-right NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MarginRight = Add("margin-right", SizeValue.Fixed(Length.Zero), false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the margin-bottom NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MarginBottom = Add("margin-bottom", SizeValue.Fixed(Length.Zero), false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the margin-left NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> MarginLeft = Add("margin-left", SizeValue.Fixed(Length.Zero), false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the border-top-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> BorderTopWidth = Add("border-top-width", Length.Zero, false, StyleEffects.Layout, ParseBorderWidth, animatable: true, syntax: LineWidth);

    /// <summary>
    /// Gets the descriptor for the border-right-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> BorderRightWidth = Add("border-right-width", Length.Zero, false, StyleEffects.Layout, ParseBorderWidth, animatable: true, syntax: LineWidth);

    /// <summary>
    /// Gets the descriptor for the border-bottom-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> BorderBottomWidth = Add("border-bottom-width", Length.Zero, false, StyleEffects.Layout, ParseBorderWidth, animatable: true, syntax: LineWidth);

    /// <summary>
    /// Gets the descriptor for the border-left-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> BorderLeftWidth = Add("border-left-width", Length.Zero, false, StyleEffects.Layout, ParseBorderWidth, animatable: true, syntax: LineWidth);

    /// <summary>
    /// Gets the descriptor for the box-sizing NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BoxSizing> BoxSizing = Add("box-sizing", Style.BoxSizing.BorderBox, false, StyleEffects.Layout, r => ParseKeyword(r, BoxSizingKeywords), syntax: Choice(BoxSizingKeywords));

    /// <summary>
    /// Gets the descriptor for the overflow-x NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Overflow> OverflowX = Add("overflow-x", Style.Overflow.Visible, false, StyleEffects.Layout, r => ParseKeyword(r, OverflowKeywords), syntax: Choice(OverflowKeywords));

    /// <summary>
    /// Gets the descriptor for the aspect-ratio NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<AspectRatio> AspectRatio = Add("aspect-ratio", Style.AspectRatio.Auto, false, StyleEffects.Layout, ParseAspectRatio, syntax: "auto | <ratio>");

    /// <summary>
    /// Gets the descriptor for the overflow-y NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Overflow> OverflowY = Add("overflow-y", Style.Overflow.Visible, false, StyleEffects.Layout, r => ParseKeyword(r, OverflowKeywords), syntax: Choice(OverflowKeywords));

    /// <summary>Gets the inherited scrolling behavior; auto applies offsets immediately.</summary>
    public static readonly StyleProperty<ScrollBehavior> ScrollBehavior = Add("scroll-behavior", Style.ScrollBehavior.Auto, true, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, ScrollBehavior> { ["auto"] = Style.ScrollBehavior.Auto, ["smooth"] = Style.ScrollBehavior.Smooth }), syntax: "auto | smooth");

    /// <summary>Gets the inherited duration of a smooth scroll request.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollDuration = Add("scroll-duration", TimeSpan.FromMilliseconds(180), true, StyleEffects.None, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited timing function of smooth scrolling.</summary>
    public static readonly StyleProperty<EasingFunction> ScrollTimingFunction = Add("scroll-timing-function", EasingFunction.EaseOut, true, StyleEffects.None, ParseEasing,
        syntax: "linear | ease | ease-in | ease-out | ease-in-out | <easing-function>");

    /// <summary>Gets the inherited distance per wheel detent; precise pixel input is not scaled.</summary>
    public static readonly StyleProperty<ScrollWheelStep> ScrollWheelStep = Add("scroll-wheel-step", Style.ScrollWheelStep.Auto, true, StyleEffects.None, ParseScrollWheelStep,
        syntax: "auto | <length> | <number> lines");

    private static ScrollWheelStep ParseScrollWheelStep(TokenReader reader)
    {
        if (reader.TryConsumeIdent("auto")) return Style.ScrollWheelStep.Auto;
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Dimension && token.Value.Equals("lines", StringComparison.OrdinalIgnoreCase))
        {
            reader.Next();
            if (!double.IsFinite(token.Number) || token.Number < 0) throw TokenReader.Error("Expected a non-negative line count.", token);
            return new(ScrollWheelStepKind.Lines, Length.Zero, token.Number);
        }
        if (token.Kind == StyleTokenKind.Number)
        {
            var number = ParseNumber(reader, 0);
            if (reader.TryConsumeIdent("lines")) return new(ScrollWheelStepKind.Lines, Length.Zero, number);
            if (number == 0) return new(ScrollWheelStepKind.Length, Length.Zero, 0);
            throw TokenReader.Error("Expected 'lines' after the line count, or a length such as 24px.", token);
        }
        return new(ScrollWheelStepKind.Length, ParseLength(reader, allowPercent: false), 0);
    }

    /// <summary>Controls scrollbar visibility without changing whether the content can scroll. Inherited.</summary>
    public static readonly StyleProperty<ScrollbarVisibility> ScrollbarVisibility = Add("scrollbar-visibility", Style.ScrollbarVisibility.Visible, true, StyleEffects.Paint | StyleEffects.Layout,
        r => ParseKeyword(r, ScrollbarVisibilityKeywords), syntax: Choice(ScrollbarVisibilityKeywords));

    /// <summary>Gets the inherited thickness of both scrollbar axes.</summary>
    public static readonly StyleProperty<Length> ScrollbarWidth = Add("scrollbar-width", Length.Px(4), true, StyleEffects.Paint | StyleEffects.Layout, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited scrollbar thumb color.</summary>
    public static readonly StyleProperty<Color> ScrollbarThumbColor = Add("scrollbar-thumb-color", new Color(128, 128, 128, 176), true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited scrollbar track color.</summary>
    public static readonly StyleProperty<Color> ScrollbarTrackColor = Add("scrollbar-track-color", Style.Color.Transparent, true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited corner radius of scrollbar thumbs and tracks.</summary>
    public static readonly StyleProperty<Length> ScrollbarRadius = Add("scrollbar-radius", Length.Px(2), true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited scrollbar distance from the inner border edge.</summary>
    public static readonly StyleProperty<Length> ScrollbarInset = Add("scrollbar-inset", Length.Px(2), true, StyleEffects.Paint | StyleEffects.Layout, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited minimum thumb length, clamped to the available track.</summary>
    public static readonly StyleProperty<Length> ScrollbarMinThumbLength = Add("scrollbar-min-thumb-length", Length.Px(24), true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited scrollbar opacity, multiplied by automatic reveal opacity.</summary>
    public static readonly StyleProperty<double> ScrollbarOpacity = Add("scrollbar-opacity", 1d, true, StyleEffects.Paint, r => Math.Clamp(ParseNumber(r), 0, 1), animatable: true, syntax: "<number>");

    /// <summary>Gets the inherited duration of the automatic reveal fade.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarFadeInDuration = Add("scrollbar-fade-in-duration", TimeSpan.FromMilliseconds(120), true, StyleEffects.Paint, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited duration of the automatic hide fade.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarFadeOutDuration = Add("scrollbar-fade-out-duration", TimeSpan.FromMilliseconds(200), true, StyleEffects.Paint, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited delay before an automatic hide fade starts.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarFadeOutDelay = Add("scrollbar-fade-out-delay", TimeSpan.FromMilliseconds(300), true, StyleEffects.Paint, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited timing function of automatic scrollbar fades.</summary>
    public static readonly StyleProperty<EasingFunction> ScrollbarFadeTimingFunction = Add("scrollbar-fade-timing-function", EasingFunction.EaseOut, true, StyleEffects.Paint, ParseEasing, syntax: "linear | ease | ease-in | ease-out | ease-in-out | <easing-function>");

    /// <summary>Gets the inherited idle time after the last wheel event before :wheel-scrolling ends.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarWheelTimeout = Add("scrollbar-wheel-timeout", TimeSpan.FromMilliseconds(600), true, StyleEffects.None, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited pointer reveal distance from the viewport edge, at least width plus inset.</summary>
    public static readonly StyleProperty<Length> ScrollbarRevealDistance = Add("scrollbar-reveal-distance", Length.Px(12), true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), syntax: "<length>");

    /// <summary>Gets the inherited placement; autohide always overlays content.</summary>
    public static readonly StyleProperty<ScrollbarPlacement> ScrollbarPlacement = Add("scrollbar-placement", Style.ScrollbarPlacement.Overlay, true, StyleEffects.Layout,
        r => ParseKeyword(r, new Dictionary<string, ScrollbarPlacement> { ["overlay"] = Style.ScrollbarPlacement.Overlay, ["reserve"] = Style.ScrollbarPlacement.Reserve }), syntax: "overlay | reserve");

    /// <summary>Gets the inherited track radius; auto uses scrollbar-radius.</summary>
    public static readonly StyleProperty<SizeValue> ScrollbarTrackRadius = Add("scrollbar-track-radius", SizeValue.Auto, true, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(r, allowPercent: false)), animatable: true, syntax: "auto | <length>");

    /// <summary>Gets the inherited track border width.</summary>
    public static readonly StyleProperty<Length> ScrollbarTrackBorderWidth = Add("scrollbar-track-border-width", Length.Zero, true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited track border color.</summary>
    public static readonly StyleProperty<Color> ScrollbarTrackBorderColor = Add("scrollbar-track-border-color", Style.Color.CurrentColor, true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets whether inherited scrollbar end buttons are shown.</summary>
    public static readonly StyleProperty<bool> ScrollbarButtons = Add("scrollbar-buttons", false, true, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool> { ["none"] = false, ["visible"] = true }), syntax: "none | visible");

    /// <summary>Gets the inherited button length along its scrollbar axis, clamped for short scrollbars.</summary>
    public static readonly StyleProperty<Length> ScrollbarButtonLength = Add("scrollbar-button-length", Length.Px(16), true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited end-button arrow color.</summary>
    public static readonly StyleProperty<Color> ScrollbarButtonColor = Add("scrollbar-button-color", Style.Color.CurrentColor, true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited end-button background color.</summary>
    public static readonly StyleProperty<Color> ScrollbarButtonBackground = Add("scrollbar-button-background", Style.Color.Transparent, true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited background of the hovered end button.</summary>
    public static readonly StyleProperty<Color> ScrollbarButtonHoverBackground = Add("scrollbar-button-hover-background", new Color(128, 128, 128, 64), true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited background of the pressed end button.</summary>
    public static readonly StyleProperty<Color> ScrollbarButtonActiveBackground = Add("scrollbar-button-active-background", new Color(128, 128, 128, 128), true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited arrow color when the button cannot scroll further.</summary>
    public static readonly StyleProperty<Color> ScrollbarButtonDisabledColor = Add("scrollbar-button-disabled-color", new Color(128, 128, 128, 128), true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>Gets the inherited end-button corner radius.</summary>
    public static readonly StyleProperty<Length> ScrollbarButtonRadius = Add("scrollbar-button-radius", Length.Px(2), true, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the inherited distance scrolled by one end-button step.</summary>
    public static readonly StyleProperty<Length> ScrollbarButtonStep = Add("scrollbar-button-step", Length.Px(40), true, StyleEffects.None, r => ParseLength(r, allowPercent: false), syntax: "<length>");

    /// <summary>Gets the inherited delay before a held end button starts repeating.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarButtonRepeatDelay = Add("scrollbar-button-repeat-delay", TimeSpan.FromMilliseconds(400), true, StyleEffects.None, ParseTime, syntax: "<time>");

    /// <summary>Gets the inherited repeat interval of a held button; zero disables repetition.</summary>
    public static readonly StyleProperty<TimeSpan> ScrollbarButtonRepeatInterval = Add("scrollbar-button-repeat-interval", TimeSpan.FromMilliseconds(50), true, StyleEffects.None, ParseTime, syntax: "<time>");

    /// <summary>Gets the descriptor for the position NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<Position> Position = Add("position", Style.Position.Static, false, StyleEffects.Layout, r => ParseKeyword(r, PositionKeywords), syntax: Choice(PositionKeywords));

    /// <summary>Gets the descriptor for the top NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Top = Add("top", SizeValue.Auto, false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>Gets the descriptor for the right NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Right = Add("right", SizeValue.Auto, false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>Gets the descriptor for the bottom NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Bottom = Add("bottom", SizeValue.Auto, false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>Gets the descriptor for the left NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<SizeValue> Left = Add("left", SizeValue.Auto, false, StyleEffects.Layout, ParseMargin, animatable: true, syntax: "auto | <length-percentage>");

    /// <summary>Gets the descriptor for the z-index NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<ZIndex> ZIndex = Add("z-index", Style.ZIndex.Auto, false, StyleEffects.Paint, ParseZIndex, syntax: "auto | <integer>");

    /// <summary>
    /// Gets the descriptor for the background-image NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<ImageValue>> BackgroundImage = Add("background-image", ValueList<ImageValue>.Empty, false, StyleEffects.Paint, ParseImages,
        syntax: "none | [<url> | <gradient>]#");

    /// <summary>Gets the gradient painted across the border ring; none uses the individual border colors.</summary>
    public static readonly StyleProperty<ValueList<ImageValue>> BorderImageSource = Add("border-image-source", ValueList<ImageValue>.Empty, false, StyleEffects.Paint, ParseBorderImage,
        syntax: "none | <gradient>");

    /// <summary>Gets whether the background is clipped inside the border.</summary>
    public static readonly StyleProperty<bool> BackgroundClip = Add("background-clip", false, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["border-box"] = false, ["padding-box"] = true }), syntax: "border-box | padding-box");

    /// <summary>Gets the width of check, chevron and ring vector strokes; percentages use the smaller content dimension and zero uses the shape's default width.</summary>
    public static readonly StyleProperty<Length> StrokeWidth = Add("stroke-width", Length.Zero, false, StyleEffects.Paint, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>Gets the ring vector stroke radius; percentages use the smaller content dimension and zero uses the shape's default radius. Open glyphs ignore this property.</summary>
    public static readonly StyleProperty<Length> StrokeRadius = Add("stroke-radius", Length.Zero, false, StyleEffects.Paint, r => ParseLength(r), animatable: true, syntax: "<length-percentage>");

    /// <summary>Gets the path of a ring glyph; border-box follows the rounded border rectangle and measures progress by path length from top center clockwise. Stroke-radius controls its inset, or zero insets half the stroke width.</summary>
    public static readonly StyleProperty<RingShape> RingShape = Add("ring-shape", Style.RingShape.Circle, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, RingShape>(StringComparer.OrdinalIgnoreCase) { ["circle"] = Style.RingShape.Circle, ["border-box"] = Style.RingShape.BorderBox }), syntax: "circle | border-box");

    /// <summary>Gets the scalar presentation policy; none is immediate, linear modes use a duration per unit, and exponential uses a decay factor per normalized 60Hz frame.</summary>
    public static readonly StyleProperty<NumberMotionKind> NumberMotion = Add("number-motion", NumberMotionKind.None, false, StyleEffects.None,
        r => ParseKeyword(r, new Dictionary<string, NumberMotionKind>(StringComparer.OrdinalIgnoreCase) { ["none"] = NumberMotionKind.None, ["linear"] = NumberMotionKind.Linear, ["linear-increase"] = NumberMotionKind.LinearIncrease, ["exponential"] = NumberMotionKind.Exponential }), syntax: "none | linear | linear-increase | exponential");

    /// <summary>Gets the time per scalar unit for linear number motion; zero is immediate. Total motion time must fit the supported timer range.</summary>
    public static readonly StyleProperty<TimeSpan> NumberMotionDuration = Add("number-motion-duration", TimeSpan.Zero, false, StyleEffects.None,
        ParseTime, syntax: "<time>");

    /// <summary>Gets the fraction of remaining distance removed per normalized 60Hz reference frame; zero or one applies the target immediately.</summary>
    public static readonly StyleProperty<double> NumberMotionDecay = Add("number-motion-decay", 0d, false, StyleEffects.None, r => ParseNumber(r, 0, 1), syntax: "<number>");

    /// <summary>Gets the nonnegative absolute distance at which exponential number motion snaps to its target. Zero uses floating-point convergence.</summary>
    public static readonly StyleProperty<double> NumberMotionTolerance = Add("number-motion-tolerance", 0d, false, StyleEffects.None, r => ParseNumber(r, 0), syntax: "<number>");

    /// <summary>Gets the nonnegative absolute tolerance used for displayed start/end states, capped at half the presenter's range.</summary>
    public static readonly StyleProperty<double> NumberBoundaryTolerance = Add("number-boundary-tolerance", 0d, false, StyleEffects.None, r => ParseNumber(r, 0), syntax: "<number>");

    /// <summary>Gets the start fraction of a check, chevron or ring stroke. Open glyphs trim by cumulative path length; ring fractions are relative to progress.</summary>
    public static readonly StyleProperty<double> StrokeTrimStart = Add("stroke-trim-start", 0d, false, StyleEffects.Paint, r => ParseNumber(r, 0, 1), animatable: true, syntax: "<number>");

    /// <summary>Gets the end fraction of a check, chevron or ring stroke. End values at or below the start draw nothing.</summary>
    public static readonly StyleProperty<double> StrokeTrimEnd = Add("stroke-trim-end", 1d, false, StyleEffects.Paint, r => ParseNumber(r, 0, 1), animatable: true, syntax: "<number>");

    /// <summary>
    /// Gets the descriptor for the background-position NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<PositionValue>> BackgroundPosition = Add("background-position",
        new ValueList<PositionValue>([new PositionValue(Length.Percent(0), Length.Percent(0))]), false, StyleEffects.Paint,
        r => ParseEach(r, part => ParsePosition(part) is var (x, y) ? new PositionValue(x, y) : default),
        syntax: "[[left | center | right | top | bottom | <length-percentage>]{1,2}]#");

    /// <summary>
    /// Gets the descriptor for the background-size NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<BackgroundSize>> BackgroundSize = Add("background-size", new ValueList<BackgroundSize>([Style.BackgroundSize.Auto]), false,
        StyleEffects.Paint, r => ParseEach(r, ParseBackgroundSize), syntax: "[cover | contain | [auto | <length-percentage>]{1,2}]#");

    /// <summary>
    /// Gets the descriptor for the background-repeat NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<BackgroundRepeat>> BackgroundRepeat = Add("background-repeat", new ValueList<BackgroundRepeat>([Style.BackgroundRepeat.Repeat]), false,
        StyleEffects.Paint, r => ParseEach(r, part => ParseKeyword(part, RepeatKeywords)), syntax: "[repeat | no-repeat | repeat-x | repeat-y]#");

    /// <summary>
    /// Gets the descriptor for the background-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> BackgroundColor = Add("background-color", Style.Color.Transparent, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the border-top-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> BorderTopColor = Add("border-top-color", Style.Color.CurrentColor, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the border-right-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> BorderRightColor = Add("border-right-color", Style.Color.CurrentColor, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the border-bottom-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> BorderBottomColor = Add("border-bottom-color", Style.Color.CurrentColor, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the border-left-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> BorderLeftColor = Add("border-left-color", Style.Color.CurrentColor, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the border-top-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BorderStyle> BorderTopStyle = Add("border-top-style", Style.BorderStyle.None, false, StyleEffects.Layout, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the border-right-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BorderStyle> BorderRightStyle = Add("border-right-style", Style.BorderStyle.None, false, StyleEffects.Layout, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the border-bottom-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BorderStyle> BorderBottomStyle = Add("border-bottom-style", Style.BorderStyle.None, false, StyleEffects.Layout, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the border-left-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BorderStyle> BorderLeftStyle = Add("border-left-style", Style.BorderStyle.None, false, StyleEffects.Layout, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the border-top-left-radius NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<RadiusValue> BorderTopLeftRadius = Add("border-top-left-radius", RadiusValue.Zero, false, StyleEffects.Paint, ParseRadius, animatable: true, syntax: "<length-percentage>{1,2}");

    /// <summary>
    /// Gets the descriptor for the border-top-right-radius NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<RadiusValue> BorderTopRightRadius = Add("border-top-right-radius", RadiusValue.Zero, false, StyleEffects.Paint, ParseRadius, animatable: true, syntax: "<length-percentage>{1,2}");

    /// <summary>
    /// Gets the descriptor for the border-bottom-right-radius NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<RadiusValue> BorderBottomRightRadius = Add("border-bottom-right-radius", RadiusValue.Zero, false, StyleEffects.Paint, ParseRadius, animatable: true, syntax: "<length-percentage>{1,2}");

    /// <summary>
    /// Gets the descriptor for the border-bottom-left-radius NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<RadiusValue> BorderBottomLeftRadius = Add("border-bottom-left-radius", RadiusValue.Zero, false, StyleEffects.Paint, ParseRadius, animatable: true, syntax: "<length-percentage>{1,2}");

    /// <summary>
    /// Gets the descriptor for the outline-width NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> OutlineWidth = Add("outline-width", Length.Px(3), false, StyleEffects.Paint, ParseBorderWidth, animatable: true, syntax: LineWidth);

    /// <summary>
    /// Gets the descriptor for the outline-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<BorderStyle> OutlineStyle = Add("outline-style", Style.BorderStyle.None, false, StyleEffects.Paint, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the outline-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> OutlineColor = Add("outline-color", Style.Color.CurrentColor, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the outline-offset NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> OutlineOffset = Add("outline-offset", Length.Zero, false, StyleEffects.Paint, r => ParseLength(r, allowPercent: false, allowNegative: true), animatable: true, syntax: "<length>");

    /// <summary>Gets the width of a second outline ring immediately inside the primary outline.</summary>
    public static readonly StyleProperty<Length> OutlineInnerWidth = Add("outline-inner-width", Length.Zero, false, StyleEffects.Paint, r => ParseLength(r, allowPercent: false), animatable: true, syntax: "<length>");

    /// <summary>Gets the color of the inner outline ring.</summary>
    public static readonly StyleProperty<Color> OutlineInnerColor = Add("outline-inner-color", Style.Color.Transparent, false, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the box-shadow NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<BoxShadow>> BoxShadow = Add("box-shadow", ValueList<BoxShadow>.Empty, false, StyleEffects.Paint, ParseShadows, animatable: true,
        syntax: "none | [inset? && <length>{2,4} && <color>?]#");

    /// <summary>Exterior shadow of a window frame drawn by Nazor.</summary>
    public static readonly StyleProperty<WindowFrameShadows> WindowFrameBoxShadow = Add("-window-frame-box-shadow", default(WindowFrameShadows),
        false, StyleEffects.Paint, ParseFrameShadows,
        syntax: "auto | none | [<length>{2,4} && <color>?]#");

    /// <summary>Width of the visible border of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<Length> WindowFrameBorderWidth = Add("-window-frame-border-width", Length.Zero, false,
        StyleEffects.Paint, ParseBorderWidth, syntax: LineWidth);

    /// <summary>Line style of the visible border of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<BorderStyle> WindowFrameBorderStyle = Add("-window-frame-border-style", Style.BorderStyle.None, false,
        StyleEffects.Paint, r => ParseKeyword(r, BorderStyleKeywords), syntax: Choice(BorderStyleKeywords));

    /// <summary>Color of the visible border of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<Color> WindowFrameBorderColor = Add("-window-frame-border-color", Style.Color.CurrentColor, false,
        StyleEffects.Paint, ParseColor, syntax: "<color>");

    /// <summary>Top-left corner radius of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameBorderTopLeftRadius = AddFrameLength("-window-frame-border-top-left-radius");

    /// <summary>Top-right corner radius of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameBorderTopRightRadius = AddFrameLength("-window-frame-border-top-right-radius");

    /// <summary>Bottom-right corner radius of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameBorderBottomRightRadius = AddFrameLength("-window-frame-border-bottom-right-radius");

    /// <summary>Bottom-left corner radius of a frame drawn by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameBorderBottomLeftRadius = AddFrameLength("-window-frame-border-bottom-left-radius");

    /// <summary>Width of the resize band inside a frame edge controlled by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameResizeInset = AddFrameLength("-window-frame-resize-inset");

    /// <summary>Width of the resize band outside a frame edge controlled by Nazor.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameResizeOutset = AddFrameLength("-window-frame-resize-outset");

    /// <summary>Distance from each corner along the resize bands that uses diagonal resizing.</summary>
    public static readonly StyleProperty<SizeValue> WindowFrameResizeCornerLength = AddFrameLength("-window-frame-resize-corner-length");

    /// <summary>Whether to show the client resize grip when interactive resize is available.</summary>
    public static readonly StyleProperty<bool> WindowResizeGrip = Add("-window-resize-grip", false, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool> { ["auto"] = true, ["none"] = false }), syntax: "auto | none");

    /// <summary>Color of the resize grip drawn by Nazor.</summary>
    public static readonly StyleProperty<Color> WindowResizeGripColor = Add("-window-resize-grip-color", Style.Color.CurrentColor, false,
        StyleEffects.Paint, ParseColor, syntax: "<color>");

    private static StyleProperty<SizeValue> AddFrameLength(string name) => Add(name, SizeValue.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? SizeValue.Auto : ParseFrameLength(r), syntax: "auto | <length>");

    private static SizeValue ParseFrameLength(TokenReader reader) => SizeValue.Fixed(ParseLength(reader, allowPercent: false));

    /// <summary>Gets the descriptor for the opacity NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<double> Opacity = Add("opacity", 1d, false, StyleEffects.Composite, r => Math.Clamp(ParseNumber(r), 0, 1), animatable: true, syntax: "<number>");

    /// <summary>Gets the noninherited ordered backdrop-filter descriptor. It affects earlier content of the current surface inside the rounded border box, not desktop pixels behind a native window. Repeated supported operations retain order; animation is discrete.</summary>
    public static readonly StyleProperty<FilterValue> BackdropFilter = Add("backdrop-filter", FilterValue.None, false, StyleEffects.Paint,
        ParseFilter, syntax: "none | <filter-function-list>");

    /// <summary>Filters the element and its foreground subtree as one group, before element opacity; none initially, noninherited and nonanimatable.</summary>
    public static readonly StyleProperty<FilterValue> Filter = Add("filter", FilterValue.None, false, StyleEffects.Paint,
        ParseFilter, syntax: "none | <filter-function-list>");

    /// <summary>Noninherited element-group color blending; initially normal, paint effects, discrete.</summary>
    public static readonly StyleProperty<BlendMode> MixBlendMode = Add("mix-blend-mode", Style.BlendMode.Normal, false, StyleEffects.Paint,
        r => ParseKeyword(r, BlendKeywords), syntax: Choice(BlendKeywords));

    /// <summary>Explicit noninherited group isolation; initially auto, paint effects, discrete.</summary>
    public static readonly StyleProperty<Isolation> Isolation = Add("isolation", Style.Isolation.Auto, false, StyleEffects.Paint,
        r => ParseKeyword(r, IsolationKeywords), syntax: Choice(IsolationKeywords));

    /// <summary>Noninherited image-layer blend modes, cycled over the background images; initially normal, paint effects, discrete.</summary>
    public static readonly StyleProperty<ValueList<BlendMode>> BackgroundBlendMode = Add("background-blend-mode", new ValueList<BlendMode>([Style.BlendMode.Normal]), false,
        StyleEffects.Paint, r => ParseEach(r, p => ParseKeyword(p, BlendKeywords)), syntax: $"[{Choice(BlendKeywords)}]#");

    /// <summary>Specifies how a replaced image fits its content box.</summary>
    public static readonly StyleProperty<ObjectFit> ObjectFit = Add("object-fit", Style.ObjectFit.Fill, false, StyleEffects.Paint, r => ParseKeyword(r, ObjectFitKeywords),
        syntax: Choice(ObjectFitKeywords));

    /// <summary>
    /// Gets the descriptor for the object-position NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<PositionValue> ObjectPosition = Add("object-position", PositionValue.Center, false, StyleEffects.Paint,
        r => ParsePosition(r) is var (x, y) ? new PositionValue(x, y) : PositionValue.Center, animatable: true,
        syntax: "[left | center | right | top | bottom | <length-percentage>]{1,2}");

    /// <summary>
    /// Gets the descriptor for the transform NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<TransformFunction>> Transform = Add("transform", ValueList<TransformFunction>.Empty, false, StyleEffects.Composite,
        ParseTransform, animatable: true, syntax: "none | <transform-function>+");

    /// <summary>
    /// Gets the descriptor for the transform-origin NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TransformOrigin> TransformOrigin = Add("transform-origin", Style.TransformOrigin.Center, false, StyleEffects.Composite,
        ParseTransformOrigin, animatable: true, syntax: "[left | center | right | top | bottom | <length-percentage>]{1,2}");

    /// <summary>Gets the border-box inset clip with optional elliptical corner radii. Insets and radii animate without changing layout; none disables clipping and interpolates discretely.</summary>
    public static readonly StyleProperty<ClipPathValue> ClipPath = Add("clip-path", ClipPathValue.None, false, StyleEffects.Composite,
        ParseClipPath, animatable: true, syntax: "none | <inset-rect>");

    /// <summary>
    /// Gets the descriptor for the visibility NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Visibility> Visibility = Add("visibility", Style.Visibility.Visible, true, StyleEffects.Paint, r => ParseKeyword(r, VisibilityKeywords), syntax: Choice(VisibilityKeywords));

    /// <summary>
    /// Gets the descriptor for the pointer-events NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<PointerEvents> PointerEvents = Add("pointer-events", Style.PointerEvents.Auto, true, StyleEffects.None,
        r => ParseKeyword(r, PointerEventsKeywords), syntax: Choice(PointerEventsKeywords));

    /// <summary>
    /// Gets the descriptor for the user-select NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<UserSelect> UserSelect = Add("user-select", Style.UserSelect.Auto, true, StyleEffects.None,
        r => ParseKeyword(r, UserSelectKeywords), syntax: Choice(UserSelectKeywords));

    /// <summary>Gets the descriptor for the cursor NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<Cursor> Cursor = Add("cursor", Style.Cursor.Default, true, StyleEffects.None, r => ParseKeyword(r, CursorKeywords), syntax: Choice(CursorKeywords));

    /// <summary>
    /// Gets the descriptor for the -windows-title-bar-height NSS property, including its grammar and initial
    /// value.
    /// </summary>
    public static readonly StyleProperty<WindowsTitleBarHeight> WindowsTitleBarHeight = Add("-windows-title-bar-height", global::Nazor.WindowsTitleBarHeight.Standard, false, StyleEffects.Layout,
        r => ParseKeyword(r, new Dictionary<string, WindowsTitleBarHeight> { ["standard"] = global::Nazor.WindowsTitleBarHeight.Standard, ["tall"] = global::Nazor.WindowsTitleBarHeight.Tall }), syntax: "standard | tall");

    /// <summary>Native Windows title bar background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarBackgroundColor = Add("-windows-title-bar-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarForegroundColor = Add("-windows-title-bar-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar inactive background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarInactiveBackgroundColor = Add("-windows-title-bar-inactive-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar inactive foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarInactiveForegroundColor = Add("-windows-title-bar-inactive-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonBackgroundColor = Add("-windows-title-bar-button-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonForegroundColor = Add("-windows-title-bar-button-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button inactive background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonInactiveBackgroundColor = Add("-windows-title-bar-button-inactive-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button inactive foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonInactiveForegroundColor = Add("-windows-title-bar-button-inactive-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button hover background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonHoverBackgroundColor = Add("-windows-title-bar-button-hover-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button hover foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonHoverForegroundColor = Add("-windows-title-bar-button-hover-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button pressed background color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonPressedBackgroundColor = Add("-windows-title-bar-button-pressed-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Native Windows title bar button pressed foreground color; auto restores the system default.</summary>
    public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonPressedForegroundColor = Add("-windows-title-bar-button-pressed-foreground-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Top border width. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeBorderTopWidth = Add("-windows-badge-border-top-width", Length.Zero, false, StyleEffects.Paint,
        ParseBorderWidth, syntax: "thin | medium | thick | <length>");

    /// <summary>Top border pattern. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderTopStyle = Add("-windows-badge-border-top-style", Style.BorderStyle.Solid, false, StyleEffects.Paint,
        ParseBadgeBorderStyle, syntax: "none | solid | dashed | dotted");

    /// <summary>Top border color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeBorderTopColor = Add("-windows-badge-border-top-color", Style.Color.CurrentColor, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Right border width. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeBorderRightWidth = Add("-windows-badge-border-right-width", Length.Zero, false, StyleEffects.Paint,
        ParseBorderWidth, syntax: "thin | medium | thick | <length>");

    /// <summary>Right border pattern. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderRightStyle = Add("-windows-badge-border-right-style", Style.BorderStyle.Solid, false, StyleEffects.Paint,
        ParseBadgeBorderStyle, syntax: "none | solid | dashed | dotted");

    /// <summary>Right border color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeBorderRightColor = Add("-windows-badge-border-right-color", Style.Color.CurrentColor, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Bottom border width. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeBorderBottomWidth = Add("-windows-badge-border-bottom-width", Length.Zero, false, StyleEffects.Paint,
        ParseBorderWidth, syntax: "thin | medium | thick | <length>");

    /// <summary>Bottom border pattern. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderBottomStyle = Add("-windows-badge-border-bottom-style", Style.BorderStyle.Solid, false, StyleEffects.Paint,
        ParseBadgeBorderStyle, syntax: "none | solid | dashed | dotted");

    /// <summary>Bottom border color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeBorderBottomColor = Add("-windows-badge-border-bottom-color", Style.Color.CurrentColor, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Left border width. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeBorderLeftWidth = Add("-windows-badge-border-left-width", Length.Zero, false, StyleEffects.Paint,
        ParseBorderWidth, syntax: "thin | medium | thick | <length>");

    /// <summary>Left border pattern. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderLeftStyle = Add("-windows-badge-border-left-style", Style.BorderStyle.Solid, false, StyleEffects.Paint,
        ParseBadgeBorderStyle, syntax: "none | solid | dashed | dotted");

    /// <summary>Left border color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeBorderLeftColor = Add("-windows-badge-border-left-color", Style.Color.CurrentColor, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Decorative background layers, first on top. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<ImageValue>> WindowsBadgeBackgroundImage = Add("-windows-badge-background-image", ValueList<ImageValue>.Empty, false, StyleEffects.Paint,
        ParseImages, syntax: "none | [<url> | <gradient>]#");

    /// <summary>Background position relative to free space in each layer. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<PositionValue>> WindowsBadgeBackgroundPosition = Add("-windows-badge-background-position", new ValueList<PositionValue>([new PositionValue(Length.Percent(0), Length.Percent(0))]), false, StyleEffects.Paint,
        r => ParseEach(r, part => ParsePosition(part) is var (x, y) ? new PositionValue(x, y) : default), syntax: "[[left | center | right | top | bottom | <length-percentage>]{1,2}]#");

    /// <summary>Background layer sizing. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<BackgroundSize>> WindowsBadgeBackgroundSize = Add("-windows-badge-background-size", new ValueList<BackgroundSize>([Style.BackgroundSize.Auto]), false, StyleEffects.Paint,
        r => ParseEach(r, ParseBackgroundSize), syntax: "[cover | contain | [auto | <length-percentage>]{1,2}]#");

    /// <summary>Background layer repetition. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<BackgroundRepeat>> WindowsBadgeBackgroundRepeat = Add("-windows-badge-background-repeat", new ValueList<BackgroundRepeat>([Style.BackgroundRepeat.Repeat]), false, StyleEffects.Paint,
        r => ParseEach(r, ParseBadgeRepeat), syntax: "[repeat | no-repeat | repeat-x | repeat-y]#");

    /// <summary>Whether the background is clipped inside the border. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<bool> WindowsBadgeBackgroundClip = Add("-windows-badge-background-clip", false, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool> { ["border-box"] = false, ["padding-box"] = true }), syntax: "border-box | padding-box");

    /// <summary>Static transform of the complete badge, including shadows and mask. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<TransformFunction>> WindowsBadgeTransform = Add("-windows-badge-transform", ValueList<TransformFunction>.Empty, false, StyleEffects.Paint,
        ParseTransform, syntax: "none | <transform-function>+");

    /// <summary>Transform origin within the badge container. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<TransformOrigin> WindowsBadgeTransformOrigin = Add("-windows-badge-transform-origin", Style.TransformOrigin.Center, false, StyleEffects.Paint,
        ParseTransformOrigin, syntax: "[left | center | right | top | bottom | <length-percentage>]{1,2}");

    /// <summary>Optional raster or SVG alpha mask, stretched over the container. Pending or failed masks hide the badge. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<ImageValue>> WindowsBadgeMaskImage = Add("-windows-badge-mask-image", ValueList<ImageValue>.Empty, false, StyleEffects.Paint,
        ParseBadgeMask, syntax: "none | <url>");

    /// <summary>Container background; auto uses red for counts and transparent for icons. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<NativeColor> WindowsBadgeBackgroundColor = Add("-windows-badge-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>Count foreground color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeColor = Add("-windows-badge-color", Style.Color.White, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Container size within the fixed 16 logical pixel overlay. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeSize = Add("-windows-badge-size", Length.Px(16), false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Container dimension; auto uses the badge size. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<SizeValue> WindowsBadgeWidth = Add("-windows-badge-width", SizeValue.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(r)), syntax: "auto | <length-percentage>");

    /// <summary>Container dimension; auto uses the badge size. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<SizeValue> WindowsBadgeHeight = Add("-windows-badge-height", SizeValue.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(r)), syntax: "auto | <length-percentage>");

    /// <summary>Container offset from the center of the overlay. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeOffsetX = Add("-windows-badge-offset-x", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r, allowNegative: true), syntax: "<length-percentage>");

    /// <summary>Container offset from the center of the overlay. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeOffsetY = Add("-windows-badge-offset-y", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r, allowNegative: true), syntax: "<length-percentage>");

    /// <summary>Opacity of the complete badge, including shadows. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<double> WindowsBadgeOpacity = Add("-windows-badge-opacity", 1d, false, StyleEffects.Paint,
        r => Math.Clamp(ParseNumber(r), 0, 1), syntax: "<number>");

    /// <summary>Space between the border and badge content. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgePaddingTop = Add("-windows-badge-padding-top", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Space between the border and badge content. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgePaddingRight = Add("-windows-badge-padding-right", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Space between the border and badge content. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgePaddingBottom = Add("-windows-badge-padding-bottom", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Space between the border and badge content. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgePaddingLeft = Add("-windows-badge-padding-left", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Container corner radius. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderTopLeftRadius = Add("-windows-badge-border-top-left-radius", new RadiusValue(Length.Percent(50), Length.Percent(50)), false, StyleEffects.Paint,
        ParseRadius, syntax: "<length-percentage>{1,2}");

    /// <summary>Container corner radius. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderTopRightRadius = Add("-windows-badge-border-top-right-radius", new RadiusValue(Length.Percent(50), Length.Percent(50)), false, StyleEffects.Paint,
        ParseRadius, syntax: "<length-percentage>{1,2}");

    /// <summary>Container corner radius. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderBottomRightRadius = Add("-windows-badge-border-bottom-right-radius", new RadiusValue(Length.Percent(50), Length.Percent(50)), false, StyleEffects.Paint,
        ParseRadius, syntax: "<length-percentage>{1,2}");

    /// <summary>Container corner radius. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderBottomLeftRadius = Add("-windows-badge-border-bottom-left-radius", new RadiusValue(Length.Percent(50), Length.Percent(50)), false, StyleEffects.Paint,
        ParseRadius, syntax: "<length-percentage>{1,2}");

    /// <summary>Count font families in fallback order. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<string>> WindowsBadgeFontFamily = Add("-windows-badge-font-family", new ValueList<string>(["Segoe UI"]), false, StyleEffects.Paint,
        ParseFontFamily, syntax: "<family-name>#");

    /// <summary>Count font size before optional shrinking. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeFontSize = Add("-windows-badge-font-size", Length.Px(11.2), false, StyleEffects.Paint,
        r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>Count font weight. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<int> WindowsBadgeFontWeight = Add("-windows-badge-font-weight", 700, false, StyleEffects.Paint,
        ParseFontWeight, syntax: "normal | bold | <number>");

    /// <summary>Count font style. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<FontStyle> WindowsBadgeFontStyle = Add("-windows-badge-font-style", Style.FontStyle.Normal, false, StyleEffects.Paint,
        r => ParseKeyword(r, FontStyleKeywords), syntax: "normal | italic");

    /// <summary>Extra spacing between count characters. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeLetterSpacing = Add("-windows-badge-letter-spacing", Length.Zero, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("normal") ? Length.Zero : ParseLength(r, allowPercent: false, allowNegative: true), syntax: "normal | <length>");

    /// <summary>Horizontal count alignment. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<TextAlign> WindowsBadgeTextAlign = Add("-windows-badge-text-align", Style.TextAlign.Center, false, StyleEffects.Paint,
        r => ParseKeyword(r, TextAlignKeywords), syntax: "start | end | left | right | center");

    /// <summary>Vertical count alignment. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<double> WindowsBadgeVerticalAlign = Add("-windows-badge-vertical-align", 0.5d, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, double> { ["top"] = 0, ["middle"] = 0.5, ["bottom"] = 1 }), syntax: "top | middle | bottom");

    /// <summary>Whether the count shrinks to fit the content area. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<bool> WindowsBadgeTextFit = Add("-windows-badge-text-fit", true, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool> { ["shrink"] = true, ["none"] = false }), syntax: "shrink | none");

    /// <summary>Count outline width. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Length> WindowsBadgeTextStrokeWidth = Add("-windows-badge-text-stroke-width", Length.Zero, false, StyleEffects.Paint,
        r => ParseLength(r, allowPercent: false), syntax: "<length>");

    /// <summary>Count outline color. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<Color> WindowsBadgeTextStrokeColor = Add("-windows-badge-text-stroke-color", Style.Color.CurrentColor, false, StyleEffects.Paint,
        ParseColor, syntax: "<color>");

    /// <summary>Background for SetBadgeIcon; auto uses the container background. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<NativeColor> WindowsBadgeImageBackgroundColor = Add("-windows-badge-image-background-color", NativeColor.Auto, false, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? NativeColor.Auto : new NativeColor(ParseColor(r)), syntax: "auto | <color>");

    /// <summary>How the code supplied image fits the content box. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ObjectFit> WindowsBadgeImageFit = Add("-windows-badge-image-fit", Style.ObjectFit.Contain, false, StyleEffects.Paint,
        r => ParseKeyword(r, ObjectFitKeywords), syntax: "fill | contain | cover | none | scale-down");

    /// <summary>Position of the code supplied image in its content box. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<PositionValue> WindowsBadgeImagePosition = Add("-windows-badge-image-position", PositionValue.Center, false, StyleEffects.Paint,
        r => ParsePosition(r) is var (x, y) ? new PositionValue(x, y) : PositionValue.Center,
        syntax: "[left | center | right | top | bottom | <length-percentage>]{1,2}");

    /// <summary>Opacity of the image, excluding its background and border. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<double> WindowsBadgeImageOpacity = Add("-windows-badge-image-opacity", 1d, false, StyleEffects.Paint,
        r => Math.Clamp(ParseNumber(r), 0, 1), syntax: "<number>");

    /// <summary>Whether to clip the image to the rounded container. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<bool> WindowsBadgeImageClip = Add("-windows-badge-image-clip", false, false, StyleEffects.Paint,
        r => ParseKeyword(r, new Dictionary<string, bool> { ["none"] = false, ["border-box"] = true }), syntax: "none | border-box");

    /// <summary>Container shadows. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<BoxShadow>> WindowsBadgeBoxShadow = Add("-windows-badge-box-shadow", ValueList<BoxShadow>.Empty, false, StyleEffects.Paint,
        ParseShadows, syntax: "none | [inset? && <length>{2,4} && <color>?]#");

    /// <summary>Count text shadows. Applies to the root Window on Windows.</summary>
    public static readonly StyleProperty<ValueList<BoxShadow>> WindowsBadgeTextShadow = Add("-windows-badge-text-shadow", ValueList<BoxShadow>.Empty, false, StyleEffects.Paint,
        r => ParseShadows(r, text: true), syntax: "none | [<length>{2,3} && <color>?]#");

    /// <summary>Native declarations apply only to their owning element, including selectors by class.</summary>
    /// <param name="property">The style property to read or update.</param>
    /// <param name="elementName">The element name exposed to NSS type selectors.</param>
    /// <param name="isRoot">The is root (bool).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool AppliesTo(StyleProperty property, string elementName, bool isRoot) => property.Name switch
    {
        var name when (name.StartsWith("-windows-title-bar-", StringComparison.Ordinal) || name.StartsWith("-windows-badge-", StringComparison.Ordinal)) => elementName == "Window" && isRoot,
        "-window-resize-grip" or "-window-resize-grip-color" => elementName == "Window" && isRoot,
        var frame when frame.StartsWith("-window-frame-", StringComparison.Ordinal) => elementName == "Window" && isRoot,
        _ => true,
    };

    /// <summary>Gets the descriptor for the color NSS property, including its grammar and initial value.</summary>
    public static readonly StyleProperty<Color> Color = Add("color", Style.Color.Black, true, StyleEffects.Paint, ParseColor, animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the font-family NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<string>> FontFamily = Add("font-family", new ValueList<string>(["Segoe UI"]), true, StyleEffects.Layout, ParseFontFamily, syntax: "<family-name>#");

    /// <summary>Ordered exact-family and glyph pairs for a decorative text element; none uses its ordinary text.</summary>
    public static readonly StyleProperty<ValueList<FontSymbol>> FontSymbol = Add("font-symbol", ValueList<FontSymbol>.Empty, false, StyleEffects.Layout,
        ParseFontSymbol, syntax: "none | [<string> <string>]#");

    /// <summary>Gets the text fragments generated by before/after pseudo-elements. An empty list suppresses the box; a list containing an empty string creates a decorative box.</summary>
    public static readonly StyleProperty<ValueList<string>> Content = Add("content", new ValueList<string>([]), false, StyleEffects.Layout, ParseContent, syntax: "normal | none | <string>+");

    /// <summary>
    /// Gets the descriptor for the font-size NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> FontSize = Add("font-size", Length.Px(14), true, StyleEffects.Layout, r => ParseLength(r), syntax: "<length-percentage>");

    /// <summary>
    /// Gets the descriptor for the font-weight NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<int> FontWeight = Add("font-weight", 400, true, StyleEffects.Layout, ParseFontWeight, syntax: "normal | bold | <number>");

    /// <summary>
    /// Gets the descriptor for the font-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<FontStyle> FontStyle = Add("font-style", Style.FontStyle.Normal, true, StyleEffects.Layout, r => ParseKeyword(r, FontStyleKeywords), syntax: Choice(FontStyleKeywords));

    /// <summary>Controls automatic optical sizing: CSS logical pixels, the font default, or native typographic points.</summary>
    public static readonly StyleProperty<FontOpticalSizing> FontOpticalSizing = Add("font-optical-sizing", Style.FontOpticalSizing.Auto, true, StyleEffects.Layout,
        r => ParseKeyword(r, FontOpticalSizingKeywords), syntax: Choice(FontOpticalSizingKeywords));

    /// <summary>Explicit case-sensitive variable-font axis coordinates, overriding automatic optical sizing; normal uses no overrides.</summary>
    public static readonly StyleProperty<ValueList<FontVariationSetting>> FontVariationSettings = Add("font-variation-settings", ValueList<FontVariationSetting>.Empty, true, StyleEffects.Layout,
        FontVariationSetting.Parse, syntax: "normal | [<string> <number>]#");

    /// <summary>Inherited case-sensitive OpenType feature selections; initially normal, layout effects, discrete.</summary>
    public static readonly StyleProperty<ValueList<FontFeatureSetting>> FontFeatureSettings = Add("font-feature-settings", ValueList<FontFeatureSetting>.Empty, true, StyleEffects.Layout,
        FontFeatureSetting.Parse, syntax: "normal | [<string> [on | off | <integer>]?]#");

    /// <summary>Inherited opt-in synthesis of missing bold/italic faces; initially none, layout effects, discrete.</summary>
    public static readonly StyleProperty<FontSynthesis> FontSynthesis = Add("font-synthesis", Style.FontSynthesis.None, true, StyleEffects.Layout,
        ParseFontSynthesis, syntax: "none | [weight || style]");

    /// <summary>Specifies the text line-height metric.</summary>
    public static readonly StyleProperty<LineHeight> LineHeight = Add("line-height", Style.LineHeight.Normal, true, StyleEffects.Layout, ParseLineHeight, syntax: "normal | <number> | <length-percentage>");

    /// <summary>
    /// Gets the descriptor for the text-align NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TextAlign> TextAlign = Add("text-align", Style.TextAlign.Start, true, StyleEffects.Layout, r => ParseKeyword(r, TextAlignKeywords), syntax: Choice(TextAlignKeywords));

    /// <summary>
    /// Gets the descriptor for the overflow-wrap NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<OverflowWrap> OverflowWrap = Add("overflow-wrap", Style.OverflowWrap.Normal, true, StyleEffects.Layout, r => ParseKeyword(r, OverflowWrapKeywords), syntax: Choice(OverflowWrapKeywords));

    /// <summary>
    /// Gets the descriptor for the text-transform NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TextTransform> TextTransform = Add("text-transform", Style.TextTransform.None, true, StyleEffects.Layout, r => ParseKeyword(r, TextTransformKeywords), syntax: Choice(TextTransformKeywords));

    /// <summary>
    /// Gets the descriptor for the letter-spacing NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Length> LetterSpacing = Add("letter-spacing", Length.Zero, true, StyleEffects.Layout,
        r => r.TryConsumeIdent("normal") ? Length.Zero : ParseLength(r, allowPercent: false, allowNegative: true), syntax: "normal | <length>");

    /// <summary>
    /// Gets the descriptor for the text-decoration-line NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TextDecorationLine> TextDecorationLine = Add("text-decoration-line", Style.TextDecorationLine.None, true, StyleEffects.Paint,
        ParseDecorationLine, syntax: "none | [underline || overline || line-through]");

    /// <summary>
    /// Gets the descriptor for the text-decoration-style NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TextDecorationStyle> TextDecorationStyle = Add("text-decoration-style", Style.TextDecorationStyle.Solid, true, StyleEffects.Paint,
        r => ParseKeyword(r, DecorationStyleKeywords), syntax: Choice(DecorationStyleKeywords));

    /// <summary>
    /// Gets the descriptor for the text-decoration-color NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<Color> TextDecorationColor = Add("text-decoration-color", Style.Color.CurrentColor, true, StyleEffects.Paint, ParseColor,
        animatable: true, syntax: "<color>");

    /// <summary>
    /// Gets the descriptor for the text-decoration-thickness NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<SizeValue> TextDecorationThickness = Add("text-decoration-thickness", SizeValue.Auto, true, StyleEffects.Paint,
        r => r.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(r, allowPercent: false)), syntax: "auto | <length>");

    /// <summary>
    /// Gets the descriptor for the text-overflow NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<TextOverflow> TextOverflow = Add("text-overflow", Style.TextOverflow.Clip, false, StyleEffects.Paint,
        r => ParseKeyword(r, TextOverflowKeywords), syntax: Choice(TextOverflowKeywords));

    /// <summary>
    /// Gets the descriptor for the white-space NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<WhiteSpace> WhiteSpace = Add("white-space", Style.WhiteSpace.Normal, true, StyleEffects.Layout, r => ParseKeyword(r, WhiteSpaceKeywords), syntax: Choice(WhiteSpaceKeywords));

    /// <summary>
    /// Gets the descriptor for the transition NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<TransitionDefinition>> Transition = Add("transition", ValueList<TransitionDefinition>.Empty, false, StyleEffects.None, ParseTransitions, syntax: "none | [[all | <property>] || <time> || <easing-function> || <time>]#");

    /// <summary>
    /// Gets the descriptor for the animation NSS property, including its grammar and initial value.
    /// </summary>
    public static readonly StyleProperty<ValueList<AnimationDefinition>> Animation = Add("animation", ValueList<AnimationDefinition>.Empty, false, StyleEffects.None, ParseAnimations,
        syntax: "none | [<time> || <easing-function> || <time> || [infinite | <number>] || [normal | reverse | alternate | alternate-reverse] || " +
            "[none | forwards | backwards | both] || [running | paused] || <keyframes-name>]#");

    /// <summary>Gets the theme-controlled retention time for an overlay's exit animation; zero removes it immediately.</summary>
    public static readonly StyleProperty<TimeSpan> OverlayExitDuration = Add("overlay-exit-duration", TimeSpan.Zero, false, StyleEffects.None,
        reader =>
        {
            var token = reader.Peek();
            var milliseconds = token.Number * (token.Value == "s" ? 1000 : 1);
            if (!double.IsFinite(milliseconds) || milliseconds > uint.MaxValue - 1)
                throw TokenReader.Error("Overlay exit duration exceeds the supported timer interval.", token);
            var duration = ParseTime(reader);
            return duration;
        }, syntax: "<time>");

    /// <summary>Gets the theme-controlled pointer-hover delay before opening a submenu. Keyboard and explicit activation remain immediate.</summary>
    public static readonly StyleProperty<TimeSpan> SubmenuOpenDelay = Add("submenu-open-delay", TimeSpan.FromMilliseconds(250), false, StyleEffects.None,
        reader =>
        {
            var token = reader.Peek();
            var milliseconds = token.Number * (token.Value == "s" ? 1000 : 1);
            if (!double.IsFinite(milliseconds) || milliseconds > uint.MaxValue - 1)
                throw TokenReader.Error("Submenu open delay exceeds the supported timer interval.", token);
            return ParseTime(reader);
        }, syntax: "<time>");

    static StyleProperties() => RegisterShorthands();

    private static void RegisterShorthands()
    {
        StyleProperty[] frameRadius = [WindowFrameBorderTopLeftRadius, WindowFrameBorderTopRightRadius,
            WindowFrameBorderBottomRightRadius, WindowFrameBorderBottomLeftRadius];
        AddShorthand("-window-frame-border-radius", frameRadius, "auto | <length>{1,4}", r => r.TryConsumeIdent("auto")
            ? frameRadius.Select(property => (property, (object)SizeValue.Auto)).ToArray()
            : Box(r, frameRadius, reader => ParseFrameLength(reader)));
        AddShorthand("-window-frame-border", [WindowFrameBorderWidth, WindowFrameBorderStyle, WindowFrameBorderColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(WindowFrameBorderWidth, WindowFrameBorderStyle, WindowFrameBorderColor)]));
        StyleProperty[] badgePadding = [WindowsBadgePaddingTop, WindowsBadgePaddingRight, WindowsBadgePaddingBottom, WindowsBadgePaddingLeft];
        StyleProperty[] badgeRadius = [WindowsBadgeBorderTopLeftRadius, WindowsBadgeBorderTopRightRadius, WindowsBadgeBorderBottomRightRadius, WindowsBadgeBorderBottomLeftRadius];
        AddShorthand("-windows-badge-padding", badgePadding, "<length-percentage>{1,4}", r => Box(r, badgePadding, reader => ParseLength(reader)));
        AddShorthand("-windows-badge-border-radius", badgeRadius, "<length-percentage>{1,4} [/ <length-percentage>{1,4}]?",
            r => ParseBorderRadius(r).Select((pair, index) => (badgeRadius[index], pair.Item2)).ToArray());
        RegisterBadgeShorthands();
        StyleProperty[] padding = [PaddingTop, PaddingRight, PaddingBottom, PaddingLeft];
        StyleProperty[] margin = [MarginTop, MarginRight, MarginBottom, MarginLeft];
        StyleProperty[] borderWidth = [BorderTopWidth, BorderRightWidth, BorderBottomWidth, BorderLeftWidth];
        StyleProperty[] inset = [Top, Right, Bottom, Left];
        AddShorthand("padding", padding, "<length-percentage>{1,4}", r => Box(r, padding, reader => ParseLength(reader)));
        AddShorthand("margin", margin, "[auto | <length-percentage>]{1,4}", r => Box(r, margin, reader => ParseMargin(reader)));
        AddShorthand("border-width", borderWidth, $"[{LineWidth}]{{1,4}}", r => Box(r, borderWidth, reader => ParseBorderWidth(reader)));
        StyleProperty[] borderStyle = [BorderTopStyle, BorderRightStyle, BorderBottomStyle, BorderLeftStyle];
        StyleProperty[] borderColor = [BorderTopColor, BorderRightColor, BorderBottomColor, BorderLeftColor];
        StyleProperty[] radius = [BorderTopLeftRadius, BorderTopRightRadius, BorderBottomRightRadius, BorderBottomLeftRadius];
        AddShorthand("border-style", borderStyle, $"[{Choice(BorderStyleKeywords)}]{{1,4}}", r => Box(r, borderStyle, reader => ParseKeyword(reader, BorderStyleKeywords)));
        AddShorthand("border-color", borderColor, "<color>{1,4}", r => Box(r, borderColor, reader => ParseColor(reader)));
        AddShorthand("border-radius", radius, "<length-percentage>{1,4} [/ <length-percentage>{1,4}]?", ParseBorderRadius);
        AddShorthand("border-top", [BorderTopWidth, BorderTopStyle, BorderTopColor], BorderSideSyntax, r => ParseBorderSides(r, [(BorderTopWidth, BorderTopStyle, BorderTopColor)]));
        AddShorthand("border-right", [BorderRightWidth, BorderRightStyle, BorderRightColor], BorderSideSyntax, r => ParseBorderSides(r, [(BorderRightWidth, BorderRightStyle, BorderRightColor)]));
        AddShorthand("border-bottom", [BorderBottomWidth, BorderBottomStyle, BorderBottomColor], BorderSideSyntax, r => ParseBorderSides(r, [(BorderBottomWidth, BorderBottomStyle, BorderBottomColor)]));
        AddShorthand("border-left", [BorderLeftWidth, BorderLeftStyle, BorderLeftColor], BorderSideSyntax, r => ParseBorderSides(r, [(BorderLeftWidth, BorderLeftStyle, BorderLeftColor)]));
        AddShorthand("outline", [OutlineWidth, OutlineStyle, OutlineColor], BorderSideSyntax, r => ParseBorderSides(r, [(OutlineWidth, OutlineStyle, OutlineColor)]));
        AddShorthand("inset", inset, "[auto | <length-percentage>]{1,4}", r => Box(r, inset, reader => ParseMargin(reader)));
        AddShorthand("gap", [RowGap, ColumnGap], "[normal | <length-percentage>]{1,2}", r =>
        {
            var row = ParseGap(r);
            var column = r.AtEnd ? row : ParseGap(r);
            return [(RowGap, row), (ColumnGap, column)];
        });
        AddShorthand("overflow", [OverflowX, OverflowY], $"[{Choice(OverflowKeywords)}]{{1,2}}", r =>
        {
            var x = ParseKeyword(r, OverflowKeywords);
            var y = r.AtEnd ? x : ParseKeyword(r, OverflowKeywords);
            return [(OverflowX, x), (OverflowY, y)];
        });
        AddShorthand("background", [BackgroundColor, BackgroundImage, BackgroundPosition, BackgroundSize, BackgroundRepeat],
            "[<bg-layer> ,]* [[<image> | none] || <position> [/ <bg-size>]? || [repeat | no-repeat | repeat-x | repeat-y] || <color>]", ParseBackground);
        AddShorthand("flex", [FlexGrow, FlexShrink, FlexBasis], "none | auto | <number> <number>? [auto | content | <length-percentage>]?", ParseFlex);
        AddShorthand("grid-row", [GridRowStart, GridRowEnd], "<grid-line> [/ <grid-line>]?", r => ParseGridSpan(r, GridRowStart, GridRowEnd));
        AddShorthand("grid-column", [GridColumnStart, GridColumnEnd], "<grid-line> [/ <grid-line>]?", r => ParseGridSpan(r, GridColumnStart, GridColumnEnd));
        AddShorthand("grid-area", [GridRowStart, GridColumnStart, GridRowEnd, GridColumnEnd], "<grid-line> [/ <grid-line>]{0,3}", ParseGridArea);
        AddShorthand("place-items", [AlignItems, JustifyItems], $"[{Choice(AlignItemsKeywords)}]{{1,2}}",
            r => ParsePlace(r, AlignItems, AlignItemsKeywords, JustifyItems, AlignItemsKeywords));
        AddShorthand("place-self", [AlignSelf, JustifySelf], $"[{Choice(AlignSelfKeywords)}]{{1,2}}",
            r => ParsePlace(r, AlignSelf, AlignSelfKeywords, JustifySelf, AlignSelfKeywords));
        AddShorthand("place-content", [AlignContent, JustifyContent], $"[{Choice(AlignContentKeywords)}] [{Choice(JustifyKeywords)}]?",
            r => ParsePlace(r, AlignContent, AlignContentKeywords, JustifyContent, JustifyKeywords));
        AddShorthand("text-decoration", [TextDecorationLine, TextDecorationStyle, TextDecorationColor, TextDecorationThickness],
            $"[none | [underline || overline || line-through]] || [{Choice(DecorationStyleKeywords)}] || <color> || [auto | <length>]", ParseTextDecoration);
        AddShorthand("flex-flow", [FlexDirection, FlexWrap], $"[{Choice(DirectionKeywords)}] || [{Choice(WrapKeywords)}]", ParseFlexFlow);
        AddShorthand("border", [BorderTopWidth, BorderRightWidth, BorderBottomWidth, BorderLeftWidth, .. borderStyle, .. borderColor], BorderSideSyntax,
            r => ParseBorderSides(r, [(BorderTopWidth, BorderTopStyle, BorderTopColor), (BorderRightWidth, BorderRightStyle, BorderRightColor),
                (BorderBottomWidth, BorderBottomStyle, BorderBottomColor), (BorderLeftWidth, BorderLeftStyle, BorderLeftColor)]));
    }

    /// <summary>Gets all registered longhand style properties in identifier order.</summary>
    public static IReadOnlyList<StyleProperty> All => AllProperties;

    /// <summary>The shorthands, expanded at compile time into <see cref="All" />.</summary>
    public static IReadOnlyCollection<ShorthandProperty> AllShorthands => Shorthands.Values;

    /// <summary>The keywords every property accepts.</summary>
    public static IReadOnlyList<string> GlobalKeywords { get; } = ["inherit", "initial", "unset"];

    /// <summary>
    /// The color keywords of <c>&lt;color&gt;</c>: the CSS named colors, <c>transparent</c> and <c>currentcolor</c>.
    /// </summary>
    public static IReadOnlyList<string> ColorNames { get; } = [.. NamedColors.Names.Append("currentcolor").Order(StringComparer.Ordinal)];

    /// <summary>Looks up a registered longhand property by its NSS name.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="property">When this method returns, contains the property produced by the operation.</param>
    /// <returns>true if a longhand property was found; otherwise, false.</returns>
    public static bool TryGet(string name, out StyleProperty property) => ByName.TryGetValue(name, out property!);

    /// <summary>Looks up a registered shorthand property by its NSS name.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="shorthand">When this method returns, contains the shorthand produced by the operation.</param>
    /// <returns>true if a shorthand property was found; otherwise, false.</returns>
    public static bool TryGetShorthand(string name, out ShorthandProperty shorthand) => Shorthands.TryGetValue(name, out shorthand!);

    /// <summary>Determines whether the name begins with the custom-property prefix --.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>true if the name starts with --; otherwise, false.</returns>
    public static bool IsCustomProperty(string name) => name.StartsWith("--", StringComparison.Ordinal) && name.Length > 2;

    private static StyleProperty<T> Add<T>(string name, T initial, bool inherited, StyleEffects effects, Func<TokenReader, T> parser, bool animatable = false, string syntax = "") where T : notnull
    {
        var property = new StyleProperty<T>(AllProperties.Count, name, initial, inherited, effects, animatable, syntax, parser);
        AllProperties.Add(property);
        ByName.Add(name, property);
        return property;
    }

    private static void AddShorthand(string name, StyleProperty[] longhands, string syntax, Func<TokenReader, IReadOnlyList<(StyleProperty, object)>> expand) =>
        Shorthands.Add(name, new ShorthandProperty(name, longhands, syntax, expand));

    private static IReadOnlyList<(StyleProperty, object)> Box(TokenReader reader, StyleProperty[] longhands, Func<TokenReader, object> parse)
    {
        var values = new List<object>();
        while (!reader.AtEnd && values.Count < 4) values.Add(parse(reader));
        if (values.Count == 0) throw TokenReader.Error("Expected a value.", reader.Peek());
        var top = values[0];
        var right = values.Count > 1 ? values[1] : top;
        var bottom = values.Count > 2 ? values[2] : top;
        var left = values.Count > 3 ? values[3] : right;
        return [(longhands[0], top), (longhands[1], right), (longhands[2], bottom), (longhands[3], left)];
    }

    private static SizeValue ParseSizeOrAuto(TokenReader reader) =>
        reader.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(reader));

    private static SizeValue ParseSizeOrNone(TokenReader reader) =>
        reader.TryConsumeIdent("none") ? SizeValue.None : SizeValue.Fixed(ParseLength(reader));

    private static SizeValue ParseMargin(TokenReader reader) =>
        reader.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(reader, allowNegative: true));

    private static SizeValue ParseFlexBasis(TokenReader reader)
    {
        if (reader.TryConsumeIdent("content")) return SizeValue.Content;
        return ParseSize(reader, "auto");
    }

    private static SizeValue ParseSize(TokenReader reader, string keyword)
    {
        if (reader.TryConsumeIdent(keyword)) return keyword == "none" ? SizeValue.None : SizeValue.Auto;
        if (reader.TryConsumeIdent("min-content")) return SizeValue.MinContent;
        if (reader.TryConsumeIdent("max-content")) return SizeValue.MaxContent;
        if (reader.TryConsumeIdent("fit-content")) return SizeValue.FitContent;
        if (reader.Peek().Is(StyleTokenKind.Function, "fit-content"))
            throw TokenReader.Error("fit-content() is only for grid tracks: use fit-content, or max-width with a length.", reader.Peek());
        return SizeValue.Fixed(ParseLength(reader));
    }

    private static AspectRatio ParseAspectRatio(TokenReader reader)
    {
        var auto = reader.TryConsumeIdent("auto");
        if (reader.AtEnd)
        {
            if (auto) return Style.AspectRatio.Auto;
            throw TokenReader.Error("Expected auto or a ratio such as 16 / 9.", reader.Peek());
        }
        var first = reader.Peek();
        var width = ParseNumber(reader, 0);
        var height = 1d;
        if (reader.Peek().IsDelim('/'))
        {
            reader.Next();
            height = ParseNumber(reader, 0);
        }
        if (!auto) reader.TryConsumeIdent("auto");
        if (width <= 0 || height <= 0) throw TokenReader.Error("Both sides of a ratio must be positive.", first);
        return new AspectRatio(width / height);
    }

    private static ZIndex ParseZIndex(TokenReader reader) =>
        reader.TryConsumeIdent("auto") ? Style.ZIndex.Auto : Style.ZIndex.Of(ParseInteger(reader));

    private static Length ParseGap(TokenReader reader) => reader.TryConsumeIdent("normal") ? Length.Zero : ParseLength(reader);

    private static Length ParseBorderWidth(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Ident)
        {
            reader.Next();
            return token.Value.ToLowerInvariant() switch
            {
                "thin" => Length.Px(1),
                "medium" => Length.Px(3),
                "thick" => Length.Px(5),
                _ => throw TokenReader.Error("Expected a border width.", token),
            };
        }
        return ParseLength(reader, allowPercent: false);
    }

    private static int ParseFontWeight(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Ident)
        {
            reader.Next();
            return token.Value.ToLowerInvariant() switch
            {
                "normal" => 400,
                "bold" => 700,
                _ => throw TokenReader.Error("Expected normal, bold or a weight between 1 and 1000.", token),
            };
        }
        return (int)ParseNumber(reader, 1, 1000);
    }

    private static LineHeight ParseLineHeight(TokenReader reader)
    {
        if (reader.TryConsumeIdent("normal")) return Style.LineHeight.Normal;
        if (reader.Peek().Kind == StyleTokenKind.Number && reader.Peek().Number != 0) return Style.LineHeight.Multiplier(ParseNumber(reader, 0));
        return Style.LineHeight.Fixed(ParseLength(reader));
    }

    private static ValueList<string> ParseContent(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none") || reader.TryConsumeIdent("normal")) return new ValueList<string>([]);
        var fragments = new List<string>();
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            if (token.Kind != StyleTokenKind.String) throw TokenReader.Error("Expected a quoted content string, normal or none.", token);
            fragments.Add(token.Value);
        }
        if (fragments.Count == 0) throw TokenReader.Error("Expected a content value.", reader.Peek());
        return new ValueList<string>(fragments);
    }

    private static ValueList<string> ParseFontFamily(TokenReader reader)
    {
        var families = new List<string>();
        foreach (var part in reader.SplitByComma())
        {
            var first = part.Peek();
            if (first.Kind == StyleTokenKind.String)
            {
                part.Next();
                part.ExpectEnd();
                families.Add(first.Value);
                continue;
            }
            var names = new List<string>();
            while (!part.AtEnd)
            {
                var token = part.Next();
                if (token.Kind != StyleTokenKind.Ident) throw TokenReader.Error("Expected a font family name.", token);
                names.Add(token.Value);
            }
            if (names.Count == 0) throw TokenReader.Error("Expected a font family name.", first);
            families.Add(string.Join(' ', names));
        }
        return new ValueList<string>(families);
    }

    private static ValueList<FontSymbol> ParseFontSymbol(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return ValueList<FontSymbol>.Empty;
        var symbols = new List<FontSymbol>();
        foreach (var part in reader.SplitByComma())
        {
            var family = part.Next();
            var glyph = part.Next();
            part.ExpectEnd();
            if (family.Kind != StyleTokenKind.String || string.IsNullOrWhiteSpace(family.Value) || family.Value.Any(char.IsControl))
                throw TokenReader.Error("Expected a nonblank quoted font family without control characters.", family);
            if (glyph.Kind != StyleTokenKind.String || string.IsNullOrWhiteSpace(glyph.Value) || glyph.Value.Any(char.IsControl))
                throw TokenReader.Error("Expected nonblank quoted glyph text without control characters.", glyph);
            if (symbols.Count == FontSymbols.MaximumCandidates)
                throw TokenReader.Error("At most 32 font-symbol alternatives are supported.", family);
            symbols.Add(new(family.Value, glyph.Value));
        }
        if (symbols.Count == 0) throw TokenReader.Error("Expected a font-symbol pair or none.", reader.Peek());
        return new(symbols);
    }

    private static ValueList<TransitionDefinition> ParseTransitions(TokenReader reader)
    {
        if (reader.Peek().Is(StyleTokenKind.Ident, "none"))
        {
            reader.Next();
            return ValueList<TransitionDefinition>.Empty;
        }
        var transitions = new List<TransitionDefinition>();
        foreach (var part in reader.SplitByComma())
        {
            string? property = null;
            TimeSpan? duration = null, delay = null;
            EasingFunction? easing = null;
            while (!part.AtEnd)
            {
                var token = part.Peek();
                if (token.Kind is StyleTokenKind.Dimension or StyleTokenKind.Number)
                {
                    if (duration is null) duration = ParseTime(part);
                    else if (delay is null) delay = ParseTime(part);
                    else throw TokenReader.Error("A transition has at most a duration and a delay.", token);
                }
                else if (token.Kind == StyleTokenKind.Function || (token.Kind == StyleTokenKind.Ident && IsEasingKeyword(token.Value)))
                {
                    if (easing is not null) throw TokenReader.Error("Duplicate timing function.", token);
                    easing = ParseEasing(part);
                }
                else if (token.Kind == StyleTokenKind.Ident && property is null)
                {
                    part.Next();
                    var name = token.Value.ToLowerInvariant();
                    if (name != "all" && !ByName.ContainsKey(name) && !Shorthands.ContainsKey(name))
                        throw TokenReader.Error($"Unknown property '{token.Value}' in transition.", token);
                    property = name == "background" ? BackgroundColor.Name : name;
                }
                else throw TokenReader.Error($"Unexpected '{token}' in transition.", token);
            }
            transitions.Add(new TransitionDefinition(property ?? "all", duration ?? TimeSpan.Zero, easing ?? EasingFunction.Ease, delay ?? TimeSpan.Zero));
        }
        return new ValueList<TransitionDefinition>(transitions);
    }

    private static ValueList<AnimationDefinition> ParseAnimations(TokenReader reader)
    {
        if (reader.Tokens.Where(t => t.Kind != StyleTokenKind.Whitespace).ToArray() is [{ Kind: StyleTokenKind.Ident } only] &&
            only.Value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            reader.Next();
            return ValueList<AnimationDefinition>.Empty;
        }
        var animations = new List<AnimationDefinition>();
        foreach (var part in reader.SplitByComma())
        {
            if (part.AtEnd) throw TokenReader.Error("Expected an animation.", part.Peek());
            string? name = null;
            var named = false;
            TimeSpan? duration = null, delay = null;
            EasingFunction? easing = null;
            double? iterations = null;
            AnimationDirection? direction = null;
            AnimationFillMode? fill = null;
            bool? paused = null;
            while (!part.AtEnd)
            {
                var token = part.Peek();
                var word = token.Kind == StyleTokenKind.Ident ? token.Value.ToLowerInvariant() : "";
                if (token.Kind == StyleTokenKind.Dimension)
                {
                    if (duration is null) duration = ParseTime(part);
                    else if (delay is null) delay = ParseSignedTime(part);
                    else throw TokenReader.Error("An animation has at most a duration and a delay.", token);
                    continue;
                }
                if (token.Kind == StyleTokenKind.Number)
                {
                    if (iterations is not null) throw TokenReader.Error("Duplicate iteration count.", token);
                    iterations = ParseNumber(part, 0);
                    continue;
                }
                if (easing is null && (token.Kind == StyleTokenKind.Function || IsEasingKeyword(word)))
                {
                    easing = ParseEasing(part);
                    continue;
                }
                part.Next();
                if (iterations is null && word == "infinite") iterations = double.PositiveInfinity;
                else if (direction is null && AnimationDirections.TryGetValue(word, out var value)) direction = value;
                else if (fill is null && AnimationFillModes.TryGetValue(word, out var mode)) fill = mode;
                else if (paused is null && word is "running" or "paused") paused = word == "paused";
                else if (!named && token.Kind is StyleTokenKind.Ident or StyleTokenKind.String)
                {
                    named = true;
                    name = word == "none" ? null : token.Value;
                }
                else throw TokenReader.Error($"Unexpected '{token}' in animation.", token);
            }
            animations.Add(new AnimationDefinition(name, duration ?? TimeSpan.Zero, easing ?? EasingFunction.Ease, delay ?? TimeSpan.Zero,
                iterations ?? 1, direction ?? AnimationDirection.Normal, fill ?? AnimationFillMode.None, paused ?? false));
        }
        return new ValueList<AnimationDefinition>(animations);
    }

    private static readonly Dictionary<string, AnimationDirection> AnimationDirections = new()
    {
        ["normal"] = AnimationDirection.Normal, ["reverse"] = AnimationDirection.Reverse,
        ["alternate"] = AnimationDirection.Alternate, ["alternate-reverse"] = AnimationDirection.AlternateReverse,
    };

    private static readonly Dictionary<string, AnimationFillMode> AnimationFillModes = new()
    {
        ["none"] = AnimationFillMode.None, ["forwards"] = AnimationFillMode.Forwards,
        ["backwards"] = AnimationFillMode.Backwards, ["both"] = AnimationFillMode.Both,
    };

    private static TimeSpan ParseSignedTime(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Dimension && token.Number < 0 && token.Value.ToLowerInvariant() is "ms" or "s")
        {
            reader.Next();
            return token.Value.Equals("ms", StringComparison.OrdinalIgnoreCase) ? TimeSpan.FromMilliseconds(token.Number) : TimeSpan.FromSeconds(token.Number);
        }
        return ParseTime(reader);
    }

    private static bool IsEasingKeyword(string value) => value.ToLowerInvariant() is "linear" or "ease" or "ease-in" or "ease-out" or "ease-in-out";

    private static IReadOnlyList<(StyleProperty, object)> ParseFlex(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return [(FlexGrow, 0d), (FlexShrink, 0d), (FlexBasis, SizeValue.Auto)];
        if (reader.TryConsumeIdent("auto")) return [(FlexGrow, 1d), (FlexShrink, 1d), (FlexBasis, SizeValue.Auto)];
        double grow = 1, shrink = 1;
        var basis = SizeValue.Fixed(Length.Percent(0));
        var numbers = 0;
        var sawBasis = false;
        while (!reader.AtEnd)
        {
            var token = reader.Peek();

            if (token.Kind == StyleTokenKind.Number && numbers < 2)
            {
                var value = ParseNumber(reader, 0);
                if (numbers++ == 0) grow = value;
                else shrink = value;
            }
            else if (!sawBasis)
            {
                basis = ParseFlexBasis(reader);
                sawBasis = true;
            }
            else throw TokenReader.Error($"Unexpected '{token}' in flex.", token);
        }
        if (numbers == 0 && !sawBasis) throw TokenReader.Error("Expected a flex value.", reader.Peek());
        return [(FlexGrow, grow), (FlexShrink, shrink), (FlexBasis, basis)];
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseFlexFlow(TokenReader reader)
    {
        FlexDirection? direction = null;
        FlexWrap? wrap = null;
        while (!reader.AtEnd)
        {
            var token = reader.Peek();
            var name = token.Kind == StyleTokenKind.Ident ? token.Value.ToLowerInvariant() : "";
            if (DirectionKeywords.ContainsKey(name) && direction is null) direction = ParseKeyword(reader, DirectionKeywords);
            else if (WrapKeywords.ContainsKey(name) && wrap is null) wrap = ParseKeyword(reader, WrapKeywords);
            else throw TokenReader.Error($"Unexpected '{token}' in flex-flow.", token);
        }
        if (direction is null && wrap is null) throw TokenReader.Error("Expected a flex direction or a wrap value.", reader.Peek());
        return [(FlexDirection, direction ?? Style.FlexDirection.Row), (FlexWrap, wrap ?? Style.FlexWrap.NoWrap)];
    }

    private static GridTrackList ParseTrackList(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return GridTrackList.None;
        var tracks = new List<TrackSize>();
        var repeat = GridAutoRepeat.None;
        var repeatIndex = 0;
        var repeatTracks = new List<TrackSize>();
        while (!reader.AtEnd)
        {
            var token = reader.Peek();
            if (token.Kind == StyleTokenKind.LeftBracket)
                throw TokenReader.Error("Line names ([name]) are not supported: name the areas with grid-template-areas.", token);
            if (token.Kind == StyleTokenKind.Function && token.Value.Equals("repeat", StringComparison.OrdinalIgnoreCase))
            {
                reader.Next();
                var count = 0;
                var mode = GridAutoRepeat.None;
                var first = reader.Peek();
                if (first.Is(StyleTokenKind.Ident, "auto-fill") || first.Is(StyleTokenKind.Ident, "auto-fit"))
                {
                    if (repeat != GridAutoRepeat.None) throw TokenReader.Error("A track list has at most one repeat(auto-fill) or repeat(auto-fit).", first);
                    reader.Next();
                    mode = first.Is(StyleTokenKind.Ident, "auto-fill") ? GridAutoRepeat.Fill : GridAutoRepeat.Fit;
                }
                else count = ParseInteger(reader, 1);
                reader.Expect(StyleTokenKind.Comma, "','");
                var inner = new List<TrackSize>();
                while (!reader.AtEnd && reader.Peek().Kind != StyleTokenKind.RightParen) inner.Add(ParseTrackSize(reader));
                if (inner.Count == 0) throw TokenReader.Error("repeat() needs at least one track size.", reader.Peek());
                reader.Expect(StyleTokenKind.RightParen, "')'");
                if (mode == GridAutoRepeat.None)
                {
                    if ((long)count * inner.Count > 10000) throw TokenReader.Error("Too many tracks: at most 10000.", first);
                    for (var i = 0; i < count; i++) tracks.AddRange(inner);
                }
                else
                {
                    if (!inner.All(IsFixedSize))
                        throw TokenReader.Error("repeat(auto-fill) and repeat(auto-fit) need fixed sizes: lengths, or minmax() with a length.", first);
                    repeat = mode;
                    repeatIndex = tracks.Count;
                    repeatTracks = inner;
                }
                continue;
            }
            tracks.Add(ParseTrackSize(reader));
        }
        if (tracks.Count == 0 && repeat == GridAutoRepeat.None) throw TokenReader.Error("Expected a track size.", reader.Peek());
        if (repeat != GridAutoRepeat.None && !tracks.All(IsFixedSize))
            throw TokenReader.Error("Next to repeat(auto-fill) or repeat(auto-fit) every track needs a fixed size.", reader.Tokens.Count > 0 ? reader.Tokens[0] : reader.Peek());
        return new GridTrackList(new ValueList<TrackSize>(tracks), repeat, repeatIndex, new ValueList<TrackSize>(repeatTracks));
    }

    private static bool IsFixedSize(TrackSize track) =>
        !track.FitContent && track.Min.Kind != TrackBreadthKind.Flex && (track.Min.IsFixed || track.Max.IsFixed);

    private static ValueList<TrackSize> ParseTrackSizes(TokenReader reader)
    {
        var tracks = new List<TrackSize>();
        while (!reader.AtEnd) tracks.Add(ParseTrackSize(reader));
        if (tracks.Count == 0) throw TokenReader.Error("Expected a track size.", reader.Peek());
        return new ValueList<TrackSize>(tracks);
    }

    private static TrackSize ParseTrackSize(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Function && token.Value.Equals("minmax", StringComparison.OrdinalIgnoreCase))
        {
            reader.Next();
            var min = ParseBreadth(reader, allowFlex: false);
            reader.Expect(StyleTokenKind.Comma, "','");
            var max = ParseBreadth(reader, allowFlex: true);
            reader.Expect(StyleTokenKind.RightParen, "')'");
            return new TrackSize(min, max);
        }
        if (token.Kind == StyleTokenKind.Function && token.Value.Equals("fit-content", StringComparison.OrdinalIgnoreCase))
        {
            reader.Next();
            var limit = ParseLength(reader);
            reader.Expect(StyleTokenKind.RightParen, "')'");
            return new TrackSize(TrackBreadth.Auto, TrackBreadth.Fixed(limit), FitContent: true);
        }
        var breadth = ParseBreadth(reader, allowFlex: true);
        return breadth.Kind == TrackBreadthKind.Flex ? new TrackSize(TrackBreadth.Auto, breadth) : new TrackSize(breadth, breadth);
    }

    private static TrackBreadth ParseBreadth(TokenReader reader, bool allowFlex)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Ident)
        {
            switch (token.Value.ToLowerInvariant())
            {
                case "auto":
                    reader.Next();
                    return TrackBreadth.Auto;
                case "min-content":
                    reader.Next();
                    return TrackBreadth.MinContent;
                case "max-content":
                    reader.Next();
                    return TrackBreadth.MaxContent;
            }
            throw TokenReader.Error($"Expected a track size, found '{token}'.", token);
        }
        if (token.Kind == StyleTokenKind.Dimension && token.Value == "fr")
        {
            reader.Next();
            if (!allowFlex) throw TokenReader.Error("fr is not allowed as the minimum of minmax().", token);
            if (token.Number < 0) throw TokenReader.Error("Negative values are not allowed here.", token);
            return TrackBreadth.Fr(token.Number);
        }
        return TrackBreadth.Fixed(ParseLength(reader));
    }

    private static GridAreas ParseAreas(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return GridAreas.None;
        var rows = new List<string>();
        var first = reader.Peek();
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            if (token.Kind != StyleTokenKind.String) throw TokenReader.Error("Expected a string for each row of the grid.", token);
            rows.Add(token.Value);
        }
        try
        {
            return new GridAreas(rows);
        }
        catch (ArgumentException exception)
        {
            throw TokenReader.Error(exception.Message, first);
        }
    }

    private static GridAutoFlow ParseAutoFlow(TokenReader reader)
    {
        bool? column = null;
        var dense = false;
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            if (token.Is(StyleTokenKind.Ident, "row") && column is null) column = false;
            else if (token.Is(StyleTokenKind.Ident, "column") && column is null) column = true;
            else if (token.Is(StyleTokenKind.Ident, "dense") && !dense) dense = true;
            else throw TokenReader.Error($"Unexpected '{token}' in grid-auto-flow.", token);
        }
        if (column is null && !dense) throw TokenReader.Error("Expected row, column or dense.", reader.Peek());
        return (column ?? false, dense) switch
        {
            (false, false) => Style.GridAutoFlow.Row,
            (true, false) => Style.GridAutoFlow.Column,
            (false, true) => Style.GridAutoFlow.RowDense,
            _ => Style.GridAutoFlow.ColumnDense,
        };
    }

    private static GridLine ParseGridLine(TokenReader reader)
    {
        var token = reader.Peek();
        if (reader.TryConsumeIdent("auto")) return GridLine.Auto;
        if (reader.TryConsumeIdent("span"))
        {
            if (reader.Peek().Kind != StyleTokenKind.Number) throw TokenReader.Error("Expected the number of tracks after span: named lines are not supported.", reader.Peek());
            return GridLine.Span(ParseInteger(reader, 1));
        }
        if (token.Kind == StyleTokenKind.Number)
        {
            var line = ParseInteger(reader);
            if (line == 0) throw TokenReader.Error("There is no grid line 0: lines start at 1, or at -1 from the end.", token);
            return GridLine.Line(line);
        }
        if (token.Kind == StyleTokenKind.Ident)
        {
            reader.Next();
            return GridLine.Area(token.Value);
        }
        throw TokenReader.Error("Expected auto, a line number, span and a number, or an area name.", token);
    }

    private static bool TryConsumeSlash(TokenReader reader)
    {
        if (!reader.Peek().IsDelim('/')) return false;
        reader.Next();
        return true;
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseGridSpan(TokenReader reader, StyleProperty<GridLine> start, StyleProperty<GridLine> end)
    {
        var first = ParseGridLine(reader);
        var second = TryConsumeSlash(reader) ? ParseGridLine(reader) : first.Kind == GridLineKind.Area ? first : GridLine.Auto;
        return [(start, first), (end, second)];
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseGridArea(TokenReader reader)
    {
        var lines = new List<GridLine> { ParseGridLine(reader) };
        while (lines.Count < 4 && TryConsumeSlash(reader)) lines.Add(ParseGridLine(reader));
        GridLine Default(int index, int from) => lines.Count > index ? lines[index] : lines[from].Kind == GridLineKind.Area ? lines[from] : GridLine.Auto;
        var rowStart = lines[0];
        var columnStart = Default(1, 0);
        var rowEnd = Default(2, 0);
        var columnEnd = lines.Count > 3 ? lines[3] : columnStart.Kind == GridLineKind.Area ? columnStart : GridLine.Auto;
        return [(GridRowStart, rowStart), (GridColumnStart, columnStart), (GridRowEnd, rowEnd), (GridColumnEnd, columnEnd)];
    }

    private static IReadOnlyList<(StyleProperty, object)> ParsePlace<TAlign, TJustify>(TokenReader reader,
        StyleProperty<TAlign> align, Dictionary<string, TAlign> alignKeywords, StyleProperty<TJustify> justify, Dictionary<string, TJustify> justifyKeywords)
        where TAlign : struct where TJustify : struct
    {
        var first = reader.Peek();
        var alignValue = ParseKeyword(reader, alignKeywords);
        var justifyValue = reader.AtEnd ? ParseKeyword(new TokenReader([first]), justifyKeywords) : ParseKeyword(reader, justifyKeywords);
        return [(align, alignValue), (justify, justifyValue)];
    }

    private static TextDecorationLine ParseDecorationLine(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return Style.TextDecorationLine.None;
        var lines = Style.TextDecorationLine.None;
        while (!reader.AtEnd)
        {
            var token = reader.Next();
            var line = token.Kind != StyleTokenKind.Ident ? Style.TextDecorationLine.None : token.Value.ToLowerInvariant() switch
            {
                "underline" => Style.TextDecorationLine.Underline,
                "overline" => Style.TextDecorationLine.Overline,
                "line-through" => Style.TextDecorationLine.LineThrough,
                _ => Style.TextDecorationLine.None,
            };
            if (line == Style.TextDecorationLine.None) throw TokenReader.Error("Expected underline, overline or line-through.", token);
            if ((lines & line) != 0) throw TokenReader.Error($"Duplicate '{token}'.", token);
            lines |= line;
        }
        return lines;
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseTextDecoration(TokenReader reader)
    {
        var lineTokens = new List<StyleToken>();
        TextDecorationStyle? style = null;
        Color? color = null;
        SizeValue? thickness = null;
        while (!reader.AtEnd)
        {
            var token = reader.Peek();
            var name = token.Kind == StyleTokenKind.Ident ? token.Value.ToLowerInvariant() : "";
            if (name is "underline" or "overline" or "line-through" or "none") lineTokens.Add(reader.Next());
            else if (DecorationStyleKeywords.ContainsKey(name) && style is null) style = ParseKeyword(reader, DecorationStyleKeywords);
            else if ((IsLengthStart(token) || name == "auto") && thickness is null)
                thickness = reader.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(reader, allowPercent: false));
            else if (color is null) color = ParseColor(reader);
            else throw TokenReader.Error($"Unexpected '{token}' in text-decoration.", token);
        }
        var lines = lineTokens.Count == 0 ? Style.TextDecorationLine.None : ParseDecorationLine(new TokenReader(lineTokens));
        if (lineTokens.Count == 0 && style is null && color is null && thickness is null) throw TokenReader.Error("Expected a text decoration.", reader.Peek());
        return [(TextDecorationLine, lines), (TextDecorationStyle, style ?? Style.TextDecorationStyle.Solid),
            (TextDecorationColor, color ?? Style.Color.CurrentColor), (TextDecorationThickness, thickness ?? SizeValue.Auto)];
    }

    private static ValueList<ImageValue> ParseBorderImage(TokenReader reader)
    {
        var token = reader.Peek();
        var images = ParseImages(reader);
        if (images.Items.Count > 1 || images.Items.Any(image => image is not Gradient))
            throw TokenReader.Error("A border image must be one gradient or none.", token);
        return images;
    }

    private static ValueList<ImageValue> ParseImages(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return ValueList<ImageValue>.Empty;
        var images = new List<ImageValue>();
        foreach (var part in reader.SplitByComma())
        {
            images.Add(ParseImage(part));
            part.ExpectEnd();
        }
        return new ValueList<ImageValue>(images);
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseBackground(TokenReader reader)
    {
        var layers = reader.SplitByComma();
        var images = new List<ImageValue>();
        var positions = new List<PositionValue>();
        var sizes = new List<BackgroundSize>();
        var repeats = new List<BackgroundRepeat>();
        Color? color = null;
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            bool image = false, imageNone = false;
            PositionValue? position = null;
            BackgroundSize? size = null;
            BackgroundRepeat? repeat = null;
            if (layer.AtEnd) throw TokenReader.Error("Expected a color, an image or a gradient.", layer.Peek());
            while (!layer.AtEnd)
            {
                var token = layer.Peek();
                var word = token.Kind == StyleTokenKind.Ident ? token.Value.ToLowerInvariant() : "";
                if (token.Kind == StyleTokenKind.Function && IsImageFunction(token.Value) || word == "none")
                {
                    if (image) throw TokenReader.Error("A background layer has one image.", token);
                    image = true;
                    if (layer.TryConsumeIdent("none")) imageNone = true;
                    else images.Add(ParseImage(layer));
                }
                else if (RepeatKeywords.ContainsKey(word) && repeat is null) repeat = ParseKeyword(layer, RepeatKeywords);
                else if (IsPositionStart(token) && position is null)
                {
                    position = ParseBackgroundPosition(layer);
                    if (layer.Peek().IsDelim('/'))
                    {
                        layer.Next();
                        size = ParseBackgroundSize(layer);
                    }
                }
                else if (i == layers.Count - 1 && color is null) color = ParseColor(layer);
                else throw TokenReader.Error(color is null ? "Only the last background layer has a color." : $"Unexpected '{token}' in background.", token);
            }
            if (!image || imageNone) continue;
            positions.Add(position ?? new PositionValue(Length.Percent(0), Length.Percent(0)));
            sizes.Add(size ?? Style.BackgroundSize.Auto);
            repeats.Add(repeat ?? Style.BackgroundRepeat.Repeat);
        }
        return [(BackgroundColor, color ?? Style.Color.Transparent), (BackgroundImage, new ValueList<ImageValue>(images)),
            (BackgroundPosition, positions.Count > 0 ? new ValueList<PositionValue>(positions) : BackgroundPosition.Initial),
            (BackgroundSize, sizes.Count > 0 ? new ValueList<BackgroundSize>(sizes) : BackgroundSize.Initial),
            (BackgroundRepeat, repeats.Count > 0 ? new ValueList<BackgroundRepeat>(repeats) : BackgroundRepeat.Initial)];
    }

    /// <summary>
    /// Reads the position of a background layer: one or two components, stopping at the first value that cannot start
    /// one (the size after <c>/</c>, a repeat keyword or the color), so each <c>calc()</c> counts as one component.
    /// </summary>
    private static PositionValue ParseBackgroundPosition(TokenReader reader)
    {
        var (x, y) = ParsePosition(reader, stopAtOtherValues: true);
        return new PositionValue(x, y);
    }

    private static bool IsPositionStart(StyleToken token) =>
        IsLengthStart(token) || token.Kind == StyleTokenKind.Ident &&
        token.Value.ToLowerInvariant() is "left" or "right" or "top" or "bottom" or "center";

    private static ValueList<T> ParseEach<T>(TokenReader reader, Func<TokenReader, T> parse) => new(reader.SplitByComma().Select(part =>
    {
        var value = parse(part);
        part.ExpectEnd();
        return value;
    }).ToArray());

    private static BackgroundSize ParseBackgroundSize(TokenReader reader)
    {
        if (reader.TryConsumeIdent("cover")) return new BackgroundSize(BackgroundSizeKind.Cover, SizeValue.Auto, SizeValue.Auto);
        if (reader.TryConsumeIdent("contain")) return new BackgroundSize(BackgroundSizeKind.Contain, SizeValue.Auto, SizeValue.Auto);
        var width = ParseAutoLength(reader);
        var token = reader.Peek();
        var height = token.Is(StyleTokenKind.Ident, "auto") || IsLengthStart(token) ? ParseAutoLength(reader) : SizeValue.Auto;
        return new BackgroundSize(BackgroundSizeKind.Explicit, width, height);

        static SizeValue ParseAutoLength(TokenReader reader) => reader.TryConsumeIdent("auto") ? SizeValue.Auto : SizeValue.Fixed(ParseLength(reader));
    }

    private static ImageValue ParseImage(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind == StyleTokenKind.Function && token.Value.Equals("url", StringComparison.OrdinalIgnoreCase))
        {
            reader.Next();
            var url = reader.Next();
            if (url.Kind != StyleTokenKind.String || url.Value.Length == 0) throw TokenReader.Error("Expected the URL of the image: url(\"images/logo.png\").", url);
            reader.Expect(StyleTokenKind.RightParen, "')'");
            return new UrlImage(url.Value);
        }
        return ParseGradient(reader);
    }

    private static bool IsImageFunction(string name) => name.ToLowerInvariant() is "linear-gradient" or "radial-gradient" or "repeating-linear-gradient"
        or "repeating-radial-gradient" or "conic-gradient" or "repeating-conic-gradient" or "url" or "image";

    private static Gradient ParseGradient(TokenReader reader)
    {
        var function = reader.Next();
        if (function.Kind != StyleTokenKind.Function) throw TokenReader.Error("Expected a linear, radial or conic gradient.", function);
        var name = function.Value.ToLowerInvariant();
        var repeating = name.StartsWith("repeating-", StringComparison.Ordinal);
        if (repeating) name = name[10..];
        if (name is not ("linear-gradient" or "radial-gradient" or "conic-gradient"))
            throw TokenReader.Error($"{function.Value}() is not supported: use a linear, radial or conic gradient.", function);
        var arguments = FunctionArguments(reader, function);
        Gradient gradient = name switch
        {
            "linear-gradient" => ParseLinearGradient(arguments, function),
            "radial-gradient" => ParseRadialGradient(arguments, function),
            _ => ParseConicGradient(arguments, function),
        };
        return gradient with { Repeating = repeating };
    }

    private static ValueList<TransformFunction> ParseTransform(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return ValueList<TransformFunction>.Empty;
        var functions = new List<TransformFunction>();
        while (!reader.AtEnd)
        {
            var function = reader.Next();
            if (function.Kind != StyleTokenKind.Function)
                throw TokenReader.Error($"Expected a transform function (translate(), scale(), rotate(), skew(), matrix()), found '{function}'.", function);
            functions.Add(ParseTransformFunction(function, FunctionArguments(reader, function)));
        }
        if (functions.Count == 0) throw TokenReader.Error("Expected none or a transform function.", reader.Peek());
        return new ValueList<TransformFunction>(functions);
    }

    private static TransformFunction ParseTransformFunction(StyleToken function, List<TokenReader> arguments)
    {
        var name = function.Value.ToLowerInvariant();
        var (min, max) = name switch
        {
            "translate" or "scale" or "skew" => (1, 2),
            "translatex" or "translatey" or "scalex" or "scaley" or "rotate" or "skewx" or "skewy" => (1, 1),
            "matrix" => (6, 6),
            "translate3d" or "translatez" or "scale3d" or "scalez" or "rotate3d" or "rotatex" or "rotatey" or "rotatez" or "matrix3d" or "perspective" =>
                throw TokenReader.Error($"{function.Value}() is a 3D transform: only 2D transforms are supported.", function),
            _ => throw TokenReader.Error($"Unknown transform function '{function.Value}()': use translate, scale, rotate, skew or matrix.", function),
        };
        if (arguments.Count < min || arguments.Count > max || arguments.Any(a => a.AtEnd))
            throw TokenReader.Error(min == max ? $"{function.Value}() takes {min} comma-separated value{(min == 1 ? "" : "s")}." : $"{function.Value}() takes one or two comma-separated values.", function);
        T Argument<T>(int index, Func<TokenReader, T> parse)
        {
            var value = parse(arguments[index]);
            arguments[index].ExpectEnd();
            return value;
        }
        Length Translation(int index) => Argument(index, r => ParseLength(r, allowNegative: true));
        double Scale(int index) => Argument(index, ParseScale);
        double Angle(int index) => Argument(index, ParseAngle);
        switch (name)
        {
            case "translate": return new TranslateFunction(Translation(0), arguments.Count > 1 ? Translation(1) : Length.Zero);
            case "translatex": return new TranslateFunction(Translation(0), Length.Zero);
            case "translatey": return new TranslateFunction(Length.Zero, Translation(0));
            case "scale":
                var x = Scale(0);
                return new ScaleFunction(x, arguments.Count > 1 ? Scale(1) : x);
            case "scalex": return new ScaleFunction(Scale(0), 1);
            case "scaley": return new ScaleFunction(1, Scale(0));
            case "rotate": return new RotateFunction(Angle(0));
            case "skew": return new SkewFunction(Angle(0), arguments.Count > 1 ? Angle(1) : 0);
            case "skewx": return new SkewFunction(Angle(0), 0);
            case "skewy": return new SkewFunction(0, Angle(0));
            default:
                var values = Enumerable.Range(0, 6).Select(i => Argument(i, r => ParseNumber(r))).ToArray();
                return new MatrixFunction(values[0], values[1], values[2], values[3], values[4], values[5]);
        }
    }

    private static double ParseScale(TokenReader reader)
    {
        var token = reader.Peek();
        if (token.Kind != StyleTokenKind.Percentage) return ParseNumber(reader);
        reader.Next();
        return token.Number / 100;
    }

    private static ClipPathValue ParseClipPath(TokenReader reader)
    {
        if (reader.TryConsumeIdent("none")) return ClipPathValue.None;
        var function = reader.Next();
        if (!function.Is(StyleTokenKind.Function, "inset")) throw TokenReader.Error("Expected none or inset().", function);
        var arguments = reader.FunctionArguments(function);
        if (arguments.Count != 1) throw TokenReader.Error("Expected one to four space-separated insets.", function);
        var values = new List<Length>();
        var argument = arguments[0];
        while (!argument.AtEnd && !argument.Peek().Is(StyleTokenKind.Ident, "round") && values.Count < 4) values.Add(ParseLength(argument, allowNegative: true));
        if (values.Count == 0) throw TokenReader.Error("Expected one to four insets.", function);
        var radii = argument.TryConsumeIdent("round") ? ParseRadiusCorners(argument) : [RadiusValue.Zero, RadiusValue.Zero, RadiusValue.Zero, RadiusValue.Zero];
        argument.ExpectEnd();
        return new ClipPathValue(values[0], values.Count > 1 ? values[1] : values[0],
            values.Count > 2 ? values[2] : values[0], values.Count > 3 ? values[3] : values.Count > 1 ? values[1] : values[0])
        {
            TopLeftRadius = radii[0], TopRightRadius = radii[1], BottomRightRadius = radii[2], BottomLeftRadius = radii[3],
        };
    }

    private static TransformOrigin ParseTransformOrigin(TokenReader reader)
    {
        var (x, y) = ParsePosition(reader);
        return new TransformOrigin(x, y);
    }

    private static List<TokenReader> FunctionArguments(TokenReader reader, StyleToken function) => reader.FunctionArguments(function);

    private static LinearGradient ParseLinearGradient(List<TokenReader> arguments, StyleToken function)
    {
        var first = arguments[0];
        var token = first.Peek();
        double? angle = 180;
        int toX = 0, toY = 0;
        var stops = arguments;
        if (token.Kind == StyleTokenKind.Dimension || (token.Kind == StyleTokenKind.Number && token.Number == 0))
        {
            angle = ParseAngle(first);
            first.ExpectEnd();
            stops = arguments.Skip(1).ToList();
        }
        else if (first.TryConsumeIdent("to"))
        {
            angle = null;
            while (!first.AtEnd)
            {
                var side = first.Next();
                switch (side.Kind == StyleTokenKind.Ident ? side.Value.ToLowerInvariant() : "")
                {
                    case "top" when toY == 0: toY = -1; break;
                    case "bottom" when toY == 0: toY = 1; break;
                    case "left" when toX == 0: toX = -1; break;
                    case "right" when toX == 0: toX = 1; break;
                    default: throw TokenReader.Error($"Unexpected '{side}' after to: expected top, bottom, left or right.", side);
                }
            }
            if (toX == 0 && toY == 0) throw TokenReader.Error("Expected a side or a corner after to.", token);
            stops = arguments.Skip(1).ToList();
        }
        return new LinearGradient(angle, toX, toY, ParseColorStops(stops, function));
    }

    private static RadialGradient ParseRadialGradient(List<TokenReader> arguments, StyleToken function)
    {
        var first = arguments[0];
        RadialShape? shape = null;
        var extent = RadialExtent.FarthestCorner;
        var sizes = new List<Length>();
        Length centerX = Length.Percent(50), centerY = Length.Percent(50);
        var stops = arguments;
        if (!StartsWithColor(first.Peek()))
        {
            stops = arguments.Skip(1).ToList();
            var explicitExtent = false;
            while (!first.AtEnd && !first.Peek().Is(StyleTokenKind.Ident, "at"))
            {
                var token = first.Peek();
                if (token.Kind == StyleTokenKind.Ident)
                {
                    first.Next();
                    switch (token.Value.ToLowerInvariant())
                    {
                        case "circle" when shape is null: shape = RadialShape.Circle; break;
                        case "ellipse" when shape is null: shape = RadialShape.Ellipse; break;
                        case "closest-side" when !explicitExtent: extent = RadialExtent.ClosestSide; explicitExtent = true; break;
                        case "farthest-side" when !explicitExtent: extent = RadialExtent.FarthestSide; explicitExtent = true; break;
                        case "closest-corner" when !explicitExtent: extent = RadialExtent.ClosestCorner; explicitExtent = true; break;
                        case "farthest-corner" when !explicitExtent: extent = RadialExtent.FarthestCorner; explicitExtent = true; break;
                        default: throw TokenReader.Error($"Unexpected '{token}' in radial-gradient().", token);
                    }
                }
                else
                {
                    if (explicitExtent || sizes.Count == 2) throw TokenReader.Error($"Unexpected '{token}' in radial-gradient().", token);
                    sizes.Add(ParseLength(first));
                }
            }
            if (sizes.Count > 0)
            {
                shape ??= sizes.Count == 1 ? RadialShape.Circle : RadialShape.Ellipse;
                if (shape == RadialShape.Circle && (sizes.Count != 1 || sizes[0].IsPercent))
                    throw TokenReader.Error("A circle has one radius, a length.", first.Tokens[0]);
                if (shape == RadialShape.Ellipse && sizes.Count != 2)
                    throw TokenReader.Error("An ellipse has two radii.", first.Tokens[0]);
                extent = RadialExtent.Explicit;
            }
            if (first.TryConsumeIdent("at")) (centerX, centerY) = ParsePosition(first);
            first.ExpectEnd();
        }
        var circle = shape == RadialShape.Circle;
        return new RadialGradient(shape ?? RadialShape.Ellipse, extent, sizes.Count > 0 ? sizes[0] : Length.Zero,
            sizes.Count > 1 ? sizes[1] : circle && sizes.Count > 0 ? sizes[0] : Length.Zero, centerX, centerY, ParseColorStops(stops, function));
    }

    private static bool StartsWithColor(StyleToken token) =>
        token.Kind == StyleTokenKind.Hash ||
        (token.Kind == StyleTokenKind.Function && token.Value.ToLowerInvariant() is "rgb" or "rgba" or "hsl" or "hsla" or "color-mix" or "var") ||
        (token.Kind == StyleTokenKind.Ident && (NamedColors.TryGet(token.Value, out _) || token.Value.Equals("currentcolor", StringComparison.OrdinalIgnoreCase)));

    private static (Length X, Length Y) ParsePosition(TokenReader reader, bool stopAtOtherValues = false)
    {
        var start = reader.Peek();
        var values = new List<(Length Value, char Axis)>();
        while (!reader.AtEnd && values.Count < 2)
        {
            var token = reader.Peek();
            if (stopAtOtherValues && !IsPositionStart(token)) break;
            if (token.Kind == StyleTokenKind.Ident)
            {
                reader.Next();
                values.Add(token.Value.ToLowerInvariant() switch
                {
                    "left" => (Length.Percent(0), 'x'),
                    "right" => (Length.Percent(100), 'x'),
                    "top" => (Length.Percent(0), 'y'),
                    "bottom" => (Length.Percent(100), 'y'),
                    "center" => (Length.Percent(50), 'c'),
                    _ => throw TokenReader.Error($"Unexpected '{token}' in a position.", token),
                });
            }
            else values.Add((ParseLength(reader, allowNegative: true), 'n'));
        }
        if (values.Count == 0) throw TokenReader.Error("Expected a position after at.", reader.Peek());
        if (values.Count == 1) return values[0].Axis == 'y' ? (Length.Percent(50), values[0].Value) : (values[0].Value, Length.Percent(50));

        if (values[0].Axis == 'y' || values[1].Axis == 'x')
        {
            if (values[0].Axis is 'x' || values[1].Axis is 'y' || values[0].Axis == 'n' || values[1].Axis == 'n')
                throw TokenReader.Error("Invalid position: use x then y, or two keywords.", start);
            return (values[1].Value, values[0].Value);
        }
        return (values[0].Value, values[1].Value);
    }

    private static ConicGradient ParseConicGradient(List<TokenReader> arguments, StyleToken function)
    {
        var first = arguments[0];
        var angle = 0d;
        var x = Length.Percent(50); var y = Length.Percent(50);
        var preamble = first.Peek().Is(StyleTokenKind.Ident, "from") || first.Peek().Is(StyleTokenKind.Ident, "at");
        if (preamble)
        {
            if (first.TryConsumeIdent("from")) angle = ParseAngle(first);
            if (first.TryConsumeIdent("at")) (x, y) = ParsePosition(first);
            first.ExpectEnd();
        }
        return new ConicGradient(angle, x, y, ParseColorStops(preamble ? arguments.Skip(1).ToList() : arguments, function, angular: true));
    }

    private static Length ParseAngularStop(TokenReader reader)
    {
        if (reader.Peek().Kind != StyleTokenKind.Percentage) return Length.Percent(ParseAngle(reader) / 3.6);
        return Length.Percent(reader.Next().Number);
    }

    private static ValueList<ColorStop> ParseColorStops(List<TokenReader> parts, StyleToken function, bool angular = false)
    {
        var stops = new List<ColorStop>();
        foreach (var part in parts)
        {
            var token = part.Peek();
            if (IsLengthStart(token))
                throw TokenReader.Error("Color hints (a position without a color) are not supported.", token);
            var color = ParseColor(part);
            if (part.AtEnd)
            {
                stops.Add(new ColorStop(color, null));
                continue;
            }
            stops.Add(new ColorStop(color, angular ? ParseAngularStop(part) : ParseLength(part, allowNegative: true)));
            if (!part.AtEnd) stops.Add(new ColorStop(color, angular ? ParseAngularStop(part) : ParseLength(part, allowNegative: true)));
            part.ExpectEnd();
        }
        if (stops.Count < 2) throw TokenReader.Error($"{function.Value}() needs at least two color stops.", function);
        return new ValueList<ColorStop>(stops);
    }

    private static double ParseAngle(TokenReader reader)
    {
        var token = reader.Next();
        if (!double.IsFinite(token.Number)) throw TokenReader.Error("An angle must be finite.", token);
        if (token.Kind == StyleTokenKind.Number && token.Number == 0) return 0;
        if (token.Kind == StyleTokenKind.Dimension)
        {
            switch (token.Value.ToLowerInvariant())
            {
                case "deg": return token.Number;
                case "grad": return token.Number * 0.9;
                case "rad": return FiniteAngle(token.Number / Math.PI * 180, token);
                case "turn": return FiniteAngle(token.Number * 360, token);
            }
        }
        throw TokenReader.Error("Expected an angle (deg, grad, rad, turn).", token);
    }

    private static double FiniteAngle(double angle, StyleToken token) => double.IsFinite(angle) ? angle : throw TokenReader.Error("An angle must be finite.", token);

    private static WindowFrameShadows ParseFrameShadows(TokenReader reader)
    {
        if (reader.TryConsumeIdent("auto")) return default;
        var shadows = ParseShadows(reader);
        if (shadows.Items.Any(shadow => shadow.Inset)) throw TokenReader.Error("A window frame shadow must be exterior.", reader.Peek());
        return new(shadows);
    }

    private static ValueList<BoxShadow> ParseShadows(TokenReader reader)
        => ParseShadows(reader, text: false);

    private static ValueList<BoxShadow> ParseShadows(TokenReader reader, bool text)
    {
        if (reader.TryConsumeIdent("none")) return ValueList<BoxShadow>.Empty;
        var shadows = new List<BoxShadow>();
        foreach (var part in reader.SplitByComma())
        {
            var inset = false;
            var lengths = new List<Length>();
            Color? color = null;
            var first = part.Peek();
            while (!part.AtEnd)
            {
                var token = part.Peek();
                if (token.Is(StyleTokenKind.Ident, "inset"))
                {
                    if (text) throw TokenReader.Error("Badge text shadows cannot use inset.", token);
                    if (inset) throw TokenReader.Error("Duplicate inset.", token);
                    part.Next();
                    inset = true;
                }
                else if (IsLengthStart(token))
                {
                    if (text && lengths.Count == 3) throw TokenReader.Error("Badge text shadows cannot use spread.", token);
                    if (lengths.Count == 4) throw TokenReader.Error("A shadow has at most four lengths: x, y, blur and spread.", token);
                    var length = ParseLength(part, allowPercent: false, allowNegative: true);
                    if (lengths.Count == 2 && length.Value < 0) throw TokenReader.Error("The blur radius cannot be negative.", token);
                    if (lengths.Count == 2 && length.Expression is { } blur) length = Length.Calc(blur with { NonNegative = true });
                    lengths.Add(length);
                }
                else
                {
                    if (color is not null) throw TokenReader.Error($"Unexpected '{token}' in box-shadow.", token);
                    color = ParseColor(part);
                }
            }
            if (lengths.Count < 2) throw TokenReader.Error("A shadow needs at least the x and y offsets.", first);
            shadows.Add(new BoxShadow(inset, lengths[0], lengths[1], lengths.Count > 2 ? lengths[2] : Length.Zero, lengths.Count > 3 ? lengths[3] : Length.Zero,
                color ?? Style.Color.CurrentColor));
        }
        return new ValueList<BoxShadow>(shadows);
    }

    private static RadiusValue ParseRadius(TokenReader reader)
    {
        var x = ParseLength(reader);
        return new RadiusValue(x, reader.AtEnd ? x : ParseLength(reader));
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseBorderRadius(TokenReader reader)
    {
        var radii = ParseRadiusCorners(reader);
        return [(BorderTopLeftRadius, radii[0]), (BorderTopRightRadius, radii[1]), (BorderBottomRightRadius, radii[2]), (BorderBottomLeftRadius, radii[3])];
    }

    private static RadiusValue[] ParseRadiusCorners(TokenReader reader)
    {
        static Length[] Corners(List<Length> values) =>
            [values[0], values.Count > 1 ? values[1] : values[0], values.Count > 2 ? values[2] : values[0], values.Count > 3 ? values[3] : values.Count > 1 ? values[1] : values[0]];
        var horizontal = new List<Length>();
        while (!reader.AtEnd && !reader.Peek().IsDelim('/') && horizontal.Count < 4) horizontal.Add(ParseLength(reader));
        if (horizontal.Count == 0) throw TokenReader.Error("Expected a radius.", reader.Peek());
        var vertical = horizontal;
        if (reader.Peek().IsDelim('/'))
        {
            reader.Next();
            vertical = [];
            while (!reader.AtEnd && vertical.Count < 4) vertical.Add(ParseLength(reader));
            if (vertical.Count == 0) throw TokenReader.Error("Expected a vertical radius after '/'.", reader.Peek());
        }
        var x = Corners(horizontal);
        var y = Corners(vertical);
        return [new RadiusValue(x[0], y[0]), new RadiusValue(x[1], y[1]), new RadiusValue(x[2], y[2]), new RadiusValue(x[3], y[3])];
    }

    private static IReadOnlyList<(StyleProperty, object)> ParseBorderSides(TokenReader reader, (StyleProperty Width, StyleProperty Style, StyleProperty Color)[] sides)
    {
        var (width, style, color) = ParseBorderSide(reader, sides[0].Width.Name.StartsWith("outline", StringComparison.Ordinal) ? "outline" : "border");
        var result = new List<(StyleProperty, object)>();
        foreach (var side in sides) result.Add((side.Width, width));
        foreach (var side in sides) result.Add((side.Style, style));
        foreach (var side in sides) result.Add((side.Color, color));
        return result;
    }

    private static (Length Width, BorderStyle Style, Color Color) ParseBorderSide(TokenReader reader, string name)
    {
        Length? width = null;
        BorderStyle? style = null;
        Color? color = null;
        while (!reader.AtEnd)
        {
            var token = reader.Peek();
            if (IsLengthStart(token) || (token.Kind == StyleTokenKind.Ident && token.Value.ToLowerInvariant() is "thin" or "medium" or "thick"))
            {
                if (width is not null) throw TokenReader.Error($"Duplicate {name} width.", token);
                width = ParseBorderWidth(reader);
            }
            else if (token.Kind == StyleTokenKind.Ident && BorderStyleKeywords.ContainsKey(token.Value.ToLowerInvariant()))
            {
                if (style is not null) throw TokenReader.Error($"Duplicate {name} style.", token);
                style = ParseKeyword(reader, BorderStyleKeywords);
            }
            else
            {
                if (color is not null) throw TokenReader.Error($"Unexpected '{token}' in {name}.", token);
                color = ParseColor(reader);
            }
        }
        if (width is null && style is null && color is null) throw TokenReader.Error($"Expected a width, a style or a color for {name}.", reader.Peek());
        return (width ?? Length.Px(3), style ?? Style.BorderStyle.None, color ?? Style.Color.CurrentColor);
    }
}
