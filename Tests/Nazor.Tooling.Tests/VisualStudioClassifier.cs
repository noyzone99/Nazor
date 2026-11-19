using System.Xml.Linq;

namespace Nazor.Tooling.Tests;

internal sealed class VisualStudioTheme
{
    private readonly Dictionary<string, ScopeMap> maps = new(StringComparer.Ordinal);
    private readonly List<VisualStudioTheme> includes = [];

    public string Path { get; }

    public List<string> Missing { get; } = [];

    private VisualStudioTheme(string path, Func<string, bool> classificationExists, Dictionary<string, VisualStudioTheme> loaded)
    {
        Path = path;
        loaded[System.IO.Path.GetFullPath(path)] = this;
        var root = XElement.Load(path).Element("dict")!;
        if (Value(root, "includes") is { } list)
        {
            foreach (var file in list.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
            {
                var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, file));
                includes.Add(loaded.TryGetValue(full, out var theme) ? theme : new VisualStudioTheme(full, classificationExists, loaded));
            }
        }
        foreach (var rule in Value<XElement>(root, "settings")!.Elements("dict"))
        {
            var selector = Value(rule, "scope");
            var classification = Value(Value<XElement>(rule, "settings")!, "vsclassificationtype")?.Trim();
            if (selector is null || classification is null) continue;
            if (!classificationExists(classification))
            {
                Missing.Add(classification);
                continue;
            }
            foreach (var part in selector.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var names = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var map = Map(names[^1]);
                if (names.Length == 1) map.Simple = classification;
                else
                {
                    map.Complex.Add((names[..^1], classification));
                    foreach (var ancestor in names[..^1]) Map(ancestor);
                }
            }
        }
    }

    public static VisualStudioTheme Load(string path, Func<string, bool> classificationExists) => new(path, classificationExists, []);

    public string? Classify(IReadOnlyList<string> scopes)
    {
        var stack = scopes.Select(Find).Where(m => m is not null).Select(m => m!).ToList();
        for (var i = stack.Count - 1; i >= 0; i--)
            if (stack[i].Classification(stack, i) is { } classification) return classification;
        return null;
    }

    private ScopeMap Map(string name)
    {
        if (!maps.TryGetValue(name, out var map)) maps[name] = map = new ScopeMap(this, name);
        return map;
    }

    internal ScopeMap? Find(string scope)
    {
        var name = scope;
        ScopeMap? found;
        while (!maps.TryGetValue(name, out found))
        {
            var dot = name.LastIndexOf('.');
            name = dot <= 0 ? "" : name[..dot];
            if (name.Length == 0) break;
        }
        if (name.Length != scope.Length)
        {
            foreach (var include in includes)
            {
                if (include.Find(scope) is { } other && other.Name.Length > name.Length)
                {
                    found = other;
                    name = other.Name;
                }
            }
        }
        return found;
    }

    private static string? Value(XElement dict, string key) => Value<XElement>(dict, key)?.Value;

    private static T? Value<T>(XElement dict, string key) where T : XElement =>
        dict.Elements("key").FirstOrDefault(k => k.Value == key)?.ElementsAfterSelf().FirstOrDefault() as T;

    internal sealed class ScopeMap(VisualStudioTheme theme, string name)
    {
        public string Name { get; } = name;
        public string? Simple { get; set; }
        public List<(string[] Ancestors, string Classification)> Complex { get; } = [];

        private ScopeMap? Parent
        {
            get
            {
                var dot = Name.LastIndexOf('.');
                return dot <= 0 ? null : theme.Find(Name[..dot]);
            }
        }

        public bool IsAncestorOf(ScopeMap? other)
        {
            for (; other is not null; other = other.Parent)
                if (ReferenceEquals(other, this)) return true;
            return false;
        }

        public string? Classification(List<ScopeMap> stack, int current)
        {
            if (Complex.Count > 0 && current > 0)
            {
                (int[] Positions, string Classification)? best = null;
                foreach (var (ancestors, classification) in Complex)
                {
                    var positions = Match(ancestors, stack, current);
                    if (positions is not null && (best is null || Better(positions, best.Value.Positions))) best = (positions, classification);
                }
                return best?.Classification;
            }
            return Simple ?? Parent?.Classification(stack, current);
        }

        private int[]? Match(string[] ancestors, List<ScopeMap> stack, int current)
        {
            var positions = new List<int>();
            var index = current;
            for (var a = ancestors.Length - 1; a >= 0; a--)
            {
                var ancestor = theme.Find(ancestors[a]);
                do
                {
                    if (--index < a) return null;
                }
                while (ancestor is null || !ancestor.IsAncestorOf(stack[index]));
                positions.Add(index);
            }
            return [.. positions];
        }

        private static bool Better(int[] challenger, int[] best)
        {
            for (var i = 0; i < Math.Min(challenger.Length, best.Length); i++)
                if (challenger[i] != best[i]) return challenger[i] > best[i];
            return challenger.Length > best.Length;
        }

        public override string ToString() => Name;
    }
}

internal static class VisualStudioClassifications
{
    public static readonly HashSet<string> Core = new(StringComparer.OrdinalIgnoreCase)
    {
        "comment", "identifier", "keyword", "literal", "number", "operator", "preprocessor keyword", "string", "type",
        "symbol reference", "markup node", "markup attribute", "markup attribute value",
        "keyword - control", "class name", "struct name", "interface name", "enum name", "delegate name", "record class name",
        "type parameter name", "namespace name", "method name", "extension method name", "property name", "field name",
        "local name", "parameter name", "punctuation", "string - escape character", "string - verbatim",
    };

    public static readonly HashSet<string> Web = new(StringComparer.OrdinalIgnoreCase)
    {
        "HTML Element Name", "HTML Attribute Name", "HTML Attribute Value", "HTML Tag Delimiter", "HTML Operator",
        "HTML Comment", "CSS Selector", "CSS Property Name", "CSS Property Value", "CSS Comment", "CSS Keyword", "RazorDirective",
    };

    public static bool InCore(string name) => Core.Contains(name);

    public static bool InCoreOrWeb(string name) => Core.Contains(name) || Web.Contains(name);
}
