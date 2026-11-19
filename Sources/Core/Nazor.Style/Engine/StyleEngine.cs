using System.Globalization;
using Nazor.Rendering;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>Cascade levels, from the weakest to the strongest.</summary>
public enum StyleLevel
{
    /// <summary>Applies theme defaults at the lowest stylesheet cascade level.</summary>
    Theme,

    /// <summary>Applies application-wide stylesheet rules above the theme level.</summary>
    Application,

    /// <summary>Applies window-local stylesheet rules above application rules.</summary>
    Window,

    /// <summary>Applies component-scoped stylesheet rules at the highest stylesheet level.</summary>
    Component,
}

internal sealed class ElementStyle
{
    public ComputedStyle? Computed;
    public bool Dirty = true;

    public bool SubtreeDirty = true;

    public bool UsesParent;
    public Dictionary<PseudoElement, ComputedStyle>? Pseudos;
    public Dictionary<PseudoElement, ElementNode>? Generated;
}

internal sealed record GeneratedElement(ElementNode Origin, PseudoElement Target);

/// <summary>
/// Resolves the computed style of every element of a renderer and keeps it up to date after render batches,
/// interaction-state changes, stylesheet replacements and viewport changes (for media queries and viewport units).
/// The cascade sorts matching rules by level (theme, application, window, component-scoped), then specificity,
/// nearest scope and source order. Scoped rules target declared elements; <c>::deep</c> also reaches descendants of a
/// declared anchor. Only <c>var()</c> results are parsed per element.
/// </summary>
public sealed class StyleEngine : IDisposable
{
    private static readonly SystemColors lightColors = SystemColors.Resolve();
    private static readonly SystemColors darkColors = SystemColors.Resolve(scheme: ColorScheme.Dark);
    private static SystemColors ColorsOf(MediaPreferences value) => value.Colors ?? (value.ColorScheme == ColorScheme.Dark ? darkColors : lightColors);

    private readonly Renderer renderer;
    private readonly StyleSheet?[] globalSheets = new StyleSheet?[3];
    private readonly HashSet<StyleSheet> scopedInUse = [];

    private readonly HashSet<StyleSheet> scopedSeen = [];

    private readonly HashSet<ElementNode> viewportDependent = [];
    private bool computeUsedViewport;

    private ComputedStyle? rootStyle;
    private float? systemButtonsHeight;
    private Func<ComputedStyle, FontRelativeMetrics>? fontMetricsResolver;

    /// <summary>Gets or sets the logical horizontal font measurements used by ch and ex lengths.</summary>
    /// <remarks>The callback receives the relevant computed font: the parent's for font-size, the element's otherwise. Null or unavailable measurements use 0.5em. Changing the resolver invalidates styles; font registries must call InvalidateAll when their available faces change. Returned measurements must be finite and nonnegative.</remarks>
    public Func<ComputedStyle, FontRelativeMetrics>? FontMetricsResolver
    {
        get => fontMetricsResolver;
        set
        {
            if (ReferenceEquals(fontMetricsResolver, value)) return;
            fontMetricsResolver = value;
            InvalidateAll();
        }
    }

    /// <summary>Measured integrated control height in logical pixels; null when unavailable.</summary>
    public float? SystemButtonsHeight
    {
        get => systemButtonsHeight;
        set
        {
            if (value is { } height && (!float.IsFinite(height) || height < 0)) throw new ArgumentOutOfRangeException(nameof(value));
            if (systemButtonsHeight == value) return;
            systemButtonsHeight = value;
            rootStyle = null;
            InvalidateAll();
        }
    }
    private bool computeUsedParent;
    private Viewport viewport = new(800, 600);
    private MediaPreferences preferences;
    private bool allDirty = true;
    private bool isDirty = true;
    private bool hasGeneratedContent;
    private readonly Dictionary<(ComputedStyle Parent, string Name, string? Inline, bool Root, int Rules), CascadeResult> cascades = [];
    private sealed record CascadeResult((int Level, StyleRule Rule, int ScopeDistance)[] Matched, ComputedStyle Style, bool Parent, bool Viewport);
    private bool sharingCascades;
    private readonly object[] specifiedValues = new object[StyleProperties.All.Count];
    private readonly bool[] relativeValues = new bool[StyleProperties.All.Count];
    private readonly List<(int Level, StyleRule Rule, int ScopeDistance)> matchedRules = [];
    private Dictionary<string, IReadOnlyList<StyleToken>>? rawVariables;
    private IReadOnlyDictionary<string, IReadOnlyList<StyleToken>>? resolvedVariables;
    private readonly Dictionary<(PendingDeclaration Declaration, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> Custom), (StyleProperty Property, object Value, bool Relative)[]> pendingValues = [];
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PendingDeclaration, (StyleProperty Property, object Value, bool Relative)[]> constantPending = new();

    private int seenVersion = -1;

    /// <summary>Initializes a new instance of StyleEngine.</summary>
    /// <param name="renderer">The renderer that owns the component tree.</param>
    public StyleEngine(Renderer renderer)
    {
        this.renderer = renderer;
        renderer.BatchCompleted += OnBatchCompleted;
    }

    /// <summary>Raised by <see cref="Update()" /> for each element whose computed values changed.</summary>
    public event Action<ElementNode, StyleEffects>? StyleChanged;

    /// <summary>Gets or sets the theme stylesheet at the lowest cascade level.</summary>
    public StyleSheet? Theme
    {
        get => globalSheets[0];
        set => SetSheet(0, value);
    }

