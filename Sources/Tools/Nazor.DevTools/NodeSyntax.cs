using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

/// <summary>A captured node to navigate to: a component or a physical element/text, by instance ID.</summary>
public sealed record NodeLink(string Kind, string Id);

/// <summary>A run of text in a tree row, link or breadcrumb, styled by its syntax class.</summary>
public readonly record struct SyntaxToken(string Text, string Class);

/// <summary>
/// Formats captured nodes the way browser tools show markup: elements as tags with their attributes, texts as quoted
/// strings and components by their simple type name and key. Values are shortened for display only.
/// </summary>
public static class NodeSyntax
{
    private const int MaxAttributes = 4;
    private const int MaxValue = 40;

    /// <summary>The type name as code writes it, without namespace or assembly-qualified generic arguments.</summary>
    public static string ShortName(string typeName)
    {
        var tick = typeName.IndexOf('`', StringComparison.Ordinal);
        var name = tick < 0 ? typeName : typeName[..tick];
        name = name[(name.LastIndexOf('.') + 1)..];
        return tick < 0 ? name : name + "<…>";
    }

    public static IEnumerable<SyntaxToken> Component(ComponentInfo component, bool withInstance = true)
    {
        yield return new(ShortName(component.TypeName), component.IsFramework ? "syntax-framework" : "syntax-component");
        if (component.Key is { } key)
        {
            yield return new(" key", "syntax-attribute");
            yield return new("=", "syntax-punctuation");
            yield return new("\"" + Shorten(key) + "\"", "syntax-value");
        }
        if (withInstance) yield return new(" #" + component.Id, "syntax-meta");
    }

    public static IEnumerable<SyntaxToken> Element(ElementInfo element, bool withAttributes = true)
    {
        if (element.Kind == "text")
        {
            yield return new("\"" + Shorten(element.Text ?? "") + "\"", "syntax-text");
            yield break;
        }
        yield return new("<", "syntax-punctuation");
        yield return new(element.Name, "syntax-tag");
        if (withAttributes)
            foreach (var (name, value) in Attributes(element).Take(MaxAttributes))
            {
                yield return new(" " + name, "syntax-attribute");
                yield return new("=", "syntax-punctuation");
                yield return new("\"" + Shorten(value) + "\"", "syntax-value");
            }
        if (withAttributes && Attributes(element).Skip(MaxAttributes).Any()) yield return new(" …", "syntax-punctuation");
        yield return new(">", "syntax-punctuation");
    }

    /// <summary>A compact selector-like label, such as <c>Button#save.primary</c>, used by breadcrumbs.</summary>
    public static string Selector(ElementInfo element) => element.Kind == "text" ? "#text" :
        element.Name + (element.ElementId is { Length: > 0 } id ? "#" + id : "") + string.Concat((element.Classes ?? []).Take(2).Select(c => "." + c));

    // Markup ID and classes first, as authors write them, then the remaining captured attributes in capture order.
    private static IEnumerable<(string Name, string Value)> Attributes(ElementInfo element)
    {
        if (element.ElementId is { Length: > 0 } elementId) yield return ("id", elementId);
        if (element.Classes is { Length: > 0 } classes) yield return ("class", string.Join(' ', classes));
        foreach (var attribute in element.Attributes ?? [])
            if (attribute.Name is not ("id" or "class")) yield return (attribute.Name, attribute.Value);
    }

    private static string Shorten(string value)
    {
        var line = value.ReplaceLineEndings(" ");
        return line.Length <= MaxValue ? line : line[..MaxValue] + "…";
    }
}
