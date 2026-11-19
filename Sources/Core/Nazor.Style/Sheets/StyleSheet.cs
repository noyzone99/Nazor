using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>The size of the window content in logical units, tested by media queries.</summary>
/// <param name="Width">The width of the value in its coordinate system.</param>
/// <param name="Height">The height of the value in its coordinate system.</param>
public readonly record struct Viewport(double Width, double Height);

/// <summary>Specifies an explicit light or dark color scheme, or follows the system preference.</summary>
public enum ColorScheme : byte
{
    /// <summary>Uses a light color scheme.</summary>
    Light,

    /// <summary>Uses a dark color scheme.</summary>
    Dark,

    /// <summary>Follows the platform color-scheme preference.</summary>
    System,
}

/// <summary>
/// The user preferences that media queries test: the color scheme (the app mode of Windows), reduced motion ("Show
/// animations" off) and forced colors (a contrast theme of Windows, which also means <c>prefers-contrast: more</c>);
/// and the system colors that NSS names (<c>Canvas</c>…), the defaults when null. The default is light, with
/// animations, without forced colors.
/// </summary>
/// <param name="ColorScheme">The color scheme (ColorScheme).</param>
/// <param name="ReducedMotion">The reduced motion (bool).</param>
/// <param name="ForcedColors">The forced colors (bool).</param>
/// <param name="Colors">The colors (SystemColors?).</param>
/// <param name="Platform">The platform (PlatformKind?).</param>
/// <param name="Density">The resolved control density. Compact is the default; Automatic also falls back to compact for headless callers. Window hosts resolve application and window overrides before publishing preferences.</param>
public readonly record struct MediaPreferences(ColorScheme ColorScheme, bool ReducedMotion, bool ForcedColors = false, SystemColors? Colors = null, PlatformKind? Platform = null,
    ControlDensity Density = ControlDensity.Compact);