    /// <summary>Gets or sets the application-wide stylesheet.</summary>
    public StyleSheet? Application
    {
        get => globalSheets[1];
        set => SetSheet(1, value);
    }

    /// <summary>Gets or sets the stylesheet local to this window.</summary>
    public StyleSheet? Window
    {
        get => globalSheets[2];
        set => SetSheet(2, value);
    }

    /// <summary>Gets or sets the logical viewport dimensions used for media queries and relative units.</summary>
    public Viewport Viewport
    {
        get => viewport;
        set
        {
            if (viewport == value) return;
            var before = MediaState(viewport, preferences);
            viewport = value;
            foreach (var element in viewportDependent)
            {
                if (element.GetFeature<ElementStyle>() is { } state) state.Dirty = true;
                isDirty = true;
            }
            if (MediaState(value, preferences) == before) return;
            allDirty = true;
            isDirty = true;
        }
    }

    /// <summary>The color scheme and reduced motion tested by <c>@media (prefers-…)</c>.</summary>
    public MediaPreferences Preferences
    {
        get => preferences;
        set
        {
            if (preferences == value) return;
            var before = MediaState(viewport, preferences);
            var colors = !Equals(ColorsOf(preferences), ColorsOf(value));
            preferences = value;

            if (colors) rootStyle = null;
            if (!colors && MediaState(viewport, value) == before) return;
            allDirty = true;
            isDirty = true;
        }
    }

    /// <summary>
    /// True when an <see cref="Update()" /> would recompute something, including after a sheet was replaced in the
    /// <see cref="StyleRegistry" />.
    /// </summary>
    public bool IsDirty => isDirty || seenVersion != StyleRegistry.Version;

    /// <summary>
    /// Recomputes every element at the next <see cref="Update()" />, for example after a hot reload delta.
    /// </summary>
    public void InvalidateAll()
    {
        allDirty = true;
        isDirty = true;
    }

    /// <summary>Elements whose style was recomputed by the last <see cref="Update()" />.</summary>
    public int LastRecomputedCount { get; private set; }

    /// <summary>Gets the total elements recomputed over this engine's lifetime, including lazy style lookups and recomputations that preserve the same values.</summary>
    public long TotalRecomputedCount { get; private set; }

    /// <summary>The element's computed style, computing pending changes first.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting ComputedStyle value.</returns>
    public ComputedStyle GetStyle(ElementNode element)
    {
        if (element.GetFeature<GeneratedElement>() is { } generated) return GetPseudoStyle(generated.Origin, generated.Target);
        if (IsDirty) Update();
        return element.GetFeature<ElementStyle>()?.Computed ?? Compute(element, ParentStyle(element));
    }

    /// <summary>Computes the style of a pseudo-element using the originating element's selectors, scope and inherited values. No inline declarations apply to the pseudo-element.</summary>
    /// <param name="element">The originating retained element.</param>
    /// <param name="target">The pseudo-element to resolve; None returns the element style.</param>
    /// <returns>The computed pseudo-element style.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The target is undefined.</exception>
    public ComputedStyle GetPseudoStyle(ElementNode element, PseudoElement target)
    {
        if (!Enum.IsDefined(target)) throw new ArgumentOutOfRangeException(nameof(target));
        var parent = GetStyle(element);
        if (target == PseudoElement.None) return parent;
        var state = element.GetFeature<ElementStyle>()!;
        var pseudos = state.Pseudos ??= [];
        if (pseudos.TryGetValue(target, out var cached)) return cached;
        var computed = Compute(element, parent, target);
        if (computeUsedViewport) viewportDependent.Add(element);
        return pseudos[target] = computed;
    }

    /// <summary>Gets a retained decorative before/after box when content generates it. The box has the originating element as parent but is excluded from its authored children and accessibility tree.</summary>
    /// <param name="element">The originating element.</param>
    /// <param name="target">Before or After.</param>
    /// <returns>The retained box, or null when content is normal/none or display is none.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The target is not Before or After.</exception>
    public ElementNode? GetGeneratedElement(ElementNode element, PseudoElement target)
    {
        if (target is not (PseudoElement.Before or PseudoElement.After)) throw new ArgumentOutOfRangeException(nameof(target));
        if (element.GetFeature<GeneratedElement>() is not null) return null;
        if (element.GetFeature<ElementStyle>()?.Generated?.GetValueOrDefault(target) is { } retained) retained.IsRemoved = true;
        if (IsDirty) Update();
        if (!hasGeneratedContent) return null;
        if (!globalSheets.Any(s => s?.Index.HasTarget(target) == true) &&
            !ElementScopes(element).Any(scope => StyleRegistry.GetScoped(scope)?.Index.HasTarget(target) == true)) return null;
        var style = GetPseudoStyle(element, target);
        var fragments = style.Get(StyleProperties.Content).Items;
        if (fragments.Count == 0 || style.Display == Display.None) return null;
        var generated = element.GetFeature<ElementStyle>()!.Generated ??= [];
        if (!generated.TryGetValue(target, out var box))
        {
            box = new ElementNode("::" + target.ToString().ToLowerInvariant(), element.Scope, element.Owner, element.DeclaringComponent) { Parent = element };
            box.SetFeature(new GeneratedElement(element, target));
            box.LogicalChildren.Add(new TextNode("") { Parent = box });
            generated[target] = box;
        }
        ((TextNode)box.LogicalChildren[0]).Text = string.Concat(fragments);
        box.IsRemoved = false;
        return box;
    }

