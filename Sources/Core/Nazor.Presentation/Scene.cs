using System.Text;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Layout;
using Nazor.Rendering;
using Nazor.Style;
using GraphicsColor = Nazor.Graphics.Color;
using GraphicsSize = Nazor.Graphics.Size;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Presentation;

/// <summary>
/// Scroll position of an element whose <c>overflow-x</c> or <c>overflow-y</c> is <c>auto</c> or <c>scroll</c>. An
/// axis that does not scroll has offset and maximum 0.
/// </summary>
public sealed class ScrollState
{
    /// <summary>Gets the horizontal offset in logical units.</summary>
    public float OffsetX { get; internal set; }

    /// <summary>Largest horizontal offset: content width minus viewport width (0 when everything fits).</summary>
    public float MaxOffsetX { get; internal set; }

    /// <summary>Gets the logical width of the visible scroll viewport.</summary>
    public float ViewportWidth { get; internal set; }

    /// <summary>Gets the logical width of the scrollable content.</summary>
    public float ContentWidth { get; internal set; }

    /// <summary>Gets the vertical offset in logical units.</summary>
    public float OffsetY { get; internal set; }

    /// <summary>Largest offset: content height minus viewport height (0 when everything fits).</summary>
    public float MaxOffsetY { get; internal set; }

    /// <summary>Gets the logical height of the visible scroll viewport.</summary>
    public float ViewportHeight { get; internal set; }

    /// <summary>Gets the logical height of the scrollable content.</summary>
    public float ContentHeight { get; internal set; }

    /// <summary>Gets whether the content overflows the viewport horizontally.</summary>
    public bool CanScrollX => MaxOffsetX > 0;

    /// <summary>Gets whether the content overflows the viewport vertically.</summary>
    public bool CanScrollY => MaxOffsetY > 0;

    /// <summary>The element can scroll on at least one axis.</summary>
    public bool CanScroll => CanScrollX || CanScrollY;
}

/// <summary>
/// Editing state of a text input, owned by the input system and drawn by the scene: caret, selection (between
/// <see cref="SelectionAnchor" /> and <see cref="CaretIndex" />), the IME composition shown inline at the caret, and
/// the horizontal scroll that keeps the caret visible. Indices are UTF-16 positions in <see cref="Value" />.
/// </summary>
public sealed class TextEditState
{
    /// <summary>The value the editor last knew (from the element or produced by an edit).</summary>
    public string Value { get; set; } = "";

    /// <summary>Gets or sets the UTF-16 caret offset in the text.</summary>
    public int CaretIndex { get; set; }

    /// <summary>Gets or sets the UTF-16 offset at which the text selection is anchored.</summary>
    public int SelectionAnchor { get; set; }

    /// <summary>Preedit text of an IME composition, inserted at the caret while composing.</summary>
    public string? Composition { get; set; }

    /// <summary>Caret position inside <see cref="Composition" />.</summary>
    public int CompositionCursor { get; set; }

    /// <summary>Gets or sets the horizontal text scroll offset in logical units.</summary>
    public float ScrollX { get; set; }

    /// <summary>The vertical scroll of a text box on more lines.</summary>
    public float ScrollY { get; set; }

    /// <summary>The x the caret keeps while it moves up and down between lines.</summary>
    public float? DesiredX { get; set; }

    /// <summary>Gets the smaller UTF-16 offset of the selection endpoints.</summary>
    public int SelectionStart => Math.Min(CaretIndex, SelectionAnchor);

    /// <summary>Gets the larger UTF-16 offset of the selection endpoints.</summary>
    public int SelectionEnd => Math.Max(CaretIndex, SelectionAnchor);

    /// <summary>Gets whether the text selection contains at least one character.</summary>
    public bool HasSelection => CaretIndex != SelectionAnchor;

    /// <summary>The caret position in the displayed text (value plus composition).</summary>
    public int DisplayCaret => Composition is { Length: > 0 } ? Math.Min(CaretIndex, Value.Length) + CompositionCursor : CaretIndex;

    /// <summary>The states before the last edits, the latest last (Ctrl+Z goes back to it).</summary>
    public List<TextSnapshot> UndoStates { get; } = [];

    /// <summary>The states undone, the latest last (Ctrl+Y goes forward to it).</summary>
    public List<TextSnapshot> RedoStates { get; } = [];

    /// <summary>Where the characters typed last end: typing there continues the same undo step.</summary>
    public int? TypingEnd { get; set; }
}

/// <summary>A value of a text box with its caret and selection, kept for undo and redo.</summary>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="CaretIndex">The caret index (int).</param>
/// <param name="SelectionAnchor">The selection anchor (int).</param>
public readonly record struct TextSnapshot(string Value, int CaretIndex, int SelectionAnchor);

internal sealed class ElementLayout
{
    public required LayoutNode Node;
    public ComputedStyle? Style;
    public TextContent? Text;
    public ComputedStyle? PlaceholderStyle;
    public ElementNode? Before, After;
    public IReadOnlyList<Node>? VisualChildren;
    public ScrollState? Scroll;

    public bool Animated;

    public ImageState? Image;

    public Dictionary<string, ImageState>? Backgrounds;

    public Point? OverlayShift;
    public Point? OverlayOrigin;
    public bool? DropDownAbove;
    public Rect? DropDownAnchor, DropDownArea;
    public string? DropDownPreference;
    public bool PopupConstrained;
    public Rect? NativePopupBounds;
    public float NativePopupShadow;
    public Rect? ReportedBox;
    public Size? ReportedContentSize;
    public readonly Dictionary<TextNode, (LayoutNode Node, TextContent Text)> AnonymousText = [];
    public InlineFlow? Inline;
    public PushClip? ContentClip;
}

/// <summary>
/// One drawable surface: keeps style, layout and drawing of the elements rendered by a component renderer in sync.
/// <see cref="Update" /> resolves styles and lays out the tree for the viewport; <see cref="BuildDisplayList" />
/// produces the drawing commands; <see cref="HitTest(Point)" /> finds the element under a point. Text is measured and
/// drawn through the same <see cref="TextLayout" />.
/// </summary>
public sealed partial class Scene : IDisposable
{
    private readonly Renderer renderer;
    private readonly IFontProvider fonts;
    private readonly ITextShaper shaper;
    private readonly ShapingCache shapes;
    private readonly LayoutNode root = new() { Tag = "viewport" };
    private readonly Dictionary<TextNode, (LayoutNode Node, TextContent Text)> rootText = [];
    private float layoutScale = float.NaN;
    private bool layoutWasAnimated;

    private readonly ImageCache images;

    private readonly List<ElementNode> overlays = [];
    private readonly List<LayoutNode> overlayNodes = [];
    private readonly HashSet<ElementNode> scrollChanged = new(ReferenceEqualityComparer.Instance);
    private readonly List<(ElementNode Element, ResizeEventArgs Args)> resizeChanged = [];
    private readonly List<(ElementNode Element, string Name, EventArgs Args)> imageEvents = [];

    /// <summary>Initializes a new instance of Scene.</summary>
    /// <param name="renderer">The renderer that owns the component tree.</param>
    /// <param name="fonts">The provider used to resolve font faces and fallback glyphs.</param>
    /// <param name="shaper">The service that converts text into positioned glyphs.</param>
    /// <param name="styles">The styles (StyleEngine?).</param>
    /// <param name="clock">The clock used to measure animation time.</param>
    /// <param name="images">The image decoder used for raster or vector image content.</param>
    public Scene(Renderer renderer, IFontProvider fonts, ITextShaper shaper, StyleEngine? styles = null, IAnimationClock? clock = null,
        IImageDecoder? images = null)
    {
        this.renderer = renderer;
        this.fonts = fonts;
        this.shaper = shaper;
        shapes = new ShapingCache(fonts, shaper);
        this.images = new ImageCache(images);
        this.images.Completed += () => ImageLoaded?.Invoke();
        Styles = styles ?? new StyleEngine(renderer);
        Styles.FontMetricsResolver = MeasureFontRelativeUnits;
        Transitions = new TransitionEngine(clock ?? new SystemAnimationClock());
        renderer.BatchCompleted += DetachOverlayMotion;
        renderer.BatchCompleted += DetachLayoutMotion;
    }

    /// <summary>Gets the style engine used to compute element styles.</summary>
    public StyleEngine Styles { get; }

    /// <summary>The fonts and shaper used by the scene, also used for diagnostic labels.</summary>
    public IFontProvider Fonts => fonts;

    /// <summary>Gets the service used to shape text into positioned glyphs.</summary>
    public ITextShaper TextShaper => shaper;

    /// <summary>The logical window's open overlays, the last one on top, including overlays presented on native popup surfaces.</summary>
    public IReadOnlyList<ElementNode> Overlays => overlays.Where(o => !o.IsRemoved && IsOverlay(o) && IsRendered(o)).ToArray();

    /// <summary>Gets whether the element is an open overlay laid out apart from its parent.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public static bool IsOverlay(ElementNode element) => element.Attributes.GetValueOrDefault("overlay") is true;

    /// <summary>
    /// Raised on a thread-pool thread when an image load ends: the host asks for a frame on the UI thread, where the
    /// next <see cref="Update" /> lays the image out and reports it to its element.
    /// </summary>
    public event Action? ImageLoaded;

    internal Task<LoadedImage> LoadImage(ImageSource source, float scale) => images.Load(source, scale);

    /// <summary>
    /// Completes when the images that the elements are loading (Image, Icon, url() backgrounds) have loaded or
    /// failed.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task WhenImagesLoaded()
    {
        var pending = new List<Task>();
        foreach (var node in renderer.RootComponents.SelectMany(r => r.Nodes)) Collect(node);
        return Task.WhenAll(pending);

        void Collect(Node node)
        {
            if (node is not ElementNode element) return;
            var layout = element.GetFeature<ElementLayout>();
            var tasks = (layout?.Backgrounds?.Values.Select(b => b.Task) ?? []).Append(layout?.Image?.Task);
            foreach (var task in tasks)
                if (task is { IsCompleted: false }) pending.Add(task.ContinueWith(_ => { }, TaskScheduler.Default));
            foreach (var child in VisualChildren(element)) Collect(child);
        }
    }

    /// <summary>
    /// Transitions of the animatable values (colors, opacity): started by computed-style changes, sampled once per
    /// <see cref="BuildDisplayList" />. The computed styles stay untouched.
    /// </summary>
    public TransitionEngine Transitions { get; }

    /// <summary>Viewport size in logical units.</summary>
    public GraphicsSize Viewport { get; set; } = new(800, 600);

    /// <summary>Physical pixels per logical unit (DPI / 96).</summary>
    public float Scale { get; set; } = 1;

    /// <summary>Color drawn behind everything.</summary>
    public GraphicsColor Background { get; set; } = GraphicsColor.White;

    /// <summary>Gets the root node of the scene's physical layout tree.</summary>
    public LayoutNode RootLayout => root;

    /// <summary>Work done by the last <see cref="Update" />: styles recomputed and layouts computed.</summary>
    public (int StylesRecomputed, int LayoutComputations) LastUpdate { get; private set; }

    /// <summary>
    /// How long the last <see cref="Update" /> took: the styles, then the rest (layout, scroll states).
    /// </summary>
    public (TimeSpan Styles, TimeSpan Layout) LastUpdateDuration { get; private set; }

    /// <summary>Resolves styles and computes the layout of every element.</summary>
    public void Update()
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        RefreshFontRelativeUnits();
        CaptureLayoutMotion();
        ExpireWheelScrolling();
        RepeatScrollBarButton();

        var window = renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().FirstOrDefault(e => e.Name == "Window");
        var windowSheet = window?.Attributes.GetValueOrDefault("stylesheet") as StyleSheet;
        if (window is not null && !ReferenceEquals(Styles.Window, windowSheet)) Styles.Window = windowSheet;
        Styles.Viewport = new Viewport(Viewport.Width, Viewport.Height);
        Styles.Update();
        if (TickNumberPresentations()) Styles.Update();
        var styled = System.Diagnostics.Stopwatch.GetTimestamp();
        var stylesRecomputed = Styles.LastRecomputedCount;
        var scaleChanged = layoutScale != Scale;
        layoutScale = Scale;

        if (Transitions.IsAnimating(StyleEffects.Layout)) Transitions.Tick();
        var nodes = renderer.RootComponents.SelectMany(r => r.Nodes).ToArray();
        var layouts = ArrangeNodes(nodes, scaleChanged);
        if (TickNumberPresentations())
        {
            Styles.Update();
            layouts += ArrangeNodes(nodes, scaleChanged: false);
        }
        if (PublishPopupPlacement())
        {
            Styles.Update();
            layouts += ArrangeNodes(nodes, scaleChanged: false);
        }
        ApplyLayoutMotion();
        foreach (var node in nodes)
            if (node is ElementNode element) UpdateScrollStates(element);
        layoutWasAnimated = Transitions.IsAnimating(StyleEffects.Layout);
        TickSmoothScrolls();
        PruneScrollBarFades();