/// <summary>
/// A conjunction of width/height bounds and preferences, e.g. <c>(min-width: 400px) and (prefers-color-scheme: dark)</c>;
/// a null part is not tested.
/// </summary>
/// <param name="MinWidth">The min width (double?).</param>
/// <param name="MaxWidth">The max width (double?).</param>
/// <param name="MinHeight">The min height (double?).</param>
/// <param name="MaxHeight">The max height (double?).</param>
/// <param name="PrefersColorScheme">The prefers color scheme (ColorScheme?).</param>
/// <param name="PrefersReducedMotion">The prefers reduced motion (bool?).</param>
/// <param name="ForcedColors">The forced colors (bool?).</param>
/// <param name="PrefersMoreContrast">The prefers more contrast (bool?).</param>
/// <param name="Platform">The platform (PlatformKind?).</param>
/// <param name="Density">The required compact or touch density, or null to ignore density.</param>
public sealed record MediaCondition(double? MinWidth, double? MaxWidth, double? MinHeight, double? MaxHeight,
    ColorScheme? PrefersColorScheme = null, bool? PrefersReducedMotion = null, bool? ForcedColors = null, bool? PrefersMoreContrast = null, PlatformKind? Platform = null,
    ControlDensity? Density = null)
{
    /// <summary>An unconditional media condition that matches every viewport.</summary>
    public static readonly MediaCondition Always = new(null, null, null, null);

    /// <summary>A condition that never matches, as two contradicting preferences give.</summary>
    public static readonly MediaCondition Never = new(1, 0, null, null);

    /// <summary>Tests whether the viewport and media preferences satisfy this condition.</summary>
    /// <param name="viewport">The viewport dimensions used to evaluate the media query.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(Viewport viewport) => Matches(viewport, default);

    /// <summary>Tests whether the viewport and media preferences satisfy this condition.</summary>
    /// <param name="viewport">The viewport dimensions used to evaluate the media query.</param>
    /// <param name="preferences">The system and application preferences used to evaluate the media query.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(Viewport viewport, MediaPreferences preferences) =>
        (MinWidth is not { } minWidth || viewport.Width >= minWidth) && (MaxWidth is not { } maxWidth || viewport.Width <= maxWidth) &&
        (MinHeight is not { } minHeight || viewport.Height >= minHeight) && (MaxHeight is not { } maxHeight || viewport.Height <= maxHeight) &&
        (PrefersColorScheme is not { } scheme || preferences.ColorScheme == scheme) &&
        (PrefersReducedMotion is not { } reduced || preferences.ReducedMotion == reduced) &&
        (ForcedColors is not { } forced || preferences.ForcedColors == forced) &&
        (PrefersMoreContrast is not { } more || preferences.ForcedColors == more) &&
        (Platform is not { } platform || preferences.Platform == platform) &&
        (Density is not { } density || density == (preferences.Density == ControlDensity.Touch ? ControlDensity.Touch : ControlDensity.Compact));

    /// <summary>Both conditions, as for an <c>@media</c> nested in another.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>The resulting MediaCondition value.</returns>
    public MediaCondition And(MediaCondition other)
    {
        if ((PrefersColorScheme is { } a && other.PrefersColorScheme is { } b && a != b) ||
            (PrefersReducedMotion is { } c && other.PrefersReducedMotion is { } d && c != d) ||
            (ForcedColors is { } e && other.ForcedColors is { } f && e != f) ||
            (PrefersMoreContrast is { } g && other.PrefersMoreContrast is { } h && g != h) ||
            (Platform is { } i && other.Platform is { } j && i != j) ||
            (Density is { } k && other.Density is { } l && k != l))
            return Never;
        return new(Max(MinWidth, other.MinWidth), Min(MaxWidth, other.MaxWidth), Max(MinHeight, other.MinHeight), Min(MaxHeight, other.MaxHeight),
            PrefersColorScheme ?? other.PrefersColorScheme, PrefersReducedMotion ?? other.PrefersReducedMotion,
            ForcedColors ?? other.ForcedColors, PrefersMoreContrast ?? other.PrefersMoreContrast, Platform ?? other.Platform, Density ?? other.Density);
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString()
    {
        var parts = new List<string>();
        if (MinWidth is { } a) parts.Add(Feature("min-width", a));
        if (MaxWidth is { } b) parts.Add(Feature("max-width", b));
        if (MinHeight is { } c) parts.Add(Feature("min-height", c));
        if (MaxHeight is { } d) parts.Add(Feature("max-height", d));
        if (PrefersColorScheme is { } scheme) parts.Add($"(prefers-color-scheme: {(scheme == ColorScheme.Dark ? "dark" : "light")})");
        if (PrefersReducedMotion is { } reduced) parts.Add($"(prefers-reduced-motion: {(reduced ? "reduce" : "no-preference")})");
        if (ForcedColors is { } forced) parts.Add($"(forced-colors: {(forced ? "active" : "none")})");
        if (PrefersMoreContrast is { } more) parts.Add($"(prefers-contrast: {(more ? "more" : "no-preference")})");
        if (Platform is { } platform) parts.Add($"(platform: {platform.ToString().ToLowerInvariant()})");
        if (Density is { } density) parts.Add($"(density: {density.ToString().ToLowerInvariant()})");
        return parts.Count == 0 ? "all" : string.Join(" and ", parts);
    }

    private static string Feature(string name, double value) => string.Create(CultureInfo.InvariantCulture, $"({name}: {value}px)");
    private static double? Max(double? a, double? b) => a is null ? b : b is null ? a : Math.Max(a.Value, b.Value);
    private static double? Min(double? a, double? b) => a is null ? b : b is null ? a : Math.Min(a.Value, b.Value);
}

/// <summary>A media query list: matches when any of its conditions matches.</summary>
/// <param name="conditions">The media conditions combined by the query.</param>
public sealed class MediaQuery(IReadOnlyList<MediaCondition> conditions)
{
    /// <summary>Gets the media conditions that make up this query.</summary>
    public IReadOnlyList<MediaCondition> Conditions { get; } = conditions;

    /// <summary>Tests whether the viewport and media preferences satisfy this condition.</summary>
    /// <param name="viewport">The viewport dimensions used to evaluate the media query.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(Viewport viewport) => Matches(viewport, default);

    /// <summary>Tests whether the viewport and media preferences satisfy this condition.</summary>
    /// <param name="viewport">The viewport dimensions used to evaluate the media query.</param>
    /// <param name="preferences">The system and application preferences used to evaluate the media query.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Matches(Viewport viewport, MediaPreferences preferences) => Conditions.Any(c => c.Matches(viewport, preferences));

    /// <summary>Combines the two media queries so that both must match.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>The resulting MediaQuery value.</returns>
    public MediaQuery And(MediaQuery other) => new(Conditions.SelectMany(a => other.Conditions.Select(a.And)).ToArray());

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => string.Join(", ", Conditions);
}

/// <summary>Specifies how a property obtains its value through the cascade and inheritance.</summary>
public enum GlobalKeyword : byte
{
    /// <summary>Uses the property's registered initial value.</summary>
    Initial,

    /// <summary>Uses the parent's computed value.</summary>
    Inherit,

    /// <summary>Inherits inherited properties and uses the initial value for other properties.</summary>
    Unset,
}

/// <summary>A declaration as compiled from NSS.</summary>
public abstract class StyleDeclaration
{
    private protected StyleDeclaration()
    {
    }
}

/// <summary>A longhand with a value already parsed and validated by the compiler.</summary>
/// <param name="property">The style property to read or update.</param>
/// <param name="value">The value to use.</param>
public sealed class ValueDeclaration(StyleProperty property, object value) : StyleDeclaration
{
    /// <summary>Gets the style property targeted by this value or declaration.</summary>
    public StyleProperty Property { get; } = property;

    /// <summary>Gets the parsed value assigned to the style property.</summary>
    public object Value { get; } = value;

    internal bool IsRelative { get; } = RelativeLengths.NeedsComputing(property, value);

    /// <summary>Returns a textual representation, formatting keyword enums, combined decoration/grid keywords, times and numbers with NSS syntax.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Property == StyleProperties.BackgroundClip && Value is bool clip
        ? $"{Property.Name}: {(clip ? "padding-box" : "border-box")}" : $"{Property.Name}: {FormatValue()}";

    private string FormatValue()
    {
        if (Value is TimeSpan time) return time.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture) + "ms";
        if (Value is double number) return number.ToString("R", CultureInfo.InvariantCulture);
        if (Value is ValueList<FontVariationSetting> { Items.Count: 0 }) return "normal";
        if (Value is ValueList<FontFeatureSetting> { Items.Count: 0 }) return "normal";
        if (Value is FontSynthesis synthesis) return synthesis == FontSynthesis.None ? "none" :
            string.Join(" ",new[] { synthesis.HasFlag(FontSynthesis.Weight) ? "weight" : null, synthesis.HasFlag(FontSynthesis.Style) ? "style" : null }.Where(value => value is not null));
        if (Value is TextDecorationLine lines && lines != TextDecorationLine.None)
            return string.Join(" ", new[]
            {
                lines.HasFlag(TextDecorationLine.Underline) ? "underline" : null,
                lines.HasFlag(TextDecorationLine.Overline) ? "overline" : null,
                lines.HasFlag(TextDecorationLine.LineThrough) ? "line-through" : null,
            }.Where(keyword => keyword is not null));
        if (Value is GridAutoFlow.ColumnDense) return "column dense";
        if (Value is Enum enumeration)
        {
            foreach (var keyword in Property.Keywords)
            {
                try
                {
                    if (Equals(Property.Parse(new TokenReader(StyleTokenizer.Tokenize(keyword))), enumeration)) return keyword;
                }
                catch (StyleValueException) { }
            }
        }
        return Value.ToString() ?? "";
    }
}

