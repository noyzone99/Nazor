using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;
using Range = Nazor.LanguageServer.Protocol.Range;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;
using RoslynSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;

namespace Nazor.LanguageServer.Features;

internal readonly record struct FileRange(string Path, Range Range);

internal sealed record RenameEdits(Dictionary<string, List<TextEdit>> Edits, IReadOnlyList<(string From, string To)> Files);

internal sealed class RequestFailedException(string message) : Exception(message);
internal sealed record MappedCSharpDiagnostic(string Id, int Severity, int Start, int Length, string Message, IReadOnlyList<int>? Tags = null, string? HelpLink = null, bool Hidden = false);

internal sealed class CSharpLanguage
{
    private static readonly string[] CommitCharacters = [".", "(", ";", ",", "["];

    private (Document Document, ImmutableArray<RoslynCompletion.CompletionItem> Items)? lastCompletion;

    public async Task<List<CompletionItem>?> CompleteAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, char? trigger, string text, CancellationToken cancellation)
    {
        if (snapshot.Document(component) is not { } document) return null;
        var mapped = component.ToGenerated(offset);
        var trailingDot = mapped is null && offset > 0 && text[offset - 1] == '.' && component.ToGenerated(offset - 1) is not null;
        if (trailingDot)
        {
            var end = component.ToGenerated(offset - 1)!.Value;
            document = document.WithText((await document.GetTextAsync(cancellation)).WithChanges(new TextChange(new TextSpan(end, 0), ".")));
            mapped = end + 1;
        }
        if (mapped is not { } position) return null;
        var service = RoslynCompletion.CompletionService.GetService(document);
        if (service is null) return null;
        var roslynTrigger = trigger is { } c && !char.IsWhiteSpace(c) ? RoslynCompletion.CompletionTrigger.CreateInsertionTrigger(c) : RoslynCompletion.CompletionTrigger.Invoke;
        var list = await service.GetCompletionsAsync(document, position, roslynTrigger, cancellationToken: cancellation);
        if (list.ItemsList.Count == 0) return null;
        var original = trailingDot && list.Span.Start == position && list.Span.Length == 0
            ? (offset, 0) : component.ToOriginal(list.Span.Start, list.Span.Length);
        if (original is not { } span) return null;
        var range = TextPositions.Range(text, span.Start, span.Start + span.Length);
        var items = list.ItemsList.ToImmutableArray();
        lastCompletion = (document, items);
        List<CompletionItem> result = [.. items.Select((item, index) => new CompletionItem(item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix, VsElements.Kind(item.Tags))
        {
            TextEdit = new TextEdit(range, item.Properties.TryGetValue("InsertionText", out var insertion) ? insertion : item.DisplayText),
            SortText = item.SortText,
            FilterText = item.FilterText,
            CommitCharacters = CommitCharacters,
            Data = new JsonObject { ["csharp"] = index },
            VsIcon = VsElements.Image(VsElements.Image(item.Tags)),
        })];
        if (!list.Span.IsEmpty && await document.GetSyntaxRootAsync(cancellation) is { } root)
        {
            string[] snippet = [WellKnownTags.Snippet];
            foreach (var (shortcut, description) in ClassicSnippets.At(root.FindToken(list.Span.Start)))
            {
                if (result.Any(i => i.Label == shortcut)) continue;
                result.Add(new CompletionItem(shortcut, VsElements.Kind(snippet))
                {
                    TextEdit = new TextEdit(range, shortcut),
                    SortText = shortcut,
                    FilterText = shortcut,
                    Detail = description,
                    VsIcon = VsElements.Image(VsElements.Image(snippet)),
                });
            }
        }
        return result;
    }

    public async Task<CompletionItem> ResolveAsync(CompletionItem item, CancellationToken cancellation)
    {
        if (item.Data?["csharp"] is not { } index || lastCompletion is not { } last || index.GetValue<int>() >= last.Items.Length) return item;
        var service = RoslynCompletion.CompletionService.GetService(last.Document);
        var description = service is null ? null : await service.GetDescriptionAsync(last.Document, last.Items[index.GetValue<int>()], cancellation);
        if (description is null || description.TaggedParts.IsEmpty) return item;
        return item with
        {
            Documentation = new MarkupContent("```csharp\n" + description.Text + "\n```"),
            VsDescription = VsElements.Text(description.TaggedParts),
        };
    }

    public async Task<(JsonObject Raw, string Plain, int Start, int Length)?> HoverAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        if (component.ToGenerated(offset) is not { } position || snapshot.Document(component) is not { } document) return null;
        var service = QuickInfoService.GetService(document);
        var info = service is null ? null : await service.GetQuickInfoAsync(document, position, cancellation);
        if (info is null || info.Sections.IsEmpty) return null;
        if (component.ToOriginal(info.Span.Start, info.Span.Length) is not { } span) return null;
        var elements = new List<JsonNode>();
        var plain = new StringBuilder();
        foreach (var section in info.Sections)
        {
            if (section.TaggedParts.IsEmpty) continue;
            var classified = VsElements.Text(section.TaggedParts);
            elements.Add(section.Kind == QuickInfoSectionKinds.Description
                ? VsElements.Container(VsElements.Wrapped, [VsElements.Image(VsElements.Image(info.Tags)), classified])
                : classified);
            if (plain.Length != 0) plain.Append("\n\n");
            plain.Append(section.Text);
        }
        return (VsElements.Container(VsElements.Stacked | VsElements.VerticalPadding, elements), plain.ToString(), span.Start, span.Length);
    }

    public static async Task<List<FileRange>> DefinitionAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        var result = new List<FileRange>();
        if (await SymbolAtAsync(snapshot, component, offset, cancellation) is not { } found) return result;
        foreach (var location in found.Symbol.Locations)
            if (Locate(snapshot, location) is { } place && !result.Contains(place)) result.Add(place);
        if (result.Count == 0 && found.Symbol is INamedTypeSymbol)
            foreach (var location in found.Symbol.Locations)
                if (Owner(snapshot, location) is { } owner) result.Add(new(owner.Path, new Range(new Position(0, 0), new Position(0, 0))));
        if (result.Count == 0 && await snapshot.CSharp.GetCompilationAsync(cancellation) is { } compilation &&
            MetadataAsSource.Locate(found.Symbol, compilation) is { } metadata)
            result.Add(metadata);
        return result;
    }

    public static async Task<(int Start, int Length, string Name)?> PrepareRenameAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        if (await SymbolAtAsync(snapshot, component, offset, cancellation) is not { } found ||
            !CanRename(snapshot, found.Symbol) && ComponentOf(snapshot, found.Symbol) is null) return null;
        if (component.ToOriginal(found.Token.Span.Start, found.Token.Span.Length) is not { } span) return null;
        return (span.Start, span.Length, found.Token.ValueText);
    }

    public static async Task<RenameEdits?> RenameAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, string newName, CancellationToken cancellation)
    {
        if (await SymbolAtAsync(snapshot, component, offset, cancellation) is not { } found) return null;
        if (ComponentOf(snapshot, found.Symbol) is { } target) return await RenameComponentAsync(snapshot, found.Symbol, target, newName, cancellation);
        if (!CanRename(snapshot, found.Symbol)) return null;
        return new(await RenameEditsAsync(snapshot, found.Symbol, newName, cancellation), []);
    }

    private static async Task<RenameEdits> RenameComponentAsync(ProjectSnapshot snapshot, ISymbol type, ComponentDocument target, string newName, CancellationToken cancellation)
    {
        if (!Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(newName))
            throw new RequestFailedException($"'{newName}' is not a valid component name.");
        var directory = Path.GetDirectoryName(target.Path)!;
        var newPath = Path.Combine(directory, newName + ".nazor");
        if (!string.Equals(newPath, target.Path, StringComparison.OrdinalIgnoreCase) && (File.Exists(newPath) || snapshot.Component(newPath) is not null))
            throw new RequestFailedException($"{newName}.nazor already exists.");
        var edits = await RenameEditsAsync(snapshot, type, newName, cancellation);
        foreach (var component in snapshot.Components.Values)
        {
            if (!edits.TryGetValue(component.Path, out var list)) continue;
            var tags = NazorLanguage.Tags(component.Text);
            foreach (var edit in list.ToList())
            {
                var start = TextPositions.Offset(component.Text, edit.Range.Start);
                var tag = tags.FirstOrDefault(t => !t.IsEnd && !t.SelfClosing && start >= t.NameStart && start < t.NameStart + t.Name.Length);
                if (tag is null || NazorLanguage.EndOf(tags, tag) is not { } end) continue;
                var nameStart = end.NameStart + start - tag.NameStart;
                var endEdit = new TextEdit(TextPositions.Range(component.Text, nameStart, nameStart + type.Name.Length), newName);
                if (!list.Contains(endEdit)) list.Add(endEdit);
            }
        }
        var files = new List<(string, string)>();
        foreach (var suffix in new[] { "", ".cs", ".nss" })
            if (suffix.Length == 0 || File.Exists(target.Path + suffix))
                files.Add((target.Path + suffix, newPath + suffix));
        return new(edits, files);
    }

    private static async Task<Dictionary<string, List<TextEdit>>> RenameEditsAsync(ProjectSnapshot snapshot, ISymbol symbol, string newName, CancellationToken cancellation)
    {
        var solution = snapshot.CSharp.Solution;
        var renamed = await Renamer.RenameSymbolAsync(solution, symbol, new SymbolRenameOptions(), newName, cancellation);
        var edits = new Dictionary<string, List<TextEdit>>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in renamed.GetChanges(solution).GetProjectChanges())
        {
            foreach (var id in project.GetChangedDocuments())
            {
                var before = solution.GetDocument(id)!;
                var after = renamed.GetDocument(id)!;
                var text = await before.GetTextAsync(cancellation);
                var owner = before.FilePath is { } generated && generated.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) ? snapshot.Component(generated[..^".g.cs".Length]) : null;
                if (owner is null && (before.FilePath is null || !File.Exists(before.FilePath))) continue;
                var path = owner?.Path ?? before.FilePath!;
                foreach (var change in await after.GetTextChangesAsync(before, cancellation))
                {
                    TextEdit edit;
                    if (owner is not null)
                    {
                        if (owner.ToOriginal(change.Span.Start, change.Span.Length) is not { } span ||
                            string.CompareOrdinal(owner.Text, span.Start, text.ToString(change.Span), 0, span.Length) != 0) continue;
                        edit = new(TextPositions.Range(owner.Text, span.Start, span.Start + span.Length), change.NewText ?? "");
                    }
                    else
                    {
                        var lines = text.Lines.GetLinePositionSpan(change.Span);
                        edit = new(new Range(new Position(lines.Start.Line, lines.Start.Character), new Position(lines.End.Line, lines.End.Character)), change.NewText ?? "");
                    }
                    var list = edits.TryGetValue(path, out var existing) ? existing : edits[path] = [];
                    if (!list.Contains(edit)) list.Add(edit);
                }
            }
        }
        return edits;
    }

    public static async Task<(int Start, int Length, string Snippet)?> SnippetAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        if (component.ToGenerated(offset) is not { } position || snapshot.Document(component) is not { } document) return null;
        var service = RoslynCompletion.CompletionService.GetService(document);
        if (service is null) return null;
        var list = await service.GetCompletionsAsync(document, position, RoslynCompletion.CompletionTrigger.Invoke, cancellationToken: cancellation);
        if (list.Span.End != position || list.Span.IsEmpty) return null;
        var text = await document.GetTextAsync(cancellation);
        var word = text.ToString(list.Span);
        var item = list.ItemsList.FirstOrDefault(i => i.DisplayText == word && i.Tags.Contains(WellKnownTags.Snippet));
        if (item is null)
        {
            if (await document.GetSyntaxRootAsync(cancellation) is not { } root || ClassicSnippets.Find(word, root.FindToken(list.Span.Start)) is not { } classic ||
                component.ToOriginal(list.Span.Start, list.Span.Length) is not { } shortcut) return null;
            return (shortcut.Start, shortcut.Length, Reindent(classic, component.Text, shortcut.Start));
        }
        var change = await service.GetChangeAsync(document, item, null, cancellation);
        var changed = change.TextChange.Span;
        if (!changed.Contains(list.Span) || text.ToString(TextSpan.FromBounds(changed.Start, list.Span.Start)).Trim().Length != 0 ||
            text.ToString(TextSpan.FromBounds(list.Span.End, changed.End)).Trim().Length != 0) return null;
        var snippet = (LspSnippet(change) ?? PlainSnippet(change)).Trim(' ', '\t', '\r', '\n');
        if (component.ToOriginal(list.Span.Start, list.Span.Length) is not { } span) return null;
        return (span.Start, span.Length, Reindent(snippet, component.Text, span.Start));
    }

    private static string? LspSnippet(RoslynCompletion.CompletionChange change) =>
        typeof(RoslynCompletion.CompletionChange).GetProperty("Properties", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(change)
            is IReadOnlyDictionary<string, string> properties && properties.TryGetValue("LSPSnippet", out var snippet) ? snippet : null;

    private static string PlainSnippet(RoslynCompletion.CompletionChange change)
    {
        var text = change.TextChange.NewText ?? "";
        var caret = Math.Clamp((change.NewPosition ?? change.TextChange.Span.Start + text.Length) - change.TextChange.Span.Start, 0, text.Length);
        return Escape(text[..caret]) + "$0" + Escape(text[caret..]);
        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("$", "\\$").Replace("}", "\\}");
    }

    private static string Reindent(string snippet, string file, int start)
    {
        var lines = snippet.Split('\n');
        if (lines.Length == 1) return snippet;
        var lineStart = start;
        while (lineStart > 0 && file[lineStart - 1] is not ('\r' or '\n')) lineStart--;
        var prefix = file[lineStart..start];
        var indentation = prefix[..(prefix.Length - prefix.TrimStart(' ', '\t').Length)];
        var lead = lines.Skip(1).Where(l => l.Trim().Length != 0).Select(l => l.Length - l.TrimStart(' ', '\t').Length).DefaultIfEmpty(0).Min();
        for (var i = 1; i < lines.Length; i++)
            if (lines[i].Trim().Length != 0) lines[i] = indentation + lines[i][Math.Min(lead, lines[i].Length - lines[i].TrimStart(' ', '\t').Length)..];
        return string.Join('\n', lines);
    }

    public static List<TextEdit> GeneratedEdits(ComponentDocument component, string built, IEnumerable<(int Start, int Length, string Text)> changes)
    {
        var edits = new List<TextEdit>();
        if (component.Generated.CSharp != built) return edits;
        foreach (var (start, length, newText) in changes)
        {
            if (start < 0 || length < 0 || start + length > built.Length || component.ToOriginal(start, length) is not { } span ||
                string.CompareOrdinal(component.Text, span.Start, built, start, length) != 0) continue;
            var edit = new TextEdit(TextPositions.Range(component.Text, span.Start, span.Start + span.Length), newText);
            if (!edits.Contains(edit)) edits.Add(edit);
        }
        return edits;
    }

    public static async Task<List<TextEdit>> StaleRenameEditsAsync(ProjectSnapshot snapshot, ComponentDocument component, string built,
        IReadOnlyList<(int Start, int Length, string Text)> changes, CancellationToken cancellation)
    {
        if (changes.Count == 0 || snapshot.Document(component) is not { } document ||
            changes.Any(c => c.Start < 0 || c.Length <= 0 || c.Start + c.Length > built.Length)) return [];
        var oldName = built.Substring(changes[0].Start, changes[0].Length);
        var newName = changes[0].Text;
        if (!Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(newName) ||
            changes.Any(c => c.Text != newName || string.CompareOrdinal(built, c.Start, oldName, 0, c.Length) != 0 || c.Length != oldName.Length)) return [];
        var after = built;
        foreach (var change in changes.OrderByDescending(c => c.Start)) after = after[..change.Start] + change.Text + after[(change.Start + change.Length)..];
        if (after == component.Generated.CSharp) return [];
        var before = document.WithText(SourceText.From(built));
        ISymbol? symbol = null;
        foreach (var change in changes)
        {
            if (await SymbolFinder.FindSymbolAtPositionAsync(before, change.Start, cancellation) is { } found && found.Name == oldName)
            {
                symbol = found is IAliasSymbol alias ? alias.Target : found.OriginalDefinition;
                break;
            }
        }
        if (symbol is null || DocumentationCommentId.CreateDeclarationId(symbol) is not { } id ||
            await snapshot.CSharp.GetCompilationAsync(cancellation) is not { } compilation ||
            DocumentationCommentId.GetFirstSymbolForDeclarationId(id, compilation) is not { } current || !CanRename(snapshot, current)) return [];
        return (await RenameEditsAsync(snapshot, current, newName, cancellation)).GetValueOrDefault(component.Path) ?? [];
    }

    private static async Task<(ISymbol Symbol, SyntaxToken Token)?> SymbolAtAsync(ProjectSnapshot snapshot, ComponentDocument component, int offset, CancellationToken cancellation)
    {
        if (component.ToGenerated(offset) is not { } position || snapshot.Document(component) is not { } document) return null;
        var root = await document.GetSyntaxRootAsync(cancellation);
        if (root is null) return null;
        foreach (var candidate in new[] { position, position - 1 })
        {
            if (candidate < 0) continue;
            var token = root.FindToken(candidate);
            if (!token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken) || !token.Span.IntersectsWith(candidate) && token.Span.End != candidate) continue;
            if (await SymbolFinder.FindSymbolAtPositionAsync(document, token.SpanStart, cancellation) is { } symbol)
                return (symbol is IAliasSymbol alias ? alias.Target : symbol.OriginalDefinition, token);
        }
        return null;
    }

    private static ComponentDocument? ComponentOf(ProjectSnapshot snapshot, ISymbol symbol) =>
        symbol is INamedTypeSymbol ? symbol.Locations.Select(l => Owner(snapshot, l)).FirstOrDefault(c => c is not null) : null;

    private static bool CanRename(ProjectSnapshot snapshot, ISymbol symbol) =>
        symbol.Locations.Length != 0 && symbol.Locations.All(l => l.IsInSource) &&
        !(symbol is INamedTypeSymbol && symbol.Locations.Any(l => Owner(snapshot, l) is not null)) &&
        symbol.Kind is not (SymbolKind.Namespace or SymbolKind.Assembly);

    private static ComponentDocument? Owner(ProjectSnapshot snapshot, Location location) =>
        location.SourceTree?.FilePath is { } path && path.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase) ? snapshot.Component(path[..^".g.cs".Length]) : null;

    private static FileRange? Locate(ProjectSnapshot snapshot, Location location)
    {
        if (!location.IsInSource || location.SourceTree is not { } tree) return null;
        if (Owner(snapshot, location) is { } owner)
            return owner.ToOriginal(location.SourceSpan.Start, location.SourceSpan.Length) is { } span
                ? new(owner.Path, TextPositions.Range(owner.Text, span.Start, span.Start + span.Length))
                : null;
        if (!File.Exists(tree.FilePath)) return null;
        var lines = location.GetLineSpan();
        return new(tree.FilePath, new Range(new Position(lines.StartLinePosition.Line, lines.StartLinePosition.Character), new Position(lines.EndLinePosition.Line, lines.EndLinePosition.Character)));
    }

    public static async Task<List<MappedCSharpDiagnostic>> DiagnosticsAsync(ProjectSnapshot snapshot, ComponentDocument component, CancellationToken cancellation)
    {
        var result = new List<MappedCSharpDiagnostic>();
        if (snapshot.Document(component) is not { } document || await document.GetSemanticModelAsync(cancellation) is not { } model) return result;
        foreach (var diagnostic in (await ProjectTools.CompilerDiagnosticsAsync(snapshot, cancellation)).Concat(await ProjectTools.DiagnosticsAsync(snapshot, cancellation)))
        {
            if (diagnostic.IsSuppressed) continue;
            if (!diagnostic.Location.IsInSource)
            {
                if (diagnostic.Id is "NZL001" or "AD0001") result.Add(new(diagnostic.Id, 2, 0, 0, diagnostic.GetMessage()));
                continue;
            }
            if (diagnostic.Location.SourceTree != model.SyntaxTree) continue;
            var span = diagnostic.Location.SourceSpan;
            if (component.ToOriginal(span.Start, span.Length) is not { } original) continue;
            var severity = diagnostic.Severity switch { RoslynSeverity.Error => 1, RoslynSeverity.Warning => 2, RoslynSeverity.Info => 3, _ => 4 };
            var tags = diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary) ? new[] { 1 } : null;
            result.Add(new(diagnostic.Id, severity, original.Start, original.Length, diagnostic.GetMessage(), tags, diagnostic.Descriptor.HelpLinkUri, diagnostic.Severity == RoslynSeverity.Hidden));
        }
        return result;
    }

    public static async Task<List<(int Start, int Length, string Classification)>> ClassifyAsync(ProjectSnapshot snapshot, ComponentDocument component, CancellationToken cancellation)
    {
        var result = new List<(int, int, string)>();
        if (snapshot.Document(component) is not { } document) return result;
        var seen = new HashSet<(int, int, string)>();
        foreach (var mapping in component.Generated.Mappings)
        {
            if (mapping.Length == 0) continue;
            foreach (var span in await Classifier.GetClassifiedSpansAsync(document, new TextSpan(mapping.GeneratedStart, mapping.Length), cancellation))
            {
                if (span.ClassificationType is ClassificationTypeNames.WhiteSpace or ClassificationTypeNames.Text) continue;
                var start = Math.Max(span.TextSpan.Start, mapping.GeneratedStart);
                var end = Math.Min(span.TextSpan.End, mapping.GeneratedEnd);
                if (end <= start) continue;
                var token = (mapping.OriginalStart + start - mapping.GeneratedStart, end - start, span.ClassificationType);
                if (seen.Add(token)) result.Add(token);
            }
        }
        return result;
    }
}
