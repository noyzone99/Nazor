using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

internal readonly record struct TreeBranch(string Kind, string? Parent);

internal sealed record PagedTreeState(IReadOnlySet<string> Expandable, IReadOnlySet<TreeBranch> Loaded, IReadOnlySet<TreeBranch> More,
    bool IncludeFramework, bool MoreOutputs);

internal sealed class PagedTreeCache
{
    internal readonly Dictionary<TreeBranch, Branch> Branches = [];
    private readonly Dictionary<TreeBranch, List<TreeBrowseNode>> paths = [];
    private TreeBrowseNode? details;
    internal string? OutputCursor { get; set; }
    internal TreeBrowseNode? Detail => details;
    internal CaptureStamp? Capture { get; set; }
    internal long Revision { get; set; }
    internal bool IncludeFramework { get; set; }

    internal sealed record Branch(long Revision, TreeBrowseNode[] Nodes, string? Cursor);

    internal static string Id(TreeBrowseNode node) => node.Component?.Id ?? node.Element!.Id;

    internal void ClearPaths() { paths.Clear(); details = null; OutputCursor = null; }

    internal void AddPath(string kind, IReadOnlyList<TreeBrowseNode> path, TreeBrowseNode detail)
    {
        foreach (var branch in paths.Keys.Where(branch => branch.Kind == kind).ToArray()) paths.Remove(branch);
        foreach (var (node, index) in path.Select((node, index) => (node, index)))
        {
            var parent = index + 1 < path.Count ? Id(path[index + 1]) : null;
            var branch = new TreeBranch(kind, parent);
            if (!paths.TryGetValue(branch, out var nodes)) paths.Add(branch, nodes = []);
            if (!nodes.Any(item => Id(item) == Id(node))) nodes.Add(node);
        }
        details = detail;
    }

    internal void UpdateDetail(TreeBrowseNode detail) => details = detail;

    internal (TreeSnapshot Tree, PagedTreeState State) View()
    {
        var components = new List<ComponentInfo>();
        var elements = new List<ElementInfo>();
        var expandable = new HashSet<string>();
        var seen = new HashSet<string>();
        var componentRoots = Claim(Children(new("component", null)));
        var elementRoots = Claim(Children(new("element", null)));
        var pending = new Stack<(TreeBrowseNode Node, string? Parent)>();
        Push(componentRoots, null);
        Push(elementRoots, null);
        while (pending.TryPop(out var entry))
        {
            var id = Id(entry.Node);
            var node = details is { } selected && Id(selected) == id ? selected : entry.Node;
            var kind = node.Component is not null ? "component" : "element";
            var children = Claim(Children(new(kind, id)));
            if (node.HasChildren || children.Length > 0) expandable.Add(id);
            var ids = children.Select(Id).ToArray();
            if (node.Component is { } component) components.Add(component with { ParentId = entry.Parent, Children = ids });
            else if (node.Element is { } element) elements.Add(element with { ParentId = entry.Parent, Children = ids });
            Push(children, id);
        }
        return (new(Capture!, Revision, componentRoots.Select(Id).ToArray(), elementRoots.Select(Id).ToArray(), components.ToArray(), elements.ToArray()),
            new(expandable, Branches.Keys.ToHashSet(), Branches.Where(pair => pair.Value.Cursor is not null).Select(pair => pair.Key).ToHashSet(),
                IncludeFramework, OutputCursor is not null));

        TreeBrowseNode[] Claim(TreeBrowseNode[] nodes) => nodes.Where(node => seen.Add(Id(node))).ToArray();

        TreeBrowseNode[] Children(TreeBranch branch)
        {
            var current = Branches.GetValueOrDefault(branch)?.Nodes ?? [];
            return paths.TryGetValue(branch, out var extra) ? current.Concat(extra).DistinctBy(Id).ToArray() : current;
        }

        void Push(TreeBrowseNode[] children, string? parent)
        {
            for (var index = children.Length - 1; index >= 0; index--) pending.Push((children[index], parent));
        }
    }
}
