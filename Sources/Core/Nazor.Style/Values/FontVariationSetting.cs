using System.Globalization;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>Controls automatic optical sizing for text.</summary>
public enum FontOpticalSizing : byte
{
    /// <summary>Uses the computed font size in logical pixels, matching CSS.</summary>
    Auto,

    /// <summary>Disables automatic optical sizing; explicit axis coordinates still apply.</summary>
    None,

    /// <summary>Uses typographic points for native desktop text; an NSS extension.</summary>
    Native,
}

/// <summary>An explicit coordinate on a case-sensitive OpenType variation axis.</summary>
public readonly record struct FontVariationSetting
{
    /// <summary>Initializes an axis coordinate.</summary>
    /// <param name="tag">Exactly four printable ASCII characters, including spaces.</param>
    /// <param name="value">A finite design coordinate; supported font axes clamp it to their range.</param>
    /// <exception cref="ArgumentException">The tag is not four printable ASCII characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public FontVariationSetting(string tag, double value)
    {
        if (!IsValidTag(tag)) throw new ArgumentException("A variation axis tag must contain exactly four printable ASCII characters.", nameof(tag));
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Tag = tag;
        Value = value;
    }

    /// <summary>Gets the four-character, case-sensitive axis tag.</summary>
    public string Tag { get; }

    /// <summary>Gets the finite design coordinate.</summary>
    public double Value { get; }

    /// <summary>Formats this coordinate as a quoted NSS axis tag followed by its number.</summary>
    /// <returns>The NSS representation.</returns>
    public override string ToString() => new StyleToken(StyleTokenKind.String, Tag, 0, 0, 0) + " " + Value.ToString("R", CultureInfo.InvariantCulture);

    internal static bool IsValidTag(string? tag) => tag is { Length: 4 } && tag.All(c => c is >= ' ' and <= '~');

    internal static ValueList<FontVariationSetting> Canonicalize(IEnumerable<FontVariationSetting> settings)
    {
        var axes = new SortedDictionary<string, FontVariationSetting>(StringComparer.Ordinal);
        foreach (var setting in settings)
        {
            if (!IsValidTag(setting.Tag) || !double.IsFinite(setting.Value)) throw new ArgumentException("Invalid font variation coordinate.", nameof(settings));
            axes[setting.Tag] = setting;
        }
        return new ValueList<FontVariationSetting>(axes.Values);
    }

    internal static ValueList<FontVariationSetting> Parse(TokenReader reader)
    {
        if (reader.TryConsumeIdent("normal")) return ValueList<FontVariationSetting>.Empty;
        var settings = new List<FontVariationSetting>();
        foreach (var part in reader.SplitByComma())
        {
            var tag = part.Next();
            var coordinate = part.Next();
            part.ExpectEnd();
            if (tag.Kind != StyleTokenKind.String || !IsValidTag(tag.Value))
                throw TokenReader.Error("Expected a quoted four-character printable ASCII variation axis tag.", tag);
            if (coordinate.Kind != StyleTokenKind.Number || !double.IsFinite(coordinate.Number))
                throw TokenReader.Error("Expected a finite variation axis coordinate.", coordinate);
            settings.Add(new(tag.Value, coordinate.Number));
        }
        return Canonicalize(settings);
    }
}
