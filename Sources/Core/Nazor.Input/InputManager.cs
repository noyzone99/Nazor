using Nazor.Graphics;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Input;

/// <summary>
/// What a point of a window with the content in the title bar is (<see cref="InputManager.RegionAt" />).
/// </summary>
public enum WindowRegion
{
    /// <summary>Routes input to the application content.</summary>
    Client,

    /// <summary>Uses native title-bar dragging and double-click behavior.</summary>
    Caption,

    /// <summary>Uses native maximize-button hit testing, including Snap Layouts where supported.</summary>
    MaximizeButton,
}

/// <summary>
/// Turns normalised pointer and keyboard input into element states and semantic events: <c>:hover</c> on the element
/// under the pointer and its ancestors, <c>:active</c> on a pressed activatable element or a captured slider/handle,
/// focus and focus-visible,
/// Tab order, and <c>onclick</c> from a pointer release over the pressed element or from Enter/Space on the focused
/// one. Elements opt in through attributes set by controls: <c>onclick</c>, <c>focusable</c>, <c>disabled</c>.
/// Everything runs on the dispatcher thread.
/// </summary>
public sealed partial class InputManager : IDisposable, IElementInteraction
{
    private readonly List<ElementNode> focusWithin = [];

    private bool scrollDispatchQueued;
    private ElementNode? activeModal;
    private ElementNode? pendingFocusScroll;
    private readonly Dictionary<ElementNode,ElementNode> overlayModalScopes = [];
    private readonly HashSet<ElementNode> observedOverlays = [];
    private readonly HashSet<ElementNode> exitingOverlays = [];
    private readonly Dictionary<ElementNode,ElementNode> overlayOwners = [];
    private RootComponent? tooltipHost;
    private ElementNode? tooltipTarget;
    private ElementNode? tooltipPending;
    private int tooltipGeneration;
    private CancellationTokenSource? tooltipCancellation;
    private bool disposed;
    private int focusVersion;
    private bool dispatchingPointerPress;

    private readonly Dictionary<ElementNode, (ElementNode? Element, bool Visible)> overlayOrigins = new(ReferenceEqualityComparer.Instance);

    /// <summary>Backend editing conventions; real event modifiers remain unchanged.</summary>
    public TextInputPolicy TextPolicy { get; set; } = TextInputPolicy.Default;

    /// <summary>Logical distance per wheel detent; configured by the backend.</summary>
    public float WheelScrollDistance { get; set; } = 48;

    /// <summary>
    /// Gets or sets whether a wheel detent scrolls by a viewport page rather than a fixed distance.
    /// </summary>
    public bool WheelScrollByPage { get; set; }

    private readonly Renderer renderer;
    private readonly Scene scene;
    private readonly List<ElementNode> hoverChain = [];
    private ElementNode? pressedByPointer;
    private ElementNode? pressedByKey;
    private Point lastPointer;
    private readonly HashSet<PointerButton> pointerButtons = [];

    private readonly Dictionary<ElementNode, ElementNode> barFocus = new(ReferenceEqualityComparer.Instance);
    private KeyModifiers lastPointerModifiers;
    private bool windowFocused = true;

    /// <summary>
    /// Dispatches input to the retained element tree and manages focus, selection, and pointer capture.
    /// </summary>
    /// <param name="renderer">The renderer that owns the component tree.</param>
    /// <param name="scene">The scene used for layout, hit testing, and drawing.</param>
    /// <param name="keyBindings">The command registry; null creates a registry for this input manager.</param>
    public InputManager(Renderer renderer, Scene scene, KeyBindings? keyBindings = null)
    {
        this.renderer = renderer;
        this.scene = scene;
        KeyBindings = keyBindings ?? new KeyBindings();
        renderer.BatchCompleted += OnBatchCompleted;
        scene.Updated += OnSceneUpdated;
        foreach (var element in renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().SelectMany(Descendants))
        {
            element.SetFeature<IElementInteraction>(this);
            SyncDisabled(element);
        }
    }

    /// <summary>The deepest element under the pointer.</summary>
    public ElementNode? Hovered => hoverChain.Count == 0 ? null : hoverChain[0];

    /// <summary>Gets the element with keyboard focus, or null when no element is focused.</summary>
    public ElementNode? Focused { get; private set; }

    /// <summary>Gets the keyboard command registry used for input dispatch.</summary>
    public KeyBindings KeyBindings { get; }

    /// <summary>Gets the element receiving captured pointer input, or null when capture is inactive.</summary>
    public ElementNode? CapturedPointer { get; private set; }

    /// <summary>Occurs when pointer capture changes.</summary>
    public event Action<bool>? PointerCaptureChanged;