/// <summary><c>initial</c>, <c>inherit</c> or <c>unset</c> on a longhand.</summary>
/// <param name="property">The style property to read or update.</param>
/// <param name="keyword">The keyword (GlobalKeyword).</param>
public sealed class KeywordDeclaration(StyleProperty property, GlobalKeyword keyword) : StyleDeclaration
{
    /// <summary>Gets the style property targeted by this value or declaration.</summary>
    public StyleProperty Property { get; } = property;

    /// <summary>Gets the global cascade keyword assigned to this property.</summary>
    public GlobalKeyword Keyword { get; } = keyword;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Property.Name}: {Keyword.ToString().ToLowerInvariant()}";
}

/// <summary>
/// A longhand or shorthand whose value contains <c>var()</c>: parsed for each element once the custom properties are
/// known. An invalid result behaves as <c>unset</c>.
/// </summary>
/// <param name="propertyName">The canonical NSS property name.</param>
/// <param name="tokens">The NSS lexical token sequence.</param>
public sealed class PendingDeclaration(string propertyName, IReadOnlyList<StyleToken> tokens) : StyleDeclaration
{
    /// <summary>Gets the NSS property name to resolve after custom-property substitution.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>Gets the NSS token sequence represented by this declaration or reader.</summary>
    public IReadOnlyList<StyleToken> Tokens { get; } = tokens;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{PropertyName}: {StyleToken.Serialize(Tokens)}";
}

/// <summary>A custom property such as <c>--surface</c>: inherited token list, possibly referencing others.</summary>
/// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="tokens">The NSS lexical token sequence.</param>
public sealed class CustomPropertyDeclaration(string name, IReadOnlyList<StyleToken> tokens) : StyleDeclaration
{
    /// <summary>Gets the case-sensitive custom-property name, including its leading --.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the NSS token sequence represented by this declaration or reader.</summary>
    public IReadOnlyList<StyleToken> Tokens { get; } = tokens;

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Name}: {StyleToken.Serialize(Tokens)}";
}

