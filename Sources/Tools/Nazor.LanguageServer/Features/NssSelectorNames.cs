using Nazor.Style.Syntax;

namespace Nazor.LanguageServer.Features;

internal sealed record NssSelectorNames(IReadOnlyList<string> Classes, IReadOnlyList<string> Ids)
{
    public bool Global { get; init; }
    public IReadOnlyDictionary<string, string?> IdTypes { get; init; } = new Dictionary<string, string?>();
    public static NssSelectorNames FromMarkup(string text, string path)
    {
        var classes = new HashSet<string>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var types = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var tag in MarkupSource.Tags(text, path).Where(t => !t.IsEnd))
            foreach (var attribute in MarkupSource.Attributes(text[(tag.NameStart + tag.Name.Length)..tag.End], assignedOnly: true))
            {
                if (attribute.Value is not { } value) continue;
                if (attribute.Key.Equals("Class", StringComparison.OrdinalIgnoreCase))
                    foreach (var name in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                        if (StyleTokenizer.Tokenize(name) is [{ Kind: StyleTokenKind.Ident, Value: var parsed }, { Kind: StyleTokenKind.EndOfFile }] && parsed == name)
                            classes.Add(name);
                if (attribute.Key.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                    StyleTokenizer.Tokenize("#" + value.Trim()) is [{ Kind: StyleTokenKind.Hash, Value: var id }, { Kind: StyleTokenKind.EndOfFile }] && id == value.Trim())
                {
                    types[id] = ids.Add(id) && id == value ? tag.Name : null;
                }
            }
        return new([.. classes.Order(StringComparer.Ordinal)], [.. ids.Order(StringComparer.Ordinal)]) { IdTypes = types };
    }
}
