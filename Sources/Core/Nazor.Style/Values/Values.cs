using System.Globalization;

namespace Nazor.Style;

/// <summary>Identifies the unit or calculation used to resolve an NSS length.</summary>
public enum LengthUnit : byte
{
    /// <summary>Measures logical pixels.</summary>
    Px,

    /// <summary>Measures a percentage of the applicable reference dimension.</summary>
    Percent,

    /// <summary>Measures multiples of the current font size.</summary>
    Em,

    /// <summary>Measures multiples of the root font size.</summary>
    Rem,

    /// <summary>Measures hundredths of the viewport width.</summary>
    Vw,

    /// <summary>Measures hundredths of the viewport height.</summary>
    Vh,

    /// <summary>Measures hundredths of the smaller viewport dimension.</summary>
    Vmin,

    /// <summary>Measures hundredths of the larger viewport dimension.</summary>
    Vmax,

    /// <summary>Evaluates a calculation expression to resolve the length.</summary>
    Calc,

    /// <summary>Measures multiples of the used horizontal advance of the font's zero glyph.</summary>
    Ch,

    /// <summary>Measures multiples of the first available font's used x-height.</summary>
    Ex,
}

/// <summary>
/// A length in logical units, a percentage resolved against a basis defined by each property, a relative length (em,
/// rem, vw...) or an expression. The style engine converts relative units to px: computed values have only px,
/// percentages and expressions with percentages.
/// </summary>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="Unit">The unit or calculation mode used to resolve the length.</param>
/// <param name="Expression">The optional calculation expression used when the length unit is Calc.</param>
public readonly record struct Length(double Value, LengthUnit Unit, CalcExpression? Expression = null)
{
    /// <summary>The length with all components set to zero.</summary>
    public static readonly Length Zero = new(0, LengthUnit.Px);

    /// <summary>Creates a length expressed in logical pixels.</summary>
    /// <param name="value">The numeric magnitude in the unit selected by this factory method.</param>
    /// <returns>The resulting Length value.</returns>
    public static Length Px(double value) => new(value, LengthUnit.Px);

    /// <summary>Creates a length expressed as a percentage of a reference dimension.</summary>
    /// <param name="value">The numeric magnitude in the unit selected by this factory method.</param>
    /// <returns>The resulting Length value.</returns>
    public static Length Percent(double value) => new(value, LengthUnit.Percent);

    /// <summary>Creates a length backed by a calculation expression.</summary>
    /// <param name="expression">The expression (CalcExpression).</param>
    /// <returns>The resulting Length value.</returns>
    public static Length Calc(CalcExpression expression) => new(0, LengthUnit.Calc, expression);

    /// <summary>The value depends on the percentage basis: a percentage or an expression with percentages.</summary>
    public bool IsPercent => Unit == LengthUnit.Percent || (Unit == LengthUnit.Calc && Expression!.HasPercent);

    /// <summary>Font-relative units, viewport units, or an expression with them.</summary>
    public bool IsRelative => Unit is >= LengthUnit.Em and <= LengthUnit.Vmax or LengthUnit.Ch or LengthUnit.Ex || (Unit == LengthUnit.Calc && Expression!.IsRelative);

    /// <summary>Resolves the length into logical pixels using the supplied percentage basis.</summary>
    /// <param name="basis">The reference dimension used to resolve percentage values.</param>
    /// <returns>The resulting double value.</returns>
    public double Resolve(double basis) => Unit switch
    {
        LengthUnit.Percent => basis * Value / 100,
        LengthUnit.Calc => Expression!.Evaluate(basis),
        _ => Value,
    };

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Unit switch
    {
        LengthUnit.Calc => Expression!.ToString(),
        LengthUnit.Percent => Value.ToString("0.###", CultureInfo.InvariantCulture) + "%",
        _ => Value.ToString("0.###", CultureInfo.InvariantCulture) + Unit.ToString().ToLowerInvariant(),
    };
}

/// <summary>Specifies automatic, fixed, and intrinsic sizing modes.</summary>
public enum SizeKind : byte
{
    /// <summary>Lets the layout algorithm determine the size.</summary>
    Auto,