/// <summary>
/// A style rule after nesting expansion: one selector, an optional media query, declarations and source order.
/// </summary>
/// <param name="selector">The selector (Selector).</param>
/// <param name="media">The media (MediaQuery?).</param>
/// <param name="declarations">The declarations assigned by the rule or keyframe.</param>
/// <param name="order">The order (int).</param>
public sealed class StyleRule(Selector selector, MediaQuery? media, IReadOnlyList<StyleDeclaration> declarations, int order)
{
    /// <summary>Gets the selector that determines which elements the rule matches.</summary>
    public Selector Selector { get; } = selector;

    /// <summary>Gets the media query that restricts this rule, or null when it is unconditional.</summary>
    public MediaQuery? Media { get; } = media;

    /// <summary>Gets the style declarations applied by this rule or keyframe.</summary>
    public IReadOnlyList<StyleDeclaration> Declarations { get; } = declarations;

    /// <summary>Position in the stylesheet; later rules win among equal specificity.</summary>
    public int Order { get; } = order;
}

/// <summary>
/// A compiled stylesheet: rules in source order and <c>@keyframes</c> rules. Built by generated code, never by
/// parsing NSS at runtime. Rules and keyframes are one immutable snapshot that development tools may swap with
/// <see cref="Replace(StyleSheet)" /> while the sheet keeps its identity; readers take <see cref="Rules" /> once.
/// </summary>
public sealed class StyleSheet : IStyleSheet
{
    /// <summary>A sheet without rules; never replaced and never tracked by <see cref="StyleRegistry" />.</summary>
    public static readonly StyleSheet Empty = new("", [], [], track: false);

    private Content content;
    private Composition? composition;
    private int version;
    private WindowDecorationPresenters? decorationPresenters;

    /// <summary>Decoration presenters associated with this sheet, or inherited from the last composed sheet that supplies them.</summary>
    public WindowDecorationPresenters? WindowDecorations => decorationPresenters ?? composition?.WindowDecorations;

    /// <summary>Associates statically registered decoration presenters with a new sheet while preserving this sheet and its live rules.</summary>
    /// <param name="presenters">The theme's optional title-bar and caption-button composition.</param>
    /// <returns>A live composition with the supplied presenter metadata.</returns>
    public StyleSheet WithWindowDecorations(WindowDecorationPresenters presenters)
    {
        ArgumentNullException.ThrowIfNull(presenters);
        var result = Combine(this, new StyleSheet("window-decoration-presenters", [], [], track: false));
        result.decorationPresenters = presenters;
        return result;
    }

    /// <summary>
    /// Combines compiled stylesheets into one sheet. Rules keep the supplied sheet order, and later sheets win among
    /// equal-specificity rules. Replacing an input sheet updates the combined sheet in place.
    /// </summary>
    /// <param name="sheets">The sheets to combine, from lowest to highest source order.</param>
    /// <returns>The combined stylesheet, or the sole input when one sheet is supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sheets"/> or an entry is null.</exception>
    public static StyleSheet Combine(params StyleSheet[] sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);
        foreach (var sheet in sheets) ArgumentNullException.ThrowIfNull(sheet);
        if (sheets.Length == 0) return Empty;
        if (sheets.Length == 1) return sheets[0];

