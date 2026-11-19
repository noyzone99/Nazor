using System.Globalization;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>An explicit, case-sensitive OpenType feature applied to a shaped text segment.</summary>
public readonly record struct FontFeatureSetting
{
    /// <summary>Initializes a font feature.</summary>
    /// <param name="tag">Exactly four printable ASCII characters.</param>
    /// <param name="value">Zero disables the feature, one enables it, higher values select font-defined alternates.</param>
    /// <exception cref="ArgumentException">The tag is invalid.</exception>
    public FontFeatureSetting(string tag, uint value = 1)
    {
        if (!FontVariationSetting.IsValidTag(tag)) throw new ArgumentException("A feature tag must contain exactly four printable ASCII characters.", nameof(tag));
        Tag = tag; Value = value;
    }

    /// <summary>The case-sensitive four-character tag.</summary>
    public string Tag { get; }
    /// <summary>The unsigned feature selection.</summary>
    public uint Value { get; }
    /// <summary>Formats a quoted tag and numeric selection.</summary>
    /// <returns>Canonical NSS syntax.</returns>
    public override string ToString() => new StyleToken(StyleTokenKind.String,Tag,0,0,0) + " " + Value.ToString(CultureInfo.InvariantCulture);

    internal static ValueList<FontFeatureSetting> Canonicalize(IEnumerable<FontFeatureSetting> values)
    {
        var result = new SortedDictionary<string,FontFeatureSetting>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!FontVariationSetting.IsValidTag(value.Tag)) throw new ArgumentException("Invalid font feature tag.",nameof(values));
            result[value.Tag] = value;
        }
        return new(result.Values);
    }

    internal static ValueList<FontFeatureSetting> Parse(TokenReader reader)
    {
        if (reader.TryConsumeIdent("normal")) return ValueList<FontFeatureSetting>.Empty;
        var result = new List<FontFeatureSetting>();
        foreach (var part in reader.SplitByComma())
        {
            var tag = part.Next();
            if (tag.Kind != StyleTokenKind.String || !FontVariationSetting.IsValidTag(tag.Value)) throw TokenReader.Error("Expected a quoted four-character printable ASCII feature tag.",tag);
            uint value = 1;
            if (part.TryConsumeIdent("off")) value = 0;
            else if (!part.TryConsumeIdent("on") && !part.AtEnd)
            {
                var number = part.Next();
                if (number.Kind != StyleTokenKind.Number || !double.IsFinite(number.Number) || number.Number < 0 || number.Number > uint.MaxValue || Math.Truncate(number.Number) != number.Number)
                    throw TokenReader.Error("Expected an unsigned 32-bit feature selection.",number);
                value = (uint)number.Number;
            }
            part.ExpectEnd(); result.Add(new(tag.Value,value));
        }
        return Canonicalize(result);
    }
}
