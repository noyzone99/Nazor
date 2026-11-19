using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nazor.LanguageServer.Features;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer;

internal sealed partial class NazorLanguageServer
{
    private IReadOnlyList<NssReferenceRule> SelectorReferences(JsonObject parameters)
    {
        var uri = Uri(parameters);
        var path = TextDocument.PathOf(uri);
        if (!path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".nss", StringComparison.OrdinalIgnoreCase)) return [];
        var open = documents.All.ToDictionary(d => d.Path, d => d.Text, StringComparer.OrdinalIgnoreCase);
        var text = parameters["text"]?.GetValue<string>() ?? open.GetValueOrDefault(path) ?? ReadText(path);
        if (text is null) return [];
        if (path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase)) open[path] = text;
        var snapshot = Project(path).Current();
        return NssReferences.Query(path, text, SelectorDocuments(path, open, snapshot), snapshot.Analysis.Catalog);
    }

    private NssReferenceDocument[] SelectorDocuments(string path, IReadOnlyDictionary<string, string> open, ProjectSnapshot? snapshot = null)
    {
        var context = ProjectContext.ForDocument(path);
        var folders = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { [context.Directory] = true };
        var files = context.Components.Concat(Directory.EnumerateFiles(context.Directory, "*.nazor", context.ProjectFile is null ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
            .Concat(open.Keys.Where(p => p.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase)))
            .Where(p => p.StartsWith(context.Directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !ProjectContext.IsBuildOutput(context.Directory, p) &&
                OwnedFolder(Path.GetDirectoryName(p)!))
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);
        return files.Select(p =>
        {
            var source = open.GetValueOrDefault(p) ?? ReadText(p) ?? "";
            var explicitNamespace = Regex.Match(source, @"(?m)^\s*@namespace\s+([\w.]+)");
            var generatedNamespace = snapshot?.Component(p) is { } component ? Regex.Match(component.Generated.CSharp, @"\bnamespace\s+([\w.]+)") : Match.Empty;
            var ns = explicitNamespace.Success ? explicitNamespace.Groups[1].Value : generatedNamespace.Success ? generatedNamespace.Groups[1].Value : context.RootNamespace;
            return new NssReferenceDocument(p, source, ns + "." + Path.GetFileNameWithoutExtension(p));
        }).ToArray();

        bool OwnedFolder(string folder)
        {
            if (folders.TryGetValue(folder, out var owned)) return owned;
            return folders[folder] = context.ProjectFile is not null && !Directory.EnumerateFiles(folder, "*.csproj").Any() &&
                Path.GetDirectoryName(folder) is { } parent && OwnedFolder(parent);
        }
    }

    private object[] SelectorCodeLens(JsonObject parameters)
    {
        var uri = Uri(parameters);
        var text = documents.Get(uri)?.Text ?? ReadText(TextDocument.PathOf(uri));
        if (text is null) return [];
        return [.. SelectorReferences(parameters).Select(rule =>
        {
            var range = TextPositions.Range(text, rule.Start, rule.Start + rule.Length);
            var title = $"{rule.References.Count}{(rule.Partial ? "+" : "")} {(rule.Global ? "potential " : "")}references";
            return new { range, command = new { title, command = "nazor.showNssReferences", arguments = new object[] { uri, range.Start, rule.References } } };
        })];
    }

    private IReadOnlyList<LspLocation> SelectorLocations(JsonObject parameters)
    {
        var document = documents.Get(Uri(parameters));
        if (document is null) return [];
        var offset = TextPositions.Offset(document.Text, PositionOf(parameters));
        return SelectorReferences(parameters).FirstOrDefault(r => offset >= r.Start && offset <= r.Start + r.Length)?.References
            .Select(r => new LspLocation(r.Uri, r.Range)).ToArray() ?? [];
    }
}