    /// <summary>Recomputes dirty elements top-down and raises <see cref="StyleChanged" />.</summary>
    public void Update()
    {
        var registryVersion = StyleRegistry.Version;
        var all = allDirty || seenVersion != registryVersion;
        LastRecomputedCount = 0;

        if (!all && !isDirty) return;
        seenVersion = registryVersion;

        if (all)
        {
            scopedInUse.Clear();
            scopedSeen.Clear();
        }
        allDirty = false;
        isDirty = false;
        sharingCascades = true;
        try
        {
            foreach (var root in renderer.RootComponents)
                foreach (var node in root.Nodes)
                    if (node is ElementNode element) Update(element, RootStyle, all, false, false, false);
        }
        finally
        {
            sharingCascades = false;
            cascades.Clear();
            pendingValues.Clear();
            Array.Clear(specifiedValues);
            matchedRules.Clear();
        }
        hasGeneratedContent = globalSheets.Any(Generates) || scopedSeen.Any(Generates);

        static bool Generates(StyleSheet? sheet) => sheet is not null &&
            (sheet.Index.HasTarget(PseudoElement.Before) || sheet.Index.HasTarget(PseudoElement.After));
    }

    private void Update(ElementNode element, ComputedStyle parentStyle, bool force, bool inherited, bool parentChanged, bool rematch)
    {
        var state = element.GetFeature<ElementStyle>();
        if (state is null)
        {
            state = new ElementStyle();
            element.SetFeature(state);
            element.StatesChanged += OnStatesChanged;
        }
        var changed = false;
        var inheritedChanged = false;
        var subtree = rematch || state.SubtreeDirty;
        state.SubtreeDirty = false;
        if (force || state.Dirty || rematch || inherited || (parentChanged && state.UsesParent))
        {
            state.Pseudos?.Clear();
            var previous = state.Computed;
            var computed = Compute(element, parentStyle);
            if (computeUsedViewport) viewportDependent.Add(element);
            else viewportDependent.Remove(element);
            state.UsesParent = computeUsedParent;
            LastRecomputedCount++;
            TotalRecomputedCount++;
            state.Dirty = false;
            if (previous is null)
            {
                state.Computed = computed;
                changed = inheritedChanged = true;
            }
            else
            {
                var effects = previous.Compare(computed);
                inheritedChanged = !previous.InheritedEqual(computed);
                changed = effects != StyleEffects.None || inheritedChanged;

                if (changed) state.Computed = computed;
                if (effects != StyleEffects.None) StyleChanged?.Invoke(element, effects);
            }
        }

        var children = element.Children;
        for (var i = 0; i < children.Count; i++)
            if (children[i] is ElementNode childElement) Update(childElement, state.Computed!, force, inheritedChanged, changed, subtree);
    }

    private void SetSheet(int index, StyleSheet? sheet)
    {
        if (ReferenceEquals(globalSheets[index], sheet)) return;
        globalSheets[index] = sheet;
        allDirty = true;
        isDirty = true;
    }

    private void OnBatchCompleted(object? sender, RenderBatch batch)
    {
        foreach (var element in batch.AddedElements)
        {
            MarkDirty(element);
            InvalidateHas(element);
        }
        foreach (var (element, name) in batch.UpdatedAttributes)
        {
            var nonlocal = AttributeAffectsOtherElements(name);
            MarkDirty(element, nonlocal);
            if (nonlocal && SiblingSensitive(element.Scope)) MarkSiblingsDirty(element);
            InvalidateHas(element, name);
        }
        foreach (var text in batch.UpdatedTexts)
            if (text.ParentElement is { } parent && TextSensitive(parent.Scope))
            {
                MarkDirty(parent);
                if (SiblingSensitive(parent.Scope)) MarkSiblingsDirty(parent);
                InvalidateHas(parent, text: true);
            }

        foreach (var parent in batch.ChildrenChanged)
        {
            if (Structural(parent.Children.OfType<ElementNode>().Select(c => c.Scope).Append(parent.Scope))) MarkDirty(parent);
            InvalidateHas(parent, structure: true);
        }
        if (batch.RootNodesChanged && Structural(renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().Select(e => e.Scope)))
        {
            allDirty = true;
            isDirty = true;
        }
        foreach (var node in batch.RemovedNodes)
        {
            if (node is not ElementNode element) continue;
            element.StatesChanged -= OnStatesChanged;
            if (element.GetFeature<ElementStyle>()?.Generated is { } generated)
                foreach (var box in generated.Values) box.IsRemoved = true;
            viewportDependent.Remove(element);
        }
        if (batch.AddedElements.Count != 0 || batch.UpdatedElements.Count != 0 || batch.RootNodesChanged) isDirty = true;
    }

    private void OnStatesChanged(ElementNode element, ElementStates previous)
    {
        var changed = element.States ^ previous;
        if (element.GetFeature<ElementStyle>() is { } state)
        {
            state.Dirty = true;
            if (Rematches(element, changed))
            {
                state.SubtreeDirty = true;
                if (SiblingSensitive(element.Scope)) MarkSiblingsDirty(element);
            }
        }
        isDirty = true;
        InvalidateHas(element, states: changed);
    }

    private void InvalidateHas(ElementNode element, string? attribute = null, ElementStates states = ElementStates.None, bool structure = false, bool text = false)
    {
        foreach (var sheet in globalSheets)
            if (sheet is not null) Invalidate(sheet);
        foreach (var sheet in scopedSeen) Invalidate(sheet);

        void Invalidate(StyleSheet sheet)
        {
            foreach (var dependency in sheet.HasDependencies)
            {
                if (!dependency.Relevant(attribute, states, text)) continue;
                foreach (var origin in dependency.AffectedOrigins(element, structure))
                {
                    MarkDirty(origin);
                    if (dependency.AffectsSiblings) MarkSiblingsDirty(origin);
                }
            }
        }
    }

