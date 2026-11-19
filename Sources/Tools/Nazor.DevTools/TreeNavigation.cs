using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed class TreeNavigation
{
    private readonly Dictionary<string, ComponentInfo> components;
    private readonly Dictionary<string, ElementInfo> elements;
    private readonly Dictionary<string, string[]> componentChildren = [];
    private readonly Dictionary<string, string[]> elementChildren = [];
    private readonly Dictionary<string, string?> componentParents = [];
    private readonly Dictionary<string, string?> elementParents = [];
    public string[] ComponentRoots { get; }
    public string[] ElementRoots { get; }
    public IEnumerable<string> ComponentIds => componentChildren.Keys;
    public IEnumerable<string> ElementIds => elementChildren.Keys;
    public TreeNavigation(TreeSnapshot? tree, string query = "", bool showFramework = false)
    {
        components = tree?.Components.ToDictionary(c => c.Id) ?? [];
        elements = tree?.Elements.ToDictionary(e => e.Id) ?? [];
        var visible = new Dictionary<string, List<string>>();
        var roots = new List<string>();
        var pending = new Stack<(string Id, string? Parent)>((tree?.ComponentRoots ?? []).Reverse().Select(id => (id, (string?)null)));
        while (pending.TryPop(out var item))
        {
            var component = components[item.Id];
            var parent = item.Parent;
            if (showFramework || !component.IsFramework)
            {
                (parent is null ? roots : visible[parent]).Add(item.Id);
                visible[item.Id] = [];
                componentParents[item.Id] = parent;
                parent = item.Id;
            }
            foreach (var child in component.Children.Reverse()) pending.Push((child, parent));
        }
        foreach (var pair in visible) componentChildren[pair.Key] = pair.Value.ToArray();
        foreach (var node in elements.Values) { elementChildren[node.Id] = node.Children; elementParents[node.Id] = node.ParentId; }
        ComponentRoots = Filter(roots.ToArray(), componentChildren, componentParents, id => ComponentLabel(id) + " " + components[id].TypeName, query);
        ElementRoots = Filter(tree?.ElementRoots ?? [], elementChildren, elementParents,
            id => ElementLabel(id) + " " + elements[id].Text + " " + string.Join(" ", elements[id].Attributes?.Select(p => p.Name + "=" + p.Value) ?? []), query);
    }
    private static string[] Filter(string[] roots, Dictionary<string, string[]> children, Dictionary<string, string?> parents, Func<string, string> text, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return roots;
        var keep = new HashSet<string>();
        foreach (var id in children.Keys)
            if (text(id).Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
                for (string? at = id; at is not null && keep.Add(at); at = parents.GetValueOrDefault(at)) { }
        foreach (var id in children.Keys.ToArray())
            if (!keep.Contains(id)) children.Remove(id);
            else children[id] = children[id].Where(keep.Contains).ToArray();
        return roots.Where(keep.Contains).ToArray();
    }
    public IReadOnlyList<string> ComponentChildren(string id) => componentChildren.GetValueOrDefault(id) ?? [];
    public IReadOnlyList<string> ElementChildren(string id) => elementChildren.GetValueOrDefault(id) ?? [];
    public bool Contains(string kind, string id) => (kind == "component" ? componentChildren : elementChildren).ContainsKey(id);
    public string ComponentLabel(string id) => components.TryGetValue(id, out var c)
        ? NodeSyntax.ShortName(c.TypeName) + " #" + id + (c.Key is null ? "" : " [" + c.Key + "]") + (c.OutputElements.Length == 0 ? " (no output)" : c.OutputElements.Length > 1 ? " (" + c.OutputElements.Length + " outputs)" : "") : id;
    public string ElementLabel(string id) => elements.TryGetValue(id, out var e)
        ? (e.Kind == "text" ? "#text " + Short(e.Text ?? "") : "<" + e.Name + (e.ElementId is null ? "" : " id=" + e.ElementId) + ">" + string.Concat((e.Classes ?? []).Select(c => "." + c))) + " #" + id + (e.IsRendered == false ? " (hidden)" : "") : id;
    private static string Short(string text) => text.Length <= 48 ? text : text[..48] + "...";
    public string[] Breadcrumbs(string kind, string id)
    {
        var path = new List<string>();
        for (string? at = id; at is not null; at = kind == "component" ? components.GetValueOrDefault(at)?.ParentId : elements.GetValueOrDefault(at)?.ParentId) path.Add(at);
        path.Reverse();
        return path.ToArray();
    }
}
