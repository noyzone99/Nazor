using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Nazor.Accessibility;

namespace Nazor.Platform.Windows.Automation;

[GeneratedComClass]
internal sealed partial class TextRangeProvider : ITextRangeProvider
{
    private readonly AutomationBridge bridge;
    private readonly SemanticNode node;
    private int start, end;

    public TextRangeProvider(AutomationBridge bridge, SemanticNode node, int start, int end)
    {
        this.bridge = bridge;
        this.node = node;
        (this.start, this.end) = (Math.Min(start, end), Math.Max(start, end));
    }

    private string Text => bridge.Tree.TextOf(node) ?? "";

    public ITextRangeProvider Clone() => Ui(() => (ITextRangeProvider)new TextRangeProvider(bridge, node, start, end));

    public bool Compare(ITextRangeProvider range) => Ui(() => range is TextRangeProvider other && other.node == node && other.Clamped() is var o && (o.start, o.end) == (start, end));

    public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint) =>
        Ui(() => Endpoint(endpoint) - Other(targetRange).Endpoint(targetEndpoint));

    public void ExpandToEnclosingUnit(TextUnit unit) => Ui(() => (start, end) = TextUnits.Enclosing(Boundaries(unit), start));

    public ITextRangeProvider? FindAttribute(int attributeId, Variant value, bool backward) => Ui(() =>
        attributeId == Uia.IsReadOnlyAttribute && value.ToObject() is false && start < end ? (ITextRangeProvider)new TextRangeProvider(bridge, node, start, end) : null);

    public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase) => Ui(() =>
    {
        if (string.IsNullOrEmpty(text)) return null;
        var span = Text[start..end];
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var index = backward ? span.LastIndexOf(text, comparison) : span.IndexOf(text, comparison);
        return index < 0 ? null : (ITextRangeProvider)new TextRangeProvider(bridge, node, start + index, start + index + text.Length);
    });

    public Variant GetAttributeValue(int attributeId) => Ui(() =>
    {
        if (attributeId == Uia.IsReadOnlyAttribute) return Variant.FromObject(false);
        Marshal.ThrowExceptionForHR(Uia.UiaGetReservedNotSupportedValue(out var notSupported));
        return new Variant { Type = Variant.VT_UNKNOWN, Pointer = notSupported };
    });

    public nint GetBoundingRectangles() => Ui(() => Uia.CreateRectangleArray([.. bridge.Tree.TextBounds(node, start, end).Select(bridge.ToScreen)]));

    public IRawElementProviderSimple GetEnclosingElement() => Ui(() => (IRawElementProviderSimple)bridge.Wrap(node));

    public string GetText(int maxLength) => Ui(() =>
    {
        var text = Text[start..end];
        return maxLength >= 0 && text.Length > maxLength ? text[..maxLength] : text;
    });

    public int Move(TextUnit unit, int count) => Ui(() => TextUnits.MoveRange(Boundaries(unit), ref start, ref end, count));

    public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count) => Ui(() =>
    {
        var boundaries = Boundaries(unit);
        if (endpoint == TextPatternRangeEndpoint.End)
        {
            var moved = TextUnits.Move(boundaries, ref end, count);
            start = Math.Min(start, end);
            return moved;
        }
        var steps = TextUnits.Move(boundaries, ref start, count);
        end = Math.Max(start, end);
        return steps;
    });

    public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint) => Ui(() =>
    {
        var position = Other(targetRange).Endpoint(targetEndpoint);
        if (endpoint == TextPatternRangeEndpoint.Start) (start, end) = (position, Math.Max(position, end));
        else (start, end) = (Math.Min(start, position), position);
    });

    public void Select() => Ui(() =>
    {
        if (!bridge.Tree.SelectText(node, start, end)) throw new COMException("The text cannot be selected.", Uia.E_INVALIDOPERATION);
    });

    public void AddToSelection() => throw new COMException("A text box has one selection.", Uia.E_INVALIDOPERATION);

    public void RemoveFromSelection() => throw new COMException("A text box has one selection.", Uia.E_INVALIDOPERATION);

    public void ScrollIntoView(bool alignToTop) => Ui(() => bridge.Tree.ScrollIntoView(node));

    public nint GetChildren() => Ui(() => Uia.CreateUnknownArray<IRawElementProviderSimple>([]));

    private int[] Boundaries(TextUnit unit) => TextUnits.Boundaries(Text, unit, unit == TextUnit.Line ? bridge.Tree.TextLines(node) : []);

    private int Endpoint(TextPatternRangeEndpoint endpoint) => endpoint == TextPatternRangeEndpoint.Start ? start : end;

    private TextRangeProvider Other(ITextRangeProvider range) =>
        range is TextRangeProvider other && other.node == node ? other.Clamped() : throw new ArgumentException("The range belongs to another text.", nameof(range));

    private TextRangeProvider Clamped()
    {
        var length = Text.Length;
        start = Math.Clamp(start, 0, length);
        end = Math.Clamp(end, start, length);
        return this;
    }

    private T Ui<T>(Func<T> call) => bridge.OnUiThread(() =>
    {
        if (!node.IsDetached) bridge.Tree.EnsureCurrent();
        if (node.IsDetached) throw AutomationBridge.ElementNotAvailable();
        Clamped();
        return call();
    });

    private void Ui(Action call) => Ui(() =>
    {
        call();
        return true;
    });
}