    private bool Rematches(ElementNode element, ElementStates changed)
    {
        foreach (var sheet in globalSheets)
            if (sheet is not null && Concerns(sheet)) return true;
        foreach (var sheet in scopedSeen)
            if (Concerns(sheet)) return true;
        return false;

        bool Concerns(StyleSheet sheet)
        {
            foreach (var dependency in sheet.StateDependencies)
                if (dependency.Concerns(element, changed)) return true;
            return false;
        }
    }

    private bool Structural(IEnumerable<Type?> scopes) =>
        globalSheets.Any(s => s?.HasStructuralSelectors == true) ||
        scopedSeen.Any(s => s.HasStructuralSelectors) ||
        scopes.Distinct().Any(scope => scope is not null && StyleRegistry.GetScoped(scope)?.HasStructuralSelectors == true);

    private bool SiblingSensitive(Type? scope) => globalSheets.Any(s => s?.HasSiblingDependencies == true) ||
        scopedSeen.Any(s => s.HasSiblingDependencies) || scope is not null && StyleRegistry.GetScoped(scope)?.HasSiblingDependencies == true;

    private bool TextSensitive(Type? scope) => globalSheets.Any(s => s?.ObservesText == true) ||
        scopedSeen.Any(s => s.ObservesText) || scope is not null && StyleRegistry.GetScoped(scope)?.ObservesText == true;

    private bool AttributeAffectsOtherElements(string name)
    {
        if (name is "class" or "id" or "style" or "disabled") return true;
        foreach (var sheet in globalSheets)
            if (sheet?.AttributeAffectsOtherElements(name) == true) return true;
        foreach (var sheet in scopedSeen)
            if (sheet.AttributeAffectsOtherElements(name)) return true;
        return false;
    }

    private void MarkDirty(ElementNode element, bool descendants = true)
    {
        var state = element.GetFeature<ElementStyle>();
        if (state is not null)
        {
            state.Dirty = true;
            state.SubtreeDirty |= descendants;
        }
        isDirty = true;
    }

    private void MarkSiblingsDirty(ElementNode element)
    {
        if (element.ParentElement is { } parent) MarkDirty(parent);
        else allDirty = isDirty = true;
    }

