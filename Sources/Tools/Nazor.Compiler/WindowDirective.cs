using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nazor.Compiler;

internal sealed record WindowDirective(int Offset, int ParametersOffset, ParameterListSyntax Parameters)
{
    internal static (string Text, WindowDirective? Window) Extract(string source, string path, ICollection<NazorDiagnostic> diagnostics)
    {
        var masked = source.ToCharArray();
        WindowDirective? window = null;
        var text = SourceText.From(source);
        foreach (var line in text.Lines)
        {
            var value = line.ToString();
            var start = value.Length - value.TrimStart().Length;
            if (!value.AsSpan(start).StartsWith("@window", StringComparison.Ordinal) ||
                value.Length > start + 7 && SyntaxFacts.IsIdentifierPartCharacter(value[start + 7])) continue;
            var offset = line.Start + start;
            var context = RazorSdkAdapter.ClassifyDirective(new string(masked, 0, offset + 7), source[(offset + 7)..], path, offset);
            if (context == StyleDirectiveContext.None) continue;
            void Error(string message) => diagnostics.Add(new("NZC030", NazorDiagnosticSeverity.Error, path, line.LineNumber + 1, start + 1, message));
            if (context != StyleDirectiveContext.TopLevel || Path.GetFileName(path).Equals("_Imports.nazor", StringComparison.OrdinalIgnoreCase))
                Error("@window must appear at file top level and cannot be imported.");
            if (window is not null) Error("A component can declare @window only once.");
            var rest = value[(start + 7)..].Trim();
            var parameters = SyntaxFactory.ParseParameterList(rest.Length == 0 ? "()" : rest);
            if (rest.Length != 0 && (!rest.StartsWith('(') || !rest.EndsWith(')')) || parameters.ContainsDiagnostics ||
                parameters.Parameters.Any(p => p.Type is null || p.Modifiers.Count != 0 || p.AttributeLists.Count != 0))
                Error("Use @window or @window (Type Name, Type OptionalName = defaultValue). Configure the title with Window.Title.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Window" };
            foreach (var parameter in parameters.Parameters)
                if (!names.Add(parameter.Identifier.ValueText)) Error($"Duplicate or reserved window parameter '{parameter.Identifier.ValueText}'.");
            window ??= new(offset, rest.Length == 0 ? offset + 7 : line.Start + value.IndexOf(rest, start + 7, StringComparison.Ordinal), parameters);
            for (var i = offset; i < line.End; i++) masked[i] = ' ';
        }
        if (window is not null)
            foreach (var line in text.Lines)
            {
                var value = line.ToString().TrimStart();
                if (!value.StartsWith("@inherits ", StringComparison.Ordinal)) continue;
                var offset = line.End - value.Length;
                if (RazorSdkAdapter.ClassifyDirective(source[..(offset + 9)], source[(offset + 9)..], path, offset) != StyleDirectiveContext.None)
                    diagnostics.Add(new("NZC030", NazorDiagnosticSeverity.Error, path, line.LineNumber + 1, 1, "@window supplies its own base class and cannot be combined with @inherits."));
            }
        return (new string(masked), window);
    }

    internal string Apply(string code, PositionMap? map = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var component = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.ValueText != "TypeInference");
        var changes = new List<TextChange>();
        if (component.BaseList is { Types.Count: > 0 } bases)
            changes.Add(new(bases.Types[0].Type.Span, "global::Nazor.WindowComponentBase"));
        foreach (var method in component.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "BuildRenderTree"))
            changes.Add(new(method.Identifier.Span, "BuildWindowContent"));
        var properties = string.Concat(Parameters.Parameters.Select(p =>
            $"\n[global::Nazor.Parameter]{(p.Default is null ? "[global::Nazor.EditorRequired]" : "")} public {p.Type} {p.Identifier} {{ get; set; }} = {(p.Default is null ? "default!" : p.Default.Value.ToString())};\n"));
        changes.Add(new(new TextSpan(component.CloseBraceToken.SpanStart, 0), "\n#nullable enable\n" + properties + "\n#nullable restore\n"));
        map?.Then(changes);
        return tree.GetText().WithChanges(changes).ToString();
    }

    internal IEnumerable<NazorSourceMapping> Mappings(string generated)
    {
        foreach (var parameter in Parameters.Parameters)
        {
            var signature = $"public {parameter.Type} {parameter.Identifier} {{ get; set; }}";
            var index = generated.LastIndexOf(signature, StringComparison.Ordinal);
            if (index < 0) continue;
            yield return new(ParametersOffset + parameter.Identifier.SpanStart,
                index + "public ".Length + parameter.Type!.ToString().Length + 1, parameter.Identifier.Span.Length);
            yield return new(ParametersOffset + parameter.Type.SpanStart, index + "public ".Length, parameter.Type.Span.Length);
            if (parameter.Default is { } value)
                yield return new(ParametersOffset + value.Value.SpanStart, index + signature.Length + 3, value.Value.Span.Length);
        }
    }
}
