using System.Collections.Frozen;

namespace Nazor.Style;

public sealed partial record SystemColors
{
    /// <summary>Completes a partial backend palette for the selected desktop and appearance. Supplied colors win; missing accent shades and semantic roles are derived before falling back. Names are case-insensitive. The input is copied.</summary>
    /// <param name="colors">CSS system colors, accent shades and additional PascalCase semantic roles; null uses profile defaults.</param>
    /// <param name="scheme">The effective light or dark appearance.</param>
    /// <param name="profile">The fallback desktop palette.</param>
    /// <returns>A complete palette independent of the input dictionary.</returns>
    public static SystemColors Resolve(IReadOnlyDictionary<string, Color>? colors = null, ColorScheme scheme = ColorScheme.Light,
        SystemColorProfile profile = SystemColorProfile.Generic)
    {
        var supplied = colors is null ? new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, Color>(colors, StringComparer.OrdinalIgnoreCase);
        var d = Defaults(profile, scheme);
        Color Get(string name, Color fallback) => supplied.GetValueOrDefault(name, fallback);
        Color Text(string name, string background, Color fallback) => Get(name,
            supplied.TryGetValue(background, out var value) ? TextOn(value) : fallback);
        var accent = Get("AccentColor", d.AccentColor);
        var customAccent = supplied.ContainsKey("AccentColor");
        var highlight = Get("Highlight", accent);
        var highlightText = Get("HighlightText", TextOn(highlight));
        var selected = Get("SelectedItem", highlight);
        var resolved = d with
        {
            Canvas = Get("Canvas", d.Canvas), CanvasText = Text("CanvasText", "Canvas", d.CanvasText),
            Field = Get("Field", d.Field), FieldText = Text("FieldText", "Field", d.FieldText),
            ButtonFace = Get("ButtonFace", d.ButtonFace), ButtonText = Text("ButtonText", "ButtonFace", d.ButtonText),
            ButtonBorder = Get("ButtonBorder", d.ButtonBorder), GrayText = Get("GrayText", d.GrayText),
            Highlight = highlight, HighlightText = highlightText,
            SelectedItem = selected, SelectedItemText = Get("SelectedItemText", supplied.ContainsKey("SelectedItem") ? TextOn(selected) : highlightText),
            LinkText = Get("LinkText", customAccent ? Mix(accent, scheme == ColorScheme.Dark ? Color.White : Color.Black, 0.25) : d.LinkText),
            VisitedText = Get("VisitedText", d.VisitedText), ActiveText = Get("ActiveText", accent),
            Mark = Get("Mark", d.Mark), MarkText = Text("MarkText", "Mark", d.MarkText),
            AccentColor = accent, AccentColorText = Get("AccentColorText", TextOn(accent)),
            AccentLight1 = Get("AccentLight1", customAccent ? Mix(accent, Color.White, 0.2) : d.AccentLight1),
            AccentLight2 = Get("AccentLight2", customAccent ? Mix(accent, Color.White, 0.4) : d.AccentLight2),
            AccentLight3 = Get("AccentLight3", customAccent ? Mix(accent, Color.White, 0.6) : d.AccentLight3),
            AccentDark1 = Get("AccentDark1", customAccent ? Mix(accent, Color.Black, 0.2) : d.AccentDark1),
            AccentDark2 = Get("AccentDark2", customAccent ? Mix(accent, Color.Black, 0.4) : d.AccentDark2),
            AccentDark3 = Get("AccentDark3", customAccent ? Mix(accent, Color.Black, 0.6) : d.AccentDark3),
        };
        return resolved with { Palette = CompletePalette(resolved, supplied).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase) };
    }

    private static Color Rgb(uint rgb) => Color.FromRgba((rgb << 8) | 255);

    private static Color Mix(Color from, Color to, double amount) => new(
        (byte)Math.Round(from.R + (to.R - from.R) * amount),
        (byte)Math.Round(from.G + (to.G - from.G) * amount),
        (byte)Math.Round(from.B + (to.B - from.B) * amount),
        (byte)Math.Round(from.A + (to.A - from.A) * amount));

    private static SystemColors Defaults(SystemColorProfile profile, ColorScheme scheme)
    {
        var dark = scheme == ColorScheme.Dark;
        var (canvas, field, button, text, accent) = profile switch
        {
            SystemColorProfile.KDE => dark ? (0x232629u, 0x1b1e20u, 0x31363bu, 0xeff0f1u, 0x3daee9u)
                : (0xeff0f1u, 0xffffffu, 0xeff0f1u, 0x232629u, 0x3daee9u),
            SystemColorProfile.GNOME => dark ? (0x242424u, 0x1e1e1eu, 0x353535u, 0xffffffu, 0x3584e4u)
                : (0xfafafau, 0xffffffu, 0xebebebu, 0x2e3436u, 0x3584e4u),
            SystemColorProfile.MacOS => dark ? (0x1e1e1eu, 0x1e1e1eu, 0x3a3a3au, 0xffffffu, 0x0a84ffu)
                : (0xecececu, 0xffffffu, 0xffffffu, 0x1d1d1fu, 0x007affu),
            SystemColorProfile.IOS => dark ? (0x000000u, 0x1c1c1eu, 0x2c2c2eu, 0xffffffu, 0x0a84ffu)
                : (0xf2f2f7u, 0xffffffu, 0xffffffu, 0x000000u, 0x007affu),
            SystemColorProfile.Android => dark ? (0x141218u, 0x211f26u, 0x2b2930u, 0xe6e0e9u, 0xd0bcffu)
                : (0xfff7ffu, 0xffffffu, 0xf3edf7u, 0x1d1b20u, 0x6750a4u),
            _ => dark ? (0x202020u, 0x2c2c2cu, 0x2d2d2du, 0xffffffu, 0x60cdffu)
                : (0xf3f3f3u, 0xffffffu, 0xfbfbfbu, 0x1b1b1bu, 0x0078d4u),
        };
        var background = Rgb(canvas);
        var foreground = Rgb(text);
        var color = Rgb(accent);
        return Default with
        {
            Canvas = background, CanvasText = foreground, Field = Rgb(field), FieldText = foreground,
            ButtonFace = Rgb(button), ButtonText = foreground, ButtonBorder = Mix(background, foreground, dark ? 0.25 : 0.3),
            GrayText = Mix(background, foreground, 0.55),
            Highlight = color, HighlightText = TextOn(color), SelectedItem = color, SelectedItemText = TextOn(color),
            LinkText = dark ? Mix(color, Color.White, 0.2) : Mix(color, Color.Black, 0.25),
            VisitedText = dark ? Rgb(0xd0a9f5) : Rgb(0x7040a0), ActiveText = color,
            AccentColor = color, AccentColorText = TextOn(color),
            AccentLight1 = Mix(color, Color.White, 0.2), AccentLight2 = Mix(color, Color.White, 0.4), AccentLight3 = Mix(color, Color.White, 0.6),
            AccentDark1 = Mix(color, Color.Black, 0.2), AccentDark2 = Mix(color, Color.Black, 0.4), AccentDark3 = Mix(color, Color.Black, 0.6),
        };
    }

    private static Dictionary<string, Color> CompletePalette(SystemColors colors, IReadOnlyDictionary<string, Color> supplied)
    {
        var dark = TextOn(colors.Canvas) == Color.White;
        Color Get(string name, Color fallback) => supplied.GetValueOrDefault(name, fallback);
        Color Text(string name, string background, Color fallback) => Get(name,
            supplied.TryGetValue(background, out var value) ? TextOn(value) : fallback);
        var success = Get("Success", dark ? Rgb(0x6ccb5f) : Rgb(0x0f7b0f));
        var warning = Get("Warning", dark ? Rgb(0xfce100) : Rgb(0x9d5d00));
        var error = Get("Error", dark ? Rgb(0xff99a4) : Rgb(0xc42b1c));
        var result = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            ["CanvasAlternate"] = Get("CanvasAlternate", colors.Field),
            ["CanvasAlternateText"] = Text("CanvasAlternateText", "CanvasAlternate", colors.FieldText),
            ["CanvasTextSecondary"] = Get("CanvasTextSecondary", Mix(colors.Canvas, colors.CanvasText, 0.7)),
            ["ButtonHover"] = Get("ButtonHover", Mix(colors.ButtonFace, colors.ButtonText, 0.06)),
            ["ButtonHoverText"] = Text("ButtonHoverText", "ButtonHover", colors.ButtonText),
            ["ButtonPressed"] = Get("ButtonPressed", Mix(colors.ButtonFace, colors.ButtonText, 0.1)),
            ["ButtonPressedText"] = Text("ButtonPressedText", "ButtonPressed", colors.ButtonText),
            ["ButtonDisabled"] = Get("ButtonDisabled", Mix(colors.ButtonFace, colors.Canvas, 0.5)),
            ["ButtonDisabledText"] = Get("ButtonDisabledText", colors.GrayText),
            ["FieldHover"] = Get("FieldHover", Mix(colors.Field, colors.FieldText, 0.03)),
            ["FieldHoverText"] = Text("FieldHoverText", "FieldHover", colors.FieldText),
            ["FieldDisabled"] = Get("FieldDisabled", colors.Canvas),
            ["FieldDisabledText"] = Get("FieldDisabledText", colors.GrayText),
            ["Header"] = Get("Header", colors.Canvas), ["HeaderText"] = Text("HeaderText", "Header", colors.CanvasText),
            ["Tooltip"] = Get("Tooltip", colors.Canvas), ["TooltipText"] = Text("TooltipText", "Tooltip", colors.CanvasText),
            ["Focus"] = Get("Focus", colors.AccentColor),
            ["Success"] = success, ["SuccessBackground"] = Get("SuccessBackground", Mix(colors.Canvas, success, 0.12)),
            ["SuccessText"] = Text("SuccessText", "SuccessBackground", colors.CanvasText),
            ["Warning"] = warning, ["WarningBackground"] = Get("WarningBackground", Mix(colors.Canvas, warning, 0.12)),
            ["WarningText"] = Text("WarningText", "WarningBackground", colors.CanvasText),
            ["Error"] = error, ["ErrorBackground"] = Get("ErrorBackground", Mix(colors.Canvas, error, 0.12)),
            ["ErrorText"] = Text("ErrorText", "ErrorBackground", colors.CanvasText),
            ["InfoBackground"] = Get("InfoBackground", colors.Canvas),
            ["InfoText"] = Text("InfoText", "InfoBackground", colors.CanvasText),
            ["Destructive"] = Get("Destructive", error),
            ["DestructiveText"] = Get("DestructiveText", TextOn(Get("Destructive", error))),
            ["InactiveCanvas"] = Get("InactiveCanvas", colors.Canvas),
            ["InactiveCanvasText"] = Text("InactiveCanvasText", "InactiveCanvas", colors.CanvasText),
        };
        foreach (var (name, color) in supplied)
            if (!AllVariables.ContainsKey(name) && Property(name) is not null) result[name] = color;
        return result;
    }
}