    private string MediaState(Viewport value, MediaPreferences media)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var sheet in globalSheets.OfType<StyleSheet>().Concat(scopedInUse))
        {
            var rules = sheet.Rules;
            foreach (var rule in rules)
                if (rule.Media is not null) builder.Append(rule.Media.Matches(value, media) ? '1' : '0');
        }
        return builder.ToString();
    }

    /// <summary>
    /// The <c>@keyframes</c> rule named <paramref name="name" /> for an element, its values computed for it: em, rem
    /// and viewport units converted, <c>var()</c> substituted, <c>currentColor</c> resolved, <c>inherit</c> and <c>initial</c>
    /// applied. The scoped sheet of the element comes first, then the window, application and theme levels; null when
    /// no sheet has the name.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting IReadOnlyList&lt;ResolvedKeyframe&gt;? value.</returns>
    /// <exception cref="ArgumentNullException">element, name is null.</exception>
    public IReadOnlyList<ResolvedKeyframe>? ResolveKeyframes(ElementNode element, string name)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(name);
        var rule = (element.Scope is { } scope ? StyleRegistry.GetScoped(scope)?.FindKeyframes(name) : null) ??
            globalSheets[2]?.FindKeyframes(name) ?? globalSheets[1]?.FindKeyframes(name) ?? globalSheets[0]?.FindKeyframes(name);
        if (rule is null) return null;
        var style = GetStyle(element);
        var parent = ParentStyle(element);
        var lengths = new RelativeLengths(style.FontSize, style.RootFontSize, viewport, () => FontMetricsResolver?.Invoke(style) ?? new FontRelativeMetrics());
        var frames = new List<ResolvedKeyframe>();
        foreach (var frame in rule.Keyframes.OrderBy(f => f.Offset))
        {
            var values = new Dictionary<StyleProperty, object>();
            foreach (var declaration in frame.Declarations)
            {
                switch (declaration)
                {
                    case ValueDeclaration value:
                        Set(value.Property, value.Value);
                        break;
                    case KeywordDeclaration keyword:
                        Set(keyword.Property, keyword.Keyword);
                        break;
                    case PendingDeclaration pending:
                        foreach (var (property, value) in VariableResolver.ParsePending(pending, style.CustomProperties)) Set(property, value);
                        break;
                }
            }
            frames.Add(new ResolvedKeyframe(frame.Offset, frame.Easing, values));

            void Set(StyleProperty property, object value)
            {

                if (!property.Animatable) return;
                value = value switch
                {
                    GlobalKeyword.Inherit => parent.Get(property),
                    GlobalKeyword.Initial => property.InitialValue,
                    GlobalKeyword.Unset => property.Inherited ? parent.Get(property) : property.InitialValue,
                    _ => RelativeLengths.NeedsComputing(property, value) ? lengths.Compute(property, value) : value,
                };
                if (value is Color { IsCurrentColor: true }) value = style.Color;
                if (value is NativeColor { Value.IsCurrentColor: true }) value = new NativeColor(style.Color);
                if (value is ValueList<BoxShadow> shadows && shadows.Items.Any(s => s.Color.IsCurrentColor))
                    value = new ValueList<BoxShadow>(shadows.Items.Select(s => s.Color.IsCurrentColor ? s with { Color = style.Color } : s));
                values[property] = value;
            }
        }
        return frames;
    }

    private ComputedStyle ParentStyle(ElementNode element) =>
        element.ParentElement is { } parent ? GetStyle(parent) : RootStyle;

    private ComputedStyle RootStyle => rootStyle ??= CreateRootStyle();

    private ComputedStyle CreateRootStyle()
    {
        var variables = new Dictionary<string, IReadOnlyList<StyleToken>>(ColorsOf(preferences).CustomProperties, StringComparer.Ordinal);
        if (systemButtonsHeight is { } height)
            variables["--nz-window-controls-height"] = [.. StyleTokenizer.Tokenize(height.ToString(CultureInfo.InvariantCulture) + "px").Where(t => t.Kind != StyleTokenKind.EndOfFile)];
        return ComputedStyle.Initial.WithCustomProperties(variables);
    }

    private ComputedStyle Compute(ElementNode element, ComputedStyle parent, PseudoElement target = PseudoElement.None)
    {
        var matched = matchedRules;
        matched.Clear();
        for (var level = 0; level < globalSheets.Length; level++)
            if (globalSheets[level] is { } sheet) sheet.Index.Match(element, viewport, preferences, level, matched, target: target);
        var scopeDistance = 0;
        foreach (var scope in ElementScopes(element))
        {
            if (StyleRegistry.GetScoped(scope) is not { } scoped) continue;
            if (scoped.HasMediaQueries) scopedInUse.Add(scoped);
            scopedSeen.Add(scoped);
            scoped.Index.Match(element, viewport, preferences, (int)StyleLevel.Component, matched, scope, scopeDistance++, target);
        }
        matched.Sort(static (a, b) =>
        {
            var byLevel = a.Level.CompareTo(b.Level);
            if (byLevel != 0) return byLevel;
            var bySpecificity = a.Rule.Selector.Specificity.CompareTo(b.Rule.Selector.Specificity);
            if (bySpecificity != 0) return bySpecificity;
            var byScope = b.ScopeDistance.CompareTo(a.ScopeDistance);
            return byScope != 0 ? byScope : a.Rule.Order.CompareTo(b.Rule.Order);
        });

        var inline = target == PseudoElement.None ? element.Attributes.GetValueOrDefault("style") as string : null;
        var hash = new HashCode();
        foreach (var match in matched) hash.Add(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(match.Rule));
        var key = (parent, element.Name, inline, element.ParentElement is null, hash.ToHashCode());
        var share = target == PseudoElement.None && sharingCascades && string.IsNullOrEmpty(inline);
        if (share && cascades.TryGetValue(key, out var cached) && SameRules(cached.Matched, matched))
        {
            computeUsedParent = cached.Parent;
            computeUsedViewport = cached.Viewport;
            return cached.Style;
        }
        IReadOnlyList<StyleDeclaration> inlineDeclarations = inline is { Length: > 0 } ? InlineStyle.Parse(inline) : [];
        Dictionary<string, IReadOnlyList<StyleToken>>? rawCustom = null;
        foreach (var match in matched) rawCustom = CollectCustom(match.Rule.Declarations, parent, rawCustom);
        rawCustom = CollectCustom(inlineDeclarations, parent, rawCustom);
        var custom = rawCustom is null ? parent.CustomProperties : ResolveVariables(rawCustom);

        var specified = specifiedValues;
        Array.Clear(specified);
        if (target == PseudoElement.None && element.Name == "WindowContent")
        {
            specified[StyleProperties.OverflowX.Id] = Overflow.Auto;
            specified[StyleProperties.OverflowY.Id] = Overflow.Auto;
        }
        bool[]? relative = null;
        foreach (var match in matched) ApplyDeclarations(match.Rule.Declarations, custom, ref relative);
        ApplyDeclarations(inlineDeclarations, custom, ref relative);

        var values = specified;
        computeUsedParent = false;
        for (var propertyIndex = 0; propertyIndex < StyleProperties.All.Count; propertyIndex++)
        {
            var property = StyleProperties.All[propertyIndex];
            var applies = target == PseudoElement.None ? property != StyleProperties.Content && StyleProperties.AppliesTo(property, element.Name, element.ParentElement is null)
                : AppliesToPseudo(property, target);
            var value = applies ? specified[property.Id] : null;
            if (target == PseudoElement.Selection && property == StyleProperties.BackgroundColor && value is null)
                value = (element.States & ElementStates.Focus) != 0 ? new Color(0x00, 0x78, 0xd4, 0x60) : new Color(0x80, 0x80, 0x80, 0x40);
            if (value is GlobalKeyword.Inherit && !property.Inherited) computeUsedParent = true;
            values[property.Id] = value switch
            {
                null => property.Inherited ? parent.Get(property) : property.InitialValue,
                GlobalKeyword.Inherit => parent.Get(property),
                GlobalKeyword.Initial => property.InitialValue,
                GlobalKeyword.Unset => property.Inherited ? parent.Get(property) : property.InitialValue,
                _ => value,
            };
        }

        computeUsedViewport = false;
        var rootFontSize = parent.RootFontSize;
        if (relative is not null)
        {
            var lengths = new RelativeLengths(parent.FontSize, parent.RootFontSize, viewport, () => FontMetricsResolver?.Invoke(parent) ?? new FontRelativeMetrics());
            var fontSize = StyleProperties.FontSize;
            if (relative[fontSize.Id]) values[fontSize.Id] = lengths.Compute(fontSize, values[fontSize.Id]);
            lengths.FontSize = ((Length)values[fontSize.Id]).Value;
            if (target == PseudoElement.None && element.ParentElement is null) lengths.RootFontSize = lengths.FontSize;
            lengths.FontMetrics = () => FontMetricsResolver?.Invoke(new ComputedStyle((object[])values.Clone(), custom, lengths.RootFontSize)) ?? new FontRelativeMetrics();
            for (var i = 0; i < StyleProperties.All.Count; i++)
                if (relative[i] && StyleProperties.All[i] != fontSize) values[i] = lengths.Compute(StyleProperties.All[i], values[i]);
            computeUsedViewport = lengths.UsedViewport;
        }
        if (target == PseudoElement.None && element.ParentElement is null) rootFontSize = ((Length)values[StyleProperties.FontSize.Id]).Value;

        if (values[StyleProperties.Color.Id] is Color { IsCurrentColor: true }) values[StyleProperties.Color.Id] = parent.Get((StyleProperty)StyleProperties.Color);
        var colorValue = values[StyleProperties.Color.Id];
        var color = (Color)colorValue;
        for (var propertyIndex = 0; propertyIndex < StyleProperties.All.Count; propertyIndex++)
        {
            var property = StyleProperties.All[propertyIndex];
            if (values[property.Id] is NativeColor { Value.IsCurrentColor: true }) values[property.Id] = new NativeColor(color);
            if (values[property.Id] is Color { IsCurrentColor: true }) values[property.Id] = colorValue;
            if (values[property.Id] is ValueList<BoxShadow> shadows && shadows.Items.Any(s => s.Color.IsCurrentColor))
                values[property.Id] = ResolveShadowColors(shadows, color);
            if (values[property.Id] is WindowFrameShadows { Shadows: { } frameShadows } && frameShadows.Items.Any(s => s.Color.IsCurrentColor))
                values[property.Id] = new WindowFrameShadows(ResolveShadowColors(frameShadows, color));
        }

        var computed = ComputedStyle.Create(values, custom, rootFontSize, target == PseudoElement.None ? element.GetFeature<ElementStyle>()?.Computed : null);
        Array.Clear(specifiedValues);
        if (share && cascades.Count < 256) cascades[key] = new(matched.ToArray(), computed, computeUsedParent, computeUsedViewport);
        return computed;
    }

    internal static bool AppliesToPseudo(StyleProperty property, PseudoElement target) => target switch
    {
        PseudoElement.Selection => property == StyleProperties.Color || property == StyleProperties.BackgroundColor,
        PseudoElement.Placeholder => property.Name is "font-family" or "font-size" or "font-style" or "font-weight" or "line-height" or
            "letter-spacing" or "white-space" or "overflow-wrap" or "text-transform" or "text-align" or "color" or "opacity" or
            "text-decoration-line" or "text-decoration-color" or "text-decoration-style" or "text-decoration-thickness",
        _ => StyleProperties.AppliesTo(property, "::generated", false),
    };

    private static ValueList<BoxShadow> ResolveShadowColors(ValueList<BoxShadow> shadows, Color color) =>
        new(shadows.Items.Select(s => s.Color.IsCurrentColor ? s with { Color = color } : s));

    private static Dictionary<string, IReadOnlyList<StyleToken>>? CollectCustom(IReadOnlyList<StyleDeclaration> declarations,
        ComputedStyle parent, Dictionary<string, IReadOnlyList<StyleToken>>? raw)
    {
        for (var i = 0; i < declarations.Count; i++)
            if (declarations[i] is CustomPropertyDeclaration property)
                (raw ??= new Dictionary<string, IReadOnlyList<StyleToken>>(parent.CustomProperties, StringComparer.Ordinal))[property.Name] = property.Tokens;
        return raw;
    }

    private void ApplyDeclarations(IReadOnlyList<StyleDeclaration> declarations, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> custom, ref bool[]? relative)
    {
        for (var i = 0; i < declarations.Count; i++)
        {
            switch (declarations[i])
            {
                case ValueDeclaration value:
                    specifiedValues[value.Property.Id] = value.Value;
                    MarkRelative(value.Property, value.IsRelative, ref relative);
                    break;
                case KeywordDeclaration keyword:
                    specifiedValues[keyword.Property.Id] = keyword.Keyword;
                    MarkRelative(keyword.Property, false, ref relative);
                    break;
                case PendingDeclaration pending:
                    foreach (var (property, value, isRelative) in ResolvePending(pending, custom))
                    {
                        specifiedValues[property.Id] = value;
                        MarkRelative(property, isRelative, ref relative);
                    }
                    break;
            }
        }
    }

    private void MarkRelative(StyleProperty property, bool isRelative, ref bool[]? relative)
    {
        if (isRelative && relative is null)
        {
            relative = relativeValues;
            Array.Clear(relative);
        }
        if (relative is not null) relative[property.Id] = isRelative;
    }

    private static bool SameRules((int Level, StyleRule Rule, int ScopeDistance)[] left, List<(int Level, StyleRule Rule, int ScopeDistance)> right)
    {
        if (left.Length != right.Count) return false;
        for (var i = 0; i < left.Length; i++)
            if (!ReferenceEquals(left[i].Rule, right[i].Rule)) return false;
        return true;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> ResolveVariables(Dictionary<string, IReadOnlyList<StyleToken>> raw)
    {
        if (rawVariables is not null && rawVariables.Count == raw.Count)
        {
            var equal = true;
            foreach (var (name, tokens) in raw)
                if (!rawVariables.TryGetValue(name, out var previous) || !ReferenceEquals(tokens, previous)) { equal = false; break; }
            if (equal) return resolvedVariables!;
        }
        rawVariables = raw;
        return resolvedVariables = VariableResolver.Resolve(raw);
    }

    private (StyleProperty Property, object Value, bool Relative)[] ResolvePending(PendingDeclaration pending, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> custom)
    {
        if (!VariableResolver.ContainsVar(pending.Tokens))
            return constantPending.GetValue(pending, static declaration => Parse(declaration, ComputedStyle.Initial.CustomProperties));
        var key = (pending, custom);
        if (sharingCascades && pendingValues.TryGetValue(key, out var cached)) return cached;
        var values = Parse(pending, custom);
        if (sharingCascades && pendingValues.Count < 1024) pendingValues[key] = values;
        return values;

        static (StyleProperty Property, object Value, bool Relative)[] Parse(PendingDeclaration declaration, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> variables) =>
            VariableResolver.ParsePending(declaration, variables).Select(pair => (pair.Property, pair.Value, RelativeLengths.NeedsComputing(pair.Property, pair.Value))).ToArray();
    }

    /// <summary>
    /// How the style of an element came to be, for developer tools: the cascade made again from the sheets on request
    /// (never during a frame), keeping what <see cref="Compute" /> throws away.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting StyleTrace value.</returns>
    public StyleTrace Trace(ElementNode element)
    {
        if (element.IsRemoved) throw new ArgumentException("The element was removed from the tree.", nameof(element));
        var computed = GetStyle(element);

        var candidates = new List<(int Level, StyleSheet Sheet, StyleRule Rule, int ScopeDistance)>();
        for (var level = 0; level < globalSheets.Length; level++)
            if (globalSheets[level] is { } sheet) Add(sheet, level);
        var scopeDistance = 0;
        foreach (var scope in ElementScopes(element))
            if (StyleRegistry.GetScoped(scope) is { } scoped) Add(scoped, (int)StyleLevel.Component, scope, scopeDistance++);
        candidates.Sort(static (a, b) =>
        {
            var byLevel = a.Level.CompareTo(b.Level);
            if (byLevel != 0) return byLevel;
            var bySpecificity = a.Rule.Selector.Specificity.CompareTo(b.Rule.Selector.Specificity);
            if (bySpecificity != 0) return bySpecificity;
            var byScope = b.ScopeDistance.CompareTo(a.ScopeDistance);
            return byScope != 0 ? byScope : a.Rule.Order.CompareTo(b.Rule.Order);
        });
        var active = candidates.Select(c => c.Rule.Media is null || c.Rule.Media.Matches(viewport, preferences)).ToList();
        IReadOnlyList<StyleDeclaration> inline = element.Attributes.GetValueOrDefault("style") is string { Length: > 0 } text ? InlineStyle.Parse(text) : [];

        var winners = new Dictionary<string, (int Rule, int Declaration)>(StringComparer.Ordinal);
        var longhands = new Dictionary<StyleDeclaration, List<string>>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i <= candidates.Count; i++)
        {
            var declarations = i < candidates.Count ? candidates[i].Rule.Declarations : inline;
            for (var j = 0; j < declarations.Count; j++)
            {
                var names = longhands[declarations[j]] = Longhands(declarations[j], computed.CustomProperties);
                if (i == candidates.Count || active[i])
                    foreach (var name in names) winners[name] = (i < candidates.Count ? i : -1, j);
            }
        }

        var rules = candidates.Select((c, i) => new TracedRule((StyleLevel)c.Level, c.Sheet.Source, c.Rule.Selector.ToString(), c.Rule.Selector.Specificity,
            c.Rule.Media?.ToString(), active[i], c.Rule.Order, Declarations(c.Rule.Declarations, i))).ToList();
        var values = StyleProperties.All.Select(property => Value(property.Name, computed.Get(property)?.ToString() ?? "", property.Inherited)).ToList();
        var custom = computed.CustomProperties.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => Value(p.Key, StyleToken.Serialize(p.Value), inherited: true)).ToList();
        return new StyleTrace(rules, Declarations(inline, -1), values, custom);

        void Add(StyleSheet sheet, int level, Type? scope = null, int scopeDistance = 0)
        {
            foreach (var rule in sheet.Rules)
                if (scope is null ? rule.Selector.Matches(element) : rule.Selector.MatchesScoped(element, scope)) candidates.Add((level, sheet, rule, scopeDistance));
        }

        List<TracedDeclaration> Declarations(IReadOnlyList<StyleDeclaration> declarations, int rule) => [.. declarations.Select((declaration, j) =>
        {
            var written = declaration.ToString() ?? "";
            var colon = written.IndexOf(": ", StringComparison.Ordinal);
            var names = longhands[declaration];
            return new TracedDeclaration(colon < 0 ? written : written[..colon], colon < 0 ? "" : written[(colon + 2)..], names,
                [.. names.Where(name => winners.TryGetValue(name, out var winner) && winner == (rule, j))]);
        })];

        TracedValue Value(string name, string value, bool inherited) => winners.TryGetValue(name, out var winner)
            ? new TracedValue(name, value, winner.Rule < 0 ? ValueSource.Inline : ValueSource.Rule, winner.Rule < 0 ? null : winner.Rule, winner.Declaration)
            : new TracedValue(name, value, inherited ? ValueSource.Inherited : ValueSource.Initial);
    }

    private static List<string> Longhands(StyleDeclaration declaration, IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> custom) => declaration switch
    {
        ValueDeclaration value => [value.Property.Name],
        KeywordDeclaration keyword => [keyword.Property.Name],
        PendingDeclaration pending => [.. VariableResolver.ParsePending(pending, custom).Select(p => p.Property.Name)],
        CustomPropertyDeclaration customProperty => [customProperty.Name],
        _ => [],
    };

    private static IEnumerable<Type> ElementScopes(ElementNode element)
    {
        Type? first = null;
        HashSet<Type>? seen = null;
        for (Node? node = element; node is not null; node = node.Parent)
        {
            var scope = node switch { ElementNode visual => visual.Scope, ComponentNode component => component.DeclaringScope, _ => null };
            if (scope is null || scope == first) continue;
            if (first is null) { first = scope; yield return scope; }
            else if ((seen ??= [first]).Add(scope)) yield return scope;
        }
    }

    private void Collect(StyleSheet sheet, int level, ElementNode element, List<(int, StyleRule, int)> matched) =>
        sheet.Index.Match(element, viewport, preferences, level, matched);

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        renderer.BatchCompleted -= OnBatchCompleted;
        fontMetricsResolver = null;
    }
}