        var combined = new StyleSheet("combined", [], [], track: false)
        {
            composition = new Composition([.. sheets]),
        };
        StyleRegistry.TrackCombined(combined);
        combined.composition.Refresh(combined);
        return combined;
    }

    /// <summary>Initializes a new instance of StyleSheet.</summary>
    /// <param name="source">The stylesheet source identity or input content.</param>
    /// <param name="rules">The style rules in cascade source order.</param>
    public StyleSheet(string source, IReadOnlyList<StyleRule> rules)
        : this(source, rules, [], track: true)
    {
    }

    /// <summary>Initializes a new instance of StyleSheet.</summary>
    /// <param name="source">The stylesheet source identity or input content.</param>
    /// <param name="rules">The style rules in cascade source order.</param>
    /// <param name="keyframes">The keyframe animation definitions supplied by the stylesheet.</param>
    public StyleSheet(string source, IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes)
        : this(source, rules, keyframes, track: true)
    {
    }

    internal StyleSheet(string source, IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes, bool track)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(keyframes);
        Source = source;
        content = new Content([.. rules], [.. keyframes]);
        if (track) StyleRegistry.Track(this);
    }

    private sealed record Content(StyleRule[] Rules, KeyframesRule[] Keyframes)
    {
        private RuleIndex? index;

        public RuleIndex Index => index ??= new RuleIndex(Rules);

        public bool HasMediaQueries { get; } = Rules.Any(r => r.Media is not null);

        public bool HasStructuralSelectors { get; } = Rules.Any(r => r.Selector.IsStructural);
        public bool HasSiblingDependencies { get; } = Rules.Any(r => r.Selector.HasSiblingDependencies);
        public bool ObservesText { get; } = Rules.Any(r => r.Selector.ObservesText);

        public IReadOnlyList<StateDependency> StateDependencies { get; } = StateDependency.Of(Rules);
        public IReadOnlyList<HasDependency> HasDependencies { get; } = HasDependency.Of(Rules);
        public HashSet<string>? NonlocalAttributes { get; } = CollectNonlocalAttributes(Rules);
    }

    internal RuleIndex Index => Volatile.Read(ref content).Index;
    internal bool HasSiblingDependencies => Volatile.Read(ref content).HasSiblingDependencies;
    internal bool ObservesText => Volatile.Read(ref content).ObservesText;

    internal IReadOnlyList<StateDependency> StateDependencies => Volatile.Read(ref content).StateDependencies;
    internal IReadOnlyList<HasDependency> HasDependencies => Volatile.Read(ref content).HasDependencies;
    // A custom pseudo-selector can inspect attributes that are not declared in its selector.
    internal bool AttributeAffectsOtherElements(string name) => Volatile.Read(ref content).NonlocalAttributes?.Contains(name) ?? true;

    private static HashSet<string>? CollectNonlocalAttributes(StyleRule[] rules)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules)
            if (!Collect(rule.Selector, subject: true)) return null;
        return names;

        bool Collect(Selector selector, bool subject)
        {
            for (var i = 0; i < selector.Compounds.Count; i++)
            {
                var compound = selector.Compounds[i];
                var local = subject && i == selector.Compounds.Count - 1;
                if (!local)
                    foreach (var attribute in compound.Attributes) names.Add(attribute.Name);
                foreach (var pseudo in compound.Pseudos)
                    if (pseudo is LogicalSelector logical)
                    {
                        foreach (var inner in logical.Selectors.Items)
                            if (!Collect(inner, local)) return false;
                    }
                    else if (pseudo is NthSelector { Filter: { } filter })
                    {
                        foreach (var inner in filter.Items)
                            if (!Collect(inner, subject: false)) return false;
                    }
                    else if (pseudo is NamedStateSelector)
                    {
                        if (!local) names.Add("custom-states");
                    }
                    else if (pseudo is PartSelector)
                    {
                        if (!local) names.Add("style-parts");
                    }
                    else if (pseudo is ItemPositionSelector)
                    {
                        if (!local) names.Add("item-position");
                    }
                    else if (pseudo is not (NthSelector or OnlyChildSelector or EmptySelector or RootSelector or ComponentRootSelector)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Origin of the rules: the path relative to the project with '/' separators (for example "Styles/Dark.nss" or
    /// "Components/DeviceCard.nazor"), or the file name alone when the project directory is unknown. Development
    /// tools find live sheets by it.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// The current rule snapshot. Read it once into a local: <see cref="Replace(StyleSheet)" /> swaps it as a whole.
    /// </summary>
    public IReadOnlyList<StyleRule> Rules => Volatile.Read(ref content).Rules;

    /// <summary>The <c>@keyframes</c> rules, in source order.</summary>
    public IReadOnlyList<KeyframesRule> Keyframes => Volatile.Read(ref content).Keyframes;

    /// <summary>
    /// The last <c>@keyframes</c> rule with this name (names are case-sensitive, as in CSS), or null.
    /// </summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting KeyframesRule? value.</returns>
    public KeyframesRule? FindKeyframes(string name)
    {
        var keyframes = Keyframes;
        for (var i = keyframes.Count - 1; i >= 0; i--)
            if (keyframes[i].Name == name) return keyframes[i];
        return null;
    }

    /// <summary>
    /// 0 at creation, incremented by every <see cref="Replace(StyleSheet)" /> that changed the rules.
    /// </summary>
    public int Version => Volatile.Read(ref version);

    /// <summary>Gets whether any stylesheet rule is guarded by a media query.</summary>
    public bool HasMediaQueries => Volatile.Read(ref content).HasMediaQueries;

    /// <summary>
    /// Some rule depends on siblings or children (<c>:first-child</c>, <c>+</c>, <c>:empty</c>…): child list changes
    /// restyle.
    /// </summary>
    public bool HasStructuralSelectors => Volatile.Read(ref content).HasStructuralSelectors;

    /// <summary>
    /// Replaces the rules and keyframes and notifies consumers when the replacement changes the sheet.
    /// </summary>
    /// <returns>False, doing nothing, when <see cref="Describe()" /> of the new content equals the current one.</returns>
    /// <param name="sheet">The stylesheet supplied to the operation.</param>
    /// <exception cref="ArgumentNullException">sheet is null.</exception>
    public bool Replace(StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var other = Volatile.Read(ref sheet.content);
        return Replace(other.Rules, other.Keyframes);
    }

    /// <summary>
    /// Replaces the style rules while retaining the current keyframes, notifying consumers when the content changes.
    /// </summary>
    /// <param name="rules">The style rules in cascade source order.</param>
    /// <returns>true when the sheet was replaced by different content; otherwise, false.</returns>
    public bool Replace(IReadOnlyList<StyleRule> rules) => Replace(rules, Keyframes);

    /// <summary>
    /// Replaces the rules and keyframes and notifies consumers when the replacement changes the sheet.
    /// </summary>
    /// <param name="rules">The style rules in cascade source order.</param>
    /// <param name="keyframes">The keyframe animation definitions supplied by the stylesheet.</param>
    /// <returns>true when the sheet was replaced by different content; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">rules, keyframes is null.</exception>
    public bool Replace(IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes)
        => Replace(rules, keyframes, notify: true);

    private bool Replace(IReadOnlyList<StyleRule> rules, IReadOnlyList<KeyframesRule> keyframes, bool notify)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(keyframes);
        var snapshot = new Content([.. rules], [.. keyframes]);
        if (Describe(snapshot) == Describe(Volatile.Read(ref content))) return false;
        Volatile.Write(ref content, snapshot);
        Interlocked.Increment(ref version);
        if (notify) StyleRegistry.NotifyChanged();
        return true;
    }

    internal void RefreshComposition() => composition?.Refresh(this);

    private sealed class Composition(StyleSheet[] sources)
    {
        public WindowDecorationPresenters? WindowDecorations => sources.Reverse().Select(sheet => sheet.WindowDecorations).FirstOrDefault(value => value is not null);
        private readonly object gate = new();
        private int[]? versions;

        public void Refresh(StyleSheet target)
        {
            lock (gate)
            {
                while (true)
                {
                    var before = sources.Select(sheet => sheet.Version).ToArray();
                    if (versions is not null && before.SequenceEqual(versions)) return;

                    var rules = new List<StyleRule>();
                    foreach (var source in sources)
                        foreach (var rule in source.Rules.OrderBy(rule => rule.Order))
                            rules.Add(new(rule.Selector, rule.Media, rule.Declarations, rules.Count));
                    var keyframes = sources.SelectMany(sheet => sheet.Keyframes).ToArray();
                    var after = sources.Select(sheet => sheet.Version).ToArray();
                    if (!before.SequenceEqual(after)) continue;

                    versions = after;
                    target.Replace(rules, keyframes, notify: false);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Canonical text of the compiled sheet: one line per rule with media, selector, specificity and declarations,
    /// then one line per <c>@keyframes</c> rule.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    public string Describe() => Describe(Volatile.Read(ref content));

    private static string Describe(Content content)
    {
        var builder = new StringBuilder();
        foreach (var rule in content.Rules)
        {
            if (rule.Media is not null) builder.Append("@media ").Append(rule.Media).Append(" | ");
            builder.Append(rule.Selector).Append(" (").Append(rule.Selector.Specificity).Append(") { ");
            builder.Append(string.Join("; ", rule.Declarations)).Append(" }\n");
        }
        foreach (var keyframes in content.Keyframes) builder.Append(keyframes).Append('\n');
        return builder.ToString();
    }
}

/// <summary>
/// Scoped stylesheets of components, registered by generated code when the assembly loads and created on first use.
/// The key is the component type that declared the elements. Development tools replace registered sheets
/// in place; <see cref="Version" /> tells engines that something changed, and optional tracking finds live sheets by
/// <see cref="StyleSheet.Source" />.
/// </summary>
public static class StyleRegistry
{
    private static readonly ConcurrentDictionary<Type, Entry> Scoped = new();
    private static readonly List<WeakReference<StyleSheet>> Tracked = [];
    private static readonly List<WeakReference<StyleSheet>> Combined = [];
    private static readonly Dictionary<string, (StyleRule[] Rules, KeyframesRule[]? Keyframes)> Pending = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock Gate = new();
    private static volatile bool trackingEnabled;
    private static int version;
    private static int pruneAt = 64;

    /// <summary>
    /// Registers, or re-registers, the factory of the scoped sheet of a component type. When the sheet of an existing
    /// entry was already created it is kept and its rules are replaced in place, so elements styled by it keep their
    /// reference; otherwise the new factory runs on first use. Calls <see cref="NotifyChanged" /> when an entry
    /// existed: the first registration of a type (every module initializer) cannot affect a computed style and costs
    /// engines nothing.
    /// </summary>
    /// <param name="component">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="factory">The factory used to create the component's scoped stylesheet on first use.</param>
    /// <exception cref="ArgumentNullException">component, factory is null.</exception>
    public static void RegisterScoped(Type component, Func<StyleSheet> factory)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(factory);
        StyleSheet? created = null;
        bool existed;
        lock (Gate)
        {
            existed = Scoped.TryGetValue(component, out var entry);
            if (existed && entry!.Sheet.IsValueCreated)
            {
                created = entry.Sheet.Value;
                entry.Factory = factory;
            }
            else Scoped[component] = new Entry(factory);
        }
        if (created is not null) ReplaceInPlace(created, factory);
        if (existed) NotifyChanged();
    }

    /// <summary>The scoped sheet of a component type, created on first use; null when it has none.</summary>
    /// <param name="component">The component type whose scoped styles or render output are being requested.</param>
    /// <returns>The resulting StyleSheet? value.</returns>
    /// <exception cref="ArgumentNullException">component is null.</exception>
    public static StyleSheet? GetScoped(Type component)
    {
        ArgumentNullException.ThrowIfNull(component);
        component = Normalize(component);
        if (Scoped.TryGetValue(component, out var entry)) return entry.Sheet.Value;

        RunModuleConstructor(component);
        return Scoped.TryGetValue(component, out entry) ? entry.Sheet.Value : null;
    }

    /// <summary>
    /// Development-time: makes <paramref name="sheet" /> the scoped sheet of a component type. An already created
    /// sheet keeps its identity and takes the rules of <paramref name="sheet" />; one not created yet becomes
    /// <paramref name="sheet" /> itself. The registered factory is kept, so a later <see cref="Refresh" /> (a hot
    /// reload delta) still runs the generated code rather than returning the pushed rules again; a type without a
    /// factory gets one returning the sheet. Calls <see cref="NotifyChanged" />.
    /// </summary>
    /// <param name="component">The component type whose scoped styles or render output are being requested.</param>
    /// <param name="sheet">The stylesheet supplied to the operation.</param>
    /// <exception cref="ArgumentNullException">component, sheet is null.</exception>
    public static void ReplaceScoped(Type component, StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(sheet);
        component = Normalize(component);
        RunModuleConstructor(component);
        StyleSheet? created = null;
        lock (Gate)
        {
            if (!Scoped.TryGetValue(component, out var entry)) Scoped[component] = new Entry(() => sheet);
            else if (entry.Sheet.IsValueCreated) created = entry.Sheet.Value;
            else entry.Sheet = new Lazy<StyleSheet>(() => sheet, LazyThreadSafetyMode.ExecutionAndPublication);
        }
        if (created is not null && !ReferenceEquals(created, sheet))
        {
            Untrack(sheet);
            created.Replace(sheet);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Re-invokes the registered factory of each type (its body may have been patched by a hot reload delta) and
    /// replaces the created sheet in place; a sheet not created yet is created later by the current factory. Unknown
    /// types are ignored; null means every registered type. A failing factory does not stop the others: the first
    /// failure is thrown at the end.
    /// </summary>
    /// <param name="components">The components (Type[]?).</param>
    public static void Refresh(Type[]? components)
    {
        var types = components is null ? Scoped.Keys.ToArray() : components.Where(t => t is not null).Select(Normalize).Distinct().ToArray();
        List<Exception>? errors = null;
        foreach (var type in types)
        {
            StyleSheet? created = null;
            Func<StyleSheet> factory;
            lock (Gate)
            {
                if (!Scoped.TryGetValue(type, out var entry)) continue;
                factory = entry.Factory;
                if (entry.Sheet.IsValueCreated) created = entry.Sheet.Value;
                else entry.Sheet = new Lazy<StyleSheet>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
            }
            if (created is null) continue;
            try
            {
                ReplaceInPlace(created, factory);
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }
        if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(errors[0]);
        if (errors is not null) throw new AggregateException(errors);
    }

    /// <summary>The component types with a registered scoped sheet, created or not.</summary>
    public static IReadOnlyList<Type> ScopedTypes => Scoped.Keys.ToArray();

    /// <summary>
    /// Incremented by <see cref="NotifyChanged" />; a <see cref="StyleEngine" /> compares it on every update.
    /// </summary>
    public static int Version => Volatile.Read(ref version);

    /// <summary>
    /// Tells engines that a registered or replaced sheet changed; they recompute at their next update.
    /// </summary>
    public static void NotifyChanged()
    {
        StyleSheet[] combined;
        lock (Gate)
        {
            Combined.RemoveAll(reference => !reference.TryGetTarget(out _));
            combined = [.. Combined.Select(reference => reference.TryGetTarget(out var sheet) ? sheet : null).OfType<StyleSheet>()];
        }
        foreach (var sheet in combined) sheet.RefreshComposition();
        Interlocked.Increment(ref version);
    }

    internal static void TrackCombined(StyleSheet sheet)
    {
        lock (Gate) Combined.Add(new WeakReference<StyleSheet>(sheet));
    }

    /// <summary>
    /// Weak tracking of live sheets by <see cref="StyleSheet.Source" />, for development tools. Off by default:
    /// release applications and tools pay nothing. Sheets created while it was off are not found.
    /// </summary>
    public static bool TrackingEnabled
    {
        get => trackingEnabled;
        set => trackingEnabled = value;
    }

    /// <summary>
    /// The live sheets with this source (case-insensitive, '\' as '/'); empty when tracking is off.
    /// </summary>
    /// <param name="source">The stylesheet source identity or input content.</param>
    /// <returns>The resulting IReadOnlyList&lt;StyleSheet&gt; value.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static IReadOnlyList<StyleSheet> Find(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!trackingEnabled) return [];
        var key = NormalizeSource(source);
        lock (Gate)
        {
            var result = new List<StyleSheet>();
            Tracked.RemoveAll(reference => !reference.TryGetTarget(out _));
            foreach (var reference in Tracked)
                if (reference.TryGetTarget(out var sheet) && string.Equals(NormalizeSource(sheet.Source), key, StringComparison.OrdinalIgnoreCase)) result.Add(sheet);
            return result;
        }
    }

    /// <summary>
    /// Rules applied to a sheet with this source when it is created later (lazily created <c>StyleSheets.X</c>
    /// properties), while tracking is enabled. The latest call wins.
    /// </summary>
    /// <param name="source">The stylesheet source identity or input content.</param>
    /// <param name="rules">The style rules in cascade source order.</param>
    /// <exception cref="ArgumentNullException">source, rules is null.</exception>
    public static void SetPending(string source, IReadOnlyList<StyleRule> rules)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rules);
        lock (Gate) Pending[NormalizeSource(source)] = ([.. rules], null);
    }

    /// <summary>
    /// As <see cref="SetPending(string, IReadOnlyList{StyleRule})" />, with the keyframes of the sheet too.
    /// </summary>
    /// <param name="source">The stylesheet source identity or input content.</param>
    /// <param name="sheet">The stylesheet supplied to the operation.</param>
    /// <exception cref="ArgumentNullException">source, sheet is null.</exception>
    public static void SetPending(string source, StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sheet);
        lock (Gate) Pending[NormalizeSource(source)] = ([.. sheet.Rules], [.. sheet.Keyframes]);
    }

    internal static void Track(StyleSheet sheet)
    {
        if (!trackingEnabled) return;
        (StyleRule[] Rules, KeyframesRule[]? Keyframes)? pending;
        lock (Gate)
        {
            if (Tracked.Count >= pruneAt)
            {
                Tracked.RemoveAll(reference => !reference.TryGetTarget(out _));
                pruneAt = Math.Max(64, Tracked.Count * 2);
            }
            Tracked.Add(new WeakReference<StyleSheet>(sheet));
            pending = Pending.TryGetValue(NormalizeSource(sheet.Source), out var entry) ? entry : null;
        }
        if (pending is { } found) sheet.Replace(found.Rules, found.Keyframes ?? sheet.Keyframes);
    }

    private static void Untrack(StyleSheet sheet)
    {
        if (!trackingEnabled) return;
        lock (Gate) Tracked.RemoveAll(reference => !reference.TryGetTarget(out var target) || ReferenceEquals(target, sheet));
    }

    private static void ReplaceInPlace(StyleSheet created, Func<StyleSheet> factory)
    {
        var sheet = factory() ?? throw new InvalidOperationException("A scoped stylesheet factory returned null.");
        if (ReferenceEquals(sheet, created)) return;
        Untrack(sheet);
        created.Replace(sheet);
    }

    private static Type Normalize(Type component) =>
        component.IsGenericType && !component.IsGenericTypeDefinition ? component.GetGenericTypeDefinition() : component;

    private static string NormalizeSource(string source) => source.Replace('\\', '/');

    private static void RunModuleConstructor(Type component) =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(component.Module.ModuleHandle);

    private sealed class Entry(Func<StyleSheet> factory)
    {
        public volatile Func<StyleSheet> Factory = factory;
        public volatile Lazy<StyleSheet> Sheet = new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
