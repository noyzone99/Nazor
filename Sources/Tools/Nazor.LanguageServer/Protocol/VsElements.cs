using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;

namespace Nazor.LanguageServer.Protocol;

internal static class VsElements
{
    private const string ImageCatalog = "ae27a6b0-e345-4288-96df-5eaf394ee369";

    public const int Wrapped = 0;
    public const int Stacked = 1;
    public const int VerticalPadding = 2;

    public static JsonObject Run(string classification, string text) => new()
    {
        ["_vs_type"] = "ClassifiedTextRun",
        ["ClassificationTypeName"] = classification,
        ["Text"] = text,
        ["MarkerTagType"] = null,
        ["Style"] = 0,
    };

    public static JsonObject Text(IEnumerable<JsonObject> runs) => new()
    {
        ["_vs_type"] = "ClassifiedTextElement",
        ["Runs"] = new JsonArray([.. runs]),
    };

    public static JsonObject Container(int style, IEnumerable<JsonNode> elements) => new()
    {
        ["_vs_type"] = "ContainerElement",
        ["Elements"] = new JsonArray([.. elements]),
        ["Style"] = style,
    };

    public static JsonObject Image(int id) => Image(ImageCatalog, id);

    public static JsonObject Image(string catalog, int id) => new()
    {
        ["_vs_type"] = "ImageElement",
        ["ImageId"] = new JsonObject { ["_vs_type"] = "ImageId", ["Guid"] = catalog, ["Id"] = id },
    };

    public static JsonObject Text(IEnumerable<TaggedText> parts) =>
        Text(parts.Where(p => p.Text.Length != 0).Select(p => Run(Classification(p.Tag), p.Text)));

    public static string Classification(string tag) => tag switch
    {
        TextTags.Keyword => "keyword",
        TextTags.Class => "class name",
        TextTags.Record => "record class name",
        TextTags.RecordStruct => "record struct name",
        TextTags.Struct => "struct name",
        TextTags.Interface => "interface name",
        TextTags.Enum => "enum name",
        TextTags.Delegate => "delegate name",
        TextTags.TypeParameter => "type parameter name",
        TextTags.Module => "module name",
        TextTags.Method => "method name",
        TextTags.ExtensionMethod => "extension method name",
        TextTags.Property => "property name",
        TextTags.Field => "field name",
        TextTags.Constant => "constant name",
        TextTags.EnumMember => "enum member name",
        TextTags.Event => "event name",
        TextTags.Local or TextTags.RangeVariable => "local name",
        TextTags.Parameter => "parameter name",
        TextTags.Label => "label name",
        TextTags.Namespace => "namespace name",
        TextTags.Operator => "operator",
        TextTags.Punctuation => "punctuation",
        TextTags.StringLiteral => "string",
        TextTags.NumericLiteral => "number",
        _ => "text",
    };

    public static int Image(IEnumerable<string> tags)
    {
        var set = tags.ToHashSet(StringComparer.Ordinal);
        int Access(int @public, int @protected, int @private, int other) =>
            set.Contains(WellKnownTags.Public) ? @public : set.Contains(WellKnownTags.Protected) ? @protected : set.Contains(WellKnownTags.Private) ? @private : other;
        if (set.Contains(WellKnownTags.Class)) return Access(473, 472, 471, 463);
        if (set.Contains(WellKnownTags.Property)) return Access(2436, 2435, 2434, 2429);
        if (set.Contains(WellKnownTags.Method)) return Access(1880, 1879, 1878, 1874);
        if (set.Contains(WellKnownTags.ExtensionMethod)) return 1204;
        if (set.Contains(WellKnownTags.Field)) return Access(1222, 1221, 1220, 1217);
        if (set.Contains(WellKnownTags.Event)) return Access(1152, 1142, 1150, 1142);
        if (set.Contains(WellKnownTags.Constant)) return Access(620, 616, 618, 616);
        if (set.Contains(WellKnownTags.Local) || set.Contains(WellKnownTags.RangeVariable)) return 1747;
        if (set.Contains(WellKnownTags.Parameter)) return 2242;
        if (set.Contains(WellKnownTags.Namespace)) return 1951;
        if (set.Contains(WellKnownTags.Keyword)) return 1589;
        if (set.Contains(WellKnownTags.EnumMember)) return 1125;
        if (set.Contains(WellKnownTags.Enum)) return 1131;
        if (set.Contains(WellKnownTags.Interface)) return 1608;
        if (set.Contains(WellKnownTags.Structure)) return 2996;
        if (set.Contains(WellKnownTags.Delegate)) return 913;
        if (set.Contains(WellKnownTags.TypeParameter)) return 3233;
        if (set.Contains(WellKnownTags.Operator)) return 2177;
        if (set.Contains(WellKnownTags.Snippet)) return 2852;
        return 3233;
    }

    public static int Kind(IEnumerable<string> tags)
    {
        var set = tags.ToHashSet(StringComparer.Ordinal);
        if (set.Contains(WellKnownTags.Class) || set.Contains(WellKnownTags.Delegate)) return CompletionItemKind.Class;
        if (set.Contains(WellKnownTags.Structure)) return 22;
        if (set.Contains(WellKnownTags.Interface)) return 8;
        if (set.Contains(WellKnownTags.Enum)) return 13;
        if (set.Contains(WellKnownTags.EnumMember)) return 20;
        if (set.Contains(WellKnownTags.Method) || set.Contains(WellKnownTags.ExtensionMethod)) return 2;
        if (set.Contains(WellKnownTags.Property)) return CompletionItemKind.Property;
        if (set.Contains(WellKnownTags.Field)) return 5;
        if (set.Contains(WellKnownTags.Event)) return CompletionItemKind.Event;
        if (set.Contains(WellKnownTags.Local) || set.Contains(WellKnownTags.Parameter) || set.Contains(WellKnownTags.RangeVariable)) return CompletionItemKind.Variable;
        if (set.Contains(WellKnownTags.Namespace)) return 9;
        if (set.Contains(WellKnownTags.Keyword)) return CompletionItemKind.Keyword;
        if (set.Contains(WellKnownTags.Constant)) return 21;
        if (set.Contains(WellKnownTags.TypeParameter)) return 25;
        if (set.Contains(WellKnownTags.Snippet)) return 15;
        if (set.Contains(WellKnownTags.Operator)) return 24;
        return 1;
    }
}