internal static class VariableResolver
{
    public static IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> Resolve(Dictionary<string, IReadOnlyList<StyleToken>> raw)
    {
        if (!raw.Values.Any(ContainsVar)) return raw;
        var resolved = new Dictionary<string, IReadOnlyList<StyleToken>>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var invalid = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in raw.Keys) ResolveOne(name);
        return resolved;

        IReadOnlyList<StyleToken>? ResolveOne(string name)
        {
            if (resolved.TryGetValue(name, out var done)) return done;
            if (invalid.Contains(name) || !raw.TryGetValue(name, out var tokens)) return null;
            if (!visiting.Add(name))
            {
                invalid.Add(name);
                return null;
            }
            var result = Substitute(tokens, ResolveOne);
            visiting.Remove(name);
            if (result is null || invalid.Contains(name))
            {
                invalid.Add(name);
                return null;
            }
            resolved[name] = result;
            return result;
        }
    }

    public static IEnumerable<(StyleProperty Property, object Value)> ParsePending(PendingDeclaration pending,
        IReadOnlyDictionary<string, IReadOnlyList<StyleToken>> custom)
    {
        var substituted = Substitute(pending.Tokens, name => custom.TryGetValue(name, out var tokens) ? tokens : null);
        var shorthand = StyleProperties.TryGetShorthand(pending.PropertyName, out var s) ? s : null;
        var longhands = shorthand?.Longhands ?? (StyleProperties.TryGet(pending.PropertyName, out var p) ? [p] : []);
        if (substituted is not null)
        {
            try
            {
                var reader = new TokenReader(substituted);
                if (reader.Peek().Kind == StyleTokenKind.Ident && TryKeyword(reader.Peek().Value, out var keyword) && new TokenReader(substituted).SkipOne().AtEnd)
                    return longhands.Select(l => (l, (object)keyword));
                return shorthand is not null ? shorthand.Expand(reader) : longhands.Select(l => (l, l.Parse(reader))).ToArray();
            }
            catch (StyleValueException)
            {
            }
        }

        return longhands.Select(l => (l, (object)GlobalKeyword.Unset));
    }

    private static bool TryKeyword(string value, out GlobalKeyword keyword)
    {
        keyword = value.ToLowerInvariant() switch
        {
            "inherit" => GlobalKeyword.Inherit,
            "initial" => GlobalKeyword.Initial,
            "unset" => GlobalKeyword.Unset,
            _ => (GlobalKeyword)255,
        };
        return (byte)keyword != 255;
    }

    private static TokenReader SkipOne(this TokenReader reader)
    {
        reader.Next();
        return reader;
    }

    public static bool ContainsVar(IReadOnlyList<StyleToken> tokens) =>
        tokens.Any(t => t.Kind == StyleTokenKind.Function && t.Value.Equals("var", StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<StyleToken>? Substitute(IReadOnlyList<StyleToken> tokens, Func<string, IReadOnlyList<StyleToken>?> lookup)
    {
        if (!ContainsVar(tokens)) return tokens;
        var output = new List<StyleToken>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind != StyleTokenKind.Function || !token.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
            {
                output.Add(token);
                continue;
            }

            var depth = 1;
            var j = i + 1;
            var comma = -1;
            for (; j < tokens.Count && depth > 0; j++)
            {
                if (tokens[j].Kind is StyleTokenKind.Function or StyleTokenKind.LeftParen) depth++;
                else if (tokens[j].Kind == StyleTokenKind.RightParen) depth--;
                else if (tokens[j].Kind == StyleTokenKind.Comma && depth == 1 && comma < 0) comma = j;
            }
            var end = j - 1;
            var nameToken = tokens.Skip(i + 1).Take((comma < 0 ? end : comma) - i - 1).Where(t => t.Kind != StyleTokenKind.Whitespace).ToArray();
            if (nameToken is not [{ Kind: StyleTokenKind.Ident } name] || !StyleProperties.IsCustomProperty(name.Value)) return null;
            var value = lookup(name.Value);
            if (value is null && comma >= 0)
                value = Substitute(tokens.Skip(comma + 1).Take(end - comma - 1).ToArray(), lookup);
            if (value is null) return null;
            output.AddRange(value);
            i = end;
        }
        return output;
    }
}
