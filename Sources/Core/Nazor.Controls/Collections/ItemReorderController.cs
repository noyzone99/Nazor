using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>A request to move one item within its current collection. The application applies the move and supplies updated items.</summary>
/// <typeparam name="TItem">The collection's item type.</typeparam>
/// <param name="Item">The item to move.</param>
/// <param name="OldIndex">Its index before removal.</param>
/// <param name="NewIndex">Its final index after removal and insertion.</param>
public readonly record struct ItemReorderRequest<TItem>(TItem Item, int OldIndex, int NewIndex);

internal sealed class ItemReorderController(
    Func<ElementReference> owner,
    string rowName,
    Func<int, object?> keyAt,
    Func<int, int, bool> canMove,
    Func<int, int, Task> commit,
    Action changed)
{
    private Gesture? gesture;
    private bool suppressPointerActivation;

    public bool Horizontal { get; set; }
    public bool Enabled { get; set; }
    public Func<int, int, bool, int>? Destination { get; set; }
    public Func<(double X, double Y, double Width, double Height)?>? DropBounds { get; set; }
    public Func<int, double, double, Task>? DropOutside { get; set; }
    public bool IsDragging => gesture?.Moved == true;
    public int Source => IsDragging ? gesture!.Source : -1;
    public int Target => IsDragging ? gesture!.Target : -1;
    public bool After => gesture?.After == true;
    public bool SuppressPointerActivation => suppressPointerActivation;
    public bool SuppressClick(PointerEventArgs e) => !e.IsSynthetic && suppressPointerActivation;

    public void Synchronize()
    {
        if (gesture is { } active && (!Enabled || !Equals(keyAt(active.Source), active.Key) || !canMove(active.Source, active.Source))) Cancel();
    }

    public void Down(int index, PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary) return;
        Cancel();
        suppressPointerActivation = false;
        if (!Enabled || e.IsSynthetic || e.Modifiers != KeyModifiers.None || !double.IsFinite(e.X) || !double.IsFinite(e.Y) ||
            keyAt(index) is not { } key || !canMove(index, index)) return;
        var row = FindRow(e.OriginalTarget);
        if (row is null || row.IsDisabled || !OwnRow(row)) return;
        for (var node = e.OriginalTarget; node is not null && node != row; node = node.ParentElement)
            if (node.Attributes.GetValueOrDefault("focusable") is true || node.Attributes.GetValueOrDefault("editable") is true ||
                node.Name is "TreeViewExpander" or "TreeViewItemCheck" or "ListViewItemCheck" or "GridViewItemCheck") return;
        var capture = new ElementReference(row);
        if (capture.CapturePointer()) gesture = new(index, key, e.X, e.Y, capture);
    }

    public void Move(PointerEventArgs e)
    {
        if (gesture is not { } active) return;
        if (!Enabled || !active.Capture.IsPointerCaptured || !Equals(keyAt(active.Source), active.Key) || !canMove(active.Source, active.Source)) { Cancel(); return; }
        if (!double.IsFinite(e.X) || !double.IsFinite(e.Y)) return;
        var dx = e.X - active.X;
        var dy = e.Y - active.Y;
        if (!active.Moved && Math.Max(Math.Abs(dx), Math.Abs(dy)) < 6) return;
        active.Moved = true;
        suppressPointerActivation = true;
        var previous = (active.Target, active.After);
        active.Target = -1;
        if (owner().Element is { } root)
        {
            foreach (var row in ControlTree.Descendants(root).Where(node => node.Name == rowName && !node.IsDisabled && OwnRow(node)))
            {
                if (row.Attributes.GetValueOrDefault("index") is not int index || !canMove(active.Source, index) ||
                    new ElementReference(row).GetBounds() is not { } box ||
                    e.X < box.X || e.X > box.X + box.Width || e.Y < box.Y || e.Y > box.Y + box.Height) continue;
                active.Target = index;
                active.After = Horizontal ? e.X >= box.X + box.Width / 2 : e.Y >= box.Y + box.Height / 2;
                break;
            }
        }
        if (previous != (active.Target, active.After) || !active.Reported)
        {
            active.Reported = true;
            changed();
        }
    }

    public async Task UpAsync(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary || gesture is not { } active) return;
        if (!active.Capture.IsPointerCaptured) { Cancel(); return; }
        if (!double.IsFinite(e.X) || !double.IsFinite(e.Y)) { Cancel(); return; }
        Move(e);
        if (gesture is null) return;
        var target = active.Target;
        var next = target + (active.After ? 1 : 0);
        if (next > active.Source) next--;
        if (target >= 0 && Destination is { } destination) next = destination(active.Source, target, active.After);
        var apply = active.Moved && target >= 0 && next != active.Source && Enabled &&
            Equals(keyAt(active.Source), active.Key) && canMove(active.Source, target) && canMove(active.Source, next);
        var outside = active.Moved && Enabled && Equals(keyAt(active.Source), active.Key) && canMove(active.Source, active.Source) &&
            DropOutside is not null && DropBounds?.Invoke() is { Width: > 0, Height: > 0 } bounds &&
            double.IsFinite(bounds.X) && double.IsFinite(bounds.Y) && double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height) &&
            (e.X < bounds.X || e.X > bounds.X + bounds.Width || e.Y < bounds.Y || e.Y > bounds.Y + bounds.Height);
        var dropOutside = DropOutside;
        Cancel();
        if (outside) await dropOutside!(active.Source, e.X, e.Y);
        else if (apply) await commit(active.Source, next);
    }

    public bool CancelFromKey(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key != "Escape" || gesture is null) return false;
        e.Handled = true;
        Cancel();
        return true;
    }

    public async Task<bool> KeyAsync(KeyboardEventArgs e, int index, int count, int step = 1)
    {
        if (CancelFromKey(e)) return true;
        if (!Enabled || e.Handled || index < 0 || index >= count ||
            e.Modifiers != (KeyModifiers.Control | KeyModifiers.Shift) && e.Modifiers != (KeyModifiers.Meta | KeyModifiers.Shift)) return false;
        if (owner().Element is { } root && ControlTree.Descendants(root).FirstOrDefault(node => node.States.HasFlag(ElementStates.Focus)) is { } focus &&
            (focus.Name != rowName || !OwnRow(focus))) return false;
        var delta = e.Key switch
        {
            "ArrowUp" => -step,
            "ArrowDown" => step,
            "ArrowLeft" when Horizontal => -1,
            "ArrowRight" when Horizontal => 1,
            _ => 0,
        };
        if (delta == 0) return false;
        e.Handled = true;
        var next = Math.Clamp(index + delta, 0, count - 1);
        if (next != index && canMove(index, next)) await commit(index, next);
        return true;
    }

    public void Lost(PointerEventArgs e)
    {
        if (gesture is { } active && e.OriginalTarget == active.Capture.Element) Cancel();
    }

    public void Cancel()
    {
        if (gesture is not { } active) return;
        gesture = null;
        if (active.Capture.IsPointerCaptured) active.Capture.ReleasePointerCapture();
        changed();
    }

    private ElementNode? FindRow(ElementNode? target)
    {
        for (var node = target; node is not null && node != owner().Element; node = node.ParentElement)
            if (node.Name == rowName) return node;
        return null;
    }

    private bool OwnRow(ElementNode row)
    {
        var root = owner().Element;
        if (root is null) return false;
        for (var node = row.ParentElement; node is not null; node = node.ParentElement)
            if (node.Name == root.Name) return ReferenceEquals(node, root);
        return false;
    }

    private sealed class Gesture(int source, object key, double x, double y, ElementReference capture)
    {
        public int Source { get; } = source;
        public object Key { get; } = key;
        public double X { get; } = x;
        public double Y { get; } = y;
        public ElementReference Capture { get; } = capture;
        public int Target { get; set; } = -1;
        public bool After { get; set; }
        public bool Moved { get; set; }
        public bool Reported { get; set; }
    }
}
