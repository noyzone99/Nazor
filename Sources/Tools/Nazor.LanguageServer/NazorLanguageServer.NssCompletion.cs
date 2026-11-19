using System.Collections.Concurrent;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer;

internal sealed partial class NazorLanguageServer
{
    private readonly ConcurrentDictionary<string, (string Text, NssSelectorNames Names)> selectorNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, NssReferenceDocument[]> selectorSources = new(StringComparer.OrdinalIgnoreCase);

    private CompletionList? SelectorCompletion(TextDocument document, int offset)
    {
        if (!document.IsComponent && !document.IsStyleSheet) return null;
        var start = 0;
        var end = document.Text.Length;
        if (document.IsComponent)
        {
            var context = NazorLanguage.Classify(document.Text, offset);
            if (context.Kind != NazorContextKind.Style) return null;
            start = context.Start;
            end = context.End;
        }
        var styles = document.Text[start..end];
        if (NssLanguage.Classify(styles, offset - start).Kind is not (NssContextKind.ClassName or NssContextKind.Id)) return null;
        return new(true, NssLanguage.Complete(styles, offset - start, [], document.Text, o => o + start, visualStudio, () => SelectorNames(document)));
    }

    private NssSelectorNames SelectorNames(TextDocument document)
    {
        var owner = document.IsComponent ? document.Path : document.Path.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase) ? document.Path[..^4] : null;
        if (owner is not null) return Names(owner, ReadText(owner) ?? "");

        if (!selectorSources.TryGetValue(document.Path, out var sources))
        {
            sources = SelectorDocuments(document.Path, documents.All.ToDictionary(d => d.Path, d => d.Text, StringComparer.OrdinalIgnoreCase));
            selectorSources[document.Path] = sources;
        }
        var names = sources.Select(source => Names(source.Path, documents.TextOf(source.Path) ?? source.Text)).ToArray();
        return new([.. names.SelectMany(n => n.Classes).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            [.. names.SelectMany(n => n.Ids).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)])
        {
            Global = true,
            IdTypes = names.SelectMany(n => n.IdTypes).GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.First().Value : null, StringComparer.Ordinal),
        };
    }

    private NssSelectorNames Names(string path, string text)
    {
        if (selectorNames.TryGetValue(path, out var cached) && cached.Text == text) return cached.Names;
        var names = NssSelectorNames.FromMarkup(text, path);
        selectorNames[path] = (text, names);
        return names;
    }
}
