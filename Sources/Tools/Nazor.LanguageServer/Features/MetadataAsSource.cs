using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nazor.LanguageServer.Protocol;

namespace Nazor.LanguageServer.Features;

internal static class MetadataAsSource
{
    private static readonly SymbolDisplayFormat Format = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeConstantValue | SymbolDisplayMemberOptions.IncludeRef |
            SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeExtensionThis,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays);

    public static string Folder { get; set; } = Path.Combine(Path.GetTempPath(), "Nazor", "MetadataAsSource");

    public static FileRange? Locate(ISymbol symbol, Compilation compilation)
    {
        symbol = Definition(symbol);
        if (!symbol.Locations.Any(l => l.IsInMetadata)) return null;
        var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        while (type?.ContainingType is { } outer) type = outer;
        if (type is null) return null;

        var writer = new Writer();
        var text = writer.File(type, compilation.GetMetadataReference(type.ContainingAssembly) is PortableExecutableReference { FilePath: { } file } ? file : null);
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } containing ? containing.ToDisplayString() : "global";
        var path = Path.Combine(Folder, Safe(type.ContainingAssembly.Name), Safe(ns), Safe(type.MetadataName) + ".cs");
        Save(path, text);
        var (start, length) = writer.Names.TryGetValue(symbol, out var place) ? place : writer.Names[type];
        return new(path, TextPositions.Range(text, start, start + length));
    }

    private static string Safe(string name) => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    private static ISymbol Definition(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { ReducedFrom: { } reduced } => reduced.OriginalDefinition,
        IMethodSymbol { AssociatedSymbol: { } associated } => associated.OriginalDefinition,
        IParameterSymbol or ITypeParameterSymbol => Definition(symbol.ContainingSymbol),
        _ => symbol.OriginalDefinition,
    };

    private static void Save(string path, string text)
    {
        try
        {
            if (File.Exists(path))
            {
                if (File.ReadAllText(path) == text) return;
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, text);
            File.Move(temporary, path, overwrite: true);
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (!File.Exists(path)) throw;
        }
    }

    private sealed class Writer
    {
        private readonly StringBuilder body = new();
        private readonly HashSet<string> namespaces = new(StringComparer.Ordinal);
        private int indent;

        public Dictionary<ISymbol, (int Start, int Length)> Names { get; } = new(SymbolEqualityComparer.Default);

        public string File(INamedTypeSymbol type, string? assemblyPath)
        {
            var ns = type.ContainingNamespace is { IsGlobalNamespace: false } containing ? containing.ToDisplayString() : null;
            Type(type);
            var header = new StringBuilder();
            header.Append("#region Assembly ").Append(type.ContainingAssembly.Identity.GetDisplayName()).Append('\n');
            if (assemblyPath is not null) header.Append("// ").Append(assemblyPath).Append('\n');
            header.Append("#endregion\n\n");
            var usings = namespaces.Where(n => n != ns).Order(StringComparer.Ordinal).ToList();
            foreach (var name in usings.Where(n => n == "System" || n.StartsWith("System.", StringComparison.Ordinal)).Concat(usings.Where(n => n != "System" && !n.StartsWith("System.", StringComparison.Ordinal))))
                header.Append("using ").Append(name).Append(";\n");
            if (usings.Count > 0) header.Append('\n');
            if (ns is not null) header.Append("namespace ").Append(ns).Append(";\n\n");
            foreach (var key in Names.Keys.ToList()) Names[key] = (Names[key].Start + header.Length, Names[key].Length);
            return header.Append(body).ToString();
        }

        private void Type(INamedTypeSymbol type)
        {
            Documentation(type);
            Attributes(type);
            Line();
            body.Append(Accessibility(type.DeclaredAccessibility));
            if (type.TypeKind == TypeKind.Delegate && type.DelegateInvokeMethod is { } invoke)
            {
                body.Append("delegate ").Append(Display(invoke.ReturnType)).Append(' ');
                Name(type, type.Name);
                body.Append(TypeParameters(type)).Append('(').Append(string.Join(", ", invoke.Parameters.Select(Display))).Append(");\n");
                return;
            }
            if (type.TypeKind == TypeKind.Class && type.IsStatic) body.Append("static ");
            else if (type.TypeKind == TypeKind.Class && type.IsAbstract) body.Append("abstract ");
            else if (type.TypeKind == TypeKind.Class && type.IsSealed) body.Append("sealed ");
            if (type.IsReadOnly && type.TypeKind == TypeKind.Struct) body.Append("readonly ");
            body.Append(type.TypeKind switch
            {
                TypeKind.Interface => "interface ",
                TypeKind.Struct => type.IsRecord ? "record struct " : "struct ",
                TypeKind.Enum => "enum ",
                _ => type.IsRecord ? "record " : "class ",
            });
            Name(type, type.Name);
            body.Append(TypeParameters(type));
            var bases = new List<ITypeSymbol>();
            if (type.TypeKind == TypeKind.Enum && type.EnumUnderlyingType is { SpecialType: not SpecialType.System_Int32 } underlying) bases.Add(underlying);
            if (type.TypeKind == TypeKind.Class && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType && !type.IsRecord) bases.Add(baseType);
            if (type.TypeKind != TypeKind.Enum) bases.AddRange(type.Interfaces);
            if (bases.Count > 0) body.Append(" : ").Append(string.Join(", ", bases.Select(Display)));
            foreach (var parameter in type.TypeParameters) Constraints(parameter);
            body.Append('\n');
            Line();
            body.Append("{\n");
            indent++;
            var members = type.GetMembers().Where(Visible).ToList();
            var first = true;
            if (type.TypeKind == TypeKind.Enum)
            {
                foreach (var field in members.OfType<IFieldSymbol>().Where(f => f.HasConstantValue))
                {
                    Documentation(field);
                    Line();
                    Name(field, field.Name);
                    body.Append(" = ").Append(field.ConstantValue).Append(",\n");
                }
            }
            else
            {
                foreach (var group in new IEnumerable<ISymbol>[]
                {
                    members.OfType<IFieldSymbol>().OrderByDescending(f => f.IsConst),
                    members.OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor),
                    members.OfType<IPropertySymbol>(),
                    members.OfType<IEventSymbol>(),
                    members.OfType<IMethodSymbol>().Where(m => m.MethodKind is MethodKind.Ordinary),
                    members.OfType<IMethodSymbol>().Where(m => m.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion),
                })
                {
                    foreach (var member in group)
                    {
                        if (!first) body.Append('\n');
                        first = false;
                        Member(member);
                    }
                }
                foreach (var nested in members.OfType<INamedTypeSymbol>())
                {
                    if (!first) body.Append('\n');
                    first = false;
                    Type(nested);
                }
            }
            indent--;
            Line();
            body.Append("}\n");
        }

        private void Member(ISymbol member)
        {
            Documentation(member);
            Attributes(member);
            Line();
            var start = body.Length;
            var parts = member.ToDisplayParts(Format);
            foreach (var part in parts)
            {
                if (part.Symbol is ITypeSymbol type) Collect(type);
                if (part.Kind is SymbolDisplayPartKind.MethodName or SymbolDisplayPartKind.PropertyName or SymbolDisplayPartKind.FieldName or
                        SymbolDisplayPartKind.EventName or SymbolDisplayPartKind.ConstantName or SymbolDisplayPartKind.ClassName or SymbolDisplayPartKind.StructName or SymbolDisplayPartKind.Keyword &&
                    SymbolEqualityComparer.Default.Equals(part.Symbol, member) && !Names.ContainsKey(member))
                    Names[member] = (body.Length, part.ToString().Length);
                body.Append(part.ToString());
            }
            if (!Names.ContainsKey(member)) Names[member] = (start, body.Length - start);
            if (member is not IPropertySymbol) body.Append(';');
            body.Append('\n');
        }

        private void Name(ISymbol symbol, string name)
        {
            Names[symbol] = (body.Length, name.Length);
            body.Append(name);
        }

        private string Display(ITypeSymbol type)
        {
            Collect(type);
            return type.ToDisplayString(Format);
        }

        private string Display(IParameterSymbol parameter)
        {
            Collect(parameter.Type);
            return parameter.ToDisplayString(Format);
        }

        private string TypeParameters(INamedTypeSymbol type) => type.TypeParameters.Length == 0 ? "" :
            "<" + string.Join(", ", type.TypeParameters.Select(p => (p.Variance switch { VarianceKind.In => "in ", VarianceKind.Out => "out ", _ => "" }) + p.Name)) + ">";

        private void Constraints(ITypeParameterSymbol parameter)
        {
            var constraints = new List<string>();
            if (parameter.HasReferenceTypeConstraint) constraints.Add("class");
            if (parameter.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
            else if (parameter.HasValueTypeConstraint) constraints.Add("struct");
            if (parameter.HasNotNullConstraint) constraints.Add("notnull");
            constraints.AddRange(parameter.ConstraintTypes.Select(Display));
            if (parameter.HasConstructorConstraint) constraints.Add("new()");
            if (constraints.Count > 0) body.Append(" where ").Append(parameter.Name).Append(" : ").Append(string.Join(", ", constraints));
        }

        private void Collect(ITypeSymbol type)
        {
            switch (type)
            {
                case INamedTypeSymbol named:
                    if (named.SpecialType == SpecialType.None && named.ContainingNamespace is { IsGlobalNamespace: false } ns) namespaces.Add(ns.ToDisplayString());
                    foreach (var argument in named.TypeArguments) Collect(argument);
                    break;
                case IArrayTypeSymbol array:
                    Collect(array.ElementType);
                    break;
                case IPointerTypeSymbol pointer:
                    Collect(pointer.PointedAtType);
                    break;
            }
        }

        private void Attributes(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is not { } type || type.ContainingNamespace?.ToDisplayString() is "System.Runtime.CompilerServices" or "System.Diagnostics" or "System.Reflection" or "System.Diagnostics.CodeAnalysis")
                    continue;
                Collect(type);
                var name = type.Name.EndsWith("Attribute", StringComparison.Ordinal) ? type.Name[..^"Attribute".Length] : type.Name;
                var arguments = attribute.ConstructorArguments.Select(a => a.ToCSharpString())
                    .Concat(attribute.NamedArguments.Select(a => $"{a.Key} = {a.Value.ToCSharpString()}")).ToList();
                Line();
                body.Append('[').Append(name);
                if (arguments.Count > 0) body.Append('(').Append(string.Join(", ", arguments)).Append(')');
                body.Append("]\n");
            }
        }

        private void Documentation(ISymbol symbol)
        {
            var xml = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) return;
            XElement member;
            try
            {
                member = XElement.Parse(xml);
            }
            catch (System.Xml.XmlException)
            {
                return;
            }
            foreach (var element in member.Elements())
            {
                foreach (var line in element.ToString(SaveOptions.DisableFormatting).Replace("\r\n", "\n").Split('\n'))
                {
                    Line();
                    body.Append("/// ").Append(line.Trim()).Append('\n');
                }
            }
        }

        private void Line() => body.Append(' ', indent * 4);

        private static bool Visible(ISymbol member) =>
            !member.IsImplicitlyDeclared && member.DeclaredAccessibility is Microsoft.CodeAnalysis.Accessibility.Public or Microsoft.CodeAnalysis.Accessibility.Protected or
                Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal &&
            member is not IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise or MethodKind.StaticConstructor or MethodKind.Destructor };

        private static string Accessibility(Microsoft.CodeAnalysis.Accessibility accessibility) => accessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.Public => "public ",
            Microsoft.CodeAnalysis.Accessibility.Protected => "protected ",
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => "protected internal ",
            _ => "",
        };
    }
}