    /// <summary>Requests focus for the target element, optionally preserving scroll offsets. Requests made during pointer-down dispatch use pointer focus cues.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="preventScroll">Whether focusing should preserve the current scroll position.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task FocusAsync(ElementNode element, bool preventScroll = false) => renderer.Dispatcher.InvokeAsync(() =>
    {
        Focus(element, visible: !dispatchingPointerPress);
        if (!preventScroll && Focused == element) BringIntoView(element);
    });

    /// <summary>
    /// Captures pointer input for the target element until capture is released or canceled. Transferring capture
    /// raises <c>onlostpointercapture</c> on the previous live owner after the new owner takes capture.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true if pointer capture was acquired; otherwise, false.</returns>
    public bool CapturePointer(ElementNode element)
    {
        renderer.Dispatcher.AssertAccess();
        if (pointerButtons.Count == 0 || element.IsRemoved || !scene.IsRendered(element) || IsDisabled(element)) return false;
        if (CapturedPointer == element) return true;
        var previous = CapturedPointer;
        CapturedPointer = element;
        if (previous is not null) CancelRangePointer(previous);
        PointerCaptureChanged?.Invoke(true);
        if (previous is not null) Dispatch(previous, "onlostpointercapture", PointerArgs(lastPointer, PointerButton.None, lastPointerModifiers));
        return true;
    }

    /// <summary>Gets whether the specified element currently owns pointer capture.</summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>true while the element owns pointer capture; otherwise, false.</returns>
    public bool HasPointerCapture(ElementNode element) => CapturedPointer == element;

    /// <summary>Gets the element's border box in logical window coordinates, as <see cref="ElementReference.GetBounds" /> asks.</summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>The left and top edges, the width and the height, or null when the element is not laid out.</returns>
    public (double X, double Y, double Width, double Height)? GetBounds(ElementNode element)
    {
        if (element.IsRemoved || !scene.IsRendered(element)) return null;
        var bounds = scene.GetBounds(element);
        return (bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    /// <summary>Converts a logical window point to coordinates relative to the element's untransformed border-box origin, including its own and ancestor transforms and scrolling.</summary>
    /// <param name="element">The element whose local coordinate space receives the point.</param>
    /// <param name="windowX">The horizontal logical window coordinate.</param>
    /// <param name="windowY">The vertical logical window coordinate.</param>
    /// <returns>The local point, or null for nonfinite/out-of-range coordinates, detached/hidden/unlaid-out elements or singular transforms.</returns>
    public (double X, double Y)? GetLocalPoint(ElementNode element, double windowX, double windowY)
    {
        if (disposed || !float.IsFinite((float)windowX) || !float.IsFinite((float)windowY) || element.IsRemoved || !scene.IsRendered(element) ||
            scene.GetLayoutNode(element) is not { } layout || !scene.GetTransform(element).TryInvert(out var inverse)) return null;
        var point = inverse.Transform(new((float)windowX, (float)windowY));
        return float.IsFinite(point.X) && float.IsFinite(point.Y)
            ? (point.X - layout.RoundedFrame.X, point.Y - layout.RoundedFrame.Y) : null;
    }

    /// <summary>
    /// Scrolls a container to an offset, as <see cref="ElementReference.ScrollTo" /> asks; <c>onscroll</c> follows at
    /// once.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="left">The requested horizontal scroll offset, or null to keep it unchanged.</param>
    /// <param name="top">The requested vertical scroll offset, or null to keep it unchanged.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollTo(ElementNode element, double? left, double? top)
        => ScrollTo(element, left, top, immediate: false);

    private bool ScrollTo(ElementNode element, double? left, double? top, bool immediate)
    {
        if (element.IsRemoved || !scene.ScrollTo(element, (float?)left, (float?)top, immediate)) return false;
        DispatchScrollChanges();
        VisualStateChanged?.Invoke();
        return true;
    }

    private void DispatchScrollChanges()
    {
        foreach (var element in scene.TakeScrollChanges())
        {
            var scroll = scene.GetScrollState(element)!;
            Dispatch(element, "onscroll", new ScrollEventArgs
            {
                ScrollLeft = scroll.OffsetX, ScrollTop = scroll.OffsetY, ViewportWidth = scroll.ViewportWidth,
                ViewportHeight = scroll.ViewportHeight, ContentWidth = scroll.ContentWidth, ContentHeight = scroll.ContentHeight,
            });
        }
        foreach (var (element, args) in scene.TakeResizeChanges()) Dispatch(element, "onresize", args);
    }

    private void QueueScrollChanges()
    {
        if (scrollDispatchQueued) return;
        scrollDispatchQueued = true;
        if (renderer.Dispatcher is UIDispatcher dispatcher) dispatcher.Post(Dispatch);
        else Dispatch();

        void Dispatch()
        {
            scrollDispatchQueued = false;
            DispatchScrollChanges();
        }
    }

    /// <summary>
    /// Releases pointer capture held by the target element and raises <c>onlostpointercapture</c> on its live owner.
    /// Releasing an element that does not own capture has no effect. Cleanup notification also runs while disabled
    /// or while its modal surface exits.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    public void ReleasePointerCapture(ElementNode element)
    {
        renderer.Dispatcher.AssertAccess();
        if (CapturedPointer != element) return;
        CapturedPointer = null;
        CancelRangePointer(element);
        if (draggedScrollBar == element || pressedScrollBarButton == element)
        {
            draggedScrollBar = null;
            pressedScrollBarButton = null;
            scene.ReleaseScrollBarButton();
            UpdateScrollBarHover(scrollBarPointer, scrollBarPointer is { } point ? scene.HitTest(point) : null);
            VisualStateChanged?.Invoke();
        }
        PointerCaptureChanged?.Invoke(false);
        Dispatch(element, "onlostpointercapture", PointerArgs(lastPointer, PointerButton.None, lastPointerModifiers));
    }

    /// <summary>Raised when states or the focus change, so the host can schedule a frame.</summary>
    public event Action? VisualStateChanged;

    /// <summary>Processes the pointer moved notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void PointerMoved(Point point, KeyModifiers modifiers)
    {
        lastPointer = point;
        lastPointerModifiers = modifiers;
        var target = scene.HitTest(point);
        UpdateHover(target);
        UpdateScrollBarHover(point, target);
        if (ScrollBarPointerMoved(point)) return;
        if (Dispatch(CapturedPointer ?? target, "onpointermove", PointerArgs(point, PointerButton.None, modifiers))) return;
        RangePointerMoved(point);
        TextBoxPointerMoved(point);

        if (pressedByPointer is { } pressed) SetState(pressed, ElementStates.Pressed, IsWithin(scene.HitTest(point), pressed));
    }

    /// <summary>Processes the pointer pressed notification in logical viewport coordinates, preserving synchronous focus requests made by pointer handlers.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void PointerPressed(Point point, PointerButton button, KeyModifiers modifiers)
    {
        lastPointer = point;
        lastPointerModifiers = modifiers;
        var target = scene.HitTest(point);
        UpdateHover(target);
        pointerButtons.Add(button);
        HideTooltip();
        if (CapturedPointer is null && DismissOutside(target)) return;
        UpdateScrollBarHover(point, target);
        if (CapturedPointer is null && button == PointerButton.Primary && ScrollBarPointerPressed(target, point)) return;
        var previousFocusVersion = focusVersion;
        var previousDispatchingPointerPress = dispatchingPointerPress;
        dispatchingPointerPress = true;
        try
        {
            if (Dispatch(CapturedPointer ?? target, "onpointerdown", PointerArgs(point, button, modifiers))) return;
        }
        finally { dispatchingPointerPress = previousDispatchingPointerPress; }
        if (button != PointerButton.Primary) return;
        if (focusVersion == previousFocusVersion && !Ancestors(target).Any(element => element.Attributes.GetValueOrDefault("preserve-focus-on-press") is true))
            Focus(Ancestors(target).FirstOrDefault(IsFocusable), visible: false);
        if (RangePointerPressed(target, point)) return;
        if (FocusedText is { } box && Ancestors(target).Contains(box))
        {
            TextBoxPointerPressed(box, point, modifiers);
            return;
        }
        pressedByPointer = Ancestors(target).FirstOrDefault(IsActivatable);
        if (pressedByPointer is not null) SetState(pressedByPointer, ElementStates.Pressed, true);
    }

    /// <summary>Processes the pointer released notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void PointerReleased(Point point, PointerButton button, KeyModifiers modifiers)
    {
        lastPointer = point;
        lastPointerModifiers = modifiers;
        if (button == PointerButton.Primary && (draggedScrollBar ?? pressedScrollBarButton) is { } scrollContainer)
        {
            UpdateScrollBarHover(point, scene.HitTest(point));
            ScrollBarPointerMoved(point);
            pointerButtons.Remove(button);
            ReleasePointerCapture(scrollContainer);
            return;
        }
        var target = CapturedPointer ?? scene.HitTest(point);
        var prevented = Dispatch(target, "onpointerup", PointerArgs(point, button, modifiers));
        pointerButtons.Remove(button);
        if (button == PointerButton.Primary) RangePointerReleased();
        if (pointerButtons.Count == 0 && CapturedPointer is { } captured) ReleasePointerCapture(captured);
        if (button == PointerButton.Primary)
        {
            dragSelecting = null;
        }
        if (button == PointerButton.Secondary && !prevented && !Dispatch(target, "oncontextmenu", PointerArgs(point, button, modifiers)) &&
            TextBoxForMenu(target) is { } box)
            ShowTextMenu(box, point, clicked: true);
        if (button != PointerButton.Primary || pressedByPointer is not { } pressed) return;
        pressedByPointer = null;
        SetState(pressed, ElementStates.Pressed, false);
        if (!prevented && !pressed.IsRemoved && IsWithin(target, pressed))
            Dispatch(target, "onclick", PointerArgs(point, button, modifiers));
    }

    /// <summary>Processes the pointer left notification in logical viewport coordinates.</summary>
    public void PointerLeft()
    {
        UpdateHover(null, pointerInside: false);
        UpdateScrollBarHover(null, null);
    }

    /// <summary>Processes the pointer double clicked notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void PointerDoubleClicked(Point point, PointerButton button, KeyModifiers modifiers) =>
        Dispatch(CapturedPointer ?? scene.HitTest(point), "ondblclick", PointerArgs(point, button, modifiers));

    /// <summary>Opens the context menu associated with the focused element.</summary>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void ContextMenuRequested(KeyModifiers modifiers = KeyModifiers.None)
    {
        var bounds = Focused is { } focused ? scene.GetBounds(focused) : default;

        if (FocusedTextBox is not null && GetTextCaretRect() is { } caret) bounds = caret;
        if (!Dispatch(Focused ?? EventRoot, "oncontextmenu", new PointerEventArgs { IsSynthetic = true, Modifiers = modifiers, X = bounds.X, Y = bounds.Bottom }) &&
            TextBoxForMenu(Focused) is { } box)
            ShowTextMenu(box, new Point(bounds.X, bounds.Bottom), clicked: false);
    }

    /// <summary>How long the pointer rests on a control, or the keyboard focus, before its tooltip shows.</summary>
    public TimeSpan TooltipDelay
    {
        get => tooltipDelay;
        set
        {
            if (value < TimeSpan.Zero || value.TotalMilliseconds > uint.MaxValue-1) throw new ArgumentOutOfRangeException(nameof(value));
            tooltipDelay = value;
        }
    }
    private TimeSpan tooltipDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>The control whose tooltip is showing, or null.</summary>
    public ElementNode? TooltipTarget => tooltipTarget;

    /// <summary>The wheel moved by <paramref name="notches" /> (positive away from the user: scroll up).</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="notches">The number of wheel detents delivered by the backend.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    public void PointerWheel(Point point, float notches, KeyModifiers modifiers) => PointerWheel(point, 0, notches, modifiers);

    /// <summary>
    /// A wheel moved: <paramref name="deltaX" /> notches to the right (horizontal wheel, tilt),
    /// <paramref name="deltaY" /> away from the user. Unless a handler prevents it, the nearest container that can
    /// scroll on that axis scrolls; Shift turns the vertical wheel horizontal. Scrolling stops at the containing
    /// overlay surface, and an active modal surface blocks wheel interaction with its background.
    /// </summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="deltaX">The horizontal scroll delta in the specified unit.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <param name="unit">The unit used for the scroll deltas.</param>
    public void PointerWheel(Point point, float deltaX, float deltaY, KeyModifiers modifiers, ScrollDeltaUnit unit = ScrollDeltaUnit.Notches)
    {
        lastPointer = point;
        lastPointerModifiers = modifiers;
        var target = scene.HitTest(point);
        if (target is null || !InModalScope(target)) return;
        var args = new WheelEventArgs { X = point.X, Y = point.Y, DeltaX = deltaX, DeltaY = deltaY, Modifiers = modifiers, DeltaUnit = unit };
        if (Dispatch(target, "onwheel", args) || args.Handled) return;
        if (deltaX == 0 && deltaY == 0) return;
        var horizontal = deltaX != 0 ? deltaX : (modifiers & KeyModifiers.Shift) != 0 ? -deltaY : 0;
        var vertical = horizontal == 0 ? -deltaY : 0;
        ElementNode? wheelTarget = null;
        for (var element = target; element is not null; element = Scene.IsOverlay(element) ? null : element.ParentElement)
        {
            if (scene.GetScrollState(element) is not { } scroll || !(horizontal != 0 ? scroll.CanScrollX : scroll.CanScrollY)) continue;
            wheelTarget ??= element;
            if (!scene.ScrollWheel(element, horizontal, vertical, unit, WheelScrollDistance, WheelScrollByPage)) continue;
            scene.NotifyWheelScroll(element);
            DispatchScrollChanges();

            UpdateHover(scene.HitTest(point));
            VisualStateChanged?.Invoke();
            return;
        }
        if (wheelTarget is not null)
        {
            scene.NotifyWheelScroll(wheelTarget);
            VisualStateChanged?.Invoke();
        }
    }

    /// <summary>The platform cancelled the interaction (capture lost, window deactivated).</summary>
    public void PointerCanceled() => PointerCanceled(clearHover: true);

    /// <summary>Cancels a gesture; inspection preserves the hover while taking over the pointer.</summary>
    /// <param name="clearHover">Whether the current pointer hover state should also be cleared.</param>
    public void PointerCanceled(bool clearHover)
    {
        if (pressedByPointer is { } pressed) SetState(pressed, ElementStates.Pressed, false);
        pressedByPointer = null;
        pointerButtons.Clear();
        if (CapturedPointer is { } captured) ReleasePointerCapture(captured);
        dragSelecting = null;
        draggedRange = null;
        if (clearHover)
        {
            UpdateHover(null, pointerInside: false);
            UpdateScrollBarHover(null, null);
        }
    }

    /// <summary>Handles navigation and activation keys; returns true when the key was consumed.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <param name="repeat">Whether this key event is generated by auto-repeat.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool KeyDown(string key, KeyModifiers modifiers, bool repeat)
    {
        if (ModalIsExiting) return true;
        var args = new KeyboardEventArgs { Key = key, Modifiers = modifiers, Repeat = repeat };
        if (KeyBindings.TryGetCommand(args, out var command))
        {
            Observe(renderer.DispatchEventAsync(command, args));
            return true;
        }
        var focusedBefore = Focused;
        if (Dispatch(Focused ?? EventRoot, "onkeydown", args) || args.Handled) return true;

        if (Focused != focusedBefore) return true;
        if (key == "ContextMenu" || key == "F10" && modifiers == KeyModifiers.Shift)
        {
            ContextMenuRequested(modifiers);
            return true;
        }

        if (key == "Escape" && (tooltipTarget is not null || scene.Overlays.Any(IsTooltip)))
        {
            var controller = tooltipTarget is { } target ? TooltipController(target) : null;
            foreach (var tooltip in scene.Overlays.Where(IsTooltip).ToArray()) Dispatch(tooltip,"ondismiss",EventArgs.Empty);
            if (controller?.ContentElement is not { } content || !Scene.IsOverlay(content)) HideTooltip();
            return true;
        }
        if (key == "Escape" && tooltipPending is not null) HideTooltip();
        if (key == "Escape" && pressedByKey is null && scene.Overlays.LastOrDefault(o => !IsTooltip(o)) is { } top && (top.Attributes.GetValueOrDefault("escape-dismiss") is bool escape ? escape : IsLightDismiss(top)))
        {
            Dispatch(top, "ondismiss", EventArgs.Empty);
            return true;
        }

        if (key == "Tab" && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 && scene.Overlays.LastOrDefault(overlay=>overlay.Attributes.GetValueOrDefault("menu") is true) is { } currentMenu)
        {
            while (overlayOwners.GetValueOrDefault(currentMenu) is { } parent && parent.Attributes.GetValueOrDefault("menu") is true) currentMenu = parent;
            Dispatch(currentMenu,"ondismiss",EventArgs.Empty);
            MoveFocus((modifiers & KeyModifiers.Shift) != 0 ? -1 : 1); return true;
        }

        if (key == "ArrowLeft" && modifiers == KeyModifiers.None && MenuOf(Focused) is { } submenu &&
            submenu.Attributes.GetValueOrDefault("submenu") is true)
        {
            Dispatch(submenu, "ondismiss", EventArgs.Empty);
            return true;
        }

        if (key is "ArrowDown" or "ArrowUp" or "Home" or "End" && Focused is { } current && MenuOf(current) is { } menu && scene.Overlays.Contains(menu))
        {
            var items = Descendants(menu).Where(e => IsFocusable(e) && TabIndex(e) >= 0 && MenuOf(e)==menu && e.Attributes.GetValueOrDefault("role") is "menuitem" or "menuitemcheckbox" or "menuitemradio" or "option").ToList();
            if (items.Count == 0) return true;
            var index = items.IndexOf(current);
            var next = key switch
            {
                "Home" => 0,
                "End" => items.Count - 1,
                "ArrowDown" => Math.Min(index + 1, items.Count - 1),
                _ => index < 0 ? items.Count - 1 : Math.Max(index - 1, 0),
            };
            Focus(items[next], visible: true);
            BringIntoView(items[next]);
            return true;
        }

        if (key is "ArrowDown" or "ArrowRight" or "ArrowUp" or "ArrowLeft" or "Home" or "End" && Focused is { } choice && ChoiceGroupOf(choice) is { } group &&
            (key is not ("Home" or "End") || choice.Attributes.GetValueOrDefault("role") is "tab"))
        {
            var role = choice.Attributes["role"];
            var choices = Descendants(group).Where(e => Equals(e.Attributes.GetValueOrDefault("role"), role) && IsFocusable(e)).ToList();
            var index = key switch
            {
                "Home" => 0,
                "End" => choices.Count - 1,
                _ => (choices.IndexOf(choice) + (key is "ArrowDown" or "ArrowRight" ? 1 : -1) + choices.Count) % choices.Count,
            };
            var next = choices[index];
            Focus(next, visible: true);
            Activate(next, new PointerEventArgs { IsSynthetic = true, Modifiers = modifiers });
            return true;
        }
        if (Focused is { } range && IsRange(range) && RangeKeyDown(range, key)) return true;

        if (Focused is { } combo && combo.Attributes.GetValueOrDefault("role") is "combobox" && key is "ArrowDown" or "ArrowUp" or "Home" or "End" or "F4") return true;
        if (Focused is { } list && list.Attributes.GetValueOrDefault("role") is "listbox" && key is "ArrowDown" or "ArrowUp" or "Home" or "End" or "PageUp" or "PageDown" or " ") return true;
        if (Focused is { } tree && tree.Attributes.GetValueOrDefault("role") is "tree" && key is "ArrowDown" or "ArrowUp" or "ArrowLeft" or "ArrowRight" or "Home" or "End" or "PageUp" or "PageDown" or " ") return true;
        if (Focused is { } splitter && splitter.Attributes.GetValueOrDefault("role") is "splitter" && key is "ArrowLeft" or "ArrowRight" or "Home" or "End") return true;

        if (key == "Enter" && FocusedTextBox is { } textBox && !IsMultiline(textBox)) CommitTextChange(textBox);
        if (FocusedTextBox is { } box && TextBoxKeyDown(box, key, modifiers)) return true;
        if (FocusedText is { } selectable && !IsTextBox(selectable) && SelectableTextKeyDown(selectable, key, modifiers)) return true;

        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 && Focused is { } item && BarOf(item) is { } bar && BarStep(bar, key) is { } step)
        {
            var items = BarItems(bar);
            var index = items.IndexOf(item);
            var next = step switch
            {
                int.MinValue => 0,
                int.MaxValue => items.Count - 1,
                _ => (index + step + items.Count) % items.Count,
            };
            Focus(items[next], visible: true);
            return true;
        }
        switch (key)
        {
            case "Tab" when (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0:
                MoveFocus((modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);
                return true;
            case "Enter" when Focused is { } focused && IsActivatable(focused):
                if (!repeat) { HideTooltip(); Activate(focused,new PointerEventArgs { IsSynthetic=true,Modifiers=modifiers }); }
                return true;

            case "Enter" when DefaultButton() is { } button:
                if (!repeat) { HideTooltip(); Activate(button,new PointerEventArgs { IsSynthetic=true,Modifiers=modifiers }); }
                return true;

            case "Enter" when Focused is { } editor && FormOf(editor) is { } form:
                if (!repeat && form.Attributes.GetValueOrDefault("__form-submit") is EventCallback submit) Observe(submit.InvokeAsync());
                return true;

            case " " when (modifiers & KeyModifiers.Alt) == 0 && Focused is { } focused && IsActivatable(focused):
                if (!repeat)
                {
                    HideTooltip(); pressedByKey = focused;
                    SetState(focused, ElementStates.Pressed, true);
                }
                return true;
            case "Escape" when pressedByKey is { } pressed:
                pressedByKey = null;
                SetState(pressed, ElementStates.Pressed, false);
                return true;
            case "PageUp" or "PageDown" or "Home" or "End" when scene.FindScrollContainer(KeyboardScrollTarget) is { } container:
                var state = scene.GetScrollState(container)!;

                var sideways = !state.CanScrollY;
                var delta = key switch
                {
                    "PageUp" => -(sideways ? state.ViewportWidth : state.ViewportHeight) * 0.9f,
                    "PageDown" => (sideways ? state.ViewportWidth : state.ViewportHeight) * 0.9f,
                    "Home" => -(sideways ? state.OffsetX : state.OffsetY),
                    _ => sideways ? state.MaxOffsetX - state.OffsetX : state.MaxOffsetY - state.OffsetY,
                };
                var moved = key is "Home" or "End"
                    ? scene.ScrollTo(container, sideways ? key == "Home" ? 0 : state.MaxOffsetX : null,
                        sideways ? null : key == "Home" ? 0 : state.MaxOffsetY)
                    : sideways ? scene.ScrollBy(container, delta, 0) : scene.ScrollBy(container, delta);
                if (!moved) return false;
                DispatchScrollChanges();
                UpdateHover(scene.HitTest(lastPointer));
                VisualStateChanged?.Invoke();
                return true;
        }
        return false;
    }

    /// <summary>Processes a released key and reports whether it was handled.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool KeyUp(string key, KeyModifiers modifiers)
    {
        if (ModalIsExiting) return true;
        var prevented = Dispatch(Focused ?? EventRoot, "onkeyup", new KeyboardEventArgs { Key = key, Modifiers = modifiers });
        if (key != " " || pressedByKey is not { } pressed) return prevented;
        pressedByKey = null;
        SetState(pressed, ElementStates.Pressed, false);
        if (!prevented && !pressed.IsRemoved && pressed == Focused) Activate(pressed, new PointerEventArgs { IsSynthetic = true, Modifiers = modifiers });
        return true;
    }

    /// <summary>
    /// Moves the focus; <paramref name="visible" /> marks keyboard-originated focus (<c>:focus-visible</c>).
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="visible">Whether the window, control, or native decoration should be visible.</param>
    public void Focus(ElementNode? element, bool visible)
    {
        if (element is not null && (!IsFocusable(element) || !CanInteract(element))) return;
        focusVersion++;
        if (Focused == element)
        {
            if (element is not null) SetState(element, ElementStates.FocusVisible, visible);
            return;
        }
        var previousElement = Focused;
        Focused = element;
        if (previousElement is { } previous)
        {
            if (IsTextBox(previous) && !previous.IsRemoved) OnTextBoxBlurred(previous);
            else if (previous.GetFeature<TextEditState>() is not null && !previous.IsRemoved) OnSelectableTextBlurred(previous);
            SetState(previous, ElementStates.Focus, false);
            SetState(previous, ElementStates.FocusVisible, false);
            Dispatch(previous, "onblur", new FocusEventArgs { RelatedTarget = element });
            Dispatch(previous, "onfocusout", new FocusEventArgs { RelatedTarget = element });
        }
        if (element is { IsRemoved: false } && Focused == element)
        {
            SetState(element, ElementStates.Focus, true);
            SetState(element, ElementStates.FocusVisible, visible);
            if (IsTextBox(element)) OnTextBoxFocused(element, keyboard: visible);
            Dispatch(element, "onfocus", new FocusEventArgs { RelatedTarget = previousElement });
            Dispatch(element, "onfocusin", new FocusEventArgs { RelatedTarget = previousElement });
        }
        SetFocusWithin(Focused);
        if (Focused is { } inBar && BarOf(inBar) is { } bar) barFocus[bar] = inBar;

        if (visible && Ancestors(element).FirstOrDefault(HasTooltip) is { } withTooltip) ScheduleTooltip(withTooltip);
        else if (TooltipOwner is { } owner && !IsWithin(element,owner) && !hoverChain.Contains(owner)) HideTooltip();
        VisualStateChanged?.Invoke();
    }

    private static string? TooltipOf(ElementNode element) => element.Attributes.GetValueOrDefault("tooltip") is string { Length: > 0 } text ? text : null;
    private static ITooltipController? TooltipController(ElementNode element) => element.Attributes.GetValueOrDefault("tooltip-controller") as ITooltipController;
    private static bool HasTooltip(ElementNode element) => TooltipOf(element) is not null || TooltipController(element)?.IsAvailable is true;
    private ElementNode? TooltipOwner => tooltipTarget ?? tooltipPending;
    private bool KeyboardOwnsTooltip(ElementNode owner) => Focused is { } focused && IsWithin(focused,owner) && (focused.States & ElementStates.FocusVisible) != 0;
    private bool IsTooltipContent(ElementNode element) => IsTooltip(element) && tooltipTarget is { } owner &&
        (TooltipController(owner)?.ContentElement == element || element.Attributes.GetValueOrDefault("anchor") == owner);

    private bool PointerWithinTooltipContent() => scene.Overlays.Any(element => IsTooltipContent(element) &&
        element.Attributes.GetValueOrDefault("exiting") is not true && scene.GetLayoutNode(element) is { } layout &&
        scene.GetTransform(element).TryInvert(out var inverse) && layout.RoundedFrame.Contains(inverse.Transform(lastPointer)));

    private void ScheduleTooltip(ElementNode element)
    {
        if (disposed) return;
        if (TooltipOwner == element) return;
        HideTooltip(); tooltipPending = element;
        tooltipCancellation = new CancellationTokenSource();
        Observe(ShowTooltipAfterDelay(element,tooltipGeneration,TooltipController(element)?.Delay ?? TooltipDelay,tooltipCancellation.Token));
    }
    private async Task ShowTooltipAfterDelay(ElementNode element,int generation,TimeSpan delay,CancellationToken cancellation)
    {
        try
        {
            await Task.Delay(delay,cancellation).ConfigureAwait(false);
            if (cancellation.IsCancellationRequested) return;
            await renderer.Dispatcher.InvokeAsync(()=>
            {
                if (disposed || cancellation.IsCancellationRequested || generation != tooltipGeneration || element.IsRemoved || !HasTooltip(element)) return;
                (tooltipTarget,tooltipPending) = (element,null);
                if (TooltipController(element) is { } controller) Observe(renderer.DispatchEventAsync(EventCallback.Factory.Create(controller,controller.OpenAsync),null));
                else
                {
                    tooltipHost ??= renderer.AddRootComponent<TooltipHost>();
                    ((TooltipHost)tooltipHost.Component).Show(element,TooltipOf(element)!);
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (disposed) { }
    }
    private void HideTooltip()
    {
        tooltipGeneration++;
        tooltipCancellation?.Cancel(); tooltipCancellation?.Dispose(); tooltipCancellation = null;
        tooltipPending = null;
        if (tooltipTarget is not { } target) return;
        tooltipTarget = null;
        if (TooltipController(target) is { } controller)
        {
            if (controller.IsAvailable) Observe(renderer.DispatchEventAsync(EventCallback.Factory.Create(controller,controller.CloseAsync),null));
        }
        else ((TooltipHost?)tooltipHost?.Component)?.Hide();
    }

    private static bool IsTooltip(ElementNode overlay) => overlay.Attributes.GetValueOrDefault("role") is "tooltip";

    private static ElementNode? ChoiceGroupOf(ElementNode element) => element.Attributes.GetValueOrDefault("role") switch
    {
        "radio" => Ancestors(element).Skip(1).FirstOrDefault(e => e.Attributes.GetValueOrDefault("role") is "radiogroup"),
        "tab" => Ancestors(element).Skip(1).FirstOrDefault(e => e.Attributes.GetValueOrDefault("role") is "tablist"),
        _ => null,
    };

    private static ElementNode? BarOf(ElementNode element)
    {
        foreach (var ancestor in Ancestors(element).Skip(1))
        {
            if (Scene.IsOverlay(ancestor)) return null;
            if (ancestor.Attributes.GetValueOrDefault("role") is "toolbar" or "menubar") return ancestor;
        }
        return null;
    }

    private List<ElementNode> BarItems(ElementNode bar) => [.. Descendants(bar).Where(e => IsFocusable(e) && TabIndex(e) >= 0 && BarOf(e) == bar)];

    private static int? BarStep(ElementNode bar, string key) => (key, bar.Attributes.GetValueOrDefault("vertical") is true) switch
    {
        ("Home", _) => int.MinValue,
        ("End", _) => int.MaxValue,
        ("ArrowRight", false) or ("ArrowDown", true) => 1,
        ("ArrowLeft", false) or ("ArrowUp", true) => -1,
        _ => null,
    };

    private ElementNode? BarStop(ElementNode bar, List<ElementNode> focusable)
    {
        if (Focused is { } focused && BarOf(focused) == bar) return focused;
        if (barFocus.TryGetValue(bar, out var last) && focusable.Contains(last)) return last;
        return focusable.FirstOrDefault(e => BarOf(e) == bar);
    }

    /// <summary>
    /// What a point of the window is for the system, in a window with the content in the title bar: an element with
    /// the attribute <c>window-drag</c> (a title bar) moves the window where no control that takes the pointer is
    /// above it. Focusable controls, pointer event handlers, tooltips, selectable text and scrollable content keep
    /// their client input, including composite control frames. A <c>snap-layout-area</c> ancestor marks the native maximize region without changing the target of
    /// pointer events.
    /// </summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting WindowRegion value.</returns>
    public WindowRegion RegionAt(Point point)
    {
        if (CapturedPointer is not null) return WindowRegion.Client;
        var ancestors = Ancestors(scene.HitTest(point)).TakeWhile(e => !Scene.IsOverlay(e)).ToArray();
        if (ancestors.Any(e => e.Attributes.GetValueOrDefault("snap-layout-area") is true &&
            scene.GetStyle(e).Get(StyleProperties.Visibility) == Visibility.Visible &&
            scene.GetStyle(e).Get(StyleProperties.PointerEvents) == PointerEvents.Auto &&
            scene.GetLayoutNode(e) is { } layout && scene.GetTransform(e).TryInvert(out var inverse) &&
            layout.RoundedFrame.Contains(inverse.Transform(point))))
            return WindowRegion.MaximizeButton;
        foreach (var element in ancestors)
        {
            if (IsActivatable(element) || IsFocusable(element) || HasTooltip(element) ||
                !IsDisabled(element) && (HasPointerHandler(element) || CanSelectText(element) || scene.GetScrollState(element)?.CanScroll == true))
                return WindowRegion.Client;
            if (element.Attributes.GetValueOrDefault("window-drag") is true) return WindowRegion.Caption;
        }
        return WindowRegion.Client;
    }

    private static bool HasPointerHandler(ElementNode element)
    {
        foreach (var (key, value) in element.Attributes)
        {
            var name = key.AsSpan();
            if (name.StartsWith("__internal_", StringComparison.Ordinal)) name = name[11..];
            else if (name.StartsWith("__default_", StringComparison.Ordinal)) name = name[10..];
            if (name.StartsWith("preventDefault_", StringComparison.Ordinal)) name = name[15..];
            else if (name.StartsWith("stopPropagation_", StringComparison.Ordinal)) name = name[16..];
            var tag = name.IndexOf('#');
            if (tag >= 0) name = name[..tag];
            if ((value is IEventCallback { HasDelegate: true } || value is true) &&
                name is "onclick" or "ondblclick" or "oncontextmenu" or "onpointerdown" or "onpointerup" or
                    "onpointermove" or "onpointerenter" or "onpointerleave" or "onwheel") return true;
        }
        return false;
    }

    private ElementNode? DefaultButton()
    {
        var form = Focused is { } focused ? FormOf(focused) : null;
        if (form is null && scene.Overlays.Any(o => !IsTooltip(o)))
        {
            var overlay = Ancestors(Focused).FirstOrDefault(Scene.IsOverlay);
            return overlay?.Attributes.GetValueOrDefault("role") is "dialog" or "alertdialog"
                ? Descendants(overlay).FirstOrDefault(element => IsActivatable(element) && IsFocusable(element) &&
                    element.Attributes.GetValueOrDefault("default") is true && Ancestors(element).FirstOrDefault(Scene.IsOverlay) == overlay)
                : null;
        }
        return renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().SelectMany(Descendants)
            .FirstOrDefault(element => IsActivatable(element) && IsFocusable(element) &&
                (form is null ? element.Attributes.GetValueOrDefault("default") is true && !Ancestors(element).Any(node => node.Attributes.GetValueOrDefault("role") is "form")
                    : FormOf(element) == form &&
                      (element.Attributes.GetValueOrDefault("default") is true || element.Attributes.GetValueOrDefault("button-type") is "submit")));
    }

    private static ElementNode? FormOf(ElementNode element) => Ancestors(element).TakeWhile(node=>!Scene.IsOverlay(node)).FirstOrDefault(node=>node.Attributes.GetValueOrDefault("role") is "form");

    private static ElementNode? MenuOf(ElementNode? element) => Ancestors(element).FirstOrDefault(e => Scene.IsOverlay(e) && e.Attributes.GetValueOrDefault("menu") is true);

    private void SetFocusWithin(ElementNode? element)
    {
        var chain = new List<ElementNode>();
        for (var current = element; current is not null; current = current.ParentElement) chain.Add(current);
        foreach (var old in focusWithin)
            if (!chain.Contains(old)) SetState(old, ElementStates.FocusWithin, false, notify: false);
        foreach (var item in chain) SetState(item, ElementStates.FocusWithin, true, notify: false);
        focusWithin.Clear();
        focusWithin.AddRange(chain);
    }

    /// <summary>
    /// Activates an element as a click would (semantic action, for example from accessibility tools): false when it
    /// is disabled or has no <c>onclick</c> handler.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Activate(ElementNode element) => Activate(element, KeyModifiers.None);

    /// <summary>
    /// Activates an element as a click with modifiers does (Ctrl+click adds an item to a selection).
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <param name="modifiers">The modifier keys pressed when the input occurred.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Activate(ElementNode element, KeyModifiers modifiers)
    {
        if (!CanInteract(element) || !IsActivatable(element)) return false;
        HideTooltip(); Activate(element, new PointerEventArgs { IsSynthetic = true, Modifiers = modifiers });
        return true;
    }

    /// <summary>
    /// Brings an element inside the visible area of its scrolling ancestors, stopping at its overlay surface;
    /// true when something scrolled.
    /// </summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool BringIntoView(ElementNode element)
    {
        if (!scene.BringIntoView(element)) return false;
        DispatchScrollChanges();
        UpdateHover(scene.HitTest(lastPointer));
        VisualStateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Scrolls a scrolling container by <paramref name="deltaY" /> logical units; true when it moved.
    /// </summary>
    /// <param name="container">The container (ElementNode).</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(ElementNode container, float deltaY) => ScrollBy(container, 0, deltaY);

    /// <summary>Scrolls a scrolling container on both axes, in logical units; true when it moved.</summary>
    /// <param name="container">The container (ElementNode).</param>
    /// <param name="deltaX">The horizontal scroll delta in the specified unit.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(ElementNode container, float deltaX, float deltaY)
    {
        if (!scene.ScrollBy(container, deltaX, deltaY)) return false;
        UpdateHover(scene.HitTest(lastPointer));
        VisualStateChanged?.Invoke();
        return true;
    }

    /// <summary>The window lost or regained the keyboard focus.</summary>
    /// <param name="focused">Whether the native window has keyboard focus.</param>
    public void WindowFocusChanged(bool focused)
    {
        if (windowFocused == focused) return;
        windowFocused = focused;
        if (Focused is { } element)
        {
            if (!focused && IsTextBox(element)) OnTextBoxBlurred(element);
            Dispatch(element, focused ? "onfocus" : "onblur", new FocusEventArgs());
            Dispatch(element, focused ? "onfocusin" : "onfocusout", new FocusEventArgs());
        }
        if (focused) return;
        if (pressedByKey is { } pressed) SetState(pressed, ElementStates.Pressed, false);
        pressedByKey = null;
        PointerCanceled();
    }

    /// <summary>The cursor requested by the style of the element under <paramref name="point" />.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting Cursor value.</returns>
    public Cursor CursorAt(Point point) => scene.HitTest(point) is { } element ? scene.GetStyle(element).Get(StyleProperties.Cursor) : Cursor.Default;

    private void MoveFocus(int direction)
    {

        var roots = activeModal is { } modal
            ? new[] { modal }.Concat(overlayModalScopes.Where(pair=>pair.Value==modal && !pair.Key.IsRemoved && Scene.IsOverlay(pair.Key)).Select(pair=>pair.Key))
            : renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>();
        var all = roots.SelectMany(Descendants).Distinct()
            .Where(e => IsFocusable(e) && TabIndex(e) >= 0).OrderBy(e => TabIndex(e) > 0 ? 0 : 1).ThenBy(TabIndex).ToList();

        var stops = new Dictionary<ElementNode, ElementNode?>(ReferenceEqualityComparer.Instance);
        var focusable = all.Where(e => BarOf(e) is not { } bar || e == (stops.TryGetValue(bar, out var stop) ? stop : stops[bar] = BarStop(bar, all))).ToList();
        if (focusable.Count == 0) return;
        var index = Focused is null ? -1 : focusable.IndexOf(Focused);
        var next = index < 0 ? (direction > 0 ? 0 : focusable.Count - 1) : (index + direction + focusable.Count) % focusable.Count;
        Focus(focusable[next], visible: true);

        if (scene.BringIntoView(focusable[next]))
        {
            DispatchScrollChanges();
            VisualStateChanged?.Invoke();
        }
    }

    private void Activate(ElementNode element, PointerEventArgs args)
    {
        if (!IsActivatable(element)) return;
        Dispatch(element, "onclick", args);
        VisualStateChanged?.Invoke();
    }

    private void UpdateHover(ElementNode? target, bool pointerInside = true)
    {
        if (target is not null && !InModalScope(target)) target = null;
        var chain = Ancestors(target).ToList();
        if (chain.FirstOrDefault(element=>HasTooltip(element) && InModalScope(element)) is { } withTooltip) ScheduleTooltip(withTooltip);
        else if (TooltipOwner is { } owner && !KeyboardOwnsTooltip(owner) && !chain.Any(IsTooltipContent) &&
            (!pointerInside || !PointerWithinTooltipContent())) HideTooltip();
        if (chain.SequenceEqual(hoverChain)) return;
        if (target is not null && MenuOf(target) is { } menu && chain.FirstOrDefault(element=>element.Attributes.GetValueOrDefault("role") is "menuitem" or "menuitemcheckbox" or "menuitemradio") is { } menuItem && IsFocusable(menuItem))
        {
            foreach (var child in scene.Overlays.Where(overlay=>overlay.Attributes.GetValueOrDefault("submenu") is true && overlayOwners.GetValueOrDefault(overlay)==menu &&
                !(overlay.Attributes.GetValueOrDefault("anchor") is ElementNode anchor && IsWithin(target,anchor))).ToArray())
                Dispatch(child,"ondismiss",EventArgs.Empty);
            Focus(menuItem,visible:false);
        }

        var leaving = hoverChain.Except(chain).ToArray();
        var entering = chain.Except(hoverChain).Reverse().ToArray();
        foreach (var element in leaving) SetState(element, ElementStates.Hover, false, notify: false);
        foreach (var element in entering) SetState(element, ElementStates.Hover, true, notify: false);
        hoverChain.Clear();
        hoverChain.AddRange(chain);
        foreach (var element in leaving) Dispatch(element, "onpointerleave", PointerArgs(lastPointer, PointerButton.None, lastPointerModifiers));
        foreach (var element in entering) Dispatch(element, "onpointerenter", PointerArgs(lastPointer, PointerButton.None, lastPointerModifiers));
        VisualStateChanged?.Invoke();
    }

    private void SetState(ElementNode element, ElementStates state, bool value, bool notify = true)
    {
        if (element.IsRemoved || ((element.States & state) != 0) == value) return;
        element.SetState(state, value);
        if (notify) VisualStateChanged?.Invoke();
    }

    private void OnBatchCompleted(object? sender, RenderBatch batch)
    {
        foreach (var element in batch.AddedElements.Concat(batch.UpdatedElements))
        {
            element.SetFeature<IElementInteraction>(this);
            SyncDisabled(element);
            if (IsTextBox(element)) SyncTextBox(element);
        }
        foreach (var element in batch.AddedElements)
            if (Scene.IsOverlay(element) && scene.IsRendered(element) && observedOverlays.Add(element)) OverlayOpened(element);
        activeModal = scene.Overlays.LastOrDefault(IsModal);
        if (Focused is { } focused && (focused.IsDisabled || ModalIsExiting)) Focus(null, visible: false);
        RestoreFocusOfClosedOverlays();
        foreach (var (overlay,owner) in overlayOwners.Where(pair=>pair.Value.IsRemoved || !Scene.IsOverlay(pair.Value)).ToArray())
        {
            overlayOwners.Remove(overlay); overlayModalScopes.Remove(overlay);
            if (!overlay.IsRemoved && Scene.IsOverlay(overlay)) Dispatch(overlay,"ondismiss",EventArgs.Empty);
        }
        if (TooltipOwner is { IsRemoved:true }) HideTooltip();
        else if (tooltipTarget is { } tooltip && TooltipController(tooltip) is null && TooltipOf(tooltip) is { } text && tooltipHost?.Component is TooltipHost host)
            host.Show(tooltip,text);
        if (CapturedPointer is { } captured && (captured.IsRemoved || IsDisabled(captured))) PointerCanceled();
        if (batch.RemovedNodes.Count == 0) return;
        foreach (var element in batch.RemovedNodes.OfType<ElementNode>()) element.SetFeature<IElementInteraction>(null);
        foreach (var box in committedText.Keys.Where(e => e.IsRemoved).ToArray()) committedText.Remove(box);
        foreach (var bar in barFocus.Where(b => b.Key.IsRemoved || b.Value.IsRemoved).Select(b => b.Key).ToArray()) barFocus.Remove(bar);
        if (Focused is { IsRemoved: true })
        {
            Focused = null;
            SetFocusWithin(null);
        }
        if (pressedByPointer is { IsRemoved: true }) pressedByPointer = null;
        if (pressedByKey is { IsRemoved: true }) pressedByKey = null;
        if (dragSelecting is { IsRemoved: true }) dragSelecting = null;
        if (hoverChain.Any(e => e.IsRemoved))
        {
            hoverChain.RemoveAll(e => e.IsRemoved);
            UpdateHover(scene.HitTest(lastPointer));
        }
    }

    private void OverlayOpened(ElementNode overlay)
    {
        activeModal = scene.Overlays.LastOrDefault(IsModal);
        var anchor = overlay.Attributes.GetValueOrDefault("anchor") as ElementNode;
        var owner = Ancestors(overlay.ParentElement).Concat(Ancestors(anchor)).Concat(Ancestors(Focused))
            .FirstOrDefault(element=>element != overlay && !element.IsRemoved && Scene.IsOverlay(element));
        if (owner is not null) overlayOwners[overlay] = owner;
        if (owner is not null && overlay.Attributes.GetValueOrDefault("submenu") is true)
            foreach (var sibling in scene.Overlays.Where(other=>other != overlay && other.Attributes.GetValueOrDefault("submenu") is true && overlayOwners.GetValueOrDefault(other)==owner).ToArray())
                Dispatch(sibling,"ondismiss",EventArgs.Empty);
        if (activeModal is { } modal && overlay != modal &&
            (IsWithin(overlay,modal) || Focused is { } origin && CanInteract(origin) || overlay.Attributes.GetValueOrDefault("anchor") is ElementNode anchorElement && CanInteract(anchorElement)))
            overlayModalScopes[overlay] = modal;
        if (IsTooltip(overlay)) return;
        var returnFocus = overlay.Attributes.GetValueOrDefault("return-focus") as ElementNode ?? Focused;
        overlayOrigins.TryAdd(overlay, (returnFocus,returnFocus is { } focused && (focused.States & ElementStates.FocusVisible) != 0));
        if (!IsModal(overlay) && overlay.Attributes.GetValueOrDefault("focus-first") is not true) return;

        var preferred = overlay.Attributes.GetValueOrDefault("initial-focus") is Func<ElementReference?> resolve ? resolve()?.Element : overlay.Attributes.GetValueOrDefault("initial-focus") as ElementNode;
        var cancel = overlay.Attributes.GetValueOrDefault("prefer-cancel") is true ? Descendants(overlay).FirstOrDefault(e=>e.Attributes.GetValueOrDefault("dialog-cancel") is true) : null;
        var first = preferred is not null && IsWithin(preferred,overlay) && IsFocusable(preferred) ? preferred :
            (cancel is null ? null : Descendants(cancel).FirstOrDefault(IsFocusable)) ??
            Descendants(overlay).FirstOrDefault(e => e.Attributes.GetValueOrDefault("autofocus") is true && IsFocusable(e)) ??
            Descendants(overlay).FirstOrDefault(e => e.Attributes.GetValueOrDefault("selected") is true && IsFocusable(e)) ??
            Descendants(overlay).Where(e => IsFocusable(e) && TabIndex(e) >= 0).OrderBy(e => TabIndex(e) > 0 ? 0 : 1).ThenBy(TabIndex).FirstOrDefault();
        if (first is not null)
        {
            Focus(first, visible: overlayOrigins[overlay].Visible);
            pendingFocusScroll = first;
        }
        else if (IsFocusable(overlay)) Focus(overlay,visible:overlayOrigins[overlay].Visible);
    }

    private void RestoreFocusOfClosedOverlays()
    {
        foreach (var (overlay, origin) in overlayOrigins.Where(o => o.Key.IsRemoved || !Scene.IsOverlay(o.Key) || !scene.IsRendered(o.Key)).ToArray())
        {
            var focusInside = Focused is null || Focused.IsRemoved || IsWithin(Focused, overlay) || Ancestors(Focused).Any(element=>IsOwnedBy(element,overlay));
            if (focusInside && origin.Element is { IsRemoved: false } element && IsFocusable(element))
            {
                if (!CanInteract(element)) continue;
                overlayOrigins.Remove(overlay); Focus(element,origin.Visible);
            }
            else overlayOrigins.Remove(overlay);
        }
    }

    private bool IsOwnedBy(ElementNode element,ElementNode owner)
    {
        for (var depth=0; depth<=overlayOwners.Count && overlayOwners.TryGetValue(element,out var parent); depth++)
        {
            if (parent==owner) return true;
            element=parent;
        }
        return false;
    }

    private bool DismissOutside(ElementNode? target)
    {
        var consumed = false;
        foreach (var overlay in scene.Overlays.Reverse().Where(o => !IsTooltip(o)))
        {
            if (IsWithin(target, overlay)) break;
            if (!IsModal(overlay) && overlay.Attributes.GetValueOrDefault("anchor-interaction") is true && target is not null &&
                overlay.Attributes.GetValueOrDefault("anchor") is ElementNode { IsRemoved: false } interactionAnchor &&
                scene.IsRendered(interactionAnchor) && IsWithin(target, interactionAnchor) && CanInteract(target)) break;
            if (IsLightDismiss(overlay))
            {
                var parentMenu = target is null ? null : MenuOf(target);
                var withinParentMenu = overlay.Attributes.GetValueOrDefault("submenu") is true && parentMenu is not null && IsOwnedBy(overlay,parentMenu) &&
                    !(overlay.Attributes.GetValueOrDefault("anchor") is ElementNode anchor && IsWithin(target,anchor));
                Dispatch(overlay,"ondismiss",EventArgs.Empty);
                consumed |= !withinParentMenu;
            }
            if (IsModal(overlay)) return true;
        }
        return consumed;
    }

    private static bool IsModal(ElementNode overlay) => overlay.Attributes.GetValueOrDefault("modal") is true;

    private static bool IsLightDismiss(ElementNode overlay) => overlay.Attributes.GetValueOrDefault("light-dismiss") is true;

    private void SyncDisabled(ElementNode element)
    {
        var disabled = element.Attributes.TryGetValue("disabled", out var value) && value is true;
        SetState(element, ElementStates.Disabled, disabled);
        SetState(element, ElementStates.Checked, element.Attributes.TryGetValue("checked", out var isChecked) && isChecked is true);
        if (disabled && Focused == element) Focus(null, visible: false);
    }

    private static bool IsWithin(ElementNode? node, ElementNode ancestor) => Ancestors(node).Contains(ancestor);

    /// <summary>Requests dismissal of an interactive overlay through its ordinary dismissal event.</summary>
    public bool Dismiss(ElementNode overlay)
    {
        if (!CanInteract(overlay) || !Scene.IsOverlay(overlay)) return false;
        Dispatch(overlay,"ondismiss",EventArgs.Empty); return true;
    }

    /// <summary>Gets whether user interaction can reach this element in the active modal scope.</summary>
    public bool CanInteract(ElementNode element) => !element.IsRemoved && !element.IsDisabled && scene.IsRendered(element) && InModalScope(element);

    private bool ModalIsExiting => activeModal is { IsRemoved: false } modal && Ancestors(modal).Any(element => element.Attributes.GetValueOrDefault("exiting") is true);

    private bool InModalScope(ElementNode element) => !ModalIsExiting && (activeModal is not { IsRemoved:false } modal || IsWithin(element,modal) || Ancestors(element).Any(parent=>overlayModalScopes.GetValueOrDefault(parent)==modal && Scene.IsOverlay(parent)));

    private static bool IsDisabled(ElementNode element) => element.IsDisabled;

    private static bool IsActivatable(ElementNode element) => !element.IsRemoved && !IsDisabled(element) &&
        (element.Attributes.GetValueOrDefault("onclick") is IEventCallback { HasDelegate: true } ||
         element.Attributes.GetValueOrDefault("__default_onclick") is IEventCallback { HasDelegate: true } ||
         element.Attributes.GetValueOrDefault("role") is "button" or "link" && Ancestors(element).Skip(1).Any(parent=>parent.Attributes.GetValueOrDefault("activation-proxy") is true));

    private static int TabIndex(ElementNode element) => element.Attributes.GetValueOrDefault("tabindex") switch
    {
        int index => index,
        string text when int.TryParse(text, out var index) => index,
        _ => 0,
    };

    private bool IsFocusable(ElementNode element) =>
        !IsDisabled(element) && !element.IsRemoved && (element.Attributes.GetValueOrDefault("focusable") is true || element.Attributes.ContainsKey("tabindex")) &&
        scene.IsRendered(element);

    private void OnSceneUpdated()
    {
        var shown = scene.Overlays;
        observedOverlays.RemoveWhere(element => !shown.Contains(element));
        exitingOverlays.RemoveWhere(element => !shown.Contains(element));
        foreach (var overlay in shown)
        {
            if (overlay.Attributes.GetValueOrDefault("exiting") is true) exitingOverlays.Add(overlay);
            else
            {
                var added = observedOverlays.Add(overlay);
                var reopened = exitingOverlays.Remove(overlay);
                if (added || reopened) OverlayOpened(overlay);
            }
        }
        activeModal = shown.LastOrDefault(IsModal);
        RestoreFocusOfClosedOverlays();
        if (pendingFocusScroll is { } target)
        {
            pendingFocusScroll = null;
            if (Focused == target && !target.IsRemoved) BringIntoView(target);
        }
        activeModal = scene.Overlays.LastOrDefault(IsModal);
        if (hoverChain.FirstOrDefault() is { } hovered && !InModalScope(hovered)) UpdateHover(null, pointerInside: false);
        if (TooltipOwner is { } tooltipOwner && (!scene.IsRendered(tooltipOwner) || !HasTooltip(tooltipOwner) || !InModalScope(tooltipOwner) ||
            tooltipTarget == tooltipOwner && TooltipController(tooltipOwner)?.ContentElement is { } content && !Scene.IsOverlay(content))) HideTooltip();
        foreach (var overlay in overlayModalScopes.Keys.Where(element=>element.IsRemoved || !Scene.IsOverlay(element)).ToArray()) overlayModalScopes.Remove(overlay);
        foreach (var overlay in overlayOwners.Keys.Where(element=>element.IsRemoved || !Scene.IsOverlay(element)).ToArray()) overlayOwners.Remove(overlay);
        if (CapturedPointer is { } captured && !scene.IsRendered(captured)) PointerCanceled();
        if (draggedScrollBar is { } container && scene.GetScrollBar(container, draggedScrollBarHorizontal) is null) PointerCanceled();
        if (pressedScrollBarButton is { } buttonContainer &&
            (scene.GetScrollBar(buttonContainer, draggedScrollBarHorizontal) is not { } buttonBar || buttonBar.DecreaseButton.IsEmpty)) PointerCanceled();
        UpdateScrollBarHover(scrollBarPointer, scrollBarPointer is { } point ? scene.HitTest(point) : null);
        if (pressedByKey is { } pressed && !scene.IsRendered(pressed))
        {
            pressedByKey = null;
            SetState(pressed, ElementStates.Pressed, false);
        }
        if (Focused is { } focused && (!scene.IsRendered(focused) || focused.IsDisabled || ModalIsExiting)) Focus(null,visible:false);
        if (activeModal is { } modal && Focused is { } outside && !CanInteract(outside))
        {
            var first = Descendants(modal).FirstOrDefault(element=>IsFocusable(element) && TabIndex(element)>=0) ?? modal;
            Focus(first,visible:true);
        }
        QueueScrollChanges();
    }

    private static IEnumerable<ElementNode> Ancestors(ElementNode? element)
    {
        for (var current = element; current is not null; current = current.ParentElement) yield return current;
    }

    private static IEnumerable<ElementNode> Descendants(ElementNode element) =>
        new[] { element }.Concat(element.Children.OfType<ElementNode>().SelectMany(Descendants));

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; HideTooltip();
        PointerCanceled();
        committedText.Clear();
        foreach (var element in renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().SelectMany(Descendants))
            element.SetFeature<IElementInteraction>(null);
        renderer.BatchCompleted -= OnBatchCompleted;
        scene.Updated -= OnSceneUpdated;
    }

    private ElementNode? EventRoot => renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().FirstOrDefault();
    private ElementNode? KeyboardScrollTarget
    {
        get
        {
            var target = Focused ?? Hovered;
            if (activeModal is { IsRemoved: false } modal) return target is not null && InModalScope(target) ? target : modal;
            return target ?? (EventRoot is { Name: "Window" } window
                ? window.Children.OfType<ElementNode>().FirstOrDefault(element => element.Name == "WindowContent") : null);
        }
    }

    private static PointerEventArgs PointerArgs(Point point, PointerButton button, KeyModifiers modifiers) =>
        new() { X = point.X, Y = point.Y, Button = button, Modifiers = modifiers };

    private bool Dispatch(ElementNode? target, string name, object args)
    {
        if (target is null || target.IsRemoved || ((IsDisabled(target) || ModalIsExiting) && name is not ("onblur" or "onfocusout" or "onpointerleave" or "onlostpointercapture" or "onresize"))) return false;
        Observe(renderer.DispatchEventAsync(target, name, args, out var preventDefault));
        return preventDefault;
    }

    private void Observe(Task task)
    {
        task.ContinueWith(static t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
    }
}
