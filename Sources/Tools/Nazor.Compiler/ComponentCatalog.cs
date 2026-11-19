using System.Text;
using Microsoft.CodeAnalysis;

namespace Nazor.Compiler;

internal static class ComponentCatalog
{
    public const string RuntimeAssembly = "Nazor.Core";
    private const string ComponentInterface = "Nazor.IComponent";
    private const string ParameterAttribute = "Nazor.ParameterAttribute";
    private const string EventHandlerAttribute = "Nazor.EventHandlerAttribute";
    private const string AspNet = "global::Microsoft.AspNetCore.Components";

    public static HashSet<string> ComponentNames(IAssemblySymbol assembly) =>
        Types(assembly.GlobalNamespace).Where(t => t.DeclaredAccessibility == Accessibility.Public && !t.IsAbstract && t.TypeKind == TypeKind.Class && IsComponent(t))
            .Select(t => $"{t.ContainingNamespace.ToDisplayString()}.{t.Name}").ToHashSet(StringComparer.Ordinal);

    public static string CreateFacades(Compilation compilation, ICollection<NazorDiagnostic> errors)
    {
        var assemblies = new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols)
            .Where(UsesNazor).Distinct<IAssemblySymbol>(SymbolEqualityComparer.Default).ToArray();
        var types = assemblies.SelectMany(a => Types(a.GlobalNamespace)).ToArray();
        var source = new StringBuilder();
        var facadeIndex = 0;
        foreach (var component in types.Where(t => t.DeclaredAccessibility == Accessibility.Public && !t.IsAbstract && t.TypeKind == TypeKind.Class && IsComponent(t))
                     .OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal))
        {
            if (component.ContainingType is not null || component.ContainingNamespace.IsGlobalNamespace ||
                !component.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0))
            {
                errors.Add(Error(component, "NZC001",
                    $"Component '{component}' must be a public, top-level class with a namespace and a public parameterless constructor."));
                continue;
            }
            source.AppendLine($"namespace {component.ContainingNamespace.ToDisplayString()} {{");
            var typeParameters = component.TypeParameters.Length == 0 ? "" : "<" + string.Join(", ", component.TypeParameters.Select(p => "@" + p.Name)) + ">";
            var members = new StringBuilder();
            var inherited = new StringBuilder();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var type = component; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
            {
                foreach (var property in type.GetMembers().OfType<IPropertySymbol>().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!IsParameter(property) || !seen.Add(property.Name)) continue;
                    if (property.IsStatic || property.IsIndexer || property.DeclaredAccessibility != Accessibility.Public ||
                        property.SetMethod?.DeclaredAccessibility != Accessibility.Public || property.SetMethod.IsInitOnly)
                    {
                        errors.Add(Error(property, "NZC002", $"Parameter '{component}.{property.Name}' must be a public writable instance property."));
                        continue;
                    }
                    var capture = property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ParameterAttribute &&
                        a.NamedArguments.Any(n => n.Key == "CaptureUnmatchedValues" && n.Value.Value is true));
                    var target = property.Name == component.Name ? inherited : members;
                    if (IsRequired(property)) target.AppendLine($"[{AspNet}.EditorRequired]");
                    target.AppendLine($"[{AspNet}.Parameter(CaptureUnmatchedValues = {capture.ToString().ToLowerInvariant()})] public {(HasErrors(property.Type) ? "object" : DiscoveryType(property.Type))} @{property.Name} {{ get; set; }}");
                }
            }
            var baseType = AspNet + ".ComponentBase";
            if (inherited.Length > 0)
            {
                string baseName;
                do { baseName = "__NazorFacadeParameters" + facadeIndex++; }
                while (types.Any(type=>type.ContainingNamespace.ToDisplayString()==component.ContainingNamespace.ToDisplayString() && type.Name==baseName));
                source.AppendLine($"public abstract class {baseName}{typeParameters} : {baseType} {Constraints(component)} {{ {inherited} }}");
                baseType = baseName + typeParameters;
            }
            foreach (var attribute in component.GetAttributes().Where(a => a.AttributeClass?.ToDisplayString() == "Nazor.CascadingTypeParameterAttribute"))
                source.AppendLine($"[{AspNet}.CascadingTypeParameter({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((string)attribute.ConstructorArguments[0].Value!, true)})]");
            source.AppendLine($"public class @{component.Name}{typeParameters} : {baseType} {Constraints(component)} {{ {members} }} }}");
        }

        var handlers = types.SelectMany(type => type.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == EventHandlerAttribute && a.ConstructorArguments.Length >= 2)
                .Select(a => (Namespace: type.ContainingNamespace, Attribute: a)))
            .Where(h => !h.Namespace.IsGlobalNamespace && h.Attribute.ConstructorArguments[0].Value is string && h.Attribute.ConstructorArguments[1].Value is ITypeSymbol)
            .GroupBy(h => h.Namespace.ToDisplayString(), StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (var group in handlers)
        {
            source.AppendLine($"namespace {group.Key} {{");
            foreach (var (_, attribute) in group.OrderBy(h => (string)h.Attribute.ConstructorArguments[0].Value!, StringComparer.Ordinal))
            {
                var name = (string)attribute.ConstructorArguments[0].Value!;
                var argsType = ((ITypeSymbol)attribute.ConstructorArguments[1].Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var stop = attribute.ConstructorArguments.Length < 3 || attribute.ConstructorArguments[2].Value is true;
                var prevent = attribute.ConstructorArguments.Length < 4 || attribute.ConstructorArguments[3].Value is true;
                source.AppendLine($"[{AspNet}.EventHandler(\"{name}\", typeof({argsType}), {stop.ToString().ToLowerInvariant()}, {prevent.ToString().ToLowerInvariant()})]");
            }
            source.AppendLine("public static class EventHandlers { } }");
        }
        return source.ToString();
    }

    private static bool HasErrors(ITypeSymbol type) => type switch
    {
        { TypeKind: TypeKind.Error } => true,
        IArrayTypeSymbol array => HasErrors(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(HasErrors),
        _ => false,
    };

    private static string Constraints(INamedTypeSymbol component) => string.Join(" ", component.TypeParameters.Select(parameter =>
    {
        var constraints = new List<string>();
        if (parameter.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
        else if (parameter.HasValueTypeConstraint) constraints.Add("struct");
        else if (parameter.HasReferenceTypeConstraint) constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        else if (parameter.HasNotNullConstraint) constraints.Add("notnull");
        constraints.AddRange(parameter.ConstraintTypes.Select(DiscoveryType));
        if (parameter.HasConstructorConstraint) constraints.Add("new()");
        if (parameter.AllowsRefLikeType) constraints.Add("allows ref struct");
        return constraints.Count == 0 ? "" : $"where @{parameter.Name} : {string.Join(", ", constraints)}";
    }));

    private static bool UsesNazor(IAssemblySymbol assembly) =>
        assembly.Name == RuntimeAssembly || assembly.Modules.Any(m => m.ReferencedAssemblies.Any(r => r.Name == RuntimeAssembly));

    private static bool IsComponent(INamedTypeSymbol type) => type.AllInterfaces.Any(i => i.ToDisplayString() == ComponentInterface);

    private static bool IsParameter(IPropertySymbol property)
    {
        for (IPropertySymbol? current = property; current is not null; current = current.OverriddenProperty)
            if (current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ParameterAttribute)) return true;
        return false;
    }

    private static bool IsRequired(IPropertySymbol property)
    {
        for (IPropertySymbol? current = property; current is not null; current = current.OverriddenProperty)
            if (current.IsRequired || current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Nazor.EditorRequiredAttribute")) return true;
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol space) => space.GetTypeMembers()
        .SelectMany(TypeAndNested).Concat(space.GetNamespaceMembers().SelectMany(Types));

    private static IEnumerable<INamedTypeSymbol> TypeAndNested(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().SelectMany(TypeAndNested));

    private static string DiscoveryType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { ContainingNamespace: { } ns } named && ns.ToDisplayString() == "Nazor")
        {
            switch (named.Name)
            {
                case "RenderFragment" when !named.IsGenericType:
                    return $"{AspNet}.RenderFragment";
                case "RenderFragment" when named.IsGenericType:
                    return $"{AspNet}.RenderFragment<{DiscoveryType(named.TypeArguments[0])}>";
                case "EventCallback" when !named.IsGenericType:
                    return $"{AspNet}.EventCallback";
                case "EventCallback" when named.IsGenericType:
                    return $"{AspNet}.EventCallback<{DiscoveryType(named.TypeArguments[0])}>";
            }
        }
        if (type is INamedTypeSymbol { IsGenericType: true, NullableAnnotation: not NullableAnnotation.Annotated } generic && generic.TypeArguments.Any(t => t is INamedTypeSymbol))
        {
            var definition = generic.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var open = definition.IndexOf('<');
            if (open > 0 && generic.ContainingType is null)
                return $"{definition[..open]}<{string.Join(", ", generic.TypeArguments.Select(DiscoveryType))}>";
        }
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static NazorDiagnostic Error(ISymbol symbol, string id, string message)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null) return NazorDiagnostic.Error(id, message);
        var span = location.GetMappedLineSpan();
        return new(id, NazorDiagnosticSeverity.Error, span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, message);
    }
}