    /// <summary>Leaves the maximum size unconstrained.</summary>
    None,

    /// <summary>Uses the content-based flex basis.</summary>
    Content,

    /// <summary>Resolves the supplied length to determine the size.</summary>
    Fixed,

    /// <summary>The narrowest width of the content: every soft wrap taken (CSS Sizing §2).</summary>
    MinContent,

    /// <summary>The width of the content without wrapping.</summary>
    MaxContent,

    /// <summary>The available width, but at least min-content and at most max-content.</summary>
    FitContent,
}

/// <summary>
/// A size that may also be <c>auto</c>, <c>none</c> (for maxima), <c>content</c> (for flex-basis) or an intrinsic
/// keyword (<c>min-content</c>, <c>max-content</c>, <c>fit-content</c>).
/// </summary>
/// <param name="Kind">The size kind represented by this value.</param>
/// <param name="Length">The absolute, relative, or percentage length to use.</param>
public readonly record struct SizeValue(SizeKind Kind, Length Length)
{
    /// <summary>Automatic sizing determined by the layout algorithm.</summary>
    public static readonly SizeValue Auto = new(SizeKind.Auto, Length.Zero);

    /// <summary>An unconstrained maximum size.</summary>
    public static readonly SizeValue None = new(SizeKind.None, Length.Zero);

    /// <summary>Content-based sizing for a flex item's basis.</summary>
    public static readonly SizeValue Content = new(SizeKind.Content, Length.Zero);

    /// <summary>Sizing at the smallest intrinsic content width.</summary>
    public static readonly SizeValue MinContent = new(SizeKind.MinContent, Length.Zero);

    /// <summary>Sizing at the unwrapped intrinsic content width.</summary>
    public static readonly SizeValue MaxContent = new(SizeKind.MaxContent, Length.Zero);

    /// <summary>Sizing clamped between minimum-content and maximum-content dimensions.</summary>
    public static readonly SizeValue FitContent = new(SizeKind.FitContent, Length.Zero);

    /// <summary>Gets whether the size uses minimum-content, maximum-content, or fit-content sizing.</summary>
    public bool IsIntrinsic => Kind is SizeKind.MinContent or SizeKind.MaxContent or SizeKind.FitContent;

    /// <summary>Creates a size constrained by the supplied length.</summary>
    /// <param name="length">The absolute, relative, or percentage length to use.</param>
    /// <returns>The resulting SizeValue value.</returns>
    public static SizeValue Fixed(Length length) => new(SizeKind.Fixed, length);

    /// <summary>Gets whether this size is represented by a concrete length.</summary>
    public bool IsFixed => Kind == SizeKind.Fixed;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        SizeKind.Auto => "auto",
        SizeKind.None => "none",
        SizeKind.Content => "content",
        SizeKind.MinContent => "min-content",
        SizeKind.MaxContent => "max-content",
        SizeKind.FitContent => "fit-content",
        _ => Length.ToString(),
    };
}

/// <summary>The preferred aspect ratio of a box, width divided by height; <see cref="Auto" /> is none.</summary>
/// <param name="Ratio">The preferred width divided by height; a nonpositive value represents automatic sizing.</param>
public readonly record struct AspectRatio(double Ratio)
{
    /// <summary>No explicit preferred aspect ratio.</summary>
    public static readonly AspectRatio Auto = new(0);

    /// <summary>Gets whether no positive preferred width-to-height ratio was supplied.</summary>
    public bool IsAuto => Ratio <= 0;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => IsAuto ? "auto" : Ratio.ToString("0.####", CultureInfo.InvariantCulture);
}

