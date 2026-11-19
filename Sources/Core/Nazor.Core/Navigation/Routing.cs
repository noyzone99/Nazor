using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Nazor.Routing;

/// <summary>
/// A page found for an address, with the values of its route parameters (converted to their constraint).
/// </summary>
/// <param name="Page">The page (Type).</param>
/// <param name="Template">The template (string).</param>
/// <param name="Parameters">The parameters (IReadOnlyDictionary&lt;string, object?&gt;).</param>
public sealed record RouteMatch(Type Page, string Template, IReadOnlyDictionary<string, object?> Parameters);

/// <summary>
/// The pages of some assemblies (types with <see cref="RouteAttribute" />) and the matching of addresses.
/// </summary>
public sealed class RouteTable
{
    private static readonly ConcurrentDictionary<Assembly, Route[]> Cache = new();
    private readonly Route[] routes;

    private RouteTable(Route[] routes) => this.routes = routes;

    /// <summary>The routes of the pages in <paramref name="assemblies" />, the most specific first.</summary>
    /// <param name="assemblies">The assemblies (IEnumerable&lt;Assembly&gt;).</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public static RouteTable Create(IEnumerable<Assembly> assemblies) =>
        new([.. assemblies.Distinct().SelectMany(assembly => Cache.GetOrAdd(assembly, Scan)).OrderByDescending(route => route.Precedence)]);

    /// <summary>The routes of explicit pages, for tests and for pages outside the assemblies.</summary>
    /// <param name="pages">The pages (IEnumerable&lt;Type&gt;).</param>
    /// <returns>The event callback associated with the receiver.</returns>
    public static RouteTable Create(IEnumerable<Type> pages) => new([.. pages.SelectMany(Routes).OrderByDescending(route => route.Precedence)]);

    /// <summary>The templates known, the most specific first.</summary>
    public IEnumerable<string> Templates => routes.Select(route => route.Template);

    /// <summary>The page of <paramref name="uri" /> (its path; the query is ignored), or null.</summary>
    /// <param name="uri">The target address or image URI.</param>
    /// <returns>The resulting RouteMatch? value.</returns>
    public RouteMatch? Match(string uri)
    {
        var path = uri.Split('?', '#')[0];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        foreach (var route in routes)
            if (route.TryMatch(segments, out var values)) return new RouteMatch(route.Page, route.Template, values);
        return null;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Nazor.Sdk preserves every IComponent in application references, including pages reached only through routes.")]
    private static Route[] Scan(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = [.. exception.Types.OfType<Type>()];
        }
        return [.. types.Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract).SelectMany(Routes)];
    }

    private static IEnumerable<Route> Routes(Type page) => page.GetCustomAttributes<RouteAttribute>(inherit: false).Select(route => Route.Parse(page, route.Template));
}

internal sealed class Route
{
    private readonly Segment[] segments;

    private Route(Type page, string template, Segment[] segments)
    {
        Page = page;
        Template = template;
        this.segments = segments;

        Precedence = segments.Sum(segment => segment.Parameter is null ? 100 : segment.Constraint is null ? 1 : 10) * 10 + segments.Length;
    }

    public Type Page { get; }
    public string Template { get; }
    public int Precedence { get; }

    public static Route Parse(Type page, string template)
    {
        var parts = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segments = new Segment[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (!part.StartsWith('{') || !part.EndsWith('}'))
            {
                segments[i] = new Segment(part, null, null, false);
                continue;
            }
            var inner = part[1..^1];
            var optional = inner.EndsWith('?');
            if (optional) inner = inner[..^1];
            if (optional && i != parts.Length - 1) throw new FormatException($"Only the last parameter of '{template}' can be optional.");
            var colon = inner.IndexOf(':', StringComparison.Ordinal);
            var (name, constraint) = colon < 0 ? (inner, null) : (inner[..colon], inner[(colon + 1)..].ToLowerInvariant());
            if (name.Length == 0) throw new FormatException($"A parameter of '{template}' has no name.");
            if (constraint is not (null or "int" or "long" or "bool" or "guid" or "decimal" or "double" or "datetime"))
                throw new FormatException($"Unknown constraint '{constraint}' in '{template}'.");
            segments[i] = new Segment(null, name, constraint, optional);
        }
        return new Route(page, template, segments);
    }

    public bool TryMatch(string[] path, out IReadOnlyDictionary<string, object?> values)
    {
        var found = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        values = found;
        var required = segments.Length > 0 && segments[^1].Optional ? segments.Length - 1 : segments.Length;
        if (path.Length < required || path.Length > segments.Length) return false;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (i >= path.Length)
            {
                found[segment.Parameter!] = null;
                continue;
            }
            if (segment.Literal is { } literal)
            {
                if (!string.Equals(literal, path[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            else if (Convert(path[i], segment.Constraint) is { } value) found[segment.Parameter!] = value;
            else return false;
        }
        return true;
    }

    private static object? Convert(string text, string? constraint)
    {
        var culture = CultureInfo.InvariantCulture;
        return constraint switch
        {
            null => text,
            "int" => int.TryParse(text, NumberStyles.Integer, culture, out var i) ? i : null,
            "long" => long.TryParse(text, NumberStyles.Integer, culture, out var l) ? l : null,
            "bool" => bool.TryParse(text, out var b) ? b : null,
            "guid" => Guid.TryParse(text, out var g) ? g : null,
            "decimal" => decimal.TryParse(text, NumberStyles.Number, culture, out var m) ? m : null,
            "double" => double.TryParse(text, NumberStyles.Float, culture, out var d) ? d : null,
            _ => DateTime.TryParse(text, culture, DateTimeStyles.None, out var t) ? t : null,
        };
    }

    private readonly record struct Segment(string? Literal, string? Parameter, string? Constraint, bool Optional);
}
