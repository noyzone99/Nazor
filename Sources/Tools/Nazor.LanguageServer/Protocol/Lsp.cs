using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nazor.LanguageServer.Protocol;


internal readonly record struct Position(int Line, int Character);

internal readonly record struct Range(Position Start, Position End);

internal static class DiagnosticSeverity
{
    public const int Error = 1;
    public const int Warning = 2;
}

internal static class VsDiagnosticTags
{
    public const int IntellisenseError = int.MaxValue - 2;
    public const int HiddenInErrorList = int.MaxValue - 4;
    public const int VisibleInErrorList = int.MaxValue - 5;
    public const int HiddenInEditor = int.MaxValue - 6;
    public const int SuppressEditorToolTip = int.MaxValue - 7;
}

internal sealed record Diagnostic(Range Range, int Severity, string Code, string Message)
{
    public string Source => "nazor";
    public IReadOnlyList<int>? Tags { get; init; }
    public DiagnosticCodeDescription? CodeDescription { get; init; }
}

internal sealed record DiagnosticCodeDescription(string Href);

internal sealed record PublishDiagnosticsParams(string Uri, int? Version, IReadOnlyList<Diagnostic> Diagnostics);

internal static class CompletionItemKind
{
    public const int Function = 3;
    public const int Variable = 6;
    public const int Class = 7;
    public const int Property = 10;
    public const int Value = 12;
    public const int Keyword = 14;
    public const int Color = 16;
    public const int Event = 23;
}

internal sealed record MarkupContent(string Value, string Kind = "markdown");

internal sealed record TextEdit(Range Range, string NewText);

internal sealed record CompletionItem(string Label, int Kind)
{
    public string? Detail { get; init; }

    public MarkupContent? Documentation { get; init; }

    public TextEdit? TextEdit { get; init; }

    public IReadOnlyList<TextEdit>? AdditionalTextEdits { get; init; }

    public string? SortText { get; init; }

    public string? FilterText { get; init; }

    public IReadOnlyList<string>? CommitCharacters { get; init; }

    public JsonNode? Data { get; init; }

    [JsonPropertyName("_vs_icon")]
    public JsonNode? VsIcon { get; init; }

    [JsonPropertyName("_vs_description")]
    public JsonNode? VsDescription { get; init; }
}

internal sealed record CompletionList(bool IsIncomplete, IReadOnlyList<CompletionItem> Items);

internal sealed record Hover(MarkupContent Contents, Range? Range)
{
    [JsonPropertyName("_vs_rawContent")]
    public JsonNode? VsRawContent { get; init; }
}

internal sealed record ParameterInformation(int[] Label, MarkupContent? Documentation);

internal sealed record SignatureInformation(string Label, MarkupContent? Documentation, IReadOnlyList<ParameterInformation> Parameters)
{
    public int? ActiveParameter { get; init; }
}

internal sealed record SignatureHelp(IReadOnlyList<SignatureInformation> Signatures, int ActiveSignature, int ActiveParameter);

internal static class LspSymbolKind
{
    public const int Module = 2;
    public const int Namespace = 3;
    public const int Class = 5;
    public const int Method = 6;
    public const int Property = 7;
    public const int Field = 8;
    public const int Constructor = 9;
    public const int Enum = 10;
    public const int Interface = 11;
    public const int EnumMember = 22;
    public const int Struct = 23;
    public const int Event = 24;
}

internal sealed record DocumentSymbol(string Name, int Kind, Range Range, Range SelectionRange)
{
    public string? Detail { get; init; }

    public IReadOnlyList<DocumentSymbol>? Children { get; init; }
}

internal sealed record LspLocation(string Uri, Range Range);

internal sealed record SymbolInformation(string Name, int Kind, LspLocation Location, string? ContainerName);

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class TextPositions
{
    public static int Offset(string text, Position position)
    {
        var line = 0;
        var offset = 0;
        while (line < position.Line && offset < text.Length)
        {
            var c = text[offset++];
            if (c == '\r' && offset < text.Length && text[offset] == '\n') offset++;
            if (c is '\r' or '\n') line++;
        }
        var end = offset;
        while (end < text.Length && text[end] is not ('\r' or '\n')) end++;
        return Math.Min(offset + Math.Max(position.Character, 0), end);
    }

    public static Position Position(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var line = 0;
        var lineStart = 0;
        for (var i = 0; i < offset; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') continue;
            if (text[i] is '\r' or '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }
        return new(line, offset - lineStart);
    }

    public static Range Range(string text, int start, int end) => new(Position(text, start), Position(text, end));
}
