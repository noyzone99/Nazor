using Nazor.Rendering;

namespace Nazor.Diagnostics;

public sealed partial class DevToolsSession
{
    internal readonly record struct BrowseNode(ComponentNodeInfo? Component, StructuralNodeInfo? Element, bool HasChildren);
    internal readonly record struct BrowsePage(BrowseNode[] Nodes, bool HasMore, int Visited);

    internal sealed class TreeWalk(DevToolsSession session, string kind, string mode, Node? start, string? query, bool includeFramework)
    {
        private const int VisitLimit = 256;
        private const int PageSize = 64;
        private readonly Stack<(Node? Parent, int Index)> pending = new();
        private Node? ancestor;
        private bool started;

        internal BrowsePage Read(CancellationToken token)
        {
            session.Renderer.Dispatcher.AssertAccess();
            var result = new List<BrowseNode>();
            var visited = 0;
            var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            bool CanVisit() => visited < VisitLimit && result.Count < PageSize &&
                (visited < 8 || System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds < 3);
            if (mode == "details")
                return new([session.BrowseDescription(start!, null, true)], false, 1);

            if (mode == "path")
            {
                if (!started) { ancestor = start; started = true; }
                while (ancestor is { } node && CanVisit())
                {
                    token.ThrowIfCancellationRequested();
                    visited++;
                    if (node is ComponentNode) session.IdOf(node);
                    if (MatchesKind(node)) result.Add(session.BrowseDescription(node, null, false));
                    ancestor = node.Parent;
                }
                return new(result.ToArray(), ancestor is not null, visited);
            }

            if (!started) { Push(start); started = true; }
            while (CanVisit() && pending.TryPop(out var frame))
            {
                token.ThrowIfCancellationRequested();
                var (parent, index) = frame;
                if (index + 1 < Count(parent)) pending.Push((parent, index + 1));
                var node = parent is null ? session.Renderer.RootComponents[index].Node : parent.LogicalChildren![index];
                visited++;
                if (node.IsRemoved) continue;
                if (MatchesKind(node))
                {
                    if (query is null || session.BrowseMatches(node, query))
                    {
                        var parentId = mode == "children" && start is not null ? session.IdOf(start) : (int?)null;
                        result.Add(session.BrowseDescription(node, parentId, false));
                    }
                    if (mode != "search") continue;
                }
                Push(node);
            }
            return new(result.ToArray(), pending.Count > 0, visited);
        }

        private bool MatchesKind(Node node) => kind == "element" || mode == "outputs"
            ? node is ElementNode or TextNode
            : node is ComponentNode component && (includeFramework || !IsFramework(component.Component.GetType()));

        private int Count(Node? node) => node is null ? session.Renderer.RootComponents.Count : node.LogicalChildren?.Count ?? 0;

        private void Push(Node? node)
        {
            if (Count(node) > 0) pending.Push((node, 0));
        }
    }

    private bool BrowseMatches(Node node, string query)
    {
        bool Match(string? text) => text?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        return ids.TryGetValue(node, out var id) && Match(id.ToString(System.Globalization.CultureInfo.InvariantCulture)) || node switch
        {
            ComponentNode component => Match(component.Component.GetType().FullName) || Match(component.Key is { } key ? Format(key) : null),
            TextNode text => Match(Preview(text.Text, 16384)),
            ElementNode element => Match(Preview(element.Name)) || Match(Preview(element.Id)) || Match(Preview(element.Class)) ||
                element.Attributes.Take(32).Any(pair => Match(pair.Key) || Match(pair.Value is string value ? Preview(value) : null)),
            _ => false,
        };
    }

    private BrowseNode BrowseDescription(Node node, int? parentId, bool details)
    {
        var id = IdOf(node);
        var expandable = node.LogicalChildren is { Count: > 0 };
        if (node is ComponentNode component)
        {
            var type = component.Component.GetType();
            return new(new ComponentNodeInfo(id, parentId, 0, Preview(type.FullName ?? type.Name)!,
                component.Key is { } key ? Preview(Format(key)) : null, IsFramework(type), KnownComponentId(component.DeclaringComponent), [], [])
            {
                Parameters = details ? BrowseProperties(component.State.DirectParameters.Take(MaxCaptureProperties).Select(p => new KeyValuePair<string, object?>(p.Name, p.Value))) : null,
            }, null, expandable);
        }
        if (node is TextNode text)
            return new(null, new(id, parentId, 0, NodeKind.Text, "#text", Preview(text.Text, details ? 16384 : 256), null, null, null, null, []), false);

        var element = (ElementNode)node;
        return new(null, new StructuralNodeInfo(id, parentId, 0, NodeKind.Element, Preview(element.Name)!, null,
            element.Key is { } elementKey ? Preview(Format(elementKey)) : null, KnownComponentId(element.Owner), KnownComponentId(element.DeclaringComponent),
            BrowseVisibility(element, details), [])
        {
            ElementId = Preview(element.Id),
            Classes = Preview(element.Class)?.Split(' ', 32, StringSplitOptions.RemoveEmptyEntries),
            Attributes = details ? BrowseProperties(element.Attributes.Take(MaxCaptureProperties).Where(pair => pair.Value is not (IEventCallback or Delegate))) : null,
            Events = details ? element.Attributes.Take(MaxCaptureProperties).Where(pair => pair.Value is IEventCallback or Delegate).Select(pair => Preview(pair.Key)!).ToArray() : null,
            States = details ? Enum.GetValues<ElementStates>().Where(state => state != ElementStates.None && element.States.HasFlag(state)).Select(state => state.ToString().ToLowerInvariant()).ToArray() : null,
        }, expandable);
    }

    private static bool IsFramework(Type type) => type.Assembly == typeof(ComponentBase).Assembly || type.Assembly.GetName().Name == "Nazor.Controls";

    private bool? BrowseVisibility(ElementNode element, bool details)
    {
        if (Scene.Styles.IsDirty) return null;
        var style = Scene.Styles.GetStyle(element);
        if (style.Get(Nazor.Style.StyleProperties.Visibility) != Nazor.Style.Visibility.Visible || style.Display == Nazor.Style.Display.None) return false;
        if (!details) return null;
        var remaining = MaxCaptureDepth;
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (--remaining == 0) return null;
            if (parent is ElementNode owner && Scene.Styles.GetStyle(owner).Display == Nazor.Style.Display.None) return false;
        }
        return true;
    }

    private static string? Preview(string? text, int limit = 256) => text is not null && text.Length > limit ? text[..(limit - 1)] + "…" : text;

    private static IReadOnlyDictionary<string, string> BrowseProperties(IEnumerable<KeyValuePair<string, object?>> values)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in values) result[Preview(name)!] = value is null ? "null" : Preview(Format(value), 1024)!;
        return result;
    }
}
