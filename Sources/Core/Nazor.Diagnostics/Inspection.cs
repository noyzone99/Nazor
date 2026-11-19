using System.Globalization;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Diagnostics;

/// <summary>Inspects a live window's component tree, visual state, and input selection.</summary>
public sealed partial class DevToolsSession
{
    private readonly HashSet<PointerButton> inspectButtons = [];
    private TextLayout? labelLayout;
    private int labelFontVersion;
    private bool escapePressed;

    /// <summary>Gets whether pointer-based diagnostic inspection is active.</summary>
    public bool IsInspecting { get; private set; }

    /// <summary>Gets the highlighted node's diagnostic identifier, or null when no node is highlighted.</summary>
    public int? HighlightedId { get; private set; }

    /// <summary>Gets the selected node's diagnostic identifier, or null when no node is selected.</summary>
    public int? SelectedId { get; private set; }

    /// <summary>The host requests one frame when the highlight or inspection mode changes.</summary>
    public event Action? OverlayChanged;

    /// <summary>Occurs when selection changes.</summary>
    public event Action<int?>? SelectionChanged;

    /// <summary>
    /// Highlights the node with the specified diagnostic identifier, or clears the highlight for null. A component
    /// highlights the boxes of its top-level rendered elements; a text has no box and clears the highlight.
    /// </summary>
    /// <param name="id">The diagnostic identifier of the node, or null to clear the current target.</param>
    public void Highlight(int? id)
    {
        Renderer.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        id = id is { } value && NodeOf(value) is ElementNode or ComponentNode ? value : null;
        if (HighlightedId == id) return;
        HighlightedId = id;
        OverlayChanged?.Invoke();
    }

    /// <summary>
    /// Selects the node with the specified diagnostic identifier, or clears the selection for null.
    /// </summary>
    /// <param name="id">The diagnostic identifier of the node, or null to clear the current target.</param>
    public void Select(int? id)
    {
        Renderer.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        id = id is { } value && NodeOf(value) is not null ? value : null;
        Highlight(id);
        if (SelectedId == id) return;
        SelectedId = id;
        SelectionChanged?.Invoke(id);
    }

    /// <summary>Enables or disables pointer-based element inspection.</summary>
    /// <param name="enabled">Whether the feature or interaction is enabled.</param>
    public void SetInspectMode(bool enabled)
    {
        Renderer.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsInspecting == enabled) return;

