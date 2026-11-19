using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace Nazor.Skia;

internal static class SkiaFontNames
{
    internal static string[] TypographicFamilies(SKTypeface typeface)
    {
        if (!typeface.TryGetTableData(0x6e616d65, out var table) || table is not { Length: >= 6 }) return [];
        var count = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(2));
        var storage = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(4));
        if (count > (table.Length - 6) / 12 || storage > table.Length) return [];
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var encoding = new UnicodeEncoding(true, false, true);
        for (var index = 0; index < count; index++)
        {
            var record = table.AsSpan(6 + index * 12, 12);
            var platform = BinaryPrimitives.ReadUInt16BigEndian(record);
            var platformEncoding = BinaryPrimitives.ReadUInt16BigEndian(record[2..]);
            if (BinaryPrimitives.ReadUInt16BigEndian(record[6..]) is not (1 or 16) ||
                !(platform == 0 || platform == 3 && platformEncoding is 0 or 1 or 10)) continue;
            var length = BinaryPrimitives.ReadUInt16BigEndian(record[8..]);
            var offset = storage + BinaryPrimitives.ReadUInt16BigEndian(record[10..]);
            if (length % 2 != 0 || offset > table.Length - length) continue;
            try
            {
                var name = encoding.GetString(table, offset, length);
                if (!string.IsNullOrWhiteSpace(name) && !name.Any(char.IsControl)) names.Add(name);
            }
            catch (DecoderFallbackException) { }
        }
        return names.ToArray();
    }
}
