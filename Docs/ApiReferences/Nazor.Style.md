# Nazor.Style

NSS properties, tokenization, selectors, style sheets, computed values, media queries, and style invalidation.

This runtime package supports .NET 8 or later. NSS compilation is performed by the separate .NET 10 LTS build tool bundled in Nazor.Sdk; see [build tools and application targets](../Guide/03-Projects-and-Builds.md#build-tools-and-application-targets).

This reference covers the public and protected API declared by this package. Inherited members follow the contract of their declaring type. Positional record parameters also define public properties with the same names.

## Types

- [`ComponentRootSelector`](#type-componentrootselector)

- [`ItemPositionSelector`](#type-itempositionselector)

- [`PartSelector`](#type-partselector)

- [`StylePartSet`](#type-stylepartset)

- [`NamedStateSelector`](#type-namedstateselector)



- [`RelativeSelector`](#type-relativeselector)

- [`HasSelector`](#type-hasselector)

- [`NativeColor`](#type-nativecolor)

- [`AlignContent`](#type-aligncontent)
- [`AlignItems`](#type-alignitems)
- [`AlignSelf`](#type-alignself)
- [`AnimationDefinition`](#type-animationdefinition)
- [`AnimationDirection`](#type-animationdirection)
- [`AnimationFillMode`](#type-animationfillmode)
- [`AspectRatio`](#type-aspectratio)
- [`AttributeMatch`](#type-attributematch)
- [`AttributeSelector`](#type-attributeselector)
- [`BackgroundRepeat`](#type-backgroundrepeat)
- [`BackgroundSize`](#type-backgroundsize)
- [`BackgroundSizeKind`](#type-backgroundsizekind)
- [`BorderStyle`](#type-borderstyle)
- [`BoxShadow`](#type-boxshadow)
- [`BoxSizing`](#type-boxsizing)
- [`CalcExpression`](#type-calcexpression)
- [`CalcFunction`](#type-calcfunction)
- [`CalcFunctionKind`](#type-calcfunctionkind)
- [`CalcLength`](#type-calclength)
- [`CalcNode`](#type-calcnode)
- [`CalcNumber`](#type-calcnumber)
- [`CalcOperation`](#type-calcoperation)
- [`CalcOperator`](#type-calcoperator)
- [`ClipPathValue`](#type-clippathvalue)
- [`Color`](#type-color)
- [`ColorScheme`](#type-colorscheme)
- [`ColorStop`](#type-colorstop)
- [`Combinator`](#type-combinator)
- [`CompoundSelector`](#type-compoundselector)
- [`ComputedStyle`](#type-computedstyle)
- [`Cursor`](#type-cursor)
- [`CustomPropertyDeclaration`](#type-custompropertydeclaration)
- [`Display`](#type-display)
- [`EasingFunction`](#type-easingfunction)
- [`EmptySelector`](#type-emptyselector)
- [`FlexDirection`](#type-flexdirection)
- [`FlexWrap`](#type-flexwrap)
- [`FontStyle`](#type-fontstyle)
- [`GlobalKeyword`](#type-globalkeyword)
- [`Gradient`](#type-gradient)
- [`GridArea`](#type-gridarea)
- [`GridAreas`](#type-gridareas)
- [`GridAutoFlow`](#type-gridautoflow)
- [`GridAutoRepeat`](#type-gridautorepeat)
- [`GridLine`](#type-gridline)
- [`GridLineKind`](#type-gridlinekind)
- [`GridTrackList`](#type-gridtracklist)
- [`ImageValue`](#type-imagevalue)
- [`InlineStyle`](#type-inlinestyle)
- [`JustifyContent`](#type-justifycontent)
- [`Keyframe`](#type-keyframe)
- [`KeyframesRule`](#type-keyframesrule)
- [`KeywordDeclaration`](#type-keyworddeclaration)
- [`Length`](#type-length)
- [`LengthUnit`](#type-lengthunit)
- [`LinearGradient`](#type-lineargradient)
- [`LineHeight`](#type-lineheight)
- [`LineHeightKind`](#type-lineheightkind)
- [`LogicalPseudo`](#type-logicalpseudo)
- [`LogicalSelector`](#type-logicalselector)
- [`MatrixFunction`](#type-matrixfunction)
- [`MediaCondition`](#type-mediacondition)
- [`MediaPreferences`](#type-mediapreferences)
- [`MediaQuery`](#type-mediaquery)
- [`MixFunction`](#type-mixfunction)
- [`NthSelector`](#type-nthselector)
- [`ObjectFit`](#type-objectfit)
- [`OnlyChildSelector`](#type-onlychildselector)
- [`Overflow`](#type-overflow)
- [`OverflowWrap`](#type-overflowwrap)
- [`PendingDeclaration`](#type-pendingdeclaration)
- [`PointerEvents`](#type-pointerevents)
- [`Position`](#type-position)
- [`PositionValue`](#type-positionvalue)
- [`PseudoClasses`](#type-pseudoclasses)
- [`PseudoSelector`](#type-pseudoselector)
- [`RadialExtent`](#type-radialextent)
- [`RadialGradient`](#type-radialgradient)
- [`RadialShape`](#type-radialshape)
- [`RadiusValue`](#type-radiusvalue)
- [`ResolvedKeyframe`](#type-resolvedkeyframe)
- [`RootSelector`](#type-rootselector)
- [`RotateFunction`](#type-rotatefunction)
- [`ScaleFunction`](#type-scalefunction)
- [`Selector`](#type-selector)
- [`ShorthandProperty`](#type-shorthandproperty)
- [`SizeKind`](#type-sizekind)
- [`SizeValue`](#type-sizevalue)
- [`SkewFunction`](#type-skewfunction)
- [`Specificity`](#type-specificity)
- [`StyleDeclaration`](#type-styledeclaration)
- [`StyleEffects`](#type-styleeffects)
- [`StyleEngine`](#type-styleengine)
- [`StyleHotReload`](#type-stylehotreload)
- [`StyleLevel`](#type-stylelevel)
- [`StyleProperties`](#type-styleproperties)
- [`ScrollbarVisibility`](#type-scrollbarvisibility)
- [`ScrollbarPlacement`](#type-scrollbarplacement)
- [`StyleProperty`](#type-styleproperty)
- [`StyleProperty<T>`](#type-stylepropertyt)
- [`StyleRegistry`](#type-styleregistry)
- [`StyleRule`](#type-stylerule)
- [`StyleSheet`](#type-stylesheet)
- [`StyleSheetJson`](#type-stylesheetjson)
- [`StyleToken`](#type-styletoken)
- [`StyleTokenizer`](#type-styletokenizer)
- [`StyleTokenKind`](#type-styletokenkind)
- [`StyleTrace`](#type-styletrace)
- [`StyleValueException`](#type-stylevalueexception)
- [`SystemColors`](#type-systemcolors)
- [`TextAlign`](#type-textalign)
- [`TextDecorationLine`](#type-textdecorationline)
- [`TextDecorationStyle`](#type-textdecorationstyle)
- [`TextOverflow`](#type-textoverflow)
- [`TextTransform`](#type-texttransform)
- [`TokenReader`](#type-tokenreader)
- [`TracedDeclaration`](#type-traceddeclaration)
- [`TracedRule`](#type-tracedrule)
- [`TracedValue`](#type-tracedvalue)
- [`TrackBreadth`](#type-trackbreadth)
- [`TrackBreadthKind`](#type-trackbreadthkind)
- [`TrackSize`](#type-tracksize)
- [`TransformFunction`](#type-transformfunction)
- [`TransformOrigin`](#type-transformorigin)
- [`TransitionDefinition`](#type-transitiondefinition)
- [`TranslateFunction`](#type-translatefunction)
- [`UrlImage`](#type-urlimage)
- [`UserSelect`](#type-userselect)
- [`ValueDeclaration`](#type-valuedeclaration)
- [`ValueList<T>`](#type-valuelistt)
- [`ValueParsers`](#type-valueparsers)
- [`ValueSource`](#type-valuesource)
- [`Viewport`](#type-viewport)
- [`Visibility`](#type-visibility)
- [`WhiteSpace`](#type-whitespace)
- [`ZIndex`](#type-zindex)

<a id="type-aligncontent"></a>

## `AlignContent`

Namespace: `Nazor.Style`

```csharp
public enum AlignContent : byte
```

Distribution of the lines of a multi-line flex container on the cross axis.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Uses the layout mode's normal alignment behavior.

#### `Stretch`

```csharp
Stretch
```

Stretches items or tracks to fill available space.

#### `FlexStart`

```csharp
FlexStart
```

Aligns items or tracks at the start of the applicable axis.

#### `FlexEnd`

```csharp
FlexEnd
```

Aligns items or tracks at the end of the applicable axis.

#### `Center`

```csharp
Center
```

Centers items or tracks within the available space.

#### `SpaceBetween`

```csharp
SpaceBetween
```

Distributes free space between items or tracks.

#### `SpaceAround`

```csharp
SpaceAround
```

Distributes free space around items or tracks, with half-sized outer gaps.

#### `SpaceEvenly`

```csharp
SpaceEvenly
```

Distributes free space into equal inner and outer gaps.


<a id="type-alignitems"></a>

## `AlignItems`

Namespace: `Nazor.Style`

```csharp
public enum AlignItems : byte
```

Specifies the default cross-axis alignment of child items.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Stretch`

```csharp
Stretch
```

Stretches items or tracks to fill available space.

#### `FlexStart`

```csharp
FlexStart
```

Aligns items or tracks at the start of the applicable axis.

#### `FlexEnd`

```csharp
FlexEnd
```

Aligns items or tracks at the end of the applicable axis.

#### `Center`

```csharp
Center
```

Centers items or tracks within the available space.


<a id="type-alignself"></a>

## `AlignSelf`

Namespace: `Nazor.Style`

```csharp
public enum AlignSelf : byte
```

Specifies a child item's cross-axis alignment override.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
Auto
```

Uses the parent alignment setting.

#### `Stretch`

```csharp
Stretch
```

Stretches items or tracks to fill available space.

#### `FlexStart`

```csharp
FlexStart
```

Aligns items or tracks at the start of the applicable axis.

#### `FlexEnd`

```csharp
FlexEnd
```

Aligns items or tracks at the end of the applicable axis.

#### `Center`

```csharp
Center
```

Centers items or tracks within the available space.


<a id="type-animationdefinition"></a>

## `AnimationDefinition`

Namespace: `Nazor.Style`

```csharp
public sealed record AnimationDefinition(string? Name, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay, double IterationCount, AnimationDirection Direction, AnimationFillMode FillMode, bool Paused)
```

One entry of the `animation` list: the name of a `@keyframes` rule (null for `none`), duration, timing function, delay (negative starts midway), iterations (`double.PositiveInfinity` for `infinite`), direction, fill mode and play state.

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `Duration` | The duration of the animation or transition. |
| `Easing` | The easing (EasingFunction). |
| `Delay` | The delay before the operation or animation starts. |
| `IterationCount` | The iteration count (double). |
| `Direction` | The direction (AnimationDirection). |
| `FillMode` | The fill mode (AnimationFillMode). |
| `Paused` | The paused (bool). |

[Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-animationdirection"></a>

## `AnimationDirection`

Namespace: `Nazor.Style`

```csharp
public enum AnimationDirection : byte
```

Specifies playback order and whether successive animation iterations alternate.

[Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

### Members

#### `Normal`

```csharp
Normal
```

Plays every iteration from the first keyframe to the last.

#### `Reverse`

```csharp
Reverse
```

Plays every iteration from the last keyframe to the first.

#### `Alternate`

```csharp
Alternate
```

Alternates direction, beginning with forward playback.

#### `AlternateReverse`

```csharp
AlternateReverse
```

Alternates direction, beginning with reverse playback.


<a id="type-animationfillmode"></a>

## `AnimationFillMode`

Namespace: `Nazor.Style`

```csharp
public enum AnimationFillMode : byte
```

Specifies whether animation values apply before or after the active interval.

[Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

### Members

#### `None`

```csharp
None
```

Applies animation values only during the active interval.

#### `Forwards`

```csharp
Forwards
```

Retains the ending animation value after the active interval.

#### `Backwards`

```csharp
Backwards
```

Applies the starting animation value during the delay.

#### `Both`

```csharp
Both
```

Applies both backwards and forwards fill behavior.


<a id="type-aspectratio"></a>

## `AspectRatio`

Namespace: `Nazor.Style`

```csharp
public readonly record struct AspectRatio(double Ratio)
```

The preferred aspect ratio of a box, width divided by height; `Auto` is none.

| Parameter | Description |
| --- | --- |
| `Ratio` | The preferred width divided by height; a nonpositive value represents automatic sizing. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
public static readonly AspectRatio Auto ;
```

No explicit preferred aspect ratio.

#### `IsAuto`

```csharp
public bool IsAuto {get; }
```

Gets whether no positive preferred width-to-height ratio was supplied.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-attributematch"></a>

## `AttributeMatch`

Namespace: `Nazor.Style`

```csharp
public enum AttributeMatch : byte
```

Specifies how an attribute selector compares the element's attribute value.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Exists`

```csharp
Exists
```

Requires the attribute to be present.

#### `Equals`

```csharp
Equals
```

Requires an exact attribute-value match.

#### `Includes`

```csharp
Includes
```

Requires a whitespace-separated word to match.

#### `DashMatch`

```csharp
DashMatch
```

Matches the value exactly or as a prefix followed by a hyphen.

#### `Prefix`

```csharp
Prefix
```

Requires the attribute value to begin with the selector value.

#### `Suffix`

```csharp
Suffix
```

Requires the attribute value to end with the selector value.

#### `Substring`

```csharp
Substring
```

Requires the attribute value to contain the selector value.


<a id="type-attributeselector"></a>

## `AttributeSelector`

Namespace: `Nazor.Style`

```csharp
public sealed record AttributeSelector(string Name, AttributeMatch Match, string? Value, bool IgnoreCase = false)
```

An attribute selector. The attribute name ignores case, like HTML; an attribute set to null or `false` is absent (`[disabled]` matches a disabled control); other values compare as their invariant text, with case unless `IgnoreCase` (the `i` flag).

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `Match` | The match (AttributeMatch). |
| `Value` | The numeric value or payload represented by this record. |
| `IgnoreCase` | The ignore case (bool). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-backgroundrepeat"></a>

## `BackgroundRepeat`

Namespace: `Nazor.Style`

```csharp
public enum BackgroundRepeat : byte
```

How a background image repeats in its box.

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `Repeat`

```csharp
Repeat
```

Tiles the background image along both axes.

#### `NoRepeat`

```csharp
NoRepeat
```

Draws one background image without tiling.

#### `RepeatX`

```csharp
RepeatX
```

Tiles the background image horizontally.

#### `RepeatY`

```csharp
RepeatY
```

Tiles the background image vertically.


<a id="type-backgroundsize"></a>

## `BackgroundSize`

Namespace: `Nazor.Style`

```csharp
public readonly record struct BackgroundSize(BackgroundSizeKind Kind, SizeValue Width, SizeValue Height)
```

A value of `background-size`: `cover`, `contain` or a width and a height (auto keeps the proportions).

| Parameter | Description |
| --- | --- |
| `Kind` | The background size kind represented by this value. |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `Auto`

```csharp
public static readonly BackgroundSize Auto ;
```

Intrinsic background-image sizing, preserving the image's aspect ratio.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-backgroundsizekind"></a>

## `BackgroundSizeKind`

Namespace: `Nazor.Style`

```csharp
public enum BackgroundSizeKind : byte
```

Specifies explicit image dimensions or aspect-preserving cover and contain sizing.

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `Explicit`

```csharp
Explicit
```

Uses the declared horizontal and vertical image dimensions.

#### `Cover`

```csharp
Cover
```

Scales the image proportionally until it covers the painting box.

#### `Contain`

```csharp
Contain
```

Scales the image proportionally to fit entirely inside the painting box.


<a id="type-borderstyle"></a>

## `BorderStyle`

Namespace: `Nazor.Style`

```csharp
public enum BorderStyle : byte
```

The line style of a border side or of the outline.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `None`

```csharp
None
```

Does not draw a border line.

#### `Solid`

```csharp
Solid
```

Draws a continuous border line.

#### `Dashed`

```csharp
Dashed
```

Draws a sequence of border dashes.

#### `Dotted`

```csharp
Dotted
```

Draws a sequence of border dots.


<a id="type-boxshadow"></a>

## `BoxShadow`

Namespace: `Nazor.Style`

```csharp
public sealed record BoxShadow(bool Inset, Length X, Length Y, Length Blur, Length Spread, Color Color)
```

One shadow of `box-shadow`: offsets, blur radius (the CSS blur, twice the standard deviation), spread (negative shrinks) and color; `Inset` shadows fall inside the padding box.

| Parameter | Description |
| --- | --- |
| `Inset` | The inset (bool). |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |
| `Blur` | The blur (Length). |
| `Spread` | The spread (Length). |
| `Color` | The color (Color). |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-boxsizing"></a>

## `BoxSizing`

Namespace: `Nazor.Style`

```csharp
public enum BoxSizing : byte
```

Specifies whether declared dimensions include borders and padding.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `BorderBox`

```csharp
BorderBox
```

Includes padding and borders in declared width and height.

#### `ContentBox`

```csharp
ContentBox
```

Applies declared width and height to content, adding padding and borders outside them.


<a id="type-calcexpression"></a>

## `CalcExpression`

Namespace: `Nazor.Style`

```csharp
public sealed record CalcExpression(CalcNode Root, bool NonNegative)
```

A length computed by an expression. `NonNegative` clamps the result at zero, for properties that do not accept negative lengths. Expressions without relative units are reduced when parsed: `calc(10px + 2 * 5px)` is 20px, `calc(100% - 16px)` stays an expression.

| Parameter | Description |
| --- | --- |
| `Root` | The root (CalcNode). |
| `NonNegative` | The non negative (bool). |

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `HasPercent`

```csharp
public bool HasPercent { get; }
```

The expression depends on the percentage basis.

#### `IsRelative`

```csharp
public bool IsRelative { get; }
```

The expression has em, rem or viewport units, converted by the style engine.

#### `Evaluate`

```csharp
public double Evaluate(double basis)
```

Evaluates the calculation against a reference dimension for percentages.

| Parameter | Description |
| --- | --- |
| `basis` | The reference dimension used to resolve percentage values. |

Returns: The resulting double value.

#### `Map`

```csharp
public Length Map(Func<Length, Length> map)
```

Replaces the leaves (for example em with px), then reduces the expression.

| Parameter | Description |
| --- | --- |
| `map` | The map (Func<Length, Length>). |

Returns: The resulting Length value.

#### `Simplify`

```csharp
public static Length Simplify(CalcNode root, bool nonNegative)
```

A plain length when the expression reduces to px or to a percentage, otherwise an expression.

| Parameter | Description |
| --- | --- |
| `root` | The root (CalcNode). |
| `nonNegative` | The non negative (bool). |

Returns: The resulting Length value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-calcfunction"></a>

## `CalcFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record CalcFunction(CalcFunctionKind Kind, ValueList<CalcNode> Arguments) : CalcNode
```

`min()`, `max()` or `clamp(min, value, max)`.

| Parameter | Description |
| --- | --- |
| `Kind` | The calc function kind represented by this value. |
| `Arguments` | The arguments (ValueList<CalcNode>). |

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-calcfunctionkind"></a>

## `CalcFunctionKind`

Namespace: `Nazor.Style`

```csharp
public enum CalcFunctionKind : byte
```

Identifies the minimum, maximum, or clamp calculation function.

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `Min`

```csharp
Min
```

Selects the smallest evaluated argument.

#### `Max`

```csharp
Max
```

Selects the largest evaluated argument.

#### `Clamp`

```csharp
Clamp
```

Clamps the preferred value between the minimum and maximum bounds.


<a id="type-calclength"></a>

## `CalcLength`

Namespace: `Nazor.Style`

```csharp
public sealed record CalcLength(Length Length) : CalcNode
```

A length leaf: px, a percentage or a relative unit, never another calculation.

| Parameter | Description |
| --- | --- |
| `Length` | The absolute, relative, or percentage length to use. |

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-calcnode"></a>

## `CalcNode`

Namespace: `Nazor.Style`

```csharp
public abstract record CalcNode
```

A node of a `calc()`, `min()`, `max()` or `clamp()` expression (CSS Values §10): a number, a length leaf, an operation or a comparison function.

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)


<a id="type-calcnumber"></a>

## `CalcNumber`

Namespace: `Nazor.Style`

```csharp
public sealed record CalcNumber(double Value) : CalcNode
```

Represents a unitless numeric literal in an NSS calculation.

| Parameter | Description |
| --- | --- |
| `Value` | The numeric value or payload represented by this record. |

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-calcoperation"></a>

## `CalcOperation`

Namespace: `Nazor.Style`

```csharp
public sealed record CalcOperation(CalcOperator Operator, CalcNode Left, CalcNode Right) : CalcNode
```

Represents a binary arithmetic operation between two calculation nodes.

| Parameter | Description |
| --- | --- |
| `Operator` | The operator (CalcOperator). |
| `Left` | The value associated with the left side or operand. |
| `Right` | The value associated with the right side or operand. |

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-calcoperator"></a>

## `CalcOperator`

Namespace: `Nazor.Style`

```csharp
public enum CalcOperator : byte
```

Identifies a binary arithmetic operator in a calculation expression.

[Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

### Members

#### `Add`

```csharp
Add
```

Adds the left and right operands.

#### `Subtract`

```csharp
Subtract
```

Subtracts the right operand from the left.

#### `Multiply`

```csharp
Multiply
```

Multiplies the operands.

#### `Divide`

```csharp
Divide
```

Divides the left operand by the right.


<a id="type-clippathvalue"></a>

## `ClipPathValue`

Namespace: `Nazor.Style`

```csharp
public readonly record struct ClipPathValue(Length Top, Length Right, Length Bottom, Length Left, bool IsNone = false)
```

An optionally rounded NSS inset clip relative to the border box, independent of layout and scrolling. `Top` and `Bottom` percentages use its height; `Right` and `Left` use its width. Negative insets expand the clip. `IsNone` disables clipping. The same animated shape clips painting and pointer hits; it does not change layout bounds or scroll extents. The four init-only `TopLeftRadius`, `TopRightRadius`, `BottomRightRadius`, and `BottomLeftRadius` properties default to `RadiusValue.Zero`. Their horizontal/vertical percentages use the original border-box width/height. Oversized corners scale down together to fit the inset rectangle; a zero radius axis makes that corner square.

[Source](../../Sources/Core/Nazor.Style/Values/ClipPathValue.cs)

### Members

`None` is the static value that disables clipping, including overflowing descendants and shadows. `ToString()` returns `none` or an `inset(...)` expression containing all four lengths and optional `round` corner radii. Insets and both radius axes interpolate component by component; transitions involving `none` change discretely. NSS accepts one to four inset lengths followed by optional `round` with the border-radius shorthand, including a `/` between horizontal and vertical values. Compiled C# and stylesheet JSON preserve every axis. JSON without radius fields remains square. The existing five-argument constructor and five-output deconstruction retain their contracts.

<a id="type-nativecolor"></a>

## `NativeColor`

Namespace: `Nazor.Style`

```csharp
public readonly record struct NativeColor(Color? Value)
```

An explicit color or `auto` for the default selected by the consuming property. Native decoration properties use the operating system color on auto. Scrollbar interaction colors follow the corresponding normal fill. `Value` is nullable, and null means auto. `ToString()` returns the explicit color or `"auto"`. NSS resolves `currentColor`, system color aliases and custom properties. Compiled C# and stylesheet JSON preserve both explicit colors and auto.

[Source](../../Sources/Core/Nazor.Style/Values/NativeColor.cs)

<a id="type-color"></a>

## `Color`

Namespace: `Nazor.Style`

```csharp
public readonly record struct Color(byte R, byte G, byte B, byte A, bool IsCurrentColor = false)
```

An sRGB color with straight (non-premultiplied) alpha, or the `currentColor` keyword.

| Parameter | Description |
| --- | --- |
| `R` | The red channel in the range from zero to 255. |
| `G` | The green channel in the range from zero to 255. |
| `B` | The blue channel in the range from zero to 255. |
| `A` | The alpha channel in the range from zero to 255. |
| `IsCurrentColor` | The is current color (bool). |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Transparent`

```csharp
public static readonly Color Transparent ;
```

Fully transparent black, with all RGBA channels set to zero.

#### `Black`

```csharp
public static readonly Color Black ;
```

Opaque black, with zero RGB channels and full alpha.

#### `White`

```csharp
public static readonly Color White ;
```

Opaque white, with all RGBA channels set to 255.

#### `CurrentColor`

```csharp
public static readonly Color CurrentColor ;
```

Placeholder resolved to the element's `color` when styles are computed.

#### `FromRgb`

```csharp
public static Color FromRgb(byte r, byte g, byte b)
```

Creates an opaque color from red, green, and blue channels.

| Parameter | Description |
| --- | --- |
| `r` | The red channel in the range from zero to 255. |
| `g` | The green channel in the range from zero to 255. |
| `b` | The blue channel in the range from zero to 255. |

Returns: The resulting Color value.

#### `ToRgba`

```csharp
public uint ToRgba()
```

Packs as 0xRRGGBBAA, the CSS hexadecimal order.

Returns: The resulting uint value.

#### `FromRgba`

```csharp
public static Color FromRgba(uint rgba)
```

Creates a color from a packed 32-bit RGBA value, with red in the most significant byte.

| Parameter | Description |
| --- | --- |
| `rgba` | The packed color in 0xRRGGBBAA byte order. |

Returns: The resulting Color value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-colorscheme"></a>

## `ColorScheme`

Namespace: `Nazor.Style`

```csharp
public enum ColorScheme : byte
```

Specifies an explicit light or dark color scheme, or follows the system preference.

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Light`

```csharp
Light
```

Uses a light color scheme.

#### `Dark`

```csharp
Dark
```

Uses a dark color scheme.

#### `System`

```csharp
System
```

Follows the platform color-scheme preference.


<a id="type-colorstop"></a>

## `ColorStop`

Namespace: `Nazor.Style`

```csharp
public sealed record ColorStop(Color Color, Length? Position)
```

A color stop of a gradient; `Position` null is placed by the CSS fix-up rules.

| Parameter | Description |
| --- | --- |
| `Color` | The color (Color). |
| `Position` | The position (Length?). |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-combinator"></a>

## `Combinator`

Namespace: `Nazor.Style`

```csharp
public enum Combinator : byte
```

Specifies the tree relationship between adjacent compound selectors.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Descendant`

```csharp
Descendant
```

Matches a descendant within the current component scope.

#### `Child`

```csharp
Child
```

Matches an immediate child.

#### `NextSibling`

```csharp
NextSibling
```

Matches the immediately following sibling.

#### `SubsequentSibling`

```csharp
SubsequentSibling
```

Matches any following sibling.

#### `Deep`

```csharp
Deep
```

Matches descendants across component scope boundaries.


<a id="type-compoundselector"></a>

## `CompoundSelector`

Namespace: `Nazor.Style`

```csharp
public sealed class CompoundSelector
```

Conditions on a single element: type, identifier, classes, interaction states, attributes and other pseudo-classes.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `CompoundSelector`

```csharp
public CompoundSelector(string? type, string? id, IReadOnlyList<string> classes, ElementStates states)
```

Initializes a new instance of CompoundSelector.

| Parameter | Description |
| --- | --- |
| `type` | The type (string?). |
| `id` | The diagnostic identifier of the node, or null to clear the current target. |
| `classes` | The class names required by the compound selector. |
| `states` | The element interaction state flags required by the selector. |

#### `CompoundSelector`

```csharp
public CompoundSelector(string? type, string? id, IReadOnlyList<string> classes, ElementStates states, IReadOnlyList<AttributeSelector> attributes, IReadOnlyList<PseudoSelector> pseudos, PseudoElement pseudoElement = PseudoElement.None)
```

Initializes a new instance of CompoundSelector.

| Parameter | Description |
| --- | --- |
| `type` | The type (string?). |
| `id` | The diagnostic identifier of the node, or null to clear the current target. |
| `classes` | The class names required by the compound selector. |
| `states` | The element interaction state flags required by the selector. |
| `attributes` | The attributes or selector constraints to apply. |
| `pseudos` | The structural or logical pseudo-selectors to match. |

#### `Type`

```csharp
public string? Type { get; }
```

Element name, or null for the universal selector.

#### `Id`

```csharp
public string? Id { get; }
```

Gets the required element identifier, or null when no identifier constraint is present.

#### `Classes`

```csharp
public IReadOnlyList<string> Classes { get; }
```

Gets the class names used by the element or selector.

#### `States`

```csharp
public ElementStates States { get; }
```

States that must all be set (pseudo-classes such as `:hover`).

#### `Attributes`

```csharp
public IReadOnlyList<AttributeSelector> Attributes { get; }
```

Gets the element attributes or attribute-selector constraints.

#### `Pseudos`

```csharp
public IReadOnlyList<PseudoSelector> Pseudos { get; }
```

Structural and logical pseudo-classes (`:first-child`, `:not()`…).

#### `PseudoElement`

```csharp
public PseudoElement PseudoElement { get; }
```

Terminal pseudo-element target, or `PseudoElement.None` for an ordinary element selector. It contributes one type specificity unit. See [pseudo-elements](../Guide/17-Selectors-and-Media-Queries.md#pseudo-elements).

#### `Specificity`

```csharp
public Specificity Specificity {get; }
```

Gets the selector's identifier, class, and type precedence counts.

#### `Matches`

```csharp
public bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-computedstyle"></a>

## `ComputedStyle`

Namespace: `Nazor.Style`

```csharp
public sealed class ComputedStyle
```

The computed values of an element: one value per property in `StyleProperties.All`, plus the inherited custom properties (already substituted). Percentages are kept, because their basis is known only to layout; `currentColor` is resolved, and so are em, rem and viewport units.

[Source](../../Sources/Core/Nazor.Style/Engine/ComputedStyle.cs)

### Members

#### `Initial`

```csharp
public static ComputedStyle Initial { get; }
```

Every property at its initial value (currentColor resolved to the initial color), with no custom properties.

#### `CustomProperties`

```csharp
public IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> CustomProperties { get; }
```

Gets the cascaded custom-property token sequences indexed by their names.

#### `Get`

```csharp
public T Get<T>(StyleProperty<T> property) where T : notnull
```

Gets the computed value for the specified style property.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |

Returns: The computed property value in its declared CLR representation.

#### `Get`

```csharp
public object Get(StyleProperty property)
```

Gets the computed value for the specified style property.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |

Returns: The computed property value in its declared CLR representation.

#### `Display`

```csharp
public Display Display {get; }
```

Gets the computed layout display mode.

#### `Color`

```csharp
public Color Color {get; }
```

Gets the computed foreground color.

#### `BackgroundColor`

```csharp
public Color BackgroundColor {get; }
```

Gets the computed background color.

#### `Opacity`

```csharp
public double Opacity {get; }
```

Gets the computed opacity multiplier.

#### `FontSize`

```csharp
public double FontSize {get; }
```

The computed font size in px, the basis of em.

#### `RootFontSize`

```csharp
public double RootFontSize { get; }
```

The font size of the root element in px, the basis of rem.

#### `Compare`

```csharp
public StyleEffects Compare(ComputedStyle other)
```

Union of the effects of the properties whose values differ.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: The resulting StyleEffects value.

#### `Describe`

```csharp
public string Describe()
```

Values that differ from the initial ones, in registry order, then custom properties; for tests and diagnostics.

Returns: The resulting string value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-cursor"></a>

## `Cursor`

Namespace: `Nazor.Style`

```csharp
public enum Cursor : byte
```

Specifies the pointer cursor requested by an element.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Default`

```csharp
Default
```

Displays a cursor indicating the default arrow.

#### `Pointer`

```csharp
Pointer
```

Displays a cursor indicating a link-selection hand.

#### `Text`

```csharp
Text
```

Displays a cursor indicating a text-selection I-beam.

#### `ResizeHorizontal`

```csharp
ResizeHorizontal
```

Displays a cursor indicating horizontal resizing.

#### `ResizeVertical`

```csharp
ResizeVertical
```

Displays a cursor indicating vertical resizing.

#### `Move`

```csharp
Move
```

Displays a cursor indicating movement in all directions.

#### `NotAllowed`

```csharp
NotAllowed
```

Displays a cursor indicating an unavailable operation.

#### `Wait`

```csharp
Wait
```

Displays a cursor indicating a busy operation.

#### `Progress`

```csharp
Progress
```

Displays a cursor indicating background progress.

#### `Help`

```csharp
Help
```

Displays a cursor indicating contextual help.

#### `Crosshair`

```csharp
Crosshair
```

Displays a cursor indicating a precision crosshair.


<a id="type-custompropertydeclaration"></a>

## `CustomPropertyDeclaration`

Namespace: `Nazor.Style`

```csharp
public sealed class CustomPropertyDeclaration(string name, IReadOnlyList<StyleToken> tokens) : StyleDeclaration
```

A custom property such as `--surface`: inherited token list, possibly referencing others.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `tokens` | The NSS lexical token sequence. |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Name`

```csharp
public string Name { get; }
```

Gets the case-sensitive custom-property name, including its leading --.

#### `Tokens`

```csharp
public IReadOnlyList<StyleToken> Tokens { get; }
```

Gets the NSS token sequence represented by this declaration or reader.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-display"></a>

## `Display`

Namespace: `Nazor.Style`

```csharp
public enum Display : byte
```

Specifies block, inline text, flex, grid, or suppressed layout.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Block`

```csharp
Block
```

Uses block layout.

#### `Flex`

```csharp
Flex
```

Uses flex layout for child items.

#### `Grid`

```csharp
Grid
```

Uses grid layout for child items.

#### `None`

```csharp
None
```

Omits the element and its descendants from layout and painting.


#### `Inline`

```csharp
Inline
```

Participates in a parent text flow with shared wrapping and baseline placement. Flex/grid children and mixed block/inline containers retain ordinary layout. This appended value is 4.


<a id="type-easingfunction"></a>

## `EasingFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record EasingFunction(double X1, double Y1, double X2, double Y2)
```

A cubic Bézier timing function; the CSS keywords are presets.

| Parameter | Description |
| --- | --- |
| `X1` | The x1 (double). |
| `Y1` | The y1 (double). |
| `X2` | The x2 (double). |
| `Y2` | The y2 (double). |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Linear`

```csharp
public static readonly EasingFunction Linear ;
```

A timing function with constant progress.

#### `Ease`

```csharp
public static readonly EasingFunction Ease ;
```

The CSS ease cubic Bezier timing function.

#### `EaseIn`

```csharp
public static readonly EasingFunction EaseIn ;
```

The CSS ease-in cubic Bezier timing function.

#### `EaseOut`

```csharp
public static readonly EasingFunction EaseOut ;
```

The CSS ease-out cubic Bezier timing function.

#### `EaseInOut`

```csharp
public static readonly EasingFunction EaseInOut ;
```

The CSS ease-in-out cubic Bezier timing function.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-emptyselector"></a>

## `EmptySelector`

Namespace: `Nazor.Style`

```csharp
public sealed record EmptySelector : PseudoSelector
```

`:empty`: no element children and no text.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public override bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-controlstateselector"></a>

## `ControlStateSelector`

Namespace: `Nazor.Style`

Matches retained control/editor metadata. Accepted names are `enabled`, `indeterminate`, `read-only`, `read-write`, `placeholder-shown`, `valid`, `invalid`, `user-valid`, `user-invalid`, `required`, and `optional`. Each adds `(0,1,0)` specificity. See [control-state applicability](../Guide/17-Selectors-and-Media-Queries.md#state-and-structural-pseudo-classes).

| Declaration | Contract |
| --- | --- |
| `public sealed record ControlStateSelector : PseudoSelector, IEquatable<ControlStateSelector>` | An immutable control-state selector. |
| `public ControlStateSelector(string name)` | Normalize the name without a colon, ignoring case. Throws `ArgumentNullException` for null or `ArgumentException` for an unsupported name. |
| `public string Name { get; }` | The normalized pseudo-class name. |
| `public override bool Matches(ElementNode element)` | Match role, effective editability/disabled state, placeholder presentation or the application-owned field flags. Disabled/read-only controls retain required/invalid metadata. |
| `public override string ToString()` | The name prefixed with a colon. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(ControlStateSelector? left, ControlStateSelector? right)` | Compare record values for inequality. |
| `public static bool operator ==(ControlStateSelector? left, ControlStateSelector? right)` | Compare record values for equality. |
| `public override int GetHashCode()` | Hash the record value. |
| `public override bool Equals(object? obj)` | Compare record values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare this selector through its base record contract. |
| `public bool Equals(ControlStateSelector? other)` | Compare normalized selector names. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |

<a id="type-flexdirection"></a>

## `FlexDirection`

Namespace: `Nazor.Style`

```csharp
public enum FlexDirection : byte
```

Specifies the main axis and item order of a flex container.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Row`

```csharp
Row
```

Lays out items horizontally in forward order.

#### `RowReverse`

```csharp
RowReverse
```

Lays out items horizontally in reverse order.

#### `Column`

```csharp
Column
```

Lays out items vertically in forward order.

#### `ColumnReverse`

```csharp
ColumnReverse
```

Lays out items vertically in reverse order.


<a id="type-flexwrap"></a>

## `FlexWrap`

Namespace: `Nazor.Style`

```csharp
public enum FlexWrap : byte
```

Specifies whether flex items wrap and whether the line order is reversed.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `NoWrap`

```csharp
NoWrap
```

Keeps flex items on a single line.

#### `Wrap`

```csharp
Wrap
```

Wraps flex items onto additional lines.

#### `WrapReverse`

```csharp
WrapReverse
```

Wraps flex items with the cross-axis line order reversed.


<a id="type-fontstyle"></a>

## `FontStyle`

Namespace: `Nazor.Style`

```csharp
public enum FontStyle : byte
```

Specifies normal or italic font selection.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Selects an upright font face.

#### `Italic`

```csharp
Italic
```

Selects an italic font face.


<a id="type-globalkeyword"></a>

## `GlobalKeyword`

Namespace: `Nazor.Style`

```csharp
public enum GlobalKeyword : byte
```

Specifies how a property obtains its value through the cascade and inheritance.

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Initial`

```csharp
Initial
```

Uses the property's registered initial value.

#### `Inherit`

```csharp
Inherit
```

Uses the parent's computed value.

#### `Unset`

```csharp
Unset
```

Inherits inherited properties and uses the initial value for other properties.


<a id="type-gradient"></a>

## `Gradient`

Namespace: `Nazor.Style`

```csharp
public abstract record Gradient(ValueList<ColorStop> Stops) : ImageValue
```

A gradient image.

| Parameter | Description |
| --- | --- |
| `Stops` | The stops (ValueList<ColorStop>). |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)


<a id="type-gridarea"></a>

## `GridArea`

Namespace: `Nazor.Style`

```csharp
public readonly record struct GridArea(string Name, int RowStart, int RowEnd, int ColumnStart, int ColumnEnd)
```

A named area of `grid-template-areas`: 0-based tracks, end exclusive.

| Parameter | Description |
| --- | --- |
| `Name` | The name used to identify the parameter, property, resource, or registration. |
| `RowStart` | The row start (int). |
| `RowEnd` | The row end (int). |
| `ColumnStart` | The column start (int). |
| `ColumnEnd` | The column end (int). |

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)


<a id="type-gridareas"></a>

## `GridAreas`

Namespace: `Nazor.Style`

```csharp
public sealed class GridAreas : IEquatable<GridAreas>
```

The value of `grid-template-areas`: one string per row, cells separated by spaces, a run of dots for an empty cell. Every name must form a filled rectangle; the constructor rejects the rest with `ArgumentException` (the NSS compiler reports it as NZS102).

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `None`

```csharp
public static readonly GridAreas None ;
```

An empty named-grid-area definition.

#### `GridAreas`

```csharp
public GridAreas(IEnumerable<string> rows)
```

Initializes a new instance of GridAreas.

| Parameter | Description |
| --- | --- |
| `rows` | The named-area rows, with whitespace separating cells and dots indicating empty cells. |

#### `Rows`

```csharp
public ValueList<string> Rows { get; }
```

The rows with their cells separated by one space.

#### `RowCount`

```csharp
public int RowCount {get; }
```

Gets the number of rows in the grid-area definition.

#### `Columns`

```csharp
public int Columns { get; }
```

Gets the number of columns in the grid-area definition.

#### `Areas`

```csharp
public IReadOnlyCollection<GridArea> Areas {get; }
```

Gets the named grid areas and their row and column bounds.

#### `TryGetArea`

```csharp
public bool TryGetArea(string name, out GridArea area)
```

Looks up the row and column bounds of a named grid area.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `area` | When this method returns, contains the area produced by the operation. |

Returns: true if the named area exists; otherwise, false.

#### `Equals`

```csharp
public bool Equals(GridAreas? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-gridautoflow"></a>

## `GridAutoFlow`

Namespace: `Nazor.Style`

```csharp
public enum GridAutoFlow : byte
```

Specifies the axis and packing strategy used to place implicit grid items.

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Row`

```csharp
Row
```

Places auto-positioned items by filling rows.

#### `Column`

```csharp
Column
```

Places auto-positioned items by filling columns.

#### `RowDense`

```csharp
RowDense
```

Fills rows and backfills earlier gaps.

#### `ColumnDense`

```csharp
ColumnDense
```

Fills columns and backfills earlier gaps.


<a id="type-gridautorepeat"></a>

## `GridAutoRepeat`

Namespace: `Nazor.Style`

```csharp
public enum GridAutoRepeat : byte
```

Specifies how repeated grid tracks fill the available space.

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `None`

```csharp
None
```

Uses the explicit track repetition count.

#### `Fill`

```csharp
Fill
```

Creates as many repeated tracks as fit the available space.

#### `Fit`

```csharp
Fit
```

Creates fitting repeated tracks and collapses unused tracks.


<a id="type-gridline"></a>

## `GridLine`

Namespace: `Nazor.Style`

```csharp
public readonly record struct GridLine(GridLineKind Kind, int Value, string? Name)
```

One side of a grid item's placement: `grid-row-start`, `grid-column-end`...

| Parameter | Description |
| --- | --- |
| `Kind` | The grid line kind represented by this value. |
| `Value` | The numeric value or payload represented by this record. |
| `Name` | The name used to identify the parameter, property, resource, or registration. |

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Auto`

```csharp
public static readonly GridLine Auto ;
```

Automatic grid-item placement without an explicit line or span.

#### `Line`

```csharp
public static GridLine Line(int value)
```

Creates a grid placement at the specified one-based line, with negative values counting from the end.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting GridLine value.

#### `Span`

```csharp
public static GridLine Span(int value)
```

Creates a grid placement spanning the specified number of tracks.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting GridLine value.

#### `Area`

```csharp
public static GridLine Area(string name)
```

Creates a grid placement that references a named area.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting GridLine value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-gridlinekind"></a>

## `GridLineKind`

Namespace: `Nazor.Style`

```csharp
public enum GridLineKind : byte
```

Distinguishes automatic placement, explicit grid lines, spans, and named areas.

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Auto`

```csharp
Auto
```

Places the item using the grid auto-placement algorithm.

#### `Line`

```csharp
Line
```

A line number: from 1 at the start of the explicit grid, negative from its end.

#### `Span`

```csharp
Span
```

`span N`: the item covers N tracks from the other line.

#### `Area`

```csharp
Area
```

The start or end line of a named area of `grid-template-areas`.


<a id="type-gridtracklist"></a>

## `GridTrackList`

Namespace: `Nazor.Style`

```csharp
public sealed record GridTrackList(ValueList<TrackSize> Tracks, GridAutoRepeat Repeat, int RepeatIndex, ValueList<TrackSize> RepeatTracks)
```

The value of `grid-template-columns` and `grid-template-rows`: explicit tracks, with repetitions of a fixed count already expanded and at most one automatic repetition inserted before track `RepeatIndex`. `None` is the keyword `none`.

| Parameter | Description |
| --- | --- |
| `Tracks` | The track definitions in declaration order. |
| `Repeat` | The repeat (GridAutoRepeat). |
| `RepeatIndex` | The repeat index (int). |
| `RepeatTracks` | The repeat tracks (ValueList<TrackSize>). |

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `None`

```csharp
public static readonly GridTrackList None ;
```

An empty explicit track definition.

#### `GridTrackList`

```csharp
public GridTrackList(IEnumerable<TrackSize> tracks)
```

Initializes a new instance of GridTrackList.

| Parameter | Description |
| --- | --- |
| `tracks` | The track definitions in declaration order. |

#### `IsNone`

```csharp
public bool IsNone {get; }
```

Gets whether the explicit track list is empty and has no automatic repetition.

#### `Expand`

```csharp
public IReadOnlyList<TrackSize> Expand(int repetitions)
```

The tracks with the automatic repetition repeated `repetitions` times.

| Parameter | Description |
| --- | --- |
| `repetitions` | The repetitions (int). |

Returns: The resulting IReadOnlyList<TrackSize> value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-imagevalue"></a>

## `ImageValue`

Namespace: `Nazor.Style`

```csharp
public abstract record ImageValue
```

A layer of `background-image`: a gradient or an image by URL.

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)


<a id="type-inlinestyle"></a>

## `InlineStyle`

Namespace: `Nazor.Style`

```csharp
public static class InlineStyle
```

The declarations of a `style` attribute (`width: 40%; --accent: red`), read at runtime: they apply after every rule of every level, as the inline styles of CSS. Each value is parsed for the element, after `var()`, so an invalid one behaves as `unset`; unknown properties and pieces without a colon are skipped, as browsers do.

[Source](../../Sources/Core/Nazor.Style/Engine/InlineStyle.cs)

### Members

#### `Parse`

```csharp
public static IReadOnlyList<StyleDeclaration> Parse(string text)
```

The declarations of `text`, in order; the same text is read once.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |

Returns: The resulting IReadOnlyList<StyleDeclaration> value.

Throws `ArgumentNullException`: text is null.


<a id="type-justifycontent"></a>

## `JustifyContent`

Namespace: `Nazor.Style`

```csharp
public enum JustifyContent : byte
```

Specifies how free space is distributed along the layout's main axis.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Uses the layout mode's normal alignment behavior.

#### `Stretch`

```csharp
Stretch
```

Stretches items or tracks to fill available space.

#### `FlexStart`

```csharp
FlexStart
```

Aligns items or tracks at the start of the applicable axis.

#### `FlexEnd`

```csharp
FlexEnd
```

Aligns items or tracks at the end of the applicable axis.

#### `Center`

```csharp
Center
```

Centers items or tracks within the available space.

#### `SpaceBetween`

```csharp
SpaceBetween
```

Distributes free space between items or tracks.

#### `SpaceAround`

```csharp
SpaceAround
```

Distributes free space around items or tracks, with half-sized outer gaps.

#### `SpaceEvenly`

```csharp
SpaceEvenly
```

Distributes free space into equal inner and outer gaps.


<a id="type-keyframe"></a>

## `Keyframe`

Namespace: `Nazor.Style`

```csharp
public sealed class Keyframe(double offset, IReadOnlyList<StyleDeclaration> declarations, EasingFunction? easing = null)
```

One keyframe of a `@keyframes` rule: its offset in the iteration (0 for `from`, 1 for `to`), its declarations (animatable properties only) and, with `animation-timing-function`, the timing function of the interval that starts here.

| Parameter | Description |
| --- | --- |
| `offset` | The offset (double). |
| `declarations` | The declarations assigned by the rule or keyframe. |
| `easing` | The easing (EasingFunction?). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)

### Members

#### `Offset`

```csharp
public double Offset { get; }
```

Gets the normalized keyframe position, where zero is the start and one is the end.

#### `Declarations`

```csharp
public IReadOnlyList<StyleDeclaration> Declarations { get; }
```

Gets the style declarations applied by this rule or keyframe.

#### `Easing`

```csharp
public EasingFunction? Easing { get; }
```

Gets the optional easing function used for the segment starting at this keyframe.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-keyframesrule"></a>

## `KeyframesRule`

Namespace: `Nazor.Style`

```csharp
public sealed class KeyframesRule(string name, IReadOnlyList<Keyframe> keyframes)
```

A `@keyframes` rule: a name and its keyframes by offset (equal offsets in source order).

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `keyframes` | The keyframe animation definitions supplied by the stylesheet. |

[Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)

### Members

#### `Name`

```csharp
public string Name { get; }
```

Gets the identifier referenced by animation-name.

#### `Keyframes`

```csharp
public IReadOnlyList<Keyframe> Keyframes { get; }
```

Gets the ordered keyframes used to evaluate this animation.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-keyworddeclaration"></a>

## `KeywordDeclaration`

Namespace: `Nazor.Style`

```csharp
public sealed class KeywordDeclaration(StyleProperty property, GlobalKeyword keyword) : StyleDeclaration
```

`initial`, `inherit` or `unset` on a longhand.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |
| `keyword` | The keyword (GlobalKeyword). |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Property`

```csharp
public StyleProperty Property { get; }
```

Gets the style property targeted by this value or declaration.

#### `Keyword`

```csharp
public GlobalKeyword Keyword { get; }
```

Gets the global cascade keyword assigned to this property.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-length"></a>

## `Length`

Namespace: `Nazor.Style`

```csharp
public readonly record struct Length(double Value, LengthUnit Unit, CalcExpression? Expression = null)
```

A length in logical units, a percentage resolved against a basis defined by each property, a relative length (em, rem, vw...) or an expression. The style engine converts relative units to px: computed values have only px, percentages and expressions with percentages.

| Parameter | Description |
| --- | --- |
| `Value` | The numeric value or payload represented by this record. |
| `Unit` | The unit or calculation mode used to resolve the length. |
| `Expression` | The optional calculation expression used when the length unit is Calc. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Zero`

```csharp
public static readonly Length Zero ;
```

The length with all components set to zero.

#### `Px`

```csharp
public static Length Px(double value)
```

Creates a length expressed in logical pixels.

| Parameter | Description |
| --- | --- |
| `value` | The numeric magnitude in the unit selected by this factory method. |

Returns: The resulting Length value.

#### `Percent`

```csharp
public static Length Percent(double value)
```

Creates a length expressed as a percentage of a reference dimension.

| Parameter | Description |
| --- | --- |
| `value` | The numeric magnitude in the unit selected by this factory method. |

Returns: The resulting Length value.

#### `Calc`

```csharp
public static Length Calc(CalcExpression expression)
```

Creates a length backed by a calculation expression.

| Parameter | Description |
| --- | --- |
| `expression` | The expression (CalcExpression). |

Returns: The resulting Length value.

#### `IsPercent`

```csharp
public bool IsPercent {get; }
```

The value depends on the percentage basis: a percentage or an expression with percentages.

#### `IsRelative`

```csharp
public bool IsRelative {get; }
```

Em, rem, viewport units, or an expression with them.

#### `Resolve`

```csharp
public double Resolve(double basis)
```

Resolves the length into logical pixels using the supplied percentage basis.

| Parameter | Description |
| --- | --- |
| `basis` | The reference dimension used to resolve percentage values. |

Returns: The resulting double value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-lengthunit"></a>

## `LengthUnit`

Namespace: `Nazor.Style`

```csharp
public enum LengthUnit : byte
```

Identifies the unit or calculation used to resolve an NSS length.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Px`

```csharp
Px
```

Measures logical pixels.

#### `Percent`

```csharp
Percent
```

Measures a percentage of the applicable reference dimension.

#### `Em`

```csharp
Em
```

Measures multiples of the current font size.

#### `Rem`

```csharp
Rem
```

Measures multiples of the root font size.

#### `Vw`

```csharp
Vw
```

Measures hundredths of the viewport width.

#### `Vh`

```csharp
Vh
```

Measures hundredths of the viewport height.

#### `Vmin`

```csharp
Vmin
```

Measures hundredths of the smaller viewport dimension.

#### `Vmax`

```csharp
Vmax
```

Measures hundredths of the larger viewport dimension.

#### `Calc`

```csharp
Calc
```

Evaluates a calculation expression to resolve the length.


<a id="type-lineargradient"></a>

## `LinearGradient`

Namespace: `Nazor.Style`

```csharp
public sealed record LinearGradient(double? Angle, int ToX, int ToY, ValueList<ColorStop> Stops) : Gradient(Stops)
```

`linear-gradient()`: an angle in degrees (0 points up, clockwise), or a side or corner as `ToX` (−1 left, 1 right) and `ToY` (−1 top, 1 bottom), resolved against the box when drawn as CSS requires for corners.

| Parameter | Description |
| --- | --- |
| `Angle` | The angle (double?). |
| `ToX` | The to x (int). |
| `ToY` | The to y (int). |
| `Stops` | The stops (ValueList<ColorStop>). |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-lineheight"></a>

## `LineHeight`

Namespace: `Nazor.Style`

```csharp
public readonly record struct LineHeight(LineHeightKind Kind, double Number, Length Length)
```

`normal`, a multiplier of the font size, or a length.

| Parameter | Description |
| --- | --- |
| `Kind` | The line height kind represented by this value. |
| `Number` | The number (double). |
| `Length` | The absolute, relative, or percentage length to use. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
public static readonly LineHeight Normal ;
```

The normal line height determined by font metrics.

#### `Multiplier`

```csharp
public static LineHeight Multiplier(double value)
```

Creates a unitless line-height multiplier relative to the font size.

| Parameter | Description |
| --- | --- |
| `value` | The numeric magnitude in the unit selected by this factory method. |

Returns: The resulting LineHeight value.

#### `Fixed`

```csharp
public static LineHeight Fixed(Length length)
```

Creates a line height from an absolute or percentage length.

| Parameter | Description |
| --- | --- |
| `length` | The absolute, relative, or percentage length to use. |

Returns: The resulting LineHeight value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-lineheightkind"></a>

## `LineHeightKind`

Namespace: `Nazor.Style`

```csharp
public enum LineHeightKind : byte
```

Specifies the available line height kind values.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Uses the font's normal line metrics.

#### `Number`

```csharp
Number
```

Uses a unitless multiplier of the font size.

#### `Length`

```csharp
Length
```

Uses a length resolved to a logical line height.


<a id="type-logicalpseudo"></a>

## `LogicalPseudo`

Namespace: `Nazor.Style`

```csharp
public enum LogicalPseudo : byte
```

Specifies the matching and specificity behavior of a logical pseudo-class.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Is`

```csharp
Is
```

Matches any argument selector and uses the greatest argument specificity.

#### `Not`

```csharp
Not
```

Matches when none of the argument selectors match.

#### `Where`

```csharp
Where
```

Matches any argument selector with zero specificity.


<a id="type-logicalselector"></a>

## `LogicalSelector`

Namespace: `Nazor.Style`

```csharp
public sealed record LogicalSelector(LogicalPseudo Kind, ValueList<Selector> Selectors) : PseudoSelector
```

`:is()`, `:not()` and `:where()` with a list of complex selectors about the same element.

| Parameter | Description |
| --- | --- |
| `Kind` | The logical pseudo represented by this value. |
| `Selectors` | The selectors (ValueList<Selector>). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public override bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Specificity`

```csharp
public override Specificity Specificity {get; }
```

Gets the selector's identifier, class, and type precedence counts.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-matrixfunction"></a>

## `MatrixFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record MatrixFunction(double A, double B, double C, double D, double E, double F) : TransformFunction
```

`matrix(a, b, c, d, e, f)`: x' = a·x + c·y + e, y' = b·x + d·y + f, translations in px.

| Parameter | Description |
| --- | --- |
| `A` | The alpha channel in the range from zero to 255. |
| `B` | The blue channel in the range from zero to 255. |
| `C` | The c (double). |
| `D` | The d (double). |
| `E` | The e (double). |
| `F` | The f (double). |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-mediacondition"></a>

## `MediaCondition`

Namespace: `Nazor.Style`

```csharp
public sealed record MediaCondition(double? MinWidth, double? MaxWidth, double? MinHeight, double? MaxHeight, ColorScheme? PrefersColorScheme = null, bool? PrefersReducedMotion = null, bool? ForcedColors = null, bool? PrefersMoreContrast = null, PlatformKind? Platform = null, ControlDensity? Density = null)
```

A conjunction of width/height bounds and preferences, e.g. `(min-width: 400px) and (prefers-color-scheme: dark)`; a null part is not tested.

| Parameter | Description |
| --- | --- |
| `MinWidth` | The min width (double?). |
| `MaxWidth` | The max width (double?). |
| `MinHeight` | The min height (double?). |
| `MaxHeight` | The max height (double?). |
| `PrefersColorScheme` | The prefers color scheme (ColorScheme?). |
| `PrefersReducedMotion` | The prefers reduced motion (bool?). |
| `ForcedColors` | The forced colors (bool?). |
| `PrefersMoreContrast` | The prefers more contrast (bool?). |
| `Platform` | The platform (PlatformKind?). |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Always`

```csharp
public static readonly MediaCondition Always ;
```

An unconditional media condition that matches every viewport.

#### `Never`

```csharp
public static readonly MediaCondition Never ;
```

A condition that never matches, as two contradicting preferences give.

#### `Matches`

```csharp
public bool Matches(Viewport viewport)
```

Tests whether the viewport and media preferences satisfy this condition.

| Parameter | Description |
| --- | --- |
| `viewport` | The viewport dimensions used to evaluate the media query. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Matches`

```csharp
public bool Matches(Viewport viewport, MediaPreferences preferences)
```

Tests whether the viewport and media preferences satisfy this condition.

| Parameter | Description |
| --- | --- |
| `viewport` | The viewport dimensions used to evaluate the media query. |
| `preferences` | The system and application preferences used to evaluate the media query. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `And`

```csharp
public MediaCondition And(MediaCondition other)
```

Both conditions, as for an `@media` nested in another.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: The resulting MediaCondition value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-mediapreferences"></a>

## `MediaPreferences`

Namespace: `Nazor.Style`

```csharp
public readonly record struct MediaPreferences(ColorScheme ColorScheme, bool ReducedMotion, bool ForcedColors = false, SystemColors? Colors = null, PlatformKind? Platform = null, ControlDensity Density = ControlDensity.Compact)
```

The user preferences that media queries test: the color scheme (the app mode of Windows), reduced motion ("Show animations" off) and forced colors (a contrast theme of Windows, which also means `prefers-contrast: more`); and the system colors that NSS names (`Canvas`…), the defaults when null. The default is light, with animations, without forced colors.

| Parameter | Description |
| --- | --- |
| `ColorScheme` | The color scheme (ColorScheme). |
| `ReducedMotion` | The reduced motion (bool). |
| `ForcedColors` | The forced colors (bool). |
| `Colors` | The colors (SystemColors?). |
| `Platform` | The platform (PlatformKind?). |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)


<a id="type-mediaquery"></a>

## `MediaQuery`

Namespace: `Nazor.Style`

```csharp
public sealed class MediaQuery(IReadOnlyList<MediaCondition> conditions)
```

A media query list: matches when any of its conditions matches.

| Parameter | Description |
| --- | --- |
| `conditions` | The media conditions combined by the query. |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Conditions`

```csharp
public IReadOnlyList<MediaCondition> Conditions { get; }
```

Gets the media conditions that make up this query.

#### `Matches`

```csharp
public bool Matches(Viewport viewport)
```

Tests whether the viewport and media preferences satisfy this condition.

| Parameter | Description |
| --- | --- |
| `viewport` | The viewport dimensions used to evaluate the media query. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Matches`

```csharp
public bool Matches(Viewport viewport, MediaPreferences preferences)
```

Tests whether the viewport and media preferences satisfy this condition.

| Parameter | Description |
| --- | --- |
| `viewport` | The viewport dimensions used to evaluate the media query. |
| `preferences` | The system and application preferences used to evaluate the media query. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `And`

```csharp
public MediaQuery And(MediaQuery other)
```

Combines the two media queries so that both must match.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: The resulting MediaQuery value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-mixfunction"></a>

## `MixFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record MixFunction(ValueList<TransformFunction> From, ValueList<TransformFunction> To, double Progress) : TransformFunction
```

The value of a transition between two transform lists that do not match function by function (CSS Transforms 1 §9): their matrices, which depend on the element's box, are interpolated by decomposition when the element is drawn. Never written in NSS.

| Parameter | Description |
| --- | --- |
| `From` | The from (ValueList<TransformFunction>). |
| `To` | The to (ValueList<TransformFunction>). |
| `Progress` | The progress (double). |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-nthselector"></a>

## `NthSelector`

Namespace: `Nazor.Style`

```csharp
public sealed record NthSelector(int A, int B, bool FromEnd, bool OfType, string Name) : PseudoSelector
```

`:nth-child(An+B)` and its family: the element's 1-based position among its eligible element siblings, from the end with `FromEnd`, is A×n+B for some n ≥ 0. `:first-child` is (0, 1), `:last-child` the same from the end. Optional `Filter` selects eligible physical siblings for the child/last-child forms; `OfType` instead restricts to siblings with the same element name.

| Declaration | Contract |
| --- | --- |
| `public ValueList<Selector>? Filter { get; init; }` | An optional selector-list filter for nth-child/nth-last-child. Null counts all siblings. Scoped matching preserves component boundaries until an explicit outer deep combinator. |
| `public override Specificity Specificity { get; }` | One pseudo-class unit plus the greatest filter specificity, if present. |

| Parameter | Description |
| --- | --- |
| `A` | The step in An+B; zero matches only the B position. |
| `B` | The additive offset in the one-based position expression. |
| `FromEnd` | Whether positions are counted from the last eligible sibling. |
| `OfType` | Whether only siblings of the same element type are counted. |
| `Name` | The unfiltered NSS spelling, such as :nth-child(2n+1) or :first-child. |

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public override bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-objectfit"></a>

## `ObjectFit`

Namespace: `Nazor.Style`

```csharp
public enum ObjectFit : byte
```

How an image fills its content box (`object-fit`).

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `Fill`

```csharp
Fill
```

Stretches the image to fill the destination box.

#### `Contain`

```csharp
Contain
```

Preserves the aspect ratio while fitting the whole image inside the box.

#### `Cover`

```csharp
Cover
```

Preserves the aspect ratio while filling and cropping to the box.

#### `None`

```csharp
None
```

Uses the intrinsic image dimensions.

#### `ScaleDown`

```csharp
ScaleDown
```

Uses the smaller result of intrinsic sizing and contain sizing.


<a id="type-onlychildselector"></a>

## `OnlyChildSelector`

Namespace: `Nazor.Style`

```csharp
public sealed record OnlyChildSelector(bool OfType) : PseudoSelector
```

`:only-child` and `:only-of-type`.

| Parameter | Description |
| --- | --- |
| `OfType` | The of type (bool). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public override bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-overflow"></a>

## `Overflow`

Namespace: `Nazor.Style`

```csharp
public enum Overflow : byte
```

Specifies clipping and scrolling behavior when content exceeds its box.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Visible`

```csharp
Visible
```

Paints overflowing content outside the box.

#### `Hidden`

```csharp
Hidden
```

Clips overflow without exposing scrolling behavior.

#### `Scroll`

```csharp
Scroll
```

Clips overflow and enables scrolling.

#### `Auto`

```csharp
Auto
```

Enables scrolling when content overflows.


<a id="type-overflowwrap"></a>

## `OverflowWrap`

Namespace: `Nazor.Style`

```csharp
public enum OverflowWrap : byte
```

Specifies when words may be broken to prevent text overflow.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Breaks text only at normal wrapping opportunities.

#### `BreakWord`

```csharp
BreakWord
```

Allows an otherwise overflowing word to break, without reducing its intrinsic minimum width.

#### `Anywhere`

```csharp
Anywhere
```

Allows a word to break at any character boundary and includes those opportunities in intrinsic sizing.


<a id="type-pendingdeclaration"></a>

## `PendingDeclaration`

Namespace: `Nazor.Style`

```csharp
public sealed class PendingDeclaration(string propertyName, IReadOnlyList<StyleToken> tokens) : StyleDeclaration
```

A longhand or shorthand whose value contains `var()`: parsed for each element once the custom properties are known. An invalid result behaves as `unset`.

| Parameter | Description |
| --- | --- |
| `propertyName` | The canonical NSS property name. |
| `tokens` | The NSS lexical token sequence. |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `PropertyName`

```csharp
public string PropertyName { get; }
```

Gets the NSS property name to resolve after custom-property substitution.

#### `Tokens`

```csharp
public IReadOnlyList<StyleToken> Tokens { get; }
```

Gets the NSS token sequence represented by this declaration or reader.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-pointerevents"></a>

## `PointerEvents`

Namespace: `Nazor.Style`

```csharp
public enum PointerEvents : byte
```

Specifies whether an element participates in pointer hit testing.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
Auto
```

Allows normal pointer hit testing.

#### `None`

```csharp
None
```

Excludes the element from pointer targeting.


<a id="type-position"></a>

## `Position`

Namespace: `Nazor.Style`

```csharp
public enum Position : byte
```

Specifies whether a node participates in normal flow or uses positioned layout.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Static`

```csharp
Static
```

Participates in normal flow without positional offsets.

#### `Relative`

```csharp
Relative
```

Participates in normal flow and applies offsets to its visual position.

#### `Absolute`

```csharp
Absolute
```

Positions the node outside normal flow relative to its containing block.


<a id="type-positionvalue"></a>

## `PositionValue`

Namespace: `Nazor.Style`

```csharp
public readonly record struct PositionValue(Length X, Length Y)
```

A point of a box from its top left corner, as `object-position` writes it: percentages are of the space left over (50% centers), lengths are offsets.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `Center`

```csharp
public static readonly PositionValue Center ;
```

A centered axis position at 50 percent of the reference dimension.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-pseudoelement"></a>

## `PseudoElement`

Namespace: `Nazor.Style`

```csharp
public enum PseudoElement
```

A generated decorative box or text portion relative to an originating element.

| Declaration | Contract |
| --- | --- |
| `None = 0` | Select the originating element itself. |
| `Before = 1` | Generate its first decorative child box. |
| `After = 2` | Generate its last decorative child box. |
| `Placeholder = 3` | Style empty editable fields' placeholder text. |
| `Selection = 4` | Style the foreground/background of selected editable text. |

<a id="type-pseudoclasses"></a>

## `PseudoClasses`

Namespace: `Nazor.Style`

```csharp
public static class PseudoClasses
```

Pseudo-classes supported by NSS: the interaction states they test, and the others by name.

`Names` includes `popup-above`; `State("popup-above")` maps to `ElementStates.PopupAbove`, maintained by presentation after final anchored placement. NSS parsing, serialization and generated C# preserve this state selector.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Names`

```csharp
public static readonly IReadOnlyList<(ElementStates State, string Name)> Names ;
```

The supported interaction pseudo-class names mapped to their element state flags.

#### `Structural`

```csharp
public static readonly IReadOnlyList<string> Structural ;
```

The structural pseudo-classes without arguments.

#### `Functional`

```csharp
public static readonly IReadOnlyList<string> Functional ;
```

The pseudo-classes with arguments, written with their parenthesis.

#### `All`

```csharp
public static IEnumerable<string> All {get; }
```

Every name, for editors: states, structural and functional.

#### `TryGet`

```csharp
public static bool TryGet(string name, out ElementStates state)
```

Resolves a pseudo-class name to its corresponding element interaction state.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `state` | When this method returns, contains the state produced by the operation. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Structure`

```csharp
public static PseudoSelector? Structure(string name)
```

A structural or derived control-state pseudo-class without arguments, or null.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting PseudoSelector? value.


<a id="type-pseudoselector"></a>

## `PseudoSelector`

Namespace: `Nazor.Style`

```csharp
public abstract record PseudoSelector
```

A pseudo-class that is not an interaction state: structural or logical.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public abstract bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Specificity`

```csharp
public virtual Specificity Specificity {get; }
```

What the pseudo-class adds to the specificity of its compound (Selectors 4 §16).


<a id="type-radialextent"></a>

## `RadialExtent`

Namespace: `Nazor.Style`

```csharp
public enum RadialExtent : byte
```

Specifies how a radial gradient's radii are derived from its center and painting box.

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `FarthestCorner`

```csharp
FarthestCorner
```

Extends the gradient to the farthest corner of its painting box.

#### `ClosestCorner`

```csharp
ClosestCorner
```

Extends the gradient to the closest corner of its painting box.

#### `FarthestSide`

```csharp
FarthestSide
```

Extends the gradient to the farthest side of its painting box.

#### `ClosestSide`

```csharp
ClosestSide
```

Extends the gradient to the closest side of its painting box.

#### `Explicit`

```csharp
Explicit
```

The radii are `RadialGradient.SizeX` and `RadialGradient.SizeY`.


<a id="type-radialgradient"></a>

## `RadialGradient`

Namespace: `Nazor.Style`

```csharp
public sealed record RadialGradient(RadialShape Shape, RadialExtent Extent, Length SizeX, Length SizeY, Length CenterX, Length CenterY, ValueList<ColorStop> Stops) : Gradient(Stops)
```

`radial-gradient()`: an ellipse or a circle, sized by an extent keyword or by explicit radii (percentages of the box for an ellipse), centered at `CenterX`, `CenterY`.

| Parameter | Description |
| --- | --- |
| `Shape` | The shape (RadialShape). |
| `Extent` | The extent (RadialExtent). |
| `SizeX` | The size x (Length). |
| `SizeY` | The size y (Length). |
| `CenterX` | The center x (Length). |
| `CenterY` | The center y (Length). |
| `Stops` | The stops (ValueList<ColorStop>). |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-radialshape"></a>

## `RadialShape`

Namespace: `Nazor.Style`

```csharp
public enum RadialShape : byte
```

Specifies whether a radial gradient uses a circle or ellipse.

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `Ellipse`

```csharp
Ellipse
```

Uses independently resolved horizontal and vertical radii.

#### `Circle`

```csharp
Circle
```

Uses the same radius along both axes.


<a id="type-radiusvalue"></a>

## `RadiusValue`

Namespace: `Nazor.Style`

```csharp
public readonly record struct RadiusValue(Length X, Length Y)
```

The radius of one corner: `X` horizontal, `Y` vertical, lengths or percentages of the border box.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Zero`

```csharp
public static readonly RadiusValue Zero ;
```

The radius value with all components set to zero.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-resolvedkeyframe"></a>

## `ResolvedKeyframe`

Namespace: `Nazor.Style`

```csharp
public sealed record ResolvedKeyframe(double Offset, EasingFunction? Easing, IReadOnlyDictionary<StyleProperty, object> Values)
```

A keyframe resolved for one element: its offset, timing function and computed values.

| Parameter | Description |
| --- | --- |
| `Offset` | The offset (double). |
| `Easing` | The easing (EasingFunction?). |
| `Values` | The values (IReadOnlyDictionary<StyleProperty, object>). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)


<a id="type-rootselector"></a>

## `RootSelector`

Namespace: `Nazor.Style`

```csharp
public sealed record RootSelector : PseudoSelector
```

`:root`: an element without a parent element (the window).

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Matches`

```csharp
public override bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-rotatefunction"></a>

## `RotateFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record RotateFunction(double Angle) : TransformFunction
```

`rotate()`, clockwise.

| Parameter | Description |
| --- | --- |
| `Angle` | The angle (double). |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-scalefunction"></a>

## `ScaleFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record ScaleFunction(double X, double Y) : TransformFunction
```

`scale()`, `scaleX()`, `scaleY()`.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-selector"></a>

## `Selector`

Namespace: `Nazor.Style`

```csharp
public sealed class Selector : IEquatable<Selector>
```

A complex selector: compounds from left to right joined by combinators. Matching starts from the subject (the last compound), physical ancestors/siblings and explicit logical component qualifiers.

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Selector`

```csharp
public Selector(IReadOnlyList<CompoundSelector> compounds, IReadOnlyList<Combinator> combinators)
```

Initializes a new instance of Selector.

| Parameter | Description |
| --- | --- |
| `compounds` | The compound selectors in matching order. |
| `combinators` | The relationships between successive compound selectors. |

#### `Compounds`

```csharp
public IReadOnlyList<CompoundSelector> Compounds { get; }
```

Gets the compound selectors in matching order.

#### `Combinators`

```csharp
public IReadOnlyList<Combinator> Combinators { get; }
```

Gets the relationships between successive compound selectors.

#### `Specificity`

```csharp
public Specificity Specificity { get; }
```

Gets the selector's identifier, class, and type precedence counts.

#### `Subject`

```csharp
public CompoundSelector Subject {get; }
```

Gets the final compound selector describing the element being matched.

#### `IsStructural`

```csharp
public bool IsStructural {get; }
```

Whether matching depends on siblings or on the presence of children (structural pseudo-classes, `+`, `~`).

#### `HasDeep`

```csharp
public bool HasDeep {get; }
```

Gets whether this selector contains a component-boundary-crossing deep combinator.

#### `Matches`

```csharp
public bool Matches(ElementNode element)
```

Tests whether the element satisfies this selector.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public bool Equals(Selector? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-shorthandproperty"></a>

## `ShorthandProperty`

Namespace: `Nazor.Style`

```csharp
public sealed class ShorthandProperty
```

A shorthand expanded into longhands, at compile time or after `var()` substitution.

[Source](../../Sources/Core/Nazor.Style/Properties/StyleProperty.cs)

### Members

#### `Name`

```csharp
public string Name { get; }
```

Gets the canonical NSS name of the shorthand property.

#### `Syntax`

```csharp
public string Syntax { get; }
```

Gets the NSS value-definition grammar accepted by this property.

#### `Keywords`

```csharp
public IReadOnlyList<string> Keywords { get; }
```

Gets the literal keywords accepted by the property's grammar.

#### `ValueTypes`

```csharp
public IReadOnlyList<string> ValueTypes { get; }
```

Gets the value-type names referenced by the property's grammar.

#### `Longhands`

```csharp
public IReadOnlyList<StyleProperty> Longhands { get; }
```

Gets the longhand properties assigned by this shorthand.

#### `Expand`

```csharp
public IReadOnlyList<(StyleProperty Property, object Value)> Expand(TokenReader reader)
```

Parses a shorthand and expands it into its constituent longhand property values.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The longhand property and parsed value pairs assigned by the shorthand.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-sizekind"></a>

## `SizeKind`

Namespace: `Nazor.Style`

```csharp
public enum SizeKind : byte
```

Specifies automatic, fixed, and intrinsic sizing modes.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
Auto
```

Lets the layout algorithm determine the size.

#### `None`

```csharp
None
```

Leaves the maximum size unconstrained.

#### `Content`

```csharp
Content
```

Uses the content-based flex basis.

#### `Fixed`

```csharp
Fixed
```

Resolves the supplied length to determine the size.

#### `MinContent`

```csharp
MinContent
```

The narrowest width of the content: every soft wrap taken (CSS Sizing §2).

#### `MaxContent`

```csharp
MaxContent
```

The width of the content without wrapping.

#### `FitContent`

```csharp
FitContent
```

The available width, but at least min-content and at most max-content.


<a id="type-sizevalue"></a>

## `SizeValue`

Namespace: `Nazor.Style`

```csharp
public readonly record struct SizeValue(SizeKind Kind, Length Length)
```

A size that may also be `auto`, `none` (for maxima), `content` (for flex-basis) or an intrinsic keyword (`min-content`, `max-content`, `fit-content`).

| Parameter | Description |
| --- | --- |
| `Kind` | The size kind represented by this value. |
| `Length` | The absolute, relative, or percentage length to use. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
public static readonly SizeValue Auto ;
```

Automatic sizing determined by the layout algorithm.

#### `None`

```csharp
public static readonly SizeValue None ;
```

An unconstrained maximum size.

#### `Content`

```csharp
public static readonly SizeValue Content ;
```

Content-based sizing for a flex item's basis.

#### `MinContent`

```csharp
public static readonly SizeValue MinContent ;
```

Sizing at the smallest intrinsic content width.

#### `MaxContent`

```csharp
public static readonly SizeValue MaxContent ;
```

Sizing at the unwrapped intrinsic content width.

#### `FitContent`

```csharp
public static readonly SizeValue FitContent ;
```

Sizing clamped between minimum-content and maximum-content dimensions.

#### `IsIntrinsic`

```csharp
public bool IsIntrinsic {get; }
```

Gets whether the size uses minimum-content, maximum-content, or fit-content sizing.

#### `Fixed`

```csharp
public static SizeValue Fixed(Length length)
```

Creates a size constrained by the supplied length.

| Parameter | Description |
| --- | --- |
| `length` | The absolute, relative, or percentage length to use. |

Returns: The resulting SizeValue value.

#### `IsFixed`

```csharp
public bool IsFixed {get; }
```

Gets whether this size is represented by a concrete length.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-skewfunction"></a>

## `SkewFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record SkewFunction(double X, double Y) : TransformFunction
```

`skew()`, `skewX()`, `skewY()`.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-specificity"></a>

## `Specificity`

Namespace: `Nazor.Style`

```csharp
public readonly record struct Specificity(int Ids, int Classes, int Types) : IComparable<Specificity>
```

CSS specificity: identifiers, then classes and pseudo-classes, then types.

| Parameter | Description |
| --- | --- |
| `Ids` | The ids (int). |
| `Classes` | The class names required by the compound selector. |
| `Types` | The types (int). |

[Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

### Members

#### `Zero`

```csharp
public static readonly Specificity Zero ;
```

The specificity with all components set to zero.

#### `CompareTo`

```csharp
public int CompareTo(Specificity other)
```

Compares selector specificity lexicographically by identifier, class, and type counts.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: A negative value, zero, or a positive value when this specificity is less than, equal to, or greater than the other value.

#### `operator +`

```csharp
public static Specificity operator +(Specificity a, Specificity b)
```

Adds the corresponding components of the two operands.

| Parameter | Description |
| --- | --- |
| `a` | The a (Specificity). |
| `b` | The blue channel in the range from zero to 255. |

Returns: The resulting Specificity value.

#### `Max`

```csharp
public static Specificity Max(Specificity a, Specificity b)
```

Returns the greater of two selector specificities.

| Parameter | Description |
| --- | --- |
| `a` | The a (Specificity). |
| `b` | The blue channel in the range from zero to 255. |

Returns: The operand with the greater specificity.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-styledeclaration"></a>

## `StyleDeclaration`

Namespace: `Nazor.Style`

```csharp
public abstract class StyleDeclaration
```

A declaration as compiled from NSS.

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)


<a id="type-styleeffects"></a>

## `StyleEffects`

Namespace: `Nazor.Style`

```csharp
[Flags] public enum StyleEffects
```

What has to be redone when a computed value changes.

[Source](../../Sources/Core/Nazor.Style/Properties/StyleProperty.cs)

### Members

#### `None`

```csharp
None = 0
```

Requires no layout, paint, or compositing invalidation.

#### `Layout`

```csharp
Layout = 1
```

Geometry of the element and possibly of its neighbours.

#### `Paint`

```csharp
Paint = 2
```

Redraw of the element without geometry changes.

#### `Composite`

```csharp
Composite = 4
```

Visual update that a renderer may composite without repainting (opacity).


<a id="type-styleengine"></a>

## `StyleEngine`

Namespace: `Nazor.Style`

```csharp
public sealed class StyleEngine : IDisposable
```

Resolves the computed style of every element of a renderer and keeps it up to date after render batches, interaction-state changes, stylesheet replacements and viewport changes (for media queries and viewport units). The cascade sorts matching rules by level (theme, application, window, component-scoped), then specificity, nearest scope and source order. Scoped rules target declared elements; `::deep` also reaches descendants of a declared anchor. Only `var()` results are parsed per element.

[Source](../../Sources/Core/Nazor.Style/Engine/StyleEngine.cs)

### Members

#### `SystemButtonsHeight`

```csharp
public float? SystemButtonsHeight { get ; set ; }
```

Measured integrated control height in logical pixels; null when unavailable.

#### `StyleEngine`

```csharp
public StyleEngine(Renderer renderer)
```

Initializes a new instance of StyleEngine.

| Parameter | Description |
| --- | --- |
| `renderer` | The renderer that owns the component tree. |

#### `StyleChanged`

```csharp
public event Action<ElementNode, StyleEffects>? StyleChanged;
```

Raised by `Update()` for each element whose computed values changed.

#### `Theme`

```csharp
public StyleSheet? Theme { get ; set ; }
```

Gets or sets the theme stylesheet at the lowest cascade level.

#### `Application`

```csharp
public StyleSheet? Application { get ; set ; }
```

Gets or sets the application-wide stylesheet.

#### `Window`

```csharp
public StyleSheet? Window { get ; set ; }
```

Gets or sets the stylesheet local to this window.

#### `Viewport`

```csharp
public Viewport Viewport { get ; set ; }
```

Gets or sets the logical viewport dimensions used for media queries and relative units.

#### `Preferences`

```csharp
public MediaPreferences Preferences { get ; set ; }
```

The color scheme and reduced motion tested by `@media (prefers-…)`.

#### `IsDirty`

```csharp
public bool IsDirty {get; }
```

True when an `Update()` would recompute something, including after a sheet was replaced in the `StyleRegistry`.

#### `InvalidateAll`

```csharp
public void InvalidateAll()
```

Recomputes every element at the next `Update()`, for example after a hot reload delta.

#### `LastRecomputedCount`

```csharp
public int LastRecomputedCount { get; private set;}
```

Elements whose style was recomputed by the last `Update()`.

#### `TotalRecomputedCount`

```csharp
public long TotalRecomputedCount { get; private set; }
```

Gets the total elements recomputed over this engine's lifetime, including lazy style lookups and recomputations that preserve the same values.

#### `GetStyle`

```csharp
public ComputedStyle GetStyle(ElementNode element)
```

The element's computed style, computing pending changes first.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting ComputedStyle value.

#### `GetPseudoStyle`

```csharp
public ComputedStyle GetPseudoStyle(ElementNode element, PseudoElement target)
```

Resolve a pseudo-element against the originating `element`'s selectors, component scope, media and inherited values. The origin's inline declarations do not apply directly to the pseudo-element. `None` returns the ordinary style. Throws `ArgumentOutOfRangeException` for an undefined target. Calls compute pending style updates on the renderer's thread.

#### `GetGeneratedElement`

```csharp
public ElementNode? GetGeneratedElement(ElementNode element, PseudoElement target)
```

Return a retained decorative before/after box, or null when `content` suppresses generation or `display` is none. Its parent is the originating element, but it is excluded from authored children and accessibility semantics. Identity is retained across text/style changes. Throws `ArgumentOutOfRangeException` for targets other than Before/After. Scene rendering suppresses generation on editable and replaced image elements. Calls compute pending styles on the renderer's thread.

#### `Update`

```csharp
public void Update()
```

Recomputes dirty elements top-down and raises `StyleChanged`.

#### `ResolveKeyframes`

```csharp
public IReadOnlyList<ResolvedKeyframe>? ResolveKeyframes(ElementNode element, string name)
```

The `@keyframes` rule named `name` for an element, its values computed for it: em, rem and viewport units converted, `var()` substituted, `currentColor` resolved, `inherit` and `initial` applied. The scoped sheet of the element comes first, then the window, application and theme levels; null when no sheet has the name.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting IReadOnlyList<ResolvedKeyframe>? value.

Throws `ArgumentNullException`: element, name is null.

#### `Trace`

```csharp
public StyleTrace Trace(ElementNode element)
```

How the style of an element came to be, for developer tools: the cascade made again from the sheets on request (never during a frame), keeping what `Compute` throws away.

| Parameter | Description |
| --- | --- |
| `element` | The element to inspect or update. |

Returns: The resulting StyleTrace value.

#### `Dispose`

```csharp
public void Dispose()
```

Releases the resources and event subscriptions owned by this instance.


<a id="type-stylehotreload"></a>

## `StyleHotReload`

Namespace: `Nazor.Style`

```csharp
public static class StyleHotReload
```

Metadata update handler of Nazor.Style: after a .NET Hot Reload delta the scoped sheet factories of the updated component types are re-run (their body may have changed) and the live sheets are replaced in place through `StyleRegistry.Refresh`. Runs on an arbitrary thread, never throws; failures are kept in `LastError` for the host to log.

[Source](../../Sources/Core/Nazor.Style/Engine/StyleHotReload.cs)

### Members

#### `RegistrationMethod`

```csharp
public const string RegistrationMethod = "__NazorRegisterStyleSheet";
```

The static method generated in a component class to register its scoped sheet.

#### `GenericRegistrationClassPrefix`

```csharp
public const string GenericRegistrationClassPrefix = "__NazorStyles_";
```

The prefix of the static class generated next to a generic component to hold its registration (a generic type cannot declare a module initializer): `__NazorStyles_ListView` for `ListView<T>`.

#### `LastError`

```csharp
public static Exception? LastError { get; private set;}
```

The first failure of the last delta, or null; reset at every delta.


<a id="type-stylelevel"></a>

## `StyleLevel`

Namespace: `Nazor.Style`

```csharp
public enum StyleLevel
```

Cascade levels, from the weakest to the strongest.

[Source](../../Sources/Core/Nazor.Style/Engine/StyleEngine.cs)

### Members

#### `Theme`

```csharp
Theme
```

Applies theme defaults at the lowest stylesheet cascade level.

#### `Application`

```csharp
Application
```

Applies application-wide stylesheet rules above the theme level.

#### `Window`

```csharp
Window
```

Applies window-local stylesheet rules above application rules.

#### `Component`

```csharp
Component
```

Applies component-scoped stylesheet rules at the highest stylesheet level.


<a id="type-styleproperties"></a>

## `StyleProperties`

Namespace: `Nazor.Style`

```csharp
public static class StyleProperties
```

Provides the registered NSS longhand properties. Each descriptor specifies its grammar, initial value, inheritance, and invalidation effects. Unsupported property names are rejected during stylesheet compilation.

[Source](../../Sources/Core/Nazor.Style/Properties/StyleProperties.cs)

### Members

#### `Display`

```csharp
public static readonly StyleProperty<Display> Display ;
```

Gets the descriptor for the display NSS property, including its grammar and initial value.

#### `FlexDirection`

```csharp
public static readonly StyleProperty<FlexDirection> FlexDirection ;
```

Gets the descriptor for the flex-direction NSS property, including its grammar and initial value.

#### `FlexWrap`

```csharp
public static readonly StyleProperty<FlexWrap> FlexWrap ;
```

Gets the descriptor for the flex-wrap NSS property, including its grammar and initial value.

#### `JustifyContent`

```csharp
public static readonly StyleProperty<JustifyContent> JustifyContent ;
```

Gets the descriptor for the justify-content NSS property, including its grammar and initial value.

#### `AlignContent`

```csharp
public static readonly StyleProperty<AlignContent> AlignContent ;
```

Gets the descriptor for the align-content NSS property, including its grammar and initial value.

#### `AlignItems`

```csharp
public static readonly StyleProperty<AlignItems> AlignItems ;
```

Gets the descriptor for the align-items NSS property, including its grammar and initial value.

#### `AlignSelf`

```csharp
public static readonly StyleProperty<AlignSelf> AlignSelf ;
```

Gets the descriptor for the align-self NSS property, including its grammar and initial value.

#### `FlexGrow`

```csharp
public static readonly StyleProperty<double> FlexGrow ;
```

Gets the descriptor for the flex-grow NSS property, including its grammar and initial value.

#### `FlexShrink`

```csharp
public static readonly StyleProperty<double> FlexShrink ;
```

Gets the descriptor for the flex-shrink NSS property, including its grammar and initial value.

#### `FlexBasis`

```csharp
public static readonly StyleProperty<SizeValue> FlexBasis ;
```

Gets the descriptor for the flex-basis NSS property, including its grammar and initial value.

#### `Order`

```csharp
public static readonly StyleProperty<int> Order ;
```

Gets the descriptor for the order NSS property, including its grammar and initial value.

#### `GridTemplateColumns`

```csharp
public static readonly StyleProperty<GridTrackList> GridTemplateColumns ;
```

Gets the descriptor for the grid-template-columns NSS property, including its grammar and initial value.

#### `GridTemplateRows`

```csharp
public static readonly StyleProperty<GridTrackList> GridTemplateRows ;
```

Gets the descriptor for the grid-template-rows NSS property, including its grammar and initial value.

#### `GridTemplateAreas`

```csharp
public static readonly StyleProperty<GridAreas> GridTemplateAreas ;
```

Gets the descriptor for the grid-template-areas NSS property, including its grammar and initial value.

#### `GridAutoColumns`

```csharp
public static readonly StyleProperty<ValueList<TrackSize>> GridAutoColumns ;
```

Gets the descriptor for the grid-auto-columns NSS property, including its grammar and initial value.

#### `GridAutoRows`

```csharp
public static readonly StyleProperty<ValueList<TrackSize>> GridAutoRows ;
```

Gets the descriptor for the grid-auto-rows NSS property, including its grammar and initial value.

#### `GridAutoFlow`

```csharp
public static readonly StyleProperty<GridAutoFlow> GridAutoFlow ;
```

Gets the descriptor for the grid-auto-flow NSS property, including its grammar and initial value.

#### `GridRowStart`

```csharp
public static readonly StyleProperty<GridLine> GridRowStart ;
```

Gets the descriptor for the grid-row-start NSS property, including its grammar and initial value.

#### `GridRowEnd`

```csharp
public static readonly StyleProperty<GridLine> GridRowEnd ;
```

Gets the descriptor for the grid-row-end NSS property, including its grammar and initial value.

#### `GridColumnStart`

```csharp
public static readonly StyleProperty<GridLine> GridColumnStart ;
```

Gets the descriptor for the grid-column-start NSS property, including its grammar and initial value.

#### `GridColumnEnd`

```csharp
public static readonly StyleProperty<GridLine> GridColumnEnd ;
```

Gets the descriptor for the grid-column-end NSS property, including its grammar and initial value.

#### `JustifyItems`

```csharp
public static readonly StyleProperty<AlignItems> JustifyItems ;
```

Gets the descriptor for the justify-items NSS property, including its grammar and initial value.

#### `JustifySelf`

```csharp
public static readonly StyleProperty<AlignSelf> JustifySelf ;
```

Gets the descriptor for the justify-self NSS property, including its grammar and initial value.

#### `RowGap`

```csharp
public static readonly StyleProperty<Length> RowGap ;
```

Gets the descriptor for the row-gap NSS property, including its grammar and initial value.

#### `ColumnGap`

```csharp
public static readonly StyleProperty<Length> ColumnGap ;
```

Gets the descriptor for the column-gap NSS property, including its grammar and initial value.

#### `Width`

```csharp
public static readonly StyleProperty<SizeValue> Width ;
```

Gets the descriptor for the width NSS property, including its grammar and initial value.

#### `Height`

```csharp
public static readonly StyleProperty<SizeValue> Height ;
```

Gets the descriptor for the height NSS property, including its grammar and initial value.

#### `MinWidth`

```csharp
public static readonly StyleProperty<SizeValue> MinWidth ;
```

Gets the descriptor for the min-width NSS property, including its grammar and initial value.

#### `MinHeight`

```csharp
public static readonly StyleProperty<SizeValue> MinHeight ;
```

Gets the descriptor for the min-height NSS property, including its grammar and initial value.

#### `MaxWidth`

```csharp
public static readonly StyleProperty<SizeValue> MaxWidth ;
```

Gets the descriptor for the max-width NSS property, including its grammar and initial value.

#### `MaxHeight`

```csharp
public static readonly StyleProperty<SizeValue> MaxHeight ;
```

Gets the descriptor for the max-height NSS property, including its grammar and initial value.

#### `PaddingTop`

```csharp
public static readonly StyleProperty<Length> PaddingTop ;
```

Gets the descriptor for the padding-top NSS property, including its grammar and initial value.

#### `PaddingRight`

```csharp
public static readonly StyleProperty<Length> PaddingRight ;
```

Gets the descriptor for the padding-right NSS property, including its grammar and initial value.

#### `PaddingBottom`

```csharp
public static readonly StyleProperty<Length> PaddingBottom ;
```

Gets the descriptor for the padding-bottom NSS property, including its grammar and initial value.

#### `PaddingLeft`

```csharp
public static readonly StyleProperty<Length> PaddingLeft ;
```

Gets the descriptor for the padding-left NSS property, including its grammar and initial value.

#### `MarginTop`

```csharp
public static readonly StyleProperty<SizeValue> MarginTop ;
```

Gets the descriptor for the margin-top NSS property, including its grammar and initial value.

#### `MarginRight`

```csharp
public static readonly StyleProperty<SizeValue> MarginRight ;
```

Gets the descriptor for the margin-right NSS property, including its grammar and initial value.

#### `MarginBottom`

```csharp
public static readonly StyleProperty<SizeValue> MarginBottom ;
```

Gets the descriptor for the margin-bottom NSS property, including its grammar and initial value.

#### `MarginLeft`

```csharp
public static readonly StyleProperty<SizeValue> MarginLeft ;
```

Gets the descriptor for the margin-left NSS property, including its grammar and initial value.

#### `BorderTopWidth`

```csharp
public static readonly StyleProperty<Length> BorderTopWidth ;
```

Gets the descriptor for the border-top-width NSS property, including its grammar and initial value.

#### `BorderRightWidth`

```csharp
public static readonly StyleProperty<Length> BorderRightWidth ;
```

Gets the descriptor for the border-right-width NSS property, including its grammar and initial value.

#### `BorderBottomWidth`

```csharp
public static readonly StyleProperty<Length> BorderBottomWidth ;
```

Gets the descriptor for the border-bottom-width NSS property, including its grammar and initial value.

#### `BorderLeftWidth`

```csharp
public static readonly StyleProperty<Length> BorderLeftWidth ;
```

Gets the descriptor for the border-left-width NSS property, including its grammar and initial value.

#### `BoxSizing`

```csharp
public static readonly StyleProperty<BoxSizing> BoxSizing ;
```

Gets the descriptor for the box-sizing NSS property, including its grammar and initial value.

#### `OverflowX`

```csharp
public static readonly StyleProperty<Overflow> OverflowX ;
```

Gets the descriptor for the overflow-x NSS property, including its grammar and initial value.

#### `AspectRatio`

```csharp
public static readonly StyleProperty<AspectRatio> AspectRatio ;
```

Gets the descriptor for the aspect-ratio NSS property, including its grammar and initial value.

#### `OverflowY`

```csharp
public static readonly StyleProperty<Overflow> OverflowY ;
```

Gets the descriptor for the overflow-y NSS property, including its grammar and initial value.

#### Scrolling properties

```csharp
public static readonly StyleProperty<ScrollBehavior> ScrollBehavior;
public static readonly StyleProperty<TimeSpan> ScrollDuration;
public static readonly StyleProperty<EasingFunction> ScrollTimingFunction;
public static readonly StyleProperty<ScrollWheelStep> ScrollWheelStep;
```

Inherited scroll request settings. `ScrollBehavior` is `Auto` (immediate, default) or `Smooth`; the default duration is 180ms and easing is ease-out. `ScrollWheelStep` is a value record `(ScrollWheelStepKind Kind, Length Length, double Lines)` with kinds `Auto`, `Length` and `Lines`; its default `Auto` follows the system. Relative lengths resolve in the container's style, and line counts use its computed line height. All offsets remain fractional; no line snapping is introduced. See [the scrolling property catalog](../Guide/19-NSS-Property-Reference.md#scrolling).

#### Scrollbar properties

All scrollbar descriptors are inherited and apply to both overflowing axes. Opacity, geometry and normal fills support standard transitions and keyframes. Pointer interaction selects the hover or active fill immediately. Timing, reveal distance and visibility control automatic fades and wheel behavior. Use global `:root` defaults or component-scoped overrides. [Scrollbar property catalog](../Guide/19-NSS-Property-Reference.md#scrollbars)

```csharp
public static readonly StyleProperty<ScrollbarVisibility> ScrollbarVisibility;
public static readonly StyleProperty<Length> ScrollbarWidth;
public static readonly StyleProperty<Color> ScrollbarThumbColor;
public static readonly StyleProperty<SizeValue> ScrollbarThumbWidth;
public static readonly StyleProperty<SizeValue> ScrollbarThumbExpandedWidth;
public static readonly StyleProperty<NativeColor> ScrollbarThumbHoverColor;
public static readonly StyleProperty<NativeColor> ScrollbarThumbActiveColor;
public static readonly StyleProperty<Length> ScrollbarThumbBorderWidth;
public static readonly StyleProperty<Color> ScrollbarThumbBorderColor;
public static readonly StyleProperty<NativeColor> ScrollbarThumbBorderHighlightColor;
public static readonly StyleProperty<Color> ScrollbarTrackColor;
public static readonly StyleProperty<NativeColor> ScrollbarTrackHoverColor;
public static readonly StyleProperty<Length> ScrollbarRadius;
public static readonly StyleProperty<Length> ScrollbarInset;
public static readonly StyleProperty<Length> ScrollbarMinThumbLength;
public static readonly StyleProperty<double> ScrollbarOpacity;
public static readonly StyleProperty<TimeSpan> ScrollbarFadeInDuration;
public static readonly StyleProperty<TimeSpan> ScrollbarFadeOutDuration;
public static readonly StyleProperty<TimeSpan> ScrollbarFadeOutDelay;
public static readonly StyleProperty<EasingFunction> ScrollbarFadeTimingFunction;
public static readonly StyleProperty<TimeSpan> ScrollbarWheelTimeout;
public static readonly StyleProperty<Length> ScrollbarRevealDistance;
public static readonly StyleProperty<ScrollbarPlacement> ScrollbarPlacement;
public static readonly StyleProperty<SizeValue> ScrollbarTrackRadius;
public static readonly StyleProperty<Length> ScrollbarTrackBorderWidth;
public static readonly StyleProperty<Color> ScrollbarTrackBorderColor;
public static readonly StyleProperty<bool> ScrollbarButtons;
public static readonly StyleProperty<Length> ScrollbarButtonLength;
public static readonly StyleProperty<Color> ScrollbarButtonColor;
public static readonly StyleProperty<Color> ScrollbarButtonBackground;
public static readonly StyleProperty<Color> ScrollbarButtonHoverBackground;
public static readonly StyleProperty<Color> ScrollbarButtonActiveBackground;
public static readonly StyleProperty<Color> ScrollbarButtonDisabledColor;
public static readonly StyleProperty<Length> ScrollbarButtonRadius;
public static readonly StyleProperty<Length> ScrollbarButtonStep;
public static readonly StyleProperty<TimeSpan> ScrollbarButtonRepeatDelay;
public static readonly StyleProperty<TimeSpan> ScrollbarButtonRepeatInterval;
```

#### `Position`

```csharp
public static readonly StyleProperty<Position> Position ;
```

Gets the descriptor for the position NSS property, including its grammar and initial value.

#### `Top`

```csharp
public static readonly StyleProperty<SizeValue> Top ;
```

Gets the descriptor for the top NSS property, including its grammar and initial value.

#### `Right`

```csharp
public static readonly StyleProperty<SizeValue> Right ;
```

Gets the descriptor for the right NSS property, including its grammar and initial value.

#### `Bottom`

```csharp
public static readonly StyleProperty<SizeValue> Bottom ;
```

Gets the descriptor for the bottom NSS property, including its grammar and initial value.

#### `Left`

```csharp
public static readonly StyleProperty<SizeValue> Left ;
```

Gets the descriptor for the left NSS property, including its grammar and initial value.

#### `ZIndex`

```csharp
public static readonly StyleProperty<ZIndex> ZIndex ;
```

Gets the descriptor for the z-index NSS property, including its grammar and initial value.

#### `BackgroundImage`

```csharp
public static readonly StyleProperty<ValueList<ImageValue>> BackgroundImage ;
```

Gets the descriptor for the background-image NSS property, including its grammar and initial value.

#### Gradient borders, focus and vector strokes

The following additional paint descriptors support gradient borders and vector motion:

| Member | Descriptor type | Contract |
| --- | --- | --- |
| `BorderImageSource` | `StyleProperty<ValueList<ImageValue>>` | One gradient spanning the border ring, or `none`; URL images and multiple gradients are rejected. |
| `BackgroundClip` | `StyleProperty<bool>` | `false` for `border-box`, `true` for `padding-box`; initial `false`. |
| `OutlineInnerWidth` | `StyleProperty<Length>` | Nonnegative inner outline width, initially zero; animatable. |
| `OutlineInnerColor` | `StyleProperty<Color>` | Inner outline color, initially transparent; animatable. |
| `StrokeWidth` | `StyleProperty<Length>` | Check, chevron and ring stroke width; percentages use the smaller content dimension; zero selects the shape default; animatable. |
| `StrokeRadius` | `StyleProperty<Length>` | Ring radius or border-path inset basis, including percentages of the smaller dimension; zero selects the shape default. Ignored by open glyphs; animatable. |
| `RingShape` | `StyleProperty<RingShape>` | Circle by default, or a rounded border-box path following border-radius clockwise by path length. |
| `StrokeTrimStart` | `StyleProperty<double>` | Stroke start fraction in `[0,1]`, initially zero; animatable. |
| `StrokeTrimEnd` | `StyleProperty<double>` | Stroke end fraction in `[0,1]`, initially one; animatable. |

These descriptors are not inherited. They invalidate paint without recomputing layout. An inner outline adjoins the inside of the primary outline, without changing layout or the primary outline's outset. Check and chevron trim fractions measure cumulative polyline length and preserve intervening corners. Ring trim fractions multiply the control's determinate progress fraction. End values at or below the start paint no stroke. Open glyphs retain their automatic width when `StrokeWidth` is zero; explicit stroke ink is included in culling and damage. Font glyphs, images and ordinary borders are unaffected. See [vector glyph strokes](../Guide/19-NSS-Property-Reference.md#vector-glyph-strokes) for an NSS example.

#### `BackgroundPosition`

```csharp
public static readonly StyleProperty<ValueList<PositionValue>> BackgroundPosition ;
```

Gets the descriptor for the background-position NSS property, including its grammar and initial value.

#### `BackgroundSize`

```csharp
public static readonly StyleProperty<ValueList<BackgroundSize>> BackgroundSize ;
```

Gets the descriptor for the background-size NSS property, including its grammar and initial value.

#### `BackgroundRepeat`

```csharp
public static readonly StyleProperty<ValueList<BackgroundRepeat>> BackgroundRepeat ;
```

Gets the descriptor for the background-repeat NSS property, including its grammar and initial value.

#### `BackgroundColor`

```csharp
public static readonly StyleProperty<Color> BackgroundColor ;
```

Gets the descriptor for the background-color NSS property, including its grammar and initial value.

#### `BorderTopColor`

```csharp
public static readonly StyleProperty<Color> BorderTopColor ;
```

Gets the descriptor for the border-top-color NSS property, including its grammar and initial value.

#### `BorderRightColor`

```csharp
public static readonly StyleProperty<Color> BorderRightColor ;
```

Gets the descriptor for the border-right-color NSS property, including its grammar and initial value.

#### `BorderBottomColor`

```csharp
public static readonly StyleProperty<Color> BorderBottomColor ;
```

Gets the descriptor for the border-bottom-color NSS property, including its grammar and initial value.

#### `BorderLeftColor`

```csharp
public static readonly StyleProperty<Color> BorderLeftColor ;
```

Gets the descriptor for the border-left-color NSS property, including its grammar and initial value.

#### `BorderTopStyle`

```csharp
public static readonly StyleProperty<BorderStyle> BorderTopStyle ;
```

Gets the descriptor for the border-top-style NSS property, including its grammar and initial value.

#### `BorderRightStyle`

```csharp
public static readonly StyleProperty<BorderStyle> BorderRightStyle ;
```

Gets the descriptor for the border-right-style NSS property, including its grammar and initial value.

#### `BorderBottomStyle`

```csharp
public static readonly StyleProperty<BorderStyle> BorderBottomStyle ;
```

Gets the descriptor for the border-bottom-style NSS property, including its grammar and initial value.

#### `BorderLeftStyle`

```csharp
public static readonly StyleProperty<BorderStyle> BorderLeftStyle ;
```

Gets the descriptor for the border-left-style NSS property, including its grammar and initial value.

#### `BorderTopLeftRadius`

```csharp
public static readonly StyleProperty<RadiusValue> BorderTopLeftRadius ;
```

Gets the descriptor for the border-top-left-radius NSS property, including its grammar and initial value.

#### `BorderTopRightRadius`

```csharp
public static readonly StyleProperty<RadiusValue> BorderTopRightRadius ;
```

Gets the descriptor for the border-top-right-radius NSS property, including its grammar and initial value.

#### `BorderBottomRightRadius`

```csharp
public static readonly StyleProperty<RadiusValue> BorderBottomRightRadius ;
```

Gets the descriptor for the border-bottom-right-radius NSS property, including its grammar and initial value.

#### `BorderBottomLeftRadius`

```csharp
public static readonly StyleProperty<RadiusValue> BorderBottomLeftRadius ;
```

Gets the descriptor for the border-bottom-left-radius NSS property, including its grammar and initial value.

#### `OutlineWidth`

```csharp
public static readonly StyleProperty<Length> OutlineWidth ;
```

Gets the descriptor for the outline-width NSS property, including its grammar and initial value.

#### `OutlineStyle`

```csharp
public static readonly StyleProperty<BorderStyle> OutlineStyle ;
```

Gets the descriptor for the outline-style NSS property, including its grammar and initial value.

#### `OutlineColor`

```csharp
public static readonly StyleProperty<Color> OutlineColor ;
```

Gets the descriptor for the outline-color NSS property, including its grammar and initial value.

#### `OutlineOffset`

```csharp
public static readonly StyleProperty<Length> OutlineOffset ;
```

Gets the descriptor for the outline-offset NSS property, including its grammar and initial value.

#### `BoxShadow`

```csharp
public static readonly StyleProperty<ValueList<BoxShadow>> BoxShadow ;
```

Gets the descriptor for the box-shadow NSS property, including its grammar and initial value.

#### `Opacity`

```csharp
public static readonly StyleProperty<double> Opacity ;
```

Gets the descriptor for the opacity NSS property, including its grammar and initial value.

#### `ObjectFit`

```csharp
public static readonly StyleProperty<ObjectFit> ObjectFit ;
```

Gets the descriptor for the object-fit NSS property, including its grammar and initial value.

#### `ObjectPosition`

```csharp
public static readonly StyleProperty<PositionValue> ObjectPosition ;
```

Gets the descriptor for the object-position NSS property, including its grammar and initial value.

#### `Transform`

```csharp
public static readonly StyleProperty<ValueList<TransformFunction>> Transform ;
```

Gets the descriptor for the transform NSS property, including its grammar and initial value.

#### `TransformOrigin`

```csharp
public static readonly StyleProperty<TransformOrigin> TransformOrigin ;
```

Gets the descriptor for the transform-origin NSS property, including its grammar and initial value.

#### `ClipPath`

```csharp
public static readonly StyleProperty<ClipPathValue> ClipPath;
```

Gets the non-inherited border-box inset clip with optional elliptical corners. Initial value: `none`. NSS accepts `none | inset(<length-percentage>{1,4} [round <border-radius>]?)`. Insets and corner radii animate without layout work; `none` changes discretely. See [rectangular clipping](../Guide/19-NSS-Property-Reference.md#rectangular-clipping).

#### `Visibility`

```csharp
public static readonly StyleProperty<Visibility> Visibility ;
```

Gets the descriptor for the visibility NSS property, including its grammar and initial value.

#### `PointerEvents`

```csharp
public static readonly StyleProperty<PointerEvents> PointerEvents ;
```

Gets the descriptor for the pointer-events NSS property, including its grammar and initial value.

#### `UserSelect`

```csharp
public static readonly StyleProperty<UserSelect> UserSelect ;
```

Gets the descriptor for the user-select NSS property, including its grammar and initial value.

#### `Cursor`

```csharp
public static readonly StyleProperty<Cursor> Cursor ;
```

Gets the descriptor for the cursor NSS property, including its grammar and initial value.

#### `WindowsTitleBarHeight`

```csharp
public static readonly StyleProperty<WindowsTitleBarHeight> WindowsTitleBarHeight ;
```

Gets the descriptor for the -windows-title-bar-height NSS property, including its grammar and initial value.



#### `WindowsTitleBarBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarBackgroundColor;
```

Descriptor for `-windows-title-bar-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarForegroundColor;
```

Descriptor for `-windows-title-bar-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarInactiveBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarInactiveBackgroundColor;
```

Descriptor for `-windows-title-bar-inactive-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarInactiveForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarInactiveForegroundColor;
```

Descriptor for `-windows-title-bar-inactive-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonBackgroundColor;
```

Descriptor for `-windows-title-bar-button-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonForegroundColor;
```

Descriptor for `-windows-title-bar-button-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonInactiveBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonInactiveBackgroundColor;
```

Descriptor for `-windows-title-bar-button-inactive-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonInactiveForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonInactiveForegroundColor;
```

Descriptor for `-windows-title-bar-button-inactive-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonHoverBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonHoverBackgroundColor;
```

Descriptor for `-windows-title-bar-button-hover-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonHoverForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonHoverForegroundColor;
```

Descriptor for `-windows-title-bar-button-hover-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonPressedBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonPressedBackgroundColor;
```

Descriptor for `-windows-title-bar-button-pressed-background-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

#### `WindowsTitleBarButtonPressedForegroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsTitleBarButtonPressedForegroundColor;
```

Descriptor for `-windows-title-bar-button-pressed-foreground-color`, with syntax `auto | <color>`, initial value `auto`, no inheritance and paint effects. Applies only to the root `Window` on Windows 11.

The twelve descriptors map to [WindowsTitleBarColors](Nazor.Platform.md#type-windowstitlebarcolors). In custom caption mode only button colors affect native controls. See the [native property reference](../Guide/19-NSS-Property-Reference.md#windows-native-properties) for platform restrictions and precedence over existing TitleBar control defaults.

#### `WindowsBadgeBorderTopWidth`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeBorderTopWidth;
```

Descriptor for `-windows-badge-border-top-width`: `thin | medium | thick | <length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderTopStyle`

```csharp
public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderTopStyle;
```

Descriptor for `-windows-badge-border-top-style`: `none | solid | dashed | dotted`, initially `Solid`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderTopColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeBorderTopColor;
```

Descriptor for `-windows-badge-border-top-color`: `<color>`, initially `currentColor`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderRightWidth`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeBorderRightWidth;
```

Descriptor for `-windows-badge-border-right-width`: `thin | medium | thick | <length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderRightStyle`

```csharp
public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderRightStyle;
```

Descriptor for `-windows-badge-border-right-style`: `none | solid | dashed | dotted`, initially `Solid`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderRightColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeBorderRightColor;
```

Descriptor for `-windows-badge-border-right-color`: `<color>`, initially `currentColor`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderBottomWidth`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeBorderBottomWidth;
```

Descriptor for `-windows-badge-border-bottom-width`: `thin | medium | thick | <length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderBottomStyle`

```csharp
public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderBottomStyle;
```

Descriptor for `-windows-badge-border-bottom-style`: `none | solid | dashed | dotted`, initially `Solid`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderBottomColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeBorderBottomColor;
```

Descriptor for `-windows-badge-border-bottom-color`: `<color>`, initially `currentColor`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderLeftWidth`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeBorderLeftWidth;
```

Descriptor for `-windows-badge-border-left-width`: `thin | medium | thick | <length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderLeftStyle`

```csharp
public static readonly StyleProperty<BorderStyle> WindowsBadgeBorderLeftStyle;
```

Descriptor for `-windows-badge-border-left-style`: `none | solid | dashed | dotted`, initially `Solid`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderLeftColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeBorderLeftColor;
```

Descriptor for `-windows-badge-border-left-color`: `<color>`, initially `currentColor`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundImage`

```csharp
public static readonly StyleProperty<ValueList<ImageValue>> WindowsBadgeBackgroundImage;
```

Descriptor for `-windows-badge-background-image`: `none | [<url> | <gradient>]#`, initially `none`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundPosition`

```csharp
public static readonly StyleProperty<ValueList<PositionValue>> WindowsBadgeBackgroundPosition;
```

Descriptor for `-windows-badge-background-position`: `[[left | center | right | top | bottom | <length-percentage>]{1,2}]#`, initially `0% 0%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundSize`

```csharp
public static readonly StyleProperty<ValueList<BackgroundSize>> WindowsBadgeBackgroundSize;
```

Descriptor for `-windows-badge-background-size`: `[cover | contain | [auto | <length-percentage>]{1,2}]#`, initially `auto`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundRepeat`

```csharp
public static readonly StyleProperty<ValueList<BackgroundRepeat>> WindowsBadgeBackgroundRepeat;
```

Descriptor for `-windows-badge-background-repeat`: `[repeat | no-repeat | repeat-x | repeat-y]#`, initially `repeat`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundClip`

```csharp
public static readonly StyleProperty<bool> WindowsBadgeBackgroundClip;
```

Descriptor for `-windows-badge-background-clip`: `border-box | padding-box`, initially `border-box`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTransform`

```csharp
public static readonly StyleProperty<ValueList<TransformFunction>> WindowsBadgeTransform;
```

Descriptor for `-windows-badge-transform`: `none | <transform-function>+`, initially `none`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTransformOrigin`

```csharp
public static readonly StyleProperty<TransformOrigin> WindowsBadgeTransformOrigin;
```

Descriptor for `-windows-badge-transform-origin`: `[left | center | right | top | bottom | <length-percentage>]{1,2}`, initially `50% 50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeMaskImage`

```csharp
public static readonly StyleProperty<ValueList<ImageValue>> WindowsBadgeMaskImage;
```

Descriptor for `-windows-badge-mask-image`: `none | <url>`, initially `none`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsBadgeBackgroundColor;
```

Descriptor for `-windows-badge-background-color`: `auto | <color>`, initially `auto`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeColor;
```

Descriptor for `-windows-badge-color`: `<color>`, initially `#ffffff`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeSize`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeSize;
```

Descriptor for `-windows-badge-size`: `<length-percentage>`, initially `16px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeWidth`

```csharp
public static readonly StyleProperty<SizeValue> WindowsBadgeWidth;
```

Descriptor for `-windows-badge-width`: `auto | <length-percentage>`, initially `auto`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeHeight`

```csharp
public static readonly StyleProperty<SizeValue> WindowsBadgeHeight;
```

Descriptor for `-windows-badge-height`: `auto | <length-percentage>`, initially `auto`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeOffsetX`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeOffsetX;
```

Descriptor for `-windows-badge-offset-x`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeOffsetY`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeOffsetY;
```

Descriptor for `-windows-badge-offset-y`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeOpacity`

```csharp
public static readonly StyleProperty<double> WindowsBadgeOpacity;
```

Descriptor for `-windows-badge-opacity`: `<number>`, initially `1`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgePaddingTop`

```csharp
public static readonly StyleProperty<Length> WindowsBadgePaddingTop;
```

Descriptor for `-windows-badge-padding-top`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgePaddingRight`

```csharp
public static readonly StyleProperty<Length> WindowsBadgePaddingRight;
```

Descriptor for `-windows-badge-padding-right`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgePaddingBottom`

```csharp
public static readonly StyleProperty<Length> WindowsBadgePaddingBottom;
```

Descriptor for `-windows-badge-padding-bottom`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgePaddingLeft`

```csharp
public static readonly StyleProperty<Length> WindowsBadgePaddingLeft;
```

Descriptor for `-windows-badge-padding-left`: `<length-percentage>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderTopLeftRadius`

```csharp
public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderTopLeftRadius;
```

Descriptor for `-windows-badge-border-top-left-radius`: `<length-percentage>{1,2}`, initially `50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderTopRightRadius`

```csharp
public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderTopRightRadius;
```

Descriptor for `-windows-badge-border-top-right-radius`: `<length-percentage>{1,2}`, initially `50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderBottomRightRadius`

```csharp
public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderBottomRightRadius;
```

Descriptor for `-windows-badge-border-bottom-right-radius`: `<length-percentage>{1,2}`, initially `50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBorderBottomLeftRadius`

```csharp
public static readonly StyleProperty<RadiusValue> WindowsBadgeBorderBottomLeftRadius;
```

Descriptor for `-windows-badge-border-bottom-left-radius`: `<length-percentage>{1,2}`, initially `50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeFontFamily`

```csharp
public static readonly StyleProperty<ValueList<string>> WindowsBadgeFontFamily;
```

Descriptor for `-windows-badge-font-family`: `<family-name>#`, initially `Segoe UI`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeFontSize`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeFontSize;
```

Descriptor for `-windows-badge-font-size`: `<length-percentage>`, initially `11.2px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeFontWeight`

```csharp
public static readonly StyleProperty<int> WindowsBadgeFontWeight;
```

Descriptor for `-windows-badge-font-weight`: `normal | bold | <number>`, initially `700`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeFontStyle`

```csharp
public static readonly StyleProperty<FontStyle> WindowsBadgeFontStyle;
```

Descriptor for `-windows-badge-font-style`: `normal | italic`, initially `Normal`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeLetterSpacing`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeLetterSpacing;
```

Descriptor for `-windows-badge-letter-spacing`: `normal | <length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTextAlign`

```csharp
public static readonly StyleProperty<TextAlign> WindowsBadgeTextAlign;
```

Descriptor for `-windows-badge-text-align`: `start | end | left | right | center`, initially `Center`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeVerticalAlign`

```csharp
public static readonly StyleProperty<double> WindowsBadgeVerticalAlign;
```

Descriptor for `-windows-badge-vertical-align`: `top | middle | bottom`, initially `0.5`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTextFit`

```csharp
public static readonly StyleProperty<bool> WindowsBadgeTextFit;
```

Descriptor for `-windows-badge-text-fit`: `shrink | none`, initially `True`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTextStrokeWidth`

```csharp
public static readonly StyleProperty<Length> WindowsBadgeTextStrokeWidth;
```

Descriptor for `-windows-badge-text-stroke-width`: `<length>`, initially `0px`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTextStrokeColor`

```csharp
public static readonly StyleProperty<Color> WindowsBadgeTextStrokeColor;
```

Descriptor for `-windows-badge-text-stroke-color`: `<color>`, initially `currentColor`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeImageBackgroundColor`

```csharp
public static readonly StyleProperty<NativeColor> WindowsBadgeImageBackgroundColor;
```

Descriptor for `-windows-badge-image-background-color`: `auto | <color>`, initially `auto`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeImageFit`

```csharp
public static readonly StyleProperty<ObjectFit> WindowsBadgeImageFit;
```

Descriptor for `-windows-badge-image-fit`: `fill | contain | cover | none | scale-down`, initially `Contain`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeImagePosition`

```csharp
public static readonly StyleProperty<PositionValue> WindowsBadgeImagePosition;
```

Descriptor for `-windows-badge-image-position`: `[left | center | right | top | bottom | <length-percentage>]{1,2}`, initially `50% 50%`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeImageOpacity`

```csharp
public static readonly StyleProperty<double> WindowsBadgeImageOpacity;
```

Descriptor for `-windows-badge-image-opacity`: `<number>`, initially `1`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeImageClip`

```csharp
public static readonly StyleProperty<bool> WindowsBadgeImageClip;
```

Descriptor for `-windows-badge-image-clip`: `none | border-box`, initially `False`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeBoxShadow`

```csharp
public static readonly StyleProperty<ValueList<BoxShadow>> WindowsBadgeBoxShadow;
```

Descriptor for `-windows-badge-box-shadow`: `none | [inset? && <length>{2,4} && <color>?]#`, initially `none`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

#### `WindowsBadgeTextShadow`

```csharp
public static readonly StyleProperty<ValueList<BoxShadow>> WindowsBadgeTextShadow;
```

Descriptor for `-windows-badge-text-shadow`: `none | [<length>{2,3} && <color>?]#`, initially `none`, with paint effects, no inheritance and no animation. Applies only to the root Window on Windows.

See the [badge property reference](../Guide/19-NSS-Property-Reference.md#windows-launcher-badges) for composition, units, all badge shorthands, alpha masks and application font ownership. Border width/style/color now expand to their four side descriptors.

#### `AppliesTo`

```csharp
public static bool AppliesTo(StyleProperty property, string elementName, bool isRoot)
```

Native declarations apply only to their owning element, including selectors by class.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |
| `elementName` | The element name exposed to NSS type selectors. |
| `isRoot` | The is root (bool). |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Color`

```csharp
public static readonly StyleProperty<Color> Color ;
```

Gets the descriptor for the color NSS property, including its grammar and initial value.

#### `FontFamily`

```csharp
public static readonly StyleProperty<ValueList<string>> FontFamily ;
```

Gets the descriptor for the font-family NSS property, including its grammar and initial value.

#### `Content`

```csharp
public static readonly StyleProperty<ValueList<string>> Content;
```

Text fragments for before/after generated boxes. The initial empty list means `normal`/`none`; a list containing an empty string creates a decorative box. Accepts `normal | none | <string>+`. Noninherited, affects layout, and is not animatable. Ignored on ordinary elements and text-portion pseudo-elements.

#### `FontSymbol`

```csharp
public static readonly StyleProperty<ValueList<FontSymbol>> FontSymbol;
```

Descriptor for `font-symbol`: `none | [<string> <string>]#`, initially `none`, noninherited, with layout effects and no animation. Each `Nazor.FontSymbol` pair supplies an exact internal family and nonblank Unicode glyph text without control characters. At most 32 pairs are accepted. The active provider must cover the complete text in that exact family; unavailable pairs are skipped together. No covered candidate draws no symbol ink. `none` preserves ordinary text. Applies to decorative text and generated parts, and is ignored by editable text. Typed `Icon.Symbol` metadata takes precedence. It does not itself generate a `::before`/`::after` box: use `content` too.

#### `FontSize`

```csharp
public static readonly StyleProperty<Length> FontSize ;
```

Gets the descriptor for the font-size NSS property, including its grammar and initial value.

#### `FontWeight`

```csharp
public static readonly StyleProperty<int> FontWeight ;
```

Gets the descriptor for the font-weight NSS property, including its grammar and initial value.

#### `FontStyle`

```csharp
public static readonly StyleProperty<FontStyle> FontStyle ;
```

Gets the descriptor for the font-style NSS property, including its grammar and initial value.

### Variable-font descriptors

| Declaration | Contract |
| --- | --- |
| `public static readonly StyleProperty<FontOpticalSizing> FontOpticalSizing;` | Inherited font-optical-sizing: auto / none / native; Auto initially; layout effects. |
| `public static readonly StyleProperty<ValueList<FontFeatureSetting>> FontFeatureSettings;` | Inherited explicit OpenType features; initially normal, layout effects, discrete. |
| `public static readonly StyleProperty<FontSynthesis> FontSynthesis;` | Inherited synthesis opt-in; initially none, layout effects, discrete. |
| `public static readonly StyleProperty<ValueList<FontVariationSetting>> FontVariationSettings;` | Inherited font-variation-settings: normal or quoted-tag/finite-number list; empty initially; layout effects. Last duplicate wins and output is ordinal-sorted. |

### Backdrop descriptor

| Declaration | Contract |
| --- | --- |
| `public static readonly StyleProperty<BlendMode> MixBlendMode;` | Noninherited group blending; initially normal, paint effects, discrete. |
| `public static readonly StyleProperty<Isolation> Isolation;` | Noninherited group isolation; initially auto, paint effects, discrete. |
| `public static readonly StyleProperty<ValueList<BlendMode>> BackgroundBlendMode;` | Noninherited cycled background image-layer modes; initially normal, paint effects, discrete. |
| `public static readonly StyleProperty<FilterValue> Filter;` | Noninherited, nonanimatable ordered element-content filtering; initially none, paint effects. |
| `public static readonly StyleProperty<FilterValue> BackdropFilter;` | Noninherited, nonanimatable backdrop-filter; initially None, paint effects. An ordered chain shared with element filtering. |

#### `LineHeight`

```csharp
public static readonly StyleProperty<LineHeight> LineHeight ;
```

Gets the descriptor for the line-height NSS property, including its grammar and initial value.

#### `TextAlign`

```csharp
public static readonly StyleProperty<TextAlign> TextAlign ;
```

Gets the descriptor for the text-align NSS property, including its grammar and initial value.

#### `OverflowWrap`

```csharp
public static readonly StyleProperty<OverflowWrap> OverflowWrap ;
```

Gets the descriptor for the overflow-wrap NSS property, including its grammar and initial value.

#### `TextTransform`

```csharp
public static readonly StyleProperty<TextTransform> TextTransform ;
```

Gets the descriptor for the text-transform NSS property, including its grammar and initial value.

#### `LetterSpacing`

```csharp
public static readonly StyleProperty<Length> LetterSpacing ;
```

Gets the descriptor for the letter-spacing NSS property, including its grammar and initial value.

#### `TextDecorationLine`

```csharp
public static readonly StyleProperty<TextDecorationLine> TextDecorationLine ;
```

Gets the descriptor for the text-decoration-line NSS property, including its grammar and initial value.

#### `TextDecorationStyle`

```csharp
public static readonly StyleProperty<TextDecorationStyle> TextDecorationStyle ;
```

Gets the descriptor for the text-decoration-style NSS property, including its grammar and initial value.

#### `TextDecorationColor`

```csharp
public static readonly StyleProperty<Color> TextDecorationColor ;
```

Gets the descriptor for the text-decoration-color NSS property, including its grammar and initial value.

#### `TextDecorationThickness`

```csharp
public static readonly StyleProperty<SizeValue> TextDecorationThickness ;
```

Gets the descriptor for the text-decoration-thickness NSS property, including its grammar and initial value.

#### `TextOverflow`

```csharp
public static readonly StyleProperty<TextOverflow> TextOverflow ;
```

Gets the descriptor for the text-overflow NSS property, including its grammar and initial value.

#### `WhiteSpace`

```csharp
public static readonly StyleProperty<WhiteSpace> WhiteSpace ;
```

Gets the descriptor for the white-space NSS property, including its grammar and initial value.

#### `OverlayExitDuration`

```csharp
public static readonly StyleProperty<TimeSpan> OverlayExitDuration;
```

Descriptor for `overlay-exit-duration`: noninherited, nonanimatable `<time>`, initially zero. Nonnegative finite intervals up to `4294967294ms` are accepted. Overlay and `exit-presence=true` surfaces use it to retain closing content; reduced motion overrides retention. Set the value on the normal surface, not only its exit selector. [NSS contract](../Guide/19-NSS-Property-Reference.md#motion)

#### `Transition`

```csharp
public static readonly StyleProperty<ValueList<TransitionDefinition>> Transition ;
```

Gets the descriptor for the transition NSS property, including its grammar and initial value.

#### `Animation`

```csharp
public static readonly StyleProperty<ValueList<AnimationDefinition>> Animation ;
```

Gets the descriptor for the animation NSS property, including its grammar and initial value.

#### `All`

```csharp
public static IReadOnlyList<StyleProperty> All {get; }
```

Gets all registered longhand style properties in identifier order.

#### `AllShorthands`

```csharp
public static IReadOnlyCollection<ShorthandProperty> AllShorthands {get; }
```

The shorthands, expanded at compile time into `All`.

#### `GlobalKeywords`

```csharp
public static IReadOnlyList<string> GlobalKeywords { get; }
```

The keywords every property accepts.

#### `ColorNames`

```csharp
public static IReadOnlyList<string> ColorNames { get; }
```

The color keywords of `<color>`: the CSS named colors, `transparent` and `currentcolor`.

#### `TryGet`

```csharp
public static bool TryGet(string name, out StyleProperty property)
```

Looks up a registered longhand property by its NSS name.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `property` | When this method returns, contains the property produced by the operation. |

Returns: true if a longhand property was found; otherwise, false.

#### `TryGetShorthand`

```csharp
public static bool TryGetShorthand(string name, out ShorthandProperty shorthand)
```

Looks up a registered shorthand property by its NSS name.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |
| `shorthand` | When this method returns, contains the shorthand produced by the operation. |

Returns: true if a shorthand property was found; otherwise, false.

#### `IsCustomProperty`

```csharp
public static bool IsCustomProperty(string name)
```

Determines whether the name begins with the custom-property prefix --.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: true if the name starts with --; otherwise, false.


<a id="type-scrollbarplacement"></a>

## `ScrollbarPlacement`

Namespace: `Nazor.Style`

```csharp
public enum ScrollbarPlacement : byte
```

`Overlay` draws above content without changing its available size. `Reserve` allocates space beside overflowing content with visible scrollbars. Autohide always uses overlay placement, even if the computed placement value is `Reserve`.

[Source](../../Sources/Core/Nazor.Style/Values/ScrollbarPlacement.cs)

<a id="type-scrollbarvisibility"></a>

## `ScrollbarVisibility`

Namespace: `Nazor.Style`

```csharp
public enum ScrollbarVisibility : byte
```

Controls scrollbar appearance on overflowing scroll containers. `None` hides the bar while preserving scrolling. `Visible` shows it whenever its axis overflows. `AutoHide` reveals it near the pointer, while dragging or during wheel scrolling, with configurable fades.

[Source](../../Sources/Core/Nazor.Style/Values/ScrollbarVisibility.cs)

<a id="type-styleproperty"></a>

## `StyleProperty`

Namespace: `Nazor.Style`

```csharp
public abstract class StyleProperty
```

A longhand property: name, value grammar, initial value, inheritance and effects.

[Source](../../Sources/Core/Nazor.Style/Properties/StyleProperty.cs)

### Members

#### `Id`

```csharp
public int Id { get; }
```

Index in `StyleProperties.All`.

#### `Name`

```csharp
public string Name { get; }
```

Gets the canonical NSS name of the longhand property.

#### `Inherited`

```csharp
public bool Inherited { get; }
```

Inherited properties take the parent's computed value when not specified.

#### `Effects`

```csharp
public StyleEffects Effects { get; }
```

Gets the layout, paint, or compositing work invalidated when the value changes.

#### `Animatable`

```csharp
public bool Animatable { get; }
```

Values can be interpolated by transitions.

#### `Syntax`

```csharp
public string Syntax { get; }
```

The accepted values in CSS value definition syntax, for example `auto | <length-percentage>`.

#### `Keywords`

```csharp
public IReadOnlyList<string> Keywords { get; }
```

The keywords of `Syntax` (`auto`), besides `inherit`, `initial` and `unset`.

#### `ValueTypes`

```csharp
public IReadOnlyList<string> ValueTypes { get; }
```

The value types of `Syntax` without brackets (`length-percentage`, `color`...).

#### `InitialValue`

```csharp
public abstract object InitialValue { get; }
```

Gets the property's initial value before cascading and inheritance.

#### `ValueType`

```csharp
public abstract Type ValueType { get; }
```

Gets the CLR type used to represent values of this property.

#### `Parse`

```csharp
public abstract object Parse(TokenReader reader)
```

Parses the complete value; throws `StyleValueException` on invalid input.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting object value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-stylepropertyt"></a>

## `StyleProperty<T>`

Namespace: `Nazor.Style`

```csharp
public sealed class StyleProperty<T> : StyleProperty where T : notnull
```

Describes a typed NSS longhand property and its parser.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

[Source](../../Sources/Core/Nazor.Style/Properties/StyleProperty.cs)

### Members

#### `Initial`

```csharp
public T Initial { get; }
```

Gets the property's strongly typed initial value.

#### `InitialValue`

```csharp
public override object InitialValue {get; }
```

Gets the property's initial value before cascading and inheritance.

#### `ValueType`

```csharp
public override Type ValueType {get; }
```

Gets the CLR type used to represent values of this property.

#### `Parse`

```csharp
public override object Parse(TokenReader reader)
```

Parses a value according to this property's grammar.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting object value.


<a id="type-styleregistry"></a>

## `StyleRegistry`

Namespace: `Nazor.Style`

```csharp
public static class StyleRegistry
```

Scoped stylesheets of components, registered by generated code when the assembly loads and created on first use. The key is the component type that declared the elements. Development tools replace registered sheets in place; `Version` tells engines that something changed, and optional tracking finds live sheets by `StyleSheet.Source`.

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `RegisterScoped`

```csharp
public static void RegisterScoped(Type component, Func<StyleSheet> factory)
```

Registers, or re-registers, the factory of the scoped sheet of a component type. When the sheet of an existing entry was already created it is kept and its rules are replaced in place, so elements styled by it keep their reference; otherwise the new factory runs on first use. Calls `NotifyChanged` when an entry existed: the first registration of a type (every module initializer) cannot affect a computed style and costs engines nothing.

| Parameter | Description |
| --- | --- |
| `component` | The component type whose scoped styles or render output are being requested. |
| `factory` | The factory used to create the component's scoped stylesheet on first use. |

Throws `ArgumentNullException`: component, factory is null.

#### `GetScoped`

```csharp
public static StyleSheet? GetScoped(Type component)
```

The scoped sheet of a component type, created on first use; null when it has none.

| Parameter | Description |
| --- | --- |
| `component` | The component type whose scoped styles or render output are being requested. |

Returns: The resulting StyleSheet? value.

Throws `ArgumentNullException`: component is null.

#### `ReplaceScoped`

```csharp
public static void ReplaceScoped(Type component, StyleSheet sheet)
```

Development-time: makes `sheet` the scoped sheet of a component type. An already created sheet keeps its identity and takes the rules of `sheet`; one not created yet becomes `sheet` itself. The registered factory is kept, so a later `Refresh` (a hot reload delta) still runs the generated code rather than returning the pushed rules again; a type without a factory gets one returning the sheet. Calls `NotifyChanged`.

| Parameter | Description |
| --- | --- |
| `component` | The component type whose scoped styles or render output are being requested. |
| `sheet` | The stylesheet supplied to the operation. |

Throws `ArgumentNullException`: component, sheet is null.

#### `Refresh`

```csharp
public static void Refresh(Type[]? components)
```

Re-invokes the registered factory of each type (its body may have been patched by a hot reload delta) and replaces the created sheet in place; a sheet not created yet is created later by the current factory. Unknown types are ignored; null means every registered type. A failing factory does not stop the others: the first failure is thrown at the end.

| Parameter | Description |
| --- | --- |
| `components` | The components (Type[]?). |

#### `ScopedTypes`

```csharp
public static IReadOnlyList<Type> ScopedTypes {get; }
```

The component types with a registered scoped sheet, created or not.

#### `Version`

```csharp
public static int Version {get; }
```

Incremented by `NotifyChanged`; a `StyleEngine` compares it on every update.

#### `NotifyChanged`

```csharp
public static void NotifyChanged()
```

Tells engines that a registered or replaced sheet changed; they recompute at their next update.

#### `TrackingEnabled`

```csharp
public static bool TrackingEnabled { get ; set ; }
```

Weak tracking of live sheets by `StyleSheet.Source`, for development tools. Off by default: release applications and tools pay nothing. Sheets created while it was off are not found.

#### `Find`

```csharp
public static IReadOnlyList<StyleSheet> Find(string source)
```

The live sheets with this source (case-insensitive, '\' as '/'); empty when tracking is off.

| Parameter | Description |
| --- | --- |
| `source` | The stylesheet source identity or input content. |

Returns: The resulting IReadOnlyList<StyleSheet> value.

Throws `ArgumentNullException`: source is null.

#### `SetPending`

```csharp
public static void SetPending(string source, IReadOnlyList<StyleRule> rules)
```

Rules applied to a sheet with this source when it is created later (lazily created `StyleSheets.X` properties), while tracking is enabled. The latest call wins.

| Parameter | Description |
| --- | --- |
| `source` | The stylesheet source identity or input content. |
| `rules` | The style rules in cascade source order. |

Throws `ArgumentNullException`: source, rules is null.

#### `SetPending`

```csharp
public static void SetPending(string source, StyleSheet sheet)
```

As `SetPending(string, IReadOnlyList{StyleRule})`, with the keyframes of the sheet too.

| Parameter | Description |
| --- | --- |
| `source` | The stylesheet source identity or input content. |
| `sheet` | The stylesheet supplied to the operation. |

Throws `ArgumentNullException`: source, sheet is null.


<a id="type-stylerule"></a>

## `StyleRule`

Namespace: `Nazor.Style`

```csharp
public sealed class StyleRule(Selector selector, MediaQuery? media, IReadOnlyList<StyleDeclaration> declarations, int order)
```

A style rule after nesting expansion: one selector, an optional media query, declarations and source order.

| Parameter | Description |
| --- | --- |
| `selector` | The selector (Selector). |
| `media` | The media (MediaQuery?). |
| `declarations` | The declarations assigned by the rule or keyframe. |
| `order` | The order (int). |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Selector`

```csharp
public Selector Selector { get; }
```

Gets the selector that determines which elements the rule matches.

#### `Media`

```csharp
public MediaQuery? Media { get; }
```

Gets the media query that restricts this rule, or null when it is unconditional.

#### `Declarations`

```csharp
public IReadOnlyList<StyleDeclaration> Declarations { get; }
```

Gets the style declarations applied by this rule or keyframe.

#### `Order`

```csharp
public int Order { get; }
```

Position in the stylesheet; later rules win among equal specificity.


<a id="type-stylesheetjson"></a>

## `StyleSheetJson`

Namespace: `Nazor.Style`

```csharp
public static class StyleSheetJson
```

JSON form of a compiled `StyleSheet`, for development tools that send sheets to a running application (hot reload of styles). It round-trips everything the C# emitter handles; properties resolve through `StyleProperties`, so a sheet compiled by another version of the registry is rejected with `InvalidDataException`.

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheetJson.cs)

### Members

#### `Serialize`

```csharp
public static string Serialize(StyleSheet sheet)
```

Serializes the style sheet to its JSON text representation.

| Parameter | Description |
| --- | --- |
| `sheet` | The stylesheet to serialize. |

Returns: The serialized stylesheet JSON text.

#### `ToJson`

```csharp
public static JsonNode ToJson(StyleSheet sheet)
```

Converts the style sheet into its structured JSON representation.

| Parameter | Description |
| --- | --- |
| `sheet` | The stylesheet to serialize. |

Returns: The JSON node representing the stylesheet.

Throws `ArgumentNullException`: sheet is null.

#### `Deserialize`

```csharp
public static StyleSheet Deserialize(string json)
```

Parses the JSON of `Serialize`; malformed input raises `InvalidDataException` naming the offending node.

| Parameter | Description |
| --- | --- |
| `json` | The serialized stylesheet JSON text. |

Returns: The resulting StyleSheet value.

Throws `ArgumentNullException`: json is null.

#### `FromJson`

```csharp
public static StyleSheet FromJson(JsonNode node)
```

Reconstructs a style sheet from its serialized JSON node.

| Parameter | Description |
| --- | --- |
| `node` | The JSON node or retained tree node to inspect. |

Returns: The stylesheet reconstructed from the JSON node.

Throws `ArgumentNullException`: node is null.


<a id="type-styletoken"></a>

## `StyleToken`

Namespace: `Nazor.Style.Syntax`

```csharp
public readonly record struct StyleToken(StyleTokenKind Kind, string Value, double Number, int Start, int Length)
```

A token of NSS syntax, including CSS escapes in quoted strings but not escaped identifiers or full URL tokens. `Value` is the identifier, function or at-keyword name, string contents, hash name, unit or delimiter; `Start` is the offset in the tokenized text.

| Parameter | Description |
| --- | --- |
| `Kind` | The style token kind represented by this value. |
| `Value` | The numeric value or payload represented by this record. |
| `Number` | The number (double). |
| `Start` | The start offset in the UTF-16 source text. |
| `Length` | The length of the token in the original UTF-16 source text. |

[Source](../../Sources/Core/Nazor.Style/Syntax/StyleToken.cs)

### Members

#### `Is`

```csharp
public bool Is(StyleTokenKind kind, string value)
```

Tests the token kind and value, comparing the value without regard to case.

| Parameter | Description |
| --- | --- |
| `kind` | The style token kind represented by this value. |
| `value` | The value to use. |

Returns: true if both the kind and case-insensitive value match; otherwise, false.

#### `IsDelim`

```csharp
public bool IsDelim(char c)
```

Tests whether this token is the specified delimiter character.

| Parameter | Description |
| --- | --- |
| `c` | The c (char). |

Returns: true if the token represents the specified delimiter; otherwise, false.

#### `ToString`

```csharp
public override string ToString()
```

Serializes the token back to NSS text, safely quoting string backslashes, quotes and control characters for diagnostics and substituted values.

Returns: The textual representation of this value.

#### `Serialize`

```csharp
public static string Serialize(IEnumerable<StyleToken> tokens)
```

Serializes a token sequence back to NSS text.

| Parameter | Description |
| --- | --- |
| `tokens` | The NSS lexical token sequence. |

Returns: The NSS text reconstructed from the token sequence.


<a id="type-styletokenizer"></a>

## `StyleTokenizer`

Namespace: `Nazor.Style.Syntax`

```csharp
public static class StyleTokenizer
```

Converts CSS text into tokens. Comments are dropped; whitespace runs become one token.

[Source](../../Sources/Core/Nazor.Style/Syntax/StyleToken.cs)

### Members

#### `Tokenize`

```csharp
public static List<StyleToken> Tokenize(string text)
```

Converts NSS text into tokens with original UTF-16 source positions, decoding quoted CSS hex escapes and line continuations. Unterminated strings remain invalid NSS.

| Parameter | Description |
| --- | --- |
| `text` | The text to process. |

Returns: The tokens produced from the source text.


<a id="type-styletokenkind"></a>

## `StyleTokenKind`

Namespace: `Nazor.Style.Syntax`

```csharp
public enum StyleTokenKind : byte
```

Identifies the lexical category of an NSS token.

[Source](../../Sources/Core/Nazor.Style/Syntax/StyleToken.cs)

### Members

#### `Ident`

```csharp
Ident
```

Represents an identifier.

#### `Function`

```csharp
Function
```

Represents a function name and opening parenthesis.

#### `AtKeyword`

```csharp
AtKeyword
```

Represents an at-rule keyword.

#### `Hash`

```csharp
Hash
```

Represents a hash-prefixed value.

#### `String`

```csharp
String
```

Represents a quoted string.

#### `Number`

```csharp
Number
```

Represents a unitless number.

#### `Percentage`

```csharp
Percentage
```

Represents a percentage.

#### `Dimension`

```csharp
Dimension
```

Represents a number with a unit.

#### `Whitespace`

```csharp
Whitespace
```

Represents whitespace.

#### `Colon`

```csharp
Colon
```

Represents a colon.

#### `Semicolon`

```csharp
Semicolon
```

Represents a semicolon.

#### `Comma`

```csharp
Comma
```

Represents a comma.

#### `LeftBracket`

```csharp
LeftBracket
```

Represents an opening square bracket.

#### `RightBracket`

```csharp
RightBracket
```

Represents a closing square bracket.

#### `LeftParen`

```csharp
LeftParen
```

Represents an opening parenthesis.

#### `RightParen`

```csharp
RightParen
```

Represents a closing parenthesis.

#### `LeftBrace`

```csharp
LeftBrace
```

Represents an opening brace.

#### `RightBrace`

```csharp
RightBrace
```

Represents a closing brace.

#### `Delim`

```csharp
Delim
```

Represents a delimiter character.

#### `BadString`

```csharp
BadString
```

Represents an unterminated or invalid string.

#### `EndOfFile`

```csharp
EndOfFile
```

Represents the end of the source text.


<a id="type-styletrace"></a>

## `StyleTrace`

Namespace: `Nazor.Style`

```csharp
public sealed record StyleTrace(IReadOnlyList<TracedRule> Rules, IReadOnlyList<TracedDeclaration> Inline, IReadOnlyList<TracedValue> Values, IReadOnlyList<TracedValue> CustomProperties)
```

How the style of an element came to be (`StyleEngine.Trace`): the rules that select it in cascade order, the declarations of its style attribute, and every computed value with where it comes from. Plain data, for developer tools.

| Parameter | Description |
| --- | --- |
| `Rules` | The style rules in cascade source order. |
| `Inline` | The inline (IReadOnlyList<TracedDeclaration>). |
| `Values` | The values (IReadOnlyList<TracedValue>). |
| `CustomProperties` | The custom properties (IReadOnlyList<TracedValue>). |

[Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)


<a id="type-stylevalueexception"></a>

## `StyleValueException`

Namespace: `Nazor.Style.Syntax`

```csharp
public sealed class StyleValueException(string message, StyleToken token) : Exception(message)
```

Raised when a value does not match the grammar of a property.

| Parameter | Description |
| --- | --- |
| `message` | The message (string). |
| `token` | The NSS token identifying the value or error location. |

[Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

### Members

#### `Token`

```csharp
public StyleToken Token { get; }
```

Gets the source token associated with the invalid value.


<a id="type-systemcolors"></a>

## `SystemColors`

Namespace: `Nazor.Style`

```csharp
public sealed record SystemColors
```

CSS system colors, accent shades and additional semantic palette roles. CSS names are NSS color keywords; shades and additional roles are root variables. `Resolve` completes backend colors for the selected desktop and light/dark appearance. `Default` supplies the classic light palette. See the [palette contract](../Guide/16-Styling-and-Themes.md#system-palettes-and-backend-fallbacks).

System keywords alias ordinary inherited custom properties: overriding `--nz-system-canvas` also changes `Canvas`. The namespace is not protected or read-only. Prefer theme tokens such as `--nz-surface` for application customization; see [system color keywords and theme tokens](../Guide/16-Styling-and-Themes.md#system-color-keywords-and-theme-tokens).

[Source](../../Sources/Core/Nazor.Style/Values/SystemColors.cs)

### Members

#### `Names`

```csharp
public static readonly IReadOnlyList<string> Names ;
```

The names of CSS Color 4, in the case CSS writes them.

#### `AccentShades`

```csharp
public static readonly IReadOnlyList<string> AccentShades ;
```

The shades of the accent color, lighter and darker, as Windows names them.

#### `Default`

```csharp
public static SystemColors Default { get; }
```

The classic light colors of Windows.

#### `Palette`

```csharp
public IReadOnlyDictionary<string, Color> Palette { get; }
```

Additional semantic colors, such as Tooltip, TooltipText, Header and HeaderText. `Resolve` returns an immutable snapshot, including backend extension roles. Use `Property(name)` to obtain the NSS variable name.

#### `Resolve`

```csharp
public static SystemColors Resolve(
    IReadOnlyDictionary<string, Color>? colors = null,
    ColorScheme scheme = ColorScheme.Light,
    SystemColorProfile profile = SystemColorProfile.Generic)
```

Completes a partial backend palette. Supplied colors win; missing accent shades and semantic roles are derived before falling back to the desktop profile. Names are case-insensitive and inputs are copied. `scheme` is the effective light or dark appearance. Profiles approximate desktop defaults, not arbitrary user themes.

#### `Canvas`

```csharp
public Color Canvas { get; init;}
```

Gets or sets the system color for the application surface background.

#### `CanvasText`

```csharp
public Color CanvasText { get; init;}
```

Gets or sets the system color for text drawn on the application surface.

#### `Field`

```csharp
public Color Field { get; init;}
```

Gets or sets the system color for the input-field background.

#### `FieldText`

```csharp
public Color FieldText { get; init;}
```

Gets or sets the system color for text drawn in an input field.

#### `ButtonFace`

```csharp
public Color ButtonFace { get; init;}
```

Gets or sets the system color for a button surface.

#### `ButtonText`

```csharp
public Color ButtonText { get; init;}
```

Gets or sets the system color for text drawn on a button.

#### `ButtonBorder`

```csharp
public Color ButtonBorder { get; init;}
```

Gets or sets the system color for a button border.

#### `Highlight`

```csharp
public Color Highlight { get; init;}
```

Gets or sets the system color for the highlighted-selection background.

#### `HighlightText`

```csharp
public Color HighlightText { get; init;}
```

Gets or sets the system color for text drawn in a highlighted selection.

#### `SelectedItem`

```csharp
public Color SelectedItem { get; init;}
```

Gets or sets the system color for the selected-item background.

#### `SelectedItemText`

```csharp
public Color SelectedItemText { get; init;}
```

Gets or sets the system color for text drawn on a selected item.

#### `GrayText`

```csharp
public Color GrayText { get; init;}
```

Gets or sets the system color for disabled text.

#### `LinkText`

```csharp
public Color LinkText { get; init;}
```

Gets or sets the system color for an unvisited link.

#### `VisitedText`

```csharp
public Color VisitedText { get; init;}
```

Gets or sets the system color for a visited link.

#### `ActiveText`

```csharp
public Color ActiveText { get; init;}
```

Gets or sets the system color for an active link.

#### `Mark`

```csharp
public Color Mark { get; init;}
```

Gets or sets the system color for the marked-text background.

#### `MarkText`

```csharp
public Color MarkText { get; init;}
```

Gets or sets the system color for marked text.

#### `AccentColor`

```csharp
public Color AccentColor { get; init;}
```

Gets or sets the system color for accented controls.

#### `AccentColorText`

```csharp
public Color AccentColorText { get; init;}
```

Gets or sets the system color for text drawn on an accent surface.

#### `AccentLight1`

```csharp
public Color AccentLight1 { get; init;}
```

Gets or sets accent shade 1 on the lighter side of the system accent color.

#### `AccentLight2`

```csharp
public Color AccentLight2 { get; init;}
```

Gets or sets accent shade 2 on the lighter side of the system accent color.

#### `AccentLight3`

```csharp
public Color AccentLight3 { get; init;}
```

Gets or sets accent shade 3 on the lighter side of the system accent color.

#### `AccentDark1`

```csharp
public Color AccentDark1 { get; init;}
```

Gets or sets accent shade 1 on the darker side of the system accent color.

#### `AccentDark2`

```csharp
public Color AccentDark2 { get; init;}
```

Gets or sets accent shade 2 on the darker side of the system accent color.

#### `AccentDark3`

```csharp
public Color AccentDark3 { get; init;}
```

Gets or sets accent shade 3 on the darker side of the system accent color.

#### `this`

```csharp
public Color? this[string name] {get; }
```

The value of a system color or of a shade of the accent by its name (any case); null for another name.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

#### `TextOn`

```csharp
public static Color TextOn(Color background)
```

White or black, the one readable on a background: white when it reaches the contrast of 4.5 that text needs (as Windows writes on its accents), otherwise the one with more contrast.

| Parameter | Description |
| --- | --- |
| `background` | The background color used to clear the surface. |

Returns: The resulting Color value.

#### `MayName`

```csharp
public static bool MayName(string propertyName)
```

Whether the values of a property may name system colors.

| Parameter | Description |
| --- | --- |
| `propertyName` | The canonical NSS property name. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `Variable`

```csharp
public static string? Variable(string name)
```

The custom property that carries a system color, or null when the name is not one.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting string? value.

#### `Property`

```csharp
public static string? Property(string name)
```

The custom property for a CSS system color, accent shade or ASCII alphanumeric semantic role. Invalid names return null. Additional PascalCase roles use kebab-case: `TooltipText` becomes `--nz-system-tooltip-text`.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: The resulting string? value.

#### `Substitute`

```csharp
public static IReadOnlyList<StyleToken> Substitute(IReadOnlyList<StyleToken> value)
```

A value with its system color names replaced by the custom properties that carry them (`Canvas` becomes `var(--nz-system-canvas)`); the same list when it has none. References remain dynamic and follow normal custom-property inheritance and overrides.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting IReadOnlyList<StyleToken> value.


<a id="type-textalign"></a>

## `TextAlign`

Namespace: `Nazor.Style`

```csharp
public enum TextAlign : byte
```

Specifies horizontal text alignment within a line box.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Start`

```csharp
Start
```

Aligns text to the start edge of its line box.

#### `End`

```csharp
End
```

Aligns text to the end edge of its line box.

#### `Left`

```csharp
Left
```

Aligns text to the left edge of its line box.

#### `Right`

```csharp
Right
```

Aligns text to the right edge of its line box.

#### `Center`

```csharp
Center
```

Centers text within its line box.


<a id="type-textdecorationline"></a>

## `TextDecorationLine`

Namespace: `Nazor.Style`

```csharp
[Flags] public enum TextDecorationLine : byte
```

The lines of `text-decoration-line`; any combination.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `None`

```csharp
None = 0
```

Draws no text decoration.

#### `Underline`

```csharp
Underline = 1
```

Draws a line below the text.

#### `Overline`

```csharp
Overline = 2
```

Draws a line above the text.

#### `LineThrough`

```csharp
LineThrough = 4
```

Draws a line through the text.


<a id="type-textdecorationstyle"></a>

## `TextDecorationStyle`

Namespace: `Nazor.Style`

```csharp
public enum TextDecorationStyle : byte
```

Specifies the stroke pattern for a text decoration.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Solid`

```csharp
Solid
```

Draws a continuous decoration line.

#### `Double`

```csharp
Double
```

Draws two parallel decoration lines.

#### `Dotted`

```csharp
Dotted
```

Draws the decoration as dots.

#### `Dashed`

```csharp
Dashed
```

Draws the decoration as dashes.

#### `Wavy`

```csharp
Wavy
```

Draws the decoration as a wavy line.


<a id="type-textoverflow"></a>

## `TextOverflow`

Namespace: `Nazor.Style`

```csharp
public enum TextOverflow : byte
```

Specifies how clipped text indicates horizontal overflow.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Clip`

```csharp
Clip
```

Clips text at the available width.

#### `Ellipsis`

```csharp
Ellipsis
```

Adds an ellipsis when text exceeds the available width.


<a id="type-texttransform"></a>

## `TextTransform`

Namespace: `Nazor.Style`

```csharp
public enum TextTransform : byte
```

Specifies the case transformation applied to displayed text.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `None`

```csharp
None
```

Preserves the source text's case.

#### `Uppercase`

```csharp
Uppercase
```

Displays the text in uppercase.

#### `Lowercase`

```csharp
Lowercase
```

Displays the text in lowercase.

#### `Capitalize`

```csharp
Capitalize
```

Capitalizes the start of each word.


<a id="type-tokenreader"></a>

## `TokenReader`

Namespace: `Nazor.Style.Syntax`

```csharp
public sealed class TokenReader(IReadOnlyList<StyleToken> tokens)
```

Reads the component values of a declaration, skipping whitespace on request.

| Parameter | Description |
| --- | --- |
| `tokens` | The NSS lexical token sequence. |

[Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

### Members

#### `Tokens`

```csharp
public IReadOnlyList<StyleToken> Tokens { get; }
```

Gets the NSS token sequence represented by this declaration or reader.

#### `AtEnd`

```csharp
public bool AtEnd { get ; }
```

Gets whether only whitespace or an end-of-input token remains.

#### `Peek`

```csharp
public StyleToken Peek()
```

Returns the next token without consuming it, skipping whitespace.

Returns: The next non-whitespace token, or the end-of-input token.

#### `Next`

```csharp
public StyleToken Next()
```

Consumes and returns the next token, skipping surrounding whitespace.

Returns: The next non-whitespace token, or the end-of-input token.

#### `WhitespaceBefore`

```csharp
public bool WhitespaceBefore { get ; }
```

Whitespace comes before the next token (as `+` and `-` require in `calc()`).

#### `WhitespaceAfter`

```csharp
public bool WhitespaceAfter {get; }
```

Whitespace follows the last token read.

#### `TryConsume`

```csharp
public bool TryConsume(StyleTokenKind kind)
```

Consumes the next token if its kind matches the supplied kind.

| Parameter | Description |
| --- | --- |
| `kind` | The style token kind represented by this value. |

Returns: true if a matching token was consumed; otherwise, false.

#### `TryConsumeIdent`

```csharp
public bool TryConsumeIdent(string name)
```

Consumes the next token if it is the specified case-insensitive identifier.

| Parameter | Description |
| --- | --- |
| `name` | The name used to identify the parameter, property, resource, or registration. |

Returns: true if the specified identifier was consumed; otherwise, false.

#### `Expect`

```csharp
public void Expect(StyleTokenKind kind, string description)
```

Consumes a token of the required kind or throws a style-value exception.

| Parameter | Description |
| --- | --- |
| `kind` | The style token kind represented by this value. |
| `description` | The expected syntax displayed in an error message. |

#### `ExpectEnd`

```csharp
public void ExpectEnd()
```

Throws a style-value exception if unconsumed tokens remain.

#### `SplitByComma`

```csharp
public List<TokenReader> SplitByComma()
```

Splits the remaining tokens at top-level commas.

Returns: The resulting List<TokenReader> value.

#### `FunctionArguments`

```csharp
public List<TokenReader> FunctionArguments(StyleToken function)
```

The arguments of the function whose name was just read, split at its top-level commas, up to its closing parenthesis. Whitespace stays: `calc()` needs it around `+` and `-`.

| Parameter | Description |
| --- | --- |
| `function` | The function (StyleToken). |

Returns: The resulting List<TokenReader> value.

#### `End`

```csharp
public StyleToken End {get; }
```

Gets the end-of-input token returned when no tokens remain.

#### `Error`

```csharp
public static StyleValueException Error(string message, StyleToken token)
```

Creates a style-value exception associated with the supplied token.

| Parameter | Description |
| --- | --- |
| `message` | The message (string). |
| `token` | The NSS token identifying the value or error location. |

Returns: The exception carrying the message and source token.


<a id="type-traceddeclaration"></a>

## `TracedDeclaration`

Namespace: `Nazor.Style`

```csharp
public sealed record TracedDeclaration(string Property, string Value, IReadOnlyList<string> Longhands, IReadOnlyList<string> Winning)
```

A declaration of a rule or of the style attribute: the longhands it sets (a shorthand with `var()` sets several) and those for which it is the declaration that wins.

| Parameter | Description |
| --- | --- |
| `Property` | The style property targeted by the value or declaration. |
| `Value` | The numeric value or payload represented by this record. |
| `Longhands` | The longhands (IReadOnlyList<string>). |
| `Winning` | The winning (IReadOnlyList<string>). |

[Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

### Members

#### `IsOverridden`

```csharp
public bool IsOverridden {get; }
```

A later declaration (or a more specific rule) sets every longhand of this one.


<a id="type-tracedrule"></a>

## `TracedRule`

Namespace: `Nazor.Style`

```csharp
public sealed record TracedRule(StyleLevel Level, string Sheet, string Selector, Specificity Specificity, string? Media, bool MediaMatches, int Order, IReadOnlyList<TracedDeclaration> Declarations)
```

A rule whose selector matches the element, at its level of the cascade. A rule whose `@media` does not match now is listed too, without effect (`MediaMatches` false).

| Parameter | Description |
| --- | --- |
| `Level` | The level (StyleLevel). |
| `Sheet` | The stylesheet to serialize. |
| `Selector` | The selector (string). |
| `Specificity` | The specificity (Specificity). |
| `Media` | The media (string?). |
| `MediaMatches` | The media matches (bool). |
| `Order` | The order (int). |
| `Declarations` | The declarations assigned by the rule or keyframe. |

[Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)


<a id="type-tracedvalue"></a>

## `TracedValue`

Namespace: `Nazor.Style`

```csharp
public sealed record TracedValue(string Property, string Value, ValueSource Source, int? Rule = null, int? Declaration = null)
```

A computed value with its source: the index of the rule in `StyleTrace.Rules` and of the declaration in it (or in `StyleTrace.Inline`) that set it.

| Parameter | Description |
| --- | --- |
| `Property` | The style property targeted by the value or declaration. |
| `Value` | The numeric value or payload represented by this record. |
| `Source` | The stylesheet source identity or input content. |
| `Rule` | The rule (int?). |
| `Declaration` | The declaration (int?). |

[Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)


<a id="type-trackbreadth"></a>

## `TrackBreadth`

Namespace: `Nazor.Style`

```csharp
public readonly record struct TrackBreadth(TrackBreadthKind Kind, Length Length, double Flex)
```

One side of a track size: the minimum or the maximum sizing function (CSS Grid §7.2.3).

| Parameter | Description |
| --- | --- |
| `Kind` | The track breadth kind represented by this value. |
| `Length` | The absolute, relative, or percentage length to use. |
| `Flex` | The flex (double). |

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Auto`

```csharp
public static readonly TrackBreadth Auto ;
```

Automatic grid-track sizing based on the contained items.

#### `MinContent`

```csharp
public static readonly TrackBreadth MinContent ;
```

Minimum-content intrinsic grid-track sizing.

#### `MaxContent`

```csharp
public static readonly TrackBreadth MaxContent ;
```

Maximum-content intrinsic grid-track sizing.

#### `Fixed`

```csharp
public static TrackBreadth Fixed(Length length)
```

Creates a track breadth from the supplied length.

| Parameter | Description |
| --- | --- |
| `length` | The absolute, relative, or percentage length to use. |

Returns: The resulting TrackBreadth value.

#### `Fr`

```csharp
public static TrackBreadth Fr(double value)
```

Creates a flexible track breadth in fractional units.

| Parameter | Description |
| --- | --- |
| `value` | The numeric magnitude in the unit selected by this factory method. |

Returns: The resulting TrackBreadth value.

#### `IsFixed`

```csharp
public bool IsFixed {get; }
```

Gets whether the track breadth is represented by a length.

#### `IsIntrinsic`

```csharp
public bool IsIntrinsic {get; }
```

Gets whether the track breadth uses automatic, minimum-content, or maximum-content sizing.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-trackbreadthkind"></a>

## `TrackBreadthKind`

Namespace: `Nazor.Style`

```csharp
public enum TrackBreadthKind : byte
```

Specifies how a grid track's breadth is sized.

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Length`

```csharp
Length
```

A length or a percentage of the grid container's inner size.

#### `Flex`

```csharp
Flex
```

A share of the leftover space (`fr`); only as a maximum.

#### `Auto`

```csharp
Auto
```

Sizes the track from the automatic contributions of its items.

#### `MinContent`

```csharp
MinContent
```

Uses the minimum intrinsic content contribution.

#### `MaxContent`

```csharp
MaxContent
```

Uses the maximum intrinsic content contribution.


<a id="type-tracksize"></a>

## `TrackSize`

Namespace: `Nazor.Style`

```csharp
public readonly record struct TrackSize(TrackBreadth Min, TrackBreadth Max, bool FitContent = false)
```

The size of a grid track: `minmax(Min, Max)`. A plain length, `auto` or keyword is the same on both sides, `1fr` is `minmax(auto, 1fr)`; with `FitContent` the maximum is `max-content` clamped by the length in `Max` (`fit-content()`).

| Parameter | Description |
| --- | --- |
| `Min` | The smallest accepted value. |
| `Max` | The largest accepted value. |
| `FitContent` | The fit content (bool). |

[Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

### Members

#### `Auto`

```csharp
public static readonly TrackSize Auto ;
```

A grid track with automatic minimum and maximum breadth.

#### `Fixed`

```csharp
public static TrackSize Fixed(Length length)
```

Creates a grid track whose minimum and maximum use the supplied length.

| Parameter | Description |
| --- | --- |
| `length` | The absolute, relative, or percentage length to use. |

Returns: The resulting TrackSize value.

#### `Fr`

```csharp
public static TrackSize Fr(double value)
```

Creates a flexible grid track using the supplied fractional weight.

| Parameter | Description |
| --- | --- |
| `value` | The numeric magnitude in the unit selected by this factory method. |

Returns: The resulting TrackSize value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-transformfunction"></a>

## `TransformFunction`

Namespace: `Nazor.Style`

```csharp
public abstract record TransformFunction
```

One function of `transform` (CSS Transforms 1, in 2D); angles are in degrees.

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)


<a id="type-transformorigin"></a>

## `TransformOrigin`

Namespace: `Nazor.Style`

```csharp
public readonly record struct TransformOrigin(Length X, Length Y)
```

The point `transform` turns around: lengths or percentages of the border box from its top left corner.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `Center`

```csharp
public static readonly TransformOrigin Center ;
```

A transform origin centered horizontally and vertically in the element's box.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-transitiondefinition"></a>

## `TransitionDefinition`

Namespace: `Nazor.Style`

```csharp
public sealed record TransitionDefinition(string Property, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay)
```

One entry of the `transition` list; `Property` is a property name or "all".

| Parameter | Description |
| --- | --- |
| `Property` | The style property targeted by the value or declaration. |
| `Duration` | The duration of the animation or transition. |
| `Easing` | The easing (EasingFunction). |
| `Delay` | The delay before the operation or animation starts. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-translatefunction"></a>

## `TranslateFunction`

Namespace: `Nazor.Style`

```csharp
public sealed record TranslateFunction(Length X, Length Y) : TransformFunction
```

`translate()`, `translateX()`, `translateY()`: percentages of the element's border box.

| Parameter | Description |
| --- | --- |
| `X` | The horizontal component of the value. |
| `Y` | The vertical component of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-urlimage"></a>

## `UrlImage`

Namespace: `Nazor.Style`

```csharp
public sealed record UrlImage(string Url) : ImageValue
```

`url()`: an image file. The compiler resolves a relative URL against the folder of the style sheet, so the value is relative to the project (and to the application folder, where the SDK copies the images); http and https URLs stay as written.

| Parameter | Description |
| --- | --- |
| `Url` | The url (string). |

[Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

### Members

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-userselect"></a>

## `UserSelect`

Namespace: `Nazor.Style`

```csharp
public enum UserSelect : byte
```

Specifies whether and how users can select the element's text.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
Auto
```

Uses the control-specific selection behavior.

#### `None`

```csharp
None
```

Disables user text selection.

#### `Text`

```csharp
Text
```

Allows selecting text ranges.

#### `All`

```csharp
All
```

Selects the entire text as a unit.


<a id="type-valuedeclaration"></a>

## `ValueDeclaration`

Namespace: `Nazor.Style`

```csharp
public sealed class ValueDeclaration(StyleProperty property, object value) : StyleDeclaration
```

A longhand with a value already parsed and validated by the compiler.

| Parameter | Description |
| --- | --- |
| `property` | The style property to read or update. |
| `value` | The value to use. |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

### Members

#### `Property`

```csharp
public StyleProperty Property { get; }
```

Gets the style property targeted by this value or declaration.

#### `Value`

```csharp
public object Value { get; }
```

Gets the parsed value assigned to the style property.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation using registered NSS enum keywords, space-separated combined text-decoration lines, grid column/dense keywords, invariant times and round-trip numeric values.

Returns: The textual representation of this value.


<a id="type-valuelistt"></a>

## `ValueList<T>`

Namespace: `Nazor.Style`

```csharp
public sealed class ValueList<T> : IEquatable<ValueList<T>>
```

An immutable list with value equality, used for list-valued properties.

| Type parameter | Description |
| --- | --- |
| `T` | The type of the value. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Empty`

```csharp
public static readonly ValueList<T> Empty ;
```

An empty value list value.

#### `ValueList`

```csharp
public ValueList(IEnumerable<T> items)
```

Initializes a new instance of ValueList.

| Parameter | Description |
| --- | --- |
| `items` | The items to store in the immutable value list. |

#### `Items`

```csharp
public IReadOnlyList<T> Items { get; }
```

Gets the ordered values contained in this immutable list.

#### `Equals`

```csharp
public bool Equals(ValueList<T>? other)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `other` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `Equals`

```csharp
public override bool Equals(object? obj)
```

Determines whether the supplied value is equal to this instance.

| Parameter | Description |
| --- | --- |
| `obj` | The value to compare with this instance. |

Returns: true if the supplied value satisfies the comparison; otherwise, false.

#### `GetHashCode`

```csharp
public override int GetHashCode()
```

Returns a hash code consistent with equality for this value.

Returns: The hash code for this value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.


<a id="type-valueparsers"></a>

## `ValueParsers`

Namespace: `Nazor.Style.Syntax`

```csharp
public static class ValueParsers
```

Grammars of the value types shared by properties.

[Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

### Members

#### `IsLengthStart`

```csharp
public static bool IsLengthStart(StyleToken token)
```

Starts a length: a number, a dimension, a percentage or a math function.

| Parameter | Description |
| --- | --- |
| `token` | The NSS token identifying the value or error location. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `IsMathFunction`

```csharp
public static bool IsMathFunction(StyleToken token)
```

Determines whether the token begins a supported NSS mathematical function.

| Parameter | Description |
| --- | --- |
| `token` | The NSS token identifying the value or error location. |

Returns: true when the operation succeeds or the condition is satisfied; otherwise, false.

#### `ParseLength`

```csharp
public static Length ParseLength(TokenReader reader, bool allowPercent = true, bool allowNegative = false)
```

Parses length from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |
| `allowPercent` | Whether percentage lengths are accepted. |
| `allowNegative` | Whether negative length values are accepted. |

Returns: The resulting Length value.

#### `ParseNumber`

```csharp
public static double ParseNumber(TokenReader reader, double min = double.MinValue, double max = double.MaxValue)
```

Parses a number or a finite unitless calc, min, max or clamp expression from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |
| `min` | The smallest accepted value. |
| `max` | The largest accepted value. |

Returns: The resulting double value.

#### `ParseInteger`

```csharp
public static int ParseInteger(TokenReader reader)
```

Parses integer from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting int value.

#### `ParseInteger`

```csharp
public static int ParseInteger(TokenReader reader, int min)
```

Parses integer from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |
| `min` | The smallest accepted value. |

Returns: The resulting int value.

#### `ParseTime`

```csharp
public static TimeSpan ParseTime(TokenReader reader)
```

Parses time from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting TimeSpan value.

#### `ParseColor`

```csharp
public static Color ParseColor(TokenReader reader)
```

Parses an NSS color, including premultiplied sRGB or Oklab color mixing, into straight-alpha 8-bit sRGB.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting Color value.

#### `ParseKeyword`

```csharp
public static TEnum ParseKeyword<TEnum>(TokenReader reader, IReadOnlyDictionary<string, TEnum> keywords) where TEnum : struct
```

Parses keyword from the current token position.

| Type parameter | Description |
| --- | --- |
| `TEnum` | The enumeration type represented by the accepted keywords. |

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |
| `keywords` | The case-insensitive map from NSS keywords to their values. |

Returns: The resulting TEnum value.

#### `ParseEasing`

```csharp
public static EasingFunction ParseEasing(TokenReader reader)
```

Parses easing from the current token position.

| Parameter | Description |
| --- | --- |
| `reader` | The token reader positioned at the start of the value. |

Returns: The resulting EasingFunction value.


<a id="type-valuesource"></a>

## `ValueSource`

Namespace: `Nazor.Style`

```csharp
public enum ValueSource
```

Where a computed value comes from.

[Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

### Members

#### `Rule`

```csharp
Rule
```

A declaration of a rule (`TracedValue.Rule`).

#### `Inline`

```csharp
Inline
```

A declaration of the style attribute.

#### `Inherited`

```csharp
Inherited
```

Not declared: the value of the parent, for an inherited property.

#### `Initial`

```csharp
Initial
```

Not declared: the initial value of the property.


<a id="type-viewport"></a>

## `Viewport`

Namespace: `Nazor.Style`

```csharp
public readonly record struct Viewport(double Width, double Height)
```

The size of the window content in logical units, tested by media queries.

| Parameter | Description |
| --- | --- |
| `Width` | The width of the value in its coordinate system. |
| `Height` | The height of the value in its coordinate system. |

[Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)


<a id="type-visibility"></a>

## `Visibility`

Namespace: `Nazor.Style`

```csharp
public enum Visibility : byte
```

Specifies whether an element is painted while retaining its layout space.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Visible`

```csharp
Visible
```

Paints the element normally.

#### `Hidden`

```csharp
Hidden
```

Suppresses painting while preserving the layout space.


<a id="type-whitespace"></a>

## `WhiteSpace`

Namespace: `Nazor.Style`

```csharp
public enum WhiteSpace : byte
```

Specifies whitespace collapsing and text wrapping behavior.

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Normal`

```csharp
Normal
```

Collapses whitespace and wraps text as needed.

#### `NoWrap`

```csharp
NoWrap
```

Collapses whitespace and disables line wrapping.

#### `Pre`

```csharp
Pre
```

Preserves whitespace and explicit line breaks without wrapping.

#### `PreWrap`

```csharp
PreWrap
```

Preserves whitespace and explicit line breaks and permits wrapping.

#### `PreLine`

```csharp
PreLine
```

Collapses spaces, preserves explicit line breaks, and permits wrapping.


<a id="type-zindex"></a>

## `ZIndex`

Namespace: `Nazor.Style`

```csharp
public readonly record struct ZIndex(bool IsAuto, int Value)
```

The `z-index` of an element: `auto` or an integer.

| Parameter | Description |
| --- | --- |
| `IsAuto` | The is auto (bool). |
| `Value` | The numeric value or payload represented by this record. |

[Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

### Members

#### `Auto`

```csharp
public static readonly ZIndex Auto ;
```

Automatic stacking order within the current stacking context.

#### `Of`

```csharp
public static ZIndex Of(int value)
```

Creates an explicit stacking order from an integer value.

| Parameter | Description |
| --- | --- |
| `value` | The value to use. |

Returns: The resulting ZIndex value.

#### `ToString`

```csharp
public override string ToString()
```

Returns a textual representation of this value.

Returns: The textual representation of this value.

## API signature index

These declarations supplement the contracts above. Inherited members are documented with the declaring type.

### `AlignContent`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AlignContent` | Distribution of the lines of a multi-line flex container on the cross axis. |
| `Normal = 0` | Uses the layout mode's normal alignment behavior. |
| `Stretch = 1` | Stretches items or tracks to fill available space. |
| `FlexStart = 2` | Aligns items or tracks at the start of the applicable axis. |
| `FlexEnd = 3` | Aligns items or tracks at the end of the applicable axis. |
| `Center = 4` | Centers items or tracks within the available space. |
| `SpaceBetween = 5` | Distributes free space between items or tracks. |
| `SpaceAround = 6` | Distributes free space around items or tracks, with half-sized outer gaps. |
| `SpaceEvenly = 7` | Distributes free space into equal inner and outer gaps. |

### `AlignItems`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AlignItems` | Specifies the default cross-axis alignment of child items. |
| `Stretch = 0` | Stretches items or tracks to fill available space. |
| `FlexStart = 1` | Aligns items or tracks at the start of the applicable axis. |
| `FlexEnd = 2` | Aligns items or tracks at the end of the applicable axis. |
| `Center = 3` | Centers items or tracks within the available space. |

### `AlignSelf`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AlignSelf` | Specifies a child item's cross-axis alignment override. |
| `Auto = 0` | Uses the parent alignment setting. |
| `Stretch = 1` | Stretches items or tracks to fill available space. |
| `FlexStart = 2` | Aligns items or tracks at the start of the applicable axis. |
| `FlexEnd = 3` | Aligns items or tracks at the end of the applicable axis. |
| `Center = 4` | Centers items or tracks within the available space. |

### `AnimationDefinition`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record AnimationDefinition : IEquatable<AnimationDefinition>` | One entry of the `animation` list: the name of a `@keyframes` rule (null for `none`), duration, timing function, delay (negative starts midway), iterations (`System.Double.PositiveInfinity` for `infinite`), direction, fill mode and play state.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Duration`. The duration of the animation or transition.<br>`Easing`. The easing (EasingFunction).<br>`Delay`. The delay before the operation or animation starts.<br>`IterationCount`. The iteration count (double).<br>`Direction`. The direction (AnimationDirection).<br>`FillMode`. The fill mode (AnimationFillMode).<br>`Paused`. The paused (bool). |
| `public AnimationDefinition(string? Name, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay, double IterationCount, AnimationDirection Direction, AnimationFillMode FillMode, bool Paused)` | One entry of the `animation` list: the name of a `@keyframes` rule (null for `none`), duration, timing function, delay (negative starts midway), iterations (`System.Double.PositiveInfinity` for `infinite`), direction, fill mode and play state.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Duration`. The duration of the animation or transition.<br>`Easing`. The easing (EasingFunction).<br>`Delay`. The delay before the operation or animation starts.<br>`IterationCount`. The iteration count (double).<br>`Direction`. The direction (AnimationDirection).<br>`FillMode`. The fill mode (AnimationFillMode).<br>`Paused`. The paused (bool). |
| `public static bool operator !=(AnimationDefinition? left, AnimationDefinition? right)` |  |
| `public static bool operator ==(AnimationDefinition? left, AnimationDefinition? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(AnimationDefinition? other)` |  |
| `public void Deconstruct(out string? Name, out TimeSpan Duration, out EasingFunction Easing, out TimeSpan Delay, out double IterationCount, out AnimationDirection Direction, out AnimationFillMode FillMode, out bool Paused)` |  |
| `public string? Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public TimeSpan Duration { get; init; }` | The duration of the animation or transition. |
| `public EasingFunction Easing { get; init; }` | The easing (EasingFunction). |
| `public TimeSpan Delay { get; init; }` | The delay before the operation or animation starts. |
| `public double IterationCount { get; init; }` | The iteration count (double). |
| `public AnimationDirection Direction { get; init; }` | The direction (AnimationDirection). |
| `public AnimationFillMode FillMode { get; init; }` | The fill mode (AnimationFillMode). |
| `public bool Paused { get; init; }` | The paused (bool). |

### `AnimationDirection`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AnimationDirection` | Specifies playback order and whether successive animation iterations alternate. |
| `Normal = 0` | Plays every iteration from the first keyframe to the last. |
| `Reverse = 1` | Plays every iteration from the last keyframe to the first. |
| `Alternate = 2` | Alternates direction, beginning with forward playback. |
| `AlternateReverse = 3` | Alternates direction, beginning with reverse playback. |

### `AnimationFillMode`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/AnimationValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AnimationFillMode` | Specifies whether animation values apply before or after the active interval. |
| `None = 0` | Applies animation values only during the active interval. |
| `Forwards = 1` | Retains the ending animation value after the active interval. |
| `Backwards = 2` | Applies the starting animation value during the delay. |
| `Both = 3` | Applies both backwards and forwards fill behavior. |

### `AspectRatio`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct AspectRatio : IEquatable<AspectRatio>` | The preferred aspect ratio of a box, width divided by height; `Nazor.Style.AspectRatio.Auto` is none.<br>`Ratio`. The preferred width divided by height; a nonpositive value represents automatic sizing. |
| `public AspectRatio()` |  |
| `public AspectRatio(double Ratio)` | The preferred aspect ratio of a box, width divided by height; `Nazor.Style.AspectRatio.Auto` is none.<br>`Ratio`. The preferred width divided by height; a nonpositive value represents automatic sizing. |
| `public static bool operator !=(AspectRatio left, AspectRatio right)` |  |
| `public static bool operator ==(AspectRatio left, AspectRatio right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(AspectRatio other)` |  |
| `public void Deconstruct(out double Ratio)` |  |
| `public double Ratio { get; init; }` | The preferred width divided by height; a nonpositive value represents automatic sizing. |

### `AttributeMatch`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public enum AttributeMatch` | Specifies how an attribute selector compares the element's attribute value. |
| `Exists = 0` | Requires the attribute to be present. |
| `Equals = 1` | Requires an exact attribute-value match. |
| `Includes = 2` | Requires a whitespace-separated word to match. |
| `DashMatch = 3` | Matches the value exactly or as a prefix followed by a hyphen. |
| `Prefix = 4` | Requires the attribute value to begin with the selector value. |
| `Suffix = 5` | Requires the attribute value to end with the selector value. |
| `Substring = 6` | Requires the attribute value to contain the selector value. |

### `AttributeSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record AttributeSelector : IEquatable<AttributeSelector>` | An attribute selector. The attribute name ignores case, like HTML; an attribute set to null or `false` is absent (`[disabled]` matches a disabled control); other values compare as their invariant text, with case unless `Nazor.Style.AttributeSelector.IgnoreCase` (the `i` flag).<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Match`. The match (AttributeMatch).<br>`Value`. The numeric value or payload represented by this record.<br>`IgnoreCase`. The ignore case (bool). |
| `public AttributeSelector(string Name, AttributeMatch Match, string? Value, bool IgnoreCase = false)` | An attribute selector. The attribute name ignores case, like HTML; an attribute set to null or `false` is absent (`[disabled]` matches a disabled control); other values compare as their invariant text, with case unless `Nazor.Style.AttributeSelector.IgnoreCase` (the `i` flag).<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`Match`. The match (AttributeMatch).<br>`Value`. The numeric value or payload represented by this record.<br>`IgnoreCase`. The ignore case (bool). |
| `public static bool operator !=(AttributeSelector? left, AttributeSelector? right)` |  |
| `public static bool operator ==(AttributeSelector? left, AttributeSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(AttributeSelector? other)` |  |
| `public void Deconstruct(out string Name, out AttributeMatch Match, out string? Value, out bool IgnoreCase)` |  |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public AttributeMatch Match { get; init; }` | The match (AttributeMatch). |
| `public string? Value { get; init; }` | The numeric value or payload represented by this record. |
| `public bool IgnoreCase { get; init; }` | The ignore case (bool). |

### `BackgroundRepeat`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BackgroundRepeat` | How a background image repeats in its box. |
| `Repeat = 0` | Tiles the background image along both axes. |
| `NoRepeat = 1` | Draws one background image without tiling. |
| `RepeatX = 2` | Tiles the background image horizontally. |
| `RepeatY = 3` | Tiles the background image vertically. |

### `BackgroundSize`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct BackgroundSize : IEquatable<BackgroundSize>` | A value of `background-size`: `cover`, `contain` or a width and a height (auto keeps the proportions).<br>`Kind`. The background size kind represented by this value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public BackgroundSize()` |  |
| `public BackgroundSize(BackgroundSizeKind Kind, SizeValue Width, SizeValue Height)` | A value of `background-size`: `cover`, `contain` or a width and a height (auto keeps the proportions).<br>`Kind`. The background size kind represented by this value.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public static bool operator !=(BackgroundSize left, BackgroundSize right)` |  |
| `public static bool operator ==(BackgroundSize left, BackgroundSize right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(BackgroundSize other)` |  |
| `public void Deconstruct(out BackgroundSizeKind Kind, out SizeValue Width, out SizeValue Height)` |  |
| `public BackgroundSizeKind Kind { get; init; }` | The background size kind represented by this value. |
| `public SizeValue Width { get; init; }` | The width of the value in its coordinate system. |
| `public SizeValue Height { get; init; }` | The height of the value in its coordinate system. |

### `BackgroundSizeKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BackgroundSizeKind` | Specifies explicit image dimensions or aspect-preserving cover and contain sizing. |
| `Explicit = 0` | Uses the declared horizontal and vertical image dimensions. |
| `Cover = 1` | Scales the image proportionally until it covers the painting box. |
| `Contain = 2` | Scales the image proportionally to fit entirely inside the painting box. |

### `BorderStyle`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BorderStyle` | The line style of a border side or of the outline. |
| `None = 0` | Does not draw a border line. |
| `Solid = 1` | Draws a continuous border line. |
| `Dashed = 2` | Draws a sequence of border dashes. |
| `Dotted = 3` | Draws a sequence of border dots. |

### `BoxShadow`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record BoxShadow : IEquatable<BoxShadow>` | One shadow of `box-shadow`: offsets, blur radius (the CSS blur, twice the standard deviation), spread (negative shrinks) and color; `Nazor.Style.BoxShadow.Inset` shadows fall inside the padding box.<br>`Inset`. The inset (bool).<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Blur`. The blur (Length).<br>`Spread`. The spread (Length).<br>`Color`. The color (Color). |
| `public BoxShadow(bool Inset, Length X, Length Y, Length Blur, Length Spread, Color Color)` | One shadow of `box-shadow`: offsets, blur radius (the CSS blur, twice the standard deviation), spread (negative shrinks) and color; `Nazor.Style.BoxShadow.Inset` shadows fall inside the padding box.<br>`Inset`. The inset (bool).<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value.<br>`Blur`. The blur (Length).<br>`Spread`. The spread (Length).<br>`Color`. The color (Color). |
| `public static bool operator !=(BoxShadow? left, BoxShadow? right)` |  |
| `public static bool operator ==(BoxShadow? left, BoxShadow? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(BoxShadow? other)` |  |
| `public void Deconstruct(out bool Inset, out Length X, out Length Y, out Length Blur, out Length Spread, out Color Color)` |  |
| `public bool Inset { get; init; }` | The inset (bool). |
| `public Length X { get; init; }` | The horizontal component of the value. |
| `public Length Y { get; init; }` | The vertical component of the value. |
| `public Length Blur { get; init; }` | The blur (Length). |
| `public Length Spread { get; init; }` | The spread (Length). |
| `public Color Color { get; init; }` | The color (Color). |

### `BoxSizing`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum BoxSizing` | Specifies whether declared dimensions include borders and padding. |
| `BorderBox = 0` | Includes padding and borders in declared width and height. |
| `ContentBox = 1` | Applies declared width and height to content, adding padding and borders outside them. |

### `CalcExpression`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CalcExpression : IEquatable<CalcExpression>` | A length computed by an expression. `Nazor.Style.CalcExpression.NonNegative` clamps the result at zero, for properties that do not accept negative lengths. Expressions without relative units are reduced when parsed: `calc(10px + 2 * 5px)` is 20px, `calc(100% - 16px)` stays an expression.<br>`Root`. The root (CalcNode).<br>`NonNegative`. The non negative (bool). |
| `public CalcExpression(CalcNode Root, bool NonNegative)` | A length computed by an expression. `Nazor.Style.CalcExpression.NonNegative` clamps the result at zero, for properties that do not accept negative lengths. Expressions without relative units are reduced when parsed: `calc(10px + 2 * 5px)` is 20px, `calc(100% - 16px)` stays an expression.<br>`Root`. The root (CalcNode).<br>`NonNegative`. The non negative (bool). |
| `public static bool operator !=(CalcExpression? left, CalcExpression? right)` |  |
| `public static bool operator ==(CalcExpression? left, CalcExpression? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(CalcExpression? other)` |  |
| `public void Deconstruct(out CalcNode Root, out bool NonNegative)` |  |
| `public CalcNode Root { get; init; }` | The root (CalcNode). |
| `public bool NonNegative { get; init; }` | The non negative (bool). |

### `CalcFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CalcFunction : CalcNode, IEquatable<CalcFunction>` | `min()`, `max()` or `clamp(min, value, max)`.<br>`Kind`. The calc function kind represented by this value.<br>`Arguments`. The arguments (ValueList<CalcNode>). |
| `public CalcFunction(CalcFunctionKind Kind, ValueList<CalcNode> Arguments)` | `min()`, `max()` or `clamp(min, value, max)`.<br>`Kind`. The calc function kind represented by this value.<br>`Arguments`. The arguments (ValueList<CalcNode>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(CalcFunction? left, CalcFunction? right)` |  |
| `public static bool operator ==(CalcFunction? left, CalcFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(CalcNode? other)` |  |
| `public bool Equals(CalcFunction? other)` |  |
| `public void Deconstruct(out CalcFunctionKind Kind, out ValueList<CalcNode> Arguments)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public CalcFunctionKind Kind { get; init; }` | The calc function kind represented by this value. |
| `public ValueList<CalcNode> Arguments { get; init; }` | The arguments (ValueList<CalcNode>). |

### `CalcFunctionKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CalcFunctionKind` | Identifies the minimum, maximum, or clamp calculation function. |
| `Min = 0` | Selects the smallest evaluated argument. |
| `Max = 1` | Selects the largest evaluated argument. |
| `Clamp = 2` | Clamps the preferred value between the minimum and maximum bounds. |

### `CalcLength`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CalcLength : CalcNode, IEquatable<CalcLength>` | A length leaf: px, a percentage or a relative unit, never another calculation.<br>`Length`. The absolute, relative, or percentage length to use. |
| `public CalcLength(Length Length)` | A length leaf: px, a percentage or a relative unit, never another calculation.<br>`Length`. The absolute, relative, or percentage length to use. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(CalcLength? left, CalcLength? right)` |  |
| `public static bool operator ==(CalcLength? left, CalcLength? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(CalcNode? other)` |  |
| `public bool Equals(CalcLength? other)` |  |
| `public void Deconstruct(out Length Length)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Length Length { get; init; }` | The absolute, relative, or percentage length to use. |

### `CalcNode`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record CalcNode : IEquatable<CalcNode>` | A node of a `calc()`, `min()`, `max()` or `clamp()` expression (CSS Values §10): a number, a length leaf, an operation or a comparison function. |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(CalcNode? left, CalcNode? right)` |  |
| `public static bool operator ==(CalcNode? left, CalcNode? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(CalcNode? other)` |  |
| `protected CalcNode(CalcNode original)` |  |
| `protected virtual Type EqualityContract { get; }` |  |

### `CalcNumber`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CalcNumber : CalcNode, IEquatable<CalcNumber>` | Represents a unitless numeric literal in an NSS calculation.<br>`Value`. The numeric value or payload represented by this record. |
| `public CalcNumber(double Value)` | Represents a unitless numeric literal in an NSS calculation.<br>`Value`. The numeric value or payload represented by this record. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(CalcNumber? left, CalcNumber? right)` |  |
| `public static bool operator ==(CalcNumber? left, CalcNumber? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(CalcNode? other)` |  |
| `public bool Equals(CalcNumber? other)` |  |
| `public void Deconstruct(out double Value)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double Value { get; init; }` | The numeric value or payload represented by this record. |

### `CalcOperation`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record CalcOperation : CalcNode, IEquatable<CalcOperation>` | Represents a binary arithmetic operation between two calculation nodes.<br>`Operator`. The operator (CalcOperator).<br>`Left`. The value associated with the left side or operand.<br>`Right`. The value associated with the right side or operand. |
| `public CalcOperation(CalcOperator Operator, CalcNode Left, CalcNode Right)` | Represents a binary arithmetic operation between two calculation nodes.<br>`Operator`. The operator (CalcOperator).<br>`Left`. The value associated with the left side or operand.<br>`Right`. The value associated with the right side or operand. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(CalcOperation? left, CalcOperation? right)` |  |
| `public static bool operator ==(CalcOperation? left, CalcOperation? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(CalcNode? other)` |  |
| `public bool Equals(CalcOperation? other)` |  |
| `public void Deconstruct(out CalcOperator Operator, out CalcNode Left, out CalcNode Right)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public CalcOperator Operator { get; init; }` | The operator (CalcOperator). |
| `public CalcNode Left { get; init; }` | The value associated with the left side or operand. |
| `public CalcNode Right { get; init; }` | The value associated with the right side or operand. |

### `CalcOperator`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/CalcValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum CalcOperator` | Identifies a binary arithmetic operator in a calculation expression. |
| `Add = 0` | Adds the left and right operands. |
| `Subtract = 1` | Subtracts the right operand from the left. |
| `Multiply = 2` | Multiplies the operands. |
| `Divide = 3` | Divides the left operand by the right. |

### `ClipPathValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ClipPathValue.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ClipPathValue : IEquatable<ClipPathValue>` | An optionally rounded NSS inset clip relative to the border box, independent of layout and scrolling.<br>`Top`. The top inset; percentages resolve against the border-box height.<br>`Right`. The right inset; percentages resolve against the border-box width.<br>`Bottom`. The bottom inset; percentages resolve against the border-box height.<br>`Left`. The left inset; percentages resolve against the border-box width.<br>`IsNone`. Whether clipping is disabled. |
| `public static readonly ClipPathValue None` | Disables clipping, including clipping of overflowing descendants and shadows. |
| `public ClipPathValue()` |  |
| `public ClipPathValue(Length Top, Length Right, Length Bottom, Length Left, bool IsNone = false)` | An optionally rounded NSS inset clip relative to the border box, independent of layout and scrolling.<br>`Top`. The top inset; percentages resolve against the border-box height.<br>`Right`. The right inset; percentages resolve against the border-box width.<br>`Bottom`. The bottom inset; percentages resolve against the border-box height.<br>`Left`. The left inset; percentages resolve against the border-box width.<br>`IsNone`. Whether clipping is disabled. |
| `public override string ToString()` | Returns the NSS representation, with four inset values, optional elliptical corner radii, or `none`.<br>**Returns**. The NSS clip value. |
| `public static bool operator !=(ClipPathValue left, ClipPathValue right)` |  |
| `public static bool operator ==(ClipPathValue left, ClipPathValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ClipPathValue other)` |  |
| `public void Deconstruct(out Length Top, out Length Right, out Length Bottom, out Length Left, out bool IsNone)` |  |
| `public Length Top { get; init; }` | The top inset; percentages resolve against the border-box height. |
| `public Length Right { get; init; }` | The right inset; percentages resolve against the border-box width. |
| `public Length Bottom { get; init; }` | The bottom inset; percentages resolve against the border-box height. |
| `public Length Left { get; init; }` | The left inset; percentages resolve against the border-box width. |
| `public bool IsNone { get; init; }` | Whether clipping is disabled. |
| `public RadiusValue TopLeftRadius { get; init; }` | The elliptical corner radius. Percentage axes use the original border-box width and height; zero produces a square corner. |
| `public RadiusValue TopRightRadius { get; init; }` | The elliptical corner radius. Percentage axes use the original border-box width and height; zero produces a square corner. |
| `public RadiusValue BottomRightRadius { get; init; }` | The elliptical corner radius. Percentage axes use the original border-box width and height; zero produces a square corner. |
| `public RadiusValue BottomLeftRadius { get; init; }` | The elliptical corner radius. Percentage axes use the original border-box width and height; zero produces a square corner. |

### `Color`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Color : IEquatable<Color>` | An sRGB color with straight (non-premultiplied) alpha, or the `currentColor` keyword.<br>`R`. The red channel in the range from zero to 255.<br>`G`. The green channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`A`. The alpha channel in the range from zero to 255.<br>`IsCurrentColor`. The is current color (bool). |
| `public Color()` |  |
| `public Color(byte R, byte G, byte B, byte A, bool IsCurrentColor = false)` | An sRGB color with straight (non-premultiplied) alpha, or the `currentColor` keyword.<br>`R`. The red channel in the range from zero to 255.<br>`G`. The green channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`A`. The alpha channel in the range from zero to 255.<br>`IsCurrentColor`. The is current color (bool). |
| `public static bool operator !=(Color left, Color right)` |  |
| `public static bool operator ==(Color left, Color right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Color other)` |  |
| `public void Deconstruct(out byte R, out byte G, out byte B, out byte A, out bool IsCurrentColor)` |  |
| `public byte R { get; init; }` | The red channel in the range from zero to 255. |
| `public byte G { get; init; }` | The green channel in the range from zero to 255. |
| `public byte B { get; init; }` | The blue channel in the range from zero to 255. |
| `public byte A { get; init; }` | The alpha channel in the range from zero to 255. |
| `public bool IsCurrentColor { get; init; }` | The is current color (bool). |

### `ColorScheme`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ColorScheme` | Specifies an explicit light or dark color scheme, or follows the system preference. |
| `Light = 0` | Uses a light color scheme. |
| `Dark = 1` | Uses a dark color scheme. |
| `System = 2` | Follows the platform color-scheme preference. |

### `ColorStop`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ColorStop : IEquatable<ColorStop>` | A color stop of a gradient; `Nazor.Style.ColorStop.Position` null is placed by the CSS fix-up rules.<br>`Color`. The color (Color).<br>`Position`. The position (Length?). |
| `public ColorStop(Color Color, Length? Position)` | A color stop of a gradient; `Nazor.Style.ColorStop.Position` null is placed by the CSS fix-up rules.<br>`Color`. The color (Color).<br>`Position`. The position (Length?). |
| `public static bool operator !=(ColorStop? left, ColorStop? right)` |  |
| `public static bool operator ==(ColorStop? left, ColorStop? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ColorStop? other)` |  |
| `public void Deconstruct(out Color Color, out Length? Position)` |  |
| `public Color Color { get; init; }` | The color (Color). |
| `public Length? Position { get; init; }` | The position (Length?). |

### `Combinator`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Combinator` | Specifies the tree relationship between adjacent compound selectors. |
| `Descendant = 0` | Matches a descendant within the current component scope. |
| `Child = 1` | Matches an immediate child. |
| `NextSibling = 2` | Matches the immediately following sibling. |
| `SubsequentSibling = 3` | Matches any following sibling. |
| `Deep = 4` | Matches descendants across component scope boundaries. |

### `Cursor`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Cursor` | Specifies the pointer cursor requested by an element. |
| `Default = 0` | Displays a cursor indicating the default arrow. |
| `Pointer = 1` | Displays a cursor indicating a link-selection hand. |
| `Text = 2` | Displays a cursor indicating a text-selection I-beam. |
| `ResizeHorizontal = 3` | Displays a cursor indicating horizontal resizing. |
| `ResizeVertical = 4` | Displays a cursor indicating vertical resizing. |
| `Move = 5` | Displays a cursor indicating movement in all directions. |
| `NotAllowed = 6` | Displays a cursor indicating an unavailable operation. |
| `Wait = 7` | Displays a cursor indicating a busy operation. |
| `Progress = 8` | Displays a cursor indicating background progress. |
| `Help = 9` | Displays a cursor indicating contextual help. |
| `Crosshair = 10` | Displays a cursor indicating a precision crosshair. |
| `ResizeNorthwestSoutheast = 11` | Displays a cursor indicating diagonal resizing from the top-left or bottom-right corner. |
| `ResizeNortheastSouthwest = 12` | Displays a cursor indicating diagonal resizing from the top-right or bottom-left corner. |

### `CustomPropertyDeclaration`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class CustomPropertyDeclaration : StyleDeclaration` | A custom property such as `--surface`: inherited token list, possibly referencing others.<br>`name`. The name used to identify the parameter, property, resource, or registration.<br>`tokens`. The NSS lexical token sequence. |
| `public CustomPropertyDeclaration(string name, IReadOnlyList<StyleToken> tokens)` | A custom property such as `--surface`: inherited token list, possibly referencing others.<br>`name`. The name used to identify the parameter, property, resource, or registration.<br>`tokens`. The NSS lexical token sequence. |

### `Display`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Display` | Specifies block, inline text, flex, grid, or suppressed layout. |
| `Block = 0` | Uses block layout. |
| `Flex = 1` | Uses flex layout for child items. |
| `Grid = 2` | Uses grid layout for child items. |
| `None = 3` | Omits the element and its descendants from layout and painting. |
| `Inline = 4` | Participates in a shared horizontal text flow. |

### `EasingFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record EasingFunction : IEquatable<EasingFunction>` | A cubic Bézier timing function; the CSS keywords are presets.<br>`X1`. The x1 (double).<br>`Y1`. The y1 (double).<br>`X2`. The x2 (double).<br>`Y2`. The y2 (double). |
| `public EasingFunction(double X1, double Y1, double X2, double Y2)` | A cubic Bézier timing function; the CSS keywords are presets.<br>`X1`. The x1 (double).<br>`Y1`. The y1 (double).<br>`X2`. The x2 (double).<br>`Y2`. The y2 (double). |
| `public static bool operator !=(EasingFunction? left, EasingFunction? right)` |  |
| `public static bool operator ==(EasingFunction? left, EasingFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(EasingFunction? other)` |  |
| `public void Deconstruct(out double X1, out double Y1, out double X2, out double Y2)` |  |
| `public double X1 { get; init; }` | The x1 (double). |
| `public double Y1 { get; init; }` | The y1 (double). |
| `public double X2 { get; init; }` | The x2 (double). |
| `public double Y2 { get; init; }` | The y2 (double). |

### `EmptySelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record EmptySelector : PseudoSelector, IEquatable<EmptySelector>` | `:empty`: no element children and no text. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(EmptySelector? left, EmptySelector? right)` |  |
| `public static bool operator ==(EmptySelector? left, EmptySelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(PseudoSelector? other)` |  |
| `public bool Equals(EmptySelector? other)` |  |
| `public EmptySelector()` |  |
| `protected override Type EqualityContract { get; }` |  |

### `FlexDirection`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FlexDirection` | Specifies the main axis and item order of a flex container. |
| `Row = 0` | Lays out items horizontally in forward order. |
| `RowReverse = 1` | Lays out items horizontally in reverse order. |
| `Column = 2` | Lays out items vertically in forward order. |
| `ColumnReverse = 3` | Lays out items vertically in reverse order. |

### `FlexWrap`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FlexWrap` | Specifies whether flex items wrap and whether the line order is reversed. |
| `NoWrap = 0` | Keeps flex items on a single line. |
| `Wrap = 1` | Wraps flex items onto additional lines. |
| `WrapReverse = 2` | Wraps flex items with the cross-axis line order reversed. |

### `FontStyle`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FontStyle` | Specifies normal or italic font selection. |
| `Normal = 0` | Selects an upright font face. |
| `Italic = 1` | Selects an italic font face. |

### `GlobalKeyword`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public enum GlobalKeyword` | Specifies how a property obtains its value through the cascade and inheritance. |
| `Initial = 0` | Uses the property's registered initial value. |
| `Inherit = 1` | Uses the parent's computed value. |
| `Unset = 2` | Inherits inherited properties and uses the initial value for other properties. |

### `Gradient`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record Gradient : ImageValue, IEquatable<Gradient>` | A gradient image.<br>`Stops`. The stops (ValueList<ColorStop>). |
| `protected Gradient(ValueList<ColorStop> Stops)` | A gradient image.<br>`Stops`. The stops (ValueList<ColorStop>). |
| `public override string ToString()` |  |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(Gradient? left, Gradient? right)` |  |
| `public static bool operator ==(Gradient? left, Gradient? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(ImageValue? other)` |  |
| `public virtual bool Equals(Gradient? other)` |  |
| `protected Gradient(Gradient original)` |  |
| `public void Deconstruct(out ValueList<ColorStop> Stops)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public ValueList<ColorStop> Stops { get; init; }` | The stops (ValueList<ColorStop>). |

### `GridArea`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct GridArea : IEquatable<GridArea>` | A named area of `grid-template-areas`: 0-based tracks, end exclusive.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`RowStart`. The row start (int).<br>`RowEnd`. The row end (int).<br>`ColumnStart`. The column start (int).<br>`ColumnEnd`. The column end (int). |
| `public GridArea()` |  |
| `public GridArea(string Name, int RowStart, int RowEnd, int ColumnStart, int ColumnEnd)` | A named area of `grid-template-areas`: 0-based tracks, end exclusive.<br>`Name`. The name used to identify the parameter, property, resource, or registration.<br>`RowStart`. The row start (int).<br>`RowEnd`. The row end (int).<br>`ColumnStart`. The column start (int).<br>`ColumnEnd`. The column end (int). |
| `public override string ToString()` |  |
| `public static bool operator !=(GridArea left, GridArea right)` |  |
| `public static bool operator ==(GridArea left, GridArea right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(GridArea other)` |  |
| `public void Deconstruct(out string Name, out int RowStart, out int RowEnd, out int ColumnStart, out int ColumnEnd)` |  |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |
| `public int RowStart { get; init; }` | The row start (int). |
| `public int RowEnd { get; init; }` | The row end (int). |
| `public int ColumnStart { get; init; }` | The column start (int). |
| `public int ColumnEnd { get; init; }` | The column end (int). |

### `GridAutoFlow`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum GridAutoFlow` | Specifies the axis and packing strategy used to place implicit grid items. |
| `Row = 0` | Places auto-positioned items by filling rows. |
| `Column = 1` | Places auto-positioned items by filling columns. |
| `RowDense = 2` | Fills rows and backfills earlier gaps. |
| `ColumnDense = 3` | Fills columns and backfills earlier gaps. |

### `GridAutoRepeat`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum GridAutoRepeat` | Specifies how repeated grid tracks fill the available space. |
| `None = 0` | Uses the explicit track repetition count. |
| `Fill = 1` | Creates as many repeated tracks as fit the available space. |
| `Fit = 2` | Creates fitting repeated tracks and collapses unused tracks. |

### `GridLine`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct GridLine : IEquatable<GridLine>` | One side of a grid item's placement: `grid-row-start`, `grid-column-end`...<br>`Kind`. The grid line kind represented by this value.<br>`Value`. The numeric value or payload represented by this record.<br>`Name`. The name used to identify the parameter, property, resource, or registration. |
| `public GridLine()` |  |
| `public GridLine(GridLineKind Kind, int Value, string? Name)` | One side of a grid item's placement: `grid-row-start`, `grid-column-end`...<br>`Kind`. The grid line kind represented by this value.<br>`Value`. The numeric value or payload represented by this record.<br>`Name`. The name used to identify the parameter, property, resource, or registration. |
| `public static bool operator !=(GridLine left, GridLine right)` |  |
| `public static bool operator ==(GridLine left, GridLine right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(GridLine other)` |  |
| `public void Deconstruct(out GridLineKind Kind, out int Value, out string? Name)` |  |
| `public GridLineKind Kind { get; init; }` | The grid line kind represented by this value. |
| `public int Value { get; init; }` | The numeric value or payload represented by this record. |
| `public string? Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |

### `GridLineKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum GridLineKind` | Distinguishes automatic placement, explicit grid lines, spans, and named areas. |
| `Auto = 0` | Places the item using the grid auto-placement algorithm. |
| `Line = 1` | A line number: from 1 at the start of the explicit grid, negative from its end. |
| `Span = 2` | `span N`: the item covers N tracks from the other line. |
| `Area = 3` | The start or end line of a named area of `grid-template-areas`. |

### `GridTrackList`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record GridTrackList : IEquatable<GridTrackList>` | The value of `grid-template-columns` and `grid-template-rows`: explicit tracks, with repetitions of a fixed count already expanded and at most one automatic repetition inserted before track `Nazor.Style.GridTrackList.RepeatIndex`. `Nazor.Style.GridTrackList.None` is the keyword `none`.<br>`Tracks`. The track definitions in declaration order.<br>`Repeat`. The repeat (GridAutoRepeat).<br>`RepeatIndex`. The repeat index (int).<br>`RepeatTracks`. The repeat tracks (ValueList<TrackSize>). |
| `public GridTrackList(ValueList<TrackSize> Tracks, GridAutoRepeat Repeat, int RepeatIndex, ValueList<TrackSize> RepeatTracks)` | The value of `grid-template-columns` and `grid-template-rows`: explicit tracks, with repetitions of a fixed count already expanded and at most one automatic repetition inserted before track `Nazor.Style.GridTrackList.RepeatIndex`. `Nazor.Style.GridTrackList.None` is the keyword `none`.<br>`Tracks`. The track definitions in declaration order.<br>`Repeat`. The repeat (GridAutoRepeat).<br>`RepeatIndex`. The repeat index (int).<br>`RepeatTracks`. The repeat tracks (ValueList<TrackSize>). |
| `public static bool operator !=(GridTrackList? left, GridTrackList? right)` |  |
| `public static bool operator ==(GridTrackList? left, GridTrackList? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(GridTrackList? other)` |  |
| `public void Deconstruct(out ValueList<TrackSize> Tracks, out GridAutoRepeat Repeat, out int RepeatIndex, out ValueList<TrackSize> RepeatTracks)` |  |
| `public ValueList<TrackSize> Tracks { get; init; }` | The track definitions in declaration order. |
| `public GridAutoRepeat Repeat { get; init; }` | The repeat (GridAutoRepeat). |
| `public int RepeatIndex { get; init; }` | The repeat index (int). |
| `public ValueList<TrackSize> RepeatTracks { get; init; }` | The repeat tracks (ValueList<TrackSize>). |

### `ImageValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record ImageValue : IEquatable<ImageValue>` | A layer of `background-image`: a gradient or an image by URL. |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(ImageValue? left, ImageValue? right)` |  |
| `public static bool operator ==(ImageValue? left, ImageValue? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(ImageValue? other)` |  |
| `protected ImageValue(ImageValue original)` |  |
| `protected ImageValue()` |  |
| `protected virtual Type EqualityContract { get; }` |  |

### `JustifyContent`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum JustifyContent` | Specifies how free space is distributed along the layout's main axis. |
| `Normal = 0` | Uses the layout mode's normal alignment behavior. |
| `Stretch = 1` | Stretches items or tracks to fill available space. |
| `FlexStart = 2` | Aligns items or tracks at the start of the applicable axis. |
| `FlexEnd = 3` | Aligns items or tracks at the end of the applicable axis. |
| `Center = 4` | Centers items or tracks within the available space. |
| `SpaceBetween = 5` | Distributes free space between items or tracks. |
| `SpaceAround = 6` | Distributes free space around items or tracks, with half-sized outer gaps. |
| `SpaceEvenly = 7` | Distributes free space into equal inner and outer gaps. |

### `Keyframe`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class Keyframe` | One keyframe of a `@keyframes` rule: its offset in the iteration (0 for `from`, 1 for `to`), its declarations (animatable properties only) and, with `animation-timing-function`, the timing function of the interval that starts here.<br>`offset`. The offset (double).<br>`declarations`. The declarations assigned by the rule or keyframe.<br>`easing`. The easing (EasingFunction?). |
| `public Keyframe(double offset, IReadOnlyList<StyleDeclaration> declarations, EasingFunction? easing = null)` | One keyframe of a `@keyframes` rule: its offset in the iteration (0 for `from`, 1 for `to`), its declarations (animatable properties only) and, with `animation-timing-function`, the timing function of the interval that starts here.<br>`offset`. The offset (double).<br>`declarations`. The declarations assigned by the rule or keyframe.<br>`easing`. The easing (EasingFunction?). |

### `KeyframesRule`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class KeyframesRule` | A `@keyframes` rule: a name and its keyframes by offset (equal offsets in source order).<br>`name`. The name used to identify the parameter, property, resource, or registration.<br>`keyframes`. The keyframe animation definitions supplied by the stylesheet. |
| `public KeyframesRule(string name, IReadOnlyList<Keyframe> keyframes)` | A `@keyframes` rule: a name and its keyframes by offset (equal offsets in source order).<br>`name`. The name used to identify the parameter, property, resource, or registration.<br>`keyframes`. The keyframe animation definitions supplied by the stylesheet. |

### `KeywordDeclaration`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class KeywordDeclaration : StyleDeclaration` | `initial`, `inherit` or `unset` on a longhand.<br>`property`. The style property to read or update.<br>`keyword`. The keyword (GlobalKeyword). |
| `public KeywordDeclaration(StyleProperty property, GlobalKeyword keyword)` | `initial`, `inherit` or `unset` on a longhand.<br>`property`. The style property to read or update.<br>`keyword`. The keyword (GlobalKeyword). |

### `Length`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Length : IEquatable<Length>` | A length in logical units, a percentage resolved against a basis defined by each property, a relative length (em, rem, vw...) or an expression. The style engine converts relative units to px: computed values have only px, percentages and expressions with percentages.<br>`Value`. The numeric value or payload represented by this record.<br>`Unit`. The unit or calculation mode used to resolve the length.<br>`Expression`. The optional calculation expression used when the length unit is Calc. |
| `public Length()` |  |
| `public Length(double Value, LengthUnit Unit, CalcExpression? Expression = null)` | A length in logical units, a percentage resolved against a basis defined by each property, a relative length (em, rem, vw...) or an expression. The style engine converts relative units to px: computed values have only px, percentages and expressions with percentages.<br>`Value`. The numeric value or payload represented by this record.<br>`Unit`. The unit or calculation mode used to resolve the length.<br>`Expression`. The optional calculation expression used when the length unit is Calc. |
| `public static bool operator !=(Length left, Length right)` |  |
| `public static bool operator ==(Length left, Length right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Length other)` |  |
| `public void Deconstruct(out double Value, out LengthUnit Unit, out CalcExpression? Expression)` |  |
| `public double Value { get; init; }` | The numeric value or payload represented by this record. |
| `public LengthUnit Unit { get; init; }` | The unit or calculation mode used to resolve the length. |
| `public CalcExpression? Expression { get; init; }` | The optional calculation expression used when the length unit is Calc. |

### `LengthUnit`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LengthUnit` | Identifies the unit or calculation used to resolve an NSS length. |
| `Px = 0` | Measures logical pixels. |
| `Percent = 1` | Measures a percentage of the applicable reference dimension. |
| `Em = 2` | Measures multiples of the current font size. |
| `Rem = 3` | Measures multiples of the root font size. |
| `Vw = 4` | Measures hundredths of the viewport width. |
| `Vh = 5` | Measures hundredths of the viewport height. |
| `Vmin = 6` | Measures hundredths of the smaller viewport dimension. |
| `Vmax = 7` | Measures hundredths of the larger viewport dimension. |
| `Calc = 8` | Evaluates a calculation expression to resolve the length. |
| `Ch = 9` | Horizontal zero-glyph advance, resolved by computed font metrics. |
| `Ex = 10` | First available font x-height, resolved by computed font metrics. |

### `LineHeight`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct LineHeight : IEquatable<LineHeight>` | `normal`, a multiplier of the font size, or a length.<br>`Kind`. The line height kind represented by this value.<br>`Number`. The number (double).<br>`Length`. The absolute, relative, or percentage length to use. |
| `public LineHeight()` |  |
| `public LineHeight(LineHeightKind Kind, double Number, Length Length)` | `normal`, a multiplier of the font size, or a length.<br>`Kind`. The line height kind represented by this value.<br>`Number`. The number (double).<br>`Length`. The absolute, relative, or percentage length to use. |
| `public static bool operator !=(LineHeight left, LineHeight right)` |  |
| `public static bool operator ==(LineHeight left, LineHeight right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(LineHeight other)` |  |
| `public void Deconstruct(out LineHeightKind Kind, out double Number, out Length Length)` |  |
| `public LineHeightKind Kind { get; init; }` | The line height kind represented by this value. |
| `public double Number { get; init; }` | The number (double). |
| `public Length Length { get; init; }` | The absolute, relative, or percentage length to use. |

### `LineHeightKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LineHeightKind` | Specifies the available line height kind values. |
| `Normal = 0` | Uses the font's normal line metrics. |
| `Number = 1` | Uses a unitless multiplier of the font size. |
| `Length = 2` | Uses a length resolved to a logical line height. |

### `LinearGradient`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LinearGradient : Gradient, IEquatable<LinearGradient>` | `linear-gradient()`: an angle in degrees (0 points up, clockwise), or a side or corner as `Nazor.Style.LinearGradient.ToX` (−1 left, 1 right) and `Nazor.Style.LinearGradient.ToY` (−1 top, 1 bottom), resolved against the box when drawn as CSS requires for corners.<br>`Angle`. The angle (double?).<br>`ToX`. The to x (int).<br>`ToY`. The to y (int).<br>`Stops`. The stops (ValueList<ColorStop>). |
| `public LinearGradient(double? Angle, int ToX, int ToY, ValueList<ColorStop> Stops)` | `linear-gradient()`: an angle in degrees (0 points up, clockwise), or a side or corner as `Nazor.Style.LinearGradient.ToX` (−1 left, 1 right) and `Nazor.Style.LinearGradient.ToY` (−1 top, 1 bottom), resolved against the box when drawn as CSS requires for corners.<br>`Angle`. The angle (double?).<br>`ToX`. The to x (int).<br>`ToY`. The to y (int).<br>`Stops`. The stops (ValueList<ColorStop>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(LinearGradient? left, LinearGradient? right)` |  |
| `public static bool operator ==(LinearGradient? left, LinearGradient? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(Gradient? other)` |  |
| `public bool Equals(LinearGradient? other)` |  |
| `public void Deconstruct(out double? Angle, out int ToX, out int ToY, out ValueList<ColorStop> Stops)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double? Angle { get; init; }` | The angle (double?). |
| `public int ToX { get; init; }` | The to x (int). |
| `public int ToY { get; init; }` | The to y (int). |

### `LogicalPseudo`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public enum LogicalPseudo` | Specifies the matching and specificity behavior of a logical pseudo-class. |
| `Is = 0` | Matches any argument selector and uses the greatest argument specificity. |
| `Not = 1` | Matches when none of the argument selectors match. |
| `Where = 2` | Matches any argument selector with zero specificity. |

### `LogicalSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record LogicalSelector : PseudoSelector, IEquatable<LogicalSelector>` | `:is()`, `:not()` and `:where()` with a list of complex selectors about the same element.<br>`Kind`. The logical pseudo represented by this value.<br>`Selectors`. The selectors (ValueList<Selector>). |
| `public LogicalSelector(LogicalPseudo Kind, ValueList<Selector> Selectors)` | `:is()`, `:not()` and `:where()` with a list of complex selectors about the same element.<br>`Kind`. The logical pseudo represented by this value.<br>`Selectors`. The selectors (ValueList<Selector>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(LogicalSelector? left, LogicalSelector? right)` |  |
| `public static bool operator ==(LogicalSelector? left, LogicalSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(PseudoSelector? other)` |  |
| `public bool Equals(LogicalSelector? other)` |  |
| `public void Deconstruct(out LogicalPseudo Kind, out ValueList<Selector> Selectors)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public LogicalPseudo Kind { get; init; }` | The logical pseudo represented by this value. |
| `public ValueList<Selector> Selectors { get; init; }` | The selectors (ValueList<Selector>). |

### `MatrixFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record MatrixFunction : TransformFunction, IEquatable<MatrixFunction>` | `matrix(a, b, c, d, e, f)`: x' = a·x + c·y + e, y' = b·x + d·y + f, translations in px.<br>`A`. The alpha channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`C`. The c (double).<br>`D`. The d (double).<br>`E`. The e (double).<br>`F`. The f (double). |
| `public MatrixFunction(double A, double B, double C, double D, double E, double F)` | `matrix(a, b, c, d, e, f)`: x' = a·x + c·y + e, y' = b·x + d·y + f, translations in px.<br>`A`. The alpha channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`C`. The c (double).<br>`D`. The d (double).<br>`E`. The e (double).<br>`F`. The f (double). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(MatrixFunction? left, MatrixFunction? right)` |  |
| `public static bool operator ==(MatrixFunction? left, MatrixFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(MatrixFunction? other)` |  |
| `public void Deconstruct(out double A, out double B, out double C, out double D, out double E, out double F)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double A { get; init; }` | The alpha channel in the range from zero to 255. |
| `public double B { get; init; }` | The blue channel in the range from zero to 255. |
| `public double C { get; init; }` | The c (double). |
| `public double D { get; init; }` | The d (double). |
| `public double E { get; init; }` | The e (double). |
| `public double F { get; init; }` | The f (double). |

### `MediaCondition`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record MediaCondition : IEquatable<MediaCondition>` | A conjunction of width/height bounds and preferences, e.g. `(min-width: 400px) and (prefers-color-scheme: dark)`; a null part is not tested.<br>`MinWidth`. The min width (double?).<br>`MaxWidth`. The max width (double?).<br>`MinHeight`. The min height (double?).<br>`MaxHeight`. The max height (double?).<br>`PrefersColorScheme`. The prefers color scheme (ColorScheme?).<br>`PrefersReducedMotion`. The prefers reduced motion (bool?).<br>`ForcedColors`. The forced colors (bool?).<br>`PrefersMoreContrast`. The prefers more contrast (bool?).<br>`Platform`. The platform (PlatformKind?). |
| `public MediaCondition(double? MinWidth, double? MaxWidth, double? MinHeight, double? MaxHeight, ColorScheme? PrefersColorScheme = null, bool? PrefersReducedMotion = null, bool? ForcedColors = null, bool? PrefersMoreContrast = null, PlatformKind? Platform = null, ControlDensity? Density = null)` | A conjunction of width/height bounds and preferences, e.g. `(min-width: 400px) and (prefers-color-scheme: dark)`; a null part is not tested.<br>`MinWidth`. The min width (double?).<br>`MaxWidth`. The max width (double?).<br>`MinHeight`. The min height (double?).<br>`MaxHeight`. The max height (double?).<br>`PrefersColorScheme`. The prefers color scheme (ColorScheme?).<br>`PrefersReducedMotion`. The prefers reduced motion (bool?).<br>`ForcedColors`. The forced colors (bool?).<br>`PrefersMoreContrast`. The prefers more contrast (bool?).<br>`Platform`. The platform (PlatformKind?). |
| `public static bool operator !=(MediaCondition? left, MediaCondition? right)` |  |
| `public static bool operator ==(MediaCondition? left, MediaCondition? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(MediaCondition? other)` |  |
| `public void Deconstruct(out double? MinWidth, out double? MaxWidth, out double? MinHeight, out double? MaxHeight, out ColorScheme? PrefersColorScheme, out bool? PrefersReducedMotion, out bool? ForcedColors, out bool? PrefersMoreContrast, out PlatformKind? Platform, out ControlDensity? Density)` |  |
| `public double? MinWidth { get; init; }` | The min width (double?). |
| `public double? MaxWidth { get; init; }` | The max width (double?). |
| `public double? MinHeight { get; init; }` | The min height (double?). |
| `public double? MaxHeight { get; init; }` | The max height (double?). |
| `public ColorScheme? PrefersColorScheme { get; init; }` | The prefers color scheme (ColorScheme?). |
| `public bool? PrefersReducedMotion { get; init; }` | The prefers reduced motion (bool?). |
| `public bool? ForcedColors { get; init; }` | The forced colors (bool?). |
| `public bool? PrefersMoreContrast { get; init; }` | The prefers more contrast (bool?). |
| `public PlatformKind? Platform { get; init; }` | The platform (PlatformKind?). |
| `public ControlDensity? Density { get; init; }` | Required compact/touch density, or null to ignore density. Nested conflicting density conditions never match. |

### `MediaPreferences`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct MediaPreferences : IEquatable<MediaPreferences>` | The user preferences that media queries test: the color scheme (the app mode of Windows), reduced motion ("Show animations" off) and forced colors (a contrast theme of Windows, which also means `prefers-contrast: more`); and the system colors that NSS names (`Canvas`…), the defaults when null. The default is light, with animations, without forced colors.<br>`ColorScheme`. The color scheme (ColorScheme).<br>`ReducedMotion`. The reduced motion (bool).<br>`ForcedColors`. The forced colors (bool).<br>`Colors`. The colors (SystemColors?).<br>`Platform`. The platform (PlatformKind?). |
| `public MediaPreferences()` |  |
| `public MediaPreferences(ColorScheme ColorScheme, bool ReducedMotion, bool ForcedColors = false, SystemColors? Colors = null, PlatformKind? Platform = null, ControlDensity Density = ControlDensity.Compact)` | The user preferences that media queries test: the color scheme (the app mode of Windows), reduced motion ("Show animations" off) and forced colors (a contrast theme of Windows, which also means `prefers-contrast: more`); and the system colors that NSS names (`Canvas`…), the defaults when null. The default is light, with animations, without forced colors.<br>`ColorScheme`. The color scheme (ColorScheme).<br>`ReducedMotion`. The reduced motion (bool).<br>`ForcedColors`. The forced colors (bool).<br>`Colors`. The colors (SystemColors?).<br>`Platform`. The platform (PlatformKind?). |
| `public override string ToString()` |  |
| `public static bool operator !=(MediaPreferences left, MediaPreferences right)` |  |
| `public static bool operator ==(MediaPreferences left, MediaPreferences right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(MediaPreferences other)` |  |
| `public void Deconstruct(out ColorScheme ColorScheme, out bool ReducedMotion, out bool ForcedColors, out SystemColors? Colors, out PlatformKind? Platform, out ControlDensity Density)` |  |
| `public ColorScheme ColorScheme { get; init; }` | The color scheme (ColorScheme). |
| `public bool ReducedMotion { get; init; }` | The reduced motion (bool). |
| `public bool ForcedColors { get; init; }` | The forced colors (bool). |
| `public SystemColors? Colors { get; init; }` | The colors (SystemColors?). |
| `public PlatformKind? Platform { get; init; }` | The platform (PlatformKind?). |
| `public ControlDensity Density { get; init; }` | Resolved control density, compact by constructor default. Automatic/default(struct) also matches compact in media evaluation. Window hosts resolve window/application/host precedence before publishing it. |

### `MediaQuery`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class MediaQuery` | A media query list: matches when any of its conditions matches.<br>`conditions`. The media conditions combined by the query. |
| `public MediaQuery(IReadOnlyList<MediaCondition> conditions)` | A media query list: matches when any of its conditions matches.<br>`conditions`. The media conditions combined by the query. |

### `MixFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record MixFunction : TransformFunction, IEquatable<MixFunction>` | The value of a transition between two transform lists that do not match function by function (CSS Transforms 1 §9): their matrices, which depend on the element's box, are interpolated by decomposition when the element is drawn. Never written in NSS.<br>`From`. The from (ValueList<TransformFunction>).<br>`To`. The to (ValueList<TransformFunction>).<br>`Progress`. The progress (double). |
| `public MixFunction(ValueList<TransformFunction> From, ValueList<TransformFunction> To, double Progress)` | The value of a transition between two transform lists that do not match function by function (CSS Transforms 1 §9): their matrices, which depend on the element's box, are interpolated by decomposition when the element is drawn. Never written in NSS.<br>`From`. The from (ValueList<TransformFunction>).<br>`To`. The to (ValueList<TransformFunction>).<br>`Progress`. The progress (double). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(MixFunction? left, MixFunction? right)` |  |
| `public static bool operator ==(MixFunction? left, MixFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(MixFunction? other)` |  |
| `public void Deconstruct(out ValueList<TransformFunction> From, out ValueList<TransformFunction> To, out double Progress)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public ValueList<TransformFunction> From { get; init; }` | The from (ValueList<TransformFunction>). |
| `public ValueList<TransformFunction> To { get; init; }` | The to (ValueList<TransformFunction>). |
| `public double Progress { get; init; }` | The progress (double). |

### `NativeColor`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/NativeColor.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct NativeColor : IEquatable<NativeColor>` | An explicit color or the default selected by the consuming property.<br>`Value`. Explicit color. Null means auto. |
| `public NativeColor()` |  |
| `public NativeColor(Color? Value)` | An explicit color or the default selected by the consuming property.<br>`Value`. Explicit color. Null means auto. |
| `public override string ToString()` |  |
| `public static bool operator !=(NativeColor left, NativeColor right)` |  |
| `public static bool operator ==(NativeColor left, NativeColor right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(NativeColor other)` |  |
| `public void Deconstruct(out Color? Value)` |  |
| `public Color? Value { get; init; }` | Explicit color; null means auto. |
| `public static NativeColor Auto { get; }` | Uses the automatic color for the property. |

### `NthSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record NthSelector : PseudoSelector, IEquatable<NthSelector>` | `:nth-child(An+B)` and its family: the element's 1-based position among its element siblings (or those of the same type), from the end with `Nazor.Style.NthSelector.FromEnd`, is A×n+B for some n ≥ 0. `:first-child` is (0, 1), `:last-child` the same from the end.<br>`A`. The alpha channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`FromEnd`. The from end (bool).<br>`OfType`. The of type (bool).<br>`Name`. The name used to identify the parameter, property, resource, or registration. |
| `public NthSelector(int A, int B, bool FromEnd, bool OfType, string Name)` | `:nth-child(An+B)` and its family: the element's 1-based position among its element siblings (or those of the same type), from the end with `Nazor.Style.NthSelector.FromEnd`, is A×n+B for some n ≥ 0. `:first-child` is (0, 1), `:last-child` the same from the end.<br>`A`. The alpha channel in the range from zero to 255.<br>`B`. The blue channel in the range from zero to 255.<br>`FromEnd`. The from end (bool).<br>`OfType`. The of type (bool).<br>`Name`. The name used to identify the parameter, property, resource, or registration. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(NthSelector? left, NthSelector? right)` |  |
| `public static bool operator ==(NthSelector? left, NthSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(PseudoSelector? other)` |  |
| `public bool Equals(NthSelector? other)` |  |
| `public void Deconstruct(out int A, out int B, out bool FromEnd, out bool OfType, out string Name)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public int A { get; init; }` | The alpha channel in the range from zero to 255. |
| `public int B { get; init; }` | The blue channel in the range from zero to 255. |
| `public bool FromEnd { get; init; }` | The from end (bool). |
| `public bool OfType { get; init; }` | The of type (bool). |
| `public string Name { get; init; }` | The name used to identify the parameter, property, resource, or registration. |

### `ObjectFit`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ObjectFit` | How an image fills its content box (`object-fit`). |
| `Fill = 0` | Stretches the image to fill the destination box. |
| `Contain = 1` | Preserves the aspect ratio while fitting the whole image inside the box. |
| `Cover = 2` | Preserves the aspect ratio while filling and cropping to the box. |
| `None = 3` | Uses the intrinsic image dimensions. |
| `ScaleDown = 4` | Uses the smaller result of intrinsic sizing and contain sizing. |

### `OnlyChildSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record OnlyChildSelector : PseudoSelector, IEquatable<OnlyChildSelector>` | `:only-child` and `:only-of-type`.<br>`OfType`. The of type (bool). |
| `public OnlyChildSelector(bool OfType)` | `:only-child` and `:only-of-type`.<br>`OfType`. The of type (bool). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(OnlyChildSelector? left, OnlyChildSelector? right)` |  |
| `public static bool operator ==(OnlyChildSelector? left, OnlyChildSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(PseudoSelector? other)` |  |
| `public bool Equals(OnlyChildSelector? other)` |  |
| `public void Deconstruct(out bool OfType)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public bool OfType { get; init; }` | The of type (bool). |

### `Overflow`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Overflow` | Specifies clipping and scrolling behavior when content exceeds its box. |
| `Visible = 0` | Paints overflowing content outside the box. |
| `Hidden = 1` | Clips overflow without exposing scrolling behavior. |
| `Scroll = 2` | Clips overflow and enables scrolling. |
| `Auto = 3` | Enables scrolling when content overflows. |

### `OverflowWrap`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum OverflowWrap` | Specifies when words may be broken to prevent text overflow. |
| `Normal = 0` | Breaks text only at normal wrapping opportunities. |
| `BreakWord = 1` | Allows an otherwise overflowing word to break, without reducing its intrinsic minimum width. |
| `Anywhere = 2` | Allows a word to break at any character boundary and includes those opportunities in intrinsic sizing. |

### `PendingDeclaration`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class PendingDeclaration : StyleDeclaration` | A longhand or shorthand whose value contains `var()`: parsed for each element once the custom properties are known. An invalid result behaves as `unset`.<br>`propertyName`. The canonical NSS property name.<br>`tokens`. The NSS lexical token sequence. |
| `public PendingDeclaration(string propertyName, IReadOnlyList<StyleToken> tokens)` | A longhand or shorthand whose value contains `var()`: parsed for each element once the custom properties are known. An invalid result behaves as `unset`.<br>`propertyName`. The canonical NSS property name.<br>`tokens`. The NSS lexical token sequence. |

### `PointerEvents`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum PointerEvents` | Specifies whether an element participates in pointer hit testing. |
| `Auto = 0` | Allows normal pointer hit testing. |
| `None = 1` | Excludes the element from pointer targeting. |

### `Position`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Position` | Specifies whether a node participates in normal flow or uses positioned layout. |
| `Static = 0` | Participates in normal flow without positional offsets. |
| `Relative = 1` | Participates in normal flow and applies offsets to its visual position. |
| `Absolute = 2` | Positions the node outside normal flow relative to its containing block. |

### `PositionValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct PositionValue : IEquatable<PositionValue>` | A point of a box from its top left corner, as `object-position` writes it: percentages are of the space left over (50% centers), lengths are offsets.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public PositionValue()` |  |
| `public PositionValue(Length X, Length Y)` | A point of a box from its top left corner, as `object-position` writes it: percentages are of the space left over (50% centers), lengths are offsets.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public static bool operator !=(PositionValue left, PositionValue right)` |  |
| `public static bool operator ==(PositionValue left, PositionValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(PositionValue other)` |  |
| `public void Deconstruct(out Length X, out Length Y)` |  |
| `public Length X { get; init; }` | The horizontal component of the value. |
| `public Length Y { get; init; }` | The vertical component of the value. |

### `PseudoSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record PseudoSelector : IEquatable<PseudoSelector>` | A pseudo-class that is not an interaction state: structural or logical. |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(PseudoSelector? left, PseudoSelector? right)` |  |
| `public static bool operator ==(PseudoSelector? left, PseudoSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(PseudoSelector? other)` |  |
| `protected PseudoSelector(PseudoSelector original)` |  |
| `protected PseudoSelector()` |  |
| `protected virtual Type EqualityContract { get; }` |  |

### `RadialExtent`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RadialExtent` | Specifies how a radial gradient's radii are derived from its center and painting box. |
| `FarthestCorner = 0` | Extends the gradient to the farthest corner of its painting box. |
| `ClosestCorner = 1` | Extends the gradient to the closest corner of its painting box. |
| `FarthestSide = 2` | Extends the gradient to the farthest side of its painting box. |
| `ClosestSide = 3` | Extends the gradient to the closest side of its painting box. |
| `Explicit = 4` | The radii are `Nazor.Style.RadialGradient.SizeX` and `Nazor.Style.RadialGradient.SizeY`. |

### `RadialGradient`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RadialGradient : Gradient, IEquatable<RadialGradient>` | `radial-gradient()`: an ellipse or a circle, sized by an extent keyword or by explicit radii (percentages of the box for an ellipse), centered at `Nazor.Style.RadialGradient.CenterX`, `Nazor.Style.RadialGradient.CenterY`.<br>`Shape`. The shape (RadialShape).<br>`Extent`. The extent (RadialExtent).<br>`SizeX`. The size x (Length).<br>`SizeY`. The size y (Length).<br>`CenterX`. The center x (Length).<br>`CenterY`. The center y (Length).<br>`Stops`. The stops (ValueList<ColorStop>). |
| `public RadialGradient(RadialShape Shape, RadialExtent Extent, Length SizeX, Length SizeY, Length CenterX, Length CenterY, ValueList<ColorStop> Stops)` | `radial-gradient()`: an ellipse or a circle, sized by an extent keyword or by explicit radii (percentages of the box for an ellipse), centered at `Nazor.Style.RadialGradient.CenterX`, `Nazor.Style.RadialGradient.CenterY`.<br>`Shape`. The shape (RadialShape).<br>`Extent`. The extent (RadialExtent).<br>`SizeX`. The size x (Length).<br>`SizeY`. The size y (Length).<br>`CenterX`. The center x (Length).<br>`CenterY`. The center y (Length).<br>`Stops`. The stops (ValueList<ColorStop>). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(RadialGradient? left, RadialGradient? right)` |  |
| `public static bool operator ==(RadialGradient? left, RadialGradient? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(Gradient? other)` |  |
| `public bool Equals(RadialGradient? other)` |  |
| `public void Deconstruct(out RadialShape Shape, out RadialExtent Extent, out Length SizeX, out Length SizeY, out Length CenterX, out Length CenterY, out ValueList<ColorStop> Stops)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public RadialShape Shape { get; init; }` | The shape (RadialShape). |
| `public RadialExtent Extent { get; init; }` | The extent (RadialExtent). |
| `public Length SizeX { get; init; }` | The size x (Length). |
| `public Length SizeY { get; init; }` | The size y (Length). |
| `public Length CenterX { get; init; }` | The center x (Length). |
| `public Length CenterY { get; init; }` | The center y (Length). |

### `RadialShape`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RadialShape` | Specifies whether a radial gradient uses a circle or ellipse. |
| `Ellipse = 0` | Uses independently resolved horizontal and vertical radii. |
| `Circle = 1` | Uses the same radius along both axes. |

### `RadiusValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct RadiusValue : IEquatable<RadiusValue>` | The radius of one corner: `X` horizontal, `Y` vertical, lengths or percentages of the border box.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public RadiusValue()` |  |
| `public RadiusValue(Length X, Length Y)` | The radius of one corner: `X` horizontal, `Y` vertical, lengths or percentages of the border box.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public static bool operator !=(RadiusValue left, RadiusValue right)` |  |
| `public static bool operator ==(RadiusValue left, RadiusValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(RadiusValue other)` |  |
| `public void Deconstruct(out Length X, out Length Y)` |  |
| `public Length X { get; init; }` | The horizontal component of the value. |
| `public Length Y { get; init; }` | The vertical component of the value. |

### `ResolvedKeyframe`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Keyframes.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ResolvedKeyframe : IEquatable<ResolvedKeyframe>` | A keyframe resolved for one element: its offset, timing function and computed values.<br>`Offset`. The offset (double).<br>`Easing`. The easing (EasingFunction?).<br>`Values`. The values (IReadOnlyDictionary<StyleProperty, object>). |
| `public ResolvedKeyframe(double Offset, EasingFunction? Easing, IReadOnlyDictionary<StyleProperty, object> Values)` | A keyframe resolved for one element: its offset, timing function and computed values.<br>`Offset`. The offset (double).<br>`Easing`. The easing (EasingFunction?).<br>`Values`. The values (IReadOnlyDictionary<StyleProperty, object>). |
| `public override string ToString()` |  |
| `public static bool operator !=(ResolvedKeyframe? left, ResolvedKeyframe? right)` |  |
| `public static bool operator ==(ResolvedKeyframe? left, ResolvedKeyframe? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(ResolvedKeyframe? other)` |  |
| `public void Deconstruct(out double Offset, out EasingFunction? Easing, out IReadOnlyDictionary<StyleProperty, object> Values)` |  |
| `public double Offset { get; init; }` | The offset (double). |
| `public EasingFunction? Easing { get; init; }` | The easing (EasingFunction?). |
| `public IReadOnlyDictionary<StyleProperty, object> Values { get; init; }` | The values (IReadOnlyDictionary<StyleProperty, object>). |

### `RootSelector`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RootSelector : PseudoSelector, IEquatable<RootSelector>` | `:root`: an element without a parent element (the window). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(RootSelector? left, RootSelector? right)` |  |
| `public static bool operator ==(RootSelector? left, RootSelector? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(PseudoSelector? other)` |  |
| `public bool Equals(RootSelector? other)` |  |
| `public RootSelector()` |  |
| `protected override Type EqualityContract { get; }` |  |

### `RotateFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record RotateFunction : TransformFunction, IEquatable<RotateFunction>` | `rotate()`, clockwise.<br>`Angle`. The angle (double). |
| `public RotateFunction(double Angle)` | `rotate()`, clockwise.<br>`Angle`. The angle (double). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(RotateFunction? left, RotateFunction? right)` |  |
| `public static bool operator ==(RotateFunction? left, RotateFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(RotateFunction? other)` |  |
| `public void Deconstruct(out double Angle)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double Angle { get; init; }` | The angle (double). |

### `ScaleFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record ScaleFunction : TransformFunction, IEquatable<ScaleFunction>` | `scale()`, `scaleX()`, `scaleY()`.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public ScaleFunction(double X, double Y)` | `scale()`, `scaleX()`, `scaleY()`.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(ScaleFunction? left, ScaleFunction? right)` |  |
| `public static bool operator ==(ScaleFunction? left, ScaleFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(ScaleFunction? other)` |  |
| `public void Deconstruct(out double X, out double Y)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double X { get; init; }` | The horizontal component of the value. |
| `public double Y { get; init; }` | The vertical component of the value. |

### `ScrollBehavior`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ScrollBehavior.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ScrollBehavior` | How wheel, keyboard and programmatic scroll requests move the content. |
| `Auto = 0` | Applies the requested offset immediately. |
| `Smooth = 1` | Interpolates to the requested offset using the configured duration and easing. |

### `ScrollWheelStep`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ScrollWheelStep.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ScrollWheelStep : IEquatable<ScrollWheelStep>` | A wheel distance; line counts define a distance, never a snapping grid.<br>`Kind`. The distance unit.<br>`Length`. The length when Kind is Length.<br>`Lines`. The line count when Kind is Lines. |
| `public static readonly ScrollWheelStep Auto` | Uses the backend's system wheel settings. |
| `public ScrollWheelStep()` |  |
| `public ScrollWheelStep(ScrollWheelStepKind Kind, Length Length, double Lines)` | A wheel distance; line counts define a distance, never a snapping grid.<br>`Kind`. The distance unit.<br>`Length`. The length when Kind is Length.<br>`Lines`. The line count when Kind is Lines. |
| `public override string ToString()` | Returns the NSS representation. |
| `public static bool operator !=(ScrollWheelStep left, ScrollWheelStep right)` |  |
| `public static bool operator ==(ScrollWheelStep left, ScrollWheelStep right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ScrollWheelStep other)` |  |
| `public void Deconstruct(out ScrollWheelStepKind Kind, out Length Length, out double Lines)` |  |
| `public ScrollWheelStepKind Kind { get; init; }` | The distance unit. |
| `public Length Length { get; init; }` | The length when Kind is Length. |
| `public double Lines { get; init; }` | The line count when Kind is Lines. |

### `ScrollWheelStepKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ScrollWheelStep.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ScrollWheelStepKind` | The unit used for one wheel detent. |
| `Auto = 0` | Uses the backend's system wheel settings. |
| `Length = 1` | Uses a logical length. |
| `Lines = 2` | Uses a multiple of the scroll container's computed line height. |

### `ScrollbarPlacement`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ScrollbarPlacement.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ScrollbarPlacement` | Controls whether a visible scrollbar overlays content or reserves space beside it. |
| `Overlay = 0` | Draws above the content without changing its available size. |
| `Reserve = 1` | Reserves space beside overflowing content. Autohide always uses overlay placement. |

### `ScrollbarVisibility`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/ScrollbarVisibility.cs)

| Declaration | Contract |
| --- | --- |
| `public enum ScrollbarVisibility` | Controls the appearance of scrollbars on overflowing scroll containers. |
| `None = 0` | Hides scrollbars while preserving wheel, keyboard and programmatic scrolling. |
| `Visible = 1` | Shows scrollbars whenever content overflows on the corresponding axis. |
| `AutoHide = 2` | Reveals scrollbars near the pointer, during thumb dragging or wheel scrolling, with configurable fades. |

### `SizeKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum SizeKind` | Specifies automatic, fixed, and intrinsic sizing modes. |
| `Auto = 0` | Lets the layout algorithm determine the size. |
| `None = 1` | Leaves the maximum size unconstrained. |
| `Content = 2` | Uses the content-based flex basis. |
| `Fixed = 3` | Resolves the supplied length to determine the size. |
| `MinContent = 4` | The narrowest width of the content: every soft wrap taken (CSS Sizing §2). |
| `MaxContent = 5` | The width of the content without wrapping. |
| `FitContent = 6` | The available width, but at least min-content and at most max-content. |

### `SizeValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct SizeValue : IEquatable<SizeValue>` | A size that may also be `auto`, `none` (for maxima), `content` (for flex-basis) or an intrinsic keyword (`min-content`, `max-content`, `fit-content`).<br>`Kind`. The size kind represented by this value.<br>`Length`. The absolute, relative, or percentage length to use. |
| `public SizeValue()` |  |
| `public SizeValue(SizeKind Kind, Length Length)` | A size that may also be `auto`, `none` (for maxima), `content` (for flex-basis) or an intrinsic keyword (`min-content`, `max-content`, `fit-content`).<br>`Kind`. The size kind represented by this value.<br>`Length`. The absolute, relative, or percentage length to use. |
| `public static bool operator !=(SizeValue left, SizeValue right)` |  |
| `public static bool operator ==(SizeValue left, SizeValue right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(SizeValue other)` |  |
| `public void Deconstruct(out SizeKind Kind, out Length Length)` |  |
| `public SizeKind Kind { get; init; }` | The size kind represented by this value. |
| `public Length Length { get; init; }` | The absolute, relative, or percentage length to use. |

### `SkewFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SkewFunction : TransformFunction, IEquatable<SkewFunction>` | `skew()`, `skewX()`, `skewY()`.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public SkewFunction(double X, double Y)` | `skew()`, `skewX()`, `skewY()`.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(SkewFunction? left, SkewFunction? right)` |  |
| `public static bool operator ==(SkewFunction? left, SkewFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(SkewFunction? other)` |  |
| `public void Deconstruct(out double X, out double Y)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public double X { get; init; }` | The horizontal component of the value. |
| `public double Y { get; init; }` | The vertical component of the value. |

### `Specificity`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/Selector.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Specificity : IComparable<Specificity>, IEquatable<Specificity>` | CSS specificity: identifiers, then classes and pseudo-classes, then types.<br>`Ids`. The ids (int).<br>`Classes`. The class names required by the compound selector.<br>`Types`. The types (int). |
| `public Specificity()` |  |
| `public Specificity(int Ids, int Classes, int Types)` | CSS specificity: identifiers, then classes and pseudo-classes, then types.<br>`Ids`. The ids (int).<br>`Classes`. The class names required by the compound selector.<br>`Types`. The types (int). |
| `public static bool operator !=(Specificity left, Specificity right)` |  |
| `public static bool operator ==(Specificity left, Specificity right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Specificity other)` |  |
| `public void Deconstruct(out int Ids, out int Classes, out int Types)` |  |
| `public int Ids { get; init; }` | The ids (int). |
| `public int Classes { get; init; }` | The class names required by the compound selector. |
| `public int Types { get; init; }` | The types (int). |

### `StyleEffects`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Properties/StyleProperty.cs)

| Declaration | Contract |
| --- | --- |
| `public enum StyleEffects` | What has to be redone when a computed value changes. |

### `StyleEngine`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleEngine.cs)

| Declaration | Contract |
| --- | --- |
| `public Func<ComputedStyle, FontRelativeMetrics>? FontMetricsResolver { get; set; }` | Optional logical font metrics callback for ch/ex. Parent font for font-size, own computed font otherwise. Missing metrics use0.5em; values must be finite/nonnegative. Replacement invalidates styles; providers must invalidate after registry changes. Dispose releases the callback. |
| `public int LastRecomputedCount { get; }` | Elements whose style was recomputed by the last `Nazor.Style.StyleEngine.Update`. |
| `public long TotalRecomputedCount { get; }` | Gets the total elements recomputed over this engine's lifetime, including lazy style lookups and recomputations that preserve the same values. |

### `StyleHotReload`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleHotReload.cs)

| Declaration | Contract |
| --- | --- |
| `public static Exception? LastError { get; }` | The first failure of the last delta, or null; reset at every delta. |

### `StyleLevel`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleEngine.cs)

| Declaration | Contract |
| --- | --- |
| `Theme = 0` | Applies theme defaults at the lowest stylesheet cascade level. |
| `Application = 1` | Applies application-wide stylesheet rules above the theme level. |
| `Window = 2` | Applies window-local stylesheet rules above application rules. |
| `Component = 3` | Applies component-scoped stylesheet rules at the highest stylesheet level. |

### `StyleProperties`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Properties/StyleProperties.cs)

| Declaration | Contract |
| --- | --- |
| `public static readonly StyleProperty<TimeSpan> OverlayExitDuration` | Gets the theme-controlled retention time for an overlay or retained surface exit; zero removes it immediately. |
| `public static readonly StyleProperty<TimeSpan> SubmenuOpenDelay` | Gets the theme-controlled pointer-hover opening delay. Noninherited/nonanimatable <time>, default 250ms, nonnegative finite intervals up to 4294967294ms. Keyboard and explicit activation remain immediate. |
| `public static readonly StyleProperty<ValueList<ImageValue>> BorderImageSource` | Gets the gradient painted across the border ring; none uses the individual border colors. |
| `public static readonly StyleProperty<bool> BackgroundClip` | Gets whether the background is clipped inside the border. |
| `public static readonly StyleProperty<Length> StrokeWidth` | Gets the width of check, chevron and ring vector strokes; percentages use the smaller content dimension and zero uses the shape's default width. |
| `public static readonly StyleProperty<Length> StrokeRadius` | Gets the ring vector stroke radius; percentages use the smaller content dimension and zero uses the shape's default radius. Open glyphs ignore this property. |
| `public static readonly StyleProperty<RingShape> RingShape` | Selects circle or rounded border-box ring geometry; zero stroke-radius uses a half-stroke inset. |
| `public static readonly StyleProperty<NumberMotionKind> NumberMotion` | None, linear, linear-increase or exponential numeric presentation; none initially. |
| `public static readonly StyleProperty<TimeSpan> NumberMotionDuration` | Linear time per scalar unit; zero initially. |
| `public static readonly StyleProperty<double> NumberMotionDecay` | Remaining-distance fraction removed per normalized 60Hz frame; zero initially. |
| `public static readonly StyleProperty<double> NumberMotionTolerance` | Nonnegative exponential snap tolerance; zero initially. |
| `public static readonly StyleProperty<double> NumberBoundaryTolerance` | Nonnegative displayed boundary tolerance, capped at half the range; zero initially. |
| `public static readonly StyleProperty<double> StrokeTrimStart` | Gets the start fraction of a check, chevron or ring stroke. Open glyphs trim by cumulative path length; ring fractions are relative to progress. |
| `public static readonly StyleProperty<double> StrokeTrimEnd` | Gets the end fraction of a check, chevron or ring stroke. End values at or below the start draw nothing. |
| `public static readonly StyleProperty<Length> OutlineInnerWidth` | Gets the width of a second outline ring immediately inside the primary outline. |
| `public static readonly StyleProperty<Color> OutlineInnerColor` | Gets the color of the inner outline ring. |
| `public static readonly StyleProperty<WindowFrameShadows> WindowFrameBoxShadow` | Exterior shadows of a frame drawn by Nazor, or the backend default. |
| `public static readonly StyleProperty<Length> WindowFrameBorderWidth` | Width of the outline drawn inside the visible frame. |
| `public static readonly StyleProperty<BorderStyle> WindowFrameBorderStyle` | Line style of the frame outline. |
| `public static readonly StyleProperty<Color> WindowFrameBorderColor` | Color of the frame outline. |
| `public static readonly StyleProperty<SizeValue> WindowFrameBorderTopLeftRadius` | Top-left frame corner radius, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameBorderTopRightRadius` | Top-right frame corner radius, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameBorderBottomRightRadius` | Bottom-right frame corner radius, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameBorderBottomLeftRadius` | Bottom-left frame corner radius, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameResizeInset` | Inner resize band in logical units, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameResizeOutset` | Outer resize band in logical units, or the backend default. |
| `public static readonly StyleProperty<SizeValue> WindowFrameResizeCornerLength` | Length of the diagonal resize region along each edge, or the backend default. |
| `public static readonly StyleProperty<bool> WindowResizeGrip` | Whether to display the client resize grip where supported. |
| `public static readonly StyleProperty<Color> WindowResizeGripColor` | Color of the resize grip glyph. |

### `WindowFrameShadows`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct WindowFrameShadows : IEquatable<WindowFrameShadows>` | Exterior window frame shadows. |
| `public WindowFrameShadows()` | Uses the backend default. |
| `public WindowFrameShadows(ValueList<BoxShadow>? Shadows)` | Uses the specified list. Null selects the backend default and an empty list removes the shadow. |
| `public ValueList<BoxShadow>? Shadows { get; init; }` | The explicit shadows or null for the backend default. |
| `public void Deconstruct(out ValueList<BoxShadow>? Shadows)` | Returns the shadow list. |
| `public bool Equals(WindowFrameShadows other)` | Compares shadow values. |
| `public override bool Equals(object obj)` | Compares shadow values. |
| `public override int GetHashCode()` | Returns the hash code for the shadow value. |
| `public static bool operator ==(WindowFrameShadows left, WindowFrameShadows right)` | Determines whether two shadow values are equal. |
| `public static bool operator !=(WindowFrameShadows left, WindowFrameShadows right)` | Determines whether two shadow values differ. |
| `public override string ToString()` | Returns the NSS representation. |

### `StyleRule`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class StyleRule` | A style rule after nesting expansion: one selector, an optional media query, declarations and source order.<br>`selector`. The selector (Selector).<br>`media`. The media (MediaQuery?).<br>`declarations`. The declarations assigned by the rule or keyframe.<br>`order`. The order (int). |
| `public StyleRule(Selector selector, MediaQuery? media, IReadOnlyList<StyleDeclaration> declarations, int order)` | A style rule after nesting expansion: one selector, an optional media query, declarations and source order.<br>`selector`. The selector (Selector).<br>`media`. The media (MediaQuery?).<br>`declarations`. The declarations assigned by the rule or keyframe.<br>`order`. The order (int). |

### `StyleTrace`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record StyleTrace : IEquatable<StyleTrace>` | How the style of an element came to be (`Nazor.Style.StyleEngine.Trace(Nazor.Rendering.ElementNode)`): the rules that select it in cascade order, the declarations of its style attribute, and every computed value with where it comes from. Plain data, for developer tools.<br>`Rules`. The style rules in cascade source order.<br>`Inline`. The inline (IReadOnlyList<TracedDeclaration>).<br>`Values`. The values (IReadOnlyList<TracedValue>).<br>`CustomProperties`. The custom properties (IReadOnlyList<TracedValue>). |
| `public StyleTrace(IReadOnlyList<TracedRule> Rules, IReadOnlyList<TracedDeclaration> Inline, IReadOnlyList<TracedValue> Values, IReadOnlyList<TracedValue> CustomProperties)` | How the style of an element came to be (`Nazor.Style.StyleEngine.Trace(Nazor.Rendering.ElementNode)`): the rules that select it in cascade order, the declarations of its style attribute, and every computed value with where it comes from. Plain data, for developer tools.<br>`Rules`. The style rules in cascade source order.<br>`Inline`. The inline (IReadOnlyList<TracedDeclaration>).<br>`Values`. The values (IReadOnlyList<TracedValue>).<br>`CustomProperties`. The custom properties (IReadOnlyList<TracedValue>). |
| `public override string ToString()` |  |
| `public static bool operator !=(StyleTrace? left, StyleTrace? right)` |  |
| `public static bool operator ==(StyleTrace? left, StyleTrace? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(StyleTrace? other)` |  |
| `public void Deconstruct(out IReadOnlyList<TracedRule> Rules, out IReadOnlyList<TracedDeclaration> Inline, out IReadOnlyList<TracedValue> Values, out IReadOnlyList<TracedValue> CustomProperties)` |  |
| `public IReadOnlyList<TracedRule> Rules { get; init; }` | The style rules in cascade source order. |
| `public IReadOnlyList<TracedDeclaration> Inline { get; init; }` | The inline (IReadOnlyList<TracedDeclaration>). |
| `public IReadOnlyList<TracedValue> Values { get; init; }` | The values (IReadOnlyList<TracedValue>). |
| `public IReadOnlyList<TracedValue> CustomProperties { get; init; }` | The custom properties (IReadOnlyList<TracedValue>). |

### `SystemColors`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/SystemColorPalette.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record SystemColors : IEquatable<SystemColors>` | The system colors of CSS (`Canvas`, `CanvasText`, `ButtonFace`…): the colors the user chose for the system, including contrast themes. NSS accepts CSS names wherever a color goes, including custom properties. Resolve completes backend colors for the desktop and appearance. Accent shades and additional semantic roles are root custom properties, for example `var(--nz-system-accent-dark-1)` and `var(--nz-system-tooltip-text)`. System keywords alias ordinary inherited custom properties: overriding `--nz-system-canvas` also changes `Canvas`. Prefer theme tokens such as `--nz-surface` for application customization. |
| `public static SystemColors Resolve(IReadOnlyDictionary<string, Color>? colors = null, ColorScheme scheme = ColorScheme.Light, SystemColorProfile profile = SystemColorProfile.Generic)` | Completes a partial backend palette for the selected desktop and appearance. Supplied colors win; missing accent shades and semantic roles are derived before falling back. Names are case-insensitive. The input is copied.<br>`colors`. CSS system colors, accent shades and additional PascalCase semantic roles; null uses profile defaults.<br>`scheme`. The effective light or dark appearance.<br>`profile`. The fallback desktop palette.<br>**Returns**. A complete palette independent of the input dictionary. |
| `public override string ToString()` |  |
| `public static bool operator !=(SystemColors? left, SystemColors? right)` |  |
| `public static bool operator ==(SystemColors? left, SystemColors? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(SystemColors? other)` |  |
| `public SystemColors()` |  |

### `TextAlign`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextAlign` | Specifies horizontal text alignment within a line box. |
| `Start = 0` | Aligns text to the start edge of its line box. |
| `End = 1` | Aligns text to the end edge of its line box. |
| `Left = 2` | Aligns text to the left edge of its line box. |
| `Right = 3` | Aligns text to the right edge of its line box. |
| `Center = 4` | Centers text within its line box. |

### `TextDecorationLine`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextDecorationLine` | The lines of `text-decoration-line`; any combination. |

### `TextDecorationStyle`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextDecorationStyle` | Specifies the stroke pattern for a text decoration. |
| `Solid = 0` | Draws a continuous decoration line. |
| `Double = 1` | Draws two parallel decoration lines. |
| `Dotted = 2` | Draws the decoration as dots. |
| `Dashed = 3` | Draws the decoration as dashes. |
| `Wavy = 4` | Draws the decoration as a wavy line. |

### `TextOverflow`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextOverflow` | Specifies how clipped text indicates horizontal overflow. |
| `Clip = 0` | Clips text at the available width. |
| `Ellipsis = 1` | Adds an ellipsis when text exceeds the available width. |

### `TextTransform`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TextTransform` | Specifies the case transformation applied to displayed text. |
| `None = 0` | Preserves the source text's case. |
| `Uppercase = 1` | Displays the text in uppercase. |
| `Lowercase = 2` | Displays the text in lowercase. |
| `Capitalize = 3` | Capitalizes the start of each word. |

### `TracedDeclaration`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TracedDeclaration : IEquatable<TracedDeclaration>` | A declaration of a rule or of the style attribute: the longhands it sets (a shorthand with `var()` sets several) and those for which it is the declaration that wins.<br>`Property`. The style property targeted by the value or declaration.<br>`Value`. The numeric value or payload represented by this record.<br>`Longhands`. The longhands (IReadOnlyList<string>).<br>`Winning`. The winning (IReadOnlyList<string>). |
| `public TracedDeclaration(string Property, string Value, IReadOnlyList<string> Longhands, IReadOnlyList<string> Winning)` | A declaration of a rule or of the style attribute: the longhands it sets (a shorthand with `var()` sets several) and those for which it is the declaration that wins.<br>`Property`. The style property targeted by the value or declaration.<br>`Value`. The numeric value or payload represented by this record.<br>`Longhands`. The longhands (IReadOnlyList<string>).<br>`Winning`. The winning (IReadOnlyList<string>). |
| `public override string ToString()` |  |
| `public static bool operator !=(TracedDeclaration? left, TracedDeclaration? right)` |  |
| `public static bool operator ==(TracedDeclaration? left, TracedDeclaration? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TracedDeclaration? other)` |  |
| `public void Deconstruct(out string Property, out string Value, out IReadOnlyList<string> Longhands, out IReadOnlyList<string> Winning)` |  |
| `public string Property { get; init; }` | The style property targeted by the value or declaration. |
| `public string Value { get; init; }` | The numeric value or payload represented by this record. |
| `public IReadOnlyList<string> Longhands { get; init; }` | The longhands (IReadOnlyList<string>). |
| `public IReadOnlyList<string> Winning { get; init; }` | The winning (IReadOnlyList<string>). |

### `TracedRule`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TracedRule : IEquatable<TracedRule>` | A rule whose selector matches the element, at its level of the cascade. A rule whose `@media` does not match now is listed too, without effect (`Nazor.Style.TracedRule.MediaMatches` false).<br>`Level`. The level (StyleLevel).<br>`Sheet`. The stylesheet to serialize.<br>`Selector`. The selector (string).<br>`Specificity`. The specificity (Specificity).<br>`Media`. The media (string?).<br>`MediaMatches`. The media matches (bool).<br>`Order`. The order (int).<br>`Declarations`. The declarations assigned by the rule or keyframe. |
| `public TracedRule(StyleLevel Level, string Sheet, string Selector, Specificity Specificity, string? Media, bool MediaMatches, int Order, IReadOnlyList<TracedDeclaration> Declarations)` | A rule whose selector matches the element, at its level of the cascade. A rule whose `@media` does not match now is listed too, without effect (`Nazor.Style.TracedRule.MediaMatches` false).<br>`Level`. The level (StyleLevel).<br>`Sheet`. The stylesheet to serialize.<br>`Selector`. The selector (string).<br>`Specificity`. The specificity (Specificity).<br>`Media`. The media (string?).<br>`MediaMatches`. The media matches (bool).<br>`Order`. The order (int).<br>`Declarations`. The declarations assigned by the rule or keyframe. |
| `public override string ToString()` |  |
| `public static bool operator !=(TracedRule? left, TracedRule? right)` |  |
| `public static bool operator ==(TracedRule? left, TracedRule? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TracedRule? other)` |  |
| `public void Deconstruct(out StyleLevel Level, out string Sheet, out string Selector, out Specificity Specificity, out string? Media, out bool MediaMatches, out int Order, out IReadOnlyList<TracedDeclaration> Declarations)` |  |
| `public StyleLevel Level { get; init; }` | The level (StyleLevel). |
| `public string Sheet { get; init; }` | The stylesheet to serialize. |
| `public string Selector { get; init; }` | The selector (string). |
| `public Specificity Specificity { get; init; }` | The specificity (Specificity). |
| `public string? Media { get; init; }` | The media (string?). |
| `public bool MediaMatches { get; init; }` | The media matches (bool). |
| `public int Order { get; init; }` | The order (int). |
| `public IReadOnlyList<TracedDeclaration> Declarations { get; init; }` | The declarations assigned by the rule or keyframe. |

### `TracedValue`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TracedValue : IEquatable<TracedValue>` | A computed value with its source: the index of the rule in `Nazor.Style.StyleTrace.Rules` and of the declaration in it (or in `Nazor.Style.StyleTrace.Inline`) that set it.<br>`Property`. The style property targeted by the value or declaration.<br>`Value`. The numeric value or payload represented by this record.<br>`Source`. The stylesheet source identity or input content.<br>`Rule`. The rule (int?).<br>`Declaration`. The declaration (int?). |
| `public TracedValue(string Property, string Value, ValueSource Source, int? Rule = null, int? Declaration = null)` | A computed value with its source: the index of the rule in `Nazor.Style.StyleTrace.Rules` and of the declaration in it (or in `Nazor.Style.StyleTrace.Inline`) that set it.<br>`Property`. The style property targeted by the value or declaration.<br>`Value`. The numeric value or payload represented by this record.<br>`Source`. The stylesheet source identity or input content.<br>`Rule`. The rule (int?).<br>`Declaration`. The declaration (int?). |
| `public override string ToString()` |  |
| `public static bool operator !=(TracedValue? left, TracedValue? right)` |  |
| `public static bool operator ==(TracedValue? left, TracedValue? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TracedValue? other)` |  |
| `public void Deconstruct(out string Property, out string Value, out ValueSource Source, out int? Rule, out int? Declaration)` |  |
| `public string Property { get; init; }` | The style property targeted by the value or declaration. |
| `public string Value { get; init; }` | The numeric value or payload represented by this record. |
| `public ValueSource Source { get; init; }` | The stylesheet source identity or input content. |
| `public int? Rule { get; init; }` | The rule (int?). |
| `public int? Declaration { get; init; }` | The declaration (int?). |

### `TrackBreadth`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TrackBreadth : IEquatable<TrackBreadth>` | One side of a track size: the minimum or the maximum sizing function (CSS Grid §7.2.3).<br>`Kind`. The track breadth kind represented by this value.<br>`Length`. The absolute, relative, or percentage length to use.<br>`Flex`. The flex (double). |
| `public TrackBreadth()` |  |
| `public TrackBreadth(TrackBreadthKind Kind, Length Length, double Flex)` | One side of a track size: the minimum or the maximum sizing function (CSS Grid §7.2.3).<br>`Kind`. The track breadth kind represented by this value.<br>`Length`. The absolute, relative, or percentage length to use.<br>`Flex`. The flex (double). |
| `public static bool operator !=(TrackBreadth left, TrackBreadth right)` |  |
| `public static bool operator ==(TrackBreadth left, TrackBreadth right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TrackBreadth other)` |  |
| `public void Deconstruct(out TrackBreadthKind Kind, out Length Length, out double Flex)` |  |
| `public TrackBreadthKind Kind { get; init; }` | The track breadth kind represented by this value. |
| `public Length Length { get; init; }` | The absolute, relative, or percentage length to use. |
| `public double Flex { get; init; }` | The flex (double). |

### `TrackBreadthKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public enum TrackBreadthKind` | Specifies how a grid track's breadth is sized. |
| `Length = 0` | A length or a percentage of the grid container's inner size. |
| `Flex = 1` | A share of the leftover space (`fr`); only as a maximum. |
| `Auto = 2` | Sizes the track from the automatic contributions of its items. |
| `MinContent = 3` | Uses the minimum intrinsic content contribution. |
| `MaxContent = 4` | Uses the maximum intrinsic content contribution. |

### `TrackSize`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GridValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TrackSize : IEquatable<TrackSize>` | The size of a grid track: `minmax(Min, Max)`. A plain length, `auto` or keyword is the same on both sides, `1fr` is `minmax(auto, 1fr)`; with `Nazor.Style.TrackSize.FitContent` the maximum is `max-content` clamped by the length in `Nazor.Style.TrackSize.Max` (`fit-content()`).<br>`Min`. The smallest accepted value.<br>`Max`. The largest accepted value.<br>`FitContent`. The fit content (bool). |
| `public TrackSize()` |  |
| `public TrackSize(TrackBreadth Min, TrackBreadth Max, bool FitContent = false)` | The size of a grid track: `minmax(Min, Max)`. A plain length, `auto` or keyword is the same on both sides, `1fr` is `minmax(auto, 1fr)`; with `Nazor.Style.TrackSize.FitContent` the maximum is `max-content` clamped by the length in `Nazor.Style.TrackSize.Max` (`fit-content()`).<br>`Min`. The smallest accepted value.<br>`Max`. The largest accepted value.<br>`FitContent`. The fit content (bool). |
| `public static bool operator !=(TrackSize left, TrackSize right)` |  |
| `public static bool operator ==(TrackSize left, TrackSize right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TrackSize other)` |  |
| `public void Deconstruct(out TrackBreadth Min, out TrackBreadth Max, out bool FitContent)` |  |
| `public TrackBreadth Min { get; init; }` | The smallest accepted value. |
| `public TrackBreadth Max { get; init; }` | The largest accepted value. |
| `public bool FitContent { get; init; }` | The fit content (bool). |

### `TransformFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public abstract record TransformFunction : IEquatable<TransformFunction>` | One function of `transform` (CSS Transforms 1, in 2D); angles are in degrees. |
| `public override string ToString()` |  |
| `protected virtual bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(TransformFunction? left, TransformFunction? right)` |  |
| `public static bool operator ==(TransformFunction? left, TransformFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public virtual bool Equals(TransformFunction? other)` |  |
| `protected TransformFunction(TransformFunction original)` |  |
| `protected virtual Type EqualityContract { get; }` |  |

### `TransformOrigin`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct TransformOrigin : IEquatable<TransformOrigin>` | The point `transform` turns around: lengths or percentages of the border box from its top left corner.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public TransformOrigin()` |  |
| `public TransformOrigin(Length X, Length Y)` | The point `transform` turns around: lengths or percentages of the border box from its top left corner.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public static bool operator !=(TransformOrigin left, TransformOrigin right)` |  |
| `public static bool operator ==(TransformOrigin left, TransformOrigin right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TransformOrigin other)` |  |
| `public void Deconstruct(out Length X, out Length Y)` |  |
| `public Length X { get; init; }` | The horizontal component of the value. |
| `public Length Y { get; init; }` | The vertical component of the value. |

### `TransitionDefinition`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TransitionDefinition : IEquatable<TransitionDefinition>` | One entry of the `transition` list; `Nazor.Style.TransitionDefinition.Property` is a property name or "all".<br>`Property`. The style property targeted by the value or declaration.<br>`Duration`. The duration of the animation or transition.<br>`Easing`. The easing (EasingFunction).<br>`Delay`. The delay before the operation or animation starts. |
| `public TransitionDefinition(string Property, TimeSpan Duration, EasingFunction Easing, TimeSpan Delay)` | One entry of the `transition` list; `Nazor.Style.TransitionDefinition.Property` is a property name or "all".<br>`Property`. The style property targeted by the value or declaration.<br>`Duration`. The duration of the animation or transition.<br>`Easing`. The easing (EasingFunction).<br>`Delay`. The delay before the operation or animation starts. |
| `public static bool operator !=(TransitionDefinition? left, TransitionDefinition? right)` |  |
| `public static bool operator ==(TransitionDefinition? left, TransitionDefinition? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TransitionDefinition? other)` |  |
| `public void Deconstruct(out string Property, out TimeSpan Duration, out EasingFunction Easing, out TimeSpan Delay)` |  |
| `public string Property { get; init; }` | The style property targeted by the value or declaration. |
| `public TimeSpan Duration { get; init; }` | The duration of the animation or transition. |
| `public EasingFunction Easing { get; init; }` | The easing (EasingFunction). |
| `public TimeSpan Delay { get; init; }` | The delay before the operation or animation starts. |

### `TranslateFunction`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/TransformValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record TranslateFunction : TransformFunction, IEquatable<TranslateFunction>` | `translate()`, `translateX()`, `translateY()`: percentages of the element's border box.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `public TranslateFunction(Length X, Length Y)` | `translate()`, `translateX()`, `translateY()`: percentages of the element's border box.<br>`X`. The horizontal component of the value.<br>`Y`. The vertical component of the value. |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(TranslateFunction? left, TranslateFunction? right)` |  |
| `public static bool operator ==(TranslateFunction? left, TranslateFunction? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(TransformFunction? other)` |  |
| `public bool Equals(TranslateFunction? other)` |  |
| `public void Deconstruct(out Length X, out Length Y)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public Length X { get; init; }` | The horizontal component of the value. |
| `public Length Y { get; init; }` | The vertical component of the value. |

### `UrlImage`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed record UrlImage : ImageValue, IEquatable<UrlImage>` | `url()`: an image file. The compiler resolves a relative URL against the folder of the style sheet, so the value is relative to the project (and to the application folder, where the SDK copies the images); http and https URLs stay as written.<br>`Url`. The url (string). |
| `public UrlImage(string Url)` | `url()`: an image file. The compiler resolves a relative URL against the folder of the style sheet, so the value is relative to the project (and to the application folder, where the SDK copies the images); http and https URLs stay as written.<br>`Url`. The url (string). |
| `protected override bool PrintMembers(StringBuilder builder)` |  |
| `public static bool operator !=(UrlImage? left, UrlImage? right)` |  |
| `public static bool operator ==(UrlImage? left, UrlImage? right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object? obj)` |  |
| `public override sealed bool Equals(ImageValue? other)` |  |
| `public bool Equals(UrlImage? other)` |  |
| `public void Deconstruct(out string Url)` |  |
| `protected override Type EqualityContract { get; }` |  |
| `public string Url { get; init; }` | The url (string). |

### `UserSelect`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum UserSelect` | Specifies whether and how users can select the element's text. |
| `Auto = 0` | Uses the control-specific selection behavior. |
| `None = 1` | Disables user text selection. |
| `Text = 2` | Allows selecting text ranges. |
| `All = 3` | Selects the entire text as a unit. |

### `ValueDeclaration`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class ValueDeclaration : StyleDeclaration` | A longhand with a value already parsed and validated by the compiler.<br>`property`. The style property to read or update.<br>`value`. The value to use. |
| `public ValueDeclaration(StyleProperty property, object value)` | A longhand with a value already parsed and validated by the compiler.<br>`property`. The style property to read or update.<br>`value`. The value to use. |

### `ValueSource`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/StyleTrace.cs)

| Declaration | Contract |
| --- | --- |
| `Rule = 0` | A declaration of a rule (`Nazor.Style.TracedValue.Rule`). |
| `Inline = 1` | A declaration of the style attribute. |
| `Inherited = 2` | Not declared: the value of the parent, for an inherited property. |
| `Initial = 3` | Not declared: the initial value of the property. |

### `Viewport`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Sheets/StyleSheet.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct Viewport : IEquatable<Viewport>` | The size of the window content in logical units, tested by media queries.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public Viewport()` |  |
| `public Viewport(double Width, double Height)` | The size of the window content in logical units, tested by media queries.<br>`Width`. The width of the value in its coordinate system.<br>`Height`. The height of the value in its coordinate system. |
| `public override string ToString()` |  |
| `public static bool operator !=(Viewport left, Viewport right)` |  |
| `public static bool operator ==(Viewport left, Viewport right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Viewport other)` |  |
| `public void Deconstruct(out double Width, out double Height)` |  |
| `public double Width { get; init; }` | The width of the value in its coordinate system. |
| `public double Height { get; init; }` | The height of the value in its coordinate system. |

### `Visibility`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum Visibility` | Specifies whether an element is painted while retaining its layout space. |
| `Visible = 0` | Paints the element normally. |
| `Hidden = 1` | Suppresses painting while preserving the layout space. |

### `WhiteSpace`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public enum WhiteSpace` | Specifies whitespace collapsing and text wrapping behavior. |
| `Normal = 0` | Collapses whitespace and wraps text as needed. |
| `NoWrap = 1` | Collapses whitespace and disables line wrapping. |
| `Pre = 2` | Preserves whitespace and explicit line breaks without wrapping. |
| `PreWrap = 3` | Preserves whitespace and explicit line breaks and permits wrapping. |
| `PreLine = 4` | Collapses spaces, preserves explicit line breaks, and permits wrapping. |

### `ZIndex`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/Values.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct ZIndex : IEquatable<ZIndex>` | The `z-index` of an element: `auto` or an integer.<br>`IsAuto`. The is auto (bool).<br>`Value`. The numeric value or payload represented by this record. |
| `public ZIndex()` |  |
| `public ZIndex(bool IsAuto, int Value)` | The `z-index` of an element: `auto` or an integer.<br>`IsAuto`. The is auto (bool).<br>`Value`. The numeric value or payload represented by this record. |
| `public static bool operator !=(ZIndex left, ZIndex right)` |  |
| `public static bool operator ==(ZIndex left, ZIndex right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ZIndex other)` |  |
| `public void Deconstruct(out bool IsAuto, out int Value)` |  |
| `public bool IsAuto { get; init; }` | The is auto (bool). |
| `public int Value { get; init; }` | The numeric value or payload represented by this record. |

### `StyleToken`

Namespace `Nazor.Style.Syntax`. [Source](../../Sources/Core/Nazor.Style/Syntax/StyleToken.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct StyleToken : IEquatable<StyleToken>` | A token of NSS syntax, including CSS escapes in quoted strings but not escaped identifiers or full URL tokens. `Nazor.Style.Syntax.StyleToken.Value` is the identifier, function or at-keyword name, string contents, hash name, unit or delimiter; `Nazor.Style.Syntax.StyleToken.Start` is the offset in the tokenized text.<br>`Kind`. The style token kind represented by this value.<br>`Value`. The numeric value or payload represented by this record.<br>`Number`. The number (double).<br>`Start`. The start offset in the UTF-16 source text.<br>`Length`. The length of the token in the original UTF-16 source text. |
| `public StyleToken()` |  |
| `public StyleToken(StyleTokenKind Kind, string Value, double Number, int Start, int Length)` | A token of NSS syntax, including CSS escapes in quoted strings but not escaped identifiers or full URL tokens. `Nazor.Style.Syntax.StyleToken.Value` is the identifier, function or at-keyword name, string contents, hash name, unit or delimiter; `Nazor.Style.Syntax.StyleToken.Start` is the offset in the tokenized text.<br>`Kind`. The style token kind represented by this value.<br>`Value`. The numeric value or payload represented by this record.<br>`Number`. The number (double).<br>`Start`. The start offset in the UTF-16 source text.<br>`Length`. The length of the token in the original UTF-16 source text. |
| `public static bool operator !=(StyleToken left, StyleToken right)` |  |
| `public static bool operator ==(StyleToken left, StyleToken right)` |  |
| `public override int GetHashCode()` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(StyleToken other)` |  |
| `public void Deconstruct(out StyleTokenKind Kind, out string Value, out double Number, out int Start, out int Length)` |  |
| `public StyleTokenKind Kind { get; init; }` | The style token kind represented by this value. |
| `public string Value { get; init; }` | The numeric value or payload represented by this record. |
| `public double Number { get; init; }` | The number (double). |
| `public int Start { get; init; }` | The start offset in the UTF-16 source text. |
| `public int Length { get; init; }` | The length of the token in the original UTF-16 source text. |

### `StyleTokenKind`

Namespace `Nazor.Style.Syntax`. [Source](../../Sources/Core/Nazor.Style/Syntax/StyleToken.cs)

| Declaration | Contract |
| --- | --- |
| `public enum StyleTokenKind` | Identifies the lexical category of an NSS token. |
| `Ident = 0` | Represents an identifier. |
| `Function = 1` | Represents a function name and opening parenthesis. |
| `AtKeyword = 2` | Represents an at-rule keyword. |
| `Hash = 3` | Represents a hash-prefixed value. |
| `String = 4` | Represents a quoted string. |
| `Number = 5` | Represents a unitless number. |
| `Percentage = 6` | Represents a percentage. |
| `Dimension = 7` | Represents a number with a unit. |
| `Whitespace = 8` | Represents whitespace. |
| `Colon = 9` | Represents a colon. |
| `Semicolon = 10` | Represents a semicolon. |
| `Comma = 11` | Represents a comma. |
| `LeftBracket = 12` | Represents an opening square bracket. |
| `RightBracket = 13` | Represents a closing square bracket. |
| `LeftParen = 14` | Represents an opening parenthesis. |
| `RightParen = 15` | Represents a closing parenthesis. |
| `LeftBrace = 16` | Represents an opening brace. |
| `RightBrace = 17` | Represents a closing brace. |
| `Delim = 18` | Represents a delimiter character. |
| `BadString = 19` | Represents an unterminated or invalid string. |
| `EndOfFile = 20` | Represents the end of the source text. |

### `StyleValueException`

Namespace `Nazor.Style.Syntax`. [Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class StyleValueException : Exception` | Raised when a value does not match the grammar of a property.<br>`message`. The message (string).<br>`token`. The NSS token identifying the value or error location. |
| `public StyleValueException(string message, StyleToken token)` | Raised when a value does not match the grammar of a property.<br>`message`. The message (string).<br>`token`. The NSS token identifying the value or error location. |

### `TokenReader`

Namespace `Nazor.Style.Syntax`. [Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

| Declaration | Contract |
| --- | --- |
| `public sealed class TokenReader` | Reads the component values of a declaration, skipping whitespace on request.<br>`tokens`. The NSS lexical token sequence. |
| `public TokenReader(IReadOnlyList<StyleToken> tokens)` | Reads the component values of a declaration, skipping whitespace on request.<br>`tokens`. The NSS lexical token sequence. |

### `ValueParsers`

Namespace `Nazor.Style.Syntax`. [Source](../../Sources/Core/Nazor.Style/Syntax/TokenReader.cs)

| Declaration | Contract |
| --- | --- |
| `public static double ParseNumber(TokenReader reader, double min = -1.7976931348623157E+308, double max = 1.7976931348623157E+308)` | Parses a number or a finite unitless calc, min, max or clamp expression from the current token position.<br>`reader`. The token reader positioned at the start of the value.<br>`min`. The smallest accepted value.<br>`max`. The largest accepted value.<br>**Returns**. The resulting double value. |


<a id="type-stylesheet"></a>

### `StyleSheet`

Namespace `Nazor.Style`.

A compiled stylesheet: rules in source order and @keyframes rules. Built by generated code, never by parsing NSS at runtime. Rules and keyframes are one immutable snapshot that development tools may swap with `Nazor.Style.StyleSheet.Replace(Nazor.Style.StyleSheet)` while the sheet keeps its identity; readers take `Nazor.Style.StyleSheet.Rules` once.

| Declaration | Contract |
| --- | --- |
| `public sealed class StyleSheet : IStyleSheet` | A compiled stylesheet: rules in source order and @keyframes rules. Built by generated code, never by parsing NSS at runtime. Rules and keyframes are one immutable snapshot that development tools may swap with `Nazor.Style.StyleSheet.Replace(Nazor.Style.StyleSheet)` while the sheet keeps its identity; readers take `Nazor.Style.StyleSheet.Rules` once. |
| `public static readonly StyleSheet Empty` | A sheet without rules; never replaced and never tracked by `Nazor.Style.StyleRegistry`. |
| `public StyleSheet WithWindowDecorations(WindowDecorationPresenters presenters)` | Associates statically registered decoration presenters with a new sheet while preserving this sheet and its live rules. A live composition with the supplied presenter metadata. |
| `public static StyleSheet Combine(params StyleSheet[] sheets)` | Combines compiled stylesheets into one sheet. Rules keep the supplied sheet order, and later sheets win among equal-specificity rules. Replacing an input sheet updates the combined sheet in place. The combined stylesheet, or the sole input when one sheet is supplied. |
| `public StyleSheet(string source, IReadOnlyList<StyleRule> rules)` | Initializes a new instance of StyleSheet. |
| `public StyleSheet(string source, IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes)` | Initializes a new instance of StyleSheet. |
| `public KeyframesRule? FindKeyframes(string name)` | The last @keyframes rule with this name (names are case-sensitive, as in CSS), or null. The resulting KeyframesRule? value. |
| `public bool Replace(StyleSheet sheet)` | Replaces the rules and keyframes and notifies consumers when the replacement changes the sheet. False, doing nothing, when `Nazor.Style.StyleSheet.Describe` of the new content equals the current one. |
| `public bool Replace(IReadOnlyList<StyleRule> rules)` | Replaces the style rules while retaining the current keyframes, notifying consumers when the content changes. true when the sheet was replaced by different content; otherwise, false. |
| `public bool Replace(IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes)` | Replaces the rules and keyframes and notifies consumers when the replacement changes the sheet. true when the sheet was replaced by different content; otherwise, false. |
| `public string Describe()` | Canonical text of the compiled sheet: one line per rule with media, selector, specificity and declarations, then one line per @keyframes rule. The resulting string value. |
| `public WindowDecorationPresenters? WindowDecorations { get; }` | Decoration presenters associated with this sheet, or inherited from the last composed sheet that supplies them. |
| `public string Source { get; }` | Origin of the rules: the path relative to the project with '/' separators (for example "Styles/Dark.nss" or "Components/DeviceCard.nazor"), or the file name alone when the project directory is unknown. Development tools find live sheets by it. |
| `public IReadOnlyList<StyleRule> Rules { get; }` | The current rule snapshot. Read it once into a local: `Nazor.Style.StyleSheet.Replace(Nazor.Style.StyleSheet)` swaps it as a whole. |
| `public IReadOnlyList<KeyframesRule> Keyframes { get; }` | The @keyframes rules, in source order. |
| `public int Version { get; }` | 0 at creation, incremented by every `Nazor.Style.StyleSheet.Replace(Nazor.Style.StyleSheet)` that changed the rules. |
| `public bool HasMediaQueries { get; }` | Gets whether any stylesheet rule is guarded by a media query. |
| `public bool HasStructuralSelectors { get; }` | Some rule depends on siblings or children (:first-child, +, :emptyâ€¦): child list changes restyle. |

See [title bars and platform integration](../Guide/14-Title-Bars-and-Platform-Integration.md) for complete configuration policies, capability checks, explicit fallback and User content lifecycle.

<a id="type-relativeselector"></a>

## `RelativeSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record RelativeSelector : IEquatable<RelativeSelector>` | An immutable leading-relation/selector pair. |
| `public RelativeSelector(Combinator Relation, Selector Selector)` | The complex selector evaluated relative to the origin. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `public static bool operator !=(RelativeSelector? left, RelativeSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(RelativeSelector? left, RelativeSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public bool Equals(RelativeSelector? other)` | Compare immutable values of the same type. |
| `public void Deconstruct(out Combinator Relation, out Selector Selector)` | Read the positional record values into the corresponding outputs. |
| `public Combinator Relation { get; init; }` | The leading descendant, child, next-sibling or following-sibling relation. |
| `public Selector Selector { get; init; }` | The complex selector evaluated relative to the origin. |
<a id="type-hasselector"></a>

## `HasSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record HasSelector : PseudoSelector, IEquatable<HasSelector>` | An immutable relational selector with anchored relative arguments. |
| `public HasSelector(ValueList<RelativeSelector> Selectors)` | Create immutable relative-list selector data. Compiler/JSON input enforces nonempty lists, supported relations and nesting restrictions. |
| `public override bool Matches(ElementNode element)` | Test anchored relative selectors without a sheet scope. The style engine also supplies scoped matching. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(HasSelector? left, HasSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(HasSelector? left, HasSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare immutable values of the same type. |
| `public bool Equals(HasSelector? other)` | Compare immutable values of the same type. |
| `public void Deconstruct(out ValueList<RelativeSelector> Selectors)` | Read the positional record values into the corresponding outputs. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |
| `public ValueList<RelativeSelector> Selectors { get; init; }` | The nonempty list of supported relative selector arguments. |
| `public override Specificity Specificity { get; }` | Greatest argument specificity, without an additional pseudo-class unit. |


<a id="type-namedstateselector"></a>

## `NamedStateSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record NamedStateSelector : PseudoSelector, IEquatable<NamedStateSelector>` | A pseudo-class matching one explicitly published custom state. |
| `public NamedStateSelector(string name)` | Create a selector for one simple case-sensitive identifier; invalid names throw ArgumentException. |
| `public override bool Matches(ElementNode element)` | Test the typed custom-states attribute for this case-sensitive name; states are not inherited. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(NamedStateSelector? left, NamedStateSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(NamedStateSelector? left, NamedStateSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare immutable values of the same type. |
| `public bool Equals(NamedStateSelector? other)` | Compare immutable values of the same type. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |
| `public string Name { get; }` | The case-sensitive published identifier. |

<a id="type-partselector"></a>

## `PartSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record PartSelector : PseudoSelector, IEquatable<PartSelector>` | A component-qualified selector for an existing public part. |
| `public PartSelector(string component, string name)` | Create a component-qualified part selector; invalid simple names throw ArgumentException. |
| `public override bool Matches(ElementNode element)` | Test explicit part bindings and logical source/exposing-instance ownership; creates no element. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(PartSelector? left, PartSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(PartSelector? left, PartSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare immutable values of the same type. |
| `public bool Equals(PartSelector? other)` | Compare immutable values of the same type. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |
| `public string Component { get; }` | The case-sensitive exposing component simple name, without generic arity. |
| `public string Name { get; }` | The case-sensitive published identifier. |
| `public override Specificity Specificity { get; }` | Component type plus part specificity: (0,0,2). |
<a id="type-stylepartset"></a>

## `StylePartSet`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed class StylePartSet : IEquatable<StylePartSet>` | Immutable instance-owned public part declarations; publish through style-parts. |
| `public StylePartSet Expose(IComponent owner, string name)` | Add an explicit owner/name binding. Null owner or an invalid identifier throws; duplicate bindings leave the snapshot unchanged. |
| `public StylePartSet Export(IComponent owner, string name, StylePartSet exports)` | Add requested outer names to an already exposed local binding while retaining its original source owner. Throws for null arguments or an unknown local part. |
| `public bool Equals(StylePartSet? other)` | Compare immutable values of the same type; component bindings compare owner/source instances by reference. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type; component bindings compare owner/source instances by reference. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public static StylePartSet Empty { get; }` | The shared empty immutable declaration. |

<a id="type-componentrootselector"></a>

## `ComponentRootSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record ComponentRootSelector : PseudoSelector, IEquatable<ComponentRootSelector>` | A scoped-only selector for the current component instance's physical declaration roots. |
| `public override bool Matches(ElementNode element)` | Return false without sheet scope. The engine matches scoped declaration-instance roots and stops at the first physical parent. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(ComponentRootSelector? left, ComponentRootSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(ComponentRootSelector? left, ComponentRootSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare immutable values of the same type. |
| `public bool Equals(ComponentRootSelector? other)` | Compare immutable values of the same type. |
| `public ComponentRootSelector()` | Create an immutable scoped-root selector. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |
<a id="type-itempositionselector"></a>

## `ItemPositionSelector`

Namespace: `Nazor.Style`

| Declaration | Contract |
| --- | --- |
| `public sealed record ItemPositionSelector : PseudoSelector, IEquatable<ItemPositionSelector>` | A pseudo-class matching explicit logical collection positions. |
| `public ItemPositionSelector(int A, int B, bool FromEnd, string Name)` | Create explicit logical-position selector data using An+B and the supplied NSS spelling. |
| `public override bool Matches(ElementNode element)` | Test valid typed item-position metadata against An+B, without counting realized siblings or enumerating data. |
| `public override string ToString()` | Return the NSS selector spelling. |
| `protected override bool PrintMembers(StringBuilder builder)` | Append record values to a diagnostic builder. |
| `public static bool operator !=(ItemPositionSelector? left, ItemPositionSelector? right)` | Compare immutable values for equality or inequality. |
| `public static bool operator ==(ItemPositionSelector? left, ItemPositionSelector? right)` | Compare immutable values for equality or inequality. |
| `public override int GetHashCode()` | Hash the value consistently with equality. |
| `public override bool Equals(object? obj)` | Compare immutable values of the same type. |
| `public override sealed bool Equals(PseudoSelector? other)` | Compare immutable values of the same type. |
| `public bool Equals(ItemPositionSelector? other)` | Compare immutable values of the same type. |
| `public void Deconstruct(out int A, out int B, out bool FromEnd, out string Name)` | Read the positional record values into the corresponding outputs. |
| `protected override Type EqualityContract { get; }` | The runtime record type participating in equality. |
| `public int A { get; init; }` | The signed An+B step; zero tests only B. |
| `public int B { get; init; }` | The signed one-based An+B offset. |
| `public bool FromEnd { get; init; }` | Count from the complete known end when true. |
| `public string Name { get; init; }` | The NSS spelling supplied for this position selector. |

`PseudoClasses.Names` also includes `popup-origin-right` and `popup-origin-bottom`, mapped to presentation-maintained ElementStates flags. They report the nearest arranged corner, independent of the requested side.

<a id="type-ringshape"></a>

## `RingShape`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/RingShape.cs)

| Declaration | Contract |
| --- | --- |
| `public enum RingShape` | Byte-backed progress-path geometry. |
| `Circle = 0` | Keeps the existing content-box circle. |
| `BorderBox = 1` | Follows the rounded border rectangle, starting on the top edge nearest its center; trims measure cumulative clockwise path length. |

<a id="type-numbermotionkind"></a>

## `NumberMotionKind`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/NumberMotionKind.cs)

| Declaration | Contract |
| --- | --- |
| `public enum NumberMotionKind` | Byte-backed scalar presentation policy. |
| `None = 0` | Applies the target immediately. |
| `Linear = 1` | Uses number-motion-duration per scalar unit. |
| `LinearIncrease = 2` | Animates increases linearly and applies decreases immediately. |
| `Exponential = 3` | Removes number-motion-decay of the remaining distance per normalized 60Hz reference frame and stops at its configured tolerance. |

See [numeric presentation](../Guide/23-Transitions-and-Animations.md#numeric-presentation).

`ValueDeclaration.ToString()` emits a descriptor's canonical registered enum keyword, invariant millisecond times and round-trip numeric values, so compound keywords such as `border-box`/`linear-increase` and numeric-motion durations compile again after NSS serialization. Combined decoration lines are ordered `underline overline line-through` with absent lines omitted; grid flow uses `row`, `column`, `dense` or `column dense`. These textual forms preserve parsed enum values when recompiled.


<a id="type-fontopticalsizing"></a>

## `FontOpticalSizing`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/FontVariationSetting.cs)

| Declaration | Contract |
| --- | --- |
| `public enum FontOpticalSizing` | NSS automatic optical-size policy. |
| `Auto = 0` | Initial CSS policy using logical pixels. |
| `None = 1` | Disables automatic optical sizing. |
| `Native = 2` | NSS extension using typographic points. |

<a id="type-fontvariationsetting"></a>

## `FontVariationSetting`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/FontVariationSetting.cs)

| Declaration | Contract |
| --- | --- |
| `public readonly struct FontVariationSetting : IEquatable<FontVariationSetting>` | A case-sensitive explicit OpenType axis coordinate. |
| `public FontVariationSetting()` | The default value is not a valid coordinate. Use the validating constructor. |
| `public FontVariationSetting(string tag, double value)` | Exactly four printable ASCII characters; finite double coordinate. Invalid tag throws ArgumentException; nonfinite coordinate throws ArgumentOutOfRangeException. |
| `public string Tag { get; }` | Four-character tag. |
| `public double Value { get; }` | Finite design coordinate, clamped by the selected font backend. |
| `public override string ToString()` | Safely quotes the NSS tag and preserves the numeric coordinate. |
| `public bool Equals(FontVariationSetting other)` | Value equality. |
| `public override bool Equals(object obj)` | Value equality. |
| `public override int GetHashCode()` | Hash consistent with equality. |
| `public static bool operator ==(FontVariationSetting left, FontVariationSetting right)` | Value equality. |
| `public static bool operator !=(FontVariationSetting left, FontVariationSetting right)` | Value inequality. |

### `FontRelativeMetrics`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Engine/FontRelativeMetrics.cs)

Horizontal logical font measurements supplied to StyleEngine without a graphics-backend dependency. Null metrics independently use0.5em. Scene supplies its real provider's zero advance/x-height and refreshes computed values after font registrations.

| Declaration | Contract |
| --- | --- |
| `public readonly struct FontRelativeMetrics : IEquatable<FontRelativeMetrics>` | Immutable optional logical measurements. |
| `public FontRelativeMetrics()` | Both measurements unavailable. |
| `public FontRelativeMetrics(double? ZeroAdvance = null, double? XHeight = null)` | Creates a metric pair. Available values must be finite and nonnegative. |
| `public double? ZeroAdvance { get; init; }` | Unshaped horizontal zero-glyph advance. |
| `public double? XHeight { get; init; }` | First available font x-height. |
| `public void Deconstruct(out double? ZeroAdvance, out double? XHeight)` | Deconstructs the pair. |
| `public override string ToString()` | Formats the value. |
| `public static bool operator !=(FontRelativeMetrics left, FontRelativeMetrics right)` | Value inequality. |
| `public static bool operator ==(FontRelativeMetrics left, FontRelativeMetrics right)` | Value equality. |
| `public override int GetHashCode()` | Value hash. |
| `public override bool Equals(object obj)` | Value equality. |
| `public bool Equals(FontRelativeMetrics other)` | Value equality. |

<a id="type-conicgradient"></a>

### `ConicGradient`

Namespace `Nazor.Style`. [Source](../../Sources/Core/Nazor.Style/Values/GradientValues.cs)

A clockwise angular gradient, with zero pointing up. Stop positions use percentages of one turn; missing positions follow gradient stop fix-up.

| Declaration | Contract |
| --- | --- |
| `public sealed record ConicGradient : Gradient, IEquatable<ConicGradient>` | A clockwise angular gradient, with zero pointing up. Stop positions use percentages of one turn; missing positions follow gradient stop fix-up. |
| `public ConicGradient(double Angle, Length CenterX, Length CenterY, ValueList<ColorStop> Stops)` | A clockwise angular gradient, with zero pointing up. Stop positions use percentages of one turn; missing positions follow gradient stop fix-up. |
| `public override string ToString()` | Formats the conic image using canonical percentage stops. A compilable NSS gradient. |
| `protected override bool PrintMembers(StringBuilder builder)` | Standard .NET value or component semantics. |
| `public static bool operator !=(ConicGradient? left, ConicGradient? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(ConicGradient? left, ConicGradient? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public override sealed bool Equals(Gradient? other)` | Standard .NET value or component semantics. |
| `public bool Equals(ConicGradient? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out double Angle, out Length CenterX, out Length CenterY, out ValueList<ColorStop> Stops)` | Standard .NET value or component semantics. |
| `protected override Type EqualityContract { get; }` | Standard .NET value or component semantics. |
| `public double Angle { get; init; }` | Rotation in degrees. |
| `public Length CenterX { get; init; }` | Horizontal center relative to the gradient box. |
| `public Length CenterY { get; init; }` | Vertical center relative to the gradient box. |

### `Gradient`

Namespace `Nazor.Style`.

| Declaration | Contract |
| --- | --- |
| `public bool Repeating { get; init; }` | Repeats the fixed-up first/last-stop interval; false by default. |

### `FilterKind`

Namespace `Nazor.Style`.

A supported image-filter function. Operations are applied in declared order.

| Declaration | Contract |
| --- | --- |
| `public enum FilterKind` | A supported image-filter function. Operations are applied in declared order. |
| `Blur = 0` | Gaussian blur with a logical-length standard deviation. |
| `Brightness = 1` | Scales RGB components. |
| `Contrast = 2` | Scales RGB distance from middle gray. |
| `Grayscale = 3` | Interpolates toward grayscale. |
| `HueRotate = 4` | Rotates hue by an angle in degrees. |
| `Invert = 5` | Interpolates toward inverted RGB components. |
| `Opacity = 6` | Scales alpha. |
| `Saturate = 7` | Scales color saturation. |
| `Sepia = 8` | Interpolates toward sepia. |

### `FilterFunction`

Namespace `Nazor.Style`.

One filter function. Blur uses Radius; other functions use Amount. HueRotate permits negative degrees; other amounts must be finite and nonnegative.

| Declaration | Contract |
| --- | --- |
| `public sealed record FilterFunction : IEquatable<FilterFunction>` | One filter function. Blur uses Radius; other functions use Amount. HueRotate permits negative degrees; other amounts must be finite and nonnegative. |
| `public FilterFunction(FilterKind Kind, double Amount = 1, Length Radius = default(Length))` | One filter function. Blur uses Radius; other functions use Amount. HueRotate permits negative degrees; other amounts must be finite and nonnegative. |
| `public override string ToString()` | Returns the canonical NSS function. A function expression. |
| `public static bool operator !=(FilterFunction? left, FilterFunction? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(FilterFunction? left, FilterFunction? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(FilterFunction? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out FilterKind Kind, out double Amount, out Length Radius)` | Standard .NET value or component semantics. |
| `public FilterKind Kind { get; init; }` | The operation. |
| `public double Amount { get; init; }` | A scalar, or degrees for HueRotate. |
| `public Length Radius { get; init; }` | Gaussian standard deviation, resolved from a nonnegative length. |

### `FilterValue`

Namespace `Nazor.Style`.

An ordered list shared by filter and backdrop-filter. Empty means none. NSS accepts at most64 operations, including repetitions.

| Declaration | Contract |
| --- | --- |
| `public sealed record FilterValue : IEquatable<FilterValue>` | An ordered list shared by filter and backdrop-filter. Empty means none. NSS accepts at most64 operations, including repetitions. |
| `public static readonly FilterValue None` | The initial unfiltered value. |
| `public FilterValue(ValueList<FilterFunction> Functions)` | An ordered list shared by filter and backdrop-filter. Empty means none. NSS accepts at most64 operations, including repetitions. |
| `public override string ToString()` | Formats the ordered chain or none. A canonical NSS value. |
| `public static bool operator !=(FilterValue? left, FilterValue? right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(FilterValue? left, FilterValue? right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object? obj)` | Standard .NET value or component semantics. |
| `public bool Equals(FilterValue? other)` | Standard .NET value or component semantics. |
| `public void Deconstruct(out ValueList<FilterFunction> Functions)` | Standard .NET value or component semantics. |
| `public ValueList<FilterFunction> Functions { get; init; }` | The ordered function values. |
| `public bool IsNone { get; }` | Whether there are no operations. |

### `BlendMode`

Namespace `Nazor.Style`.

Color blending of an isolated source group with its destination.

| Declaration | Contract |
| --- | --- |
| `public enum BlendMode` | Color blending of an isolated source group with its destination. |
| `Normal = 0` | Uses source-over without color blending. |
| `Multiply = 1` | Multiplies source and backdrop channels. |
| `Screen = 2` | Multiplies complementary channels. |
| `Overlay = 3` | Uses multiply or screen according to backdrop luminance. |
| `Darken = 4` | Chooses the darker channel. |
| `Lighten = 5` | Chooses the lighter channel. |
| `ColorDodge = 6` | Brightens the backdrop according to the source. |
| `ColorBurn = 7` | Darkens the backdrop according to the source. |
| `HardLight = 8` | Uses multiply or screen according to source luminance. |
| `SoftLight = 9` | Applies a soft contrast adjustment. |
| `Difference = 10` | Uses the absolute channel difference. |
| `Exclusion = 11` | Uses a low-contrast channel difference. |
| `Hue = 12` | Uses source hue with backdrop saturation and luminosity. |
| `Saturation = 13` | Uses source saturation with backdrop hue and luminosity. |
| `Color = 14` | Uses source hue and saturation with backdrop luminosity. |
| `Luminosity = 15` | Uses source luminosity with backdrop hue and saturation. |

### `Isolation`

Namespace `Nazor.Style`.

Whether descendants blend only against pixels inside the element group.

| Declaration | Contract |
| --- | --- |
| `public enum Isolation` | Whether descendants blend only against pixels inside the element group. |
| `Auto = 0` | No additional isolation beyond existing opacity/filter/blend groups. |
| `Isolate = 1` | Starts an isolated transparent group for the complete element subtree. |

### `FontFeatureSetting`

Namespace `Nazor.Style`.

An explicit, case-sensitive OpenType feature applied to a shaped text segment.

| Declaration | Contract |
| --- | --- |
| `public readonly struct FontFeatureSetting : IEquatable<FontFeatureSetting>` | An explicit, case-sensitive OpenType feature applied to a shaped text segment. |
| `public FontFeatureSetting()` | Creates a new instance. |
| `public FontFeatureSetting(string tag, uint value = 1)` | Initializes a font feature. |
| `public override string ToString()` | Formats a quoted tag and numeric selection. Canonical NSS syntax. |
| `public static bool operator !=(FontFeatureSetting left, FontFeatureSetting right)` | Standard .NET value or component semantics. |
| `public static bool operator ==(FontFeatureSetting left, FontFeatureSetting right)` | Standard .NET value or component semantics. |
| `public override int GetHashCode()` | Standard .NET value or component semantics. |
| `public override bool Equals(object obj)` | Standard .NET value or component semantics. |
| `public bool Equals(FontFeatureSetting other)` | Standard .NET value or component semantics. |
| `public string Tag { get; }` | The case-sensitive four-character tag. |
| `public uint Value { get; }` | The unsigned feature selection. |

### `FontSynthesis`

Namespace `Nazor.Style`.

Permitted synthetic font styles when a real face or variation axis is unavailable. None preserves unsynthesized rendering.

| Declaration | Contract |
| --- | --- |
| `public enum FontSynthesis` | Permitted synthetic font styles when a real face or variation axis is unavailable. None preserves unsynthesized rendering. |
| `None = 0` | Uses only real font faces and variation axes. |
| `Weight = 1` | Permits simulated bold for a requested weight of at least 600 when the selected face is lighter. |
| `Style = 2` | Permits a 12-degree synthetic oblique when italic is requested but unavailable. |
