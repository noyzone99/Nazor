using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static class ApiSignatures
{
    private static readonly SymbolDisplayFormat Display = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeRef,
        delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeExtensionThis,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
        kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword | SymbolDisplayKindOptions.IncludeMemberKeyword,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static Dictionary<string, HashSet<string>> Read(string markdown)
    {
        var types = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var headings = Regex.Matches(markdown, @"(?m)^#{2,3} `(?<name>[^`]+)`[ \t]*\r?\n(?:[ \t]*\r?\n)*Namespace[: ]");
        for (var index = 0; index < headings.Count; index++)
        {
            var heading = headings[index];
            var name = heading.Groups["name"].Value;
            if (!types.TryGetValue(name, out var signatures)) types.Add(name, signatures = new(StringComparer.Ordinal));
            var end = index + 1 < headings.Count ? headings[index + 1].Index : markdown.Length;
            foreach (var line in markdown[heading.Index..end].Split('\n'))
            {
                var table = Regex.Match(line, @"^\| `(.+?)` \|");
                signatures.Add(Normalize(table.Success ? table.Groups[1].Value : line));
            }
        }
        return types;
    }

    public static string Normalize(string text)
    {
        text = Regex.Replace(text.Trim().Trim('`').TrimEnd(';'), @"(?<!protected )\b(?:internal|private) (?:get|set|init);", "");
        if (Regex.IsMatch(text, @"\{\s*get;\s*\}$")) text = text.Replace("readonly ", "", StringComparison.Ordinal);
        text = Regex.Replace(text, @"\bdefault\([\w.]+\)", "default");
        return Regex.Replace(text.Replace("\\|", "|", StringComparison.Ordinal), @"\s+", "");
    }

    public static IEnumerable<(string Type, string Signature)> From(INamespaceSymbol @namespace) =>
        @namespace.GetTypeMembers().Where(Visible).SelectMany(From)
            .Concat(@namespace.GetNamespaceMembers().SelectMany(From));

    private static IEnumerable<(string Type, string Signature)> From(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters));
        yield return (name, TypeDeclaration(type));
        if (type.TypeKind != TypeKind.Delegate)
        {
            foreach (var member in type.GetMembers().Where(Visible))
            {
                if (member is INamedTypeSymbol || member.Name.StartsWith('<')) continue;
                if (member is IMethodSymbol method && (type.TypeKind == TypeKind.Enum || method.AssociatedSymbol is not null)) continue;
                yield return (name, MemberDeclaration(type, member));
            }
        }
        foreach (var nested in type.GetTypeMembers().Where(Visible).SelectMany(From)) yield return nested;
    }

    private static string TypeDeclaration(INamedTypeSymbol type)
    {
        var signature = type.ToDisplayString(Display);
        if (type.TypeKind == TypeKind.Delegate) return signature;
        var accessibility = type.DeclaredAccessibility == Accessibility.Public ? "public " : "protected ";
        if (!signature.StartsWith(accessibility, StringComparison.Ordinal))
        {
            var modifier = type.IsStatic ? "static " : type.IsReadOnly ? "readonly " : type.IsRefLikeType ? "ref " :
                type.TypeKind == TypeKind.Class && type.IsAbstract ? "abstract " : type.TypeKind == TypeKind.Class && type.IsSealed ? "sealed " : "";
            signature = accessibility + (signature.StartsWith(modifier, StringComparison.Ordinal) ? "" : modifier) + signature;
        }
        if (type.IsRecord) signature = Regex.Replace(signature, @"\b(class|struct)\b", "record $1");
        var where = signature.IndexOf(" where ", StringComparison.Ordinal);
        var parents = (type.BaseType is { SpecialType: SpecialType.None } parent ? new[] { parent } : []).Concat(type.Interfaces)
            .Select(item => item.ToDisplayString(Display.WithKindOptions(SymbolDisplayKindOptions.None).WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters))).ToArray();
        return parents.Length == 0 ? signature : signature.Insert(where < 0 ? signature.Length : where, " : " + string.Join(", ", parents));
    }

    private static string MemberDeclaration(INamedTypeSymbol type, ISymbol member)
    {
        var signature = member.ToDisplayString(Display);
        var parameters = member switch { IMethodSymbol method => method.Parameters, IPropertySymbol property => property.Parameters, _ => [] };
        foreach (var parameter in parameters)
        {
            if (!parameter.HasExplicitDefaultValue || parameter.ExplicitDefaultValue is null) continue;
            var parameterType = parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : parameter.Type;
            if (parameterType is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType) continue;
            var parameterName = Regex.Escape(parameter.Name);
            signature = Regex.Replace(signature, $@"(\b{parameterName}\s*=\s*)([A-Za-z_][A-Za-z_0-9]*)\b(?![.])",
                "$1" + enumType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + ".$2");
        }
        if (member is not IFieldSymbol { HasConstantValue: true } field) return signature;
        var value = field.ConstantValue is null ? "null" : SymbolDisplay.FormatPrimitive(field.ConstantValue, quoteStrings: true, useHexadecimalNumbers: false);
        return (type.TypeKind == TypeKind.Enum ? field.Name : signature) + " = " + value;
    }

    private static bool Visible(ISymbol member) => member.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
}
