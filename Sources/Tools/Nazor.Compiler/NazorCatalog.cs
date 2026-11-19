using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;

namespace Nazor.Compiler;

public enum NazorParameterKind
{
    Value,
    EventCallback,
    ChildContent,
}

public sealed record NazorParameterInfo(string Name, string TypeName, NazorParameterKind Kind, string? Documentation)
{
    public bool IsRequired { get; init; }
}

public sealed record NazorComponentInfo(string TagName, string TypeName, string? Namespace, string? Documentation, IReadOnlyList<NazorParameterInfo> Parameters);

public sealed record NazorDirectiveAttributeInfo(string Name, string TagName, string? Documentation);

public sealed partial record NazorCatalog(IReadOnlyList<NazorComponentInfo> Components, IReadOnlyList<NazorDirectiveAttributeInfo> DirectiveAttributes)
{
    private const string AspNetComponents = "Microsoft.AspNetCore.Components.";

    public static readonly NazorCatalog Empty = new([], []);

    internal static NazorCatalog From(TagHelperCollection tagHelpers)
    {
        var components = new List<NazorComponentInfo>();
        var attributes = new Dictionary<(string, string), NazorDirectiveAttributeInfo>();
        foreach (var tagHelper in tagHelpers)
        {
            if ((tagHelper.Flags & TagHelperFlags.IsFullyQualifiedNameMatch) != 0) continue;
            switch (tagHelper.Kind)
            {
                case TagHelperKind.Component:
                    foreach (var rule in tagHelper.TagMatchingRules)
                    {
                        var parameters = tagHelper.BoundAttributes.Where(a => !a.IsDirectiveAttribute)
                            .Select(a => new NazorParameterInfo(a.Name, NazorType(a.TypeName), Kind(a.TypeName), Documentation(a.Documentation))
                            { IsRequired = (a.Flags & BoundAttributeFlags.IsEditorRequired) != 0 })
                            .OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                        components.Add(new(rule.TagName, tagHelper.TypeName, tagHelper.TypeNamespace, Documentation(tagHelper.Documentation), parameters));
                    }
                    break;
                case TagHelperKind.EventHandler or TagHelperKind.Bind or TagHelperKind.Key or TagHelperKind.Ref or TagHelperKind.Splat:
                    foreach (var rule in tagHelper.TagMatchingRules)
                    {
                        foreach (var attribute in tagHelper.BoundAttributes.Where(a => a.IsDirectiveAttribute))
                        {
                            attributes.TryAdd((attribute.Name, rule.TagName), new(attribute.Name, rule.TagName, Documentation(attribute.Documentation)));
                            foreach (var parameter in attribute.Parameters.Where(_ => tagHelper.Kind == TagHelperKind.EventHandler))
                            {
                                var name = attribute.Name + ":" + parameter.Name;
                                attributes.TryAdd((name, rule.TagName), new(name, rule.TagName, Documentation(parameter.Documentation)));
                            }
                        }
                    }
                    break;
            }
        }
        return new(components.DistinctBy(c => (c.TagName, c.TypeName)).OrderBy(c => c.TagName, StringComparer.Ordinal).ToArray(),
            attributes.Values.OrderBy(a => a.Name, StringComparer.Ordinal).ThenBy(a => a.TagName, StringComparer.Ordinal).ToArray());
    }

    public IEnumerable<NazorComponentInfo> Find(string tagName) =>
        Components.Where(c => c.TagName == tagName || c.TypeName == tagName);

    public IEnumerable<NazorDirectiveAttributeInfo> DirectiveAttributesFor(string tagName)
    {
        var binds = Find(tagName).SelectMany(component => component.Parameters
                .Where(p => p.Kind == NazorParameterKind.Value &&
                            component.Parameters.Any(c => c.Name == p.Name + "Changed" && c.Kind == NazorParameterKind.EventCallback))
                .Select(p => new NazorDirectiveAttributeInfo("@bind-" + p.Name, tagName, $"Binds {p.Name} to a value and updates it from {p.Name}Changed.")));
        return DirectiveAttributes.Where(a => (a.TagName == "*" || a.TagName == tagName) && !a.Name.EndsWith("...", StringComparison.Ordinal))
            .Concat(binds).DistinctBy(a => a.Name).OrderBy(a => a.Name, StringComparer.Ordinal);
    }

    private static string NazorType(string typeName) => KeywordTypes().Replace(typeName.Replace("global::", "", StringComparison.Ordinal)
        .Replace(AspNetComponents, "Nazor.", StringComparison.Ordinal), match => match.Groups["name"].Value switch
        {
            "String" => "string", "Boolean" => "bool", "Int32" => "int", "Int64" => "long", "Double" => "double",
            "Single" => "float", "Decimal" => "decimal", "Object" => "object", "Char" => "char", "Byte" => "byte",
            var other => "System." + other,
        });

    [GeneratedRegex(@"\bSystem\.(?<name>String|Boolean|Int32|Int64|Double|Single|Decimal|Object|Char|Byte)\b(?!\.)")]
    private static partial Regex KeywordTypes();

    private static NazorParameterKind Kind(string typeName)
    {
        var name = typeName.Replace("global::", "", StringComparison.Ordinal);
        if (name.StartsWith(AspNetComponents + "EventCallback", StringComparison.Ordinal)) return NazorParameterKind.EventCallback;
        if (name.StartsWith(AspNetComponents + "RenderFragment", StringComparison.Ordinal)) return NazorParameterKind.ChildContent;
        return NazorParameterKind.Value;
    }

    private static string? Documentation(string? documentation) => string.IsNullOrWhiteSpace(documentation) ? null : documentation.Trim();
}
