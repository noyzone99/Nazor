using Nazor.Rendering;

namespace Nazor.Style;

internal sealed class RuleIndex
{

    private const string Spaces = " \t\r\n\f";

    private readonly Dictionary<string, List<StyleRule>> byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<StyleRule>> byClass = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<StyleRule>> byType = new(StringComparer.Ordinal);
    private readonly List<StyleRule> universal = [];
    private readonly HashSet<PseudoElement> targets = [];

    internal bool HasTarget(PseudoElement target) => targets.Contains(target);
#if NET9_0_OR_GREATER
    private readonly Dictionary<string, List<StyleRule>>.AlternateLookup<ReadOnlySpan<char>> classes;
#endif

    public RuleIndex(IReadOnlyList<StyleRule> rules)
    {
        foreach (var rule in rules)
        {
            var subject = rule.Selector.Subject;
            targets.Add(subject.PseudoElement);
            var list = subject.Id is { } id ? Bucket(byId, id)
                : subject.Classes.Count > 0 ? Bucket(byClass, subject.Classes[0])
                : subject.Type is { } type ? Bucket(byType, type)
                : universal;
            list.Add(rule);
        }
#if NET9_0_OR_GREATER
        classes = byClass.GetAlternateLookup<ReadOnlySpan<char>>();
#endif

        static List<StyleRule> Bucket(Dictionary<string, List<StyleRule>> buckets, string key) =>
            buckets.TryGetValue(key, out var list) ? list : buckets[key] = [];
    }

    public void Match(ElementNode element, Viewport viewport, MediaPreferences preferences, int level, List<(int Level, StyleRule Rule, int ScopeDistance)> matched, Type? scope = null, int scopeDistance = 0, PseudoElement target = PseudoElement.None)
    {
        if (element.Id is { } id && byId.TryGetValue(id, out var withId)) Add(withId);
        if (element.Class is { } classAttribute && byClass.Count > 0)
        {
            var attribute = classAttribute.AsSpan();
            foreach (var range in attribute.SplitAny(Spaces))
            {
                var name = attribute[range];

#if NET9_0_OR_GREATER
                if (!name.IsEmpty && !Repeated(attribute, range) && classes.TryGetValue(name, out var withClass)) Add(withClass);
#else
                if (!name.IsEmpty && !Repeated(attribute, range) && byClass.TryGetValue(name.ToString(), out var withClass)) Add(withClass);
#endif
            }
        }
        if (byType.TryGetValue(element.Name, out var withType)) Add(withType);
        Add(universal);

        void Add(List<StyleRule> rules)
        {
            foreach (var rule in rules)
                if ((rule.Media is null || rule.Media.Matches(viewport, preferences)) &&
                    (scope is null ? rule.Selector.MatchesTarget(element, target) : rule.Selector.MatchesScoped(element, scope, target))) matched.Add((level, rule, scopeDistance));
        }
    }

    private static bool Repeated(ReadOnlySpan<char> attribute, Range occurrence)
    {
        var name = attribute[occurrence];
        var start = occurrence.Start.GetOffset(attribute.Length);
        foreach (var range in attribute.SplitAny(Spaces))
        {
            if (range.Start.GetOffset(attribute.Length) >= start) return false;
            if (attribute[range].SequenceEqual(name)) return true;
        }
        return false;
    }
}
