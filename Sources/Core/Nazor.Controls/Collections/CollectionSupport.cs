using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

internal sealed class ItemSelection<TItem>
{
    private readonly ControllableState<TItem?> single = new(default);
    private readonly ControllableState<IReadOnlyCollection<TItem>?> multiple = new(null);

    public SelectionModel<object> Model { get; } = new(SelectionMode.Single);

    public Func<TItem, object> Key { get; set; } = item => item!;

    public void Sync(SelectionMode mode, TItem? item, IReadOnlyCollection<TItem>? items)
    {
        if (Model.Mode != mode) Model.Mode = mode;
        if (mode == SelectionMode.Multiple)
        {
            if (multiple.Sync(items)) Model.Replace(items?.Select(Key) ?? []);
        }
        else if (single.Sync(item) || mode == SelectionMode.None) Model.Replace(item is null || mode == SelectionMode.None ? [] : [Key(item)]);
    }

    public bool IsSelected(TItem item) => Model.Count > 0 && Model.IsSelected(Key(item));

    public async Task NotifyAsync(IEnumerable<TItem> ordered, EventCallback<TItem?> itemChanged, EventCallback<IReadOnlyCollection<TItem>> itemsChanged, ElementReference element)
    {
        if (Model.Mode == SelectionMode.Multiple)
        {
            TItem[] list = Model.Count == 0 ? [] : [.. ordered.Where(IsSelected)];
            multiple.Set(list);
            await itemsChanged.InvokeAsync(list);
            await Events.RaiseChangeAsync(element, list);
            return;
        }
        var chosen = Model.Count == 0 ? default : ordered.FirstOrDefault(IsSelected);
        if (!single.Set(chosen)) return;
        await itemChanged.InvokeAsync(chosen);
        await Events.RaiseChangeAsync(element, chosen);
    }
}

internal sealed class RowFocus
{
    public int Pending { get; set; } = -1;

    public bool RootQuietly { get; set; }

    public bool Quiet { get; private set; }

    public async Task ApplyAsync(ElementReference root, string rowName)
    {
        if (root.Element is not { IsRemoved: false } element) return;
        if (Pending >= 0)
        {
            var target = Pending;
            var row = ControlTree.Descendants(element).FirstOrDefault(node => node.Name == rowName && node.Attributes.GetValueOrDefault("index") is int index && index == target);
            if (row is null) return;
            Pending = -1;
            RootQuietly = false;
            if ((row.States & ElementStates.Focus) == 0) await new ElementReference(row).FocusAsync(true);
            return;
        }
        if (!RootQuietly) return;
        RootQuietly = false;
        Quiet = true;
        try { await root.FocusAsync(true); }
        finally { Quiet = false; }
    }

    public static bool HasFocusWithin(ElementReference root) =>
        root.Element is { } element && (element.States & (ElementStates.Focus | ElementStates.FocusWithin)) != 0;

    public static bool HasOwnFocus(ElementReference root) =>
        root.Element is { } element && (element.States & ElementStates.Focus) != 0;
}

internal static class CollectionKeys
{
    public static bool Control(KeyboardEventArgs e) => (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;

    public static bool Shift(KeyboardEventArgs e) => (e.Modifiers & KeyModifiers.Shift) != 0;

    public static bool Control(PointerEventArgs e) => (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;

    public static bool Shift(PointerEventArgs e) => !e.IsSynthetic && (e.Modifiers & KeyModifiers.Shift) != 0;

    public static KeyboardEventArgs WithoutControl(KeyboardEventArgs e) =>
        Control(e) && (e.Key.StartsWith("Arrow", StringComparison.Ordinal) || e.Key is "PageUp" or "PageDown")
            ? new KeyboardEventArgs { Key = e.Key, Modifiers = e.Modifiers & ~(KeyModifiers.Control | KeyModifiers.Meta), Repeat = e.Repeat }
            : e;
}