        if (Transitions.IsRunning || Transitions.HasAnimations) Transitions.Prune(static owner => owner is ElementNode { IsRemoved: true });
        LastUpdate = (stylesRecomputed, layouts);
        LastUpdateDuration = (System.Diagnostics.Stopwatch.GetElapsedTime(started, styled), System.Diagnostics.Stopwatch.GetElapsedTime(styled));

        if (imageEvents.Count > 0)
        {
            var events = imageEvents.ToArray();
            imageEvents.Clear();
            foreach (var (element, name, args) in events)
                if (!element.IsRemoved) _ = renderer.DispatchEventAsync(element, name, args);
        }
        Updated?.Invoke();
        NotifyOverlayMotionPreferences();
    }

    private int ArrangeNodes(IReadOnlyList<Node> nodes, bool scaleChanged)
    {
        overlayNodes.Clear();
        var flow = LayoutChildren(nodes, rootText, ComputedStyle.Initial, scaleChanged);
        root.SetChildren([.. flow, .. overlayNodes.Where(node => node.Tag is ElementNode element && IsRendered(element))]);
        overlays.RemoveAll(o => o.IsRemoved || !IsOverlay(o) || !IsRendered(o));
        foreach (var node in overlayNodes)
            if (node.Tag is ElementNode element && IsRendered(element) && !overlays.Contains(element)) overlays.Add(element);
        var layouts = LayoutWithScrollBars(root.IsDirty || scaleChanged);
        if (WidenOverlays()) layouts += LayoutWithScrollBars(reset: true);
        if (FitDropDowns()) layouts += LayoutWithScrollBars(reset: true);
        if (PlaceOverlays(arrange: true))
        {
            layouts += LayoutWithScrollBars(reset: true);
            PlaceOverlays(arrange: false);
        }
        return layouts;
    }

    private bool PublishPopupPlacement()
    {
        var changed = false;
        foreach (var overlay in overlays)
        {
            var side = overlay.GetFeature<ElementLayout>() is { OverlayShift: { } shift } layout && OverlayAnchor(overlay) is { } anchor
                ? ArrangedOverlaySide(overlay, layout.Node.RoundedFrame.Offset(shift.X, shift.Y), anchor) : null;
            changed |= PublishState(overlay, ElementStates.PopupAbove, side == "top");
            changed |= PublishState(overlay, ElementStates.PopupLeft, side == "left");
            changed |= PublishState(overlay, ElementStates.PopupRight, side == "right");
            changed |= PublishPopupOrigins(overlay);
        }
        return changed;

        static bool PublishState(ElementNode overlay, ElementStates state, bool active)
        {
            if (overlay.States.HasFlag(state) == active) return false;
            overlay.SetState(state, active);
            return true;
        }
    }

    internal void UpdateMotion()
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (Styles.IsDirty || root.IsDirty || layoutWasAnimated || HasNumberMotion || Transitions.IsAnimating(StyleEffects.Layout) ||
            overlays.Any(overlay => overlay.Attributes.GetValueOrDefault("placement") is not "none") || pressedScrollBarButton is not null ||
            wheelScrollUntil.Values.Any(deadline => deadline <= Transitions.Clock.Now))
        {
            Update();
            return;
        }

        TickSmoothScrolls();
        PruneScrollBarFades();
        LastUpdate = (0, 0);
        LastUpdateDuration = (TimeSpan.Zero, System.Diagnostics.Stopwatch.GetElapsedTime(started));
        Updated?.Invoke();
    }

    /// <summary>Raised at the end of <see cref="Update" />: styles and layout are current.</summary>
    public event Action? Updated;

    /// <summary>
    /// Whether an element is shown: it is in the tree, no ancestor-or-self has <c>display: none</c> and its own <c>visibility</c>
    /// is <c>visible</c>. Only shown elements take the focus.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool IsRendered(ElementNode element)
    {
        if (element.IsRemoved || GetStyle(element).Get(StyleProperties.Visibility) != Visibility.Visible) return false;
        if (element.Attributes.GetValueOrDefault("exiting") is true && element.Attributes.GetValueOrDefault("anchor") is ElementNode anchor &&
            anchor != element && !IsRendered(anchor)) return false;
        if (element.Attributes.GetValueOrDefault("overlay-surface") is Func<ElementNode?> resolve &&
            resolve() is { } surface && surface != element && !IsRendered(surface)) return false;
        for (var current = element; current is not null; current = current.ParentElement)
            if (GetStyle(current).Display == Display.None) return false;
        return true;
    }

    /// <summary>The scroll state of a scroll container, or null for other elements.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting ScrollState? value.</returns>
    public ScrollState? GetScrollState(ElementNode element) => element.GetFeature<ElementLayout>()?.Scroll;

    /// <summary>The nearest ancestor-or-self that can scroll, without crossing the containing overlay surface.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting ElementNode? value.</returns>
    public ElementNode? FindScrollContainer(ElementNode? element)
    {
        for (var current = element; current is not null; current = IsOverlay(current) ? null : current.ParentElement)
            if (GetScrollState(current) is { CanScroll: true }) return current;
        return null;
    }

    /// <summary>
    /// Scrolls by <paramref name="deltaY" /> logical units; returns false when already at the limit.
    /// </summary>
    /// <param name="container">The container (ElementNode).</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(ElementNode container, float deltaY) => ScrollBy(container, 0, deltaY);

    /// <summary>
    /// Requests scrolling by <paramref name="deltaX" /> and <paramref name="deltaY" /> logical units using NSS scroll behavior;
    /// false when the destination is unchanged. Pending relative requests accumulate without snapping.
    /// </summary>
    /// <param name="container">The container (ElementNode).</param>
    /// <param name="deltaX">The horizontal scroll delta in the specified unit.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(ElementNode container, float deltaX, float deltaY) =>
        RequestScroll(container, deltaX, deltaY, relative: true, immediate: false);

    /// <inheritdoc cref="ScrollBy(ElementNode, float, float)"/>
    /// <param name="container">The scroll container.</param>
    /// <param name="deltaX">The horizontal distance in logical units.</param>
    /// <param name="deltaY">The vertical distance in logical units.</param>
    /// <param name="immediate">Bypasses smooth scrolling for direct manipulation.</param>
    public bool ScrollBy(ElementNode container, float deltaX, float deltaY, bool immediate) =>
        RequestScroll(container, deltaX, deltaY, relative: true, immediate);

    /// <summary>
    /// Requests an offset within the container's limits using NSS scroll behavior (null keeps that axis).
    /// Returns true when a destination is accepted, including before the first animated frame.
    /// </summary>
    /// <param name="container">The container (ElementNode).</param>
    /// <param name="x">The x (float?).</param>
    /// <param name="y">The y (float?).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollTo(ElementNode container, float? x, float? y) =>
        RequestScroll(container, x, y, relative: false, immediate: false);

    /// <inheritdoc cref="ScrollTo(ElementNode, float?, float?)"/>
    /// <param name="container">The scroll container.</param>
    /// <param name="x">The horizontal offset, or null to keep that axis.</param>
    /// <param name="y">The vertical offset, or null to keep that axis.</param>
    /// <param name="immediate">Bypasses smooth scrolling for direct manipulation.</param>
    public bool ScrollTo(ElementNode container, float? x, float? y, bool immediate) =>
        RequestScroll(container, x, y, relative: false, immediate);

    /// <summary>
    /// The scroll containers whose offset, viewport or content changed since the last call, by scrolling or by a
    /// layout: the input system tells them with <c>onscroll</c>.
    /// </summary>
    /// <returns>The resulting IReadOnlyList&lt;ElementNode&gt; value.</returns>
    public IReadOnlyList<ElementNode> TakeScrollChanges()
    {
        if (scrollChanged.Count == 0) return [];
        var changed = scrollChanged.Where(e => !e.IsRemoved && GetScrollState(e) is not null).ToList();
        scrollChanged.Clear();
        return changed;
    }

    /// <summary>
    /// The elements listening to <c>onresize</c> whose size or position within their parent changed since the last
    /// call, with logical layout values before pixel snapping: the input system tells them with <c>onresize</c>.
    /// </summary>
    /// <returns>The changed elements and their new box.</returns>
    public IReadOnlyList<(ElementNode Element, ResizeEventArgs Args)> TakeResizeChanges()
    {
        if (resizeChanged.Count == 0) return [];
        var changed = resizeChanged.Where(change => !change.Element.IsRemoved).ToList();
        resizeChanged.Clear();
        return changed;
    }

    /// <summary>
    /// Scrolls the containers around <paramref name="element" /> so that it becomes visible, stopping at the
    /// containing overlay surface so that revealing popup content does not scroll its background.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool BringIntoView(ElementNode element)
    {
        var changed = false;
        if (element.GetFeature<ElementLayout>() is not { } target) return false;
        var bounds = target.Node.RoundedFrame;
        for (var container = IsOverlay(element) ? null : element.ParentElement; container is not null; container = IsOverlay(container) ? null : container.ParentElement)
        {
            if (container.GetFeature<ElementLayout>() is not { Scroll: { CanScroll: true } scroll } layout) continue;
            StopScrolling(container);
            var (offsetX, offsetY) = (scroll.OffsetX, scroll.OffsetY);
            if (scroll.CanScrollY)
            {
                var top = layout.Node.RoundedFrame.Y + layout.Node.Border.Top;
                var visibleTop = top + scroll.OffsetY;
                var visibleBottom = visibleTop + scroll.ViewportHeight;
                if (bounds.Y < visibleTop) changed |= ScrollTo(scroll, bounds.Y - top);
                else if (bounds.Bottom > visibleBottom) changed |= ScrollTo(scroll, bounds.Bottom - top - scroll.ViewportHeight);
            }
            if (scroll.CanScrollX)
            {
                var left = layout.Node.RoundedFrame.X + layout.Node.Border.Left;
                var visibleLeft = left + scroll.OffsetX;
                var visibleRight = visibleLeft + scroll.ViewportWidth;
                if (bounds.X < visibleLeft) changed |= ScrollToX(scroll, bounds.X - left);
                else if (bounds.Right > visibleRight) changed |= ScrollToX(scroll, bounds.Right - left - scroll.ViewportWidth);
            }
            if ((offsetX, offsetY) != (scroll.OffsetX, scroll.OffsetY)) scrollChanged.Add(container);
        }
        return changed;
    }

    private static bool ScrollTo(ScrollState scroll, float offset)
    {
        offset = Math.Clamp(offset, 0, scroll.MaxOffsetY);
        if (offset == scroll.OffsetY) return false;
        scroll.OffsetY = offset;
        return true;
    }

    private static bool ScrollToX(ScrollState scroll, float offset)
    {
        offset = Math.Clamp(offset, 0, scroll.MaxOffsetX);
        if (offset == scroll.OffsetX) return false;
        scroll.OffsetX = offset;
        return true;
    }

    private static bool ListensToResize(ElementNode element)
    {
        foreach (var (key, value) in element.Attributes)
            if (value is IEventCallback { HasDelegate: true } && (key == "onresize" || key.StartsWith("__internal_onresize", StringComparison.Ordinal)))
                return true;
        return false;
    }

    private void UpdateScrollStates(ElementNode element)
    {
        if (element.GetFeature<ElementLayout>() is not { Style: { } style } layout) return;
        if (ListensToResize(element))
        {
            var frame = layout.Node.AbsoluteFrame;
            var parent = element.ParentElement?.GetFeature<ElementLayout>()?.Node.AbsoluteFrame ?? new Rect(0, 0, 0, 0);
            var box = new Rect(frame.X - parent.X, frame.Y - parent.Y, frame.Width, frame.Height);
            var contentSize = new Size(Math.Max(0, layout.Node.ContentWidth), Math.Max(0, frame.Height - layout.Node.Padding.Vertical - layout.Node.Border.Vertical));
            if (layout.ReportedBox != box || layout.ReportedContentSize != contentSize)
            {
                layout.ReportedBox = box;
                layout.ReportedContentSize = contentSize;
                resizeChanged.Add((element, new ResizeEventArgs
                {
                    OffsetLeft = box.X, OffsetTop = box.Y, Width = box.Width, Height = box.Height,
                    ContentWidth = contentSize.Width, ContentHeight = contentSize.Height,
                }));
            }
        }
        else (layout.ReportedBox, layout.ReportedContentSize) = (null, null);
        var scrollX = style.Get(StyleProperties.OverflowX) is Overflow.Auto or Overflow.Scroll;
        var scrollY = style.Get(StyleProperties.OverflowY) is Overflow.Auto or Overflow.Scroll;
        if (scrollX || scrollY)
        {
            var node = layout.Node;
            var created = layout.Scroll is null;
            var scroll = layout.Scroll ??= new ScrollState();
            var before = (scroll.OffsetX, scroll.OffsetY, scroll.ViewportWidth, scroll.ViewportHeight, scroll.ContentWidth, scroll.ContentHeight);
            var (contentRight, contentBottom) = ContentExtent(node);
            var gutter = node.Style.ScrollbarGutter;
            scroll.ViewportHeight = Math.Max(0, node.Frame.Height - node.Border.Vertical - gutter.Vertical);
            scroll.ContentHeight = scrollY ? Math.Max(0, contentBottom + node.Padding.Bottom - node.Border.Top - gutter.Bottom) : scroll.ViewportHeight;
            var overflowY = scroll.ContentHeight - scroll.ViewportHeight;
            scroll.MaxOffsetY = overflowY > 0.01f ? overflowY : 0;
            scroll.OffsetY = Math.Clamp(scroll.OffsetY, 0, scroll.MaxOffsetY);
            scroll.ViewportWidth = Math.Max(0, node.Frame.Width - node.Border.Horizontal - gutter.Horizontal);
            scroll.ContentWidth = scrollX ? Math.Max(0, contentRight + node.Padding.Right - node.Border.Left - gutter.Right) : scroll.ViewportWidth;
            var overflowX = scroll.ContentWidth - scroll.ViewportWidth;
            scroll.MaxOffsetX = overflowX > 0.01f ? overflowX : 0;
            scroll.OffsetX = Math.Clamp(scroll.OffsetX, 0, scroll.MaxOffsetX);
            if (created || before != (scroll.OffsetX, scroll.OffsetY, scroll.ViewportWidth, scroll.ViewportHeight, scroll.ContentWidth, scroll.ContentHeight))
                scrollChanged.Add(element);
        }
        else layout.Scroll = null;
        var children = VisualChildren(element);
        for (var i = 0; i < children.Count; i++)
            if (children[i] is ElementNode childElement) UpdateScrollStates(childElement);
    }

    private static (float Right, float Bottom) ContentExtent(LayoutNode node, bool top = true)
    {
        float right = 0, bottom = 0;
        foreach (var child in node.Children)
        {
            if (child.Style.Display == Display.None) continue;
            if (top && child.Style.Position == Position.Absolute && node.Style.Position == Position.Static) continue;
            right = Math.Max(right, child.Frame.Right + child.Margin.Right);
            bottom = Math.Max(bottom, child.Frame.Bottom + child.Margin.Bottom);
            if (child.Children.Count == 0 || child.Style.OverflowX != Overflow.Visible || child.Style.OverflowY != Overflow.Visible) continue;
            var (innerRight, innerBottom) = ContentExtent(child, top: false);
            right = Math.Max(right, child.Frame.X + innerRight);
            bottom = Math.Max(bottom, child.Frame.Y + innerBottom);
        }
        return (right, bottom);
    }

    /// <summary>
    /// The element's border box in logical window coordinates, snapped to physical pixels: the layout box moved by
    /// the scroll offsets of the ancestors and, with transforms, the bounds of the transformed box (as <c>getBoundingClientRect()</c>).
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect GetBounds(ElementNode element) =>
        element.GetFeature<ElementLayout>() is { } layout ? GetTransform(element).TransformBounds(layout.Node.RoundedFrame) : Rect.Empty;

    /// <summary>
    /// From the element's own coordinates (those of its layout box, <see cref="GetContentBox" />) to the window: its
    /// transform, then the scroll offsets and transforms of its ancestors.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting Matrix value.</returns>
    public Matrix GetTransform(ElementNode element)
    {
        SampleCompositionTime();
        var matrix = Matrix.Identity;
        if (element.GetFeature<ElementLayout>() is { Style: { } style } layout && TransformOf(element, style, layout.Node.RoundedFrame) is { } own) matrix = own;

        for (var ancestor = IsOverlay(element) ? null : element.ParentElement; ancestor is not null; ancestor = IsOverlay(ancestor) ? null : ancestor.ParentElement)
        {
            if (ancestor.GetFeature<ElementLayout>() is not { } ancestorLayout) continue;
            if (ancestorLayout.Scroll is { } scroll) matrix = matrix.Then(Matrix.Translation(-scroll.OffsetX, -scroll.OffsetY));
            if (ancestorLayout.Style is { } ancestorStyle && TransformOf(ancestor, ancestorStyle, ancestorLayout.Node.RoundedFrame) is { } transform)
                matrix = matrix.Then(transform);
        }
        return matrix;
    }

    /// <summary>
    /// The layout box of an element (frame snapped to pixels, padding, border, margin) in its own coordinates, before
    /// scroll offsets and transforms; null when it is not laid out. For tools.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting LayoutNode? value.</returns>
    public LayoutNode? GetLayoutNode(ElementNode element) => element.GetFeature<ElementLayout>()?.Node;

    /// <summary>
    /// The content box of an element in its own coordinates, before scroll offsets and transforms (<see cref="GetTransform" />).
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect GetContentBox(ElementNode element) => element.GetFeature<ElementLayout>()?.Node.ContentBox ?? Rect.Empty;

    private T Effective<T>(ElementNode element, ComputedStyle style, StyleProperty<T> property) where T : notnull =>
        Transitions.GetValue(element, property, style.Get(property));

    private Matrix? TransformOf(ElementNode element, ComputedStyle style, Rect box)
    {
        var own = OwnTransform(element, style, box);
        if (element.GetFeature<ElementLayout>()?.OverlayShift is not { } shift) return own;
        return (own ?? Matrix.Identity).Then(Matrix.Translation(shift.X, shift.Y));
    }

    private Matrix? OwnTransform(ElementNode element, ComputedStyle style, Rect box)
    {
        var functions = Effective(element, style, StyleProperties.Transform);
        if (functions.Items.Count == 0) return null;
        var origin = Effective(element, style, StyleProperties.TransformOrigin);
        var arranged = element.GetFeature<ElementLayout>()?.OverlayOrigin;
        var x = arranged?.X ?? box.X + (float)origin.X.Resolve(box.Width);
        var y = arranged?.Y ?? box.Y + (float)origin.Y.Resolve(box.Height);
        return Matrix.Translation(-x, -y).Then(Compose(functions, box)).Then(Matrix.Translation(x, y));
    }

    internal static Matrix Compose(ValueList<TransformFunction> functions, Rect box)
    {
        var matrix = Matrix.Identity;
        for (var i = functions.Items.Count - 1; i >= 0; i--) matrix = matrix.Then(ToMatrix(functions.Items[i], box));
        return matrix;
    }

    private static Matrix ToMatrix(TransformFunction function, Rect box) => function switch
    {
        MixFunction mix => Matrix.Interpolate(Compose(mix.From, box), Compose(mix.To, box), mix.Progress),
        TranslateFunction t => Matrix.Translation((float)t.X.Resolve(box.Width), (float)t.Y.Resolve(box.Height)),
        ScaleFunction s => Matrix.Scaling((float)s.X, (float)s.Y),
        RotateFunction r => Matrix.Rotation(r.Angle),
        SkewFunction k => Matrix.Skewing(k.X, k.Y),
        MatrixFunction m => new Matrix((float)m.A, (float)m.B, (float)m.C, (float)m.D, (float)m.E, (float)m.F),
        _ => Matrix.Identity,
    };

    /// <summary>Gets the computed style associated with the element.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting ComputedStyle value.</returns>
    public ComputedStyle GetStyle(ElementNode element) => Styles.GetStyle(element);

    /// <summary>
    /// The deepest element containing <paramref name="point" />, honouring clipping and visibility.
    /// </summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting ElementNode? value.</returns>
    public ElementNode? HitTest(Point point)
    {
        SampleCompositionTime();

        for (var i = overlays.Count - 1; i >= 0; i--)
            if (!overlays[i].IsRemoved && HitTest(overlays[i], point) is { } inOverlay) return inOverlay;
        ElementNode? result = null;
        foreach (var node in renderer.RootComponents.SelectMany(r => r.Nodes))
            if (node is ElementNode element) result = HitTest(element, point) ?? result;
        return result;
    }

    private ElementNode? HitTest(ElementNode element, Point point)
    {
        if (element.GetFeature<ElementLayout>() is not { } layout || layout.Style is not { } style || style.Display == Display.None) return null;
        var bounds = layout.Node.RoundedFrame;

        if (TransformOf(element, style, bounds) is { } transform)
        {
            if (!transform.TryInvert(out var inverse)) return null;
            point = inverse.Transform(point);
        }
        if (InsetClip(element, style, bounds) is { } clip && !clip.Contains(point)) return null;
        var clips = style.Get(StyleProperties.OverflowX) != Overflow.Visible || style.Get(StyleProperties.OverflowY) != Overflow.Visible;
        if (clips && !bounds.Contains(point)) return null;
        if (style.Get(StyleProperties.Visibility) == Visibility.Visible && style.Get(StyleProperties.PointerEvents) == PointerEvents.Auto &&
            (GetScrollBar(element, horizontal: false) is { } vertical && vertical.Bounds.Contains(point) ||
             GetScrollBar(element, horizontal: true) is { } horizontal && horizontal.Bounds.Contains(point))) return element;
        if (clips && !layout.Node.Style.ScrollbarGutter.IsZero && !ScrollViewport(layout.Node).Contains(point))
            return style.Get(StyleProperties.Visibility) == Visibility.Visible && style.Get(StyleProperties.PointerEvents) == PointerEvents.Auto ? element : null;
        var childPoint = layout.Scroll is { } scroll ? new Point(point.X + scroll.OffsetX, point.Y + scroll.OffsetY) : point;
        var children = PaintOrder(element, style);
        for (var i = children.Count - 1; i >= 0; i--)
            if (children[i] is ElementNode child && !IsOverlay(child) && HitTest(child, childPoint) is { } hit)
                return ReferenceEquals(hit, layout.Before) || ReferenceEquals(hit, layout.After) ? element : hit;

        return style.Get(StyleProperties.Visibility) == Visibility.Visible && style.Get(StyleProperties.PointerEvents) == PointerEvents.Auto &&
            bounds.Contains(point) && InlineContains(element, point) ? element : null;
    }

    /// <summary>
    /// The value an element shows for an animatable property: the running transition's value at the current frame
    /// time, otherwise the computed value.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="property">The style property to read or update.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The resulting T value.</returns>
    public T GetEffectiveValue<T>(ElementNode element, StyleProperty<T> property) where T : notnull
    {
        SampleCompositionTime();
        return Transitions.GetValue(element, property, GetStyle(element).Get(property));
    }

    /// <summary>
    /// Drawing commands for the current layout, in paint order; samples the transitions once. What cannot be seen
    /// (outside the window, scrolled away or outside a clip) is not drawn.
    /// Circular box decorations retain their original square geometry rather than independently pixel-rounded edges;
    /// layout bounds, clipping, content and hit testing keep their existing rounding.
    /// </summary>
    /// <returns>The resulting DisplayList value.</returns>
    public DisplayList BuildDisplayList()
    {
        Transitions.Tick();
        var builder = displayBuilder;
        var window = new Rect(0, 0, Viewport.Width, Viewport.Height);
        inks.Clear();
        building = true;
        try
        {
            inView = window;
            foreach (var node in renderer.RootComponents.SelectMany(r => r.Nodes))
            {
                if (node is ElementNode element && !IsOverlay(element)) Paint(element, builder);
                else if (node is TextNode text && rootText.TryGetValue(text, out var anonymous)) PaintText(anonymous.Node, anonymous.Text, ComputedStyle.Initial, ComputedStyle.Initial.Color, builder);
            }
            foreach (var overlay in overlays)
            {
                inView = window;
                if (!overlay.IsRemoved && GetNativePopupBounds(overlay) is null) Paint(overlay, builder);
            }
            return builder.Build(Viewport);
        }
        finally
        {
            building = false;
            inks.Clear();
            builder.Reset();
        }
    }

    private readonly DisplayListBuilder displayBuilder = new();

    private static readonly Rect Everywhere = new(-1e9f, -1e9f, 2e9f, 2e9f);
    private readonly Dictionary<ElementNode, Ink> inks = [];

    private readonly record struct Ink(Rect All, Rect Own, Matrix? Transform);
    private Rect inView = Everywhere;
    private bool building;

    /// <summary>Indented element geometry, for tests: name, classes and snapped border box.</summary>
    /// <returns>The resulting string value.</returns>
    public string DescribeLayout()
    {
        var builder = new StringBuilder();
        foreach (var node in renderer.RootComponents.SelectMany(r => r.Nodes))
            if (node is ElementNode element) Describe(element, 0, builder);
        return builder.ToString();
    }

    private static void Describe(ElementNode element, int depth, StringBuilder builder)
    {
        if (element.GetFeature<ElementLayout>() is not { } layout) return;
        builder.Append(' ', depth * 2).Append(element).Append(' ').Append(layout.Node.RoundedFrame).Append('\n');
        foreach (var child in element.Children)
            if (child is ElementNode childElement) Describe(childElement, depth + 1, builder);
    }

    private List<LayoutNode> LayoutChildren(IReadOnlyList<Node> nodes, Dictionary<TextNode, (LayoutNode, TextContent)> anonymous, ComputedStyle parentStyle, bool scaleChanged,
        IReadOnlyList<FontSymbol>? symbols = null, bool symbolText = true)
    {
        if (childLists.Count <= childDepth) childLists.Add([]);
        var result = childLists[childDepth++];
        result.Clear();
        try
        {
            var used = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is ElementNode element)
                {
                    var node = SyncElement(element, scaleChanged);
                    if (IsOverlay(element)) overlayNodes.Add(node);
                    else result.Add(node);
                    continue;
                }

                var first = (TextNode)nodes[i];
                var text = first.Text;
                if (i + 1 < nodes.Count && nodes[i + 1] is TextNode)
                {
                    var joined = new StringBuilder(text);
                    while (i + 1 < nodes.Count && nodes[i + 1] is TextNode next)
                    {
                        joined.Append(next.Text);
                        i++;
                    }
                    text = joined.ToString();
                }
                if (!anonymous.TryGetValue(first, out var entry))
                {
                    var content = new TextContent(fonts, shaper, shapes);
                    var node = new LayoutNode { Tag = first, Measure = content.Measure, Style = LayoutStyle.Default };
                    entry = (node, content);
                    anonymous[first] = entry;
                }
                if (entry.Item2.Update(text, parentStyle, symbols: symbols, symbolText: symbolText)) entry.Item1.MarkDirty();
                used++;
                result.Add(entry.Item1);
            }

            if (anonymous.Count > used)
                foreach (var stale in anonymous.Keys.Where(k => !StartsRun(nodes, k)).ToArray()) anonymous.Remove(stale);
            return result;
        }
        finally
        {
            childDepth--;
        }

        static bool StartsRun(IReadOnlyList<Node> nodes, TextNode text)
        {
            for (var i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], text)) return i == 0 || nodes[i - 1] is not TextNode;
            return false;
        }
    }

    private readonly List<List<LayoutNode>> childLists = [];
    private int childDepth;

    private LayoutNode SyncElement(ElementNode element, bool scaleChanged)
    {
        if (element.GetFeature<InlinePart>() is { } inlinePart) ClearPart(element, inlinePart.Flow);
        var layout = element.GetFeature<ElementLayout>();
        if (layout is null)
        {
            layout = new ElementLayout { Node = new LayoutNode { Tag = element } };
            element.SetFeature(layout);
        }
        var style = Styles.GetStyle(element);
        if (!ReferenceEquals(style, layout.Style) || scaleChanged || layout.Node.Style.ContentInsets != ContentInsets(element) ||
            layout.Node.Style.ScrollbarGutter != RetainedScrollBarGutter(element, style))
        {
            var geometryChanged = layout.Style is null || scaleChanged || layout.Animated ||
                layout.Node.Style.ContentInsets != ContentInsets(element) ||
                layout.Node.Style.ScrollbarGutter != RetainedScrollBarGutter(element, style) ||
                (layout.Style.Compare(style) & (StyleEffects.Layout | StyleEffects.Composite)) != 0;

            if (layout.Style is { } previous && !ReferenceEquals(previous, style)) Transitions.StyleChanged(element, previous, style);

            if (!ReferenceEquals(style, layout.Style) && (style.Get(StyleProperties.Animation).Items.Count > 0 || Transitions.HasAnimationsOn(element)))
                Transitions.UpdateAnimations(element, style, name => Styles.ResolveKeyframes(element, name));
            layout.Style = style;
            layout.Animated = Transitions.IsAnimating(element, StyleEffects.Layout);
            if (geometryChanged || layout.Animated) layout.Node.Style = ToLayoutStyle(element, style);
        }
        else if (layout.Animated || Transitions.IsAnimating(element, StyleEffects.Layout))
        {

            layout.Animated = Transitions.IsAnimating(element, StyleEffects.Layout);
            layout.Node.Style = ToLayoutStyle(element, style);
        }
        if (layout.Node.Style.TablePart != element.Attributes.GetValueOrDefault("table-part") as string ||
            layout.Node.Style.TableRowSpan != (element.Attributes.GetValueOrDefault("row-span") as int? ?? 1) ||
            layout.Node.Style.TableColumnSpan != (element.Attributes.GetValueOrDefault("column-span") as int? ?? 1))
            layout.Node.Style = ToLayoutStyle(element, style);
        if (layout.PopupConstrained)
        {
            layout.Node.Style = ToLayoutStyle(element, style);
            layout.PopupConstrained = false;
        }
        if (element.Attributes.GetValueOrDefault("drop-down") is not string) layout.DropDownAbove = null;
        layout.NativePopupBounds = null;
        SyncBackgrounds(layout, style);
        if (IsOverlay(element) || element.Attributes.GetValueOrDefault("exit-presence") is true) AttachOverlayMotion(element);
        AttachLayoutMotion(element);
        AttachNumberPresentation(element);
        if (IsOverlay(element))
        {
            layout.Node.Style = element.Attributes.GetValueOrDefault("placement") is "none"
                ? layout.Node.Style with { Position = Position.Absolute }
                : layout.Node.Style with
                {
                    Position = Position.Absolute, Top = SizeValue.Fixed(Length.Zero), Left = SizeValue.Fixed(Length.Zero), Right = SizeValue.Auto, Bottom = SizeValue.Auto,
                };
        }
        else layout.OverlayShift = null;
        if (element.Attributes.GetValueOrDefault("source") is ImageSource source)
        {
            SyncImage(element, layout, source);
            return layout.Node;
        }
        layout.Image = null;
        var generated = !TextEditorRoles.IsEditor(element);
        layout.Before = generated ? Styles.GetGeneratedElement(element, PseudoElement.Before) : null;
        layout.After = generated ? Styles.GetGeneratedElement(element, PseudoElement.After) : null;
        layout.VisualChildren = layout.Before is not null || layout.After is not null
            ? [.. layout.Before is { } before ? new Node[] { before } : [], .. element.Children, .. layout.After is { } after ? new Node[] { after } : []]
            : null;
        var children = VisualChildren(element);
        layout.PlaceholderStyle = null;

        if (TrySyncInline(element, layout, style, children)) return layout.Node;

        if (children.Count > 0 && OnlyText(children) && style.Display is not (Display.Flex or Display.Grid))
        {
            if (layout.Node.Children.Count > 0) layout.Node.SetChildren([]);
            layout.AnonymousText.Clear();
            layout.Text ??= new TextContent(fonts, shaper, shapes);
            var raw = children.Count == 1 ? ((TextNode)children[0]).Text : string.Concat(children.Cast<TextNode>().Select(t => t.Text));
            var editState = element.GetFeature<TextEditState>();
            if (editState is { Composition: { Length: > 0 } composition })
            {

                if (element.Attributes.GetValueOrDefault("password-char") is string { Length: 1 } mask) composition = new string(mask[0], composition.Length);
                raw = raw.Insert(Math.Clamp(editState.CaretIndex, 0, raw.Length), composition);
            }

            var edited = editState is not null && TextEditorRoles.IsEditor(element);
            if (raw.Length == 0 && element.Attributes.GetValueOrDefault("placeholder-text") is string { Length: > 0 } placeholder)
            {
                raw = placeholder;
                layout.PlaceholderStyle = Styles.GetPseudoStyle(element, PseudoElement.Placeholder);
            }
            if (layout.Text.Update(raw, layout.PlaceholderStyle ?? style, editable: edited && layout.PlaceholderStyle is null,
                symbols: element.Attributes.GetValueOrDefault("font-symbol-candidates") as IReadOnlyList<FontSymbol>, symbolText: generated) || layout.Node.Measure is null)
                layout.Node.Measure = layout.Text.Measure;
        }
        else
        {
            if (layout.Node.Measure is not null) layout.Node.Measure = null;
            layout.Text = null;
            layout.Node.SetChildren(LayoutChildren(children, layout.AnonymousText, style, scaleChanged,
                element.Attributes.GetValueOrDefault("font-symbol-candidates") as IReadOnlyList<FontSymbol>, symbolText: generated));
        }
        return layout.Node;
    }

    private static IReadOnlyList<Node> VisualChildren(ElementNode element) => element.GetFeature<ElementLayout>()?.VisualChildren ?? element.Children;

    private static bool OnlyText(IReadOnlyList<Node> children)
    {
        for (var i = 0; i < children.Count; i++)
            if (children[i] is not TextNode) return false;
        return true;
    }

    private bool WidenOverlays()
    {
        var changed = false;
        foreach (var overlay in overlays)
        {
            if (overlay.Attributes.GetValueOrDefault("anchor-width") is not true ||
                overlay.Attributes.GetValueOrDefault("anchor") is not ElementNode { IsRemoved: false } anchor || !IsRendered(anchor) ||
                anchor.GetFeature<ElementLayout>() is not { } anchorLayout || overlay.GetFeature<ElementLayout>() is not { Style: { } style } layout) continue;
            var own = style.Get(StyleProperties.MinWidth) is { Kind: SizeKind.Fixed, Length: { Unit: LengthUnit.Px } length } ? (float)length.Value : 0;
            var width = SizeValue.Fixed(Length.Px(Math.Max(own, anchorLayout.Node.RoundedFrame.Width)));
            if (layout.Node.Style.MinWidth == width) continue;
            layout.Node.Style = layout.Node.Style with { MinWidth = width };
            changed = true;
        }
        return changed;
    }

    private bool PlaceOverlays(bool arrange)
    {
        if (arrange) NativePopupLayoutStarting?.Invoke();
        var constrained = false;
        foreach (var overlay in overlays)
        {
            if (overlay.GetFeature<ElementLayout>() is not { } layout) continue;
            layout.OverlayShift = null;
            layout.OverlayOrigin = null;
            var frame = layout.Node.RoundedFrame;
            var (width, height) = (frame.Width, frame.Height);
            float x, y;

            var anchorBounds = OverlayAnchor(overlay);
            var alignmentOffset = OverlayAlignmentOffset(overlay, frame.Y);
            var placement = overlay.Attributes.GetValueOrDefault("placement") as string ?? "bottom";
            if (placement == "none") continue;
            var dash = placement.IndexOf('-');
            var side = dash < 0 ? placement : placement[..dash];
            var align = dash < 0 ? "start" : placement[(dash + 1)..];
            var gap = overlay.Attributes.GetValueOrDefault("overlay-offset") is double offset && double.IsFinite(offset) ? (float)offset : 0;
            // The surface's margin on the side facing the anchor adds to the offset, so a theme can separate drop-downs
            // and menus from their anchor without changing the control.
            var margin = layout.Node.Margin;
            var (gapBelow, gapAbove, gapRight, gapLeft) = (gap + margin.Top, gap + margin.Bottom, gap + margin.Left, gap + margin.Right);
            var external = anchorBounds is not null && OffersNativePopup(overlay);
            if (external)
            {
                width = MathF.Ceiling(layout.Node.Frame.Width * Scale) / Scale;
                height = MathF.Ceiling(layout.Node.Frame.Height * Scale) / Scale;
            }
            var area = external ? NativePopupWorkArea?.Invoke(anchorBounds!.Value) : WindowViewport;
            var visible = area ?? new Rect(-float.MaxValue / 4, -float.MaxValue / 4, float.MaxValue / 2, float.MaxValue / 2);
            if (arrange && anchorBounds is not null && overlay.Attributes.GetValueOrDefault("role") is "menu")
                constrained |= ConstrainPopup(layout, visible.Width, visible.Height);
            if (anchorBounds is { } a)
            {
                var alignX = align switch { "center" => a.X + (a.Width - width) / 2, "end" => a.Right - width, _ => a.X };
                var alignY = align switch { "center" => a.Y + (a.Height - height) / 2, "end" => a.Bottom - height, _ => a.Y };
                (x, y) = alignmentOffset is { } targetOffset
                    ? (alignX, a.Y + a.Height / 2 - targetOffset)
                    : overlay.Attributes.GetValueOrDefault("drop-down") is "center"
                    ? (alignX, a.Y + (a.Height - height) / 2)
                    : layout.DropDownAbove is { } above
                    ? (alignX, above ? a.Y - gapAbove - height : a.Bottom + gapBelow)
                    : side switch
                {
                    "top" => (alignX, a.Y - gapAbove - height < visible.Y && a.Bottom + gapBelow + height <= visible.Bottom ? a.Bottom + gapBelow : a.Y - gapAbove - height),
                    "right" => (a.Right + gapRight + width > visible.Right && a.X - gapLeft - width >= visible.X ? a.X - gapLeft - width : a.Right + gapRight, alignY),
                    "left" => (a.X - gapLeft - width < visible.X && a.Right + gapRight + width <= visible.Right ? a.Right + gapRight : a.X - gapLeft - width, alignY),
                    _ => (alignX, a.Bottom + gapBelow + height > visible.Bottom && a.Y - gapAbove - height >= visible.Y ? a.Y - gapAbove - height : a.Bottom + gapBelow),
                };
            }
            else (x, y) = ((Viewport.Width - width) / 2, (Viewport.Height - height) / 2);
            if (arrange && external)
            {
                var ink = InkBounds(overlay);
                var shadow = MathF.Ceiling(Math.Max(0, Math.Max(Math.Max(frame.X - ink.X, ink.Right - frame.Right),
                    Math.Max(frame.Y - ink.Y, ink.Bottom - frame.Bottom))));
                var desired = new Rect(Math.Clamp(x, visible.X, Math.Max(visible.X, visible.Right - width)),
                    Math.Clamp(y, visible.Y, Math.Max(visible.Y, visible.Bottom - height)),
                    Math.Min(width, visible.Width), Math.Min(height, visible.Height));
                var direction = overlay.Attributes.GetValueOrDefault("drop-down") as string;
                if (direction is "top" or "bottom") side = direction;
                else if (direction == "auto" && layout.DropDownAbove is { } resolvedAbove) side = resolvedAbove ? "top" : "bottom";
                var sideGap = side switch { "top" => gapAbove, "right" => gapRight, "left" => gapLeft, _ => gapBelow };
                if (alignmentOffset is not null || direction == "center")
                {
                    side = "bottom";
                    sideGap = desired.Y - anchorBounds!.Value.Bottom;
                }
                layout.NativePopupBounds = ArrangeNativePopup!(overlay, new(anchorBounds!.Value, desired, shadow, side, align, sideGap));
                layout.NativePopupShadow = shadow;
                if (layout.NativePopupBounds is { } bounds)
                {
                    constrained |= ConstrainPopup(layout, bounds.Width, bounds.Height);
                    if (direction == "auto") layout.DropDownAbove = bounds.Bottom <= anchorBounds.Value.Y + 0.01f;
                }
                else if (overlay.Attributes.GetValueOrDefault("drop-down") is string)
                {
                    constrained |= FitDropDown(overlay, layout, anchorBounds.Value, WindowViewport);
                    y = direction == "center"
                        ? anchorBounds.Value.Y + anchorBounds.Value.Height / 2 - (alignmentOffset ?? height / 2)
                        : layout.DropDownAbove == true ? anchorBounds.Value.Y - gapAbove - height : anchorBounds.Value.Bottom + gapBelow;
                }
            }
            if (layout.NativePopupBounds is { } native) (x, y) = (native.X, native.Y);
            else
            {
                x = Math.Clamp(x, 0, Math.Max(0, Viewport.Width - width));
                y = Math.Clamp(y, 0, Math.Max(0, Viewport.Height - height));
            }
            layout.OverlayShift = new Point(x - frame.X, y - frame.Y);
            ArrangeOverlayArrows(overlay, layout, new Rect(x, y, width, height), anchorBounds);
        }
        return constrained;
    }

    private float? OverlayAlignmentOffset(ElementNode overlay, float top)
    {
        var target = overlay.Attributes.GetValueOrDefault("overlay-alignment-target") as ElementNode;
        if (target is null && overlay.Attributes.GetValueOrDefault("drop-down") is "center")
            target = overlay.Children.OfType<ElementNode>().FirstOrDefault(child => child.Attributes.GetValueOrDefault("role") is "option" &&
                child.Attributes.GetValueOrDefault("selected") is true);
        if (target is null || target.IsRemoved || target == overlay || !IsRendered(target)) return null;
        for (var ancestor = target.ParentElement; ancestor is not null; ancestor = ancestor.ParentElement)
        {
            if (ancestor != overlay) continue;
            var bounds = GetBounds(target);
            return bounds.Width > 0 && bounds.Height > 0 ? bounds.Y + bounds.Height / 2 - top : null;
        }
        return null;
    }

    private static string ArrangedOverlaySide(ElementNode overlay, Rect surface, Rect anchor)
    {
        var placement = overlay.Attributes.GetValueOrDefault("placement") as string ?? "bottom";
        var dash = placement.IndexOf('-');
        var requested = placement.AsSpan(0, dash < 0 ? placement.Length : dash);
        if (requested.SequenceEqual("left") || requested.SequenceEqual("right"))
        {
            if (surface.Right <= anchor.X + 0.01f) return "left";
            if (surface.X >= anchor.Right - 0.01f) return "right";
        }
        if (surface.Bottom <= anchor.Y + 0.01f) return "top";
        if (surface.Y >= anchor.Bottom - 0.01f) return "bottom";
        if (surface.Right <= anchor.X + 0.01f) return "left";
        if (surface.X >= anchor.Right - 0.01f) return "right";
        return "bottom";
    }

    private void ArrangeOverlayArrows(ElementNode overlay, ElementLayout surfaceLayout, Rect surface, Rect? anchor)
    {
        foreach (var child in overlay.Children)
        {
            if (child is not ElementNode arrow || arrow.Attributes.GetValueOrDefault("overlay-arrow") is not true) continue;
            if (arrow.GetFeature<ElementLayout>() is not { Style: { } style } layout) continue;
            layout.OverlayShift = null;
            if (anchor is not { } target || !IsRendered(arrow)) continue;
            var frame = layout.Node.RoundedFrame;
            var transformed = OwnTransform(arrow, style, frame)?.TransformBounds(frame) ?? frame;
            var radius = BorderRadius(overlay, GetStyle(overlay), surface);
            var margin = layout.Node.Margin;
            var side = ArrangedOverlaySide(overlay, surface, target);
            var halfWidth = transformed.Width / 2;
            var halfHeight = transformed.Height / 2;
            var x = Clamp(target.X + target.Width / 2, surface.X, surface.Width, halfWidth,
                Math.Max(margin.Left, Math.Max(radius.TopLeft.X, radius.BottomLeft.X)),
                Math.Max(margin.Right, Math.Max(radius.TopRight.X, radius.BottomRight.X)));
            var y = Clamp(target.Y + target.Height / 2, surface.Y, surface.Height, halfHeight,
                Math.Max(margin.Top, Math.Max(radius.TopLeft.Y, radius.TopRight.Y)),
                Math.Max(margin.Bottom, Math.Max(radius.BottomLeft.Y, radius.BottomRight.Y)));
            if (side == "top") y = surface.Bottom;
            else if (side == "bottom") y = surface.Y;
            else if (side == "left") x = surface.Right;
            else x = surface.X;
            var shift = surfaceLayout.OverlayShift ?? default;
            layout.OverlayShift = new Point(x - shift.X - transformed.X - halfWidth, y - shift.Y - transformed.Y - halfHeight);
            if (overlay.Attributes.GetValueOrDefault("overlay-origin-at-arrow") is true)
            {
                var tipX = x + (side == "left" ? halfWidth : side == "right" ? -halfWidth : 0);
                var tipY = y + (side == "top" ? halfHeight : side == "bottom" ? -halfHeight : 0);
                surfaceLayout.OverlayOrigin = new Point(tipX - shift.X, tipY - shift.Y);
            }
        }

        static float Clamp(float point, float start, float length, float half, float before, float after)
        {
            var minimum = start + half + Math.Max(0, before);
            var maximum = start + length - half - Math.Max(0, after);
            return maximum < minimum ? start + length / 2 : Math.Clamp(point, minimum, maximum);
        }
    }

    private Rect? OverlayAnchor(ElementNode overlay) =>
        overlay.Attributes.GetValueOrDefault("anchor") is ElementNode { IsRemoved: false } anchor && IsRendered(anchor) ? GetBounds(anchor)
        : overlay.Attributes.GetValueOrDefault("anchor-x") is double x && overlay.Attributes.GetValueOrDefault("anchor-y") is double y
            ? new Rect((float)x, (float)y, 0, 0)
            : null;

    private static bool HasUrl(IReadOnlyList<ImageValue> layers)
    {
        for (var i = 0; i < layers.Count; i++)
            if (layers[i] is UrlImage) return true;
        return false;
    }

    private void SyncBackgrounds(ElementLayout layout, ComputedStyle style)
    {
        var layers = style.Get(StyleProperties.BackgroundImage).Items;
        if (!HasUrl(layers))
        {
            layout.Backgrounds = null;
            return;
        }
        layout.Backgrounds ??= [];
        foreach (var url in layers.OfType<UrlImage>())
        {
            if (!layout.Backgrounds.TryGetValue(url.Url, out var state)) layout.Backgrounds[url.Url] = state = new ImageState();
            ImageSource source;
            try
            {
                source = ImageSource.Parse(url.Url);
            }
            catch (ArgumentException)
            {
                continue;
            }
            if (!source.Equals(state.Source) || (source.HasVariants && state.Scale != Scale))
            {
                state.Source = source;
                state.Scale = Scale;
                state.Task = images.Load(source, Scale);
                state.Reported = false;
            }
            if (state.Task is { IsCompleted: true } task && !state.Reported)
            {
                state.Reported = true;
                state.Shown = task.IsCompletedSuccessfully ? task.Result : null;
                if (!task.IsCompletedSuccessfully) images.Forget(source, Scale);
            }
        }
    }

    private static void PaintBackgrounds(ElementLayout layout, ComputedStyle style, Rect area, CornerRadius radius, DisplayListBuilder builder) =>
        BackgroundPainter.Paint(style.Get(StyleProperties.BackgroundImage).Items, style.Get(StyleProperties.BackgroundPosition).Items,
            style.Get(StyleProperties.BackgroundSize).Items, style.Get(StyleProperties.BackgroundRepeat).Items, style.Color,
            url => layout.Backgrounds?.GetValueOrDefault(url)?.Shown, area, radius, builder, style.Get(StyleProperties.BackgroundBlendMode).Items);

    private void SyncImage(ElementNode element, ElementLayout layout, ImageSource source)
    {
        layout.Text = null;
        layout.Before = layout.After = null;
        layout.VisualChildren = null;
        layout.PlaceholderStyle = null;
        layout.AnonymousText.Clear();
        layout.Node.SetChildren([]);
        var state = layout.Image ??= new ImageState();
        if (!source.Equals(state.Source) || (source.HasVariants && state.Scale != Scale))
        {
            state.Source = source;
            state.Scale = Scale;
            state.Task = images.Load(source, Scale);
            state.Reported = false;
        }
        if (state.Task is { IsCompleted: true } task && !state.Reported)
        {
            state.Reported = true;
            if (task.IsCompletedSuccessfully)
            {
                state.Shown = task.Result;
                var size = state.Shown.NaturalSize;
                imageEvents.Add((element, "onload", new ImageLoadEventArgs { Width = size.Width, Height = size.Height }));
            }
            else
            {
                images.Forget(source, Scale);
                state.Shown = null;
                imageEvents.Add((element, "onerror", new ImageErrorEventArgs { Message = task.Exception?.GetBaseException().Message ?? "The image load was cancelled." }));
            }
            layout.Node.MarkDirty();
        }

        var replaced = layout.Node.Style with { IsReplaced = true };
        if (state.Shown?.NaturalSize is { Width: > 0, Height: > 0 } natural && layout.Style?.Get(StyleProperties.AspectRatio) == AspectRatio.Auto)
            replaced = replaced with { AspectRatio = new AspectRatio(natural.Width / natural.Height) };
        layout.Node.Style = replaced;
        layout.Node.Measure ??= (knownWidth, knownHeight, _, _) => MeasureImage(layout, knownWidth, knownHeight);
    }

    private static GraphicsSize MeasureImage(ElementLayout layout, float? knownWidth, float? knownHeight)
    {
        if (layout.Image?.Shown is not { } shown) return new GraphicsSize(knownWidth ?? 0, knownHeight ?? 0);
        var natural = shown.NaturalSize;
        var ratio = natural.Height > 0 ? natural.Width / natural.Height : 0;
        return (knownWidth, knownHeight) switch
        {
            ({ } width, { } height) => new GraphicsSize(width, height),
            ({ } width, null) => new GraphicsSize(width, ratio > 0 ? width / ratio : natural.Height),
            (null, { } height) => new GraphicsSize(ratio > 0 ? height * ratio : natural.Width, height),
            _ => natural,
        };
    }

    private void PaintImage(ElementNode element, ComputedStyle style, LayoutNode node, LoadedImage image, CornerRadius radius, DisplayListBuilder builder)
    {
        var box = node.ContentBox;
        var natural = image.NaturalSize;
        if (box.IsEmpty || natural.Width <= 0 || natural.Height <= 0) return;
        var contain = Math.Min(box.Width / natural.Width, box.Height / natural.Height);
        var size = style.Get(StyleProperties.ObjectFit) switch
        {
            ObjectFit.Contain => new GraphicsSize(natural.Width * contain, natural.Height * contain),
            ObjectFit.Cover => Scaled(Math.Max(box.Width / natural.Width, box.Height / natural.Height)),
            ObjectFit.None => natural,
            ObjectFit.ScaleDown => contain < 1 ? Scaled(contain) : natural,
            _ => box.Size,
        };
        var position = Effective(element, style, StyleProperties.ObjectPosition);
        var destination = new Rect(box.X + (float)position.X.Resolve(box.Width - size.Width), box.Y + (float)position.Y.Resolve(box.Height - size.Height), size.Width, size.Height);
        var inner = radius.Deflate(node.Border + node.Padding);
        var overflows = destination.X < box.X || destination.Y < box.Y || destination.Right > box.Right || destination.Bottom > box.Bottom;
        if (!inner.IsZero) builder.Add(new PushClip(box, inner));

        var tint = element.Attributes.GetValueOrDefault("tint") is true ? ToGraphics(Effective(element, style, StyleProperties.Color)) : (GraphicsColor?)null;
        builder.Add(new DrawImage(image.Image, destination, overflows && inner.IsZero ? box : null, tint));
        if (!inner.IsZero) builder.Add(Pop.Instance);

        GraphicsSize Scaled(float factor) => new(natural.Width * factor, natural.Height * factor);
    }

    private static Thickness ContentInsets(ElementNode element) => element.Name == "TitleBar"
        ? new Thickness(element.Attributes.GetValueOrDefault("system-buttons-left-inset") as float? ?? 0, 0,
            element.Attributes.GetValueOrDefault("system-buttons-right-inset") as float? ?? 0, 0)
        : Thickness.Zero;

    private LayoutStyle ToLayoutStyle(ElementNode element, ComputedStyle s)
    {
        T Get<T>(StyleProperty<T> property) where T : notnull => Transitions.GetValue(element, property, s.Get(property));

        float Border(StyleProperty<Length> width, StyleProperty<BorderStyle> style) =>
            Get(style) == BorderStyle.None ? 0 : SnapLine((float)Get(width).Value);
        return new LayoutStyle
        {
            Display = s.Display,
            TablePart = element.Attributes.GetValueOrDefault("table-part") as string,
            TableRowSpan = element.Attributes.GetValueOrDefault("row-span") as int? ?? 1,
            TableColumnSpan = element.Attributes.GetValueOrDefault("column-span") as int? ?? 1,
            FlexDirection = Get(StyleProperties.FlexDirection),
            FlexWrap = Get(StyleProperties.FlexWrap),
            JustifyContent = Get(StyleProperties.JustifyContent),
            AlignContent = Get(StyleProperties.AlignContent),
            AlignItems = Get(StyleProperties.AlignItems),
            AlignSelf = Get(StyleProperties.AlignSelf),
            FlexGrow = (float)Get(StyleProperties.FlexGrow),
            FlexShrink = (float)Get(StyleProperties.FlexShrink),
            FlexBasis = Get(StyleProperties.FlexBasis),
            Order = Get(StyleProperties.Order),
            Width = Get(StyleProperties.Width),
            Height = Get(StyleProperties.Height),
            MinWidth = Get(StyleProperties.MinWidth),
            MinHeight = Get(StyleProperties.MinHeight),
            MaxWidth = Get(StyleProperties.MaxWidth),
            MaxHeight = Get(StyleProperties.MaxHeight),
            ContentInsets = ContentInsets(element),
            ScrollbarGutter = RetainedScrollBarGutter(element, s),
            PaddingLeft = Get(StyleProperties.PaddingLeft),
            PaddingTop = Get(StyleProperties.PaddingTop),
            PaddingRight = Get(StyleProperties.PaddingRight),
            PaddingBottom = Get(StyleProperties.PaddingBottom),
            Border = new Thickness(Border(StyleProperties.BorderLeftWidth, StyleProperties.BorderLeftStyle), Border(StyleProperties.BorderTopWidth, StyleProperties.BorderTopStyle),
                Border(StyleProperties.BorderRightWidth, StyleProperties.BorderRightStyle), Border(StyleProperties.BorderBottomWidth, StyleProperties.BorderBottomStyle)),
            MarginLeft = Get(StyleProperties.MarginLeft),
            MarginTop = Get(StyleProperties.MarginTop),
            MarginRight = Get(StyleProperties.MarginRight),
            MarginBottom = Get(StyleProperties.MarginBottom),
            RowGap = Get(StyleProperties.RowGap),
            ColumnGap = Get(StyleProperties.ColumnGap),
            BoxSizing = Get(StyleProperties.BoxSizing),
            OverflowX = Get(StyleProperties.OverflowX),
            OverflowY = Get(StyleProperties.OverflowY),
            AspectRatio = Get(StyleProperties.AspectRatio),
            Position = Get(StyleProperties.Position),
            Transformed = Get(StyleProperties.Transform).Items.Count > 0,
            Top = Get(StyleProperties.Top),
            Right = Get(StyleProperties.Right),
            Bottom = Get(StyleProperties.Bottom),
            Left = Get(StyleProperties.Left),
            GridTemplateColumns = Get(StyleProperties.GridTemplateColumns),
            GridTemplateRows = Get(StyleProperties.GridTemplateRows),
            GridTemplateAreas = Get(StyleProperties.GridTemplateAreas),
            GridAutoColumns = Get(StyleProperties.GridAutoColumns),
            GridAutoRows = Get(StyleProperties.GridAutoRows),
            GridAutoFlow = Get(StyleProperties.GridAutoFlow),
            GridRowStart = Get(StyleProperties.GridRowStart),
            GridRowEnd = Get(StyleProperties.GridRowEnd),
            GridColumnStart = Get(StyleProperties.GridColumnStart),
            GridColumnEnd = Get(StyleProperties.GridColumnEnd),
            JustifyItems = Get(StyleProperties.JustifyItems),
            JustifySelf = Get(StyleProperties.JustifySelf),
        };
    }

    private void Paint(ElementNode element, DisplayListBuilder builder)
    {
        if (inView.IsEmpty) return;
        if (element.GetFeature<ElementLayout>() is not { } layout || layout.Style is not { } style || style.Display == Display.None) return;
        var node = layout.Node;
        var bounds = node.RoundedFrame;
        var visible = style.Get(StyleProperties.Visibility) == Visibility.Visible;
        var opacity = (float)Transitions.GetValue(element, StyleProperties.Opacity, style.Opacity);
        var backdrop = style.Get(StyleProperties.BackdropFilter);
        var filters = style.Get(StyleProperties.Filter);
        var composition = backdrop.IsNone && filters.IsNone ? CaptureComposition(element, style, bounds) : null;
        if (opacity <= 0 && composition is null) return;

        var (ink, own, transform) = Inks(element);
        if (composition is null && (transform is { } moved ? moved.TransformBounds(ink) : ink).Intersect(inView).IsEmpty) return;

        var seen = !(transform is { } shifted ? shifted.TransformBounds(own) : own).Intersect(inView).IsEmpty;
        var drawn = visible && (seen || composition is not null);
        var filtering = drawn && !backdrop.IsNone;
        var filteringContent = visible && !filters.IsNone;
        var outside = inView;
        var clip = InsetClip(element, style, bounds);
        if (clip is { Bounds.IsEmpty: true }) return;
        if (composition is not null)
        {
            builder.Add(new PushComposition(composition));
            inView = Everywhere;
        }
        else if (transform is { } matrix)
        {
            builder.Add(new PushTransform(matrix));
            inView = matrix.TryInvert(out var inverse) ? inverse.TransformBounds(inView) : Everywhere;
        }

        if (clip is { } inset)
        {
            builder.Add(new PushClip(inset.Bounds, inset.Radius));
            inView = inView.Intersect(inset.Bounds);
        }

        var radius = BorderRadius(element, style, bounds);
        var decorationBounds = DecorationBounds(element, style, node);
        var decorationRadius = BorderRadius(element, style, decorationBounds);
        var blend = style.Get(StyleProperties.MixBlendMode);
        var blending = blend != Nazor.Style.BlendMode.Normal || style.Get(StyleProperties.Isolation) == Isolation.Isolate;
        if (blending) builder.Add(new PushBlend(BlendModes.Resolve(blend),ink));
        if (filteringContent)
        {
            builder.Add(new PushFilter(FilterOperations(filters),ink,opacity));
            inView=inView.Inflate(FilterOutset(filters));
        }
        if (filtering)
            builder.Add(new PushBackdrop(decorationBounds, decorationRadius, ink,0,Opacity:filteringContent ? 1 : opacity) { Filters=FilterOperations(backdrop) });
        else if (!filteringContent && opacity < 1 && composition is null) builder.Add(new PushOpacity(opacity, ink));
        if (drawn)
        {

            var shadows = Effective(element, style, StyleProperties.BoxShadow).Items;
            PaintShadows(shadows, decorationBounds, decorationRadius, inset: false, builder);
            var background = ToGraphics(Transitions.GetValue(element, StyleProperties.BackgroundColor, style.BackgroundColor));
            var backgroundBounds = style.Get(StyleProperties.BackgroundClip) ? decorationBounds.Deflate(node.Border) : decorationBounds;
            var backgroundRadius = style.Get(StyleProperties.BackgroundClip) ? decorationRadius.Deflate(node.Border) : decorationRadius;
            var blendBackground = style.Get(StyleProperties.BackgroundBlendMode).Items.Any(mode => mode != Nazor.Style.BlendMode.Normal);
            if (blendBackground) builder.Add(new PushBlend(Nazor.Graphics.BlendMode.Normal,backgroundBounds));
            if (!background.IsTransparent) builder.Add(new FillRectangle(backgroundBounds, backgroundRadius, background));

            PaintBackgrounds(layout, style, backgroundBounds, backgroundRadius, builder);
            if (blendBackground) builder.Add(Pop.Instance);
            PaintShadows(shadows, decorationBounds.Deflate(node.Border), decorationRadius.Deflate(node.Border), inset: true, builder);
            PaintBorder(element, style, node.Border, decorationBounds, decorationRadius, builder);
        }
        var clips = style.Get(StyleProperties.OverflowX) != Overflow.Visible || style.Get(StyleProperties.OverflowY) != Overflow.Visible;
        if (clips)
        {
            var clipBounds = bounds.Deflate(node.Border);
            var clipRadius = radius.Deflate(node.Border);
            if (layout.ContentClip is not { } cachedClip || cachedClip.Rect != clipBounds || cachedClip.Radius != clipRadius)
                layout.ContentClip = new PushClip(clipBounds, clipRadius);
            builder.Add(layout.ContentClip);
            inView = inView.Intersect(bounds.Deflate(node.Border));
        }
        var reserved = clips && !node.Style.ScrollbarGutter.IsZero;
        if (reserved)
        {
            builder.Add(new PushClip(ScrollViewport(node), 0));
            inView = inView.Intersect(ScrollViewport(node));
        }
        var color = Transitions.GetValue(element, StyleProperties.Color, style.Color);
        if (layout.Text is { } text && drawn)
        {
            if (layout.Inline is not null)
            {
                if (element.GetFeature<TextEditState>() is { } inlineEdit) PaintInlineSelection(element, layout, inlineEdit, builder);
                else PaintInline(element, layout, builder);
            }
            else if (element.GetFeature<TextEditState>() is { } edit) PaintEditableText(element, node, text, color, edit, builder);
            else
            {
                var textStyle = layout.PlaceholderStyle ?? style;
                var shownColor = layout.PlaceholderStyle is null ? color : textStyle.Color with { A = (byte)Math.Round(textStyle.Color.A * textStyle.Opacity) };
                PaintText(node, text, textStyle, shownColor, builder, ellipsis: style.Get(StyleProperties.TextOverflow) == TextOverflow.Ellipsis &&
                    style.Get(StyleProperties.OverflowX) != Overflow.Visible, decorationOpacity: layout.PlaceholderStyle?.Opacity ?? 1);
            }
        }
        if (drawn && element.Attributes.GetValueOrDefault("glyph") is "minus")
            PaintPolyline([new Point(node.ContentBox.X + node.ContentBox.Width * 0.18f, node.ContentBox.Y + node.ContentBox.Height / 2),
                new Point(node.ContentBox.X + node.ContentBox.Width * 0.84f, node.ContentBox.Y + node.ContentBox.Height / 2)],
                GlyphStrokeWidth(Effective(element, style, StyleProperties.StrokeWidth), Math.Min(node.ContentBox.Width, node.ContentBox.Height), 1.5f), color,
                Effective(element, style, StyleProperties.StrokeTrimStart), Effective(element, style, StyleProperties.StrokeTrimEnd), builder);
        if (drawn && element.Attributes.GetValueOrDefault("glyph") is "check")
            PaintCheckMark(node.ContentBox, color, Effective(element, style, StyleProperties.StrokeWidth),
                Effective(element, style, StyleProperties.StrokeTrimStart), Effective(element, style, StyleProperties.StrokeTrimEnd), builder);
        if (drawn && element.Attributes.GetValueOrDefault("glyph") is string { } chevron && chevron.StartsWith("chevron-", StringComparison.Ordinal))
            PaintChevron(node.ContentBox, color, chevron, Effective(element, style, StyleProperties.StrokeWidth),
                Effective(element, style, StyleProperties.StrokeTrimStart), Effective(element, style, StyleProperties.StrokeTrimEnd), builder);
        if (drawn && element.Attributes.GetValueOrDefault("glyph") is "ring" && element.Attributes.GetValueOrDefault("ring-fraction") is double fraction)
            PaintRing(style.Get(StyleProperties.RingShape) == RingShape.BorderBox ? node.RoundedFrame : node.ContentBox, color, fraction,
                Effective(element, style, StyleProperties.StrokeWidth), Effective(element, style, StyleProperties.StrokeRadius),
                Effective(element, style, StyleProperties.StrokeTrimStart), Effective(element, style, StyleProperties.StrokeTrimEnd),
                style.Get(StyleProperties.RingShape), radius, builder);
        if (drawn && layout.Image?.Shown is { } image) PaintImage(element, style, node, image, radius, builder);
        var scrolled = layout.Scroll is { OffsetY: > 0 } or { OffsetX: > 0 };
        if (scrolled)
        {
            builder.Add(new PushTranslate(-layout.Scroll!.OffsetX, -layout.Scroll.OffsetY));
            inView = inView.Offset(layout.Scroll.OffsetX, layout.Scroll.OffsetY);
        }
        var paintChildren = layout.Inline is null ? PaintOrder(element, style) : [];
        for (var childIndex = 0; childIndex < paintChildren.Count; childIndex++)
        {
            var child = paintChildren[childIndex];
            if (child is ElementNode childElement) { if (!IsOverlay(childElement)) Paint(childElement, builder); }
            else if (child is TextNode textNode && layout.AnonymousText.TryGetValue(textNode, out var anonymous) && visible &&
                !TextInk(anonymous.Node, anonymous.Text, anonymous.Node.RoundedFrame).Intersect(inView).IsEmpty)
                PaintText(anonymous.Node, anonymous.Text, style, color, builder);
        }
        if (scrolled) builder.Add(Pop.Instance);
        if (reserved) builder.Add(Pop.Instance);
        if ((seen || composition is not null) && visible && layout.Scroll is { CanScroll: true }) PaintScrollBars(element, style, builder);
        if (clips) builder.Add(Pop.Instance);

        if (drawn) PaintOutline(element, style, decorationBounds, decorationRadius, builder);
        if (filtering) builder.Add(Pop.Instance);
        if (filteringContent) builder.Add(Pop.Instance);
        else if (!filtering && opacity < 1 && composition is null) builder.Add(Pop.Instance);
        if (blending) builder.Add(Pop.Instance);
        if (clip is not null) builder.Add(Pop.Instance);
        if (transform is not null && composition is null) builder.Add(Pop.Instance);
        if (composition is not null) builder.Add(Pop.Instance);
        inView = outside;
    }

    private void PaintBorder(ElementNode element, ComputedStyle style, Thickness widths, Rect bounds, CornerRadius radius, DisplayListBuilder builder)
    {
        if (widths.IsZero) return;
        BorderSide Side(StyleProperty<StyleColor> color, StyleProperty<BorderStyle> line) =>
            new(ToGraphics(Transitions.GetValue(element, color, style.Get(color))), ToLineStyle(style.Get(line)));
        var top = Side(StyleProperties.BorderTopColor, StyleProperties.BorderTopStyle);
        var right = Side(StyleProperties.BorderRightColor, StyleProperties.BorderRightStyle);
        var bottom = Side(StyleProperties.BorderBottomColor, StyleProperties.BorderBottomStyle);
        var left = Side(StyleProperties.BorderLeftColor, StyleProperties.BorderLeftStyle);
        var brush = style.Get(StyleProperties.BorderImageSource).Items.FirstOrDefault() is Gradient gradient
            ? GradientGeometry.Resolve(gradient, bounds, style.Color) : null;
        if (brush is null && top.Color.IsTransparent && right.Color.IsTransparent && bottom.Color.IsTransparent && left.Color.IsTransparent) return;
        builder.Add(new DrawBorder(bounds, radius, widths, top, right, bottom, left) { Brush = brush });
    }

    private static void PaintShadows(IReadOnlyList<BoxShadow> shadows, Rect box, CornerRadius radius, bool inset, DisplayListBuilder builder)
    {
        for (var i = shadows.Count - 1; i >= 0; i--)
        {
            var shadow = shadows[i];
            if (shadow.Inset != inset) continue;
            var color = ToGraphics(shadow.Color);
            if (color.IsTransparent) continue;
            builder.Add(new DrawShadow(box, radius, new Point((float)shadow.X.Value, (float)shadow.Y.Value), (float)shadow.Blur.Value, (float)shadow.Spread.Value, color, inset));
        }
    }

    private void PaintOutline(ElementNode element, ComputedStyle style, Rect bounds, CornerRadius radius, DisplayListBuilder builder)
    {
        var lineStyle = style.Get(StyleProperties.OutlineStyle);
        if (lineStyle == BorderStyle.None) return;
        var width = SnapLine((float)Effective(element, style, StyleProperties.OutlineWidth).Value);
        var color = ToGraphics(Effective(element, style, StyleProperties.OutlineColor));
        if (width <= 0 || color.IsTransparent) return;
        var extent = (float)Effective(element, style, StyleProperties.OutlineOffset).Value + width;
        builder.Add(new DrawBorder(bounds.Inflate(extent), radius.Inflate(extent), new Thickness(width, width, width, width), color, ToLineStyle(lineStyle)));
        var innerWidth = SnapLine((float)Effective(element, style, StyleProperties.OutlineInnerWidth).Value);
        var innerColor = ToGraphics(Effective(element, style, StyleProperties.OutlineInnerColor));
        if (innerWidth > 0 && !innerColor.IsTransparent)
            builder.Add(new DrawBorder(bounds.Inflate(extent - width), radius.Inflate(extent - width), new Thickness(innerWidth, innerWidth, innerWidth, innerWidth), innerColor));
    }

    private float InkOutset(ElementNode element, ComputedStyle style)
    {
        var outset = style.Get(StyleProperties.OutlineStyle) == BorderStyle.None ? 0
            : Math.Max(0, (float)(Effective(element, style, StyleProperties.OutlineOffset).Value + Effective(element, style, StyleProperties.OutlineWidth).Value));
        if (element.Attributes.GetValueOrDefault("glyph") is string glyph && (glyph == "check" || glyph.StartsWith("chevron-", StringComparison.Ordinal)) &&
            element.GetFeature<ElementLayout>() is { } layout)
            outset = Math.Max(outset, (float)Effective(element, style, StyleProperties.StrokeWidth).Resolve(Math.Min(layout.Node.ContentBox.Width, layout.Node.ContentBox.Height)) / 2);
        foreach (var shadow in Effective(element, style, StyleProperties.BoxShadow).Items)
        {
            if (shadow.Inset) continue;
            var reach = Math.Max(Math.Abs(shadow.X.Value), Math.Abs(shadow.Y.Value)) + shadow.Blur.Value + shadow.Spread.Value;
            outset = Math.Max(outset, (float)reach);
        }
        return outset;
    }

    private Rect InkBounds(ElementNode element)
    {
        var ink = Inks(element);
        return ink.Transform is { } transform ? transform.TransformBounds(ink.All) : ink.All;
    }

    private Ink Inks(ElementNode element)
    {
        if (building && inks.TryGetValue(element, out var measured)) return measured;
        var result = MeasureInk(element);
        if (building) inks[element] = result;
        return result;
    }

    private Ink MeasureInk(ElementNode element)
    {
        if (element.GetFeature<ElementLayout>() is not { Style: { } style } layout) return new(Rect.Empty, Rect.Empty, null);
        var transform = TransformOf(element, style, layout.Node.RoundedFrame);

        var box = Union(layout.Node.RoundedFrame, DecorationBounds(element, style, layout.Node)).Inflate(InkOutset(element, style));
        var own = layout.Text is { } text ? TextInk(layout.Node, text, box) : box;
        // A compositor transform may bring this subtree into view after the UI thread has stopped painting.
        if (composing && Transitions.IsAnimating(element, StyleEffects.Composite)) return new(Everywhere, own, null);
        var filterOutset=FilterOutset(style.Get(StyleProperties.Filter));
        if (style.Get(StyleProperties.OverflowX) != Overflow.Visible || style.Get(StyleProperties.OverflowY) != Overflow.Visible)
            return new(box.Inflate(filterOutset), own.Inflate(filterOutset), transform);
        var ink = own;
        foreach (var (textNode, content) in layout.AnonymousText.Values) ink = Union(ink, TextInk(textNode, content, textNode.RoundedFrame));
        foreach (var child in VisualChildren(element))
        {
            if (child is not ElementNode childElement || IsOverlay(childElement) || childElement.GetFeature<ElementLayout>() is not { Style: { } childStyle } ||
                childStyle.Display == Display.None) continue;
            var (childInk, _, childTransform) = Inks(childElement);
            ink = Union(ink, childTransform is { } moved ? moved.TransformBounds(childInk) : childInk);
        }
        return new(ink.Inflate(filterOutset), own.Inflate(filterOutset), transform);
    }

    private static Rect TextInk(LayoutNode node, TextContent text, Rect ink)
    {
        if (text.Text.Length == 0) return ink;
        var lines = text.Get(node.ContentWidth);
        var box = node.ContentBox;
        var over = Math.Max(0, lines.Width - box.Width);
        return Union(ink, new Rect(box.X - over, box.Y, box.Width + 2 * over, Math.Max(box.Height, lines.Height)).Inflate(lines.InkOutset));
    }

    private static Rect Union(Rect a, Rect b)
    {
        if (b.IsEmpty) return a;
        if (a.IsEmpty) return b;
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        return new Rect(left, top, Math.Max(a.Right, b.Right) - left, Math.Max(a.Bottom, b.Bottom) - top);
    }

    private static LineStyle ToLineStyle(BorderStyle style) => style switch
    {
        BorderStyle.Dashed => LineStyle.Dashed,
        BorderStyle.Dotted => LineStyle.Dotted,
        _ => LineStyle.Solid,
    };

    private float SnapLine(float value) => value <= 0 ? 0 : Math.Max(1, MathF.Round(value * Scale)) / Scale;

    private IReadOnlyList<Node> PaintOrder(ElementNode element, ComputedStyle style)
    {
        var children = VisualChildren(element);

        var flex = style.Display is Display.Flex or Display.Grid;
        var previous = (ZIndex: 0, Layer: 0, Order: 0);
        for (var i = 0; i < children.Count; i++)
        {
            var key = PaintKey(children[i], flex);
            if (i > 0 && key.CompareTo(previous) < 0) return SortedPaintOrder(children, flex);
            previous = key;
        }
        return children;
    }

    private IReadOnlyList<Node> SortedPaintOrder(IReadOnlyList<Node> children, bool flex) =>
        [.. children.OrderBy(c => PaintKey(c, flex))];

    private (int ZIndex, int Layer, int Order) PaintKey(Node node, bool flexParent)
    {
        if (node is not ElementNode element) return (0, 0, 0);
        var style = GetStyle(element);

        var positioned = style.Get(StyleProperties.Position) != Position.Static || style.Get(StyleProperties.Transform).Items.Count > 0;
        var z = style.Get(StyleProperties.ZIndex);

        var stacked = !z.IsAuto && (positioned || flexParent);
        return (stacked ? z.Value : 0, positioned || stacked ? 1 : 0, flexParent ? style.Get(StyleProperties.Order) : 0);
    }

    private Rect DecorationBounds(ElementNode element, ComputedStyle style, LayoutNode node)
    {
        var bounds = node.AbsoluteFrame;
        if (bounds.Width <= 0 || bounds.Width != bounds.Height) return node.RoundedFrame;
        var radius = BorderRadius(element, style, bounds);
        return radius.IsUniform && radius.TopLeft.X == radius.TopLeft.Y && radius.TopLeft.X >= bounds.Width / 2
            ? bounds : node.RoundedFrame;
    }

    private CornerRadius BorderRadius(ElementNode element, ComputedStyle style, Rect bounds)
    {
        Radius Corner(StyleProperty<RadiusValue> property)
        {
            var value = Effective(element, style, property);
            return new Radius((float)value.X.Resolve(bounds.Width), (float)value.Y.Resolve(bounds.Height));
        }
        return new CornerRadius(Corner(StyleProperties.BorderTopLeftRadius), Corner(StyleProperties.BorderTopRightRadius),
            Corner(StyleProperties.BorderBottomRightRadius), Corner(StyleProperties.BorderBottomLeftRadius));
    }

    private void PaintEditableText(ElementNode element, LayoutNode node, TextContent text, StyleColor textColor, TextEditState edit, DisplayListBuilder builder)
    {
        var box = node.ContentBox;
        var layout = text.Get(node.ContentWidth);
        var focused = (element.States & ElementStates.Focus) != 0;
        var color = ToGraphics(textColor);
        var placeholder = element.GetFeature<ElementLayout>()?.PlaceholderStyle;
        var shownColor = placeholder is null ? color : ToGraphics(placeholder.Color);
        if (placeholder is not null) shownColor = shownColor with { A = (byte)Math.Round(shownColor.A * placeholder.Opacity) };
        var drawing = text.GetDrawing(layout, Point.Zero, shownColor, TextAlignmentOf(placeholder ?? GetStyle(element)), box.Width,
            placeholder is null ? null : Decoration(placeholder, placeholder.Opacity));
        builder.Add(new PushClip(box, 0));
        builder.Add(new PushTranslate(box.X - edit.ScrollX, box.Y - edit.ScrollY));
        var selected = edit.HasSelection && edit.Composition is null && placeholder is null;
        var selection = selected ? Styles.GetPseudoStyle(element, PseudoElement.Selection) : null;
        var rectangles = selected ? layout.GetSelectionRects(Math.Min(edit.SelectionStart, text.Text.Length), Math.Min(edit.SelectionEnd, text.Text.Length))
            .Select(rect => rect.Offset(GetEditableLineOffset(element, layout.Lines.First(line => line.Top == rect.Y)), 0)).ToArray() : [];
        foreach (var area in rectangles)
            builder.Add(new FillRectangle(area, 0, ToGraphics(selection!.BackgroundColor)));
        if (selection is null || ToGraphics(selection.Color) == shownColor)
        {
            if (text.Text.Length > 0) builder.Add(drawing);
        }
        else
        {
            var selectedDrawing = new DrawText(layout, Point.Zero, ToGraphics(selection.Color), drawing.Alignment, box.Width);
            var index = 0;
            foreach (var line in layout.Lines)
            {
                var visible = new Rect(edit.ScrollX, line.Top, box.Width, line.Height);
                var area = index < rectangles.Length && rectangles[index].Y == line.Top ? rectangles[index++].Intersect(visible) : Rect.Empty;
                if (area.IsEmpty) DrawClipped(visible, drawing);
                else
                {
                    DrawClipped(new Rect(visible.X, line.Top, area.X - visible.X, line.Height), drawing);
                    DrawClipped(area, selectedDrawing);
                    DrawClipped(new Rect(area.Right, line.Top, visible.Right - area.Right, line.Height), drawing);
                }
            }
        }
        if (edit.Composition is { Length: > 0 } composition)
        {
            var start = Math.Min(edit.CaretIndex, text.Text.Length);
            var line = layout.LineAt(start);
            var offset = GetEditableLineOffset(element, line);
            var x0 = layout.GetCaretX(start) + offset;
            var x1 = layout.GetCaretX(Math.Min(start + composition.Length, text.Text.Length)) + offset;
            builder.Add(new FillRectangle(new Rect(x0, line.Baseline + 2, x1 - x0, Math.Max(1, 1 / Scale)), 0, color));
        }

        if (focused && TextEditorRoles.IsEditor(element))
        {
            var caret = layout.GetCaretRect(Math.Min(edit.DisplayCaret, text.Text.Length));
            builder.Add(new FillRectangle(caret.Offset(GetEditableLineOffset(element, layout.LineAt(Math.Min(edit.DisplayCaret, text.Text.Length))), 0)
                with { Width = Math.Max(1, MathF.Round(Scale)) / Scale }, 0, color));
        }
        builder.Add(Pop.Instance);
        builder.Add(Pop.Instance);

        void DrawClipped(Rect area, DrawText command)
        {
            if (area.IsEmpty) return;
            builder.Add(new PushClip(area, 0));
            builder.Add(command);
            builder.Add(Pop.Instance);
        }
    }

    /// <summary>
    /// The layout of an editable element's displayed text (broken at its width when it wraps), or null.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting TextLayout? value.</returns>
    public TextLayout? GetEditableLayout(ElementNode element) =>
        element.GetFeature<ElementLayout>() is { Text: { } text } layout ? text.Get(layout.Node.ContentWidth) : null;

    /// <summary>Gets the horizontal alignment offset of an editable text line inside its content box.</summary>
    /// <param name="element">The editable element.</param>
    /// <param name="line">A line from the element's current editable layout.</param>
    /// <returns>The logical offset used by text painting, caret placement, selection and hit testing.</returns>
    public float GetEditableLineOffset(ElementNode element, TextLine line) => TextAlignmentOf(GetStyle(element)) switch
    {
        TextAlignment.Center => (GetContentBox(element).Width - line.Width) / 2,
        TextAlignment.End => GetContentBox(element).Width - line.Width,
        _ => 0,
    };

    /// <summary>
    /// The content box of an element in window coordinates, scroll offsets and transforms included (its bounds when
    /// transformed).
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting Rect value.</returns>
    public Rect GetContentBounds(ElementNode element) => GetTransform(element).TransformBounds(GetContentBox(element));

    private static void PaintCheckMark(Rect box, StyleColor color, Length strokeWidth, double trimStart, double trimEnd, DisplayListBuilder builder)
    {
        if (box.IsEmpty) return;
        var thickness = Math.Max(1.5f, Math.Min(box.Width, box.Height) * 0.14f);
        PaintPolyline(
            [new Point(box.X + box.Width * 0.18f, box.Y + box.Height * 0.52f), new Point(box.X + box.Width * 0.42f, box.Y + box.Height * 0.76f),
             new Point(box.X + box.Width * 0.84f, box.Y + box.Height * 0.26f)],
            GlyphStrokeWidth(strokeWidth, Math.Min(box.Width, box.Height), thickness), color, trimStart, trimEnd, builder);
    }

    private static void PaintChevron(Rect box, StyleColor color, string glyph, Length strokeWidth, double trimStart, double trimEnd, DisplayListBuilder builder)
    {
        if (box.IsEmpty) return;
        var size = Math.Min(box.Width, box.Height) * 0.5f;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var half = size / 2;
        Point[] points = glyph switch
        {
            "chevron-down" => [new(x - half, y - half / 2), new(x, y + half / 2), new(x + half, y - half / 2)],
            "chevron-up" => [new(x - half, y + half / 2), new(x, y - half / 2), new(x + half, y + half / 2)],
            "chevron-left" => [new(x + half / 2, y - half), new(x - half / 2, y), new(x + half / 2, y + half)],
            _ => [new(x - half / 2, y - half), new(x + half / 2, y), new(x - half / 2, y + half)],
        };
        PaintPolyline(points, GlyphStrokeWidth(strokeWidth, Math.Min(box.Width, box.Height), Math.Max(1, size / 6)),
            color, trimStart, trimEnd, builder);
    }

    private static float GlyphStrokeWidth(Length width, float size, float automatic)
    {
        var resolved = (float)width.Resolve(size);
        return resolved == 0 ? automatic : resolved;
    }

    private static void PaintPolyline(Point[] points, float thickness, StyleColor color, double trimStart, double trimEnd, DisplayListBuilder builder)
    {
        var start = (float)Math.Clamp(trimStart, 0, 1);
        var end = (float)Math.Clamp(trimEnd, 0, 1);
        if (thickness <= 0 || end <= start) return;
        if (start == 0 && end == 1)
        {
            builder.Add(new DrawPolyline(points, thickness, ToGraphics(color)));
            return;
        }
        static float Distance(Point a, Point b) => MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        var total = 0f;
        for (var i = 1; i < points.Length; i++) total += Distance(points[i - 1], points[i]);
        start *= total;
        end *= total;
        var trimmed = new List<Point>(points.Length);
        var at = 0f;
        for (var i = 1; i < points.Length && at < end; i++)
        {
            var from = points[i - 1];
            var to = points[i];
            var length = Distance(from, to);
            if (length > 0 && at + length > start)
            {
                Point Along(float distance) => new(from.X + (to.X - from.X) * distance / length, from.Y + (to.Y - from.Y) * distance / length);
                if (trimmed.Count == 0) trimmed.Add(Along(Math.Max(0, start - at)));
                trimmed.Add(Along(Math.Min(length, end - at)));
            }
            at += length;
        }
        if (trimmed.Count > 1) builder.Add(new DrawPolyline(trimmed, thickness, ToGraphics(color)));
    }

    private static void PaintRing(Rect box, StyleColor color, double fraction, Length strokeWidth, Length strokeRadius, double trimStart, double trimEnd, RingShape shape, CornerRadius corners, DisplayListBuilder builder)
    {
        var sweep = Math.Clamp(fraction * (trimEnd - trimStart), 0, 1);
        if (box.IsEmpty || sweep <= 0) return;
        var size = Math.Min(box.Width, box.Height);
        var thickness = GlyphStrokeWidth(strokeWidth, size, Math.Max(1.5f, size / 8));
        if (shape == RingShape.BorderBox)
        {
            var inset = strokeRadius == Length.Zero ? thickness / 2 : Math.Max(0, size / 2 - (float)strokeRadius.Resolve(size));
            PaintBorderRing(box, corners, inset, thickness, fraction * trimStart, fraction * trimEnd, color, builder);
            return;
        }
        var radius = strokeRadius == Length.Zero ? (size - thickness) / 2 : (float)strokeRadius.Resolve(size);
        var (centerX, centerY) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        var segments = Math.Max(2, (int)Math.Ceiling(64 * sweep));
        var points = new Point[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var angle = -Math.PI / 2 + 2 * Math.PI * (fraction * trimStart + sweep * i / segments);
            points[i] = new Point(centerX + radius * (float)Math.Cos(angle), centerY + radius * (float)Math.Sin(angle));
        }
        builder.Add(new DrawPolyline(points, thickness, ToGraphics(color)));
    }

    private static void PaintText(LayoutNode node, TextContent text, ComputedStyle style, StyleColor color, DisplayListBuilder builder, bool ellipsis = false, double decorationOpacity = 1)
    {
        if (text.Text.Length == 0) return;
        var box = node.ContentBox;

        var textLayout = ellipsis ? text.GetEllipsized(node.ContentWidth) : text.Get(node.ContentWidth);
        var alignment = TextAlignmentOf(style);
        builder.Add(text.GetDrawing(textLayout, box.Position, ToGraphics(color), alignment, box.Width, Decoration(style, decorationOpacity)));
    }

    private static TextAlignment TextAlignmentOf(ComputedStyle style) => style.Get(StyleProperties.TextAlign) switch
    {
        TextAlign.Center => TextAlignment.Center,
        TextAlign.End or TextAlign.Right => TextAlignment.End,
        _ => TextAlignment.Start,
    };

    private static TextDecoration? Decoration(ComputedStyle style, double opacity = 1)
    {
        var lines = style.Get(StyleProperties.TextDecorationLine);
        if (lines == TextDecorationLine.None) return null;
        var color = style.Get(StyleProperties.TextDecorationColor);
        var thickness = style.Get(StyleProperties.TextDecorationThickness);
        var foreground = ToGraphics(color.IsCurrentColor ? style.Color : color);
        foreground = foreground with { A = (byte)Math.Round(foreground.A * opacity) };
        return new TextDecoration((DecorationLines)(byte)lines, foreground,
            (DecorationStyle)(byte)style.Get(StyleProperties.TextDecorationStyle), thickness.IsFixed ? (float)thickness.Length.Value : 0);
    }

    private static GraphicsColor ToGraphics(StyleColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        DisposeLayoutMotion();
        numberPresentations.Clear();
        renderer.BatchCompleted -= DetachOverlayMotion;
        foreach (var element in overlayMotionOwners) element.SetFeature<IOverlayMotion>(null);
        overlayMotionOwners.Clear();
        overlayMotionPreferencesChanged = null;
        foreach (var element in wheelScrollUntil.Keys)
            if (!element.IsRemoved) element.SetState(ElementStates.WheelScrolling, false);
        wheelScrollUntil.Clear();
        smoothScrolls.Clear();
        scrollBarFades.Clear();
        revealedScrollBars.Clear();
        scrollBarContainers.Clear();
        hoveredScrollBar = null;
        draggedScrollBarThumb = null;
        pressedScrollBarButton = null;
        hoveredScrollBarButton = null;
        Styles.Dispose();
    }
}