/// <summary>An sRGB color with straight (non-premultiplied) alpha, or the <c>currentColor</c> keyword.</summary>
/// <param name="R">The red channel in the range from zero to 255.</param>
/// <param name="G">The green channel in the range from zero to 255.</param>
/// <param name="B">The blue channel in the range from zero to 255.</param>
/// <param name="A">The alpha channel in the range from zero to 255.</param>
/// <param name="IsCurrentColor">The is current color (bool).</param>
public readonly record struct Color(byte R, byte G, byte B, byte A, bool IsCurrentColor = false)
{
    /// <summary>Fully transparent black, with all RGBA channels set to zero.</summary>
    public static readonly Color Transparent = new(0, 0, 0, 0);

    /// <summary>Opaque black, with zero RGB channels and full alpha.</summary>
    public static readonly Color Black = new(0, 0, 0, 255);

    /// <summary>Opaque white, with all RGBA channels set to 255.</summary>
    public static readonly Color White = new(255, 255, 255, 255);

    /// <summary>Placeholder resolved to the element's <c>color</c> when styles are computed.</summary>
    public static readonly Color CurrentColor = new(0, 0, 0, 0, true);

    /// <summary>Creates an opaque color from red, green, and blue channels.</summary>
    /// <param name="r">The red channel in the range from zero to 255.</param>
    /// <param name="g">The green channel in the range from zero to 255.</param>
    /// <param name="b">The blue channel in the range from zero to 255.</param>
    /// <returns>The resulting Color value.</returns>
    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);

    /// <summary>Packs as 0xRRGGBBAA, the CSS hexadecimal order.</summary>
    /// <returns>The resulting uint value.</returns>
    public uint ToRgba() => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | A;

    /// <summary>Creates a color from a packed 32-bit RGBA value, with red in the most significant byte.</summary>
    /// <param name="rgba">The packed color in 0xRRGGBBAA byte order.</param>
    /// <returns>The resulting Color value.</returns>
    public static Color FromRgba(uint rgba) => new((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => IsCurrentColor ? "currentColor" : A == 255 ? $"#{R:x2}{G:x2}{B:x2}" : $"#{R:x2}{G:x2}{B:x2}{A:x2}";
}

/// <summary>Specifies block, inline text, flex, grid, or suppressed layout.</summary>
public enum Display : byte
{
    /// <summary>Uses block layout.</summary>
    Block,

    /// <summary>Uses flex layout for child items.</summary>
    Flex,

    /// <summary>Uses grid layout for child items.</summary>
    Grid,

    /// <summary>Omits the element and its descendants from layout and painting.</summary>
    None,

    /// <summary>Participates in a parent text flow with shared wrapping and baseline placement; block/flex/grid children establish ordinary layout instead.</summary>
    Inline,
}

/// <summary>Specifies the main axis and item order of a flex container.</summary>
public enum FlexDirection : byte
{
    /// <summary>Lays out items horizontally in forward order.</summary>
    Row,

    /// <summary>Lays out items horizontally in reverse order.</summary>
    RowReverse,

    /// <summary>Lays out items vertically in forward order.</summary>
    Column,

    /// <summary>Lays out items vertically in reverse order.</summary>
    ColumnReverse,
}

/// <summary>Specifies whether flex items wrap and whether the line order is reversed.</summary>
public enum FlexWrap : byte
{
    /// <summary>Keeps flex items on a single line.</summary>
    NoWrap,

    /// <summary>Wraps flex items onto additional lines.</summary>
    Wrap,

    /// <summary>Wraps flex items with the cross-axis line order reversed.</summary>
    WrapReverse,
}

/// <summary>Specifies how free space is distributed along the layout's main axis.</summary>
public enum JustifyContent : byte
{
    /// <summary>Uses the layout mode's normal alignment behavior.</summary>
    Normal,

    /// <summary>Stretches items or tracks to fill available space.</summary>
    Stretch,

    /// <summary>Aligns items or tracks at the start of the applicable axis.</summary>
    FlexStart,

    /// <summary>Aligns items or tracks at the end of the applicable axis.</summary>
    FlexEnd,

    /// <summary>Centers items or tracks within the available space.</summary>
    Center,

    /// <summary>Distributes free space between items or tracks.</summary>
    SpaceBetween,

    /// <summary>Distributes free space around items or tracks, with half-sized outer gaps.</summary>
    SpaceAround,

    /// <summary>Distributes free space into equal inner and outer gaps.</summary>
    SpaceEvenly,
}

/// <summary>Distribution of the lines of a multi-line flex container on the cross axis.</summary>
public enum AlignContent : byte
{
    /// <summary>Uses the layout mode's normal alignment behavior.</summary>
    Normal,

    /// <summary>Stretches items or tracks to fill available space.</summary>
    Stretch,

    /// <summary>Aligns items or tracks at the start of the applicable axis.</summary>
    FlexStart,

    /// <summary>Aligns items or tracks at the end of the applicable axis.</summary>
    FlexEnd,

    /// <summary>Centers items or tracks within the available space.</summary>
    Center,

    /// <summary>Distributes free space between items or tracks.</summary>
    SpaceBetween,

    /// <summary>Distributes free space around items or tracks, with half-sized outer gaps.</summary>
    SpaceAround,

    /// <summary>Distributes free space into equal inner and outer gaps.</summary>
    SpaceEvenly,
}

/// <summary>Specifies the default cross-axis alignment of child items.</summary>
public enum AlignItems : byte
{
    /// <summary>Stretches items or tracks to fill available space.</summary>
    Stretch,

    /// <summary>Aligns items or tracks at the start of the applicable axis.</summary>
    FlexStart,

    /// <summary>Aligns items or tracks at the end of the applicable axis.</summary>
    FlexEnd,

    /// <summary>Centers items or tracks within the available space.</summary>
    Center,
}

/// <summary>Specifies a child item's cross-axis alignment override.</summary>
public enum AlignSelf : byte
{
    /// <summary>Uses the parent alignment setting.</summary>
    Auto,

    /// <summary>Stretches items or tracks to fill available space.</summary>
    Stretch,

    /// <summary>Aligns items or tracks at the start of the applicable axis.</summary>
    FlexStart,

    /// <summary>Aligns items or tracks at the end of the applicable axis.</summary>
    FlexEnd,

    /// <summary>Centers items or tracks within the available space.</summary>
    Center,
}

/// <summary>Specifies whether a node participates in normal flow or uses positioned layout.</summary>
public enum Position : byte
{
    /// <summary>Participates in normal flow without positional offsets.</summary>
    Static,

    /// <summary>Participates in normal flow and applies offsets to its visual position.</summary>
    Relative,

    /// <summary>Positions the node outside normal flow relative to its containing block.</summary>
    Absolute,
}

/// <summary>The <c>z-index</c> of an element: <c>auto</c> or an integer.</summary>
/// <param name="IsAuto">The is auto (bool).</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
public readonly record struct ZIndex(bool IsAuto, int Value)
{
    /// <summary>Automatic stacking order within the current stacking context.</summary>
    public static readonly ZIndex Auto = new(true, 0);

    /// <summary>Creates an explicit stacking order from an integer value.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting ZIndex value.</returns>
    public static ZIndex Of(int value) => new(false, value);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => IsAuto ? "auto" : Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Specifies whether declared dimensions include borders and padding.</summary>
public enum BoxSizing : byte
{
    /// <summary>Includes padding and borders in declared width and height.</summary>
    BorderBox,

    /// <summary>Applies declared width and height to content, adding padding and borders outside them.</summary>
    ContentBox,
}

/// <summary>The line style of a border side or of the outline.</summary>
public enum BorderStyle : byte
{
    /// <summary>Does not draw a border line.</summary>
    None,

    /// <summary>Draws a continuous border line.</summary>
    Solid,

    /// <summary>Draws a sequence of border dashes.</summary>
    Dashed,

    /// <summary>Draws a sequence of border dots.</summary>
    Dotted,
}

/// <summary>
/// One shadow of <c>box-shadow</c>: offsets, blur radius (the CSS blur, twice the standard deviation), spread
/// (negative shrinks) and color; <see cref="Inset" /> shadows fall inside the padding box.
/// </summary>
/// <param name="Inset">The inset (bool).</param>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
/// <param name="Blur">The blur (Length).</param>
/// <param name="Spread">The spread (Length).</param>
/// <param name="Color">The color (Color).</param>
public sealed record BoxShadow(bool Inset, Length X, Length Y, Length Blur, Length Spread, Color Color)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{(Inset ? "inset " : "")}{X} {Y} {Blur} {Spread} {Color}";
}

/// <summary>
/// The radius of one corner: <paramref name="X" /> horizontal, <paramref name="Y" /> vertical, lengths or percentages
/// of the border box.
/// </summary>
/// <param name="X">The horizontal component of the value.</param>
/// <param name="Y">The vertical component of the value.</param>
public readonly record struct RadiusValue(Length X, Length Y)
{
    /// <summary>The radius value with all components set to zero.</summary>
    public static readonly RadiusValue Zero = new(Length.Zero, Length.Zero);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => X == Y ? X.ToString() : $"{X} {Y}";
}

/// <summary>Specifies clipping and scrolling behavior when content exceeds its box.</summary>
public enum Overflow : byte
{
    /// <summary>Paints overflowing content outside the box.</summary>
    Visible,

    /// <summary>Clips overflow without exposing scrolling behavior.</summary>
    Hidden,

    /// <summary>Clips overflow and enables scrolling.</summary>
    Scroll,

    /// <summary>Enables scrolling when content overflows.</summary>
    Auto,
}

/// <summary>Specifies normal or italic font selection.</summary>
public enum FontStyle : byte
{
    /// <summary>Selects an upright font face.</summary>
    Normal,

    /// <summary>Selects an italic font face.</summary>
    Italic,
}

/// <summary>Specifies horizontal text alignment within a line box.</summary>
public enum TextAlign : byte
{
    /// <summary>Aligns text to the start edge of its line box.</summary>
    Start,

    /// <summary>Aligns text to the end edge of its line box.</summary>
    End,

    /// <summary>Aligns text to the left edge of its line box.</summary>
    Left,

    /// <summary>Aligns text to the right edge of its line box.</summary>
    Right,

    /// <summary>Centers text within its line box.</summary>
    Center,
}

/// <summary>Specifies whitespace collapsing and text wrapping behavior.</summary>
public enum WhiteSpace : byte
{
    /// <summary>Collapses whitespace and wraps text as needed.</summary>
    Normal,

    /// <summary>Collapses whitespace and disables line wrapping.</summary>
    NoWrap,

    /// <summary>Preserves whitespace and explicit line breaks without wrapping.</summary>
    Pre,

    /// <summary>Preserves whitespace and explicit line breaks and permits wrapping.</summary>
    PreWrap,

    /// <summary>Collapses spaces, preserves explicit line breaks, and permits wrapping.</summary>
    PreLine,
}

/// <summary>The lines of <c>text-decoration-line</c>; any combination.</summary>
[Flags]
public enum TextDecorationLine : byte
{
    /// <summary>Draws no text decoration.</summary>
    None = 0,

    /// <summary>Draws a line below the text.</summary>
    Underline = 1,

    /// <summary>Draws a line above the text.</summary>
    Overline = 2,

    /// <summary>Draws a line through the text.</summary>
    LineThrough = 4,
}

/// <summary>Specifies the stroke pattern for a text decoration.</summary>
public enum TextDecorationStyle : byte
{
    /// <summary>Draws a continuous decoration line.</summary>
    Solid,

    /// <summary>Draws two parallel decoration lines.</summary>
    Double,

    /// <summary>Draws the decoration as dots.</summary>
    Dotted,

    /// <summary>Draws the decoration as dashes.</summary>
    Dashed,

    /// <summary>Draws the decoration as a wavy line.</summary>
    Wavy,
}

/// <summary>Specifies how clipped text indicates horizontal overflow.</summary>
public enum TextOverflow : byte
{
    /// <summary>Clips text at the available width.</summary>
    Clip,

    /// <summary>Adds an ellipsis when text exceeds the available width.</summary>
    Ellipsis,
}

/// <summary>Specifies when words may be broken to prevent text overflow.</summary>
public enum OverflowWrap : byte
{
    /// <summary>Breaks text only at normal wrapping opportunities.</summary>
    Normal,

    /// <summary>
    /// Allows an otherwise overflowing word to break, without reducing its intrinsic minimum width.
    /// </summary>
    BreakWord,

    /// <summary>
    /// Allows a word to break at any character boundary and includes those opportunities in intrinsic sizing.
    /// </summary>
    Anywhere,
}

/// <summary>Specifies the case transformation applied to displayed text.</summary>
public enum TextTransform : byte
{
    /// <summary>Preserves the source text's case.</summary>
    None,

    /// <summary>Displays the text in uppercase.</summary>
    Uppercase,

    /// <summary>Displays the text in lowercase.</summary>
    Lowercase,

    /// <summary>Capitalizes the start of each word.</summary>
    Capitalize,
}

/// <summary>Specifies the pointer cursor requested by an element.</summary>
public enum Cursor : byte
{
    /// <summary>Displays a cursor indicating the default arrow.</summary>
    Default,

    /// <summary>Displays a cursor indicating a link-selection hand.</summary>
    Pointer,

    /// <summary>Displays a cursor indicating a text-selection I-beam.</summary>
    Text,

    /// <summary>Displays a cursor indicating horizontal resizing.</summary>
    ResizeHorizontal,

    /// <summary>Displays a cursor indicating vertical resizing.</summary>
    ResizeVertical,

    /// <summary>Displays a cursor indicating movement in all directions.</summary>
    Move,

    /// <summary>Displays a cursor indicating an unavailable operation.</summary>
    NotAllowed,

    /// <summary>Displays a cursor indicating a busy operation.</summary>
    Wait,

    /// <summary>Displays a cursor indicating background progress.</summary>
    Progress,

    /// <summary>Displays a cursor indicating contextual help.</summary>
    Help,

    /// <summary>Displays a cursor indicating a precision crosshair.</summary>
    Crosshair,

    /// <summary>Displays a cursor indicating diagonal resizing from the top-left or bottom-right corner.</summary>
    ResizeNorthwestSoutheast,

    /// <summary>Displays a cursor indicating diagonal resizing from the top-right or bottom-left corner.</summary>
    ResizeNortheastSouthwest,
}

/// <summary>Specifies whether an element participates in pointer hit testing.</summary>
public enum PointerEvents : byte
{
    /// <summary>Allows normal pointer hit testing.</summary>
    Auto,

    /// <summary>Excludes the element from pointer targeting.</summary>
    None,
}

/// <summary>Specifies whether and how users can select the element's text.</summary>
public enum UserSelect : byte
{
    /// <summary>Uses the control-specific selection behavior.</summary>
    Auto,

    /// <summary>Disables user text selection.</summary>
    None,

    /// <summary>Allows selecting text ranges.</summary>
    Text,

    /// <summary>Selects the entire text as a unit.</summary>
    All,
}

/// <summary>Specifies whether an element is painted while retaining its layout space.</summary>
public enum Visibility : byte
{
    /// <summary>Paints the element normally.</summary>
    Visible,

    /// <summary>Suppresses painting while preserving the layout space.</summary>
    Hidden,
}

/// <summary>Specifies the available line height kind values.</summary>
public enum LineHeightKind : byte
{
    /// <summary>Uses the font's normal line metrics.</summary>
    Normal,

    /// <summary>Uses a unitless multiplier of the font size.</summary>
    Number,

    /// <summary>Uses a length resolved to a logical line height.</summary>
    Length,
}

/// <summary><c>normal</c>, a multiplier of the font size, or a length.</summary>
/// <param name="Kind">The line height kind represented by this value.</param>
/// <param name="Number">The number (double).</param>
/// <param name="Length">The absolute, relative, or percentage length to use.</param>
public readonly record struct LineHeight(LineHeightKind Kind, double Number, Length Length)
{
    /// <summary>The normal line height determined by font metrics.</summary>
    public static readonly LineHeight Normal = new(LineHeightKind.Normal, 0, Length.Zero);

    /// <summary>Creates a unitless line-height multiplier relative to the font size.</summary>
    /// <param name="value">The numeric magnitude in the unit selected by this factory method.</param>
    /// <returns>The resulting LineHeight value.</returns>
    public static LineHeight Multiplier(double value) => new(LineHeightKind.Number, value, Length.Zero);

    /// <summary>Creates a line height from an absolute or percentage length.</summary>
    /// <param name="length">The absolute, relative, or percentage length to use.</param>
    /// <returns>The resulting LineHeight value.</returns>
    public static LineHeight Fixed(Length length) => new(LineHeightKind.Length, 0, length);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Kind switch
    {
        LineHeightKind.Normal => "normal",
        LineHeightKind.Number => Number.ToString("0.###", CultureInfo.InvariantCulture),
        _ => Length.ToString(),
    };
}

/// <summary>A cubic Bézier timing function; the CSS keywords are presets.</summary>
/// <param name="X1">The x1 (double).</param>
/// <param name="Y1">The y1 (double).</param>
/// <param name="X2">The x2 (double).</param>
/// <param name="Y2">The y2 (double).</param>
public sealed record EasingFunction(double X1, double Y1, double X2, double Y2)
{
    /// <summary>A timing function with constant progress.</summary>
    public static readonly EasingFunction Linear = new(0, 0, 1, 1);

    /// <summary>The CSS ease cubic Bezier timing function.</summary>
    public static readonly EasingFunction Ease = new(0.25, 0.1, 0.25, 1);

    /// <summary>The CSS ease-in cubic Bezier timing function.</summary>
    public static readonly EasingFunction EaseIn = new(0.42, 0, 1, 1);

    /// <summary>The CSS ease-out cubic Bezier timing function.</summary>
    public static readonly EasingFunction EaseOut = new(0, 0, 0.58, 1);

    /// <summary>The CSS ease-in-out cubic Bezier timing function.</summary>
    public static readonly EasingFunction EaseInOut = new(0.42, 0, 0.58, 1);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() =>
        this == Linear ? "linear" : this == Ease ? "ease" : this == EaseIn ? "ease-in" : this == EaseOut ? "ease-out" : this == EaseInOut ? "ease-in-out" :
        string.Create(CultureInfo.InvariantCulture, $"cubic-bezier({X1}, {Y1}, {X2}, {Y2})");
}

/// <summary>One entry of the <c>transition</c> list; <see cref="Property" /> is a property name or "all".</summary>
/// <param name="Property">The style property targeted by the value or declaration.</param>
/// <param name="Duration">The duration of the animation or transition.</param>
/// <param name="Easing">The easing (EasingFunction).</param>
/// <param name="Delay">The delay before the operation or animation starts.</param>
public sealed record TransitionDefinition(string Property, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay)
{
    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Property} {Duration.TotalMilliseconds}ms {Easing} {Delay.TotalMilliseconds}ms");
}

/// <summary>Exterior window frame shadows, or the platform default when unspecified.</summary>
/// <param name="Shadows">The explicit list, including an empty list for no shadow, or null for the default.</param>
public readonly record struct WindowFrameShadows(ValueList<BoxShadow>? Shadows)
{
    /// <summary>Returns the NSS representation of the frame shadow value.</summary>
    public override string ToString() => Shadows?.ToString() ?? "auto";
}

/// <summary>An immutable list with value equality, used for list-valued properties.</summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class ValueList<T> : IEquatable<ValueList<T>>
{
    /// <summary>An empty value list value.</summary>
    public static readonly ValueList<T> Empty = new([]);

    /// <summary>Initializes a new instance of ValueList.</summary>
    /// <param name="items">The items to store in the immutable value list.</param>
    public ValueList(IEnumerable<T> items) => Items = items.ToArray();

    /// <summary>Gets the ordered values contained in this immutable list.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(ValueList<T>? other) => other is not null && Items.SequenceEqual(other.Items);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is ValueList<T> other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items) hash.Add(item);
        return hash.ToHashCode();
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Items.Count == 0 ? "none" : string.Join(", ", Items);
}