        if (enabled) Input.PointerCanceled(clearHover: false);
        IsInspecting = enabled;
        if (!enabled) Highlight(null);
        OverlayChanged?.Invoke();
    }

    /// <summary>True when inspection consumed this input; the application must not receive it.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool PointerMoved(Point point)
    {
        if (!IsInspecting) return false;
        Scene.Update();
        Highlight(Scene.HitTest(point) is { } element ? IdOf(element) : null);
        return true;
    }

    /// <summary>Processes the pointer pressed notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool PointerPressed(Point point, PointerButton button)
    {
        if (!IsInspecting) return false;
        inspectButtons.Add(button);
        PointerMoved(point);
        return true;
    }

    /// <summary>Processes the pointer released notification in logical viewport coordinates.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <param name="button">The pointer button associated with the event.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool PointerReleased(Point point, PointerButton button)
    {

        var pressed = inspectButtons.Remove(button);
        if (!IsInspecting) return pressed;
        PointerMoved(point);
        if (button == PointerButton.Primary && pressed)
        {
            IsInspecting = false;
            Select(HighlightedId);
            OverlayChanged?.Invoke();
        }
        return true;
    }

    /// <summary>Processes the pointer left notification in logical viewport coordinates.</summary>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool PointerLeft()
    {
        if (!IsInspecting) return false;
        Highlight(null);
        return true;
    }

    /// <summary>Leaves inspect mode and clears the highlighted element.</summary>
    public void CancelInspection()
    {
        inspectButtons.Clear();
        SetInspectMode(false);
    }

    /// <summary>Processes an inspector key press and reports whether the inspector consumed it.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool KeyDown(string key)
    {
        if (!IsInspecting || key != "Escape") return false;
        escapePressed = true;
        SetInspectMode(false);
        return true;
    }

    /// <summary>Processes a released key and reports whether it was handled.</summary>
    /// <param name="key">The normalized name of the key.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool KeyUp(string key)
    {
        if (!escapePressed || key != "Escape") return false;
        escapePressed = false;
        return true;
    }

    /// <summary>
    /// Adds the box model above the application's drawings, after the scene has been updated. Its commands
    /// participate in damage tracking so moving or removing it restores the old area.
    /// </summary>
    /// <param name="list">The list (DisplayList).</param>
    /// <returns>The resulting DisplayList value.</returns>
    public DisplayList Compose(DisplayList list)
    {
        if (disposed || HighlightedId is not { } id || ReadOverlay(id) is not { } overlay) return list;
        var commands = new List<DrawCommand>(list.Commands);
        commands.Add(new PushClip(new Rect(0, 0, list.Size.Width, list.Size.Height), 0));
        foreach (var box in overlay.Boxes)
        {
            Ring(box.MarginBox, box.BorderBox, new Color(0xF6, 0xB2, 0x6B, 0xA8));
            Ring(box.BorderBox, box.PaddingBox, new Color(0xFF, 0xE5, 0x99, 0xA8));
            Ring(box.PaddingBox, box.ContentBox, new Color(0x93, 0xC4, 0x7D, 0x8C));
            commands.Add(new FillRectangle(box.ContentBox, 0, new Color(0x6F, 0xA8, 0xDC, 0xA8)));
        }
        var bounds = overlay.Bounds;
        var scale = overlay.Boxes[0].Scale;
        var guide = new Color(0x6F, 0xA8, 0xDC, 0xA8);
        foreach (var x in new[] { bounds.X, bounds.Right })
            commands.Add(new DrawPolyline([new Point(x, 0), new Point(x, list.Size.Height)], 1 / scale, guide));
        foreach (var y in new[] { bounds.Y, bounds.Bottom })
            commands.Add(new DrawPolyline([new Point(0, y), new Point(list.Size.Width, y)], 1 / scale, guide));

        var label = string.Create(CultureInfo.InvariantCulture, $"{overlay.Name}  {bounds.Width:0.##} × {bounds.Height:0.##}");
        if (labelLayout?.Text != label || labelFontVersion != Scene.Fonts.Version)
        {
            labelLayout = new TextLayout(label, new TextStyle([], 12), null, Scene.Fonts, Scene.TextShaper, wrap: false);
            labelFontVersion = Scene.Fonts.Version;
        }
        var size = labelLayout.Size;
        var left = Math.Clamp(bounds.X, 0, Math.Max(0, list.Size.Width - size.Width - 12));
        var top = bounds.Y >= size.Height + 12 ? bounds.Y - size.Height - 12 : bounds.Bottom + 4;
        top = Math.Clamp(top, 0, Math.Max(0, list.Size.Height - size.Height - 8));
        commands.Add(new FillRectangle(new Rect(left, top, size.Width + 12, size.Height + 8), 3, new Color(32, 32, 32, 255)));
        commands.Add(new DrawText(labelLayout, new Point(left + 6, top + 4), Color.White));
        commands.Add(Pop.Instance);
        return new DisplayList(list.Size, commands);

        void Ring(Rect outer, Rect inner, Color color)
        {

            var widths = new Thickness(Math.Max(0, inner.X - outer.X), Math.Max(0, inner.Y - outer.Y),
                Math.Max(0, outer.Right - inner.Right), Math.Max(0, outer.Bottom - inner.Bottom));
            commands.Add(new DrawBorder(outer, 0, widths, color));
        }
    }

    private const int MaxOverlayBoxes = 64;

    private sealed record Overlay(string Name, List<BoxModel> Boxes, Rect Bounds);

    // An element draws its own box. A component has no box of its own: it draws the boxes of the first elements met
    // on each logical branch below it (its outputs), labelled with the component's type and their combined bounds.
    private Overlay? ReadOverlay(int id)
    {
        if (ElementOf(id) is { } element)
        {
            if (ReadBox(element) is not { } box) return null;
            var name = element.Name + (element.Id is { Length: > 0 } attributeId ? "#" + attributeId : "") +
                (element.Class is { Length: > 0 } classes ? "." + string.Join('.', classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) : "");
            return new(name, [box], box.BorderBox);
        }
        if (NodeOf(id) is not ComponentNode component || component.LogicalChildren is not { } roots) return null;
        var boxes = new List<BoxModel>();
        var pending = new Stack<Node>();
        for (var i = roots.Count - 1; i >= 0; i--) pending.Push(roots[i]);
        while (boxes.Count < MaxOverlayBoxes && pending.TryPop(out var node))
        {
            if (node.IsRemoved) continue;
            if (node is ElementNode output)
            {
                if (ReadBox(output) is { } box) boxes.Add(box);
            }
            else if (node.LogicalChildren is { } children)
                for (var i = children.Count - 1; i >= 0; i--) pending.Push(children[i]);
        }
        if (boxes.Count == 0) return null;
        var (left, top) = (boxes.Min(b => b.BorderBox.X), boxes.Min(b => b.BorderBox.Y));
        var bounds = new Rect(left, top, boxes.Max(b => b.BorderBox.Right) - left, boxes.Max(b => b.BorderBox.Bottom) - top);
        return new(TypeName(component.Component.GetType()), boxes, bounds);
    }
}
