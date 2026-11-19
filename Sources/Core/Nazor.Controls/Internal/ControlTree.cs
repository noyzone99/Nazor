using System.Globalization;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

internal static class ControlTree
{
    internal static IEnumerable<ElementNode> Ancestors(ElementNode? element)
    {
        for (var node = element; node is not null; node = node.ParentElement) yield return node;
    }

    internal static IEnumerable<ElementNode> Descendants(ElementNode element)
    {
        foreach (var child in element.Children.OfType<ElementNode>())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    internal static ElementNode? Find(ElementNode element, string id) =>
        element.Root?.Nodes.OfType<ElementNode>().SelectMany(node => Descendants(node).Prepend(node))
            .FirstOrDefault(node => Equals(node.Attributes.GetValueOrDefault("id"), id));

    internal static bool IsFieldControl(ElementNode element) =>
        element.Attributes.GetValueOrDefault("role") is "textbox" or "checkbox" or "switch" or "radiogroup" or "combobox" or "slider"
            or "listbox" or "tree" or "togglebutton" or "spinbutton";

    internal static bool IsFocusable(ElementNode element) =>
        !element.IsDisabled && (element.Attributes.GetValueOrDefault("focusable") is true || element.Attributes.GetValueOrDefault("tabindex") is int);

    internal static bool IsFieldFramePress(PointerEventArgs e, ElementReference frame) =>
        e.Button == PointerButton.Primary && !Ancestors(e.OriginalTarget).TakeWhile(node => node != frame.Element).Any(IsFocusable);

    internal static string? Text(ElementNode element)
    {
        if (element.Attributes.GetValueOrDefault("accessible-name") is string { Length: > 0 } name) return name;
        var text = new System.Text.StringBuilder();
        Append(element, text);
        return text.Length == 0 ? null : text.ToString().Trim();
    }

    private static void Append(Node node, System.Text.StringBuilder text)
    {
        if (node is TextNode leaf) text.Append(leaf.Text);
        else if (node is ElementNode element)
            foreach (var child in element.Children) Append(child, text);
    }
}

internal static class Css
{
    internal static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    internal static string Px(double value) => Number(value) + "px";

    internal static string? Join(string? own, string? style) =>
        string.IsNullOrWhiteSpace(own) ? style : string.IsNullOrWhiteSpace(style) ? own : own + "; " + style;
}

internal static class UniqueId
{
    private static long next;

    internal static string Create(string prefix) => "nz-" + prefix + "-" + Interlocked.Increment(ref next).ToString(CultureInfo.InvariantCulture);
}

internal static class RangeMath
{
    internal static void Validate(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (!double.IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (!double.IsFinite(maximum) || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
    }

    internal static double Fraction(double value, double minimum, double maximum) =>
        maximum > minimum ? Math.Clamp((value - minimum) / (maximum - minimum), 0, 1) : 0;

    internal static void AddRange(RenderTreeBuilder builder, int sequence, double value, double minimum, double maximum)
    {
        builder.AddAttribute(sequence, "range-min", minimum);
        builder.AddAttribute(sequence + 1, "range-max", Math.Max(minimum, maximum));
        builder.AddAttribute(sequence + 2, "range-value", Math.Clamp(value, minimum, Math.Max(minimum, maximum)));
    }
}

internal static class Events
{
    internal static async Task RaiseChangeAsync(ElementReference reference, object? value, bool commit = true)
    {
        if (reference.Element is not { IsRemoved: false, Root: { } root } element) return;
        await root.Renderer.DispatchEventAsync(element, "oninput", new ChangeEventArgs { Value = value });
        if (commit) await root.Renderer.DispatchEventAsync(element, "onchange", new ChangeEventArgs { Value = value });
    }
}
