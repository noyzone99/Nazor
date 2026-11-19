using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nazor.Compiler;

// Keep the existing reflection contract, including C# components, inherited private injection and generic controls.
internal static class ComponentMetadata
{
    internal static string Generate(IEnumerable<string> paths)
    {
        var references = paths.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).Select(Path.GetFullPath)
            .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        var compilation = CSharpCompilation.Create("Nazor.ComponentMetadata", references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All));
        var root = new XElement("linker");
        var values = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) continue;
            var types = Types(assembly.GlobalNamespace).ToArray();
            // A generic [Parameter] can hide a struct constructor from the property signature.
            foreach (var value in types.Where(t => t.TypeKind == TypeKind.Struct && !t.IsRefLikeType && t.SpecialType == SpecialType.None &&
                         t.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared && c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)))
                values.TryAdd(assembly.Name + ":" + Name(value), value);
            var components = types.Where(type => type.AllInterfaces.Any(i => i.ToDisplayString() == "Nazor.IComponent") ||
                type.GetMembers().OfType<IPropertySymbol>().Any(p => p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() is
                    "Nazor.ParameterAttribute" or "Nazor.CascadingParameterAttribute" or "Nazor.InjectAttribute"))).ToArray();
            if (components.Length == 0) continue;
            var element = new XElement("assembly", new XAttribute("fullname", assembly.Name));
            foreach (var type in components.OrderBy(Name, StringComparer.Ordinal))
            {
                element.Add(new XElement("type", new XAttribute("fullname", Name(type)), new XAttribute("preserve", "all")));
            }
            root.Add(element);
        }
        foreach (var value in values.Values.OrderBy(v => v.ContainingAssembly.Name + ":" + Name(v), StringComparer.Ordinal))
        {
            var assembly = root.Elements("assembly").FirstOrDefault(a => (string?)a.Attribute("fullname") == value.ContainingAssembly.Name);
            if (assembly is null) { assembly = new XElement("assembly", new XAttribute("fullname", value.ContainingAssembly.Name)); root.Add(assembly); }
            if (!assembly.Elements("type").Any(t => (string?)t.Attribute("fullname") == Name(value)))
                assembly.Add(new XElement("type", new XAttribute("fullname", Name(value)), new XAttribute("preserve", "nothing"), new XElement("method", new XAttribute("signature", "System.Void .ctor()"))));
        }
        return root.ToString();
    }

    private static string Name(INamedTypeSymbol type) => type.ContainingType is { } parent
        ? Name(parent) + "/" + type.MetadataName
        : (type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + ".") + type.MetadataName;

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol container)
    {
        foreach (var type in container.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in Types(type)) yield return nested;
        }
        if (container is INamespaceSymbol ns)
            foreach (var child in ns.GetNamespaceMembers())
                foreach (var type in Types(child)) yield return type;
    }
}
