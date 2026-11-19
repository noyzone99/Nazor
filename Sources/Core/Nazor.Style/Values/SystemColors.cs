using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>
/// The system colors of CSS (<c>Canvas</c>, <c>CanvasText</c>, <c>ButtonFace</c>…): the colors the user chose for the
/// system, including contrast themes. NSS accepts CSS names wherever a color goes, including custom properties.
/// Resolve completes backend colors for the desktop and appearance. Accent shades and additional semantic roles
/// are root custom properties, for example <c>var(--nz-system-accent-dark-1)</c> and <c>var(--nz-system-tooltip-text)</c>.
/// System keywords alias ordinary inherited custom properties: overriding <c>--nz-system-canvas</c> also changes
/// <c>Canvas</c>. Prefer theme tokens such as <c>--nz-surface</c> for application customization.
/// </summary>
public sealed partial record SystemColors
{
    private const string Prefix = "--nz-system-";

    /// <summary>The names of CSS Color 4, in the case CSS writes them.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "AccentColor", "AccentColorText", "ActiveText", "ButtonBorder", "ButtonFace", "ButtonText", "Canvas", "CanvasText", "Field",
        "FieldText", "GrayText", "Highlight", "HighlightText", "LinkText", "Mark", "MarkText", "SelectedItem", "SelectedItemText", "VisitedText",
    ];

    /// <summary>The shades of the accent color, lighter and darker, as Windows names them.</summary>
    public static readonly IReadOnlyList<string> AccentShades = ["AccentLight1", "AccentLight2", "AccentLight3", "AccentDark1", "AccentDark2", "AccentDark3"];

    private static readonly Dictionary<string, string> Variables = Names.ToDictionary(n => n, n => Prefix + n.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> AllVariables = new(Variables.Concat(AccentShades.Select(s =>
        KeyValuePair.Create(s, $"{Prefix}accent-{s[6..^1].ToLowerInvariant()}-{s[^1]}"))), StringComparer.OrdinalIgnoreCase);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SystemColors, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>>> Properties = new();

    /// <summary>The classic light colors of Windows.</summary>
    public static SystemColors Default { get; } = new();

    /// <summary>Additional semantic colors, such as Tooltip, TooltipText, Header and HeaderText. Resolve snapshots backend values; use NSS variables from Property to access these roles.</summary>
    public IReadOnlyDictionary<string, Color> Palette { get; private init; } = System.Collections.Frozen.FrozenDictionary<string, Color>.Empty;

    /// <summary>Gets or sets the system color for the application surface background.</summary>
    public Color Canvas { get; init; } = Color.White;

    /// <summary>Gets or sets the system color for text drawn on the application surface.</summary>
    public Color CanvasText { get; init; } = Color.Black;

    /// <summary>Gets or sets the system color for the input-field background.</summary>
    public Color Field { get; init; } = Color.White;

    /// <summary>Gets or sets the system color for text drawn in an input field.</summary>
    public Color FieldText { get; init; } = Color.Black;

    /// <summary>Gets or sets the system color for a button surface.</summary>
    public Color ButtonFace { get; init; } = Color.FromRgb(0xf0, 0xf0, 0xf0);

    /// <summary>Gets or sets the system color for text drawn on a button.</summary>
    public Color ButtonText { get; init; } = Color.Black;

    /// <summary>Gets or sets the system color for a button border.</summary>
    public Color ButtonBorder { get; init; } = Color.Black;

    /// <summary>Gets or sets the system color for the highlighted-selection background.</summary>
    public Color Highlight { get; init; } = Color.FromRgb(0x00, 0x78, 0xd7);

    /// <summary>Gets or sets the system color for text drawn in a highlighted selection.</summary>
    public Color HighlightText { get; init; } = Color.White;

    /// <summary>Gets or sets the system color for the selected-item background.</summary>
    public Color SelectedItem { get; init; } = Color.FromRgb(0x00, 0x78, 0xd7);

    /// <summary>Gets or sets the system color for text drawn on a selected item.</summary>
    public Color SelectedItemText { get; init; } = Color.White;

    /// <summary>Gets or sets the system color for disabled text.</summary>
    public Color GrayText { get; init; } = Color.FromRgb(0x6d, 0x6d, 0x6d);

    /// <summary>Gets or sets the system color for an unvisited link.</summary>
    public Color LinkText { get; init; } = Color.FromRgb(0x00, 0x66, 0xcc);

    /// <summary>Gets or sets the system color for a visited link.</summary>
    public Color VisitedText { get; init; } = Color.FromRgb(0x00, 0x66, 0xcc);

    /// <summary>Gets or sets the system color for an active link.</summary>
    public Color ActiveText { get; init; } = Color.FromRgb(0x00, 0x66, 0xcc);

    /// <summary>Gets or sets the system color for the marked-text background.</summary>
    public Color Mark { get; init; } = Color.FromRgb(0xff, 0xff, 0x00);

    /// <summary>Gets or sets the system color for marked text.</summary>
    public Color MarkText { get; init; } = Color.Black;

    /// <summary>Gets or sets the system color for accented controls.</summary>
    public Color AccentColor { get; init; } = Color.FromRgb(0x00, 0x78, 0xd4);

    /// <summary>Gets or sets the system color for text drawn on an accent surface.</summary>
    public Color AccentColorText { get; init; } = Color.White;

    /// <summary>Gets or sets accent shade 1 on the lighter side of the system accent color.</summary>
    public Color AccentLight1 { get; init; } = Color.FromRgb(0x00, 0x93, 0xf9);

    /// <summary>Gets or sets accent shade 2 on the lighter side of the system accent color.</summary>
    public Color AccentLight2 { get; init; } = Color.FromRgb(0x60, 0xcd, 0xff);

    /// <summary>Gets or sets accent shade 3 on the lighter side of the system accent color.</summary>
    public Color AccentLight3 { get; init; } = Color.FromRgb(0x99, 0xeb, 0xff);

    /// <summary>Gets or sets accent shade 1 on the darker side of the system accent color.</summary>
    public Color AccentDark1 { get; init; } = Color.FromRgb(0x00, 0x5f, 0xb8);

    /// <summary>Gets or sets accent shade 2 on the darker side of the system accent color.</summary>
    public Color AccentDark2 { get; init; } = Color.FromRgb(0x00, 0x3e, 0x92);

    /// <summary>Gets or sets accent shade 3 on the darker side of the system accent color.</summary>
    public Color AccentDark3 { get; init; } = Color.FromRgb(0x00, 0x1a, 0x68);

    /// <summary>
    /// The value of a system color or of a shade of the accent by its name (any case); null for another name.
    /// </summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    public Color? this[string name] => AllVariables.TryGetValue(name, out var variable) ? Get(variable[Prefix.Length..])
        : Palette.TryGetValue(name, out var color) ? color : null;

    /// <summary>
    /// White or black, the one readable on a background: white when it reaches the contrast of 4.5 that text needs
    /// (as Windows writes on its accents), otherwise the one with more contrast.
    /// </summary>
    /// <param name="background">The background color used to clear the surface.</param>
    /// <returns>The resulting Color value.</returns>
    public static Color TextOn(Color background)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        var luminance = 0.2126 * Linear(background.R) + 0.7152 * Linear(background.G) + 0.0722 * Linear(background.B);
        var (onWhite, onBlack) = (1.05 / (luminance + 0.05), (luminance + 0.05) / 0.05);
        return onWhite >= 4.5 || onWhite >= onBlack ? Color.White : Color.Black;
    }

    private Color Get(string lower) => lower switch
    {
        "accent-light-1" => AccentLight1,
        "accent-light-2" => AccentLight2,
        "accent-light-3" => AccentLight3,
        "accent-dark-1" => AccentDark1,
        "accent-dark-2" => AccentDark2,
        "accent-dark-3" => AccentDark3,
        "accentcolor" => AccentColor,
        "accentcolortext" => AccentColorText,
        "activetext" => ActiveText,
        "buttonborder" => ButtonBorder,
        "buttonface" => ButtonFace,
        "buttontext" => ButtonText,
        "canvas" => Canvas,
        "canvastext" => CanvasText,
        "field" => Field,
        "fieldtext" => FieldText,
        "graytext" => GrayText,
        "highlight" => Highlight,
        "highlighttext" => HighlightText,
        "linktext" => LinkText,
        "mark" => Mark,
        "marktext" => MarkText,
        "selecteditem" => SelectedItem,
        "selecteditemtext" => SelectedItemText,
        _ => VisitedText,
    };

    private static readonly HashSet<string> NotColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "font", "font-family", "animation", "animation-name", "transition", "transition-property",
    };

    /// <summary>Whether the values of a property may name system colors.</summary>
    /// <param name="propertyName">The canonical NSS property name.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool MayName(string propertyName) => !NotColors.Contains(propertyName);

    /// <summary>The custom property that carries a system color, or null when the name is not one.</summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting string? value.</returns>
    public static string? Variable(string name) => Variables.GetValueOrDefault(name);

    /// <summary>
    /// The custom property for a CSS system color, accent shade or ASCII alphanumeric semantic role. Invalid names return null.
    /// Additional PascalCase roles use kebab-case, for example TooltipText becomes --nz-system-tooltip-text.
    /// </summary>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting string? value.</returns>
    public static string? Property(string name)
    {
        if (AllVariables.TryGetValue(name, out var variable)) return variable;
        if (name.Length == 0 || !char.IsAsciiLetter(name[0]) || name.Any(c => !char.IsAsciiLetterOrDigit(c))) return null;
        var result = new System.Text.StringBuilder(Prefix);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) ||
                i + 1 < name.Length && char.IsLower(name[i + 1]))) result.Append('-');
            result.Append(char.ToLowerInvariant(name[i]));
        }
        return result.ToString();
    }

    internal IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> CustomProperties => Properties.GetValue(this, static colors =>
    {
        var result = AllVariables.Values.ToDictionary(v => v, v => (IReadOnlyList<StyleToken>)Tokens(colors.Get(v[Prefix.Length..])), StringComparer.Ordinal);
        foreach (var (name, color) in CompletePalette(colors, colors.Palette))
            if (Property(name) is { } variable && !result.ContainsKey(variable)) result[variable] = Tokens(color);
        return result;
    });

    private static List<StyleToken> Tokens(Color color) =>
        [.. StyleTokenizer.Tokenize(color.ToString()).Where(t => t.Kind != StyleTokenKind.EndOfFile)];

    /// <summary>
    /// A value with its system color names replaced by the custom properties that carry them (<c>Canvas</c> becomes
    /// <c>var(--nz-system-canvas)</c>); the same list when it has none.
    /// References remain dynamic and follow normal custom-property inheritance and overrides.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting IReadOnlyList&lt;StyleToken&gt; value.</returns>
    public static IReadOnlyList<StyleToken> Substitute(IReadOnlyList<StyleToken> value)
    {
        List<StyleToken>? result = null;
        for (var i = 0; i < value.Count; i++)
        {
            var token = value[i];
            if (token.Kind == StyleTokenKind.Ident && Variable(token.Value) is { } variable)
            {
                result ??= [.. value.Take(i)];
                result.Add(new StyleToken(StyleTokenKind.Function, "var", 0, token.Start, token.Length));
                result.Add(new StyleToken(StyleTokenKind.Ident, variable, 0, token.Start, token.Length));
                result.Add(new StyleToken(StyleTokenKind.RightParen, ")", 0, token.Start, token.Length));
            }
            else result?.Add(token);
        }
        return result ?? value;
    }
}
