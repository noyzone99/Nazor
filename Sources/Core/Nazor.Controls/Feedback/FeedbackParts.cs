using System.Globalization;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

internal static class FeedbackParts
{
    internal static string SeverityText(Severity severity) => severity switch
    {
        Severity.Success => ControlStrings.Get("Success"),
        Severity.Warning => ControlStrings.Get("Warning"),
        Severity.Error => ControlStrings.Get("Error"),
        _ => ControlStrings.Get("Information"),
    };

    internal static string Politeness(Severity severity) => severity == Severity.Error ? "assertive" : "polite";

    internal static void Validate(Severity severity, string name)
    {
        if (!Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(name);
    }

    internal static void Mark(RenderTreeBuilder builder, int sequence, string name, Severity severity)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "severity", Vocabulary.Name(severity));
        builder.AddAttribute(sequence + 2, "role", "image");
        builder.AddAttribute(sequence + 3, "accessible-name", SeverityText(severity));
        builder.AddAttribute(sequence + 4, "glyph", severity == Severity.Success ? "check" : null);
        if (severity != Severity.Success) builder.AddContent(sequence + 5, severity switch { Severity.Warning => "!", Severity.Error => "×", _ => "i" });
        builder.CloseElement();
    }

    internal static void Button(RenderTreeBuilder builder, int sequence, string name, string? text, string? accessibleName, object receiver, Func<Task> action)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "focusable", true);
        builder.AddAttribute(sequence + 3, "accessible-name", accessibleName);
        builder.AddAttribute(sequence + 4, "onclick", EventCallback.Factory.Create<PointerEventArgs>(receiver, action));
        builder.AddContent(sequence + 5, text);
        builder.CloseElement();
    }

    internal static void CloseButton(RenderTreeBuilder builder, int sequence, string name, object receiver, Func<Task> action) =>
        Button(builder, sequence, name, "×", ControlStrings.Get("Close"), receiver, action);

    internal static string Percent(double fraction) => fraction.ToString("P0", CultureInfo.CurrentCulture);

    internal static void ValidateRange(double? value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum must be finite.");
        if (!double.IsFinite(maximum) || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum), "The maximum must be finite and not below the minimum.");
        if (value is { } v && !double.IsFinite(v)) throw new ArgumentOutOfRangeException(name, "The value must be finite.");
    }

    internal static int Filled(double fraction, int segments) => (int)Math.Floor(fraction * segments + 1e-9);
}
