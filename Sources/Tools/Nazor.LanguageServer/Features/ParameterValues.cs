using Microsoft.CodeAnalysis;
using Nazor.LanguageServer.Protocol;
using Nazor.LanguageServer.Workspace;

namespace Nazor.LanguageServer.Features;

internal static class ParameterValues
{
    public static async Task<List<CompletionItem>> CompleteAsync(ProjectSnapshot snapshot, ComponentDocument component,
        NazorContext context, int offset, CancellationToken cancellation)
    {
        if (context.Kind != NazorContextKind.AttributeValue || context.Attribute is null) return [];
        var value = component.Text[context.Start..context.End];
        var escaped = value.StartsWith('@');
        if (value.Any(c => !char.IsLetterOrDigit(c) && c is not ('_' or '.' or '@')) || value.Count(c => c == '@') > (escaped ? 1 : 0)) return [];
        var candidates = snapshot.Analysis.Catalog.Find(context.Tag!).ToArray();
        if (candidates.Length != 1 || await snapshot.CSharp.GetCompilationAsync(cancellation) is not { } compilation) return [];
        var name = candidates[0].TypeName;
        if (name.IndexOf('<') is var generic && generic >= 0)
            name = name[..generic] + "`" + (name[generic..].Count(c => c == ',') + 1);
        if (compilation.GetTypeByMetadataName(name) is not { } target) return [];
        IPropertySymbol? parameter = null;
        for (var type = target; type is not null && parameter is null; type = type.BaseType)
            parameter = type.GetMembers(context.Attribute).OfType<IPropertySymbol>().FirstOrDefault();
        if (parameter is null) return [];
        var parameterType = parameter.Type;
        if (parameterType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            parameterType = nullable.TypeArguments[0];
        var start = context.Start + (escaped ? 1 : 0);
        if (offset < start) return [];
        var range = TextPositions.Range(component.Text, start, context.End);
        if (parameterType.SpecialType == SpecialType.System_Boolean)
            return [.. new[] { "true", "false" }.Select(label => new CompletionItem(label, CompletionItemKind.Keyword)
            {
                TextEdit = new(range, label), Detail = parameter.Type.ToDisplayString(), SortText = "0000_" + label,
            })];
        var valueTypes = parameterType.TypeKind == TypeKind.Enum ? new[] { parameterType } :
            parameterType.GetMembers("op_Implicit").OfType<IMethodSymbol>()
                .Where(m => m.Parameters.Length == 1 && m.Parameters[0].Type.TypeKind == TypeKind.Enum && SymbolEqualityComparer.Default.Equals(m.ReturnType, parameterType))
                .Select(m => m.Parameters[0].Type).ToArray();
        return [.. valueTypes.SelectMany(valueType =>
        {
            return valueType.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue && !f.IsImplicitlyDeclared)
                .Select(field => new CompletionItem(valueType.Name + "." + field.Name, 20  )
                {
                    TextEdit = new(range, valueType.Name + "." + field.Name),
                    Detail = parameter.Type.ToDisplayString(), SortText = "0000_" + valueType.Name + "." + field.Name,
                });
        })];
    }
}
