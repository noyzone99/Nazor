using System.Collections;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

internal sealed class PickerIntegerRange(int minimum, int maximum) : IReadOnlyList<int>
{
    public int Count => Math.Max(0, maximum - minimum + 1);
    public int this[int index] => index >= 0 && index < Count ? minimum + index : throw new ArgumentOutOfRangeException(nameof(index));
    public IEnumerator<int> GetEnumerator() { for (var i = 0; i < Count; i++) yield return this[i]; }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class SegmentedPickerColumn : Control
{
    public SegmentedPickerColumn() { }

    private const double RowHeight = 40;
    private readonly string ownId = UniqueId.Create("picker-column");
    private int selected;
    private bool focusPending;
    private double wheel;
    private (double Y, int Index)? drag;

    [Parameter] public string Name { get; set; } = "";
    [Parameter] public IReadOnlyList<int> Values { get; set; } = [];
    [Parameter] public int Value { get; set; }
    [Parameter] public Func<int, string> Format { get; set; } = value => value.ToString(System.Globalization.CultureInfo.CurrentCulture);
    [Parameter] public EventCallback<int> ValueChanged { get; set; }
    [Parameter] public EventCallback<string> OnFocused { get; set; }

    protected override string ElementName => "SegmentedPickerColumn";
    protected override int? ElementTabIndex => -1;

    protected override void OnParametersSet() => selected = Find(Value);

    private int Find(int value)
    {
        var low = 0;
        var high = Values.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (Values[middle] < value) low = middle + 1;
            else high = middle;
        }
        return Math.Clamp(low, 0, Math.Max(0, Values.Count - 1));
    }

    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "listbox");
        builder.AddAttribute(11, "column", Name);
        builder.AddAttribute(12, "focusable", true);
        builder.AddAttribute(13, "__internal_onkeydown#column", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(14, "__internal_onwheel#column", EventCallback.Factory.Create<WheelEventArgs>(this, WheelAsync));
        builder.AddAttribute(15, "__internal_onpointerdown#column", EventCallback.Factory.Create<PointerEventArgs>(this, DownAsync));
        builder.AddAttribute(16, "__internal_onpointermove#column", EventCallback.Factory.Create<PointerEventArgs>(this, MoveAsync));
        builder.AddAttribute(17, "__internal_onpointerup#column", EventCallback.Factory.Create<PointerEventArgs>(this, Up));
        builder.AddAttribute(18, "__internal_onfocusin#column", EventCallback.Factory.Create<FocusEventArgs>(this, () => OnFocused.InvokeAsync(Name)));
        builder.AddAttribute(19, "__internal_onlostpointercapture#column", EventCallback.Factory.Create<PointerEventArgs>(this, e => { if (e.OriginalTarget == Element.Element) drag = null; }));
    }

    protected override void AddContent(RenderTreeBuilder builder)
    {
        for (var slot = -4; slot <= 4; slot++)
        {
            var position = selected + slot;
            if (position < 0 || position >= Values.Count)
            {
                builder.OpenElement(1000, "SegmentedPickerSpacer");
                builder.SetKey(("space", slot));
                builder.AddAttribute(1001, "semantic-hidden", true);
                builder.CloseElement();
                continue;
            }
            var value = Values[position];
            builder.OpenElement(1010, "SegmentedPickerOption");
            builder.SetKey(value);
            builder.AddAttribute(1011, "id", ownId + "-" + value);
            builder.AddAttribute(1012, "role", "option");
            builder.AddAttribute(1013, "selected", position == selected);
            builder.AddAttribute(1014, "current", position == selected);
            builder.AddAttribute(1015, "focusable", true);
            builder.AddAttribute(1016, "tabindex", position == selected ? 0 : -1);
            builder.AddAttribute(1017, "position-in-set", position + 1);
            builder.AddAttribute(1018, "size-of-set", Values.Count);
            builder.AddAttribute(1019, "picker-value", value);
            builder.AddAttribute(1020, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ChooseAsync(position)));
            builder.AddContent(1021, Format(value));
            builder.CloseElement();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!focusPending) return;
        focusPending = false;
        if (Element.Element is { } root && ControlTree.Descendants(root).FirstOrDefault(e => e.Attributes.GetValueOrDefault("current") is true) is { } option)
            _ = new ElementReference(option).FocusAsync(preventScroll: true);
    }

    private Task ChooseAsync(int position)
    {
        if (Values.Count == 0) return Task.CompletedTask;
        position = Math.Clamp(position, 0, Values.Count - 1);
        focusPending = true;
        if (position == selected) { StateHasChanged(); return Task.CompletedTask; }
        selected = position;
        StateHasChanged();
        return ValueChanged.InvokeAsync(Values[position]);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None) return Task.CompletedTask;
        var position = e.Key switch
        {
            "ArrowUp" => selected - 1,
            "ArrowDown" => selected + 1,
            "PageUp" => selected - 5,
            "PageDown" => selected + 5,
            "Home" => 0,
            "End" => Values.Count - 1,
            _ => (int?)null,
        };
        if (position is not { } target) return Task.CompletedTask;
        e.Handled = true;
        return ChooseAsync(target);
    }

    private Task WheelAsync(WheelEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.DeltaY == 0 || !double.IsFinite(e.DeltaY)) return Task.CompletedTask;
        e.Handled = true;
        wheel = Math.Clamp(wheel + (e.DeltaUnit == ScrollDeltaUnit.Notches ? -e.DeltaY : -e.DeltaY / RowHeight), -Values.Count, Values.Count);
        var steps = (int)Math.Truncate(wheel);
        wheel -= steps;
        return steps == 0 ? Task.CompletedTask : ChooseAsync(selected + steps);
    }

    private async Task DownAsync(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary || e.IsSynthetic || e.Modifiers != KeyModifiers.None) return;
        for (var node = e.OriginalTarget; node is not null && node != Element.Element; node = node.ParentElement)
        {
            if (node.Attributes.GetValueOrDefault("picker-value") is not int value) continue;
            await ChooseAsync(Find(value));
            break;
        }
        if (Element.CapturePointer()) drag = (e.Y, selected);
    }

    private Task MoveAsync(PointerEventArgs e)
    {
        if (drag is not { } origin) return Task.CompletedTask;
        if (!Element.IsPointerCaptured) { drag = null; return Task.CompletedTask; }
        return ChooseAsync(origin.Index - (int)Math.Truncate((e.Y - origin.Y) / RowHeight));
    }

    private void Up(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary || drag is null) return;
        drag = null;
        if (Element.IsPointerCaptured) Element.ReleasePointerCapture();
    }
}
