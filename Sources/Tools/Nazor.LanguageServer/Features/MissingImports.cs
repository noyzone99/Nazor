using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Features;

internal static class MissingImports
{
    public static TextEdit? Edit(ComponentDocument component, string ns)
    {
        if (string.IsNullOrEmpty(ns)) return null;
        var root = CSharpSyntaxTree.ParseText(component.Generated.CSharp).GetCompilationUnitRoot();
        if (root.Usings.Any(u => u.Alias is null && u.Name?.ToString() == ns) ||
            root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().Any(n => n.Name.ToString() == ns)) return null;
        var at = component.Text.StartsWith('\uFEFF') ? 1 : 0;
        var newline = component.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return new(TextPositions.Range(component.Text, at, at), "@using " + ns + newline);
    }

    public static async Task<List<CodeActionResult>> ActionsAsync(ProjectSnapshot snapshot, ComponentDocument component,
        int start, int end, CancellationToken cancellation)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var qualified = new List<CodeActionResult>();
        void Qualify(string ns, int from, int length, string name, bool markup)
        {
            if (Edit(component, ns) is null) return;
            var text = component.Text;
            var replacement = ns + "." + name;
            var updated = text;
            if (markup)
            {
                var tags = NazorLanguage.Tags(text);
                var tag = tags.FirstOrDefault(t => t.NameStart == from);
                if (tag is not null && NazorLanguage.EndOf(tags, tag) is { } closing)
                    updated = updated.Remove(closing.NameStart, closing.Name.Length).Insert(closing.NameStart, replacement);
            }
            updated = updated.Remove(from, length).Insert(from, replacement);
            qualified.Add(new("Use " + replacement, [new(component.Path, text, updated)]));
        }
        var context = NazorLanguage.Classify(component.Text, start);
        if (context.Kind == NazorContextKind.TagName && context.Tag is { } tag)
        {
            var candidates = snapshot.Analysis.Catalog.Find(tag).ToArray();
            if (!candidates.Any(c => Edit(component, c.Namespace ?? "") is null))
                foreach (var candidate in candidates)
                    if (candidate.Namespace is { } ns)
                    {
                        namespaces.Add(ns);
                        var tags = NazorLanguage.Tags(component.Text);
                        var selected = tags.FirstOrDefault(t => t.Start == context.TagStart);
                        var opening = selected is { IsEnd: true } ? NazorLanguage.StartOf(tags, selected) : selected;
                        if (opening is not null) Qualify(ns, opening.NameStart, opening.Name.Length, tag, markup: true);
                    }
        }
        if (snapshot.Document(component) is { } document && await document.GetSemanticModelAsync(cancellation) is { } model)
        {
            foreach (var diagnostic in model.GetDiagnostics(cancellationToken: cancellation).Where(d => d.Id is "CS0246" or "CS0103"))
            {
                var span = diagnostic.Location.SourceSpan;
                if (component.ToOriginal(span.Start, span.Length) is not { } original || original.Start > end || original.Start + original.Length < start) continue;
                var name = component.Text.Substring(original.Start, original.Length);
                if (!SyntaxFacts.IsValidIdentifier(name)) continue;
                var types = await SymbolFinder.FindDeclarationsAsync(snapshot.CSharp, name, ignoreCase: false, SymbolFilter.Type, cancellation);
                foreach (var type in types.OfType<INamedTypeSymbol>().Where(t => t.ContainingType is null &&
                    (t.DeclaredAccessibility == Accessibility.Public || SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, model.Compilation.Assembly))))
                    if (!type.ContainingNamespace.IsGlobalNamespace)
                    {
                        var ns = type.ContainingNamespace.ToDisplayString();
                        namespaces.Add(ns);
                        Qualify(ns, original.Start, original.Length, name, markup: false);
                    }
            }
        }
        return [.. namespaces.Order(StringComparer.Ordinal).Select(ns => (Namespace: ns, Edit: Edit(component, ns)))
            .Where(candidate => candidate.Edit is not null).Select(candidate =>
            {
                var at = TextPositions.Offset(component.Text, candidate.Edit!.Range.Start);
                return new CodeActionResult("Add @using " + candidate.Namespace,
                    [new(component.Path, component.Text, component.Text.Insert(at, candidate.Edit.NewText))]);
            }).Concat(qualified).DistinctBy(a => a.Title)];
    }
}
