namespace Nazor.Themes.Fluent;

/// <summary>A semantic WinUI icon that prefers an available Segoe family and falls back to packaged Fluent System Icons.</summary>
/// <remarks>
/// Register the portable fonts with <c>AddFluentFonts()</c>. Resolution checks the actual font provider's exact family
/// and Unicode coverage; it does not assume that a font is available from the operating system name. No Segoe font is
/// distributed. The portable fallback has different artwork; use <see cref="FluentSymbol" /> for fixed portable artwork.
/// The default value is empty and cannot be used as an active <c>Icon.Symbol</c>.
/// </remarks>
public readonly struct WinUISymbol : IFontSymbol
{
    private readonly string? glyph;
    private readonly IReadOnlyList<IFontSymbol>? fallbacks;

    private WinUISymbol(string glyph, FluentSymbol portable, string? legacyGlyph = null)
    {
        this.glyph = glyph;
        fallbacks = Array.AsReadOnly<IFontSymbol>([new FontSymbol("Segoe MDL2 Assets", legacyGlyph ?? glyph), portable]);
    }

    /// <summary>Gets Segoe Fluent Icons, or an empty family for the default value.</summary>
    public string FontFamily => glyph is null ? "" : "Segoe Fluent Icons";

    /// <summary>Gets the preferred Segoe Fluent Icons glyph, or empty text for the default value.</summary>
    public string Glyph => glyph ?? "";

    /// <summary>Gets the ordered Segoe MDL2 Assets and packaged Fluent System Icons alternatives.</summary>
    public IReadOnlyList<IFontSymbol> Fallbacks => fallbacks ?? Array.Empty<IFontSymbol>();

    /// <summary>Gets the Search icon, with Search16Regular as its portable alternative.</summary>
    public static WinUISymbol Search { get; } = new("\uE721", FluentSymbol.Search16Regular);

    /// <summary>Gets the Calendar icon, with CalendarLtr20Regular as its portable alternative.</summary>
    public static WinUISymbol Calendar { get; } = new("\uE787", FluentSymbol.CalendarLtr20Regular);

    /// <summary>Gets the RevealPasswordMedium icon, with the legacy reveal and Eye16Regular alternatives.</summary>
    public static WinUISymbol RevealPassword { get; } = new("\uF78D", FluentSymbol.Eye16Regular, "\uE052");

    /// <summary>Gets the CalendarView previous navigation glyph, EDDB at the template's 8px size, with CaretUp12Filled as its portable alternative.</summary>
    public static WinUISymbol CalendarPrevious { get; } = new("\uEDDB", FluentSymbol.CaretUp12Filled);

    /// <summary>Gets the CalendarView next navigation glyph, EDDC at the template's 8px size, with CaretDown12Filled as its portable alternative.</summary>
    public static WinUISymbol CalendarNext { get; } = new("\uEDDC", FluentSymbol.CaretDown12Filled);

    /// <summary>Gets the Add icon, with Add12Regular as its portable alternative.</summary>
    public static WinUISymbol Add { get; } = new("\uE710", FluentSymbol.Add12Regular);

    /// <summary>Gets the Cancel icon, with Dismiss12Regular as its portable alternative.</summary>
    public static WinUISymbol Cancel { get; } = new("\uE711", FluentSymbol.Dismiss12Regular);

    /// <summary>Gets the Info icon, with Info24Filled as its portable alternative.</summary>
    public static WinUISymbol Information { get; } = new("\uE946", FluentSymbol.Info24Filled);

    /// <summary>Gets the Warning icon, with Warning24Filled as its portable alternative.</summary>
    public static WinUISymbol Warning { get; } = new("\uE7BA", FluentSymbol.Warning24Filled);

    /// <summary>Gets the ErrorBadge icon, with ErrorCircle24Filled as its portable alternative.</summary>
    public static WinUISymbol Error { get; } = new("\uEA39", FluentSymbol.ErrorCircle24Filled);

    /// <summary>Gets the Help icon, with QuestionCircle24Filled as its portable alternative.</summary>
    public static WinUISymbol Help { get; } = new("\uE897", FluentSymbol.QuestionCircle24Filled);
}
